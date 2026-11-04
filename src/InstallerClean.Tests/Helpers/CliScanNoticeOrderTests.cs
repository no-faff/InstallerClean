using InstallerClean.Cli;
using InstallerClean.Helpers;
using InstallerClean.Models;
using InstallerClean.Resources;
using InstallerClean.Services;
using NSubstitute;

namespace InstallerClean.Tests.Helpers;

/// <summary>
/// Where the command line prints the scan's notices: the count of files left alone, the
/// drives and shares given up, the files missing from the folder and the files held back
/// for their times. A /s that lists files prints
/// them ahead of the line counting the files, so the list is the last thing printed; every
/// other run prints them after the line saying what the scan found, or after the scanning
/// line where no such line is printed. Driven through the real work method.
///
/// WHAT THE FIXTURES SET UP is a scan meeting every condition that has a stdout line
/// (EveryNotice), with the offer of two files or without it, so a notice printed in the
/// wrong place has a list or a count line to be found beside; and, for the runs that need
/// no notice, a scan with the offer alone or with nothing at all (OffersNothing).
/// </summary>
public class CliScanNoticeOrderTests
{
    private const string OfferA = @"C:\Windows\Installer\offer-a.msi";
    private const string OfferB = @"C:\Windows\Installer\offer-b.msi";
    private static readonly SourceRootGivenUp DriveE = new("E:", SourceRootGiveUpRoute.NoAnswer, 1);

    [Fact]
    public async Task A_scan_that_lists_files_prints_its_notices_ahead_of_the_count_line_and_ends_on_the_list()
    {
        var scan = EveryNotice();

        var run = await Run("/s", scan);

        var lines = run.Stdout.Split(Environment.NewLine);
        var notices = NoticeOpenings(scan)
            .Select(opening => Array.FindIndex(lines, l => l.StartsWith(opening, StringComparison.Ordinal)))
            .ToArray();
        Assert.Equal(Strings.Cli_ScanningInstaller, lines[0]);
        Assert.Equal(1, notices[0]);
        for (var i = 1; i < notices.Length; i++)
            Assert.True(notices[i] > notices[i - 1], $"Notice {i} out of order at {notices[i]}.\n{run.Stdout}");

        var found = Array.FindIndex(lines, l => l.StartsWith(Opening(Strings.Cli_FoundOrphans), StringComparison.Ordinal));
        var lastRow = Array.FindIndex(lines, l => l.StartsWith(LastRow, StringComparison.Ordinal));
        Assert.True(found == notices[^1] + 1 && lastRow == found + 2, run.Stdout);
        Assert.Equal(lines.Length - 2, lastRow);
        Assert.Equal(string.Empty, lines[^1]);
    }

    [Fact]
    public async Task A_scan_that_lists_files_and_has_no_notices_ends_at_the_last_row()
    {
        var run = await Run("/s", new ScanResult(Offer(), Array.Empty<RegisteredPackage>(), 0));

        var lines = run.Stdout.Split(Environment.NewLine);
        Assert.StartsWith(LastRow, lines[^2], StringComparison.Ordinal);
        Assert.Equal(string.Empty, lines[^1]);
    }

    [Fact]
    public async Task A_delete_prints_the_notices_under_the_line_counting_what_was_found()
    {
        var scan = EveryNotice();

        var run = await Run("/d", scan);

        var lines = run.Stdout.Split(Environment.NewLine);
        var found = Array.FindIndex(lines, l => l.StartsWith(Opening(Strings.Cli_FoundOrphans), StringComparison.Ordinal));
        var deleted = Array.FindIndex(lines, l => l.StartsWith(Opening(Strings.Cli_DeletedFiles), StringComparison.Ordinal));
        var notices = NoticeOpenings(scan)
            .Select(opening => Array.FindIndex(lines, l => l.StartsWith(opening, StringComparison.Ordinal)))
            .ToArray();
        Assert.True(found >= 0, run.Stdout);
        Assert.Equal(found + 1, notices[0]);
        for (var i = 1; i < notices.Length; i++)
            Assert.True(notices[i] > notices[i - 1], $"Notice {i} out of order at {notices[i]}.\n{run.Stdout}");
        Assert.True(deleted > notices[^1], run.Stdout);
    }

