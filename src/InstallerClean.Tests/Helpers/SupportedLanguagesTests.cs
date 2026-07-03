using System.Globalization;
using InstallerClean.Helpers;

namespace InstallerClean.Tests.Helpers;

public class SupportedLanguagesTests
{
    [Theory]
    [InlineData("en-GB", "en-GB")]
    [InlineData("pt-BR", "pt-BR")]
    [InlineData("zh-Hans", "zh-Hans")]
    public void Active_matches_a_shipped_name_exactly(string culture, string expected)
        => Assert.Equal(expected, SupportedLanguages.Active(CultureInfo.GetCultureInfo(culture)));

    [Theory]
    [InlineData("it-CH", "it")]
    [InlineData("ja-JP", "ja")]
    [InlineData("de-AT", "de")]
    public void Active_matches_a_regional_culture_to_its_language(string culture, string expected)
        => Assert.Equal(expected, SupportedLanguages.Active(CultureInfo.GetCultureInfo(culture)));

    // zh-CN's ISO language name is "zh", which matches no shipped entry, yet
    // its resources resolve through the zh-Hans parent and render Chinese; the
    // reported language has to walk the same chain or the globe menu ticks
    // English on a Chinese UI and treats a click on English as a no-op.
    [Theory]
    [InlineData("zh-CN", "zh-Hans")]
    [InlineData("zh-SG", "zh-Hans")]
    public void Active_matches_through_the_script_level_parent(string culture, string expected)
        => Assert.Equal(expected, SupportedLanguages.Active(CultureInfo.GetCultureInfo(culture)));

    [Theory]
    [InlineData("en-US")] // no en satellite; the neutral resx is the display
    [InlineData("pt-PT")] // pt-BR is not on pt-PT's fallback chain
    [InlineData("zh-TW")] // Traditional Chinese is not shipped
    [InlineData("nl-NL")] // no Dutch translation at all
    public void Active_reports_neutral_for_a_culture_that_displays_English(string culture)
        => Assert.Equal(SupportedLanguages.Neutral, SupportedLanguages.Active(CultureInfo.GetCultureInfo(culture)));

    [Fact]
    public void Active_reports_neutral_for_the_invariant_culture()
        => Assert.Equal(SupportedLanguages.Neutral, SupportedLanguages.Active(CultureInfo.InvariantCulture));
}
