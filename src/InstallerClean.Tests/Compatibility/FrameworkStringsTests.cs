using System.Text;

namespace InstallerClean.Tests.Compatibility;

/// <summary>
/// The string, character and span members the app calls as .NET 10 has them: ordinal tests for a
/// first or last character, ordinal searches that fold case only for OrdinalIgnoreCase, splits
/// at one separator with a count and options, the ASCII character tests, and the ASCII span
/// comparisons.
/// </summary>
public class FrameworkStringsTests
{
    [Theory]
    [InlineData("\\abc", '\\', true)]
    [InlineData("abc", '\\', false)]
    [InlineData("", '\\', false)]
    [InlineData("\u00AD\\abc", '\\', false)]
    public void StartsWith_a_char_is_the_first_character_compared_ordinally(string text, char value, bool expected)
    {
        Assert.Equal(expected, text.StartsWith(value));
    }

    [Theory]
    [InlineData("C:", ':', true)]
    [InlineData("C:\\", ':', false)]
    [InlineData("", ':', false)]
    [InlineData("C:\u00AD", ':', false)]
    public void EndsWith_a_char_is_the_last_character_compared_ordinally(string text, char value, bool expected)
    {
        Assert.Equal(expected, text.EndsWith(value));
    }

    [Theory]
    [InlineData("C:\\a%b", '%', true)]
    [InlineData("C:\\ab", '%', false)]
    [InlineData("a\u0000b", '\0', true)]
    [InlineData("", '\0', false)]
    [InlineData("A", 'a', false)]
    public void Contains_a_char_compares_each_character_ordinally(string text, char value, bool expected)
    {
        Assert.Equal(expected, text.Contains(value));
    }

    [Fact]
    public void Contains_a_char_on_null_text_fails_as_a_call_on_null_does()
    {
        string text = null!;

        Assert.Throws<NullReferenceException>(() => text.Contains('%'));
    }

    [Theory]
    [InlineData("a<installer>b", "<installer>", StringComparison.Ordinal, true)]
    [InlineData("a<INSTALLER>b", "<installer>", StringComparison.Ordinal, false)]
    [InlineData("a<INSTALLER>b", "<installer>", StringComparison.OrdinalIgnoreCase, true)]
    [InlineData("a\u0000b", "ab", StringComparison.OrdinalIgnoreCase, false)]
    [InlineData("a\u00ADb", "ab", StringComparison.OrdinalIgnoreCase, false)]
    [InlineData("abc", "", StringComparison.OrdinalIgnoreCase, true)]
    [InlineData("", "", StringComparison.Ordinal, true)]
    [InlineData("ab", "abc", StringComparison.OrdinalIgnoreCase, false)]
    public void Contains_searches_ordinally_folding_case_only_when_asked(
        string text, string value, StringComparison comparison, bool expected)
    {
        Assert.Equal(expected, text.Contains(value, comparison));
    }

    [Theory]
    [InlineData("a<installer>b", "<installer>", 0, StringComparison.Ordinal, 1)]
    [InlineData("a<INSTALLER>b", "<installer>", 0, StringComparison.Ordinal, -1)]
    [InlineData("a<INSTALLER>b", "<installer>", 0, StringComparison.OrdinalIgnoreCase, 1)]
    [InlineData(@"C:\Windows\Installer and c:\windows\installer", @"C:\WINDOWS\INSTALLER", 1, StringComparison.OrdinalIgnoreCase, 25)]
    [InlineData("a\u0000b", "ab", 0, StringComparison.OrdinalIgnoreCase, -1)]
    [InlineData("a\u00ADb", "ab", 0, StringComparison.OrdinalIgnoreCase, -1)]
    [InlineData("abc", "", 2, StringComparison.OrdinalIgnoreCase, 2)]
    [InlineData("abc", "", 3, StringComparison.Ordinal, 3)]
    [InlineData("ab", "abc", 0, StringComparison.OrdinalIgnoreCase, -1)]
    public void IndexOfOrdinal_finds_the_first_ordinal_match_folding_case_only_when_asked(
        string text, string value, int startIndex, StringComparison comparison, int expected)
    {
        Assert.Equal(expected, text.IndexOfOrdinal(value, startIndex, comparison));
        if (startIndex == 0)
            Assert.Equal(expected, text.IndexOfOrdinal(value, comparison));
    }


