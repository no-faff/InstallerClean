using System.IO.Abstractions;
using InstallerClean.Helpers;

namespace InstallerClean.Services;

/// <summary>
/// The folders a Move makes on the way to the destination it was given. Creating the
/// destination also creates every missing folder above it, so a Move that is then
/// abandoned has more than one folder to take away. The folders to take away are those
/// <see cref="Create"/> lists, and each is removed only while it is empty.
/// </summary>
internal static class CreatedFolders
{
    /// <summary>
    /// The folders on the way to <paramref name="destination"/> that look absent, the
    /// destination first and the top-most last, each spelled as its full path so a
    /// <c>..</c> in the destination names the folders a create would make. Empty where the
    /// destination is there already. A drive root is never listed. Whether a listed folder
    /// was made by the Move is for <see cref="Create"/> to say, from the create.
    /// </summary>
    internal static IReadOnlyList<string> MissingOnPath(IFileSystem fileSystem, string destination)
    {
        var missing = new List<string>();
        string folder;
        try
        {
            folder = PathText.TrimEndingDirectorySeparator(FullPaths.GetFullPath(destination));
        }
        catch
        {
            // A destination that cannot be spelled as a full path cannot be created either.
            return missing;
        }
        while (!string.IsNullOrEmpty(folder) && !fileSystem.Directory.Exists(folder))
        {
            var parent = PathText.GetDirectoryName(folder);
            if (string.IsNullOrEmpty(parent)) break;
            missing.Add(folder);
            folder = parent;
        }
        return missing;
    }

    /// <summary>
    /// Creates <paramref name="destination"/> and every missing folder above it, and adds
    /// to <paramref name="folders"/> the folders <paramref name="guard"/> said it made, in
    /// the order <see cref="RemoveWhileEmpty"/> takes them: the destination first, the
    /// top-most last. Nothing is listed that a create did not say it made.
    /// <para>
    /// Each missing folder is created on its own, top-most first. One the guard says was
    /// already there was not made by the Move, so it is not listed, and neither is any
    /// folder made above it, which holds it. One the guard says failed ends the creating
    /// and is not listed either, nor is any folder below it; the folders made above it
    /// stay listed. Where nothing was missing, or the creating ended on a failure, the
    /// destination is then created through <paramref name="fileSystem"/>, which makes
    /// whatever is left or throws the failure, with the folders already made listed.
    /// </para>
    /// </summary>
    internal static void Create(
        IFileSystem fileSystem, IDestinationGuard guard, string destination, List<string> folders)
    {
        var candidates = MissingOnPath(fileSystem, destination);
        var made = new List<string>();
        var ended = false;
        try
        {
            for (var level = candidates.Count - 1; level >= 0; level--)
            {
                var created = guard.TryCreateFolder(candidates[level]);
                if (created == FolderCreation.Made)
                {
                    made.Add(candidates[level]);
                }
                else if (created == FolderCreation.AlreadyThere)
                {
                    made.Clear();
                }
                else
                {
                    ended = true;
                    break;
                }
            }
        }
        finally
        {
            made.Reverse();
            folders.AddRange(made);
        }

        if (candidates.Count == 0 || ended)
            fileSystem.Directory.CreateDirectory(destination);
    }

    /// <summary>
    /// Removes <paramref name="folders"/> in the order given, each through
    /// <paramref name="guard"/> and so only where it is empty. The list is the one
    /// <see cref="Create"/> leaves: each folder's parent is the next in it, and the
    /// parent of the last is a folder that was there before the Move, which the guard is
    /// told as the existing ancestor. A folder that is no longer there is passed over;
    /// the first that is there and will not go ends the removal, so a folder holding
    /// anything keeps every folder above it. A failure of the removal itself ends it too
    /// and is not thrown, so it never replaces the error a caller is about to report.
    /// </summary>
    internal static void RemoveWhileEmpty(
        IFileSystem fileSystem, IDestinationGuard guard, IReadOnlyList<string> folders)
    {
        try
        {
            if (folders.Count == 0) return;
            var existingAncestor = PathText.GetDirectoryName(folders[^1]);
            if (string.IsNullOrEmpty(existingAncestor)) return;

            foreach (var folder in folders)
            {
                if (guard.TryRemoveEmptyFolder(folder, existingAncestor)) continue;
                if (fileSystem.Directory.Exists(folder)) return;
            }
        }
        catch
        {
        }
    }
}
