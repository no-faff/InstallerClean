using System.Reflection;
using System.Runtime.InteropServices;
using InstallerClean.Resources;
using InstallerClean.Services;

namespace InstallerClean.Models;

/// <summary>
/// User-shareable diagnostic record produced after every cleanup
/// operation (Move, Delete, or scan-with-no-orphans). Persisted as
/// <c>last-run.json</c> in <c>%LOCALAPPDATA%\NoFaff\InstallerClean</c>;
/// the user can view the file in their default JSON editor before
/// sharing, and the contents are exactly what gets POSTed if they
/// click "Send result log to No Faff".
///
/// Schema is intentionally narrow. Every field is either categorical
/// or a count; no file paths, no user names, no machine identifiers,
/// no time-of-day, nothing that could correlate two runs from the
/// same user.
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
        long bytesCleared,
        string moveDestinationKind,
        bool cancelled) =>
        new(
            CurrentSchemaVersion,
            AppInfo.Current(),
            ResolveOs(),
            ScanInfo.From(scan, scanDurationMs, pendingReboot),
            OperationInfo.FromMove(move, scan.RemovableFiles.Count, bytesCleared, moveDestinationKind, cancelled));

    public static ResultLogEntry ForDelete(
        ScanResult scan,
        long scanDurationMs,
        string pendingReboot,
        DeleteResult delete,
        long bytesCleared,
        bool cancelled) =>
        new(
            CurrentSchemaVersion,
            AppInfo.Current(),
            ResolveOs(),
            ScanInfo.From(scan, scanDurationMs, pendingReboot),
            OperationInfo.FromDelete(delete, scan.RemovableFiles.Count, bytesCleared, cancelled));

    private static string ResolveOs() =>
        $"{RuntimeInformation.OSDescription} ({RuntimeInformation.OSArchitecture})";
}

public sealed record AppInfo(string Version)
{
    public static AppInfo Current() =>
        new(Assembly.GetExecutingAssembly().GetName().Version?.ToString(3) ?? "0.0.0");
}

public sealed record ScanInfo(
    long DurationMs,
    int RegisteredCount,
    int RemovableCount,
    int OrphanedCount,
    int SupersededCount,
    int MissingFromDiskCount,
    string PendingReboot)
{
    public static ScanInfo From(ScanResult scan, long durationMs, string pendingReboot)
    {
        var supersededLabel = Strings.Reason_Superseded;
        var supersededCount = scan.RemovableFiles.Count(f => f.Reason == supersededLabel);
        return new(
            durationMs,
            scan.RegisteredPackages.Count,
            scan.RemovableFiles.Count,
            scan.RemovableFiles.Count - supersededCount,
            supersededCount,
            scan.MissingFromDiskCount,
            pendingReboot);
    }
}

/// <summary>
/// What the user did after the scan and how it landed.
/// <see cref="Kind"/> is <c>scan</c> when the user took no action
/// (the scan reported zero orphans, or the user dismissed without
/// running Move or Delete); <c>move</c> or <c>delete</c> otherwise.
/// <see cref="Outcome"/> is <c>complete</c> / <c>partial</c> /
/// <c>cancelled</c> / <c>failed</c> / <c>noFiles</c>.
/// <see cref="Errors"/> is the per-category count only (no paths,
/// no exception messages). <see cref="MoveDestinationKind"/> is
/// null when not a move; otherwise <c>sameDrive</c> /
/// <c>differentFixedDrive</c> / <c>removableDrive</c> /
/// <c>uncShare</c> / <c>unknown</c>.
/// </summary>
public sealed record OperationInfo(
    string Kind,
    string Outcome,
    int FilesProcessed,
    int FilesFailed,
    long BytesCleared,
    IReadOnlyList<ErrorBucket> Errors,
    string? MoveDestinationKind)
{
    public static OperationInfo ScanOnly() =>
        new(OperationKinds.Scan, OperationOutcomes.NoFiles, 0, 0, 0, Array.Empty<ErrorBucket>(), null);

    public static OperationInfo FromMove(MoveResult result, int totalCandidates, long bytesCleared,
        string moveDestinationKind, bool cancelled) =>
        new(
            OperationKinds.Move,
            ClassifyOutcome(result.MovedCount, result.Errors.Count, totalCandidates, cancelled),
            result.MovedCount,
            result.Errors.Count,
            bytesCleared,
            BucketErrors(result.Errors),
            moveDestinationKind);

    public static OperationInfo FromDelete(DeleteResult result, int totalCandidates, long bytesCleared,
        bool cancelled) =>
        new(
            OperationKinds.Delete,
            ClassifyOutcome(result.DeletedCount, result.Errors.Count, totalCandidates, cancelled),
            result.DeletedCount,
            result.Errors.Count,
            bytesCleared,
            BucketErrors(result.Errors),
            null);

    private static string ClassifyOutcome(int processed, int failed, int total, bool cancelled)
    {
        if (cancelled) return OperationOutcomes.Cancelled;
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
    public const string Cancelled = "cancelled";
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
