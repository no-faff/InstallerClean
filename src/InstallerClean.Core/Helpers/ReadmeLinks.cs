using System.Globalization;

namespace InstallerClean.Helpers;

/// <summary>
/// GitHub README links in the language the app is displaying. Every README
/// carries the same explicit anchors (<c>recovery</c>, <c>is-it-safe</c>,
/// <c>reports-stats</c>), so a link built here lands on the section in the
/// reader's own language rather than sending every user to the English page.
/// </summary>
public static class ReadmeLinks
{
    private const string Repo = "https://github.com/no-faff/InstallerClean";

    /// <summary>
    /// The whole README matching the displayed language, with no anchor.
    /// Every language gets the rendered document itself, English included:
    /// the repository home renders README.md too, but it opens below the
    /// file list rather than at the top of the document, and it is already
    /// where the About window's star pill goes, so the guide link beside it
    /// would repeat a page the same window already offers.
    /// </summary>
    public static string Home(CultureInfo uiCulture) => Build(anchor: null, uiCulture);

    /// <summary>
    /// The URL of <paramref name="anchor"/> in the README matching the
    /// displayed language.
    /// </summary>
    public static string For(string anchor, CultureInfo uiCulture) => Build(anchor, uiCulture);

    private static string Build(string? anchor, CultureInfo uiCulture)
    {
        var fragment = anchor is null ? string.Empty : $"#{anchor}";
        var active = SupportedLanguages.Active(uiCulture);

        // An anchored English link keeps the repository home: GitHub renders
        // README.md there and the fragment still lands on the section, so the
        // shorter URL is the one worth showing. Unanchored, that same page
        // lands the reader above the document rather than in it, so English
        // names the file like every other language does (see Home).
        if (string.Equals(active, SupportedLanguages.Neutral, StringComparison.OrdinalIgnoreCase))
            return anchor is null ? $"{Repo}/blob/main/README.md" : $"{Repo}{fragment}";

        // The one name mismatch is deliberate: the zh-Hans satellite pairs
        // with README.zh-CN.md, because the README family is named by
        // locale and the satellites by script.
        var readme = string.Equals(active, "zh-Hans", StringComparison.OrdinalIgnoreCase)
            ? "zh-CN"
            : active;
        return $"{Repo}/blob/main/README.{readme}.md{fragment}";
    }
}
