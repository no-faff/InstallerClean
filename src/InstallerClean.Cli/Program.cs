using System.Diagnostics;
using System.Text;
using Microsoft.Extensions.DependencyInjection;
using InstallerClean.Helpers;
using InstallerClean.Models;
using InstallerClean.Resources;
using InstallerClean.Services;

namespace InstallerClean.Cli;

/// <summary>
/// Console entry point. A real .NET console exe (subsystem CONSOLE)
/// so PowerShell, cmd and scheduled tasks block on the process
/// naturally. Resolves services from a CLI-only DI container that
/// knows nothing about MessageBox, Window or MainViewModel: the only
/// shared surface with the GUI is <see cref="Services.CoreComposition.AddInstallerCleanCore"/>.
/// The arg-to-command and result-to-exit-code decisions live in
/// <see cref="CliContract"/> (Core) so they carry unit-test coverage; this
/// host stays a thin Console/Environment shell around them.
/// </summary>
internal static class Program
{
    // Exit codes are defined once in CliExitCode (Core) so the host and its
    // tests cannot drift on the contract RMM tooling pins to; the per-code
    // rationale lives on the CliExitCode members. Aliased here so the body
    // reads ExitOk / ExitError rather than the qualified form at every return.
    private const int ExitOk = CliExitCode.Ok;
    private const int ExitError = CliExitCode.Error;
    private const int ExitPartial = CliExitCode.Partial;
    private const int ExitTransient = CliExitCode.Transient;
    private const int ExitCancelled = CliExitCode.Cancelled;

    public static int Main(string[] args)
    {
        // A newer version a copy downloaded, started to put itself at that copy's path once
        // the copy has ended, does that and nothing else, printing nothing: it runs with no
        // console of its own. Exit code 0 where the newer version is in place, 1 otherwise,
        // with what failed in crash.log and the Application log. A request carrying a command
        // runs that command standing for the copy at the path, and then starts the replace;
        // where the process it names does not run from the file at the path, it runs nothing,
        // sets no event and writes crash.log a line saying so. A replace switch in a request
        // not well formed goes on to the arguments' own check, which refuses it as an argument
        // the command does not know, with the usage error and its Application-log entry; the
        // started event it may carry is taken out unset, so no copy reads this run as the
        // newer version starting, and crash.log says which it was.
        s_standsFor = null;
        s_copyHeld?.Dispose();
        s_copyHeld = null;
        string? startedEvent;
        switch (HandOverContract.ReadReplaceRequest(args, out var replaceRequest))
        {
            case ReplaceRead.Asked when replaceRequest!.Command.Count == 0:
                StorageHelpers.TurnOffCriticalErrorBoxForTheProcess();
                return CommandLineReplacement.Run(replaceRequest) ? ExitOk : ExitError;
            case ReplaceRead.Asked:
                StorageHelpers.TurnOffCriticalErrorBoxForTheProcess();
                s_copyHeld = StagedRun.HoldCopy(replaceRequest);
                if (s_copyHeld is null)
                {
                    CrashLog.TryWrite(new ArgumentException(HandOverContract.NotTheCopyRefusal));
                    return ExitError;
                }
                s_standsFor = replaceRequest;
                args = [.. replaceRequest.Command];
                break;
            case ReplaceRead.Refused:
                CrashLog.TryWrite(new ArgumentException(HandOverContract.ReplaceNotWellFormedRefusal));
                args = HandOverContract.TakeStartedEvent(args, out _);
                break;
        }

        CancelKey.Process.Hook();

        // A version that updated itself passes the one it starts an event it sets on Ctrl+C,
        // and one it waits on until that version sets it. Their switches come out here,
        // before anything reads, prints or records the arguments, and the second is set at
        // once: the copy that started this one ends it and goes on as itself where it is not
        // set in time. Where it cannot be set, this run ends here with the error code,
        // having printed and run nothing, and the crash log says why. The copy that started
        // it writes the Application-log entry for the update. A run standing for another copy
        // has its started event in the request.
        args = CliContract.TakeCancelEvent(args, out var cancelEvent);
        args = HandOverContract.TakeStartedEvent(args, out startedEvent);
        if (!HandOverContract.SignalStarted(s_standsFor?.StartedEvent ?? startedEvent)) return ExitError;
        if (cancelEvent is not null) CancelKey.Process.Watch(cancelEvent);

        // No critical-error box for the whole run: a read reaching a drive with no disc or card in
        // it answers with an error rather than waiting on a box that a scheduled task has nobody
        // to dismiss.
        StorageHelpers.TurnOffCriticalErrorBoxForTheProcess();

        // A throw out of Run (a name or ACL clash constructing the single-instance
        // mutex, say) would otherwise reach the runtime's default handler:
        // ex.ToString() to stderr (a cross-profile path leak under elevation), no
        // Application-log record, and an undocumented exit code no RMM can branch
        // on. Route any such throw through the same crash-log + audit + ExitError
        // path the work loop uses, and the note where the Application log refused
        // an entry, which prints once in a run wherever it is printed from.
        // Run holds the single-instance mutex on this thread (acquire and release
        // both here, per the Win32 owner-thread rule) and guards its own cleanup, and
        // the work reports what it throws through its own catches (DoWorkAsync), so
        // what lands here is a throw from Run's pre-flight or one thrown inside one of
        // the work's catches.
        //
        // The code page is set and restored around all of that, the catch-all
        // included, so the crash line still goes out in UTF-8 and the console is
        // handed back last.
        //
        // EVERYTHING THE RUN PRINTS GOES THROUGH ConsoleGuard, installed after the
        // code page is set. A write that fails loses its own text and never ends the
        // run, so no failed write reaches this catch-all or the work loop's. Run is
        // handed the guard, so a run whose product is its output can answer for a
        // write that failed.
        var previousOutputEncoding = TrySetUtf8Output();
        int exitCode;
        try
        {
            var output = new ConsoleGuard(Console.Out);
            Console.SetOut(output);
            exitCode = Run(args, output);
        }
        catch (Exception ex)
        {
            exitCode = ReportRunFailure(args, ex);
        }
        finally
        {
            if (!_newerVersionHasConsole) RestoreOutputEncoding(previousOutputEncoding);
        }

        // A run standing for another copy starts the replace once its command has ended,
        // however it ended, and ends with the command's exit code whether or not the replace
        // said it had started: the copy it stands for ends with that code.
        if (s_standsFor is { } standsFor) StartReplace(standsFor);
        s_copyHeld?.Dispose();
        s_copyHeld = null;
        return exitCode;
    }

    /// <summary>
    /// The request this run stands for another copy under, where it runs a command for an
    /// older copy that downloaded it (<see cref="StagedRun"/>), and null on every other run.
    /// What the run decides from its own path it decides from the request's path instead.
    /// </summary>
    private static ReplaceRequest? s_standsFor;

    /// <summary>
    /// The process of the copy this run stands for, held open from the check that it runs
    /// from the file at the path until the replace has been started, so its process ID names
    /// that process and no other when the replace opens it.
    /// </summary>
    private static Microsoft.Win32.SafeHandles.SafeProcessHandle? s_copyHeld;

    /// <summary>
    /// The path of the copy this run is: the copy it stands for where it stands for one, and
    /// otherwise its own, or null where the process has no path.
    /// </summary>
    private static string? CopyPath =>
        s_standsFor?.ExePath ?? (Environment.ProcessPath is { Length: > 0 } own ? own : null);

    /// <summary>
    /// Test seam: starts the replace for the request a run stood for once its command has
    /// ended (<see cref="StagedRun.StartReplace(ReplaceRequest, string)"/>), so a test of a run standing for a copy
    /// starts nothing.
    /// </summary>
    internal static Action<ReplaceRequest> StartReplace { get; set; } =
        request => StagedRun.StartReplace(request, Environment.ProcessPath ?? string.Empty);

    /// <summary>
    /// Whether this run ends while the newer version it started may still be running in this
    /// console. The newer version then writes in the code page it found, UTF-8, and puts back
    /// that code page as it ends, so this run leaves the code page alone rather than change it
    /// under the newer version's output.
    /// </summary>
    private static bool _newerVersionHasConsole;

    /// <summary>Called where this run ends with the newer version it started perhaps still running in this console.</summary>
    internal static void LeaveConsoleToNewerVersion() => _newerVersionHasConsole = true;

    /// <summary>
    /// Reports an exception thrown out of <see cref="Run"/> to <see cref="Main"/>'s catch-all,
    /// as <see cref="ReportUnexpectedError(string, Exception)"/> reports one, then prints the
    /// note where the Application log has refused an entry and the run has not printed it,
    /// and returns its exit code.
    /// </summary>
    internal static int ReportRunFailure(string[] args, Exception ex)
    {
        var exitCode = ReportUnexpectedError(CliContract.CommandToken(args)?.ToLowerInvariant() ?? "(none)", ex);
        NoteEventLogUnavailable();
        return exitCode;
    }

    /// <summary>
    /// Pins stdout to UTF-8 so a <c>Cli.*</c> translation into a non-ASCII
    /// language does not mojibake under redirected output
    /// (<c>cmd /c installerclean-cli /s > out.txt</c>) or PowerShell 5's OEM
    /// default code page. Returns the encoding that was in force, for
    /// <see cref="RestoreOutputEncoding"/>, or null when nothing was changed.
    /// </summary>
    /// <remarks>
    /// Failure is swallowed, and that asymmetry with the rest of the pre-flight is
    /// the point: the setter calls SetConsoleOutputCP, which fails when the process
    /// has no console at all (a DETACHED_PROCESS launcher, a session-0 service
    /// wrapper), and the BCL surfaces that as an IOException. What is lost is
    /// cosmetic, correct glyphs in non-ASCII stdout nobody is reading on a headless
    /// run; what letting it escape would cost is the whole run, every run, with the
    /// cache never cleaned.
    /// </remarks>
    private static Encoding? TrySetUtf8Output()
    {
        try
        {
            var previous = Console.OutputEncoding;
            Console.OutputEncoding = Encoding.UTF8;
            return previous;
        }
        catch (Exception)
        {
            // Either the read or the write failed, so nothing was changed and
            // there is nothing to give back. Carry on in the inherited code page.
            return null;
        }
    }

    /// <summary>
    /// Puts the console output code page back the way this process found it.
    /// </summary>
    /// <remarks>
    /// The code page belongs to the console, and a child process shares the
    /// console of the shell that launched it, so a run that leaves it on 65001
    /// leaves every later command in that window there too. It costs nothing on
    /// the path the UTF-8 setting exists for, a scheduled task getting a console
    /// of its own; interactive use is what was paying for it.
    /// </remarks>
    private static void RestoreOutputEncoding(Encoding? previous)
    {
        if (previous is null) return;
        try
        {
            Console.OutputEncoding = previous;
        }
        catch (Exception)
        {
            // Nothing more can be done for the console here, and nothing left to
            // tell.
        }
    }

    private static int Run(string[] args, ConsoleGuard output)
    {
        // Human-facing stdout follows the OS UI culture: Italian on an Italian
        // machine, Japanese on a Japanese one; a locale with no satellite falls
        // back through the resx hierarchy to neutral English. CurrentCulture is
        // left untouched, so sizes format in the OS region ("3,2 GB"). The lines
        // other software reads stay English regardless: the Application-channel
        // Event Log (RMM greps it for known English phrases) and the
        // "\d+ errors:" stdout header, whose shape is held for a script scrape,
        // are built through MachineContract, which forces en-GB at the emit
        // site. The count in that header and the "[i/total]" progress lines are
        // plain integers that group in no culture, so only the "errors" noun
        // needs forcing, not the numbers.

        var invocation = CliContract.ParseArguments(args);
        switch (invocation.Command)
        {
            // What help and version are asked for is their output, so a write to it that
            // failed fails the run. Neither writes an Application-log entry either way.
            case CliCommand.Help:
                PrintUsage();
                return output.Failed ? ExitError : ExitOk;
            case CliCommand.Version:
                PrintVersion();
                return output.Failed ? ExitError : ExitOk;
            case CliCommand.NoArguments:
                // An argless run is a misconfiguration, most often a scheduled
                // task that dropped its flag. Print usage like --help but exit
                // non-zero and leave an audit record, so the failure is visible
                // instead of a silent "success" that did nothing.
                PrintUsage();
                MachineContract.WriteEventLog(CliEventClass.HardError, () => Strings.Cli_EventLogNoArguments);
                NoteEventLogUnavailable();
                return ExitError;
            case CliCommand.UnknownArgument:
                return ReportBadArguments(invocation,
                    string.Format(Strings.Cli_UnknownArgument, invocation.OffendingArgument));
            case CliCommand.TooManyArguments:
            {
                // A recognised flag carried an extra token, and which flag decides
                // the answer. /m takes a path, so the extra token is usually an
                // unquoted one with a space ("/m D:\My Backup" splits into three,
                // leaving "Backup" over) and the hint names the quoting fix rather
                // than calling the user's own path fragment an unknown flag. /s and
                // /d take no path, so the way in is combining flags, which the
                // quoting advice answers about a flag they did not type. The audit
                // entry and exit code are identical either way; only the hint moves.
                var takesAPath = string.Equals(CliContract.CommandToken(args), "/m", StringComparison.OrdinalIgnoreCase);
                return ReportBadArguments(invocation, string.Format(
                    takesAPath ? Strings.Cli_TooManyArguments : Strings.Cli_TooManyArgumentsNoPath,
                    invocation.OffendingArgument));
            }
        }

        // Only the work commands (/s, /d, /m) remain. The lower-cased flag
        // drives the EventLog mode label and the /d-or-/m mutex check.
        var arg = CliContract.CommandToken(args)!.ToLowerInvariant();

        // A run without administrator rights is refused here, before the update and
        // the single-instance mutex, so it scans nothing and meets no
        // other InstallerClean process. The manifest asks Windows for the rights,
        // and Windows can still start the tool without them, a compatibility setting
        // being one way, so the tool asks its own token; AdministratorRights says
        // what the scan needs them for. Help and --version return above and need no
        // rights.
        if (!AdministratorRights.Held())
            return RefuseWithoutAdministratorRights(arg);

        using var cts = new CancellationTokenSource();
        var updateHost = NewUpdateHost(arg, invocation);

        // A Ctrl+C from here until the run returns cancels the run, before the update and the
        // mutex as well as during the work, and one that came before this line does so as it
        // runs. The registration is released as the run returns.
        using var onCtrlC = CancelKey.Process.Answer(CancelRun);

        // Runs once, on the console's own control-handler thread or, where Ctrl+C came
        // before the registration, on this one. Nothing in here may throw: on the
        // control-handler thread Main's try/catch does not cover it, so an escape is
        // unhandled, a runtime abort and a stack trace on stderr in place of the
        // documented exit code and the single Application-log entry, on a surface that
        // deliberately keeps ex.Message out.
        void CancelRun()
        {
            // Once the newer version this run updated to is being started or running in this
            // console, it gets the same Ctrl+C, through the console or the event this run
            // passes it. It prints the notice and stops its own work, so this run holds its
            // own line back, and goes on waiting for its exit code and its summary; the host
            // prints the line where the start fails or Ctrl+C ends the newer version before
            // it can answer it. The token is cancelled all the same, which is how the run
            // knows the Ctrl+C came in that last case.
            if (updateHost.TakeCancellingLine())
                SayCancelling(Strings.Cli_Cancelling);
            // Cancelled whether or not the line went out, so the run stops either way. The
            // run may have returned and disposed the source meanwhile, which throws here.
            try
            {
                cts.Cancel();
            }
            catch (Exception ex)
            {
                Helpers.CrashLog.TryWrite(ex);
            }
        }

        // What a replace left beside this exe, then the staging folders earlier updates left
        // in %ProgramData%; a staging folder a running update holds is left whole, its handle
        // sharing no delete. A run standing for another copy runs from the staging folder
        // and leaves that copy's folder, and the staging folders, to the copy itself.
        if (s_standsFor is null && Environment.ProcessPath is { Length: > 0 } ownPath)
        {
            ExeReplacement.TryRemoveLeftovers(ownPath);
            UpdateStagingFolder.RemoveLeftovers(UpdateStagingFolder.DefaultRoot);
        }

        var update = cts.IsCancellationRequested
            ? UpdateConclusion.GoesOn
            : RunUpdate(arg, args, invocation, updateHost, cts.Token);
        if (update.ExitCode is int updateExitCode) return updateExitCode;

        // A Ctrl+C before the update, one the update did not end the run on, or one that
        // came where no update was checked for, stops the run here, before the mutex and
        // the scan.
        if (cts.IsCancellationRequested) return ReportCancelledBeforeWork(arg);

        // Made before the take, so nothing between the take and the try that releases the
        // mutex can throw.
        var report = new RunReport(MayAsk(ConsoleCanAsk(), updateHost));
        var take = TakeInstanceLock(arg, s_standsFor is not null, () => new InstanceLock(), update.RunningInstead, cts.Token,
            () => new StagedRunCover(InstanceLock.Name,
                s_copyHeld ?? throw new InvalidOperationException("A run standing for a copy holds no handle on it.")));
        if (take.ExitCode is int blockedExitCode) return blockedExitCode;
        var instanceLock = take.Lock;
        var cover = take.Cover;
        int exitCode;
        try
        {
            // Sync-over-async wrapper so the acquired mutex (held on the
            // Main thread per its Win32 owner-thread rule) is released
            // by the same thread. Without this, the first await inside
            // RunWorkAsync would hop the continuation onto a thread-pool
            // thread and the finally's ReleaseMutex would throw
            // ApplicationException, orphaning the mutex until process
            // exit and handing the next instance an abandoned
            // mutex. A console Main has no
            // SynchronizationContext, so GetResult here cannot deadlock
            // on captured-context resumption.
            exitCode = RunWorkAsync(arg, invocation, cts.Token, output: output, report: report, cover: cover)
                .GetAwaiter().GetResult();
        }
        finally
        {
            // RunWorkAsync has already written this run's summary entry, and printed
            // the note about an Application log that refused it, by the time it
            // returns, so nothing in this cleanup may reach Main's catch-all: that
            // writes a second, and one summary per run is what an RMM counts runs by.
            try
            {
                (cover as IDisposable)?.Dispose();
                instanceLock?.Release();
                instanceLock?.Dispose();
            }
            catch (Exception ex)
            {
                Helpers.CrashLog.TryWrite(ex);
            }
        }

        // The question comes after the mutex is released, so a person reading it holds up
        // neither the window nor another run, and after the run's summary entry, so the
        // exit code and the entry are the work's whatever the answer is. The tip line
        // a Delete or Move held back follows it.
        OfferReport(report, updateHost, cts.Token);
        return exitCode;
    }

