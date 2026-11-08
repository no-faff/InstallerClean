using System.IO.Abstractions;
using InstallerClean.Helpers;
using InstallerClean.Interop.Native;

namespace InstallerClean.Services;

/// <summary>
/// The app's <see cref="IDirectoryInfoFactory"/>: each <see cref="IDirectoryInfo"/> it makes is
/// a <see cref="FrameworkDirectoryInfo"/>, which lists the folder as .NET 10's DirectoryInfo
/// does.
/// </summary>
internal sealed class FrameworkDirectoryInfoFactory(IFileSystem fileSystem) : IDirectoryInfoFactory
{
    /// <inheritdoc />
    public IFileSystem FileSystem { get; } = fileSystem;

    /// <inheritdoc />
    public IDirectoryInfo New(string path) => new FrameworkDirectoryInfo(FileSystem, path);

    /// <summary>
    /// Not ported: a <see cref="DirectoryInfo"/> holds a path .NET Framework has already
    /// normalised, so a wrapper over one would read it otherwise than .NET 10.
    /// </summary>
    public IDirectoryInfo? Wrap(DirectoryInfo? directoryInfo) =>
        throw new NotSupportedException("Not ported: make the folder info with New.");
}

/// <summary>
/// A folder, and the files directly in it, as .NET 10's <c>DirectoryInfo</c> reads and lists
/// them on Windows (dotnet/runtime v10.0.0, <c>DirectoryInfo.cs</c>, <c>FileSystemInfo.cs</c>,
/// <c>FileSystemInfo.Windows.cs</c> and <c>FileSystemEnumerableFactory.cs</c>). It holds no
/// <see cref="DirectoryInfo"/>, whose constructor on .NET Framework runs the path through
/// .NET Framework's own normaliser.
/// </summary>
/// <remarks>
/// The path is made a full path when the object is made, so a null, empty or malformed path
/// throws there. Each file a listing finds is a <see cref="FrameworkFileInfo"/> holding the
/// attributes and size the listing read, as .NET 10's are. A listing is of this folder only,
/// and one asked for with <see cref="SearchOption.AllDirectories"/> is a
/// <see cref="NotSupportedException"/>. The other members the app does not read are not
/// ported and throw <see cref="NotSupportedException"/>; BannedSymbols.txt refuses a call to
/// any of them, and to the access-control extensions that would read through one.
/// </remarks>
internal sealed class FrameworkDirectoryInfo : FrameworkFileSystemInfo, IDirectoryInfo
{
    private string? _name;

    internal FrameworkDirectoryInfo(IFileSystem fileSystem, string path)
        : base(fileSystem, path ?? throw new ArgumentNullException(nameof(path)), FullPaths.GetFullPath(path))
    {
    }

    /// <summary>
    /// The folder's own name: the whole full path for a root, or else its last part, with one
    /// separator at the end left off.
    /// </summary>
    public string Name => _name ??=
        PathText.GetRootLength(_fullPath) == _fullPath.Length
            ? _fullPath
            : PathText.GetFileName(PathText.TrimEndingDirectorySeparator(_fullPath))!;

    /// <summary>The files directly in the folder, as .NET 10 lists them.</summary>
    public IEnumerable<IFileInfo> EnumerateFiles() => EnumerateFiles("*");

    /// <summary>
    /// The files directly in the folder whose names match <paramref name="searchPattern"/>,
    /// matched against each file's own name only, as .NET 10 lists them.
    /// </summary>
    public IEnumerable<IFileInfo> EnumerateFiles(string searchPattern) => EnumerateTopFiles(searchPattern);

    /// <summary>
    /// As <see cref="EnumerateFiles(string)"/>. A value of <paramref name="searchOption"/> that is
    /// neither member is an <see cref="ArgumentOutOfRangeException"/>, as on .NET 10, and
    /// <see cref="SearchOption.AllDirectories"/> is a <see cref="NotSupportedException"/>.
    /// </summary>
    public IEnumerable<IFileInfo> EnumerateFiles(string searchPattern, SearchOption searchOption)
    {
        if (searchOption != SearchOption.TopDirectoryOnly && searchOption != SearchOption.AllDirectories)
            throw new ArgumentOutOfRangeException(nameof(searchOption), "Enum value was out of legal range.");
        if (searchOption == SearchOption.AllDirectories)
            throw new NotSupportedException("Not ported: the listing reads one folder, not the folders inside it.");

        return EnumerateTopFiles(searchPattern);
    }

