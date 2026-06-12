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
        >= 1_073_741_824 => string.Format(Strings.Display_Size_GB, bytes / 1_073_741_824.0),
        >= 1_048_576 => string.Format(Strings.Display_Size_MB, bytes / 1_048_576.0),
        >= 1_024 => string.Format(Strings.Display_Size_KB, bytes / 1_024.0),
        _ => string.Format(Strings.Display_Size_B, bytes)
    };

    internal static string FormatElapsed(TimeSpan elapsed) =>
        elapsed.TotalSeconds < 1
            ? string.Format(Strings.Display_Elapsed_Ms, elapsed.TotalMilliseconds)
            : string.Format(Strings.Display_Elapsed_S, elapsed.TotalSeconds);

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

    /// <summary>"error"/"errors" pair, sourced from Strings.resx.</summary>
    internal static string PluraliseError(int count) =>
        Pluralise(count, Strings.Plural_Error_Singular, Strings.Plural_Error_Plural);

    /// <summary>"package"/"packages" pair, sourced from Strings.resx.</summary>
    internal static string PluralisePackage(int count) =>
        Pluralise(count, Strings.Plural_Package_Singular, Strings.Plural_Package_Plural);

    /// <summary>"product"/"products" pair, sourced from Strings.resx.</summary>
    internal static string PluraliseProduct(int count) =>
        Pluralise(count, Strings.Plural_Product_Singular, Strings.Plural_Product_Plural);

    internal static string PluralisePatch(int count) =>
        Pluralise(count, Strings.Plural_Patch_Singular, Strings.Plural_Patch_Plural);
}
