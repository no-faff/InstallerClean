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

    private const string Superseding = "{AAAAAAAA-0000-0000-0000-00000000000A}";
    private const string StillApplied = "{BBBBBBBB-0000-0000-0000-00000000000B}";
    private const string Patch = "{CCCCCCCC-0000-0000-0000-00000000000C}";
    private const string Shared = @"C:\Windows\Installer\shared.msp";

    private static InstallerQueryService.FallbackRead NoFallback(
        Dictionary<string, RegisteredPackage> claimed, CancellationToken ct) => new(0, 0);

    private static Task<InstallerQueryResult> Run(FakeApi msi) =>
        new InstallerQueryService(msi, NoFallback).GetRegisteredPackagesAsync();

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
            if (index >= _products.Count) return NoMoreItems;
            Write(installedProductCode, _products[(int)index]);
            return Success;
        }

        public uint EnumPatches(string? productCode, string? userSid, MsiInstallContext context,
            MsiPatchFilter filter, uint index, char[]? patchCode, char[]? targetProductCode,
            out MsiInstallContext targetProductContext, char[]? targetUserSid, ref uint targetUserSidLength)
        {
            targetProductContext = MsiInstallContext.Machine;
            // A false clean end: the list stops before it should and says so the
            // same way a finished list does.
            if (productCode is not null && EnumerationEndsEarlyFor.Contains(productCode))
                return NoMoreItems;
            var list = productCode is not null && _patchesOf.TryGetValue(productCode, out var l) ? l : null;
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
