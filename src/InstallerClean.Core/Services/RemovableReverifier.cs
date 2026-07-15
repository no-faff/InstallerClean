using InstallerClean.Models;

namespace InstallerClean.Services;

/// <summary>
/// Default <see cref="IRemovableReverifier"/>: re-runs
/// <see cref="IInstallerQueryService.GetRegisteredPackagesAsync"/> and drops any
/// candidate whose path a currently-registered, non-removable package claims.
/// Testable through the same <c>IMsiApi</c> seam the query service uses.
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
        var registered = await _queryService.GetRegisteredPackagesAsync(null, cancellationToken)
            .ConfigureAwait(false);

        // The set of paths a currently NON-removable registered package claims. A
        // reverted patch (Superseded -> Applied) reappears here; a still-superseded
        // patch is IsRemovable and stays out; a true orphan was never registered.
        var nonRemovable = new HashSet<string>(
            registered.Where(p => !p.IsRemovable).Select(p => p.LocalPackagePath),
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

        return new ReverifyResult(surviving.AsReadOnly(), dropped.AsReadOnly());
    }
}
