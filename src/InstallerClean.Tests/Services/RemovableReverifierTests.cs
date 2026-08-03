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

    /// <summary>
    /// The pre-lease pass only. Its <c>IMsiApi</c> is a bare substitute because
    /// nothing on this path touches it: the under-lease re-read is the only
    /// caller, and it has tests of its own below that drive the API deliberately.
    /// </summary>
    private static RemovableReverifier Reverifier(IInstallerQueryService query) =>
        new(query, Substitute.For<InstallerClean.Interop.IMsiApi>());

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
        var svc = Reverifier(Query(NonRemovable(reverted)));

        var result = await svc.ReverifyAsync(new[] { reverted, orphan });

        Assert.Equal(new[] { orphan }, result.Surviving);
        Assert.Equal(new[] { reverted }, result.Dropped);
    }

    [Fact]
    public async Task Keeps_a_candidate_still_marked_removable()
    {
        const string superseded = @"C:\Windows\Installer\superseded.msp";
        // Still superseded => IsRemovable, so not in the non-removable set.
        var svc = Reverifier(Query(Removable(superseded)));

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
        var svc = Reverifier(Query(NonRemovable(newlyClaimed)));

        var result = await svc.ReverifyAsync(new[] { newlyClaimed });

        Assert.Empty(result.Surviving);
        Assert.Equal(new[] { newlyClaimed }, result.Dropped);
    }

    [Fact]
    public async Task Keeps_a_true_orphan_not_in_the_registered_set()
    {
        const string orphan = @"C:\Windows\Installer\orphan.msi";
        var svc = Reverifier(Query(NonRemovable(@"C:\Windows\Installer\unrelated.msi")));

        var result = await svc.ReverifyAsync(new[] { orphan });

        Assert.Equal(new[] { orphan }, result.Surviving);
        Assert.Empty(result.Dropped);
    }

    [Fact]
    public async Task Path_match_is_case_insensitive()
    {
        const string candidate = @"C:\Windows\Installer\Patch.msp";
        var svc = Reverifier(Query(NonRemovable(@"c:\windows\installer\patch.msp")));

        var result = await svc.ReverifyAsync(new[] { candidate });

        Assert.Empty(result.Surviving);
        Assert.Equal(new[] { candidate }, result.Dropped);
    }

    [Fact]
    public async Task Empty_candidate_list_returns_empty_without_querying()
    {
        var q = Substitute.For<IInstallerQueryService>();
        var svc = Reverifier(q);

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
        var svc = Reverifier(q);

        var result = await svc.ReverifyAsync(new[] { superseded });

        Assert.Empty(result.Surviving);
        Assert.Equal(new[] { superseded }, result.Dropped);
        Assert.True(result.RecordsIncomplete);
    }

    [Fact]
    public async Task A_healthy_reverify_does_not_claim_the_records_were_short()
    {
        var svc = Reverifier(Query(NonRemovable(@"C:\Windows\Installer\reverted.msp")));

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
        var svc = Reverifier(q);

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
        var svc = Reverifier(q);

        await Assert.ThrowsAsync<LocalisedInvalidOperationException>(
            () => svc.ReverifyAsync(new[] { @"C:\Windows\Installer\x.msi" }));
    }

    // ---- The under-lease re-read ----------------------------------------
    //
    // The pre-lease pass above answers with a whole enumeration. This one
    // answers about named claims, because it runs while the machine-wide
    // installer mutex is held and an enumeration must not.

    private const string PatchA = "{AAAAAAAA-0000-0000-0000-000000000001}";
    private const string PatchB = "{BBBBBBBB-0000-0000-0000-000000000002}";
    private const string ProductOne = "{11111111-0000-0000-0000-000000000001}";
    private const string ProductTwo = "{22222222-0000-0000-0000-000000000002}";

    private static PatchClaim Claim(string path, string patchCode, string productCode) =>
        new(path, patchCode, productCode, null, 4);

    [Fact]
    public void An_empty_claim_list_answers_without_touching_the_api()
    {
        var msi = new ScriptedPatchApi();
        var svc = new RemovableReverifier(Substitute.For<IInstallerQueryService>(), msi);

        Assert.Empty(svc.RecheckUnderLease(Array.Empty<PatchClaim>()).HeldBack);
        // The ordinary batch is true orphans, which carry no claim at all, so the
        // machine-wide lock is held over no API call whatsoever on most runs.
        Assert.Equal(0, msi.Reads);
    }

    [Fact]
    public void A_claim_still_superseded_and_not_uninstallable_is_left_alone()
    {
        const string path = @"C:\Windows\Installer\superseded.msp";
        var msi = new ScriptedPatchApi();
        msi.Set(PatchA, ProductOne, state: "2", uninstallable: "0");
        var svc = new RemovableReverifier(Substitute.For<IInstallerQueryService>(), msi);

        Assert.Empty(svc.RecheckUnderLease(new[] { Claim(path, PatchA, ProductOne) }).HeldBack);
    }

    [Fact]
    public void A_claim_that_reverted_to_applied_reclaims_its_path()
    {
        // The producer the whole check exists for: the superseding patch was
        // uninstalled after the caller's enumeration read this one as Superseded,
        // so it is Applied again and its cached .msp is needed.
        const string path = @"C:\Windows\Installer\reverted.msp";
        var msi = new ScriptedPatchApi();
        msi.Set(PatchA, ProductOne, state: "1", uninstallable: "0");
        var svc = new RemovableReverifier(Substitute.For<IInstallerQueryService>(), msi);

        Assert.Equal(new[] { path }, svc.RecheckUnderLease(new[] { Claim(path, PatchA, ProductOne) }).HeldBack);
    }

    [Fact]
    public void A_superseded_claim_that_can_still_be_uninstalled_reclaims_its_path()
    {
        // Superseded is not sufficient on its own: a superseded patch that can
        // still be uninstalled needs its .msp to roll back with.
        const string path = @"C:\Windows\Installer\rollbackable.msp";
        var msi = new ScriptedPatchApi();
        msi.Set(PatchA, ProductOne, state: "2", uninstallable: "1");
        var svc = new RemovableReverifier(Substitute.For<IInstallerQueryService>(), msi);

        Assert.Equal(new[] { path }, svc.RecheckUnderLease(new[] { Claim(path, PatchA, ProductOne) }).HeldBack);
    }

    [Fact]
    public void A_property_that_cannot_be_read_reclaims_its_path()
    {
        // Fails closed. A read that could not be made has not shown the file to
        // be removable, and this is the last check in front of a permanent delete.
        const string path = @"C:\Windows\Installer\unreadable.msp";
        var msi = new ScriptedPatchApi();
        msi.Set(PatchA, ProductOne, state: "2", uninstallable: "0");
        msi.FailProperty(PatchA, ProductOne, "State");
        var svc = new RemovableReverifier(Substitute.For<IInstallerQueryService>(), msi);

        var recheck = svc.RecheckUnderLease(new[] { Claim(path, PatchA, ProductOne) });

        Assert.Equal(new[] { path }, recheck.HeldBack);
        // And it says WHY, because the user is shown a cause and this is not the
        // same cause as a program having taken the file back. The completion copy
        // carries a separate sentence for exactly this.
        Assert.True(recheck.RecordsIncomplete);
    }

    [Fact]
    public void A_verdict_that_moved_does_not_claim_the_records_were_short()
    {
        const string path = @"C:\Windows\Installer\reverted.msp";
        var msi = new ScriptedPatchApi();
        msi.Set(PatchA, ProductOne, state: "1", uninstallable: "0");
        var svc = new RemovableReverifier(Substitute.For<IInstallerQueryService>(), msi);

        var recheck = svc.RecheckUnderLease(new[] { Claim(path, PatchA, ProductOne) });

        Assert.Equal(new[] { path }, recheck.HeldBack);
        Assert.False(recheck.RecordsIncomplete);
    }

    [Fact]
    public void A_second_product_holding_the_same_patch_can_reclaim_it_alone()
    {
        // THE case the whole design turns on, and the one a single retained
        // product code cannot see. One .msp, two products holding it. The merge
        // that builds the registered rows keeps one row per path, so whichever
        // product it kept is the only one a per-path re-read would ask. Here the
        // kept one still says Superseded and the other has reverted to Applied.
        const string shared = @"C:\Windows\Installer\shared.msp";
        var msi = new ScriptedPatchApi();
        msi.Set(PatchA, ProductOne, state: "2", uninstallable: "0"); // still removable
        msi.Set(PatchA, ProductTwo, state: "1", uninstallable: "0"); // needed again

        var svc = new RemovableReverifier(Substitute.For<IInstallerQueryService>(), msi);
        var reclaimed = svc.RecheckUnderLease(new[]
        {
            Claim(shared, PatchA, ProductOne),
            Claim(shared, PatchA, ProductTwo),
        }).HeldBack;

        Assert.Equal(new[] { shared }, reclaimed);
    }

    [Fact]
    public void A_path_is_reported_once_however_many_of_its_claims_have_moved()
    {
        const string shared = @"C:\Windows\Installer\shared.msp";
        var msi = new ScriptedPatchApi();
        msi.Set(PatchA, ProductOne, state: "1", uninstallable: "0");
        msi.Set(PatchA, ProductTwo, state: "1", uninstallable: "0");

        var svc = new RemovableReverifier(Substitute.For<IInstallerQueryService>(), msi);
        var reclaimed = svc.RecheckUnderLease(new[]
        {
            Claim(shared, PatchA, ProductOne),
            Claim(shared, PatchA, ProductTwo),
        }).HeldBack;

        // One verdict per path is all a caller can act on, and the second claim
        // is not queried at all once the first has condemned it.
        Assert.Equal(new[] { shared }, reclaimed);
        Assert.Equal(2, msi.Reads); // State + Uninstallable, for the first claim only
    }

    [Fact]
    public void Unrelated_paths_are_judged_independently()
    {
        const string kept = @"C:\Windows\Installer\kept.msp";
        const string taken = @"C:\Windows\Installer\taken.msp";
        var msi = new ScriptedPatchApi();
        msi.Set(PatchA, ProductOne, state: "2", uninstallable: "0");
        msi.Set(PatchB, ProductOne, state: "1", uninstallable: "0");

        var svc = new RemovableReverifier(Substitute.For<IInstallerQueryService>(), msi);
        var reclaimed = svc.RecheckUnderLease(new[]
        {
            Claim(kept, PatchA, ProductOne),
            Claim(taken, PatchB, ProductOne),
        }).HeldBack;

        Assert.Equal(new[] { taken }, reclaimed);
    }

    [Fact]
    public async Task The_pre_lease_pass_carries_forward_only_the_survivors_claims()
    {
        // What the action service is handed has to match what it is acting on: a
        // dropped path is already out of the batch, and re-reading it under the
        // installer lock would be work done to confirm a decision nothing can act
        // on.
        const string dropped = @"C:\Windows\Installer\dropped.msp";
        const string kept = @"C:\Windows\Installer\kept.msp";
        var q = Substitute.For<IInstallerQueryService>();
        q.GetRegisteredPackagesAsync(Arg.Any<IProgress<ScanProgressUpdate>?>(), Arg.Any<CancellationToken>())
            .Returns(new InstallerQueryResult(
                new[] { NonRemovable(dropped) },
                UnreadableProductCount: 0,
                PatchClaims: new[] { Claim(dropped, PatchA, ProductOne), Claim(kept, PatchB, ProductOne) }));
        var svc = Reverifier(q);

        var result = await svc.ReverifyAsync(new[] { dropped, kept });

        Assert.Equal(new[] { kept }, result.Surviving);
        var claim = Assert.Single(result.SurvivingPatchClaims);
        Assert.Equal(kept, claim.LocalPackagePath);
    }

    /// <summary>
    /// A minimal scriptable <see cref="InstallerClean.Interop.IMsiApi"/> for the
    /// under-lease re-read, which reads two patch properties and nothing else.
    /// The enumeration entry points throw rather than answering: the whole point
    /// of this path is that it does not enumerate while holding the installer
    /// mutex, so a call reaching one is the defect, not a case to model.
    /// </summary>
    private sealed class ScriptedPatchApi : InstallerClean.Interop.IMsiApi
    {
        private const uint Success = 0;
        private const uint MoreData = 234;
        private const uint FunctionFailed = 1627;

        private readonly Dictionary<(string, string, string), string> _values = new();
        private readonly HashSet<(string, string, string)> _failing = new();

        /// <summary>Property reads made, so a test can pin that a path is judged once.</summary>
        public int Reads { get; private set; }

        public void Set(string patchCode, string productCode, string state, string uninstallable)
        {
            _values[(patchCode, productCode, "State")] = state;
            _values[(patchCode, productCode, "Uninstallable")] = uninstallable;
        }

        public void FailProperty(string patchCode, string productCode, string property) =>
            _failing.Add((patchCode, productCode, property));

        public uint GetPatchInfo(string patchCode, string productCode, string? userSid,
            InstallerClean.Interop.MsiInstallContext context, string property,
            char[]? value, ref uint valueLength)
        {
            if (value is null) Reads++;
            if (_failing.Contains((patchCode, productCode, property)))
            {
                valueLength = 0;
                return FunctionFailed;
            }
            var val = _values.GetValueOrDefault((patchCode, productCode, property), "");
            if (val.Length == 0) { valueLength = 0; return Success; }
            if (value is null) { valueLength = (uint)val.Length; return MoreData; }
            var n = Math.Min(val.Length, value.Length);
            for (var i = 0; i < n; i++) value[i] = val[i];
            valueLength = (uint)val.Length;
            return Success;
        }

        public uint EnumProducts(string? productCode, string? userSid,
            InstallerClean.Interop.MsiInstallContext context, uint index,
            char[]? installedProductCode, out InstallerClean.Interop.MsiInstallContext installedContext,
            char[]? sid, ref uint sidLength) =>
            throw new InvalidOperationException("The under-lease re-read must not enumerate products.");

        public uint EnumPatches(string? productCode, string? userSid,
            InstallerClean.Interop.MsiInstallContext context, InstallerClean.Interop.MsiPatchFilter filter,
            uint index, char[]? patchCode, char[]? targetProductCode,
            out InstallerClean.Interop.MsiInstallContext targetProductContext,
            char[]? targetUserSid, ref uint targetUserSidLength) =>
            throw new InvalidOperationException("The under-lease re-read must not enumerate patches.");

        public uint GetProductInfo(string productCode, string? userSid,
            InstallerClean.Interop.MsiInstallContext context, string property,
            char[]? value, ref uint valueLength) =>
            throw new InvalidOperationException("The under-lease re-read must not read product properties.");
    }
}
