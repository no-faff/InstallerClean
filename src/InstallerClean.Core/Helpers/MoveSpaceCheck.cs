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
    /// Whether <paramref name="destination"/> is on the same VOLUME as the
    /// installer cache. A move there is a rename, so it consumes nothing and
    /// frees nothing.
    /// </summary>
    /// <remarks>
    /// BOTH SIDES ARE ASKED OF WINDOWS, NEVER WORKED OUT FROM THE PATH. Windows lets
    /// a volume be mounted into an empty folder instead of being given a letter, so a
    /// second disk can sit at <c>C:\Data</c>, or at <c>C:\Windows\Installer</c>
    /// itself. <c>Path.GetPathRoot</c> answers <c>C:\</c> for a path under either,
    /// and a mounted volume at a path cannot be told from an ordinary folder by the
    /// path alone. The cache side is the cache's own volume, not the system
    /// directory's: the two are the same until a volume is mounted between them.
    ///
    /// EVERY READER OF THE ANSWER DEPENDS ON IT. The confirmation dialog's warning
    /// that the space does not come back until the folder goes, the Move button's
    /// tooltip, the completion screen's restore line, the destination's kind in the
    /// result log and the opt-in report, and
    /// <see cref="RefusalFreeSpace(string, long, long?)"/>, which skips the
    /// free-space check for a move that needs no room. A true given for a
    /// destination on another volume would skip the measurement on a move that
    /// copies; a false given for one on the cache's volume would run the check on a
    /// move needing no room, where it could refuse a Move that would have worked,
    /// and would leave the dialog's warning collapsed and the completion and the
    /// record claiming freed space.
    ///
    /// GetVolumePathName validates a REMOTE path over the network, so a call that
    /// can meet one belongs off the dispatcher. <see cref="StorageHelpers.IsRemotePath"/>
    /// answers those from the spelling without asking, and the window resolves the
    /// tooltip's copy on a background hop.
    ///
    /// NOTHING IS CACHED ON EITHER SIDE. A mount point can be created or removed
    /// under a path while the app is open, and a held same-volume answer would then
    /// skip the free-space check on a volume nothing had measured.
    /// <see cref="InstallerCacheHelpers.InstallerFolder"/>'s path is resolved once and
    /// kept, since no running process sees the Windows directory move; which volume is
    /// mounted at that path is asked each time.
    ///
    /// THE NAME SAYS DRIVE AND THE QUESTION IS ABOUT A VOLUME.
    /// <c>MoveDestinationKinds.SameDrive</c> is the literal <c>sameDrive</c> that goes
    /// into the result log and the opt-in report, a wire value matched at the
    /// receiving end against a fixed set of names, so it cannot follow a rename made
    /// here, and nothing on this side rejects a value outside that set. Renaming the
    /// method alone would leave the code saying volume and the record it feeds saying
    /// drive.
    ///
    /// <see cref="AvailableFreeSpaceForDestination"/> walks from the destination
    /// rather than the path root for the same reason, so a mount point answers for
    /// its own volume, and MoveFilesService's ReconcileMove treats a mount point as a
    /// volume boundary that falls back to copy-and-delete.
    ///
    /// Anything it cannot resolve is not the same volume, and for the decision
    /// this method owns that is the safe way round:
    /// <see cref="RefusalFreeSpace(string, long, long?)"/> consults the
    /// measurement instead of skipping it, so a move that really copies is
    /// measured whichever side of the comparison declined to answer.
    ///
    /// A CALLER THAT CLASSIFIES THE DESTINATION TAKES
    /// <see cref="ResolveIsOnInstallerCacheDrive"/>. Through this method a refusal on
    /// either side arrives as a plain false, the same as a destination genuinely on
    /// another volume, and a caller classifying on that would report space freed by a
    /// move that freed none. A caller that goes on to ask which volume the destination
    /// is on puts the same question to the same path, so a destination refused here
    /// is refused there too; the cache side has no second query anywhere, so its
    /// refusal is seen only through the three-state overload.
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
    internal static bool IsOnInstallerCacheDrive(string destination, string? installerCacheRoot = null) =>
        ResolveIsOnInstallerCacheDrive(destination, installerCacheRoot) ?? false;

    /// <summary>
    /// The same question as <see cref="IsOnInstallerCacheDrive"/>, answered in
    /// three states: true and false where Windows settled it, and null where it
    /// did not.
    /// </summary>
    /// <remarks>
    /// FOR A CALLER THAT SAYS SOMETHING ABOUT THE ANSWER RATHER THAN ACTING ON
    /// IT. Telling a person where their space goes, or writing which kind of
    /// destination it was into the record, is a claim, and a claim needs the
    /// difference between "another volume" and "Windows would not say". The
    /// caller that acts takes the bool and gets the safe way round with it.
    ///
    /// A REMOTE PATH IS A REAL FALSE AND NOT A REFUSAL. A share is not a local
    /// volume, so it is not this one, and that is settled from the spelling
    /// before anything is asked of the network. Null is for the three places
    /// where the question was put and came back without an answer: either
    /// volume failing to resolve, and anything thrown on the way.
    ///
    /// An empty destination is null rather than false for the same reason. There
    /// is no path to be on any volume, so there is nothing to report either way.
    /// </remarks>
    /// <param name="destination">The folder the move would write into.</param>
    /// <param name="installerCacheRoot">
    /// As <see cref="IsOnInstallerCacheDrive"/>'s, which carries the whole of why
    /// it exists.
    /// </param>
    internal static bool? ResolveIsOnInstallerCacheDrive(
        string destination, string? installerCacheRoot = null)
    {
        if (string.IsNullOrWhiteSpace(destination)) return null;

        if (StorageHelpers.IsRemotePath(destination)) return false;

        try
        {
            // The cache's volume, not the system directory's, and asked rather
            // than remembered: the two part where a volume is mounted between them.
            var cacheVolume = StorageHelpers.GetVolumeMountPoint(
                installerCacheRoot ?? InstallerCacheHelpers.InstallerFolder);
            if (cacheVolume is null) return null;

            var destinationVolume =
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
