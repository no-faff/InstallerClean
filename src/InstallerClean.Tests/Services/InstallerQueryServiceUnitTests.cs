using InstallerClean.Interop;
using InstallerClean.Models;
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
/// The registry fallback runs after the API enumeration on every call. On a CI
/// host it either reads real UserData entries or throws and is swallowed to
/// crash.log; either way it only ADDS non-removable rows, so these assertions
/// filter to the specific paths the fake produces rather than asserting a total
/// count (except where the fake yields entries the fallback cannot collide
/// with). That the fallback only ever adds is the very rule the merge tests
/// pin, and they call <c>MergeClaim</c> directly to do it: the fallback reads
/// the live registry through no seam, so a test cannot feed it a colliding row.
/// </summary>
public class InstallerQueryServiceUnitTests
{
    private const uint Success = 0, AccessDenied = 5, MoreData = 234, NoMoreItems = 259;

    private static async Task<InstallerQueryResult> Run(FakeMsiApi msi) =>
        await new InstallerQueryService(msi).GetRegisteredPackagesAsync();

    // ---- Item 1: shared-patch verdict merge ----

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
    public async Task A_product_row_downgrades_a_corrupt_patch_claim_on_its_own_package()
    {
        // The in-cache variant of the corrupt-record threat CandidateGuard covers
        // out-of-cache: patch X's LocalPackage is corrupt and names product B's
        // cached .msi instead of an .msp of its own. A enumerates first and claims
        // the path removable. B's product row must be able to take it back;
        // first-writer-wins would decide it on enumeration order and recycle a
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

    // ---- Item 2: Uninstallable guard fails safe ----

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

    // ---- Item 6: patch enumeration AccessDenied throws (matches product loop) ----

    [Fact]
    public async Task Patch_enumeration_access_denied_throws()
    {
        var msi = new FakeMsiApi();
        msi.AddProduct("{A}");
        msi.PatchEnumResult["{A}"] = AccessDenied;

        await Assert.ThrowsAsync<LocalisedAccessException>(() => Run(msi));
    }

    // ---- Item 7: an empty GUID accepted as success is not added ----

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

    // ---- Item 8: the index cap ends enumeration loudly, not silently ----

    [Fact]
    public async Task Product_enumeration_that_never_ends_throws_at_the_cap()
    {
        var msi = new FakeMsiApi { NeverEndProducts = true };

        await Assert.ThrowsAsync<LocalisedInvalidOperationException>(() => Run(msi));
    }

    [Fact]
    public async Task Patch_enumeration_that_never_ends_throws_at_the_cap()
    {
        var msi = new FakeMsiApi();
        msi.AddProduct("{A}");
        msi.NeverEndPatchesFor = "{A}";

        await Assert.ThrowsAsync<LocalisedInvalidOperationException>(() => Run(msi));
    }

    // ---- Item 9: scattered per-product failures are tolerated (no throw) ----

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
        Assert.Equal(0, result.UnreadableProductCount);
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
        Assert.Equal(1, result.UnreadableProductCount);
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
        Assert.Equal(1, result.UnreadableProductCount);
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
        Assert.Equal(1, result.UnreadableProductCount);
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
        Assert.Equal(1, result.UnreadableProductCount);
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
        msi.AddPatch("{B}", "{Q}", localPackage: @"C:\Windows\Installer\q.msp", state: "1", uninstallable: "1");
        msi.AddPatch("{B}", "{R}", localPackage: @"C:\Windows\Installer\r.msp", state: "1", uninstallable: "1");
        msi.PatchRowResult[("{B}", 0)] = 1603;
        msi.PatchRowResult[("{B}", 1)] = 1603;

        var result = await Run(msi);

        Assert.Equal(2, result.UnreadableProductCount);
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
            char[]? value, ref uint valueLength) =>
            DoubleCall(_productProps.GetValueOrDefault((productCode, property), ""), value, ref valueLength);

        public uint GetPatchInfo(string patchCode, string productCode, string? userSid, MsiInstallContext context,
            string property, char[]? value, ref uint valueLength) =>
            DoubleCall(_patchProps.GetValueOrDefault((patchCode, productCode, property), ""), value, ref valueLength);

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