    [Fact]
    public void IndexOfOrdinal_refuses_a_null_value_a_start_past_the_end_and_a_culture_comparison()
    {
        var text = "abc";

        Assert.Throws<ArgumentNullException>(() => text.IndexOfOrdinal(null!, StringComparison.Ordinal));
        Assert.Throws<ArgumentOutOfRangeException>(() => text.IndexOfOrdinal("a", 4, StringComparison.Ordinal));
        Assert.Throws<ArgumentOutOfRangeException>(() => text.IndexOfOrdinal("a", -1, StringComparison.Ordinal));
        Assert.Throws<NotSupportedException>(() => text.IndexOfOrdinal("a", StringComparison.CurrentCultureIgnoreCase));
    }

    [Fact]
    public void Contains_refuses_a_null_value_and_an_unknown_comparison()
    {
        var text = "abc";

        Assert.Throws<ArgumentNullException>(() => text.Contains(null!, StringComparison.Ordinal));
        Assert.Throws<ArgumentException>(() => text.Contains("a", (StringComparison)99));
        Assert.Throws<ArgumentNullException>(() => text.Contains(null!, (StringComparison)99));
    }

    [Theory]
    [InlineData("a<installer>b<installer>", "<installer>", "X", StringComparison.Ordinal, "aXbX")]
    [InlineData("a<INSTALLER>b<installer>", "<installer>", "X", StringComparison.Ordinal, "a<INSTALLER>bX")]
    [InlineData("a<INSTALLER>b<installer>", "<installer>", "X", StringComparison.OrdinalIgnoreCase, "aXbX")]
    [InlineData("aaa", "aa", "b", StringComparison.OrdinalIgnoreCase, "ba")]
    [InlineData("a\u0000b", "ab", "X", StringComparison.OrdinalIgnoreCase, "a\u0000b")]
    [InlineData("axbx", "X", null, StringComparison.OrdinalIgnoreCase, "ab")]
    public void Replace_replaces_each_ordinal_match_left_to_right(
        string text, string oldValue, string? newValue, StringComparison comparison, string expected)
    {
        Assert.Equal(expected, text.Replace(oldValue, newValue, comparison));
    }

    [Theory]
    [InlineData(StringComparison.Ordinal)]
    [InlineData(StringComparison.OrdinalIgnoreCase)]
    public void Replace_gives_back_the_same_text_where_nothing_matches(StringComparison comparison)
    {
        var text = "abc";

        Assert.Same(text, text.Replace("x", "y", comparison));
    }

    [Fact]
    public void Replace_refuses_a_null_or_empty_old_value()
    {
        var text = "abc";

        Assert.Throws<ArgumentNullException>(() => text.Replace(null!, "x", StringComparison.OrdinalIgnoreCase));
        Assert.Throws<ArgumentException>(() => text.Replace("", "x", StringComparison.OrdinalIgnoreCase));
        Assert.Throws<ArgumentNullException>(() => text.Replace(null!, "x", StringComparison.Ordinal));
        Assert.IsNotType<ArgumentNullException>(
            Assert.ThrowsAny<ArgumentException>(() => text.Replace(null!, "x", (StringComparison)99)));
    }

    [Fact]
    public void Split_at_a_char_drops_empty_parts_when_asked()
    {
        var text = "a,b,,c,";

        Assert.Equal(new[] { "a", "b", "c" }, text.Split(',', StringSplitOptions.RemoveEmptyEntries));
        Assert.Equal(new[] { "a", "b", "", "c", "" }, text.Split(',', StringSplitOptions.None));
    }

