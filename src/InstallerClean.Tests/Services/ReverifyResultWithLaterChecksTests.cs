using InstallerClean.Models;
using InstallerClean.Services;

namespace InstallerClean.Tests.Services;

/// <summary>
/// What <see cref="ReverifyResult.WithLaterChecks"/> does to the first check's result: the
/// path lists and the reasons, and also the drives and shares a later check gave up and the
/// waits it made, each of which is a different question from what it does to the path lists.
/// </summary>
/// <remarks>
/// WithLaterChecks IS THE ONE PLACE IN THE APP THAT BUILDS A ReverifyResult OUT OF
/// ANOTHER ONE, so it is the one place a member can be dropped in the making. The tests
/// here cover the members it names, and one it does not name, which has to come through
/// unchanged.
/// </remarks>
public class ReverifyResultWithLaterChecksTests
{
    private const string Held = @"C:\Windows\Installer\held.msp";
    private const string Kept = @"C:\Windows\Installer\kept.msp";

    [Fact]
    public void The_fold_moves_a_held_back_path_from_surviving_to_dropped()
    {
        var before = new ReverifyResult(new[] { Held, Kept }, Array.Empty<string>());

        var after = before.WithLaterChecks(new[] { Held }, new HeldBackReasons(Reclaimed: 1));

        Assert.Equal(new[] { Kept }, after.Surviving);
        Assert.Equal(new[] { Held }, after.Dropped);
    }

    [Fact]
    public void The_fold_adds_the_later_reasons_to_the_first_checks()
    {
        var before = new ReverifyResult(
            new[] { Held, Kept }, new[] { @"C:\Windows\Installer\first.msp" },
            new HeldBackReasons(RecordsChanged: 1));

        var after = before.WithLaterChecks(new[] { Held }, new HeldBackReasons(Reclaimed: 1));

        Assert.Equal(new HeldBackReasons(Reclaimed: 1, RecordsChanged: 1), after.Reasons);
        Assert.Equal(new[] { @"C:\Windows\Installer\first.msp", Held }, after.Dropped);
    }

    [Fact]
    public void The_fold_carries_the_drives_and_shares_given_up_and_the_waits_through()
    {
        // They are the first check's, and the report reads them off the folded result, so a
        // rebuild that dropped them would send nought.
        var givenUp = new[]
        {
            new SourceRootGivenUp(@"\\nas\apps", SourceRootGiveUpRoute.NoAnswer, 2),
            new SourceRootGivenUp("E:", SourceRootGiveUpRoute.StoppedWaiting, 0),
        };
        var before = new ReverifyResult(
            new[] { Held, Kept }, Array.Empty<string>(),
            SourceRootsGivenUp: givenUp, SourceWaitCount: 3);

        var after = before.WithLaterChecks(new[] { Held }, new HeldBackReasons(Reclaimed: 1));

        Assert.Equal(givenUp, after.SourceRootsGivenUp);
        Assert.Equal(new[] { givenUp[0] }, after.SourceRootsGivenUpKeepingFiles);
        Assert.Equal(3, after.SourceWaitCount);
    }

    [Fact]
    public void The_roots_a_later_check_gave_up_follow_the_first_checks_in_order()
    {
        var first = new SourceRootGivenUp(@"\\nas\apps", SourceRootGiveUpRoute.NoAnswer, 2);
        var second = new SourceRootGivenUp("E:", SourceRootGiveUpRoute.StoppedWaiting, 1);
        var third = new SourceRootGivenUp("F:", SourceRootGiveUpRoute.NoAnswer, 4);
        var before = new ReverifyResult(
            new[] { Held, Kept }, Array.Empty<string>(),
            SourceRootsGivenUp: new[] { first });

        var after = before.WithLaterChecks(
            new[] { Held }, new HeldBackReasons(Reclaimed: 1),
            sourceRootsGivenUp: new[] { second, third });

        Assert.Equal(new[] { first, second, third }, after.SourceRootsGivenUp);
    }

    [Fact]
    public void The_wait_counts_are_added()
    {
        var before = new ReverifyResult(
            new[] { Held, Kept }, Array.Empty<string>(), SourceWaitCount: 3);

        var after = before.WithLaterChecks(
            new[] { Held }, new HeldBackReasons(Reclaimed: 1), sourceWaitCount: 2);

        Assert.Equal(5, after.SourceWaitCount);
    }

    [Fact]
    public void A_root_given_up_with_nothing_held_back_gives_a_new_result_carrying_it()
    {
        var givenUp = new SourceRootGivenUp("E:", SourceRootGiveUpRoute.StoppedWaiting, 1);
        var before = new ReverifyResult(new[] { Kept }, Array.Empty<string>());

        var after = before.WithLaterChecks(
            Array.Empty<string>(), default, sourceRootsGivenUp: new[] { givenUp });

        Assert.NotSame(before, after);
        Assert.Equal(new[] { givenUp }, after.SourceRootsGivenUp);
        Assert.Equal(new[] { Kept }, after.Surviving);
        Assert.Empty(after.Dropped);
    }

    [Fact]
    public void A_wait_count_with_nothing_held_back_gives_a_new_result_carrying_it()
    {
        var before = new ReverifyResult(new[] { Kept }, Array.Empty<string>(), SourceWaitCount: 1);

        var after = before.WithLaterChecks(Array.Empty<string>(), default, sourceWaitCount: 2);

        Assert.NotSame(before, after);
        Assert.Equal(3, after.SourceWaitCount);
        Assert.Equal(new[] { Kept }, after.Surviving);
    }

    [Fact]
    public void A_member_the_fold_does_not_name_is_carried_through()
    {
        var activity = PendingRebootResult.Block(PendingRebootReason.InstallerInProgressMarker);
        var before = new ReverifyResult(
            new[] { Held, Kept }, Array.Empty<string>(), InstallerActivity: activity);

        var after = before.WithLaterChecks(new[] { Held }, new HeldBackReasons(Reclaimed: 1));

        Assert.Same(activity, after.InstallerActivity);
    }

    [Fact]
    public void A_fold_that_adds_nothing_returns_the_result_it_was_given()
    {
        var before = new ReverifyResult(new[] { Kept }, Array.Empty<string>());

        Assert.Same(before, before.WithLaterChecks(Array.Empty<string>(), default));
    }
}
