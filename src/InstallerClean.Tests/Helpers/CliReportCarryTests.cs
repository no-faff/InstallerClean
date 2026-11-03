using InstallerClean.Cli;
using InstallerClean.Helpers;
using InstallerClean.Models;
using InstallerClean.Resources;
using InstallerClean.Services;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;

namespace InstallerClean.Tests.Helpers;

/// <summary>
/// Which runs of the command line carry the anonymous report. A run at a console that
/// finished, on a PC with no earlier run, hands the report to the offer: a scan that
/// listed files or found none, and a Delete or Move that found nothing to act on or ran
/// to its end, with or without files acted on. A cancelled run, a refused one and a
/// stopped Move carry nothing. A run nobody can answer builds no report and does not
/// start the PC's first-run check. Driven through the real work method with substitute
/// services.
/// </summary>
public class CliReportCarryTests
{
    private static readonly string Destination =
        Path.Combine(Path.GetTempPath(), "installerclean-cli-report-carry-test");

    private const string File1 = @"C:\Windows\Installer\a.msi";
    private const string File2 = @"C:\Windows\Installer\b.msp";

    private static readonly FileOperationError[] NoErrors = Array.Empty<FileOperationError>();

    [Fact]
    public async Task A_scan_that_listed_files_carries_the_report_and_makes_this_the_first_run()
    {
        var rig = new Rig();

        var exitCode = await rig.Run("/s");

        Assert.Equal(CliExitCode.Ok, exitCode);
        var entry = Assert.IsType<ResultLogEntry>(rig.Report.Entry);
        Assert.Equal(OperationKinds.Scan, entry.Operation.Kind);
        Assert.Equal("commandLine", entry.App.Host);
        Assert.Equal("GB", entry.App.WindowsRegion);
        rig.Mark.Received(1).TryClaim();
        rig.Mark.DidNotReceive().Set();
    }

    [Fact]
    public async Task A_scan_that_offered_nothing_carries_the_report()
    {
        var rig = new Rig { Offered = false };

        var exitCode = await rig.Run("/s");

        Assert.Equal(CliExitCode.Ok, exitCode);
        Assert.Equal(OperationKinds.Scan, rig.Report.Entry!.Operation.Kind);
        rig.Mark.Received(1).TryClaim();
        rig.Mark.DidNotReceive().Set();
    }

    [Fact]
    public async Task A_scan_whose_claim_is_refused_carries_nothing()
    {
        var rig = new Rig { ClaimGranted = false };

        var exitCode = await rig.Run("/s");

        Assert.Equal(CliExitCode.Ok, exitCode);
        Assert.Null(rig.Report.Entry);
        rig.Mark.Received(1).TryClaim();
        rig.Mark.DidNotReceive().Set();
    }

    [Theory]
    [InlineData("/d")]
    [InlineData("/m")]
    public async Task A_delete_or_move_that_found_nothing_to_act_on_carries_a_scan_only_report(string arg)
    {
        var rig = new Rig { Offered = false };

        var exitCode = await rig.Run(arg, arg == "/m" ? Destination : null);

        Assert.Equal(CliExitCode.Ok, exitCode);
        Assert.Equal(OperationKinds.Scan, rig.Report.Entry!.Operation.Kind);
    }

    [Fact]
    public async Task A_scan_on_a_PC_with_an_earlier_run_carries_nothing_and_leaves_the_mark()
    {
        var rig = new Rig { EarlierRun = true };

        await rig.Run("/s");

        Assert.Null(rig.Report.Entry);
        rig.Mark.DidNotReceive().Set();
        rig.Mark.DidNotReceive().TryClaim();
    }

    [Theory]
    [InlineData("/s")]
    [InlineData("/d")]
    [InlineData("/m")]
    public async Task A_run_nobody_can_answer_builds_no_report_and_starts_no_check(string arg)
    {
        var rig = new Rig { CanAsk = false };

        await rig.Run(arg);

        Assert.Null(rig.Report.Entry);
        _ = rig.Earlier.DidNotReceive().ShowsAnEarlierRunAsync();
        _ = rig.Earlier.DidNotReceive().ShowsAnEarlierRunWithinAsync(Arg.Any<TimeSpan>());
        rig.Region.DidNotReceive().Read();
    }

