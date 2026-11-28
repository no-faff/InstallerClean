using InstallerClean.Cli;
using InstallerClean.Helpers;
using InstallerClean.Models;
using InstallerClean.Resources;
using InstallerClean.Services;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;

namespace InstallerClean.Tests.Helpers;

/// <summary>
/// What a <c>/d</c> or <c>/m</c> says and returns when its batch ends for a reason of its
/// own (<see cref="BatchEnd"/>): before its first file, because the check made before the
/// batch found no standing, or part-way, because a check made in the middle of the batch
/// did not.
///
/// THE SENTENCE AND THE ENTRY ARE READ BACK FROM THE BUILDERS, and the whole run is read
/// back too: the sentence the operator reads, the exit code a scheduler acts on and the
/// one entry the run writes, as the suite's recorder receives it. The walk over every
/// member of <see cref="BatchEndReason"/> is what fails when a reason is added without
/// its sentence.
///
/// STDOUT IS READ BACK THROUGH <c>Console.SetOut</c>, WHICH IS PROCESS-GLOBAL. The assembly
/// disables test parallelisation (AssemblyInfo.cs), which is what makes the capture safe.
/// </summary>
public class CliBatchEndedTests
{
    private const string OfferA = @"C:\Windows\Installer\offer-a.msi";
    private const string OfferB = @"C:\Windows\Installer\offer-b.msi";

    /// <summary>
    /// Temp is fully qualified on either host and outside both forbidden sets, so the
    /// <c>/m</c> destination gates pass it. Nothing is created: the move service is a
    /// substitute.
    /// </summary>
    private static readonly string Destination =
        Path.Combine(Path.GetTempPath(), "installerclean-cli-batch-ended-test");

    private static readonly FileOperationError[] NoErrors = Array.Empty<FileOperationError>();

    // ---- the sentence ----

    [Theory]
    [InlineData(BatchEndReason.WaitBoundReached)]
    [InlineData(BatchEndReason.RecordsKeptChanging)]
    [InlineData(BatchEndReason.WatchUnavailable)]
    public void A_reason_with_a_sentence_of_its_own_prints_it(BatchEndReason reason)
    {
        var expected = reason switch
        {
            BatchEndReason.WaitBoundReached => Strings.Cli_BatchEnded_WaitBoundReached,
            BatchEndReason.RecordsKeptChanging => Strings.Cli_BatchEnded_RecordsKeptChanging,
            _ => Strings.Cli_BatchEnded_WatchUnavailable,
        };

        Assert.Equal(expected, Program.BatchEndedLine("/d", new BatchEnd(reason)));
        Assert.Equal(expected, Program.BatchEndedLine("/m", new BatchEnd(reason)));
    }

    [Fact]
    public void A_folder_the_app_could_not_use_names_the_folder_and_the_windows_error()
    {
        var line = Program.BatchEndedLine(
            "/d", new BatchEnd(BatchEndReason.InHandFolderUnavailable, WindowsError: 5));

        Assert.Equal(
            string.Format(Strings.Cli_BatchEnded_InHandFolderUnavailable, InHandFolders.FolderName, 5), line);
    }

    [Theory]
    [InlineData(PendingRebootReason.MsiExecuteMutexAccessRefused)]
    [InlineData(PendingRebootReason.InstallerInProgressMarkerAccessRefused)]
    public void A_signal_read_Windows_refused_takes_the_sentence_that_says_what_a_batch_did(PendingRebootReason reason)
    {
        var line = Program.BatchEndedLine("/d",
            new BatchEnd(BatchEndReason.SignalWaitCannotClear, PendingRebootResult.Block(reason)));

        Assert.Equal(Strings.Cli_BatchEnded_AccessRefused, line);
    }

    [Theory]
    [InlineData("/d")]
    [InlineData("/m")]
    public void Any_other_blocked_signal_read_takes_the_sentence_the_run_before_a_batch_prints(string arg)
    {
        var line = Program.BatchEndedLine(arg,
            new BatchEnd(BatchEndReason.SignalWaitCannotClear,
                PendingRebootResult.Block(PendingRebootReason.PendingRenameInCache, "x")));

        Assert.Equal(
            Program.PendingRebootBlockedMessage(arg, PendingRebootReason.PendingRenameInCache, "x", null), line);
    }

