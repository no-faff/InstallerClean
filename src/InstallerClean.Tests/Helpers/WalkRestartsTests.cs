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

    /// <summary>
    /// A read answering one scripted answer per call, and repeating the last: null for an
    /// error, "" for an absence, and anything else for a value.
    /// </summary>
    private sealed class ScriptedRead(params string?[] answers)
    {
        public int Reads { get; private set; }

        public string? Once() => answers[Math.Min(Reads++, answers.Length - 1)];
    }

    private static string? Read(WalkRestarts restarts, ScriptedRead read, string key = "one") =>
        restarts.KeyedRead(() => key, read.Once,
            stands: answer => answer is not null,
            standsAgain: answer => !string.IsNullOrEmpty(answer),
            agrees: string.Equals);

    [Fact]
    public void A_read_whose_first_answer_stands_is_made_once_with_no_pause()
    {
        var pauses = new List<TimeSpan>();
        var read = new ScriptedRead("");

        Assert.Equal("", Read(new WalkRestarts(pauses.Add), read));
        Assert.Equal(1, read.Reads);
        Assert.Empty(pauses);
    }

    [Fact]
    public void A_value_read_after_an_error_is_taken_where_the_read_after_the_longer_pause_gives_it_again()
    {
        var pauses = new List<TimeSpan>();
        var read = new ScriptedRead(null, "v");

        Assert.Equal("v", Read(new WalkRestarts(pauses.Add), read));
        Assert.Equal(3, read.Reads);
        Assert.Equal(new[] { First, Second }, pauses);
    }

    [Fact]
    public void A_value_read_after_an_error_that_the_next_read_does_not_give_again_leaves_the_first_read()
    {
        var read = new ScriptedRead(null, "v", "w");

        Assert.Null(Read(new WalkRestarts(_ => { }), read));
        Assert.Equal(3, read.Reads);
    }

    [Fact]
    public void An_absence_read_after_an_error_does_not_stand_and_the_first_read_is_the_answer()
    {
        var pauses = new List<TimeSpan>();
        var read = new ScriptedRead(null, "");

        Assert.Null(Read(new WalkRestarts(pauses.Add), read));
        Assert.Equal(WalkRestarts.Attempts, read.Reads);
        Assert.Equal(new[] { First, Second }, pauses);
    }

    [Fact]
    public void An_error_on_the_second_read_leaves_the_third_and_one_more_to_agree()
    {
        var pauses = new List<TimeSpan>();
        var read = new ScriptedRead(null, null, "v");

        Assert.Equal("v", Read(new WalkRestarts(pauses.Add), read));
        Assert.Equal(4, read.Reads);
        Assert.Equal(new[] { First, Second, Second }, pauses);
    }

    [Fact]
    public void A_key_whose_reads_never_stood_is_read_once_on_every_later_ask()
    {
        var restarts = new WalkRestarts(_ => { });

        var spent = new ScriptedRead((string?)null);
        Assert.Null(Read(restarts, spent));
        Assert.Equal(WalkRestarts.Attempts, spent.Reads);

        // The same key in another case, as product codes and accounts come.
        var later = new ScriptedRead(null, "v");
        Assert.Null(Read(restarts, later, "ONE"));
        Assert.Equal(1, later.Reads);

        // A key within it is read once too.
        var within = new ScriptedRead(null, "v");
        Assert.Null(restarts.KeyedRead(() => "two", within.Once, a => a is not null, a => !string.IsNullOrEmpty(a),
            string.Equals, within: () => "one"));
        Assert.Equal(1, within.Reads);

        // Another key is read again as before.
        var other = new ScriptedRead(null, "v");
        Assert.Equal("v", Read(restarts, other, "three"));
    }

    [Fact]
    public void A_key_whose_read_was_answered_on_a_later_attempt_is_read_again_on_its_next_ask()
    {
        var restarts = new WalkRestarts(_ => { });

        Read(restarts, new ScriptedRead(null, "v"));

        Assert.Equal("v", Read(restarts, new ScriptedRead(null, "v")));
    }

    [Fact]
    public void Keyed_reads_and_keyed_walks_share_one_pause_limit()
    {
        var pauses = new List<TimeSpan>();
        var restarts = new WalkRestarts(pauses.Add);

        for (var key = 0; key < 20; key++)
            Read(restarts, new ScriptedRead((string?)null), $"read {key}");
        restarts.KeyedWalk(new ScriptedWalk(false, true).Once, w => w.Stands);

        var paused = pauses.Aggregate(TimeSpan.Zero, (sum, wait) => sum + wait);
        Assert.True(paused <= WalkRestarts.KeyedPauseLimit, $"paused {paused}");
    }

    [Fact]
    public void A_value_read_after_an_error_is_not_taken_where_the_pause_before_the_read_agreeing_with_it_cannot_be_made()
    {
        var pauses = new List<TimeSpan>();
        var restarts = new WalkRestarts(pauses.Add);

        // Reads that never stand spend the pause limit.
        for (var key = 0; key < 20; key++)
            Read(restarts, new ScriptedRead((string?)null), $"read {key}");
        var paused = pauses.Count;

        var read = new ScriptedRead(null, "v");
        Assert.Null(Read(restarts, read, "last"));
        Assert.Equal(paused, pauses.Count);
    }

    [Fact]
    public void Two_values_that_differ_spend_no_key()
    {
        var restarts = new WalkRestarts(_ => { });

        Assert.Null(Read(restarts, new ScriptedRead(null, "v", "w")));

        Assert.Equal("v", Read(restarts, new ScriptedRead(null, "v")));
    }

    [Fact]
    public void A_first_read_the_rule_names_spends_the_key_it_is_within()
    {
        var restarts = new WalkRestarts(_ => { });

        restarts.KeyedRead(() => "pairing", new ScriptedRead("gone").Once, a => a is not "gone",
            a => a is not "gone", string.Equals, within: () => "installation", spendsWithin: a => a == "gone");

        var other = new ScriptedRead(null, "v");
        Assert.Null(restarts.KeyedRead(() => "another pairing", other.Once, a => a is not null,
            a => !string.IsNullOrEmpty(a), string.Equals, within: () => "installation"));
        Assert.Equal(1, other.Reads);
    }
}
