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
/// as one. Three populations are in here and only the first carries a claim: a
/// path a live registration names, superseded and obsoleted patches included
/// (Windows holds those too, and their state is not a statement about the file),
/// counted in <see cref="RegisteredClaimedCount"/>; a superseded patch whose
/// removable verdict the scan took away, because it could not establish something
/// the offer needs (<c>RemovableWithheld</c>), counted in
/// <see cref="RegisteredWithheldCount"/>; and a patch whose State or Uninstallable
/// read failed, or came back empty where its verdict turns on it
/// (<c>VerdictUnreadable</c>), about which nothing was established at all, counted
/// in <see cref="RegisteredUnjudgedCount"/>. All three are kept. What they do not
/// share is a sentence, which is why each has its own count.
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
/// withhold that row's verdict, unless it was withheld on a failed read of its own
/// patch file. Everything else is in here: a product's own cached package, an
/// applied patch, a patch whose state no read established, a path only the registry
/// fallback named, every superseded row whose product condition could not be
/// settled, and every row withheld on anything else, the scan-wide withholding's
/// included.
///
/// THE THIRD CONJUNCT KEEPS A WITHHELD ROW IN THE COUNT. The scan-wide withholding
/// takes the verdict off rows the per-product pass had already judged clean, so such
/// a row carries the withheld flag and an AllNonRemovable verdict at once. A scan that
/// has declined to rely on that verdict does not rely on it to leave the row out of
/// this count, the notice or the program's name.
///
/// IT STATES NO CAUSE AND NOTHING BUILT ON IT MAY EITHER. A cached file can go
/// missing in more than one way, and one cause named for a set that can have
/// several is false of some of its members. What the number says is that Windows
/// holds records naming files that are not there.
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
/// Program entries in Windows Installer's records this scan could not settle, carried
/// through from <see cref="InstallerQueryResult.UnaccountedProductCount"/>, whose remarks
/// are the ones to read before quoting this: it is not confined to records that failed
/// to read. A non-zero value withholds every superseded-patch verdict, so the offer is
/// shorter than the machine would otherwise give.
/// </param>
/// <param name="WithheldCount">
/// What withholding the removable class cost a run: superseded patches whose file
/// was on disk and whose removable verdict the scan took away, because it could not
/// establish something the offer needs.
///
/// SUPERSEDED ROWS ONLY. Obsoleted rows are NOT in it; they are not withheld, they
/// are simply not offered, and they have their own count. The predicate settles it:
/// nothing reaches the flag without having carried IsRemovable, and IsRemovablePatch
/// requires state 2. THAT IS A USER-FACING CLAIM RATHER THAN AN INTERNAL ONE: the
/// Application log entry for it names the class in as many words
/// (<c>Cli.EventLogSupersededHeldBack</c>), so this count and that noun have to agree.
///
/// AND A PRODUCT THAT COULD ROLL BACK ONTO THE FILE IS NOT IN IT EITHER. That
/// condition is
/// <see cref="ProductPatchSet.RemovablePatchPresent"/>, and the downgrade it reaches
/// passes withheld FALSE, because the scan positively established a live claim
/// rather than failing to establish anything. Worse() lets it beat Unestablished
/// where a row meets both, so the mixed case is excluded with it. What is left is the
/// scan failing to establish something, about one file or about the whole machine.
/// About one file, it is a read about the patch that established nothing, in
/// Downgrade's own words, or a product key no installation accounts for whose patch
/// listing names the patch. About the whole machine, it is one of the conditions that
/// withhold every such row at once (<see cref="RegisteredPackage.WithheldScanWide"/>): a
/// program entry the scan could not check, a cached patch file it could not match to a
/// program it asks about, a recorded path it could not settle, a machine-wide list of
/// patch registrations that did not run to its end, a product key no installation
/// accounts for whose patch listing was not established, or a failed read of Windows
/// Installer's records in the registry.
/// <see cref="SupersededScanWideWithheldCount"/> counts the rows those withheld, and
/// <see cref="SupersededRecordedPathUnestablishedCount"/> those the scan-wide withholding
/// took while a recorded path went unsettled.
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
/// <see cref="InstallerQueryResult.Census"/>. The opt-in report reads it. The
/// withholdings it drives are decided on the enumeration's own copy, before this result
/// is built.
/// </param>
/// <param name="ShortNameCreation">
/// The machine's 8dot3 short-name creation policy, one of
/// <see cref="ShortNameCreationLabels"/>. Sampled once per scan and used for
/// nothing but the opt-in report, so it is a plain label with no default assumed:
/// an unconfigured machine reads as <see cref="ShortNameCreationLabels.Unset"/>
/// rather than as whichever setting a document guesses is usual.
/// </param>
/// <param name="RegisteredClaimedCount">
/// Kept rows carrying neither <c>RemovableWithheld</c> nor <c>VerdictUnreadable</c>.
/// Each is a file a live registration positively claims (a product's own cached
/// package, an applied patch, a path the registry fallback named, or a patch Windows
/// reports superseded or obsoleted), except a superseded patch that kept its removable
/// verdict and was not offered: one the containment check refused or could not place,
/// which is kept on that finding, and one whose file was not on the disk when the scan
/// looked, which had nothing to offer. Apart from those rows, the one population in
/// <see cref="RegisteredPackages"/> that a sentence about being needed is true of.
///
/// SUPERSEDED AND OBSOLETED PATCHES CARRYING NEITHER FLAG ARE IN IT. Microsoft's own
/// word for both states is "applied", and Windows opens the cached file of every patch
/// registered to a product whether or not it has been superseded.
/// <see cref="RegisteredSupersededCount"/> counts rows in this, in
/// <see cref="RegisteredWithheldCount"/> and in <see cref="RegisteredUnjudgedCount"/>,
/// so it is added to none of them.
/// </param>
/// <param name="RegisteredClaimedBytes">
/// The same population's bytes, files on disk only, on the same rule as
/// <see cref="RegisteredTotalBytes"/>. It exists because a count and a size shown
/// together are read as one statement: a size taken across both populations
/// beside a count of one of them would attribute the other's space to files this
/// scan says are needed.
/// </param>
/// <param name="RegisteredWithheldCount">
/// Kept ROWS Windows reports superseded whose removable verdict the scan took away,
/// because it could not establish something the offer needs: every row carrying
/// <c>RemovableWithheld</c>, whether or not its file is still on the disk.
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
/// with no superseded patch and on a machine whose every superseded patch was offered,
/// and those are different findings that this count cannot separate; the scan-time
/// registration counts are what separate them.
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
/// which is not the same as the population that exists: a superseded row the scan
/// offers has left this list, so what is counted here is every superseded row it
/// did not offer and every obsoleted row, which it never offers.
///
/// A SUB-COUNT AND NOT A PARTITION MEMBER, and its rows can sit in any of the three.
/// A row carrying neither flag is in <see cref="RegisteredClaimedCount"/>;
/// a superseded row whose removable verdict the scan took away, because it could not
/// establish something the offer needs, is in <see cref="RegisteredWithheldCount"/>;
/// and a patch whose State read gave 2 or 4 and whose Uninstallable read then failed,
/// or gave 2 and whose Uninstallable came back empty, is in
/// <see cref="RegisteredUnjudgedCount"/>. So it is added to none of them.
/// A superseded or obsoleted row whose file has already gone is in one of the two
/// missing counts instead, decided by <c>MissingFilesReport.Affected</c> rather than
/// by the state.
/// </param>
/// <param name="RegisteredSupersededBytes">
/// The same population's bytes: the space on disk taken by the superseded and
/// obsoleted patches this scan keeps. Files on disk only, on the same rule as
/// <see cref="RegisteredTotalBytes"/>.
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
/// Every file from the folder walk this scan declined to offer, each decision's files in
/// walk order. FIVE DECISIONS PUT A FILE HERE AND THEY ARE NOT ONE THING, so they are
/// listed rather than covered by a sentence that would be false of one of them:
///
/// The containment check refused the file, which is a reparse point or resolves somewhere
/// other than directly in the folder, or could not answer for it
/// (<see cref="WithholdingSplit.ContainmentRefusedCount"/> and
/// <see cref="WithholdingSplit.ContainmentUnestablishedCount"/>); or the scan could not
/// establish which cached files belong to which programs, which withholds the whole
/// walk-derived set at once (<see cref="WithholdingSplit.WholesaleCount"/>, and
/// <see cref="WithholdingLeg"/> for the three findings behind it); or this one
/// candidate's own
/// identity could not be read, so nothing could compare it against the
/// registrations and it is kept back while the rest stand
/// (<see cref="CandidateIdentityReads"/>); or the screen kept the candidate on what the
/// file declares: an installation package whose own declared product Windows still holds
/// a record of where some installation of it opens a package, its cached copy or its
/// original at a source, not shown to be another file, one that an installation not
/// ruled out as a second copy opens as its package, one kept while the packages such an
/// installation opens could not all be seen, or one whose declaration this scan could
/// not settle, or a patch whose declared patch Windows holds a registration of
/// without every registration recording another present copy, or whose declaration
/// this scan could not settle; or the age check kept a candidate everything
/// else let through, it not being shown to have last changed before a moment Windows
/// Installer had nothing in progress (<see cref="WithholdingSplit.ScanSignalCount"/>,
/// <see cref="WithholdingSplit.NotBeforeTheClockCount"/>,
/// <see cref="WithholdingSplit.AgeUnestablishedCount"/> and
/// <see cref="WithholdingSplit.ClockSetBackCount"/>). A run can hold files put here
/// by any of the five, and a reader of this list may assume none of them.
///
/// NO SURFACE STATES A CAUSE OVER IT AND NONE MAY START. The main window counts these
/// into its left-alone line. The Details window lists them among the registrations, in
/// one list, with nothing marking a row out: no heading, no column, no indicator and no
/// lookup of a program that might have used the file. Both are true of every row
/// whichever cause put it there, and there is no per-file record of which did.
///
/// WHY THE FIRST CAUSE TAKES A WHOLE SET AND NOT ONE FILE, and the answer is the same
/// shape for all three findings behind it: the app knows a needed file may be sitting in
/// the candidate list and cannot say WHICH. For an unspellable claim, the claim is kept
/// in the raw spelling Windows gave, so it matches nothing the walk produces, and the
/// identity match cannot help either, because a value the path API refuses is a value
/// CreateFile refuses too and there is nothing to open and compare. For a product the
/// registry names that this scan could not ask about, nothing shows it is not a second
/// copy of a program, whose original package declares the base product code and can be a
/// file in the folder that the copy's source list names; and no file in the folder is
/// compared with a product nothing asked, whether as a package it opens or as a patch it
/// holds. Every unclaimed file of the set is therefore one that could have been meant,
/// and the app cannot say of any of them that nothing needs it.
///
/// THE SUPERSEDED HALF OF THE OFFER IS NEVER PUT HERE. Those rows are judged on
/// products, through registry keys read by product code and patch code, and the
/// wholesale conditions withhold the walk's unclaimed candidates, which a superseded row
/// never is. They cannot be reached by the per-file decisions either, and the structure
/// is what says so: the identity comparison, the screen and the age check all run over
/// the walk's unclaimed candidates, and a superseded row reaches the offer from the
/// registered set without ever having been one. A superseded row the containment check
/// refuses stays on <see cref="RegisteredPackages"/>, which already puts it on the
/// left-alone line, and is counted in <see cref="SupersededContainmentRefusedCount"/> or
/// <see cref="SupersededContainmentUnestablishedCount"/>.
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
/// <param name="WithheldNotBeforeTheClockBytes">
/// The size of the files <see cref="WithholdingSplit.NotBeforeTheClockCount"/> counts, so
/// that <see cref="Helpers.RecentChangeReport"/> can give their size. Carried here rather
/// than on the split, which holds counts and nothing else.
/// </param>
/// <param name="SupersededContainmentRefusedCount">
/// Superseded rows whose file is on disk, that carried a removable verdict to the offer,
/// and that the containment check then refused: the recorded path is a reparse point, or
/// resolves somewhere other than directly in the folder. The row stays on
/// <see cref="RegisteredPackages"/>, so the left-alone line and the Details window already
/// have it as a registered file.
///
/// IT IS NOT <see cref="WithheldCount"/>, which counts superseded rows on disk whose
/// removable verdict was taken away. These rows kept theirs.
/// <see cref="SupersededHeldBackCount"/> adds the three together for both hosts' counts of
/// files held back, and the opt-in report carries each apart.
///
/// APPENDED AFTER THE MEMBERS ABOVE IT, so a positional construction of those still
/// means what it meant.
/// </param>
/// <param name="SupersededContainmentUnestablishedCount">
/// The same rows where the containment check could not answer: the path's attributes, or
/// the location it resolves to, would not read, or the Installer folder's own location was
/// not established, so where the path resolves could not be compared with it.
///
/// NOT ADDED TO <see cref="SupersededContainmentRefusedCount"/> ANYWHERE BUT IN
/// <see cref="SupersededHeldBackCount"/>, which states no cause. One is a finding about the
/// file and the other the check failing to answer.
/// </param>
/// <param name="SourceRootsGivenUp">
/// Every drive or share, or path of another form, the declared-product screen stopped
/// reading in this scan, with the route it was given up by and how many files the screen
/// kept at it (<see cref="Services.DeclaredProductScreening.RootsGivenUp"/>). Empty where it gave none
/// up, and where the screen did not run or its answer was not used.
/// <see cref="SourceRootsGivenUpKeepingFiles"/> is the part of it a host speaks of.
///
/// IT IS NOT AN ARM OF <see cref="WithheldBy"/>. Every file it counts is on
/// <see cref="WithheldFiles"/> and counted there under the screen's own verdict, so its
/// counts are never added to the split's.
/// </param>
/// <param name="SourceWaitCount">
/// How many waits the declared-product screen made in this scan, each a read still waiting
/// after about a second, for which a host showing the waits put its waiting line up
/// (<see cref="Services.DeclaredProductScreening.WaitCount"/>). Counted whether or not the
/// scan used the screen's answer, and nought where the screen did not run.
/// </param>
/// <param name="CachedPackageCensus">
/// What the declared-product screen found in this scan of the two conditions under which it
/// holds back every installation package it would otherwise let through
/// (<see cref="Services.DeclaredProductScreening.CachedPackages"/>). A pass that looked at no
/// installation where the screen did not run, looked at none, or its answer was not used.
/// </param>
/// <param name="WithheldDeclaredProductInstalledCauses">
/// The files <see cref="WithholdingSplit.DeclaredProductInstalledCount"/> counts, by why the
/// declared-product screen kept each one (<see cref="Services.DeclaredProductScreening.InstalledCauses"/>).
/// None counted where the screen did not run, kept no file for that reason, or its answer was not
/// used.
///
/// APPENDED AFTER <see cref="CachedPackageCensus"/>, so a positional construction of the
/// members above still means what it meant.
/// </param>
/// <param name="InstallerActivityAtScan">
/// What the scan read of Windows Installer's in-progress signals just after its clock: a
/// block where any was set, and every file from the folder walk the age check is handed
/// is then held back in the arm <see cref="WithholdingSplit.ScanSignalCount"/> counts.
/// Null where the scan read none, which no production scan does.
///
/// APPENDED AFTER <see cref="WithheldDeclaredProductInstalledCauses"/>, so a positional
/// construction of the members above still means what it meant.
/// </param>
/// <param name="OwnPerUserSourceLists">
/// The source lists of the running account's own per-user-unmanaged installations the
/// declared-product screen read in this scan, each installation once
/// (<see cref="Services.DeclaredProductScreening.OwnPerUserLists"/>). None where the screen did
/// not run, read no such list, or its answer was not used.
///
/// APPENDED AFTER <see cref="InstallerActivityAtScan"/>, so a positional construction of the
/// members above still means what it meant.
/// </param>
/// <param name="UnlistedKeysWithInstallProperties">
/// The product keys no installation in this scan's enumeration accounts for that have an
/// <c>InstallProperties</c> key (<see cref="InstallerQueryResult.UnlistedProductKeys"/>).
/// </param>
/// <param name="UnlistedKeysWithoutInstallProperties">The same keys without one.</param>
/// <param name="UnlistedKeyHolds">
/// The files the declared-product screen kept for those keys
/// (<see cref="Services.DeclaredProductScreening.UnlistedHolds"/>). None where the screen did not
/// run, kept nothing for one, or its answer was not used.
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
    long WithheldNotBeforeTheClockBytes = 0,
    int SupersededContainmentRefusedCount = 0,
    int SupersededContainmentUnestablishedCount = 0,
    IReadOnlyList<SourceRootGivenUp>? SourceRootsGivenUp = null,
    int SourceWaitCount = 0,
    CachedPackageCensus? CachedPackageCensus = null,
    DeclaredProductInstalledCauses? WithheldDeclaredProductInstalledCauses = null,
    Services.PendingRebootResult? InstallerActivityAtScan = null,
    Services.OwnPerUserSourceLists? OwnPerUserSourceLists = null,
    int UnlistedKeysWithInstallProperties = 0,
    int UnlistedKeysWithoutInstallProperties = 0,
    Services.UnlistedKeyHolds? UnlistedKeyHolds = null)
{
    /// <summary>Never null: a result built without them reads as a scan that kept nothing for a key.</summary>
    public Services.UnlistedKeyHolds UnlistedKeyHolds { get; init; }
        = UnlistedKeyHolds ?? Services.UnlistedKeyHolds.None;

    /// <summary>Never null: a result built without them reads as a scan that read no such list.</summary>
    public Services.OwnPerUserSourceLists OwnPerUserSourceLists { get; init; }
        = OwnPerUserSourceLists ?? Services.OwnPerUserSourceLists.None;

    /// <summary>Never null: a result built without the list reads as one that gave nothing up.</summary>
    public IReadOnlyList<SourceRootGivenUp> SourceRootsGivenUp { get; init; }
        = SourceRootsGivenUp ?? Array.Empty<SourceRootGivenUp>();

    /// <summary>Never null: a result built without them reads as a scan that kept no file for that reason.</summary>
    public DeclaredProductInstalledCauses WithheldDeclaredProductInstalledCauses { get; init; }
        = WithheldDeclaredProductInstalledCauses ?? DeclaredProductInstalledCauses.None;

    /// <summary>Never null: a result built without one reads as a scan that read no installation.</summary>
    public CachedPackageCensus CachedPackageCensus { get; init; }
        = CachedPackageCensus ?? CachedPackageCensus.None;

    /// <summary>
    /// The drives and shares in <see cref="SourceRootsGivenUp"/> at which the screen kept at
    /// least one file, in the order it gave them up. A host saying that the scan gave up a
    /// drive or share names these and no others: a root given up with no file kept at it
    /// changed nothing the scan decided.
    /// </summary>
    public IReadOnlyList<SourceRootGivenUp> SourceRootsGivenUpKeepingFiles =>
        SourceRootGivenUp.KeepingFiles(SourceRootsGivenUp);

    /// <summary>
    /// Whether <see cref="SourceRootsGivenUpKeepingFiles"/> names anything: the scan gave up
    /// a drive or share and kept at least one file because of it. Read off
    /// <see cref="SourceRootsGivenUp"/> directly, so asking builds no list.
    /// </summary>
    public bool HasSourceRootsGivenUpKeepingFiles => SourceRootsGivenUp.Any(root => root.KeptAnyFile);

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

    /// <summary>
    /// How many files the scan left alone: every registered file that is on the disk, and
    /// every file on <see cref="WithheldFiles"/>. It is the figure both hosts print as
    /// "N files left alone", the window under its unneeded count and the command line
    /// after its summary line, from this one expression, so the two cannot answer
    /// differently for one machine state.
    ///
    /// THE WITHHELD FILES ARE COUNTED HERE BECAUSE THEY ARE IN NEITHER OTHER POPULATION.
    /// A withheld file is neither offered nor a registered row, no registration naming
    /// it, so without this term the offer and this count would between them leave every
    /// withheld file out. The property is that the two take in every file a registration
    /// names and every file on the scan's candidate list; anything added to this count
    /// has to keep that.
    ///
    /// NO CAUSE TRAVELS WITH IT. What is true of every file counted is that the app left
    /// it alone, and why any particular one was left is not something this count says.
    ///
    /// A REGISTRATION WHOSE FILE IS GONE IS NOT COUNTED. A file that is not in the folder
    /// was not left alone, and there is nothing on the disk for a size beside it to
    /// measure. <see cref="RegisteredTotalBytes"/> is summed under the same existence
    /// test, so the count and the size describe one population.
    /// </summary>
    public int LeftAloneCount =>
        RegisteredPackages.Count(p => !p.IsMissingFromDisk) + (WithheldFiles?.Count ?? 0);

    /// <summary>
    /// The size of the files <see cref="LeftAloneCount"/> counts: the registered files on
    /// the disk and the withheld files, summed under the same two tests as the count, so
    /// the window's size beside its count describes one population.
    /// </summary>
    public long LeftAloneTotalBytes => RegisteredTotalBytes + WithheldTotalBytes;

    /// <summary>Total bytes of the files on <see cref="WithheldFiles"/>.</summary>
    public long WithheldTotalBytes =>
        WithheldFiles?.Sum(f => f.SizeBytes) ?? 0;

    /// <summary>
    /// Whether the scan found one of Windows Installer's in-progress signals set when it
    /// read them just after its clock (<see cref="InstallerActivityAtScan"/>). Where it
    /// did, every file from the folder walk the age check was handed was held back
    /// (<see cref="WithholdingSplit.ScanSignalCount"/>), and where that was at least one
    /// file both hosts give the signal's own reason in a line of its own
    /// (<see cref="Helpers.ScanSignalReport"/>).
    /// </summary>
    public bool InstallerActivityAtScanBlocked => InstallerActivityAtScan?.IsBlocked == true;

    /// <summary>
    /// How many withheld files the command line's held-back Application log entries
    /// count: every withheld file except those
    /// <see cref="WithholdingSplit.DeclaredProductInstalledCount"/>,
    /// <see cref="WithholdingSplit.NotBeforeTheClockCount"/> and
    /// <see cref="WithholdingSplit.DeclaredPatchRegisteredCount"/> count.
    ///
    /// IT IS THE LIST LESS THOSE THREE, NOT A SUM OF THE OTHERS, so a withheld file no
    /// arm counted, or one counted by an arm added to <see cref="WithholdingSplit"/>
    /// later, is counted here. A file the scan held because an in-progress signal was set
    /// is counted here too, and the command line prints that signal's reason beside it.
    /// </summary>
    public int UnestablishedWithheldCount =>
        Math.Max(0, (WithheldFiles?.Count ?? 0)
            - WithheldBy.DeclaredProductInstalledCount
            - WithheldBy.NotBeforeTheClockCount
            - WithheldBy.DeclaredPatchRegisteredCount);

    /// <summary>
    /// What this run's withholding amounts to, for the command line deciding what its
    /// held-back Application log entries say. The window's finished screen reads none of
    /// it: it chooses between the all-clear and the screen saying nothing was offered
    /// through <see cref="IsAllClear"/>.
    ///
    /// THE COMMAND LINE MAKES TWO DECISIONS AND NOT ONE: whether to write a held-back
    /// entry about a withholding at all, which <see cref="HasWithholdingToReport"/>
    /// answers, and which of the two sentences the machine has earned. It writes none for
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
    /// AND THE WHOLESALE READING NEEDS NOTHING WALK-DERIVED ON THE OFFER. The wholesale
    /// sentences say the scan offered nothing it found by looking in the folder, so a run
    /// offering a walk-derived file takes the per-file reading, whose sentences are true of
    /// every held file. An offered superseded patch comes from its own registration and
    /// not from the folder, so it does not count against the wholesale reading.
    ///
    /// A RUN WHOSE WITHHELD FILES WERE ALL COUNTED BY THE DECLARED-PRODUCT-INSTALLED AND
    /// DECLARED-PATCH-REGISTERED ARMS AND <see cref="WithholdingSplit.NotBeforeTheClockCount"/> READS AS
    /// <see cref="WithholdingAccount.KeptWithoutNotice"/>. The test is that those three
    /// account for the whole list, never that no other arm fired, so a file no arm
    /// counted, or one counted by an arm added later, keeps the run on the per-file
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

            if (WithheldBy.WholesaleCount == withheld
                && RemovableFiles.All(file => file.IsRemovablePatch))
                return WithholdingAccount.WholeWalkOffer;

            return WithheldBy.DeclaredProductInstalledCount
                + WithheldBy.NotBeforeTheClockCount
                + WithheldBy.DeclaredPatchRegisteredCount == withheld
                    ? WithholdingAccount.KeptWithoutNotice
                    : WithholdingAccount.PerFile;
        }
    }

    /// <summary>
    /// Whether the command line has a held-back Application log entry to write for this
    /// run's withholding: false for <see cref="WithholdingAccount.Nothing"/> and
    /// <see cref="WithholdingAccount.KeptWithoutNotice"/>, true for every other
    /// reading. Written as the two readings with no such sentence excluded, so a reading
    /// added to the enum is reported rather than silenced.
    /// </summary>
    public bool HasWithholdingToReport =>
        Withholding is not (WithholdingAccount.Nothing or WithholdingAccount.KeptWithoutNotice);

    /// <summary>
    /// How many files this scan held back without establishing that anything needs them:
    /// every file on <see cref="WithheldFiles"/> except those
    /// <see cref="WithholdingSplit.DeclaredProductInstalledCount"/> and
    /// <see cref="WithholdingSplit.DeclaredPatchRegisteredCount"/> count, together with
    /// the superseded files <see cref="SupersededHeldBackCount"/> counts. Above zero, it
    /// keeps the all-clear off the window's finished screen (<see cref="IsAllClear"/>).
    ///
    /// THE TWO ARMS LEFT OUT KEEP A FILE BECAUSE WINDOWS HOLDS A RECORD OF THE PROGRAM OR
    /// PATCH IT DECLARES, OR OF A PROGRAM THAT OPENS IT AS ITS PACKAGE. Every other file
    /// held back, and every superseded file <see cref="SupersededHeldBackCount"/> counts,
    /// was kept without the scan establishing that anything needs it: a file not shown to
    /// have last changed before a moment Windows Installer had nothing in progress, a file
    /// whose times were not established, a file the containment check refused or could not answer for, a
    /// superseded patch the scan held back, and a file kept on any other verdict alike.
    ///
    /// IT IS THE LIST LESS THOSE TWO ARMS, NOT A SUM OF THE OTHERS, so a walk-derived file
    /// no arm counted, or one counted by an arm added to <see cref="WithholdingSplit"/>
    /// later, is counted here.
    ///
    /// THE COMMAND LINE DOES NOT READ IT. Its Application log entries count
    /// <see cref="UnestablishedWithheldCount"/> and <see cref="SupersededHeldBackCount"/>
    /// separately, and the first of those leaves out the files
    /// <see cref="WithholdingSplit.NotBeforeTheClockCount"/> counts.
    /// </summary>
    public int UnsettledHeldBackCount =>
        Math.Max(0, (WithheldFiles?.Count ?? 0)
            - WithheldBy.DeclaredProductInstalledCount
            - WithheldBy.DeclaredPatchRegisteredCount)
        + SupersededHeldBackCount;

    /// <summary>
    /// How many superseded files this scan kept back from the offer: those
    /// <see cref="WithheldCount"/> counts, and those the containment check refused or could
    /// not answer for. The command line's Application log entry for them counts it, and
    /// <see cref="UnsettledHeldBackCount"/> takes it in, so a superseded file held back keeps
    /// the all-clear off the window's finished screen.
    ///
    /// IT IS A COUNT AND NEVER A CAUSE. Its three members are different findings, and what
    /// is true of all of them is that the app could not establish the file is one it can
    /// offer. The opt-in report carries the three apart and never this sum.
    /// </summary>
    public int SupersededHeldBackCount =>
        WithheldCount + SupersededContainmentRefusedCount + SupersededContainmentUnestablishedCount;

    /// <summary>
    /// How many superseded files this scan withheld on a recorded path it could not
    /// settle: rows carrying
    /// <see cref="RegisteredPackage.WithheldOnRecordedPathUnestablished"/> whose file is on
    /// disk. The opt-in report carries it as a count of its own.
    ///
    /// A SUB-COUNT OF <see cref="WithheldCount"/>, over the same on-disk test, so it never
    /// exceeds it. It counts every row the enumeration's scan-wide withholding took while
    /// that condition held, including a row the unaccounted-products condition withheld at
    /// the same time, so it answers how many files this condition holds back whatever else
    /// fired. Giving the other condition precedence would make it read nought on a machine
    /// meeting both. A row an earlier pass had already withheld is not in it, this
    /// condition having taken nothing from that row.
    /// </summary>
    public int SupersededRecordedPathUnestablishedCount =>
        RegisteredPackages.Count(p => p.WithheldOnRecordedPathUnestablished && p.FileExists);

    /// <summary>
    /// How many superseded files a product key no installation accounts for held back: rows
    /// carrying <see cref="RegisteredPackage.WithheldOnUnlistedKey"/> whose file is on disk. The
    /// opt-in report carries it as a count of its own.
    ///
    /// A SUB-COUNT OF <see cref="WithheldCount"/>, over the same on-disk test. It can overlap
    /// <see cref="SupersededScanWideWithheldCount"/>: a row withheld where a key's listing was
    /// not established carries both marks, and so does a row a key's listing names on a scan
    /// whose read of the registry's records failed. A row a key's listing names, on a scan with
    /// neither, carries this one alone, and a failed read alone sets only the scan-wide mark.
    /// A row an earlier pass had already withheld carries no unlisted-key mark, the listing
    /// having taken nothing from it.
    /// </summary>
    public int SupersededHeldForUnlistedKeyCount =>
        RegisteredPackages.Count(p => p.WithheldOnUnlistedKey && p.FileExists);

    /// <summary>
    /// How many superseded files a condition on the whole machine held back: rows carrying
    /// <see cref="RegisteredPackage.WithheldScanWide"/> whose file is on disk. The
    /// conditions are named there. The opt-in report carries it as a count of its own.
    ///
    /// A SUB-COUNT OF <see cref="WithheldCount"/>, over the same on-disk test. The rest of
    /// that count is superseded files withheld for a reason about that one file, a read about
    /// its patch that established nothing or a product key no installation accounts for whose
    /// patch listing names its patch, so the two together tell a hold on the whole machine
    /// from a hold on one file.
    /// <see cref="SupersededRecordedPathUnestablishedCount"/> is a sub-count of this one.
    /// </summary>
    public int SupersededScanWideWithheldCount =>
        RegisteredPackages.Count(p => p.WithheldScanWide && p.FileExists);

    /// <summary>
    /// Whether this scan held back a file <see cref="UnsettledHeldBackCount"/> counts. It is
    /// one of the two things that keep the all-clear off the window's finished screen
    /// (<see cref="IsAllClear"/>).
    /// </summary>
    public bool HasUnsettledHeldBack => UnsettledHeldBackCount > 0;

    /// <summary>
    /// Whether the window's finished screen, on a run that offered nothing, gives the
    /// all-clear: nothing held back that <see cref="UnsettledHeldBackCount"/> counts, and
    /// no drive or share given up with a file kept because of it
    /// (<see cref="HasSourceRootsGivenUpKeepingFiles"/>). Every other such run gets the
    /// screen saying nothing was offered. The command line prints its clean line on the
    /// same rule.
    ///
    /// A FILE KEPT AT A DRIVE OR SHARE GIVEN UP NEED NOT BE IN THAT COUNT. One whose own
    /// program's packages could not all be read is kept as declaring a program Windows
    /// still has installed, an arm the count leaves out, so this reads the drives and
    /// shares given up beside the count rather than the count alone.
    /// </summary>
    public bool IsAllClear => !HasUnsettledHeldBack && !HasSourceRootsGivenUpKeepingFiles;

    /// <summary>
    /// Whether this scan has a line to give about recently changed files
    /// (<see cref="Helpers.RecentChangeReport.Line(ScanResult)"/>): it held back at least
    /// one file whose times are not before its clock, with Windows Installer's in-progress
    /// signals clear when it read them. Such a file is one <see cref="UnsettledHeldBackCount"/>
    /// counts, so a scan with this line is never <see cref="IsAllClear"/>.
    /// </summary>
    public bool HasRecentChangeLine => WithheldBy.NotBeforeTheClockCount > 0;
}