    /// <summary>How long the PC's first-run check is waited for before a report is carried, as the window waits.</summary>
    private static readonly TimeSpan ReportCheckBound = TimeSpan.FromSeconds(3);

    /// <summary>
    /// The kind of copy this command-line tool is, as the report names it
    /// (<see cref="AppCopy.DetectRunningCommandLine"/>), decided from <see cref="CopyPath"/>,
    /// or not known where there is none.
    /// </summary>
    private static AppCopyKind RunningCopy(IServiceProvider services) =>
        CopyPath is { } exePath
            ? AppCopy.DetectRunningCommandLine(exePath, services.GetRequiredService<IRegistryReader>())
            : AppCopyKind.Undetermined;

    /// <summary>
    /// After the work of a run, at a console somebody can answer in: offers to send the
    /// report of a run that was the PC's first and finished
    /// (<see cref="RunReport.Entry"/>), or sends the report an earlier run was told to
    /// send and could not, and then prints the tip line a Delete or Move held back
    /// (<see cref="WriteHeldDonation"/>). Nothing is asked or sent where nobody can answer,
    /// or where Ctrl+C has come. Never throws.
    /// </summary>
    internal static void OfferReport(RunReport report, IConsolePrompt console, CancellationToken token)
    {
        if (!report.CanAsk || token.IsCancellationRequested) return;
        try
        {
            OfferReport(report, console, new ResultLogService(), new SettingsService(), token);
        }
        catch (Exception ex)
        {
            Helpers.CrashLog.TryWrite(ex);
            WriteHeldDonation(report, ReportOfferOutcome.NotOffered, token);
        }
    }

    /// <summary>The offer and the tip line over the services they use, which a test supplies.</summary>
    internal static void OfferReport(
        RunReport report, IConsolePrompt console, IResultLogService resultLog, ISettingsService settings,
        CancellationToken token)
    {
        if (!report.CanAsk || token.IsCancellationRequested) return;
        var offer = new ConsoleReportOffer(
            console, resultLog, settings,
            Path.Combine(AppDataFolder.Location, "last-run.json"),
            ReadmeLinks.For("reports-stats", Localisation.UiCulture),
            () => CancelKey.Process.Closing,
            CommandLineUpdate.ControlCBound,
            ConsoleQuestion.AnswerBound);
        var outcome = report.Entry is { } entry
            ? offer.Offer(entry, token)
            : offer.SendWaiting(token);
        WriteHeldDonation(report, outcome, token);
    }

    /// <summary>
    /// The run's own take of the single-instance mutex, and the line saying it runs with this
    /// version. Mutate-the-cache operations (/d and /m) take the same mutex the WPF GUI uses,
    /// so a command-line run cannot race the GUI mid-delete or mid-move, and a second
    /// command-line run is turned away too. /s is read-only and takes none.
    /// For /d and /m the copy a run stands for (<paramref name="standsForCopy"/>) holds it until
    /// this run has ended (HandOverContract.ReadReplaceRequest), so such a run takes no lock
    /// here and is given the cover <paramref name="newCover"/> makes
    /// (<see cref="StagedRunCover"/>), which answers whether that copy's hold or its own covers
    /// the run before it acts, for the caller to dispose once the run has ended.
    /// Answers the lock for the caller to release on this same thread, or none where nothing
    /// was taken; or the exit code of a run the mutex turned away, which has said so and
    /// written its entry.
    /// Where the update itself failed or InstallerClean was in use,
    /// <paramref name="runningInstead"/> builds the line saying the run goes on with this
    /// version, printed once the run does go on: after the take, and not after Ctrl+C. It is
    /// guarded where it stands, the lock being held and the caller's release not yet reached.
    /// </summary>
    internal static LockTake TakeInstanceLock(
        string arg, bool standsForCopy, Func<InstanceLock> newLock, Func<string>? runningInstead,
        CancellationToken token, Func<IRunCover>? newCover = null)
    {
        InstanceLock? instanceLock = null;
        IRunCover? cover = null;
        if (arg is "/d" or "/m" && standsForCopy)
        {
            // The cover's own thread makes the run's take (StagedRunCover). A Move or Delete
            // standing for a copy never goes on without one.
            cover = (newCover ?? throw new ArgumentNullException(nameof(newCover)))();
        }
        else if (arg is "/d" or "/m")
        {
            instanceLock = newLock();
            if (!instanceLock.TryTake())
            {
                // Nothing on this path may reach Main's catch-all (ReportMutexBlocked), and
                // the dispose below is guarded where it stands for the same reason.
                ReportMutexBlocked(arg);
                NoteEventLogUnavailable();
                try
                {
                    instanceLock.Dispose();
                }
                catch (Exception ex)
                {
                    Helpers.CrashLog.TryWrite(ex);
                }
                return new LockTake(null, ExitTransient);
            }
        }

        if (runningInstead is not null && !token.IsCancellationRequested)
        {
            try
            {
                Console.WriteLine(runningInstead());
            }
            catch (Exception ex)
            {
                Helpers.CrashLog.TryWrite(ex);
            }
        }
        return new LockTake(instanceLock, null, cover);
    }

    /// <summary>
    /// What <see cref="TakeInstanceLock"/> answers: the lock it took, or the exit code of a run
    /// the mutex turned away, and the cover of a run standing for a copy.
    /// </summary>
    internal readonly record struct LockTake(InstanceLock? Lock, int? ExitCode, IRunCover? Cover = null);

    /// <summary>
    /// Says a Move or Delete is not run because the single-instance mutex is held elsewhere,
    /// and writes its entry, and answers <see cref="ExitTransient"/>.
    /// </summary>
    private static int ReportMutexBlocked(string arg)
    {
        // RMM consumer polling the Application channel for InstallerClean entries needs an
        // audit record on the skipped path to distinguish it from "the task never fired".
        ReportMutexStop(() => [Strings.Cli_MutexBlocked], CliEventClass.TransientSkip,
            () => string.Format(Strings.Cli_EventLogMutexBlocked, arg));
        return ExitTransient;
    }

    /// <summary>
    /// Prints the lines <paramref name="lines"/> builds and writes the run's entry, of
    /// <paramref name="eventClass"/> with <paramref name="entry"/>, for a Move or Delete the
    /// single-instance mutex turned away or no longer covered. Never throws.
    /// </summary>
    private static void ReportMutexStop(
        Func<IEnumerable<string>> lines, CliEventClass eventClass, Func<string> entry)
    {
        // Nothing here may reach Main's catch-all, and the two halves of it fail
        // differently. A throw before the write costs the run its class: a 2000
        // TransientSkip and exit 75, the one code a scheduler retries on, become a 4000
        // HardError and exit 1. A throw after it produces that 4000 as this run's SECOND
        // entry, which is what an RMM counts runs by. So the lines are guarded where they
        // stand, and the write is guarded inside EventLogWriter.Write, which builds the
        // entry there so that no part of the call is evaluated in this frame.
        try
        {
            foreach (var line in lines()) Console.WriteLine(line);
        }
        catch (Exception ex)
        {
            Helpers.CrashLog.TryWrite(ex);
        }
        MachineContract.WriteEventLog(eventClass, entry);
    }

    /// <summary>
    /// What the update made before a run ended in, as <see cref="RunUpdate"/> answers it: the
    /// exit code where the run ends there, or, where it goes on, the line saying it goes on
    /// with this version, where one is said.
    /// </summary>
    private readonly record struct UpdateConclusion(int? ExitCode, Func<string>? RunningInstead)
    {
        /// <summary>The run goes on, with nothing said about the update.</summary>
        public static UpdateConclusion GoesOn => default;
    }

    /// <summary>
    /// The update made before a /s, /d or /m (<see cref="CommandLineUpdate"/>). Answers the
    /// exit code where the run ends here, as <see cref="ConcludeUpdate"/> decides from what the
    /// update ended in, or that the run goes on with this version, with the line saying so
    /// where the update itself failed or InstallerClean was in use.
    ///
    /// Nothing is checked where <c>/noupdate</c> was given, this account's settings turn
    /// the check off, or the run stands for another copy, being that copy's update itself.
    /// A throw from building what the update needs goes to crash.log and the run goes on, as
    /// it does after an update that fails with this version's file at its path.
    /// </summary>
    private static UpdateConclusion RunUpdate(
        string arg, string[] args, CliInvocation invocation, ConsoleUpdateHost host, CancellationToken token)
    {
        CommandLineUpdateResult result;
        try
        {
            if (invocation.SkipUpdate || s_standsFor is not null || Environment.ProcessPath is not { Length: > 0 } exePath)
                return UpdateConclusion.GoesOn;
            if (!new SettingsService().Load().AutoUpdateCheck)
                return UpdateConclusion.GoesOn;

            var canAsk = MayAsk(ConsoleCanAsk(), host);
            var updater = new AppUpdater(
                AppCopy.DetectRunningCommandLine(exePath, new RegistryReader()),
                exePath,
                new UpdateDownloadService(),
                UpdateStagingFolder.DefaultRoot,
                CommandLineUpdate.SetupArguments);
            var checker = new UpdateCheckService();
            result = new CommandLineUpdate(
                    ct => checker.CheckAsync(UpdateCheckOrigin.Automatic, ct), updater, host)
                .Run(canAsk, arg, args, exePath, token);
        }
        catch (Exception ex)
        {
            Helpers.CrashLog.TryWrite(ex);
            return UpdateConclusion.GoesOn;
        }

        var cancelled = token.IsCancellationRequested;
        if (cancelled && result is CommandLineUpdateResult.HandedOver { ExitCode: CommandLineUpdate.ControlCExit })
            host.SayHeldBackCancelling();
        if (ConcludeUpdate(result, arg, cancelled) is int exitCode) return new UpdateConclusion(exitCode, null);
        // The run goes on with this version. Where the update itself failed or InstallerClean
        // was in use, it says so once it does (TakeInstanceLock).
        return result is CommandLineUpdateResult.CarriedOn { SaysRunningInstead: true, FromVersion: { } runningVersion }
            ? new UpdateConclusion(null, () => string.Format(Strings.Cli_UpdateRunningInstead, arg, runningVersion))
            : UpdateConclusion.GoesOn;
    }

    /// <summary>
    /// Prints a blank line and <paramref name="line"/>, the line saying the run is
    /// cancelling. Never throws: it runs on the console's control-handler thread too.
    /// </summary>
    internal static void SayCancelling(string line)
    {
        // A console being closed is given no line: nobody can read it, and Windows ends
        // the process within seconds, before a setup still running would finish.
        if (CancelKey.Process.Closing) return;
        try
        {
            Console.WriteLine();
            Console.WriteLine(line);
        }
        catch (Exception ex)
        {
            Helpers.CrashLog.TryWrite(ex);
        }
    }

    /// <summary>
    /// Writes what the update ended in to the Application log and answers the exit code
    /// where the run ends here: the newer version's, where it ran in this console;
    /// <see cref="ExitCancelled"/> after Ctrl+C at the question, during the download,
    /// after the newer version was not run, or where Ctrl+C ended the newer version before
    /// it could answer it. Answers null where the run goes on with this version, having
    /// written the notice saying why the newer version was not run.
    /// <paramref name="cancelled"/> is whether Ctrl+C had come when the update returned.
    /// </summary>
    internal static int? ConcludeUpdate(CommandLineUpdateResult result, string arg, bool cancelled)
    {
        switch (result)
        {
            case CommandLineUpdateResult.HandedOver { ExitCode: CommandLineUpdate.ControlCExit } when cancelled:
                // The newer version was ended by the Ctrl+C this run took too, while it was
                // still starting and before it could answer it, so it wrote no summary. This
                // run writes the one the newer version writes for a Ctrl+C a moment later.
                // The same exit code from a console closed under it is passed on as it is.
                return ReportCancelledBeforeWork(arg);
            case CommandLineUpdateResult.HandedOver handedOver:
                // The newer version writes the run's summary. This run's one entry is the
                // 3005 written as it handed over, so the note is printed here where the log
                // refused it.
                NoteEventLogUnavailable();
                return handedOver.ExitCode;
            case CommandLineUpdateResult.Cancelled:
                return ReportCancelledBeforeWork(arg);
            case CommandLineUpdateResult.FailedThenCancelled failed:
                // Nothing changed this version's file and nothing ran, so the notice saying why
                // the update did not happen is the run's one entry.
                return ReportCancelled(CliEventClass.UpdateFailedNotice,
                    () => UpdateFailedThenCancelledEventLogLine(arg, failed.FromVersion, failed.ToVersion, failed.NotMade));
            case CommandLineUpdateResult.CarriedOn { NotMade: { } notMade, FromVersion: { } from, ToVersion: { } to }
                when cancelled:
                // Ctrl+C came as the update returned, and the run stops here before the scan,
                // so its one entry is the one above.
                return ReportCancelled(CliEventClass.UpdateFailedNotice,
                    () => UpdateFailedThenCancelledEventLogLine(arg, from, to, notMade));
            case CommandLineUpdateResult.CarriedOn { NotMade: { } notMade, FromVersion: { } from, ToVersion: { } to }:
                // The notice says why the update was not made and nothing of what then runs,
                // which the run's own summary says.
                MachineContract.WriteEventLog(CliContract.UpdateNotMadeClass(notMade),
                    () => UpdateFailedEventLogLine(arg, from, to, notMade));
                return null;
            default:
                return null;
        }
    }

    /// <summary>
    /// Ends a run cancelled before any work was done, with the same line, entry and code
    /// as a run cancelled during its scan.
    /// </summary>
    private static int ReportCancelledBeforeWork(string arg) =>
        ReportCancelled(CliEventClass.TransientSkip, () => string.Format(Strings.Cli_EventLogCancelledNoWork, arg));

    /// <summary>
    /// Ends a cancelled run that did no work: writes its one entry, of
    /// <paramref name="eventClass"/> with <paramref name="line"/>, prints "Cancelled.", and
    /// answers <see cref="ExitCancelled"/>. Where the console is being closed the entry goes
    /// first, the run having only seconds left and a console that may already be gone.
    /// </summary>
    private static int ReportCancelled(CliEventClass eventClass, Func<string> line)
    {
        var closing = CancelKey.Process.Closing;
        if (closing) MachineContract.WriteEventLog(eventClass, line);
        Console.WriteLine(Strings.Cli_Cancelled);
        if (!closing) MachineContract.WriteEventLog(eventClass, line);
        NoteEventLogUnavailable();
        return ExitCancelled;
    }

    /// <summary>
    /// Whether a person can see this console and answer a question in it: what
    /// <see cref="ConsoleIsWatched"/> asks, and standard input is not redirected. A run
    /// nobody can answer takes the update without asking.
    /// </summary>
    internal static bool ConsoleCanAsk() => ConsoleIsWatched() && !Console.IsInputRedirected;

    /// <summary>
    /// Whether this run may put a question at the console: <paramref name="consoleCanAsk"/>
    /// (<see cref="ConsoleCanAsk"/> in a run) and the run not told to ask nothing
    /// (<see cref="ConsoleUpdateHost.AsksNothingMore"/>). The update question and the report
    /// offer both read it, each at the moment it would be asked, so an update question nobody
    /// answered leaves the report offer unasked.
    /// </summary>
    internal static bool MayAsk(bool consoleCanAsk, ConsoleUpdateHost host) =>
        consoleCanAsk && !host.AsksNothingMore;

    /// <summary>
    /// The run's console host, asking nothing where <paramref name="invocation"/> carries
    /// <see cref="CliContract.NoQuestionsSwitch"/>.
    /// </summary>
    internal static ConsoleUpdateHost NewUpdateHost(string arg, CliInvocation invocation) =>
        new(arg, asksNothing: invocation.NoQuestions);

    /// <summary>
    /// The Event ID 3005 line: this run, at <paramref name="fromVersion"/>, found
    /// <paramref name="toVersion"/> and is running <paramref name="arguments"/> with it.
    /// </summary>
    /// <remarks>
    /// Built outside the en-GB scope, like <see cref="AbortedMoveEventLogLine"/>: the
    /// caller wraps it, so the line renders English in production and in the ambient
    /// culture anywhere else.
    /// </remarks>
    internal static string UpdatedEventLogLine(
        string arg, string fromVersion, string toVersion, IReadOnlyList<string> arguments) =>
        string.Format(Strings.Cli_EventLogUpdated, arg, fromVersion, toVersion, string.Join(' ', arguments));

    /// <summary>
    /// The Event ID 3006 line: this run could not update itself from
    /// <paramref name="fromVersion"/> to <paramref name="toVersion"/>, for the reason
    /// <paramref name="notMade"/> gives.
    /// </summary>
    /// <remarks>
    /// Built outside the en-GB scope, like <see cref="AbortedMoveEventLogLine"/>: the
    /// caller wraps it, so the line renders English in production and in the ambient
    /// culture anywhere else.
    /// </remarks>
    internal static string UpdateFailedEventLogLine(
        string arg, string fromVersion, string toVersion, UpdateNotMade notMade) =>
        string.Format(UpdateFailedFormat(notMade, thenCancelled: false), arg, fromVersion, toVersion);

