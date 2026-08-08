using InstallerClean.Interop;
using InstallerClean.Models;
using InstallerClean.Services;

namespace InstallerClean.Tests.Services;

/// <summary>
/// The route these exist for: a patch that is Superseded under one product and
/// Applied under another stays non-removable only if the Applied row reaches the
/// merge, and that row arrives through the second product's patch enumeration. An
/// enumeration that returns ERROR_NO_MORE_ITEMS early is indistinguishable from
/// one that finished, so the Applied row is lost and NOTHING SIGNALS IT: the
/// product is not counted unreadable, the registry fallback recovers paths and
/// not verdicts, and its unclaimed-patch signal counts only paths it was first to
/// claim, which this is not.
///
/// So the removable verdict is re-established by asking every enumerated product
/// about the patch directly, and these pin that it is really asked, that each
/// answer lands on the right side, and that a machine with nothing removable pays
/// nothing.
///
/// THE PATHS HERE ARE NEVER ASSERTED ON, deliberately. Registered paths go
/// through Path.GetFullPath, which does not treat a backslash as a separator off
/// Windows, so an assertion naming a path would pass on the CI host and fail
/// anywhere else the logic is exercised. Every assertion below is about a
/// verdict, which is what these tests are about anyway.
/// </summary>
public class InstallerQueryServicePatchTruncationTests
{
    private const uint Success = 0, MoreData = 234, NoMoreItems = 259;
    private const uint UnknownPatch = 1647, BadConfiguration = 1610;
    private const uint AccessDenied = 5, InvalidParameter = 87, UnknownProduct = 1605;

    private const string Superseding = "{AAAAAAAA-0000-0000-0000-00000000000A}";
    private const string StillApplied = "{BBBBBBBB-0000-0000-0000-00000000000B}";
    private const string Patch = "{CCCCCCCC-0000-0000-0000-00000000000C}";
    private const string Shared = @"C:\Windows\Installer\shared.msp";

    private static InstallerQueryService.FallbackRead NoFallback(
        Dictionary<string, RegisteredPackage> claimed, CancellationToken ct) => new(0, 0);

    private static Task<InstallerQueryResult> Run(FakeApi msi) =>
        new InstallerQueryService(msi, NoFallback).GetRegisteredPackagesAsync();

    private static Task<InstallerQueryResult> Run(FakeApi msi, IPackageIdentityReader reader) =>
        new InstallerQueryService(msi, NoFallback, null, reader).GetRegisteredPackagesAsync();

    /// <summary>A patch file declaring the given target products.</summary>
    private static IPackageIdentityReader Reader(string path, params string[] targets) =>
        new FakeReader { [path] = new PackageIdentity(Patch, IsPatch: true, targets) };

    /// <summary>A patch file that yields no identity at all.</summary>
    private static IPackageIdentityReader UnreadableReader(string path) =>
        new FakeReader { [path] = null };

    private sealed class FakeReader : IPackageIdentityReader
    {
        private readonly Dictionary<string, PackageIdentity?> _byPath = new(StringComparer.OrdinalIgnoreCase);

        public PackageIdentity? this[string path] { set => _byPath[path] = value; }

        public PackageIdentity? Read(string filePath, bool isPatch, out string detail)
        {
            detail = string.Empty;
            // Matched on the leaf, because the service normalises the path before
            // it reaches here and that does different things on different hosts.
            foreach (var kv in _byPath)
                if (filePath.EndsWith(System.IO.Path.GetFileName(kv.Key), StringComparison.OrdinalIgnoreCase))
                    return kv.Value;
            return new PackageIdentity(string.Empty, isPatch, Array.Empty<string>());
        }
    }

    /// <summary>
    /// Two products hold one cached patch. The first says superseded and no
    /// longer uninstallable; the second still has it applied and its patch
    /// enumeration comes back empty, which is the whole fault.
    /// </summary>
    private static FakeApi TwoProductsOneSharedPatch(bool truncateSecond = true)
    {
        var msi = new FakeApi();
        msi.AddProduct(Superseding);
        msi.AddProduct(StillApplied);
        msi.HoldPatch(Superseding, Patch, Shared, state: "2", uninstallable: "0");
        msi.HoldPatch(StillApplied, Patch, Shared, state: "1", uninstallable: "1");
        // The second product's enumeration ends at index 0 while the product
        // itself enumerated fine, which is the state nothing else notices.
        if (truncateSecond) msi.EnumerationEndsEarlyFor.Add(StillApplied);
        return msi;
    }

