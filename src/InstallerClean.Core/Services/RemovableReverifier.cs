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

        // The set of paths a currently NON-removable registered package claims. A
        // reverted patch (Superseded -> Applied) reappears here; a still-superseded
        // patch is IsRemovable and stays out; a true orphan was never registered.
        //
        // A re-verify whose own enumeration was incomplete inherits the withheld
        // removable class from the query, so every superseded candidate lands in
        // this set and is dropped. That is the intended direction (the check that
        // cannot confirm keeps the file), and it is why the result reports WHY it
        // dropped: the drop no longer implies a program reclaimed the file.
        var nonRemovable = new HashSet<string>(
            query.Packages.Where(p => !p.IsRemovable).Select(p => p.LocalPackagePath),
            StringComparer.OrdinalIgnoreCase);

        var surviving = new List<string>(candidatePaths.Count);
        var dropped = new List<string>();
        foreach (var path in candidatePaths)
        {
            if (nonRemovable.Contains(path))
                dropped.Add(path);
            else
                surviving.Add(path);
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

        return new ReverifyResult(surviving.AsReadOnly(), dropped.AsReadOnly(), query.RecordsIncomplete,
            survivingClaims.AsReadOnly());
    }

    /// <inheritdoc />
    public UnderLeaseRecheck RecheckUnderLease(IReadOnlyList<PatchClaim> claims)
    {
        if (claims.Count == 0) return new UnderLeaseRecheck(Array.Empty<string>());

        var reclaimed = new List<string>();
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var recordsIncomplete = false;

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

            // A pairing the records positively no longer hold is not a pairing
            // that reclaims anything, and it is the one answer here that must not
            // be folded into either of the other two. A claim says "patch P,
            // applied to product Q, holds this file". If the API answers that Q is
            // not installed or that P is not applied to it, the claim itself has
            // gone; it cannot have reverted to Applied, because there is no
            // registration left to be in any state at all. So it condemns nothing
            // and the path's fate rests on its other claims, which is exactly what
            // the pre-lease pass would have concluded, that enumeration listing no
            // product to claim the path in the first place. Folding it into
            // "unreadable" told the user the records could not be read when they
            // were read perfectly, and folding it into "a program wants it again"
            // would name the opposite of what happened.
            //
            // Fail closed on a mixed answer: only a pairing where nothing failed
            // AND something positively said the record is absent takes this route.
            // The two reads are of one pairing an instant apart, so a mix is a
            // race, and a race resolves to the cautious side.
            var notRegistered = state.NotRegistered || uninstallable.NotRegistered;
            var unreadable = (state.Unreadable && !state.NotRegistered)
                || (uninstallable.Unreadable && !uninstallable.NotRegistered);
            if (notRegistered && !unreadable) continue;

            // A read that failed is not an answer. It has not shown the file to be
            // removable, this is the last check standing in front of a permanent
            // delete, and the scan's own rule fails the same way for the same
            // reason. Note the asymmetry with the pre-lease pass, which is not an
            // inconsistency: that one inherits a whole enumeration's withholding
            // and reports it, where this one is judging a single named pairing and
            // a failure here is about that pairing alone.
            if (unreadable ||
                !InstallerQueryService.IsRemovablePatch(state.Value, uninstallable.Value))
            {
                if (unreadable) recordsIncomplete = true;
                seen.Add(claim.LocalPackagePath);
                reclaimed.Add(claim.LocalPackagePath);
            }
        }

        return new UnderLeaseRecheck(reclaimed.AsReadOnly(), recordsIncomplete);
    }
}
