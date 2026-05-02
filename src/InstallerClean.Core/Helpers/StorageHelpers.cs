using InstallerClean.Interop.Native;

namespace InstallerClean.Helpers;

internal static class StorageHelpers
{
    /// <summary>
    /// Returns the number of bytes available to the current user at
    /// <paramref name="path"/>, or null if the space cannot be
    /// determined. Handles local drives, UNC shares and mapped drives
    /// uniformly via Kernel32.GetDiskFreeSpaceEx.
    /// </summary>
    internal static long? GetAvailableFreeSpace(string path)
    {
        if (string.IsNullOrEmpty(path)) return null;

        try
        {
            if (!Kernel32.GetDiskFreeSpaceEx(path, out var free, out _, out _))
                return null;
            return (long)free;
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// True if <paramref name="path"/> exists and is a reparse point
    /// (NTFS junction or symlink). Used to refuse elevated writes
    /// through paths an attacker could redirect into a sensitive
    /// location.
    /// </summary>
    internal static bool IsReparsePoint(string path)
    {
        try
        {
            var attrs = File.GetAttributes(path);
            return (attrs & FileAttributes.ReparsePoint) != 0;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// True if either <paramref name="targetPath"/> or its IMMEDIATE parent
    /// directory is a reparse point. Use this immediately before any
    /// elevated write to a predictable %LOCALAPPDATA% path so neither a
    /// folder-level junction nor a file-level symlink at that depth can
    /// redirect the write into a sensitive location.
    /// </summary>
    /// <remarks>
    /// SCOPE: this is intentionally a TWO-LEVEL check (file + parent),
    /// not a full chain-to-root walk. The threat model is "same-account
    /// attacker plants a junction in our owned folder"; a junction at a
    /// deeper ancestor (e.g. at %LOCALAPPDATA% itself or at the user
    /// profile) is outside the threat model because that ancestor is
    /// owner-only DACL'd by Windows and the same-account attacker
    /// already owns it. Callers protecting paths with deeper ownership
    /// boundaries should walk further themselves; for InstallerClean's
    /// settings + crash log, file + parent is the right depth.
    /// LIMITATION: check-then-write is not atomic; a sufficiently fast
    /// attacker could swap between the check and the write. The DACL on
    /// %LOCALAPPDATA% closes the realistic race window in practice, but
    /// the guarantee is best-effort rather than absolute.
    /// </remarks>
    internal static bool IsRedirected(string targetPath)
    {
        if (string.IsNullOrEmpty(targetPath)) return false;
        if (IsReparsePoint(targetPath)) return true;
        var folder = Path.GetDirectoryName(targetPath);
        return !string.IsNullOrEmpty(folder)
            && Directory.Exists(folder)
            && IsReparsePoint(folder);
    }
}
