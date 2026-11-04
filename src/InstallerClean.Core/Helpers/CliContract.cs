using System.Diagnostics;
using InstallerClean.Services;

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
    /// <summary>
    /// An explicit help request (<c>--help</c>, <c>/?</c>, <c>-h</c>): print usage, and exit Ok
    /// where all of it was written and Error where a write failed.
    /// </summary>
    Help,

    /// <summary>
    /// A version request (<c>--version</c>, <c>-v</c>): print the version, and exit Ok where it
    /// was written and Error where the write failed.
    /// </summary>
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
/// <param name="SkipUpdate">
/// Whether <c>/noupdate</c> was given, which turns off the update check and the update
/// for this run.
/// </param>
/// <param name="NoQuestions">
/// Whether <c>/noquestions</c> was given, which turns off every question for this run
/// (<see cref="CliContract.NoQuestionsSwitch"/>).
/// </param>
internal readonly record struct CliInvocation(
    CliCommand Command,
    string? OffendingArgument,
    string? MoveDestination,
    bool SkipUpdate = false,
    bool NoQuestions = false);

/// <summary>
/// The CLI process exit codes, the contract RMM tooling and scheduled tasks
/// pin to. Defined once, in Core, so the host and its tests cannot drift
/// apart on the values.
/// </summary>
internal static class CliExitCode
{
    /// <summary>
    /// 0: the run did what it was asked.
    ///
    /// FIVE RUNS REACH IT, AND FOUR OF THEM PROCESS NOTHING. <c>/?</c> and
    /// <c>--version</c> print and stop, and a scan-only run processes nothing at all,
    /// whether it listed sixty-eight files or none. What those four are asked for is
    /// their output, so each takes this code only where every write to it went through,
    /// and <see cref="Error"/> where one failed. Only a delete or a move batch processes
    /// anything, and it takes this code when no file in it errored
    /// (<see cref="CliContract.ClassifyFileOperation"/>), whatever became of its output:
    /// what it is asked for is the files.
    ///
    /// SO IT NAMES WHAT THE FIVE SHARE AND NO CAUSE. A cause would be false of some
    /// of them, and what they share is the whole of what may be said about them
    /// together.
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
    /// argument, a batch in which every file failed, a run the app declined to
    /// perform at all, or a help request, a version request or a scan-only run
    /// whose output was not all written, its output being what each of those is
    /// asked for. Distinct from <see cref="Partial"/> so a retry policy can
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
/// entry; the rest are notices. What a consumer may rely on about the two kinds
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
    /// Notice: the scan could not check a program entry in Windows Installer's records,
    /// or could not match a cached patch file to a program it asks about, so no
    /// superseded patch was offered.
    ///
    /// Raised where <c>ScanResult.UnaccountedProductCount</c> or
    /// <c>EnumerationCensus.UnattributedPatchFileCount</c> is above nought, each a
    /// condition on which InstallerQueryService takes the removable verdict off every
    /// superseded row. A recorded path the scan could not settle withholds the same rows
    /// and does not raise it: this notice is about the records the scan could not check,
    /// not about the withholding. The Event ID is 3000 and is
    /// the wire contract; the member's name is this codebase's, and renaming it changes
    /// nothing a filter on the event log sees.
    /// </summary>
    ScanRecordsIncompleteNotice,

    /// <summary>
    /// Notice: registrations Windows holds name a file that is not in the cache.
    /// Nothing that bites today, but an update or uninstall of those programs
    /// can fail on it.
    ///
    /// IT SAYS NOTHING ABOUT WHAT REMOVED THEM AND NEITHER MAY THIS COMMENT. Any
    /// tool that deletes from that folder can leave the same record.
    /// </summary>
    ScanMissingFilesNotice,

    /// <summary>
    /// Notice: the scan held back files the folder walk found, either all of them because it
    /// could not establish which cached files belong to the programs installed here, or
    /// particular files it could not establish are unneeded. Raised where
    /// <c>ScanResult.HasWithholdingToReport</c> is true, whether or not anything else was
    /// offered, and carrying <c>ScanResult.UnestablishedWithheldCount</c>.
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
    /// genuinely was short", and this run's list was short for the same kind of
    /// reason. It is not the missing-files case, which is a standing property of a
    /// machine and repeats nightly for ever.
    ///
    /// THE PER-FILE MESSAGE NAMES NO CAUSE AND MAY NOT ACQUIRE ONE: several different
    /// findings hold particular files back, and it says what is true of all of them. The
    /// wholesale message names the condition, which that reading alone earns
    /// (<c>WithholdingAccount.WholeWalkOffer</c>).
    ///
    /// IT IS RAISED BESIDE AN OFFER TOO, so gating it on an empty offer would lose it on the
    /// machine whose walk offer was held back while a superseded file was offered.
    /// </summary>
    ScanNothingOfferedNotice,

