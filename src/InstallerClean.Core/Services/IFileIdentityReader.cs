namespace InstallerClean.Services;

/// <summary>
/// What <see cref="IFileIdentityReader.ReadOutcome"/> established, with the five
/// ways it can fail kept apart rather than collapsed into one <c>false</c>.
///
/// FOUR OF THE FIVE KEEP FILES BACK AND ONE DOES NOT, and that is the only
/// distinction anything here draws. A path this reader cannot identify is a path
/// the scan cannot match to a walked file, so a registration it was asked about
/// claims nothing through it and the cached file that registration means can be any
/// candidate in the list. <see cref="NamesNothing"/> is the
/// exception and the reason the members are kept apart: a registration whose cached
/// file has already gone gives up nothing, because there is no file for anything to
/// have claimed.
///
/// THAT EXCEPTION IS ONLY READABLE BECAUSE EVERY RECORDED PATH IS RESOLVED FIRST.
/// A claim leaving <c>InstallerQueryService.NormaliseLocalPackagePath</c> is either
/// a location the kernel proved or one whose failure to resolve has already
/// withheld the whole walk-derived offer, so a proven location holding nothing can
/// only mean the file is gone. Before that widening landed the same answer also
/// covered a spelling nothing had settled, and the two were indistinguishable.
///
/// NOTHING BRANCHES ON WHICH OF THE FOUR IT IS. They are counted apart because
/// they are four different facts about a machine, in company with
/// <c>PathResolution</c>, whose note carries the same argument at greater length.
/// </summary>
public enum FileIdentityRead
{
    /// <summary>
    /// The filesystem named the file, so the out value is an identity that can be
    /// compared. The only member any caller treats as success.
    /// </summary>
    Read,

    /// <summary>
    /// There was no string to open. Neither side of the scan can produce this
    /// today: a registration is merged only where its recorded value has a length,
    /// and a candidate carries the path the walk found it at. It counts as a
    /// give-up rather than as an absence because if it ever does fire, nothing was
    /// established about the file the value meant.
    /// </summary>
    NotAPath,

    /// <summary>
    /// Nothing is at the path. THE ONE MEMBER THAT IS NOT A GIVE-UP: on the
    /// registration side it is a cached file that has already gone, which claims
    /// no candidate; on the candidate side it is a file that went between the walk
    /// and this read, which no registration's identity can match either.
    /// </summary>
    NamesNothing,

    /// <summary>
    /// Something is there and <c>CreateFile</c> would not hand back a handle on
    /// it: any error other than the two the kernel uses for an absence.
    ///
    /// ANOTHER PROCESS HOLDING THE FILE IS NOT ONE OF THEM, which is why the open
    /// is written the way it is. It asks for no access bits at all, so there is
    /// nothing for an existing opener's share mode to exclude, and an installer
    /// working on its own cached package cannot make this fail. What is left is
    /// mostly an ACL refusing a process that is already running elevated.
    /// </summary>
    OpenRefused,

    /// <summary>
    /// The handle opened and the filesystem would not give the file's id: a volume
    /// or a Windows build that does not answer this information class.
    /// </summary>
    IdentityUnavailable,

    /// <summary>
    /// The attempt threw. Distinct from every member above, each of which is the
    /// call answering rather than failing to complete.
    /// </summary>
    Faulted,
}

/// <summary>The one question the scan asks of a <see cref="FileIdentityRead"/>.</summary>
public static class FileIdentityReadOutcomes
{
    /// <summary>
    /// Whether this answer keeps files back: the file is there and nothing here can
    /// say which file it is.
    ///
    /// WRITTEN AS THE COMPLEMENT OF THE TWO SETTLED ANSWERS RATHER THAN AS A LIST
    /// OF THE FOUR, so a member added to the enum later withholds instead of
    /// silently passing. Naming the give-ups would leave the new member outside
    /// every branch, with a green build and a file on the offer to show for it.
    ///
    /// <c>FileIdentityReadTally.RefusedTotal</c> is the same membership counted,
    /// and it has to name its four because a count cannot be written as a
    /// complement. The two are held together by a test that walks the enum.
    /// </summary>
    public static bool GivesUpAWithholding(this FileIdentityRead outcome) =>
        outcome is not FileIdentityRead.Read and not FileIdentityRead.NamesNothing;
}

