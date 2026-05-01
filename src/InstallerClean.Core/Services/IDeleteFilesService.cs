using InstallerClean.Models;

namespace InstallerClean.Services;

public interface IDeleteFilesService
{
    Task<DeleteResult> DeleteFilesAsync(
        IEnumerable<string> filePaths,
        IProgress<OperationProgress>? progress = null,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Outcome of a Delete operation. <see cref="DeletedCount"/> counts
/// files successfully sent to the Recycle Bin; <see cref="Errors"/>
/// contains one categorised <see cref="FileOperationError"/> per file
/// the service could not delete. The two together always sum to the
/// input count.
/// </summary>
public record DeleteResult(
    int DeletedCount,
    IReadOnlyList<FileOperationError> Errors);
