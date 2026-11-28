namespace InstallerClean.Services;

/// <summary>
/// The app's own folder inside the Installer folder, which a file passes through on its
/// way out. A Move or Delete opens each file so that nothing else can open it, renames
/// it out of its place into the batch's folder here, looks once more at whether Windows
/// Installer is busy or its records have changed, and only then deletes the file or
/// sends it on. Where that look says no, one rename puts the file straight back.
///
/// WHY A FILE LEAVES ITS PLACE BEFORE THE LAST LOOK. Windows Installer takes up a cached
/// file only where it finds the file at its recorded path, and writes its record of the
/// use before it looks. So a file that has left its place, with the records then read
/// unchanged, has not been taken up, however long the app's own calls took.
///
/// AT MOST ONE FILE IS EVER OUT OF ITS PLACE, and only for the calls that remove it.
/// Windows Installer looks for a cached file at its recorded path and nowhere else, so
/// nothing is taken that is not about to go.
///
/// <see cref="HandleInHandFolder"/> is the way the app does it, on file handles;
/// <see cref="ByPathInHandFolder"/> is the one the test seams use, where the filesystem
/// is a substitute.
/// </summary>
public interface IInHandFolder
{
    /// <summary>
    /// Makes a folder for one batch inside <see cref="InHandFolders.FolderName"/> in
    /// <paramref name="installerFolder"/>, and keeps it open until the batch is disposed
    /// of. Null where it could not be made, with the Windows error in
    /// <paramref name="failureCode"/>; nothing has been touched then.
    /// </summary>
    IInHandBatch? OpenBatch(string installerFolder, out int failureCode);
}

/// <summary>Names the app's own folder inside the Installer folder.</summary>
public static class InHandFolders
{
    /// <summary>
    /// The folder inside the Installer folder that holds each batch's own. The scan lists
    /// only the files directly in the Installer folder, so nothing in here is ever listed.
    /// </summary>
    public const string FolderName = "InstallerClean-in-hand";
}

/// <summary>
/// One batch's folder, open for as long as the batch runs. Disposing of it removes the
/// folder, and the folder above it, each only where it is empty.
///
/// A FOLDER FOR EACH BATCH, so a file a killed batch left behind can never stand in the
/// way of a later batch taking a file Windows has since cached under the same name.
/// </summary>
public interface IInHandBatch : IDisposable
{
    /// <summary>The batch's folder, as a full path.</summary>
    string Folder { get; }

    /// <summary>
    /// Opens the file at <paramref name="path"/> where it stands, so that nothing else
    /// can open it, without following a link. The file has not moved.
    ///
    /// THE OPEN SHARES NOTHING, so Windows refuses it whenever anything has the file open,
    /// to read, write or delete, and that refusal is <see cref="HoldOutcome.InUse"/>: an
    /// answer from Windows that something is using the file at this moment. It is what
    /// keeps a file whose installer has a window open and has raised no other sign. Do
    /// not add a sharing flag to it.
    /// </summary>
    FileHold Hold(string path);

    /// <summary>
    /// Opens the file at <paramref name="path"/> to read it for a copy, sharing reading
    /// and nothing else, without following a link. Windows Installer's own opens of a
    /// cached file succeed beside this handle, so an installation that runs while a
    /// copy is being made is not disturbed by it.
    /// </summary>
    CopySourceOpen OpenToCopy(string path);

    /// <summary>
    /// Whether a file can be renamed from this batch's folder into
    /// <paramref name="destination"/>, asked of Windows by renaming an empty file of the
    /// app's own there and removing it. A Move must know before it takes any file: a file
    /// that cannot go by a rename is copied first, with the original untouched in its place.
    /// </summary>
    RenameReach ReachOf(DestinationFolder destination, out int failureCode);
}

/// <summary>Whether a rename from the batch's folder reaches a destination.</summary>
public enum RenameReach
{
    /// <summary>
    /// The question was not answered, so nothing is established. First member so that it
    /// is what the type's own zero carries. A Move treats it as
    /// <see cref="AnotherVolume"/>: a copy made first is right on any volume.
    /// </summary>
    Unestablished,

    /// <summary>The rename worked: the destination is on the Installer folder's volume.</summary>
    SameVolume,

