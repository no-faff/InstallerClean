using InstallerClean.Models;
using InstallerClean.Services;
using Microsoft.Win32;

namespace InstallerClean.Tests.Services.Integration;

/// <summary>
/// What the registry's per-product patch listing reduces to, which is the reading
/// the superseded-patch condition rests on.
///
/// AN INTEGRATION TEST BECAUSE THE SUBJECT IS A REAL KEY LISTING. The reader takes a
/// <c>RegistryKey</c> and walks its subkeys, and the properties that matter are ones
/// a fake cannot have: that a value's registry TYPE decides whether it is accepted,
/// that a subkey with no value at all is distinguishable from one carrying a zero,
/// and that <c>AllPatches</c> sitting beside the subkeys is not read.
///
/// Writes are confined to a GUID-named key under HKCU and removed in a finally, so
/// nothing needs elevation and nothing outlives the test, exactly as
/// <see cref="LocalPackageValueTypeTests"/> does it.
///
/// THE PACKED KEY NAMES HERE ARE NOT REAL PACKED GUIDS AND DO NOT NEED TO BE. The
/// reader is handed the product key's own name and opens two subkeys under it; it
/// never parses either name. Unpacking is the caller's job and has its own tests.
/// </summary>
public class ProductPatchSetTests
{
    private const string Product = "0000000000000000000000000000AAAA";

    // TWO REAL PATCH KEY NAMES AND THE CODES THEY UNPACK TO, taken from the pairs
    // measured off one elevated machine's hive and pinned in
    // InstallerQueryServiceProductCodeUnpackTests. Real ones rather than invented
    // ones, because these fixtures are about what the reader makes of a name and an
    // expectation composed by applying the same reading of the format the code
    // applies would pass on a shared misunderstanding.
    //
    // THE OTHER FIXTURES IN THIS FILE STILL NAME THEIR PATCHES p1 AND p2, and that is
    // deliberate rather than an oversight: those tests are about the verdict, which
    // never parses a name, and their unparseable names now also exercise the arm where
    // the listing cannot be established while the verdict still reads clean.
    private const string PackedPatchOne = "4D54076CED4F5BA32BBD3E5FAD1CD4C9";
    private const string PatchOne = "{C67045D4-F4DE-3AB5-B2DB-E3F5DAC14D9C}";
    private const string PackedPatchTwo = "2D0058F6F08A743309184BE1178C95B2";
    private const string PatchTwo = "{6F8500D2-A80F-3347-9081-B41E71C8592B}";

    [Fact]
    public void Every_patch_declaring_a_zero_is_the_only_clean_answer()
    {
        WithProductsKey(products =>
        {
            Patch(products, "p1", uninstallable: 0);
            Patch(products, "p2", uninstallable: 0);

            var (set, keys, registrations, _) = Read(products);

            Assert.Equal(ProductPatchSet.AllNonRemovable, set);
            Assert.Equal(1, keys);
            Assert.Equal(2, registrations);
        });
    }

    [Fact]
    public void One_patch_declaring_non_zero_fails_the_whole_product()
    {
        // The condition is about the product, not the patch: any patch here that can
        // be uninstalled can reach for a superseded sibling's cached file, so one is
        // enough and the other patches' answers do not soften it.
        WithProductsKey(products =>
        {
            Patch(products, "p1", uninstallable: 0);
            Patch(products, "p2", uninstallable: 1);
            Patch(products, "p3", uninstallable: 0);

            var (set, _, _, _) = Read(products);

            Assert.Equal(ProductPatchSet.RemovablePatchPresent, set);
        });
    }

    [Theory]
    // Stored as text, which is how the API side reports the same property. Accepting
    // it here would read an unanticipated store as a clean product, which is the one
    // direction that puts a file on the list.
    [InlineData(RegistryValueKind.String, "0")]
    // A 64-bit number, which is not what a REG_DWORD read yields.
    [InlineData(RegistryValueKind.QWord, 0L)]
    // A binary value, the shape a corrupt write leaves.
    [InlineData(RegistryValueKind.Binary, new byte[] { 0 })]
    public void A_value_that_is_not_an_int_leaves_the_set_unestablished(RegistryValueKind kind, object value)
    {
        WithProductsKey(products =>
        {
            using var patches = products.CreateSubKey($@"{Product}\Patches\p1", writable: true)!;
            patches.SetValue("Uninstallable", value, kind);

            var (set, _, registrations, _) = Read(products);

            Assert.Equal(ProductPatchSet.Unestablished, set);
            // Counted anyway: the registration exists and was seen, which is what the
            // shape figure is for. Only the verdict withholds.
            Assert.Equal(1, registrations);
        });
    }