    [Fact]
    public async Task A_patch_another_product_still_needs_is_not_offered_when_its_row_was_lost()
    {
        var result = await Run(TwoProductsOneSharedPatch());

        var row = Assert.Single(result.Packages);
        Assert.False(row.IsRemovable);
        // Not a withholding. A product was asked and answered that it holds the
        // patch and has not shown it removable, which is a live claim on the
        // file, and calling it a withholding would name a cause that did not
        // occur.
        Assert.False(row.RemovableWithheld);
    }

    [Fact]
    public async Task The_same_machine_without_the_truncation_reaches_the_same_verdict()
    {
        // The control that makes the test above mean something: with the second
        // product's enumeration whole, the merge's own downgrade produces the
        // identical answer. So the fix restores a verdict rather than inventing
        // one.
        var result = await Run(TwoProductsOneSharedPatch(truncateSecond: false));

        var row = Assert.Single(result.Packages);
        Assert.False(row.IsRemovable);
        Assert.False(row.RemovableWithheld);
    }

    [Fact]
    public async Task A_patch_no_other_product_holds_is_still_offered()
    {
        // The direction that must NOT move. Every other product answers that it
        // does not hold the patch, which is a positive answer and not a failure,
        // so the superseded verdict stands and the file is still offered.
        var msi = new FakeApi();
        msi.AddProduct(Superseding);
        msi.AddProduct(StillApplied);
        msi.HoldPatch(Superseding, Patch, Shared, state: "2", uninstallable: "0");

        var result = await Run(msi);

        var row = Assert.Single(result.Packages);
        Assert.True(row.IsRemovable);
    }

    [Fact]
    public async Task A_read_that_could_not_answer_keeps_the_patch_and_says_it_was_withheld()
    {
        var msi = TwoProductsOneSharedPatch();
        msi.PatchPropertyResult[(Patch, StillApplied, "State")] = BadConfiguration;

        var result = await Run(msi);

        var row = Assert.Single(result.Packages);
        Assert.False(row.IsRemovable);
        // The other of the two meanings: nothing was established, so the row is
        // kept for want of a verdict rather than on one.
        Assert.True(row.RemovableWithheld);
    }

    [Fact]
    public async Task An_unreadable_uninstallable_value_keeps_the_patch_too()
    {
        // Uninstallable is only asked once State has answered, so this pins the
        // second read's failure separately from the first's.
        var msi = TwoProductsOneSharedPatch();
        msi.PatchPropertyResult[(Patch, StillApplied, "Uninstallable")] = BadConfiguration;

        var result = await Run(msi);

        var row = Assert.Single(result.Packages);
        Assert.False(row.IsRemovable);
        Assert.True(row.RemovableWithheld);
    }

    [Fact]
    public async Task A_machine_with_nothing_removable_is_never_asked_anything()
    {
        // The cost guard. This pass scales with products multiplied by removable
        // candidates, so a machine with no removable candidate has to pay
        // nothing, and that is the overwhelmingly common machine.
        var msi = new FakeApi();
        msi.AddProduct(Superseding);
        msi.AddProduct(StillApplied);
        msi.HoldPatch(Superseding, Patch, Shared, state: "1", uninstallable: "1");
        msi.EnumerationEndsEarlyFor.Add(StillApplied);

        var result = await Run(msi);

        Assert.False(Assert.Single(result.Packages).IsRemovable);
        Assert.Empty(msi.ConfirmationAsks);
    }

    [Fact]
    public async Task A_pairing_the_enumeration_already_read_is_not_asked_again()
    {
        // Both products enumerated the patch, so both claims reached the merge
        // and there is nothing left to establish. Re-asking would get the same
        // answers for the same reason and would cost a read per pairing on every
        // scan.
        var msi = TwoProductsOneSharedPatch(truncateSecond: false);

        await Run(msi);

        Assert.Empty(msi.ConfirmationAsks);
    }

    [Fact]
    public async Task Only_the_products_that_never_named_the_patch_are_asked()
    {
        var msi = TwoProductsOneSharedPatch();

        await Run(msi);

        // One pairing, once: the product whose enumeration came back short. The
        // other product's claim was read by the enumeration itself.
        Assert.Equal(new[] { (Patch, StillApplied) }, msi.ConfirmationAsks.Distinct().ToArray());
    }

