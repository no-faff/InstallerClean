using InstallerClean.Cli;
using InstallerClean.Helpers;
using InstallerClean.Models;
using InstallerClean.Resources;
using InstallerClean.Services;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;

namespace InstallerClean.Tests.Helpers;

/// <summary>
/// What the command line says on a run with nothing to offer, driven through the real
/// work method rather than through the strings.
///
/// THREE MACHINES REACH THAT BRANCH AND THEY ARE NOT ONE THING. The folder held nothing
/// this scan can offer; a rule about the machine's records emptied the walk-derived
/// offer in one go; or the files were judged one at a time and none could be cleared.
/// The clean line is a statement about the FOLDER and only the first has earned it, and
/// the two withholding sentences each name something the other's machine did not meet.
///
/// THE FIXTURES ARE WHAT THIS FILE IS. Every other file that drives this method scripts
/// a scan with two removable files in it, so the branch below is reached by none of
/// them and every assertion about it would be made over a run that never took it. Read
/// what each test SETS UP rather than what it asserts: they differ in the withheld list
/// and in the split that says what put those files there, which is exactly the pair the
/// reading is derived from.
/// </summary>
public class CliNothingOfferedTests
{
    private const string HeldA = @"C:\Windows\Installer\a.msi";
    private const string HeldB = @"C:\Windows\Installer\b.msi";

    [Fact]
    public async Task A_folder_with_nothing_to_offer_gets_the_clean_line()
    {
        var (exit, stdout) = await Run(Scan(withheld: 0, split: default));

        Assert.Equal(CliExitCode.Ok, exit);
        Assert.Contains(Strings.Cli_FoundNoOrphans, stdout, StringComparison.Ordinal);
        // The reason list belongs to a run that kept something back. A heading over
        // nothing on a clean machine would read as output that failed.
        Assert.DoesNotContain(Strings.Cli_WithheldReasons_Header, stdout, StringComparison.Ordinal);
    }

    [Fact]
    public async Task A_wholesale_withholding_gets_the_wholesale_line_and_not_the_clean_one()
    {
        var (exit, stdout) = await Run(Scan(
            withheld: 2,
            split: new WithholdingSplit(WholesaleCount: 2),
            wholesaleFlag: true,
            census: SecondInstanceUnruled));

        Assert.Equal(CliExitCode.Ok, exit);
        Assert.Contains(Expected(Strings.Cli_NothingOffered_Plural, 2), stdout, StringComparison.Ordinal);
        Assert.DoesNotContain(Strings.Cli_FoundNoOrphans, stdout, StringComparison.Ordinal);
        // AND NOT THE LISTED LEAD, which is for a machine offered something beside the
        // withheld half and is gated on the offer having anything in it. This machine
        // was offered nothing, so printing it here would put two sentences about one
        // folder on screen, the second describing a run the first says did not happen.
        Assert.DoesNotContain(Expected(Strings.Cli_NothingListed_Plural, 2), stdout, StringComparison.Ordinal);
    }

    [Fact]
    public async Task A_per_file_withholding_gets_its_own_line_and_not_the_wholesale_one()
    {
        // THE MACHINE THE PER-FILE LINE EXISTS FOR. Nothing emptied the offer wholesale; the
        // declared-product screen kept two files and the folder is not clean. Before
        // there was a line for it this run printed "Found no unneeded files", which is
        // a statement about a folder that has two files nobody vouched for in it.
        var (exit, stdout) = await Run(Scan(
            withheld: 2,
            split: new WithholdingSplit(DeclaredProductInstalledCount: 2)));

        Assert.Equal(CliExitCode.Ok, exit);
        Assert.Contains(Expected(Strings.Cli_NothingOfferedPerFile_Plural, 2), stdout, StringComparison.Ordinal);
        Assert.DoesNotContain(Strings.Cli_FoundNoOrphans, stdout, StringComparison.Ordinal);
        // Named rather than merely different from the clean line: the wholesale
        // sentence is also different from it, and is false of this machine.
        Assert.DoesNotContain(Expected(Strings.Cli_NothingOffered_Plural, 2), stdout, StringComparison.Ordinal);
    }

