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
    /// Whether <paramref name="destination"/> resolves to the same drive as the
    /// installer cache, which is the system drive. A move there is a rename, so
    /// it consumes nothing and frees nothing.
    ///
    /// Path arithmetic with no drive query, because the Move button's tooltip
    /// re-reads it on every keystroke, and that is also the limit of it. A
    /// destination under a mount point inside the system drive compares equal on
    /// GetPathRoot while sitting on a separate volume, so this answers same-drive
    /// for a move that is really a copy and a delete across a boundary, and
    /// <see cref="RefusalFreeSpace(string, long, long?)"/> then passes it without
    /// measuring anything. GetVolumePathName is what would settle it, at a volume
    /// query per keystroke. The rest of the codebase already knows the case:
    /// <see cref="AvailableFreeSpaceForDestination"/> walks from the destination
    /// rather than the path root so a mount point answers for its own volume, and
    /// MoveFilesService's ReconcileMove says a mount point falls back to
    /// copy-and-delete like any other volume boundary.
    ///
    /// Anything it cannot resolve is not the same drive, which is the safe way
    /// round: a caller then omits a claim rather than making a wrong one.
    /// </summary>
    internal static bool IsOnInstallerCacheDrive(string destination)
    {
        if (string.IsNullOrWhiteSpace(destination)) return false;
        try
        {
            var destRoot = Path.GetPathRoot(Path.GetFullPath(destination));
            if (string.IsNullOrEmpty(destRoot)) return false;
            var systemRoot = Path.GetPathRoot(
                Environment.GetFolderPath(Environment.SpecialFolder.System));
            return string.Equals(destRoot, systemRoot, StringComparison.OrdinalIgnoreCase);
        }
        catch
        {
            return false;
        }
    }

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
