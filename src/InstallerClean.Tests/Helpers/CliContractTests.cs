using System.Diagnostics;
using InstallerClean.Helpers;

namespace InstallerClean.Tests.Helpers;

/// <summary>
/// Pins the CLI's arg-to-command and result-to-exit-code contract that RMM
/// tooling depends on. The branch logic lives in CliContract (Core) rather than
/// in the console host, so that it is decided in one place and held here; what
/// coverage the host itself has reaches only the helpers it exposes for the
/// purpose, never a path through its work loop.
/// </summary>
/// <remarks>
/// CliCommand and CliEventClass are internal to Core (visible here via
/// InternalsVisibleTo), so they appear only in method bodies, never in a
/// public test-method signature, which CS0051 forbids.
/// </remarks>
public class CliContractTests
{
    [Fact]
    public void ParseArguments_no_args_is_a_usage_error_not_help()
    {
        // An argless run must fail visibly (a scheduled task that dropped its
        // flag), so it is distinct from an explicit --help, which stays Ok.
        Assert.Equal(CliCommand.NoArguments, CliContract.ParseArguments([]).Command);
    }

    [Theory]
    [InlineData("--help")]
    [InlineData("/?")]
    [InlineData("-h")]
    public void ParseArguments_help_flags_are_help(string flag)
    {
        Assert.Equal(CliCommand.Help, CliContract.ParseArguments([flag]).Command);
    }

    [Theory]
    [InlineData("--version")]
    [InlineData("-v")]
    [InlineData("--VERSION")]
    public void ParseArguments_version_flags_are_version(string flag)
    {
        Assert.Equal(CliCommand.Version, CliContract.ParseArguments([flag]).Command);
    }

    [Fact]
    public void ParseArguments_slash_s_is_scan_only()
    {
        Assert.Equal(CliCommand.ScanOnly, CliContract.ParseArguments(["/s"]).Command);
    }

    [Fact]
    public void ParseArguments_slash_d_is_delete()
    {
        Assert.Equal(CliCommand.Delete, CliContract.ParseArguments(["/d"]).Command);
    }

    [Fact]
    public void ParseArguments_slash_m_is_move()
    {
        Assert.Equal(CliCommand.Move, CliContract.ParseArguments(["/m"]).Command);
    }

    [Theory]
    [InlineData("/S")]
    [InlineData("/D")]
    [InlineData("/M")]
    [InlineData("--HELP")]
    public void ParseArguments_is_case_insensitive_on_the_flag(string upper)
    {
        // The flag comparison lower-cases first (PowerShell users type /S),
        // so the upper-case form resolves to the same command as the lower.
        var fromLower = CliContract.ParseArguments([upper.ToLowerInvariant()]).Command;
        var fromUpper = CliContract.ParseArguments([upper]).Command;
        Assert.Equal(fromLower, fromUpper);
    }

    [Fact]
    public void ParseArguments_unknown_flag_reports_the_token_in_original_case()
    {
        var result = CliContract.ParseArguments(["/X"]);
        Assert.Equal(CliCommand.UnknownArgument, result.Command);
        Assert.Equal("/X", result.OffendingArgument);
    }

    [Theory]
    [InlineData("/s", "extra")]
    [InlineData("/d", "extra")]
    public void ParseArguments_extra_token_for_scan_or_delete_is_too_many(string flag, string extra)
    {
        var result = CliContract.ParseArguments([flag, extra]);
        Assert.Equal(CliCommand.TooManyArguments, result.Command);
        Assert.Equal(extra, result.OffendingArgument);
    }

    [Fact]
    public void ParseArguments_move_without_path_has_no_destination()
    {
        var result = CliContract.ParseArguments(["/m"]);
        Assert.Equal(CliCommand.Move, result.Command);
        Assert.Null(result.MoveDestination);
    }

    [Fact]
    public void ParseArguments_move_with_path_carries_the_destination_untrimmed()
    {
        // Surrounding whitespace is what makes this test say anything: with a
        // clean "D:\Backup" it passes whether the parser trims or not. The
        // token is handed back exactly as the shell delivered it, spaces and
        // case alike, because a path is the caller's to state and a parser
        // that tidied it would be guessing at a folder name. The GUI trims its
        // own text box; this side does not.
        var result = CliContract.ParseArguments(["/m", "  D:\\Backup  "]);
        Assert.Equal(CliCommand.Move, result.Command);
        Assert.Equal("  D:\\Backup  ", result.MoveDestination);
    }

