using InstallerClean.Models;

namespace InstallerClean.Services;

/// <summary>
/// A Move stopped by one of its own guards after files had already left
/// <c>C:\Windows\Installer</c>, carrying what the batch had done when it
/// stopped.
///
/// It derives from <see cref="LocalisedInvalidOperationException"/> because the
/// message is the guard's own resx sentence and a host that only knows the base
/// type still shows it, which is the whole contract for what may be echoed to a
/// user under elevation. <see cref="Partial"/> is what a host can additionally
/// use: the files in it are in the destination folder, so the count, the size
/// and the line telling the user they can put them back are all owed, and
/// throwing them away with the frame is what this type exists to stop.
///
/// A guard tripping mid-flight is deliberately an exception rather than another
/// flag on <see cref="MoveResult"/>: <c>Cancelled</c> is the user's own choice
/// and <c>InstallerBusy</c> is a precondition that stopped the batch before it
/// touched anything, and this is neither.
/// </summary>
public sealed class MoveAbortedException : LocalisedInvalidOperationException
{
    public MoveAbortedException(string message, MoveResult partial) : base(message) =>
        Partial = partial;

    /// <summary>What the batch had moved, and what it had failed on, when it stopped.</summary>
    public MoveResult Partial { get; }
}