    [Fact]
    public async Task A_run_given_no_report_to_fill_runs_as_it_always_did()
    {
        var rig = new Rig();

        var exitCode = await rig.Run("/s", withReport: false);

        Assert.Equal(CliExitCode.Ok, exitCode);
        _ = rig.Earlier.DidNotReceive().ShowsAnEarlierRunAsync();
    }

    [Fact]
    public async Task A_delete_that_deleted_files_carries_a_delete_report_decided_before_the_batch()
    {
        var rig = new Rig();
        rig.Delete.DeleteFilesAsync(Arg.Any<IEnumerable<string>>(), Arg.Any<UnderLeaseClaims>(),
                Arg.Any<IProgress<OperationProgress>?>(), Arg.Any<CancellationToken>())
            .Returns(new DeleteResult(2, NoErrors));

        var exitCode = await rig.Run("/d");

        Assert.Equal(CliExitCode.Ok, exitCode);
        var entry = Assert.IsType<ResultLogEntry>(rig.Report.Entry);
        Assert.Equal(OperationKinds.Delete, entry.Operation.Kind);
        Assert.Equal(2, entry.Operation.FilesProcessed);
        Assert.Equal("commandLine", entry.App.Host);
        // The batch sets the mark itself, so the answer it would read is taken first.
        Received.InOrder(() =>
        {
            _ = rig.Earlier.ShowsAnEarlierRunWithinAsync(Arg.Any<TimeSpan>());
            _ = rig.Delete.DeleteFilesAsync(Arg.Any<IEnumerable<string>>(), Arg.Any<UnderLeaseClaims>(),
                Arg.Any<IProgress<OperationProgress>?>(), Arg.Any<CancellationToken>());
        });
    }

    [Fact]
    public async Task A_delete_on_a_PC_with_an_earlier_run_carries_nothing()
    {
        var rig = new Rig { EarlierRun = true };
        rig.Delete.DeleteFilesAsync(Arg.Any<IEnumerable<string>>(), Arg.Any<UnderLeaseClaims>(),
                Arg.Any<IProgress<OperationProgress>?>(), Arg.Any<CancellationToken>())
            .Returns(new DeleteResult(2, NoErrors));

        await rig.Run("/d");

        Assert.Null(rig.Report.Entry);
    }

    [Fact]
    public async Task A_delete_in_which_some_files_failed_still_carries_the_report()
    {
        var rig = new Rig();
        rig.Delete.DeleteFilesAsync(Arg.Any<IEnumerable<string>>(), Arg.Any<UnderLeaseClaims>(),
                Arg.Any<IProgress<OperationProgress>?>(), Arg.Any<CancellationToken>())
            .Returns(new DeleteResult(1, new FileOperationError[] { new UnknownError(File2) }));

        var exitCode = await rig.Run("/d");

        Assert.Equal(CliExitCode.Partial, exitCode);
        var entry = Assert.IsType<ResultLogEntry>(rig.Report.Entry);
        Assert.Equal(1, entry.Operation.FilesFailed);
    }

    [Fact]
    public async Task A_delete_where_the_check_before_it_took_every_file_back_carries_the_report()
    {
        var rig = new Rig { ReverifyKeeps = false };

        var exitCode = await rig.Run("/d");

        Assert.Equal(CliExitCode.Ok, exitCode);
        Assert.Equal(OperationKinds.Delete, rig.Report.Entry!.Operation.Kind);
        Assert.Equal(0, rig.Report.Entry.Operation.FilesProcessed);
    }

    [Fact]
    public async Task A_move_that_moved_files_carries_a_move_report_naming_where_it_went()
    {
        var rig = new Rig();
        rig.Move.MoveFilesAsync(Arg.Any<IEnumerable<string>>(), Arg.Any<string>(),
                Arg.Any<UnderLeaseClaims>(), Arg.Any<IProgress<OperationProgress>?>(),
                Arg.Any<CancellationToken>())
            .Returns(new MoveResult(2, NoErrors));

        var exitCode = await rig.Run("/m", Destination);

        Assert.Equal(CliExitCode.Ok, exitCode);
        var entry = Assert.IsType<ResultLogEntry>(rig.Report.Entry);
        Assert.Equal(OperationKinds.Move, entry.Operation.Kind);
        Assert.False(string.IsNullOrEmpty(entry.Operation.MoveDestinationKind));
    }

