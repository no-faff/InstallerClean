namespace InstallerClean.Models;

/// <summary>
/// The output of a single <c>FileSystemScanService</c> run. The whole UI
/// state derives from this record: the orphan list, the registered list,
/// the size totals on the main screen, and the discrepancy banner are
/// all functions of these fields.
/// </summary>
/// <param name="RemovableFiles">
/// Files at the root of <c>C:\Windows\Installer</c> that no registration names,
/// judged first as text and then, for whatever the text left over, by asking the
/// filesystem which file each recorded path really names. Safe to move or delete.
///
/// ONE PATHWAY REACHES IT. A patch Windows reports superseded or obsoleted is
/// registered, so it never enters this list whatever its state: Windows opens
/// every patch registered to a product whether or not it has been superseded
/// (see <see cref="RegisteredPackage.IsMissingFromDisk"/> for the citation), so
/// the state is a label on the record rather than permission to remove the file.
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
/// is the third and is extinct.
/// </param>
/// <param name="RegisteredTotalBytes">
/// Sum of <see cref="RegisteredPackage.FileSizeBytes"/> across
/// <see cref="RegisteredPackages"/> where the file actually exists on
/// disk. Excludes <see cref="MissingFromDiskCount"/> entries so the
/// total never includes non-existent files.
/// </param>
/// <param name="MissingNotSupersededCount">
/// Registrations whose <c>LocalPackage</c> file is not on disk and which carry no
/// superseded or obsoleted state: a product's own cached package, an applied
/// patch, a patch whose state no read established, a path only the registry
/// fallback named.
///
/// IT STATES NO CAUSE AND NOTHING BUILT ON IT MAY EITHER. It used to say a
/// non-zero value means another tool removed files Windows still references. That
/// is one cause named for a set that can have several, and after 3.0.0 this
/// application is itself a candidate cause on any machine that ran v1.0.0 to
/// v2.3.0: those versions offered superseded patches, and deleting one leaves
/// exactly this record. What the number says is that Windows holds records naming
/// files that are not there.
/// </param>
/// <param name="MissingSupersededCount">
/// The same condition where the registration is a patch Windows reports
/// superseded (2) or obsoleted (4). Note the width: the report schema's
/// <c>supersededCount</c> is state 2 alone, and this is both states, because
/// nothing here turns on which of them a record carries.
///
/// COUNTED APART FROM ITS SIBLING AND SPOKEN WITH IT. The split is kept so the
/// data does not lose it and anyone reading a report can still see the shape of a
/// machine. It earns no sentence of its own: the two have the same consequence
/// (Windows opens every registered patch's file whether superseded or not, and a
/// missing one gives error 1635) and the same recovery step, and the only thing
/// that ever separated them is what removed the file, which no surface may speak
/// to. <see cref="RegisteredPackage.IsMissingFromDisk"/> carries the citations
/// and is the property both hosts read.
/// </param>
/// <param name="UnaccountedProductCount">
/// Installed products this scan did not account for, carried through from
/// <see cref="InstallerQueryResult.UnaccountedProductCount"/>, whose remarks are
/// the ones to read before quoting this: it is not confined to records that
/// failed to read, and it is an estimate rather than a headcount.
///
/// WHAT A NON-ZERO VALUE MEANS CHANGED IN 3.0.0 and the old meaning has gone. It
/// used to mean the scan had withheld every superseded-patch verdict, so the
/// offer was shorter than the machine would normally give. Nothing is withheld
/// now, because no verdict is granted to withhold. What survives is the other
/// half, and it is about the missing-files report rather than the offer: a
/// product whose records did not fully read is a product whose registrations this
/// scan may not have seen, so the count of records naming files that are not
/// there can be short. "No missing files" and "no missing files that could be
/// seen" are different claims and only the second is earned on such a run.
/// </param>
/// <param name="WithheldCount">
/// What withholding the removable class cost a run: superseded or obsoleted
/// packages whose file was on disk and which the scan would have offered, had it
/// been able to say that no installed product still needed them.
///
/// PERMANENTLY ZERO FROM 3.0.0, and kept rather than removed because the field is
/// in the result-log schema and a receiver reading a version it knows must not
/// meet a key that has vanished. Nothing grants a removable verdict, so nothing
/// takes one away. The command line's 3000 notice no longer carries it.
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
/// Kept files the records called superseded or obsoleted and a scan would not act
/// on, because it could not establish that no installed product still needed
/// them: every row carrying <c>RemovableWithheld</c>.
///
/// PERMANENTLY ZERO FROM 3.0.0. Nothing grants a removable verdict, so nothing
/// withholds one. Kept for the same reason <see cref="WithheldCount"/> is, and
/// still counted off the list rather than assumed, so a row that somehow acquired
/// the flag would show up rather than being silently folded into its neighbour.
/// </param>
/// <param name="RegisteredUnjudgedCount">
/// Kept files whose patch state no read established, one per path
/// (<c>VerdictUnreadable</c>). The question was put and the records did not
/// answer.
///
/// THE THREE PARTITION <see cref="RegisteredPackages"/> EXACTLY and a test holds
/// them to it, one of the three now being permanently empty. The two flags cannot
/// both be set on one row, so no file is counted twice and none falls between.
/// </param>
/// <param name="RegisteredSupersededCount">
/// Kept files whose registration is a patch Windows reports superseded (2) or
/// obsoleted (4), and whose file is on disk. The population 3.0.0 stopped
/// offering, measured where it sits: still in the folder, still registered, still
/// counted in the left-alone totals.
///
/// A SUB-COUNT AND NOT A PARTITION MEMBER. Nearly all of these rows are inside
/// <see cref="RegisteredClaimedCount"/>; one shape falls under
/// <see cref="RegisteredUnjudgedCount"/> instead, a patch whose State read gave 2
/// or 4 and whose Uninstallable read then failed, so the two must never be added.
/// Ones whose file has already gone are not here at all; they are in
/// <see cref="MissingSupersededCount"/>.
/// </param>
/// <param name="RegisteredSupersededBytes">
/// The same population's bytes. It is the figure nobody had: the field data
/// records superseded patches by count only, so how much space they occupy on a
/// real machine has only ever been estimated. Files on disk only, on the same
/// rule as <see cref="RegisteredTotalBytes"/>.
/// </param>
public record ScanResult(
    IReadOnlyList<OrphanedFile> RemovableFiles,
    IReadOnlyList<RegisteredPackage> RegisteredPackages,
    long RegisteredTotalBytes,
    int MissingNotSupersededCount = 0,
    int MissingSupersededCount = 0,
    int UnaccountedProductCount = 0,
    int WithheldCount = 0,
    EnumerationCensus Census = default,
    string ShortNameCreation = ShortNameCreationLabels.Unreadable,
    int RegisteredClaimedCount = 0,
    long RegisteredClaimedBytes = 0,
    int RegisteredWithheldCount = 0,
    int RegisteredUnjudgedCount = 0,
    int RegisteredSupersededCount = 0,
    long RegisteredSupersededBytes = 0)
{
    /// <summary>
    /// Every registration naming a file that is not on disk; the sum of the two
    /// sub-counts and the figure both hosts speak, the split beneath it being
    /// data rather than copy.
    /// </summary>
    public int MissingFromDiskCount => MissingNotSupersededCount + MissingSupersededCount;

    /// <summary>Total bytes of the files this scan is offering for removal.</summary>
    public long RemovableTotalBytes => RemovableFiles.Sum(f => f.SizeBytes);
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
    /// Setting 3: creation is off everywhere but the system volume, which is the
    /// volume <c>C:\Windows\Installer</c> is on.
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
