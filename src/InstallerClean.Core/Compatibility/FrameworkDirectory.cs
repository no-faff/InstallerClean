using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;
using InstallerClean.Helpers;
using InstallerClean.Interop.Native;

namespace System.IO;

/// <summary>
/// <c>Directory.Exists</c> and <c>Directory.Move</c> as .NET 10 answers whether a folder
/// is there and moves one on Windows (dotnet/runtime v10.0.0, <c>Directory.cs</c>,
/// <c>FileSystem.cs</c>, <c>FileSystem.Windows.cs</c> and <c>Interop.MoveFileEx.cs</c>),
/// called in place of .NET Framework's.
/// </summary>
/// <remarks>
/// .NET Framework's own Move passes the paths to MoveFileW as the caller gave them, so a path of
/// 260 characters or more fails where the long-path policy is off, refuses a rename that
/// changes only the case of the name, and names neither path when the destination exists.
/// </remarks>
internal static class FrameworkDirectory
{
    private const int ERROR_FILE_NOT_FOUND = 2;
    private const int ERROR_PATH_NOT_FOUND = 3;
    private const int ERROR_ACCESS_DENIED = 5;
    private const int ERROR_ALREADY_EXISTS = 183;

    /// <summary>
    /// Whether <paramref name="path"/> names a folder, as .NET 10's <c>Directory.Exists</c>
    /// answers it (<c>Directory.cs</c>): false for a null or empty path, for a file, and for
    /// any path that cannot be read; a path that is not rooted is read from the working
    /// directory.
    /// </summary>
    internal static bool Exists([NotNullWhen(true)] string? path)
    {
        try
        {
            if (path == null)
                return false;
            if (path.Length == 0)
                return false;

            return FrameworkFileSystem.DirectoryExists(FullPaths.GetFullPath(path));
        }
        catch (ArgumentException) { }
        catch (IOException) { }
        catch (UnauthorizedAccessException) { }

        return false;
    }

    /// <summary>
    /// Creates the folder at <paramref name="path"/> and every folder above it that is not
    /// there, as .NET 10's <c>Directory.CreateDirectory</c> does; a folder already there is
    /// not an error. A null path is an <see cref="ArgumentNullException"/> and an empty one an
    /// <see cref="ArgumentException"/>. Each folder goes to Windows with <c>\\?\</c> in front
    /// whatever its length.
    /// </summary>
    internal static void CreateDirectory(string path)
    {
        ArgumentException.ThrowIfNullOrEmpty(path);
        FrameworkFileSystem.CreateDirectory(FullPaths.GetFullPath(path));
    }

    /// <summary>
    /// Removes the empty folder at <paramref name="path"/>, as .NET 10's
    /// <c>Directory.Delete(string)</c> does: a folder with anything in it stays, and is an
    /// <see cref="IOException"/>.
    /// </summary>
    internal static void Delete(string path) =>
        FrameworkFileSystem.RemoveDirectory(FullPaths.GetFullPath(path));

    /// <summary>
    /// The full paths of the files directly in the folder at <paramref name="path"/> whose
    /// names match <paramref name="searchPattern"/>, each spelled from
    /// <paramref name="path"/> as given, as .NET 10's <c>Directory.EnumerateFiles</c> lists
    /// them (<see cref="FrameworkFileSystemEnumerable{T}"/>). The pattern is matched against
    /// each entry's own name only.
    /// </summary>
    internal static IEnumerable<string> EnumerateFiles(string path, string searchPattern) =>
        EnumeratePaths(path, searchPattern, files: true, directories: false);

    /// <summary>
    /// The full paths of the folders directly in the folder at <paramref name="path"/> whose
    /// names match <paramref name="searchPattern"/>, as .NET 10's
    /// <c>Directory.EnumerateDirectories</c> lists them.
    /// </summary>
    internal static IEnumerable<string> EnumerateDirectories(string path, string searchPattern) =>
        EnumeratePaths(path, searchPattern, files: false, directories: true);

    /// <summary>
    /// The full paths of everything directly in the folder at <paramref name="path"/>, as
    /// .NET 10's <c>Directory.EnumerateFileSystemEntries</c> lists them.
    /// </summary>
    internal static IEnumerable<string> EnumerateFileSystemEntries(string path) =>
        EnumeratePaths(path, "*", files: true, directories: true);

    private static IEnumerable<string> EnumeratePaths(string path, string searchPattern, bool files, bool directories)
    {
        if (path is null) throw new ArgumentNullException(nameof(path));
        if (searchPattern is null) throw new ArgumentNullException(nameof(searchPattern));

        NormalizeInputs(ref path, ref searchPattern);

        return new FrameworkFileSystemEnumerable<string>(
            path,
            entry => entry.ToSpecifiedFullPath(),
            entry => (entry.IsDirectory ? directories : files)
                && FrameworkFileSystemName.MatchesWin32Expression(searchPattern.AsSpan(), entry.FileName.AsSpan(), ignoreCase: true));
    }

