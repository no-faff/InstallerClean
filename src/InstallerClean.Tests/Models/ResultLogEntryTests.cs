using System.Text.Json;
using InstallerClean.Models;
using InstallerClean.Services;
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
    public void Schema_version_is_three()
    {
        // The receiving Edge Function field-validates per version; a
        // silent bump routes every record through its lenient
        // v<n>-unknown/ path. This pin makes a version change a
        // deliberate, reviewed act. Schema 3 carries the per-file
        // delete error code (the IFileOperation HRESULT) on each error
        // bucket and uses the IFileOperation-era category names
        // (RecycleFailed / PermanentlyDeleted) the IFileOperation delete engine
        // produces, where schema 2 had ShellRefused and no code.
        Assert.Equal(3, ResultLogEntry.CurrentSchemaVersion);
    }

    [Fact]
    public void Delete_error_bucket_carries_per_code_counts_for_RecycleFailed()
    {
        // One category bucket can hold files that failed with different
        // shell HRESULTs, so the bucket records a per-code histogram, not
        // a single code. Two files failed E_FAIL, one E_ACCESSDENIED.
        var errors = new List<FileOperationError>
        {
            new RecycleFailed(@"C:\Windows\Installer\a.msi", unchecked((int)0x80004005)),
            new RecycleFailed(@"C:\Windows\Installer\b.msi", unchecked((int)0x80004005)),
            new RecycleFailed(@"C:\Windows\Installer\c.msi", unchecked((int)0x80070005)),
        };

        var op = OperationInfo.FromDelete(new DeleteResult(0, errors), totalCandidates: 3, bytesFreed: 0);

        var bucket = Assert.Single(op.Errors);
        Assert.Equal("RecycleFailed", bucket.Category);
        Assert.Equal(3, bucket.Count);
        Assert.NotNull(bucket.Codes);
        Assert.Equal(2, bucket.Codes!["0x80004005"]);
        Assert.Equal(1, bucket.Codes!["0x80070005"]);
    }

    [Fact]
    public void Delete_error_bucket_carries_codes_for_PermanentlyDeleted()
    {
        // PermanentlyDeleted records the hrDelete that accompanied the
        // silent nuke. It is a success HRESULT (the shell reported the
        // delete succeeded while skipping the bin), distinct from
        // RecycleFailed's failure code, but it is carried for the same
        // diagnostic reason.
        var errors = new List<FileOperationError>
        {
            new PermanentlyDeleted(@"C:\Windows\Installer\a.msi", unchecked((int)0x00270008)),
            new PermanentlyDeleted(@"C:\Windows\Installer\b.msi", unchecked((int)0x00270008)),
        };

        var op = OperationInfo.FromDelete(new DeleteResult(0, errors), totalCandidates: 2, bytesFreed: 0);

        var bucket = Assert.Single(op.Errors);
        Assert.Equal("PermanentlyDeleted", bucket.Category);
        Assert.NotNull(bucket.Codes);
        Assert.Equal(2, bucket.Codes!["0x00270008"]);
    }

    [Fact]
    public void Error_bucket_without_a_shell_code_omits_the_codes_field()
    {
        // MissingSourceFile carries no HRESULT; its bucket leaves Codes
        // null and the field is omitted from the wire shape entirely
        // (not serialised as null), so a category that cannot carry a
        // code never emits an empty map.
        var errors = new List<FileOperationError> { new MissingSourceFile(@"C:\Windows\Installer\gone.msi") };

        var op = OperationInfo.FromDelete(new DeleteResult(0, errors), totalCandidates: 1, bytesFreed: 0);

        var bucket = Assert.Single(op.Errors);
        Assert.Null(bucket.Codes);

        var json = JsonSerializer.Serialize(op, JsonOptions);
        Assert.DoesNotContain("codes", json);
    }

    [Fact]
    public void Codes_serialise_as_a_camelCase_hex_keyed_map()
    {
        // Wire-format pin: the receiving Edge Function allowlists the
        // key name `codes` and validates each key against 0xNNNNNNNN.
        // The hex formatting matches how the same HRESULT renders on the
        // completion screen, so a code in a report and a code in a
        // screenshot read identically.
        var errors = new List<FileOperationError>
        {
            new RecycleFailed(@"C:\Windows\Installer\a.msi", unchecked((int)0x80004005)),
        };

        var op = OperationInfo.FromDelete(new DeleteResult(0, errors), totalCandidates: 1, bytesFreed: 0);

        var json = JsonSerializer.Serialize(op, JsonOptions);
        Assert.Contains("\"codes\"", json);
        Assert.Contains("\"0x80004005\"", json);
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
        // ScanInfo.From is culture-invariant (it doesn't read the
        // localised Reason string). PatchState=Superseded (2) sets
        // IsRemovablePatch only; PatchState=Obsoleted (4) sets both
        // flags; true orphans set neither.
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
        // Obsoleted entries (IsObsoleted=true) increment obsoletedCount
        // and not supersededCount; a scan with only obsoleted entries
        // produces supersededCount=0 and obsoletedCount=N.
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
