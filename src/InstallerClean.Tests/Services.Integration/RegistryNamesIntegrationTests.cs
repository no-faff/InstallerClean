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
    public void A_listing_runs_past_the_count_Windows_gave_first()
    {
        WithKey(key =>
        {
            key.CreateSubKey("a")!.Dispose();
            key.CreateSubKey("b")!.Dispose();

            // The count is two when the listing starts, and a third subkey is created before
            // index 2 is asked for. Whatever index Windows gives the new subkey, index 2 then
            // names a subkey of the key, so the listing reads three names past a count of two.
            // The registry lists subkeys in name order, an order Microsoft does not document,
            // which puts "zz-added" at index 2 itself.
            var calls = new Writing(RegistryNames.CallsOn(key.Handle),
                (listing, index) => { if (listing == 1 && index == 2) key.CreateSubKey("zz-added")!.Dispose(); });

            var listed = RegistryNames.SubKeys(calls);
            Assert.Equal(3, listed.Length);
            Assert.All(listed, name => Assert.Contains(name, key.GetSubKeyNames()));
            Assert.Equal(new[] { "a", "b", "zz-added" }, listed.OrderBy(n => n, StringComparer.Ordinal));
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

            // One listing alone: the registry lists subkeys in name order, an order Microsoft
            // does not document, so "a-added" takes index 0 and moves "b" to index 1, where it
            // is read again, and "a-added" is not read at all.
            var once = RegistryNames.SubKeys(new Writing(RegistryNames.CallsOn(key.Handle), write));
            Assert.Equal(new[] { "b", "b", "c" }, once);
            key.DeleteSubKey("a-added");

            // The settled names are the framework's own listing of the unchanged key afterwards,
            // and the names the key holds.
            var settled = RegistryNames.SubKeysUnchanged(new Writing(RegistryNames.CallsOn(key.Handle), write), _ => { });
            Assert.Equal(key.GetSubKeyNames(), settled);
            Assert.Equal(new[] { "a-added", "b", "c" }, settled.OrderBy(n => n, StringComparer.Ordinal));
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

            // One listing alone: the registry lists subkeys in name order, an order Microsoft
            // does not document, so "a" and "b" are read at indexes 0 and 1, index 2 is past the
            // end once "a" goes, and "c" is not read.
            var once = RegistryNames.SubKeys(new Writing(RegistryNames.CallsOn(key.Handle), write));
            Assert.Equal(new[] { "a", "b" }, once);
            key.CreateSubKey("a")!.Dispose();

            // The settled names are the framework's own listing of the unchanged key afterwards,
            // and the names the key holds.
            var settled = RegistryNames.SubKeysUnchanged(new Writing(RegistryNames.CallsOn(key.Handle), write), _ => { });
            Assert.Equal(key.GetSubKeyNames(), settled);
            Assert.Equal(new[] { "b", "c" }, settled.OrderBy(n => n, StringComparer.Ordinal));
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
