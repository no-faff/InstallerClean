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
            // Best effort. Silent failure keeps the CLI robust in environments
            // where the event log isn't writable (restricted containers,
            // deprecated OS builds, policy-locked machines).
        }
    }

    private static void EnsureSource()
    {
        // Source registration is per-machine and requires admin on first
        // run. InstallerClean's manifest always elevates, so this normally
        // succeeds. On subsequent runs SourceExists returns true and we
        // skip the registration call.
        if (!EventLog.SourceExists(SourceName))
            EventLog.CreateEventSource(SourceName, "Application");
    }
}
