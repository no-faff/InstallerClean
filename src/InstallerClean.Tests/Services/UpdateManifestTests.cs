using System.Text;
using InstallerClean.Services;
using Microsoft.Win32;
using NSubstitute;

namespace InstallerClean.Tests.Services;

/// <summary>
/// What a release's update.json answers a copy about to update to it. These read no
/// file and no network, so they run on any host.
/// </summary>
public class UpdateManifestTests
{
    private static byte[] Json(string text) => Encoding.UTF8.GetBytes(text);

    private static byte[] Requiring(string key, string requires) => Json(
        "{\"schema\": 1, \"downloads\": {\"" + key + "\": {\"requires\": [" + requires + "]}}}");

    private static readonly Func<bool> Has48 = () => true;
    private static readonly Func<bool> Lacks48 = () => false;

    [Theory]
    [InlineData(AppCopyKind.Setup, "setup")]
    [InlineData(AppCopyKind.Portable, "portable")]
    [InlineData(AppCopyKind.CommandLineDownload, "cli")]
    public void A_download_listed_with_nothing_required_may_be_installed(AppCopyKind kind, string key) =>
        Assert.Null(UpdateManifest.Judge(Requiring(key, ""), "3.6.0", kind, Lacks48));

    [Fact]
    public void Each_kind_with_a_download_has_a_key_and_no_other_does()
    {
        foreach (var kind in Enum.GetValues<AppCopyKind>())
            Assert.Equal(AppCopy.CanUpdateItself(kind), UpdateManifest.DownloadKey(kind) is not null);
    }

    [Fact]
    public void Net_Framework_48_is_required_only_where_the_PC_lacks_it()
    {
        var file = Requiring("portable", "\"netfx-4.8\"");

        Assert.Null(UpdateManifest.Judge(file, "3.6.0", AppCopyKind.Portable, Has48));
        Assert.Equal(UpdateRefusal.NeedsNetFramework48,
            UpdateManifest.Judge(file, "3.6.0", AppCopyKind.Portable, Lacks48));
    }

    [Theory]
    [InlineData("\"dotnet-desktop-11\"")]
    [InlineData("\"netfx-4.8\", \"dotnet-desktop-11\"")]
    [InlineData("\"dotnet-desktop-11\", \"netfx-4.8\"")]
    [InlineData("4.8")]
    public void A_requirement_this_copy_does_not_know_refuses_it_whatever_else_is_met(string requires) =>
        Assert.Equal(UpdateRefusal.CannotInstallFromThisCopy,
            UpdateManifest.Judge(Requiring("portable", requires), "3.6.0", AppCopyKind.Portable, Has48));

    [Theory]
    [InlineData("""{"schema": 2, "downloads": {"portable": {"requires": []}}}""")]
    [InlineData("""{"schema": "1", "downloads": {"portable": {"requires": []}}}""")]
    [InlineData("""{"downloads": {"portable": {"requires": []}}}""")]
    [InlineData("""{"schema": 1, "downloads": {"slim": {"requires": []}}}""")]
    [InlineData("""{"schema": 1, "downloads": {"portable": {}}}""")]
    [InlineData("""{"schema": 1, "downloads": {"portable": {"requires": "netfx-4.8"}}}""")]
    [InlineData("""{"schema": 1, "downloads": []}""")]
    [InlineData("""[]""")]
    [InlineData("""{"schema": 1, "downloads": {"portable": {"requires": []}""")]
    [InlineData("not json")]
    [InlineData("")]
    public void A_file_this_copy_cannot_act_on_refuses_it(string text) =>
        Assert.Equal(UpdateRefusal.CannotInstallFromThisCopy,
            UpdateManifest.Judge(Json(text), "3.6.0", AppCopyKind.Portable, Has48));

    [Fact]
    public void Fields_this_schema_does_not_name_are_passed_over() =>
        Assert.Null(UpdateManifest.Judge(Json(
            """{"schema": 1, "note": "x", "downloads": {"portable": {"requires": [], "size": 5}, "arm64": {}}}"""),
            "3.6.0", AppCopyKind.Portable, Has48));

    [Theory]
    [InlineData("3.4.0")]
    [InlineData("3.5.0")]
    [InlineData("3.5")]
    public void A_release_at_or_below_the_last_version_without_the_file_may_have_none(string version) =>
        Assert.Null(UpdateManifest.Judge(null, version, AppCopyKind.Portable, Has48));

    [Theory]
    [InlineData("3.5.1")]
    [InlineData("3.6.0")]
    [InlineData("4.0.0")]
    [InlineData("not a version")]
    public void A_later_release_without_the_file_is_refused(string version) =>
        Assert.Equal(UpdateRefusal.CannotInstallFromThisCopy,
            UpdateManifest.Judge(null, version, AppCopyKind.Portable, Has48));

    [Fact]
    public void A_copy_with_no_download_is_refused()
    {
        foreach (var kind in new[] { AppCopyKind.FromSource, AppCopyKind.Scoop, AppCopyKind.Undetermined })
            Assert.Equal(UpdateRefusal.CannotInstallFromThisCopy,
                UpdateManifest.Judge(Requiring("portable", ""), "3.6.0", kind, Has48));
    }

    [Theory]
    [InlineData(RegistryDwordState.Read, 528040, true)]
    [InlineData(RegistryDwordState.Read, 533320, true)]
    [InlineData(RegistryDwordState.Read, 528039, false)]
    [InlineData(RegistryDwordState.Absent, 0, false)]
    [InlineData(RegistryDwordState.Unreadable, 0, false)]
    [InlineData(RegistryDwordState.WrongType, 0, false)]
    public void Net_Framework_48_is_read_from_the_release_value(RegistryDwordState state, int value, bool has)
    {
        var registry = Substitute.For<IRegistryReader>();
        registry.LocalMachineDwordValue(
                @"SOFTWARE\Microsoft\NET Framework Setup\NDP\v4\Full", "Release")
            .Returns(new RegistryDwordRead(state, value));

        Assert.Equal(has, UpdateManifest.HasNetFramework48(registry));
    }

    [Fact]
    public void The_file_sits_beside_the_releases_downloads() =>
        Assert.Equal(new Uri("https://github.com/no-faff/InstallerClean/releases/download/v3.6.0/update.json"),
            UpdateManifest.Address("3.6.0"));

    [Theory]
    [InlineData("{}", true)]
    [InlineData("[1, 2]", true)]
    [InlineData("<html>Sign in</html>", false)]
    [InlineData("", false)]
    public void Only_a_body_that_parses_as_JSON_is_JSON(string body, bool json) =>
        Assert.Equal(json, UpdateManifest.IsJson(Json(body)));
}
