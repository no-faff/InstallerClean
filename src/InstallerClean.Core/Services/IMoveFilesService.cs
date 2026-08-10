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
/// Outcome of a Move. When <see cref="Cancelled"/>, <see cref="InstallerBusy"/>
/// and <see cref="InstallerLockUnavailable"/> are all <c>false</c>,
/// <see cref="MovedCount"/> + <see cref="Errors"/>.Count + <see cref="HeldBack"/>.Count
/// sum to the input count: every file is either moved, recorded as a failure, or
/// kept back by the under-lease re-read (never silently dropped).
/// <see cref="HeldBack"/> is in that sum rather than a footnote to it, because it
/// is the term a caller is most likely to forget and the one that makes the other
/// two describe a smaller batch than the caller handed in. When
/// <see cref="Cancelled"/> is <c>true</c> the batch was stopped mid-way, so the
/// three account for the files reached before the cancel and the rest of the input
/// was never touched.
///
/// One outcome is NOT in this record, and a caller reading only the record would
/// miss it: a destination guard tripping mid-batch throws
/// <see cref="MoveAbortedException"/>, which carries a result of exactly this
/// shape covering the files the batch reached before it stopped. The sums above
/// hold of that partial result too; what does not hold is the assumption that a
/// returned result is the only way this method reports.
/// </summary>
/// <param name="InstallerBusy">
/// The batch was refused before it started because a Windows Installer
/// transaction held <c>Global\_MSIExecute</c>: nothing was touched, so
/// <see cref="MovedCount"/> is 0 and <see cref="Errors"/> is empty. The caller
/// re-checks the pending-reboot gate and shows its banner.
/// </param>
/// <param name="InstallerLockUnavailable">
/// The batch was refused before it started because <c>Global\_MSIExecute</c>
/// could not be acquired AND nothing else was holding it: nothing was touched,
/// and the service returns before it creates or probes the destination folder.
/// Kept separate from <see cref="InstallerBusy"/> because the two need different
/// answers, not different wording. The pending-reboot gate is what reports the
/// busy case, and it can say nothing at all about this one: no process holds the
/// mutex, so a re-run of the gate comes back clean and the caller would report a
/// refusal it could not account for. This flag carries its own sentence instead.
/// </param>
/// <param name="HeldBack">
/// Paths dropped from the batch by the re-read taken under the installer mutex,
/// and therefore never touched. They are the same conditions the caller's pre-act
/// re-verify reports and are meant to be folded into it: a program claims the file
/// again, the records no longer hold the registration, or a read failed and
/// nothing has shown the file is not needed. <see cref="HeldBackReasons"/> says
/// how many fell to each. They are NOT errors and NOT failures, so they are not in
/// <see cref="Errors"/>, and a caller summing input against
/// <see cref="MovedCount"/> + <see cref="Errors"/> must subtract them.
/// </param>
/// <param name="HeldBackReasons">
/// How many of the <see cref="HeldBack"/> paths fell to each cause. Carried
/// through so the caller folds it into <see cref="ReverifyResult.Reasons"/> and
/// the user is shown one line per cause that occurred: the causes have different
/// copy, and a single batch can meet more than one, so a sentence chosen for the
/// set would name a cause that did not happen to some of the files.
/// </param>
public record MoveResult(
    int MovedCount,
    IReadOnlyList<FileOperationError> Errors,
    bool Cancelled = false,
    bool InstallerBusy = false,
    bool InstallerLockUnavailable = false,
    IReadOnlyList<string>? HeldBack = null,
    HeldBackReasons HeldBackReasons = default)
{
    /// <summary>Never null: an absent list reads as nothing held back.</summary>
    public IReadOnlyList<string> HeldBack { get; init; } = HeldBack ?? Array.Empty<string>();
}
