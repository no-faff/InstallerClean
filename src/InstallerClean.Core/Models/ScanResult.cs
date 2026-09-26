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
/// IT STATES NO CAUSE AND NOTHING BUILT ON IT MAY EITHER. One cause named for a set
/// that can have several is false of some of its members, and this
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
/// ENOUGH. A split on the state alone would call a superseded patch's absence
/// harmless, and it is not: with the superseded files gone, uninstalling the
/// superseding patch discards both patches and goes to the unpatched base, with
/// Windows looking for the absent files. So a superseded row whose product
/// condition could not be settled is NOT in here; it is in the affected half, and
/// the missing-files line speaks for it.
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
/// IT BEARS ON BOTH HALVES OF THE SCAN. A non-zero value withholds every
/// superseded-patch verdict, so the offer is shorter than the machine would otherwise
/// give. It also bears on the missing-files report: a product whose records did not
/// fully read is a product whose registrations this scan may not have seen, so the
/// count of records naming files that are not there can be short. "No missing files"
/// and "no missing files that could be seen" are different claims and only the second
/// is earned on such a run.
/// </param>
/// <param name="WithheldCount">
/// What withholding the removable class cost a run: superseded packages whose file
/// was on disk and which the scan would have offered, had it been able to say that
/// no installed product still needed them.
///
/// WHAT THE WITHHOLDING COST THIS RUN: rows Windows reports superseded whose
/// file is on disk and which declared themselves non-removable, held back because a
/// read established nothing. Obsoleted rows are NOT in it; they are not withheld,
/// they are simply not offered, and they have their own count. The predicate settles
/// it: nothing reaches the flag without having
/// carried IsRemovable, and IsRemovablePatch requires state 2. THAT IS A
/// USER-FACING CLAIM RATHER THAN AN INTERNAL ONE: the command line names the class
/// in as many words (<c>Cli.SupersededHeldBack</c>), so this count and that noun
/// have to agree.
///
/// AND A PRODUCT THAT COULD ROLL BACK ONTO THE FILE IS NOT IN IT EITHER. That
/// condition is
/// <see cref="ProductPatchSet.RemovablePatchPresent"/>, and the downgrade it reaches
/// passes withheld FALSE, because the scan positively established a live claim
/// rather than failing to establish anything. Worse() lets it beat Unestablished
/// where a row meets both, so the mixed case is excluded with it. What is left is
/// exactly one thing, in Downgrade's own words: a read that established nothing.
///
/// THE ON-DISK QUALIFIER IS THE WHOLE DIFFERENCE FROM
/// <see cref="RegisteredWithheldCount"/> AND IT IS LOAD-BEARING. A row whose file
/// has already gone held nothing back: an absent file could never have been
/// offered, the branch that offers a superseded row being gated on its existence.
/// Counting such rows here would overstate how much the withholding holds back, on
/// the one channel that answers what the app is doing on machines nobody here can
/// see. The two counts differ only on a machine that has already lost part of its
/// cache; on an intact one they are equal.
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
/// so it counts every row on that list carrying the flag, one whose file has gone
/// included; leaving such a row out would leave a hole in the partition, and the
/// partition is over the list rather than over any screen built from it.
/// <see cref="WithheldCount"/> answers what the withholding COST,
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
/// Cached patch paths whose merged row Windows reports superseded (2), one per path
/// however many programs register the patch, counted at scan time off the MACHINE and
/// never off the offer, whatever its removability, whether or not its file is on the
/// disk and whether or not anything was offered. The name says registrations and the
/// count is of paths.
///
/// THE DISTINCTION FROM THE OFFER-DERIVED FIGURE IS THE WHOLE POINT OF IT. A count
/// taken from the offer sees only the rows that reached it, so it cannot answer
/// whether a machine HAS any. The difference between the two is a mixed set, several
/// separate conditions keeping a row off the offer, so no single cause is stated for
/// it.
/// </param>
/// <param name="WithheldFiles">
/// Every candidate this scan declined to offer, in walk order. FOUR DECISIONS PUT A FILE
/// HERE AND THEY ARE NOT ONE THING, so they are listed rather than covered by a sentence
/// that would be false of one of them:
///
/// The scan could not establish which cached files belong to which programs, which
/// withholds the whole walk-derived set at once
/// (<see cref="WithholdingSplit.WholesaleCount"/>, and <see cref="WithholdingLeg"/> for
/// the three findings that empty the offer wholesale); or this one candidate's own
/// identity could not be read, so nothing could compare it against the
/// registrations and it is kept back while the rest stand
/// (<see cref="CandidateIdentityReads"/>); or the screen kept the candidate on what the
/// file declares: an installation package whose own declared product Windows still holds
/// a record of where some installation of it opens a package, its cached copy or its
/// original at a source, not shown to be another file, or whose declaration this scan
/// could not settle, or a patch whose declared patch Windows holds a registration of
/// where some registration opens a copy of the patch, its cached copy or its original at
/// a source, not shown to be another file, or whose declaration this scan could not
/// settle; or the age check kept a candidate everything
/// else let through, its age not being shown to be a day or more
/// (<see cref="WithholdingSplit.UnderADayOldCount"/> and
/// <see cref="WithholdingSplit.AgeUnestablishedCount"/>). A run can hold files put here
/// by any of the four, and a reader of this list may assume none of them.
///
/// NO SURFACE STATES A CAUSE OVER IT AND NONE MAY START. The main window counts these
/// into its left-alone line. The Details window lists them among the registrations, in
/// one list, with nothing marking a row out: no heading, no column, no indicator and no
/// lookup of a program that might have used the file. Both are true of every row
/// whichever cause put it there, and there is no per-file record of which did.
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
/// THE SUPERSEDED HALF OF THE OFFER IS NEVER PUT HERE. Those rows are judged on
/// products, through registry keys read by product code and patch code, and the
/// wholesale conditions withhold the walk's unclaimed candidates, which a superseded row
/// never is. They cannot be reached by the per-file decisions either, and the structure
/// is what says so: the identity comparison, the screen and the age check all run over
/// the walk's unclaimed candidates, and a superseded row reaches the offer from the
/// registered set without ever having been one.
///
/// IT EXISTS SO A WITHHELD FILE APPEARS ON ONE OF THE TWO SUMMARY LINES. It would
/// otherwise appear in neither: not offered, and not a registered row either, because
/// no registration names it, so it would be counted nowhere and nobody could notice.
///
/// The list rather than a count and a total, so the number shown and the rows shown
/// cannot come apart: both are read off this. Null means a scan that never reached
/// the decision, which is not the same as a scan that kept nothing back, and both
/// read as an empty list to a caller that does not care.
/// </param>
/// <param name="ObsoletedRegistrationCount">
/// The same for state 4. For this class it is the ONLY figure that can ever be
/// non-zero, obsoleted patches not being offered at all, so it is how a machine's
/// having any shows. Counting them puts nothing on anyone's list.
/// </param>
/// <param name="RegistrationIdentityReads">
/// What the file-identity reader answered when the scan asked which file each
/// registration's recorded path names. See
/// <see cref="FileIdentityReadTally"/>; <see cref="FileIdentityReadTally.RefusedTotal"/>
/// above zero is <see cref="WithholdingLeg.FileIdentityUnestablished"/>, one of the
/// conditions that withhold the walk-derived offer wholesale.
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
/// <param name="WithheldDeclaredProductInstalledBytes">
/// The size of the files <see cref="WithholdingSplit.DeclaredProductInstalledCount"/>
/// counts, so that <see cref="UnestablishedWithheldBytes"/> and
/// <see cref="UnsettledHeldBackBytes"/> can each leave them out. Carried here rather
/// than on the split, which holds counts and nothing else.
/// </param>
/// <param name="WithheldUnderADayOldBytes">
/// The size of the files <see cref="WithholdingSplit.UnderADayOldCount"/> counts, so
/// that <see cref="UnestablishedWithheldBytes"/> can leave them out, carried the same
/// way.
/// </param>
/// <param name="WithheldDeclaredPatchRegisteredBytes">
/// The size of the files <see cref="WithholdingSplit.DeclaredPatchRegisteredCount"/>
/// counts, so that <see cref="UnestablishedWithheldBytes"/> and
/// <see cref="UnsettledHeldBackBytes"/> can each leave them out, carried the same way.
/// </param>
/// <param name="SupersededWithheldBytes">
/// The size of the files <see cref="WithheldCount"/> counts, summed over the same rows:
/// superseded rows held back whose file is on disk. <see cref="UnsettledHeldBackBytes"/>
/// adds it to the size of the walk-derived files.
///
/// IT IS NOT <see cref="RegisteredSupersededBytes"/>, which sizes every superseded or
/// obsoleted row the scan is keeping, held back or not.
///
/// APPENDED AFTER EVERY OTHER MEMBER, so a positional construction of the rest still
/// means what it meant.
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
    FileIdentityReadTally RegistrationIdentityReads = default,
    FileIdentityReadTally CandidateIdentityReads = default,
    WithholdingSplit WithheldBy = default,
    long WithheldDeclaredProductInstalledBytes = 0,
    long WithheldUnderADayOldBytes = 0,
    long WithheldDeclaredPatchRegisteredBytes = 0,
    long SupersededWithheldBytes = 0)
{
    /// <summary>
    /// Every registration naming a file that is not on disk, the sum of the two
    /// sub-counts.
    ///
    /// IT IS THE FIGURE THE ANONYMOUS REPORT CARRIES, AND NOT THE ONE EITHER HOST
    /// SPEAKS. Both the window and the command line print
    /// <see cref="MissingAffectedCount"/>: the window through its own property of the
    /// same name, which is assigned that half, and the command line at every site it
    /// reads. The report carries this total alongside the affected half, so the
    /// series it feeds keeps one meaning across the release.
    ///
    /// SO FOLLOW THE ASSIGNMENT AND NOT THE NAME. A view model property spelled the
    /// same way holds a different quantity, and reading the two as one makes the
    /// banner look as though it speaks for a population it does not.
    /// </summary>
    public int MissingFromDiskCount => MissingAffectedCount + MissingUnaffectedCount;

    /// <summary>Total bytes of the files this scan is offering for removal.</summary>
    public long RemovableTotalBytes => RemovableFiles.Sum(f => f.SizeBytes);

    /// <summary>Total bytes of the files this scan declined to offer.</summary>
    public long WithheldTotalBytes =>
        WithheldFiles?.Sum(f => f.SizeBytes) ?? 0;

    /// <summary>
    /// How many withheld files the command line's held-back sentences speak of: every
    /// withheld file except those
    /// <see cref="WithholdingSplit.DeclaredProductInstalledCount"/>,
    /// <see cref="WithholdingSplit.UnderADayOldCount"/> and
    /// <see cref="WithholdingSplit.DeclaredPatchRegisteredCount"/> count.
    /// <see cref="UnestablishedWithheldBytes"/> is their size.
    ///
    /// IT IS THE LIST LESS THOSE THREE ARMS, NOT A SUM OF THE OTHERS, so a withheld
    /// file no arm counted, or one counted by an arm added to
    /// <see cref="WithholdingSplit"/> later, is counted here.
    /// </summary>
    public int UnestablishedWithheldCount =>
        Math.Max(0, (WithheldFiles?.Count ?? 0)
            - WithheldBy.DeclaredProductInstalledCount
            - WithheldBy.UnderADayOldCount
            - WithheldBy.DeclaredPatchRegisteredCount);

    /// <summary>
    /// The size of the files <see cref="UnestablishedWithheldCount"/> counts, on the same
    /// reading: the whole withheld list's size less that of the files kept because they
    /// declare a program Windows still has installed, that of the files kept because
    /// they are under a day old, and that of the patch copies kept because Windows holds
    /// a registration of the patch they declare.
    /// </summary>
    public long UnestablishedWithheldBytes =>
        Math.Max(0, WithheldTotalBytes
            - WithheldDeclaredProductInstalledBytes
            - WithheldUnderADayOldBytes
            - WithheldDeclaredPatchRegisteredBytes);

    /// <summary>
    /// Which conditions kept the walk-derived offer back, for a host that explains the
    /// withholding rather than merely reporting it.
    ///
    /// IT IS A SECOND CALLER OF THE GATE'S OWN EXPRESSION AND NOT A SECOND DERIVATION.
    /// The scan computes the gate from these same two values before it builds this
    /// result, so what a host reads here cannot disagree with what the scan acted on.
    /// The list is empty on any scan whose offer stood. It does not say whether the
    /// withholding took anything, because the gate also fires on a machine whose walk
    /// found nothing to withhold; <see cref="WithholdingSplit.WholesaleCount"/> counts
    /// what it took.
    /// </summary>
    public IReadOnlyList<WithholdingLeg> WithholdingLegsFired =>
        WithholdingLegs.Fired(Census, RegistrationIdentityReads);

    /// <summary>
    /// What this run's withholding amounts to, for the command line deciding what to
    /// tell somebody about it. The window's finished screen reads
    /// <see cref="UnsettledHeldBackCount"/> and <see cref="UnsettledHeldBackIsWholesale"/>
    /// instead.
    ///
    /// THE COMMAND LINE MAKES TWO DECISIONS AND NOT ONE: whether to say anything about a
    /// withholding at all, which <see cref="HasWithholdingToReport"/> answers, and
    /// which of the two sentences the machine has earned. It says nothing for
    /// <see cref="WithholdingAccount.Nothing"/> and
    /// <see cref="WithholdingAccount.KeptWithoutNotice"/> alike. Asking
    /// whether <see cref="WithheldFiles"/> is empty answers neither question, and nor
    /// does asking whether <see cref="WithholdingSplit.WholesaleCount"/> is above zero:
    /// it is, on a run whose wholesale branch took files after the identity pass had
    /// already kept some back one at a time.
    ///
    /// THE WHOLESALE SENTENCE NEEDS POSITIVE EVIDENCE AND THE PER-FILE ONE DOES NOT.
    /// That asymmetry is the whole of why this is written as "the wholesale arm
    /// accounts for all of them" rather than as "no per-file arm fired". The per-file
    /// sentence says the scan could not establish these files were unneeded, which is
    /// true of every file on that list whatever put it there. The wholesale sentence
    /// names a cause, and that cause is false of a file kept back one at a time, such
    /// as one whose declared product Windows still holds a record of: the wholesale
    /// cause is a finding about the machine's records and did not keep that file. So
    /// the wholesale reading is reached only where the wholesale arm accounts for the
    /// whole list, and everything else takes the sentence that is true of all of them.
    ///
    /// WHICH IS ALSO WHAT MAKES THIS RIGHT OVER A SPLIT THAT HAS FALLEN SHORT. A file
    /// on the list that no arm counted leaves the wholesale arm short of the list's own
    /// length, so the machine takes the per-file sentence, which is still true of it.
    /// Read the other way round, an uncounted file would have been swept into a cause
    /// nobody established.
    ///
    /// A RUN WHOSE WITHHELD FILES WERE ALL COUNTED BY THE DECLARED-PRODUCT-INSTALLED,
    /// UNDER-A-DAY-OLD AND DECLARED-PATCH-REGISTERED ARMS READS AS
    /// <see cref="WithholdingAccount.KeptWithoutNotice"/>. The test is that those three
    /// arms account for the whole list, never that no other arm fired, so a file no
    /// arm counted, or one counted by an arm added later, keeps the run on the per-file
    /// sentence.
    ///
    /// IT IS DERIVED AND NOT CARRIED, so nothing can set it apart from the two values
    /// it is read off and leave a result disagreeing with itself.
    /// </summary>
    public WithholdingAccount Withholding
    {
        get
        {
            var withheld = WithheldFiles?.Count ?? 0;
            if (withheld == 0) return WithholdingAccount.Nothing;

            if (WithheldBy.WholesaleCount == withheld)
                return WithholdingAccount.WholeWalkOffer;

            return WithheldBy.DeclaredProductInstalledCount
                + WithheldBy.UnderADayOldCount
                + WithheldBy.DeclaredPatchRegisteredCount == withheld
                    ? WithholdingAccount.KeptWithoutNotice
                    : WithholdingAccount.PerFile;
        }
    }

    /// <summary>
    /// Whether the command line has anything to say about this run's withholding: false
    /// for <see cref="WithholdingAccount.Nothing"/> and
    /// <see cref="WithholdingAccount.KeptWithoutNotice"/>, true for every other
    /// reading. Written as the two silent readings excluded, so a reading added to the
    /// enum is reported rather than silenced.
    /// </summary>
    public bool HasWithholdingToReport =>
        Withholding is not (WithholdingAccount.Nothing or WithholdingAccount.KeptWithoutNotice);

    /// <summary>
    /// Whether the conditions the command line can name account for every file
    /// <see cref="UnestablishedWithheldCount"/> counts: the wholesale arm, spoken for by
    /// <see cref="WithholdingLegsFired"/>, and the arms
    /// <see cref="WithholdingSplit.ArmsFired"/> can return.
    ///
    /// WHERE IT IS FALSE, A LIST OF REASONS UNDER THE HELD-BACK SENTENCE WOULD BE SHORT
    /// OF THE FILES THAT SENTENCE COUNTS, so the command line prints the sentence on its
    /// own. The age-unestablished arm has no reason line and is the arm that makes it
    /// false; a file no arm counted makes it false too.
    ///
    /// WRITTEN AS THE NAMED ARMS ADDING UP TO THE COUNT, so an arm added to
    /// <see cref="WithholdingSplit"/> later without a reason line makes it false rather
    /// than leaving a list that leaves its files out.
    /// </summary>
    public bool NamedConditionsCoverEveryHeldBackFile =>
        WithheldBy.WholesaleCount
        + WithheldBy.IdentityUnestablishedCount
        + WithheldBy.DeclaredProductUnestablishedCount
        + WithheldBy.ScreenUnansweredCount
        + WithheldBy.DeclaredPatchUnestablishedCount
        == UnestablishedWithheldCount;

    /// <summary>
    /// How many files the window's finished screen says were held back: every file on
    /// <see cref="WithheldFiles"/> except those
    /// <see cref="WithholdingSplit.DeclaredProductInstalledCount"/> and
    /// <see cref="WithholdingSplit.DeclaredPatchRegisteredCount"/> count, together with
    /// the superseded files <see cref="WithheldCount"/> counts.
    /// <see cref="UnsettledHeldBackBytes"/> is their size.
    ///
    /// THE TWO ARMS LEFT OUT KEEP A FILE BECAUSE WINDOWS HOLDS A RECORD OF THE PROGRAM OR
    /// PATCH IT DECLARES. Every other file held back, and every superseded file
    /// <see cref="WithheldCount"/> counts, was kept without the scan establishing either
    /// way whether anything needs it: a file under a day old, a file whose age was not
    /// established, a superseded patch the scan held back, and a file kept on any other
    /// verdict alike.
    ///
    /// IT IS THE LIST LESS THOSE TWO ARMS, NOT A SUM OF THE OTHERS, so a walk-derived file
    /// no arm counted, or one counted by an arm added to <see cref="WithholdingSplit"/>
    /// later, is counted here.
    ///
    /// THE COMMAND LINE DOES NOT READ IT. It speaks
    /// <see cref="UnestablishedWithheldCount"/> and <see cref="WithheldCount"/> in separate
    /// sentences, and the first of those leaves out a file under a day old.
    /// </summary>
    public int UnsettledHeldBackCount =>
        Math.Max(0, (WithheldFiles?.Count ?? 0)
            - WithheldBy.DeclaredProductInstalledCount
            - WithheldBy.DeclaredPatchRegisteredCount)
        + WithheldCount;

    /// <summary>
    /// The size of the files <see cref="UnsettledHeldBackCount"/> counts, on the same
    /// reading: the withheld list's size less that of the files the
    /// declared-product-installed and declared-patch-registered arms count, together with
    /// <see cref="SupersededWithheldBytes"/>.
    /// </summary>
    public long UnsettledHeldBackBytes =>
        Math.Max(0, WithheldTotalBytes
            - WithheldDeclaredProductInstalledBytes
            - WithheldDeclaredPatchRegisteredBytes)
        + SupersededWithheldBytes;

    /// <summary>
    /// Whether the window's finished screen, on a run that offered nothing, speaks of
    /// files held back rather than giving the all-clear: true wherever
    /// <see cref="UnsettledHeldBackCount"/> is above zero.
    /// </summary>
    public bool HasUnsettledHeldBack => UnsettledHeldBackCount > 0;

    /// <summary>
    /// Whether the wholesale arm accounts for every file
    /// <see cref="UnsettledHeldBackCount"/> counts. Where it does, the window's finished
    /// screen gives the sentence naming what the scan could not establish about the
    /// machine's records; on any other run with files to count, it gives the sentence
    /// true of every one of them, on the reasoning <see cref="Withholding"/> sets out.
    ///
    /// A SUPERSEDED FILE ALWAYS TAKES THE OTHER SENTENCE. The wholesale arm counts
    /// walk-derived files alone, so it cannot account for a superseded one.
    ///
    /// FALSE WHERE NOTHING IS COUNTED. A wholesale arm of zero equals a count of zero, and
    /// that equality is evidence of nothing.
    /// </summary>
    public bool UnsettledHeldBackIsWholesale =>
        UnsettledHeldBackCount > 0 && WithheldBy.WholesaleCount == UnsettledHeldBackCount;
}

