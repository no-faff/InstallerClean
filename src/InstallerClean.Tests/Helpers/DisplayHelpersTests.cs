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