    [Fact]
    public void Split_at_a_char_trims_each_part_when_asked()
    {
        var text = " a , b ,, c ";

        Assert.Equal(new[] { "a", "b", "", "c" }, text.Split(',', (StringSplitOptions)2));
        Assert.Equal(new[] { "a", "b", "c" }, text.Split(',', (StringSplitOptions)3));
    }

    [Fact]
    public void Split_with_a_count_leaves_the_rest_in_the_last_part()
    {
        var text = "a;b;c;d";

        Assert.Equal(new[] { "a", "b", "c;d" }, text.Split(';', 3));
        Assert.Equal(new[] { "a;b;c;d" }, text.Split(';', 1));
        Assert.Empty(text.Split(';', 0));
    }

    [Fact]
    public void Split_with_a_count_skips_empty_parts_before_the_last_when_asked()
    {
        var text = "a;;b;c";

        Assert.Equal(new[] { "a", "b;c" }, text.Split(';', 2, StringSplitOptions.RemoveEmptyEntries));
        Assert.Equal(new[] { "a", "", "b;c" }, text.Split(';', 3));
    }

    [Fact]
    public void Split_with_a_count_ends_on_an_empty_part_after_a_final_separator()
    {
        var text = "a,";

        Assert.Equal(new[] { "a", "" }, text.Split(',', 5));
    }

    [Fact]
    public void Split_at_a_string_finds_each_separator_without_overlap()
    {
        var text = "a\r\nb\r\n\r\nc";

        Assert.Equal(new[] { "a", "b", "", "c" }, text.Split("\r\n"));
        Assert.Equal(new[] { "a", "b", "c" }, text.Split("\r\n", StringSplitOptions.RemoveEmptyEntries));
        Assert.Equal(new[] { "a", "b\r\n\r\nc" }, text.Split("\r\n", 2));
    }

    [Fact]
    public void Split_at_a_string_overlapping_itself_takes_the_leftmost_occurrence()
    {
        var text = "aaab";

        Assert.Equal(new[] { "", "ab" }, text.Split("aa"));
    }

    [Fact]
    public void Split_at_a_null_or_empty_string_gives_the_whole_text()
    {
        var text = "a b";

        Assert.Equal(new[] { "a b" }, text.Split((string?)null));
        Assert.Equal(new[] { "a b" }, text.Split(""));
    }

    [Fact]
    public void Split_of_empty_text_gives_one_empty_part_or_none()
    {
        var text = "";

        Assert.Equal(new[] { "" }, text.Split(","));
        Assert.Empty(text.Split(",", StringSplitOptions.RemoveEmptyEntries));
        Assert.Empty(text.Split(',', StringSplitOptions.RemoveEmptyEntries));
    }

    [Fact]
    public void Split_refuses_a_negative_count_and_an_unknown_option()
    {
        var text = "a,b";

        Assert.Throws<ArgumentOutOfRangeException>(() => text.Split(',', -1));
        Assert.Throws<ArgumentException>(() => text.Split(',', (StringSplitOptions)4));
    }

    [Fact]
    public void Join_at_a_char_writes_null_values_as_nothing()
    {
        Assert.Equal("a,,c", string.Join(',', new List<string?> { "a", null, "c" }));
        Assert.Equal("1 2 3", string.Join(' ', new[] { 1, 2, 3 }.AsEnumerable()));
        Assert.Equal("", string.Join(',', new List<string>()));
        Assert.Throws<ArgumentNullException>(() => string.Join(',', (IEnumerable<string>)null!));
    }

