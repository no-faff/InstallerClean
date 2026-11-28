namespace InstallerClean.Services;

/// <summary>Result of a pending-reboot check: a verdict, and when blocking, the specific reason.</summary>
public sealed record PendingRebootResult
{
    public PendingRebootVerdict Verdict { get; }
    public PendingRebootReason? Reason { get; }
    public string? Detail { get; }

    /// <summary>
    /// The Windows error number the check failed on, where it failed on one: set only on
    /// a <see cref="PendingRebootReason.MsiExecuteMutexUnreadable"/> or
    /// <see cref="PendingRebootReason.InstallerServiceUnreadable"/> block whose failure
    /// Windows raised, and null on every other result. The sentences for that reason give
    /// it with the text Windows gives for it, and say nothing about a cause where it is null.
    /// </summary>
    public int? WindowsError { get; }

    private PendingRebootResult(
        PendingRebootVerdict verdict, PendingRebootReason? reason, string? detail, int? windowsError)
    {
        Verdict = verdict;
        Reason = reason;
        Detail = detail;
        WindowsError = windowsError;
    }

    public static PendingRebootResult Clean { get; } =
        new(PendingRebootVerdict.Clean, null, null, null);

    /// <summary>Construct a Block verdict. Reason is required so the type cannot represent Block-with-null-Reason.</summary>
    public static PendingRebootResult Block(
        PendingRebootReason reason, string? detail = null, int? windowsError = null) =>
        new(PendingRebootVerdict.Block, reason, detail, windowsError);

    /// <summary>True when the verdict is Block.</summary>
    public bool IsBlocked => Verdict == PendingRebootVerdict.Block;
}

public enum PendingRebootVerdict
{
    Clean,
    Block,
}

/// <summary>Specific reason for a Block verdict.</summary>
public enum PendingRebootReason
{
    /// <summary>Global\_MSIExecute mutex is held: a Windows Installer transaction is currently running. Source: MS Learn, _MSIExecute Mutex.</summary>
    MsiExecuteMutexHeld,

    /// <summary>
    /// Global\_MSIExecute exists and its security refuses this process the rights to
    /// open it, so whether a Windows Installer transaction holds it was never sampled.
    ///
    /// IT IS A SEPARATE VERDICT FROM <see cref="MsiExecuteMutexHeld"/> BECAUSE ONE
    /// MESSAGE CANNOT BE TRUE OF BOTH. That one says something is using Windows
    /// Installer right now; this one says only that the app was not allowed to look,
    /// and nothing has been seen holding the lock.
    ///
    /// A Move or Delete reads the mutex again at each file, through the same probe, so
    /// it meets the same refusal under this reason: one condition and one exit code,
    /// wherever it is met.
    /// </summary>
    MsiExecuteMutexAccessRefused,

    /// <summary>
    /// HKLM\SOFTWARE\Microsoft\Windows\CurrentVersion\Installer\InProgress exists: an
    /// earlier Windows Installer installation is active and incomplete, and Windows Installer
    /// deletes the key when that installation completes. Source: MS Learn, Msizap Remarks.
    /// </summary>
    InstallerInProgress,

    /// <summary>An entry in HKLM\SYSTEM\CurrentControlSet\Control\Session Manager\PendingFileRenameOperations targets a path under %SystemRoot%\Installer. The value and its pair format are documented at MS Learn, MoveFileExA.</summary>
    PendingRenameInCache,

    /// <summary>
    /// An entry in the same value names somewhere the app could not place, so whether
    /// it is under %SystemRoot%\Installer is not established either way.
    ///
    /// IT IS A SEPARATE VERDICT FROM <see cref="PendingRenameInCache"/> BECAUSE ONE
    /// MESSAGE CANNOT BE TRUE OF BOTH. That one says an operation is queued against the
    /// cache and names the path it found; this one says only that an operation is
    /// queued. A single verdict covering the two would put a cause in front of a user
    /// that is false of one of them.
    ///
    /// AND IT IS A SEPARATE THING AGAIN FROM <see cref="RegistryCheckUnreadable"/>, which
    /// is a read the gate could not make and leaves the app knowing nothing about whether
    /// anything is queued at all. This is the opposite: something IS queued, and its
    /// target is what could not be established.
    /// </summary>
    PendingRenameUnresolved,

