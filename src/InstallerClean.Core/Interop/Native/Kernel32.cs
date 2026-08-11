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

    // GetFinalPathNameByHandle flags. VOLUME_NAME_DOS names the volume by
    // its drive letter, giving "\\?\X:\Folder\...", where VOLUME_NAME_GUID
    // would give "\\?\Volume{guid}\...". Both forms keep the \\?\ prefix,
    // which callers strip (InstallerCacheHelpers.StripLongPathPrefix) to
    // get back a path comparable to a user-typed one.
    public const uint VOLUME_NAME_DOS = 0x0;
}
