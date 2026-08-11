using InstallerClean.Helpers;
using InstallerClean.Models;
using InstallerClean.Services;
using NSubstitute;

namespace InstallerClean.Tests.Services;

/// <summary>
/// The identity check at action time: the last thing between a candidate and a
/// permanent delete, and for some files the ONLY identity check there is.
///
/// That last part is the reason it exists and is easy to miss. The scan runs the
/// veto on the orphan branch alone, so a superseded or obsoleted patch is offered
/// without ever being screened. If its registration has gone by the time the
/// button is pressed, no registration names its path, so it arrives here as an
/// orphan and is screened for the first time.
///
/// <see cref="IdentityVetoTests"/> pins what the veto decides. These pin which
/// candidates reach it, what each verdict does to the batch, and which cause is
/// reported, because a right outcome under a wrong cause is a defect: the cause is
/// the whole of what the user is told.
/// </summary>
public class RemovableReverifierIdentityTests
{
    private const string Orphan = @"C:\Windows\Installer\orphan.msi";
    private const string OrphanPatch = @"C:\Windows\Installer\orphan.msp";
    private const string Superseded = @"C:\Windows\Installer\superseded.msp";

    private static RegisteredPackage Removable(string path) =>
        new(path, "Product", "{00000000-0000-0000-0000-000000000001}", PatchState: 2, IsRemovable: true);

    private static RegisteredPackage NonRemovable(string path) =>
        new(path, "Product", "{00000000-0000-0000-0000-000000000001}");

    private static IInstallerQueryService Query(params RegisteredPackage[] packages)
    {
        var q = Substitute.For<IInstallerQueryService>();
        q.GetRegisteredPackagesAsync(Arg.Any<IProgress<ScanProgressUpdate>?>(), Arg.Any<CancellationToken>())
            .Returns(new InstallerQueryResult(packages.ToList().AsReadOnly()));
        return q;
    }

    private static RemovableReverifier Reverifier(IInstallerQueryService query, IIdentityVeto veto) =>
        new(query, Substitute.For<InstallerClean.Interop.IMsiApi>(), veto);

    [Theory]
    [InlineData(CandidateIdentityOutcome.Claimed)]
    [InlineData(CandidateIdentityOutcome.IdentityUnreadable)]
    [InlineData(CandidateIdentityOutcome.RecordsUnaskable)]
    public async Task Drops_an_orphan_the_identity_check_keeps_back(CandidateIdentityOutcome outcome)
    {
        var svc = Reverifier(Query(), new ScriptedVeto(outcome));

        var result = await svc.ReverifyAsync(new[] { Orphan });

        Assert.Empty(result.Surviving);
        Assert.Equal(new[] { Orphan }, result.Dropped);
        // The invariant that makes under-reporting impossible: every dropped path
        // is counted against exactly one cause.
        Assert.Equal(result.Dropped.Count, result.Reasons.Total);
    }

    [Fact]
    public async Task Keeps_an_orphan_the_identity_check_clears()
    {
        var svc = Reverifier(Query(), new ScriptedVeto(CandidateIdentityOutcome.Unclaimed));

        var result = await svc.ReverifyAsync(new[] { Orphan });

        Assert.Equal(new[] { Orphan }, result.Surviving);
        Assert.Empty(result.Dropped);
        Assert.Equal(0, result.Reasons.Total);
    }

