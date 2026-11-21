using System.Diagnostics;

namespace InstallerClean.Helpers;

/// <summary>
/// Time arithmetic .NET 10 provides and .NET Framework does not, computed as .NET 10
/// computes it (dotnet/runtime v10.0.0): a duration times a number, from
/// <c>TimeSpan.cs</c>'s <c>operator *</c>; a number of milliseconds, seconds, minutes,
/// hours or days as a duration, from its <c>FromMilliseconds</c>, <c>FromSeconds</c>,
/// <c>FromMinutes</c>, <c>FromHours</c> and <c>FromDays</c> taking a double; and the time
/// since a <see cref="Stopwatch"/> timestamp, from <c>Stopwatch.cs</c>'s
/// <c>GetElapsedTime</c>.
///
/// All of it works in ticks, never through milliseconds: each of .NET Framework's
/// <c>TimeSpan.From*(double)</c> members rounds its duration to a whole millisecond, so a
/// product of 400 ticks taken that way is zero.
/// </summary>
internal static class Durations
{
    /// <summary>
    /// <paramref name="duration"/> times <paramref name="factor"/>, to the nearest tick, a
    /// tie going to the even tick. A NaN factor is an <see cref="ArgumentException"/>, and a
    /// product outside what a <see cref="TimeSpan"/> holds an <see cref="OverflowException"/>.
    /// </summary>
    public static TimeSpan Times(TimeSpan duration, double factor)
    {
        if (double.IsNaN(factor))
            throw new ArgumentException("TimeSpan does not accept floating point Not-a-Number values.", nameof(factor));

        var ticks = RoundToEven(duration.Ticks * factor);
        if (ticks > TimeSpan.MaxValue.Ticks || ticks < TimeSpan.MinValue.Ticks || double.IsNaN(ticks))
            throw new OverflowException("TimeSpan overflowed because the duration is too long.");
        if (ticks == TimeSpan.MaxValue.Ticks)
            return TimeSpan.MaxValue;
        return new TimeSpan((long)ticks);
    }

    /// <summary>
    /// <paramref name="seconds"/> as a duration, truncated to a whole tick, as .NET 10's
    /// <c>TimeSpan.FromSeconds(double)</c> answers.
    /// </summary>
    public static TimeSpan FromSeconds(double seconds) => Interval(seconds, TimeSpan.TicksPerSecond);

    /// <summary>
    /// <paramref name="milliseconds"/> as a duration, truncated to a whole tick, as .NET
    /// 10's <c>TimeSpan.FromMilliseconds(double)</c> answers.
    /// </summary>
    public static TimeSpan FromMilliseconds(double milliseconds) => Interval(milliseconds, TimeSpan.TicksPerMillisecond);

    /// <summary>
    /// <paramref name="minutes"/> as a duration, truncated to a whole tick, as .NET 10's
    /// <c>TimeSpan.FromMinutes(double)</c> answers.
    /// </summary>
    public static TimeSpan FromMinutes(double minutes) => Interval(minutes, TimeSpan.TicksPerMinute);

    /// <summary>
    /// <paramref name="hours"/> as a duration, truncated to a whole tick, as .NET 10's
    /// <c>TimeSpan.FromHours(double)</c> answers.
    /// </summary>
    public static TimeSpan FromHours(double hours) => Interval(hours, TimeSpan.TicksPerHour);

    /// <summary>
    /// <paramref name="days"/> as a duration, truncated to a whole tick, as .NET 10's
    /// <c>TimeSpan.FromDays(double)</c> answers.
    /// </summary>
    public static TimeSpan FromDays(double days) => Interval(days, TimeSpan.TicksPerDay);

    /// <summary>
    /// <paramref name="value"/> units of <paramref name="scale"/> ticks each, from
    /// <c>TimeSpan.cs</c>'s <c>Interval</c> and <c>IntervalFromDoubleTicks</c>. A NaN is
    /// an <see cref="ArgumentException"/> naming no parameter, and a duration outside
    /// what a <see cref="TimeSpan"/> holds an <see cref="OverflowException"/>.
    /// </summary>
    private static TimeSpan Interval(double value, double scale)
    {
        if (double.IsNaN(value))
            throw new ArgumentException("TimeSpan does not accept floating point Not-a-Number values.");

        var ticks = value * scale;
        if (ticks > long.MaxValue || ticks < long.MinValue || double.IsNaN(ticks))
            throw new OverflowException("TimeSpan overflowed because the duration is too long.");
        if (ticks == long.MaxValue)
            return TimeSpan.MaxValue;
        return new TimeSpan((long)ticks);
    }

    // .NET 10's Stopwatch keeps this ratio in a static, s_tickFrequency.
    private static readonly double StopwatchTickFrequency = (double)TimeSpan.TicksPerSecond / Stopwatch.Frequency;

    /// <summary>The time since <paramref name="startingTimestamp"/>, a <see cref="Stopwatch.GetTimestamp"/> value.</summary>
    public static TimeSpan Since(long startingTimestamp) =>
        Between(startingTimestamp, Stopwatch.GetTimestamp(), StopwatchTickFrequency);

    /// <summary>
    /// The time from one timestamp to another on a clock with
    /// <paramref name="tickFrequency"/> TimeSpan ticks to each of its own, truncated to a
    /// whole tick.
    /// </summary>
    internal static TimeSpan Between(long startingTimestamp, long endingTimestamp, double tickFrequency) =>
        new(Saturating((endingTimestamp - startingTimestamp) * tickFrequency));

    /// <summary>
    /// <paramref name="value"/> to the nearest whole number, a tie going to the even one, as
    /// .NET 10's <c>Math.Round(double)</c> answers. On .NET Framework that method is native
    /// code, so the rounding is done here: adding and taking away 2^52 leaves a double with
    /// no fraction, rounded in the processor's own nearest-even mode.
    /// </summary>
    internal static double RoundToEven(double value)
    {
        const double TwoToThe52 = 4503599627370496.0;
        if (!(Math.Abs(value) < TwoToThe52) || value == 0) return value; // whole already, infinite, NaN or a zero of either sign
        var shift = value < 0 ? -TwoToThe52 : TwoToThe52;
        var rounded = (value + shift) - shift;
        // A value that rounds to zero keeps its sign, as Math.Round's does.
        return rounded == 0 ? (value < 0 ? -0.0 : 0.0) : rounded;
    }

    /// <summary>
    /// <paramref name="value"/> truncated to a whole number, held to what a long can carry and
    /// NaN as zero, as .NET 10 converts a double to a long. .NET Framework's conversion gives
    /// long.MinValue for anything out of range.
    /// </summary>
    internal static long Saturating(double value)
    {
        if (double.IsNaN(value)) return 0;
        if (value >= 9223372036854775807.0) return long.MaxValue;
        if (value <= -9223372036854775808.0) return long.MinValue;
        return (long)value;
    }
}
