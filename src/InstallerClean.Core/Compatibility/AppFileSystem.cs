using System.IO.Abstractions;

namespace InstallerClean.Services;

// BannedSymbols.txt refuses System.IO.Abstractions.FileSystem and FileWrapper's constructor
// everywhere else, so every IFileSystem and IFile the shipped code makes is one of these two.
// The two base lists and the calls to the base constructors are the uses that remain.
#pragma warning disable RS0030

/// <summary>
/// The app's <see cref="IFileSystem"/>: System.IO.Abstractions' own, except that its
/// <see cref="File"/> moves a file as .NET 10 does (<see cref="FrameworkFileWrapper"/>).
/// </summary>
internal sealed class AppFileSystem : FileSystem
{
    public AppFileSystem()
    {
        File = new FrameworkFileWrapper(this);
    }

    /// <inheritdoc />
    public override IFile File { get; }
}

/// <summary>
/// System.IO.Abstractions' <see cref="FileWrapper"/>, whose every member calls
/// <c>System.IO.File</c>, except <see cref="Move(string, string)"/>, which moves the file as
/// .NET 10's <c>File.Move</c> does: MoveFileExW with MOVEFILE_COPY_ALLOWED, and
/// <c>\\?\</c> in front of a path of 260 characters or more.
/// </summary>
internal sealed class FrameworkFileWrapper(IFileSystem fileSystem) : FileWrapper(fileSystem)
{
    /// <inheritdoc />
    public override void Move(string sourceFileName, string destFileName) =>
        System.IO.File.Move(sourceFileName, destFileName, overwrite: false);
}

#pragma warning restore RS0030
