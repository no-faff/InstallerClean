using InstallerClean.Models;
using InstallerClean.Resources;
using InstallerClean.Services;
using Xunit;

namespace InstallerClean.Tests.Models;

/// <summary>
/// The derived figures on <see cref="ScanResult"/>, which the hosts read instead of
/// summing the lists themselves.
///
/// WHY A SUM GETS A TEST AT ALL. The withheld total is where the size on the
/// completion screen starts, and the one thing that figure must never be is the
/// folder's total: printing that would tell somebody that much was going spare
/// when nothing established it. A sum over the wrong list is not a compile error and
/// reads as a plausible number on screen.
/// </summary>
public class ScanResultTests
{
    private static OrphanedFile File(string name, long bytes) =>
        new(@"C:\Windows\Installer\" + name, bytes, false, false, false, Strings.Reason_Orphaned);

    [Fact]
    public void The_withheld_total_sums_the_withheld_list_and_not_the_offer()
    {
        // THE FIXTURE IS THE TEST. The two lists carry deliberately different totals,
        // so a sum taken over the wrong one comes out at the other's figure rather
        // than at something that merely looks wrong.
        var result = new ScanResult(
            RemovableFiles: [File("offered.msi", 9_000_000)],
            RegisteredPackages: [],
            RegisteredTotalBytes: 5_000_000,
            WithheldFiles: [File("a.msi", 1024), File("b.msp", 2048)]);

        Assert.Equal(3072, result.WithheldTotalBytes);
        Assert.Equal(9_000_000, result.RemovableTotalBytes);
    }

    [Fact]
    public void A_host_speaks_of_the_drives_and_shares_given_up_with_a_file_kept_there_in_the_order_given_up()
    {
        var result = new ScanResult(
            RemovableFiles: [],
            RegisteredPackages: [],
            RegisteredTotalBytes: 0,
            SourceRootsGivenUp:
            [
                new(@"\\nas\apps", SourceRootGiveUpRoute.NoAnswer, 2),
                new("D:", SourceRootGiveUpRoute.ReadsAddUp, 0),
                new("E:", SourceRootGiveUpRoute.SlowFailure, 1),
            ]);

        Assert.Equal(
            [new(@"\\nas\apps", SourceRootGiveUpRoute.NoAnswer, 2), new("E:", SourceRootGiveUpRoute.SlowFailure, 1)],
            result.SourceRootsGivenUpKeepingFiles);
    }

    [Fact]
    public void A_result_built_without_the_drives_and_shares_given_up_reads_as_one_that_gave_none_up()
    {
        var result = new ScanResult([], [], 0);

        Assert.Empty(result.SourceRootsGivenUp);
        Assert.Empty(result.SourceRootsGivenUpKeepingFiles);
        Assert.False(result.HasSourceRootsGivenUpKeepingFiles);
        Assert.True(result.IsAllClear);
    }

    [Fact]
    public void A_drive_given_up_keeping_a_file_takes_the_all_clear_away_where_the_held_back_count_does_not()
    {
        // The file is kept for a program Windows still has installed, an arm the count
        // leaves out, so the count says nothing was held back and the drive is what says
        // the scan did not finish checking.
        var result = new ScanResult(
            RemovableFiles: [],
            RegisteredPackages: [],
            RegisteredTotalBytes: 0,
            WithheldFiles: [File("a.msi", 1024)],
            WithheldBy: new WithholdingSplit(DeclaredProductInstalledCount: 1),
            SourceRootsGivenUp: [new("D:", SourceRootGiveUpRoute.StoppedWaiting, 1)]);

        Assert.False(result.HasUnsettledHeldBack);
        Assert.True(result.HasSourceRootsGivenUpKeepingFiles);
        Assert.False(result.IsAllClear);
    }

    [Fact]
    public void A_drive_given_up_with_nothing_kept_there_leaves_the_all_clear()
    {
        var result = new ScanResult(
            RemovableFiles: [],
            RegisteredPackages: [],
            RegisteredTotalBytes: 0,
            SourceRootsGivenUp:
            [
                new("D:", SourceRootGiveUpRoute.ReadsAddUp, 0),
                new(@"\\nas\apps", SourceRootGiveUpRoute.StoppedWaiting, 0),
            ]);

        Assert.False(result.HasSourceRootsGivenUpKeepingFiles);
        Assert.True(result.IsAllClear);
    }

    [Fact]
    public void A_file_held_back_takes_the_all_clear_away_with_no_drive_given_up()
    {
        var result = new ScanResult(
            RemovableFiles: [],
            RegisteredPackages: [],
            RegisteredTotalBytes: 0,
            WithheldFiles: [File("a.msi", 1024)],
            WithheldBy: new WithholdingSplit(DeclaredProductUnestablishedCount: 1));

        Assert.True(result.HasUnsettledHeldBack);
        Assert.False(result.HasSourceRootsGivenUpKeepingFiles);
        Assert.False(result.IsAllClear);
    }

    [Fact]
    public void A_scan_that_withheld_nothing_totals_zero_rather_than_throwing()
    {
        // The list is optional on the record and half the suite's fixtures leave it
        // null, so the null case is the ordinary one rather than an edge.
        var noList = new ScanResult([], [], 0);
        var emptyList = new ScanResult([], [], 0, WithheldFiles: []);

        Assert.Equal(0, noList.WithheldTotalBytes);
        Assert.Equal(0, emptyList.WithheldTotalBytes);
    }

    // ---- Which account the withholding earns ----
    //
    // THE ASYMMETRY IS WHAT THESE ARE ABOUT AND IT IS NOT SYMMETRICAL BY ACCIDENT. The
    // per-file reading says the scan could not establish these files were unneeded,
    // which is true of every file on the list whatever put it there. The wholesale
    // reading names what the scan could not establish about the machine's records, and
    // that is false of a file kept back because Windows still holds a record of the
    // product it declares, which no finding about the machine's records kept. So the
    // wholesale reading is the one that has to be earned, and the fixtures below differ
    // in what they give it to earn it with.

    [Fact]
    public void A_scan_that_kept_nothing_back_has_no_withholding_to_account_for()
    {
        // Both spellings of nothing, because half the suite's fixtures leave the list
        // null and the other half pass an empty one.
        Assert.Equal(WithholdingAccount.Nothing, new ScanResult([], [], 0).Withholding);
        Assert.Equal(WithholdingAccount.Nothing,
            new ScanResult([], [], 0, WithheldFiles: []).Withholding);
    }

    [Fact]
    public void A_withholding_the_wholesale_arm_accounts_for_whole_reads_as_wholesale()
    {
        // The one machine the wholesale sentence is true of: every file on the list was
        // put there by the branch that sentence describes.
        var result = new ScanResult([], [], 0,
            WithheldFiles: [File("a.msi", 1024), File("b.msi", 2048)],
            WithheldBy: new WithholdingSplit(WholesaleCount: 2));

        Assert.Equal(WithholdingAccount.WholeWalkOffer, result.Withholding);
    }

    [Theory]
    [InlineData(false, WithholdingAccount.PerFile)]
    [InlineData(true, WithholdingAccount.WholeWalkOffer)]
    public void A_wholesale_arm_beside_a_walked_file_on_the_offer_reads_as_per_file(
        bool offeredIsSuperseded, WithholdingAccount expected)
    {
        // The wholesale arm took the installation packages and a patch file went on to
        // the checks on the file and was offered. The wholesale sentences say nothing
        // the folder walk found was offered, which is false of that run. A superseded
        // patch on the offer comes from its own registration, and leaves them true.
        var offered = new OrphanedFile(@"C:\Windows\Installer\c.msp", 4096, IsPatch: true,
            IsRemovablePatch: offeredIsSuperseded, IsObsoleted: false, Strings.Reason_Orphaned);
        var result = new ScanResult([offered], [], 0,
            WithheldFiles: [File("a.msi", 1024), File("b.msi", 2048)],
            WithheldBy: new WithholdingSplit(WholesaleCount: 2));

        Assert.Equal(expected, result.Withholding);
    }

    [Fact]
    public void A_withholding_with_no_wholesale_share_reads_as_per_file()
    {
        // The declared-product screen failing to settle two files, which is the machine
        // the per-file reading exists for: the wholesale arm took nothing and the folder
        // is not clean.
        var result = new ScanResult([], [], 0,
            WithheldFiles: [File("a.msi", 1024), File("b.msi", 2048)],
            WithheldBy: new WithholdingSplit(DeclaredProductUnestablishedCount: 2));

        Assert.Equal(WithholdingAccount.PerFile, result.Withholding);
        Assert.True(result.HasWithholdingToReport);
        Assert.Equal(2, result.UnestablishedWithheldCount);
    }

    [Fact]
    public void Files_all_kept_for_an_installed_program_read_as_that_and_have_nothing_to_report()
    {
        // Every file on the list was counted by the declared-product-installed arm, so
        // the reading is its own and no surface says anything about it.
        var result = new ScanResult([], [], 0,
            WithheldFiles: [File("a.msi", 1024), File("b.msi", 2048)],
            WithheldBy: new WithholdingSplit(DeclaredProductInstalledCount: 2));

        Assert.Equal(WithholdingAccount.KeptWithoutNotice, result.Withholding);
        Assert.False(result.HasWithholdingToReport);
        Assert.Equal(0, result.UnestablishedWithheldCount);
    }

    [Fact]
    public void Files_all_kept_for_their_times_have_nothing_to_report()
    {
        // Files whose times are not before the scan's clock take the same reading as those
        // kept for an installed program, and leave the command line's held-back Application
        // log entries nothing to count: the line about recently changed files speaks for them.
        var result = new ScanResult([], [], 0,
            WithheldFiles: [File("a.msi", 1024), File("b.msp", 2048)],
            WithheldBy: new WithholdingSplit(NotBeforeTheClockCount: 2),
            WithheldNotBeforeTheClockBytes: 3072);

        Assert.Equal(WithholdingAccount.KeptWithoutNotice, result.Withholding);
        Assert.False(result.HasWithholdingToReport);
        Assert.Equal(0, result.UnestablishedWithheldCount);
    }

    [Fact]
    public void Files_kept_for_an_installed_program_and_for_their_age_together_have_nothing_to_report()
    {
        // Two of the silent arms between them account for the list.
        var result = new ScanResult([], [], 0,
            WithheldFiles: [File("a.msi", 1024), File("b.msi", 2048)],
            WithheldBy: new WithholdingSplit(DeclaredProductInstalledCount: 1, NotBeforeTheClockCount: 1),
            WithheldNotBeforeTheClockBytes: 2048);

        Assert.Equal(WithholdingAccount.KeptWithoutNotice, result.Withholding);
        Assert.False(result.HasWithholdingToReport);
        Assert.Equal(0, result.UnestablishedWithheldCount);
    }

    [Fact]
    public void Files_all_kept_for_their_patch_s_registrations_have_nothing_to_report()
    {
        // A patch copy kept because Windows holds a registration of the patch it declares
        // takes the reading a file kept for an installed program takes, and leaves the
        // command line's held-back Application log entries nothing to count.
        var result = new ScanResult([], [], 0,
            WithheldFiles: [File("a.msp", 1024), File("b.msp", 2048)],
            WithheldBy: new WithholdingSplit(DeclaredPatchRegisteredCount: 2));

        Assert.Equal(WithholdingAccount.KeptWithoutNotice, result.Withholding);
        Assert.False(result.HasWithholdingToReport);
        Assert.Equal(0, result.UnestablishedWithheldCount);
    }

    [Fact]
    public void Files_kept_by_the_three_silent_arms_together_have_nothing_to_report()
    {
        var result = new ScanResult([], [], 0,
            WithheldFiles: [File("a.msi", 1024), File("b.msi", 2048), File("c.msp", 4096)],
            WithheldBy: new WithholdingSplit(
                DeclaredProductInstalledCount: 1, NotBeforeTheClockCount: 1, DeclaredPatchRegisteredCount: 1),
            WithheldNotBeforeTheClockBytes: 2048);

        Assert.Equal(WithholdingAccount.KeptWithoutNotice, result.Withholding);
        Assert.False(result.HasWithholdingToReport);
        Assert.Equal(0, result.UnestablishedWithheldCount);
    }

    [Fact]
    public void A_file_kept_for_its_patch_s_registrations_beside_one_the_scan_could_not_settle_reads_as_per_file()
    {
        // The per-file sentence speaks of the file the scan could not settle, and its
        // count and size leave the patch copy out.
        var result = new ScanResult([], [], 0,
            WithheldFiles: [File("a.msp", 1024), File("b.msi", 2048)],
            WithheldBy: new WithholdingSplit(DeclaredPatchRegisteredCount: 1, IdentityUnestablishedCount: 1));

        Assert.Equal(WithholdingAccount.PerFile, result.Withholding);
        Assert.True(result.HasWithholdingToReport);
        Assert.Equal(1, result.UnestablishedWithheldCount);
    }

    [Fact]
    public void A_withheld_file_the_split_did_not_count_keeps_a_run_off_the_silent_reading_beside_a_patch_hold()
    {
        // THE MUST-MISS FOR THE DECLARED-PATCH-REGISTERED ARM, on the rule the must-miss
        // tests for the other two silent arms pin: the silent arms have to account for
        // the whole list, and a file none of them counted keeps the run per-file.
        var result = new ScanResult([], [], 0,
            WithheldFiles: [File("a.msp", 1024), File("b.msi", 2048)],
            WithheldBy: new WithholdingSplit(DeclaredPatchRegisteredCount: 1));

        Assert.Equal(WithholdingAccount.PerFile, result.Withholding);
        Assert.Equal(1, result.UnestablishedWithheldCount);
    }

    [Fact]
    public void A_patch_copy_the_scan_could_not_settle_is_spoken_of_per_file()
    {
        // The patch half's unsettled arm is not one of the silent arms. The Application
        // log's per-file entry counts its file.
        var result = new ScanResult([], [], 0,
            WithheldFiles: [File("a.msp", 1024)],
            WithheldBy: new WithholdingSplit(DeclaredPatchUnestablishedCount: 1));

        Assert.Equal(WithholdingAccount.PerFile, result.Withholding);
        Assert.True(result.HasWithholdingToReport);
        Assert.Equal(1, result.UnestablishedWithheldCount);
    }

    [Fact]
    public void A_patch_copy_and_a_package_the_scan_could_not_settle_are_counted_together()
    {
        // Two members, not one shared: each half's unsettled arm is a different set of
        // inabilities, and both are among the files the Application log's per-file entry counts.
        var result = new ScanResult([], [], 0,
            WithheldFiles: [File("a.msi", 1024), File("b.msp", 2048)],
            WithheldBy: new WithholdingSplit(
                DeclaredProductUnestablishedCount: 1, DeclaredPatchUnestablishedCount: 1));

        Assert.Equal(2, result.UnestablishedWithheldCount);
    }

    [Fact]
    public void Packages_kept_beside_a_second_copy_whose_packages_could_not_all_be_seen_are_held_back_per_file()
    {
        // The arm is not one of the silent arms, and its files are counted as unsettled.
        var result = new ScanResult([], [], 0,
            WithheldFiles: [File("a.msi", 1024), File("b.msi", 2048)],
            WithheldBy: new WithholdingSplit(SecondCopyUnestablishedCount: 2));

        Assert.Equal(WithholdingAccount.PerFile, result.Withholding);
        Assert.True(result.HasWithholdingToReport);
        Assert.Equal(2, result.UnestablishedWithheldCount);
        Assert.Equal(2, result.UnsettledHeldBackCount);
    }

    [Fact]
    public void A_patch_copy_the_scan_could_not_settle_is_counted_apart_from_one_kept_for_its_patch_s_registrations()
    {
        // One of each. The sentence counts and sizes the first alone, and only the
        // first has a line.
        var result = new ScanResult([], [], 0,
            WithheldFiles: [File("a.msp", 1024), File("b.msp", 2048)],
            WithheldBy: new WithholdingSplit(DeclaredPatchRegisteredCount: 1, DeclaredPatchUnestablishedCount: 1));

        Assert.Equal(WithholdingAccount.PerFile, result.Withholding);
        Assert.Equal(1, result.UnestablishedWithheldCount);
    }

    [Fact]
    public void A_file_kept_for_its_age_beside_one_the_scan_could_not_settle_reads_as_per_file()
    {
        // The per-file sentence speaks of the file the scan could not settle, and its
        // count and size leave the other out.
        var result = new ScanResult([], [], 0,
            WithheldFiles: [File("a.msi", 1024), File("b.msi", 2048)],
            WithheldBy: new WithholdingSplit(NotBeforeTheClockCount: 1, DeclaredProductUnestablishedCount: 1),
            WithheldNotBeforeTheClockBytes: 1024);

        Assert.Equal(WithholdingAccount.PerFile, result.Withholding);
        Assert.True(result.HasWithholdingToReport);
        Assert.Equal(1, result.UnestablishedWithheldCount);
    }

    [Fact]
    public void A_withheld_file_the_split_did_not_count_keeps_a_run_off_the_silent_reading_beside_an_age_hold()
    {
        // THE MUST-MISS FOR THE AGE ARM, on the same rule as the one below for the
        // installed-program arm: the silent arms have to account for the whole list,
        // and a file none of them counted keeps the run per-file.
        var result = new ScanResult([], [], 0,
            WithheldFiles: [File("a.msi", 1024), File("b.msi", 2048)],
            WithheldBy: new WithholdingSplit(NotBeforeTheClockCount: 1),
            WithheldNotBeforeTheClockBytes: 1024);

        Assert.Equal(WithholdingAccount.PerFile, result.Withholding);
        Assert.Equal(1, result.UnestablishedWithheldCount);
    }

    [Fact]
    public void Both_age_arms_count_towards_the_split_total()
    {
        Assert.Equal(28, new WithholdingSplit(1, 2, 3, 4, 5, 6, 7).Total);
    }

    [Fact]
    public void A_file_whose_age_was_not_established_is_spoken_of_per_file()
    {
        // Kept by the age check with no age read. Not one of the silent arms, so it
        // is among the files the command line's held-back Application log entry counts.
        var result = new ScanResult([], [], 0,
            WithheldFiles: [File("a.msi", 1024)],
            WithheldBy: new WithholdingSplit(AgeUnestablishedCount: 1));

        Assert.Equal(WithholdingAccount.PerFile, result.Withholding);
        Assert.True(result.HasWithholdingToReport);
        Assert.Equal(1, result.UnestablishedWithheldCount);
    }

    [Fact]
    public void A_file_whose_age_was_not_established_is_counted_apart_from_one_under_a_day_old()
    {
        // One of each. The sentence counts and sizes the second alone.
        var result = new ScanResult([], [], 0,
            WithheldFiles: [File("a.msi", 1024), File("b.msi", 2048)],
            WithheldBy: new WithholdingSplit(NotBeforeTheClockCount: 1, AgeUnestablishedCount: 1),
            WithheldNotBeforeTheClockBytes: 1024);

        Assert.Equal(WithholdingAccount.PerFile, result.Withholding);
        Assert.Equal(1, result.UnestablishedWithheldCount);
    }

    [Fact]
    public void A_file_kept_for_an_installed_program_beside_one_the_scan_could_not_settle_reads_as_per_file()
    {
        // The per-file sentence speaks of the file the scan could not settle, and its
        // count and size leave the other out.
        var result = new ScanResult([], [], 0,
            WithheldFiles: [File("a.msi", 1024), File("b.msi", 2048)],
            WithheldBy: new WithholdingSplit(DeclaredProductInstalledCount: 1, IdentityUnestablishedCount: 1));

        Assert.Equal(WithholdingAccount.PerFile, result.Withholding);
        Assert.True(result.HasWithholdingToReport);
        Assert.Equal(1, result.UnestablishedWithheldCount);
    }

    [Fact]
    public void A_withheld_file_the_split_did_not_count_keeps_a_run_off_the_installed_program_reading()
    {
        // THE MUST-MISS FOR THE DECLARED-PRODUCT-INSTALLED ARM. One file counted under
        // it and one counted under nothing: no other arm fired, and the run still reads
        // per-file, because the rule is that this arm accounts for the whole list. The
        // uncounted file is among those the Application log's per-file entry counts.
        var result = new ScanResult([], [], 0,
            WithheldFiles: [File("a.msi", 1024), File("b.msi", 2048)],
            WithheldBy: new WithholdingSplit(DeclaredProductInstalledCount: 1));

        Assert.Equal(WithholdingAccount.PerFile, result.Withholding);
        Assert.True(result.HasWithholdingToReport);
        Assert.Equal(1, result.UnestablishedWithheldCount);
    }

    [Fact]
    public void Only_the_two_silent_readings_have_nothing_to_report()
    {
        // Held against the enum's length, so a reading added later fails here until a
        // result that derives it is added. Each reading is reached through a result
        // that derives it, not set.
        var nothing = new ScanResult([], [], 0);
        var wholesale = new ScanResult([], [], 0,
            WithheldFiles: [File("a.msi", 1024)],
            WithheldBy: new WithholdingSplit(WholesaleCount: 1));
        var perFile = new ScanResult([], [], 0,
            WithheldFiles: [File("a.msi", 1024)],
            WithheldBy: new WithholdingSplit(ScreenUnansweredCount: 1));
        var keptWithoutNotice = new ScanResult([], [], 0,
            WithheldFiles: [File("a.msi", 1024)],
            WithheldBy: new WithholdingSplit(DeclaredProductInstalledCount: 1));

        var byReading = new Dictionary<WithholdingAccount, ScanResult>
        {
            [nothing.Withholding] = nothing,
            [wholesale.Withholding] = wholesale,
            [perFile.Withholding] = perFile,
            [keptWithoutNotice.Withholding] = keptWithoutNotice,
        };

        Assert.Equal(Enum.GetValues<WithholdingAccount>().Length, byReading.Count);
        foreach (var (reading, result) in byReading)
        {
            var silent = reading is WithholdingAccount.Nothing or WithholdingAccount.KeptWithoutNotice;
            Assert.True(silent != result.HasWithholdingToReport, $"{reading} reported {result.HasWithholdingToReport}");
        }
    }

    [Fact]
    public void A_run_that_withheld_both_ways_reads_as_per_file()
    {
        // THE MIXED RUN, AND IT IS REACHABLE RATHER THAN HYPOTHETICAL: the identity
        // pass keeps files one at a time before the wholesale branch takes the rest, so
        // the list holds files from both. The wholesale sentence is false of the half
        // the identity pass took, and the per-file one is true of every file here, so
        // the superordinate is what this machine gets.
        var result = new ScanResult([], [], 0,
            WithheldFiles: [File("a.msi", 1024), File("b.msi", 2048), File("c.msi", 512)],
            WithheldBy: new WithholdingSplit(IdentityUnestablishedCount: 1, WholesaleCount: 2));

        Assert.Equal(WithholdingAccount.PerFile, result.Withholding);
    }

    [Fact]
    public void A_withheld_file_the_split_did_not_count_reads_as_per_file()
    {
        // A file on the list that no arm counted leaves the wholesale arm short of the
        // list's own length, so the reading falls to the sentence that is true of every
        // file rather than sweeping the uncounted one under a cause nobody established.
        // No per-file arm fired either, so this fixture is where "the wholesale arm
        // accounts for all of them" and "no per-file arm fired" give different readings.
        var result = new ScanResult([], [], 0,
            WithheldFiles: [File("a.msi", 1024), File("b.msi", 2048)],
            WithheldBy: new WithholdingSplit(WholesaleCount: 1));

        Assert.Equal(WithholdingAccount.PerFile, result.Withholding);
    }

    // ---- What a scan counts as held back unsettled ----
    //
    // EVERY FILE HELD BACK EXCEPT THOSE THE DECLARED-PRODUCT-INSTALLED AND
    // DECLARED-PATCH-REGISTERED ARMS KEPT, TOGETHER WITH THE SUPERSEDED FILES. Above zero,
    // the count keeps the all-clear off the window's finished screen.
    //
    // THE COMMAND LINE'S MEMBERS ARE ASSERTED BESIDE IT where the two readings part, so
    // a change reaching them from here fails.

    [Fact]
    public void A_scan_that_held_nothing_back_counts_nothing_unsettled()
    {
        var noList = new ScanResult([], [], 0);
        var emptyList = new ScanResult([], [], 0, WithheldFiles: []);

        foreach (var result in new[] { noList, emptyList })
        {
            Assert.Equal(0, result.UnsettledHeldBackCount);
            Assert.False(result.HasUnsettledHeldBack);
        }
    }

    [Fact]
    public void Files_kept_for_an_installed_program_are_not_counted_as_unsettled()
    {
        var result = new ScanResult([], [], 0,
            WithheldFiles: [File("a.msi", 1024), File("b.msi", 2048)],
            WithheldBy: new WithholdingSplit(DeclaredProductInstalledCount: 2));

        Assert.Equal(0, result.UnsettledHeldBackCount);
        Assert.False(result.HasUnsettledHeldBack);
    }

    [Fact]
    public void Patch_copies_kept_for_their_patch_s_registrations_are_not_counted_as_unsettled()
    {
        var result = new ScanResult([], [], 0,
            WithheldFiles: [File("a.msp", 1024), File("b.msp", 2048)],
            WithheldBy: new WithholdingSplit(DeclaredPatchRegisteredCount: 2));

        Assert.Equal(0, result.UnsettledHeldBackCount);
        Assert.False(result.HasUnsettledHeldBack);
    }

    [Fact]
    public void The_two_arms_together_are_not_counted_as_unsettled()
    {
        var result = new ScanResult([], [], 0,
            WithheldFiles: [File("a.msi", 1024), File("b.msp", 2048)],
            WithheldBy: new WithholdingSplit(DeclaredProductInstalledCount: 1, DeclaredPatchRegisteredCount: 1));

        Assert.Equal(0, result.UnsettledHeldBackCount);
        Assert.False(result.HasUnsettledHeldBack);
    }

    [Fact]
    public void Files_kept_for_their_times_are_counted_as_unsettled_and_not_by_the_command_line()
    {
        var result = new ScanResult([], [], 0,
            WithheldFiles: [File("a.msi", 1024), File("b.msi", 2048)],
            WithheldBy: new WithholdingSplit(NotBeforeTheClockCount: 2),
            WithheldNotBeforeTheClockBytes: 3072);

        Assert.Equal(2, result.UnsettledHeldBackCount);
        Assert.True(result.HasUnsettledHeldBack);

        Assert.Equal(WithholdingAccount.KeptWithoutNotice, result.Withholding);
        Assert.False(result.HasWithholdingToReport);
        Assert.Equal(0, result.UnestablishedWithheldCount);
    }

    private static readonly PendingRebootResult SignalSet =
        PendingRebootResult.Block(PendingRebootReason.MsiExecuteMutexHeld);

    [Theory]
    [InlineData("nothing held back", false)]
    [InlineData("kept for their times", true)]
    [InlineData("kept for their times, the signals read clear", true)]
    [InlineData("kept because a signal was set", false)]
    public void A_scan_has_a_line_about_recently_changed_files_where_it_held_one_for_its_times(
        string run, bool hasLine)
    {
        var result = run switch
        {
            "nothing held back" => new ScanResult([], [], 0),
            "kept for their times" => new ScanResult([], [], 0,
                WithheldFiles: [File("a.msi", 1024), File("b.msi", 2048)],
                WithheldBy: new WithholdingSplit(NotBeforeTheClockCount: 2),
                WithheldNotBeforeTheClockBytes: 3072),
            "kept for their times, the signals read clear" => new ScanResult([], [], 0,
                WithheldFiles: [File("a.msi", 1024), File("b.msi", 2048)],
                WithheldBy: new WithholdingSplit(NotBeforeTheClockCount: 2),
                WithheldNotBeforeTheClockBytes: 3072,
                InstallerActivityAtScan: PendingRebootResult.Clean),
            // The signal's reason speaks for these files, so there is no line, and the
            // command line's clean line reads the same answer.
            "kept because a signal was set" => new ScanResult([], [], 0,
                WithheldFiles: [File("a.msi", 1024), File("b.msi", 2048)],
                WithheldBy: new WithholdingSplit(ScanSignalCount: 2),
                InstallerActivityAtScan: SignalSet),
            _ => throw new ArgumentOutOfRangeException(nameof(run), run, null),
        };

        Assert.Equal(hasLine, result.HasRecentChangeLine);
    }

    [Fact]
    public void Files_kept_because_a_signal_was_set_are_counted_by_every_held_back_count()
    {
        var result = new ScanResult([], [], 0,
            WithheldFiles: [File("a.msi", 1024), File("b.msp", 2048)],
            WithheldBy: new WithholdingSplit(ScanSignalCount: 2),
            InstallerActivityAtScan: SignalSet);

        Assert.True(result.InstallerActivityAtScanBlocked);
        Assert.Equal(0, result.WithheldBy.NotBeforeTheClockCount);
        Assert.Equal(0, result.WithheldNotBeforeTheClockBytes);
        Assert.Equal(2, result.UnestablishedWithheldCount);
        Assert.Equal(WithholdingAccount.PerFile, result.Withholding);
        Assert.True(result.HasWithholdingToReport);
        Assert.Equal(2, result.UnsettledHeldBackCount);
    }

    [Fact]
    public void A_file_kept_for_its_times_is_counted_beside_one_kept_for_an_installed_program()
    {
        var result = new ScanResult([], [], 0,
            WithheldFiles: [File("a.msi", 1024), File("b.msi", 2048)],
            WithheldBy: new WithholdingSplit(DeclaredProductInstalledCount: 1, NotBeforeTheClockCount: 1),
            WithheldNotBeforeTheClockBytes: 2048);

        Assert.Equal(1, result.UnsettledHeldBackCount);
    }

    [Fact]
    public void A_file_kept_for_its_times_is_counted_beside_a_patch_copy_kept_for_its_patch_s_registrations()
    {
        var result = new ScanResult([], [], 0,
            WithheldFiles: [File("a.msp", 1024), File("b.msi", 4096)],
            WithheldBy: new WithholdingSplit(NotBeforeTheClockCount: 1, DeclaredPatchRegisteredCount: 1),
            WithheldNotBeforeTheClockBytes: 4096);

        Assert.Equal(1, result.UnsettledHeldBackCount);
    }

    [Fact]
    public void A_file_whose_times_were_not_established_is_counted_as_unsettled()
    {
        var result = new ScanResult([], [], 0,
            WithheldFiles: [File("a.msi", 1024)],
            WithheldBy: new WithholdingSplit(AgeUnestablishedCount: 1));

        Assert.Equal(1, result.UnsettledHeldBackCount);
        Assert.True(result.HasUnsettledHeldBack);
    }

    [Fact]
    public void Superseded_files_held_back_are_counted_as_unsettled_and_not_in_the_command_line_s_walk_sentence()
    {
        var result = new ScanResult([], [], 0,
            WithheldCount: 3,
            WithheldFiles: []);

        Assert.Equal(3, result.UnsettledHeldBackCount);
        Assert.True(result.HasUnsettledHeldBack);

        Assert.Equal(WithholdingAccount.Nothing, result.Withholding);
        Assert.False(result.HasWithholdingToReport);
        Assert.Equal(0, result.UnestablishedWithheldCount);
    }

    [Fact]
    public void A_wholesale_withholding_is_counted_as_unsettled()
    {
        var result = new ScanResult([], [], 0,
            WithheldFiles: [File("a.msi", 1024), File("b.msi", 2048)],
            WithheldBy: new WithholdingSplit(WholesaleCount: 2));

        Assert.Equal(2, result.UnsettledHeldBackCount);
    }

    [Fact]
    public void A_superseded_file_beside_a_wholesale_withholding_is_counted_with_it()
    {
        var result = new ScanResult([], [], 0,
            WithheldCount: 1,
            WithheldFiles: [File("a.msi", 1024), File("b.msi", 2048)],
            WithheldBy: new WithholdingSplit(WholesaleCount: 2));

        Assert.Equal(3, result.UnsettledHeldBackCount);

        Assert.Equal(WithholdingAccount.WholeWalkOffer, result.Withholding);
        Assert.Equal(2, result.UnestablishedWithheldCount);
    }

    [Fact]
    public void Superseded_files_are_counted_with_the_walk_s_files_held_one_at_a_time()
    {
        var result = new ScanResult([], [], 0,
            WithheldCount: 2,
            WithheldFiles: [File("a.msi", 1024), File("b.msi", 2048)],
            WithheldBy: new WithholdingSplit(DeclaredProductInstalledCount: 1, ScreenUnansweredCount: 1));

        Assert.Equal(3, result.UnsettledHeldBackCount);
    }

    [Fact]
    public void A_withheld_file_no_arm_counted_is_counted_as_unsettled()
    {
        // THE LIST LESS THE TWO ARMS, NOT A SUM OF THE OTHERS, so a file on the list the
        // split did not count, or one counted by an arm added later, is spoken of.
        var result = new ScanResult([], [], 0,
            WithheldFiles: [File("a.msi", 1024), File("b.msi", 2048)],
            WithheldBy: new WithholdingSplit(DeclaredProductInstalledCount: 1));

        Assert.Equal(1, result.UnsettledHeldBackCount);
        Assert.True(result.HasUnsettledHeldBack);
    }

    [Fact]
    public void Superseded_files_the_containment_check_kept_back_are_counted_as_unsettled()
    {
        // A run whose only kept files are superseded rows the containment check refused
        // or could not answer for. The rows are registered files, so they are on no
        // withheld list; they are counted as unsettled anyway, so a run holding nothing
        // else back is not given the all-clear.
        var result = new ScanResult([], [], 0,
            WithheldFiles: [],
            SupersededContainmentRefusedCount: 2,
            SupersededContainmentUnestablishedCount: 1);

        Assert.Equal(3, result.SupersededHeldBackCount);
        Assert.Equal(3, result.UnsettledHeldBackCount);
        Assert.True(result.HasUnsettledHeldBack);
        Assert.False(result.IsAllClear);

        // The command line's Application log entry for the folder walk is not about them,
        // and its entry for superseded files reads SupersededHeldBackCount.
        Assert.Equal(WithholdingAccount.Nothing, result.Withholding);
        Assert.Equal(0, result.UnestablishedWithheldCount);
    }

    [Fact]
    public void Superseded_files_held_back_either_way_are_one_count()
    {
        // Distinct figures on each side, so a member left out of the sum comes out at a
        // number that cannot be mistaken for the right one.
        var result = new ScanResult([], [], 0,
            WithheldCount: 4,
            WithheldFiles: [],
            SupersededContainmentRefusedCount: 20,
            SupersededContainmentUnestablishedCount: 300);

        Assert.Equal(324, result.SupersededHeldBackCount);
        Assert.Equal(324, result.UnsettledHeldBackCount);
    }

    [Fact]
    public void Superseded_files_withheld_on_an_unsettled_recorded_path_are_counted_where_the_file_is_on_disk()
    {
        // Three withheld superseded rows: one the scan-wide withholding took on an unsettled
        // recorded path with its file on disk, one it took with its file gone, and one an
        // earlier check withheld. Only the first is a file this condition holds back.
        var result = new ScanResult([],
            [
                WithheldSuperseded("present.msp", onUnsettledPath: true, fileExists: true),
                WithheldSuperseded("gone.msp", onUnsettledPath: true, fileExists: false),
                WithheldSuperseded("earlier.msp", onUnsettledPath: false, fileExists: true),
            ], 0);

        Assert.Equal(1, result.SupersededRecordedPathUnestablishedCount);
    }

    private static RegisteredPackage WithheldSuperseded(string name, bool onUnsettledPath, bool fileExists) =>
        new(@"C:\Windows\Installer\" + name, "Product", "{A}",
            PatchState: 2, RemovableWithheld: true, FileExists: fileExists,
            WithheldOnRecordedPathUnestablished: onUnsettledPath);

    [Fact]
    public void Files_from_the_walk_the_containment_check_kept_back_are_held_back_per_file()
    {
        // Both containment arms on the withheld list and nothing else. They are counted
        // as unsettled and by the command line's held-back entry in the Application log.
        var result = new ScanResult([], [], 0,
            WithheldFiles: [File("link.msi", 0), File("unread.msp", 4096)],
            WithheldBy: new WithholdingSplit(ContainmentRefusedCount: 1, ContainmentUnestablishedCount: 1));

        Assert.Equal(2, result.WithheldBy.Total);
        Assert.Equal(2, result.UnsettledHeldBackCount);

        Assert.Equal(WithholdingAccount.PerFile, result.Withholding);
        Assert.True(result.HasWithholdingToReport);
        Assert.Equal(2, result.UnestablishedWithheldCount);
    }

    [Fact]
    public void A_file_the_containment_check_kept_back_beside_a_wholesale_withholding_takes_the_per_file_reading()
    {
        // The wholesale sentence names a cause about the machine's records, which is
        // false of a file the containment check kept back: that check asks where the
        // file is, and nothing about the records.
        var result = new ScanResult([], [], 0,
            WithheldFiles: [File("a.msi", 1024), File("b.msi", 2048), File("link.msi", 0)],
            WithheldBy: new WithholdingSplit(WholesaleCount: 2, ContainmentRefusedCount: 1));

        Assert.Equal(3, result.UnsettledHeldBackCount);
        Assert.Equal(WithholdingAccount.PerFile, result.Withholding);
    }
}
