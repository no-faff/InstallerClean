using InstallerClean.Models;

namespace InstallerClean.Services;

/// <summary>
/// Sends orphaned MSI / MSP files to the Recycle Bin. The "delete via
/// Recycle Bin, never permanent" rule is part of the project's safety
/// story: a user who realises afterwards that they shouldn't have
/// cleaned a particular file can restore it from the bin.
/// </summary>
/// <remarks>
/// The implementation calls <c>SHFileOperationW</c> with
/// <c>FOF_ALLOWUNDO</c>. We deliberately do not use
/// <c>Microsoft.VisualBasic.FileIO.FileSystem.DeleteFile</c>: it
/// requires an STA thread and would force every caller (including the
/// CLI, which has no UI thread) to marshal. The shell-level call works
/// from any thread and gives identical Recycle Bin semantics.
/// Permanent deletion is intentionally not exposed: there is no Move-
/// style "I really meant it" override for Delete.
/// </remarks>
public interface IDeleteFilesService
{
    /// <summary>
    /// Send every path in <paramref name="filePaths"/> to the Recycle
    /// Bin. Returns a <see cref="DeleteResult"/> with one
    /// <see cref="FileOperationError"/> per file the service could not
    /// delete.
    /// </summary>
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
