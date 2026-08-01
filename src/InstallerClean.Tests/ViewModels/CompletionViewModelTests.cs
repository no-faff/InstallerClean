using InstallerClean.Models;
using InstallerClean.Resources;
using InstallerClean.ViewModels;

namespace InstallerClean.Tests.ViewModels;

public class CompletionViewModelTests
{
    /// <summary>
    /// A category whose sentence varies with the file. No shipped category does
    /// that today, which is why this is declared here rather than borrowed:
    /// RecycleFailed was the one that did, and it went with the Recycle Bin.
    /// The behaviour it stands for did not go, so it is held from here.
    /// </summary>
    private sealed record VaryingMessage(string FilePath, string Message)
        : FileOperationError(FilePath)
    {
        public override string LocalisedMessage => Message;
    }

    [Fact]
    public void FormatErrorBreakdown_splits_one_type_whose_sentence_varies_by_file()
    {
        // The grouping key is (type, message) rather than type alone, and this
        // is what that buys: a bucket's heading is taken from its FIRST member,
        // so a type that ever tailors its wording would otherwise print one
        // file's sentence over files that failed differently. Nothing shipped
        // tailors one today; the comment on FormatErrorBreakdown keeps the key
        // that way for the category that will, and this is the test that says
        // what "that way" means.
        var errors = new List<FileOperationError>
        {
            new VaryingMessage(@"C:\Windows\Installer\a.msi", "One thing went wrong."),
            new VaryingMessage(@"C:\Windows\Installer\b.msi", "A different thing went wrong."),
        };

        var text = CompletionViewModel.FormatErrorBreakdown(errors);

        Assert.Contains("One thing went wrong.", text);
        Assert.Contains("A different thing went wrong.", text);
        Assert.Contains("- a.msi", text);
        Assert.Contains("- b.msi", text);
    }

    [Fact]
    public void FormatErrorBreakdown_heads_a_bucket_with_its_pluralised_sentence()
    {
        var errors = new List<FileOperationError>
        {
            new FileInUse(@"C:\Windows\Installer\a.msi"),
            new FileInUse(@"C:\Windows\Installer\b.msi"),
        };

        var text = CompletionViewModel.FormatErrorBreakdown(errors);

        // The heading introduces the list, and carries no "(2)" bracket. Such a
        // bracket is a count no language can inflect, sitting on a sentence that
        // is already singular or plural, and it reads as a reference number.
        Assert.StartsWith(Strings.Error_FileInUse_Plural, text);
        Assert.DoesNotContain("(2)", text);
    }

    [Fact]
    public void FormatErrorBreakdown_keeps_a_single_failure_singular()
    {
        var errors = new List<FileOperationError> { new FileInUse(@"C:\Windows\Installer\a.msi") };

        var text = CompletionViewModel.FormatErrorBreakdown(errors);

        Assert.StartsWith(Strings.Error_FileInUse_Singular, text);
    }

    [Fact]
    public void FormatErrorBreakdown_indents_filenames_with_a_visible_marker()
    {
        var errors = new List<FileOperationError>
        {
            new AccessDenied(@"C:\Windows\Installer\a.msi"),
            new AccessDenied(@"C:\Windows\Installer\b.msi"),
        };

        var text = CompletionViewModel.FormatErrorBreakdown(errors);

        // Leading spaces alone vanish in a proportional font, so the hyphen is
        // what separates a filename from the sentence above it.
        Assert.Contains("- a.msi", text);
        Assert.Contains("- b.msi", text);
        // Filenames only: the full path can name another user's profile under
        // elevation.
        Assert.DoesNotContain(@"C:\Windows\Installer", text);
    }

    [Fact]
    public void ShowDeleteSummary_reads_freed_and_carries_no_restore_line()
    {
        var vm = new CompletionViewModel();
        vm.ShowDeleteSummary(deletedCount: 2, deletedBytes: 3 * 1024 * 1024,
            errors: new List<FileOperationError>());

        // A delete reclaims the disk at the instant it happens, so the headline
        // says so. The screen makes no safety claim and offers no recovery
        // advice, which is why it is the one completion state with no line
        // under the summary and no link.
        Assert.Contains("freed", vm.Heading);
        Assert.Equal(string.Empty, vm.Restore);
    }

