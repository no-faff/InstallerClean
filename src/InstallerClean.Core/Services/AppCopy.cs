using InstallerClean.Helpers;

namespace InstallerClean.Services;

/// <summary>
/// Which kind of copy of InstallerClean is running, which decides what an update
/// downloads and how it puts the newer version in place.
/// </summary>
public enum AppCopyKind
{
    /// <summary>
    /// Built from source rather than published as a single file. It has no download
    /// of its own to replace itself with, so it offers the release page alone.
    /// </summary>
    FromSource,

    /// <summary>
    /// Installed by the setup, which winget and Chocolatey also install. The newer
    /// setup updates it.
    /// </summary>
    Setup,

    /// <summary>
    /// The portable download as Scoop installs it. Scoop updates it, so the app gives
    /// Scoop's own update command (<see cref="AppCopy.ScoopUpdateCommand"/>) in place of
    /// updating itself.
    /// </summary>
    Scoop,

    /// <summary>The portable download, which carries its own .NET runtime.</summary>
    Portable,

    /// <summary>The slim download, which runs on the .NET Desktop Runtime installed on the PC.</summary>
    Slim,

    /// <summary>
    /// The command line as its own download, outside the setup's folder. It updates by
    /// replacing its own file, as the portable does.
    /// </summary>
    CommandLineDownload,

    /// <summary>
    /// A single-file copy on a PC whose setup record could not be read, or is there and
    /// names no folder that can be compared, so whether the setup installed this copy is
    /// not known. It does not update itself, so it offers the release page alone.
    /// </summary>
    Undetermined,
}

/// <summary>What the setup's record says. See <see cref="SetupRecord"/>.</summary>
public enum SetupRecordState
{
    /// <summary>
    /// The record could not be read, or is there and names no folder that can be
    /// compared. First member, so the type's zero is the reading that established nothing.
    /// </summary>
    CannotTell,

    /// <summary>There is no record: the setup has not installed InstallerClean on this PC.</summary>
    None,

    /// <summary>The record names the setup's folder, in <see cref="SetupRecord.Folder"/>.</summary>
    Folder,
}

/// <summary>
/// The setup's record of where it installed InstallerClean, as
/// <see cref="AppCopy.ReadSetupRecord"/> reads it. Made only through <see cref="None"/>,
/// <see cref="CannotTell"/> and <see cref="At"/>, so a record in the
/// <see cref="SetupRecordState.Folder"/> state always holds a full path made absolute
/// and stripped of trailing separators, and the type's default is
/// <see cref="SetupRecordState.CannotTell"/>.
/// </summary>
public readonly record struct SetupRecord
{
    private SetupRecord(SetupRecordState state, string? folder)
    {
        State = state;
        Folder = folder;
    }

    /// <summary>What the record says.</summary>
    public SetupRecordState State { get; }

    /// <summary>The setup's folder where <see cref="State"/> is <see cref="SetupRecordState.Folder"/>, and null otherwise.</summary>
    public string? Folder { get; }

    /// <summary>There is no record.</summary>
    public static SetupRecord None => new(SetupRecordState.None, null);

    /// <summary>The record could not be read or names nothing that can be compared.</summary>
    public static SetupRecord CannotTell => new(SetupRecordState.CannotTell, null);

    /// <summary>
    /// The record naming <paramref name="installLocation"/>, or <see cref="CannotTell"/>
    /// where it is blank, not a full path, or not a path at all.
    /// </summary>
    public static SetupRecord At(string? installLocation) =>
        !string.IsNullOrWhiteSpace(installLocation)
        && PathText.IsPathFullyQualified(installLocation)
        && AppCopy.Normalise(installLocation) is { } folder
            ? new SetupRecord(SetupRecordState.Folder, folder)
            : CannotTell;
}

/// <summary>
/// Scoop's update command for a copy Scoop installed, and whether it is for a machine-wide
/// install, which is run from an administrator's terminal and has <c>--global</c> after the
/// app's name.
/// </summary>
/// <param name="Command">The command, as it is typed.</param>
/// <param name="Global">Whether Scoop installed this copy for every user of the PC.</param>
public sealed record ScoopUpdate(string Command, bool Global);

