using System.Runtime.InteropServices;
using System.Text;
using InstallerClean.Interop.Native;

namespace InstallerClean.Helpers;

/// <summary>
/// <c>System.IO.Path.GetFullPath</c> and <c>GetRelativePath</c> as .NET 10 computes them on
/// Windows (dotnet/runtime v10.0.0, <c>Path.Windows.cs</c>, <c>Path.cs</c> and
/// <c>PathHelper.Windows.cs</c>), called in their place.
///
/// The full path is Windows' own answer: GetFullPathNameW resolves the path against the
/// current folder and drive and does Win32's trimming, and GetLongPathNameW then expands any
/// short (8.3) name in it. Nothing else is checked or trimmed. .NET Framework's own
/// GetFullPath does more: it trims white space and U+00A0 off the end, throws for
/// <c>"</c>, <c>&lt;</c>, <c>&gt;</c>, <c>|</c>, a control character or a component over 255
/// characters, and expands only the short names in components of 12 characters or fewer.
/// </summary>
internal static class FullPaths
{
    private const int MaxShortPath = 260;
    private const int ArrayMaxLength = 0x7FFFFFC7;
    private const int ERROR_FILE_NOT_FOUND = 2;
    private const int ERROR_PATH_NOT_FOUND = 3;
    private const int ERROR_BAD_PATHNAME = 161;

    private const string ExtendedPathPrefix = @"\\?\";
    private const string UncExtendedPathPrefix = @"\\?\UNC\";
    private const int DevicePrefixLength = 4;
    private const int UncPrefixLength = 2;
    private const int UncExtendedPrefixLength = 8;

    // The first buffer for GetFullPathNameW, one per thread, as .NET 10's is on the stack: the
    // scan resolves a path for every file it reads.
    [ThreadStatic]
    private static char[]? t_firstBuffer;

    /// <summary>
    /// The string to hand one of the app's own Windows calls for <paramref name="path"/>.
    /// Where both <paramref name="path"/> and its full path are under 260 characters, that is
    /// <paramref name="path"/> exactly as given, so Windows reads it as it always has. Otherwise
    /// it is the full path, with <c>\\?\</c> in front where it is 260 characters or longer, so
    /// Windows takes it whether or not the long-path policy is on. Where the full path cannot be
    /// had, it is <paramref name="path"/> as given, so Windows answers as it did before.
    /// </summary>
    /// <remarks>
    /// The full path is measured as Windows itself builds it, with any short (8.3) name left
    /// as it is: expanding one would read the disk for every call, and Windows never expands one.
    ///
    /// A path under 260 that ends in a space or a period goes to Windows as given, which takes
    /// the space or period off, as it did for these calls before. The ports in Compatibility
    /// put <c>\\?\</c> in front of such a path instead, as .NET 10 does for its own calls; these
    /// are the app's own calls, not ports.
    ///
    /// The result goes to Windows only. A caller keeps its own string for anything it logs,
    /// shows or compares.
    /// </remarks>
    internal static string ForWindowsCall(string path)
    {
        if (path is null || PathText.IsEffectivelyEmpty(path) || path.IndexOf('\0') >= 0 || PathText.IsExtended(path))
            return path!;

        string fullPath;
        try
        {
            fullPath = Normalize(path, expandShortNames: false);
        }
        catch
        {
            return path;
        }

        if (path.Length < MaxShortPath && fullPath.Length < MaxShortPath)
            return path;

        return fullPath.Length >= MaxShortPath ? PathText.EnsureExtendedPrefix(fullPath) : fullPath;
    }

    /// <summary>
    /// The full path of <paramref name="path"/>. A path beginning <c>\\?\</c> or <c>\??\</c>
    /// is returned as it is. Null is an <see cref="ArgumentNullException"/>; an empty path,
    /// one of spaces alone, or one holding a null character is an
    /// <see cref="ArgumentException"/> naming <c>path</c>; a failure in Windows is the
    /// exception <see cref="Win32Marshal"/> gives for its error, naming the path.
    /// </summary>
    public static string GetFullPath(string path)
    {
        if (path is null) throw new ArgumentNullException(nameof(path));

        if (PathText.IsEffectivelyEmpty(path))
            throw new ArgumentException("The path is empty.", nameof(path));

        // Windows reads a null character as the end of the string.
        if (path.IndexOf('\0') >= 0)
            throw new ArgumentException("Null character in path.", nameof(path));

        // Windows does not normalise a \\?\ path, and GetFullPathNameW does not root one correctly.
        if (PathText.IsExtended(path))
            return path;

        return Normalize(path);
    }

