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
/// <see cref="InstallerQueryServiceUnitTests"/> through the same seam.
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
    /// The pass over the registrations alone: no file is opened and no signals are read.
    /// </summary>
    private static RemovableReverifier Reverifier(IInstallerQueryService query) =>
        new(query);

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

    [Theory]
    [InlineData(LackingProgramKeep.WouldApply, false)]
    [InlineData(LackingProgramKeep.SupersededThere, false)]
    [InlineData(LackingProgramKeep.AnswerNotHad, true)]
    public async Task A_file_now_kept_for_a_program_that_lacks_its_patch_is_held_back_under_that_cause(
        LackingProgramKeep answer, bool withheld)
    {
        // The scan listed the file. By the time of the check, a program the patch is for,
        // which does not hold it, keeps it: one installed or changed since the scan.
        const string superseded = @"C:\Windows\Installer\superseded.msp";
        var svc = Reverifier(Query(NonRemovable(superseded) with
        {
            PatchState = 2, RemovableWithheld = withheld, KeptForLackingProgram = answer,
        }));

        var result = await svc.ReverifyAsync(new[] { superseded });

        Assert.Empty(result.Surviving);
        Assert.Equal(new[] { superseded }, result.Dropped);
        // Under its own cause whichever answer keeps it, the answer that was not had
        // included, which would otherwise count as records that could not be read.
        Assert.Equal(new HeldBackReasons(LackingProgram: 1), result.Reasons);
        Assert.Equal(1, result.Reasons.Total);
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
        new(query, null, null, null, null, null, signals);

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

    [Fact]
    public async Task A_dropped_path_is_not_among_the_survivors_beside_a_path_that_is_confirmed()
    {
        const string dropped = @"C:\Windows\Installer\dropped.msp";
        const string kept = @"C:\Windows\Installer\kept.msp";
        var svc = Reverifier(Query(NonRemovable(dropped)));

        var result = await svc.ReverifyAsync(new[] { dropped, kept });

        Assert.Equal(new[] { kept }, result.Surviving);
        Assert.Equal(new[] { dropped }, result.Dropped);
    }
}