/// <summary>
/// Decides which <see cref="AppCopyKind"/> is running from facts the caller gathers,
/// and builds the download address for a kind and a version.
/// </summary>
public static class AppCopy
{
    /// <summary>
    /// The uninstall key the setup writes. Inno Setup names it after the script's AppId
    /// with "_is1" on the end, and writes InstallLocation there on every install.
    /// </summary>
    public const string SetupUninstallKeyPath =
        @"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\InstallerClean_is1";

    /// <summary>The value under <see cref="SetupUninstallKeyPath"/> naming the setup's folder.</summary>
    public const string InstallLocationValueName = "InstallLocation";

    /// <summary>
    /// The command line's file name, both in the setup's folder and as its own download,
    /// which carries no version in its name.
    /// </summary>
    public const string CommandLineFileName = "installerclean-cli.exe";

    /// <summary>
    /// The command that updates Scoop's copy where the folder it runs from does not name
    /// the app: Scoop names an app after its manifest's file, and InstallerClean's
    /// manifest in the Extras bucket is installerclean.json.
    /// <see cref="ReadScoopUpdate"/> answers the command for a folder that does.
    ///
    /// Scoop looks for a running copy by comparing each process's path with the app's
    /// folder, and stops the update where it finds one, unless its
    /// ignore_running_processes setting is on. A process whose path the shell cannot read,
    /// such as an elevated InstallerClean seen from a shell that is not elevated, is not
    /// compared. So wherever the command is shown, the line above it asks for InstallerClean
    /// to be closed before it is run.
    /// </summary>
    public const string ScoopUpdateCommand = "scoop update installerclean";

    /// <summary>The update of a Scoop copy that is not known to be machine-wide: <see cref="ScoopUpdateCommand"/>.</summary>
    public static ScoopUpdate DefaultScoopUpdate { get; } = new(ScoopUpdateCommand, Global: false);

    /// <summary>
    /// Where every download is published. A release's files sit under its tag,
    /// "v" and the version, and copies already out there build their download
    /// address the same way, so the tag and the file names in the release workflow
    /// have to keep this shape.
    /// </summary>
    internal const string DownloadRoot = "https://github.com/no-faff/InstallerClean/releases/download/";

    /// <summary>
    /// The kind of copy, in this order: a build that is not a single file is from
    /// source; a copy in the folder the setup recorded is the setup's; a copy beside
    /// the files Scoop writes into each version's folder is Scoop's; a copy on a PC
    /// whose setup record cannot tell is undetermined; otherwise the build says whether
    /// it carries its own runtime, which tells the portable from the slim.
    ///
    /// The setup installs the very file the portable download is, so only the
    /// setup's record tells those two apart, and it is asked before the build is.
    /// Scoop's two files are asked before a record that cannot tell, because the setup
    /// never writes them.
    /// </summary>
    /// <param name="isSingleFile">Whether the running build is a single-file bundle.</param>
    /// <param name="isSelfContained">Whether the build carries its own .NET runtime.</param>
    /// <param name="exeFolder">The folder the running exe is in.</param>
    /// <param name="setup">The setup's record (<see cref="ReadSetupRecord"/>).</param>
    /// <param name="fileExists">Answers whether a file exists at a path.</param>
    public static AppCopyKind Decide(
        bool isSingleFile,
        bool isSelfContained,
        string exeFolder,
        SetupRecord setup,
        Func<string, bool> fileExists)
    {
        if (!isSingleFile) return AppCopyKind.FromSource;
        if (IsSetupFolder(exeFolder, setup)) return AppCopyKind.Setup;
        if (IsScoopFolder(exeFolder, fileExists)) return AppCopyKind.Scoop;
        if (setup.State == SetupRecordState.CannotTell) return AppCopyKind.Undetermined;
        return isSelfContained ? AppCopyKind.Portable : AppCopyKind.Slim;
    }

    /// <summary>
    /// The kind of command-line copy, in this order: a build that is not a single file
    /// is from source; a copy in the folder the setup recorded is the setup's; a copy on
    /// a PC whose setup record cannot tell is undetermined; any other is the command
    /// line's own download. Scoop installs only the window, so no copy of the command
    /// line is Scoop's.
    /// </summary>
    /// <param name="isSingleFile">Whether the running build is a single-file bundle.</param>
    /// <param name="exeFolder">The folder the running exe is in.</param>
    /// <param name="setup">The setup's record (<see cref="ReadSetupRecord"/>).</param>
    public static AppCopyKind DecideCommandLine(bool isSingleFile, string exeFolder, SetupRecord setup)
    {
        if (!isSingleFile) return AppCopyKind.FromSource;
        if (IsSetupFolder(exeFolder, setup)) return AppCopyKind.Setup;
        if (setup.State == SetupRecordState.CannotTell) return AppCopyKind.Undetermined;
        return AppCopyKind.CommandLineDownload;
    }

