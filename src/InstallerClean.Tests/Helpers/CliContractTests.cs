using System.Diagnostics;
using InstallerClean.Helpers;

namespace InstallerClean.Tests.Helpers;

/// <summary>
/// Pins the CLI's arg-to-command and result-to-exit-code contract that RMM
/// tooling depends on. The console host (installerclean-cli) has no test
/// coverage of its own (the Tests project does not reference it), so the
/// branch logic lives in CliContract (Core) and is verified here.
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
        Assert.Equal(3000, CliContract.EventIdFor(CliEventClass.ScanWithheldNotice));
        Assert.Equal(3001, CliContract.EventIdFor(CliEventClass.ScanMissingFilesNotice));
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

        Assert.DoesNotContain(CliContract.EventIdFor(CliEventClass.ScanWithheldNotice), outcomes);
        Assert.DoesNotContain(CliContract.EventIdFor(CliEventClass.ScanMissingFilesNotice), outcomes);
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
    public void EntryType_the_withheld_notice_is_warning()
    {
        Assert.Equal(EventLogEntryType.Warning,
            CliContract.EntryTypeFor(CliEventClass.ScanWithheldNotice));
    }

    [Fact]
    public void EntryType_the_missing_files_notice_is_information()
    {
        // The run-versus-machine split, which EntryTypeFor's own doc argues.
        // Every other class is Warning, so this is the arm a tidy-up folds away.
        Assert.Equal(EventLogEntryType.Information,
            CliContract.EntryTypeFor(CliEventClass.ScanMissingFilesNotice));
    }
}
