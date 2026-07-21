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
    // one section of it. For the fourteen translations that is For's URL
    // without the fragment; English is the one language where the two
    // differ, because an anchor works from the repository home and an
    // unanchored landing there does not (see ReadmeLinks.Home).

    [Theory]
    [InlineData("en-GB")]
    [InlineData("en-US")]
    [InlineData("nl-NL")]
    public void Home_links_the_english_readme_when_english_is_displayed(string culture)
        => Assert.Equal(
            "https://github.com/no-faff/InstallerClean/blob/main/README.md",
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
        Assert.StartsWith(
            "https://github.com/no-faff/InstallerClean/blob/main/README",
            url,
            StringComparison.Ordinal);
        Assert.EndsWith(".md", url, StringComparison.Ordinal);
    }

    /// <summary>
    /// The About window's star pill opens the repository home
    /// (<c>AboutWindow.StarClick</c>). The guide link sits a few lines above
    /// it in the same small window, so a language whose guide resolves to
    /// that same page gives the reader two differently-labelled controls
    /// onto one destination.
    /// </summary>
    [Theory]
    [MemberData(nameof(SupportedCultureNames))]
    public void Home_never_repeats_the_star_pills_destination(string cultureName)
        => Assert.NotEqual(
            "https://github.com/no-faff/InstallerClean",
            ReadmeLinks.Home(CultureInfo.GetCultureInfo(cultureName)));
}
