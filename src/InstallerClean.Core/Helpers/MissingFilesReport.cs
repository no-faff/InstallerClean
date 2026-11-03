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
/// ONE POPULATION, ONE SENTENCE, AND NO CAUSE NAMED. The patch STATE settles nothing
/// on its own: Windows opens every patch registered to a product whether or not it
/// has been superseded, so a superseded registration naming an absent file is a
/// record Windows will act on and cannot satisfy exactly as an applied one is
/// (<see cref="RegisteredPackage.IsMissingFromDisk"/> carries the citations and
/// why no single consequence may be named). The data keeps the split, in
/// <see cref="ScanResult.MissingUnaffectedCount"/> and its sibling; the copy does
/// not.
///
/// WHAT THE PREDICATE BELOW GRADES IS ONE CASE, NARROWLY, and it is the only thing
/// here that does: the state together with further facts the scan has to establish
/// positively. A summary sits above the predicate and is read first, so it
/// states the part that holds either way and leaves the population to the expression
/// that decides it.
///
/// AND IT SAYS NOTHING ABOUT WHAT REMOVED THEM. Every tool that deletes from this
/// folder leaves an identical record, this one included where it removes a superseded
/// patch whose registration Windows keeps, so the app cannot tell whose work it is
/// looking at and must not imply it can. It fires on machines this app has never run
/// on.
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
    /// Neither half of that conjunction is enough alone. The state is not: Windows opens
    /// every patch registered to a product whether or not it has been superseded, so a
    /// superseded file's absence is harmless only where nothing sharing the patch could
    /// roll back onto it. Nor is the app's own removable verdict: an obsoleted patch is
    /// not removable for a policy reason rather than a dangerous one, so that verdict
    /// says nothing about whether its absence matters.
    ///
    /// AND AN UNESTABLISHED VERDICT IS AFFECTED, deliberately, which is the opposite
    /// direction from the offer. Both refuse to claim what the app has not shown: there
    /// that the file is spare, here that its absence is harmless.
    ///
    /// AND A WITHHELD ROW IS AFFECTED WHATEVER ITS VERDICT SAYS, EXCEPT WHERE THE ONLY
    /// CAUSE WAS THE UNREAD PATCH FILE. That exception is the last conjunct. The pass that
    /// confirms a removable verdict reads the patch file to ask which products it
    /// declares, and withholds where it cannot. For a row this exception decides, the
    /// first conjunct has already found the file GONE, so the read it failed is a read of
    /// the very file whose absence is the subject: there is nothing there to open, and
    /// the read fails for a file this app removed exactly as it fails for one anything
    /// else removed. Such a row is judged on the verdict it was positively given.
    ///
    /// AND THE PASS RECORDS THAT CAUSE ONLY AFTER ASKING. It puts the patch, under every
    /// code naming the file, to every installation it can name without the file, and
    /// records the unread file as the cause only where every one of them answered and none
    /// holds the patch. An installation that holds it makes the row a claim, carrying its
    /// state where that state is the stronger reading and the installation's own claim
    /// never reached the merge, and one that did not answer withholds the row without the
    /// cause.
    ///
    /// SO THE ROW KEEPS THAT VERDICT, on a run that came up short elsewhere as much as on
    /// one that did not. The scan-wide withholding takes the removable verdict off every
    /// row still carrying one and leaves this marker alone. It fires on two machine-level
    /// conditions. The terms of the first are a product the enumeration DID return whose
    /// lost records the registry does not hold, a product it returned whose installations
    /// the keyed ask did not settle and whose registry entry names a cached file the
    /// enumeration never claimed that is on the disk, a product the registry names that
    /// the scan could not settle, and a cached patch file no product the scan asks is
    /// recorded as holding; none of them is "a holder of this patch went unseen", so none
    /// of them bears on this file. The second, a recorded path the scan could not settle,
    /// can be exactly that holder, and the holder's registration then
    /// reaches this predicate through the row it lands on: where this file has gone, a
    /// registration that means it but is kept in the unsettled spelling names the same
    /// absent file, or names nothing, so that row reads missing too and is judged here on
    /// its own terms. An applied row whose file is missing is always reported. Such a run
    /// removes no superseded patch at all.
    ///
    /// THE EVIDENCE IS OF THE KIND THE OFFER ACTS ON. The verdict that keeps this row out
    /// of the count is the per-product verdict a superseded patch has to carry to be
    /// offered for permanent removal. It is re-established from the machine's own records
    /// on every run: nothing here remembers what the app removed. And this predicate
    /// speaks only about a file that has already gone, so it bears on no offer.
    ///
    /// A ROW WITHHELD FOR ANY OTHER CAUSE IS REPORTED. Where the machine-wide patch
    /// enumeration did not answer, the confirmation pass downgrades every removable path
    /// before its patch file is read, with no unread-file marker set, so such a row arrives
    /// here withheld without that marker and is reported. That condition bears on this patch
    /// itself: a holder of it may have gone unseen. A row downgraded because its patch set could not be established is
    /// reported too, by the verdict clause above rather than by this one.
    ///
    /// THE FLAG NAMES A CAUSE AND THIS LINE DECIDES WHAT THE CAUSE MEANT, because it
    /// carries two meanings and only one is a tautology. A file that is THERE and will not
    /// give up an identity is a real inability; such a row is not missing, so the first
    /// conjunct leaves it out. See
    /// <see cref="RegisteredPackage.WithheldOnUnreadableFile"/>.
    ///
    /// AND THE STATE HAS TO BE THE WHOLE STORY OF WHAT HOLDS THE FILE. A row can read
    /// superseded while a product's own package record names the file, or while a claim
    /// whose State would not read names it, or while an installation asked about the patch
    /// did not answer; none of those brought a state to put on the row. Such a row carries
    /// <see cref="RegisteredPackage.OtherHoldNotRuledOut"/> and is reported. An installation
    /// that holds the patch applied carries that state onto the row, which then leaves the
    /// first conjunct of the exemption.
    /// </summary>
    internal static bool Affected(RegisteredPackage row) =>
        row.IsMissingFromDisk && !AbsenceShownHarmless(row);

    /// <summary>
    /// The exemption <see cref="Affected"/> grants a missing row, as a fact about the
    /// records alone: the row is a superseded or obsoleted patch, nothing else is known to
    /// hold its file, it was withheld for nothing but the unread patch file, and every
    /// product sharing the patch was shown to hold no patch that could be uninstalled.
    ///
    /// NAMED SO THE ENUMERATION ASKS THE SAME QUESTION. The pass that asks installations
    /// about a patch no other pass asks about picks its rows with this and
    /// <see cref="AbsenceHarmlessIfJudgedClean"/>, and a second copy of the conjunction there
    /// would drift from this one.
    /// </summary>
    internal static bool AbsenceShownHarmless(RegisteredPackage row) =>
        AbsenceHarmlessIfJudgedClean(row)
        && row.ProductPatchSetVerdict == ProductPatchSet.AllNonRemovable;

    /// <summary>
    /// <see cref="AbsenceShownHarmless"/> without its verdict, for the pass that decides which
    /// products a row's verdict is taken across and so runs before there is one.
    /// </summary>
    internal static bool AbsenceHarmlessIfJudgedClean(RegisteredPackage row) =>
        row.IsSupersededOrObsoleted
        && !row.OtherHoldNotRuledOut
        && (!row.RemovableWithheld || row.WithheldOnUnreadableFile);

    /// <summary>
    /// The programs behind a scan's missing registrations, most-affected first, then
    /// alphabetical so two runs of one machine agree. Rows carrying no product name are
    /// folded into a single unnamed entry at the end, because several of them are not
    /// several programs: the registry fallback names none of its rows, so counting them as
    /// one program each would invent a headcount.
    ///
    /// IT NAMES THE SAME POPULATION THE BANNER COUNTS, WHICH IS NARROWER THAN EVERY
    /// MISSING REGISTRATION. The banner fires where something could still reach
    /// for a file that is gone, so a registration whose absence the app has positively
    /// established to be harmless is not one of the programs it should name: listing it
    /// would put a program in front of somebody as affected when the same scan had just
    /// decided it is not. The two filters are the same expression and must stay that way;
    /// see <see cref="Affected"/> for why the conjunction and not either half.
    /// </summary>
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
                DisplayHelpers.FormatCount(otherPrograms)));

        if (unnamedFiles > 0)
            parts.Add(string.Format(
                DisplayHelpers.Pluralise(unnamedFiles,
                    Strings.Summary_MissingFromDisk_Unnamed_Singular,
                    Strings.Summary_MissingFromDisk_Unnamed_Plural,
                    "Summary.MissingFromDisk.Unnamed"),
                DisplayHelpers.FormatCount(unnamedFiles)));

        return string.Join(Strings.Display_ListSeparator, parts);
    }
}
