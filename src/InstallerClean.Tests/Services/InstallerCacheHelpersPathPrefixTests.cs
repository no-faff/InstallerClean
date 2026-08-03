using InstallerClean.Services;

namespace InstallerClean.Tests.Services;

/// <summary>
/// <see cref="InstallerCacheHelpers.StripLongPathPrefix"/>, which is pure string
/// work and needs no filesystem. It runs on two very different inputs: what
/// GetFinalPathNameByHandle hands back (a drive-letter or UNC form, always), and
/// a registered LocalPackage value, which nothing obliges to be either. The
/// second is why the drive-letter condition exists, and these pin both.
/// </summary>
public class InstallerCacheHelpersPathPrefixTests
{
    [Theory]
    // The kernel's own two forms, which the resolver feeds straight back in.
    [InlineData(@"\\?\C:\Windows\Installer\9f05cba.msi", @"C:\Windows\Installer\9f05cba.msi")]
    [InlineData(@"\\?\UNC\server\share\9f05cba.msi", @"\\server\share\9f05cba.msi")]
    // The NT object form, which reaches this only from a registered value.
    [InlineData(@"\??\C:\Windows\Installer\9f05cba.msi", @"C:\Windows\Installer\9f05cba.msi")]
    [InlineData(@"\??\UNC\server\share\9f05cba.msi", @"\\server\share\9f05cba.msi")]
    // Lower case, because the prefix is matched Ordinal and the drive letter is
    // the only part of it that varies.
    [InlineData(@"\\?\d:\windows\installer\9f05cba.msi", @"d:\windows\installer\9f05cba.msi")]
    public void A_prefix_over_a_drive_letter_or_a_UNC_share_comes_off(string value, string expected) =>
        Assert.Equal(expected, InstallerCacheHelpers.StripLongPathPrefix(value));

    [Theory]
    // The form that made this a bug rather than a tidy-up: stripped blind, what
    // is left has no root, and GetFullPath resolves it against the process
    // working directory, so the GUI and the command line answer differently for
    // one registration.
    [InlineData(@"\\?\Volume{9c3a1d2e-0000-0000-0000-100000000000}\Windows\Installer\9f05cba.msi")]
    [InlineData(@"\??\Volume{9c3a1d2e-0000-0000-0000-100000000000}\Windows\Installer\9f05cba.msi")]
    // The other device forms that reach the same nowhere.
    [InlineData(@"\\?\GLOBALROOT\Device\HarddiskVolume3\Windows\Installer\9f05cba.msi")]
    [InlineData(@"\\?\HarddiskVolume3\Windows\Installer\9f05cba.msi")]
    // A prefix and nothing behind it, and a prefix over a letter with no colon.
    [InlineData(@"\\?\")]
    [InlineData(@"\\?\C")]
    [InlineData(@"\??\1:\Windows\Installer\9f05cba.msi")]
    public void A_prefix_over_anything_else_is_left_whole(string value) =>
        Assert.Equal(value, InstallerCacheHelpers.StripLongPathPrefix(value));

    [Theory]
    [InlineData(@"C:\Windows\Installer\9f05cba.msi")]
    [InlineData(@"\\server\share\9f05cba.msi")]
    [InlineData(@"\Windows\Installer\9f05cba.msi")]
    [InlineData("")]
    public void A_path_carrying_no_prefix_is_returned_as_it_arrived(string value) =>
        Assert.Equal(value, InstallerCacheHelpers.StripLongPathPrefix(value));

    /// <summary>
    /// The property the drive-letter condition exists for, asserted as a property
    /// rather than case by case: whatever comes back is either the input
    /// untouched or a path that still says which volume it is on. A result that
    /// is neither is the fault, because its meaning then depends on where the
    /// process was started from.
    /// </summary>
    [Theory]
    [InlineData(@"\\?\C:\Windows\Installer\9f05cba.msi")]
    [InlineData(@"\\?\UNC\server\share\9f05cba.msi")]
    [InlineData(@"\??\C:\Windows\Installer\9f05cba.msi")]
    [InlineData(@"\\?\Volume{9c3a1d2e-0000-0000-0000-100000000000}\Windows\Installer\9f05cba.msi")]
    [InlineData(@"\\?\GLOBALROOT\Device\HarddiskVolume3\Windows\Installer\9f05cba.msi")]
    [InlineData(@"\??\Volume{9c3a1d2e-0000-0000-0000-100000000000}\Windows\Installer\9f05cba.msi")]
    public void The_result_is_never_a_path_the_working_directory_would_complete(string value)
    {
        var stripped = InstallerCacheHelpers.StripLongPathPrefix(value);

        Assert.True(stripped == value || Path.IsPathFullyQualified(stripped),
            $"'{value}' came back as '{stripped}', which names a different file per working directory.");
    }
}