    [Fact]
    public void Concat_of_two_spans_is_their_characters_in_order()
    {
        var path = @"\??\C:\Windows";

        Assert.Equal(@"\\?\C:\Windows", string.Concat(@"\\?\", path.AsSpan(4)));
        Assert.Same(string.Empty, string.Concat(ReadOnlySpan<char>.Empty, ReadOnlySpan<char>.Empty));
    }

    [Theory]
    [InlineData('0', true, false, false, false, true)]
    [InlineData('9', true, false, false, false, true)]
    [InlineData('/', false, false, false, false, false)]
    [InlineData(':', false, false, false, false, false)]
    [InlineData('A', false, true, false, true, true)]
    [InlineData('F', false, true, false, true, true)]
    [InlineData('G', false, true, false, true, false)]
    [InlineData('Z', false, true, false, true, false)]
    [InlineData('a', false, true, true, false, true)]
    [InlineData('f', false, true, true, false, true)]
    [InlineData('g', false, true, true, false, false)]
    [InlineData('z', false, true, true, false, false)]
    [InlineData('@', false, false, false, false, false)]
    [InlineData('[', false, false, false, false, false)]
    [InlineData('`', false, false, false, false, false)]
    [InlineData('{', false, false, false, false, false)]
    [InlineData('\u0660', false, false, false, false, false)]
    [InlineData('\u00E9', false, false, false, false, false)]
    [InlineData('\u00C9', false, false, false, false, false)]
    [InlineData('\uFF21', false, false, false, false, false)]
    public void The_ASCII_character_tests_answer_for_ASCII_alone(
        char c, bool digit, bool letter, bool lower, bool upper, bool hex)
    {
        Assert.Equal(digit, char.IsAsciiDigit(c));
        Assert.Equal(letter, char.IsAsciiLetter(c));
        Assert.Equal(lower, char.IsAsciiLetterLower(c));
        Assert.Equal(upper, char.IsAsciiLetterUpper(c));
        Assert.Equal(hex, char.IsAsciiHexDigit(c));
    }

    [Theory]
    [InlineData("", true)]
    [InlineData("abc\u007F", true)]
    [InlineData("abc\u0080", false)]
    [InlineData("\u00E9", false)]
    public void Ascii_IsValid_holds_for_U0000_to_U007F_alone(string text, bool expected)
    {
        Assert.Equal(expected, Ascii.IsValid(text.AsSpan()));
    }

    [Theory]
    [InlineData("SETUP.EXE", "setup.exe", true)]
    [InlineData("abc", "abd", false)]
    [InlineData("abc", "ab", false)]
    [InlineData("[", "{", false)]
    [InlineData("@", "`", false)]
    [InlineData("\u00C9", "\u00E9", false)]
    [InlineData("\u00E9", "\u00E9", false)]
    [InlineData("K", "\u212A", false)]
    [InlineData("", "", true)]
    public void Ascii_EqualsIgnoreCase_folds_A_to_Z_and_refuses_anything_outside_ASCII(
        string left, string right, bool expected)
    {
        Assert.Equal(expected, Ascii.EqualsIgnoreCase(left.AsSpan(), right.AsSpan()));
    }

    [Theory]
    [InlineData("0f9A", "0123456789abcdefABCDEF", -1)]
    [InlineData("0f9g", "0123456789abcdefABCDEF", 3)]
    [InlineData("x", "", 0)]
    [InlineData("", "", -1)]
    [InlineData("", "abc", -1)]
    public void IndexOfAnyExcept_finds_the_first_character_outside_the_set(string text, string values, int expected)
    {
        var span = text.AsSpan();

        Assert.Equal(expected, span.IndexOfAnyExcept(values.AsSpan()));
    }

    [Fact]
    public void The_searches_refuse_a_culture_comparison()
    {
        var text = "abc";

        var contains = Assert.Throws<NotSupportedException>(() => text.Contains("b", StringComparison.InvariantCultureIgnoreCase));
        var replace = Assert.Throws<NotSupportedException>(() => text.Replace("b", "x", StringComparison.CurrentCulture));

        Assert.Contains("InvariantCultureIgnoreCase", contains.Message, StringComparison.Ordinal);
        Assert.Contains("CurrentCulture", replace.Message, StringComparison.Ordinal);
    }
}
