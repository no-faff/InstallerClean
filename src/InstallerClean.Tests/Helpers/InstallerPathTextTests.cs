using System;
using System.Globalization;
using InstallerClean.Helpers;
using InstallerClean.Services;

namespace InstallerClean.Tests.Helpers;

/// <summary>
/// The display-time binding that stops <c>C:\Windows\Installer</c> splitting
/// across two lines. Pure string work, so it is assertable without a UI thread.
/// That WPF's line breaker then honours the word joiner is not assertable here
/// at all; it was settled by eye on the running app, and the receipt is on
/// <see cref="InstallerPathText"/> itself.
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

    // The resolved folder, read the same way the code under test reads it,
    // rather than the C: literal: KeepWhole binds %SystemRoot%\Installer, so a
    // hardcoded path would assert nothing on a machine whose Windows lives
    // elsewhere and would fail there while the app was working correctly.
    private static readonly string Path = InstallerCacheHelpers.InstallerFolder;

    // Built from Path by the rule rather than written out again: a joiner
    // either side of every backslash. That is all four seams, the drive colon
    // included, because the seam after the colon and the seam before the first
    // backslash are the same position. The drive letter's own colon takes none,
    // which the assertions below pin separately.
    private static readonly string BoundPath = Path.Replace("\\", Wj + "\\" + Wj);

    [Fact]
    public void KeepWhole_binds_the_backslash_seams_and_the_drive_colon_but_not_the_drive_letter()
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
        var lowered = Path.ToLowerInvariant();

        var result = InstallerPathText.KeepWhole(lowered);

        Assert.Equal(lowered.Replace("\\", Wj + "\\" + Wj), result);
    }

    [Theory]
    [InlineData("")]
    [InlineData("Nothing to clean up.")]
    public void KeepWhole_returns_a_string_naming_no_installer_folder_unchanged(string text)
    {
        Assert.Equal(text, InstallerPathText.KeepWhole(text));
        Assert.DoesNotContain(Wj, InstallerPathText.KeepWhole(text));
    }

    [Fact]
    public void KeepWhole_leaves_a_near_miss_alone()
    {
        // Both are derived from the real folder rather than written out, so the
        // near miss stays a near miss on a machine whose Windows is not on C:.
        // The parent on its own is a prefix of the folder and not the folder;
        // the second is the same leaf under a different parent.
        var parent = Path[..Path.LastIndexOf('\\')];
        var elsewhere = "D:\\Backup" + Path[Path.LastIndexOf('\\')..];

        Assert.Equal(parent, InstallerPathText.KeepWhole(parent));
        Assert.Equal(elsewhere, InstallerPathText.KeepWhole(elsewhere));
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
