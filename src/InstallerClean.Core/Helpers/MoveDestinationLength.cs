namespace InstallerClean.Helpers;

/// <summary>
/// Whether a Move would put a file at a path of 260 characters or more. Many programs on
/// Windows, File Explorer among them, take paths shorter than MAX_PATH only, so a file
/// moved to a longer path could not be opened or put back through them. In Core so the
/// window, the command line and the move service give the same answer.
/// </summary>
/// <remarks>
/// The length is that of the path the move writes: the destination's full path, joined
/// to the file's name as <see cref="PathText.Combine(string, string)"/> joins them. A
/// <c>..</c> in a destination without <c>\\?\</c> is resolved first, as Windows resolves
/// it, and a <c>\\?\</c> or <c>\\.\</c> in front is left out of the count, since a
/// program is given the path without it.
/// </remarks>
internal static class MoveDestinationLength
{
    /// <summary>The length at which a path is refused: MAX_PATH, which counts the terminating NUL.</summary>
    internal const int Limit = 260;

    /// <summary>
    /// Whether the Move would write a path of <see cref="Limit"/> characters or more.
    /// <paramref name="filePaths"/> are the files to move; the longest name among them
    /// decides. With no files named, the measure is a name of one character, so a destination
    /// is refused where no file at all would fit in it, which is a full path of 258 characters
    /// or more (a separator and one character take it to 260).
    /// </summary>
    /// <remarks>
    /// A destination whose full path cannot be made answers false and is left to the checks
    /// after this one, which refuse it.
    /// </remarks>
    internal static bool TooLong(string destination, IEnumerable<string>? filePaths = null)
    {
        string folder;
        try
        {
            folder = PathText.TrimEndingDirectorySeparator(WithoutDevicePrefix(FullPaths.GetFullPath(destination)));
        }
        catch
        {
            return false;
        }

        string? longest = null;
        if (filePaths is not null)
        {
            foreach (var filePath in filePaths)
            {
                var name = PathText.GetFileName(filePath);
                if (string.IsNullOrEmpty(name)) continue;
                if (longest is null || name.Length > longest.Length) longest = name;
            }
        }

        return PathText.Combine(folder, longest ?? "x").Length >= Limit;
    }

    /// <summary>
    /// <paramref name="path"/> as a program is given it: <c>\\?\C:\a</c>, <c>\\.\C:\a</c> and
    /// <c>\??\C:\a</c> as <c>C:\a</c>, and <c>\\?\UNC\server\share</c> as
    /// <c>\\server\share</c>, with <c>UNC</c> in any case. Any other
    /// path is returned as it is.
    /// </summary>
    private static string WithoutDevicePrefix(string path)
    {
        if (!path.StartsWith(@"\\?\", StringComparison.Ordinal)
            && !path.StartsWith(@"\\.\", StringComparison.Ordinal)
            && !path.StartsWith(@"\??\", StringComparison.Ordinal))
            return path;

        var rest = path.Substring(4);
        if (rest.StartsWith(@"UNC\", StringComparison.OrdinalIgnoreCase))
            return @"\\" + rest.Substring(4);
        return rest.Length >= 2 && rest[1] == ':' && char.IsLetter(rest[0]) ? rest : path;
    }
}
