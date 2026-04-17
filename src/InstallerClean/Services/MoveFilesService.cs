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
        // Defence in depth: the UI (MainViewModel.MoveAllAsync) and the CLI
        // (App.RunCliAsync) already check this, but the service must not
        // trust callers. If we somehow end up here with a destination that
        // resolves into C:\Windows\Installer, the whole safety model
        // collapses.
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
        // Write one random-named probe file and delete it. This fails fast
        // with a single clean error when the destination is read-only or
        // lacks write permission, instead of collecting per-file errors
        // for every one of potentially thousands of source files.
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

    // GetUniqueDestPath uses File.Exists + counter to find a free name.
    // If another process creates the chosen name between this check and
    // File.Move (the destination is a user folder so it's theoretically
    // possible), File.Move throws IOException because it refuses to
    // overwrite existing targets. The caller's catch reports a per-file
    // error; no data is lost and no symlink follow-through can occur.
    // DO NOT switch to File.Move(src, dst, overwrite: true) here without
    // also defending against a reparse-point planted at destPath during
    // that race.
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
