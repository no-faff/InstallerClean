using InstallerClean.Models;

namespace InstallerClean.Helpers;

/// <summary>
/// The Move destination label the result log and the opt-in report carry, in Core so any
/// host builds it from one definition.
/// </summary>
public static class MoveDestinationClassifier
{
    /// <summary>
    /// The volume question as the app asks it, and the value every reading of it falls
    /// back to. A wrapper rather than the method itself because
    /// <see cref="MoveSpaceCheck.ResolveIsOnInstallerCacheDrive"/> carries an optional
    /// second parameter, which no delegate of this shape can bind.
    /// </summary>
    public static bool? AskWhetherOnCacheVolume(string destination) =>
        MoveSpaceCheck.ResolveIsOnInstallerCacheDrive(destination);

    /// <summary>
    /// Classifies the move destination for the result log. Returns one of
    /// <see cref="MoveDestinationKinds"/>: <c>uncShare</c> for a share,
    /// <c>sameDrive</c> when the destination is on the volume the Installer
    /// cache is on, <c>removableDrive</c> or <c>differentFixedDrive</c> for
    /// another volume by what is mounted there, and <c>unknown</c> for anything
    /// that could not be established.
    /// </summary>
    /// <remarks>
    /// ASKED ABOUT THE VOLUME AND NOT THE LETTER. Do not classify from the drive
    /// letter: Path.GetPathRoot handed to DriveInfo classifies a destination under
    /// a volume mounted into a folder on C: by whatever C: is, and a removable disk
    /// mounted at <c>C:\Data</c> then reads as a fixed drive. No sentence the user
    /// reads depends on the difference, because ClassifySpaceOutcome maps fixed,
    /// removable and share alike to "space was freed". The field is read in the
    /// result log and in the opt-in report, where a wrong value reads as an answer.
    ///
    /// DriveInfo cannot be asked the right question. It is built from a
    /// drive-letter root, and a mounted folder has no letter, so
    /// StorageHelpers.GetDriveKind takes the mount point instead.
    ///
    /// THREE VOLUME QUERIES RATHER THAN TWO, ON PURPOSE. The same-drive answer comes
    /// from MoveSpaceCheck so that this window and the command line cannot answer
    /// it differently, which is the reason that decision lives in Core at all.
    /// Two of the three are its own, one for the cache folder and one for the
    /// destination; threading the destination's mount point out of it to save
    /// the duplicate below would buy back one call per Move and put that back at
    /// risk. This runs once per operation, inside the pre-flight, off the
    /// dispatcher.
    /// </remarks>
    /// <param name="dest">The folder the move would write into.</param>
    /// <param name="resolveIsOnCacheVolume">
    /// The same-drive question, taken rather than reached for: the window hands over
    /// the resolve behind its tooltip, so the label written to the result log and the
    /// answer behind the tooltip come from one place. Omitted, it is
    /// <see cref="AskWhetherOnCacheVolume"/>, which is what the app itself asks, so a
    /// call site that leaves it out gets the app's own answer.
    /// </param>
    public static string Classify(
        string dest, Func<string, bool?>? resolveIsOnCacheVolume = null)
    {
        if (string.IsNullOrWhiteSpace(dest)) return MoveDestinationKinds.Unknown;

        // Answered from the spelling, before anything is asked of the network.
        // The same test MoveSpaceCheck uses, so the two cannot disagree about
        // what counts as a share.
        if (StorageHelpers.IsRemotePath(dest)) return MoveDestinationKinds.UncShare;

        // Three-state for the same reason the tooltip is: this value goes into
        // the result log and the opt-in report, and it is read as an answer. A
        // volume question Windows would not settle is unknown, not another
        // drive, and calling it another drive is what would let the completion
        // card head a move with a freed count it had not established.
        var onCacheVolume = (resolveIsOnCacheVolume ?? AskWhetherOnCacheVolume)(dest);
        if (onCacheVolume is null) return MoveDestinationKinds.Unknown;
        if (onCacheVolume.Value) return MoveDestinationKinds.SameDrive;

        try
        {
            var volume = StorageHelpers.GetVolumeMountPoint(FullPaths.GetFullPath(dest));
            if (volume is null) return MoveDestinationKinds.Unknown;

            return StorageHelpers.GetDriveKind(volume) switch
            {
                DriveType.Fixed => MoveDestinationKinds.DifferentFixedDrive,
                DriveType.Removable => MoveDestinationKinds.RemovableDrive,
                // A mapped drive letter reaches here rather than the spelling
                // test above, its path being local in shape and remote in fact.
                DriveType.Network => MoveDestinationKinds.UncShare,
                _ => MoveDestinationKinds.Unknown,
            };
        }
        catch
        {
            return MoveDestinationKinds.Unknown;
        }
    }
}
