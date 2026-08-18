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

    /// <summary>The patch that supersedes <see cref="Patch"/> and can still come off.</summary>
    private const string Superseder = "{DDDDDDDD-0000-0000-0000-00000000000D}";

    /// <summary>A product code no machine here has, for the resolve's clean negative.</summary>
    private const string NotInstalled = "{EEEEEEEE-0000-0000-0000-00000000000E}";

    private const string Shared = @"C:\Windows\Installer\shared.msp";

    private static InstallerQueryService.FallbackRead NoFallback(
        Dictionary<string, RegisteredPackage> claimed, CancellationToken ct) => new(0, 0);

    private static Task<InstallerQueryResult> Run(FakeApi msi) =>
        new InstallerQueryService(msi, NoFallback).GetRegisteredPackagesAsync();

    /// <summary>
    /// Drives the confirmation pass over the state the enumeration used to hand
    /// it, and returns the claimed set for the assertions.
    ///
    /// WHY THESE TESTS DRIVE IT DIRECTLY RATHER THAN THROUGH A SCAN, AND IT IS NO
    /// LONGER BECAUSE NOTHING REACHES IT. This paragraph said that from 3.0.0 no
    /// enumeration grants a removable verdict to any patch, so nothing reached this
    /// pass through a real scan. That was true while the superseded class was out;
    /// restoring the offer restored the route, the enumeration grants the verdict
    /// again through IsRemovablePatch, and a full scan does now reach this pass. The
    /// reason to drive it directly is the ordinary one instead: an assertion about
    /// this pass should turn on this pass, and a scan puts the whole enumeration,
    /// the merge and the per-product condition in front of it.
    ///
    /// There is deliberately NO production switch that re-grants the verdict
    /// selectively: a flag in a shipped binary that turns a class of file back on is
    /// the thing the release exists to prevent.
    ///
    /// THE STARTING VERDICT COMES FROM THE PRODUCTION RULE, not from a copy of it.
    /// <see cref="InstallerQueryService.IsRemovablePatch"/> decides it and
    /// <see cref="InstallerQueryService.MergeClaim"/> merges it, so the only thing
    /// assembled here is the shape of the enumeration's output: which products the
    /// walk returned and which pairings it read. That is a small reimplementation
    /// and it can drift; what it cannot do is change what the pass under test
    /// concludes from what it is given.
    /// </summary>
    private static Dictionary<string, RegisteredPackage> Confirm(
        FakeApi msi, IPackageIdentityReader? reader = null, params string[] recovered)
    {
        var claimed = new Dictionary<string, RegisteredPackage>(StringComparer.OrdinalIgnoreCase);
        var claims = new List<PatchClaim>();

        foreach (var row in msi.EnumeratedPairings())
        {
            claims.Add(new PatchClaim(row.Path, row.Patch, row.Product, row.UserSid, (int)row.Context));
            int.TryParse(row.State, out var patchState);
            InstallerQueryService.MergeClaim(
                claimed,
                new RegisteredPackage(row.Path, "Product", row.Product, patchState,
                    InstallerQueryService.IsRemovablePatch(row.State, row.Uninstallable)),
                InstallerQueryService.ClaimSource.InstallerApi);
        }

        // The per-product patch-set reading, and it comes from a DIFFERENT source
        // from the claims above, which is the correction rather than a detail. These
        // tests' subject is route A and the confirmation pass, not the
        // superseded-patch condition: without a clean reading every product would be
        // unestablished, every path would be withheld for that reason alone, and
        // every assertion here would pass or fail for something it is not about. The
        // condition has its own tests.
        //
        // BUILT FROM EVERY REGISTRATION RATHER THAN FROM THE ENUMERATED ONES. It was
        // built from the enumerated rows, which meant a product the walk LOST had no
        // patch set, read as unestablished, and withheld the path before the pass
        // could go and find it. That is the exact case these tests exist for, so they
        // were withholding on the fixture's shape instead of exercising the pass. In
        // production the two sources really are different: the claims come from the
        // API walk, and the patch sets are read by walking UserData's own product
        // subkeys, which see a product the walk never returned.
        var patchSets = new Dictionary<string, ProductPatchSet>(
            StringComparer.OrdinalIgnoreCase);
        foreach (var (product, uninstallable) in msi.AllRegistrations())
        {
            var verdict = uninstallable == "0"
                ? ProductPatchSet.AllNonRemovable
                : uninstallable.Length == 0
                    ? ProductPatchSet.Unestablished
                    : ProductPatchSet.RemovablePatchPresent;
            patchSets[product] = patchSets.TryGetValue(product, out var seen)
                ? InstallerQueryService.Worse(seen, verdict)
                : verdict;
        }

        new InstallerQueryService(msi, NoFallback, null, reader).ConfirmRemovableAgainstEveryProduct(
            claimed,
            claims,
            msi.WalkedProductInstances.ToList(),
            recovered.Select(p => (ProductCode: p, Sid: (string?)null, Context: MsiInstallContext.Machine)).ToList(),
            patchSets,
            patchSets,
            CancellationToken.None);

        return claimed;
    }

    /// <summary>The one row every test here is about.</summary>
    private static RegisteredPackage TheSharedPatch(Dictionary<string, RegisteredPackage> claimed) =>
        Assert.Contains(Shared, claimed);

    private static Task<InstallerQueryResult> Run(FakeApi msi, IPackageIdentityReader reader) =>
        new InstallerQueryService(msi, NoFallback, null, reader).GetRegisteredPackagesAsync();

    /// <summary>
    /// A fallback that read the registry cleanly and found the given product
    /// codes. The key count is the number of codes, which is what a real read of
    /// those keys would report.
    /// </summary>
    private static InstallerQueryService.FallbackReader Registry(params string[] productCodes) =>
        (_, _) => new InstallerQueryService.FallbackRead(
            Failures: 0, ProductKeys: productCodes.Length, RegistryProductCodes: productCodes);

    /// <summary>
    /// The same read, plus the per-product patch set each of those products carries,
    /// every one of them positively established as holding nothing that can be
    /// uninstalled.
    ///
    /// WITHOUT IT NO TEST DRIVING THE WHOLE PIPELINE CAN PRODUCE A REMOVABLE ROW, and
    /// that is a trap rather than an inconvenience. <see cref="Registry"/> leaves the
    /// patch sets null, and a null map answers "unestablished" for every product, so
    /// the per-product condition takes the verdict away and marks the row withheld
    /// before anything downstream of it runs. A test asserting that something FURTHER
    /// DOWN withheld the row then passes whether or not that thing exists at all. One
    /// test in this file was doing exactly that, and its own commit named the next CI
    /// run as what would settle the question; CI cannot settle it, because the
    /// assertion is green either way.
    ///
    /// A CLEAN SET FOR EVERY PRODUCT RATHER THAN FOR THE SHARERS ALONE, deliberately.
    /// It leaves exactly one thing in the run able to withhold, which is whatever the
    /// test is actually about, so a green result names its own cause.
    /// </summary>
    private static InstallerQueryService.FallbackReader RegistryWithCleanPatchSets(
        params string[] productCodes) =>
        (_, _) => new InstallerQueryService.FallbackRead(
            Failures: 0,
            ProductKeys: productCodes.Length,
            RegistryProductCodes: productCodes,
            ProductPatchSets: productCodes.ToDictionary(
                c => c, _ => ProductPatchSet.AllNonRemovable, StringComparer.OrdinalIgnoreCase));

    private static Task<InstallerQueryResult> Run(
        FakeApi msi, InstallerQueryService.FallbackReader fallback) =>
        new InstallerQueryService(msi, fallback).GetRegisteredPackagesAsync();

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
    /// <summary>
    /// Two products sharing one patch, where the REGISTRY CANNOT SETTLE THE PATH and
    /// the confirmation pass has to go and ask. The second product holds the patch
    /// applied and declares it not uninstallable, so its registered patch set reads
    /// all-non-removable and the per-product condition passes the path through; the
    /// pairing itself is still one the pass will find is not removable, because the
    /// state is applied rather than superseded.
    ///
    /// IT EXISTS BECAUSE <see cref="TwoProductsOneSharedPatch"/> CANNOT REACH THE PASS
    /// AT ALL, and that is correct behaviour rather than a fault in it. There the
    /// sibling declares the patch uninstallable, so the registry reports a removable
    /// patch present, and the condition settles the path on that evidence before the
    /// pairing reads happen. The condition is deliberately in front of the pass for
    /// exactly that saving. A test of the PASS therefore needs a machine the condition
    /// does not answer for, and this is the ordinary shape of one: most second
    /// products holding a patch cannot uninstall it.
    ///
    /// The shared fixture is left alone rather than changed to this, because several
    /// tests below are about the sibling still needing the patch, and re-pointing them
    /// silently would be the same class of fault this fixture exists to avoid.
    /// </summary>
    private static FakeApi TwoProductsTheRegistryCannotSettle()
    {
        var msi = new FakeApi();
        msi.AddProduct(Superseding);
        msi.AddProduct(StillApplied);
        msi.HoldPatch(Superseding, Patch, Shared, state: "2", uninstallable: "0");
        // Applied and NOT uninstallable: all-non-removable to the registry, and still
        // not a removable pairing to the pass, which needs a superseded state.
        msi.HoldPatch(StillApplied, Patch, Shared, state: "1", uninstallable: "0");
        msi.EnumerationEndsEarlyFor.Add(StillApplied);
        return msi;
    }

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
    public void A_patch_another_product_still_needs_is_not_offered_when_its_row_was_lost()
    {
        var row = TheSharedPatch(Confirm(TwoProductsOneSharedPatch()));
        Assert.False(row.IsRemovable);
        // Not a withholding. A product was asked and answered that it holds the
        // patch and has not shown it removable, which is a live claim on the
        // file, and calling it a withholding would name a cause that did not
        // occur.
        Assert.False(row.RemovableWithheld);
    }

    [Fact]
    public void The_same_machine_without_the_truncation_reaches_the_same_verdict()
    {
        // The control that makes the test above mean something: with the second
        // product's enumeration whole, the merge's own downgrade produces the
        // identical answer. So the fix restores a verdict rather than inventing
        // one.
        var row = TheSharedPatch(Confirm(TwoProductsOneSharedPatch(truncateSecond: false)));
        Assert.False(row.IsRemovable);
        Assert.False(row.RemovableWithheld);
    }

    [Fact]
    public void A_patch_no_other_product_holds_is_still_offered()
    {
        // The direction that must NOT move. Every other product answers that it
        // does not hold the patch, which is a positive answer and not a failure,
        // so the superseded verdict stands and the file is still offered.
        var msi = new FakeApi();
        msi.AddProduct(Superseding);
        msi.AddProduct(StillApplied);
        msi.HoldPatch(Superseding, Patch, Shared, state: "2", uninstallable: "0");

        var row = TheSharedPatch(Confirm(msi));
        Assert.True(row.IsRemovable);
    }

    [Fact]
    public void A_read_that_could_not_answer_keeps_the_patch_and_says_it_was_withheld()
    {
        var msi = TwoProductsTheRegistryCannotSettle();
        msi.PatchPropertyResult[(Patch, StillApplied, "State")] = BadConfiguration;

        var row = TheSharedPatch(Confirm(msi));
        Assert.False(row.IsRemovable);
        // The other of the two meanings: nothing was established, so the row is
        // kept for want of a verdict rather than on one.
        Assert.True(row.RemovableWithheld);
    }

    [Fact]
    public void An_unreadable_uninstallable_value_keeps_the_patch_too()
    {
        // Uninstallable is only asked once State has answered, so this pins the
        // second read's failure separately from the first's.
        var msi = TwoProductsTheRegistryCannotSettle();
        msi.PatchPropertyResult[(Patch, StillApplied, "Uninstallable")] = BadConfiguration;

        var row = TheSharedPatch(Confirm(msi));
        Assert.False(row.IsRemovable);
        Assert.True(row.RemovableWithheld);
    }

    [Fact]
    public void A_machine_with_nothing_removable_is_never_asked_anything()
    {
        // The cost guard. This pass scales with products multiplied by removable
        // candidates, so a machine with no removable candidate has to pay
        // nothing, and that is the overwhelmingly common machine.
        var msi = new FakeApi();
        msi.AddProduct(Superseding);
        msi.AddProduct(StillApplied);
        msi.HoldPatch(Superseding, Patch, Shared, state: "1", uninstallable: "1");
        msi.EnumerationEndsEarlyFor.Add(StillApplied);

        Assert.False(TheSharedPatch(Confirm(msi)).IsRemovable);
        Assert.Empty(msi.ConfirmationAsks);
    }

    [Fact]
    public void A_pairing_the_enumeration_already_read_is_not_asked_again()
    {
        // Both products enumerated the patch, so both claims reached the merge
        // and there is nothing left to establish. Re-asking would get the same
        // answers for the same reason and would cost a read per pairing on every
        // scan.
        var msi = TwoProductsOneSharedPatch(truncateSecond: false);

        Confirm(msi);

        Assert.Empty(msi.ConfirmationAsks);
    }

    [Fact]
    public void Only_the_products_that_never_named_the_patch_are_asked()
    {
        var msi = TwoProductsTheRegistryCannotSettle();

        Confirm(msi);

        // One pairing, once: the product whose enumeration came back short. The
        // other product's claim was read by the enumeration itself.
        Assert.Equal(new[] { (Patch, StillApplied) }, msi.ConfirmationAsks.Distinct().ToArray());
    }

    [Fact]
    public void Every_patch_code_naming_one_path_is_confirmed_not_just_the_last()
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

        var row = TheSharedPatch(Confirm(msi));
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
        //
        // AND IT SAID NOTHING OF THE KIND UNTIL THE PATCH SETS WERE SUPPLIED HERE.
        // The fallback was built with a key count alone, which leaves the per-product
        // patch sets null, and a null map answers "unestablished" for every product
        // before the API's own reading is weighed. So the superseded row was withheld
        // by the condition that asks whether anything on any product sharing the patch
        // could be uninstalled, and the assertion below failed for a reason that has
        // nothing to do with the machine shape this test is named for. An ordinary
        // machine's registry answers that question, which is what these three lines
        // now say it does.
        //
        // THE SECOND PRODUCT'S VERDICT IS SUPPLIED AND IS DELIBERATELY THE WORSE ONE.
        // It holds no patches, so a real read of its Patches key establishes nothing,
        // and it claims none of this path, so nothing may consult it. Naming it here
        // rather than leaving it out is what holds the pass to reading the verdicts of
        // the products that SHARE the patch and not of every product on the machine.
        //
        // The extra key decides nothing any more and is left because the machine
        // really has one: a shortfall against the registry's total stopped being an
        // input when the question moved from arithmetic to asking Windows by name.
        var msi = new FakeApi();
        msi.AddProduct(Superseding);
        msi.AddProduct(StillApplied);
        msi.HoldPatch(Superseding, Patch, Shared, state: "2", uninstallable: "0");

        var result = await new InstallerQueryService(msi,
                (_, _) => new InstallerQueryService.FallbackRead(0, msi.WalkedProducts + 1,
                    ProductPatchSets: new Dictionary<string, ProductPatchSet>(StringComparer.OrdinalIgnoreCase)
                    {
                        [Superseding] = ProductPatchSet.AllNonRemovable,
                        [StillApplied] = ProductPatchSet.Unestablished,
                    }))
            .GetRegisteredPackagesAsync();

        var row = Assert.Single(result.Packages);
        Assert.True(row.IsRemovable);
        Assert.False(row.RemovableWithheld);
        Assert.Equal(0, result.UnaccountedProductCount);
    }

    // ---- Route A: the product the walk never returned ----

    [Fact]
    public void A_product_the_walk_never_returned_is_found_and_still_holds_the_patch()
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

        var row = TheSharedPatch(Confirm(msi));
        Assert.False(row.IsRemovable);
        Assert.False(row.RemovableWithheld);
    }

    [Fact]
    public void A_machine_wide_enumeration_that_refuses_withholds_every_removable_verdict()
    {
        // An enumeration that came back empty because it refused, read as "no
        // other product holds it", is the exact fault this pass exists to close.
        // So a refusal is not an answer and everything still removable is kept.
        var msi = TwoProductsOneSharedPatch();
        msi.MachineWidePatchEnumResult = BadConfiguration;

        var row = TheSharedPatch(Confirm(msi));
        Assert.False(row.IsRemovable);
        Assert.True(row.RemovableWithheld);
    }

    [Theory]
    [InlineData(AccessDenied)]
    [InlineData(InvalidParameter)]
    [InlineData(UnknownProduct)]
    public void Every_documented_failure_of_that_enumeration_withholds(uint code)
    {
        // The page lists these among its returns. None of them is an answer, and
        // each is decided here rather than falling through a default.
        var msi = TwoProductsOneSharedPatch();
        msi.MachineWidePatchEnumResult = code;

        Assert.True(TheSharedPatch(Confirm(msi)).RemovableWithheld);
    }

    [Fact]
    public void A_machine_wide_row_that_names_nothing_withholds()
    {
        // A success that wrote no codes cannot be used and cannot be shown to be
        // harmless, so it is treated as the row that was missed.
        var msi = TwoProductsOneSharedPatch();
        msi.MachineWideEmitsEmptyRow = true;

        Assert.True(TheSharedPatch(Confirm(msi)).RemovableWithheld);
    }

    [Fact]
    public void A_machine_with_nothing_removable_never_runs_the_machine_wide_enumeration()
    {
        // It is the one call in this pass that scales with the whole machine, so
        // the ordinary machine must not pay for it.
        var msi = new FakeApi();
        msi.AddProduct(Superseding);
        msi.HoldPatch(Superseding, Patch, Shared, state: "1", uninstallable: "1");

        Confirm(msi);

        Assert.Equal(0, msi.MachineWideRowsServed);
    }

    [Fact]
    public void A_product_that_has_the_patch_registered_but_not_applied_still_keeps_it()
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

        var row = TheSharedPatch(Confirm(msi));
        Assert.False(row.IsRemovable);
        Assert.False(row.RemovableWithheld);
    }

    // ---- Route B: what the patch file itself declares ----

    [Fact]
    public void A_target_the_patch_file_names_is_asked_even_when_no_enumeration_names_it()
    {
        // Route A has a documented blind spot: in the per-user-unmanaged context
        // it enumerates only patches installed with Windows Installer 3.0 for
        // users other than the current one. The file's own Template does not care
        // what any enumeration returned, which is why both routes exist.
        //
        // WHAT THIS FIXTURE MAKES TRUE, AND IT IS CHOSEN RATHER THAN INHERITED. The
        // hidden product holds the patch APPLIED and not uninstallable, so its
        // registered patch set reads all-non-removable and the per-product condition
        // passes the path through to this pass, while the PAIRING is still one this
        // pass will find is not removable. The same reasoning, and the same shape, as
        // TwoProductsTheRegistryCannotSettle.
        //
        // IT USED TO DECLARE THE PATCH UNINSTALLABLE, AND THAT STOPPED TESTING THIS.
        // Once the patch file's declared targets were unioned into the per-product
        // condition as well, a hidden product carrying a removable patch was settled
        // by that condition BEFORE this pass ran, so the pairing below was never asked
        // and both assertions passed on the earlier mechanism. Nothing about the
        // assertions would have shown it, which is why the ask is now asserted too.
        var msi = new FakeApi();
        msi.AddProduct(Superseding);
        msi.HoldPatch(Superseding, Patch, Shared, state: "2", uninstallable: "0");
        // Installed and holding the patch, invisible to BOTH enumerations.
        msi.HiddenFromWalk.Add(StillApplied);
        msi.HoldPatchInvisibleToEnumeration(StillApplied, Patch, state: "1", uninstallable: "0");

        var row = TheSharedPatch(Confirm(msi, Reader(Shared, StillApplied)));
        Assert.False(row.IsRemovable);
        Assert.False(row.RemovableWithheld);
        // THE SUBJECT, ASSERTED. Without this the test passes whenever anything at all
        // takes the verdict away, which is how it survived the change above.
        Assert.Contains((Patch, StillApplied), msi.ConfirmationAsks);
    }

    /// <summary>
    /// The product the patch file names, that nothing else on the machine can reach,
    /// and that holds a REMOVABLE patch of its own.
    ///
    /// THIS IS THE GAP THE DECLARED TARGETS CLOSE, and it is a different question from
    /// the test above. That one asks whether the pairing is put to a product no
    /// enumeration named; this one asks whether that product's whole PATCH SET is
    /// judged. The pairing answers "superseded and not uninstallable", which is
    /// truthful and is the wrong question: what can reach for the cached file is not
    /// this patch coming off, it is the SUPERSEDING patch coming off and rolling the
    /// product back. So the pairing pass cannot withhold here and must not be relied
    /// on to.
    ///
    /// WHAT THE FIXTURE MAKES TRUE: the hidden product holds the shared patch
    /// superseded and not uninstallable, so every pairing read about it is a clean
    /// answer, AND holds a second patch that IS uninstallable, so its registered patch
    /// set reads removable-patch-present. The claims cannot see it, because the walk
    /// never returned it. Route A cannot see it, because it holds both patches
    /// invisibly to enumeration. The patch file's Template is the only thing on the
    /// machine that names it.
    /// </summary>
    [Fact]
    public void A_product_only_the_patch_file_names_has_its_patch_set_judged()
    {
        var msi = new FakeApi();
        msi.AddProduct(Superseding);
        msi.HoldPatch(Superseding, Patch, Shared, state: "2", uninstallable: "0");
        msi.HiddenFromWalk.Add(StillApplied);
        msi.HoldPatchInvisibleToEnumeration(StillApplied, Patch, state: "2", uninstallable: "0");
        msi.HoldPatchInvisibleToEnumeration(StillApplied, Superseder, state: "1", uninstallable: "1");

        var row = TheSharedPatch(Confirm(msi, Reader(Shared, StillApplied)));

        Assert.False(row.IsRemovable);
        // A FINDING AND NOT AN INABILITY. The app established that something on that
        // product can be uninstalled; it did not fail to establish anything. The two
        // reach different sentences and a test that accepted either would pass on the
        // wrong one.
        Assert.False(row.RemovableWithheld);
    }

    /// <summary>
    /// MUST-MISS CONTROL FOR THE TEST ABOVE. The identical machine, with the patch
    /// file declaring nothing, and the offer stands.
    ///
    /// It is what makes the test above mean anything: without it, a run that withheld
    /// every superseded patch for any reason at all would pass it. The two differ in
    /// one thing only, which is whether the file names the product.
    /// </summary>
    [Fact]
    public void The_same_machine_offers_the_patch_when_the_file_names_nobody()
    {
        var msi = new FakeApi();
        msi.AddProduct(Superseding);
        msi.HoldPatch(Superseding, Patch, Shared, state: "2", uninstallable: "0");
        msi.HiddenFromWalk.Add(StillApplied);
        msi.HoldPatchInvisibleToEnumeration(StillApplied, Patch, state: "2", uninstallable: "0");
        msi.HoldPatchInvisibleToEnumeration(StillApplied, Superseder, state: "1", uninstallable: "1");

        Assert.True(TheSharedPatch(Confirm(msi)).IsRemovable);
    }

    /// <summary>
    /// A declared target that is installed and holds no patches at all costs the offer
    /// nothing, which is what makes reading the declared targets affordable.
    ///
    /// A patch names in its Template every product it may be applied to, and on an
    /// ordinary machine most of them either are not installed or hold nothing that can
    /// be uninstalled. If a product like that answered "could not establish", the
    /// condition would withhold, and it would withhold on nearly every superseded
    /// patch on nearly every machine. It answers all-non-removable instead, because a
    /// product with no registered patch positively holds no removable one.
    ///
    /// WHAT THIS TEST DOES AND DOES NOT ESTABLISH, because the difference matters.
    /// This is a MUST-MISS CONTROL and it passes with or without the change that
    /// unions declared targets in; its job is to fail if that union ever starts
    /// withholding on a clean product. It does NOT establish that a real registry read
    /// of a patch-less product returns all-non-removable, because the verdict is
    /// supplied here rather than read. The real read is pinned on Windows by
    /// ProductPatchSetTests.A_product_with_no_Patches_key_is_clean_and_its_key_is_not_counted,
    /// and that test is the one that fails without the change.
    ///
    /// It goes through a whole scan rather than through Confirm, deliberately: Confirm
    /// builds its patch sets from the fixture's patch REGISTRATIONS, so a product
    /// holding none is absent from that map altogether and would read as unestablished
    /// for a reason no machine has. The registry walk this stands in for visits every
    /// product key and writes a verdict for each.
    /// </summary>
    [Fact]
    public async Task A_declared_target_holding_no_patches_leaves_the_offer_standing()
    {
        var msi = new FakeApi();
        msi.AddProduct(Superseding);
        msi.HoldPatch(Superseding, Patch, Shared, state: "2", uninstallable: "0");
        // Installed, named by the patch file, and holding nothing whatever.
        msi.HiddenFromWalk.Add(StillApplied);

        var result = await new InstallerQueryService(
                msi,
                RegistryWithCleanPatchSets(Superseding, StillApplied),
                null,
                Reader(Shared, StillApplied))
            .GetRegisteredPackagesAsync();

        var row = Assert.Single(result.Packages);
        Assert.True(row.IsRemovable);
        Assert.False(row.RemovableWithheld);
    }

    [Fact]
    public void A_patch_whose_own_declaration_cannot_be_read_is_withheld()
    {
        // The row has to still be removable when route B runs, or the merge has
        // already settled it and the file is never read.
        var msi = new FakeApi();
        msi.AddProduct(Superseding);
        msi.HoldPatch(Superseding, Patch, Shared, state: "2", uninstallable: "0");

        var row = TheSharedPatch(Confirm(msi, UnreadableReader(Shared)));
        Assert.False(row.IsRemovable);
        Assert.True(row.RemovableWithheld);
    }

    /// <summary>
    /// A product that is not there holds no patches, so it says nothing either way
    /// and must not withhold. This is the direction that would delete the feature
    /// if it went wrong, and a patch declares mostly products the machine does not
    /// have, so it is the ordinary case rather than an edge.
    ///
    /// BOTH RETURNS ARE FORCED THROUGH THE RESOLVE RATHER THAN LEFT TO THE FAKE.
    /// Microsoft's return table for that function carries ERROR_NO_MORE_ITEMS and
    /// ERROR_UNKNOWN_PRODUCT, the latter glossed as the product not being
    /// installed in the context asked about, and which one a given msi.dll picks
    /// for a keyed call is not established anywhere. A fake left to choose one
    /// tests the fake's choice, and the answer this test wants is that the choice
    /// cannot matter.
    /// </summary>
    [Theory]
    [InlineData(NoMoreItems)]
    [InlineData(UnknownProduct)]
    public void A_declared_target_that_is_not_installed_is_a_clean_answer(uint notInstalled)
    {
        var msi = new FakeApi();
        msi.AddProduct(Superseding);
        msi.HoldPatch(Superseding, Patch, Shared, state: "2", uninstallable: "0");
        msi.ProductResolveResult[NotInstalled] = notInstalled;

        Assert.True(TheSharedPatch(Confirm(msi, Reader(Shared, NotInstalled))).IsRemovable);
    }

    [Fact]
    public void A_declared_target_that_cannot_be_located_withholds()
    {
        var msi = new FakeApi();
        msi.AddProduct(Superseding);
        msi.HoldPatch(Superseding, Patch, Shared, state: "2", uninstallable: "0");
        msi.ProductResolveResult[StillApplied] = BadConfiguration;

        Assert.True(TheSharedPatch(Confirm(msi, Reader(Shared, StillApplied))).RemovableWithheld);
    }

    /// <summary>
    /// The registry names a product the walk never returned, the keyed ask finds it
    /// installed, and it turns out to be holding the patch. Nothing else on the
    /// machine can reach it: the walk did not return it, so the product loop never
    /// asked it anything, and it holds the patch invisibly to every enumeration, so
    /// route A does not name it and the patch file declares no targets here.
    ///
    /// The verdict must come back as an ordinary claim rather than as a withholding,
    /// and that distinction is the entire gain over a headcount. A shortfall against
    /// the registry's own total can only ever say "something was missed, so trust
    /// nothing"; a name can be put to Windows, and Windows answers.
    /// </summary>
    [Fact]
    public void A_product_only_the_registry_names_is_asked_and_its_claim_stands()
    {
        var msi = new FakeApi();
        msi.AddProduct(Superseding);
        msi.HoldPatch(Superseding, Patch, Shared, state: "2", uninstallable: "0");
        msi.HiddenFromWalk.Add(StillApplied);
        msi.HoldPatchInvisibleToEnumeration(StillApplied, Patch, state: "1", uninstallable: "1");

        // The recovered product is handed in, standing for the registry
        // comparison that finds it and the keyed ask that confirms it installed;
        // that part of the scan is unchanged and has its own tests below.
        var row = TheSharedPatch(Confirm(msi, null, StillApplied));
        Assert.False(row.IsRemovable);
        // Not withheld: a product said it still holds the patch. The scan knows
        // why this file is being kept and could say so.
        Assert.False(row.RemovableWithheld);
        Assert.Contains((Patch, StillApplied), msi.ConfirmationAsks);
    }

    /// <summary>
    /// The residue case, and the one the tolerance band existed to guess at. A
    /// UserData key outliving its product makes the registry run ahead of the live
    /// set, which is ordinary on a healthy machine. Asked by name, it answers "not
    /// installed", which settles it: no product is missing, so nothing is withheld
    /// and the removable patch is still offered.
    ///
    /// A headcount cannot reach this answer at any tolerance. It sees one more key
    /// than products and has to choose between absorbing a real truncation and
    /// withholding on a stale key, which is the choice the band was picking a
    /// number for.
    /// </summary>
    [Fact]
    public async Task A_registry_key_whose_product_is_gone_settles_as_residue_and_costs_nothing()
    {
        var msi = new FakeApi();
        msi.AddProduct(Superseding);
        msi.HoldPatch(Superseding, Patch, Shared, state: "2", uninstallable: "0");

        // Named by the registry, installed nowhere: neither walked nor hidden.
        // The enumeration settles it as residue and counts nothing, and the
        // confirmation pass is left with nothing to overturn.
        var result = await Run(msi, Registry(Superseding, NotInstalled));
        Assert.Equal(0, result.UnaccountedProductCount);

        Assert.True(TheSharedPatch(Confirm(msi)).IsRemovable);
    }

    /// <summary>
    /// The registry names a product and Windows will not say whether it is
    /// installed. Whether the enumeration was complete cannot be established, so
    /// the removable class goes. This is the honest end of the mechanism: it
    /// refuses to answer rather than picking the answer that keeps the list long.
    /// </summary>
    [Fact]
    public async Task A_registry_code_windows_will_not_answer_about_withholds()
    {
        var msi = new FakeApi();
        msi.AddProduct(Superseding);
        msi.HoldPatch(Superseding, Patch, Shared, state: "2", uninstallable: "0");
        msi.ProductResolveResult[StillApplied] = BadConfiguration;

        // A product the registry named and Windows would not answer about is one the
        // enumeration cannot account for, which drives the refusal gate and the
        // records-incomplete notice.
        //
        // AND IT NOW WITHHOLDS SOMETHING, WHICH IS WHAT CHANGED. This asserted the
        // flag was clear, on a comment saying the withholding had nothing left to
        // withhold: true while no scan offered a superseded patch at all. 3.0.0
        // offers that class, so a scan that cannot account for a product takes the
        // whole removable class back and marks every row it took, which is the
        // long-standing rule finally having a subject again.
        //
        // THE ASSERTION WAS RIGHT AND THE FIXTURE COULD NOT REACH IT, WHICH IS THE
        // WHOLE OF WHAT WAS WRONG HERE. The withholding is guarded on the row still
        // being removable when it runs, and it runs after the per-product condition.
        // Built on a registry read carrying no patch sets, every product read as
        // unestablished, so the condition had already taken the verdict away and set
        // this very flag, and the withholding this test is named for was stepped over
        // on a row it could no longer touch. Delete that withholding outright and the
        // test still passed. Its own commit named the next CI run as what would settle
        // the reading; CI cannot settle it, because it is green either way. A clean
        // patch set for every product is what leaves the row removable, so the flag
        // below can only have come from the rule in the name.
        var result = await Run(msi, RegistryWithCleanPatchSets(Superseding, StillApplied));

        Assert.Equal(1, result.UnaccountedProductCount);
        var row = Assert.Single(result.Packages);
        Assert.False(row.IsRemovable);
        Assert.True(row.RemovableWithheld);
    }

    /// <summary>
    /// The must-miss control for the test above, and without it that one passes just
    /// as well against a run that withholds unconditionally.
    ///
    /// One field of the machine differs: no product is unaccounted for. Everything
    /// else is the same fixture, so if the row comes back offered here and withheld
    /// there, the unaccounted product is what did it and nothing else can be.
    /// </summary>
    [Fact]
    public async Task The_same_machine_with_every_product_accounted_for_still_offers_the_patch()
    {
        var msi = new FakeApi();
        msi.AddProduct(Superseding);
        msi.HoldPatch(Superseding, Patch, Shared, state: "2", uninstallable: "0");

        var result = await Run(msi, RegistryWithCleanPatchSets(Superseding));

        Assert.Equal(0, result.UnaccountedProductCount);
        var row = Assert.Single(result.Packages);
        Assert.True(row.IsRemovable);
        Assert.False(row.RemovableWithheld);
    }

    /// <summary>
    /// A fallback that never ran reports no codes at all, and that must not read as
    /// a registry holding nothing: an empty set says the enumeration missed no
    /// product, where no set at all says nobody looked. A comparison that did not
    /// happen may neither recover a product nor withhold on its own silence.
    /// </summary>
    [Fact]
    public async Task No_registry_read_neither_recovers_nor_withholds()
    {
        var msi = new FakeApi();
        msi.AddProduct(Superseding);
        msi.HoldPatch(Superseding, Patch, Shared, state: "2", uninstallable: "0");

        var result = await Run(msi);
        Assert.Equal(0, result.Census.RecoveredProductCount);
        Assert.Equal(0, result.Census.UnansweredProductCount);

        Assert.True(TheSharedPatch(Confirm(msi)).IsRemovable);
    }

    /// <summary>
    /// The two census tallies. A machine reporting a recovered product and one
    /// reporting only residue are the two states a difference between product
    /// totals cannot tell apart, which is what a tolerance band on that difference
    /// was guessing at before it was removed. These separate them by name, and
    /// report the separation per machine.
    /// </summary>
    [Fact]
    public async Task The_census_separates_a_recovered_product_from_registry_residue()
    {
        var msi = new FakeApi();
        msi.AddProduct(Superseding);
        msi.HoldPatch(Superseding, Patch, Shared, state: "2", uninstallable: "0");
        msi.HiddenFromWalk.Add(StillApplied);

        var result = await Run(msi, Registry(Superseding, StillApplied, NotInstalled));

        Assert.Equal(1, result.Census.RecoveredProductCount);
        Assert.Equal(0, result.Census.UnansweredProductCount);
        Assert.Equal(3, result.Census.RegistryProductKeys);
        Assert.Equal(1, result.Census.ProductCount);
    }

    /// <summary>
    /// A per-user product must be asked about AS ITSELF: the account and context
    /// the walk handed back have to come round to the keyed read unchanged.
    ///
    /// THIS IS THE FAULT CLASS THAT HAS COST THIS PASS TWICE, and neither instance
    /// was a wrong verdict anybody could see. A keyed read given the wrong account
    /// is refused by Windows, the refusal is read as "could not ask", and every
    /// candidate is withheld: the app finds nothing, on every machine, while every
    /// test that only checks which PAIRINGS were asked still passes. Asserting the
    /// pairing is not asserting the question.
    /// </summary>
    [Fact]
    public void A_per_user_product_is_asked_under_its_own_account_and_context()
    {
        const string Sid = "S-1-5-21-1-2-3-1001";
        var msi = new FakeApi();
        msi.AddProduct(Superseding);
        msi.HoldPatch(Superseding, Patch, Shared, state: "2", uninstallable: "0");
        msi.AddPerUserProduct(StillApplied, Sid);
        msi.HoldPatchInvisibleToEnumeration(StillApplied, Patch, state: "1", uninstallable: "1");

        var row = TheSharedPatch(Confirm(msi));

        Assert.Contains((Patch, StillApplied, (string?)Sid, MsiInstallContext.UserUnmanaged),
            msi.ConfirmationAskIdentities);
        Assert.False(row.IsRemovable);
        Assert.False(row.RemovableWithheld);
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

        /// <summary>
        /// The same asks with the account and context each was made under. Kept
        /// beside the pairing rather than folded into it because the two questions
        /// are different: the pairing asks whether the right product was asked at
        /// all, and this asks whether it was asked AS ITSELF. Nothing recorded the
        /// second until a defect turned on it.
        /// </summary>
        public List<(string Patch, string Product, string? Sid, MsiInstallContext Context)>
            ConfirmationAskIdentities { get; } = new();

        public void AddProduct(string code) => _products.Add(code);

        /// <summary>
        /// A product installed under a named account rather than per-machine. The
        /// walk hands back its SID and context, and every keyed read about it is
        /// then supposed to carry both back unchanged.
        /// </summary>
        public void AddPerUserProduct(string code, string sid)
        {
            _products.Add(code);
            _perUser[code] = sid;
        }

        private readonly Dictionary<string, string> _perUser = new();

        /// <summary>How many products the walk returns, for the headcount tests.</summary>
        public int WalkedProducts => _products.Count;

        /// <summary>
        /// What the walk returns, each with the account and context it is
        /// installed under, in walk order. The enumeration hands this list to the
        /// confirmation pass, so the harness that drives that pass directly has to
        /// hand it the same thing; hidden products are absent, which is what makes
        /// them hidden.
        /// </summary>
        public IEnumerable<(string ProductCode, string? UserSid, MsiInstallContext Context)>
            WalkedProductInstances =>
            _products.Select(p => _perUser.TryGetValue(p, out var sid)
                ? (p, (string?)sid, MsiInstallContext.UserUnmanaged)
                : (p, (string?)null, MsiInstallContext.Machine));

        /// <summary>
        /// The (patch, product) pairings the patch enumeration would have read:
        /// every patch a walked product holds, unless that product's enumeration
        /// ends early or the pairing was declared invisible to enumeration. Both
        /// of those are the fault the confirmation pass exists to close, so a
        /// harness that included them would be handing the pass the answer.
        /// </summary>
        public IEnumerable<(string Patch, string Product, string Path, string State,
            string Uninstallable, string? UserSid, MsiInstallContext Context)> EnumeratedPairings()
        {
            foreach (var (product, sid, context) in WalkedProductInstances)
            {
                if (EnumerationEndsEarlyFor.Contains(product)) continue;
                if (!_patchesOf.TryGetValue(product, out var patches)) continue;
                foreach (var patch in patches)
                    yield return (patch, product,
                        _patchProps[(patch, product, "LocalPackage")],
                        _patchProps[(patch, product, "State")],
                        _patchProps[(patch, product, "Uninstallable")],
                        sid, context);
            }
        }

        /// <summary>
        /// Every (product, patch) registration this fixture holds, whether or not any
        /// enumeration names it.
        ///
        /// IT IS DELIBERATELY WIDER THAN <see cref="EnumeratedPairings"/> AND THE
        /// DIFFERENCE IS THE WHOLE POINT. That one models what the API walk returned,
        /// so it must exclude a product the walk lost: including it would hand the
        /// confirmation pass the very claim the pass exists to go and find. This one
        /// models what the REGISTRY holds, and the registry does see a product the
        /// walk lost, because the per-product patch sets are read by walking
        /// UserData's own product subkeys rather than the enumeration's output.
        /// Building both from one source made every lost product's patch set read
        /// unestablished, which withheld the path before the pass could reach it.
        /// </summary>
        public IEnumerable<(string Product, string Uninstallable)> AllRegistrations()
        {
            foreach (var ((_, product, prop), value) in _patchProps)
                if (prop == "Uninstallable")
                    yield return (product, value);
        }

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
            var walked = _products[(int)index];
            Write(installedProductCode, walked);
            if (_perUser.TryGetValue(walked, out var perUserSid))
            {
                installedContext = MsiInstallContext.UserUnmanaged;
                Write(sid, perUserSid);
                sidLength = (uint)perUserSid.Length;
            }
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
            if (!enumeratedIt)
            {
                ConfirmationAsks.Add((patchCode, productCode));
                ConfirmationAskIdentities.Add((patchCode, productCode, userSid, context));
            }

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
