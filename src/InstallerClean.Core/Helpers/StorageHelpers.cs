using System.Runtime.InteropServices;
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
    ///   <item>CreateAlways: create a fresh real file, truncating any
    ///   pre-existing content (e.g. write to a freshly-named temp file).</item>
    ///   <item>CreateNew: create a file that did not exist, failing where one
    ///   does. A file created this way takes the folder's inherited permissions.</item>
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
            AtomicOpenMode.CreateAlways => Kernel32.CREATE_ALWAYS,
            AtomicOpenMode.CreateNew => Kernel32.CREATE_NEW,
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

    /// <summary>
    /// Creates <paramref name="path"/> as a new file, with the same reparse-point refusal as
    /// <see cref="OpenAtomic"/>, shared with nothing, and deleted by Windows as the handle
    /// closes, however the process holding it ends. Returns null on any failure, a file
    /// already at the name among them, with the error Windows gave in
    /// <paramref name="error"/>, or 0 where the name is a reparse point.
    ///
    /// Shared with nothing, the file cannot be opened or deleted by anyone else while it is
    /// held.
    /// </summary>
    internal static SafeFileHandle? CreateHeld(string path, out int error) =>
        Open(path, Kernel32.GENERIC_WRITE, Kernel32.CREATE_NEW, out error, share: 0, Kernel32.FILE_FLAG_DELETE_ON_CLOSE);

    /// <summary>
    /// Opens the existing file <paramref name="path"/> to read it, with the same reparse-point
    /// refusal as <see cref="OpenAtomic"/>, shared only for reading, so nobody else can write,
    /// rename or delete it while the handle is held. Returns null on any failure, another
    /// handle already open to write or delete it among them, with the error Windows gave in
    /// <paramref name="error"/>, or 0 where the name is a reparse point.
    /// </summary>
    internal static SafeFileHandle? OpenHeldToRead(string path, out int error) =>
        Open(path, Kernel32.GENERIC_READ, Kernel32.OPEN_EXISTING, out error, share: Kernel32.FILE_SHARE_READ);

    private static SafeFileHandle? Open(
        string path, uint desiredAccess, uint disposition, uint share = Kernel32.FILE_SHARE_ALL, uint moreFlags = 0) =>
        Open(path, desiredAccess, disposition, out _, share, moreFlags);

    // Answers null on any failure, with the error Windows gave in error, or 0 where the
    // path is empty or its final component is a reparse point.
    private static SafeFileHandle? Open(
        string path, uint desiredAccess, uint disposition, out int error,
        uint share = Kernel32.FILE_SHARE_ALL, uint moreFlags = 0)
    {
        error = 0;
        if (string.IsNullOrEmpty(path)) return null;

        uint flags = Kernel32.FILE_FLAG_OPEN_REPARSE_POINT | moreFlags;

        var handle = Kernel32.CreateFile(
            path, desiredAccess, share, IntPtr.Zero,
            disposition, flags, IntPtr.Zero);
        if (handle.IsInvalid)
        {
            error = Marshal.GetLastWin32Error();
            return null;
        }

        if (!Kernel32.GetFileInformationByHandle(handle, out var info))
        {
            error = Marshal.GetLastWin32Error();
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
        /// <summary>Always create a fresh file, truncating any
        /// pre-existing content.</summary>
        CreateAlways,
        /// <summary>Create a file that does not exist yet, and fail where
        /// one does.</summary>
        CreateNew,
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
    /// Whether <paramref name="path"/> names a share rather than a local
    /// volume, decided from the spelling alone and touching nothing.
    /// </summary>
    /// <remarks>
    /// TWO JOBS, AND THE FIRST IS THE ONE THAT MATTERS. It is true on its own
    /// terms: a share is not a local volume, so it cannot be the one the
    /// installer cache is on, and no query could make it so. That it also keeps
    /// the one call that goes to the network away from callers that must not
    /// block is the second job, not the reason.
    ///
    /// BOTH SEPARATORS, because Windows accepts either and a user types into a
    /// TextBox. <c>//server/share</c> is the same path as
    /// <c>\\server\share</c> and a test written against backslashes alone lets
    /// it through.
    ///
    /// THE DEVICE PREFIXES SPLIT, and the split is not the obvious one.
    /// <c>\\?\</c> and <c>\\.\</c> open with the same two separators a share
    /// does, and <c>\\?\C:\Backup</c> is an ordinary local path wearing a
    /// prefix. But <c>\\?\UNC\server\share</c> is a share wearing the same
    /// prefix, and Win32 parses it as one: GetVolumePathName's own example
    /// table has <c>\\?\UNC\W:\Windows</c> failing with error 123 because the
    /// share could not be reached. So the prefix is not the answer; what
    /// follows it is.
    /// </remarks>
    internal static bool IsRemotePath(string path)
    {
        if (path is null || path.Length < 2) return false;
        if (!IsSeparator(path[0]) || !IsSeparator(path[1])) return false;

        // \\?\ or \\.\ : a device path, local unless UNC follows.
        if (path.Length >= 4 && (path[2] == '?' || path[2] == '.') && IsSeparator(path[3]))
            return path.Length >= 8
                && path.AsSpan(4, 3).Equals("UNC", StringComparison.OrdinalIgnoreCase)
                && IsSeparator(path[7]);

        return true;

        static bool IsSeparator(char c) => c == '\\' || c == '/';
    }

    /// <summary>
    /// The mount point of the volume hosting <paramref name="path"/>, or null
    /// where it could not be established.
    /// </summary>
    /// <remarks>
    /// WHAT IT IS FOR, because a path root looks like the same answer and is
    /// not. <c>Path.GetPathRoot</c> answers <c>C:\</c> for a path under a
    /// volume mounted into a folder on C:, so two paths on different storage
    /// compare equal on it. This answers the mount point, which is the volume,
    /// and it is what a Move has to compare: whether the space comes back and
    /// whether there is room are both properties of the volume rather than of
    /// the letter.
    ///
    /// NULL RATHER THAN A GUESS ON ANY FAILURE, which is the direction every
    /// caller here needs, in both of its senses: a comparison against null
    /// establishes nothing, so a caller omits a claim instead of making a wrong
    /// one, AND the free-space measurement then runs rather than being skipped.
    /// The second is the one the app exists for.
    ///
    /// THE BUFFER IS SIZED FROM THE PATH AND NOT FIXED, and the reason is a
    /// documented silent failure rather than a fear of long paths. Win32: "There
    /// are certain special cases that do not return a trailing backslash. These
    /// occur when the output buffer length is one character too short... If the
    /// output buffer is more than one character too short, the function will
    /// fail and return an error." So a short buffer does not reliably fail. At
    /// exactly one character short it SUCCEEDS and hands back a string with the
    /// trailing backslash missing, which compares unequal to a correct one and
    /// would answer "a different volume" for the volume it was asked about.
    /// Win32's own advice is to size the buffer against GetFullPathName's
    /// answer, which is what the caller has already resolved and passed in; the
    /// two extra characters cover a backslash the input did not carry and the
    /// terminator. MAX_PATH is the floor because the returned mount point is
    /// not always a prefix of the input (a path through a junction answers for
    /// the volume the junction points at, which can be spelled longer).
    /// </remarks>
    internal static string? GetVolumeMountPoint(string path)
    {
        if (string.IsNullOrWhiteSpace(path)) return null;

        try
        {
            var buffer = new char[Math.Max(261, path.Length + 2)];
            if (!Kernel32.GetVolumePathName(path, buffer, (uint)buffer.Length))
                return null;

            var end = Array.IndexOf(buffer, '\0');
            var value = new string(buffer, 0, end < 0 ? buffer.Length : end);
            return value.Length == 0 ? null : value;
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// What kind of storage is mounted at <paramref name="volumeMountPoint"/>,
    /// which is <see cref="GetVolumeMountPoint"/>'s answer rather than a drive
    /// letter.
    /// </summary>
    /// <remarks>
    /// WRITTEN OUT RATHER THAN CAST. The Win32 constants and
    /// <see cref="DriveType"/> share their numbering, so a cast compiles and is
    /// right today; it is also a dependency on two separate numberings staying
    /// aligned that nothing would report if either moved. The switch costs
    /// nothing and says what the correspondence is.
    ///
    /// A trailing separator is appended where the caller's value lacks one,
    /// because Win32 states outright that a trailing backslash is required and
    /// answers DRIVE_NO_ROOT_DIR without one. GetVolumeMountPoint's answer
    /// carries it; a hand-written mount point may not.
    /// </remarks>
    internal static DriveType GetDriveKind(string volumeMountPoint)
    {
        if (string.IsNullOrWhiteSpace(volumeMountPoint)) return DriveType.Unknown;

        try
        {
            var root = volumeMountPoint.EndsWith('\\') || volumeMountPoint.EndsWith('/')
                ? volumeMountPoint
                : volumeMountPoint + '\\';

            return Kernel32.GetDriveType(root) switch
            {
                Kernel32.DRIVE_NO_ROOT_DIR => DriveType.NoRootDirectory,
                Kernel32.DRIVE_REMOVABLE => DriveType.Removable,
                Kernel32.DRIVE_FIXED => DriveType.Fixed,
                Kernel32.DRIVE_REMOTE => DriveType.Network,
                Kernel32.DRIVE_CDROM => DriveType.CDRom,
                Kernel32.DRIVE_RAMDISK => DriveType.Ram,
                _ => DriveType.Unknown,
            };
        }
        catch
        {
            return DriveType.Unknown;
        }
    }

    /// <summary>
    /// Whether the drive whose root is <paramref name="driveRoot"/>, a drive letter, its colon
    /// and a backslash, answers that it is not ready (<see cref="Kernel32.ERROR_NOT_READY"/>) or
    /// that it has no media in it (<see cref="Kernel32.ERROR_NO_MEDIA_IN_DRIVE"/>), as an optical
    /// drive with no disc in it and a card reader with no card do. False for every other answer,
    /// a call that throws included. Asked with no critical-error box
    /// (<see cref="WithNoCriticalErrorBox"/>).
    /// </summary>
    internal static bool IsNotReady(string driveRoot)
    {
        try
        {
            using var _ = WithNoCriticalErrorBox();
            return !Kernel32.GetVolumeInformation(
                    driveRoot, IntPtr.Zero, 0, IntPtr.Zero, IntPtr.Zero, IntPtr.Zero, IntPtr.Zero, 0)
                && IsNotReadyAnswer(System.Runtime.InteropServices.Marshal.GetLastWin32Error());
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Whether <paramref name="error"/>, the error a drive answered, is one <see cref="IsNotReady"/>
    /// takes as the drive not being ready: <see cref="Kernel32.ERROR_NOT_READY"/> or
    /// <see cref="Kernel32.ERROR_NO_MEDIA_IN_DRIVE"/>, and no other.
    /// </summary>
    internal static bool IsNotReadyAnswer(int error) =>
        error is Kernel32.ERROR_NOT_READY or Kernel32.ERROR_NO_MEDIA_IN_DRIVE;

    /// <summary>
    /// Opens <paramref name="path"/> for its metadata alone, with no critical-error box on the
    /// calling thread while it opens (<see cref="WithNoCriticalErrorBox"/>), so a path on a drive
    /// with no disc or card in it answers with an error rather than Windows asking for one.
    /// <paramref name="error"/> is the error the open answered where the handle is invalid, read
    /// before anything else on the thread can overwrite it, and 0 where it opened: the last error
    /// after a successful open is whatever the last failure anywhere left behind. Read the error
    /// from <paramref name="error"/> and never from <c>Marshal.GetLastWin32Error</c> after this
    /// returns, which answers about the call that put the thread's mode back.
    ///
    /// <c>FileIdentityReader</c>'s reads of a path's identity and its volume, and the final-path
    /// resolution <see cref="InstallerClean.Services.InstallerCacheHelpers"/> makes
    /// (<c>Win32FinalPathKernel</c>), open here. Every other read in the app runs under the process's error mode, which each host sets
    /// as it starts so that no critical-error box is shown
    /// (<see cref="TurnOffCriticalErrorBoxForTheProcess"/>). This open sets the thread's own mode
    /// as well, so it shows no box where Core runs without a host.
    /// </summary>
    /// <remarks>
    /// NO ACCESS BITS AT ALL. The metadata a caller asks for needs none, and requesting read
    /// access would fail on a file another process has open without sharing it, which for the
    /// Installer folder means anything msiexec is working on. FILE_SHARE_ALL for the other half of
    /// that: an installer holding its own cached package must not make this open fail.
    ///
    /// NO FILE_FLAG_OPEN_REPARSE_POINT: links are followed, so a path that reaches a file through
    /// a junction opens the file. FILE_FLAG_BACKUP_SEMANTICS so a path that names a directory
    /// opens rather than failing; a caller comparing files reads such a handle as a file no
    /// candidate is, and a caller asking about a folder needs it to open.
    /// </remarks>
    internal static SafeFileHandle OpenForMetadata(string path, out int error)
    {
        using var noBox = WithNoCriticalErrorBox();
        var handle = Kernel32.CreateFile(
            path,
            0,
            Kernel32.FILE_SHARE_ALL,
            IntPtr.Zero,
            Kernel32.OPEN_EXISTING,
            Kernel32.FILE_FLAG_BACKUP_SEMANTICS,
            IntPtr.Zero);
        error = handle.IsInvalid ? System.Runtime.InteropServices.Marshal.GetLastWin32Error() : 0;
        return handle;
    }

    /// <summary>
    /// Turns off, for the whole process, the box Windows shows when a read reaches a drive with
    /// no disc or card in it (<see cref="Kernel32.SEM_FAILCRITICALERRORS"/>), keeping the process's
    /// other error-mode flags, so a read the app makes of such a drive answers with an error
    /// instead of waiting on the person. Each host calls it once, first thing as it starts;
    /// nothing in Core calls it, so Core run without a host changes no process state. A call that
    /// cannot be made changes nothing.
    /// </summary>
    internal static void TurnOffCriticalErrorBoxForTheProcess()
    {
        try
        {
            Kernel32.SetErrorMode(Kernel32.GetErrorMode() | Kernel32.SEM_FAILCRITICALERRORS);
        }
        catch
        {
            // Not on Windows: there is no box to turn off.
        }
    }

    /// <summary>
    /// Turns off, for the calling thread, the box Windows shows when a read reaches a drive with
    /// no disc or card in it (<see cref="Kernel32.SEM_FAILCRITICALERRORS"/>), so the read answers
    /// with an error instead of waiting on the person; disposing the answer puts the thread's own
    /// mode back. The thread's other error-mode flags are kept as they are, and the process's mode
    /// and every other thread's are left alone. Where the call fails or cannot be made, nothing is
    /// changed and nothing is put back.
    /// </summary>
    internal static NoCriticalErrorBox WithNoCriticalErrorBox()
    {
        try
        {
            var old = Kernel32.GetThreadErrorMode();
            return Kernel32.SetThreadErrorMode(old | Kernel32.SEM_FAILCRITICALERRORS, out _)
                ? new NoCriticalErrorBox(old, set: true)
                : default;
        }
        catch
        {
            return default;
        }
    }

    /// <summary>
    /// The thread's error mode as <see cref="WithNoCriticalErrorBox"/> found it, put back on dispose
    /// where that call set a new one. Disposed on the thread that made it.
    /// </summary>
    internal readonly struct NoCriticalErrorBox(uint old, bool set) : IDisposable
    {
        public void Dispose()
        {
            if (set) Kernel32.SetThreadErrorMode(old, out _);
        }
    }

    /// <summary>
    /// The names the folder's entry for the file at <paramref name="path"/> holds: its
    /// name, and its 8.3 short name where it has one other than that name. Null where
    /// Windows does not answer.
    /// </summary>
    /// <remarks>
    /// A SHORT NAME NEED NOT CARRY A '~'. One given by hand can be any name the 8.3 form
    /// allows, SETUP.MSI among them, so it is read from the entry
    /// (<see cref="Kernel32.FindFirstFile"/>).
    /// </remarks>
    internal static IReadOnlyList<string>? GetNamesInFolder(string path)
    {
        if (string.IsNullOrWhiteSpace(path)) return null;

        try
        {
            var search = Kernel32.FindFirstFile(path, out var entry);
            if (search == InvalidHandle) return null;
            Kernel32.FindClose(search);

            string name, shortName;
            unsafe
            {
                name = NullTerminated(entry.cFileName, Kernel32.WIN32_FIND_DATA.FileNameLength);
                shortName = NullTerminated(
                    entry.cAlternateFileName, Kernel32.WIN32_FIND_DATA.AlternateFileNameLength);
            }
            if (name.Length == 0) return null;

            return shortName.Length == 0 ? [name] : [name, shortName];
        }
        catch
        {
            return null;
        }

        // The UTF-16 code units of a buffer up to its first null, or the whole buffer
        // where it holds none.
        static unsafe string NullTerminated(ushort* buffer, int length)
        {
            var end = 0;
            while (end < length && buffer[end] != 0) end++;
            return new string((char*)buffer, 0, end);
        }
    }

    /// <summary>FindFirstFile's failure return.</summary>
    private static readonly IntPtr InvalidHandle = new(-1);

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
    /// What the attribute read in <see cref="CheckReparsePoint"/> established about
    /// the path it was given. Three states, not two, because
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
    /// junction or symlink. Move and Delete refuse a source in
    /// C:\Windows\Installer that has been replaced with one.
    ///
    /// A move can read straight through a link. .NET calls MoveFileEx with
    /// MOVEFILE_COPY_ALLOWED (dotnet/runtime, Interop.MoveFileEx.cs), Win32
    /// documents that flag as simulating a cross-volume move with CopyFile plus
    /// DeleteFile, and CopyFile documents that "if the source file is a symbolic
    /// link, the actual file copied is the target of the symbolic link". So a
    /// link planted in the cache and pointing anywhere this elevated process can
    /// read copies THAT file's contents into a folder the user picked, which is
    /// an arbitrary read wearing the shape of a relocation. Whether a same-volume
    /// rename would follow the link is documented neither way and does not need
    /// to be: the flag is always set, so the copy path is always available.
    ///
    /// Delete has no equivalent, DeleteFile being documented to remove the link
    /// and not the target, and refuses on the plainer ground that a link is not
    /// the file the app was told about.
    ///
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
