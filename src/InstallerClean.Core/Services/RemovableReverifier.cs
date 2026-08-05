using InstallerClean.Models;

namespace InstallerClean.Services;

/// <summary>
/// Default <see cref="IRemovableReverifier"/>: re-runs
/// <see cref="IInstallerQueryService.GetRegisteredPackagesAsync"/> and drops any
/// candidate whose path a currently-registered, non-removable package claims.
/// Testable through the same <c>IMsiApi</c> seam the query service uses.
///
/// The full re-enumeration is the cost of the answer, not an oversight to be
/// optimised into a per-candidate query later. Most candidates are orphans, and
/// an orphan's verdict is the ABSENCE of any claim on its path: there is no
/// registration to re-read, because the reason the file is a candidate is that
/// no registration names it. Only walking the whole registered set again can
/// re-establish an absence. A per-candidate form could re-read the superseded
/// and obsoleted rows, which do have an identity to query, and would silently
/// answer nothing at all for every orphan, while still reporting itself as a
/// re-verification.
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
        // whether that row's verdict was WITHHELD rather than read. A reverted
        // patch (Superseded -> Applied) appears here unwithheld; a still-superseded
        // patch is IsRemovable and does not appear at all; a true orphan was never
        // registered.
        //
        // A re-verify whose own enumeration was incomplete inherits the withheld
        // removable class from the query, so every superseded candidate lands here
        // too. That is the intended direction (the check that cannot confirm keeps
        // the file), and the withheld flag is what stops the drop being reported as
        // a program reclaiming the file. Both kinds can be in one batch, which is
        // why the cause is carried per path and not per run.
        //
        // A dictionary rather than a set because InstallerQueryResult.Packages is
        // one row per claimed path, so there is a single answer to record for each.
        var nonRemovable = new Dictionary<string, bool>(StringComparer.OrdinalIgnoreCase);
        foreach (var pkg in query.Packages)
            if (!pkg.IsRemovable)
                nonRemovable[pkg.LocalPackagePath] = pkg.RemovableWithheld;

        var surviving = new List<string>(candidatePaths.Count);
        var dropped = new List<string>();
        var reasons = default(HeldBackReasons);
        foreach (var path in candidatePaths)
        {
            if (nonRemovable.TryGetValue(path, out var withheld))
            {
                dropped.Add(path);
                // Withheld means the enumeration never established what this patch
                // was, so nothing here shows a program wants the file; unwithheld
                // means a registered package positively claims it.
                reasons = reasons.Plus(withheld
                    ? HeldBackReason.RecordsUnreadable
                    : HeldBackReason.Reclaimed);
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
            // rule fails the same way. Note the asymmetry with the pre-lease pass,
            // which is not an inconsistency: that one inherits a whole enumeration's
            // withholding, where this one judges a single named pairing and a
            // failure here is about that pairing alone.
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
