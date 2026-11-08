using System.Diagnostics.CodeAnalysis;
using InstallerClean.Helpers;
using InstallerClean.Interop.Native;

namespace System.IO;

/// <summary>
/// <c>File.Exists</c> and <c>File.GetAttributes</c>, called in place of .NET Framework's,
/// and <c>File.Move(string, string, bool)</c>, which .NET Framework 4.8 does not have, as
/// .NET 10 answers and moves a file on Windows (dotnet/runtime v10.0.0, <c>File.cs</c>,
/// <c>FileSystem.Attributes.Windows.cs</c>, <c>FileSystem.Windows.cs</c> and
/// <c>Interop.MoveFileEx.cs</c>).
/// </summary>
internal static class FrameworkFile
{
    /// <summary>
    /// Whether <paramref name="path"/> names a file, as .NET 10's <c>File.Exists</c> answers it
    /// (<c>File.cs</c>): false for a null or empty path, for one whose full path ends in a
    /// separator, for a folder, and for any path that cannot be read; a path that is not
    /// rooted is read from the working directory.
    /// </summary>
    internal static bool Exists([NotNullWhen(true)] string? path)
    {
        try
        {
            if (path == null)
                return false;
            if (path.Length == 0)
                return false;

            path = FullPaths.GetFullPath(path);

            // A separator at the end would be taken off by the read below, which could then
            // answer for a file of that name.
            if (path.Length > 0 && PathText.IsDirectorySeparator(path[path.Length - 1]))
                return false;

            return FrameworkFileSystem.FileExists(path);
        }
        catch (ArgumentException) { }
        catch (IOException) { }
        catch (UnauthorizedAccessException) { }

        return false;
    }

    /// <summary>
    /// The attributes of the file or folder at <paramref name="path"/>, as .NET 10's
    /// <c>File.GetAttributes</c> reads them: the full path is read, and a refusal from
    /// Windows, nothing being there included, is the exception <see cref="Win32Marshal"/>
    /// gives for its error, naming the full path.
    /// </summary>
    internal static FileAttributes GetAttributes(string path) =>
        FrameworkFileSystem.GetAttributes(FullPaths.GetFullPath(path));

    extension(File)
    {
        /// <summary>
        /// Moves the file at <paramref name="sourceFileName"/> to
        /// <paramref name="destFileName"/>, on the same volume or another.
        /// </summary>
        /// <remarks>
        /// Both paths are made full paths first, and the source must then be a file: a
        /// folder, or nothing, is a <see cref="FileNotFoundException"/> naming the source's
        /// full path, as MoveFileExW itself would move a folder. The move is MoveFileExW with
        /// MOVEFILE_COPY_ALLOWED, so a move to another volume copies the file and deletes the
        /// source, and with MOVEFILE_REPLACE_EXISTING as well where
        /// <paramref name="overwrite"/> is true, which on one volume replaces a file or a link
        /// at the destination in the same step, never writing through the link. A path of
        /// 260 characters or more, or one ending in a space or a period, goes to Windows with
        /// <c>\\?\</c> in front.
        ///
        /// A null path is an <see cref="ArgumentNullException"/> and an empty one an
        /// <see cref="ArgumentException"/>, the source checked first. A refusal from Windows
        /// is the exception <see cref="Win32Marshal"/> gives for its error, naming no path:
        /// an existing destination is an <see cref="IOException"/> whose HResult carries 183
        /// on the same volume or 80 on another, and a source in use one carrying 32 or 33.
        /// </remarks>
        public static void Move(string sourceFileName, string destFileName, bool overwrite)
        {
            ArgumentException.ThrowIfNullOrEmpty(sourceFileName);
            ArgumentException.ThrowIfNullOrEmpty(destFileName);

            var fullSourceFileName = FullPaths.GetFullPath(sourceFileName);
            var fullDestFileName = FullPaths.GetFullPath(destFileName);

            if (!FrameworkFileSystem.FileExists(fullSourceFileName))
                throw new FileNotFoundException($"Could not find file '{fullSourceFileName}'.", fullSourceFileName);

            var flags = Kernel32.MOVEFILE_COPY_ALLOWED;
            if (overwrite) flags |= Kernel32.MOVEFILE_REPLACE_EXISTING;

            if (!Kernel32.MoveFileEx(
                    PathText.EnsureExtendedPrefixIfNeeded(fullSourceFileName),
                    PathText.EnsureExtendedPrefixIfNeeded(fullDestFileName),
                    flags))
            {
                throw Win32Marshal.GetExceptionForLastWin32Error();
            }
        }
    }
}
