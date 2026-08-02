using System.IO.Abstractions;
using InstallerClean.Models;
using InstallerClean.Resources;

namespace InstallerClean.Services;

public sealed class MoveFilesService : IMoveFilesService
{
    private readonly IFileSystem _fs;
    private readonly IMutexProbe _mutex;

    /// <summary>
    /// Test-only real-folder override for the containment guard's cache root
    /// (null in production, which uses the real <c>C:\Windows\Installer</c>).
    /// Lets the real-filesystem integration tests treat a %TEMP% sandbox as the
    /// cache so a legitimately in-bounds source is not refused, without ever
    /// touching the real cache. Mirrors <c>FileSystemScanService</c>'s own
    /// installer-folder override.
    /// </summary>
    private readonly string? _installerFolderOverride;

    /// <summary>
    /// Constructor. The DI container injects the registered
    /// <see cref="IFileSystem"/> and <see cref="IMutexProbe"/> singletons in
    /// production; the mutex is held for the batch so a msiexec starting
    /// mid-move waits instead of racing the cache.
    /// </summary>
    public MoveFilesService(IFileSystem fileSystem, IMutexProbe mutex) : this(fileSystem, mutex, null) { }

    /// <summary>Test constructor. No mutex hold (the hold is exercised via the seam constructor below).</summary>
    internal MoveFilesService(IFileSystem fileSystem) : this(fileSystem, NullMutexProbe.Instance, null) { }

    /// <summary>Test constructor. Points the source containment guard at a real sandbox folder; no mutex hold.</summary>
    internal MoveFilesService(IFileSystem fileSystem, string? installerFolderOverride)
        : this(fileSystem, NullMutexProbe.Instance, installerFolderOverride) { }

    /// <summary>Seam constructor: an injected <see cref="IMutexProbe"/> (real or fake) plus the sandbox override.</summary>
    internal MoveFilesService(IFileSystem fileSystem, IMutexProbe mutex, string? installerFolderOverride)
    {
        _fs = fileSystem;
        _mutex = mutex;
        _installerFolderOverride = installerFolderOverride;
    }

