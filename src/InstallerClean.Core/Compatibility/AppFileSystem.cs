using System.IO.Abstractions;

namespace InstallerClean.Services;

// BannedSymbols.txt refuses System.IO.Abstractions.FileSystem and the FileWrapper and
// DirectoryWrapper constructors everywhere else, so every IFileSystem, IFile and IDirectory
// the shipped code makes is one of these.
// The base lists and the calls to the base constructors are the uses that remain.
#pragma warning disable RS0030

/// <summary>
/// The app's <see cref="IFileSystem"/>: System.IO.Abstractions' own, except that its
/// <see cref="File"/> and <see cref="Directory"/> answer whether a file or a folder is there,
/// read and set a file's attributes, move and delete a file, and make and remove a folder as
/// .NET 10 does
/// (<see cref="FrameworkFileWrapper"/>, <see cref="FrameworkDirectoryWrapper"/>), and its
/// <see cref="FileInfo"/> reads a file's attributes and size as .NET 10's FileInfo does
/// (<see cref="FrameworkFileInfoFactory"/>), and its <see cref="DirectoryInfo"/> lists a
/// folder's files as .NET 10's DirectoryInfo does (<see cref="FrameworkDirectoryInfoFactory"/>).
/// </summary>
internal sealed class AppFileSystem : FileSystem
{
    public AppFileSystem()
    {
        File = new FrameworkFileWrapper(this);
        Directory = new FrameworkDirectoryWrapper(this);
        FileInfo = new FrameworkFileInfoFactory(this);
        DirectoryInfo = new FrameworkDirectoryInfoFactory(this);
    }

    /// <inheritdoc />
    public override IFile File { get; }

    /// <inheritdoc />
    public override IDirectory Directory { get; }

    /// <inheritdoc />
    public override IFileInfoFactory FileInfo { get; }

    /// <inheritdoc />
    public override IDirectoryInfoFactory DirectoryInfo { get; }
}

/// <summary>
/// System.IO.Abstractions' <see cref="FileWrapper"/>, whose every member calls
/// <c>System.IO.File</c>, except these, which do as .NET 10's own: <see cref="Exists"/>,
/// <see cref="GetAttributes(string)"/>, <see cref="SetAttributes"/> and <see cref="Delete"/>
/// (<see cref="FrameworkFile"/>), and
/// <see cref="Move(string, string)"/>, MoveFileExW with MOVEFILE_COPY_ALLOWED and
/// <c>\\?\</c> in front of a path of 260 characters or more.
/// </summary>
internal sealed class FrameworkFileWrapper(IFileSystem fileSystem) : FileWrapper(fileSystem)
{
    /// <inheritdoc />
    public override bool Exists(string? path) => FrameworkFile.Exists(path);

    /// <inheritdoc />
    public override FileAttributes GetAttributes(string path) => FrameworkFile.GetAttributes(path);

    /// <inheritdoc />
    public override void SetAttributes(string path, FileAttributes fileAttributes) =>
        FrameworkFile.SetAttributes(path, fileAttributes);

    /// <inheritdoc />
    public override void Delete(string path) => FrameworkFile.Delete(path);

    /// <inheritdoc />
    public override void Move(string sourceFileName, string destFileName) =>
        System.IO.File.Move(sourceFileName, destFileName, overwrite: false);
}

/// <summary>
/// System.IO.Abstractions' <see cref="DirectoryWrapper"/>, whose every member calls
/// <c>System.IO.Directory</c>, except these, which do as .NET 10's own
/// (<see cref="FrameworkDirectory"/>): <see cref="Exists"/>,
/// <see cref="CreateDirectory(string)"/> and <see cref="Delete(string)"/>.
/// </summary>
internal sealed class FrameworkDirectoryWrapper(IFileSystem fileSystem) : DirectoryWrapper(fileSystem)
{
    /// <inheritdoc />
    public override bool Exists(string? path) => FrameworkDirectory.Exists(path);

    /// <summary>
    /// Creates the folder and every folder above it that is not there, and returns the
    /// folder's <see cref="FrameworkDirectoryInfo"/>, as .NET 10's
    /// <c>Directory.CreateDirectory</c> returns its DirectoryInfo.
    /// </summary>
    public override IDirectoryInfo CreateDirectory(string path)
    {
        FrameworkDirectory.CreateDirectory(path);
        return new FrameworkDirectoryInfo(FileSystem, path);
    }

    /// <summary>Removes the empty folder, as .NET 10's <c>Directory.Delete(string)</c> does.</summary>
    public override void Delete(string path) => FrameworkDirectory.Delete(path);
}

#pragma warning restore RS0030
