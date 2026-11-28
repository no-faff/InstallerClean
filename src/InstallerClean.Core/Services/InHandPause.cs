using System.Diagnostics;
using InstallerClean.Compatibility;
using InstallerClean.Helpers;

namespace InstallerClean.Services;

/// <summary>
/// The pauses made around a file that has been out of its place: by a Move or a Delete
/// (<see cref="BatchRun"/>) and by the scan after it puts back what an earlier batch left
/// (<see cref="IInHandRecovery"/>).
/// </summary>
internal static class InHandPause
{
    /// <summary>
    /// How long is waited after a put-back before anything judges the file by its times:
    /// the full check a settle makes, and the scan that follows a recovery.
    ///
    /// A RENAME MOVES A FILE'S CHANGE TIME, and the scan and the full check pass a file no
    /// record names only where each of its times is before their own clock, which moves in
    /// the system clock's steps (<see cref="FileStampClock"/>, <see cref="CachedFileAge"/>).
    /// A clock read in the step the file was put back in would read the file as changed
    /// at or after it, and the file would be kept for that scan or dropped from that
    /// batch. The longest such step is under 16 milliseconds, and this is three of them.
    /// </summary>
    internal static readonly TimeSpan AfterAPutBack = Durations.FromMilliseconds(50);

    /// <summary>What a pause waits through, for a test that must not wait or that reads the pauses. Null is the app's own delay.</summary>
    internal static Func<TimeSpan, CancellationToken, Task>? ForTests { get; set; }

    /// <summary>
    /// Waits out what is left of <paramref name="length"/> since the moment
    /// <paramref name="since"/>, a timestamp of <see cref="Stopwatch"/>. Nought is no
    /// moment, and nothing is waited for. A cancel of <paramref name="token"/> ends the
    /// wait with OperationCanceledException.
    /// </summary>
    internal static Task SinceAsync(long since, TimeSpan length, CancellationToken token)
    {
        if (since == 0) return Task.CompletedTask;
        var passed = Durations.FromSeconds((Stopwatch.GetTimestamp() - since) / (double)Stopwatch.Frequency);
        return passed >= length
            ? Task.CompletedTask
            : (ForTests ?? FrameworkTask.Delay)(length - passed, token);
    }
}
