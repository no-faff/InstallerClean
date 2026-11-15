namespace InstallerClean.Helpers;

/// <summary>
/// The folder a copy keeps its files in when it is run as a portable app: the folder its exe
/// is in, where a file named <c>InstallerClean-portable.txt</c>, or <c>InstallerClean-portable</c>
/// with no extension, sits beside the exe. Its content is not read. Without that file a copy
/// keeps its files in <c>%LOCALAPPDATA%\InstallerClean</c> (<see cref="AppDataFolder"/>).
/// </summary>
/// <remarks>
/// The copy is the one this process stands for: the exe a <c>/replacecopy:</c> switch names,
/// where a newer version running from its staging folder replaces a copy or runs a command for
/// it, and otherwise the exe this process runs from. So a copy updating itself keeps writing
/// beside its own exe, and the marker, which the replace never touches, keeps the copy portable
/// after it.
///
/// Read before the start checks answer, by <see cref="PickedLanguageAtStart"/>, so everything
/// here uses only what .NET Framework 4.0 has (NetFrameworkCheck's remarks).
/// </remarks>
internal static class PortableFolder
{
    /// <summary>The names the marker file is found by, the second for a file made with no extension.</summary>
    internal static readonly string[] MarkerNames = { "InstallerClean-portable.txt", "InstallerClean-portable" };

    /// <summary>
    /// The folder of the copy this process stands for where the marker is beside its exe, or
    /// null, for what runs before the start checks. <see cref="AppDataFolder"/> takes the copy
    /// from the replace request the hand-over reads, and leaves the setup's copy out. Never
    /// throws: anything that fails is no marker found.
    /// </summary>
    internal static string? Find()
    {
        try
        {
            var entry = System.Reflection.Assembly.GetEntryAssembly();
            return Find(CopyStoodFor(Environment.GetCommandLineArgs()) ?? entry?.Location, path => FrameworkFile.Exists(path));
        }
        catch (Exception)
        {
            return null;
        }
    }

    /// <summary>
    /// The folder of the copy <paramref name="args"/> stand for, the exe named by their one
    /// <c>/replacecopy:</c> switch with a full path or else <paramref name="runningExe"/>,
    /// where <paramref name="isFile"/> finds one of <see cref="MarkerNames"/> beside it; or null.
    /// </summary>
    internal static string? Find(string[] args, string? runningExe, Func<string, bool> isFile) =>
        Find(CopyStoodFor(args) ?? runningExe, isFile);

    /// <summary>
    /// The folder <paramref name="exe"/> is in, where <paramref name="isFile"/> finds one of
    /// <see cref="MarkerNames"/> beside it; or null.
    /// </summary>
    internal static string? Find(string? exe, Func<string, bool> isFile)
    {
        if (string.IsNullOrEmpty(exe)) return null;
        // The exe's folder by the last separator, as a full path names it. Joined by hand, as
        // what runs before the start checks keeps to .NET Framework 4.0.
        // A drive's root keeps its separator, since "C:" alone names the drive's working folder.
        var cut = exe.LastIndexOfAny(new[] { '\\', '/' });
        if (cut <= 0) return null;
        var folder = cut == 2 && exe[1] == ':' ? exe.Substring(0, 3) : exe.Substring(0, cut);
        var prefix = folder.EndsWith(@"\", StringComparison.Ordinal) || folder.EndsWith("/", StringComparison.Ordinal)
            ? folder
            : folder + @"\";
        foreach (var name in MarkerNames)
        {
            if (isFile(prefix + name)) return folder;
        }
        return null;
    }

    /// <summary>
    /// The exe the one <c>/replacecopy:</c> switch in <paramref name="args"/> names with a full
    /// path, or null where there is none, more than one, or a path that is not full.
    /// </summary>
    private static string? CopyStoodFor(string[] args)
    {
        string? named = null;
        foreach (var arg in args)
        {
            if (arg is null || !arg.StartsWith(HandOverContract.ReplaceCopySwitch, StringComparison.OrdinalIgnoreCase))
                continue;
            if (named is not null) return null;
            named = arg.Substring(HandOverContract.ReplaceCopySwitch.Length);
        }
        if (named is null || named.Length < 3) return null;
        var drive = char.IsLetter(named[0]) && named[1] == ':' && (named[2] == '\\' || named[2] == '/');
        var unc = named.StartsWith(@"\\", StringComparison.Ordinal);
        return drive || unc ? named : null;
    }
}
