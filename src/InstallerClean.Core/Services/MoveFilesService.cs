using System.ComponentModel;
using System.IO.Abstractions;
using InstallerClean.Helpers;
using InstallerClean.Interop.Native;
using InstallerClean.Models;
using InstallerClean.Resources;

namespace InstallerClean.Services;

/// <summary>
/// Default <see cref="IMoveFilesService"/>. Once the destination has passed its gates, the
/// service asks Windows whether a rename from the app's own folder inside the Installer
/// folder reaches it (<see cref="IInHandBatch.ReachOf"/>), and every file of the batch
/// then goes one of two ways.
///
/// WHERE A RENAME REACHES THE DESTINATION, each file goes through four steps:
/// 1. Look (<see cref="BatchRun.Look"/>). Not clean: settle, and start the file again if
///    it is still in the batch.
/// 2. Ask the host's question, open the file so that nothing else can open it, and rename
///    it out of its place into the batch's folder.
/// 3. Look again. Not clean: put the file back, settle, and start it again.
/// 4. Rename the file through its handle to its name in the destination, replacing
///    nothing.
///
/// WHERE IT DOES NOT, THE COPY COMES FIRST, with the original untouched in its place:
/// 1. Look, so that a copy is not made for nothing.
/// 2. Open the original to read, sharing reading, and copy it into the destination under
///    a temporary name. Finish the copy: write it through to the device, check its length,
///    carry the attributes and the last-write time across, and give it its real name.
/// 3. Look. Not clean: settle, keeping the copy.
/// 4. Ask the host's question, open the original so that nothing else can open it, check
///    that it is still the file the copy was made from, and rename it into the batch's
///    folder.
/// 5. Look again. Not clean: put the original back and settle, keeping the copy.
/// 6. Delete the original through its handle.
/// A copy whose original then stays where it was, for any reason, is removed.
///
/// THE FILE LEAVES ITS PLACE BEFORE THE LAST LOOK ON BOTH WAYS, for the reason
/// <see cref="DeleteFilesService"/> gives, and nothing that can block runs between the
/// open that shares nothing and the finish. That is why the copy is made, finished and
/// named before the original is taken: a destination that is slow or fails is never what
/// keeps a file out of its place.
/// </summary>
public sealed class MoveFilesService : IMoveFilesService
{
    /// <summary>How much of a file one read of a copy takes. A cancel is honoured between reads.</summary>
    private const int CopyBufferBytes = 1 << 20;

    /// <summary>
    /// How many times running a file's copy may be made again because the file changed
    /// between the copy and the take, before the file is filed as an IO failure.
    /// </summary>
    private const int CopyAttemptsLimit = 3;

    private readonly IFileSystem _fs;
    private readonly IPendingRebootService _signals;
    private readonly IBatchCheck _batchCheck;
    private readonly IInHandFolder _hand;
    private readonly IDestinationGuard _destinationGuard;

    /// <summary>
    /// Test-only real-folder override for the Installer folder (null in production, which
    /// uses the real <c>C:\Windows\Installer</c>). Lets the real-filesystem integration
    /// tests treat a %TEMP% sandbox as the cache so a legitimately in-bounds source is not
    /// refused, without ever touching the real cache. Mirrors
    /// <c>FileSystemScanService</c>'s own installer-folder override.
    /// </summary>
    private readonly string? _installerFolderOverride;

    public MoveFilesService(
        IFileSystem fileSystem, IPendingRebootService signals, IBatchCheck batchCheck, IInHandFolder hand,
        IDestinationGuard destinationGuard)
        : this(fileSystem, signals, batchCheck, hand, destinationGuard, null) { }

    /// <summary>
    /// Test constructor for a substituted filesystem: signals that read clean, a hand and
    /// a destination guard that act by path through <paramref name="fileSystem"/>, and no
    /// later check, so a test whose look is not clean fails loudly unless it passes a
    /// batch check of its own through the seam constructor.
    /// </summary>
    internal MoveFilesService(IFileSystem fileSystem)
        : this(fileSystem, null, null, null, null, null) { }

    /// <summary>Seam constructor: each piece a test passes as null takes the quiet form the constructor above names.</summary>
    internal MoveFilesService(
        IFileSystem fileSystem, IPendingRebootService? signals, IBatchCheck? batchCheck, IInHandFolder? hand,
        IDestinationGuard? destinationGuard, string? installerFolderOverride)
    {
        _fs = fileSystem;
        _signals = signals ?? QuietInstallerSignals.Instance;
        _batchCheck = batchCheck ?? NoLaterBatchCheck.Instance;
        _hand = hand ?? new ByPathInHandFolder(fileSystem);
        _destinationGuard = destinationGuard ?? new ByPathDestinationGuard(fileSystem);
        _installerFolderOverride = installerFolderOverride;
    }

