using InstallerClean.Models;

namespace InstallerClean.Services;

/// <summary>
/// Moves orphaned MSI / MSP files out of <c>C:\Windows\Installer</c>
/// to a user-chosen destination so they can be restored if anything
/// breaks. The "move don't delete" path is the project's primary
/// safety story: nothing leaves the user's machine, and recovery is a
/// drag-and-drop.
/// </summary>
/// <remarks>
/// SECURITY: the service refuses any destination that resolves
/// (after symlink and junction expansion) to <c>C:\Windows\Installer</c>
/// or any of its descendants. A destination that bypassed this check
/// would let a Move silently put files back into the cache and break
/// the restore-after-mistakes contract. The check uses
/// <c>InstallerCacheHelpers.IsInstallerFolderOrChild</c> against the
/// real filesystem regardless of any injected
/// <c>System.IO.Abstractions.IFileSystem</c>; tests cannot bypass it
/// by passing a MockFileSystem.
/// </remarks>
public interface IMoveFilesService
{
    /// <summary>
    /// Move every path in <paramref name="filePaths"/> into
    /// <paramref name="destinationFolder"/>, creating it if missing.
    /// Returns a <see cref="MoveResult"/> with one
    /// <see cref="FileOperationError"/> per file the service could
    /// not move. Throws <see cref="InvalidOperationException"/> if
    /// the destination resolves inside the Installer folder, or
    /// <see cref="UnauthorizedAccessException"/> if the destination
    /// folder is not writable.
    /// </summary>
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
