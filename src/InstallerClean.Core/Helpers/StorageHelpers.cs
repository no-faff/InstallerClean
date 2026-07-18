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
        return Open(path, desired, disposition);
    }

    /// <summary>
    /// Opens <paramref name="path"/> for appending only, creating it if it does
    /// not exist, with the same reparse-point refusal as
    /// <see cref="OpenAtomic"/>. Returns null on any failure.
    ///
    /// The handle carries FILE_APPEND_DATA and not FILE_WRITE_DATA, so Win32
    /// resolves the end of the file and writes there as one atomic step and no
    /// concurrent writer can be handed the same offset. Opening for
    /// GENERIC_WRITE and seeking to the end does not have that property: the
    /// seek and the write are two steps, and two writers who seek between each
    /// other's calls both write at the same offset, so the second silently
    /// overwrites the first. The length can still be read (FILE_READ_ATTRIBUTES),
    /// which is what an append-only log needs to know whether it is fresh.
    /// </summary>
    internal static SafeFileHandle? OpenAtomicAppend(string path) =>
        Open(path,
            Kernel32.FILE_APPEND_DATA | Kernel32.FILE_READ_ATTRIBUTES,
            Kernel32.OPEN_ALWAYS);

    private static SafeFileHandle? Open(string path, uint desiredAccess, uint disposition)
    {
        if (string.IsNullOrEmpty(path)) return null;

        uint flags = Kernel32.FILE_FLAG_OPEN_REPARSE_POINT;

        var handle = Kernel32.CreateFile(
            path, desiredAccess, Kernel32.FILE_SHARE_ALL, IntPtr.Zero,
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
    /// Deletes the temp file a failed write-temp-then-rename left behind, for
    /// the two savers that pair with <see cref="OpenAtomic"/>. Each failure
    /// names a fresh temp file, so without this one every one of them strands
    /// another file in <c>%LOCALAPPDATA%</c> that no later run ever removes.
    /// Swallows everything it meets: the caller is already returning "the write
    /// did not land", and a cleanup that threw would escalate a handled failure
    /// into an unhandled one, on a thread-pool thread in the debounced case.
    /// </summary>
    internal static void TryDeleteTempFile(string path)
    {
        try
        {
            if (File.Exists(path))
                File.Delete(path);
        }
        catch
        {
            // A temp file that cannot be removed is litter, not a fault worth
            // failing or logging a second time for.
        }
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
    /// What an attribute read established about <paramref name="path"/> in
    /// <see cref="CheckReparsePoint"/>. Three states, not two, because
    /// "attributes say it is an ordinary file" and "the attributes could not be
    /// read" are opposite facts to a safety gate and a bool collapses them.
    /// </summary>
    internal enum ReparseCheck
    {
        /// <summary>The attributes were read and carry no reparse-point bit.</summary>
        No,
        /// <summary>The attributes were read and carry the reparse-point bit.</summary>
        Yes,
        /// <summary>
        /// The attributes could not be read, so neither of the above was
        /// established. A safety gate must refuse; a metadata reader may
        /// proceed and fail on its own terms.
        /// </summary>
        Unreadable,
    }

    /// <summary>
    /// Reads <paramref name="path"/>'s attributes to decide whether it is a
    /// junction or symlink. Move and Delete refuse source files in
    /// C:\Windows\Installer that have been replaced with a symlink because
    /// following the link would silently relocate an OS file out of System32.
    /// The check is best-effort against a TOCTOU swap; <see cref="OpenAtomic"/>
    /// is the race-free path for the write side.
    ///
    /// A path with nothing at it is <see cref="ReparseCheck.No"/> rather than
    /// <see cref="ReparseCheck.Unreadable"/>: absence is not unreadability. Win32
    /// documents GetFileAttributes as returning the attributes OF a symbolic link
    /// rather than of its target, which is the only reason the reparse bit is
    /// observable here at all, so a link whose target is gone still answers Yes
    /// and cannot hide behind a not-found. What is left on the not-found side is
    /// a path that names nothing, where the operation about to run would fail on
    /// its own with a truthful error. Every other failure (a refused DACL, a
    /// malformed or over-long path, a device error) leaves the question open and
    /// answers Unreadable.
    ///
    /// <paramref name="error"/> carries the exception behind an Unreadable so a
    /// caller can put it in the crash log; it is null for the other two.
    /// </summary>
    internal static ReparseCheck CheckReparsePoint(string path, out Exception? error)
    {
        error = null;
        try
        {
            var attrs = File.GetAttributes(path);
            return (attrs & FileAttributes.ReparsePoint) != 0 ? ReparseCheck.Yes : ReparseCheck.No;
        }
        catch (FileNotFoundException)
        {
            return ReparseCheck.No;
        }
        catch (DirectoryNotFoundException)
        {
            return ReparseCheck.No;
        }
        catch (Exception ex)
        {
            error = ex;
            return ReparseCheck.Unreadable;
        }
    }

    /// <summary>
    /// The tolerant form, for callers reading metadata rather than guarding an
    /// action: an unreadable attribute read answers false, so the caller goes on
    /// and meets the same failure where it actually matters. Any caller deciding
    /// whether to MOVE or DELETE a file wants
    /// <see cref="CheckReparsePoint(string, out Exception?)"/> instead, and its
    /// Unreadable state.
    /// </summary>
    internal static bool IsReparsePoint(string path) =>
        CheckReparsePoint(path, out _) == ReparseCheck.Yes;

}