    /// <summary>
    /// Notice: the scan held back superseded files it could not be certain are no
    /// longer needed. Raised where <c>ScanResult.SupersededHeldBackCount</c> is above
    /// nought, and carrying that count.
    ///
    /// WARNING, on the rule at <see cref="CliContract.EntryTypeFor"/>: this run's offer
    /// was short of files it would otherwise have offered, which is the reason
    /// <see cref="ScanRecordsIncompleteNotice"/> is a warning too.
    ///
    /// IT NAMES NO CAUSE: several separate findings reach the count and no sentence
    /// naming one is true of the others.
    /// </summary>
    ScanSupersededHeldBackNotice,

    /// <summary>
    /// Notice: the scan, or the check made before a Move or Delete, stopped waiting for
    /// one or more drives or shares while files still had to be checked against them, and
    /// left those files alone. The entry names the drives and shares, says which of the two
    /// passes it was, and counts the files. Raised where the pass's
    /// <c>SourceRootsGivenUpKeepingFiles</c> is not empty, so a drive or share given up
    /// with no file kept at it raises nothing. A run whose scan and check each gave one up
    /// writes it twice, once per pass.
    ///
    /// WARNING, on the rule at <see cref="CliContract.EntryTypeFor"/>: that pass left files
    /// it had not finished checking, so the run fell short of its job by those files.
    /// </summary>
    SourcesGivenUpNotice,

    /// <summary>
    /// Notice: the run found a newer version and started it in its place, naming what it was
    /// started to run. It is written once the newer version has said it started, and says
    /// nothing of what that run then does: the newer version writes the run's summary, so this entry sits
    /// beside a summary written by another process, and a 3007 where it could not take this
    /// copy's place. Information, on the rule at <see cref="CliContract.EntryTypeFor"/>:
    /// the run fell short of nothing.
    /// </summary>
    UpdatedNotice,

    /// <summary>
    /// Notice: the run found a newer version and did not run with it, for a reason that can
    /// pass by the next run (the download failed, the newer version could not be run, or
    /// InstallerClean was in use). It is written as soon as that is known and says nothing of
    /// what then runs, which the run's own summary says. Where the update itself saw Ctrl+C,
    /// whatever the reason, it says nothing ran and is the run's only entry.
    /// WARNING, on the rule at <see cref="CliContract.EntryTypeFor"/>: the run did its job with a
    /// version older than the one it set out to use, or did not do it.
    /// </summary>
    UpdateFailedNotice,

    /// <summary>
    /// Notice: the run found a newer version this copy does not update to, for a reason that
    /// holds on every run until something on the PC changes (the copy cannot tell whether its
    /// setup installed it, it was built from source, the release needs .NET Framework 4.8 or
    /// the release rules this copy out), written as soon as that is known. It shares Event ID
    /// 3006 with <see cref="UpdateFailedNotice"/>, so one filter sees every update not made.
    /// INFORMATION, on the rule at <see cref="CliContract.EntryTypeFor"/>: it is true every night
    /// on a PC with nothing wrong with it.
    /// </summary>
    UpdateNotPossibleNotice,

    /// <summary>
    /// Notice: a newer version a copy had downloaded did not put itself at that copy's
    /// path, because once the copy had closed nothing was left there. The newer version
    /// writes it, from the window's update as from the command line's. It is not a run,
    /// and is the only entry of the process writing it. WARNING, on the rule at
    /// <see cref="CliContract.EntryTypeFor"/>: the update fell short, and the copy is gone.
    /// </summary>
    UpdateNotInstalledNotice,
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
    /// The switch that turns off the update check and the update for one run. It may
    /// come anywhere on the command line, in any case. A command line updating itself
    /// starts the newer version with it, after the switches
    /// <see cref="HandOverContract.ReplaceArguments"/> builds, so the newer version goes
    /// straight to the command.
    /// </summary>
    internal const string NoUpdateSwitch = "/noupdate";