    [Theory]
    [InlineData("the first notice")]
    [InlineData("the first row")]
    public async Task A_scan_whose_output_fails_exits_1_on_one_error_entry_with_every_notice_written(
        string failsAt)
    {
        var scan = EveryNotice();
        var console = new ConsoleFailingAt(failsAt == "the first row"
            ? Offer()[0].FileName
            : LeftAloneReport.Line(scan.LeftAloneCount));

        var run = await Run("/s", scan, console);

        Assert.True(console.Refused > 0, $"The console refused no write, so it never failed at {failsAt}.");
        Assert.Equal(CliExitCode.Error, run.ExitCode);
        Assert.Contains(run.Entries, entry => entry.Class == CliEventClass.ScanWalkHeldBackNotice);
        Assert.Contains(run.Entries, entry => entry.Class == CliEventClass.SourcesGivenUpNotice);
        Assert.Contains(run.Entries, entry => entry.Class == CliEventClass.ScanSupersededHeldBackNotice);
        Assert.Contains(run.Entries, entry => entry.Class == CliEventClass.ScanMissingFilesNotice);
        AssertEndsOnTheErrorForTheFailedWrite(run);
    }

    [Fact]
    public async Task A_scan_that_offers_nothing_and_whose_output_fails_exits_1_on_one_error_entry()
    {
        var console = new ConsoleFailingAt(Strings.Cli_FoundNoOrphans);

        var run = await Run("/s", OffersNothing(), console);

        Assert.True(console.Refused > 0, "The console refused no write.");
        Assert.Equal(CliExitCode.Error, run.ExitCode);
        AssertEndsOnTheErrorForTheFailedWrite(run);
    }

    [Fact]
    public async Task A_delete_that_has_nothing_to_delete_and_whose_output_fails_exits_as_that_delete()
    {
        var console = new ConsoleFailingAt(Strings.Cli_FoundNoOrphans);

        var run = await Run("/d", OffersNothing(), console);

        Assert.True(console.Refused > 0, "The console refused no write.");
        Assert.Equal(CliExitCode.Ok, run.ExitCode);
        var summary = Assert.Single(run.Entries, CliRunFixtures.IsSummary);
        Assert.Equal(CliEventClass.Ok, summary.Class);
        Assert.Equal(MachineContract.English(() => string.Format(Strings.Cli_EventLogScanNoOrphans, "/d")), summary.Text);
    }

    [Fact]
    public async Task A_scan_whose_notices_the_log_refused_prints_the_log_note_ahead_of_the_count_line_and_ends_on_the_list()
    {
        var scan = EveryNotice();

        var run = await CliRunFixtures.WithTheLogRefusing(entry => !CliRunFixtures.IsSummary(entry), () => Run("/s", scan));

        var lines = run.Stdout.Split(Environment.NewLine);
        var lastNotice = Array.FindIndex(lines, l => l.StartsWith(NoticeOpenings(scan)[^1], StringComparison.Ordinal));
        var note = Array.IndexOf(lines, Strings.Cli_EventLogUnavailable);
        var found = Array.FindIndex(lines, l => l.StartsWith(Opening(Strings.Cli_FoundOrphans), StringComparison.Ordinal));
        Assert.Equal(1, lines.Count(l => l == Strings.Cli_EventLogUnavailable));
        Assert.True(note == lastNotice + 1 && found == note + 1, run.Stdout);
        Assert.StartsWith(LastRow, lines[^2], StringComparison.Ordinal);
        Assert.Equal(string.Empty, lines[^1]);
    }

    [Fact]
    public async Task A_scan_whose_summary_alone_the_log_refused_prints_the_log_note_after_the_list()
    {
        var run = await CliRunFixtures.WithTheLogRefusing(entry => entry == CliEventClass.Ok, () => Run("/s", EveryNotice()));

        var lines = run.Stdout.Split(Environment.NewLine);
        Assert.Equal(1, lines.Count(l => l == Strings.Cli_EventLogUnavailable));
        Assert.Equal(Strings.Cli_EventLogUnavailable, lines[^2]);
        Assert.StartsWith(LastRow, lines[^3], StringComparison.Ordinal);
    }

