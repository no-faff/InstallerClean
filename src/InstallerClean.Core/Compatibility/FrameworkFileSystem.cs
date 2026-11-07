using System.Runtime.InteropServices;
using InstallerClean.Helpers;
using InstallerClean.Interop.Native;

namespace System.IO;

/// <summary>
/// Whether a file is at a full path, read as .NET 10 reads it before <c>File.Move</c> on
/// Windows (dotnet/runtime v10.0.0, <c>FileSystem.Attributes.Windows.cs</c>,
/// <c>Interop.GetFileAttributesEx.cs</c>, <c>Interop.FindFirstFileEx.cs</c> and
/// <c>DisableMediaInsertionPrompt.cs</c>).
/// </summary>
internal static class FrameworkFileSystem
{
    private const int ERROR_SUCCESS = 0;
    private const int ERROR_FILE_NOT_FOUND = 2;
    private const int ERROR_PATH_NOT_FOUND = 3;
    private const int ERROR_INVALID_HANDLE = 6;
    private const int ERROR_NOT_READY = 21;
    private const int ERROR_BAD_NETPATH = 53;
    private const int ERROR_NETWORK_ACCESS_DENIED = 65;
    private const int ERROR_BAD_NET_NAME = 67;
    private const int ERROR_INVALID_PARAMETER = 87;
    private const int ERROR_INVALID_NAME = 123;
    private const int ERROR_BAD_PATHNAME = 161;
    private const int ERROR_FILENAME_EXCED_RANGE = 206;
    private const int ERROR_NETWORK_UNREACHABLE = 1231;

    private const uint InvalidFileAttributes = 0xFFFFFFFF;

    /// <summary>
    /// Whether <paramref name="fullPath"/> names a file: something answers there, and it is
    /// not a folder. A link to a file counts as a file, read by its own attributes.
    /// </summary>
    internal static bool FileExists(string fullPath) =>
        FillAttributeInfo(fullPath, out var data) == ERROR_SUCCESS
        && data.dwFileAttributes != InvalidFileAttributes
        && (data.dwFileAttributes & Kernel32.FILE_ATTRIBUTE_DIRECTORY) == 0;

    /// <summary>
    /// The attributes, times and size of what <paramref name="path"/> names, with 0 as the
    /// answer, or the Windows error number that stopped the read.
    /// </summary>
    /// <remarks>
    /// One separator at the end is taken off first, as neither call reads a path ending in
    /// one. Windows shows no box asking for a disc or a card while the thread reads: the
    /// thread's error mode is SEM_FAILCRITICALERRORS alone for the read, as .NET 10 sets it,
    /// and is put back after. StorageHelpers.WithNoCriticalErrorBox keeps the thread's other
    /// flags, so it would set a different mode from .NET 10's here.
    ///
    /// A file waiting to be deleted refuses GetFileAttributesEx with access denied, and a
    /// system file such as pagefile.sys refuses it with a sharing violation, while a search
    /// of its folder still lists it with its attributes. So every error but those meaning
    /// nothing can be reached at the path is asked again through FindFirstFileEx, and its
    /// answer, attributes or error, is the one returned.
    /// </remarks>
    internal static int FillAttributeInfo(string path, out Kernel32.WIN32_FILE_ATTRIBUTE_DATA data)
    {
        var errorCode = ERROR_SUCCESS;

        path = PathText.TrimEndingDirectorySeparator(path);

        var promptOff = Kernel32.SetThreadErrorMode(Kernel32.SEM_FAILCRITICALERRORS, out var oldMode);
        try
        {
            if (!Kernel32.GetFileAttributesEx(
                    PathText.EnsureExtendedPrefixIfNeeded(path), Kernel32.GetFileExInfoStandard, out data))
            {
                errorCode = Marshal.GetLastWin32Error();

                if (!IsPathUnreachableError(errorCode))
                {
                    var handle = Kernel32.FindFirstFileEx(
                        PathText.EnsureExtendedPrefixIfNeeded(path),
                        Kernel32.FindExInfoBasic,
                        out var findData,
                        Kernel32.FindExSearchNameMatch,
                        IntPtr.Zero,
                        0);

                    if (handle == InvalidHandle)
                    {
                        errorCode = Marshal.GetLastWin32Error();
                    }
                    else
                    {
                        Kernel32.FindClose(handle);
                        errorCode = ERROR_SUCCESS;
                        data = new Kernel32.WIN32_FILE_ATTRIBUTE_DATA
                        {
                            dwFileAttributes = findData.dwFileAttributes,
                            ftCreationTime = findData.ftCreationTime,
                            ftLastAccessTime = findData.ftLastAccessTime,
                            ftLastWriteTime = findData.ftLastWriteTime,
                            nFileSizeHigh = findData.nFileSizeHigh,
                            nFileSizeLow = findData.nFileSizeLow,
                        };
                    }
                }
            }
        }
        finally
        {
            if (promptOff) Kernel32.SetThreadErrorMode(oldMode, out _);
        }

        return errorCode;
    }

    /// <summary>
    /// Whether <paramref name="errorCode"/> means nothing can be reached at the path: it is
    /// missing, malformed, too long, or on a drive or share that does not answer.
    /// </summary>
    internal static bool IsPathUnreachableError(int errorCode) =>
        errorCode is ERROR_FILE_NOT_FOUND
            or ERROR_PATH_NOT_FOUND
            or ERROR_NOT_READY
            or ERROR_INVALID_NAME
            or ERROR_BAD_PATHNAME
            or ERROR_BAD_NETPATH
            or ERROR_BAD_NET_NAME
            or ERROR_INVALID_PARAMETER
            or ERROR_NETWORK_UNREACHABLE
            or ERROR_NETWORK_ACCESS_DENIED
            or ERROR_INVALID_HANDLE
            or ERROR_FILENAME_EXCED_RANGE;

    private static readonly IntPtr InvalidHandle = new(-1);
}
