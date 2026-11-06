using System.Runtime.InteropServices;
using System.Text;
using InstallerClean.Services;
using Microsoft.Win32;
using Microsoft.Win32.SafeHandles;

namespace InstallerClean.Tests.Services.Integration;

/// <summary>
/// What the registry fallback does with a cached-package value it cannot read as a
/// string, under either name a registration records one in: <c>LocalPackage</c>, and
/// <c>ManagedLocalPackage</c> for a per-user managed installation. Every test here
/// runs once per name, from <see cref="InstallerQueryService.CachedPackageValueNames"/>.
///
/// AN INTEGRATION TEST BECAUSE THE SUBJECT IS THE FRAMEWORK'S OWN BEHAVIOUR, not
/// this code's. <c>RegistryKey.GetValue</c> is documented as returning null for a
/// REG_NONE or a REG_LINK value "instead of the actual value", and returns a
/// REG_NONE's bytes all the same, so a fake registry would test whichever answer it
/// was written to give. Only a real key holding a real value of each type puts the
/// framework in the loop.
///
/// Writes are confined to a GUID-named key under HKCU and removed in a finally, so
/// nothing needs elevation and nothing outlives the test. HKCU rather than HKLM for
/// the same reason.
///
/// WHAT IS PINNED IS THE PROPERTY, NOT WHICH BRANCH DELIVERS IT. Whether a given
/// .NET build hands back null for a REG_NONE or hands back a byte array, a value
/// that is THERE must never be reported as a registration that simply has no
/// cached path: one is a read that failed and belongs in the failure count that
/// weighs the degraded-sources gate, and the other is an ordinary state. A subtree
/// of the first silently read as the second is a fallback reporting itself as
/// having run cleanly and found nothing to say, which is the one condition that
/// gate exists to tell apart from a healthy machine.
/// </summary>
public class LocalPackageValueTypeTests
{
    private static string TestKeyPath => $@"Software\InstallerCleanTests\{Guid.NewGuid():N}";

    /// <summary>Every name the fallback reads, one row each.</summary>
    public static TheoryData<string> ValueNames() =>
        new(InstallerQueryService.CachedPackageValueNames);

    [Theory]
    [MemberData(nameof(ValueNames))]
    public void A_value_stored_as_REG_NONE_is_a_failed_read_and_not_an_absence(string valueName)
    {
        WithTestKey(key =>
        {
            key.SetValue(valueName, Array.Empty<byte>(), RegistryValueKind.None);

            var read = InstallerQueryService.TryReadLocalPackage(key, valueName, out var path);

            Assert.False(read);
            Assert.Null(path);
        });
    }

    [Theory]
    [MemberData(nameof(ValueNames))]
    public void A_key_with_no_such_value_at_all_is_an_absence_and_not_a_failure(string valueName)
    {
        // The must-fail control for the test above. Without it, a reader that
        // reported failure for every key would pass that one, and an app that
        // counted every advertised product as a broken registration would look
        // exactly like a correct one.
        WithTestKey(key =>
        {
            key.SetValue("SomethingElse", "x", RegistryValueKind.String);

            var read = InstallerQueryService.TryReadLocalPackage(key, valueName, out var path);

            Assert.True(read);
            Assert.Null(path);
        });
    }

    [Theory]
    [MemberData(nameof(ValueNames))]
    public void An_ordinary_string_value_is_read(string valueName)
    {
        WithTestKey(key =>
        {
            key.SetValue(valueName, @"C:\Windows\Installer\9f05cba.msi", RegistryValueKind.String);

            var read = InstallerQueryService.TryReadLocalPackage(key, valueName, out var path);

            Assert.True(read);
            Assert.Equal(@"C:\Windows\Installer\9f05cba.msi", path);
        });
    }