    [Fact]
    public void A_check_that_threw_has_no_sentence()
    {
        var ended = new BatchEnd(BatchEndReason.CheckFailed, Failure: new InvalidOperationException("boom"));

        Assert.Throws<ArgumentOutOfRangeException>(() => Program.BatchEndedLine("/d", ended));
    }

    [Fact]
    public void Every_reason_but_a_failed_check_has_a_sentence()
    {
        var without = new List<string>();

        foreach (var reason in Enum.GetValues<BatchEndReason>())
        {
            if (reason == BatchEndReason.CheckFailed) continue;
            // The two reasons a read of the signals can accompany carry one.
            var signals = reason is BatchEndReason.SignalWaitCannotClear or BatchEndReason.WaitBoundReached
                ? PendingRebootResult.Block(PendingRebootReason.MsiExecuteMutexHeld)
                : null;
            string line;
            try
            {
                line = Program.BatchEndedLine("/d", new BatchEnd(reason, signals));
            }
            catch (ArgumentOutOfRangeException)
            {
                without.Add($"{reason}: no sentence");
                continue;
            }

            if (string.IsNullOrWhiteSpace(line)) without.Add($"{reason}: an empty sentence");
        }

        Assert.True(without.Count == 0, string.Join("; ", without));
    }

    // ---- the entry ----

    [Fact]
    public void An_end_after_some_files_names_how_many_of_how_many_and_the_reason()
    {
        var line = MachineContract.English(() => Program.BatchEndedEventLogLine(
            "/d", new BatchEnd(BatchEndReason.RecordsKeptChanging), 2, 5));

        Assert.Equal(
            MachineContract.English(() => string.Format(
                Strings.Cli_EventLogEndedPartial, "/d", 2, 5, "files", "RecordsKeptChanging")),
            line);
    }

    [Fact]
    public void An_end_before_any_file_says_no_work_was_done_and_names_the_reason()
    {
        var line = MachineContract.English(() => Program.BatchEndedEventLogLine(
            "/d", new BatchEnd(BatchEndReason.RecordsKeptChanging), 0, 5));

        Assert.Equal(
            MachineContract.English(() => string.Format(
                Strings.Cli_EventLogEndedNoWork, "/d", "RecordsKeptChanging")),
            line);
    }

    [Fact]
    public void An_end_on_a_signal_read_follows_the_reason_with_the_signal_s_name()
    {
        var ended = new BatchEnd(BatchEndReason.SignalWaitCannotClear,
            PendingRebootResult.Block(PendingRebootReason.PendingRenameInCache));

        var line = MachineContract.English(() => Program.BatchEndedEventLogLine("/m", ended, 0, 3));

        Assert.Equal(
            MachineContract.English(() => string.Format(
                Strings.Cli_EventLogEndedNoWork, "/m",
                "SignalWaitCannotClear: PendingRenameInCache")),
            line);
    }

    // ---- whole runs: the check before the batch ends ----

    [Theory]
    [InlineData("/d")]
    [InlineData("/m")]
    public async Task A_wait_for_Windows_Installer_that_ran_out_ends_the_run_come_back_later(string arg)
    {
        var delete = Substitute.For<IDeleteFilesService>();
        var move = Substitute.For<IMoveFilesService>();
        var signals = PendingRebootResult.Block(PendingRebootReason.MsiExecuteMutexHeld);

        var run = await Run(arg,
            CliRunFixtures.BatchCheckEnding(BatchEndReason.WaitBoundReached, signals),
            delete, move);

        Assert.Contains(Strings.Cli_BatchEnded_WaitBoundReached, run.Stdout);
        Assert.Equal(CliExitCode.Transient, run.ExitCode);
        var entry = Assert.Single(run.Entries);
        Assert.Equal(CliEventClass.TransientSkip, entry.Class);
        Assert.False(delete.ReceivedCalls().Any() || move.ReceivedCalls().Any(),
            "an action service was called");
    }

