using InstallerClean.Helpers;
using InstallerClean.Models;
using InstallerClean.Services;
using NSubstitute;

namespace InstallerClean.Tests.Services;

/// <summary>
/// Unit tests for <see cref="RemovableReverifier"/>: the action-time re-check
/// that drops a candidate a currently-registered, non-removable package now
/// claims (a superseded patch reverted to Applied between scan and click). The
/// query service is faked; its own correctness is covered by
/// <see cref="InstallerQueryServiceUnitTests"/> through the same IMsiApi seam.
/// </summary>
public class RemovableReverifierTests
{
    private static RegisteredPackage NonRemovable(string path) =>
        new(path, "Product", "{00000000-0000-0000-0000-000000000001}");

    private static RegisteredPackage Removable(string path) =>
        new(path, "Product", "{00000000-0000-0000-0000-000000000001}", PatchState: 2, IsRemovable: true);

    /// <summary>A superseded patch on a query whose product enumeration was incomplete.</summary>
    private static RegisteredPackage Withheld(string path) =>
        new(path, "Product", "{00000000-0000-0000-0000-000000000001}",
            PatchState: 2, IsRemovable: false, RemovableWithheld: true);

    /// <summary>
    /// A patch row the re-verify's own enumeration produced with its State or
    /// Uninstallable read failed. Non-removable like the row above and for a
    /// different reason: that one is a verdict this scan would not act on, this
    /// one is no verdict at all. PatchState stays 0 because an unreadable State
    /// is what leaves it there.
    /// </summary>
    private static RegisteredPackage VerdictUnread(string path) =>
        new(path, "Product", "{00000000-0000-0000-0000-000000000001}",
            IsRemovable: false, VerdictUnreadable: true);

    /// <summary>
    /// The pre-lease pass only. Its <c>IMsiApi</c> is a bare substitute because
    /// nothing on this path touches it: the under-lease re-read is the only
    /// caller, and it has tests of its own below that drive the API deliberately.
    /// </summary>
    private static RemovableReverifier Reverifier(IInstallerQueryService query) =>
        new(query, Substitute.For<InstallerClean.Interop.IMsiApi>());

    private static IInstallerQueryService Query(params RegisteredPackage[] pkgs)
    {
        var q = Substitute.For<IInstallerQueryService>();
        q.GetRegisteredPackagesAsync(Arg.Any<IProgress<ScanProgressUpdate>?>(), Arg.Any<CancellationToken>())
            .Returns(new InstallerQueryResult(pkgs.ToList().AsReadOnly()));
        return q;
    }

    [Fact]
    public async Task Drops_a_candidate_that_reverted_to_non_removable()
    {
        const string reverted = @"C:\Windows\Installer\reverted.msp";
        const string orphan = @"C:\Windows\Installer\orphan.msi";
        // At action time the reverted patch is now a registered, non-removable package.
        var svc = Reverifier(Query(NonRemovable(reverted)));

        var result = await svc.ReverifyAsync(new[] { reverted, orphan });

        Assert.Equal(new[] { orphan }, result.Surviving);
        Assert.Equal(new[] { reverted }, result.Dropped);
    }

    [Fact]
    public async Task Drops_a_candidate_a_superseded_registration_names()
    {
        // EVERY REGISTERED PATH IS IN THE NON-REMOVABLE MAP, so a candidate any
        // registration names is dropped with a cause, superseded ones included. No
        // enumeration grants the removable verdict, so nothing is excluded from that
        // map by construction.
        const string superseded = @"C:\Windows\Installer\superseded.msp";
        var svc = Reverifier(Query(NonRemovable(superseded) with { PatchState = 2 }));

        var result = await svc.ReverifyAsync(new[] { superseded });

        Assert.Empty(result.Surviving);
        Assert.Equal(new[] { superseded }, result.Dropped);
    }

    [Fact]
    public async Task Keeps_a_candidate_no_registration_names_at_all()
    {
        // The direction that must not move with it. A true orphan is a candidate
        // precisely because nothing names its path, so nothing in the re-verify's
        // map can match it and it survives to be acted on.
        const string orphan = @"C:\Windows\Installer\orphan.msi";
        var svc = Reverifier(Query(NonRemovable(@"C:\Windows\Installer\something-else.msi")));

        var result = await svc.ReverifyAsync(new[] { orphan });

        Assert.Equal(new[] { orphan }, result.Surviving);
        Assert.Empty(result.Dropped);
    }

    [Fact]
    public async Task Drops_a_scan_time_orphan_a_new_registration_has_since_claimed()
    {
        // Same drop mechanism as the reverted-patch test above, pinned separately
        // because it stands for a different producer and the interface's contract
        // now names it. An install that cached its package before the folder walk
        // reached it and registered it after the query had passed leaves a file
        // the scan measured as an orphan and a program owns by action time. The
        // re-verify catching it is what keeps it out of a permanent delete.
        const string newlyClaimed = @"C:\Windows\Installer\just-installed.msi";
        var svc = Reverifier(Query(NonRemovable(newlyClaimed)));

        var result = await svc.ReverifyAsync(new[] { newlyClaimed });

        Assert.Empty(result.Surviving);
        Assert.Equal(new[] { newlyClaimed }, result.Dropped);
    }

    [Fact]
    public async Task Keeps_a_true_orphan_not_in_the_registered_set()
    {
        const string orphan = @"C:\Windows\Installer\orphan.msi";
        var svc = Reverifier(Query(NonRemovable(@"C:\Windows\Installer\unrelated.msi")));

        var result = await svc.ReverifyAsync(new[] { orphan });

        Assert.Equal(new[] { orphan }, result.Surviving);
        Assert.Empty(result.Dropped);
    }

    [Fact]
    public async Task Path_match_is_case_insensitive()
    {
        const string candidate = @"C:\Windows\Installer\Patch.msp";
        var svc = Reverifier(Query(NonRemovable(@"c:\windows\installer\patch.msp")));

        var result = await svc.ReverifyAsync(new[] { candidate });

        Assert.Empty(result.Surviving);
        Assert.Equal(new[] { candidate }, result.Dropped);
    }