/// <summary>
/// Answers which file on disk a path names, as an identity that can be compared,
/// so two spellings of one file can be recognised as one file.
///
/// WHY IT EXISTS. A registration records where its cached package is as a string,
/// and the scan finds files by walking the folder. Deciding whether a walked file
/// is the one a registration names by comparing those two strings only works
/// while the two strings agree, and Windows Installer writes whatever spelling was
/// in force when the product was installed. One machine's records can spell the
/// same folder as <c>C:\WINDOWS\Installer</c>, <c>C:\Windows\Installer</c> and
/// <c>c:\Windows\Installer</c>, and only a case-insensitive comparison keeps those
/// together.
///
/// Case is the one divergence that comparison closes. The others are a junction,
/// directory symlink or volume mount point anywhere in the chain, a leaf that is
/// itself a symlink, a substituted or mapped drive letter, a UNC spelling, the
/// <c>\\.\</c> device form and an extended-length prefix the kernel declines to
/// expand. Each one names the right file and does not look like the walk's spelling.
///
/// CLOSING THOSE ONE AT A TIME CANNOT BE FINISHED, because the list is somebody's
/// enumeration and nothing tells you when it is complete. Asking the filesystem
/// which file a path names removes the question: every spelling above resolves to
/// the same file, so a registration whose path OPENS is matched to its file
/// whatever it was written as, and no list has to be right.
///
/// A FAILED READ IS NOT A NEUTRAL ANSWER. A path that will not open yields no
/// identity, so the registration behind it claims nothing through this reader and
/// its cached file can be any file in the folder. Every outcome above is counted at
/// <c>RegistrationIdentityMatch.Compare</c>, and each of the four failures keeps
/// files back.
///
/// A WRONG ANSWER IN HERE CAN ONLY EVER COST AN OFFER: an identity that matches
/// nothing changes nothing, an identity that matches wrongly keeps a file back, and
/// a failure keeps files back. Nothing in here can put a file on the offer.
/// </summary>
public interface IFileIdentityReader
{
    /// <summary>
    /// The identity of the file <paramref name="path"/> names, with the answer
    /// named rather than collapsed. <see cref="FileIdentityRead.Read"/> is the
    /// only outcome that fills <paramref name="identity"/>.
    ///
    /// Links are FOLLOWED, which is the opposite of what the containment guards
    /// do and is deliberate. Those ask "is this path itself a reparse point",
    /// because a reparse point is a thing not to act on. This asks "which file is
    /// at the end of this path", because a registration reaching its cached
    /// package through a junction still needs that package.
    /// </summary>
    FileIdentityRead ReadOutcome(string path, out FileIdentity identity);

    /// <summary>
    /// <see cref="ReadOutcome"/> with its answer narrowed to the one question a
    /// caller that only wants to compare files is asking. Defined off the outcome
    /// and sealed, so the two cannot drift apart and no implementation has to be
    /// trusted to keep them in step.
    ///
    /// The scan takes the outcome form instead, because it counts what it was
    /// told as well as acting on it, and a second read to recover the reason
    /// would be a second handle on the same file.
    /// </summary>
    sealed bool TryRead(string path, out FileIdentity identity) =>
        ReadOutcome(path, out identity) == FileIdentityRead.Read;
}

/// <summary>
/// One file on one machine: the volume it sits on and its 128-bit id within that
/// volume, carried as two halves that are compared and never interpreted.
///
/// A record struct so it can key a set directly. Both halves of the id are part of
/// the value: taking only the low half would be the 64-bit identifier Microsoft
/// documents as not necessarily unique on ReFS, which is the collision this type
/// exists to avoid.
/// </summary>
public readonly record struct FileIdentity(
    ulong VolumeSerialNumber,
    ulong FileIdLow,
    ulong FileIdHigh);
