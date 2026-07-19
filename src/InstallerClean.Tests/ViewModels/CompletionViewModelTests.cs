using InstallerClean.Models;
using InstallerClean.Resources;
using InstallerClean.ViewModels;

namespace InstallerClean.Tests.ViewModels;

public class CompletionViewModelTests
{
    [Fact]
    public void FormatErrorBreakdown_splits_same_type_with_different_messages()
    {
        // RecycleFailed tailors its sentence by HRESULT (access denied vs
        // file in use vs generic); a mixed batch must show each sentence
        // over its own files, not the first file's sentence over all.
        var errors = new List<FileOperationError>
        {
            new RecycleFailed(@"C:\Windows\Installer\a.msi", unchecked((int)0x80070005)),
            new RecycleFailed(@"C:\Windows\Installer\b.msi", unchecked((int)0x80070020)),
        };

        var text = CompletionViewModel.FormatErrorBreakdown(errors);

        Assert.Contains(errors[0].LocalisedMessage, text);
        Assert.Contains(errors[1].LocalisedMessage, text);
        Assert.Contains("a.msi", text);
        Assert.Contains("b.msi", text);
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

        // The heading introduces the list, and carries no "(2)" bracket: that
        // was a count no language could inflect, sitting on a singular
        // sentence, and it read as a reference number.
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

        // Two spaces alone are invisible in a proportional font, which left the
        // filenames reading as a run-on of the sentence above them. The hyphen
        // is what makes the indent survive Poppins.
        Assert.Contains("  - a.msi", text);
        Assert.Contains("  - b.msi", text);
        // Filenames only: the full path can name another user's profile under
        // elevation.
        Assert.DoesNotContain(@"C:\Windows\Installer", text);
    }

    [Fact]
    public void ShowDeleteSummary_reads_cleaned_up_not_freed_and_sets_the_space_hint()
    {
        var vm = new CompletionViewModel();
        vm.ShowDeleteSummary(deletedCount: 2, deletedBytes: 3 * 1024 * 1024,
            errors: new List<FileOperationError>());

        // A Recycle-Bin delete reclaims no disk until the bin is emptied, so
        // the headline reads "cleaned up", and the space hint names the
        // emptying step.
        Assert.Contains("cleaned up", vm.Heading);
        Assert.DoesNotContain("freed", vm.Heading);
        Assert.Equal(Strings.Completion_DeleteSpaceHint, vm.SpaceHint);
    }

    [Fact]
    public void ShowMoveSummary_keeps_freed_for_a_space_freeing_move_and_carries_no_space_hint()
    {
        var vm = new CompletionViewModel();
        vm.ShowMoveSummary(movedCount: 1, movedBytes: 1024 * 1024, destination: @"D:\backup",
            errors: new List<FileOperationError>(), freesSpace: true);

        Assert.Contains("freed", vm.Heading);
        Assert.Equal(string.Empty, vm.SpaceHint);
    }

    [Fact]
    public void ShowPermanentDeleteSummary_reads_cleaned_up_and_carries_no_space_hint()
    {
        var vm = new CompletionViewModel();
        vm.ShowPermanentDeleteSummary(deletedCount: 1, deletedBytes: 1024 * 1024,
            errors: new List<FileOperationError>());

        // No bin to empty: the disk is reclaimed at that instant. The headline
        // still reads "cleaned up" for consistency with the recycle path.
        Assert.Contains("cleaned up", vm.Heading);
        Assert.Equal(string.Empty, vm.SpaceHint);
    }

    [Fact]
    public void Space_hint_from_a_delete_is_cleared_by_a_following_move()
    {
        // The view-model instance is reused across operations, so a stale space
        // hint from a prior delete must not bleed into a later move.
        var vm = new CompletionViewModel();
        vm.ShowDeleteSummary(deletedCount: 1, deletedBytes: 1024 * 1024,
            errors: new List<FileOperationError>());
        Assert.NotEqual(string.Empty, vm.SpaceHint);

        vm.ShowMoveSummary(movedCount: 1, movedBytes: 1024 * 1024, destination: @"D:\b",
            errors: new List<FileOperationError>(), freesSpace: false);
        Assert.Equal(string.Empty, vm.SpaceHint);
    }

