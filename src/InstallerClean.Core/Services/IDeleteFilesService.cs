using InstallerClean.Models;

namespace InstallerClean.Services;

/// <summary>
/// Deletes orphaned MSI / MSP files permanently, through the injected
/// <see cref="System.IO.Abstractions.IFileSystem"/> like every other
/// file-touching service, so the loop is testable against a MockFileSystem.
/// The two safety checks it applies per file deliberately do not go through
/// that abstraction: the reparse-point refusal and the containment guard read
/// the real filesystem whatever is injected, so a mock cannot talk its way
/// past either.
/// </summary>
public interface IDeleteFilesService
{
    /// <summary>
    /// Delete every path in <paramref name="filePaths"/>. Per-file failures are
    /// recorded in <see cref="DeleteResult.Errors"/>, not thrown. A cancellation
    /// requested mid-operation is NOT thrown: the batch stops and the
    /// accumulated result is returned with <see cref="DeleteResult.Cancelled"/>
    /// set (a cancellation before the worker starts still surfaces as
    /// <see cref="OperationCanceledException"/>).
    /// </summary>
    Task<DeleteResult> DeleteFilesAsync(
        IEnumerable<string> filePaths,
        IProgress<OperationProgress>? progress = null,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Outcome of a Delete. When <see cref="Cancelled"/> and
/// <see cref="InstallerBusy"/> are both <c>false</c>,
/// <see cref="DeletedCount"/> + <see cref="Errors"/>.Count sum to the input
/// count: every file was deleted or recorded as an error. When
/// <see cref="Cancelled"/> is <c>true</c> the batch was stopped mid-way, so the
/// count and errors reflect the files reached before the cancel and the rest of
/// the input was never touched. When <see cref="InstallerBusy"/> is <c>true</c>
/// the batch was refused before it started because a Windows Installer
/// transaction held <c>Global\_MSIExecute</c>: nothing was touched. The caller
/// re-checks the pending-reboot gate and shows its banner.
/// </summary>
public record DeleteResult(
    int DeletedCount,
    IReadOnlyList<FileOperationError> Errors,
    bool Cancelled = false,
    bool InstallerBusy = false);
