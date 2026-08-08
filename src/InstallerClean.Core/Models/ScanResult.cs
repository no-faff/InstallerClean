namespace InstallerClean.Models;

/// <summary>
/// The output of a single <c>FileSystemScanService</c> run. The whole UI
/// state derives from this record: the orphan list, the registered list,
/// the size totals on the main screen, and the discrepancy banner are
/// all functions of these fields.
/// </summary>
/// <param name="RemovableFiles">
/// Files in <c>C:\Windows\Installer</c> that the API does not claim plus
/// patches that the API marks superseded or obsoleted and whose file is
/// still on disk. Safe to move or delete. Superseded entries whose
/// underlying file is already gone count against
/// <see cref="MissingFromDiskCount"/> rather than appearing here, because
/// a Move or Delete would fail with MissingSourceFile.
/// </param>
/// <param name="RegisteredPackages">
/// <c>LocalPackage</c> paths the API still claims that aren't marked
/// superseded or obsoleted. Superseded patches go into
/// <see cref="RemovableFiles"/> instead. Drives the registered list
/// and the totals on the main screen. On a scan with a non-zero
/// <see cref="UnaccountedProductCount"/> the superseded patches are in here as
/// well, carrying <c>RemovableWithheld</c>: that scan is keeping them, so it
/// counts them among the files it is keeping.
/// </param>
/// <param name="RegisteredTotalBytes">
/// Sum of <see cref="RegisteredPackage.FileSizeBytes"/> across
/// <see cref="RegisteredPackages"/> where the file actually exists on
/// disk. Excludes <see cref="MissingFromDiskCount"/> entries so the
/// total never includes non-existent files.
/// </param>
/// <param name="MissingNonRemovableCount">
/// Packages the API still treats as in-use but whose <c>LocalPackage</c>
/// file is missing on disk. A non-zero value is the load-bearing signal
/// for the missing-from-disk banner: it means another tool removed
/// files Windows still references and a future install / uninstall /
/// patch will fail when it goes looking for them.
/// </param>
/// <param name="MissingRemovableCount">
/// Packages the API has marked superseded or obsoleted whose file is
/// already gone from disk. Benign: Windows considers these removable,
/// the file has already been removed, the entry is just leftover MSI
/// registration. Counted separately from
/// <see cref="MissingNonRemovableCount"/> so the banner only fires for
/// the actionable case. A superseded package whose verdict this scan withheld
/// counts here too: the file having gone is the same expected end state either
/// way, and only this scan's verdict was withheld.
/// </param>
/// <param name="UnaccountedProductCount">
/// Installed products this scan did not account for, carried through from
/// <see cref="InstallerQueryResult.UnaccountedProductCount"/>, whose remarks are
/// the ones to read before quoting this: it is not confined to records that
/// failed to read, and it is an estimate rather than a headcount. Non-zero means
/// the scan withheld every superseded-patch verdict, so
/// <see cref="RemovableFiles"/> carries orphans only; the summary says so. Zero
/// on any healthy machine, which is why nothing else keys off it.
/// </param>
/// <param name="WithheldCount">
/// What the withholding cost this run: superseded or obsoleted packages whose
/// file is on disk and which the scan would have offered, had it been able to
/// say that no installed product still needs them.
/// <see cref="UnaccountedProductCount"/> is the reason, this is the price, and
/// both are zero on a scan that read every product's records.
///
/// Two consumers. The command line reads it now, into the 3000 notice's
/// <c>Cli.EventLogScanWithheld</c> line, so an operator watching a fleet learns
/// what a withheld run cost and not merely that one happened. The schema 4
/// result-log payload takes it as well, where it sits beside the act-time
/// re-verify's own held-back count, a different number: this one is what never
/// reached the user, that one is what stopped qualifying between the list and
/// the button.
/// </param>
/// <param name="IdentityClaimedCount">
/// Candidates the identity pass kept back because a live registration answers to
/// what the file says it is. No registration named its PATH, so the path
/// comparison had nothing to go on; asking about the identity found something.
///
/// It is a positive claim on the file and nothing stronger. It does NOT establish
/// that a program would break without this particular copy: a product that caches
/// a fresh package on each of twenty updates leaves nineteen files that answer to
/// a live product code and are dead weight, and every one of them counts here.
/// Copy built on this figure has to say what it really means.
/// </param>
/// <param name="IdentityUnreadableCount">
/// Candidates kept back because the file did not yield an identity to ask about
/// at all: it would not open, it declares no code, or a patch names no product it
/// targets. An inability about the FILE.
/// </param>
/// <param name="IdentityUnaskableCount">
/// Candidates kept back because the identity was read and the question could not
/// be put to Windows. An inability about the RECORDS.
///
/// THE THREE ARE SEPARATE BECAUSE THEY ARE THREE DIFFERENT THINGS TO HAVE FOUND
/// OUT, and nothing may report them under one sentence. A confirmed claim, an
/// unreadable file and an unanswerable question have no honest superordinate: any
/// sentence covering all three either says nothing or says something false of two
/// of them. They are summed nowhere for the same reason.
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
public record ScanResult(
    IReadOnlyList<OrphanedFile> RemovableFiles,
    IReadOnlyList<RegisteredPackage> RegisteredPackages,
    long RegisteredTotalBytes,
    int MissingNonRemovableCount = 0,
    int MissingRemovableCount = 0,
    int UnaccountedProductCount = 0,
    int WithheldCount = 0,
    int IdentityClaimedCount = 0,
    int IdentityUnreadableCount = 0,
    int IdentityUnaskableCount = 0,
    EnumerationCensus Census = default,
    string ShortNameCreation = ShortNameCreationLabels.Unreadable)
{
    /// <summary>Total registered packages missing on disk; sum of the two sub-counts.</summary>
    public int MissingFromDiskCount => MissingNonRemovableCount + MissingRemovableCount;

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
