using System.Globalization;

namespace InstallerClean.Helpers;

/// <summary>
/// Culture names the app ships a UI translation for. <see cref="Neutral"/>
/// (English) is the neutral resx; every other entry has a
/// <c>Strings.&lt;name&gt;.resx</c> satellite. Adding a language: ship the
/// satellite resx, add its name here, and add its endonym to the bottom-bar
/// language menu (MainWindow's LanguageChoices). This is the validation list
/// the language preference is checked against, so an unsupported value in
/// settings.json falls back to Automatic rather than selecting a culture the
/// app has no translation for.
/// </summary>
public static class SupportedLanguages
{
    /// <summary>The neutral resx culture, displayed for any culture without a satellite.</summary>
    public const string Neutral = "en-GB";

    public static readonly IReadOnlyList<string> CultureNames = new[] { Neutral, "it" };

    /// <summary>
    /// The supported-language name the app is actually displaying for
    /// <paramref name="uiCulture"/>: an exact or language-level match (so
    /// it-IT and it-CH both map to <c>"it"</c>), else <see cref="Neutral"/>,
    /// which every culture without a satellite resolves to. The active
    /// language must be read from the displayed culture, not from an explicit
    /// override alone: a default install carries no override and follows the
    /// OS, yet still displays one of these languages, so the globe menu's
    /// tick and its re-pick-is-a-no-op both depend on this.
    /// </summary>
    public static string Active(CultureInfo uiCulture)
        => CultureNames.FirstOrDefault(name =>
               string.Equals(name, uiCulture.Name, StringComparison.OrdinalIgnoreCase)
               || string.Equals(name, uiCulture.TwoLetterISOLanguageName, StringComparison.OrdinalIgnoreCase))
           ?? Neutral;
}