    private static string UpdateFailedFormat(UpdateNotMade notMade, bool thenCancelled) => notMade switch
    {
        UpdateNotMade.DownloadFailed => thenCancelled
            ? Strings.Cli_EventLogUpdateDownloadFailedThenCancelled
            : Strings.Cli_EventLogUpdateDownloadFailed,
        UpdateNotMade.InUse => thenCancelled
            ? Strings.Cli_EventLogUpdateInUseThenCancelled
            : Strings.Cli_EventLogUpdateInUse,
        UpdateNotMade.CopyCannotTell => thenCancelled
            ? Strings.Cli_EventLogUpdateCopyCannotTellThenCancelled
            : Strings.Cli_EventLogUpdateCopyCannotTell,
        UpdateNotMade.FromSource => thenCancelled
            ? Strings.Cli_EventLogUpdateFromSourceThenCancelled
            : Strings.Cli_EventLogUpdateFromSource,
        UpdateNotMade.NeedsNetFramework48 => thenCancelled
            ? Strings.Cli_EventLogUpdateNeedsNetFramework48ThenCancelled
            : Strings.Cli_EventLogUpdateNeedsNetFramework48,
        UpdateNotMade.CannotInstallAutomatically => thenCancelled
            ? Strings.Cli_EventLogUpdateCannotInstallThenCancelled
            : Strings.Cli_EventLogUpdateCannotInstall,
        _ => thenCancelled
            ? Strings.Cli_EventLogUpdateNotRunThenCancelled
            : Strings.Cli_EventLogUpdateNotRun,
    };

    /// <summary>
    /// The Event ID 3006 line for a run that could not update itself, for the reason
    /// <paramref name="notMade"/> gives, and was then cancelled, so nothing ran. Built
    /// outside the en-GB scope, as <see cref="UpdateFailedEventLogLine"/> is.
    /// </summary>
    internal static string UpdateFailedThenCancelledEventLogLine(
        string arg, string fromVersion, string toVersion, UpdateNotMade notMade) =>
        string.Format(UpdateFailedFormat(notMade, thenCancelled: true), arg, fromVersion, toVersion);


    /// <summary>
    /// Prints what a run held back, in the operator's language: ONE counted
    /// sentence naming no cause, from <see cref="HeldBackReport.Line"/>, which the
    /// window renders from as well. The two hosts must not answer differently for
    /// one machine state and they share no printing code, so the sentence is Core's
    /// and neither host composes anything.
    ///
    /// CALLED ONCE PER RUN, ON A TALLY THE CALLER HAS ALREADY FOLDED. Both the
    /// pre-act re-verify and the action services' under-lease re-read hold files
    /// back, and which side of the installer mutex a file was condemned on is a
    /// fact about how the check is built rather than about the file. The sentence
    /// names no cause, so two printings in one run read as one finding repeated
    /// rather than as two. ADD THE TALLIES AND CALL THIS ONCE; a second call in one
    /// run is the fault rather than the wording.
    ///
    /// Takes the tally and not the path list, though every caller holds both. The
    /// tally already answers how many files there are, so a second argument here
    /// would be one that has to agree with it and could stop doing so. A run that
    /// held nothing back prints nothing, so a count of zero never reaches a sentence.
    ///
    /// Deliberately not a machine-read line. The Application-channel summary
    /// already carries what the run did in English, and an RMM reads that.
    /// </summary>
    internal static void ReportHeldBack(HeldBackReasons reasons)
    {
        var line = HeldBackReport.Line(reasons);
        if (line.Length > 0) Console.WriteLine(line);
    }

    /// <summary>
    /// Prints the line naming the drives and shares one pass stopped waiting for while
    /// files still had to be checked against them, in the operator's language, from
    /// <see cref="SourcesGivenUpReport.CommandLine"/>, which the window's line shares its
    /// forms with. A pass that gave none up prints nothing.
    ///
    /// CALLED FOR THE CHECK MADE BEFORE ACTING. The scan's own line, from the same
    /// helper, goes out with the scan's other notices (<see cref="ReportScanSignals"/>).
    /// The two are separate waits and can be on different drives, so a run whose scan and
    /// check each gave one up prints the line twice, each true of its own pass.
    ///
    /// Deliberately not a machine-read line. <see cref="NoteSourcesGivenUp"/> writes the
    /// same pass to the Application channel in English, and an RMM reads that.
    /// </summary>
    internal static void ReportSourcesGivenUp(IReadOnlyList<SourceRootGivenUp> roots)
    {
        var line = SourcesGivenUpReport.CommandLine(roots);
        if (line.Length > 0) Console.WriteLine(line);
    }

    /// <summary>
    /// Writes Event ID 3004 for one pass that stopped waiting for drives or shares while
    /// files still had to be checked against them, and nothing for a pass that gave none
    /// up. <paramref name="duringTheCheck"/> says which pass it was: the scan, or the check
    /// made before a Move or Delete acts.
    /// </summary>
    private static void NoteSourcesGivenUp(
        string arg, IReadOnlyList<SourceRootGivenUp> roots, bool duringTheCheck)
    {
        if (roots.Count == 0) return;
        MachineContract.WriteEventLog(CliEventClass.SourcesGivenUpNotice,
            () => SourcesGivenUpNoticeEventLogLine(arg, roots, duringTheCheck));
    }

    /// <summary>
    /// The outcome line, Event ID 1000, for a scan that offered nothing, had no other
    /// withholding to report, and stopped waiting for the drives and shares in
    /// <paramref name="roots"/> while files still had to be checked against them. It
    /// counts the drives and shares and the files left alone because of them; the 3004
    /// notice beside it names them.
    /// </summary>
    /// <remarks>
    /// Built outside the en-GB scope, like <see cref="AbortedMoveEventLogLine"/>: the
    /// caller wraps it, so the line renders English in production and in the ambient
    /// culture anywhere else.
    /// </remarks>
    internal static string SourcesGivenUpEventLogLine(string arg, IReadOnlyList<SourceRootGivenUp> roots)
    {
        var files = FilesKept(roots);
        return string.Format(
            DisplayHelpers.Pluralise(roots.Count,
                Strings.Cli_EventLogSourcesGivenUp_Singular,
                Strings.Cli_EventLogSourcesGivenUp_Plural,
                "Cli.EventLogSourcesGivenUp"),
            arg, roots.Count, files, DisplayHelpers.PluraliseFile(files));
    }

    /// <summary>
    /// The Event ID 3004 line for one pass: how many drives and shares it stopped waiting
    /// for, their names as the console's list names them, which pass it was, and how many
    /// files it left alone because of them.
    /// </summary>
    /// <remarks>
    /// Built outside the en-GB scope, like <see cref="AbortedMoveEventLogLine"/>: the
    /// caller wraps it, so the names' separator and the word for a drive render English in
    /// production and in the ambient culture anywhere else.
    /// </remarks>
    internal static string SourcesGivenUpNoticeEventLogLine(
        string arg, IReadOnlyList<SourceRootGivenUp> roots, bool duringTheCheck)
    {
        var files = FilesKept(roots);
        var template = duringTheCheck
            ? DisplayHelpers.Pluralise(roots.Count,
                Strings.Cli_EventLogSourcesGivenUpCheckNotice_Singular,
                Strings.Cli_EventLogSourcesGivenUpCheckNotice_Plural,
                "Cli.EventLogSourcesGivenUpCheckNotice")
            : DisplayHelpers.Pluralise(roots.Count,
                Strings.Cli_EventLogSourcesGivenUpScanNotice_Singular,
                Strings.Cli_EventLogSourcesGivenUpScanNotice_Plural,
                "Cli.EventLogSourcesGivenUpScanNotice");
        return string.Format(template,
            arg, roots.Count, SourcesGivenUpReport.ListOf(roots), files, DisplayHelpers.PluraliseFile(files));
    }

    /// <summary>
    /// How many files one pass left alone at the drives and shares in
    /// <paramref name="roots"/>. A file is counted at one root only, the one whose give-up
    /// stopped its check, so the sum counts each file once.
    /// </summary>
    private static int FilesKept(IReadOnlyList<SourceRootGivenUp> roots) =>
        roots.Sum(root => root.FilesKept);

    /// <summary>
    /// Whether the note saying the Application log refused an entry has been printed in this
    /// process, by <see cref="NoteEventLogUnavailable"/> or in a scan's listing.
    /// </summary>
    internal static bool EventLogNotePrinted { get; set; }

    /// <summary>
    /// Prints the one stdout audit line saying the Application channel was
    /// unwritable, so an RMM consumer polling for entries that never arrived has
    /// a record of why: where the log has refused an entry and the line has not been
    /// printed yet (<see cref="EventLogNotePrinted"/>), so every caller can call it and the
    /// run prints it once. Nothing here may throw: most callers have already written this
    /// run's Application-log entry, and a throw from here would reach a catch-all that
    /// writes a second. In a run, <see cref="ConsoleGuard"/> takes a failed write first,
    /// and the note carries a guard of its own as well, so the one entry does not rest
    /// on the console guard being in place.
    /// </summary>
    private static void NoteEventLogUnavailable()
    {
        if (!EventLogWriter.EventLogUnavailable || EventLogNotePrinted) return;
        EventLogNotePrinted = true;
        try
        {
            Console.WriteLine(Strings.Cli_EventLogUnavailable);
        }
        catch (Exception ex)
        {
            Helpers.CrashLog.TryWrite(ex);
        }
    }

    /// <param name="servicesOverride">
    /// The service provider to run against, or null to build the real one. NULL ON
    /// EVERY PRODUCTION PATH, and the only caller that passes anything is a test.
    ///
    /// IT IS THE PROVIDER'S CONSTRUCTION AND NOTHING ELSE. Not a line of behaviour sits
    /// on either side of it: what follows reads services out of whichever provider it
    /// was handed and cannot tell them apart. A branch that did more than this would be
    /// a path production never takes, tested in place of the one it does.
    ///
    /// A SUPPLIED PROVIDER BELONGS TO ITS CALLER AND IS NOT DISPOSED HERE. The one this
    /// method builds for itself still is, which is why the two are separate locals
    /// rather than one: disposing a caller's provider would close it under a test that
    /// still holds it, and skipping the disposal of its own would leak every singleton
    /// in the graph on the ordinary path.
    /// </param>
    /// <param name="consoleWatched">
    /// Asked once a Delete or Move that was not cancelled has acted on files: whether
    /// somebody is watching the console. Where it answers false the run gives no tip
    /// line; where true, the line prints there or is held in <paramref name="report"/> for
    /// after the report question or the send of a waiting report. Null asks
    /// <see cref="ConsoleIsWatched"/>, as every production path does. A test that reads
    /// the line passes its own answer, standard output being redirected under a test
    /// runner.
    /// </param>
    /// <param name="output">
    /// The guard <see cref="Main"/> put the run's output behind, which a scan-only run reads
    /// to answer for a write that failed. Null only where a caller installed no guard, and
    /// then no failed write is read.
    /// </param>
    /// <param name="cover">
    /// For a Move or Delete standing for an older copy, which goes on without its own hold
    /// on the single-instance mutex: asked before the batch and before each file is moved or
    /// deleted, so a run it does not cover stops there, saying how far it got and naming no
    /// cause. Null for every other run.
    /// </param>
    internal static async Task<int> RunWorkAsync(
        string arg, CliInvocation invocation, CancellationToken token,
        IServiceProvider? servicesOverride = null, Func<bool>? consoleWatched = null,
        ConsoleGuard? output = null, RunReport? report = null, IRunCover? cover = null)
    {
        // The note follows the run's entry wherever the work returns. Where an exception
        // escapes the work, thrown inside one of its catches, nothing here prints it: Main
        // reports the exception and prints the note after its own line and entry.
        var exitCode = await DoWorkAsync(arg, invocation, token, servicesOverride, consoleWatched, output, report, cover);
        NoteEventLogUnavailable();
        return exitCode;
    }

