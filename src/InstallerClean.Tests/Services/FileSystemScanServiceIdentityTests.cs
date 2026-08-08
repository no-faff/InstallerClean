using System.IO.Abstractions.TestingHelpers;
using InstallerClean.Models;
using InstallerClean.Services;
using NSubstitute;

namespace InstallerClean.Tests.Services;

/// <summary>
/// How <see cref="FileSystemScanService"/> uses the identity veto: which branch
/// it applies to, what it does to the offer, what it does NOT do to the two
/// sanity gates, and where its counts end up.
///
/// The veto itself is faked here. Its own rules are pinned in
/// <see cref="IdentityVetoTests"/>; what these are about is the wiring, and the
/// wiring is where the two mistakes with real consequences live. One would
/// withhold the superseded-patch class on every machine for ever. The other would
/// turn the safest possible scan into a refusal.
/// </summary>
public class FileSystemScanServiceIdentityTests
{
    private const string CacheRoot = @"C:\Windows\Installer";
    private const string OrphanOne = @"C:\Windows\Installer\one.msi";
    private const string OrphanTwo = @"C:\Windows\Installer\two.msi";

    [Fact]
    public async Task A_kept_back_candidate_does_not_reach_the_offer()
    {
        var fs = Cache(OrphanOne, OrphanTwo);
        var veto = Veto((OrphanOne, CandidateIdentityOutcome.Claimed));

        var result = await Scan(fs, veto, Query());

        var offered = Assert.Single(result.RemovableFiles);
        Assert.Equal(OrphanTwo, offered.FullPath);
    }

    [Fact]
    public async Task The_surviving_candidates_keep_their_walk_order()
    {
        // The offer is built by appending survivors in the order the walk found
        // them, so a pass that keeps nothing back leaves the list exactly as it
        // was before the pass existed.
        var fs = Cache(OrphanOne, OrphanTwo);

        var result = await Scan(fs, Veto(), Query());

        Assert.Equal(new[] { OrphanOne, OrphanTwo },
            result.RemovableFiles.Select(f => f.FullPath).ToArray());
    }

    [Fact]
    public async Task The_veto_is_not_applied_to_superseded_patches()
    {
        // THE ONE THAT WOULD HAVE BEEN A PERMANENT REGRESSION. A superseded patch
        // is offered BECAUSE Windows positively said the patch is superseded and
        // no longer uninstallable, so it is a file Windows knows by construction.
        // Putting it through a check whose keeping condition is "Windows knows
        // this identity" would withhold the whole class on every machine.
        //
        // The veto here would keep back anything it was shown; the assertion is
        // that the patch was never shown to it.
        const string superseded = @"C:\Windows\Installer\old.msp";
        var fs = new MockFileSystem(new Dictionary<string, MockFileData>
        {
            [superseded] = new("x"),
        });
        var veto = new RecordingVeto(CandidateIdentityOutcome.Claimed);

        var result = await Scan(fs, veto, Query(
            new RegisteredPackage(superseded, "Product", "{P}", PatchState: 2, IsRemovable: true)));

        Assert.Single(result.RemovableFiles);
        Assert.True(result.RemovableFiles[0].IsRemovablePatch);
        Assert.Empty(veto.Seen);
    }

    [Fact]
    public async Task A_patch_candidate_is_screened_as_a_patch()
    {
        // The reader takes an entirely different route for the two kinds, and it
        // is told which by this flag rather than working it out.
        const string orphanPatch = @"C:\Windows\Installer\orphan.msp";
        var fs = Cache(OrphanOne, orphanPatch);
        var veto = new RecordingVeto(CandidateIdentityOutcome.Unclaimed);

        await Scan(fs, veto, Query());

        Assert.Equal(new[] { (OrphanOne, false), (orphanPatch, true) },
            veto.Seen.Select(c => (c.FullPath, c.IsPatch)).ToArray());
    }

    [Fact]
    public async Task A_scan_the_veto_emptied_is_not_a_failed_correlation()
    {
        // THE OTHER ONE. Both sanity gates read an empty offer as evidence that
        // the path comparison did not work. A comparison that worked perfectly
        // and a veto that then kept every candidate back also produce an empty
        // offer, and refusing that scan would turn the safest possible outcome
        // into an error message. The gates therefore count what the offer was
        // BEFORE the pass.
        //
        // The registered set is arranged so the correlation gate's other two
        // conditions both hold: one present registered package, and twenty
        // missing ones.
        var fs = new MockFileSystem(new Dictionary<string, MockFileData>
        {
            [OrphanOne] = new("x"),
            [@"C:\Windows\Installer\present.msi"] = new("x"),
        });
        var packages = new List<RegisteredPackage>
        {
            new(@"C:\Windows\Installer\present.msi", "Present", "{P}"),
        };
        for (var i = 0; i < 20; i++)
            packages.Add(new RegisteredPackage($@"C:\Windows\Installer\gone{i}.msi", "Gone", "{G}"));

        var veto = Veto((OrphanOne, CandidateIdentityOutcome.Claimed));

        var result = await Scan(fs, veto, Query(packages.ToArray()));

        Assert.Empty(result.RemovableFiles);
        Assert.Equal(1, result.IdentityClaimedCount);
    }

