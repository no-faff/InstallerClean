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
    /// <param name="underLeaseClaims">
    /// Both halves of what the under-lease re-read asks about, as one argument: the
    /// claims naming the paths in <paramref name="filePaths"/>
    /// (<see cref="ReverifyResult.SurvivingPatchClaims"/>), and the claims on every
    /// product those name (<see cref="ReverifyResult.SiblingPatchClaims"/>), because
    /// the offer rests on a fact about the OTHER patches on those products.
    /// Production builds it with <see cref="UnderLeaseClaims.From"/> out of the
    /// caller's pre-act re-verify, so the two halves always come from one
    /// enumeration and from each other. Re-read once the
    /// installer mutex is held and before any file is touched, so a verdict that
    /// moved while the caller's enumeration was running is caught inside the hold
    /// rather than outside it; the paths it condemns come back in
    /// <see cref="MoveResult.HeldBack"/>. An empty batch means nothing to
    /// re-read, which is the ordinary case: a true orphan carries no claim, and a
    /// caller that has none passes <see cref="UnderLeaseClaims.None"/>.
    ///
    /// REQUIRED, AND ITS POSITION FOLLOWS FROM THAT RATHER THAN BEING A CHOICE.
    /// It was optional and last, and an omitted argument became
    /// <see cref="UnderLeaseClaims.None"/>, which the re-read answers at its first
    /// line. So the last check standing in front of the act passed without asking
    /// anything, and nothing could say so: the files go either way, and the only
    /// difference is whether a verdict that moved under the lease was looked for.
    /// A required parameter cannot follow optional ones, so removing the omission
    /// moved this in among the parameters that say what the batch is, which is
    /// where it belongs by meaning. Passing <see cref="UnderLeaseClaims.None"/> is the statement
    /// that there are none, and that is a different act from forgetting to look.
    /// A <c>default</c> value is not a substitute for it: the two lists are then
    /// null rather than empty and the re-read throws on the first of them, which
    /// is loud and is meant to be.
    /// </param>
    Task<MoveResult> MoveFilesAsync(
        IEnumerable<string> filePaths,
        string destinationFolder,
        UnderLeaseClaims underLeaseClaims,
        IProgress<OperationProgress>? progress = null,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Outcome of a Move. When <see cref="Cancelled"/>, <see cref="InstallerBusy"/>
/// and the two installer-lock flags are all <c>false</c>,
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
/// could not be acquired and nothing was shown to be holding it: nothing was
/// touched, and the service returns before it creates or probes the destination
/// folder. Kept separate from <see cref="InstallerBusy"/> because the two need
/// different answers, not different wording. The pending-reboot gate is what
/// reports the busy case, and it accounts for this one neither way: its probe
/// asks through a different call requesting different rights, so it can come
/// back clean and leave a refusal with nothing on screen explaining it, and on
/// an object whose DACL refuses that call too it reports held, which would
/// assert an install nothing has shown. This flag carries its own sentence
/// instead.
/// </param>
/// <param name="InstallerLockAccessRefused">
/// The batch was refused before it started because the security on
/// <c>Global\_MSIExecute</c> refused this process the rights to open it, so
/// whether a transaction held it was never sampled: nothing was touched, and the
/// service returns at the same point as <see cref="InstallerLockUnavailable"/>.
/// Kept apart from that flag because the two are different facts about the
/// machine and the user is told them in different words. This one is a setting on
/// the object rather than a condition that arose while asking, so a sentence
/// telling the user something was holding the lock would be false of it.
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
/// through so the caller ADDS it to <see cref="ReverifyResult.Reasons"/>: the two
/// producers hold back different files, so the run's one held-back line counts
/// both and neither host prints twice.
///
/// THE COUNTS OUTLIVED THE SENTENCES THEY WERE FOR. Until 3.0.0 the user was shown
/// one line per cause, and this note gave that as the reason for carrying them
/// through. The line names no cause now, and these are still carried because they
/// travel in the opt-in result log, which is the only place the causes can still
/// be told apart on a real machine.
/// </param>
public record MoveResult(
    int MovedCount,
    IReadOnlyList<FileOperationError> Errors,
    bool Cancelled = false,
    bool InstallerBusy = false,
    bool InstallerLockUnavailable = false,
    bool InstallerLockAccessRefused = false,
    IReadOnlyList<string>? HeldBack = null,
    HeldBackReasons HeldBackReasons = default)
{
    /// <summary>Never null: an absent list reads as nothing held back.</summary>
    public IReadOnlyList<string> HeldBack { get; init; } = HeldBack ?? Array.Empty<string>();
}
