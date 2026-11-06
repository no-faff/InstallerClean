using System.Diagnostics;

namespace InstallerClean.Helpers;

/// <summary>
/// Writes Application-channel entries for a CLI run, so sysadmins running
/// InstallerClean under Task Scheduler can audit what happened without trawling
/// stdout redirects. A run writes at most one summary entry: /? and --version write
/// none, an argless or malformed invocation writes one, and a scan, delete or move
/// run writes one for its outcome. Beside that last kind a run may also write
/// notices the scan's own findings call for. Every one of them comes through here.
/// </summary>
/// <remarks>
/// The entries can disclose two kinds of path to the Application log, which is
/// readable by every authenticated user on the machine. Some summary lines carry
/// the user's own typed destination path. The hard-error entry additionally
/// carries the crash-log path, which is derived from <c>%LOCALAPPDATA%</c> and so
/// embeds the elevating account's profile name, not user-typed input. Both are an
/// accepted trade: on an unattended Task Scheduler run stdout is discarded, so
/// this entry is the only audit trail back to what happened and to the crash log
/// that holds the detail, and a Windows account name is a weak secret on a machine
/// the reader can already run code on. The entry is machine-read, so dropping the
/// path later is a safer direction to move in than adding it back.
/// </remarks>
internal static class EventLogWriter
{
    /// <summary>
    /// The event source every entry is written under, and the name the window's start
    /// check reads the command line's entries back by (<c>CommandLineRunRecord</c>).
    /// </summary>
    internal const string SourceName = "InstallerClean";

    private const string ApplicationLog = "Application";

    /// <summary>
    /// Sticky flag: set true on the first Write that fails (source
    /// creation denied by Group Policy, event-log service stopped,
    /// source pre-mapped to a non-Application log). The command line
    /// prints a one-line stdout warning, once in a process, when this is
    /// set, so an RMM consumer expecting Application-channel entries can tell
    /// "the channel was unwritable" apart from "nothing happened".
    /// </summary>
    internal static bool EventLogUnavailable
    {
        get => Volatile.Read(ref _eventLogUnavailable) != 0;
        set => Volatile.Write(ref _eventLogUnavailable, value ? 1 : 0);
    }

    private static int _eventLogUnavailable;

    /// <summary>
    /// When this process registered the event source, as a <see cref="Stopwatch"/>
    /// timestamp, or 0 where it has not.
    /// </summary>
    private static long _registeredAt;

    /// <summary>
    /// How much of <see cref="RegistrationSettleBound"/> is left since this process
    /// registered the source, or zero where it did not or the bound has passed.
    /// </summary>
    private static TimeSpan SettleTimeLeft()
    {
        var registeredAt = Interlocked.Read(ref _registeredAt);
        if (registeredAt == 0) return TimeSpan.Zero;
        var left = RegistrationSettleBound - Durations.Since(registeredAt);
        return left > TimeSpan.Zero ? left : TimeSpan.Zero;
    }

    /// <summary>
    /// How long a write is tried again after this process registered the source, and how
    /// often. Windows can refuse a write under a source registered moments before, with
    /// InvalidOperationException, until its own list of sources has caught up.
    /// </summary>
    internal static readonly TimeSpan RegistrationSettleBound = TimeSpan.FromSeconds(5);
    internal static readonly TimeSpan RegistrationSettleInterval = TimeSpan.FromMilliseconds(250);

    /// <summary>
    /// Runs <paramref name="write"/>. A write that throws InvalidOperationException is
    /// tried again every <paramref name="interval"/> while <paramref name="settleTimeLeft"/>
    /// lasts, the time left of the settle bound since this process registered the source,
    /// and the last throw goes to the caller. With no time left, a source registered
    /// before this process among them, and for any other throw, ObjectDisposedException
    /// included, the throw goes to the caller at once. <paramref name="sleep"/> waits
    /// between tries; a test passes one that does not.
    /// </summary>
    internal static void WriteRetryingAfterRegistration(
        Action write, TimeSpan settleTimeLeft, TimeSpan interval, Action<TimeSpan> sleep)
    {
        var waited = TimeSpan.Zero;
        while (true)
        {
            try
            {
                write();
                return;
            }
            catch (InvalidOperationException ex) when (ex is not ObjectDisposedException && waited < settleTimeLeft)
            {
                sleep(interval);
                waited += interval;
            }
        }
    }

    /// <summary>
    /// What the most recent failed <see cref="Write"/> met, set with
    /// <see cref="EventLogUnavailable"/>: the step that refused, and where it threw, the
    /// exception's type, HResult and message. Null until a write fails. The first failed
    /// write in a process writes it to crash.log, and the Application-log integration test
    /// prints it when its write fails.
    /// </summary>
    internal static string? LastFailure { get; set; }

    /// <summary>
    /// Null in both hosts. When set, <see cref="Write"/> hands it the class and the
    /// built entry in place of the source check and the write to the Application
    /// channel, and does everything else as it always does: the entry is built inside
    /// the same guard, and a sink that throws marks the log unavailable as a refused
    /// write would. The test assembly sets it when it loads, so the entries its tests
    /// write through here are recorded rather than written.
    /// </summary>
    /// <remarks>
    /// The call stays above <see cref="EnsureSourceMappedToApplicationLog"/>. Below it,
    /// a run with a sink set would still register the event source, which is a write
    /// to the machine's registry, on any host that never had it.
    /// </remarks>
    internal static Action<CliEventClass, string>? Sink { get; set; }

