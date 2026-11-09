namespace InstallerClean.Tests.Helpers;

/// <summary>
/// The folder every test that makes files on disk makes them in: one folder per AppDomain,
/// %TEMP%\InstallerClean.Tests\runs\ and a GUID, deleted as the domain ends.
/// </summary>
/// <remarks>
/// A test host killed part-way through a run never reaches its domain's end, so its folder is
/// left. The first use in each later domain sweeps those: each run's folder holds a file named
/// <c>owner</c> open, unshared, for as long as its domain is loaded, so a folder whose owner
/// file can be deleted belongs to no live run and goes whole, and a folder whose owner is held
/// is another run's and stays.
/// </remarks>
internal static class TestTemp
{
    private const string OwnerName = "owner";

    private static readonly string Runs =
        Path.Combine(Path.GetTempPath(), "InstallerClean.Tests", "runs");

    private static readonly Lazy<string> RunFolder = new(Start, LazyThreadSafetyMode.ExecutionAndPublication);

    private static FileStream? _owner;

    /// <summary>This domain's folder, made and its owner file opened on first use.</summary>
    internal static string Run => RunFolder.Value;

    /// <summary>
    /// A new, empty folder in <see cref="Run"/>, named <paramref name="prefix"/> and a GUID.
    /// </summary>
    internal static string NewFolder(string prefix)
    {
        var folder = Path.Combine(Run, prefix + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(folder);
        return folder;
    }

    /// <summary>
    /// A path in <see cref="Run"/> for a file named <paramref name="prefix"/>, a GUID and
    /// <paramref name="suffix"/>, nothing made.
    /// </summary>
    internal static string NewFilePath(string prefix, string suffix = "") =>
        Path.Combine(Run, prefix + Guid.NewGuid().ToString("N") + suffix);

    /// <summary>
    /// Deletes <paramref name="folder"/> and everything in it, read-only files included, and
    /// does nothing where it is not there. The folder is named with the \\?\ prefix, so paths
    /// of 260 characters and more inside it are deleted too. A folder that is itself a link,
    /// its target there or not, is removed as a link, and what it points at is left. A delete
    /// still refused after a few tries a tenth of a second apart, as a file another process is
    /// closing can be, throws an IOException naming the folder with the last refusal inside it,
    /// so the test whose clean-up it is fails. A folder <see cref="KeepOnFailure"/> marked is
    /// left for the run's end instead, and a folder <see cref="LeaveForRunEnd"/> marked is
    /// left by every delete, of it or of a folder holding it, while everything round it goes.
    /// </summary>
    internal static void Delete(string folder) => Delete(folder, attempts: 5);

    /// <summary>
    /// Returns false, so that as an exception filter it never catches: written as
    /// <c>catch (Exception) when (TestTemp.KeepOnFailure(folder)) { }</c> ahead of a
    /// <c>finally</c> that deletes the folder, it marks the folder as the exception leaves the
    /// test, and the delete in the <c>finally</c> then leaves it for the run's end, so a refused
    /// delete never takes the place of the test's own failure.
    /// </summary>
    internal static bool KeepOnFailure(string folder)
    {
        Mark(Kept, folder);
        return false;
    }

    /// <summary>
    /// Leaves <paramref name="folder"/> in place for the rest of the run, for the next run's
    /// sweep: every <see cref="Delete(string)"/> of it, or of a folder holding it, deletes everything
    /// else and leaves it. For a test whose subject is a file held open for the rest of the run,
    /// named by the folder that file is in.
    /// </summary>
    internal static void LeaveForRunEnd(string folder) => Mark(Left, folder);

    private static readonly HashSet<string> Kept = new(StringComparer.OrdinalIgnoreCase);
    private static readonly HashSet<string> Left = new(StringComparer.OrdinalIgnoreCase);

    private static void Mark(HashSet<string> marks, string folder)
    {
        lock (marks) marks.Add(Path.GetFullPath(folder).TrimEnd('\\'));
    }

    private static void Delete(string folder, int attempts)
    {
        var full = Path.GetFullPath(folder).TrimEnd('\\');
        lock (Kept)
        {
            if (Kept.Remove(full)) return;
        }
        string[] leftHere;
        lock (Left)
            leftHere = Left.Where(l => l.Equals(full, StringComparison.OrdinalIgnoreCase)
                || l.StartsWith(full + "\\", StringComparison.OrdinalIgnoreCase)
                || full.StartsWith(l + "\\", StringComparison.OrdinalIgnoreCase)).ToArray();
        if (leftHere.Length > 0)
        {
            DeleteAround(full, leftHere);
            return;
        }
        var target = folder.StartsWith(@"\\", StringComparison.Ordinal) ? folder : @"\\?\" + Path.GetFullPath(folder);
        Exception? last = null;
        for (var attempt = 0; attempt < attempts; attempt++)
        {
            if (attempt > 0) Thread.Sleep(100);
            try
            {
                FileAttributes attributes;
                try
                {
                    attributes = File.GetAttributes(target);
                }
                catch (Exception ex) when (ex is FileNotFoundException or DirectoryNotFoundException)
                {
                    return;
                }
                if ((attributes & FileAttributes.ReparsePoint) != 0)
                {
                    Directory.Delete(target);
                    return;
                }
                ClearReadOnly(target);
                Directory.Delete(target, recursive: true);
                return;
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
            {
                last = ex;
            }
        }
        throw new IOException($"The test folder {folder} could not be deleted.", last);
    }

    /// <summary>
    /// Deletes every folder in <paramref name="runs"/> whose owner file can be deleted, and
    /// leaves every folder whose owner file is held. A folder whose delete is refused, or that
    /// another run removes first, is left for a later sweep, and each is tried once.
    /// </summary>
    internal static void Sweep(string runs)
    {
        try
        {
            foreach (var folder in Directory.EnumerateDirectories(runs))
            {
                try
                {
                    var owner = Path.Combine(folder, OwnerName);
                    if (File.Exists(owner)) File.Delete(owner);
                    Delete(folder, attempts: 1);
                }
                catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
                {
                    // The owner file is held: the folder is a live run's.
                }
            }
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            // The runs folder is not there, or another run removed it while it was listed.
        }
    }

    /// <summary>
    /// Makes <paramref name="folder"/> and opens its owner file, unshared, for the caller to
    /// hold. Another run's sweep can remove the folder between the two; the folder is then
    /// made again, up to five times.
    /// </summary>
    internal static FileStream Claim(string folder)
    {
        for (var attempt = 1; ; attempt++)
        {
            Directory.CreateDirectory(folder);
            try
            {
                return new FileStream(Path.Combine(folder, OwnerName), FileMode.CreateNew, FileAccess.Write, FileShare.None);
            }
            catch (DirectoryNotFoundException) when (attempt < 5)
            {
            }
        }
    }

    private static string Start()
    {
        Sweep(Runs);
        var folder = Path.Combine(Runs, Guid.NewGuid().ToString("N"));
        // LongPathWindowsCallTests builds a 300-character path from a folder of this one's
        // length plus 162 characters, with room for at least one more.
        if (folder.Length > 130)
            throw new InvalidOperationException(
                $"The temp folder is too deep for the long-path tests: {folder} is {folder.Length} characters.");
        _owner = Claim(folder);
        DomainEnd.Then(() =>
        {
            _owner.Dispose();
            Delete(folder);
            // Not recursive, so a folder another run is still using stays.
            try { Directory.Delete(Runs); } catch (IOException) { }
            try { Directory.Delete(Path.GetDirectoryName(Runs)!); } catch (IOException) { }
        });
        return folder;
    }

    // Deletes everything in folder but the folders in left and the folders on the way to them,
    // each child through Delete, so a refusal there fails as any other does.
    private static void DeleteAround(string folder, string[] left)
    {
        if (left.Any(l => folder.Equals(l, StringComparison.OrdinalIgnoreCase)
                || folder.StartsWith(l + "\\", StringComparison.OrdinalIgnoreCase)))
            return;
        if (!Directory.Exists(folder)) return;
        foreach (var entry in new DirectoryInfo(folder).EnumerateFileSystemInfos())
        {
            var path = entry.FullName.TrimEnd('\\');
            var onTheWay = left.Any(l => l.StartsWith(path + "\\", StringComparison.OrdinalIgnoreCase)
                || l.Equals(path, StringComparison.OrdinalIgnoreCase));
            if (onTheWay) DeleteAround(path, left);
            else if ((entry.Attributes & FileAttributes.Directory) != 0) Delete(path, attempts: 5);
            else
            {
                entry.Attributes &= ~FileAttributes.ReadOnly;
                entry.Delete();
            }
        }
    }

    // A link is never followed: tests make links to folders outside the run's, and what is in
    // those is not the run's to change. Directory.Delete removes a link without entering it.
    private static void ClearReadOnly(string folder)
    {
        foreach (var entry in new DirectoryInfo(folder).EnumerateFileSystemInfos())
        {
            var attributes = entry.Attributes;
            if ((attributes & FileAttributes.ReadOnly) != 0)
                entry.Attributes = attributes & ~FileAttributes.ReadOnly;
            if ((attributes & (FileAttributes.Directory | FileAttributes.ReparsePoint)) == FileAttributes.Directory)
                ClearReadOnly(entry.FullName);
        }
    }
}
