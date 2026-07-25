using System;
using System.Globalization;
using InstallerClean.Helpers;

namespace InstallerClean.Tests.Helpers;

/// <summary>
/// The display-time binding that stops <c>C:\Windows\Installer</c> splitting
/// across two lines. Pure string work, so it is assertable without a UI thread;
/// whether WPF's line breaker then honours the word joiner is not, and is an
/// eye check on the running app.
///
/// The word joiner is referenced as <c>(char)0x2060</c> throughout, never as a
/// literal character and never as an escape inside a string, for the reason
/// <see cref="CompositionParsingTests"/> gives: a literal is invisible on screen
/// and survives no round trip through a tool that normalises whitespace, and an
/// escape inside a string is exactly the mistake that once shipped.
/// </summary>
public class InstallerPathTextTests
{
    private const char Wj = (char)0x2060;
    private const string Path = "C:\\Windows\\Installer";
    private static readonly string BoundPath =
        "C:" + Wj + "\\" + Wj + "Windows" + Wj + "\\" + Wj + "Installer";

    [Fact]
    public void KeepWhole_binds_every_seam_of_the_path_and_no_letter_pair()
    {
        Assert.Equal(BoundPath, InstallerPathText.KeepWhole(Path));
    }

    [Fact]
    public void KeepWhole_inserts_the_character_not_the_text_of_the_escape()
    {
        var result = InstallerPathText.KeepWhole(Path);

        Assert.Equal(4, result.Count(c => c == Wj));
        Assert.DoesNotContain("u2060", result, StringComparison.OrdinalIgnoreCase);
        // Strip the joiners and the path is character for character what it was.
        Assert.Equal(Path, new string(result.Where(c => c != Wj).ToArray()));
    }

    [Fact]
    public void KeepWhole_leaves_the_rest_of_the_sentence_alone()
    {
        var result = InstallerPathText.KeepWhole("They sit in " + Path + ", left behind.");

        Assert.Equal("They sit in " + BoundPath + ", left behind.", result);
    }

    [Fact]
    public void KeepWhole_binds_every_occurrence()
    {
        var result = InstallerPathText.KeepWhole(Path + " and " + Path);

        Assert.Equal(BoundPath + " and " + BoundPath, result);
    }

    [Fact]
    public void KeepWhole_matches_case_insensitively_and_keeps_the_case_it_found()
    {
        var result = InstallerPathText.KeepWhole("c:\\windows\\installer");

        Assert.Equal("c:" + Wj + "\\" + Wj + "windows" + Wj + "\\" + Wj + "installer", result);
    }

    [Theory]
    [InlineData("")]
    [InlineData("Nothing to clean up.")]
    [InlineData("C:\\Windows")]
    [InlineData("D:\\Backup\\Installer")]
    public void KeepWhole_returns_a_string_naming_no_installer_folder_unchanged(string text)
    {
        Assert.Equal(text, InstallerPathText.KeepWhole(text));
        Assert.DoesNotContain(Wj, InstallerPathText.KeepWhole(text));
    }

    [Fact]
    public void KeepWhole_treats_a_null_as_empty()
    {
        Assert.Equal(string.Empty, InstallerPathText.KeepWhole(null));
    }

    [Fact]
    public void Converter_binds_the_path_it_is_handed()
    {
        var converter = new InstallerPathTextConverter();

        var result = converter.Convert(Path, typeof(string), null, CultureInfo.InvariantCulture);

        Assert.Equal(BoundPath, result);
    }

    [Fact]
    public void Converter_is_one_way()
    {
        var converter = new InstallerPathTextConverter();

        Assert.Throws<NotSupportedException>(
            () => converter.ConvertBack(BoundPath, typeof(string), null, CultureInfo.InvariantCulture));
    }
}
