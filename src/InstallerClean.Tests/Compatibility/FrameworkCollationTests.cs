using System.Globalization;
using InstallerClean.Compatibility;

namespace InstallerClean.Tests.Compatibility;

/// <summary>
/// <see cref="FrameworkCollation"/> sorts in a culture as .NET 10's CompareInfo.Compare does
/// on Windows, through the ICU Windows ships. The expected signs are what .NET 10 gave for
/// each pair. NLS orders ')' ahead of '_' and tells full-width capitals from their small
/// letters under IgnoreCase, and an ICU collator lowered to secondary strength without
/// .NET 10's rules makes katakana equal to hiragana, so each of those would fail here.
/// </summary>
public class FrameworkCollationTests
{
    [Fact]
    public void Windows_here_ships_an_ICU_with_the_functions_the_sort_calls()
    {
        Assert.NotNull(WindowsIcu.Loaded);
        Assert.NotNull(FrameworkCollation.Icu.Load(WindowsIcu.Loaded!));
    }

    [Theory]
    [InlineData("en-GB", "a_b", "a)b", -1, -1)]
    [InlineData("en-GB", "Microsoft .NET AppHost Pack - 8.0.32 (x64_arm64)", "Microsoft .NET AppHost Pack - 8.0.32 (x64)", -1, -1)]
    [InlineData("en-GB", "Zebra", "apple", 1, 1)]
    [InlineData("en-GB", "\u00E9clair", "eclair", 1, 1)]
    [InlineData("en-GB", "abc", "ABC", -1, 0)]
    [InlineData("en-GB", "\u30A2", "\u3042", 1, 1)]
    [InlineData("en-GB", "\uFF71", "\u30A2", 1, 1)]
    [InlineData("en-GB", "\uFF21\uFF22\uFF23", "\uFF41\uFF42\uFF43", 1, 0)]
    [InlineData("en-GB", "\uFF21\uFF22\uFF23", "ABC", 1, 1)]
    [InlineData("en-GB", "\u042F\u0431\u043B\u043E\u043A\u043E", "\u0401\u043B\u043A\u0430", 1, 1)]
    [InlineData("en-GB", "\u4E2D\u6587", "\u65E5\u672C", -1, -1)]
    [InlineData("en-GB", "", "a", -1, -1)]
    [InlineData("en-GB", "a", "a ", -1, -1)]
    [InlineData("fr-FR", "a_b", "a)b", -1, -1)]
    [InlineData("ru-RU", "a_b", "a)b", -1, -1)]
    [InlineData("ja-JP", "\u30A2", "\u3042", 0, 1)]
    [InlineData("ja-JP", "\uFF21\uFF22\uFF23", "ABC", 0, 1)]
    public void Two_names_sort_as_NET_10_sorts_them(string culture, string first, string second, int none, int ignoringCase)
    {
        var info = CultureInfo.GetCultureInfo(culture);

        Assert.Equal(none, Math.Sign(FrameworkCollation.Compare(first, second, info, CompareOptions.None)));
        Assert.Equal(ignoringCase, Math.Sign(FrameworkCollation.Compare(first, second, info, CompareOptions.IgnoreCase)));
        Assert.Equal(-none, Math.Sign(FrameworkCollation.Compare(second, first, info, CompareOptions.None)));
    }

    [Theory]
    [InlineData(null, "a", -1)]
    [InlineData("a", null, 1)]
    [InlineData(null, null, 0)]
    public void A_null_sorts_ahead_of_every_string(string? first, string? second, int expected)
    {
        Assert.Equal(expected, FrameworkCollation.Compare(first, second, CultureInfo.GetCultureInfo("en-GB"), CompareOptions.None));
    }

    [Fact]
    public void Options_the_port_does_not_carry_are_refused()
    {
        Assert.Throws<ArgumentException>(() =>
            FrameworkCollation.Compare("a", "b", CultureInfo.InvariantCulture, CompareOptions.IgnoreSymbols));
    }

    [Fact]
    public void Where_Windows_has_no_ICU_the_culture_sorts_through_NLS()
    {
        var culture = CultureInfo.GetCultureInfo("en-GB");

        Assert.Equal(
            Math.Sign(culture.CompareInfo.Compare("a_b", "a)b", CompareOptions.None)),
            Math.Sign(FrameworkCollation.For(culture, CompareOptions.None, icu: null)("a_b", "a)b")));
    }

    [Theory]
    [InlineData("en-GB", "en-GB", "en-GB")]
    [InlineData("de-DE_phoneb", "de-DE@collation=phoneb", "de-DE@collation=phoneb")]
    [InlineData("zh-Hans", "zh-Hans", "zh-Hans")]
    public void A_culture_name_reaches_ICU_as_NET_10_writes_it(string culture, string asked, string opened)
    {
        string? given = null;
        var name = FrameworkCollation.LocaleName(culture, n => { given = n; return n.Replace('-', '_'); });

        Assert.Equal(asked, given);
        Assert.Equal(opened, name);
    }

    [Fact]
    public void The_rules_for_ignoring_case_are_NET_10s()
    {
        var rules = FrameworkCollation.IgnoreCaseRules("&x<y");

        Assert.StartsWith("&x<y&\u3041<\u30A1&\u3042<\u30A2", rules, StringComparison.Ordinal);
        Assert.Contains("&\\!<\uFF01&\\\"<\uFF02", rules, StringComparison.Ordinal);
        Assert.Contains("&0<\uFF10", rules, StringComparison.Ordinal);
        Assert.Contains("&\u3164<\uFFA0", rules, StringComparison.Ordinal);
        Assert.Contains("&\uFF41=\uFF21&\uFF42=\uFF22", rules, StringComparison.Ordinal);
        Assert.EndsWith("&\uFF5A=\uFF3A&a=a", rules, StringComparison.Ordinal);
        Assert.Equal(FrameworkCollation.HalfFullLower.Length, FrameworkCollation.HalfFullHigher.Length);
    }

