using InstallerClean.Models;

namespace InstallerClean.Services;

public interface IMoveFilesService
{
    Task<MoveResult> MoveFilesAsync(
        IEnumerable<string> filePaths,
        string destinationFolder,
        IProgress<OperationProgress>? progress = null,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Outcome of a Move operation. <see cref="MovedCount"/> counts files
/// successfully relocated; <see cref="Errors"/> contains one
/// categorised <see cref="FileOperationError"/> per file the service
/// could not move. The two together always sum to the input count
/// (the service either moves a file, records an error, or stops via
/// cancellation - never silently drops one).
/// </summary>
public record MoveResult(
    int MovedCount,
    IReadOnlyList<FileOperationError> Errors);
