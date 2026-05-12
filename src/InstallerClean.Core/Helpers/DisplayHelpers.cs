using System;
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
        >= 1_073_741_824 => $"{bytes / 1_073_741_824.0:F2} GB",
        >= 1_048_576 => $"{bytes / 1_048_576.0:F1} MB",
        >= 1_024 => $"{bytes / 1_024.0:F1} KB",
        _ => $"{bytes} B"
    };

    internal static string FormatElapsed(TimeSpan elapsed) =>
        elapsed.TotalSeconds < 1
            ? $"{elapsed.TotalMilliseconds:F0}ms"
            : $"{elapsed.TotalSeconds:F1}s";

    /// <summary>
    /// Natural-language elapsed time for body copy. Renders sub-second
    /// scans as "less than a second" and second-plus scans as
    /// "{N.N} seconds" so an all-clean overlay reads as a sentence
    /// rather than a CLI status pill. <see cref="FormatElapsed"/> stays
    /// the right call for the short-form metadata pills.
    /// </summary>
    internal static string FormatElapsedLong(TimeSpan elapsed) =>
        elapsed.TotalSeconds < 1
            ? "less than a second"
            : $"{elapsed.TotalSeconds:F1} seconds";

    /// <summary>
    /// Picks the singular or plural fragment per the English n != 1
    /// rule. Consumers pass resx-sourced fragments; this helper holds
    /// no English nouns of its own, so the body is the only site a
    /// richer pluraliser (Slavic case selection, Arabic dual, etc)
    /// would need to replace.
    /// </summary>
    internal static string Pluralise(int count, string singular, string plural) =>
        count == 1 ? singular : plural;

    /// <summary>"file"/"files" pair, sourced from Strings.resx.</summary>
    internal static string PluraliseFile(int count) =>
        Pluralise(count, Strings.Plural_File_Singular, Strings.Plural_File_Plural);

    /// <summary>"file is"/"files are" pair, sourced from Strings.resx.</summary>
    internal static string PluraliseFileVerb(int count) =>
        Pluralise(count, Strings.Plural_FileVerb_Singular, Strings.Plural_FileVerb_Plural);

    /// <summary>"error"/"errors" pair, sourced from Strings.resx.</summary>
    internal static string PluraliseError(int count) =>
        Pluralise(count, Strings.Plural_Error_Singular, Strings.Plural_Error_Plural);

    /// <summary>"package"/"packages" pair, sourced from Strings.resx.</summary>
    internal static string PluralisePackage(int count) =>
        Pluralise(count, Strings.Plural_Package_Singular, Strings.Plural_Package_Plural);

    /// <summary>"product"/"products" pair, sourced from Strings.resx.</summary>
    internal static string PluraliseProduct(int count) =>
        Pluralise(count, Strings.Plural_Product_Singular, Strings.Plural_Product_Plural);
}
