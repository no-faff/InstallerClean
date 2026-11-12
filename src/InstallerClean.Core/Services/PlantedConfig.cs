using System.Text.RegularExpressions;
using InstallerClean.Compatibility;
using InstallerClean.Helpers;
using InstallerClean.Resources;

namespace InstallerClean.Services;

/// <summary>
/// Decides whether a copy must refuse to start because a <c>.exe.config</c> file sits
/// beside it that InstallerClean's own build did not write.
/// </summary>
/// <remarks>
/// On .NET Framework the runtime reads a loose <c>&lt;exe&gt;.config</c> from the exe's own
/// folder, and its <c>system.net</c> and <c>system.diagnostics</c> sections can name a type
/// that loads code at the first network request, after the app's own code is running. The
/// build writes a config beside each exe that names the runtime and nothing else, and the
/// setup installs that one; the portable and command-line downloads ship the exe alone. A
/// config that says anything more is a file somebody else wrote, perhaps a drive-by download
/// beside a copy in the Downloads folder, and the copy refuses to start rather than reach
/// that first request. The setup's copy sits in the folder the setup recorded, which a
/// drive-by cannot write, so a config there is never refused.
///
/// This is read in <c>Main</c> or the window's start, never in a module initializer: the
/// read reaches System.IO and the registry, and the first such call in a module initializer
/// fixes the runtime's compatibility defaults at .NET Framework 4.0. By <c>Main</c> the
/// domain knows its target framework, so the read is safe.
/// </remarks>
internal static class PlantedConfig
{
    // The whole of a config the build writes, once the whitespace between its tags is gone:
    // the XML declaration, then the runtime the exe asks for and, in the merged exe, the
    // empty runtime section ILRepack leaves.
    private const string Declaration = @"(?:<\?xml version=""1\.0"" encoding=""utf-8""\?>)?";
    private const string Startup =
        @"<startup><supportedRuntime version=""v4\.0"" sku=""\.NETFramework,Version=v4\.8"" ?/></startup>";

    // The binding redirects the build adds where the exe's packages sit beside it as
    // assemblies of their own: in a build from source, never in the merged exe. Each names
    // an assembly and the version every request for it is sent to, and nothing else.
    private const string Redirect =
        @"<assemblyBinding xmlns=""urn:schemas-microsoft-com:asm\.v1""><dependentAssembly>"
        + @"<assemblyIdentity name=""[A-Za-z0-9.]+"" publicKeyToken=""[0-9a-f]{16}"" culture=""neutral"" ?/>"
        + @"<bindingRedirect oldVersion=""[0-9.]+-[0-9.]+"" newVersion=""[0-9.]+"" ?/>"
        + @"</dependentAssembly></assemblyBinding>";

    private static readonly Regex MergedShape = new(
        "^" + Declaration + "<configuration>" + Startup + "(?:<runtime ?/>)?</configuration>\\z",
        RegexOptions.CultureInvariant);

    private static readonly Regex UnmergedShape = new(
        "^" + Declaration + "<configuration>(?:" + Startup + ")?"
        + "(?:<runtime ?/>|<runtime>(?:" + Redirect + ")*</runtime>)?</configuration>\\z",
        RegexOptions.CultureInvariant);

    // XML's own whitespace, between one tag and the next or around the whole file. A no-break
    // space or any other character there is text the runtime reads, and does not match.
    private static readonly Regex BetweenTags = new(@"(?<=>)[ \t\r\n]+(?=<)", RegexOptions.CultureInvariant);

    /// <summary>
    /// Whether <paramref name="text"/> is a config InstallerClean's build writes: the
    /// runtime the exe asks for and, where <paramref name="merged"/> is false, binding
    /// redirects of the shape the build adds for packages beside the exe. Matched as text,
    /// character for character once the whitespace between tags is gone, so a comment, an
    /// entity, another attribute or another section is not the build's. No XML reader reads
    /// it: on .NET Framework that reader consults the config system, which reads this file.
    /// </summary>
    internal static bool IsTheBuildsOwn(string text, bool merged)
    {
        var bare = BetweenTags.Replace(text.Trim(' ', '\t', '\r', '\n'), string.Empty);
        return (merged ? MergedShape : UnmergedShape).IsMatch(bare);
    }

    /// <summary>
    /// Whether the copy running from <paramref name="exeFolder"/> must refuse to start. True
    /// where the text of the config beside the copy (<paramref name="configText"/>, null
    /// where it could not be read) is not one the build writes (<see cref="IsTheBuildsOwn"/>),
    /// and the copy is not the one the setup installed (<paramref name="setup"/>).
    /// </summary>
    internal static bool RefuseToStart(string? configText, bool merged, string exeFolder, SetupRecord setup)
    {
        if (configText is not null && IsTheBuildsOwn(configText, merged)) return false;
        return !AppCopy.IsSetupFolder(exeFolder, setup);
    }

    /// <summary>
    /// The message to show the user and then end the process, with the config file's name
    /// and its full path filled in, or null where the copy may start. Each host calls this
    /// from its start, never from a module initializer: it reads the config path the runtime
    /// bound and the file there, which reach System.IO and the registry. The window shows it
    /// under <see cref="Strings"/>' <c>Startup.ErrorTitle</c> and the command line prints it.
    /// </summary>
    /// <remarks>
    /// The runtime names the config as the exe's path with <c>.Config</c> after it, so the
    /// name shown is read from the folder, spelled as it is on disk. A config that is there
    /// and cannot be read is refused, its text being unknown. The exe is merged where this
    /// class sits in an assembly other than Core's own, which is every copy a user downloads
    /// or installs: the merge names its output after the exe.
    /// </remarks>
    internal static string? RefusalMessage(string? exePath, IRegistryReader registry)
    {
        var configPath = AppDomain.CurrentDomain.SetupInformation.ConfigurationFile;
        if (string.IsNullOrEmpty(configPath) || !FrameworkFile.Exists(configPath)) return null;

        var configText = ReadText(configPath);
        var merged = !string.Equals(
            typeof(PlantedConfig).Assembly.GetName().Name, "InstallerClean.Core", StringComparison.Ordinal);
        var exeFolder = string.IsNullOrEmpty(exePath) ? string.Empty : PathText.GetDirectoryName(exePath) ?? string.Empty;
        if (!RefuseToStart(configText, merged, exeFolder, AppCopy.ReadSetupRecord(registry))) return null;

        var shown = NameOnDisk(configPath);
        return string.Format(Strings.Get("Startup.PlantedConfigRefusal"), PathText.GetFileName(shown), shown);
    }

    /// <summary>
    /// The text of the config at <paramref name="configPath"/>, decoded as UTF-8 unless a
    /// byte-order mark names another encoding, with the mark taken off, as the runtime reads
    /// it; null where it cannot be read. A byte that is not UTF-8 reads as U+FFFD, which no
    /// config the build writes holds.
    /// </summary>
    internal static string? ReadText(string configPath)
    {
        try
        {
            return FrameworkFileStream.ReadAllText(configPath);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            return null;
        }
    }

    // The config's path with its file name as the folder spells it, or the runtime's spelling
    // where the folder cannot be listed.
    private static string NameOnDisk(string configPath)
    {
        try
        {
            if (PathText.GetDirectoryName(configPath) is { Length: > 0 } folder
                && FrameworkDirectory.EnumerateFiles(folder, PathText.GetFileName(configPath)).FirstOrDefault() is { } onDisk)
                return onDisk;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException) { }
        return configPath;
    }
}
