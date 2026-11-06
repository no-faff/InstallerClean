using InstallerClean.Helpers;
namespace InstallerClean.Services;

/// <summary>
/// What <see cref="IFileTimesReader.ReadOutcome"/> established, with each way it can
/// fall short kept apart rather than collapsed into one <c>false</c>.
///
/// ONLY <see cref="Read"/> LETS A FILE THROUGH, and it means more than that the times
/// were read: the file is a plain file on a local, fixed NTFS volume. Every other
/// member keeps the file back. Nothing branches on which one it is; they are named
/// apart because they are different facts about a file, and a caller that ever
/// counts them should be able to.
/// </summary>
public enum FileTimesRead
{
    /// <summary>
    /// The times were read off a plain file on a local, fixed NTFS volume. The only
    /// member that fills the out value, and the only one a caller treats as an
    /// answer.
    /// </summary>
    Read,

    /// <summary>There was no string to open.</summary>
    NotAPath,

    /// <summary>
    /// Nothing is at the path: the file went between the walk and this read.
    /// </summary>
    NamesNothing,

    /// <summary>
    /// Something is at the path and no handle could be opened on it.
    /// </summary>
    OpenRefused,

    /// <summary>
    /// The handle landed on a reparse point or a directory rather than on a file.
    /// </summary>
    NotAPlainFile,

    /// <summary>
    /// The file system would not give the times, or gave a change time that is not
    /// a point in time.
    /// </summary>
    TimesUnavailable,

    /// <summary>
    /// The volume holding the file could not be named by a volume GUID path, or would
    /// not say which file system it carries.
    /// </summary>
    VolumeUnestablished,

    /// <summary>
    /// The volume answered, and it is not a local, fixed volume.
    /// </summary>
    NotAFixedVolume,

    /// <summary>
    /// The volume answered, and its file system is not NTFS.
    /// </summary>
    NotNtfs,

    /// <summary>
    /// The attempt threw. Distinct from every member above, each of which is the
    /// system answering rather than the call failing to complete.
    /// </summary>
    Faulted,
}

/// <summary>
/// The three times of a file that <see cref="CachedFileAge"/> reads, in UTC.
///
/// LAST ACCESS IS NOT AMONG THEM. On a volume that updates it, any read of the file
/// moves it, this app's own scan included, so it says nothing about when the file
/// was made.
/// </summary>
public readonly record struct FileTimes(
    DateTime CreationUtc,
    DateTime LastWriteUtc,
    DateTime ChangeUtc);

/// <summary>
/// Reads a file's times, and establishes that they are kept by NTFS on a local,
/// fixed volume, for <see cref="CachedFileAge"/> to judge the file's age by.
///
/// THE FILE SYSTEM IS PART OF THE ANSWER AND NOT A SEPARATE QUESTION. NTFS keeps a
/// change time of its own, set when the file's contents or attributes change; FAT32
/// and exFAT keep none. A time read off any volume but a local NTFS one is not taken
/// as an answer, and the reader says so rather than handing it back.
/// </summary>
public interface IFileTimesReader
{
    /// <summary>
    /// The times of the file <paramref name="path"/> names, with the answer named
    /// rather than collapsed. <see cref="FileTimesRead.Read"/> is the only outcome
    /// that fills <paramref name="times"/>.
    ///
    /// Links are NOT followed. A candidate is a file the walk found directly in the
    /// folder and the containment guard has already refused a reparse point, so a
    /// link here is one that appeared since, and it is reported as
    /// <see cref="FileTimesRead.NotAPlainFile"/> rather than read through.
    /// </summary>
    FileTimesRead ReadOutcome(string path, out FileTimes times);
}

