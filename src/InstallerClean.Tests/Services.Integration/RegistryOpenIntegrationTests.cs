using InstallerClean.Helpers;
using Microsoft.Win32;

namespace InstallerClean.Tests.Services.Integration;

/// <summary>
/// <see cref="RegistryOpen.OpenSubKeyEarned"/> over the real registry, under each hive's
/// base key and under a key opened beneath one, in the 64-bit view the app reads. Every
/// key asked for that is not there is named by a fresh GUID.
/// </summary>
public class RegistryOpenIntegrationTests
{
    [Theory]
    [InlineData(RegistryHive.LocalMachine)]
    [InlineData(RegistryHive.Users)]
    [InlineData(RegistryHive.CurrentUser)]
    public void A_key_that_is_not_there_under_a_hives_base_key_is_not_there(RegistryHive hive)
    {
        using var root = RegistryKey.OpenBaseKey(hive, RegistryView.Registry64);

        Assert.Null(root.OpenSubKeyEarned(Missing()));
        Assert.Null(root.OpenSubKeyEarned($@"{Missing()}\Deeper"));
    }

    [Fact]
    public void A_key_that_is_not_there_under_an_opened_key_is_not_there()
    {
        using var root = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry64);
        using var software = root.OpenSubKeyEarned("SOFTWARE");

        Assert.NotNull(software);
        Assert.Null(software!.OpenSubKeyEarned(Missing()));
    }

    [Fact]
    public void A_key_that_is_there_opens()
    {
        using var root = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry64);
        using var key = root.OpenSubKeyEarned(@"SOFTWARE\Microsoft\Windows\CurrentVersion");

        Assert.NotNull(key);
        Assert.EndsWith(@"\SOFTWARE\Microsoft\Windows\CurrentVersion", key!.Name, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void A_key_asked_for_under_a_parent_deleted_while_open_throws_with_Windows_code()
    {
        var scratch = $@"Software\InstallerCleanTests\{Guid.NewGuid():N}";
        Registry.CurrentUser.CreateSubKey($@"{scratch}\child")!.Dispose();
        try
        {
            using var root = RegistryKey.OpenBaseKey(RegistryHive.CurrentUser, RegistryView.Registry64);
            using var parent = root.OpenSubKeyEarned(scratch);
            Assert.NotNull(parent);

            Registry.CurrentUser.DeleteSubKeyTree(scratch);

            var thrown = Assert.Throws<IOException>(() => parent!.OpenSubKeyEarned("child"));
            Assert.Equal(ERROR_KEY_DELETED, thrown.HResult);
        }
        finally
        {
            Registry.CurrentUser.DeleteSubKeyTree(scratch, throwOnMissingSubKey: false);
        }
    }

    private const int ERROR_KEY_DELETED = 1018;

    private static string Missing() => "InstallerCleanTests-absent-" + Guid.NewGuid().ToString("N");
}