    [Fact]
    public void ParseArguments_move_with_a_third_token_is_too_many()
    {
        // /m accepts one path; a third token is the extra one to name (an
        // unquoted "D:\My Backup" arrives split, and silently dropping the
        // tail would move files to the wrong folder).
        var result = CliContract.ParseArguments(["/m", @"D:\My", "Backup"]);
        Assert.Equal(CliCommand.TooManyArguments, result.Command);
        Assert.Equal("Backup", result.OffendingArgument);
    }

    [Theory]
    [InlineData(5, 0, 0)]  // no errors -> success
    [InlineData(3, 2, 2)]  // some processed, some failed -> partial
    [InlineData(0, 5, 1)]  // nothing processed -> hard failure
    [InlineData(0, 0, 0)]  // nothing to process, nothing failed -> success (the
                           // act-time re-verify dropped every candidate; the CLI
                           // acts on none and the run still exits Ok)
    public void ClassifyFileOperation_maps_counts_to_exit_code(int processed, int errors, int expectedExit)
    {
        Assert.Equal(expectedExit, CliContract.ClassifyFileOperation(processed, errors).ExitCode);
    }

    [Fact]
    public void ClassifyFileOperation_no_errors_is_ok_class()
    {
        Assert.Equal(CliEventClass.Ok, CliContract.ClassifyFileOperation(5, 0).EventClass);
    }

    [Fact]
    public void ClassifyFileOperation_some_processed_some_failed_is_partial_class()
    {
        Assert.Equal(CliEventClass.Partial, CliContract.ClassifyFileOperation(3, 2).EventClass);
    }

    [Fact]
    public void ClassifyFileOperation_nothing_processed_is_hard_error_class()
    {
        Assert.Equal(CliEventClass.HardError, CliContract.ClassifyFileOperation(0, 5).EventClass);
    }

    [Theory]
    // A batch that had moved something before its destination guard stopped it:
    // partial, never Ok, because the run did not finish and the files it did
    // move have left C:\Windows\Installer for a folder the operator has to be
    // told about.
    [InlineData(1, 2)]
    [InlineData(500, 2)]
    // The guard tripped before the first file, so nothing was committed and
    // nothing is anywhere new.
    [InlineData(0, 1)]
    public void ClassifyAbortedMove_never_reports_a_stopped_batch_as_clean(int moved, int expectedExit)
    {
        Assert.Equal(expectedExit, CliContract.ClassifyAbortedMove(moved).ExitCode);
    }

    [Fact]
    public void ClassifyAbortedMove_does_not_follow_the_finished_batch_rule()
    {
        // The distinction this exists for: a stopped batch with no per-file
        // error would be Ok under ClassifyFileOperation, which reads a zero
        // error count as "every file completed".
        Assert.Equal(CliExitCode.Ok, CliContract.ClassifyFileOperation(3, 0).ExitCode);
        Assert.Equal(CliExitCode.Partial, CliContract.ClassifyAbortedMove(3).ExitCode);
        Assert.Equal(CliEventClass.Partial, CliContract.ClassifyAbortedMove(3).EventClass);
        Assert.Equal(CliEventClass.HardError, CliContract.ClassifyAbortedMove(0).EventClass);
    }

    [Fact]
    public void EventId_ok_is_1000() => Assert.Equal(1000, CliContract.EventIdFor(CliEventClass.Ok));

    [Fact]
    public void EventId_partial_is_1002() => Assert.Equal(1002, CliContract.EventIdFor(CliEventClass.Partial));

    [Fact]
    public void EventId_transient_skip_is_2000() => Assert.Equal(2000, CliContract.EventIdFor(CliEventClass.TransientSkip));

    [Fact]
    public void EventId_hard_error_is_4000() => Assert.Equal(4000, CliContract.EventIdFor(CliEventClass.HardError));

    [Fact]
    public void EntryType_ok_is_information()
    {
        Assert.Equal(EventLogEntryType.Information, CliContract.EntryTypeFor(CliEventClass.Ok));
    }

    [Fact]
    public void EntryType_partial_is_warning()
    {
        Assert.Equal(EventLogEntryType.Warning, CliContract.EntryTypeFor(CliEventClass.Partial));
    }

    [Fact]
    public void EntryType_transient_skip_is_warning()
    {
        Assert.Equal(EventLogEntryType.Warning, CliContract.EntryTypeFor(CliEventClass.TransientSkip));
    }

    [Fact]
    public void EntryType_hard_error_is_warning()
    {
        Assert.Equal(EventLogEntryType.Warning, CliContract.EntryTypeFor(CliEventClass.HardError));
    }