    /// <summary>
    /// The kind of the command line running from <paramref name="exePath"/>, from this
    /// build and the setup's record.
    /// </summary>
    /// <param name="exePath">The running exe's path.</param>
    /// <param name="registry">Reads the setup's record.</param>
    public static AppCopyKind DetectRunningCommandLine(string exePath, IRegistryReader registry)
    {
#if DEBUG
        if (PretendedCommandLineKind() is { } pretended)
            return pretended;
#endif
        return DecideCommandLine(
            IsSingleFileBuild(),
            PathText.GetDirectoryName(exePath) ?? string.Empty,
            ReadSetupRecord(registry));
    }

    /// <summary>
    /// The kind of the copy running from <paramref name="exePath"/>, from this build,
    /// the setup's record and the folder the exe is in.
    /// </summary>
    /// <param name="exePath">The running exe's path.</param>
    /// <param name="isSelfContained">
    /// Whether the build carries its own .NET runtime, as the host's build states it.
    /// </param>
    /// <param name="registry">Reads the setup's record.</param>
    public static AppCopyKind DetectRunning(string exePath, bool isSelfContained, IRegistryReader registry)
    {
#if DEBUG
        if (PretendedKind() is { } pretended)
            return pretended;
#endif
        return Decide(
            IsSingleFileBuild(),
            isSelfContained,
            PathText.GetDirectoryName(exePath) ?? string.Empty,
            ReadSetupRecord(registry),
            FrameworkFile.Exists);
    }

    /// <summary>
    /// Whether the running build is a single-file bundle. Inside one, an assembly has
    /// no file of its own, and its Location is empty; every build from source has one.
    /// </summary>
    private static bool IsSingleFileBuild() =>
        string.IsNullOrEmpty(typeof(AppCopy).Assembly.Location);

#if DEBUG
    /// <summary>
    /// Set to setup, portable, slim, scoop or undetermined, a Debug build of the window
    /// takes itself for that kind of copy, and set to setup, cli or undetermined, a Debug
    /// build of the command line takes itself for the setup's copy, its own download or a
    /// copy whose setup record cannot tell, so a build from source can
    /// show what each kind of copy offers and take each route an update takes. The name
    /// and its read sit only inside <c>#if DEBUG</c> blocks, and
    /// <c>scripts/check-debug-only-switches.mjs</c> holds them there, so no Release
    /// build can be told it is another kind of copy.
    /// </summary>
    internal const string PretendCopyVariable = "INSTALLERCLEAN_DEBUG_PRETEND_COPY";

    private static AppCopyKind? PretendedKind() =>
        Environment.GetEnvironmentVariable(PretendCopyVariable)?.Trim().ToLowerInvariant() switch
        {
            "setup" => AppCopyKind.Setup,
            "portable" => AppCopyKind.Portable,
            "slim" => AppCopyKind.Slim,
            "scoop" => AppCopyKind.Scoop,
            "undetermined" => AppCopyKind.Undetermined,
            _ => null,
        };

    private static AppCopyKind? PretendedCommandLineKind() =>
        Environment.GetEnvironmentVariable(PretendCopyVariable)?.Trim().ToLowerInvariant() switch
        {
            "setup" => AppCopyKind.Setup,
            "cli" => AppCopyKind.CommandLineDownload,
            "undetermined" => AppCopyKind.Undetermined,
            _ => null,
        };
#endif

    /// <summary>
    /// The setup's record from the 64-bit view of the registry: the folder its
    /// InstallLocation names; none where the key is absent; or cannot tell where the key
    /// cannot be read, or is there and its InstallLocation is missing, holds no text or
    /// is not a full path. The setup writes InstallLocation on every install, so a key without
    /// a usable one is a record that cannot say whether this copy is the setup's.
    /// </summary>
    public static SetupRecord ReadSetupRecord(IRegistryReader registry)
    {
        var read = registry.LocalMachineValues(SetupUninstallKeyPath, CancellationToken.None);
        if (read.Presence == RegistryKeyPresence.Absent) return SetupRecord.None;
        if (read.Presence != RegistryKeyPresence.Present || read.Values is null) return SetupRecord.CannotTell;
        foreach (var value in read.Values)
        {
            if (string.Equals(value.Name, InstallLocationValueName, StringComparison.OrdinalIgnoreCase))
                return SetupRecord.At(value.Text);
        }
        return SetupRecord.CannotTell;
    }

