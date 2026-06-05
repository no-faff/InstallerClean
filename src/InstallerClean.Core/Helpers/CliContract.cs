using System.Diagnostics;

namespace InstallerClean.Helpers;

/// <summary>
/// The command requested on the CLI command line, decided purely from the
/// argument vector. The console host (<c>installerclean-cli</c>) maps each
/// value to an action and a process exit code. The arg-to-command and
/// result-to-exit-code mappings live here, in Core, so they carry unit-test
/// coverage: the Tests project references Core but not the console
/// executable (the documented project-layout split), so logic that stays in
/// the host's <c>Main</c> cannot be tested at all.
/// </summary>
internal enum CliCommand
{
    /// <summary>Print usage and exit Ok (<c>--help</c>, <c>/?</c>, <c>-h</c>, or no arguments).</summary>
    Help,

    /// <summary><c>/s</c>: scan and list removable files, read-only.</summary>
    ScanOnly,

    /// <summary><c>/d</c>: scan, then send removable files to the Recycle Bin.</summary>
    Delete,

    /// <summary><c>/m</c>: scan, then move removable files to a destination.</summary>
    Move,

    /// <summary>The first token is not a recognised flag.</summary>
    UnknownArgument,

    /// <summary>A recognised flag carried more tokens than it accepts.</summary>
    TooManyArguments,
}

/// <summary>
/// A parsed command line: the <see cref="CliCommand"/> plus the two tokens
/// the host needs downstream.
/// </summary>
/// <param name="Command">The command the argument vector resolved to.</param>
/// <param name="OffendingArgument">
/// For <see cref="CliCommand.UnknownArgument"/> and
/// <see cref="CliCommand.TooManyArguments"/>, the token to name in the error
/// message, in its original case as typed (the stdout message echoes it back
/// verbatim). Null for every other command.
/// </param>
/// <param name="MoveDestination">
/// For <see cref="CliCommand.Move"/>, the optional path argument
/// (<c>installerclean-cli /m D:\Backup</c>), untrimmed; null when absent so
/// the host falls back to the saved settings destination.
/// </param>
internal readonly record struct CliInvocation(
    CliCommand Command,
    string? OffendingArgument,
    string? MoveDestination);

/// <summary>
/// The CLI process exit codes, the contract RMM tooling and scheduled tasks
/// pin to. Defined once, in Core, so the host and its tests cannot drift
/// apart on the values.
/// </summary>
internal static class CliExitCode
{
    /// <summary>0: every file the scan flagged was processed.</summary>
    public const int Ok = 0;

    /// <summary>
    /// 1: hard failure with nothing accomplished, a scan that threw, a
    /// malformed or absent argument, or a batch in which every file failed.
    /// Distinct from <see cref="Partial"/> so a retry policy can treat
    /// total failure differently from a run that did part of the work.
    /// </summary>
    public const int Error = 1;

    /// <summary>
    /// 2: partial. The operation processed some files but at least one
    /// failed. Distinct from <see cref="Error"/> so a retry policy can act
    /// on the partial case without re-running a wholesale failure.
    /// </summary>
    public const int Partial = 2;

    /// <summary>
    /// 75 (POSIX EX_TEMPFAIL): a temporary condition blocked the run, the
    /// single-instance mutex was held, a pending Windows Installer
    /// transaction blocks cache changes, or the Recycle Bin is unavailable
    /// for the volume. Distinct from <see cref="Error"/> so a
    /// retry-on-transient policy can fire here and back off on hard failure.
    /// </summary>
    public const int Transient = 75;

    /// <summary>130 (POSIX 128 + SIGINT): the run was cancelled with Ctrl+C.</summary>
    public const int Cancelled = 130;
}

/// <summary>
/// The outcome class of a CLI run. Each value carries a stable Windows
/// Event ID and entry type (see <see cref="CliContract.EventIdFor"/> /
/// <see cref="CliContract.EntryTypeFor"/>) so a consumer can classify a run
/// by Event ID, which is language-independent, instead of string-matching
/// the English summary the entry carries (the Application channel is
/// English-only by deliberate design).
/// </summary>
internal enum CliEventClass
{
    /// <summary>The run did its job: a clean scan, or every flagged file processed.</summary>
    Ok,

