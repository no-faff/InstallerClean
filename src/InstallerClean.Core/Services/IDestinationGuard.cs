using System.IO.Abstractions;
using System.Runtime.InteropServices;
using InstallerClean.Helpers;
using InstallerClean.Interop.Native;

namespace InstallerClean.Services;

/// <summary>
/// The acts a Move makes on its destination: the creation of a folder, a write
/// test, the removal of a folder the Move created, the opening of the folder for the
/// length of the batch, the creation of a copy in it, and the discarding of a copy
/// whose original then stayed where it was. The creation of a folder says from the create itself whether it made the
/// folder. The user may pick a folder another account can write, so the write test, the
/// discard and the removal are each reached in a way that a folder swapped in under the
/// same name part way through cannot redirect. The removal reaches the folder through
/// every folder the Move made above it, each opened without following a link, so a link
/// put in place of any of them refuses the removal.
/// <see cref="HandleDestinationGuard"/> is that way in the app;
/// <see cref="ByPathDestinationGuard"/> is the one the test seams use, where the real
/// filesystem is substituted.
/// </summary>
public interface IDestinationGuard
{
    /// <summary>
    /// Creates the one folder <paramref name="folder"/>, whose parent is there already,
    /// and says from the create itself whether it made it: <see cref="FolderCreation.Made"/>
    /// where this call made it, <see cref="FolderCreation.AlreadyThere"/> where the name
    /// was taken before the call, and <see cref="FolderCreation.Failed"/> where the create
    /// failed for any other reason. Never throws.
    /// </summary>
    FolderCreation TryCreateFolder(string folder);

    /// <summary>
    /// Whether the folder accepts a new file and lets it be read back, with the Win32
    /// cause of a refusal in <paramref name="failureCode"/> for a caller to record and
    /// the step that refused in <paramref name="refusal"/>, so a caller names a cause
    /// only where the probe established it. A folder whose files can be read and not
    /// deleted passes.
    /// </summary>
    bool TryProbeWritable(string folder, out int failureCode, out WriteProbeRefusal refusal);

    /// <summary>
    /// Removes the file <paramref name="name"/> in <paramref name="destination"/>, where
    /// it is still there, so a file reported as not moved is one the user has exactly one
    /// of. Answers true where it is gone, false where it is present and would not go, with
    /// the Win32 cause in <paramref name="failureCode"/>. A folder at the name is left and
    /// answered false with ERROR_DIRECTORY_NOT_SUPPORTED.
    ///
    /// IT REMOVES FROM THE FOLDER THAT WAS OPENED, which is the folder the copy was
    /// created in, and not from whatever the folder's path names by then.
    /// </summary>
    bool TryDiscardCopy(DestinationFolder destination, string name, out int failureCode);

    /// <summary>
    /// Removes <paramref name="folder"/> where it is empty. <paramref name="existingAncestor"/>
    /// is the nearest folder above it that was there before the Move: the folder the user
    /// chose, or the one the chosen path leads to, which may be a volume mount point or a
    /// directory junction and is followed to the directory it stands for. Every folder
    /// between it and <paramref name="folder"/> is one the Move made, and each is reached
    /// from the one above without following a link, so a link in the place of any of them
    /// answers false and removes nothing. Answers false too where the folder is not empty.
    /// </summary>
    bool TryRemoveEmptyFolder(string folder, string existingAncestor);

    /// <summary>
    /// Opens <paramref name="folder"/> for the length of a Move, so that each file sent
    /// into it lands in the folder that was opened and not in one put in its place under
    /// the same name. Null where it cannot be opened, with the Win32 cause in
    /// <paramref name="failureCode"/>.
    /// </summary>
    DestinationFolder? Open(string folder, out int failureCode);