    /// <summary>
    /// The switch that turns off every question for one run, the update question and the
    /// report offer, so the run asks nothing at the console and goes on as a run nobody can
    /// answer does. It may come anywhere on the command line, in any case. A command line
    /// whose update question went unanswered starts the newer version with it, beside
    /// <see cref="NoUpdateSwitch"/>, because that run's read of the console is still waiting
    /// and would take the line typed at the newer version's question. It is there for that
    /// hand-over and is not in the help.
    ///
    /// DO NOT STOP ACCEPTING IT. A copy that passes it does so to every newer version it
    /// ever updates to (<see cref="HandOverContract.ReadReplaceRequest"/>), and a version
    /// refusing it is one that copy cannot update to after a question nobody answered.
    /// </summary>
    internal const string NoQuestionsSwitch = "/noquestions";

    /// <summary>
    /// The switch through which the version that started this one passes, after the colon,
    /// the id of an event that version sets on Ctrl+C. Only a run updating itself passes
    /// it, so it is not in the help. It goes beside the switches
    /// <see cref="HandOverContract.ReplaceArguments"/> builds.
    /// </summary>
    internal const string CancelEventSwitch = "/cancelevent:";

    /// <summary>
    /// The name of the event <paramref name="id"/> stands for, in the session's own
    /// namespace, which the newer version shares, being started by this process.
    /// </summary>
    internal static string CancelEventName(Guid id) => @"Local\InstallerClean_Cancel_" + id.ToString("N");

    /// <summary>The argument passing <paramref name="id"/> with <see cref="CancelEventSwitch"/>.</summary>
    internal static string CancelEventArgument(Guid id) => CancelEventSwitch + id.ToString("N");

    /// <summary>
    /// The name of the event whose id <see cref="CancelEventArgument"/> wrote after the
    /// colon. Answers false for anything else.
    /// </summary>
    internal static bool TryReadCancelEvent(string value, out string name)
    {
        var read = Guid.TryParseExact(value, "N", out var id);
        name = read ? CancelEventName(id) : string.Empty;
        return read;
    }

    /// <summary>
    /// Takes every <see cref="CancelEventSwitch"/> carrying an event's id out of
    /// <paramref name="args"/>, wherever it stands and in any case, and answers what is
    /// left. <paramref name="cancelEvent"/> is the first one's id, or null where there is
    /// none. One carrying anything else is left in, and <see cref="ParseArguments"/>
    /// refuses it as an argument it does not know, wherever it stands.
    /// </summary>
    internal static string[] TakeCancelEvent(string[] args, out string? cancelEvent) =>
        HandOverContract.TakeEventSwitch(args, CancelEventSwitch, out cancelEvent);

    /// <summary>
    /// Resolves the argument vector to a <see cref="CliInvocation"/>.
    /// <see cref="NoUpdateSwitch"/> and <see cref="NoQuestionsSwitch"/> are taken out first,
    /// wherever they stand, and what is left is parsed: so <c>/m /noupdate</c> is a
    /// <c>/m</c> with no path, never a <c>/m</c> to a folder of that name. The first token
    /// left decides the command, lower-cased so the comparison is case-insensitive
    /// (PowerShell users frequently type <c>/S</c>). The offending-argument and destination
    /// tokens are returned in
    /// their original case. A <see cref="CancelEventSwitch"/> or
    /// <see cref="HandOverContract.StartedEventSwitch"/> still here, carrying no event's
    /// id, is refused as an argument the command does not know, wherever it stands, so it
    /// is never read as a Move's destination, and so is a
    /// <see cref="HandOverContract.ReplaceCopySwitch"/> or
    /// <see cref="HandOverContract.AfterProcessSwitch"/>.
    /// </summary>
    internal static CliInvocation ParseArguments(string[] args)
    {
        if (args.FirstOrDefault(a => a.StartsWith(CancelEventSwitch, StringComparison.OrdinalIgnoreCase)
                || a.StartsWith(HandOverContract.StartedEventSwitch, StringComparison.OrdinalIgnoreCase)
                || a.StartsWith(HandOverContract.ReplaceCopySwitch, StringComparison.OrdinalIgnoreCase)
                || a.StartsWith(HandOverContract.AfterProcessSwitch, StringComparison.OrdinalIgnoreCase)) is { } stray)
            return new CliInvocation(CliCommand.UnknownArgument, stray, null);
        var rest = args.Where(a => !IsRunSwitch(a)).ToArray();
        return ParseCommand(rest) with
        {
            SkipUpdate = args.Any(a => Is(a, NoUpdateSwitch)),
            NoQuestions = args.Any(a => Is(a, NoQuestionsSwitch)),
        };
    }

