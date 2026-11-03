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
/// THREE MACHINES REACH THAT BRANCH AND THEY ARE NOT ONE THING. The folder held nothing
/// this scan can offer, or only files it keeps back without a notice, such as a file
/// declaring a program Windows still has installed; a rule about the machine's records
/// emptied the walk-derived offer in one go; or the files were judged one at a time and
/// none could be cleared.
/// The clean line is printed for the first alone, and there only where neither a line
/// about recently changed files nor a line naming a drive or share given up is printed.
/// The two withholding sentences each name something the other's machine did not meet.
///
/// THE FIXTURES ARE WHAT THIS FILE IS. The branch below is taken only where the scan
/// offers nothing, so a fixture with a file in its offer would make every assertion
/// about it over a run that never took it. Read what each test SETS UP rather than what
/// it asserts: they differ in the withheld list
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
            census: ProductNobodyCouldAsk));

        Assert.Equal(CliExitCode.Ok, exit);
        Assert.DoesNotContain(Strings.Cli_FoundNoOrphans, stdout, StringComparison.Ordinal);
        // ONCE, AND NOT AGAIN AS THE LISTED LEAD, which is for a machine offered something
        // beside the withheld half and is gated on the offer having anything in it. The
        // two read alike, so a count is what tells them apart: this machine was offered
        // nothing, and the lead printed here as well would put the sentence on screen
        // twice.
        Assert.Equal(1, Occurrences(stdout, Expected(Strings.Cli_NothingOffered_Plural, 2)));
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
        Assert.DoesNotContain(Strings.Cli_FoundNoOrphans, stdout, StringComparison.Ordinal);
        // Named rather than merely different from the clean line: the wholesale
        // sentence is also different from it, and is false of this machine.
        Assert.DoesNotContain(Expected(Strings.Cli_NothingOffered_Plural, 2), stdout, StringComparison.Ordinal);
        // ONCE, AND NOT AGAIN AS THE LISTED LEAD, on its own branch. The wholesale fixture
        // asserts the same thing about the other one, and the guard has two branches to
        // widen: one that lets the lead onto every machine and one that lets it onto
        // per-file machines alone. Each fixture holds the branch its own machine takes.
        Assert.Equal(1, Occurrences(stdout, Expected(Strings.Cli_NothingOfferedPerFile_Plural, 2)));
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
            census: ProductNobodyCouldAsk));

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
            census: ProductNobodyCouldAsk));

        Assert.Contains(Strings.Cli_WithheldReasons_Header, stdout, StringComparison.Ordinal);
        Assert.Contains(Program.LineFor(WithholdingLeg.RegistryProductUnaskable), stdout, StringComparison.Ordinal);
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
    public async Task A_run_meeting_the_unaskable_product_leg_and_the_second_copy_arm_prints_their_one_line_once()
    {
        // The second-copy arm prints the RegistryProductUnaskable leg's line. A run holding
        // files both ways prints it once, under one heading, beside the other lines it met.
        var (_, stdout) = await Run(Scan(
            withheld: 2,
            split: new WithholdingSplit(WholesaleCount: 1, SecondCopyUnestablishedCount: 1),
            census: ProductNobodyCouldAsk));

        var line = Program.LineFor(WithholdingSplitArm.SecondCopyUnestablished);

        Assert.Equal(Program.LineFor(WithholdingLeg.RegistryProductUnaskable), line);
        Assert.Equal(1, Occurrences(stdout, Strings.Cli_WithheldReasons_Header));
        Assert.Equal(1, Occurrences(stdout, line));
    }

    [Fact]
    public async Task The_second_copy_arm_on_its_own_gets_the_per_file_line_and_its_reason()
    {
        // Every held file counted by the second-copy arm: the per-file sentence, since the
        // wholesale arm accounts for none of them, and the arm's line under it.
        var (_, stdout) = await Run(Scan(
            withheld: 2,
            split: new WithholdingSplit(SecondCopyUnestablishedCount: 2)));

        Assert.Contains(Expected(Strings.Cli_NothingOfferedPerFile_Plural, 2), stdout, StringComparison.Ordinal);
        Assert.Equal(1, Occurrences(stdout, Strings.Cli_WithheldReasons_Header));
        Assert.Equal(1, Occurrences(stdout, Strings.Cli_WithheldReasons_SecondInstance));
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

    [Theory]
    [InlineData(2, 0)]
    [InlineData(1, 1)]
    public async Task A_run_holding_files_for_their_times_says_when_to_scan_again_in_place_of_the_clean_line(
        int forTheirTimes, int forAnInstalledProgram)
    {
        // Kept for their times with the signals clear, alone or beside a file kept for a
        // program Windows still has installed: no held-back sentence and no reasons, and
        // the line about recently changed files where the clean line would be.
        var result = Scan(
            withheld: 2,
            split: new WithholdingSplit(
                NotBeforeTheClockCount: forTheirTimes, DeclaredProductInstalledCount: forAnInstalledProgram),
            positiveBytes: 1024 * forAnInstalledProgram,
            notBeforeTheClockBytes: 1024 * forTheirTimes,
            atScan: PendingRebootResult.Clean);

        var (exit, stdout) = await Run(result);

        Assert.Equal(CliExitCode.Ok, exit);
        Assert.DoesNotContain(Strings.Cli_FoundNoOrphans, stdout, StringComparison.Ordinal);
        Assert.DoesNotContain(Opening(Strings.Cli_NothingOfferedPerFile_Plural), stdout, StringComparison.Ordinal);
        Assert.DoesNotContain(Strings.Cli_WithheldReasons_Header, stdout, StringComparison.Ordinal);

        var line = RecentChangeReport.Line(result);
        Assert.NotEqual(string.Empty, line);
        Assert.Equal(1, Occurrences(stdout, line));
    }

    [Theory]
    [InlineData(PendingRebootReason.MsiExecuteMutexHeld)]
    [InlineData(PendingRebootReason.InstallerInProgress)]
    [InlineData(PendingRebootReason.PendingRenameInCache)]
    [InlineData(PendingRebootReason.RegistryCheckUnreadable)]
    [InlineData(PendingRebootReason.MsiExecuteMutexUnreadable)]
    public async Task A_run_whose_walked_files_were_held_for_a_signal_gets_the_per_file_line_and_the_signal_s_line(
        PendingRebootReason reason)
    {
        // Every walked file held because a signal was set just after the scan's clock: the
        // per-file sentence counts them, the signal's own line says why, and neither the
        // clean line nor the line about recently changed files is printed.
        var result = Scan(
            withheld: 2,
            split: new WithholdingSplit(ScanSignalCount: 2),
            atScan: PendingRebootResult.Block(reason));

        var (exit, stdout) = await Run(result);

        Assert.Equal(CliExitCode.Ok, exit);
        Assert.Contains(Expected(Strings.Cli_NothingOfferedPerFile_Plural, 2), stdout, StringComparison.Ordinal);
        Assert.Equal(1, Occurrences(stdout, ScanSignalReport.CommandLine(reason)));
        Assert.DoesNotContain(Strings.Cli_FoundNoOrphans, stdout, StringComparison.Ordinal);
        Assert.DoesNotContain(Strings.Cli_WithheldReasons_Header, stdout, StringComparison.Ordinal);
        Assert.Equal(string.Empty, RecentChangeReport.Line(result));
    }

    [Fact]
    public async Task A_run_whose_walked_files_were_held_for_the_clock_gets_the_per_file_line_and_the_clock_s_line()
    {
        // The system clock went back while the scan read the files' times, so every walked
        // file its times passed was held for that. The per-file sentence counts them and the
        // clock's own line says why, with no signal's line beside it.
        var result = Scan(
            withheld: 2,
            split: new WithholdingSplit(ClockSetBackCount: 2),
            atScan: PendingRebootResult.Clean);

        var (exit, stdout) = await Run(result);

        Assert.Equal(CliExitCode.Ok, exit);
        Assert.Contains(Expected(Strings.Cli_NothingOfferedPerFile_Plural, 2), stdout, StringComparison.Ordinal);
        Assert.Equal(1, Occurrences(stdout, Strings.Cli_ScanSignal_ClockSetBack));
        Assert.Equal(Strings.Cli_ScanSignal_ClockSetBack, ScanSignalReport.CommandLine(result));
        Assert.DoesNotContain(Strings.Cli_ScanSignal_Other, stdout, StringComparison.Ordinal);
        Assert.DoesNotContain(Strings.Cli_WithheldReasons_Header, stdout, StringComparison.Ordinal);
    }

    [Fact]
    public async Task A_run_holding_no_file_for_its_times_says_nothing_about_times()
    {
        // The same reader of the split, on a run whose held files were kept for another
        // reason, so the absence is of the line and not of a run.
        var (_, stdout) = await Run(Scan(
            withheld: 2,
            split: new WithholdingSplit(DeclaredProductUnestablishedCount: 2)));

        Assert.Contains(Opening(Strings.Cli_NothingOfferedPerFile_Plural), stdout, StringComparison.Ordinal);
        // The words after the size, which no count or size changes.
        var line = Strings.Completion_RecentChange_Plural;
        var words = line[(line.IndexOf("({2})", StringComparison.Ordinal) + 5)..];
        Assert.NotEqual(string.Empty, words.Trim());
        Assert.DoesNotContain(words, stdout, StringComparison.Ordinal);
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
    public async Task Files_the_containment_check_kept_back_get_the_per_file_line_with_no_reasons_under_it()
    {
        // One file the check refused and one it could not answer for. Both are counted
        // by the per-file line, so the run is not given the clean one, and neither has a
        // reason line, so the heading is not printed over nothing.
        var (exit, stdout) = await Run(Scan(
            withheld: 2,
            split: new WithholdingSplit(ContainmentRefusedCount: 1, ContainmentUnestablishedCount: 1)));

        Assert.Equal(CliExitCode.Ok, exit);
        Assert.Contains(Expected(Strings.Cli_NothingOfferedPerFile_Plural, 2), stdout, StringComparison.Ordinal);
        Assert.DoesNotContain(Strings.Cli_FoundNoOrphans, stdout, StringComparison.Ordinal);
        Assert.DoesNotContain(Strings.Cli_WithheldReasons_Header, stdout, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Superseded_patches_the_containment_check_kept_back_are_counted_in_the_superseded_line()
    {
        // Distinct figures, so a member left out of the line's count comes out at a
        // number that cannot be mistaken for the right one: one superseded patch held
        // back for its products, two the check refused and four it could not answer
        // for. The line is the one a superseded patch held back for its products has
        // always had, printed after the clean line, which is about the folder walk.
        var result = Scan(withheld: 0, split: default) with
        {
            WithheldCount = 1,
            SupersededContainmentRefusedCount = 2,
            SupersededContainmentUnestablishedCount = 4,
        };

        var (exit, stdout) = await Run(result);

        Assert.Equal(CliExitCode.Ok, exit);
        Assert.Contains(
            string.Format(Strings.Cli_SupersededHeldBack_Plural, DisplayHelpers.FormatCount(7)),
            stdout, StringComparison.Ordinal);
        Assert.Contains(Strings.Cli_FoundNoOrphans, stdout, StringComparison.Ordinal);
    }

    [Fact]
    public async Task A_single_superseded_patch_the_containment_check_refused_gets_the_one_form()
    {
        // Nothing else held back: without it the run would print the clean line alone.
        var result = Scan(withheld: 0, split: default) with { SupersededContainmentRefusedCount = 1 };

        var (_, stdout) = await Run(result);

        Assert.Contains(Strings.Cli_SupersededHeldBack_Singular, stdout, StringComparison.Ordinal);
    }

    [Fact]
    public async Task The_one_form_names_the_size_and_never_the_numeral()
    {
        // A count of one is reachable, being a folder holding a single unclaimed file,
        // and the plural form renders "1 file are unneeded" for it.
        var (_, stdout) = await Run(Scan(
            withheld: 1,
            split: new WithholdingSplit(DeclaredProductUnestablishedCount: 1)));

        Assert.Contains(Expected(Strings.Cli_NothingOfferedPerFile_Singular, 1), stdout, StringComparison.Ordinal);
        Assert.DoesNotContain(Expected(Strings.Cli_NothingOfferedPerFile_Plural, 1), stdout, StringComparison.Ordinal);
    }

    // ---- fixtures ----

    /// <summary>
    /// A census with the RegistryProductUnaskable leg set, a product the registry names that
    /// the scan could not ask about, so a fixture that wants the wholesale branch's own
    /// reason line has one to print. The legs are read off the census rather than off the split, so a
    /// wholesale count on its own would leave the breakdown empty and a test about it
    /// passing over nothing.
    /// </summary>
    private static EnumerationCensus ProductNobodyCouldAsk =>
        new(UnansweredProductCount: 1);

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
        long notBeforeTheClockBytes = 0, long patchBytes = 0, PendingRebootResult? atScan = null) =>
        new(Array.Empty<OrphanedFile>(), Array.Empty<RegisteredPackage>(), 0,
            Census: census,
            WithheldFiles: Held(withheld),
            WithheldBy: split,
            WithheldDeclaredProductInstalledBytes: positiveBytes,
            WithheldNotBeforeTheClockBytes: notBeforeTheClockBytes,
            WithheldDeclaredPatchRegisteredBytes: patchBytes,
            InstallerActivityAtScan: atScan);

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
