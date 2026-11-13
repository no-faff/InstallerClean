using System.Globalization;
using System.Text.RegularExpressions;

namespace InstallerClean.Helpers;

/// <summary>
/// The language picked in the app, read for the message the window shows where its start
/// checks refuse, before anything else the window runs.
/// </summary>
/// <remarks>
/// The window applies the pick later, through <c>SettingsService</c>, whose JSON reading and
/// folder move need more than .NET Framework 4.0 has, and a copy that refuses never gets that
/// far. So this reads settings.json itself and takes the <c>language</c> value as the app
/// writes it, then checks it with <see cref="LanguagePreference.Resolve"/> as the window does.
/// It reads the file in <c>%LOCALAPPDATA%\InstallerClean</c>, or where there is none there,
/// the one in <c>%LOCALAPPDATA%\NoFaff\InstallerClean</c>, where earlier versions keep it,
/// which is the file <see cref="AppDataFolder"/> would settle on. It moves and writes
/// nothing. Everything here is held to what <c>NetFrameworkCheck</c>'s remarks set out.
/// </remarks>
internal static class PickedLanguageAtStart
{
    // SettingsService's cap.
    private const int MaxReadBytes = 64 * 1024;

    // A "language" key and its string value. Inside a JSON string a quote is escaped, so the
    // key's quotes match only a key. A culture name holds no quote or backslash.
    private const string LanguageValue = "\"language\"\\s*:\\s*\"([^\"\\\\]{1,32})\"";

    /// <summary>
    /// The language picked in the app, or null where none is picked, the pick is not a language
    /// the app ships, or anything here fails: the message is then in the language Windows
    /// displays.
    /// </summary>
    internal static CultureInfo? Read()
    {
        try
        {
            var localAppData = KnownFolders.GetPath(Environment.SpecialFolder.LocalApplicationData);
            if (string.IsNullOrEmpty(localAppData)) return null;
            var root = localAppData.TrimEnd('\\');
            return FromSettings(
                SettingsText(root + @"\InstallerClean\settings.json")
                ?? SettingsText(root + @"\NoFaff\InstallerClean\settings.json"));
        }
        catch (Exception)
        {
            return null;
        }
    }

    /// <summary>
    /// The language the settings text <paramref name="json"/> picks, or null where it picks
    /// none the app ships.
    /// </summary>
    internal static CultureInfo? FromSettings(string? json)
    {
        if (json is null) return null;
        var match = Regex.Match(json, LanguageValue, RegexOptions.CultureInvariant);
        return match.Success ? LanguagePreference.Resolve(match.Groups[1].Value) : null;
    }

    /// <summary>
    /// The text of the settings file at <paramref name="path"/>, empty where it is past the cap,
    /// or null where there is none.
    /// </summary>
    private static string? SettingsText(string path)
    {
        FileStream stream;
        try
        {
            stream = FrameworkFileStream.Open(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete);
        }
        catch (Exception ex) when (ex is FileNotFoundException or DirectoryNotFoundException)
        {
            return null;
        }
        using (stream)
        {
            if (stream.Length > MaxReadBytes) return string.Empty;
            using var reader = new StreamReader(stream);
            return reader.ReadToEnd();
        }
    }
}
