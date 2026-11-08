using InstallerClean.Helpers;

namespace InstallerClean.Tests.Helpers;

/// <summary>
/// <see cref="FullPaths"/> answers as .NET 10's <c>System.IO.Path.GetFullPath</c> and
/// <c>GetRelativePath</c> answer on Windows. Among the inputs are the shapes .NET Framework's
/// own GetFullPath answers otherwise: a <c>|</c>, a trailing U+00A0, a component of more than
/// 255 characters and a colon after the root, each of which Windows resolves as it is.
/// </summary>
public class FullPathsTests
{
    [Theory]
    [InlineData(@"C:\a\..\b", @"C:\b")]
    [InlineData(@"C:\a\.\b", @"C:\a\b")]
    [InlineData(@"C:/a/b", @"C:\a\b")]
    [InlineData(@"C:\a.  ", @"C:\a")]
    [InlineData(@"C:\a|b", @"C:\a|b")]
    [InlineData("C:\\a\u00A0", "C:\\a\u00A0")]
    [InlineData(@"C:\a\b:c", @"C:\a\b:c")]
    [InlineData(@"\\server\share\a\..\b", @"\\server\share\b")]
    public void A_path_is_resolved_as_Windows_resolves_it(string path, string expected)
    {
        Assert.Equal(expected, FullPaths.GetFullPath(path));
    }

    [Fact]
    public void A_component_longer_than_255_characters_is_resolved_as_it_is()
    {
        var path = @"C:\" + new string('n', 256) + @"\x";

        Assert.Equal(path, FullPaths.GetFullPath(path));
    }

    [Theory]
    [InlineData(@"\\?\C:\x\..\y")]
    [InlineData(@"\??\C:\x")]
    public void An_extended_path_is_returned_as_it_is(string path)
    {
        Assert.Same(path, FullPaths.GetFullPath(path));
    }

    [Fact]
    public void A_path_that_is_already_full_is_returned_as_the_same_string()
    {
        var path = @"C:\Windows\Installer";

        Assert.Same(path, FullPaths.GetFullPath(path));
    }

    [Fact]
    public void A_null_path_names_the_parameter()
    {
        Assert.Equal("path", Assert.Throws<ArgumentNullException>(() => FullPaths.GetFullPath(null!)).ParamName);
    }

    [Theory]
    [InlineData("", "The path is empty.")]
    [InlineData("   ", "The path is empty.")]
    [InlineData("C:\\a\0b", "Null character in path.")]
    public void An_empty_blank_or_null_character_path_names_the_parameter(string path, string message)
    {
        var thrown = Assert.Throws<ArgumentException>(() => FullPaths.GetFullPath(path));

        Assert.Equal("path", thrown.ParamName);
        Assert.StartsWith(message, thrown.Message);
    }