    /// <summary>
    /// <paramref name="path"/> relative to the folder <paramref name="relativeTo"/>, both made
    /// full paths first and compared ignoring case: <c>.</c> where they are the same folder,
    /// and <paramref name="path"/>'s full path where the two have different roots. A trailing
    /// separator on <paramref name="path"/> is kept. The arguments are checked as
    /// <see cref="GetFullPath"/> checks its own, <paramref name="relativeTo"/> first.
    /// </summary>
    public static string GetRelativePath(string relativeTo, string path)
    {
        if (relativeTo is null) throw new ArgumentNullException(nameof(relativeTo));
        if (path is null) throw new ArgumentNullException(nameof(path));

        if (PathText.IsEffectivelyEmpty(relativeTo))
            throw new ArgumentException("The path is empty.", nameof(relativeTo));
        if (PathText.IsEffectivelyEmpty(path))
            throw new ArgumentException("The path is empty.", nameof(path));

        relativeTo = GetFullPath(relativeTo);
        path = GetFullPath(path);

        if (!AreRootsEqual(relativeTo, path))
            return path;

        var commonLength = GetCommonPathLength(relativeTo, path);

        if (commonLength == 0)
            return path;

        // A trailing separator does not count in the comparison.
        var relativeToLength = relativeTo.Length;
        if (EndsInDirectorySeparator(relativeTo))
            relativeToLength--;

        var pathEndsInSeparator = EndsInDirectorySeparator(path);
        var pathLength = path.Length;
        if (pathEndsInSeparator)
            pathLength--;

        if (relativeToLength == pathLength && commonLength >= relativeToLength)
            return ".";

        // C:\Foo and C:\Bar give ..\Bar; C:\Foo and C:\Foo\Bar give Bar; C:\Foo\Bar and
        // C:\Bar\Bar give ..\..\Bar\Bar; C:\Foo\Foo and C:\Foo\Bar give ..\Bar.
        var builder = new StringBuilder(Math.Max(relativeTo.Length, path.Length));

        if (commonLength < relativeToLength)
        {
            builder.Append("..");

            for (var i = commonLength + 1; i < relativeToLength; i++)
            {
                if (PathText.IsDirectorySeparator(relativeTo[i]))
                    builder.Append('\\').Append("..");
            }
        }
        else if (PathText.IsDirectorySeparator(path[commonLength]))
        {
            commonLength++;
        }

        var differenceLength = pathLength - commonLength;
        if (pathEndsInSeparator)
            differenceLength++;

        if (differenceLength > 0)
        {
            if (builder.Length > 0)
                builder.Append('\\');

            builder.Append(path, commonLength, differenceLength);
        }

        return builder.ToString();
    }

    // GetFullPathNameW's answer, then, where expandShortNames is set, the short names in it
    // expanded where it holds a '~'. The string given is returned where the answer is the same text.
    private static string Normalize(string path, bool expandShortNames = true)
    {
        var buffer = t_firstBuffer ??= new char[MaxShortPath];
        uint result;
        while ((result = Kernel32.GetFullPathName(path, (uint)buffer.Length, buffer, IntPtr.Zero)) > buffer.Length)
            buffer = new char[GrownCapacity(buffer.Length, checked((int)result))];

        if (result == 0)
        {
            var errorCode = Marshal.GetLastWin32Error();
            if (errorCode == 0)
                errorCode = ERROR_BAD_PATHNAME;
            throw Win32Marshal.GetExceptionForWin32Error(errorCode, path);
        }

        var length = (int)result;
        if (expandShortNames && Array.IndexOf(buffer, '~', 0, length) >= 0)
            return TryExpandShortFileName(buffer, length, path);

        return SameText(buffer, 0, length, path) ? path : new string(buffer, 0, length);
    }

