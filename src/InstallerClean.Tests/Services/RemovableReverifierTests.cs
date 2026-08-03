using InstallerClean.Models;
using InstallerClean.Services;
using NSubstitute;

namespace InstallerClean.Tests.Services;

/// <summary>
/// Unit tests for <see cref="RemovableReverifier"/>: the action-time re-check
/// that drops a candidate a currently-registered, non-removable package now
/// claims (a superseded patch reverted to Applied between scan and click). The
/// query service is faked; its own correctness is covered by
/// <see cref="InstallerQueryServiceUnitTests"/> through the same IMsiApi seam.
/// </summary>
public class RemovableReverifierTests
{
    private static RegisteredPackage NonRemovable(string path) =>
        new(path, "Product", "{00000000-0000-0000-0000-000000000001}");

    private static RegisteredPackage Removable(string path) =>
        new(path, "Product", "{00000000-0000-0000-0000-000000000001}", PatchState: 2, IsRemovable: true);

    /// <summary>A superseded patch on a query whose product enumeration was incomplete.</summary>
    private static RegisteredPackage Withheld(string path) =>
        new(path, "Product", "{00000000-0000-0000-0000-000000000001}",
            PatchState: 2, IsRemovable: false, RemovableWithheld: true);

    private static IInstallerQueryService Query(params RegisteredPackage[] pkgs)
    {
        var q = Substitute.For<IInstallerQueryService>();
        q.GetRegisteredPackagesAsync(Arg.Any<IProgress<ScanProgressUpdate>?>(), Arg.Any<CancellationToken>())
            .Returns(new InstallerQueryResult(pkgs.ToList().AsReadOnly()));
        return q;
    }

    [Fact]
    public async Task Drops_a_candidate_that_reverted_to_non_removable()
    {
        const string reverted = @"C:\Windows\Installer\reverted.msp";
        const string orphan = @"C:\Windows\Installer\orphan.msi";
        // At action time the reverted patch is now a registered, non-removable package.
        var svc = new RemovableReverifier(Query(NonRemovable(reverted)));

        var result = await svc.ReverifyAsync(new[] { reverted, orphan });

        Assert.Equal(new[] { orphan }, result.Surviving);
        Assert.Equal(new[] { reverted }, result.Dropped);
    }

    [Fact]
    public async Task Keeps_a_candidate_still_marked_removable()
    {
        const string superseded = @"C:\Windows\Installer\superseded.msp";
        // Still superseded => IsRemovable, so not in the non-removable set.
        var svc = new RemovableReverifier(Query(Removable(superseded)));

        var result = await svc.ReverifyAsync(new[] { superseded });

        Assert.Equal(new[] { superseded }, result.Surviving);
        Assert.Empty(result.Dropped);
    }

    [Fact]
    public async Task Drops_a_scan_time_orphan_a_new_registration_has_since_claimed()
    {
        // Same drop mechanism as the reverted-patch test above, pinned separately
        // because it stands for a different producer and the interface's contract
        // now names it. An install that cached its package before the folder walk
        // reached it and registered it after the query had passed leaves a file
        // the scan measured as an orphan and a program owns by action time. The
        // re-verify catching it is what keeps it out of a permanent delete.
        const string newlyClaimed = @"C:\Windows\Installer\just-installed.msi";
        var svc = new RemovableReverifier(Query(NonRemovable(newlyClaimed)));

        var result = await svc.ReverifyAsync(new[] { newlyClaimed });

        Assert.Empty(result.Surviving);
        Assert.Equal(new[] { newlyClaimed }, result.Dropped);
    }

    [Fact]
    public async Task Keeps_a_true_orphan_not_in_the_registered_set()
    {
        const string orphan = @"C:\Windows\Installer\orphan.msi";
        var svc = new RemovableReverifier(Query(NonRemovable(@"C:\Windows\Installer\unrelated.msi")));

        var result = await svc.ReverifyAsync(new[] { orphan });

        Assert.Equal(new[] { orphan }, result.Surviving);
        Assert.Empty(result.Dropped);
    }

