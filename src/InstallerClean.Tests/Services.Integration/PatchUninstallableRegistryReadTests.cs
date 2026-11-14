using System.Security.AccessControl;
using System.Security.Principal;
using InstallerClean.Services;
using Microsoft.Win32;

namespace InstallerClean.Tests.Services.Integration;

/// <summary>
/// What <see cref="InstallerQueryService.ReadPatchUninstallable"/> answers for each shape the
/// <c>Uninstallable</c> value of one pairing can take, read from a real <c>RegistryKey</c>.
/// The account's subtree is built under a GUID-named key in HKCU standing in for
/// <c>UserData\S-1-5-18</c> and removed in a finally, so nothing needs elevation and nothing
/// outlives the test.
///
/// Only a <c>REG_DWORD</c> that reads gives a number. The check before a Move or Delete passes
/// a per-machine sibling on a zero alone, so every other shape answers null and holds the
/// file.
/// </summary>
public class PatchUninstallableRegistryReadTests
{
    // A real product and patch code and the packed names Windows Installer writes them under,
    // so the read's own packing names the key the test built.
    private const string ProductCode = "{F0C3E5D1-1ADE-321E-8167-68EF0DE699A5}";
    private const string PackedProduct = "1D5E3C0FEDA1E123187686FED06E995A";
    private const string PatchCode = "{6F8500D2-A80F-3347-9081-B41E71C8592B}";
    private const string PackedPatch = "2D0058F6F08A743309184BE1178C95B2";

    private const string PatchKey = $@"Products\{PackedProduct}\Patches\{PackedPatch}";

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    public void A_DWORD_value_is_read_as_its_number(int value)
    {
        WithAccount(account =>
        {
            using (var key = account.CreateSubKey(PatchKey, writable: true)!)
                key.SetValue("Uninstallable", value, RegistryValueKind.DWord);

            Assert.Equal(value, InstallerQueryService.ReadPatchUninstallable(account, ProductCode, PatchCode));
        });
    }

    [Theory]
    [InlineData("")]
    [InlineData("0")]
    public void A_text_value_answers_nothing(string value)
    {
        WithAccount(account =>
        {
            using (var key = account.CreateSubKey(PatchKey, writable: true)!)
                key.SetValue("Uninstallable", value, RegistryValueKind.String);

            Assert.Null(InstallerQueryService.ReadPatchUninstallable(account, ProductCode, PatchCode));
        });
    }

    [Fact]
    public void A_value_that_is_not_there_answers_nothing()
    {
        WithAccount(account =>
        {
            using (var key = account.CreateSubKey(PatchKey, writable: true)!)
                key.SetValue("State", 1, RegistryValueKind.DWord);

            Assert.Null(InstallerQueryService.ReadPatchUninstallable(account, ProductCode, PatchCode));
        });
    }

    [Fact]
    public void A_key_that_is_not_there_answers_nothing()
    {
        WithAccount(account =>
        {
            account.CreateSubKey($@"Products\{PackedProduct}\Patches", writable: true)!.Dispose();

            Assert.Null(InstallerQueryService.ReadPatchUninstallable(account, ProductCode, PatchCode));
        });
    }

    /// <summary>
    /// The patch's key holds a zero and denies this account the right to read it, so the
    /// open fails with access denied.
    /// </summary>
    [Fact]
    public void A_key_that_will_not_open_answers_nothing()
    {
        WithAccount(account =>
        {
            using (var key = account.CreateSubKey(PatchKey, writable: true)!)
                key.SetValue("Uninstallable", 0, RegistryValueKind.DWord);

            SetDeny(account, add: true);
            try
            {
                Assert.Null(InstallerQueryService.ReadPatchUninstallable(account, ProductCode, PatchCode));
            }
            finally
            {
                SetDeny(account, add: false);
            }
        });
    }

    [Theory]
    [InlineData("not-a-code", PatchCode)]
    [InlineData(ProductCode, "not-a-code")]
    public void A_code_that_will_not_pack_answers_nothing(string productCode, string patchCode)
    {
        WithAccount(account =>
        {
            using (var key = account.CreateSubKey(PatchKey, writable: true)!)
                key.SetValue("Uninstallable", 0, RegistryValueKind.DWord);

            Assert.Null(InstallerQueryService.ReadPatchUninstallable(account, productCode, patchCode));
        });
    }

    private static readonly RegistryRights DeniedRights =
        RegistryRights.QueryValues | RegistryRights.EnumerateSubKeys;

    private static void SetDeny(RegistryKey account, bool add)
    {
        using var key = account.OpenSubKey(PatchKey, RegistryKeyPermissionCheck.ReadWriteSubTree,
            RegistryRights.ChangePermissions | RegistryRights.ReadPermissions)!;
        var security = key.GetAccessControl();
        var rule = new RegistryAccessRule(WindowsIdentity.GetCurrent().User!, DeniedRights,
            AccessControlType.Deny);
        if (add) security.AddAccessRule(rule);
        else security.RemoveAccessRule(rule);
        key.SetAccessControl(security);
    }

    private static void WithAccount(Action<RegistryKey> body)
    {
        var keyPath = $@"Software\InstallerCleanTests\{Guid.NewGuid():N}";
        try
        {
            using var account = Registry.CurrentUser.CreateSubKey(keyPath, writable: true);
            Assert.NotNull(account);
            body(account);
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