    /// <summary>
    /// The work of a /s, /d or /m run, whose parameters <see cref="RunWorkAsync"/> documents.
    /// Returns the run's exit code once the run's entry is written; everything its work throws
    /// is reported by its own catches, and only an exception thrown inside one of those
    /// escapes it.
    /// </summary>
    private static async Task<int> DoWorkAsync(
        string arg, CliInvocation invocation, CancellationToken token,
        IServiceProvider? servicesOverride, Func<bool>? consoleWatched, ConsoleGuard? output,
        RunReport? report, IRunCover? cover)
    {
        consoleWatched ??= ConsoleIsWatched;

        // What a cancelled batch had actually committed, read by the OCE catch to
        // write its EventLog summary and to pick ExitPartial over ExitCancelled.
        // Taken from the service's own DeletedCount / MovedCount, never from the
        // progress reporter: both services report BEFORE they touch the file, so a
        // Ctrl+C between the first report and the first delete leaves the reporter
        // saying one and the batch having done none, and exit 2 is defined as work
        // committed.
        int committedCount = 0;
        int totalToProcess = 0;
        // The backup folder a cancelled Move left files in, for the undo line the
        // catch below prints. Hoisted out of the try for the same reason
        // committedCount is: moveDest is declared inside it and the catch cannot
        // see it. Empty on every other path, which is what gates the line.
        string cancelledMoveDestination = string.Empty;

        // The scan and the re-verify stop on Ctrl+C and where the run's cover is lost. The
        // batch asks the cover as the last thing before each file (mayAct), never waiting,
        // since it holds the Windows Installer mutex meanwhile; where the run is not covered
        // the batch stops before that file, and this is cancelled once the batch has
        // returned, so the run ends in the catch for a stopped cover.
        using var workSource = cover is null ? null : CancellationTokenSource.CreateLinkedTokenSource(token, cover.Lost);
        var workToken = workSource?.Token ?? token;
        var coverStopped = false;
        Func<string, bool>? mayAct = cover is null ? null : _ =>
        {
            if (cover.Covers()) return true;
            coverStopped = true;
            return false;
        };
        // Asked before the batch, outside the Windows Installer mutex, so a run not covered
        // by then ends with nothing done, in the same catch.
        void StopWhereNotCovered()
        {
            if (cover is null || cover.Covers()) return;
            workSource!.Cancel();
            workSource.Token.ThrowIfCancellationRequested();
        }

        try
        {
            using var ownedServices = servicesOverride is null
                ? new ServiceCollection()
                    .AddInstallerCleanCore()
                    .BuildServiceProvider(new ServiceProviderOptions
                    {
                        ValidateScopes = true,
                        ValidateOnBuild = true,
                    })
                : null;
            IServiceProvider services = servicesOverride ?? ownedServices!;

            // THE REPORT OF THIS RUN, WHERE SOMEBODY CAN ANSWER FOR IT. The start check runs
            // from here, alongside the scan, so the PC's first run is decided before any
            // batch sets the mark itself. A run nobody can answer starts no check, builds
            // nothing and sets no mark beyond the one a batch sets.
            var reporting = report is { CanAsk: true };
            bool? pcHadEarlierRun = null;
            if (reporting)
                _ = services.GetRequiredService<IEarlierRunCheck>().ShowsAnEarlierRunAsync();

            async Task<bool> PcHadEarlierRunAsync()
            {
                pcHadEarlierRun ??= await services.GetRequiredService<IEarlierRunCheck>()
                    .ShowsAnEarlierRunWithinAsync(ReportCheckBound);
                return pcHadEarlierRun.Value;
            }

            // Carries the report of a run that finished, where this is the PC's first run:
            // builds it, then claims the PC-wide mark, so a Ctrl+C at the question leaves
            // this the PC's first run all the same. A claim that is refused, the mark being
            // set already by a window or another run that finished first, carries nothing. A
            // report that cannot be built is left out and the run ends as it would have.
            async Task CarryReportAsync(Func<ResultLogEntry> build)
            {
                if (!reporting || await PcHadEarlierRunAsync()) return;
                try
                {
                    var entry = build();
                    if (!services.GetRequiredService<IFirstRunMark>().TryClaim()) return;
                    report!.Entry = entry;
                }
                catch (Exception ex)
                {
                    Helpers.CrashLog.TryWrite(ex);
                }
            }

            // The tip line of a Delete or Move that acted on files. Where the run goes on
            // to print about its report, a question about the one it carried or the send of
            // one an earlier run left waiting, the line is held in the report and Main
            // prints it after that, so it is the last thing on the screen. Anywhere else it
            // prints here.
            void AskForDonationAfterReport()
            {
                if (!consoleWatched()) return;
                if (reporting && (report!.Entry is not null || ReportIsWaiting(services)))
                    report!.DonationHeld = true;
                else
                    WriteDonation();
            }

            string? WindowsRegion() => services.GetRequiredService<IWindowsRegion>().Read();

            // The report of a scan, which is also the report of a Delete or Move that found
            // nothing to act on.
            ResultLogEntry ScanOnlyReport(ScanResult scan, long scanMs) => ResultLogEntry.ForScanOnly(
                scan, scanMs, WindowsRegion(), ReportHost.CommandLine, RunningCopy(services));

            // For /m, resolve and validate the destination before the scan so
            // a misconfigured task fails fast instead of paying a full
            // Installer-folder walk every run before erroring on the path.
            string moveDest = string.Empty;
            if (arg == "/m")
            {
                var destFailure = ResolveAndValidateMoveDestination(services, invocation, arg, out moveDest);
                if (destFailure is int destExitCode)
                    return destExitCode;
            }

            var scanService = services.GetRequiredService<IFileSystemScanService>();

            Console.WriteLine(Strings.Cli_ScanningInstaller);
            var scanTimer = Stopwatch.StartNew();
            var scanResult = await scanService.ScanAsync(cancellationToken: workToken);
            var scanDurationMs = scanTimer.ElapsedMilliseconds;

            var count = scanResult.RemovableFiles.Count;
            var totalBytes = scanResult.RemovableFiles.Sum(f => f.SizeBytes);
            var size = DisplayHelpers.FormatSize(totalBytes);
            // Read once: the property builds its list afresh on every read.
            var sourcesGivenUp = scanResult.SourceRootsGivenUpKeepingFiles;
            // A clean machine gets one line, not a count of zero and a size of
            // zero followed by a second line saying the same thing: somebody
            // reading a scheduled task's log wants the state of the machine
            // rather than the tool's intention towards it.
            //
            // THE CLEAN LINE IS PRINTED WHEREVER NOTHING IS OFFERED, EXCEPT WHERE ONE OF TWO
            // LINES IS PRINTED INSTEAD: the line about files held back for their times, and
            // the line naming the drives and shares the scan stopped waiting for. Where
            // either is among the notices the clean line is not printed. Whether the first is
            // printed is the scan's answer (HasRecentChangeLine), the same one
            // ReportScanSignals spends.
            //
            // THE HOST DOES NOT PARTITION ANYTHING TO GET HERE. Deciding it here would
            // mean reading a split the scan owns, and a host that infers one decision's
            // outcome from figures the others also write to means something different
            // the moment any of them moves. The scan answers it where the withholding
            // happens; this branch spends the answer.
            //
            // THE FIRST NOTICE SAYS HOW MANY FILES WERE LEFT ALONE (ScanResult.LeftAloneCount),
            // the figure the window shows under its unneeded count. It follows the line saying
            // what the scan found or the clean line, and is the first line after the scanning
            // one where neither is printed and on a /s that lists files.
            //
            // withheldCount is UnestablishedWithheldCount, which leaves out any file kept
            // because it declares an installed program, for its times
            // (ScanResult.WithheldBy.NotBeforeTheClockCount) or as a copy of a patch
            // Windows holds a registration of. It is the figure the Application log's
            // held-back entries carry.
            var withheldCount = scanResult.UnestablishedWithheldCount;

            // The log's wholesale sentence names a cause, so it is written only for the
            // reading that earns it and the per-file one for every other reading the
            // scan says is worth reporting.
            var wholesale = scanResult.Withholding == WithholdingAccount.WholeWalkOffer;

            string FoundLine() =>
                string.Format(
                    DisplayHelpers.Pluralise(count, Strings.Cli_FoundOrphans, "Cli.FoundOrphans"),
                    DisplayHelpers.FormatCount(count), DisplayHelpers.PluraliseFile(count), size);

            // THE NOTICES' APPLICATION-LOG ENTRIES ARE WRITTEN HERE AND THEIR STDOUT LINES
            // COME BACK TO BE PRINTED, after the line saying what the scan found or the clean
            // line, or after the scanning line where neither is printed. A /s that lists files prints them
            // ahead of the line counting the files instead, so the list sits straight under
            // that line, and a script reading the rows takes the indented lines after it to
            // the end of the output. What can follow the list is never indented: the unexpected-error
            // line of a run whose output failed and the note saying the Application log
            // refused the run's summary, in either order, and a blank line wherever a failed
            // write lost a whole line (ConsoleGuard).
            var notices = ReportScanSignals(arg, scanResult, sourcesGivenUp);

            if (arg == "/s" && count > 0)
            {
                // The name column is measured off this run's own longest name
                // rather than fixed, because the cache's names are derived from
                // package identity and vary in length from machine to machine: a
                // constant is padding every row of one list out and too narrow
                // for another, and one name past it puts the wandering size and
                // reason straight back. Only the name is padded and nothing is
                // right-aligned. Plus two, with the two spaces in the format,
                // leaves four columns before the bracket: two separates a pair of
                // words, and reading down sixty-odd rows wants the bracket column
                // standing clear of the ragged name ends above it. This is the
                // output most likely to be pasted into a ticket.
                var nameColumn = scanResult.RemovableFiles.Max(f => f.FileName.Length) + 2;
                var listing = new List<string>(notices)
                {
                    FoundLine(),
                    string.Join(Environment.NewLine,
                        scanResult.RemovableFiles.Select(f =>
                            $"  {f.FileName.PadRight(nameColumn)}  ({f.SizeDisplay}, {f.Reason})")),
                };

                // THE NOTE SAYING THE APPLICATION LOG REFUSED AN ENTRY GOES WITH THE OTHER
                // NOTES, ahead of the count line, where the log has already refused a notice's
                // entry. Where it has refused none, whether it takes the summary is known only
                // once the summary is written, after the list, and the note then follows the
                // list. A check of the log made before printing would be a forecast, since a log
                // can pass one and still refuse the entry, a stopped Event Log service being one
                // way.
                if (EventLogWriter.EventLogUnavailable)
                {
                    listing.Insert(notices.Count, Strings.Cli_EventLogUnavailable);
                    EventLogNotePrinted = true;
                }

                // EVERY LINE IS BUILT AND PRINTED BEFORE THE SUMMARY IS WRITTEN, so the summary
                // says whether the list got out (EndScanOnly). A line that cannot be built, a
                // translation whose placeholders do not match its arguments for example, throws
                // before the list is printed, and the catch-all's entry is the run's one.
                //
                // The noun and size are recomputed inside the en-GB scope rather than reusing
                // the human-facing `size` (which is in the OS region and grouped), so this
                // audit line reads fully English and carries the size in the form tooling
                // reads.
                listing.ForEach(Console.WriteLine);
                await CarryReportAsync(() => ScanOnlyReport(scanResult, scanDurationMs));
                return EndScanOnly(arg, output,
                    () => string.Format(Strings.Cli_EventLogScanFound,
                        arg, count, DisplayHelpers.PluraliseFile(count),
                        DisplayHelpers.FormatSizeForMachine(totalBytes)));
            }

            if (count > 0)
                Console.WriteLine(FoundLine());
            else if (sourcesGivenUp.Count == 0 && !scanResult.HasRecentChangeLine)
                Console.WriteLine(Strings.Cli_FoundNoOrphans);

            notices.ForEach(Console.WriteLine);

            if (count == 0)
            {
                // The audit line tells a machine the scan could not judge from a clean one.
                // On stdout the clean line reports that nothing was identified, and is printed
                // on both unless one of the two notices that replace it is printed. A monitoring
                // tool watching the Application channel reads the split here. A machine with no
                // withholding to report and no drive or share given up takes the clean entry,
                // whether or not files were held back for their times: the line about them is
                // on stdout alone.
                //
                // ONE EVENT CLASS FOR ALL OF THEM. The run did its job on each of these
                // machines, so each entry belongs in the outcome band, and the notice
                // beside it in the 3000 band is what tells a machine the scan could not
                // judge from a clean one by number. The message differs too, so a filter
                // reading the English tells them apart as well.
                //
                // A WITHHOLDING TO REPORT TAKES ITS OWN MESSAGE EVEN WHERE A DRIVE OR
                // SHARE WAS GIVEN UP TOO: it counts the files held back, and the notice
                // ReportScanSignals has already written names the drives and shares.
                string NothingOfferedSummary() =>
                    scanResult.HasWithholdingToReport
                        ? string.Format(
                            wholesale
                                ? Strings.Cli_EventLogNothingOffered
                                : Strings.Cli_EventLogNothingOfferedPerFile,
                            arg, withheldCount, DisplayHelpers.PluraliseFile(withheldCount))
                        : sourcesGivenUp.Count > 0
                            ? SourcesGivenUpEventLogLine(arg, sourcesGivenUp)
                            : string.Format(Strings.Cli_EventLogScanNoOrphans, arg);

                // A scan that offered nothing carries the report, and so does a Delete or
                // Move that found nothing to act on, as the window's card for them does.
                await CarryReportAsync(() => ScanOnlyReport(scanResult, scanDurationMs));

                // A Delete or Move that found nothing to act on exits as that Delete or Move,
                // which is what it was asked for, whatever became of its output.
                if (arg != "/s")
                {
                    MachineContract.WriteEventLog(CliEventClass.Ok, NothingOfferedSummary);
                    return ExitOk;
                }

                // A scan-only run prints the note saying the Application log refused an entry
                // under its notices where the log has already refused one of theirs, as the
                // listing branch above prints it with them, and then ends as that branch does.
                NoteEventLogUnavailable();
                return EndScanOnly(arg, output, NothingOfferedSummary);
            }

            // /s acts on nothing, so it skips the gate here. The scan's own read of the same
            // signals, made on every mode, is in the scan.
            if (arg is "/d" or "/m")
            {
                var rebootService = services.GetRequiredService<IPendingRebootService>();
                var rebootCheck = rebootService.Check();
                if (rebootCheck.IsBlocked)
                    // Block + null Reason is unreachable per the PendingRebootResult.Block
                    // factory contract; .Value is safe inside this IsBlocked branch.
                    return EmitPendingRebootBlocked(
                        arg, rebootCheck.Reason!.Value, rebootCheck.Detail, rebootCheck.WindowsError);
            }

            // Re-verify the removable set immediately before acting: the Windows
            // Installer records are enumerated again, and every file no record
            // names is judged again the way the scan judged it (see
            // IRemovableReverifier). It runs inside this try, so a re-verify that
            // throws (the enumeration can fail, e.g. LocalisedAccessException)
            // stops the batch through the existing error path rather than acting
            // on an un-verified set, and a cancellation routes to the OCE catch.
            // It runs synchronously before the batch; the CLI blocks on it
            // naturally.
            var reverifier = services.GetRequiredService<IRemovableReverifier>();
            var reverify = await reverifier.ReverifyAsync(
                scanResult.RemovableFiles.Select(f => f.FullPath).ToList(), workToken);

            // THE DRIVES AND SHARES THIS CHECK STOPPED WAITING FOR WHILE FILES STILL HAD TO
            // BE CHECKED AGAINST THEM, read once. The action services' own re-read gives
            // none up, so this list is everything the run gives up after the scan.
            //
            // ITS NOTICE IS WRITTEN HERE, AHEAD OF EVERY RETURN THE RUN CAN TAKE FROM NOW
            // ON, as ReportScanSignals writes the scan's ahead of every return after the
            // scan. A run the action then refuses still left those files alone, so the
            // notice is true of it. The console line waits for the held-back line below
            // and is silent wherever that line is.
            var checkSourcesGivenUp = reverify.SourceRootsGivenUpKeepingFiles;
            NoteSourcesGivenUp(arg, checkSourcesGivenUp, duringTheCheck: true);

            // A Windows Installer transaction under way when the re-verify read the
            // in-progress signals refuses the run as the gate above refuses it, with the same
            // line, entry and exit code. The re-verify itself holds back only the files no
            // record names on such a read, and a superseded patch in the same batch is
            // stopped here. Block + null Reason is unreachable, as at the gate.
            if (reverify.InstallerActivity is { IsBlocked: true } installerActivity)
                return EmitPendingRebootBlocked(arg, installerActivity.Reason!.Value,
                    installerActivity.Detail, installerActivity.WindowsError);

            var survivingSet = new HashSet<string>(reverify.Surviving, StringComparer.OrdinalIgnoreCase);
            var survivingFiles = scanResult.RemovableFiles
                .Where(f => survivingSet.Contains(f.FullPath)).ToList();

            // The counts and byte figures further down are recomputed from the
            // survivor subset, so "X of Y" and the freed-space total describe what
            // was acted on rather than the pre-reverify scan.
            //
            // NOTHING IS PRINTED HERE, AND THAT IS THE FOLD. What this pass held
            // back and what the action service's own re-read holds back are one
            // account of one batch, so the tally is carried down in heldBack and
            // printed ONCE beside the service's, which is what the window does when
            // it folds both into a single ReverifyResult before the completion
            // overlay reads it. The two producers print the same sentence, so
            // printing here as well would put it on stdout twice for one batch with
            // nothing to tell the two numbers apart.
            //
            // THE PATHS THAT PRINT NOTHING ARE THE ONES THE WINDOW IS ALSO SILENT
            // ON: an installer-busy refusal, a refused lock, an unavailable lock and
            // a free-space refusal all return before the print, and none of them
            // commits anything, so there is no completed-of-intended count for the
            // sentence to sit beside. The window reaches no completion screen on
            // any of the four either.
            //
            // A MACHINE THE SCAN WOULD NOW OFFER NO WALKED FILE ON DOES NOT REFUSE
            // THE RUN. The re-verify drops the walk-derived half of the batch and any
            // superseded file a fresh scan would now hold back, each counted in
            // heldBack, and the rest of the batch goes on.
            var heldBack = reverify.Reasons;

            var filePaths = survivingFiles.Select(f => f.FullPath).ToList();
            count = survivingFiles.Count;
            totalBytes = survivingFiles.Sum(f => f.SizeBytes);

            // Per-file progress, reported synchronously on the producing
            // thread (see SynchronousProgress). A console Main has no
            // SynchronizationContext, so Progress<T> would marshal each
            // report through the thread pool and let a "[i/total]" line
            // print after the post-await summary ("Deleted N files."),
            // breaking the stdout line order an RMM scrapes.
            totalToProcess = count;
            // Guarded here as well as by ConsoleGuard, so the handler holds wherever
            // RunWorkAsync is called from. The action services report from inside
            // their per-file try, so a throw out of this handler is filed as an
            // error against a file that is perfectly fine. A failed write is a fact
            // about the console and never about the file being deleted or moved.
            //
            // One crash-log entry per run rather than one per file. One cause, such
            // as a full disk, can refuse many reports in a row, so an entry each
            // would spend crash.log's rotation budget on copies of a cause already
            // recorded and evict the history behind them, which is the failure
            // PerItemFailureLog exists to prevent on the service side of the same
            // batch.
            var progressWriteFailed = false;
            var progress = new SynchronousProgress<OperationProgress>(p =>
            {
                try
                {
                    Console.WriteLine($"  [{p.CurrentFile}/{p.TotalFiles}] {p.CurrentFileName}");
                }
                catch (Exception ex)
                {
                    if (progressWriteFailed) return;
                    progressWriteFailed = true;
                    Helpers.CrashLog.TryWrite(ex);
                }
            });

            // Set where a batch moves or deletes a file, or ends where how far it got is
            // not known (HandOverBatchAsync). A run that carries its report claims it
            // (CarryReportAsync), and the claim counts a mark this run set as its own.
            var firstRunMark = services.GetRequiredService<IFirstRunMark>();

            if (arg == "/d")
            {
                var deleteService = services.GetRequiredService<IDeleteFilesService>();
                if (filePaths.Count > 0) StopWhereNotCovered();
                // Silent when the re-verify just above took every file back. It
                // prints nothing itself: the run's one held-back line below is
                // what carries those files. Announcing a batch of none here and
                // then reporting that none of it happened reads as a fault twice
                // over on a run where nothing went wrong.
                if (count > 0)
                    Console.WriteLine(string.Format(
                        DisplayHelpers.Pluralise(count, Strings.Cli_DeletingFiles, "Cli.DeletingFiles"),
                        DisplayHelpers.FormatCount(count), DisplayHelpers.PluraliseFile(count)));
                // Skip the service when the re-verify left nothing to act on:
                // DeleteFilesService returns 0/0 for an empty list anyway, but
                // synthesizing it keeps the /d and /m branches symmetric (Move
                // would otherwise create and probe its destination for an empty
                // batch). The summary path below still fires with 0, exit Ok, so the
                // one-summary-per-run event-log contract holds.
                // Decided here, ahead of the batch, because a batch that deletes a file sets
                // the mark the answer reads.
                if (reporting) await PcHadEarlierRunAsync();
                var operationTimer = Stopwatch.StartNew();
                var result = await HandOverBatchAsync(firstRunMark,
                    () => filePaths.Count == 0
                        ? Task.FromResult(new DeleteResult(0, Array.Empty<FileOperationError>()))
                        : deleteService.DeleteFilesAsync(
                            filePaths, UnderLeaseClaims.From(reverify),
                            progress: progress, cancellationToken: workToken, mayAct: mayAct),
                    r => r.DeletedCount);
                var operationDurationMs = operationTimer.ElapsedMilliseconds;

                // A Windows Installer transaction grabbed Global\_MSIExecute in the
                // race after the gate check passed, so the service refused and
                // touched nothing. Report it identically to a pre-act gate block.
                if (result.InstallerBusy)
                    return EmitPendingRebootBlocked(arg, PendingRebootReason.MsiExecuteMutexHeld, null);

                // The security on Global\_MSIExecute refused the service the rights
                // to open it, a refusal that began after the gate check passed, so
                // it refused and touched nothing. Reported under the gate's own
                // reason for the same refusal, so the run reads identically wherever
                // the refusal was met.
                if (result.InstallerLockAccessRefused)
                    return EmitPendingRebootBlocked(arg, PendingRebootReason.MsiExecuteMutexAccessRefused, null);

                // The service could not take Global\_MSIExecute and nothing was
                // shown to be holding it, so it refused and touched nothing.
                if (result.InstallerLockUnavailable)
                    return EmitInstallerLockUnavailable(arg);

                // THE RUN'S ONE HELD-BACK LINE, printed here because this is the
                // last of the two producers to answer: the service takes the
                // installer mutex and re-reads the batch's patch claims inside it,
                // so anything condemned there is condemned after the pre-act pass
                // has already finished. Adding rather than replacing, because the
                // two hold back DIFFERENT files and the number on the line is every
                // file the run held back.
                //
                // Ahead of the "deleted N" line, so the run reads in the order it
                // happened: what was intended, what was held back, what was done.
                // Ahead of the cancel re-entry below for a harder reason than
                // order: that re-entry leaves this method, so a run that holds
                // files back and is then cancelled would say nothing at all about
                // them if this line sat any later. The window reports them on
                // both paths.
                //
                // THE LINE NAMING THE DRIVES AND SHARES THE CHECK STOPPED WAITING FOR
                // FOLLOWS IT, for the same two reasons: the files it speaks of were
                // held back by that check, so it reads under the count of them, and it
                // goes ahead of the cancel re-entry with it.
                heldBack += result.HeldBackReasons;
                ReportHeldBack(heldBack);
                ReportSourcesGivenUp(checkSourcesGivenUp);
                // Held-back files were never touched, so they leave the tally the
                // same way the errors below do. Without this they would be counted
                // as freed bytes, the byte sum discounting errors alone.
                // totalToProcess moves with them: it is the "of N" the cancelled-run
                // audit line prints, and left at the pre-fold count it describes a
                // batch that included files the run never intended to reach.
                if (result.HeldBack.Count > 0)
                {
                    survivingFiles = FoldHeldBack(survivingFiles, result.HeldBack);
                    count = survivingFiles.Count;
                    totalBytes = survivingFiles.Sum(f => f.SizeBytes);
                    totalToProcess = count;
                }

                Console.WriteLine(string.Format(
                    DisplayHelpers.Pluralise(result.DeletedCount, Strings.Cli_DeletedFiles, "Cli.DeletedFiles"),
                    DisplayHelpers.FormatCount(result.DeletedCount),
                    DisplayHelpers.PluraliseFile(result.DeletedCount)));
                if (result.Errors.Count > 0)
                {
                    // Plural "errors:" whatever the count, so that
                    // `grep -E '[0-9]+ errors:'` matches a one-error run too. The
                    // grammar ("1 errors:") is the price and is paid on purpose,
                    // and the noun is forced English (MachineContract) so it stays
                    // "errors" on a non-English machine.
                    //
                    // The shape is held; it is not published. Neither the README's
                    // "## Command line" section nor --help names it, and the
                    // README tells scripters to key off the exit code rather than
                    // parse the text.
                    Console.WriteLine(MachineContract.English(
                        () => $"{result.Errors.Count} {Strings.Plural_Error_Plural}:"));
                    foreach (var err in result.Errors)
                        Console.WriteLine($"  {Path.GetFileName(err.FilePath)}: {err.LocalisedMessage}");
                }

                // The service returns its partial result on a mid-batch cancel
                // rather than throwing, so re-enter the OCE catch by hand: a
                // cancelled run gets one cancelled-run event-log entry and a
                // Partial or Cancelled exit code whichever way the cancellation
                // reached the host. The service's own count is what the catch
                // attributes on, and this is the only place it can be read.
                if (result.Cancelled)
                {
                    committedCount = result.DeletedCount;
                    if (coverStopped) workSource!.Cancel();
                    workToken.ThrowIfCancellationRequested();
                }

                // Bytes-recovered figure excludes the per-file error
                // list. Reporting the scan total on a partial failure
                // would overstate the freed-space figure for every run
                // that didn't process every file.
                long actualBytes = result.Errors.Count == 0
                    ? totalBytes
                    : SumBytesExcludingErrors(survivingFiles, result.Errors);
                var outcome = CliContract.ClassifyFileOperation(result.DeletedCount, result.Errors.Count);
                // The folded check, so the under-lease re-read's own held-back files are
                // counted with the pre-act ones, as the window's report counts them.
                await CarryReportAsync(() => ResultLogEntry.ForDelete(
                    scanResult, scanDurationMs, result, actualBytes, operationDurationMs,
                    reverify.WithHeldBack(result.HeldBack, result.HeldBackReasons),
                    WindowsRegion(), ReportHost.CommandLine, RunningCopy(services)));

                // After the cancel re-entry, so a cancelled run never prints it: there
                // the undo, or the cancellation itself, stays the last line.
                if (result.DeletedCount > 0 && !result.Cancelled)
                    AskForDonationAfterReport();

                // Size and nouns are recomputed inside the en-GB scope, not
                // reused from the stdout copies, so the audit line reads fully
                // English ("3.2 GB", "files") on a localised machine, and the size
                // takes the ungrouped form tooling reads.
                MachineContract.WriteEventLog(outcome.EventClass,
                    () => string.Format(Strings.Cli_EventLogDeleteSummary,
                        arg, result.DeletedCount, count, DisplayHelpers.PluraliseFile(count),
                        DisplayHelpers.FormatSizeForMachine(actualBytes), result.Errors.Count,
                        DisplayHelpers.PluraliseError(result.Errors.Count)));
                return outcome.ExitCode;
            }

            // The /m destination was resolved and validated before the scan
            // (see ResolveAndValidateMoveDestination); moveDest is non-empty,
            // fully qualified and outside the Installer and system folders.
            var moveService = services.GetRequiredService<IMoveFilesService>();

            // Room at the destination, decided by the Core rule the window
            // applies, which refused a Move it had no room for while this ran it
            // and filled the volume. Asked here rather than in the pre-scan
            // destination validation because only the surviving set's size says
            // how much room is needed, and asked before the folder is created so a
            // refusal leaves nothing behind.
            if (MoveSpaceCheck.RefusalFreeSpace(moveDest, totalBytes) is long freeAtDestination)
            {
                Console.WriteLine(string.Format(Strings.Cli_MoveNotEnoughSpace,
                    moveDest, DisplayHelpers.FormatSize(totalBytes),
                    DisplayHelpers.FormatSize(freeAtDestination)));
                // Sizes recomputed inside the en-GB scope; see the summary lines
                // below for why the stdout copies are not reused.
                MachineContract.WriteEventLog(CliEventClass.HardError,
                    () => string.Format(Strings.Cli_EventLogMoveNotEnoughSpace,
                        arg, moveDest, DisplayHelpers.FormatSizeForMachine(freeAtDestination),
                        DisplayHelpers.FormatSizeForMachine(totalBytes)));
                return ExitError;
            }

            // Asked before the destination folder is made, which the move service does first.
            if (filePaths.Count > 0) StopWhereNotCovered();

            // Silent at zero; see the /d branch.
            if (count > 0)
                Console.WriteLine(string.Format(
                    DisplayHelpers.Pluralise(count, Strings.Cli_MovingFiles, "Cli.MovingFiles"),
                    DisplayHelpers.FormatCount(count), DisplayHelpers.PluraliseFile(count), moveDest));
            // See the /d branch: skip the service (and MoveFilesService's
            // destination-folder create + probe) when nothing survived the
            // re-verify; synthesize the empty result so the summary path still fires
            // with 0 and exit Ok.
            MoveResult moveResult;
            // Decided here, ahead of the batch, as in the /d branch.
            if (reporting) await PcHadEarlierRunAsync();
            var moveTimer = Stopwatch.StartNew();
            try
            {
                moveResult = await HandOverBatchAsync(firstRunMark,
                    () => filePaths.Count == 0
                        ? Task.FromResult(new MoveResult(0, Array.Empty<FileOperationError>()))
                        : moveService.MoveFilesAsync(filePaths, moveDest,
                            UnderLeaseClaims.From(reverify), progress, workToken, mayAct),
                    r => r.MovedCount);
            }
            catch (MoveAbortedException ex)
            {
                RecordFirstRun(firstRunMark, ex.Partial.MovedCount);

                // Caught at the call site rather than at the method's own arms,
                // where survivingFiles and moveDest are out of scope, which is
                // also where the window catches its copy. Without this the base
                // type's arm below prints the destination problem and nothing
                // about the files already sitting in the destination folder.
                //
                // The same fold as the completed path, and the abort arm needs it
                // more rather than less. survivingFiles is the list the byte
                // figure is taken from POSITIONALLY: the service filtered its own
                // copy and this host did not, so summing the first reached-many
                // entries of an unfiltered list counts held-back files as moved
                // and drops real ones off the end. Not a wrong total, the wrong
                // rows. The count goes the same way, being the "of N" in the
                // Application-log line an RMM audits.
                heldBack += ex.Partial.HeldBackReasons;
                ReportHeldBack(heldBack);
                ReportSourcesGivenUp(checkSourcesGivenUp);
                if (ex.Partial.HeldBack.Count > 0)
                {
                    survivingFiles = FoldHeldBack(survivingFiles, ex.Partial.HeldBack);
                    count = survivingFiles.Count;
                }
                return ReportAbortedMove(arg, ex, moveDest, count, survivingFiles);
            }

            var moveDurationMs = moveTimer.ElapsedMilliseconds;

            // Global\_MSIExecute found held at the service boundary: same outcome
            // as a gate block.
            if (moveResult.InstallerBusy)
                return EmitPendingRebootBlocked(arg, PendingRebootReason.MsiExecuteMutexHeld, null);

            // Global\_MSIExecute refused the service the rights to open it: same
            // outcome as the gate meeting that refusal. See the /d branch.
            if (moveResult.InstallerLockAccessRefused)
                return EmitPendingRebootBlocked(arg, PendingRebootReason.MsiExecuteMutexAccessRefused, null);

            // The service could not take Global\_MSIExecute and nothing was shown
            // to be holding it, so it refused and touched nothing, its own
            // destination folder included.
            if (moveResult.InstallerLockUnavailable)
                return EmitInstallerLockUnavailable(arg);

            // The run's one held-back line, and the check's line naming the drives
            // and shares under it. See the /d branch for why the two producers'
            // tallies are added and printed here rather than one each, and for why
            // both come ahead of the cancel re-entry.
            heldBack += moveResult.HeldBackReasons;
            ReportHeldBack(heldBack);
            ReportSourcesGivenUp(checkSourcesGivenUp);
            if (moveResult.HeldBack.Count > 0)
            {
                survivingFiles = FoldHeldBack(survivingFiles, moveResult.HeldBack);
                count = survivingFiles.Count;
                totalBytes = survivingFiles.Sum(f => f.SizeBytes);
                totalToProcess = count;
            }

            Console.WriteLine(string.Format(
                DisplayHelpers.Pluralise(moveResult.MovedCount, Strings.Cli_MovedFiles, "Cli.MovedFiles"),
                DisplayHelpers.FormatCount(moveResult.MovedCount),
                DisplayHelpers.PluraliseFile(moveResult.MovedCount)));
            if (moveResult.Errors.Count > 0)
            {
                // See the matching block in the /d branch for the always-plural
                // rationale, the English-forced noun, and what is and is not
                // published about the "\d+ errors:" shape.
                Console.WriteLine(MachineContract.English(
                    () => $"{moveResult.Errors.Count} {Strings.Plural_Error_Plural}:"));
                foreach (var err in moveResult.Errors)
                    Console.WriteLine($"  {Path.GetFileName(err.FilePath)}: {err.LocalisedMessage}");
            }
            // AFTER THE ERROR BLOCK RATHER THAN BETWEEN IT AND THE SUMMARY, so a
            // script scraping the "\d+ errors:" shape finds that block straight under
            // the line counting what was moved.
            //
            // Only where something moved, on the silent-at-zero rule the run lines
            // above already follow. A move that put no file in the folder has not
            // made one worth naming.
            //
            // AND NOT ON A CANCEL, which is the second conjunct. The sentence asks
            // the reader to check their programs and then delete the backup, so it
            // is advice for a run that went the distance: it tells somebody who
            // stopped the move part-way to go through with the thing they stopped.
            // The cancel re-entry below is what that run reports instead. The
            // aborted-move path keeps the line: whatever stopped the batch, the
            // files already moved are in a folder the reader has to go and
            // deal with.
            if (moveResult.MovedCount > 0 && !moveResult.Cancelled)
                Console.WriteLine(string.Format(Strings.Cli_MoveRestoreHint, moveDest));

            // Partial result returned on a mid-batch cancel; re-enter the OCE catch
            // so the machine contract matches a thrown cancellation. See the /d
            // branch above.
            if (moveResult.Cancelled)
            {
                committedCount = moveResult.MovedCount;
                // Only where a file actually reached the destination: a cancel that
                // moved nothing has left nothing to put back, on the same
                // silent-at-zero rule the run lines follow.
                if (moveResult.MovedCount > 0) cancelledMoveDestination = moveDest;
                if (coverStopped) workSource!.Cancel();
                workToken.ThrowIfCancellationRequested();
            }

            // Same per-file error exclusion as the /d branch.
            long actualMovedBytes = moveResult.Errors.Count == 0
                ? totalBytes
                : SumBytesExcludingErrors(survivingFiles, moveResult.Errors);
            var moveOutcome = CliContract.ClassifyFileOperation(moveResult.MovedCount, moveResult.Errors.Count);
            await CarryReportAsync(() => ResultLogEntry.ForMove(
                scanResult, scanDurationMs, moveResult, actualMovedBytes, moveDurationMs,
                MoveDestinationClassifier.Classify(moveDest),
                reverify.WithHeldBack(moveResult.HeldBack, moveResult.HeldBackReasons),
                WindowsRegion(), ReportHost.CommandLine, RunningCopy(services)));

            // See the /d branch. The stopped Move returns through ReportAbortedMove
            // above and never reaches this.
            if (moveResult.MovedCount > 0 && !moveResult.Cancelled)
                AskForDonationAfterReport();

            // Size and nouns recomputed inside the en-GB scope; see the /d
            // summary above for why the stdout copies are not reused.
            MachineContract.WriteEventLog(moveOutcome.EventClass,
                () => string.Format(Strings.Cli_EventLogMoveSummary,
                    arg, moveResult.MovedCount, count, DisplayHelpers.PluraliseFile(count),
                    moveDest, DisplayHelpers.FormatSizeForMachine(actualMovedBytes), moveResult.Errors.Count,
                    DisplayHelpers.PluraliseError(moveResult.Errors.Count)));
            return moveOutcome.ExitCode;
        }
        catch (Exception ex) when (Classify(ex) == WorkFailure.Cancelled
            && cover is not null && workSource is { IsCancellationRequested: true } && !token.IsCancellationRequested)
        {
            // The run stopped because the cover no longer covered it: before its next file, or
            // in the scan or the re-verify. Nobody cancelled it, and why the cover stopped is
            // not established, so the line names no cause, only how far the run got, and for
            // a Move into which folder, and that the same command run again finishes it, with
            // no undo, the run being one to finish rather than put back. With nothing moved
            // or deleted it is a run to try again, as one turned away before it started is.
            if (committedCount == 0)
            {
                ReportMutexStop(
                    () => [arg == "/m" ? Strings.Cli_CoverLostNoWorkMove : Strings.Cli_CoverLostNoWorkDelete],
                    CliEventClass.TransientSkip,
                    () => string.Format(Strings.Cli_EventLogCoverLostNoWork, arg));
                return ExitTransient;
            }
            ReportMutexStop(
                () => [arg == "/m"
                    ? string.Format(Strings.Cli_MutexLostMove,
                        DisplayHelpers.FormatCount(committedCount), DisplayHelpers.FormatCount(totalToProcess),
                        DisplayHelpers.PluraliseFile(totalToProcess), cancelledMoveDestination)
                    : string.Format(Strings.Cli_MutexLostDelete,
                        DisplayHelpers.FormatCount(committedCount), DisplayHelpers.FormatCount(totalToProcess),
                        DisplayHelpers.PluraliseFile(totalToProcess))],
                CliEventClass.Partial,
                () => string.Format(Strings.Cli_EventLogMutexLostPartial,
                    arg, committedCount, totalToProcess, DisplayHelpers.PluraliseFile(totalToProcess)));
            return ExitPartial;
        }
        catch (Exception ex) when (Classify(ex) == WorkFailure.Cancelled)
        {
            // EventLog the cancellation so a Task Scheduler audit can see how far the run
            // got, and pick ExitPartial when work committed before the cancellation
            // arrived. A run cancelled before any file was processed (a cancel during the
            // scan, or before the first delete/move) still writes one entry so "each /s,
            // /d or /m run writes one summary" holds for every run: TransientSkip, not
            // Partial, since nothing committed and a re-run can succeed. Where the console
            // is being closed the entry goes before the lines, the run having only seconds
            // left and a console that may already be gone.
            void WriteEntry()
            {
                if (committedCount > 0)
                    MachineContract.WriteEventLog(CliEventClass.Partial,
                        () => string.Format(Strings.Cli_EventLogCancelledPartial,
                            arg, committedCount, totalToProcess,
                            DisplayHelpers.PluraliseFile(totalToProcess)));
                else
                    MachineContract.WriteEventLog(CliEventClass.TransientSkip,
                        () => string.Format(Strings.Cli_EventLogCancelledNoWork, arg));
            }
            var closing = CancelKey.Process.Closing;
            if (closing) WriteEntry();
            Console.WriteLine(Strings.Cli_Cancelled);
            // The undo, after the cancellation and not before it: the reader is told
            // the run stopped and then how to put back what it moved, which is the
            // order the window's card reads in. Empty unless a cancelled Move left
            // files in the destination, so a Delete and a Move that reached no file
            // print nothing here.
            if (cancelledMoveDestination.Length > 0)
                Console.WriteLine(string.Format(
                    Strings.Cli_MoveCancelledRestoreHint, cancelledMoveDestination));
            if (!closing) WriteEntry();
            return committedCount > 0 ? ExitPartial : ExitCancelled;
        }
        catch (Exception ex) when (Classify(ex) == WorkFailure.Localised)
        {
            // LocalisedAccessException and LocalisedInvalidOperationException
            // are the contract: services that raise them have built the
            // Message from a resx string with user-controlled template args
            // only, so echoing under elevation is safe and distinguishes "MSI
            // enumerator access denied" from "cannot write the destination
            // folder". BCL-raised UnauthorizedAccessException from deep in the
            // framework can carry cross-profile paths and falls through to the
            // generic catch below.
            Console.WriteLine(ex.Message);
            // The template is forced English (the RMM grep anchor "{0} mode
            // failed:"); ex.Message is already built in the OS language and
            // safe to echo, so the reason rides into the audit line localised
            // (the same sentence printed to stdout above).
            MachineContract.WriteEventLog(CliEventClass.HardError,
                () => string.Format(Strings.Cli_EventLogValidationFailed, arg, ex.Message));
            return ExitError;
        }
        catch (Exception ex)
        {
            // No specific catch anticipated this. Hand it to the shared
            // last-resort handler so a work-loop crash and a pre-flight crash
            // (Main's guard routes here too) report identically: crash.log, one
            // HardError audit entry, ExitError, and never ex.Message.
            return ReportUnexpectedError(arg, ex);
        }
    }

