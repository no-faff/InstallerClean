using System.IO.Abstractions;
using InstallerClean.Models;

namespace InstallerClean.Services;

public sealed class DeleteFilesService : IDeleteFilesService
{
    private readonly IFileSystem _fs;
    private readonly IRecycleEngine _engine;
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
    /// <see cref="IFileSystem"/>, <see cref="IRecycleEngine"/> and
    /// <see cref="IMutexProbe"/> singletons in production; the mutex is held for
    /// the batch so a msiexec starting mid-delete waits instead of racing the
    /// cache.
    /// </summary>
    public DeleteFilesService(IFileSystem fileSystem, IRecycleEngine engine, IMutexProbe mutex)
        : this(fileSystem, engine, mutex, null) { }

    /// <summary>Test constructor. No mutex hold (the hold is exercised via the seam constructor below).</summary>
    internal DeleteFilesService(IFileSystem fileSystem, IRecycleEngine engine)
        : this(fileSystem, engine, NullMutexProbe.Instance, null) { }

    /// <summary>Test constructor. Points the source containment guard at a real sandbox folder; no mutex hold.</summary>
    internal DeleteFilesService(IFileSystem fileSystem, IRecycleEngine engine, string? installerFolderOverride)
        : this(fileSystem, engine, NullMutexProbe.Instance, installerFolderOverride) { }

    /// <summary>Seam constructor: an injected <see cref="IMutexProbe"/> (real or fake) plus the sandbox override.</summary>
    internal DeleteFilesService(IFileSystem fileSystem, IRecycleEngine engine, IMutexProbe mutex, string? installerFolderOverride)
    {
        _fs = fileSystem;
        _engine = engine;
        _mutex = mutex;
        _installerFolderOverride = installerFolderOverride;
    }

    public bool CanRecycleToVolume(string path) => _engine.CanRecycleToVolume(path);

    public Task<DeleteResult> DeleteFilesAsync(
        IEnumerable<string> filePaths,
        bool permitPermanentDelete = false,
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

            // The shell recycle is recycle-or-permanently-delete: when the
            // bin is unavailable a file is nuked while every HRESULT still
            // reports success. So unless the caller has already consented
            // to permanent deletion, probe the files' volume once and
            // refuse the whole batch rather than silently deleting. Recycle
            // behaviour is per-volume, so the probe rides on the volume the
            // files actually sit on (orphans are all under the same one).
            //
            // Ahead of the mutex acquire below on purpose. The probe mutates
            // nothing and can refuse the whole batch, so running it inside the
            // hold would extend a machine-wide Windows Installer lock across
            // work that may end in touching no file at all. It also decides
            // which refusal a caller sees when both apply, and the bin winning
            // is the accepted outcome: neither has touched a file, and a caller
            // that answers the bin question meets the installer one on the very
            // next call.
            if (!permitPermanentDelete && !_engine.CanRecycleToVolume(pathList[0]))
                return new DeleteResult(0, Array.Empty<FileOperationError>(), RecycleUnavailable: true);

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
            // bounded by file count and each file is one shell call, but a single
            // recycle that hangs (an unresponsive shell, a stalled network
            // volume) holds the machine's installer lock until this process is
            // killed. That is accepted because the alternative is msiexec writing
            // the cache in the middle of a delete, which costs a needed file
            // rather than a wait. Non-mutating pre-work stays outside the hold,
            // which is why the volume probe above runs ahead of it.
            var lease = _mutex.TryAcquire(PendingRebootService.MsiExecuteMutexName, out var heldByAnother);
            if (lease is null && heldByAnother)
                return new DeleteResult(0, Array.Empty<FileOperationError>(), InstallerBusy: true);

            try
            {
            int deleted = 0;
            var errors = new List<FileOperationError>();
            var failureLog = new PerFileFailureLog("Delete");
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
                    // a shell recycle of a symlink removes the link, so following
                    // one out of the cache is refused. Real-FS check
                    // (MockFileSystem cannot bypass). Reparse first, then the
                    // containment check, so a symlink is reported as one. An
                    // attribute read that FAILS refuses the file as UnknownError
                    // rather than as a symlink, which it has not been shown to be.
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

                    // Containment guard at the service boundary: never recycle a
                    // file that does not resolve inside C:\Windows\Installer, even
                    // if a corrupt candidate reached here. This is the source-side
                    // choke point matching the destination's. A path that
                    // could not be resolved at all is refused the same way and
                    // reported without the out-of-bounds claim; see the matching
                    // block in MoveFilesService.
                    var safety = CandidateGuard.CheckSafeToRemove(filePath, _installerFolderOverride);
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

                    var outcome = _engine.RecycleFile(filePath);
                    switch (outcome.Outcome)
                    {
                        case RecycleOutcome.Recycled:
                            deleted++;
                            break;
                        // With consent a nuke counts as deleted; without it
                        // the file is gone and that is recorded honestly so
                        // the user is never told it reached the bin.
                        case RecycleOutcome.PermanentlyDeleted when permitPermanentDelete:
                            deleted++;
                            break;
                        case RecycleOutcome.PermanentlyDeleted:
                            errors.Add(new PermanentlyDeleted(filePath, outcome.HResult));
                            break;
                        // Failed, and any future outcome, recorded with its
                        // HRESULT for telemetry; the file was left in place.
                        default:
                            errors.Add(new RecycleFailed(filePath, outcome.HResult));
                            break;
                    }
                }
                // Logged for the same reason as the matching block in
                // MoveFilesService: the framework exception's detail exists
                // nowhere else once the category has been filed. A recycle
                // failure the shell reports through an HRESULT is not an
                // exception and does not come through here; it carries its code
                // on the RecycleFailed entry instead.
                catch (UnauthorizedAccessException ex)
                {
                    failureLog.Record(ex);
                    errors.Add(new AccessDenied(filePath));
                }
                catch (IOException ex)
                {
                    failureLog.Record(ex);
                    errors.Add(new IOFailure(filePath));
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
                // Return what was recycled before the cancel rather than
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
}
