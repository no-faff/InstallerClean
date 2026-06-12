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
        // Mutex first because an active install is the most decisive signal; if it
        // fires, the InProgress and PendingFileRenameOperations probes are skipped.
        bool mutexHeld;
        try
        {
            mutexHeld = _mutex.IsHeld(MsiExecuteMutexName);
        }
        catch (Exception ex) when (ex is not OutOfMemoryException and not StackOverflowException)
        {
            // OOM and StackOverflow propagate, matching RegistryReader / MutexProbe:
            // swallowing them here would downgrade a real memory-pressure failure to
            // "no signal" and let the gate report Clean while an install is in flight.
            mutexHeld = false;
        }
        if (mutexHeld)
            return PendingRebootResult.Block(PendingRebootReason.MsiExecuteMutexHeld);

        // IRegistryReader documents "never throws", but the unit tests deliberately
        // substitute throwing fakes to exercise the fail-open path; this wrap keeps
        // Check's contract intact whether the bound implementation honours the
        // interface contract or not.
        bool installerInProgress;
        try
        {
            installerInProgress = _registry.LocalMachineKeyExists(InstallerInProgressKey);
        }
        catch (Exception ex) when (ex is not OutOfMemoryException and not StackOverflowException)
        {
            installerInProgress = false;
        }
        if (installerInProgress)
            return PendingRebootResult.Block(PendingRebootReason.InstallerInProgress);

        // Bare PendingFileRenameOperations is too broad (any third-party uninstaller writes
        // to it); refine to "source path inside %SystemRoot%\Installer".
        string[]? renames;
        try
        {
            renames = _registry.LocalMachineMultiStringValue(
                SessionManagerKey, PendingFileRenameOperationsValue);
        }
        catch (Exception ex) when (ex is not OutOfMemoryException and not StackOverflowException)
        {
            renames = null;
        }
        if (renames is not null)
        {
            var windowsRoot = _windowsRootOverride
                ?? Environment.GetFolderPath(Environment.SpecialFolder.Windows);
            var installerRoot = Path.Combine(windowsRoot, "Installer");
            var installerRootBoundary = installerRoot + Path.DirectorySeparatorChar;

            foreach (var raw in renames)
            {
                if (string.IsNullOrEmpty(raw)) continue;
                var cleaned = StripNtPathPrefix(raw);

                // Path.GetFullPath resolves \..\ traversal so a poisoned entry like
                // "C:\Windows\Installer\..\..\Users\Other\secret" cannot pass the prefix
                // check and reach the Detail field.
                string canonical;
                try
                {
                    canonical = Path.GetFullPath(cleaned);
                }
                catch (Exception ex) when (ex is ArgumentException
                                        or PathTooLongException
                                        or NotSupportedException)
                {
                    continue;
                }

                // Equality OR separator-anchored prefix; bare StartsWith would match a
                // sibling like C:\Windows\InstallerExtra against C:\Windows\Installer.
                if (canonical.Equals(installerRoot, StringComparison.OrdinalIgnoreCase) ||
                    canonical.StartsWith(installerRootBoundary, StringComparison.OrdinalIgnoreCase))
                {
                    return PendingRebootResult.Block(
                        PendingRebootReason.PendingRenameInCache,
                        canonical);
                }
            }
        }

        return PendingRebootResult.Clean;
    }

    /// <summary>
    /// Strips the NT object form (\??\) and long-path (\\?\) prefixes
    /// used by Session Manager. A destination entry queued with
    /// MOVEFILE_REPLACE_EXISTING carries a leading '!' before the
    /// prefix ("!\??\C:\..."); it encodes the replace flag, not the
    /// path, and must come off first or the prefix never matches and a
    /// rename INTO the cache slips the gate.
    /// </summary>
    private static string StripNtPathPrefix(string s)
    {
        if (s.StartsWith("!", StringComparison.Ordinal))
            s = s[1..];
        return
            s.StartsWith(@"\??\", StringComparison.Ordinal) ? s[4..] :
            s.StartsWith(@"\\?\", StringComparison.Ordinal) ? s[4..] :
            s;
    }
}