/// <summary>
/// What a scan's withholding amounts to, in the terms the command line has to speak it.
/// The window's finished screen does not read it; its reading is
/// <see cref="ScanResult.UnsettledHeldBackCount"/>.
///
/// THE COMMAND LINE STAYS SILENT FOR <see cref="Nothing"/> AND
/// <see cref="KeptWithoutNotice"/> AND FOR NOTHING ELSE, through
/// <see cref="ScanResult.HasWithholdingToReport"/>, and speaks the wholesale sentence for
/// <see cref="WholeWalkOffer"/> and the per-file one for every other member. A member
/// added later is therefore spoken of in the sentence true of every file, rather than
/// passed over.
///
/// IT IS A READING OF A RESULT AND NOT A DECISION OF ITS OWN. Nothing sets one of
/// these; <see cref="ScanResult.Withholding"/> derives it from the withheld list and
/// the split, so it cannot drift from either.
/// </summary>
public enum WithholdingAccount
{
    /// <summary>
    /// This scan kept nothing back from the folder walk, so the command line's walk
    /// sentence has nothing to count.
    /// </summary>
    Nothing,

    /// <summary>
    /// Every file kept back was kept by the wholesale arm, so the sentence naming what
    /// the scan could not establish about the machine's records is true of all of them.
    /// </summary>
    WholeWalkOffer,

