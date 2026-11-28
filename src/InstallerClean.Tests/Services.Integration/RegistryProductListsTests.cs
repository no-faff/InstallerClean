using InstallerClean.Interop;
using InstallerClean.Services;
using Microsoft.Win32;

namespace InstallerClean.Tests.Services.Integration;

/// <summary>
/// The accounts whose own lists the product walk reads one at a time, as the registry of the
/// machine running the suite holds them.
///
/// AN INTEGRATION TEST BECAUSE THE FACT IT TURNS ON IS THE MACHINE'S. The system account's
/// hive is loaded under <c>HKEY_USERS</c> on every Windows, and Windows Installer refuses a
/// per-user walk for it with ERROR_INVALID_PARAMETER. An account list holding it would make
/// every stopped per-user walk end in a refusal, so the test first shows the machine holds
/// the hive, which is what lets it fail.
/// </summary>
public class RegistryProductListsTests
{
    private const string LocalSystem = "S-1-5-18";

    [Fact]
    public void The_system_account_is_never_an_account_whose_own_list_is_walked()
    {
        using (var users = RegistryKey.OpenBaseKey(RegistryHive.Users, RegistryView.Registry64))
        using (var hive = users.OpenSubKey(LocalSystem))
            Assert.NotNull(hive);

        foreach (var context in new[] { MsiInstallContext.UserUnmanaged, MsiInstallContext.UserManaged })
        {
            var accounts = RegistryProductLists.Instance.Accounts(context, CancellationToken.None);
            Assert.NotNull(accounts);
            Assert.DoesNotContain(accounts!, a => string.Equals(a, LocalSystem, StringComparison.OrdinalIgnoreCase));
        }
    }

    [Fact]
    public void Every_hive_loaded_under_an_accounts_name_but_the_systems_is_an_account_whose_own_list_is_walked()
    {
        // The account running the suite has its hive loaded, so the machine holds at least one
        // such hive and the test can fail.
        using var users = RegistryKey.OpenBaseKey(RegistryHive.Users, RegistryView.Registry64);
        var hives = users.GetSubKeyNames()
            .Where(name => InstallerQueryService.IsAccount(name)
                           && !string.Equals(name, LocalSystem, StringComparison.OrdinalIgnoreCase))
            .ToArray();
        Assert.NotEmpty(hives);

        var accounts = RegistryProductLists.Instance.Accounts(MsiInstallContext.UserUnmanaged, CancellationToken.None);

        Assert.NotNull(accounts);
        foreach (var hive in hives)
            Assert.Contains(accounts!, a => string.Equals(a, hive, StringComparison.OrdinalIgnoreCase));
    }
}
