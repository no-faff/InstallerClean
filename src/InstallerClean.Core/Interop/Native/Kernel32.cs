using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;
using Microsoft.Win32.SafeHandles;

namespace InstallerClean.Interop.Native;

/// <summary>
/// P/Invoke surface for kernel32.dll. Uses the .NET 8 source-generated
/// <see cref="LibraryImportAttribute"/> stubs (faster, AOT-friendly,
/// no marshalling reflection at runtime).
/// </summary>
internal static partial class Kernel32
{
    private const string Library = "kernel32.dll";

    /// <summary>
    /// Opens a file or directory and returns a handle suitable for
    /// passing to <see cref="GetFinalPathNameByHandle"/>.
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
    /// junctions and symlinks. The output buffer must be sized in
    /// characters; the return value is the character count required
    /// (excluding the null terminator).
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

    [StructLayout(LayoutKind.Sequential)]
    public struct FILETIME
    {
        public uint dwLowDateTime;
        public uint dwHighDateTime;
    }

    public const uint GENERIC_READ           = 0x80000000;
    public const uint GENERIC_WRITE          = 0x40000000;

    public const uint CREATE_ALWAYS          = 2;
    public const uint OPEN_EXISTING          = 3;
    public const uint OPEN_ALWAYS            = 4;

    public const uint FILE_SHARE_ALL         = 0x00000007;

    public const uint FILE_FLAG_BACKUP_SEMANTICS    = 0x02000000;
    public const uint FILE_FLAG_OPEN_REPARSE_POINT  = 0x00200000;

    public const uint FILE_ATTRIBUTE_REPARSE_POINT  = 0x00000400;

    // GetFinalPathNameByHandle flags. VolumeNameDos returns a path of
    // the form "X:\Folder\..." rather than the raw NT-namespace
    // "\\?\Volume{guid}\..." form.
    public const uint VOLUME_NAME_DOS = 0x0;

    [LibraryImport(Library, EntryPoint = "OpenProcess", SetLastError = true)]
    public static partial SafeProcessHandle OpenProcess(
        uint desiredAccess,
        [MarshalAs(UnmanagedType.Bool)] bool inheritHandle,
        uint processId);

    public const uint PROCESS_QUERY_INFORMATION = 0x0400;
}
