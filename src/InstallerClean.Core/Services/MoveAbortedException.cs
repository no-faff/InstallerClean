using InstallerClean.Models;

namespace InstallerClean.Services;

/// <summary>
/// A Move stopped by one of its own guards after files had already left
/// <c>C:\Windows\Installer</c>, carrying what the batch had done when it
/// stopped.
///
/// It derives from <see cref="LocalisedInvalidOperationException"/> because the
/// message is the guard's own resx sentence, so a host that only knows the base
/// type still shows it. <see cref="Partial"/> is what a host can additionally
/// use.
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
