using InstallerClean.Helpers;

namespace InstallerClean.Tests.Helpers;

/// <summary>
/// <see cref="WalkRestarts"/> makes a walk again where its answer does not stand. Each
/// test hands it a walk that answers from a script, and a pause that records the wait
/// and does not sleep.
/// </summary>
public class WalkRestartsTests
{
    private static readonly TimeSpan First = TimeSpan.FromMilliseconds(50);
    private static readonly TimeSpan Second = TimeSpan.FromMilliseconds(250);

    /// <summary>A walk answering one scripted result per call, and repeating the last.</summary>
    private sealed class ScriptedWalk(params bool[] stands)
    {
        public int Walks { get; private set; }

        public (int Walk, bool Stands) Once()
        {
            var answer = stands[Math.Min(Walks, stands.Length - 1)];
            return (++Walks, answer);
        }
    }

    [Fact]
    public void A_walk_whose_answer_stands_is_made_once_with_no_pause()
    {
        var pauses = new List<TimeSpan>();
        var walk = new ScriptedWalk(true);

        var result = new WalkRestarts(pauses.Add).Walk(walk.Once, w => w.Stands);

        Assert.Equal((1, true), result);
        Assert.Empty(pauses);
    }

    [Fact]
    public void A_walk_whose_answer_does_not_stand_is_made_again_and_the_second_is_the_answer()
    {
        var pauses = new List<TimeSpan>();
        var walk = new ScriptedWalk(false, true);

        var result = new WalkRestarts(pauses.Add).Walk(walk.Once, w => w.Stands);

        Assert.Equal((2, true), result);
        Assert.Equal(new[] { First }, pauses);
    }

    [Fact]
    public void A_walk_whose_answer_never_stands_is_made_three_times_and_the_first_is_handed_back()
    {
        var pauses = new List<TimeSpan>();
        var walk = new ScriptedWalk(false);

        var result = new WalkRestarts(pauses.Add).Walk(walk.Once, w => w.Stands);

        Assert.Equal((1, false), result);
        Assert.Equal(3, walk.Walks);
        Assert.Equal(new[] { First, Second }, pauses);
    }

    [Fact]
    public void A_keyed_walk_restarts_as_any_other_does()
    {
        var pauses = new List<TimeSpan>();
        var walk = new ScriptedWalk(false, false, true);

        var result = new WalkRestarts(pauses.Add).KeyedWalk(walk.Once, w => w.Stands);

        Assert.Equal((3, true), result);
        Assert.Equal(new[] { First, Second }, pauses);
    }

    [Fact]
    public void Keyed_walks_restart_without_a_pause_once_their_pauses_reach_the_limit()
    {
        var pauses = new List<TimeSpan>();
        var restarts = new WalkRestarts(pauses.Add);

        // Far more lists than the limit pays for, none of whose walks ever stands.
        var walksMade = new List<int>();
        for (var list = 0; list < 40; list++)
        {
            var walk = new ScriptedWalk(false);
            restarts.KeyedWalk(walk.Once, w => w.Stands);
            walksMade.Add(walk.Walks);
        }

        var paused = pauses.Aggregate(TimeSpan.Zero, (sum, wait) => sum + wait);
        Assert.True(paused <= WalkRestarts.KeyedPauseLimit, $"paused {paused}");

        // Every list is walked the full number of times, the last with no pause.
        Assert.All(walksMade, made => Assert.Equal(WalkRestarts.Attempts, made));
        var before = pauses.Count;
        restarts.KeyedWalk(new ScriptedWalk(false).Once, w => w.Stands);
        Assert.Equal(before, pauses.Count);
    }

    [Fact]
    public void A_list_walked_once_a_scan_restarts_after_the_keyed_walks_have_spent_their_limit()
    {
        var pauses = new List<TimeSpan>();
        var restarts = new WalkRestarts(pauses.Add);
        for (var list = 0; list < 40; list++)
            restarts.KeyedWalk(new ScriptedWalk(false).Once, w => w.Stands);
        pauses.Clear();

        var walk = new ScriptedWalk(false, true);
        var result = restarts.Walk(walk.Once, w => w.Stands);

        Assert.Equal((2, true), result);
        Assert.Equal(new[] { First }, pauses);
    }

    [Fact]
    public void With_no_pause_handed_in_a_cancel_of_the_token_ends_the_wait()
    {
        // The pause the app itself makes, which the suite replaces for its whole run.
        var forTests = WalkRestarts.PauseForTests;
        WalkRestarts.PauseForTests = null;
        try
        {
            using var cancelled = new CancellationTokenSource();
            cancelled.Cancel();
            var walk = new ScriptedWalk(false);

            Assert.Throws<OperationCanceledException>(
                () => WalkRestarts.For(cancelled.Token).Walk(walk.Once, w => w.Stands));
            Assert.Equal(1, walk.Walks);
        }
        finally
        {
            WalkRestarts.PauseForTests = forTests;
        }
    }

    [Fact]
    public void A_pause_that_throws_ends_the_walks()
    {
        var walk = new ScriptedWalk(false);
        var restarts = new WalkRestarts(_ => throw new OperationCanceledException());

        Assert.Throws<OperationCanceledException>(() => restarts.Walk(walk.Once, w => w.Stands));
        Assert.Equal(1, walk.Walks);
    }
}
