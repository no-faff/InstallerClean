using System.Diagnostics;

namespace InstallerClean.Helpers;

/// <summary>
/// Writes a single summary entry to the Windows Application event log per
/// CLI run, so sysadmins running InstallerClean under Task Scheduler can
/// audit what happened without trawling stdout redirects.
/// </summary>
/// <remarks>
/// Some summary lines include the user's typed destination path. The
/// Application log is readable by every authenticated user, so under a
/// multi-user threat model this leaks the path. Accepted: the path is
/// the calling user's own input (not a cross-profile reference) and
/// Task Scheduler audit needs the path for diagnosis.
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
    /// Writes the summary entry, classified by <paramref name="outcome"/> so
    /// the entry carries a stable Event ID and entry type (see
    /// <see cref="CliContract.EventIdFor"/> / <see cref="CliContract.EntryTypeFor"/>).
    /// Never throws; a failed write (source creation denied, event log
    /// service stopped, non-Windows host, source mapped to a non-Application
    /// log) is swallowed because the primary output channel is stdout, not
    /// the event log.
    /// </summary>
    internal static void Write(CliEventClass outcome, string summary)
    {
        try
        {
            if (!EnsureSourceMappedToApplicationLog())
            {
                EventLogUnavailable = true;
                return;
            }
            EventLog.WriteEntry(SourceName, summary,
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