    [Fact]
    public async Task Empty_candidate_list_returns_empty_without_querying()
    {
        var q = Substitute.For<IInstallerQueryService>();
        var svc = Reverifier(q);

        var result = await svc.ReverifyAsync(Array.Empty<string>());

        Assert.Empty(result.Surviving);
        Assert.Empty(result.Dropped);
        await q.DidNotReceive().GetRegisteredPackagesAsync(
            Arg.Any<IProgress<ScanProgressUpdate>?>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task A_degraded_reverify_keeps_a_superseded_candidate_back_and_says_why()
    {
        // The re-verify is the same query, so it inherits the withholding: a
        // patch it cannot judge is kept in place rather than acted on. It reports
        // WHY, because the only other reason it drops anything is a program
        // reclaiming the file, and that is not what happened here.
        const string superseded = @"C:\Windows\Installer\superseded.msp";
        var q = Substitute.For<IInstallerQueryService>();
        q.GetRegisteredPackagesAsync(Arg.Any<IProgress<ScanProgressUpdate>?>(), Arg.Any<CancellationToken>())
            .Returns(new InstallerQueryResult(
                new[] { Withheld(superseded) }, UnaccountedProductCount: 1));
        var svc = Reverifier(q);

        var result = await svc.ReverifyAsync(new[] { superseded });

        Assert.Empty(result.Surviving);
        Assert.Equal(new[] { superseded }, result.Dropped);
        Assert.Equal(new HeldBackReasons(RecordsUnreadable: 1), result.Reasons);
    }

    [Fact]
    public async Task A_healthy_reverify_does_not_claim_the_records_were_short()
    {
        var svc = Reverifier(Query(NonRemovable(@"C:\Windows\Installer\reverted.msp")));

        var result = await svc.ReverifyAsync(new[] { @"C:\Windows\Installer\reverted.msp" });

        Assert.Equal(new HeldBackReasons(Reclaimed: 1), result.Reasons);
    }

    [Fact]
    public async Task A_candidate_whose_verdict_would_not_read_is_not_reported_as_a_claim()
    {
        // The row is non-removable, so the file is kept either way and the drop
        // itself is not in question. What is in question is the sentence: the
        // reclaim cause means a registered product's live claim NAMES the file,
        // and a State or Uninstallable read that failed names nothing. Reported
        // as a reclaim, the user is told a program wants the file back on the
        // strength of a question nobody got an answer to.
        const string unread = @"C:\Windows\Installer\unread-verdict.msp";
        var svc = Reverifier(Query(VerdictUnread(unread)));

        var result = await svc.ReverifyAsync(new[] { unread });

        Assert.Empty(result.Surviving);
        Assert.Equal(new[] { unread }, result.Dropped);
        Assert.Equal(new HeldBackReasons(RecordsUnreadable: 1), result.Reasons);
    }

    [Fact]
    public async Task One_batch_can_carry_a_claim_and_an_unread_verdict_at_once()
    {
        // The two rows come from one enumeration and are different findings, so
        // one sentence for the batch is false of one of them whichever sentence
        // is picked. Three causes rather than two makes the point sharper than
        // the withheld pairing does: the withheld row was positively read as
        // removable, this one was not read at all, and both are counted apart
        // from the file a product really does claim.
        const string reclaimed = @"C:\Windows\Installer\reverted.msp";
        const string unread = @"C:\Windows\Installer\unread-verdict.msp";
        var svc = Reverifier(Query(NonRemovable(reclaimed), VerdictUnread(unread)));

        var result = await svc.ReverifyAsync(new[] { reclaimed, unread });

        Assert.Equal(new[] { reclaimed, unread }, result.Dropped);
        Assert.Equal(new HeldBackReasons(Reclaimed: 1, RecordsUnreadable: 1), result.Reasons);
        Assert.Equal(result.Dropped.Count, result.Reasons.Total);
    }

    [Fact]
    public async Task A_degraded_reverify_names_each_dropped_files_own_cause()
    {
        // One enumeration, two causes, and before the cause was carried per path
        // the withholding spoke for the whole batch: a file a live registered
        // product genuinely claims was reported as one whose records could not be
        // read. Withholding moves ONLY rows that were removable
        // (InstallerQueryService's withhold loop), so the row that never was
        // separates itself.
        const string reclaimed = @"C:\Windows\Installer\reverted.msp";
        const string withheld = @"C:\Windows\Installer\superseded.msp";
        var q = Substitute.For<IInstallerQueryService>();
        q.GetRegisteredPackagesAsync(Arg.Any<IProgress<ScanProgressUpdate>?>(), Arg.Any<CancellationToken>())
            .Returns(new InstallerQueryResult(
                new[] { NonRemovable(reclaimed), Withheld(withheld) }, UnaccountedProductCount: 1));
        var svc = Reverifier(q);

        var result = await svc.ReverifyAsync(new[] { reclaimed, withheld });

        Assert.Equal(new[] { reclaimed, withheld }, result.Dropped);
        Assert.Equal(new HeldBackReasons(Reclaimed: 1, RecordsUnreadable: 1), result.Reasons);
    }

    [Fact]
    public async Task Every_dropped_path_is_counted_against_exactly_one_cause()
    {
        // The tally and the list are built at the same point and have to agree, or
        // a screen reports fewer files than the batch lost.
        const string reclaimed = @"C:\Windows\Installer\reverted.msp";
        const string withheld = @"C:\Windows\Installer\superseded.msp";
        const string orphan = @"C:\Windows\Installer\orphan.msi";
        var q = Substitute.For<IInstallerQueryService>();
        q.GetRegisteredPackagesAsync(Arg.Any<IProgress<ScanProgressUpdate>?>(), Arg.Any<CancellationToken>())
            .Returns(new InstallerQueryResult(
                new[] { NonRemovable(reclaimed), Withheld(withheld) }, UnaccountedProductCount: 1));
        var svc = Reverifier(q);

        var result = await svc.ReverifyAsync(new[] { reclaimed, withheld, orphan });

        Assert.Equal(result.Dropped.Count, result.Reasons.Total);
    }

    [Fact]
    public async Task A_degraded_reverify_still_leaves_a_true_orphan_actionable()
    {
        // Withholding costs superseded-patch cleanup and nothing else: an orphan
        // was never in the registered set, so no enumeration failure can put it
        // there.
        const string orphan = @"C:\Windows\Installer\orphan.msi";
        var q = Substitute.For<IInstallerQueryService>();
        q.GetRegisteredPackagesAsync(Arg.Any<IProgress<ScanProgressUpdate>?>(), Arg.Any<CancellationToken>())
            .Returns(new InstallerQueryResult(
                Array.Empty<RegisteredPackage>(), UnaccountedProductCount: 3));
        var svc = Reverifier(q);

        var result = await svc.ReverifyAsync(new[] { orphan });

        Assert.Equal(new[] { orphan }, result.Surviving);
        Assert.Empty(result.Dropped);
    }

    [Fact]
    public async Task An_enumeration_failure_propagates_rather_than_passing_the_batch()
    {
        var q = Substitute.For<IInstallerQueryService>();
        q.GetRegisteredPackagesAsync(Arg.Any<IProgress<ScanProgressUpdate>?>(), Arg.Any<CancellationToken>())
            .Returns<InstallerQueryResult>(_ => throw new LocalisedInvalidOperationException("boom"));
        var svc = Reverifier(q);

        await Assert.ThrowsAsync<LocalisedInvalidOperationException>(
            () => svc.ReverifyAsync(new[] { @"C:\Windows\Installer\x.msi" }));
    }

    // ---- The in-progress signals' read, handed back to the host ----------
    //
    // These build the pass with the signals' reader and none of the file readers, so
    // no file is opened and the age check does not run: what is held is the read on
    // the result, which is what each host stops the batch on.

    private static RemovableReverifier ReadingSignals(IInstallerQueryService query, IPendingRebootService signals) =>
        new(query, Substitute.For<InstallerClean.Interop.IMsiApi>(), null, null, null, null, null, signals);

    private static IPendingRebootService Signals(PendingRebootResult answer)
    {
        var signals = Substitute.For<IPendingRebootService>();
        signals.Check().Returns(answer);
        return signals;
    }

    [Fact]
    public async Task A_blocked_signal_read_is_handed_back_beside_a_superseded_patch_the_pass_confirms()
    {
        // The batch's superseded patch is judged by its own registration, which is still
        // removable, so the pass confirms it whatever the signals say. The read on the
        // result is what keeps it from the Move or Delete.
        const string superseded = @"C:\Windows\Installer\superseded.msp";
        var blocked = PendingRebootResult.Block(PendingRebootReason.InstallerInProgressMarker);
        var svc = ReadingSignals(Query(Removable(superseded)), Signals(blocked));

        var result = await svc.ReverifyAsync(new[] { superseded });

        Assert.Equal(new[] { superseded }, result.Surviving);
        Assert.Same(blocked, result.InstallerActivity);
    }

    [Fact]
    public async Task A_clean_signal_read_is_handed_back_as_clean()
    {
        const string superseded = @"C:\Windows\Installer\superseded.msp";
        var svc = ReadingSignals(Query(Removable(superseded)), Signals(PendingRebootResult.Clean));

        var result = await svc.ReverifyAsync(new[] { superseded });

        Assert.Equal(new[] { superseded }, result.Surviving);
        Assert.NotNull(result.InstallerActivity);
        Assert.False(result.InstallerActivity!.IsBlocked);
    }

    [Fact]
    public async Task A_pass_reading_no_signals_hands_back_no_read()
    {
        const string superseded = @"C:\Windows\Installer\superseded.msp";
        var svc = Reverifier(Query(Removable(superseded)));

        var result = await svc.ReverifyAsync(new[] { superseded });

        Assert.Null(result.InstallerActivity);
    }

    // ---- The under-lease re-read ----------------------------------------
    //
    // The pre-lease pass above answers with a whole enumeration. This one
    // answers about named claims, because it runs while the machine-wide
    // installer mutex is held and an enumeration must not.

    private const string PatchA = "{AAAAAAAA-0000-0000-0000-000000000001}";
    private const string PatchB = "{BBBBBBBB-0000-0000-0000-000000000002}";
    private const string PatchC = "{CCCCCCCC-0000-0000-0000-000000000003}";
    private const string ProductOne = "{11111111-0000-0000-0000-000000000001}";
    private const string ProductTwo = "{22222222-0000-0000-0000-000000000002}";

    private static PatchClaim Claim(string path, string patchCode, string productCode) =>
        new(path, patchCode, productCode, null, 4);

    // ---- The per-product condition, re-read under the lease ----
    //
    // The offer now rests on a fact about OTHER patches: a superseded patch is offered
    // only where nothing on any product sharing it can be uninstalled and roll back
    // onto its file. So the re-read has to re-ask about the siblings, not only about
    // the batch's own pairings, or it does not re-check the fact the offer rests on.

    // ---- Reads made again under the lease where one fails ----

    private static (UnderLeaseRecheck Recheck, List<TimeSpan> Pauses) RecheckRecordingPauses(
        RemovableReverifier svc, UnderLeaseClaims claims)
    {
        var pauses = new List<TimeSpan>();
        var forTests = WalkRestarts.PauseForTests;
        WalkRestarts.PauseForTests = pauses.Add;
        try
        {
            return (svc.RecheckUnderLease(claims), pauses);
        }
        finally
        {
            WalkRestarts.PauseForTests = forTests;
        }
    }

    [Theory]
    [InlineData(PatchA, "State")]
    [InlineData(PatchA, "Uninstallable")]
    [InlineData(PatchB, "Uninstallable")]
    public void A_read_under_the_lease_that_fails_once_lets_the_batch_through_as_a_clean_read_does(
        string patchCode, string property)
    {
        // The batch's own pairing and a sibling on its product, both superseded and not
        // removable, which passes; one read fails once.
        const string path = @"C:\Windows\Installer\superseded.msp";
        var msi = new ScriptedPatchApi();
        msi.Set(PatchA, ProductOne, state: "2", uninstallable: "0");
        msi.Set(PatchB, ProductOne, state: "2", uninstallable: "0");
        msi.FailPropertyOnce(patchCode, ProductOne, property);
        var svc = new RemovableReverifier(Substitute.For<IInstallerQueryService>(), msi);

        var (recheck, pauses) = RecheckRecordingPauses(svc, new UnderLeaseClaims(
            new[] { Claim(path, PatchA, ProductOne) },
            new[] { Claim(path, PatchA, ProductOne), Claim(@"C:\Windows\Installer\other.msp", PatchB, ProductOne) }));

        Assert.Empty(recheck.HeldBack);
        Assert.Equal(new[] { TimeSpan.FromMilliseconds(50), TimeSpan.FromMilliseconds(250) }, pauses);
    }

    [Fact]
    public void A_read_under_the_lease_that_keeps_failing_holds_the_path_back_and_pauses_no_longer_than_the_limit()
    {
        const string path = @"C:\Windows\Installer\superseded.msp";
        var msi = new ScriptedPatchApi();
        msi.Set(PatchA, ProductOne, state: "2", uninstallable: "0");
        msi.Set(PatchB, ProductOne, state: "2", uninstallable: "0");
        msi.FailProperty(PatchA, ProductOne, "State");
        msi.FailProperty(PatchB, ProductOne, "Uninstallable");
        var svc = new RemovableReverifier(Substitute.For<IInstallerQueryService>(), msi);

        var (recheck, pauses) = RecheckRecordingPauses(svc, new UnderLeaseClaims(
            new[] { Claim(path, PatchA, ProductOne) },
            new[] { Claim(path, PatchA, ProductOne), Claim(@"C:\Windows\Installer\other.msp", PatchB, ProductOne) }));

        // The first failing read is made again once, after the short pause; the long pause a
        // second read would need, and the confirming one, are past the limit.
        Assert.Equal(new[] { path }, recheck.HeldBack);
        Assert.Equal(new[] { TimeSpan.FromMilliseconds(50) }, pauses);
    }

    [Theory]
    [InlineData(1605u)]
    public void A_record_under_the_lease_answering_that_it_has_gone_is_read_once_and_holds_the_path_back(uint code)
    {
        const string path = @"C:\Windows\Installer\superseded.msp";
        var msi = new ScriptedPatchApi();
        msi.Set(PatchA, ProductOne, state: "2", uninstallable: "0");
        msi.AbsentRecord(PatchA, ProductOne, "State", code);
        var svc = new RemovableReverifier(Substitute.For<IInstallerQueryService>(), msi);

        var (recheck, pauses) = RecheckRecordingPauses(svc, new UnderLeaseClaims(
            new[] { Claim(path, PatchA, ProductOne) }, new[] { Claim(path, PatchA, ProductOne) }));

        Assert.Equal(new[] { path }, recheck.HeldBack);
        Assert.Empty(pauses);
    }

    [Fact]
    public void A_sibling_patch_turning_removable_holds_the_batch_path_back()
    {
        // THE CASE THE SIBLING PASS EXISTS FOR. The batch's own pairing is unchanged and
        // still passes on its own answer; what moved is another patch on the same
        // product, which can now be uninstalled and roll the product back onto this
        // file. Without the sibling pass this batch would proceed.
        const string path = @"C:\Windows\Installer\superseded.msp";
        var msi = new ScriptedPatchApi();
        msi.Set(PatchA, ProductOne, state: "2", uninstallable: "0");
        msi.Set(PatchB, ProductOne, state: "2", uninstallable: "1");
        var svc = new RemovableReverifier(Substitute.For<IInstallerQueryService>(), msi);

        var recheck = svc.RecheckUnderLease(new UnderLeaseClaims(
            new[] { Claim(path, PatchA, ProductOne) },
            new[] { Claim(path, PatchA, ProductOne), Claim(@"C:\Windows\Installer\other.msp", PatchB, ProductOne) }));

        Assert.Equal(new[] { path }, recheck.HeldBack);
        Assert.Equal(1, recheck.Reasons.Reclaimed);
    }

    [Fact]
    public void A_sibling_on_a_DIFFERENT_product_does_not_hold_the_batch_path_back()
    {
        // THE MUST-MISS CONTROL FOR THE TEST ABOVE. Without it a sibling pass that held
        // back on any dirty patch anywhere would pass that test and empty every offer on
        // any machine with one removable patch. The condition is per product because the
        // file is only reachable from the products it is registered to.
        const string path = @"C:\Windows\Installer\superseded.msp";
        var msi = new ScriptedPatchApi();
        msi.Set(PatchA, ProductOne, state: "2", uninstallable: "0");
        msi.Set(PatchB, ProductTwo, state: "2", uninstallable: "1");
        var svc = new RemovableReverifier(Substitute.For<IInstallerQueryService>(), msi);

        var recheck = svc.RecheckUnderLease(new UnderLeaseClaims(
            new[] { Claim(path, PatchA, ProductOne) },
            new[] { Claim(path, PatchA, ProductOne), Claim(@"C:\Windows\Installer\other.msp", PatchB, ProductTwo) }));

        Assert.Empty(recheck.HeldBack);
    }

    [Fact]
    public void A_sibling_whose_Uninstallable_comes_back_empty_holds_the_batch_path_back_as_unread()
    {
        // Anything that is not a positive zero refuses. An answer that came back empty
        // is counted as the inability it is rather than as a claim: it does not say
        // whether the sibling can be uninstalled, so the app has not established that
        // nothing can roll back. The scan's reading of a product's patch set takes the
        // same answer as unestablished, and the pre-lease pass counts the file it holds
        // for that the same way.
        const string path = @"C:\Windows\Installer\superseded.msp";
        var msi = new ScriptedPatchApi();
        msi.Set(PatchA, ProductOne, state: "2", uninstallable: "0");
        msi.Set(PatchB, ProductOne, state: "2", uninstallable: "");
        var svc = new RemovableReverifier(Substitute.For<IInstallerQueryService>(), msi);

        var recheck = svc.RecheckUnderLease(new UnderLeaseClaims(
            new[] { Claim(path, PatchA, ProductOne) },
            new[] { Claim(path, PatchA, ProductOne), Claim(@"C:\Windows\Installer\other.msp", PatchB, ProductOne) }));

        Assert.Equal(new[] { path }, recheck.HeldBack);
        Assert.Equal(new HeldBackReasons(RecordsUnreadable: 1), recheck.Reasons);
    }

    [Fact]
    public void A_sibling_whose_Uninstallable_read_fails_holds_the_batch_path_back_as_unread()
    {
        // A read that fails with a code that is not an absence answer says nothing about
        // whether the sibling can be uninstalled, so the app has not established that
        // nothing can roll back onto the file. It is counted as the read that did not
        // answer.
        const string path = @"C:\Windows\Installer\superseded.msp";
        var msi = new ScriptedPatchApi();
        msi.Set(PatchA, ProductOne, state: "2", uninstallable: "0");
        msi.Set(PatchB, ProductOne, state: "2", uninstallable: "0");
        msi.FailProperty(PatchB, ProductOne, "Uninstallable");
        var svc = new RemovableReverifier(Substitute.For<IInstallerQueryService>(), msi);

        var recheck = svc.RecheckUnderLease(new UnderLeaseClaims(
            new[] { Claim(path, PatchA, ProductOne) },
            new[] { Claim(path, PatchA, ProductOne), Claim(@"C:\Windows\Installer\other.msp", PatchB, ProductOne) }));

        Assert.Equal(new[] { path }, recheck.HeldBack);
        Assert.Equal(new HeldBackReasons(RecordsUnreadable: 1), recheck.Reasons);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void A_sibling_that_can_be_uninstalled_outranks_one_whose_read_fails_whichever_is_read_first(
        bool claimReadFirst)
    {
        // Two other patches on the product: one's Uninstallable read fails, and one
        // answers that it can be uninstalled. A failed read does not end the reads on the
        // product, so the claim is still read and the path is counted on it in either
        // order.
        const string path = @"C:\Windows\Installer\superseded.msp";
        var msi = new ScriptedPatchApi();
        msi.Set(PatchA, ProductOne, state: "2", uninstallable: "0");
        msi.Set(PatchB, ProductOne, state: "2", uninstallable: "0");
        msi.FailProperty(PatchB, ProductOne, "Uninstallable");
        msi.Set(PatchC, ProductOne, state: "2", uninstallable: "1");
        var svc = new RemovableReverifier(Substitute.For<IInstallerQueryService>(), msi);

        var failing = Claim(@"C:\Windows\Installer\failing.msp", PatchB, ProductOne);
        var claiming = Claim(@"C:\Windows\Installer\claiming.msp", PatchC, ProductOne);
        var recheck = svc.RecheckUnderLease(new UnderLeaseClaims(
            new[] { Claim(path, PatchA, ProductOne) },
            claimReadFirst
                ? new[] { Claim(path, PatchA, ProductOne), claiming, failing }
                : new[] { Claim(path, PatchA, ProductOne), failing, claiming }));

        Assert.Equal(new[] { path }, recheck.HeldBack);
        Assert.Equal(new HeldBackReasons(Reclaimed: 1), recheck.Reasons);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void A_sibling_that_can_be_uninstalled_outranks_one_whose_answer_is_empty_whichever_is_read_first(
        bool claimReadFirst)
    {
        // Two other patches on the product: one answers its Uninstallable empty, which
        // establishes nothing, and one answers that it can be uninstalled, a live claim
        // on the rollback. The path is counted on the claim in either order.
        const string path = @"C:\Windows\Installer\superseded.msp";
        var msi = new ScriptedPatchApi();
        msi.Set(PatchA, ProductOne, state: "2", uninstallable: "0");
        msi.Set(PatchB, ProductOne, state: "2", uninstallable: "");
        msi.Set(PatchC, ProductOne, state: "2", uninstallable: "1");
        var svc = new RemovableReverifier(Substitute.For<IInstallerQueryService>(), msi);

        var empty = Claim(@"C:\Windows\Installer\empty.msp", PatchB, ProductOne);
        var claiming = Claim(@"C:\Windows\Installer\claiming.msp", PatchC, ProductOne);
        var recheck = svc.RecheckUnderLease(new UnderLeaseClaims(
            new[] { Claim(path, PatchA, ProductOne) },
            claimReadFirst
                ? new[] { Claim(path, PatchA, ProductOne), claiming, empty }
                : new[] { Claim(path, PatchA, ProductOne), empty, claiming }));

        Assert.Equal(new[] { path }, recheck.HeldBack);
        Assert.Equal(new HeldBackReasons(Reclaimed: 1), recheck.Reasons);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void A_path_on_two_products_is_counted_on_the_claim_whichever_product_is_read_first(
        bool claimingProductFirst)
    {
        // One cached patch held by two products, and each product has one other patch.
        // Product one's answers its Uninstallable empty and product two's answers that it
        // can be uninstalled. The rollback on product two reaches for the file, so the
        // path is counted on that claim in either order.
        const string path = @"C:\Windows\Installer\shared.msp";
        var msi = new ScriptedPatchApi();
        msi.Set(PatchA, ProductOne, state: "2", uninstallable: "0");
        msi.Set(PatchA, ProductTwo, state: "2", uninstallable: "0");
        msi.Set(PatchB, ProductOne, state: "2", uninstallable: "");
        msi.Set(PatchB, ProductTwo, state: "2", uninstallable: "1");
        var svc = new RemovableReverifier(Substitute.For<IInstallerQueryService>(), msi);

        var onOne = new[] { Claim(path, PatchA, ProductOne), Claim(@"C:\Windows\Installer\other.msp", PatchB, ProductOne) };
        var onTwo = new[] { Claim(path, PatchA, ProductTwo), Claim(@"C:\Windows\Installer\other.msp", PatchB, ProductTwo) };
        var recheck = svc.RecheckUnderLease(new UnderLeaseClaims(
            new[] { Claim(path, PatchA, ProductOne), Claim(path, PatchA, ProductTwo) },
            claimingProductFirst ? onTwo.Concat(onOne).ToArray() : onOne.Concat(onTwo).ToArray()));

        Assert.Equal(new[] { path }, recheck.HeldBack);
        Assert.Equal(new HeldBackReasons(Reclaimed: 1), recheck.Reasons);
    }

    [Fact]
    public void A_sibling_answering_that_its_product_is_not_installed_holds_the_batch_path_back()
    {
        // The batch's own pairing on product one has just been re-read and answered, so
        // the product is there, and a sibling read on it answering that the product is
        // not installed contradicts that re-read. It condemns, and it is counted as the
        // read that did not answer rather than as a claim.
        const string path = @"C:\Windows\Installer\superseded.msp";
        var msi = new ScriptedPatchApi();
        msi.Set(PatchA, ProductOne, state: "2", uninstallable: "0");
        msi.Set(PatchB, ProductOne, state: "2", uninstallable: "0");
        msi.AbsentRecord(PatchB, ProductOne, "Uninstallable", code: 1605);
        var svc = new RemovableReverifier(Substitute.For<IInstallerQueryService>(), msi);

        var recheck = svc.RecheckUnderLease(new UnderLeaseClaims(
            new[] { Claim(path, PatchA, ProductOne) },
            new[] { Claim(path, PatchA, ProductOne), Claim(@"C:\Windows\Installer\other.msp", PatchB, ProductOne) }));

        Assert.Equal(new[] { path }, recheck.HeldBack);
        Assert.Equal(new HeldBackReasons(RecordsUnreadable: 1), recheck.Reasons);
    }

    [Fact]
    public void A_sibling_answering_that_the_product_holds_no_record_of_it_holds_the_batch_path_back()
    {
        // The sibling pairing was listed by the enumeration the pre-lease re-verify ran
        // moments before, and under the lease it answers that the product holds no record
        // of that patch. The two answers contradict each other, so it condemns, counted
        // as the read that did not answer. The batch's own pairing still passes on its
        // own answer, so the sibling read is what holds the path back.
        const string path = @"C:\Windows\Installer\superseded.msp";
        var msi = new ScriptedPatchApi();
        msi.Set(PatchA, ProductOne, state: "2", uninstallable: "0");
        msi.Set(PatchB, ProductOne, state: "2", uninstallable: "0");
        msi.AbsentRecord(PatchB, ProductOne, "Uninstallable", code: 1647);
        var svc = new RemovableReverifier(Substitute.For<IInstallerQueryService>(), msi);

        var recheck = svc.RecheckUnderLease(new UnderLeaseClaims(
            new[] { Claim(path, PatchA, ProductOne) },
            new[] { Claim(path, PatchA, ProductOne), Claim(@"C:\Windows\Installer\other.msp", PatchB, ProductOne) }));

        Assert.Equal(new[] { path }, recheck.HeldBack);
        Assert.Equal(new HeldBackReasons(RecordsUnreadable: 1), recheck.Reasons);
    }

    [Fact]
    public void An_empty_sibling_list_is_a_caller_saying_so_rather_than_an_accident()
    {
        // THIS TEST REPLACED A GUARD AND THE HISTORY IS THE POINT. The two lists first
        // shipped as two arguments, where a caller could pass the batch and forget the
        // siblings and silently receive the weaker check, so the re-read detected the
        // mismatch and refused the batch. That was the wrong answer twice over: it could
        // only fire on a programming error, and refusing puts a sentence on somebody's
        // screen which would have had to name a cause about their machine that had not
        // occurred. One argument removes the mistake instead of detecting it.
        //
        // So an empty sibling half is now a caller stating there are none, and the
        // batch's own pairing decides it alone. What pins the wire instead is that the
        // production callers hand over the pre-lease result whole, asserted in
        // MainViewModelTests.
        const string path = @"C:\Windows\Installer\superseded.msp";
        var msi = new ScriptedPatchApi();
        msi.Set(PatchA, ProductOne, state: "2", uninstallable: "0");
        var svc = new RemovableReverifier(Substitute.For<IInstallerQueryService>(), msi);

        var recheck = svc.RecheckUnderLease(new UnderLeaseClaims(
            new[] { Claim(path, PatchA, ProductOne) },
            Array.Empty<PatchClaim>()));

        Assert.Empty(recheck.HeldBack);
    }

    [Fact]
    public void An_empty_claim_list_answers_without_touching_the_api()
    {
        var msi = new ScriptedPatchApi();
        var svc = new RemovableReverifier(Substitute.For<IInstallerQueryService>(), msi);

        Assert.Empty(svc.RecheckUnderLease(new UnderLeaseClaims(Array.Empty<PatchClaim>(), Array.Empty<PatchClaim>())).HeldBack);
        // The ordinary batch is true orphans, which carry no claim at all, so the
        // machine-wide lock is held over no API call whatsoever on most runs.
        Assert.Equal(0, msi.Reads);
    }

    [Fact]
    public void A_claim_still_superseded_and_not_uninstallable_is_left_alone()
    {
        const string path = @"C:\Windows\Installer\superseded.msp";
        var msi = new ScriptedPatchApi();
        msi.Set(PatchA, ProductOne, state: "2", uninstallable: "0");
        var svc = new RemovableReverifier(Substitute.For<IInstallerQueryService>(), msi);

        Assert.Empty(svc.RecheckUnderLease(new UnderLeaseClaims(new[] { Claim(path, PatchA, ProductOne) }, new[] { Claim(path, PatchA, ProductOne) })).HeldBack);
    }

    [Fact]
    public void A_claim_that_reverted_to_applied_reclaims_its_path()
    {
        // The producer the whole check exists for: the superseding patch was
        // uninstalled after the caller's enumeration read this one as Superseded,
        // so it is Applied again and its cached .msp is needed.
        const string path = @"C:\Windows\Installer\reverted.msp";
        var msi = new ScriptedPatchApi();
        msi.Set(PatchA, ProductOne, state: "1", uninstallable: "0");
        var svc = new RemovableReverifier(Substitute.For<IInstallerQueryService>(), msi);

        Assert.Equal(new[] { path }, svc.RecheckUnderLease(new UnderLeaseClaims(new[] { Claim(path, PatchA, ProductOne) }, new[] { Claim(path, PatchA, ProductOne) })).HeldBack);
    }

    [Fact]
    public void A_superseded_claim_that_can_still_be_uninstalled_reclaims_its_path()
    {
        // Superseded is not sufficient on its own: a superseded patch that can
        // still be uninstalled needs its .msp to roll back with.
        const string path = @"C:\Windows\Installer\rollbackable.msp";
        var msi = new ScriptedPatchApi();
        msi.Set(PatchA, ProductOne, state: "2", uninstallable: "1");
        var svc = new RemovableReverifier(Substitute.For<IInstallerQueryService>(), msi);

        Assert.Equal(new[] { path }, svc.RecheckUnderLease(new UnderLeaseClaims(new[] { Claim(path, PatchA, ProductOne) }, new[] { Claim(path, PatchA, ProductOne) })).HeldBack);
    }

    [Fact]
    public void A_property_that_cannot_be_read_reclaims_its_path()
    {
        // Fails closed. A read that could not be made has not shown the file to
        // be removable, and this is the last check in front of a permanent delete.
        const string path = @"C:\Windows\Installer\unreadable.msp";
        var msi = new ScriptedPatchApi();
        msi.Set(PatchA, ProductOne, state: "2", uninstallable: "0");
        msi.FailProperty(PatchA, ProductOne, "State");
        var svc = new RemovableReverifier(Substitute.For<IInstallerQueryService>(), msi);

        var recheck = svc.RecheckUnderLease(new UnderLeaseClaims(new[] { Claim(path, PatchA, ProductOne) }, new[] { Claim(path, PatchA, ProductOne) }));

        Assert.Equal(new[] { path }, recheck.HeldBack);
        // And it says WHY, because the user is shown a cause and this is not the
        // same cause as a program having taken the file back. The completion copy
        // carries a separate sentence for exactly this.
        Assert.Equal(new HeldBackReasons(RecordsUnreadable: 1), recheck.Reasons);
    }

    [Fact]
    public void A_verdict_that_moved_does_not_claim_the_records_were_short()
    {
        const string path = @"C:\Windows\Installer\reverted.msp";
        var msi = new ScriptedPatchApi();
        msi.Set(PatchA, ProductOne, state: "1", uninstallable: "0");
        var svc = new RemovableReverifier(Substitute.For<IInstallerQueryService>(), msi);

        var recheck = svc.RecheckUnderLease(new UnderLeaseClaims(new[] { Claim(path, PatchA, ProductOne) }, new[] { Claim(path, PatchA, ProductOne) }));

        Assert.Equal(new[] { path }, recheck.HeldBack);
        Assert.Equal(new HeldBackReasons(Reclaimed: 1), recheck.Reasons);
    }

    [Fact]
    public void A_pairing_the_records_no_longer_hold_is_kept_back_rather_than_released()
    {
        // The fail-open window this whole check used to carry: an absence answer
        // condemned nothing, so the file went into the operation on the strength of
        // it. The absence code means "no such product in the account and context
        // you asked in", and the context a claim carries was settled at scan time,
        // so a pairing that has moved context since answers absent while its
        // registration is live.
        const string path = @"C:\Windows\Installer\moved.msp";
        var msi = new ScriptedPatchApi();
        msi.Set(PatchA, ProductOne, state: "2", uninstallable: "0");
        msi.AbsentRecord(PatchA, ProductOne, "State");
        var svc = new RemovableReverifier(Substitute.For<IInstallerQueryService>(), msi);

        var recheck = svc.RecheckUnderLease(new UnderLeaseClaims(new[] { Claim(path, PatchA, ProductOne) }, new[] { Claim(path, PatchA, ProductOne) }));

        Assert.Equal(new[] { path }, recheck.HeldBack);
        // And it is its own cause. A read that succeeded is not an unreadable
        // record, and a registration that has gone cannot have gone back to being
        // needed, so either of the other two sentences would be false here.
        Assert.Equal(new HeldBackReasons(RecordsChanged: 1), recheck.Reasons);
    }

    [Fact]
    public void A_patch_no_longer_applied_to_its_product_is_the_same_absence()
    {
        // The second member of the absence allowlist. MsiGetPatchInfoEx answers
        // 1647 where the product is there and the patch is not, and the claim is
        // the pairing, so nothing about the file's fate changes.
        const string path = @"C:\Windows\Installer\unapplied.msp";
        var msi = new ScriptedPatchApi();
        msi.Set(PatchA, ProductOne, state: "2", uninstallable: "0");
        msi.AbsentRecord(PatchA, ProductOne, "Uninstallable", code: 1647);
        var svc = new RemovableReverifier(Substitute.For<IInstallerQueryService>(), msi);

        var recheck = svc.RecheckUnderLease(new UnderLeaseClaims(new[] { Claim(path, PatchA, ProductOne) }, new[] { Claim(path, PatchA, ProductOne) }));

        Assert.Equal(new[] { path }, recheck.HeldBack);
        Assert.Equal(new HeldBackReasons(RecordsChanged: 1), recheck.Reasons);
    }

    [Fact]
    public void An_absence_beside_a_failed_read_is_reported_as_the_failure_it_contains()
    {
        // One pairing, two reads an instant apart, and they disagree about what
        // kind of answer this is. The one that failed is the honest thing to say:
        // something here could not be read, whatever the other property managed.
        // Calling it a changed record would claim a clean read that half of this
        // did not get.
        const string path = @"C:\Windows\Installer\mixed.msp";
        var msi = new ScriptedPatchApi();
        msi.Set(PatchA, ProductOne, state: "2", uninstallable: "0");
        msi.AbsentRecord(PatchA, ProductOne, "State");
        msi.FailProperty(PatchA, ProductOne, "Uninstallable");
        var svc = new RemovableReverifier(Substitute.For<IInstallerQueryService>(), msi);

        var recheck = svc.RecheckUnderLease(new UnderLeaseClaims(new[] { Claim(path, PatchA, ProductOne) }, new[] { Claim(path, PatchA, ProductOne) }));

        Assert.Equal(new[] { path }, recheck.HeldBack);
        Assert.Equal(new HeldBackReasons(RecordsUnreadable: 1), recheck.Reasons);
    }

    [Theory]
    // An empty Uninstallable beside a superseded State, and an empty State, leave the
    // pairing's verdict unestablished.
    [InlineData("2", "", HeldBackReason.RecordsUnreadable)]
    [InlineData("", "0", HeldBackReason.RecordsUnreadable)]
    // Beside an applied or obsoleted State an empty Uninstallable decides nothing, and
    // the pairing is a claim.
    [InlineData("1", "", HeldBackReason.Reclaimed)]
    [InlineData("4", "", HeldBackReason.Reclaimed)]
    public void An_empty_answer_about_the_batch_pairing_is_counted_as_unread_where_the_verdict_turns_on_it(
        string state, string uninstallable, HeldBackReason expected)
    {
        const string path = @"C:\Windows\Installer\empty.msp";
        var msi = new ScriptedPatchApi();
        msi.Set(PatchA, ProductOne, state, uninstallable);
        var svc = new RemovableReverifier(Substitute.For<IInstallerQueryService>(), msi);

        var recheck = svc.RecheckUnderLease(new UnderLeaseClaims(new[] { Claim(path, PatchA, ProductOne) }, new[] { Claim(path, PatchA, ProductOne) }));

        Assert.Equal(new[] { path }, recheck.HeldBack);
        Assert.Equal(default(HeldBackReasons).Plus(expected), recheck.Reasons);
    }

    [Theory]
    [InlineData("failed", "applied", HeldBackReason.Reclaimed)]
    [InlineData("applied", "failed", HeldBackReason.Reclaimed)]
    [InlineData("empty", "applied", HeldBackReason.Reclaimed)]
    [InlineData("applied", "empty", HeldBackReason.Reclaimed)]
    [InlineData("gone", "applied", HeldBackReason.Reclaimed)]
    [InlineData("applied", "gone", HeldBackReason.Reclaimed)]
    [InlineData("failed", "gone", HeldBackReason.RecordsChanged)]
    [InlineData("gone", "failed", HeldBackReason.RecordsChanged)]
    public void A_path_whose_claims_answer_differently_is_counted_on_the_strongest_whichever_is_read_first(
        string first, string second, HeldBackReason expected)
    {
        // One cached patch held by two products, whose pairings answer differently. A
        // live claim outranks a registration that has gone, which outranks a read that
        // did not answer, and the order the claims are read in decides nothing.
        const string shared = @"C:\Windows\Installer\shared.msp";
        var msi = new ScriptedPatchApi();
        Answer(msi, ProductOne, first);
        Answer(msi, ProductTwo, second);
        var svc = new RemovableReverifier(Substitute.For<IInstallerQueryService>(), msi);

        var claims = new[] { Claim(shared, PatchA, ProductOne), Claim(shared, PatchA, ProductTwo) };
        var recheck = svc.RecheckUnderLease(new UnderLeaseClaims(claims, claims));

        Assert.Equal(new[] { shared }, recheck.HeldBack);
        Assert.Equal(default(HeldBackReasons).Plus(expected), recheck.Reasons);

        static void Answer(ScriptedPatchApi msi, string product, string answer)
        {
            switch (answer)
            {
                case "applied":
                    msi.Set(PatchA, product, state: "1", uninstallable: "0");
                    break;
                case "empty":
                    msi.Set(PatchA, product, state: "2", uninstallable: "");
                    break;
                case "failed":
                    msi.Set(PatchA, product, state: "2", uninstallable: "0");
                    msi.FailProperty(PatchA, product, "State");
                    break;
                case "gone":
                    msi.Set(PatchA, product, state: "2", uninstallable: "0");
                    msi.AbsentRecord(PatchA, product, "State");
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(answer), answer, null);
            }
        }
    }

    [Fact]
    public void Claims_that_answered_differently_are_counted_against_their_own_causes()
    {
        // A mixed batch under one lease, which is what the tally exists for. Three
        // paths, three causes, and no cause standing in for another.
        const string reverted = @"C:\Windows\Installer\reverted.msp";
        const string gone = @"C:\Windows\Installer\gone.msp";
        const string unreadable = @"C:\Windows\Installer\unreadable.msp";
        var msi = new ScriptedPatchApi();
        msi.Set(PatchA, ProductOne, state: "1", uninstallable: "0");
        msi.Set(PatchB, ProductOne, state: "2", uninstallable: "0");
        msi.AbsentRecord(PatchB, ProductOne, "State");
        msi.Set(PatchA, ProductTwo, state: "2", uninstallable: "0");
        msi.FailProperty(PatchA, ProductTwo, "State");
        var svc = new RemovableReverifier(Substitute.For<IInstallerQueryService>(), msi);

        var recheck = svc.RecheckUnderLease(new UnderLeaseClaims(new[]
        {
            Claim(reverted, PatchA, ProductOne),
            Claim(gone, PatchB, ProductOne),
            Claim(unreadable, PatchA, ProductTwo),
        }, new[]
        {
            Claim(reverted, PatchA, ProductOne),
            Claim(gone, PatchB, ProductOne),
            Claim(unreadable, PatchA, ProductTwo),
        }));

        Assert.Equal(new[] { reverted, gone, unreadable }, recheck.HeldBack);
        Assert.Equal(
            new HeldBackReasons(Reclaimed: 1, RecordsChanged: 1, RecordsUnreadable: 1),
            recheck.Reasons);
        Assert.Equal(recheck.HeldBack.Count, recheck.Reasons.Total);
    }

    [Fact]
    public void A_second_product_holding_the_same_patch_can_reclaim_it_alone()
    {
        // THE case the whole design turns on, and the one a single retained
        // product code cannot see. One .msp, two products holding it. The merge
        // that builds the registered rows keeps one row per path, so whichever
        // product it kept is the only one a per-path re-read would ask. Here the
        // kept one still says Superseded and the other has reverted to Applied.
        const string shared = @"C:\Windows\Installer\shared.msp";
        var msi = new ScriptedPatchApi();
        msi.Set(PatchA, ProductOne, state: "2", uninstallable: "0"); // still removable
        msi.Set(PatchA, ProductTwo, state: "1", uninstallable: "0"); // needed again

        var svc = new RemovableReverifier(Substitute.For<IInstallerQueryService>(), msi);
        var reclaimed = svc.RecheckUnderLease(new UnderLeaseClaims(new[]
        {
            Claim(shared, PatchA, ProductOne),
            Claim(shared, PatchA, ProductTwo),
        }, new[]
        {
            Claim(shared, PatchA, ProductOne),
            Claim(shared, PatchA, ProductTwo),
        })).HeldBack;

        Assert.Equal(new[] { shared }, reclaimed);
    }

    [Fact]
    public void A_path_is_reported_once_however_many_of_its_claims_have_moved()
    {
        const string shared = @"C:\Windows\Installer\shared.msp";
        var msi = new ScriptedPatchApi();
        msi.Set(PatchA, ProductOne, state: "1", uninstallable: "0");
        msi.Set(PatchA, ProductTwo, state: "1", uninstallable: "0");

        var svc = new RemovableReverifier(Substitute.For<IInstallerQueryService>(), msi);
        var reclaimed = svc.RecheckUnderLease(new UnderLeaseClaims(new[]
        {
            Claim(shared, PatchA, ProductOne),
            Claim(shared, PatchA, ProductTwo),
        }, new[]
        {
            Claim(shared, PatchA, ProductOne),
            Claim(shared, PatchA, ProductTwo),
        })).HeldBack;

        // One verdict per path is all a caller can act on, and the second claim
        // is not queried at all once the first has shown a live claim.
        Assert.Equal(new[] { shared }, reclaimed);

        // FOUR READS, AND THE SPLIT IS THE ASSERTION RATHER THAN THE TOTAL. Two are
        // the batch loop's State and Uninstallable for the FIRST claim only, which is
        // the short-circuit this test is about: a second claim on a path a live claim
        // has already settled is skipped. The other two are the sibling pass
        // added for the rollback case, which reads Uninstallable once per PRODUCT
        // holding the patch, and this fixture names two products. A bare total would
        // have gone on passing if the short-circuit broke and the sibling pass got
        // cheaper by the same amount.
        Assert.Equal(2 + 2, msi.Reads);
    }

    [Fact]
    public void Unrelated_paths_are_judged_independently()
    {
        const string kept = @"C:\Windows\Installer\kept.msp";
        const string taken = @"C:\Windows\Installer\taken.msp";
        var msi = new ScriptedPatchApi();
        msi.Set(PatchA, ProductOne, state: "2", uninstallable: "0");
        msi.Set(PatchB, ProductOne, state: "1", uninstallable: "0");

        var svc = new RemovableReverifier(Substitute.For<IInstallerQueryService>(), msi);
        var reclaimed = svc.RecheckUnderLease(new UnderLeaseClaims(new[]
        {
            Claim(kept, PatchA, ProductOne),
            Claim(taken, PatchB, ProductOne),
        }, new[]
        {
            Claim(kept, PatchA, ProductOne),
            Claim(taken, PatchB, ProductOne),
        })).HeldBack;

        Assert.Equal(new[] { taken }, reclaimed);
    }

    [Fact]
    public async Task The_pre_lease_pass_carries_forward_only_the_survivors_claims()
    {
        // What the action service is handed has to match what it is acting on: a
        // dropped path is already out of the batch, and re-reading it under the
        // installer lock would be work done to confirm a decision nothing can act
        // on.
        const string dropped = @"C:\Windows\Installer\dropped.msp";
        const string kept = @"C:\Windows\Installer\kept.msp";
        var q = Substitute.For<IInstallerQueryService>();
        q.GetRegisteredPackagesAsync(Arg.Any<IProgress<ScanProgressUpdate>?>(), Arg.Any<CancellationToken>())
            .Returns(new InstallerQueryResult(
                new[] { NonRemovable(dropped) },
                UnaccountedProductCount: 0,
                PatchClaims: new[] { Claim(dropped, PatchA, ProductOne), Claim(kept, PatchB, ProductOne) }));
        var svc = Reverifier(q);

        var result = await svc.ReverifyAsync(new[] { dropped, kept });

        Assert.Equal(new[] { kept }, result.Surviving);
        var claim = Assert.Single(result.SurvivingPatchClaims);
        Assert.Equal(kept, claim.LocalPackagePath);
    }

    /// <summary>
    /// A minimal scriptable <see cref="InstallerClean.Interop.IMsiApi"/> for the
    /// under-lease re-read, which reads two patch properties and nothing else.
    /// The enumeration entry points throw rather than answering: the whole point
    /// of this path is that it does not enumerate while holding the installer
    /// mutex, so a call reaching one is the defect, not a case to model.
    /// </summary>
    private sealed class ScriptedPatchApi : InstallerClean.Interop.IMsiApi
    {
        private const uint Success = 0;
        private const uint MoreData = 234;
        private const uint FunctionFailed = 1627;
        // The two codes MsiGetPatchInfoEx documents for a pairing it cannot find,
        // and the only two InstallerQueryService marks NotRegistered rather than
        // leaving as a bare failure; it marks the second PatchNotHeld as well. A test
        // scripting anything else here is scripting the other side of that allowlist.
        private const uint UnknownProduct = 1605;
        private const uint UnknownPatch = 1647;

        private readonly Dictionary<(string, string, string), string> _values = new();
        private readonly HashSet<(string, string, string)> _failing = new();
        private readonly Dictionary<(string, string, string), uint> _absent = new();

        /// <summary>Property reads made, so a test can pin that a path is judged once.</summary>
        public int Reads { get; private set; }

        public void Set(string patchCode, string productCode, string state, string uninstallable)
        {
            _values[(patchCode, productCode, "State")] = state;
            _values[(patchCode, productCode, "Uninstallable")] = uninstallable;
        }

        public void FailProperty(string patchCode, string productCode, string property) =>
            _failing.Add((patchCode, productCode, property));

        private readonly Dictionary<(string, string, string), int> _failingOnce = new();

        /// <summary>
        /// An error that passes: the next <paramref name="calls"/> reads of this property fail,
        /// and every read after them answers as scripted.
        /// </summary>
        public void FailPropertyOnce(string patchCode, string productCode, string property, int calls = 1) =>
            _failingOnce[(patchCode, productCode, property)] = calls;

        /// <summary>
        /// Answers this property with a documented "no such record" code rather than
        /// with a value or an undocumented failure. Takes the code so a
        /// test can pin both members of the allowlist: a product that has gone
        /// (1605) and a patch that is no longer applied to it (1647) come back
        /// differently and mean the same thing here.
        /// </summary>
        public void AbsentRecord(string patchCode, string productCode, string property,
            uint code = UnknownProduct) =>
            _absent[(patchCode, productCode, property)] = code;

        public uint GetPatchInfo(string patchCode, string productCode, string? userSid,
            InstallerClean.Interop.MsiInstallContext context, string property,
            char[]? value, ref uint valueLength)
        {
            if (value is null) Reads++;
            if (value is null && _failingOnce.TryGetValue((patchCode, productCode, property), out var once) && once > 0)
            {
                _failingOnce[(patchCode, productCode, property)] = once - 1;
                valueLength = 0;
                return FunctionFailed;
            }
            if (_failing.Contains((patchCode, productCode, property)))
            {
                valueLength = 0;
                return FunctionFailed;
            }
            if (_absent.TryGetValue((patchCode, productCode, property), out var absence))
            {
                valueLength = 0;
                return absence;
            }
            var val = _values.GetValueOrDefault((patchCode, productCode, property), "");
            if (val.Length == 0) { valueLength = 0; return Success; }
            if (value is null) { valueLength = (uint)val.Length; return MoreData; }
            var n = Math.Min(val.Length, value.Length);
            for (var i = 0; i < n; i++) value[i] = val[i];
            valueLength = (uint)val.Length;
            return Success;
        }

        public uint EnumProducts(string? productCode, string? userSid,
            InstallerClean.Interop.MsiInstallContext context, uint index,
            char[]? installedProductCode, out InstallerClean.Interop.MsiInstallContext installedContext,
            char[]? sid, ref uint sidLength) =>
            throw new InvalidOperationException("The under-lease re-read must not enumerate products.");

        public uint EnumPatches(string? productCode, string? userSid,
            InstallerClean.Interop.MsiInstallContext context, InstallerClean.Interop.MsiPatchFilter filter,
            uint index, char[]? patchCode, char[]? targetProductCode,
            out InstallerClean.Interop.MsiInstallContext targetProductContext,
            char[]? targetUserSid, ref uint targetUserSidLength) =>
            throw new InvalidOperationException("The under-lease re-read must not enumerate patches.");

        public uint GetProductInfo(string productCode, string? userSid,
            InstallerClean.Interop.MsiInstallContext context, string property,
            char[]? value, ref uint valueLength) =>
            throw new InvalidOperationException("The under-lease re-read must not read product properties.");

        public uint EnumSources(string productCode, string? userSid,
            InstallerClean.Interop.MsiInstallContext context, uint options, uint index,
            char[]? source, ref uint sourceLength) =>
            throw new InvalidOperationException("The under-lease re-read must not read source lists.");

        public uint EnumMediaDisks(string productCodeOrPatchCode, string? userSid,
            InstallerClean.Interop.MsiInstallContext context, uint options, uint index, out uint diskId) =>
            throw new InvalidOperationException("The under-lease re-read must not read source lists.");

        public uint GetSourceListInfo(string productCodeOrPatchCode, string? userSid,
            InstallerClean.Interop.MsiInstallContext context, uint options, string property,
            char[]? value, ref uint valueLength) =>
            throw new InvalidOperationException("The under-lease re-read must not read source lists.");
    }
}
