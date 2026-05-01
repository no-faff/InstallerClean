using System.IO.Abstractions;
using InstallerClean.Interop;
using InstallerClean.Models;

namespace InstallerClean.Services;

public sealed class DeleteFilesService : IDeleteFilesService
{
    private readonly IFileSystem _fs;

    /// <summary>Production constructor: real on-disk filesystem.</summary>
    public DeleteFilesService() : this(new FileSystem()) { }

    /// <summary>
    /// Test constructor: inject a <see cref="MockFileSystem"/> (or any
    /// other <see cref="IFileSystem"/>) so unit tests can verify the
    /// File.Exists pre-check and per-file error categorisation
    /// without touching <c>%TEMP%</c>. Note that the actual recycle-
    /// bin send still goes through SHFileOperationW; that call cannot
    /// be virtualised and is exercised only in the integration tests.
    /// </summary>
    internal DeleteFilesService(IFileSystem fileSystem)
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

                try
                {
                    if (!_fs.File.Exists(filePath))
                    {
                        errors.Add(new MissingSourceFile(filePath));
                        continue;
                    }
                    var fileName = _fs.Path.GetFileName(filePath);
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
