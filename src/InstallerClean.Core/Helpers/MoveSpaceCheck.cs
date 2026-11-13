using InstallerClean.Services;

namespace InstallerClean.Helpers;

/// <summary>
/// The one decision both hosts make before a Move: is there room for it. In Core so
/// the window and the command line give the same answer. A Move run without the room
/// fills the volume, collects a failure for every file after that, and leaves
/// <c>C:\Windows\Installer</c> half emptied into a full drive.
///
/// Path work and a volume query only. Nothing here goes through
/// <c>System.IO.Abstractions</c>: the question is about a real volume, so a
/// MockFileSystem must not be able to answer it.
/// </summary>
internal static class MoveSpaceCheck
{
    /// <summary>
    /// Whether a Move to <paramref name="destination"/> renames each file on the
    /// installer cache's VOLUME rather than copying it: the destination is a local path
    /// on that volume. A rename consumes nothing and frees nothing.
    /// </summary>
    /// <remarks>
    /// THIS IS THE QUESTION <see cref="RefusalFreeSpace(string, long, long?)"/> ASKS, AND
    /// ONLY IT. A Move through a share copies every file, even through this PC's own share
    /// onto the cache's own volume, so a share answers false here without being asked, and
    /// so does a drive letter mapped onto a share, its volume being the redirector's. Where
    /// the space ends up is <see cref="ResolveIsOnInstallerCacheDrive"/>'s question, and
    /// the two part for exactly that destination. Decide whether a Move copies from this,
    /// never from that.
    ///
    /// BOTH SIDES ARE ASKED OF WINDOWS, NEVER WORKED OUT FROM THE PATH. Windows lets
    /// a volume be mounted into an empty folder instead of being given a letter, so a
    /// second disk can sit at <c>C:\Data</c>, or at <c>C:\Windows\Installer</c>
    /// itself. <c>Path.GetPathRoot</c> answers <c>C:\</c> for a path under either,
    /// and a mounted volume at a path cannot be told from an ordinary folder by the
    /// path alone. The cache side is the cache's own volume, not the system
    /// directory's: the two are the same until a volume is mounted between them.
    ///
    /// A true given for a destination on another volume would skip the measurement on
    /// a move that copies; a false given for one on the cache's volume runs the check on
    /// a move needing no room, where it could refuse a Move that would have worked.
    /// Anything it cannot resolve is not a rename, and for this decision that is the safe
    /// way round: the measurement is consulted instead of skipped, so a move that really
    /// copies is measured whichever side of the comparison declined to answer.
    ///
    /// GetVolumePathName validates a REMOTE path over the network, so a call that
    /// can meet one belongs off the dispatcher. <see cref="StorageHelpers.IsRemotePath"/>
    /// answers those from the spelling without asking.
    ///
    /// NOTHING IS CACHED ON EITHER SIDE. A mount point can be created or removed
    /// under a path while the app is open, and a held same-volume answer would then
    /// skip the free-space check on a volume nothing had measured.
    /// <see cref="InstallerCacheHelpers.InstallerFolder"/>'s path is resolved once and
    /// kept, since no running process sees the Windows directory move; which volume is
    /// mounted at that path is asked each time.
    ///
    /// <see cref="AvailableFreeSpaceForDestination"/> walks from the destination
    /// rather than the path root for the same reason, so a mount point answers for
    /// its own volume, and MoveFilesService's ReconcileMove treats a mount point as a
    /// volume boundary that falls back to copy-and-delete.
    /// </remarks>
    /// <param name="installerCacheRoot">
    /// Test-only real-folder override for the cache root (null in production,
    /// which asks about the real <see cref="InstallerCacheHelpers.InstallerFolder"/>).
    /// The volume question still goes to Windows about a real path, so this
    /// relocates WHICH path is asked about and nothing else; it cannot let a
    /// MockFileSystem answer it, the whole file being outside
    /// <c>System.IO.Abstractions</c> for that reason.
    ///
    /// The cache's volume and the system directory's are the same string on every
    /// host this suite can run on, so this is how a test puts the cache root on a
    /// volume other than the system directory's, which the real cache folder cannot
    /// be moved to without a second disk and administrator rights.
    /// </param>
    internal static bool RenamesOnInstallerCacheDrive(string destination, string? installerCacheRoot = null) =>
        LocalVolumeMatch(destination, installerCacheRoot, out _) ?? false;

