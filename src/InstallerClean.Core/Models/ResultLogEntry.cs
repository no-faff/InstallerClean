using System.Globalization;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text.Json.Serialization;
using InstallerClean.Services;

namespace InstallerClean.Models;

/// <summary>
/// Diagnostic record produced after every cleanup operation (Move,
/// Delete, or scan-with-no-orphans). Persisted as <c>last-run.json</c>
/// in <c>%LOCALAPPDATA%\NoFaff\InstallerClean</c>; the file's contents
/// are exactly what gets POSTed when the Send-result button is
/// confirmed.
///
/// Schema is intentionally narrow. Every field is either categorical
/// or a count; no file paths, no usernames, no machine identifiers,
/// no time-of-day, nothing that could correlate two runs from the
/// same machine.
/// </summary>
public sealed record ResultLogEntry(
    int SchemaVersion,
    AppInfo App,
    string Os,
    MachineInfo Machine,
    ScanInfo Scan,
    OperationInfo Operation)
{
    /// <summary>
    /// Schema 2 separates <see cref="ScanInfo.ObsoletedCount"/>
    /// (PatchState=4) from <see cref="ScanInfo.SupersededCount"/>
    /// (PatchState=2). Schema 1 envelopes lump both states under
    /// supersededCount; receivers must branch on this version before
    /// reading either field.
    ///
    /// Schema 3 added an optional per-code count map to each error bucket,
    /// carrying the shell HRESULTs behind two delete-only categories. Delete
    /// no longer goes through the shell, so those two categories and the map
    /// with them stopped being produced: a schema-3 report from that change
    /// on carries the same error categories as a Move and no <c>codes</c>
    /// field at all. Both are subtractions from an allowlisting receiver's
    /// point of view, which is why the version did not move for them.
    ///
    /// SCHEMA 4 IS THE FIRST BUMP THAT IS NOT A SHAPE CHANGE FOR ITS OWN SAKE.
    /// Every safety claim this app makes was measured on one machine, and one
    /// machine can falsify a universal and can never confirm one, so the payload
    /// now carries what varies BETWEEN machines: a <c>machine</c> object of shape
    /// facts, the three terms behind the withholding rather than the one number
    /// that mixes them, the identity pass's three outcomes, the act-time
    /// re-verify's five, and the byte totals a count-shaped question cannot
    /// answer. <c>pendingReboot</c> leaves, being structurally forced on any run
    /// that could act and unvarying across every report received.
    ///
    /// SIX OF THOSE KEYS WENT AGAIN IN 3.0.0 AND THE VERSION DID NOT MOVE, on the
    /// schema-3 precedent above: the identity pass's three scan-time outcomes, its
    /// instance-transform count, and two of the re-verify's five held-back causes
    /// all stopped being produced when the check that produced them was removed. Two
    /// arrived in the same release, under <c>machine</c>, counting the products
    /// installed as a second instance of themselves and the products that would not
    /// answer that question. A KEY THAT CEASES TO BE PRODUCED IS A SUBTRACTION; A
    /// KEY WHOSE MEANING CHANGES IS NOT, which is why the missing-files split was
    /// added beside its total rather than over it.
    ///
    /// A receiver that does not recognise a version stores the report under a
    /// lenient v&lt;n&gt;-unknown/ prefix rather than rejecting it, so a bump
    /// never loses data even if the allowlist has not caught up. THAT LENIENCE
    /// DOES NOT EXTEND TO THE TOP LEVEL: the receiver's top-level key allowlist
    /// runs for every version including the ones it cannot validate, so
    /// <c>machine</c> arriving before the receiving end knows the name is a
    /// rejected report and a user told sending failed. The receiver ships first.
    /// </summary>
    public const int CurrentSchemaVersion = 4;

    public static ResultLogEntry ForScanOnly(ScanResult scan, long scanDurationMs) =>
        new(
            CurrentSchemaVersion,
            AppInfo.Current(),
            ResolveOs(),
            MachineInfo.From(scan),
            ScanInfo.From(scan, scanDurationMs),
            OperationInfo.ScanOnly());

    public static ResultLogEntry ForMove(
        ScanResult scan,
        long scanDurationMs,
        MoveResult move,
        long bytesFreed,
        long operationDurationMs,
        string moveDestinationKind,
        HeldBackReasons heldBack) =>
        new(
            CurrentSchemaVersion,
            AppInfo.Current(),
            ResolveOs(),
            MachineInfo.From(scan),
            ScanInfo.From(scan, scanDurationMs),
            OperationInfo.FromMove(move, bytesFreed, operationDurationMs, moveDestinationKind, heldBack));

    public static ResultLogEntry ForDelete(
        ScanResult scan,
        long scanDurationMs,
        DeleteResult delete,
        long bytesFreed,
        long operationDurationMs,
        HeldBackReasons heldBack) =>
        new(
            CurrentSchemaVersion,
            AppInfo.Current(),
            ResolveOs(),
            MachineInfo.From(scan),
            ScanInfo.From(scan, scanDurationMs),
            OperationInfo.FromDelete(delete, bytesFreed, operationDurationMs, heldBack));

    private static string ResolveOs()
    {
        // Bucket to OS family and architecture only. The raw
        // RuntimeInformation.OSDescription string carries the Windows
        // build number (e.g. "10.0.26100"), which can narrow an
        // Insider-ring user to a population small enough to function
        // as a fingerprint. The schema's no-machine-identifier
        // contract requires a coarser shape.
        //
        // Build-number boundaries: Windows 11 starts at 22000
        // (released 2021-10-05). Earlier NT 10 builds are Windows 10.
        // The boundary holds for Windows 11 24H2 / build 26100 which
        // is also the Server 2025 build; the family label calls it
        // Windows 11 because the client population dominates and the
        // server population is a fraction of a percent of installs.
        var build = Environment.OSVersion.Version.Build;
        var family = build switch
        {
            >= 22000 => "Windows 11",
            >= 10000 => "Windows 10",
            > 0 => "Windows",
            _ => "Unknown",
        };
        return $"{family} ({RuntimeInformation.OSArchitecture})";
    }
}