    /// <summary>The destination guard this service acts through, so a test can confirm the production path holds the handle one.</summary>
    internal IDestinationGuard DestinationGuard => _destinationGuard;

    /// <summary>The hand this service takes files through, so a test can confirm the production path holds the handle one.</summary>
    internal IInHandFolder Hand => _hand;

    public Task<MoveResult> MoveFilesAsync(
        StandingCheck check,
        string destinationFolder,
        IProgress<OperationProgress>? progress = null,
        CancellationToken cancellationToken = default,
        Func<string, bool>? mayAct = null) =>
        Task.Run(() => RunAsync(check, destinationFolder, progress, cancellationToken, mayAct), cancellationToken);

    private async Task<MoveResult> RunAsync(
        StandingCheck check, string destinationFolder, IProgress<OperationProgress>? progress,
        CancellationToken cancellationToken, Func<string, bool>? mayAct)
    {
        // The check is this call's from here, whichever way the call ends.
        using var owned = check;

        // The destination gates are inside the worker, not on the
        // caller's thread, which for the GUI is the dispatcher: two of them
        // resolve a user-chosen path through CreateFile and
        // GetFinalPathNameByHandle, twice over, and for a share path open its
        // nearest folder through the share again to read its file id, and a
        // mapped drive or a UNC share that has gone away stalls each one for
        // the SMB timeout with the operating overlay already up and its
        // Cancel button unpressable.

        // Reject relative destinations: FullPaths.GetFullPath would otherwise
        // resolve them against the process CWD, and the CLI host's CWD
        // is whatever the caller invoked it from.
        if (!PathText.IsPathFullyQualified(destinationFolder))
            throw new LocalisedInvalidOperationException(
                string.Format(Strings.Error_DestinationNotFullyQualified, destinationFolder));

        // A destination where a moved file's path would reach MAX_PATH is refused before
        // any check below reads the destination, so a refusal touches nothing.
        if (MoveDestinationLength.TooLong(destinationFolder, check.Files))
            throw new LocalisedInvalidOperationException(
                string.Format(Strings.Error_PathTooLong, destinationFolder));

        // Destination must not resolve inside C:\Windows\Installer;
        // ResolveFinalPath expands junctions so a reparse-point
        // destination cannot smuggle the batch into the cache folder.
        if (InstallerCacheHelpers.IsInstallerFolderOrChild(destinationFolder))
            throw new LocalisedInvalidOperationException(
                string.Format(Strings.Error_MoveIntoInstaller, destinationFolder));

        // System-folder gate at the service boundary, not just at the
        // call sites: %SystemRoot%, %ProgramFiles%, %ProgramFiles(x86)%
        // and %ProgramData% sit on the Win32 DLL search path and the
        // SxS resolution path. Anchoring the check inside MoveFilesAsync
        // means any future caller (integration test, new automation
        // entry point) that bypasses the GUI/CLI validation block still
        // hits the same gate.
        if (InstallerCacheHelpers.IsSystemFolderOrChild(destinationFolder))
            throw new LocalisedInvalidOperationException(
                string.Format(Strings.Error_DestinationInSystemFolder, destinationFolder));

        // The create makes the destination and every missing folder above it.
        // A create that fails, or a write test that fails, comes before the
        // first file moves, so the folders this call made are removed again,
        // each only while it is empty. The gates between the two leave them: a
        // destination that has just been shown to resolve into the installer
        // cache or a system folder is not a place to remove a folder from.
        var createdFolders = new List<string>();
        try
        {
            CreateDestinationFolder(destinationFolder, createdFolders);
        }
        catch
        {
            CreatedFolders.RemoveWhileEmpty(_fs, _destinationGuard, createdFolders);
            throw;
        }

        // Re-check after the create closes the TOCTOU window
        // where a junction could be swapped into the leaf.
        if (InstallerCacheHelpers.IsInstallerFolderOrChild(destinationFolder))
            throw new LocalisedInvalidOperationException(
                string.Format(Strings.Error_MoveIntoInstaller, destinationFolder));
        if (InstallerCacheHelpers.IsSystemFolderOrChild(destinationFolder))
            throw new LocalisedInvalidOperationException(
                string.Format(Strings.Error_DestinationInSystemFolder, destinationFolder));

        // Capture the canonical destination once, then re-resolve
        // per iteration. The per-iteration check catches a junction
        // swap on the destination's parent folder during the loop:
        // without it, a relabelled leaf folder would silently route
        // the remaining files into the junction's target. None of
        // these checks is redundant on another: each answers for a
        // moment the others cannot see, the pre-loop pair for the
        // CreateDirectory point, the pair below for the value this
        // capture hands the loop, and the loop-body check for every
        // per-file move.
        // The bool is kept, not discarded: it says whether the kernel really
        // expanded this path, and the per-iteration check below compares
        // proof states as well as strings.
        var canonicalProven = InstallerCacheHelpers.TryResolveFinalPath(
            destinationFolder, out var canonicalRaw);
        var canonicalDestination = canonicalRaw.TrimEnd(Path.DirectorySeparatorChar);

        // THE GATES ABOVE APPROVED A DIFFERENT RESOLUTION FROM THE ONE THE
        // LOOP DEFENDS. They ask about destinationFolder and resolve it
        // themselves; the line above resolves the same string again, later,
        // and it is that answer the per-file comparison then holds everything
        // to. Without the pair below, a junction swapped in between the two
        // would leave the loop faithfully defending a baseline no gate had
        // seen, and ClassifyAbort could not notice: it asks whether the
        // resolution moved and whether proof was lost, and containment is
        // neither of those questions.
        //
        // Asked of the captured value rather than of the path a third time,
        // which is the point: the question has to be about the string being
        // defended. A capture whose resolution degraded answers "not inside"
        // and is let through, exactly as the gates above treat one, because
        // refusing on a path that merely could not be expanded would strand a
        // user with nowhere to move to.
        //
        // The per-file check in the loop is what holds each later resolution of
        // destinationFolder to this capture.
        if (InstallerCacheHelpers.ResolvesInsideInstallerFolder(canonicalDestination, canonicalProven))
            throw new LocalisedInvalidOperationException(
                string.Format(Strings.Error_MoveIntoInstaller, destinationFolder));
        if (InstallerCacheHelpers.ResolvesInsideSystemFolder(canonicalDestination, canonicalProven))
            throw new LocalisedInvalidOperationException(
                string.Format(Strings.Error_DestinationInSystemFolder, destinationFolder));

        try
        {
            ProbeDestinationWriteable(destinationFolder);
        }
        catch
        {
            CreatedFolders.RemoveWhileEmpty(_fs, _destinationGuard, createdFolders);
            throw;
        }

        // An empty batch has made and tested its destination, as any batch does, and has
        // nothing more to do.
        if (check.Files.Count == 0)
            return new MoveResult(0, Array.Empty<FileOperationError>());

        MoveResult result;
        MoveAbortReason? abortReason;
        try
        {
            (result, abortReason) = await RunBatchAsync(
                check, destinationFolder, canonicalDestination, canonicalProven, progress, cancellationToken,
                mayAct).ConfigureAwait(false);
        }
        catch (LocalisedAccessException)
        {
            // The destination would not open for the batch, which comes before the
            // first file, as a write test that fails does.
            CreatedFolders.RemoveWhileEmpty(_fs, _destinationGuard, createdFolders);
            throw;
        }

        // A guard that trips mid-flight throws, like this service's other guards and
        // unlike a cancel, which is the user's own choice. What it must not do is take
        // the batch's account of itself with it, which is why the result is built first
        // and travels on the exception.
        //
        // The message names the folder the CALLER asked for, because it asks the reader
        // to go and check that folder and that is the one they configured. Where the
        // files ended up is a different question and travels separately.
        if (abortReason is { } abort)
            throw new MoveAbortedException(
                string.Format(Strings.Error_DestinationChangedMidBatch, destinationFolder),
                result, DisplayPath(canonicalDestination), abort);

        // A batch that moved nothing leaves no folder behind: the folders this call made
        // are removed again, each only while it is empty, once the batch has let go of
        // the destination. A batch the destination guard stopped leaves them, a path that
        // has just been shown to lead somewhere else not being a place to remove a
        // folder from.
        if (result.MovedCount == 0)
            CreatedFolders.RemoveWhileEmpty(_fs, _destinationGuard, createdFolders);

        return result;
    }

