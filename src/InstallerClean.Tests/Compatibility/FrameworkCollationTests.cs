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
            Math.Sign(FrameworkCollation.Compare("a_b", "a)b", culture, CompareOptions.None, icu: null)));
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
}
