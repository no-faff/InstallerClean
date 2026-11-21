using System.Runtime.InteropServices;

namespace InstallerClean.Tests.Helpers;

/// <summary>
/// A fact that runs only where the temporary folder's volume gives a folder a short (8.3)
/// name, and is reported skipped anywhere else. Windows can be set to make no short names on
/// a volume, and then there is nothing to expand.
/// </summary>
public sealed class ShortNamesFactAttribute : FactAttribute
{
    // Asked once per run: xUnit builds the attribute for every test it marks, at discovery and
    // again at the run. A folder that cannot be made or read counts as no short names.
    private static readonly Lazy<bool> TempMakesShortNames = new(() =>
    {
        try
        {
            return OperatingSystem.IsWindows() && ShortName(Path.GetTempPath()) is not null;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            return false;
        }
    });

    public ShortNamesFactAttribute()
    {
        if (!TempMakesShortNames.Value)
            Skip = "Needs a temporary folder on a volume that makes short (8.3) names.";
    }

    /// <summary>
    /// The short spelling of a new folder with a long name made in <paramref name="parent"/>,
    /// or null where the volume gives it none. The folder is removed again.
    /// </summary>
    internal static string? ShortName(string parent)
    {
        var folder = Path.Combine(parent, "ic short name probe " + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(folder);
        try
        {
            var shortPath = GetShortPath(folder);
            var name = shortPath is null ? null : Path.GetFileName(shortPath);
            return name is null || string.Equals(name, Path.GetFileName(folder), StringComparison.OrdinalIgnoreCase)
                ? null
                : name;
        }
        finally
        {
            Directory.Delete(folder);
        }
    }

    /// <summary>The path with each component Windows has a short name for in its short form.</summary>
    internal static string? GetShortPath(string path)
    {
        var buffer = new char[1024];
        var length = GetShortPathName(path, buffer, (uint)buffer.Length);
        return length == 0 || length > buffer.Length ? null : new string(buffer, 0, (int)length);
    }

    /// <summary>
    /// The path with every short name in it in its long form, as Windows gives it: the expected
    /// answer, read from Windows directly rather than through the code under test.
    /// </summary>
    internal static string? GetLongPath(string path)
    {
        var buffer = new char[1024];
        var length = GetLongPathName(path, buffer, (uint)buffer.Length);
        return length == 0 || length > buffer.Length ? null : new string(buffer, 0, (int)length);
    }

    [DllImport("kernel32.dll", EntryPoint = "GetShortPathNameW", CharSet = CharSet.Unicode, ExactSpelling = true, SetLastError = true)]
    private static extern uint GetShortPathName(string lpszLongPath, [Out] char[] lpszShortPath, uint cchBuffer);

    [DllImport("kernel32.dll", EntryPoint = "GetLongPathNameW", CharSet = CharSet.Unicode, ExactSpelling = true, SetLastError = true)]
    private static extern uint GetLongPathName(string lpszShortPath, [Out] char[] lpszLongPath, uint cchBuffer);
}
