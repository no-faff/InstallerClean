using InstallerClean.Models;
using InstallerClean.Services;
using InstallerClean.Tests.Services;

namespace InstallerClean.Tests.Helpers;

/// <summary>A reading that is whatever the test says it is now.</summary>
internal sealed class FakeRecordsFingerprint : IInstallerRecordsFingerprint
{
    public string Digest { get; set; } = "A";
    public string? Failure { get; set; }
    public int Taken { get; private set; }

    /// <summary>Run at each reading, so a test can see what the machine looked like then.</summary>
    public Action? OnTake { get; set; }

    public InstallerRecordsPrint Take()
    {
        Taken++;
        OnTake?.Invoke();
        return new InstallerRecordsPrint(Digest, Failure);
    }

    public static InstallerRecordsPrint Reading(string digest) => new(digest, null);
}

/// <summary>
/// A re-check that confirms every path except the ones it is told to drop, and hands back
/// the reading the test sets as the new baseline.
/// </summary>
internal sealed class FakeRecheckingReverifier : IRemovableReverifier
{
    private readonly HashSet<string> _drop;

    public FakeRecheckingReverifier(IEnumerable<string>? drop = null, string newReading = "B")
    {
        _drop = new HashSet<string>(drop ?? Array.Empty<string>(), StringComparer.OrdinalIgnoreCase);
        NewReading = newReading;
    }

    public string NewReading { get; set; }
    public PendingRebootResult? Activity { get; set; }
    public List<IReadOnlyList<string>> Calls { get; } = new();

    public Task<ReverifyResult> ReverifyAsync(
        IReadOnlyList<string> candidatePaths,
        CancellationToken cancellationToken = default,
        IProgress<ScanProgressUpdate>? progress = null)
    {
        Calls.Add(candidatePaths.ToList());
        var dropped = candidatePaths.Where(_drop.Contains).ToList();
        var surviving = candidatePaths.Where(p => !_drop.Contains(p)).ToList();
        return Task.FromResult(new ReverifyResult(surviving, dropped,
            new HeldBackReasons(Reclaimed: dropped.Count), InstallerActivity: Activity)
        {
            Print = FakeRecordsFingerprint.Reading(NewReading),
        });
    }

    public UnderLeaseRecheck RecheckUnderLease(UnderLeaseClaims claims) =>
        new(Array.Empty<string>());
}

/// <summary>Calls the handler on the reporting thread, as the command line's progress does.</summary>
internal sealed class RecordingProgress<T>(Action<T> handler) : IProgress<T>
{
    public void Report(T value) => handler(value);
}

internal static class HoldRunnerFixture
{
    /// <summary>
    /// A runner over fakes whose clock advances only when the test says, and whose pause
    /// returns at once, so a wait costs a test no time.
    /// </summary>
    public static HoldRunner Runner(FakeInstallerTransactionApi api, IInstallerRecordsFingerprint fingerprint,
        IRemovableReverifier? reverifier = null, HoldRunnerLimits? limits = null, Func<TimeSpan>? clock = null)
    {
        var now = TimeSpan.Zero;
        return new HoldRunner(api, fingerprint, reverifier ?? new FakeRecheckingReverifier(),
            limits ?? HoldRunnerLimits.Default,
            clock ?? (() => now),
            (wait, token) => { now += wait; return !token.IsCancellationRequested; });
    }

    /// <summary>Claims carrying the reading the batch was checked against.</summary>
    public static UnderLeaseClaims CheckedAgainst(string digest) =>
        UnderLeaseClaims.None with { Print = FakeRecordsFingerprint.Reading(digest) };
}