    // The full path in output[0..outputLength) with every short name GetLongPathNameW can
    // expand expanded. Where the path does not wholly exist, the folders above it are tried
    // in turn, from the end, and the part that does not exist is put back after the expanded
    // part. Any other failure, or reaching the root, leaves the path as it is. The extended
    // prefix is put on for the call so the path can pass 260 characters, and taken off again.
    private static string TryExpandShortFileName(char[] output, int outputLength, string originalPath)
    {
        var fullPath = new string(output, 0, outputLength);
        var rootLength = PathText.GetRootLength(fullPath);
        var isDevice = PathText.IsDevice(fullPath);

        char[] input;
        var isDosUnc = false;
        var rootDifference = 0;
        var wasDotDevice = false;

        if (isDevice)
        {
            input = fullPath.ToCharArray();

            if (output[2] == '.')
            {
                wasDotDevice = true;
                input[2] = '?';
            }
        }
        else
        {
            isDosUnc = outputLength > 1 && output[0] == '\\' && output[1] == '\\';
            if (isDosUnc)
            {
                input = (UncExtendedPathPrefix + fullPath.Substring(UncPrefixLength)).ToCharArray();
                rootDifference = UncExtendedPrefixLength - UncPrefixLength;
            }
            else
            {
                input = (ExtendedPathPrefix + fullPath).ToCharArray();
                rootDifference = DevicePrefixLength;
            }
        }

        rootLength += rootDifference;
        var inputLength = input.Length;

        var success = false;
        var foundIndex = inputLength - 1;
        var resultLength = 0;

        while (!success)
        {
            // A null character written into input below ends the path Windows reads there.
            var result = Kernel32.GetLongPathName(UpToNullCharacter(input), output, (uint)output.Length);

            if (input[foundIndex] == '\0')
                input[foundIndex] = '\\';

            if (result == 0)
            {
                var error = Marshal.GetLastWin32Error();
                if (error != ERROR_FILE_NOT_FOUND && error != ERROR_PATH_NOT_FOUND)
                    break;

                foundIndex--;

                for (; foundIndex > rootLength && input[foundIndex] != '\\'; foundIndex--)
                {
                }

                if (foundIndex == rootLength)
                    break;

                input[foundIndex] = '\0';
            }
            else if (result > output.Length)
            {
                // The null character above is already gone, so the next call asks about
                // the whole path and the walk goes on from the folder above the one whose
                // answer did not fit. Where that happens depends on the buffer's size, so
                // the size follows .NET 10's at every step.
                output = new char[GrownCapacity(output.Length, checked((int)result))];
            }
            else
            {
                success = true;
                resultLength = (int)result;
                if (foundIndex < inputLength - 1)
                {
                    // Only part of the path exists: the rest goes back after it as it was.
                    var tailLength = inputLength - foundIndex;
                    var joined = new char[Math.Max(output.Length, resultLength + tailLength)];
                    Array.Copy(output, joined, resultLength);
                    Array.Copy(input, foundIndex, joined, resultLength, tailLength);
                    output = joined;
                    resultLength += tailLength;
                }
            }
        }

        var chosen = success ? output : input;
        var chosenLength = success ? resultLength : inputLength;

        if (wasDotDevice)
            chosen[2] = '.';

        // \\?\UNC\server\share becomes \\server\share once the prefix's first six
        // characters are taken off.
        if (isDosUnc)
            chosen[UncExtendedPrefixLength - UncPrefixLength] = '\\';

        return SameText(chosen, rootDifference, chosenLength - rootDifference, originalPath)
            ? originalPath
            : new string(chosen, rootDifference, chosenLength - rootDifference);
    }

    // The size .NET 10's buffer takes when it must hold `required` characters: twice its
    // size or `required`, whichever is more, as ValueStringBuilder grows, then the length
    // of the array ArrayPool's shared pool hands out for that, a power of two from 16.
    private static int GrownCapacity(int capacity, int required)
    {
        var wanted = (int)Math.Max((uint)required, Math.Min((uint)capacity * 2, (uint)ArrayMaxLength));
        if (wanted > 1 << 30)
            return wanted;

        var rented = 16;
        while (rented < wanted)
            rented <<= 1;
        return rented;
    }

    private static string UpToNullCharacter(char[] text)
    {
        var end = Array.IndexOf(text, '\0');
        return end < 0 ? new string(text) : new string(text, 0, end);
    }

    private static bool SameText(char[] text, int start, int length, string value)
    {
        if (length != value.Length)
            return false;

        for (var i = 0; i < length; i++)
        {
            if (text[start + i] != value[i])
                return false;
        }

        return true;
    }

    private static bool EndsInDirectorySeparator(string path) =>
        path.Length > 0 && PathText.IsDirectorySeparator(path[path.Length - 1]);

    private static bool AreRootsEqual(string first, string second)
    {
        var firstRootLength = PathText.GetRootLength(first);
        var secondRootLength = PathText.GetRootLength(second);

        return firstRootLength == secondRootLength
            && string.Compare(first, 0, second, 0, firstRootLength, StringComparison.OrdinalIgnoreCase) == 0;
    }

    // The length the two paths share, ignoring case, cut back to a separator unless one
    // path ends where the other has a separator next.
    private static int GetCommonPathLength(string first, string second)
    {
        var commonChars = EqualStartingCharacterCount(first, second);

        if (commonChars == 0)
            return commonChars;

        if (commonChars == first.Length
            && (commonChars == second.Length || PathText.IsDirectorySeparator(second[commonChars])))
            return commonChars;

        if (commonChars == second.Length && PathText.IsDirectorySeparator(first[commonChars]))
            return commonChars;

        // C:\Foodie and C:\Foobar share C:\.
        while (commonChars > 0 && !PathText.IsDirectorySeparator(first[commonChars - 1]))
            commonChars--;

        return commonChars;
    }

    private static int EqualStartingCharacterCount(string first, string second)
    {
        if (string.IsNullOrEmpty(first) || string.IsNullOrEmpty(second)) return 0;

        var commonChars = 0;
        while (commonChars < first.Length && commonChars < second.Length
            && (first[commonChars] == second[commonChars]
                || char.ToUpperInvariant(first[commonChars]) == char.ToUpperInvariant(second[commonChars])))
        {
            commonChars++;
        }

        return commonChars;
    }
}
