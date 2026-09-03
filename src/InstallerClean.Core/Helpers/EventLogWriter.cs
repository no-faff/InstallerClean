using System.Diagnostics;

namespace InstallerClean.Helpers;

/// <summary>
/// Writes Application-channel entries for a CLI run, so sysadmins running
/// InstallerClean under Task Scheduler can audit what happened without trawling
/// stdout redirects. A run that does work writes one summary entry, and beside it
/// any notices the scan's own findings call for; every one of them comes through
/// here.
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
    private const string SourceName = "InstallerClean";

    /// <summary>
    /// Sticky flag: set true on the first Write that fails (source
    /// creation denied by Group Policy, event-log service stopped,
    /// source pre-mapped to a non-Application log). The CLI Main
    /// surfaces a one-line stdout warning when this is set, so an
    /// RMM consumer expecting Application-channel entries can tell
    /// "the channel was unwritable" apart from "nothing happened".
    /// </summary>
    internal static bool EventLogUnavailable { get; private set; }

    /// <summary>
    /// Writes one entry, summary or notice, classified by <paramref name="outcome"/>
    /// so it carries a stable Event ID and entry type (see
    /// <see cref="CliContract.EventIdFor"/> / <see cref="CliContract.EntryTypeFor"/>).
    /// Never throws; a failed write (source creation denied, event log
    /// service stopped, non-Windows host, source mapped to a non-Application
    /// log) is swallowed because the primary output channel is stdout, not
    /// the event log.
    /// </summary>
    /// <param name="buildEntry">
    /// Builds the entry text, and it is a callback rather than a built string so
    /// the build runs inside the try below. C# evaluates an argument in the
    /// caller's frame, so taking the text would leave the one part of the write
    /// that formats a resx template outside the guard the rest of it has.
    /// </param>
    internal static void Write(CliEventClass outcome, Func<string> buildEntry)
    {
        try
        {
            var entry = buildEntry();
            if (!EnsureSourceMappedToApplicationLog())
            {
                EventLogUnavailable = true;
                return;
            }
            EventLog.WriteEntry(SourceName, entry,
                CliContract.EntryTypeFor(outcome), CliContract.EventIdFor(outcome));
        }
        catch
        {
            EventLogUnavailable = true;
            // Stdout is the primary channel; silent failure here keeps the
            // CLI working on hosts where the event log isn't writable.
        }
    }

    /// <summary>
    /// Ensures the InstallerClean event source exists and is registered
    /// against the Application log. Returns false if the source is
    /// pre-registered against a different log (e.g. an older install
    /// pointed it at System). Refusing to write into a non-Application
    /// log keeps user-typed paths out of any log whose DACL is wider
    /// than Application's; the writer drops the entry rather than
    /// mis-routing it.
    /// </summary>
    private static bool EnsureSourceMappedToApplicationLog()
    {
        // First-run registration requires admin; the app.manifest's
        // requireAdministrator guarantees this caller has it. Subsequent
        // runs short-circuit via SourceExists.
        //
        // SourceExists then CreateEventSource is a check-then-act pair,
        // not atomic: a different process can register the source against
        // a different log between the two calls and CreateEventSource
        // throws ArgumentException. The outer Write try/catch swallows
        // the throw; the next run's SourceExists branch catches the
        // cross-log mapping via LogNameFromSourceName below and refuses
        // to write. The race is benign and recovers on the next call.
        if (!EventLog.SourceExists(SourceName))
        {
            EventLog.CreateEventSource(SourceName, "Application");
            return true;
        }
        var existingLog = EventLog.LogNameFromSourceName(SourceName, ".");
        return string.Equals(existingLog, "Application", StringComparison.OrdinalIgnoreCase);
    }
}
