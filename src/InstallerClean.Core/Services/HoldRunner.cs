using System.Diagnostics;
using InstallerClean.Helpers;

namespace InstallerClean.Services;

/// <summary>How a <see cref="HoldRunner.Run"/> ended.</summary>
public enum HoldRunEnd
{
    /// <summary>Every file was reached, or held back by a re-check.</summary>
    Finished,

    /// <summary>Windows Installer stayed busy for the whole wait, so the files not reached were left alone.</summary>
    InstallerBusy,

    /// <summary>The DisableRollback policy is set, so no hold can be taken and no file was reached.</summary>
    RollbackDisabled,

    /// <summary>A begin failed some other way, so the files not reached were left alone.</summary>
    HoldUnavailable,

    /// <summary>An end failed, so the process takes no further hold and the files not reached were left alone.</summary>
    EndFailed,

    /// <summary>A re-check found Windows Installer busy or a restart pending, so the files not reached were left alone.</summary>
    InstallerActivity,
}

/// <summary>What <see cref="HoldRunner.Run"/> did.</summary>
/// <param name="End">How it ended.</param>
/// <param name="Reached">How many files the act was called for, in the order given.</param>
/// <param name="HeldBack">Files a re-check made between holds did not confirm, never acted on.</param>
/// <param name="Reasons">Why, counted.</param>
/// <param name="Code">The Windows Installer code behind an end other than <see cref="HoldRunEnd.Finished"/>.</param>
/// <param name="InstallerActivity">The re-check's reading where it ended the run.</param>
public sealed record HoldRunResult(
    HoldRunEnd End,
    int Reached,
    IReadOnlyList<string> HeldBack,
    HeldBackReasons Reasons,
    uint? Code = null,
    PendingRebootResult? InstallerActivity = null);

/// <summary>
/// What a run has found so far, kept outside the run so a caller still has it when the
/// run ends in a cancel or a stop.
/// </summary>
public sealed class HoldRunTally
{
    /// <summary>Files a re-check made between holds did not confirm, never acted on.</summary>
    public List<string> HeldBack { get; } = new();

    /// <summary>Why, counted.</summary>
    public HeldBackReasons Reasons { get; set; }

    /// <summary>How many files the act was called for.</summary>
    public int Reached { get; set; }

    /// <summary>A re-check's reading of Windows Installer activity that stopped the run, if one did.</summary>
    public PendingRebootResult? InstallerActivity { get; set; }

    internal void HoldBack(IReadOnlyList<string> paths, HeldBackReason reason)
    {
        HeldBack.AddRange(paths);
        for (var i = 0; i < paths.Count; i++)
            Reasons = Reasons.Plus(reason);
    }
}

/// <summary>The bounds a <see cref="HoldRunner"/> works within.</summary>
/// <param name="GroupBudget">How long after a begin returns the runner goes on starting files in the same hold.</param>
/// <param name="GroupFiles">The most files acted on in one hold.</param>
/// <param name="BusyWait">
/// How long the runner waits, trying again each second, while Windows Installer is busy
/// before the first file. Once a file has been acted on, it waits until Windows Installer
/// is free or the run is cancelled, so a batch is not left part done by an installation
/// that started between two holds.
/// </param>
/// <param name="Rechecks">How many times one run re-checks its remaining files after the records changed.</param>
public sealed record HoldRunnerLimits(TimeSpan GroupBudget, int GroupFiles, TimeSpan BusyWait, int Rechecks)
{
    /// <summary>
    /// A hold short enough that an installer started in it and retried by a bootstrapper
    /// is still retried after it ends, and long enough that a batch takes few holds.
    /// </summary>
    public static HoldRunnerLimits Default { get; } =
        new(Durations.FromSeconds(1), 200, Durations.FromSeconds(60), 3);
}

/// <summary>
/// Acts on a batch of files inside Windows Installer transactions of the app's own, a
/// group at a time. For each group it takes an <see cref="InstallerHold"/>, compares a
/// fresh <see cref="InstallerRecordsPrint"/> with the one the batch was checked
/// against, and only where they match calls the act for each file until the group's
/// budget is spent. Then it ends the hold and reports the group's files.
///
/// THE ACT RUNS INSIDE THE HOLD, AND NOTHING ELSE THAT CAN WAIT DOES. While a hold is
/// open every installer on the machine is turned away, so progress is reported after
/// the hold ends, and a re-check, which can wait on a source folder, runs between holds.
/// The act itself must not wait on anything but the local file it acts on; it is
/// called on this thread, between a begin and an end made on this thread.
///
/// A reading that does not match means something Windows Installer records changed
/// after the batch was checked. No file is acted on in that hold. The remaining files
/// are checked again outside it, those the check does not confirm are held back, and
/// the run goes on against the new reading. After <see cref="HoldRunnerLimits.Rechecks"/>
/// such re-checks the remaining files are held back as records changed.
/// </summary>
public sealed class HoldRunner
{
    /// <summary>
    /// The transaction's name. Windows uses it as the description of any System Restore
    /// point a begin makes.
    /// </summary>
    public const string TransactionName = "InstallerClean";