    /// <summary>
    /// The folder and pattern as .NET 10's <c>FileSystemEnumerableFactory.NormalizeInputs</c>
    /// leaves them for Win32 matching: a folder at the front of the pattern moves onto the
    /// folder, and the rest of the pattern is put into the form the matcher reads. False
    /// where a folder moved onto <paramref name="directory"/>, which then has to be resolved
    /// again; true otherwise.
    /// </summary>
    internal static bool NormalizeInputs(ref string directory, ref string expression)
    {
        if (PathText.IsPathRooted(expression))
            throw new ArgumentException("Second path fragment must not be a drive or UNC name.", nameof(expression));
        if (expression.IndexOf('\0') >= 0)
            throw new ArgumentException("Null character in path.", expression);
        if (directory.IndexOf('\0') >= 0)
            throw new ArgumentException("Null character in path.", directory);

        var directoryUnchanged = true;

        var directoryName = PathText.GetDirectoryNameAsWritten(expression);
        if (directoryName.Length != 0)
        {
            directory = PathText.Join(directory, directoryName);
            expression = expression.Substring(directoryName.Length + 1);
            directoryUnchanged = false;
        }

        if (expression != "*")
        {
            expression = string.IsNullOrEmpty(expression) || expression == "." || expression == "*.*"
                ? "*"
                : FrameworkFileSystemName.TranslateWin32Expression(expression);
        }

        return directoryUnchanged;
    }

    /// <summary>
    /// Moves the folder at <paramref name="sourceDirName"/>, with everything in it, to
    /// <paramref name="destDirName"/> on the same volume. A rename that changes only the case
    /// of the folder's name is made.
    /// </summary>
    /// <remarks>
    /// Both paths are made full paths first. A null path is an
    /// <see cref="ArgumentNullException"/> and an empty one an <see cref="ArgumentException"/>,
    /// the source checked first. Two paths naming the same folder by the same name, and two
    /// on different roots, are each an <see cref="IOException"/> before Windows is asked. The
    /// move is MoveFileExW with MOVEFILE_COPY_ALLOWED and nothing replaced, each path with
    /// <c>\\?\</c> in front where it is 260 characters or more or ends in a space or a period.
    /// A source that is not there is a <see cref="DirectoryNotFoundException"/> naming it, an
    /// existing destination an <see cref="IOException"/> naming it, access denied an
    /// <see cref="IOException"/> naming the source, and any other refusal the exception
    /// <see cref="Win32Marshal"/> gives for its error, naming no path.
    /// </remarks>
    internal static void Move(string sourceDirName, string destDirName)
    {
        ArgumentException.ThrowIfNullOrEmpty(sourceDirName);
        ArgumentException.ThrowIfNullOrEmpty(destDirName);

        var sourceFullPath = FullPaths.GetFullPath(sourceDirName);
        var destFullPath = FullPaths.GetFullPath(destDirName);

        var source = PathText.TrimEndingDirectorySeparator(sourceFullPath);
        var destination = PathText.TrimEndingDirectorySeparator(destFullPath);

        // The same path, unless only the case of the folder's own name differs.
        if (string.Equals(source, destination, StringComparison.OrdinalIgnoreCase)
            && string.Equals(PathText.GetFileName(source), PathText.GetFileName(destination), StringComparison.Ordinal))
        {
            throw new IOException("Source and destination path must be different.");
        }

        if (!string.Equals(
                PathText.GetPathRoot(sourceFullPath) ?? "",
                PathText.GetPathRoot(destFullPath) ?? "",
                StringComparison.OrdinalIgnoreCase))
        {
            throw new IOException("Source and destination path must have identical roots. Move will not work across volumes.");
        }

        if (!Kernel32.MoveFileEx(
                PathText.EnsureExtendedPrefixIfNeeded(sourceFullPath),
                PathText.EnsureExtendedPrefixIfNeeded(destFullPath),
                Kernel32.MOVEFILE_COPY_ALLOWED))
        {
            var errorCode = Marshal.GetLastWin32Error();

            if (errorCode == ERROR_FILE_NOT_FOUND)
                throw Win32Marshal.GetExceptionForWin32Error(ERROR_PATH_NOT_FOUND, sourceFullPath);

            if (errorCode == ERROR_ALREADY_EXISTS)
                throw Win32Marshal.GetExceptionForWin32Error(ERROR_ALREADY_EXISTS, destFullPath);

            if (errorCode == ERROR_ACCESS_DENIED)
                throw new IOException(
                    $"Access to the path '{sourceFullPath}' is denied.", Win32Marshal.MakeHRFromErrorCode(errorCode));

            throw Win32Marshal.GetExceptionForWin32Error(errorCode);
        }
    }
}
