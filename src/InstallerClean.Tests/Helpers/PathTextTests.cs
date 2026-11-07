using InstallerClean.Helpers;

namespace InstallerClean.Tests.Helpers;

/// <summary>
/// <see cref="PathText"/> answers as .NET 10's <c>System.IO.Path</c> answers on Windows. Every
/// expected value here is .NET 10's own answer for the input. Among the inputs are the
/// shapes on which .NET Framework's members answer otherwise: a colon after the root, a
/// character other than a letter before a colon, a first part ending in a colon, an empty
/// or blank string, and the characters .NET Framework refuses in a path.
/// </summary>
public class PathTextTests
{
    [Theory]
    // A first part ending in a colon still takes a backslash.
    [InlineData("C:", "x", @"C:\x")]
    // Only a letter before a colon is a drive, so "1:b" is joined like any other name.
    [InlineData(@"C:\a", "1:b", @"C:\a\1:b")]
    [InlineData(@"C:\a", "D:b", "D:b")]
    [InlineData(@"C:\a", @"\b", @"\b")]
    [InlineData(@"C:\a\", "b", @"C:\a\b")]
    [InlineData(@"C:\a", "b|c", @"C:\a\b|c")]
    [InlineData(@"C:\a", "b\t", "C:\\a\\b\t")]
    [InlineData("", "b", "b")]
    [InlineData("a", "", "a")]
    public void Two_parts_are_joined_as_on_NET_10(string first, string second, string expected)
    {
        Assert.Equal(expected, PathText.Combine(first, second));
    }

    [Theory]
    [InlineData(@"C:\a", "", "b", @"C:\a\b")]
    [InlineData(@"C:\a", @"D:\b", "c", @"D:\b\c")]
    [InlineData("C:", "a", "b", @"C:\a\b")]
    [InlineData(@"C:\a", "1:b", "c", @"C:\a\1:b\c")]
    public void Three_parts_are_joined_as_on_NET_10(string first, string second, string third, string expected)
    {
        Assert.Equal(expected, PathText.Combine(first, second, third));
    }

    [Fact]
    public void Four_parts_and_any_number_of_parts_are_joined_as_on_NET_10()
    {
        Assert.Equal(@"C:\a\1:b\c", PathText.Combine("C:", "a", "1:b", "c"));
        Assert.Equal(@"D:\b\c", PathText.Combine(@"C:\a", "", @"D:\b", "c"));
        Assert.Equal(@"C:\a\b|c", PathText.Combine(new[] { "C:", "", "a", "b|c" }));
        Assert.Equal(@"\d\e", PathText.Combine(new[] { @"C:\a", "b", @"\d", "e" }));
        Assert.Equal("", PathText.Combine(new string[0]));
        Assert.Equal("paths", Assert.Throws<ArgumentNullException>(() => PathText.Combine((string[])null!)).ParamName);
        Assert.Equal("paths", Assert.Throws<ArgumentNullException>(() => PathText.Combine(new[] { "a", null! })).ParamName);
        Assert.Equal("path4", Assert.Throws<ArgumentNullException>(() => PathText.Combine("a", "b", "c", null!)).ParamName);
    }

    [Fact]
    public void A_null_part_is_refused_by_name()
    {
        Assert.Equal("path1", Assert.Throws<ArgumentNullException>(() => PathText.Combine(null!, "b")).ParamName);
        Assert.Equal("path2", Assert.Throws<ArgumentNullException>(() => PathText.Combine("a", null!)).ParamName);
        Assert.Equal("path3", Assert.Throws<ArgumentNullException>(() => PathText.Combine("a", "b", null!)).ParamName);
    }

    [Theory]
    [InlineData("1:x", false)]
    [InlineData("+:x", false)]
    [InlineData("c:x", true)]
    [InlineData("/x", true)]
    [InlineData("", false)]
    [InlineData(null, false)]
    public void Only_a_letter_and_a_colon_or_a_separator_is_a_root(string? path, bool expected)
    {
        Assert.Equal(expected, PathText.IsPathRooted(path));
    }

    [Fact]
    public void A_colon_after_the_root_is_part_of_the_name()
    {
        const string stream = @"C:\Windows\Installer\a.msp:stream";

        Assert.Equal(".msp:stream", PathText.GetExtension(stream));
        Assert.Equal("a.msp:stream", PathText.GetFileName(stream));
        Assert.Equal("a", PathText.GetFileNameWithoutExtension(stream));
        Assert.Equal(@"C:\Windows\Installer", PathText.GetDirectoryName(stream));
        Assert.Equal("Foo", PathText.GetFileName("C:Foo"));
    }

    [Theory]
    [InlineData(@"C:\i\a.", "")]
    [InlineData(@"C:\i.d\a", "")]
    [InlineData(@"C:\i\a.msi", ".msi")]
    [InlineData(null, null)]
    public void The_extension_is_read_from_the_last_part_alone(string? path, string? expected)
    {
        Assert.Equal(expected, PathText.GetExtension(path));
    }

    [Theory]
    [InlineData("", null)]
    [InlineData("  ", null)]
    // Only spaces count as blank: a tab is a name with no folder.
    [InlineData("\t", "")]
    [InlineData(null, null)]
    [InlineData(@"C:\", null)]
    [InlineData(@"C:\a", @"C:\")]
    [InlineData(@"C:/a//b\\c", @"C:\a\b")]
    [InlineData(@"\\server\share", null)]
    [InlineData(@"\\server\share\x", @"\\server\share")]
    [InlineData(@"\\?\C:\a\b", @"\\?\C:\a")]
    [InlineData(@"\\?\UNC\s\sh\x", @"\\?\UNC\s\sh")]
    public void The_folder_part_is_read_as_on_NET_10(string? path, string? expected)
    {
        Assert.Equal(expected, PathText.GetDirectoryName(path));
    }

    [Theory]
    [InlineData("C:\\a|b\\c", "C:\\a|b", "c")]
    [InlineData("C:\\a\u0001\\c", "C:\\a\u0001", "c")]
    [InlineData("C:\\a\\b\"<>.msp", "C:\\a", "b\"<>.msp")]
    public void Characters_a_name_cannot_hold_are_read_as_text(string path, string folder, string name)
    {
        Assert.Equal(folder, PathText.GetDirectoryName(path));
        Assert.Equal(name, PathText.GetFileName(path));
        Assert.Equal(name, PathText.Combine(folder, name).Substring(folder.Length + 1));
    }

    [Theory]
    [InlineData("C:x", false)]
    [InlineData(@"C:\x", true)]
    [InlineData(@"\\?\x", true)]
    [InlineData(@"\\server\share", true)]
    [InlineData(@"\x", false)]
    [InlineData(@"1:\x", false)]
    public void A_path_is_fully_qualified_only_from_a_drive_root_or_two_separators(string path, bool expected)
    {
        Assert.Equal(expected, PathText.IsPathFullyQualified(path));
    }

    [Fact]
    public void A_null_path_is_not_asked_whether_it_is_fully_qualified()
    {
        Assert.Equal("path", Assert.Throws<ArgumentNullException>(() => PathText.IsPathFullyQualified(null!)).ParamName);
    }

    [Theory]
    [InlineData(@"C:\", @"C:\")]
    [InlineData(@"C:\a\", @"C:\a")]
    [InlineData(@"C:\a\\", @"C:\a\")]
    [InlineData(@"\\server\share\", @"\\server\share")]
    [InlineData(@"D:\backup\", @"D:\backup")]
    public void One_ending_separator_is_taken_off_unless_the_path_is_a_root(string path, string expected)
    {
        Assert.Equal(expected, PathText.TrimEndingDirectorySeparator(path));
    }

    [Theory]
    [InlineData(@"C:\a\b", @"C:\")]
    [InlineData(@"C:/a", @"C:\")]
    [InlineData(@"C:a", @"C:")]
    [InlineData(@"\a", @"\")]
    [InlineData(@"\\server\share\a", @"\\server\share")]
    [InlineData(@"\\?\C:\a", @"\\?\C:\")]
    [InlineData(@"a\b", "")]
    public void The_root_is_read_off_the_front_of_the_path(string path, string expected)
    {
        Assert.Equal(expected, PathText.GetPathRoot(path));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void A_path_with_no_text_has_no_root(string? path)
    {
        Assert.Null(PathText.GetPathRoot(path));
    }

    [Theory]
    [InlineData(@"C:\a", @"C:\a")]
    [InlineData(@"C:\a ", @"\\?\C:\a ")]
    [InlineData(@"C:\a.", @"\\?\C:\a.")]
    [InlineData(@"\\server\share\a.", @"\\?\UNC\server\share\a.")]
    [InlineData(@"\\?\C:\a.", @"\\?\C:\a.")]
    [InlineData(@"\\.\C:\a.", @"\\.\C:\a.")]
    [InlineData(@"a.", @"a.")]
    [InlineData(@"C:a.", @"C:a.")]
    [InlineData(@"//server/share/a.", @"\\?\//server/share/a.")]
    public void A_path_ending_in_a_space_or_a_period_takes_the_prefix_unless_relative_or_a_device_path(
        string path, string expected)
    {
        Assert.Equal(expected, PathText.EnsureExtendedPrefixIfNeeded(path));
    }

    [Fact]
    public void A_path_takes_the_prefix_from_260_characters()
    {
        var below = @"C:\" + new string('a', 256);
        var at = below + "a";
        Assert.Equal(259, below.Length);

        Assert.Same(below, PathText.EnsureExtendedPrefixIfNeeded(below));
        Assert.Equal(@"\\?\" + at, PathText.EnsureExtendedPrefixIfNeeded(at));
        Assert.Equal(@"\\?\UNC\server\" + new string('s', 260),
            PathText.EnsureExtendedPrefixIfNeeded(@"\\server\" + new string('s', 260)));
    }
}
