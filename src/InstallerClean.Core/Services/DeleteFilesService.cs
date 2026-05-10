using System.IO.Abstractions;
using InstallerClean.Interop;
using InstallerClean.Models;

namespace InstallerClean.Services;

public sealed class DeleteFilesService : IDeleteFilesService
{
    private readonly IFileSystem _fs;

    /// <summary>
    /// Constructor. The DI container injects the registered
    /// <see cref="IFileSystem"/> singleton in production; tests pass
    /// a <see cref="MockFileSystem"/> so the File.Exists pre-check
    /// and per-file error categorisation can be verified without
    /// touching <c>%TEMP%</c>. The recycle-bin send itself still
    /// goes through SHFileOperationW, which is exercised only in
    /// the integration tests.
    /// </summary>
    public DeleteFilesService(IFileSystem fileSystem)
    {
        _fs = fileSystem;
    }

    public Task<DeleteResult> DeleteFilesAsync(
        IEnumerable<string> filePaths,
        IProgress<OperationProgress>? progress = null,
        CancellationToken cancellationToken = default)
    {
        return Task.Run(() =>
        {
            int deleted = 0;
            var errors = new List<FileOperationError>();
            var pathList = filePaths as IReadOnlyList<string> ?? filePaths.ToList();
            var total = pathList.Count;

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

                    // Native SHFileOperationW avoids VB's FileSystem.DeleteFile
                    // which can try to show error dialogs from a non-STA thread.
                    var result = ShellFileOperations.SendToRecycleBin(filePath);
                    if (result != 0)
                    {
                        errors.Add(new ShellRefused(filePath, result));
                        continue;
                    }
                    deleted++;
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