    /// <summary>
    /// Ends a scan-only run once every line it has is printed, and returns its exit code.
    /// What a scan-only run is asked for is its output, so a write to it that failed, such
    /// as one to a redirect on a full disk, fails the run: the run reports the guard's first
    /// failed write through <see cref="ReportUnexpectedError(string, Exception, ValueTuple{string, bool})"/>,
    /// whose HardError entry is its summary and whose line follows its output, and exits 1.
    /// Otherwise it writes the Ok entry <paramref name="summary"/> builds and exits 0. One
    /// summary either way, which is what an RMM counts runs by.
    ///
    /// WHERE THE APPLICATION LOG REFUSES THE OK ENTRY, THE NOTE SAYING SO IS PRINTED HERE, and
    /// its write counts like every other: where it fails, the run reports it and exits 1 as
    /// above. The flag is read on both sides of the write because it is sticky. Where it was
    /// already set, the log refused an earlier entry, the caller has printed the note, and the
    /// Ok entry may have landed, so a HardError after it would be a second summary. Where the
    /// write set it, the log took no entry, so the HardError is the only one that can land.
    /// </summary>
    private static int EndScanOnly(string arg, ConsoleGuard? output, Func<string> summary)
    {
        if (output?.FirstFailure is { } failure)
            return ReportUnexpectedError(arg, failure.Exception, failure.CrashLog);

        var refusedEarlier = EventLogWriter.EventLogUnavailable;
        MachineContract.WriteEventLog(CliEventClass.Ok, summary);
        if (refusedEarlier || !EventLogWriter.EventLogUnavailable)
            return ExitOk;

        NoteEventLogUnavailable();
        return output?.FirstFailure is { } noteFailure
            ? ReportUnexpectedError(arg, noteFailure.Exception, noteFailure.CrashLog)
            : ExitOk;
    }

