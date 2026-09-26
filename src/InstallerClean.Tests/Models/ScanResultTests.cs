using InstallerClean.Models;
using InstallerClean.Resources;
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
    public void A_scan_that_withheld_nothing_totals_zero_rather_than_throwing()
    {
        // The list is optional on the record and half the suite's fixtures leave it
        // null, so the null case is the ordinary one rather than an edge.
        var noList = new ScanResult([], [], 0);
        var emptyList = new ScanResult([], [], 0, WithheldFiles: []);

        Assert.Equal(0, noList.WithheldTotalBytes);
        Assert.Equal(0, emptyList.WithheldTotalBytes);
    }

    [Fact]
    public void A_scan_defaults_to_not_having_withheld_its_offer_wholesale()
    {
        // FALSE IS THE HONEST DEFAULT and it is pinned because the fixtures that omit
        // it are asserting things about ordinary machines. A default of true would
        // put every one of them on the wrong completion screen.
        Assert.False(new ScanResult([], [], 0).WalkOfferWithheldWholesale);
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
            WalkOfferWithheldWholesale: true,
            WithheldBy: new WithholdingSplit(WholesaleCount: 2));

        Assert.Equal(WithholdingAccount.WholeWalkOffer, result.Withholding);
    }

    [Fact]
    public void A_withholding_with_no_wholesale_share_reads_as_per_file()
    {
        // The declared-product screen failing to settle two files, which is the machine
        // the per-file reading exists for: the flag is false and the folder is not clean.
        var result = new ScanResult([], [], 0,
            WithheldFiles: [File("a.msi", 1024), File("b.msi", 2048)],
            WithheldBy: new WithholdingSplit(DeclaredProductUnestablishedCount: 2));

        Assert.Equal(WithholdingAccount.PerFile, result.Withholding);
        Assert.True(result.HasWithholdingToReport);
        Assert.Equal(2, result.UnestablishedWithheldCount);
        Assert.Equal(3072, result.UnestablishedWithheldBytes);
    }

    [Fact]
    public void Files_all_kept_for_an_installed_program_read_as_that_and_have_nothing_to_report()
    {
        // Every file on the list was counted by the declared-product-installed arm, so
        // the reading is its own and no surface says anything about it.
        var result = new ScanResult([], [], 0,
            WithheldFiles: [File("a.msi", 1024), File("b.msi", 2048)],
            WithheldBy: new WithholdingSplit(DeclaredProductInstalledCount: 2),
            WithheldDeclaredProductInstalledBytes: 3072);

        Assert.Equal(WithholdingAccount.KeptWithoutNotice, result.Withholding);
        Assert.False(result.HasWithholdingToReport);
        Assert.Equal(0, result.UnestablishedWithheldCount);
        Assert.Equal(0, result.UnestablishedWithheldBytes);
    }

    [Fact]
    public void Files_all_kept_for_their_age_have_nothing_to_report()
    {
        // Files read as under a day old take the same reading as those kept for an
        // installed program, and leave the held-back sentences nothing to count.
        var result = new ScanResult([], [], 0,
            WithheldFiles: [File("a.msi", 1024), File("b.msp", 2048)],
            WithheldBy: new WithholdingSplit(UnderADayOldCount: 2),
            WithheldUnderADayOldBytes: 3072);

        Assert.Equal(WithholdingAccount.KeptWithoutNotice, result.Withholding);
        Assert.False(result.HasWithholdingToReport);
        Assert.Equal(0, result.UnestablishedWithheldCount);
        Assert.Equal(0, result.UnestablishedWithheldBytes);
        Assert.Empty(result.WithheldBy.ArmsFired);
    }

    [Fact]
    public void Files_kept_for_an_installed_program_and_for_their_age_together_have_nothing_to_report()
    {
        // Two of the silent arms between them account for the list.
        var result = new ScanResult([], [], 0,
            WithheldFiles: [File("a.msi", 1024), File("b.msi", 2048)],
            WithheldBy: new WithholdingSplit(DeclaredProductInstalledCount: 1, UnderADayOldCount: 1),
            WithheldDeclaredProductInstalledBytes: 1024,
            WithheldUnderADayOldBytes: 2048);

        Assert.Equal(WithholdingAccount.KeptWithoutNotice, result.Withholding);
        Assert.False(result.HasWithholdingToReport);
        Assert.Equal(0, result.UnestablishedWithheldCount);
        Assert.Equal(0, result.UnestablishedWithheldBytes);
    }

    [Fact]
    public void Files_all_kept_for_their_patch_s_registrations_have_nothing_to_report()
    {
        // A patch copy kept because Windows holds a registration of the patch it declares
        // takes the reading a file kept for an installed program takes, and leaves the
        // held-back sentences nothing to count.
        var result = new ScanResult([], [], 0,
            WithheldFiles: [File("a.msp", 1024), File("b.msp", 2048)],
            WithheldBy: new WithholdingSplit(DeclaredPatchRegisteredCount: 2),
            WithheldDeclaredPatchRegisteredBytes: 3072);

        Assert.Equal(WithholdingAccount.KeptWithoutNotice, result.Withholding);
        Assert.False(result.HasWithholdingToReport);
        Assert.Equal(0, result.UnestablishedWithheldCount);
        Assert.Equal(0, result.UnestablishedWithheldBytes);
        Assert.Empty(result.WithheldBy.ArmsFired);
    }

    [Fact]
    public void Files_kept_by_the_three_silent_arms_together_have_nothing_to_report()
    {
        var result = new ScanResult([], [], 0,
            WithheldFiles: [File("a.msi", 1024), File("b.msi", 2048), File("c.msp", 4096)],
            WithheldBy: new WithholdingSplit(
                DeclaredProductInstalledCount: 1, UnderADayOldCount: 1, DeclaredPatchRegisteredCount: 1),
            WithheldDeclaredProductInstalledBytes: 1024,
            WithheldUnderADayOldBytes: 2048,
            WithheldDeclaredPatchRegisteredBytes: 4096);

        Assert.Equal(WithholdingAccount.KeptWithoutNotice, result.Withholding);
        Assert.False(result.HasWithholdingToReport);
        Assert.Equal(0, result.UnestablishedWithheldCount);
        Assert.Equal(0, result.UnestablishedWithheldBytes);
    }

    [Fact]
    public void A_file_kept_for_its_patch_s_registrations_beside_one_the_scan_could_not_settle_reads_as_per_file()
    {
        // The per-file sentence speaks of the file the scan could not settle, and its
        // count and size leave the patch copy out.
        var result = new ScanResult([], [], 0,
            WithheldFiles: [File("a.msp", 1024), File("b.msi", 2048)],
            WithheldBy: new WithholdingSplit(DeclaredPatchRegisteredCount: 1, IdentityUnestablishedCount: 1),
            WithheldDeclaredPatchRegisteredBytes: 1024);

        Assert.Equal(WithholdingAccount.PerFile, result.Withholding);
        Assert.True(result.HasWithholdingToReport);
        Assert.Equal(1, result.UnestablishedWithheldCount);
        Assert.Equal(2048, result.UnestablishedWithheldBytes);
        Assert.Equal(new[] { WithholdingSplitArm.IdentityUnestablished }, result.WithheldBy.ArmsFired);
        Assert.True(result.NamedConditionsCoverEveryHeldBackFile);
    }

    [Fact]
    public void A_withheld_file_the_split_did_not_count_keeps_a_run_off_the_silent_reading_beside_a_patch_hold()
    {
        // THE MUST-MISS FOR THE DECLARED-PATCH-REGISTERED ARM, on the rule the must-miss
        // tests for the other two silent arms pin: the silent arms have to account for
        // the whole list, and a file none of them counted keeps the run per-file.
        var result = new ScanResult([], [], 0,
            WithheldFiles: [File("a.msp", 1024), File("b.msi", 2048)],
            WithheldBy: new WithholdingSplit(DeclaredPatchRegisteredCount: 1),
            WithheldDeclaredPatchRegisteredBytes: 1024);

        Assert.Equal(WithholdingAccount.PerFile, result.Withholding);
        Assert.Equal(1, result.UnestablishedWithheldCount);
        Assert.Equal(2048, result.UnestablishedWithheldBytes);
    }

    [Fact]
    public void A_patch_copy_the_scan_could_not_settle_is_spoken_of_with_a_line_of_its_own()
    {
        // The patch half's unsettled arm is not one of the silent arms. The per-file
        // sentence counts and sizes its file, and it names a line of its own, so the
        // reasons account for everything the sentence counts.
        var result = new ScanResult([], [], 0,
            WithheldFiles: [File("a.msp", 1024)],
            WithheldBy: new WithholdingSplit(DeclaredPatchUnestablishedCount: 1));

        Assert.Equal(WithholdingAccount.PerFile, result.Withholding);
        Assert.True(result.HasWithholdingToReport);
        Assert.Equal(1, result.UnestablishedWithheldCount);
        Assert.Equal(1024, result.UnestablishedWithheldBytes);
        Assert.Equal(new[] { WithholdingSplitArm.DeclaredPatchUnestablished }, result.WithheldBy.ArmsFired);
        Assert.True(result.NamedConditionsCoverEveryHeldBackFile);
    }

    [Fact]
    public void A_patch_copy_and_a_package_the_scan_could_not_settle_each_name_their_own_arm()
    {
        // Two members, not one shared: each half's unsettled arm is a different set of
        // inabilities, and a line for one would state a cause over the other's file.
        var result = new ScanResult([], [], 0,
            WithheldFiles: [File("a.msi", 1024), File("b.msp", 2048)],
            WithheldBy: new WithholdingSplit(
                DeclaredProductUnestablishedCount: 1, DeclaredPatchUnestablishedCount: 1));

        Assert.Equal(2, result.UnestablishedWithheldCount);
        Assert.Equal(new[]
        {
            WithholdingSplitArm.DeclaredProductUnestablished,
            WithholdingSplitArm.DeclaredPatchUnestablished,
        }, result.WithheldBy.ArmsFired);
        Assert.True(result.NamedConditionsCoverEveryHeldBackFile);
    }

    [Fact]
    public void A_patch_copy_the_scan_could_not_settle_is_counted_apart_from_one_kept_for_its_patch_s_registrations()
    {
        // One of each. The sentence counts and sizes the first alone, and only the
        // first has a line.
        var result = new ScanResult([], [], 0,
            WithheldFiles: [File("a.msp", 1024), File("b.msp", 2048)],
            WithheldBy: new WithholdingSplit(DeclaredPatchRegisteredCount: 1, DeclaredPatchUnestablishedCount: 1),
            WithheldDeclaredPatchRegisteredBytes: 1024);

        Assert.Equal(WithholdingAccount.PerFile, result.Withholding);
        Assert.Equal(1, result.UnestablishedWithheldCount);
        Assert.Equal(2048, result.UnestablishedWithheldBytes);
        Assert.Equal(new[] { WithholdingSplitArm.DeclaredPatchUnestablished }, result.WithheldBy.ArmsFired);
        Assert.True(result.NamedConditionsCoverEveryHeldBackFile);
    }

    [Fact]
    public void A_file_kept_for_its_age_beside_one_the_scan_could_not_settle_reads_as_per_file()
    {
        // The per-file sentence speaks of the file the scan could not settle, and its
        // count and size leave the other out.
        var result = new ScanResult([], [], 0,
            WithheldFiles: [File("a.msi", 1024), File("b.msi", 2048)],
            WithheldBy: new WithholdingSplit(UnderADayOldCount: 1, DeclaredProductUnestablishedCount: 1),
            WithheldUnderADayOldBytes: 1024);

        Assert.Equal(WithholdingAccount.PerFile, result.Withholding);
        Assert.True(result.HasWithholdingToReport);
        Assert.Equal(1, result.UnestablishedWithheldCount);
        Assert.Equal(2048, result.UnestablishedWithheldBytes);
        Assert.Equal(new[] { WithholdingSplitArm.DeclaredProductUnestablished }, result.WithheldBy.ArmsFired);
    }

    [Fact]
    public void A_withheld_file_the_split_did_not_count_keeps_a_run_off_the_silent_reading_beside_an_age_hold()
    {
        // THE MUST-MISS FOR THE AGE ARM, on the same rule as the one below for the
        // installed-program arm: the silent arms have to account for the whole list,
        // and a file none of them counted keeps the run per-file.
        var result = new ScanResult([], [], 0,
            WithheldFiles: [File("a.msi", 1024), File("b.msi", 2048)],
            WithheldBy: new WithholdingSplit(UnderADayOldCount: 1),
            WithheldUnderADayOldBytes: 1024);

        Assert.Equal(WithholdingAccount.PerFile, result.Withholding);
        Assert.Equal(1, result.UnestablishedWithheldCount);
        Assert.Equal(2048, result.UnestablishedWithheldBytes);
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
        // is among the files the held-back sentence counts, and no reason line speaks
        // for it.
        var result = new ScanResult([], [], 0,
            WithheldFiles: [File("a.msi", 1024)],
            WithheldBy: new WithholdingSplit(AgeUnestablishedCount: 1));

        Assert.Equal(WithholdingAccount.PerFile, result.Withholding);
        Assert.True(result.HasWithholdingToReport);
        Assert.Equal(1, result.UnestablishedWithheldCount);
        Assert.Equal(1024, result.UnestablishedWithheldBytes);
        Assert.Empty(result.WithheldBy.ArmsFired);
        Assert.False(result.NamedConditionsCoverEveryHeldBackFile);
    }

    [Fact]
    public void A_file_whose_age_was_not_established_is_counted_apart_from_one_under_a_day_old()
    {
        // One of each. The sentence counts and sizes the second alone.
        var result = new ScanResult([], [], 0,
            WithheldFiles: [File("a.msi", 1024), File("b.msi", 2048)],
            WithheldBy: new WithholdingSplit(UnderADayOldCount: 1, AgeUnestablishedCount: 1),
            WithheldUnderADayOldBytes: 1024);

        Assert.Equal(WithholdingAccount.PerFile, result.Withholding);
        Assert.Equal(1, result.UnestablishedWithheldCount);
        Assert.Equal(2048, result.UnestablishedWithheldBytes);
        Assert.False(result.NamedConditionsCoverEveryHeldBackFile);
    }

    [Fact]
    public void The_named_conditions_cover_a_run_whose_every_counted_file_has_a_line()
    {
        // The screen's unsettled file has a line; the file under a day old is not in
        // the count at all. So the reasons account for everything the sentence counts.
        var result = new ScanResult([], [], 0,
            WithheldFiles: [File("a.msi", 1024), File("b.msi", 2048)],
            WithheldBy: new WithholdingSplit(UnderADayOldCount: 1, DeclaredProductUnestablishedCount: 1),
            WithheldUnderADayOldBytes: 1024);

        Assert.True(result.NamedConditionsCoverEveryHeldBackFile);
    }

    [Fact]
    public void The_named_conditions_do_not_cover_a_run_beside_a_file_whose_age_was_not_established()
    {
        // The same run with the second file's age not established: the screen's line
        // is true of one of the two files the sentence counts and says nothing of the
        // other.
        var result = new ScanResult([], [], 0,
            WithheldFiles: [File("a.msi", 1024), File("b.msi", 2048)],
            WithheldBy: new WithholdingSplit(AgeUnestablishedCount: 1, DeclaredProductUnestablishedCount: 1));

        Assert.Equal(2, result.UnestablishedWithheldCount);
        Assert.False(result.NamedConditionsCoverEveryHeldBackFile);
    }

    [Fact]
    public void The_named_conditions_cover_a_wholesale_withholding()
    {
        // The legs speak for the wholesale arm.
        var result = new ScanResult([], [], 0,
            WithheldFiles: [File("a.msi", 1024), File("b.msi", 2048)],
            WithheldBy: new WithholdingSplit(WholesaleCount: 2));

        Assert.True(result.NamedConditionsCoverEveryHeldBackFile);
    }

    [Fact]
    public void A_file_kept_for_an_installed_program_beside_one_the_scan_could_not_settle_reads_as_per_file()
    {
        // The per-file sentence speaks of the file the scan could not settle, and its
        // count and size leave the other out.
        var result = new ScanResult([], [], 0,
            WithheldFiles: [File("a.msi", 1024), File("b.msi", 2048)],
            WithheldBy: new WithholdingSplit(DeclaredProductInstalledCount: 1, IdentityUnestablishedCount: 1),
            WithheldDeclaredProductInstalledBytes: 1024);

        Assert.Equal(WithholdingAccount.PerFile, result.Withholding);
        Assert.True(result.HasWithholdingToReport);
        Assert.Equal(1, result.UnestablishedWithheldCount);
        Assert.Equal(2048, result.UnestablishedWithheldBytes);
    }

    [Fact]
    public void A_withheld_file_the_split_did_not_count_keeps_a_run_off_the_installed_program_reading()
    {
        // THE MUST-MISS FOR THE DECLARED-PRODUCT-INSTALLED ARM. One file counted under
        // it and one counted under nothing: no other arm fired, and the run still reads
        // per-file, because the rule is that this arm accounts for the whole list. The
        // uncounted file is among those the per-file sentence counts.
        var result = new ScanResult([], [], 0,
            WithheldFiles: [File("a.msi", 1024), File("b.msi", 2048)],
            WithheldBy: new WithholdingSplit(DeclaredProductInstalledCount: 1),
            WithheldDeclaredProductInstalledBytes: 1024);

        Assert.Equal(WithholdingAccount.PerFile, result.Withholding);
        Assert.True(result.HasWithholdingToReport);
        Assert.Equal(1, result.UnestablishedWithheldCount);
        Assert.Equal(2048, result.UnestablishedWithheldBytes);
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
        // the flag is true and the list holds files from both. The wholesale sentence
        // is false of the half the identity pass took, and the per-file one is true of
        // every file here, so the superordinate is what this machine gets.
        var result = new ScanResult([], [], 0,
            WithheldFiles: [File("a.msi", 1024), File("b.msi", 2048), File("c.msi", 512)],
            WalkOfferWithheldWholesale: true,
            WithheldBy: new WithholdingSplit(IdentityUnestablishedCount: 1, WholesaleCount: 2));

        Assert.Equal(WithholdingAccount.PerFile, result.Withholding);
    }

    [Fact]
    public void A_withheld_file_the_split_did_not_count_reads_as_per_file()
    {
        // A file on the list that no arm counted leaves the wholesale arm short of the
        // list's own length, so the reading falls to the sentence that is true of every
        // file rather than sweeping the uncounted one under a cause nobody established.
        // Written as a fixture rather than as a comment because the direction it fails
        // in is the whole reason the rule is "the wholesale arm accounts for all of
        // them" and not "no per-file arm fired".
        var result = new ScanResult([], [], 0,
            WithheldFiles: [File("a.msi", 1024), File("b.msi", 2048)],
            WalkOfferWithheldWholesale: true,
            WithheldBy: new WithholdingSplit(WholesaleCount: 1));

        Assert.Equal(WithholdingAccount.PerFile, result.Withholding);
    }

    [Fact]
    public void The_wholesale_flag_on_its_own_does_not_decide_the_reading()
    {
        // THE MUST-MISS CONTROL FOR THE WHOLE RULE. Two results carrying the SAME flag
        // read differently, and two carrying different flags read the same, so nothing
        // here can be passing because the reading quietly follows the flag.
        var flagTrueWholesale = new ScanResult([], [], 0,
            WithheldFiles: [File("a.msi", 1024)],
            WalkOfferWithheldWholesale: true,
            WithheldBy: new WithholdingSplit(WholesaleCount: 1));
        var flagTruePerFile = new ScanResult([], [], 0,
            WithheldFiles: [File("a.msi", 1024)],
            WalkOfferWithheldWholesale: true,
            WithheldBy: new WithholdingSplit(IdentityUnestablishedCount: 1));
        var flagFalsePerFile = new ScanResult([], [], 0,
            WithheldFiles: [File("a.msi", 1024)],
            WithheldBy: new WithholdingSplit(IdentityUnestablishedCount: 1));

        Assert.Equal(WithholdingAccount.WholeWalkOffer, flagTrueWholesale.Withholding);
        Assert.Equal(WithholdingAccount.PerFile, flagTruePerFile.Withholding);
        Assert.Equal(WithholdingAccount.PerFile, flagFalsePerFile.Withholding);
    }

    // ---- What the window's finished screen counts ----
    //
    // EVERY FILE HELD BACK EXCEPT THOSE THE DECLARED-PRODUCT-INSTALLED AND
    // DECLARED-PATCH-REGISTERED ARMS KEPT, TOGETHER WITH THE SUPERSEDED FILES. Every file
    // and every arm's size below carries a different value, so a reading that subtracts
    // an arm it should not, or drops an addend, lands on a figure no assertion accepts.
    // The day-old fixtures set both the arm's count and its size, and the superseded
    // fixtures both the count and the size, because a fixture leaving either at zero
    // passes a reading that wrongly subtracts it.
    //
    // THE COMMAND LINE'S MEMBERS ARE ASSERTED BESIDE IT where the two readings part, so
    // a change reaching them from here fails.

    [Fact]
    public void A_scan_that_held_nothing_back_gives_the_finished_screen_nothing_to_count()
    {
        var noList = new ScanResult([], [], 0);
        var emptyList = new ScanResult([], [], 0, WithheldFiles: []);

        foreach (var result in new[] { noList, emptyList })
        {
            Assert.Equal(0, result.UnsettledHeldBackCount);
            Assert.Equal(0, result.UnsettledHeldBackBytes);
            Assert.False(result.HasUnsettledHeldBack);
            Assert.False(result.UnsettledHeldBackIsWholesale);
        }
    }

    [Fact]
    public void Files_kept_for_an_installed_program_are_not_counted_on_the_finished_screen()
    {
        var result = new ScanResult([], [], 0,
            WithheldFiles: [File("a.msi", 1024), File("b.msi", 2048)],
            WithheldBy: new WithholdingSplit(DeclaredProductInstalledCount: 2),
            WithheldDeclaredProductInstalledBytes: 3072);

        Assert.Equal(0, result.UnsettledHeldBackCount);
        Assert.Equal(0, result.UnsettledHeldBackBytes);
        Assert.False(result.HasUnsettledHeldBack);
    }

    [Fact]
    public void Patch_copies_kept_for_their_patch_s_registrations_are_not_counted_on_the_finished_screen()
    {
        var result = new ScanResult([], [], 0,
            WithheldFiles: [File("a.msp", 1024), File("b.msp", 2048)],
            WithheldBy: new WithholdingSplit(DeclaredPatchRegisteredCount: 2),
            WithheldDeclaredPatchRegisteredBytes: 3072);

        Assert.Equal(0, result.UnsettledHeldBackCount);
        Assert.Equal(0, result.UnsettledHeldBackBytes);
        Assert.False(result.HasUnsettledHeldBack);
    }

    [Fact]
    public void The_two_arms_together_are_not_counted_on_the_finished_screen()
    {
        var result = new ScanResult([], [], 0,
            WithheldFiles: [File("a.msi", 1024), File("b.msp", 2048)],
            WithheldBy: new WithholdingSplit(DeclaredProductInstalledCount: 1, DeclaredPatchRegisteredCount: 1),
            WithheldDeclaredProductInstalledBytes: 1024,
            WithheldDeclaredPatchRegisteredBytes: 2048);

        Assert.Equal(0, result.UnsettledHeldBackCount);
        Assert.Equal(0, result.UnsettledHeldBackBytes);
        Assert.False(result.HasUnsettledHeldBack);
    }

    [Fact]
    public void Files_under_a_day_old_are_counted_on_the_finished_screen_and_not_by_the_command_line()
    {
        var result = new ScanResult([], [], 0,
            WithheldFiles: [File("a.msi", 1024), File("b.msi", 2048)],
            WithheldBy: new WithholdingSplit(UnderADayOldCount: 2),
            WithheldUnderADayOldBytes: 3072);

        Assert.Equal(2, result.UnsettledHeldBackCount);
        Assert.Equal(3072, result.UnsettledHeldBackBytes);
        Assert.True(result.HasUnsettledHeldBack);
        Assert.False(result.UnsettledHeldBackIsWholesale);

        Assert.Equal(WithholdingAccount.KeptWithoutNotice, result.Withholding);
        Assert.False(result.HasWithholdingToReport);
        Assert.Equal(0, result.UnestablishedWithheldCount);
        Assert.Equal(0, result.UnestablishedWithheldBytes);
    }

    [Fact]
    public void A_file_under_a_day_old_is_counted_beside_one_kept_for_an_installed_program()
    {
        var result = new ScanResult([], [], 0,
            WithheldFiles: [File("a.msi", 1024), File("b.msi", 2048)],
            WithheldBy: new WithholdingSplit(DeclaredProductInstalledCount: 1, UnderADayOldCount: 1),
            WithheldDeclaredProductInstalledBytes: 1024,
            WithheldUnderADayOldBytes: 2048);

        Assert.Equal(1, result.UnsettledHeldBackCount);
        Assert.Equal(2048, result.UnsettledHeldBackBytes);
        Assert.False(result.UnsettledHeldBackIsWholesale);
    }

    [Fact]
    public void A_file_under_a_day_old_is_counted_beside_a_patch_copy_kept_for_its_patch_s_registrations()
    {
        var result = new ScanResult([], [], 0,
            WithheldFiles: [File("a.msp", 1024), File("b.msi", 4096)],
            WithheldBy: new WithholdingSplit(UnderADayOldCount: 1, DeclaredPatchRegisteredCount: 1),
            WithheldUnderADayOldBytes: 4096,
            WithheldDeclaredPatchRegisteredBytes: 1024);

        Assert.Equal(1, result.UnsettledHeldBackCount);
        Assert.Equal(4096, result.UnsettledHeldBackBytes);
    }

    [Fact]
    public void A_file_whose_age_was_not_established_is_counted_on_the_finished_screen()
    {
        var result = new ScanResult([], [], 0,
            WithheldFiles: [File("a.msi", 1024)],
            WithheldBy: new WithholdingSplit(AgeUnestablishedCount: 1));

        Assert.Equal(1, result.UnsettledHeldBackCount);
        Assert.Equal(1024, result.UnsettledHeldBackBytes);
        Assert.True(result.HasUnsettledHeldBack);
        Assert.False(result.UnsettledHeldBackIsWholesale);
    }

    [Fact]
    public void Superseded_files_held_back_are_counted_on_the_finished_screen_and_not_in_the_command_line_s_walk_sentence()
    {
        var result = new ScanResult([], [], 0,
            WithheldCount: 3,
            WithheldFiles: [],
            SupersededWithheldBytes: 16384);

        Assert.Equal(3, result.UnsettledHeldBackCount);
        Assert.Equal(16384, result.UnsettledHeldBackBytes);
        Assert.True(result.HasUnsettledHeldBack);
        Assert.False(result.UnsettledHeldBackIsWholesale);

        Assert.Equal(WithholdingAccount.Nothing, result.Withholding);
        Assert.False(result.HasWithholdingToReport);
        Assert.Equal(0, result.UnestablishedWithheldCount);
        Assert.Equal(0, result.UnestablishedWithheldBytes);
    }

    [Fact]
    public void A_wholesale_withholding_alone_takes_the_wholesale_reading_on_the_finished_screen()
    {
        var result = new ScanResult([], [], 0,
            WithheldFiles: [File("a.msi", 1024), File("b.msi", 2048)],
            WithheldBy: new WithholdingSplit(WholesaleCount: 2));

        Assert.Equal(2, result.UnsettledHeldBackCount);
        Assert.Equal(3072, result.UnsettledHeldBackBytes);
        Assert.True(result.UnsettledHeldBackIsWholesale);
    }

    [Fact]
    public void A_superseded_file_beside_a_wholesale_withholding_takes_the_per_file_reading_and_is_counted_with_it()
    {
        var result = new ScanResult([], [], 0,
            WithheldCount: 1,
            WithheldFiles: [File("a.msi", 1024), File("b.msi", 2048)],
            WithheldBy: new WithholdingSplit(WholesaleCount: 2),
            SupersededWithheldBytes: 16384);

        Assert.Equal(3, result.UnsettledHeldBackCount);
        Assert.Equal(1024 + 2048 + 16384, result.UnsettledHeldBackBytes);
        Assert.False(result.UnsettledHeldBackIsWholesale);

        Assert.Equal(WithholdingAccount.WholeWalkOffer, result.Withholding);
        Assert.Equal(2, result.UnestablishedWithheldCount);
        Assert.Equal(3072, result.UnestablishedWithheldBytes);
    }

    [Fact]
    public void Superseded_files_are_counted_with_the_walk_s_files_held_one_at_a_time()
    {
        var result = new ScanResult([], [], 0,
            WithheldCount: 2,
            WithheldFiles: [File("a.msi", 1024), File("b.msi", 2048)],
            WithheldBy: new WithholdingSplit(DeclaredProductInstalledCount: 1, ScreenUnansweredCount: 1),
            WithheldDeclaredProductInstalledBytes: 2048,
            SupersededWithheldBytes: 16384);

        Assert.Equal(3, result.UnsettledHeldBackCount);
        Assert.Equal(1024 + 16384, result.UnsettledHeldBackBytes);
        Assert.False(result.UnsettledHeldBackIsWholesale);
    }

    [Fact]
    public void A_withheld_file_no_arm_counted_is_counted_on_the_finished_screen()
    {
        // THE LIST LESS THE TWO ARMS, NOT A SUM OF THE OTHERS, so a file on the list the
        // split did not count, or one counted by an arm added later, is spoken of.
        var result = new ScanResult([], [], 0,
            WithheldFiles: [File("a.msi", 1024), File("b.msi", 2048)],
            WithheldBy: new WithholdingSplit(DeclaredProductInstalledCount: 1),
            WithheldDeclaredProductInstalledBytes: 1024);

        Assert.Equal(1, result.UnsettledHeldBackCount);
        Assert.Equal(2048, result.UnsettledHeldBackBytes);
        Assert.True(result.HasUnsettledHeldBack);
    }
}