    /// <summary>
    /// Whether the space a Move to <paramref name="destination"/> takes up stays on the
    /// installer cache's VOLUME, answered in three states: true and false where Windows
    /// settled it, and null where it did not. A Move there frees nothing.
    /// </summary>
    /// <remarks>
    /// FOR A CALLER THAT SAYS SOMETHING ABOUT WHERE THE SPACE GOES. The confirmation
    /// dialog's warning that the space does not come back until the folder goes, the
    /// Move button's tooltip, the completion screen's heading and restore line, and the
    /// destination's kind in the result log and the opt-in report all read it
    /// (<see cref="MoveDestinationClassifier.Classify"/>). Telling a person where their
    /// space goes, or writing which kind of destination it was into the record, is a
    /// claim, and a claim needs the difference between "another volume" and "Windows
    /// would not say". It is not whether the Move copies:
    /// <see cref="RenamesOnInstallerCacheDrive"/> answers that, and a Move through this
    /// PC's own share copies onto this very volume.
    ///
    /// A LOCAL PATH IS ASKED BY ITS VOLUME, as <see cref="RenamesOnInstallerCacheDrive"/>
    /// asks it.
    ///
    /// A SHARE, OR A DRIVE LETTER MAPPED ONTO ONE, IS ASKED WHETHER IT IS THIS PC'S OWN
    /// SHARE ONTO THAT VOLUME. The nearest folder of the destination that is there is opened,
    /// through the share, and the serial number in its file id (<c>FILE_ID_INFO</c>) is
    /// compared with the cache root's. A match is confirmed the way the destination gates
    /// confirm one (<see cref="InstallerCacheHelpers.LocalPathThroughThisPCsVolumes"/>): the
    /// same file id opens on the cache's volume as a folder ending in the share path's own
    /// folder names. So a share on another computer imaged from the same disk, whose volume
    /// carries the same serial number, answers false. A share whose server does not answer
    /// the file-id question answers false, the answer a share has always had here. Any
    /// other failure to read either side, and anything thrown, answers null.
    ///
    /// The share is asked over the network, so a caller belongs off the dispatcher, as
    /// every caller is: the window's tooltip on its background hop and the pre-flight's
    /// classification inside its <c>Task.Run</c>.
    ///
    /// An empty destination is null. There is no path to be on any volume, so there is
    /// nothing to report either way.
    /// </remarks>
    /// <param name="destination">The folder the move would write into.</param>
    /// <param name="installerCacheRoot">
    /// As <see cref="RenamesOnInstallerCacheDrive"/>'s, which carries the whole of why
    /// it exists.
    /// </param>
    /// <param name="identities">
    /// Reads a folder's file id. Null in production (<see cref="FileIdentityReader"/>).
    /// </param>
    /// <param name="localPathOf">
    /// Confirms a serial number match by the folder's local path. Null in production
    /// (<see cref="InstallerCacheHelpers.LocalPathThroughThisPCsVolumes"/>).
    /// </param>
    internal static bool? ResolveIsOnInstallerCacheDrive(
        string destination, string? installerCacheRoot = null, IFileIdentityReader? identities = null,
        LocalPathReader? localPathOf = null)
    {
        if (string.IsNullOrWhiteSpace(destination)) return null;

        try
        {
            if (StorageHelpers.IsRemotePath(destination))
                return ShareOnCacheVolume(destination, installerCacheRoot, identities, localPathOf);

            var local = LocalVolumeMatch(destination, installerCacheRoot, out var destinationVolume);
            if (local != false || destinationVolume is null) return local;

            return StorageHelpers.GetDriveKind(destinationVolume) == DriveType.Network
                ? ShareOnCacheVolume(destination, installerCacheRoot, identities, localPathOf)
                : false;
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// Whether <paramref name="destination"/>, spelled as a local path, is on the cache's
    /// volume, asked of Windows by each side's volume mount point: true or false where both
    /// resolved, null where either did not or anything threw. A share's spelling answers
    /// false without being asked. <paramref name="destinationVolume"/> is the destination's
    /// mount point where it resolved.
    /// </summary>
    private static bool? LocalVolumeMatch(
        string destination, string? installerCacheRoot, out string? destinationVolume)
    {
        destinationVolume = null;
        if (string.IsNullOrWhiteSpace(destination)) return null;

        if (StorageHelpers.IsRemotePath(destination)) return false;

        try
        {
            // The cache's volume, not the system directory's, and asked rather
            // than remembered: the two part where a volume is mounted between them.
            var cacheVolume = StorageHelpers.GetVolumeMountPoint(
                installerCacheRoot ?? InstallerCacheHelpers.InstallerFolder);
            if (cacheVolume is null) return null;

            destinationVolume =
                StorageHelpers.GetVolumeMountPoint(FullPaths.GetFullPath(destination));
            if (destinationVolume is null) return null;

            return string.Equals(destinationVolume, cacheVolume, StringComparison.OrdinalIgnoreCase);
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// Whether the nearest folder of <paramref name="destination"/> that is there is on the
    /// cache root's volume, by the serial number in its file id, confirmed by its local path:
    /// true or false where both were read, false where the destination's folder gives no file
    /// id or its id does not open there as the same folder, and null where either side would
    /// not read. A folder that is not there yet is passed over for its parent.
    /// </summary>
    private static bool? ShareOnCacheVolume(
        string destination, string? installerCacheRoot, IFileIdentityReader? identities,
        LocalPathReader? localPathOf)
    {
        identities ??= Identities;
        var cacheRoot = installerCacheRoot ?? InstallerCacheHelpers.InstallerFolder;
        if (identities.ReadOutcome(cacheRoot, out var root) != FileIdentityRead.Read)
            return null;

        string? folder = PathText.TrimEndingDirectorySeparator(destination);
        while (!string.IsNullOrEmpty(folder))
        {
            switch (identities.ReadOutcome(folder, out var reached))
            {
                case FileIdentityRead.Read:
                    return reached.VolumeSerialNumber == root.VolumeSerialNumber
                        && (localPathOf ?? InstallerCacheHelpers.LocalPathThroughThisPCsVolumes)(destination, [cacheRoot])
                            is not null;
                case FileIdentityRead.IdentityUnavailable:
                    return false;
                case FileIdentityRead.NamesNothing:
                    var parent = PathText.GetDirectoryName(folder);
                    if (parent is null || parent.Equals(folder, StringComparison.OrdinalIgnoreCase)) return null;

                    // A volume or share root is asked with its trailing separator, the
                    // spelling that opens a share's root as a folder.
                    var parentRoot = PathText.GetPathRoot(parent);
                    if (parentRoot is not null
                        && parent.TrimEnd('\\', '/').Equals(parentRoot.TrimEnd('\\', '/'), StringComparison.OrdinalIgnoreCase))
                        parent = parentRoot.TrimEnd('\\', '/') + Path.DirectorySeparatorChar;
                    folder = parent;
                    break;
                default:
                    return null;
            }
        }
        return null;
    }

    /// <summary>The reader <see cref="ShareOnCacheVolume"/> uses in production.</summary>
    private static readonly IFileIdentityReader Identities = new FileIdentityReader();

    /// <summary>
    /// Decides whether a Move of <paramref name="totalBytes"/> has room at
    /// <paramref name="destination"/>, given the free space already measured
    /// there. Returns the free byte count when the move must be refused, null
    /// when it may go ahead.
    /// </summary>
    /// <remarks>
    /// Two cases go ahead without the measurement being consulted. A same-volume
    /// move needs no room, and refusing one on free space would refuse exactly
    /// the nearly-full drive holding the cache, which is the machine this app
    /// exists for. That is the system drive on an ordinary machine; it is the
    /// cache's volume that counts, and the two can differ. An unmeasurable
    /// destination (a share whose caller lacks query rights) has established
    /// nothing, and the same rule covers it: no claim rather than a wrong one.
    /// </remarks>
    internal static long? RefusalFreeSpace(string destination, long totalBytes, long? availableFreeSpace)
    {
        if (RenamesOnInstallerCacheDrive(destination)) return null;
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
            var probe = FullPaths.GetFullPath(destination);
            while (!FrameworkDirectory.Exists(probe))
            {
                var parent = PathText.GetDirectoryName(probe);
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