    /// <summary>Some work committed but at least one file failed, or a Ctrl+C landed mid-batch.</summary>
    Partial,

    /// <summary>The run was skipped or aborted before doing its job and a later run can succeed.</summary>
    TransientSkip,

    /// <summary>The run failed outright: a bad invocation, a scan that threw, or a whole batch that failed.</summary>
    HardError,
}

/// <summary>The exit code and Event-log class chosen for a finished file operation.</summary>
/// <param name="ExitCode">The process exit code to return.</param>
/// <param name="EventClass">The Event-log class for the summary entry.</param>
internal readonly record struct CliOperationOutcome(int ExitCode, CliEventClass EventClass);

/// <summary>
/// Pure decision logic for the console host: argument vector to
/// <see cref="CliCommand"/>, finished batch to exit code and Event-log
/// class, and Event-log class to its wire-format Event ID and entry type.
/// Holding this here keeps <c>Main</c> a thin Console/Environment shell and
/// puts the contract under test coverage Core can reach.
/// </summary>
internal static class CliContract
{
    /// <summary>
    /// Resolves the argument vector to a <see cref="CliInvocation"/>. The
    /// first token decides the command, lower-cased so the comparison is
    /// case-insensitive (PowerShell users frequently type <c>/S</c>). The
    /// offending-argument and destination tokens are returned in their
    /// original case.
    /// </summary>
    internal static CliInvocation ParseArguments(string[] args)
    {
        // No arguments is treated as a help request, the same as --help.
        var first = args.Length == 0 ? string.Empty : args[0].ToLowerInvariant();

        if (args.Length == 0 || first is "--help" or "/?" or "-h")
            return new CliInvocation(CliCommand.Help, null, null);

        if (first is not "/d" and not "/m" and not "/s")
            return new CliInvocation(CliCommand.UnknownArgument, args[0], null);

        // /m takes an optional second token (the destination); /s and /d
        // take none. Anything beyond is rejected so an unquoted path with
        // spaces ("/m D:\My Backup") is not silently truncated to "D:\My".
        var maxArgs = first == "/m" ? 2 : 1;
        if (args.Length > maxArgs)
            return new CliInvocation(CliCommand.TooManyArguments, args[maxArgs], null);

        var command = first switch
        {
            "/s" => CliCommand.ScanOnly,
            "/d" => CliCommand.Delete,
            _ => CliCommand.Move,
        };
        var destination = command == CliCommand.Move && args.Length > 1 ? args[1] : null;
        return new CliInvocation(command, null, destination);
    }

    /// <summary>
    /// Maps a finished move/delete batch to its exit code and Event-log
    /// class from the count actually processed and the count that errored.
    /// Shared by the <c>/d</c> and <c>/m</c> paths so both report on one
    /// axis: no errors is success, some processed with some failed is
    /// partial, nothing processed is a hard failure.
    /// </summary>
    internal static CliOperationOutcome ClassifyFileOperation(int processedCount, int errorCount)
    {
        if (errorCount == 0)
            return new CliOperationOutcome(CliExitCode.Ok, CliEventClass.Ok);
        if (processedCount > 0)
            return new CliOperationOutcome(CliExitCode.Partial, CliEventClass.Partial);
        return new CliOperationOutcome(CliExitCode.Error, CliEventClass.HardError);
    }

    /// <summary>
    /// The stable Application-channel Event ID for an outcome class. The
    /// 1000 band is "work happened" (success and partial), 2000 a
    /// transient skip, 4000 a hard failure, so an RMM filter can select an
    /// outcome by number without reading the English message.
    /// </summary>
    internal static int EventIdFor(CliEventClass outcome) => outcome switch
    {
        CliEventClass.Ok => 1000,
        CliEventClass.Partial => 1002,
        CliEventClass.TransientSkip => 2000,
        CliEventClass.HardError => 4000,
        _ => 0,
    };

    /// <summary>
    /// The Application-channel entry type for an outcome class. Only a clean
    /// success is Information; partial, transient-skip and hard-error are
    /// Warning so a "Warning and above" filter catches every run that did
    /// not fully do its job.
    /// </summary>
    internal static EventLogEntryType EntryTypeFor(CliEventClass outcome) =>
        outcome == CliEventClass.Ok ? EventLogEntryType.Information : EventLogEntryType.Warning;
}