    [Theory]
    [InlineData("/d")]
    [InlineData("/m")]
    public async Task A_watch_that_could_not_be_set_up_ends_the_run_as_an_error(string arg)
    {
        var delete = Substitute.For<IDeleteFilesService>();
        var move = Substitute.For<IMoveFilesService>();

        var run = await Run(arg,
            CliRunFixtures.BatchCheckEnding(BatchEndReason.WatchUnavailable), delete, move);

        Assert.Contains(Strings.Cli_BatchEnded_WatchUnavailable, run.Stdout);
        Assert.Equal(CliExitCode.Error, run.ExitCode);
        var entry = Assert.Single(run.Entries);
        Assert.Equal(CliEventClass.HardError, entry.Class);
        Assert.False(delete.ReceivedCalls().Any() || move.ReceivedCalls().Any(),
            "an action service was called");
    }

    // ---- whole runs: the batch ends part-way or before its first file ----

    [Theory]
    [InlineData("/d")]
    [InlineData("/m")]
    public async Task A_batch_that_ended_after_two_files_says_so_after_what_it_did_and_exits_partial(string arg)
    {
        var ended = new BatchEnd(BatchEndReason.RecordsKeptChanging);
        var (delete, move) = Services(
            new DeleteResult(2, NoErrors, Ended: ended), new MoveResult(2, NoErrors, Ended: ended));

        var run = await Run(arg, CliRunFixtures.BatchCheckStandingOverEverything(), delete, move,
            withReport: true);

        var done = arg == "/m"
            ? string.Format(DisplayHelpers.Pluralise(2, Strings.Cli_MovedFiles, "Cli.MovedFiles"),
                DisplayHelpers.FormatCount(2), DisplayHelpers.PluraliseFile(2))
            : string.Format(DisplayHelpers.Pluralise(2, Strings.Cli_DeletedFiles, "Cli.DeletedFiles"),
                DisplayHelpers.FormatCount(2), DisplayHelpers.PluraliseFile(2));
        var doneAt = run.Stdout.IndexOf(done, StringComparison.Ordinal);
        var endedAt = run.Stdout.IndexOf(Strings.Cli_BatchEnded_RecordsKeptChanging, StringComparison.Ordinal);
        Assert.True(doneAt >= 0, run.Stdout);
        Assert.True(endedAt > doneAt, "the reason is not printed after what the batch did\n" + run.Stdout);
        Assert.Equal(CliExitCode.Partial, run.ExitCode);
        var entry = Assert.Single(run.Entries);
        Assert.Equal(CliEventClass.Partial, entry.Class);
        Assert.Equal(
            MachineContract.English(() => Program.BatchEndedEventLogLine(arg, ended, 2, 2)), entry.Text);
        Assert.Null(run.Report!.Entry);
    }

    [Theory]
    [InlineData("/d")]
    [InlineData("/m")]
    public async Task A_batch_ended_by_a_blocked_signal_read_before_its_first_file_ends_as_the_gate_does(string arg)
    {
        var reason = PendingRebootReason.PendingRenameInCache;
        var signals = PendingRebootResult.Block(reason, "x");
        var ended = new BatchEnd(BatchEndReason.SignalWaitCannotClear, signals);
        var (delete, move) = Services(
            new DeleteResult(0, NoErrors, Ended: ended), new MoveResult(0, NoErrors, Ended: ended));

        var run = await Run(arg, CliRunFixtures.BatchCheckStandingOverEverything(), delete, move);

        // The same run, ended by the check before the batch on the same read: the sentence,
        // the exit code and the entry are the same wherever the read ends it.
        var first = await Run(arg,
            CliRunFixtures.BatchCheckEnding(BatchEndReason.SignalWaitCannotClear, signals),
            Substitute.For<IDeleteFilesService>(), Substitute.For<IMoveFilesService>());

        var sentence = Program.PendingRebootBlockedMessage(arg, reason, "x", null);
        Assert.Contains(sentence, run.Stdout);
        Assert.Contains(sentence, first.Stdout);
        Assert.Equal(CliExitCode.Transient, run.ExitCode);
        Assert.Equal(first.ExitCode, run.ExitCode);
        Assert.Equal(Assert.Single(first.Entries), Assert.Single(run.Entries));
        Assert.DoesNotContain(Strings.Cli_BatchEnded_AccessRefused, run.Stdout);
    }

