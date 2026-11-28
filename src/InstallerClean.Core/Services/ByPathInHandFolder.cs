using System.IO.Abstractions;
using InstallerClean.Helpers;
using InstallerClean.Interop.Native;

namespace InstallerClean.Services;

/// <summary>
/// Makes each act by path through an injected <see cref="IFileSystem"/>, for the test
/// seam where that filesystem is a substitute, so a test of a Move or a Delete follows a
/// file out of its place, back, onward or away. The app never uses it: the production
/// path takes <see cref="HandleInHandFolder"/>, which a wiring test holds.
///
/// IT SHOWS THE ORDER OF THE STEPS AND NOTHING ABOUT WHAT WINDOWS REFUSES. A substitute
/// has no handles and no sharing, so a file held here can be opened by anything. What an
/// open that shares nothing keeps out is shown by the tests of the app's own, on Windows.
/// </summary>
internal sealed class ByPathInHandFolder(IFileSystem fileSystem) : IInHandFolder
{
    private readonly IFileSystem _fs = fileSystem;

    public IInHandBatch? OpenBatch(string installerFolder, out int failureCode)
    {
        failureCode = 0;
        var folder = PathText.Combine(
            PathText.Combine(installerFolder, InHandFolders.FolderName), Guid.NewGuid().ToString("N"));
        try
        {
            _fs.Directory.CreateDirectory(folder);
            return new Batch(_fs, folder);
        }
        catch (Exception ex)
        {
            failureCode = ByPathDestinationGuard.Win32CodeOf(ex);
            return null;
        }
    }

    // The listings below are allowed here as the by-path destination guard's own reads
    // are: they read only through the filesystem a test substitutes.
#pragma warning disable RS0030
    public ILeftInHand? OpenLeft(string installerFolder)
    {
        var inHand = PathText.Combine(installerFolder, InHandFolders.FolderName);
        try
        {
            if (!_fs.Directory.Exists(inHand)) return null;

            var batches = new List<LeftBatch>();
            foreach (var folder in _fs.Directory.GetDirectories(inHand))
            {
                var name = PathText.GetFileName(folder);
                var files = new List<LeftFile>();
                foreach (var file in _fs.Directory.GetFiles(folder))
                    files.Add(new LeftFile(name, PathText.GetFileName(file), _fs.FileInfo.New(file).Length));

                // A substitute keeps no short names.
                batches.Add(new LeftBatch(name, null, files));
            }
            return new Left(_fs, installerFolder, inHand, batches);
        }
        catch
        {
            return null;
        }
    }

    private sealed class Left(IFileSystem fs, string installerFolder, string inHand, IReadOnlyList<LeftBatch> batches)
        : ILeftInHand
    {
        public IReadOnlyList<LeftBatch> Batches { get; } = batches;

        public FileHold Hold(LeftFile file)
        {
            var folder = PathText.Combine(inHand, file.Batch);
            var path = PathText.Combine(folder, file.Name);
            try
            {
                if (!fs.File.Exists(path))
                    return new FileHold(fs.Directory.Exists(path) ? HoldOutcome.NotAPlainFile : HoldOutcome.Gone);
                if (fs.File.GetAttributes(path).HasFlag(FileAttributes.ReparsePoint))
                    return new FileHold(HoldOutcome.NotAPlainFile);
                return new FileHold(
                    HoldOutcome.Held,
                    new HeldFile(fs, PathText.Combine(installerFolder, file.Name), folder, lyingAt: path));
            }
            catch (Exception ex)
            {
                var error = ByPathDestinationGuard.Win32CodeOf(ex);
                return new FileHold(HandleInHandFolder.OutcomeOf(error), WindowsError: error);
            }
        }

        public void Dispose()
        {
            // Each folder goes only where it is empty, as the app's own does.
            try
            {
                foreach (var batch in Batches)
                {
                    var folder = PathText.Combine(inHand, batch.Name);
                    if (fs.Directory.Exists(folder) && !fs.Directory.EnumerateFileSystemEntries(folder).Any())
                        fs.Directory.Delete(folder);
                }
                if (fs.Directory.Exists(inHand) && !fs.Directory.EnumerateFileSystemEntries(inHand).Any())
                    fs.Directory.Delete(inHand);
            }
            catch
            {
            }
        }
    }
#pragma warning restore RS0030

    private sealed class Batch(IFileSystem fs, string folder) : IInHandBatch
    {
        public string Folder { get; } = folder;

        public FileHold Hold(string path)
        {
            try
            {
                if (!fs.File.Exists(path))
                    return new FileHold(fs.Directory.Exists(path) ? HoldOutcome.NotAPlainFile : HoldOutcome.Gone);
                var attributes = fs.File.GetAttributes(path);
                if (attributes.HasFlag(FileAttributes.ReparsePoint)) return new FileHold(HoldOutcome.NotAPlainFile);
                return new FileHold(HoldOutcome.Held, new HeldFile(fs, path, Folder));
            }
            catch (Exception ex)
            {
                var error = ByPathDestinationGuard.Win32CodeOf(ex);
                return new FileHold(HandleInHandFolder.OutcomeOf(error), WindowsError: error);
            }
        }