    /// <summary>
    /// Whether a person can be watching this console: standard output is not
    /// redirected to a file or another program, and the process runs in an
    /// interactive window station, which <see cref="Interop.Native.WindowStation.IsInteractive()"/> answers.
    /// </summary>
    internal static bool ConsoleIsWatched() =>
        !Console.IsOutputRedirected && Interop.Native.WindowStation.IsInteractive();

    /// <summary>
    /// The tip line, after a blank line, on standard output only. It prints the tip
    /// page's address as it stands, with nothing added. Never written to the
    /// Application log, and nothing about the run's outcome or exit code depends on it.
    /// </summary>
    private static void WriteDonation()
    {
        Console.WriteLine();
        Console.WriteLine(string.Format(Strings.Cli_DonateAsk, SupportLink.Url));
    }

    /// <summary>
    /// Whether an earlier run left a report waiting to be sent, which
    /// <see cref="OfferReport(RunReport, IConsolePrompt, CancellationToken)"/> sends with
    /// a line of its own where the saved report is still there.
    /// </summary>
    private static bool ReportIsWaiting(IServiceProvider services) =>
        services.GetRequiredService<ISettingsService>().TryLoad(out var current)
        && ConsoleReportOffer.IsWaiting(current);

    /// <summary>
    /// The tip line a Delete or Move held back for the end of the run
    /// (<see cref="RunReport.DonationHeld"/>), printed after the report question and what
    /// followed it, so it is the last thing the run prints. A report offer ended by Ctrl+C,
    /// or a Ctrl+C that has come by then, leaves the line out: the cancellation stays the
    /// last line, as it does for a cancelled run. Console writes cannot throw, as
    /// <see cref="ConsoleGuard"/> carries every one.
    /// </summary>
    internal static void WriteHeldDonation(RunReport report, ReportOfferOutcome offer, CancellationToken token)
    {
        if (report.DonationHeld && offer != ReportOfferOutcome.Cancelled && !token.IsCancellationRequested)
            WriteDonation();
    }

    /// <summary>
    /// The three ways <see cref="DoWorkAsync"/> reports a failure, as <see cref="Classify"/>
    /// sorts them.
    /// </summary>
    private enum WorkFailure { Cancelled, Localised, Unforeseen }

    /// <summary>
    /// Which of <see cref="DoWorkAsync"/>'s catches reports a failure: a cancellation; a
    /// localised exception, whose message the app built from its own strings and which is
    /// safe to echo; or anything else, which the last catch reports without its message.
    /// <see cref="HandOverBatchAsync{TResult}"/> reads the same answer to decide whether a
    /// failed batch sets the first-run mark, so moving a type from one answer to another
    /// changes both at once.
    ///
    /// It names LocalisedAccessException and never its base type,
    /// UnauthorizedAccessException, which the framework raises with messages that can
    /// carry a path out of another user's profile. It is a type test and nothing more:
    /// the catches run it as an exception filter, ahead of the finally blocks between
    /// the throw and the catch, so it must not log, write or read a service.
    /// </summary>
    private static WorkFailure Classify(Exception ex) => ex switch
    {
        OperationCanceledException => WorkFailure.Cancelled,
        LocalisedAccessException or LocalisedInvalidOperationException => WorkFailure.Localised,
        _ => WorkFailure.Unforeseen,
    };

    /// <summary>
    /// Hands one batch to the Delete or Move service and records the PC's first run
    /// (<see cref="IFirstRunMark"/>) from how the batch ends. Where the service's own
    /// count of files moved or deleted (<paramref name="actedOn"/>) is above nought, the
    /// mark is set as the batch returns, ahead of everything the caller does with the
    /// result, the cancel re-entry among them. Where the service fails with an exception
    /// <see cref="Classify"/> calls unforeseen, which <see cref="DoWorkAsync"/> reports
    /// through its last catch, the mark is set before the failure goes on: how far that
    /// batch got is not known, and the failure line it ends in names no count for the
    /// window's start check to read.
    ///
    /// A cancellation and a localised exception leave the mark to the caller. The
    /// services raise a cancellation, and every localised exception but one, before any
    /// file is acted on. The one raised part way, a stopped Move (MoveAbortedException),
    /// carries the count of files it had moved, which its catch records. Any other
    /// localised exception a service raises part way needs the same, because it passes
    /// this filter without setting the mark.
    /// </summary>
    private static async Task<TResult> HandOverBatchAsync<TResult>(
        IFirstRunMark mark, Func<Task<TResult>> batch, Func<TResult, int> actedOn)
    {
        TResult result;
        try
        {
            result = await batch();
        }
        catch (Exception ex) when (Classify(ex) == WorkFailure.Unforeseen)
        {
            mark.Set();
            throw;
        }
        RecordFirstRun(mark, actedOn(result));
        return result;
    }

    /// <summary>
    /// Sets the PC's first-run mark where <paramref name="filesActedOn"/>, the service's
    /// own count of files moved or deleted, is above nought: a Delete's or a Move's
    /// result (<see cref="HandOverBatchAsync{TResult}"/>), and a stopped Move's partial result. A
    /// scan, and a run whose count came back at nought, leave the mark as it was here; a run
    /// at a console that carries its report sets it where it carries it.
    /// </summary>
    private static void RecordFirstRun(IFirstRunMark mark, int filesActedOn)
    {
        if (filesActedOn > 0)
            mark.Set();
    }

    /// <summary>
    /// Reports the scan-level conditions that are facts about the machine rather
    /// than about this run: how many files the scan left alone, an offer withheld, the
    /// drives and shares the scan stopped waiting for while files still had to be
    /// checked against them (<paramref name="sourcesGivenUp"/>, the scan's
    /// <see cref="ScanResult.SourceRootsGivenUpKeepingFiles"/> as the caller read
    /// it), superseded files kept back, every file the folder walk found where the scan found
    /// an in-progress signal set, files kept back because their times are not before the
    /// moment the scan began,
    /// records the scan could not fully read, and registrations naming a file that is
    /// not there.
    ///
    /// THE SURFACE IS PER CONDITION AND NOT ONE RULE OVER ALL OF THEM. Some reach
    /// stdout in the operator's language and the Application log in English,
    /// because scheduled tasks discard the first and RMM tools read the second.
    /// Others reach one of the two alone, each for the reason the comment at that
    /// condition gives.
    ///
    /// IT WRITES THE APPLICATION-LOG ENTRIES AND RETURNS THE STDOUT LINES, in the order
    /// they are to be printed, for the caller to print where its output needs them. Every
    /// entry is written before any of the lines is printed, so a console that fails while
    /// printing them costs none of these entries.
    /// </summary>
    /// <remarks>
    /// Called once, immediately after the scan, so every return the work loop can
    /// take from there on has written these entries first, the nothing-to-do one
    /// included: a fleet carrying either condition every night for a month
    /// otherwise looks, on the only surface anybody watches, exactly like a fleet
    /// with nothing to clean. The four returns in
    /// <see cref="ResolveAndValidateMoveDestination"/> come before the scan, so
    /// there is nothing to report by the time they take it.
    /// </remarks>
    private static List<string> ReportScanSignals(
        string arg, ScanResult scanResult, IReadOnlyList<SourceRootGivenUp> sourcesGivenUp)
    {
        // HOW MANY FILES THE SCAN LEFT ALONE, ON EVERY SCAN AND AS THE FIRST NOTICE. It is
        // the window's own count and phrase (ScanResult.LeftAloneCount through
        // LeftAloneReport), so the two hosts say the same for one machine state, and it
        // names no cause: what is true of every file it counts is that the app left it
        // alone. It follows the line saying what the scan found, or the clean line, and
        // comes ahead of the lines below, which say why particular files were kept. A /s
        // that lists files prints the notices ahead of its count line, so this is the first
        // line under the scanning one there.
        var lines = new List<string> { LeftAloneReport.Line(scanResult.LeftAloneCount) };

        // FILES THE FOLDER WALK FOUND WERE HELD BACK, IN ONE GO OR ONE AT A TIME. An
        // audit entry in the Application log for every machine that meets it.
        //
        // THIS BLOCK IS HERE RATHER THAN INSIDE THE EMPTY-OFFER BRANCH ABOVE THIS
        // METHOD, SO IT REACHES BOTH MACHINES. Written where the offer is empty, it
        // would miss the machine whose walk offer went while a superseded row
        // survived, and that is a machine a monitoring tool most needs to be able to
        // see: its "Found N unneeded files" is true and says nothing at all about the
        // half that was withheld.
        //
        // A NOTICE AND NOT AN OUTCOME. The run scanned, judged, withheld correctly
        // and reported, so its outcome entry stays in the 1000 band; what the band
        // cannot carry is the difference between this machine and a clean one, which
        // is what a number in the 3000 band is for. See CliEventClass.
        //
        // NOTHING IS REPORTED HERE FOR A RUN WHOSE EVERY HELD FILE WAS KEPT FOR A
        // PROGRAM WINDOWS STILL HAS INSTALLED, FOR ITS TIMES OR FOR ITS PATCH'S
        // REGISTRATIONS, the three WithholdingAccount.KeptWithoutNotice names:
        // HasWithholdingToReport is false for it. A file kept for its times has a line of
        // its own further down.
        if (scanResult.HasWithholdingToReport)
        {
            // WHICH OF THE TWO SENTENCES, ASKED ONCE. The wholesale one names a cause and
            // is spoken only for the reading that earns it.
            //
            // THE COUNT IS UnestablishedWithheldCount, the same figure the empty-offer
            // summary entry carries.
            var perFile = scanResult.Withholding != WithholdingAccount.WholeWalkOffer;
            var heldBack = scanResult.UnestablishedWithheldCount;

            MachineContract.WriteEventLog(CliEventClass.ScanWalkHeldBackNotice,
                () => string.Format(
                    perFile
                        ? Strings.Cli_EventLogNothingOfferedPerFileNotice
                        : Strings.Cli_EventLogNothingOfferedNotice,
                    arg, heldBack, DisplayHelpers.PluraliseFile(heldBack)));
        }

        // WHY EVERY FILE THE FOLDER WALK FOUND WAS HELD BACK, WHERE THE SCAN FOUND ONE OF
        // WINDOWS INSTALLER'S IN-PROGRESS SIGNALS SET JUST AFTER ITS CLOCK, OR THE SYSTEM
        // CLOCK WENT BACK WHILE IT READ THE FILES' TIMES. On every mode and on stdout alone:
        // the left-alone line above already counts those files, and the held-back entry is
        // the Application log's record of them. This says which signal, in the terms the
        // gate's own sentences use, or that the clock went back, and refuses nothing: where
        // anything is offered, /d and /m read the gate again before acting and stop where it
        // is set.
        var scanSignal = ScanSignalReport.CommandLine(scanResult);
        if (scanSignal.Length > 0) lines.Add(scanSignal);

        // THE DRIVES AND SHARES THE SCAN STOPPED WAITING FOR WHILE FILES STILL HAD TO BE
        // CHECKED AGAINST THEM, on stdout and as Event ID 3004. After the left-alone line
        // above, which also speaks of files the scan left alone, and ahead of the line
        // about registrations naming a file that is not there. The window draws its own
        // line under its left-alone count and above its missing-files line, which is this
        // order.
        var givenUp = SourcesGivenUpReport.CommandLine(sourcesGivenUp);
        if (givenUp.Length > 0) lines.Add(givenUp);
        NoteSourcesGivenUp(arg, sourcesGivenUp, duringTheCheck: false);

        // SUPERSEDED FILES HELD BACK, written to the Application log wherever the count
        // is above zero, as Event ID 3003. The count is SupersededHeldBackCount: the
        // superseded files whose removable verdict was taken away and those the
        // containment check refused or could not answer for. It names no cause, several
        // separate findings reaching this count and no sentence naming one of them being
        // true of the files the others contribute. A scheduled run throws the console
        // away, so without the entry the log would say what was offered and never that
        // superseded files were held back beside it.
        var supersededHeldBack = scanResult.SupersededHeldBackCount;
        if (supersededHeldBack > 0)
        {
            MachineContract.WriteEventLog(CliEventClass.ScanSupersededHeldBackNotice,
                () => string.Format(
                    DisplayHelpers.Pluralise(supersededHeldBack,
                        Strings.Cli_EventLogSupersededHeldBack_Singular,
                        Strings.Cli_EventLogSupersededHeldBack_Plural,
                        "Cli.EventLogSupersededHeldBack"),
                    arg, supersededHeldBack));
        }

        // THE NOTICE HAS ITS OWN CONDITION, SEPARATE FROM THE ENTRY ABOVE. The entry is
        // written on the count of superseded files held back; this is written where the
        // enumeration withheld every superseded patch because it could not check a
        // program entry in Windows Installer's records or match a cached patch file to a
        // program it asks about. It is the payload of Event ID 3000, a machine surface
        // with an RMM filter downstream. Re-gating it changes which machines log it with
        // every test still green, and a measurement that goes quiet reads exactly like
        // nothing being wrong.
        //
        // THEY ARE NOT TWO VIEWS OF ONE QUANTITY. This counts entries or files, the
        // trigger for ONE of the several routes into the count above. A machine can meet
        // either condition without the other: this one with no superseded file to hold
        // back, or the count above with nothing this one counts.
        //
        // The count is here, for an RMM to hang a filter on; stdout carries no line for this
        // condition. The program entries are the figure where there are any,
        // and the patch files only where they are the whole reason; see
        // InstallerQueryResult.UnaccountedProductCount for the three kinds of entry and
        // EnumerationCensus.UnattributedPatchFileCount for the files.
        var uncheckedEntries = scanResult.UnaccountedProductCount;
        var unmatchedPatchFiles = scanResult.Census.UnattributedPatchFileCount;
        if (uncheckedEntries > 0)
            MachineContract.WriteEventLog(CliEventClass.ScanRecordsIncompleteNotice,
                () => string.Format(
                    DisplayHelpers.Pluralise(uncheckedEntries,
                        Strings.Cli_EventLogScanWithheld_Singular,
                        Strings.Cli_EventLogScanWithheld_Plural,
                        "Cli.EventLogScanWithheld"),
                    arg, uncheckedEntries));
        else if (unmatchedPatchFiles > 0)
            MachineContract.WriteEventLog(CliEventClass.ScanRecordsIncompleteNotice,
                () => string.Format(
                    DisplayHelpers.Pluralise(unmatchedPatchFiles,
                        Strings.Cli_EventLogScanWithheldPatchFiles_Singular,
                        Strings.Cli_EventLogScanWithheldPatchFiles_Plural,
                        "Cli.EventLogScanWithheldPatchFiles"),
                    arg, unmatchedPatchFiles));

        // THE AFFECTED HALF, and the two hosts must not diverge on which population
        // this is. A registration whose absence the app positively established to be
        // harmless is not reported: the state alone would not establish that, Windows
        // opening every registered patch's cached file whichever state it carries, so
        // what earns the exclusion is the state CONJOINED with every sharing product
        // having been shown to hold no patch that could be uninstalled.
        if (scanResult.MissingAffectedCount > 0)
        {
            var programs = MissingFilesReport.Inline(
                MissingFilesReport.Products(scanResult.RegisteredPackages));
            lines.Add(string.Format(
                DisplayHelpers.Pluralise(scanResult.MissingAffectedCount,
                    Strings.Cli_MissingFromDisk_Singular,
                    Strings.Cli_MissingFromDisk_Plural,
                    "Cli.MissingFromDisk"),
                DisplayHelpers.FormatCount(scanResult.MissingAffectedCount), programs));
            MachineContract.WriteEventLog(CliEventClass.ScanMissingFilesNotice,
                () => string.Format(
                    DisplayHelpers.Pluralise(scanResult.MissingAffectedCount,
                        Strings.Cli_EventLogMissingFromDisk_Singular,
                        Strings.Cli_EventLogMissingFromDisk_Plural,
                        "Cli.EventLogMissingFromDisk"),
                    arg, scanResult.MissingAffectedCount));
        }

        // FILES HELD BACK BECAUSE THEIR TIMES ARE NOT BEFORE THE MOMENT THE SCAN BEGAN: the
        // sentence the window's cards carry (RecentChangeReport.Line), printed wherever the
        // scan held at least one file back for that reason, on every mode and whatever
        // else was offered. It is on stdout only, for whoever reads the console; no
        // Application-log entry carries it.
        var recentChange = RecentChangeReport.Line(scanResult);
        if (recentChange.Length > 0)
            lines.Add(recentChange);

        return lines;
    }

