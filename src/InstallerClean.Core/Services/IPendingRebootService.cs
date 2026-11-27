namespace InstallerClean.Services;

/// <summary>Detects whether the MSI cache is at risk from a Windows Installer operation in flight or queued for next reboot.</summary>
public interface IPendingRebootService
{
    /// <summary>
    /// Probes the four signals and returns a result. Reads only.
    ///
    /// IT THROWS IN ONE CASE AND NO OTHER: a read of Windows Installer's in-progress
    /// file that failed in a way none of <see cref="InstallerInProgressMarkerReading"/>'s
    /// members names. Nothing is established by such a read and none of this gate's
    /// sentences is true of it, so it reaches the caller's own error path instead.
    ///
    /// A REGISTRY READ THAT COULD NOT BE MADE IS NOT A READ THAT CAME BACK CLEAR.
    /// Where either registry read does not answer, whether the cache is at risk is
    /// not established, and this returns a Block naming that rather than the verdict
    /// a quiet machine would have produced.
    ///
    /// The mutex answers on its own terms, stated on <see cref="IMutexProbe.Sample"/>:
    /// a held mutex blocks as <see cref="PendingRebootReason.MsiExecuteMutexHeld"/>, an
    /// existing mutex whose security refuses the probe blocks as
    /// <see cref="PendingRebootReason.MsiExecuteMutexAccessRefused"/>, and a probe that
    /// fails any other way blocks as
    /// <see cref="PendingRebootReason.MsiExecuteMutexUnreadable"/> unless another signal
    /// blocks with a reason of its own. Only a mutex that is not there, unowned or
    /// abandoned counts as not held.
    /// </summary>
    PendingRebootResult Check();

    /// <summary>
    /// Whether Windows Installer is busy at this moment: the mutex, the InProgress key
    /// and the in-progress file, read as <see cref="Check"/> reads them and in its order,
    /// and then the Windows Installer service's own state, which blocks unless the
    /// service is stopped or is not there. The queued file operations
    /// <see cref="Check"/> reads last are not read. It throws in the one case
    /// <see cref="Check"/> does.
    ///
    /// THE SERVICE IS READ WITH THE MUTEX AND NEVER IN ITS PLACE. An installation's
    /// client takes the mutex a few milliseconds before the service leaves its stopped
    /// state, so for those milliseconds only the mutex says that one has begun; and
    /// between the passes of an installation that makes more than one, the mutex is free
    /// and only the service says that one is under way.
    ///
    /// <see cref="Check"/> DOES NOT READ THE SERVICE, and must not come to: the scan and
    /// the check before a Move or Delete hold every file no record names while
    /// <see cref="Check"/> is blocked, and the service runs on for minutes after every
    /// installation, so a scan made in those minutes lists its files as any other does.
    /// </summary>
    PendingRebootResult CheckBusy();
}