        public CopySourceOpen OpenToCopy(string path)
        {
            try
            {
                if (!fs.File.Exists(path))
                    return new CopySourceOpen(fs.Directory.Exists(path) ? HoldOutcome.NotAPlainFile : HoldOutcome.Gone);
                var attributes = fs.File.GetAttributes(path);
                if (attributes.HasFlag(FileAttributes.ReparsePoint)) return new CopySourceOpen(HoldOutcome.NotAPlainFile);

                // Allowed here as the by-path destination guard's own reads are: this
                // reads only through the filesystem a test substitutes.
#pragma warning disable RS0030
                var content = fs.File.Open(path, FileMode.Open, FileAccess.Read, FileShare.Read);
#pragma warning restore RS0030
                return new CopySourceOpen(
                    HoldOutcome.Held, new CopySource(content, StampOf(fs, path, content.Length), attributes));
            }
            catch (Exception ex)
            {
                var error = ByPathDestinationGuard.Win32CodeOf(ex);
                return new CopySourceOpen(HandleInHandFolder.OutcomeOf(error), WindowsError: error);
            }
        }

        /// <summary>The same drive or share answers that a rename reaches it, as a volume does for the app's own.</summary>
        public RenameReach ReachOf(DestinationFolder destination, out int failureCode)
        {
            failureCode = 0;
            return string.Equals(
                PathText.GetPathRoot(Folder), PathText.GetPathRoot(destination.Path), StringComparison.OrdinalIgnoreCase)
                ? RenameReach.SameVolume
                : RenameReach.AnotherVolume;
        }

        public void Dispose()
        {
            // Each folder goes only where it is empty, as the app's own does.
            foreach (var each in new[] { Folder, PathText.GetDirectoryName(Folder) })
            {
                try
                {
                    if (each is not null && fs.Directory.Exists(each)) fs.Directory.Delete(each);
                }
                catch
                {
                    return;
                }
            }
        }
    }

    /// <summary>
    /// A stamp from what a path gives: the length and the last-write time. A substitute
    /// keeps no file IDs, so every file's identity here is the same empty one.
    /// </summary>
#pragma warning disable RS0030
    private static FileStamp StampOf(IFileSystem fs, string path, long length) =>
        new(default, length, fs.File.GetLastWriteTimeUtc(path).ToFileTimeUtc());
#pragma warning restore RS0030

    private sealed class CopySource(Stream content, FileStamp stamp, FileAttributes attributes) : ICopySource
    {
        public Stream Content { get; } = content;
        public FileStamp Stamp { get; } = stamp;
        public FileAttributes Attributes { get; } = attributes;

        public void Dispose() => Content.Dispose();
    }

    /// <param name="lyingAt">
    /// For a file an earlier batch left: where it lies, in that batch's folder. Null for
    /// a file held in its place.
    /// </param>
    private sealed class HeldFile(IFileSystem fs, string recordedPath, string batchFolder, string? lyingAt = null)
        : IHeldFile
    {
        private string _current = lyingAt ?? recordedPath;

        public string RecordedPath { get; } = recordedPath;

        public bool IsOut { get; private set; } = lyingAt is not null;

        public bool TryReadStamp(out FileStamp stamp)
        {
            stamp = default;
            try
            {
                stamp = StampOf(fs, _current, fs.FileInfo.New(_current).Length);
                return true;
            }
            catch
            {
                return false;
            }
        }

        public bool TakeOut(out int failureCode)
        {
            if (!Move(PathText.Combine(batchFolder, PathText.GetFileName(RecordedPath)), out failureCode)) return false;
            IsOut = true;
            return true;
        }

        public bool PutBack(out int failureCode)
        {
            if (!Move(RecordedPath, out failureCode)) return false;
            IsOut = false;
            return true;
        }

        public bool MoveInto(DestinationFolder destination, string name, out int failureCode)
        {
            if (!Move(PathText.Combine(destination.Path, name), out failureCode)) return false;
            IsOut = false;
            return true;
        }

        public bool Delete(out int failureCode)
        {
            failureCode = 0;
            FileAttributes? cleared = null;
            try
            {
                var attributes = fs.File.GetAttributes(_current);
                if (attributes.HasFlag(FileAttributes.ReadOnly))
                {
                    fs.File.SetAttributes(_current, attributes & ~FileAttributes.ReadOnly);
                    cleared = attributes;
                }
                fs.File.Delete(_current);
                return true;
            }
            catch (Exception ex)
            {
                failureCode = ByPathDestinationGuard.Win32CodeOf(ex);
                if (cleared is { } original)
                {
                    try { fs.File.SetAttributes(_current, original); } catch { }
                }
                return false;
            }
        }

        // A name already taken refuses the rename, as it does for the app's own.
        private bool Move(string to, out int failureCode)
        {
            failureCode = 0;
            try
            {
                if (fs.File.Exists(to) || fs.Directory.Exists(to))
                {
                    failureCode = Kernel32.ERROR_ALREADY_EXISTS;
                    return false;
                }
                fs.File.Move(_current, to);
                _current = to;
                return true;
            }
            catch (Exception ex)
            {
                failureCode = ByPathDestinationGuard.Win32CodeOf(ex);
                return false;
            }
        }

        public void Dispose() { }
    }
}
