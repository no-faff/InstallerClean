using InstallerClean.Cli;
using InstallerClean.Helpers;
using InstallerClean.Models;
using InstallerClean.Resources;
using InstallerClean.Services;
using NSubstitute;

namespace InstallerClean.Tests.Helpers;

/// <summary>
/// The 3000-band notices a scan writes to the Application channel beside its summary,
/// each driven through the real work method from a scan result that meets its
/// condition, and read back as the suite's recorder receives it: the class, which
/// decides the Event ID and entry type, and the English text.
/// </summary>
/// <remarks>
/// Each run is a <c>/s</c>, which writes its notices from the same method as a
/// <c>/d</c> or <c>/m</c> and touches no file. Every test also holds that the run
/// writes exactly one summary entry beside its notice, since a notice never stands in
/// for one.
/// </remarks>
public class CliNoticeEntryTests
{
    private const string Flag = "/s";
    private const string HeldA = @"C:\Windows\Installer\a.msi";
    private const string HeldB = @"C:\Windows\Installer\b.msi";

    [Fact]
    public async Task A_clean_scan_writes_its_summary_and_no_notice()
    {
        var entries = await Run(Clean);

        Assert.Single(entries);
        Assert.DoesNotContain(entries, entry => IsNotice(entry.Class));
    }

    [Fact]
    public async Task Program_entries_the_scan_could_not_check_write_3000_with_their_count()
    {
        var entries = await Run(Clean with { UnaccountedProductCount = 2 });

        AssertNotice(entries, CliEventClass.ScanRecordsIncompleteNotice,
            MachineContract.English(() => string.Format(Strings.Cli_EventLogScanWithheld_Plural, Flag, 2)));
    }

    [Fact]
    public async Task Patch_files_the_scan_could_not_match_write_3000_with_their_count()
    {
        var entries = await Run(Clean with { Census = new EnumerationCensus(UnattributedPatchFileCount: 3) });

        AssertNotice(entries, CliEventClass.ScanRecordsIncompleteNotice,
            MachineContract.English(() => string.Format(Strings.Cli_EventLogScanWithheldPatchFiles_Plural, Flag, 3)));
    }

    [Fact]
    public async Task Missing_files_a_program_may_still_need_write_3001_with_their_count()
    {
        var entries = await Run(Clean with { MissingAffectedCount = 2 });

        AssertNotice(entries, CliEventClass.ScanMissingFilesNotice,
            MachineContract.English(() => string.Format(Strings.Cli_EventLogMissingFromDisk_Plural, Flag, 2)));
    }

    [Fact]
    public async Task Files_judged_one_at_a_time_and_held_back_write_3002_in_its_per_file_form()
    {
        var entries = await Run(Clean with
        {
            WithheldFiles = Held,
            WithheldBy = new WithholdingSplit(DeclaredProductUnestablishedCount: 2),
        });

        AssertNotice(entries, CliEventClass.ScanWalkHeldBackNotice,
            MachineContract.English(() => string.Format(Strings.Cli_EventLogNothingOfferedPerFileNotice,
                Flag, 2, DisplayHelpers.PluraliseFile(2))));
    }

    [Fact]
    public async Task A_walk_offer_withheld_whole_writes_3002_in_its_wholesale_form()
    {
        var entries = await Run(Clean with
        {
            WithheldFiles = Held,
            WithheldBy = new WithholdingSplit(WholesaleCount: 2),
            Census = new EnumerationCensus(UnansweredProductCount: 1),
        });

        AssertNotice(entries, CliEventClass.ScanWalkHeldBackNotice,
            MachineContract.English(() => string.Format(Strings.Cli_EventLogNothingOfferedNotice,
                Flag, 2, DisplayHelpers.PluraliseFile(2))));
    }

    [Fact]
    public async Task A_superseded_patch_held_back_writes_3003_with_its_count()
    {
        var entries = await Run(Clean with { WithheldCount = 1 });

        AssertNotice(entries, CliEventClass.ScanSupersededHeldBackNotice,
            MachineContract.English(() => string.Format(Strings.Cli_EventLogSupersededHeldBack_Singular, Flag, 1)));
    }

    // ---- fixtures ----

    private static ScanResult Clean =>
        new(Array.Empty<OrphanedFile>(), Array.Empty<RegisteredPackage>(), 0);

    private static OrphanedFile[] Held =>
        [new OrphanedFile(HeldA, 1024, false, false, false, "unclaimed"),
         new OrphanedFile(HeldB, 1024, false, false, false, "unclaimed")];

    private static bool IsNotice(CliEventClass outcome) =>
        CliContract.EventIdFor(outcome) / 1000 == 3;

    /// <summary>
    /// One entry of the notice's class, carrying <paramref name="expected"/>, and one
    /// summary beside it. Each caller builds the expected text inside
    /// MachineContract.English, the en-GB scope the write site builds in.
    /// </summary>
    private static void AssertNotice(
        IReadOnlyList<(CliEventClass Class, string Text)> entries,
        CliEventClass notice, string expected)
    {
        var written = Assert.Single(entries, entry => entry.Class == notice);
        Assert.Equal(expected, written.Text);
        Assert.Single(entries, entry => !IsNotice(entry.Class));
    }

    private static async Task<IReadOnlyList<(CliEventClass Class, string Text)>> Run(ScanResult result)
    {
        var scan = Substitute.For<IFileSystemScanService>();
        scan.ScanAsync(Arg.Any<IProgress<ScanProgressUpdate>?>(), Arg.Any<CancellationToken>())
            .Returns(result);

        var services = CliRunFixtures.Services(scan);

        // Console.SetOut and the recorder are process-global; the assembly disables test
        // parallelisation, which is what makes both safe to read back here.
        var original = Console.Out;
        using var buffer = new StringWriter();
        try
        {
            Console.SetOut(buffer);
            EventLogRecorder.Clear();
            await Program.RunWorkAsync(
                Flag, new CliInvocation(CliCommand.ScanOnly, null, null),
                CancellationToken.None, services);
            return EventLogRecorder.Entries;
        }
        finally
        {
            Console.SetOut(original);
        }
    }
}