    [Fact]
    public void EventId_notices_have_a_band_of_their_own()
    {
        Assert.Equal(3000, CliContract.EventIdFor(CliEventClass.ScanRecordsIncompleteNotice));
        Assert.Equal(3001, CliContract.EventIdFor(CliEventClass.ScanMissingFilesNotice));
        Assert.Equal(3002, CliContract.EventIdFor(CliEventClass.ScanNothingOfferedNotice));
        Assert.Equal(3003, CliContract.EventIdFor(CliEventClass.ScanSupersededHeldBackNotice));
        Assert.Equal(3004, CliContract.EventIdFor(CliEventClass.SourcesGivenUpNotice));
        Assert.Equal(3005, CliContract.EventIdFor(CliEventClass.UpdatedNotice));
        Assert.Equal(3006, CliContract.EventIdFor(CliEventClass.UpdateFailedNotice));
        Assert.Equal(3006, CliContract.EventIdFor(CliEventClass.UpdateNotPossibleNotice));
    }

    [Fact]
    public void EventId_no_notice_collides_with_an_outcome()
    {
        // The whole reason the notices have IDs of their own: they are written
        // beside a run's single summary entry, so a consumer counting runs by
        // the summary's ID must not be able to match one of these by accident.
        var outcomes = new[]
        {
            CliEventClass.Ok, CliEventClass.Partial,
            CliEventClass.TransientSkip, CliEventClass.HardError,
        }.Select(CliContract.EventIdFor).ToHashSet();

        Assert.DoesNotContain(CliContract.EventIdFor(CliEventClass.ScanRecordsIncompleteNotice), outcomes);
        Assert.DoesNotContain(CliContract.EventIdFor(CliEventClass.ScanMissingFilesNotice), outcomes);
        Assert.DoesNotContain(CliContract.EventIdFor(CliEventClass.ScanNothingOfferedNotice), outcomes);
        Assert.DoesNotContain(CliContract.EventIdFor(CliEventClass.ScanSupersededHeldBackNotice), outcomes);
        Assert.DoesNotContain(CliContract.EventIdFor(CliEventClass.SourcesGivenUpNotice), outcomes);
        Assert.DoesNotContain(CliContract.EventIdFor(CliEventClass.UpdatedNotice), outcomes);
        Assert.DoesNotContain(CliContract.EventIdFor(CliEventClass.UpdateFailedNotice), outcomes);
        Assert.DoesNotContain(CliContract.EventIdFor(CliEventClass.UpdateNotPossibleNotice), outcomes);
    }

    [Fact]
    public void EventId_no_two_classes_share_a_number()
    {
        // A DUPLICATE IS THE ONE FAULT THE TWO TESTS ABOVE CANNOT SEE, and it is
        // the likeliest: adding a class means typing a number next to a column of
        // near-identical ones, and a second 3001 would put two different findings
        // under one filter with nothing failing. Enumerated rather than listed, so
        // the next class is covered without anybody remembering to add it here.
        //
        // The denominator is asserted rather than assumed: a walk that found no
        // members would report no duplicates, which reads exactly like a clean map.
        var classes = Enum.GetValues<CliEventClass>();
        Assert.True(classes.Length >= 7,
            $"CliEventClass has {classes.Length} members, which is too few for this walk to be "
            + "measuring what it claims.");

        //
        // The two 3006 notices share it by design, so one filter sees every update not made,
        // and are counted as one class here. Any other pair is a fault.
        var ids = classes.Where(c => c != CliEventClass.UpdateNotPossibleNotice)
            .Select(CliContract.EventIdFor).ToArray();
        var duplicated = ids.GroupBy(id => id).Where(g => g.Count() > 1).Select(g => g.Key).ToArray();

        Assert.True(duplicated.Length == 0,
            "Two event classes share an Event ID, so a filter selecting one selects both: "
            + string.Join(", ", duplicated));
    }

    [Fact]
    public void EventId_is_assigned_for_every_class()
    {
        // 0 is the switch's unmapped default, so a class added without an ID
        // would ship entries no filter can select. Nothing else enumerates the
        // enum.
        Assert.All(Enum.GetValues<CliEventClass>(),
            c => Assert.NotEqual(0, CliContract.EventIdFor(c)));
    }

    [Fact]
    public void EntryType_the_update_notices_follow_the_run()
    {
        // The run that ran the newer version fell short of nothing, and so does one whose copy
        // does not update to it for a reason true on every run; the run that did not run it for
        // a reason that can pass did its job with an older version, or did not do it.
        Assert.Equal(EventLogEntryType.Information, CliContract.EntryTypeFor(CliEventClass.UpdatedNotice));
        Assert.Equal(EventLogEntryType.Information, CliContract.EntryTypeFor(CliEventClass.UpdateNotPossibleNotice));
        Assert.Equal(EventLogEntryType.Warning, CliContract.EntryTypeFor(CliEventClass.UpdateFailedNotice));
    }

