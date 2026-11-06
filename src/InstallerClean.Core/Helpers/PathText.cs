using System.Diagnostics.CodeAnalysis;

namespace InstallerClean.Helpers;

/// <summary>
/// The text-only members of <c>System.IO.Path</c> the app calls, computed as .NET 10
/// computes them on Windows (dotnet/runtime v10.0.0, <c>Path.cs</c>, <c>Path.Windows.cs</c>,
/// <c>PathInternal.cs</c> and <c>PathInternal.Windows.cs</c>), and called in their place.
///
/// Each one reads only the string it is given: no call to Windows, and none to
/// <c>System.IO.Path</c>, so no AppContext switch and no framework version changes an
/// answer. .NET Framework's own members differ from .NET 10's on text a path can hold: they
/// throw <see cref="ArgumentException"/> for <c>"</c>, <c>&lt;</c>, <c>&gt;</c>, <c>|</c>
/// and the control characters, take a colon anywhere for a separator, so a stream name
/// such as <c>a.msp:stream</c> loses its file name, count any character before a colon as
/// a drive, and throw for an empty or blank string where .NET 10 answers null. Here none
/// of that happens.
///
/// Both separators are accepted, a drive is a letter from A to Z, either case, followed by
/// a colon, and a path beginning <c>\\?\</c>, <c>\\.\</c> or <c>\??\</c> is a device path
/// whose root runs to the separator after the prefix's next component, except
/// <c>\\?\UNC\server\share</c>, whose root is the share as <c>\\server\share</c>'s is.
/// </summary>
internal static class PathText
{
    private const char DirectorySeparatorChar = '\\';
    private const char AltDirectorySeparatorChar = '/';
    private const char VolumeSeparatorChar = ':';
    private const string DirectorySeparatorString = "\\";
    private static readonly char[] DirectorySeparators = [DirectorySeparatorChar, AltDirectorySeparatorChar];

    private const int DevicePrefixLength = 4;
    private const int UncPrefixLength = 2;
    private const int UncExtendedPrefixLength = 8;

    /// <summary>
    /// The folder part of <paramref name="path"/>, each run of separators in it made one
    /// backslash, except a run at its start, which keeps at most two: null where the path
    /// is null, empty, all spaces or a root alone, such as <c>\</c>, <c>C:</c>, <c>C:\</c> or
    /// <c>\\server\share</c>.
    /// </summary>
    public static string? GetDirectoryName(string? path)
    {
        if (path is null || IsEffectivelyEmpty(path))
            return null;

        var end = GetDirectoryNameOffset(path);
        return end >= 0 ? NormalizeDirectorySeparators(path.Substring(0, end)) : null;
    }

    /// <summary>
    /// The extension of <paramref name="path"/>, its period included: empty where the last
    /// part has no period or ends in one, and null where the path is null.
    /// </summary>
    [return: NotNullIfNotNull(nameof(path))]
    public static string? GetExtension(string? path)
    {
        if (path is null)
            return null;

        var length = path.Length;
        for (var i = length - 1; i >= 0; i--)
        {
            var ch = path[i];
            if (ch == '.')
                return i != length - 1 ? path.Substring(i) : string.Empty;
            if (IsDirectorySeparator(ch))
                break;
        }
        return string.Empty;
    }

    /// <summary>
    /// The text after the last separator in <paramref name="path"/>, or after its root where
    /// the root runs past that separator: <c>C:name</c> gives <c>name</c>, and a root alone,
    /// such as <c>\\server\share</c>, gives an empty string. Null where the path is null. A
    /// colon after the root is part of the name.
    /// </summary>
    [return: NotNullIfNotNull(nameof(path))]
    public static string? GetFileName(string? path)
    {
        if (path is null)
            return null;

        var start = FileNameStart(path);
        return start == 0 ? path : path.Substring(start);
    }

