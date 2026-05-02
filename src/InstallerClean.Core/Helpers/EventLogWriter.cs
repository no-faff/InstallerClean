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

    internal enum Level
    {
        Information,
        Warning,
    }

    /// <summary>
    /// Writes the summary entry. Never throws; a failed write (source
    /// creation denied, event log service stopped, non-Windows host,
    /// source mapped to a non-Application log) is swallowed because the
    /// primary output channel is stdout, not the event log.
    /// </summary>
    internal static void Write(Level level, string summary)
    {
        try
        {
            if (!EnsureSourceMappedToApplicationLog())
                return;
            var entryType = level == Level.Warning
                ? EventLogEntryType.Warning
                : EventLogEntryType.Information;
            EventLog.WriteEntry(SourceName, summary, entryType);
        }
        catch
        {
            // Stdout is the primary channel; silent failure here keeps the
            // CLI working on hosts where the event log isn't writable.
        }
    }

    /// <summary>
    /// Ensures the InstallerClean event source exists and is registered
    /// against the Application log. Returns false if the source is
    /// pre-registered against a different log (e.g. an older install
    /// pointed it at System): writing summaries containing user-typed
    /// paths into an attacker-readable log is an information-disclosure
    /// path even on otherwise correct DACLs, so the writer skips
    /// silently rather than mis-routing the entry.
    /// </summary>
    private static bool EnsureSourceMappedToApplicationLog()
    {
        // First-run registration requires admin (our manifest guarantees it);
        // subsequent runs short-circuit via SourceExists.
        if (!EventLog.SourceExists(SourceName))
        {
            EventLog.CreateEventSource(SourceName, "Application");
            return true;
        }
        var existingLog = EventLog.LogNameFromSourceName(SourceName, ".");
        return string.Equals(existingLog, "Application", StringComparison.OrdinalIgnoreCase);
    }
}