    // The listing as .NET 10's InternalEnumerateInfos and FileSystemEnumerableFactory.FileInfos
    // make it, with EnumerationOptions.Compatible's Win32 matching, ignoring case.
    private IEnumerable<IFileInfo> EnumerateTopFiles(string searchPattern)
    {
        if (searchPattern is null) throw new ArgumentNullException(nameof(searchPattern));

        var path = _fullPath;
        var isNormalized = FrameworkDirectory.NormalizeInputs(ref path, ref searchPattern);
        var expression = searchPattern;
        var fileSystem = FileSystem;

        return new FrameworkFileSystemEnumerable<IFileInfo>(
            path,
            entry => new FrameworkFileInfo(fileSystem, entry.ToFullPath(), entry.FileName, entry.Data),
            entry => !entry.IsDirectory
                && FrameworkFileSystemName.MatchesWin32Expression(expression.AsSpan(), entry.FileName.AsSpan(), ignoreCase: true),
            isNormalized);
    }

    // Not ported. Each is refused in shipped code by BannedSymbols.txt.
    public bool Exists => throw NotPorted();
    public IDirectoryInfo? Parent => throw NotPorted();
    public IDirectoryInfo Root => throw NotPorted();
    public DateTime CreationTime { get => throw NotPorted(); set => throw NotPorted(); }
    public DateTime CreationTimeUtc { get => throw NotPorted(); set => throw NotPorted(); }
    public DateTime LastAccessTime { get => throw NotPorted(); set => throw NotPorted(); }
    public DateTime LastAccessTimeUtc { get => throw NotPorted(); set => throw NotPorted(); }
    public DateTime LastWriteTime { get => throw NotPorted(); set => throw NotPorted(); }
    public DateTime LastWriteTimeUtc { get => throw NotPorted(); set => throw NotPorted(); }
    public void Create() => throw NotPorted();
    public IDirectoryInfo CreateSubdirectory(string path) => throw NotPorted();
    public void Delete() => throw NotPorted();
    public void Delete(bool recursive) => throw NotPorted();
    public IEnumerable<IDirectoryInfo> EnumerateDirectories() => throw NotPorted();
    public IEnumerable<IDirectoryInfo> EnumerateDirectories(string searchPattern) => throw NotPorted();
    public IEnumerable<IDirectoryInfo> EnumerateDirectories(string searchPattern, SearchOption searchOption) => throw NotPorted();
    public IEnumerable<IFileSystemInfo> EnumerateFileSystemInfos() => throw NotPorted();
    public IEnumerable<IFileSystemInfo> EnumerateFileSystemInfos(string searchPattern) => throw NotPorted();
    public IEnumerable<IFileSystemInfo> EnumerateFileSystemInfos(string searchPattern, SearchOption searchOption) => throw NotPorted();
    public IDirectoryInfo[] GetDirectories() => throw NotPorted();
    public IDirectoryInfo[] GetDirectories(string searchPattern) => throw NotPorted();
    public IDirectoryInfo[] GetDirectories(string searchPattern, SearchOption searchOption) => throw NotPorted();
    public IFileInfo[] GetFiles() => throw NotPorted();
    public IFileInfo[] GetFiles(string searchPattern) => throw NotPorted();
    public IFileInfo[] GetFiles(string searchPattern, SearchOption searchOption) => throw NotPorted();
    public IFileSystemInfo[] GetFileSystemInfos() => throw NotPorted();
    public IFileSystemInfo[] GetFileSystemInfos(string searchPattern) => throw NotPorted();
    public IFileSystemInfo[] GetFileSystemInfos(string searchPattern, SearchOption searchOption) => throw NotPorted();
    public void MoveTo(string destDirName) => throw NotPorted();

    private static NotSupportedException NotPorted() =>
        new("Not ported: port .NET 10's member before calling it, as .NET Framework reads the folder otherwise.");
}
