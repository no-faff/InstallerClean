using System.Globalization;
using InstallerClean.Helpers;

namespace InstallerClean.Tests.Helpers;

public class DisplayHelpersTests
{
    [Theory]
    [InlineData(0, "0 B")]
    [InlineData(512, "512 B")]
    [InlineData(1_023, "1023 B")]
    [InlineData(1_024, "1.0 KB")]
    [InlineData(5_500, "5.4 KB")]
    [InlineData(1_048_576, "1.0 MB")]
    [InlineData(52_428_800, "50.0 MB")]
    [InlineData(1_073_741_824, "1.00 GB")]
    [InlineData(5_368_709_120, "5.00 GB")]
    [InlineData(107_374_182_400, "100.00 GB")]
    public void FormatSize_formats_correctly_in_en_US(long bytes, string expected)
    {
        using var scope = new CultureScope(CultureInfo.GetCultureInfo("en-US"));
        Assert.Equal(expected, DisplayHelpers.FormatSize(bytes));
    }

    [Theory]
    [InlineData(0, "files")]
    [InlineData(1, "file")]
    [InlineData(2, "files")]
    [InlineData(100, "files")]
    public void Pluralise_returns_correct_form(int count, string expected)
    {
        // English (the test host culture) only ever resolves One/Other, so the
        // key prefix's Few/Many lookup is never read; any prefix works here.
        Assert.Equal(expected, DisplayHelpers.Pluralise(count, "file", "files", "Plural.File"));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(5)]
    public void Pluralise_flat_overload_equals_the_doubled_form(int count)
    {
        // The flat overload takes ONE string, so it cannot be handed two
        // different ones the way the doubled call sites could be. It must behave
        // exactly like the three-string form called with that string twice. A
        // prefix with no resx overrides makes every plural category fall back to
        // the flat string, so this holds in any UI culture.
        //
        // A REAL PREFIX RATHER THAN A MADE-UP ONE, AND THAT IS NOT TIDINESS.
        // DisplayHelpers.QuestionFor has no default arm: a prefix nobody has
        // classified throws rather than being absorbed, which is what stops the
        // inventory being silently short. "Test.FlatOverload" was invented here and
        // could never have exercised the override lookup this test describes, since
        // no satellite has ever held a key by that name in any form. This one is
        // passed by the app, carries no override in any of the fifteen languages,
        // and so falls back in every category exactly as the comment above says.
        const string flat = "Found {0} registered {1}.";
        const string prefix = "Completion.MoveCancelledSummary";
        Assert.Equal(
            DisplayHelpers.Pluralise(count, flat, flat, prefix),
            DisplayHelpers.Pluralise(count, flat, prefix));
        Assert.Equal(flat, DisplayHelpers.Pluralise(count, flat, prefix));
    }

    [Theory]
    [InlineData(1, "One")]
    [InlineData(21, "One")]
    [InlineData(101, "One")]
    [InlineData(2, "Few")]
    [InlineData(4, "Few")]
    [InlineData(22, "Few")]
    [InlineData(5, "Many")]
    [InlineData(11, "Many")]
    [InlineData(12, "Many")]
    [InlineData(25, "Many")]
    [InlineData(111, "Many")]
    public void CategoryFor_russian_selects_one_few_many(int n, string expected)
    {
        Assert.Equal(expected, DisplayHelpers.CategoryFor(new CultureInfo("ru"), n).ToString());
    }

    [Theory]
    [InlineData(1, "One")]
    [InlineData(2, "Few")]
    [InlineData(4, "Few")]
    [InlineData(22, "Few")]
    [InlineData(24, "Few")]
    [InlineData(0, "Many")]
    [InlineData(5, "Many")]
    [InlineData(11, "Many")]
    [InlineData(14, "Many")]
    [InlineData(25, "Many")]
    public void CategoryFor_polish_selects_one_few_many(int n, string expected)
    {
        Assert.Equal(expected, DisplayHelpers.CategoryFor(new CultureInfo("pl"), n).ToString());
    }

