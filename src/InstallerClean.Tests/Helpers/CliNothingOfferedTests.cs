using InstallerClean.Cli;
using InstallerClean.Helpers;
using InstallerClean.Models;
using InstallerClean.Resources;
using InstallerClean.Services;
using NSubstitute;

namespace InstallerClean.Tests.Helpers;

/// <summary>
/// What the command line says on a run with nothing to offer, driven through the real
/// work method rather than through the strings.
///
/// EVERY SUCH RUN ENDS ITS SUMMARY THE SAME WAY: the line saying nothing was found, then
/// the line counting the files the scan left alone. The count is the window's, so the
/// fixtures set up the three populations it takes in: registered files on the disk,
/// registered files whose file has gone, and files the scan withheld.
///
/// WHAT KEPT A FILE BACK DOES NOT CHANGE WHAT IS PRINTED. The wholesale arm, the per-file
/// arms and the arms that keep a file for a program Windows still has installed all give
/// the same two lines, so one theory walks the split's members. The reasons live in the
/// Application log's entries and in the opt-in report.
///
/// THE ONE EXCEPTION IS A LINE THAT STANDS IN PLACE OF THE CLEAN ONE: the line about files
/// held back for their times, and the line naming the drives and shares the scan stopped
/// waiting for. Each says the files it speaks of were kept, so the clean line is not
/// printed over it.
///
/// THE FIXTURES ARE WHAT THIS FILE IS. The branch below is taken only where the scan
/// offers nothing, so a fixture with a file in its offer would make every assertion about
/// it over a run that never took it. Read what each test SETS UP rather than what it
/// asserts: they differ in the withheld list and in the split that says what put those
/// files there.
/// </summary>
public class CliNothingOfferedTests
{
    private const string HeldA = @"C:\Windows\Installer\a.msi";
    private const string HeldB = @"C:\Windows\Installer\b.msi";

    [Fact]
    public async Task A_folder_with_nothing_to_offer_gets_the_clean_line_and_the_count()
    {
        var (exit, stdout) = await Run(Scan(registered: 3, withheld: 0, split: default));

        Assert.Equal(CliExitCode.Ok, exit);
        Assert.Equal(Lines(Strings.Cli_FoundNoOrphans, LeftAlone(3)), AfterScanningLine(stdout));
    }

    [Fact]
    public async Task The_count_is_the_windows_own_and_takes_in_the_withheld_files_but_not_a_missing_one()
    {
        // Five registered files on the disk, two whose file has gone and two withheld.
        // The window's count is the five and the two withheld; a registration whose file
        // is not in the folder was not left alone. The result's own property is held
        // against the figure so the two cannot be changed apart.
        var result = Scan(registered: 5, withheld: 2, split: new WithholdingSplit(WholesaleCount: 2),
            missing: 2, census: ProductNobodyCouldAsk);

        var (_, stdout) = await Run(result);

        Assert.Equal(7, result.LeftAloneCount);
        Assert.Equal(Lines(Strings.Cli_FoundNoOrphans, LeftAlone(7)), AfterScanningLine(stdout));
    }

    [Fact]
    public async Task One_file_left_alone_takes_the_one_form()
    {
        var (_, stdout) = await Run(Scan(registered: 1, withheld: 0, split: default));

        Assert.Equal(
            Lines(Strings.Cli_FoundNoOrphans,
                string.Format(Strings.Summary_RegisteredStillUsed_Singular, DisplayHelpers.FormatCount(1))),
            AfterScanningLine(stdout));
    }

    [Fact]
    public async Task A_machine_with_no_registered_file_prints_the_count_at_nought()
    {
        // Printed on every scan, so a count of nought is a line like any other.
        var (_, stdout) = await Run(Scan(registered: 0, withheld: 0, split: default));

        Assert.Equal(Lines(Strings.Cli_FoundNoOrphans, LeftAlone(0)), AfterScanningLine(stdout));
    }