    [Fact]
    public async Task A_check_that_threw_after_files_were_deleted_ends_the_run_as_a_partial_one_and_prints_the_failure()
    {
        var ended = new BatchEnd(BatchEndReason.CheckFailed, Failure: new InvalidOperationException("boom"));
        var delete = Substitute.For<IDeleteFilesService>();
        delete.DeleteFilesAsync(Arg.Any<StandingCheck>(), Arg.Any<IProgress<OperationProgress>?>(), Arg.Any<CancellationToken>())
            .Returns(new DeleteResult(1, NoErrors, Ended: ended));

        var run = await Run("/d", CliRunFixtures.BatchCheckStandingOverEverything(), delete,
            Substitute.For<IMoveFilesService>());

        // A file was deleted, so the run is a partial one: the line saying what the batch
        // had done, then the failure by its type and where its detail went, never its
        // message, and one partial entry that counts the file and names the failure.
        Assert.Null(run.Thrown);
        Assert.Equal(CliExitCode.Partial, run.ExitCode);
        var lines = run.Stdout.Split(Environment.NewLine);
        var done = string.Format(DisplayHelpers.Pluralise(1, Strings.Cli_DeletedFiles, "Cli.DeletedFiles"),
            DisplayHelpers.FormatCount(1), DisplayHelpers.PluraliseFile(1));
        var doneAt = Array.IndexOf(lines, done);
        var errorAt = Array.FindIndex(lines, line =>
            line == string.Format(Strings.Cli_GenericError, nameof(InvalidOperationException), CrashLog.LogPath)
            || line == string.Format(Strings.Cli_GenericError_NoLog, nameof(InvalidOperationException)));
        Assert.True(doneAt >= 0, run.Stdout);
        Assert.True(errorAt > doneAt, "the error line is not printed after what the batch did\n" + run.Stdout);
        Assert.DoesNotContain("boom", run.Stdout);
        var entry = Assert.Single(run.Entries);
        Assert.Equal(CliEventClass.Partial, entry.Class);
        Assert.Equal(
            MachineContract.English(() => Program.BatchEndedEventLogLine("/d", ended, 1, 2)), entry.Text);
        Assert.Contains("CheckFailed: InvalidOperationException", entry.Text);
    }

    [Fact]
    public async Task A_check_that_threw_with_nothing_deleted_ends_the_run_as_an_unexpected_error()
    {
        var delete = Substitute.For<IDeleteFilesService>();
        delete.DeleteFilesAsync(Arg.Any<StandingCheck>(), Arg.Any<IProgress<OperationProgress>?>(), Arg.Any<CancellationToken>())
            .Returns(new DeleteResult(0, NoErrors,
                Ended: new BatchEnd(BatchEndReason.CheckFailed, Failure: new InvalidOperationException("boom"))));

        var run = await Run("/d", CliRunFixtures.BatchCheckStandingOverEverything(), delete,
            Substitute.For<IMoveFilesService>());

        // Nothing was deleted, so the run reports the check's exception as it reports
        // any other failure of its work: the unexpected-error line, one hard-error entry
        // and the error code.
        Assert.Null(run.Thrown);
        Assert.Equal(CliExitCode.Error, run.ExitCode);
        Assert.Contains(run.Stdout.Split(Environment.NewLine), line =>
            line == string.Format(Strings.Cli_GenericError, nameof(InvalidOperationException), CrashLog.LogPath)
            || line == string.Format(Strings.Cli_GenericError_NoLog, nameof(InvalidOperationException)));
        Assert.Equal(CliEventClass.HardError, Assert.Single(run.Entries).Class);
    }

