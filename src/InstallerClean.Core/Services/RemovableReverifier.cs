using InstallerClean.Models;

namespace InstallerClean.Services;

/// <summary>
/// Default <see cref="IRemovableReverifier"/>: re-runs
/// <see cref="IInstallerQueryService.GetRegisteredPackagesAsync"/> and drops any
/// candidate whose path a currently-registered, non-removable package claims.
/// Testable through the same <c>IMsiApi</c> seam the query service uses.
///
/// The full re-enumeration is the cost of the answer for the question this asks.
/// Most candidates are orphans, and what this re-establishes about an orphan is
/// the ABSENCE of any claim ON ITS PATH: there is no registration to re-read,
/// because the reason the file is a candidate is that no registration names it,
/// and only walking the whole registered set again can re-establish an absence.
/// A per-candidate re-read of the same question would answer nothing at all for
/// every orphan while still reporting itself as a re-verification.
///
/// THAT ARGUMENT IS ABOUT THE PATH QUESTION AND THERE IS NO LONGER A SECOND ONE
/// HERE. A per-candidate identity re-read ran below this until 3.0.0, opening each
/// surviving candidate and asking Windows about the code the file declares about
/// itself, and it went with the scan-time pass it duplicated. What re-verifies a
/// candidate now is this full re-enumeration and the under-lease re-read below it,
/// both of which ask about registrations rather than about file contents.
/// </summary>
public sealed class RemovableReverifier : IRemovableReverifier
{
    private readonly IInstallerQueryService _queryService;
    private readonly Interop.IMsiApi _msi;

    /// <summary>
    /// The query service answers the pre-lease pass; the raw API answers the
    /// under-lease one. Two seams rather than one because the second cannot go
    /// through the first: <see cref="IInstallerQueryService"/> offers a whole
    /// enumeration and nothing narrower, and an enumeration is the one thing that
    /// must not run inside a machine-wide installer lock.
    /// </summary>
    public RemovableReverifier(IInstallerQueryService queryService, Interop.IMsiApi msi)
    {
        _queryService = queryService;
        _msi = msi;
    }

    public async Task<ReverifyResult> ReverifyAsync(
        IReadOnlyList<string> candidatePaths,
        CancellationToken cancellationToken = default)
    {
        if (candidatePaths.Count == 0)
            return new ReverifyResult(candidatePaths, Array.Empty<string>());

        // ConfigureAwait(false): Core has no thread affinity; the caller runs this
        // off the dispatcher (behind the operating overlay), exactly as the scan does.
        var query = await _queryService.GetRegisteredPackagesAsync(null, cancellationToken)
            .ConfigureAwait(false);

        // Every path a currently NON-removable registered package claims, against
        // the cause its own row supports. A reverted patch (Superseded -> Applied)
        // appears here as a live claim; a still-superseded patch is IsRemovable and
        // does not appear at all; a true orphan was never registered.
        //
        // ONE ROW IN TWO CARRIES NO CLAIM AT ALL, and telling them apart is the
        // whole of what this map is for. A patch whose State or Uninstallable read
        // failed lands here having established nothing either way: non-removable
        // for want of a verdict rather than on one, so reporting it as a program
        // reclaiming the file would name a cause that did not occur. The withheld kind
        // is the third and occurs again from 3.0.0: a superseded patch whose product's
        // patch set this run could not establish is non-removable for want of a reading
        // rather than on one. Both kinds can be in one batch, which is why the cause is
        // carried per path and not per run.
        //
        // A STILL-REMOVABLE SUPERSEDED PATCH IS DELIBERATELY NOT IN THIS MAP, and that
        // is the one entry whose absence is the point. The map is what condemns a
        // candidate, so a row that is still removable must stay out of it or the offer
        // would be emptied by the pass that exists to re-check it. A superseded patch
        // whose verdict has MOVED since the scan is non-removable now and is therefore
        // in the map, dropped with a cause, which is exactly the reverting-patch case
        // this whole pass was built for.
        //
        // THE ROW DECIDES, NOT THE CANDIDATE, AND ONE CASE THEREFORE READS WEAKER
        // THAN IT COULD. A candidate the scan measured as an orphan, whose path a
        // patch row names here with its verdict unread, is reported as records that
        // could not be read, where "a registration names it now" would also have
        // been true and is the stronger of the two. Keeping the stronger one would
        // mean deciding the cause from what the SCAN saw, and the scan's own
        // reading is exactly what this pass exists to distrust. A weaker true
        // sentence is not the failure here; a stronger one reached by trusting the
        // reading under test would be.
        //
        // A dictionary rather than a set because InstallerQueryResult.Packages is
        // one row per claimed path, so there is a single answer to record for each.
        var nonRemovable = new Dictionary<string, HeldBackReason>(StringComparer.OrdinalIgnoreCase);
        foreach (var pkg in query.Packages)
            if (!pkg.IsRemovable)
                nonRemovable[pkg.LocalPackagePath] =
                    pkg.RemovableWithheld || pkg.VerdictUnreadable
                        ? HeldBackReason.RecordsUnreadable
                        : HeldBackReason.Reclaimed;

        var surviving = new List<string>(candidatePaths.Count);
        var dropped = new List<string>();
        var reasons = default(HeldBackReasons);
        foreach (var path in candidatePaths)
        {
            if (nonRemovable.TryGetValue(path, out var reason))
            {
                dropped.Add(path);
                reasons = reasons.Plus(reason);
            }
            else
            {
                surviving.Add(path);
            }
        }

        // The claims naming a surviving path, carried forward so the action
        // service can re-read them under its own hold. Filtered to the survivors
        // rather than passed whole: a dropped path is already out of the batch,
        // and re-reading it under the installer lock would be work done to
        // confirm a decision nothing can act on.
        var survivingPaths = new HashSet<string>(surviving, StringComparer.OrdinalIgnoreCase);
        var survivingClaims = query.PatchClaims
            .Where(c => survivingPaths.Contains(c.LocalPackagePath))
            .ToList();

        // THE SIBLING PAIRINGS, carried forward so the under-lease re-read can apply the
        // per-product condition rather than only the batch's own pairings. The offer
        // rests on a fact about OTHER patches, and a re-verify that does not re-check the
        // fact the offer rests on is not a re-verify.
        //
        // Collected here rather than under the lease because collecting them needs the
        // enumeration this pass has just run, and an enumeration is the one thing that
        // must not happen inside the machine-wide installer lock. What crosses into the
        // lock is a list of codes to re-read by key.
        //
        // It includes the surviving claims themselves, a patch's own removability being
        // part of the condition, and it is deduplicated by pairing rather than by patch:
        // one patch registered to three products is three pairings and each answers for
        // its own product.
        var survivingProducts = new HashSet<string>(
            survivingClaims.Select(c => c.ProductCode), StringComparer.OrdinalIgnoreCase);
        var siblingClaims = query.PatchClaims
            .Where(c => survivingProducts.Contains(c.ProductCode))
            .ToList();

        return new ReverifyResult(surviving.AsReadOnly(), dropped.AsReadOnly(), reasons,
            survivingClaims.AsReadOnly(), siblingClaims.AsReadOnly());
    }

