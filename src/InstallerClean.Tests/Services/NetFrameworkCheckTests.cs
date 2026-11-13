using System.Globalization;
using InstallerClean.Helpers;
using InstallerClean.Resources;
using InstallerClean.Services;

namespace InstallerClean.Tests.Services;

/// <summary>
/// What a copy says at its start on a PC without .NET Framework 4.8, read from the Release
/// value 4.8 writes, and that it says nothing where the PC has it.
/// </summary>
public class NetFrameworkCheckTests
{
    private static RegistryDwordRead Release(RegistryDwordState state, int value = 0) => new(state, value);

    [Theory]
    [InlineData(528040, true)]
    [InlineData(533320, true)]
    [InlineData(528040, false)]
    public void A_pc_with_net_framework_48_starts_the_copy(int release, bool commandLine)
    {
        Assert.Null(NetFrameworkCheck.RefusalMessage(Release(RegistryDwordState.Read, release), commandLine));
    }

    [Theory]
    [InlineData(RegistryDwordState.Read, 528039)]
    [InlineData(RegistryDwordState.Read, 461808)]
    [InlineData(RegistryDwordState.Absent, 0)]
    public void A_pc_without_it_is_told_what_to_install_and_where(RegistryDwordState state, int release)
    {
        var window = NetFrameworkCheck.RefusalMessage(Release(state, release), commandLine: false);
        var commandLine = NetFrameworkCheck.RefusalMessage(Release(state, release), commandLine: true);

        var link = string.Format(Strings.Cli_UpdateDownloadLink,
            Strings.UpdateNotice_DownloadNetFramework48, UpdateManifest.NetFramework48DownloadUrl);
        Assert.Equal(Strings.Cli_NeedsNetFramework48 + Environment.NewLine + link, commandLine);
        Assert.Equal(Strings.Startup_NeedsNetFramework48 + Environment.NewLine + Environment.NewLine
            + Strings.UpdateNotice_DownloadNetFramework48 + ":" + Environment.NewLine
            + "https://go.microsoft.com/fwlink/?linkid=2088631", window);
    }

    [Theory]
    [InlineData(RegistryDwordState.Unreadable)]
    [InlineData(RegistryDwordState.WrongType)]
    public void A_release_value_that_cannot_be_read_starts_the_copy(RegistryDwordState state)
    {
        Assert.Null(NetFrameworkCheck.RefusalMessage(Release(state), commandLine: false));
        Assert.Null(NetFrameworkCheck.RefusalMessage(Release(state), commandLine: true));
    }

    // In each language the window's link line is that language's Cli.UpdateDownloadLink
    // with the address taken off its end, and the address stands alone on the last line.
    public static TheoryData<string> Languages()
    {
        var data = new TheoryData<string>();
        foreach (var culture in SupportedLanguages.CultureNames) data.Add(culture);
        return data;
    }

    [Theory]
    [MemberData(nameof(Languages))]
    public void The_window_gives_the_address_a_line_of_its_own_in_every_language(string culture)
    {
        var before = CultureInfo.CurrentUICulture;
        try
        {
            CultureInfo.CurrentUICulture = CultureInfo.GetCultureInfo(culture);
            var url = UpdateManifest.NetFramework48DownloadUrl;
            var full = string.Format(Strings.Cli_UpdateDownloadLink, Strings.UpdateNotice_DownloadNetFramework48, url);
            Assert.EndsWith(url, full, StringComparison.Ordinal);
            var name = full.Substring(0, full.Length - url.Length).TrimEnd();
            Assert.StartsWith(Strings.UpdateNotice_DownloadNetFramework48, name, StringComparison.Ordinal);

            var lines = NetFrameworkCheck.RefusalMessage(Release(RegistryDwordState.Absent), commandLine: false)!
                .Split(new[] { Environment.NewLine }, StringSplitOptions.None);

            Assert.Equal(new[] { Strings.Startup_NeedsNetFramework48, "", name, url }, lines);
        }
        finally
        {
            CultureInfo.CurrentUICulture = before;
        }
    }
}
