using InstallerClean.Models;
using InstallerClean.Services;

namespace InstallerClean.Tests.Services;

/// <summary>
/// The arithmetic behind the withholding split, apart from the scan that fills it.
///
/// WHAT THIS COVERS AND WHAT IT DOES NOT. <see cref="WithholdingSplitTests"/> drives
/// real scans and pins that each decision counts the files it keeps; those need the
/// folder walk. This needs nothing, and pins the two things the walk cannot show: that
/// the total is the five and only the five, and that a screen verdict the split does
/// not name is counted under neither of the two it does.
///
/// THE SECOND IS THE ONE WORTH HAVING. <c>DeclaredProductOutcome.Withholds</c> is
/// written as the complement of the two verdicts that keep a file, so a member added
/// to that enum withholds by default and arrives here unnamed. Counting it under
/// either named verdict would put a cause on it that nobody established. Counting it
/// nowhere leaves the five short of the list, which is what the completeness assertion
/// in every scan test reports.
/// </summary>
public class WithholdingSplitTallyTests
{
    [Fact]
    public void The_total_is_the_five_and_nothing_else()
    {
        var split = new WithholdingSplit(
            IdentityUnestablishedCount: 1,
            WholesaleCount: 20,
            DeclaredProductInstalledCount: 300,
            DeclaredProductUnestablishedCount: 4000,
            ScreenUnansweredCount: 50000);

        // Distinct powers of ten, so any member left out of the sum or counted twice
        // changes the answer rather than happening to cancel.
        Assert.Equal(54321, split.Total);
    }

    [Fact]
    public void A_split_nobody_filled_is_five_zeroes_and_a_zero_total()
    {
        // The state of the great majority of scans, and the default of the struct the
        // result carries, so a scan that kept nothing back cannot report a figure.
        Assert.Equal(0, default(WithholdingSplit).Total);
    }

    [Fact]
    public void Each_arm_counts_into_its_own_member()
    {
        var tally = new FileSystemScanService.WithholdingSplitTally();

        tally.IdentityUnestablished();
        tally.IdentityUnestablished();
        tally.Wholesale(7);
        tally.ScreenUnanswered(3);
        tally.Screened(DeclaredProductOutcome.DeclaredProductInstalled);
        tally.Screened(DeclaredProductOutcome.Unestablished);
        tally.Screened(DeclaredProductOutcome.Unestablished);

        var split = tally.Taken();

        Assert.Equal(2, split.IdentityUnestablishedCount);
        Assert.Equal(7, split.WholesaleCount);
        Assert.Equal(3, split.ScreenUnansweredCount);
        Assert.Equal(1, split.DeclaredProductInstalledCount);
        Assert.Equal(2, split.DeclaredProductUnestablishedCount);
        Assert.Equal(15, split.Total);
    }

    [Fact]
    public void A_screen_verdict_that_keeps_the_file_is_counted_nowhere()
    {
        // The two verdicts that let a file through never reach the tally, the caller
        // asking Withholds first. Passing them anyway pins that neither is filed under
        // a withholding arm if that call site is ever restructured.
        var tally = new FileSystemScanService.WithholdingSplitTally();

        tally.Screened(DeclaredProductOutcome.NotAProductPackage);
        tally.Screened(DeclaredProductOutcome.DeclaredProductNotInstalled);

        Assert.Equal(default, tally.Taken());
    }

    [Fact]
    public void A_withholding_verdict_the_split_does_not_name_is_counted_under_neither()
    {
        // Cast past the enum's members deliberately: this is the state a fifth outcome
        // would arrive in before anybody split it out, and Withholds would already be
        // keeping its files back. Neither named arm may claim it, because neither of
        // their causes was established for it.
        var tally = new FileSystemScanService.WithholdingSplitTally();

        tally.Screened((DeclaredProductOutcome)99);

        var split = tally.Taken();

        Assert.Equal(0, split.DeclaredProductInstalledCount);
        Assert.Equal(0, split.DeclaredProductUnestablishedCount);
        // And the total falls short of the file, which is what the completeness
        // assertion in the scan tests turns into a failure rather than a silent gap.
        Assert.Equal(0, split.Total);
    }
}
