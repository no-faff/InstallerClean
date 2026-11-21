using System.Runtime.InteropServices;
using System.Text;
using InstallerClean.Helpers;
using InstallerClean.Interop.Native;
using Microsoft.Win32.SafeHandles;

namespace System.IO;

/// <summary>
/// A file opened by path to read, as .NET 10's <c>FileStream</c> opens one on Windows (dotnet/runtime
/// v10.0.0, <c>FileStream.cs</c>, <c>FileStreamHelpers.cs</c>, <c>OSFileStreamStrategy.cs</c>,
/// <c>SafeFileHandle.Windows.cs</c> and <c>Interop.CreateFile.cs</c>), and
/// <c>File.ReadAllText</c>, <c>File.ReadAllBytes</c> and <c>File.OpenRead</c> built on it as
/// .NET 10 builds them (<c>File.cs</c>, <c>StreamReader.cs</c>), called in place of
/// .NET Framework's.
/// </summary>
/// <remarks>
/// .NET Framework's own FileStream runs the path through its normaliser before Windows is
/// asked, so a name ending in a no-break space opens the file without it, a <c>|</c> is an
/// <see cref="ArgumentException"/>, and a path of 260 characters or more fails where the
/// long-path policy is off. Here the path is made full by .NET 10's rules, which take a
/// period or a space off the end of a name as Windows does and keep a no-break space, and
/// goes to CreateFileW with <c>\\?\</c> in front where it is 260 characters or longer; a
/// refusal is the exception <see cref="Win32Marshal"/> gives for its error, naming the full
/// path. The stream over the handle is .NET Framework's, with its default buffer of 4096
/// bytes, which is .NET 10's too.
/// <para>
/// A file is opened to read only. A write goes through <c>StorageHelpers.OpenAtomic</c>,
/// which refuses a link at the name, so access with <see cref="FileAccess.Write"/> is a
/// <see cref="NotSupportedException"/> here, as are an asynchronous handle and an
/// inheritable one, which no caller asks for.
/// </para>
/// </remarks>
internal static class FrameworkFileStream
{
    private const int ERROR_ACCESS_DENIED = 5;
    private const int ERROR_PATH_NOT_FOUND = 3;

    // SECURITY_SQOS_PRESENT with SECURITY_ANONYMOUS, which is 0: a named pipe opened by
    // path cannot take on the identity of this process.
    private const uint SECURITY_SQOS_PRESENT = 0x00100000;

    private const int MaxArrayLength = 0x7FFFFFC7;

    // .NET 10's FileStreamHelpers.ValidFileOptions, with NoBuffering and BackupOrRestore,
    // which FileOptions does not name.
    private const FileOptions ValidFileOptions = FileOptions.WriteThrough | FileOptions.Asynchronous
        | FileOptions.RandomAccess | FileOptions.DeleteOnClose | FileOptions.SequentialScan
        | FileOptions.Encrypted | (FileOptions)0x20000000 | (FileOptions)0x02000000;

    /// <summary>
    /// Opens the file at <paramref name="path"/> to read, as .NET 10's
    /// <c>new FileStream(path, mode, access, share)</c> opens it, for a synchronous stream.
    /// A null path is an <see cref="ArgumentNullException"/>, an empty one an
    /// <see cref="ArgumentException"/>, and a mode, access or share out of range, or a mode
    /// that writes given read access alone, an <see cref="ArgumentOutOfRangeException"/> or
    /// <see cref="ArgumentException"/> as .NET 10 gives them.
    /// </summary>
    internal static FileStream Open(
        string path, FileMode mode, FileAccess access, FileShare share, FileOptions options = FileOptions.None)
    {
        ValidateArguments(path, mode, access, share, options);

        if ((access & FileAccess.Write) != 0)
            throw new NotSupportedException("Not ported: open a file to write through StorageHelpers.OpenAtomic.");
        if ((options & FileOptions.Asynchronous) != 0)
            throw new NotSupportedException("Not ported: an asynchronous handle.");
        if ((share & FileShare.Inheritable) != 0)
            throw new NotSupportedException("Not ported: an inheritable handle.");

        var handle = OpenHandle(FullPaths.GetFullPath(path), mode, access, share, options);
        try
        {
            return new FileStream(handle, access);
        }
        catch
        {
            handle.Dispose();
            throw;
        }
    }

    /// <summary>
    /// The text of the file at <paramref name="path"/>, opened as .NET 10's
    /// <c>File.ReadAllText</c> opens it and decoded as UTF-8 unless a byte-order mark at the
    /// start names another encoding. The decoding is .NET Framework's, which replaces bytes
    /// that are not UTF-8 with U+FFFD in other places than .NET 10's does.
    /// </summary>
    internal static string ReadAllText(string path)
    {
        ArgumentException.ThrowIfNullOrEmpty(path);

        using var reader = new StreamReader(
            Open(path, FileMode.Open, FileAccess.Read, FileShare.Read),
            Encoding.UTF8,
            detectEncodingFromByteOrderMarks: true);
        return reader.ReadToEnd();
    }

