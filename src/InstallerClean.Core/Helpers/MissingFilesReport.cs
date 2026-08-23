using InstallerClean.Models;
using InstallerClean.Resources;

namespace InstallerClean.Helpers;

/// <summary>
/// Renders what Windows holds a record for and the folder does not have: how many
/// files, and which programs they belong to.
///
/// It lives in Core rather than in either host for the reason
/// <see cref="HeldBackReport"/> does: the two hosts must answer identically for
/// one machine state and they do not share the code that prints it, the window
/// composing a line for the main screen and the command line writing to stdout.
/// Rendering the parts here leaves each host only its own joining to do.
///
/// ONE POPULATION, ONE SENTENCE, AND NO CAUSE NAMED. A registration naming an
/// absent file is the same condition whether or not Windows has marked the patch
/// superseded, because Windows opens every patch registered to a product either
/// way and a missing one gives error 1635
/// (<see cref="RegisteredPackage.IsMissingFromDisk"/> carries the citations). The
/// data keeps the split, in <see cref="ScanResult.MissingUnaffectedCount"/> and
/// its sibling; the copy does not.
///
/// AND IT SAYS NOTHING ABOUT WHAT REMOVED THEM, which is not a nicety. Every tool
/// that has ever deleted from this folder leaves an identical record, this one
/// included up to v2.3.0, so the app cannot tell whose work it is looking at and
/// must not imply it can. It fires on machines this app has never run on.
/// </summary>
internal static class MissingFilesReport
{
    /// <summary>
    /// How many programs to name before the rest become a number. Three fits the
    /// main window's line at a readable length and is generous for a condition
    /// most machines never meet; the Details window lists every one of them, which
    /// is where somebody who needs the full set is sent.
    /// </summary>
    private const int MaxNamed = 3;

    /// <summary>One program with a missing file, and how many of its files are missing.</summary>
    /// <param name="ProductName">
    /// Empty where nothing named it. Not a fault: the registry fallback claims a
    /// path without a product name, so a registration only it reached has none to
    /// give, and a residue key whose product has gone is exactly the shape whose
    /// file tends to be absent. Those rows are counted and never named.
    /// </param>
    internal readonly record struct AffectedProduct(string ProductName, int FileCount);

    /// <summary>
    /// ONE EXPRESSION FOR THE BANNER'S POPULATION, NAMED SO THE SURFACES CANNOT DRIFT.
    /// A missing registration is affected unless the app has positively established that
    /// nothing could reach for the file: the state is superseded or obsoleted AND every
    /// product sharing the patch was shown to hold no patch that could be uninstalled.
    ///
    /// Neither half of that conjunction would do alone. The state alone calls a missing
    /// superseded file benign because Windows marked the patch replaced, which was
    /// measured false. The app's own removable verdict alone fires on every missing
    /// OBSOLETED registration, because such a patch is not removable for a policy reason
    /// rather than a dangerous one, and that is an alarm at past users about files this
    /// app itself removed.
    ///
    /// AND AN UNESTABLISHED VERDICT IS AFFECTED, deliberately, which is the opposite
    /// direction from the offer. Both refuse to claim what the app has not shown: there
    /// that the file is spare, here that its absence is harmless.
    ///
    /// AND A WITHHELD ROW IS AFFECTED WHATEVER ITS VERDICT SAYS, EXCEPT WHERE THE ONLY
    /// CAUSE WAS THE UNREAD PATCH FILE. That exception is the last conjunct, and it is a
    /// tautology rather than a concession. The pass that confirms a removable verdict
    /// reads the patch file to ask which products it declares, and withholds where it
    /// cannot. For a row that has REACHED THIS PREDICATE the file is GONE, so the read it
    /// failed is a read of the very file whose absence is the subject: there is nothing
    /// there to open, nobody can perform it on any machine ever, and it fails for a file
    /// this app removed exactly as it fails for one anything else removed. Read as a
    /// reason to warn, it had the app offer a file, remove it, and then report it as a
    /// thing a repair could fail on, which is the claim the offer's own condition exists
    /// to rule out.
    ///
    /// SO THE ROW KEEPS THE VERDICT IT WAS POSITIVELY GIVEN, and it keeps it on a run that
    /// came up short elsewhere as much as on one that did not. Until 3.0.0 the scan-wide
    /// withholding cleared this marker whenever the run lost a claim anywhere, which put
    /// exactly this row back under the banner. What that fired on was a machine-level
    /// count whose three terms are a failed read on a product the enumeration DID return,
    /// a product the registry saw and the enumeration did not, and a registry key Windows
    /// would not answer about. None of them is "a holder of this patch went unseen", so
    /// none of them bears on this file. The residual it was reaching for is real and is
    /// answered where answering still changes an outcome: on such a run the app removes no
    /// superseded patch at all.
    ///
    /// AND THE STANDARD IS THE OFFER'S, WHICH IS WHY THAT IS NOT A WEAKENING. The evidence
    /// that the absence is harmless is evidence of the same kind the app acts on when it
    /// offers a file for permanent removal, and offering is by far the more consequential
    /// of the two. A rule strict enough to distrust that evidence here would have had to
    /// refuse the offer first. It is not the app remembering that it removed the file:
    /// nothing here has any memory, and the verdict is re-established from the machine's
    /// own records on the run that goes quiet. Nor can this line cover an offer either
    /// way: it speaks only about a file that has already gone.
    ///
    /// THE ROUTE THAT STILL DEFEATS THE CARVE-OUT, AND IT IS THE ONE THAT SHOULD. Where
    /// the machine-wide patch enumeration did not answer, the confirmation pass downgrades
    /// every removable path with no marker set, so such a row arrives here withheld and
    /// unmarked and is reported. That is the run on which the app really did fail to
    /// establish something about THIS patch, rather than the run on which something else
    /// about the machine failed. A row downgraded because its patch set could not be
    /// established is reported too, by the verdict clause above rather than by this one.
    ///
    /// THE FLAG NAMES A CAUSE AND THIS LINE DECIDES WHAT THE CAUSE MEANT, because it
    /// carries two meanings and only one is a tautology. A file that is THERE and will not
    /// give up an identity is a real inability; such a row is not missing, so it never
    /// reaches this expression at all. See
    /// <see cref="RegisteredPackage.WithheldOnUnreadableFile"/>.
    /// </summary>
    internal static bool Affected(RegisteredPackage row) =>
        row.IsMissingFromDisk
        && !(row.IsSupersededOrObsoleted
             && row.ProductPatchSetVerdict == ProductPatchSet.AllNonRemovable
             && (!row.RemovableWithheld || row.WithheldOnUnreadableFile));

