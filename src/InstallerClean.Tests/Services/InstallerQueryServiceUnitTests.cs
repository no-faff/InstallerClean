using InstallerClean.Interop;
using InstallerClean.Models;
using InstallerClean.Resources;
using InstallerClean.Services;

namespace InstallerClean.Tests.Services;

/// <summary>
/// Unit tests for <see cref="InstallerQueryService"/>'s verdict logic, driven
/// through the <see cref="IMsiApi"/> seam with a scriptable fake so every
/// error path that decides a cached file's fate runs without an elevated
/// Windows host. The real-API integration tests
/// (<see cref="Integration.InstallerQueryServiceTests"/>) still cover the live
/// enumeration; these pin the merge, the fail-safe guards and the enumeration
/// failure handling that had no test before.
///
/// The registry fallback runs after the API enumeration on every call, and
/// these tests bind it to a stub that reads nothing and reports no failures, so
/// a run's outcome depends on the scripted API alone. Letting it read the live
/// UserData keys of whatever host the suite ran on would be tolerable if the
/// fallback could only ADD rows (the assertions filter to the paths the fake
/// produces), but the degraded-sources gate reads its failure count and the
/// enumeration cross-check reads how many products it walked, so a CI machine's
/// own registry would decide the outcome of every test that scripts a short
/// enumeration. The merge rules the fallback participates in are pinned by
/// calling <c>MergeClaim</c> directly.
/// </summary>
public class InstallerQueryServiceUnitTests
{
    private const uint Success = 0, AccessDenied = 5, MoreData = 234, NoMoreItems = 259;

    /// <summary>
    /// Codes the property reads branch on. <see cref="BadConfiguration"/> stands
    /// for the whole unreadable class (any code not on the benign allowlist
    /// reaches the same branch); <see cref="UnknownProperty"/> is the one a
    /// record that simply does not carry the property returns, which a
    /// 2026-07-18 Windows probe established is distinct from a failure.
    /// </summary>
    private const uint UnknownProperty = 1608, BadConfiguration = 1610;

    /// <summary>
    /// ERROR_INVALID_PARAMETER. What MsiEnumPatchesEx returns at every index when
    /// the szUserSid is one MsiEnumProductsEx emitted but the patch enumerator
    /// rejects (S-1-5-18 for a per-user-managed instance), where every index
    /// refuses identically.
    /// </summary>
    private const uint InvalidParameter = 87;

    /// <summary>
    /// A registry fallback that contributes nothing, fails at nothing and names
    /// no products: the healthy-second-source baseline every test but the
    /// degraded ones wants. Naming no products keeps it out of the
    /// enumeration-shortfall cross-check as well, which weighs the API's count
    /// against the registry's and cannot find the API short of nothing.
    /// </summary>
    private static InstallerQueryService.FallbackRead NoFallback(
        Dictionary<string, RegisteredPackage> claimed, CancellationToken ct) => new(0, 0);

    private static async Task<InstallerQueryResult> Run(FakeMsiApi msi) =>
        await new InstallerQueryService(msi, NoFallback).GetRegisteredPackagesAsync();

    /// <summary>
    /// Runs with a fallback that reports <paramref name="fallbackFailures"/>
    /// failed key reads, the second half of the degraded-sources gate.
    /// </summary>
    private static async Task<InstallerQueryResult> Run(FakeMsiApi msi, int fallbackFailures) =>
        await new InstallerQueryService(msi, (_, _) => new InstallerQueryService.FallbackRead(fallbackFailures, 0))
            .GetRegisteredPackagesAsync();

    /// <summary>
    /// Runs with a healthy fallback that names <paramref name="registryProducts"/>
    /// product keys, which is the count the enumeration cross-check weighs the
    /// API's own against, and reports
    /// <paramref name="unclaimedProductFiles"/> / <paramref name="unclaimedPatchFiles"/>
    /// registry entries naming a cached file that is on disk and that the API's
    /// own loop never claimed.
    ///
    /// The two unclaimed counts arrive here finished, because the real reader
    /// answers both halves itself: it holds the paths, and it asks the live
    /// filesystem about them. Nothing downstream can recompute either (the merge
    /// keeps one row per path and does not record which source reached it first),
    /// so what a test can drive is the observation, not the disk behind it.
    /// </summary>
    private static async Task<InstallerQueryResult> RunAgainstRegistry(
        FakeMsiApi msi,
        int registryProducts,
        int unclaimedProductFiles = 0,
        int unclaimedPatchFiles = 0) =>
        await new InstallerQueryService(msi, (_, _) => new InstallerQueryService.FallbackRead(
                0, registryProducts, unclaimedProductFiles, unclaimedPatchFiles))
            .GetRegisteredPackagesAsync();

    // ---- Shared-patch verdict merge ----

    [Theory]
    [InlineData(true)]   // superseded product enumerates first
    [InlineData(false)]  // applied product enumerates first
    public async Task Shared_patch_applied_to_one_product_is_never_removable(bool supersededFirst)
    {
        const string shared = @"C:\Windows\Installer\shared.msp";
        var msi = new FakeMsiApi();
        var (first, second) = supersededFirst ? ("{SUP}", "{APP}") : ("{APP}", "{SUP}");
        msi.AddProduct(first);
        msi.AddProduct(second);
        // Superseded + uninstallable "0" => removable for the {SUP} product.
        msi.AddPatch("{SUP}", "{P}", localPackage: shared, state: "2", uninstallable: "0");
        // Applied for {APP}: still needed, must win the merge.
        msi.AddPatch("{APP}", "{P}", localPackage: shared, state: "1", uninstallable: "1");

        var result = await Run(msi);

        var row = Assert.Single(result.Packages, r => r.LocalPackagePath == shared);
        Assert.False(row.IsRemovable);
        Assert.Equal(1, row.PatchState); // carries the applied product's state
    }

    [Fact]
    public async Task Every_product_claiming_a_shared_patch_keeps_its_own_identity()
    {
        // The merge above keeps ONE row per path and therefore one product code.
        // The claims are what the act-time re-read asks with, so they must keep
        // both products: asking only the one the merge happened to retain asks
        // about one of two, and the other is exactly where a verdict can move.
        const string shared = @"C:\Windows\Installer\shared.msp";
        var msi = new FakeMsiApi();
        msi.AddProduct("{ONE}");
        msi.AddProduct("{TWO}");
        msi.AddPatch("{ONE}", "{P}", localPackage: shared, state: "2", uninstallable: "0");
        msi.AddPatch("{TWO}", "{P}", localPackage: shared, state: "2", uninstallable: "0");

        var result = await Run(msi);

        Assert.Single(result.Packages, r => r.LocalPackagePath == shared);
        var claims = result.PatchClaims.Where(c => c.LocalPackagePath == shared).ToList();
        Assert.Equal(2, claims.Count);
        Assert.Equal(new[] { "{ONE}", "{TWO}" }, claims.Select(c => c.ProductCode).Order().ToArray());
        Assert.All(claims, c => Assert.Equal("{P}", c.PatchCode));
    }

    [Fact]
    public async Task A_claim_is_recorded_whatever_the_patch_state_says()
    {
        // Not filtered to the removable ones. A claim that is Applied today is
        // the one that proves a path is still needed if a later re-read finds it,
        // so keeping only the removable claims would discard the answers worth
        // having.
        const string shared = @"C:\Windows\Installer\shared.msp";
        var msi = new FakeMsiApi();
        msi.AddProduct("{SUP}");
        msi.AddProduct("{APP}");
        msi.AddPatch("{SUP}", "{P}", localPackage: shared, state: "2", uninstallable: "0");
        msi.AddPatch("{APP}", "{P}", localPackage: shared, state: "1", uninstallable: "1");

        var result = await Run(msi);

        Assert.Equal(2, result.PatchClaims.Count(c => c.LocalPackagePath == shared));
    }

    [Fact]
    public async Task A_product_row_downgrades_a_corrupt_patch_claim_on_its_own_package()
    {
        // The in-cache variant of the corrupt-record threat CandidateGuard covers
        // out-of-cache: patch X's LocalPackage is corrupt and names product B's
        // cached .msi instead of an .msp of its own. A enumerates first and claims
        // the path removable. B's product row must be able to take it back;
        // first-writer-wins would decide it on enumeration order and remove a
        // package B still needs.
        const string productPackage = @"C:\Windows\Installer\product-b.msi";
        var msi = new FakeMsiApi();
        msi.AddProduct("{A}");
        msi.AddProduct("{B}");
        msi.AddPatch("{A}", "{P}", localPackage: productPackage, state: "2", uninstallable: "0");
        msi.SetProductProperty("{B}", "LocalPackage", productPackage);

        var result = await Run(msi);

        Assert.False(Assert.Single(result.Packages, r => r.LocalPackagePath == productPackage).IsRemovable);
    }

    [Fact]
    public async Task A_patch_row_never_upgrades_a_products_claim_on_its_own_package()
    {
        // The same collision in the other enumeration order. Downgrade-only means
        // the outcome does not depend on which of the two enumerated first.
        const string productPackage = @"C:\Windows\Installer\product-a.msi";
        var msi = new FakeMsiApi();
        msi.AddProduct("{A}");
        msi.AddProduct("{B}");
        msi.SetProductProperty("{A}", "LocalPackage", productPackage);
        msi.AddPatch("{B}", "{P}", localPackage: productPackage, state: "2", uninstallable: "0");

        var result = await Run(msi);

        Assert.False(Assert.Single(result.Packages, r => r.LocalPackagePath == productPackage).IsRemovable);
    }

    [Fact]
    public void A_registry_fallback_claim_never_downgrades_an_api_verdict()
    {
        // The scoping rule the whole merge rests on, pinned here because the real
        // fallback reads the live registry and cannot be driven from a test. The
        // fallback reads the same UserData keys the API read and runs after it, so
        // every superseded patch has a fallback row waiting for its own path:
        // letting one downgrade would strip the removable verdict off every patch
        // the API had just identified, and superseded-patch detection would return
        // nothing at all.
        const string superseded = @"C:\Windows\Installer\superseded.msp";
        var claimed = new Dictionary<string, RegisteredPackage>(StringComparer.OrdinalIgnoreCase);
        InstallerQueryService.MergeClaim(claimed,
            new RegisteredPackage(superseded, "Product", "{A}", PatchState: 2, IsRemovable: true),
            InstallerQueryService.ClaimSource.InstallerApi);

        InstallerQueryService.MergeClaim(claimed,
            new RegisteredPackage(superseded, "", ""),
            InstallerQueryService.ClaimSource.RegistryFallback);

        Assert.True(claimed[superseded].IsRemovable);
        Assert.Equal("Product", claimed[superseded].ProductName);
    }

    [Fact]
    public void A_registry_fallback_claim_adds_a_path_the_api_never_returned()
    {
        // The other half of the fallback's contract: it is the app's second
        // "still needed" source, so a path only it knows about must land, or the
        // file it names is offered as an orphan.
        const string onlyInRegistry = @"C:\Windows\Installer\registry-only.msi";
        var claimed = new Dictionary<string, RegisteredPackage>(StringComparer.OrdinalIgnoreCase);

        InstallerQueryService.MergeClaim(claimed,
            new RegisteredPackage(onlyInRegistry, "", ""),
            InstallerQueryService.ClaimSource.RegistryFallback);

        Assert.False(Assert.Contains(onlyInRegistry, claimed).IsRemovable);
    }

