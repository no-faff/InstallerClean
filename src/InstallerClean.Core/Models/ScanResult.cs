namespace InstallerClean.Models;

/// <summary>
/// The output of a single <c>FileSystemScanService</c> run. The whole UI
/// state derives from this record: the orphan list, the registered list,
/// the size totals on the main screen, and the discrepancy banner are
/// all functions of these fields.
/// </summary>
/// <param name="RemovableFiles">
/// The files this scan is offering to move or delete. Two populations, and the
/// paragraph below says which and on what each was judged.
///
/// TWO PATHWAYS REACH IT AND THEY ASK DIFFERENT QUESTIONS. A file no registration
/// names arrives from the folder walk, having been judged on paths. A superseded
/// patch arrives from the registered set, having been judged on products: Windows
/// reports it superseded, it declares itself non-removable, and every product it is
/// registered under was established to hold no patch that could be uninstalled and
/// roll back onto its file. Obsoleted patches reach it by neither pathway and are
/// counted instead.
///
/// The state alone is never permission. Windows opens every patch registered to a
/// product whether or not it has been superseded (see
/// <see cref="RegisteredPackage.IsMissingFromDisk"/> for the citation), so a
/// superseded label is a fact about the record and the per-product condition is what
/// makes the file safe to offer.
/// </param>
/// <param name="RegisteredPackages">
/// <c>LocalPackage</c> paths this scan is keeping, which is every path any
/// registration names. Drives the registered list and the totals on the main
/// screen.
///
/// IT IS NOT A LIST OF FILES SHOWN TO BE NEEDED, and no surface may describe it
/// as one. Two populations are in here and only the first carries a claim: a
/// path a live registration names, superseded and obsoleted patches included
/// (Windows holds those too, and their state is not a statement about the file);
/// and a patch whose State or Uninstallable read failed
/// (<c>VerdictUnreadable</c>), about which nothing was established at all. Both
/// are kept, which is the safe direction and is not in question. What they do
/// not share is a sentence, which is why they are counted apart in
/// <see cref="RegisteredClaimedCount"/> and
/// <see cref="RegisteredUnjudgedCount"/>. <see cref="RegisteredWithheldCount"/>
/// is the third and counts what the per-product condition held back.
/// </param>
/// <param name="RegisteredTotalBytes">
/// Sum of <see cref="RegisteredPackage.FileSizeBytes"/> across
/// <see cref="RegisteredPackages"/> where the file actually exists on
/// disk. Excludes <see cref="MissingFromDiskCount"/> entries so the
/// total never includes non-existent files.
/// </param>
/// <param name="MissingAffectedCount">
/// Registrations whose <c>LocalPackage</c> file is not on disk and whose absence
/// this scan could NOT establish to be harmless. It is the population the
/// missing-files line speaks for, and it is <c>MissingFilesReport.Affected</c> that
/// decides it.
///
/// THE AXIS IS A CONJUNCTION AND IT IS NOT THE PATCH STATE. A row is left out of
/// this count only where Windows reports the patch superseded or obsoleted, AND the
/// per-product condition positively established that nothing on any product sharing
/// it could be uninstalled and roll back onto its file, AND this scan did not
/// withhold that row's verdict. Everything else is in here: a product's own cached
/// package, an applied patch, a patch whose state no read established, a path only
/// the registry fallback named, every superseded row whose product condition could
/// not be settled, and every row a scan that lost a claim held back.
///
/// THE THIRD CONJUNCT ARRIVED LAST AND IT CLOSES A SILENCE. A run that lost a claim
/// anywhere withholds the whole removable class, and it does that to rows the
/// per-product pass had already judged clean, so such a row carries the withheld flag
/// and an AllNonRemovable verdict at once. Read on the verdict alone it left this
/// count, and the notice, and the program's name, all of which simply did not appear.
/// A scan that has just declined to rely on a verdict may not then rely on it to stay
/// quiet.
///
/// IT WAS CALLED <c>MissingNotSupersededCount</c> AND THAT NAME DESCRIBED AN AXIS
/// THE CODE STOPPED USING. The split moved to the conjunction in 3.0.0 and the two
/// names stayed behind, so both said the state decided it when the state is half of
/// what decides it. A name is a specification to the next reader, and these two were
/// specifying the rule the release had just replaced.
///
/// IT STATES NO CAUSE AND NOTHING BUILT ON IT MAY EITHER. It used to say a
/// non-zero value means another tool removed files Windows still references. That
/// is one cause named for a set that can have several, and after 3.0.0 this
/// application is itself a candidate cause on any machine that ran v1.0.0 to
/// v2.3.0: those versions offered superseded patches, and deleting one leaves
/// exactly this record. What the number says is that Windows holds records naming
/// files that are not there.
/// </param>
/// <param name="MissingUnaffectedCount">
/// The other half: registrations whose file is not on disk and whose absence this
/// scan POSITIVELY established to be harmless. Windows reports the patch superseded
/// or obsoleted, every product sharing it was established to hold no patch that
/// could be uninstalled and roll back onto its file, and the scan did not go on to
/// withhold that verdict for a claim it lost elsewhere.
///
/// BOTH HALVES OF THAT CONJUNCTION ARE LOAD-BEARING AND THE STATE ALONE IS NOT
/// ENOUGH. Splitting on the state was tried and the claim it makes was measured
/// false: with the superseded files gone, uninstalling the superseding patch
/// discarded both patches and went to the unpatched base, with Windows demonstrably
/// looking for the absent files. So a superseded row whose product condition could
/// not be settled is NOT in here; it is in the affected half, and the
/// missing-files line speaks for it.
///
/// COUNTED APART FROM ITS SIBLING AND NOT SPOKEN. The split is data, kept so a
/// report can still show the shape of a machine, and no surface states it. It earns
/// no sentence of its own: what separates the two is what this scan could
/// establish, which is a fact about the scan rather than about the file.
/// <see cref="RegisteredPackage.IsMissingFromDisk"/> carries the citations and is
/// the property both hosts read.
/// </param>
/// <param name="UnaccountedProductCount">
/// Installed products this scan did not account for, carried through from
/// <see cref="InstallerQueryResult.UnaccountedProductCount"/>, whose remarks are
/// the ones to read before quoting this: it is not confined to records that
/// failed to read, and it is an estimate rather than a headcount.
///
/// IT BEARS ON BOTH HALVES OF THE SCAN AND THE SECOND IS THE ONE USUALLY FORGOTTEN.
/// A non-zero value withholds every superseded-patch verdict, so the offer is shorter
/// than the machine would normally give, which is the meaning it has always had. It
/// also bears on the missing-files report: a product whose records did not fully read
/// is a product whose registrations this scan may not have seen, so the count of
/// records naming files that are not there can be short. "No missing files" and "no
/// missing files that could be seen" are different claims and only the second is
/// earned on such a run.
/// </param>
/// <param name="WithheldCount">
/// What withholding the removable class cost a run: superseded packages whose file
/// was on disk and which the scan would have offered, had it been able to say that
/// no installed product still needed them.
///
/// A REAL FIGURE AGAIN, HAVING BEEN A LITERAL ZERO WHILE NOTHING WAS OFFERED. It
/// counts what the withholding cost this run: rows Windows reports superseded whose
/// file is on disk and which declared themselves non-removable, held back because a
/// read established nothing. Obsoleted rows are NOT in it; they are not withheld,
/// they are simply not offered, and they have their own count. This paragraph and
/// the line above it said "superseded or obsoleted" while this one said the
/// opposite, and the predicate settles it: nothing reaches the flag without having
/// carried IsRemovable, and IsRemovablePatch requires state 2. THAT IS A
/// USER-FACING CLAIM NOW RATHER THAN AN INTERNAL ONE: both hosts name the class in
/// as many words (<c>Summary.SupersededHeldBack</c>, <c>Cli.SupersededHeldBack</c>),
/// so this count and that noun have to agree.
///
/// AND A PRODUCT THAT COULD ROLL BACK ONTO THE FILE IS NOT IN IT EITHER, WHICH THIS
/// NOTE USED TO LIST AS A CONTRIBUTOR. That condition is
/// <see cref="ProductPatchSet.RemovablePatchPresent"/>, and the downgrade it reaches
/// passes withheld FALSE, because the scan positively established a live claim
/// rather than failing to establish anything. Worse() lets it beat Unestablished
/// where a row meets both, so the mixed case is excluded with it. What is left is
/// exactly one thing, in Downgrade's own words: a read that established nothing.
///
/// THE ON-DISK QUALIFIER IS THE WHOLE DIFFERENCE FROM
/// <see cref="RegisteredWithheldCount"/> AND IT IS LOAD-BEARING. A row whose file
/// has already gone cost this run nothing: an absent file could never have been
/// offered, the branch that offers a superseded row being gated on its existence.
/// The two counts were one variable until 3.0.0, so this figure carried those rows
/// and overstated what the withholding cost, on the one channel that answers what
/// the app is doing on machines nobody here can see. A cost figure that overstates
/// invites relaxing the condition it is measuring. The direction of the correction
/// is downward and only on a machine that has already lost part of its cache; on an
/// intact one the two counts are equal.
/// </param>
/// <param name="Census">
/// What the enumeration behind this scan measured about itself and about the
/// machine, carried straight through from
/// <see cref="InstallerQueryResult.Census"/>. Instrumentation for the opt-in
/// report; nothing in the app reads it to decide anything.
/// </param>
/// <param name="ShortNameCreation">
/// The machine's 8dot3 short-name creation policy, one of
/// <see cref="ShortNameCreationLabels"/>. Sampled once per scan and used for
/// nothing but the opt-in report, so it is a plain label with no default assumed:
/// an unconfigured machine reads as <see cref="ShortNameCreationLabels.Unset"/>
/// rather than as whichever setting a document guesses is usual.
/// </param>
/// <param name="RegisteredClaimedCount">
/// Kept files a live registration positively claims: a product's own cached
/// package, an applied patch, a path the registry fallback named, or a patch
/// Windows reports superseded or obsoleted. The one population in
/// <see cref="RegisteredPackages"/> that a sentence about being needed is true
/// of.
///
/// SUPERSEDED AND OBSOLETED PATCHES ARE INSIDE IT AND THAT IS THE CORRECTION
/// RATHER THAN A LOOSENING. Microsoft's own word for both states is "applied",
/// and Windows opens the cached file of every patch registered to a product
/// whether or not it has been superseded, so such a row is a live claim and
/// counting it as one is the true reading.
/// <see cref="RegisteredSupersededCount"/> is a sub-count of this, not a fourth
/// population.
/// </param>
/// <param name="RegisteredClaimedBytes">
/// The same population's bytes, files on disk only, on the same rule as
/// <see cref="RegisteredTotalBytes"/>. It exists because a count and a size shown
/// together are read as one statement: a size taken across both populations
/// beside a count of one of them would attribute the other's space to files this
/// scan says are needed.
/// </param>
/// <param name="RegisteredWithheldCount">
/// Kept ROWS the records called superseded or obsoleted and a scan would not act
/// on, because it could not establish that no installed product still needed
/// them: every row carrying <c>RemovableWithheld</c>, whether or not its file is
/// still on the disk.
///
/// NEARLY THE SAME POPULATION AS <see cref="WithheldCount"/> AND DELIBERATELY NOT
/// THE SAME COUNT. This one is a member of a three-way partition of the kept list,
/// so it counts what the registered-files window lists, and that window lists a row
/// whose file has gone like any other; leaving such a row out would leave a hole in
/// the partition. <see cref="WithheldCount"/> answers what the withholding COST,
/// and a row whose file is absent cost nothing. The two agree on any machine whose
/// cache is intact and differ by exactly the withheld rows whose files something
/// else has already removed.
///
/// Counted off the kept list rather than tallied through the loop that built it, so
/// the number shown and the rows shown cannot come apart. It reads zero on a machine
/// with no superseded patch and on a machine whose every superseded patch passed the
/// condition, and those are different findings that this count cannot separate; the
/// scan-time registration counts are what separate them.
/// </param>
/// <param name="RegisteredUnjudgedCount">
/// Kept files whose patch state no read established, one per path
/// (<c>VerdictUnreadable</c>). The question was put and the records did not
/// answer.
///
/// THE THREE PARTITION <see cref="RegisteredPackages"/> EXACTLY and a test holds
/// them to it. The two flags cannot both be set on one row, so no file is counted
/// twice and none falls between.
/// </param>
/// <param name="RegisteredSupersededCount">
/// Kept files whose registration is a patch Windows reports superseded (2) or
/// obsoleted (4), and whose file is on disk. The population this scan is KEEPING,
/// which is not the same as the population that exists: every superseded row that
/// passed the per-product condition has left this list for the offer, so what is
/// counted here is the withheld superseded rows plus every obsoleted row, the latter
/// never being offered at all.
///
/// A SUB-COUNT AND NOT A PARTITION MEMBER. Nearly all of these rows are inside
/// <see cref="RegisteredClaimedCount"/>; one shape falls under
/// <see cref="RegisteredUnjudgedCount"/> instead, a patch whose State read gave 2
/// or 4 and whose Uninstallable read then failed, so the two must never be added.
/// Ones whose file has already gone are in one of the two missing counts,
/// decided by <c>MissingFilesReport.Affected</c> rather than by the state.
/// </param>
/// <param name="RegisteredSupersededBytes">
/// The same population's bytes. It is the figure nobody had: the field data
/// records superseded patches by count only, so how much space they occupy on a
/// real machine has only ever been estimated. Files on disk only, on the same
/// rule as <see cref="RegisteredTotalBytes"/>.
/// </param>
/// <param name="SupersededRegistrationCount">
/// Every registration Windows reports superseded (2), counted at scan time off the
/// MACHINE and never off the offer, whatever its removability and whether or not
/// anything was offered.
///
/// THE DISTINCTION FROM THE OFFER-DERIVED FIGURE IS THE WHOLE POINT OF IT. A count
/// taken from the offer can only ever see the registrations that passed the
/// removability condition, so it cannot answer whether a machine HAS any. The two
/// differing is itself the finding, being the size of the class the condition
/// excludes, which nobody has measured.
/// </param>
/// <param name="WithheldFiles">
/// Every candidate this scan declined to offer, in walk order. TWO CAUSES REACH IT
/// AND THEY ARE NOT ONE THING, so they are listed rather than covered by a sentence
/// that would be false of one of them:
///
/// The scan could not establish which cached files belong to which programs, which
/// withholds the whole walk-derived set at once (<see cref="WalkOfferWithheldWholesale"/>,
/// and see it for the three findings that reach it); or the candidate is an installation
/// package whose own declared product Windows still holds a record of, or whose
/// declaration this scan could not settle, which withholds that one file. A run can
/// hold files put here by either, and a reader of this list may assume neither.
///
/// NO SURFACE STATES A CAUSE OVER IT AND NONE MAY START. The main window counts these
/// into its left-alone line. The Details window lists them among the registrations, in
/// one list, with nothing marking a row out: no heading, no column, no indicator and no
/// lookup of a program that might have used the file. Both are true of every row
/// whichever cause put it there, and there is no per-file record of which did.
///
/// THE HEADING THAT USED TO CARRY THIS RULE IS GONE AND THE RULE IS NOT. Until 3.0.0 the
/// Details window held a second group under "InstallerClean couldn't be sure about
/// these", chosen because it was true of every row whatever had put it there. The two
/// groups are now one list and that heading has gone with them, which weakens nothing
/// here: a surface that says nothing about these rows cannot state a cause over them.
///
/// WHY THE FIRST CAUSE TAKES THE WHOLE WALK-DERIVED SET AND NOT ONE FILE, and the
/// answer is the same shape for all three findings behind it: the app knows a needed
/// file may be sitting in the candidate list and cannot say WHICH. For an unspellable
/// claim, the claim is kept in the raw spelling Windows gave, so it matches nothing the
/// walk produces, and the identity match cannot help either, because a value the path
/// API refuses is a value CreateFile refuses too and there is nothing to open and
/// compare. For a second copy of one program, the cached package registered to it
/// declares the base product code, so the per-file screen can be told there is no such
/// record while the second copy's own registration still needs the file, and nothing in
/// the scan can work out which cached file belongs to that copy. Every unclaimed file is
/// therefore one that could have been meant, and the app cannot say of any of them that
/// nothing needs it.
///
/// THE SUPERSEDED HALF OF THE OFFER IS NOT IN HERE, under either cause. Those rows
/// are judged on products, through registry keys read by product code and patch code,
/// and nothing on that path reads a cached-package path at all; measured with a
/// planted unspellable value beside an ordinary-value control, the sibling patch's
/// offer was identical. They cannot be reached by the second cause either, on the
/// structure rather than on a measurement: that screen runs over the walk's unclaimed
/// candidates, a superseded row reaches the offer from the registered set without
/// ever having been one, and the screen refuses a patch in any case. What remains
/// unobserved rather than ruled out is an unspellable registration naming the very
/// same file an offered superseded row names, which would be a second claim on that
/// path that the merge cannot see.
///
/// IT EXISTS SO THE TWO SUMMARY LINES ACCOUNT FOR EVERY FILE IN THE FOLDER. A
/// withheld file would otherwise appear in neither: not offered, and not a registered
/// row either, because no registration names it. The two lines could then add up to
/// less than the folder holds, with the difference in no line at all and no way for
/// anyone to notice.
///
/// The list rather than a count and a total, so the number shown and the rows shown
/// cannot come apart: both are read off this. Null means a scan that never reached
/// the decision, which is not the same as a scan that kept nothing back, and both
/// read as an empty list to a caller that does not care.
/// </param>
/// <param name="ObsoletedRegistrationCount">
/// The same for state 4. For this class it is the ONLY figure that can ever be
/// non-zero, obsoleted patches not being offered at all, so it is the only way the
/// question of whether anybody has any is ever answered.
///
/// WHY THAT QUESTION IS OPEN AT ALL. Across every report this project has received,
/// obsoleted patches have never been seen on any machine, so offering them would
/// reclaim nothing; and nobody has ever manufactured one to test with. Counting them
/// answers the question that was going to be answered by offering them, and puts
/// nothing on anyone's list.
/// </param>
/// <param name="WalkOfferWithheldWholesale">
/// True where this scan emptied its walk-derived offer in one go, rather than judging
/// each candidate and keeping it, AND that emptying actually took a file off a list it
/// would otherwise have made.
///
/// THE SECOND HALF OF THAT IS NEW AND IS THE WHOLE OF WHAT THIS FLAG IS FOR. It used to
/// report only which branch the scan took, so it was true on a machine whose walk found
/// no unclaimed candidates and therefore held nothing back, and the window had to check
/// <see cref="WithheldFiles"/> itself before showing the screen that reads this. That
/// gate was a host counting a list two different decisions contribute to: the moment
/// either one's membership changes, the gate changes meaning and nothing fails. The
/// question is now answered where the withholding happens, which is the only place that
/// knows what that withholding took.
///
/// IT IS NOT "THE OFFER IS EMPTY" AND THE TWO MUST NOT BE CONFLATED, which is the
/// whole reason this exists rather than the hosts asking
/// <see cref="RemovableFiles"/> whether it is empty. An empty offer has two quite
/// different meanings: the folder holds nothing this scan can offer, and the scan
/// could not establish enough to offer anything. The first is a clean machine and the
/// second is a machine full of files nobody has vouched for, and a screen saying
/// "nothing to clean up in your Installer folder" is true of the first and false of
/// the second.
///
/// IT CAN BE TRUE WHILE THE OFFER IS NOT EMPTY. The rule covers the walk-derived half
/// only; a superseded registration that survived every withholding is offered beside
/// it, so a host reading this must still ask what the offer holds.
///
/// IT CANNOT BE TRUE WITH <see cref="WithheldFiles"/> EMPTY, which is a change and not
/// a coincidence: see above. A machine that took the branch and had nothing to withhold
/// reads false here, and the all-clear is right for it, nothing in its folder having
/// gone unclaimed. The reverse does not hold, and no host may assume it: the other
/// decision puts files in that list on runs where this is false.
///
/// NO CAUSE TRAVELS WITH IT AND NONE MAY BE ADDED. Several conditions can empty an
/// offer wholesale and they are different facts about a machine, so a bool is the
/// whole of what may be carried: any sentence naming one cause would be false on the
/// others. The census is where the causes are counted apart.
///
/// THE LINE ABOVE USED TO OPEN "A RULE ABOUT THE MACHINE'S RECORDS", WHICH WAS A
/// CAUSE, and 3.0.0 added a condition it is false of. It has been taken out rather
/// than joined by a second: two named causes is the same fault with more words, and
/// a host reading this for something to say would find one true of half the set. The
/// only thing true of every member is that something this scan asked about did not
/// answer.
/// </param>
/// <param name="RegistrationIdentityReads">
/// What the file-identity reader answered when the scan asked which file each
/// registration's recorded path names. See
/// <see cref="FileIdentityReadTally"/>; <see cref="FileIdentityReadTally.RefusedTotal"/>
/// above zero is one of the conditions behind
/// <see cref="WalkOfferWithheldWholesale"/>.
/// </param>
/// <param name="CandidateIdentityReads">
/// The same for the other side of that comparison, one read per candidate the
/// registration side left anything to compare against.
///
/// ITS REFUSALS DO NOT EMPTY THE OFFER AND THE ASYMMETRY IS THE POINT. A
/// registration nobody could identify might name any candidate in the list, so none
/// of them can be offered. A candidate nobody could identify is one file: every
/// other candidate was compared against the registrations by a read that answered,
/// so that one is kept back and the rest stand.
/// </param>
public record ScanResult(
    IReadOnlyList<OrphanedFile> RemovableFiles,
    IReadOnlyList<RegisteredPackage> RegisteredPackages,
    long RegisteredTotalBytes,
    int MissingAffectedCount = 0,
    int MissingUnaffectedCount = 0,
    int UnaccountedProductCount = 0,
    int WithheldCount = 0,
    EnumerationCensus Census = default,
    string ShortNameCreation = ShortNameCreationLabels.Unreadable,
    int RegisteredClaimedCount = 0,
    long RegisteredClaimedBytes = 0,
    int RegisteredWithheldCount = 0,
    int RegisteredUnjudgedCount = 0,
    int RegisteredSupersededCount = 0,
    long RegisteredSupersededBytes = 0,
    int SupersededRegistrationCount = 0,
    int ObsoletedRegistrationCount = 0,
    IReadOnlyList<OrphanedFile>? WithheldFiles = null,
    bool WalkOfferWithheldWholesale = false,
    FileIdentityReadTally RegistrationIdentityReads = default,
    FileIdentityReadTally CandidateIdentityReads = default)
{
    /// <summary>
    /// Every registration naming a file that is not on disk; the sum of the two
    /// sub-counts and the figure both hosts speak, the split beneath it being
    /// data rather than copy.
    /// </summary>
    public int MissingFromDiskCount => MissingAffectedCount + MissingUnaffectedCount;

    /// <summary>Total bytes of the files this scan is offering for removal.</summary>
    public long RemovableTotalBytes => RemovableFiles.Sum(f => f.SizeBytes);

    /// <summary>Total bytes of the files this scan declined to offer.</summary>
    public long WithheldTotalBytes =>
        WithheldFiles?.Sum(f => f.SizeBytes) ?? 0;
}

