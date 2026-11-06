using InstallerClean.Services;

namespace InstallerClean.Tests.Services;

/// <summary>
/// The rule that decides whether a cached file was last created, written and changed
/// before the scan's clock, which a file the folder walk found must have been before the
/// scan offers it.
///
/// EVERY FIXTURE STATES ITS TIMES AGAINST ONE CLOCK, so each test varies one time and
/// the reader can see which one moved the answer.
/// </summary>
public class CachedFileAgeTests
{
    private static readonly DateTimeOffset Clock =
        new(2030, 6, 15, 12, 0, 0, TimeSpan.Zero);

    private static readonly DateTime TwoDaysBefore = Clock.UtcDateTime.AddDays(-2);

    private static FileTimes AllAt(DateTime at) => new(at, at, at);

    // ---- What lets a file through ----

    [Fact]
    public void A_file_whose_three_times_are_two_days_before_the_clock_is_before_it()
    {
        Assert.True(CachedFileAge.IsBeforeTheClock(FileTimesRead.Read, AllAt(TwoDaysBefore), Clock));
    }

    [Fact]
    public void A_file_whose_latest_time_is_a_tick_before_the_clock_is_before_it()
    {
        // Nothing about the file's age is asked beyond that: a tick before the clock is
        // as good as a year.
        Assert.True(CachedFileAge.IsBeforeTheClock(
            FileTimesRead.Read, AllAt(Clock.UtcDateTime - TimeSpan.FromTicks(1)), Clock));
        Assert.True(CachedFileAge.IsBeforeTheClock(
            FileTimesRead.Read, AllAt(Clock.UtcDateTime.AddMinutes(-1)), Clock));
    }

    // ---- What keeps a file back ----

    [Fact]
    public void A_file_whose_latest_time_is_the_clock_is_not()
    {
        Assert.False(CachedFileAge.IsBeforeTheClock(FileTimesRead.Read, AllAt(Clock.UtcDateTime), Clock));
    }

    [Fact]
    public void A_late_change_time_keeps_a_file_whose_creation_and_last_write_are_old()
    {
        // A copy given its source's creation and last-write times, whose change
        // time is its own.
        var times = new FileTimes(
            CreationUtc: TwoDaysBefore.AddYears(-5),
            LastWriteUtc: TwoDaysBefore.AddYears(-5),
            ChangeUtc: Clock.UtcDateTime.AddSeconds(5));

        Assert.False(CachedFileAge.IsBeforeTheClock(FileTimesRead.Read, times, Clock));
    }

    [Fact]
    public void A_late_creation_time_keeps_a_file_whose_last_write_and_change_are_old()
    {
        // A copy given its source's last-write and change times, whose creation time
        // is its own.
        var times = new FileTimes(
            CreationUtc: Clock.UtcDateTime.AddSeconds(5),
            LastWriteUtc: TwoDaysBefore.AddYears(-5),
            ChangeUtc: TwoDaysBefore.AddYears(-5));

        Assert.False(CachedFileAge.IsBeforeTheClock(FileTimesRead.Read, times, Clock));
    }

    [Fact]
    public void A_late_last_write_keeps_a_file_whose_creation_and_change_are_old()
    {
        var times = new FileTimes(
            CreationUtc: TwoDaysBefore,
            LastWriteUtc: Clock.UtcDateTime.AddSeconds(5),
            ChangeUtc: TwoDaysBefore);

        Assert.False(CachedFileAge.IsBeforeTheClock(FileTimesRead.Read, times, Clock));
    }

    [Fact]
    public void A_time_later_than_the_scan_clock_keeps_the_file()
    {
        Assert.False(CachedFileAge.IsBeforeTheClock(
            FileTimesRead.Read, AllAt(Clock.UtcDateTime.AddDays(3)), Clock));
    }

    [Fact]
    public void Every_answer_but_a_read_keeps_the_file_whatever_times_it_carries()
    {
        // Walked over the whole enum with times that would pass, so the rule has to
        // be refusing on the outcome and not on the times. A member added later
        // keeps the file by default, and this is where that is held.
        var permitting = Enum.GetValues<FileTimesRead>()
            .Where(o => CachedFileAge.IsBeforeTheClock(o, AllAt(TwoDaysBefore), Clock))
            .ToArray();

        Assert.Equal(new[] { FileTimesRead.Read }, permitting);
    }

