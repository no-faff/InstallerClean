using InstallerClean.Helpers;
using Microsoft.Win32;

namespace InstallerClean.Tests.Services.Integration;

/// <summary>
/// <see cref="RegistryNames"/> over a real key, through the Windows calls themselves. The
/// key is a GUID-named one under HKCU, removed in a finally, so nothing needs elevation
/// and nothing outlives the test.
/// </summary>
public class RegistryNamesIntegrationTests
{
    [Fact]
    public void A_keys_subkeys_and_values_are_the_names_the_framework_lists()
    {
        WithKey(key =>
        {
            foreach (var name in new[] { "beta", "alpha", "gamma" })
                key.CreateSubKey(name)!.Dispose();
            key.SetValue("", "the default value");
            key.SetValue("one", 1, RegistryValueKind.DWord);
            key.SetValue("two", new byte[0], RegistryValueKind.None);

            Assert.Equal(key.GetSubKeyNames(), RegistryNames.SubKeysOf(key));
            Assert.Equal(key.GetValueNames(), RegistryNames.ValuesOf(key));
            Assert.Equal(3, RegistryNames.SubKeysOf(key).Length);
            Assert.Contains("", RegistryNames.ValuesOf(key));
        });
    }

    [Fact]
    public void A_key_with_nothing_under_it_lists_nothing()
    {
        WithKey(key =>
        {
            Assert.Empty(RegistryNames.SubKeysOf(key));
            Assert.Empty(RegistryNames.ValuesOf(key));
        });
    }

    [Fact]
    public void A_subkey_created_while_the_key_is_listed_is_in_the_listing()
    {
        WithKey(key =>
        {
            key.CreateSubKey("a")!.Dispose();
            key.CreateSubKey("b")!.Dispose();

            // Windows keeps a key's subkeys in name order, so one named to sort last is
            // at an index the listing has not yet reached.
            var calls = new AddingAfterFirstName(RegistryNames.CallsOn(key.Handle), key, "zz-added");

            Assert.Equal(new[] { "a", "b", "zz-added" }, RegistryNames.SubKeys(calls));
        });
    }

    [Fact]
    public void A_hives_base_key_is_refused_and_stays_usable()
    {
        using var hive = RegistryKey.OpenBaseKey(RegistryHive.CurrentUser, RegistryView.Registry64);

        Assert.Throws<ArgumentException>(() => RegistryNames.SubKeysOf(hive));
        Assert.Throws<ArgumentException>(() => RegistryNames.ValuesOf(hive));
        using var software = hive.OpenSubKey("Software");
        Assert.NotNull(software);
    }

    [Fact]
    public void A_disposed_key_is_ObjectDisposedException()
    {
        WithKey(key =>
        {
            var parentPath = key.Name.Substring(key.Name.IndexOf('\\') + 1);
            var copy = Registry.CurrentUser.OpenSubKey(parentPath)!;
            copy.Dispose();

            Assert.Throws<ObjectDisposedException>(() => RegistryNames.SubKeysOf(copy));
            Assert.Throws<ObjectDisposedException>(() => RegistryNames.ValuesOf(copy));
        });
    }

    private static void WithKey(Action<RegistryKey> test)
    {
        var path = $@"Software\InstallerClean.Tests\{Guid.NewGuid():N}";
        try
        {
            using var key = Registry.CurrentUser.CreateSubKey(path, writable: true)!;
            test(key);
        }
        finally
        {
            Registry.CurrentUser.DeleteSubKeyTree(path, throwOnMissingSubKey: false);
        }
    }

    // The real calls, with a subkey created on the key once its first name is read.
    private sealed class AddingAfterFirstName(
        RegistryNames.IKeyCalls inner, RegistryKey key, string added) : RegistryNames.IKeyCalls
    {
        private bool _added;

        public int QueryCounts(out int subKeys, out int values) => inner.QueryCounts(out subKeys, out values);

        public int EnumKey(int index, char[] name, ref int nameLength)
        {
            var result = inner.EnumKey(index, name, ref nameLength);
            if (!_added)
            {
                key.CreateSubKey(added)!.Dispose();
                _added = true;
            }
            return result;
        }

        public int EnumValue(int index, char[] name, ref int nameLength) => inner.EnumValue(index, name, ref nameLength);

        public void MarkHandleInvalid() => inner.MarkHandleInvalid();
    }
}
