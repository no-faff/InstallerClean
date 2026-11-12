using Microsoft.Win32;
using NSubstitute;
using InstallerClean.Services;

namespace InstallerClean.Tests.Services;

/// <summary>
/// Whether a copy refuses to start because a .exe.config sits beside it that the build did
/// not write. The build's own config names the runtime, and in a build from source the
/// binding redirects for the packages beside the exe; any other config is refused, except
/// beside the setup's copy.
/// </summary>
public class PlantedConfigTests
{
    private const string SetupFolder = @"C:\Program Files\InstallerClean";
    private const string DownloadFolder = @"C:\Users\person\Downloads";

    // The configs the build writes, as their text reads once the byte-order mark is taken
    // off. The merged window's is ILRepack's, with the empty runtime section; the command
    // line's is the same without it, here with CRLF line endings.
    private const string MergedWindow =
        "<?xml version=\"1.0\" encoding=\"utf-8\"?>\n<configuration>\n  <startup>\n"
        + "    <supportedRuntime version=\"v4.0\" sku=\".NETFramework,Version=v4.8\" />\n  </startup>\n"
        + "  <runtime />\n</configuration>";

    private const string MergedCommandLine =
        "<?xml version=\"1.0\" encoding=\"utf-8\"?>\r\n<configuration>\r\n  <startup>\r\n"
        + "    <supportedRuntime version=\"v4.0\" sku=\".NETFramework,Version=v4.8\" />\r\n  </startup>\r\n"
        + "</configuration>\r\n";

    // The window built from source, whose packages sit beside it: two of its redirects.
    private const string WindowFromSource =
        "<?xml version=\"1.0\" encoding=\"utf-8\"?>\n<configuration>\n  <startup>\n"
        + "    <supportedRuntime version=\"v4.0\" sku=\".NETFramework,Version=v4.8\" />\n  </startup>\n"
        + "  <runtime>\n"
        + Redirects
        + "  </runtime>\n</configuration>\n";

    // The test assembly's own config, which the test host runs under: redirects and no
    // startup section.
    private const string TestHost =
        "<?xml version=\"1.0\" encoding=\"utf-8\"?>\n<configuration>\n  <runtime>\n"
        + Redirects
        + "  </runtime>\n</configuration>\n";

    private const string Redirects =
        "    <assemblyBinding xmlns=\"urn:schemas-microsoft-com:asm.v1\">\n      <dependentAssembly>\n"
        + "        <assemblyIdentity name=\"Microsoft.Bcl.AsyncInterfaces\" publicKeyToken=\"cc7b13ffcd2ddd51\" culture=\"neutral\" />\n"
        + "        <bindingRedirect oldVersion=\"0.0.0.0-10.0.0.7\" newVersion=\"10.0.0.7\" />\n"
        + "      </dependentAssembly>\n    </assemblyBinding>\n"
        + "    <assemblyBinding xmlns=\"urn:schemas-microsoft-com:asm.v1\">\n      <dependentAssembly>\n"
        + "        <assemblyIdentity name=\"System.Runtime.CompilerServices.Unsafe\" publicKeyToken=\"b03f5f7f11d50a3a\" culture=\"neutral\" />\n"
        + "        <bindingRedirect oldVersion=\"0.0.0.0-6.0.3.0\" newVersion=\"6.0.3.0\" />\n"
        + "      </dependentAssembly>\n    </assemblyBinding>\n";

    // A config that names a proxy module type, read at the first network request.
    private const string WithProxyModule =
        "<?xml version=\"1.0\" encoding=\"utf-8\"?>\n<configuration>\n  <startup>\n"
        + "    <supportedRuntime version=\"v4.0\" sku=\".NETFramework,Version=v4.8\" />\n  </startup>\n"
        + "  <system.net><defaultProxy><module type=\"Some.Module, Some\" /></defaultProxy></system.net>\n"
        + "</configuration>\n";

    // The setup's record as AppCopy.ReadSetupRecord reads a key holding installLocation, or
    // an absent key where it is null.
    private static SetupRecord RecordOf(string? installLocation) =>
        AppCopy.ReadSetupRecord(Registry(installLocation is null
            ? new RegistryKeyValues(RegistryKeyPresence.Absent)
            : new RegistryKeyValues(RegistryKeyPresence.Present,
                [new RegistryValue("InstallLocation", RegistryValueKind.String, installLocation)])));

    private static IRegistryReader Registry(RegistryKeyValues read)
    {
        var registry = Substitute.For<IRegistryReader>();
        registry.LocalMachineValues(AppCopy.SetupUninstallKeyPath, Arg.Any<CancellationToken>()).Returns(read);
        return registry;
    }

    // ---- which configs are the build's own ----