    /// <summary>
    /// Creates a new, empty file in <paramref name="destination"/> under a temporary name
    /// of the guard's own, for a copy to be written into. Null where it cannot be
    /// created, with the Win32 cause in <paramref name="failureCode"/>.
    /// </summary>
    ICopyTarget? CreateCopy(DestinationFolder destination, out int failureCode);
}

/// <summary>
/// A Move's destination folder, opened once for the batch
/// (<see cref="IDestinationGuard.Open"/>). The app's guard holds a handle on the folder,
/// and every act made with this lands relative to that handle; the test seam's carries
/// the path alone.
/// </summary>
public sealed class DestinationFolder : IDisposable
{
    internal DestinationFolder(string path, Microsoft.Win32.SafeHandles.SafeFileHandle? handle)
    {
        Path = path;
        Handle = handle;
    }

    /// <summary>The folder's path, as the caller gave it.</summary>
    public string Path { get; }

    /// <summary>The handle on the folder, where the guard that opened it holds one.</summary>
    internal Microsoft.Win32.SafeHandles.SafeFileHandle? Handle { get; }

    public void Dispose() => Handle?.Dispose();
}

/// <summary>
/// A copy being written into a Move's destination, under a temporary name that cannot be
/// taken for a finished file. Disposing of it before <see cref="Finish"/> has answered
/// <see cref="CopyFinish.Finished"/> removes the temporary file.
/// </summary>
public interface ICopyTarget : IDisposable
{
    /// <summary>
    /// Where the copy's bytes are written, from the start, each write going straight to
    /// the file. The target owns it and closes it: a caller writes to it and does not
    /// dispose of it.
    /// </summary>
    Stream Content { get; }

    /// <summary>
    /// Finishes the copy: writes everything through to the device, checks that the file
    /// is <paramref name="expectedLength"/> bytes long, gives it
    /// <paramref name="attributes"/> and <paramref name="lastWriteTime"/> (a FILETIME),
    /// and renames it to <paramref name="name"/> in the destination, replacing nothing.
    /// Any answer but <see cref="CopyFinish.Finished"/> leaves the temporary file for
    /// <see cref="IDisposable.Dispose"/> to remove, with the Win32 cause, where there is
    /// one, in <paramref name="failureCode"/>.
    /// </summary>
    CopyFinish Finish(string name, long expectedLength, FileAttributes attributes, long lastWriteTime, out int failureCode);
}

/// <summary>What <see cref="ICopyTarget.Finish"/> did.</summary>
public enum CopyFinish
{
    /// <summary>A step failed. First member so that it is what the type's own zero carries.</summary>
    Failed,

    /// <summary>The copy is whole and under its real name, and both have been written through to the device.</summary>
    Finished,

    /// <summary>The copy is not the length of the file it was made from.</summary>
    LengthDiffers,

    /// <summary>Something already has the real name in the destination.</summary>
    NameTaken,
}

/// <summary>What <see cref="IDestinationGuard.TryCreateFolder"/> found.</summary>
public enum FolderCreation
{
    /// <summary>
    /// The create failed, so nothing is established about the folder. First member so
    /// that it is what the type's own zero carries, for the reason
    /// <see cref="FirstRunMarkState.Unreadable"/> is first.
    /// </summary>
    Failed,

    /// <summary>The create made the folder.</summary>
    Made,

    /// <summary>The folder was there before the create, as a folder or as anything else with that name.</summary>
    AlreadyThere,
}