/// <summary>
/// Which build produced the report, and which language its user was reading.
/// </summary>
/// <param name="Language">
/// The UI culture the app resolved for this run, as a plain BCP 47 tag. One of
/// sixteen values on any build that ships, so it cannot narrow anybody: it is
/// there because a report about a screen nobody can read in their own language
/// is a different report, and because which languages are actually used is not
/// otherwise knowable.
/// </param>
public sealed record AppInfo(string Version, string Language)
{
    public static AppInfo Current() =>
        new(Assembly.GetExecutingAssembly().GetName().Version?.ToString(3) ?? "0.0.0",
            // The UI culture rather than the format culture: this answers which
            // strings the user was shown. Invariant resolves to an empty name,
            // which is reported as it is rather than being filled in with a
            // plausible tag.
            CultureInfo.CurrentUICulture.Name is { Length: > 0 } name ? name : "invariant");
}

/// <summary>
/// What the machine is like, as opposed to what this run did. Every field is a
/// count or a fixed label, and none of them narrows a machine to a person: the
/// shape of a Windows Installer cache is not a fingerprint, and nothing here
/// records a path, a product, a name or a time.
///
/// IT IS A TOP-LEVEL OBJECT RATHER THAN MORE KEYS UNDER <c>scan</c> because these
/// answer the same on two consecutive scans of the same machine and the run
/// figures do not, and a reader that has to keep a list of which key is which has
/// been handed the wrong structure.
///
/// The reason any of it is collected: every claim this app makes about what is
/// safe to remove was measured on one machine, which has short-name creation off,
/// two patches and a cache of a few hundred files. None of those is known to be
/// ordinary and one machine cannot make them so.
/// </summary>
/// <param name="ShortNameCreation">
/// Where the machine still generates 8dot3 short names, one of
/// <see cref="ShortNameCreationLabels"/>.
/// </param>
/// <param name="LongFileNameCount">
/// Registered cached paths whose file name is longer than eight characters before
/// the extension, so the name itself cannot be an 8dot3 short name. Read against
/// <see cref="ScanInfo.RegisteredCount"/> in the same report, which is the
/// population it is drawn from.
///
/// The census calls the same number <c>LongLeafStemCount</c>, which is the precise
/// word for the part of a name before its extension. This one is what a person
/// reads off the confirmation dialog, and it pairs with
/// <see cref="ShortNameCreation"/> two lines above: short name against long name is
/// a pair anybody can follow without knowing what a stem is.
/// </param>
/// <param name="NonStringLocalPackageCount">
/// Registrations whose cached-path value was there and was not a string. Every
/// report answering zero is the evidence that reading it as one is safe; a single
/// report answering otherwise is the evidence that it is not, and one such report
/// is worth more than any number of the first kind.
/// </param>
/// <param name="UnreadablePatchStateCount">
/// Patches whose state could not be read during the scan, one per product-to-patch
/// pairing. It sizes a known wrong sentence rather than a lost file: both reads
/// fail towards keeping the file.
/// </param>
/// <param name="UnreadableVerdictPathCount">
/// The same failures counted once per cached PATH rather than once per pairing.
///
/// IT IS HERE RATHER THAN UNDER THE SCAN BECAUSE THE PAIR IS THE READING and
/// splitting them across two objects would throw that away: a machine where
/// several products' reads failed on one shared patch reports a high pairing count
/// against a single path, and a machine where the failures are spread reports the
/// two close together. Those are different faults wearing one number, and only the
/// two side by side tell them apart.
///
/// It counts REGISTRATIONS, not files on the disk: existence is not tested, unlike
/// the unclaimed-file counts under the scan. Nothing may read it as a quantity of
/// space or as files a user could go and look at.
/// </param>
/// <param name="UnparseableProductKeyCount">
/// Registry product key names that yielded no product code, so there was nothing
/// to ask Windows about. The registry says the machine has a product and nothing
/// can turn its name into a question.
///
/// A MACHINE FACT AND NOT A RUN OBSERVATION, which is what puts it here: it is
/// counted while walking every product key rather than only the ones a run
/// happened to miss, so two scans of one machine agree about it. Its sibling under
/// the scan, the unanswered count, is the opposite on both points.
/// </param>
/// <param name="ProductCount">Installed products the enumeration returned.</param>
/// <param name="RegistryProductKeyCount">
/// Installed products the REGISTRY holds, which is the only count of a machine's
/// products that does not come from the enumeration being measured. It sits here
/// beside <see cref="ProductCount"/> rather than under the scan because two scans
/// of one machine agree about it.
///
/// The pair is the whole of what makes a truncated enumeration visible from
/// outside, and the app's own rule for reading it absorbs a difference of two
/// outright plus a fifth proportionally. That band was set from the residue of
/// one machine. Sending both numbers raw is what lets anybody else's machine say
/// whether it is the right band.
/// </param>
/// <param name="PatchClaimCount">
/// Product-to-patch claims read, one per claim rather than per patch. With
/// <see cref="ProductCount"/> it gives the ratio that says how patch-heavy a real
/// machine is, which is the single thing the measured machine is least like.
/// </param>
/// <param name="InstanceProductCount">
/// Products installed as a second instance of themselves under an instance
/// transform. PRODUCTS, not files, and not a count of anything held back.
///
/// IT IS HERE BECAUSE THE APP HAS JUST STOPPED BEING ABLE TO TELL US. Until 3.0.0
/// this condition emptied the entire offer on any machine carrying one; that
/// withholding is gone, along with the check it protected, because nothing reads a
/// product code out of a cached file any more and the condition therefore picks out
/// no risk such a machine does not share with every other. What is left is a
/// question nobody anywhere has an answer to: whether a machine carrying such a
/// product exists in the field at all. Every measurement this project has of it is
/// from one machine, where it reads zero.
///
/// A machine fact rather than a run observation, which is what puts it in this
/// object: two scans of one machine agree about it.
/// </param>
/// <param name="InstanceTypeUnreadableCount">
/// Products whose <c>InstanceType</c> read failed, so they were neither counted
/// above nor shown to be ordinary.
///
/// IT TRAVELS SO THAT A ZERO ABOVE CANNOT BE READ AS "NO SUCH PRODUCT HERE". A
/// complete negative is a zero in both, and a zero above with a number here is a
/// machine that did not answer rather than a machine with none. The completeness of
/// the walk that asked is carried by the counts already in this object and under
/// the scan: a product the enumeration never reached was never asked this either.
/// </param>
/// <param name="SupersededRegistrationCount">
/// Registrations Windows reports superseded, counted off the machine rather than off
/// the offer. A machine fact: two scans of one machine agree about it, where the
/// scan object's <c>supersededCount</c> answers what a run OFFERED and moves with the
/// condition.
///
/// THE TWO DIFFERING IS THE MEASUREMENT NOBODY HAS. The difference is the size of the
/// class the per-product condition excludes, and no reading of one machine can
/// establish it.
/// </param>
/// <param name="ObsoletedRegistrationCount">
/// The same for obsoleted registrations, and for that class this is the only figure
/// that can ever be non-zero: they are not offered, so nothing derived from the offer
/// can see them. It exists to answer whether any machine anywhere has one, which no
/// report has ever shown and nobody has ever manufactured one to test with.
/// </param>
/// <param name="ProductPatchKeyCount">
/// Products whose registry patch-list key opened, from the listing the per-product
/// condition rests on. Against <see cref="ProductCount"/> it says how usual it is for
/// a product to carry one.
/// </param>
/// <param name="ProductPatchRegistrationCount">
/// Patch subkeys under those keys, one per (product, patch) registration. With the
/// count above it is the shape fact the measured machine is least like.
/// </param>
/// <param name="ProductsWithRemovablePatchCount">
/// Products where at least one registered patch positively declared itself removable,
/// so a rollback there could reach for a superseded patch's cached file. THE FIGURE
/// THAT SAYS WHAT THE CONDITION COSTS, and one machine cannot answer it.
/// </param>
/// <param name="ProductsWithPatchSetUnestablishedCount">
/// Products whose patch set could not be established. The other half of the same
/// question and kept apart from it: one is the condition finding a reason to withhold,
/// this is the condition unable to look.
/// </param>
/// <param name="PathResolverAttemptCount">
/// Recorded paths this scan put to the final-path resolver, which is asked only for a
/// value carrying a long-path or NT object prefix or an 8dot3 alias.
///
/// READ THE FIVE BELOW AGAINST IT OR NOT AT ALL. On a machine flagging no path the
/// resolver is never asked and all five read zero, which on the wire is identical to a
/// machine that asked and got five clean answers. This is the only thing separating
/// those two readings, and every one of the five is uninterpretable without it.
/// </param>
/// <param name="PathResolverNotAPathCount">
/// Of those, refused outright as not a path.
/// </param>
/// <param name="PathResolverNoAncestorCount">
/// Of those, with no existing component up to the root: an unattached drive, an
/// unmapped share, a detached virtual disk. An ordinary machine state.
/// </param>
/// <param name="PathResolverOpenRefusedCount">
/// Of those, where an ancestor existed and no handle could be opened on it, most often
/// an ACL. The second ordinary state, and the pair is why these are not one number: a
/// count folding all five together could not be acted on, and acting on such a count
/// was designed twice and withdrawn twice.
/// </param>
/// <param name="PathResolverNoFinalNameCount">
/// Of those, where an opened handle yielded an empty final name.
/// </param>
/// <param name="PathResolverFaultedCount">
/// Of those, where the attempt threw rather than answering. The resolved count is not
/// sent: it is the attempts less these five, and a stored copy could disagree with its
/// own parts.
/// </param>
/// <param name="PathNormalisationRefusedCount">
/// Recorded values this scan could not turn into a path at all, whatever refused them.
/// The sum of the three below, computed from them at the one place they are read, so
/// the total and its parts cannot come apart.
///
/// WHAT IT MEANS, and it is the figure this group exists for: such a claim is kept in
/// the raw spelling Windows gave, matches nothing the folder walk produces, and the
/// cached file it names is offered as unclaimed. It cannot be produced by a missing
/// file, a missing drive or a permission, which is what separates it from the two
/// ordinary states above.
///
/// NOTHING IN THE APPLICATION ACTS ON IT IN THIS RELEASE. It is here to size a failure
/// nobody has measured, which is the step two withdrawn designs skipped.
/// </param>
/// <param name="PathNormalisationRefusedAtExpansionCount">
/// Of those, refused while expanding an environment variable.
///
/// THE THREE ARE ONE POPULATION SPLIT BY CAUSE AND MUST NOT BE DESCRIBED AS ONE. A
/// sentence naming any single cause is false of the other two members; the only thing
/// true of every member is the superordinate above.
/// </param>
/// <param name="PathNormalisationRefusedAtPrefixStripCount">
/// Of those, refused while taking a prefix off or preparing the resolver's ask.
/// </param>
/// <param name="PathNormalisationRefusedAtFullPathCount">
/// Of those, refused by the full-path call: an embedded null, a device name, a length
/// past the API's limit. The member that fires in practice.
/// </param>
public sealed record MachineInfo(
    string ShortNameCreation,
    int LongFileNameCount,
    int NonStringLocalPackageCount,
    int UnreadablePatchStateCount,
    int UnreadableVerdictPathCount,
    int UnparseableProductKeyCount,
    int ProductCount,
    int RegistryProductKeyCount,
    int PatchClaimCount,
    int InstanceProductCount,
    int InstanceTypeUnreadableCount,
    int SupersededRegistrationCount,
    int ObsoletedRegistrationCount,
    int ProductPatchKeyCount,
    int ProductPatchRegistrationCount,
    int ProductsWithRemovablePatchCount,
    int ProductsWithPatchSetUnestablishedCount,
    int PathResolverAttemptCount,
    int PathResolverNotAPathCount,
    int PathResolverNoAncestorCount,
    int PathResolverOpenRefusedCount,
    int PathResolverNoFinalNameCount,
    int PathResolverFaultedCount,
    int PathNormalisationRefusedAtExpansionCount,
    int PathNormalisationRefusedAtPrefixStripCount,
    int PathNormalisationRefusedAtFullPathCount)
{
    public static MachineInfo From(ScanResult scan) =>
        new(
            scan.ShortNameCreation,
            scan.Census.LongLeafStemCount,
            scan.Census.NonStringLocalPackageValues,
            scan.Census.UnreadablePatchStates,
            scan.Census.UnreadableVerdictPaths,
            scan.Census.UnparseableProductKeyNames,
            scan.Census.ProductCount,
            scan.Census.RegistryProductKeys,
            scan.Census.PatchClaimCount,
            scan.Census.InstanceProductCount,
            scan.Census.InstanceTypeUnreadableCount,
            scan.SupersededRegistrationCount,
            scan.ObsoletedRegistrationCount,
            scan.Census.ProductPatchKeyCount,
            scan.Census.ProductPatchRegistrationCount,
            scan.Census.ProductsWithRemovablePatchCount,
            scan.Census.ProductsWithPatchSetUnestablishedCount,
            scan.Census.PathResolverAttemptCount,
            scan.Census.PathResolverNotAPathCount,
            scan.Census.PathResolverNoAncestorCount,
            scan.Census.PathResolverOpenRefusedCount,
            scan.Census.PathResolverNoFinalNameCount,
            scan.Census.PathResolverFaultedCount,
            scan.Census.PathNormalisationRefusedAtExpansionCount,
            scan.Census.PathNormalisationRefusedAtPrefixStripCount,
            scan.Census.PathNormalisationRefusedAtFullPathCount);

    /// <summary>
    /// Every recorded value this scan could not turn into a path, whatever refused
    /// it: the sum of the three above.
    ///
    /// DERIVED RATHER THAN PASSED IN, and that is the whole of why it is down here
    /// instead of among the parameters. As a parameter it could be constructed
    /// disagreeing with its own parts, and a total that contradicts its breakdown
    /// inside one object is the failure that no reader of the payload could
    /// possibly diagnose. It is serialised like any other property, so the receiver
    /// sees it as a key beside them.
    ///
    /// IT IS THE ONLY MEMBER OF THIS GROUP A SENTENCE MAY BE BUILT ON, the three
    /// parts being three different facts about a machine. What it means is that the
    /// recorded path could not be turned into a path at all, so the claim is kept in
    /// the raw spelling Windows gave, matches nothing the folder walk produces, and
    /// the cached file it names is offered as unclaimed.
    /// </summary>
    public int PathNormalisationRefusedCount =>
        PathNormalisationRefusedAtExpansionCount
        + PathNormalisationRefusedAtPrefixStripCount
        + PathNormalisationRefusedAtFullPathCount;
}

