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
/// throws there. One made by a folder listing holds what the listing read. Any other file is
/// not read until a member asks for its attributes or its size, and the answer is kept until
/// <see cref="FrameworkFileSystemInfo.Refresh"/>. Nothing at the path is not an error until
/// then: <see cref="FrameworkFileSystemInfo.Attributes"/> reads every bit set, and
/// <see cref="Length"/> throws
/// a <see cref="FileNotFoundException"/>. Members the app does not read are not ported and
/// throw <see cref="NotSupportedException"/>; BannedSymbols.txt refuses a call to any of
/// them, and to the access-control extensions that would read through one.
/// </remarks>
internal sealed class FrameworkFileInfo : FrameworkFileSystemInfo, IFileInfo
{
    private string? _name;

    internal FrameworkFileInfo(IFileSystem fileSystem, string fileName)
        : base(fileSystem, fileName ?? throw new ArgumentNullException(nameof(fileName)), FullPaths.GetFullPath(fileName))
    {
    }

    /// <summary>
    /// A file a folder listing found, as .NET 10's <c>FileSystemInfo.Create</c> makes it: the
    /// name is the listing's, and the attributes, times and size are the listing's.
    /// </summary>
    internal FrameworkFileInfo(
        IFileSystem fileSystem,
        string fullPath,
        string fileName,
        in Kernel32.WIN32_FILE_ATTRIBUTE_DATA data)
        : base(fileSystem, fullPath, data)
    {
        _name = fileName;
    }

    /// <inheritdoc />
    public string Name => _name ??= PathText.GetFileName(_originalPath);

    /// <inheritdoc />
    public string? DirectoryName => PathText.GetDirectoryName(_fullPath);

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

    private static NotSupportedException NotPorted() =>
        new("Not ported: port .NET 10's member before calling it, as .NET Framework reads the file otherwise.");
}