    [Fact]
    public async Task Every_patch_code_naming_one_path_is_confirmed_not_just_the_last()
    {
        // A path can be named by more than one patch code: claims are collected
        // per claim because several products claim one file, and a corrupt
        // LocalPackage can aim a patch row at a file that is not that patch's.
        // Confirming one code and clearing the file on its answer would leave the
        // other unasked, which is the direction that costs a file.
        const string other = "{DDDDDDDD-0000-0000-0000-00000000000D}";
        var msi = new FakeApi();
        msi.AddProduct(Superseding);
        msi.AddProduct(StillApplied);
        // Both codes name the same cached file and both look removable to the
        // product that enumerated them.
        msi.HoldPatch(Superseding, Patch, Shared, state: "2", uninstallable: "0");
        msi.HoldPatch(Superseding, other, Shared, state: "2", uninstallable: "0");
        // The second product still has ONE of them applied, and its enumeration
        // never says so. It is deliberately the code that a per-path map would
        // have dropped, so this test fails if only one code per path is asked
        // about rather than passing on the survivor's answer.
        msi.HoldPatch(StillApplied, Patch, Shared, state: "1", uninstallable: "1");
        msi.EnumerationEndsEarlyFor.Add(StillApplied);

        var result = await Run(msi);

        var row = Assert.Single(result.Packages);
        Assert.False(row.IsRemovable);
        Assert.False(row.RemovableWithheld);
    }

    [Fact]
    public async Task The_ordinary_machine_shape_still_offers_its_removable_patch()
    {
        // THE SHAPE THAT MUST NOT REGRESS, taken from the machine all of this was
        // measured on: the enumeration is whole, the registry legitimately runs
        // one key ahead because a failed uninstall left it behind, and a
        // superseded patch nothing else holds is on the disk.
        //
        // Everything added here withholds when it cannot answer, so the failure
        // mode of the whole pass is an empty offer. This is the test that says it
        // is not empty on the machine people actually have.
        var msi = new FakeApi();
        msi.AddProduct(Superseding);
        msi.AddProduct(StillApplied);
        msi.HoldPatch(Superseding, Patch, Shared, state: "2", uninstallable: "0");

        var result = await new InstallerQueryService(msi,
                (_, _) => new InstallerQueryService.FallbackRead(0, msi.WalkedProducts + 1))
            .GetRegisteredPackagesAsync();

        var row = Assert.Single(result.Packages);
        Assert.True(row.IsRemovable);
        Assert.False(row.RemovableWithheld);
        Assert.Equal(0, result.UnaccountedProductCount);
    }

    // ---- Route A: the product the walk never returned ----

    [Fact]
    public async Task A_product_the_walk_never_returned_is_found_and_still_holds_the_patch()
    {
        // THE RESIDUE THIS ROUTE EXISTS FOR. The product enumeration itself came
        // back short, so the product holding the patch is in no list the pass can
        // iterate. Asking the API about no product in particular names it, and
        // the keyed read then settles it.
        var msi = new FakeApi();
        msi.AddProduct(Superseding);
        msi.HoldPatch(Superseding, Patch, Shared, state: "2", uninstallable: "0");
        // Installed, holds the patch as applied, and absent from the walk.
        msi.HiddenFromWalk.Add(StillApplied);
        msi.HoldPatch(StillApplied, Patch, Shared, state: "1", uninstallable: "1");

        var result = await Run(msi);

        var row = Assert.Single(result.Packages);
        Assert.False(row.IsRemovable);
        Assert.False(row.RemovableWithheld);
    }

    [Fact]
    public async Task A_machine_wide_enumeration_that_refuses_withholds_every_removable_verdict()
    {
        // An enumeration that came back empty because it refused, read as "no
        // other product holds it", is the exact fault this pass exists to close.
        // So a refusal is not an answer and everything still removable is kept.
        var msi = TwoProductsOneSharedPatch();
        msi.MachineWidePatchEnumResult = BadConfiguration;

        var result = await Run(msi);

        var row = Assert.Single(result.Packages);
        Assert.False(row.IsRemovable);
        Assert.True(row.RemovableWithheld);
    }