/// <summary>
/// What the scan found. Counts and byte totals only.
/// </summary>
/// <param name="RegisteredBytes">
/// Total size of the registered files that are really on disk, and
/// <paramref name="RemovableBytes"/> the same for the files being offered.
///
/// THESE TWO ARE THE STRONGEST FIELDS IN THE SCHEMA and the reason is worth
/// keeping: the question they answer is whether somebody can tell, before running
/// anything, that they probably have something to reclaim. Against the reports
/// received up to this release a COUNT-shaped threshold answered it backwards,
/// machines with the fewest registered files that did find something having freed
/// MORE than the larger ones. If the tell exists it is in bytes, and no report had
/// ever carried them.
/// </param>
/// <param name="MissingNeededCount">
/// The half of <paramref name="MissingFromDiskCount"/> whose registration carries
/// no superseded or obsoleted state. Added BESIDE the total rather than replacing
/// it: the total is read by the public chart with no version gate, and replacing
/// it would split a live series at this release. The other half falls out by
/// subtraction.
///
/// THE NAME IS NOW WRONG AND THE WIRE SHAPE IS HELD ANYWAY. It says "needed"
/// because the other half was read as benign, and that reading is what 3.0.0
/// removes: Windows opens every registered patch's cached file whether superseded
/// or not, so both halves are registrations naming a file that is not there and
/// neither is the lesser. The population also shifts slightly at this release,
/// having previously excluded any patch a scan called removable and now excluding
/// every superseded or obsoleted one. Renaming a key is a schema decision with a
/// receiver on the other end of it, so the key stays and this note is the record.
/// </param>
/// <param name="WithheldPatchCount">
/// Superseded files a scan would have offered and did not: it could not account for
/// every installed product, or some product sharing the patch holds one that could be
/// uninstalled and roll back onto the file, or that product's patch set could not be
/// established at all. A real figure again from 3.0.0, having been a literal zero
/// while nothing registered was offered. Obsoleted files are not in it: they are not
/// withheld, they are simply not offered, and they have their own count.
/// </param>
/// <param name="UnreadableProductCount">
/// Products whose records came back short. An exact per-product tally.
/// </param>
/// <param name="SkippedProductRowCount">
/// Enumeration rows that could not be read at all. A subset of
/// <paramref name="UnreadableProductCount"/>, sent because the two answer
/// different questions about one product: that a claim was lost, and that the row
/// itself never arrived.
/// </param>
/// <param name="UnclaimedProductFileCount">
/// Product registrations naming a cached file that is really on the disk and that
/// the enumeration never claimed.
/// </param>
/// <param name="RecoveredProductCount">
/// Products the registry named, this enumeration never returned, and a keyed ask
/// then found installed. THE TRUNCATION, MEASURED, where every other signal about
/// a short enumeration is an inference from two totals.
///
/// A non-zero reading anywhere would be the first evidence that a truncated
/// enumeration happens at all, which is a premise a whole mechanism once rested on
/// and which nothing has ever confirmed. It withholds nothing: the products behind
/// it were asked about rather than guessed at.
///
/// UNDER THE SCAN AND NOT THE MACHINE because it exists only where a run came back
/// short, so two scans of one machine need not agree about it.
/// </param>
/// <param name="UnansweredProductCount">
/// Products the registry named, this enumeration never returned, and Windows would
/// then not say were installed or not. A question that was put and got no answer,
/// which withholds, because nothing about an enumeration's completeness follows
/// from silence.
///
/// NOT THE MACHINE OBJECT'S UNPARSEABLE COUNT, and the two may never be added
/// together under one name outside the withholding total: Windows was never asked
/// about those, so a sentence about what Windows would not say is false of every
/// one of them. One figure carried both until this schema separated them.
/// </param>
/// <param name="UnclaimedPatchFileCount">
/// The same for patch registrations. A patch entry names no product, so it
/// establishes only that at least one product went unreached.
///
/// THESE ARE THE TALLIES, AND THE FIGURE THE APP DERIVES FROM THEM IS SENT
/// NOWHERE. That figure is a product estimate floored at one by patch evidence and
/// biased low by a deliberately generous subtraction, so it is a lower bound and a
/// field called a count would have asserted an exactness it has not got.
///
/// It is reproducible from these plus <paramref name="UnreadableProductCount"/>,
/// so nothing is lost by sending the tallies instead. The machine object's two
/// product headcounts are no longer inputs to anything the app derives: their
/// difference turned out unable to tell a truncated enumeration from ordinary
/// registry residue, and the products behind it are asked about by name instead.
/// They travel because how far a real machine's registry runs ahead of its
/// enumeration is a fact only these reports can establish.
/// </param>
public sealed record ScanInfo(
    long DurationMs,
    int RegisteredCount,
    long RegisteredBytes,
    int OrphanedCount,
    int SupersededCount,
    int ObsoletedCount,
    long RemovableBytes,
    int MissingFromDiskCount,
    int MissingNeededCount,
    int WithheldPatchCount,
    int UnreadableProductCount,
    int SkippedProductRowCount,
    int UnclaimedProductFileCount,
    int UnclaimedPatchFileCount,
    int RecoveredProductCount,
    int UnansweredProductCount)
{
    public static ScanInfo From(ScanResult scan, long durationMs)
    {
        // DERIVED FROM THE OFFER, WHICH IS WHAT THESE TWO KEYS HAVE ALWAYS MEANT, and
        // the obsoleted one is now structurally zero rather than incidentally so: an
        // obsoleted patch cannot reach the offer at all. It stays derived rather than
        // written as a literal, because the derivation is what would notice if one ever
        // did, where a hard-coded zero would report a clean shape over it.
        //
        // THE SCAN-TIME COUNTS ARE THE DIFFERENT QUESTION AND TRAVEL SEPARATELY. These
        // two answer what this run OFFERED; the machine object's registration counts
        // answer what the machine HAS. For obsoleted patches the second is the only one
        // that can ever be non-zero, which is the whole reason it was added.
        var obsoletedCount = scan.RemovableFiles.Count(f => f.IsObsoleted);
        var supersededCount = scan.RemovableFiles.Count(f => f.IsRemovablePatch) - obsoletedCount;
        return new(
            durationMs,
            scan.RegisteredPackages.Count,
            scan.RegisteredTotalBytes,
            scan.RemovableFiles.Count - supersededCount - obsoletedCount,
            supersededCount,
            obsoletedCount,
            scan.RemovableTotalBytes,
            scan.MissingFromDiskCount,
            // The wire shape is unchanged and the population behind this one has
            // moved a little; see the field's own note.
            scan.MissingNotSupersededCount,
            scan.WithheldCount,
            scan.Census.UnreadableProducts,
            scan.Census.SkippedProductRows,
            scan.Census.UnclaimedProductFiles,
            scan.Census.UnclaimedPatchFiles,
            scan.Census.RecoveredProductCount,
            scan.Census.UnansweredProductCount);
    }
}