/// <summary>
/// Reaches each act through a handle opened on the destination folder, so the act lands
/// on the folder the handle names and not on a folder put in its place under the same
/// name. This is what the app uses.
/// </summary>
internal sealed class HandleDestinationGuard : IDestinationGuard
{
    /// <summary>
    /// Makes the one folder through CreateDirectoryW, given its full path with <c>\\?\</c> in
    /// front whatever its length, as .NET 10's own CreateDirectory gives it: without the
    /// prefix, CreateDirectoryW's documented limit is 248 characters. A folder whose full path
    /// cannot be made is <see cref="FolderCreation.Failed"/>.
    /// </summary>
    public FolderCreation TryCreateFolder(string folder)
    {
        try
        {
            var attributes = Kernel32.SECURITY_ATTRIBUTES.With(IntPtr.Zero);
            var path = PathText.EnsureExtendedPrefix(FullPaths.GetFullPath(folder));
            if (Kernel32.CreateDirectory(path, in attributes)) return FolderCreation.Made;
            return Marshal.GetLastWin32Error() == Kernel32.ERROR_ALREADY_EXISTS
                ? FolderCreation.AlreadyThere
                : FolderCreation.Failed;
        }
        catch
        {
            return FolderCreation.Failed;
        }
    }

    public bool TryProbeWritable(string folder, out int failureCode, out WriteProbeRefusal refusal) =>
        DirectoryGuard.TryProbeWritable(folder, out failureCode, out refusal);

    public bool TryDiscardCopy(DestinationFolder destination, string name, out int failureCode)
    {
        failureCode = 0;
        if (destination.Handle is not { } folder || string.IsNullOrEmpty(name))
        {
            failureCode = Kernel32.ERROR_INVALID_PARAMETER;
            return false;
        }
        return DirectoryGuard.TryDiscardChildCopy(folder, name, out failureCode);
    }

    public DestinationFolder? Open(string folder, out int failureCode)
    {
        var handle = DirectoryGuard.OpenDirectoryFollow(folder, out failureCode);
        if (handle is not null) return new DestinationFolder(folder, handle);

        // A folder that was not asked of Windows at all, an empty path among them, has
        // no Windows error at all, and 0 would read as success.
        if (failureCode == 0) failureCode = Kernel32.ERROR_INVALID_PARAMETER;
        return null;
    }

    /// <summary>
    /// What the copy's handle asks for: to write the file's bytes and attributes, to read
    /// its attributes back, and to delete it, which the rename to its real name and the
    /// removal of a copy that is not finished both need.
    /// </summary>
    internal const uint CopyAccess =
        Kernel32.GENERIC_WRITE | Kernel32.FILE_READ_ATTRIBUTES | Kernel32.FILE_WRITE_ATTRIBUTES | Ntdll.DELETE;

    /// <summary>
    /// The name a copy is written under until it is finished: the app's own prefix, a
    /// random part and <c>.tmp</c>, which no cached file is named.
    /// </summary>
    internal static string TemporaryCopyName() => "InstallerClean-copying-" + Guid.NewGuid().ToString("N") + ".tmp";

    public ICopyTarget? CreateCopy(DestinationFolder destination, out int failureCode)
    {
        failureCode = 0;
        if (destination.Handle is not { } folder)
        {
            failureCode = Kernel32.ERROR_INVALID_PARAMETER;
            return null;
        }

        var temporary = TemporaryCopyName();
        var handle = DirectoryGuard.CreateChildFileHeld(folder, temporary, CopyAccess, out var status);
        if (handle is null)
        {
            failureCode = (int)Ntdll.RtlNtStatusToDosError(status);
            return null;
        }
        return new CopyTarget(handle, destination, temporary);
    }

    private sealed class CopyTarget : ICopyTarget
    {
        private readonly Microsoft.Win32.SafeHandles.SafeFileHandle _handle;
        private readonly DestinationFolder _destination;
        private readonly string _temporary;
        private readonly FileStream _content;
        private bool _finished;

        public CopyTarget(
            Microsoft.Win32.SafeHandles.SafeFileHandle handle, DestinationFolder destination, string temporary)
        {
            _handle = handle;
            _destination = destination;
            _temporary = temporary;

            // No buffer of the stream's own: a write that fails is the caller's to hear
            // of when it is made, and nothing is left for the close to write.
            _content = StorageHelpers.UnbufferedWriter(handle);
        }

        public Stream Content => _content;