    [Theory]
    [InlineData(AccessDenied)]
    [InlineData(InvalidParameter)]
    [InlineData(UnknownProduct)]
    public async Task Every_documented_failure_of_that_enumeration_withholds(uint code)
    {
        // The page lists these among its returns. None of them is an answer, and
        // each is decided here rather than falling through a default.
        var msi = TwoProductsOneSharedPatch();
        msi.MachineWidePatchEnumResult = code;

        var result = await Run(msi);

        Assert.True(Assert.Single(result.Packages).RemovableWithheld);
    }

    [Fact]
    public async Task A_machine_wide_row_that_names_nothing_withholds()
    {
        // A success that wrote no codes cannot be used and cannot be shown to be
        // harmless, so it is treated as the row that was missed.
        var msi = TwoProductsOneSharedPatch();
        msi.MachineWideEmitsEmptyRow = true;

        var result = await Run(msi);

        Assert.True(Assert.Single(result.Packages).RemovableWithheld);
    }

    [Fact]
    public async Task A_machine_with_nothing_removable_never_runs_the_machine_wide_enumeration()
    {
        // It is the one call in this pass that scales with the whole machine, so
        // the ordinary machine must not pay for it.
        var msi = new FakeApi();
        msi.AddProduct(Superseding);
        msi.HoldPatch(Superseding, Patch, Shared, state: "1", uninstallable: "1");

        await Run(msi);

        Assert.Equal(0, msi.MachineWideRowsServed);
    }

    [Fact]
    public async Task A_product_that_has_the_patch_registered_but_not_applied_still_keeps_it()
    {
        // MSIPATCHSTATE_REGISTERED is 8: registered and not yet applied. It is
        // not a removable state, so a product holding it that way keeps the file,
        // and this pins that the confirmation reads it that way rather than
        // treating anything that is not Applied as spent.
        //
        // It matters more than its rarity suggests. The enumeration's REGISTERED
        // filter carries its own exclusion for other users' per-user-unmanaged
        // patches, and unlike the sibling exclusion on the context itself that one
        // names no installer version, so this is the state in which a holder can
        // be invisible to route A on a current machine.
        var msi = new FakeApi();
        msi.AddProduct(Superseding);
        msi.HoldPatch(Superseding, Patch, Shared, state: "2", uninstallable: "0");
        msi.HiddenFromWalk.Add(StillApplied);
        msi.HoldPatch(StillApplied, Patch, Shared, state: "8", uninstallable: "0");

        var result = await Run(msi);

        var row = Assert.Single(result.Packages);
        Assert.False(row.IsRemovable);
        Assert.False(row.RemovableWithheld);
    }

    // ---- Route B: what the patch file itself declares ----

    [Fact]
    public async Task A_target_the_patch_file_names_is_asked_even_when_no_enumeration_names_it()
    {
        // Route A has a documented blind spot: in the per-user-unmanaged context
        // it enumerates only patches installed with Windows Installer 3.0 for
        // users other than the current one. The file's own Template does not care
        // what any enumeration returned, which is why both routes exist.
        var msi = new FakeApi();
        msi.AddProduct(Superseding);
        msi.HoldPatch(Superseding, Patch, Shared, state: "2", uninstallable: "0");
        // Installed and holding the patch, invisible to BOTH enumerations.
        msi.HiddenFromWalk.Add(StillApplied);
        msi.HoldPatchInvisibleToEnumeration(StillApplied, Patch, state: "1", uninstallable: "1");

        var result = await Run(msi, Reader(Shared, StillApplied));

        var row = Assert.Single(result.Packages);
        Assert.False(row.IsRemovable);
        Assert.False(row.RemovableWithheld);
    }

    [Fact]
    public async Task A_patch_whose_own_declaration_cannot_be_read_is_withheld()
    {
        // The row has to still be removable when route B runs, or the merge has
        // already settled it and the file is never read.
        var msi = new FakeApi();
        msi.AddProduct(Superseding);
        msi.HoldPatch(Superseding, Patch, Shared, state: "2", uninstallable: "0");

        var result = await Run(msi, UnreadableReader(Shared));

        var row = Assert.Single(result.Packages);
        Assert.False(row.IsRemovable);
        Assert.True(row.RemovableWithheld);
    }

