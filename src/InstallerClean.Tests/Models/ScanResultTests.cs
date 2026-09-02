using InstallerClean.Models;
using InstallerClean.Resources;
using Xunit;

namespace InstallerClean.Tests.Models;

/// <summary>
/// The derived figures on <see cref="ScanResult"/>, which the hosts read instead of
/// summing the lists themselves.
///
/// WHY A SUM GETS A TEST AT ALL. The withheld total is what the wholesale completion
/// screen prints as "held back {1}", and the one thing that figure must never be is
/// the folder's total: printing that would tell somebody that much was going spare
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
    // product it declares: for that file the scan was certain. So the wholesale reading
    // is the one that has to be earned, and the fixtures below differ in what they give
    // it to earn it with.

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
        // The declared-product screen keeping two files, which is the machine the
        // per-file reading exists for: the flag is false and the folder is not clean.
        var result = new ScanResult([], [], 0,
            WithheldFiles: [File("a.msi", 1024), File("b.msi", 2048)],
            WithheldBy: new WithholdingSplit(DeclaredProductInstalledCount: 2));

        Assert.Equal(WithholdingAccount.PerFile, result.Withholding);
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
}