    /// <summary>
    /// <see cref="GetFileName"/>'s answer up to its last period, or whole where it has none;
    /// null where the path is null.
    /// </summary>
    [return: NotNullIfNotNull(nameof(path))]
    public static string? GetFileNameWithoutExtension(string? path)
    {
        if (path is null)
            return null;

        var start = FileNameStart(path);
        var lastPeriod = path.LastIndexOf('.');
        var end = lastPeriod >= start ? lastPeriod : path.Length;
        return start == 0 && end == path.Length ? path : path.Substring(start, end - start);
    }

    /// <summary>
    /// Whether <paramref name="path"/> starts at a root: a separator, or a drive letter and
    /// a colon. False for null.
    /// </summary>
    public static bool IsPathRooted([NotNullWhen(true)] string? path) =>
        path is not null
        && ((path.Length >= 1 && IsDirectorySeparator(path[0]))
            || (path.Length >= 2 && IsValidDriveChar(path[0]) && path[1] == VolumeSeparatorChar));

    /// <summary>
    /// Whether <paramref name="path"/> names one place whatever the current folder and drive:
    /// a drive letter, a colon and a separator, two separators, or a separator and a
    /// <c>?</c>. A null path is an <see cref="ArgumentNullException"/>.
    /// </summary>
    public static bool IsPathFullyQualified(string path)
    {
        if (path is null) throw new ArgumentNullException(nameof(path));
        return !IsPartiallyQualified(path);
    }

    /// <summary>
    /// <paramref name="path"/> without one separator at its end, unless the path is a root
    /// alone.
    /// </summary>
    public static string TrimEndingDirectorySeparator(string path) =>
        !string.IsNullOrEmpty(path) && IsDirectorySeparator(path[path.Length - 1]) && GetRootLength(path) != path.Length
            ? path.Substring(0, path.Length - 1)
            : path;

    /// <summary>
    /// <paramref name="path2"/> joined to <paramref name="path1"/> with one backslash between
    /// them where neither has a separator at the join. An empty part is left out, and a rooted
    /// <paramref name="path2"/> is the answer alone. A null part is an
    /// <see cref="ArgumentNullException"/> naming it.
    /// </summary>
    public static string Combine(string path1, string path2)
    {
        if (path1 is null) throw new ArgumentNullException(nameof(path1));
        if (path2 is null) throw new ArgumentNullException(nameof(path2));

        return CombineInternal(path1, path2);
    }

    /// <summary>
    /// The three parts joined as <see cref="Combine(string, string)"/> joins two, starting
    /// again from the last rooted part.
    /// </summary>
    public static string Combine(string path1, string path2, string path3)
    {
        if (path1 is null) throw new ArgumentNullException(nameof(path1));
        if (path2 is null) throw new ArgumentNullException(nameof(path2));
        if (path3 is null) throw new ArgumentNullException(nameof(path3));

        return CombineInternal(path1, path2, path3);
    }

    /// <summary>
    /// The four parts joined as <see cref="Combine(string, string)"/> joins two, starting
    /// again from the last rooted part.
    /// </summary>
    public static string Combine(string path1, string path2, string path3, string path4)
    {
        if (path1 is null) throw new ArgumentNullException(nameof(path1));
        if (path2 is null) throw new ArgumentNullException(nameof(path2));
        if (path3 is null) throw new ArgumentNullException(nameof(path3));
        if (path4 is null) throw new ArgumentNullException(nameof(path4));

        return CombineInternal(path1, path2, path3, path4);
    }

    /// <summary>
    /// Every part joined as <see cref="Combine(string, string)"/> joins two, starting again
    /// from the last rooted part. A null array, or a null part in it, is an
    /// <see cref="ArgumentNullException"/> naming <paramref name="paths"/>.
    /// </summary>
    public static string Combine(params string[] paths)
    {
        if (paths is null) throw new ArgumentNullException(nameof(paths));

        var firstComponent = 0;
        for (var i = 0; i < paths.Length; i++)
        {
            if (paths[i] is null) throw new ArgumentNullException(nameof(paths));
            if (paths[i].Length != 0 && IsPathRooted(paths[i]))
                firstComponent = i;
        }

        var builder = new System.Text.StringBuilder();
        for (var i = firstComponent; i < paths.Length; i++)
        {
            if (paths[i].Length == 0)
                continue;

            if (builder.Length != 0 && !IsDirectorySeparator(builder[builder.Length - 1]))
                builder.Append(DirectorySeparatorChar);

            builder.Append(paths[i]);
        }

        return builder.ToString();
    }

