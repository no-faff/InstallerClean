using System;
using System.Globalization;
using System.Reflection;
using InstallerClean.Resources;

namespace InstallerClean.Helpers;

internal static class DisplayHelpers
{
    internal static string GetVersionString()
    {
        var version = Assembly.GetExecutingAssembly().GetName().Version;
        return version is not null
            ? string.Format(Strings.Version_Display, $"{version.Major}.{version.Minor}.{version.Build}")
            : string.Empty;
    }

    internal static string FormatSize(long bytes) => bytes switch
    {
        >= 1_073_741_824 => string.Format(Localisation.FormatCulture, Strings.Display_Size_GB, bytes / 1_073_741_824.0),
        >= 1_048_576 => string.Format(Localisation.FormatCulture, Strings.Display_Size_MB, bytes / 1_048_576.0),
        >= 1_024 => string.Format(Localisation.FormatCulture, Strings.Display_Size_KB, bytes / 1_024.0),
        _ => string.Format(Localisation.FormatCulture, Strings.Display_Size_B, bytes)
    };

    internal static string FormatElapsed(TimeSpan elapsed) =>
        elapsed.TotalSeconds < 1
            ? string.Format(Localisation.FormatCulture, Strings.Display_Elapsed_Ms, elapsed.TotalMilliseconds)
            : string.Format(Localisation.FormatCulture, Strings.Display_Elapsed_S, elapsed.TotalSeconds);

    /// <summary>
    /// Natural-language elapsed time for body copy. Renders sub-second
    /// scans as "less than a second" and second-plus scans as
    /// "{N.N} seconds" so an all-clean overlay reads as a sentence
    /// rather than a CLI status pill. <see cref="FormatElapsed"/> stays
    /// the right call for the short-form metadata pills.
    /// </summary>
    internal static string FormatElapsedLong(TimeSpan elapsed) =>
        elapsed.TotalSeconds < 1
            ? Strings.Display_ElapsedLong_LessThanASecond
            : string.Format(Strings.Display_ElapsedLong_Seconds, elapsed.TotalSeconds);

    internal enum PluralCategory { One, Few, Many, Other }

    /// <summary>
    /// Unicode CLDR plural category for <paramref name="culture"/>, so a language
    /// with more than English's one/other split (Russian's 2-4 "few", etc) selects
    /// the right fragment. Integer counts only. Takes the culture explicitly so it
    /// is testable without process-global state.
    /// </summary>
    internal static PluralCategory CategoryFor(CultureInfo culture, int count)
    {
        int n = count < 0 ? -count : count;
        switch (culture.TwoLetterISOLanguageName)
        {
            case "ru":
            case "uk": // East Slavic: one / few / many
                int mod10 = n % 10, mod100 = n % 100;
                if (mod10 == 1 && mod100 != 11) return PluralCategory.One;
                if (mod10 >= 2 && mod10 <= 4 && (mod100 < 12 || mod100 > 14)) return PluralCategory.Few;
                return PluralCategory.Many;
            case "fr":
            case "pt": // 0 and 1 are singular
                return n is 0 or 1 ? PluralCategory.One : PluralCategory.Other;
            case "ja":
            case "ko":
            case "zh": // no count inflection
                return PluralCategory.Other;
            default: // en, de, es, it, ...: singular only at exactly 1
                return n == 1 ? PluralCategory.One : PluralCategory.Other;
        }
    }

    /// <summary>
    /// Picks the count fragment for <paramref name="count"/> in the current UI
    /// language. <paramref name="singular"/>/<paramref name="plural"/> are the resx
    /// one/other forms; a language needing the few/many categories supplies them as
    /// <c>{keyPrefix}.Few</c> / <c>.Many</c> in its own satellite, read here by name
    /// (an absent one falls back to plural). <paramref name="keyPrefix"/> is the resx
    /// key without the form suffix and MUST match it exactly, or the few/many lookup
    /// silently misses and falls back to plural.
    /// </summary>
    internal static string Pluralise(int count, string singular, string plural, string keyPrefix) =>
        CategoryFor(Localisation.UiCulture, count) switch
        {
            PluralCategory.One => singular,
            PluralCategory.Few => Strings.ResourceManager.GetString($"{keyPrefix}.Few", Localisation.UiCulture) ?? plural,
            PluralCategory.Many => Strings.ResourceManager.GetString($"{keyPrefix}.Many", Localisation.UiCulture) ?? plural,
            _ => plural,
        };

    /// <summary>"file"/"files" pair, sourced from Strings.resx.</summary>
    internal static string PluraliseFile(int count) =>
        Pluralise(count, Strings.Plural_File_Singular, Strings.Plural_File_Plural, "Plural.File");

    /// <summary>"error"/"errors" pair, sourced from Strings.resx.</summary>
    internal static string PluraliseError(int count) =>
        Pluralise(count, Strings.Plural_Error_Singular, Strings.Plural_Error_Plural, "Plural.Error");

    /// <summary>"package"/"packages" pair, sourced from Strings.resx.</summary>
    internal static string PluralisePackage(int count) =>
        Pluralise(count, Strings.Plural_Package_Singular, Strings.Plural_Package_Plural, "Plural.Package");

    /// <summary>"product"/"products" pair, sourced from Strings.resx.</summary>
    internal static string PluraliseProduct(int count) =>
        Pluralise(count, Strings.Plural_Product_Singular, Strings.Plural_Product_Plural, "Plural.Product");

    internal static string PluralisePatch(int count) =>
        Pluralise(count, Strings.Plural_Patch_Singular, Strings.Plural_Patch_Plural, "Plural.Patch");
}