    [Fact]
    public async Task Each_verdict_is_reported_under_its_own_cause()
    {
        // Three orphans, three verdicts, three different sentences. A batch that
        // met all three and reported any one of them would name a cause that did
        // not occur for two thirds of its files.
        var veto = new PerPathVeto(new Dictionary<string, CandidateIdentityOutcome>
        {
            [Orphan] = CandidateIdentityOutcome.Claimed,
            [OrphanPatch] = CandidateIdentityOutcome.IdentityUnreadable,
            [@"C:\Windows\Installer\third.msi"] = CandidateIdentityOutcome.RecordsUnaskable,
        });
        var svc = Reverifier(Query(), veto);

        var result = await svc.ReverifyAsync(
            new[] { Orphan, OrphanPatch, @"C:\Windows\Installer\third.msi" });

        Assert.Equal(1, result.Reasons.IdentityClaimed);
        Assert.Equal(1, result.Reasons.IdentityUnreadable);
        // The unaskable state has no line of its own: it is a failure to read the
        // records, which is what this cause already says, so it counts there.
        Assert.Equal(1, result.Reasons.RecordsUnreadable);
        Assert.Equal(3, result.Reasons.Total);
        Assert.Equal(3, HeldBackReport.Lines(result.Reasons).Count);
    }

    [Fact]
    public async Task A_candidate_a_superseded_registration_names_is_dropped_before_the_identity_check()
    {
        // THE SCOPE RULE INVERTED, AND THAT IS THE 3.0.0 CHANGE. It used to be
        // that a superseded patch survived here and was never screened, because it
        // was offered on Windows positively saying so and a check whose keeping
        // condition is that Windows knows the identity would have withheld the
        // whole class on every machine. Nothing is offered on that verdict now, so
        // a candidate any registration names is dropped by the path comparison
        // before the identity check is reached, and the exemption has no subject.
        //
        // The veto here would keep back anything it was shown, so its empty log is
        // the assertion that the drop happened first rather than after it.
        var veto = new RecordingVeto(CandidateIdentityOutcome.Claimed);
        var svc = Reverifier(Query(NonRemovable(Superseded) with { PatchState = 2 }), veto);

        var result = await svc.ReverifyAsync(new[] { Superseded });

        Assert.Empty(result.Surviving);
        Assert.Equal(new[] { Superseded }, result.Dropped);
        Assert.Equal(1, result.Reasons.Reclaimed);
        Assert.Empty(veto.Seen);
    }

    [Fact]
    public async Task A_superseded_patch_whose_registration_has_gone_IS_screened()
    {
        // Break one, and the reason this check is worth more than a repeat of the
        // scan's. Nothing names the path any more, so it arrives as an orphan and
        // is screened for the first time in its life.
        var veto = new RecordingVeto(CandidateIdentityOutcome.IdentityUnreadable);
        var svc = Reverifier(Query(), veto);

        var result = await svc.ReverifyAsync(new[] { Superseded });

        Assert.Equal(new[] { Superseded }, result.Dropped);
        var seen = Assert.Single(veto.Seen);
        Assert.Equal(Superseded, seen.FullPath);
        Assert.True(seen.IsPatch);
    }

    [Fact]
    public async Task A_candidate_a_registration_now_claims_never_reaches_the_identity_check()
    {
        // The path comparison settles it first, and its cause is the stronger of
        // the two: a live claim NAMES the file, where the identity check could
        // only ever say a record exists under the name the file gives itself.
        var veto = new RecordingVeto(CandidateIdentityOutcome.Claimed);
        var svc = Reverifier(Query(NonRemovable(Orphan)), veto);

        var result = await svc.ReverifyAsync(new[] { Orphan });

        Assert.Equal(new[] { Orphan }, result.Dropped);
        Assert.Equal(1, result.Reasons.Reclaimed);
        Assert.Equal(0, result.Reasons.IdentityClaimed);
        Assert.Empty(veto.Seen);
    }

    [Fact]
    public async Task The_survivors_keep_their_order_when_one_in_the_middle_is_dropped()
    {
        // The drop walks the batch back to front so an index it has not used yet
        // cannot move under it. This is what would break silently if that changed.
        const string a = @"C:\Windows\Installer\a.msi";
        const string b = @"C:\Windows\Installer\b.msi";
        const string c = @"C:\Windows\Installer\c.msi";
        var veto = new PerPathVeto(new Dictionary<string, CandidateIdentityOutcome>
        {
            [b] = CandidateIdentityOutcome.Claimed,
        });
        var svc = Reverifier(Query(), veto);

        var result = await svc.ReverifyAsync(new[] { a, b, c });

        Assert.Equal(new[] { a, c }, result.Surviving);
        Assert.Equal(new[] { b }, result.Dropped);
    }