    [Fact]
    public async Task A_cancelled_delete_carries_nothing()
    {
        using var cts = new CancellationTokenSource();
        var rig = new Rig();
        rig.Delete.DeleteFilesAsync(Arg.Any<IEnumerable<string>>(), Arg.Any<UnderLeaseClaims>(),
                Arg.Any<IProgress<OperationProgress>?>(), Arg.Any<CancellationToken>())
            .Returns(_ =>
            {
                cts.Cancel();
                return new DeleteResult(1, NoErrors, Cancelled: true);
            });

        var exitCode = await rig.Run("/d", token: cts.Token);

        Assert.Equal(CliExitCode.Partial, exitCode);
        Assert.Null(rig.Report.Entry);
    }

    [Fact]
    public async Task A_stopped_move_carries_nothing()
    {
        var rig = new Rig();
        rig.Move.MoveFilesAsync(Arg.Any<IEnumerable<string>>(), Arg.Any<string>(),
                Arg.Any<UnderLeaseClaims>(), Arg.Any<IProgress<OperationProgress>?>(),
                Arg.Any<CancellationToken>())
            .Returns<MoveResult>(_ => throw new MoveAbortedException(
                "stopped", new MoveResult(1, NoErrors), Destination, MoveAbortReason.ResolvesElsewhere));

        await rig.Run("/m", Destination);

        Assert.Null(rig.Report.Entry);
    }

    [Theory]
    [InlineData("/d")]
    [InlineData("/m")]
    public async Task A_run_the_pending_reboot_gate_refuses_carries_nothing(string arg)
    {
        var rig = new Rig();
        rig.Reboot.Check().Returns(PendingRebootResult.Block(PendingRebootReason.MsiExecuteMutexHeld));

        var exitCode = await rig.Run(arg, arg == "/m" ? Destination : null);

        Assert.NotEqual(CliExitCode.Ok, exitCode);
        Assert.Null(rig.Report.Entry);
        rig.Mark.DidNotReceive().Set();
        rig.Mark.DidNotReceive().TryClaim();
    }

    [Fact]
    public async Task A_report_that_cannot_be_built_leaves_the_run_and_the_mark_as_they_were()
    {
        var rig = new Rig();
        rig.Region.Read().Returns(_ => throw new InvalidOperationException("region"));

        var exitCode = await rig.Run("/s");

        Assert.Equal(CliExitCode.Ok, exitCode);
        Assert.Null(rig.Report.Entry);
        rig.Mark.DidNotReceive().Set();
        rig.Mark.DidNotReceive().TryClaim();
    }

    // ---- the offer, as Main calls it ----

