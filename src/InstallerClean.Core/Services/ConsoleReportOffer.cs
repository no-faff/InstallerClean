using InstallerClean.Helpers;
using InstallerClean.Models;
using InstallerClean.Resources;

namespace InstallerClean.Services;

/// <summary>How <see cref="ConsoleReportOffer"/> ended.</summary>
internal enum ReportOfferOutcome
{
    /// <summary>Nothing was asked: the report could not be saved, or none was waiting.</summary>
    NotOffered,

    /// <summary>The report was sent and the server accepted it.</summary>
    Sent,

    /// <summary>The answer was no, and the report is not sent, now or later.</summary>
    Declined,

    /// <summary>The answer was yes and the report did not go. It is kept for a later run.</summary>
    NotSent,

    /// <summary>Ctrl+C or a closing console came while the question or the send was waiting.</summary>
    Cancelled,
}

/// <summary>
/// The command line's offer to send the anonymous report of a run: the report is saved
/// as <c>last-run.json</c>, the person is asked, and the report goes only on a yes. Also
/// sends a report an earlier run left waiting. The settings it keeps are the ones the
/// window keeps (<see cref="AppSettings.ReportSendPending"/> and
/// <see cref="AppSettings.HasSentResultLog"/>), so either program sends what the other
/// left waiting.
/// </summary>
internal sealed class ConsoleReportOffer(
    IConsolePrompt console,
    IResultLogService resultLog,
    ISettingsService settings,
    string reportPath,
    string chartAddress,
    Func<bool> consoleClosing,
    TimeSpan controlCBound,
    TimeSpan answerBound)
{
    /// <summary>
    /// Saves <paramref name="entry"/> and asks whether to send it. Enter or Y, or this
    /// language's letter for yes, sends; N or its letter for no does not; a question
    /// mark prints what the report holds and the report itself, then asks again;
    /// anything else asks again. The end of input is no, unless the token is cancelled
    /// within the bound this offer was given for it, since Ctrl+C at the prompt can
    /// itself be read as the end of input. Nothing answered within the bound this offer was
    /// given for an answer is no, said as such. Where the report cannot be saved nothing is
    /// asked, as there is no file to name or to send. Never throws.
    /// </summary>
    public ReportOfferOutcome Offer(ResultLogEntry entry, CancellationToken token)
    {
        try
        {
            if (!resultLog.WriteAsync(entry).GetAwaiter().GetResult())
                return ReportOfferOutcome.NotOffered;

            console.WriteLine(string.Empty);
            while (true)
            {
                switch (ConsoleQuestion.Ask(
                    console, (yes, no) => string.Format(Strings.Cli_ReportPrompt, reportPath, yes, no),
                    token, controlCBound, answerBound, out var answer))
                {
                    case ConsoleAnswer.Yes: return Send(waiting: false, token);
                    case ConsoleAnswer.No: return Declined();
                    case ConsoleAnswer.TimedOut:
                        console.WriteLine(Strings.Cli_ReportNoAnswer);
                        return Declined();
                    case ConsoleAnswer.Cancelled: return Cancelled();
                    case ConsoleAnswer.Other when answer == "?": ShowReport(); break;
                }
            }
        }
        catch (Exception ex)
        {
            CrashLog.TryWrite(ex);
            return ReportOfferOutcome.NotSent;
        }
    }

    /// <summary>
    /// Sends the report an earlier run left waiting, where
    /// <see cref="AppSettings.ReportSendPending"/> is set and
    /// <see cref="AppSettings.HasSentResultLog"/> is not, after a line saying what is
    /// being sent. Asks nothing. Never throws.
    /// </summary>
    public ReportOfferOutcome SendWaiting(CancellationToken token)
    {
        try
        {
            if (!settings.TryLoad(out var current) || !IsWaiting(current))
                return ReportOfferOutcome.NotOffered;
            // A report whose file is no longer there has nothing to send or to be kept.
            if (!resultLog.LastLogExists())
            {
                if (!settings.Update(s => s.ReportSendPending = false))
                    CrashLog.TryWrite(new InvalidOperationException(
                        "settings.json could not record that no report is waiting to be sent."));
                return ReportOfferOutcome.NotOffered;
            }
            console.WriteLine(Strings.Cli_ReportWaiting);
            return Send(waiting: true, token);
        }
        catch (Exception ex)
        {
            CrashLog.TryWrite(ex);
            return ReportOfferOutcome.NotSent;
        }
    }

    /// <summary>
    /// Whether <paramref name="current"/> says an earlier run left a report waiting to be
    /// sent: <see cref="AppSettings.ReportSendPending"/> is set and
    /// <see cref="AppSettings.HasSentResultLog"/> is not. <see cref="SendWaiting"/> sends
    /// one only where this answers true and the saved report is still there.
    /// </summary>
    internal static bool IsWaiting(AppSettings current) => current.ReportSendPending && !current.HasSentResultLog;

    /// <summary>
    /// Prints what the report holds, the panel's words with the chart named by its
    /// address, then the report as saved.
    /// </summary>
    private void ShowReport()
    {
        console.WriteLine(string.Empty);
        foreach (var line in ReportPanelText.ConsoleLines(
            string.Format(Strings.Cli_ReportChartAt, chartAddress)))
            console.WriteLine(line);
        console.WriteLine(string.Empty);
        console.WriteLine(Strings.Cli_ReportInFull);
        console.WriteLine(string.Empty);
        if (resultLog.ReadLastLogAsync().GetAwaiter().GetResult() is { } body)
            console.WriteLine(body);
        console.WriteLine(string.Empty);
    }

    private ReportOfferOutcome Declined()
    {
        console.WriteLine(Strings.Cli_ReportNotSent);
        return ReportOfferOutcome.Declined;
    }

    /// <summary>
    /// A closing console is given no line, as nobody can read it. The report stays
    /// unsent and not waiting: no yes was given.
    /// </summary>
    private ReportOfferOutcome Cancelled()
    {
        if (!consoleClosing()) console.WriteLine(Strings.Cli_ReportNotSent);
        return ReportOfferOutcome.Cancelled;
    }

    /// <summary>
    /// Records that the report is waiting, so a run that cannot send it leaves it for
    /// the next, sends the saved file, and where the server accepts it records that this
    /// account's report has gone. <paramref name="waiting"/> is a report an earlier run
    /// left, which is already recorded as waiting.
    /// </summary>
    private ReportOfferOutcome Send(bool waiting, CancellationToken token)
    {
        if (!waiting && !settings.Update(s => s.ReportSendPending = true))
            CrashLog.TryWrite(new InvalidOperationException(
                "settings.json could not record that the report is waiting to be sent."));

        ResultLogSendOutcome outcome;
        try
        {
            var body = resultLog.ReadLastLogAsync(token).GetAwaiter().GetResult();
            outcome = body is null
                ? ResultLogSendOutcome.NoLogToSend
                : resultLog.SendAsync(body, token).GetAwaiter().GetResult();
        }
        catch (OperationCanceledException)
        {
            // Ctrl+C while a yes is being sent stops it: the report is not sent and no
            // longer waiting, as the line says. A report an earlier run left stays
            // waiting for the next.
            if (!waiting && !settings.Update(s => s.ReportSendPending = false))
                CrashLog.TryWrite(new InvalidOperationException(
                    "settings.json could not record that the report is not waiting to be sent."));
            if (!consoleClosing()) console.WriteLine(Strings.Cli_ReportNotSent);
            return ReportOfferOutcome.Cancelled;
        }

        if (outcome != ResultLogSendOutcome.Sent)
        {
            console.WriteLine(string.Format(Strings.Cli_ReportCouldNotBeSent, reportPath));
            return ReportOfferOutcome.NotSent;
        }

        if (!settings.Update(s =>
            {
                s.HasSentResultLog = true;
                s.ReportSendPending = false;
            }))
            CrashLog.TryWrite(new InvalidOperationException(
                "The report was sent and settings.json could not record it."));
        console.WriteLine(Strings.Cli_ReportSent);
        return ReportOfferOutcome.Sent;
    }
}