    /// <summary>
    /// The programs behind a scan's missing registrations, most-affected first, then
    /// alphabetical so two runs of one machine agree. Rows carrying no product name are
    /// folded into a single unnamed entry at the end, because several of them are not
    /// several programs: the registry fallback names none of its rows, so counting them as
    /// one program each would invent a headcount.
    ///
    /// IT NAMES THE SAME POPULATION THE BANNER COUNTS, WHICH IS NARROWER THAN "EVERY
    /// MISSING REGISTRATION" FROM 3.0.0. The banner fires where something could still reach
    /// for a file that is gone, so a registration whose absence the app has positively
    /// established to be harmless is not one of the programs it should name: listing it
    /// would put a program in front of somebody as affected when the same scan had just
    /// decided it is not. The two filters are the same expression and must stay that way;
    /// see <see cref="Affected"/> for why the conjunction and not either half.
    /// </summary>
    /// <remarks>
    /// THIS DOCUMENTATION SAT ON <see cref="Affected"/> FROM 2026-08-17 TO 2026-08-23 and
    /// described this method from there. The commit that introduced the predicate put its
    /// summary directly beneath this one, so one member carried two summary blocks and this
    /// one carried none, and the orphaned text told its reader to "see Affected" from inside
    /// what had become Affected's own documentation. Nothing about either method changed
    /// when it was moved back.
    /// </remarks>
    internal static IReadOnlyList<AffectedProduct> Products(IEnumerable<RegisteredPackage> registered)
    {
        var named = new List<AffectedProduct>();
        var unnamed = 0;

        foreach (var group in registered
            .Where(Affected)
            .GroupBy(p => p.ProductName ?? string.Empty, StringComparer.OrdinalIgnoreCase))
        {
            if (string.IsNullOrWhiteSpace(group.Key)) unnamed += group.Count();
            else named.Add(new AffectedProduct(group.First().ProductName, group.Count()));
        }

        named.Sort((a, b) => b.FileCount != a.FileCount
            ? b.FileCount.CompareTo(a.FileCount)
            : string.Compare(a.ProductName, b.ProductName, StringComparison.CurrentCultureIgnoreCase));

        if (unnamed > 0) named.Add(new AffectedProduct(string.Empty, unnamed));
        return named;
    }

    /// <summary>
    /// The programs as one comma-joined phrase for a surface with a single line to
    /// spend, naming at most <see cref="MaxNamed"/> and closing on how many were
    /// not named. Empty for an empty list, which is a caller's cue to say nothing
    /// at all rather than to print an empty clause.
    ///
    /// Commas and no conjunction, which is this app's existing habit for a list of
    /// counts (Summary.OrphanedWindow) and is what keeps the phrase translatable:
    /// where "and" goes in a list of three is a per-language question, and the
    /// separator is a resx value so a language that does not join with a comma can
    /// say so.
    /// </summary>
    /// <remarks>
    /// The two tails are separate and may not be merged into one "and N more".
    /// A program past the cap is one this app CAN name and did not have room for;
    /// a row with no product name is one nothing named at all. Folding them
    /// together would put both under whichever sentence was chosen, and one of the
    /// two would then be false of half the members.
    /// </remarks>
    internal static string Inline(IReadOnlyList<AffectedProduct> products)
    {
        if (products.Count == 0) return string.Empty;

        var parts = new List<string>(MaxNamed + 2);
        var otherPrograms = 0;
        var unnamedFiles = 0;

        foreach (var product in products)
        {
            // An unnamed group can never take one of the named slots, however many
            // files it carries: there is no name to print.
            if (product.ProductName.Length == 0) unnamedFiles += product.FileCount;
            else if (parts.Count < MaxNamed) parts.Add(product.ProductName);
            else otherPrograms++;
        }

        if (otherPrograms > 0)
            parts.Add(string.Format(
                DisplayHelpers.Pluralise(otherPrograms,
                    Strings.Summary_MissingFromDisk_OtherPrograms_Singular,
                    Strings.Summary_MissingFromDisk_OtherPrograms_Plural,
                    "Summary.MissingFromDisk.OtherPrograms"),
                otherPrograms));

        if (unnamedFiles > 0)
            parts.Add(string.Format(
                DisplayHelpers.Pluralise(unnamedFiles,
                    Strings.Summary_MissingFromDisk_Unnamed_Singular,
                    Strings.Summary_MissingFromDisk_Unnamed_Plural,
                    "Summary.MissingFromDisk.Unnamed"),
                unnamedFiles));

        return string.Join(Strings.Display_ListSeparator, parts);
    }
}
