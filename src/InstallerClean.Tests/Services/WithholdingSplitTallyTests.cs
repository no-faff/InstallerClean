using System.Reflection;
using InstallerClean.Models;
using InstallerClean.Services;

namespace InstallerClean.Tests.Services;

/// <summary>
/// The arithmetic behind the withholding split, apart from the scan that fills it.
///
/// WHAT THIS COVERS AND WHAT IT DOES NOT. <see cref="WithholdingSplitTests"/> drives
/// real scans and pins that each decision counts the files it keeps; those need the
/// folder walk. This needs nothing, and pins the three things the walk cannot show:
/// that the total is the five and only the five, that a screen verdict the split does
/// not name is counted under neither of the two it does, and that the enum declares no
/// withholding verdict the split leaves unnamed.
///
/// THE LAST IS THE ONE WORTH HAVING. <c>DeclaredProductOutcome.Withholds</c> is
/// written as the complement of the two verdicts that keep a file, so a member added
/// to that enum withholds by default and arrives here unnamed. Counting it under
/// either named verdict would put a cause on it that nobody established, so it counts
/// nowhere and the split falls short of the list it splits. Walking the enum here is
/// what asks for that arm as the member is added.
/// </summary>
public class WithholdingSplitTallyTests
{
    /// <summary>
    /// The split's arms, read off its primary constructor so an arm added there is
    /// picked up here without anybody remembering a list. Every parameter is a count,
    /// so unlike the identity tally there is nothing to leave out.
    /// </summary>
    private static string[] Arms() =>
        typeof(WithholdingSplit)
            .GetConstructors()
            .OrderByDescending(c => c.GetParameters().Length)
            .First()
            .GetParameters()
            .Select(p => p.Name!)
            .OrderBy(n => n, StringComparer.Ordinal)
            .ToArray();

    private static int Read(string arm, WithholdingSplit split) =>
        (int)typeof(WithholdingSplit).GetProperty(arm, BindingFlags.Instance | BindingFlags.Public)!
            .GetValue(split)!;

    private static string[] Moved(WithholdingSplit split) =>
        Arms().Where(a => Read(a, split) != 0).ToArray();

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
        // And the total falls short of the file rather than claiming it under a cause
        // nobody established. This is a value cast past the enum, so no scan produces
        // it; a real member arriving in this position is what the walk below asks an
        // arm for.
        Assert.Equal(0, split.Total);
    }

    [Fact]
    public void Every_withholding_verdict_the_enum_declares_counts_into_an_arm_of_its_own()
    {
        // Driven from the enum rather than from the two the switch names, so a member
        // added to it arrives asking for an arm instead of being kept back and counted
        // under nothing. Withholds is the complement of the two verdicts that keep a
        // file, so the walk picks a new member up without anybody adding it here.
        var withholding = Enum.GetValues<DeclaredProductOutcome>()
            .Where(o => o.Withholds())
            .ToArray();

        // A set that came back empty would leave the loop below passing over no cases
        // at all, which reads exactly like a clean result. A floor rather than a count,
        // so splitting a verdict out does not fail this for the wrong reason.
        Assert.True(withholding.Length >= 2, "the withholding verdict list came back short");

        // AN ARM OF ITS OWN AND NOT MERELY AN ARM. A verdict folded into a neighbour's
        // case still counts into one, so a total of one cannot tell that apart from the
        // arm this asks for. The slot each verdict lands in is recorded and no two may
        // share one: counting a verdict under its neighbour would put that neighbour's
        // cause on it, and the arms reach a line the user reads.
        var seen = new Dictionary<string, DeclaredProductOutcome>(StringComparer.Ordinal);

        foreach (var outcome in withholding)
        {
            var tally = new FileSystemScanService.WithholdingSplitTally();

            tally.Screened(outcome);

            var moved = Moved(tally.Taken());

            Assert.True(moved.Length == 1,
                $"{outcome} withholds and moved {moved.Length} arms of the split "
                + $"({string.Join(", ", moved)}); it needs exactly one of its own.");
            Assert.True(!seen.TryGetValue(moved[0], out var already),
                $"{outcome} and {already} both count into {moved[0]}, so the split "
                + "cannot tell them apart and a surface reading it would state one "
                + "verdict's cause over the other's file.");
            seen[moved[0]] = outcome;
        }
    }
}
