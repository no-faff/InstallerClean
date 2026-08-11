namespace InstallerClean.Services;

/// <summary>
/// Answers which file on disk a path names, as an identity that can be compared,
/// so two spellings of one file can be recognised as one file.
///
/// WHY IT EXISTS. A registration records where its cached package is as a string,
/// and the scan finds files by walking the folder. Deciding whether a walked file
/// is the one a registration names by comparing those two strings only works
/// while the two strings agree, and Windows Installer writes whatever spelling was
/// in force when the product was installed. One machine's own records spell the
/// same folder three ways (<c>C:\WINDOWS\Installer</c> 121 times,
/// <c>C:\Windows\Installer</c> 15, <c>c:\Windows\Installer</c> twice), and only a
/// case-insensitive comparison keeps those together.
///
/// Case is the divergence anybody has seen. The ones nobody has seen are the
/// problem: a junction, directory symlink or volume mount point anywhere in the
/// chain, a leaf that is itself a symlink, a substituted or mapped drive letter,
/// a UNC spelling, the <c>\\.\</c> device form, an unexpanded environment
/// variable, and an extended-length prefix the kernel declines to expand. Each
/// one names the right file and does not look like the walk's spelling.
///
/// CLOSING THOSE ONE AT A TIME CANNOT BE FINISHED, because the list is somebody's
/// enumeration and nothing tells you when it is complete. Asking the filesystem
/// which file a path names removes the question: every spelling above resolves to
/// the same file, so a registration whose path OPENS is matched to its file
/// whatever it was written as, and no list has to be right.
///
/// IT ONLY EVER WITHHOLDS, and that is what makes it safe to add. It can take a
/// candidate off the offer and can never put one on: a path that will not open
/// yields no identity, an identity that matches nothing changes nothing, and the
/// string comparison it sits behind is unchanged. So the worst a fault in here can
/// do is offer fewer files than the app could have offered.
/// </summary>
public interface IFileIdentityReader
{
    /// <summary>
    /// The identity of the file <paramref name="path"/> names, or false where
    /// there is no answer.
    ///
    /// False covers every reason at once and the caller treats them alike: the
    /// file is not there, the handle would not open, the volume does not carry
    /// the information, or the call failed. None of them is a match, and none of
    /// them is a reason to offer anything.
    ///
    /// Links are FOLLOWED, which is the opposite of what the containment guards
    /// do and is deliberate. Those ask "is this path itself a reparse point",
    /// because a reparse point is a thing not to act on. This asks "which file is
    /// at the end of this path", because a registration reaching its cached
    /// package through a junction still needs that package.
    /// </summary>
    bool TryRead(string path, out FileIdentity identity);
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
