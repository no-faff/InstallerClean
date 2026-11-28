using InstallerClean.Models;
using InstallerClean.Services;
using InstallerClean.Tests.Helpers;
using NSubstitute;

namespace InstallerClean.Tests.Services;

/// <summary>
/// <see cref="BatchCheck"/>: when a check stands, what throws one away, how long it waits
/// for a busy Windows Installer, and each way it ends with no check standing. The full
/// check, the signals and the watch are fakes, and the delay returns at once, so nothing
/// here waits.
/// </summary>
public class BatchCheckTests
{
    private static readonly string[] Files = [@"C:\Windows\Installer\a.msi", @"C:\Windows\Installer\b.msp"];

    private static readonly PendingRebootResult MutexHeld =
        PendingRebootResult.Block(PendingRebootReason.MsiExecuteMutexHeld);

    private static readonly PendingRebootResult ServiceRunning =
        PendingRebootResult.Block(PendingRebootReason.InstallerServiceRunning);

    private static readonly PendingRebootResult QueuedRename =
        PendingRebootResult.Block(PendingRebootReason.PendingRenameInCache, "x");

    private readonly ScriptedSignals _signals = new();
    private readonly IRemovableReverifier _reverifier = Substitute.For<IRemovableReverifier>();
    private readonly FakeRecordsWatchFactory _watches = new();
    private readonly List<TimeSpan> _delays = [];
    private readonly List<BatchCheckProgress> _reports = [];

    /// <summary>The full check confirms every file it is given, unless a test says otherwise.</summary>
    public BatchCheckTests() =>
        _reverifier.ReverifyAsync(
                Arg.Any<IReadOnlyList<string>>(), Arg.Any<CancellationToken>(),
                Arg.Any<IProgress<ScanProgressUpdate>>())
            .Returns(call => Task.FromResult(Confirming(call.Arg<IReadOnlyList<string>>())));

    private static ReverifyResult Confirming(IReadOnlyList<string> files, PendingRebootResult? atItsStart = null) =>
        new(files, Array.Empty<string>(), InstallerActivity: atItsStart ?? PendingRebootResult.Clean);

    private BatchCheck Sut(IRecordsWatchFactory? watches = null) =>
        new(_signals, _reverifier, watches ?? _watches, (turn, token) =>
        {
            token.ThrowIfCancellationRequested();
            _delays.Add(turn);
            return Task.CompletedTask;
        });

    private Task<BatchCheckResult> Run(
        BatchCheck sut, TimeSpan? bound = null, CancellationToken token = default) =>
        sut.RunAsync(Files, bound, new SyncProgress<BatchCheckProgress>(_reports.Add), token);

    private int ChecksMade => _reverifier.ReceivedCalls().Count();

    [Fact]
    public async Task A_check_made_on_a_quiet_machine_stands_and_hands_back_its_watch_still_armed()
    {
        var result = await Run(Sut());

        Assert.Null(result.Ended);
        Assert.NotNull(result.Stood);
        Assert.Equal(Files, result.Stood!.Files);
        Assert.Null(result.Stood.WaitBound);
        var watch = Assert.Single(_watches.Armed);
        Assert.False(watch.Disposed);
        Assert.Equal(1, result.Counts.ChecksMade);
        Assert.Equal(0, result.Counts.ChecksThatDidNotStand);
        Assert.Empty(_delays);
    }

    [Fact]
    public async Task The_standing_check_carries_the_bound_it_was_made_under()
    {
        var bound = TimeSpan.FromMinutes(30);

        var result = await Run(Sut(), bound);

        Assert.Equal(bound, result.Stood!.WaitBound);
    }

    [Fact]
    public async Task Disposing_of_the_result_releases_the_watch_and_is_safe_twice()
    {
        var result = await Run(Sut());

        result.Dispose();
        result.Dispose();
        result.Stood!.Dispose();

        Assert.Equal(1, _watches.Armed[0].Disposals);
    }

    [Fact]
    public async Task The_watch_is_armed_before_the_check_and_read_after_the_second_signals_read()
    {
        var order = new List<string>();
        var signals = Substitute.For<IPendingRebootService>();
        signals.Check().Returns(_ =>
        {
            order.Add("every signal");
            return PendingRebootResult.Clean;
        });
        signals.CheckBusy().Returns(_ =>
        {
            order.Add("busy signals");
            return PendingRebootResult.Clean;
        });
        var watches = new FakeRecordsWatchFactory(_ =>
        {
            order.Add("arm");
            return new FakeRecordsWatch { OnRead = (_, _) => order.Add("read the watch") };
        });
        _reverifier.ReverifyAsync(
                Arg.Any<IReadOnlyList<string>>(), Arg.Any<CancellationToken>(),
                Arg.Any<IProgress<ScanProgressUpdate>>())
            .Returns(call =>
            {
                order.Add("check");
                return Task.FromResult(Confirming(call.Arg<IReadOnlyList<string>>()));
            });
        var sut = new BatchCheck(signals, _reverifier, watches, (_, _) => Task.CompletedTask);

        var result = await sut.RunAsync(Files, null);

        Assert.NotNull(result.Stood);
        Assert.Equal(
            ["every signal", "busy signals", "arm", "read the watch", "check", "every signal", "read the watch"],
            order);
    }

