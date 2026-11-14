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
    /// What the batch was checked against. Its <see cref="UnderLeaseClaims.Print"/> is
    /// the reading of Windows Installer's records the caller's pre-act re-verify held
    /// for, and production builds it with <see cref="UnderLeaseClaims.From"/> out of
    /// that re-verify. Each hold compares a fresh reading with it before touching a
    /// file; where they differ, the files not yet reached are checked again outside the
    /// hold, and those the check does not confirm come back in
    /// <see cref="DeleteResult.HeldBack"/>. A value with no reading, such as
    /// <see cref="UnderLeaseClaims.None"/>, matches no hold, so no file is deleted on it.
    /// </param>
    /// <param name="mayAct">
    /// Asked with each file's path before the checks on that file, and again before each
    /// act on it, the last time immediately before it is deleted. False ends the batch as a
    /// cancellation does, with what was done so far and that file untouched. Asked inside
    /// a Windows Installer transaction that turns away every installer on the machine, so
    /// it must answer at once. Null asks nothing.
    /// </param>
    Task<DeleteResult> DeleteFilesAsync(
        IEnumerable<string> filePaths,
        UnderLeaseClaims underLeaseClaims,
        IProgress<OperationProgress>? progress = null,
        CancellationToken cancellationToken = default,
        Func<string, bool>? mayAct = null);
}

/// <summary>
/// Outcome of a Delete. When <see cref="Cancelled"/> and the refusal flags are all
/// <c>false</c>, <see cref="DeletedCount"/> + <see cref="Errors"/>.Count +
/// <see cref="HeldBack"/>.Count sum to the input count: every file was deleted,
/// recorded as an error, or held back by a re-check. <see cref="HeldBack"/> is in that
/// sum rather than a footnote to it, because it is the term a caller is most likely to
/// forget. When <see cref="Cancelled"/> is <c>true</c> the batch was stopped mid-way,
/// so the three account for the files reached before the cancel and the rest of the
/// input was never touched.
/// </summary>
/// <param name="InstallerBusy">
/// Windows Installer was busy with another installation for the whole of the wait
/// before the first file, or a re-check found it busy before the first file: nothing
/// was touched. Once a file has been deleted, a busy Windows Installer is waited out
/// instead, so this is never set beside a deleted file.
/// </param>
/// <param name="InstallerLockUnavailable">
/// Windows Installer would not open the app's transaction for a reason other than
/// being busy, or ending one failed. Before the first file nothing was touched; later,
/// <see cref="DeletedCount"/> says what was done and the rest was left alone. The code
/// Windows Installer gave is in the crash log.
/// </param>
/// <param name="InstallerLockAccessRefused">Not set by a Delete.</param>
/// <param name="HeldBack">
/// Paths a re-check made between holds did not confirm, and therefore never touched.
/// Their causes are ones the caller's pre-act re-verify reports, and they are meant to
/// be folded into it. <see cref="HeldBackReasons"/> says how many fell to each. They
/// are NOT errors, so they are not in <see cref="Errors"/>.
/// </param>
/// <param name="HeldBackReasons">
/// How many of the <see cref="HeldBack"/> paths fell to each cause. Carried through so
/// the caller ADDS it to <see cref="ReverifyResult.Reasons"/>: the two producers hold
/// back different files, so the run's one held-back line counts both and neither host
/// prints twice. The counts travel in the opt-in result log, which is the only place
/// the causes can be told apart on a real machine.
/// </param>
public record DeleteResult(
    int DeletedCount,
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

    /// <summary>
    /// The DisableRollback policy is set, so Windows Installer opens no transaction and
    /// nothing was deleted.
    /// </summary>
    public bool InstallerRollbackDisabled { get; init; }
}