    /// <summary>
    /// The token naming the command: the first argument that is neither
    /// <see cref="NoUpdateSwitch"/> nor <see cref="NoQuestionsSwitch"/>, as typed, or null
    /// where there is none.
    /// </summary>
    internal static string? CommandToken(string[] args) => args.FirstOrDefault(a => !IsRunSwitch(a));

    /// <summary>
    /// Whether <paramref name="arg"/> is <see cref="NoUpdateSwitch"/> or
    /// <see cref="NoQuestionsSwitch"/>, in any case: a switch about the run, taken out before
    /// the command and its path are read.
    /// </summary>
    private static bool IsRunSwitch(string arg) => Is(arg, NoUpdateSwitch) || Is(arg, NoQuestionsSwitch);

    /// <summary>Whether <paramref name="arg"/> is <paramref name="runSwitch"/>, in any case.</summary>
    private static bool Is(string arg, string runSwitch) =>
        arg.Equals(runSwitch, StringComparison.OrdinalIgnoreCase);

    private static CliInvocation ParseCommand(string[] args)
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
    /// The class of the 3006 a run that goes on with this version writes: Information where
    /// the reason holds on every run until something on the PC changes, Warning where it can
    /// pass by the next run.
    /// </summary>
    internal static CliEventClass UpdateNotMadeClass(UpdateNotMade notMade) => notMade switch
    {
        UpdateNotMade.CopyCannotTell or UpdateNotMade.FromSource or UpdateNotMade.NeedsNetFramework48
            or UpdateNotMade.CannotInstallAutomatically => CliEventClass.UpdateNotPossibleNotice,
        _ => CliEventClass.UpdateFailedNotice,
    };

    /// <summary>
    /// The stable Application-channel Event ID for an entry class. The 1000 band
    /// is "work happened" (success and partial), 2000 a transient skip, 4000 a
    /// hard failure, so an RMM filter can select an outcome by number without
    /// reading the English message. The 3000 band is the notices, which are not
    /// outcomes and are not counted as runs, except the 3006 that is the only entry of a
    /// run whose update saw Ctrl+C with nothing run. A 3007 is not a run either:
    /// the newer version writes it alone, having run no command.
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
        CliEventClass.ScanSupersededHeldBackNotice => 3003,
        CliEventClass.SourcesGivenUpNotice => 3004,
        CliEventClass.UpdatedNotice => 3005,
        CliEventClass.UpdateFailedNotice or CliEventClass.UpdateNotPossibleNotice => 3006,
        CliEventClass.UpdateNotInstalledNotice => 3007,
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
    /// superseded-files notice is Warning on the same reading: that run's offer was
    /// short of files it held back. So is the notice for the drives and shares a pass
    /// stopped waiting for: that pass left files it had not finished checking. The
    /// missing-files notice is the one that is not about the run:
    /// it is true whether or not the run worked and repeats for as long as the
    /// files are gone, so at Warning a machine with nothing wrong with it posts
    /// one nightly for ever, and the operator's answer to that is a suppression
    /// rule on Source = InstallerClean, which takes the 4000 band with it.
    ///
    /// The update notices follow the same rule. A run that ran the newer version fell
    /// short of nothing, so its notice is Information, and so is a run whose copy does not
    /// update to it for a reason true on every run, since a Warning there would post nightly
    /// on a healthy PC. A run that did not run it for a reason that can pass did its job with
    /// an older version than it set out to use, and one where Ctrl+C came did not do it, so
    /// those notices are Warning. The two classes sharing 3006 differ in it.
    /// </summary>
    internal static EventLogEntryType EntryTypeFor(CliEventClass outcome) => outcome switch
    {
        CliEventClass.Ok or CliEventClass.ScanMissingFilesNotice or CliEventClass.UpdatedNotice
            or CliEventClass.UpdateNotPossibleNotice
            => EventLogEntryType.Information,
        _ => EventLogEntryType.Warning,
    };
}
