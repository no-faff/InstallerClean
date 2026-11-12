using System.Runtime.InteropServices;
using InstallerClean.Helpers;
using InstallerClean.Interop.Native;

namespace InstallerClean.Services;

/// <summary>
/// Production <see cref="IFileTimesReader"/>: opens the file for its attributes
/// alone and asks the one handle for the file's times, the volume it is on and that
/// volume's file system.
///
/// IT USES THE REAL FILESYSTEM AND TAKES NO <c>IFileSystem</c>, for the reason
/// <see cref="FileIdentityReader"/> gives: a file system's name and the times it
/// keeps are facts about a volume, and there is no abstraction over them to inject.
/// Every failure in here keeps the file back.
///
/// EVERY QUESTION IS PUT TO THE HANDLE, NEVER TO THE PATH. A path can reach a file
/// through junctions, mount points, substituted drives and symbolic links, and its
/// spelling says nothing reliable about which volume the file is on. The handle is
/// opened after all of that has been followed, so the volume it answers for is the
/// file's own.
/// </summary>
internal sealed class FileTimesReader : IFileTimesReader
{
    // Room for a volume GUID path and far more of the file's path than a file
    // directly in the cache folder has, so the retry below is for the unexpected.
    private const int FinalPathBufferLength = 512;

    // MAX_PATH + 1, the size Win32 documents for the file-system name buffer.
    private const int FileSystemNameBufferLength = 261;

    private const string VolumeGuidPrefix = @"\\?\Volume{";

    /// <inheritdoc />
    public FileTimesRead ReadOutcome(string path, out FileTimes times)
    {
        times = default;
        if (string.IsNullOrEmpty(path)) return FileTimesRead.NotAPath;

        try
        {
            // FILE_READ_ATTRIBUTES and every share flag. The right asked for is
            // outside the data-sharing check, so this open does not fail on a file
            // Windows Installer holds, and while it is open another process can still
            // open the file and delete it. What it does hold off is a rename that
            // would replace the file: Windows refuses that while this handle is open.
            // The handle is closed when this method returns.
            //
            // FILE_FLAG_OPEN_REPARSE_POINT so a link is opened as itself rather than
            // followed, and reported below. FILE_FLAG_BACKUP_SEMANTICS so a path that
            // turns out to name a directory opens and is reported below, rather than
            // failing as a refusal.
            using var handle = Kernel32.CreateFile(
                FullPaths.ForWindowsCall(path),
                Kernel32.FILE_READ_ATTRIBUTES,
                Kernel32.FILE_SHARE_ALL,
                IntPtr.Zero,
                Kernel32.OPEN_EXISTING,
                Kernel32.FILE_FLAG_OPEN_REPARSE_POINT | Kernel32.FILE_FLAG_BACKUP_SEMANTICS,
                IntPtr.Zero);

            if (handle.IsInvalid)
            {
                // Read at once and only on this branch; StorageHelpers.OpenForMetadata says why.
                var error = Marshal.GetLastWin32Error();
                return error is Kernel32.ERROR_FILE_NOT_FOUND or Kernel32.ERROR_PATH_NOT_FOUND
                    ? FileTimesRead.NamesNothing
                    : FileTimesRead.OpenRefused;
            }

            // Every question is asked before any answer is judged, so that what the
            // answers mean is decided in one place that takes no handle. Each call
            // reads and none of them changes anything.
            var basicRead = Kernel32.GetFileBasicInfoByHandle(
                handle,
                Kernel32.FileBasicInfo,
                out var basic,
                (uint)System.Runtime.CompilerServices.Unsafe.SizeOf<Kernel32.FILE_BASIC_INFO>());
            var volume = VolumeOf(handle);

            return Classify(
                new HandleAnswers(
                    basicRead ? basic : null,
                    volume,
                    volume is null ? DriveType.Unknown : StorageHelpers.GetDriveKind(volume),
                    FileSystemOf(handle)),
                out times);
        }
        catch
        {
            return FileTimesRead.Faulted;
        }
    }

    /// <summary>
    /// What Windows answered about the file behind one open handle, before anything is
    /// concluded from it.
    /// </summary>
    /// <param name="Basic">The file's times and attributes, or null where the call failed.</param>
    /// <param name="VolumeRoot">
    /// The <c>\\?\Volume{guid}\</c> root of the volume holding the file, or null where
    /// the handle yields none.
    /// </param>
    /// <param name="DriveKind">That volume's drive type. Not read where there is no root.</param>
    /// <param name="FileSystemName">
    /// The name of that volume's file system, or null where the call failed.
    /// </param>
    internal readonly record struct HandleAnswers(
        Kernel32.FILE_BASIC_INFO? Basic,
        string? VolumeRoot,
        DriveType DriveKind,
        string? FileSystemName);