    [Fact]
    public async Task A_reason_waiting_does_not_change_ends_it_at_the_first_read_with_no_check_made()
    {
        _signals.All(QueuedRename);

        var result = await Run(Sut());

        Assert.Null(result.Stood);
        Assert.Equal(BatchEndReason.SignalWaitCannotClear, result.Ended!.Reason);
        Assert.Same(QueuedRename, result.Ended.Signals);
        Assert.Equal(0, ChecksMade);
        Assert.Empty(_watches.Armed);
        Assert.Equal(0, _signals.BusyReads);
    }

    [Fact]
    public async Task A_busy_reason_at_the_first_read_is_left_to_the_wait()
    {
        _signals.All(MutexHeld);
        _signals.Busy(MutexHeld, times: 2);

        var result = await Run(Sut());

        Assert.NotNull(result.Stood);
        Assert.Equal(2, _delays.Count);
    }

    [Fact]
    public async Task It_waits_half_a_second_a_turn_while_Windows_Installer_is_busy_and_checks_when_it_is_not()
    {
        _signals.Busy(MutexHeld, times: 3);

        var result = await Run(Sut());

        Assert.NotNull(result.Stood);
        Assert.Equal(3, _delays.Count);
        Assert.All(_delays, turn => Assert.Equal(TimeSpan.FromMilliseconds(500), turn));
        Assert.Equal(1, ChecksMade);
        Assert.Equal(1, result.Counts.WaitsForMutex);
    }

    [Fact]
    public async Task A_wait_is_reported_once_when_it_begins_and_again_only_where_its_reason_changes()
    {
        _signals.Busy(MutexHeld, times: 3).Busy(ServiceRunning, times: 2);

        var result = await Run(Sut());

        Assert.Equal(
            [BatchCheckStage.Waiting, BatchCheckStage.Waiting, BatchCheckStage.Checking],
            _reports.Select(r => r.Stage));
        Assert.Same(MutexHeld, _reports[0].Signals);
        Assert.Same(ServiceRunning, _reports[1].Signals);
        Assert.Equal(1, result.Counts.WaitsForMutex);
        Assert.Equal(1, result.Counts.WaitsForService);
    }

    [Fact]
    public async Task A_busy_Windows_Installer_ends_it_at_the_bound_with_the_last_read_and_no_check_made()
    {
        _signals.Busy(ServiceRunning, times: 100);

        var result = await Run(Sut(), bound: TimeSpan.FromSeconds(2));

        Assert.Equal(BatchEndReason.WaitBoundReached, result.Ended!.Reason);
        Assert.Same(ServiceRunning, result.Ended.Signals);
        Assert.Equal(4, _delays.Count);
        Assert.Equal(0, ChecksMade);
        Assert.Empty(_watches.Armed);
    }

    [Fact]
    public async Task With_no_bound_it_goes_on_waiting()
    {
        _signals.Busy(ServiceRunning, times: 5000);

        var result = await Run(Sut(), bound: null);

        Assert.NotNull(result.Stood);
        Assert.Equal(5000, _delays.Count);
    }

    [Fact]
    public async Task A_busy_read_that_waiting_does_not_change_ends_it_without_waiting()
    {
        var unreadable = PendingRebootResult.Block(PendingRebootReason.InstallerServiceUnreadable, windowsError: 5);
        _signals.Busy(unreadable);

        var result = await Run(Sut());

        Assert.Equal(BatchEndReason.SignalWaitCannotClear, result.Ended!.Reason);
        Assert.Same(unreadable, result.Ended.Signals);
        Assert.Empty(_delays);
        Assert.Equal(0, ChecksMade);
    }

    [Fact]
    public async Task A_watch_that_could_not_be_armed_ends_it_before_the_check_and_is_released()
    {
        var watches = new FakeRecordsWatchFactory(
            _ => new FakeRecordsWatch { Reading = RecordsWatchReading.Unestablished });

        var result = await Run(Sut(watches));

        Assert.Equal(BatchEndReason.WatchUnavailable, result.Ended!.Reason);
        Assert.Null(result.Ended.Signals);
        Assert.Equal(0, ChecksMade);
        Assert.True(Assert.Single(watches.Armed).Disposed);
    }