/// <summary>
/// Operation taken after the scan and the outcome.
/// <see cref="Kind"/> is <c>scan</c> when no Move or Delete ran
/// (the scan reported zero orphans, or the completion overlay was
/// dismissed without Move/Delete); <c>move</c> or <c>delete</c>
/// otherwise. <see cref="Outcome"/> is <c>complete</c> /
/// <c>partial</c> / <c>failed</c> / <c>noFiles</c>. <see cref="Errors"/>
/// is the per-category count only (no paths, no exception messages).
/// <see cref="MoveDestinationKind"/> is null when not a move; otherwise
/// <c>sameDrive</c> / <c>differentFixedDrive</c> / <c>removableDrive</c>
/// / <c>uncShare</c> / <c>unknown</c>.
///
/// <see cref="DurationMs"/> is THIS operation's, and the payload also carries the
/// scan's own under <c>scan</c>. Two durations, and the one that has never been
/// reported is this one: whether a three-thousand-file delete is a pleasant thing
/// to sit through is not otherwise knowable. Zero on a scan-only run, where no
/// operation ran to time.
///
/// The five held-back counts are the act-time re-verify's, and they are NOT the
/// scan's withholding: this is what stopped qualifying between the list appearing
/// and the button being pressed, where <c>scan.withheldPatchCount</c> is what
/// never reached the list at all. They are five numbers rather than one because a
/// single batch can meet several causes and one cause named for the set would be
/// false of some of its members; they are not summed here for the same reason.
/// </summary>
public sealed record OperationInfo(
    string Kind,
    string Outcome,
    long DurationMs,
    int FilesProcessed,
    int FilesFailed,
    long BytesFreed,
    IReadOnlyList<ErrorBucket> Errors,
    string? MoveDestinationKind,
    int HeldBackReclaimed,
    int HeldBackRecordsChanged,
    int HeldBackRecordsUnreadable)
{
    public static OperationInfo ScanOnly() =>
        new(OperationKinds.Scan, OperationOutcomes.NoFiles, 0, 0, 0, 0,
            Array.Empty<ErrorBucket>(), null, 0, 0, 0);

    public static OperationInfo FromMove(MoveResult result, long bytesFreed, long durationMs,
        string moveDestinationKind, HeldBackReasons heldBack) =>
        new(
            OperationKinds.Move,
            ClassifyOutcome(result.MovedCount, result.Errors.Count),
            durationMs,
            result.MovedCount,
            result.Errors.Count,
            bytesFreed,
            BucketErrors(result.Errors),
            moveDestinationKind,
            heldBack.Reclaimed,
            heldBack.RecordsChanged,
            heldBack.RecordsUnreadable);

    public static OperationInfo FromDelete(DeleteResult result, long bytesFreed, long durationMs,
        HeldBackReasons heldBack) =>
        new(
            OperationKinds.Delete,
            ClassifyOutcome(result.DeletedCount, result.Errors.Count),
            durationMs,
            result.DeletedCount,
            result.Errors.Count,
            bytesFreed,
            BucketErrors(result.Errors),
            null,
            heldBack.Reclaimed,
            heldBack.RecordsChanged,
            heldBack.RecordsUnreadable);

    /// <summary>
    /// The outcome label, decided from the two counts the finished batch
    /// reports and nothing else. Deliberately the same rule as
    /// <see cref="Helpers.CliContract.ClassifyFileOperation"/>, which reaches
    /// the same three answers for the CLI's exit code: the two surfaces
    /// describe one operation and must agree about it.
    ///
    /// No candidate total is taken. Those two counts ARE the batch that was
    /// attempted, whereas any total handed in from outside describes an
    /// earlier moment, and the act-time re-verify sits between the two: it can
    /// hold a candidate back, so a scan-shaped total exceeds what was attempted
    /// and "everything failed" stops being expressible as failed == total. That
    /// is what silently retired the failed label once, and a rule that reads
    /// only its own batch cannot be broken again by a stage landing between the
    /// scan and the act.
    /// </summary>
    private static string ClassifyOutcome(int processed, int failed)
    {
        if (failed == 0) return OperationOutcomes.Complete;
        if (processed > 0) return OperationOutcomes.Partial;
        return OperationOutcomes.Failed;
    }

    private static IReadOnlyList<ErrorBucket> BucketErrors(IReadOnlyList<FileOperationError> errors)
    {
        if (errors.Count == 0) return Array.Empty<ErrorBucket>();
        return errors
            .GroupBy(e => e.GetType().Name)
            .Select(g => new ErrorBucket(g.Key, g.Count()))
            .OrderByDescending(b => b.Count)
            .ToList();
    }
}

/// <summary>
/// One error category in a result-log operation: the category name and how
/// many files fell into it, and nothing else. The category name is the error
/// record's type name, so it is a value the receiver allowlists.
/// </summary>
public sealed record ErrorBucket(string Category, int Count);

public static class OperationKinds
{
    public const string Scan = "scan";
    public const string Move = "move";
    public const string Delete = "delete";
}

public static class OperationOutcomes
{
    public const string Complete = "complete";
    public const string Partial = "partial";
    public const string Failed = "failed";
    public const string NoFiles = "noFiles";
}

public static class MoveDestinationKinds
{
    public const string SameDrive = "sameDrive";
    public const string DifferentFixedDrive = "differentFixedDrive";
    public const string RemovableDrive = "removableDrive";
    public const string UncShare = "uncShare";
    public const string Unknown = "unknown";
}

// PendingRebootLabels lived here and went with schema 4's pendingReboot field.
// It labelled a state for the payload alone, and the payload dropped the field
// because a move or a delete is GATED on that state and so can only ever report
// it clean, leaving a scan-only run as the sole place it could vary, where it
// never had. The banner keeps its own separate property and is untouched.
