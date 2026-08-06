using InstallerClean.Models;

namespace InstallerClean.Services;

/// <summary>
/// A Move stopped by one of its own guards after files had already left
/// <c>C:\Windows\Installer</c>, carrying what the batch had done when it
/// stopped.
///
/// <see cref="Reason"/> is the guard's own account of which condition fired, and
/// no host copy selects on it: the two conditions differ in nothing a reader does
/// next, and a sentence naming one would be naming it for a batch that met the
/// other. It is here so a host that ever does need the distinction has to ask for
/// it, rather than inferring one from the fact that this type was thrown.
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
    public MoveAbortedException(string message, MoveResult partial,
        string destination, MoveAbortReason reason) : base(message)
    {
        Partial = partial;
        Destination = destination;
        Reason = reason;
    }

    /// <summary>What the batch had moved, and what it had failed on, when it stopped.</summary>
    public MoveResult Partial { get; }

    /// <summary>
    /// The folder the files in <see cref="Partial"/> are actually in, which is
    /// the destination as it resolved when the batch started rather than the
    /// path the caller asked for.
    ///
    /// The two differ in exactly the case this exception reports. Every file the
    /// batch moved went wherever the destination pointed then, and the guard
    /// stops the batch before moving the file that detects the change, so no file
    /// is ever moved to a changed target. A host naming the caller's own string
    /// after that would name a folder the files are not in, and send anyone
    /// following it to an empty one.
    /// </summary>
    public string Destination { get; }

    /// <summary>Which of the guard's two conditions stopped the batch.</summary>
    public MoveAbortReason Reason { get; }
}

/// <summary>
/// Why a Move's destination guard stopped a batch part way. Two states because
/// they are two different things to have found out, and one of them is not a
/// change of target at all: a share that dropped or an ACL that closed leaves the
/// destination exactly where the user put it.
///
/// What they share is the whole of what a host acts on: the batch is stopped, the
/// files already moved are in <see cref="MoveAbortedException.Destination"/>, and
/// the next step is the same either way.
/// </summary>
public enum MoveAbortReason
{
    /// <summary>
    /// The destination resolves somewhere other than it did when the batch
    /// started. Something replaced or redirected the folder.
    /// </summary>
    ResolvesElsewhere,

    /// <summary>
    /// The destination resolved when the batch started and will not resolve now,
    /// so nothing can be shown about where it points. A network destination going
    /// away mid-batch reaches this, as does an ACL change closing the folder to
    /// the elevated process. Treated as a change because a resolve that degraded
    /// is a path whose reparse points went unexpanded, which is the one thing a
    /// containment check exists to see through; that is a fail-safe about what to
    /// DO, and it establishes nothing about what happened to the folder.
    /// </summary>
    StoppedResolving,
}
