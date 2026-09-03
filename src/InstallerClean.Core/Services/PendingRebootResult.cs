namespace InstallerClean.Services;

/// <summary>Result of a pending-reboot check: a verdict, and when blocking, the specific reason.</summary>
public sealed record PendingRebootResult
{
    public PendingRebootVerdict Verdict { get; }
    public PendingRebootReason? Reason { get; }
    public string? Detail { get; }

    private PendingRebootResult(PendingRebootVerdict verdict, PendingRebootReason? reason, string? detail)
    {
        Verdict = verdict;
        Reason = reason;
        Detail = detail;
    }

    public static PendingRebootResult Clean { get; } =
        new(PendingRebootVerdict.Clean, null, null);

    /// <summary>Construct a Block verdict. Reason is required so the type cannot represent Block-with-null-Reason.</summary>
    public static PendingRebootResult Block(PendingRebootReason reason, string? detail = null) =>
        new(PendingRebootVerdict.Block, reason, detail);

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

    /// <summary>HKLM\SOFTWARE\Microsoft\Windows\CurrentVersion\Installer\InProgress exists: a previous Windows Installer transaction is suspended. Source: MS Learn, Msizap Remarks.</summary>
    InstallerInProgress,

    /// <summary>An entry in HKLM\SYSTEM\CurrentControlSet\Control\Session Manager\PendingFileRenameOperations targets a path under %SystemRoot%\Installer. Source: MS Learn, MoveFileExA.</summary>
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
}