    [Fact]
    public async Task A_declared_target_that_is_not_installed_is_a_clean_answer()
    {
        // A product that is not there holds no patches, so it says nothing either
        // way and must not withhold. This is the direction that would delete the
        // feature if it went wrong.
        var msi = new FakeApi();
        msi.AddProduct(Superseding);
        msi.HoldPatch(Superseding, Patch, Shared, state: "2", uninstallable: "0");

        var result = await Run(msi, Reader(Shared, "{EEEEEEEE-0000-0000-0000-00000000000E}"));

        Assert.True(Assert.Single(result.Packages).IsRemovable);
    }

    [Fact]
    public async Task A_declared_target_that_cannot_be_located_withholds()
    {
        var msi = new FakeApi();
        msi.AddProduct(Superseding);
        msi.HoldPatch(Superseding, Patch, Shared, state: "2", uninstallable: "0");
        msi.ProductResolveResult[StillApplied] = BadConfiguration;

        var result = await Run(msi, Reader(Shared, StillApplied));

        Assert.True(Assert.Single(result.Packages).RemovableWithheld);
    }

    /// <summary>
    /// A machine, declared rather than scripted call by call. It answers the four
    /// entry points off the products and patches it has been told about, and can
    /// be made to end one product's patch enumeration early without that product
    /// forgetting the patch it holds, which is exactly the state under test.
    /// </summary>
    private sealed class FakeApi : IMsiApi
    {
        private readonly List<string> _products = new();
        private readonly Dictionary<string, List<string>> _patchesOf = new();
        private readonly Dictionary<(string Patch, string Product, string Property), string> _patchProps = new();

        /// <summary>Products whose patch enumeration returns nothing at all.</summary>
        public HashSet<string> EnumerationEndsEarlyFor { get; } = new();

        /// <summary>
        /// Products that ARE installed and that the product walk never returns,
        /// which is the truncation this whole pass exists for. They answer a
        /// filtered enumeration and every keyed read exactly as a real product
        /// would; they are simply absent from the walk.
        /// </summary>
        public HashSet<string> HiddenFromWalk { get; } = new();

        /// <summary>Forces a return out of the machine-wide patch enumeration (route A).</summary>
        public uint? MachineWidePatchEnumResult { get; set; }

        /// <summary>Makes the machine-wide enumeration report a row naming nothing.</summary>
        public bool MachineWideEmitsEmptyRow { get; set; }

        /// <summary>Forces a return out of the filtered product enumeration for one code.</summary>
        public Dictionary<string, uint> ProductResolveResult { get; } = new();

        /// <summary>Machine-wide patch rows actually served, for the cost assertions.</summary>
        public int MachineWideRowsServed { get; private set; }

        public Dictionary<(string Patch, string Product, string Property), uint> PatchPropertyResult { get; } = new();

        /// <summary>
        /// Pairings asked about that the product's OWN enumeration never
        /// produced, which is exactly what the confirmation pass costs and
        /// nothing else. Keyed on that rather than on when the call arrived,
        /// because the scan reads a product's own patch rows after that product's
        /// enumeration has already ended, so anything timed would count those too.
        /// </summary>
        public List<(string Patch, string Product)> ConfirmationAsks { get; } = new();

        public void AddProduct(string code) => _products.Add(code);

        /// <summary>How many products the walk returns, for the headcount tests.</summary>
        public int WalkedProducts => _products.Count;

        /// <summary>
        /// The pairing exists and every keyed read answers for it, while no
        /// enumeration names it. That is route A's documented blind spot in the
        /// flesh, and the state only the patch file's own Template can reach.
        /// </summary>
        public void HoldPatchInvisibleToEnumeration(string productCode, string patchCode,
            string state, string uninstallable)
        {
            _patchProps[(patchCode, productCode, "State")] = state;
            _patchProps[(patchCode, productCode, "Uninstallable")] = uninstallable;
        }

        public void HoldPatch(string productCode, string patchCode, string localPackage,
            string state, string uninstallable)
        {
            (_patchesOf.TryGetValue(productCode, out var list)
                ? list : _patchesOf[productCode] = new()).Add(patchCode);
            _patchProps[(patchCode, productCode, "LocalPackage")] = localPackage;
            _patchProps[(patchCode, productCode, "State")] = state;
            _patchProps[(patchCode, productCode, "Uninstallable")] = uninstallable;
        }