    // ---- Which of the two keeping verdicts ----

    [Fact]
    public void A_latest_time_the_ahead_limit_after_the_clock_is_not_before_it()
    {
        // A copy written just before the clock was set back reads as ahead of it. A day
        // ahead is still a time the scan can place.
        Assert.Equal(CachedFileAgeVerdict.NotBeforeTheClock, CachedFileAge.Judge(
            FileTimesRead.Read, AllAt(Clock.UtcDateTime + CachedFileAge.AheadLimit), Clock));
    }

    [Fact]
    public void A_latest_time_more_than_the_ahead_limit_after_the_clock_is_not_established()
    {
        Assert.Equal(CachedFileAgeVerdict.Unestablished, CachedFileAge.Judge(
            FileTimesRead.Read,
            AllAt(Clock.UtcDateTime + CachedFileAge.AheadLimit + TimeSpan.FromTicks(1)),
            Clock));
    }

    [Fact]
    public void A_latest_time_at_the_clock_is_not_before_it_and_a_tick_earlier_is()
    {
        Assert.Equal(CachedFileAgeVerdict.NotBeforeTheClock, CachedFileAge.Judge(
            FileTimesRead.Read, AllAt(Clock.UtcDateTime), Clock));
        Assert.Equal(CachedFileAgeVerdict.BeforeTheClock, CachedFileAge.Judge(
            FileTimesRead.Read, AllAt(Clock.UtcDateTime - TimeSpan.FromTicks(1)), Clock));
    }

    [Fact]
    public void Every_answer_but_a_read_is_times_not_established()
    {
        // Times that would read as not before the clock, so the verdict has to come from
        // the outcome.
        var notEstablished = Enum.GetValues<FileTimesRead>()
            .Where(o => CachedFileAge.Judge(o, AllAt(Clock.UtcDateTime.AddSeconds(5)), Clock)
                == CachedFileAgeVerdict.Unestablished)
            .ToArray();

        Assert.Equal(Enum.GetValues<FileTimesRead>().Where(o => o != FileTimesRead.Read), notEstablished);
    }

    // ---- Which time is read ----

    [Fact]
    public void The_latest_of_the_three_is_the_one_read()
    {
        var early = TwoDaysBefore.AddDays(-10);
        var mid = TwoDaysBefore.AddDays(-5);
        var late = TwoDaysBefore;

        Assert.Equal(late, CachedFileAge.Latest(new FileTimes(late, early, mid)));
        Assert.Equal(late, CachedFileAge.Latest(new FileTimes(early, late, mid)));
        Assert.Equal(late, CachedFileAge.Latest(new FileTimes(mid, early, late)));
    }

    [Fact]
    public void The_ahead_limit_is_one_day()
    {
        Assert.Equal(TimeSpan.FromDays(1), CachedFileAge.AheadLimit);
    }

    // ---- Whether the wall clock went back while the times were read ----

    [Fact]
    public void A_wall_clock_that_kept_pace_did_not_go_back()
    {
        var clock = new SteppingClock(Clock, TimeSpan.FromMinutes(5), TimeSpan.FromMinutes(5));

        Assert.False(ClockWentBack(clock));
    }

    [Fact]
    public void A_wall_clock_that_ran_ahead_did_not_go_back()
    {
        var clock = new SteppingClock(Clock, TimeSpan.FromHours(1), TimeSpan.FromSeconds(2));

        Assert.False(ClockWentBack(clock));
    }

    [Fact]
    public void A_wall_clock_set_back_before_any_time_passed_went_back()
    {
        var clock = new SteppingClock(Clock, TimeSpan.FromSeconds(-1), TimeSpan.Zero);

        Assert.True(ClockWentBack(clock));
    }

