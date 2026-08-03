using InstallerClean.Models;
using InstallerClean.Services;

namespace InstallerClean.Tests.Helpers;

/// <summary>
/// Hand fake for the under-lease half of <see cref="IRemovableReverifier"/>, so
/// an action service's behaviour when a claim has moved can be driven without a
/// Windows Installer database.
///
/// It also records what the mutex probe had done by the time it was called,
/// which is the only way to pin the property the whole check rests on: the
/// re-read is worth nothing unless it happens INSIDE the hold, and a service
/// that took it afterwards would pass every other assertion here.
/// </summary>
internal sealed class FakeReclaimingReverifier : IRemovableReverifier
{
    private readonly IReadOnlyList<string> _reclaim;
    private readonly bool _recordsIncomplete;
    private readonly FakeMutexProbe? _watching;
    private readonly Action? _atRecheck;

    /// <param name="atRecheck">
    /// Runs at the instant the re-read is entered, and exists because the lease
    /// counters alone cannot pin the other half of what the re-read promises: not
    /// only that it runs inside the hold, but that it runs before a single file
    /// has been touched. A test observes the filesystem through this, or throws
    /// from it to drive the failure path and pin that the lease is released
    /// anyway.
    /// </param>
    public FakeReclaimingReverifier(IReadOnlyList<string> reclaim, FakeMutexProbe? watching = null,
        bool recordsIncomplete = false, Action? atRecheck = null)
    {
        _reclaim = reclaim;
        _watching = watching;
        _recordsIncomplete = recordsIncomplete;
        _atRecheck = atRecheck;
    }

    /// <summary>Claims the service handed over, so a test can pin what it passed on.</summary>
    public IReadOnlyList<PatchClaim>? ClaimsSeen { get; private set; }

    /// <summary>Leases the probe had granted when this was called; null if never called.</summary>
    public int? LeasesHeldWhenCalled { get; private set; }

    /// <summary>Leases the probe had released when this was called.</summary>
    public int? LeasesReleasedWhenCalled { get; private set; }

    public Task<ReverifyResult> ReverifyAsync(
        IReadOnlyList<string> candidatePaths,
        CancellationToken cancellationToken = default) =>
        throw new NotSupportedException("This fake stands in for the under-lease half only.");

    public UnderLeaseRecheck RecheckUnderLease(IReadOnlyList<PatchClaim> claims)
    {
        ClaimsSeen = claims;
        LeasesHeldWhenCalled = _watching?.Acquired;
        LeasesReleasedWhenCalled = _watching?.Released;
        _atRecheck?.Invoke();
        return new UnderLeaseRecheck(_reclaim, _recordsIncomplete);
    }
}
