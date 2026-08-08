using System.Text.Json;
using InstallerClean.Helpers;
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
///
/// That receiver allowlists every key at every object level, so a field this
/// side renames is not a mismatch anybody sees: the report is accepted, the key
/// is dropped, and the series simply stops. Hence the whole-payload pin below
/// rather than a test per interesting field.
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
        DurationMs: 900,
        FilesProcessed: 5,
        FilesFailed: 0,
        BytesFreed: 1024,
        Errors: Array.Empty<ErrorBucket>(),
        MoveDestinationKind: MoveDestinationKinds.SameDrive,
        HeldBackReclaimed: 0,
        HeldBackRecordsChanged: 0,
        HeldBackRecordsUnreadable: 0,
        HeldBackIdentityClaimed: 0,
        HeldBackIdentityUnreadable: 0);

    private static ScanInfo SampleScan() => new(
        DurationMs: 100,
        RegisteredCount: 50,
        RegisteredBytes: 5_000_000,
        OrphanedCount: 3,
        SupersededCount: 2,
        ObsoletedCount: 0,
        RemovableBytes: 300_000,
        MissingFromDiskCount: 0,
        MissingNeededCount: 0,
        WithheldPatchCount: 0,
        UnreadableProductCount: 0,
        SkippedProductRowCount: 0,
        UnclaimedProductFileCount: 0,
        UnclaimedPatchFileCount: 0,
        RecoveredProductCount: 0,
        UnresolvableProductCount: 0,
        KeptIdentityClaimedCount: 0,
        KeptIdentityUnreadableCount: 0,
        KeptIdentityUnaskableCount: 0);

    private static MachineInfo SampleMachine() => new(
        ShortNameCreation: ShortNameCreationLabels.NoVolumes,
        LongFileNameCount: 0,
        NonStringLocalPackageCount: 0,
        UnreadablePatchStateCount: 0,
        UnreadableVerdictPathCount: 0,
        ProductCount: 137,
        RegistryProductKeyCount: 137,
        PatchClaimCount: 2);

    private static ResultLogEntry SampleEntry() => new(
        SchemaVersion: ResultLogEntry.CurrentSchemaVersion,
        App: new AppInfo("1.8.0", "en-GB"),
        Os: "Windows 11 (X64)",
        Machine: SampleMachine(),
        Scan: SampleScan(),
        Operation: SampleOperation());

    [Fact]
    public void Serialises_bytesFreed_not_bytesCleared()
    {
        var json = JsonSerializer.Serialize(SampleEntry(), JsonOptions);

        Assert.Contains("\"bytesFreed\"", json);
        Assert.DoesNotContain("bytesCleared", json);
    }

    [Fact]
    public void Drops_removableCount_in_favour_of_three_atoms()
    {
        var json = JsonSerializer.Serialize(SampleEntry(), JsonOptions);

        Assert.Contains("\"orphanedCount\"", json);
        Assert.Contains("\"supersededCount\"", json);
        Assert.Contains("\"obsoletedCount\"", json);
        Assert.DoesNotContain("removableCount", json);
    }

    [Fact]
    public void Schema_version_is_four()
    {
        // The receiving Edge Function field-validates per version; a silent bump
        // routes every record through its lenient v<n>-unknown/ path. This pin
        // makes a version change a deliberate, reviewed act. It did not move when
        // Delete stopped going through the shell, which retired two delete-only
        // error categories and the per-code map that only they populated: an
        // allowlisting receiver sees both as subtractions, so neither needed a new
        // version to be understood.
        //
        // It moved to 4 for the population fields, which are additions, and for
        // pendingReboot leaving, which is not: a receiver that requires that field
        // has to be told which versions still carry it.
        Assert.Equal(4, ResultLogEntry.CurrentSchemaVersion);
    }

    [Fact]
    public void The_whole_payload_is_pinned_key_by_key()
    {
        // ONE TEST FOR THE WHOLE SHAPE, because the failure this guards against is
        // not a wrong value: it is a key that quietly stops being sent, which the
        // receiver accepts in silence and which no per-field assertion would ever
        // reach. Every key the receiver allowlists is named here, so adding a
        // field to the payload without adding it to the receiver fails here first.
        var json = JsonSerializer.Serialize(SampleEntry(), JsonOptions);
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        Assert.Equal(
            ["schemaVersion", "app", "os", "machine", "scan", "operation"],
            root.EnumerateObject().Select(p => p.Name));

        Assert.Equal(
            ["version", "language"],
            root.GetProperty("app").EnumerateObject().Select(p => p.Name));

        Assert.Equal(
            [
                "shortNameCreation", "longFileNameCount", "nonStringLocalPackageCount",
                "unreadablePatchStateCount", "unreadableVerdictPathCount",
                "productCount", "registryProductKeyCount", "patchClaimCount",
            ],
            root.GetProperty("machine").EnumerateObject().Select(p => p.Name));

        Assert.Equal(
            [
                "durationMs", "registeredCount", "registeredBytes", "orphanedCount",
                "supersededCount", "obsoletedCount", "removableBytes", "missingFromDiskCount",
                "missingNeededCount", "withheldPatchCount", "unreadableProductCount",
                "skippedProductRowCount", "unclaimedProductFileCount", "unclaimedPatchFileCount",
                "recoveredProductCount", "unresolvableProductCount",
                "keptIdentityClaimedCount", "keptIdentityUnreadableCount",
                "keptIdentityUnaskableCount",
            ],
            root.GetProperty("scan").EnumerateObject().Select(p => p.Name));

        Assert.Equal(
            [
                "kind", "outcome", "durationMs", "filesProcessed", "filesFailed", "bytesFreed",
                "errors", "moveDestinationKind", "heldBackReclaimed", "heldBackRecordsChanged",
                "heldBackRecordsUnreadable", "heldBackIdentityClaimed", "heldBackIdentityUnreadable",
            ],
            root.GetProperty("operation").EnumerateObject().Select(p => p.Name));
    }

    [Fact]
    public void The_payload_carries_no_pendingReboot_anywhere()
    {
        // It went with schema 4: a move or a delete is gated on that state and so
        // can only ever report it clean, and the one place it could vary never
        // had. Pinned as an absence because a re-add would otherwise be a silent
        // 400 from a receiver that no longer allowlists the key.
        var json = JsonSerializer.Serialize(SampleEntry(), JsonOptions);

        Assert.DoesNotContain("pendingReboot", json, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void The_two_kinds_of_holding_back_keep_different_names()
    {
        // The scan's withholding and the act-time re-verify's are different
        // numbers about different moments, and they sit in one payload. Neither
        // may be called just "held back", which is what this pins: getting it
        // wrong is a silent data fault rather than a compile error, because both
        // are ints and either would serialise happily under the other's name.
        var json = JsonSerializer.Serialize(SampleEntry(), JsonOptions);

        Assert.Contains("\"withheldPatchCount\"", json);
        Assert.Contains("\"heldBackReclaimed\"", json);
        Assert.DoesNotContain("\"heldBackCount\"", json);
        Assert.DoesNotContain("\"withheldCount\"", json);
    }

    [Fact]
    public void The_scan_duration_and_the_operation_duration_are_both_carried()
    {
        // Two durationMs keys in one payload, one per object, and the pair is the
        // point: the scan's has always been sent and the operation's never has.
        var json = JsonSerializer.Serialize(SampleEntry(), JsonOptions);
        using var doc = JsonDocument.Parse(json);

        Assert.Equal(100, doc.RootElement.GetProperty("scan").GetProperty("durationMs").GetInt64());
        Assert.Equal(900, doc.RootElement.GetProperty("operation").GetProperty("durationMs").GetInt64());
    }

    [Fact]
    public void The_withholding_arithmetic_travels_as_its_tallies_and_never_as_its_terms()
    {
        // The app derives a product estimate from these, floored at one and
        // biased low, which is not the count its name would claim and IS
        // reproducible from what is sent. So the tallies go and the derived term
        // goes nowhere. The registry and API headcounts travel for their own
        // sake rather than as its inputs: nothing is derived from the difference
        // between them any more, and a fleet's spread of that difference is a
        // fact about machines that no verdict of the app's carries.
        var scan = new ScanResult(
            Array.Empty<OrphanedFile>(), Array.Empty<RegisteredPackage>(), 0,
            UnaccountedProductCount: 9,
            Census: new EnumerationCensus(
                UnreadableProducts: 4, SkippedProductRows: 1,
                RegistryProductKeys: 40, UnclaimedProductFiles: 6, UnclaimedPatchFiles: 2,
                ProductCount: 30));

        var info = ScanInfo.From(scan, 10);
        var machine = MachineInfo.From(scan);

        Assert.Equal(4, info.UnreadableProductCount);
        Assert.Equal(1, info.SkippedProductRowCount);
        Assert.Equal(6, info.UnclaimedProductFileCount);
        Assert.Equal(2, info.UnclaimedPatchFileCount);
        Assert.Equal(30, machine.ProductCount);
        Assert.Equal(40, machine.RegistryProductKeyCount);

        // The derived figure reproduces from those six, which is the whole
        // argument for sending tallies: the never-claimed estimate net of the
        // unreadable products and floored at one because a patch file was seen
        // unclaimed.
        Assert.Equal(2, Math.Max(1, info.UnclaimedProductFileCount - info.UnreadableProductCount));

        var json = JsonSerializer.Serialize(info, JsonOptions);
        Assert.DoesNotContain("unaccounted", json, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("shortfall", json, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("unlisted", json, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void A_registry_ahead_of_the_enumeration_is_still_visible_in_the_payload()
    {
        // THE FIELD SET'S STRONGEST REASON, pinned so it cannot be quietly undone.
        // The app withholds nothing on a difference between these two totals, so
        // a machine whose registry holds two more products than the enumeration
        // returned reaches every verdict a machine with none does. Sending both
        // headcounts is what tells them apart, and how common that difference is
        // across real machines is a thing only the reports can answer.
        var absorbed = new ScanResult(
            Array.Empty<OrphanedFile>(), Array.Empty<RegisteredPackage>(), 0,
            Census: new EnumerationCensus(RegistryProductKeys: 139, ProductCount: 137));
        var clean = new ScanResult(
            Array.Empty<OrphanedFile>(), Array.Empty<RegisteredPackage>(), 0,
            Census: new EnumerationCensus(RegistryProductKeys: 137, ProductCount: 137));

        Assert.NotEqual(MachineInfo.From(clean), MachineInfo.From(absorbed));
        Assert.Equal(2,
            MachineInfo.From(absorbed).RegistryProductKeyCount - MachineInfo.From(absorbed).ProductCount);
    }

    [Fact]
    public void All_five_held_back_causes_reach_the_payload_and_are_not_summed()
    {
        // A batch can meet several causes at once, and one cause named for the set
        // would be false of some of its members. Five distinct values so a
        // transposition between two of them fails rather than cancelling out.
        var reasons = new HeldBackReasons(
            Reclaimed: 1, RecordsChanged: 2, RecordsUnreadable: 3,
            IdentityClaimed: 4, IdentityUnreadable: 5);

        var op = OperationInfo.FromDelete(
            new DeleteResult(0, Array.Empty<FileOperationError>()),
            bytesFreed: 0, durationMs: 0, heldBack: reasons);

        Assert.Equal(1, op.HeldBackReclaimed);
        Assert.Equal(2, op.HeldBackRecordsChanged);
        Assert.Equal(3, op.HeldBackRecordsUnreadable);
        Assert.Equal(4, op.HeldBackIdentityClaimed);
        Assert.Equal(5, op.HeldBackIdentityUnreadable);

        // The tally knows its own total and the payload deliberately does not
        // carry it: a total invites one sentence over five causes.
        Assert.Equal(15, reasons.Total);
        var json = JsonSerializer.Serialize(op, JsonOptions);
        Assert.DoesNotContain("heldBackTotal", json);
    }

    [Fact]
    public void The_machine_object_reports_the_scan_it_was_built_from()
    {
        var scan = new ScanResult(
            Array.Empty<OrphanedFile>(), Array.Empty<RegisteredPackage>(), 0,
            Census: new EnumerationCensus(
                NonStringLocalPackageValues: 1, UnreadablePatchStates: 2,
                ProductCount: 3, PatchClaimCount: 4, LongLeafStemCount: 5,
                RegistryProductKeys: 6),
            ShortNameCreation: ShortNameCreationLabels.PerVolume);

        var machine = MachineInfo.From(scan);

        Assert.Equal(ShortNameCreationLabels.PerVolume, machine.ShortNameCreation);
        Assert.Equal(5, machine.LongFileNameCount);
        Assert.Equal(1, machine.NonStringLocalPackageCount);
        Assert.Equal(2, machine.UnreadablePatchStateCount);
        Assert.Equal(3, machine.ProductCount);
        Assert.Equal(6, machine.RegistryProductKeyCount);
        Assert.Equal(4, machine.PatchClaimCount);
    }

    [Fact]
    public void A_scan_nobody_sampled_reports_the_short_name_policy_as_unreadable()
    {
        // The default matters: a ScanResult built without a probe must not read as
        // a machine whose policy is known, and every other label would say
        // something nobody measured.
        var scan = new ScanResult(Array.Empty<OrphanedFile>(), Array.Empty<RegisteredPackage>(), 0);

        Assert.Equal(ShortNameCreationLabels.Unreadable, MachineInfo.From(scan).ShortNameCreation);
    }

    [Fact]
    public void The_scan_object_carries_both_byte_totals()
    {
        var files = new List<OrphanedFile>
        {
            new(@"C:\a.msi", 1000, false, IsRemovablePatch: false, IsObsoleted: false, "Orphaned"),
            new(@"C:\b.msi", 2000, false, IsRemovablePatch: false, IsObsoleted: false, "Orphaned"),
        };
        var scan = new ScanResult(files, Array.Empty<RegisteredPackage>(), RegisteredTotalBytes: 9999);

        var info = ScanInfo.From(scan, 10);

        Assert.Equal(9999, info.RegisteredBytes);
        Assert.Equal(3000, info.RemovableBytes);
    }

    [Fact]
    public void The_needed_half_of_the_missing_count_is_added_beside_the_total_not_instead_of_it()
    {
        // The public chart reads missingFromDiskCount with no version gate, so
        // replacing it would split a live series at this release. Both are sent
        // and the benign half falls out by subtraction.
        var scan = new ScanResult(
            Array.Empty<OrphanedFile>(), Array.Empty<RegisteredPackage>(), 0,
            MissingNonRemovableCount: 2, MissingRemovableCount: 7);

        var info = ScanInfo.From(scan, 10);

        Assert.Equal(9, info.MissingFromDiskCount);
        Assert.Equal(2, info.MissingNeededCount);
    }

    [Fact]
    public void The_display_language_is_the_UI_culture_and_never_empty()
    {
        // Sixteen possible values on any shipped build, so it narrows nobody; it
        // is here because which languages are actually used is not otherwise
        // knowable. The invariant culture has an empty name and is reported as a
        // word rather than as a blank, which would read as a field that failed.
        var language = AppInfo.Current().Language;

        Assert.False(string.IsNullOrWhiteSpace(language));
    }

    [Fact]
    public void Move_error_bucket_categorises_a_held_open_file_as_FileInUse()
    {
        // The result log derives a bucket's category from the record's type
        // name, so splitting a held-open file out of IOFailure gives the
        // aggregate a new category for free. That is the whole cost of the
        // split as far as the log is concerned: the category stays a label with
        // no path or identifier in it. The name is therefore load-bearing, which
        // is what this pins.
        var errors = new List<FileOperationError>
        {
            new FileInUse(@"C:\Windows\Installer\a.msi"),
            new FileInUse(@"C:\Windows\Installer\b.msi"),
            new IOFailure(@"C:\Windows\Installer\c.msi"),
        };

        var op = OperationInfo.FromMove(new MoveResult(0, errors),
            bytesFreed: 0, durationMs: 0,
            moveDestinationKind: MoveDestinationKinds.DifferentFixedDrive,
            heldBack: default);

        var inUse = Assert.Single(op.Errors, b => b.Category == "FileInUse");
        Assert.Equal(2, inUse.Count);
        Assert.Single(op.Errors, b => b.Category == "IOFailure");
    }

    [Fact]
    public void An_error_bucket_is_a_category_and_a_count_and_nothing_else()
    {
        // The per-code map went with the shell delete: no category carries an
        // HRESULT any more, so no bucket emits one. The wire shape is pinned
        // here rather than assumed, because the receiving Edge Function
        // allowlists field names and a stray key is rejected at the door.
        var errors = new List<FileOperationError> { new MissingSourceFile(@"C:\Windows\Installer\gone.msi") };

        var op = OperationInfo.FromDelete(new DeleteResult(0, errors),
            bytesFreed: 0, durationMs: 0, heldBack: default);

        var bucket = Assert.Single(op.Errors);
        Assert.Equal("MissingSourceFile", bucket.Category);
        Assert.Equal(1, bucket.Count);

        var json = JsonSerializer.Serialize(op, JsonOptions);
        Assert.DoesNotContain("codes", json);
    }

    [Fact]
    public void OperationInfo_ScanOnly_produces_noFiles_outcome()
    {
        var op = OperationInfo.ScanOnly();
        Assert.Equal(OperationKinds.Scan, op.Kind);
        Assert.Equal(OperationOutcomes.NoFiles, op.Outcome);
        Assert.Equal(0, op.DurationMs);
        Assert.Equal(0, op.FilesProcessed);
        Assert.Equal(0, op.FilesFailed);
        Assert.Equal(0, op.BytesFreed);
        Assert.Empty(op.Errors);
        Assert.Null(op.MoveDestinationKind);

        // No operation ran, so nothing was held back by one. Zero here is a real
        // answer rather than an absent field, which is what keeps the receiver's
        // required-key check the same on all three run kinds.
        Assert.Equal(0, op.HeldBackReclaimed);
        Assert.Equal(0, op.HeldBackIdentityUnreadable);
    }

    [Fact]
    public void A_batch_whose_every_file_failed_is_failed_even_though_the_scan_offered_more()
    {
        // The act-time re-verify runs between the scan and the batch and can
        // hold candidates back, so the batch is a subset of what the scan
        // offered. Five files were offered, the re-verify kept three back, and
        // both survivors then failed: nothing was processed, so the operation
        // failed, and a rule measuring against the scan's five would call this
        // a partial success beside filesProcessed: 0.
        var errors = new List<FileOperationError>
        {
            new FileInUse(@"C:\Windows\Installer\a.msi"),
            new FileInUse(@"C:\Windows\Installer\b.msi"),
        };

        var op = OperationInfo.FromDelete(new DeleteResult(0, errors),
            bytesFreed: 0, durationMs: 0,
            heldBack: new HeldBackReasons(Reclaimed: 3));

        Assert.Equal(OperationOutcomes.Failed, op.Outcome);
        Assert.Equal(0, op.FilesProcessed);
        Assert.Equal(2, op.FilesFailed);
        Assert.Equal(3, op.HeldBackReclaimed);
    }

    [Fact]
    public void A_batch_that_reached_no_file_at_all_is_complete_not_failed()
    {
        // The all-dropped shape: the re-verify held every candidate back, so
        // nothing was attempted and nothing errored. Failure needs something to
        // have failed, and an operation that correctly declined to act on
        // anything is not one. (The GUI does not even write an entry for this
        // one, showing the held-back summary instead, but the classifier is
        // reached by the CLI and must not invent a failure from two zeroes.)
        var op = OperationInfo.FromMove(new MoveResult(0, Array.Empty<FileOperationError>()),
            bytesFreed: 0, durationMs: 0,
            moveDestinationKind: MoveDestinationKinds.SameDrive, heldBack: default);

        Assert.Equal(OperationOutcomes.Complete, op.Outcome);
        Assert.Equal(0, op.FilesProcessed);
        Assert.Equal(0, op.FilesFailed);
    }

    [Fact]
    public void A_batch_with_one_success_and_one_failure_is_partial()
    {
        var errors = new List<FileOperationError> { new FileInUse(@"C:\Windows\Installer\a.msi") };

        var op = OperationInfo.FromDelete(new DeleteResult(1, errors),
            bytesFreed: 1024, durationMs: 0, heldBack: default);

        Assert.Equal(OperationOutcomes.Partial, op.Outcome);
    }

    [Fact]
    public void The_result_log_and_the_CLI_label_the_same_batch_the_same_way()
    {
        // Two surfaces read one operation: the CLI's exit code and the result
        // log's outcome. They are separate rules by necessity (different return
        // types), so this walks the three answers over the same counts to keep
        // the pair honest.
        (int Processed, int Failed)[] batches = [(3, 0), (2, 1), (0, 2), (0, 0)];

        foreach (var (processed, failed) in batches)
        {
            var errors = Enumerable.Range(0, failed)
                .Select(i => (FileOperationError)new FileInUse($@"C:\Windows\Installer\{i}.msi"))
                .ToList();
            var outcome = OperationInfo.FromDelete(new DeleteResult(processed, errors),
                bytesFreed: 0, durationMs: 0, heldBack: default).Outcome;
            var cli = CliContract.ClassifyFileOperation(processed, failed);

            var expected = cli.ExitCode switch
            {
                CliExitCode.Ok => OperationOutcomes.Complete,
                CliExitCode.Partial => OperationOutcomes.Partial,
                _ => OperationOutcomes.Failed,
            };
            Assert.Equal(expected, outcome);
        }
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

        var info = ScanInfo.From(scan, 500);

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

        var info = ScanInfo.From(scan, 200);

        Assert.Equal(0, info.OrphanedCount);
        Assert.Equal(0, info.SupersededCount);
        Assert.Equal(2, info.ObsoletedCount);
    }
}
