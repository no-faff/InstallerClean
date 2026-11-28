using System.IO.Abstractions.TestingHelpers;
using InstallerClean.Interop.Native;
using InstallerClean.Models;
using InstallerClean.Services;
using InstallerClean.Tests.Helpers;

namespace InstallerClean.Tests.Services;

/// <summary>
/// The steps <see cref="DeleteFilesService"/> makes on each file: the look, the take, the
/// second look and the delete, and what follows a look that is not clean, a file that is
/// in use, a step Windows refuses and a file that will not go back.
///
/// The filesystem is a substitute and the hand acts on it by path
/// (<see cref="ScriptedInHandFolder"/>), so each test follows a file out of its place and
/// back, or away. The two checks the service makes against the real filesystem pass for a
/// path directly under the real Installer folder, which is where every file here is named.
/// </summary>
public class DeleteFilesServiceStepsTests
{
    private const string Dir = @"C:\Windows\Installer";
    private static readonly string A = $@"{Dir}\a.msi";
    private static readonly string B = $@"{Dir}\b.msi";
    private static readonly string C = $@"{Dir}\c.msi";
    private static readonly string InHand = $@"{Dir}\{InHandFolders.FolderName}";

    private readonly MockFileSystem _fs = new();
    private readonly ScriptedInHandFolder _hand;
    private readonly ScriptedSignals _signals = new();
    private readonly ScriptedBatchCheck _checks = new();
    private readonly FakeRecordsWatch _watch = new();
    private readonly List<OperationProgress> _reports = [];

    public DeleteFilesServiceStepsTests()
    {
        foreach (var path in new[] { A, B, C }) _fs.AddFile(path, new MockFileData("payload"));
        _hand = new ScriptedInHandFolder(_fs);
    }

    private Task<DeleteResult> Delete(
        IReadOnlyList<string>? files = null, CancellationToken token = default,
        Func<string, bool>? mayAct = null, TimeSpan? bound = null) =>
        new DeleteFilesService(_fs, _signals, _checks, _hand, installerFolderOverride: null).DeleteFilesAsync(
            StandingChecks.Of(files ?? [A, B, C], _watch, bound),
            new SyncProgress<OperationProgress>(_reports.Add), token, mayAct);

    /// <summary>Makes the batch's first watch read as changed at one read of it, counted from one.</summary>
    private void RecordsChangeAtRead(int read) =>
        _watch.OnRead = (watch, n) =>
        {
            if (n == read) watch.Reading = RecordsWatchReading.Changed;
        };

    [Fact]
    public async Task Each_file_is_held_taken_and_deleted_between_two_looks()
    {
        var result = await Delete();

        Assert.Equal(3, result.DeletedCount);
        Assert.Empty(result.Errors);
        Assert.Null(result.Ended);
        Assert.Equal(
            ["hold a.msi", "take a.msi", "delete a.msi", "hold b.msi", "take b.msi", "delete b.msi",
             "hold c.msi", "take c.msi", "delete c.msi"],
            _hand.Steps);
        Assert.Equal(6, _watch.Reads);
        Assert.Equal(6, _signals.BusyReads);
        Assert.Empty(_checks.Calls);
        Assert.All(new[] { A, B, C }, path => Assert.False(_fs.File.Exists(path)));
    }

    [Fact]
    public async Task The_second_look_is_made_with_the_file_out_of_its_place()
    {
        var inPlaceAtEachLook = new List<bool>();
        var signals = NSubstitute.Substitute.For<IPendingRebootService>();
        NSubstitute.SubstituteExtensions.Returns(signals.CheckBusy(), _ =>
        {
            inPlaceAtEachLook.Add(_fs.File.Exists(A));
            return PendingRebootResult.Clean;
        });

        var result = await new DeleteFilesService(_fs, signals, _checks, _hand, null)
            .DeleteFilesAsync(StandingChecks.Of([A], _watch));

        Assert.Equal(1, result.DeletedCount);
        Assert.Equal([true, false], inPlaceAtEachLook);
    }

    [Fact]
    public async Task The_batchs_folder_is_gone_when_the_batch_ends_and_the_watch_is_released()
    {
        await Delete();

        Assert.False(_fs.Directory.Exists(InHand));
        Assert.True(_watch.Disposed);
    }

