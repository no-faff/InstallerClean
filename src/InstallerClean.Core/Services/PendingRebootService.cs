namespace InstallerClean.Services;

/// <summary>
/// Returns Block when any of three signals indicates the MSI cache is currently at risk:
/// the _MSIExecute mutex, the Installer\InProgress key, or a PendingFileRenameOperations
/// entry whose source path is under %SystemRoot%\Installer.
/// </summary>
public sealed class PendingRebootService : IPendingRebootService
{
    /// <summary>The Windows Installer execute mutex. Global\_ namespace makes it visible across sessions.</summary>
    internal const string MsiExecuteMutexName = @"Global\_MSIExecute";

    /// <summary>Presence indicates an unresolved Windows Installer transaction (MS Learn, Msizap Remarks).</summary>
    internal const string InstallerInProgressKey =
        @"SOFTWARE\Microsoft\Windows\CurrentVersion\Installer\InProgress";

    internal const string SessionManagerKey =
        @"SYSTEM\CurrentControlSet\Control\Session Manager";

    internal const string PendingFileRenameOperationsValue = "PendingFileRenameOperations";

    private readonly IRegistryReader _registry;
    private readonly IMutexProbe _mutex;

    /// <summary>Override for %SystemRoot%; null in production.</summary>
    private readonly string? _windowsRootOverride;

    public PendingRebootService(IRegistryReader registry, IMutexProbe mutex)
        : this(registry, mutex, windowsRootOverride: null)
    {
    }

    /// <summary>Test-only constructor. Lets the path comparison run against an arbitrary %SystemRoot%.</summary>
    internal PendingRebootService(
        IRegistryReader registry,
        IMutexProbe mutex,
        string? windowsRootOverride)
    {
        _registry = registry;
        _mutex = mutex;
        _windowsRootOverride = windowsRootOverride;
    }

    public PendingRebootResult Check()
    {
        // Cheapest probe (no registry hit), so it goes first.
        bool mutexHeld;
        try
        {
            mutexHeld = _mutex.Exists(MsiExecuteMutexName);
        }
        catch
        {
            mutexHeld = false;
        }
        if (mutexHeld)
            return new PendingRebootResult(PendingRebootVerdict.Block, PendingRebootReason.MsiExecuteMutexHeld, null);

        // IRegistryReader documents "never throws"; the wrap defends against a buggy implementation
        // without changing the contract.
        bool installerInProgress;
        try
        {
            installerInProgress = _registry.LocalMachineKeyExists(InstallerInProgressKey);
        }
        catch
        {
            installerInProgress = false;
        }
        if (installerInProgress)
            return new PendingRebootResult(PendingRebootVerdict.Block, PendingRebootReason.InstallerInProgress, null);

        // Bare PendingFileRenameOperations is too broad (any third-party uninstaller writes
        // to it); refine to "source path inside %SystemRoot%\Installer".
        string[]? renames;
        try
        {
            renames = _registry.LocalMachineMultiStringValue(
                SessionManagerKey, PendingFileRenameOperationsValue);
        }
        catch
        {
            renames = null;
        }
        if (renames is not null)
        {
            var windowsRoot = _windowsRootOverride
                ?? Environment.GetFolderPath(Environment.SpecialFolder.Windows);
            var installerRoot = Path.Combine(windowsRoot, "Installer");

            foreach (var raw in renames)
            {
                if (string.IsNullOrEmpty(raw)) continue;
                var cleaned = StripNtPathPrefix(raw);
                if (cleaned.StartsWith(installerRoot, StringComparison.OrdinalIgnoreCase))
                {
                    return new PendingRebootResult(
                        PendingRebootVerdict.Block,
                        PendingRebootReason.PendingRenameInCache,
                        cleaned);
                }
            }
        }

        return PendingRebootResult.Clean;
    }

    /// <summary>Strips the NT object form (\??\) and long-path (\\?\) prefixes used by Session Manager.</summary>
    private static string StripNtPathPrefix(string s) =>
        s.StartsWith(@"\??\", StringComparison.Ordinal) ? s[4..] :
        s.StartsWith(@"\\?\", StringComparison.Ordinal) ? s[4..] :
        s;
}
