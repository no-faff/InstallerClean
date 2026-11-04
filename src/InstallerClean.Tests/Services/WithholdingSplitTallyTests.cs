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
/// that the total is every arm and only the arms, that a screen verdict the split does
/// not name is counted under none of the ones it does, and that the enum declares no
/// withholding verdict the split leaves unnamed.
///
/// THE LAST IS THE ONE WORTH HAVING. <c>DeclaredProductOutcome.Withholds</c> is
/// written as the complement of the verdicts that let a file through, so a member
/// added to that enum withholds by default and arrives here unnamed. Counting it under
/// a named verdict would put a cause on it that nobody established, so it counts
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
    public void The_total_is_every_arm_and_nothing_else()
    {
        var split = new WithholdingSplit(
            IdentityUnestablishedCount: 1,
            WholesaleCount: 3,
            DeclaredProductInstalledCount: 9,
            DeclaredProductUnestablishedCount: 27,
            ScreenUnansweredCount: 81,
            NotBeforeTheClockCount: 243,
            AgeUnestablishedCount: 729,
            DeclaredPatchRegisteredCount: 2187,
            DeclaredPatchUnestablishedCount: 6561,
            ContainmentRefusedCount: 19683,
            ContainmentUnestablishedCount: 59049,
            SecondCopyUnestablishedCount: 177147,
            ScanSignalCount: 531441,
            ClockSetBackCount: 1594323);

        // Distinct powers of three, so any member left out of the sum or counted twice
        // changes the answer rather than happening to cancel: with every member taken
        // nought, once or twice, the sum has one spelling in base three, and it is all
        // ones only where every member is taken once.
        Assert.Equal(2391484, split.Total);
    }

    [Fact]
    public void A_split_nobody_filled_is_all_zeroes_and_a_zero_total()
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
        // A candidate whose own identity the screen could not read counts in the
        // identity comparison's arm.
        tally.Screened(DeclaredProductOutcome.CandidateIdentityUnestablished, DeclaredProductInstalledCause.None);
        tally.Wholesale(7);
        tally.ScreenUnanswered(3);
        tally.Screened(DeclaredProductOutcome.DeclaredProductInstalled, DeclaredProductInstalledCause.None);
        tally.Screened(DeclaredProductOutcome.Unestablished, DeclaredProductInstalledCause.None);
        tally.Screened(DeclaredProductOutcome.Unestablished, DeclaredProductInstalledCause.None);
        tally.Screened(DeclaredProductOutcome.DeclaredPatchRegistered, DeclaredProductInstalledCause.None);
        tally.Screened(DeclaredProductOutcome.DeclaredPatchRegistered, DeclaredProductInstalledCause.None);
        tally.Screened(DeclaredProductOutcome.DeclaredPatchRegistered, DeclaredProductInstalledCause.None);
        tally.Screened(DeclaredProductOutcome.DeclaredPatchUnestablished, DeclaredProductInstalledCause.None);
        tally.Screened(DeclaredProductOutcome.DeclaredPatchUnestablished, DeclaredProductInstalledCause.None);
        tally.Screened(DeclaredProductOutcome.DeclaredPatchUnestablished, DeclaredProductInstalledCause.None);
        tally.Screened(DeclaredProductOutcome.DeclaredPatchUnestablished, DeclaredProductInstalledCause.None);
        tally.Contained(CandidateGuard.RemovalSafety.Refused);
        tally.Contained(CandidateGuard.RemovalSafety.Refused);
        tally.Contained(CandidateGuard.RemovalSafety.Refused);
        tally.Contained(CandidateGuard.RemovalSafety.Refused);
        tally.Contained(CandidateGuard.RemovalSafety.Refused);
        tally.Contained(CandidateGuard.RemovalSafety.Unproven);
        tally.Contained(CandidateGuard.RemovalSafety.Unproven);
        tally.Contained(CandidateGuard.RemovalSafety.Unproven);
        tally.Contained(CandidateGuard.RemovalSafety.Unproven);
        tally.Contained(CandidateGuard.RemovalSafety.Unproven);
        tally.Contained(CandidateGuard.RemovalSafety.Unproven);
        tally.ScanSignal(8);
        tally.ClockSetBack(9);

        var split = tally.Taken();

        Assert.Equal(3, split.IdentityUnestablishedCount);
        Assert.Equal(7, split.WholesaleCount);
        Assert.Equal(3, split.ScreenUnansweredCount);
        Assert.Equal(1, split.DeclaredProductInstalledCount);
        Assert.Equal(2, split.DeclaredProductUnestablishedCount);
        Assert.Equal(3, split.DeclaredPatchRegisteredCount);
        Assert.Equal(4, split.DeclaredPatchUnestablishedCount);
        Assert.Equal(5, split.ContainmentRefusedCount);
        Assert.Equal(6, split.ContainmentUnestablishedCount);
        Assert.Equal(8, split.ScanSignalCount);
        Assert.Equal(9, split.ClockSetBackCount);
        Assert.Equal(51, split.Total);
    }

    [Fact]
    public void A_screen_verdict_that_lets_the_file_through_is_counted_nowhere()
    {
        // The verdicts that let a file through never reach the tally, the caller asking
        // Withholds first. Passing them anyway pins that none is filed under a
        // withholding arm if that call site is ever restructured.
        var tally = new FileSystemScanService.WithholdingSplitTally();

        tally.Screened(DeclaredProductOutcome.DeclaredProductNotInstalled, DeclaredProductInstalledCause.None);
        tally.Screened(DeclaredProductOutcome.DeclaredProductCachedAsAnotherFile, DeclaredProductInstalledCause.None);
        tally.Screened(DeclaredProductOutcome.DeclaredPatchNotRegistered, DeclaredProductInstalledCause.None);
        tally.Screened(DeclaredProductOutcome.DeclaredPatchCachedAsAnotherFile, DeclaredProductInstalledCause.None);

        Assert.Equal(default, tally.Taken());
    }

    [Fact]
    public void A_withholding_verdict_the_split_does_not_name_is_counted_under_none_of_its_arms()
    {
        // Cast past the enum's members deliberately: this is the state an outcome added
        // to the enum would arrive in before anybody split it out, and Withholds would
        // already be keeping its files back. No named arm may claim it, because none of
        // their causes was established for it.
        var tally = new FileSystemScanService.WithholdingSplitTally();

        tally.Screened((DeclaredProductOutcome)99, DeclaredProductInstalledCause.None);

        var split = tally.Taken();

        Assert.Equal(0, split.DeclaredProductInstalledCount);
        Assert.Equal(0, split.DeclaredProductUnestablishedCount);
        Assert.Equal(0, split.DeclaredPatchRegisteredCount);
        Assert.Equal(0, split.DeclaredPatchUnestablishedCount);
        // And the total falls short of the file rather than claiming it under a cause
        // nobody established. This is a value cast past the enum, so no scan produces
        // it; a real member arriving in this position is what the walk below asks an
        // arm for.
        Assert.Equal(0, split.Total);
    }

    [Fact]
    public void Every_withholding_verdict_the_enum_declares_counts_into_an_arm_of_its_own()
    {
        // Driven from the enum rather than from the verdicts the switch names, so a
        // member added to it arrives asking for an arm instead of being kept back and
        // counted under nothing. Withholds is the complement of the verdicts that let a
        // file through, so the walk picks a new member up without anybody adding it here.
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

            tally.Screened(outcome, DeclaredProductInstalledCause.None);

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

    /// <summary>The counts by cause, by member name, read off the record's primary constructor.</summary>
    private static Dictionary<string, int> ByCause(DeclaredProductInstalledCauses causes) =>
        typeof(DeclaredProductInstalledCauses)
            .GetConstructors()
            .OrderByDescending(c => c.GetParameters().Length)
            .First()
            .GetParameters()
            .ToDictionary(
                p => p.Name!,
                p => (int)typeof(DeclaredProductInstalledCauses)
                    .GetProperty(p.Name!, BindingFlags.Instance | BindingFlags.Public)!.GetValue(causes)!,
                StringComparer.Ordinal);

    [Fact]
    public void Every_cause_the_enum_declares_counts_into_the_member_of_its_own_name()
    {
        // Driven from the enum, so a cause added to it arrives asking for a member of its own
        // instead of being counted in the arm and under no cause.
        var causes = Enum.GetValues<DeclaredProductInstalledCause>()
            .Where(c => c != DeclaredProductInstalledCause.None)
            .ToArray();

        // A list that came back empty would leave the loop below checking nothing.
        Assert.True(causes.Length >= 2, "the cause list came back short");
        Assert.Equal(
            causes.Select(c => c.ToString()).Order(StringComparer.Ordinal),
            ByCause(DeclaredProductInstalledCauses.None).Keys.Order(StringComparer.Ordinal));

        foreach (var cause in causes)
        {
            var tally = new FileSystemScanService.WithholdingSplitTally();

            tally.Screened(DeclaredProductOutcome.DeclaredProductInstalled, cause);

            var moved = ByCause(tally.DeclaredProductInstalledCauses).Where(m => m.Value != 0).ToArray();
            Assert.Equal(cause.ToString(), Assert.Single(moved).Key);
            Assert.Equal(1, moved[0].Value);
            Assert.Equal(1, tally.Taken().DeclaredProductInstalledCount);
        }
    }

    [Fact]
    public void The_counts_by_cause_add_up_to_the_arm_where_every_file_has_a_cause()
    {
        var tally = new FileSystemScanService.WithholdingSplitTally();

        tally.Screened(DeclaredProductOutcome.DeclaredProductInstalled, DeclaredProductInstalledCause.IsItsCachedPackage);
        tally.Screened(DeclaredProductOutcome.DeclaredProductInstalled, DeclaredProductInstalledCause.SourcesGivenUp);
        tally.Screened(DeclaredProductOutcome.DeclaredProductInstalled, DeclaredProductInstalledCause.SourcesGivenUp);
        tally.Screened(DeclaredProductOutcome.DeclaredProductInstalled, DeclaredProductInstalledCause.ByName);

        Assert.Equal(4, tally.Taken().DeclaredProductInstalledCount);
        Assert.Equal(4, ByCause(tally.DeclaredProductInstalledCauses).Values.Sum());
        Assert.Equal(2, tally.DeclaredProductInstalledCauses.SourcesGivenUp);
    }

    [Fact]
    public void A_file_with_no_cause_is_counted_in_the_arm_and_under_no_cause()
    {
        // None, and values cast past the enum's members either side, are counted in the arm, which
        // is the verdict's, and under none of the causes, none of which was given.
        var tally = new FileSystemScanService.WithholdingSplitTally();

        tally.Screened(DeclaredProductOutcome.DeclaredProductInstalled, DeclaredProductInstalledCause.None);
        tally.Screened(DeclaredProductOutcome.DeclaredProductInstalled, (DeclaredProductInstalledCause)99);
        tally.Screened(DeclaredProductOutcome.DeclaredProductInstalled, (DeclaredProductInstalledCause)(-1));

        Assert.Equal(3, tally.Taken().DeclaredProductInstalledCount);
        Assert.Equal(DeclaredProductInstalledCauses.None, tally.DeclaredProductInstalledCauses);
    }

    [Fact]
    public void A_cause_given_with_any_other_verdict_is_counted_under_no_cause()
    {
        var tally = new FileSystemScanService.WithholdingSplitTally();

        foreach (var outcome in Enum.GetValues<DeclaredProductOutcome>()
                     .Where(o => o != DeclaredProductOutcome.DeclaredProductInstalled))
            tally.Screened(outcome, DeclaredProductInstalledCause.IsItsCachedPackage);

        Assert.Equal(0, tally.Taken().DeclaredProductInstalledCount);
        Assert.Equal(DeclaredProductInstalledCauses.None, tally.DeclaredProductInstalledCauses);
    }

    [Fact]
    public void The_not_before_the_clock_arm_counts_its_files_and_sums_their_size()
    {
        var tally = new FileSystemScanService.WithholdingSplitTally();

        tally.NotBeforeTheClock(1024);
        tally.NotBeforeTheClock(2048);
        tally.NotBeforeTheClock(4096);

        Assert.Equal(3, tally.Taken().NotBeforeTheClockCount);
        Assert.Equal(7168, tally.NotBeforeTheClockBytes);
    }
}
