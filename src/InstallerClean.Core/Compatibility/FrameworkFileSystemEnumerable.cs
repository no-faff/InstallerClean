using System.Collections;
using System.Runtime.InteropServices;
using InstallerClean.Helpers;
using InstallerClean.Interop.Native;
using Microsoft.Win32.SafeHandles;

namespace System.IO;

/// <summary>
/// One entry of a folder listing, as .NET 10's <c>FileSystemEntry</c> gives it on Windows:
/// the name, attributes, times and size the listing read, the folder as a full path, and the
/// folder as the caller gave it.
/// </summary>
internal readonly struct FrameworkFileSystemEntry(
    string originalRootDirectory,
    string directory,
    string fileName,
    Kernel32.WIN32_FILE_ATTRIBUTE_DATA data)
{
    /// <summary>The folder as the caller gave it.</summary>
    internal string OriginalRootDirectory { get; } = originalRootDirectory;

    /// <summary>
    /// The folder as a full path, with no separator at the end unless it is a root.
    /// </summary>
    internal string Directory { get; } = directory;

    internal string FileName { get; } = fileName;

    /// <summary>
    /// The attributes, times and size the listing read, laid out as .NET 10's
    /// <c>FileSystemInfo.Init</c> copies them.
    /// </summary>
    internal Kernel32.WIN32_FILE_ATTRIBUTE_DATA Data { get; } = data;

    internal FileAttributes Attributes => (FileAttributes)Data.dwFileAttributes;

    internal bool IsDirectory => (Attributes & FileAttributes.Directory) != 0;

    /// <summary>
    /// The entry's name joined to the folder's full path, as .NET 10's <c>ToFullPath</c>
    /// gives it and its FileInfo and DirectoryInfo listings name each entry.
    /// </summary>
    internal string ToFullPath() => PathText.Join(Directory, FileName);

    /// <summary>
    /// The entry's name joined to the folder as the caller gave it, as .NET 10's Directory
    /// members return it: a folder given as a drive and colon alone, <c>C:</c>, takes the
    /// name with nothing between.
    /// </summary>
    internal string ToSpecifiedFullPath() =>
        OriginalRootDirectory.Length == 2 && OriginalRootDirectory[1] == Path.VolumeSeparatorChar
            ? OriginalRootDirectory + FileName
            : PathText.Join(OriginalRootDirectory, FileName);
}

/// <summary>
/// The entries of one folder, not its subfolders, as .NET 10's <c>FileSystemEnumerable</c>
/// lists them on Windows with the options its Directory members use
/// (dotnet/runtime v10.0.0, <c>FileSystemEnumerator.Windows.cs</c>,
/// <c>FileSystemEnumerator.cs</c>, <c>FileSystemEnumerableFactory.cs</c> and
/// <c>EnumerationOptions.Compatible</c>): every entry but <c>.</c> and <c>..</c>, whatever
/// its attributes, read with NtQueryDirectoryFile through a handle on the folder.
/// </summary>
/// <remarks>
/// The folder is opened when the listing is made, so a folder that is not there, or cannot
/// be opened, throws there: a missing folder is a <see cref="DirectoryNotFoundException"/>
/// and any other refusal the exception <see cref="Win32Marshal"/> gives for its error, each
/// naming the folder. A refusal while the entries are read is thrown the same way from
/// <see cref="IEnumerator.MoveNext"/>. The first enumeration uses that first handle; each
/// later one opens the folder again.
/// </remarks>
internal sealed class FrameworkFileSystemEnumerable<T> : IEnumerable<T>
{
    private readonly string _directory;
    private readonly Func<FrameworkFileSystemEntry, T> _transform;
    private readonly Func<FrameworkFileSystemEntry, bool> _include;
    private readonly bool _isNormalized;
    private Enumerator? _first;

    /// <param name="directory">The folder to list.</param>
    /// <param name="transform">What each entry becomes.</param>
    /// <param name="include">Which entries are listed.</param>
    /// <param name="isNormalized">
    /// Whether <paramref name="directory"/> is already a full path, as .NET 10's
    /// <c>FileSystemEnumerator</c> takes it: where it is, the folder is not resolved again.
    /// </param>
    internal FrameworkFileSystemEnumerable(
        string directory,
        Func<FrameworkFileSystemEntry, T> transform,
        Func<FrameworkFileSystemEntry, bool> include,
        bool isNormalized = false)
    {
        _directory = directory ?? throw new ArgumentNullException(nameof(directory));
        _transform = transform;
        _include = include;
        _isNormalized = isNormalized;
        _first = new Enumerator(this);
    }