    [Theory]
    [InlineData(21)]
    [InlineData(101)]
    [InlineData(31)]
    public void CategoryFor_polish_parts_company_with_east_slavic_above_twenty(int n)
    {
        // The one this file exists for. Polish "one" is strictly n == 1, where
        // Russian and Ukrainian also take 21, 31 and 101, and the two branches
        // sit next to each other reading almost alike: folding Polish into the
        // one above it would make every Polish count ending in 1 read as a
        // singular, and nothing but the code's own comment stood in the way.
        Assert.Equal("Many", DisplayHelpers.CategoryFor(new CultureInfo("pl"), n).ToString());
        Assert.Equal("One", DisplayHelpers.CategoryFor(new CultureInfo("ru"), n).ToString());
        Assert.Equal("One", DisplayHelpers.CategoryFor(new CultureInfo("uk"), n).ToString());
    }

    [Theory]
    [InlineData("fr", 0, "One")]
    [InlineData("fr", 1, "One")]
    [InlineData("fr", 2, "Other")]
    [InlineData("pt", 0, "One")]
    [InlineData("pt", 1, "One")]
    [InlineData("pt", 2, "Other")]
    public void CategoryFor_french_and_portuguese_take_zero_as_singular(string culture, int n, string expected)
    {
        Assert.Equal(expected, DisplayHelpers.CategoryFor(new CultureInfo(culture), n).ToString());
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(5)]
    public void CategoryFor_turkish_never_inflects(int n)
    {
        // A Turkish noun stays singular after a numeral, so the count sentence
        // does not inflect at all and "one" would be the wrong fragment even at
        // one.
        Assert.Equal("Other", DisplayHelpers.CategoryFor(new CultureInfo("tr"), n).ToString());
    }

    [Theory]
    [InlineData("ja")]
    [InlineData("ko")]
    [InlineData("zh")]
    [InlineData("id")]
    [InlineData("vi")]
    public void CategoryFor_uninflected_languages_are_always_other(string culture)
    {
        foreach (var n in new[] { 0, 1, 2, 5, 21 })
            Assert.Equal("Other", DisplayHelpers.CategoryFor(new CultureInfo(culture), n).ToString());
    }

    [Theory]
    [InlineData("en")]
    [InlineData("de")]
    [InlineData("es")]
    [InlineData("it")]
    [InlineData("nl")]
    public void CategoryFor_defaults_to_singular_only_at_one(string culture)
    {
        Assert.Equal("One", DisplayHelpers.CategoryFor(new CultureInfo(culture), 1).ToString());
        foreach (var n in new[] { 0, 2, 5, 21 })
            Assert.Equal("Other", DisplayHelpers.CategoryFor(new CultureInfo(culture), n).ToString());
    }

    [Theory]
    [InlineData("de-DE", "1,0 KB")]
    [InlineData("fr-FR", "1,0 KB")]
    [InlineData("en-GB", "1.0 KB")]
    [InlineData("ja-JP", "1.0 KB")]
    public void FormatSize_follows_system_culture_for_decimal_separator(string cultureName, string expected)
    {
        using var scope = new CultureScope(CultureInfo.GetCultureInfo(cultureName));
        Assert.Equal(expected, DisplayHelpers.FormatSize(1024));
    }

    [Fact]
    public void FormatSize_never_throws_across_many_cultures()
    {
        var cultures = new[] { "en-US", "en-GB", "de-DE", "fr-FR", "ja-JP", "tr-TR", "ar-SA", "hi-IN" };
        foreach (var name in cultures)
        {
            using var scope = new CultureScope(CultureInfo.GetCultureInfo(name));
            var _ = DisplayHelpers.FormatSize(1_073_741_824);
        }
    }

    private sealed class CultureScope : IDisposable
    {
        private readonly CultureInfo _previous;

        public CultureScope(CultureInfo culture)
        {
            _previous = CultureInfo.CurrentCulture;
            CultureInfo.CurrentCulture = culture;
        }

        public void Dispose() => CultureInfo.CurrentCulture = _previous;
    }
}
