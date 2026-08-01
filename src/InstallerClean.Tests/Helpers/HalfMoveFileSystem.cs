using System.IO.Abstractions;
using System.IO.Abstractions.TestingHelpers;

namespace InstallerClean.Tests.Helpers;

/// <summary>
/// A <see cref="MockFileSystem"/> whose <c>File.Move</c> copies and leaves the
/// source where it was, which is what Win32 does to a cross-volume move whose
/// source cannot be deleted: MOVEFILE_COPY_ALLOWED simulates the move with
/// CopyFile plus DeleteFile, and "if the file is successfully copied to a
/// different volume and the original file is unable to be deleted, the function
/// succeeds leaving the source file intact".
///
/// Nothing else can produce that state under test. MockFileSystem's own Move is
/// a move, and a real-filesystem test cannot make one volume refuse a delete on
/// demand, so without this double the only code that meets the condition is a
/// user's.
/// </summary>
internal sealed class HalfMoveFileSystem : MockFileSystem
{
    private readonly HalfMoveFile _file;

    internal HalfMoveFileSystem() => _file = new HalfMoveFile(this);

    public override IFile File => _file;

    /// <summary>
    /// Paths whose deletion throws instead of happening, keyed by path. Stands
    /// in for the two conditions that leave a real source behind after the copy
    /// (a read-only attribute, another program holding the file open), which
    /// MockFileSystem does not enforce, and for a destination folder that
    /// accepts a create and refuses a delete.
    /// </summary>
    internal Dictionary<string, Exception> DeleteFailures { get; } =
        new(StringComparer.OrdinalIgnoreCase);

    private sealed class HalfMoveFile : MockFile
    {
        private readonly HalfMoveFileSystem _fs;

        internal HalfMoveFile(HalfMoveFileSystem fs) : base(fs) => _fs = fs;

        public override void Move(string sourceFileName, string destFileName) =>
            Copy(sourceFileName, destFileName);

        public override void Delete(string path)
        {
            if (_fs.DeleteFailures.TryGetValue(path, out var failure)) throw failure;

            // Win32 documents DeleteFile as failing with ERROR_ACCESS_DENIED on
            // a read-only file and says the attribute has to be removed first.
            // MockFileSystem does not enforce it, so a service that never
            // cleared the attribute would pass a test written against the mock
            // alone and fail on a user's machine.
            if (base.Exists(path) && base.GetAttributes(path).HasFlag(FileAttributes.ReadOnly))
                throw new UnauthorizedAccessException($"read-only: {path}");

            base.Delete(path);
        }
    }
}
