using System.ComponentModel;
using System.IO.Abstractions;
using InstallerClean.Helpers;
using InstallerClean.Models;

namespace InstallerClean.Services;

/// <summary>
/// Default <see cref="IDeleteFilesService"/>. Each file goes through four steps, and the
/// order of them is what the service is for:
///
/// 1. Look (<see cref="BatchRun.Look"/>). Not clean: settle, and start the file again if
///    it is still in the batch. Nothing has been touched.
/// 2. Ask the host's question, open the file so that nothing else can open it, and rename
///    it out of its place into the batch's folder.
/// 3. Look again. Not clean: put the file back, settle, and start it again if it is still
///    in the batch.
/// 4. Through the handle, clear the read-only attribute if it is set and ask for the file
///    to be deleted when the handle closes; close the handle.
///
/// THE FILE LEAVES ITS PLACE BEFORE THE SECOND LOOK, and that order is the guard. Windows
/// Installer takes up a cached file only where it finds the file at its recorded path, and
/// writes its record of the use before it looks. So a file that is out of its place, with
/// the records then read unchanged, has not been taken up, however long the calls took.
/// Deleting first and looking after would act on a file a record had just been written
/// for.
///
/// NOTHING THAT CAN BLOCK RUNS BETWEEN STEP 2'S OPEN AND STEP 4. From the open on,
/// nothing else can open the file, so it is held only for the few calls that remove it:
/// the progress report, the host's question and the cancel check all come before the
/// open, and a cancel that arrives after it takes effect at the next file.
/// </summary>
public sealed class DeleteFilesService : IDeleteFilesService
{
    private readonly IFileSystem _fs;
    private readonly IPendingRebootService _signals;
    private readonly IBatchCheck _batchCheck;
    private readonly IInHandFolder _hand;

    /// <summary>
    /// Test-only real-folder override for the Installer folder (null in production). See
    /// the matching field on <c>MoveFilesService</c>: it lets the real-filesystem
    /// integration tests treat a %TEMP% sandbox as the cache without touching the real
    /// one, and does not let a MockFileSystem bypass the containment guard.
    /// </summary>
    private readonly string? _installerFolderOverride;

    public DeleteFilesService(
        IFileSystem fileSystem, IPendingRebootService signals, IBatchCheck batchCheck, IInHandFolder hand)
        : this(fileSystem, signals, batchCheck, hand, null) { }

    /// <summary>
    /// Test constructor for a substituted filesystem: signals that read clean, a hand that
    /// acts by path through <paramref name="fileSystem"/>, and no later check, so a test
    /// whose look is not clean fails loudly unless it passes a batch check of its own
    /// through the seam constructor.
    /// </summary>
    internal DeleteFilesService(IFileSystem fileSystem)
        : this(fileSystem, null, null, null, null) { }

    /// <summary>Seam constructor: each piece a test passes as null takes the quiet form the constructor above names.</summary>
    internal DeleteFilesService(
        IFileSystem fileSystem, IPendingRebootService? signals, IBatchCheck? batchCheck, IInHandFolder? hand,
        string? installerFolderOverride)
    {
        _fs = fileSystem;
        _signals = signals ?? QuietInstallerSignals.Instance;
        _batchCheck = batchCheck ?? NoLaterBatchCheck.Instance;
        _hand = hand ?? new ByPathInHandFolder(fileSystem);
        _installerFolderOverride = installerFolderOverride;
    }

    /// <summary>The hand this service takes files through, so a test can confirm the production path holds the handle one.</summary>
    internal IInHandFolder Hand => _hand;

    public Task<DeleteResult> DeleteFilesAsync(
        StandingCheck check,
        IProgress<OperationProgress>? progress = null,
        CancellationToken cancellationToken = default,
        Func<string, bool>? mayAct = null) =>
        Task.Run(() => RunAsync(check, progress, cancellationToken, mayAct), cancellationToken);

