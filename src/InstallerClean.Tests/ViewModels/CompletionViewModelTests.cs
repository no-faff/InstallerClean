using System.Globalization;
using InstallerClean.Helpers;
using InstallerClean.Models;
using InstallerClean.Resources;
using InstallerClean.Services;
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

    private sealed class LocalisationScope : IDisposable
    {
        public LocalisationScope(CultureInfo culture) => Localisation.Set(culture, culture);

        public void Dispose() => Localisation.Reset();
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
    public void FormatErrorBreakdown_marks_each_filename_with_a_hyphen()
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
        // says so, and this is a completion state carrying no line under the
        // summary.
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
        Assert.Equal(Strings.Completion_MoveRestoreHintSameDrive, vm.Restore);

        // A volume the classification could not read claims nothing either way,
        // so it takes the same verb but the line that names no drive. The
        // destination is not what selects this and is never read here: the
        // outcome arrives already decided (a share is FreedSpace, not this).
        vm.ShowMoveSummary(movedCount: 2, movedBytes: 1024 * 1024, destination: @"D:\backup",
            errors: [], space: MoveSpaceOutcome.Unclassified);
        Assert.DoesNotContain("freed", vm.Heading);
        Assert.Equal(Strings.Completion_MoveRestoreHint, vm.Restore);
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
        Assert.Equal("2 files could not be moved.", vm.FailedCount);
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
        Assert.Equal("2 files could not be moved.", vm.FailedCount);
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
        Assert.Equal("3 files could not be deleted.", vm.FailedCount);
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
        Assert.Equal("1 file could not be deleted.", vm.FailedCount);
    }

    /// <summary>
    /// Every language the app ships, because the English line names the failure
    /// count on its own and the rule under test is about the number beside it.
    /// </summary>
    public static TheoryData<string> ShippedCultures()
    {
        var data = new TheoryData<string>();
        foreach (var name in SupportedLanguages.CultureNames)
            data.Add(name);

        return data;
    }

    [Theory]
    [MemberData(nameof(ShippedCultures))]
    public void A_cancelled_move_names_what_failed_and_leaves_the_batch_to_the_summary(string cultureName)
    {
        using var scope = new LocalisationScope(CultureInfo.GetCultureInfo(cultureName));

        var vm = new CompletionViewModel();
        vm.ShowMoveCancelledSummary(movedCount: 3, totalCount: 71, movedBytes: 1024,
            destination: @"D:\backup", errors: Failures(2), space: MoveSpaceOutcome.FreedSpace);

        // The line names what failed and nothing else. 71 were queued and 5 were
        // reached, and neither number belongs beside a failure count: the 66 the
        // cancel never got to are not files that could not be moved, and the 3
        // that moved are not either. The summary line below names the whole
        // batch, which is its own job.
        //
        // WALKED ACROSS THE LANGUAGES RATHER THAN ASSERTED ON ONE STRING, because
        // each language builds this sentence its own way and a number put back
        // into any one of them would show up in that language and nowhere else.
        Assert.Contains("2", vm.FailedCount);
        Assert.DoesNotContain("71", vm.FailedCount);
        Assert.DoesNotContain("5", vm.FailedCount);
        Assert.Contains("71", vm.Summary);
        // A cancel is not a failure, so the heading stays as it was. This is the
        // control on the pair below: three files really did move and the size
        // heading is stating what they freed, so errors alone must not warn.
        Assert.False(vm.HeadingIsWarning);
    }

    // A cancel that reached no file and hit an error, on both paths. Until 3.0.0
    // the heading was "0 B freed" in the success colour over a line saying a file
    // could not be processed, and the delete test below asserted exactly that, in
    // its name as well as its body. The size heading has nothing to state here, so
    // it gives way to the same string the completed paths use.

    [Fact]
    public void A_stopped_move_says_why_it_stopped_and_not_to_delete_the_folder()
    {
        var vm = new CompletionViewModel();
        vm.ShowMoveStoppedSummary(movedCount: 62, movedBytes: 1024 * 1024, destination: @"D:\backup",
            errors: [], space: MoveSpaceOutcome.FreedSpace);

        Assert.Equal(
            string.Format(Strings.Error_DestinationChangedMidBatch, @"D:\backup"),
            vm.Restore);

        // The control that makes the absence attributable. Without it, a line that
        // had stopped being written at all would satisfy the assertion above just
        // as well as one that had been replaced.
        var finished = new CompletionViewModel();
        finished.ShowMoveSummary(movedCount: 62, movedBytes: 1024 * 1024, destination: @"D:\backup",
            errors: [], space: MoveSpaceOutcome.FreedSpace);
        Assert.Equal(Strings.Completion_MoveRestoreHint, finished.Restore);

        // Everything above that line is the finished card's, and it is meant to
        // be: those files really did move and that space really did come back.
        Assert.Equal(finished.Heading, vm.Heading);
        Assert.Equal(finished.Summary, vm.Summary);
        Assert.Equal(finished.SummaryDestination, vm.SummaryDestination);
        Assert.False(vm.HeadingIsWarning);
    }

    [Fact]
    public void A_stopped_move_that_moved_nothing_still_says_why_it_stopped()
    {
        var vm = new CompletionViewModel();
        vm.ShowMoveStoppedSummary(movedCount: 0, movedBytes: 0, destination: @"D:\backup",
            errors: Failures(2), space: MoveSpaceOutcome.FreedSpace);

        // The warning heading empties the summary and, on a Move that reached the
        // end, the line under it as well. The stop sentence is not that line's
        // advice about a folder's contents, it is why the run ended, so it is the
        // one thing that survives the arm which clears everything else.
        Assert.True(vm.HeadingIsWarning);
        Assert.Equal(string.Empty, vm.Summary);
        Assert.Equal(
            string.Format(Strings.Error_DestinationChangedMidBatch, @"D:\backup"),
            vm.Restore);
    }

    [Fact]
    public void A_cancelled_delete_that_deleted_nothing_and_hit_an_error_warns()
    {
        var vm = new CompletionViewModel();
        vm.ShowDeleteCancelledSummary(deletedCount: 0, totalCount: 40, deletedBytes: 0,
            errors: Failures(1));

        Assert.True(vm.HeadingIsWarning);
        Assert.Equal(Strings.Completion_NothingDeleted, vm.Heading);
        Assert.Equal("1 file could not be deleted.", vm.FailedCount);
        // And the cancelled sentence survives the warning, which is where these two
        // part company with ShowMoveSummary and ShowDeleteSummary. It is the only
        // line on the screen saying the run was stopped rather than that it failed.
        Assert.NotEqual(string.Empty, vm.Summary);
    }

    [Fact]
    public void A_cancelled_move_that_moved_nothing_and_hit_an_error_warns()
    {
        var vm = new CompletionViewModel();
        vm.ShowMoveCancelledSummary(movedCount: 0, totalCount: 40, movedBytes: 0,
            destination: @"D:\backup", errors: Failures(2), space: MoveSpaceOutcome.FreedSpace);

        Assert.True(vm.HeadingIsWarning);
        Assert.Equal(Strings.Completion_NothingMoved, vm.Heading);
        Assert.NotEqual(string.Empty, vm.Summary);
    }

    // The three cancelled paths reach the same nothing-was-acted-on state the
    // completed paths guard against, by a different route: a cancel pressed
    // after the first few files failed. The advice is about files that arrived
    // somewhere, so it is as false here as it is there.

    [Fact]
    public void A_cancelled_move_names_the_folder_its_files_went_to()
    {
        // The two sentences on this card are a pair: the first names where the
        // files that moved have gone, the second names where they came from, and
        // between them they say how to put things back. Neither is any use to a
        // reader on its own, so the destination reaching the summary is held here
        // rather than left to the call site.
        var vm = new CompletionViewModel();
        vm.ShowMoveCancelledSummary(movedCount: 4, totalCount: 20, movedBytes: 1024,
            destination: @"D:\InstallerBackup", errors: Array.Empty<FileOperationError>(),
            space: MoveSpaceOutcome.FreedSpace);

        Assert.Contains(@"D:\InstallerBackup", vm.Summary);
        Assert.Contains("4", vm.Summary);
        Assert.Contains("20", vm.Summary);
        // And it stays in the sentence rather than going onto a line of its own,
        // which is what leaving SummaryDestination empty buys: the host splits the
        // summary at this value, and here the cancel follows the path.
        Assert.Equal(string.Empty, vm.SummaryDestination);
    }

    [Fact]
    public void A_cancelled_move_that_moved_nothing_drops_its_restore_hint()
    {
        var vm = new CompletionViewModel();
        vm.ShowMoveCancelledSummary(movedCount: 0, totalCount: 40, movedBytes: 0,
            destination: @"D:\backup", errors: Failures(2), space: MoveSpaceOutcome.FreedSpace);

        // Nothing reached the destination, so nothing may invite copying it back.
        Assert.Equal(string.Empty, vm.Restore);
    }

    [Fact]
    public void A_cancelled_move_that_moved_something_keeps_its_restore_hint()
    {
        var vm = new CompletionViewModel();
        vm.ShowMoveCancelledSummary(movedCount: 3, totalCount: 40, movedBytes: 1024,
            destination: @"D:\backup", errors: Failures(2), space: MoveSpaceOutcome.FreedSpace);

        // The cancelled screen's own line, not the completed screen's. They share
        // no key, so a change to one cannot silently move the other.
        Assert.Equal(Strings.Completion_MoveCancelledRestoreHint, vm.Restore);
        Assert.NotEqual(Strings.Completion_MoveRestoreHint, vm.Restore);
    }

    [Fact]
    public void A_cancelled_delete_carries_no_restore_line_whatever_it_reached()
    {
        // Same rule as the completed delete: nothing under the summary at any
        // count.
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

    [Fact]
    public void The_wholesale_screen_says_what_was_held_back_and_shows_the_scan_receipt()
    {
        // THE FIGURES ARE THE WITHHELD SET AND NEVER THE FOLDER. The body says the app
        // held back what it might otherwise have OFFERED, which is a claim about what
        // the scan would have listed; a folder total there would tell somebody that
        // much was going spare, which nothing established. The sentence is not quoted
        // here, because a quotation of a value that is still being worded goes stale
        // in a comment nothing checks.
        var vm = new CompletionViewModel();

        vm.ShowNothingOffered(
            WithholdingAccount.WholeWalkOffer, withheldCount: 3, withheldBytes: 3072,
            installedProductCount: 5, scanDurationMs: 10);

        Assert.True(vm.IsComplete);
        Assert.False(vm.HeadingIsWarning);
        Assert.Equal(Strings.Completion_NothingOffered, vm.Heading);
        Assert.Equal(
            string.Format(
                Strings.Completion_NothingOfferedBody_Plural,
                3, DisplayHelpers.PluraliseFile(3), DisplayHelpers.FormatSize(3072)),
            vm.Summary);

        // THE RECEIPT IS NOT DECORATION. A heading and a body with no evidence that a
        // scan ran reads as a failure rather than as a result, which is the opposite
        // of what this screen has to say. It is the same line the all-clear carries.
        Assert.Equal(
            string.Format(
                Strings.Completion_NothingToCleanUpReceipt,
                5, DisplayHelpers.PluraliseProduct(5),
                DisplayHelpers.FormatElapsedLong(TimeSpan.FromMilliseconds(10))),
            vm.Restore);

        // Nothing was freed, so no donate prompt and the Send tooltip takes its
        // please-send-anyway form: this cohort is the one the aggregate most needs.
        Assert.False(vm.ShowDonate);
        Assert.True(vm.LastResultFreedNothing);
        Assert.Equal(Strings.Tooltip_SendResultLog_NothingFound, vm.SendResultLogTooltip);
    }

    [Fact]
    public void The_wholesale_screen_at_one_file_does_not_say_all_1_files()
    {
        // A COUNT OF ONE IS REACHABLE, being a folder holding a single unclaimed file,
        // and the plural form renders "held back all 1 files" for it. The one-form
        // drops the numeral and names the size alone. The literal assertion below is
        // the control: formatting the right key with the wrong argument, or selecting
        // the plural at a count of one, both still satisfy a key-level assertion.
        var vm = new CompletionViewModel();

        vm.ShowNothingOffered(
            WithholdingAccount.WholeWalkOffer, withheldCount: 1, withheldBytes: 1024,
            installedProductCount: 5, scanDurationMs: 10);

        Assert.Equal(
            string.Format(
                Strings.Completion_NothingOfferedBody_Singular,
                1, DisplayHelpers.PluraliseFile(1), DisplayHelpers.FormatSize(1024)),
            vm.Summary);
        Assert.Contains("the one file", vm.Summary);
        Assert.Contains(DisplayHelpers.FormatSize(1024), vm.Summary);
        Assert.DoesNotContain("1 files", vm.Summary);

        // The one-form spends {2} alone, so the noun argument must not reach the
        // screen: "the one file (file)" is what a wrong index looks like here, and it
        // is the shape that hid the size while the value still carried {1} for it.
        Assert.DoesNotContain($"({DisplayHelpers.PluraliseFile(1)})", vm.Summary);
    }

    [Fact]
    public void The_wholesale_screen_and_the_all_clear_do_not_share_a_sentence()
    {
        // THE WHOLE POINT OF THE SECOND SCREEN, pinned so that a later tidy cannot
        // collapse them back into one. One says the folder holds nothing to remove;
        // the other says the app could not establish enough to offer anything, on a
        // machine whose folder may be full. Showing the first where the second is true
        // is a claim about somebody's disk the scan never made.
        var allClear = new CompletionViewModel();
        var nothingOffered = new CompletionViewModel();

        allClear.ShowAllClear(installedProductCount: 5, scanDurationMs: 10);
        nothingOffered.ShowNothingOffered(
            WithholdingAccount.WholeWalkOffer, withheldCount: 3, withheldBytes: 3072,
            installedProductCount: 5, scanDurationMs: 10);

        Assert.NotEqual(allClear.Heading, nothingOffered.Heading);
        Assert.NotEqual(allClear.Summary, nothingOffered.Summary);
        // The receipt IS shared, deliberately, and is the one thing that should match.
        Assert.Equal(allClear.Restore, nothingOffered.Restore);
    }

    [Fact]
    public void The_two_withholding_bodies_do_not_share_a_sentence()
    {
        // THE SAME POINT ONE LEVEL DOWN, and the level where it is easiest to lose.
        // One heading carries two bodies: one says the scan could not tell which
        // cached files belong to which installed programs, the other only that it
        // could not clear the files it judged one at a time. Each is false of the
        // other's machine, so a tidy that collapsed them would put a cause on a set
        // that did not earn it, with the heading and the receipt still matching and
        // nothing else to notice.
        var wholesale = new CompletionViewModel();
        var perFile = new CompletionViewModel();

        wholesale.ShowNothingOffered(
            WithholdingAccount.WholeWalkOffer, withheldCount: 3, withheldBytes: 3072,
            installedProductCount: 5, scanDurationMs: 10);
        perFile.ShowNothingOffered(
            WithholdingAccount.PerFile, withheldCount: 3, withheldBytes: 3072,
            installedProductCount: 5, scanDurationMs: 10);

        Assert.NotEqual(wholesale.Summary, perFile.Summary);
        // Everything else about the screen IS shared, which is what makes the body the
        // only thing carrying the difference and the only thing worth pinning.
        Assert.Equal(wholesale.Heading, perFile.Heading);
        Assert.Equal(wholesale.Restore, perFile.Restore);
    }

    [Fact]
    public void The_per_file_body_is_the_one_the_per_file_reading_renders()
    {
        // The key-level assertion the test above cannot make: NotEqual would be
        // satisfied by any two different strings, including the right key formatted
        // with the wrong arguments. This names the value.
        var vm = new CompletionViewModel();

        vm.ShowNothingOffered(
            WithholdingAccount.PerFile, withheldCount: 3, withheldBytes: 3072,
            installedProductCount: 5, scanDurationMs: 10);

        Assert.Equal(
            string.Format(
                Strings.Completion_NothingOfferedPerFileBody_Plural,
                3, DisplayHelpers.PluraliseFile(3), DisplayHelpers.FormatSize(3072)),
            vm.Summary);
    }

    // The kept-back block. One sentence since 3.0.0, naming no cause, carrying the
    // batch total. What is pinned here is that the mix of causes cannot be read off
    // it and that the number is every file held back.

    private static string Line(int count) =>
        string.Format(
            count == 1 ? Strings.Completion_HeldBack_Singular : Strings.Completion_HeldBack_Plural,
            count);

    [Fact]
    public void A_batch_kept_back_for_one_cause_carries_the_sentence_at_its_count()
    {
        var vm = new CompletionViewModel();

        vm.ShowDeleteSummary(deletedCount: 2, deletedBytes: 4096, errors: [],
            reverify: new ReverifyResult([], ["a.msp"], new HeldBackReasons(Reclaimed: 1)));

        Assert.Equal(Line(1), vm.Skipped);
    }

    [Fact]
    public void A_batch_kept_back_three_ways_carries_ONE_line_at_the_batch_total()
    {
        // Where three sentences used to print. The four counts still exist on the
        // tally and travel in the result log; what the user gets is one line whose
        // number is 2 + 1 + 1 rather than any one cause's.
        var vm = new CompletionViewModel();

        vm.ShowDeleteSummary(deletedCount: 1, deletedBytes: 4096, errors: [],
            reverify: new ReverifyResult([], ["a.msp", "b.msp", "c.msp", "d.msp"],
                new HeldBackReasons(Reclaimed: 2, RecordsChanged: 1, RecordsUnreadable: 1)));

        Assert.Equal(new[] { Line(4) }, vm.Skipped.Split(Environment.NewLine));
    }

    [Fact]
    public void The_kept_back_line_does_not_say_which_cause_a_batch_met()
    {
        // Two batches of the same size reached by different causes, one of them the
        // cause that is about the machine rather than about any file. The screen
        // cannot tell them apart, which is the whole of what "names no cause" means
        // and is worth a test rather than a comment.
        var perFile = new CompletionViewModel();
        perFile.ShowDeleteSummary(deletedCount: 1, deletedBytes: 4096, errors: [],
            reverify: new ReverifyResult([], ["a.msp", "b.msp", "c.msp"],
                new HeldBackReasons(Reclaimed: 2, RecordsChanged: 1)));

        var machineWide = new CompletionViewModel();
        machineWide.ShowDeleteSummary(deletedCount: 1, deletedBytes: 4096, errors: [],
            reverify: new ReverifyResult([], ["a.msp", "b.msp", "c.msp"],
                new HeldBackReasons(OwnershipUnestablished: 3)));

        Assert.Equal(Line(3), perFile.Skipped);
        Assert.Equal(perFile.Skipped, machineWide.Skipped);
    }

    [Fact]
    public void A_batch_that_kept_nothing_back_carries_no_line_at_all()
    {
        // The commonest run. An empty string is what collapses the block, so a
        // count of zero must not reach it as a sentence.
        var vm = new CompletionViewModel();

        vm.ShowDeleteSummary(deletedCount: 2, deletedBytes: 4096, errors: [],
            reverify: new ReverifyResult(["a.msi", "b.msi"], []));

        Assert.Equal(string.Empty, vm.Skipped);
    }

    [Fact]
    public void The_all_skipped_screen_carries_the_kept_back_line_in_its_summary()
    {
        // This screen routes the line through Summary instead of Skipped, and
        // Summary is the one completion field with no Text binding: its inlines are
        // composed in the window's code-behind. That path splits on newlines, so
        // one sentence yields one Run and no break, which is why the collapse needs
        // nothing done to it.
        var vm = new CompletionViewModel();

        vm.ShowReverifyAllSkipped(new ReverifyResult([], ["a.msp", "b.msp"],
            new HeldBackReasons(Reclaimed: 1, RecordsUnreadable: 1)), deleting: false);

        Assert.Equal(Line(2), vm.Summary);
        Assert.DoesNotContain(Environment.NewLine, vm.Summary, System.StringComparison.Ordinal);
        // The heading beside the block, because a summary naming causes under a
        // heading saying there were none is the state this screen was rewritten out
        // of. Completion_AllClean is right for a machine with nothing to do and
        // wrong here, where everything was kept back. Which of the two per-button
        // headings appears is the theory below's question, not this one's.
        Assert.Equal(Strings.Completion_NothingMoved, vm.Heading);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void The_all_skipped_screen_names_the_button_that_was_pressed(bool deleting)
    {
        // The screen only ever follows Move or Delete, so a heading naming neither
        // is a word the user never pressed. It read "Nothing removed" both ways
        // until 3.0.0, with both of these strings already in the file and already
        // picked between by ShowMoveSummary and ShowDeleteSummary.
        var vm = new CompletionViewModel();

        vm.ShowReverifyAllSkipped(
            new ReverifyResult([], ["a.msp"], new HeldBackReasons(Reclaimed: 1)), deleting);

        Assert.Equal(
            deleting ? Strings.Completion_NothingDeleted : Strings.Completion_NothingMoved,
            vm.Heading);
        // And not in the warning colour on either branch, which is what separates
        // this screen from the two it borrows the strings from. There they mean an
        // operation that got nowhere; here they mean the check ahead of the batch
        // doing its job, and painting that as a failure would contradict the one
        // rule the app is built on.
        Assert.False(vm.HeadingIsWarning);
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