/// <summary>
/// What a scan's withholding amounts to, in the terms the command line's Application log
/// entries speak it. The window's finished screen does not read it; it reads
/// <see cref="ScanResult.IsAllClear"/>.
///
/// THE COMMAND LINE WRITES NO HELD-BACK ENTRY FOR <see cref="Nothing"/> AND
/// <see cref="KeptWithoutNotice"/> AND FOR NOTHING ELSE, through
/// <see cref="ScanResult.HasWithholdingToReport"/>, and speaks the wholesale sentence for
/// <see cref="WholeWalkOffer"/> and the per-file one for every other member. A member
/// added later is therefore spoken of in the sentence true of every file, rather than
/// passed over.
///
/// IT IS A READING OF A RESULT AND NOT A DECISION OF ITS OWN. Nothing sets one of
/// these; <see cref="ScanResult.Withholding"/> derives it from the withheld list, the
/// split and the offer, so it cannot drift from any of them.
/// </summary>
public enum WithholdingAccount
{
    /// <summary>
    /// This scan kept nothing back from the folder walk, so the command line's walk
    /// entry has nothing to count.
    /// </summary>
    Nothing,

    /// <summary>
    /// Every file kept back was kept by the wholesale arm and nothing the folder walk
    /// found is on the offer, so the sentence naming what the scan could not establish
    /// about the machine's records is true of all of them.
    /// </summary>
    WholeWalkOffer,

