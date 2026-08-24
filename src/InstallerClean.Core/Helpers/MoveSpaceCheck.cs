namespace InstallerClean.Helpers;

/// <summary>
/// The one decision both hosts make before a Move: is there room for it. In Core
/// because the two answers must not diverge. The window refused a Move it had no
/// room for and the command line ran it, filling the volume, collecting a failure
/// for every file after that, and leaving <c>C:\Windows\Installer</c> half emptied
/// into a full drive.
///
/// Path work and a volume query only. Nothing here goes through
/// <c>System.IO.Abstractions</c>: the question is about a real volume, so a
/// MockFileSystem must not be able to answer it.
/// </summary>
internal static class MoveSpaceCheck
{
    /// <summary>
    /// Whether <paramref name="destination"/> is on the same VOLUME as the
    /// installer cache, which is the system drive. A move there is a rename, so
    /// it consumes nothing and frees nothing.
    /// </summary>
    /// <remarks>
    /// IT COMPARED PATH ROOTS UNTIL 3.0.0, AND A REAL MACHINE SHAPE MADE THAT
    /// ANSWER WRONG. Windows lets a volume be mounted into an empty folder
    /// instead of being given a letter, so a second disk can sit at
    /// <c>C:\Data</c>. <c>Path.GetPathRoot</c> answers <c>C:\</c> for a
    /// destination under it, and a mounted volume at <c>C:\Data</c> and an
    /// ordinary folder at <c>C:\Data</c> cannot be told apart from the path
    /// alone, which is why no amount of path arithmetic could have settled it.
    ///
    /// FOUR SURFACES TOOK THAT ANSWER AND TWO OF THEM ACTED ON IT. The
    /// confirmation dialog told the user the space would not come back until
    /// they deleted the folder, which is false there and is a statement about
    /// somebody's own disk that nothing had established; the Move button's
    /// tooltip and the completion screen's restore line said the same; and
    /// <see cref="RefusalFreeSpace(string, long, long?)"/> returned on its first
    /// line, so the free-space check did not run at all. That last is the exact
    /// fault this file exists for, arriving through the one destination it could
    /// not see.
    ///
    /// THE REASON GIVEN FOR LEAVING IT COVERED ONE OF THE FOUR. It was cost: the
    /// tooltip re-read this on every keystroke. The other three are once per
    /// operation and already sit inside a pre-flight that does two path
    /// resolutions and a write probe, so nothing was being bought there. And the
    /// stated cost was not the real hazard either. The hazard is that
    /// GetVolumePathName validates a REMOTE path over the network, so the call
    /// belongs off the dispatcher whatever it costs;
    /// <see cref="StorageHelpers.IsRemotePath"/> answers those without asking,
    /// and the window resolves the tooltip's copy on a background hop.
    ///
    /// NOTHING IS CACHED PER DESTINATION, WHICH IS A DECISION AND NOT AN
    /// OMISSION. A mount point can be created or removed under a path while the
    /// app is open, and a cached same-volume answer surviving that would skip
    /// the free-space check on a volume nothing had measured, which is this
    /// change's own fault reintroduced by the thing meant to make it cheap.
    ///
    /// The rest of the file already knew the case, which is what made this one
    /// method out of step rather than the app declining an exotic machine:
    /// <see cref="AvailableFreeSpaceForDestination"/> walks from the destination
    /// rather than the path root so a mount point answers for its own volume,
    /// and MoveFilesService's ReconcileMove says a mount point falls back to
    /// copy-and-delete like any other volume boundary.
    ///
    /// Anything it cannot resolve is not the same volume, which is the safe way
    /// round in both directions: a caller omits a claim rather than making a
    /// wrong one, and the free-space measurement runs rather than being skipped.
    /// </remarks>
    internal static bool IsOnInstallerCacheDrive(string destination)
    {
        if (string.IsNullOrWhiteSpace(destination)) return false;

        // Asked before anything is resolved, and true on its own terms rather
        // than as a guard: a share is not a local volume, so it is not this one.
        if (StorageHelpers.IsRemotePath(destination)) return false;

        try
        {
            var systemVolume = SystemVolumeMountPoint();
            if (systemVolume is null) return false;

            var destinationVolume =
                StorageHelpers.GetVolumeMountPoint(Path.GetFullPath(destination));
            if (destinationVolume is null) return false;

            return string.Equals(destinationVolume, systemVolume, StringComparison.OrdinalIgnoreCase);
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// The volume the running Windows is on, answered once and kept.
    /// </summary>
    /// <remarks>
    /// SAFE TO KEEP FOR THE LIFE OF THE PROCESS in a way the destination's
    /// answer is not: the volume hosting the running system cannot be unmounted
    /// or remounted underneath it. That asymmetry is the whole reason one side
    /// is held and the other is asked every time.
    ///
    /// A FAILURE IS NOT KEPT. Storing one would leave the app answering "not the
    /// same volume" for the rest of the session on a single transient refusal:
    /// the safe direction, and permanently degraded, with no same-drive warning
    /// where one is due and a free-space check on a move that needs none.
    /// Leaving the field null retries on the next read and self-heals.
    ///
    /// Two threads racing here both do the same query and store the same string.
    /// The loser's work is wasted and nothing else happens, which is why there is
    /// no lock around a value that cannot change.
    /// </remarks>
    private static string? _systemVolume;

    private static string? SystemVolumeMountPoint() =>
        _systemVolume ??= StorageHelpers.GetVolumeMountPoint(
            Environment.GetFolderPath(Environment.SpecialFolder.System));

    /// <summary>
    /// Decides whether a Move of <paramref name="totalBytes"/> has room at
    /// <paramref name="destination"/>, given the free space already measured
    /// there. Returns the free byte count when the move must be refused, null
    /// when it may go ahead.
    /// </summary>
    /// <remarks>
    /// Two cases go ahead without the measurement being consulted. A same-drive
    /// move needs no room, and refusing one on free space would refuse exactly
    /// the nearly-full system drive this app exists for. An unmeasurable
    /// destination (a share whose caller lacks query rights) has established
    /// nothing, and the same rule covers it: no claim rather than a wrong one.
    /// </remarks>
    internal static long? RefusalFreeSpace(string destination, long totalBytes, long? availableFreeSpace)
    {
        if (IsOnInstallerCacheDrive(destination)) return null;
        if (availableFreeSpace is not long free) return null;
        return free < totalBytes ? free : null;
    }

    /// <summary>
    /// <see cref="RefusalFreeSpace(string, long, long?)"/> for a caller that has
    /// not measured the destination itself, which is the command line: its
    /// destination is validated before the scan and created by
    /// <c>MoveFilesService</c>, so nothing on the way has had reason to ask.
    /// </summary>
    internal static long? RefusalFreeSpace(string destination, long totalBytes) =>
        RefusalFreeSpace(destination, totalBytes, AvailableFreeSpaceForDestination(destination));

    /// <summary>
    /// The free space at <paramref name="destination"/>, or at the nearest
    /// ancestor of it that exists.
    /// </summary>
    /// <remarks>
    /// GetDiskFreeSpaceEx takes a directory that exists and fails on one that does
    /// not, and the destination usually does not: the scheduled
    /// <c>/m D:\Backup</c> this check is for names a folder the Move is about to
    /// create, so measuring it directly returns null and the check silently passes
    /// on the invocation it was written for. Free space is a property of the
    /// volume, so an ancestor answers for the leaf. The walk starts at the
    /// destination rather than jumping to the path root because a folder that DOES
    /// exist may be a mount point for another volume, and the root would then
    /// answer for the wrong one.
    /// </remarks>
    internal static long? AvailableFreeSpaceForDestination(string destination)
    {
        if (string.IsNullOrWhiteSpace(destination)) return null;
        try
        {
            var probe = Path.GetFullPath(destination);
            while (!Directory.Exists(probe))
            {
                var parent = Path.GetDirectoryName(probe);
                // Null at a path root, and equal to itself if GetDirectoryName
                // ever stops making progress: either way no ancestor is left.
                if (string.IsNullOrEmpty(parent) || parent == probe) return null;
                probe = parent;
            }
            return StorageHelpers.GetAvailableFreeSpace(probe);
        }
        catch
        {
            return null;
        }
    }
}