    private static string CombineInternal(string first, string second)
    {
        if (first.Length == 0) return second;
        if (second.Length == 0) return first;
        if (IsPathRooted(second)) return second;

        return NeedsSeparator(first, second)
            ? string.Concat(first, DirectorySeparatorString, second)
            : string.Concat(first, second);
    }

    private static string CombineInternal(string first, string second, string third)
    {
        if (first.Length == 0) return CombineInternal(second, third);
        if (second.Length == 0) return CombineInternal(first, third);
        if (third.Length == 0) return CombineInternal(first, second);

        if (IsPathRooted(third)) return third;
        if (IsPathRooted(second)) return CombineInternal(second, third);

        return string.Concat(
            first,
            NeedsSeparator(first, second) ? DirectorySeparatorString : string.Empty,
            second,
            NeedsSeparator(second, third) ? DirectorySeparatorString : string.Empty,
            third);
    }

    private static string CombineInternal(string first, string second, string third, string fourth)
    {
        if (first.Length == 0) return CombineInternal(second, third, fourth);
        if (second.Length == 0) return CombineInternal(first, third, fourth);
        if (third.Length == 0) return CombineInternal(first, second, fourth);
        if (fourth.Length == 0) return CombineInternal(first, second, third);

        if (IsPathRooted(fourth)) return fourth;
        if (IsPathRooted(third)) return CombineInternal(third, fourth);
        if (IsPathRooted(second)) return CombineInternal(second, third, fourth);

        return string.Concat(
            first,
            NeedsSeparator(first, second) ? DirectorySeparatorString : string.Empty,
            second,
            NeedsSeparator(second, third) ? DirectorySeparatorString : string.Empty,
            third,
            NeedsSeparator(third, fourth) ? DirectorySeparatorString : string.Empty,
            fourth);
    }

    // Only a separator counts at the join: a first part ending in a colon, "C:", still
    // takes a backslash after it.
    private static bool NeedsSeparator(string first, string second) =>
        !IsDirectorySeparator(first[first.Length - 1]) && !IsDirectorySeparator(second[0]);

    // Where the file name starts: after the last separator, or after the root where that is
    // further on. A root is read only where the path is not all spaces.
    private static int FileNameStart(string path)
    {
        var root = IsEffectivelyEmpty(path) ? 0 : GetRootLength(path);
        var i = path.LastIndexOfAny(DirectorySeparators);
        return i < root ? root : i + 1;
    }

    private static int GetDirectoryNameOffset(string path)
    {
        var rootLength = GetRootLength(path);
        var end = path.Length;
        if (end <= rootLength)
            return -1;

        while (end > rootLength && !IsDirectorySeparator(path[--end])) { }

        // Separators running into the one found, as in C:\a\\b, go with it.
        while (end > rootLength && IsDirectorySeparator(path[end - 1]))
            end--;

        return end;
    }

