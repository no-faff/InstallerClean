using InstallerClean.Models;

namespace InstallerClean.Services;

/// <summary>
/// Sends orphaned MSI / MSP files to the Recycle Bin through the shell
/// IFileOperation API (via <see cref="IRecycleEngine"/> on a dedicated
/// STA thread). The shell recycle is recycle-or-permanently-delete, not
/// recycle-or-fail: when the Recycle Bin is unavailable a file is
/// deleted permanently while the HRESULTs still report success. So the
/// service probes the files' volume once up front and, unless the
/// caller consents to permanent deletion, refuses the batch rather than
/// silently nuking files. It fails closed rather than falling back to a
/// permanent delete when the bin is unavailable.
/// </summary>
public interface IDeleteFilesService
{
    /// <summary>
    /// Send every path in <paramref name="filePaths"/> to the Recycle
    /// Bin. Per-file failures are recorded in
    /// <see cref="DeleteResult.Errors"/>, not thrown; cancellation
    /// throws <see cref="OperationCanceledException"/>.
    /// </summary>
    /// <param name="permitPermanentDelete">
    /// When <c>false</c> (the default) the service probes the files'
    /// volume before the loop; if the Recycle Bin is unavailable it
    /// deletes nothing and returns
    /// <see cref="DeleteResult.RecycleUnavailable"/> = <c>true</c> so
    /// the caller can offer Move, consent, or cancel. When <c>true</c>
    /// the caller has consented to permanent deletion, so files that
    /// cannot be recycled are deleted permanently and counted as
    /// deleted.
    /// </param>
    Task<DeleteResult> DeleteFilesAsync(
        IEnumerable<string> filePaths,
        bool permitPermanentDelete = false,
        IProgress<OperationProgress>? progress = null,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Outcome of a Delete. When <see cref="RecycleUnavailable"/> is
/// <c>false</c>, <see cref="DeletedCount"/> + <see cref="Errors"/>.Count
/// sum to the input count: every file was recycled, permanently deleted
/// with consent, or recorded as an error. When
/// <see cref="RecycleUnavailable"/> is <c>true</c> the batch was refused
/// before the loop (the bin was unavailable and the caller did not
/// permit permanent deletion): nothing was touched, so
/// <see cref="DeletedCount"/> is 0 and <see cref="Errors"/> is empty.
/// </summary>
public record DeleteResult(
    int DeletedCount,
    IReadOnlyList<FileOperationError> Errors,
    bool RecycleUnavailable = false);