    private readonly IInstallerTransactionApi _api;
    private readonly IInstallerRecordsFingerprint _fingerprint;
    private readonly IRemovableReverifier _reverifier;
    private readonly HoldRunnerLimits _limits;
    private readonly Func<TimeSpan> _clock;
    private readonly Func<TimeSpan, CancellationToken, bool> _pause;
    private readonly bool _compareReadings = true;

    /// <summary>
    /// A runner for the action services' test constructors: it opens no transaction on the
    /// machine and does not compare readings, so a test of what the services do to files
    /// can run without Windows Installer. Never registered for the app.
    /// </summary>
    internal static HoldRunner ForTests() =>
        new(new NoTransaction(), new NoReading(), NullRemovableReverifier.Instance,
            HoldRunnerLimits.Default, null, null, compareReadings: false);

    private sealed class NoTransaction : IInstallerTransactionApi
    {
        public uint Begin(string name, out InstallerTransactionHandles handles)
        {
            handles = default;
            return 0;
        }

        public uint Commit() => 0;

        public void Release(InstallerTransactionHandles handles) { }
    }

    private sealed class NoReading : IInstallerRecordsFingerprint
    {
        public InstallerRecordsPrint Take() => InstallerRecordsPrint.None;
    }

    public HoldRunner(IInstallerTransactionApi api, IInstallerRecordsFingerprint fingerprint,
        IRemovableReverifier reverifier)
        : this(api, fingerprint, reverifier, HoldRunnerLimits.Default, null, null) { }

    /// <summary>
    /// Test constructor. <paramref name="clock"/> is the time elapsed since some fixed
    /// point; <paramref name="pause"/> waits, returning false where the token was cancelled.
    /// </summary>
    internal HoldRunner(IInstallerTransactionApi api, IInstallerRecordsFingerprint fingerprint,
        IRemovableReverifier reverifier, HoldRunnerLimits limits,
        Func<TimeSpan>? clock, Func<TimeSpan, CancellationToken, bool>? pause,
        bool compareReadings = true)
    {
        _compareReadings = compareReadings;
        _api = api;
        _fingerprint = fingerprint;
        _reverifier = reverifier;
        _limits = limits;
        if (clock is null)
        {
            var watch = Stopwatch.StartNew();
            clock = () => watch.Elapsed;
        }
        _clock = clock;
        _pause = pause ?? ((wait, token) => !token.WaitHandle.WaitOne(wait));
    }