/// <summary>
/// Whether a cached file the folder walk found was last created, written and changed
/// before the scan's clock, which it must have been before the scan offers it.
///
/// WHAT IT IS FOR. Windows Installer puts a new copy of a package into the folder
/// before any of its records names that copy, and while no record names it the copy
/// looks exactly like a spare. The scan reads Windows Installer's in-progress signals
/// just after its clock and keeps every file the walk found where any is set. Where
/// none is, a session that was running at the clock has ended, and its records were
/// written as part of it, so a copy it made and kept is named by the time the scan
/// reads the records. This check takes care of the files no such reading can speak
/// for: one created, written or changed at or after the clock, by a session that
/// started after the signals were read, is kept.
///
/// THE TIME IS THE LATEST OF THREE, because a copy can take some of its times from
/// its source. Given its source's creation and last-write times, a copy has a change
/// time from when they were set; given its source's last-write and change times, it
/// has a creation time from when it was made. No one of the three is the copy's own in
/// both cases, and the latest of them is.
///
/// THE CLOCK IS READ IN THE STEPS FILE TIMES ARE STAMPED IN (<see cref="FileStampClock"/>).
/// Read from the precise system time instead, it could stand up to a tick past a time
/// stamped after it, and a copy written in that moment would read as before it.
///
/// EVERYTHING BUT A POSITIVE ANSWER KEEPS THE FILE, written as the one case that
/// lets it through: a time that could not be read, a volume that is not local NTFS
/// and a time that is not before the clock.
/// </summary>
public static class CachedFileAge
{
    /// <summary>
    /// How far after the clock a file's latest time can be and still be taken as a time
    /// at all. Within it the file is <see cref="CachedFileAgeVerdict.NotBeforeTheClock"/>,
    /// and a copy written a moment before the clock was set back reads as a little way
    /// ahead of it. Beyond it the time is not one the scan can place, and the file is
    /// <see cref="CachedFileAgeVerdict.Unestablished"/>. Both keep the file; this decides
    /// only which count it is in.
    /// </summary>
    public static readonly TimeSpan AheadLimit = TimeSpan.FromDays(1);

    /// <summary>
    /// How far the wall clock may run slow against the monotonic one, as a share of the
    /// time that passed, before <see cref="ClockWentBack(TimeProvider, WallClockReading)"/>
    /// answers true: one part in a thousand, up to <see cref="FallBehindLimit"/>, which it
    /// reaches in a pass of a hundred seconds. In a shorter pass it absorbs the rate at which a
    /// system clock left to itself drifts from the monotonic one, awake or across a sleep, and
    /// the widths of two readings no wider than <see cref="NarrowReading"/>. Windows Time
    /// slewing a clock that is ahead runs it slow, on some settings by more than this, and the
    /// check then answers true and the files are held: the wall clock lost that time. A larger
    /// share lets a larger step back through in such a pass; a smaller one holds files where
    /// the wall clock has only drifted.
    /// </summary>
    public const double SlowRateAllowance = 0.001;

    /// <summary>
    /// The most the wall clock may stand behind the monotonic one at the second reading,
    /// measured from the first, however long the pass ran, before
    /// <see cref="ClockWentBack(TimeProvider, WallClockReading)"/> answers true: a tenth of a
    /// second. <see cref="SlowRateAllowance"/> grows with the pass and this stops it there, so
    /// in a long pass a clock stepped back by more than a tenth of a second, and still that far
    /// back at the second reading, answers true as it does in a short one. Windows Time steps
    /// the clock only where it is out by more than its MaxAllowedPhaseOffset, by default one
    /// second outside a domain and five minutes within one, and slews it otherwise; the
    /// reference NTP daemon steps it, by default, where it is out by more than 0.128 seconds.
    /// A pass long enough to fall behind by this much through drift alone holds its files, as
    /// a clock slewed slow does. A larger limit lets a larger step back through in a long
    /// pass; a smaller one holds files in long passes where the clock only drifted.
    /// </summary>
    public static readonly TimeSpan FallBehindLimit = TimeSpan.FromMilliseconds(100);

    /// <summary>
    /// How long a wall-clock reading may take, from the monotonic timestamp before it to the
    /// one after it, and be kept at the first try: 20 microseconds. A reading that takes
    /// longer is taken again (<see cref="ReadWallClock"/>). Two readings this wide come within
    /// <see cref="SlowRateAllowance"/> of any pass longer than forty milliseconds, which is
    /// twice this limit divided by the allowance.
    /// </summary>
    public static readonly TimeSpan NarrowReading = TimeSpan.FromTicks(200);

