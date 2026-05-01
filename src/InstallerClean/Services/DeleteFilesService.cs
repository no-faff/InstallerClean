using InstallerClean.Interop;
using InstallerClean.Models;

namespace InstallerClean.Services;

public sealed class DeleteFilesService : IDeleteFilesService
{
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

                try
                {
                    if (!File.Exists(filePath))
                    {
                        errors.Add(new MissingSourceFile(filePath));
                        continue;
                    }
                    var fileName = Path.GetFileName(filePath);
                    progress?.Report(new OperationProgress(i + 1, total, fileName));

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
                catch (UnauthorizedAccessException ex)
                {
                    errors.Add(new AccessDenied(filePath, ex.Message));
                }
                catch (IOException ex)
                {
                    errors.Add(new IOFailure(filePath, ex.Message));
                }
                catch (Exception ex)
                {
                    errors.Add(new UnknownError(filePath, ex.GetType().Name, ex.Message));
                }
            }

            InstallerCacheHelpers.PruneEmptySubdirectories(cancellationToken);
            return new DeleteResult(deleted, errors.AsReadOnly());
        }, cancellationToken);
    }
}
