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
    /// with them stopped being produced: a schema-3 report from this version
    /// on carries the same error categories as a Move and no <c>codes</c>
    /// field at all. Both are subtractions from an allowlisting receiver's
    /// point of view, which is why the version does not move for them; the
    /// version is the schema-4 work's to change, and a receiver that does not
    /// recognise a version stores the report under a lenient
    /// v&lt;n&gt;-unknown/ prefix rather than rejecting it, so a bump never
    /// loses data even if the allowlist has not caught up.
    /// </summary>
    public const int CurrentSchemaVersion = 3;

    public static ResultLogEntry ForScanOnly(ScanResult scan, long scanDurationMs, string pendingReboot) =>
        new(
            CurrentSchemaVersion,
            AppInfo.Current(),
            ResolveOs(),
            ScanInfo.From(scan, scanDurationMs, pendingReboot),
            OperationInfo.ScanOnly());

    public static ResultLogEntry ForMove(
        ScanResult scan,
        long scanDurationMs,
        string pendingReboot,
        MoveResult move,
        long bytesFreed,
        string moveDestinationKind) =>
        new(
            CurrentSchemaVersion,
            AppInfo.Current(),
            ResolveOs(),
            ScanInfo.From(scan, scanDurationMs, pendingReboot),
            OperationInfo.FromMove(move, bytesFreed, moveDestinationKind));

    public static ResultLogEntry ForDelete(
        ScanResult scan,
        long scanDurationMs,
        string pendingReboot,
        DeleteResult delete,
        long bytesFreed) =>
        new(
            CurrentSchemaVersion,
            AppInfo.Current(),
            ResolveOs(),
            ScanInfo.From(scan, scanDurationMs, pendingReboot),
            OperationInfo.FromDelete(delete, bytesFreed));

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

public sealed record AppInfo(string Version)
{
    public static AppInfo Current() =>
        new(Assembly.GetExecutingAssembly().GetName().Version?.ToString(3) ?? "0.0.0");
}

public sealed record ScanInfo(
    long DurationMs,
    int RegisteredCount,
    int OrphanedCount,
    int SupersededCount,
    int ObsoletedCount,
    int MissingFromDiskCount,
    string PendingReboot)
{
    public static ScanInfo From(ScanResult scan, long durationMs, string pendingReboot)
    {
        // IsRemovablePatch is the union of Superseded (2) and Obsoleted
        // (4); IsObsoleted is true only for PatchState=4. OrphanedCount
        // is the remainder after subtracting both.
        var obsoletedCount = scan.RemovableFiles.Count(f => f.IsObsoleted);
        var supersededCount = scan.RemovableFiles.Count(f => f.IsRemovablePatch) - obsoletedCount;
        return new(
            durationMs,
            scan.RegisteredPackages.Count,
            scan.RemovableFiles.Count - supersededCount - obsoletedCount,
            supersededCount,
            obsoletedCount,
            scan.MissingFromDiskCount,
            pendingReboot);
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
/// </summary>
public sealed record OperationInfo(
    string Kind,
    string Outcome,
    int FilesProcessed,
    int FilesFailed,
    long BytesFreed,
    IReadOnlyList<ErrorBucket> Errors,
    string? MoveDestinationKind)
{
    public static OperationInfo ScanOnly() =>
        new(OperationKinds.Scan, OperationOutcomes.NoFiles, 0, 0, 0, Array.Empty<ErrorBucket>(), null);

    public static OperationInfo FromMove(MoveResult result, long bytesFreed,
        string moveDestinationKind) =>
        new(
            OperationKinds.Move,
            ClassifyOutcome(result.MovedCount, result.Errors.Count),
            result.MovedCount,
            result.Errors.Count,
            bytesFreed,
            BucketErrors(result.Errors),
            moveDestinationKind);

    public static OperationInfo FromDelete(DeleteResult result, long bytesFreed) =>
        new(
            OperationKinds.Delete,
            ClassifyOutcome(result.DeletedCount, result.Errors.Count),
            result.DeletedCount,
            result.Errors.Count,
            bytesFreed,
            BucketErrors(result.Errors),
            null);

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

public static class PendingRebootLabels
{
    public const string Clean = "clean";
    public const string MsiExecuteMutexHeld = "msiExecuteMutexHeld";
    public const string InstallerInProgress = "installerInProgress";
    public const string PendingRenameInCache = "pendingRenameInCache";

    /// <summary>
    /// A Block whose reason has no label of its own, which is how a fourth
    /// <c>PendingRebootReason</c> would arrive. It exists so that state cannot
    /// be recorded as <see cref="Clean"/> and read straight past.
    /// </summary>
    public const string BlockedOther = "blockedOther";
}
