using System.Globalization;

namespace InstallerClean.Helpers;

/// <summary>
/// GitHub README links in the language the app is displaying. Every README
/// carries the same explicit anchors (<c>recovery</c>, <c>is-it-safe</c>), so
/// a link built here lands on the section in the reader's own language rather
/// than sending every user to the English page.
/// </summary>
public static class ReadmeLinks
{
    private const string Repo = "https://github.com/no-faff/InstallerClean";

    /// <summary>
    /// The URL of <paramref name="anchor"/> in the README matching the
    /// displayed language: the repository home for English (GitHub renders
    /// README.md there), the rendered translated README otherwise. The one
    /// name mismatch is deliberate: the zh-Hans satellite pairs with
    /// README.zh-CN.md, because the README family is named by locale and the
    /// satellites by script.
    /// </summary>
    public static string For(string anchor, CultureInfo uiCulture)
    {
        var active = SupportedLanguages.Active(uiCulture);
        if (string.Equals(active, SupportedLanguages.Neutral, StringComparison.OrdinalIgnoreCase))
            return $"{Repo}#{anchor}";

        var readme = string.Equals(active, "zh-Hans", StringComparison.OrdinalIgnoreCase)
            ? "zh-CN"
            : active;
        return $"{Repo}/blob/main/README.{readme}.md#{anchor}";
    }
}
