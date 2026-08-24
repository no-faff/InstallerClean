using InstallerClean.Helpers;

namespace InstallerClean.Tests.Helpers;

/// <summary>
/// The spelling test that decides whether a destination is a share.
/// </summary>
/// <remarks>
/// It carries two loads and the second is the one a reader is likely to miss.
/// A share is not a local volume, so it cannot be the one the installer cache
/// is on: that is true on its own terms and needs no query. And because it needs
/// no query, it is also what keeps a path Win32 would validate OVER THE NETWORK
/// away from a caller that must not block, which matters most on the Move
/// button's tooltip.
///
/// SO A CASE THAT WRONGLY READS AS LOCAL IS NOT A COSMETIC MISS. It sends a
/// share to GetVolumePathName, which the documentation says validates it as a
/// remote drive that exists and the current user can access. Both spellings and
/// both device prefixes are pinned below for that reason.
/// </remarks>
public class StorageHelpersRemotePathTests
{
    [Theory]
    [InlineData(@"\\server\backup")]
    // Windows accepts either separator and the destination comes out of a
    // TextBox, so a test written against backslashes alone would pass while the
    // forward-slash spelling went to the network on the dispatcher.
    [InlineData("//server/backup")]
    [InlineData("//server\\backup")]
    // A share wearing the long-path prefix is still a share, and Win32 parses it
    // as one: GetVolumePathName's own example table has \\?\UNC\W:\Windows
    // failing because the share could not be reached.
    [InlineData(@"\\?\UNC\server\backup")]
    [InlineData("//?/UNC/server/backup")]
    [InlineData(@"\\?\unc\server\backup")]
    [InlineData(@"\\.\UNC\server\backup")]
    public void IsRemotePath_sees_a_share_however_it_is_spelled(string path) =>
        Assert.True(StorageHelpers.IsRemotePath(path));

    [Theory]
    [InlineData("")]
    [InlineData(@"C:\backup")]
    [InlineData(@"D:\backup\monthly")]
    [InlineData("backup")]
    // The device prefixes are the trap: they open with the same two separators a
    // share does, and what follows them is what decides. These are ordinary
    // local paths wearing a prefix and must not be turned away, or a long-path
    // destination on the system volume would stop being recognised as one and
    // the Move would be refused for want of space it does not need.
    [InlineData(@"\\?\C:\backup")]
    [InlineData(@"\\.\C:\backup")]
    [InlineData(@"\\?\Volume{00000000-0000-0000-0000-000000000000}\backup")]
    public void IsRemotePath_leaves_a_local_path_alone(string path) =>
        Assert.False(StorageHelpers.IsRemotePath(path));
}
