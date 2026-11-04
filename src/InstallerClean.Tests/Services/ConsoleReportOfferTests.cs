using InstallerClean.Helpers;
using InstallerClean.Models;
using InstallerClean.Resources;
using InstallerClean.Services;
using NSubstitute;

namespace InstallerClean.Tests.Services;

/// <summary>
/// The command line's offer to send the report: the question and its answers, what the
/// question mark prints, what is kept in settings.json for the window to find, and the
/// send of a report an earlier run left waiting. Driven through a scripted console,
/// with the strings read from the resources so the tests hold in any language.
/// </summary>
public class ConsoleReportOfferTests
{
    private const string ReportPath = @"C:\Users\someone\AppData\Local\InstallerClean\last-run.json";
    private const string ChartAddress = "https://github.com/no-faff/InstallerClean#reports-stats";
    private const string Body = "{\"schemaVersion\":12}";

    private static readonly ResultLogEntry Entry = ResultLogEntry.ForScanOnly(
        new ScanResult(Array.Empty<OrphanedFile>(), Array.Empty<RegisteredPackage>(), RegisteredTotalBytes: 0),
        5, "GB", ReportHost.CommandLine, AppCopyKind.CommandLineDownload);

    [Theory]
    [InlineData("")]
    [InlineData("y")]
    [InlineData("Y")]
    [InlineData("  yes ")]
    public void An_answer_of_yes_sends_the_report_and_records_that_it_has_gone(string answer)
    {
        var rig = new Rig(answer);

        var outcome = rig.Offer();

        Assert.Equal(ReportOfferOutcome.Sent, outcome);
        rig.Log.Received(1).SendAsync(Body, Arg.Any<CancellationToken>());
        Assert.True(rig.Settings.State.HasSentResultLog);
        Assert.False(rig.Settings.State.ReportSendPending);
        Assert.Equal(Strings.Cli_ReportSent, rig.Console.Lines[^1]);
    }

    [Fact]
    public void Keys_typed_before_the_question_are_discarded_before_it_is_printed()
    {
        var rig = new Rig("n");

        rig.Offer();

        Assert.Equal(["discard", "write", "read"], rig.Console.Steps);
    }

    [Theory]
    [InlineData("n")]
    [InlineData("N")]
    [InlineData("no")]
    public void An_answer_of_no_sends_nothing_now_or_later(string answer)
    {
        var rig = new Rig(answer);

        var outcome = rig.Offer();

        Assert.Equal(ReportOfferOutcome.Declined, outcome);
        rig.Log.DidNotReceive().SendAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
        Assert.False(rig.Settings.State.ReportSendPending);
        Assert.False(rig.Settings.State.HasSentResultLog);
        Assert.Equal(Strings.Cli_ReportNotSent, rig.Console.Lines[^1]);
    }

    [Fact]
    public void A_question_nobody_answers_sends_nothing_and_says_so()
    {
        var rig = new Rig(ScriptedConsole.NoAnswer);

        var outcome = rig.Offer();

        Assert.Equal(ReportOfferOutcome.Declined, outcome);
        rig.Log.DidNotReceive().SendAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
        Assert.False(rig.Settings.State.ReportSendPending);
        Assert.False(rig.Settings.State.HasSentResultLog);
        Assert.Equal([string.Empty, Strings.Cli_ReportNoAnswer, Strings.Cli_ReportNotSent], rig.Console.Lines[^3..]);
    }

    [Fact]
    public void The_report_is_saved_before_the_question_and_the_question_names_the_file()
    {
        var rig = new Rig("n");

        rig.Offer();

        rig.Log.Received(1).WriteAsync(Entry, Arg.Any<CancellationToken>());
        var question = Assert.Single(rig.Console.Writes);
        Assert.Contains(ReportPath, question);
        Assert.Contains("[" + Strings.Cli_UpdateAnswerYes.Trim().ToUpper() + "/"
            + Strings.Cli_UpdateAnswerNo.Trim().ToLower() + "/?]", question);
    }

