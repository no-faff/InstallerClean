using System.Diagnostics;
using InstallerClean.Compatibility;
using InstallerClean.Helpers;
using InstallerClean.Models;

namespace InstallerClean.Services;

/// <summary>
/// Default <see cref="IBatchCheck"/>. One call reads every signal once
/// (<see cref="IPendingRebootService.Check"/>) and ends there where a reason waiting does
/// not change blocks it, so that such a batch ends at once and not after a check. It then
/// runs these steps until a check stands or the batch has to end:
///
/// 1. Read whether Windows Installer is busy (<see cref="IPendingRebootService.CheckBusy"/>).
///    Blocked by a reason waiting does not change: end. Busy: wait half a second and read
///    again, up to the caller's bound.
/// 2. Arm a watch on Windows Installer's records, then run the full check
///    (<see cref="IRemovableReverifier.ReverifyAsync"/>), which reads the signals just
///    after its own clock.
/// 3. Read the signals again (<see cref="IPendingRebootService.Check"/>), then read the
///    watch.
/// 4. Both reads clean and the watch unchanged: the check stands and is handed back with
///    its watch. Otherwise the check is thrown away whole, its watch with it, and the
///    steps begin again.
///
/// THE WATCH IS ARMED BEFORE THE CHECK READS ANYTHING AND READ AFTER ITS LAST READ. A
/// record written between the two shows on the watch, so a check that stands read every
/// record as it still is. Arming it later, or reading it before the second signals read,
/// leaves a stretch of the check the watch does not answer for.
///
/// A CHECK THE SIGNALS BLOCK ENDS ON ITS OWN READ where the reason is one waiting does not
/// change, and never by going back to step 1 for it. The check's reads take in the file
/// operations queued for the next restart and step 1's read does not, so a queued
/// operation naming the Installer folder reads clean at step 1 and blocked in every
/// check.
/// </summary>
public sealed class BatchCheck : IBatchCheck
{
    /// <summary>How long one turn of the wait for Windows Installer lasts.</summary>
    internal static readonly TimeSpan WaitTurn = Durations.FromMilliseconds(500);

    /// <summary>
    /// How many checks running may fail to stand on the watch alone before the batch ends
    /// (<see cref="BatchEndReason.RecordsKeptChanging"/>).
    /// </summary>
    internal const int ChangedChecksLimit = 3;

    private readonly IPendingRebootService _signals;
    private readonly IRemovableReverifier _reverifier;
    private readonly IRecordsWatchFactory _watches;
    private readonly Func<TimeSpan, CancellationToken, Task> _delay;

    public BatchCheck(
        IPendingRebootService signals, IRemovableReverifier reverifier, IRecordsWatchFactory watches)
        : this(signals, reverifier, watches, null) { }

    /// <summary>Test constructor.</summary>
    /// <param name="delay">
    /// What a turn of the wait is made through. Null is the app's own delay. The bound is
    /// counted in turns made, so a test whose delay returns at once reaches the bound
    /// without waiting for it.
    /// </param>
    internal BatchCheck(
        IPendingRebootService signals, IRemovableReverifier reverifier, IRecordsWatchFactory watches,
        Func<TimeSpan, CancellationToken, Task>? delay)
    {
        _signals = signals;
        _reverifier = reverifier;
        _watches = watches;
        _delay = delay ?? FrameworkTask.Delay;
    }