    // The length of the root: \\server\share and \\?\UNC\server\share up to, not including,
    // the separator after the share; a device path such as \\?\C:\ up to and including the
    // separator after its first component; \ alone; C: and C:\; otherwise none.
    private static int GetRootLength(string path)
    {
        var pathLength = path.Length;
        var i = 0;

        var deviceSyntax = IsDevice(path);
        var deviceUnc = deviceSyntax && IsDeviceUnc(path);

        if ((!deviceSyntax || deviceUnc) && pathLength > 0 && IsDirectorySeparator(path[0]))
        {
            if (deviceUnc || (pathLength > 1 && IsDirectorySeparator(path[1])))
            {
                i = deviceUnc ? UncExtendedPrefixLength : UncPrefixLength;

                // The server and the share: up to the second separator after the prefix.
                var n = 2;
                while (i < pathLength && (!IsDirectorySeparator(path[i]) || --n > 0))
                    i++;
            }
            else
            {
                i = 1;
            }
        }
        else if (deviceSyntax)
        {
            i = DevicePrefixLength;
            while (i < pathLength && !IsDirectorySeparator(path[i]))
                i++;

            if (i < pathLength && i > DevicePrefixLength && IsDirectorySeparator(path[i]))
                i++;
        }
        else if (pathLength >= 2 && path[1] == VolumeSeparatorChar && IsValidDriveChar(path[0]))
        {
            i = 2;
            if (pathLength > 2 && IsDirectorySeparator(path[2]))
                i++;
        }

        return i;
    }

    private static bool IsPartiallyQualified(string path)
    {
        if (path.Length < 2)
            return true;

        if (IsDirectorySeparator(path[0]))
            return !(path[1] == '?' || IsDirectorySeparator(path[1]));

        return !(path.Length >= 3
            && path[1] == VolumeSeparatorChar
            && IsDirectorySeparator(path[2])
            && IsValidDriveChar(path[0]));
    }

    // \\?\ and \\.\ with either separator in place of each backslash, and \??\ with
    // backslashes alone.
    private static bool IsDevice(string path) =>
        IsExtended(path)
        || (path.Length >= DevicePrefixLength
            && IsDirectorySeparator(path[0])
            && IsDirectorySeparator(path[1])
            && (path[2] == '.' || path[2] == '?')
            && IsDirectorySeparator(path[3]));

    private static bool IsDeviceUnc(string path) =>
        path.Length >= UncExtendedPrefixLength
        && IsDevice(path)
        && IsDirectorySeparator(path[7])
        && path[4] == 'U'
        && path[5] == 'N'
        && path[6] == 'C';

    // \\?\ and \??\, backslashes only.
    private static bool IsExtended(string path) =>
        path.Length >= DevicePrefixLength
        && path[0] == '\\'
        && (path[1] == '\\' || path[1] == '?')
        && path[2] == '?'
        && path[3] == '\\';

    private static bool IsDirectorySeparator(char c) => c == DirectorySeparatorChar || c == AltDirectorySeparatorChar;

    private static bool IsValidDriveChar(char value) => (uint)((value | 0x20) - 'a') <= (uint)('z' - 'a');

    // Empty, or spaces alone. A tab or any other white space is text.
    private static bool IsEffectivelyEmpty(string path)
    {
        foreach (var c in path)
        {
            if (c != ' ')
                return false;
        }
        return true;
    }

    // Each run of separators becomes one backslash, except at the very start, where the first
    // separator is kept and the run after it becomes one more backslash, so \\server keeps
    // its two.
    private static string NormalizeDirectorySeparators(string path)
    {
        if (path.Length == 0)
            return path;

        var normalized = true;
        for (var i = 0; i < path.Length; i++)
        {
            var current = path[i];
            if (IsDirectorySeparator(current)
                && (current != DirectorySeparatorChar
                    || (i > 0 && i + 1 < path.Length && IsDirectorySeparator(path[i + 1]))))
            {
                normalized = false;
                break;
            }
        }

        if (normalized)
            return path;

        var builder = new System.Text.StringBuilder(path.Length);
        var start = 0;
        if (IsDirectorySeparator(path[start]))
        {
            start++;
            builder.Append(DirectorySeparatorChar);
        }

        for (var i = start; i < path.Length; i++)
        {
            var current = path[i];
            if (IsDirectorySeparator(current))
            {
                if (i + 1 < path.Length && IsDirectorySeparator(path[i + 1]))
                    continue;

                current = DirectorySeparatorChar;
            }

            builder.Append(current);
        }

        return builder.ToString();
    }
}
