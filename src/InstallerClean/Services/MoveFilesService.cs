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
        // SECURITY: the entire restore-after-mistakes story collapses
        // if files move back inside C:\Windows\Installer. The service
        // refuses directly rather than trusting upstream callers to
        // have checked, and ResolveFinalPath inside
        // IsInstallerFolderOrChild expands junctions so the destination
        // can't sneak through behind a reparse point. Note that the
        // junction resolution lives in InstallerCacheHelpers and uses
        // the real filesystem regardless of the injected IFileSystem;
        // production callers always pass a real path so this stays
        // correct.
        if (InstallerCacheHelpers.IsInstallerFolderOrChild(destinationFolder))
            throw new InvalidOperationException(
                string.Format(Strings.Error_MoveIntoInstaller, destinationFolder));

        return Task.Run(() =>
        {
            _fs.Directory.CreateDirectory(destinationFolder);
            ProbeDestinationWriteable(destinationFolder);

            int moved = 0;
            var errors = new List<FileOperationError>();
            var pathList = filePaths as IReadOnlyList<string> ?? filePaths.ToList();
            var total = pathList.Count;

            for (int i = 0; i < total; i++)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var sourcePath = pathList[i];

                try
                {
                    if (!_fs.File.Exists(sourcePath))
                    {
                        errors.Add(new MissingSourceFile(sourcePath));
                        continue;
                    }

                    var fileName = _fs.Path.GetFileName(sourcePath);
                    progress?.Report(new OperationProgress(i + 1, total, fileName));

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

            InstallerCacheHelpers.PruneEmptySubdirectories(cancellationToken);
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
            throw new UnauthorizedAccessException(
                string.Format(Strings.Error_CannotWriteFolder, folder, ex.Message), ex);
        }
    }

    // SECURITY: do not switch to File.Move(src, dst, overwrite: true)
    // without also defending against a reparse-point planted at
    // destPath during the unique-name race. The current File.Move
    // refuses existing targets, so the race ends in a per-file error
    // rather than a symlink follow-through to a sensitive location.
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
    /// suffix attempts all collide. Caught one frame up and converted
    /// to a <see cref="DestinationCollision"/> entry in the result so
    /// the rest of the batch keeps moving. Private because no other
    /// code path needs to see it.
    /// </summary>
    private sealed class DestinationCollisionException : Exception
    {
        public string FileName { get; }
        public DestinationCollisionException(string fileName) =>
            FileName = fileName;
    }
}
