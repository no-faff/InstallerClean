using InstallerClean.Models;

namespace InstallerClean.Services;

/// <summary>
/// Makes the check a Move or Delete acts on: the full check of the batch's files
/// (<see cref="IRemovableReverifier"/>), made while a watch on Windows Installer's records
/// is armed and with Windows Installer's signals read at both ends, and made again until
/// one stands.
///
/// A CHECK STANDS where the signals read clean before it and after it and the watch reads
/// <see cref="RecordsWatchReading.Unchanged"/> after it. Every record the check read was
/// then as the check read it for the whole of the check, and stays so for as long as the
/// watch it hands back goes on reading unchanged.
///
/// BOTH HOSTS CALL IT FOR THE FIRST CHECK OF A BATCH AND BOTH SERVICES FOR EVERY LATER ONE,
/// so the start of a batch and its middle wait, give up and stand by one rule.
/// </summary>
public interface IBatchCheck
{
    /// <summary>
    /// Waits for Windows Installer to stop being busy, then checks
    /// <paramref name="filePaths"/>, and does both again until a check stands or the batch
    /// has to end.
    ///
    /// IT WAITS BEFORE IT CHECKS. The full check holds every file no record names while a
    /// signal is up, so a check made while Windows Installer is busy would drop files that
    /// pass a minute later.
    ///
    /// A cancellation, and any exception the full check raises, comes out as it is, with
    /// no watch left armed.
    /// </summary>
    /// <param name="waitBound">
    /// The longest this call goes on without a check that stands because Windows Installer
    /// is busy, after which it ends with <see cref="BatchEndReason.WaitBoundReached"/>.
    /// Null waits for as long as it takes, for a host whose user can see the wait and
    /// cancel it.
    /// </param>
    /// <param name="progress">
    /// Told when a wait for Windows Installer begins, when a check begins, and each wait
    /// the check makes on a source folder. Reported on whichever thread the call is
    /// running on.
    /// </param>
    Task<BatchCheckResult> RunAsync(
        IReadOnlyList<string> filePaths,
        TimeSpan? waitBound,
        IProgress<BatchCheckProgress>? progress = null,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// What <see cref="IBatchCheck.RunAsync"/> came to: a check that stood, or the reason
/// there is none. Exactly one of <see cref="Stood"/> and <see cref="Ended"/> is set.
/// Disposing of it disposes of the standing check, so a host that stops between the check
/// and the batch leaves no watch armed.
/// </summary>
public sealed class BatchCheckResult : IDisposable
{
    internal BatchCheckResult(StandingCheck? stood, BatchEnd? ended, BatchCheckCounts counts)
    {
        Stood = stood;
        Ended = ended;
        Counts = counts;
    }

    /// <summary>The check that stood, with its watch still armed. Null where the call ended without one.</summary>
    public StandingCheck? Stood { get; }

    /// <summary>Why there is no standing check. Null where there is one.</summary>
    public BatchEnd? Ended { get; }

    /// <summary>What the call did on the way, for the opt-in report.</summary>
    public BatchCheckCounts Counts { get; }

    public void Dispose() => Stood?.Dispose();
}

/// <summary>
/// A full check that stood, and the watch armed before it, which is still armed. It is
/// the batch a Move or Delete is handed: the files in <see cref="Files"/> are the ones
/// the check confirmed, and no file outside it has been checked under this watch.
///
/// ONLY <see cref="IBatchCheck"/> MAKES ONE, which is what lets the services take it in
/// place of a list of paths: a caller cannot hand over files that no armed watch covers.
/// Keep the constructor internal.
///
/// Disposing of it releases the watch. It is safe to dispose of more than once, and the
/// host that asked for it and the service it was handed to both do.
/// </summary>
public sealed class StandingCheck : IDisposable
{
    private int _disposed;

    internal StandingCheck(ReverifyResult result, IRecordsWatch watch, TimeSpan? waitBound)
    {
        Result = result;
        Watch = watch;
        WaitBound = waitBound;
    }

    /// <summary>
    /// The full check's own result: what it confirmed, what it dropped and under which
    /// causes, and the drives and shares it stopped waiting for.
    /// </summary>
    public ReverifyResult Result { get; }

    /// <summary>The files the check confirmed, in the order they were handed in.</summary>
    public IReadOnlyList<string> Files => Result.Surviving;

    /// <summary>
    /// The bound the check was made under (<see cref="IBatchCheck.RunAsync"/>), which
    /// every later check of the same batch is made under too.
    /// </summary>
    public TimeSpan? WaitBound { get; }

    /// <summary>The watch armed before the check began.</summary>
    internal IRecordsWatch Watch { get; }

    public void Dispose()
    {
        if (Interlocked.Exchange(ref _disposed, 1) == 0) Watch.Dispose();
    }
}

/// <summary>Why a batch ended before its last file, or before its first.</summary>
/// <param name="Reason">Which of the reasons it is.</param>
/// <param name="Signals">
/// The read of Windows Installer's signals that ended it, for
/// <see cref="BatchEndReason.SignalWaitCannotClear"/> and
/// <see cref="BatchEndReason.WaitBoundReached"/>. Null for the others.
/// </param>
/// <param name="WindowsError">
/// The Windows error, for <see cref="BatchEndReason.InHandFolderUnavailable"/>. Nought for the others.
/// </param>
/// <param name="Failure">
/// What was thrown, for <see cref="BatchEndReason.CheckFailed"/>. Null for the others.
/// </param>
public sealed record BatchEnd(
    BatchEndReason Reason,
    PendingRebootResult? Signals = null,
    int WindowsError = 0,
    Exception? Failure = null);

/// <summary>
/// The reasons a batch ends other than by reaching its last file, by a cancel or by its
/// destination. None of them is a finding about a file: every file not yet reached is
/// where it was.
/// </summary>
public enum BatchEndReason
{
    /// <summary>
    /// A read of Windows Installer's signals was blocked by a reason that waiting does not
    /// change (<see cref="PendingRebootReasons.ClearsByWaiting"/>): a signal that could not
    /// be read, one the app was refused permission to read, or a file operation queued for
    /// the next restart that names the Installer folder.
    /// </summary>
    SignalWaitCannotClear,

    /// <summary>Windows Installer stayed busy for the whole of the wait's bound.</summary>
    WaitBoundReached,

    /// <summary>
    /// Three checks running did not stand because Windows Installer's records were written
    /// while each was reading them, with no wait between them.
    /// </summary>
    RecordsKeptChanging,

    /// <summary>The watch on Windows Installer's records could not be armed.</summary>
    WatchUnavailable,

    /// <summary>
    /// The batch's own folder inside the Installer folder could not be made
    /// (<see cref="IInHandFolder.OpenBatch"/>), so no file was touched.
    /// </summary>
    InHandFolderUnavailable,

    /// <summary>
    /// A check made in the middle of a batch, or a read of the signals before a file,
    /// threw. The file in hand was put back first, and the batch's result carries what
    /// was done before it, which a throw out of the batch would lose.
    /// </summary>
    CheckFailed,
}

/// <summary>One thing <see cref="IBatchCheck.RunAsync"/> reports while it runs.</summary>
/// <param name="Stage">What has begun.</param>
/// <param name="Signals">The blocked read being waited on, for <see cref="BatchCheckStage.Waiting"/>.</param>
/// <param name="SourceWait">
/// The full check's own report, for <see cref="BatchCheckStage.SourceWait"/>: a wait on a
/// program's source folder beginning, with the handle that stops it, or that wait ending
/// (<see cref="IRemovableReverifier.ReverifyAsync"/>).
/// </param>
public sealed record BatchCheckProgress(
    BatchCheckStage Stage,
    PendingRebootResult? Signals = null,
    ScanProgressUpdate? SourceWait = null);

/// <summary>What a <see cref="BatchCheckProgress"/> reports.</summary>
public enum BatchCheckStage
{
    /// <summary>
    /// Windows Installer is busy and the call is waiting for it. Reported once when a wait
    /// begins, and again only where the reason changes.
    /// </summary>
    Waiting,

    /// <summary>A full check is beginning.</summary>
    Checking,

    /// <summary>The full check is reporting a wait of its own on a source folder.</summary>
    SourceWait,
}

/// <summary>
/// What one or more calls of <see cref="IBatchCheck.RunAsync"/> did, as counts and times
/// for the opt-in report. Nothing a person reads is built from them.
/// </summary>
/// <param name="ChecksMade">Full checks begun.</param>
/// <param name="ChecksThatDidNotStand">Of those, the ones thrown away because a signal was up or the records changed.</param>
/// <param name="CheckMilliseconds">Time spent in full checks.</param>
/// <param name="WaitMilliseconds">Time spent waiting for Windows Installer.</param>
/// <param name="WaitsForMutex">Waits that began on the Windows Installer mutex being held.</param>
/// <param name="WaitsForInProgressKey">Waits that began on the InProgress registry key.</param>
/// <param name="WaitsForInProgressFile">Waits that began on Windows Installer's in-progress file.</param>
/// <param name="WaitsForService">Waits that began on the Windows Installer service running.</param>
/// <param name="CallsNeverBusy">
/// Calls that found Windows Installer not busy at every read they made of it: the read of
/// every signal at the outset clear, the first read of whether it is busy clear, no wait
/// begun and no check thrown away for a signal. One call counts once at most, and a call
/// that ended on its outset read, having made no read of whether Windows Installer is busy,
/// counts nothing.
/// </param>
public readonly record struct BatchCheckCounts(
    int ChecksMade = 0,
    int ChecksThatDidNotStand = 0,
    long CheckMilliseconds = 0,
    long WaitMilliseconds = 0,
    int WaitsForMutex = 0,
    int WaitsForInProgressKey = 0,
    int WaitsForInProgressFile = 0,
    int WaitsForService = 0,
    int CallsNeverBusy = 0)
{
    /// <summary>The two added, for a batch that made more than one call.</summary>
    public static BatchCheckCounts operator +(BatchCheckCounts a, BatchCheckCounts b) =>
        new(a.ChecksMade + b.ChecksMade,
            a.ChecksThatDidNotStand + b.ChecksThatDidNotStand,
            a.CheckMilliseconds + b.CheckMilliseconds,
            a.WaitMilliseconds + b.WaitMilliseconds,
            a.WaitsForMutex + b.WaitsForMutex,
            a.WaitsForInProgressKey + b.WaitsForInProgressKey,
            a.WaitsForInProgressFile + b.WaitsForInProgressFile,
            a.WaitsForService + b.WaitsForService,
            a.CallsNeverBusy + b.CallsNeverBusy);

    /// <summary>
    /// These counts with one more wait counted against <paramref name="reason"/>. A reason
    /// no wait is made on counts nothing.
    /// </summary>
    internal BatchCheckCounts PlusWaitFor(PendingRebootReason reason) => reason switch
    {
        PendingRebootReason.MsiExecuteMutexHeld => this with { WaitsForMutex = WaitsForMutex + 1 },
        PendingRebootReason.InstallerInProgress => this with { WaitsForInProgressKey = WaitsForInProgressKey + 1 },
        PendingRebootReason.InstallerInProgressMarker =>
            this with { WaitsForInProgressFile = WaitsForInProgressFile + 1 },
        PendingRebootReason.InstallerServiceRunning => this with { WaitsForService = WaitsForService + 1 },
        _ => this,
    };
}