    [Fact]
    public async Task A_check_ended_by_a_cancellation_that_was_not_the_runs_own_is_not_reported_as_the_run_being_cancelled()
    {
        var delete = Substitute.For<IDeleteFilesService>();
        delete.DeleteFilesAsync(Arg.Any<StandingCheck>(), Arg.Any<IProgress<OperationProgress>?>(), Arg.Any<CancellationToken>())
            .Returns(new DeleteResult(0, NoErrors,
                Ended: new BatchEnd(BatchEndReason.CheckFailed, Failure: new OperationCanceledException("not the run's"))));

        var run = await Run("/d", CliRunFixtures.BatchCheckStandingOverEverything(), delete,
            Substitute.For<IMoveFilesService>());

        Assert.Null(run.Thrown);
        Assert.Equal(CliExitCode.Error, run.ExitCode);
        Assert.DoesNotContain(Strings.Cli_Cancelled, run.Stdout);
        Assert.Equal(CliEventClass.HardError, Assert.Single(run.Entries).Class);
    }

    // ---- whole runs: what the check says while it waits ----

    [Fact]
    public async Task The_wait_bound_the_check_is_given_is_thirty_minutes()
    {
        TimeSpan? bound = null;
        var batchCheck = Substitute.For<IBatchCheck>();
        batchCheck.RunAsync(
                Arg.Any<IReadOnlyList<string>>(), Arg.Do<TimeSpan?>(b => bound = b),
                Arg.Any<IProgress<BatchCheckProgress>>(), Arg.Any<CancellationToken>())
            .Returns(ci => Task.FromResult(StandingChecks.StoodOver(OfferA, OfferB)));
        var (delete, move) = Services(new DeleteResult(2, NoErrors), new MoveResult(2, NoErrors));

        await Run("/d", batchCheck, delete, move);

        Assert.Equal(TimeSpan.FromMinutes(30), Program.BusyWaitBound);
        Assert.Equal(TimeSpan.FromMinutes(30), bound);
    }

    [Fact]
    public async Task A_wait_reported_in_the_middle_of_the_batch_prints_its_line()
    {
        var delete = Substitute.For<IDeleteFilesService>();
        delete.DeleteFilesAsync(Arg.Any<StandingCheck>(), Arg.Any<IProgress<OperationProgress>?>(), Arg.Any<CancellationToken>())
            .Returns(ci =>
            {
                ci.Arg<IProgress<OperationProgress>>().Report(new OperationProgress(2, 5, string.Empty,
                    new BatchCheckProgress(BatchCheckStage.Waiting,
                        PendingRebootResult.Block(PendingRebootReason.InstallerServiceRunning))));
                return new DeleteResult(2, NoErrors);
            });

        var run = await Run("/d", CliRunFixtures.BatchCheckStandingOverEverything(), delete,
            Substitute.For<IMoveFilesService>());

        Assert.Contains(Strings.Cli_WaitingForInstaller, run.Stdout);
        Assert.DoesNotContain(Strings.Cli_CheckingAgain, run.Stdout);
    }

    [Fact]
    public async Task A_check_made_in_the_middle_of_the_batch_prints_its_line()
    {
        var delete = Substitute.For<IDeleteFilesService>();
        delete.DeleteFilesAsync(Arg.Any<StandingCheck>(), Arg.Any<IProgress<OperationProgress>?>(), Arg.Any<CancellationToken>())
            .Returns(ci =>
            {
                ci.Arg<IProgress<OperationProgress>>().Report(new OperationProgress(2, 5, string.Empty,
                    new BatchCheckProgress(BatchCheckStage.Checking)));
                return new DeleteResult(2, NoErrors);
            });

        var run = await Run("/d", CliRunFixtures.BatchCheckStandingOverEverything(), delete,
            Substitute.For<IMoveFilesService>());

        Assert.Contains(Strings.Cli_CheckingAgain, run.Stdout);
        Assert.DoesNotContain(Strings.Cli_WaitingForInstaller, run.Stdout);
    }

    [Fact]
    public async Task A_wait_in_the_check_before_the_batch_prints_its_line()
    {
        var run = await RunWithFirstCheckReporting(BatchCheckStage.Waiting);

        Assert.Contains(Strings.Cli_WaitingForInstaller, run.Stdout);
    }

    [Fact]
    public async Task A_check_before_the_batch_prints_nothing_for_its_own_check()
    {
        var run = await RunWithFirstCheckReporting(BatchCheckStage.Checking);

        Assert.DoesNotContain(Strings.Cli_CheckingAgain, run.Stdout);
        Assert.DoesNotContain(Strings.Cli_WaitingForInstaller, run.Stdout);
    }

