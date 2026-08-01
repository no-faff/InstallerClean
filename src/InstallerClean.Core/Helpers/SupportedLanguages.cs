using System.Globalization;

namespace InstallerClean.Helpers;

/// <summary>
/// Culture names the app ships a UI translation for. <see cref="Neutral"/>
/// (English) is the neutral resx; every other entry has a
/// <c>Strings.&lt;name&gt;.resx</c> satellite. Adding a language: ship the
/// satellite resx and add its name here. That is the whole step. This list is
/// what the bottom-bar language menu is built from, so a language added here
/// is selectable without touching the menu, and it is the validation list the
/// saved language preference is checked against, so an unsupported value in
/// settings.json falls back to Automatic rather than selecting a culture the
/// app has no translation for. The menu carries a hand-written endonym per
/// language and falls back to the framework's own name for one it does not
/// know, which is a cosmetic gap rather than a missing entry.
/// </summary>
public static class SupportedLanguages
{
    /// <summary>The neutral resx culture, displayed for any culture without a satellite.</summary>
    public const string Neutral = "en-GB";

    public static readonly IReadOnlyList<string> CultureNames = new[]
    {
        Neutral, "zh-Hans", "ru", "es", "ja", "pt-BR", "pl", "tr",
        "ko", "fr", "it", "de", "id", "vi", "uk", "nl",
    };

    /// <summary>
    /// The supported-language name the app is actually displaying for
    /// <paramref name="uiCulture"/>: the first entry on the culture's parent
    /// chain, the same chain satellite resolution probes (it-CH falls back to
    /// <c>"it"</c>; zh-CN to zh-Hans, so it maps to <c>"zh-Hans"</c>), else
    /// <see cref="Neutral"/>, which every culture without a satellite
    /// resolves to. A two-letter comparison is not enough: zh-CN's ISO
    /// language name is "zh", which matches no entry, yet its resources
    /// resolve through zh-Hans, so the app renders Chinese while a two-letter
    /// check reports English. The active language must be read from the
    /// displayed culture, not from an explicit override alone: a default
    /// install carries no override and follows the OS, yet still displays one
    /// of these languages, so the globe menu's tick and its re-pick-is-a-no-op
    /// both depend on this.
    /// </summary>
    public static string Active(CultureInfo uiCulture)
    {
        for (var culture = uiCulture;
             !string.IsNullOrEmpty(culture.Name);
             culture = culture.Parent)
        {
            var match = CultureNames.FirstOrDefault(name =>
                string.Equals(name, culture.Name, StringComparison.OrdinalIgnoreCase));
            if (match is not null)
                return match;
        }

        return Neutral;
    }
}