    [Fact]
    public void A_patch_carrying_no_Uninstallable_leaves_the_set_unestablished()
    {
        WithProductsKey(products =>
        {
            using var patch = products.CreateSubKey($@"{Product}\Patches\p1", writable: true)!;
            patch.SetValue("State", 2, RegistryValueKind.DWord);

            var (set, _, _, _) = Read(products);

            Assert.Equal(ProductPatchSet.Unestablished, set);
        });
    }

    [Fact]
    public void A_positive_finding_outranks_an_unestablished_one_within_a_product()
    {
        // Both withhold, so the assertion is about which CAUSE the row carries: the
        // app knowing there is a removable patch is a finding, and being unable to
        // read one patch is not, so the finding is what gets said.
        WithProductsKey(products =>
        {
            Patch(products, "p1", uninstallable: 1);
            using var broken = products.CreateSubKey($@"{Product}\Patches\p2", writable: true)!;
            broken.SetValue("Uninstallable", "nonsense", RegistryValueKind.String);

            var (set, _, _, _) = Read(products);

            Assert.Equal(ProductPatchSet.RemovablePatchPresent, set);
        });
    }

    [Fact]
    public void A_product_with_no_Patches_key_is_clean_and_its_key_is_not_counted()
    {
        // WHAT THIS FIXTURE MAKES TRUE: the product key exists and carries no Patches
        // subkey at all. That is a product holding no registered patch, so it holds no
        // removable one, so nothing on it can be uninstalled and reach for a
        // superseded patch's cached file. The verdict says so.
        //
        // IT USED TO ANSWER Unestablished AND THE RECORDED REASON WAS THAT THE TWO WAYS
        // OF GETTING NOTHING COULD NOT BE TOLD APART. They can: a key that exists and
        // will not open THROWS, and the caller catches it and writes Unestablished with
        // its own failure cause. Nothing that fails to open arrives here.
        //
        // AND THE OLD REASON'S SECOND HALF WAS THAT IT COST NOTHING, because the
        // verdict was only ever consulted for a product some candidate patch was
        // registered to. That stopped being true when the judged product set gained
        // the patch file's own declared targets, which name products holding no patch
        // at all. Withholding on those would have withheld the superseded class on
        // every ordinary machine.
        WithProductsKey(products =>
        {
            using var _ = products.CreateSubKey($@"{Product}\InstallProperties", writable: true)!;

            var (set, keys, registrations, codes) = Read(products);

            Assert.Equal(ProductPatchSet.AllNonRemovable, set);
            // AND THE SAME SENTENCE ABOUT THE LISTING, WHICH IS A SEPARATE ANSWER AND
            // NOT A RESTATEMENT. A product holding no registered patch holds a
            // complete, empty list of them, and the caller may act on that: a
            // recovered product whose list is empty can reach for no cached file at
            // all. NotNull is the assertion that matters. Null here would mean "nobody
            // established what this product holds", which is what every unreadable
            // reading returns, and it would keep every file back on this machine
            // rather than none.
            Assert.NotNull(codes);
            Assert.Empty(codes);
            // NOT COUNTED, AND THAT IS WHAT KEEPS THIS FIXTURE DISTINGUISHABLE FROM THE
            // NEXT ONE. The two now agree on the verdict, deliberately, because they
            // say the same thing about the machine. The counter is the only thing left
            // that tells them apart, and it answers a different question: how usual it
            // is for a product to carry the key at all. Moving the increment above the
            // branch would make these two fixtures indistinguishable in every respect
            // and neither test could then fail at its own subject.
            Assert.Equal(0, keys);
            Assert.Equal(0, registrations);
        });
    }