        public unsafe CopyFinish Finish(
            string name, long expectedLength, FileAttributes attributes, long lastWriteTime, out int failureCode)
        {
            failureCode = 0;
            try
            {
                // Through to the device, so the copy does not rest on the cache when the
                // file it was made from goes.
                _content.Flush(flushToDisk: true);
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or ObjectDisposedException)
            {
                failureCode = WindowsErrors.Of(ex) ?? Kernel32.ERROR_UNIDENTIFIED_ERROR;
                return CopyFinish.Failed;
            }

            if (!Kernel32.GetFileInformationByHandle(_handle, out var info))
            {
                failureCode = Marshal.GetLastWin32Error();
                return CopyFinish.Failed;
            }
            if ((((long)info.nFileSizeHigh << 32) | info.nFileSizeLow) != expectedLength)
                return CopyFinish.LengthDiffers;

            // The attributes SetFileAttributes can set, and normal where none is left,
            // a zero leaving them unchanged. The other times are left zero, as they are.
            var carried = (uint)attributes & CarriedAttributes;
            var basic = new Kernel32.FILE_BASIC_INFO
            {
                LastWriteTime = lastWriteTime,
                FileAttributes = carried == 0 ? AttributeNormal : carried,
            };
            if (!Kernel32.SetFileBasicInfoByHandle(
                    _handle, Kernel32.FileBasicInfo, in basic, (uint)sizeof(Kernel32.FILE_BASIC_INFO)))
            {
                failureCode = Marshal.GetLastWin32Error();
                return CopyFinish.Failed;
            }

            if (!DirectoryGuard.RenameByHandle(_handle, _destination.Handle, name, out failureCode))
                return failureCode == Kernel32.ERROR_ALREADY_EXISTS ? CopyFinish.NameTaken : CopyFinish.Failed;

            // Through to the device again, for the name, the time and the attributes.
            // Until this answers the copy is not finished, and disposing removes it.
            try
            {
                _content.Flush(flushToDisk: true);
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
            {
                failureCode = WindowsErrors.Of(ex) ?? Kernel32.ERROR_UNIDENTIFIED_ERROR;
                return CopyFinish.Failed;
            }

            _finished = true;
            return CopyFinish.Finished;
        }

        /// <summary>Archive, hidden, normal, not content indexed, offline, read-only, system and temporary.</summary>
        private const uint CarriedAttributes = 0x000031A7;
        private const uint AttributeNormal = 0x00000080;

        public void Dispose()
        {
            // A copy that was not finished is removed through its own handle, a read-only
            // attribute it was given cleared for that, before the handle closes.
            if (!_finished && !_handle.IsClosed)
            {
                var attributes = Kernel32.GetFileInformationByHandle(_handle, out var info) ? info.dwFileAttributes : 0;
                DirectoryGuard.DeleteClearingReadOnly(_handle, attributes);
            }
            else if (!_finished && _destination.Handle is { IsClosed: false } folder)
            {
                // The stream was closed under the target, and its handle with it: the
                // temporary file is removed by its name inside the folder's handle.
                DirectoryGuard.TryRemoveChildFile(folder, _temporary);
            }

            try
            {
                _content.Dispose();
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
            {
                // The handle is closed whatever the close's own last write did.
            }
        }
    }

    public bool TryRemoveEmptyFolder(string folder, string existingAncestor)
    {
        if (string.IsNullOrEmpty(folder) || string.IsNullOrEmpty(existingAncestor)) return false;

        var relative = FullPaths.GetRelativePath(existingAncestor, folder);
        if (PathText.IsPathRooted(relative)) return false;
        var names = relative.Split(
            [Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar],
            StringSplitOptions.RemoveEmptyEntries);
        if (names.Length == 0 || names.Any(name => name is "." or "..")) return false;

        using var ancestor = DirectoryGuard.OpenDirectoryFollow(existingAncestor, out _);
        return ancestor is not null && DirectoryGuard.TryRemoveEmptyDescendantFolder(ancestor, names);
    }
}

/// <summary>
/// Acts by path through an injected <see cref="IFileSystem"/>, for the test seam where
/// that filesystem is a substitute. The app never uses it: the production path takes
/// <see cref="HandleDestinationGuard"/>, which a wiring test holds.
/// </summary>
internal sealed class ByPathDestinationGuard(IFileSystem fileSystem) : IDestinationGuard
{
    private readonly IFileSystem _fs = fileSystem;

