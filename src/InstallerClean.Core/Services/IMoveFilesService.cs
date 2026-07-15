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
    /// <see cref="MoveResult.Errors"/>, not exceptions. A cancellation
    /// requested mid-operation is NOT thrown: the batch stops and the
    /// accumulated result is returned with <see cref="MoveResult.Cancelled"/>
    /// set (a cancellation before the worker starts still surfaces as
    /// <see cref="OperationCanceledException"/>).
    /// </summary>
    Task<MoveResult> MoveFilesAsync(
        IEnumerable<string> filePaths,
        string destinationFolder,
        IProgress<OperationProgress>? progress = null,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Outcome of a Move. When <see cref="Cancelled"/> and
/// <see cref="InstallerBusy"/> are both <c>false</c>, <see cref="MovedCount"/> +
/// <see cref="Errors"/>.Count sum to the input count: every file is either moved
/// or recorded as a failure (never silently dropped). When <see cref="Cancelled"/>
/// is <c>true</c> the batch was stopped mid-way, so the two sum to the number of
/// files reached before the cancel and the rest of the input was never touched.
/// When <see cref="InstallerBusy"/> is <c>true</c> the batch was refused before it
/// started because a Windows Installer transaction held <c>Global\_MSIExecute</c>:
/// nothing was touched, so <see cref="MovedCount"/> is 0 and <see cref="Errors"/>
/// is empty. The caller re-checks the pending-reboot gate and shows its banner.
/// </summary>
public record MoveResult(
    int MovedCount,
    IReadOnlyList<FileOperationError> Errors,
    bool Cancelled = false,
    bool InstallerBusy = false);