    /// <summary>
    /// How many times <see cref="ReadWallClock"/> takes a reading before it keeps the
    /// narrowest of them: sixteen.
    /// </summary>
    public const int WallClockTries = 16;

    /// <summary>
    /// The precise wall clock read between two monotonic timestamps, the reading the scan
    /// and the check made just before a Move or Delete take before their own clock and
    /// again once they have read the last file's times
    /// (<see cref="ClockWentBack(TimeProvider, WallClockReading)"/>).
    ///
    /// THE PRECISE TIME AND NOT THE ONE FILES ARE JUDGED AGAINST. <see cref="FileStampClock"/>
    /// answers with the time as it stood at the last tick of the system clock, which stands
    /// behind the time that passed by however long ago that tick was, at each reading. The
    /// precise time moves between ticks with the monotonic clock, so the two keep pace
    /// wherever the system clock is not stepped or slewed. A clock that is not a
    /// <see cref="IPreciseWallClock"/> answers with its own time; both hosts give the scan
    /// and the check before a Move or Delete <see cref="FileStampClock"/>.
    ///
    /// UP TO <see cref="WallClockTries"/> READINGS, keeping the first no wider than
    /// <see cref="NarrowReading"/>, or else the narrowest. Every gap between the wall clock
    /// and the timestamps either side of it widens the time
    /// <see cref="ClockWentBack(TimeProvider, WallClockReading, WallClockReading)"/> measures
    /// against, which moves it only towards answering true.
    /// </summary>
    public static WallClockReading ReadWallClock(TimeProvider clock)
    {
        WallClockReading narrowest = default;
        for (var tries = 1; tries <= WallClockTries; tries++)
        {
            var before = clock.GetTimestamp();
            var wall = clock is IPreciseWallClock precise ? precise.GetPreciseUtcNow() : clock.GetUtcNow();
            var after = clock.GetTimestamp();
            var reading = new WallClockReading(before, wall, after, tries);

            narrowest = tries == 1 || reading.Width(clock) < narrowest.Width(clock)
                ? reading
                : narrowest with { Tries = tries };
            if (narrowest.Width(clock) <= NarrowReading) break;
        }

        return narrowest;
    }

    /// <summary>
    /// Whether the wall clock has fallen behind the monotonic one since
    /// <paramref name="atStart"/>, read again now (<see cref="ReadWallClock"/>).
    ///
    /// WHAT IT IS FOR. While the system clock runs forward, a file written after the scan's
    /// clock is read carries a time at or after it. Where the clock is set back after it is
    /// read, a file written in the moments that follow is stamped before it, and
    /// <see cref="Judge"/> reads it as before the clock. So the scan and the check made just
    /// before a Move or Delete take <paramref name="atStart"/> before their clock, ask this
    /// once they have read the last file's times, and hold every file that passed where it
    /// answers true.
    /// </summary>
    public static bool ClockWentBack(TimeProvider clock, WallClockReading atStart) =>
        ClockWentBack(clock, atStart, ReadWallClock(clock));

    /// <summary>
    /// Whether the wall clock advanced less between <paramref name="atStart"/> and
    /// <paramref name="atEnd"/> than the most time that can have passed between the two
    /// readings, by more than <see cref="SlowRateAllowance"/> of that time or
    /// <see cref="FallBehindLimit"/>, whichever is less.
    ///
    /// THE MOST TIME, TAKEN OUTSIDE BOTH READINGS: from the timestamp before the first to
    /// the timestamp after the second. A step back larger than the allowance therefore
    /// answers true however long either reading took.
    ///
    /// A WALL CLOCK THAT RAN AHEAD ANSWERS FALSE: a time stamped after a step forward is
    /// later still.
    /// </summary>
    public static bool ClockWentBack(TimeProvider clock, WallClockReading atStart, WallClockReading atEnd)
    {
        var elapsed = clock.GetElapsedTime(atStart.TimestampBefore, atEnd.TimestampAfter);
        var wallAdvance = atEnd.Wall - atStart.Wall;
        var allowed = Durations.Times(elapsed, SlowRateAllowance);
        if (allowed > FallBehindLimit) allowed = FallBehindLimit;
        return elapsed - wallAdvance > allowed;
    }

