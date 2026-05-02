using InstallerClean.Models;

namespace InstallerClean.Services;

/// <summary>
/// Sends orphaned MSI / MSP files to the Recycle Bin via
/// <c>SHFileOperationW</c> with <c>FOF_ALLOWUNDO</c>. Permanent
/// deletion is intentionally not exposed: every Delete is undoable
/// from the bin. SHFileOperationW is used (not VB's
/// FileSystem.DeleteFile) because it works from any thread and the
/// CLI has no UI thread to marshal onto.
/// </summary>
public interface IDeleteFilesService
{
    /// <summary>
    /// Send every path in <paramref name="filePaths"/> to the Recycle
    /// Bin. Per-file failures are recorded in
    /// <see cref="DeleteResult.Errors"/>, not thrown.
    /// </summary>
    Task<DeleteResult> DeleteFilesAsync(
        IEnumerable<string> filePaths,
        IProgress<OperationProgress>? progress = null,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Outcome of a Delete. <see cref="DeletedCount"/> + <see cref="Errors"/>.Count
/// always sum to the input count.
/// </summary>
public record DeleteResult(
    int DeletedCount,
    IReadOnlyList<FileOperationError> Errors);
