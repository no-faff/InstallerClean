using InstallerClean.Resources;
using InstallerClean.Services;

namespace InstallerClean.Tests.Services;

/// <summary>
/// What a copy says at its start on a PC older than Windows 10 version 1607, read from the
/// CurrentBuild value Windows keeps, and that it says nothing where the PC runs 1607 or later
/// or the build cannot be read.
/// </summary>
public class WindowsVersionCheckTests
{
    [Theory]
    [InlineData("14393")]
    [InlineData("19045")]
    [InlineData("22000")]
    [InlineData("26300")]
    public void A_pc_on_windows_10_1607_or_later_starts_the_copy(string build)
    {
        Assert.Null(WindowsVersionCheck.RefusalMessage(build));
    }

    // Windows 7 SP1, 8, 8.1, Windows 10 1507 and 1511, and the build just below 1607's.
    [Theory]
    [InlineData("7601")]
    [InlineData("9200")]
    [InlineData("9600")]
    [InlineData("10240")]
    [InlineData("10586")]
    [InlineData("14392")]
    public void A_pc_on_older_windows_is_told_which_windows_the_copy_needs(string build)
    {
        Assert.Equal(Strings.FrameworkCheck.Startup_NeedsWindows10Version1607, WindowsVersionCheck.RefusalMessage(build));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("build")]
    [InlineData(" 7601")]
    [InlineData("7601.1")]
    [InlineData("-7601")]
    [InlineData("99999999999")]
    public void A_build_that_cannot_be_read_as_a_number_starts_the_copy(string? build)
    {
        Assert.Null(WindowsVersionCheck.RefusalMessage(build));
    }
}