    [Fact]
    public async Task Path_match_is_case_insensitive()
    {
        const string candidate = @"C:\Windows\Installer\Patch.msp";
        var svc = new RemovableReverifier(Query(NonRemovable(@"c:\windows\installer\patch.msp")));

        var result = await svc.ReverifyAsync(new[] { candidate });

        Assert.Empty(result.Surviving);
        Assert.Equal(new[] { candidate }, result.Dropped);
    }

    [Fact]
    public async Task Empty_candidate_list_returns_empty_without_querying()
    {
        var q = Substitute.For<IInstallerQueryService>();
        var svc = new RemovableReverifier(q);

        var result = await svc.ReverifyAsync(Array.Empty<string>());

        Assert.Empty(result.Surviving);
        Assert.Empty(result.Dropped);
        await q.DidNotReceive().GetRegisteredPackagesAsync(
            Arg.Any<IProgress<ScanProgressUpdate>?>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task A_degraded_reverify_keeps_a_superseded_candidate_back_and_says_why()
    {
        // The re-verify is the same query, so it inherits the withholding: a
        // patch it cannot judge is kept in place rather than acted on. It reports
        // WHY, because the only other reason it drops anything is a program
        // reclaiming the file, and that is not what happened here.
        const string superseded = @"C:\Windows\Installer\superseded.msp";
        var q = Substitute.For<IInstallerQueryService>();
        q.GetRegisteredPackagesAsync(Arg.Any<IProgress<ScanProgressUpdate>?>(), Arg.Any<CancellationToken>())
            .Returns(new InstallerQueryResult(
                new[] { Withheld(superseded) }, UnreadableProductCount: 1));
        var svc = new RemovableReverifier(q);

        var result = await svc.ReverifyAsync(new[] { superseded });

        Assert.Empty(result.Surviving);
        Assert.Equal(new[] { superseded }, result.Dropped);
        Assert.True(result.RecordsIncomplete);
    }

    [Fact]
    public async Task A_healthy_reverify_does_not_claim_the_records_were_short()
    {
        var svc = new RemovableReverifier(Query(NonRemovable(@"C:\Windows\Installer\reverted.msp")));

        var result = await svc.ReverifyAsync(new[] { @"C:\Windows\Installer\reverted.msp" });

        Assert.False(result.RecordsIncomplete);
    }

    [Fact]
    public async Task A_degraded_reverify_still_leaves_a_true_orphan_actionable()
    {
        // Withholding costs superseded-patch cleanup and nothing else: an orphan
        // was never in the registered set, so no enumeration failure can put it
        // there.
        const string orphan = @"C:\Windows\Installer\orphan.msi";
        var q = Substitute.For<IInstallerQueryService>();
        q.GetRegisteredPackagesAsync(Arg.Any<IProgress<ScanProgressUpdate>?>(), Arg.Any<CancellationToken>())
            .Returns(new InstallerQueryResult(
                Array.Empty<RegisteredPackage>(), UnreadableProductCount: 3));
        var svc = new RemovableReverifier(q);

        var result = await svc.ReverifyAsync(new[] { orphan });

        Assert.Equal(new[] { orphan }, result.Surviving);
        Assert.Empty(result.Dropped);
    }

    [Fact]
    public async Task An_enumeration_failure_propagates_rather_than_passing_the_batch()
    {
        var q = Substitute.For<IInstallerQueryService>();
        q.GetRegisteredPackagesAsync(Arg.Any<IProgress<ScanProgressUpdate>?>(), Arg.Any<CancellationToken>())
            .Returns<InstallerQueryResult>(_ => throw new LocalisedInvalidOperationException("boom"));
        var svc = new RemovableReverifier(q);

        await Assert.ThrowsAsync<LocalisedInvalidOperationException>(
            () => svc.ReverifyAsync(new[] { @"C:\Windows\Installer\x.msi" }));
    }
}