    /// <summary>
    /// Runs the batch against a destination that has passed its gates, and returns its
    /// result with the condition the destination guard stopped it on, where it did.
    /// Everything the batch holds open is closed by the time this returns.
    /// </summary>
    private async Task<(MoveResult Result, MoveAbortReason? AbortReason)> RunBatchAsync(
        StandingCheck check, string destinationFolder, string canonicalDestination, bool canonicalProven,
        IProgress<OperationProgress>? progress, CancellationToken cancellationToken, Func<string, bool>? mayAct)
    {
        // Resolved once for the batch; the containment guard resolves each SOURCE per
        // file against it (see InstallerCacheRoot). Separate from canonicalDestination
        // above, which guards the other end and is re-resolved per file for a reason of
        // its own.
        var cacheRoot = InstallerCacheRoot.Resolve(_installerFolderOverride);
        using var run = new BatchRun(
            check, _batchCheck, _signals, _fs, cacheRoot, "Move", progress, cancellationToken);

        // The batch's folder is made after the destination has taken its write test and
        // before any file is opened, and removed when the batch ends where it is empty.
        // Declared after the run so that it is disposed of first; every held file is
        // closed before either.
        using var hand = _hand.OpenBatch(
            _installerFolderOverride ?? InstallerCacheHelpers.InstallerFolder, out var handError);
        if (hand is null)
        {
            CrashLog.TryWrite(new IOException(
                "Move ended before its first file: the app's own folder inside the Installer folder could not be made.",
                new Win32Exception(handError)));
            return (new MoveResult(0, Array.Empty<FileOperationError>(),
                Ended: new BatchEnd(BatchEndReason.InHandFolderUnavailable, WindowsError: handError)), null);
        }

        // The destination is opened once and stays open for the batch. Every file sent
        // into it, every copy made in it and every copy removed from it is reached
        // relative to this handle, so each lands in the folder that passed the gates
        // above and not in one put in its place under the same name.
        using var destination = _destinationGuard.Open(destinationFolder, out var openError);
        if (destination is null)
            throw new LocalisedAccessException(
                string.Format(Strings.Error_CannotWriteFolder, destinationFolder),
                new Win32Exception(openError));

        // Asked once, before any file is taken, because a file that cannot go by a rename
        // has to be copied before it is taken. An answer that was not established is
        // treated as another volume: a copy made first is right on any volume.
        var byCopy = hand.ReachOf(destination, out _) != RenameReach.SameVolume;

        var batch = new Batch(
            this, run, hand, destination, destinationFolder, canonicalDestination, canonicalProven, byCopy,
            cancellationToken, mayAct);
        var cancelled = false;
        try
        {
            // The batch's files, and then one more try for each that was in use.
            foreach (var lastTry in new[] { false, true })
            {
                if (lastTry) await run.BeforeTheSecondTryAsync().ConfigureAwait(false);
                var pass = lastTry ? run.InUse.ToList() : run.Files;
                foreach (var sourcePath in pass)
                {
                    var outcome = await batch.MoveOneAsync(sourcePath, lastTry).ConfigureAwait(false);
                    if (outcome == FileOutcome.Acted) batch.Moved++;
                    if (outcome == FileOutcome.BatchEnded) goto finished;
                }
            }
        finished:;
        }
        catch (OperationCanceledException)
        {
            // What moved before the cancel is returned, not thrown away. The cancel
            // checks, a cancel inside a copy or a settle and the host's mayAct answering
            // no are the only sources: the per-file catch-all lets only those through
            // (BatchStop.EndsBatch).
            cancelled = true;
        }

        // A batch that stopped before the second try at its in-use files files them now.
        run.FileWhatIsStillInUse();

        // Outside the cancel catch so a batch the user stopped still accounts for what
        // its failures cost the log.
        run.FailureLog.WriteClosingEntry();

        return (new MoveResult(batch.Moved, run.Errors.AsReadOnly(), cancelled,
            run.HeldBack.ToList().AsReadOnly(), run.HeldBackReasons, run.Ended,
            run.SourceRootsGivenUp.ToList().AsReadOnly(), run.SourceWaitCount, run.CheckCounts), batch.AbortReason);
    }

