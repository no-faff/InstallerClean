using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;

namespace InstallerClean.Interop.Native;

/// <summary>
/// P/Invoke surface for kernel32.dll.
///
/// Every declaration names its entry point exactly, and every one whose entry point
/// is a W function, or that passes a string or a char[], is CharSet.Unicode: a
/// string then reaches the function as a pointer to UTF-16 characters and a char as
/// two bytes, as a W function reads them. Under the default ANSI set each char
/// would be converted to one byte, and the function would read the result as
/// something else. A char[] buffer is passed as a pointer to its first element,
/// and [Out] copies what the function wrote back into the array wherever the
/// marshaller copies the array rather than pinning it.
///
/// Every struct passed here holds only integers, pointers and other such structs,
/// so its layout in memory is the one Windows reads and writes, and the marshaller
/// converts nothing in it. The marshaller would convert a bool field to a
/// four-byte BOOL and, under the default ANSI set, a char field to one byte, so
/// neither is declared.
/// </summary>
internal static class Kernel32
{
    private const string Library = "kernel32.dll";

    /// <summary>
    /// Opens a file or directory. Declared here rather than reached
    /// through FileStream because the callers need creation flags and
    /// access bits the framework does not surface:
    /// FILE_FLAG_OPEN_REPARSE_POINT for the reparse-point guards,
    /// FILE_FLAG_BACKUP_SEMANTICS to open a directory handle at all, and
    /// FILE_APPEND_DATA granted without FILE_WRITE_DATA.
    /// </summary>
    [DllImport(Library, EntryPoint = "CreateFileW", CharSet = CharSet.Unicode, ExactSpelling = true, SetLastError = true)]
    public static extern SafeFileHandle CreateFile(
        string lpFileName,
        uint dwDesiredAccess,
        uint dwShareMode,
        IntPtr lpSecurityAttributes,
        uint dwCreationDisposition,
        uint dwFlagsAndAttributes,
        IntPtr hTemplateFile);

    /// <summary>
    /// Resolves a file handle to its final canonical path, expanding
    /// junctions and symlinks. The output buffer is sized in characters.
    /// The return value counts characters, but on the two outcomes it
    /// counts them differently: on success it is the length written,
    /// EXCLUDING the null terminator; when the buffer was too small it is
    /// the size required, INCLUDING the null terminator. A retry therefore
    /// allocates exactly the returned count, not one more.
    /// </summary>
    [DllImport(Library, EntryPoint = "GetFinalPathNameByHandleW", CharSet = CharSet.Unicode, ExactSpelling = true, SetLastError = true)]
    public static extern uint GetFinalPathNameByHandle(
        SafeFileHandle hFile,
        [Out] char[] lpszFilePath,
        uint cchFilePath,
        uint dwFlags);