    [Fact]
    public void ShowMoveSummary_reads_freed_only_when_the_folder_is_on_another_drive()
    {
        var vm = new CompletionViewModel();

        vm.ShowMoveSummary(movedCount: 1, movedBytes: 1024 * 1024, destination: @"D:\backup",
            errors: [], space: MoveSpaceOutcome.FreedSpace);
        Assert.Contains("freed", vm.Heading);

        // A same-drive move is a rename: nothing is reclaimed until the folder
        // goes, so the claim-less verb is used and the line beneath says when
        // the space comes back.
        vm.ShowMoveSummary(movedCount: 1, movedBytes: 1024 * 1024, destination: @"C:\backup",
            errors: [], space: MoveSpaceOutcome.SameDrive);
        Assert.DoesNotContain("freed", vm.Heading);
        Assert.Equal(Strings.Completion_MoveRestoreHintSameDrive_Singular, vm.Restore);

        // A volume the classification could not read claims nothing either way,
        // so it takes the same verb but the line that names no drive. The
        // destination is not what selects this and is never read here: the
        // outcome arrives already decided (a share is FreedSpace, not this).
        vm.ShowMoveSummary(movedCount: 2, movedBytes: 1024 * 1024, destination: @"D:\backup",
            errors: [], space: MoveSpaceOutcome.Unclassified);
        Assert.DoesNotContain("freed", vm.Heading);
        Assert.Equal(Strings.Completion_MoveRestoreHint_Plural, vm.Restore);
    }

    [Fact]
    public void ShowMoveSummary_sets_summary_destination_to_the_raw_path()
    {
        // The WPF host locates this raw string inside the formatted Summary
        // to force the destination onto its own line; it must be the
        // literal, unformatted path handed to ShowMoveSummary.
        var vm = new CompletionViewModel();
        vm.ShowMoveSummary(movedCount: 1, movedBytes: 1024 * 1024, destination: @"D:\backup",
            errors: [], space: MoveSpaceOutcome.FreedSpace);

        Assert.Equal(@"D:\backup", vm.SummaryDestination);
        Assert.Contains(@"D:\backup", vm.Summary);
    }

    [Fact]
    public void Summary_destination_from_a_move_is_cleared_by_a_following_delete()
    {
        // The view-model instance is reused across operations, so a stale
        // destination path from a prior move must not bleed into a later
        // delete summary (which has no destination placeholder at all).
        var vm = new CompletionViewModel();
        vm.ShowMoveSummary(movedCount: 1, movedBytes: 1024 * 1024, destination: @"D:\backup",
            errors: [], space: MoveSpaceOutcome.FreedSpace);
        Assert.NotEqual(string.Empty, vm.SummaryDestination);

        vm.ShowDeleteSummary(deletedCount: 1, deletedBytes: 1024 * 1024,
            errors: new List<FileOperationError>());
        Assert.Equal(string.Empty, vm.SummaryDestination);
    }

    // The three heading states, the failure count line and the acted-on-nothing
    // suppressions. The contract in one line: the heading says one thing, the
    // count line says how many failed, and nothing on the screen describes files
    // arriving somewhere none of them reached.

    private static List<FileOperationError> Failures(int count) =>
        [.. Enumerable.Range(0, count).Select(i =>
            (FileOperationError)new FileInUse($@"C:\Windows\Installer\f{i}.msi"))];

    [Fact]
    public void A_move_that_fully_succeeded_keeps_the_success_heading_and_no_count_line()
    {
        var vm = new CompletionViewModel();
        vm.ShowMoveSummary(movedCount: 3, movedBytes: 1024 * 1024, destination: @"D:\backup",
            errors: [], space: MoveSpaceOutcome.FreedSpace);

        Assert.False(vm.HeadingIsWarning);
        Assert.Contains("freed", vm.Heading);
        Assert.Equal(string.Empty, vm.FailedCount);
    }

