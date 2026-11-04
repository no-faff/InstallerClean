using InstallerClean.Models;
using InstallerClean.Resources;

namespace InstallerClean.Helpers;

/// <summary>
/// Renders how many files a scan left alone, as one counted phrase naming no cause.
///
/// It lives in Core rather than in either host because the two must answer identically
/// for one machine state and do not share the code that prints it: the window binds the
/// phrase under its unneeded count, the command line writes it to stdout after its
/// summary line. Both take the count from <see cref="ScanResult.LeftAloneCount"/>.
///
/// THE PHRASE IS THE MAIN WINDOW'S OWN STRING, so the command line cannot word it
/// differently. It carries the count and nothing else: no size, no cause, no full stop.
/// </summary>
internal static class LeftAloneReport
{
    /// <summary>The phrase for <paramref name="count"/> files, at a count of nought too.</summary>
    internal static string Line(int count) =>
        string.Format(
            DisplayHelpers.Pluralise(count,
                Strings.Summary_RegisteredStillUsed_Singular,
                Strings.Summary_RegisteredStillUsed_Plural,
                "Summary.RegisteredStillUsed"),
            DisplayHelpers.FormatCount(count));
}
