namespace InstallerClean.Services;

/// <summary>
/// Test seam over the three volume and device P/Invokes the pending-reboot gate
/// needs to read a queued rename written in one of the object manager's own
/// spellings. Introduced so that gate's verdict logic can be pinned by unit
/// tests with a fake, exactly as <see cref="PendingRebootService"/> takes
/// <see cref="IRegistryReader"/> and <see cref="IMutexProbe"/>. The production
/// implementation (<see cref="VolumeMountProbe"/>) is a thin pass-through to the
/// static <c>Kernel32</c> P/Invokes; no buffer contract and no decision crosses
/// this boundary.
/// </summary>
/// <remarks>
/// EVERY MEMBER SEPARATES "THE ANSWER IS NOTHING" FROM "THERE WAS NO ANSWER",
/// because for this caller those mean opposite things. A volume mounted at no
/// path cannot hold anything inside the installer cache, which is a finding; a
/// query that failed leaves the caller unable to say where the entry points,
/// which is the condition the gate blocks on. Collapsing the two into an empty
/// list would turn the second into the first and let a queued operation nobody
/// could locate pass as harmless.
/// </remarks>
public interface IVolumeMountProbe
{
    /// <summary>
    /// Every path the volume named by <paramref name="volumeGuidPath"/> is
    /// mounted at, drive roots and directory mount points alike. The name is the
    /// GUID form with its trailing separator, <c>\\?\Volume{...}\</c>, which is
    /// what Win32 requires and what <see cref="VolumeGuidPaths"/> hands back.
    ///
    /// A volume mounted nowhere answers with no paths. That is an ordinary state
    /// rather than an error: an EFI system partition sits on a perfectly healthy
    /// machine without a mount point, which is why anything naming it has to be
    /// written in a device form in the first place.
    /// </summary>
    VolumeMountPoints MountPointsFor(string volumeGuidPath);

    /// <summary>
    /// Every volume on the machine, in the GUID form
    /// <see cref="MountPointsFor"/> takes. Null where the enumeration could not
    /// be made at all, which is not the same as a machine with no volumes.
    /// </summary>
    IReadOnlyList<string>? VolumeGuidPaths();

    /// <summary>
    /// The NT object the given MS-DOS device name is a symbolic link to:
    /// <c>Volume{...}</c> answers something of the form
    /// <c>\Device\HarddiskVolume3</c>. Null where the name resolves to nothing
    /// or the query failed.
    ///
    /// The name carries no enclosing backslashes and no trailing separator,
    /// which is what the underlying call expects and is why a volume GUID path
    /// is trimmed before it is passed here.
    /// </summary>
    string? DosDeviceTarget(string dosDeviceName);
}

/// <summary>
/// What <see cref="IVolumeMountProbe.MountPointsFor"/> established, with the two
/// empty results kept apart rather than collapsed into one list.
/// </summary>
public sealed record VolumeMountPoints
{
    private VolumeMountPoints(bool answered, IReadOnlyList<string> paths)
    {
        Answered = answered;
        Paths = paths;
    }

    /// <summary>
    /// True when the query answered. READ THIS BEFORE <see cref="Paths"/>: an
    /// answer of no paths at all is still an answer, and it is the one a volume
    /// mounted nowhere gives.
    /// </summary>
    public bool Answered { get; }

    /// <summary>
    /// Every path the volume is mounted at. Empty where it is mounted nowhere,
    /// and empty where nothing was established, which is why
    /// <see cref="Answered"/> is what separates them.
    /// </summary>
    public IReadOnlyList<string> Paths { get; }

    /// <summary>The query answered, with however many paths it found.</summary>
    public static VolumeMountPoints Answer(IReadOnlyList<string> paths) => new(true, paths);

    /// <summary>The query did not answer. Nothing is established about the volume.</summary>
    public static VolumeMountPoints NoAnswer { get; } = new(false, Array.Empty<string>());
}
