namespace InstallerClean.Services;

/// <summary>
/// Re-checks a set of removal candidates against the Windows Installer API
/// immediately before a Move or Delete acts on them, to catch the one window
/// neither the fresh pending-reboot gate nor the P1 mutex hold can see: a patch
/// whose state changed AND settled between the scan and the click (a superseded
/// patch reverted to Applied because its superseding patch was uninstalled).
///
/// It re-runs the full classifier (<see cref="IInstallerQueryService"/>) rather
/// than re-querying a single retained product code, because after the
/// shared-patch verdict merge a patch can revert to Applied for a DIFFERENT
/// product than the one whose code survived the merge; re-enumerating is correct
/// across every product for nothing but a few seconds spent before a rare,
/// destructive batch. True orphans (files the API never claimed) are never
/// dropped: they cannot reappear in the registered set.
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
}

/// <summary>
/// Result of a re-verify. <see cref="Surviving"/> + <see cref="Dropped"/> partition
/// the input: <see cref="Surviving"/> is still safe to act on, <see cref="Dropped"/>
/// is now claimed by a non-removable registered package and must be skipped.
/// </summary>
public record ReverifyResult(
    IReadOnlyList<string> Surviving,
    IReadOnlyList<string> Dropped);
