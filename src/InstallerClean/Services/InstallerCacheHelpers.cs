using System.Runtime.InteropServices;
using System.Text;
using Microsoft.Win32.SafeHandles;

namespace InstallerClean.Services;

internal static class InstallerCacheHelpers
{
    internal static readonly string InstallerFolder =
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Windows), "Installer");

    internal static bool IsInstallerFolderOrChild(string path)
    {
        if (string.IsNullOrWhiteSpace(path)) return false;

        var resolvedInput = ResolveFinalPath(path)
            .TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        var resolvedInstaller = ResolveFinalPath(InstallerFolder)
            .TrimEnd(Path.DirectorySeparatorChar);

        return resolvedInput.Equals(resolvedInstaller, StringComparison.OrdinalIgnoreCase)
            || resolvedInput.StartsWith(resolvedInstaller + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Expands symlinks, NTFS junctions and `subst`-mapped drives to the
    /// real on-disk path. Required so a destination check can't be bypassed
    /// by picking a junction that points inside C:\Windows\Installer.
    /// Falls back to Path.GetFullPath if the resolution call fails.
    /// </summary>
    internal static string ResolveFinalPath(string path)
    {
        string normalised;
        try { normalised = Path.GetFullPath(path); }
        catch { return path; }

        // GetFinalPathNameByHandle needs an existing target; walk up until
        // an ancestor exists and open that.
        var probe = normalised;
        while (probe.Length > 0 && !Directory.Exists(probe) && !File.Exists(probe))
        {
            var parent = Path.GetDirectoryName(probe);
            if (string.IsNullOrEmpty(parent) || parent == probe) return normalised;
            probe = parent;
        }

        try
        {
            using var handle = CreateFile(
                probe,
                0,
                FileShareAll,
                IntPtr.Zero,
                OpenExisting,
                FileFlagBackupSemantics,
                IntPtr.Zero);

            if (handle.IsInvalid) return normalised;

            var buffer = new StringBuilder(PathBufferLength);
            var length = GetFinalPathNameByHandle(handle, buffer, (uint)buffer.Capacity, VolumeNameDos);
            if (length == 0) return normalised;
            if (length >= buffer.Capacity)
            {
                buffer = new StringBuilder((int)length + 1);
                length = GetFinalPathNameByHandle(handle, buffer, (uint)buffer.Capacity, VolumeNameDos);
                if (length == 0) return normalised;
            }

            var resolved = StripLongPathPrefix(buffer.ToString());

            // Reattach the not-yet-created suffix the caller asked about.
            if (probe.Length < normalised.Length)
            {
                var suffix = normalised.Substring(probe.Length);
                resolved = resolved.TrimEnd(Path.DirectorySeparatorChar) + suffix;
            }

            return resolved;
        }
        catch
        {
            return normalised;
        }
    }

    private static string StripLongPathPrefix(string path)
    {
        const string uncPrefix = @"\\?\UNC\";
        const string longPrefix = @"\\?\";
        if (path.StartsWith(uncPrefix, StringComparison.Ordinal))
            return @"\\" + path.Substring(uncPrefix.Length);
        if (path.StartsWith(longPrefix, StringComparison.Ordinal))
            return path.Substring(longPrefix.Length);
        return path;
    }

    /// <summary>
    /// Deletes empty subdirectories inside C:\Windows\Installer.
    /// Processes deepest first so nested empty trees collapse in one pass.
    /// Cancellable because a deeply nested Installer tree can take several
    /// seconds to walk.
    /// </summary>
    internal static void PruneEmptySubdirectories(CancellationToken cancellationToken = default)
    {
        if (!Directory.Exists(InstallerFolder)) return;

        foreach (var dir in Directory.EnumerateDirectories(InstallerFolder, "*", SearchOption.AllDirectories)
            .OrderByDescending(d => d.Length))
        {
            cancellationToken.ThrowIfCancellationRequested();
            try
            {
                if (!Directory.EnumerateFileSystemEntries(dir).Any())
                    Directory.Delete(dir);
            }
            catch (Exception) { /* skip protected directories */ }
        }
    }

    // ---- P/Invoke surface ----

    private const uint OpenExisting          = 3;
    private const uint FileShareAll          = 0x00000007;
    private const uint FileFlagBackupSemantics = 0x02000000;
    private const uint VolumeNameDos         = 0x0;
    private const int  PathBufferLength      = 520;

    [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Unicode, EntryPoint = "CreateFileW")]
    private static extern SafeFileHandle CreateFile(
        string lpFileName,
        uint dwDesiredAccess,
        uint dwShareMode,
        IntPtr lpSecurityAttributes,
        uint dwCreationDisposition,
        uint dwFlagsAndAttributes,
        IntPtr hTemplateFile);

    [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Unicode, EntryPoint = "GetFinalPathNameByHandleW")]
    private static extern uint GetFinalPathNameByHandle(
        SafeFileHandle hFile,
        StringBuilder lpszFilePath,
        uint cchFilePath,
        uint dwFlags);
}
