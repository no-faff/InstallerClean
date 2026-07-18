using System.IO.Abstractions;
using System.Security;
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
    /// <param name="installerFolderRoot">
    /// Test-only real-folder override for the cache root (null in production,
    /// which uses the real <see cref="InstallerFolder"/>). The comparison still
    /// runs against the REAL filesystem via <see cref="ResolveFinalPath"/>, so
    /// this only relocates the check to a real sandbox directory for the
    /// integration tests; it does NOT let a MockFileSystem bypass the gate. It
    /// mirrors <c>FileSystemScanService</c>'s own installer-folder override.
    /// </param>
    internal static bool IsInstallerFolderOrChild(string path, string? installerFolderRoot = null)
    {
        if (string.IsNullOrWhiteSpace(path)) return false;

        TryResolveFinalPath(path, out var resolvedInput);
        return ResolvesInsideInstallerFolder(resolvedInput, installerFolderRoot);
    }

    /// <summary>
    /// The containment comparison itself, over an ALREADY-resolved input. The
    /// source-side gate (<see cref="CandidateGuard"/>) takes this form together
    /// with <see cref="TryResolveFinalPath"/>, so that it can refuse a path
    /// whose resolution degraded; the destination gate above ignores that
    /// distinction, and the two sides want opposite answers for it. A
    /// destination gate asks "is this forbidden", so an unresolvable answer of
    /// "not forbidden" lets the move proceed, where refusing would strand a user
    /// with no destination they could use. A source gate asks "is this in
    /// bounds", and the whole reason it exists is that a corrupt LocalPackage
    /// value can name a file anywhere on disk, so "could not prove it is in
    /// bounds" and "out of bounds" earn the same refusal.
    ///
    /// The cache root is compared in its best-effort form even when ITS
    /// resolution degraded, and that is safe in the direction that matters: a
    /// fully resolved input is a real canonical path, so if the root is a
    /// junction this comparison fails to match and the caller refuses. An
    /// unresolvable root can only cost a false negative, never a false positive.
    /// </summary>
    internal static bool ResolvesInsideInstallerFolder(string resolvedInput, string? installerFolderRoot = null)
    {
        var input = resolvedInput
            .TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        var installer = ResolveFinalPath(installerFolderRoot ?? InstallerFolder)
            .TrimEnd(Path.DirectorySeparatorChar);

        return input.Equals(installer, StringComparison.OrdinalIgnoreCase)
            || input.StartsWith(installer + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// True if <paramref name="path"/> resolves under any of the
    /// canonical Windows system folders: <c>%SystemRoot%</c>,
    /// <c>%ProgramFiles%</c>, <c>%ProgramFiles(x86)%</c>, or
    /// <c>%ProgramData%</c>. Symlinks, junctions and subst-mapped
    /// drives are expanded the same way as
    /// <see cref="IsInstallerFolderOrChild"/>. The CLI uses this to
    /// refuse a saved Move destination that resolves under a system
    /// folder, since those folders are on documented DLL-search and
    /// SxS-resolution paths and the CLI writes there silently
    /// (without showing the user the resolved path first). Per-user
    /// Documents/Desktop are deliberately not in this list: they're
    /// data folders, not system trust boundaries.
    /// </summary>
    internal static bool IsSystemFolderOrChild(string path)
    {
        if (string.IsNullOrWhiteSpace(path)) return false;

        var resolvedInput = ResolveFinalPath(path)
            .TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);

        var systemRoots = new[]
        {
            Environment.GetFolderPath(Environment.SpecialFolder.Windows),
            Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles),
            Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86),
            Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData),
        };

        foreach (var root in systemRoots)
        {
            if (string.IsNullOrEmpty(root)) continue;
            var resolvedRoot = ResolveFinalPath(root).TrimEnd(Path.DirectorySeparatorChar);
            if (resolvedInput.Equals(resolvedRoot, StringComparison.OrdinalIgnoreCase))
                return true;
            if (resolvedInput.StartsWith(resolvedRoot + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase))
                return true;
        }
        return false;
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
        TryResolveFinalPath(path, out var resolved);
        return resolved;
    }

    /// <summary>
    /// <see cref="ResolveFinalPath"/> with its degradation made visible: false
    /// means the kernel never expanded this path and
    /// <paramref name="resolved"/> is the best-effort string instead of a proven
    /// one. Every caller gets the same string either way, so the bool is the
    /// only difference and no caller is forced to handle it.
    ///
    /// It matters because a degraded result is exactly a path whose reparse
    /// points went UNexpanded, which is the one thing a containment check is
    /// there to see through. A gate that cannot tell the two apart calls a
    /// junction in-bounds on the strength of how its name is spelled. See
    /// <see cref="ResolvesInsideInstallerFolder"/> for which side wants which.
    /// </summary>
    internal static bool TryResolveFinalPath(string path, out string resolved)
    {
        string normalised;
        try { normalised = Path.GetFullPath(path); }
        catch { resolved = path; return false; }

        resolved = normalised;

        // GetFinalPathNameByHandle needs an existing target; walk up
        // until an ancestor exists and open that.
        var probe = normalised;
        while (probe.Length > 0 && !Directory.Exists(probe) && !File.Exists(probe))
        {
            var parent = Path.GetDirectoryName(probe);
            if (string.IsNullOrEmpty(parent) || parent == probe) return false;
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

            if (handle.IsInvalid) return false;

            var buffer = new char[PathBufferLength];
            var length = Kernel32.GetFinalPathNameByHandle(
                handle, buffer, (uint)buffer.Length, Kernel32.VOLUME_NAME_DOS);
            if (length == 0) return false;
            if (length >= buffer.Length)
            {
                // Buffer too small. The returned length includes the
                // null terminator in the required-size case, so allocate
                // exactly that many chars and retry.
                buffer = new char[length];
                length = Kernel32.GetFinalPathNameByHandle(
                    handle, buffer, (uint)buffer.Length, Kernel32.VOLUME_NAME_DOS);
                if (length == 0) return false;
            }

            var final = StripLongPathPrefix(new string(buffer, 0, (int)length));

            // Reattach the not-yet-created suffix to the resolved root.
            // Path.Combine handles the separator boundary; probe = "C:\"
            // gives a suffix without a leading separator, others with.
            if (probe.Length < normalised.Length)
            {
                var suffix = normalised.Substring(probe.Length)
                    .TrimStart(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
                final = Path.Combine(final, suffix);
            }

            resolved = final;
            return true;
        }
        catch
        {
            resolved = normalised;
            return false;
        }
    }

    /// <summary>
    /// Strips the <c>\\?\</c> long-path prefix the kernel adds. Keeps
    /// the path comparable to user-typed paths and to the value of
    /// <see cref="InstallerFolder"/>.
    /// </summary>
    internal static string StripLongPathPrefix(string path)
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
    /// <remarks>
    /// Goes through the caller's <paramref name="fileSystem"/> rather than
    /// System.IO, unlike <see cref="IsInstallerFolderOrChild"/> and
    /// StorageHelpers.IsReparsePoint next door: those two are gates a
    /// MockFileSystem must not be able to defeat, while this is cleanup that
    /// runs after the gates have already passed. Injected, a test's
    /// MockFileSystem has no C:\Windows\Installer, so the prune returns at the
    /// Exists check instead of walking the host's real cache folder.
    /// The folder is NOT a parameter, so no caller can aim the prune anywhere
    /// else.
    /// </remarks>
    internal static void PruneEmptySubdirectories(
        IFileSystem fileSystem,
        CancellationToken cancellationToken = default)
    {
        if (!fileSystem.Directory.Exists(InstallerFolder)) return;

        // Match FileSystemScanService: skip reparse points so a junction
        // planted inside the Installer folder cannot redirect the prune
        // pass to delete empty directories outside the cache.
        var options = new EnumerationOptions
        {
            RecurseSubdirectories = true,
            AttributesToSkip = FileAttributes.ReparsePoint,
            IgnoreInaccessible = true,
        };

        // Best-effort: the prune runs after a move or delete has already
        // committed, so a failure here must not undo that success.
        try
        {
            foreach (var dir in fileSystem.Directory.EnumerateDirectories(InstallerFolder, "*", options)
                .OrderByDescending(d => d.Length))
            {
                cancellationToken.ThrowIfCancellationRequested();
                try
                {
                    if (!fileSystem.Directory.EnumerateFileSystemEntries(dir).Any())
                        fileSystem.Directory.Delete(dir);
                }
                catch (IOException) { /* directory not empty by the time Delete fires, or filesystem busy */ }
                catch (UnauthorizedAccessException) { /* DACL refuses the elevated process; rare but possible */ }
                catch (SecurityException) { /* permission denied at a higher tier */ }
            }
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            // A failure enumerating or sorting the tree (the folder vanishing
            // mid-walk, or a share violation surfaced by the enumerator itself
            // rather than by a per-directory Delete) is swallowed: it would
            // otherwise propagate into a caller's generic catch and flip an
            // already-committed delete or move to a hard error. Cancellation is
            // excluded so the caller still sees a requested stop.
        }
    }

    // 520 chars covers any practical Windows long path (260 standard +
    // headroom for the \\?\ prefix and the not-yet-created suffix the
    // caller may attach).
    private const int PathBufferLength = 520;
}