    [Fact]
    public async Task A_first_look_that_finds_the_records_changed_checks_every_file_again_before_any_is_touched()
    {
        RecordsChangeAtRead(1);

        var result = await Delete();

        Assert.Equal(3, result.DeletedCount);
        Assert.Equal([A, B, C], Assert.Single(_checks.Calls));
        Assert.Equal("hold a.msi", _hand.Steps[0]);
        Assert.Equal(1, _hand.Steps.Count(step => step == "hold a.msi"));
        Assert.True(_watch.Disposed);
        Assert.True(Assert.Single(_checks.Watches).Disposed);
    }

    [Fact]
    public async Task A_first_look_that_finds_Windows_Installer_busy_does_the_same()
    {
        _signals.Busy(PendingRebootResult.Block(PendingRebootReason.InstallerServiceRunning));

        var result = await Delete();

        Assert.Equal(3, result.DeletedCount);
        Assert.Single(_checks.Calls);
    }

    [Fact]
    public async Task A_second_look_that_is_not_clean_puts_the_file_back_checks_again_and_starts_the_file_again()
    {
        RecordsChangeAtRead(2);

        var result = await Delete();

        Assert.Equal(3, result.DeletedCount);
        Assert.Empty(result.Errors);
        Assert.Equal(
            ["hold a.msi", "take a.msi", "put-back a.msi", "hold a.msi", "take a.msi", "delete a.msi"],
            _hand.Steps.Take(6));
        Assert.Equal([A, B, C], Assert.Single(_checks.Calls));
    }

    [Fact]
    public async Task A_later_check_is_asked_only_about_the_files_not_yet_done()
    {
        // The third read of the watch is the first look at the second file.
        RecordsChangeAtRead(3);

        await Delete();

        Assert.Equal([B, C], Assert.Single(_checks.Calls));
    }

    [Fact]
    public async Task A_later_check_is_made_under_the_bound_the_first_was()
    {
        RecordsChangeAtRead(1);

        await Delete(bound: TimeSpan.FromMinutes(30));

        Assert.Equal(TimeSpan.FromMinutes(30), Assert.Single(_checks.Bounds));
    }

    [Fact]
    public async Task A_file_a_later_check_drops_while_it_was_in_hand_is_back_in_its_place_and_held_back()
    {
        RecordsChangeAtRead(2);
        _checks.ThenStands(A);

        var result = await Delete();

        Assert.Equal(2, result.DeletedCount);
        Assert.Empty(result.Errors);
        Assert.Equal([A], result.HeldBack);
        Assert.Equal(1, result.HeldBackReasons.Reclaimed);
        Assert.True(_fs.File.Exists(A));
        Assert.Equal(["hold a.msi", "take a.msi", "put-back a.msi", "hold b.msi"], _hand.Steps.Take(4));
    }

    [Fact]
    public async Task A_file_a_later_check_drops_before_it_was_reached_is_never_touched()
    {
        RecordsChangeAtRead(1);
        _checks.ThenStands(C);

        var result = await Delete();

        Assert.Equal(2, result.DeletedCount);
        Assert.Equal([C], result.HeldBack);
        Assert.True(_fs.File.Exists(C));
        Assert.DoesNotContain("hold c.msi", _hand.Steps);
    }

    [Fact]
    public async Task The_count_reported_is_of_the_files_reached_among_those_still_in_the_batch()
    {
        RecordsChangeAtRead(2);
        _checks.ThenStands(A);

        await Delete();

        var files = _reports.Where(report => report.Check is null).ToList();
        Assert.Equal(["a.msi", "b.msi", "c.msi"], files.Select(report => report.CurrentFileName));
        Assert.Equal([(1, 3), (1, 2), (2, 2)], files.Select(report => (report.CurrentFile, report.TotalFiles)));
    }

    [Fact]
    public async Task What_was_deleted_what_failed_and_what_was_held_back_add_up_to_the_batch()
    {
        RecordsChangeAtRead(1);
        _checks.ThenStands(C);
        _hand.Refuse("delete", B, Kernel32.ERROR_ACCESS_DENIED);

        var result = await Delete();

        Assert.Equal(1, result.DeletedCount);
        Assert.Single(result.Errors);
        Assert.Single(result.HeldBack);
        Assert.Equal(3, result.DeletedCount + result.Errors.Count + result.HeldBack.Count);
    }