    // ---- fixtures ----

    private static Task<Outcome> RunWithFirstCheckReporting(BatchCheckStage stage)
    {
        var batchCheck = Substitute.For<IBatchCheck>();
        batchCheck.RunAsync(
                Arg.Any<IReadOnlyList<string>>(), Arg.Any<TimeSpan?>(),
                Arg.Any<IProgress<BatchCheckProgress>>(), Arg.Any<CancellationToken>())
            .Returns(ci =>
            {
                ci.Arg<IProgress<BatchCheckProgress>>().Report(new BatchCheckProgress(stage,
                    stage == BatchCheckStage.Waiting
                        ? PendingRebootResult.Block(PendingRebootReason.InstallerServiceRunning)
                        : null));
                return Task.FromResult(StandingChecks.StoodOver(OfferA, OfferB));
            });
        var (delete, move) = Services(new DeleteResult(2, NoErrors), new MoveResult(2, NoErrors));
        return Run("/d", batchCheck, delete, move);
    }

    private static (IDeleteFilesService Delete, IMoveFilesService Move) Services(
        DeleteResult delete, MoveResult move)
    {
        var deleteService = Substitute.For<IDeleteFilesService>();
        deleteService.DeleteFilesAsync(Arg.Any<StandingCheck>(), Arg.Any<IProgress<OperationProgress>?>(), Arg.Any<CancellationToken>())
            .Returns(delete);
        var moveService = Substitute.For<IMoveFilesService>();
        moveService.MoveFilesAsync(Arg.Any<StandingCheck>(), Arg.Any<string>(),
                Arg.Any<IProgress<OperationProgress>?>(), Arg.Any<CancellationToken>())
            .Returns(move);
        return (deleteService, moveService);
    }

    /// <summary>
    /// What a run returned or threw, what it printed and the entries it wrote, with the
    /// report it carried where <c>withReport</c> was set.
    /// </summary>
    private sealed record Outcome(
        int ExitCode, string Stdout, IReadOnlyList<(CliEventClass Class, string Text)> Entries,
        Exception? Thrown, RunReport? Report);

    /// <summary>
    /// Runs <paramref name="arg"/> over a scan offering two files, with the check and the
    /// services given. With <paramref name="withReport"/> the run is one that could carry
    /// the anonymous report: nobody has run before, so a run that finished would.
    /// </summary>
    private static async Task<Outcome> Run(
        string arg, IBatchCheck batchCheck, IDeleteFilesService delete, IMoveFilesService move,
        bool withReport = false)
    {
        var scan = Substitute.For<IFileSystemScanService>();
        scan.ScanAsync(Arg.Any<IProgress<ScanProgressUpdate>?>(), Arg.Any<CancellationToken>())
            .Returns(new ScanResult(
                [new OrphanedFile(OfferA, 1024, false, false, false, "unclaimed"),
                 new OrphanedFile(OfferB, 1024, false, false, false, "unclaimed")],
                Array.Empty<RegisteredPackage>(), 0));

        var reboot = Substitute.For<IPendingRebootService>();
        reboot.Check().Returns(PendingRebootResult.Clean);

        var earlier = Substitute.For<IEarlierRunCheck>();
        earlier.ShowsAnEarlierRunAsync().Returns(_ => Task.FromResult(false));
        earlier.ShowsAnEarlierRunWithinAsync(Arg.Any<TimeSpan>()).Returns(_ => Task.FromResult(false));
        var mark = Substitute.For<IFirstRunMark>();
        mark.TryClaim().Returns(true);
        var region = Substitute.For<IWindowsRegion>();
        region.Read().Returns("GB");
        var settings = Substitute.For<ISettingsService>();
        settings.TryLoad(out Arg.Any<AppSettings>()).Returns(call =>
        {
            call[0] = new AppSettings();
            return true;
        });

        var services = new ServiceCollection()
            .AddSingleton(scan)
            .AddSingleton(reboot)
            .AddSingleton(batchCheck)
            .AddSingleton(delete)
            .AddSingleton(move)
            .AddSingleton(settings)
            .AddSingleton(mark)
            .AddSingleton(earlier)
            .AddSingleton(region)
            .AddSingleton(Substitute.For<IRegistryReader>())
            .BuildServiceProvider();

        var report = withReport ? new RunReport(canAsk: true) : null;
        var invocation = arg == "/m"
            ? new CliInvocation(CliCommand.Move, null, Destination)
            : new CliInvocation(CliCommand.Delete, null, null);

        var original = Console.Out;
        using var buffer = new StringWriter();
        try
        {
            Console.SetOut(buffer);
            EventLogRecorder.Clear();
            try
            {
                var exitCode = await Program.RunWorkAsync(
                    arg, invocation, CancellationToken.None, services, () => false, report: report);
                return new Outcome(exitCode, buffer.ToString(), EventLogRecorder.Entries, null, report);
            }
            catch (Exception ex)
            {
                return new Outcome(-1, buffer.ToString(), EventLogRecorder.Entries, ex, report);
            }
        }
        finally
        {
            Console.SetOut(original);
        }
    }

