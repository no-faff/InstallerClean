using InstallerClean.Interop.Native;

namespace InstallerClean.Services;

/// <summary>
/// The clock the age check judges a cached file's times against
/// (<see cref="CachedFileAge"/>): the system time as it stood at the last tick of the
/// system clock, the time that advances in the same steps as the times a file system
/// stamps.
///
/// THE ORDINARY CLOCK IS THE WRONG ONE HERE. <see cref="TimeProvider.System"/> answers
/// with <see cref="DateTimeOffset.UtcNow"/>, whose source the runtime chooses. Where that
/// is the precise system time, it runs up to a tick ahead of this one, so judged against
/// it a file written just after the clock is read can carry a time just before it. This
/// clock asks Windows for each time itself.
///
/// Timers and timestamps are the base class's, so the timestamp is the system's monotonic
/// one. The age check measures the precise time's advance against it
/// (<see cref="GetPreciseUtcNow"/>, <see cref="CachedFileAge.ReadWallClock"/>).
/// </summary>
internal sealed class FileStampClock : TimeProvider, IPreciseWallClock
{
    public override DateTimeOffset GetUtcNow()
    {
        Kernel32.GetSystemTimeAsFileTime(out var fileTime);
        return new DateTimeOffset(DateTime.FromFileTimeUtc(fileTime));
    }

    /// <summary>
    /// The precise system time, which moves between ticks with the monotonic clock.
    /// </summary>
    public DateTimeOffset GetPreciseUtcNow()
    {
        Kernel32.GetSystemTimePreciseAsFileTime(out var fileTime);
        return new DateTimeOffset(DateTime.FromFileTimeUtc(fileTime));
    }
}
