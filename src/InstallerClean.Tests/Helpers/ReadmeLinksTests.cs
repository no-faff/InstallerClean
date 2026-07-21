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

    // Home is the About window's guide link: the whole document rather than
    // one section of it, so the only difference from For is the missing
    // fragment. The per-language file has to stay identical, because the two
    // are the same map and a divergence would send one link to a page the
    // other says does not exist.

    [Theory]
    [InlineData("en-GB")]
    [InlineData("en-US")]
    [InlineData("nl-NL")]
    public void Home_links_the_repository_home_when_english_is_displayed(string culture)
        => Assert.Equal(
            "https://github.com/no-faff/InstallerClean",
            ReadmeLinks.Home(CultureInfo.GetCultureInfo(culture)));

    [Theory]
    [InlineData("it", "README.it.md")]
    [InlineData("ja-JP", "README.ja.md")]
    [InlineData("pt-BR", "README.pt-BR.md")]
    [InlineData("zh-CN", "README.zh-CN.md")]
    [InlineData("zh-Hans", "README.zh-CN.md")]
    public void Home_links_the_displayed_languages_readme(string culture, string readme)
        => Assert.Equal(
            $"https://github.com/no-faff/InstallerClean/blob/main/{readme}",
            ReadmeLinks.Home(CultureInfo.GetCultureInfo(culture)));

    public static TheoryData<string> SupportedCultureNames()
    {
        var data = new TheoryData<string>();
        foreach (var name in SupportedLanguages.CultureNames)
            data.Add(name);
        return data;
    }

    /// <summary>
    /// The failure this guards is the quiet one. A language added to
    /// <see cref="SupportedLanguages.CultureNames"/> whose README is not
    /// named to the convention still yields a working-looking URL, just a
    /// 404, and nothing on screen or in a build would say so.
    /// </summary>
    [Theory]
    [MemberData(nameof(SupportedCultureNames))]
    public void Every_supported_language_has_its_own_readme(string cultureName)
    {
        var url = ReadmeLinks.Home(CultureInfo.GetCultureInfo(cultureName));

        Assert.True(Uri.TryCreate(url, UriKind.Absolute, out var uri));
        Assert.Equal(Uri.UriSchemeHttps, uri!.Scheme);

        var isEnglish = cultureName == SupportedLanguages.Neutral;
        Assert.Equal(isEnglish, url == "https://github.com/no-faff/InstallerClean");
        if (!isEnglish)
            Assert.EndsWith(".md", url, StringComparison.Ordinal);
    }
}