    [Fact]
    public void A_report_that_cannot_be_saved_asks_nothing_and_sends_nothing()
    {
        var rig = new Rig("y") { WriteSucceeds = false };

        var outcome = rig.Offer();

        Assert.Equal(ReportOfferOutcome.NotOffered, outcome);
        Assert.Empty(rig.Console.Writes);
        Assert.Empty(rig.Console.Lines);
        rig.Log.DidNotReceive().SendAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public void A_question_mark_prints_what_the_report_holds_then_the_report_then_asks_again()
    {
        var rig = new Rig("?", "n");

        var outcome = rig.Offer();

        Assert.Equal(ReportOfferOutcome.Declined, outcome);
        Assert.Equal(2, rig.Console.Writes.Count);
        Assert.Equal(rig.Console.Writes[0], rig.Console.Writes[1]);

        var printed = rig.Console.Lines;
        // The opening words name the chart by its address, with no brackets left.
        var intro = printed.Single(l => l.Contains(ChartAddress));
        Assert.DoesNotContain("[", intro);
        Assert.DoesNotContain("]", intro);
        // Every line of the window's panel, in the order the window draws them.
        var at = printed.IndexOf(intro);
        foreach (var line in ReportPanelText.Lines)
        {
            at = printed.IndexOf("- " + line, at + 1);
            Assert.True(at > 0, line);
        }
        Assert.True(printed.IndexOf(Strings.Completion_ReportPanel_Closing) > at);
        // Then the notice and the report as saved, then the question again.
        var notice = printed.IndexOf(Strings.Cli_ReportInFull);
        Assert.True(notice > at);
        Assert.Equal(Body, printed[notice + 2]);
        rig.Log.DidNotReceive().SendAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public void An_answer_that_is_neither_asks_again()
    {
        var rig = new Rig("perhaps", "n");

        var outcome = rig.Offer();

        Assert.Equal(ReportOfferOutcome.Declined, outcome);
        Assert.Equal(2, rig.Console.Writes.Count);
    }

    [Fact]
    public void The_end_of_input_is_no()
    {
        var rig = new Rig(new string?[] { null });

        var outcome = rig.Offer();

        Assert.Equal(ReportOfferOutcome.Declined, outcome);
        rig.Log.DidNotReceive().SendAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public void Ctrl_C_at_the_question_sends_nothing_and_says_so()
    {
        using var cts = new CancellationTokenSource();
        var rig = new Rig(new string?[] { null }) { OnRead = cts.Cancel };

        var outcome = rig.Offer(cts.Token);

        Assert.Equal(ReportOfferOutcome.Cancelled, outcome);
        rig.Log.DidNotReceive().SendAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
        Assert.False(rig.Settings.State.ReportSendPending);
        Assert.Equal(Strings.Cli_ReportNotSent, rig.Console.Lines[^1]);
    }

    [Fact]
    public void A_console_being_closed_at_the_question_prints_nothing()
    {
        using var cts = new CancellationTokenSource();
        var rig = new Rig(new string?[] { null }) { OnRead = cts.Cancel, Closing = true };

        var outcome = rig.Offer(cts.Token);

        Assert.Equal(ReportOfferOutcome.Cancelled, outcome);
        Assert.DoesNotContain(Strings.Cli_ReportNotSent, rig.Console.Lines);
        rig.Log.DidNotReceive().SendAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Theory]
    [InlineData(ResultLogSendOutcome.NetworkUnavailable)]
    [InlineData(ResultLogSendOutcome.Timeout)]
    [InlineData(ResultLogSendOutcome.ServerError)]
    [InlineData(ResultLogSendOutcome.Unknown)]
    public void A_send_that_is_not_accepted_leaves_the_report_waiting_and_names_the_file(
        ResultLogSendOutcome sendOutcome)
    {
        var rig = new Rig("y") { SendOutcome = sendOutcome };

        var outcome = rig.Offer();

        Assert.Equal(ReportOfferOutcome.NotSent, outcome);
        Assert.True(rig.Settings.State.ReportSendPending);
        Assert.False(rig.Settings.State.HasSentResultLog);
        Assert.Equal(string.Format(Strings.Cli_ReportCouldNotBeSent, ReportPath), rig.Console.Lines[^1]);
    }

    [Fact]
    public void A_report_that_cannot_be_read_back_is_not_sent_and_stays_waiting()
    {
        var rig = new Rig("y") { Body = null };

        var outcome = rig.Offer();

        Assert.Equal(ReportOfferOutcome.NotSent, outcome);
        Assert.True(rig.Settings.State.ReportSendPending);
        rig.Log.DidNotReceive().SendAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public void Ctrl_C_while_a_yes_is_being_sent_leaves_the_report_neither_sent_nor_waiting()
    {
        using var cts = new CancellationTokenSource();
        var rig = new Rig("y") { SendThrowsCancelled = true };

        var outcome = rig.Offer(cts.Token);

        Assert.Equal(ReportOfferOutcome.Cancelled, outcome);
        Assert.False(rig.Settings.State.ReportSendPending);
        Assert.False(rig.Settings.State.HasSentResultLog);
        Assert.Equal(Strings.Cli_ReportNotSent, rig.Console.Lines[^1]);
    }

    [Fact]
    public void Ctrl_C_while_a_waiting_report_is_being_sent_leaves_it_waiting()
    {
        var rig = new Rig { SendThrowsCancelled = true };
        rig.Settings.State.ReportSendPending = true;

        var outcome = rig.SendWaiting();

        Assert.Equal(ReportOfferOutcome.Cancelled, outcome);
        Assert.True(rig.Settings.State.ReportSendPending);
        Assert.False(rig.Settings.State.HasSentResultLog);
    }

    [Fact]
    public void A_waiting_report_whose_file_is_gone_is_dropped_without_a_word()
    {
        var rig = new Rig { LogExists = false };
        rig.Settings.State.ReportSendPending = true;

        var outcome = rig.SendWaiting();

        Assert.Equal(ReportOfferOutcome.NotOffered, outcome);
        Assert.False(rig.Settings.State.ReportSendPending);
        Assert.Empty(rig.Console.Lines);
        rig.Log.DidNotReceive().SendAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public void A_report_waiting_from_an_earlier_run_is_announced_and_sent_without_a_question()
    {
        var rig = new Rig();
        rig.Settings.State.ReportSendPending = true;

        var outcome = rig.SendWaiting();

        Assert.Equal(ReportOfferOutcome.Sent, outcome);
        Assert.Empty(rig.Console.Writes);
        Assert.Equal(new[] { Strings.Cli_ReportWaiting, Strings.Cli_ReportSent }, rig.Console.Lines);
        Assert.True(rig.Settings.State.HasSentResultLog);
        Assert.False(rig.Settings.State.ReportSendPending);
    }

    [Fact]
    public void A_waiting_report_that_still_cannot_be_sent_stays_waiting()
    {
        var rig = new Rig { SendOutcome = ResultLogSendOutcome.NetworkUnavailable };
        rig.Settings.State.ReportSendPending = true;

        var outcome = rig.SendWaiting();

        Assert.Equal(ReportOfferOutcome.NotSent, outcome);
        Assert.True(rig.Settings.State.ReportSendPending);
    }

    [Theory]
    [InlineData(false, false)]
    [InlineData(true, true)]
    [InlineData(false, true)]
    public void Nothing_is_said_or_sent_unless_a_report_is_waiting_and_has_not_gone(
        bool reportSendPending, bool hasSent)
    {
        var rig = new Rig();
        rig.Settings.State.ReportSendPending = reportSendPending;
        rig.Settings.State.HasSentResultLog = hasSent;

        var outcome = rig.SendWaiting();

        Assert.Equal(ReportOfferOutcome.NotOffered, outcome);
        Assert.Empty(rig.Console.Lines);
        rig.Log.DidNotReceive().SendAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public void Settings_that_cannot_be_read_send_nothing()
    {
        var rig = new Rig();
        rig.Settings.Readable = false;

        Assert.Equal(ReportOfferOutcome.NotOffered, rig.SendWaiting());
        rig.Log.DidNotReceive().SendAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    /// <summary>The offer over a scripted console, a substitute log and in-memory settings.</summary>
    private sealed class Rig
    {
        public Rig(params string?[] answers)
        {
            Console = new ScriptedConsole(this, answers);
            Settings = new MemorySettings();
            Log = Substitute.For<IResultLogService>();
            Log.WriteAsync(Arg.Any<ResultLogEntry>(), Arg.Any<CancellationToken>())
                .Returns(_ => Task.FromResult(WriteSucceeds));
            Log.LastLogExists().Returns(_ => LogExists);
            Log.ReadLastLogAsync(Arg.Any<CancellationToken>()).Returns(_ => Task.FromResult<string?>(Body));
            Log.SendAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(_ =>
                SendThrowsCancelled
                    ? Task.FromException<ResultLogSendOutcome>(new OperationCanceledException())
                    : Task.FromResult(SendOutcome));
        }

        public ScriptedConsole Console { get; }
        public MemorySettings Settings { get; }
        public IResultLogService Log { get; }
        public bool WriteSucceeds { get; set; } = true;
        public bool LogExists { get; set; } = true;
        public string? Body { get; set; } = ConsoleReportOfferTests.Body;
        public ResultLogSendOutcome SendOutcome { get; set; } = ResultLogSendOutcome.Sent;
        public bool SendThrowsCancelled { get; set; }
        public bool Closing { get; set; }
        public Action? OnRead { get; set; }

        public ReportOfferOutcome Offer(CancellationToken token = default) => Make().Offer(Entry, token);

        public ReportOfferOutcome SendWaiting(CancellationToken token = default) => Make().SendWaiting(token);

        private ConsoleReportOffer Make() => new(
            Console, Log, Settings, ReportPath, ChartAddress, () => Closing, TimeSpan.Zero,
            TimeSpan.FromMilliseconds(50));
    }

    private sealed class ScriptedConsole(Rig rig, string?[] answers) : IConsolePrompt
    {
        /// <summary>An answer that never comes: the read waits until its token is cancelled.</summary>
        public const string NoAnswer = "\0 no answer";

        private int _next;

        public List<string> Lines { get; } = [];
        public List<string> Writes { get; } = [];

        public void WriteLine(string line) => Lines.Add(line);

        public void Write(string text)
        {
            Steps.Add("write");
            Writes.Add(text);
        }

        /// <summary>What happened at the console, in order: "write", "discard" or "read".</summary>
        public List<string> Steps { get; } = [];

        public void DiscardTypedAhead() => Steps.Add("discard");

        public string? ReadLine(CancellationToken token)
        {
            Steps.Add("read");
            rig.OnRead?.Invoke();
            var answer = _next < answers.Length ? answers[_next++] : null;
            if (answer != NoAnswer) return answer;
            token.WaitHandle.WaitOne();
            return null;
        }
    }

    private sealed class MemorySettings : ISettingsService
    {
        public AppSettings State { get; } = new();
        public bool Readable { get; set; } = true;

        public AppSettings Load() => State;

        public bool TryLoad(out AppSettings settings)
        {
            settings = State;
            return Readable;
        }

        public bool TrySave(AppSettings settings) => true;

        public bool Update(Action<AppSettings> mutate)
        {
            mutate(State);
            return true;
        }
    }
}