    /// <summary>
    /// Reads whether the folder is there and creates it where it is not, so the answer
    /// rests on a read before the create, which a substituted filesystem cannot do
    /// otherwise. A create that throws is <see cref="FolderCreation.Failed"/>.
    /// </summary>
    public FolderCreation TryCreateFolder(string folder)
    {
        try
        {
            if (_fs.Directory.Exists(folder)) return FolderCreation.AlreadyThere;
            _fs.Directory.CreateDirectory(folder);
            return FolderCreation.Made;
        }
        catch
        {
            return FolderCreation.Failed;
        }
    }

    /// <summary>
    /// Writes an empty file, reads it back and deletes it, giving the answers the app's
    /// guard gives: a refused write or read fails the probe, an
    /// <see cref="UnauthorizedAccessException"/> naming the step as refused access and
    /// any other exception as a failure of it, and a refused delete does not fail the
    /// probe, since a folder whose files can be read and not deleted passes there too.
    /// There is no folder to open, so <see cref="WriteProbeRefusal.FolderOpen"/> is
    /// never given.
    /// </summary>
    public bool TryProbeWritable(string folder, out int failureCode, out WriteProbeRefusal refusal)
    {
        failureCode = 0;
        refusal = WriteProbeRefusal.None;
        var probe = PathText.Combine(folder, _fs.Path.GetRandomFileName());
        // BannedSymbols.txt refuses WriteAllBytes and ReadAllBytes, which reach .NET
        // Framework's own File. They are allowed here alone: the app's composition
        // registers HandleDestinationGuard, which the wiring test holds, so this guard
        // writes only through the filesystem a test substitutes.
#pragma warning disable RS0030
        try
        {
            _fs.File.WriteAllBytes(probe, Array.Empty<byte>());
        }
        catch (Exception ex)
        {
            failureCode = Win32CodeOf(ex);
            refusal = ex is UnauthorizedAccessException
                ? WriteProbeRefusal.CreateRefusedAccess : WriteProbeRefusal.CreateFailed;
            return false;
        }

        try
        {
            _fs.File.ReadAllBytes(probe);
            return true;
        }
#pragma warning restore RS0030
        catch (Exception ex)
        {
            failureCode = Win32CodeOf(ex);
            refusal = ex is UnauthorizedAccessException
                ? WriteProbeRefusal.ReadBackRefusedAccess : WriteProbeRefusal.ReadBackFailed;
            return false;
        }
        finally
        {
            try { _fs.File.Delete(probe); } catch { }
        }
    }

    public bool TryDiscardCopy(DestinationFolder destination, string name, out int failureCode)
    {
        failureCode = 0;
        var destinationPath = PathText.Combine(destination.Path, name);
        try
        {
            if (!_fs.File.Exists(destinationPath))
            {
                if (!_fs.Directory.Exists(destinationPath)) return true;
                failureCode = Kernel32.ERROR_DIRECTORY_NOT_SUPPORTED;
                return false;
            }
            var attributes = _fs.File.GetAttributes(destinationPath);
            var readOnly = attributes.HasFlag(FileAttributes.ReadOnly);
            if (readOnly)
                _fs.File.SetAttributes(destinationPath, attributes & ~FileAttributes.ReadOnly);
            try
            {
                _fs.File.Delete(destinationPath);
            }
            catch
            {
                // The copy stays as it was, read-only included.
                if (readOnly)
                {
                    try { _fs.File.SetAttributes(destinationPath, attributes); } catch { }
                }
                throw;
            }
            return true;
        }
        catch (Exception ex)
        {
            failureCode = Win32CodeOf(ex);
            return false;
        }
    }

