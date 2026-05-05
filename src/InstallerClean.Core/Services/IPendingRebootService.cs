namespace InstallerClean.Services;

/// <summary>Detects whether the MSI cache is at risk from a Windows Installer operation in flight or queued for next reboot.</summary>
public interface IPendingRebootService
{
    /// <summary>Probes the three signals and returns a result. Reads only. Never throws; failed reads are treated as no signal.</summary>
    PendingRebootResult Check();
}
