namespace InstallerClean.Helpers;

/// <summary>
/// How the stripe on a busy progress bar crosses it. Holds the arithmetic and
/// none of the drawing, so it can be exercised without a window;
/// <see cref="IndeterminateSweep"/> runs it.
///
/// The stripe starts wholly off the left end, its right edge on the track's left
/// edge, and finishes wholly off the right end, its left edge on the track's
/// right edge. Both ends come from the track, so the stripe crosses the whole bar
/// however wide the bar is drawn. The speed is constant, so a wider bar takes
/// longer to cross rather than being crossed faster.
/// </summary>
internal static class SweepTravel
{
    /// <summary>How fast the stripe moves, in device-independent pixels a second.</summary>
    public const double PixelsPerSecond = 286;

    /// <summary>
    /// Where the stripe's left edge starts and finishes, and how long one
    /// crossing takes, for a stripe <paramref name="stripeWidth"/> wide on a
    /// track <paramref name="trackWidth"/> wide.
    /// </summary>
    public static (double From, double To, TimeSpan Duration) For(double stripeWidth, double trackWidth)
    {
        var from = -stripeWidth;
        var to = trackWidth;
        return (from, to, Durations.FromSeconds((to - from) / PixelsPerSecond));
    }
}
