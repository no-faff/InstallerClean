using System.Runtime.InteropServices;
using InstallerClean.Helpers;
using InstallerClean.Interop.Native;
using Microsoft.Win32.SafeHandles;

namespace InstallerClean.Services;

/// <summary>
/// The app's <see cref="IInHandFolder"/>: every act on a file is made through a handle
/// opened on that file, so what is renamed, put back or deleted is the file that was
/// opened and checked, whatever has happened to its name since.
/// </summary>
internal sealed class HandleInHandFolder : IInHandFolder
{
    private const int STATUS_OBJECT_NAME_COLLISION = unchecked((int)0xC0000035);

    // The answers Windows gives while a folder is being removed, or once it has gone.
    private const int STATUS_OBJECT_NAME_NOT_FOUND = unchecked((int)0xC0000034);
    private const int STATUS_OBJECT_PATH_NOT_FOUND = unchecked((int)0xC000003A);
    private const int STATUS_DELETE_PENDING = unchecked((int)0xC0000056);
    private const int STATUS_FILE_DELETED = unchecked((int)0xC0000123);

    /// <summary>
    /// How many times <see cref="OpenBatch"/> makes the in-hand folder and the batch's
    /// folder inside it, and how long it waits between two tries.
    ///
    /// A SCAN IN ANOTHER PROCESS REMOVES THE IN-HAND FOLDER WHERE IT FINDS IT EMPTY
    /// (<see cref="OpenLeft"/>), and can find it so between this making it and this
    /// making the batch's folder inside it. The batch's folder then fails to be made, for
    /// no reason of the batch's own, and both are made again.
    /// </summary>
    internal const int OpenBatchTries = 3;
    private static readonly TimeSpan BetweenOpenBatchTries = Durations.FromMilliseconds(15);

    /// <summary>
    /// Null in the app. A test sets it to act between the opening of the in-hand folder and
    /// the making of the batch's folder inside it, as a scan in another process can.
    /// </summary>
    internal Action? BeforeTheBatchsFolderForTests { get; set; }

    public IInHandBatch? OpenBatch(string installerFolder, out int failureCode)
    {
        var installer = DirectoryGuard.OpenDirectoryFollow(installerFolder, out failureCode);
        if (installer is null) return null;

        try
        {
            for (var attempt = 1; ; attempt++)
            {
                var batch = TryOpenBatch(
                    installer, installerFolder, BeforeTheBatchsFolderForTests, out failureCode, out var removedMeanwhile);
                if (batch is not null) return batch;
                if (!removedMeanwhile || attempt == OpenBatchTries)
                {
                    installer.Dispose();
                    return null;
                }
                Thread.Sleep(BetweenOpenBatchTries);
            }
        }
        catch
        {
            installer.Dispose();
            throw;
        }
    }

    /// <summary>
    /// One try at <see cref="OpenBatch"/>'s two folders. <paramref name="removedMeanwhile"/>
    /// is true where it failed because the in-hand folder went, or was going, under it.
    /// </summary>
    private static Batch? TryOpenBatch(
        SafeFileHandle installer, string installerFolder, Action? beforeTheBatchsFolder,
        out int failureCode, out bool removedMeanwhile)
    {
        removedMeanwhile = false;

        // The folder that holds every batch's own is made where it is not there, and
        // opened as itself either way: a link or a file under its name opens as
        // nothing, and the batch is refused.
        using (var made = DirectoryGuard.CreateChildFolderHeld(
                   installer, InHandFolders.FolderName, IntPtr.Zero, out var status))
        {
            if (made is null && status != STATUS_OBJECT_NAME_COLLISION)
            {
                removedMeanwhile = IsBeingRemoved(status);
                failureCode = (int)Ntdll.RtlNtStatusToDosError(status);
                return null;
            }
        }

        var inHand = DirectoryGuard.OpenChildFolderNoFollow(
            installer, InHandFolders.FolderName, Ntdll.FILE_GENERIC_READ, out var openStatus);
        if (inHand is null)
        {
            removedMeanwhile = IsBeingRemoved(openStatus);
            failureCode = Kernel32.ERROR_DIRECTORY;
            return null;
        }

        try
        {
            beforeTheBatchsFolder?.Invoke();

            var name = Guid.NewGuid().ToString("N");
            var batch = DirectoryGuard.CreateChildFolderHeld(inHand, name, IntPtr.Zero, out var batchStatus);
            if (batch is null)
            {
                removedMeanwhile = IsBeingRemoved(batchStatus);
                failureCode = (int)Ntdll.RtlNtStatusToDosError(batchStatus);
                inHand.Dispose();
                DirectoryGuard.TryRemoveEmptyChildFolder(installer, InHandFolders.FolderName);
                return null;
            }

            failureCode = 0;
            return new Batch(
                installer, inHand, batch, name,
                PathText.Combine(PathText.Combine(installerFolder, InHandFolders.FolderName), name));
        }
        catch
        {
            inHand.Dispose();
            throw;
        }
    }