    /// <summary>Windows answered that the destination is on another volume.</summary>
    AnotherVolume,
}

/// <summary>What <see cref="IInHandBatch.Hold"/> found.</summary>
public enum HoldOutcome
{
    /// <summary>
    /// The open failed for a reason no other member names; the Windows error is in
    /// <see cref="FileHold.WindowsError"/>. First member so that it is what the type's
    /// own zero carries.
    /// </summary>
    Refused,

    /// <summary>The file is open to the app alone, in its place.</summary>
    Held,

    /// <summary>Something else has the file open.</summary>
    InUse,

    /// <summary>Nothing is at the path.</summary>
    Gone,

    /// <summary>What is at the path is a link or a folder, which the app does not act on.</summary>
    NotAPlainFile,
}

/// <summary>The answer to <see cref="IInHandBatch.Hold"/>: the outcome, and the held file where there is one.</summary>
public readonly record struct FileHold(HoldOutcome Outcome, IHeldFile? File = null, int WindowsError = 0);

/// <summary>
/// The answer to <see cref="IInHandBatch.OpenToCopy"/>, with the outcomes
/// <see cref="IInHandBatch.Hold"/> has: <see cref="HoldOutcome.Held"/> is an open source,
/// which holds the file only against being written or deleted.
/// </summary>
public readonly record struct CopySourceOpen(HoldOutcome Outcome, ICopySource? Source = null, int WindowsError = 0);

/// <summary>
/// What tells one state of one file from any other: which file it is, how long it is
/// and when it was last written. A copy is made from one open of a file and the file is
/// then taken through another; the two opens are of the same bytes where their stamps
/// are equal.
/// </summary>
public readonly record struct FileStamp(FileIdentity Identity, long Length, long LastWriteTime);

/// <summary>A file open for a copy to be read from.</summary>
public interface ICopySource : IDisposable
{
    /// <summary>The file's bytes, from the start.</summary>
    Stream Content { get; }

    /// <summary>The file's stamp, read from the open handle.</summary>
    FileStamp Stamp { get; }

    /// <summary>The file's attributes, read from the open handle, for the copy to carry.</summary>
    FileAttributes Attributes { get; }
}

/// <summary>
/// A file the app has open so that nothing else can open it. It is either in its place
/// or in the batch's folder, and every act on it is made through the one handle.
///
/// DISPOSE OF IT ONLY WHEN THE FILE IS IN A FINISHED PLACE: deleted, at its destination,
/// or back at its recorded path. Disposing closes the handle wherever the file is, and a
/// file left in the batch's folder stays there, whole.
/// </summary>
public interface IHeldFile : IDisposable
{
    /// <summary>The path the file was held at, which <see cref="PutBack"/> returns it to.</summary>
    string RecordedPath { get; }

    /// <summary>Whether the file is out of its place, in the batch's folder.</summary>
    bool IsOut { get; }

    /// <summary>The file's stamp, read from the handle. False where it could not be read.</summary>
    bool TryReadStamp(out FileStamp stamp);

    /// <summary>
    /// Renames the file into the batch's folder, keeping its name and replacing nothing.
    /// False where Windows refused, with the file still in its place.
    /// </summary>
    bool TakeOut(out int failureCode);

    /// <summary>
    /// Renames the file back to <see cref="RecordedPath"/>, replacing nothing. False
    /// where Windows refused, with the file still in the batch's folder: something now
    /// stands under its name, or the rename failed.
    /// </summary>
    bool PutBack(out int failureCode);

    /// <summary>
    /// Asks for the file to be deleted when its handle closes, clearing the read-only
    /// attribute first where the file has it. True means disposing of this now deletes
    /// the file. False where Windows refused, with the attribute as it was and the file
    /// where it was.
    /// </summary>
    bool Delete(out int failureCode);

    /// <summary>
    /// Renames the file to <paramref name="name"/> in <paramref name="destination"/>,
    /// replacing nothing. False where Windows refused, with the file where it was;
    /// <see cref="Interop.Native.Kernel32.ERROR_NOT_SAME_DEVICE"/> says the destination is
    /// on another volume.
    /// </summary>
    bool MoveInto(DestinationFolder destination, string name, out int failureCode);
}