    /// <summary>
    /// Files were kept back and neither the wholesale arm nor the three arms read as
    /// <see cref="KeptWithoutNotice"/> account for all of them, so the only sentence
    /// true of every file this reading counts is that the scan could not establish they
    /// were unneeded. A run that kept files back both ways reads as this, the wholesale
    /// sentence being false of the half it did not cover.
    ///
    /// THE COMMAND LINE'S SENTENCE COUNTS
    /// <see cref="ScanResult.UnestablishedWithheldCount"/>, NOT THE WHOLE LIST, so a file
    /// any of those three arms kept is left out of it. A file the age check kept because
    /// its age was not established is in it.
    /// </summary>
    PerFile,

    /// <summary>
    /// Every file kept back was counted by the declared-product-installed arm, the
    /// under-a-day-old arm or the declared-patch-registered arm. The first is a file
    /// that declares a program Windows still has installed, where at least one
    /// installation of that program opens a package, its cached copy or its original at
    /// a source, that the check could not show is a different file. The second is a file
    /// whose times were read and show it was created, written or changed less than a day
    /// before the scan, or no more than a day after it, which every later scan judges
    /// afresh. The third is a patch copy whose declared patch Windows holds a
    /// registration of, where at least one registration opens a copy of the patch, its
    /// cached copy or its original at a source, that the check could not show is a
    /// different file. The command line says what it says on a run that kept nothing
    /// back, and the files stay among those left alone. The window's finished screen
    /// counts a file under a day old among those held back, through
    /// <see cref="ScanResult.UnsettledHeldBackCount"/>.
    /// </summary>
    KeptWithoutNotice,
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

/// <summary>
/// Which decision kept each file on <see cref="ScanResult.WithheldFiles"/> back.
///
/// EXACTLY FOUR DECISIONS PUT A FILE ON THAT LIST AND THEY ARE MUTUALLY EXCLUSIVE
/// PER FILE, so this is a partition of it rather than nine overlapping views. The
/// identity comparison keeps one candidate at a time; the wholesale arm keeps every
/// remaining candidate in one go and the per-file screen and age check are skipped
/// entirely; the screen keeps a candidate on its own verdict, counted in an arm per
/// withholding verdict; and the age check keeps a candidate the screen let through that
/// has not been shown to be a day old, counted in two arms by whether its age was
/// established. A file an earlier decision has already taken is off the list a later
/// one is handed, so nothing lands twice.
///
/// THE COUNTS ARE CARRIED APART BECAUSE THEY ARE READ APART. Each member is one fact
/// about one machine, and nothing may add any two of them and call the result a
/// cause: what is true of every file on the list is only that the scan declined to
/// offer it. The opt-in report carries all nine, each under its own key, and a member
/// added here goes there too. Inside the app, the declared-product-installed,
/// under-a-day-old and declared-patch-registered counts are read by
/// <see cref="ScanResult.UnestablishedWithheldCount"/> and
/// <see cref="ScanResult.Withholding"/>, and the first and last of those three by
/// <see cref="ScanResult.UnsettledHeldBackCount"/> as well. The wholesale count is read
/// by <see cref="ScanResult.Withholding"/>,
/// <see cref="ScanResult.NamedConditionsCoverEveryHeldBackFile"/> and
/// <see cref="ScanResult.UnsettledHeldBackIsWholesale"/>. The age-unestablished count is
/// read by <see cref="Total"/> alone, and the other four by <see cref="ArmsFired"/> and
/// <see cref="ScanResult.NamedConditionsCoverEveryHeldBackFile"/>.
///
/// <see cref="Total"/> IS WHAT HOLDS THE PARTITION HONEST, and it is asserted against
/// the list's own length rather than trusted. A partition is a partition until
/// somebody adds a branch, and a tenth arm arriving later would appear in none of
/// these nine while the list grew underneath them.
/// </summary>
/// <param name="UnderADayOldCount">
/// Candidates the age check kept back as under a day old: every other decision let the
/// file through, its times were read on a local NTFS volume, and the latest of them is
/// less than <see cref="Services.CachedFileAge.MinimumAge"/> before the scan's own
/// clock, or no more than that after it. See <see cref="Services.CachedFileAge.Judge"/>.
///
/// APPENDED AFTER THE OTHER FIVE, so a positional construction of the first five still
/// means what it meant.
/// </param>
/// <param name="AgeUnestablishedCount">
/// Candidates the age check kept back because their age was not established: the
/// reader answered anything but <see cref="Services.FileTimesRead.Read"/>, so the
/// times could not be read, or were not read on a local, fixed NTFS volume; or the
/// latest of them is more than <see cref="Services.CachedFileAge.MinimumAge"/> after
/// the scan's own clock, which is not an age.
/// </param>
/// <param name="DeclaredPatchRegisteredCount">
/// Patch copies the screen kept back because Windows holds a registration of the patch
/// each declares, and for at least one registration the screen could not show that every
/// copy of the patch it opens, cached or original, is a different file. See
/// <see cref="Services.DeclaredProductOutcome.DeclaredPatchRegistered"/>.
///
/// APPENDED AFTER THE OTHER SEVEN, so a positional construction of the first seven
/// still means what it meant.
/// </param>
/// <param name="DeclaredPatchUnestablishedCount">
/// Patch copies the screen kept back because the copy yielded no patch code and target
/// products to ask about, or the registrations of the patch it declares could not all be
/// found. See <see cref="Services.DeclaredProductOutcome.DeclaredPatchUnestablished"/>.
///
/// APPENDED AFTER THE OTHER EIGHT, so a positional construction of the first eight
/// still means what it meant.
/// </param>
public readonly record struct WithholdingSplit(
    int IdentityUnestablishedCount = 0,
    int WholesaleCount = 0,
    int DeclaredProductInstalledCount = 0,
    int DeclaredProductUnestablishedCount = 0,
    int ScreenUnansweredCount = 0,
    int UnderADayOldCount = 0,
    int AgeUnestablishedCount = 0,
    int DeclaredPatchRegisteredCount = 0,
    int DeclaredPatchUnestablishedCount = 0)
{
    /// <summary>
    /// Every file the nine account for. It equals <see cref="ScanResult.WithheldFiles"/>'s
    /// own length on any scan that filled both, and a test holds it there.
    ///
    /// IT IS A COUNT AND NEVER A CAUSE. The nine members are nine different findings
    /// about a machine, so this figure answers "how many were held back" and nothing
    /// whatever about why.
    /// </summary>
    public int Total =>
        IdentityUnestablishedCount
        + WholesaleCount
        + DeclaredProductInstalledCount
        + DeclaredProductUnestablishedCount
        + ScreenUnansweredCount
        + UnderADayOldCount
        + AgeUnestablishedCount
        + DeclaredPatchRegisteredCount
        + DeclaredPatchUnestablishedCount;

    /// <summary>
    /// Which of the per-file decisions the scan could not settle kept anything back,
    /// in declaration order, for the command line, which explains the withholding
    /// rather than only reporting it.
    ///
    /// THE WHOLESALE ARM IS NOT AMONG THEM. <see cref="WholesaleCount"/> counts files
    /// kept back on a condition about the machine's records, and
    /// <see cref="ScanResult.WithholdingLegsFired"/> already names which of those
    /// conditions held. A line built on the count would say less than the legs do and
    /// would say it a second time.
    ///
    /// NOR ARE THE DECLARED-PRODUCT-INSTALLED, UNDER-A-DAY-OLD, DECLARED-PATCH-REGISTERED
    /// AND AGE-UNESTABLISHED ARMS, so the command line names no reason for the files
    /// those four count. It says nothing of the first three. The fourth is spoken of in
    /// its per-file sentence with no line of its own, and
    /// <see cref="ScanResult.NamedConditionsCoverEveryHeldBackFile"/> is what tells it
    /// that such files are among those the sentence counts. The window's finished screen
    /// names no reasons at all, and counts the under-a-day-old and age-unestablished arms'
    /// files among those it says were held back.
    ///
    /// A MEMBER MEANS ONE DECISION KEPT AT LEAST ONE FILE, AND NEVER A CAUSE FOR ANY
    /// PARTICULAR ONE. Any combination of them can hold at once, so nothing sums over
    /// this list: <see cref="Total"/> is the sum, and it answers how many rather than
    /// why any single file is on the list.
    /// </summary>
    public IReadOnlyList<WithholdingSplitArm> ArmsFired
    {
        get
        {
            var fired = new List<WithholdingSplitArm>(4);

            if (IdentityUnestablishedCount > 0)
                fired.Add(WithholdingSplitArm.IdentityUnestablished);
            if (DeclaredProductUnestablishedCount > 0)
                fired.Add(WithholdingSplitArm.DeclaredProductUnestablished);
            if (ScreenUnansweredCount > 0)
                fired.Add(WithholdingSplitArm.ScreenUnanswered);
            if (DeclaredPatchUnestablishedCount > 0)
                fired.Add(WithholdingSplitArm.DeclaredPatchUnestablished);

            return fired;
        }
    }
}

/// <summary>
/// The per-file withholding decisions the scan could not settle, one member per arm of
/// <see cref="WithholdingSplit"/> that speaks for itself. The wholesale arm is spoken
/// for by the legs, and the declared-product-installed, under-a-day-old,
/// declared-patch-registered and age-unestablished arms have no reason line, so none of
/// the five has a member.
///
/// ONE MEMBER PER ARM RATHER THAN ONE PER CAUSE. They are different inabilities, so
/// nothing may add them together or write one sentence over them that names a cause.
///
/// THE ORDER IS THE ORDER THEY ARE REPORTED IN, and it is the order of the arms they
/// name, so a reader holding a breakdown against <see cref="WithholdingSplit"/> meets
/// them the same way in each.
/// </summary>
public enum WithholdingSplitArm
{
    /// <summary>A file in the folder would not identify itself.</summary>
    IdentityUnestablished,