    /// <summary>
    /// Prints a bad-invocation message and usage, writes the one shared
    /// Application-log entry, and returns <see cref="ExitError"/>. The stdout
    /// <paramref name="stdoutMessage"/> differs between an unrecognised flag and a
    /// recognised flag with an extra token, but the audit entry
    /// (<c>Cli.EventLogBadArguments</c>) and the exit code are identical for both,
    /// so an RMM filter matching the Application channel sees one "bad arguments"
    /// contract whichever the user hit. The arg switch returns before
    /// <see cref="RunWorkAsync"/>, which prints the note saying the Application log
    /// refused an entry as it ends, so the note is printed here.
    /// </summary>
    private static int ReportBadArguments(CliInvocation invocation, string stdoutMessage)
    {
        Console.WriteLine(stdoutMessage);
        Console.WriteLine();
        PrintUsage();
        MachineContract.WriteEventLog(CliEventClass.HardError,
            () => string.Format(Strings.Cli_EventLogBadArguments, invocation.OffendingArgument));
        NoteEventLogUnavailable();
        return ExitError;
    }

    /// <summary>
    /// The last-resort handler for an exception no specific catch anticipated,
    /// shared by <see cref="DoWorkAsync"/>'s catch-all and <see cref="Main"/>'s
    /// pre-flight guard so the two report on the same contract. Writes the full
    /// detail to crash.log and reports the exception through the overload below.
    /// </summary>
    private static int ReportUnexpectedError(string mode, Exception ex) =>
        ReportUnexpectedError(mode, ex, Helpers.CrashLog.TryWrite(ex));

    /// <summary>
    /// Reports an exception crash.log has already been given, <paramref name="crash"/>
    /// being where it went, so the exception is not recorded twice: the overload above
    /// for the two catch-alls, and <see cref="EndScanOnly"/> for a scan-only run's first
    /// failed write. Prints only the exception type name and the crash-log path (never
    /// <c>ex.Message</c>: under elevation it can carry a path out of another user's
    /// profile, and Task Scheduler / RMM tooling captures stdout to disk), records one
    /// HardError Application-log entry, and returns <see cref="ExitError"/> so a scheduled
    /// task sees a documented exit code rather than a runtime abort with an undocumented
    /// one. Safe to call after the console itself has failed (the stdout write is guarded,
    /// and <see cref="EventLogWriter"/> swallows its own IO).
    /// </summary>
    private static int ReportUnexpectedError(string mode, Exception ex, (string Path, bool Written) crash)
    {
        var typeName = ex.GetType().Name;
        try
        {
            Console.WriteLine(crash.Written
                ? string.Format(Strings.Cli_GenericError, typeName, crash.Path)
                : string.Format(Strings.Cli_GenericError_NoLog, typeName));
        }
        catch (Exception)
        {
            // Broad on purpose, and broader than a stdout failure alone would need.
            // Main's own catch clause reaches this through ReportRunFailure, so anything
            // it lets past there reaches the runtime default handler and produces the
            // undocumented exit the routing exists to prevent. Two things in the try
            // can throw: the write itself, and formatting a resx string. The exception
            // being reported has already been given to crash.log, and the audit entry
            // below still fires.
        }
        MachineContract.WriteEventLog(CliEventClass.HardError, () => crash.Written
            ? string.Format(Strings.Cli_EventLogHardError, mode, typeName, crash.Path)
            : string.Format(Strings.Cli_EventLogHardError_NoLog, mode, typeName));
        return ExitError;
    }

    /// <summary>
    /// Sums the SizeBytes of every scanned removable file whose FullPath
    /// is not in the error list. Matches the GUI's actually-moved-bytes
    /// computation at CleanupViewModel.cs so a fleet of GUI and CLI
    /// machines produces telemetry on the same axis.
    /// </summary>
    private static long SumBytesExcludingErrors(
        IReadOnlyList<OrphanedFile> removableFiles,
        IReadOnlyList<FileOperationError> errors)
    {
        var errorPaths = new HashSet<string>(
            errors.Select(e => e.FilePath), StringComparer.OrdinalIgnoreCase);
        return removableFiles
            .Where(f => !errorPaths.Contains(f.FullPath))
            .Sum(f => f.SizeBytes);
    }

    /// <summary>
    /// Drops the paths an action service held back from this run's own list of
    /// the files it meant to act on. They were never touched, so they leave the
    /// count, the byte total and every figure taken from the list.
    ///
    /// One helper for the three places that need it rather than the same three
    /// lines written out at each, because the three are not equally obvious and
    /// the least obvious one was missed. The completed and cancelled paths use
    /// the list for sums; the aborted path feeds it to
    /// <see cref="CompletedBytes"/>, which reads it POSITIONALLY, so a held-back
    /// entry left in it is not an inflated total but the wrong rows entirely.
    /// </summary>
    internal static List<OrphanedFile> FoldHeldBack(
        IReadOnlyList<OrphanedFile> files, IReadOnlyList<string> heldBack)
    {
        if (heldBack.Count == 0) return files.ToList();

        // The same case-insensitive comparison the scan matches paths on: a claim's
        // package path is normalised the way the registered rows are, so a held-back
        // path differing only in case still names the file it names.
        var reclaimed = new HashSet<string>(heldBack, StringComparer.OrdinalIgnoreCase);
        return files.Where(f => !reclaimed.Contains(f.FullPath)).ToList();
    }

    /// <summary>
    /// Bytes of the files a batch that stopped part way actually moved. The
    /// action services take their input in order and stop where they stop, so the
    /// files they reached are the first (<paramref name="completedCount"/> plus
    /// the errors); of those, the ones that did not error are what moved.
    /// <see cref="SumBytesExcludingErrors"/> can lean on "no errors means every
    /// file completed" instead, which a batch that stopped cannot.
    /// Matches CleanupViewModel's own CompletedBytes so a fleet of GUI and CLI
    /// machines produces telemetry on the same axis.
    /// </summary>
    internal static long CompletedBytes(
        IReadOnlyList<OrphanedFile> files, int completedCount,
        IReadOnlyList<FileOperationError> errors)
    {
        var reached = completedCount + errors.Count;
        if (errors.Count == 0)
            return files.Take(reached).Sum(f => f.SizeBytes);

        var errorPaths = new HashSet<string>(
            errors.Select(e => e.FilePath), StringComparer.OrdinalIgnoreCase);
        return files.Take(reached).Where(f => !errorPaths.Contains(f.FullPath)).Sum(f => f.SizeBytes);
    }

    /// <summary>
    /// Reports a Move that one of the service's own destination guards stopped
    /// part way, and returns the exit code for it. What makes it owed is that
    /// files have already left <c>C:\Windows\Installer</c> and are in the
    /// destination folder: without it a scheduled task's log records the
    /// destination problem and not where those files went.
    /// </summary>
    /// <remarks>
    /// The summary and the error block come first, in the shape and the order an
    /// ordinary <c>/m</c> prints them, so a script's <c>\d+ errors:</c> scrape lands
    /// where it always does. The guard's reason follows, and the line naming the
    /// backup folder comes last, under the news rather than above it.
    /// </remarks>
    private static int ReportAbortedMove(
        string arg, MoveAbortedException ex, string moveDest, int count,
        IReadOnlyList<OrphanedFile> survivingFiles)
    {
        var partial = ex.Partial;
        Console.WriteLine(string.Format(
            DisplayHelpers.Pluralise(partial.MovedCount, Strings.Cli_MovedFiles, "Cli.MovedFiles"),
            DisplayHelpers.FormatCount(partial.MovedCount),
            DisplayHelpers.PluraliseFile(partial.MovedCount)));
        if (partial.Errors.Count > 0)
        {
            // See the /d branch for the always-plural rationale and the
            // English-forced noun.
            Console.WriteLine(MachineContract.English(
                () => $"{partial.Errors.Count} {Strings.Plural_Error_Plural}:"));
            foreach (var err in partial.Errors)
                Console.WriteLine($"  {Path.GetFileName(err.FilePath)}: {err.LocalisedMessage}");
        }
        // The command line's wording of the guard's sentence rather than
        // ex.Message, which is the window's: that one closes on Re-scan, a button
        // this surface has not got. Substituting it is sound because
        // MoveAbortedException is raised from exactly one place, always built
        // from Error.DestinationChangedMidBatch over the destination this method
        // was handed, so the two sentences can only ever describe the same fault
        // about the same folder. Neither selects on MoveAbortReason, and both
        // hosts are held to that together: the guard's two conditions differ in
        // nothing the reader of this line does next, and a sentence that named
        // one of them would be naming it for a batch that met the other.
        //
        // It names the configured path on purpose, asking the reader to go and look
        // at what they set. The line below names where the files went, and the two
        // are different folders in exactly the case this method reports.
        Console.WriteLine(string.Format(Strings.Cli_DestinationChangedMidBatch, moveDest));
        // UNDER THE SENTENCE SAYING THE RUN WENT NO FURTHER, which is the order the
        // cancel re-entry above takes with its own pair: the reader learns the run
        // stopped and is then told what to do about the files it had already moved,
        // so the line they act on is the last one. Above the news, an instruction to
        // check their programs and then delete the backup folder reads as the close
        // of a run that went the distance.
        //
        // IT NAMES ex.Destination AND NOT THE PATH THIS METHOD WAS HANDED, which is
        // the folder the files are actually in and the one thing this call does
        // differently from the ordinary /m one; the property's own summary says why
        // the two can differ. The line's whole job is to name the folder somebody
        // has to go and delete.
        //
        // Only where something moved, as in the /m branch.
        if (partial.MovedCount > 0)
            Console.WriteLine(string.Format(Strings.Cli_MoveRestoreHint, ex.Destination));

        var outcome = CliContract.ClassifyAbortedMove(partial.MovedCount);
        // Built inside the en-GB scope, never before it; see the /d summary for
        // why the stdout copies are not reused.
        MachineContract.WriteEventLog(outcome.EventClass,
            () => AbortedMoveEventLogLine(arg, ex, count, survivingFiles));
        return outcome.ExitCode;
    }

    /// <summary>
    /// The Application-channel line for a stopped Move. Its own method so a test
    /// can read the sentence rather than the event log, this being the surface a
    /// sysadmin filters on and the one with no companion warning beside it.
    /// </summary>
    /// <remarks>
    /// It names <see cref="MoveAbortedException.Destination"/> and never the
    /// destination the run was given, which is the one place in this host where
    /// the two can be different folders; the property's own summary says why. The
    /// stdout sentence above keeps the given path on purpose, asking the reader to
    /// go and check the folder they configured.
    /// </remarks>
    internal static string AbortedMoveEventLogLine(
        string arg, MoveAbortedException ex, int count,
        IReadOnlyList<OrphanedFile> survivingFiles)
    {
        var partial = ex.Partial;
        return string.Format(Strings.Cli_EventLogMoveAborted,
            arg, partial.MovedCount, count, DisplayHelpers.PluraliseFile(count), ex.Destination,
            DisplayHelpers.FormatSizeForMachine(CompletedBytes(survivingFiles, partial.MovedCount, partial.Errors)),
            partial.Errors.Count, DisplayHelpers.PluraliseError(partial.Errors.Count));
    }

    /// <summary>
    /// The stdout sentence for a run refused because <c>Global\_MSIExecute</c>
    /// could not be taken and nothing could be shown to be holding it, chosen by
    /// the flag that ran. <paramref name="arg"/> is the lower-cased flag, so the
    /// comparison needs no casing rules.
    /// </summary>
    /// <remarks>
    /// Two sentences rather than one, because each closes by naming what did not
    /// happen to the files and neither ending is true of the other's run. Keyed
    /// off the flag rather than a bool the caller passes, so a caller cannot hand
    /// this the wrong one; a third mutating mode would have to come here for a
    /// sentence of its own, which is the failure worth having.
    /// </remarks>
    internal static string InstallerLockUnavailableLine(string arg) =>
        arg == "/m" ? Strings.Cli_MoveInstallerLockUnavailable : Strings.Cli_InstallerLockUnavailable;

    /// <summary>
    /// The Application-channel line for that same refusal. ONE line covers both
    /// flags and <c>{0}</c> names which one ran, so its tail has to be true of a
    /// move and of a delete alike: an ending naming a single action is false of
    /// half the runs that can produce it.
    /// </summary>
    /// <remarks>
    /// Built outside the en-GB scope, like <see cref="AbortedMoveEventLogLine"/>:
    /// the caller wraps it, so the line renders English in production and in the
    /// ambient culture anywhere else.
    /// </remarks>
    internal static string InstallerLockUnavailableEventLogLine(string arg) =>
        string.Format(Strings.Cli_EventLogInstallerLockUnavailable, arg);

    /// <summary>
    /// The stdout sentence for a run refused because the security on
    /// <c>Global\_MSIExecute</c> would not let the app open it, wherever the
    /// refusal was met: <see cref="PendingRebootBlockedMessage"/> prints it for
    /// <see cref="PendingRebootReason.MsiExecuteMutexAccessRefused"/>, which covers the
    /// gate and the action services' acquire alike. Keyed off the flag exactly as
    /// <see cref="InstallerLockUnavailableLine"/> is, and separate from it because
    /// the two refusals are different facts about the machine: one says nothing
    /// could be shown to hold the lock, this one says the app was not allowed to
    /// look.
    /// </summary>
    internal static string InstallerLockAccessRefusedLine(string arg) =>
        arg == "/m" ? Strings.Cli_MoveInstallerLockAccessRefused : Strings.Cli_InstallerLockAccessRefused;

    /// <summary>
    /// Reports a <c>/d</c> or <c>/m</c> the action service refused because it could
    /// not take <c>Global\_MSIExecute</c> and nothing was shown to be holding it, and
    /// returns the exit code for it.
    /// </summary>
    /// <remarks>
    /// Deliberately NOT routed through <see cref="EmitPendingRebootBlocked"/>: no
    /// <see cref="PendingRebootReason"/> describes it. The gate's probe reads the
    /// same failure as not held, so the gate never reports this condition and has no
    /// sentence for it. TransientSkip and ExitTransient, on the reasoning that
    /// method gives its transient reasons: the condition can clear on its own, so a
    /// scheduler should come back rather than treat the machine as broken.
    ///
    /// The two lines it emits are built by methods of their own, so their wording
    /// can be read back without driving a run.
    /// </remarks>
    private static int EmitInstallerLockUnavailable(string arg)
    {
        Console.WriteLine(InstallerLockUnavailableLine(arg));
        MachineContract.WriteEventLog(CliEventClass.TransientSkip,
            () => InstallerLockUnavailableEventLogLine(arg));
        return ExitTransient;
    }

    /// <summary>
    /// The Application-channel line for a <c>/s</c>, <c>/d</c> or <c>/m</c> run
    /// refused for want of administrator rights. One line covers all three flags and
    /// <c>{0}</c> names which one ran.
    /// </summary>
    /// <remarks>
    /// Built outside the en-GB scope, like <see cref="InstallerLockUnavailableEventLogLine"/>:
    /// the caller wraps it, so the line renders English in production and in the
    /// ambient culture anywhere else.
    /// </remarks>
    internal static string AdminRightsNeededEventLogLine(string arg) =>
        string.Format(Strings.Cli_EventLogAdminRightsNeeded, arg);

    /// <summary>
    /// Reports a <c>/s</c>, <c>/d</c> or <c>/m</c> run refused because the process
    /// holds no administrator rights, and returns the exit code for it.
    /// </summary>
    /// <remarks>
    /// HardError and ExitError, on <see cref="CliExitCode.Error"/>'s own rule: the
    /// rights a process runs with change only when somebody starts it differently, so
    /// a scheduler retrying on the transient code would be refused on every run.
    ///
    /// It returns before <see cref="RunWorkAsync"/>, which prints the note on an unwritable
    /// Application channel as it ends, so it prints the note itself, as the mutex refusal
    /// does.
    ///
    /// The event-log line it writes (<see cref="AdminRightsNeededEventLogLine"/>) is
    /// reachable separately and this method is not, for the reason
    /// <see cref="EmitInstallerLockUnavailable"/> gives.
    /// </remarks>
    private static int RefuseWithoutAdministratorRights(string arg)
    {
        Console.WriteLine(Strings.Cli_AdminRightsNeeded);
        MachineContract.WriteEventLog(CliEventClass.HardError,
            () => AdminRightsNeededEventLogLine(arg));
        NoteEventLogUnavailable();
        return ExitError;
    }