    [Theory]
    [InlineData("wholesale")]
    [InlineData("identity")]
    [InlineData("declared product unestablished")]
    [InlineData("screen unanswered")]
    [InlineData("declared patch unestablished")]
    [InlineData("second copy unestablished")]
    [InlineData("declared product installed")]
    [InlineData("declared patch registered")]
    [InlineData("age unestablished")]
    [InlineData("containment refused")]
    [InlineData("containment unestablished")]
    public async Task Whatever_kept_the_files_back_the_run_prints_the_clean_line_and_the_count(string arm)
    {
        // Two files held back by one arm. The lines are the same for every arm, and
        // nothing printed names a cause: the stdout is the scanning line and these two.
        var split = arm switch
        {
            "wholesale" => new WithholdingSplit(WholesaleCount: 2),
            "identity" => new WithholdingSplit(IdentityUnestablishedCount: 2),
            "declared product unestablished" => new WithholdingSplit(DeclaredProductUnestablishedCount: 2),
            "screen unanswered" => new WithholdingSplit(ScreenUnansweredCount: 2),
            "declared patch unestablished" => new WithholdingSplit(DeclaredPatchUnestablishedCount: 2),
            "second copy unestablished" => new WithholdingSplit(SecondCopyUnestablishedCount: 2),
            "declared product installed" => new WithholdingSplit(DeclaredProductInstalledCount: 2),
            "declared patch registered" => new WithholdingSplit(DeclaredPatchRegisteredCount: 2),
            "age unestablished" => new WithholdingSplit(AgeUnestablishedCount: 2),
            "containment refused" => new WithholdingSplit(ContainmentRefusedCount: 2),
            "containment unestablished" => new WithholdingSplit(ContainmentUnestablishedCount: 2),
            _ => throw new ArgumentOutOfRangeException(nameof(arm), arm, "an arm this theory does not set up"),
        };

        var (exit, stdout) = await Run(Scan(
            registered: 10, withheld: 2, split: split,
            census: arm == "wholesale" ? ProductNobodyCouldAsk : default));

        Assert.Equal(CliExitCode.Ok, exit);
        Assert.Equal(Lines(Strings.Cli_FoundNoOrphans, LeftAlone(12)), AfterScanningLine(stdout));
    }

