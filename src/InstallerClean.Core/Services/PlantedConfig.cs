using InstallerClean.Compatibility;
using InstallerClean.Helpers;
using InstallerClean.Resources;

namespace InstallerClean.Services;

/// <summary>
/// Decides whether a copy must refuse to start because a <c>.exe.config</c> file sits
/// beside it that it did not ship.
/// </summary>
/// <remarks>
/// On .NET Framework the runtime reads a loose <c>&lt;exe&gt;.config</c> from the exe's own
/// folder, and its <c>system.net</c> and <c>system.diagnostics</c> sections can name a type
/// that loads code at the first network request, after the app's own code is running. The
/// portable and command-line downloads ship no config, so a config beside either of them is
/// a file a drive-by download could have dropped there, and the copy refuses to start rather
/// than reach that first request. The setup's copy does ship a config, and sits in the
/// folder the setup recorded, which a drive-by cannot write, so it is never refused.
///
/// This is read in <c>Main</c> or the window's start, never in a module initializer: the
/// read reaches System.IO and the registry, and the first such call in a module initializer
/// fixes the runtime's compatibility defaults at .NET Framework 4.0. By <c>Main</c> the
/// domain knows its target framework, so the read is safe.
/// </remarks>
internal static class PlantedConfig
{
    /// <summary>
    /// Whether the copy running from <paramref name="exeFolder"/> must refuse to start. True
    /// where <paramref name="configExists"/> is true, <paramref name="configPath"/> names a
    /// file, and the copy is not the one the setup installed (<paramref name="setup"/>). The
    /// host reads <paramref name="configPath"/> from
    /// <c>AppDomain.CurrentDomain.SetupInformation.ConfigurationFile</c> and
    /// <paramref name="configExists"/> from the file system.
    /// </summary>
    internal static bool RefuseToStart(string? configPath, bool configExists, string exeFolder, SetupRecord setup)
    {
        if (string.IsNullOrEmpty(configPath) || !configExists) return false;
        return !AppCopy.IsSetupFolder(exeFolder, setup);
    }

    /// <summary>
    /// The message to show the user and then end the process, with the config file's name
    /// and its full path filled in, or null where the copy may start. Each host calls this
    /// from its start, never from a module initializer: it reads the config path the runtime
    /// bound and whether the file is on disk, which reach System.IO and the registry. The
    /// window shows it under <see cref="Strings"/>' <c>Startup.ErrorTitle</c> and the command
    /// line prints it.
    /// </summary>
    internal static string? RefusalMessage(string? exePath, IRegistryReader registry)
    {
        var configPath = AppDomain.CurrentDomain.SetupInformation.ConfigurationFile;
        var exeFolder = string.IsNullOrEmpty(exePath) ? string.Empty : PathText.GetDirectoryName(exePath) ?? string.Empty;
        var exists = !string.IsNullOrEmpty(configPath) && FrameworkFile.Exists(configPath);
        if (!RefuseToStart(configPath, exists, exeFolder, AppCopy.ReadSetupRecord(registry))) return null;
        return string.Format(Strings.Get("Startup.PlantedConfigRefusal"), PathText.GetFileName(configPath), configPath);
    }
}