    /// <summary>
    /// The bytes of the file at <paramref name="path"/>, as .NET 10's <c>File.ReadAllBytes</c>
    /// reads them: a file whose size is over the largest array is an
    /// <see cref="IOException"/>, one that ends before its size is an
    /// <see cref="EndOfStreamException"/>, and one whose size reads 0 is read to its end.
    /// </summary>
    internal static byte[] ReadAllBytes(string path)
    {
        using var stream = Open(path, FileMode.Open, FileAccess.Read, FileShare.Read, FileOptions.SequentialScan);

        long length = 0;
        if (stream.CanSeek && (length = stream.Length) > MaxArrayLength)
            throw new IOException(
                "The file is too long. This operation is currently limited to supporting files less than 2 gigabytes in size.");

        if (length == 0)
        {
            using var copy = new MemoryStream();
            stream.CopyTo(copy);
            return copy.ToArray();
        }

        var bytes = new byte[(int)length];
        var index = 0;
        while (index < bytes.Length)
        {
            var read = stream.Read(bytes, index, bytes.Length - index);
            if (read == 0) throw new EndOfStreamException("Unable to read beyond the end of the stream.");
            index += read;
        }
        return bytes;
    }

    /// <summary>
    /// The file at <paramref name="path"/> opened to read, as .NET 10's <c>File.OpenRead</c>
    /// opens it: others may read it while it is open.
    /// </summary>
    internal static FileStream OpenRead(string path) =>
        Open(path, FileMode.Open, FileAccess.Read, FileShare.Read);

    /// <summary>.NET 10's <c>FileStreamHelpers.ValidateArguments</c>, for a synchronous open with no preallocation.</summary>
    private static void ValidateArguments(
        string path, FileMode mode, FileAccess access, FileShare share, FileOptions options)
    {
        ArgumentException.ThrowIfNullOrEmpty(path);

        var tempShare = share & ~FileShare.Inheritable;
        string? badArg = null;
        if (mode < FileMode.CreateNew || mode > FileMode.Append)
            badArg = nameof(mode);
        else if (access < FileAccess.Read || access > FileAccess.ReadWrite)
            badArg = nameof(access);
        else if (tempShare < FileShare.None || tempShare > (FileShare.ReadWrite | FileShare.Delete))
            badArg = nameof(share);

        if (badArg != null)
            throw new ArgumentOutOfRangeException(badArg, "Enum value was out of legal range.");

        if (options != FileOptions.None && (options & ~ValidFileOptions) != 0)
            throw new ArgumentOutOfRangeException(nameof(options), "Enum value was out of legal range.");

        if ((access & FileAccess.Write) == 0
            && mode is FileMode.Truncate or FileMode.CreateNew or FileMode.Create or FileMode.Append)
            throw new ArgumentException(
                $"Combining FileMode: {mode} with FileAccess: {access} is invalid.", nameof(access));

        if ((access & FileAccess.Read) != 0 && mode == FileMode.Append)
            throw new ArgumentException(
                "Append access can be requested only in write-only mode.", nameof(access));
    }

    /// <summary>
    /// .NET 10's <c>SafeFileHandle.Open</c> for a synchronous handle: CreateFileW with no box
    /// asking for a disc or a card while it opens, and a refusal thrown as
    /// <see cref="Win32Marshal"/> gives it, naming <paramref name="fullPath"/>.
    /// </summary>
    /// <remarks>
    /// .NET 10 passes a SECURITY_ATTRIBUTES whose every field is zero where the share is not
    /// inheritable, which CreateFileW reads as no attributes, as it reads the null passed
    /// here.
    /// </remarks>
    private static SafeFileHandle OpenHandle(
        string fullPath, FileMode mode, FileAccess access, FileShare share, FileOptions options)
    {
        var desiredAccess = (access & FileAccess.Read) == FileAccess.Read ? Kernel32.GENERIC_READ : 0;

        var flags = (uint)options | SECURITY_SQOS_PRESENT;

        SafeFileHandle handle;
        int errorCode;
        var promptOff = Kernel32.SetThreadErrorMode(Kernel32.SEM_FAILCRITICALERRORS, out var oldMode);
        try
        {
            handle = Kernel32.CreateFile(
                PathText.EnsureExtendedPrefixIfNeeded(fullPath),
                desiredAccess, (uint)share, IntPtr.Zero, (uint)mode, flags, IntPtr.Zero);
            errorCode = handle.IsInvalid ? Marshal.GetLastWin32Error() : 0;
        }
        finally
        {
            if (promptOff) Kernel32.SetThreadErrorMode(oldMode, out _);
        }

        if (!handle.IsInvalid) return handle;

        handle.Dispose();

        // A drive root opened as a file answers ERROR_PATH_NOT_FOUND; .NET 10 reports it
        // as it reports every other folder.
        if (errorCode == ERROR_PATH_NOT_FOUND && fullPath.Length == PathText.GetRootLength(fullPath))
            errorCode = ERROR_ACCESS_DENIED;

        throw Win32Marshal.GetExceptionForWin32Error(errorCode, fullPath);
    }
}