    /// <summary>
    /// A file would not say which product it belongs to, or Windows would not answer
    /// about the product it named. Two inabilities under one arm, as the verdict they
    /// come from keeps them.
    /// </summary>
    DeclaredProductUnestablished,

    /// <summary>
    /// The screen answered about a different number of files than it was handed, so
    /// none of its answers could be read against a file.
    /// </summary>
    ScreenUnanswered,

    /// <summary>
    /// A patch copy would not say which patch it is, or would not say which products it
    /// is for, or Windows would not fully answer which installations hold the patch it
    /// named. Several inabilities under one arm, as the verdict they come from keeps
    /// them.
    /// </summary>
    DeclaredPatchUnestablished,
}

/// <summary>
/// The named conditions that keep the walk-derived offer back, one member per leg.
///
/// THEY ARE CONDITIONS THE RUN MET AND NOT A PARTITION OF ANY FILE SET. Any
/// combination of them can hold at once and none of them counts anything, so nothing
/// sums over these and no line built on one may state a cause for the files that were
/// withheld.
///
/// THE ORDER IS THE ORDER THEY ARE REPORTED IN, so a reader meeting two of them meets
/// them the same way twice.
/// </summary>
public enum WithholdingLeg
{
    /// <summary>A path in Windows Installer's own records would not resolve.</summary>
    RecordedPathUnestablished,

