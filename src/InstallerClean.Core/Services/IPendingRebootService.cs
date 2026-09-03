namespace InstallerClean.Services;

/// <summary>Detects whether the MSI cache is at risk from a Windows Installer operation in flight or queued for next reboot.</summary>
public interface IPendingRebootService
{
    /// <summary>
    /// Probes the three signals and returns a result. Reads only. Never throws.
    ///
    /// A PROBE THAT COULD NOT BE MADE IS NOT A PROBE THAT CAME BACK CLEAR. Where a
    /// read does not answer, whether the cache is at risk is not established, and
    /// this returns a Block naming that rather than the verdict a quiet machine
    /// would have produced.
    /// </summary>
    PendingRebootResult Check();
}
