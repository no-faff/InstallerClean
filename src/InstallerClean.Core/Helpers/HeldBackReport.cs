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
/// Two are positive claims of different strengths, one is an inability about the
/// records, one is an inability about the FILE, and one is neither (the records no
/// longer hold the registration), so any sentence covering them all either says
/// nothing or says something false of most of them.
///
/// WHERE A SUPERORDINATE DOES HOLD IT IS TAKEN, which is the difference between a
/// partition and a pile. The identity check's unaskable state gets no line of its
/// own: an account list that would not read, a property read answering outside its
/// documented set and a patch enumeration that did not reach a clean end are all
/// failures to read the Windows Installer records, which is what
/// <see cref="HeldBackReason.RecordsUnreadable"/> already says, so they count under
/// that sentence rather than beside it.
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
    /// several reads downwards from what was established to what could not be:
    /// a live claim that names the file, then a record under the name the file
    /// gives itself, then a registration the records no longer hold, then a file
    /// that could not be read, then records that could not be read. The two
    /// positive findings lead because they are the only ones that say anything
    /// about the file; the two inabilities trail because neither establishes
    /// anything about it at all. It is fixed here rather than at each call site so
    /// both hosts read the same way round.
    /// </summary>
    internal static IReadOnlyList<string> Lines(HeldBackReasons reasons)
    {
        if (reasons.Total == 0) return Array.Empty<string>();

        var lines = new List<string>(5);
        Add(lines, reasons.Reclaimed,
            Strings.Completion_ReverifySkipped, "Completion.ReverifySkipped");
        Add(lines, reasons.IdentityClaimed,
            Strings.Completion_ReverifyIdentityClaimed, "Completion.ReverifyIdentityClaimed");
        Add(lines, reasons.RecordsChanged,
            Strings.Completion_ReverifyRecordsChanged, "Completion.ReverifyRecordsChanged");
        Add(lines, reasons.IdentityUnreadable,
            Strings.Completion_ReverifyIdentityUnreadable, "Completion.ReverifyIdentityUnreadable");
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