    private static bool IsBeingRemoved(int status) =>
        status is STATUS_OBJECT_NAME_NOT_FOUND or STATUS_OBJECT_PATH_NOT_FOUND
            or STATUS_DELETE_PENDING or STATUS_FILE_DELETED;

    public ILeftInHand? OpenLeft(string installerFolder)
    {
        var installer = DirectoryGuard.OpenDirectoryFollow(installerFolder, out _);
        if (installer is null) return null;

        SafeFileHandle? inHand = null;
        try
        {
            inHand = DirectoryGuard.OpenChildFolderNoFollow(installer, InHandFolders.FolderName);
            var entries = inHand is null ? null : DirectoryGuard.ListChildren(inHand);
            if (entries is null)
            {
                inHand?.Dispose();
                installer.Dispose();
                return null;
            }

            var inHandPath = PathText.Combine(installerFolder, InHandFolders.FolderName);
            var folders = new List<string>();
            var batches = new List<LeftBatch>();
            foreach (var entry in entries)
            {
                if (!entry.IsFolder) continue;
                folders.Add(entry.Name);

                // A folder that has gone since the listing, or will not open or list, is
                // left out: a batch that has just ended removes its own.
                using var folder = DirectoryGuard.OpenChildFolderNoFollow(inHand!, entry.Name);
                var inside = folder is null ? null : DirectoryGuard.ListChildren(folder);
                if (inside is null) continue;

                var files = new List<LeftFile>();
                foreach (var file in inside)
                    if (!file.IsFolder) files.Add(new LeftFile(entry.Name, file.Name, file.Length));

                var names = StorageHelpers.GetNamesInFolder(PathText.Combine(inHandPath, entry.Name));
                batches.Add(new LeftBatch(entry.Name, names is { Count: > 1 } ? names[1] : null, files));
            }

            return new Left(installer, inHand!, installerFolder, folders, batches);
        }
        catch
        {
            inHand?.Dispose();
            installer.Dispose();
            throw;
        }
    }

    private sealed class Left : ILeftInHand
    {
        private readonly SafeFileHandle _installer;
        private readonly SafeFileHandle _inHand;
        private readonly string _installerFolder;
        private readonly IReadOnlyList<string> _folders;
        private bool _disposed;

        public Left(
            SafeFileHandle installer, SafeFileHandle inHand, string installerFolder,
            IReadOnlyList<string> folders, IReadOnlyList<LeftBatch> batches)
        {
            _installer = installer;
            _inHand = inHand;
            _installerFolder = installerFolder;
            _folders = folders;
            Batches = batches;
        }

        public IReadOnlyList<LeftBatch> Batches { get; }

