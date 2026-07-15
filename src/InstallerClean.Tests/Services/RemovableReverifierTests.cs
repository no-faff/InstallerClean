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

    private static IInstallerQueryService Query(params RegisteredPackage[] pkgs)
    {
        var q = Substitute.For<IInstallerQueryService>();
        q.GetRegisteredPackagesAsync(Arg.Any<IProgress<ScanProgressUpdate>?>(), Arg.Any<CancellationToken>())
            .Returns(pkgs.ToList().AsReadOnly());
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
    public async Task An_enumeration_failure_propagates_rather_than_passing_the_batch()
    {
        var q = Substitute.For<IInstallerQueryService>();
        q.GetRegisteredPackagesAsync(Arg.Any<IProgress<ScanProgressUpdate>?>(), Arg.Any<CancellationToken>())
            .Returns<IReadOnlyList<RegisteredPackage>>(_ => throw new LocalisedInvalidOperationException("boom"));
        var svc = new RemovableReverifier(q);

        await Assert.ThrowsAsync<LocalisedInvalidOperationException>(
            () => svc.ReverifyAsync(new[] { @"C:\Windows\Installer\x.msi" }));
    }
}
