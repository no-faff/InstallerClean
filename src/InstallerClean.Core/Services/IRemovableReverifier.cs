using InstallerClean.Models;

namespace InstallerClean.Services;

/// <summary>
/// Re-checks a set of removal candidates against the Windows Installer API
/// immediately before a Move or Delete acts on them, to catch the one window
/// neither the fresh pending-reboot gate nor the <c>Global\_MSIExecute</c> hold
/// can see: a patch
/// whose state changed AND settled between the scan and the click (a superseded
/// patch reverted to Applied because its superseding patch was uninstalled).
///
/// It re-runs the full classifier (<see cref="IInstallerQueryService"/>) rather
/// than re-querying a single retained product code, because after the
/// shared-patch verdict merge a patch can revert to Applied for a DIFFERENT
/// product than the one whose code survived the merge; re-enumerating is correct
/// across every product for nothing but a few seconds spent before a rare,
/// destructive batch.
///
/// A true orphan can be dropped here too, and that is the second reason the
/// enumeration is full rather than per candidate. A file the API never claimed is
/// not a file it can never claim: an install that wrote its package into the
/// cache before the folder walk reached it, and registered that package after the
/// query had already passed, leaves a file that is an orphan by every measurement
/// the scan made and is claimed by the time the user clicks. Only re-walking the
/// whole registered set finds it, and finding it is the last thing between that
/// file and a permanent delete.
/// </summary>
public interface IRemovableReverifier
{
    /// <summary>
    /// Re-enumerates the registered set and splits <paramref name="candidatePaths"/>
    /// into those still safe to remove and those a currently-registered,
    /// non-removable package now claims (which must be dropped from the batch and
    /// reported as skipped). An empty input short-circuits without querying.
    /// Propagates any exception the enumeration raises (an inability to
    /// re-verify must stop the batch, not silently pass it).
    /// </summary>
    Task<ReverifyResult> ReverifyAsync(
        IReadOnlyList<string> candidatePaths,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Re-reads the given patch claims and returns the paths the re-read has not
    /// left shown to be removable, for the action services to call ONCE THEY HOLD
    /// <c>Global\_MSIExecute</c> and before they touch a file. Not "no longer
    /// removable": one of the three causes is a read that established nothing
    /// either way, and it keeps the file for want of a verdict rather than on one.
    ///
    /// It exists because <see cref="ReverifyAsync"/> cannot be the last word. The
    /// hold is taken inside the action service, so every caller runs the full
    /// re-verify before it, and the window the batch acts across is that
    /// enumeration's whole duration rather than the instant after it. Windows
    /// writes a patch's registration during the execute sequence, and
    /// <c>_MSIExecute</c> is documented as set only while the execute-sequence
    /// tables are being processed, so the write falls inside the phase the mutex
    /// covers.
    ///
    /// What is NOT established, and must not be written here as though it were:
    /// whether an info API can return a registration its own transaction has
    /// written and not yet committed. That answer decides how WIDE the window
    /// this closes really was; it does not decide whether the re-read is worth
    /// taking, which is why this was built without it.
    ///
    /// Synchronous, and that is a requirement rather than a convenience: the
    /// lease must be released by the thread that took it, so the whole hold is one
    /// unbroken synchronous body with no await in it to hop threads.
    ///
    /// WHAT IT COVERS, stated narrowly because the difference matters. It re-asks
    /// about claims that already existed, so it catches a verdict changing on one
    /// of them, which is the reverting superseded patch the full re-verify is for.
    /// It cannot see a claim from a product that held none when the claims were
    /// collected: there is nothing to re-ask about, and only re-walking the whole
    /// registered set would find it. That case is covered up to the full
    /// re-verify's own enumeration and no further. Closing it as well would mean
    /// running that enumeration inside a machine-wide installer lock on every run,
    /// which is a worse trade than the sliver it buys.
    /// </summary>
    /// <param name="claims">
    /// Every claim naming a path still in the batch, from the
    /// <see cref="ReverifyResult.SurvivingPatchClaims"/> the pre-lease pass
    /// returned. Empty short-circuits without touching the API, which is the
    /// ordinary case: most batches are true orphans, which carry no claim to
    /// re-read.
    /// </param>
    UnderLeaseRecheck RecheckUnderLease(IReadOnlyList<PatchClaim> claims);
}

/// <summary>
/// Why one file was kept back. Three states rather than two because they are
/// three different things to have found out, and the report the user reads names
/// the cause: a confirmed positive, an inability, and neither.
/// </summary>
public enum HeldBackReason
{
    /// <summary>
    /// The records were read and say an installed product needs the file: a
    /// superseded patch back at Applied, or one still uninstallable and so needed
    /// to roll back with.
    /// </summary>
    Reclaimed,