    [Fact]
    public void A_report_carried_by_the_run_is_offered_and_not_a_waiting_one_sent()
    {
        var entry = ResultLogEntry.ForScanOnly(
            new ScanResult(Array.Empty<OrphanedFile>(), Array.Empty<RegisteredPackage>(), RegisteredTotalBytes: 0),
            1, "GB", ReportHost.CommandLine, AppCopyKind.CommandLineDownload);
        var report = new RunReport(canAsk: true) { Entry = entry };
        var log = Substitute.For<IResultLogService>();
        log.WriteAsync(entry, Arg.Any<CancellationToken>()).Returns(true);
        var console = new SilentConsole("n");

        Program.OfferReport(report, console, log, Substitute.For<ISettingsService>(), CancellationToken.None);

        log.Received(1).WriteAsync(entry, Arg.Any<CancellationToken>());
        log.DidNotReceive().SendAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public void A_run_with_no_report_looks_for_one_an_earlier_run_left()
    {
        var report = new RunReport(canAsk: true);
        var settings = Substitute.For<ISettingsService>();
        settings.TryLoad(out Arg.Any<AppSettings>()).Returns(call =>
        {
            call[0] = new AppSettings { ReportSendPending = true };
            return true;
        });
        var log = Substitute.For<IResultLogService>();
        log.LastLogExists().Returns(true);
        log.ReadLastLogAsync(Arg.Any<CancellationToken>()).Returns("{}");
        log.SendAsync("{}", Arg.Any<CancellationToken>()).Returns(ResultLogSendOutcome.Sent);

        Program.OfferReport(report, new SilentConsole(), log, settings, CancellationToken.None);

        log.Received(1).SendAsync("{}", Arg.Any<CancellationToken>());
    }

    [Fact]
    public void A_run_nobody_can_answer_offers_and_sends_nothing()
    {
        var report = new RunReport(canAsk: false);
        var log = Substitute.For<IResultLogService>();
        var settings = Substitute.For<ISettingsService>();

        Program.OfferReport(report, new SilentConsole(), log, settings, CancellationToken.None);

        Assert.Empty(log.ReceivedCalls());
        Assert.Empty(settings.ReceivedCalls());
    }

    [Fact]
    public void A_run_that_Ctrl_C_has_ended_offers_and_sends_nothing()
    {
        using var cts = new CancellationTokenSource();
        cts.Cancel();
        var report = new RunReport(canAsk: true) { Entry = null };
        var log = Substitute.For<IResultLogService>();
        var settings = Substitute.For<ISettingsService>();

        Program.OfferReport(report, new SilentConsole(), log, settings, cts.Token);

        Assert.Empty(log.ReceivedCalls());
        Assert.Empty(settings.ReceivedCalls());
    }

    // ---- the tip line, held for the end of a run that goes on to the report ----

    private static string DonateLine => string.Format(Strings.Cli_DonateAsk, SupportLink.Url);

    private static Rig WatchedRigWhoseDeleteDeletes()
    {
        var rig = new Rig { Watched = true };
        rig.Delete.DeleteFilesAsync(Arg.Any<IEnumerable<string>>(), Arg.Any<UnderLeaseClaims>(),
                Arg.Any<IProgress<OperationProgress>?>(), Arg.Any<CancellationToken>())
            .Returns(new DeleteResult(2, NoErrors));
        return rig;
    }

    [Fact]
    public async Task A_delete_that_carries_a_report_holds_its_tip_line_for_the_end_of_the_run()
    {
        var rig = WatchedRigWhoseDeleteDeletes();

        await rig.Run("/d");

        Assert.NotNull(rig.Report.Entry);
        Assert.True(rig.Report.DonationHeld);
        Assert.DoesNotContain(SupportLink.Url, rig.Stdout);
    }

    [Fact]
    public async Task A_move_that_carries_a_report_holds_its_tip_line_for_the_end_of_the_run()
    {
        var rig = new Rig { Watched = true };
        rig.Move.MoveFilesAsync(Arg.Any<IEnumerable<string>>(), Arg.Any<string>(),
                Arg.Any<UnderLeaseClaims>(), Arg.Any<IProgress<OperationProgress>?>(),
                Arg.Any<CancellationToken>())
            .Returns(new MoveResult(2, NoErrors));

        await rig.Run("/m", Destination);

        Assert.NotNull(rig.Report.Entry);
        Assert.True(rig.Report.DonationHeld);
        Assert.DoesNotContain(SupportLink.Url, rig.Stdout);
    }

    [Fact]
    public async Task A_delete_on_a_PC_where_an_earlier_run_left_a_report_waiting_holds_its_tip_line()
    {
        var rig = WatchedRigWhoseDeleteDeletes();
        rig.EarlierRun = true;
        rig.ReportWaiting = true;

        await rig.Run("/d");

        Assert.Null(rig.Report.Entry);
        Assert.True(rig.Report.DonationHeld);
        Assert.DoesNotContain(SupportLink.Url, rig.Stdout);
    }

    [Fact]
    public async Task A_delete_with_no_report_to_ask_about_or_send_prints_its_tip_line_as_it_always_did()
    {
        var rig = WatchedRigWhoseDeleteDeletes();
        rig.EarlierRun = true;

        await rig.Run("/d");

        Assert.False(rig.Report.DonationHeld);
        Assert.EndsWith(Environment.NewLine + Environment.NewLine + DonateLine + Environment.NewLine, rig.Stdout);
    }

    [Fact]
    public async Task A_delete_whose_claim_is_refused_prints_its_tip_line_in_place()
    {
        var rig = WatchedRigWhoseDeleteDeletes();
        rig.ClaimGranted = false;

        await rig.Run("/d");

        Assert.False(rig.Report.DonationHeld);
        Assert.EndsWith(Environment.NewLine + Environment.NewLine + DonateLine + Environment.NewLine, rig.Stdout);
    }

    [Fact]
    public async Task A_delete_at_a_console_nobody_can_answer_in_prints_its_tip_line_in_place()
    {
        // Output is watched and input is redirected, so nothing is asked.
        var rig = WatchedRigWhoseDeleteDeletes();
        rig.CanAsk = false;
        rig.ReportWaiting = true;

        await rig.Run("/d");

        Assert.False(rig.Report.DonationHeld);
        Assert.EndsWith(Environment.NewLine + Environment.NewLine + DonateLine + Environment.NewLine, rig.Stdout);
    }

    [Fact]
    public async Task A_delete_that_deleted_nothing_and_a_scan_hold_no_tip_line()
    {
        var nothing = new Rig { Watched = true, Offered = false };
        await nothing.Run("/d");
        var scan = new Rig { Watched = true };
        await scan.Run("/s");

        Assert.False(nothing.Report.DonationHeld);
        Assert.False(scan.Report.DonationHeld);
        Assert.DoesNotContain(SupportLink.Url, nothing.Stdout);
        Assert.DoesNotContain(SupportLink.Url, scan.Stdout);
    }

    [Theory]
    [InlineData("y", ResultLogSendOutcome.Sent, "sent")]
    [InlineData("n", ResultLogSendOutcome.Sent, "declined")]
    [InlineData("y", ResultLogSendOutcome.NetworkUnavailable, "not sent")]
    public void The_held_tip_line_is_the_last_thing_printed_after_the_question(
        string answer, ResultLogSendOutcome sendOutcome, string how)
    {
        var report = new RunReport(canAsk: true) { Entry = AnEntry(), DonationHeld = true };

        var stdout = EndOfRun(report, sendOutcome, CancellationToken.None, answer);

        Assert.EndsWith(Environment.NewLine + Environment.NewLine + DonateLine + Environment.NewLine, stdout);
        var reportLine = how switch
        {
            "sent" => Strings.Cli_ReportSent,
            "declined" => Strings.Cli_ReportNotSent,
            _ => string.Format(Strings.Cli_ReportCouldNotBeSent, Path.Combine(AppDataFolder.Location, "last-run.json")),
        };
        Assert.True(stdout.IndexOf(reportLine, StringComparison.Ordinal)
            < stdout.IndexOf(DonateLine, StringComparison.Ordinal));
    }

    [Fact]
    public void The_held_tip_line_follows_the_send_of_a_report_an_earlier_run_left_waiting()
    {
        var report = new RunReport(canAsk: true) { DonationHeld = true };

        var stdout = EndOfRun(report, ResultLogSendOutcome.Sent, CancellationToken.None, reportWaiting: true);

        Assert.Contains(Strings.Cli_ReportWaiting, stdout);
        Assert.EndsWith(Environment.NewLine + Environment.NewLine + DonateLine + Environment.NewLine, stdout);
        Assert.True(stdout.IndexOf(Strings.Cli_ReportSent, StringComparison.Ordinal)
            < stdout.IndexOf(DonateLine, StringComparison.Ordinal));
    }

    [Fact]
    public void Ctrl_C_at_the_question_leaves_the_held_tip_line_out()
    {
        using var cts = new CancellationTokenSource();
        var report = new RunReport(canAsk: true) { Entry = AnEntry(), DonationHeld = true };

        var stdout = EndOfRun(report, ResultLogSendOutcome.Sent, cts.Token, cancelAtTheQuestion: cts);

        Assert.Contains(Strings.Cli_ReportNotSent, stdout);
        Assert.DoesNotContain(SupportLink.Url, stdout);
    }

    [Fact]
    public void An_offer_that_ended_in_a_cancel_leaves_the_held_tip_line_out_whatever_the_token_says()
    {
        var report = new RunReport(canAsk: true) { DonationHeld = true };
        var original = Console.Out;
        using var buffer = new StringWriter();
        try
        {
            Console.SetOut(buffer);
            Program.WriteHeldDonation(report, ReportOfferOutcome.Cancelled, CancellationToken.None);
        }
        finally
        {
            Console.SetOut(original);
        }

        Assert.Empty(buffer.ToString());
    }

    [Fact]
    public void A_Ctrl_C_that_came_before_the_question_leaves_the_held_tip_line_out()
    {
        using var cts = new CancellationTokenSource();
        cts.Cancel();
        var report = new RunReport(canAsk: true) { Entry = AnEntry(), DonationHeld = true };

        var stdout = EndOfRun(report, ResultLogSendOutcome.Sent, cts.Token);

        Assert.Empty(stdout);
    }

    [Fact]
    public void A_tip_line_nothing_held_is_not_printed_after_the_question()
    {
        var report = new RunReport(canAsk: true) { Entry = AnEntry() };

        var stdout = EndOfRun(report, ResultLogSendOutcome.Sent, CancellationToken.None, "n");

        Assert.DoesNotContain(SupportLink.Url, stdout);
    }

    private static ResultLogEntry AnEntry() => ResultLogEntry.ForScanOnly(
        new ScanResult(Array.Empty<OrphanedFile>(), Array.Empty<RegisteredPackage>(), RegisteredTotalBytes: 0),
        1, "GB", ReportHost.CommandLine, AppCopyKind.CommandLineDownload);

    /// <summary>
    /// The end of a run as Main does it, through the offer over substitute services, and
    /// what it printed. The console prints into the same buffer as the tip line does,
    /// so the order is what is read back. <paramref name="cancelAtTheQuestion"/>, where
    /// given, is cancelled as the question is read, which ends the read with no answer, as
    /// Ctrl+C at the prompt does. <paramref name="reportWaiting"/> is an earlier run's
    /// report left to be sent.
    /// </summary>
    private static string EndOfRun(
        RunReport report, ResultLogSendOutcome sendOutcome, CancellationToken token,
        string? answer = null, CancellationTokenSource? cancelAtTheQuestion = null, bool reportWaiting = false)
    {
        var log = Substitute.For<IResultLogService>();
        log.WriteAsync(Arg.Any<ResultLogEntry>(), Arg.Any<CancellationToken>()).Returns(true);
        log.LastLogExists().Returns(true);
        log.ReadLastLogAsync(Arg.Any<CancellationToken>()).Returns("{}");
        log.SendAsync("{}", Arg.Any<CancellationToken>()).Returns(sendOutcome);
        var settings = Substitute.For<ISettingsService>();
        settings.Update(Arg.Any<Action<AppSettings>>()).Returns(true);
        settings.TryLoad(out Arg.Any<AppSettings>()).Returns(call =>
        {
            call[0] = new AppSettings { ReportSendPending = reportWaiting };
            return true;
        });

        var original = Console.Out;
        using var buffer = new StringWriter();
        try
        {
            Console.SetOut(buffer);
            Program.OfferReport(report, new RecordingConsole(answer, cancelAtTheQuestion), log, settings, token);
            return buffer.ToString();
        }
        finally
        {
            Console.SetOut(original);
        }
    }

    // ---- fixtures ----

    /// <summary>A console that prints to standard output and answers its one read with <paramref name="answer"/>, cancelling <paramref name="cancelling"/> first where given.</summary>
    private sealed class RecordingConsole(string? answer, CancellationTokenSource? cancelling) : IConsolePrompt
    {
        public void WriteLine(string line) => Console.WriteLine(line);

        public void Write(string text) => Console.Write(text);

        public string? ReadLine(CancellationToken token)
        {
            cancelling?.Cancel();
            return answer;
        }
    }

    /// <summary>A console that prints nowhere and answers each read with the next answer it was given, then with the end of input.</summary>
    private sealed class SilentConsole(params string?[] answers) : IConsolePrompt
    {
        private int _next;

        public void WriteLine(string line) { }

        public void Write(string text) { }

        public string? ReadLine(CancellationToken token) => _next < answers.Length ? answers[_next++] : null;
    }

    private sealed class Rig
    {
        public Rig()
        {
            Mark = Substitute.For<IFirstRunMark>();
            Mark.TryClaim().Returns(_ => ClaimGranted);
            Earlier = Substitute.For<IEarlierRunCheck>();
            Earlier.ShowsAnEarlierRunAsync().Returns(_ => Task.FromResult(EarlierRun));
            Earlier.ShowsAnEarlierRunWithinAsync(Arg.Any<TimeSpan>()).Returns(_ => Task.FromResult(EarlierRun));
            Region = Substitute.For<IWindowsRegion>();
            Region.Read().Returns("GB");
            Reboot = Substitute.For<IPendingRebootService>();
            Reboot.Check().Returns(PendingRebootResult.Clean);
            Delete = Substitute.For<IDeleteFilesService>();
            Move = Substitute.For<IMoveFilesService>();
        }

        public IFirstRunMark Mark { get; }
        public IEarlierRunCheck Earlier { get; }
        public IWindowsRegion Region { get; }
        public IPendingRebootService Reboot { get; }
        public IDeleteFilesService Delete { get; }
        public IMoveFilesService Move { get; }
        public RunReport Report { get; private set; } = new(canAsk: true);

        public bool EarlierRun { get; set; }
        public bool ClaimGranted { get; set; } = true;
        public bool CanAsk { get; set; } = true;
        public bool Offered { get; set; } = true;
        public bool ReverifyKeeps { get; set; } = true;

        /// <summary>The answer the run is given for whether somebody is watching its console.</summary>
        public bool Watched { get; set; }

        /// <summary>Whether the settings say an earlier run left a report waiting to be sent.</summary>
        public bool ReportWaiting { get; set; }

        /// <summary>What the last run printed to standard output.</summary>
        public string Stdout { get; private set; } = string.Empty;

        public async Task<int> Run(
            string arg, string? destination = null, CancellationToken token = default, bool withReport = true)
        {
            Report = new RunReport(CanAsk);
            var command = arg switch
            {
                "/s" => CliCommand.ScanOnly,
                "/m" => CliCommand.Move,
                _ => CliCommand.Delete,
            };
            EventLogRecorder.Clear();
            var original = Console.Out;
            using var buffer = new StringWriter();
            try
            {
                Console.SetOut(buffer);
                return await Program.RunWorkAsync(
                    arg, new CliInvocation(command, null, destination), token, Services(), () => Watched,
                    report: withReport ? Report : null);
            }
            finally
            {
                Console.SetOut(original);
                Stdout = buffer.ToString();
            }
        }

        private ISettingsService Settings()
        {
            var settings = Substitute.For<ISettingsService>();
            settings.TryLoad(out Arg.Any<AppSettings>()).Returns(call =>
            {
                call[0] = new AppSettings { ReportSendPending = ReportWaiting };
                return true;
            });
            return settings;
        }

        private IServiceProvider Services()
        {
            var files = Offered
                ? new[]
                {
                    new OrphanedFile(File1, 100, IsPatch: false, IsRemovablePatch: false,
                        IsObsoleted: false, Reason: "unclaimed"),
                    new OrphanedFile(File2, 200, IsPatch: true, IsRemovablePatch: true,
                        IsObsoleted: false, Reason: "superseded"),
                }
                : Array.Empty<OrphanedFile>();
            var scan = Substitute.For<IFileSystemScanService>();
            scan.ScanAsync(Arg.Any<IProgress<ScanProgressUpdate>?>(), Arg.Any<CancellationToken>())
                .Returns(new ScanResult(files, Array.Empty<RegisteredPackage>(), RegisteredTotalBytes: 0));

            var reverifier = Substitute.For<IRemovableReverifier>();
            reverifier.ReverifyAsync(Arg.Any<IReadOnlyList<string>>(), Arg.Any<CancellationToken>())
                .Returns(ReverifyKeeps
                    ? new ReverifyResult(
                        new[] { File1, File2 }, Array.Empty<string>(),
                        SurvivingPatchClaims: new[]
                        {
                            new PatchClaim(File2, "{AAAA1111-0000-0000-0000-000000000002}",
                                "{PPPP1111-0000-0000-0000-000000000001}", null, 2),
                        },
                        SiblingPatchClaims: Array.Empty<PatchClaim>())
                    : new ReverifyResult(Array.Empty<string>(), new[] { File1, File2 }));

            return new ServiceCollection()
                .AddSingleton(scan)
                .AddSingleton(Reboot)
                .AddSingleton(reverifier)
                .AddSingleton(Delete)
                .AddSingleton(Move)
                .AddSingleton(Settings())
                .AddSingleton(Mark)
                .AddSingleton(Earlier)
                .AddSingleton(Region)
                .AddSingleton(Substitute.For<IRegistryReader>())
                .BuildServiceProvider();
        }
    }
}