    [Theory]
    [InlineData(MergedWindow, true)]
    [InlineData(MergedCommandLine, true)]
    [InlineData(MergedWindow, false)]
    [InlineData(MergedCommandLine, false)]
    [InlineData(WindowFromSource, false)]
    [InlineData(TestHost, false)]
    public void A_config_the_build_writes_is_its_own(string text, bool merged)
    {
        Assert.True(PlantedConfig.IsTheBuildsOwn(text, merged));
    }

    [Theory]
    [InlineData(WindowFromSource)]
    [InlineData(TestHost)]
    public void A_merged_exe_takes_no_binding_redirect_as_its_own(string text)
    {
        Assert.False(PlantedConfig.IsTheBuildsOwn(text, merged: true));
    }

    public static TheoryData<string> NotTheBuilds() => new()
    {
        WithProxyModule,
        // A trace listener, read at the first trace.
        MergedCommandLine.Replace("</configuration>",
            "<system.diagnostics><trace><listeners><add name=\"x\" type=\"Some.Listener, Some\" /></listeners></trace></system.diagnostics></configuration>"),
        // A section handler of its own.
        MergedCommandLine.Replace("<configuration>",
            "<configuration><configSections><section name=\"x\" type=\"Some.Handler, Some\" /></configSections>"),
        // The merged shape with an assembly to load from a file of somebody else's choosing.
        MergedWindow.Replace("<runtime />",
            "<runtime><assemblyBinding xmlns=\"urn:schemas-microsoft-com:asm.v1\"><dependentAssembly>"
            + "<assemblyIdentity name=\"System.Memory\" publicKeyToken=\"cc7b13ffcd2ddd51\" culture=\"neutral\" />"
            + "<codeBase version=\"4.0.5.0\" href=\"file:///C:/x.dll\" /></dependentAssembly></assemblyBinding></runtime>"),
        // A redirect that also says where to load the assembly from.
        WindowFromSource.Replace("<bindingRedirect oldVersion=\"0.0.0.0-6.0.3.0\" newVersion=\"6.0.3.0\" />",
            "<bindingRedirect oldVersion=\"0.0.0.0-6.0.3.0\" newVersion=\"6.0.3.0\" /><codeBase version=\"6.0.3.0\" href=\"file:///C:/x.dll\" />"),
        // A folder to probe for assemblies.
        TestHost.Replace("<runtime>",
            "<runtime><assemblyBinding xmlns=\"urn:schemas-microsoft-com:asm.v1\"><probing privatePath=\"x\" /></assemblyBinding>"),
        // A domain manager, named in the runtime section.
        MergedWindow.Replace("<runtime />",
            "<runtime><appDomainManagerAssembly value=\"Some\" /><appDomainManagerType value=\"Some.Manager\" /></runtime>"),
        // Another attribute on the startup section.
        MergedCommandLine.Replace("<startup>", "<startup useLegacyV2RuntimeActivationPolicy=\"true\">"),
        // A comment, an entity and a no-break space between tags: text the build never writes.
        MergedCommandLine.Replace("</startup>", "</startup><!-- x -->"),
        MergedCommandLine.Replace("v4.8", "v4&#x2E;8"),
        MergedCommandLine.Replace("  <startup>", "\u00A0<startup>"),
        // Two documents' worth, and an empty file.
        MergedCommandLine + MergedCommandLine,
        "",
    };

    [Theory]
    [MemberData(nameof(NotTheBuilds))]
    public void A_config_that_says_anything_more_is_not_the_builds(string text)
    {
        Assert.False(PlantedConfig.IsTheBuildsOwn(text, merged: false));
        Assert.False(PlantedConfig.IsTheBuildsOwn(text, merged: true));
    }

