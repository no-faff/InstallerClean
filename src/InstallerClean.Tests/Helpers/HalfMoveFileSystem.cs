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
    /// in for another program holding the source open, which nothing in the
    /// mock models, and for a destination folder that accepts a create and
    /// refuses a delete. The other condition that leaves a real source behind,
    /// a read-only attribute, needs no entry here: the base implementation
    /// refuses that delete on its own.
    /// </summary>
    internal Dictionary<string, Exception> DeleteFailures { get; } =
        new(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Paths that are gone by the time their attributes are read, and answer
    /// that read as missing. Poses the one interleaving nothing else can, a
    /// third party taking the source away between the reconcile's existence
    /// check and its attribute read: both calls are the framework's and the
    /// window between them is inside one method.
    /// </summary>
    internal HashSet<string> VanishOnAttributeRead { get; } =
        new(StringComparer.OrdinalIgnoreCase);

    private sealed class HalfMoveFile : MockFile
    {
        private readonly HalfMoveFileSystem _fs;

        internal HalfMoveFile(HalfMoveFileSystem fs) : base(fs) => _fs = fs;

        public override void Move(string sourceFileName, string destFileName) =>
            Copy(sourceFileName, destFileName);

        public override FileAttributes GetAttributes(string path)
        {
            if (_fs.VanishOnAttributeRead.Remove(path))
            {
                _fs.RemoveFile(path);
                throw new FileNotFoundException($"vanished before the attribute read: {path}", path);
            }

            return base.GetAttributes(path);
        }

        public override void Delete(string path)
        {
            if (_fs.DeleteFailures.TryGetValue(path, out var failure)) throw failure;

            // Win32 documents DeleteFile as failing with ERROR_ACCESS_DENIED on
            // a read-only file. TestingHelpers 22.2.0, the pinned version,
            // enforces it too (checked by running it), so this sits dead behind
            // the base implementation and is insurance rather than cover: the
            // enforcement is not in the library's published contract, and an
            // upgrade dropping it would leave the double unable to tell a
            // service that clears the attribute from one that never did.
            if (base.Exists(path) && base.GetAttributes(path).HasFlag(FileAttributes.ReadOnly))
                throw new UnauthorizedAccessException($"read-only: {path}");

            base.Delete(path);
        }
    }
}
