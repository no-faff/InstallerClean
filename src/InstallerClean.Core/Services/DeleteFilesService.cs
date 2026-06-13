using System.IO.Abstractions;
using InstallerClean.Models;

namespace InstallerClean.Services;

public sealed class DeleteFilesService : IDeleteFilesService
{
    private readonly IFileSystem _fs;
    private readonly IRecycleEngine _engine;

    /// <summary>
    /// Constructor. The DI container injects the registered
    /// <see cref="IFileSystem"/> and <see cref="IRecycleEngine"/>
    /// singletons in production; tests pass a <see cref="MockFileSystem"/>
    /// and a fake engine so the per-file outcome mapping, the
    /// probe-and-refuse decision and the cancellation path are verified
    /// without touching the real Recycle Bin.
    /// </summary>
    public DeleteFilesService(IFileSystem fileSystem, IRecycleEngine engine)
    {
        _fs = fileSystem;
        _engine = engine;
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
            if (!permitPermanentDelete && !_engine.CanRecycleToVolume(pathList[0]))
                return new DeleteResult(0, Array.Empty<FileOperationError>(), RecycleUnavailable: true);

            int deleted = 0;
            var errors = new List<FileOperationError>();

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
                catch (UnauthorizedAccessException)
                {
                    errors.Add(new AccessDenied(filePath));
                }
                catch (IOException)
                {
                    errors.Add(new IOFailure(filePath));
                }
                catch (Exception)
                {
                    errors.Add(new UnknownError(filePath));
                }
            }

            // CancellationToken.None: best-effort cleanup. See the
            // matching comment in MoveFilesService for the rationale.
            InstallerCacheHelpers.PruneEmptySubdirectories(CancellationToken.None);
            return new DeleteResult(deleted, errors.AsReadOnly());
        }, cancellationToken);
    }
}