    /// <summary>
    /// One call's batch: what the steps on each file need, kept for the length of the call
    /// and no longer.
    /// </summary>
    private sealed class Batch(
        MoveFilesService service, BatchRun run, IInHandBatch hand, DestinationFolder destination,
        string destinationFolder, string canonicalDestination, bool canonicalProven, bool byCopy,
        CancellationToken cancellationToken, Func<string, bool>? mayAct)
    {
        private byte[]? _copyBuffer;

        /// <summary>How many files have moved.</summary>
        internal int Moved { get; set; }

        /// <summary>Set where the destination guard stopped the batch, to the condition it stopped on.</summary>
        internal MoveAbortReason? AbortReason { get; private set; }

        /// <summary>
        /// Moves one file, and removes its copy from the destination wherever a copy was
        /// made and the original then stayed: the file was dropped by a settle, was
        /// refused at the take, could not be deleted, or the batch ended or was cancelled
        /// first. A file reported as not moved is then one the user has exactly one of.
        /// </summary>
        internal async Task<FileOutcome> MoveOneAsync(string sourcePath, bool lastTry)
        {
            var copy = new Copy();
            var outcome = FileOutcome.NotActed;
            try
            {
                outcome = await TryMoveAsync(sourcePath, lastTry, copy).ConfigureAwait(false);
                return outcome;
            }
            finally
            {
                if (copy.Made && outcome != FileOutcome.Acted) Discard(copy, PathText.GetFileName(sourcePath));
            }
        }

        private async Task<FileOutcome> TryMoveAsync(string sourcePath, bool lastTry, Copy copy)
        {
            var fileName = PathText.GetFileName(sourcePath);

            // Each turn is one try at the file. A settle that keeps the file in the batch
            // starts it again from the top, so every check on it and on the destination is
            // made again; a copy already made and finished is kept and not made twice.
            while (true)
            {
                cancellationToken.ThrowIfCancellationRequested();
                if (!run.InBatch(sourcePath)) return FileOutcome.NotActed;

                IHeldFile? held = null;
                try
                {
                    // The host's question is asked before the report as well as before the
                    // take, so a batch the host has stopped covering stops before this
                    // file's progress line.
                    if (mayAct is not null && !mayAct(sourcePath)) throw new BatchStoppedException();
                    // Before the checks below, so the visible counter advances on missing
                    // and reparse-point entries. Inside the try, so a progress consumer
                    // that throws costs this one file an error and not the batch.
                    run.ReportReached(sourcePath);
                    // Asked again after the report, which can block on the console: a
                    // cancel that came meanwhile leaves this file alone.
                    cancellationToken.ThrowIfCancellationRequested();

                    // Re-resolve the destination and compare it with the capture, AFTER
                    // the report and not before it: the report hands control to a consumer
                    // that can run for as long as it likes, and the check comes after it
                    // so that no consumer runs between the check and the move.
                    //
                    // WHAT THE CHECK IS FOR. Each file is sent relative to the handle
                    // opened on the destination when the batch began, so a folder put in
                    // the destination's place cannot take a file. What a swap would do is
                    // leave the path the user chose naming a folder the files are not in.
                    // The batch stops at the first file that finds the path resolving
                    // elsewhere, and reports the folder the files are in.
                    //
                    // WHAT IS COMPARED: whether the kernel expanded the path, and the path
                    // it expanded to, each against the capture. A re-resolve that has lost
                    // the proof the capture had stops the batch whatever its string says,
                    // because a resolve that DEGRADED is exactly a path whose reparse
                    // points went UNexpanded, which is the one thing a containment check
                    // exists to see through. A re-resolve naming a different folder stops
                    // it too. See ClassifyAbort, which keeps the two apart.
                    var resolveProven = InstallerCacheHelpers.TryResolveFinalPath(
                        destinationFolder, out var currentRaw);
                    var currentResolved = currentRaw.TrimEnd(Path.DirectorySeparatorChar);
                    if (ClassifyAbort(canonicalProven, resolveProven, canonicalDestination, currentResolved)
                        is { } abort)
                    {
                        AbortReason = abort;

                        // The only record of which condition fired. The host is told the
                        // batch stopped and where the files are, which is the same either
                        // way. Once per batch, on a path that ends it. The noun agrees
                        // with the batch size, which is what "of {total}" governs.
                        var total = run.Files.Count;
                        var batchFiles = total == 1 ? "file" : "files";
                        CrashLog.TryWrite(new InvalidOperationException(
                            $"Move stopped by the destination guard ({abort}). Captured "
                            + $"'{canonicalDestination}' (resolved: {canonicalProven}); re-resolved "
                            + $"'{currentResolved}' (resolved: {resolveProven}). "
                            + $"{Moved} of {total} {batchFiles} had moved."));

                        // Out through the batch's one exit and not up the stack: files
                        // have already left C:\Windows\Installer, so the count, the size
                        // and the line telling the user they can put them back are all
                        // real, and the failure log's closing entry is still owed.
                        return FileOutcome.BatchEnded;
                    }

                    if (!run.PassesSourceChecks(sourcePath)) return FileOutcome.NotActed;

                    if (!copy.Made)
                    {
                        // A NAME ALREADY IN THE DESTINATION IS REFUSED AND NOT WORKED
                        // AROUND, and what decides that is a promise the main window makes
                        // before the move. It tells the reader that putting the moved
                        // files back into the cache folder restores everything, and
                        // Windows Installer looks for a cached package by the exact path
                        // it recorded. A file parked as "thing (1).msi" does not go back
                        // as "thing.msi", so a suffix appended here would make that
                        // sentence false for the file it renamed, with nothing recording
                        // which file that was. Refusing the one file keeps the promise
                        // true for every file that did move, and costs the user nothing
                        // they cannot recover: the file is still in the cache, and a
                        // different folder or an emptied one moves it.
                        //
                        // IT SITS AFTER THE CONTAINMENT GUARD DELIBERATELY. Above the
                        // guard, a source that does not resolve inside the cache would be
                        // reported as a name clash in the backup folder, a cause nobody
                        // has shown, and not as the out-of-bounds or unresolvable path the
                        // guard established it to be. The guard's reason outranks this one
                        // wherever both apply.
                        //
                        // The rename and the copy's own naming both replace nothing, so a
                        // file or a link that appears at the name after this test stays as
                        // it is and the file is refused then. This test is what makes the
                        // ordinary clash a stated refusal before anything is opened or
                        // copied.
                        if (service._fs.File.Exists(PathText.Combine(destinationFolder, fileName)))
                        {
                            run.Refuse(new DestinationCollision(sourcePath, fileName));
                            return FileOutcome.NotActed;
                        }

                        if (byCopy)
                        {
                            // The look before the copy. The copy would do no harm; the
                            // look spares making one for nothing.
                            var before = run.Look();
                            if (before != LookResult.Clean)
                            {
                                if (!await run.SettleAsync(before).ConfigureAwait(false))
                                    return FileOutcome.BatchEnded;
                                continue;
                            }

                            if (!MakeCopy(sourcePath, fileName, lastTry, copy)) return FileOutcome.NotActed;
                        }
                    }

                    // The look before the take.
                    var look = run.Look();
                    if (look != LookResult.Clean)
                    {
                        if (!await run.SettleAsync(look).ConfigureAwait(false)) return FileOutcome.BatchEnded;
                        continue;
                    }

                    // The host's question is the last thing before the file is opened.
                    if (mayAct is not null && !mayAct(sourcePath)) throw new BatchStoppedException();
                    held = run.Hold(hand, sourcePath, lastTry);
                    if (held is null) return FileOutcome.NotActed;

                    if (byCopy)
                    {
                        // The copy was read through one open and the file is taken through
                        // another. Only a file whose identity, length and last-write time
                        // are those the copy was made from is the file the copy is of.
                        if (!held.TryReadStamp(out var now))
                        {
                            held.Dispose();
                            held = null;
                            run.FailureLog.Record(new IOException(
                                $"Move: {sourcePath} could not be read again after its copy was made, so the copy was not shown to be of it."));
                            run.Refuse(new IOFailure(sourcePath));
                            return FileOutcome.NotActed;
                        }
                        if (now != copy.Stamp)
                        {
                            held.Dispose();
                            held = null;
                            Discard(copy, fileName);
                            if (copy.Attempts >= CopyAttemptsLimit)
                            {
                                run.FailureLog.Record(new IOException(
                                    $"Move: {sourcePath} changed after each of {copy.Attempts} copies of it was made."));
                                run.Refuse(new IOFailure(sourcePath));
                                return FileOutcome.NotActed;
                            }
                            continue;
                        }
                    }

                    if (!run.TakeOut(held, lastTry))
                    {
                        held = null;
                        return FileOutcome.NotActed;
                    }

                    // The look with the file out of its place.
                    look = run.Look();
                    if (look != LookResult.Clean)
                    {
                        var back = run.PutBack(held, hand);
                        held = null;
                        if (!await run.SettleAsync(look).ConfigureAwait(false)) return FileOutcome.BatchEnded;
                        if (!back) return FileOutcome.NotActed;
                        continue;
                    }

                    // The finish. By copy, the original is deleted through its handle,
                    // where a true answer means closing the handle deletes it. By rename,
                    // the file goes on to its name in the destination. A refusal of either
                    // puts the file back.
                    if (byCopy ? held.Delete(out var finishError) : held.MoveInto(destination, fileName, out finishError))
                    {
                        held.Dispose();
                        held = null;
                        run.Done(sourcePath);
                        return FileOutcome.Acted;
                    }

                    var returned = run.PutBack(held, hand);
                    held = null;
                    if (!returned) return FileOutcome.NotActed;
                    if (byCopy)
                        run.RefuseByWindowsError(sourcePath, finishError, "the delete of the original");
                    else if (finishError == Kernel32.ERROR_ALREADY_EXISTS)
                        run.Refuse(new DestinationCollision(sourcePath, fileName));
                    else
                        // ERROR_NOT_SAME_DEVICE is filed here with every other refusal. The
                        // rename is made relative to the destination's open handle, which
                        // stays on the volume it was opened on, so the answer says nothing
                        // about where the destination's path now leads.
                        run.RefuseForDestination(sourcePath, finishError, "the rename into the destination");
                    return FileOutcome.NotActed;
                }
                // Framework exceptions carry detail that exists nowhere else once the
                // category has been filed, so each is logged.
                catch (Exception ex) when (!BatchStop.EndsBatch(ex, cancellationToken))
                {
                    run.FailureLog.Record(ex);
                    var returned = held is null || run.PutBack(held, hand);
                    held = null;
                    if (returned) run.Refuse(Categorise(ex, sourcePath));
                    return FileOutcome.NotActed;
                }
                finally
                {
                    // Every way out with a file in hand ends in a put-back: closing the
                    // handle alone leaves the file in the batch's folder.
                    if (held is not null) run.PutBack(held, hand);
                }
            }
        }

        /// <summary>
        /// Copies <paramref name="sourcePath"/> into the destination and finishes the
        /// copy under its real name. False where no finished copy was made, with the file
        /// filed, or remembered as in use, and nothing of the copy left in the
        /// destination. The original is read through a handle that shares reading, which
        /// Windows Installer's own opens of a cached file succeed beside, and is not
        /// otherwise touched.
        /// </summary>
        private bool MakeCopy(string sourcePath, string fileName, bool lastTry, Copy copy)
        {
            copy.Attempts++;
            using var source = run.OpenToCopy(hand, sourcePath, lastTry);
            if (source is null) return false;

            var target = service._destinationGuard.CreateCopy(destination, out var createError);
            if (target is null)
            {
                run.RefuseForDestination(sourcePath, createError, "the creation of the copy");
                return false;
            }

            // Disposing of a target that did not finish removes its temporary file.
            using (target)
            {
                var buffer = _copyBuffer ??= new byte[CopyBufferBytes];
                try
                {
                    // Whole arrays, each written straight through: the target's stream
                    // keeps no buffer of its own.
                    int read;
                    while ((read = source.Content.Read(buffer, 0, buffer.Length)) > 0)
                    {
                        cancellationToken.ThrowIfCancellationRequested();
                        target.Content.Write(buffer, 0, read);
                    }
                }
                catch (Exception ex) when (!BatchStop.EndsBatch(ex, cancellationToken))
                {
                    run.FailureLog.Record(ex);
                    run.Refuse(Categorise(ex, sourcePath));
                    return false;
                }

                // The read handle is closed before the copy takes its real name.
                var stamp = source.Stamp;
                var attributes = source.Attributes;
                source.Dispose();

                switch (target.Finish(fileName, stamp.Length, attributes, stamp.LastWriteTime, out var finishError))
                {
                    case CopyFinish.Finished:
                        copy.Made = true;
                        copy.Stamp = stamp;
                        return true;
                    case CopyFinish.NameTaken:
                        run.Refuse(new DestinationCollision(sourcePath, fileName));
                        return false;
                    case CopyFinish.LengthDiffers:
                        run.FailureLog.Record(new IOException(
                            $"Move: the copy of {sourcePath} was not the length of the file, so it was removed."));
                        run.Refuse(new IOFailure(sourcePath));
                        return false;
                    default:
                        run.RefuseForDestination(sourcePath, finishError, "the finish of the copy");
                        return false;
                }
            }
        }

        /// <summary>
        /// Removes a finished copy from the destination, relative to the handle the copy
        /// was created through. A copy that will not go is the one state with nothing to
        /// say for itself: the error filed for the file says it was not moved, which is
        /// true, and does not mention the copy. It is recorded.
        /// </summary>
        private void Discard(Copy copy, string fileName)
        {
            copy.Made = false;
            if (!service._destinationGuard.TryDiscardCopy(destination, fileName, out var failureCode))
                run.FailureLog.Record(new InvalidOperationException(
                    $"A copy of {fileName} could not be removed from {destinationFolder} after its original stayed "
                    + "where it was, so that file now exists in both places.",
                    new Win32Exception(failureCode)));
        }
    }

