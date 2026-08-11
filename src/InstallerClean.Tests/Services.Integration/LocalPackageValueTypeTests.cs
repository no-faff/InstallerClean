using InstallerClean.Services;
using Microsoft.Win32;

namespace InstallerClean.Tests.Services.Integration;

/// <summary>
/// What the registry fallback does with a <c>LocalPackage</c> value it cannot read
/// as a string.
///
/// AN INTEGRATION TEST BECAUSE THE SUBJECT IS THE FRAMEWORK'S OWN BEHAVIOUR, not
/// this code's. <c>RegistryKey.GetValue</c> is documented as not supporting
/// REG_NONE or REG_LINK, returning null for both "instead of the actual value", so
/// a fake registry cannot exercise the case at all: the whole defect is that a
/// PRESENT value arrives looking exactly like an absent one. Only a real key
/// holding a real REG_NONE puts the framework in the loop.
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

    [Fact]
    public void A_LocalPackage_stored_as_REG_NONE_is_a_failed_read_and_not_an_absence()
    {
        WithTestKey(key =>
        {
            key.SetValue("LocalPackage", Array.Empty<byte>(), RegistryValueKind.None);

            var read = InstallerQueryService.TryReadLocalPackage(key, out var path);

            Assert.False(read);
            Assert.Null(path);
        });
    }

    [Fact]
    public void A_key_with_no_LocalPackage_at_all_is_an_absence_and_not_a_failure()
    {
        // The must-fail control for the test above. Without it, a reader that
        // reported failure for every key would pass that one, and an app that
        // counted every advertised product as a broken registration would look
        // exactly like a correct one.
        WithTestKey(key =>
        {
            key.SetValue("SomethingElse", "x", RegistryValueKind.String);

            var read = InstallerQueryService.TryReadLocalPackage(key, out var path);

            Assert.True(read);
            Assert.Null(path);
        });
    }

    [Fact]
    public void An_ordinary_string_value_is_read()
    {
        WithTestKey(key =>
        {
            key.SetValue("LocalPackage", @"C:\Windows\Installer\9f05cba.msi", RegistryValueKind.String);

            var read = InstallerQueryService.TryReadLocalPackage(key, out var path);

            Assert.True(read);
            Assert.Equal(@"C:\Windows\Installer\9f05cba.msi", path);
        });
    }

    [Fact]
    public void A_LocalPackage_stored_as_a_number_is_a_failed_read()
    {
        // The shape the cast already caught before REG_NONE was understood. Kept
        // so a change that reworked the null handling could not quietly drop it.
        WithTestKey(key =>
        {
            key.SetValue("LocalPackage", 42, RegistryValueKind.DWord);

            var read = InstallerQueryService.TryReadLocalPackage(key, out var path);

            Assert.False(read);
            Assert.Null(path);
        });
    }

    [Fact]
    public void The_value_name_is_matched_without_regard_to_case()
    {
        // Registry value names are case-insensitive, so a key holding
        // "localpackage" holds a LocalPackage. A case-sensitive presence test
        // would send this one back down the absence path, which is the exact
        // defect being fixed, reintroduced by the fix.
        WithTestKey(key =>
        {
            key.SetValue("localpackage", Array.Empty<byte>(), RegistryValueKind.None);

            var read = InstallerQueryService.TryReadLocalPackage(key, out _);

            Assert.False(read);
        });
    }

    [Fact]
    public void A_null_key_is_an_absence()
    {
        var read = InstallerQueryService.TryReadLocalPackage(null, out var path);

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
