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

    public RemovableReverifier(IInstallerQueryService queryService) => _queryService = queryService;

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

        return new ReverifyResult(surviving.AsReadOnly(), dropped.AsReadOnly(), query.RecordsIncomplete);
    }
}
