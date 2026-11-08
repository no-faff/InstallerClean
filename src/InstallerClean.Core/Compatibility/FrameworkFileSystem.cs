using System.Runtime.InteropServices;
using InstallerClean.Helpers;
using InstallerClean.Interop.Native;

namespace System.IO;

/// <summary>
/// Whether a file or a folder is at a full path, and its attributes, read as .NET 10 reads
/// them on Windows (dotnet/runtime v10.0.0, <c>FileSystem.Attributes.Windows.cs</c>,
/// <c>FileSystem.Windows.cs</c>,
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
    /// Whether <paramref name="fullPath"/> names a folder: something answers there, and it is
    /// a folder. A link to a folder counts as a folder, read by its own attributes.
    /// </summary>
    internal static bool DirectoryExists(string fullPath) =>
        FillAttributeInfo(fullPath, out var data) == ERROR_SUCCESS
        && data.dwFileAttributes != InvalidFileAttributes
        && (data.dwFileAttributes & Kernel32.FILE_ATTRIBUTE_DIRECTORY) != 0;

    /// <summary>
    /// The attributes of what <paramref name="fullPath"/> names. A refusal from Windows,
    /// nothing being there included, is the exception <see cref="Win32Marshal"/> gives for its
    /// error, naming <paramref name="fullPath"/>.
    /// </summary>
    internal static FileAttributes GetAttributes(string fullPath)
    {
        var errorCode = FillAttributeInfo(fullPath, out var data);
        if (errorCode != ERROR_SUCCESS)
            throw Win32Marshal.GetExceptionForWin32Error(errorCode, fullPath);
        return (FileAttributes)data.dwFileAttributes;
    }

    /// <summary>
    /// The size of the file at <paramref name="fullPath"/>, read once as .NET 10's FileInfo
    /// reads it. Nothing there, or a folder, is a <see cref="FileNotFoundException"/> naming
    /// <paramref name="fullPath"/>; any other refusal is the exception
    /// <see cref="Win32Marshal"/> gives for its error.
    /// </summary>
    internal static long GetLength(string fullPath)
    {
        var errorCode = FillAttributeInfo(fullPath, out var data, returnErrorOnNotFound: false);
        if (errorCode != ERROR_SUCCESS)
            throw Win32Marshal.GetExceptionForWin32Error(errorCode, fullPath);
        return LengthOf(fullPath, data);
    }

    /// <summary>
    /// The size <paramref name="data"/> holds for the file at <paramref name="fullPath"/>, read
    /// with <c>returnErrorOnNotFound</c> false. A folder, or nothing there, which reads as
    /// every attribute bit set, the folder bit among them, is a
    /// <see cref="FileNotFoundException"/> naming <paramref name="fullPath"/>.
    /// </summary>
    internal static long LengthOf(string fullPath, in Kernel32.WIN32_FILE_ATTRIBUTE_DATA data)
    {
        if ((data.dwFileAttributes & Kernel32.FILE_ATTRIBUTE_DIRECTORY) != 0)
            throw Win32Marshal.GetExceptionForWin32Error(ERROR_FILE_NOT_FOUND, fullPath);

        return ((long)data.nFileSizeHigh << 32) | (data.nFileSizeLow & 0xFFFFFFFFL);
    }

    /// <summary>
    /// Deletes the file at <paramref name="fullPath"/>. Nothing there is not an error; any
    /// other refusal from Windows is the exception <see cref="Win32Marshal"/> gives for its
    /// error, naming <paramref name="fullPath"/>.
    /// </summary>
    internal static void DeleteFile(string fullPath)
    {
        if (Kernel32.DeleteFile(PathText.EnsureExtendedPrefixIfNeeded(fullPath)))
            return;

        var errorCode = Marshal.GetLastWin32Error();
        if (errorCode == ERROR_FILE_NOT_FOUND)
            return;
        throw Win32Marshal.GetExceptionForWin32Error(errorCode, fullPath);
    }

    /// <summary>
    /// Sets the attributes of what <paramref name="fullPath"/> names. Attributes Windows will
    /// not set are an <see cref="ArgumentException"/>; any other refusal is the exception
    /// <see cref="Win32Marshal"/> gives for its error, naming <paramref name="fullPath"/>.
    /// </summary>
    internal static void SetAttributes(string fullPath, FileAttributes attributes)
    {
        if (Kernel32.SetFileAttributes(PathText.EnsureExtendedPrefixIfNeeded(fullPath), (uint)attributes))
            return;

        var errorCode = Marshal.GetLastWin32Error();
        if (errorCode == ERROR_INVALID_PARAMETER)
            throw new ArgumentException("Invalid File or Directory attributes value.", nameof(attributes));
        throw Win32Marshal.GetExceptionForWin32Error(errorCode, fullPath);
    }

    /// <summary>
    /// The attributes, times and size of what <paramref name="path"/> names, with 0 as the
    /// answer, or the Windows error number that stopped the read.
    /// </summary>
    internal static int FillAttributeInfo(string path, out Kernel32.WIN32_FILE_ATTRIBUTE_DATA data) =>
        FillAttributeInfo(path, out data, returnErrorOnNotFound: true);

    /// <summary>
    /// As <see cref="FillAttributeInfo(string, out Kernel32.WIN32_FILE_ATTRIBUTE_DATA)"/>,
    /// except that where <paramref name="returnErrorOnNotFound"/> is false, nothing being at
    /// the path, or a drive that is not ready, answers 0 with every attribute bit set, which is
    /// how .NET 10's FileInfo and DirectoryInfo hold a path with nothing there.
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
    internal static int FillAttributeInfo(
        string path, out Kernel32.WIN32_FILE_ATTRIBUTE_DATA data, bool returnErrorOnNotFound)
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

        if (errorCode != ERROR_SUCCESS && !returnErrorOnNotFound
            && errorCode is ERROR_FILE_NOT_FOUND or ERROR_PATH_NOT_FOUND or ERROR_NOT_READY)
        {
            data.dwFileAttributes = InvalidFileAttributes;
            return ERROR_SUCCESS;
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