    /// <summary>
    /// Runs the batch. <paramref name="act"/> is called inside a hold for each file, in
    /// order; <paramref name="processed"/> is called outside the holds for each file the
    /// act returned for, with how many files had been reported before it and how many
    /// files the batch now holds, those held back by a re-check taken out. An exception
    /// from the act ends the run after the hold has ended and the files of the group the
    /// act returned for have been reported, the file it threw for not among them, since
    /// the act did not finish with it; <paramref name="tally"/> still holds what was found.
    /// </summary>
    public HoldRunResult Run(
        IReadOnlyList<string> paths,
        InstallerRecordsPrint baseline,
        Action<string> act,
        Action<int, int, string> processed,
        CancellationToken cancellationToken,
        HoldRunTally? tally = null)
    {
        tally ??= new HoldRunTally();
        var queue = new List<string>(paths);
        var next = 0;
        var reported = 0;
        var rechecks = 0;
        var lastReading = baseline;

        HoldRunResult Stop(HoldRunEnd end, uint? code = null)
        {
            // The crash log is the only place the machine's own answer is written down;
            // the person is told the run stopped, in the host's words.
            if (end is HoldRunEnd.HoldUnavailable or HoldRunEnd.EndFailed or HoldRunEnd.RollbackDisabled)
                Helpers.CrashLog.TryWrite(new InvalidOperationException(
                    $"A batch stopped with {tally.Reached} file(s) reached: {end}, Windows Installer code {code?.ToString() ?? "none"}."));
            return new(end, tally.Reached, tally.HeldBack, tally.Reasons, code, tally.InstallerActivity);
        }

        // A baseline that is not a complete reading can match no hold, so no hold is
        // taken for it: the files go straight to a re-check, as after a mismatch.
        var matched = false;

        while (next < queue.Count)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (!_compareReadings || baseline.Complete)
            {
            var hold = BeginWaitingWhileBusy(tally.Reached > 0, out var start, out var code, cancellationToken);
            if (hold is null)
                return start switch
                {
                    InstallerHoldStart.InstallerBusy => Stop(HoldRunEnd.InstallerBusy, code),
                    InstallerHoldStart.RollbackDisabled => Stop(HoldRunEnd.RollbackDisabled, code),
                    _ => Stop(HoldRunEnd.HoldUnavailable, code),
                };

            var groupStart = next;
            var finished = next;
            matched = false;
            try
            {
                using (hold)
                {
                    lastReading = _fingerprint.Take();
                    matched = !_compareReadings || baseline.Matches(lastReading);
                    if (matched)
                    {
                        var opened = _clock();
                        while (next < queue.Count
                            && next - groupStart < _limits.GroupFiles
                            && (next == groupStart || _clock() - opened < _limits.GroupBudget))
                        {
                            cancellationToken.ThrowIfCancellationRequested();
                            var path = queue[next];
                            next++;
                            tally.Reached++;
                            act(path);
                            finished = next;
                        }
                    }
                }
            }
            finally
            {
                for (var i = groupStart; i < finished; i++)
                    processed(reported++, queue.Count, queue[i]);
            }

            if (!hold.Ended)
                return Stop(HoldRunEnd.EndFailed, hold.EndCode);

            if (matched)
                continue;
            }

            var remaining = queue.GetRange(next, queue.Count - next);
            if (rechecks == _limits.Rechecks)
            {
                // A reading that could not be completed has not shown a change; it has
                // shown nothing, and the files are counted under that cause.
                var reason = lastReading.Complete && baseline.Complete
                    ? HeldBackReason.RecordsChanged
                    : HeldBackReason.RecordsUnreadable;
                tally.HoldBack(remaining, reason);
                queue.RemoveRange(next, remaining.Count);
                return Stop(HoldRunEnd.Finished);
            }
            rechecks++;

            ReverifyResult recheck;
            try
            {
                recheck = _reverifier.ReverifyAsync(remaining, cancellationToken).GetAwaiter().GetResult();
            }
            catch (Exception ex) when (tally.Reached > 0
                && ex is not OperationCanceledException and not OutOfMemoryException and not StackOverflowException)
            {
                // Files have been acted on, so the run returns what it did rather than
                // throwing it away; the files left were not confirmed and are held back.
                Helpers.CrashLog.TryWrite(ex);
                tally.HoldBack(remaining, HeldBackReason.RecordsUnreadable);
                return Stop(HoldRunEnd.Finished);
            }

            if (recheck.InstallerActivity is { IsBlocked: true } activity)
            {
                // Before any file, the run stops as a busy Windows Installer stops it.
                // After one, the files left are held back as files that could not be
                // checked, so the batch still ends with every file accounted for, and
                // the reading goes back for the host to show.
                tally.InstallerActivity = activity;
                if (tally.Reached == 0)
                    return Stop(HoldRunEnd.InstallerActivity);
                tally.HoldBack(remaining, HeldBackReason.RecordsUnreadable);
                return Stop(HoldRunEnd.Finished);
            }

            tally.HeldBack.AddRange(recheck.Dropped);
            tally.Reasons += recheck.Reasons;
            queue.RemoveRange(next, remaining.Count);
            queue.AddRange(recheck.Surviving);
            baseline = recheck.Print ?? InstallerRecordsPrint.None;
        }

        return Stop(HoldRunEnd.Finished);
    }

    private InstallerHold? BeginWaitingWhileBusy(
        bool actedYet, out InstallerHoldStart start, out uint code, CancellationToken cancellationToken)
    {
        var waitStarted = _clock();
        while (true)
        {
            var hold = InstallerHold.TryBegin(_api, TransactionName, out start, out code);
            if (hold is not null || start != InstallerHoldStart.InstallerBusy)
                return hold;
            if (!actedYet && _clock() - waitStarted >= _limits.BusyWait)
                return null;
            if (!_pause(Durations.FromSeconds(1), cancellationToken))
                cancellationToken.ThrowIfCancellationRequested();
        }
    }
}