    [Fact]
    public void A_claim_that_establishes_something_displaces_a_row_that_does_not()
    {
        // Two non-removable claims on one path are not always the same finding.
        // The row that wins is the one carrying a live claim, because it is the
        // only one that supports a sentence: the other is a read that failed and
        // says nothing about the file at all.
        const string shared = @"C:\Windows\Installer\shared.msp";
        var claimed = new Dictionary<string, RegisteredPackage>(StringComparer.OrdinalIgnoreCase);
        InstallerQueryService.MergeClaim(claimed,
            new RegisteredPackage(shared, "Unread", "{A}", VerdictUnreadable: true),
            InstallerQueryService.ClaimSource.InstallerApi);

        InstallerQueryService.MergeClaim(claimed,
            new RegisteredPackage(shared, "Applied", "{B}", PatchState: 1),
            InstallerQueryService.ClaimSource.InstallerApi);

        Assert.False(claimed[shared].VerdictUnreadable);
        Assert.Equal("Applied", claimed[shared].ProductName);
    }

    [Fact]
    public void A_row_whose_verdict_never_read_does_not_displace_a_live_claim()
    {
        // The same pair the other way round, which is the half that makes the
        // rule an answer rather than a preference: whichever order the
        // enumeration reaches these two products in, the file is reported the
        // same way.
        const string shared = @"C:\Windows\Installer\shared.msp";
        var claimed = new Dictionary<string, RegisteredPackage>(StringComparer.OrdinalIgnoreCase);
        InstallerQueryService.MergeClaim(claimed,
            new RegisteredPackage(shared, "Applied", "{B}", PatchState: 1),
            InstallerQueryService.ClaimSource.InstallerApi);

        InstallerQueryService.MergeClaim(claimed,
            new RegisteredPackage(shared, "Unread", "{A}", VerdictUnreadable: true),
            InstallerQueryService.ClaimSource.InstallerApi);

        Assert.False(claimed[shared].VerdictUnreadable);
        Assert.Equal("Applied", claimed[shared].ProductName);
    }

    [Fact]
    public void A_removable_claim_does_not_displace_a_row_whose_verdict_never_read()
    {
        // The guard on the rule above. Displacing on the cause must not become a
        // route back to removable: a product reading the patch as superseded says
        // nothing about the product whose read failed, and the file it would
        // release is one nobody has been able to ask about.
        const string shared = @"C:\Windows\Installer\shared.msp";
        var claimed = new Dictionary<string, RegisteredPackage>(StringComparer.OrdinalIgnoreCase);
        InstallerQueryService.MergeClaim(claimed,
            new RegisteredPackage(shared, "Unread", "{A}", VerdictUnreadable: true),
            InstallerQueryService.ClaimSource.InstallerApi);

        InstallerQueryService.MergeClaim(claimed,
            new RegisteredPackage(shared, "Superseded", "{B}", PatchState: 2, IsRemovable: true),
            InstallerQueryService.ClaimSource.InstallerApi);

        Assert.False(claimed[shared].IsRemovable);
        Assert.True(claimed[shared].VerdictUnreadable);
    }

    [Fact]
    public void An_api_claim_never_upgrades_a_non_removable_row()
    {
        const string shared = @"C:\Windows\Installer\shared.msp";
        var claimed = new Dictionary<string, RegisteredPackage>(StringComparer.OrdinalIgnoreCase);
        InstallerQueryService.MergeClaim(claimed,
            new RegisteredPackage(shared, "Applied", "{A}", PatchState: 1),
            InstallerQueryService.ClaimSource.InstallerApi);

        InstallerQueryService.MergeClaim(claimed,
            new RegisteredPackage(shared, "Superseded", "{B}", PatchState: 2, IsRemovable: true),
            InstallerQueryService.ClaimSource.InstallerApi);

        Assert.False(claimed[shared].IsRemovable);
    }

    // ---- ...and says whether it added the path ----
    //
    // The whole of the fallback's second job rests on this bool being exact: it
    // is what separates "no product the API loop reached ever named this file"
    // from "the API named it first". A merge that reported an add for a path it
    // had only downgraded would withhold the removable class on every machine
    // with a shared patch; one that reported none would withhold on none.

    [Fact]
    public void A_fallback_claim_reports_the_add_only_the_first_time()
    {
        const string path = @"C:\Windows\Installer\registry-only.msi";
        var claimed = new Dictionary<string, RegisteredPackage>(StringComparer.OrdinalIgnoreCase);

        Assert.True(InstallerQueryService.MergeClaim(claimed, new RegisteredPackage(path, "", ""),
            InstallerQueryService.ClaimSource.RegistryFallback));
        Assert.False(InstallerQueryService.MergeClaim(claimed, new RegisteredPackage(path, "", ""),
            InstallerQueryService.ClaimSource.RegistryFallback));
    }

    [Fact]
    public void A_fallback_claim_on_a_path_the_api_already_claimed_reports_no_add()
    {
        // The ordinary case on a healthy machine, and the one the count must read
        // as zero: the fallback re-reads the same UserData key the API did.
        const string path = @"C:\Windows\Installer\shared.msi";
        var claimed = new Dictionary<string, RegisteredPackage>(StringComparer.OrdinalIgnoreCase);
        InstallerQueryService.MergeClaim(claimed, new RegisteredPackage(path, "Product", "{A}"),
            InstallerQueryService.ClaimSource.InstallerApi);

        Assert.False(InstallerQueryService.MergeClaim(claimed, new RegisteredPackage(path, "", ""),
            InstallerQueryService.ClaimSource.RegistryFallback));
    }

    [Fact]
    public void An_api_claim_reports_an_add_for_a_new_path_and_not_for_a_downgrade()
    {
        // A downgrade rewrites the row and adds no path, so it is not an add.
        // Nothing counts API adds today; the bool means the same thing on both
        // arms so that a caller which starts to cannot be handed a second rule.
        const string path = @"C:\Windows\Installer\shared.msp";
        var claimed = new Dictionary<string, RegisteredPackage>(StringComparer.OrdinalIgnoreCase);

        Assert.True(InstallerQueryService.MergeClaim(claimed,
            new RegisteredPackage(path, "Superseded", "{A}", PatchState: 2, IsRemovable: true),
            InstallerQueryService.ClaimSource.InstallerApi));

        Assert.False(InstallerQueryService.MergeClaim(claimed,
            new RegisteredPackage(path, "Applied", "{B}", PatchState: 1),
            InstallerQueryService.ClaimSource.InstallerApi));
        Assert.False(claimed[path].IsRemovable);
    }

    [Fact]
    public async Task Superseded_patch_claimed_by_a_single_product_stays_removable()
    {
        const string dead = @"C:\Windows\Installer\dead.msp";
        var msi = new FakeMsiApi();
        msi.AddProduct("{A}");
        msi.AddPatch("{A}", "{P}", localPackage: dead, state: "2", uninstallable: "0");

        var result = await Run(msi);

        Assert.True(Assert.Single(result.Packages, r => r.LocalPackagePath == dead).IsRemovable);
    }

    // ---- Uninstallable guard fails safe ----

    [Fact]
    public async Task Unreadable_Uninstallable_keeps_a_superseded_patch()
    {
        const string p = @"C:\Windows\Installer\uninst.msp";
        var msi = new FakeMsiApi();
        msi.AddProduct("{A}");
        // Uninstallable left unset => the property read returns empty.
        msi.AddPatch("{A}", "{P}", localPackage: p, state: "2", uninstallable: null);

        var result = await Run(msi);

        Assert.False(Assert.Single(result.Packages, r => r.LocalPackagePath == p).IsRemovable);
    }

    [Fact]
    public async Task Positively_read_zero_Uninstallable_allows_removal_of_a_superseded_patch()
    {
        const string p = @"C:\Windows\Installer\rem0.msp";
        var msi = new FakeMsiApi();
        msi.AddProduct("{A}");
        msi.AddPatch("{A}", "{P}", localPackage: p, state: "2", uninstallable: "0");

        var result = await Run(msi);

        Assert.True(Assert.Single(result.Packages, r => r.LocalPackagePath == p).IsRemovable);
    }

    // ---- Patch enumeration AccessDenied throws (matches product loop) ----

    [Fact]
    public async Task Patch_enumeration_access_denied_throws()
    {
        var msi = new FakeMsiApi();
        msi.AddProduct("{A}");
        msi.PatchEnumResult["{A}"] = AccessDenied;

        await Assert.ThrowsAsync<LocalisedAccessException>(() => Run(msi));
    }

    // ---- A patch enumeration that collapses degrades that product, not the scan ----
    //
    // One product whose patch rows keep coming back unreadable (the DisplayLink
    // shape: the SID the product enumerator emitted is rejected by the patch
    // enumerator, so every index refuses) is a per-product loss, not a scan
    // failure. It ends that product's patch enumeration and returns Incomplete,
    // which reaches the same withholding, count and fallback the other losses do.
    // Both cap sites convert: the non-success run and the empty-GUID run. The
    // AccessDenied throw and the never-ended-cap throw stay scan-fatal.

    [Fact]
    public async Task A_products_patch_enumeration_refusing_every_index_degrades_without_aborting_the_scan()
    {
        // The DisplayLink shape: {DL}'s patch enumeration returns 87 at every
        // index. It degrades to one unreadable product rather than taking the scan
        // down, and the other product's rows are untouched.
        const string dead = @"C:\Windows\Installer\other-superseded.msp";
        var msi = new FakeMsiApi();
        msi.AddProduct("{A}");
        msi.AddPatch("{A}", "{P}", localPackage: dead, state: "2", uninstallable: "0"); // removable under A
        msi.AddProduct("{DL}");
        msi.SetProductProperty("{DL}", "LocalPackage", @"C:\Windows\Installer\displaylink.msi");
        msi.PatchEnumResult["{DL}"] = InvalidParameter; // 87 at every index

        var result = await Run(msi); // completes: no throw

        Assert.Contains(result.Packages, r => r.LocalPackagePath == @"C:\Windows\Installer\displaylink.msi");
        var row = Assert.Single(result.Packages, r => r.LocalPackagePath == dead);
        Assert.False(row.IsRemovable);              // removable class withheld scan-wide
        Assert.True(row.RemovableWithheld);
        Assert.Equal(1, result.UnaccountedProductCount); // the degraded product counts exactly once
        Assert.True(result.RecordsIncomplete);
    }