        public FileHold Hold(LeftFile file)
        {
            using var folder = DirectoryGuard.OpenChildFolderNoFollow(_inHand, file.Batch);
            if (folder is null) return new FileHold(HoldOutcome.Gone);

            // Opened relative to the batch's folder, itself opened relative to the in-hand
            // folder, so the file held is the one that was listed, whatever any path names
            // by now. Renaming needs the right to delete and nothing more.
            var handle = DirectoryGuard.OpenChildFileAlone(folder, file.Name, HoldAccess, out var status);
            if (handle is null)
            {
                var error = (int)Ntdll.RtlNtStatusToDosError(status);
                return new FileHold(OutcomeOf(error), WindowsError: error);
            }

            if (!Kernel32.GetFileInformationByHandle(handle, out var info))
            {
                var error = Marshal.GetLastWin32Error();
                handle.Dispose();
                return new FileHold(HoldOutcome.Refused, WindowsError: error);
            }

            if ((info.dwFileAttributes & (Kernel32.FILE_ATTRIBUTE_REPARSE_POINT | Kernel32.FILE_ATTRIBUTE_DIRECTORY)) != 0)
            {
                handle.Dispose();
                return new FileHold(HoldOutcome.NotAPlainFile);
            }

            var batchFolder = PathText.Combine(PathText.Combine(_installerFolder, InHandFolders.FolderName), file.Batch);
            return new FileHold(
                HoldOutcome.Held,
                new HeldFile(
                    handle, PathText.Combine(_installerFolder, file.Name), batchFolder, info.dwFileAttributes,
                    home: _installer));
        }

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;

