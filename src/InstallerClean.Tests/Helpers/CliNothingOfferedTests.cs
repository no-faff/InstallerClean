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
/// this scan can offer, or only files it keeps back without a notice, such as a file
/// declaring a program Windows still has installed; a rule about the machine's records
/// emptied the walk-derived offer in one go; or the files were judged one at a time and
/// none could be cleared.
/// The clean line is printed for the first alone, and the two withholding sentences
/// each name something the other's machine did not meet.
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
    public async Task A_run_whose_every_held_file_is_kept_for_an_installed_program_gets_the_clean_line()
    {
        // Every held file was kept for a program Windows still has installed, so the
        // files are left alone like any registered file and the run prints the clean
        // line.
        var (exit, stdout) = await Run(Scan(
            withheld: 2,
            split: new WithholdingSplit(DeclaredProductInstalledCount: 2),
            positiveBytes: 2048));

        Assert.Equal(CliExitCode.Ok, exit);
        Assert.Contains(Strings.Cli_FoundNoOrphans, stdout, StringComparison.Ordinal);
        // By each sentence's opening words rather than its formatted whole, so a line
        // printed with any count or size at all is caught.
        foreach (var line in new[]
                 {
                     Strings.Cli_NothingOfferedPerFile_Singular, Strings.Cli_NothingOfferedPerFile_Plural,
                     Strings.Cli_NothingOffered_Singular, Strings.Cli_NothingOffered_Plural,
                     Strings.Cli_NothingListedPerFile_Singular, Strings.Cli_NothingListedPerFile_Plural,
                 })
            Assert.DoesNotContain(Opening(line), stdout, StringComparison.Ordinal);
        Assert.DoesNotContain(Strings.Cli_WithheldReasons_Header, stdout, StringComparison.Ordinal);
    }

    [Fact]
    public async Task A_run_whose_every_held_file_is_kept_for_its_patch_s_registrations_gets_the_clean_line()
    {
        // Every held file is a patch copy kept because Windows holds a registration of
        // the patch it declares, so the files are left alone like a file kept for an
        // installed program and the run prints the clean line.
        var (exit, stdout) = await Run(Scan(
            withheld: 2,
            split: new WithholdingSplit(DeclaredPatchRegisteredCount: 2),
            patchBytes: 2048));

        Assert.Equal(CliExitCode.Ok, exit);
        Assert.Contains(Strings.Cli_FoundNoOrphans, stdout, StringComparison.Ordinal);
        foreach (var line in new[]
                 {
                     Strings.Cli_NothingOfferedPerFile_Singular, Strings.Cli_NothingOfferedPerFile_Plural,
                     Strings.Cli_NothingOffered_Singular, Strings.Cli_NothingOffered_Plural,
                     Strings.Cli_NothingListedPerFile_Singular, Strings.Cli_NothingListedPerFile_Plural,
                 })
            Assert.DoesNotContain(Opening(line), stdout, StringComparison.Ordinal);
        Assert.DoesNotContain(Strings.Cli_WithheldReasons_Header, stdout, StringComparison.Ordinal);
    }

    [Fact]
    public async Task A_per_file_withholding_gets_its_own_line_and_not_the_wholesale_one()
    {
        // THE MACHINE THE PER-FILE LINE EXISTS FOR. Nothing emptied the offer wholesale;
        // the declared-product screen could not settle two files, so the folder holds two
        // files nobody vouched for and "Found no unneeded files" is not printed for it.
        var (exit, stdout) = await Run(Scan(
            withheld: 2,
            split: new WithholdingSplit(DeclaredProductUnestablishedCount: 2)));

        Assert.Equal(CliExitCode.Ok, exit);
        Assert.Contains(Expected(Strings.Cli_NothingOfferedPerFile_Plural, 2), stdout, StringComparison.Ordinal);
        Assert.DoesNotContain(Strings.Cli_FoundNoOrphans, stdout, StringComparison.Ordinal);
        // Named rather than merely different from the clean line: the wholesale
        // sentence is also different from it, and is false of this machine.
        Assert.DoesNotContain(Expected(Strings.Cli_NothingOffered_Plural, 2), stdout, StringComparison.Ordinal);
        // AND NOT THE LISTED LEAD, on its own branch. The wholesale fixture asserts the
        // same thing about the other one, and the guard has two branches to widen: one
        // that lets the lead onto every machine and one that lets it onto per-file
        // machines alone. Each fixture holds the branch its own machine takes.
        Assert.DoesNotContain(Expected(Strings.Cli_NothingListedPerFile_Plural, 2), stdout, StringComparison.Ordinal);
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
            split: new WithholdingSplit(IdentityUnestablishedCount: 1, WholesaleCount: 1),
            census: SecondInstanceUnruled));

        Assert.Contains(Strings.Cli_WithheldReasons_Header, stdout, StringComparison.Ordinal);
        Assert.Contains(Program.LineFor(WithholdingLeg.SecondInstanceNotRuledOut), stdout, StringComparison.Ordinal);
        Assert.Contains(Program.LineFor(WithholdingSplitArm.IdentityUnestablished), stdout, StringComparison.Ordinal);
        // And nothing it did not meet.
        Assert.DoesNotContain(Program.LineFor(WithholdingSplitArm.ScreenUnanswered), stdout, StringComparison.Ordinal);
    }

    [Fact]
    public async Task A_file_kept_for_an_installed_program_is_left_out_of_the_line_and_its_reasons()
    {
        // Two files held back: one the declared-product-installed arm counted and one
        // the screen could not settle. The line counts and sizes the second alone, and
        // the reasons under it are the second's alone.
        var (exit, stdout) = await Run(Scan(
            withheld: 2,
            split: new WithholdingSplit(DeclaredProductInstalledCount: 1, DeclaredProductUnestablishedCount: 1),
            positiveBytes: 1024));

        Assert.Equal(CliExitCode.Ok, exit);
        Assert.Contains(Expected(Strings.Cli_NothingOfferedPerFile_Singular, 1), stdout, StringComparison.Ordinal);
        Assert.DoesNotContain(Expected(Strings.Cli_NothingOfferedPerFile_Plural, 2), stdout, StringComparison.Ordinal);
        Assert.Contains(Strings.Cli_WithheldReasons_Header, stdout, StringComparison.Ordinal);
        Assert.Contains(Program.LineFor(WithholdingSplitArm.DeclaredProductUnestablished), stdout, StringComparison.Ordinal);
        Assert.DoesNotContain(Program.LineFor(WithholdingSplitArm.ScreenUnanswered), stdout, StringComparison.Ordinal);
        Assert.DoesNotContain(Program.LineFor(WithholdingSplitArm.IdentityUnestablished), stdout, StringComparison.Ordinal);
    }

    [Fact]
    public async Task A_patch_copy_the_scan_could_not_settle_gets_the_per_file_line_and_its_own_reason()
    {
        // The patch half's unsettled arm is not one of the silent arms: the line counts
        // its file, and the reason under it is that arm's own. The heading and the
        // reason are each counted, so a reason falling back to the heading's text reads
        // as the heading printed twice rather than as a reason found. The fixture's file
        // names are the shared ones; this host reads counts and never a name.
        var (exit, stdout) = await Run(Scan(
            withheld: 1,
            split: new WithholdingSplit(DeclaredPatchUnestablishedCount: 1)));

        Assert.Equal(CliExitCode.Ok, exit);
        Assert.Contains(Expected(Strings.Cli_NothingOfferedPerFile_Singular, 1), stdout, StringComparison.Ordinal);
        Assert.Equal(1, Occurrences(stdout, Strings.Cli_WithheldReasons_Header));
        Assert.Equal(1, Occurrences(stdout, Program.LineFor(WithholdingSplitArm.DeclaredPatchUnestablished)));
        Assert.DoesNotContain(Program.LineFor(WithholdingSplitArm.DeclaredProductUnestablished), stdout, StringComparison.Ordinal);
    }

    [Fact]
    public async Task A_package_and_a_patch_copy_the_scan_could_not_settle_each_get_their_own_reason()
    {
        // One of each half's unsettled arm. Two lines, each printed once, the package's
        // first as the split declares them: a line shared between the two would state
        // one half's cause over the other's file.
        var (_, stdout) = await Run(Scan(
            withheld: 2,
            split: new WithholdingSplit(DeclaredProductUnestablishedCount: 1, DeclaredPatchUnestablishedCount: 1)));

        var product = Program.LineFor(WithholdingSplitArm.DeclaredProductUnestablished);
        var patch = Program.LineFor(WithholdingSplitArm.DeclaredPatchUnestablished);

        Assert.Contains(Expected(Strings.Cli_NothingOfferedPerFile_Plural, 2), stdout, StringComparison.Ordinal);
        Assert.Equal(1, Occurrences(stdout, Strings.Cli_WithheldReasons_Header));
        Assert.Equal(1, Occurrences(stdout, product));
        Assert.Equal(1, Occurrences(stdout, patch));
        Assert.True(
            stdout.IndexOf(product, StringComparison.Ordinal) < stdout.IndexOf(patch, StringComparison.Ordinal),
            "the package's reason should come before the patch copy's");
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
    public async Task A_run_whose_every_held_file_was_read_as_under_a_day_old_gets_the_clean_line()
    {
        // Kept for their age with their age read, which is left alone without a word
        // as the installed-program arm's files are.
        var (exit, stdout) = await Run(Scan(
            withheld: 2,
            split: new WithholdingSplit(UnderADayOldCount: 2),
            underADayOldBytes: 2048));

        Assert.Equal(CliExitCode.Ok, exit);
        Assert.Contains(Strings.Cli_FoundNoOrphans, stdout, StringComparison.Ordinal);
        Assert.DoesNotContain(Opening(Strings.Cli_NothingOfferedPerFile_Plural), stdout, StringComparison.Ordinal);
        Assert.DoesNotContain(Strings.Cli_WithheldReasons_Header, stdout, StringComparison.Ordinal);
    }

    [Fact]
    public async Task A_file_whose_age_was_not_established_gets_the_per_file_line_with_no_reasons_under_it()
    {
        // Counted by the per-file line, and no reason line speaks for it, so the
        // heading is not printed over nothing.
        var (exit, stdout) = await Run(Scan(
            withheld: 2,
            split: new WithholdingSplit(AgeUnestablishedCount: 2)));

        Assert.Equal(CliExitCode.Ok, exit);
        Assert.Contains(Expected(Strings.Cli_NothingOfferedPerFile_Plural, 2), stdout, StringComparison.Ordinal);
        Assert.DoesNotContain(Strings.Cli_FoundNoOrphans, stdout, StringComparison.Ordinal);
        Assert.DoesNotContain(Strings.Cli_WithheldReasons_Header, stdout, StringComparison.Ordinal);
    }

    [Fact]
    public async Task A_reason_list_that_would_leave_a_counted_file_out_is_not_printed()
    {
        // Two files in the line: one the screen could not settle, which has a reason
        // line, and one whose age was not established, which has none. A list under
        // the line would give a reason for one of the two files it counts, so the line
        // stands alone.
        var (exit, stdout) = await Run(Scan(
            withheld: 2,
            split: new WithholdingSplit(DeclaredProductUnestablishedCount: 1, AgeUnestablishedCount: 1)));

        Assert.Equal(CliExitCode.Ok, exit);
        Assert.Contains(Expected(Strings.Cli_NothingOfferedPerFile_Plural, 2), stdout, StringComparison.Ordinal);
        Assert.DoesNotContain(Strings.Cli_WithheldReasons_Header, stdout, StringComparison.Ordinal);
        Assert.DoesNotContain(Program.LineFor(WithholdingSplitArm.DeclaredProductUnestablished), stdout, StringComparison.Ordinal);
    }

    [Fact]
    public async Task The_one_form_names_the_size_and_never_the_numeral()
    {
        // A count of one is reachable, being a folder holding a single unclaimed file,
        // and the plural form renders "held back all 1 files" for it.
        var (_, stdout) = await Run(Scan(
            withheld: 1,
            split: new WithholdingSplit(DeclaredProductUnestablishedCount: 1)));

        Assert.Contains(Expected(Strings.Cli_NothingOfferedPerFile_Singular, 1), stdout, StringComparison.Ordinal);
        Assert.DoesNotContain("1 files", stdout, StringComparison.Ordinal);
    }

    // ---- fixtures ----

    /// <summary>
    /// A census with the second-instance leg set, so a fixture that wants the wholesale
    /// branch's own reason line has one to print. The legs are read off the census
    /// rather than off the split, so a wholesale count on its own would leave the
    /// breakdown empty and a test about it passing over nothing.
    /// </summary>
    private static EnumerationCensus SecondInstanceUnruled =>
        new(InstanceProductCount: 1);

    // A sentence's words up to its first placeholder, which no count or size changes.
    private static string Opening(string value) => value[..value.IndexOf('{')];

    // How many times a line appears in the output, compared ordinally and without
    // overlap.
    private static int Occurrences(string text, string value)
    {
        var count = 0;
        for (var at = text.IndexOf(value, StringComparison.Ordinal);
             at >= 0;
             at = text.IndexOf(value, at + value.Length, StringComparison.Ordinal))
            count++;
        return count;
    }

    private static string Expected(string value, int count) =>
        string.Format(value, count, DisplayHelpers.PluraliseFile(count),
            DisplayHelpers.FormatSize(count * 1024L));

    private static ScanResult Scan(
        int withheld, WithholdingSplit split,
        EnumerationCensus census = default, long positiveBytes = 0,
        long underADayOldBytes = 0, long patchBytes = 0) =>
        new(Array.Empty<OrphanedFile>(), Array.Empty<RegisteredPackage>(), 0,
            Census: census,
            WithheldFiles: Held(withheld),
            WithheldBy: split,
            WithheldDeclaredProductInstalledBytes: positiveBytes,
            WithheldUnderADayOldBytes: underADayOldBytes,
            WithheldDeclaredPatchRegisteredBytes: patchBytes);

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