    [Fact]
    public async Task Patch_rows_read_before_a_collapse_still_merge_and_the_product_counts_once()
    {
        // {B} enumerates one real row, then its enumeration collapses into a run of
        // failures. The row read before the collapse keeps its claim, and {B} is
        // counted once, not once per failed index.
        const string merged = @"C:\Windows\Installer\merged-before-collapse.msp";
        var msi = new FakeMsiApi();
        msi.AddProduct("{A}");
        msi.SetProductProperty("{A}", "LocalPackage", @"C:\Windows\Installer\a.msi");
        msi.AddProduct("{B}");
        // Index 0 is a real patch row; indices 1..20 refuse, a 20-run collapse that
        // trips the per-product stop count.
        var codes = new List<string> { "{P}" };
        for (uint i = 1; i <= 20; i++) { codes.Add($"{{x{i}}}"); msi.PatchRowResult[("{B}", i)] = InvalidParameter; }
        msi.PatchCodes["{B}"] = codes;
        msi.SetPatchProperty("{P}", "{B}", "LocalPackage", merged);
        msi.SetPatchProperty("{P}", "{B}", "State", "1");        // applied, non-removable
        msi.SetPatchProperty("{P}", "{B}", "Uninstallable", "1");

        var result = await Run(msi);

        Assert.Contains(result.Packages, r => r.LocalPackagePath == merged); // the read row survived
        Assert.Equal(1, result.UnaccountedProductCount);                      // B counted once
    }

    [Fact]
    public async Task A_run_of_empty_patch_guids_degrades_the_product_the_same_way_the_error_run_does()
    {
        // The other converted cap site: a sustained run of Success-with-no-GUID
        // rows is the same "this product's rows are unreadable" state and must
        // degrade identically, proving BOTH arms convert, not only the error one.
        const string dead = @"C:\Windows\Installer\empty-run-other.msp";
        var msi = new FakeMsiApi();
        msi.AddProduct("{A}");
        msi.AddPatch("{A}", "{P}", localPackage: dead, state: "2", uninstallable: "0"); // removable under A
        msi.AddProduct("{B}");
        msi.SetProductProperty("{B}", "LocalPackage", @"C:\Windows\Installer\b.msi");
        msi.PatchCodes["{B}"] = Enumerable.Repeat("", 20).ToList(); // 20 Success returns, no GUID written

        var result = await Run(msi); // completes: no throw

        Assert.Contains(result.Packages, r => r.LocalPackagePath == @"C:\Windows\Installer\b.msi");
        var row = Assert.Single(result.Packages, r => r.LocalPackagePath == dead);
        Assert.False(row.IsRemovable);
        Assert.True(row.RemovableWithheld);
        Assert.Equal(1, result.UnaccountedProductCount);
        Assert.True(result.RecordsIncomplete);
    }

    [Fact]
    public async Task Every_products_patch_enumeration_refusing_still_completes_with_a_clean_fallback()
    {
        // Even if EVERY product's patch enumeration refuses, a clean registry
        // fallback keeps the scan a completed scan, reporting all of them
        // unreadable rather than aborting.
        var msi = new FakeMsiApi();
        msi.AddProduct("{A}");
        msi.SetProductProperty("{A}", "LocalPackage", @"C:\Windows\Installer\a.msi");
        msi.PatchEnumResult["{A}"] = InvalidParameter;
        msi.AddProduct("{B}");
        msi.SetProductProperty("{B}", "LocalPackage", @"C:\Windows\Installer\b.msi");
        msi.PatchEnumResult["{B}"] = InvalidParameter;

        var result = await Run(msi, fallbackFailures: 0);

        Assert.Equal(2, result.UnaccountedProductCount);
        Assert.Contains(result.Packages, r => r.LocalPackagePath == @"C:\Windows\Installer\a.msi");
        Assert.Contains(result.Packages, r => r.LocalPackagePath == @"C:\Windows\Installer\b.msi");
    }

    [Fact]
    public async Task A_patch_collapse_alongside_a_failing_fallback_still_refuses_the_scan()
    {
        // The both-sources-degraded gate must still fire when the API side is
        // degraded by a patch collapse (not only by a product-row failure, which
        // the existing gate test covers) and the registry fallback is ALSO failing
        // reads: that is the state the withholding cannot cover.
        var msi = new FakeMsiApi();
        msi.AddProduct("{A}");
        msi.SetProductProperty("{A}", "LocalPackage", @"C:\Windows\Installer\a.msi");
        msi.PatchEnumResult["{A}"] = InvalidParameter;

        var ex = await Assert.ThrowsAsync<LocalisedInvalidOperationException>(
            () => Run(msi, fallbackFailures: 1));

        Assert.Equal(Strings.Error_ScanRecordsUnreadable, ex.Message);
    }

    [Fact]
    public async Task A_reverify_over_a_patch_collapse_degraded_query_keeps_the_candidate_and_reports_why()
    {
        // End to end through the reverifier, off the REAL query degraded via the
        // IMsiApi seam (RemovableReverifierTests pins the same behaviour off a
        // mocked query). A patch-collapse-degraded query withholds the removable
        // class, so a superseded candidate is kept in place at action time and the
        // result says the records were incomplete, not that a program reclaimed it.
        const string superseded = @"C:\Windows\Installer\reverify-collapse.msp";
        var msi = new FakeMsiApi();
        msi.AddProduct("{A}");
        msi.AddPatch("{A}", "{P}", localPackage: superseded, state: "2", uninstallable: "0"); // removable under A
        msi.AddProduct("{DL}");
        msi.SetProductProperty("{DL}", "LocalPackage", @"C:\Windows\Installer\displaylink.msi");
        msi.PatchEnumResult["{DL}"] = InvalidParameter; // the collapse that degrades the query

        var reverifier = new RemovableReverifier(new InstallerQueryService(msi, NoFallback), msi);
        var result = await reverifier.ReverifyAsync(new[] { superseded });

        Assert.Empty(result.Surviving);
        Assert.Equal(new[] { superseded }, result.Dropped);
        Assert.Equal(new HeldBackReasons(RecordsUnreadable: 1), result.Reasons);
    }

    // ---- An empty GUID accepted as success is not added ----

    [Fact]
    public async Task Empty_product_guid_is_not_added_and_counts_as_non_success()
    {
        const string p = @"C:\Windows\Installer\valid.msi";
        var msi = new FakeMsiApi();
        msi.AddProduct("");   // Success return, empty code
        msi.AddProduct("{A}");
        msi.SetProductProperty("{A}", "LocalPackage", p);

        var result = await Run(msi);

        Assert.DoesNotContain(result.Packages, r => r.LocalPackagePath.Length == 0);
        Assert.Contains(result.Packages, r => r.LocalPackagePath == p);
    }

    [Fact]
    public async Task Empty_patch_guid_is_not_added()
    {
        const string ppath = @"C:\Windows\Installer\real.msp";
        var msi = new FakeMsiApi();
        msi.AddProduct("{A}");
        msi.PatchCodes["{A}"] = new() { "", "{P}" }; // first row empty, second real
        msi.SetPatchProperty("{P}", "{A}", "LocalPackage", ppath);
        msi.SetPatchProperty("{P}", "{A}", "State", "2");
        msi.SetPatchProperty("{P}", "{A}", "Uninstallable", "0");

        var result = await Run(msi);

        Assert.Contains(result.Packages, r => r.LocalPackagePath == ppath);
    }

    // ---- An enumeration that ended early, seen from the registry's side ----
    //
    // The API cannot report this about itself: NoMoreItems at index 3 of 200
    // reads exactly like the end of a 3-product machine, so the only evidence is
    // that the registry knows about products the enumeration never mentioned.

    /// <summary>
    /// A machine with one product, one superseded patch of its own, and nothing
    /// else. Whether that patch is offered is the whole subject of the four
    /// tests below.
    /// </summary>
    private static FakeMsiApi OneProductWithASupersededPatch(string patch)
    {
        var msi = new FakeMsiApi();
        msi.AddProduct("{A}");
        msi.AddPatch("{A}", "{P}", localPackage: patch, state: "2", uninstallable: "0");
        return msi;
    }

    [Fact]
    public async Task An_enumeration_far_short_of_the_registrys_product_count_withholds_the_removable_class()
    {
        const string patch = @"C:\Windows\Installer\superseded.msp";

        var result = await RunAgainstRegistry(OneProductWithASupersededPatch(patch), registryProducts: 40);

        // Withheld, not refused: the orphan half of the scan is unaffected and
        // the user still has a list to act on.
        var row = Assert.Single(result.Packages, r => r.LocalPackagePath == patch);
        Assert.False(row.IsRemovable);
        Assert.True(row.RemovableWithheld);
        Assert.Equal(39, result.UnaccountedProductCount);
    }

    [Fact]
    public async Task A_registry_two_products_ahead_of_the_api_withholds_nothing()
    {
        // UserData product keys outlive a failed uninstall, so the registry
        // legitimately runs a little ahead on a healthy machine. Two is absorbed
        // outright, which is what stops a machine with a handful of
        // registrations tripping on one piece of residue.
        const string patch = @"C:\Windows\Installer\superseded.msp";
        var msi = OneProductWithASupersededPatch(patch);
        msi.AddProduct("{B}");
        msi.AddProduct("{C}");

        var result = await RunAgainstRegistry(msi, registryProducts: 5);

        Assert.True(Assert.Single(result.Packages, r => r.LocalPackagePath == patch).IsRemovable);
        Assert.Equal(0, result.UnaccountedProductCount);
    }

    [Fact]
    public async Task A_registry_a_tenth_ahead_of_the_api_withholds_nothing()
    {
        // The proportional half, which is what keeps the absolute one from
        // becoming a rule about big machines: nine products of residue on a
        // ninety-product machine is not an enumeration that gave up early.
        const string patch = @"C:\Windows\Installer\superseded.msp";
        var msi = OneProductWithASupersededPatch(patch);
        for (var i = 0; i < 89; i++) msi.AddProduct($"{{P{i}}}");

        var result = await RunAgainstRegistry(msi, registryProducts: 100);

        Assert.True(Assert.Single(result.Packages, r => r.LocalPackagePath == patch).IsRemovable);
        Assert.Equal(0, result.UnaccountedProductCount);
    }

    [Fact]
    public async Task A_short_enumeration_beside_a_failing_fallback_still_withholds_rather_than_refusing()
    {
        // The refusal above it stays keyed on what the API said about ITSELF. A
        // shortfall is inferred from two counts that can differ innocently, and
        // inference is sound enough to hold a class back and not sound enough to
        // take a machine's scan away: this run has both a short count and a
        // failed key read and still comes back with a list.
        const string patch = @"C:\Windows\Installer\superseded.msp";
        var msi = OneProductWithASupersededPatch(patch);

        var result = await new InstallerQueryService(msi,
            (_, _) => new InstallerQueryService.FallbackRead(Failures: 3, ProductKeys: 40))
            .GetRegisteredPackagesAsync();

        Assert.True(Assert.Single(result.Packages, r => r.LocalPackagePath == patch).RemovableWithheld);
    }