    [Theory]
    [InlineData(2, 0)]
    [InlineData(1, 1)]
    public async Task A_run_holding_files_for_their_times_prints_the_count_then_the_line_about_them_in_place_of_the_clean_line(
        int forTheirTimes, int forAnInstalledProgram)
    {
        // Kept for their times with the signals clear, alone or beside a file kept for a
        // program Windows still has installed: the line about recently changed files
        // stands where the clean line would be, after the count.
        var result = Scan(
            registered: 4, withheld: 2,
            split: new WithholdingSplit(
                NotBeforeTheClockCount: forTheirTimes, DeclaredProductInstalledCount: forAnInstalledProgram),
            notBeforeTheClockBytes: 1024 * forTheirTimes,
            atScan: PendingRebootResult.Clean);

        var (exit, stdout) = await Run(result);

        Assert.Equal(CliExitCode.Ok, exit);
        var line = RecentChangeReport.Line(result);
        Assert.NotEqual(string.Empty, line);
        Assert.Equal(Lines(LeftAlone(6), line), AfterScanningLine(stdout));
        Assert.DoesNotContain(Strings.Cli_FoundNoOrphans, stdout, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData(PendingRebootReason.MsiExecuteMutexHeld)]
    [InlineData(PendingRebootReason.InstallerInProgress)]
    [InlineData(PendingRebootReason.PendingRenameInCache)]
    [InlineData(PendingRebootReason.RegistryCheckUnreadable)]
    [InlineData(PendingRebootReason.MsiExecuteMutexUnreadable)]
    public async Task A_run_whose_walked_files_were_held_for_a_signal_prints_the_signal_s_line_after_the_count(
        PendingRebootReason reason)
    {
        // Every walked file held because a signal was set just after the scan's clock: the
        // signal's own line says why, and the line about recently changed files is absent.
        var result = Scan(
            registered: 4, withheld: 2,
            split: new WithholdingSplit(ScanSignalCount: 2),
            atScan: PendingRebootResult.Block(reason));

        var (exit, stdout) = await Run(result);

        Assert.Equal(CliExitCode.Ok, exit);
        Assert.Equal(
            Lines(Strings.Cli_FoundNoOrphans, LeftAlone(6), ScanSignalReport.CommandLine(reason)),
            AfterScanningLine(stdout));
        Assert.Equal(string.Empty, RecentChangeReport.Line(result));
    }

    [Fact]
    public async Task A_run_whose_walked_files_were_held_for_the_clock_prints_the_clock_s_line_after_the_count()
    {
        // The system clock went back while the scan read the files' times, so every walked
        // file its times passed was held for that. The clock's own line says why, with no
        // signal's line beside it.
        var result = Scan(
            registered: 4, withheld: 2,
            split: new WithholdingSplit(ClockSetBackCount: 2),
            atScan: PendingRebootResult.Clean);

        var (exit, stdout) = await Run(result);

        Assert.Equal(CliExitCode.Ok, exit);
        Assert.Equal(
            Lines(Strings.Cli_FoundNoOrphans, LeftAlone(6), Strings.Cli_ScanSignal_ClockSetBack),
            AfterScanningLine(stdout));
        Assert.Equal(Strings.Cli_ScanSignal_ClockSetBack, ScanSignalReport.CommandLine(result));
    }

    [Fact]
    public async Task A_run_holding_no_file_for_its_times_says_nothing_about_times()
    {
        // The same reader of the split, on a run whose held files were kept for another
        // reason, so the absence is of the line and not of a run.
        var (_, stdout) = await Run(Scan(
            registered: 4, withheld: 2,
            split: new WithholdingSplit(DeclaredProductUnestablishedCount: 2)));

        // The words after the size, which no count or size changes.
        var line = Strings.Completion_RecentChange_Plural;
        var words = line[(line.IndexOf("({2})", StringComparison.Ordinal) + 5)..];
        Assert.NotEqual(string.Empty, words.Trim());
        Assert.DoesNotContain(words, stdout, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Superseded_patches_the_scan_held_back_add_no_line_of_their_own()
    {
        // Distinct figures: one superseded patch held back for its products, two the
        // containment check refused and four it could not answer for. They are registered
        // rows and so already in the count; what the run adds for them is the
        // Application log's entry, which CliNoticeEntryTests reads.
        var result = Scan(registered: 9, withheld: 0, split: default) with
        {
            WithheldCount = 1,
            SupersededContainmentRefusedCount = 2,
            SupersededContainmentUnestablishedCount = 4,
        };

        var (exit, stdout) = await Run(result);

        Assert.Equal(CliExitCode.Ok, exit);
        Assert.Equal(7, result.SupersededHeldBackCount);
        Assert.Equal(Lines(Strings.Cli_FoundNoOrphans, LeftAlone(9)), AfterScanningLine(stdout));
    }

    // ---- fixtures ----

    private static string LeftAlone(int count) => LeftAloneReport.Line(count);

    /// <summary>
    /// A census with the RegistryProductUnaskable leg set, a product the registry names that
    /// the scan could not ask about, so a fixture that wants the wholesale branch has one.
    /// The legs are read off the census rather than off the split, so a wholesale count on
    /// its own would leave the breakdown empty and a test about it passing over nothing.
    /// </summary>
    private static EnumerationCensus ProductNobodyCouldAsk =>
        new(UnansweredProductCount: 1);

    // The lines joined the way the console writes them, ending in a line break.
    private static string Lines(params string[] lines) =>
        string.Concat(lines.Select(line => line + Environment.NewLine));

    // Everything after the scanning line, which names the folder and so differs by machine.
    private static string AfterScanningLine(string stdout)
    {
        var at = stdout.IndexOf(Environment.NewLine, StringComparison.Ordinal);
        Assert.True(at >= 0, "the run printed no line at all");
        return stdout[(at + Environment.NewLine.Length)..];
    }

    private static ScanResult Scan(
        int registered, int withheld, WithholdingSplit split,
        EnumerationCensus census = default, long notBeforeTheClockBytes = 0,
        PendingRebootResult? atScan = null, int missing = 0) =>
        new(Array.Empty<OrphanedFile>(), Registered(registered, missing), 0,
            Census: census,
            WithheldFiles: Held(withheld),
            WithheldBy: split,
            WithheldNotBeforeTheClockBytes: notBeforeTheClockBytes,
            InstallerActivityAtScan: atScan);

    // Registered files on the disk, then registrations whose file has gone.
    private static RegisteredPackage[] Registered(int onDisk, int missing) =>
        Enumerable.Range(0, onDisk)
            .Select(i => new RegisteredPackage($@"C:\Windows\Installer\r{i}.msi", "Product", $"{{{i}}}"))
            .Concat(Enumerable.Range(0, missing)
                .Select(i => new RegisteredPackage(
                    $@"C:\Windows\Installer\gone{i}.msi", "Product", $"{{gone-{i}}}", FileExists: false)))
            .ToArray();

    private static OrphanedFile[] Held(int n) =>
        n switch
        {
            0 => Array.Empty<OrphanedFile>(),
            1 => [new OrphanedFile(HeldA, 1024, false, false, false, "unclaimed")],
            _ => [new OrphanedFile(HeldA, 1024, false, false, false, "unclaimed"),
                  new OrphanedFile(HeldB, 1024, false, false, false, "unclaimed")],
        };

    private static async Task<(int ExitCode, string Stdout)> Run(ScanResult result)
    {
        var scan = Substitute.For<IFileSystemScanService>();
        scan.ScanAsync(Arg.Any<IProgress<ScanProgressUpdate>?>(), Arg.Any<CancellationToken>())
            .Returns(result);

        var reboot = Substitute.For<IPendingRebootService>();
        reboot.Check().Returns(PendingRebootResult.Clean);

        var services = CliRunFixtures.Services(scan, reboot: reboot);

        var original = Console.Out;
        using var buffer = new StringWriter();
        try
        {
            Console.SetOut(buffer);
            var exitCode = await Program.RunWorkAsync(
                "/d", new CliInvocation(CliCommand.Delete, null, null),
                CancellationToken.None, services);
            return (exitCode, buffer.ToString());
        }
        finally
        {
            Console.SetOut(original);
        }
    }
}
