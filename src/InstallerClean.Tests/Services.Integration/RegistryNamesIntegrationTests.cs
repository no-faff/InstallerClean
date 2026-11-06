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
    public void A_subkey_created_past_the_listing_is_in_the_listing()
    {
        WithKey(key =>
        {
            key.CreateSubKey("a")!.Dispose();
            key.CreateSubKey("b")!.Dispose();

            // Windows keeps a key's subkeys in name order, so one named to sort last is
            // at an index the listing has not yet reached.
            var calls = new Writing(RegistryNames.CallsOn(key.Handle),
                (listing, index) => { if (listing == 1 && index == 1) key.CreateSubKey("zz-added")!.Dispose(); });

            Assert.Equal(new[] { "a", "b", "zz-added" }, RegistryNames.SubKeys(calls));
        });
    }

    [Fact]
    public void A_subkey_created_ahead_of_the_listing_repeats_a_name_and_the_settled_names_are_given()
    {
        WithKey(key =>
        {
            key.CreateSubKey("b")!.Dispose();
            key.CreateSubKey("c")!.Dispose();
            Action<int, int> write = (listing, index) =>
            {
                if (listing == 1 && index == 1) key.CreateSubKey("a-added")!.Dispose();
            };

            // One listing alone: "b" is read again at index 1, and "a-added" not at all.
            var once = RegistryNames.SubKeys(new Writing(RegistryNames.CallsOn(key.Handle), write));
            Assert.Equal(new[] { "b", "b", "c" }, once);
            key.DeleteSubKey("a-added");

            Assert.Equal(new[] { "a-added", "b", "c" },
                RegistryNames.SubKeysUnchanged(new Writing(RegistryNames.CallsOn(key.Handle), write)));
        });
    }

    [Fact]
    public void A_subkey_deleted_ahead_of_the_listing_skips_a_name_and_the_settled_names_are_given()
    {
        WithKey(key =>
        {
            foreach (var name in new[] { "a", "b", "c" }) key.CreateSubKey(name)!.Dispose();
            Action<int, int> write = (listing, index) =>
            {
                if (listing == 1 && index == 2) key.DeleteSubKey("a");
            };

            // One listing alone: index 2 is past the end once "a" goes, so "c" is not read.
            var once = RegistryNames.SubKeys(new Writing(RegistryNames.CallsOn(key.Handle), write));
            Assert.Equal(new[] { "a", "b" }, once);
            key.CreateSubKey("a")!.Dispose();

            Assert.Equal(new[] { "b", "c" },
                RegistryNames.SubKeysUnchanged(new Writing(RegistryNames.CallsOn(key.Handle), write)));
        });
    }

    [Fact]
    public void A_value_deleted_ahead_of_the_listing_gives_the_settled_names()
    {
        WithKey(key =>
        {
            foreach (var name in new[] { "a", "b", "c" }) key.SetValue(name, 1, RegistryValueKind.DWord);
            Action<int, int> write = (listing, index) =>
            {
                if (listing == 1 && index == 2) key.DeleteValue("a");
            };

            // Windows does not document the order values are listed in after a delete, so
            // the settled names are held against its own listing of the key afterwards.
            var settled = RegistryNames.ValuesUnchanged(new Writing(RegistryNames.CallsOn(key.Handle), write));
            Assert.Equal(key.GetValueNames(), settled);
            Assert.Equal(new[] { "b", "c" }, settled.OrderBy(n => n, StringComparer.Ordinal));
        });
    }

    [Fact]
    public void A_key_written_during_every_listing_is_unread()
    {
        WithKey(key =>
        {
            key.CreateSubKey("m")!.Dispose();
            key.CreateSubKey("n")!.Dispose();
            var calls = new Writing(RegistryNames.CallsOn(key.Handle),
                (listing, index) => { if (index == 1) key.CreateSubKey($"a{listing:D2}")!.Dispose(); });

            Assert.Throws<IOException>(() => RegistryNames.SubKeysUnchanged(calls));
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

    // The real calls, with a write made before an enumeration call. The write is given
    // which listing is under way, counted from 1 by the counts asked for, and the index
    // about to be read.
    private sealed class Writing(RegistryNames.IKeyCalls inner, Action<int, int> beforeEnum) : RegistryNames.IKeyCalls
    {
        private int _listing;

        public int QueryCounts(out int subKeys, out int values)
        {
            _listing++;
            return inner.QueryCounts(out subKeys, out values);
        }

        public int EnumKey(int index, char[] name, ref int nameLength)
        {
            beforeEnum(_listing, index);
            return inner.EnumKey(index, name, ref nameLength);
        }

        public int EnumValue(int index, char[] name, ref int nameLength)
        {
            beforeEnum(_listing, index);
            return inner.EnumValue(index, name, ref nameLength);
        }

        public void MarkHandleInvalid() => inner.MarkHandleInvalid();
    }
}