    [Theory]
    [InlineData("en-GB", "-yes", "y", false)]
    [InlineData("en-GB", "'yes", "y", false)]
    [InlineData("en-GB", "yes", "Y", true)]
    [InlineData("en-GB", "Yes", "yes!", false)]
    [InlineData("en-GB", "", "y", false)]
    [InlineData("en-GB", "o\u0308k", "o", false)]
    [InlineData("en-GB", "\u00ADyes", "y", true)]
    [InlineData("en-GB", "\uFF59es", "y", false)]
    [InlineData("en-GB", "y\u0301es", "y", false)]
    [InlineData("de-DE", "Ja", "j", true)]
    [InlineData("ru-RU", "\u0434\u0430", "\u0414", true)]
    [InlineData("tr-TR", "\u0131lk", "I", true)]
    [InlineData("tr-TR", "ilk", "\u0130", true)]
    [InlineData("tr-TR", "Ilk", "i", false)]
    [InlineData("fr-FR", "Oui", "o", true)]
    [InlineData("en-GB", "\u0001y", "y", true)]
    [InlineData("fr-FR", "\u0001y", "y", true)]
    [InlineData("fr-FR", "-oui", "o", false)]
    [InlineData("ja-JP", "\u30A2", "\u3042", false)]
    public void An_answer_starts_with_a_letter_as_NET_10_matches_it(string culture, string answer, string letter, bool starts)
    {
        Assert.Equal(starts, FrameworkCollation.StartsWithIgnoringCase(answer, letter, CultureInfo.GetCultureInfo(culture)));
    }

    /// <summary>
    /// Answers that start, or do not start, with a letter as .NET 10 matches them, each
    /// with .NET 10's answer: marks after the letter, characters ICU ignores ahead of it,
    /// other forms of the letter, and punctuation ahead of it.
    /// </summary>
    private static readonly (string Culture, string Answer, string Letter, bool Starts)[] Candidates =
    [
        ("en-GB", "\u006F\u0308\u006B", "o", false),
        ("en-GB", "\u0079\u0301\u0065\u0073", "y", false),
        ("en-GB", "\u0001\u0079", "y", true),
        ("en-GB", "\u0000\u0079", "y", true),
        ("en-GB", "\u000D\u000A\u0079", "y", false),
        ("en-GB", "\u200B\u0079", "y", true),
        ("en-GB", "\u200D\u0079", "y", true),
        ("en-GB", "\uFEFF\u0079", "y", true),
        ("en-GB", "\u0079\u200D", "y", true),
        ("en-GB", "\u2010\u0079", "y", false),
        ("en-GB", "y", "\u0079\u0308", false),
        ("en-GB", "\uFF39", "y", false),
        ("en-GB", "\u24E8", "y", true),
        ("en-GB", "\u00FD\u0065", "y", false),
        ("en-GB", "Yes", "\uFF59", false),
        ("fr-FR", "\u006F\u0308\u0075\u0069", "o", false),
        ("fr-FR", "\u200B\u006F", "o", true),
        ("de-DE", "\u006A\u0308\u0061", "j", false),
        ("ru-RU", "\u0435\u0308", "\u0415", false),
        ("ru-RU", "\u0451", "\u0415", false),
        ("de-DE", "\u00E4", "a", false),
        ("sv-SE", "\u00E4", "a", false),
        ("da-DK", "aa", "a", false),
        ("en-GB", "--y", "y", false),
        ("en-GB", "-yes", "y", false),
        ("en-GB", "'yes", "y", false),
    ];

    [Fact]
    public void Each_candidate_answer_starts_with_its_letter_as_NET_10_matches_it()
    {
        foreach (var (culture, answer, letter, starts) in Candidates)
            Assert.True(
                starts == FrameworkCollation.StartsWithIgnoringCase(answer, letter, CultureInfo.GetCultureInfo(culture)),
                $"{culture} {Hex(answer)} against {Hex(letter)}: .NET 10 answers {starts}.");
    }

    [Fact]
    public void NLS_answers_at_least_one_candidate_otherwise_than_NET_10()
    {
        // The difference the port exists for: .NET Framework's own IsPrefix matches through
        // NLS. Every candidate it answers, and how, is printed where none differs.
        var nls = Candidates.Select(c => (c, Answer: CultureInfo.GetCultureInfo(c.Culture).CompareInfo
            .IsPrefix(c.Answer, c.Letter, CompareOptions.IgnoreCase))).ToArray();

        Assert.True(
            nls.Any(n => n.Answer != n.c.Starts),
            "NLS answered every candidate as .NET 10 does: "
                + string.Join("; ", nls.Select(n => $"{n.c.Culture} {Hex(n.c.Answer)}/{Hex(n.c.Letter)} {n.Answer}")));
    }

    private static string Hex(string text) =>
        string.Join(" ", text.Select(c => ((int)c).ToString("X4", CultureInfo.InvariantCulture)));

    [Theory]
    [InlineData("yes", "Y", true)]
    [InlineData("Yes", "yes!", false)]
    [InlineData("", "y", false)]
    [InlineData("-y", "y", false)]
    [InlineData("\u0001y", "y", null)]
    [InlineData("y\u0301", "y", null)]
    [InlineData("\u00E9", "e", null)]
    public void Plain_ASCII_is_matched_by_letter_and_anything_near_another_character_goes_to_the_collator(string answer, string letter, bool? starts)
    {
        Assert.Equal(starts, FrameworkCollation.AsciiPrefixIgnoringCase(answer, letter));
    }
}
