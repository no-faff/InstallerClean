using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;
using Microsoft.Win32.SafeHandles;

namespace InstallerClean.Interop.Native;

/// <summary>
/// P/Invoke surface for kernel32.dll. Uses the source-generated
/// <see cref="LibraryImportAttribute"/> stubs rather than DllImport so
/// the marshalling code is emitted at compile time, free of runtime
/// reflection and friendly to AOT.
/// </summary>
internal static partial class Kernel32
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
    [LibraryImport(Library, EntryPoint = "CreateFileW", SetLastError = true,
                   StringMarshalling = StringMarshalling.Utf16)]
    public static partial SafeFileHandle CreateFile(
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
    [LibraryImport(Library, EntryPoint = "GetFinalPathNameByHandleW", SetLastError = true)]
    public static partial uint GetFinalPathNameByHandle(
        SafeFileHandle hFile,
        [MarshalUsing(CountElementName = nameof(cchFilePath))] char[] lpszFilePath,
        uint cchFilePath,
        uint dwFlags);

    /// <summary>
    /// Retrieves disk free-space figures for the volume hosting
    /// <paramref name="lpDirectoryName"/>. Handles local drives, UNC
    /// shares and mapped drives uniformly.
    /// </summary>
    [LibraryImport(Library, EntryPoint = "GetDiskFreeSpaceExW", SetLastError = true,
                   StringMarshalling = StringMarshalling.Utf16)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static partial bool GetDiskFreeSpaceEx(
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
    /// <see cref="StorageHelpers.IsRemotePath"/> is what keeps the shapes that
    /// can do it away from here.
    ///
    /// <c>CountElementName</c> and not <c>ConstantElementCount</c>, which is the
    /// opposite of the rule the <c>MsiEnum*</c> buffers follow and is right for
    /// the same reason: those take no count parameter, so naming one injects a
    /// phantom argument. This function takes <paramref name="cchBufferLength"/>
    /// and must be told the size, exactly as GetFinalPathNameByHandle above is.
    /// </summary>
    [LibraryImport(Library, EntryPoint = "GetVolumePathNameW", SetLastError = true,
                   StringMarshalling = StringMarshalling.Utf16)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static partial bool GetVolumePathName(
        string lpszFileName,
        [MarshalUsing(CountElementName = nameof(cchBufferLength))] char[] lpszVolumePathName,
        uint cchBufferLength);

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
    [LibraryImport(Library, EntryPoint = "GetDriveTypeW",
                   StringMarshalling = StringMarshalling.Utf16)]
    public static partial uint GetDriveType(string lpRootPathName);

    /// <summary>
    /// Reads basic file metadata (attributes, size, timestamps) from an
    /// open handle. Used to detect whether a CreateFile-with-OPEN_REPARSE_POINT
    /// handle is pointing at a reparse point or a real file.
    /// </summary>
    [LibraryImport(Library, EntryPoint = "GetFileInformationByHandle", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static partial bool GetFileInformationByHandle(
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
    [LibraryImport(Library, EntryPoint = "GetFileInformationByHandleEx", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static partial bool GetFileInformationByHandleEx(
        SafeFileHandle hFile,
        uint fileInformationClass,
        out FILE_ID_INFO lpFileInformation,
        uint dwBufferSize);

    /// <summary>
    /// The volume and the 128-bit file id that together name one file on one
    /// machine. The id is declared as two ulongs rather than as a 16-byte array
    /// because the assembly disables runtime marshalling, so the struct has to be
    /// blittable; the layout is byte-for-byte the FILE_ID_128 the API writes, and
    /// the halves are never interpreted, only compared.
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

    [StructLayout(LayoutKind.Sequential)]
    public struct FILETIME
    {
        public uint dwLowDateTime;
        public uint dwHighDateTime;
    }

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

    public const uint CREATE_ALWAYS          = 2;
    public const uint OPEN_EXISTING          = 3;
    public const uint OPEN_ALWAYS            = 4;

    public const uint FILE_SHARE_ALL         = 0x00000007;

    public const uint FILE_FLAG_BACKUP_SEMANTICS    = 0x02000000;
    public const uint FILE_FLAG_OPEN_REPARSE_POINT  = 0x00200000;

    public const uint FILE_ATTRIBUTE_REPARSE_POINT  = 0x00000400;

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

    // GetFinalPathNameByHandle flags. VOLUME_NAME_DOS names the volume by
    // its drive letter, giving "\\?\X:\Folder\...", where VOLUME_NAME_GUID
    // would give "\\?\Volume{guid}\...". Both forms keep the \\?\ prefix,
    // which callers strip (InstallerCacheHelpers.StripLongPathPrefix) to
    // get back a path comparable to a user-typed one.
    public const uint VOLUME_NAME_DOS = 0x0;

    // The two codes CreateFile reports for an absence, and the only two a caller
    // may read as "nothing is at this path": the leaf is missing, or a component
    // above it is. Every other failure means the open was refused rather than that
    // there was nothing to open, which is the distinction FileIdentityReader turns
    // into two quite different answers.
    public const int ERROR_FILE_NOT_FOUND = 2;
    public const int ERROR_PATH_NOT_FOUND = 3;
}