    [Fact]
    public async Task A_later_check_that_ends_the_batch_leaves_the_file_in_hand_back_in_its_place_and_the_rest_untouched()
    {
        // The fourth read of the watch is the second look at the second file.
        RecordsChangeAtRead(4);
        _checks.ThenEnds(BatchEndReason.RecordsKeptChanging);

        var result = await Delete();

        Assert.Equal(BatchEndReason.RecordsKeptChanging, result.Ended!.Reason);
        Assert.False(result.Cancelled);
        Assert.Equal(1, result.DeletedCount);
        Assert.Empty(result.Errors);
        Assert.False(_fs.File.Exists(A));
        Assert.True(_fs.File.Exists(B));
        Assert.True(_fs.File.Exists(C));
        Assert.Equal("put-back b.msi", _hand.Steps[^1]);
        Assert.False(_fs.Directory.Exists(InHand));
    }

    [Fact]
    public async Task The_read_that_ended_a_later_check_comes_back_on_the_result()
    {
        var blocked = PendingRebootResult.Block(PendingRebootReason.PendingRenameInCache, "x");
        RecordsChangeAtRead(1);
        _checks.ThenEnds(BatchEndReason.SignalWaitCannotClear, blocked);

        var result = await Delete();

        Assert.Equal(BatchEndReason.SignalWaitCannotClear, result.Ended!.Reason);
        Assert.Same(blocked, result.Ended.Signals);
        Assert.Equal(0, result.DeletedCount);
        Assert.Empty(_hand.Steps);
    }

    [Fact]
    public async Task A_later_check_that_throws_ends_the_batch_with_what_was_done_and_the_file_back_in_its_place()
    {
        var failure = new InvalidOperationException("the enumeration failed");
        RecordsChangeAtRead(4);
        _checks.ThenThrows(failure);

        var result = await Delete();

        Assert.Equal(BatchEndReason.CheckFailed, result.Ended!.Reason);
        Assert.Same(failure, result.Ended.Failure);
        Assert.Equal(1, result.DeletedCount);
        Assert.True(_fs.File.Exists(B));
    }

    [Fact]
    public async Task A_look_that_throws_with_a_file_in_hand_puts_it_back_and_ends_the_batch()
    {
        var failure = new InvalidOperationException("the in-progress file would not read");
        _signals.BusyClean().BusyThrows(failure);

        var result = await Delete();

        Assert.Equal(BatchEndReason.CheckFailed, result.Ended!.Reason);
        Assert.Same(failure, result.Ended.Failure);
        Assert.Equal(0, result.DeletedCount);
        Assert.Empty(result.Errors);
        Assert.Equal(["hold a.msi", "take a.msi", "put-back a.msi"], _hand.Steps);
        Assert.True(_fs.File.Exists(A));
        Assert.Empty(_checks.Calls);
    }

    [Fact]
    public async Task A_cancel_during_a_later_check_stops_the_batch_with_the_file_back_in_its_place()
    {
        using var source = new CancellationTokenSource();
        RecordsChangeAtRead(2);
        _checks.Then(_ =>
        {
            source.Cancel();
            throw new OperationCanceledException(source.Token);
        });

        var result = await Delete(token: source.Token);

        Assert.True(result.Cancelled);
        Assert.Null(result.Ended);
        Assert.Equal(0, result.DeletedCount);
        Assert.True(_fs.File.Exists(A));
    }

    [Fact]
    public async Task What_a_later_check_reports_reaches_the_batchs_progress_with_the_count_as_it_stands()
    {
        var waiting = new BatchCheckProgress(
            BatchCheckStage.Waiting, PendingRebootResult.Block(PendingRebootReason.InstallerServiceRunning));
        RecordsChangeAtRead(3);
        _checks.Reports = [waiting, new BatchCheckProgress(BatchCheckStage.Checking)];

        await Delete();

        var stages = _reports.Where(report => report.Check is not null).ToList();
        Assert.Equal([BatchCheckStage.Waiting, BatchCheckStage.Checking], stages.Select(s => s.Check!.Stage));
        Assert.Same(waiting, stages[0].Check);
        Assert.All(stages, stage => Assert.Equal((2, 3, string.Empty), (stage.CurrentFile, stage.TotalFiles, stage.CurrentFileName)));
    }