    [Fact]
    public async Task A_row_the_enumeration_skipped_is_not_counted_twice()
    {
        // A skipped row sits inside both counts: it is one product the
        // enumeration owned up to losing, and it is also absent from the
        // enumerated total the registry count is measured against. Twenty
        // registrations, four read cleanly, four skipped and twelve the
        // enumeration never mentioned is sixteen programs whose records this
        // scan did not get. Adding the counts made it twenty, which is every
        // product on the machine including the four it read perfectly well.
        const string patch = @"C:\Windows\Installer\superseded.msp";
        var msi = new FakeMsiApi();
        for (var i = 0; i < 4; i++)
        {
            // Success with no product GUID: one row lost, counted once, and
            // interleaved so the consecutive-failure cap is never approached.
            msi.AddProduct("");
            msi.AddProduct($"{{P{i}}}");
            msi.SetProductProperty($"{{P{i}}}", "LocalPackage", $@"C:\Windows\Installer\p{i}.msi");
        }
        msi.AddPatch("{P0}", "{PATCH}", localPackage: patch, state: "2", uninstallable: "0");

        var result = await RunAgainstRegistry(msi, registryProducts: 20);

        Assert.Equal(16, result.UnaccountedProductCount);
        Assert.True(Assert.Single(result.Packages, r => r.LocalPackagePath == patch).RemovableWithheld);
    }

    // ---- ...and seen directly, in a path only the registry ever claimed ----
    //
    // The headcount above infers a loss from two totals that differ for innocent
    // reasons, so it has to tolerate a band. The fallback reads the same UserData
    // keys the API read and runs after the whole API loop, so a path it is the
    // first to claim is one no product the loop reached ever named: that is the
    // loss itself rather than a difference of totals, and the band does not
    // apply to it.

    [Fact]
    public async Task A_path_only_the_registry_claimed_withholds_the_removable_class()
    {
        const string patch = @"C:\Windows\Installer\superseded.msp";

        // The registry names exactly as many products as the API enumerated, so
        // the headcount is silent and this fires on the observation alone.
        var result = await RunAgainstRegistry(OneProductWithASupersededPatch(patch),
            registryProducts: 1, unclaimedProductFiles: 1);

        var row = Assert.Single(result.Packages, r => r.LocalPackagePath == patch);
        Assert.False(row.IsRemovable);
        Assert.True(row.RemovableWithheld);
        Assert.Equal(1, result.UnaccountedProductCount);
    }

    [Fact]
    public async Task An_unclaimed_path_inside_the_headcounts_silent_band_still_withholds()
    {
        // The case the observation exists for. Ninety products enumerated
        // against a hundred registry keys is a tenth short, which the
        // proportional clause absorbs outright, so the headcount cannot fire
        // however many products were really lost.
        const string patch = @"C:\Windows\Installer\superseded.msp";
        var msi = OneProductWithASupersededPatch(patch);
        for (var i = 0; i < 89; i++) msi.AddProduct($"{{P{i}}}");

        var result = await RunAgainstRegistry(msi, registryProducts: 100, unclaimedProductFiles: 10);

        Assert.True(Assert.Single(result.Packages, r => r.LocalPackagePath == patch).RemovableWithheld);
        Assert.Equal(10, result.UnaccountedProductCount);
    }

    [Fact]
    public async Task An_unclaimed_patch_path_withholds_but_counts_only_one_product()
    {
        // A patch entry carries no product code, and one lost product can hold
        // any number of patches, so three of them are evidence of at least one
        // product and of no particular number.
        const string patch = @"C:\Windows\Installer\superseded.msp";

        var result = await RunAgainstRegistry(OneProductWithASupersededPatch(patch),
            registryProducts: 1, unclaimedPatchFiles: 3);

        Assert.True(Assert.Single(result.Packages, r => r.LocalPackagePath == patch).RemovableWithheld);
        Assert.Equal(1, result.UnaccountedProductCount);
    }

    [Fact]
    public async Task The_headcount_and_the_observation_are_not_added_together()
    {
        // Both signals estimate one quantity from opposite sides, so a machine
        // that trips both has lost thirty-nine products, not seventy-eight. What
        // the number reaches the user AS is nothing: the notice it gates says only
        // that something in the records could not be matched up, because this is
        // an estimate and two of its four terms are absences rather than failed
        // reads. The command line's Application-channel entry is the one surface
        // that still prints it.
        const string patch = @"C:\Windows\Installer\superseded.msp";

        var result = await RunAgainstRegistry(OneProductWithASupersededPatch(patch),
            registryProducts: 40, unclaimedProductFiles: 39);

        Assert.Equal(39, result.UnaccountedProductCount);
    }

    [Fact]
    public async Task A_product_already_counted_unreadable_is_not_counted_again_as_unclaimed()
    {
        // The double count the subtraction is for: a product whose row the API
        // skipped has its registry value claimed by the fallback alone, so it
        // shows up in both counts for one loss.
        const string patch = @"C:\Windows\Installer\superseded.msp";
        var msi = OneProductWithASupersededPatch(patch);
        msi.AddProduct("{B}", result: 1603 /* ERROR_INSTALL_FAILURE */);

        var result = await RunAgainstRegistry(msi, registryProducts: 2, unclaimedProductFiles: 1);

        Assert.True(Assert.Single(result.Packages, r => r.LocalPackagePath == patch).RemovableWithheld);
        Assert.Equal(1, result.UnaccountedProductCount);
    }

    [Fact]
    public async Task A_registry_that_claimed_nothing_of_its_own_keeps_the_removable_verdicts()
    {
        // The false-fire control. Every path the registry names was already
        // claimed by the API, which is what a whole enumeration looks like, and
        // superseded-patch cleanup has to survive it or the feature is dead.
        const string patch = @"C:\Windows\Installer\superseded.msp";

        var result = await RunAgainstRegistry(OneProductWithASupersededPatch(patch), registryProducts: 1);

        Assert.True(Assert.Single(result.Packages, r => r.LocalPackagePath == patch).IsRemovable);
        Assert.Equal(0, result.UnaccountedProductCount);
    }

    // ---- Nothing claims a cached file at all ----

    [Fact]
    public async Task A_product_set_that_claims_nothing_refuses_the_scan()
    {
        // The single catastrophic-failure backstop, and the one refusal in this
        // file that had no test: even a fresh Windows install has OS-level MSI
        // products, so no claim at all means the records are damaged or
        // unreadable, and a scan that believed the answer would call every file
        // in C:\Windows\Installer orphaned.
        var msi = new FakeMsiApi();

        var ex = await Assert.ThrowsAsync<LocalisedInvalidOperationException>(() => Run(msi));

        Assert.Equal(Strings.Error_InstallerDbEmpty, ex.Message);
    }

    [Fact]
    public async Task A_claim_from_the_fallback_alone_is_enough_to_scan()
    {
        // Worth as much as the refusal above and easier to break: the count is
        // tested AFTER the fallback has run, so an API that returns no product
        // while the registry still names a cached file is a scan, not a
        // refusal. Reordering the gate would turn that machine's ordinary run
        // into "the installer database is empty".
        const string fromRegistry = @"C:\Windows\Installer\registry-only.msi";
        var msi = new FakeMsiApi();

        var result = await new InstallerQueryService(msi, (claimed, _) =>
        {
            claimed[fromRegistry] = new RegisteredPackage(fromRegistry, "", "");
            return new InstallerQueryService.FallbackRead(0, 1);
        }).GetRegisteredPackagesAsync();

        Assert.Equal(fromRegistry, Assert.Single(result.Packages).LocalPackagePath);
        Assert.Equal(0, result.UnaccountedProductCount);
    }

    // ---- The index cap ends enumeration loudly, not silently ----

    // The message is asserted, not just the type, because the cap and the
    // consecutive-failure stop are different conditions that a shared string
    // describes falsely in both halves: at the cap the count is the budget
    // rather than a run of failures, and the error code is Success when every
    // row read cleanly. Asserting the type alone cannot tell the two apart.
    [Fact]
    public async Task Product_enumeration_that_never_ends_throws_at_the_cap()
    {
        var msi = new FakeMsiApi { NeverEndProducts = true };

        var ex = await Assert.ThrowsAsync<LocalisedInvalidOperationException>(() => Run(msi));

        Assert.Equal(
            string.Format(Strings.Error_MsiEnumerationNeverEnded, 10_000, MsiError.Success),
            ex.Message);
    }

    [Fact]
    public async Task Patch_enumeration_that_never_ends_throws_at_the_cap()
    {
        var msi = new FakeMsiApi();
        msi.AddProduct("{A}");
        msi.NeverEndPatchesFor = "{A}";

        var ex = await Assert.ThrowsAsync<LocalisedInvalidOperationException>(() => Run(msi));

        Assert.Equal(
            string.Format(Strings.Error_MsiPatchEnumerationNeverEnded, 10_000, MsiError.Success),
            ex.Message);
    }

    // ---- Scattered per-product failures are tolerated (no throw) ----

    [Fact]
    public async Task Scattered_product_failures_do_not_throw_and_good_products_survive()
    {
        var msi = new FakeMsiApi();
        // 30 products, every third fails: never 20 consecutive, so no throw.
        for (int i = 0; i < 30; i++)
        {
            if (i % 3 == 0)
                msi.AddProduct($"{{bad{i}}}", result: 1603 /* ERROR_INSTALL_FAILURE */);
            else
            {
                msi.AddProduct($"{{ok{i}}}");
                msi.SetProductProperty($"{{ok{i}}}", "LocalPackage", $@"C:\Windows\Installer\ok{i}.msi");
            }
        }

        var result = await Run(msi);

        Assert.Equal(20, result.Packages.Count(r => r.LocalPackagePath.StartsWith(@"C:\Windows\Installer\ok")));
    }

    [Fact]
    public async Task Twenty_consecutive_product_failures_throw()
    {
        var msi = new FakeMsiApi();
        for (int i = 0; i < 20; i++)
            msi.AddProduct($"{{bad{i}}}", result: 1603);

        await Assert.ThrowsAsync<LocalisedInvalidOperationException>(() => Run(msi));
    }

    // ---- The abandonment breadcrumb is budgeted ----
    //
    // What makes it one entry per product is a property of the registration, not
    // of a product: a SID the enumerator emits and then rejects as input refuses
    // every index for every product recorded under it. crash.log holds 512 KB
    // with a single archive, and each of these carries a message and a stack
    // trace, so the machine that most needs its crash history is the one whose
    // history this evicts.

    /// <summary>
    /// 200 products whose patch enumeration refuses at every index. Each one
    /// abandons after the tolerance and each abandonment is worth recording, but
    /// they are 200 restatements of one condition.
    /// </summary>
    [Fact]
    public async Task Two_hundred_abandoned_patch_enumerations_do_not_write_two_hundred_entries()
    {
        var written = new List<Exception>();
        var msi = new FakeMsiApi();
        for (int i = 0; i < 200; i++)
        {
            msi.AddProduct($"{{p{i:D3}}}");
            msi.SetProductProperty($"{{p{i:D3}}}", "LocalPackage", $@"C:\Windows\Installer\p{i:D3}.msi");
            msi.PatchEnumResult[$"{{p{i:D3}}}"] = InvalidParameter;
        }

        var result = await new InstallerQueryService(msi, NoFallback, written.Add)
            .GetRegisteredPackagesAsync();

        Assert.Equal(200, result.UnaccountedProductCount);

        // Twenty in full plus the one closing entry, against the 200 the
        // unbudgeted form wrote.
        Assert.Equal(21, written.Count);
        Assert.Equal(20, written.Count(e => e.Message.Contains("Patch enumeration abandoned", StringComparison.Ordinal)));

        var closing = written[^1].Message;
        Assert.Contains("Patch enumeration: 180 further failures were not logged individually",
            closing, StringComparison.Ordinal);
        // The trail has to be true of THIS caller: a suppressed abandonment
        // takes the product's identity with it and the user-facing notice
        // carries none.
        Assert.Contains("recorded nowhere else", closing, StringComparison.Ordinal);
    }