    [Fact]
    public async Task A_delete_whose_entries_the_log_refused_prints_the_log_note_once_after_its_result()
    {
        var run = await CliRunFixtures.WithTheLogRefusing(_ => true, () => Run("/d", EveryNotice()));

        var lines = run.Stdout.Split(Environment.NewLine);
        var deleted = Array.FindIndex(lines, l => l.StartsWith(Opening(Strings.Cli_DeletedFiles), StringComparison.Ordinal));
        var note = Array.IndexOf(lines, Strings.Cli_EventLogUnavailable);
        Assert.Equal(1, lines.Count(l => l == Strings.Cli_EventLogUnavailable));
        Assert.True(deleted >= 0 && note > deleted, run.Stdout);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task A_scan_whose_summary_the_log_refused_and_whose_log_note_then_fails_exits_1_on_one_error_entry(
        bool listsFiles)
    {
        var console = new ConsoleFailingAt(Strings.Cli_EventLogUnavailable);

        var run = await CliRunFixtures.WithTheLogRefusing(entry => entry == CliEventClass.Ok,
            () => Run("/s", listsFiles ? EveryNotice() : OffersNothing(), console));

        Assert.True(console.Refused > 0, "The console refused no write, so the note never failed.");
        Assert.Equal(CliExitCode.Error, run.ExitCode);
        AssertEndsOnTheErrorForTheFailedWrite(run);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task A_scan_whose_notices_the_log_refused_and_whose_log_note_fails_exits_1_on_one_error_entry(
        bool listsFiles)
    {
        var console = new ConsoleFailingAt(Strings.Cli_EventLogUnavailable);

        var run = await CliRunFixtures.WithTheLogRefusing(entry => !CliRunFixtures.IsSummary(entry),
            () => Run("/s", listsFiles ? EveryNotice() : EveryNotice() with { RemovableFiles = [] }, console));

        Assert.True(console.Refused > 0, "The console refused no write, so the note never failed.");
        Assert.Equal(CliExitCode.Error, run.ExitCode);
        AssertEndsOnTheErrorForTheFailedWrite(run);
    }

    [Fact]
    public async Task A_scan_that_offers_nothing_and_whose_notices_the_log_refused_exits_0_on_its_summary_and_prints_the_log_note_once()
    {
        var scan = EveryNotice() with { RemovableFiles = [] };

        var run = await CliRunFixtures.WithTheLogRefusing(entry => !CliRunFixtures.IsSummary(entry), () => Run("/s", scan));

        Assert.Equal(CliExitCode.Ok, run.ExitCode);
        var summary = Assert.Single(run.Entries, CliRunFixtures.IsSummary);
        Assert.Equal(CliEventClass.Ok, summary.Class);
        var lines = run.Stdout.Split(Environment.NewLine);
        Assert.Equal(1, lines.Count(l => l == Strings.Cli_EventLogUnavailable));
        Assert.Equal(Strings.Cli_EventLogUnavailable, lines[^2]);
        Assert.StartsWith(NoticeOpenings(scan)[^1], lines[^3], StringComparison.Ordinal);
    }

    // ---- fixtures ----

    /// <summary>The opening of the list's last row, which names the second file offered.</summary>
    private static string LastRow => "  " + Offer()[1].FileName + " ";

    /// <summary>A scan that offers nothing, holds nothing back and has nothing to report.</summary>
    private static ScanResult OffersNothing() =>
        new(Array.Empty<OrphanedFile>(), Array.Empty<RegisteredPackage>(), 0);

    private static OrphanedFile[] Offer() =>
        new[] { OfferA, OfferB }
            .Select(path => new OrphanedFile(path, 1024, false, false, false, "unclaimed"))
            .ToArray();

    /// <summary>
    /// An offer of two files beside one file held back because its program could not be
    /// established, one held back because its times are not before the scan's clock, one
    /// superseded file held back,
    /// a drive given up keeping a file and a registration whose file is missing.
    /// </summary>
    private static ScanResult EveryNotice() =>
        new ScanResult(
            Offer(), Array.Empty<RegisteredPackage>(), 0,
            WithheldCount: 1,
            WithheldFiles:
            [
                new OrphanedFile(@"C:\Windows\Installer\held-a.msi", 1024, false, false, false, "unclaimed"),
                new OrphanedFile(@"C:\Windows\Installer\held-b.msi", 1024, false, false, false, "unclaimed"),
            ],
            WithheldBy: new WithholdingSplit(DeclaredProductUnestablishedCount: 1, NotBeforeTheClockCount: 1),
            WithheldNotBeforeTheClockBytes: 1024,
            SourceRootsGivenUp: [DriveE])
        with { MissingAffectedCount = 1 };

    /// <summary>
    /// The opening of each notice <paramref name="scan"/> prints, in the order they are
    /// printed: the count of files left alone, the drive given up, the missing files and
    /// the files held back for their times.
    /// </summary>
    private static string[] NoticeOpenings(ScanResult scan) =>
    [
        LeftAloneReport.Line(scan.LeftAloneCount),
        SourcesGivenUpReport.CommandLine([DriveE]),
        Opening(Strings.Cli_MissingFromDisk_Singular),
        RecentChangeReport.Line(scan),
    ];

    /// <summary>
    /// The run's one outcome entry is the HardError the catch-all writes for the guard's first
    /// failed write, naming the crash.log entry the guard made, and the last line printed is
    /// the catch-all's own, on a line of its own.
    /// </summary>
    private static void AssertEndsOnTheErrorForTheFailedWrite(RunResult run)
    {
        var failure = run.Failure ?? throw new Xunit.Sdk.XunitException("The guard recorded no failed write.");
        var (crashPath, written) = failure.CrashLog;
        var typeName = failure.Exception.GetType().Name;
        var summary = Assert.Single(run.Entries, CliRunFixtures.IsSummary);
        Assert.Equal(CliEventClass.HardError, summary.Class);
        Assert.Equal(MachineContract.English(() => written
            ? string.Format(Strings.Cli_EventLogHardError, "/s", typeName, crashPath)
            : string.Format(Strings.Cli_EventLogHardError_NoLog, "/s", typeName)), summary.Text);

        var lines = run.Stdout.Split(Environment.NewLine);
        Assert.Equal(written
            ? string.Format(Strings.Cli_GenericError, typeName, crashPath)
            : string.Format(Strings.Cli_GenericError_NoLog, typeName), lines[^2]);
        Assert.Equal(string.Empty, lines[^1]);
    }

    // A sentence's words up to its first placeholder, which no name or count changes.
    private static string Opening(string value) =>
        value.Contains('{') ? value[..value.IndexOf('{')] : value;

    private sealed record RunResult(
        int ExitCode, string Stdout, IReadOnlyList<(CliEventClass Class, string Text)> Entries,
        OutputFailure? Failure);

    private static async Task<RunResult> Run(string arg, ScanResult result, StringWriter? console = null)
    {
        var scan = Substitute.For<IFileSystemScanService>();
        scan.ScanAsync(Arg.Any<IProgress<ScanProgressUpdate>?>(), Arg.Any<CancellationToken>())
            .Returns(result);

        var reboot = Substitute.For<IPendingRebootService>();
        reboot.Check().Returns(PendingRebootResult.Clean);

        var reverifier = Substitute.For<IRemovableReverifier>();
        reverifier.ReverifyAsync(Arg.Any<IReadOnlyList<string>>(), Arg.Any<CancellationToken>())
            .Returns(new ReverifyResult(result.RemovableFiles.Select(f => f.FullPath).ToList(), Array.Empty<string>()));

        var deleter = Substitute.For<IDeleteFilesService>();
        deleter.DeleteFilesAsync(Arg.Any<IEnumerable<string>>(), Arg.Any<UnderLeaseClaims>(),
                Arg.Any<IProgress<OperationProgress>?>(), Arg.Any<CancellationToken>())
            .Returns(new DeleteResult(result.RemovableFiles.Count, Array.Empty<FileOperationError>()));

        var services = CliRunFixtures.Services(scan, reboot: reboot, reverifier: reverifier, delete: deleter);

        var invocation = arg == "/d"
            ? new CliInvocation(CliCommand.Delete, null, null)
            : new CliInvocation(CliCommand.ScanOnly, null, null);

        // Console.SetOut and the recorder are process-global; the assembly disables test
        // parallelisation, which is what makes both safe to read back here. A failing
        // console goes behind the guard, as Main puts the console there.
        var original = Console.Out;
        using var buffer = console ?? new StringWriter();
        try
        {
            var guard = console is null ? null : new ConsoleGuard(buffer);
            Console.SetOut(guard ?? (TextWriter)buffer);
            EventLogRecorder.Clear();
            var exitCode = await Program.RunWorkAsync(
                arg, invocation, CancellationToken.None, services, output: guard);
            return new RunResult(exitCode, buffer.ToString(), EventLogRecorder.Entries, guard?.FirstFailure);
        }
        finally
        {
            Console.SetOut(original);
        }
    }
}