    public IEnumerator<T> GetEnumerator() =>
        Interlocked.Exchange(ref _first, null) ?? new Enumerator(this);

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    private sealed unsafe class Enumerator : IEnumerator<T>
    {
        private const int StandardBufferSize = 4096;

        private const int ERROR_FILE_NOT_FOUND = 2;
        private const int ERROR_PATH_NOT_FOUND = 3;

        private readonly FrameworkFileSystemEnumerable<T> _owner;
        private readonly string _originalRootDirectory;
        private readonly string _rootDirectory;
        private readonly object _lock = new();

        private SafeFileHandle? _directoryHandle;
        private IntPtr _buffer;
        private Ntdll.FILE_FULL_DIR_INFORMATION* _entry;
        private bool _lastEntryFound;
        private T _current = default!;

        internal Enumerator(FrameworkFileSystemEnumerable<T> owner)
        {
            _owner = owner;
            _originalRootDirectory = owner._directory;
            _rootDirectory = PathText.TrimEndingDirectorySeparator(
                owner._isNormalized ? owner._directory : FullPaths.GetFullPath(owner._directory));

            _directoryHandle = OpenDirectory(_rootDirectory);

            try
            {
                _buffer = Marshal.AllocHGlobal(StandardBufferSize);
            }
            catch
            {
                CloseDirectoryHandle();
                throw;
            }
        }

        public T Current => _current;

        object? IEnumerator.Current => Current;

        public bool MoveNext()
        {
            if (_lastEntryFound) return false;

            lock (_lock)
            {
                if (_lastEntryFound) return false;

                while (true)
                {
                    FindNextEntry();
                    if (_lastEntryFound) return false;

                    var entry = ToEntry(_entry);

                    if (entry.IsDirectory && (entry.FileName == "." || entry.FileName == ".."))
                        continue;

                    if (_owner._include(entry))
                    {
                        _current = _owner._transform(entry);
                        return true;
                    }
                }
            }
        }

        public void Reset() => throw new NotSupportedException();

        public void Dispose()
        {
            InternalDispose();
            GC.SuppressFinalize(this);
        }

        // A listing made and never read still lets its buffer go.
        ~Enumerator() => InternalDispose();

        private void InternalDispose()
        {
            lock (_lock)
            {
                _lastEntryFound = true;
                CloseDirectoryHandle();
                if (_buffer != IntPtr.Zero)
                {
                    Marshal.FreeHGlobal(_buffer);
                    _buffer = IntPtr.Zero;
                }
            }
        }

        // The data is copied as .NET 10's FileSystemInfo.Init copies it.
        private FrameworkFileSystemEntry ToEntry(Ntdll.FILE_FULL_DIR_INFORMATION* info) =>
            new(
                _originalRootDirectory,
                _rootDirectory,
                new string((char*)&info->FileName, 0, (int)(info->FileNameLength / sizeof(char))),
                new Kernel32.WIN32_FILE_ATTRIBUTE_DATA
                {
                    dwFileAttributes = info->FileAttributes,
                    ftCreationTime = *(Kernel32.FILETIME*)&info->CreationTime,
                    ftLastAccessTime = *(Kernel32.FILETIME*)&info->LastAccessTime,
                    ftLastWriteTime = *(Kernel32.FILETIME*)&info->LastWriteTime,
                    nFileSizeHigh = (uint)(info->EndOfFile >> 32),
                    nFileSizeLow = (uint)info->EndOfFile,
                });

        private void FindNextEntry()
        {
            if (_entry != null && _entry->NextEntryOffset != 0)
            {
                _entry = (Ntdll.FILE_FULL_DIR_INFORMATION*)((byte*)_entry + _entry->NextEntryOffset);
                return;
            }

            _entry = null;
            if (GetData())
                _entry = (Ntdll.FILE_FULL_DIR_INFORMATION*)_buffer;
        }

        /// <summary>
        /// Reads the next entries into the buffer: true where some were read. The end of the
        /// listing, and a volume root with nothing in it, end the listing; any other answer
        /// throws the exception <see cref="Win32Marshal"/> gives for its error, naming the
        /// folder.
        /// </summary>
        private bool GetData()
        {
            var status = Ntdll.NtQueryDirectoryFile(
                _directoryHandle!,
                IntPtr.Zero,
                IntPtr.Zero,
                IntPtr.Zero,
                out _,
                _buffer,
                StandardBufferSize,
                Ntdll.FileFullDirectoryInformation,
                ReturnSingleEntry: 0,
                IntPtr.Zero,
                RestartScan: 0);

            switch (status)
            {
                case Ntdll.STATUS_NO_MORE_FILES:
                case Ntdll.STATUS_FILE_NOT_FOUND:
                    DirectoryFinished();
                    return false;
                case Ntdll.STATUS_SUCCESS:
                    return true;
                default:
                    throw Win32Marshal.GetExceptionForWin32Error((int)Ntdll.RtlNtStatusToDosError(status), _rootDirectory);
            }
        }

        private void DirectoryFinished()
        {
            _entry = null;
            CloseDirectoryHandle();
            _lastEntryFound = true;
        }

        private void CloseDirectoryHandle()
        {
            var handle = Interlocked.Exchange(ref _directoryHandle, null);
            handle?.Dispose();
        }

        private static SafeFileHandle OpenDirectory(string path)
        {
            var handle = Kernel32.CreateFile(
                PathText.EnsureExtendedPrefixIfNeeded(path),
                Kernel32.FILE_LIST_DIRECTORY,
                Kernel32.FILE_SHARE_ALL,
                IntPtr.Zero,
                Kernel32.OPEN_EXISTING,
                Kernel32.FILE_FLAG_BACKUP_SEMANTICS,
                IntPtr.Zero);

            if (!handle.IsInvalid) return handle;

            var error = Marshal.GetLastWin32Error();
            handle.Dispose();

            // A folder that is not there is a DirectoryNotFoundException, as .NET 10 gives it.
            if (error == ERROR_FILE_NOT_FOUND)
                error = ERROR_PATH_NOT_FOUND;
            throw Win32Marshal.GetExceptionForWin32Error(error, path);
        }
    }
}
