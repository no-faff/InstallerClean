using InstallerClean.Helpers;
using InstallerClean.Interop.Native;

namespace System.IO;

/// <summary>
/// <c>File.Move(string, string, bool)</c>, which .NET Framework 4.8 does not have, as .NET 10
/// moves a file on Windows (dotnet/runtime v10.0.0, <c>File.cs</c>,
/// <c>FileSystem.Windows.cs</c> and <c>Interop.MoveFileEx.cs</c>).
/// </summary>
internal static class FrameworkFile
{
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
        internal static void Move(string sourceFileName, string destFileName, bool overwrite)
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