    /// <summary>The copy of one file, across the tries a settle makes at that file.</summary>
    private sealed class Copy
    {
        /// <summary>Whether a finished copy stands under the file's name in the destination.</summary>
        internal bool Made { get; set; }

        /// <summary>The stamp of the file as the copy was read from it.</summary>
        internal FileStamp Stamp { get; set; }

        /// <summary>How many times the copy has been begun.</summary>
        internal int Attempts { get; set; }
    }

    /// <summary>
    /// Whether the destination guard stops the batch, and on which of its two
    /// conditions, from the capture and the re-resolve. Null means carry on.
    ///
    /// The two answers exist even though the outcome is identical, and the reason
    /// is entirely about what may be SAID. OR them into one bool and the only
    /// account of a stopped batch is that its destination changed, which is false
    /// of a share that dropped or an ACL that closed: nothing replaced or
    /// redirected anything, and the folder is exactly where the user put it. One
    /// bool cannot answer WHY without answering it wrongly for the batches that
    /// met the other condition.
    ///
    /// Separated out because the loop cannot be driven into the second condition
    /// by any test the suite can write: the resolve deliberately bypasses the
    /// injected <see cref="System.IO.Abstractions.IFileSystem"/> and asks the real
    /// kernel, and on a local disk the walk always finds an existing ancestor to
    /// open, so a real-filesystem test can produce a changed target and cannot
    /// produce a lost one. As a function of the four inputs it is pinned at every
    /// combination.
    /// </summary>
    internal static MoveAbortReason? ClassifyAbort(
        bool canonicalProven, bool resolveProven,
        string canonicalDestination, string currentResolved)
    {
        // Order matters and is not a precedence chain over one condition: a
        // degraded re-resolve returns a best-effort string that may or may not
        // compare equal, so asking the comparison first would report some lost
        // destinations as changed ones and the rest not at all.
        if (canonicalProven && !resolveProven) return MoveAbortReason.StoppedResolving;
        if (!currentResolved.Equals(canonicalDestination, StringComparison.OrdinalIgnoreCase))
            return MoveAbortReason.ResolvesElsewhere;
        return null;
    }