    [Fact]
    public async Task What_a_later_check_gave_up_and_counted_comes_back_on_the_result()
    {
        var root = new SourceRootGivenUp(@"D:\", SourceRootGiveUpRoute.StoppedWaiting, 1);
        RecordsChangeAtRead(1);
        _checks.ThenStandsWith(files => new ReverifyResult(
            files.Skip(1).ToList(), [files[0]], new HeldBackReasons(FileNotConfirmed: 1), [root], SourceWaitCount: 2));

        var result = await Delete();

        Assert.Equal([root], result.SourceRootsGivenUp);
        Assert.Equal(2, result.SourceWaitCount);
        Assert.Equal(1, result.HeldBackReasons.FileNotConfirmed);
        Assert.Equal([A], result.HeldBack);
    }

    [Fact]
    public async Task A_file_in_use_is_tried_once_more_after_the_last_file()
    {
        _hand.InUse(A, times: 1);

        var result = await Delete();

        Assert.Equal(3, result.DeletedCount);
        Assert.Empty(result.Errors);
        Assert.Equal("hold a.msi", _hand.Steps[0]);
        Assert.Equal("hold b.msi", _hand.Steps[1]);
        Assert.Equal(["hold a.msi", "take a.msi", "delete a.msi"], _hand.Steps.Skip(_hand.Steps.Count - 3));
    }

    [Fact]
    public async Task The_second_try_waits_out_a_second_since_the_file_was_refused()
    {
        _hand.InUse(A, times: 1);

        var pauses = await PausesDuring(() => Delete([A]));

        var pause = Assert.Single(pauses);
        Assert.InRange(pause, TimeSpan.FromMilliseconds(500), BatchRun.PauseBeforeTheSecondTry);
    }

    [Fact]
    public async Task A_batch_with_no_file_in_use_and_no_put_back_waits_for_nothing()
    {
        var pauses = await PausesDuring(() => Delete());

        Assert.Empty(pauses);
    }

    [Fact]
    public async Task A_check_made_after_a_put_back_waits_for_the_file_times_clock_to_move_on_first()
    {
        RecordsChangeAtRead(2);

        var pauses = await PausesDuring(() => Delete([A]));

        var pause = Assert.Single(pauses);
        Assert.InRange(pause, TimeSpan.FromMilliseconds(20), InHandPause.AfterAPutBack);
        Assert.Single(_checks.Calls);
    }

    [Fact]
    public async Task A_check_made_with_no_file_put_back_does_not_wait()
    {
        RecordsChangeAtRead(1);

        var pauses = await PausesDuring(() => Delete([A]));

        Assert.Empty(pauses);
        Assert.Single(_checks.Calls);
    }

    /// <summary>
    /// Runs <paramref name="batch"/> with the run's pauses recorded and not waited out,
    /// and puts the assembly's own pause back. The assembly runs its tests one at a time.
    /// </summary>
    private static async Task<List<TimeSpan>> PausesDuring(Func<Task> batch)
    {
        var pauses = new List<TimeSpan>();
        var before = InHandPause.ForTests;
        InHandPause.ForTests = (length, _) =>
        {
            pauses.Add(length);
            return Task.CompletedTask;
        };
        try
        {
            await batch();
        }
        finally
        {
            InHandPause.ForTests = before;
        }

        return pauses;
    }

    [Fact]
    public async Task A_file_still_in_use_at_that_second_try_is_filed_as_in_use_and_stays()
    {
        _hand.InUse(A, times: 2);

        var result = await Delete();

        Assert.Equal(2, result.DeletedCount);
        var error = Assert.IsType<FileInUse>(Assert.Single(result.Errors));
        Assert.Equal(A, error.FilePath);
        Assert.True(_fs.File.Exists(A));
        Assert.Equal(2, _hand.Steps.Count(step => step == "hold a.msi"));
    }

    [Fact]
    public async Task A_sharing_violation_at_the_rename_out_of_its_place_is_in_use_too()
    {
        _hand.Refuse("take", A, Kernel32.ERROR_SHARING_VIOLATION);

        var result = await Delete();

        Assert.IsType<FileInUse>(Assert.Single(result.Errors));
        Assert.Equal(2, _hand.Steps.Count(step => step == "take a.msi"));
        Assert.True(_fs.File.Exists(A));
    }

    [Fact]
    public async Task Any_other_refusal_of_that_rename_is_filed_at_once_by_its_error()
    {
        _hand.Refuse("take", A, Kernel32.ERROR_ACCESS_DENIED);

        var result = await Delete();

        Assert.IsType<AccessDenied>(Assert.Single(result.Errors));
        Assert.Equal(1, _hand.Steps.Count(step => step == "take a.msi"));
        Assert.Equal(2, result.DeletedCount);
    }

    [Theory]
    [InlineData(Kernel32.ERROR_ACCESS_DENIED, typeof(AccessDenied))]
    [InlineData(Kernel32.ERROR_SHARING_VIOLATION, typeof(FileInUse))]
    [InlineData(Kernel32.ERROR_LOCK_VIOLATION, typeof(FileInUse))]
    [InlineData(Kernel32.ERROR_FILE_NOT_FOUND, typeof(MissingSourceFile))]
    [InlineData(1117, typeof(IOFailure))]
    public async Task A_refused_delete_puts_the_file_back_and_files_it_by_its_error(int windowsError, Type expected)
    {
        _hand.Refuse("delete", A, windowsError);

        var result = await Delete();

        Assert.IsType(expected, Assert.Single(result.Errors));
        Assert.True(_fs.File.Exists(A));
        Assert.Equal(["hold a.msi", "take a.msi", "delete a.msi", "put-back a.msi"], _hand.Steps.Take(4));
        Assert.Equal(2, result.DeletedCount);
        Assert.False(_fs.Directory.Exists(InHand));
    }

    [Fact]
    public async Task A_file_that_will_not_go_back_is_filed_with_the_folder_it_is_in_and_the_batch_goes_on()
    {
        RecordsChangeAtRead(2);
        _hand.Refuse("put-back", A, Kernel32.ERROR_ALREADY_EXISTS);

        var result = await Delete();

        var error = Assert.IsType<FileLeftInHand>(Assert.Single(result.Errors));
        Assert.Equal(A, error.FilePath);
        Assert.Equal(_hand.BatchFolder, error.InHandFolder);
        Assert.Contains(_hand.BatchFolder!, error.LocalisedMessage);
        Assert.True(_fs.File.Exists(_fs.Path.Combine(_hand.BatchFolder!, "a.msi")));
        Assert.False(_fs.File.Exists(A));
        Assert.Equal(2, result.DeletedCount);
        Assert.Equal([B, C], Assert.Single(_checks.Calls));
        Assert.True(_fs.Directory.Exists(_hand.BatchFolder!));
    }

    [Fact]
    public async Task A_file_that_will_not_go_back_after_a_refused_delete_is_filed_once()
    {
        _hand.Refuse("delete", A, Kernel32.ERROR_ACCESS_DENIED).Refuse("put-back", A, Kernel32.ERROR_ACCESS_DENIED);

        var result = await Delete();

        Assert.IsType<FileLeftInHand>(Assert.Single(result.Errors));
        Assert.Equal(2, result.DeletedCount);
    }

    [Fact]
    public async Task The_host_is_asked_before_the_file_is_opened_and_not_again_until_the_next_file()
    {
        var order = new List<string>();
        _hand.OnStep = (step, _) => order.Add(step);

        await Delete([A, B], mayAct: path =>
        {
            order.Add($"ask {_fs.Path.GetFileName(path)}");
            return true;
        });

        Assert.Equal(
            ["ask a.msi", "ask a.msi", "hold", "take", "delete", "ask b.msi", "ask b.msi", "hold", "take", "delete"],
            order);
    }

    [Fact]
    public async Task A_batch_whose_folder_cannot_be_made_ends_with_nothing_touched()
    {
        _hand.OpenBatchError = Kernel32.ERROR_DIRECTORY;

        var result = await Delete();

        Assert.Equal(BatchEndReason.InHandFolderUnavailable, result.Ended!.Reason);
        Assert.Equal(Kernel32.ERROR_DIRECTORY, result.Ended.WindowsError);
        Assert.Equal(0, result.DeletedCount);
        Assert.Empty(result.Errors);
        Assert.Empty(_reports);
        Assert.All(new[] { A, B, C }, path => Assert.True(_fs.File.Exists(path)));
        Assert.True(_watch.Disposed);
    }

    [Fact]
    public async Task A_file_in_use_whose_second_try_never_came_is_filed_as_in_use()
    {
        _hand.InUse(A, times: 1);

        var result = await Delete(mayAct: path => path != C);

        Assert.True(result.Cancelled);
        Assert.Equal(1, result.DeletedCount);
        var error = Assert.IsType<FileInUse>(Assert.Single(result.Errors));
        Assert.Equal(A, error.FilePath);
        Assert.True(_fs.File.Exists(A));
        Assert.True(_fs.File.Exists(C));
    }

    // The counts the batch keeps for the opt-in report (BatchRunCounts).

    [Fact]
    public async Task A_batch_with_every_look_clean_counts_nothing()
    {
        var result = await Delete();

        Assert.Equal(default, result.Counts);
    }

    [Theory]
    [InlineData(PendingRebootReason.MsiExecuteMutexHeld, "mutex")]
    [InlineData(PendingRebootReason.InstallerInProgress, "key")]
    [InlineData(PendingRebootReason.InstallerInProgressMarker, "file")]
    [InlineData(PendingRebootReason.InstallerServiceRunning, "service")]
    [InlineData(PendingRebootReason.MsiExecuteMutexAccessRefused, "other")]
    [InlineData(PendingRebootReason.InstallerServiceUnreadable, "other")]
    public async Task A_settle_is_counted_under_the_signal_its_look_found(PendingRebootReason reason, string cause)
    {
        _signals.Busy(PendingRebootResult.Block(reason));

        var result = await Delete();

        var expected = cause switch
        {
            "mutex" => new BatchRunCounts(Settles: 1, SettlesForMutex: 1),
            "key" => new BatchRunCounts(Settles: 1, SettlesForInProgressKey: 1),
            "file" => new BatchRunCounts(Settles: 1, SettlesForInProgressFile: 1),
            "service" => new BatchRunCounts(Settles: 1, SettlesForService: 1),
            _ => new BatchRunCounts(Settles: 1, SettlesForOtherSignal: 1),
        };
        Assert.Equal(expected, result.Counts);
    }

    [Fact]
    public async Task A_settle_whose_look_found_the_watch_not_established_is_counted_as_that()
    {
        _watch.OnRead = (watch, n) =>
        {
            if (n == 1) watch.Reading = RecordsWatchReading.Unestablished;
        };

        var result = await Delete();

        Assert.Equal(new BatchRunCounts(Settles: 1, SettlesForWatchUnestablished: 1), result.Counts);
    }

    [Fact]
    public async Task A_change_of_the_records_whose_check_was_never_busy_counts_that_check_and_its_time()
    {
        RecordsChangeAtRead(1);
        _checks.Counts = new BatchCheckCounts(ChecksMade: 2, ChecksThatDidNotStand: 1, CheckMilliseconds: 40, CallsNeverBusy: 1);

        var result = await Delete();

        Assert.Equal(
            new BatchRunCounts(
                Settles: 1, SettlesForRecordsChanged: 1, SettlesForRecordsChangedNotBusy: 1,
                ChecksForRecordsChangedNotBusy: 2, CheckMillisecondsForRecordsChangedNotBusy: 40),
            result.Counts);
        Assert.Equal(2, result.CheckCounts.ChecksMade);
    }

    [Fact]
    public async Task A_change_of_the_records_whose_check_found_Windows_Installer_busy_is_counted_only_as_a_change()
    {
        RecordsChangeAtRead(1);
        _checks.Counts = new BatchCheckCounts(ChecksMade: 1, CheckMilliseconds: 40, WaitsForMutex: 1);

        var result = await Delete();

        Assert.Equal(new BatchRunCounts(Settles: 1, SettlesForRecordsChanged: 1), result.Counts);
    }

    [Fact]
    public async Task A_settle_for_a_signal_is_not_counted_as_a_quiet_change_whatever_its_check_found()
    {
        _signals.Busy(PendingRebootResult.Block(PendingRebootReason.InstallerServiceRunning));
        _checks.Counts = new BatchCheckCounts(ChecksMade: 1, CallsNeverBusy: 1);

        var result = await Delete();

        Assert.Equal(new BatchRunCounts(Settles: 1, SettlesForService: 1), result.Counts);
    }

    [Fact]
    public async Task A_file_put_back_after_its_second_look_is_counted_and_one_whose_first_look_was_not_clean_is_not()
    {
        // The first look at the first file, then the second look at the second.
        _watch.OnRead = (watch, n) =>
        {
            if (n == 1) watch.Reading = RecordsWatchReading.Changed;
        };
        _checks.OnWatch = (watch, made) =>
        {
            if (made == 1)
                watch.OnRead = (w, n) =>
                {
                    if (n == 4) w.Reading = RecordsWatchReading.Changed;
                };
        };

        var result = await Delete();

        Assert.Equal(3, result.DeletedCount);
        Assert.Equal(2, result.Counts.Settles);
        Assert.Equal(1, result.Counts.PutBacks);
        Assert.Equal(["hold a.msi", "take a.msi", "delete a.msi", "hold b.msi", "take b.msi", "put-back b.msi"],
            _hand.Steps.Take(6));
    }

    [Fact]
    public async Task A_file_that_would_not_go_back_is_not_counted_as_put_back()
    {
        RecordsChangeAtRead(2);
        _hand.Refuse("put-back", A, Kernel32.ERROR_ALREADY_EXISTS);

        var result = await Delete();

        Assert.IsType<FileLeftInHand>(Assert.Single(result.Errors));
        Assert.Equal(0, result.Counts.PutBacks);
        Assert.Equal(1, result.Counts.Settles);
    }

    [Fact]
    public async Task A_file_put_back_after_a_look_that_failed_is_not_counted_as_put_back()
    {
        _signals.BusyClean().BusyThrows(new InvalidOperationException("the in-progress file would not read"));

        var result = await Delete();

        Assert.Equal(BatchEndReason.CheckFailed, result.Ended!.Reason);
        Assert.Equal(["hold a.msi", "take a.msi", "put-back a.msi"], _hand.Steps);
        Assert.Equal(new BatchRunCounts(FilesNotFinished: 3), result.Counts);
    }

    [Fact]
    public async Task A_cancel_in_the_pause_after_a_put_back_counts_the_put_back_and_no_settle()
    {
        using var source = new CancellationTokenSource();
        RecordsChangeAtRead(2);
        var before = InHandPause.ForTests;
        InHandPause.ForTests = (_, _) =>
        {
            source.Cancel();
            throw new OperationCanceledException(source.Token);
        };
        DeleteResult result;
        try
        {
            result = await Delete(token: source.Token);
        }
        finally
        {
            InHandPause.ForTests = before;
        }

        Assert.True(result.Cancelled);
        Assert.Empty(_checks.Calls);
        Assert.Equal(new BatchRunCounts(PutBacks: 1, FilesNotFinished: 3), result.Counts);
    }

    [Fact]
    public async Task A_file_in_use_once_is_counted_as_in_use_and_as_taken_at_the_second_try()
    {
        _hand.InUse(A, times: 1);

        var result = await Delete();

        Assert.Equal(new BatchRunCounts(InUseAtTake: 1, InUseTakenAtSecondTry: 1), result.Counts);
    }

    [Fact]
    public async Task A_file_still_in_use_at_the_second_try_is_counted_as_in_use_and_not_as_taken()
    {
        _hand.InUse(A, times: 2);

        var result = await Delete();

        Assert.Equal(new BatchRunCounts(InUseAtTake: 1), result.Counts);
    }

    [Fact]
    public async Task A_batch_that_ended_part_way_counts_the_files_it_left_untouched_and_the_four_add_up()
    {
        RecordsChangeAtRead(4);
        _checks.ThenEnds(BatchEndReason.RecordsKeptChanging);

        var result = await Delete();

        Assert.Equal(2, result.Counts.FilesNotFinished);
        Assert.Equal(1, result.Counts.PutBacks);
        Assert.Equal(3,
            result.DeletedCount + result.Errors.Count + result.HeldBack.Count + result.Counts.FilesNotFinished);
    }

    [Fact]
    public async Task A_cancelled_batch_counts_the_file_it_never_came_to_and_not_the_file_in_use_it_filed()
    {
        _hand.InUse(A, times: 1);

        var result = await Delete(mayAct: path => path != C);

        Assert.True(result.Cancelled);
        Assert.Equal(new BatchRunCounts(InUseAtTake: 1, FilesNotFinished: 1), result.Counts);
    }

    [Fact]
    public async Task A_batch_whose_folder_cannot_be_made_counts_every_file_as_not_finished()
    {
        _hand.OpenBatchError = Kernel32.ERROR_DIRECTORY;

        var result = await Delete();

        Assert.Equal(new BatchRunCounts(FilesNotFinished: 3), result.Counts);
    }

    [Fact]
    public async Task A_file_a_later_check_drops_is_held_back_and_not_counted_as_not_finished()
    {
        RecordsChangeAtRead(1);
        _checks.ThenStands(C);

        var result = await Delete();

        Assert.Equal([C], result.HeldBack);
        Assert.Equal(0, result.Counts.FilesNotFinished);
    }
}