    /// <summary>
    /// Writes one entry, summary or notice, classified by <paramref name="outcome"/>
    /// so it carries a stable Event ID and entry type (see
    /// <see cref="CliContract.EventIdFor"/> / <see cref="CliContract.EntryTypeFor"/>).
    /// Never throws; a failed write (source creation denied, event log
    /// service stopped, non-Windows host, source mapped to a non-Application
    /// log) is swallowed because the primary output channel is stdout, not
    /// the event log. What refused is recorded in <see cref="LastFailure"/>, and the first
    /// failed write in a process writes it to crash.log as well.
    /// </summary>
    /// <param name="buildEntry">
    /// Builds the entry text, and it is a callback rather than a built string so
    /// the build runs inside the try below. C# evaluates an argument in the
    /// caller's frame, so taking the text would leave the one part of the write
    /// that formats a resx template outside the guard the rest of it has.
    /// </param>
    internal static void Write(CliEventClass outcome, Func<string> buildEntry)
    {
        var step = "building the entry";
        try
        {
            var entry = buildEntry();
            if (Sink is { } sink)
            {
                step = "handing the entry to the sink";
                sink(outcome, entry);
                return;
            }
            if (!EnsureSourceMappedToApplicationLog(ref step, out var mappedLog))
            {
                var first = MarkUnavailable();
                Record(first, $"the source is registered against the log '{mappedLog}', not {ApplicationLog}");
                return;
            }
            step = "writing the entry";
            WriteRetryingAfterRegistration(
                () => EventLog.WriteEntry(SourceName, entry,
                    CliContract.EntryTypeFor(outcome), CliContract.EventIdFor(outcome)),
                SettleTimeLeft(), RegistrationSettleInterval, Thread.Sleep);
        }
        catch (Exception e)
        {
            var first = MarkUnavailable();
            // Stdout is the primary channel; silent failure here keeps the
            // CLI working on hosts where the event log isn't writable.
            string failure;
            try
            {
                failure = $"{step} threw {e.GetType().FullName} (HResult 0x{e.HResult:X8}): {e.Message}";
            }
            catch
            {
                // An exception whose message cannot be read leaves the step alone recorded.
                failure = step;
            }
            Record(first, failure);
        }
    }

    /// <summary>
    /// Sets <see cref="EventLogUnavailable"/> and answers whether this call set it, so
    /// two writes failing at once agree on which was the first.
    /// </summary>
    private static bool MarkUnavailable() => Interlocked.Exchange(ref _eventLogUnavailable, 1) == 0;

    /// <summary>
    /// Records <paramref name="failure"/> in <see cref="LastFailure"/>, once the caller has
    /// marked the log unavailable. Where <paramref name="first"/> says no write in this
    /// process had failed before, writes the failure to crash.log as one line, so a
    /// process whose entries never arrived leaves the reason on disk once rather than once
    /// per entry. Never throws: crash.log swallows its own failures.
    /// </summary>
    private static void Record(bool first, string failure)
    {
        LastFailure = failure;
        if (first)
            CrashLog.TryWrite(new InvalidOperationException(
                $"An entry was not written to the Application log: {failure}"));
    }

    /// <summary>
    /// Ensures the InstallerClean event source exists and is registered
    /// against the Application log. Returns false if the source is
    /// pre-registered against a different log (e.g. an older install
    /// pointed it at System). Refusing to write into a non-Application
    /// log keeps user-typed paths out of any log whose DACL is wider
    /// than Application's; the writer drops the entry rather than
    /// mis-routing it. <paramref name="step"/> names each call before it is made, so a
    /// caller catching a throw knows which call refused, and <paramref name="mappedLog"/>
    /// is the log the source is registered against.
    /// </summary>
    private static bool EnsureSourceMappedToApplicationLog(ref string step, out string mappedLog)
    {
        // First-run registration requires administrator rights. Every run that
        // scans has them (AdministratorRights). A run that stops before it scans
        // may not, the refusal to run without them among them, and there a lookup
        // or a registration Windows refuses throws to the outer catch, which marks
        // the log unavailable. Once registered, runs short-circuit via SourceExists.
        //
        // SourceExists then CreateEventSource is a check-then-act pair,
        // not atomic: a different process can register the source against
        // a different log between the two calls and CreateEventSource
        // throws ArgumentException. The outer Write try/catch swallows
        // the throw; the next run's SourceExists branch catches the
        // cross-log mapping via LogNameFromSourceName below and refuses
        // to write. The race is benign and recovers on the next call.
        step = "looking up the source";
        if (!EventLog.SourceExists(SourceName))
        {
            step = "registering the source";
            EventLog.CreateEventSource(SourceName, ApplicationLog);
            Interlocked.Exchange(ref _registeredAt, Math.Max(Stopwatch.GetTimestamp(), 1));
            mappedLog = ApplicationLog;
            return true;
        }
        step = "reading which log the source is registered against";
        mappedLog = EventLog.LogNameFromSourceName(SourceName, ".");
        return string.Equals(mappedLog, ApplicationLog, StringComparison.OrdinalIgnoreCase);
    }
}
