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
/// with).
/// </summary>
public class InstallerQueryServiceUnitTests
{
    private const uint Success = 0, AccessDenied = 5, MoreData = 234, NoMoreItems = 259;

    private static async Task<IReadOnlyList<RegisteredPackage>> Run(FakeMsiApi msi) =>
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

        var row = Assert.Single(result, r => r.LocalPackagePath == shared);
        Assert.False(row.IsRemovable);
        Assert.Equal(1, row.PatchState); // carries the applied product's state
    }

    [Fact]
    public async Task Superseded_patch_claimed_by_a_single_product_stays_removable()
    {
        const string dead = @"C:\Windows\Installer\dead.msp";
        var msi = new FakeMsiApi();
        msi.AddProduct("{A}");
        msi.AddPatch("{A}", "{P}", localPackage: dead, state: "2", uninstallable: "0");

        var result = await Run(msi);

        Assert.True(Assert.Single(result, r => r.LocalPackagePath == dead).IsRemovable);
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

        Assert.False(Assert.Single(result, r => r.LocalPackagePath == p).IsRemovable);
    }

    [Fact]
    public async Task Positively_read_zero_Uninstallable_allows_removal_of_a_superseded_patch()
    {
        const string p = @"C:\Windows\Installer\rem0.msp";
        var msi = new FakeMsiApi();
        msi.AddProduct("{A}");
        msi.AddPatch("{A}", "{P}", localPackage: p, state: "2", uninstallable: "0");

        var result = await Run(msi);

        Assert.True(Assert.Single(result, r => r.LocalPackagePath == p).IsRemovable);
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

        Assert.DoesNotContain(result, r => r.LocalPackagePath.Length == 0);
        Assert.Contains(result, r => r.LocalPackagePath == p);
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

        Assert.Contains(result, r => r.LocalPackagePath == ppath);
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

        Assert.Equal(20, result.Count(r => r.LocalPackagePath.StartsWith(@"C:\Windows\Installer\ok")));
    }

    [Fact]
    public async Task Twenty_consecutive_product_failures_throw()
    {
        var msi = new FakeMsiApi();
        for (int i = 0; i < 20; i++)
            msi.AddProduct($"{{bad{i}}}", result: 1603);

        await Assert.ThrowsAsync<LocalisedInvalidOperationException>(() => Run(msi));
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