    /// <inheritdoc />
    /// <remarks>
    /// IT HAS REAL WORK AGAIN FROM 3.0.0, having returned at its first line while
    /// nothing registered was offered. Its input is the patch claims naming a
    /// surviving candidate, and a surviving superseded patch is named by every product
    /// it is registered to, so the list is non-empty on any batch containing one. This
    /// is the last check standing in front of a permanent delete.
    ///
    /// IT RE-READS THE PAIRING AND NOT THE PRODUCT'S WHOLE PATCH SET, SO IT IS NARROWER
    /// THAN THE RULE THE SCAN APPLIED. This is a known gap and not a considered
    /// equivalence, and it must not be described as one. The scan withholds unless every
    /// patch on every product sharing this one positively declares itself
    /// non-removable; this re-asks only whether THIS pairing is still superseded and
    /// still declares zero, which is what <c>IsRemovablePatch</c> answers and that is
    /// half the rule. A SIBLING patch turning removable between the scan and the click
    /// is therefore not seen here.
    ///
    /// WHAT COVERS THE GAP TODAY IS THE PRE-LEASE PASS AND NOTHING ELSE. It re-runs the
    /// whole enumeration moments earlier and does apply the per-product condition, so
    /// the uncovered window is between that enumeration and this re-read rather than
    /// between the scan and the click.
    ///
    /// THE DESIGNED FIX IS NOT BUILT. It is to carry the sibling patch codes forward
    /// from the pre-lease pass, which already knows exactly which they are, and re-read
    /// them here by key: a bounded set of keyed reads rather than an enumeration, and a
    /// keyed read either answers about the record named or says there is no such record.
    /// The reason for building it is that the offer now rests on a fact about OTHER
    /// patches, and a re-verify that does not re-check the fact the offer rests on is
    /// not a re-verify. Leaving it at the narrower question is explicitly the fallback
    /// rather than the design, and the ruling that named it says nobody may adopt it
    /// without measuring the read cost first, which nobody has.
    /// </remarks>
    public UnderLeaseRecheck RecheckUnderLease(UnderLeaseClaims underLease)
    {
        var claims = underLease.Batch;
        var siblingClaims = underLease.Siblings;
        if (claims.Count == 0) return new UnderLeaseRecheck(Array.Empty<string>());

        var heldBack = new List<string>();
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var reasons = default(HeldBackReasons);


        foreach (var claim in claims)
        {
            // Any one claim turning non-removable settles the path, so once a path
            // is condemned its remaining claims are not queried. A path that has
            // passed so far still is: every claim on it has to answer, because the
            // one that has moved may be any of them.
            if (seen.Contains(claim.LocalPackagePath)) continue;

            var context = (Interop.MsiInstallContext)claim.Context;
            var state = InstallerQueryService.GetPatchProperty(
                _msi, claim.PatchCode, claim.ProductCode, claim.UserSid, context,
                Interop.MsiInstallProperty.State);
            var uninstallable = InstallerQueryService.GetPatchProperty(
                _msi, claim.PatchCode, claim.ProductCode, claim.UserSid, context,
                Interop.MsiInstallProperty.Uninstallable);

            var notRegistered = state.NotRegistered || uninstallable.NotRegistered;
            var unreadable = (state.Unreadable && !state.NotRegistered)
                || (uninstallable.Unreadable && !uninstallable.NotRegistered);

            // The order these are asked in IS the judgement; what each cause means
            // is on HeldBackReason, once.
            //
            // Absence first. It condemns, so the two tests below would condemn the
            // same file anyway, and they would name the wrong cause doing it: asked
            // after the removable test, an absent record answers that test with an
            // empty State string and is reported as a reclaim. Right outcome, wrong
            // sentence, and the sentence is the whole of what the user is told.
            //
            // Unreadable second, so a pairing where one read failed and the other
            // came back absent is reported as the failure it contains. A read that
            // could not be made has not shown the file to be removable, this is the
            // last check standing in front of a permanent delete, and the scan's own
            // rule fails the same way. The pre-lease pass answers a failed read of
            // this same pairing the same way, through the row flag its enumeration
            // sets; what it carries and this does not is a whole enumeration's
            // inherited withholding, which has no counterpart here because this
            // judges one named pairing.
            var reason =
                notRegistered && !unreadable ? HeldBackReason.RecordsChanged
                : unreadable ? HeldBackReason.RecordsUnreadable
                : !InstallerQueryService.IsRemovablePatch(state.Value, uninstallable.Value)
                    ? HeldBackReason.Reclaimed
                    : (HeldBackReason?)null;

            if (reason is null) continue;

            // One cause per path, taken from the claim that condemned it. Where a
            // path's claims disagree the later ones are never asked, so preferring
            // a different cause would mean more property reads with the
            // machine-wide installer lease held. The cause named is true of the
            // file; that a second one also applied is not a defect.
            seen.Add(claim.LocalPackagePath);
            heldBack.Add(claim.LocalPackagePath);
            reasons = reasons.Plus(reason.Value);
        }

        // THE PER-PRODUCT CONDITION, RE-READ BY KEY. Everything above re-asks about the
        // batch's own pairings; this re-asks about the OTHER patches on the products
        // those pairings name, which is the fact the offer actually rests on. Without it
        // a sibling patch turning removable between the pre-lease enumeration and this
        // moment would go unseen.
        //
        // Keyed reads and never an enumeration, which is what makes it affordable under
        // the machine-wide lease: the pre-lease pass already worked out exactly which
        // pairings to look at, so this asks about records by name and each answer is
        // about the record named or says there is no such record.
        //
        // ONE PRODUCT'S FAILURE CONDEMNS EVERY BATCH PATH ON THAT PRODUCT, which is the
        // shape of the condition rather than a shortcut: the patch's one cached file is
        // shared by every product holding it, so a rollback on any of them reaches for
        // it.
        var productsAlreadyJudged = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var sibling in siblingClaims)
        {
            // A product settled by an earlier sibling needs no second look. The verdict
            // is one-way, so re-asking could only cost reads while the lease is held.
            if (!productsAlreadyJudged.Add(sibling.ProductCode)) continue;

            foreach (var onThisProduct in siblingClaims)
            {
                if (!string.Equals(onThisProduct.ProductCode, sibling.ProductCode,
                        StringComparison.OrdinalIgnoreCase))
                    continue;

                var siblingContext = (Interop.MsiInstallContext)onThisProduct.Context;
                var siblingUninstallable = InstallerQueryService.GetPatchProperty(
                    _msi, onThisProduct.PatchCode, onThisProduct.ProductCode,
                    onThisProduct.UserSid, siblingContext,
                    Interop.MsiInstallProperty.Uninstallable);

                // A POSITIVE ZERO IS THE ONLY CLEAN ANSWER, exactly as the scan's own
                // reading of this has it. A record that is no longer registered is not a
                // patch that can be rolled back and does not condemn; anything else,
                // including a read that failed and a value that is absent, does.
                if (siblingUninstallable.NotRegistered) continue;
                if (!siblingUninstallable.Unreadable && siblingUninstallable.Value == "0") continue;

                // Every batch path registered to this product goes, with the cause the
                // read supports: a read that failed is an inability, and a patch that
                // answered something other than zero is a live claim on the rollback.
                var siblingReason = siblingUninstallable.Unreadable
                    ? HeldBackReason.RecordsUnreadable
                    : HeldBackReason.Reclaimed;

                foreach (var batchClaim in claims)
                {
                    if (!string.Equals(batchClaim.ProductCode, sibling.ProductCode,
                            StringComparison.OrdinalIgnoreCase))
                        continue;
                    if (!seen.Add(batchClaim.LocalPackagePath)) continue;

                    heldBack.Add(batchClaim.LocalPackagePath);
                    reasons = reasons.Plus(siblingReason);
                }

                break;
            }
        }

        return new UnderLeaseRecheck(heldBack.AsReadOnly(), reasons);
    }
}