    /// <summary>
    /// A resolved folder path in the form a person should read it in.
    ///
    /// The guard trims the trailing separator so two resolutions of one folder
    /// compare equal whichever way the kernel spelled them, and that is right for
    /// a comparison and wrong for a drive root: it turns <c>D:\</c> into
    /// <c>D:</c>, which names the process's current directory on that drive
    /// rather than the root of it.
    /// </summary>
    private static string DisplayPath(string resolved) =>
        resolved.EndsWith(':') ? resolved + Path.DirectorySeparatorChar : resolved;

    /// <summary>
    /// Files an exception a read or a write raised by its kind, so the copy's own
    /// failures are named as a refusal by Windows is (<see cref="BatchRun.RefuseByWindowsError"/>):
    /// a sharing or lock violation as in use, another IO error as an IO failure, an access
    /// refusal as that.
    /// </summary>
    private static FileOperationError Categorise(Exception ex, string sourcePath) => ex switch
    {
        UnauthorizedAccessException => new AccessDenied(sourcePath),
        IOException io when io.HResult is unchecked((int)0x80070020) or unchecked((int)0x80070021)
            => new FileInUse(sourcePath),
        IOException => new IOFailure(sourcePath),
        _ => new UnknownError(sourcePath),
    };

    /// <summary>
    /// Wraps <see cref="CreatedFolders.Create"/>, which fills <paramref name="createdFolders"/>
    /// with the folders the create made, so framework-thrown
    /// UnauthorizedAccessException and IOException are remapped to the
    /// same UnauthorizedAccessException-with-localised-message that
    /// <see cref="ProbeDestinationWriteable"/> produces. The caller's
    /// catch block sees one consistent contract; the framework's
    /// path-bearing message is preserved on InnerException for crash
    /// log consumers but never reaches the displayed UI.
    /// </summary>
    private void CreateDestinationFolder(string folder, List<string> createdFolders)
    {
        try
        {
            CreatedFolders.Create(_fs, _destinationGuard, folder, createdFolders);
        }
        catch (UnauthorizedAccessException ex)
        {
            throw new LocalisedAccessException(
                string.Format(Strings.Error_CannotWriteFolder, folder), ex);
        }
        catch (IOException ex)
        {
            throw new LocalisedAccessException(
                string.Format(Strings.Error_CannotWriteFolder, folder), ex);
        }
    }

    private void ProbeDestinationWriteable(string folder)
    {
        // Fail fast with one clean error rather than collecting per-file errors for
        // every source when the destination will not take a file. The guard creates
        // a write-test file through a handle opened on the folder, opens it again to
        // read it, and removes it, so a folder another account can write cannot steer
        // the test to a file in another folder. A folder that takes the file and will
        // not let it be read back is refused with its own message.
        if (!_destinationGuard.TryProbeWritable(folder, out var failureCode, out var refusal))
            throw new LocalisedAccessException(
                string.Format(
                    refusal == WriteProbeRefusal.ReadBackRefusedAccess
                        ? Strings.Error_FolderWillNotGiveFilesBack
                        : Strings.Error_CannotWriteFolder,
                    folder),
                new System.ComponentModel.Win32Exception(failureCode));
    }
}
