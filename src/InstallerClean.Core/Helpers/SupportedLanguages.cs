namespace InstallerClean.Helpers;

/// <summary>
/// Culture names the app ships a UI translation for. <c>"en-GB"</c> is the
/// neutral (English) resx; every other entry has a
/// <c>Strings.&lt;name&gt;.resx</c> satellite. Adding a language: ship the
/// satellite resx and add its name here. This is the validation list the
/// language preference is checked against, so an unsupported value in
/// settings.json falls back to Automatic rather than selecting a culture the
/// app has no translation for.
/// </summary>
public static class SupportedLanguages
{
    public static readonly IReadOnlyList<string> CultureNames = new[] { "en-GB", "it" };
}