    public Task<MoveResult> MoveFilesAsync(
        IEnumerable<string> filePaths,
        string destinationFolder,
        IProgress<OperationProgress>? progress = null,
        CancellationToken cancellationToken = default)
    {
        return Task.Run(() =>
        {
            // The three destination gates are inside the worker, not on the
            // caller's thread, which for the GUI is the dispatcher: two of them
            // resolve a user-chosen path through CreateFile and
            // GetFinalPathNameByHandle, twice over, and a mapped drive or a UNC
            // share that has gone away stalls each one for the SMB timeout with
            // the operating overlay already up and its Cancel button
            // unpressable.

            // Reject relative destinations: Path.GetFullPath would otherwise
            // resolve them against the process CWD, and the CLI host's CWD
            // is whatever the caller invoked it from.
            if (!Path.IsPathFullyQualified(destinationFolder))
                throw new LocalisedInvalidOperationException(
                    string.Format(Strings.Error_DestinationNotFullyQualified, destinationFolder));

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

            // Hold Global\_MSIExecute for the batch on this worker thread so a
            // msiexec starting mid-move waits on the mutex instead of racing the
            // cache. Acquired here (not on the dispatcher) and released in the
            // finally on the SAME thread (Win32 owner-thread rule); the body is
            // synchronous, so no await hops threads between acquire and release.
            // Held by a live transaction => refuse and touch nothing (the caller
            // re-checks the pending-reboot gate and shows its banner). A
            // DACL-refused acquire (lease null, not heldByAnother) proceeds
            // without the hold rather than refusing. This closes only the
            // sub-millisecond race after the host-side gate re-check has passed.
            //
            // What the hold costs, so nobody widens it and nobody removes it:
            // _MSIExecute is the machine-wide Windows Installer serialisation
            // mutex, so for as long as this batch runs, every installer on the
            // machine that wants it waits or fails with 1618. A single file
            // operation that hangs (a stalled network destination) therefore
            // holds the machine's installer lock until this process is killed.
            // That is accepted because the alternative is msiexec writing the
            // cache in the middle of a move, which costs a needed file rather
            // than a wait.
            //
            // Delete runs its volume probe before its acquire, and this does NOT
            // match it: everything between here and the loop is the destination
            // work, and running it before the acquire would create the
            // destination folder even on the runs the busy check above refuses.
            // A refusal that has touched nothing is worth more than a shorter
            // hold.
            var lease = _mutex.TryAcquire(PendingRebootService.MsiExecuteMutexName, out var heldByAnother);
            if (lease is null && heldByAnother)
                return new MoveResult(0, Array.Empty<FileOperationError>(), InstallerBusy: true);

            try
            {
            CreateDestinationFolder(destinationFolder);

            // Re-check after CreateDirectory closes the TOCTOU window
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
            // the remaining files into the junction's target. The
            // pre-loop IsInstallerFolderOrChild check covers the
            // CreateDirectory point; the loop-body check covers each
            // per-file move.
            var canonicalDestination = InstallerCacheHelpers.ResolveFinalPath(destinationFolder)
                .TrimEnd(Path.DirectorySeparatorChar);

            ProbeDestinationWriteable(destinationFolder);

            int moved = 0;
            var errors = new List<FileOperationError>();
            var failureLog = new PerItemFailureLog("Move",
                "The per-file list is on the completion screen.");
            // A skipped hold is not a refusal: TryAcquire returns null with
            // heldByAnother false on a DACL refusal or any other non-fatal
            // failure, and running on is the right call. It is recorded because
            // the hold is the only thing stopping a msiexec registering a package
            // mid-batch, so a run without it is the one window in which the
            // act-time re-verify's proof can go stale under the batch, and a
            // report of a needed file being removed could never be attributed to
            // it. Once per batch, so it costs nothing at any file count.
            if (lease is null)
                Helpers.CrashLog.TryWrite(new InvalidOperationException(
                    "Move ran without the Windows Installer mutex: it could not be acquired and was not held by another process."));
            // Resolved once for the batch; the guard resolves each SOURCE per
            // file against it (see InstallerCacheRoot). Separate from
            // canonicalDestination above, which guards the other end and is
            // re-resolved per iteration for a reason of its own.
            var cacheRoot = InstallerCacheRoot.Resolve(_installerFolderOverride);
            var pathList = filePaths as IReadOnlyList<string> ?? filePaths.ToList();
            var total = pathList.Count;
            bool cancelled = false;
            bool destinationChanged = false;

            try
            {
            for (int i = 0; i < total; i++)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var sourcePath = pathList[i];

                // Report progress before the skip checks so the
                // visible counter advances on missing / reparse-point
                // entries instead of jumping over them.
                progress?.Report(new OperationProgress(i + 1, total, _fs.Path.GetFileName(sourcePath)));

                // Re-resolve and compare to the canonical capture.
                var currentResolved = InstallerCacheHelpers.ResolveFinalPath(destinationFolder)
                    .TrimEnd(Path.DirectorySeparatorChar);
                if (!currentResolved.Equals(canonicalDestination, StringComparison.OrdinalIgnoreCase))
                {
                    // Out through the one exit below rather than straight up the
                    // stack: everything a stopped batch still owes is under this
                    // frame. Files have already left C:\Windows\Installer, so the
                    // count, the size and the line telling the user they can put
                    // them back are all real, and the failure log's closing entry
                    // accounts for what the batch's failures cost it.
                    destinationChanged = true;
                    break;
                }

                try
                {
                    if (!_fs.File.Exists(sourcePath))
                    {
                        errors.Add(new MissingSourceFile(sourcePath));
                        continue;
                    }

                    // Refuse a source that's a symlink or junction: a move can
                    // read through one and copy the target's contents into the
                    // user's folder, the receipts being with the check itself
                    // (StorageHelpers.CheckReparsePoint). Real-FS check
                    // (MockFileSystem cannot bypass). An attribute read that
                    // FAILS refuses the file too, but as UnknownError:
                    // SourceIsReparsePoint tells the
                    // user the file is a symlink, and a read that could not be
                    // made has not shown that.
                    var reparse = Helpers.StorageHelpers.CheckReparsePoint(sourcePath, out var reparseError);
                    if (reparse == Helpers.StorageHelpers.ReparseCheck.Yes)
                    {
                        errors.Add(new SourceIsReparsePoint(sourcePath));
                        continue;
                    }
                    if (reparse == Helpers.StorageHelpers.ReparseCheck.Unreadable)
                    {
                        failureLog.Record(reparseError!);
                        errors.Add(new UnknownError(sourcePath));
                        continue;
                    }

                    // Containment guard at the service boundary, matching the
                    // IsSystemFolderOrChild anchoring above: never move a source
                    // that does not resolve to a file directly in
                    // C:\Windows\Installer. The scan already filters candidates,
                    // but a corrupt LocalPackage value or a future caller could
                    // reach here with an out-of-bounds path; this is the choke
                    // point that refuses it. Reparse is handled just above, so a
                    // Refused here is an out-of-bounds path and
                    // CandidateOutsideCache says so; an Unproven one is a path
                    // that could not be resolved at all, which is the same
                    // refusal without the same claim.
                    var safety = CandidateGuard.CheckSafeToRemove(sourcePath, cacheRoot);
                    if (safety == CandidateGuard.RemovalSafety.Refused)
                    {
                        errors.Add(new CandidateOutsideCache(sourcePath));
                        continue;
                    }
                    if (safety == CandidateGuard.RemovalSafety.Unproven)
                    {
                        failureLog.Record(new InvalidOperationException(
                            $"Move refused: {sourcePath} could not be resolved, so it could not be shown to be inside the Installer cache."));
                        errors.Add(new UnknownError(sourcePath));
                        continue;
                    }

                    var fileName = _fs.Path.GetFileName(sourcePath);
                    var destPath = GetUniqueDestPath(destinationFolder, fileName);
                    _fs.File.Move(sourcePath, destPath);

                    // A move that returned without throwing has not necessarily
                    // happened: see ReconcileMove.
                    var halfMove = ReconcileMove(sourcePath, destPath, failureLog);
                    if (halfMove is null)
                        moved++;
                    else
                        errors.Add(halfMove);
                }
                // DestinationCollisionException alone is not logged: it is this
                // class's own control flow, thrown from one place, and its
                // error entry already states the whole cause. The rest are
                // framework exceptions whose detail exists nowhere else once
                // the category has been filed, so without this a move that
                // failed for an unforeseen reason leaves no trace at all.
                catch (DestinationCollisionException ex)
                {
                    errors.Add(new DestinationCollision(sourcePath, ex.FileName));
                }
                catch (Exception ex)
                {
                    failureLog.Record(ex);
                    errors.Add(Categorise(ex, sourcePath));
                }
            }
            }
            catch (OperationCanceledException)
            {
                // Return what moved before the cancel rather than throwing the
                // tally away. The per-file loop's ThrowIfCancellationRequested is
                // the only cancellation source and sits outside the inner
                // per-file catch, so only a real cancel lands here; a genuine
                // mid-batch error (a destination junction swap) still propagates.
                cancelled = true;
            }

            // Outside the cancel catch so a batch the user stopped still
            // accounts for what its failures cost the log.
            failureLog.WriteClosingEntry();

            // Pass CancellationToken.None: the prune is best-effort
            // post-operation cleanup. If the user pressed Cancel during
            // the prune (after all moves completed), propagating their
            // token would throw OperationCanceledException out of a
            // batch that actually succeeded - the caller would re-label
            // the run as "Move cancelled" even though every file moved.
            InstallerCacheHelpers.PruneEmptySubdirectories(_fs, CancellationToken.None);

            var result = new MoveResult(moved, errors.AsReadOnly(), cancelled);

            // A guard that trips mid-flight throws, like this service's other
            // guards and unlike a cancel, which is the user's own choice. What it
            // must not do is take the batch's account of itself with it, which is
            // why the result is built first and travels on the exception.
            if (destinationChanged)
                throw new MoveAbortedException(
                    string.Format(Strings.Error_DestinationChangedMidBatch, destinationFolder), result);

            return result;
            }
            finally
            {
                // Release on this same worker thread (Win32 owner-thread rule);
                // no-op when the acquire fell back (lease null).
                lease?.Dispose();
            }
        }, cancellationToken);
    }

    /// <summary>
    /// Settles what actually happened to a file <c>File.Move</c> reported as
    /// moved, and returns null when the file really has gone or the error to
    /// file when it has not.
    ///
    /// A move that returns without throwing is not proof the source is gone.
    /// .NET calls MoveFileEx with MOVEFILE_COPY_ALLOWED (dotnet/runtime,
    /// Interop.MoveFileEx.cs), and Win32 documents that flag as simulating a
    /// cross-volume move with CopyFile plus DeleteFile, then says outright: "If
    /// the file is successfully copied to a different volume and the original
    /// file is unable to be deleted, the function succeeds leaving the source
    /// file intact." A read-only source is exactly that case, DeleteFile being
    /// documented to fail with ERROR_ACCESS_DENIED on one, and a file another
    /// program holds open without FILE_SHARE_DELETE is a second. Same-drive
    /// moves never reach this state: a rename either happens or throws.
    ///
    /// Checked after the fact rather than pre-empted, because pre-clearing the
    /// attribute would mutate sources that never needed it and would need a
    /// same-volume test that cannot be had: a mount point inside one drive
    /// letter falls back to copy-and-delete like any other volume boundary.
    ///
    /// Clearing the read-only bit is safe here for the reasons the delete path's
    /// own comment sets out: the same two gates passed, the same confirmed
    /// intent.
    /// </summary>
    private FileOperationError? ReconcileMove(string sourcePath, string destPath, PerItemFailureLog failureLog)
    {
        if (!_fs.File.Exists(sourcePath)) return null;

        var clearedReadOnly = false;
        try
        {
            clearedReadOnly = ClearReadOnly(sourcePath);
            _fs.File.Delete(sourcePath);
            return null;
        }
        catch (Exception ex)
        {
            // An antivirus quarantining the file it has just watched being
            // copied takes the source out between the Exists above and this
            // throw. Source gone with the copy in place is the end state a
            // completed move produces, so that is a success; discarding the
            // copy would leave the user with neither.
            if (!_fs.File.Exists(sourcePath)) return null;

            failureLog.Record(ex);
            RestoreReadOnly(sourcePath, clearedReadOnly, failureLog);
            DiscardDestinationCopy(destPath, failureLog);
            return Categorise(ex, sourcePath);
        }
    }

    /// <summary>
    /// Removes the copy a half-completed move left in the destination, so a
    /// file reported as failed is a file the user still has exactly one of.
    ///
    /// A failure here is the one state with nothing to say for itself: the
    /// error the caller files says the file was left in place, which is true,
    /// and does not mention the copy. It is recorded rather than surfaced
    /// because it needs a destination that accepts a create and refuses a
    /// delete, which the write probe has already had to pass.
    ///
    /// CopyFile carries the source's attributes across, so the copy of a
    /// read-only source is read-only and the attribute has to come off before
    /// the delete. Safe on this file above any other: this process wrote it
    /// seconds ago at a name it chose.
    ///
    /// No check that what sits at destPath is still that file, deliberately:
    /// any identity check is itself racy and the window is milliseconds.
    /// </summary>
    private void DiscardDestinationCopy(string destPath, PerItemFailureLog failureLog)
    {
        try
        {
            ClearReadOnly(destPath);
            _fs.File.Delete(destPath);
        }
        catch (Exception ex)
        {
            failureLog.Record(new InvalidOperationException(
                $"A copy of {destPath} could not be removed after the move failed to remove its source, "
                + "so that file now exists in both places.", ex));
        }
    }

    /// <summary>
    /// Clears the read-only attribute, reporting whether there was one to clear
    /// so a caller that then fails can put it back.
    ///
    /// Throws are the caller's: this is one step of an operation that fails
    /// closed as a whole.
    /// </summary>
    private bool ClearReadOnly(string filePath)
    {
        var attributes = _fs.File.GetAttributes(filePath);
        if (!attributes.HasFlag(FileAttributes.ReadOnly)) return false;
        _fs.File.SetAttributes(filePath, attributes & ~FileAttributes.ReadOnly);
        return true;
    }

    /// <summary>
    /// Puts back a read-only attribute cleared from a source the delete then
    /// refused. What this service says about a file it reports as failed is that
    /// the user still has exactly one of it and exactly as it was, and a source
    /// left writable is one step of that operation committed after the operation
    /// gave up.
    ///
    /// Guarded where <see cref="ClearReadOnly"/> is not, because this runs inside
    /// the failure arm: a throw here would replace the error the caller is about
    /// to file with one about the tidying up after it.
    /// </summary>
    private void RestoreReadOnly(string filePath, bool wasCleared, PerItemFailureLog failureLog)
    {
        if (!wasCleared) return;
        try
        {
            _fs.File.SetAttributes(filePath,
                _fs.File.GetAttributes(filePath) | FileAttributes.ReadOnly);
        }
        catch (Exception ex)
        {
            failureLog.Record(new InvalidOperationException(
                $"The read-only attribute cleared from {filePath} could not be put back after the "
                + "move failed to remove it, so that file is no longer read-only.", ex));
        }
    }

    /// <summary>
    /// Shared so the loop's own failures and the reconciliation's are named the
    /// same way.
    ///
    /// ERROR_SHARING_VIOLATION and ERROR_LOCK_VIOLATION as HRESULTs: another
    /// program holds the file open, which is the one IO failure here with a
    /// cause the user can act on and the one that is not a fault. The Delete
    /// path discriminates the same two codes off the shell's HRESULT
    /// (FileOperationError.RecycleFailed), so both halves of the app name the
    /// same condition the same way.
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
    /// Wraps <c>Directory.CreateDirectory</c> so framework-thrown
    /// UnauthorizedAccessException and IOException are remapped to the
    /// same UnauthorizedAccessException-with-localised-message that
    /// <see cref="ProbeDestinationWriteable"/> produces. The caller's
    /// catch block sees one consistent contract; the framework's
    /// path-bearing message is preserved on InnerException for crash
    /// log consumers but never reaches the displayed UI.
    /// </summary>
    private void CreateDestinationFolder(string folder)
    {
        try
        {
            _fs.Directory.CreateDirectory(folder);
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
        // Fail fast with one clean error rather than collecting per-file
        // errors for every source when the destination is read-only.
        //
        // On a destination whose ACL grants create but not delete, the write
        // lands and the delete is what throws, so the probe file stays behind.
        // Nothing here can clear it: a second delete is the operation that was
        // just refused, and any probe that proves a folder accepts a file has
        // to create one. The move is refused, which is the outcome that
        // matters, and the residue is one empty random-named file on a
        // destination Windows will not let this process tidy.
        var probe = _fs.Path.Combine(folder, _fs.Path.GetRandomFileName());
        try
        {
            _fs.File.WriteAllBytes(probe, Array.Empty<byte>());
            _fs.File.Delete(probe);
        }
        catch (Exception ex)
        {
            // ex.Message stays out of the thrown message (path-leak risk
            // under elevation); the inner exception preserves it for
            // crash-log consumers via .InnerException.
            throw new LocalisedAccessException(
                string.Format(Strings.Error_CannotWriteFolder, folder), ex);
        }
    }

    // File.Move with overwrite=true follows a reparse point planted
    // at destPath during the unique-name race. The non-overwriting
    // form refuses existing targets, ending the race in a per-file
    // error rather than a symlink follow-through to a sensitive
    // location. Overwrite=true would require a reparse-point check on
    // destPath immediately before the move.
    private string GetUniqueDestPath(string folder, string fileName)
    {
        var candidate = _fs.Path.Combine(folder, fileName);
        if (!_fs.File.Exists(candidate)) return candidate;

        var nameWithout = _fs.Path.GetFileNameWithoutExtension(fileName);
        var ext = _fs.Path.GetExtension(fileName);

        for (int i = 1; i <= 10_000; i++)
        {
            candidate = _fs.Path.Combine(folder, $"{nameWithout} ({i}){ext}");
            if (!_fs.File.Exists(candidate)) return candidate;
        }

        throw new DestinationCollisionException(fileName);
    }

    /// <summary>
    /// Thrown by <see cref="GetUniqueDestPath"/> when 10,000 unique-
    /// suffix attempts all collide. The MoveFilesAsync loop catches
    /// it one frame up and folds it into the result as a
    /// <see cref="DestinationCollision"/> entry so the rest of the
    /// batch continues; nothing outside this class observes the
    /// exception type, so the sealed-private scope keeps it from
    /// leaking into the public surface.
    /// </summary>
    private sealed class DestinationCollisionException : Exception
    {
        public string FileName { get; }
        public DestinationCollisionException(string fileName) =>
            FileName = fileName;
    }
}
