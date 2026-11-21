namespace InstallerClean.Services;

/// <summary>Result of a pending-reboot check: a verdict, and when blocking, the specific reason.</summary>
public sealed record PendingRebootResult
{
    public PendingRebootVerdict Verdict { get; }
    public PendingRebootReason? Reason { get; }
    public string? Detail { get; }

    /// <summary>
    /// The Windows error number the check failed on, where it failed on one: set only on
    /// a <see cref="PendingRebootReason.MsiExecuteMutexUnreadable"/> block whose failure
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
    /// The action services' acquire asks for the same rights the gate's probe does, so
    /// they meet the same refusal, and the command line reports a refusal met there
    /// under this reason too: one condition, one sentence and one exit code, wherever it
    /// is met.
    /// </summary>
    MsiExecuteMutexAccessRefused,

    /// <summary>HKLM\SOFTWARE\Microsoft\Windows\CurrentVersion\Installer\InProgress exists: a previous Windows Installer transaction is suspended. Source: MS Learn, Msizap Remarks.</summary>
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
    /// registry value. The action services' acquire meets the same kind of failure as
    /// <see cref="MutexAcquireOutcome.NotAcquired"/> and refuses the batch on it.
    /// </summary>
    MsiExecuteMutexUnreadable,
}
