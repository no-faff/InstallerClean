using Microsoft.Win32.SafeHandles;
using InstallerClean.Interop.Native;

namespace InstallerClean.Helpers;

internal static class StorageHelpers
{
    /// <summary>
    /// Opens <paramref name="path"/> with FILE_FLAG_OPEN_REPARSE_POINT
    /// and returns the handle only if the file is real, not a reparse
    /// point. Returns null on any failure (open fails, attribute read
    /// fails, file is a reparse point). Final-component-only:
    /// directory symlinks in parents are still followed.
    /// </summary>
    /// <param name="mode">Caller picks the FileMode:
    /// <list type="bullet">
    ///   <item>OpenExisting: read or read/write an existing real file.</item>
    ///   <item>OpenAlways: open existing, create if missing (e.g. append-only log).</item>
    ///   <item>CreateAlways: create a fresh real file, truncating any
    ///   pre-existing content (e.g. write to a freshly-named temp file).</item>
    /// </list>
    /// </param>
    internal static SafeFileHandle? OpenAtomic(
        string path, FileAccess access, AtomicOpenMode mode)
    {
        if (string.IsNullOrEmpty(path)) return null;

        uint desired = access switch
        {
            FileAccess.Read => Kernel32.GENERIC_READ,
            FileAccess.Write => Kernel32.GENERIC_WRITE,
            FileAccess.ReadWrite => Kernel32.GENERIC_READ | Kernel32.GENERIC_WRITE,
            _ => Kernel32.GENERIC_READ,
        };
        uint disposition = mode switch
        {
            AtomicOpenMode.OpenExisting => Kernel32.OPEN_EXISTING,
            AtomicOpenMode.OpenAlways   => Kernel32.OPEN_ALWAYS,
            AtomicOpenMode.CreateAlways => Kernel32.CREATE_ALWAYS,
            _ => Kernel32.OPEN_EXISTING,
        };
        uint flags = Kernel32.FILE_FLAG_OPEN_REPARSE_POINT;

        var handle = Kernel32.CreateFile(
            path, desired, Kernel32.FILE_SHARE_ALL, IntPtr.Zero,
            disposition, flags, IntPtr.Zero);
        if (handle.IsInvalid) return null;

        if (!Kernel32.GetFileInformationByHandle(handle, out var info))
        {
            handle.Dispose();
            return null;
        }
        if ((info.dwFileAttributes & Kernel32.FILE_ATTRIBUTE_REPARSE_POINT) != 0)
        {
            handle.Dispose();
            return null;
        }
        return handle;
    }

    internal enum AtomicOpenMode
    {
        /// <summary>Fail if the file does not exist.</summary>
        OpenExisting,
        /// <summary>Open existing or create empty if missing. Existing
        /// content is preserved (typical for append-only logs).</summary>
        OpenAlways,
        /// <summary>Always create a fresh file, truncating any
        /// pre-existing content.</summary>
        CreateAlways,
    }

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
    /// True if <paramref name="path"/> is a junction or symlink. Used
    /// by Move/Delete to refuse source files in C:\Windows\Installer
    /// that have been replaced with a symlink (moving the symlink
    /// would silently relocate an OS file out of System32). New
    /// callers should prefer <see cref="OpenAtomic"/> which is
    /// race-free.
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

}