    /// <summary>
    /// The records were read and no longer hold the registration the claim names.
    /// Not a reclaim, because nothing is left to be in any state at all; not an
    /// unreadable record, because the read succeeded.
    ///
    /// It condemns the file rather than releasing it, which is not what the shape
    /// of the answer suggests and is the measurement this state exists for. The
    /// absence code means "no such product in the ACCOUNT AND CONTEXT you asked
    /// in", not "no such product", and the context a claim carries was settled when
    /// the scan collected it. A pairing that moved context between the scan and the
    /// click therefore answers absent while its registration is live, so releasing
    /// on that answer would put a needed file into a permanent delete on the
    /// strength of a question asked the wrong way round.
    /// </summary>
    RecordsChanged,

    /// <summary>
    /// A read failed, so nothing was established either way. It has not shown the
    /// file to be removable, which is what keeps it in place.
    /// </summary>
    RecordsUnreadable,
}

/// <summary>
/// How many files were kept back for each cause. Counts rather than one cause for
/// the set, because a batch can meet more than one and a sentence that is true of
/// four files out of five is false.
///
/// The counts are the whole of what the report needs, the paths themselves being
/// carried alongside by whichever result holds this. Every producer increments at
/// the point it adds the path, so the two cannot come apart, and
/// <see cref="Total"/> is what a test holds them to.
/// </summary>
public readonly record struct HeldBackReasons(
    int Reclaimed = 0,
    int RecordsChanged = 0,
    int RecordsUnreadable = 0)
{
    /// <summary>Files kept back for any cause. Equals the accompanying path list's count.</summary>
    public int Total => Reclaimed + RecordsChanged + RecordsUnreadable;

    /// <summary>This tally with one more file counted against <paramref name="reason"/>.</summary>
    public HeldBackReasons Plus(HeldBackReason reason) => reason switch
    {
        HeldBackReason.Reclaimed => this with { Reclaimed = Reclaimed + 1 },
        HeldBackReason.RecordsChanged => this with { RecordsChanged = RecordsChanged + 1 },
        _ => this with { RecordsUnreadable = RecordsUnreadable + 1 },
    };

    /// <summary>
    /// Merges two tallies, for the fold that joins what the pre-act re-verify kept
    /// back to what the under-lease re-read did. Addition rather than an OR of
    /// flags: the two producers keep back DIFFERENT files, so their causes
    /// accumulate instead of one standing in for both.
    /// </summary>
    public static HeldBackReasons operator +(HeldBackReasons a, HeldBackReasons b) =>
        new(a.Reclaimed + b.Reclaimed,
            a.RecordsChanged + b.RecordsChanged,
            a.RecordsUnreadable + b.RecordsUnreadable);
}

/// <summary>
/// What one under-lease re-read found.
/// </summary>
/// <param name="HeldBack">
/// The paths to drop, for any of the causes in <see cref="HeldBackReason"/>.
/// </param>
/// <param name="Reasons">
/// How many of <paramref name="HeldBack"/> fell to each cause, for exactly the
/// reason <see cref="ReverifyResult.Reasons"/> carries it: the report the user
/// reads names a cause, one re-read can meet all three, and a file's own cause is
/// the only thing that is true of it. A read that could not be made has not shown
/// the file to be removable, so it is held back whichever cause it fell to; what
/// it has not shown is that a program wants it back.
/// </param>
public record UnderLeaseRecheck(
    IReadOnlyList<string> HeldBack,
    HeldBackReasons Reasons = default);

/// <summary>
/// Result of a re-verify. <see cref="Surviving"/> + <see cref="Dropped"/> partition
/// the input: <see cref="Surviving"/> is still safe to act on, <see cref="Dropped"/>
/// is now claimed by a non-removable registered package and must be skipped.
/// </summary>
/// <param name="Reasons">
/// How many of <see cref="Dropped"/> fell to each cause. Per file rather than per
/// run: an enumeration that could not read every product withholds the removable
/// class, so a single batch can hold both a file a live registered product claims
/// and a file whose verdict was withheld, and one cause for the set would name a
/// cause that did not occur for some of them.
/// </param>
/// <param name="SurvivingPatchClaims">
/// Every claim naming a path in <see cref="Surviving"/>, for the action service
/// to re-read once it holds the installer mutex
/// (<see cref="IRemovableReverifier.RecheckUnderLease"/>). One entry per
/// claim, not per path, because a patch applied to several products is claimed
/// by each of them and any one of those verdicts can move on its own.
/// </param>
public record ReverifyResult(
    IReadOnlyList<string> Surviving,
    IReadOnlyList<string> Dropped,
    HeldBackReasons Reasons = default,
    IReadOnlyList<PatchClaim>? SurvivingPatchClaims = null)
{
    /// <summary>Never null: an absent list reads as nothing to re-read rather than as a fault.</summary>
    public IReadOnlyList<PatchClaim> SurvivingPatchClaims { get; init; }
        = SurvivingPatchClaims ?? Array.Empty<PatchClaim>();
}