    /// <summary>
    /// What the answers establish, and the file's times where they establish all of it.
    ///
    /// <see cref="FileTimesRead.Read"/> ONLY FOR A PLAIN FILE WHOSE THREE TIMES ARE
    /// READABLE AND WHOSE CHANGE TIME IS NOT ZERO, ON A VOLUME NAMED BY A GUID PATH, OF A
    /// FIXED DRIVE, WHOSE FILE SYSTEM IS EXACTLY NTFS. Any answer missing or different
    /// keeps the file back, and the checks run in that order, so the first one short
    /// names the outcome.
    /// </summary>
    internal static FileTimesRead Classify(HandleAnswers answers, out FileTimes times)
    {
        times = default;

        if (answers.Basic is not { } basic) return FileTimesRead.TimesUnavailable;

        if ((basic.FileAttributes & (Kernel32.FILE_ATTRIBUTE_REPARSE_POINT | FileAttributeDirectory)) != 0)
            return FileTimesRead.NotAPlainFile;

        // A change time of zero is NTFS not answering rather than a file last
        // changed in 1601, and a negative value is not a time at all. Either
        // would read as a very old file, so both are refused here.
        if (!TryFromFileTime(basic.ChangeTime, out var change)
            || basic.ChangeTime == 0
            || !TryFromFileTime(basic.CreationTime, out var creation)
            || !TryFromFileTime(basic.LastWriteTime, out var lastWrite))
            return FileTimesRead.TimesUnavailable;

        if (answers.VolumeRoot is null) return FileTimesRead.VolumeUnestablished;

        if (answers.DriveKind != DriveType.Fixed) return FileTimesRead.NotAFixedVolume;

        if (answers.FileSystemName is null) return FileTimesRead.VolumeUnestablished;

        // Exactly NTFS. Any other name, ReFS included, keeps the file back.
        if (!string.Equals(answers.FileSystemName, "NTFS", StringComparison.Ordinal))
            return FileTimesRead.NotNtfs;

        times = new FileTimes(creation, lastWrite, change);
        return FileTimesRead.Read;
    }

    /// <summary>
    /// The name of the file system on the volume holding the file behind
    /// <paramref name="handle"/>, or null where the call fails.
    /// </summary>
    private static string? FileSystemOf(Microsoft.Win32.SafeHandles.SafeFileHandle handle)
    {
        var fileSystem = new char[FileSystemNameBufferLength];
        if (!Kernel32.GetVolumeInformationByHandle(
                handle, IntPtr.Zero, 0, IntPtr.Zero, IntPtr.Zero, IntPtr.Zero,
                fileSystem, (uint)fileSystem.Length))
            return null;

        var end = Array.IndexOf(fileSystem, '\0');
        return new string(fileSystem, 0, end < 0 ? fileSystem.Length : end);
    }

    // FILE_ATTRIBUTE_DIRECTORY. Declared here rather than in Kernel32 because this
    // is its only reader.
    private const uint FileAttributeDirectory = 0x10;

    /// <summary>
    /// The GUID path of the volume holding the file behind <paramref name="handle"/>,
    /// <c>\\?\Volume{guid}\</c>, or null where the handle yields none.
    ///
    /// NULL FOR ANYTHING BUT A GUID PATH, and that is what makes it safe to build on.
    /// A local volume has one. A call that fails, and an answer of any other shape,
    /// both come back null, and the file is kept.
    /// </summary>
    internal static string? VolumeOf(Microsoft.Win32.SafeHandles.SafeFileHandle handle)
    {
        var buffer = new char[FinalPathBufferLength];
        var length = Kernel32.GetFinalPathNameByHandle(
            handle, buffer, (uint)buffer.Length, Kernel32.VOLUME_NAME_GUID);
        if (length == 0) return null;
        if (length >= buffer.Length)
        {
            // Too small: the required size, terminator included, was returned.
            buffer = new char[length];
            length = Kernel32.GetFinalPathNameByHandle(
                handle, buffer, (uint)buffer.Length, Kernel32.VOLUME_NAME_GUID);
            if (length == 0 || length >= buffer.Length) return null;
        }

        return VolumeRoot(new string(buffer, 0, (int)length));
    }

    /// <summary>
    /// The <c>\\?\Volume{guid}\</c> root at the start of <paramref name="finalPath"/>,
    /// or null where it does not start with one.
    /// </summary>
    internal static string? VolumeRoot(string finalPath)
    {
        if (!finalPath.StartsWith(VolumeGuidPrefix, StringComparison.OrdinalIgnoreCase))
            return null;

        var close = finalPath.IndexOfOrdinal("}\\", VolumeGuidPrefix.Length, StringComparison.Ordinal);
        return close < 0 ? null : finalPath.Substring(0, close + 2);
    }

    private static bool TryFromFileTime(long fileTime, out DateTime value)
    {
        value = default;
        if (fileTime < 0 || fileTime > DateTime.MaxValue.ToFileTimeUtc()) return false;
        value = DateTime.FromFileTimeUtc(fileTime);
        return true;
    }
}