/// <summary>
/// What the file-identity reader answered over one side of the scan's identity
/// comparison, with the five ways it can fail kept apart. One instance per side;
/// the sides are counted separately because they are asked different questions
/// about different populations and only one of them can empty an offer.
///
/// SUCCESSFUL READS ARE NOT CARRIED. They are <see cref="AttemptCount"/> less the
/// five, and a stored copy could disagree with its own parts.
///
/// A ZERO ATTEMPT COUNT IS ORDINARY AND SAYS NOTHING WENT WRONG. The comparison is
/// skipped where the walk produced no candidates, where the records hold no
/// registrations, and on the candidate side where no registration yielded an
/// identity to compare against. Five zero failures on a side that was never asked
/// look identical on the wire to five clean answers, which is what this count is
/// for.
/// </summary>
/// <param name="AttemptCount">
/// Paths put to the reader on this side, counted whether it answered or not.
/// </param>
/// <param name="NamesNothingCount">
/// Of those, the ones with no file at the path.
///
/// THE ONE FAILURE THAT IS NOT A GIVE-UP, and it is deliberately not in
/// <see cref="RefusedTotal"/>. On the registration side it is a cached file that
/// has already gone, which is ordinary and common: such a registration claims none
/// of the walked files, so nothing was lost by failing to identify it. Counting it
/// as a refusal would empty the offer on every machine holding one missing cached
/// file. On the candidate side it is a file that went between the walk and this
/// read, which no registration's identity could have matched either.
/// </param>
/// <param name="NotAPathCount">
/// Of those, the ones with no string to open at all. Neither side can produce this
/// today and a report carrying it says something nobody has seen.
/// </param>
/// <param name="OpenRefusedCount">
/// Of those, where something is at the path and no handle could be opened on it.
/// </param>
/// <param name="IdentityUnavailableCount">
/// Of those, where the handle opened and the filesystem would not give the file's
/// id: a volume or a Windows build that does not answer that class.
/// </param>
/// <param name="FaultedCount">
/// Of those, where the attempt threw rather than answering.
/// </param>
public readonly record struct FileIdentityReadTally(
    int AttemptCount = 0,
    int NamesNothingCount = 0,
    int NotAPathCount = 0,
    int OpenRefusedCount = 0,
    int IdentityUnavailableCount = 0,
    int FaultedCount = 0)
{
    /// <summary>
    /// Reads that gave up a withholding: the four failures that leave a file
    /// unidentified while it is still there.
    ///
    /// A MIXED SET, SO NOTHING MAY STATE A CAUSE FOR IT. The four are four
    /// different facts about a machine and the only thing true of every member is
    /// that the reader was asked which file a path names and did not say.
    ///
    /// SPELLED HERE AND NOWHERE ELSE. Both sides read this one expression, and the
    /// membership it defines is the same one <c>FileIdentityRead.GivesUpAWithholding</c>
    /// acts on; a copy of either would be a second, quieter version of a rule that
    /// already exists, able to answer differently after any edit to the enum.
    /// </summary>
    public int RefusedTotal =>
        NotAPathCount + OpenRefusedCount + IdentityUnavailableCount + FaultedCount;

    /// <summary>
    /// Whether this side met a path it could not identify. What the wholesale
    /// withholding asks of the registration side, and the reason it is a bool: the
    /// counts are carried apart for the report, which reads them apart, and the
    /// rule needs only whether anything failed.
    /// </summary>
    public bool AnyUnestablished => RefusedTotal > 0;
}