    /// <summary>
    /// Retrieves disk free-space figures for the volume hosting
    /// <paramref name="lpDirectoryName"/>. Handles local drives, UNC
    /// shares and mapped drives uniformly.
    /// </summary>
    [DllImport(Library, EntryPoint = "GetDiskFreeSpaceExW", CharSet = CharSet.Unicode, ExactSpelling = true, SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool GetDiskFreeSpaceEx(
        string lpDirectoryName,
        out ulong lpFreeBytesAvailableToCaller,
        out ulong lpTotalNumberOfBytes,
        out ulong lpTotalNumberOfFreeBytes);

    /// <summary>
    /// The mount point of the volume hosting <paramref name="lpszFileName"/>:
    /// <c>C:\</c> for an ordinary path on the system drive, and the mount
    /// directory itself for a path under a volume mounted into a folder.
    ///
    /// IT IS THE ONLY THING THAT SEPARATES A DRIVE LETTER FROM A VOLUME.
    /// <c>Path.GetPathRoot</c> answers <c>C:\</c> for both, so a destination
    /// under a mount point compares equal to the system drive while sitting on
    /// different storage. Everything a Move needs to know, whether the space
    /// comes back and whether there is room, turns on the volume rather than on
    /// the letter.
    ///
    /// THE PATH NEED NOT EXIST, which is what lets a caller ask about a folder
    /// the Move is about to create. Win32 documents that trailing path elements
    /// which are invalid are ignored, and that a valid volume with an invalid
    /// directory name under it still succeeds and returns that volume; its own
    /// example gives <c>G:\invalid</c> returning <c>G:\</c>. So this needs none
    /// of the walk up to the nearest existing ancestor that
    /// GetDiskFreeSpaceEx does need, and must not be given one: a second
    /// implementation of that walk could only diverge from this one.
    ///
    /// A REMOTE PATH IS VALIDATED OVER THE NETWORK. Win32: "If a network share
    /// is specified, GetVolumePathName returns the shortest path for which
    /// GetDriveType returns DRIVE_REMOTE, which means that the path is
    /// validated as a remote drive that exists, which the current user can
    /// access." That is a round trip, and on a share that is not answering it
    /// is an SMB timeout, so no caller may make this call on the dispatcher.
    /// <see cref="Helpers.StorageHelpers.IsRemotePath"/> is what keeps the
    /// shapes that can do it away from here.
    ///
    /// The buffer's size goes in <paramref name="cchBufferLength"/>, and the caller
    /// passes the array's own length there, exactly as GetFinalPathNameByHandle
    /// above is told its size. The <c>MsiEnum*</c> buffers are the opposite case,
    /// taking no count parameter; both declarations match their native signatures.
    /// </summary>
    [DllImport(Library, EntryPoint = "GetVolumePathNameW", CharSet = CharSet.Unicode, ExactSpelling = true, SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool GetVolumePathName(
        string lpszFileName,
        [Out] char[] lpszVolumePathName,
        uint cchBufferLength);

    /// <summary>
    /// Every path the volume named by <paramref name="lpszVolumeName"/> is
    /// mounted at: drive roots and directory mount points alike. The volume is
    /// named in its GUID form, <c>\\?\Volume{...}\</c>, and Win32 requires the
    /// trailing backslash.
    ///
    /// THE OUTPUT IS A DOUBLE-NULL-TERMINATED LIST AND AN EMPTY ONE IS AN
    /// ANSWER. A volume can be mounted nowhere, which is ordinary rather than
    /// exceptional: an EFI system partition is the everyday case, and Windows
    /// writes a device-form path for such a volume precisely because there is no
    /// mount point to name it by. A caller must therefore tell a call that
    /// SUCCEEDED with nothing to report from one that FAILED, because the two
    /// arrive as the same empty result and mean opposite things.
    ///
    /// The double-call buffer pattern is the one Win32 documents: a buffer too
    /// small fails with ERROR_MORE_DATA and writes the required character count
    /// to <paramref name="lpcchReturnLength"/>, which includes both terminators.
    /// </summary>
    [DllImport(Library, EntryPoint = "GetVolumePathNamesForVolumeNameW", CharSet = CharSet.Unicode, ExactSpelling = true, SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool GetVolumePathNamesForVolumeName(
        string lpszVolumeName,
        [Out] char[] lpszVolumePathNames,
        uint cchBufferLength,
        out uint lpcchReturnLength);

    /// <summary>
    /// Opens an enumeration of the machine's volumes and writes the first
    /// volume's GUID name, <c>\\?\Volume{...}\</c>, into the buffer. Returns
    /// INVALID_HANDLE_VALUE on failure; the handle must be closed with
    /// <see cref="FindVolumeClose"/>.
    ///
    /// IT ENUMERATES VOLUMES AND NOT MS-DOS DEVICE NAMES, WHICH IS THE POINT.
    /// The alternative for finding the volume behind a device name is to list
    /// every MS-DOS name and filter, which walks a set holding far more than
    /// volumes and asks the caller to recognise which entries are which. This
    /// walks exactly the volumes, so a match cannot be a name that was never a
    /// volume to begin with.
    /// </summary>
    [DllImport(Library, EntryPoint = "FindFirstVolumeW", CharSet = CharSet.Unicode, ExactSpelling = true, SetLastError = true)]
    public static extern IntPtr FindFirstVolume(
        [Out] char[] lpszVolumeName,
        uint cchBufferLength);

    /// <summary>
    /// The next volume in an enumeration opened by
    /// <see cref="FindFirstVolume"/>. Returns false at the end of the list, with
    /// <see cref="ERROR_NO_MORE_FILES"/> as the last error, and false with any
    /// other code for a real failure. The two are told apart by the code, never
    /// by the false.
    /// </summary>
    [DllImport(Library, EntryPoint = "FindNextVolumeW", CharSet = CharSet.Unicode, ExactSpelling = true, SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool FindNextVolume(
        IntPtr hFindVolume,
        [Out] char[] lpszVolumeName,
        uint cchBufferLength);

    /// <summary>Closes a volume enumeration opened by <see cref="FindFirstVolume"/>.</summary>
    [DllImport(Library, EntryPoint = "FindVolumeClose", ExactSpelling = true, SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool FindVolumeClose(IntPtr hFindVolume);

    /// <summary>
    /// The NT object the given MS-DOS device name is a symbolic link to:
    /// <c>C:</c> answers something of the form <c>\Device\HarddiskVolume3</c>,
    /// and so does a volume's own GUID name with its enclosing backslashes and
    /// trailing separator removed. Returns the number of characters written, or
    /// zero on failure.
    ///
    /// IT ONLY GOES THIS WAY ROUND. There is no call that takes an NT device
    /// name and answers which volume it is, so a caller starting from a device
    /// name walks the volumes and asks this about each one. Trying to reverse it
    /// by walking the drive letters instead answers nothing for a volume that
    /// has no letter, and those are common: an EFI system partition and a
    /// recovery partition both sit on an ordinary machine without one.
    ///
    /// The output is a double-null-terminated list, one device name usually but
    /// not necessarily, so a caller reads the first entry rather than the whole
    /// buffer as a single string.
    /// </summary>
    [DllImport(Library, EntryPoint = "QueryDosDeviceW", CharSet = CharSet.Unicode, ExactSpelling = true, SetLastError = true)]
    public static extern uint QueryDosDevice(
        string lpDeviceName,
        [Out] char[] lpTargetPath,
        uint ucchMax);

    /// <summary>
    /// Opens a search of the folder <paramref name="lpFileName"/> is in and writes the
    /// first entry it matches into <paramref name="lpFindFileData"/>. Given a path with
    /// no wildcard, that is the entry of the file the path names, found by its name or
    /// by its short name. Returns INVALID_HANDLE_VALUE on failure; the handle must be
    /// closed with <see cref="FindClose"/>.
    ///
    /// The entry holds the name and the short name the file system keeps for it.
    /// </summary>
    [DllImport(Library, EntryPoint = "FindFirstFileW", CharSet = CharSet.Unicode, ExactSpelling = true, SetLastError = true)]
    public static extern IntPtr FindFirstFile(string lpFileName, out WIN32_FIND_DATA lpFindFileData);

    /// <summary>Closes a search opened by <see cref="FindFirstFile"/>.</summary>
    [DllImport(Library, EntryPoint = "FindClose", ExactSpelling = true, SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool FindClose(IntPtr hFindFile);

    /// <summary>
    /// One folder entry as <see cref="FindFirstFile"/> writes it: the file's name in
    /// <see cref="cFileName"/> and, where the file has an 8.3 short name other than that
    /// name, the short name in <see cref="cAlternateFileName"/>, which is empty
    /// otherwise. Each is null-terminated inside its buffer.
    ///
    /// The two names are fixed buffers of the sizes the API declares, 260 and 14
    /// UTF-16 code units, held as ushort. A fixed buffer's type takes the containing
    /// struct's character set, ANSI unless the struct says otherwise, and the
    /// marshaller converts a char field in an ANSI type to one byte; a ushort is
    /// copied as it is. So the struct is byte-for-byte the WIN32_FIND_DATAW the API
    /// writes, whatever character set anybody gives it.
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    public unsafe struct WIN32_FIND_DATA
    {
        public uint dwFileAttributes;
        public FILETIME ftCreationTime;
        public FILETIME ftLastAccessTime;
        public FILETIME ftLastWriteTime;
        public uint nFileSizeHigh;
        public uint nFileSizeLow;
        public uint dwReserved0;
        public uint dwReserved1;
        public fixed ushort cFileName[FileNameLength];
        public fixed ushort cAlternateFileName[AlternateFileNameLength];

        /// <summary>The length of <see cref="cFileName"/>, MAX_PATH.</summary>
        public const int FileNameLength = 260;

        /// <summary>The length of <see cref="cAlternateFileName"/>.</summary>
        public const int AlternateFileNameLength = 14;
    }

    /// <summary>
    /// The system time as it stood at the last tick of the system clock, as a
    /// FILETIME in UTC. It moves only at a tick, so it stands behind the time
    /// <see cref="GetSystemTimePreciseAsFileTime"/> returns between ticks.
    /// </summary>
    [DllImport(Library, EntryPoint = "GetSystemTimeAsFileTime", ExactSpelling = true)]
    public static extern void GetSystemTimeAsFileTime(out long lpSystemTimeAsFileTime);

    /// <summary>
    /// The system time as a FILETIME in UTC, carried forward from the last tick of the
    /// system clock by the performance counter, so it moves between ticks.
    /// </summary>
    [DllImport(Library, EntryPoint = "GetSystemTimePreciseAsFileTime", ExactSpelling = true)]
    public static extern void GetSystemTimePreciseAsFileTime(out long lpSystemTimeAsFileTime);

    /// <summary>
    /// Whether the volume mounted at <paramref name="lpRootPathName"/> is fixed,
    /// removable, remote or something else. A TRAILING BACKSLASH IS REQUIRED,
    /// which Win32 states outright and which
    /// <see cref="GetVolumePathName"/>'s output already carries.
    ///
    /// TAKEN OVER <c>DriveInfo</c> BECAUSE A MOUNTED FOLDER HAS NO DRIVE LETTER
    /// TO GIVE IT. DriveInfo is constructed from a drive-letter root, so
    /// classifying a destination through it means classifying the letter the
    /// mount point hangs off rather than the volume the files land on, and a
    /// removable volume mounted into a folder on C: is then reported as a fixed
    /// drive. This call takes a mount point directly. Win32 pairs the two
    /// itself: GetVolumePathName's own contract for a share is defined as the
    /// shortest path for which this function answers DRIVE_REMOTE.
    ///
    /// No SetLastError: Win32 documents no extended error information for this
    /// function, and the failure is a return value
    /// (<see cref="DRIVE_UNKNOWN"/> or <see cref="DRIVE_NO_ROOT_DIR"/>) rather
    /// than a zero with a code behind it.
    /// </summary>
    [DllImport(Library, EntryPoint = "GetDriveTypeW", CharSet = CharSet.Unicode, ExactSpelling = true)]
    public static extern uint GetDriveType(string lpRootPathName);

    /// <summary>
    /// Sets the error mode of the calling thread alone to <paramref name="dwNewMode"/>, with the
    /// mode it replaces in <paramref name="lpOldMode"/>. With <see cref="SEM_FAILCRITICALERRORS"/>
    /// Windows shows no critical-error box for a read the thread makes, such as one of a drive
    /// with no disc in it, and sends the error to the thread instead.
    /// </summary>
    [DllImport(Library, EntryPoint = "SetThreadErrorMode", ExactSpelling = true, SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool SetThreadErrorMode(uint dwNewMode, out uint lpOldMode);

    /// <summary>The error mode of the calling thread (<see cref="SetThreadErrorMode"/>).</summary>
    [DllImport(Library, EntryPoint = "GetThreadErrorMode", ExactSpelling = true)]
    public static extern uint GetThreadErrorMode();

    /// <summary>
    /// Sets the error mode of the process to <paramref name="uMode"/> and answers the mode it
    /// replaces. A thread's own mode (<see cref="SetThreadErrorMode"/>) is set apart from it.
    /// </summary>
    [DllImport(Library, EntryPoint = "SetErrorMode", ExactSpelling = true)]
    public static extern uint SetErrorMode(uint uMode);

    /// <summary>The error mode of the process (<see cref="SetErrorMode"/>).</summary>
    [DllImport(Library, EntryPoint = "GetErrorMode", ExactSpelling = true)]
    public static extern uint GetErrorMode();

    /// <summary>
    /// The error mode under which the system shows no critical-error box, for a thread
    /// (<see cref="SetThreadErrorMode"/>) or for the process (<see cref="SetErrorMode"/>).
    /// </summary>
    public const uint SEM_FAILCRITICALERRORS = 0x0001;

    /// <summary>
    /// Adds <paramref name="handlerRoutine"/> to the front of the process's list of console
    /// control handlers. Windows calls it on a thread of its own for each event, and the
    /// first routine to answer nonzero ends the list.
    ///
    /// The delegate reaches Windows as a pointer to a stub that calls it, and the stub
    /// lives only as long as the delegate does. Windows keeps the pointer until the
    /// handler is removed, so the caller keeps the delegate where the garbage collector
    /// never frees it, for as long as it stays registered.
    /// </summary>
    [DllImport(Library, EntryPoint = "SetConsoleCtrlHandler", ExactSpelling = true, SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool SetConsoleCtrlHandler(
        HandlerRoutine handlerRoutine,
        [MarshalAs(UnmanagedType.Bool)] bool add);

    /// <summary>
    /// A console control handler: given the event, it answers nonzero where it took the
    /// event and zero to pass it to the routine registered before it.
    /// </summary>
    [UnmanagedFunctionPointer(CallingConvention.Winapi)]
    public delegate int HandlerRoutine(uint dwCtrlType);

    // The console control events a handler routine is given.
    public const uint CTRL_C_EVENT = 0;
    public const uint CTRL_BREAK_EVENT = 1;
    public const uint CTRL_CLOSE_EVENT = 2;
    public const uint CTRL_LOGOFF_EVENT = 5;
    public const uint CTRL_SHUTDOWN_EVENT = 6;

    /// <summary>
    /// Asks the volume at the root <paramref name="lpRootPathName"/>, a drive letter, its
    /// colon and a backslash, for its details. Declared for the answer alone: every output
    /// is passed as a null pointer, and a caller reads only whether the call succeeded and,
    /// where it did not, the last error (<see cref="ERROR_NOT_READY"/>,
    /// <see cref="ERROR_NO_MEDIA_IN_DRIVE"/>).
    /// </summary>
    [DllImport(Library, EntryPoint = "GetVolumeInformationW", CharSet = CharSet.Unicode, ExactSpelling = true, SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool GetVolumeInformation(
        string lpRootPathName,
        IntPtr lpVolumeNameBuffer,
        uint nVolumeNameSize,
        IntPtr lpVolumeSerialNumber,
        IntPtr lpMaximumComponentLength,
        IntPtr lpFileSystemFlags,
        IntPtr lpFileSystemNameBuffer,
        uint nFileSystemNameSize);

    /// <summary>
    /// Reads basic file metadata (attributes, size, timestamps) from an
    /// open handle. Used to detect whether a CreateFile-with-OPEN_REPARSE_POINT
    /// handle is pointing at a reparse point or a real file.
    /// </summary>
    [DllImport(Library, EntryPoint = "GetFileInformationByHandle", ExactSpelling = true, SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool GetFileInformationByHandle(
        SafeFileHandle hFile,
        out BY_HANDLE_FILE_INFORMATION lpFileInformation);

    [StructLayout(LayoutKind.Sequential)]
    public struct BY_HANDLE_FILE_INFORMATION
    {
        public uint dwFileAttributes;
        public FILETIME ftCreationTime;
        public FILETIME ftLastAccessTime;
        public FILETIME ftLastWriteTime;
        public uint dwVolumeSerialNumber;
        public uint nFileSizeHigh;
        public uint nFileSizeLow;
        public uint nNumberOfLinks;
        public uint nFileIndexHigh;
        public uint nFileIndexLow;
    }

    /// <summary>
    /// Reads one class of metadata from an open handle. Declared for
    /// <see cref="FILE_ID_INFO"/> only, which is why the class parameter is typed
    /// as the constant rather than as an enum: nothing else here asks for another
    /// class, and a second caller would want its own overload with its own
    /// out-parameter type anyway, the API writing a different struct per class.
    ///
    /// PREFERRED OVER <see cref="GetFileInformationByHandle"/> FOR IDENTITY, and
    /// the difference is not cosmetic. That call's nFileIndexHigh/Low pair is 64
    /// bits and Microsoft documents it as not necessarily unique on ReFS, where
    /// this call's 128-bit id is documented unique per volume on both NTFS and
    /// ReFS. A comparison that can collide would claim two files are one.
    /// </summary>
    [DllImport(Library, EntryPoint = "GetFileInformationByHandleEx", ExactSpelling = true, SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool GetFileInformationByHandleEx(
        SafeFileHandle hFile,
        uint fileInformationClass,
        out FILE_ID_INFO lpFileInformation,
        uint dwBufferSize);

    /// <summary>
    /// The volume and the 128-bit file id that together name one file on one
    /// machine. The id is declared as two ulongs rather than as a 16-byte array field,
    /// which the marshaller would convert; two ulongs are byte-for-byte the FILE_ID_128
    /// the API writes, and the halves are never interpreted, only compared.
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    public struct FILE_ID_INFO
    {
        public ulong VolumeSerialNumber;
        public ulong FileIdLow;
        public ulong FileIdHigh;
    }

    /// <summary>FileIdInfo of FILE_INFO_BY_HANDLE_CLASS.</summary>
    public const uint FileIdInfo = 18;

    /// <summary>
    /// The same call as <see cref="GetFileInformationByHandleEx"/>, declared a second
    /// time for <see cref="FILE_BASIC_INFO"/>, because the API writes a different
    /// struct for each class and an out-parameter has one type.
    ///
    /// THE HANDLE NEEDS <see cref="FILE_READ_ATTRIBUTES"/> AND NOTHING MORE. That
    /// right is outside the data-sharing check, so a handle carrying it alone
    /// neither waits on nor excludes any other opener of the file. Opened with every
    /// share flag, it also leaves the file free to be deleted. A rename that would
    /// replace the file is refused while the handle is open.
    /// </summary>
    [DllImport(Library, EntryPoint = "GetFileInformationByHandleEx", ExactSpelling = true, SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool GetFileBasicInfoByHandle(
        SafeFileHandle hFile,
        uint fileInformationClass,
        out FILE_BASIC_INFO lpFileInformation,
        uint dwBufferSize);

    /// <summary>
    /// Writes <see cref="FILE_BASIC_INFO"/> back to the file behind an open handle,
    /// which is how the read-only attribute is cleared through the handle rather than
    /// a path. The handle needs <see cref="FILE_WRITE_ATTRIBUTES"/>. A field left zero
    /// is unchanged, so only <see cref="FILE_BASIC_INFO.FileAttributes"/> is set.
    /// </summary>
    [DllImport(Library, EntryPoint = "SetFileInformationByHandle", ExactSpelling = true, SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool SetFileBasicInfoByHandle(
        SafeFileHandle hFile,
        uint fileInformationClass,
        in FILE_BASIC_INFO lpFileInformation,
        uint dwBufferSize);

    /// <summary>
    /// A file's four times and its attributes. Each time is a FILETIME count of
    /// 100-nanosecond intervals since 1601, UTC, carried as a signed 64-bit value
    /// because the API declares LARGE_INTEGER.
    ///
    /// <see cref="ChangeTime"/> IS THE ONE THE FILE SYSTEM KEEPS FOR ITSELF. NTFS
    /// sets it to the present when the file's contents or attributes change, and
    /// when the file is renamed. It is not always the file's own: CopyFile gives a
    /// copy its source's change time along with its last-write time, and the
    /// creation time is the only one of the three the copy gets fresh. So no one
    /// time dates a file by itself; <see cref="Services.CachedFileAge"/> reads the
    /// three together.
    ///
    /// Byte-for-byte the FILE_BASIC_INFO the API writes, trailing padding included:
    /// sequential layout pads the final DWORD to the struct's eight-byte alignment,
    /// which is the size the API expects to be told.
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    public struct FILE_BASIC_INFO
    {
        public long CreationTime;
        public long LastAccessTime;
        public long LastWriteTime;
        public long ChangeTime;
        public uint FileAttributes;
    }

    /// <summary>FileBasicInfo of FILE_INFO_BY_HANDLE_CLASS.</summary>
    public const uint FileBasicInfo = 0;

    /// <summary>
    /// The name of the file system on the volume that holds the file behind
    /// <paramref name="hFile"/>: <c>NTFS</c>, <c>ReFS</c>, <c>FAT32</c>,
    /// <c>exFAT</c> and so on.
    ///
    /// IT IS ASKED OF THE HANDLE AND NOT OF A PATH, which is what makes the answer
    /// the file's own. By the time a handle exists, every junction, mount point,
    /// substituted drive and symbolic link in the path that opened it has been
    /// followed, so the volume answering is the one the file is on, however the
    /// path was spelled.
    ///
    /// The other outputs are optional and are passed as null pointers; only the
    /// file-system name is asked for. The name buffer's size goes in
    /// <paramref name="nFileSystemNameSize"/>, the function taking it as a parameter.
    /// </summary>
    [DllImport(Library, EntryPoint = "GetVolumeInformationByHandleW", CharSet = CharSet.Unicode, ExactSpelling = true, SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool GetVolumeInformationByHandle(
        SafeFileHandle hFile,
        IntPtr lpVolumeNameBuffer,
        uint nVolumeNameSize,
        IntPtr lpVolumeSerialNumber,
        IntPtr lpMaximumComponentLength,
        IntPtr lpFileSystemFlags,
        [Out] char[] lpFileSystemNameBuffer,
        uint nFileSystemNameSize);

    /// <summary>
    /// The serial number of the volume that holds the file behind <paramref name="hFile"/>,
    /// asked of the handle for the reason <see cref="GetVolumeInformationByHandle"/> gives. The
    /// same entry point, with the serial number the only output asked for and every other
    /// passed as a null pointer.
    /// </summary>
    [DllImport(Library, EntryPoint = "GetVolumeInformationByHandleW", CharSet = CharSet.Unicode, ExactSpelling = true, SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool GetVolumeSerialNumberByHandle(
        SafeFileHandle hFile,
        IntPtr lpVolumeNameBuffer,
        uint nVolumeNameSize,
        out uint lpVolumeSerialNumber,
        IntPtr lpMaximumComponentLength,
        IntPtr lpFileSystemFlags,
        IntPtr lpFileSystemNameBuffer,
        uint nFileSystemNameSize);

    [StructLayout(LayoutKind.Sequential)]
    public struct FILETIME
    {
        public uint dwLowDateTime;
        public uint dwHighDateTime;
    }

    /// <summary>
    /// Creates a directory with the security descriptor the attributes carry, and
    /// fails, with ERROR_ALREADY_EXISTS, where anything already has the name. The
    /// descriptor is in place from the moment the directory exists, which is what a
    /// separate create and then set cannot give.
    /// </summary>
    [DllImport(Library, EntryPoint = "CreateDirectoryW", CharSet = CharSet.Unicode, ExactSpelling = true, SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool CreateDirectory(
        string lpPathName,
        in SECURITY_ATTRIBUTES lpSecurityAttributes);

    [StructLayout(LayoutKind.Sequential)]
    public struct SECURITY_ATTRIBUTES
    {
        public uint nLength;
        public IntPtr lpSecurityDescriptor;
        public int bInheritHandle;

        /// <summary>
        /// Attributes carrying <paramref name="securityDescriptor"/>, not inheritable by a
        /// child process. <see cref="IntPtr.Zero"/> gives the object the descriptor it
        /// inherits from its parent. A descriptor passed here must stay allocated until the
        /// call these attributes are given to has returned.
        /// </summary>
        public static SECURITY_ATTRIBUTES With(IntPtr securityDescriptor) => new()
        {
            nLength = (uint)Marshal.SizeOf<SECURITY_ATTRIBUTES>(),
            lpSecurityDescriptor = securityDescriptor,
            bInheritHandle = 0,
        };
    }

    /// <summary>
    /// Frees memory the system allocated for a caller with LocalAlloc, as
    /// ConvertStringSecurityDescriptorToSecurityDescriptor does for the descriptor it
    /// returns.
    /// </summary>
    [DllImport(Library, EntryPoint = "LocalFree", ExactSpelling = true)]
    public static extern IntPtr LocalFree(IntPtr hMem);

    /// <summary>
    /// Sets the calling thread's last error to <paramref name="dwErrCode"/>. Declared with
    /// SetLastError, so the runtime reads the code back as the call returns and
    /// <see cref="Marshal.GetLastWin32Error"/> then answers it. That is how a method hands
    /// its caller an error it read before making calls of its own.
    /// </summary>
    [DllImport(Library, EntryPoint = "SetLastError", ExactSpelling = true, SetLastError = true)]
    public static extern void SetLastError(uint dwErrCode);

    /// <summary>
    /// The text Windows gives for the error <paramref name="dwMessageId"/>, in the language
    /// Windows is shown in, written into memory Windows allocates and the caller frees with
    /// <see cref="LocalFree"/>. With <see cref="FORMAT_MESSAGE_ALLOCATE_BUFFER"/> the
    /// buffer parameter receives the pointer to that memory, so it is declared as the
    /// pointer's address. Returns the number of characters written, without the
    /// terminating null, or 0 where Windows has no text for the number.
    /// </summary>
    [DllImport(Library, EntryPoint = "FormatMessageW", CharSet = CharSet.Unicode, ExactSpelling = true, SetLastError = true)]
    public static extern uint FormatMessage(
        uint dwFlags,
        IntPtr lpSource,
        uint dwMessageId,
        uint dwLanguageId,
        out IntPtr lpBuffer,
        uint nSize,
        IntPtr Arguments);

    public const uint FORMAT_MESSAGE_ALLOCATE_BUFFER = 0x00000100;
    public const uint FORMAT_MESSAGE_IGNORE_INSERTS  = 0x00000200;
    public const uint FORMAT_MESSAGE_FROM_SYSTEM     = 0x00001000;
    public const uint FORMAT_MESSAGE_ARGUMENT_ARRAY  = 0x00002000;

    /// <summary>
    /// Removes the file or directory an open handle names, through the handle rather
    /// than a path, so nothing re-resolves the name between the open and the removal.
    /// Declared for <see cref="FILE_DISPOSITION_INFO_EX"/>, the POSIX form, whose flags
    /// let the name go at once while other handles stay open.
    ///
    /// THE HANDLE NEEDS <see cref="Ntdll.DELETE"/>. A directory goes only where it is
    /// empty, as a non-recursive removal does. A handle opened with
    /// FILE_FLAG_OPEN_REPARSE_POINT on a reparse point removes the reparse point
    /// itself, never what it points at.
    /// </summary>
    [DllImport(Library, EntryPoint = "SetFileInformationByHandle", ExactSpelling = true, SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool SetFileDispositionInfoEx(
        SafeFileHandle hFile,
        uint fileInformationClass,
        in FILE_DISPOSITION_INFO_EX lpFileInformation,
        uint dwBufferSize);

    /// <summary>
    /// The same call for <see cref="FILE_DISPOSITION_INFO"/>, the older form, for a
    /// Windows build or file system that does not take <see cref="FileDispositionInfoEx"/>.
    /// It marks the name to go when the last handle closes.
    /// </summary>
    [DllImport(Library, EntryPoint = "SetFileInformationByHandle", ExactSpelling = true, SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool SetFileDispositionInfo(
        SafeFileHandle hFile,
        uint fileInformationClass,
        in FILE_DISPOSITION_INFO lpFileInformation,
        uint dwBufferSize);

    /// <summary>The POSIX disposition: a flag word read with the flags below.</summary>
    [StructLayout(LayoutKind.Sequential)]
    public struct FILE_DISPOSITION_INFO_EX
    {
        public uint Flags;
    }

    /// <summary>The older disposition: a one-byte flag, non-zero to delete.</summary>
    [StructLayout(LayoutKind.Sequential)]
    public struct FILE_DISPOSITION_INFO
    {
        public byte DeleteFile;
    }

    /// <summary>FileDispositionInfoEx of FILE_INFO_BY_HANDLE_CLASS.</summary>
    public const uint FileDispositionInfoEx = 21;

    /// <summary>FileDispositionInfo of FILE_INFO_BY_HANDLE_CLASS.</summary>
    public const uint FileDispositionInfo = 4;

    /// <summary>Mark the name to go.</summary>
    public const uint FILE_DISPOSITION_FLAG_DELETE = 0x00000001;

    /// <summary>Let the name go at once, before the last handle closes.</summary>
    public const uint FILE_DISPOSITION_FLAG_POSIX_SEMANTICS = 0x00000002;

    /// <summary>Let a read-only file go, leaving its attributes as they are. Windows 10 version 1809 and later.</summary>
    public const uint FILE_DISPOSITION_FLAG_IGNORE_READONLY_ATTRIBUTE = 0x00000010;

    /// <summary>
    /// The pseudo-handle standing for this process, which needs no closing.
    /// </summary>
    [DllImport(Library, EntryPoint = "GetCurrentProcess", ExactSpelling = true)]
    public static extern IntPtr GetCurrentProcess();

    /// <summary>
    /// Ends the process <paramref name="hProcess"/> names at once, with
    /// <paramref name="uExitCode"/>. Given this process's own pseudo-handle it runs
    /// no more of its code: no finaliser, no exit handler and no DLL detach.
    /// </summary>
    [DllImport(Library, EntryPoint = "TerminateProcess", ExactSpelling = true, SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool TerminateProcess(IntPtr hProcess, uint uExitCode);

    /// <summary>
    /// Waits until one of <paramref name="lpHandles"/> is signalled or
    /// <paramref name="dwMilliseconds"/> have passed, and answers <see cref="WAIT_OBJECT_0"/>
    /// plus the index of the lowest-numbered handle signalled, <see cref="WAIT_ABANDONED_0"/>
    /// plus that index for a mutex abandoned by its owner, <see cref="WAIT_TIMEOUT"/> or
    /// <see cref="WAIT_FAILED"/>. It never dispatches a window message or a COM call,
    /// so no window code runs on the waiting thread meanwhile.
    /// </summary>
    [DllImport(Library, EntryPoint = "WaitForMultipleObjects", ExactSpelling = true, SetLastError = true)]
    public static extern uint WaitForMultipleObjects(
        uint nCount, IntPtr[] lpHandles, [MarshalAs(UnmanagedType.Bool)] bool bWaitAll, uint dwMilliseconds);

    public const uint WAIT_OBJECT_0 = 0;

    /// <summary>A mutex waited on whose last owner ended without releasing it: the waiting thread now owns it.</summary>
    public const uint WAIT_ABANDONED_0 = 0x80;

    public const uint WAIT_TIMEOUT = 0x102;
    public const uint WAIT_FAILED = 0xFFFFFFFF;

    /// <summary>A wait's timeout that never runs out.</summary>
    public const uint INFINITE = 0xFFFFFFFF;

    /// <summary>
    /// Renames <paramref name="lpExistingFileName"/> to <paramref name="lpNewFileName"/> as
    /// <paramref name="dwFlags"/> says. With <see cref="MOVEFILE_REPLACE_EXISTING"/> a file at
    /// the new name is replaced in the same step, so a reader of that name finds the old file
    /// or the new one and never neither; a link at the new name is replaced, not followed.
    /// </summary>
    [DllImport(Library, EntryPoint = "MoveFileExW", CharSet = CharSet.Unicode, ExactSpelling = true, SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool MoveFileEx(string lpExistingFileName, string lpNewFileName, uint dwFlags);

    public const uint MOVEFILE_REPLACE_EXISTING = 0x1;

    /// <summary>The move returns only once it is written to the disk.</summary>
    public const uint MOVEFILE_WRITE_THROUGH = 0x8;

    /// <summary>
    /// Gives the file <paramref name="lpExistingFileName"/> a second name,
    /// <paramref name="lpFileName"/>, in a folder on the same volume. Fails where the volume
    /// has no hard links, as FAT and exFAT have none, and where a file has that name.
    /// </summary>
    [DllImport(Library, EntryPoint = "CreateHardLinkW", CharSet = CharSet.Unicode, ExactSpelling = true, SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool CreateHardLink(string lpFileName, string lpExistingFileName, IntPtr lpSecurityAttributes);

    /// <summary>
    /// Suspends the calling thread for <paramref name="dwMilliseconds"/>. Like
    /// <see cref="WaitForMultipleObjects"/>, it dispatches nothing meanwhile.
    /// </summary>
    [DllImport(Library, EntryPoint = "Sleep", ExactSpelling = true)]
    public static extern void Sleep(uint dwMilliseconds);

    /// <summary>
    /// Opens the process <paramref name="dwProcessId"/> names with
    /// <paramref name="dwDesiredAccess"/>. Answers an invalid handle where it cannot, with
    /// <see cref="ERROR_INVALID_PARAMETER"/> as the last error where no process has that id.
    /// </summary>
    [DllImport(Library, EntryPoint = "OpenProcess", ExactSpelling = true, SetLastError = true)]
    public static extern SafeProcessHandle OpenProcess(
        uint dwDesiredAccess,
        [MarshalAs(UnmanagedType.Bool)] bool bInheritHandle,
        uint dwProcessId);

    /// <summary>
    /// The full path of the program a process was started from, in the Win32 form with
    /// <paramref name="dwFlags"/> 0. <paramref name="lpdwSize"/> carries the buffer's size in
    /// characters in, and the length written, without the terminating null, out. A buffer
    /// too small fails with ERROR_INSUFFICIENT_BUFFER.
    /// </summary>
    [DllImport(Library, EntryPoint = "QueryFullProcessImageNameW", CharSet = CharSet.Unicode, ExactSpelling = true, SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool QueryFullProcessImageName(
        SafeProcessHandle hProcess,
        uint dwFlags,
        [Out] char[] lpExeName,
        ref uint lpdwSize);

    /// <summary>
    /// Starts the program <paramref name="lpApplicationName"/> names, with
    /// <paramref name="lpCommandLine"/> as its whole command line, its first argument being
    /// the program's own name. Windows may write into <paramref name="lpCommandLine"/>, so it
    /// is a buffer ending in a null. Answers the new process's and its first thread's handles
    /// in <paramref name="lpProcessInformation"/>, both of which the caller closes.
    /// <paramref name="lpVariables"/> is the block of variables the program starts with, and
    /// zero gives it the caller's own.
    /// </summary>
    [DllImport(Library, EntryPoint = "CreateProcessW", CharSet = CharSet.Unicode, ExactSpelling = true, SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool CreateProcess(
        string lpApplicationName,
        [In, Out] char[] lpCommandLine,
        IntPtr lpProcessAttributes,
        IntPtr lpThreadAttributes,
        [MarshalAs(UnmanagedType.Bool)] bool bInheritHandles,
        uint dwCreationFlags,
        IntPtr lpVariables,
        string lpCurrentDirectory,
        in STARTUPINFO lpStartupInfo,
        out PROCESS_INFORMATION lpProcessInformation);

    /// <summary>
    /// What <see cref="CreateProcess"/> starts the process's window and standard handles
    /// with. All zero but <see cref="cb"/> asks for nothing: no window settings and no
    /// standard handles of the caller's choosing.
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    public struct STARTUPINFO
    {
        public uint cb;
        public IntPtr lpReserved;
        public IntPtr lpDesktop;
        public IntPtr lpTitle;
        public uint dwX;
        public uint dwY;
        public uint dwXSize;
        public uint dwYSize;
        public uint dwXCountChars;
        public uint dwYCountChars;
        public uint dwFillAttribute;
        public uint dwFlags;
        public ushort wShowWindow;
        public ushort cbReserved2;
        public IntPtr lpReserved2;
        public IntPtr hStdInput;
        public IntPtr hStdOutput;
        public IntPtr hStdError;
    }

    /// <summary>The process and the thread <see cref="CreateProcess"/> started, and their IDs.</summary>
    [StructLayout(LayoutKind.Sequential)]
    public struct PROCESS_INFORMATION
    {
        public IntPtr hProcess;
        public IntPtr hThread;
        public uint dwProcessId;
        public uint dwThreadId;
    }

    /// <summary>The new process has no console, and the console's Ctrl+C and close never reach it.</summary>
    public const uint DETACHED_PROCESS = 0x00000008;

    /// <summary>
    /// The new process is started outside the job the caller is in, so ending that job does
    /// not end it. Windows refuses the start with access denied where the job does not allow it.
    /// </summary>
    public const uint CREATE_BREAKAWAY_FROM_JOB = 0x01000000;

    /// <summary>
    /// Reads the exit code of the process <paramref name="hProcess"/> stands for into
    /// <paramref name="lpExitCode"/>, which is <see cref="STILL_ACTIVE"/> while it runs.
    /// </summary>
    [DllImport(Library, EntryPoint = "GetExitCodeProcess", ExactSpelling = true, SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool GetExitCodeProcess(SafeProcessHandle hProcess, out uint lpExitCode);

    /// <summary>What <see cref="GetExitCodeProcess"/> reads for a process still running.</summary>
    public const uint STILL_ACTIVE = 259;

    /// <summary>
    /// Sets the folders Windows searches, for the whole process, for a library asked for by
    /// its name alone and for the libraries a loaded library depends on, to those
    /// <paramref name="directoryFlags"/> names (LOAD_LIBRARY_SEARCH_*). A library loaded by
    /// its full path with LOAD_WITH_ALTERED_SEARCH_PATH has its own imports looked for first
    /// in the folder it was loaded from.
    /// </summary>
    [DllImport(Library, EntryPoint = "SetDefaultDllDirectories", ExactSpelling = true, SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool SetDefaultDllDirectories(uint directoryFlags);

    /// <summary>
    /// The process's standard input, output or error handle, as <paramref name="nStdHandle"/>
    /// names it (<see cref="STD_INPUT_HANDLE"/>). Not to be closed: the process owns it.
    /// </summary>
    [DllImport(Library, EntryPoint = "GetStdHandle", ExactSpelling = true, SetLastError = true)]
    public static extern IntPtr GetStdHandle(int nStdHandle);

    /// <summary>What <see cref="GetStdHandle"/> takes for standard input.</summary>
    public const int STD_INPUT_HANDLE = -10;

    /// <summary>
    /// Discards every input record waiting in a console's input buffer: key presses typed
    /// before a read starts. Fails on a handle that is not a console's input.
    /// </summary>
    [DllImport(Library, EntryPoint = "FlushConsoleInputBuffer", ExactSpelling = true, SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool FlushConsoleInputBuffer(IntPtr hConsoleInput);

    /// <summary>
    /// The least access <see cref="QueryFullProcessImageName"/> needs, asked for in place
    /// of fuller access a process of another account can refuse.
    /// </summary>
    public const uint PROCESS_QUERY_LIMITED_INFORMATION = 0x1000;

    public const uint GENERIC_READ           = 0x80000000;
    public const uint GENERIC_WRITE          = 0x40000000;

    // Granted on its own, and specifically WITHOUT FILE_WRITE_DATA (which
    // GENERIC_WRITE carries), FILE_APPEND_DATA is what makes every write on the
    // handle land at the end of the file as one atomic step, whatever offset the
    // caller asks for. CreateFile documents it as "the right to append data to
    // the file... for local files, write operations will not overwrite existing
    // data if this flag is specified without FILE_WRITE_DATA". FILE_READ_ATTRIBUTES
    // is what lets the length still be queried on such a handle.
    public const uint FILE_APPEND_DATA       = 0x00000004;
    public const uint FILE_READ_ATTRIBUTES   = 0x00000080;
    public const uint FILE_WRITE_ATTRIBUTES  = 0x00000100;

    public const uint CREATE_NEW             = 1;
    public const uint CREATE_ALWAYS          = 2;
    public const uint OPEN_EXISTING          = 3;
    public const uint OPEN_ALWAYS            = 4;

    public const uint FILE_SHARE_READ        = 0x00000001;
    public const uint FILE_SHARE_WRITE       = 0x00000002;
    public const uint FILE_SHARE_ALL         = 0x00000007;

    public const uint FILE_FLAG_BACKUP_SEMANTICS    = 0x02000000;
    public const uint FILE_FLAG_OPEN_REPARSE_POINT  = 0x00200000;
    public const uint FILE_FLAG_DELETE_ON_CLOSE     = 0x04000000;

    public const uint FILE_ATTRIBUTE_REPARSE_POINT  = 0x00000400;
    public const uint FILE_ATTRIBUTE_DIRECTORY      = 0x00000010;

    // GetDriveType's return values. Numbered here rather than cast to
    // System.IO.DriveType at the call site: the two happen to share their
    // numbering today, and a mapping that rests on that is a mapping nobody can
    // see break. StorageHelpers.GetDriveKind writes the correspondence out.
    public const uint DRIVE_UNKNOWN     = 0;
    public const uint DRIVE_NO_ROOT_DIR = 1;
    public const uint DRIVE_REMOVABLE   = 2;
    public const uint DRIVE_FIXED       = 3;
    public const uint DRIVE_REMOTE      = 4;
    public const uint DRIVE_CDROM       = 5;
    public const uint DRIVE_RAMDISK     = 6;

    /// <summary>
    /// The user's preferred display languages, most preferred first. With
    /// <see cref="MUI_LANGUAGE_NAME"/> each is a culture name such as
    /// <c>en-GB</c>, ended by a null, and the list by a second null. The first
    /// entry is the language Windows shows its own interface in for this user,
    /// and it is what .NET reads for the UI culture of a thread nobody has given
    /// one. A setting the app makes on its own culture does not reach it.
    ///
    /// The count is in characters, the terminators included. A call with a null
    /// buffer and a count of zero answers the size needed.
    /// </summary>
    [DllImport(Library, EntryPoint = "GetUserPreferredUILanguages", CharSet = CharSet.Unicode, ExactSpelling = true, SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool GetUserPreferredUILanguages(
        uint dwFlags,
        out uint pulNumLanguages,
        [Out] char[]? pwszLanguagesBuffer,
        ref uint pcchLanguagesBuffer);

    // GetUserPreferredUILanguages flag: culture names rather than language ids.
    public const uint MUI_LANGUAGE_NAME = 0x8;

    /// <summary>
    /// The Country or region a user has chosen in the Windows settings, for the account
    /// running the process: an ISO 3166-1 two-letter code, or a UN M.49 number where the
    /// location has no such code. Writes the name and its null terminator into the buffer
    /// and returns the count of both, or 0 on failure. Windows 10 1709 and later; on an
    /// older Windows the entry point is missing and the call throws
    /// <see cref="EntryPointNotFoundException"/>.
    /// </summary>
    [DllImport(Library, EntryPoint = "GetUserDefaultGeoName", CharSet = CharSet.Unicode, ExactSpelling = true, SetLastError = true)]
    public static extern int GetUserDefaultGeoName(
        [Out] char[] geoName,
        int geoNameCount);

    // GetFinalPathNameByHandle flags. VOLUME_NAME_DOS names the volume by
    // its drive letter, giving "\\?\X:\Folder\...", where VOLUME_NAME_GUID
    // would give "\\?\Volume{guid}\...". Both forms keep the \\?\ prefix,
    // which callers strip (InstallerCacheHelpers.StripLongPathPrefix) to
    // get back a path comparable to a user-typed one.
    public const uint VOLUME_NAME_DOS = 0x0;

    // VOLUME_NAME_GUID names the volume by its GUID path, "\\?\Volume{guid}\...",
    // instead. The mount manager gives a GUID path to local volumes, and a file
    // reached over the network has none to give; FileTimesReader treats any
    // answer that is not a GUID path on a fixed drive as a volume it cannot vouch
    // for, whichever way the call reports that.
    public const uint VOLUME_NAME_GUID = 0x1;

    // The two codes CreateFile reports for an absence, and the only two a caller
    // may read as "nothing is at this path": the leaf is missing, or a component
    // above it is. Every other failure means the open was refused rather than that
    // there was nothing to open, which is the distinction FileIdentityReader turns
    // into two quite different answers.
    public const int ERROR_FILE_NOT_FOUND = 2;
    public const int ERROR_PATH_NOT_FOUND = 3;

    // The code a drive answers with when it is not ready, as an optical drive with no disc
    // in it and a card reader with no card do.
    public const int ERROR_NOT_READY = 21;

    // "No media in drive", in Microsoft's list of system error codes.
    public const int ERROR_NO_MEDIA_IN_DRIVE = 1112;

    // What a rename or delete answers where another process has the file open without
    // sharing delete.
    public const int ERROR_SHARING_VIOLATION = 32;

    // What a read answers where another process has locked the part of the file it reads.
    public const int ERROR_LOCK_VIOLATION = 33;

    // The two the volume enumeration and the mount-point query answer with, and
    // both are ordinary rather than faults: ERROR_NO_MORE_FILES ends a volume
    // walk, and ERROR_MORE_DATA is the first half of the double-call buffer
    // pattern. A caller reading either as a failure would report a machine
    // problem where Win32 was following its own contract.
    public const int ERROR_NO_MORE_FILES = 18;
    public const int ERROR_MORE_DATA = 234;

    // What CreateDirectory answers where something already has the name.
    public const int ERROR_ALREADY_EXISTS = 183;

    // What OpenProcess answers where no process has the id, and one of the answers to a
    // disposition form that the system or the file system does not take.
    public const int ERROR_INVALID_PARAMETER = 87;

    // The other two answers to a disposition form that is not taken.
    public const int ERROR_INVALID_FUNCTION = 1;
    public const int ERROR_NOT_SUPPORTED = 50;

    // "The directory name is invalid": the code for a path that names a file where a folder is wanted.
    public const int ERROR_DIRECTORY = 267;

    // "The object manager encountered a reparse point": the code for a child that is a link where a plain file is wanted.
    public const int ERROR_REPARSE_POINT_ENCOUNTERED = 4395;

    // "An operation is not supported on a directory": the code for a folder where a plain file is wanted.
    public const int ERROR_DIRECTORY_NOT_SUPPORTED = 336;

    // "Access is denied": the error .NET raises an UnauthorizedAccessException for.
    public const int ERROR_ACCESS_DENIED = 5;

    // "Insufficient information exists to identify the cause of failure": the code for a
    // failure that arrived with no Windows error number.
    public const int ERROR_UNIDENTIFIED_ERROR = 1287;
}