    [Fact]
    public void A_partly_failed_move_keeps_the_success_heading_and_states_the_count()
    {
        var vm = new CompletionViewModel();
        vm.ShowMoveSummary(movedCount: 69, movedBytes: 1024 * 1024, destination: @"D:\backup",
            errors: Failures(2), space: MoveSpaceOutcome.FreedSpace);

        // Something really was freed, so the heading says so and the failures
        // get their own line. Carrying both in the heading makes one clause too
        // long for the card, and it clips.
        Assert.False(vm.HeadingIsWarning);
        Assert.Contains("freed", vm.Heading);
        Assert.Equal("2 of 71 could not be moved.", vm.FailedCount);
        // The destination line is the plain variant whatever happened. An error
        // count appended here lands immediately after the path, where it reads
        // as part of the folder name.
        Assert.Equal(@"69 files moved to: D:\backup", vm.Summary);
    }

    [Fact]
    public void A_move_that_achieved_nothing_says_so_and_claims_no_destination()
    {
        var vm = new CompletionViewModel();
        vm.ShowMoveSummary(movedCount: 0, movedBytes: 0, destination: @"D:\backup",
            errors: Failures(2), space: MoveSpaceOutcome.FreedSpace);

        // Routing a total failure through the success heading renders
        // "0 B freed, some files could not be proce": wrong twice, reporting a
        // total failure as a result and then clipping mid-word.
        Assert.True(vm.HeadingIsWarning);
        Assert.Equal(Strings.Completion_NothingMoved, vm.Heading);
        Assert.Equal("2 of 2 could not be moved.", vm.FailedCount);
        // Nothing reached the destination, so nothing on screen may say files
        // are there or invite copying them back.
        Assert.Equal(string.Empty, vm.Summary);
        Assert.Equal(string.Empty, vm.SummaryDestination);
        Assert.Equal(string.Empty, vm.Restore);
        Assert.NotEqual(string.Empty, vm.Errors);
    }

    [Fact]
    public void A_delete_that_achieved_nothing_says_so_and_claims_nothing()
    {
        var vm = new CompletionViewModel();
        vm.ShowDeleteSummary(deletedCount: 0, deletedBytes: 0, errors: Failures(3));

        Assert.True(vm.HeadingIsWarning);
        Assert.Equal(Strings.Completion_NothingDeleted, vm.Heading);
        Assert.Equal("3 of 3 could not be deleted.", vm.FailedCount);
        // Nothing was deleted, so nothing on screen may report a deletion.
        Assert.Equal(string.Empty, vm.Summary);
        Assert.Equal(string.Empty, vm.Restore);
    }

    [Fact]
    public void A_partly_failed_delete_keeps_the_success_heading()
    {
        var vm = new CompletionViewModel();
        vm.ShowDeleteSummary(deletedCount: 5, deletedBytes: 1024, errors: Failures(1));

        // Five files really were deleted and the space really did come back, so
        // the heading says so and the count line carries the rest.
        Assert.False(vm.HeadingIsWarning);
        Assert.Contains("freed", vm.Heading);
        Assert.Equal("1 of 6 could not be deleted.", vm.FailedCount);
    }

    [Fact]
    public void A_cancelled_move_counts_what_it_tried_not_the_batch_it_was_given()
    {
        var vm = new CompletionViewModel();
        vm.ShowMoveCancelledSummary(movedCount: 3, totalCount: 71, movedBytes: 1024,
            errors: Failures(2), space: MoveSpaceOutcome.FreedSpace);

        // 5 tried, not 71 queued: the 66 the cancel never reached are not files
        // that could not be moved. The summary line below still names the whole
        // batch, which is its own job.
        Assert.Equal("2 of 5 could not be moved.", vm.FailedCount);
        Assert.Contains("71", vm.Summary);
        // A cancel is not a failure, so the heading stays as it was.
        Assert.False(vm.HeadingIsWarning);
    }

    [Fact]
    public void A_cancelled_delete_that_deleted_nothing_still_keeps_its_heading()
    {
        var vm = new CompletionViewModel();
        vm.ShowDeleteCancelledSummary(deletedCount: 0, totalCount: 40, deletedBytes: 0,
            errors: Failures(1));

        Assert.False(vm.HeadingIsWarning);
        Assert.Equal("1 of 1 could not be deleted.", vm.FailedCount);
        Assert.NotEqual(string.Empty, vm.Summary);
    }

