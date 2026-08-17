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

    [Fact]
    public void Every_patch_declaring_a_zero_is_the_only_clean_answer()
    {
        WithProductsKey(products =>
        {
            Patch(products, "p1", uninstallable: 0);
            Patch(products, "p2", uninstallable: 0);

            var (set, keys, registrations) = Read(products);

            Assert.Equal(InstallerQueryService.ProductPatchSet.AllNonRemovable, set);
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

            var (set, _, _) = Read(products);

            Assert.Equal(InstallerQueryService.ProductPatchSet.RemovablePatchPresent, set);
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

            var (set, _, registrations) = Read(products);

            Assert.Equal(InstallerQueryService.ProductPatchSet.Unestablished, set);
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

            var (set, _, _) = Read(products);

            Assert.Equal(InstallerQueryService.ProductPatchSet.Unestablished, set);
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

            var (set, _, _) = Read(products);

            Assert.Equal(InstallerQueryService.ProductPatchSet.RemovablePatchPresent, set);
        });
    }

    [Fact]
    public void A_product_with_no_Patches_key_is_unestablished_and_its_key_is_not_counted()
    {
        // A product with no patches has no reason to carry the key, so this cannot be
        // told from a key that would not open. It costs nothing: the verdict is only
        // ever consulted for a product some candidate patch is registered to, and
        // such a product has patches by construction.
        WithProductsKey(products =>
        {
            using var _ = products.CreateSubKey($@"{Product}\InstallProperties", writable: true)!;

            var (set, keys, registrations) = Read(products);

            Assert.Equal(InstallerQueryService.ProductPatchSet.Unestablished, set);
            Assert.Equal(0, keys);
            Assert.Equal(0, registrations);
        });
    }

    [Fact]
    public void An_empty_Patches_key_is_clean_and_says_so()
    {
        // The key opened and listed nothing, which is a complete reading of an empty
        // set rather than a failure to read one. It is the must-miss control for the
        // absent-key test above: a reader that answered Unestablished for both would
        // pass that test and be unable to tell the two apart.
        WithProductsKey(products =>
        {
            using var _ = products.CreateSubKey($@"{Product}\Patches", writable: true)!;

            var (set, keys, registrations) = Read(products);

            Assert.Equal(InstallerQueryService.ProductPatchSet.AllNonRemovable, set);
            Assert.Equal(1, keys);
            Assert.Equal(0, registrations);
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

            var (set, _, registrations) = Read(products);

            Assert.Equal(InstallerQueryService.ProductPatchSet.RemovablePatchPresent, set);
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
        const InstallerQueryService.ProductPatchSet clean =
            InstallerQueryService.ProductPatchSet.AllNonRemovable;
        const InstallerQueryService.ProductPatchSet unknown =
            InstallerQueryService.ProductPatchSet.Unestablished;
        const InstallerQueryService.ProductPatchSet removable =
            InstallerQueryService.ProductPatchSet.RemovablePatchPresent;

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

    private static (InstallerQueryService.ProductPatchSet Set, int Keys, int Registrations) Read(
        RegistryKey products)
    {
        var keys = 0;
        var registrations = 0;
        var set = InstallerQueryService.ReadProductPatchSet(products, Product, ref keys, ref registrations);
        return (set, keys, registrations);
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
