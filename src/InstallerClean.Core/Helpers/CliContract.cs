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
    /// <summary>An explicit help request (<c>--help</c>, <c>/?</c>, <c>-h</c>): print usage and exit Ok.</summary>
    Help,

    /// <summary>A version request (<c>--version</c>, <c>-v</c>): print the version and exit Ok.</summary>
    Version,

    /// <summary><c>/s</c>: scan and list removable files, read-only.</summary>
    ScanOnly,

    /// <summary><c>/d</c>: scan, then delete removable files permanently.</summary>
    Delete,

    /// <summary><c>/m</c>: scan, then move removable files to a destination.</summary>
    Move,

    /// <summary>
    /// No argument at all. Treated as a usage error (non-zero exit), not a
    /// help request, so an argless scheduled task fails visibly instead of
    /// "succeeding" while doing nothing. Explicit <c>--help</c> stays Ok.
    /// </summary>
    NoArguments,

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
    /// <summary>
    /// 0: the run did what it was asked and nothing in it failed.
    ///
    /// IT SAID "every file the scan flagged was processed" AND THAT IS FALSE OF
    /// THREE OF THE FIVE RUNS THAT REACH IT. <c>/?</c> and <c>/version</c> print and
    /// stop, and a scan-only run processes nothing at all, whether it listed
    /// sixty-eight files or none. Only a delete or a move batch processes anything,
    /// and it takes this code when no file in it errored
    /// (<see cref="CliContract.ClassifyFileOperation"/>).
    ///
    /// SO IT IS A SUPERORDINATE AND NOT A CAUSE, on the standing rule that no
    /// sentence may state a cause false of any member of the set it describes. What
    /// the five share is the whole of what may be said about them together.
    ///
    /// AND IT IS NOT A STATEMENT THAT NO WORK IS LEFT, which is the reading a
    /// scheduled task is likeliest to take from a zero. A scan that lists
    /// sixty-eight files has left an administrator plenty to do and still succeeded:
    /// it did what it was asked. The help text says the same thing in the same terms
    /// and the two must not drift apart, one being what a sysadmin reads and this
    /// being what a reader of the contract believes; see <c>Cli.Help.ExitCodeOk</c>,
    /// whose own note names the same five.
    /// </summary>
    public const int Ok = 0;

    /// <summary>
    /// 1: nothing was accomplished. A scan that threw, a malformed or absent
    /// argument, a batch in which every file failed, or a run the app declined to
    /// perform at all. Distinct from <see cref="Partial"/> so a retry policy can
    /// treat total failure differently from a run that did part of the work.
    ///
    /// A REFUSAL IS NOT A FAILURE AND SHARES THIS CODE ANYWAY, which is worth
    /// saying so nobody later reads one as a crash. The app declining to act is a
    /// correct outcome and the machine is not broken; what it shares with a hard
    /// failure is the posture a caller should take, which is that nothing was done
    /// and coming back will not change that on its own.
    ///
    /// THAT LAST CLAUSE IS THE WHOLE TEST FOR WHICH CODE A BLOCKED RUN TAKES, and
    /// it is why <see cref="Transient"/> is wrong for some of them.
    /// <see cref="Transient"/> is for a condition that clears by itself, so a
    /// scheduler should come back; a condition that clears only when somebody
    /// changes the machine belongs here, or a nightly retry is refused for ever.
    /// A malformed or absent argument is the plainest worked example: nothing about
    /// waiting and running again fixes it.
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
    /// single-instance mutex was held, or a pending Windows Installer
    /// transaction blocks cache changes. Distinct from <see cref="Error"/> so a
    /// retry-on-transient policy can fire here and back off on hard failure.
    /// </summary>
    public const int Transient = 75;

    /// <summary>130 (POSIX 128 + SIGINT): the run was cancelled with Ctrl+C.</summary>
    public const int Cancelled = 130;
}

