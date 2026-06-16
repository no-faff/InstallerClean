using System.Globalization;

namespace InstallerClean.Helpers;

/// <summary>
/// Resolves the persisted <c>AppSettings.Language</c> string to a culture.
/// </summary>
public static class LanguagePreference
{
    /// <summary>
    /// The culture for an explicit, supported language name, or <c>null</c>
    /// for Automatic (null/blank) or any value not in
    /// <see cref="SupportedLanguages.CultureNames"/>. Never throws; an
    /// unrecognised value resolves to <c>null</c> so startup falls back to
    /// the OS culture rather than failing.
    /// </summary>
    public static CultureInfo? Resolve(string? setting)
    {
        if (string.IsNullOrWhiteSpace(setting))
            return null;
        if (!SupportedLanguages.CultureNames.Contains(setting, StringComparer.OrdinalIgnoreCase))
            return null;
        return CultureInfo.GetCultureInfo(setting);
    }
}
