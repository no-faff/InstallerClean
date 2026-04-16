namespace InstallerClean.Services;

internal static class InstallerCacheHelpers
{
    internal static readonly string InstallerFolder =
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Windows), "Installer");

    internal static bool IsInstallerFolderOrChild(string path)
    {
        var normalised = Path.GetFullPath(path)
            .TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        var installerNormalised = InstallerFolder
            .TrimEnd(Path.DirectorySeparatorChar);

        return normalised.Equals(installerNormalised, StringComparison.OrdinalIgnoreCase)
            || normalised.StartsWith(installerNormalised + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Deletes empty subdirectories inside C:\Windows\Installer.
    /// Processes deepest first so nested empty trees collapse in one pass.
    /// </summary>
    internal static void PruneEmptySubdirectories()
    {
        if (!Directory.Exists(InstallerFolder)) return;

        foreach (var dir in Directory.EnumerateDirectories(InstallerFolder, "*", SearchOption.AllDirectories)
            .OrderByDescending(d => d.Length)) // deepest first
        {
            try
            {
                if (!Directory.EnumerateFileSystemEntries(dir).Any())
                    Directory.Delete(dir);
            }
            catch (Exception) { /* skip protected directories */ }
        }
    }
}
