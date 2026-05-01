using InstallerClean.Helpers;

namespace InstallerClean.Tests.Helpers.Integration;

public class StorageHelpersTests
{
    [Fact]
    public void GetAvailableFreeSpace_returns_positive_for_current_drive()
    {
        var path = Path.GetTempPath();

        var result = StorageHelpers.GetAvailableFreeSpace(path);

        Assert.NotNull(result);
        Assert.True(result > 0);
    }

    [Fact]
    public void GetAvailableFreeSpace_returns_null_for_empty_path()
    {
        Assert.Null(StorageHelpers.GetAvailableFreeSpace(string.Empty));
    }

    [Fact]
    public void GetAvailableFreeSpace_returns_null_for_nonexistent_drive_letter()
    {
        // Pick a letter almost guaranteed not to be mapped
        var result = StorageHelpers.GetAvailableFreeSpace(@"Q:\nope\never");

        Assert.Null(result);
    }

    [Fact]
    public void GetAvailableFreeSpace_returns_null_for_unreachable_unc()
    {
        // Bogus UNC that will not resolve
        var result = StorageHelpers.GetAvailableFreeSpace(@"\\nonexistent-server-installerclean-test\share");

        Assert.Null(result);
    }
}