    [Theory]
    // One second: a thousandth of it is one millisecond.
    [InlineData(1, 10_000, false)]
    [InlineData(1, 10_001, true)]
    // Ten seconds: a thousandth of it is ten milliseconds.
    [InlineData(10, 100_000, false)]
    [InlineData(10, 100_001, true)]
    // A hundred seconds: a thousandth of it is the tenth of a second the limit allows.
    [InlineData(100, 1_000_000, false)]
    [InlineData(100, 1_000_001, true)]
    // Five minutes, as a scan waiting on a slow share runs: a thousandth of it is three
    // tenths of a second, and the limit holds it to one.
    [InlineData(300, 1_000_000, false)]
    [InlineData(300, 1_000_001, true)]
    [InlineData(300, 3_000_000, true)]
    // An hour, as a scan across a sleep runs.
    [InlineData(3_600, 1_000_000, false)]
    [InlineData(3_600, 1_000_001, true)]
    public void The_wall_clock_may_fall_behind_by_a_thousandth_of_the_time_that_passed_up_to_a_tenth_of_a_second(
        int seconds, long behindTicks, bool wentBack)
    {
        var elapsed = TimeSpan.FromSeconds(seconds);
        var clock = new SteppingClock(Clock, elapsed - TimeSpan.FromTicks(behindTicks), elapsed);

        Assert.Equal(wentBack, ClockWentBack(clock));
    }

    // Forty milliseconds: a thousandth of it is four hundred ticks, a fraction of a
    // millisecond, which the allowance keeps.
    [Theory]
    [InlineData(400, false)]
    [InlineData(401, true)]
    public void In_a_short_pass_the_allowance_is_kept_to_the_tick(long behindTicks, bool wentBack)
    {
        var elapsed = TimeSpan.FromMilliseconds(40);
        var clock = new SteppingClock(Clock, elapsed - TimeSpan.FromTicks(behindTicks), elapsed);

        Assert.Equal(wentBack, ClockWentBack(clock));
    }

    [Fact]
    public void The_allowance_is_a_thousandth_of_the_time_that_passed()
    {
        Assert.Equal(0.001, CachedFileAge.SlowRateAllowance);
    }

    [Fact]
    public void The_most_the_wall_clock_may_fall_behind_is_a_tenth_of_a_second()
    {
        Assert.Equal(TimeSpan.FromMilliseconds(100), CachedFileAge.FallBehindLimit);
    }

    [Fact]
    public void A_step_back_is_measured_against_the_most_time_the_two_readings_allow()
    {
        // The first reading took fifty milliseconds, so the wall clock was read somewhere in
        // them. Ten milliseconds behind over the whole second is beyond a thousandth of it,
        // though the wall clock is ahead of the least time that can have passed between the
        // two, and of the time halfway through the first reading.
        var clock = new SteppingClock(Clock, TimeSpan.Zero, TimeSpan.Zero);
        var atStart = new WallClockReading(0, Clock, TimeSpan.FromMilliseconds(50).Ticks, 1);
        var atEnd = new WallClockReading(
            TimeSpan.FromSeconds(1).Ticks, Clock + TimeSpan.FromMilliseconds(990), TimeSpan.FromSeconds(1).Ticks, 1);

        Assert.True(CachedFileAge.ClockWentBack(clock, atStart, atEnd));
    }

    // ---- Reading the wall clock ----

    [Fact]
    public void A_wall_clock_reading_takes_the_precise_time_between_two_timestamps()
    {
        var clock = new ScriptedTimestamps(Clock, Clock.AddSeconds(-30), 5, 6);

        var reading = CachedFileAge.ReadWallClock(clock);

        Assert.Equal(new WallClockReading(5, Clock.AddSeconds(-30), 6, 1), reading);
    }

    [Fact]
    public void A_clock_with_no_precise_time_gives_its_own()
    {
        var clock = new FixedTimestampsOnly(Clock);

        Assert.Equal(Clock, CachedFileAge.ReadWallClock(clock).Wall);
    }

    [Fact]
    public void A_reading_that_took_too_long_is_taken_again()
    {
        // The first reading was interrupted for a millisecond; the second took none.
        var wide = TimeSpan.FromMilliseconds(1).Ticks;
        var clock = new ScriptedTimestamps(Clock, Clock, 0, wide, wide + 10, wide + 10);

        var reading = CachedFileAge.ReadWallClock(clock);

        Assert.Equal(2, reading.Tries);
        Assert.Equal(wide + 10, reading.TimestampBefore);
        Assert.Equal(TimeSpan.Zero, reading.Width(clock));
    }

