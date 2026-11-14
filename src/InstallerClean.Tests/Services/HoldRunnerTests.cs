using InstallerClean.Services;
using InstallerClean.Tests.Helpers;
using Xunit;

namespace InstallerClean.Tests.Services;

public class HoldRunnerTests
{
    private static readonly string[] Files = { "a", "b", "c", "d" };

    [Fact]
    public void The_budget_closes_a_group_between_files_and_the_next_group_takes_a_new_hold()
    {
        var api = new FakeInstallerTransactionApi(0, 0, 0, 0);
        var now = TimeSpan.Zero;
        var runner = HoldRunnerFixture.Runner(api, new FakeRecordsFingerprint(),
            limits: HoldRunnerLimits.Default with { GroupBudget = TimeSpan.FromSeconds(1) },
            clock: () => now);
        var groups = new List<int>();

        var result = runner.Run(Files, FakeRecordsFingerprint.Reading("A"),
            _ => { groups.Add(api.Begins); now += TimeSpan.FromMilliseconds(600); },
            (_, _, _) => { }, CancellationToken.None);

        Assert.Equal(HoldRunEnd.Finished, result.End);
        Assert.Equal(4, result.Reached);
        Assert.Equal(new[] { 1, 1, 2, 2 }, groups);
        Assert.Equal(2, api.Commits);
        Assert.Equal(0, api.StrayEnds);
    }

    [Fact]
    public void A_group_never_holds_more_files_than_its_limit()
    {
        var api = new FakeInstallerTransactionApi(0, 0);
        var runner = HoldRunnerFixture.Runner(api, new FakeRecordsFingerprint(),
            limits: HoldRunnerLimits.Default with { GroupFiles = 3 });

        runner.Run(Files, FakeRecordsFingerprint.Reading("A"), _ => { }, (_, _, _) => { }, CancellationToken.None);

        Assert.Equal(2, api.Begins);
    }

    [Fact]
    public void Readings_that_keep_changing_hold_the_rest_back_after_the_last_re_check()
    {
        var api = new FakeInstallerTransactionApi(0, 0, 0, 0, 0);
        var fingerprint = new FakeRecordsFingerprint { Digest = "moving" };
        var reverifier = new FakeRecheckingReverifier(newReading: "still moving");
        var acted = new List<string>();
        var runner = HoldRunnerFixture.Runner(api, fingerprint, reverifier);

        var result = runner.Run(Files, FakeRecordsFingerprint.Reading("A"), acted.Add, (_, _, _) => { },
            CancellationToken.None);

        Assert.Empty(acted);
        Assert.Equal(Files, result.HeldBack);
        Assert.Equal(4, result.Reasons.RecordsChanged);
        Assert.Equal(HoldRunnerLimits.Default.Rechecks, reverifier.Calls.Count);
        Assert.Equal(api.Begins, api.Commits);
    }

    [Fact]
    public void A_reading_that_could_not_be_completed_holds_files_back_as_unreadable()
    {
        var api = new FakeInstallerTransactionApi(0, 0, 0, 0, 0);
        var fingerprint = new FakeRecordsFingerprint { Failure = "the walk stopped" };
        var runner = HoldRunnerFixture.Runner(api, fingerprint, new FakeRecheckingReverifier());

        var result = runner.Run(Files, FakeRecordsFingerprint.Reading("A"), _ => { }, (_, _, _) => { },
            CancellationToken.None);

        Assert.Equal(4, result.Reasons.RecordsUnreadable);
        Assert.Equal(0, result.Reached);
    }

    [Fact]
    public void A_cancel_inside_a_group_ends_the_hold_once_and_reports_what_was_done()
    {
        var api = new FakeInstallerTransactionApi(0);
        var runner = HoldRunnerFixture.Runner(api, new FakeRecordsFingerprint());
        using var cancel = new CancellationTokenSource();
        var reported = new List<string>();

        Assert.Throws<OperationCanceledException>(() => runner.Run(Files, FakeRecordsFingerprint.Reading("A"),
            path => { if (path == "b") cancel.Cancel(); },
            (_, _, path) => reported.Add(path), cancel.Token));

        Assert.Equal(new[] { "a", "b" }, reported);
        Assert.Equal(1, api.Commits);
        Assert.False(api.Open);
    }

    [Fact]
    public void Busy_before_the_first_file_gives_up_after_the_wait()
    {
        var api = new FakeInstallerTransactionApi(Enumerable.Repeat(1618u, 1000).ToArray());
        var runner = HoldRunnerFixture.Runner(api, new FakeRecordsFingerprint(),
            limits: HoldRunnerLimits.Default with { BusyWait = TimeSpan.FromSeconds(10) });

        var result = runner.Run(Files, FakeRecordsFingerprint.Reading("A"), _ => { }, (_, _, _) => { },
            CancellationToken.None);

        Assert.Equal(HoldRunEnd.InstallerBusy, result.End);
        Assert.Equal(0, result.Reached);
        Assert.InRange(api.Begins, 10, 12);
    }

    [Fact]
    public void Busy_after_a_file_is_waited_out_rather_than_leaving_the_batch_part_done()
    {
        var codes = new List<uint> { 0 };
        codes.AddRange(Enumerable.Repeat(1618u, 300));
        codes.Add(0);
        var api = new FakeInstallerTransactionApi(codes.ToArray());
        var runner = HoldRunnerFixture.Runner(api, new FakeRecordsFingerprint(),
            limits: HoldRunnerLimits.Default with { GroupFiles = 2, BusyWait = TimeSpan.FromSeconds(10) });

        var result = runner.Run(Files, FakeRecordsFingerprint.Reading("A"), _ => { }, (_, _, _) => { },
            CancellationToken.None);

        Assert.Equal(HoldRunEnd.Finished, result.End);
        Assert.Equal(4, result.Reached);
    }

    [Fact]
    public void A_failed_end_stops_the_run_and_takes_no_further_hold()
    {
        var api = new FakeInstallerTransactionApi(0, 0) { CommitCode = 5 };
        var runner = HoldRunnerFixture.Runner(api, new FakeRecordsFingerprint(),
            limits: HoldRunnerLimits.Default with { GroupFiles = 2 });

        var result = runner.Run(Files, FakeRecordsFingerprint.Reading("A"), _ => { }, (_, _, _) => { },
            CancellationToken.None);

        Assert.Equal(HoldRunEnd.EndFailed, result.End);
        Assert.Equal(2, result.Reached);
        Assert.Equal(1, api.Begins);
    }
}
