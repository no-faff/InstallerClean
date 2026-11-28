using InstallerClean.Services;

namespace InstallerClean.Tests.Helpers;

/// <summary>Makes the results of an <see cref="IBatchCheck"/> for a test, which only that component makes in the app.</summary>
internal static class StandingChecks
{
    /// <summary>A check that stood over <paramref name="files"/>, with a watch that reads unchanged and no bound.</summary>
    public static StandingCheck Of(params string[] files) => Of((IReadOnlyList<string>)files);

    /// <summary>A check that stood over <paramref name="files"/>, with the watch, bound and result a test gives.</summary>
    public static StandingCheck Of(
        IReadOnlyList<string> files, IRecordsWatch? watch = null, TimeSpan? waitBound = null,
        ReverifyResult? result = null) =>
        new(result ?? new ReverifyResult(files, Array.Empty<string>()), watch ?? new FakeRecordsWatch(), waitBound);

    /// <summary>The answer of a batch check whose check stood, with <paramref name="result"/> as what it found.</summary>
    public static BatchCheckResult Stood(ReverifyResult result, IRecordsWatch? watch = null, TimeSpan? waitBound = null) =>
        new(new StandingCheck(result, watch ?? new FakeRecordsWatch(), waitBound), null, default);

    /// <summary>The answer of a batch check whose check stood over <paramref name="files"/> and dropped none.</summary>
    public static BatchCheckResult StoodOver(params string[] files) =>
        Stood(new ReverifyResult(files, Array.Empty<string>()));

    /// <summary>The answer of a batch check that ended with no check standing.</summary>
    public static BatchCheckResult Ended(BatchEndReason reason, PendingRebootResult? signals = null) =>
        new(null, new BatchEnd(reason, signals), default);
}

/// <summary>
/// An <see cref="IBatchCheck"/> that gives the answers a test lines up, in order, and
/// records what it was asked. With no answer left it stands over every file it is given
/// and drops none.
/// </summary>
internal sealed class ScriptedBatchCheck : IBatchCheck
{
    private readonly Queue<Func<IReadOnlyList<string>, BatchCheckResult>> _answers = new();

    /// <summary>The files each call was given, in order.</summary>
    public List<IReadOnlyList<string>> Calls { get; } = [];

    /// <summary>The bound each call was given, in order.</summary>
    public List<TimeSpan?> Bounds { get; } = [];

    /// <summary>The watch of each check that stood, in order, so a test can script its readings.</summary>
    public List<FakeRecordsWatch> Watches { get; } = [];

    /// <summary>What each watch this hands out does when it is made, for a test that needs a later look to read as not clean.</summary>
    public Action<FakeRecordsWatch, int>? OnWatch { get; set; }

    /// <summary>What each call reports before it answers. Null reports nothing.</summary>
    public IReadOnlyList<BatchCheckProgress>? Reports { get; set; }

    /// <summary>The counts each check that stands comes back with.</summary>
    public BatchCheckCounts Counts { get; set; }

    /// <summary>The next call stands, dropping <paramref name="dropped"/> as claimed again.</summary>
    public ScriptedBatchCheck ThenStands(params string[] dropped) => Then(files =>
    {
        var gone = new HashSet<string>(dropped, StringComparer.OrdinalIgnoreCase);
        return StandingOver(
            new ReverifyResult(
                files.Where(f => !gone.Contains(f)).ToList(), dropped,
                new HeldBackReasons(Reclaimed: dropped.Length)));
    });

    /// <summary>The next call stands with <paramref name="result"/> as what it found.</summary>
    public ScriptedBatchCheck ThenStandsWith(Func<IReadOnlyList<string>, ReverifyResult> result) =>
        Then(files => StandingOver(result(files)));

    /// <summary>The next call ends with no check standing.</summary>
    public ScriptedBatchCheck ThenEnds(BatchEndReason reason, PendingRebootResult? signals = null) =>
        Then(_ => StandingChecks.Ended(reason, signals));

    /// <summary>The next call throws.</summary>
    public ScriptedBatchCheck ThenThrows(Exception exception) => Then(_ => throw exception);

    public ScriptedBatchCheck Then(Func<IReadOnlyList<string>, BatchCheckResult> answer)
    {
        _answers.Enqueue(answer);
        return this;
    }

    public Task<BatchCheckResult> RunAsync(
        IReadOnlyList<string> filePaths,
        TimeSpan? waitBound,
        IProgress<BatchCheckProgress>? progress = null,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        Calls.Add(filePaths.ToList());
        Bounds.Add(waitBound);
        foreach (var report in Reports ?? []) progress?.Report(report);
        var answer = _answers.Count > 0
            ? _answers.Dequeue()
            : files => StandingOver(new ReverifyResult(files.ToList(), Array.Empty<string>()));
        return Task.FromResult(answer(filePaths));
    }

    private BatchCheckResult StandingOver(ReverifyResult result)
    {
        var watch = new FakeRecordsWatch();
        Watches.Add(watch);
        OnWatch?.Invoke(watch, Watches.Count);
        return new BatchCheckResult(new StandingCheck(result, watch, null), null, Counts);
    }
}

/// <summary>
/// Windows Installer's signals as a test lines them up: each read takes the next answer,
/// and reads clean once the answers are spent.
/// </summary>
internal sealed class ScriptedSignals : IPendingRebootService
{
    private readonly Queue<Func<PendingRebootResult>> _busy = new();
    private readonly Queue<Func<PendingRebootResult>> _all = new();

    /// <summary>How many times <see cref="CheckBusy"/> has been read.</summary>
    public int BusyReads { get; private set; }

    /// <summary>How many times <see cref="Check"/> has been read.</summary>
    public int Checks { get; private set; }

    /// <summary>The next <paramref name="times"/> reads of <see cref="CheckBusy"/> give <paramref name="result"/>.</summary>
    public ScriptedSignals Busy(PendingRebootResult result, int times = 1)
    {
        for (var i = 0; i < times; i++) _busy.Enqueue(() => result);
        return this;
    }

    /// <summary>The next <paramref name="times"/> reads of <see cref="CheckBusy"/> read clean.</summary>
    public ScriptedSignals BusyClean(int times = 1) => Busy(PendingRebootResult.Clean, times);

    /// <summary>The next read of <see cref="CheckBusy"/> throws.</summary>
    public ScriptedSignals BusyThrows(Exception exception)
    {
        _busy.Enqueue(() => throw exception);
        return this;
    }

    /// <summary>The next <paramref name="times"/> reads of <see cref="Check"/> give <paramref name="result"/>.</summary>
    public ScriptedSignals All(PendingRebootResult result, int times = 1)
    {
        for (var i = 0; i < times; i++) _all.Enqueue(() => result);
        return this;
    }

    public PendingRebootResult Check()
    {
        Checks++;
        return _all.Count > 0 ? _all.Dequeue()() : PendingRebootResult.Clean;
    }

    public PendingRebootResult CheckBusy()
    {
        BusyReads++;
        return _busy.Count > 0 ? _busy.Dequeue()() : PendingRebootResult.Clean;
    }
}