    [Fact]
    public void ShowMoveSummary_sets_summary_destination_to_the_raw_path()
    {
        // The WPF host locates this raw string inside the formatted Summary
        // to force the destination onto its own line; it must be the
        // literal, unformatted path handed to ShowMoveSummary.
        var vm = new CompletionViewModel();
        vm.ShowMoveSummary(movedCount: 1, movedBytes: 1024 * 1024, destination: @"D:\backup",
            errors: new List<FileOperationError>(), freesSpace: true);

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
            errors: new List<FileOperationError>(), freesSpace: true);
        Assert.NotEqual(string.Empty, vm.SummaryDestination);

        vm.ShowDeleteSummary(deletedCount: 1, deletedBytes: 1024 * 1024,
            errors: new List<FileOperationError>());
        Assert.Equal(string.Empty, vm.SummaryDestination);
    }

    // The WPF host's BuildCompletionRestoreLine rebuilds the restore line's
    // inlines synchronously off Restore's PropertyChanged, reading SpaceHint
    // at that instant and baking the result into TextBlock inlines. A Show*
    // method that sets SpaceHint AFTER Restore lets a SpaceHint left over
    // from an earlier Delete this session leak into the rebuild, even though
    // the property's final value (asserted by the tests above) is correct.
    // These three regressions shipped in 2.0.0 (all clear, a following move,
    // a following permanent delete) and were invisible to final-state
    // assertions; only capturing SpaceHint at the moment Restore's
    // PropertyChanged fires catches them.

    [Fact]
    public void ShowAllClear_has_cleared_the_space_hint_by_the_time_Restore_changes()
    {
        var vm = new CompletionViewModel();
        vm.ShowDeleteSummary(deletedCount: 1, deletedBytes: 1024 * 1024,
            errors: new List<FileOperationError>());

        string? spaceHintDuringRestoreChange = "unset";
        vm.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName == nameof(CompletionViewModel.Restore))
                spaceHintDuringRestoreChange = vm.SpaceHint;
        };

        vm.ShowAllClear(installedProductCount: 5, scanDurationMs: 10);

        Assert.Equal(string.Empty, spaceHintDuringRestoreChange);
    }

    [Fact]
    public void ShowMoveSummary_has_cleared_the_space_hint_by_the_time_Restore_changes()
    {
        var vm = new CompletionViewModel();
        vm.ShowDeleteSummary(deletedCount: 1, deletedBytes: 1024 * 1024,
            errors: new List<FileOperationError>());

        string? spaceHintDuringRestoreChange = "unset";
        vm.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName == nameof(CompletionViewModel.Restore))
                spaceHintDuringRestoreChange = vm.SpaceHint;
        };

        vm.ShowMoveSummary(movedCount: 1, movedBytes: 1024 * 1024, destination: @"D:\backup",
            errors: new List<FileOperationError>(), freesSpace: true);

        Assert.Equal(string.Empty, spaceHintDuringRestoreChange);
    }

    [Fact]
    public void ShowPermanentDeleteSummary_has_cleared_the_space_hint_by_the_time_Restore_changes()
    {
        var vm = new CompletionViewModel();
        vm.ShowDeleteSummary(deletedCount: 1, deletedBytes: 1024 * 1024,
            errors: new List<FileOperationError>());

        string? spaceHintDuringRestoreChange = "unset";
        vm.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName == nameof(CompletionViewModel.Restore))
                spaceHintDuringRestoreChange = vm.SpaceHint;
        };

        vm.ShowPermanentDeleteSummary(deletedCount: 1, deletedBytes: 1024 * 1024,
            errors: new List<FileOperationError>());

        Assert.Equal(string.Empty, spaceHintDuringRestoreChange);
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
            errors: [], freesSpace: true);

        Assert.False(vm.HeadingIsWarning);
        Assert.Contains("freed", vm.Heading);
        Assert.Equal(string.Empty, vm.FailedCount);
    }

    [Fact]
    public void A_partly_failed_move_keeps_the_success_heading_and_states_the_count()
    {
        var vm = new CompletionViewModel();
        vm.ShowMoveSummary(movedCount: 69, movedBytes: 1024 * 1024, destination: @"D:\backup",
            errors: Failures(2), freesSpace: true);

        // Something really was freed, so the heading says so and the failures
        // get their own line. The heading used to carry both, in one clause too
        // long for the card, and clipped.
        Assert.False(vm.HeadingIsWarning);
        Assert.Contains("freed", vm.Heading);
        Assert.Equal("2 of 71 could not be moved.", vm.FailedCount);
        // The destination line is the plain variant whatever happened: the
        // error count used to be appended here, where it read as part of the
        // folder name.
        Assert.Equal(@"69 files moved to: D:\backup", vm.Summary);
    }

    [Fact]
    public void A_move_that_achieved_nothing_says_so_and_claims_no_destination()
    {
        var vm = new CompletionViewModel();
        vm.ShowMoveSummary(movedCount: 0, movedBytes: 0, destination: @"D:\backup",
            errors: Failures(2), freesSpace: true);

        // This used to render "0 B freed, some files could not be proce": wrong
        // twice, reporting a total failure as a result and then clipping.
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
    public void A_delete_that_achieved_nothing_offers_neither_bin_nor_restore()
    {
        var vm = new CompletionViewModel();
        vm.ShowDeleteSummary(deletedCount: 0, deletedBytes: 0, errors: Failures(3));

        Assert.True(vm.HeadingIsWarning);
        Assert.Equal(Strings.Completion_NothingDeleted, vm.Heading);
        Assert.Equal("3 of 3 could not be deleted.", vm.FailedCount);
        Assert.Equal(string.Empty, vm.Summary);
        // The bin gained nothing, so there is nothing to empty and nothing to
        // restore from it.
        Assert.Equal(string.Empty, vm.SpaceHint);
        Assert.Equal(string.Empty, vm.Restore);
    }

    [Fact]
    public void A_permanent_delete_that_achieved_nothing_drops_its_reassurance()
    {
        var vm = new CompletionViewModel();
        vm.ShowPermanentDeleteSummary(deletedCount: 0, deletedBytes: 0, errors: Failures(1));

        Assert.True(vm.HeadingIsWarning);
        Assert.Equal(Strings.Completion_NothingDeleted, vm.Heading);
        Assert.Equal("1 of 1 could not be deleted.", vm.FailedCount);
        Assert.Equal(string.Empty, vm.Summary);
        // "That's fine, it was safe to remove" is about a file that was
        // removed. None was.
        Assert.Equal(string.Empty, vm.Restore);
    }

    [Fact]
    public void A_partly_failed_delete_keeps_its_bin_and_restore_copy()
    {
        var vm = new CompletionViewModel();
        vm.ShowDeleteSummary(deletedCount: 5, deletedBytes: 1024, errors: Failures(1));

        Assert.False(vm.HeadingIsWarning);
        Assert.Contains("cleaned up", vm.Heading);
        Assert.Equal(Strings.Completion_DeleteSpaceHint, vm.SpaceHint);
        Assert.Equal(Strings.Completion_DeleteRestoreHint, vm.Restore);
    }

    [Fact]
    public void A_cancelled_move_counts_what_it_tried_not_the_batch_it_was_given()
    {
        var vm = new CompletionViewModel();
        vm.ShowMoveCancelledSummary(movedCount: 3, totalCount: 71, movedBytes: 1024,
            errors: Failures(2), freesSpace: true);

        // 5 tried, not 71 queued: the 66 the cancel never reached are not files
        // that could not be moved. The summary line below still names the whole
        // batch, which is its own job.
        Assert.Equal("2 of 5 could not be moved.", vm.FailedCount);
        Assert.Contains("71", vm.Summary);
        // A cancel is not a failure, so the heading stays as it was.
        Assert.False(vm.HeadingIsWarning);
    }

    [Fact]
    public void A_cancelled_delete_that_moved_nothing_still_keeps_its_heading()
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
            errors: Failures(2), freesSpace: true);

        // Nothing reached the destination, so nothing may invite copying it back.
        Assert.Equal(string.Empty, vm.Restore);
    }

    [Fact]
    public void A_cancelled_delete_that_deleted_nothing_offers_neither_bin_nor_restore()
    {
        var vm = new CompletionViewModel();
        vm.ShowDeleteCancelledSummary(deletedCount: 0, totalCount: 40, deletedBytes: 0,
            errors: Failures(3));

        Assert.Equal(string.Empty, vm.SpaceHint);
        Assert.Equal(string.Empty, vm.Restore);
    }

    [Fact]
    public void A_cancelled_permanent_delete_that_deleted_nothing_drops_its_reassurance()
    {
        var vm = new CompletionViewModel();
        vm.ShowPermanentDeleteCancelledSummary(deletedCount: 0, totalCount: 40, deletedBytes: 0,
            errors: Failures(1));

        Assert.Equal(string.Empty, vm.Restore);
    }

    [Fact]
    public void A_cancelled_move_that_moved_something_keeps_its_restore_hint()
    {
        var vm = new CompletionViewModel();
        vm.ShowMoveCancelledSummary(movedCount: 3, totalCount: 40, movedBytes: 1024,
            errors: Failures(2), freesSpace: true);

        Assert.Equal(Strings.Completion_MoveRestoreHint, vm.Restore);
    }

    [Fact]
    public void A_cancelled_delete_that_deleted_something_keeps_its_bin_and_restore_copy()
    {
        var vm = new CompletionViewModel();
        vm.ShowDeleteCancelledSummary(deletedCount: 3, totalCount: 40, deletedBytes: 1024,
            errors: Failures(2));

        Assert.Equal(Strings.Completion_DeleteSpaceHint, vm.SpaceHint);
        Assert.Equal(Strings.Completion_DeleteRestoreHint, vm.Restore);
    }

    [Fact]
    public void A_cancelled_permanent_delete_that_deleted_something_keeps_its_reassurance()
    {
        var vm = new CompletionViewModel();
        vm.ShowPermanentDeleteCancelledSummary(deletedCount: 3, totalCount: 40, deletedBytes: 1024,
            errors: Failures(2));

        Assert.NotEqual(string.Empty, vm.Restore);
    }

    [Fact]
    public void The_count_line_and_warning_heading_do_not_survive_into_the_next_operation()
    {
        // The view-model instance is reused across operations, so a count line
        // or a warning heading left over from a failed run would sit under the
        // next run's green heading.
        var vm = new CompletionViewModel();
        vm.ShowMoveSummary(movedCount: 0, movedBytes: 0, destination: @"D:\backup",
            errors: Failures(2), freesSpace: true);
        Assert.True(vm.HeadingIsWarning);
        Assert.NotEqual(string.Empty, vm.FailedCount);

        vm.ShowDeleteSummary(deletedCount: 2, deletedBytes: 1024, errors: []);
        Assert.False(vm.HeadingIsWarning);
        Assert.Equal(string.Empty, vm.FailedCount);

        vm.ShowAllClear(installedProductCount: 5, scanDurationMs: 10);
        Assert.False(vm.HeadingIsWarning);
        Assert.Equal(string.Empty, vm.FailedCount);
    }
}