    /// <summary>
    /// Files were kept back and neither the wholesale arm nor the three counts read as
    /// <see cref="KeptWithoutNotice"/> account for all of them, so the only sentence
    /// true of every file this reading counts is that the scan could not establish they
    /// were unneeded. A run that kept files back both ways reads as this, the wholesale
    /// sentence being false of the half it did not cover.
    ///
    /// THE COMMAND LINE'S ENTRY COUNTS
    /// <see cref="ScanResult.UnestablishedWithheldCount"/>, NOT THE WHOLE LIST, so a file
    /// any of those three counts is left out of it. A file the age check kept because its
    /// times were not established is in it, so is a file held because an in-progress
    /// signal was set, and so is a file the containment check refused or could not answer
    /// for.
    /// </summary>
    PerFile,

    /// <summary>
    /// Every file kept back was counted by the declared-product-installed arm,
    /// <see cref="WithholdingSplit.NotBeforeTheClockCount"/> or the declared-patch-registered
    /// arm. The first is a file that declares a program Windows still has installed, where
    /// at least one installation of that program opens a package, its cached copy or its
    /// original at a source, that the check could not show is a different file. The second
    /// is a file whose times were read and are not before the scan's clock, with Windows
    /// Installer's in-progress signals clear just after it, which every later scan judges
    /// afresh. The third is a patch copy whose declared patch Windows holds a registration
    /// of, where at least one registration records no cached copy the check could show is a
    /// different file. The command line writes no held-back entry for these files, as on
    /// a run that kept nothing back, and the files stay among those left alone; where any is
    /// of the second kind, it prints the line about those files
    /// (<see cref="Helpers.RecentChangeReport"/>). A file of the second kind is among those
    /// <see cref="ScanResult.UnsettledHeldBackCount"/> counts, so it keeps the all-clear off
    /// the window's finished screen, which gives the line about those files, and the clean
    /// line off the command line.
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
/// What kept each file on <see cref="ScanResult.WithheldFiles"/> back.
///
/// EXACTLY FIVE DECISIONS PUT A FILE ON THAT LIST AND THEY ARE MUTUALLY EXCLUSIVE
/// PER FILE, so this is a partition of it rather than overlapping views. The
/// containment check keeps a file the path comparison left unclaimed, before the
/// identity comparison sees it, counted in two arms by whether the check refused it or
/// could not answer; the identity comparison keeps one candidate at a time; the
/// wholesale arm keeps every remaining candidate in one go and the per-file screen and
/// age check are skipped entirely; the screen keeps a candidate on its own verdict,
/// counted in an arm per withholding verdict, except that the screen counts a candidate
/// whose own identity it could not read, or which had gone by the time it read it, in
/// the identity comparison's arm; and the age check keeps a candidate the screen let
/// through that it has not shown to have last changed before a moment Windows Installer
/// had nothing in progress, counted in three arms: every such candidate where a signal
/// was set just after the scan's clock, and otherwise by whether its times were
/// established. A file an earlier decision has already taken is off the list a later one
/// is handed, so nothing lands twice.
///
/// THE COUNTS ARE CARRIED APART BECAUSE THEY ARE READ APART. Each member is one fact
/// about one machine, and nothing may add any two of them and call the result a
/// cause: what is true of every file on the list is only that the scan declined to
/// offer it. The opt-in report carries every member, each under its own key, and a
/// member added here goes there too. Inside the app, the declared-product-installed,
/// not-before-the-clock and declared-patch-registered counts are read by
/// <see cref="ScanResult.UnestablishedWithheldCount"/> and
/// <see cref="ScanResult.Withholding"/>, and the first and last of those three by
/// <see cref="ScanResult.UnsettledHeldBackCount"/> as well. The wholesale count is read
/// by <see cref="ScanResult.Withholding"/>. Every other count is read by
/// <see cref="Total"/> alone.
///
/// <see cref="Total"/> IS WHAT HOLDS THE PARTITION HONEST, and it is asserted against
/// the list's own length rather than trusted. A partition is a partition until
/// somebody adds a branch, and an arm arriving later without a member here would appear
/// in none of these counts while the list grew underneath them.
/// </summary>
/// <param name="NotBeforeTheClockCount">
/// Candidates the age check kept back because their times were read on a local NTFS
/// volume and the latest of them is not before the scan's clock
/// (<see cref="Services.CachedFileAgeVerdict.NotBeforeTheClock"/>), every other decision
/// having let the file through and Windows Installer's in-progress signals having read
/// clear just after the clock. See <see cref="Services.CachedFileAge"/>.
///
/// APPENDED AFTER THE OTHER FIVE, so a positional construction of the first five still
/// means what it meant.
/// </param>
/// <param name="AgeUnestablishedCount">
/// Candidates the age check kept back because their times were not established: the
/// reader answered anything but <see cref="Services.FileTimesRead.Read"/>, so the
/// times could not be read, or were not read on a local, fixed NTFS volume; or the
/// latest of them is more than <see cref="Services.CachedFileAge.AheadLimit"/> after
/// the scan's own clock, which is not a time the scan can place.
/// </param>
/// <param name="DeclaredPatchRegisteredCount">
/// Patch copies the screen kept back because Windows holds a registration of the patch
/// each declares, and for at least one registration the screen could not show that the
/// cached copy it records is a different file. See
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
/// <param name="ContainmentRefusedCount">
/// Files from the folder walk that the containment check refused: the file is a reparse
/// point, or it resolves somewhere other than directly in the folder. The walk reads the
/// reparse-point attribute off each directory entry, and a file carrying it is counted
/// here without being put to <see cref="Services.CandidateGuard.CheckSafeToRemove"/>,
/// which would refuse it on the same attribute. See
/// <see cref="Services.CandidateGuard.RemovalSafety.Refused"/>.
///
/// APPENDED AFTER THE OTHER NINE, so a positional construction of the first nine still
/// means what it meant.
/// </param>
/// <param name="ContainmentUnestablishedCount">
/// Files from the folder walk that the containment check could not answer for: the
/// file's attributes, or the location it resolves to, would not read, or the Installer
/// folder's own location was not established, so where the file resolves could not be
/// compared with it. See <see cref="Services.CandidateGuard.RemovalSafety.Unproven"/>.
///
/// NOT TO BE ADDED TO THE ONE ABOVE. That one is a finding about the file; this is the
/// check failing to answer, and a total over the pair would say the files were reparse
/// points or lay elsewhere when some of them were never shown to be.
/// </param>
/// <param name="SecondCopyUnestablishedCount">
/// Installation packages the screen kept back because an installation the scan could
/// not rule out as a second copy of a program opens packages that could not all be
/// seen, or because an installation's cached package did not say which product it
/// declares, its own record did not show it to be an ordinary installation, and it is not
/// shown to have no package for Windows Installer to open, or because a product key no
/// installation accounts for left a second copy not ruled out. See
/// <see cref="Services.DeclaredProductOutcome.SecondCopyUnestablished"/>.
///
/// NOT TO BE ADDED TO <see cref="DeclaredProductUnestablishedCount"/>. That one is about
/// the product a file declares; this is about another installation on the machine, and
/// a total over the pair would state a cause true of neither.
///
/// APPENDED AFTER THE OTHER ELEVEN, so a positional construction of the first eleven
/// still means what it meant.
/// </param>
/// <param name="ScanSignalCount">
/// Candidates the age check kept back because one of Windows Installer's in-progress
/// signals was set when the scan read them just after its clock
/// (<see cref="ScanResult.InstallerActivityAtScan"/>), every other decision having let
/// the file through. Where a signal was set, every candidate reaching the age check is
/// counted here and no file's times are read, so on one scan this and
/// <see cref="NotBeforeTheClockCount"/> are never both above nought.
///
/// NOT TO BE ADDED TO <see cref="NotBeforeTheClockCount"/>. That one is a finding about
/// each file's times; this is a finding about the machine at one moment, made without
/// reading any file's times.
///
/// APPENDED AFTER THE OTHER TWELVE, so a positional construction of the first twelve
/// still means what it meant.
/// </param>
/// <param name="ClockSetBackCount">
/// Candidates the age check kept back because the system clock went back or lost time while
/// the scan read their times
/// (<see cref="Services.CachedFileAge.ClockWentBack(TimeProvider, Services.WallClockReading)"/>),
/// their own times having been before the scan's clock and every other decision having let
/// them through.
/// Where it did, every candidate reaching the age check is kept: the ones
/// whose times kept them are counted in <see cref="NotBeforeTheClockCount"/> and
/// <see cref="AgeUnestablishedCount"/>, and the rest here.
///
/// NOT TO BE ADDED TO <see cref="ScanSignalCount"/>. That one is a finding about Windows
/// Installer; this is a finding about the system clock, and a total over the pair would
/// say Windows Installer was busy when it was not. The two are never both above nought
/// on one scan: where a signal was set, no file's times are read.
///
/// APPENDED AFTER THE OTHER THIRTEEN, so a positional construction of the first thirteen
/// still means what it meant.
/// </param>
public readonly record struct WithholdingSplit(
    int IdentityUnestablishedCount = 0,
    int WholesaleCount = 0,
    int DeclaredProductInstalledCount = 0,
    int DeclaredProductUnestablishedCount = 0,
    int ScreenUnansweredCount = 0,
    int NotBeforeTheClockCount = 0,
    int AgeUnestablishedCount = 0,
    int DeclaredPatchRegisteredCount = 0,
    int DeclaredPatchUnestablishedCount = 0,
    int ContainmentRefusedCount = 0,
    int ContainmentUnestablishedCount = 0,
    int SecondCopyUnestablishedCount = 0,
    int ScanSignalCount = 0,
    int ClockSetBackCount = 0)
{
    /// <summary>
    /// Every file the members account for. It equals
    /// <see cref="ScanResult.WithheldFiles"/>'s own length on any scan that filled both,
    /// and a test holds it there.
    ///
    /// IT IS A COUNT AND NEVER A CAUSE. The members are different findings about a
    /// machine, so this figure answers "how many were held back" and nothing whatever
    /// about why.
    /// </summary>
    public int Total =>
        IdentityUnestablishedCount
        + WholesaleCount
        + DeclaredProductInstalledCount
        + DeclaredProductUnestablishedCount
        + ScreenUnansweredCount
        + NotBeforeTheClockCount
        + AgeUnestablishedCount
        + DeclaredPatchRegisteredCount
        + DeclaredPatchUnestablishedCount
        + ContainmentRefusedCount
        + ContainmentUnestablishedCount
        + SecondCopyUnestablishedCount
        + ScanSignalCount
        + ClockSetBackCount;
}

/// <summary>
/// The named conditions that keep the walk-derived offer back, one member per leg.
///
/// THEY ARE CONDITIONS THE RUN MET AND NOT A PARTITION OF ANY FILE SET. Any
/// combination of them can hold at once and none of them counts anything, so nothing
/// sums over these and no sentence built on one may state a cause for the files that
/// were withheld.
/// </summary>
public enum WithholdingLeg
{
    /// <summary>A path in Windows Installer's own records would not resolve.</summary>
    RecordedPathUnestablished,