    /// <summary>
    /// One of the registry reads the gate makes did not answer, so whether a Windows
    /// Installer transaction is suspended, or a file operation is queued against
    /// %SystemRoot%\Installer, is not established either way.
    ///
    /// IT COVERS BOTH READS AND NAMES NEITHER, because one sentence has to be true
    /// whichever of them it was. A message naming the InProgress key would be false of
    /// every run the PendingFileRenameOperations read produced, and the other way about.
    ///
    /// A value written in a type this does not read reaches it too. That is not an empty
    /// machine: something is recorded at the name Windows queues renames under and its
    /// contents cannot be seen, which is the same standing as an entry the placing pass
    /// cannot resolve and is refused for the same reason.
    /// </summary>
    RegistryCheckUnreadable,

    /// <summary>
    /// <c>inprogressinstallinfo.ipi</c> is in the Installer folder: Windows Installer's
    /// record of an installation it has not finished. See
    /// <see cref="IInstallerInProgressMarker"/> for what it covers that the mutex does
    /// not, which is a transaction of several packages between one package and the
    /// next.
    ///
    /// The gate checks it straight after the InProgress key.
    ///
    /// Its sentence is <c>Body.PendingReboot.Other</c> and
    /// <c>Cli.PendingRebootBlocked.Other</c>, which say that Windows Installer has
    /// something unfinished, and its Application-log label is its own name.
    /// </summary>
    InstallerInProgressMarker,

    /// <summary>
    /// Windows refused the read of <c>inprogressinstallinfo.ipi</c>, so whether the
    /// marker is there was not established.
    ///
    /// IT SHARES <see cref="MsiExecuteMutexAccessRefused"/>'S TWO SENTENCES, which say
    /// that Windows refused the app permission to check whether Windows Installer is
    /// busy and are as true of this read as of that one. It is a member of its own
    /// because that reason's Application-log label names the mutex. Its label is its
    /// own name, and a scheduler is told what that reason tells it.
    /// </summary>
    InstallerInProgressMarkerAccessRefused,

    /// <summary>
    /// The sample of Global\_MSIExecute failed in a way that is neither a refusal nor
    /// a finding, so whether a Windows Installer transaction holds it was not
    /// established. The gate gives it only where every other signal read clear, since
    /// any of them blocking names what is happening.
    ///
    /// IT IS A SEPARATE VERDICT FROM <see cref="MsiExecuteMutexAccessRefused"/>, whose
    /// sentences say Windows refused the app permission to look, which is false of
    /// this; and from <see cref="RegistryCheckUnreadable"/>, whose sentences name a
    /// registry value.
    /// </summary>
    MsiExecuteMutexUnreadable,

    /// <summary>
    /// The Windows Installer service is not stopped: starting, running or stopping. Every
    /// installation's engine runs in that service, so while it is stopped none is under
    /// way, and it stops by itself some minutes after the last one has finished. An
    /// installer window left open keeps it running for as long as the window is up.
    ///
    /// GIVEN BY <see cref="IPendingRebootService.CheckBusy"/> ALONE, and only where
    /// neither the mutex, the InProgress key nor the in-progress file blocked, so it
    /// names the minutes after an installation and not the installation itself. It is
    /// given ahead of a mutex that could not be sampled, as every signal that blocks is.
    /// </summary>
    InstallerServiceRunning,

    /// <summary>
    /// The Windows Installer service's state could not be read, so whether an
    /// installation's engine is running was not established. Carries the Windows error
    /// the read failed on (<see cref="PendingRebootResult.WindowsError"/>). Given by
    /// <see cref="IPendingRebootService.CheckBusy"/> alone.
    /// </summary>
    InstallerServiceUnreadable,
}

/// <summary>What a reason is, beyond which signal gave it.</summary>
public static class PendingRebootReasons
{
    /// <summary>
    /// Whether <paramref name="reason"/> is one an installation ending clears, so that
    /// waiting for it is worth while: the mutex held, the InProgress key or the
    /// in-progress file there, or the service running.
    ///
    /// TRUE DOES NOT PROMISE THAT A WAIT ENDS. An installation left unfinished until the
    /// next restart leaves the InProgress key or the in-progress file where it is, and
    /// an installer's window left open keeps the service running, so a caller that
    /// waits on this bounds its wait or lets the person stop it.
    ///
    /// EVERY OTHER REASON ANSWERS FALSE, a member added to the enum later among them: a
    /// file operation queued for the next restart, a signal that could not be read and a
    /// signal the app was refused permission to read do not change by being waited on.
    /// </summary>
    public static bool ClearsByWaiting(this PendingRebootReason reason) => reason is
        PendingRebootReason.MsiExecuteMutexHeld
        or PendingRebootReason.InstallerInProgress
        or PendingRebootReason.InstallerInProgressMarker
        or PendingRebootReason.InstallerServiceRunning;
}