    [Theory]
    [InlineData("InstallerClean.exe.config")]
    [InlineData("installerclean-cli.exe.config")]
    [InlineData("InstallerClean.Tests.dll.config")]
    public void Each_config_this_build_wrote_is_its_own(string name)
    {
        // The window's, the command line's and the test assembly's configs, as the build
        // wrote them beside the tests, each with its packages beside it as assemblies.
        var path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, name);
        Assert.True(File.Exists(path), $"No {name} beside the tests.");
        Assert.True(PlantedConfig.IsTheBuildsOwn(File.ReadAllText(path), merged: false), File.ReadAllText(path));
    }

    [Theory]
    [InlineData(true, "\r\n")]
    [InlineData(true, "\n")]
    [InlineData(false, "\r\n")]
    [InlineData(false, "\n")]
    public void A_config_the_build_writes_is_its_own_with_or_without_a_byte_order_mark_and_either_line_ending(
        bool byteOrderMark, string lineEnding)
    {
        foreach (var (text, merged) in new[] { (MergedWindow, true), (MergedCommandLine, true), (WindowFromSource, false), (TestHost, false) })
        {
            var read = WrittenAndRead(text.Replace("\r\n", "\n").Replace("\n", lineEnding), byteOrderMark);
            Assert.True(PlantedConfig.IsTheBuildsOwn(read!, merged), read);
        }
    }

    [Theory]
    [InlineData(true, "\r\n")]
    [InlineData(false, "\n")]
    public void A_config_that_says_anything_more_is_refused_with_or_without_a_byte_order_mark_and_either_line_ending(
        bool byteOrderMark, string lineEnding)
    {
        var listener = MergedWindow.Replace("<runtime />",
            "<runtime />\n  <system.diagnostics><trace><listeners><add name=\"x\" type=\"Some.Listener, Some\" /></listeners></trace></system.diagnostics>");
        var read = WrittenAndRead(listener.Replace("\n", lineEnding), byteOrderMark);
        Assert.False(PlantedConfig.IsTheBuildsOwn(read!, merged: true));
        Assert.False(PlantedConfig.IsTheBuildsOwn(read!, merged: false));
    }

    [Fact]
    public void A_config_with_two_byte_order_marks_is_not_the_builds()
    {
        var read = WrittenAndRead("\uFEFF" + MergedWindow, byteOrderMark: true);
        Assert.False(PlantedConfig.IsTheBuildsOwn(read!, merged: true));
    }

    [Fact]
    public void A_config_in_utf16_with_its_byte_order_mark_reads_as_the_same_text()
    {
        var path = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName() + ".config");
        try
        {
            File.WriteAllText(path, MergedCommandLine, new System.Text.UnicodeEncoding(bigEndian: false, byteOrderMark: true));
            Assert.Equal(MergedCommandLine, PlantedConfig.ReadText(path));
        }
        finally
        {
            File.Delete(path);
        }
    }

    // The text as RefusalMessage reads it from a file holding text in UTF-8, with or without
    // a byte-order mark.
    private static string? WrittenAndRead(string text, bool byteOrderMark)
    {
        var path = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName() + ".config");
        try
        {
            File.WriteAllText(path, text, new System.Text.UTF8Encoding(byteOrderMark));
            var read = PlantedConfig.ReadText(path);
            Assert.NotNull(read);
            Assert.False(read!.StartsWith("\uFEFF", StringComparison.Ordinal) && !text.StartsWith("\uFEFF", StringComparison.Ordinal),
                "The byte-order mark was read as text.");
            return read;
        }
        finally
        {
            File.Delete(path);
        }
    }

    // ---- whether the copy starts ----

    [Theory]
    [InlineData(MergedWindow)]
    [InlineData(MergedCommandLine)]
    public void A_download_beside_the_builds_own_config_starts(string text)
    {
        Assert.False(PlantedConfig.RefuseToStart(text, merged: true, DownloadFolder, RecordOf(SetupFolder)));
    }

    [Fact]
    public void A_build_from_source_beside_its_own_config_starts()
    {
        Assert.False(PlantedConfig.RefuseToStart(WindowFromSource, merged: false, DownloadFolder, RecordOf(null)));
    }

    [Fact]
    public void A_download_beside_another_config_refuses_the_start()
    {
        Assert.True(PlantedConfig.RefuseToStart(WithProxyModule, merged: true, DownloadFolder, RecordOf(SetupFolder)));
    }

    [Fact]
    public void A_download_beside_a_config_with_redirects_refuses_the_start()
    {
        Assert.True(PlantedConfig.RefuseToStart(WindowFromSource, merged: true, DownloadFolder, RecordOf(SetupFolder)));
    }

    [Fact]
    public void A_download_beside_a_config_that_cannot_be_read_refuses_the_start()
    {
        Assert.True(PlantedConfig.RefuseToStart(null, merged: true, DownloadFolder, RecordOf(SetupFolder)));
    }

    [Fact]
    public void A_config_beside_a_copy_where_no_setup_is_recorded_refuses_the_start()
    {
        Assert.True(PlantedConfig.RefuseToStart(WithProxyModule, merged: true, DownloadFolder, RecordOf(null)));
    }

    [Fact]
    public void Any_config_beside_the_setup_copy_lets_it_start()
    {
        Assert.False(PlantedConfig.RefuseToStart(WithProxyModule, merged: true, SetupFolder, RecordOf(SetupFolder)));
    }

    // ---- the process these tests run in ----

    [Fact]
    public void The_test_host_under_its_own_config_is_not_refused()
    {
        // The test host runs under the test assembly's config, which the build writes with
        // its binding redirects, from a folder no setup records.
        var config = AppDomain.CurrentDomain.SetupInformation.ConfigurationFile;
        Assert.True(File.Exists(config), $"The test host runs under no config file: '{config}'.");
        Assert.Null(PlantedConfig.RefusalMessage(Path.Combine(DownloadFolder, "installerclean-cli.exe"), Registry(new RegistryKeyValues(RegistryKeyPresence.Absent))));
    }
}