    [Fact]
    public async Task A_run_that_withheld_both_ways_gets_the_per_file_line()
    {
        // The wholesale sentence is false of the file the identity pass took, and the
        // per-file one is true of every file here, so the superordinate is what this
        // machine is told even though its wholesale branch did fire.
        var (exit, stdout) = await Run(Scan(
            withheld: 2,
            split: new WithholdingSplit(IdentityUnestablishedCount: 1, WholesaleCount: 1),
            wholesaleFlag: true,
            census: SecondInstanceUnruled));

        Assert.Equal(CliExitCode.Ok, exit);
        Assert.Contains(Expected(Strings.Cli_NothingOfferedPerFile_Plural, 2), stdout, StringComparison.Ordinal);
        Assert.DoesNotContain(Expected(Strings.Cli_NothingOffered_Plural, 2), stdout, StringComparison.Ordinal);
    }

    [Fact]
    public async Task The_reason_list_carries_a_line_from_each_half_of_a_mixed_run()
    {
        // ONE HEADING OVER BOTH SETS. The legs say what the run could not establish
        // about the records; the arms say which per-file decision kept a file. A run
        // that met both has to report both, or a reader is told one of the conditions
        // it actually met did not hold.
        var (_, stdout) = await Run(Scan(
            withheld: 2,
            split: new WithholdingSplit(DeclaredProductInstalledCount: 1, WholesaleCount: 1),
            wholesaleFlag: true,
            census: SecondInstanceUnruled));

        Assert.Contains(Strings.Cli_WithheldReasons_Header, stdout, StringComparison.Ordinal);
        Assert.Contains(Program.LineFor(WithholdingLeg.SecondInstanceNotRuledOut), stdout, StringComparison.Ordinal);
        Assert.Contains(Program.LineFor(WithholdingSplitArm.DeclaredProductInstalled), stdout, StringComparison.Ordinal);
        // And nothing it did not meet.
        Assert.DoesNotContain(Program.LineFor(WithholdingSplitArm.ScreenUnanswered), stdout, StringComparison.Ordinal);
    }

    [Fact]
    public async Task A_per_file_withholding_reports_its_reason_with_no_leg_to_carry_it()
    {
        // The half that had no surface at all before: no leg fired, so the whole
        // breakdown used to be skipped and this machine was told nothing about why.
        var (_, stdout) = await Run(Scan(
            withheld: 1,
            split: new WithholdingSplit(ScreenUnansweredCount: 1)));

        Assert.Contains(Strings.Cli_WithheldReasons_Header, stdout, StringComparison.Ordinal);
        Assert.Contains(Program.LineFor(WithholdingSplitArm.ScreenUnanswered), stdout, StringComparison.Ordinal);
    }

    [Fact]
    public async Task The_one_form_names_the_size_and_never_the_numeral()
    {
        // A count of one is reachable, being a folder holding a single unclaimed file,
        // and the plural form renders "held back all 1 files" for it.
        var (_, stdout) = await Run(Scan(
            withheld: 1,
            split: new WithholdingSplit(DeclaredProductInstalledCount: 1)));

        Assert.Contains(Expected(Strings.Cli_NothingOfferedPerFile_Singular, 1), stdout, StringComparison.Ordinal);
        Assert.DoesNotContain("1 files", stdout, StringComparison.Ordinal);
    }

    // ---- fixtures ----

    /// <summary>
    /// A census with the second-instance leg set, so a fixture that wants the wholesale
    /// branch's own reason line has one to print. The legs are read off the census
    /// rather than off the flag, so setting the flag alone would leave the breakdown
    /// empty and a test about it passing over nothing.
    /// </summary>
    private static EnumerationCensus SecondInstanceUnruled =>
        new(InstanceProductCount: 1);

    private static string Expected(string value, int count) =>
        string.Format(value, count, DisplayHelpers.PluraliseFile(count),
            DisplayHelpers.FormatSize(count * 1024L));

    private static ScanResult Scan(
        int withheld, WithholdingSplit split,
        bool wholesaleFlag = false, EnumerationCensus census = default) =>
        new(Array.Empty<OrphanedFile>(), Array.Empty<RegisteredPackage>(), 0,
            Census: census,
            WithheldFiles: Held(withheld),
            WalkOfferWithheldWholesale: wholesaleFlag,
            WithheldBy: split);

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

        var services = new ServiceCollection()
            .AddSingleton(scan)
            .AddSingleton(reboot)
            .AddSingleton(Substitute.For<IRemovableReverifier>())
            .AddSingleton(Substitute.For<IDeleteFilesService>())
            .AddSingleton(Substitute.For<IMoveFilesService>())
            .AddSingleton(Substitute.For<ISettingsService>())
            .BuildServiceProvider();

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