            // Each batch's folder goes where it is empty and nothing has it open, and then
            // the folder above them on the same terms, as a batch that ends removes its own.
            foreach (var name in _folders) DirectoryGuard.TryRemoveEmptyChildFolder(_inHand, name);
            _inHand.Dispose();
            DirectoryGuard.TryRemoveEmptyChildFolder(_installer, InHandFolders.FolderName);
            _installer.Dispose();
        }
    }

    /// <summary>
    /// What the take asks for on a file: the right to delete it, which a rename through
    /// the handle needs too, and to read its attributes. The right to write attributes is
    /// added for a read-only file alone, whose attribute is cleared before it is deleted.
    /// </summary>
    internal const uint HoldAccess = Ntdll.DELETE | Kernel32.FILE_READ_ATTRIBUTES | Ntdll.SYNCHRONIZE;

    private sealed class Batch : IInHandBatch
    {
        private readonly SafeFileHandle _installer;
        private readonly SafeFileHandle _inHand;
        private readonly SafeFileHandle _batch;
        private readonly string _name;
        private bool _disposed;

        public Batch(SafeFileHandle installer, SafeFileHandle inHand, SafeFileHandle batch, string name, string folder)
        {
            _installer = installer;
            _inHand = inHand;
            _batch = batch;
            _name = name;
            Folder = folder;
        }

        public string Folder { get; }

        public FileHold Hold(string path)
        {
            // The attributes by path first, for the one thing they decide: whether the
            // open asks for the right to write them. What is acted on is what the handle
            // reads below.
            if (!Kernel32.GetFileAttributesEx(
                    FullPaths.ForWindowsCall(path), Kernel32.GetFileExInfoStandard, out var byPath))
                return Failed(Marshal.GetLastWin32Error());

            // A folder is answered from here: the open below is one Windows refuses a
            // folder outright, with an error that says nothing of what was at the path.
            if ((byPath.dwFileAttributes & Kernel32.FILE_ATTRIBUTE_DIRECTORY) != 0)
                return new FileHold(HoldOutcome.NotAPlainFile);

            var access = HoldAccess;
            if ((byPath.dwFileAttributes & Kernel32.FILE_ATTRIBUTE_READONLY) != 0)
                access |= Kernel32.FILE_WRITE_ATTRIBUTES;

            var handle = Kernel32.CreateFile(
                FullPaths.ForWindowsCall(path), access, 0, IntPtr.Zero,
                Kernel32.OPEN_EXISTING, Kernel32.FILE_FLAG_OPEN_REPARSE_POINT, IntPtr.Zero);
            if (handle.IsInvalid)
            {
                var error = Marshal.GetLastWin32Error();
                handle.Dispose();
                return Failed(error);
            }

            if (!Kernel32.GetFileInformationByHandle(handle, out var info))
            {
                var error = Marshal.GetLastWin32Error();
                handle.Dispose();
                return new FileHold(HoldOutcome.Refused, WindowsError: error);
            }

            // The refusal of a link, made again on what the handle holds, so the file
            // acted on is the one the checks before the take looked at.
            if ((info.dwFileAttributes & (Kernel32.FILE_ATTRIBUTE_REPARSE_POINT | Kernel32.FILE_ATTRIBUTE_DIRECTORY)) != 0)
            {
                handle.Dispose();
                return new FileHold(HoldOutcome.NotAPlainFile);
            }

            return new FileHold(HoldOutcome.Held, new HeldFile(handle, path, Folder, info.dwFileAttributes));
        }

        private static FileHold Failed(int error) => new(OutcomeOf(error), WindowsError: error);

        public CopySourceOpen OpenToCopy(string path)
        {
            var handle = Kernel32.CreateFile(
                FullPaths.ForWindowsCall(path), Kernel32.GENERIC_READ, Kernel32.FILE_SHARE_READ, IntPtr.Zero,
                Kernel32.OPEN_EXISTING, Kernel32.FILE_FLAG_OPEN_REPARSE_POINT, IntPtr.Zero);
            if (handle.IsInvalid)
            {
                var error = Marshal.GetLastWin32Error();
                handle.Dispose();

                // Windows refuses this open of a folder with an error that does not say
                // so, and the attributes by path do.
                if (Kernel32.GetFileAttributesEx(
                        FullPaths.ForWindowsCall(path), Kernel32.GetFileExInfoStandard, out var byPath)
                    && (byPath.dwFileAttributes & Kernel32.FILE_ATTRIBUTE_DIRECTORY) != 0)
                    return new CopySourceOpen(HoldOutcome.NotAPlainFile);
                return new CopySourceOpen(OutcomeOf(error), WindowsError: error);
            }

            if (!Kernel32.GetFileInformationByHandle(handle, out var info))
            {
                var error = Marshal.GetLastWin32Error();
                handle.Dispose();
                return new CopySourceOpen(HoldOutcome.Refused, WindowsError: error);
            }

            if ((info.dwFileAttributes & (Kernel32.FILE_ATTRIBUTE_REPARSE_POINT | Kernel32.FILE_ATTRIBUTE_DIRECTORY)) != 0)
            {
                handle.Dispose();
                return new CopySourceOpen(HoldOutcome.NotAPlainFile);
            }

            if (!TryStamp(handle, info, out var stamp))
            {
                var error = Marshal.GetLastWin32Error();
                handle.Dispose();
                return new CopySourceOpen(HoldOutcome.Refused, WindowsError: error);
            }

            return new CopySourceOpen(
                HoldOutcome.Held,
                new CopySource(new FileStream(handle, FileAccess.Read), stamp, (FileAttributes)info.dwFileAttributes));
        }

        public RenameReach ReachOf(DestinationFolder destination, out int failureCode)
        {
            failureCode = 0;
            var leaf = InHandFolders.NewProbeName();
            using var probe = DirectoryGuard.CreateChildFileHeld(
                _batch, leaf, Ntdll.DELETE | Kernel32.FILE_READ_ATTRIBUTES, out var status);
            if (probe is null)
            {
                failureCode = (int)Ntdll.RtlNtStatusToDosError(status);
                return RenameReach.Unestablished;
            }

            // The empty file is removed through its handle wherever the rename left it.
            var reach = HeldFile.Rename(probe, destination, leaf, out failureCode)
                ? RenameReach.SameVolume
                : failureCode == Kernel32.ERROR_NOT_SAME_DEVICE ? RenameReach.AnotherVolume : RenameReach.Unestablished;
            if (!DirectoryGuard.DeleteClearingReadOnly(probe, 0) && reach == RenameReach.SameVolume)
            {
                failureCode = Marshal.GetLastWin32Error();
                return RenameReach.Unestablished;
            }

            if (reach != RenameReach.Unestablished) failureCode = 0;
            return reach;
        }

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;

            // The batch's folder is let go and then removed through the folder above it,
            // and that folder through the Installer folder, each only where it is empty: a
            // file that could not be put back stays, with both folders, until a scan puts
            // it back.
            _batch.Dispose();
            DirectoryGuard.TryRemoveEmptyChildFolder(_inHand, _name);
            _inHand.Dispose();
            DirectoryGuard.TryRemoveEmptyChildFolder(_installer, InHandFolders.FolderName);
            _installer.Dispose();
        }
    }

    /// <summary>What a failed open says about the file, from the Windows error.</summary>
    internal static HoldOutcome OutcomeOf(int error) => error switch
    {
        Kernel32.ERROR_SHARING_VIOLATION or Kernel32.ERROR_LOCK_VIOLATION => HoldOutcome.InUse,
        Kernel32.ERROR_FILE_NOT_FOUND or Kernel32.ERROR_PATH_NOT_FOUND => HoldOutcome.Gone,
        _ => HoldOutcome.Refused,
    };

    /// <summary>The stamp of the file <paramref name="handle"/> names, with <paramref name="info"/> read from the same handle.</summary>
    private static bool TryStamp(SafeFileHandle handle, Kernel32.BY_HANDLE_FILE_INFORMATION info, out FileStamp stamp)
    {
        stamp = default;
        if (DirectoryGuard.IdOf(handle) is not { } identity) return false;

        stamp = new FileStamp(
            identity,
            ((long)info.nFileSizeHigh << 32) | info.nFileSizeLow,
            ((long)info.ftLastWriteTime.dwHighDateTime << 32) | info.ftLastWriteTime.dwLowDateTime);
        return true;
    }

    private sealed class CopySource(FileStream content, FileStamp stamp, FileAttributes attributes) : ICopySource
    {
        public Stream Content { get; } = content;
        public FileStamp Stamp { get; } = stamp;
        public FileAttributes Attributes { get; } = attributes;

        public void Dispose() => Content.Dispose();
    }

    private sealed class HeldFile : IHeldFile
    {
        private readonly SafeFileHandle _handle;
        private readonly string _batchFolder;
        private readonly uint _attributes;
        private readonly SafeFileHandle? _home;

        /// <param name="home">
        /// For a file an earlier batch left, held where it lies: the Installer folder's
        /// open handle, which <see cref="PutBack"/> renames it into. Null for a file held
        /// in its place, which goes back to the path it was held at.
        /// </param>
        public HeldFile(
            SafeFileHandle handle, string recordedPath, string batchFolder, uint attributes,
            SafeFileHandle? home = null)
        {
            _handle = handle;
            RecordedPath = recordedPath;
            _batchFolder = batchFolder;
            _attributes = attributes;
            _home = home;
            IsOut = home is not null;
        }

        public string RecordedPath { get; }

        public bool IsOut { get; private set; }

        public bool TryReadStamp(out FileStamp stamp)
        {
            stamp = default;
            return Kernel32.GetFileInformationByHandle(_handle, out var info) && TryStamp(_handle, info, out stamp);
        }

        public bool TakeOut(out int failureCode)
        {
            var away = PathText.Combine(_batchFolder, PathText.GetFileName(RecordedPath));
            if (!DirectoryGuard.RenameByHandle(_handle, null, FullPaths.ForWindowsCall(away), out failureCode))
                return false;
            IsOut = true;
            return true;
        }

        public bool PutBack(out int failureCode)
        {
            var back = _home is null
                ? DirectoryGuard.RenameByHandle(_handle, null, FullPaths.ForWindowsCall(RecordedPath), out failureCode)
                : DirectoryGuard.RenameByHandle(_handle, _home, PathText.GetFileName(RecordedPath), out failureCode);
            if (!back) return false;
            IsOut = false;
            return true;
        }

        public bool Delete(out int failureCode)
        {
            // The disposition that takes effect as the handle closes, which this handle,
            // sharing nothing, is the only one to do. A read-only file has its attribute
            // cleared for it and put back where the request is refused.
            failureCode = 0;
            if (DirectoryGuard.DeleteClearingReadOnly(_handle, _attributes)) return true;
            failureCode = Marshal.GetLastWin32Error();
            return false;
        }

        public bool MoveInto(DestinationFolder destination, string name, out int failureCode)
        {
            if (!Rename(_handle, destination, name, out failureCode)) return false;
            IsOut = false;
            return true;
        }

        /// <summary>
        /// Renames a file into <paramref name="destination"/> under <paramref name="name"/>,
        /// relative to the handle the destination was opened on where it has one.
        /// </summary>
        internal static bool Rename(SafeFileHandle file, DestinationFolder destination, string name, out int failureCode) =>
            destination.Handle is { } folder
                ? DirectoryGuard.RenameByHandle(file, folder, name, out failureCode)
                : DirectoryGuard.RenameByHandle(
                    file, null, FullPaths.ForWindowsCall(PathText.Combine(destination.Path, name)), out failureCode);

        public void Dispose() => _handle.Dispose();
    }
}
