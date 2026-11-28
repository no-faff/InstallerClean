using InstallerClean.Models;

namespace InstallerClean.Services;

/// <summary>
/// Deletes orphaned MSI / MSP files permanently. Each file is opened so that nothing else
/// can open it, renamed out of its place into the app's own folder inside the Installer
/// folder (<see cref="IInHandFolder"/>), and deleted through that handle only after a
/// second look at Windows Installer's records and signals. The two safety checks made on
/// each file in its place do not go through the injected
/// <see cref="System.IO.Abstractions.IFileSystem"/>: the reparse-point refusal and the
/// containment guard read the real filesystem whatever is injected, so a mock cannot talk
/// its way past either.
/// </summary>
public interface IDeleteFilesService
{
    /// <summary>
    /// Deletes every file of <paramref name="check"/>. Per-file failures are recorded in
    /// <see cref="DeleteResult.Errors"/>, not thrown. A cancellation requested
    /// mid-operation is NOT thrown: the batch stops and the accumulated result is returned
    /// with <see cref="DeleteResult.Cancelled"/> set (a cancellation before the worker
    /// starts still surfaces as <see cref="OperationCanceledException"/>).
    /// </summary>
    /// <param name="check">
    /// The check the host made just before the call (<see cref="IBatchCheck"/>), with its
    /// watch armed. Its files are the batch. The service owns it from the call on and
    /// disposes of it, and where a look finds the records changed or Windows Installer
    /// busy, makes the check again over the files not yet done, under the same bound.
    /// </param>
    /// <param name="progress">
    /// Told each file as the batch reaches it, and each wait and check made in the middle
    /// of the batch (<see cref="OperationProgress.Check"/>).
    /// </param>
    /// <param name="mayAct">
    /// Asked with each file's path before that file's progress report, and again as the
    /// last thing before the file is opened, after every check on it. False ends the batch
    /// as a cancellation does, with what was done so far and that file untouched. Asked on
    /// the batch's own thread, so it must answer at once. Null asks nothing.
    /// </param>
    Task<DeleteResult> DeleteFilesAsync(
        StandingCheck check,
        IProgress<OperationProgress>? progress = null,
        CancellationToken cancellationToken = default,
        Func<string, bool>? mayAct = null);
}

/// <summary>
/// Outcome of a Delete. When <see cref="Cancelled"/> is <c>false</c> and
/// <see cref="Ended"/> is null,
/// <see cref="DeletedCount"/> + <see cref="Errors"/>.Count + <see cref="HeldBack"/>.Count
/// sum to the batch's file count: every file was deleted, recorded as an error, or dropped
/// by a later check. <see cref="HeldBack"/> is in that sum because it is the term a caller
/// is most likely to forget, and the one that makes the other two describe a smaller batch
/// than the caller handed in. A batch that was cancelled or that ended part-way accounts
/// in the three for the files it reached, and the rest were never touched.
/// </summary>
/// <param name="HeldBack">
/// Files a check made in the middle of the batch dropped from it, none of them touched.
/// They are not errors, so they are not in <see cref="Errors"/>, and a caller folds them
/// into the first check's own account (<see cref="ReverifyResult.WithLaterChecks"/>).
/// </param>
/// <param name="HeldBackReasons">
/// How many of <see cref="HeldBack"/> fell to each cause. Nothing a person reads names a
/// cause; the counts travel in the opt-in report.
/// </param>
/// <param name="Ended">
/// Why the batch ended before its last file, where it did. Null for a batch that reached
/// its last file and for one that was cancelled. The counts beside it are of what was done
/// before it ended, so a host shows it as a stopped batch.
/// </param>
/// <param name="SourceRootsGivenUp">
/// Every drive or share the checks made in the middle of the batch stopped waiting for,
/// as <see cref="ReverifyResult.SourceRootsGivenUp"/> is for the first check.
/// </param>
/// <param name="SourceWaitCount">The waits on a source folder those checks made.</param>
/// <param name="CheckCounts">What those checks did, for the opt-in report.</param>
/// <param name="Counts">What the batch did on its way through the files, for the opt-in report.</param>
public record DeleteResult(
    int DeletedCount,
    IReadOnlyList<FileOperationError> Errors,
    bool Cancelled = false,
    IReadOnlyList<string>? HeldBack = null,
    HeldBackReasons HeldBackReasons = default,
    BatchEnd? Ended = null,
    IReadOnlyList<SourceRootGivenUp>? SourceRootsGivenUp = null,
    int SourceWaitCount = 0,
    BatchCheckCounts CheckCounts = default,
    BatchRunCounts Counts = default)
{
    /// <summary>Never null: an absent list reads as nothing held back.</summary>
    public IReadOnlyList<string> HeldBack { get; init; } = HeldBack ?? Array.Empty<string>();

    /// <summary>Never null: an absent list reads as checks that gave nothing up.</summary>
    public IReadOnlyList<SourceRootGivenUp> SourceRootsGivenUp { get; init; }
        = SourceRootsGivenUp ?? Array.Empty<SourceRootGivenUp>();
}
