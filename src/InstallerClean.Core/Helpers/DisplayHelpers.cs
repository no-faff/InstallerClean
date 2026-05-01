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
    /// English plural rule (n != 1 takes the plural form). Consumers pass
    /// resx-sourced singular and plural fragments; this helper has no
    /// hardcoded English nouns of its own. For a future Slavic / Arabic
    /// translation a richer pluraliser would replace the body without
    /// touching call sites.
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
}