    /// <summary>
    /// The latest of the three times, which is the one the check reads.
    /// </summary>
    public static DateTime Latest(FileTimes times)
    {
        var latest = times.CreationUtc;
        if (times.LastWriteUtc > latest) latest = times.LastWriteUtc;
        if (times.ChangeUtc > latest) latest = times.ChangeUtc;
        return latest;
    }

    /// <summary>
    /// True only where <see cref="Judge"/> answers <see cref="CachedFileAgeVerdict.BeforeTheClock"/>.
    /// </summary>
    public static bool IsBeforeTheClock(FileTimesRead outcome, FileTimes times, DateTimeOffset scanClock) =>
        Judge(outcome, times, scanClock) == CachedFileAgeVerdict.BeforeTheClock;

    /// <summary>
    /// What the file's times establish against <paramref name="scanClock"/>.
    ///
    /// <see cref="CachedFileAgeVerdict.BeforeTheClock"/> where the reader answered
    /// <see cref="FileTimesRead.Read"/> and the latest of the three times is before the
    /// clock.
    /// <see cref="CachedFileAgeVerdict.NotBeforeTheClock"/> where it is at the clock, or
    /// after it by no more than <see cref="AheadLimit"/>.
    /// <see cref="CachedFileAgeVerdict.Unestablished"/> for every other answer from the
    /// reader, and for a latest time more than <see cref="AheadLimit"/> after the clock.
    /// </summary>
    public static CachedFileAgeVerdict Judge(FileTimesRead outcome, FileTimes times, DateTimeOffset scanClock)
    {
        if (outcome != FileTimesRead.Read) return CachedFileAgeVerdict.Unestablished;

        var beforeTheClock = scanClock.UtcDateTime - Latest(times);
        if (beforeTheClock > TimeSpan.Zero) return CachedFileAgeVerdict.BeforeTheClock;
        return -beforeTheClock > AheadLimit
            ? CachedFileAgeVerdict.Unestablished
            : CachedFileAgeVerdict.NotBeforeTheClock;
    }
}

/// <summary>
/// A wall-clock reading taken by <see cref="CachedFileAge.ReadWallClock"/>: the monotonic
/// timestamp just before it, the wall clock, the monotonic timestamp just after it, and how
/// many readings were taken to get it.
/// </summary>
public readonly record struct WallClockReading(long TimestampBefore, DateTimeOffset Wall, long TimestampAfter, int Tries)
{
    /// <summary>How long the reading took, on <paramref name="clock"/>'s monotonic clock.</summary>
    public TimeSpan Width(TimeProvider clock) => clock.GetElapsedTime(TimestampBefore, TimestampAfter);
}

/// <summary>
/// A clock that gives the precise system time apart from the time it answers with, for
/// <see cref="CachedFileAge.ReadWallClock"/>.
/// </summary>
internal interface IPreciseWallClock
{
    /// <summary>The system time, moving between ticks of the system clock.</summary>
    DateTimeOffset GetPreciseUtcNow();
}

/// <summary>
/// What <see cref="CachedFileAge.Judge"/> established about one file's times. Only
/// <see cref="BeforeTheClock"/> lets the file through.
///
/// THE TWO THAT KEEP IT ARE COUNTED APART. The opt-in report carries the two counts
/// separately, and the command line's held-back Application log entry counts a file
/// whose times were not established and leaves out a file not before the clock, which
/// has a line of its own. The count that keeps the window's all-clear off
/// (<see cref="Models.ScanResult.UnsettledHeldBackCount"/>) takes in both.
/// </summary>
public enum CachedFileAgeVerdict
{
    /// <summary>
    /// The times were not established. The zero, so a verdict nobody set keeps the file
    /// and is spoken of.
    /// </summary>
    Unestablished,

    /// <summary>The latest of the file's times is before the scan's clock.</summary>
    BeforeTheClock,

    /// <summary>
    /// The latest of the file's times is at the scan's clock, or after it by no more than
    /// <see cref="CachedFileAge.AheadLimit"/>.
    /// </summary>
    NotBeforeTheClock,
}
