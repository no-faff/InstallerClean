using System.IO.Abstractions;
using InstallerClean.Models;

namespace InstallerClean.Services;

public sealed class DeleteFilesService : IDeleteFilesService
{
    private readonly IFileSystem _fs;
    private readonly IMutexProbe _mutex;

    /// <summary>
    /// Test-only real-folder override for the containment guard's cache root
    /// (null in production). See the matching field on <c>MoveFilesService</c>:
    /// it lets the real-filesystem integration tests treat a %TEMP% sandbox as
    /// the cache without touching the real one, and does not let a MockFileSystem
    /// bypass the gate.
    /// </summary>
    private readonly string? _installerFolderOverride;

    /// <summary>
    /// Constructor. The DI container injects the registered
    /// <see cref="IFileSystem"/> and <see cref="IMutexProbe"/> singletons in
    /// production; the mutex is held for the batch so a msiexec starting
    /// mid-delete waits instead of racing the cache.
    /// </summary>
    public DeleteFilesService(IFileSystem fileSystem, IMutexProbe mutex)
        : this(fileSystem, mutex, null) { }

    /// <summary>Test constructor. No mutex hold (the hold is exercised via the seam constructor below).</summary>
    internal DeleteFilesService(IFileSystem fileSystem)
        : this(fileSystem, NullMutexProbe.Instance, null) { }

    /// <summary>Seam constructor: an injected <see cref="IMutexProbe"/> (real or fake) plus the sandbox override.</summary>
    internal DeleteFilesService(IFileSystem fileSystem, IMutexProbe mutex, string? installerFolderOverride)
    {
        _fs = fileSystem;
        _mutex = mutex;
        _installerFolderOverride = installerFolderOverride;
    }

