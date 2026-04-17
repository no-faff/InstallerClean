using System.Diagnostics;

namespace InstallerClean.Helpers;

/// <summary>
/// Writes a single summary entry to the Windows Application event log per
/// CLI run, so sysadmins running InstallerClean under Task Scheduler can
/// audit what happened without trawling stdout redirects.
/// </summary>
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
    /// creation denied, event log service stopped, non-Windows host) is
    /// swallowed because the primary output channel is stdout, not the
    /// event log.
    /// </summary>
    internal static void Write(Level level, string summary)
    {
        try
        {
            EnsureSource();
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

    private static void EnsureSource()
    {
        // First-run registration requires admin (our manifest guarantees it);
        // subsequent runs short-circuit via SourceExists.
        if (!EventLog.SourceExists(SourceName))
            EventLog.CreateEventSource(SourceName, "Application");
    }
}