    /// <summary>
    /// The same storm with one product failing the OTHER way, late. Both arms
    /// synthesise an InvalidOperationException carrying the same HRESULT, so
    /// without the cause strings the budget's escape hatch would see one cause
    /// and swallow the second kind entirely.
    /// </summary>
    [Fact]
    public async Task A_second_kind_of_abandonment_is_logged_however_late_it_arrives()
    {
        var written = new List<Exception>();
        var msi = new FakeMsiApi();
        for (int i = 0; i < 200; i++)
        {
            var code = $"{{p{i:D3}}}";
            msi.AddProduct(code);
            msi.SetProductProperty(code, "LocalPackage", $@"C:\Windows\Installer\p{i:D3}.msi");

            if (i == 150)
                // Rows the API returns as success carrying an empty GUID: the
                // other abandonment arm, well past the budget.
                msi.PatchCodes[code] = Enumerable.Repeat("", 20).ToList();
            else
                msi.PatchEnumResult[code] = InvalidParameter;
        }

        await new InstallerQueryService(msi, NoFallback, written.Add).GetRegisteredPackagesAsync();

        // The empty-GUID arm reports Success as its last error, which is what
        // separates the two in the entries themselves.
        Assert.Contains(written, e => e.Message.Contains("{p150}", StringComparison.Ordinal));
        Assert.Contains(written, e => e.Message.Contains("last error code 0,", StringComparison.Ordinal));
    }

    // ---- An incomplete enumeration withholds the removable class ----
    //
    // A tolerated skip costs one product's patch claims, and a patch is cached
    // once and shared across the products holding it, so the product behind a
    // skipped row may be the one that still has a removable-looking patch
    // applied. Its identity is unknowable (a failed row's product code is
    // undefined), so no narrower rule is available than withholding the class.
    // Each of the four ways a row can be lost reaches the same demotion.

    [Fact]
    public async Task A_clean_enumeration_keeps_its_removable_verdicts()
    {
        const string dead = @"C:\Windows\Installer\clean-dead.msp";
        var msi = new FakeMsiApi();
        msi.AddProduct("{A}");
        msi.AddPatch("{A}", "{P}", localPackage: dead, state: "2", uninstallable: "0");

        var result = await Run(msi);

        Assert.True(Assert.Single(result.Packages, r => r.LocalPackagePath == dead).IsRemovable);
        Assert.Equal(0, result.UnaccountedProductCount);
        Assert.False(result.RecordsIncomplete);
    }

    [Fact]
    public async Task A_skipped_product_row_withholds_every_removable_verdict()
    {
        const string dead = @"C:\Windows\Installer\skipped-product.msp";
        var msi = new FakeMsiApi();
        msi.AddProduct("{A}");
        msi.AddPatch("{A}", "{P}", localPackage: dead, state: "2", uninstallable: "0");
        // Product B is unreadable, so its patches are never enumerated and its
        // claim on {P} (which may be Applied there) never reaches the merge.
        msi.AddProduct("{B}", result: 1603 /* ERROR_INSTALL_FAILURE */);

        var result = await Run(msi);

        var row = Assert.Single(result.Packages, r => r.LocalPackagePath == dead);
        Assert.False(row.IsRemovable);
        Assert.True(row.RemovableWithheld);
        Assert.Equal(1, result.UnaccountedProductCount);
    }

    [Fact]
    public async Task A_skipped_patch_row_withholds_every_removable_verdict()
    {
        // The same corridor reached through a product whose own row read fine:
        // B enumerates, but one of its patch rows fails, so whatever that row
        // named is missing from B's claims.
        const string dead = @"C:\Windows\Installer\skipped-patch.msp";
        var msi = new FakeMsiApi();
        msi.AddProduct("{A}");
        msi.AddPatch("{A}", "{P}", localPackage: dead, state: "2", uninstallable: "0");
        msi.AddProduct("{B}");
        msi.AddPatch("{B}", "{Q}", localPackage: @"C:\Windows\Installer\other.msp", state: "1", uninstallable: "1");
        msi.PatchRowResult[("{B}", 0)] = 1603;

        var result = await Run(msi);

        var row = Assert.Single(result.Packages, r => r.LocalPackagePath == dead);
        Assert.False(row.IsRemovable);
        Assert.True(row.RemovableWithheld);
        Assert.Equal(1, result.UnaccountedProductCount);
    }

    [Fact]
    public async Task An_empty_product_guid_withholds_every_removable_verdict()
    {
        const string dead = @"C:\Windows\Installer\empty-product.msp";
        var msi = new FakeMsiApi();
        msi.AddProduct("{A}");
        msi.AddPatch("{A}", "{P}", localPackage: dead, state: "2", uninstallable: "0");
        msi.AddProduct("");   // Success return, no GUID written: the row is lost

        var result = await Run(msi);

        Assert.False(Assert.Single(result.Packages, r => r.LocalPackagePath == dead).IsRemovable);
        Assert.Equal(1, result.UnaccountedProductCount);
    }

    [Fact]
    public async Task An_empty_patch_guid_withholds_every_removable_verdict()
    {
        const string dead = @"C:\Windows\Installer\empty-patch.msp";
        var msi = new FakeMsiApi();
        msi.AddProduct("{A}");
        msi.AddPatch("{A}", "{P}", localPackage: dead, state: "2", uninstallable: "0");
        msi.AddProduct("{B}");
        msi.PatchCodes["{B}"] = new() { "" };   // Success return, no GUID written

        var result = await Run(msi);

        Assert.False(Assert.Single(result.Packages, r => r.LocalPackagePath == dead).IsRemovable);
        Assert.Equal(1, result.UnaccountedProductCount);
    }

    [Fact]
    public async Task Unreadable_products_count_a_lost_product_row_and_a_lost_patch_row_alike()
    {
        // Both leave the same hole (a product whose claims are short), so the
        // count the user reads adds them together. A product is counted once
        // however many of its patch rows failed.
        var msi = new FakeMsiApi();
        msi.AddProduct("{bad}", result: 1603);
        msi.AddProduct("{B}");
        // B's own package, so the run yields a claim. Without one the scan ends
        // with an empty set and fails as an empty installer database before it
        // can report a count; that this test passed regardless was the live
        // registry of whichever machine ran it filling the set.
        msi.SetProductProperty("{B}", "LocalPackage", @"C:\Windows\Installer\b.msi");
        msi.AddPatch("{B}", "{Q}", localPackage: @"C:\Windows\Installer\q.msp", state: "1", uninstallable: "1");
        msi.AddPatch("{B}", "{R}", localPackage: @"C:\Windows\Installer\r.msp", state: "1", uninstallable: "1");
        msi.PatchRowResult[("{B}", 0)] = 1603;
        msi.PatchRowResult[("{B}", 1)] = 1603;

        var result = await Run(msi);

        Assert.Equal(2, result.UnaccountedProductCount);
    }

    [Fact]
    public async Task Withholding_the_removable_class_leaves_orphan_detection_alone()
    {
        // The bounded-cost claim the user-facing copy makes ("Orphaned files are
        // not affected"): a withheld scan still carries every registered path, so
        // the walk still has everything it needs to tell an orphan from a
        // registered file.
        const string productPackage = @"C:\Windows\Installer\kept.msi";
        var msi = new FakeMsiApi();
        msi.AddProduct("{A}");
        msi.SetProductProperty("{A}", "LocalPackage", productPackage);
        msi.AddProduct("{bad}", result: 1603);

        var result = await Run(msi);

        Assert.Contains(result.Packages, r => r.LocalPackagePath == productPackage);
    }

    // ---- A failed LocalPackage read loses a claim the same way a lost row does ----
    //
    // The other three properties degrade safely when they cannot be read: an
    // unreadable State leaves patchState 0 and an unreadable Uninstallable leans
    // non-removable, so the row still merges as "needed". LocalPackage is the
    // property that CARRIES the claim, so a failed read of it does not degrade
    // the row, it deletes it. That is the same hole as a skipped enumeration
    // row, and it reaches the same count and the same withholding.
    //
    // What makes the discrimination possible is that a record with no cached
    // package and a record that cannot be read return different codes. A probe
    // of 136 products and 2 patches (Windows 10.0.26200, msi.dll 5.0.26100.7920,
    // 2026-07-18) found an absent property returns ERROR_UNKNOWN_PROPERTY and an
    // unreadable product returns a real error, never a zero-length success.

    [Fact]
    public async Task A_failed_product_LocalPackage_read_withholds_every_removable_verdict()
    {
        // B's row enumerates fine and its package path cannot be read, so B's
        // claim on whatever it holds never reaches the merge. B may be the
        // product still holding A's superseded-looking patch.
        const string dead = @"C:\Windows\Installer\failed-product-read.msp";
        var msi = new FakeMsiApi();
        msi.AddProduct("{A}");
        msi.AddPatch("{A}", "{P}", localPackage: dead, state: "2", uninstallable: "0");
        msi.AddProduct("{B}");
        msi.ProductPropertyResult[("{B}", "LocalPackage")] = BadConfiguration;

        var result = await Run(msi);

        var row = Assert.Single(result.Packages, r => r.LocalPackagePath == dead);
        Assert.False(row.IsRemovable);
        Assert.True(row.RemovableWithheld);
        Assert.Equal(1, result.UnaccountedProductCount);
    }

    [Fact]
    public async Task A_failed_patch_LocalPackage_read_withholds_every_removable_verdict()
    {
        // The shared-patch chain: one .msp cached once, Superseded under A and
        // Applied under B. B's row for it comes back and the path it names does
        // not, so the Applied claim that keeps the file alive is lost silently.
        const string shared = @"C:\Windows\Installer\shared-failed-read.msp";
        var msi = new FakeMsiApi();
        msi.AddProduct("{A}");
        msi.AddPatch("{A}", "{P}", localPackage: shared, state: "2", uninstallable: "0");
        msi.AddProduct("{B}");
        msi.PatchCodes["{B}"] = new() { "{P}" };
        msi.PatchPropertyResult[("{P}", "{B}", "LocalPackage")] = BadConfiguration;

        var result = await Run(msi);

        var row = Assert.Single(result.Packages, r => r.LocalPackagePath == shared);
        Assert.False(row.IsRemovable);
        Assert.True(row.RemovableWithheld);
        Assert.Equal(1, result.UnaccountedProductCount);
    }

    [Fact]
    public async Task One_product_counts_once_however_many_of_its_reads_failed()
    {
        // The count is programs, not failures. B loses its package read and a
        // patch row: still one program whose records came back short.
        var msi = new FakeMsiApi();
        msi.AddProduct("{A}");
        msi.SetProductProperty("{A}", "LocalPackage", @"C:\Windows\Installer\a.msi");
        msi.AddProduct("{B}");
        msi.ProductPropertyResult[("{B}", "LocalPackage")] = BadConfiguration;
        msi.PatchCodes["{B}"] = new() { "{Q}" };
        msi.PatchRowResult[("{B}", 0)] = 1603;

        var result = await Run(msi);

        Assert.Equal(1, result.UnaccountedProductCount);
    }

