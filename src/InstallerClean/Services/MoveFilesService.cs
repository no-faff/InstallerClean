using InstallerClean.Models;

namespace InstallerClean.Services;

public sealed class MoveFilesService : IMoveFilesService
{
    public Task<MoveResult> MoveFilesAsync(
        IEnumerable<string> filePaths,
        string destinationFolder,
        IProgress<OperationProgress>? progress = null,
        CancellationToken cancellationToken = default)
    {
        // Defence in depth: the whole safety model collapses if files move
        // back inside C:\Windows\Installer, so the service refuses directly
        // rather than trusting upstream callers to have checked.
        if (InstallerCacheHelpers.IsInstallerFolderOrChild(destinationFolder))
            throw new InvalidOperationException(
                $"Refusing to move files into the Windows Installer folder (destination: {destinationFolder}).");

        return Task.Run(() =>
        {
            Directory.CreateDirectory(destinationFolder);
            ProbeDestinationWriteable(destinationFolder);

            int moved = 0;
            var errors = new List<MoveError>();
            var pathList = filePaths as IReadOnlyList<string> ?? filePaths.ToList();
            var total = pathList.Count;

            for (int i = 0; i < total; i++)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var sourcePath = pathList[i];

                try
                {
                    if (!File.Exists(sourcePath))
                    {
                        errors.Add(new MoveError(sourcePath, "File no longer exists."));
                        continue;
                    }

                    var fileName = Path.GetFileName(sourcePath);
                    progress?.Report(new OperationProgress(i + 1, total, fileName));

                    var destPath = GetUniqueDestPath(destinationFolder, fileName);
                    File.Move(sourcePath, destPath);
                    moved++;
                }
                catch (Exception ex)
                {
                    errors.Add(new MoveError(sourcePath, ex.Message));
                }
            }

            InstallerCacheHelpers.PruneEmptySubdirectories(cancellationToken);
            return new MoveResult(moved, errors.AsReadOnly());
        }, cancellationToken);
    }

    private static void ProbeDestinationWriteable(string folder)
    {
        // Fail fast with one clean error rather than collecting per-file
        // errors for every source when the destination is read-only.
        var probe = Path.Combine(folder, Path.GetRandomFileName());
        try
        {
            File.WriteAllBytes(probe, Array.Empty<byte>());
            File.Delete(probe);
        }
        catch (Exception ex)
        {
            throw new UnauthorizedAccessException(
                $"Cannot write to {folder}: {ex.Message}", ex);
        }
    }

    // DO NOT switch to File.Move(src, dst, overwrite: true) without also
    // defending against a reparse-point planted at destPath during the
    // unique-name race. Current File.Move refuses existing targets so the
    // race ends in a per-file error, not a symlink follow-through.
    private static string GetUniqueDestPath(string folder, string fileName)
    {
        var candidate = Path.Combine(folder, fileName);
        if (!File.Exists(candidate)) return candidate;

        var nameWithout = Path.GetFileNameWithoutExtension(fileName);
        var ext = Path.GetExtension(fileName);

        for (int i = 1; i <= 10_000; i++)
        {
            candidate = Path.Combine(folder, $"{nameWithout} ({i}){ext}");
            if (!File.Exists(candidate)) return candidate;
        }

        throw new InvalidOperationException(
            $"Could not find a unique filename for '{fileName}' after 10,000 attempts.");
    }
}
