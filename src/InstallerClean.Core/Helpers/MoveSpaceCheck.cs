using InstallerClean.Services;

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
    /// installer cache. A move there is a rename, so it consumes nothing and
    /// frees nothing.
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
    /// ONE ANSWER, SEVERAL READERS, AND ONLY ONE OF THEM ACTED ON IT. The
    /// confirmation dialog told the user the space would not come back until
    /// they deleted the folder, which is false there and is a statement about
    /// somebody's own disk that nothing had established; the Move button's
    /// tooltip and the completion screen's restore line said the same; the
    /// result log and the opt-in report carried it as the destination's kind;
    /// and <see cref="RefusalFreeSpace(string, long, long?)"/> returned on its
    /// first line, so the free-space check did not run at all. That last is
    /// the exact fault this file exists for, arriving through the one
    /// destination it could not see. The rest tell the user or the record; it
    /// is the only one that changes what the app does.
    ///
    /// THE REASON GIVEN FOR LEAVING THE PATH ARITHMETIC COVERED THE TOOLTIP
    /// AND NOTHING ELSE. It was cost: the tooltip re-read this on every
    /// keystroke. Every other reader is once per Move, on an operation that
    /// already resolves the destination twice and writes a probe file into it
    /// before anything is classified, so nothing was being bought there. And
    /// the stated cost was not the real hazard either. The hazard is that
    /// GetVolumePathName validates a REMOTE path over the network, so a call
    /// that can meet a remote path belongs off the dispatcher whatever it
    /// costs; <see cref="StorageHelpers.IsRemotePath"/> answers those without
    /// asking, and the window resolves the tooltip's copy on a background hop.
    ///
    /// THE CACHE SIDE WAS NOT FIXED WITH THE DESTINATION SIDE, AND THAT IS
    /// WHAT THIS CHANGE IS. The destination side began asking Windows which
    /// volume a folder is on. The cache side went on being answered by
    /// <c>Environment.SpecialFolder.System</c>, which is the volume hosting
    /// the Windows system directory and not the volume hosting the installer
    /// cache. Those two are the same wherever nothing is mounted between them,
    /// and they part exactly where a volume is mounted at
    /// <c>C:\Windows\Installer</c> itself: the same mount point that motivated
    /// the destination fix, with the mount at the other end of the comparison.
    /// The method's own name said cache and its query said system, and the
    /// name was the accurate half.
    ///
    /// BOTH WRONG ANSWERS WERE AVAILABLE THERE AND NEITHER WAS FREE. True for
    /// a destination that is not on the cache's volume sent
    /// <see cref="RefusalFreeSpace(string, long, long?)"/> out on its first
    /// line, so a move that really copies was never measured, which is this
    /// file's own fault reached by a second route. False for a destination
    /// that really is on it cost twice over: the free-space check ran on a
    /// move needing no room and could refuse one that would have worked, and
    /// the classification starts from this same answer, so the dialog's
    /// warning stayed collapsed, the completion claimed freed space and the
    /// record agreed with it.
    ///
    /// NOTHING IS CACHED ON EITHER SIDE, WHICH IS A DECISION AND NOT AN
    /// OMISSION. A mount point can be created or removed under a path while
    /// the app is open, and a cached same-volume answer surviving that would
    /// skip the free-space check on a volume nothing had measured, which is
    /// this change's own fault reintroduced by the thing meant to make it
    /// cheap.
    ///
    /// THE CACHE SIDE WAS HELD FOR THE LIFE OF THE PROCESS AND THE
    /// JUSTIFICATION DID NOT SURVIVE THE CHANGE OF SUBJECT. It read: the
    /// volume hosting the running system cannot be unmounted or remounted
    /// underneath it, and that asymmetry is the whole reason one side is held
    /// and the other is asked every time. Every word of that is true of the
    /// system volume. None of it is true of a volume mounted at the installer
    /// cache, which is precisely the thing that can appear or vanish while the
    /// app is open, so the field went out with the subject that earned it.
    ///
    /// THE PATH IS STILL HELD AND THE VOLUME IS NOT, WHICH IS NOT THE SAME
    /// DECISION ARRIVING AGAIN. <see cref="InstallerCacheHelpers.InstallerFolder"/>
    /// is resolved once and kept, and it can be: no running process sees the
    /// Windows directory move. Which volume is mounted at that path is a
    /// question about the machine right now, and it is asked right now.
    ///
    /// THE NAME SAYS DRIVE, THE QUESTION IS ABOUT A VOLUME, AND THAT IS KEPT
    /// RATHER THAN OVERLOOKED. <c>MoveDestinationKinds.SameDrive</c> is the
    /// literal <c>sameDrive</c> that goes into the result log and the opt-in
    /// report, and the string cannot follow a rename made here: it is a wire
    /// value, matched at the receiving end against a fixed set of names.
    /// Nothing on this side rejects a value outside them, so a reader who goes
    /// looking for that guard here finds none and can conclude there is none.
    /// Renaming the method alone would leave the code saying volume and the
    /// record it feeds saying drive, which is a worse state than one loose word
    /// used consistently in both. What was actually wrong with the name was
    /// never the noun: it said cache while the query said system, and that is
    /// the half this change fixes.
    ///
    /// The rest of the file already knew the case, which is what made this one
    /// method out of step rather than the app declining an exotic machine:
    /// <see cref="AvailableFreeSpaceForDestination"/> walks from the destination
    /// rather than the path root so a mount point answers for its own volume,
    /// and MoveFilesService's ReconcileMove says a mount point falls back to
    /// copy-and-delete like any other volume boundary.
    ///
    /// Anything it cannot resolve is not the same volume, and for the decision
    /// this method owns that is the safe way round:
    /// <see cref="RefusalFreeSpace(string, long, long?)"/> consults the
    /// measurement instead of skipping it, so a move that really copies is
    /// measured whichever side of the comparison declined to answer.
    ///
    /// THE TWO REFUSALS ARE NOT INTERCHANGEABLE FOR A CALLER THAT CLASSIFIES,
    /// AND <see cref="ResolveIsOnInstallerCacheDrive"/> IS WHERE SUCH A CALLER
    /// GOES. A caller that goes on to ask which volume the destination is on is
    /// putting the same question to the same path, so a destination this method
    /// could not resolve is one it cannot resolve either: the refusal is
    /// reproduced in front of it, and it has nothing to classify. The cache side
    /// is different, no caller having a query of its own for it, so that refusal
    /// is never put a second time. Through this method both arrive as a plain
    /// false, indistinguishable from a destination genuinely on another volume,
    /// and a caller classifying on that can report space freed by a move that
    /// freed none. A bool has no room for "I could not tell", so the caller that
    /// needs the difference takes the nullable overload and the caller that wants
    /// the safe way round takes this one.
    /// </remarks>
    /// <param name="installerCacheRoot">
    /// Test-only real-folder override for the cache root (null in production,
    /// which asks about the real <see cref="InstallerCacheHelpers.InstallerFolder"/>).
    /// The volume question still goes to Windows about a real path, so this
    /// relocates WHICH path is asked about and nothing else; it cannot let a
    /// MockFileSystem answer it, the whole file being outside
    /// <c>System.IO.Abstractions</c> for that reason. It mirrors
    /// <see cref="InstallerCacheHelpers.IsInstallerFolderOrChild"/>'s own
    /// override on the neighbouring safety gate.
    ///
    /// WITHOUT IT NO ASSERTION ON THIS METHOD CAN TELL IT FROM THE ONE IT
    /// REPLACED. The cache's volume and the system directory's are the same
    /// string on every host this suite can run on, so every fixture answers the
    /// same against the fixed code and against the code that asked
    /// <c>Environment.SpecialFolder.System</c>. Moving the cache root off that
    /// volume is the one thing a test cannot do to the real cache folder without
    /// a second disk and administrator rights, and this is the parameter that
    /// does it instead.
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
            // than remembered. The two are the same until something is mounted
            // between them, and this method exists for the machine where
            // something is.
            var cacheVolume = StorageHelpers.GetVolumeMountPoint(
                installerCacheRoot ?? InstallerCacheHelpers.InstallerFolder);
            if (cacheVolume is null) return null;

            var destinationVolume =
                StorageHelpers.GetVolumeMountPoint(Path.GetFullPath(destination));
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
    /// exists for. That is the system drive on an ordinary one and it is named
    /// as the cache's here because this is the arm where the two can part. An
    /// unmeasurable
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