    /// <summary>
    /// The read at the start of a call takes in the queued file operations and the read
    /// before each check does not. Here the operation is queued after the first read, so
    /// only the check's own read sees it, and every busy read after it is clean.
    /// </summary>
    [Fact]
    public async Task A_check_blocked_by_a_reason_waiting_does_not_change_ends_on_its_own_read_and_is_not_made_again()
    {
        _reverifier.ReverifyAsync(
                Arg.Any<IReadOnlyList<string>>(), Arg.Any<CancellationToken>(),
                Arg.Any<IProgress<ScanProgressUpdate>>())
            .Returns(call => Task.FromResult(Confirming(call.Arg<IReadOnlyList<string>>(), QueuedRename)));

        var result = await Run(Sut());

        Assert.Equal(BatchEndReason.SignalWaitCannotClear, result.Ended!.Reason);
        Assert.Same(QueuedRename, result.Ended.Signals);
        Assert.Equal(1, ChecksMade);
        Assert.True(Assert.Single(_watches.Armed).Disposed);
        Assert.Equal(1, result.Counts.ChecksThatDidNotStand);
    }

    [Fact]
    public async Task A_check_whose_second_signals_read_is_blocked_by_that_kind_of_reason_ends_on_it_too()
    {
        // The first is the read at the start of the call, the second the one after the check.
        _signals.All(PendingRebootResult.Clean).All(QueuedRename);

        var result = await Run(Sut());

        Assert.Equal(BatchEndReason.SignalWaitCannotClear, result.Ended!.Reason);
        Assert.Same(QueuedRename, result.Ended.Signals);
        Assert.Equal(1, ChecksMade);
    }

    [Fact]
    public async Task A_check_made_while_a_busy_signal_came_up_is_thrown_away_with_its_watch_and_made_again()
    {
        _signals.All(PendingRebootResult.Clean).All(MutexHeld);

        var result = await Run(Sut());

        Assert.NotNull(result.Stood);
        Assert.Equal(2, ChecksMade);
        Assert.Equal(2, _watches.Armed.Count);
        Assert.True(_watches.Armed[0].Disposed);
        Assert.False(_watches.Armed[1].Disposed);
        Assert.Equal(2, result.Counts.ChecksMade);
        Assert.Equal(1, result.Counts.ChecksThatDidNotStand);
    }

    [Fact]
    public async Task A_check_whose_own_first_read_found_a_busy_signal_is_thrown_away_and_made_again()
    {
        var first = true;
        _reverifier.ReverifyAsync(
                Arg.Any<IReadOnlyList<string>>(), Arg.Any<CancellationToken>(),
                Arg.Any<IProgress<ScanProgressUpdate>>())
            .Returns(call =>
            {
                var atItsStart = first ? MutexHeld : PendingRebootResult.Clean;
                first = false;
                return Task.FromResult(Confirming(call.Arg<IReadOnlyList<string>>(), atItsStart));
            });

        var result = await Run(Sut());

        Assert.NotNull(result.Stood);
        Assert.Equal(2, ChecksMade);
        Assert.True(_watches.Armed[0].Disposed);
    }

    [Fact]
    public async Task A_check_a_busy_signal_threw_away_counts_towards_the_bound()
    {
        _signals.All(PendingRebootResult.Clean).All(MutexHeld);

        var result = await Run(Sut(), bound: TimeSpan.Zero);

        Assert.Equal(BatchEndReason.WaitBoundReached, result.Ended!.Reason);
        Assert.Same(MutexHeld, result.Ended.Signals);
        Assert.Equal(1, ChecksMade);
    }

    [Fact]
    public async Task A_check_the_records_changed_under_is_thrown_away_and_made_again()
    {
        var watches = new FakeRecordsWatchFactory(armed => new FakeRecordsWatch
        {
            // The first read is the one made as the watch is armed, the second the one
            // after the check.
            OnRead = (watch, read) =>
            {
                if (armed == 0 && read == 2) watch.Reading = RecordsWatchReading.Changed;
            },
        });

        var result = await Run(Sut(watches));

        Assert.NotNull(result.Stood);
        Assert.Equal(2, ChecksMade);
        Assert.True(watches.Armed[0].Disposed);
        Assert.False(watches.Armed[1].Disposed);
        Assert.Equal(1, result.Counts.ChecksThatDidNotStand);
    }