    public DestinationFolder? Open(string folder, out int failureCode)
    {
        failureCode = 0;
        try
        {
            if (_fs.Directory.Exists(folder)) return new DestinationFolder(folder, handle: null);
            failureCode = Kernel32.ERROR_PATH_NOT_FOUND;
            return null;
        }
        catch (Exception ex)
        {
            failureCode = Win32CodeOf(ex);
            return null;
        }
    }

    public ICopyTarget? CreateCopy(DestinationFolder destination, out int failureCode)
    {
        failureCode = 0;
        var temporary = PathText.Combine(destination.Path, HandleDestinationGuard.TemporaryCopyName());
        try
        {
            // Allowed here for the reason the write probe above gives: this guard writes
            // only through the filesystem a test substitutes.
#pragma warning disable RS0030
            return new CopyTarget(_fs, destination.Path, temporary, _fs.File.Create(temporary));
#pragma warning restore RS0030
        }
        catch (Exception ex)
        {
            failureCode = Win32CodeOf(ex);
            return null;
        }
    }

    private sealed class CopyTarget(IFileSystem fs, string folder, string temporary, Stream content) : ICopyTarget
    {
        private bool _finished;

        public Stream Content { get; } = content;

        public CopyFinish Finish(
            string name, long expectedLength, FileAttributes attributes, long lastWriteTime, out int failureCode)
        {
            failureCode = 0;
            try
            {
                Content.Flush();
                if (Content.Length != expectedLength) return CopyFinish.LengthDiffers;

                var destination = PathText.Combine(folder, name);
                if (fs.File.Exists(destination) || fs.Directory.Exists(destination)) return CopyFinish.NameTaken;

                // Closed only now, so a copy refused above can be finished under another name.
                Content.Dispose();

#pragma warning disable RS0030
                fs.File.SetLastWriteTimeUtc(temporary, DateTime.FromFileTimeUtc(lastWriteTime));
#pragma warning restore RS0030
                fs.File.Move(temporary, destination);
                fs.File.SetAttributes(destination, attributes);
                _finished = true;
                return CopyFinish.Finished;
            }
            catch (Exception ex)
            {
                failureCode = Win32CodeOf(ex);
                return CopyFinish.Failed;
            }
        }

        public void Dispose()
        {
            Content.Dispose();
            if (_finished) return;
            try
            {
                if (fs.File.Exists(temporary)) fs.File.Delete(temporary);
            }
            catch
            {
                // The temporary file stays under its temporary name.
            }
        }
    }

    public bool TryRemoveEmptyFolder(string folder, string existingAncestor)
    {
        try
        {
            // The one-argument Delete is the non-recursive removal: a folder holding anything is refused.
            _fs.Directory.Delete(folder);
            return true;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// The Windows error number the filesystem's exception stands for, in the form the app's
    /// guard gives it: <see cref="WindowsErrors.Of"/>, and
    /// <see cref="Kernel32.ERROR_ACCESS_DENIED"/> for an <see cref="UnauthorizedAccessException"/>,
    /// which .NET raises for that error. An exception .NET raised on its own account, or one
    /// that stands for no error, answers <see cref="Kernel32.ERROR_UNIDENTIFIED_ERROR"/>:
    /// never 0, which Windows reads as success.
    /// </summary>
    internal static int Win32CodeOf(Exception exception)
    {
        var code = exception is UnauthorizedAccessException
            ? Kernel32.ERROR_ACCESS_DENIED
            : WindowsErrors.Of(exception);
        return code is > 0 ? code.Value : Kernel32.ERROR_UNIDENTIFIED_ERROR;
    }
}
