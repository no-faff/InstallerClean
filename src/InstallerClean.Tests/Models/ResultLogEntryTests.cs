using System.Text.Json;
using InstallerClean.Models;
using Xunit;

namespace InstallerClean.Tests.Models;

/// <summary>
/// Wire-format pins for the result-log schema. The receiving Edge
/// Function depends on bytesFreed (not bytesCleared) and on the
/// two-atom orphanedCount + supersededCount pair (not a combined
/// removableCount); a silent rename here would land in production
/// unnoticed until the aggregator started returning zero totals.
/// </summary>
public class ResultLogEntryTests
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    };

    private static OperationInfo SampleOperation() => new(
        Kind: OperationKinds.Move,
        Outcome: OperationOutcomes.Complete,
        FilesProcessed: 5,
        FilesFailed: 0,
        BytesFreed: 1024,
        Errors: Array.Empty<ErrorBucket>(),
        MoveDestinationKind: MoveDestinationKinds.SameDrive);

    private static ScanInfo SampleScan() => new(
        DurationMs: 100,
        RegisteredCount: 50,
        OrphanedCount: 3,
        SupersededCount: 2,
        MissingFromDiskCount: 0,
        PendingReboot: PendingRebootLabels.Clean);

    [Fact]
    public void Serialises_bytesFreed_not_bytesCleared()
    {
        var entry = new ResultLogEntry(
            SchemaVersion: 1, App: new AppInfo("1.8.0"), Os: "Windows 11 (X64)",
            Scan: SampleScan(), Operation: SampleOperation());

        var json = JsonSerializer.Serialize(entry, JsonOptions);

        Assert.Contains("\"bytesFreed\"", json);
        Assert.DoesNotContain("bytesCleared", json);
    }

    [Fact]
    public void Drops_removableCount_in_favour_of_two_atoms()
    {
        var entry = new ResultLogEntry(
            SchemaVersion: 1, App: new AppInfo("1.8.0"), Os: "Windows 11 (X64)",
            Scan: SampleScan(), Operation: SampleOperation());

        var json = JsonSerializer.Serialize(entry, JsonOptions);

        Assert.Contains("\"orphanedCount\"", json);
        Assert.Contains("\"supersededCount\"", json);
        Assert.DoesNotContain("removableCount", json);
    }

    [Fact]
    public void Schema_version_is_one()
    {
        // The Edge Function only field-validates schema 1; a silent
        // bump here would route every record through the lenient
        // v<n>-unknown/ path.
        Assert.Equal(1, ResultLogEntry.CurrentSchemaVersion);
    }

    [Fact]
    public void OperationInfo_ScanOnly_produces_noFiles_outcome()
    {
        var op = OperationInfo.ScanOnly();
        Assert.Equal(OperationKinds.Scan, op.Kind);
        Assert.Equal(OperationOutcomes.NoFiles, op.Outcome);
        Assert.Equal(0, op.FilesProcessed);
        Assert.Equal(0, op.FilesFailed);
        Assert.Equal(0, op.BytesFreed);
        Assert.Empty(op.Errors);
        Assert.Null(op.MoveDestinationKind);
    }

    [Fact]
    public void ScanInfo_From_counts_orphaned_and_superseded_via_explicit_flag()
    {
        // The previous implementation compared each OrphanedFile's
        // localised Reason string against Strings.Reason_Superseded.
        // The replacement uses the explicit IsSuperseded bool stamped
        // at scan time so the count is culture-invariant.
        var files = new List<OrphanedFile>
        {
            new(@"C:\a.msi", 1024, false, IsSuperseded: false, "Orphaned"),
            new(@"C:\b.msi", 1024, false, IsSuperseded: false, "Orphaned"),
            new(@"C:\c.msp", 1024, true,  IsSuperseded: true,  "Superseded"),
            new(@"C:\d.msp", 1024, true,  IsSuperseded: true,  "Superseded"),
            new(@"C:\e.msp", 1024, true,  IsSuperseded: true,  "Superseded"),
        };
        var scan = new ScanResult(files, Array.Empty<RegisteredPackage>(), 0);

        var info = ScanInfo.From(scan, 500, PendingRebootLabels.Clean);

        Assert.Equal(2, info.OrphanedCount);
        Assert.Equal(3, info.SupersededCount);
        Assert.Equal(500, info.DurationMs);
    }
}