    /// <summary>The identity of a file named in those records would not read.</summary>
    FileIdentityUnestablished,

    /// <summary>
    /// The registry names a product the scan could not ask about, so nothing shows it is
    /// not a second copy of a program, and nothing compares a file with it
    /// (<see cref="EnumerationCensus.RegistryProductUnaskable"/>).
    /// </summary>
    RegistryProductUnaskable,

    /// <summary>
    /// A read of Windows Installer's own records in the registry failed, so nothing shows
    /// what the key that would not read would have kept
    /// (<see cref="EnumerationCensus.RegistryRecordsUnread"/>).
    /// </summary>
    RegistryRecordsUnread,
}

/// <summary>
/// The one place the walk-offer withholding is decided, and the reason it is a type
/// rather than an expression written twice.
///
/// THE GATE AND THE LIST OF WHICH LEGS FIRED READ THE SAME CALL. The scan asks whether
/// anything fired through <see cref="Any"/>, which calls <see cref="Fired"/>. Written as
/// two expressions they agree until one of them changes, after which the gate can grow a
/// condition the list does not name. Here a leg added to the enum is a leg the gate acts
/// on and a leg the list names.
/// </summary>
public static class WithholdingLegs
{
    /// <summary>
    /// Every leg that fired, in declaration order.
    ///
    /// The census is asked about every leg but the file-identity one, and the registration
    /// side of the identity comparison about that one, each through the property that owns
    /// the question rather than by naming the census's or the tally's members here: a rule
    /// that named them itself would be one edit away from silently not acting on a member
    /// added later.
    /// </summary>
    public static IReadOnlyList<WithholdingLeg> Fired(
        EnumerationCensus census, FileIdentityReadTally registrationIdentityReads)
    {
        var fired = new List<WithholdingLeg>(4);

        if (census.AnyRecordedPathUnestablished)
            fired.Add(WithholdingLeg.RecordedPathUnestablished);
        if (registrationIdentityReads.AnyUnestablished)
            fired.Add(WithholdingLeg.FileIdentityUnestablished);
        if (census.RegistryProductUnaskable)
            fired.Add(WithholdingLeg.RegistryProductUnaskable);
        if (census.RegistryRecordsUnread)
            fired.Add(WithholdingLeg.RegistryRecordsUnread);

        return fired;
    }

    /// <summary>
    /// Whether the walk-derived offer is withheld wholesale, installation packages and
    /// patch files alike: any leg at all.
    ///
    /// It calls <see cref="Fired"/> rather than repeating its conditions, which is the
    /// whole point of the type. The list holds at most one entry per leg and is built once
    /// per scan.
    /// </summary>
    public static bool Any(
        EnumerationCensus census, FileIdentityReadTally registrationIdentityReads) =>
        Fired(census, registrationIdentityReads).Count > 0;
}
