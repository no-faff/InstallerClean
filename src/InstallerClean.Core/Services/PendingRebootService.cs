namespace InstallerClean.Services;

/// <summary>
/// Returns Block when any of three signals indicates the MSI cache is currently at risk:
/// the _MSIExecute mutex, the Installer\InProgress key, or any PendingFileRenameOperations
/// entry, source or destination, that names a path under %SystemRoot%\Installer or that
/// the app cannot place at all.
/// </summary>
/// <remarks>
/// Every entry is checked, not just the sources: a queued rename INTO the cache is as
/// much a reason to keep out as one moving a file within it, and the destination form
/// is why the leading '!' comes off before anything else.
/// </remarks>
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
    private readonly IVolumeMountProbe _volumes;

    /// <summary>Override for %SystemRoot%; null in production.</summary>
    private readonly string? _windowsRootOverride;

    public PendingRebootService(IRegistryReader registry, IMutexProbe mutex, IVolumeMountProbe volumes)
        : this(registry, mutex, volumes, windowsRootOverride: null)
    {
    }

    /// <summary>Test-only constructor. Lets the path comparison run against an arbitrary %SystemRoot%.</summary>
    internal PendingRebootService(
        IRegistryReader registry,
        IMutexProbe mutex,
        IVolumeMountProbe volumes,
        string? windowsRootOverride)
    {
        _registry = registry;
        _mutex = mutex;
        _volumes = volumes;
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
        // substitute throwing fakes to exercise what a non-conforming implementation
        // would do; this wrap keeps Check's contract intact whether the bound
        // implementation honours the interface contract or not. A throw is a read
        // that did not happen, so it answers the same state the reader's own failure
        // return does and the two arrive at the gate below as one case.
        RegistryKeyPresence installerInProgress;
        try
        {
            installerInProgress = _registry.LocalMachineKeyPresence(InstallerInProgressKey);
        }
        catch (Exception ex) when (ex is not OutOfMemoryException and not StackOverflowException)
        {
            installerInProgress = RegistryKeyPresence.Unreadable;
        }
        if (installerInProgress is RegistryKeyPresence.Present)
            return PendingRebootResult.Block(PendingRebootReason.InstallerInProgress);

        // ABSENT IS THE ONLY STATE THAT CARRIES ON, AND THE TEST IS WRITTEN THAT WAY
        // ROUND ON PURPOSE. A key that is not there says no transaction is suspended,
        // which is the answer nearly every machine gives. Anything else is a read
        // that did not answer, so whether one is suspended is not established, and a
        // state added to the enum later falls here rather than through.
        if (installerInProgress is not RegistryKeyPresence.Absent)
            return PendingRebootResult.Block(PendingRebootReason.RegistryCheckUnreadable);

        // Bare PendingFileRenameOperations is too broad (any third-party uninstaller
        // writes to it); refine to "an entry, source or destination, that names a path
        // inside %SystemRoot%\Installer, or that names somewhere this cannot place".
        RegistryMultiStringRead renames;
        try
        {
            renames = _registry.LocalMachineMultiStringValue(
                SessionManagerKey, PendingFileRenameOperationsValue);
        }
        catch (Exception ex) when (ex is not OutOfMemoryException and not StackOverflowException)
        {
            renames = new RegistryMultiStringRead(RegistryMultiStringState.Unreadable);
        }

        // A value that is not there is a machine with nothing queued, and that is the
        // ordinary reading on a machine at rest.
        if (renames.State is RegistryMultiStringState.Absent)
            return PendingRebootResult.Clean;

        // Everything that is not a read array is a value whose contents this does not
        // have: refused, faulted, or written in a form it does not read. A value
        // recorded at the name Windows queues renames under, whose contents cannot be
        // seen, leaves it unestablished whether one of them names the cache, and the
        // pass below is what would have established it. The same arm takes a state
        // added to the enum later, so a new one refuses until somebody rules on it.
        if (renames is not { State: RegistryMultiStringState.Read, Entries: { } entries })
            return PendingRebootResult.Block(PendingRebootReason.RegistryCheckUnreadable);

        var windowsRoot = _windowsRootOverride
            ?? Environment.GetFolderPath(Environment.SpecialFolder.Windows);
        var installerRoot = Path.Combine(windowsRoot, "Installer");

        // AN ENTRY NAMING THE CACHE WINS OVER ONE THAT COULD NOT BE PLACED, WHATEVER
        // ORDER THEY SIT IN. The first says what is queued and names the path; the
        // second says only that something is queued somewhere unknown. Returning on
        // the first cache match is that rule: nothing later can beat it, so the pass
        // ends there, and an unplaceable entry seen on the way is remembered rather
        // than acted on until every entry has been read.
        var anyUnplaceable = false;

        foreach (var raw in entries)
        {
            if (string.IsNullOrEmpty(raw)) continue;

            switch (Locate(raw, installerRoot, out var canonical))
            {
                case EntryLocation.InsideCache:
                    return PendingRebootResult.Block(
                        PendingRebootReason.PendingRenameInCache, canonical);
                case EntryLocation.Unplaceable:
                    anyUnplaceable = true;
                    break;
            }
        }

        return anyUnplaceable
            ? PendingRebootResult.Block(PendingRebootReason.PendingRenameUnresolved)
            : PendingRebootResult.Clean;
    }

    /// <summary>Where one queued entry points, as far as this can establish it.</summary>
    private enum EntryLocation
    {
        /// <summary>It names the cache folder or something under it.</summary>
        InsideCache,

        /// <summary>It was placed, and it is somewhere else.</summary>
        Elsewhere,

        /// <summary>
        /// It names somewhere, and this could not say where. The entry is a positive
        /// finding that a file operation is queued whose target cannot be ruled out.
        /// </summary>
        Unplaceable,
    }

    /// <summary>
    /// Places one raw entry against the cache folder.
    ///
    /// The leading '!' comes off first: a destination queued with
    /// MOVEFILE_REPLACE_EXISTING carries one before the prefix ("!\??\C:\..."), and it
    /// encodes the replace flag rather than any part of the path, so leaving it on
    /// means no prefix ever matches and a rename INTO the cache slips the gate.
    ///
    /// WHAT SURVIVES <see cref="InstallerCacheHelpers.StripLongPathPrefix"/> STILL
    /// CARRYING ITS PREFIX IS THE WHOLE OF WHAT THE VOLUME LOOKUPS ARE FOR, and that is
    /// what makes the two compose. That helper takes the prefix off a drive-rooted path
    /// and turns the UNC form into an ordinary \\server\share path, which are the two
    /// remainders that are already paths this can compare. It leaves everything else
    /// whole, and everything else is a volume named some other way.
    /// </summary>
    private EntryLocation Locate(string raw, string installerRoot, out string? canonical)
    {
        canonical = null;

        var entry = raw.StartsWith('!') ? raw[1..] : raw;
        var cleaned = InstallerCacheHelpers.StripLongPathPrefix(entry);

        foreach (var prefix in NtPathPrefixes)
        {
            if (cleaned.StartsWith(prefix, StringComparison.Ordinal))
                return LocateOnNamedVolume(cleaned[prefix.Length..], installerRoot, out canonical);
        }

        return LocateOrdinaryPath(cleaned, installerRoot, out canonical);
    }

    /// <summary>
    /// An entry the prefix strip left as an ordinary path: the drive-letter form and
    /// the UNC form. A UNC path is compared like any other and simply never matches,
    /// %SystemRoot%\Installer being local on any machine that can boot.
    ///
    /// ANYTHING NOT FULLY QUALIFIED IS UNPLACEABLE RATHER THAN SKIPPED. Path.GetFullPath
    /// completes a value that is not rooted from the process's working directory, so
    /// reading one would answer about wherever the app was started from rather than
    /// about the entry. A bare "\Windows\Installer\9f05cba.msi" is the cache path
    /// without its volume, which is a queued operation this cannot place rather than one
    /// it can dismiss.
    /// </summary>
    private static EntryLocation LocateOrdinaryPath(string cleaned, string installerRoot, out string? canonical)
    {
        canonical = null;
        if (!Path.IsPathFullyQualified(cleaned))
            return EntryLocation.Unplaceable;

        if (!TryCanonicalise(cleaned, out var resolved))
            return EntryLocation.Unplaceable;

        return Place(resolved, installerRoot, out canonical);
    }

    /// <summary>
    /// An entry still carrying its prefix, which is a volume named in one of the two
    /// forms that survive the strip. Anything else that reaches here names something
    /// this cannot place; there is no reading on which such a value is safe to dismiss,
    /// because nothing establishes that it is not a form the app does not understand.
    /// </summary>
    private EntryLocation LocateOnNamedVolume(string rest, string installerRoot, out string? canonical)
    {
        canonical = null;

        if (rest.StartsWith(VolumeGuidPrefix, StringComparison.OrdinalIgnoreCase))
        {
            var separator = rest.IndexOf('\\');
            var name = separator < 0 ? rest : rest[..separator];
            var remainder = separator < 0 ? string.Empty : rest[(separator + 1)..];
            return PlaceOnVolume($@"\\?\{name}\", remainder, installerRoot, out canonical);
        }

        if (rest.StartsWith(GlobalRootPrefix, StringComparison.OrdinalIgnoreCase))
            return PlaceByDeviceName(rest[GlobalRootPrefix.Length..], installerRoot, out canonical);

        return EntryLocation.Unplaceable;
    }

    /// <summary>
    /// Finds the volume behind an NT device name and places the entry on it.
    ///
    /// THE MATCH IS AGAINST THE WHOLE HEAD OF THE ENTRY RATHER THAN A FIXED NUMBER OF
    /// COMPONENTS, because a device name is not always two of them: "\Device\Harddisk0\
    /// Partition3" names a volume exactly as "\Device\HarddiskVolume1" does. So each
    /// volume's own device name is tested as a prefix of the entry, and what follows it
    /// is the path on that volume.
    ///
    /// The volumes are walked rather than the drive letters. Asking which letter maps to
    /// a device answers nothing for a volume that has no letter, and those are ordinary:
    /// an EFI system partition and a recovery partition both sit on a healthy machine
    /// without one, and it is precisely such a volume that gets named this way in the
    /// first place. A gate that could not place them would refuse to run on any machine
    /// part-way through servicing its own boot files.
    /// </summary>
    private EntryLocation PlaceByDeviceName(string deviceAndPath, string installerRoot, out string? canonical)
    {
        canonical = null;

        var target = @"\" + deviceAndPath;
        var volumes = VolumeGuidPaths();
        if (volumes is null)
            return EntryLocation.Unplaceable;

        foreach (var volumeGuidPath in volumes)
        {
            var device = DosDeviceTarget(
                volumeGuidPath.Trim('\\').TrimStart('?').Trim('\\'));
            if (string.IsNullOrEmpty(device))
                continue;

            string remainder;
            if (target.Equals(device, StringComparison.OrdinalIgnoreCase))
                remainder = string.Empty;
            else if (target.StartsWith(device + '\\', StringComparison.OrdinalIgnoreCase))
                remainder = target[(device.Length + 1)..];
            else
                continue;

            return PlaceOnVolume(volumeGuidPath, remainder, installerRoot, out canonical);
        }

        return EntryLocation.Unplaceable;
    }

    /// <summary>
    /// Places a path that is relative to a volume's own root by asking where that volume
    /// is mounted and joining each answer to it.
    ///
    /// A VOLUME MOUNTED NOWHERE IS PLACED RATHER THAN UNPLACEABLE, and the difference is
    /// the whole reason <see cref="VolumeMountPoints.Answered"/> exists. Nothing on such
    /// a volume can be inside %SystemRoot%\Installer, that path reaching its files
    /// through a mount point, so an empty answer settles the question. An empty result
    /// from a query that failed settles nothing and looks identical.
    /// </summary>
    private EntryLocation PlaceOnVolume(
        string volumeGuidPath, string remainder, string installerRoot, out string? canonical)
    {
        canonical = null;

        var mounts = MountPointsFor(volumeGuidPath);
        if (!mounts.Answered)
            return EntryLocation.Unplaceable;

        foreach (var mount in mounts.Paths)
        {
            if (!TryCanonicalise(Path.Combine(mount, remainder), out var resolved))
                return EntryLocation.Unplaceable;

            if (Place(resolved, installerRoot, out canonical) == EntryLocation.InsideCache)
                return EntryLocation.InsideCache;
        }

        return EntryLocation.Elsewhere;
    }

    /// <summary>
    /// The containment comparison, and every arm above ends here so that none of them
    /// grows one of its own. Equality OR a separator-anchored prefix: a bare StartsWith
    /// would match a sibling like C:\Windows\InstallerExtra against C:\Windows\Installer.
    /// </summary>
    private static EntryLocation Place(string resolved, string installerRoot, out string? canonical)
    {
        var inside = resolved.Equals(installerRoot, StringComparison.OrdinalIgnoreCase)
            || resolved.StartsWith(
                installerRoot + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase);

        canonical = inside ? resolved : null;
        return inside ? EntryLocation.InsideCache : EntryLocation.Elsewhere;
    }

    /// <summary>
    /// Resolves \..\ traversal so a poisoned entry like
    /// "C:\Windows\Installer\..\..\Users\Other\secret" cannot pass the containment check
    /// and reach the Detail field. False where the value is not a path this can complete,
    /// which leaves the entry unplaceable.
    /// </summary>
    private static bool TryCanonicalise(string path, out string resolved)
    {
        try
        {
            resolved = Path.GetFullPath(path);
            return true;
        }
        catch (Exception ex) when (ex is ArgumentException
                                or PathTooLongException
                                or NotSupportedException)
        {
            resolved = string.Empty;
            return false;
        }
    }

    // THE THREE PROBE CALLS GO THROUGH THESE AND NOT DIRECTLY, so that Check keeps
    // the "never throws" contract IPendingRebootService states, exactly as the
    // registry and mutex reads above are wrapped. A throw out of a volume query is
    // that query failing, and a failed query is the app not knowing where an entry
    // points, which is the condition this gate refuses on. Answering "nothing
    // established" is therefore the same answer the call's own failure return gives,
    // and it reaches the user as a blocked run rather than as a crash.

    private VolumeMountPoints MountPointsFor(string volumeGuidPath)
    {
        try
        {
            return _volumes.MountPointsFor(volumeGuidPath);
        }
        catch (Exception ex) when (ex is not OutOfMemoryException and not StackOverflowException)
        {
            return VolumeMountPoints.NoAnswer;
        }
    }

    private IReadOnlyList<string>? VolumeGuidPaths()
    {
        try
        {
            return _volumes.VolumeGuidPaths();
        }
        catch (Exception ex) when (ex is not OutOfMemoryException and not StackOverflowException)
        {
            return null;
        }
    }

    private string? DosDeviceTarget(string dosDeviceName)
    {
        try
        {
            return _volumes.DosDeviceTarget(dosDeviceName);
        }
        catch (Exception ex) when (ex is not OutOfMemoryException and not StackOverflowException)
        {
            return null;
        }
    }

    /// <summary>
    /// The two prefixes Session Manager writes: the NT object form, and the long-path
    /// form the kernel adds. Either one still on the front after the strip means the
    /// remainder names its volume rather than carrying a drive root.
    /// </summary>
    private static readonly string[] NtPathPrefixes = { @"\??\", @"\\?\" };

    private const string VolumeGuidPrefix = "Volume{";

    private const string GlobalRootPrefix = @"GLOBALROOT\";
}