    [ShortNamesFact]
    public void A_short_name_in_a_folder_that_is_there_is_expanded()
    {
        var folder = Path.Combine(Path.GetTempPath(), "ic full paths " + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(folder);
        try
        {
            var shortPath = ShortNamesFactAttribute.GetShortPath(folder);
            var longPath = ShortNamesFactAttribute.GetLongPath(folder);
            Assert.NotNull(shortPath);
            Assert.NotEqual(Path.GetFileName(folder), Path.GetFileName(shortPath));

            Assert.Equal(longPath, FullPaths.GetFullPath(shortPath!));
        }
        finally
        {
            Directory.Delete(folder);
        }
    }

    [ShortNamesFact]
    public void A_short_name_above_a_part_that_is_not_there_is_expanded_and_the_rest_kept()
    {
        var folder = Path.Combine(Path.GetTempPath(), "ic full paths " + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(folder);
        try
        {
            var shortPath = ShortNamesFactAttribute.GetShortPath(folder);
            var longPath = ShortNamesFactAttribute.GetLongPath(folder);
            Assert.NotNull(shortPath);

            Assert.Equal(longPath + @"\missing\NOT~1.MSP", FullPaths.GetFullPath(shortPath + @"\missing\NOT~1.MSP"));
        }
        finally
        {
            Directory.Delete(folder);
        }
    }

    [Theory]
    [InlineData(@"C:\Foo", @"C:\Bar", @"..\Bar")]
    [InlineData(@"C:\Foo", @"C:\Foo\Bar", "Bar")]
    [InlineData(@"C:\Foo\Bar", @"C:\Bar\Bar", @"..\..\Bar\Bar")]
    [InlineData(@"C:\Foo\Foo", @"C:\Foo\Bar", @"..\Bar")]
    [InlineData(@"C:\Foodie", @"C:\Foobar\", @"..\Foobar\")]
    [InlineData(@"C:\a", @"D:\a", @"D:\a")]
    [InlineData(@"C:\a\", @"C:\A", ".")]
    [InlineData(@"C:\a", @"C:\A\B|c", @"B|c")]
    public void A_relative_path_is_worked_out_as_on_NET_10(string relativeTo, string path, string expected)
    {
        Assert.Equal(expected, FullPaths.GetRelativePath(relativeTo, path));
    }

    [Theory]
    [InlineData(null, "x", "relativeTo")]
    [InlineData("x", null, "path")]
    public void A_null_argument_to_GetRelativePath_is_named(string? relativeTo, string? path, string name)
    {
        Assert.Equal(name, Assert.Throws<ArgumentNullException>(() => FullPaths.GetRelativePath(relativeTo!, path!)).ParamName);
    }

    [Theory]
    [InlineData("", "x", "relativeTo")]
    [InlineData("x", "  ", "path")]
    public void An_empty_argument_to_GetRelativePath_is_named(string relativeTo, string path, string name)
    {
        var thrown = Assert.Throws<ArgumentException>(() => FullPaths.GetRelativePath(relativeTo, path));

        Assert.Equal(name, thrown.ParamName);
        Assert.StartsWith("The path is empty.", thrown.Message);
    }

    [Theory]
    [InlineData(@"C:\Windows\Installer\1a2b3c.msi")]
    [InlineData(@"relative\name.msi")]
    [InlineData(@"C:\Windows\Installer\name.msi.")]
    [InlineData(@"C:\Windows\Installer\name.msi ")]
    [InlineData(@"C:\Windows\..\Windows\Installer\name.msi")]
    public void Under_260_a_Windows_call_is_handed_the_callers_own_string(string path)
    {
        Assert.Same(path, FullPaths.ForWindowsCall(path));
    }

    [Fact]
    public void At_260_or_more_a_Windows_call_is_handed_the_full_path_with_the_prefix()
    {
        var path = @"C:\" + new string('a', 296) + @"\x.msi";

        Assert.Equal(@"\\?\" + path, FullPaths.ForWindowsCall(path));
    }

    [Fact]
    public void A_long_path_with_dot_dot_is_resolved_before_the_prefix_goes_on()
    {
        var folder = @"C:\" + new string('a', 296);

        Assert.Equal(@"\\?\" + folder + @"\x.msi", FullPaths.ForWindowsCall(folder + @"\b\..\x.msi"));
    }

    [Fact]
    public void A_long_path_whose_full_path_is_short_is_handed_over_as_its_full_path()
    {
        var path = @"C:\" + new string('a', 290) + @"\..\Windows\Installer\x.msi";

        Assert.Equal(@"C:\Windows\Installer\x.msi", FullPaths.ForWindowsCall(path));
    }

    [Fact]
    public void A_short_name_is_not_expanded_for_a_Windows_call()
    {
        var path = @"C:\" + new string('a', 250) + @"\PROGRA~1\x.msi";

        Assert.Equal(@"\\?\" + path, FullPaths.ForWindowsCall(path));
    }

    [Fact]
    public void A_long_share_path_is_handed_over_with_the_UNC_prefix()
    {
        var path = @"\\server\share\" + new string('s', 280);

        Assert.Equal(@"\\?\UNC\server\share\" + new string('s', 280), FullPaths.ForWindowsCall(path));
    }

    [Fact]
    public void A_long_path_already_prefixed_is_handed_over_as_given()
    {
        var path = @"\\?\C:\" + new string('a', 300);

        Assert.Same(path, FullPaths.ForWindowsCall(path));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("C:\\a\0b")]
    public void A_path_with_no_full_path_is_handed_over_as_given(string path)
    {
        Assert.Same(path, FullPaths.ForWindowsCall(path));
    }

    [Fact]
    public void Null_is_handed_over_as_null()
    {
        Assert.Null(FullPaths.ForWindowsCall(null!));
    }
}
