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
    /// Re-reads the given patch claims and returns the paths that are no longer
    /// removable, for the action services to call ONCE THEY HOLD
    /// <c>Global\_MSIExecute</c> and before they touch a file.
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
/// What one under-lease re-read found.
/// </summary>
/// <param name="HeldBack">
/// The paths to drop, for either of the two reasons below.
/// </param>
/// <param name="RecordsIncomplete">
/// At least one property read failed, so at least one path is held back on those
/// grounds rather than on a program having reclaimed it. It says which of the two
/// happened, for exactly the reason
/// <see cref="ReverifyResult.RecordsIncomplete"/> does: the report the user reads
/// names a cause, the app has two causes here, and only one of them is true of
/// any given run. A read that could not be made has not shown the file to be
/// removable, so it is held back either way; what it has not shown is that a
/// program wants it back.
/// </param>
public record UnderLeaseRecheck(
    IReadOnlyList<string> HeldBack,
    bool RecordsIncomplete = false);

/// <summary>
/// Result of a re-verify. <see cref="Surviving"/> + <see cref="Dropped"/> partition
/// the input: <see cref="Surviving"/> is still safe to act on, <see cref="Dropped"/>
/// is now claimed by a non-removable registered package and must be skipped.
/// </summary>
/// <param name="RecordsIncomplete">
/// The re-verify's own enumeration could not read every product, so it withheld
/// the removable class and dropped every superseded candidate on those grounds
/// rather than on a program reclaiming them. It says which of the two happened,
/// because the report the user reads names a cause and only one of the two causes
/// is true.
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
    bool RecordsIncomplete = false,
    IReadOnlyList<PatchClaim>? SurvivingPatchClaims = null)
{
    /// <summary>Never null: an absent list reads as nothing to re-read rather than as a fault.</summary>
    public IReadOnlyList<PatchClaim> SurvivingPatchClaims { get; init; }
        = SurvivingPatchClaims ?? Array.Empty<PatchClaim>();
}
