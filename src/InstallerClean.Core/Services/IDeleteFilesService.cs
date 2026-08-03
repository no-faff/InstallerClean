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
    /// <param name="patchClaims">
    /// The claims naming the paths in <paramref name="filePaths"/>, from the
    /// caller's pre-act re-verify
    /// (<see cref="ReverifyResult.SurvivingPatchClaims"/>). Re-read once the
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
        IReadOnlyList<PatchClaim>? patchClaims = null);
}

/// <summary>
/// Outcome of a Delete. When <see cref="Cancelled"/>, <see cref="InstallerBusy"/>
/// and <see cref="InstallerLockUnavailable"/> are all <c>false</c>,
/// <see cref="DeletedCount"/> + <see cref="Errors"/>.Count sum to the input
/// count: every file was deleted or recorded as an error. When
/// <see cref="Cancelled"/> is <c>true</c> the batch was stopped mid-way, so the
/// count and errors reflect the files reached before the cancel and the rest of
/// the input was never touched.
/// </summary>
/// <param name="InstallerBusy">
/// The batch was refused before it started because a Windows Installer
/// transaction held <c>Global\_MSIExecute</c>: nothing was touched. The caller
/// re-checks the pending-reboot gate and shows its banner, which now reports the
/// held mutex.
/// </param>
/// <param name="InstallerLockUnavailable">
/// The batch was refused before it started because <c>Global\_MSIExecute</c>
/// could not be acquired AND nothing else was holding it: nothing was touched.
/// Kept separate from <see cref="InstallerBusy"/> because the two need different
/// answers, not different wording. The pending-reboot gate is what reports the
/// busy case, and it can say nothing at all about this one: no process holds the
/// mutex, so a re-run of the gate comes back clean and the caller would report a
/// refusal it could not account for. This flag carries its own sentence instead.
/// </param>
/// <param name="HeldBack">
/// Paths dropped from the batch by the re-read taken under the installer mutex,
/// and therefore never touched. They are the same two conditions the caller's
/// pre-act re-verify reports and are meant to be folded into it: a program claims
/// the file again, or the records could not be read and nothing has shown that it
/// does not. <see cref="HeldBackRecordsIncomplete"/> says which.
/// They are NOT errors and NOT failures, so they are not in
/// <see cref="Errors"/>, and a caller summing input against
/// <see cref="DeletedCount"/> + <see cref="Errors"/> must subtract them.
/// </param>
/// <param name="HeldBackRecordsIncomplete">
/// At least one of the <see cref="HeldBack"/> paths is held back because a
/// property read failed rather than because a program reclaimed it. Carried
/// through so the caller folds it into
/// <see cref="ReverifyResult.RecordsIncomplete"/> and the user is shown the
/// sentence that is true: the two causes have different copy and the app has
/// always distinguished them.
/// </param>
public record DeleteResult(
    int DeletedCount,
    IReadOnlyList<FileOperationError> Errors,
    bool Cancelled = false,
    bool InstallerBusy = false,
    bool InstallerLockUnavailable = false,
    IReadOnlyList<string>? HeldBack = null,
    bool HeldBackRecordsIncomplete = false)
{
    /// <summary>Never null: an absent list reads as nothing held back.</summary>
    public IReadOnlyList<string> HeldBack { get; init; } = HeldBack ?? Array.Empty<string>();
}
