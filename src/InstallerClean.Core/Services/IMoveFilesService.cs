using InstallerClean.Models;

namespace InstallerClean.Services;

/// <summary>
/// Moves orphaned MSI / MSP files out of <c>C:\Windows\Installer</c>
/// to a user-chosen destination. Refuses any destination that resolves
/// (after symlink expansion) to <c>C:\Windows\Installer</c> or a
/// descendant, which would defeat the restore-after-mistakes contract,
/// and any that resolves under a Windows system folder, which would put
/// installer files on a DLL-search path. The reparse-point check uses
/// the real filesystem regardless of any injected <c>IFileSystem</c>.
/// </summary>
public interface IMoveFilesService
{
    /// <summary>
    /// Move every path in <paramref name="filePaths"/> into
    /// <paramref name="destinationFolder"/> (created if missing).
    /// Throws <see cref="InvalidOperationException"/> if the destination
    /// is not fully qualified, resolves inside the Installer folder,
    /// resolves under a Windows system folder, or is swapped for another
    /// folder part-way through the batch, and
    /// <see cref="UnauthorizedAccessException"/> if the destination is
    /// not writable. Per-file failures are surfaced via the result's
    /// <see cref="MoveResult.Errors"/>, not exceptions. A cancellation
    /// requested mid-operation is NOT thrown: the batch stops and the
    /// accumulated result is returned with <see cref="MoveResult.Cancelled"/>
    /// set (a cancellation before the worker starts still surfaces as
    /// <see cref="OperationCanceledException"/>).
    /// </summary>
    /// <param name="patchClaims">
    /// The claims naming the paths in <paramref name="filePaths"/>, from the
    /// caller's pre-act re-verify
    /// (<see cref="ReverifyResult.SurvivingPatchClaims"/>). Re-read once the
    /// installer mutex is held and before any file is touched, so a verdict that
    /// moved while the caller's enumeration was running is caught inside the hold
    /// rather than outside it; the paths it condemns come back in
    /// <see cref="MoveResult.HeldBack"/>. Null or empty means nothing to re-read,
    /// which is the ordinary case: a true orphan carries no claim.
    ///
    /// Last in the parameter list rather than beside
    /// <paramref name="filePaths"/>, where it belongs by meaning, because every
    /// existing caller passes the two after it positionally or by name.
    /// </param>
    Task<MoveResult> MoveFilesAsync(
        IEnumerable<string> filePaths,
        string destinationFolder,
        IProgress<OperationProgress>? progress = null,
        CancellationToken cancellationToken = default,
        IReadOnlyList<PatchClaim>? patchClaims = null);
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
/// <param name="HeldBack">
/// Paths dropped from the batch by the re-read taken under the installer mutex,
/// and therefore never touched. They are the same two conditions the caller's
/// pre-act re-verify reports and are meant to be folded into it: a program claims
/// the file again, or the records could not be read and nothing has shown that it
/// does not. <see cref="HeldBackRecordsIncomplete"/> says which.
/// They are NOT errors and NOT failures, so they are not in
/// <see cref="Errors"/>, and a caller summing input against
/// <see cref="MovedCount"/> + <see cref="Errors"/> must subtract them.
/// </param>
/// <param name="HeldBackRecordsIncomplete">
/// At least one of the <see cref="HeldBack"/> paths is held back because a
/// property read failed rather than because a program reclaimed it. Carried
/// through so the caller folds it into
/// <see cref="ReverifyResult.RecordsIncomplete"/> and the user is shown the
/// sentence that is true: the two causes have different copy and the app has
/// always distinguished them.
/// </param>
public record MoveResult(
    int MovedCount,
    IReadOnlyList<FileOperationError> Errors,
    bool Cancelled = false,
    bool InstallerBusy = false,
    IReadOnlyList<string>? HeldBack = null,
    bool HeldBackRecordsIncomplete = false)
{
    /// <summary>Never null: an absent list reads as nothing held back.</summary>
    public IReadOnlyList<string> HeldBack { get; init; } = HeldBack ?? Array.Empty<string>();
}
