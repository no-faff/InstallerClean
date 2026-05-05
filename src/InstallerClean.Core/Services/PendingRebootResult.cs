namespace InstallerClean.Services;

/// <summary>Result of a pending-reboot check: a verdict, and when blocking, the specific reason.</summary>
public sealed record PendingRebootResult(
    PendingRebootVerdict Verdict,
    PendingRebootReason? Reason,
    string? Detail)
{
    public static PendingRebootResult Clean { get; } =
        new(PendingRebootVerdict.Clean, null, null);

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
}