    private async Task<DeleteResult> RunAsync(
        StandingCheck check, IProgress<OperationProgress>? progress, CancellationToken cancellationToken,
        Func<string, bool>? mayAct)
    {
        // The check is this call's from here, whichever way the call ends.
        using var owned = check;
        if (check.Files.Count == 0)
            return new DeleteResult(0, Array.Empty<FileOperationError>());

        cancellationToken.ThrowIfCancellationRequested();

        // Resolved once for the batch; the containment guard resolves each file against
        // it (see InstallerCacheRoot).
        var cacheRoot = InstallerCacheRoot.Resolve(_installerFolderOverride);
        using var run = new BatchRun(
            check, _batchCheck, _signals, _fs, cacheRoot, "Delete", progress, cancellationToken);

        // The batch's folder is made before any file is opened, and removed when the
        // batch ends where it is empty. Declared after the run so that it is disposed of
        // first; every held file is closed before either.
        using var hand = _hand.OpenBatch(
            _installerFolderOverride ?? InstallerCacheHelpers.InstallerFolder, out var handError);
        if (hand is null)
        {
            CrashLog.TryWrite(new IOException(
                "Delete ended before its first file: the app's own folder inside the Installer folder could not be made.",
                new Win32Exception(handError)));
            return new DeleteResult(0, Array.Empty<FileOperationError>(),
                Ended: new BatchEnd(BatchEndReason.InHandFolderUnavailable, WindowsError: handError));
        }

        var deleted = 0;
        var cancelled = false;
        try
        {
            // The batch's files, and then one more try for each that was in use.
            foreach (var lastTry in new[] { false, true })
            {
                if (lastTry) await run.BeforeTheSecondTryAsync().ConfigureAwait(false);
                var pass = lastTry ? run.InUse.ToList() : run.Files;
                foreach (var filePath in pass)
                {
                    var outcome = await DeleteOneAsync(run, hand, filePath, lastTry, cancellationToken, mayAct)
                        .ConfigureAwait(false);
                    if (outcome == FileOutcome.Acted) deleted++;
                    if (outcome == FileOutcome.BatchEnded) goto finished;
                }
            }
        finished:;
        }
        catch (OperationCanceledException)
        {
            // What was deleted before the cancel is returned, not thrown away. The cancel
            // checks, a cancel inside a settle's wait or check and the host's mayAct
            // answering no are the only sources: the per-file catch-all lets only those
            // through (BatchStop.EndsBatch).
            cancelled = true;
        }

        // A batch that stopped before the second try at its in-use files files them now.
        run.FileWhatIsStillInUse();

        // Outside the cancel catch so a batch the user stopped still accounts for what
        // its failures cost the log.
        run.FailureLog.WriteClosingEntry();

        return new DeleteResult(deleted, run.Errors.AsReadOnly(), cancelled,
            run.HeldBack.ToList().AsReadOnly(), run.HeldBackReasons, run.Ended,
            run.SourceRootsGivenUp.ToList().AsReadOnly(), run.SourceWaitCount, run.CheckCounts);
    }

    private static async Task<FileOutcome> DeleteOneAsync(
        BatchRun run, IInHandBatch hand, string filePath, bool lastTry, CancellationToken cancellationToken,
        Func<string, bool>? mayAct)
    {
        // Each turn is one try at the file. A settle that keeps the file in the batch
        // starts it again from the top, so every check on it is made again.
        while (true)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (!run.InBatch(filePath)) return FileOutcome.NotActed;

            IHeldFile? held = null;
            try
            {
                // The host's question is asked before the report as well as before the
                // take, so a batch the host has stopped covering stops before this file's
                // progress line.
                if (mayAct is not null && !mayAct(filePath)) throw new BatchStoppedException();
                // Before the checks below, so a missing file still advances the visible
                // counter. Inside the try, so a progress consumer that throws costs this
                // one file an unknown error and not the batch.
                run.ReportReached(filePath);
                // Asked again after the report, which can block on the console: a cancel
                // that came meanwhile leaves this file alone.
                cancellationToken.ThrowIfCancellationRequested();

                if (!run.PassesSourceChecks(filePath)) return FileOutcome.NotActed;

                // Step 1.
                var look = run.Look();
                if (look != LookResult.Clean)
                {
                    if (!await run.SettleAsync(look).ConfigureAwait(false)) return FileOutcome.BatchEnded;
                    continue;
                }

                // Step 2. The host's question is the last thing before the file is opened.
                if (mayAct is not null && !mayAct(filePath)) throw new BatchStoppedException();
                held = run.Hold(hand, filePath, lastTry);
                if (held is null) return FileOutcome.NotActed;
                if (!run.TakeOut(held, lastTry))
                {
                    held = null;
                    return FileOutcome.NotActed;
                }

                // Step 3.
                look = run.Look();
                if (look != LookResult.Clean)
                {
                    var back = run.PutBack(held, hand);
                    held = null;
                    if (!await run.SettleAsync(look).ConfigureAwait(false)) return FileOutcome.BatchEnded;
                    if (!back) return FileOutcome.NotActed;
                    continue;
                }

                // Step 4. A true answer means closing the handle deletes the file. A false
                // one has left the read-only attribute as it was, and the file goes back.
                if (held.Delete(out var deleteError))
                {
                    held.Dispose();
                    held = null;
                    run.Done(filePath);
                    return FileOutcome.Acted;
                }

                var returned = run.PutBack(held, hand);
                held = null;
                if (returned) run.RefuseByWindowsError(filePath, deleteError, "the delete");
                return FileOutcome.NotActed;
            }
            catch (Exception ex) when (!BatchStop.EndsBatch(ex, cancellationToken))
            {
                run.FailureLog.Record(ex);
                var returned = held is null || run.PutBack(held, hand);
                held = null;
                if (returned) run.Refuse(new UnknownError(filePath));
                return FileOutcome.NotActed;
            }
            finally
            {
                // Every way out with a file in hand ends in a put-back: closing the handle
                // alone leaves the file in the batch's folder.
                if (held is not null) run.PutBack(held, hand);
            }
        }
    }
}

/// <summary>What one try at one file of a batch came to.</summary>
internal enum FileOutcome
{
    /// <summary>
    /// The file was not acted on: it was filed as an error, remembered as in use, or
    /// dropped from the batch. First member so that it is what the type's own zero carries.
    /// </summary>
    NotActed,

    /// <summary>The file was deleted or moved.</summary>
    Acted,

    /// <summary>The batch has ended (<see cref="BatchRun.Ended"/>), with this file in its place.</summary>
    BatchEnded,
}
