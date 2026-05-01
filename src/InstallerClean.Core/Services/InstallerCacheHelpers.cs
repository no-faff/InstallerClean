using InstallerClean.Interop.Native;

namespace InstallerClean.Services;

internal static class InstallerCacheHelpers
{
    internal static readonly string InstallerFolder =
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Windows), "Installer");

    /// <summary>
    /// True if <paramref name="path"/> resolves to <c>C:\Windows\Installer</c>
    /// or any descendant after symlinks/junctions/subst-mapped drives are
    /// expanded. Used as the bottom-line safety check before any move:
    /// the entire restore-after-mistakes story collapses if files end up
    /// back inside the Installer folder.
    /// </summary>
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
    /// Expands symlinks, NTFS junctions and subst-mapped drives to the
    /// real on-disk path. Required so a destination check cannot be
    /// bypassed by picking a junction that points inside
    /// C:\Windows\Installer. Falls back to Path.GetFullPath if the
    /// kernel32 resolution call fails.
    /// </summary>
    internal static string ResolveFinalPath(string path)
    {
        string normalised;
        try { normalised = Path.GetFullPath(path); }
        catch { return path; }

        // GetFinalPathNameByHandle needs an existing target; walk up
        // until an ancestor exists and open that.
        var probe = normalised;
        while (probe.Length > 0 && !Directory.Exists(probe) && !File.Exists(probe))
        {
            var parent = Path.GetDirectoryName(probe);
            if (string.IsNullOrEmpty(parent) || parent == probe) return normalised;
            probe = parent;
        }

        try
        {
            using var handle = Kernel32.CreateFile(
                probe,
                0,
                Kernel32.FILE_SHARE_ALL,
                IntPtr.Zero,
                Kernel32.OPEN_EXISTING,
                Kernel32.FILE_FLAG_BACKUP_SEMANTICS,
                IntPtr.Zero);

            if (handle.IsInvalid) return normalised;

            var buffer = new char[PathBufferLength];
            var length = Kernel32.GetFinalPathNameByHandle(
                handle, buffer, (uint)buffer.Length, Kernel32.VOLUME_NAME_DOS);
            if (length == 0) return normalised;
            if (length >= buffer.Length)
            {
                // Buffer too small. The returned length includes the
                // null terminator in the required-size case, so allocate
                // exactly that many chars and retry.
                buffer = new char[length];
                length = Kernel32.GetFinalPathNameByHandle(
                    handle, buffer, (uint)buffer.Length, Kernel32.VOLUME_NAME_DOS);
                if (length == 0) return normalised;
            }

            var resolved = StripLongPathPrefix(new string(buffer, 0, (int)length));

            // Reattach the not-yet-created suffix the caller asked
            // about so unborn destination paths still resolve to the
            // correct real-folder root.
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

    /// <summary>
    /// Strips the <c>\\?\</c> long-path prefix the kernel adds. Keeps
    /// the path comparable to user-typed paths and to the value of
    /// <see cref="InstallerFolder"/>.
    /// </summary>
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
    /// Processes deepest first so nested empty trees collapse in one
    /// pass. Cancellable because a deeply nested Installer tree can
    /// take several seconds to walk.
    /// </summary>
    internal static void PruneEmptySubdirectories(CancellationToken cancellationToken = default)
    {
        if (!Directory.Exists(InstallerFolder)) return;

        // Match FileSystemScanService: skip reparse points so a junction
        // planted inside the Installer folder cannot redirect the prune
        // pass to delete empty directories outside the cache.
        var options = new EnumerationOptions
        {
            RecurseSubdirectories = true,
            AttributesToSkip = FileAttributes.ReparsePoint,
            IgnoreInaccessible = true,
        };

        foreach (var dir in Directory.EnumerateDirectories(InstallerFolder, "*", options)
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

    // 520 chars covers any practical Windows long path (260 standard +
    // headroom for the \\?\ prefix and the not-yet-created suffix the
    // caller may attach).
    private const int PathBufferLength = 520;
}
