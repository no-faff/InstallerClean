using InstallerClean.Models;

namespace InstallerClean.Services;

/// <summary>
/// Moves orphaned MSI / MSP files out of <c>C:\Windows\Installer</c>
/// to a user-chosen destination. Refuses any destination that resolves
/// (after symlink expansion) to <c>C:\Windows\Installer</c> or a
/// descendant: that would defeat the restore-after-mistakes contract.
/// The reparse-point check uses the real filesystem regardless of any
/// injected <c>IFileSystem</c>.
/// </summary>
public interface IMoveFilesService
{
    /// <summary>
    /// Move every path in <paramref name="filePaths"/> into
    /// <paramref name="destinationFolder"/> (created if missing).
    /// Throws <see cref="InvalidOperationException"/> if the destination
    /// resolves inside the Installer folder, or
    /// <see cref="UnauthorizedAccessException"/> if the destination is
    /// not writable. Per-file failures are surfaced via the result's
    /// <see cref="MoveResult.Errors"/>, not exceptions.
    /// </summary>
    Task<MoveResult> MoveFilesAsync(
        IEnumerable<string> filePaths,
        string destinationFolder,
        IProgress<OperationProgress>? progress = null,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Outcome of a Move. <see cref="MovedCount"/> + <see cref="Errors"/>.Count
/// always sum to the input count: every file is either moved or
/// recorded as a failure (never silently dropped).
/// </summary>
public record MoveResult(
    int MovedCount,
    IReadOnlyList<FileOperationError> Errors);
