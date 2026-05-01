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
    /// Attaches the calling process's stdout/stderr to the parent's
    /// console, if any. Used by the WPF host so CLI mode prints
    /// synchronously into the terminal that launched it.
    /// </summary>
    [LibraryImport(Library)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static partial bool AttachConsole(int dwProcessId);

    public const int ATTACH_PARENT_PROCESS = -1;

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

    // CreateFile flags used by the reparse-point resolver. Centralised
    // here because they belong to the kernel32 surface, not the caller.
    public const uint OPEN_EXISTING = 3;
    public const uint FILE_SHARE_ALL = 0x00000007;
    public const uint FILE_FLAG_BACKUP_SEMANTICS = 0x02000000;

    // GetFinalPathNameByHandle flags. VolumeNameDos returns a path of
    // the form "X:\Folder\..." rather than the raw NT-namespace
    // "\\?\Volume{guid}\..." form.
    public const uint VOLUME_NAME_DOS = 0x0;
}
