using InstallerClean.Cli;
using InstallerClean.Helpers;
using InstallerClean.Models;
using InstallerClean.Resources;
using InstallerClean.Services;
using NSubstitute;

namespace InstallerClean.Tests.Helpers;

/// <summary>
/// What Main reports for an exception thrown out of the run (<c>Program.ReportRunFailure</c>):
/// the unexpected-error line, one HardError entry, exit 1, and the note saying the
/// Application log refused an entry where it did, printed once in the run.
/// </summary>
public class CliRunFailureTests
{
    [Fact]
    public async Task A_run_failure_whose_entry_the_log_refused_prints_the_log_note_after_the_error_line()
    {
        var run = await Report(logRefuses: true, workFirst: false);

        Assert.Equal(CliExitCode.Error, run.ExitCode);
        Assert.Empty(run.Entries);
        var lines = run.Stdout.Split(Environment.NewLine);
        Assert.Equal(3, lines.Length);
        Assert.Contains(lines[0], ErrorLines(nameof(InvalidOperationException)));
        Assert.Equal(Strings.Cli_EventLogUnavailable, lines[1]);
        Assert.Equal(string.Empty, lines[2]);
    }

    [Fact]
    public async Task A_run_failure_whose_entry_the_log_took_prints_no_log_note()
    {
        var run = await Report(logRefuses: false, workFirst: false);

        Assert.Equal(CliExitCode.Error, run.ExitCode);
        var entry = Assert.Single(run.Entries);
        Assert.Equal(CliEventClass.HardError, entry.Class);
        var lines = run.Stdout.Split(Environment.NewLine);
        Assert.Equal(2, lines.Length);
        Assert.Contains(lines[0], ErrorLines(nameof(InvalidOperationException)));
        Assert.Equal(string.Empty, lines[1]);
    }

    [Fact]
    public async Task A_run_failure_after_the_work_has_printed_the_log_note_does_not_print_it_again()
    {
        var run = await Report(logRefuses: true, workFirst: true);

        var lines = run.Stdout.Split(Environment.NewLine);
        Assert.Equal(1, lines.Count(l => l == Strings.Cli_EventLogUnavailable));
        Assert.Contains(lines[^2], ErrorLines(nameof(InvalidOperationException)));
    }

    [Fact]
    public async Task An_exception_thrown_inside_one_of_the_work_s_catches_prints_the_log_note_once_after_the_error_line()
    {
        // A Delete whose scan reports files missing from the folder, an entry the log refuses,
        // and whose batch comes back cancelled. The console has no guard in front of it and
        // refuses the line saying the run was cancelled, so that write throws out of the
        // cancellation's catch and out of the work, and is reported as Main reports it.
        var scan = new ScanResult(
            [new OrphanedFile(@"C:\Windows\Installer\a.msi", 100, false, false, false, "unclaimed")],
            Array.Empty<RegisteredPackage>(), 0) with { MissingAffectedCount = 1 };
        using var cts = new CancellationTokenSource();
        var delete = Substitute.For<IDeleteFilesService>();
        delete.DeleteFilesAsync(Arg.Any<StandingCheck>(), Arg.Any<IProgress<OperationProgress>?>(), Arg.Any<CancellationToken>())
            .Returns(_ =>
            {
                cts.Cancel();
                return new DeleteResult(0, Array.Empty<FileOperationError>(), Cancelled: true);
            });
        var services = CliRunFixtures.ServicesKeepingTheOffer(scan, delete: delete);

        var original = Console.Out;
        using var console = new ConsoleFailingAt(Strings.Cli_Cancelled);
        try
        {
            Console.SetOut(console);
            await CliRunFixtures.WithTheLogRefusing(_ => true, async () =>
            {
                try
                {
                    return await Program.RunWorkAsync("/d", new CliInvocation(CliCommand.Delete, null, null),
                        cts.Token, services, () => false);
                }
                catch (Exception ex)
                {
                    return Program.ReportRunFailure(["/d"], ex);
                }
            });
        }
        finally
        {
            Console.SetOut(original);
        }

        Assert.True(console.Refused > 0, "The console refused no write, so nothing was thrown out of the work.");
        var lines = console.ToString().Split(Environment.NewLine);
        var error = Array.FindIndex(lines, line => ErrorLines(nameof(IOException)).Contains(line));
        Assert.True(error >= 0, console.ToString());
        Assert.Equal(1, lines.Count(l => l == Strings.Cli_EventLogUnavailable));
        Assert.Equal(Strings.Cli_EventLogUnavailable, lines[error + 1]);
    }

    /// <summary>
    /// The two forms of the unexpected-error line for an exception of the type
    /// <paramref name="typeName"/> names: crash.log written, and crash.log refused.
    /// </summary>
    private static string[] ErrorLines(string typeName) =>
    [
        string.Format(Strings.Cli_GenericError, typeName, CrashLog.LogPath),
        string.Format(Strings.Cli_GenericError_NoLog, typeName),
    ];

    private sealed record RunResult(
        int ExitCode, string Stdout, IReadOnlyList<(CliEventClass Class, string Text)> Entries);

    /// <summary>
    /// Reports an exception for a /s run, with an Application log that refuses every entry
    /// where <paramref name="logRefuses"/> is set and records them otherwise. Where
    /// <paramref name="workFirst"/> is set, the run's work goes first, a scan that offers
    /// nothing, so the work has printed the note before the exception is reported.
    /// </summary>
    private static async Task<RunResult> Report(bool logRefuses, bool workFirst)
    {
        // Console.SetOut is process-global; the assembly disables test parallelisation.
        var original = Console.Out;
        using var console = new StringWriter();
        try
        {
            Console.SetOut(console);
            return await CliRunFixtures.WithTheLogRefusing(_ => logRefuses, async () =>
            {
                if (workFirst)
                {
                    var scan = Substitute.For<IFileSystemScanService>();
                    scan.ScanAsync(Arg.Any<IProgress<ScanProgressUpdate>?>(), Arg.Any<CancellationToken>())
                        .Returns(new ScanResult(Array.Empty<OrphanedFile>(), Array.Empty<RegisteredPackage>(), 0));
                    await Program.RunWorkAsync("/s", new CliInvocation(CliCommand.ScanOnly, null, null),
                        CancellationToken.None, CliRunFixtures.Services(scan));
                }

                EventLogRecorder.Clear();
                var exitCode = Program.ReportRunFailure(["/s"], new InvalidOperationException("thrown out of the run"));
                return new RunResult(exitCode, console.ToString(), EventLogRecorder.Entries);
            });
        }
        finally
        {
            Console.SetOut(original);
        }
    }
}