    /// <summary>
    /// The sentence a blocked run prints, one per reason.
    ///
    /// Its own method rather than an expression inside the emitter because the emitter
    /// writes to the console and to the Application channel, so nothing could read this
    /// back without doing both. CliPendingRebootStringsTests walks the enum against it,
    /// which is what keeps the fallback below unreachable; the window's banner is held
    /// to the same standard by ScanViewModelPendingRebootTests and is testable already,
    /// being a bound property.
    ///
    /// <paramref name="arg"/> is the lower-cased flag. One reason reads it: a refused
    /// lock prints the sentence the action services' refusal prints, which closes by
    /// naming what did not happen to the files and so comes in a /d and a /m form.
    /// </summary>
    internal static string PendingRebootBlockedMessage(
        string arg, PendingRebootReason reason, string? detail, int? windowsError = null) =>
        reason switch
        {
            PendingRebootReason.MsiExecuteMutexHeld =>
                Strings.Cli_PendingRebootBlocked_MsiExecuteMutex,
            PendingRebootReason.MsiExecuteMutexAccessRefused =>
                InstallerLockAccessRefusedLine(arg),
            PendingRebootReason.InstallerInProgress =>
                Strings.Cli_PendingRebootBlocked_InstallerInProgress,
            PendingRebootReason.PendingRenameInCache =>
                string.Format(
                    Strings.Cli_PendingRebootBlocked_PendingRenameInCache,
                    detail ?? string.Empty),
            PendingRebootReason.PendingRenameUnresolved =>
                Strings.Cli_PendingRebootBlocked_PendingRenameUnresolved,
            PendingRebootReason.RegistryCheckUnreadable =>
                Strings.Cli_PendingRebootBlocked_RegistryCheckUnreadable,
            // The in-progress file's own sentence is the one the fallback below
            // prints: Windows Installer has something in progress, which is what
            // the file records. A reason added later without a line of its own
            // falls through to the same text and meets the test's distinctness
            // check, which this arm keeps armed.
            PendingRebootReason.InstallerInProgressMarker =>
                Strings.Cli_PendingRebootBlocked_Other,
            // Windows refused the read of the in-progress file: the refused-lock
            // sentence says Windows refused permission to check whether Windows
            // Installer was busy, which is as true of this read as of the mutex's.
            PendingRebootReason.InstallerInProgressMarkerAccessRefused =>
                InstallerLockAccessRefusedLine(arg),
            // Given with the Windows error the check failed on, where it failed on one.
            PendingRebootReason.MsiExecuteMutexUnreadable =>
                WindowsErrors.Sentence(windowsError,
                    Strings.Cli_PendingRebootBlocked_MsiExecuteMutexUnreadable,
                    Strings.Cli_PendingRebootBlocked_MsiExecuteMutexUnreadableWindowsError),
            // What a reason with no line of its own gets: a sentence true of the
            // whole family, where a throw would reach the generic catch and report
            // a held run as an unexpected crash. It cannot fire for a null reason
            // either: PendingRebootResult.Block takes a non-nullable one.
            _ => Strings.Cli_PendingRebootBlocked_Other,
        };

    /// <summary>
    /// The short label the Application channel carries for a blocked run, one per
    /// reason. English by design, as every Cli.EventLogReason.* value is: the channel is
    /// sysadmin-facing and an RMM grep needs a stable target whatever the OS UI culture.
    ///
    /// The fallback is the enum member's own name, so a reason added without a label
    /// still leaves something greppable rather than a blank. That is a floor and not a
    /// substitute for the label, which is why the same test walks this too.
    ///
    /// CALL IT FROM INSIDE THE EVENT-LOG SCOPE. It reads through the ordinary door, so
    /// what it returns follows whatever culture is current when it runs; hoisted out of
    /// that scope it would answer in the OS language and quietly undo the arrangement
    /// its caller's comment describes.
    /// </summary>
    internal static string PendingRebootEventLogReason(PendingRebootReason reason) =>
        reason switch
        {
            PendingRebootReason.MsiExecuteMutexHeld =>
                Strings.Cli_EventLogReason_MsiExecuteMutex,
            PendingRebootReason.MsiExecuteMutexAccessRefused =>
                Strings.Cli_EventLogReason_MsiExecuteMutexAccessRefused,
            PendingRebootReason.InstallerInProgress =>
                Strings.Cli_EventLogReason_InstallerInProgress,
            PendingRebootReason.PendingRenameInCache =>
                Strings.Cli_EventLogReason_PendingRenameInCache,
            PendingRebootReason.PendingRenameUnresolved =>
                Strings.Cli_EventLogReason_PendingRenameUnresolved,
            PendingRebootReason.RegistryCheckUnreadable =>
                Strings.Cli_EventLogReason_RegistryCheckUnreadable,
            // The two in-progress-file reasons are labelled with their own names,
            // written out here so the choice is visible rather than left to the
            // fallback. Each is stable and names what the gate read.
            PendingRebootReason.InstallerInProgressMarker =>
                nameof(PendingRebootReason.InstallerInProgressMarker),
            PendingRebootReason.InstallerInProgressMarkerAccessRefused =>
                nameof(PendingRebootReason.InstallerInProgressMarkerAccessRefused),
            PendingRebootReason.MsiExecuteMutexUnreadable =>
                Strings.Cli_EventLogReason_MsiExecuteMutexUnreadable,
            _ => reason.ToString(),
        };

    /// <summary>
    /// The whole line the Application channel carries for a blocked run: the mode,
    /// the reason's own label, and the resolved path for the one reason that has
    /// one.
    ///
    /// A method of its own for the reason the two above have one: the emitter writes
    /// to the console and to the channel in the same breath, so nothing could read
    /// this line back without doing both.
    ///
    /// WHAT THE FIXED HALF SAYS, AND WHY IT SAYS SO LITTLE. The label is the whole of
    /// what the line claims about the condition. The reasons run from an installer
    /// running right now, through a suspended transaction and operations queued for the
    /// next restart, to checks the app was refused or could not make, so nothing shorter
    /// than the label is true of all of them.
    ///
    /// The detail arrives carrying its own separator, which is why the template ends
    /// in a placeholder with no space in front of it: the reasons that never carry a
    /// detail would otherwise each log a line with a space hanging off it. A Windows error
    /// the check failed on is the detail where there is one, as its number alone
    /// (<c>Cli.EventLogWindowsError</c>): the text Windows gives for it is in the language Windows
    /// is shown in, and this channel is English.
    ///
    /// CALL IT FROM INSIDE THE EVENT-LOG SCOPE, for the reason
    /// <see cref="PendingRebootEventLogReason"/> gives: it reads through the ordinary
    /// door, so it answers in whatever culture is current when it runs.
    /// </summary>
    internal static string PendingRebootEventLogLine(
        string arg, PendingRebootReason reason, string? detail, int? windowsError = null)
    {
        var logged = windowsError is { } number
            ? string.Format(Strings.Cli_EventLogWindowsError, WindowsErrors.Number(number))
            : detail;
        return string.Format(
            Strings.Cli_EventLogPendingRebootBlocked,
            arg,
            PendingRebootEventLogReason(reason),
            string.IsNullOrEmpty(logged) ? string.Empty : " " + logged);
    }

    /// <summary>
    /// Emits the pending-reboot-blocked outcome: the localised stdout reason
    /// sentence, the English Application-log entry, and the exit code its reason
    /// takes.
    /// Shared by the pre-act gate check and the action services' own boundary
    /// refusal. When a Move or Delete service acquires <c>Global\_MSIExecute</c>
    /// and finds it held
    /// (<see cref="MoveResult.InstallerBusy"/> /
    /// <see cref="DeleteResult.InstallerBusy"/>), a Windows Installer
    /// transaction took it after the gate check passed; mapping that to
    /// <see cref="PendingRebootReason.MsiExecuteMutexHeld"/> here
    /// makes the service-boundary refusal produce the identical machine contract
    /// (stdout line, event-log entry, exit code) a gate block does, so an RMM
    /// consumer cannot tell the two apart. A service refused the rights to open the
    /// object (<see cref="MoveResult.InstallerLockAccessRefused"/> /
    /// <see cref="DeleteResult.InstallerLockAccessRefused"/>) maps to
    /// <see cref="PendingRebootReason.MsiExecuteMutexAccessRefused"/> on the same
    /// terms, so one refusal carries one sentence and one exit code wherever it is
    /// met.
    /// </summary>
    private static int EmitPendingRebootBlocked(
        string arg, PendingRebootReason reason, string? detail, int? windowsError = null)
    {
        Console.WriteLine(PendingRebootBlockedMessage(arg, reason, detail, windowsError));

        // WHAT A SCHEDULER IS TOLD, DECIDED FROM THE REASON AND TAKEN AS A PAIR.
        // The exit code and the entry class are one statement about this run said to
        // two audiences, so they are read out of one expression: a code meaning
        // "come back later" beside an entry classed as a run that failed outright
        // would leave the machine contract disagreeing with itself.
        //
        // THE REASONS THAT ASSERT SOMETHING IS IN FLIGHT TAKE THE TRANSIENT CODE. An
        // install holding the mutex, a suspended transaction, Windows Installer's
        // in-progress file, a queued rename: each of those ends by itself, so a
        // scheduler that comes back finds the machine in a different state.
        //
        // A READ THE APP COULD NOT MAKE IS NOT ONE OF THEM, and this is the arm the
        // default falls to. A sample of the lock that failed is such a read. Nothing
        // about waiting says the read will succeed next time, and the sentence this run
        // prints tells the operator that if it still fails, this is not a machine
        // InstallerClean can clean. A code
        // meaning "temporary, retry" under those words has a nightly task coming
        // back for ever. The code it takes instead claims only what is true of the
        // run: nothing was processed. A reason added to the enum later lands here
        // too, on the same reasoning, until somebody decides otherwise in
        // CliPendingRebootOutcomeTests' own table.
        //
        // NOR IS A LOCK OR AN IN-PROGRESS FILE THE APP WAS REFUSED PERMISSION TO
        // READ, which take the same arm. Nothing has been seen behind either refusal,
        // and the security on an object does not change by being waited on: the
        // refusal stands for as long as the object does, and every run until then is
        // refused identically.
        var (exitCode, entryClass) = reason switch
        {
            PendingRebootReason.MsiExecuteMutexHeld or
            PendingRebootReason.InstallerInProgress or
            PendingRebootReason.InstallerInProgressMarker or
            PendingRebootReason.PendingRenameInCache or
            PendingRebootReason.PendingRenameUnresolved =>
                (ExitTransient, CliEventClass.TransientSkip),
            _ => (ExitError, CliEventClass.HardError),
        };

        // The reason label and template are built English: the Cli.EventLogReason.*
        // labels are translated in the Japanese satellite and in no other, but the
        // Application channel is sysadmin-facing and an RMM grep on a known phrase
        // needs a stable English target. Those Japanese values are inert rather than
        // wrong. The localised stdout sentence above is what the operator reads; the
        // label is resolved INSIDE the scope below, which is what makes it en-GB
        // rather than the OS language, so the call stays in the lambda.
        MachineContract.WriteEventLog(entryClass, () =>
            PendingRebootEventLogLine(arg, reason, detail, windowsError));
        return exitCode;
    }

    /// <summary>
    /// Resolves the /m destination (the command-line path, or the path saved
    /// in %LOCALAPPDATA% when none was given) and runs the fully-qualified,
    /// not-inside-Installer and not-inside-System-folder gates. Returns null
    /// when the destination is valid, setting <paramref name="dest"/>; or the
    /// exit code to return when it is not, after printing the stdout reason
    /// and writing the EventLog entry. Both sources have the same trust
    /// posture once the CLI runs elevated, so a stale Scheduled Task argument
    /// is gated exactly like a stale settings.json. Called before the scan so
    /// a misconfigured /m fails before paying for a full Installer-folder walk.
    /// </summary>
    private static int? ResolveAndValidateMoveDestination(
        IServiceProvider services, CliInvocation invocation, string arg, out string dest)
    {
        dest = invocation.MoveDestination is not null
            ? invocation.MoveDestination.Trim()
            : services.GetRequiredService<ISettingsService>().Load().MoveDestination;

        if (string.IsNullOrWhiteSpace(dest))
        {
            Console.WriteLine(Strings.Cli_NoMoveDestination);
            MachineContract.WriteEventLog(CliEventClass.HardError,
                () => string.Format(Strings.Cli_EventLogMoveNoDestination, arg));
            return ExitError;
        }

        // The event-log lambdas below capture the destination, but an out
        // parameter cannot be captured (CS1628), so copy it into a local. dest
        // is assigned once above and only read after, so the two stay equal.
        var resolved = dest;

        // Reject relative destinations: Path.GetFullPath would otherwise
        // resolve them against the process CWD, and the CLI host's CWD is
        // whatever the caller invoked it from.
        if (!Path.IsPathFullyQualified(dest))
        {
            Console.WriteLine(string.Format(Strings.Cli_MoveDestinationRelative, dest));
            MachineContract.WriteEventLog(CliEventClass.HardError,
                () => string.Format(Strings.Cli_EventLogMoveDestinationRelative, arg, resolved));
            return ExitError;
        }

        if (InstallerCacheHelpers.IsInstallerFolderOrChild(dest))
        {
            Console.WriteLine(Strings.Cli_MoveDestinationInsideInstaller);
            MachineContract.WriteEventLog(CliEventClass.HardError,
                () => MoveDestinationInsideInstallerEventLogLine(arg, resolved));
            return ExitError;
        }

        if (InstallerCacheHelpers.IsSystemFolderOrChild(dest))
        {
            Console.WriteLine(string.Format(Strings.Cli_MoveDestinationInSystemFolder, dest));
            MachineContract.WriteEventLog(CliEventClass.HardError,
                () => string.Format(Strings.Cli_EventLogMoveDestinationInSystemFolder, arg, resolved));
            return ExitError;
        }

        return null;
    }

    /// <summary>
    /// The Application-channel line for a <c>/m</c> whose destination resolves
    /// into the installer cache. <c>{1}</c> is the destination as the run was
    /// given it, and it is the whole of what this entry adds: the sentence
    /// printed to stdout beside it names no path, so this line is the only
    /// record of where the run was pointed.
    /// </summary>
    /// <remarks>
    /// Its own method so the wording can be read back without driving a run.
    ///
    /// Built outside the en-GB scope, like <see cref="AbortedMoveEventLogLine"/>:
    /// the caller wraps it, so the line renders English in production and in the
    /// ambient culture anywhere else.
    /// </remarks>
    internal static string MoveDestinationInsideInstallerEventLogLine(
        string arg, string destination) =>
        string.Format(Strings.Cli_EventLogMoveDestinationInsideInstaller, arg, destination);

    private static void PrintVersion()
    {
        // The embedded assembly version, formatted Major.Minor.Patch to match
        // the user-facing version (the fourth component is always 0 in this
        // project's scheme). AssemblyInformationalVersion carries a +<commit>
        // suffix from the deterministic build and is deliberately not used.
        var name = System.Reflection.Assembly.GetExecutingAssembly().GetName();
        var version = name.Version?.ToString(3) ?? "0.0.0";
        Console.WriteLine($"{name.Name} {version}");
    }

    private static void PrintUsage()
    {
        WriteHelp(Strings.Cli_Help_Header);
        WriteHelp(Strings.Cli_Help_Summary);
        WriteHelp(Strings.Cli_Help_Elevation);
        Console.WriteLine();
        WriteHelp(Strings.Cli_Help_Usage);
        WriteHelp(Strings.Cli_Help_Help);
        WriteHelp(Strings.Cli_Help_Version);
        WriteHelp(Strings.Cli_Help_ScanOnly);
        WriteHelp(Strings.Cli_Help_Delete);
        WriteHelp(Strings.Cli_Help_MoveDefault);
        WriteHelp(Strings.Cli_Help_MovePath);
        Console.WriteLine();
        // The saved /m default lives under %LOCALAPPDATA%, which a scheduled task
        // or service account (SYSTEM) does not share with the interactive user, so
        // a no-path /m there exits 1 every run: say so where /m is documented.
        WriteHelp(Strings.Cli_Help_MoveScheduledNote);
        Console.WriteLine();
        WriteHelp(Strings.Cli_Help_UpdateNote);
        Console.WriteLine();
        WriteHelp(Strings.Cli_Help_ExitCodesHeader);
        WriteHelp(Strings.Cli_Help_ExitCodeOk);
        WriteHelp(Strings.Cli_Help_ExitCodeError);
        WriteHelp(Strings.Cli_Help_ExitCodePartial);
        WriteHelp(Strings.Cli_Help_ExitCodeTransient);
        WriteHelp(Strings.Cli_Help_ExitCodeCancelled);
        Console.WriteLine();
        WriteHelp(Strings.Cli_Help_NoteLine1);
        Console.WriteLine();
    }

    /// <summary>
    /// Writes one help value, a console line per line break inside it.
    /// </summary>
    /// <remarks>
    /// A help value may carry more than one printed line, and the break is an
    /// <c>&amp;#10;</c> in the resx, so the string arrives holding a bare LF where
    /// every other line in the block ends in whatever the host calls a newline.
    /// Splitting first means the console is handed the same sequence for every
    /// break on the screen instead of two kinds. Every value goes through it,
    /// single-line ones included, because which of them breaks is a translation's
    /// choice rather than this method's: cutting one sentence into a string per
    /// printed line forces every language to break where English happens to, and
    /// the two entries carrying a break here were the worst column-budget
    /// overflows of the sixteen languages for as long as they were shaped that way.
    /// </remarks>
    private static void WriteHelp(string value)
    {
        foreach (var line in value.Split('\n'))
            Console.WriteLine(line);
    }

    /// <summary>
    /// An <see cref="IProgress{T}"/> that runs its handler inline on the
    /// thread that calls <see cref="Report"/>. <see cref="Progress{T}"/>
    /// instead posts each report to the captured
    /// <see cref="System.Threading.SynchronizationContext"/>, or to the
    /// thread pool when there is none; a console host has none, so its
    /// reports would arrive unordered relative to each other and to the
    /// summary line printed after the awaited operation. The CLI needs
    /// ordered progress on a single stdout stream; the GUI keeps
    /// <see cref="Progress{T}"/> because it wants the dispatcher marshal.
    /// </summary>
    private sealed class SynchronousProgress<T>(Action<T> handler) : IProgress<T>
    {
        public void Report(T value) => handler(value);
    }
}