    // ---- ...and a benign absence does NOT withhold ----
    //
    // The false-fire control. Products legitimately have no cached package, and
    // MsiPatchFilter.All includes Registered patches, which have none either. If
    // an absence counted as a failure the withholding would fire on effectively
    // every machine and superseded-patch detection would be dead.

    [Fact]
    public async Task A_product_with_no_cached_package_does_not_withhold()
    {
        const string dead = @"C:\Windows\Installer\absent-product-package.msp";
        var msi = new FakeMsiApi();
        msi.AddProduct("{A}");
        msi.AddPatch("{A}", "{P}", localPackage: dead, state: "2", uninstallable: "0");
        msi.AddProduct("{B}");
        msi.ProductPropertyResult[("{B}", "LocalPackage")] = UnknownProperty;

        var result = await Run(msi);

        Assert.True(Assert.Single(result.Packages, r => r.LocalPackagePath == dead).IsRemovable);
        Assert.Equal(0, result.UnaccountedProductCount);
    }

    [Fact]
    public async Task A_registered_patch_with_no_cached_package_does_not_withhold()
    {
        const string dead = @"C:\Windows\Installer\absent-patch-package.msp";
        var msi = new FakeMsiApi();
        msi.AddProduct("{A}");
        msi.AddPatch("{A}", "{P}", localPackage: dead, state: "2", uninstallable: "0");
        msi.AddProduct("{B}");
        msi.PatchCodes["{B}"] = new() { "{R}" };
        msi.PatchPropertyResult[("{R}", "{B}", "LocalPackage")] = UnknownProperty;

        var result = await Run(msi);

        Assert.True(Assert.Single(result.Packages, r => r.LocalPackagePath == dead).IsRemovable);
        Assert.Equal(0, result.UnaccountedProductCount);
    }

    [Fact]
    public async Task A_readable_but_empty_LocalPackage_does_not_withhold()
    {
        // The other benign shape: a success carrying zero characters. The probe
        // did not see one, and the allowlist covers it anyway, because guessing
        // which of the two shapes a real absence takes is exactly the guess that
        // must not decide a file's fate.
        const string dead = @"C:\Windows\Installer\empty-product-package.msp";
        var msi = new FakeMsiApi();
        msi.AddProduct("{A}");
        msi.AddPatch("{A}", "{P}", localPackage: dead, state: "2", uninstallable: "0");
        msi.AddProduct("{B}");   // no LocalPackage set: Success, zero length

        var result = await Run(msi);

        Assert.True(Assert.Single(result.Packages, r => r.LocalPackagePath == dead).IsRemovable);
        Assert.Equal(0, result.UnaccountedProductCount);
    }

    [Fact]
    public async Task A_failed_ProductName_read_does_not_withhold()
    {
        // Scoped to LocalPackage on purpose. A name that cannot be read costs a
        // display string, not a claim, and counting it would withhold on
        // machines where nothing that matters went wrong.
        const string dead = @"C:\Windows\Installer\no-name.msp";
        var msi = new FakeMsiApi();
        msi.AddProduct("{A}");
        msi.AddPatch("{A}", "{P}", localPackage: dead, state: "2", uninstallable: "0");
        msi.AddProduct("{B}");
        msi.SetProductProperty("{B}", "LocalPackage", @"C:\Windows\Installer\b.msi");
        msi.ProductPropertyResult[("{B}", "ProductName")] = BadConfiguration;

        var result = await Run(msi);

        Assert.True(Assert.Single(result.Packages, r => r.LocalPackagePath == dead).IsRemovable);
        Assert.Equal(0, result.UnaccountedProductCount);
        Assert.Contains(result.Packages, r => r.LocalPackagePath == @"C:\Windows\Installer\b.msi");
    }

    // ---- Both sources degraded at once refuses the scan ----
    //
    // Withholding the removable class answers a short API enumeration because
    // the registry fallback still contributes the lost product's paths as
    // non-removable rows, which is what keeps its cached file out of the orphan
    // list. When the fallback is failing reads of its own that recovery is no
    // longer established, and the scan would offer a file as an orphan under a
    // notice saying orphaned files are not affected.

    [Fact]
    public async Task Both_sources_degraded_refuses_the_scan()
    {
        var msi = new FakeMsiApi();
        msi.AddProduct("{A}");
        msi.SetProductProperty("{A}", "LocalPackage", @"C:\Windows\Installer\a.msi");
        msi.AddProduct("{B}", result: 1603);

        var ex = await Assert.ThrowsAsync<LocalisedInvalidOperationException>(
            () => Run(msi, fallbackFailures: 1));

        Assert.Equal(Strings.Error_ScanRecordsUnreadable, ex.Message);
    }

    [Fact]
    public async Task A_short_enumeration_with_a_clean_fallback_still_scans()
    {
        // One source short is the state the withholding was built for, and it
        // must stay a completed scan: refusing here would take orphan cleanup
        // away over a condition that does not threaten it.
        const string dead = @"C:\Windows\Installer\one-source-short.msp";
        var msi = new FakeMsiApi();
        msi.AddProduct("{A}");
        msi.AddPatch("{A}", "{P}", localPackage: dead, state: "2", uninstallable: "0");
        msi.AddProduct("{B}", result: 1603);

        var result = await Run(msi, fallbackFailures: 0);

        Assert.True(Assert.Single(result.Packages, r => r.LocalPackagePath == dead).RemovableWithheld);
        Assert.Equal(1, result.UnaccountedProductCount);
    }

    [Fact]
    public async Task A_failing_fallback_with_a_clean_enumeration_still_scans()
    {
        // The control that matters most in the other direction. The fallback is
        // a second source, not a check on the first: if the API read every
        // product cleanly, a bad key in UserData proves nothing about the
        // verdicts and must not cost superseded-patch detection.
        const string dead = @"C:\Windows\Installer\fallback-noise.msp";
        var msi = new FakeMsiApi();
        msi.AddProduct("{A}");
        msi.AddPatch("{A}", "{P}", localPackage: dead, state: "2", uninstallable: "0");

        var result = await Run(msi, fallbackFailures: 3);

        Assert.True(Assert.Single(result.Packages, r => r.LocalPackagePath == dead).IsRemovable);
        Assert.Equal(0, result.UnaccountedProductCount);
    }

    // ---- Claim-time path normalisation ----

    [Theory]
    // Doubled separator, the shape a naive string concatenation writes.
    [InlineData(@"C:\Windows\\Installer\normalised.msi")]
    // Forward slashes, which Windows accepts everywhere and the walk never emits.
    [InlineData(@"C:/Windows/Installer/normalised.msi")]
    // A relative segment, left behind by a path built from a base plus a suffix.
    [InlineData(@"C:\Windows\Temp\..\Installer\normalised.msi")]
    // The long-path prefix, which GetFullPath deliberately leaves alone.
    [InlineData(@"\\?\C:\Windows\Installer\normalised.msi")]
    // The NT object form. GetFullPath reads that leading separator as rooted on
    // whichever drive the process is running from, so unstripped the claim named
    // a file nowhere and the cached one was offered as an orphan.
    [InlineData(@"\??\C:\Windows\Installer\normalised.msi")]
    public async Task A_claim_is_normalised_to_the_spelling_the_folder_walk_produces(string registeredAs)
    {
        // Orphanhood is string equality against the walked paths while existence
        // is the filesystem's answer, so a registered value in any spelling the
        // walk never produces counted the file as needed on one side and offered
        // the same physical file for removal on the other.
        const string walked = @"C:\Windows\Installer\normalised.msi";
        var msi = new FakeMsiApi();
        msi.AddProduct("{A}");
        msi.SetProductProperty("{A}", "LocalPackage", registeredAs);

        var result = await Run(msi);

        Assert.Equal(walked, Assert.Single(result.Packages).LocalPackagePath);
    }

    [Fact]
    public async Task A_patch_claim_is_normalised_the_same_way()
    {
        // The patch side carries the removable verdict, so a spelling that
        // missed here would offer a still-needed .msp rather than merely
        // mis-file an .msi.
        const string walked = @"C:\Windows\Installer\patch.msp";
        var msi = new FakeMsiApi();
        msi.AddProduct("{A}");
        msi.AddPatch("{A}", "{P}", localPackage: @"C:\Windows\\Installer\patch.msp",
            state: "2", uninstallable: "0");

        var result = await Run(msi);

        Assert.Equal(walked, Assert.Single(result.Packages).LocalPackagePath);
    }

