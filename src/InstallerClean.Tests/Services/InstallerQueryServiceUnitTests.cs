using InstallerClean.Helpers;
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
    /// record that simply does not carry the property returns, and it is not a
    /// failure.
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
    /// degraded ones wants. Naming no products also keeps it out of the
    /// comparison that looks for products the enumeration missed, which has
    /// nothing to compare and so recovers none and withholds for none.
    /// </summary>
    private static InstallerQueryService.FallbackRead NoFallback(
        Dictionary<string, RegisteredPackage> claimed, CancellationToken ct) => new(0, 0);

    /// <summary>
    /// What every registered patch row looks like after an enumeration: kept, no
    /// verdict granted, none withheld, and its state recorded while deciding
    /// nothing.
    ///
    /// ONE HELPER RATHER THAN THE ASSERTIONS SPELLED OUT AT EACH SITE, because
    /// the tests below that make this claim are each ABOUT something else: what a
    /// degraded enumeration counts, which rows survive it, which conditions add a
    /// product to the unaccounted total. Spelled out at every site, one of them
    /// drifting would read as a deliberate exception.
    ///
    /// The pairing is the point. <c>RemovableWithheld</c> being false here says
    /// there was no verdict to withhold, not that nothing was withheld, and the two
    /// look identical from a single assertion. What distinguishes a degraded run
    /// from a clean one is <c>UnaccountedProductCount</c>, which the tests assert
    /// for themselves.
    /// </summary>
    private static void AssertKeptWithNoVerdict(RegisteredPackage row, int expectedState)
    {
        Assert.False(row.IsRemovable);
        Assert.False(row.RemovableWithheld);
        Assert.Equal(expectedState, row.PatchState);
    }

    /// <summary>
    /// The row is kept and the WITHHOLDING IS THE POINT, not a side effect: the
    /// enumeration did not account for every product, and a scan in that state
    /// withholds the whole removable class rather than offering what it happened to
    /// see. Every test using this asserts a non-zero unaccounted count of its own,
    /// which is the condition that fires it.
    ///
    /// IT IS THE OPPOSITE CLAIM FROM <see cref="AssertKeptWithNoVerdict"/> AND THE
    /// PAIR IS DELIBERATE. There the row carries no verdict because none was
    /// reached; here a verdict was reached and taken away. A single assertion
    /// cannot tell those apart, so a test calls the helper naming the claim its
    /// fixture makes.
    /// </summary>
    private static void AssertWithheldByADegradedEnumeration(RegisteredPackage row, int expectedState)
    {
        Assert.False(row.IsRemovable);
        Assert.True(row.RemovableWithheld);
        Assert.Equal(expectedState, row.PatchState);
    }

    /// <summary>
    /// The row is OFFERED: a superseded patch that declares itself non-removable, on
    /// a product whose registered patch set was established and holds nothing that
    /// could be uninstalled and roll back onto its file.
    ///
    /// THIS IS THE PATH PRODUCTION TAKES ON AN ORDINARY MACHINE. A fixture that
    /// supplies no per-product patch sets makes every product read unestablished, so
    /// every superseded row is withheld and the test is about the degraded machine.
    /// Supplying them is what makes a test about the ordinary one.
    /// </summary>
    private static void AssertOffered(RegisteredPackage row, int expectedState)
    {
        Assert.True(row.IsRemovable);
        Assert.False(row.RemovableWithheld);
        Assert.Equal(expectedState, row.PatchState);
    }

    private static async Task<InstallerQueryResult> Run(FakeMsiApi msi) =>
        await new InstallerQueryService(msi, NoFallback).GetRegisteredPackagesAsync();

    /// <summary>
    /// Every product in the fixture as a HEALTHY machine's registry reports it: the
    /// patch set established, and holding nothing that could be uninstalled and roll
    /// back onto a superseded patch's file.
    ///
    /// WITHOUT THIS A FIXTURE IS A BROKEN MACHINE AND MOST OF THIS FILE WAS ONE.
    /// A run supplying no per-product patch sets makes every product read
    /// unestablished, so every superseded row is withheld and a test that meant to
    /// be about something else quietly becomes a test about the degraded path.
    /// </summary>
    private static Dictionary<string, ProductPatchSet> HealthyPatchSets(FakeMsiApi msi)
    {
        var map = new Dictionary<string, ProductPatchSet>(StringComparer.OrdinalIgnoreCase);
        // Indexer rather than ToDictionary: a fixture may add the same code twice, or
        // an empty one, and neither is this helper's business to reject.
        foreach (var (code, _) in msi.Products) map[code] = ProductPatchSet.AllNonRemovable;
        return map;
    }

    /// <summary>
    /// <see cref="Run(FakeMsiApi)"/> against an ordinary machine: the same
    /// enumeration, with the registry's per-product patch verdicts supplied.
    /// </summary>
    private static async Task<InstallerQueryResult> RunHealthy(FakeMsiApi msi, int fallbackFailures = 0) =>
        await new InstallerQueryService(msi, (_, _) => new InstallerQueryService.FallbackRead(
                fallbackFailures, 0, ProductPatchSets: HealthyPatchSets(msi)))
            .GetRegisteredPackagesAsync();

    /// <summary>
    /// <see cref="RunAgainstRegistry"/> against an ordinary machine, for the tests
    /// whose subject is the registry headcount rather than the patch verdict.
    /// </summary>
    private static async Task<InstallerQueryResult> RunAgainstRegistryHealthy(
        FakeMsiApi msi, int registryProducts) =>
        await new InstallerQueryService(msi, (_, _) => new InstallerQueryService.FallbackRead(
                0, registryProducts, ProductPatchSets: HealthyPatchSets(msi)))
            .GetRegisteredPackagesAsync();

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
    /// API's own against, and reports one entry in
    /// <paramref name="unclaimedProductFileCodes"/> per registry product entry naming a
    /// cached file that is on disk and that the API's own loop never claimed, each the
    /// code its key name unpacked to.
    ///
    /// The entries arrive here finished, because the real reader answers both halves
    /// itself: it holds the paths, and it asks the live filesystem about them. Nothing
    /// downstream can recompute either (the merge keeps one row per path and does not
    /// record which source reached it first), so what a test can drive is the
    /// observation, not the disk behind it.
    /// </summary>
    private static async Task<InstallerQueryResult> RunAgainstRegistry(
        FakeMsiApi msi,
        int registryProducts,
        string?[]? unclaimedProductFileCodes = null) =>
        await new InstallerQueryService(msi, (_, _) => new InstallerQueryService.FallbackRead(
                0, registryProducts, unclaimedProductFileCodes))
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

    // ONE CACHED PATCH REGISTERED TO TWO PRODUCTS, and which reading of it the merged
    // row carries. The four below pin the state, and they are written as four because
    // they pull in different directions: a stronger reading Windows gave reaches the row
    // whichever claim's account it carries, and a claim that established no reading
    // leaves the one on the row where it is.
    //
    // THE PATH IS ASSERTED THROUGH GetFullPath FOR THE REASON THE COMMAND LINE'S OWN
    // GATE TESTS DO IT. The scan normalises every recorded path, and a drive-letter
    // spelling completes differently off Windows, so comparing against the literal
    // finds nothing on one of the two hosts while the run has produced exactly the row
    // under test.
    private const string SharedPatchPath = @"C:\Windows\Installer\shared-two-products.msp";
    private static string SharedPatchRow => Path.GetFullPath(SharedPatchPath);

    /// <summary>
    /// Builds the machine both directions start from: product A holds the patch and
    /// reads cleanly as superseded, product B holds the same cached file, and A
    /// enumerates first so its reading is the one already on the row when B's claim
    /// arrives.
    /// </summary>
    private static FakeMsiApi TwoProductsSharingOnePatch()
    {
        var msi = new FakeMsiApi();
        msi.AddProduct("{A}");
        msi.AddPatch("{A}", "{P}", localPackage: SharedPatchPath, state: "2", uninstallable: "0");
        msi.AddProduct("{B}");
        msi.AddPatch("{B}", "{P}", localPackage: SharedPatchPath, state: "2", uninstallable: "0");
        return msi;
    }

    [Fact]
    public async Task A_claim_whose_state_read_failed_leaves_the_state_windows_gave()
    {
        // B's cached path reads, so its claim reaches the merge, and its State does
        // not. A claim that established no state must not write not-a-patch over one
        // the machine answered: every consumer of that field, the superseded and
        // obsoleted counts and the per-product patch-set judging among them, reads a
        // zero as a row that is not a patch at all.
        var msi = TwoProductsSharingOnePatch();
        msi.PatchPropertyResult[("{P}", "{B}", MsiInstallProperty.State)] = 5; // access denied

        var result = await Run(msi);

        var row = Assert.Single(result.Packages, r => r.LocalPackagePath == SharedPatchRow);
        Assert.Equal(2, row.PatchState);
        Assert.True(row.IsSupersededOrObsoleted);
        // B's claim is not removable, so the row is not either, and the one claim that
        // keeps the file is a read that failed, so the row reads unjudged.
        Assert.False(row.IsRemovable);
        Assert.True(row.VerdictUnreadable);
    }

    [Fact]
    public async Task A_state_windows_gave_still_replaces_an_earlier_one()
    {
        // The direction that must survive the rule above. A patch superseded under one
        // product can still be applied under another, and it is the applied reading
        // that has to reach the row: a row left saying superseded on a machine where a
        // product still holds the patch would be describing a state nothing reported.
        var msi = TwoProductsSharingOnePatch();
        msi.SetPatchProperty("{P}", "{B}", MsiInstallProperty.State, "1");

        var result = await Run(msi);

        var row = Assert.Single(result.Packages, r => r.LocalPackagePath == SharedPatchRow);
        Assert.Equal(1, row.PatchState);
        Assert.False(row.IsSupersededOrObsoleted);
    }

    [Fact]
    public async Task A_claim_whose_uninstallable_alone_would_not_read_still_brings_its_state()
    {
        // THE CASE THAT DECIDES WHICH FIELD THE RULE IS KEYED ON. This row's unreadable
        // flag is set, because it is the OR of the two reads, while its State was
        // answered positively. A rule keyed on that flag would keep the earlier
        // superseded reading and discard the applied one Windows had just given.
        var msi = TwoProductsSharingOnePatch();
        msi.SetPatchProperty("{P}", "{B}", MsiInstallProperty.State, "1");
        msi.PatchPropertyResult[("{P}", "{B}", MsiInstallProperty.Uninstallable)] = 5;

        var result = await Run(msi);

        var row = Assert.Single(result.Packages, r => r.LocalPackagePath == SharedPatchRow);
        Assert.True(row.VerdictUnreadable);
        Assert.Equal(1, row.PatchState);
        Assert.False(row.IsSupersededOrObsoleted);
    }

    [Fact]
    public async Task A_products_claim_on_a_patchs_cached_file_leaves_the_state_windows_gave()
    {
        // A PRODUCT'S CLAIM ON A PATCH'S CACHED FILE, the shape the merge's own summary is
        // written about: a corrupt LocalPackage aiming a product row at a patch's cached
        // file. The three above all start from a removable claim; this one's first claim
        // is not removable, and the state has to hold all the same.
        //
        // Product A's patch reads Superseded and its Uninstallable does not read, so A's
        // row carries a real state, no removable verdict and the unreadable flag.
        // Product B then names the same cached file as its own package, and a product
        // row is built from its LocalPackage alone: no state, and nothing that failed to
        // read. B's claim is a live claim on the file, so the row no longer reads as
        // unjudged, and B brought no state, so the row keeps A's.
        //
        // THE PATCH SETS ARE SUPPLIED because the row under test has to be non-removable
        // for the merge's reason rather than the fixture's. A run that leaves every
        // product unestablished withholds the whole removable class, so the assertion
        // below would hold on a machine this test is not about.
        var msi = new FakeMsiApi();
        msi.AddProduct("{A}");
        msi.SetProductProperty("{A}", "LocalPackage", @"C:\Windows\Installer\a.msi");
        msi.AddPatch("{A}", "{P}", localPackage: SharedPatchPath, state: "2", uninstallable: "0");
        msi.PatchPropertyResult[("{P}", "{A}", MsiInstallProperty.Uninstallable)] = 5; // access denied
        msi.AddProduct("{B}");
        msi.SetProductProperty("{B}", "LocalPackage", SharedPatchPath);

        var result = await RunHealthy(msi);

        var row = Assert.Single(result.Packages, r => r.LocalPackagePath == SharedPatchRow);
        Assert.Equal(2, row.PatchState);
        Assert.True(row.IsSupersededOrObsoleted);

        // B's claim was heard, which is what stops this passing for the wrong reason: the
        // row does not say a read failed.
        Assert.False(row.VerdictUnreadable);
        Assert.False(row.IsRemovable);
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
    public void A_live_claim_beside_a_read_that_failed_leaves_the_row_judged()
    {
        // Two non-removable claims on one path are not always the same finding.
        // The row reads as a live claim wherever one claim that keeps the file read
        // cleanly, because that is the only one that supports a sentence: the other
        // is a read that failed and says nothing about the file at all. The row's
        // account is the applied claim's.
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
    public void A_state_windows_gave_reaches_a_row_beside_a_read_that_failed()
    {
        // The row's state is the strongest reading either claim gave, whichever claim's
        // account it carries, and not the first state reached. A cached patch can be
        // superseded under one product and still applied under another, and it is the
        // applied reading that has to reach the row: a row left saying superseded on a
        // machine where a product still holds the patch would describe a state nothing
        // reported. The row above it in this file has the same pair with the state left
        // at its default, so it cannot tell these two apart.
        const string shared = @"C:\Windows\Installer\shared.msp";
        var claimed = new Dictionary<string, RegisteredPackage>(StringComparer.OrdinalIgnoreCase);
        InstallerQueryService.MergeClaim(claimed,
            new RegisteredPackage(shared, "Superseded", "{A}", PatchState: 2, VerdictUnreadable: true),
            InstallerQueryService.ClaimSource.InstallerApi);

        InstallerQueryService.MergeClaim(claimed,
            new RegisteredPackage(shared, "Applied", "{B}", PatchState: 1),
            InstallerQueryService.ClaimSource.InstallerApi);

        Assert.Equal(1, claimed[shared].PatchState);
        Assert.Equal("Applied", claimed[shared].ProductName);
    }

    [Fact]
    public void A_live_claim_and_a_read_that_failed_leave_the_same_row_in_either_order()
    {
        // The same pair the other way round, which is the half that makes the
        // merge an answer rather than a preference: whichever order the
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
    public void A_removable_claim_beside_a_read_that_failed_leaves_the_row_unjudged_and_kept()
    {
        // A removable claim establishes nothing about the file's being kept, so
        // beside a read that failed the row stays unjudged, and it is removable
        // only where both claims are: a product reading the patch as superseded
        // says nothing about the product whose read failed, and the file it would
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

    // ---- Uninstallable guard fails safe ----

    [Theory]
    // An empty Uninstallable beside a superseded State, and an empty State, leave the
    // pairing's verdict unestablished.
    [InlineData("2", "", true)]
    [InlineData("", "0", true)]
    // Beside an applied or obsoleted State an empty Uninstallable decides nothing, and
    // the pairing is a claim.
    [InlineData("1", "", false)]
    [InlineData("4", "", false)]
    public async Task An_empty_answer_the_verdict_turns_on_keeps_the_patch_as_unread_rather_than_claimed(
        string state, string uninstallable, bool unread)
    {
        var msi = new FakeMsiApi();
        msi.AddProduct("{A}");
        msi.AddPatch("{A}", "{P}", localPackage: @"C:\Windows\Installer\p.msp", state: state, uninstallable: uninstallable);

        var result = await Run(msi);

        var row = Assert.Single(result.Packages, r => r.LocalPackagePath.EndsWith("p.msp", StringComparison.Ordinal));
        Assert.False(row.IsRemovable);
        Assert.Equal(unread, row.VerdictUnreadable);
        Assert.Equal(unread ? 1 : 0, result.Census.UnreadablePatchStates);
        Assert.Equal(unread ? 1 : 0, result.Census.UnreadableVerdictPaths);
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
        AssertWithheldByADegradedEnumeration(row, expectedState: 2);
        Assert.Equal(1, result.UnaccountedProductCount); // the degraded product counts exactly once
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
        AssertWithheldByADegradedEnumeration(row, expectedState: 2);
        Assert.Equal(1, result.UnaccountedProductCount);
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

    // ---- An empty patch GUID accepted as success is not added ----

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
    /// else. Whether that patch is offered is the whole subject of the tests
    /// below it.
    /// </summary>
    private static FakeMsiApi OneProductWithASupersededPatch(string patch)
    {
        var msi = new FakeMsiApi();
        msi.AddProduct("{A}");
        msi.AddPatch("{A}", "{P}", localPackage: patch, state: "2", uninstallable: "0");
        return msi;
    }

    /// <summary>
    /// THE HEADCOUNTS ALONE DECIDE NOTHING, AT ANY RATIO. A registry holding more
    /// product keys than the enumeration returned is the ordinary shape of a
    /// machine that has ever had an uninstall go wrong, and a difference between
    /// two totals cannot tell that from an enumeration that stopped early. What
    /// separates them is asking about the products by name, which is done above
    /// and reaches this arithmetic already settled.
    ///
    /// The ratios below are the ones a proportional rule would have divided on:
    /// one key ahead, a tenth ahead, and thirty-nine of forty. **None of them may
    /// withhold anything**, and the last is the case a machine really can be in
    /// while perfectly healthy.
    /// </summary>
    [Theory]
    [InlineData(1, 5)]     // four products enumerated, five keys
    [InlineData(90, 100)]  // a tenth ahead
    [InlineData(1, 40)]    // the registry far ahead of a small enumeration
    public async Task A_registry_ahead_of_the_enumeration_counts_nothing_on_the_totals_alone(
        int enumerated, int registryProducts)
    {
        const string patch = @"C:\Windows\Installer\superseded.msp";
        var msi = OneProductWithASupersededPatch(patch);
        for (var i = 1; i < enumerated; i++) msi.AddProduct($"{{P{i}}}");

        var result = await RunAgainstRegistryHealthy(msi, registryProducts);

        AssertOffered(Assert.Single(result.Packages, r => r.LocalPackagePath == patch), expectedState: 2);
        Assert.Equal(0, result.UnaccountedProductCount);
    }

    [Fact]
    public async Task A_registry_key_whose_name_yields_no_product_code_counts_unaccounted()
    {
        // THE ONE PLACE NAMING PRODUCTS SEES LESS THAN COUNTING THEM DID, and it
        // is why the count is not simply gone. A subkey whose name is not a packed
        // GUID is a product the registry says this machine has and that nothing
        // here can turn into a question, so it is neither recovered nor shown to
        // be residue. Withheld on the same terms as a code Windows refuses to
        // answer about, because it is that state one step earlier. Left
        // uncounted it would look exactly like a registry that agreed with the
        // enumeration.
        const string patch = @"C:\Windows\Installer\superseded.msp";
        var msi = OneProductWithASupersededPatch(patch);

        var result = await new InstallerQueryService(msi,
            (_, _) => new InstallerQueryService.FallbackRead(
                Failures: 0, ProductKeys: 3, UnparseableProductKeyNames: 2))
            .GetRegisteredPackagesAsync();

        var row = Assert.Single(result.Packages, r => r.LocalPackagePath == patch);
        AssertWithheldByADegradedEnumeration(row, expectedState: 2);
        Assert.Equal(2, result.UnaccountedProductCount);
        // The specific cause, not the sum it feeds: nothing could be asked, as
        // against a question asked and refused.
        Assert.Equal(2, result.Census.UnparseableProductKeyNames);
        Assert.Equal(0, result.Census.UnansweredProductCount);
    }

    [Fact]
    public async Task A_failing_fallback_beside_a_clean_enumeration_does_not_refuse_the_scan()
    {
        // The refusal above stays keyed on what the API said about ITSELF, and
        // this run has nothing to say: every product enumerated cleanly, and only
        // the fallback's own key reads failed. A backstop that stumbled where the
        // primary answered in full is not a reason to take a machine's scan away,
        // so the run still comes back with a list. What the failed reads do cost
        // is the comparison, which cannot name a product out of a key it never
        // read, and that shows up as keys counted with no code taken from them.
        const string patch = @"C:\Windows\Installer\superseded.msp";
        var msi = OneProductWithASupersededPatch(patch);

        var result = await new InstallerQueryService(msi,
            (_, _) => new InstallerQueryService.FallbackRead(Failures: 3, ProductKeys: 40))
            .GetRegisteredPackagesAsync();

        Assert.NotEmpty(result.Packages);
        AssertWithheldByADegradedEnumeration(Assert.Single(result.Packages, r => r.LocalPackagePath == patch), expectedState: 2);
    }

    /// <summary>
    /// A LOST CLAIM IS COUNTED ONCE, BY THE ONLY THING THAT SAW IT, AND THE REGISTRY'S
    /// OWN TOTAL ADDS NOTHING TO THE ANSWER WHATEVER IT SAYS.
    ///
    /// The products the registry holds and the enumeration did not return are settled
    /// by identity: the codes the registry holds are compared against the codes the
    /// enumeration returned, and each difference is put to Windows as a question about
    /// that one product, so a truncation is named rather than estimated and a leftover
    /// key answers "not installed". No shortfall of one product total against another
    /// is a term of the count.
    ///
    /// THAT RULE IS WHAT THIS PINS. Four claims really are lost here, so there is a real
    /// count to distort, and the registry total is walked from agreeing exactly to
    /// absurdly ahead. If a shortfall against that total is ever admitted as a term,
    /// this goes red, and it goes red in the one place where a wrong answer is not just
    /// a bad number: the count drives the withholding of the whole removable class.
    ///
    /// It is deliberately NOT the same claim as
    /// <see cref="A_registry_ahead_of_the_enumeration_counts_nothing_on_the_totals_alone"/>,
    /// which asks the question of a machine that lost nothing and can only ever assert
    /// zero. A rule that returned the shortfall whenever anything else had already been
    /// counted would pass that one and fail this.
    /// </summary>
    [Theory]
    [InlineData(4)]    // the registry agrees with the enumerated products exactly
    [InlineData(20)]   // sixteen keys the enumeration never mentioned
    [InlineData(500)]  // absurd, and it decides exactly as much: nothing
    public async Task A_lost_claim_is_counted_once_and_the_registry_total_adds_nothing(int registryProducts)
    {
        const string patch = @"C:\Windows\Installer\superseded.msp";
        var msi = new FakeMsiApi();
        for (var i = 0; i < 4; i++)
        {
            // A product whose cached-package read fails: one claim lost, counted
            // once.
            msi.AddProduct($"{{P{i}}}");
            msi.ProductPropertyResult[($"{{P{i}}}", "LocalPackage")] = BadConfiguration;
        }
        msi.AddPatch("{P0}", "{PATCH}", localPackage: patch, state: "2", uninstallable: "0");

        // Healthy patch sets, so the per-product condition settles clean and the ONE
        // thing left able to take the removable verdict away is the count under test.
        // Supplied without them, every product reads unestablished, the row is
        // withheld before this rule is reached, and the assertion below passes whether
        // the rule fired or not.
        var result = await RunAgainstRegistryHealthy(msi, registryProducts);

        Assert.Equal(4, result.UnaccountedProductCount);
        // And the count is not decoration: four products the scan cannot account for
        // takes the whole removable class back, so the superseded row is kept and
        // marked as having been kept.
        AssertWithheldByADegradedEnumeration(
            Assert.Single(result.Packages, r => r.LocalPackagePath == patch), expectedState: 2);
    }

    /// <summary>
    /// A KEY WHOSE NAME IS NO PRODUCT CODE IS ONE ENTRY, HOWEVER MANY WAYS IT SHOWS. It is
    /// counted where nothing could be asked about it, and the InstallProperties read that
    /// follows does not turn on the name, so the file it records is read as well and,
    /// being on the disk and claimed by nothing the enumeration listed, is an unclaimed
    /// file too. The unclaimed file decides nothing on its own, so the key is one entry
    /// the scan could not check, and the figure is exact.
    /// </summary>
    [Fact]
    public async Task A_key_that_names_no_product_and_claims_a_file_of_its_own_counts_once()
    {
        const string patch = @"C:\Windows\Installer\superseded.msp";
        var msi = OneProductWithASupersededPatch(patch);

        // One product subkey, in the two states such a key leaves behind at once:
        // its name yields no code to put to Windows, and the cached file it
        // records is on the disk and was never claimed by the enumeration.
        // Healthy patch sets, so the per-product condition settles clean and the
        // count under test is the only thing left able to take the removable
        // verdict away.
        var result = await new InstallerQueryService(msi, (_, _) =>
                new InstallerQueryService.FallbackRead(
                    Failures: 0, ProductKeys: 1,
                    UnclaimedProductFileCodes: [null], UnparseableProductKeyNames: 1,
                    ProductPatchSets: HealthyPatchSets(msi)))
            .GetRegisteredPackagesAsync();

        // The terms rather than the sum they feed, so a total that came out right
        // for the wrong reasons cannot pass: one key walked, one unclaimed file,
        // one name that yielded nothing, and nothing the enumeration itself lost
        // or asked about in vain.
        Assert.Equal(1, result.Census.RegistryProductKeys);
        Assert.Equal(1, result.Census.UnclaimedProductFiles);
        Assert.Equal(1, result.Census.UnparseableProductKeyNames);
        Assert.Equal(0, result.Census.UnreadableProducts);
        Assert.Equal(0, result.Census.UnansweredProductCount);

        Assert.Equal(1, result.UnaccountedProductCount);
        // The fixture's one row, and the count is not decoration: a scan that
        // cannot check a program entry takes the whole removable class back, so
        // the superseded patch is kept and marked as having been kept.
        AssertWithheldByADegradedEnumeration(Assert.Single(result.Packages), expectedState: 2);
    }

    [Fact]
    public async Task A_registry_that_claimed_nothing_of_its_own_counts_nothing_unaccounted()
    {
        // The false-fire control. Every path the registry names was already
        // claimed by the API, which is what a whole enumeration looks like, and
        // superseded-patch cleanup has to survive it or the feature is dead.
        const string patch = @"C:\Windows\Installer\superseded.msp";

        var result = await RunAgainstRegistryHealthy(OneProductWithASupersededPatch(patch), 1);

        AssertOffered(Assert.Single(result.Packages, r => r.LocalPackagePath == patch), expectedState: 2);
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

    // The message is asserted, not just the type, because the cap and the stop
    // on a row that did not read are different conditions that a shared string
    // would describe falsely: at the cap every row read and the list never
    // ended, and the error code is Success. Asserting the type alone cannot tell
    // the two apart.
    [Fact]
    public async Task Product_enumeration_that_never_ends_throws_at_the_cap()
    {
        var msi = new FakeMsiApi { NeverEndProducts = true };

        var ex = await Assert.ThrowsAsync<LocalisedInvalidOperationException>(() => Run(msi));

        // BOTH FIGURES READ 10,000 HERE AND THE FIXTURE IS WHY. Every index answers
        // with a product, so the count read cleanly runs to the cap and coincides with
        // the cap itself. That makes this assertion unable to tell the two apart, which
        // is what The_stop_message_says_how_many_products_were_read_before_it is for.
        Assert.Equal(
            string.Format(Strings.Error_MsiEnumerationNeverEnded, 10_000, MsiError.Success,
                10_000, DisplayHelpers.PluraliseProduct(10_000)),
            ex.Message);
    }

    [Fact]
    public async Task Patch_enumeration_that_never_ends_throws_at_the_cap()
    {
        var msi = new FakeMsiApi();
        msi.AddProduct("{A}");
        msi.NeverEndPatchesFor = "{A}";

        var ex = await Assert.ThrowsAsync<LocalisedInvalidOperationException>(() => Run(msi));

        // The patch loop's own count, and its noun is patches rather than products:
        // this enumeration walks one program's patch list. Same coincidence as above,
        // every index answering with a patch.
        Assert.Equal(
            string.Format(Strings.Error_MsiPatchEnumerationNeverEnded, 10_000, MsiError.Success,
                10_000, DisplayHelpers.PluralisePatch(10_000)),
            ex.Message);
    }

    // ---- A product row that does not read refuses the scan ----
    //
    // Each fixture reads a product cleanly before the row that does not, so the
    // count the message carries is not zero and a walk that stepped past the row
    // would have a list to return.

    [Fact]
    public async Task A_failed_product_row_refuses_the_scan()
    {
        var msi = new FakeMsiApi();
        msi.AddProduct("{A}");
        msi.SetProductProperty("{A}", "LocalPackage", @"C:\Windows\Installer\a.msi");
        msi.AddProduct("{B}", result: 1603 /* ERROR_INSTALL_FAILURE */);

        var ex = await Assert.ThrowsAsync<LocalisedInvalidOperationException>(() => Run(msi));

        Assert.Equal(
            string.Format(Strings.Error_MsiNonSuccess, 1603u, 1, DisplayHelpers.PluraliseProduct(1)),
            ex.Message);
    }

    [Fact]
    public async Task A_product_row_that_writes_no_code_refuses_the_scan()
    {
        // A Success return with no product code is a row that did not read, and
        // the message carries the code the call returned, which is Success.
        var msi = new FakeMsiApi();
        msi.AddProduct("{A}");
        msi.SetProductProperty("{A}", "LocalPackage", @"C:\Windows\Installer\a.msi");
        msi.AddProduct("");   // Success return, no GUID written

        var ex = await Assert.ThrowsAsync<LocalisedInvalidOperationException>(() => Run(msi));

        Assert.Equal(
            string.Format(Strings.Error_MsiNonSuccess, MsiError.Success, 1, DisplayHelpers.PluraliseProduct(1)),
            ex.Message);
    }

    [Fact]
    public async Task A_sid_retry_that_asks_for_more_again_refuses_the_scan()
    {
        // The row whose product key name is too long to be a product code: the
        // first call answers MoreData, the retry with a larger SID buffer answers
        // MoreData again, and the row did not read.
        var msi = new FakeMsiApi();
        msi.AddProduct("{A}");
        msi.SetProductProperty("{A}", "LocalPackage", @"C:\Windows\Installer\a.msi");
        msi.AddProduct("{B}");
        msi.ProductSidRetryResult[1] = MoreData;

        var ex = await Assert.ThrowsAsync<LocalisedInvalidOperationException>(() => Run(msi));

        Assert.Equal(
            string.Format(Strings.Error_MsiNonSuccess, MoreData, 1, DisplayHelpers.PluraliseProduct(1)),
            ex.Message);
    }

    /// <summary>
    /// THE WALK STOPS AT THE FAILED ROW, AND THE COUNT IN THE MESSAGE IS HOW FAR IT
    /// GOT BEFORE IT. Three products read, then a row that does not, then two more
    /// that would: the message says three. A walk that stepped past the failed row
    /// and refused only at the end of the list would say five.
    ///
    /// How many products came back cleanly first is what somebody helping needs, and
    /// reading none is a different situation from reading two hundred. The two cap
    /// tests above cannot show it, their fixtures driving the count to the cap.
    /// </summary>
    [Fact]
    public async Task The_stop_message_says_how_many_products_were_read_before_it()
    {
        var msi = new FakeMsiApi();
        for (int i = 0; i < 3; i++)
        {
            msi.AddProduct($"{{good{i}}}");
            msi.SetProductProperty($"{{good{i}}}", "LocalPackage", $@"C:\Windows\Installer\good{i}.msi");
        }
        msi.AddProduct("{bad}", result: 1603);
        for (int i = 0; i < 2; i++)
        {
            msi.AddProduct($"{{after{i}}}");
            msi.SetProductProperty($"{{after{i}}}", "LocalPackage", $@"C:\Windows\Installer\after{i}.msi");
        }

        var ex = await Assert.ThrowsAsync<LocalisedInvalidOperationException>(() => Run(msi));

        Assert.Equal(
            string.Format(Strings.Error_MsiNonSuccess, 1603u, 3, DisplayHelpers.PluraliseProduct(3)),
            ex.Message);
    }

    // ---- The abandonment breadcrumb is budgeted ----
    //
    // What makes it one entry per product is a property of the registration, not
    // of a product: an account the patch enumeration refuses as input refuses
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

        // Twenty in full plus the one closing entry, where one entry per product
        // would be 200.
        Assert.Equal(21, written.Count);
        Assert.Equal(20, written.Count(e => e.Message.Contains("Patch enumeration abandoned", StringComparison.Ordinal)));

        var closing = written[^1].Message;
        Assert.Contains("Patch enumeration: 180 further failures were not logged individually",
            closing, StringComparison.Ordinal);
        // The trail has to be true of THIS caller: a suppressed abandonment
        // takes the product's identity with it, and nothing either host shows
        // says which product's patch list was abandoned.
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
    // A skipped patch row or an empty patch GUID costs one product a patch
    // claim, and a patch is cached once and shared across the products holding
    // it, so the product behind the loss may be the one that still has a
    // removable-looking patch applied. Which patch it still holds is unknowable
    // (a failed row's patch code is undefined), so no narrower rule is available
    // than withholding the class. Each way a claim can be lost reaches the same
    // demotion.

    [Fact]
    public async Task A_clean_enumeration_counts_nothing_unaccounted()
    {
        const string dead = @"C:\Windows\Installer\clean-dead.msp";
        var msi = new FakeMsiApi();
        msi.AddProduct("{A}");
        msi.AddPatch("{A}", "{P}", localPackage: dead, state: "2", uninstallable: "0");

        var result = await RunHealthy(msi);

        AssertOffered(Assert.Single(result.Packages, r => r.LocalPackagePath == dead), expectedState: 2);
        Assert.Equal(0, result.UnaccountedProductCount);
    }

    [Fact]
    public async Task A_skipped_patch_row_counts_an_unaccounted_product()
    {
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
        AssertWithheldByADegradedEnumeration(row, expectedState: 2);
        Assert.Equal(1, result.UnaccountedProductCount);
    }

    [Fact]
    public async Task An_empty_patch_guid_counts_an_unaccounted_product()
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
    public async Task Unreadable_products_count_a_lost_package_read_and_a_lost_patch_row_alike()
    {
        // Both leave the same hole (a product whose claims are short), so the
        // count the user reads adds them together. A product is counted once
        // however many of its patch rows failed.
        var msi = new FakeMsiApi();
        msi.AddProduct("{bad}");
        msi.ProductPropertyResult[("{bad}", "LocalPackage")] = BadConfiguration;
        msi.AddProduct("{B}");
        // B's own package, so the run yields a claim. Without one the scan ends
        // with an empty set and refuses as an empty installer database before it
        // can report a count.
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
        // A withheld scan still carries every registered path, so the walk still
        // has everything it needs to tell an orphan from a registered file.
        const string productPackage = @"C:\Windows\Installer\kept.msi";
        var msi = new FakeMsiApi();
        msi.AddProduct("{A}");
        msi.SetProductProperty("{A}", "LocalPackage", productPackage);
        msi.AddProduct("{bad}");
        msi.ProductPropertyResult[("{bad}", "LocalPackage")] = BadConfiguration;

        var result = await Run(msi);

        // A product whose LocalPackage will not read is unaccounted for, which
        // withholds the removable class on this run.
        Assert.True(result.UnaccountedProductCount > 0);
        Assert.Contains(result.Packages, r => r.LocalPackagePath == productPackage);
    }

    // ---- A failed LocalPackage read loses a claim the same way a skipped patch row does ----
    //
    // The other three properties degrade safely when they cannot be read: an
    // unreadable State leaves patchState 0 and an unreadable Uninstallable leans
    // non-removable, so the row still merges as "needed". LocalPackage is the
    // property that CARRIES the claim, so a failed read of it does not degrade
    // the row, it deletes it. That is the same hole as a skipped patch row, and
    // it reaches the same count and the same withholding.
    //
    // What makes the discrimination possible is that a record with no cached
    // package and a record that cannot be read return different codes: an absent
    // property answers ERROR_UNKNOWN_PROPERTY, and an unreadable product a real
    // error rather than a zero-length success.

    [Fact]
    public async Task A_failed_product_LocalPackage_read_counts_an_unaccounted_product()
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
        AssertWithheldByADegradedEnumeration(row, expectedState: 2);
        Assert.Equal(1, result.UnaccountedProductCount);
    }

    [Fact]
    public async Task A_failed_patch_LocalPackage_read_counts_an_unaccounted_product()
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
        AssertWithheldByADegradedEnumeration(row, expectedState: 2);
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
    public async Task A_product_with_no_cached_package_counts_nothing_unaccounted()
    {
        const string dead = @"C:\Windows\Installer\absent-product-package.msp";
        var msi = new FakeMsiApi();
        msi.AddProduct("{A}");
        msi.AddPatch("{A}", "{P}", localPackage: dead, state: "2", uninstallable: "0");
        msi.AddProduct("{B}");
        msi.ProductPropertyResult[("{B}", "LocalPackage")] = UnknownProperty;

        var result = await RunHealthy(msi);

        AssertOffered(Assert.Single(result.Packages, r => r.LocalPackagePath == dead), expectedState: 2);
        Assert.Equal(0, result.UnaccountedProductCount);
    }

    [Fact]
    public async Task A_registered_patch_with_no_cached_package_counts_nothing_unaccounted()
    {
        const string dead = @"C:\Windows\Installer\absent-patch-package.msp";
        var msi = new FakeMsiApi();
        msi.AddProduct("{A}");
        msi.AddPatch("{A}", "{P}", localPackage: dead, state: "2", uninstallable: "0");
        msi.AddProduct("{B}");
        msi.PatchCodes["{B}"] = new() { "{R}" };
        msi.PatchPropertyResult[("{R}", "{B}", "LocalPackage")] = UnknownProperty;

        var result = await RunHealthy(msi);

        AssertOffered(Assert.Single(result.Packages, r => r.LocalPackagePath == dead), expectedState: 2);
        Assert.Equal(0, result.UnaccountedProductCount);
    }

    [Fact]
    public async Task A_readable_but_empty_LocalPackage_counts_nothing_unaccounted()
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

        var result = await RunHealthy(msi);

        AssertOffered(Assert.Single(result.Packages, r => r.LocalPackagePath == dead), expectedState: 2);
        Assert.Equal(0, result.UnaccountedProductCount);
    }

    [Fact]
    public async Task A_failed_ProductName_read_counts_nothing_unaccounted()
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

        var result = await RunHealthy(msi);

        AssertOffered(Assert.Single(result.Packages, r => r.LocalPackagePath == dead), expectedState: 2);
        Assert.Equal(0, result.UnaccountedProductCount);
        Assert.Contains(result.Packages, r => r.LocalPackagePath == @"C:\Windows\Installer\b.msi");
    }

    // ---- A recorded path the scan could not settle withholds the removable class too ----
    //
    // Claims meet on a row by their normalised path, so a registration kept in a spelling
    // nothing resolves need not land on the row for the file it means, and where it does
    // not, its claim never reaches that row. Which file such a claim names cannot be
    // established, so the scan-wide withholding takes every superseded row still carrying
    // its removable verdict off the offer, as it does on a scan that could not account for
    // every installed product.
    //
    // THE UNSETTLED VALUES HERE CARRY AN EMBEDDED NULL, which the normalisation refuses
    // before the resolver is asked, so they are unsettled wherever the suite runs. Rows are
    // picked by patch state rather than by path, because an ordinary value comes back in
    // whatever spelling the machine running the suite resolves it to.

    private const string SharedPatch = @"C:\Windows\Installer\shared.msp";
    private const string SharedPatchUnsettled = "C:\\Windows\\Installer\\shared\0.msp";
    private const string UnrelatedPackageUnsettled = "C:\\Windows\\Installer\\c\0.msi";

    /// <summary>
    /// The row was a superseded patch on the offer until the scan-wide withholding took its
    /// verdict while a recorded path was unsettled, and it carries the flag the opt-in
    /// report counts it by.
    /// </summary>
    private static void AssertWithheldOnAnUnsettledPath(RegisteredPackage row)
    {
        Assert.False(row.IsRemovable);
        Assert.True(row.RemovableWithheld);
        Assert.True(row.WithheldOnRecordedPathUnestablished);
        Assert.Equal(2, row.PatchState);
    }

    /// <summary>
    /// One superseded patch on product A, and product C whose own cached package is
    /// recorded as <paramref name="cPackage"/>.
    /// </summary>
    private static FakeMsiApi ASupersededPatchBesideProductC(string cPackage)
    {
        var msi = new FakeMsiApi();
        msi.AddProduct("{A}");
        msi.AddPatch("{A}", "{P}", localPackage: SharedPatch, state: "2", uninstallable: "0");
        msi.AddProduct("{C}");
        msi.SetProductProperty("{C}", "LocalPackage", cPackage);
        return msi;
    }

    [Fact]
    public async Task A_second_registration_of_a_superseded_patch_in_an_unsettled_spelling_is_asked_about_it()
    {
        // The patch is superseded under A, and applied and not uninstallable under B, and
        // both registrations name one cached file. B's value carries an embedded null and
        // is kept exactly as recorded, so its claim lands on a row of its own and A's row
        // carries no sign of it. The per-pairing pass asks B about the patch, and B's
        // answer keeps A's row as a live claim before the scan-wide withholding runs.
        var msi = new FakeMsiApi();
        msi.AddProduct("{A}");
        msi.AddProduct("{B}");
        msi.AddPatch("{A}", "{P}", localPackage: SharedPatch, state: "2", uninstallable: "0");
        msi.AddPatch("{B}", "{P}", localPackage: SharedPatchUnsettled, state: "1", uninstallable: "0");

        var result = await RunHealthy(msi);

        var row = Assert.Single(result.Packages, r => r.PatchState == 2);
        Assert.False(row.IsRemovable);
        Assert.False(row.RemovableWithheld);
        Assert.False(row.WithheldOnRecordedPathUnestablished);
        Assert.Single(result.Packages, r => r.PatchState == 1);
        Assert.True(result.Census.AnyRecordedPathUnestablished);
        Assert.Equal(0, result.UnaccountedProductCount);
    }

    [Fact]
    public async Task The_same_machine_with_that_registration_spelled_ordinarily_merges_it_onto_the_patch()
    {
        // THE MUST-MISS FOR THE TEST ABOVE, differing in B's one value: B's registration
        // names the same cached file in an ordinary spelling. Its claim meets A's on one
        // row, and the patch B holds applied keeps that row off the offer as a live claim
        // rather than as a withholding.
        var msi = new FakeMsiApi();
        msi.AddProduct("{A}");
        msi.AddProduct("{B}");
        msi.AddPatch("{A}", "{P}", localPackage: SharedPatch, state: "2", uninstallable: "0");
        msi.AddPatch("{B}", "{P}", localPackage: SharedPatch, state: "1", uninstallable: "0");

        var result = await RunHealthy(msi);

        Assert.DoesNotContain(result.Packages, r => r.PatchState == 2);
        var row = Assert.Single(result.Packages, r => r.PatchState == 1);
        Assert.False(row.IsRemovable);
        Assert.False(row.RemovableWithheld);
        Assert.False(row.WithheldOnRecordedPathUnestablished);
        Assert.False(result.Census.AnyRecordedPathUnestablished);
    }

    [Fact]
    public async Task A_second_registration_holding_the_patch_superseded_in_an_unsettled_spelling_withholds_it()
    {
        // The patch is superseded and no longer uninstallable under A and under B, and B's
        // value carries an embedded null, so B's claim lands on a row of its own. Asking B
        // about the patch finds nothing to keep the file for. The unsettled value is what
        // takes the verdict: it names a file the scan cannot place, and the scan-wide
        // withholding takes every superseded row off the offer, A's included, and B's with
        // it.
        var msi = new FakeMsiApi();
        msi.AddProduct("{A}");
        msi.AddProduct("{B}");
        msi.AddPatch("{A}", "{P}", localPackage: SharedPatch, state: "2", uninstallable: "0");
        msi.AddPatch("{B}", "{P}", localPackage: SharedPatchUnsettled, state: "2", uninstallable: "0");

        var result = await RunHealthy(msi);

        var superseded = result.Packages.Where(r => r.PatchState == 2).ToList();
        Assert.Equal(2, superseded.Count);
        Assert.All(superseded, AssertWithheldOnAnUnsettledPath);
        Assert.True(result.Census.AnyRecordedPathUnestablished);
        Assert.Equal(0, result.UnaccountedProductCount);
    }

    [Fact]
    public async Task An_unsettled_value_on_an_unrelated_product_withholds_the_superseded_patch()
    {
        // Nothing ties C's registration to the patch, and nothing needs to: its claim names
        // a file the scan cannot place, which can be the patch's file as easily as any
        // other.
        var result = await RunHealthy(ASupersededPatchBesideProductC(UnrelatedPackageUnsettled));

        AssertWithheldOnAnUnsettledPath(Assert.Single(result.Packages, r => r.PatchState == 2));
        Assert.Equal(0, result.UnaccountedProductCount);
    }

    [Fact]
    public async Task The_same_unrelated_product_in_an_ordinary_spelling_withholds_nothing()
    {
        // THE MUST-MISS FOR THE TEST ABOVE: C's value spelled ordinarily.
        var result = await RunHealthy(ASupersededPatchBesideProductC(@"C:\Windows\Installer\c.msi"));

        var row = Assert.Single(result.Packages, r => r.PatchState == 2);
        AssertOffered(row, expectedState: 2);
        Assert.False(row.WithheldOnRecordedPathUnestablished);
    }

    [Fact]
    public async Task A_value_the_resolver_refuses_withholds_the_superseded_patch()
    {
        // The volume-GUID spelling here names a volume the machine running the suite does
        // not have, so the resolver cannot settle it. That is a refusal from a different
        // population from the embedded nulls above, and the withholding asks both.
        var result = await RunHealthy(ASupersededPatchBesideProductC(
            @"\\?\Volume{9c3a1d2e-0000-0000-0000-100000000000}\Windows\Installer\c.msi"));

        AssertWithheldOnAnUnsettledPath(Assert.Single(result.Packages, r => r.PatchState == 2));
        Assert.True(result.Census.PathResolverRefusedTotal > 0);
    }

    [Fact]
    public async Task A_refusal_only_the_registry_side_counted_withholds_the_superseded_patch()
    {
        // The registry fallback normalises the values it reads with a census of its own,
        // and the withholding asks the two censuses added together, so a refusal counted on
        // that side alone withholds as one the enumeration counted does.
        var msi = new FakeMsiApi();
        msi.AddProduct("{A}");
        msi.AddPatch("{A}", "{P}", localPackage: SharedPatch, state: "2", uninstallable: "0");

        var result = await new InstallerQueryService(msi, (_, _) =>
            {
                var registrySide = new InstallerQueryService.PathCensus();
                registrySide.RecordNormalisationRefusal(InstallerQueryService.NormalisationStage.EmbeddedNull);
                return new InstallerQueryService.FallbackRead(0, 0,
                    ProductPatchSets: HealthyPatchSets(msi), Paths: registrySide);
            })
            .GetRegisteredPackagesAsync();

        AssertWithheldOnAnUnsettledPath(Assert.Single(result.Packages, r => r.PatchState == 2));
    }

    [Fact]
    public async Task The_same_machine_whose_registry_side_refused_nothing_offers_the_patch()
    {
        // THE MUST-MISS FOR THE TEST ABOVE: the registry side hands over a census of its
        // own with nothing refused in it.
        var msi = new FakeMsiApi();
        msi.AddProduct("{A}");
        msi.AddPatch("{A}", "{P}", localPackage: SharedPatch, state: "2", uninstallable: "0");

        var result = await new InstallerQueryService(msi, (_, _) =>
                new InstallerQueryService.FallbackRead(0, 0,
                    ProductPatchSets: HealthyPatchSets(msi), Paths: new InstallerQueryService.PathCensus()))
            .GetRegisteredPackagesAsync();

        var row = Assert.Single(result.Packages, r => r.PatchState == 2);
        AssertOffered(row, expectedState: 2);
        Assert.False(row.WithheldOnRecordedPathUnestablished);
    }

    [Fact]
    public async Task A_row_withheld_while_both_conditions_held_carries_the_flag()
    {
        // D's patch list is abandoned, so the scan could not account for every installed
        // product either. The flag records that this condition held when the row lost its
        // verdict, whatever else did, so the opt-in report still counts the file here.
        var msi = ASupersededPatchBesideProductC(UnrelatedPackageUnsettled);
        msi.AddProduct("{D}");
        msi.SetProductProperty("{D}", "LocalPackage", @"C:\Windows\Installer\d.msi");
        msi.PatchEnumResult["{D}"] = InvalidParameter;

        var result = await RunHealthy(msi);

        AssertWithheldOnAnUnsettledPath(Assert.Single(result.Packages, r => r.PatchState == 2));
        Assert.Equal(1, result.UnaccountedProductCount);
    }

    [Fact]
    public async Task A_row_withheld_on_an_unaccounted_product_alone_does_not_carry_the_flag()
    {
        // The same machine with C's value spelled ordinarily: the row is withheld on the
        // unaccounted product alone.
        var msi = ASupersededPatchBesideProductC(@"C:\Windows\Installer\c.msi");
        msi.AddProduct("{D}");
        msi.SetProductProperty("{D}", "LocalPackage", @"C:\Windows\Installer\d.msi");
        msi.PatchEnumResult["{D}"] = InvalidParameter;

        var result = await RunHealthy(msi);

        var row = Assert.Single(result.Packages, r => r.PatchState == 2);
        AssertWithheldByADegradedEnumeration(row, expectedState: 2);
        Assert.False(row.WithheldOnRecordedPathUnestablished);
        Assert.Equal(1, result.UnaccountedProductCount);
    }

    [Fact]
    public async Task A_row_an_earlier_check_withheld_does_not_carry_the_flag()
    {
        // A's patch set is unestablished, so the per-product condition withholds the row
        // before the scan-wide withholding runs, which then finds no verdict left to take.
        var msi = ASupersededPatchBesideProductC(UnrelatedPackageUnsettled);

        var result = await RunWithPatchSets(msi,
            ("{A}", ProductPatchSet.Unestablished), ("{C}", ProductPatchSet.AllNonRemovable));

        var row = Assert.Single(result.Packages, r => r.PatchState == 2);
        Assert.False(row.IsRemovable);
        Assert.True(row.RemovableWithheld);
        Assert.False(row.WithheldOnRecordedPathUnestablished);
        Assert.True(result.Census.AnyRecordedPathUnestablished);
    }

    [Fact]
    public async Task A_superseded_patch_file_that_would_not_read_is_kept_on_the_claim_of_a_program_holding_it_under_another_spelling()
    {
        // WHERE THE SUPERSEDED FILE HAS GONE. A cached file that is not there does not
        // read, and the per-pairing pass asks every installation about the patch all the
        // same. B holds it applied, so the row is kept on B's claim before the scan-wide
        // withholding runs, carries neither the unread-file marker nor the flag, and keeps
        // its superseded reading: B's registration of the same file, in the unsettled
        // spelling, is a row of its own naming the same absent file, and an applied row
        // whose file is missing is always counted, so the warning names B's program
        // through that row and counts the file once.
        var msi = new FakeMsiApi();
        msi.AddProduct("{A}");
        msi.AddProduct("{B}");
        msi.AddPatch("{A}", "{P}", localPackage: SharedPatch, state: "2", uninstallable: "0");
        msi.AddPatch("{B}", "{P}", localPackage: SharedPatchUnsettled, state: "1", uninstallable: "0");

        var result = await new InstallerQueryService(msi,
                (_, _) => new InstallerQueryService.FallbackRead(0, 0, ProductPatchSets: HealthyPatchSets(msi)),
                identityReader: new OnePatchFileUnread("shared.msp"))
            .GetRegisteredPackagesAsync();

        var superseded = Assert.Single(result.Packages, r => r.PatchState == 2);
        Assert.False(superseded.IsRemovable);
        Assert.False(superseded.RemovableWithheld);
        Assert.False(superseded.WithheldOnUnreadableFile);
        Assert.False(superseded.WithheldOnRecordedPathUnestablished);
        Assert.False(MissingFilesReport.Affected(superseded with { FileExists = false }));

        var applied = Assert.Single(result.Packages, r => r.PatchState == 1);
        Assert.True(MissingFilesReport.Affected(applied with { FileExists = false }));
    }

    /// <summary>
    /// A package reader under which the file whose name ends in <paramref name="leaf"/>
    /// yields nothing, as a cached file that is not there yields nothing, and every other
    /// file reads as a patch naming no product.
    /// </summary>
    private sealed class OnePatchFileUnread(string leaf) : IPackageIdentityReader
    {
        public PackageIdentity? Read(string filePath, bool isPatch, out string detail, out PackageReadRefusal refusal)
        {
            detail = string.Empty;
            refusal = PackageReadRefusal.WouldNotRead;
            return filePath.EndsWith(leaf, StringComparison.OrdinalIgnoreCase)
                ? null
                : new PackageIdentity(string.Empty, isPatch, Array.Empty<string>());
        }
    }

    /// <summary>
    /// The real query service over <paramref name="msi"/> with the registry answering for
    /// every product as an ordinary machine's does, for the re-verify to run.
    /// </summary>
    private static InstallerQueryService HealthyQuery(FakeMsiApi msi) =>
        new(msi, (_, _) => new InstallerQueryService.FallbackRead(0, 0, ProductPatchSets: HealthyPatchSets(msi)));

    [Fact]
    public async Task The_check_before_a_Move_or_Delete_drops_a_superseded_patch_on_such_a_machine()
    {
        // The re-verify runs the same enumeration, meets the same unsettled value and
        // withholds the row the same way, so the file comes out of the batch under the
        // cause a withheld row supports. The candidate is the path the enumeration gives
        // the row, which is the path the scan offered.
        var msi = ASupersededPatchBesideProductC(UnrelatedPackageUnsettled);
        var query = HealthyQuery(msi);
        var candidate = Assert.Single(
            (await query.GetRegisteredPackagesAsync()).Packages, r => r.PatchState == 2).LocalPackagePath;

        var result = await new RemovableReverifier(query, msi).ReverifyAsync(new[] { candidate });

        Assert.Empty(result.Surviving);
        Assert.Equal(new[] { candidate }, result.Dropped);
        Assert.Equal(new HeldBackReasons(RecordsUnreadable: 1), result.Reasons);
    }

    [Fact]
    public async Task The_same_check_where_every_recorded_path_settles_keeps_the_patch_in_the_batch()
    {
        // THE MUST-MISS FOR THE TEST ABOVE: C's value spelled ordinarily.
        var msi = ASupersededPatchBesideProductC(@"C:\Windows\Installer\c.msi");
        var query = HealthyQuery(msi);
        var candidate = Assert.Single(
            (await query.GetRegisteredPackagesAsync()).Packages, r => r.PatchState == 2).LocalPackagePath;

        var result = await new RemovableReverifier(query, msi).ReverifyAsync(new[] { candidate });

        Assert.Equal(new[] { candidate }, result.Surviving);
        Assert.Empty(result.Dropped);
    }

    // ---- A second registration of a superseded patch recording another path ----
    //
    // Two registrations of one patch whose recorded paths differ land on two rows, so the
    // superseded row carries no sign of the product holding the patch applied. The
    // per-pairing pass asks every installation it knows of about the patch itself, so that
    // product's answer reaches the superseded row whatever path its record names. Nothing
    // here says whether the two paths are one file, and the pass never compares them.

    private const string SharedPatchRecordedElsewhere = @"C:\Windows\Installer\shared-recorded-elsewhere.msp";

    /// <summary>
    /// Product A holds the patch superseded and no longer uninstallable at
    /// <see cref="SharedPatch"/>, and product B holds it with <paramref name="bState"/> at
    /// <see cref="SharedPatchRecordedElsewhere"/>.
    /// </summary>
    private static FakeMsiApi APatchRecordedUnderTwoPaths(string bState)
    {
        var msi = new FakeMsiApi();
        msi.AddProduct("{A}");
        msi.AddProduct("{B}");
        msi.AddPatch("{A}", "{P}", localPackage: SharedPatch, state: "2", uninstallable: "0");
        msi.AddPatch("{B}", "{P}", localPackage: SharedPatchRecordedElsewhere, state: bState, uninstallable: "0");
        return msi;
    }

    [Fact]
    public async Task A_second_registration_holding_the_patch_applied_under_another_path_keeps_it()
    {
        var result = await RunHealthy(APatchRecordedUnderTwoPaths(bState: "1"));

        var row = Assert.Single(result.Packages, r => r.PatchState == 2);
        // The per-product condition passed the row through, so what took the verdict is
        // B's answer to the per-pairing pass.
        Assert.Equal(ProductPatchSet.AllNonRemovable, row.ProductPatchSetVerdict);
        Assert.False(row.IsRemovable);
        // A live claim, not a withholding.
        Assert.False(row.RemovableWithheld);
        Assert.Single(result.Packages, r => r.PatchState == 1);
    }

    [Fact]
    public async Task The_same_machine_with_that_registration_holding_the_patch_superseded_offers_both_rows()
    {
        // THE MUST-MISS FOR THE TEST ABOVE, differing in B's one registration: B holds the
        // patch superseded and no longer uninstallable, so asking B finds nothing to keep
        // the file for.
        var result = await RunHealthy(APatchRecordedUnderTwoPaths(bState: "2"));

        var superseded = result.Packages.Where(r => r.PatchState == 2).ToList();
        Assert.Equal(2, superseded.Count);
        Assert.All(superseded, r => AssertOffered(r, expectedState: 2));
    }

    [Fact]
    public async Task The_check_before_a_Move_or_Delete_drops_a_superseded_patch_held_applied_under_another_path()
    {
        // The re-verify runs the same enumeration and its per-pairing pass asks B the same
        // question, so the file comes out of the batch under the cause a live claim
        // supports.
        var msi = APatchRecordedUnderTwoPaths(bState: "1");
        var query = HealthyQuery(msi);
        var candidate = Assert.Single(
            (await query.GetRegisteredPackagesAsync()).Packages, r => r.PatchState == 2).LocalPackagePath;

        var result = await new RemovableReverifier(query, msi).ReverifyAsync(new[] { candidate });

        Assert.Empty(result.Surviving);
        Assert.Equal(new[] { candidate }, result.Dropped);
        Assert.Equal(new HeldBackReasons(Reclaimed: 1), result.Reasons);
    }

    // ---- A superseded patch's file is read whatever it is called ----
    //
    // The row is a patch's by its registration, so the pass that reads a cached patch
    // file for the products it declares reads this one too, whatever its name.

    private const string PatchUnderAnotherName = @"C:\Windows\Installer\shared.bin";

    /// <summary>
    /// A package reader under which the file whose name ends in <paramref name="leaf"/>
    /// reads as a patch declaring <paramref name="targets"/>, and every other file reads
    /// as a patch naming no product.
    /// </summary>
    private sealed class OnePatchFileDeclaring(string leaf, params string[] targets) : IPackageIdentityReader
    {
        public PackageIdentity? Read(string filePath, bool isPatch, out string detail, out PackageReadRefusal refusal)
        {
            detail = string.Empty;
            refusal = PackageReadRefusal.WouldNotRead;
            return new PackageIdentity(string.Empty, isPatch,
                filePath.EndsWith(leaf, StringComparison.OrdinalIgnoreCase) ? targets : Array.Empty<string>());
        }
    }

    [Fact]
    public async Task A_superseded_patch_recorded_under_another_name_whose_file_does_not_read_is_withheld()
    {
        // The file is read like any cached patch file, does not yield the products it
        // declares, and the row is withheld with the unread-file marker set.
        var msi = new FakeMsiApi();
        msi.AddProduct("{A}");
        msi.AddPatch("{A}", "{P}", localPackage: PatchUnderAnotherName, state: "2", uninstallable: "0");

        var result = await new InstallerQueryService(msi,
                (_, _) => new InstallerQueryService.FallbackRead(0, 0, ProductPatchSets: HealthyPatchSets(msi)),
                identityReader: new OnePatchFileUnread("shared.bin"))
            .GetRegisteredPackagesAsync();

        var row = Assert.Single(result.Packages, r => r.PatchState == 2);
        Assert.False(row.IsRemovable);
        Assert.True(row.RemovableWithheld);
        Assert.True(row.WithheldOnUnreadableFile);
        Assert.False(row.WithheldOnRecordedPathUnestablished);
    }

    [Fact]
    public async Task A_superseded_patch_recorded_under_another_name_has_the_products_its_file_declares_asked()
    {
        // The file declares product B, which the product enumeration did not return and
        // which holds the patch applied. B is asked because the file named it, and its
        // answer keeps the row off the offer as a live claim, with B's reading, applied,
        // on the row.
        var msi = new FakeMsiApi();
        msi.AddProduct("{A}");
        msi.AddPatch("{A}", "{P}", localPackage: PatchUnderAnotherName, state: "2", uninstallable: "0");
        msi.SetPatchProperty("{P}", "{B}", "State", "1");
        msi.SetPatchProperty("{P}", "{B}", "Uninstallable", "0");

        var result = await new InstallerQueryService(msi,
                (_, _) => new InstallerQueryService.FallbackRead(0, 0,
                    ProductPatchSets: new Dictionary<string, ProductPatchSet>(StringComparer.OrdinalIgnoreCase)
                    {
                        ["{A}"] = ProductPatchSet.AllNonRemovable,
                        ["{B}"] = ProductPatchSet.AllNonRemovable,
                    }),
                identityReader: new OnePatchFileDeclaring("shared.bin", "{B}"))
            .GetRegisteredPackagesAsync();

        var row = Assert.Single(result.Packages);
        Assert.Equal(1, row.PatchState);
        Assert.False(row.IsRemovable);
        Assert.False(row.RemovableWithheld);
    }

    [Fact]
    public async Task A_program_whose_Uninstallable_comes_back_empty_beside_a_superseded_state_withholds_the_patch()
    {
        // B, asked because the file declares it, holds the patch superseded and answers
        // its Uninstallable with an empty value, which does not say whether the patch
        // can be rolled back. The row is withheld as a read that did not answer rather
        // than kept on a claim.
        var msi = new FakeMsiApi();
        msi.AddProduct("{A}");
        msi.AddPatch("{A}", "{P}", localPackage: PatchUnderAnotherName, state: "2", uninstallable: "0");
        msi.SetPatchProperty("{P}", "{B}", "State", "2");
        msi.SetPatchProperty("{P}", "{B}", "Uninstallable", "");

        var result = await RunDeclaring(msi, "{B}");

        var row = Assert.Single(result.Packages, r => r.PatchState == 2);
        Assert.False(row.IsRemovable);
        Assert.True(row.RemovableWithheld);
    }

    [Theory]
    [InlineData("empty", false)]
    [InlineData("empty", true)]
    [InlineData("failed", false)]
    [InlineData("failed", true)]
    public async Task A_program_whose_answer_does_not_come_withholds_the_patch_beside_a_claim_whichever_is_asked_first(
        string otherAnswer, bool claimAskedFirst)
    {
        // Two programs the file declares hold the patch. C has it applied, a live claim.
        // B's answer establishes nothing: an empty Uninstallable beside a superseded
        // State, or an Uninstallable read that fails. The row is withheld in either
        // order.
        var msi = new FakeMsiApi();
        msi.AddProduct("{A}");
        msi.AddPatch("{A}", "{P}", localPackage: PatchUnderAnotherName, state: "2", uninstallable: "0");
        msi.SetPatchProperty("{P}", "{B}", "State", "2");
        if (otherAnswer == "empty")
            msi.SetPatchProperty("{P}", "{B}", "Uninstallable", "");
        else
            msi.PatchPropertyResult[("{P}", "{B}", "Uninstallable")] = BadConfiguration;
        msi.SetPatchProperty("{P}", "{C}", "State", "1");
        msi.SetPatchProperty("{P}", "{C}", "Uninstallable", "0");

        var result = await RunDeclaring(msi, claimAskedFirst ? new[] { "{C}", "{B}" } : new[] { "{B}", "{C}" });

        var row = Assert.Single(result.Packages, r => r.PatchState == 2);
        Assert.False(row.IsRemovable);
        Assert.True(row.RemovableWithheld);
    }

    /// <summary>
    /// A scan of <paramref name="msi"/> on which the cached patch file at
    /// <see cref="PatchUnderAnotherName"/> declares <paramref name="targets"/>, in that
    /// order, and every product the scan can ask about holds no patch that could be
    /// uninstalled.
    /// </summary>
    private static Task<InstallerQueryResult> RunDeclaring(FakeMsiApi msi, params string[] targets)
    {
        var patchSets = new Dictionary<string, ProductPatchSet>(StringComparer.OrdinalIgnoreCase)
        {
            ["{A}"] = ProductPatchSet.AllNonRemovable,
        };
        foreach (var target in targets) patchSets[target] = ProductPatchSet.AllNonRemovable;

        return new InstallerQueryService(msi,
                (_, _) => new InstallerQueryService.FallbackRead(0, 0, ProductPatchSets: patchSets),
                identityReader: new OnePatchFileDeclaring("shared.bin", targets))
            .GetRegisteredPackagesAsync();
    }

    [Fact]
    public async Task The_same_patch_recorded_as_a_patch_file_that_reads_is_offered()
    {
        // THE MUST-MISS FOR THE TWO ABOVE: an ordinary cached patch file that reads and
        // declares no product that holds the patch.
        var msi = new FakeMsiApi();
        msi.AddProduct("{A}");
        msi.AddPatch("{A}", "{P}", localPackage: SharedPatch, state: "2", uninstallable: "0");

        var result = await new InstallerQueryService(msi,
                (_, _) => new InstallerQueryService.FallbackRead(0, 0, ProductPatchSets: HealthyPatchSets(msi)),
                identityReader: new OnePatchFileDeclaring("shared.msp"))
            .GetRegisteredPackagesAsync();

        AssertOffered(Assert.Single(result.Packages, r => r.PatchState == 2), expectedState: 2);
    }

    [Fact]
    public async Task A_superseded_patch_under_another_name_whose_file_does_not_read_keeps_its_marker_on_such_a_scan()
    {
        // On a scan where a recorded path is unsettled as well, the failed read withholds
        // the row first, so it keeps the marker and does not carry the flag, and the
        // missing-files split leaves it out where its file has gone.
        var msi = new FakeMsiApi();
        msi.AddProduct("{A}");
        msi.AddPatch("{A}", "{P}", localPackage: PatchUnderAnotherName, state: "2", uninstallable: "0");
        msi.AddProduct("{C}");
        msi.SetProductProperty("{C}", "LocalPackage", UnrelatedPackageUnsettled);

        var result = await new InstallerQueryService(msi,
                (_, _) => new InstallerQueryService.FallbackRead(0, 0, ProductPatchSets: HealthyPatchSets(msi)),
                identityReader: new OnePatchFileUnread("shared.bin"))
            .GetRegisteredPackagesAsync();

        var row = Assert.Single(result.Packages, r => r.PatchState == 2);
        Assert.True(row.RemovableWithheld);
        Assert.True(row.WithheldOnUnreadableFile);
        Assert.False(row.WithheldOnRecordedPathUnestablished);
        Assert.False(MissingFilesReport.Affected(row with { FileExists = false }));
    }

    // ---- Both sources degraded at once refuses the scan ----
    //
    // Withholding the removable class answers a claim the API loop lost because
    // the registry fallback still contributes that product's paths as
    // non-removable rows, which is what keeps its cached file out of the orphan
    // list. When the fallback is failing reads of its own that recovery is no
    // longer established, and the scan would offer a file as an orphan on a run
    // whose withholding was bounded to the superseded class.

    [Fact]
    public async Task Both_sources_degraded_refuses_the_scan()
    {
        var msi = new FakeMsiApi();
        msi.AddProduct("{A}");
        msi.SetProductProperty("{A}", "LocalPackage", @"C:\Windows\Installer\a.msi");
        msi.AddProduct("{B}");
        msi.ProductPropertyResult[("{B}", "LocalPackage")] = BadConfiguration;

        var ex = await Assert.ThrowsAsync<LocalisedInvalidOperationException>(
            () => Run(msi, fallbackFailures: 1));

        Assert.Equal(Strings.Error_ScanRecordsUnreadable, ex.Message);
    }

    [Fact]
    public async Task A_lost_claim_with_a_clean_fallback_still_scans()
    {
        // One source short is the state the withholding was built for, and it
        // must stay a completed scan: refusing here would take orphan cleanup
        // away over a condition that does not threaten it.
        const string dead = @"C:\Windows\Installer\one-source-short.msp";
        var msi = new FakeMsiApi();
        msi.AddProduct("{A}");
        msi.AddPatch("{A}", "{P}", localPackage: dead, state: "2", uninstallable: "0");
        msi.AddProduct("{B}");
        msi.ProductPropertyResult[("{B}", "LocalPackage")] = BadConfiguration;

        var result = await Run(msi, fallbackFailures: 0);

        AssertWithheldByADegradedEnumeration(Assert.Single(result.Packages, r => r.LocalPackagePath == dead), expectedState: 2);
        Assert.Equal(1, result.UnaccountedProductCount);
    }

    [Fact]
    public async Task A_failing_fallback_with_a_clean_enumeration_scans_and_holds_every_superseded_patch()
    {
        // The scan goes ahead, because the API read every product cleanly. A key in
        // UserData that would not read may be the only record of a program no walk
        // lists, whose listing could name the patch, so every superseded patch is held
        // back, scan wide.
        const string dead = @"C:\Windows\Installer\fallback-noise.msp";
        var msi = new FakeMsiApi();
        msi.AddProduct("{A}");
        msi.AddPatch("{A}", "{P}", localPackage: dead, state: "2", uninstallable: "0");

        var result = await RunHealthy(msi, fallbackFailures: 3);

        var row = Assert.Single(result.Packages, r => r.LocalPackagePath == dead);
        Assert.False(row.IsRemovable);
        Assert.True(row.RemovableWithheld);
        Assert.True(row.WithheldScanWide);
        Assert.True(row.OtherHoldNotRuledOut);
        Assert.False(row.WithheldOnUnlistedKey);
        Assert.Equal(2, row.PatchState);
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
    // A volume-GUID value and the device form beside it. Neither carries a drive
    // letter behind the prefix, so neither can have it taken off: what would be
    // left has no root at all, and GetFullPath completes such a value from the
    // process working directory, which is a different answer from the GUI and
    // from the command line for the same registration.
    [InlineData(@"\\?\Volume{9c3a1d2e-0000-0000-0000-100000000000}\Windows\Installer\vol.msi")]
    [InlineData(@"\??\Volume{9c3a1d2e-0000-0000-0000-100000000000}\Windows\Installer\vol.msi")]
    [InlineData(@"\\?\GLOBALROOT\Device\HarddiskVolume3\Windows\Installer\vol.msi")]
    public async Task A_value_with_no_drive_letter_behind_its_prefix_is_never_completed_from_the_working_directory(string registeredAs)
    {
        var msi = new FakeMsiApi();
        msi.AddProduct("{A}");
        msi.SetProductProperty("{A}", "LocalPackage", registeredAs);

        var claimed = Assert.Single((await Run(msi)).Packages).LocalPackagePath;

        // THE ASSERTION NAMES THE DEFECT RATHER THAN ONE OF THE TWO SHAPES A
        // CORRECT ANSWER CAN TAKE, and the difference decides whether this test
        // says the same thing on every machine. Two answers are right here. A
        // volume the machine does not have leaves the value carrying its prefix,
        // still naming its file and still naming its volume. A device the machine
        // does have resolves to the drive-letter spelling the folder walk
        // produces, which is what the resolver is for and is the better of the
        // two. Asserting the prefix survives accepts the first and refuses the
        // second, so it turns on whether the machine running it happens to have
        // the volume named in the value.
        //
        // What is wrong on every machine is the third answer: the prefix taken
        // off blind, leaving a rootless value that GetFullPath completes against
        // wherever the process was started. That is the one spelling that moves
        // with the caller, and naming it directly holds both correct answers and
        // admits neither the old one.
        var completedFromTheWorkingDirectory = Path.GetFullPath(registeredAs.Substring(4));
        Assert.NotEqual(completedFromTheWorkingDirectory, claimed, StringComparer.OrdinalIgnoreCase);
        Assert.EndsWith(@"\vol.msi", claimed, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task A_value_that_cannot_be_normalised_is_claimed_exactly_as_returned()
    {
        // THE EMBEDDED-NULL TEST AT THE FRONT OF THE NORMALISATION REFUSES THIS
        // VALUE, so it never reaches GetFullPath.
        // An_embedded_null_is_refused_as_its_own_cause_and_not_as_a_full_path_failure
        // below holds that refusal apart from GetFullPath's.
        //
        // The claim must survive the refusal anyway: dropping the row would turn a
        // spelling nobody can improve into a file with no claim on it at all, which
        // is an orphan.
        const string unimprovable = "C:\\Windows\\Installer\\bad\0name.msi";
        var msi = new FakeMsiApi();
        msi.AddProduct("{A}");
        msi.SetProductProperty("{A}", "LocalPackage", unimprovable);

        var result = await Run(msi);

        Assert.Equal(unimprovable, Assert.Single(result.Packages).LocalPackagePath);
    }

    [Fact]
    public async Task An_embedded_null_is_refused_as_its_own_cause_and_not_as_a_full_path_failure()
    {
        // WHAT THIS FIXTURE HOLDS IS THE POINT OF IT. On Windows the expansion cuts a
        // value at an embedded null and returns without throwing, so the embedded-null
        // test runs ahead of it: a value carrying a null is refused and counted before
        // the expansion can shorten it to C:\Windows\Installer\bad, a path that on
        // another machine could be a real file.
        const string withNull = "C:\\Windows\\Installer\\bad\0name.msi";
        var msi = new FakeMsiApi();
        msi.AddProduct("{A}");
        msi.SetProductProperty("{A}", "LocalPackage", withNull);

        var census = (await Run(msi)).Census;

        Assert.Equal(1, census.PathNormalisationRefusedAtEmbeddedNullCount);

        // AND THE OTHER THREE UNMOVED, which is the half that would go unnoticed. The
        // split exists so a report can say which cause fired; a member that also
        // incremented a neighbour would leave the total right and every part of it
        // wrong, and nothing reading the payload could tell.
        Assert.Equal(0, census.PathNormalisationRefusedAtExpansionCount);
        Assert.Equal(0, census.PathNormalisationRefusedAtPrefixStripCount);
        Assert.Equal(0, census.PathNormalisationRefusedAtFullPathCount);
        Assert.Equal(1, census.PathNormalisationRefusedTotal);
    }

    [Fact]
    public async Task An_ordinary_recorded_value_counts_no_normalisation_refusal()
    {
        // THE MUST-MISS CONTROL FOR THE TEST ABOVE, and it guards the more expensive
        // direction. A check that answered yes to every registration would satisfy
        // that test exactly as well as a correct one does, and the scan withholds its
        // whole walk-derived offer above zero, so a false positive here costs a
        // healthy machine everything it came for.
        var msi = new FakeMsiApi();
        msi.AddProduct("{A}");
        msi.SetProductProperty("{A}", "LocalPackage", @"C:\Windows\Installer\ordinary.msi");

        var census = (await Run(msi)).Census;

        Assert.Equal(0, census.PathNormalisationRefusedTotal);
    }

    /// <summary>
    /// THE RESOLVER'S DENOMINATOR, DRIVEN THROUGH A REAL SCAN. The five outcome
    /// counters are read against the attempts count: a scan that asked about no path
    /// reports five zeros, which without it are indistinguishable from five clean
    /// answers, and a receiver takes the second reading.
    ///
    /// WHICH FAILURE THE KERNEL GIVES IS NOT PINNED, DELIBERATELY. A volume GUID no
    /// machine has cannot resolve, so exactly one of the five must move; which one is
    /// a property of the platform rather than of this code, and asserting it would
    /// pin a machine. The pair that IS this counter's contract is asserted: the
    /// attempt was counted, and it produced exactly one outcome.
    /// <see cref="InstallerQueryServicePathCensusTests"/> records each of the five
    /// straight into a census and holds each to a counter of its own.
    /// </summary>
    [Fact]
    public async Task A_volume_guid_value_counts_a_resolver_attempt_and_exactly_one_outcome()
    {
        var msi = new FakeMsiApi();
        msi.AddProduct("{A}");
        msi.SetProductProperty("{A}", "LocalPackage",
            @"\\?\Volume{9c3a1d2e-0000-0000-0000-100000000000}\Windows\Installer\vol.msi");

        var census = (await Run(msi)).Census;

        Assert.Equal(1, census.PathResolverAttemptCount);
        Assert.Equal(1,
            census.PathResolverNotAPathCount
            + census.PathResolverNoAncestorCount
            + census.PathResolverOpenRefusedCount
            + census.PathResolverNoFinalNameCount
            + census.PathResolverFaultedCount);
        // AND THE SPELLING IS RECOGNISED AS ONE ONLY THE DISK COULD SETTLE, which is
        // this fixture's own subject. Every recorded value is resolved, so the attempts
        // figure above is 1 for an ordinary path too and cannot tell this value apart
        // from any other.
        Assert.Equal(1, census.PathFlaggedSpellingCount);
    }

    /// <summary>
    /// The other trigger, which is the one a real machine is likelier to hold. The
    /// outcome is not asserted at all here and the reason is the fixture rather than
    /// the assertion: the walk up to an existing ancestor reaches this machine's own
    /// Windows folder, so whether the resolution succeeds depends on the runner and
    /// on whether the volume still creates 8dot3 aliases. What does not depend on
    /// either is that the ask was counted.
    /// </summary>
    [Fact]
    public async Task An_8dot3_value_counts_a_resolver_attempt()
    {
        var msi = new FakeMsiApi();
        msi.AddProduct("{A}");
        msi.SetProductProperty("{A}", "LocalPackage", @"C:\Windows\INSTAL~1\9f05cba.msi");

        var census = (await Run(msi)).Census;

        Assert.Equal(1, census.PathResolverAttemptCount);
        // THE HALF THAT IS ABOUT THIS VALUE RATHER THAN ABOUT EVERY VALUE. Every
        // recorded path is asked about, so the count above says only that a
        // registration was read; the 8.3 alias is what this fixture is for, and the
        // character scan that recognises it is what the count below reports.
        Assert.Equal(1, census.PathFlaggedSpellingCount);
    }

    [Fact]
    public async Task An_ordinary_recorded_value_is_asked_about_too_and_carries_no_flagged_spelling()
    {
        // EVERY RECORDED VALUE IS ASKED ABOUT, ORDINARY ONES INCLUDED. The character
        // scan does not decide whether a handle is opened; it decides only what the
        // flagged-spelling count reports. The invariant that buys is that a claim
        // leaving normalisation is either a location the kernel proved or one whose
        // failure to resolve has been counted and withholds the whole walk-derived
        // offer and every superseded patch still carrying its removable verdict.
        //
        // SO THIS FIXTURE'S MUST-MISS IS THE FLAGGED-SPELLING COUNT. An ordinary value
        // must not be counted as carrying a spelling only the disk can settle, and that
        // figure is what tells this fixture apart from the two above it. The attempts
        // count cannot: it reads 1 for every recorded value on every machine.
        //
        // NOTHING HERE ASSERTS THE OUTCOME OF THE RESOLUTION, for the reason the 8.3
        // fixture gives: whether this path resolves depends on the machine the test runs
        // on, and the ask being counted does not.
        var msi = new FakeMsiApi();
        msi.AddProduct("{A}");
        msi.SetProductProperty("{A}", "LocalPackage", @"C:\Windows\Installer\ordinary.msi");

        var census = (await Run(msi)).Census;

        Assert.Equal(1, census.PathResolverAttemptCount);
        Assert.Equal(0, census.PathFlaggedSpellingCount);
        // One registration, one ask, and at most one outcome recorded against it. A
        // refusal total ABOVE the attempts count would mean an outcome was recorded
        // without an ask, which is the reading that makes the five counters meaningless.
        Assert.InRange(census.PathResolverRefusedTotal, 0, census.PathResolverAttemptCount);
    }

    // ---- The superseded-patch condition: every product, every patch ----

    private const string CleanPatch = @"C:\Windows\Installer\superseded.msp";

    /// <summary>
    /// Runs with a fallback supplying the registry's per-product patch verdicts, which
    /// is the source that makes the superseded condition's set trustworthy. Without it
    /// every product reads unestablished and nothing is offered, so any test about the
    /// offer has to say what the registry saw.
    /// </summary>
    private static async Task<InstallerQueryResult> RunWithPatchSets(
        FakeMsiApi msi, params (string Product, ProductPatchSet Set)[] sets)
    {
        var map = sets.ToDictionary(x => x.Product, x => x.Set, StringComparer.OrdinalIgnoreCase);
        return await new InstallerQueryService(
                msi,
                (_, _) => new InstallerQueryService.FallbackRead(
                    0, 0, ProductPatchSets: map))
            .GetRegisteredPackagesAsync();
    }

    [Fact]
    public async Task A_superseded_non_removable_patch_on_a_clean_product_is_offered()
    {
        // THE MUST-HIT CONTROL FOR EVERY TEST BELOW. Without it a condition that
        // withheld unconditionally would pass all of them, and the app would offer
        // nothing while every assertion about withholding stayed green.
        var msi = new FakeMsiApi();
        msi.AddProduct("{A}");
        msi.SetProductProperty("{A}", "LocalPackage", @"C:\Windows\Installer\a.msi");
        msi.AddPatch("{A}", "{P}", CleanPatch, state: "2", uninstallable: "0");

        var result = await RunWithPatchSets(msi,
            ("{A}", ProductPatchSet.AllNonRemovable));

        var row = result.Packages.Single(p => p.LocalPackagePath == CleanPatch);
        Assert.True(row.IsRemovable);
        Assert.False(row.RemovableWithheld);
    }

    [Fact]
    public async Task A_sibling_patch_that_can_be_uninstalled_withholds_the_superseded_one()
    {
        // THE ROUTE THIS CLOSES. Uninstalling the patch that
        // superseded this one rolls the product back onto this one's cached file; with
        // the file gone that rollback went all the way to the unpatched base and
        // reported success. The old rule read THIS patch's own removability, which says
        // nothing about whether the superseding patch can be uninstalled.
        var msi = new FakeMsiApi();
        msi.AddProduct("{A}");
        msi.SetProductProperty("{A}", "LocalPackage", @"C:\Windows\Installer\a.msi");
        msi.AddPatch("{A}", "{P}", CleanPatch, state: "2", uninstallable: "0");

        var result = await RunWithPatchSets(msi,
            ("{A}", ProductPatchSet.RemovablePatchPresent));

        var row = result.Packages.Single(p => p.LocalPackagePath == CleanPatch);
        Assert.False(row.IsRemovable);
        // withheld FALSE: the app looked and the answer was no, which is a different
        // sentence from being unable to look.
        Assert.False(row.RemovableWithheld);
    }

    [Fact]
    public async Task A_product_whose_patch_set_could_not_be_established_withholds_and_says_so()
    {
        var msi = new FakeMsiApi();
        msi.AddProduct("{A}");
        msi.SetProductProperty("{A}", "LocalPackage", @"C:\Windows\Installer\a.msi");
        msi.AddPatch("{A}", "{P}", CleanPatch, state: "2", uninstallable: "0");

        var result = await RunWithPatchSets(msi,
            ("{A}", ProductPatchSet.Unestablished));

        var row = result.Packages.Single(p => p.LocalPackagePath == CleanPatch);
        Assert.False(row.IsRemovable);
        // withheld TRUE: the app could not tell. The two causes reach two different
        // sentences and a surface that names one must name the right one.
        Assert.True(row.RemovableWithheld);
    }

    [Fact]
    public async Task A_registry_that_names_no_products_at_all_withholds_everything()
    {
        // The production shape of a machine whose UserData would not open. An absent
        // verdict is an inability and not a clean bill, so the offer empties rather
        // than the condition passing by default.
        var msi = new FakeMsiApi();
        msi.AddProduct("{A}");
        msi.SetProductProperty("{A}", "LocalPackage", @"C:\Windows\Installer\a.msi");
        msi.AddPatch("{A}", "{P}", CleanPatch, state: "2", uninstallable: "0");

        var result = await RunWithPatchSets(msi);

        var row = result.Packages.Single(p => p.LocalPackagePath == CleanPatch);
        Assert.False(row.IsRemovable);
        Assert.True(row.RemovableWithheld);
    }

    [Fact]
    public async Task The_condition_must_hold_for_EVERY_product_the_patch_is_registered_to()
    {
        // D1, and the reason the brief's original wording was under-specified. A patch
        // is cached once and registered once per product it applies to, and its one
        // file is shared, so a rollback on ANY of those products reaches for it. One
        // clean product is not enough.
        var msi = new FakeMsiApi();
        foreach (var code in new[] { "{A}", "{B}" })
        {
            msi.AddProduct(code);
            msi.SetProductProperty(code, "LocalPackage", $@"C:\Windows\Installer\{code.Trim('{', '}')}.msi");
            msi.AddPatch(code, "{P}", CleanPatch, state: "2", uninstallable: "0");
        }

        var result = await RunWithPatchSets(msi,
            ("{A}", ProductPatchSet.AllNonRemovable),
            ("{B}", ProductPatchSet.RemovablePatchPresent));

        var row = result.Packages.Single(p => p.LocalPackagePath == CleanPatch);
        Assert.False(row.IsRemovable);
    }

    [Fact]
    public async Task An_obsoleted_patch_is_never_offered_even_on_a_clean_product()
    {
        // Off the offer for policy rather than for safety: never observed on any
        // machine, so it reclaims nothing, and never tested. It is counted instead.
        var msi = new FakeMsiApi();
        msi.AddProduct("{A}");
        msi.SetProductProperty("{A}", "LocalPackage", @"C:\Windows\Installer\a.msi");
        msi.AddPatch("{A}", "{P}", CleanPatch, state: "4", uninstallable: "0");

        var result = await RunWithPatchSets(msi,
            ("{A}", ProductPatchSet.AllNonRemovable));

        var row = result.Packages.Single(p => p.LocalPackagePath == CleanPatch);
        Assert.False(row.IsRemovable);
        Assert.Equal(4, row.PatchState);
    }

    [Theory]
    // The patch's own conjunct survives: it declares itself removable, so it can be
    // rolled back and its file is needed to do it.
    [InlineData("1")]
    // Absent, which is an inability rather than a finding and refuses either way.
    [InlineData("")]
    public async Task A_patch_that_does_not_positively_declare_zero_is_not_offered(string uninstallable)
    {
        var msi = new FakeMsiApi();
        msi.AddProduct("{A}");
        msi.SetProductProperty("{A}", "LocalPackage", @"C:\Windows\Installer\a.msi");
        msi.AddPatch("{A}", "{P}", CleanPatch, state: "2", uninstallable: uninstallable);

        var result = await RunWithPatchSets(msi,
            ("{A}", ProductPatchSet.AllNonRemovable));

        Assert.False(result.Packages.Single(p => p.LocalPackagePath == CleanPatch).IsRemovable);
    }

    [Fact]
    public async Task An_unreadable_Uninstallable_is_not_offered_and_the_registry_cannot_rescue_it()
    {
        // Both halves have to answer positively. A read that failed establishes
        // nothing, and a clean registry reading of the product does not stand in for
        // the patch's own answer: the two sources union to withhold more, never to
        // permit what one of them refused.
        var msi = new FakeMsiApi();
        msi.AddProduct("{A}");
        msi.SetProductProperty("{A}", "LocalPackage", @"C:\Windows\Installer\a.msi");
        msi.AddPatch("{A}", "{P}", CleanPatch, state: "2", uninstallable: "0");
        msi.PatchPropertyResult[("{P}", "{A}", "Uninstallable")] = BadConfiguration;

        var result = await RunWithPatchSets(msi,
            ("{A}", ProductPatchSet.AllNonRemovable));

        var row = result.Packages.Single(p => p.LocalPackagePath == CleanPatch);
        Assert.False(row.IsRemovable);
        Assert.True(row.VerdictUnreadable);
    }

    // ---- The instance-product reading, which decides the walk-derived offer ----
    //
    // THE READING DECIDES THE WALK-DERIVED OFFER, and the pass that reads a product
    // code out of a cached file is the last one between an unclaimed candidate and the
    // offer. What acts on each reading is the mark it puts on the installation's listed
    // row (ListedInstallation.SecondCopyNotRuledOut), which that pass reads; the two
    // counts go to the report and decide nothing. The tests below are about the
    // READING: what a positive is, what an absence is, and what a failure is.
    // The population these cover is the products the enumeration returned; the ones it
    // lost are in InstallerQueryServiceSecondInstanceTests.

    [Theory]
    // Microsoft's documented positive. 1 is the value an instance transform writes.
    [InlineData("1", 1)]
    // Compared as a NUMBER and not against the string "1", because nothing
    // documents the spelling the API returns it in. Both of these would read as
    // ordinary products on a string test and are instance products.
    [InlineData("01", 1)]
    [InlineData(" 1 ", 1)]
    // A second instance beyond the first is still an instance product.
    [InlineData("2", 1)]
    // The documented ordinary installation.
    [InlineData("0", 0)]
    // Not a number, so not a positive. It is also not a failure: the read
    // succeeded and gave something nobody can act on.
    [InlineData("yes", 0)]
    [InlineData("", 0)]
    public async Task An_instance_product_is_counted_only_on_a_positive_reading(string value, int expected)
    {
        var msi = new FakeMsiApi();
        msi.AddProduct("{A}");
        msi.SetProductProperty("{A}", "LocalPackage", @"C:\Windows\Installer\a.msi");
        msi.SetProductProperty("{A}", "InstanceType", value);

        var result = await Run(msi);

        Assert.Equal(expected, result.Census.InstanceProductCount);
        Assert.Equal(0, result.Census.InstanceTypeUnreadableCount);
    }

    [Fact]
    public async Task A_product_carrying_no_InstanceType_is_neither_counted_nor_unreadable()
    {
        // The ordinary case. An absent property is documented as meaning an ordinary
        // installation, so it is a clean negative rather than a read that failed, and
        // the two must not share a counter.
        var msi = new FakeMsiApi();
        msi.AddProduct("{A}");
        msi.SetProductProperty("{A}", "LocalPackage", @"C:\Windows\Installer\a.msi");
        msi.ProductPropertyResult[("{A}", "InstanceType")] = UnknownProperty;

        var result = await Run(msi);

        Assert.Equal(0, result.Census.InstanceProductCount);
        Assert.Equal(0, result.Census.InstanceTypeUnreadableCount);
    }

    [Fact]
    public async Task An_unreadable_InstanceType_counts_apart_and_withholds_nothing()
    {
        // THE LOAD-BEARING ONE. The count exists so a zero cannot be read as "no
        // such product here", which needs the failures counted separately. And the
        // failure must not reach recordsShort: every other failed read in that loop
        // is about a CLAIM that never got to the merge and withholds the whole
        // removable class for the scan, where this property carries no claim on any
        // file at all. Wiring it into that count would empty a machine's offer over
        // a diagnostic nothing acts on.
        var msi = new FakeMsiApi();
        msi.AddProduct("{A}");
        msi.SetProductProperty("{A}", "LocalPackage", @"C:\Windows\Installer\a.msi");
        msi.ProductPropertyResult[("{A}", "InstanceType")] = BadConfiguration;

        var result = await Run(msi);

        Assert.Equal(1, result.Census.InstanceTypeUnreadableCount);
        Assert.Equal(0, result.Census.InstanceProductCount);
        // The withholding, from both ends: nothing unaccounted for, and the row
        // still carries the verdict it would have carried.
        Assert.Equal(0, result.UnaccountedProductCount);
        Assert.Equal(0, result.Census.UnreadableProducts);
    }

    [Fact]
    public async Task The_two_instance_counters_are_per_product_and_independent()
    {
        // Three products, one of each shape, so the counters are shown not to be
        // booleans wearing an int and not to be reading each other's rows.
        var msi = new FakeMsiApi();
        foreach (var code in new[] { "{A}", "{B}", "{C}" })
        {
            msi.AddProduct(code);
            msi.SetProductProperty(code, "LocalPackage", $@"C:\Windows\Installer\{code.Trim('{', '}')}.msi");
        }
        msi.SetProductProperty("{A}", "InstanceType", "1");
        msi.SetProductProperty("{B}", "InstanceType", "0");
        msi.ProductPropertyResult[("{C}", "InstanceType")] = BadConfiguration;

        var result = await Run(msi);

        Assert.Equal(1, result.Census.InstanceProductCount);
        Assert.Equal(1, result.Census.InstanceTypeUnreadableCount);
        Assert.Equal(3, result.Census.ProductCount);
        Assert.Equal(0, result.UnaccountedProductCount);
    }

    // ---- The environment-variable form ----

    // The variable is this test's own and is set for the duration of one test
    // rather than borrowed from the machine, so the assertion does not depend on
    // where Windows happens to be installed and nothing else in a parallel run
    // reads it. The name appears in the attributes as a literal because an
    // InlineData value cannot interpolate a constant; the two must stay in step.
    private const string EnvRootName = "INSTALLERCLEAN_TEST_ROOT";
    private const string EnvRootValue = @"C:\TestWindows";

    [Theory]
    // The plain form. GetFullPath completes an unexpanded value from the process
    // working directory into a well-formed path naming nothing, so the expansion is
    // what puts this claim on the file it means.
    [InlineData(@"%INSTALLERCLEAN_TEST_ROOT%\Installer\env.msi")]
    // The same with a doubled separator and a relative segment on top, so the
    // expansion and GetFullPath are shown to compose rather than either undoing
    // the other.
    [InlineData(@"%INSTALLERCLEAN_TEST_ROOT%\Temp\..\\Installer\env.msi")]
    // Behind each prefix, which is what pins the ORDER rather than merely the
    // expansion. StripLongPathPrefix takes a prefix off a DRIVE-ROOTED path only,
    // and neither of these is drive-rooted until the variable is expanded, so
    // expanding second leaves the prefix on: the \??\ form's leading separator is
    // then read as rooted on whatever drive the process is running from, and the
    // \\?\ form is left alone by GetFullPath on purpose. Both would name a file
    // nowhere.
    [InlineData(@"\??\%INSTALLERCLEAN_TEST_ROOT%\Installer\env.msi")]
    [InlineData(@"\\?\%INSTALLERCLEAN_TEST_ROOT%\Installer\env.msi")]
    public async Task An_environment_variable_in_a_claim_is_expanded_before_the_prefix_strip(string registeredAs)
    {
        var previous = Environment.GetEnvironmentVariable(EnvRootName);
        Environment.SetEnvironmentVariable(EnvRootName, EnvRootValue);
        try
        {
            var msi = new FakeMsiApi();
            msi.AddProduct("{A}");
            msi.SetProductProperty("{A}", "LocalPackage", registeredAs);

            var result = await Run(msi);

            Assert.Equal($@"{EnvRootValue}\Installer\env.msi",
                Assert.Single(result.Packages).LocalPackagePath);
        }
        finally
        {
            Environment.SetEnvironmentVariable(EnvRootName, previous);
        }
    }

    [Fact]
    public async Task A_patch_claim_is_expanded_the_same_way()
    {
        // The patch side separately, because it reaches the normaliser by its own
        // call site and because the .msp is the file a wrong answer here would put
        // on the list.
        var previous = Environment.GetEnvironmentVariable(EnvRootName);
        Environment.SetEnvironmentVariable(EnvRootName, EnvRootValue);
        try
        {
            var msi = new FakeMsiApi();
            msi.AddProduct("{A}");
            msi.AddPatch("{A}", "{P}",
                localPackage: @"%INSTALLERCLEAN_TEST_ROOT%\Installer\env.msp",
                state: "2", uninstallable: "0");

            var result = await Run(msi);

            Assert.Equal($@"{EnvRootValue}\Installer\env.msp",
                Assert.Single(result.Packages).LocalPackagePath);
        }
        finally
        {
            Environment.SetEnvironmentVariable(EnvRootName, previous);
        }
    }

    [Fact]
    public async Task A_variable_the_machine_does_not_hold_is_left_in_the_claim()
    {
        // Pinned here rather than asserted in a comment, because it is a property
        // of the platform call and not of this code, and because the alternative
        // behaviour is the dangerous one: a variable EMPTIED rather than left would
        // turn this value into "\Installer\absent.msi", which GetFullPath roots on
        // the current drive and which then looks like an ordinary claim on a
        // location nobody registered. Left as written, the claim stays recognisably
        // unspellable and behaves exactly as it did before expansion existed.
        //
        // The name is deliberately one no machine sets, and it is not the name the
        // tests above set: those restore the previous value in a finally, so a
        // failure there cannot leave a value behind that would quietly make this
        // test assert nothing.
        const string absent = @"%INSTALLERCLEAN_TEST_NO_SUCH_ROOT%\Installer\absent.msi";
        var msi = new FakeMsiApi();
        msi.AddProduct("{A}");
        msi.SetProductProperty("{A}", "LocalPackage", absent);

        var claimed = Assert.Single((await Run(msi)).Packages).LocalPackagePath;

        Assert.Contains("%INSTALLERCLEAN_TEST_NO_SUCH_ROOT%", claimed, StringComparison.Ordinal);
        Assert.EndsWith(@"\Installer\absent.msi", claimed, StringComparison.OrdinalIgnoreCase);
    }

    // ---- The SID-buffer retry's own return code ----

    [Fact]
    public async Task AccessDenied_from_the_sid_retry_refuses_the_scan()
    {
        // The access check has to cover the retry's return code too, not just
        // the first call's. A refusal coming back from the second call and
        // reaching the arm for a row that did not read would refuse the scan
        // saying an entry came back unreadable, when Windows had refused access.
        // The exception type is what tells the two refusals apart.
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
    public async Task The_census_carries_its_terms_apart_and_the_count_takes_only_what_decides()
    {
        // One product's records are short, the registry names ten products against
        // the API's two, and four cached files on disk are claimed by the registry
        // alone, one of them the short product's own. Each term survives separately.
        var msi = new FakeMsiApi();
        msi.AddProduct("{A}");
        msi.AddProduct("{B}");
        msi.SetProductProperty("{A}", "LocalPackage", @"C:\Windows\Installer\a.msi");
        msi.SetProductProperty("{B}", "LocalPackage", @"C:\Windows\Installer\b.msi");
        msi.ProductPropertyResult[("{B}", "LocalPackage")] = BadConfiguration;

        var result = await RunAgainstRegistry(msi, registryProducts: 10,
            unclaimedProductFileCodes: ["{B}", "{X}", "{Y}", "{Z}"]);

        Assert.Equal(1, result.Census.UnreadableProducts);
        // ZERO skipped rows, and it is not the same number as the one above: a
        // product whose row came back and whose value would not read is still a
        // product the API returned, and a row the walk cannot read refuses the
        // scan before any census is built.
        Assert.Equal(0, result.Census.SkippedProductRows);
        Assert.Equal(10, result.Census.RegistryProductKeys);
        Assert.Equal(2, result.Census.ProductCount);
        Assert.Equal(4, result.Census.UnclaimedProductFiles);
        Assert.Equal(0, result.Census.UnclaimedPatchFiles);

        // The figure the app acts on: the one short product, once, though its own
        // registry entry also names an unclaimed file. The other three unclaimed files
        // decide nothing on their own, and the registry's ten keys against the API's two
        // contribute NOTHING either: a difference between two totals is not evidence
        // about any product.
        Assert.Equal(1, result.Census.UnsettledEnumeratedProductCount);
        Assert.Equal(1, result.UnaccountedProductCount);
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
        // THE CASE NO SINGLE FIGURE CAN EXPRESS. The app withholds nothing on a
        // difference between totals, so on the app's own arithmetic this machine
        // is indistinguishable from a clean one. Both headcounts travel anyway,
        // because the report's job is to describe machines rather than to repeat
        // the app's conclusions, and a fleet where this difference is routinely
        // large says something about registry residue that no verdict carries.
        var msi = new FakeMsiApi();
        msi.AddProduct("{A}");
        msi.SetProductProperty("{A}", "LocalPackage", @"C:\Windows\Installer\a.msi");

        var result = await RunAgainstRegistry(msi, registryProducts: 3);

        Assert.Equal(3, result.Census.RegistryProductKeys);
        Assert.Equal(1, result.Census.ProductCount);
        // The app itself withheld nothing, and no band was involved in that.
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

    [Fact]
    public async Task The_census_separates_failed_reads_from_the_files_they_left_unread()
    {
        // The two counts answer different questions and a machine where they
        // diverge is the reason both travel. One shared patch whose read fails
        // under both products holding it is two failed reads and one file nobody
        // could judge; reported as one number, that machine is indistinguishable
        // from a machine with two separate unjudgeable files.
        const string shared = @"C:\Windows\Installer\shared.msp";
        var msi = new FakeMsiApi();
        msi.AddProduct("{A}");
        msi.AddProduct("{B}");
        msi.AddPatch("{A}", "{P}", localPackage: shared, state: "2", uninstallable: "0");
        msi.AddPatch("{B}", "{P}", localPackage: shared, state: "2", uninstallable: "0");
        msi.PatchPropertyResult[("{P}", "{A}", "State")] = BadConfiguration;
        msi.PatchPropertyResult[("{P}", "{B}", "State")] = BadConfiguration;

        var result = await Run(msi);

        Assert.Equal(2, result.Census.UnreadablePatchStates);
        Assert.Equal(1, result.Census.UnreadableVerdictPaths);
    }

    [Fact]
    public async Task The_census_counts_no_unread_verdict_on_a_scan_that_read_everything()
    {
        // The control for the pair above: on a machine where every read answers,
        // both terms are zero and a report carrying a non-zero one is saying
        // something about that machine rather than about the app.
        var msi = new FakeMsiApi();
        msi.AddProduct("{A}");
        msi.AddPatch("{A}", "{P}", localPackage: @"C:\Windows\Installer\p.msp", state: "1", uninstallable: "1");

        var result = await Run(msi);

        Assert.Equal(0, result.Census.UnreadablePatchStates);
        Assert.Equal(0, result.Census.UnreadableVerdictPaths);
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
}
