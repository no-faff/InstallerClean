using InstallerClean.Resources;
using InstallerClean.Services;

namespace InstallerClean.Tests.Services;

/// <summary>
/// Which CurrentBuild values stop a copy at its start as older than Windows 10 version 1607,
/// and what it says then.
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
        Assert.False(WindowsVersionCheck.Refuses(build));
    }

    // Windows 7 SP1, 8, 8.1, Windows 10 1507 and 1511, and the build just below 1607's.
    [Theory]
    [InlineData("7601")]
    [InlineData("9200")]
    [InlineData("9600")]
    [InlineData("10240")]
    [InlineData("10586")]
    [InlineData("14392")]
    public void A_pc_on_older_windows_is_refused(string build)
    {
        Assert.True(WindowsVersionCheck.Refuses(build));
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
        Assert.False(WindowsVersionCheck.Refuses(build));
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void Both_copies_say_which_windows_they_need(bool commandLine)
    {
        Assert.Equal(Strings.FrameworkCheck.Startup_NeedsWindows10Version1607,
            StartChecks.Message(StartRefusal.OlderWindows, commandLine));
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void A_pc_without_net_framework_48_is_told_what_to_install(bool commandLine)
    {
        Assert.Equal(NetFrameworkCheck.Message(commandLine), StartChecks.Message(StartRefusal.NoNetFramework48, commandLine));
    }
}