/// <summary>
/// The class of one Application-channel entry. Each value carries a stable
/// Windows Event ID and entry type (see <see cref="CliContract.EventIdFor"/> /
/// <see cref="CliContract.EntryTypeFor"/>) so a consumer can classify an entry
/// by Event ID, which is language-independent, instead of string-matching
/// the English summary the entry carries (the Application channel is
/// English-only by deliberate design).
///
/// The first four are outcome classes, one of which is a run's single summary
/// entry; the last two are notices. What a consumer may rely on about the two
/// together is stated once, on the CLI host's MachineContract.WriteEventLog.
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

    /// <summary>
    /// Notice: the scan could not read every installed product's records, so the
    /// registrations it saw may be short of one and anything it reports about
    /// files missing from the cache may be short with them.
    ///
    /// It was <c>ScanWithheldNotice</c>, and what it reported was that the run had
    /// withheld the superseded and obsoleted class wholesale. The rename is right and
    /// the reason once given for it is not: a run meeting this condition DOES withhold
    /// the superseded class wholesale, InstallerQueryService taking the removable
    /// verdict off every such row, and this member is raised on exactly that condition.
    /// The justification held only between 2026-08-11 and 2026-08-17, while the class
    /// was out of the offer altogether. What the rename is actually for is that the
    /// condition now carries a second meaning, about records this scan never read, and
    /// the old name claimed only the first. The Event ID does not move with the name:
    /// 3000 is the wire contract and the name is this codebase's.
    /// </summary>
    ScanRecordsIncompleteNotice,

    /// <summary>
    /// Notice: registrations Windows holds name a file that is not in the cache.
    /// Nothing that bites today, but a repair, update or uninstall of those
    /// programs can fail on it.
    ///
    /// IT SAYS NOTHING ABOUT WHAT REMOVED THEM AND NEITHER MAY THIS COMMENT. Every
    /// tool that deletes from that folder leaves the same record, this one
    /// included up to v2.3.0.
    /// </summary>
    ScanMissingFilesNotice,

    /// <summary>
    /// Notice: the scan could not establish which cached files belong to the
    /// programs installed here, so it offered none of the files it walked.
    ///
    /// IT IS THE DISTINCTION THE OUTCOME BAND CANNOT CARRY. The run did its job
    /// and its outcome entry is <see cref="Ok"/> for that reason, so a machine
    /// nothing could be judged on and a machine that is genuinely clean sit inside
    /// one number, and the band exists precisely so a filter need not read the
    /// English to tell outcomes apart. This is the notice that separates them, and
    /// it changes no existing number's meaning.
    ///
    /// WARNING RATHER THAN INFORMATION, on the rule stated at
    /// <see cref="CliContract.EntryTypeFor"/>: Warning means THIS RUN fell short of
    /// its job. The sibling notice at 3000 is Warning because "that run's list
    /// genuinely was short", and this run's list was empty for the same kind of
    /// reason. It is not the missing-files case, which is a standing property of a
    /// machine and repeats nightly for ever.
    ///
    /// IT NAMES NO CAUSE AND MAY NOT ACQUIRE ONE. Several different findings empty
    /// a walk-derived offer this way; the message says what is true of all of them.
    /// </summary>
    ScanNothingOfferedNotice,
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
        // No argument is a usage error, not a help request: an argless
        // scheduled task must fail visibly rather than silently do nothing.
        // An explicit --help / /? / -h is the deliberate request and stays Ok.
        if (args.Length == 0)
            return new CliInvocation(CliCommand.NoArguments, null, null);

        var first = args[0].ToLowerInvariant();

        if (first is "--help" or "/?" or "-h")
            return new CliInvocation(CliCommand.Help, null, null);

        if (first is "--version" or "-v")
            return new CliInvocation(CliCommand.Version, null, null);

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
    ///
    /// The result log labels the same batch from the same two counts
    /// (<c>ResultLogEntry</c>'s <c>ClassifyOutcome</c>). One operation, two
    /// readings of it, so a change to this rule belongs in both.
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
    /// Maps a Move that one of the service's own destination guards stopped part
    /// way to its exit code and Event-log class.
    ///
    /// Never Ok, however few files failed: the batch did not finish, so a
    /// scheduled task must not read the run as clean. It is decided on the moved
    /// count alone rather than through
    /// <see cref="ClassifyFileOperation(int, int)"/>, which would answer Ok for a
    /// stopped batch that happened to collect no per-file error.
    /// </summary>
    internal static CliOperationOutcome ClassifyAbortedMove(int movedCount) =>
        movedCount > 0
            ? new CliOperationOutcome(CliExitCode.Partial, CliEventClass.Partial)
            : new CliOperationOutcome(CliExitCode.Error, CliEventClass.HardError);

    /// <summary>
    /// The stable Application-channel Event ID for an entry class. The 1000 band
    /// is "work happened" (success and partial), 2000 a transient skip, 4000 a
    /// hard failure, so an RMM filter can select an outcome by number without
    /// reading the English message. The 3000 band is the notices, which are not
    /// outcomes and are not counted as runs.
    /// </summary>
    internal static int EventIdFor(CliEventClass outcome) => outcome switch
    {
        CliEventClass.Ok => 1000,
        CliEventClass.Partial => 1002,
        CliEventClass.TransientSkip => 2000,
        CliEventClass.HardError => 4000,
        CliEventClass.ScanRecordsIncompleteNotice => 3000,
        CliEventClass.ScanMissingFilesNotice => 3001,
        CliEventClass.ScanNothingOfferedNotice => 3002,
        _ => 0,
    };

    /// <summary>
    /// The Application-channel entry type for an entry class. Warning means THIS
    /// RUN fell short of its job; Information means a standing property of the
    /// machine. That reading is not new: while one entry per run was the whole
    /// contract, "source InstallerClean, level Warning" and "a run that did not
    /// fully do its job" were the same statement, and every consumer written
    /// before the notices existed was built on it.
    ///
    /// Hence a clean success Information, partial / transient-skip / hard-error
    /// Warning, and the withheld notice Warning because that run's list genuinely
    /// was short. The nothing-offered notice is Warning on that same reading and
    /// not on the missing-files one: that run's list was not merely short, it was
    /// empty, and it will be empty again on the next run only for as long as the
    /// condition holds rather than for as long as some files are absent. The
    /// missing-files notice is the one that is not about the run:
    /// it is true whether or not the run worked and repeats for as long as the
    /// files are gone, so at Warning a machine with nothing wrong with it posts
    /// one nightly for ever, and the operator's answer to that is a suppression
    /// rule on Source = InstallerClean, which takes the 4000 band with it.
    /// </summary>
    internal static EventLogEntryType EntryTypeFor(CliEventClass outcome) => outcome switch
    {
        CliEventClass.Ok or CliEventClass.ScanMissingFilesNotice => EventLogEntryType.Information,
        _ => EventLogEntryType.Warning,
    };
}