    [Theory]
    [MemberData(nameof(ValueNames))]
    public void A_value_stored_as_a_number_is_a_failed_read(string valueName)
    {
        // The shape the string cast catches, pinned apart from the REG_NONE case so a
        // change to the null handling cannot quietly drop it.
        WithTestKey(key =>
        {
            key.SetValue(valueName, 42, RegistryValueKind.DWord);

            var read = InstallerQueryService.TryReadLocalPackage(key, valueName, out var path);

            Assert.False(read);
            Assert.Null(path);
        });
    }

    [Theory]
    [MemberData(nameof(ValueNames))]
    public void The_value_name_is_matched_without_regard_to_case(string valueName)
    {
        // Registry value names are case-insensitive, so a key holding
        // "localpackage" holds a LocalPackage. A case-sensitive presence test
        // would send this one down the absence path. The value is a REG_LINK,
        // which GetValue hands back as null, so only the presence test sees it.
        WithTestKey(key =>
        {
            SetLink(key, valueName.ToLowerInvariant());

            var read = InstallerQueryService.TryReadLocalPackage(key, valueName, out _);

            Assert.False(read);
        });
    }

    [Theory]
    [MemberData(nameof(ValueNames))]
    public void A_value_stored_as_REG_LINK_is_a_failed_read_and_not_an_absence(string valueName)
    {
        WithTestKey(key =>
        {
            SetLink(key, valueName);
            Assert.Null(key.GetValue(valueName));

            var read = InstallerQueryService.TryReadLocalPackage(key, valueName, out var path);

            Assert.False(read);
            Assert.Null(path);
        });
    }

    // RegistryKey.SetValue writes no REG_LINK, so the value goes in through Windows
    // directly: a path as UTF-16 without a terminator, as a symbolic link's target is
    // stored.
    private static void SetLink(RegistryKey key, string valueName)
    {
        const int REG_LINK = 6;
        var data = Encoding.Unicode.GetBytes(@"\Registry\Machine\Software");
        var result = RegSetValueEx(key.Handle, valueName, 0, REG_LINK, data, data.Length);
        Assert.Equal(0, result);
    }

    [DllImport("advapi32.dll", EntryPoint = "RegSetValueExW", CharSet = CharSet.Unicode, ExactSpelling = true)]
    private static extern int RegSetValueEx(
        SafeRegistryHandle hKey, string lpValueName, int reserved, int dwType, byte[] lpData, int cbData);

    [Fact]
    public void Each_name_answers_for_its_own_value_and_not_the_other()
    {
        // The presence test matches the name it was asked for. A key whose
        // LocalPackage is present and unreadable holds no ManagedLocalPackage at
        // all, so asking for the second is an absence; and a readable
        // ManagedLocalPackage is not handed back as the LocalPackage.
        WithTestKey(key =>
        {
            key.SetValue("LocalPackage", Array.Empty<byte>(), RegistryValueKind.None);
            key.SetValue("ManagedLocalPackage", @"C:\Windows\Installer\1a2b3c.msi", RegistryValueKind.String);

            Assert.False(InstallerQueryService.TryReadLocalPackage(key, "LocalPackage", out var local));
            Assert.Null(local);
            Assert.True(InstallerQueryService.TryReadLocalPackage(key, "ManagedLocalPackage", out var managed));
            Assert.Equal(@"C:\Windows\Installer\1a2b3c.msi", managed);
        });
    }

    [Theory]
    [MemberData(nameof(ValueNames))]
    public void A_null_key_is_an_absence(string valueName)
    {
        var read = InstallerQueryService.TryReadLocalPackage(null, valueName, out var path);

        Assert.True(read);
        Assert.Null(path);
    }

    /// <summary>
    /// Creates a throwaway HKCU key, runs the body against it, and deletes the key
    /// and its parent whatever happens. The parent is removed only when this test
    /// run created it and left it empty, so a concurrent run cannot delete another
    /// one's key out from under it.
    /// </summary>
    private static void WithTestKey(Action<RegistryKey> body)
    {
        var path = TestKeyPath;
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
