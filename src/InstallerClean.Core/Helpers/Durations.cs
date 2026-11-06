using System.Diagnostics;

namespace InstallerClean.Helpers;

/// <summary>
/// Two pieces of time arithmetic .NET 10 provides and .NET Framework does not, computed as
/// .NET 10 computes them (dotnet/runtime v10.0.0): a duration times a number, from
/// <c>TimeSpan.cs</c>'s <c>operator *</c>, and the time since a <see cref="Stopwatch"/>
/// timestamp, from <c>Stopwatch.cs</c>'s <c>GetElapsedTime</c>.
///
/// Both work in ticks, never through milliseconds: .NET Framework's
/// <c>TimeSpan.FromMilliseconds(double)</c> and <c>FromSeconds(double)</c> round their
/// argument to a whole millisecond, so a product of 400 ticks taken that way is zero.
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