/// <summary>
/// Where a machine is still generating 8dot3 short names, as the opt-in report
/// records it. The four settings are Microsoft's, from the fsutil 8dot3name
/// reference; the three beyond them are the three ways of having no setting to
/// report, kept apart because a machine left at its default, a machine configured
/// with something this does not recognise and a read that failed are three
/// different findings and one label for all three would be false of two.
///
/// THE LABELS INVERT THE REGISTRY VALUE, which disables rather than enables, so
/// each says where short names are still being made.
///
/// WHY THIS APP RECORDS IT AT ALL, without which the labels read as trivia.
/// <c>Installer</c> is nine characters, so on a volume still making aliases the
/// cache folder has a short form of its own and a registered path can be spelled
/// <c>C:\Windows\INSTAL~1\1a2b3c.msi</c>. What that costs, and what settles it,
/// is <c>InstallerQueryService</c>'s business; this is the reading that says
/// whether the machine was making them.
/// </summary>
public static class ShortNameCreationLabels
{
    /// <summary>Setting 0: creation is on for every volume.</summary>
    public const string AllVolumes = "allVolumes";

    /// <summary>Setting 1: creation is off everywhere.</summary>
    public const string NoVolumes = "noVolumes";

    /// <summary>Setting 2: each volume carries its own flag, which this does not read.</summary>
    public const string PerVolume = "perVolume";

    /// <summary>
    /// Setting 3: creation is off everywhere but the system volume. Whether that
    /// covers the installer cache is a question about the machine rather than
    /// about the setting: the cache is on the system volume unless a volume is
    /// mounted at <c>C:\Windows\Installer</c>, and on that machine this setting
    /// leaves creation OFF for the cache.
    /// </summary>
    public const string SystemVolumeOnly = "systemVolumeOnly";

    /// <summary>No such value: the machine has never been configured either way.</summary>
    public const string Unset = "unset";

    /// <summary>A value that is there and is not one of the four documented settings.</summary>
    public const string Unrecognised = "unrecognised";

    /// <summary>
    /// The read failed, so nothing was established. The default for a
    /// <see cref="ScanResult"/> nobody sampled, so an unsampled scan cannot be
    /// read as a machine whose setting is known.
    /// </summary>
    public const string Unreadable = "unreadable";
}
