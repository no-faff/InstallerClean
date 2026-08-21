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
}
