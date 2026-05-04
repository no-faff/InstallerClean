using System.IO.Abstractions;
using InstallerClean.Models;
using InstallerClean.Resources;

namespace InstallerClean.Services;

public sealed class MoveFilesService : IMoveFilesService
{
    private readonly IFileSystem _fs;

    /// <summary>
    /// Constructor. The DI container injects the registered
    /// <see cref="IFileSystem"/> singleton in production; tests pass
    /// a <see cref="MockFileSystem"/> so the move pipeline can be
    /// verified without touching <c>%TEMP%</c>.
    /// </summary>
    public MoveFilesService(IFileSystem fileSystem)
    {
        _fs = fileSystem;
    }

    public Task<MoveResult> MoveFilesAsync(
        IEnumerable<string> filePaths,
        string destinationFolder,
        IProgress<OperationProgress>? progress = null,
        CancellationToken cancellationToken = default)
    {
        // Destination must not resolve inside C:\Windows\Installer;
        // ResolveFinalPath expands junctions so a reparse-point
        // destination cannot smuggle the batch into the cache folder.
        if (InstallerCacheHelpers.IsInstallerFolderOrChild(destinationFolder))
            throw new InvalidOperationException(
                string.Format(Strings.Error_MoveIntoInstaller, destinationFolder));

        return Task.Run(() =>
        {
            _fs.Directory.CreateDirectory(destinationFolder);

            // Re-check after CreateDirectory closes the TOCTOU window
            // where a junction could be swapped into the leaf.
            if (InstallerCacheHelpers.IsInstallerFolderOrChild(destinationFolder))
                throw new InvalidOperationException(
                    string.Format(Strings.Error_MoveIntoInstaller, destinationFolder));

            ProbeDestinationWriteable(destinationFolder);

            int moved = 0;
            var errors = new List<FileOperationError>();
            var pathList = filePaths as IReadOnlyList<string> ?? filePaths.ToList();
            var total = pathList.Count;

            for (int i = 0; i < total; i++)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var sourcePath = pathList[i];

                // Report progress before the skip checks so the
                // visible counter advances on missing / reparse-point
                // entries instead of jumping over them.
                progress?.Report(new OperationProgress(i + 1, total, _fs.Path.GetFileName(sourcePath)));

                try
                {
                    if (!_fs.File.Exists(sourcePath))
                    {
                        errors.Add(new MissingSourceFile(sourcePath));
                        continue;
                    }

                    // Refuse a source that's a symlink or junction:
                    // moving the symlink would pull an OS file out of
                    // System32. Real-FS check (MockFileSystem cannot
                    // bypass).
                    if (Helpers.StorageHelpers.IsReparsePoint(sourcePath))
                    {
                        errors.Add(new SourceIsReparsePoint(sourcePath));
                        continue;
                    }

                    var fileName = _fs.Path.GetFileName(sourcePath);
                    var destPath = GetUniqueDestPath(destinationFolder, fileName);
                    _fs.File.Move(sourcePath, destPath);
                    moved++;
                }
                catch (DestinationCollisionException ex)
                {
                    errors.Add(new DestinationCollision(sourcePath, ex.FileName));
                }
                catch (UnauthorizedAccessException ex)
                {
                    errors.Add(new AccessDenied(sourcePath, ex.Message));
                }
                catch (IOException ex)
                {
                    errors.Add(new IOFailure(sourcePath, ex.Message));
                }
                catch (Exception ex)
                {
                    errors.Add(new UnknownError(sourcePath, ex.GetType().Name, ex.Message));
                }
            }

            // Pass CancellationToken.None: the prune is best-effort
            // post-operation cleanup. If the user pressed Cancel during
            // the prune (after all moves completed), propagating their
            // token would throw OperationCanceledException out of a
            // batch that actually succeeded - the caller would re-label
            // the run as "Move cancelled" even though every file moved.
            InstallerCacheHelpers.PruneEmptySubdirectories(CancellationToken.None);
            return new MoveResult(moved, errors.AsReadOnly());
        }, cancellationToken);
    }

    private void ProbeDestinationWriteable(string folder)
    {
        // Fail fast with one clean error rather than collecting per-file
        // errors for every source when the destination is read-only.
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
            throw new UnauthorizedAccessException(
                string.Format(Strings.Error_CannotWriteFolder, folder), ex);
        }
    }

    // SECURITY: File.Move with overwrite=true would follow a reparse
    // point planted at destPath during the unique-name race. The
    // non-overwriting form refuses existing targets, ending the race
    // in a per-file error rather than a symlink follow-through to a
    // sensitive location. Switching to overwrite=true requires a
    // reparse-point check on destPath immediately before the move.
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
