using InstallerClean.Helpers;

namespace InstallerClean.Tests.Helpers;

/// <summary>
/// The parsing behind the four windows' inline-composition builders. It used to
/// sit inside <c>Window</c> constructors with no seam, which is how a
/// regular-string escape that produced six literal characters instead of a
/// zero-width space shipped unnoticed. These tests are that seam.
///
/// The zero-width space is referenced as <c>(char)0x200B</c> throughout, never a
/// literal character and never a <c>U+200B</c> escape in the source: both the
/// character and the escape are mangled by the editing tools that write this
/// file, which is the very failure the code under test guards against.
/// </summary>
public class CompositionParsingTests
{
    private const char Zws = (char)0x200B;

    [Fact]
    public void InsertPathWrapPoints_puts_a_zero_width_space_after_every_backslash()
    {
        var result = CompositionParsing.InsertPathWrapPoints(@"C:\Windows\Installer");

        Assert.Equal("C:\\" + Zws + "Windows\\" + Zws + "Installer", result);
    }

    [Fact]
    public void InsertPathWrapPoints_inserts_the_character_not_the_text_of_the_escape()
    {
        // The shipped bug: a regular-string "\\u200B" is a backslash plus the five
        // ordinary characters u200B, so a path rendered as D:U+200BBackup. The wrap
        // point must be the actual U+200B character, and the literal text "u200B"
        // must never appear.
        var result = CompositionParsing.InsertPathWrapPoints(@"D:\Backup");

        Assert.Contains(Zws, result);
        Assert.DoesNotContain("u200B", result);
        Assert.Equal("D:\\" + Zws + "Backup", result);
    }

    [Fact]
    public void InsertPathWrapPoints_wraps_a_UNC_path_at_every_backslash()
    {
        var result = CompositionParsing.InsertPathWrapPoints(@"\\server\share");

        Assert.Equal("\\" + Zws + "\\" + Zws + "server\\" + Zws + "share", result);
    }

    [Fact]
    public void InsertPathWrapPoints_leaves_a_backslash_free_string_untouched()
    {
        Assert.Equal("no backslashes here", CompositionParsing.InsertPathWrapPoints("no backslashes here"));
    }

    [Fact]
    public void SplitAtSubstring_splits_around_the_substring_and_trims_the_prefix()
    {
        var split = CompositionParsing.SplitAtSubstring(@"Moved 3 files to: D:\Backup", @"D:\Backup");

        Assert.NotNull(split);
        Assert.Equal("Moved 3 files to:", split!.Prefix);
        Assert.Equal(string.Empty, split.Suffix);
    }

    [Fact]
    public void SplitAtSubstring_keeps_the_text_on_both_sides()
    {
        var split = CompositionParsing.SplitAtSubstring("before HERE after", "HERE");

        Assert.NotNull(split);
        Assert.Equal("before", split!.Prefix);
        Assert.Equal(" after", split.Suffix);
    }

    [Fact]
    public void SplitAtSubstring_returns_null_when_the_substring_is_absent()
    {
        Assert.Null(CompositionParsing.SplitAtSubstring("no path in this sentence", @"D:\Backup"));
    }

    [Fact]
    public void SplitAtSubstring_returns_null_for_an_empty_substring()
    {
        // The all-clear and delete-to-bin summaries carry no destination, so the
        // whole line must render as one Run rather than splitting on "".
        Assert.Null(CompositionParsing.SplitAtSubstring("Nothing to clean up", string.Empty));
    }

    [Fact]
    public void SplitAtBracketedPhrase_splits_prefix_link_and_suffix()
    {
        var split = CompositionParsing.SplitAtBracketedPhrase(
            @"Copy them back to C:\Windows\Installer if anything ever breaks ([extremely unlikely]).");

        Assert.NotNull(split);
        Assert.Equal(@"Copy them back to C:\Windows\Installer if anything ever breaks (", split!.Prefix);
        Assert.Equal("extremely unlikely", split.LinkText);
        Assert.Equal(").", split.Suffix);
    }

    [Fact]
    public void SplitAtBracketedPhrase_handles_a_leading_bracket()
    {
        var split = CompositionParsing.SplitAtBracketedPhrase("[learn more] about this");

        Assert.NotNull(split);
        Assert.Equal(string.Empty, split!.Prefix);
        Assert.Equal("learn more", split.LinkText);
        Assert.Equal(" about this", split.Suffix);
    }

    [Fact]
    public void SplitAtBracketedPhrase_returns_null_without_a_complete_pair()
    {
        // The all-clear receipt and the permanent-delete reassurance carry no
        // link, so they must render verbatim.
        Assert.Null(CompositionParsing.SplitAtBracketedPhrase("A plain sentence with no link."));
        Assert.Null(CompositionParsing.SplitAtBracketedPhrase("An open [ bracket with no close."));
    }
}
