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
    /// <see cref="DeleteResult.HeldBack"/>. Null or empty means nothing to
    /// re-read, which is the ordinary case: a true orphan carries no claim.
    ///
    /// Last in the parameter list rather than beside
    /// <paramref name="filePaths"/>, where it belongs by meaning, because every
    /// existing caller passes the two after it positionally or by name.
    /// </param>
    Task<DeleteResult> DeleteFilesAsync(
        IEnumerable<string> filePaths,
        IProgress<OperationProgress>? progress = null,
        CancellationToken cancellationToken = default,
        UnderLeaseClaims? underLeaseClaims = null);
}

/// <summary>
/// Outcome of a Delete. When <see cref="Cancelled"/>, <see cref="InstallerBusy"/>
/// and <see cref="InstallerLockUnavailable"/> are all <c>false</c>,
/// <see cref="DeletedCount"/> + <see cref="Errors"/>.Count + <see cref="HeldBack"/>.Count
/// sum to the input count: every file was deleted, recorded as an error, or kept
/// back by the under-lease re-read. <see cref="HeldBack"/> is in that sum rather
/// than a footnote to it, because it is the term a caller is most likely to
/// forget and the one that makes the other two describe a smaller batch than the
/// caller handed in. When <see cref="Cancelled"/> is <c>true</c> the batch was
/// stopped mid-way, so the three account for the files reached before the cancel
/// and the rest of the input was never touched.
/// </summary>
/// <param name="InstallerBusy">
/// The batch was refused before it started because a Windows Installer
/// transaction held <c>Global\_MSIExecute</c>: nothing was touched. The caller
/// re-checks the pending-reboot gate and shows its banner, which now reports the
/// held mutex.
/// </param>
/// <param name="InstallerLockUnavailable">
/// The batch was refused before it started because <c>Global\_MSIExecute</c>
/// could not be acquired and nothing was shown to be holding it: nothing was
/// touched. Kept separate from <see cref="InstallerBusy"/> because the two need
/// different answers, not different wording. The pending-reboot gate is what
/// reports the busy case, and it accounts for this one neither way: its probe
/// asks through a different call requesting different rights, so it can come
/// back clean and leave a refusal with nothing on screen explaining it, and on
/// an object whose DACL refuses that call too it reports held, which would
/// assert an install nothing has shown. This flag carries its own sentence
/// instead.
/// </param>
/// <param name="HeldBack">
/// Paths dropped from the batch by the re-read taken under the installer mutex,
/// and therefore never touched. They are the same conditions the caller's pre-act
/// re-verify reports and are meant to be folded into it: a program claims the file
/// again, the records no longer hold the registration, or a read failed and
/// nothing has shown the file is not needed. <see cref="HeldBackReasons"/> says
/// how many fell to each. They are NOT errors and NOT failures, so they are not in
/// <see cref="Errors"/>, and a caller summing input against
/// <see cref="DeletedCount"/> + <see cref="Errors"/> must subtract them.
/// </param>
/// <param name="HeldBackReasons">
/// How many of the <see cref="HeldBack"/> paths fell to each cause. Carried
/// through so the caller folds it into <see cref="ReverifyResult.Reasons"/> and
/// the user is shown one line per cause that occurred: the causes have different
/// copy, and a single batch can meet more than one, so a sentence chosen for the
/// set would name a cause that did not happen to some of the files.
/// </param>
public record DeleteResult(
    int DeletedCount,
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