    [Fact]
    public void The_event_log_line_is_one_line_and_its_remainder_serves_both_flags()
    {
        // One line covers /d and /m, the flag being its first word, so everything after
        // the flag is the same for both and names neither action without the other.
        var ended = new BatchEnd(BatchEndReason.RecordsKeptChanging);
        var delete = MachineContract.English(() => Program.BatchEndedEventLogLine("/d", ended, 0, 0));
        var move = MachineContract.English(() => Program.BatchEndedEventLogLine("/m", ended, 0, 0));

        Assert.DoesNotContain('\n', delete);
        Assert.Equal(delete["/d".Length..], move["/m".Length..]);
        Assert.Equal(
            delete.Contains("deleted", StringComparison.OrdinalIgnoreCase),
            delete.Contains("moved", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void The_event_log_line_reads_English_on_a_machine_that_is_not_and_leaves_the_thread_as_it_found_it()
    {
        // Two properties. The line is machine-read, so it is built inside
        // MachineContract.English and an RMM gets the same words whatever language
        // Windows is in. On an Italian thread, the line is built with both thread
        // cultures set to en-GB, which is what puts a machine line's template, noun
        // and size in English, and it reads as the English line. And English swaps
        // the two cultures and puts them back in a finally, so the thread is Italian
        // again afterwards: a leak would leave en-GB on it for everything that ran
        // next, sizes and nouns included.
        //
        // The cultures are read inside the build, because no satellite carries this
        // line's template: its words read English on any thread, so they alone
        // would hold whether or not the swap took place.
        //
        // Safe to write the thread cultures because the assembly disables test
        // parallelisation (AssemblyInfo.cs).
        var ui = System.Globalization.CultureInfo.CurrentUICulture;
        var format = System.Globalization.CultureInfo.CurrentCulture;
        var italian = System.Globalization.CultureInfo.GetCultureInfo("it-IT");
        try
        {
            System.Globalization.CultureInfo.CurrentUICulture = italian;
            System.Globalization.CultureInfo.CurrentCulture = italian;

            string? builtUnderUi = null;
            string? builtUnderFormat = null;
            var line = MachineContract.English(() =>
            {
                builtUnderUi = System.Globalization.CultureInfo.CurrentUICulture.Name;
                builtUnderFormat = System.Globalization.CultureInfo.CurrentCulture.Name;
                return Program.BatchEndedEventLogLine("/d", new BatchEnd(BatchEndReason.RecordsKeptChanging), 2, 5);
            });

            Assert.Equal("en-GB", builtUnderUi);
            Assert.Equal("en-GB", builtUnderFormat);
            Assert.Contains("mode stopped", line, StringComparison.Ordinal);
            Assert.Contains("2 of 5 files", line, StringComparison.Ordinal);
            // By name rather than by instance: what matters is the culture the
            // thread is left in, not which object carries it.
            Assert.Equal("it-IT", System.Globalization.CultureInfo.CurrentUICulture.Name);
            Assert.Equal("it-IT", System.Globalization.CultureInfo.CurrentCulture.Name);
        }
        finally
        {
            System.Globalization.CultureInfo.CurrentUICulture = ui;
            System.Globalization.CultureInfo.CurrentCulture = format;
        }
    }
}