    [Fact]
    public void An_empty_Patches_key_is_clean_and_says_so()
    {
        // WHAT THIS FIXTURE MAKES TRUE: the Patches key exists and lists nothing. The
        // key opened and the listing was complete, which is a reading of an empty set
        // rather than a failure to read one.
        //
        // IT IS THE PAIR TO THE ABSENT-KEY TEST ABOVE AND THE RELATIONSHIP HAS
        // CHANGED. They used to answer differently and this one was that one's
        // must-miss control. They now answer the SAME verdict, because an absent list
        // and an empty list say the same thing about the machine, and the pair is held
        // apart by the key counter instead: 0 here against 1 there. A reader that
        // stopped distinguishing them at all would still pass both assertions on the
        // verdict and would fail on the counts, which is why the counts are asserted.
        WithProductsKey(products =>
        {
            using var _ = products.CreateSubKey($@"{Product}\Patches", writable: true)!;

            var (set, keys, registrations, codes) = Read(products);

            Assert.Equal(ProductPatchSet.AllNonRemovable, set);
            Assert.Equal(1, keys);
            Assert.Equal(0, registrations);
            // A listing that was taken and named nothing, which is the same answer the
            // absent key gives and reached the other way about. Empty rather than
            // null, for the same reason it is empty there.
            Assert.NotNull(codes);
            Assert.Empty(codes);
        });
    }

    [Fact]
    public void The_listing_names_every_patch_even_where_the_verdict_stops_at_the_first()
    {
        // WHAT THIS FIXTURE MAKES TRUE: a product holding two registered patches,
        // BOTH of them declaring themselves uninstallable, so the verdict loop returns
        // at whichever the enumeration reaches first and never sees the other. The
        // listing must still name both.
        //
        // WHY BOTH ARE REMOVABLE RATHER THAN ONE. Key enumeration order is not
        // something a test may assume, and with one removable patch this would prove
        // the property only when the enumeration happened to reach that one first. With
        // both removable the loop returns early whichever it reaches, so the fixture
        // cannot pass by luck.
        //
        // WHAT IT IS GUARDING. The listing is what lets a product recovered by name be
        // judged against the files it can reach rather than against all of them, and a
        // listing built inside the verdict loop would come back SHORT exactly on the
        // products that hold something uninstallable, which is to say on the products
        // that can cost somebody a file. That version of this code would still pass
        // every other test in this file.
        WithProductsKey(products =>
        {
            Patch(products, PackedPatchOne, uninstallable: 1);
            Patch(products, PackedPatchTwo, uninstallable: 1);

            var (set, _, _, codes) = Read(products);

            Assert.Equal(ProductPatchSet.RemovablePatchPresent, set);
            Assert.NotNull(codes);
            Assert.Equal(
                new[] { PatchOne, PatchTwo }.OrderBy(c => c, StringComparer.OrdinalIgnoreCase),
                codes.OrderBy(c => c, StringComparer.OrdinalIgnoreCase));
        });
    }

    [Fact]
    public void One_patch_name_that_is_not_a_packed_guid_leaves_the_whole_listing_unestablished()
    {
        // WHAT THIS FIXTURE MAKES TRUE: two registered patches, one named in the packed
        // form and one whose name is not a packed GUID at all. The registry is saying
        // this product holds a patch and nothing here can turn that name into a code to
        // compare, so what the product holds is not established and it is judged against
        // every cached file rather than against the one code that did unpack.
        //
        // THE VERDICT IS THE CONTROL AND IT IS THE POINT OF ASSERTING IT. It reads
        // clean, exactly as it does without the unparseable name, so nothing else in
        // the reading has been disturbed and the listing is the only thing that changed.
        // A reader that skipped the bad name and returned the one good code would pass
        // the verdict assertion and fail here.
        WithProductsKey(products =>
        {
            Patch(products, PackedPatchOne, uninstallable: 0);
            Patch(products, "not-a-packed-guid", uninstallable: 0);

            var (set, _, registrations, codes) = Read(products);

            Assert.Equal(ProductPatchSet.AllNonRemovable, set);
            Assert.Equal(2, registrations);
            Assert.Null(codes);
        });
    }

