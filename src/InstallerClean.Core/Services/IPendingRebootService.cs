namespace InstallerClean.Services;

/// <summary>
/// Detects whether Windows has queued a reboot. Used to gate the
/// Move and Delete pills with a warning banner: cleaning up the
/// installer cache while updates are mid-staging can break the
/// pending repair / rollback sequence.
/// </summary>
public interface IPendingRebootService
{
    /// <summary>
    /// True if any of the four well-known reboot-pending registry
    /// keys is present (Component Based Servicing, Auto Update,
    /// PendingFileRenameOperations, PostRebootReporting). Reads only;
    /// no registry mutation.
    /// </summary>
    bool HasPendingReboot();
}