    /// <summary>The identity of a file named in those records would not read.</summary>
    FileIdentityUnestablished,

    /// <summary>A product may be installed more than once on this machine.</summary>
    SecondInstanceNotRuledOut,
}

/// <summary>
/// The one place the walk-offer withholding is decided, and the reason it is a type
/// rather than an expression written twice.
///
/// THE GATE AND THE HOST THAT EXPLAINS IT READ THE SAME CALL. The scan asks whether
/// anything fired; the command line asks which. Written as two expressions they agree
/// until one of them changes, after which the gate can grow a fourth condition while
/// the breakdown under it goes on naming three, every test still green and the output
/// still looking like an answer. Here a leg added to the enum is a leg the gate acts
/// on and a leg the host prints, and a leg added without a line is a failing test.
/// </summary>
public static class WithholdingLegs
{
    /// <summary>
    /// Every leg that fired, in declaration order.
    ///
    /// The census is asked about two of them and the registration side of the identity
    /// comparison about the third, each through the property that owns the question
    /// rather than by naming its members here: a rule that named them itself would be
    /// one edit away from silently not acting on a member added later.
    /// </summary>
    public static IReadOnlyList<WithholdingLeg> Fired(
        EnumerationCensus census, FileIdentityReadTally registrationIdentityReads)
    {
        var fired = new List<WithholdingLeg>(3);

        if (census.AnyRecordedPathUnestablished)
            fired.Add(WithholdingLeg.RecordedPathUnestablished);
        if (registrationIdentityReads.AnyUnestablished)
            fired.Add(WithholdingLeg.FileIdentityUnestablished);
        if (census.SecondInstanceNotRuledOut)
            fired.Add(WithholdingLeg.SecondInstanceNotRuledOut);

        return fired;
    }

    /// <summary>
    /// Whether the walk-derived offer is withheld wholesale: any leg at all.
    ///
    /// It calls <see cref="Fired"/> rather than repeating its conditions, which is the
    /// whole point of the type. The list is at most three entries and is built once per
    /// scan.
    /// </summary>
    public static bool Any(
        EnumerationCensus census, FileIdentityReadTally registrationIdentityReads) =>
        Fired(census, registrationIdentityReads).Count > 0;
}
