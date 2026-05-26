using System.Text.Json;
using InstallerClean.Models;
using Xunit;

namespace InstallerClean.Tests.Models;

/// <summary>
/// Wire-format pins for the result-log schema. The receiving Edge
/// Function depends on bytesFreed (not bytesCleared) and on the
/// three-atom orphanedCount + supersededCount + obsoletedCount triple
/// (not a combined removableCount); a silent rename here would land in
/// production unnoticed until the aggregator started returning zero
/// totals.
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
        ObsoletedCount: 0,
        MissingFromDiskCount: 0,
        PendingReboot: PendingRebootLabels.Clean);

    [Fact]
    public void Serialises_bytesFreed_not_bytesCleared()
    {
        var entry = new ResultLogEntry(
            SchemaVersion: ResultLogEntry.CurrentSchemaVersion,
            App: new AppInfo("1.8.0"), Os: "Windows 11 (X64)",
            Scan: SampleScan(), Operation: SampleOperation());

        var json = JsonSerializer.Serialize(entry, JsonOptions);

        Assert.Contains("\"bytesFreed\"", json);
        Assert.DoesNotContain("bytesCleared", json);
    }

    [Fact]
    public void Drops_removableCount_in_favour_of_three_atoms()
    {
        var entry = new ResultLogEntry(
            SchemaVersion: ResultLogEntry.CurrentSchemaVersion,
            App: new AppInfo("1.8.0"), Os: "Windows 11 (X64)",
            Scan: SampleScan(), Operation: SampleOperation());

        var json = JsonSerializer.Serialize(entry, JsonOptions);

        Assert.Contains("\"orphanedCount\"", json);
        Assert.Contains("\"supersededCount\"", json);
        Assert.Contains("\"obsoletedCount\"", json);
        Assert.DoesNotContain("removableCount", json);
    }

    [Fact]
    public void Schema_version_is_two()
    {
        // v1.8.2 bumped to schema 2 to split obsoletedCount out of
        // supersededCount. A silent bump in either direction would
        // route every record through the lenient v<n>-unknown/ path
        // on the receiving Edge Function.
        Assert.Equal(2, ResultLogEntry.CurrentSchemaVersion);
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
    public void ScanInfo_From_counts_orphaned_superseded_obsoleted_via_explicit_flags()
    {
        // IsRemovablePatch and IsObsoleted are stamped at scan time so
        // ScanInfo.From doesn't have to look at the localised Reason
        // string. PatchState=Superseded (2) sets IsRemovablePatch only;
        // PatchState=Obsoleted (4) sets both flags; true orphans set
        // neither.
        var files = new List<OrphanedFile>
        {
            new(@"C:\a.msi", 1024, false, IsRemovablePatch: false, IsObsoleted: false, "Orphaned"),
            new(@"C:\b.msi", 1024, false, IsRemovablePatch: false, IsObsoleted: false, "Orphaned"),
            new(@"C:\c.msp", 1024, true,  IsRemovablePatch: true,  IsObsoleted: false, "Superseded"),
            new(@"C:\d.msp", 1024, true,  IsRemovablePatch: true,  IsObsoleted: false, "Superseded"),
            new(@"C:\e.msp", 1024, true,  IsRemovablePatch: true,  IsObsoleted: false, "Superseded"),
            new(@"C:\f.msp", 1024, true,  IsRemovablePatch: true,  IsObsoleted: true,  "Obsoleted"),
        };
        var scan = new ScanResult(files, Array.Empty<RegisteredPackage>(), 0);

        var info = ScanInfo.From(scan, 500, PendingRebootLabels.Clean);

        Assert.Equal(2, info.OrphanedCount);
        Assert.Equal(3, info.SupersededCount);
        Assert.Equal(1, info.ObsoletedCount);
        Assert.Equal(500, info.DurationMs);
    }

    [Fact]
    public void ScanInfo_From_obsoleted_only_does_not_inflate_supersededCount()
    {
        // The pre-v1.8.2 implementation set IsSuperseded=true for
        // PatchState=Obsoleted (4) too, so SupersededCount lumped both.
        // This pins the v2 split: a scan with only obsoleted entries
        // produces supersededCount=0.
        var files = new List<OrphanedFile>
        {
            new(@"C:\a.msp", 2048, true, IsRemovablePatch: true, IsObsoleted: true, "Obsoleted"),
            new(@"C:\b.msp", 2048, true, IsRemovablePatch: true, IsObsoleted: true, "Obsoleted"),
        };
        var scan = new ScanResult(files, Array.Empty<RegisteredPackage>(), 0);

        var info = ScanInfo.From(scan, 200, PendingRebootLabels.Clean);

        Assert.Equal(0, info.OrphanedCount);
        Assert.Equal(0, info.SupersededCount);
        Assert.Equal(2, info.ObsoletedCount);
    }
}