    [Fact]
    public void AllPatches_is_not_read_even_when_it_contradicts_the_subkeys()
    {
        // THE TRAP THIS TEST EXISTS FOR, AND THIS TEST IS NOW THE ONLY LIVE GUARD ON
        // IT. The same key carries an AllPatches REG_MULTI_SZ that looks like a
        // ready-made list of exactly this, and on a machine holding superseded patches
        // it lists the applied patch alone and omits the superseded ones, so anything
        // built on it silently excludes the class the condition exists for. That was
        // measured on one machine while it still held superseded patches; re-run on
        // 2026-08-17 after they had gone, all 147 products agreed, because the
        // disagreement is ABOUT superseded patches. So the field evidence has
        // evaporated and cannot be re-taken there. This test plants the disagreement
        // instead of waiting for one: AllPatches names one patch, the subkeys hold a
        // second that is removable, and reading AllPatches would answer clean.
        WithProductsKey(products =>
        {
            using (var patches = products.CreateSubKey($@"{Product}\Patches", writable: true)!)
                patches.SetValue("AllPatches", new[] { "p1" }, RegistryValueKind.MultiString);
            Patch(products, "p1", uninstallable: 0);
            Patch(products, "p2", uninstallable: 1);

            var (set, _, registrations, _) = Read(products);

            Assert.Equal(ProductPatchSet.RemovablePatchPresent, set);
            Assert.Equal(2, registrations);
        });
    }

    [Fact]
    public void Two_readings_of_one_product_merge_to_the_worse()
    {
        // Both argument orders for every pair, so the merge cannot depend on which SID
        // subtree the walk reached first. A Theory would be the natural shape and
        // cannot be used: the enum is internal, and an internal parameter type on the
        // public method xUnit needs is a compile error.
        const ProductPatchSet clean =
            ProductPatchSet.AllNonRemovable;
        const ProductPatchSet unknown =
            ProductPatchSet.Unestablished;
        const ProductPatchSet removable =
            ProductPatchSet.RemovablePatchPresent;

        Assert.Equal(clean, InstallerQueryService.Worse(clean, clean));

        Assert.Equal(unknown, InstallerQueryService.Worse(clean, unknown));
        Assert.Equal(unknown, InstallerQueryService.Worse(unknown, clean));
        Assert.Equal(unknown, InstallerQueryService.Worse(unknown, unknown));

        Assert.Equal(removable, InstallerQueryService.Worse(clean, removable));
        Assert.Equal(removable, InstallerQueryService.Worse(removable, clean));
        Assert.Equal(removable, InstallerQueryService.Worse(unknown, removable));
        Assert.Equal(removable, InstallerQueryService.Worse(removable, unknown));
        Assert.Equal(removable, InstallerQueryService.Worse(removable, removable));
    }

    private static void Patch(RegistryKey products, string name, int uninstallable)
    {
        using var patch = products.CreateSubKey($@"{Product}\Patches\{name}", writable: true)!;
        patch.SetValue("Uninstallable", uninstallable, RegistryValueKind.DWord);
        patch.SetValue("State", 2, RegistryValueKind.DWord);
    }

    private static (ProductPatchSet Set, int Keys, int Registrations, IReadOnlyCollection<string>? Codes)
        Read(RegistryKey products)
    {
        var keys = 0;
        var registrations = 0;
        var set = InstallerQueryService.ReadProductPatchSet(
            products, Product, ref keys, ref registrations, out var codes);
        return (set, keys, registrations, codes);
    }

    /// <summary>
    /// Creates a throwaway HKCU key to stand in for a SID subtree's Products key,
    /// runs the body against it, and removes it whatever happens. Same shape and same
    /// reasoning as <see cref="LocalPackageValueTypeTests"/>'s own helper.
    /// </summary>
    private static void WithProductsKey(Action<RegistryKey> body)
    {
        var path = $@"Software\InstallerCleanTests\{Guid.NewGuid():N}";
        try
        {
            using var key = Registry.CurrentUser.CreateSubKey(path, writable: true);
            Assert.NotNull(key);
            body(key);
        }
        finally
        {
            try { Registry.CurrentUser.DeleteSubKeyTree(path, throwOnMissingSubKey: false); }
            catch (Exception) { /* the test's verdict must not turn on the tidy-up */ }

            try
            {
                using var parent = Registry.CurrentUser.OpenSubKey(@"Software\InstallerCleanTests");
                if (parent is not null && parent.SubKeyCount == 0 && parent.ValueCount == 0)
                {
                    parent.Dispose();
                    Registry.CurrentUser.DeleteSubKey(@"Software\InstallerCleanTests", throwOnMissingSubKey: false);
                }
            }
            catch (Exception) { /* as above */ }
        }
    }
}