    /// <summary>
    /// Whether this kind of copy can put a newer version in its own place: the setup's
    /// copy, the portable, the slim and the command line's own download. A build from
    /// source has no download to do it with, Scoop's copy is updated by Scoop, and an
    /// undetermined copy may be the setup's. A kind not named here does not update itself.
    /// </summary>
    public static bool CanUpdateItself(AppCopyKind kind) =>
        kind is AppCopyKind.Setup or AppCopyKind.Portable or AppCopyKind.Slim or AppCopyKind.CommandLineDownload;

    /// <summary>
    /// Whether this kind of copy is updated by Scoop, so a newer version is offered as
    /// Scoop's update command (<see cref="ReadScoopUpdate"/>).
    /// </summary>
    public static bool UpdatedByScoop(AppCopyKind kind) => kind == AppCopyKind.Scoop;

    /// <summary>
    /// Scoop's update command for the copy running from <paramref name="exeFolder"/>, or
    /// null where the folder is not <c>&lt;root&gt;\apps\&lt;name&gt;\&lt;version&gt;</c>
    /// with a name Scoop gives an app. The command is <c>scoop update &lt;name&gt;</c>, with
    /// <c>--global</c> after it where the root is Scoop's global root
    /// (<see cref="ResolveScoopGlobalRoot"/>) and only then: a root that is not that
    /// one, or a global root that cannot be established, gives the command without it.
    /// </summary>
    /// <param name="exeFolder">The folder the running exe is in.</param>
    /// <param name="variable">Answers the value of a variable Scoop reads, or null where it is not set.</param>
    /// <param name="readConfig">Answers a file's text, or null where there is no such file.</param>
    /// <param name="commonApplicationData">The machine-wide application data folder (ProgramData).</param>
    public static ScoopUpdate? ReadScoopUpdate(
        string exeFolder, Func<string, string?> variable, Func<string, string?> readConfig,
        string commonApplicationData)
    {
        if (Normalise(exeFolder) is not { } versionFolder) return null;
        var nameFolder = PathText.GetDirectoryName(versionFolder);
        var appsFolder = nameFolder is null ? null : PathText.GetDirectoryName(nameFolder);
        var root = appsFolder is null ? null : PathText.GetDirectoryName(appsFolder);
        if (nameFolder is null || appsFolder is null || root is null) return null;
        if (!string.Equals(PathText.GetFileName(appsFolder), "apps", StringComparison.OrdinalIgnoreCase)) return null;

        var name = PathText.GetFileName(nameFolder);
        if (!ScoopAppName.IsMatch(name)) return null;

        var isGlobal = ResolveScoopGlobalRoot(variable, readConfig, commonApplicationData) is { } globalRoot
            && string.Equals(root, globalRoot, StringComparison.OrdinalIgnoreCase);
        return new ScoopUpdate(isGlobal ? $"scoop update {name} --global" : $"scoop update {name}", isGlobal);
    }

    /// <summary>
    /// What Scoop gives an app as a folder name, which is its manifest's file name: letters,
    /// digits, dots, hyphens and underscores, so a name that is anything else is not typed
    /// into a command.
    /// </summary>
    private static readonly System.Text.RegularExpressions.Regex ScoopAppName =
        new(@"^[A-Za-z0-9][A-Za-z0-9._-]*$", System.Text.RegularExpressions.RegexOptions.CultureInvariant);