    [Fact]
    public void A_reading_no_try_could_make_narrow_keeps_the_narrowest_of_every_try()
    {
        // Every reading took at least a millisecond; the fifth took the least.
        var timestamps = new List<long>();
        long at = 0;
        for (var i = 1; i <= CachedFileAge.WallClockTries; i++)
        {
            var width = TimeSpan.FromMilliseconds(i == 5 ? 1 : 2).Ticks;
            timestamps.Add(at);
            timestamps.Add(at + width);
            at += width + 1;
        }
        var clock = new ScriptedTimestamps(Clock, Clock, timestamps.ToArray());

        var reading = CachedFileAge.ReadWallClock(clock);

        Assert.Equal(CachedFileAge.WallClockTries, reading.Tries);
        Assert.Equal(TimeSpan.FromMilliseconds(1), reading.Width(clock));
        Assert.Equal(timestamps[8], reading.TimestampBefore);
    }

    [Fact]
    public void A_reading_is_kept_at_once_where_it_took_no_longer_than_twenty_microseconds()
    {
        Assert.Equal(200, CachedFileAge.NarrowReading.Ticks);
        Assert.Equal(16, CachedFileAge.WallClockTries);
    }

    /// <summary>
    /// <see cref="CachedFileAge.ClockWentBack(TimeProvider, WallClockReading)"/> asked as the
    /// scan asks it: the wall clock read first, then the question.
    /// </summary>
    private static bool ClockWentBack(TimeProvider clock) =>
        CachedFileAge.ClockWentBack(clock, CachedFileAge.ReadWallClock(clock));

    /// <summary>
    /// A clock whose own time is <paramref name="own"/>, whose precise time is
    /// <paramref name="precise"/>, and whose timestamps are <paramref name="timestamps"/> in
    /// turn, in ticks, the last repeating.
    /// </summary>
    private sealed class ScriptedTimestamps(DateTimeOffset own, DateTimeOffset precise, params long[] timestamps)
        : TimeProvider, IPreciseWallClock
    {
        private int _next;

        public override long TimestampFrequency => TimeSpan.TicksPerSecond;

        public override long GetTimestamp() => timestamps[Math.Min(_next++, timestamps.Length - 1)];

        public override DateTimeOffset GetUtcNow() => own;

        public DateTimeOffset GetPreciseUtcNow() => precise;
    }

    /// <summary>A clock with no precise time of its own and a timestamp that stands still.</summary>
    private sealed class FixedTimestampsOnly(DateTimeOffset now) : TimeProvider
    {
        public override long GetTimestamp() => 0;

        public override DateTimeOffset GetUtcNow() => now;
    }

    // ---- The volume root the reader builds on ----

    [Theory]
    [InlineData(@"\\?\Volume{0a1b2c3d-0000-1111-2222-333344445555}\Windows\Installer\1a2b3.msi",
        @"\\?\Volume{0a1b2c3d-0000-1111-2222-333344445555}\")]
    [InlineData(@"\\?\volume{0a1b2c3d-0000-1111-2222-333344445555}\a.msp",
        @"\\?\volume{0a1b2c3d-0000-1111-2222-333344445555}\")]
    public void A_volume_guid_path_yields_its_root(string finalPath, string root)
    {
        Assert.Equal(root, FileTimesReader.VolumeRoot(finalPath));
    }

    [Theory]
    [InlineData(@"C:\Windows\Installer\1a2b3.msi")]
    [InlineData(@"\\?\C:\Windows\Installer\1a2b3.msi")]
    [InlineData(@"\\?\UNC\server\share\Installer\1a2b3.msi")]
    [InlineData(@"\\?\UNC\server\share{0a1b2c3d}\Installer\1a2b3.msi")]
    [InlineData(@"C:\Folder{0a1b2c3d}\1a2b3.msi")]
    [InlineData(@"\\?\Volume{0a1b2c3d-0000-1111-2222-333344445555")]
    [InlineData("")]
    public void Anything_but_a_volume_guid_path_yields_no_root(string finalPath)
    {
        // Null is what keeps the file: the reader answers VolumeUnestablished for it.
        Assert.Null(FileTimesReader.VolumeRoot(finalPath));
    }
}
