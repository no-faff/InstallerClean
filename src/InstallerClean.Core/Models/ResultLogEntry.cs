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
/// Patches whose state could not be read during the scan. It sizes a known wrong
/// sentence rather than a lost file: both reads fail towards keeping the file.
/// </param>
/// <param name="ProductCount">Installed products the enumeration returned.</param>
/// <param name="PatchClaimCount">
/// Product-to-patch claims read, one per claim rather than per patch. With
/// <see cref="ProductCount"/> it gives the ratio that says how patch-heavy a real
/// machine is, which is the single thing the measured machine is least like.
/// </param>
public sealed record MachineInfo(
    string ShortNameCreation,
    int LongFileNameCount,
    int NonStringLocalPackageCount,
    int UnreadablePatchStateCount,
    int ProductCount,
    int PatchClaimCount)
{
    public static MachineInfo From(ScanResult scan) =>
        new(
            scan.ShortNameCreation,
            scan.Census.LongLeafStemCount,
            scan.Census.NonStringLocalPackageValues,
            scan.Census.UnreadablePatchStates,
            scan.Census.ProductCount,
            scan.Census.PatchClaimCount);
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
/// Registered files gone from disk that Windows still treats as needed, the half
/// of <paramref name="MissingFromDiskCount"/> that is a real problem on the
/// machine. Added BESIDE the total rather than replacing it: the total is read by
/// the public chart with no version gate, and replacing it would split a live
/// series at this release. The benign half falls out by subtraction.
/// </param>
/// <param name="WithheldPatchCount">
/// Superseded and obsoleted files this scan would have offered and did not,
/// because it could not account for every installed product. What the withholding
/// COST, where the three product terms below are its causes.
/// </param>
/// <param name="UnreadableProductCount">
/// Products whose records came back short. The only one of the three that is a
/// failure to read.
/// </param>
/// <param name="ShortfallProductCount">
/// Products the API's headcount fell short of the registry's own by, past the
/// tolerance band. An inference from two counts that can differ innocently.
/// </param>
/// <param name="UnlistedProductCount">
/// Products inferred from cached files that are on the disk, that the registry
/// claims, and that the API's own enumeration never named. An observation.
///
/// THE THREE GO SEPARATELY AND THE NUMBER THAT MIXES THEM GOES NOWHERE. Inside
/// the app they are combined as the first plus the larger of the other two,
/// because the last two estimate one quantity from opposite sides; that combined
/// figure is neither a count nor a bound, can run high as well as low, and a
/// single field carrying it would make every sentence built on it a sentence
/// about all three causes at once. Anything wanting the combined figure can
/// compute it from these and know what it has.
/// </param>
/// <param name="KeptIdentityClaimedCount">
/// Candidates the scan kept back because a live registration answers to the code
/// the FILE declares about itself. Weaker than it sounds and copy must not
/// strengthen it: one product that cached a fresh package on each of twenty
/// updates leaves nineteen files that answer to a live code and are needed by
/// nothing.
/// </param>
/// <param name="KeptIdentityUnreadableCount">
/// Kept back because the file yielded no identity to ask about. An inability
/// about the FILE.
/// </param>
/// <param name="KeptIdentityUnaskableCount">
/// Kept back because the identity was read and the question could not be put to
/// Windows. An inability about the RECORDS.
///
/// THE THREE ARE NEVER SUMMED, here or anywhere. A confirmed claim, an unreadable
/// file and an unanswerable question have no honest superordinate, and a total
/// would invite one.
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
    int ShortfallProductCount,
    int UnlistedProductCount,
    int KeptIdentityClaimedCount,
    int KeptIdentityUnreadableCount,
    int KeptIdentityUnaskableCount)
{
    public static ScanInfo From(ScanResult scan, long durationMs)
    {
        // IsRemovablePatch is the union of Superseded (2) and Obsoleted
        // (4); IsObsoleted is true only for PatchState=4. OrphanedCount
        // is the remainder after subtracting both.
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
            scan.MissingNonRemovableCount,
            scan.WithheldCount,
            scan.Census.UnreadableProducts,
            scan.Census.ShortfallProducts,
            scan.Census.ApiNeverClaimed,
            scan.IdentityClaimedCount,
            scan.IdentityUnreadableCount,
            scan.IdentityUnaskableCount);
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
    int HeldBackRecordsUnreadable,
    int HeldBackIdentityClaimed,
    int HeldBackIdentityUnreadable)
{
    public static OperationInfo ScanOnly() =>
        new(OperationKinds.Scan, OperationOutcomes.NoFiles, 0, 0, 0, 0,
            Array.Empty<ErrorBucket>(), null, 0, 0, 0, 0, 0);

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
            heldBack.RecordsUnreadable,
            heldBack.IdentityClaimed,
            heldBack.IdentityUnreadable);

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
            heldBack.RecordsUnreadable,
            heldBack.IdentityClaimed,
            heldBack.IdentityUnreadable);

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
