using System.IO.Abstractions;
using InstallerClean.Helpers;
using InstallerClean.Interop.Native;

namespace InstallerClean.Services;

/// <summary>
/// What <see cref="FrameworkFileInfo"/> and <see cref="FrameworkDirectoryInfo"/> share, as
/// .NET 10's <c>FileSystemInfo</c> holds it on Windows (dotnet/runtime v10.0.0,
/// <c>FileSystemInfo.cs</c> and <c>FileSystemInfo.Windows.cs</c>): the path as given and as a
/// full path, and the attributes, times and size, read on the first ask and kept until
/// <see cref="Refresh"/>.
/// </summary>
/// <remarks>
/// Nothing at the path is not an error until a member reads the data: the attributes then
/// read as every bit set. Any other refusal from Windows is the exception
/// <see cref="Win32Marshal"/> gives for its error, naming the full path.
/// </remarks>
internal abstract class FrameworkFileSystemInfo
{
    private const int NotRead = -1;

    private protected readonly string _originalPath;
    private protected readonly string _fullPath;
    private protected Kernel32.WIN32_FILE_ATTRIBUTE_DATA _data;
    private int _dataInitialized = NotRead;

    /// <summary>A file or folder not yet read, at <paramref name="fullPath"/>.</summary>
    private protected FrameworkFileSystemInfo(IFileSystem fileSystem, string originalPath, string fullPath)
    {
        FileSystem = fileSystem;
        _originalPath = originalPath;
        _fullPath = fullPath;
    }

    /// <summary>
    /// An entry a folder listing found, as .NET 10's <c>FileSystemInfo.Create</c> and
    /// <c>Init</c> make it: the full path is both the path given and the full path, and the
    /// data is the listing's, so nothing is read again until <see cref="Refresh"/>.
    /// </summary>
    private protected FrameworkFileSystemInfo(
        IFileSystem fileSystem,
        string fullPath,
        in Kernel32.WIN32_FILE_ATTRIBUTE_DATA data)
    {
        FileSystem = fileSystem;
        _originalPath = fullPath;
        _fullPath = fullPath;
        _data = data;
        _dataInitialized = 0;
    }

    /// <inheritdoc cref="IFileSystemEntity.FileSystem" />
    public IFileSystem FileSystem { get; }

    /// <summary>The full path.</summary>
    public string FullName => _fullPath;

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

    /// <summary>
    /// The attributes, read on the first ask. Setting them sets them on the file or folder,
    /// and the next ask reads them again.
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

    /// <summary>Reads the attributes, times and size again.</summary>
    public void Refresh() =>
        _dataInitialized = FrameworkFileSystem.FillAttributeInfo(_fullPath, out _data, returnErrorOnNotFound: false);

    /// <summary>The path as it was given.</summary>
    public override string ToString() => _originalPath;

    /// <summary>
    /// Reads the data where it has not been read, and throws where Windows refused it.
    /// </summary>
    private protected void EnsureDataInitialized()
    {
        if (_dataInitialized == NotRead)
        {
            _data = default;
            Refresh();
        }

        if (_dataInitialized != 0)
            throw Win32Marshal.GetExceptionForWin32Error(_dataInitialized, _fullPath);
    }
}