    public async Task<BatchCheckResult> RunAsync(
        IReadOnlyList<string> filePaths,
        TimeSpan? waitBound,
        IProgress<BatchCheckProgress>? progress = null,
        CancellationToken cancellationToken = default)
    {
        var counts = default(BatchCheckCounts);
        var waitClock = new Stopwatch();
        var checkClock = new Stopwatch();
        BatchCheckResult Ended(BatchEndReason reason, PendingRebootResult? signals = null) =>
            new(null, new BatchEnd(reason, signals), Timed(counts, checkClock, waitClock));

        // How long Windows Installer has been busy without a break: the turns waited, and
        // the length of each check a busy signal threw away. It goes back to nought when a
        // check finds the signals clean at both its ends. The bound is on this sum, so a
        // signal that comes and goes between a clean read and the check after it still
        // reaches the bound.
        var busyFor = TimeSpan.Zero;

        // Checks running that did not stand on the watch alone. A wait, or a check a signal
        // blocked, starts the count again.
        var changedRunning = 0;

        var sourceWaits = progress is null ? null : new SourceWaits(progress);

        // The read of every signal, made once. A busy one is left to step 1, which waits
        // for it.
        cancellationToken.ThrowIfCancellationRequested();
        var atOutset = await Task.Run(_signals.Check, cancellationToken).ConfigureAwait(false);
        if (atOutset.IsBlocked && !atOutset.Reason!.Value.ClearsByWaiting())
            return Ended(BatchEndReason.SignalWaitCannotClear, atOutset);

        while (true)
        {
            // Step 1. The reads are made off the calling thread, as the full check makes
            // its own: the window awaits this call from its dispatcher.
            PendingRebootReason? waitingOn = null;
            while (true)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var busy = await Task.Run(_signals.CheckBusy, cancellationToken).ConfigureAwait(false);
                if (!busy.IsBlocked) break;

                // Block with no reason is unreachable (PendingRebootResult.Block).
                var reason = busy.Reason!.Value;
                if (!reason.ClearsByWaiting()) return Ended(BatchEndReason.SignalWaitCannotClear, busy);
                if (waitBound is { } bound && busyFor >= bound)
                    return Ended(BatchEndReason.WaitBoundReached, busy);

                if (waitingOn != reason)
                {
                    waitingOn = reason;
                    counts = counts.PlusWaitFor(reason);
                    progress?.Report(new BatchCheckProgress(BatchCheckStage.Waiting, Signals: busy));
                }

                waitClock.Start();
                try
                {
                    await _delay(WaitTurn, cancellationToken).ConfigureAwait(false);
                }
                finally
                {
                    waitClock.Stop();
                }
                busyFor += WaitTurn;
                changedRunning = 0;
            }

            // Steps 2 and 3.
            progress?.Report(new BatchCheckProgress(BatchCheckStage.Checking));
            var watch = _watches.Arm();
            var handedOver = false;
            try
            {
                // A watch that could not be armed is known at once, and no check made
                // under it could stand.
                if (watch.Read() == RecordsWatchReading.Unestablished)
                    return Ended(BatchEndReason.WatchUnavailable);

                counts = counts with { ChecksMade = counts.ChecksMade + 1 };
                var checkStarted = checkClock.Elapsed;
                checkClock.Start();
                ReverifyResult reverify;
                PendingRebootResult atEnd;
                RecordsWatchReading reading;
                try
                {
                    reverify = await _reverifier.ReverifyAsync(filePaths, cancellationToken, sourceWaits)
                        .ConfigureAwait(false);
                    atEnd = await Task.Run(_signals.Check, cancellationToken).ConfigureAwait(false);
                    reading = watch.Read();
                }
                finally
                {
                    checkClock.Stop();
                }

                if (reading == RecordsWatchReading.Unestablished)
                    return Ended(BatchEndReason.WatchUnavailable);

                // The check's own read first, since it is the earlier of the two. A full
                // check built without a signals service carries no read, which only a
                // test's is, and counts here as a clean one.
                var blocked = reverify.InstallerActivity is { IsBlocked: true } atStart ? atStart
                    : atEnd.IsBlocked ? atEnd
                    : null;
                if (blocked is not null)
                {
                    counts = counts with { ChecksThatDidNotStand = counts.ChecksThatDidNotStand + 1 };
                    if (!blocked.Reason!.Value.ClearsByWaiting())
                        return Ended(BatchEndReason.SignalWaitCannotClear, blocked);
                    busyFor += checkClock.Elapsed - checkStarted;
                    if (waitBound is { } bound && busyFor >= bound)
                        return Ended(BatchEndReason.WaitBoundReached, blocked);
                    changedRunning = 0;
                    continue;
                }

                busyFor = TimeSpan.Zero;
                if (reading == RecordsWatchReading.Changed)
                {
                    counts = counts with { ChecksThatDidNotStand = counts.ChecksThatDidNotStand + 1 };
                    if (++changedRunning >= ChangedChecksLimit) return Ended(BatchEndReason.RecordsKeptChanging);
                    continue;
                }

                handedOver = true;
                return new BatchCheckResult(
                    new StandingCheck(reverify, watch, waitBound), null, Timed(counts, checkClock, waitClock));
            }
            finally
            {
                if (!handedOver) watch.Dispose();
            }
        }
    }

    private static BatchCheckCounts Timed(BatchCheckCounts counts, Stopwatch checks, Stopwatch waits) =>
        counts with { CheckMilliseconds = checks.ElapsedMilliseconds, WaitMilliseconds = waits.ElapsedMilliseconds };

    /// <summary>
    /// Passes the full check's own reports on as they come, on the thread that makes
    /// them, so a wait's line and the line ending it stay in their order.
    /// </summary>
    private sealed class SourceWaits(IProgress<BatchCheckProgress> to) : IProgress<ScanProgressUpdate>
    {
        public void Report(ScanProgressUpdate value) =>
            to.Report(new BatchCheckProgress(BatchCheckStage.SourceWait, SourceWait: value));
    }
}