    /// <summary>
    /// Scoop's global root, as Scoop resolves it: the SCOOP_GLOBAL variable, then the
    /// global_path setting in <c>scoop\config.json</c> under XDG_CONFIG_HOME or else
    /// under <c>.config</c> in the user profile, then <c>scoop</c> in the machine-wide
    /// application data folder. Only the settings file under the config home is read. Null
    /// where it is not established: the value found is not a full path, or the settings file
    /// is there and cannot be read or parsed.
    /// </summary>
    internal static string? ResolveScoopGlobalRoot(
        Func<string, string?> variable, Func<string, string?> readConfig, string commonApplicationData)
    {
        string? chosen = variable("SCOOP_GLOBAL");
        if (string.IsNullOrEmpty(chosen))
        {
            var configHome = variable("XDG_CONFIG_HOME");
            if (string.IsNullOrEmpty(configHome))
            {
                var profile = variable("USERPROFILE");
                if (string.IsNullOrEmpty(profile)) return null;
                configHome = PathText.Combine(profile, ".config");
            }

            string? text;
            try
            {
                text = readConfig(PathText.Combine(configHome, "scoop", "config.json"));
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or ArgumentException or NotSupportedException)
            {
                return null;
            }

            if (text is not null)
            {
                try
                {
                    using var document = System.Text.Json.JsonDocument.Parse(text);
                    if (document.RootElement.ValueKind != System.Text.Json.JsonValueKind.Object) return null;
                    foreach (var property in document.RootElement.EnumerateObject())
                    {
                        if (!string.Equals(property.Name, "global_path", StringComparison.OrdinalIgnoreCase)) continue;
                        if (property.Value.ValueKind == System.Text.Json.JsonValueKind.Null) continue;
                        if (property.Value.ValueKind != System.Text.Json.JsonValueKind.String) return null;
                        chosen = property.Value.GetString();
                    }
                }
                catch (System.Text.Json.JsonException)
                {
                    return null;
                }
            }

            if (string.IsNullOrEmpty(chosen)) chosen = PathText.Combine(commonApplicationData, "scoop");
        }

        return PathText.IsPathFullyQualified(chosen) ? Normalise(chosen) : null;
    }

    /// <summary>
    /// The file name of the download a kind updates from, or null for a kind that does
    /// not update itself: a build from source, Scoop's copy and an undetermined copy.
    /// Only the setup's name carries the version. The portable, the slim and the command
    /// line's own download keep one name across releases, because each replaces itself
    /// where it stands and the release it sits in says which version it is.
    /// </summary>
    public static string? DownloadFileName(AppCopyKind kind, string version) => kind switch
    {
        AppCopyKind.Setup => $"InstallerClean-{version}-setup.exe",
        AppCopyKind.Portable => "InstallerClean-portable.exe",
        AppCopyKind.Slim => "InstallerClean-slim.exe",
        AppCopyKind.CommandLineDownload => CommandLineFileName,
        _ => null,
    };

    /// <summary>
    /// The address of the download a kind updates from, or null for a kind that does not
    /// update itself. The version is the one the update check read off GitHub's answer,
    /// which it accepts only as numbers and dots.
    /// </summary>
    public static Uri? DownloadAddress(AppCopyKind kind, string version) =>
        DownloadFileName(kind, version) is { } name
            ? new Uri($"{DownloadRoot}v{version}/{name}")
            : null;

    /// <summary>
    /// Whether the setup's record names <paramref name="exeFolder"/>: both made absolute
    /// and stripped of trailing separators, then compared without regard to case, as
    /// Windows compares names. Inno Setup writes InstallLocation with a trailing backslash.
    /// </summary>
    private static bool IsSetupFolder(string exeFolder, SetupRecord setup) =>
        setup is { State: SetupRecordState.Folder, Folder: { } folder }
        && Normalise(exeFolder) is { } exe
        && string.Equals(exe, folder, StringComparison.OrdinalIgnoreCase);

    /// <summary>
    /// <paramref name="path"/> made absolute and stripped of trailing separators, or null
    /// where it is not a path.
    /// </summary>
    internal static string? Normalise(string path)
    {
        try
        {
            return PathText.TrimEndingDirectorySeparator(FullPaths.GetFullPath(path));
        }
        catch (Exception ex) when (ex is ArgumentException or NotSupportedException or PathTooLongException)
        {
            return null;
        }
    }

    /// <summary>
    /// Whether the folder holds the two files Scoop writes into each version's folder
    /// it installs: its copy of the app's manifest and its record of the install.
    /// Scoop 0.6.0 and later name them scoop-manifest.json and scoop-install.json;
    /// earlier versions wrote manifest.json and install.json.
    /// </summary>
    private static bool IsScoopFolder(string folder, Func<string, bool> fileExists)
    {
        bool Has(string name) => fileExists(PathText.Combine(folder, name));
        return (Has("scoop-manifest.json") || Has("manifest.json"))
            && (Has("scoop-install.json") || Has("install.json"));
    }
}
