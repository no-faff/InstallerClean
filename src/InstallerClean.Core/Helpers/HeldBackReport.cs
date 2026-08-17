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
/// A partition rather than one sentence: the causes have no honest superordinate.
/// One is a positive claim, one is an inability about the records, and one is
/// neither (the records no longer hold the registration), so any sentence covering
/// all three either says nothing or says something false of two of them.
///
/// IT CARRIED FIVE UNTIL 3.0.0, the two extra being the identity re-check's own
/// findings: a record existing under the code the FILE declares about itself, and a
/// file that yielded no code to ask about at all. Both went with that check. What
/// is worth keeping from them is the principle that survived: where a superordinate
/// genuinely holds it is taken, which is why a keyed read that answered outside its
/// documented set never got a line of its own but counted under
/// <see cref="HeldBackReason.RecordsUnreadable"/>, that being what the sentence
/// already says.
///
/// Each sentence is then held to the same test WITHIN its own cause, which is not
/// the same job and was the one left undone: a partition whose members are right
/// still needs every situation reaching a member to be true of that member's
/// sentence. Two reach the first, and only the present state of the records is
/// true of both (see <see cref="HeldBackReason.Reclaimed"/>).
/// </summary>
internal static class HeldBackReport
{
    /// <summary>
    /// One sentence per cause present in <paramref name="reasons"/>, in the order
    /// they are meant to be read, and empty when nothing was kept back.
    ///
    /// The order is strongest finding first and weakest last, so a block of
    /// several reads downwards from what was established to what could not be: a
    /// live claim that names the file, then a registration the records no longer
    /// hold, then records that could not be read. The positive finding leads
    /// because it is the only one that says anything about the file; the inability
    /// trails because it establishes nothing about it at all. It is fixed here
    /// rather than at each call site so both hosts read the same way round.
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
