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
        // reclaiming the file would name a cause that did not occur. The withheld
        // kind was the third and cannot occur now, no verdict being granted to
        // withhold; the arm stays because the flag does. Both kinds can be in one
        // batch, which is why the cause is carried per path and not per run.
        //
        // EVERY REGISTERED PATH IS IN THIS MAP FROM 3.0.0, no row being removable,
        // which is strictly stricter than what stood here before: a candidate the
        // scan offered and a registration has since come to name is now dropped
        // with a cause, where the superseded class used to be exempt from the
        // comparison by construction.
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

        return new ReverifyResult(surviving.AsReadOnly(), dropped.AsReadOnly(), reasons,
            survivingClaims.AsReadOnly());
    }

    /// <inheritdoc />
    /// <remarks>
    /// IT RETURNS AT ITS FIRST LINE FROM 3.0.0 AND IS NOT DEAD WEIGHT. Its input
    /// is the patch claims naming a SURVIVING candidate, and no surviving
    /// candidate is named by any registration now, so the list is always empty.
    /// This is the last check standing in front of a permanent delete and it is
    /// what the superseded class would need again; deleting it is a decision about
    /// the product, not a tidy-up. Do not read the empty list as evidence that the
    /// re-read it performs was unnecessary.
    /// </remarks>
    public UnderLeaseRecheck RecheckUnderLease(IReadOnlyList<PatchClaim> claims)
    {
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

        return new UnderLeaseRecheck(heldBack.AsReadOnly(), reasons);
    }
}