    [Fact]
    public async Task The_same_machine_without_the_veto_still_fails_the_correlation()
    {
        // The control for the test above, and the reason it means anything: with
        // the pass permitting, the identical machine state DOES trip the gate. So
        // the previous test shows the gate reading a pre-veto count, rather than
        // showing a gate that no longer fires at all.
        var fs = new MockFileSystem(new Dictionary<string, MockFileData>
        {
            [OrphanOne] = new("x"),
            [@"C:\Windows\Installer\present.msi"] = new("x"),
        });
        var packages = new List<RegisteredPackage>
        {
            new(@"C:\Windows\Installer\present.msi", "Present", "{P}"),
        };
        for (var i = 0; i < 20; i++)
            packages.Add(new RegisteredPackage($@"C:\Windows\Installer\gone{i}.msi", "Gone", "{G}"));

        await Assert.ThrowsAsync<LocalisedInvalidOperationException>(
            () => Scan(fs, Veto(), Query(packages.ToArray())));
    }

    [Fact]
    public async Task The_three_causes_reach_the_result_separately()
    {
        // Three counts and no total. They are three different things to have
        // found out and no sentence covers all three, so nothing sums them and
        // nothing may.
        var fs = Cache(OrphanOne, OrphanTwo, @"C:\Windows\Installer\three.msi",
            @"C:\Windows\Installer\four.msi");
        var veto = Veto(
            (OrphanOne, CandidateIdentityOutcome.Claimed),
            (OrphanTwo, CandidateIdentityOutcome.IdentityUnreadable),
            (@"C:\Windows\Installer\three.msi", CandidateIdentityOutcome.RecordsUnaskable),
            (@"C:\Windows\Installer\four.msi", CandidateIdentityOutcome.Unclaimed));

        var result = await Scan(fs, veto, Query());

        Assert.Equal(1, result.IdentityClaimedCount);
        Assert.Equal(1, result.IdentityUnreadableCount);
        Assert.Equal(1, result.IdentityUnaskableCount);
        Assert.Single(result.RemovableFiles);
    }

    [Fact]
    public async Task A_scan_with_nothing_to_screen_reports_no_cause_at_all()
    {
        // A count reported over an empty pass would put a cause in front of the
        // user that never occurred.
        var result = await Scan(new MockFileSystem(new Dictionary<string, MockFileData>
        {
            [@"C:\Windows\Installer\claimed.msi"] = new("x"),
        }), Veto(), Query(new RegisteredPackage(@"C:\Windows\Installer\claimed.msi", "P", "{P}")));

        Assert.Equal(0, result.IdentityClaimedCount);
        Assert.Equal(0, result.IdentityUnreadableCount);
        Assert.Equal(0, result.IdentityUnaskableCount);
    }

    // ---- Helpers ----

    private static MockFileSystem Cache(params string[] paths) =>
        new(paths.ToDictionary(p => p, _ => new MockFileData("x")));

    private static IInstallerQueryService Query(params RegisteredPackage[] packages)
    {
        var query = Substitute.For<IInstallerQueryService>();
        query.GetRegisteredPackagesAsync(Arg.Any<IProgress<ScanProgressUpdate>?>(), Arg.Any<CancellationToken>())
            .Returns(new InstallerQueryResult(packages.ToList().AsReadOnly()));
        return query;
    }

    private static Task<ScanResult> Scan(MockFileSystem fs, IIdentityVeto veto, IInstallerQueryService query) =>
        new FileSystemScanService(query, fs, veto, null, CacheRoot).ScanAsync();

    /// <summary>
    /// A veto with a scripted answer per path, permitting anything not scripted.
    /// </summary>
    private static IIdentityVeto Veto(params (string Path, CandidateIdentityOutcome Outcome)[] scripted)
    {
        var map = scripted.ToDictionary(s => s.Path, s => s.Outcome, StringComparer.OrdinalIgnoreCase);
        return new ScriptedVeto(map);
    }

    private sealed class ScriptedVeto(Dictionary<string, CandidateIdentityOutcome> scripted) : IIdentityVeto
    {
        public IdentityPassResult Screen(
            IReadOnlyList<IdentityCandidate> candidates,
            IProgress<ScanProgressUpdate>? progress = null,
            CancellationToken cancellationToken = default)
        {
            var outcomes = candidates
                .Select(c => scripted.GetValueOrDefault(c.FullPath, CandidateIdentityOutcome.Unclaimed))
                .ToList();
            return new IdentityPassResult(outcomes, outcomes.Count, 0);
        }
    }

    /// <summary>
    /// Records what it was asked about and answers the same way for everything,
    /// for the tests whose subject is WHICH candidates reach the pass.
    /// </summary>
    private sealed class RecordingVeto(CandidateIdentityOutcome answer) : IIdentityVeto
    {
        public List<IdentityCandidate> Seen { get; } = new();

        public IdentityPassResult Screen(
            IReadOnlyList<IdentityCandidate> candidates,
            IProgress<ScanProgressUpdate>? progress = null,
            CancellationToken cancellationToken = default)
        {
            Seen.AddRange(candidates);
            var outcomes = new CandidateIdentityOutcome[candidates.Count];
            Array.Fill(outcomes, answer);
            return new IdentityPassResult(outcomes, candidates.Count, 0);
        }
    }
}