    [Theory]
    // A volume-GUID value and the device forms beside it. None has a drive
    // letter behind the prefix, so none can have it taken off: what was left
    // had no root at all, and GetFullPath completed it from the process working
    // directory, which is a different answer from the GUI and from the command
    // line for the same registration.
    [InlineData(@"\\?\Volume{9c3a1d2e-0000-0000-0000-100000000000}\Windows\Installer\vol.msi")]
    [InlineData(@"\??\Volume{9c3a1d2e-0000-0000-0000-100000000000}\Windows\Installer\vol.msi")]
    [InlineData(@"\\?\GLOBALROOT\Device\HarddiskVolume3\Windows\Installer\vol.msi")]
    public async Task A_value_with_no_drive_letter_behind_its_prefix_keeps_the_prefix(string registeredAs)
    {
        var msi = new FakeMsiApi();
        msi.AddProduct("{A}");
        msi.SetProductProperty("{A}", "LocalPackage", registeredAs);

        var claimed = Assert.Single((await Run(msi)).Packages).LocalPackagePath;

        // Asserted at the two ends rather than as equality with the input: what
        // matters is that the value still names a volume and still names the
        // file, not that GetFullPath left every character of an extended path
        // alone. Keeping the prefix is what rules out the old answer, which
        // began at whatever drive and folder the process was started from.
        Assert.StartsWith(registeredAs.Substring(0, 4), claimed, StringComparison.Ordinal);
        Assert.EndsWith(@"\vol.msi", claimed, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task A_value_that_cannot_be_normalised_is_claimed_exactly_as_returned()
    {
        // GetFullPath refuses an embedded null. The claim must survive it
        // anyway: dropping the row would turn a spelling nobody can improve
        // into a file with no claim on it at all, which is an orphan.
        const string unimprovable = "C:\\Windows\\Installer\\bad\0name.msi";
        var msi = new FakeMsiApi();
        msi.AddProduct("{A}");
        msi.SetProductProperty("{A}", "LocalPackage", unimprovable);

        var result = await Run(msi);

        Assert.Equal(unimprovable, Assert.Single(result.Packages).LocalPackagePath);
    }

    // ---- The SID-buffer retry's own return code ----

    [Fact]
    public async Task AccessDenied_from_the_sid_retry_refuses_the_scan()
    {
        // The refusal check has to cover the retry's return code too, not just
        // the first call's. A refusal coming back from the second call and
        // falling into the tolerated-failure branch would count and demote the
        // row, leaving the scan reporting itself as merely short of a record
        // when Windows had refused it outright. Near unreachable in practice
        // (the retry only runs for a SID past 256 characters), so this pins the
        // refusal contract rather than a field failure.
        var msi = new FakeMsiApi();
        msi.AddProduct("{A}");
        msi.ProductSidRetryResult[0] = AccessDenied;

        await Assert.ThrowsAsync<LocalisedAccessException>(() => Run(msi));
    }

    [Fact]
    public async Task A_successful_sid_retry_still_yields_its_row()
    {
        // The other side: moving the classification below the retry must not
        // cost the retry's whole purpose, which is that a row needing a bigger
        // SID buffer still lands.
        const string pkg = @"C:\Windows\Installer\after-retry.msi";
        var msi = new FakeMsiApi();
        msi.AddProduct("{A}");
        msi.SetProductProperty("{A}", "LocalPackage", pkg);
        msi.ProductSidRetryResult[0] = Success;

        var result = await Run(msi);

        Assert.Equal(pkg, Assert.Single(result.Packages).LocalPackagePath);
        Assert.Equal(0, result.UnaccountedProductCount);
    }

    [Fact]
    public async Task The_sid_retry_buffer_holds_one_more_character_than_the_reported_length()
    {
        // msi.dll reports the required SID length on MoreData EXCLUDING the
        // null terminator and documents the retry buffer as that count plus
        // one. An exact-size retry is one character short, draws MoreData
        // again, and the row it exists to rescue is refused instead.
        var msi = new FakeMsiApi();
        msi.AddProduct("{A}");
        msi.SetProductProperty("{A}", "LocalPackage", @"C:\Windows\Installer\sized.msi");
        msi.ProductSidRetryResult[0] = Success;

        var result = await Run(msi);

        Assert.Equal(65u, msi.RetrySidLengthSeen);
        Assert.Single(result.Packages);
    }

    // ---- The census: instrumentation for the opt-in report ----
    //
    // Every test below pins a number that decides nothing, which is exactly why
    // they are worth having: a counter with no consumer inside the app has no
    // behaviour to fail visibly when it drifts, so the only thing between it and
    // a silently wrong population figure is a test that reads it.

    [Fact]
    public async Task The_census_carries_the_three_withheld_terms_apart_from_each_other()
    {
        // The composite the app withholds on is the first term plus the LARGER of
        // the other two, so a machine can be short by both routes and the terms
        // must survive that combination separately. Here: one product's records
        // are short, the registry names ten products against the API's two, and
        // one cached file on disk is claimed by the registry alone.
        var msi = new FakeMsiApi();
        msi.AddProduct("{A}");
        msi.AddProduct("{B}");
        msi.SetProductProperty("{A}", "LocalPackage", @"C:\Windows\Installer\a.msi");
        msi.SetProductProperty("{B}", "LocalPackage", @"C:\Windows\Installer\b.msi");
        msi.ProductPropertyResult[("{B}", "LocalPackage")] = BadConfiguration;

        var result = await RunAgainstRegistry(msi, registryProducts: 10, unclaimedProductFiles: 4);

        Assert.Equal(1, result.Census.UnreadableProducts);
        // ZERO skipped rows, and it is not the same number as the one above: a
        // product whose row came back and whose value would not read is still a
        // product the API returned, so it is not missing from the headcount. The
        // shortfall arithmetic subtracts THIS, which is why it travels separately.
        Assert.Equal(0, result.Census.SkippedProductRows);
        Assert.Equal(10, result.Census.RegistryProductKeys);
        Assert.Equal(2, result.Census.ProductCount);
        Assert.Equal(4, result.Census.UnclaimedProductFiles);
        Assert.Equal(0, result.Census.UnclaimedPatchFiles);

        // The composite the app acts on is the unreadable count plus the larger
        // of the two derived terms, and both of those reproduce from the tallies:
        // the shortfall is 10 - 2 - 0, and the never-claimed estimate is 4 - 1.
        Assert.Equal(1 + Math.Max(10 - 2 - 0, 4 - 1), result.UnaccountedProductCount);
    }

    [Fact]
    public async Task A_healthy_enumeration_leaves_every_census_term_at_zero()
    {
        // The control for the row above. Without it a census that answered zero
        // to everything on every machine would look like a clean population
        // rather than like a counter that never fires.
        var msi = new FakeMsiApi();
        msi.AddProduct("{A}");
        msi.SetProductProperty("{A}", "LocalPackage", @"C:\Windows\Installer\a.msi");

        var result = await Run(msi);

        Assert.Equal(0, result.Census.UnreadableProducts);
        Assert.Equal(0, result.Census.SkippedProductRows);
        Assert.Equal(0, result.Census.UnclaimedProductFiles);
        Assert.Equal(0, result.Census.UnclaimedPatchFiles);
        Assert.Equal(0, result.Census.UnreadablePatchStates);
        Assert.Equal(0, result.Census.NonStringLocalPackageValues);
    }

    [Fact]
    public async Task A_registry_that_holds_two_more_products_than_the_API_returned_still_reports_both_counts()
    {
        // THE CASE THE DERIVED TERM CANNOT EXPRESS. The app's tolerance band
        // absorbs a difference of two outright, so the shortfall it computes here
        // is zero and this machine is indistinguishable from a clean one on that
        // figure alone. Both headcounts travel, so it is distinguishable in the
        // report, which is the whole reason the tallies go instead of the term.
        var msi = new FakeMsiApi();
        msi.AddProduct("{A}");
        msi.SetProductProperty("{A}", "LocalPackage", @"C:\Windows\Installer\a.msi");

        var result = await RunAgainstRegistry(msi, registryProducts: 3);

        Assert.Equal(3, result.Census.RegistryProductKeys);
        Assert.Equal(1, result.Census.ProductCount);
        // The app itself withheld nothing: the band absorbed it.
        Assert.Equal(0, result.UnaccountedProductCount);
    }

    [Fact]
    public async Task A_patch_whose_state_read_fails_is_counted_kept_and_marked()
    {
        // Three halves, and the marking is the one that was missing. The failed
        // read leaves the patch non-removable, so the file is kept; the count
        // says how often the machine takes that path; and the flag is what stops
        // a later re-verify reading the same failure as a product's live claim
        // and telling the user so. Withheld stays false because nothing here was
        // ever read as removable, which is the other flag's meaning.
        var msi = new FakeMsiApi();
        msi.AddProduct("{A}");
        msi.AddPatch("{A}", "{P}", localPackage: @"C:\Windows\Installer\p.msp", state: "2", uninstallable: "0");
        msi.PatchPropertyResult[("{P}", "{A}", "State")] = BadConfiguration;

        var result = await Run(msi);

        Assert.Equal(1, result.Census.UnreadablePatchStates);
        var row = Assert.Single(result.Packages, r => r.LocalPackagePath.EndsWith("p.msp", StringComparison.Ordinal));
        Assert.False(row.IsRemovable);
        Assert.True(row.VerdictUnreadable);
        Assert.False(row.RemovableWithheld);
    }

    [Fact]
    public async Task An_unreadable_uninstallable_read_counts_and_marks_on_the_same_terms()
    {
        // Either read failing leaves the same gap, and both the count and the
        // flag say how often that happens rather than which of the two it was:
        // the two reads answer one question between them and neither alone
        // decides the verdict. A fix keyed on State alone would leave the other
        // half of the same question reporting a claim nobody read.
        var msi = new FakeMsiApi();
        msi.AddProduct("{A}");
        msi.AddPatch("{A}", "{P}", localPackage: @"C:\Windows\Installer\p.msp", state: "2", uninstallable: "0");
        msi.PatchPropertyResult[("{P}", "{A}", "Uninstallable")] = BadConfiguration;

        var result = await Run(msi);

        Assert.Equal(1, result.Census.UnreadablePatchStates);
        var row = Assert.Single(result.Packages, r => r.LocalPackagePath.EndsWith("p.msp", StringComparison.Ordinal));
        Assert.False(row.IsRemovable);
        Assert.True(row.VerdictUnreadable);
    }

    [Fact]
    public async Task A_read_that_answers_leaves_the_row_unmarked()
    {
        // The control the two above need: the flag is set by a failed read and
        // not by the ordinary non-removable outcome, or every applied patch on
        // every machine would be reported as a record nobody could read.
        var msi = new FakeMsiApi();
        msi.AddProduct("{A}");
        msi.AddPatch("{A}", "{P}", localPackage: @"C:\Windows\Installer\p.msp", state: "1", uninstallable: "1");

        var result = await Run(msi);

        Assert.Equal(0, result.Census.UnreadablePatchStates);
        var row = Assert.Single(result.Packages, r => r.LocalPackagePath.EndsWith("p.msp", StringComparison.Ordinal));
        Assert.False(row.IsRemovable);
        Assert.False(row.VerdictUnreadable);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task A_shared_patch_one_product_really_claims_is_not_reported_as_unread(bool unreadableFirst)
    {
        // One patch, two products, one of them answering and one not. The file is
        // kept whichever way round the enumeration reaches them, and so is the
        // cause: a product that positively still holds the patch is the finding,
        // and the failed read beside it is not a competing one. Both orders are
        // driven because the merge keeps one row per path, so an order-dependent
        // rule would make what the app SAYS about this file a coin flip.
        const string shared = @"C:\Windows\Installer\shared.msp";
        var msi = new FakeMsiApi();
        // {A} is always the product whose read fails; only the order moves.
        msi.AddProduct(unreadableFirst ? "{A}" : "{B}");
        msi.AddProduct(unreadableFirst ? "{B}" : "{A}");
        msi.AddPatch("{A}", "{P}", localPackage: shared, state: "2", uninstallable: "0");
        msi.AddPatch("{B}", "{P}", localPackage: shared, state: "1", uninstallable: "1");
        msi.PatchPropertyResult[("{P}", "{A}", "State")] = BadConfiguration;

        var result = await Run(msi);

        var row = Assert.Single(result.Packages, r => r.LocalPackagePath.EndsWith("shared.msp", StringComparison.Ordinal));
        Assert.False(row.IsRemovable);
        Assert.False(row.VerdictUnreadable);
    }

    [Fact]
    public async Task The_census_reports_the_product_and_patch_claim_counts()
    {
        // Per CLAIM rather than per patch: a patch applied to two products is two
        // claims, which is what makes the ratio describe the enumeration's work.
        var msi = new FakeMsiApi();
        msi.AddProduct("{A}");
        msi.AddProduct("{B}");
        msi.SetProductProperty("{A}", "LocalPackage", @"C:\Windows\Installer\a.msi");
        msi.SetProductProperty("{B}", "LocalPackage", @"C:\Windows\Installer\b.msi");
        msi.AddPatch("{A}", "{P}", localPackage: @"C:\Windows\Installer\p.msp", state: "1", uninstallable: "1");
        msi.AddPatch("{B}", "{P}", localPackage: @"C:\Windows\Installer\p.msp", state: "1", uninstallable: "1");

        var result = await Run(msi);

        Assert.Equal(2, result.Census.ProductCount);
        Assert.Equal(2, result.Census.PatchClaimCount);
        // One path, two claims: the merge keeps a single row and the ratio does
        // not, which is the whole reason they are separate numbers.
        Assert.Single(result.Packages, r => r.LocalPackagePath.EndsWith("p.msp", StringComparison.Ordinal));
    }

    [Theory]
    [InlineData(@"C:\Windows\Installer\1a2b3c4.msi", false)]   // seven, an 8dot3-shaped name
    [InlineData(@"C:\Windows\Installer\12345678.msi", false)]  // exactly eight, still short enough
    [InlineData(@"C:\Windows\Installer\123456789.msi", true)]  // nine
    [InlineData(@"C:\Windows\Installer\a.b.cdefghij.msi", true)] // last dot is the extension
    [InlineData(@"C:\Windows\Installer\noextension", true)]    // no dot at all: the leaf is the stem
    [InlineData(@"\\?\C:\Windows\Installer\1a2b3c4.msi", false)] // the long-path prefix is not the leaf
    [InlineData("", false)]
    public void A_long_leaf_stem_is_measured_to_the_last_dot_of_the_leaf(string path, bool expected)
    {
        // The separator search is explicit rather than Path.GetFileName, so this
        // answers the same on any host the suite runs on; a framework helper
        // would read the whole Windows path as the leaf anywhere but Windows and
        // every row would count.
        Assert.Equal(expected, InstallerQueryService.HasLongLeafStem(path));
    }

    [Fact]
    public async Task The_census_counts_the_claimed_paths_whose_leaf_cannot_be_a_short_name()
    {
        var msi = new FakeMsiApi();
        msi.AddProduct("{A}");
        msi.AddProduct("{B}");
        msi.SetProductProperty("{A}", "LocalPackage", @"C:\Windows\Installer\1a2b3c4.msi");
        msi.SetProductProperty("{B}", "LocalPackage", @"C:\Windows\Installer\a-very-long-name.msi");

        var result = await Run(msi);

        Assert.Equal(2, result.Census.ProductCount);
        Assert.Equal(1, result.Census.LongLeafStemCount);
    }

    [Fact]
    public async Task A_non_string_cached_path_value_is_counted_separately_and_still_counts_as_a_failure()
    {
        // Item M's own counter. It is a SUBSET of the fallback's failure count
        // rather than a term beside it, because that count feeds the
        // degraded-sources refusal and narrowing a shipped safety gate is not an
        // instrumentation change's business. The failure count itself is not on
        // the result to assert; what the row after this one pins is the gate
        // still firing on it.
        var msi = new FakeMsiApi();
        msi.AddProduct("{A}");
        msi.SetProductProperty("{A}", "LocalPackage", @"C:\Windows\Installer\a.msi");

        var result = await new InstallerQueryService(msi,
                (_, _) => new InstallerQueryService.FallbackRead(
                    Failures: 1, ProductKeys: 1, NonStringLocalPackageValues: 1))
            .GetRegisteredPackagesAsync();

        Assert.Equal(1, result.Census.NonStringLocalPackageValues);
    }

    [Fact]
    public async Task A_non_string_cached_path_value_still_refuses_a_scan_whose_other_source_is_short()
    {
        // The safety half of the row above, and the reason the new counter was
        // added BESIDE the failure count rather than carved out of it. A value
        // that is there and is not a string is a read that failed, the
        // degraded-sources gate weighs reads that failed, and a scan short on both
        // sources is refused rather than reported. Splitting the counter without
        // this test would have quietly turned that refusal off.
        //
        // It also pins that a refusal carries no census at all, which is right: a
        // machine whose records this app could not read is the last machine whose
        // population figures are worth anything.
        var msi = new FakeMsiApi();
        msi.AddProduct("{A}");
        msi.SetProductProperty("{A}", "LocalPackage", @"C:\Windows\Installer\a.msi");
        msi.ProductPropertyResult[("{A}", "LocalPackage")] = BadConfiguration;

        await Assert.ThrowsAsync<LocalisedInvalidOperationException>(() =>
            new InstallerQueryService(msi,
                    (_, _) => new InstallerQueryService.FallbackRead(
                        Failures: 1, ProductKeys: 1, NonStringLocalPackageValues: 1))
                .GetRegisteredPackagesAsync());
    }

    /// <summary>
    /// Scriptable fake over <see cref="IMsiApi"/>. Reproduces the double-call
    /// buffer contract of msi.dll: a sizing call with a null buffer returns the
    /// char count (excluding the terminator) and MoreData; the second call
    /// writes the value and returns Success.
    /// </summary>
    private sealed class FakeMsiApi : IMsiApi
    {
        public List<(string Code, uint Result)> Products { get; } = new();
        public bool NeverEndProducts { get; set; }
        public string? NeverEndPatchesFor { get; set; }
        public Dictionary<string, List<string>> PatchCodes { get; } = new();
        public Dictionary<string, uint> PatchEnumResult { get; } = new();

        /// <summary>
        /// Fails ONE row of a product's patch enumeration, keyed by (product,
        /// index). Distinct from <see cref="PatchEnumResult"/>, which refuses the
        /// product's enumeration outright: this is the scattered-failure case the
        /// loop tolerates, where the rows either side of the bad one come back.
        /// </summary>
        public Dictionary<(string ProductCode, uint Index), uint> PatchRowResult { get; } = new();

        /// <summary>
        /// Forces a return code out of a property read, keyed by (product,
        /// property) and (patch, product, property). Needed because
        /// <see cref="DoubleCall"/> models an unset property as a readable empty
        /// value, which cannot express the distinction the LocalPackage reads
        /// turn on: a record that has no cached package and a record that could
        /// not be read both arrive as "" without it, and no test could reach the
        /// branch that tells them apart.
        /// </summary>
        /// <summary>
        /// Scripts the SID-buffer retry for one product row, keyed by index: the
        /// first EnumProducts call at that index reports MoreData, and the retry
        /// returns the value given here (Success meaning the row then comes back
        /// normally). The real API only asks for a bigger buffer for a SID past
        /// 256 characters, so nothing else in this fake can reach the retry, and
        /// what the retry RETURNS is the whole subject of the tests using it.
        /// </summary>
        public Dictionary<uint, uint> ProductSidRetryResult { get; } = new();

        /// <summary>
        /// The sidLength the retry call arrived with, so a test can pin the
        /// buffer contract: the sizing call reports 64 (excluding the null),
        /// so a correctly sized retry arrives with 65.
        /// </summary>
        public uint? RetrySidLengthSeen { get; private set; }

        private readonly HashSet<uint> _sidRetried = new();

        public Dictionary<(string ProductCode, string Property), uint> ProductPropertyResult { get; } = new();
        public Dictionary<(string PatchCode, string ProductCode, string Property), uint> PatchPropertyResult { get; } = new();

        private readonly Dictionary<(string, string), string> _productProps = new();
        private readonly Dictionary<(string, string, string), string> _patchProps = new();

        public void AddProduct(string code, uint result = Success) => Products.Add((code, result));

        public void SetProductProperty(string code, string property, string value) =>
            _productProps[(code, property)] = value;

        public void SetPatchProperty(string patchCode, string productCode, string property, string value) =>
            _patchProps[(patchCode, productCode, property)] = value;

        public void AddPatch(string productCode, string patchCode, string localPackage, string state, string? uninstallable)
        {
            (PatchCodes.TryGetValue(productCode, out var list) ? list : PatchCodes[productCode] = new()).Add(patchCode);
            SetPatchProperty(patchCode, productCode, "LocalPackage", localPackage);
            SetPatchProperty(patchCode, productCode, "State", state);
            if (uninstallable is not null)
                SetPatchProperty(patchCode, productCode, "Uninstallable", uninstallable);
        }

        private static void WriteCode(char[]? buffer, string code)
        {
            if (buffer is null) return;
            for (int i = 0; i < code.Length && i < buffer.Length - 1; i++) buffer[i] = code[i];
            // The caller zeroes the buffer each iteration, so an empty code
            // leaves an all-null buffer that reads back as "".
        }

        public uint EnumProducts(string? productCode, string? userSid, MsiInstallContext context, uint index,
            char[]? installedProductCode, out MsiInstallContext installedContext, char[]? sid, ref uint sidLength)
        {
            installedContext = MsiInstallContext.Machine;
            if (NeverEndProducts)
            {
                WriteCode(installedProductCode, "{FFFFFFFF-0000-0000-0000-000000000000}");
                return Success;
            }
            if (index >= Products.Count) return NoMoreItems;
            if (ProductSidRetryResult.TryGetValue(index, out var afterRetry))
            {
                // MoreData reports the required SID length EXCLUDING the
                // terminator, as msi.dll documents for pcchSid, so the
                // correct retry buffer is one larger than the report.
                if (_sidRetried.Add(index)) { sidLength = 64; return MoreData; }
                RetrySidLengthSeen = sidLength;
                if (afterRetry != Success) return afterRetry;
            }
            var (code, result) = Products[(int)index];
            if (result != Success) return result;
            WriteCode(installedProductCode, code);
            return Success;
        }

        public uint EnumPatches(string? productCode, string? userSid, MsiInstallContext context, MsiPatchFilter filter,
            uint index, char[]? patchCode, char[]? targetProductCode, out MsiInstallContext targetProductContext,
            char[]? targetUserSid, ref uint targetUserSidLength)
        {
            targetProductContext = MsiInstallContext.Machine;
            if (productCode is not null && productCode == NeverEndPatchesFor)
            {
                WriteCode(patchCode, "{FFFFFFFF-0000-0000-0000-000000000001}");
                return Success;
            }
            if (productCode is not null && PatchEnumResult.TryGetValue(productCode, out var err))
                return err;
            var list = (productCode is not null && PatchCodes.TryGetValue(productCode, out var l)) ? l : null;
            if (list is null || index >= list.Count) return NoMoreItems;
            if (productCode is not null && PatchRowResult.TryGetValue((productCode, index), out var rowErr))
                return rowErr;
            WriteCode(patchCode, list[(int)index]);
            return Success;
        }

        public uint GetProductInfo(string productCode, string? userSid, MsiInstallContext context, string property,
            char[]? value, ref uint valueLength)
        {
            // A forced code answers the sizing call, so the real API's second
            // call never happens either: nothing survives a failed first call.
            if (ProductPropertyResult.TryGetValue((productCode, property), out var forced))
            {
                valueLength = 0;
                return forced;
            }
            return DoubleCall(_productProps.GetValueOrDefault((productCode, property), ""), value, ref valueLength);
        }

        public uint GetPatchInfo(string patchCode, string productCode, string? userSid, MsiInstallContext context,
            string property, char[]? value, ref uint valueLength)
        {
            if (PatchPropertyResult.TryGetValue((patchCode, productCode, property), out var forced))
            {
                valueLength = 0;
                return forced;
            }
            return DoubleCall(_patchProps.GetValueOrDefault((patchCode, productCode, property), ""), value, ref valueLength);
        }

        private static uint DoubleCall(string val, char[]? value, ref uint valueLength)
        {
            if (val.Length == 0) { valueLength = 0; return Success; } // readable but empty
            if (value is null) { valueLength = (uint)val.Length; return MoreData; }
            int n = Math.Min(val.Length, value.Length);
            for (int i = 0; i < n; i++) value[i] = val[i];
            valueLength = (uint)val.Length;
            return Success;
        }
    }
}
