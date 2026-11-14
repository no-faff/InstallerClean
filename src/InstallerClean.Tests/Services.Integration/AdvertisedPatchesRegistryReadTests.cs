using System.Security.AccessControl;
using System.Security.Principal;
using InstallerClean.Services;
using Microsoft.Win32;

namespace InstallerClean.Tests.Services.Integration;

/// <summary>
/// What <see cref="InstallerQueryService.ReadAdvertisedPatches"/> reads off a product's
/// advertised key, read from a real <c>RegistryKey</c>. The key standing in for
/// <c>Classes\Installer\Products</c> is built under a GUID-named key in HKCU and removed in a
/// finally, so nothing needs elevation and nothing outlives the test.
///
/// A list that will not read is null, and every reader holds the program's superseded patches
/// back on it.
/// </summary>
public class AdvertisedPatchesRegistryReadTests
{
    // A real product and patch code and the packed names Windows Installer writes them under.
    private const string ProductCode = "{F0C3E5D1-1ADE-321E-8167-68EF0DE699A5}";
    private const string PackedProduct = "1D5E3C0FEDA1E123187686FED06E995A";
    private const string PatchCode = "{6F8500D2-A80F-3347-9081-B41E71C8592B}";
    private const string PackedPatch = "2D0058F6F08A743309184BE1178C95B2";

    private const string PatchesKey = $@"{PackedProduct}\Patches";

    [Fact]
    public void Each_value_named_by_a_packed_code_is_a_patch_and_the_keys_own_list_and_default_value_are_not()
    {
        WithProducts(products =>
        {
            using (var patches = products.CreateSubKey(PatchesKey, writable: true)!)
            {
                patches.SetValue(PackedPatch, ":SP1.1;:#SP1.1", RegistryValueKind.String);
                patches.SetValue("Patches", new[] { PackedPatch }, RegistryValueKind.MultiString);
                patches.SetValue("", "default", RegistryValueKind.String);
            }

            Assert.Equal(new[] { PatchCode },
                InstallerQueryService.ReadAdvertisedPatches(products, ProductCode, CancellationToken.None));
        });
    }

    [Fact]
    public void A_product_key_with_no_Patches_key_lists_no_patch()
    {
        WithProducts(products =>
        {
            products.CreateSubKey(PackedProduct, writable: true)!.Dispose();

            Assert.Empty(InstallerQueryService.ReadAdvertisedPatches(products, ProductCode, CancellationToken.None)!);
        });
    }

    /// <summary>
    /// Windows Installer listed the product, so a product key that is not there disagrees
    /// with it, and the list is not read.
    /// </summary>
    [Fact]
    public void A_product_key_that_is_not_there_leaves_the_list_unread()
    {
        WithProducts(products =>
            Assert.Null(InstallerQueryService.ReadAdvertisedPatches(products, ProductCode, CancellationToken.None)));
    }

    [Fact]
    public void A_value_named_by_anything_else_leaves_the_list_unread()
    {
        WithProducts(products =>
        {
            using (var patches = products.CreateSubKey(PatchesKey, writable: true)!)
            {
                patches.SetValue(PackedPatch, "", RegistryValueKind.String);
                patches.SetValue("not-a-code", "", RegistryValueKind.String);
            }

            Assert.Null(InstallerQueryService.ReadAdvertisedPatches(products, ProductCode, CancellationToken.None));
        });
    }

    [Fact]
    public void A_Patches_key_that_will_not_open_leaves_the_list_unread()
    {
        WithProducts(products =>
        {
            using (var patches = products.CreateSubKey(PatchesKey, writable: true)!)
                patches.SetValue(PackedPatch, "", RegistryValueKind.String);

            SetDeny(products, add: true);
            try
            {
                Assert.Null(InstallerQueryService.ReadAdvertisedPatches(products, ProductCode, CancellationToken.None));
            }
            finally
            {
                SetDeny(products, add: false);
            }
        });
    }

    [Fact]
    public void A_code_that_will_not_pack_leaves_the_list_unread()
    {
        WithProducts(products =>
            Assert.Null(InstallerQueryService.ReadAdvertisedPatches(products, "not-a-code", CancellationToken.None)));
    }

    private static readonly RegistryRights DeniedRights =
        RegistryRights.QueryValues | RegistryRights.EnumerateSubKeys;

    private static void SetDeny(RegistryKey products, bool add)
    {
        using var key = products.OpenSubKey(PatchesKey, RegistryKeyPermissionCheck.ReadWriteSubTree,
            RegistryRights.ChangePermissions | RegistryRights.ReadPermissions)!;
        var security = key.GetAccessControl();
        var rule = new RegistryAccessRule(WindowsIdentity.GetCurrent().User!, DeniedRights,
            AccessControlType.Deny);
        if (add) security.AddAccessRule(rule);
        else security.RemoveAccessRule(rule);
        key.SetAccessControl(security);
    }

    private static void WithProducts(Action<RegistryKey> body)
    {
        var keyPath = $@"Software\InstallerCleanTests\{Guid.NewGuid():N}";
        try
        {
            using var products = Registry.CurrentUser.CreateSubKey(keyPath, writable: true);
            Assert.NotNull(products);
            body(products);
        }
        finally
        {
            try { Registry.CurrentUser.DeleteSubKeyTree(keyPath, throwOnMissingSubKey: false); }
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