    public Task<DeleteResult> DeleteFilesAsync(
        IEnumerable<string> filePaths,
        IProgress<OperationProgress>? progress = null,
        CancellationToken cancellationToken = default)
    {
        return Task.Run(() =>
        {
            var pathList = filePaths as IReadOnlyList<string> ?? filePaths.ToList();
            var total = pathList.Count;
            if (total == 0)
                return new DeleteResult(0, Array.Empty<FileOperationError>());

            cancellationToken.ThrowIfCancellationRequested();

            // Hold Global\_MSIExecute for the batch on this worker thread so a
            // msiexec starting mid-delete waits on the mutex instead of racing the
            // cache. Acquired here and released in the finally on the SAME thread
            // (Win32 owner-thread rule); the body is synchronous, so no await hops
            // threads between acquire and release. Held by a live transaction =>
            // refuse and touch nothing (the caller re-checks the pending-reboot
            // gate and shows its banner). A DACL-refused acquire proceeds without
            // the hold rather than refusing.
            //
            // What the hold costs, so nobody widens it and nobody removes it:
            // _MSIExecute is the machine-wide Windows Installer serialisation
            // mutex, so for as long as this batch runs, every installer on the
            // machine that wants it waits or fails with 1618. The batch is
            // bounded by file count and each file is one delete, so it cannot
            // stall the way a shell round trip could, but a delete against an
            // unresponsive volume still holds the machine's installer lock until
            // this process is killed. That is accepted because the alternative is
            // msiexec writing the cache in the middle of a delete, which costs a
            // needed file rather than a wait.
            var lease = _mutex.TryAcquire(PendingRebootService.MsiExecuteMutexName, out var heldByAnother);
            if (lease is null && heldByAnother)
                return new DeleteResult(0, Array.Empty<FileOperationError>(), InstallerBusy: true);

            try
            {
            int deleted = 0;
            var errors = new List<FileOperationError>();
            var failureLog = new PerItemFailureLog("Delete",
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
                    "Delete ran without the Windows Installer mutex: it could not be acquired and was not held by another process."));
            // Resolved once for the batch; the guard resolves each SOURCE per
            // file against it (see InstallerCacheRoot).
            var cacheRoot = InstallerCacheRoot.Resolve(_installerFolderOverride);
            bool cancelled = false;

            try
            {
            for (int i = 0; i < total; i++)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var filePath = pathList[i];

                // Report progress before the skip check so a missing file
                // still advances the visible counter, matching MoveFilesService.
                progress?.Report(new OperationProgress(i + 1, total, _fs.Path.GetFileName(filePath)));

                try
                {
                    if (!_fs.File.Exists(filePath))
                    {
                        errors.Add(new MissingSourceFile(filePath));
                        continue;
                    }

                    // Refuse a reparse-point source, matching MoveFilesService:
                    // deleting a symlink removes the link, so following one out
                    // of the cache is refused. Real-FS check (MockFileSystem
                    // cannot bypass). Reparse first, then the containment check,
                    // so a symlink is reported as one. An attribute read that
                    // FAILS refuses the file as UnknownError rather than as a
                    // symlink, which it has not been shown to be.
                    var reparse = Helpers.StorageHelpers.CheckReparsePoint(filePath, out var reparseError);
                    if (reparse == Helpers.StorageHelpers.ReparseCheck.Yes)
                    {
                        errors.Add(new SourceIsReparsePoint(filePath));
                        continue;
                    }
                    if (reparse == Helpers.StorageHelpers.ReparseCheck.Unreadable)
                    {
                        failureLog.Record(reparseError!);
                        errors.Add(new UnknownError(filePath));
                        continue;
                    }

                    // Containment guard at the service boundary: never delete a
                    // file that does not resolve directly into
                    // C:\Windows\Installer, even if a corrupt candidate reached
                    // here. This is the source-side choke point matching the
                    // destination's. A path that could not be resolved at all is
                    // refused the same way and reported without the
                    // out-of-bounds claim; see the matching block in
                    // MoveFilesService.
                    var safety = CandidateGuard.CheckSafeToRemove(filePath, cacheRoot);
                    if (safety == CandidateGuard.RemovalSafety.Refused)
                    {
                        errors.Add(new CandidateOutsideCache(filePath));
                        continue;
                    }
                    if (safety == CandidateGuard.RemovalSafety.Unproven)
                    {
                        failureLog.Record(new InvalidOperationException(
                            $"Delete refused: {filePath} could not be resolved, so it could not be shown to be inside the Installer cache."));
                        errors.Add(new UnknownError(filePath));
                        continue;
                    }

                    _fs.File.Delete(filePath);
                    deleted++;
                }
                // Logged for the same reason as the matching block in
                // MoveFilesService: the framework exception's detail exists
                // nowhere else once the category has been filed.
                catch (UnauthorizedAccessException ex)
                {
                    // File.Delete throws this for a READ-ONLY file as well as
                    // for a permissions refusal, and the two are not the same
                    // problem. The shell delete this replaced cleared the
                    // attribute and carried on, so leaving it here would be a
                    // regression wearing the costume of a permissions error:
                    // the user is told Windows refused access, cannot tell a
                    // read-only bit from an ACL, and can act on neither.
                    //
                    // Safe HERE and nowhere else, which is why it is inline
                    // rather than a helper anything could call. By this line the
                    // file has passed the reparse refusal and the containment
                    // guard, both reading the real filesystem, so it is a real
                    // file inside C:\Windows\Installer; and the user has
                    // confirmed the deletion. Only the read-only attribute is
                    // cleared, only once, and any throw from clearing it or from
                    // the retry fails closed exactly as before.
                    if (TryClearReadOnly(filePath, failureLog))
                    {
                        try
                        {
                            _fs.File.Delete(filePath);
                            deleted++;
                            continue;
                        }
                        catch (Exception retry)
                        {
                            // Both are recorded: without the first the crash log
                            // would not show a retry was attempted, and without
                            // the second it would not show what beat it.
                            failureLog.Record(ex);
                            failureLog.Record(retry);
                            RestoreReadOnly(filePath, failureLog);
                            errors.Add(new AccessDenied(filePath));
                            continue;
                        }
                    }
                    failureLog.Record(ex);
                    errors.Add(new AccessDenied(filePath));
                }
                catch (IOException ex)
                {
                    failureLog.Record(ex);
                    // ERROR_SHARING_VIOLATION and ERROR_LOCK_VIOLATION as
                    // HRESULTs: another program holds the file open, which is
                    // the one IO failure here with a cause the user can act on
                    // and the one that is not a fault. Discriminated exactly as
                    // MoveFilesService does, off the same two codes, so both
                    // halves of the app name the same condition the same way.
                    errors.Add(ex.HResult is unchecked((int)0x80070020) or unchecked((int)0x80070021)
                        ? new FileInUse(filePath)
                        : new IOFailure(filePath));
                }
                catch (Exception ex)
                {
                    failureLog.Record(ex);
                    errors.Add(new UnknownError(filePath));
                }
            }
            }
            catch (OperationCanceledException)
            {
                // Return what was deleted before the cancel rather than
                // throwing the tally away. The loop's ThrowIfCancellationRequested
                // is the only cancellation source and sits outside the inner
                // per-file catch, so only a real cancel lands here.
                cancelled = true;
            }

            // Outside the cancel catch so a batch the user stopped still
            // accounts for what its failures cost the log.
            failureLog.WriteClosingEntry();

            // CancellationToken.None: best-effort cleanup. See the
            // matching comment in MoveFilesService for the rationale.
            InstallerCacheHelpers.PruneEmptySubdirectories(_fs, CancellationToken.None);
            return new DeleteResult(deleted, errors.AsReadOnly(), Cancelled: cancelled);
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
    /// Clears the read-only attribute so the delete can be retried, and reports
    /// whether it is worth retrying. False for a file that was not read-only in
    /// the first place, which is the ordinary permissions refusal and has
    /// nothing here to fix, and false if the attributes cannot be read or
    /// written, which is a second refusal and is recorded rather than chased.
    /// </summary>
    private bool TryClearReadOnly(string filePath, PerItemFailureLog failureLog)
    {
        try
        {
            var attributes = _fs.File.GetAttributes(filePath);
            if (!attributes.HasFlag(FileAttributes.ReadOnly)) return false;
            _fs.File.SetAttributes(filePath, attributes & ~FileAttributes.ReadOnly);
            return true;
        }
        catch (Exception ex)
        {
            failureLog.Record(ex);
            return false;
        }
    }

    /// <summary>
    /// Puts back the read-only attribute cleared for a retry that then failed.
    /// The file is the user's, it is still in C:\Windows\Installer, and the run
    /// has just told them it could not be deleted, which has to be true of the
    /// file's attributes as well as of the file.
    ///
    /// Reached only from inside the branch where
    /// <see cref="TryClearReadOnly"/> returned true, so there is always an
    /// attribute to put back. Guarded, unlike the clear: a throw in the tidying
    /// up must not replace the two errors already recorded for this file.
    /// </summary>
    private void RestoreReadOnly(string filePath, PerItemFailureLog failureLog)
    {
        try
        {
            _fs.File.SetAttributes(filePath,
                _fs.File.GetAttributes(filePath) | FileAttributes.ReadOnly);
        }
        catch (Exception ex)
        {
            failureLog.Record(new InvalidOperationException(
                $"The read-only attribute cleared from {filePath} could not be put back after the "
                + "retried delete failed.", ex));
        }
    }
}
