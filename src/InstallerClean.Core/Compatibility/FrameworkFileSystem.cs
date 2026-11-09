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
    private const int ERROR_ACCESS_DENIED = 5;
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
    internal static bool FileExists(string fullPath) => FileExists(fullPath, out _);

    /// <summary>
    /// As <see cref="FileExists(string)"/>, with the attributes, times and size the one read
    /// gave in <paramref name="data"/>.
    /// </summary>
    internal static bool FileExists(string fullPath, out Kernel32.WIN32_FILE_ATTRIBUTE_DATA data) =>
        FillAttributeInfo(fullPath, out data) == ERROR_SUCCESS
        && data.dwFileAttributes != InvalidFileAttributes
        && (data.dwFileAttributes & Kernel32.FILE_ATTRIBUTE_DIRECTORY) == 0;

    /// <summary>
    /// Whether <paramref name="fullPath"/> names a folder: something answers there, and it is
    /// a folder. A link to a folder counts as a folder, read by its own attributes.
    /// </summary>
    internal static bool DirectoryExists(string fullPath) => DirectoryExists(fullPath, out _);

    /// <summary>
    /// As <see cref="DirectoryExists(string)"/>, with the Windows error that stopped the
    /// read, or 0, in <paramref name="lastError"/>.
    /// </summary>
    internal static bool DirectoryExists(string fullPath, out int lastError)
    {
        lastError = FillAttributeInfo(fullPath, out var data);
        return lastError == ERROR_SUCCESS
            && data.dwFileAttributes != InvalidFileAttributes
            && (data.dwFileAttributes & Kernel32.FILE_ATTRIBUTE_DIRECTORY) != 0;
    }

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
    /// Creates the folder at <paramref name="fullPath"/> and every folder above it that is
    /// not there, as .NET 10's <c>FileSystem.CreateDirectory</c> does
    /// (<c>FileSystem.DirectoryCreation.Windows.cs</c>, <c>Interop.CreateDirectory.cs</c>).
    /// </summary>
    /// <remarks>
    /// A folder already there is not an error, whatever the folders above it allow. The
    /// missing folders are found from the bottom up and made from the top down, each with
    /// <c>\\?\</c> in front whatever its length, so the limit of 248 characters Windows
    /// sets for a new folder does not apply. Where one is refused, the rest are still tried,
    /// and the first refusal is thrown as the exception <see cref="Win32Marshal"/> gives for
    /// its error, naming the full path. A file in a folder's place, or a folder there that cannot be
    /// read, is thrown naming that folder. A root that is not there is a
    /// <see cref="DirectoryNotFoundException"/> naming the root.
    /// </remarks>
    internal static void CreateDirectory(string fullPath)
    {
        if (DirectoryExists(fullPath))
            return;

        var stackDir = new List<string>();

        var somepathexists = false;
        var length = fullPath.Length;

        // A separator at the end would otherwise make the same folder twice.
        if (length >= 2 && PathText.IsDirectorySeparator(fullPath[length - 1]))
            length--;

        var lengthRoot = PathText.GetRootLength(fullPath);
        if (length > lengthRoot)
        {
            var i = length - 1;
            while (i >= lengthRoot && !somepathexists)
            {
                var dir = fullPath.Substring(0, i + 1);

                if (!DirectoryExists(dir))
                    stackDir.Add(dir);
                else
                    somepathexists = true;

                while (i > lengthRoot && !PathText.IsDirectorySeparator(fullPath[i]))
                    i--;
                i--;
            }
        }

        var count = stackDir.Count;
        var r = true;
        var firstError = 0;
        var errorString = fullPath;

        var attributes = Kernel32.SECURITY_ATTRIBUTES.With(IntPtr.Zero);
        while (stackDir.Count > 0)
        {
            var name = stackDir[stackDir.Count - 1];
            stackDir.RemoveAt(stackDir.Count - 1);

            r = Kernel32.CreateDirectory(PathText.EnsureExtendedPrefix(name), in attributes);
            if (!r && firstError == 0)
            {
                var currentError = Marshal.GetLastWin32Error();

                // Already there is not a failure unless what is there is a file, or a folder
                // that cannot be read.
                if (currentError != Kernel32.ERROR_ALREADY_EXISTS)
                {
                    firstError = currentError;
                }
                else if (FileExists(name)
                         || (!DirectoryExists(name, out currentError) && currentError == ERROR_ACCESS_DENIED))
                {
                    firstError = currentError;
                    errorString = name;
                }
            }
        }

        // Nothing was missing and nothing was found: the root itself is not there.
        if (count == 0 && !somepathexists)
        {
            var root = PathText.GetPathRoot(fullPath);
            if (!DirectoryExists(root!))
                throw Win32Marshal.GetExceptionForWin32Error(ERROR_PATH_NOT_FOUND, root);
            return;
        }

        if (!r && firstError != 0)
            throw Win32Marshal.GetExceptionForWin32Error(firstError, errorString);
    }

    /// <summary>
    /// Removes the empty folder at <paramref name="fullPath"/>, as .NET 10's
    /// <c>FileSystem.RemoveDirectory</c> does with <c>recursive</c> false. Nothing there is a
    /// <see cref="DirectoryNotFoundException"/>, access denied an <see cref="IOException"/>
    /// carrying no Windows error number, and any other refusal the exception
    /// <see cref="Win32Marshal"/> gives for its error, each naming <paramref name="fullPath"/>.
    /// </summary>
    internal static void RemoveDirectory(string fullPath)
    {
        if (Kernel32.RemoveDirectory(PathText.EnsureExtendedPrefixIfNeeded(fullPath)))
            return;

        var errorCode = Marshal.GetLastWin32Error();
        if (errorCode == ERROR_FILE_NOT_FOUND)
            errorCode = ERROR_PATH_NOT_FOUND;
        if (errorCode == ERROR_ACCESS_DENIED)
            throw new IOException($"Access to the path '{fullPath}' is denied.");
        throw Win32Marshal.GetExceptionForWin32Error(errorCode, fullPath);
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