    [Fact]
    public async Task Three_checks_running_that_the_records_changed_under_end_it()
    {
        var watches = ChangingUnder(_ => true);

        var result = await Run(Sut(watches));

        Assert.Equal(BatchEndReason.RecordsKeptChanging, result.Ended!.Reason);
        Assert.Null(result.Ended.Signals);
        Assert.Equal(3, ChecksMade);
        Assert.All(watches.Armed, watch => Assert.True(watch.Disposed));
        Assert.Equal(3, result.Counts.ChecksThatDidNotStand);
    }

    [Fact]
    public async Task A_wait_between_them_starts_the_count_of_changed_checks_again()
    {
        // Changed, changed, a wait, changed, changed, then a check that stands: four
        // checks the records changed under, and never three running without a wait.
        var watches = ChangingUnder(armed => armed < 4);
        _signals.BusyClean(times: 2).Busy(MutexHeld);

        var result = await Run(Sut(watches));

        Assert.NotNull(result.Stood);
        Assert.Equal(5, ChecksMade);
        Assert.Single(_delays);
    }

    [Fact]
    public async Task A_watch_that_reads_as_not_established_after_the_check_ends_it()
    {
        var watches = new FakeRecordsWatchFactory(_ => new FakeRecordsWatch
        {
            OnRead = (watch, read) =>
            {
                if (read == 2) watch.Reading = RecordsWatchReading.Unestablished;
            },
        });

        var result = await Run(Sut(watches));

        Assert.Equal(BatchEndReason.WatchUnavailable, result.Ended!.Reason);
        Assert.True(Assert.Single(watches.Armed).Disposed);
    }

    [Fact]
    public async Task A_check_that_throws_comes_out_as_it_is_with_its_watch_released()
    {
        _reverifier.ReverifyAsync(
                Arg.Any<IReadOnlyList<string>>(), Arg.Any<CancellationToken>(),
                Arg.Any<IProgress<ScanProgressUpdate>>())
            .Returns<Task<ReverifyResult>>(_ => throw new InvalidOperationException("the enumeration failed"));

        var thrown = await Assert.ThrowsAsync<InvalidOperationException>(() => Run(Sut()));

        Assert.Equal("the enumeration failed", thrown.Message);
        Assert.True(Assert.Single(_watches.Armed).Disposed);
    }

    [Fact]
    public async Task A_cancel_during_the_wait_comes_out_as_a_cancellation()
    {
        using var source = new CancellationTokenSource();
        _signals.Busy(MutexHeld, times: 100);
        var sut = new BatchCheck(_signals, _reverifier, _watches, (_, token) =>
        {
            source.Cancel();
            token.ThrowIfCancellationRequested();
            return Task.CompletedTask;
        });

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => Run(sut, token: source.Token));

        Assert.Equal(0, ChecksMade);
        Assert.Empty(_watches.Armed);
    }

    [Fact]
    public async Task A_wait_the_check_makes_on_a_source_folder_is_passed_on_as_it_comes()
    {
        var wait = new ScanProgressUpdate("Waiting for D:\\", IsMilestone: false, IsWait: true);
        _reverifier.ReverifyAsync(
                Arg.Any<IReadOnlyList<string>>(), Arg.Any<CancellationToken>(),
                Arg.Any<IProgress<ScanProgressUpdate>>())
            .Returns(call =>
            {
                call.Arg<IProgress<ScanProgressUpdate>>().Report(wait);
                return Task.FromResult(Confirming(call.Arg<IReadOnlyList<string>>()));
            });

        await Run(Sut());

        Assert.Equal([BatchCheckStage.Checking, BatchCheckStage.SourceWait], _reports.Select(r => r.Stage));
        Assert.Same(wait, _reports[1].SourceWait);
    }

    [Fact]
    public async Task It_checks_the_files_it_was_given_and_no_others()
    {
        await Run(Sut());

        await _reverifier.Received(1).ReverifyAsync(
            Arg.Is<IReadOnlyList<string>>(files => files.SequenceEqual(Files)), Arg.Any<CancellationToken>(),
            Arg.Any<IProgress<ScanProgressUpdate>>());
    }

    /// <summary>
    /// A factory whose watches read as changed at the read after the check, for each
    /// watch <paramref name="changes"/> answers true for by the order it was armed in.
    /// </summary>
    private static FakeRecordsWatchFactory ChangingUnder(Func<int, bool> changes) =>
        new(armed => new FakeRecordsWatch
        {
            OnRead = (watch, read) =>
            {
                if (read == 2 && changes(armed)) watch.Reading = RecordsWatchReading.Changed;
            },
        });
}