    // The three cancelled paths reach the same nothing-was-acted-on state the
    // completed paths guard against, by a different route: a cancel pressed
    // after the first few files failed. The advice is about files that arrived
    // somewhere, so it is as false here as it is there. The heading is the one
    // thing that stays, per the test above.

    [Fact]
    public void A_cancelled_move_that_moved_nothing_drops_its_restore_hint()
    {
        var vm = new CompletionViewModel();
        vm.ShowMoveCancelledSummary(movedCount: 0, totalCount: 40, movedBytes: 0,
            errors: Failures(2), space: MoveSpaceOutcome.FreedSpace);

        // Nothing reached the destination, so nothing may invite copying it back.
        Assert.Equal(string.Empty, vm.Restore);
    }

    [Fact]
    public void A_cancelled_move_that_moved_something_keeps_its_restore_hint()
    {
        var vm = new CompletionViewModel();
        vm.ShowMoveCancelledSummary(movedCount: 3, totalCount: 40, movedBytes: 1024,
            errors: Failures(2), space: MoveSpaceOutcome.FreedSpace);

        Assert.Equal(Strings.Completion_MoveRestoreHint_Plural, vm.Restore);
    }

    [Fact]
    public void A_cancelled_delete_carries_no_restore_line_whatever_it_reached()
    {
        // Same rule as the completed delete: the screen makes no safety claim,
        // so there is nothing under the summary at any count.
        var vm = new CompletionViewModel();
        vm.ShowDeleteCancelledSummary(deletedCount: 3, totalCount: 40, deletedBytes: 1024,
            errors: Failures(2));

        Assert.Equal(string.Empty, vm.Restore);
    }

    [Fact]
    public void The_count_line_and_warning_heading_do_not_survive_into_the_next_operation()
    {
        // The view-model instance is reused across operations, so a count line
        // or a warning heading left over from a failed run would sit under the
        // next run's green heading.
        var vm = new CompletionViewModel();
        vm.ShowMoveSummary(movedCount: 0, movedBytes: 0, destination: @"D:\backup",
            errors: Failures(2), space: MoveSpaceOutcome.FreedSpace);
        Assert.True(vm.HeadingIsWarning);
        Assert.NotEqual(string.Empty, vm.FailedCount);

        vm.ShowDeleteSummary(deletedCount: 2, deletedBytes: 1024, errors: []);
        Assert.False(vm.HeadingIsWarning);
        Assert.Equal(string.Empty, vm.FailedCount);

        vm.ShowAllClear(installedProductCount: 5, scanDurationMs: 10);
        Assert.False(vm.HeadingIsWarning);
        Assert.Equal(string.Empty, vm.FailedCount);
    }

    // The donate heart's gate. The ask is only ever made after the app has
    // actually delivered something, so every path that freed no bytes has to
    // leave it off, including the two that are outright successes (an
    // all-clear scan, a run the act-time re-verify held back entirely).

    [Fact]
    public void ShowDonate_is_set_by_a_delete_that_freed_bytes()
    {
        var vm = new CompletionViewModel();

        vm.ShowDeleteSummary(deletedCount: 3, deletedBytes: 4096, errors: []);

        Assert.True(vm.ShowDonate);
    }

    [Fact]
    public void ShowDonate_stays_off_for_a_delete_that_freed_nothing()
    {
        var vm = new CompletionViewModel();

        vm.ShowDeleteSummary(deletedCount: 0, deletedBytes: 0, errors: []);

        Assert.False(vm.ShowDonate);
    }

    [Fact]
    public void ShowDonate_is_cleared_by_an_all_clear_after_a_freeing_run()
    {
        // The view-model instance is reused across operations, so a heart left
        // set by the Delete would follow the user onto the all-clear that the
        // post-operation rescan produces, asking for money for a scan.
        var vm = new CompletionViewModel();
        vm.ShowDeleteSummary(deletedCount: 3, deletedBytes: 4096, errors: []);
        Assert.True(vm.ShowDonate);

        vm.ShowAllClear(installedProductCount: 5, scanDurationMs: 10);

        Assert.False(vm.ShowDonate);
    }
}
