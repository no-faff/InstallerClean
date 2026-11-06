using InstallerClean.Helpers;

namespace InstallerClean.Tests.Helpers;

/// <summary>
/// <see cref="Durations"/> computes as .NET 10's <c>TimeSpan</c> multiplication and
/// <c>Stopwatch.GetElapsedTime</c> do: in ticks, a product to the nearest tick with a tie
/// going to the even one, and an elapsed time truncated to a whole tick.
/// </summary>
public class DurationsTests
{
    [Theory]
    // A fortieth of a second times a thousandth: 400 ticks, well under a millisecond.
    [InlineData(400_000, 0.001, 400)]
    // Ties go to the even tick.
    [InlineData(500, 0.001, 0)]
    [InlineData(1_500, 0.001, 2)]
    [InlineData(2_500, 0.001, 2)]
    [InlineData(3_500, 0.001, 4)]
    // Above and below a tie go to the nearer tick.
    [InlineData(1_501, 0.001, 2)]
    [InlineData(1_499, 0.001, 1)]
    [InlineData(-1_500, 0.001, -2)]
    [InlineData(1_000_000, 2, 2_000_000)]
    [InlineData(123, 0, 0)]
    public void A_duration_times_a_number_is_rounded_to_the_nearest_tick(long ticks, double factor, long expected)
    {
        Assert.Equal(expected, Durations.Times(TimeSpan.FromTicks(ticks), factor).Ticks);
    }

    [Fact]
    public void A_NaN_factor_is_refused()
    {
        var thrown = Assert.Throws<ArgumentException>(() => Durations.Times(TimeSpan.FromSeconds(1), double.NaN));
        Assert.Equal("factor", thrown.ParamName);
    }

    [Theory]
    [InlineData(2.0)]
    [InlineData(double.PositiveInfinity)]
    [InlineData(-2.0)]
    public void A_product_past_what_a_TimeSpan_holds_is_an_overflow(double factor)
    {
        Assert.Throws<OverflowException>(() => Durations.Times(TimeSpan.MaxValue, factor));
    }

    [Fact]
    public void Zero_times_infinity_is_an_overflow()
    {
        Assert.Throws<OverflowException>(() => Durations.Times(TimeSpan.Zero, double.PositiveInfinity));
    }

    [Fact]
    public void A_product_that_rounds_to_the_largest_tick_count_is_the_largest_TimeSpan()
    {
        Assert.Equal(TimeSpan.MaxValue, Durations.Times(TimeSpan.MaxValue, 1.0));
    }

    [Theory]
    // A clock ticking ten million times a second counts ticks as they are.
    [InlineData(0, 12_345, 10_000_000, 12_345)]
    // Three times a second: one tick of it is 3,333,333.33 ticks, truncated.
    [InlineData(0, 1, 3, 3_333_333)]
    [InlineData(5, 7, 3, 6_666_666)]
    // A clock running backwards, which a timestamp taken out of order gives.
    [InlineData(7, 5, 3, -6_666_666)]
    [InlineData(42, 42, 3, 0)]
    public void The_time_between_two_timestamps_is_truncated_to_a_whole_tick(
        long start, long end, long frequency, long expected)
    {
        Assert.Equal(expected, Durations.Between(start, end, (double)TimeSpan.TicksPerSecond / frequency).Ticks);
    }

    [Theory]
    [InlineData(0.5, 0.0)]
    [InlineData(1.5, 2.0)]
    [InlineData(2.5, 2.0)]
    [InlineData(-2.5, -2.0)]
    [InlineData(0.49999999999999994, 0.0)]
    [InlineData(1.4999999999999998, 1.0)]
    [InlineData(2.5000000000000004, 3.0)]
    [InlineData(4503599627370495.5, 4503599627370496.0)]
    [InlineData(4503599627370497.0, 4503599627370497.0)]
    [InlineData(-0.3, -0.0)]
    public void A_number_rounds_to_the_nearest_whole_one_and_a_tie_to_the_even_one(double value, double expected)
    {
        var rounded = Durations.RoundToEven(value);
        Assert.Equal(expected, rounded);
        // The sign of a zero too, compared on the bits.
        Assert.Equal(BitConverter.DoubleToInt64Bits(expected), BitConverter.DoubleToInt64Bits(rounded));
    }

    [Fact]
    public void Infinities_and_NaN_round_to_themselves()
    {
        Assert.Equal(double.PositiveInfinity, Durations.RoundToEven(double.PositiveInfinity));
        Assert.Equal(double.NegativeInfinity, Durations.RoundToEven(double.NegativeInfinity));
        Assert.True(double.IsNaN(Durations.RoundToEven(double.NaN)));
    }

    [Theory]
    [InlineData(double.NaN, 0)]
    [InlineData(1e19, long.MaxValue)]
    [InlineData(-1e19, long.MinValue)]
    [InlineData(9223372036854775807.0, long.MaxValue)]
    [InlineData(-12.9, -12)]
    [InlineData(12.9, 12)]
    public void A_number_too_large_for_a_long_is_held_to_its_limit(double value, long expected)
    {
        Assert.Equal(expected, Durations.Saturating(value));
    }

    [Fact]
    public void A_time_too_long_for_a_TimeSpan_is_the_longest_one()
    {
        Assert.Equal(TimeSpan.MaxValue, Durations.Between(0, long.MaxValue, 10.0));
        Assert.Equal(TimeSpan.MinValue, Durations.Between(long.MaxValue, 0, 10.0));
    }

    [Fact]
    public void The_time_since_a_timestamp_is_never_negative_and_grows()
    {
        var start = System.Diagnostics.Stopwatch.GetTimestamp();
        var first = Durations.Since(start);
        var second = Durations.Since(start);

        Assert.True(first >= TimeSpan.Zero);
        Assert.True(second >= first);
    }
}