        private static void Write(char[]? buffer, string value)
        {
            if (buffer is null) return;
            for (var i = 0; i < value.Length && i < buffer.Length - 1; i++) buffer[i] = value[i];
        }

        public uint EnumProducts(string? productCode, string? userSid, MsiInstallContext context,
            uint index, char[]? installedProductCode, out MsiInstallContext installedContext,
            char[]? sid, ref uint sidLength)
        {
            installedContext = MsiInstallContext.Machine;

            // Filtered to one code: the question "where is this product
            // installed", which answers for a product the walk never returned.
            if (productCode is not null)
            {
                if (ProductResolveResult.TryGetValue(productCode, out var forced)) return forced;
                if (index > 0) return NoMoreItems;
                var exists = _products.Contains(productCode) || HiddenFromWalk.Contains(productCode);
                if (!exists) return NoMoreItems;
                Write(installedProductCode, productCode);
                sidLength = 0;
                return Success;
            }

            // The walk. It does NOT return the hidden products, which is the
            // whole point of them.
            if (index >= _products.Count) return NoMoreItems;
            Write(installedProductCode, _products[(int)index]);
            return Success;
        }

        public uint EnumPatches(string? productCode, string? userSid, MsiInstallContext context,
            MsiPatchFilter filter, uint index, char[]? patchCode, char[]? targetProductCode,
            out MsiInstallContext targetProductContext, char[]? targetUserSid, ref uint targetUserSidLength)
        {
            targetProductContext = MsiInstallContext.Machine;

            // Route A: no product in particular. Every pairing the machine holds,
            // including those of products the walk never returned.
            if (productCode is null)
            {
                if (MachineWidePatchEnumResult is { } forced) return forced;
                var rows = _patchesOf
                    .SelectMany(kv => kv.Value.Select(pc => (Patch: pc, Product: kv.Key)))
                    .OrderBy(r => r.Product, StringComparer.Ordinal)
                    .ThenBy(r => r.Patch, StringComparer.Ordinal)
                    .ToList();
                if (index >= rows.Count) return NoMoreItems;
                MachineWideRowsServed++;
                if (MachineWideEmitsEmptyRow) return Success;   // buffers left blank
                Write(patchCode, rows[(int)index].Patch);
                Write(targetProductCode, rows[(int)index].Product);
                targetUserSidLength = 0;
                return Success;
            }

            // A false clean end: the list stops before it should and says so the
            // same way a finished list does.
            if (EnumerationEndsEarlyFor.Contains(productCode))
                return NoMoreItems;
            var list = _patchesOf.TryGetValue(productCode, out var l) ? l : null;
            if (list is null || index >= list.Count) return NoMoreItems;
            Write(patchCode, list[(int)index]);
            return Success;
        }

        public uint GetProductInfo(string productCode, string? userSid, MsiInstallContext context,
            string property, char[]? value, ref uint valueLength)
        {
            // Products carry no cached package of their own here, so nothing but
            // the patch rows reaches the merge and every assertion is about one
            // row. A readable empty value is the benign "no such property" state.
            valueLength = 0;
            return Success;
        }

        public uint GetPatchInfo(string patchCode, string productCode, string? userSid,
            MsiInstallContext context, string property, char[]? value, ref uint valueLength)
        {
            // Recorded only where this product's own enumeration never named the
            // patch, which is the one thing the product loop cannot have asked.
            var enumeratedIt = !EnumerationEndsEarlyFor.Contains(productCode)
                && _patchesOf.TryGetValue(productCode, out var held) && held.Contains(patchCode);
            if (!enumeratedIt) ConfirmationAsks.Add((patchCode, productCode));

            if (PatchPropertyResult.TryGetValue((patchCode, productCode, property), out var forced))
            {
                valueLength = 0;
                return forced;
            }

            if (!_patchProps.TryGetValue((patchCode, productCode, property), out var val))
            {
                // This product does not hold this patch: a positive answer, and
                // the one the confirmation pass reads as "says nothing either
                // way" rather than as a failure.
                valueLength = 0;
                return UnknownPatch;
            }

            if (value is null) { valueLength = (uint)val.Length; return MoreData; }
            var n = Math.Min(val.Length, value.Length);
            for (var i = 0; i < n; i++) value[i] = val[i];
            valueLength = (uint)val.Length;
            return Success;
        }
    }
}
