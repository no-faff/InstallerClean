using InstallerClean.Resources;
using InstallerClean.Services;

namespace InstallerClean.Helpers;

/// <summary>
/// Renders what a Move or Delete kept back, as one sentence per cause that
/// actually occurred, each carrying its own count.
///
/// It lives in Core rather than in either host because the two hosts must answer
/// identically for one machine state, and they do not share the code that prints
/// it: the window composes a block for the completion overlay, the command line
/// writes a line at a time to stdout. The rule has drifted between them before,
/// three of the command line's held-back paths having gone without a fold the
/// window had on all three of its own. Rendering here leaves each host with only
/// its own joining to do, so the partition itself cannot come apart.
///
/// A partition rather than one sentence: the three causes have no honest
/// superordinate. One is a confirmed positive (a program needs the file), one is
/// an inability (the records could not be read) and one is neither (the records
/// no longer hold the registration), so any sentence covering all three either
/// says nothing or says something false of two of them.
/// </summary>
internal static class HeldBackReport
{
    /// <summary>
    /// One sentence per cause present in <paramref name="reasons"/>, in the order
    /// they are meant to be read, and empty when nothing was kept back.
    ///
    /// The order is most specific cause first: what a program was found to need,
    /// then what the records no longer hold, then what could not be read at all.
    /// It is fixed here rather than at each call site so both hosts read the same
    /// way round.
    /// </summary>
    internal static IReadOnlyList<string> Lines(HeldBackReasons reasons)
    {
        if (reasons.Total == 0) return Array.Empty<string>();

        var lines = new List<string>(3);
        Add(lines, reasons.Reclaimed,
            Strings.Completion_ReverifySkipped, "Completion.ReverifySkipped");
        Add(lines, reasons.RecordsChanged,
            Strings.Completion_ReverifyRecordsChanged, "Completion.ReverifyRecordsChanged");
        Add(lines, reasons.RecordsUnreadable,
            Strings.Completion_ReverifyIncomplete, "Completion.ReverifyIncomplete");
        return lines.AsReadOnly();
    }

    /// <summary>
    /// Appends one cause's sentence, or nothing at a count of zero: a cause that
    /// did not occur has no line, which is the whole point of the partition.
    /// </summary>
    private static void Add(List<string> lines, int count, string flat, string key)
    {
        if (count == 0) return;
        lines.Add(string.Format(
            DisplayHelpers.Pluralise(count, flat, key),
            count, DisplayHelpers.PluraliseFile(count)));
    }
}
