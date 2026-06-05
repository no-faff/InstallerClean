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
    public void ParseArguments_no_args_is_help()
    {
        Assert.Equal(CliCommand.Help, CliContract.ParseArguments([]).Command);
    }

    [Theory]
    [InlineData("--help")]
    [InlineData("/?")]
    [InlineData("-h")]
    public void ParseArguments_help_flags_are_help(string flag)
    {
        Assert.Equal(CliCommand.Help, CliContract.ParseArguments([flag]).Command);
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
        var result = CliContract.ParseArguments(["/m", @"D:\Backup"]);
        Assert.Equal(CliCommand.Move, result.Command);
        Assert.Equal(@"D:\Backup", result.MoveDestination);
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
}
