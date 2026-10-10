using System.Globalization;
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
/// (not a combined removableCount).
///
/// That receiver allowlists every key at every object level and, from schema 4,
/// requires every count a version carries, so a field this side renames or
/// stops sending is a 400 for the whole report. Hence the whole-payload pin
/// below rather than a test per interesting field.
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
        HeldBackOwnershipUnestablished: 0,
        HeldBackFileNotConfirmed: 0,
        SourcesGivenUpStoppedCount: 0,
        SourcesGivenUpNoAnswerCount: 0,
        SourcesGivenUpSlowFailureCount: 0,
        SourcesGivenUpFailedReadsCount: 0,
        SourcesGivenUpReadTimeCount: 0,
        FilesKeptForSourcesGivenUpCount: 0,
        SourceWaitsShownCount: 0);

    /// <summary>A check made when Move or Delete is clicked that held back by <paramref name="reasons"/>.</summary>
    private static ReverifyResult Check(HeldBackReasons reasons = default) => new([], [], reasons);

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
        UnansweredProductCount: 0,
        WithheldCandidateCount: 0,
        WithheldTotalBytes: 0,
        RegisteredWithheldCount: 0,
        WithheldIdentityUnestablishedCount: 0,
        WithheldWholesaleCount: 0,
        WithheldDeclaredProductInstalledCount: 0,
        WithheldDeclaredProductUnestablishedCount: 0,
        WithheldScreenUnansweredCount: 0,
        WithheldUnderADayOldCount: 0,
        WithheldAgeUnestablishedCount: 0,
        WithheldDeclaredPatchRegisteredCount: 0,
        WithheldDeclaredPatchUnestablishedCount: 0,
        WithheldContainmentRefusedCount: 0,
        WithheldContainmentUnestablishedCount: 0,
        SupersededContainmentRefusedCount: 0,
        SupersededContainmentUnestablishedCount: 0,
        SupersededRecordedPathUnestablishedCount: 0,
        UnsettledEnumeratedProductCount: 0,
        RecoveredEnumeratedInstallationCount: 0,
        UnattributedPatchFileCount: 0,
        SupersededScanWideWithheldCount: 0,
        WithheldSecondCopyUnestablishedCount: 0,
        SourcesGivenUpStoppedCount: 0,
        SourcesGivenUpNoAnswerCount: 0,
        SourcesGivenUpSlowFailureCount: 0,
        SourcesGivenUpFailedReadsCount: 0,
        SourcesGivenUpReadTimeCount: 0,
        FilesKeptForSourcesGivenUpCount: 0,
        SourceWaitsShownCount: 0,
        SecondCopyListedCheckedCount: 0,
        SecondCopyKeepPathUnreadableCount: 0,
        SecondCopyKeepNoneRecordedCount: 0,
        SecondCopyKeepNotThereCount: 0,
        SecondCopyKeepWouldNotReadCount: 0,
        SecondCopyKeepNoProductCodeCount: 0,
        SecondCopyKeepAnotherAccountCount: 0,
        SecondCopyKeepPackageCodeUnansweredCount: 0,
        SecondCopyKeepInstanceTypeNotOrdinaryCount: 0,
        SecondCopyKeepPerMachineCount: 0,
        SecondCopyReleasedOrdinaryCount: 0,
        SecondCopyUnruledCheckedCount: 0,
        SecondCopyUnseenPathUnreadableCount: 0,
        SecondCopyUnseenNoneRecordedCount: 0,
        SecondCopyUnseenNotThereCount: 0,
        SecondCopyUnseenWouldNotIdentifyCount: 0,
        SecondCopyUnseenWouldNotReadCount: 0,
        SecondCopyUnseenNoProductCodeCount: 0,
        SecondCopyUnseenPerUserUnmanagedCount: 0,
        SecondCopyUnseenSourcesGivenUpCount: 0,
        SecondCopyUnseenSourceNotRuledOutCount: 0,
        SecondCopyUnseenPerMachineCount: 0,
        SecondCopyUnseenByNameFileCount: 0,
        SecondCopyReleasedOpensNoPackageCount: 0,
        SecondCopyReleasedBySourcesCount: 0,
        SecondCopyKeepNoneRecordedOtherAnswerCount: 0,
        SecondCopyKeepNoneRecordedRegistryDisagreesCount: 0,
        SecondCopyKeepNoneRecordedSourcesNotRuledOutCount: 0);

    private static MachineInfo SampleMachine() => new(
        ShortNameCreation: ShortNameCreationLabels.NoVolumes,
        LongFileNameCount: 0,
        NonStringLocalPackageCount: 0,
        UnreadablePatchStateCount: 0,
        UnreadableVerdictPathCount: 0,
        UnparseableProductKeyCount: 0,
        ProductCount: 137,
        RegistryProductKeyCount: 137,
        PatchClaimCount: 2,
        InstanceProductCount: 0,
        InstanceTypeUnreadableCount: 0,
        SupersededRegistrationCount: 0,
        ObsoletedRegistrationCount: 0,
        ProductPatchKeyCount: 0,
        ProductPatchRegistrationCount: 0,
        ProductsWithRemovablePatchCount: 0,
        ProductsWithPatchSetUnestablishedCount: 0,
        PathResolverAttemptCount: 0,
        PathResolverNotAPathCount: 0,
        PathResolverNoAncestorCount: 0,
        PathResolverOpenRefusedCount: 0,
        PathResolverNoFinalNameCount: 0,
        PathResolverFaultedCount: 0,
        PathNormalisationRefusedAtExpansionCount: 0,
        PathNormalisationRefusedAtPrefixStripCount: 0,
        PathNormalisationRefusedAtFullPathCount: 0,
        PathNormalisationRefusedAtEmbeddedNullCount: 0,
        PathFlaggedSpellingCount: 0,
        RegistrationIdentityAttemptCount: 0,
        RegistrationIdentityNamesNothingCount: 0,
        RegistrationIdentityNotAPathCount: 0,
        RegistrationIdentityOpenRefusedCount: 0,
        RegistrationIdentityUnavailableCount: 0,
        RegistrationIdentityFaultedCount: 0,
        CandidateIdentityAttemptCount: 0,
        CandidateIdentityNamesNothingCount: 0,
        CandidateIdentityNotAPathCount: 0,
        CandidateIdentityOpenRefusedCount: 0,
        CandidateIdentityUnavailableCount: 0,
        CandidateIdentityFaultedCount: 0,
        RegistryKeyReadFailureCount: 0);

    private static ResultLogEntry SampleEntry() => new(
        SchemaVersion: ResultLogEntry.CurrentSchemaVersion,
        App: new AppInfo("1.8.0", "en-GB", "en", "GB"),
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
    public void Schema_version_is_eight()
    {
        // The receiving Edge Function field-validates per version and holds each version a
        // release sends to its exact set of keys, every count in it required; a version it
        // does not know goes to its lenient v<n>-unknown/ path. So a key added to or taken
        // from what a version carries moves the version once a release sends it, and this
        // pin makes that move a deliberate, reviewed act. Schema 8 is schema 7 with four keys
        // appended under scan, from secondCopyReleasedBySourcesCount.
        Assert.Equal(8, ResultLogEntry.CurrentSchemaVersion);
    }

    [Fact]
    public void The_whole_payload_is_pinned_key_by_key()
    {
        // ONE TEST FOR THE WHOLE SHAPE, because the failure this guards against is
        // not a wrong value: it is a key added, renamed or no longer sent, which no
        // per-field assertion would ever reach and which the receiver answers with a
        // 400 for the whole report. Every key the receiver allowlists is named here,
        // so adding a field to the payload without adding it to the receiver fails
        // here first.
        var json = JsonSerializer.Serialize(SampleEntry(), JsonOptions);
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        Assert.Equal(
            ["schemaVersion", "app", "os", "machine", "scan", "operation"],
            root.EnumerateObject().Select(p => p.Name));

        Assert.Equal(
            ["version", "language", "windowsLanguage", "windowsRegion"],
            root.GetProperty("app").EnumerateObject().Select(p => p.Name));

        Assert.Equal(
            [
                "shortNameCreation", "longFileNameCount", "nonStringLocalPackageCount",
                "unreadablePatchStateCount", "unreadableVerdictPathCount",
                "unparseableProductKeyCount", "productCount", "registryProductKeyCount",
                "patchClaimCount", "instanceProductCount", "instanceTypeUnreadableCount",
                "supersededRegistrationCount", "obsoletedRegistrationCount",
                "productPatchKeyCount", "productPatchRegistrationCount",
                "productsWithRemovablePatchCount", "productsWithPatchSetUnestablishedCount",
                "pathResolverAttemptCount", "pathResolverNotAPathCount",
                "pathResolverNoAncestorCount", "pathResolverOpenRefusedCount",
                "pathResolverNoFinalNameCount", "pathResolverFaultedCount",
                "pathNormalisationRefusedAtExpansionCount",
                "pathNormalisationRefusedAtPrefixStripCount",
                "pathNormalisationRefusedAtFullPathCount",
                // After the other three although its refusal happens before all of
                // them. The four are positional int parameters, so a member inserted
                // among them would re-point every argument after it and the build
                // would not notice; appending is the arrangement that cannot.
                "pathNormalisationRefusedAtEmbeddedNullCount",
                // What a recorded value LOOKED like rather than what happened to it,
                // and the only member of that group which is not an outcome.
                "pathFlaggedSpellingCount",
                // The identity comparison, one group per side. The two sides are
                // asked the same five questions and the answers are acted on
                // differently, which is in MachineInfo's own notes.
                "registrationIdentityAttemptCount", "registrationIdentityNamesNothingCount",
                "registrationIdentityNotAPathCount", "registrationIdentityOpenRefusedCount",
                "registrationIdentityUnavailableCount", "registrationIdentityFaultedCount",
                "candidateIdentityAttemptCount", "candidateIdentityNamesNothingCount",
                "candidateIdentityNotAPathCount", "candidateIdentityOpenRefusedCount",
                "candidateIdentityUnavailableCount", "candidateIdentityFaultedCount",
                // The registry side's failed reads, a figure of which
                // nonStringLocalPackageCount near the top of this list is a part.
                "registryKeyReadFailureCount",
                // THE DERIVED TOTALS COME LAST AS A BLOCK, not beside their parts:
                // each is a property rather than a constructor parameter, so a total
                // contradicting its own breakdown inside one object is impossible
                // rather than merely unlikely. The serialiser emits the positional
                // members first and these in declaration order.
                //
                // A derived total reaches the payload like any other key, so a new
                // one is named here too: a key the receiver has not allowlisted is a
                // 400 for the whole report rather than one dropped field.
                "pathNormalisationRefusedCount", "pathResolverRefusedCount",
                "registrationIdentityRefusedCount", "candidateIdentityRefusedCount",
            ],
            root.GetProperty("machine").EnumerateObject().Select(p => p.Name));

        Assert.Equal(
            [
                "durationMs", "registeredCount", "registeredBytes", "orphanedCount",
                "supersededCount", "obsoletedCount", "removableBytes", "missingFromDiskCount",
                "missingNeededCount", "withheldPatchCount", "unreadableProductCount",
                "skippedProductRowCount", "unclaimedProductFileCount", "unclaimedPatchFileCount",
                "recoveredProductCount", "unansweredProductCount",
                // Appended, and it is a different population from withheldPatchCount
                // three keys above it: files the walk found and the scan declined to
                // offer, where that one counts superseded registrations.
                "withheldCandidateCount",
                // The size of the population the key above counts, so that a report
                // can say what the withholding cost and not only how many files it
                // was, and the third withheld population, which is registered rows
                // whose verdict was taken away whether or not the file is still
                // there. Three withheld figures over three different populations;
                // adding any two of them would answer no question.
                "withheldTotalBytes", "registeredWithheldCount",
                // Eleven of the twelve counts that split withheldCandidateCount, appended
                // in the order the split declares them; the twelfth is last in the
                // object. Together they add up to it. Each is one finding about one
                // machine and nothing may add any two of them: the screen's four
                // verdicts, that screen having answered about nothing, a per-file
                // identity read that gave up, the whole walk-derived offer going at once
                // on a fact about the machine, the age check's two, and the containment
                // check's two.
                "withheldIdentityUnestablishedCount", "withheldWholesaleCount",
                "withheldDeclaredProductInstalledCount",
                "withheldDeclaredProductUnestablishedCount",
                "withheldScreenUnansweredCount",
                "withheldUnderADayOldCount", "withheldAgeUnestablishedCount",
                "withheldDeclaredPatchRegisteredCount",
                "withheldDeclaredPatchUnestablishedCount",
                "withheldContainmentRefusedCount", "withheldContainmentUnestablishedCount",
                // Superseded rows the containment check kept back, by its verdict. They
                // are registered rows and no part of the split above.
                "supersededContainmentRefusedCount", "supersededContainmentUnestablishedCount",
                // Superseded rows withheld on a recorded path the scan could not settle:
                // a sub-count of withheldPatchCount, never added to it.
                "supersededRecordedPathUnestablishedCount",
                // Listed programs the scan could not check, installations of listed
                // programs the recovery found, and cached patch files it could not match
                // to a program it asks about.
                "unsettledEnumeratedProductCount",
                "recoveredEnumeratedInstallationCount",
                "unattributedPatchFileCount",
                // Superseded rows the scan-wide withholding took: also a sub-count of
                // withheldPatchCount, and the recorded-path count above is a sub-count of
                // this.
                "supersededScanWideWithheldCount",
                // The twelfth arm of the split of withheldCandidateCount: installation
                // packages the screen kept beside an installation that may be a second
                // copy of a program, either one whose packages it could not all see or one
                // whose cached package does not say what it declares and whose own record
                // does not show an ordinary installation.
                "withheldSecondCopyUnestablishedCount",
                // The drives and shares the declared-product screen gave up and kept a file
                // at, one count for each way it gives one up in the order the routes are
                // declared, the files it kept at them, a part of the withheld list counted in
                // the split above and never added to it, and the waits it made.
                "sourcesGivenUpStoppedCount", "sourcesGivenUpNoAnswerCount",
                "sourcesGivenUpSlowFailureCount", "sourcesGivenUpFailedReadsCount",
                "sourcesGivenUpReadTimeCount", "filesKeptForSourcesGivenUpCount",
                "sourceWaitsShownCount",
                // What the screen found of the two conditions under which it holds back every
                // installation package it would otherwise let through. First, how many listed
                // installations it looked at, the installations setting the hold by what their
                // cached package gave and then by why their record did not settle it, those of
                // them that are per-machine, and those whose record showed them ordinary.
                "secondCopyListedCheckedCount",
                "secondCopyKeepPathUnreadableCount", "secondCopyKeepNoneRecordedCount",
                "secondCopyKeepNotThereCount", "secondCopyKeepWouldNotReadCount",
                "secondCopyKeepNoProductCodeCount",
                "secondCopyKeepAnotherAccountCount", "secondCopyKeepPackageCodeUnansweredCount",
                "secondCopyKeepInstanceTypeNotOrdinaryCount",
                "secondCopyKeepPerMachineCount", "secondCopyReleasedOrdinaryCount",
                // Then how many installations not ruled out as a second copy it looked at,
                // which step stopped the read at one whose packages could not all be seen,
                // whether that one is per-machine, and the files held back where a package on
                // the network that a file could be by its name could not be ruled out.
                "secondCopyUnruledCheckedCount",
                "secondCopyUnseenPathUnreadableCount", "secondCopyUnseenNoneRecordedCount",
                "secondCopyUnseenNotThereCount", "secondCopyUnseenWouldNotIdentifyCount",
                "secondCopyUnseenWouldNotReadCount", "secondCopyUnseenNoProductCodeCount",
                "secondCopyUnseenPerUserUnmanagedCount", "secondCopyUnseenSourcesGivenUpCount",
                "secondCopyUnseenSourceNotRuledOutCount", "secondCopyUnseenPerMachineCount",
                "secondCopyUnseenByNameFileCount",
                // And the installations it found with no package for Windows Installer to
                // open, which set no hold.
                "secondCopyReleasedOpensNoPackageCount",
                // Then the installations recording no cached package whose sources it read: those
                // whose packages it saw, which set no hold, and of those setting it, what kept each.
                "secondCopyReleasedBySourcesCount",
                "secondCopyKeepNoneRecordedOtherAnswerCount", "secondCopyKeepNoneRecordedRegistryDisagreesCount",
                "secondCopyKeepNoneRecordedSourcesNotRuledOutCount",
            ],
            root.GetProperty("scan").EnumerateObject().Select(p => p.Name));

        Assert.Equal(
            [
                "kind", "outcome", "durationMs", "filesProcessed", "filesFailed", "bytesFreed",
                "errors", "moveDestinationKind", "heldBackReclaimed", "heldBackRecordsChanged",
                "heldBackRecordsUnreadable",
                // Appended, and it is a different KIND of finding from the three
                // before it rather than a fourth of the same: those name what the
                // records said about the file that was dropped, and this one is
                // reached without reading anything about the file at all.
                "heldBackOwnershipUnestablished",
                // Schema 5's, and a fifth kind: a check the scan makes on the file
                // itself, made again just before acting.
                "heldBackFileNotConfirmed",
                // The scan's keys of the same names, for the check made when Move or
                // Delete is clicked. Its files count is a part of the held-back causes above.
                "sourcesGivenUpStoppedCount", "sourcesGivenUpNoAnswerCount",
                "sourcesGivenUpSlowFailureCount", "sourcesGivenUpFailedReadsCount",
                "sourcesGivenUpReadTimeCount", "filesKeptForSourcesGivenUpCount",
                "sourceWaitsShownCount",
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
    public void The_withholding_arithmetic_travels_as_its_tallies_and_never_as_its_sum()
    {
        // The figure the Application-log notice carries is the sum of three counts that
        // never overlap, and the report sends the three rather than the sum. The registry
        // and API headcounts travel for their own sake rather than as its inputs: nothing
        // is derived from the difference between them, and a fleet's spread of that
        // difference is a fact about machines that no verdict of the app's carries.
        var scan = new ScanResult(
            Array.Empty<OrphanedFile>(), Array.Empty<RegisteredPackage>(), 0,
            UnaccountedProductCount: 9,
            Census: new EnumerationCensus(
                UnreadableProducts: 4, SkippedProductRows: 1,
                RegistryProductKeys: 40, UnclaimedProductFiles: 6, UnclaimedPatchFiles: 2,
                ProductCount: 30, UnansweredProductCount: 2, UnparseableProductKeyNames: 3,
                UnsettledEnumeratedProductCount: 4, UnattributedPatchFileCount: 1));

        var info = ScanInfo.From(scan, 10);
        var machine = MachineInfo.From(scan);

        Assert.Equal(4, info.UnreadableProductCount);
        Assert.Equal(1, info.SkippedProductRowCount);
        Assert.Equal(6, info.UnclaimedProductFileCount);
        Assert.Equal(2, info.UnclaimedPatchFileCount);
        Assert.Equal(1, info.UnattributedPatchFileCount);
        Assert.Equal(30, machine.ProductCount);
        Assert.Equal(40, machine.RegistryProductKeyCount);

        // The figure reproduces from three of them.
        Assert.Equal(9, info.UnsettledEnumeratedProductCount + info.UnansweredProductCount
            + machine.UnparseableProductKeyCount);

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
    public void Every_held_back_cause_reaches_the_payload_and_they_are_not_summed()
    {
        // A batch can meet several causes at once, and one cause named for the set
        // would be false of some of its members. THAT IS WHY THERE ARE SEVERAL COUNTS
        // RATHER THAN ONE. Distinct values so a transposition between two of them
        // fails rather than cancelling out.
        //
        // KEEP THE COUNT OUT OF THE NAME. A name saying "all four" goes on passing
        // over four of five once a fifth cause is added, its own name claiming it
        // covered the lot: a count in a name is a claim nothing checks.
        var reasons = new HeldBackReasons(
            Reclaimed: 1, RecordsChanged: 2, RecordsUnreadable: 3, OwnershipUnestablished: 4,
            FileNotConfirmed: 5);

        var op = OperationInfo.FromDelete(
            new DeleteResult(0, Array.Empty<FileOperationError>()),
            bytesFreed: 0, durationMs: 0, check: Check(reasons));

        Assert.Equal(1, op.HeldBackReclaimed);
        Assert.Equal(2, op.HeldBackRecordsChanged);
        Assert.Equal(3, op.HeldBackRecordsUnreadable);
        Assert.Equal(4, op.HeldBackOwnershipUnestablished);
        Assert.Equal(5, op.HeldBackFileNotConfirmed);

        // The tally knows its own total and the payload deliberately does not
        // carry it: a total invites one sentence over causes that need one each.
        Assert.Equal(1 + 2 + 3 + 4 + 5, reasons.Total);
        var json = JsonSerializer.Serialize(op, JsonOptions);
        Assert.DoesNotContain("heldBackTotal", json);
    }

    [Fact]
    public void Every_held_back_cause_reaches_the_payload_from_a_Move_too()
    {
        // FromMove passes the causes on through its own argument list, written apart
        // from FromDelete's, so each of the two is held to the mapping by a test of
        // its own. Distinct values for the reason the test above gives.
        var reasons = new HeldBackReasons(
            Reclaimed: 1, RecordsChanged: 2, RecordsUnreadable: 3, OwnershipUnestablished: 4,
            FileNotConfirmed: 5);

        var op = OperationInfo.FromMove(
            new MoveResult(0, Array.Empty<FileOperationError>()),
            bytesFreed: 0, durationMs: 0,
            moveDestinationKind: MoveDestinationKinds.SameDrive, check: Check(reasons));

        Assert.Equal(1, op.HeldBackReclaimed);
        Assert.Equal(2, op.HeldBackRecordsChanged);
        Assert.Equal(3, op.HeldBackRecordsUnreadable);
        Assert.Equal(4, op.HeldBackOwnershipUnestablished);
        Assert.Equal(5, op.HeldBackFileNotConfirmed);
    }

    /// <summary>
    /// Drives and shares given up keeping files, a different number by each route, so a count
    /// read off the wrong route or the wrong position fails, and so does a pair of routes
    /// swapped: one by <see cref="SourceRootGiveUpRoute.StoppedWaiting"/>, two by
    /// <see cref="SourceRootGiveUpRoute.NoAnswer"/>, three by
    /// <see cref="SourceRootGiveUpRoute.SlowFailure"/>, four by
    /// <see cref="SourceRootGiveUpRoute.FailedReadsAddUp"/> and five by
    /// <see cref="SourceRootGiveUpRoute.ReadsAddUp"/>. Beside them, one given up keeping
    /// nothing, which no count takes in. Each files count is a different power of two, so a sum
    /// names exactly which roots it took in.
    /// </summary>
    private static IReadOnlyList<SourceRootGivenUp> GivenUpByEveryRoute() =>
    [
        new("D:", SourceRootGiveUpRoute.StoppedWaiting, 1),
        new(@"\\nas\apps", SourceRootGiveUpRoute.NoAnswer, 2),
        new("E:", SourceRootGiveUpRoute.NoAnswer, 4),
        new("F:", SourceRootGiveUpRoute.SlowFailure, 8),
        new(@"\\nas\media", SourceRootGiveUpRoute.SlowFailure, 16),
        new("G:", SourceRootGiveUpRoute.SlowFailure, 32),
        new("H:", SourceRootGiveUpRoute.FailedReadsAddUp, 64),
        new(@"\\nas\backup", SourceRootGiveUpRoute.FailedReadsAddUp, 128),
        new("I:", SourceRootGiveUpRoute.FailedReadsAddUp, 256),
        new("K:", SourceRootGiveUpRoute.FailedReadsAddUp, 512),
        new("L:", SourceRootGiveUpRoute.ReadsAddUp, 1024),
        new(@"\\nas\games", SourceRootGiveUpRoute.ReadsAddUp, 2048),
        new("M:", SourceRootGiveUpRoute.ReadsAddUp, 4096),
        new("N:", SourceRootGiveUpRoute.ReadsAddUp, 8192),
        new("O:", SourceRootGiveUpRoute.ReadsAddUp, 16384),
        new("J:", SourceRootGiveUpRoute.NoAnswer, 0),
    ];

    private static int[] GivenUpByRoute(ScanInfo info) =>
    [
        info.SourcesGivenUpStoppedCount, info.SourcesGivenUpNoAnswerCount,
        info.SourcesGivenUpSlowFailureCount, info.SourcesGivenUpFailedReadsCount,
        info.SourcesGivenUpReadTimeCount,
    ];

    private static int[] GivenUpByRoute(OperationInfo op) =>
    [
        op.SourcesGivenUpStoppedCount, op.SourcesGivenUpNoAnswerCount,
        op.SourcesGivenUpSlowFailureCount, op.SourcesGivenUpFailedReadsCount,
        op.SourcesGivenUpReadTimeCount,
    ];

    [Fact]
    public void The_drives_and_shares_a_scan_gave_up_and_kept_files_at_are_counted_by_route()
    {
        // A drive or share given up keeping nothing is in no count and adds no files, and the
        // files kept at the rest are summed.
        var scan = new ScanResult(
            Array.Empty<OrphanedFile>(), Array.Empty<RegisteredPackage>(), 0,
            SourceRootsGivenUp: GivenUpByEveryRoute(), SourceWaitCount: 9);

        var info = ScanInfo.From(scan, 10);

        Assert.Equal([1, 2, 3, 4, 5], GivenUpByRoute(info));
        Assert.Equal(32767, info.FilesKeptForSourcesGivenUpCount);
        Assert.Equal(9, info.SourceWaitsShownCount);

        // Counts only: no name travels.
        var json = JsonSerializer.Serialize(info, JsonOptions);
        Assert.DoesNotContain("nas", json);
        Assert.DoesNotContain("D:", json);
    }

    [Fact]
    public void A_scan_that_waited_and_gave_nothing_up_still_counts_its_waits()
    {
        var scan = new ScanResult(
            Array.Empty<OrphanedFile>(), Array.Empty<RegisteredPackage>(), 0, SourceWaitCount: 4);

        var info = ScanInfo.From(scan, 10);

        Assert.Equal([0, 0, 0, 0, 0], GivenUpByRoute(info));
        Assert.Equal(0, info.FilesKeptForSourcesGivenUpCount);
        Assert.Equal(4, info.SourceWaitsShownCount);
    }

    [Fact]
    public void What_the_screen_found_of_the_installations_travels_member_by_member()
    {
        // The census's ints in a row, each given a different value, so an argument that lands on
        // its neighbour's key fails here rather than sending one count under another's name.
        var scan = new ScanResult(
            Array.Empty<OrphanedFile>(), Array.Empty<RegisteredPackage>(), 0,
            CachedPackageCensus: new CachedPackageCensus(
                ListedChecked: 64, KeptPathUnreadable: 1, KeptNoneRecorded: 2, KeptNotThere: 3,
                KeptWouldNotRead: 4, KeptNoProductCode: 5, KeptAnotherAccount: 6,
                KeptPackageCodeUnanswered: 7, KeptInstanceTypeNotOrdinary: 8, KeptPerMachine: 9,
                ReleasedOrdinary: 10, UnruledChecked: 11, UnseenPathUnreadable: 12, UnseenNoneRecorded: 13,
                UnseenNotThere: 14, UnseenWouldNotIdentify: 15, UnseenWouldNotRead: 16,
                UnseenNoProductCode: 17, UnseenPerUserUnmanaged: 18, UnseenSourcesGivenUp: 19,
                UnseenSourceNotRuledOut: 20, UnseenPerMachine: 21, UnseenByNameFiles: 22,
                ReleasedOpensNoPackage: 23, ReleasedBySources: 24, KeptNoneRecordedOtherAnswer: 25,
                KeptNoneRecordedRegistryDisagrees: 26, KeptNoneRecordedSourcesNotRuledOut: 27));

        var info = ScanInfo.From(scan, 10);

        Assert.Equal(
            [64, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 17, 18, 19, 20, 21, 22, 23, 24, 25, 26, 27],
            [
                info.SecondCopyListedCheckedCount, info.SecondCopyKeepPathUnreadableCount,
                info.SecondCopyKeepNoneRecordedCount, info.SecondCopyKeepNotThereCount,
                info.SecondCopyKeepWouldNotReadCount, info.SecondCopyKeepNoProductCodeCount,
                info.SecondCopyKeepAnotherAccountCount, info.SecondCopyKeepPackageCodeUnansweredCount,
                info.SecondCopyKeepInstanceTypeNotOrdinaryCount, info.SecondCopyKeepPerMachineCount,
                info.SecondCopyReleasedOrdinaryCount,
                info.SecondCopyUnruledCheckedCount, info.SecondCopyUnseenPathUnreadableCount,
                info.SecondCopyUnseenNoneRecordedCount, info.SecondCopyUnseenNotThereCount,
                info.SecondCopyUnseenWouldNotIdentifyCount, info.SecondCopyUnseenWouldNotReadCount,
                info.SecondCopyUnseenNoProductCodeCount, info.SecondCopyUnseenPerUserUnmanagedCount,
                info.SecondCopyUnseenSourcesGivenUpCount, info.SecondCopyUnseenSourceNotRuledOutCount,
                info.SecondCopyUnseenPerMachineCount, info.SecondCopyUnseenByNameFileCount,
                info.SecondCopyReleasedOpensNoPackageCount, info.SecondCopyReleasedBySourcesCount,
                info.SecondCopyKeepNoneRecordedOtherAnswerCount, info.SecondCopyKeepNoneRecordedRegistryDisagreesCount,
                info.SecondCopyKeepNoneRecordedSourcesNotRuledOutCount,
            ]);

        using var doc = JsonDocument.Parse(JsonSerializer.Serialize(info, JsonOptions));
        Assert.Equal(64, doc.RootElement.GetProperty("secondCopyListedCheckedCount").GetInt32());
        Assert.Equal(9, doc.RootElement.GetProperty("secondCopyKeepPerMachineCount").GetInt32());
        Assert.Equal(10, doc.RootElement.GetProperty("secondCopyReleasedOrdinaryCount").GetInt32());
        Assert.Equal(11, doc.RootElement.GetProperty("secondCopyUnruledCheckedCount").GetInt32());
        Assert.Equal(21, doc.RootElement.GetProperty("secondCopyUnseenPerMachineCount").GetInt32());
        Assert.Equal(22, doc.RootElement.GetProperty("secondCopyUnseenByNameFileCount").GetInt32());
        Assert.Equal(23, doc.RootElement.GetProperty("secondCopyReleasedOpensNoPackageCount").GetInt32());
        Assert.Equal(24, doc.RootElement.GetProperty("secondCopyReleasedBySourcesCount").GetInt32());
        Assert.Equal(25, doc.RootElement.GetProperty("secondCopyKeepNoneRecordedOtherAnswerCount").GetInt32());
        Assert.Equal(26, doc.RootElement.GetProperty("secondCopyKeepNoneRecordedRegistryDisagreesCount").GetInt32());
        Assert.Equal(27, doc.RootElement.GetProperty("secondCopyKeepNoneRecordedSourcesNotRuledOutCount").GetInt32());
    }

    [Fact]
    public void A_scan_whose_screen_looked_at_no_installation_sends_a_zero_for_every_census_count()
    {
        var info = ScanInfo.From(
            new ScanResult(Array.Empty<OrphanedFile>(), Array.Empty<RegisteredPackage>(), 0), 10);

        Assert.All(
            [
                info.SecondCopyListedCheckedCount, info.SecondCopyKeepPathUnreadableCount,
                info.SecondCopyKeepNoneRecordedCount, info.SecondCopyKeepNotThereCount,
                info.SecondCopyKeepWouldNotReadCount, info.SecondCopyKeepNoProductCodeCount,
                info.SecondCopyKeepAnotherAccountCount, info.SecondCopyKeepPackageCodeUnansweredCount,
                info.SecondCopyKeepInstanceTypeNotOrdinaryCount, info.SecondCopyKeepPerMachineCount,
                info.SecondCopyReleasedOrdinaryCount,
                info.SecondCopyUnruledCheckedCount, info.SecondCopyUnseenPathUnreadableCount,
                info.SecondCopyUnseenNoneRecordedCount, info.SecondCopyUnseenNotThereCount,
                info.SecondCopyUnseenWouldNotIdentifyCount, info.SecondCopyUnseenWouldNotReadCount,
                info.SecondCopyUnseenNoProductCodeCount, info.SecondCopyUnseenPerUserUnmanagedCount,
                info.SecondCopyUnseenSourcesGivenUpCount, info.SecondCopyUnseenSourceNotRuledOutCount,
                info.SecondCopyUnseenPerMachineCount, info.SecondCopyUnseenByNameFileCount,
                info.SecondCopyReleasedOpensNoPackageCount, info.SecondCopyReleasedBySourcesCount,
                info.SecondCopyKeepNoneRecordedOtherAnswerCount, info.SecondCopyKeepNoneRecordedRegistryDisagreesCount,
                info.SecondCopyKeepNoneRecordedSourcesNotRuledOutCount,
            ],
            count => Assert.Equal(0, count));
    }

    [Fact]
    public void The_check_s_drives_and_shares_given_up_reach_the_operation_block_of_a_Delete()
    {
        var check = new ReverifyResult([], [], SourceRootsGivenUp: GivenUpByEveryRoute(), SourceWaitCount: 9);

        var op = OperationInfo.FromDelete(
            new DeleteResult(0, Array.Empty<FileOperationError>()),
            bytesFreed: 0, durationMs: 0, check: check);

        Assert.Equal([1, 2, 3, 4, 5], GivenUpByRoute(op));
        Assert.Equal(32767, op.FilesKeptForSourcesGivenUpCount);
        Assert.Equal(9, op.SourceWaitsShownCount);
    }

    [Fact]
    public void The_check_s_drives_and_shares_given_up_reach_the_operation_block_of_a_Move()
    {
        // FromMove passes them on through its own argument list, written apart from
        // FromDelete's, so each is held to the mapping by a test of its own.
        var check = new ReverifyResult([], [], SourceRootsGivenUp: GivenUpByEveryRoute(), SourceWaitCount: 9);

        var op = OperationInfo.FromMove(
            new MoveResult(0, Array.Empty<FileOperationError>()),
            bytesFreed: 0, durationMs: 0,
            moveDestinationKind: MoveDestinationKinds.SameDrive, check: check);

        Assert.Equal([1, 2, 3, 4, 5], GivenUpByRoute(op));
        Assert.Equal(32767, op.FilesKeptForSourcesGivenUpCount);
        Assert.Equal(9, op.SourceWaitsShownCount);
    }

    [Fact]
    public void A_report_s_scan_and_operation_blocks_each_count_their_own_pass()
    {
        // The scan and the check made when Move or Delete is clicked give up their own
        // drives and shares, so each block carries its own pass and not the other's.
        var scan = new ScanResult(
            Array.Empty<OrphanedFile>(), Array.Empty<RegisteredPackage>(), 0,
            SourceRootsGivenUp: [new("D:", SourceRootGiveUpRoute.SlowFailure, 3)], SourceWaitCount: 2);
        var check = new ReverifyResult([], [],
            SourceRootsGivenUp: [new("E:", SourceRootGiveUpRoute.StoppedWaiting, 5)], SourceWaitCount: 7);

        var entry = ResultLogEntry.ForDelete(
            scan, 100, new DeleteResult(1, Array.Empty<FileOperationError>()), 10, 20, check, "GB");

        Assert.Equal([0, 0, 1, 0, 0], GivenUpByRoute(entry.Scan));
        Assert.Equal(3, entry.Scan.FilesKeptForSourcesGivenUpCount);
        Assert.Equal(2, entry.Scan.SourceWaitsShownCount);
        Assert.Equal([1, 0, 0, 0, 0], GivenUpByRoute(entry.Operation));
        Assert.Equal(5, entry.Operation.FilesKeptForSourcesGivenUpCount);
        Assert.Equal(7, entry.Operation.SourceWaitsShownCount);
    }

    [Fact]
    public void Every_give_up_route_is_counted_by_its_own_key_in_the_order_the_routes_are_declared()
    {
        // THE ENUM, WALKED. A route added to it without a key of its own fails here rather
        // than in the field, and the keys stay in the order the routes are declared, which is
        // the order a root given up more than one way takes the first of.
        var routes = Enum.GetValues<SourceRootGiveUpRoute>();
        var keys = GivenUpByRoute(SampleScan()).Length;
        Assert.Equal(keys, routes.Length);

        for (var i = 0; i < routes.Length; i++)
        {
            var scan = new ScanResult(
                Array.Empty<OrphanedFile>(), Array.Empty<RegisteredPackage>(), 0,
                SourceRootsGivenUp: [new("D:", routes[i], 1)]);

            var expected = new int[keys];
            expected[i] = 1;
            Assert.Equal(expected, GivenUpByRoute(ScanInfo.From(scan, 0)));
        }
    }

    [Fact]
    public void A_give_up_route_with_no_count_fails_rather_than_going_uncounted()
    {
        var scan = new ScanResult(
            Array.Empty<OrphanedFile>(), Array.Empty<RegisteredPackage>(), 0,
            SourceRootsGivenUp: [new("D:", (SourceRootGiveUpRoute)99, 1)]);

        Assert.Throws<ArgumentOutOfRangeException>(() => ScanInfo.From(scan, 0));
    }

    [Fact]
    public void The_tally_totals_every_cause_it_carries()
    {
        // THE DENOMINATOR, ENUMERATED, because Total is a hand-written sum over a
        // record whose members can grow, and a member left out of it reads as a
        // batch that lost fewer files than it did. One member at 1 and the rest at
        // their default, so what each contributes is attributable to it alone.
        var members = typeof(HeldBackReasons).GetConstructors()
            .OrderByDescending(c => c.GetParameters().Length).First().GetParameters();

        Assert.True(members.Length >= 4,
            $"HeldBackReasons has {members.Length} parameters, which is too few for this walk "
            + "to be measuring what it claims.");
        Assert.All(members, p => Assert.Equal(typeof(int), p.ParameterType));

        var missing = new List<string>();
        foreach (var member in members)
        {
            var args = new object[members.Length];
            for (var i = 0; i < args.Length; i++) args[i] = 0;
            args[member.Position] = 1;

            var tally = (HeldBackReasons)members[0].Member
                .DeclaringType!.GetConstructors()
                .OrderByDescending(c => c.GetParameters().Length).First().Invoke(args);
            if (tally.Total != 1) missing.Add(member.Name!);
        }

        Assert.True(missing.Count == 0,
            "HeldBackReasons.Total does not count every cause it carries, so a batch reports "
            + "fewer files kept back than it kept: " + string.Join(", ", missing));
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
            MissingAffectedCount: 2, MissingUnaffectedCount: 7);

        var info = ScanInfo.From(scan, 10);

        Assert.Equal(9, info.MissingFromDiskCount);
        Assert.Equal(2, info.MissingNeededCount);
    }

    [Fact]
    public void The_report_carries_the_region_it_is_given_in_the_receivers_shape()
    {
        Assert.Equal("GB", AppInfo.Current("gb").WindowsRegion);
        Assert.Equal("419", AppInfo.Current("419").WindowsRegion);
        Assert.Equal(WindowsRegionLabel.Unreadable, AppInfo.Current(null).WindowsRegion);
    }

    [Fact]
    public void The_report_language_is_the_language_the_app_shows()
    {
        // The language the app's strings resolve to, which is what the user read. A
        // UI culture with no translation of its own reports the English the app
        // showed, a regional culture reports its language, and a language picked in
        // the app is reported as picked whatever the thread's culture says.
        var ui = CultureInfo.CurrentUICulture;
        var uiOverride = Localisation.UiCultureOverride;
        var formatOverride = Localisation.FormatCultureOverride;
        try
        {
            Localisation.Reset();

            CultureInfo.CurrentUICulture = CultureInfo.GetCultureInfo("cs-CZ");
            Assert.Equal(SupportedLanguages.Neutral, AppInfo.Current("GB").Language);

            CultureInfo.CurrentUICulture = CultureInfo.GetCultureInfo("de-AT");
            Assert.Equal("de", AppInfo.Current("GB").Language);

            var picked = CultureInfo.GetCultureInfo("ja");
            Localisation.Set(picked, picked);
            Assert.Equal("ja", AppInfo.Current("GB").Language);
        }
        finally
        {
            CultureInfo.CurrentUICulture = ui;
            if (uiOverride is null) Localisation.Reset();
            else Localisation.Set(uiOverride, formatOverride ?? uiOverride);
        }
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
            check: Check());

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
            bytesFreed: 0, durationMs: 0, check: Check());

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
        Assert.Equal(0, op.HeldBackRecordsChanged);
        Assert.Equal(0, op.HeldBackRecordsUnreadable);
        Assert.Equal(0, op.HeldBackOwnershipUnestablished);
        Assert.Equal(0, op.HeldBackFileNotConfirmed);

        // And no check gave up a drive or share or made anybody wait.
        Assert.Equal([0, 0, 0, 0, 0], GivenUpByRoute(op));
        Assert.Equal(0, op.FilesKeptForSourcesGivenUpCount);
        Assert.Equal(0, op.SourceWaitsShownCount);
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
            check: Check(new HeldBackReasons(Reclaimed: 3)));

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
            moveDestinationKind: MoveDestinationKinds.SameDrive, check: Check());

        Assert.Equal(OperationOutcomes.Complete, op.Outcome);
        Assert.Equal(0, op.FilesProcessed);
        Assert.Equal(0, op.FilesFailed);
    }

    [Fact]
    public void A_batch_with_one_success_and_one_failure_is_partial()
    {
        var errors = new List<FileOperationError> { new FileInUse(@"C:\Windows\Installer\a.msi") };

        var op = OperationInfo.FromDelete(new DeleteResult(1, errors),
            bytesFreed: 1024, durationMs: 0, check: Check());

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
                bytesFreed: 0, durationMs: 0, check: Check()).Outcome;
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
        // THE INPUTS NO LONGER ARISE FROM A SCAN AND THE DERIVATION IS KEPT
        // DELIBERATELY. No registered patch reaches the offer from 3.0.0, so both
        // figures are zero in every real report; they go on being DERIVED from the
        // offer rather than written as literals, and this is the test that would
        // notice if a patch ever reached that list again. The rows are built by
        // hand here for the same reason.
        //
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

    [Fact]
    public void ScanInfo_From_maps_every_member_to_the_scan_figure_it_names()
    {
        // EVERY SOURCE FIGURE IS DISTINCT, WHICH IS THE WHOLE MECHANISM. ScanInfo is
        // a positional record and From fills it positionally, so an argument placed
        // among the others re-points every one after it at its neighbour's value, and
        // the two being ints is all it takes for that to compile. No two figures below
        // are equal, so any such shift fails at least one assertion here.
        var removable = new List<OrphanedFile>
        {
            new(@"C:\p1.msp", 100, true, IsRemovablePatch: true, IsObsoleted: true, "Obsoleted"),
            new(@"C:\p2.msp", 200, true, IsRemovablePatch: true, IsObsoleted: true, "Obsoleted"),
            new(@"C:\p3.msp", 300, true, IsRemovablePatch: true, IsObsoleted: false, "Superseded"),
            new(@"C:\p4.msp", 400, true, IsRemovablePatch: true, IsObsoleted: false, "Superseded"),
            new(@"C:\p5.msp", 500, true, IsRemovablePatch: true, IsObsoleted: false, "Superseded"),
            new(@"C:\o1.msi", 600, false, IsRemovablePatch: false, IsObsoleted: false, "Orphaned"),
            new(@"C:\o2.msi", 700, false, IsRemovablePatch: false, IsObsoleted: false, "Orphaned"),
            new(@"C:\o3.msi", 800, false, IsRemovablePatch: false, IsObsoleted: false, "Orphaned"),
            new(@"C:\o4.msi", 900, false, IsRemovablePatch: false, IsObsoleted: false, "Orphaned"),
            new(@"C:\o5.msi", 1000, false, IsRemovablePatch: false, IsObsoleted: false, "Orphaned"),
        };
        var withheld = Enumerable.Range(1, 6)
            .Select(i => new OrphanedFile($@"C:\w{i}.msi", 11, false, false, false, "Withheld"))
            .ToList();
        // Eight of the nine are superseded rows the scan-wide withholding took. The first
        // two were taken on an unsettled recorded path, the first with its file on disk and
        // the second with its file gone, so that count is 1. The other six have their files
        // on disk, so the scan-wide count is 7. Neither figure is carried by any other member
        // here, and the on-disk test has to hold them at 1 and 7 rather than 2 and 8.
        var registered = Enumerable.Range(1, 9)
            .Select(i => i <= 8
                ? new RegisteredPackage($@"C:\r{i}.msp", $"Product {i}", $"{{code-{i}}}",
                    PatchState: 2, RemovableWithheld: true, FileExists: i != 2,
                    WithheldOnRecordedPathUnestablished: i <= 2, WithheldScanWide: true)
                : new RegisteredPackage($@"C:\r{i}.msi", $"Product {i}", $"{{code-{i}}}"))
            .ToList();

        var scan = new ScanResult(
            removable,
            registered,
            RegisteredTotalBytes: 7002,
            MissingAffectedCount: 11,
            MissingUnaffectedCount: 20,
            WithheldCount: 12,
            Census: new EnumerationCensus(
                UnreadableProducts: 13,
                SkippedProductRows: 14,
                UnclaimedProductFiles: 15,
                UnclaimedPatchFiles: 16,
                RecoveredProductCount: 17,
                UnansweredProductCount: 18,
                UnsettledEnumeratedProductCount: 34,
                RecoveredEnumeratedInstallationCount: 35,
                UnattributedPatchFileCount: 36),
            RegisteredWithheldCount: 19,
            WithheldFiles: withheld,
            WithheldBy: new WithholdingSplit(20, 21, 22, 23, 24, 25, 26, 27, 28, 29, 30, 37),
            SupersededContainmentRefusedCount: 32,
            SupersededContainmentUnestablishedCount: 33);

        var info = ScanInfo.From(scan, 7001);

        Assert.Equal(7001, info.DurationMs);
        Assert.Equal(9, info.RegisteredCount);
        Assert.Equal(7002, info.RegisteredBytes);
        Assert.Equal(5, info.OrphanedCount);
        Assert.Equal(3, info.SupersededCount);
        Assert.Equal(2, info.ObsoletedCount);
        Assert.Equal(5500, info.RemovableBytes);
        Assert.Equal(31, info.MissingFromDiskCount);
        Assert.Equal(11, info.MissingNeededCount);
        Assert.Equal(12, info.WithheldPatchCount);
        Assert.Equal(13, info.UnreadableProductCount);
        Assert.Equal(14, info.SkippedProductRowCount);
        Assert.Equal(15, info.UnclaimedProductFileCount);
        Assert.Equal(16, info.UnclaimedPatchFileCount);
        Assert.Equal(17, info.RecoveredProductCount);
        Assert.Equal(18, info.UnansweredProductCount);
        Assert.Equal(6, info.WithheldCandidateCount);
        Assert.Equal(66, info.WithheldTotalBytes);
        Assert.Equal(19, info.RegisteredWithheldCount);
        Assert.Equal(20, info.WithheldIdentityUnestablishedCount);
        Assert.Equal(21, info.WithheldWholesaleCount);
        Assert.Equal(22, info.WithheldDeclaredProductInstalledCount);
        Assert.Equal(23, info.WithheldDeclaredProductUnestablishedCount);
        Assert.Equal(24, info.WithheldScreenUnansweredCount);
        Assert.Equal(25, info.WithheldUnderADayOldCount);
        Assert.Equal(26, info.WithheldAgeUnestablishedCount);
        Assert.Equal(27, info.WithheldDeclaredPatchRegisteredCount);
        Assert.Equal(28, info.WithheldDeclaredPatchUnestablishedCount);
        Assert.Equal(29, info.WithheldContainmentRefusedCount);
        Assert.Equal(30, info.WithheldContainmentUnestablishedCount);
        Assert.Equal(32, info.SupersededContainmentRefusedCount);
        Assert.Equal(33, info.SupersededContainmentUnestablishedCount);
        Assert.Equal(1, info.SupersededRecordedPathUnestablishedCount);
        Assert.Equal(34, info.UnsettledEnumeratedProductCount);
        Assert.Equal(35, info.RecoveredEnumeratedInstallationCount);
        Assert.Equal(36, info.UnattributedPatchFileCount);
        Assert.Equal(7, info.SupersededScanWideWithheldCount);
        Assert.Equal(37, info.WithheldSecondCopyUnestablishedCount);
    }

    [Fact]
    public void Every_arm_of_the_withholding_split_travels_under_its_own_key()
    {
        // WALKED OFF THE SPLIT'S CONSTRUCTOR RATHER THAN LISTED, so an arm added to
        // WithholdingSplit fails here until the report carries it. The split is a
        // partition of withheldCandidateCount, and an arm missing from the report
        // leaves a reader holding counts that no longer add up to it with nothing
        // on the wire to say why.
        //
        // Each arm gets a distinct value, so an arm carried under its neighbour's
        // key fails too rather than passing on a coincidence.
        var arms = typeof(WithholdingSplit).GetConstructors()
            .OrderByDescending(c => c.GetParameters().Length).First();
        var members = arms.GetParameters();
        Assert.True(members.Length >= 9,
            $"WithholdingSplit has {members.Length} parameters, which is too few for this walk "
            + "to be measuring what it claims.");

        var values = members.Select(p => (object)(100 + p.Position)).ToArray();
        var split = (WithholdingSplit)arms.Invoke(values);
        var scan = new ScanResult(
            Array.Empty<OrphanedFile>(), Array.Empty<RegisteredPackage>(), 0,
            WithheldBy: split);

        var info = ScanInfo.From(scan, 0);

        var missing = new List<string>();
        var carried = 0;
        foreach (var member in members)
        {
            var key = typeof(ScanInfo).GetProperty("Withheld" + member.Name);
            var sent = key?.GetValue(info);
            if (!Equals(sent, values[member.Position]))
                missing.Add($"{member.Name} (sent {sent ?? "nothing"})");
            else
                carried += (int)sent!;
        }

        Assert.True(missing.Count == 0,
            "The report does not carry every arm of the withholding split under its own "
            + "key: " + string.Join(", ", missing));
        Assert.Equal(split.Total, carried);
    }

    [Fact]
    public void MachineInfo_From_maps_every_member_to_the_scan_figure_it_names()
    {
        // THE SAME MECHANISM AS THE SCANINFO TEST ABOVE AND FOR THE SAME REASON.
        // MachineInfo is a positional record and From fills it positionally; forty of
        // its forty-one members are ints, so a value placed among them re-points
        // every one after it at its neighbour's and still compiles. Every figure
        // below is distinct and every member is read, so a shift anywhere in the run
        // fails at least one assertion here.
        //
        // Serialisation is by property NAME, so a shift produces a payload whose keys
        // are all spelled correctly and whose values are one member out. Nothing
        // downstream can see that, which is why it is pinned here.
        var scan = new ScanResult(
            Array.Empty<OrphanedFile>(), Array.Empty<RegisteredPackage>(), 0,
            Census: new EnumerationCensus(
                LongLeafStemCount: 102,
                NonStringLocalPackageValues: 103,
                UnreadablePatchStates: 104,
                UnreadableVerdictPaths: 105,
                UnparseableProductKeyNames: 106,
                ProductCount: 107,
                RegistryProductKeys: 108,
                PatchClaimCount: 109,
                InstanceProductCount: 110,
                InstanceTypeUnreadableCount: 111,
                ProductPatchKeyCount: 114,
                ProductPatchRegistrationCount: 115,
                ProductsWithRemovablePatchCount: 116,
                ProductsWithPatchSetUnestablishedCount: 117,
                PathResolverAttemptCount: 118,
                PathResolverNotAPathCount: 119,
                PathResolverNoAncestorCount: 120,
                PathResolverOpenRefusedCount: 121,
                PathResolverNoFinalNameCount: 122,
                PathResolverFaultedCount: 123,
                PathNormalisationRefusedAtExpansionCount: 124,
                PathNormalisationRefusedAtPrefixStripCount: 125,
                PathNormalisationRefusedAtFullPathCount: 126,
                PathNormalisationRefusedAtEmbeddedNullCount: 127,
                PathFlaggedSpellingCount: 128,
                RegistryKeyReadFailures: 141),
            ShortNameCreation: ShortNameCreationLabels.PerVolume,
            SupersededRegistrationCount: 112,
            ObsoletedRegistrationCount: 113,
            RegistrationIdentityReads: new FileIdentityReadTally(129, 130, 131, 132, 133, 134),
            CandidateIdentityReads: new FileIdentityReadTally(135, 136, 137, 138, 139, 140));

        var machine = MachineInfo.From(scan);

        Assert.Equal(ShortNameCreationLabels.PerVolume, machine.ShortNameCreation);
        Assert.Equal(102, machine.LongFileNameCount);
        Assert.Equal(103, machine.NonStringLocalPackageCount);
        Assert.Equal(104, machine.UnreadablePatchStateCount);
        Assert.Equal(105, machine.UnreadableVerdictPathCount);
        Assert.Equal(106, machine.UnparseableProductKeyCount);
        Assert.Equal(107, machine.ProductCount);
        Assert.Equal(108, machine.RegistryProductKeyCount);
        Assert.Equal(109, machine.PatchClaimCount);
        Assert.Equal(110, machine.InstanceProductCount);
        Assert.Equal(111, machine.InstanceTypeUnreadableCount);
        Assert.Equal(112, machine.SupersededRegistrationCount);
        Assert.Equal(113, machine.ObsoletedRegistrationCount);
        Assert.Equal(114, machine.ProductPatchKeyCount);
        Assert.Equal(115, machine.ProductPatchRegistrationCount);
        Assert.Equal(116, machine.ProductsWithRemovablePatchCount);
        Assert.Equal(117, machine.ProductsWithPatchSetUnestablishedCount);
        Assert.Equal(118, machine.PathResolverAttemptCount);
        Assert.Equal(119, machine.PathResolverNotAPathCount);
        Assert.Equal(120, machine.PathResolverNoAncestorCount);
        Assert.Equal(121, machine.PathResolverOpenRefusedCount);
        Assert.Equal(122, machine.PathResolverNoFinalNameCount);
        Assert.Equal(123, machine.PathResolverFaultedCount);
        Assert.Equal(124, machine.PathNormalisationRefusedAtExpansionCount);
        Assert.Equal(125, machine.PathNormalisationRefusedAtPrefixStripCount);
        Assert.Equal(126, machine.PathNormalisationRefusedAtFullPathCount);
        Assert.Equal(127, machine.PathNormalisationRefusedAtEmbeddedNullCount);
        Assert.Equal(128, machine.PathFlaggedSpellingCount);
        Assert.Equal(129, machine.RegistrationIdentityAttemptCount);
        Assert.Equal(130, machine.RegistrationIdentityNamesNothingCount);
        Assert.Equal(131, machine.RegistrationIdentityNotAPathCount);
        Assert.Equal(132, machine.RegistrationIdentityOpenRefusedCount);
        Assert.Equal(133, machine.RegistrationIdentityUnavailableCount);
        Assert.Equal(134, machine.RegistrationIdentityFaultedCount);
        Assert.Equal(135, machine.CandidateIdentityAttemptCount);
        Assert.Equal(136, machine.CandidateIdentityNamesNothingCount);
        Assert.Equal(137, machine.CandidateIdentityNotAPathCount);
        Assert.Equal(138, machine.CandidateIdentityOpenRefusedCount);
        Assert.Equal(139, machine.CandidateIdentityUnavailableCount);
        Assert.Equal(140, machine.CandidateIdentityFaultedCount);
        Assert.Equal(141, machine.RegistryKeyReadFailureCount);
    }
}
