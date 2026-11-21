using System.Diagnostics;
using InstallerClean.Models;

namespace InstallerClean.Helpers;

/// <summary>
/// Passes the scan's milestones straight through and admits one ticker update per
/// interval.
///
/// The scan's ticker fires once per installed program and, while it counts the
/// cached files, once every so many of them, so the rate is the machine's rather
/// than the scan's. Each update that gets through crosses to the dispatcher,
/// replaces a line of text and starts an animation, and a name replaced faster
/// than a screen draws is a name nobody reads. Held at the reporting thread,
/// before anything is posted, so what a phase costs to report is the interval
/// rather than its item count.
///
/// The scan's phases run one after another and never report at once, but each
/// reports from whichever thread it runs on, so the timestamp crosses threads and
/// is read and written as one that does.
/// </summary>
internal sealed class ThrottledScanProgress : IProgress<ScanProgressUpdate>
{
    /// <summary>
    /// Long enough that a phase reporting thousands of items posts tens of
    /// updates a second, short enough that the bar's own easing, which is longer
    /// than this, runs into the next update and reads as one continuous motion.
    /// </summary>
    public static readonly TimeSpan DefaultInterval = Durations.FromMilliseconds(100);

    private readonly IProgress<ScanProgressUpdate> _inner;
    private readonly long _intervalTicks;
    private long _lastReport;

    public ThrottledScanProgress(IProgress<ScanProgressUpdate> inner)
        : this(inner, DefaultInterval) { }

    public ThrottledScanProgress(IProgress<ScanProgressUpdate> inner, TimeSpan interval)
    {
        _inner = inner;
        _intervalTicks = (long)(interval.TotalSeconds * Stopwatch.Frequency);
    }

    public void Report(ScanProgressUpdate value)
    {
        // A milestone passes whatever the interval says, and clears the clock so
        // the first ticker of the phase it opens is shown at once rather than
        // waiting out an interval the previous phase started.
        if (value.IsMilestone)
        {
            Volatile.Write(ref _lastReport, 0);
            _inner.Report(value);
            return;
        }

        // A wait's two updates pass whatever the interval says and leave the clock
        // alone. They come one pair to a wait, and the second is what takes the first
        // off the screen, so dropping it would leave a wait shown that is over.
        if (value.IsWait)
        {
            _inner.Report(value);
            return;
        }

        if (Admits(value)) _inner.Report(value);
    }

    private bool Admits(ScanProgressUpdate update)
    {
        var last = Volatile.Read(ref _lastReport);
        var now = Stopwatch.GetTimestamp();
        var due = last == 0 || now - last >= _intervalTicks;

        // The last item of a phase passes whether it is due or not, so a bar
        // filled from these positions reaches the end of its band instead of
        // stopping wherever the interval last let something through.
        if (!due && !(update.Total > 0 && update.Position >= update.Total)) return false;

        Volatile.Write(ref _lastReport, now);
        return true;
    }
}
