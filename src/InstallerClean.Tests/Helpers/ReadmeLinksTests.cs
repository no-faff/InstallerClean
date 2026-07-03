using System.Globalization;
using InstallerClean.Helpers;

namespace InstallerClean.Tests.Helpers;

public class ReadmeLinksTests
{
    [Theory]
    [InlineData("en-GB")]
    [InlineData("en-US")]
    [InlineData("nl-NL")] // unshipped language: the English README is what the app displays
    public void For_links_the_repository_home_when_english_is_displayed(string culture)
        => Assert.Equal(
            "https://github.com/no-faff/InstallerClean#is-it-safe",
            ReadmeLinks.For("is-it-safe", CultureInfo.GetCultureInfo(culture)));

    [Theory]
    [InlineData("it", "README.it.md")]
    [InlineData("ja-JP", "README.ja.md")]
    [InlineData("pt-BR", "README.pt-BR.md")]
    [InlineData("zh-CN", "README.zh-CN.md")] // zh-Hans satellite pairs with the zh-CN README
    [InlineData("zh-Hans", "README.zh-CN.md")]
    public void For_links_the_displayed_languages_readme(string culture, string readme)
        => Assert.Equal(
            $"https://github.com/no-faff/InstallerClean/blob/main/{readme}#recovery",
            ReadmeLinks.For("recovery", CultureInfo.GetCultureInfo(culture)));
}
