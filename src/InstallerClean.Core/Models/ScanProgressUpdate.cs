using InstallerClean.Helpers;

namespace InstallerClean.Models;

/// <summary>
/// One scan progress update. <see cref="IsMilestone"/> separates the fixed phase
/// messages from the per-item ticker that names or counts what the current phase
/// is working through. The split is for screen readers: a host reads the
/// milestone stream out through a live region, and the ticker, which can run to
/// hundreds of updates in a few seconds, is display-only, because announcing
/// every update queues speech faster than it can be spoken.
///
/// <see cref="Position"/> and <see cref="Total"/> carry where the phase has
/// reached, in items, so a host with a progress bar can fill its share of the bar
/// in proportion to the work rather than to the number of updates it has been
/// sent. They are counts rather than a
/// percentage, because what share of a bar a phase is worth belongs to whatever
/// draws the bar, and a host that draws none still has a count it can show.
///
/// <see cref="Total"/> is zero where the phase cannot know how many items it will
/// see, which is the folder walk, and a host reads that as a position it can show
/// and cannot place. A phase that starts reporting a total it cannot stand behind
/// moves a bar in proportion to a number that is still changing.
///
/// <see cref="IsWait"/> marks the third kind: a line naming what the scan is waiting
/// for, reported when a read of a source folder has waited about a second, and an
/// empty one when that wait ends. A host shows the line in the milestone's place and
/// announces it as it announces a milestone, puts back what that place showed when the
/// empty one arrives, and passes both on whatever rate it shows the ticker at. It does
/// not count either as a milestone, so a bar divided by the milestones is divided the
/// same way on a machine that waits as on one that does not. A wait update carries no
/// position, and there is one pair for each wait.
///
/// The line carries the wait itself, <see cref="Wait"/>, through which the host can stop
/// waiting for that drive or share (<see cref="SourceFolderWait.StopWaiting"/>). The empty
/// one carries none.
///
/// <see cref="PutBack"/> marks the fourth kind, of which a scan reports at most one, and
/// before any other update: the scan has put back into the Installer folder files an
/// earlier Move or Delete had left in the app's own folder there
/// (<see cref="Services.IInHandRecovery"/>). It is neither a milestone nor a line of the
/// ticker and carries no message. A host tells the person from it and not from the scan's
/// result, so that a scan which then fails or is cancelled has still said what it did.
/// </summary>
public sealed record ScanProgressUpdate(
    string Message,
    bool IsMilestone = true,
    int Position = 0,
    int Total = 0,
    bool IsWait = false,
    SourceFolderWait? Wait = null,
    Services.InHandRecoveryResult? PutBack = null)
{
    /// <summary>The update saying what the scan put back before it began, <paramref name="putBack"/>.</summary>
    internal static ScanProgressUpdate ForPutBack(Services.InHandRecoveryResult putBack) =>
        new(string.Empty, IsMilestone: false, PutBack: putBack);

    /// <summary>
    /// The update naming <paramref name="wait"/>, a wait on a drive letter and its colon, a
    /// share, or a path of another form, or, for null, the update saying that the wait has
    /// ended.
    /// </summary>
    internal static ScanProgressUpdate Waiting(SourceFolderWait? wait) =>
        new(wait is null ? string.Empty : DisplayHelpers.WaitingFor(wait.Root),
            IsMilestone: false, IsWait: true, Wait: wait);
}