    [Fact]
    public async Task Both_kinds_of_drop_can_occur_in_one_batch_and_are_counted_apart()
    {
        // A batch really can meet a path-side cause and an identity-side cause at
        // once, which is what forces the block to partition rather than state one
        // cause with a total.
        var veto = new PerPathVeto(new Dictionary<string, CandidateIdentityOutcome>
        {
            [Orphan] = CandidateIdentityOutcome.Claimed,
        });
        var svc = Reverifier(Query(NonRemovable(Superseded)), veto);

        var result = await svc.ReverifyAsync(new[] { Superseded, Orphan });

        Assert.Equal(1, result.Reasons.Reclaimed);
        Assert.Equal(1, result.Reasons.IdentityClaimed);
        Assert.Equal(2, result.Reasons.Total);
        Assert.Equal(2, result.Dropped.Count);
        Assert.Empty(result.Surviving);
        // Two causes, two lines, in the fixed order the report owns.
        Assert.Equal(2, HeldBackReport.Lines(result.Reasons).Count);
    }

    [Fact]
    public async Task The_all_skipped_report_names_every_cause_it_dropped_for()
    {
        // The screen that reads "Nothing removed" over these lines. Before the two
        // new causes existed it rendered an empty summary, which said the opposite
        // of what had happened.
        var veto = new PerPathVeto(new Dictionary<string, CandidateIdentityOutcome>
        {
            [Orphan] = CandidateIdentityOutcome.Claimed,
            [OrphanPatch] = CandidateIdentityOutcome.IdentityUnreadable,
        });
        var svc = Reverifier(Query(), veto);

        var result = await svc.ReverifyAsync(new[] { Orphan, OrphanPatch });

        Assert.Empty(result.Surviving);
        var lines = HeldBackReport.Lines(result.Reasons);
        Assert.Equal(2, lines.Count);
        Assert.All(lines, l => Assert.False(string.IsNullOrWhiteSpace(l)));
    }

    private sealed class ScriptedVeto(CandidateIdentityOutcome outcome) : IIdentityVeto
    {
        public IdentityPassResult Screen(
            IReadOnlyList<IdentityCandidate> candidates,
            IProgress<ScanProgressUpdate>? progress = null,
            CancellationToken cancellationToken = default)
        {
            var outcomes = new CandidateIdentityOutcome[candidates.Count];
            Array.Fill(outcomes, outcome);
            return new IdentityPassResult(outcomes, candidates.Count, 0);
        }
    }

    private sealed class PerPathVeto(Dictionary<string, CandidateIdentityOutcome> scripted) : IIdentityVeto
    {
        public IdentityPassResult Screen(
            IReadOnlyList<IdentityCandidate> candidates,
            IProgress<ScanProgressUpdate>? progress = null,
            CancellationToken cancellationToken = default) =>
            new(candidates
                    .Select(c => scripted.GetValueOrDefault(c.FullPath, CandidateIdentityOutcome.Unclaimed))
                    .ToList(),
                candidates.Count, 0);
    }

    private sealed class RecordingVeto(CandidateIdentityOutcome outcome) : IIdentityVeto
    {
        public List<IdentityCandidate> Seen { get; } = new();

        public IdentityPassResult Screen(
            IReadOnlyList<IdentityCandidate> candidates,
            IProgress<ScanProgressUpdate>? progress = null,
            CancellationToken cancellationToken = default)
        {
            Seen.AddRange(candidates);
            var outcomes = new CandidateIdentityOutcome[candidates.Count];
            Array.Fill(outcomes, outcome);
            return new IdentityPassResult(outcomes, candidates.Count, 0);
        }
    }
}
