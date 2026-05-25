using System.Reflection;
using System.Runtime.InteropServices;
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
    public const int CurrentSchemaVersion = 1;

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
            OperationInfo.FromMove(move, scan.RemovableFiles.Count, bytesFreed, moveDestinationKind));

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
            OperationInfo.FromDelete(delete, scan.RemovableFiles.Count, bytesFreed));

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
    int MissingFromDiskCount,
    string PendingReboot)
{
    public static ScanInfo From(ScanResult scan, long durationMs, string pendingReboot)
    {
        var supersededCount = scan.RemovableFiles.Count(f => f.IsSuperseded);
        return new(
            durationMs,
            scan.RegisteredPackages.Count,
            scan.RemovableFiles.Count - supersededCount,
            supersededCount,
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

    public static OperationInfo FromMove(MoveResult result, int totalCandidates, long bytesFreed,
        string moveDestinationKind) =>
        new(
            OperationKinds.Move,
            ClassifyOutcome(result.MovedCount, result.Errors.Count, totalCandidates),
            result.MovedCount,
            result.Errors.Count,
            bytesFreed,
            BucketErrors(result.Errors),
            moveDestinationKind);

    public static OperationInfo FromDelete(DeleteResult result, int totalCandidates, long bytesFreed) =>
        new(
            OperationKinds.Delete,
            ClassifyOutcome(result.DeletedCount, result.Errors.Count, totalCandidates),
            result.DeletedCount,
            result.Errors.Count,
            bytesFreed,
            BucketErrors(result.Errors),
            null);

    private static string ClassifyOutcome(int processed, int failed, int total)
    {
        if (processed == 0 && failed == total && total > 0) return OperationOutcomes.Failed;
        if (failed == 0) return OperationOutcomes.Complete;
        return OperationOutcomes.Partial;
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
}
