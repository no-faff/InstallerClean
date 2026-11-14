using System.Windows.Media;

namespace InstallerClean.Helpers;

/// <summary>
/// The font family each of the app's languages is drawn in, so that every
/// screen is in one family. A language whose letters Poppins carries is drawn
/// in the theme's own Type.FontFamily, Poppins then Montserrat then Segoe UI.
/// Russian, Ukrainian and Vietnamese, whose letters Poppins lacks, are drawn in
/// Montserrat. Chinese, Japanese and Korean are drawn in the interface font
/// Windows ships for each, Latin letters and digits included.
///
/// <para>
/// A line's height and the depth of its baseline come from the first family
/// in the list, times the text size. Poppins sets 1.5 and 1.1, and the bundled
/// Montserrat files carry the same values (scripts/fonts/make-fonts.py
/// writes them). The Windows fonts set their own, between 1.27 and 1.33, and
/// cannot be edited, so each of those is a composite family built here
/// with its LineSpacing and Baseline taken from the theme family. WPF lets the
/// app set those two only on a composite. So a line is the same height in
/// every language.
/// </para>
///
/// <para>
/// Segoe UI comes last in each family and takes a character the others lack,
/// from a product name or a path. Naming it keeps that character on the Windows
/// interface face rather than on whichever face the system's font fallback
/// picks for its script.
/// </para>
///
/// <para>
/// scripts/check-font-coverage.mjs reads the lists here and in Tokens.xaml and
/// fails CI where a language's text or its name in the language menu has a
/// character missing from any face of its first family, or where a bundled
/// face sets another line height or baseline.
/// </para>
/// </summary>
public static class LanguageFonts
{
    private static readonly string[] MontserratCultures = { "ru", "uk", "vi" };

    // Relative to the application's pack URI, which is how a family list built
    // in code reaches a font compiled in as a resource.
    private const string MontserratFamily = "./Fonts/#Montserrat, Segoe UI";

    // Each is in the default install of every Windows the setup admits.
    private static readonly Dictionary<string, string> WindowsFamilies = new(StringComparer.OrdinalIgnoreCase)
    {
        ["zh-Hans"] = "Microsoft YaHei UI, Segoe UI",
        ["zh-Hant"] = "Microsoft JhengHei UI, Segoe UI",
        ["ja"] = "Yu Gothic UI, Segoe UI",
        ["ko"] = "Malgun Gothic, Segoe UI",
    };

    private static readonly Dictionary<string, FontFamily> Families = new(StringComparer.OrdinalIgnoreCase);
    private static FontFamily? _theme;

    /// <summary>
    /// Builds the families from <paramref name="theme"/>, the value of
    /// Type.FontFamily in Tokens.xaml. Call it once, before the first
    /// <see cref="For"/>.
    /// </summary>
    public static void Initialise(FontFamily theme)
    {
        _theme = theme;

        var montserrat = new FontFamily(new Uri("pack://application:,,,/"), MontserratFamily);
        foreach (var culture in MontserratCultures)
            Families[culture] = montserrat;

        foreach (var (culture, targets) in WindowsFamilies)
        {
            var composite = new FontFamily
            {
                LineSpacing = theme.LineSpacing,
                Baseline = theme.Baseline,
            };
            // A FontFamilyMap covers the whole of Unicode unless given ranges,
            // so every character goes to the target list.
            composite.FamilyMaps.Add(new FontFamilyMap { Target = targets });
            Families[culture] = composite;
        }
    }

    /// <summary>
    /// The family to draw <paramref name="culture"/> in, one of the names in
    /// SupportedLanguages.CultureNames. A language not named here takes the
    /// theme family.
    /// </summary>
    public static FontFamily For(string culture)
    {
        var theme = _theme ?? throw new InvalidOperationException(
            "LanguageFonts.Initialise has not run, so there is no theme family to fall back to.");
        return Families.TryGetValue(culture, out var family) ? family : theme;
    }
}