    [Fact]
    public void EntryType_the_withheld_notice_is_warning()
    {
        Assert.Equal(EventLogEntryType.Warning,
            CliContract.EntryTypeFor(CliEventClass.ScanRecordsIncompleteNotice));
    }

    [Fact]
    public void EntryType_the_missing_files_notice_is_information()
    {
        // The run-versus-machine split, which EntryTypeFor's own doc argues.
        // Most classes are Warning, so this is the arm a tidy-up folds away.
        Assert.Equal(EventLogEntryType.Information,
            CliContract.EntryTypeFor(CliEventClass.ScanMissingFilesNotice));
    }

    [Fact]
    public void EntryType_the_nothing_offered_notice_is_warning()
    {
        // IT SITS ON THE OTHER SIDE OF THAT SAME SPLIT AND THE PAIR IS WHY THIS IS
        // PINNED. Warning means THIS RUN fell short of its job, and this run offered
        // nothing at all where it would otherwise have offered something. The
        // missing-files notice is a standing property of a machine and repeats
        // nightly for ever, which is what earns it Information; this one stops the
        // moment the condition does.
        Assert.Equal(EventLogEntryType.Warning,
            CliContract.EntryTypeFor(CliEventClass.ScanNothingOfferedNotice));
    }

    [Fact]
    public void EntryType_the_drives_and_shares_given_up_notice_is_warning()
    {
        // On the run's side of the split: the pass that wrote it left files it had not
        // finished checking, and it stops the moment the drive or share answers.
        Assert.Equal(EventLogEntryType.Warning,
            CliContract.EntryTypeFor(CliEventClass.SourcesGivenUpNotice));
    }

    [Theory]
    [InlineData(new[] { "/s", "/noupdate" }, "ScanOnly", null)]
    [InlineData(new[] { "/noupdate", "/s" }, "ScanOnly", null)]
    [InlineData(new[] { "/d", "/NoUpdate" }, "Delete", null)]
    [InlineData(new[] { "/m", "/noupdate" }, "Move", null)]
    [InlineData(new[] { "/m", "D:\\Backup", "/noupdate" }, "Move", "D:\\Backup")]
    [InlineData(new[] { "/m", "/noupdate", "D:\\Backup" }, "Move", "D:\\Backup")]
    [InlineData(new[] { "/noupdate", "--help" }, "Help", null)]
    public void ParseArguments_the_no_update_switch_is_taken_out_wherever_it_stands(
        string[] args, string command, string? destination)
    {
        // /m /noupdate is a /m with no path, never a /m to a folder called /noupdate.
        var invocation = CliContract.ParseArguments(args);

        Assert.Equal(command, invocation.Command.ToString());
        Assert.Equal(destination, invocation.MoveDestination);
        Assert.True(invocation.SkipUpdate);
    }

    private const string EventId = "6f1c2a3b000040008000000000000001";
    private const string OtherEventId = "6f1c2a3b000040008000000000000002";

    [Theory]
    [InlineData(new[] { "/cancelevent:" + EventId, "/d" }, new[] { "/d" }, EventId)]
    [InlineData(new[] { "/m", "D:\\Backup", "/CancelEvent:" + EventId }, new[] { "/m", "D:\\Backup" }, EventId)]
    [InlineData(new[] { "/s", "/noupdate", "/cancelevent:" + EventId, "/cancelevent:" + OtherEventId },
        new[] { "/s", "/noupdate" }, EventId)]
    [InlineData(new[] { "/s" }, new[] { "/s" }, null)]
    [InlineData(new[] { "/s", "/cancelevent:x" }, new[] { "/s", "/cancelevent:x" }, null)]
    public void TakeCancelEvent_takes_the_switch_out_wherever_it_stands(
        string[] args, string[] rest, string? cancelEvent)
    {
        Assert.Equal(rest, CliContract.TakeCancelEvent(args, out var value));
        Assert.Equal(cancelEvent, value);
    }

