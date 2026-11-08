using System.IO.Abstractions;
using InstallerClean.Helpers;
using InstallerClean.Interop.Native;

namespace InstallerClean.Services;

/// <summary>
/// The app's <see cref="IFileInfoFactory"/>: each <see cref="IFileInfo"/> it makes is a
/// <see cref="FrameworkFileInfo"/>, which reads the file as .NET 10's FileInfo does.
/// </summary>
internal sealed class FrameworkFileInfoFactory(IFileSystem fileSystem) : IFileInfoFactory
{
    /// <inheritdoc />
    public IFileSystem FileSystem { get; } = fileSystem;

    /// <inheritdoc />
    public IFileInfo New(string fileName) => new FrameworkFileInfo(FileSystem, fileName);

    /// <summary>
    /// Not ported: a <see cref="FileInfo"/> holds a path .NET Framework has already
    /// normalised, so a wrapper over one would read it otherwise than .NET 10.
    /// </summary>
    public IFileInfo? Wrap(FileInfo? fileInfo) =>
        throw new NotSupportedException("Not ported: make the file info with New.");
}

/// <summary>
/// A file's name, attributes and size as .NET 10's <c>FileInfo</c> reads them on Windows
/// (dotnet/runtime v10.0.0, <c>FileInfo.cs</c>, <c>FileSystemInfo.cs</c> and
/// <c>FileSystemInfo.Windows.cs</c>). It holds no <see cref="FileInfo"/>, whose constructor
/// on .NET Framework runs the path through .NET Framework's own normaliser.
/// </summary>
/// <remarks>
/// The path is made a full path when the object is made, so a null, empty or malformed path
/// throws there. The file is not read until a member asks for its attributes or its size,
/// and the answer is kept until <see cref="Refresh"/>. Nothing at the path is not an error
/// until then: <see cref="Attributes"/> reads every bit set, and <see cref="Length"/> throws
/// a <see cref="FileNotFoundException"/>. Members the app does not read are not ported and
/// throw <see cref="NotSupportedException"/>; BannedSymbols.txt refuses a call to any of
/// them, and to the access-control extensions that would read through one.
/// </remarks>
internal sealed class FrameworkFileInfo : IFileInfo
{
    private const int NotRead = -1;

    private readonly string _originalPath;
    private readonly string _fullPath;
    private string? _name;
    private Kernel32.WIN32_FILE_ATTRIBUTE_DATA _data;
    private int _dataInitialized = NotRead;

    internal FrameworkFileInfo(IFileSystem fileSystem, string fileName)
    {
        if (fileName is null) throw new ArgumentNullException(nameof(fileName));
        FileSystem = fileSystem;
        _originalPath = fileName;
        _fullPath = FullPaths.GetFullPath(fileName);
    }

    /// <inheritdoc />
    public IFileSystem FileSystem { get; }

    /// <inheritdoc />
    public string FullName => _fullPath;

    /// <inheritdoc />
    public string Name => _name ??= PathText.GetFileName(_originalPath);

    /// <summary>
    /// The full path's extension, from its last period, or empty where the last part of the
    /// path has none.
    /// </summary>
    public string Extension
    {
        get
        {
            var length = _fullPath.Length;
            for (var i = length; --i >= 0;)
            {
                var ch = _fullPath[i];
                if (ch == '.')
                    return _fullPath.Substring(i, length - i);
                if (PathText.IsDirectorySeparator(ch) || ch == Path.VolumeSeparatorChar)
                    break;
            }
            return string.Empty;
        }
    }

    /// <inheritdoc />
    public string? DirectoryName => PathText.GetDirectoryName(_fullPath);

    /// <summary>
    /// The attributes, read on the first ask. Nothing at the path reads as every bit set; any
    /// other refusal from Windows is the exception <see cref="Win32Marshal"/> gives for its
    /// error, naming the full path. Setting them sets them on the file, and the next ask
    /// reads them again.
    /// </summary>
    public FileAttributes Attributes
    {
        get
        {
            EnsureDataInitialized();
            return (FileAttributes)_data.dwFileAttributes;
        }
        set
        {
            FrameworkFileSystem.SetAttributes(_fullPath, value);
            _dataInitialized = NotRead;
        }
    }

    /// <summary>
    /// The size in bytes. A folder, or nothing at the path, is a
    /// <see cref="FileNotFoundException"/> naming the full path.
    /// </summary>
    public long Length
    {
        get
        {
            EnsureDataInitialized();
            return FrameworkFileSystem.LengthOf(_fullPath, _data);
        }
    }

    /// <summary>Reads the attributes and size again.</summary>
    public void Refresh() => RefreshCore();

    /// <summary>The path as it was given.</summary>
    public override string ToString() => _originalPath;

    // Not ported. Each is refused in shipped code by BannedSymbols.txt.
    public bool Exists => throw NotPorted();
    public IDirectoryInfo? Directory => throw NotPorted();
    public bool IsReadOnly { get => throw NotPorted(); set => throw NotPorted(); }
    public DateTime CreationTime { get => throw NotPorted(); set => throw NotPorted(); }
    public DateTime CreationTimeUtc { get => throw NotPorted(); set => throw NotPorted(); }
    public DateTime LastAccessTime { get => throw NotPorted(); set => throw NotPorted(); }
    public DateTime LastAccessTimeUtc { get => throw NotPorted(); set => throw NotPorted(); }
    public DateTime LastWriteTime { get => throw NotPorted(); set => throw NotPorted(); }
    public DateTime LastWriteTimeUtc { get => throw NotPorted(); set => throw NotPorted(); }
    public void Delete() => throw NotPorted();
    public StreamWriter AppendText() => throw NotPorted();
    public IFileInfo CopyTo(string destFileName) => throw NotPorted();
    public IFileInfo CopyTo(string destFileName, bool overwrite) => throw NotPorted();
    public FileSystemStream Create() => throw NotPorted();
    public StreamWriter CreateText() => throw NotPorted();
    public void Decrypt() => throw NotPorted();
    public void Encrypt() => throw NotPorted();
    public void MoveTo(string destFileName) => throw NotPorted();
    public FileSystemStream Open(FileMode mode) => throw NotPorted();
    public FileSystemStream Open(FileMode mode, FileAccess access) => throw NotPorted();
    public FileSystemStream Open(FileMode mode, FileAccess access, FileShare share) => throw NotPorted();
    public FileSystemStream OpenRead() => throw NotPorted();
    public StreamReader OpenText() => throw NotPorted();
    public FileSystemStream OpenWrite() => throw NotPorted();
    public IFileInfo Replace(string destinationFileName, string? destinationBackupFileName) => throw NotPorted();
    public IFileInfo Replace(string destinationFileName, string? destinationBackupFileName, bool ignoreMetadataErrors) => throw NotPorted();

    private void EnsureDataInitialized()
    {
        if (_dataInitialized == NotRead)
        {
            _data = default;
            RefreshCore();
        }

        if (_dataInitialized != 0)
            throw Win32Marshal.GetExceptionForWin32Error(_dataInitialized, _fullPath);
    }

    private void RefreshCore() =>
        _dataInitialized = FrameworkFileSystem.FillAttributeInfo(_fullPath, out _data, returnErrorOnNotFound: false);

    private static NotSupportedException NotPorted() =>
        new("Not ported: port .NET 10's member before calling it, as .NET Framework reads the file otherwise.");
}