    [Fact]
    public void The_cancel_event_switch_reads_back_the_event_it_was_written_with()
    {
        var id = Guid.NewGuid();

        CliContract.TakeCancelEvent([CliContract.CancelEventArgument(id), "/s"], out var taken);

        Assert.True(CliContract.TryReadCancelEvent(taken!, out var name));
        Assert.Equal(CliContract.CancelEventName(id), name);
        Assert.StartsWith(@"Local\", name);
    }

    [Theory]
    [InlineData("")]
    [InlineData("x")]
    [InlineData("1234")]
    [InlineData("{6f1c2a3b-0000-4000-8000-000000000000}")]
    public void The_cancel_event_switch_reads_nothing_but_an_id(string value)
    {
        Assert.False(CliContract.TryReadCancelEvent(value, out _));
    }

    [Theory]
    [InlineData("/d", "/cancelevent:x")]
    [InlineData("/m", "/cancelevent:x")]
    [InlineData("/cancelevent:x", "/s")]
    public void A_cancel_event_switch_carrying_no_id_is_refused_as_an_argument_the_command_does_not_know(
        string first, string second)
    {
        var invocation = CliContract.ParseArguments(CliContract.TakeCancelEvent([first, second], out _));

        Assert.Equal(CliCommand.UnknownArgument, invocation.Command);
        Assert.Equal("/cancelevent:x", invocation.OffendingArgument);
    }

    [Fact]
    public void ParseArguments_without_the_switch_the_update_is_on()
    {
        Assert.False(CliContract.ParseArguments(["/s"]).SkipUpdate);
        Assert.False(CliContract.ParseArguments(["/m", "D:\\Backup"]).SkipUpdate);
    }

    [Fact]
    public void ParseArguments_the_switch_alone_is_no_command()
    {
        Assert.Equal(CliCommand.NoArguments, CliContract.ParseArguments(["/noupdate"]).Command);
    }

    [Fact]
    public void ParseArguments_the_switch_still_counts_the_other_tokens()
    {
        // Taking the switch out leaves the rest parsed as it would be without it, so /s with
        // a stray token is still too many arguments, naming that token.
        var invocation = CliContract.ParseArguments(["/s", "/noupdate", "extra"]);

        Assert.Equal(CliCommand.TooManyArguments, invocation.Command);
        Assert.Equal("extra", invocation.OffendingArgument);
    }

    [Fact]
    public void CommandToken_is_the_first_argument_that_is_not_the_switch()
    {
        Assert.Equal("/M", CliContract.CommandToken(["/noupdate", "/M", "D:\\Backup"]));
        Assert.Equal("/s", CliContract.CommandToken(["/s", "/noupdate"]));
        Assert.Null(CliContract.CommandToken(["/NOUPDATE"]));
        Assert.Null(CliContract.CommandToken([]));
    }

    [Theory]
    [InlineData(new[] { "/s", "/noupdate", "/noquestions" }, "ScanOnly", null)]
    [InlineData(new[] { "/noquestions", "/s" }, "ScanOnly", null)]
    [InlineData(new[] { "/d", "/noupdate", "/NoQuestions" }, "Delete", null)]
    [InlineData(new[] { "/m", "/noquestions", "/noupdate" }, "Move", null)]
    [InlineData(new[] { "/m", "D:\\Backup", "/noupdate", "/noquestions" }, "Move", "D:\\Backup")]
    [InlineData(new[] { "/m", "/noquestions", "D:\\Backup" }, "Move", "D:\\Backup")]
    public void ParseArguments_the_no_questions_switch_is_taken_out_wherever_it_stands(
        string[] args, string command, string? destination)
    {
        // A newer version started after an update question nobody answered is given it beside
        // /noupdate. Refusing it, or reading it as a Move's path, would leave such a copy
        // unable to update to this version.
        var invocation = CliContract.ParseArguments(args);

        Assert.Equal(command, invocation.Command.ToString());
        Assert.Equal(destination, invocation.MoveDestination);
        Assert.True(invocation.NoQuestions);
    }

    [Fact]
    public void ParseArguments_without_the_no_questions_switch_leaves_the_questions_on()
    {
        var invocation = CliContract.ParseArguments(["/d", "/noupdate"]);

        Assert.True(invocation.SkipUpdate);
        Assert.False(invocation.NoQuestions);
    }

    [Fact]
    public void ParseArguments_the_no_questions_switch_does_not_turn_off_the_update()
    {
        var invocation = CliContract.ParseArguments(["/s", "/noquestions"]);

        Assert.False(invocation.SkipUpdate);
        Assert.True(invocation.NoQuestions);
    }

    [Fact]
    public void CommandToken_passes_over_the_no_questions_switch()
    {
        Assert.Equal("/M", CliContract.CommandToken(["/noquestions", "/noupdate", "/M", "D:\\Backup"]));
        Assert.Null(CliContract.CommandToken(["/NOQUESTIONS"]));
    }
}
