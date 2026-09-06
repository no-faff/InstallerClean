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
        // A throw from Run outside the work loop (a name or ACL clash constructing
        // the single-instance mutex, say, or an unwritable stdout in the cleanup
        // finally) would otherwise reach the runtime's default handler:
        // ex.ToString() to stderr (a cross-profile path leak under elevation), no
        // Application-log record, and an undocumented exit code no RMM can branch
        // on. Route any such throw through the same crash-log + audit + ExitError
        // path the work loop uses. Run holds the single-instance mutex on this
        // thread (acquire and release both here, per the Win32 owner-thread rule)
        // and RunWorkAsync owns its own catch-all, so the work itself never lands
        // here: only Run's pre-flight and its cleanup can.
        //
        // The code page is set and restored around all of that, the catch-all
        // included, so the crash line still goes out in UTF-8 and the console is
        // handed back last.
        var previousOutputEncoding = TrySetUtf8Output();
        try
        {
            return Run(args);
        }
        catch (Exception ex)
        {
            return ReportUnexpectedError(args.Length > 0 ? args[0].ToLowerInvariant() : "(none)", ex);
        }
        finally
        {
            RestoreOutputEncoding(previousOutputEncoding);
        }
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
            // The console went away under us (the parent shell closed). Nothing
            // left to restore it to, and nothing left to tell.
        }
    }

    private static int Run(string[] args)
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
            case CliCommand.Help:
                PrintUsage();
                return ExitOk;
            case CliCommand.Version:
                PrintVersion();
                return ExitOk;
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
                var takesAPath = args[0].Equals("/m", StringComparison.OrdinalIgnoreCase);
                return ReportBadArguments(invocation, string.Format(
                    takesAPath ? Strings.Cli_TooManyArguments : Strings.Cli_TooManyArgumentsNoPath,
                    invocation.OffendingArgument));
            }
        }

        // Only the work commands (/s, /d, /m) remain. The lower-cased flag
        // drives the EventLog mode label and the /d-or-/m mutex check.
        var arg = args[0].ToLowerInvariant();

        // Cancel handler before mutex: a Ctrl+C in the gap should
        // print "Cancelling..." rather than terminate via the default
        // handler.
        using var cts = new CancellationTokenSource();
        ConsoleCancelEventHandler cancelHandler = (_, cancelArgs) =>
        {
            cancelArgs.Cancel = true; // keep the process running long enough to stop gracefully
            // Nothing in here may throw. It runs on the console's own
            // control-handler thread, which Main's try/catch does not cover, so an
            // escape is unhandled: a runtime abort and a stack trace on stderr in
            // place of the documented exit code and the single Application-log
            // entry, on a surface that deliberately keeps ex.Message out. Cancel
            // raises ObjectDisposedException once the run has disposed the source,
            // which it can do mid-invocation, unsubscribing a multicast delegate
            // not waiting for a handler already running. IsCancellationRequested
            // stays readable after Dispose, so the guard below is not the hazard.
            try
            {
                // A second Ctrl+C arriving while the first is still unwinding must
                // not print the notice again. CancellationTokenSource.Cancel is
                // already idempotent, so this guard earns its place for the stdout
                // line alone: the CLI's output is a scraped surface, and one run
                // reports its cancellation once.
                if (cts.IsCancellationRequested) return;
                Console.WriteLine();
                Console.WriteLine(Strings.Cli_Cancelling);
                cts.Cancel();
            }
            catch (Exception ex)
            {
                Helpers.CrashLog.TryWrite(ex);
            }
        };
        Console.CancelKeyPress += cancelHandler;

        // Mutate-the-cache operations (/d and /m) take the same singleton
        // mutex the WPF GUI uses, so a CLI invocation cannot race the
        // GUI mid-delete or mid-move (and a second concurrent CLI run
        // is also rejected). /s is read-only and runs unconditionally.
        System.Threading.Mutex? mutex = null;
        var holdsMutex = false;
        if (arg is "/d" or "/m")
        {
            mutex = new System.Threading.Mutex(initiallyOwned: false, @"Global\InstallerClean_SingleInstance");
            try
            {
                holdsMutex = mutex.WaitOne(TimeSpan.Zero);
            }
            catch (AbandonedMutexException)
            {
                // Previous owner crashed without releasing; the runtime
                // transfers ownership to this thread.
                holdsMutex = true;
            }
            if (!holdsMutex)
            {
                // Nothing on this path may reach Main's catch-all, and the two
                // halves of it fail differently. A throw before the write costs
                // the run its class: a 2000 TransientSkip and exit 75, the one
                // code a scheduler retries on, become a 4000 HardError and exit
                // 1. A throw after it produces that 4000 as this run's SECOND
                // entry, which is what an RMM counts runs by. So every statement
                // below is guarded where it stands, and the write is guarded
                // inside EventLogWriter.Write, which builds the entry there so
                // that no part of the call is evaluated in this frame.
                try
                {
                    Console.WriteLine(Strings.Cli_MutexBlocked);
                }
                catch (Exception ex)
                {
                    Helpers.CrashLog.TryWrite(ex);
                }
                // RMM consumer polling the Application channel for
                // InstallerClean entries needs an audit record on the
                // skipped path to distinguish it from "the task never
                // fired".
                MachineContract.WriteEventLog(CliEventClass.TransientSkip,
                    () => string.Format(Strings.Cli_EventLogMutexBlocked, arg));
                NoteEventLogUnavailable();
                try
                {
                    mutex.Dispose();
                    Console.CancelKeyPress -= cancelHandler;
                }
                catch (Exception ex)
                {
                    Helpers.CrashLog.TryWrite(ex);
                }
                return ExitTransient;
            }
        }

        try
        {
            // Sync-over-async wrapper so the acquired mutex (held on the
            // Main thread per its Win32 owner-thread rule) is released
            // by the same thread. Without this, the first await inside
            // RunWorkAsync would hop the continuation onto a thread-pool
            // thread and the finally's ReleaseMutex would throw
            // ApplicationException, orphaning the mutex until process
            // exit and forcing the next instance through the
            // AbandonedMutexException path. A console Main has no
            // SynchronizationContext, so GetResult here cannot deadlock
            // on captured-context resumption.
            return RunWorkAsync(arg, invocation, cts.Token).GetAwaiter().GetResult();
        }
        finally
        {
            // RunWorkAsync has already written this run's summary entry by the
            // time it returns, so nothing in this cleanup may reach Main's
            // catch-all: that writes a second, and one summary per run is what
            // an RMM counts runs by. The stdout note carries a guard of its
            // own so that a dead stream cannot skip the mutex release beneath it,
            // and the unhook goes last inside that guard because nothing depends
            // on it having happened.
            NoteEventLogUnavailable();
            try
            {
                if (holdsMutex) mutex!.ReleaseMutex();
                mutex?.Dispose();
                Console.CancelKeyPress -= cancelHandler;
            }
            catch (Exception ex)
            {
                Helpers.CrashLog.TryWrite(ex);
            }
        }
    }

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
    /// fact about how the check is built rather than about the file. They were
    /// reported separately until the four cause-specific sentences became one, at
    /// which point two printings stopped reading as two findings and started
    /// reading as a repeat. ADD THE TALLIES AND CALL THIS ONCE; a second call in
    /// one run is the fault rather than the wording.
    ///
    /// Takes the tally and not the path list, though every caller holds both. The
    /// tally already answers how many files there are, so a second argument here
    /// would be one that has to agree with it and could stop doing so. A run that
    /// held nothing back prints nothing, which is the commonest run by far and the
    /// reason a count of zero must never reach a sentence.
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
    /// Prints the one stdout audit line saying the Application channel was
    /// unwritable, so an RMM consumer polling for entries that never arrived has
    /// a record of why. Guarded, and every caller relies on that: each has
    /// already written this run's Application-log entry, and a throw from a dead
    /// stdout would reach <see cref="Main"/>'s catch-all and produce a second.
    /// </summary>
    private static void NoteEventLogUnavailable()
    {
        if (!EventLogWriter.EventLogUnavailable) return;
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
    internal static async Task<int> RunWorkAsync(
        string arg, CliInvocation invocation, CancellationToken token,
        IServiceProvider? servicesOverride = null)
    {
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
            var scanResult = await scanService.ScanAsync(cancellationToken: token);

            var count = scanResult.RemovableFiles.Count;
            var totalBytes = scanResult.RemovableFiles.Sum(f => f.SizeBytes);
            var size = DisplayHelpers.FormatSize(totalBytes);
            // A clean machine gets one line, not a count of zero and a size of
            // zero followed by a second line saying the same thing. It is the
            // commonest output this tool produces, and somebody reading a
            // scheduled task's log wants the state of the machine rather than
            // the tool's intention towards it.
            // THREE OUTCOMES WHERE NOTHING IS OFFERED, AND WHICH ONE IS
            // ScanResult.Withholding's ANSWER RATHER THAN THIS HOST'S. An empty offer
            // has three meanings: the folder holds nothing this scan can offer; a rule
            // about the machine's records emptied the walk-derived offer in one go; or
            // the scan judged the files one at a time and could not clear them. "Found
            // no unneeded files" is a statement about the folder and only the first
            // machine has earned it, and the two withholding sentences say different
            // things that are each false of the other's machine.
            //
            // THE HOST DOES NOT PARTITION ANYTHING TO GET HERE. Deciding it here would
            // mean reading a split the scan owns, and a host that infers one decision's
            // outcome from figures the others also write to means something different
            // the moment any of them moves. The scan answers it where the withholding
            // happens; this switch spends the answer.
            //
            // The count and the size are the whole withheld set rather than any one
            // condition's share of it, exactly as the window's screen uses, so the two
            // hosts cannot disagree about one machine.
            var withheldCount = scanResult.WithheldFiles?.Count ?? 0;

            // The one-form names the size and not the numeral ("the one file"), so it
            // spends {2} and leaves {0} and {1} unused; all three are passed on every
            // branch so the forms cannot disagree about which index is which. The
            // window's own body is built the same way.
            string HeldBackLine(string singular, string plural, string keyPrefix) =>
                string.Format(
                    DisplayHelpers.Pluralise(withheldCount, singular, plural, keyPrefix),
                    DisplayHelpers.FormatCount(withheldCount),
                    DisplayHelpers.PluraliseFile(withheldCount),
                    DisplayHelpers.FormatSize(scanResult.WithheldTotalBytes));

            Console.WriteLine(count > 0
                ? string.Format(
                    DisplayHelpers.Pluralise(count, Strings.Cli_FoundOrphans, "Cli.FoundOrphans"),
                    DisplayHelpers.FormatCount(count), DisplayHelpers.PluraliseFile(count), size)
                : scanResult.Withholding switch
                {
                    WithholdingAccount.WholeWalkOffer => HeldBackLine(
                        Strings.Cli_NothingOffered_Singular,
                        Strings.Cli_NothingOffered_Plural, "Cli.NothingOffered"),
                    WithholdingAccount.PerFile => HeldBackLine(
                        Strings.Cli_NothingOfferedPerFile_Singular,
                        Strings.Cli_NothingOfferedPerFile_Plural, "Cli.NothingOfferedPerFile"),
                    _ => Strings.Cli_FoundNoOrphans,
                });

            ReportScanSignals(arg, scanResult);

            if (count == 0)
            {
                // The audit line follows the same split as stdout, so a monitoring
                // tool watching the Application channel is not told a machine is
                // clean when the scan could not judge it.
                //
                // ONE EVENT CLASS FOR BOTH, WHICH IS A LIMIT AND NOT AN OVERSIGHT.
                // The run did its job either way, so both belong in the outcome
                // band, and telling the two apart means reading the message rather
                // than filtering on the number. Splitting them would put a new Event
                // ID on the wire, which is a change to the machine contract and a
                // decision of its own; the alternative, leaving the clean line to
                // cover both, is the false statement this branch exists to stop.
                MachineContract.WriteEventLog(CliEventClass.Ok,
                    () => scanResult.Withholding switch
                    {
                        WithholdingAccount.WholeWalkOffer => string.Format(
                            Strings.Cli_EventLogNothingOffered,
                            arg, withheldCount, DisplayHelpers.PluraliseFile(withheldCount)),
                        WithholdingAccount.PerFile => string.Format(
                            Strings.Cli_EventLogNothingOfferedPerFile,
                            arg, withheldCount, DisplayHelpers.PluraliseFile(withheldCount)),
                        _ => string.Format(Strings.Cli_EventLogScanNoOrphans,
                            arg, scanResult.RegisteredPackages.Count,
                            DisplayHelpers.PluralisePackage(scanResult.RegisteredPackages.Count)),
                    });
                return ExitOk;
            }

            if (arg == "/s")
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
                Console.WriteLine(string.Join(Environment.NewLine,
                    scanResult.RemovableFiles.Select(f =>
                        $"  {f.FileName.PadRight(nameColumn)}  ({f.SizeDisplay}, {f.Reason})")));
                // The noun and size are recomputed inside the en-GB scope rather
                // than reusing the human-facing `size` (which is in the OS
                // region), so this audit line reads fully English.
                MachineContract.WriteEventLog(CliEventClass.Ok,
                    () => string.Format(Strings.Cli_EventLogScanFound,
                        arg, count, DisplayHelpers.PluraliseFile(count),
                        DisplayHelpers.FormatSize(totalBytes)));
                return ExitOk;
            }

            // /s reads only, so it skips the gate.
            if (arg is "/d" or "/m")
            {
                var rebootService = services.GetRequiredService<IPendingRebootService>();
                var rebootCheck = rebootService.Check();
                if (rebootCheck.IsBlocked)
                    // Block + null Reason is unreachable per the PendingRebootResult.Block
                    // factory contract; .Value is safe inside this IsBlocked branch.
                    return EmitPendingRebootBlocked(arg, rebootCheck.Reason!.Value, rebootCheck.Detail);
            }

            // Re-verify the removable set against the Windows Installer API
            // immediately before acting. This is the one window neither the
            // pending-reboot gate above nor the Global\_MSIExecute hold the action
            // services take can see: a patch whose state changed AND settled
            // between the scan and now (a superseded patch reverted to
            // Applied because its superseding patch was uninstalled). It runs inside
            // this try, so a re-verify that throws (the enumeration can fail, e.g.
            // LocalisedAccessException) stops the batch through the existing error
            // path rather than acting on an un-verified set, and a cancellation
            // routes to the OCE catch. It runs synchronously before the batch; the
            // CLI blocks on it naturally.
            var reverifier = services.GetRequiredService<IRemovableReverifier>();
            var reverify = await reverifier.ReverifyAsync(
                scanResult.RemovableFiles.Select(f => f.FullPath).ToList(), token);
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
            // overlay reads it. Printing here as well put two sentences on stdout
            // for one batch, and since the four cause-specific sentences became one
            // they are the SAME sentence, so a run meeting both read as a repeat
            // with nothing to tell the two numbers apart.
            //
            // THE PATHS THAT NOW PRINT NOTHING ARE THE ONES THE WINDOW IS ALSO
            // SILENT ON, which is the point rather than a loss: an installer-busy
            // refusal, an unavailable lock and a free-space refusal all return
            // before the print, and none of them commits anything, so there is no
            // completed-of-intended count for the sentence to sit beside. The
            // window reaches no completion screen on any of the three either.
            // A WHOLE-BATCH REFUSAL STOOD HERE UNTIL 3.0.0 and its shape is worth
            // keeping in mind rather than rediscovering. It fired when the machine
            // gained a product installed as a second instance of itself between the
            // scan and the click, which is a fact about the machine and not about any
            // file, so it printed one sentence, wrote a HardError audit line and
            // returned ExitError rather than dropping files from the batch with
            // per-file causes none of them had earned. HardError and not
            // TransientSkip, because the condition does not clear on its own and a
            // retry-on-transient policy would have come back nightly to be refused
            // every time. It went with the identity check that detected it. No exit
            // code changed: ExitError is still 1 and every other route to it stands.
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
            // Guarded like every other console write in this host, and with a
            // sharper reason than the rest: the action services report from inside
            // their per-file try, so a throw out of this handler is filed as a
            // per-file error against a file that is perfectly fine. A dead stdout
            // is a fact about the console and never about the file being deleted.
            //
            // One crash-log entry per run rather than one per file. Whatever stops
            // stdout accepting a line stops it for every remaining report, so an
            // entry each would spend crash.log's rotation budget on copies of a
            // cause already recorded and evict the history behind them, which is
            // the failure PerItemFailureLog exists to prevent on the service side
            // of the same batch.
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

            if (arg == "/d")
            {
                var deleteService = services.GetRequiredService<IDeleteFilesService>();
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
                var result = filePaths.Count == 0
                    ? new DeleteResult(0, Array.Empty<FileOperationError>())
                    : await deleteService.DeleteFilesAsync(
                        filePaths, UnderLeaseClaims.From(reverify),
                        progress: progress, cancellationToken: token);

                // A Windows Installer transaction grabbed Global\_MSIExecute in the
                // race after the gate check passed, so the service refused and
                // touched nothing. Report it identically to a pre-act gate block.
                if (result.InstallerBusy)
                    return EmitPendingRebootBlocked(arg, PendingRebootReason.MsiExecuteMutexHeld, null);

                // The service could not take Global\_MSIExecute and nothing was
                // shown to be holding it, so it refused and touched nothing.
                if (result.InstallerLockUnavailable)
                    return EmitInstallerLockUnavailable(arg);
                if (result.InstallerLockAccessRefused)
                    return EmitInstallerLockAccessRefused(arg);

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
                // order: that re-entry leaves this method, so a run that held
                // files back and was then cancelled used to say nothing at all
                // about them, where the window reports them on both paths.
                heldBack += result.HeldBackReasons;
                ReportHeldBack(heldBack);
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
                    token.ThrowIfCancellationRequested();
                }

                // Bytes-recovered figure excludes the per-file error
                // list. Reporting the scan total on a partial failure
                // would overstate the freed-space figure for every run
                // that didn't process every file.
                long actualBytes = result.Errors.Count == 0
                    ? totalBytes
                    : SumBytesExcludingErrors(survivingFiles, result.Errors);
                var outcome = CliContract.ClassifyFileOperation(result.DeletedCount, result.Errors.Count);
                // Size and nouns are recomputed inside the en-GB scope, not
                // reused from the stdout copies, so the audit line reads fully
                // English ("3.2 GB", "files") on a localised machine.
                MachineContract.WriteEventLog(outcome.EventClass,
                    () => string.Format(Strings.Cli_EventLogDeleteSummary,
                        arg, result.DeletedCount, count, DisplayHelpers.PluraliseFile(count),
                        DisplayHelpers.FormatSize(actualBytes), result.Errors.Count,
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
                        arg, moveDest, DisplayHelpers.FormatSize(freeAtDestination),
                        DisplayHelpers.FormatSize(totalBytes)));
                return ExitError;
            }

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
            try
            {
                moveResult = filePaths.Count == 0
                    ? new MoveResult(0, Array.Empty<FileOperationError>())
                    : await moveService.MoveFilesAsync(filePaths, moveDest,
                        UnderLeaseClaims.From(reverify), progress, token);
            }
            catch (MoveAbortedException ex)
            {
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
                if (ex.Partial.HeldBack.Count > 0)
                {
                    survivingFiles = FoldHeldBack(survivingFiles, ex.Partial.HeldBack);
                    count = survivingFiles.Count;
                }
                return ReportAbortedMove(arg, ex, moveDest, count, survivingFiles);
            }

            // Global\_MSIExecute found held at the service boundary: same outcome
            // as a gate block.
            if (moveResult.InstallerBusy)
                return EmitPendingRebootBlocked(arg, PendingRebootReason.MsiExecuteMutexHeld, null);

            // The service could not take Global\_MSIExecute and nothing was shown
            // to be holding it, so it refused and touched nothing, its own
            // destination folder included.
            if (moveResult.InstallerLockUnavailable)
                return EmitInstallerLockUnavailable(arg);
            if (moveResult.InstallerLockAccessRefused)
                return EmitInstallerLockAccessRefused(arg);

            // The run's one held-back line. See the /d branch for why the two
            // producers' tallies are added and printed here rather than one each,
            // and for why this comes ahead of the cancel re-entry.
            heldBack += moveResult.HeldBackReasons;
            ReportHeldBack(heldBack);
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
            // script scraping the "\d+ errors:" shape finds it where it has always
            // been: this line is new and the block above it is not.
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
                token.ThrowIfCancellationRequested();
            }

            // Same per-file error exclusion as the /d branch.
            long actualMovedBytes = moveResult.Errors.Count == 0
                ? totalBytes
                : SumBytesExcludingErrors(survivingFiles, moveResult.Errors);
            var moveOutcome = CliContract.ClassifyFileOperation(moveResult.MovedCount, moveResult.Errors.Count);
            // Size and nouns recomputed inside the en-GB scope; see the /d
            // summary above for why the stdout copies are not reused.
            MachineContract.WriteEventLog(moveOutcome.EventClass,
                () => string.Format(Strings.Cli_EventLogMoveSummary,
                    arg, moveResult.MovedCount, count, DisplayHelpers.PluraliseFile(count),
                    moveDest, DisplayHelpers.FormatSize(actualMovedBytes), moveResult.Errors.Count,
                    DisplayHelpers.PluraliseError(moveResult.Errors.Count)));
            return moveOutcome.ExitCode;
        }
        catch (OperationCanceledException)
        {
            Console.WriteLine(Strings.Cli_Cancelled);
            // The undo, after the cancellation and not before it: the reader is told
            // the run stopped and then how to put back what it moved, which is the
            // order the window's card reads in. Empty unless a cancelled Move left
            // files in the destination, so a Delete and a Move that reached no file
            // print nothing here.
            if (cancelledMoveDestination.Length > 0)
                Console.WriteLine(string.Format(
                    Strings.Cli_MoveCancelledRestoreHint, cancelledMoveDestination));
            // EventLog the cancellation so a Task Scheduler audit can
            // see how far the run got, and pick ExitPartial when work
            // committed before the cancellation arrived.
            if (committedCount > 0)
            {
                MachineContract.WriteEventLog(CliEventClass.Partial,
                    () => string.Format(Strings.Cli_EventLogCancelledPartial,
                        arg, committedCount, totalToProcess,
                        DisplayHelpers.PluraliseFile(totalToProcess)));
                return ExitPartial;
            }
            // Cancelled before any file was processed (a cancel during the
            // scan, or before the first delete/move). Still write one entry
            // so "each /s, /d or /m run writes one summary" holds for every
            // run. TransientSkip, not Partial: nothing committed and a re-run
            // can succeed.
            MachineContract.WriteEventLog(CliEventClass.TransientSkip,
                () => string.Format(Strings.Cli_EventLogCancelledNoWork, arg));
            return ExitCancelled;
        }
        catch (LocalisedAccessException ex)
        {
            // LocalisedAccessException is the contract: services that
            // raise it have built the Message from a resx string with
            // user-controlled template args only, so echoing under
            // elevation is safe and distinguishes "MSI enumerator
            // access denied" from "cannot write the destination
            // folder". BCL-raised UnauthorizedAccessException from
            // deep in the framework can carry cross-profile paths and
            // falls through to the generic catch below.
            Console.WriteLine(ex.Message);
            // The template is forced English (the RMM grep anchor "{0} mode
            // failed:"); ex.Message is a LocalisedAccessException/
            // LocalisedInvalidOperationException message, already built in the
            // OS language and safe to echo, so the reason rides into the audit
            // line localised (the same sentence printed to stdout above).
            MachineContract.WriteEventLog(CliEventClass.HardError,
                () => string.Format(Strings.Cli_EventLogValidationFailed, arg, ex.Message));
            return ExitError;
        }
        catch (LocalisedInvalidOperationException ex)
        {
            // Same safe-to-echo contract as LocalisedAccessException.
            Console.WriteLine(ex.Message);
            // The template is forced English (the RMM grep anchor "{0} mode
            // failed:"); ex.Message is a LocalisedAccessException/
            // LocalisedInvalidOperationException message, already built in the
            // OS language and safe to echo, so the reason rides into the audit
            // line localised (the same sentence printed to stdout above).
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
    /// Reports the scan-level conditions that are facts about the machine rather
    /// than about this run: an offer withheld and the conditions behind it,
    /// superseded files kept back, records the scan could not fully read, and
    /// registrations naming a file that is not there.
    ///
    /// THE SURFACE IS PER CONDITION AND NOT ONE RULE OVER ALL OF THEM. Some reach
    /// stdout in the operator's language and the Application log in English,
    /// because scheduled tasks discard the first and RMM tools read the second.
    /// Others reach one of the two alone, each for the reason the comment at that
    /// condition gives.
    /// </summary>
    /// <remarks>
    /// Called once, immediately after the scan, so every return the work loop can
    /// take from there on has reported these first, the nothing-to-do one
    /// included: a fleet carrying either condition every night for a month
    /// otherwise looks, on the only surface anybody watches, exactly like a fleet
    /// with nothing to clean. The four returns in
    /// <see cref="ResolveAndValidateMoveDestination"/> come before the scan, so
    /// there is nothing to report by the time they take it.
    /// </remarks>
    private static void ReportScanSignals(string arg, ScanResult scanResult)
    {
        // THE WALK OFFER WAS EMPTIED WHOLESALE. An audit line for every machine that
        // meets it, and on stdout a lead, a header and one line per condition the run
        // met. The lead is the one part that is gated, on whether anything was offered
        // beside the withheld half, and the comment at that line says what decides it.
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
        if (scanResult.Withholding != WithholdingAccount.Nothing)
        {
            // WHICH OF THE TWO SENTENCES, ASKED ONCE AND SPENT THREE TIMES BELOW, so
            // the audit line, the lead and the breakdown cannot describe one machine
            // three different ways.
            var perFile = scanResult.Withholding == WithholdingAccount.PerFile;
            var heldBack = scanResult.WithheldFiles?.Count ?? 0;

            MachineContract.WriteEventLog(CliEventClass.ScanNothingOfferedNotice,
                () => string.Format(
                    perFile
                        ? Strings.Cli_EventLogNothingOfferedPerFileNotice
                        : Strings.Cli_EventLogNothingOfferedNotice,
                    arg, heldBack, DisplayHelpers.PluraliseFile(heldBack)));

            // THE LEAD, FOR THE MACHINE THAT OTHERWISE HEARS NOTHING. Where the offer
            // is empty the branch above this method has already said this in the line
            // it printed instead of the clean one. Where something WAS offered beside
            // the withheld half, that branch printed "Found N unneeded files" and said
            // nothing at all about the half that went; this is that machine's only
            // statement of it, and the window has had a line for exactly it.
            if (scanResult.RemovableFiles.Count > 0)
                Console.WriteLine(string.Format(
                    DisplayHelpers.Pluralise(heldBack,
                        perFile
                            ? Strings.Cli_NothingListedPerFile_Singular
                            : Strings.Cli_NothingListed_Singular,
                        perFile
                            ? Strings.Cli_NothingListedPerFile_Plural
                            : Strings.Cli_NothingListed_Plural,
                        perFile ? "Cli.NothingListedPerFile" : "Cli.NothingListed"),
                    DisplayHelpers.FormatCount(heldBack), DisplayHelpers.PluraliseFile(heldBack),
                    DisplayHelpers.FormatSize(scanResult.WithheldTotalBytes)));

            // AND WHY, ONE LINE PER CONDITION THE RUN MET, FROM BOTH HALVES OF THE
            // WITHHOLDING. The legs are read off the result, which calls the same
            // expression the scan gated on; the arms are read off the split the scan
            // filled as it withheld. So neither half can name a different set of
            // conditions from the one that acted, and a run that withheld both ways
            // gets both sets rather than whichever the surface happened to ask for.
            // No line carries a count and none states a cause for any file: these are
            // conditions, any combination of them can hold, and nothing sums.
            //
            // THE HEADING GOES WITH THE LINES OR NOT AT ALL. A caption reading "Why it
            // couldn't be certain:" over nothing reads as output that failed rather
            // than as a run with nothing to add, so it is printed from the list rather
            // than beside it.
            var reasons = scanResult.WithholdingLegsFired.Select(leg => LineFor(leg))
                .Concat(scanResult.WithheldBy.ArmsFired.Select(arm => LineFor(arm)))
                .ToList();

            if (reasons.Count > 0)
            {
                Console.WriteLine(Strings.Cli_WithheldReasons_Header);
                foreach (var line in reasons)
                    Console.WriteLine(line);
            }
        }

        // SUPERSEDED FILES HELD BACK. Word for word the sentence the window prints,
        // which it did not used to be: the window's old version closed on Re-scan and
        // this surface has no such button, and the clause that would still have forced
        // them apart came off both. It names no cause, six separate findings reaching
        // this count and no sentence naming one of them being true of the files the
        // other five contribute; the string's own remarks carry that and why
        // "superseded" is earned.
        if (scanResult.WithheldCount > 0)
            Console.WriteLine(string.Format(
                DisplayHelpers.Pluralise(scanResult.WithheldCount,
                    Strings.Cli_SupersededHeldBack_Singular,
                    Strings.Cli_SupersededHeldBack_Plural,
                    "Cli.SupersededHeldBack"),
                DisplayHelpers.FormatCount(scanResult.WithheldCount)));

        // AND THE NOTICE IS ITS OWN CONDITION NOW, WHICH IS THE WHOLE POINT OF THE
        // SPLIT. The two sat in one branch for as long as the human line was gated on
        // this figure. That line has moved onto the count of files held back and this
        // has not moved at all, deliberately: Event ID 3000 is a machine surface with
        // an RMM filter downstream and this figure is its payload, so re-gating it
        // would have changed which machines log it with every test still green. A
        // measurement that goes quiet reads exactly like nothing being wrong.
        //
        // THEY ARE NOT TWO VIEWS OF ONE QUANTITY. This counts installed products the
        // enumeration could not account for, which is the trigger for ONE of the six
        // routes into the count above. A machine can meet either condition without the other,
        // and the commonest is meeting this one with no superseded file to hold back.
        //
        // The count does not appear in the human line and does appear here. Four
        // different things contribute to it and only two are failures to read, so it
        // is an estimate that can come out high as well as low: a precision a sentence
        // must not claim, and a number an RMM needs to hang a filter on. See
        // MachineContract for what that figure is worth.
        if (scanResult.UnaccountedProductCount > 0)
            MachineContract.WriteEventLog(CliEventClass.ScanRecordsIncompleteNotice,
                () => string.Format(Strings.Cli_EventLogScanWithheld,
                    arg, scanResult.UnaccountedProductCount));

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
            Console.WriteLine(string.Format(
                DisplayHelpers.Pluralise(scanResult.MissingAffectedCount,
                    Strings.Cli_MissingFromDisk_Singular,
                    Strings.Cli_MissingFromDisk_Plural,
                    "Cli.MissingFromDisk"),
                DisplayHelpers.FormatCount(scanResult.MissingAffectedCount), programs));
            MachineContract.WriteEventLog(CliEventClass.ScanMissingFilesNotice,
                () => string.Format(Strings.Cli_EventLogMissingFromDisk,
                    arg, scanResult.MissingAffectedCount));
        }
    }

    /// <summary>
    /// Prints a bad-invocation message and usage, writes the one shared
    /// Application-log entry, and returns <see cref="ExitError"/>. The stdout
    /// <paramref name="stdoutMessage"/> differs between an unrecognised flag and a
    /// recognised flag with an extra token, but the audit entry
    /// (<c>Cli.EventLogBadArguments</c>) and the exit code are identical for both,
    /// so an RMM filter matching the Application channel sees one "bad arguments"
    /// contract whichever the user hit. The arg switch returns before the work
    /// loop's try/finally, so the event-log-unavailable note is emitted inline here.
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
    /// shared by <see cref="RunWorkAsync"/>'s catch-all and <see cref="Main"/>'s
    /// pre-flight guard so the two report on the same contract. Writes the full
    /// detail to crash.log, prints only the exception type name and the
    /// crash-log path (never <c>ex.Message</c>: under elevation it can carry a
    /// path out of another user's profile, and Task Scheduler / RMM tooling
    /// captures stdout to disk), records one HardError Application-log entry, and
    /// returns <see cref="ExitError"/> so a scheduled task sees a documented exit
    /// code rather than a runtime abort with an undocumented one. Safe to call
    /// after the console itself has failed (the stdout write is guarded, and
    /// <see cref="CrashLog.TryWrite"/> and <see cref="EventLogWriter"/> both
    /// swallow their own IO), which is the state a pre-flight console failure
    /// leaves behind.
    /// </summary>
    private static int ReportUnexpectedError(string mode, Exception ex)
    {
        var crash = Helpers.CrashLog.TryWrite(ex);
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
            // This runs inside Main's own catch clause, so anything it lets past
            // reaches the runtime default handler and produces the undocumented exit
            // the routing exists to prevent. Two things in the try can throw: the
            // write itself, and formatting a resx string. The exception that brought
            // us here is already in crash.log, and the audit entry below still fires.
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
            DisplayHelpers.FormatSize(CompletedBytes(survivingFiles, partial.MovedCount, partial.Errors)),
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
    /// <c>Global\_MSIExecute</c> would not let the app open it. Keyed off the flag
    /// exactly as <see cref="InstallerLockUnavailableLine"/> is, and separate from
    /// it because the two refusals are different facts about the machine: one says
    /// nothing could be shown to hold the lock, this one says the app was not
    /// allowed to look.
    /// </summary>
    internal static string InstallerLockAccessRefusedLine(string arg) =>
        arg == "/m" ? Strings.Cli_MoveInstallerLockAccessRefused : Strings.Cli_InstallerLockAccessRefused;

    /// <summary>
    /// The Application-channel line for that refusal. ONE line covers both flags
    /// and <c>{0}</c> names which one ran, on the same terms as
    /// <see cref="InstallerLockUnavailableEventLogLine"/>: an ending naming a
    /// single action is false of half the runs that can produce it.
    /// </summary>
    /// <remarks>
    /// Built outside the en-GB scope, like its sibling: the caller wraps it, so the
    /// line renders English in production and in the ambient culture anywhere else.
    /// </remarks>
    internal static string InstallerLockAccessRefusedEventLogLine(string arg) =>
        string.Format(Strings.Cli_EventLogInstallerLockAccessRefused, arg);

    /// <summary>
    /// Reports a <c>/d</c> or <c>/m</c> the action service refused for want of
    /// <c>Global\_MSIExecute</c>, and returns the exit code for it.
    /// </summary>
    /// <remarks>
    /// Deliberately NOT routed through <see cref="EmitPendingRebootBlocked"/>:
    /// every <see cref="PendingRebootReason"/> it can name asserts something is in
    /// progress, and the defining fact here is that nothing has been shown to be.
    /// TransientSkip and ExitTransient all the same, on that method's own
    /// reasoning: the condition can clear on its own, so a scheduler should come
    /// back rather than treat the machine as broken.
    ///
    /// The two lines it emits are separately reachable and this method is not, so
    /// that the wording can be held by tests without one of them writing to the
    /// Application channel. An entry forged by a test run is indistinguishable
    /// from one a real run wrote, on the channel whose whole contract is that a
    /// run leaves exactly one summary; the suite must not be able to add to
    /// somebody's audit trail.
    /// </remarks>
    private static int EmitInstallerLockUnavailable(string arg)
    {
        Console.WriteLine(InstallerLockUnavailableLine(arg));
        MachineContract.WriteEventLog(CliEventClass.TransientSkip,
            () => InstallerLockUnavailableEventLogLine(arg));
        return ExitTransient;
    }

    /// <summary>
    /// Reports a <c>/d</c> or <c>/m</c> the action service refused because the
    /// security on <c>Global\_MSIExecute</c> would not let it open the object, and
    /// returns the exit code for it.
    /// </summary>
    /// <remarks>
    /// The SAME exit code and event class as
    /// <see cref="EmitInstallerLockUnavailable"/>, and only the wording differs.
    /// What the two refusals have in common is a run that stopped before it
    /// touched anything, which is what the shared code carries to a scheduler;
    /// what the operator is told is which of the two happened, which is what they
    /// differ in. Neither line tells the operator whether to retry: the app was
    /// refused the one question that would have said, so it reports what happened
    /// and leaves that judgement to whoever reads it.
    ///
    /// Its two lines are separately reachable and this method is not, on the same
    /// terms as its sibling: the wording can be held by tests without one of them
    /// writing to the Application channel.
    /// </remarks>
    private static int EmitInstallerLockAccessRefused(string arg)
    {
        Console.WriteLine(InstallerLockAccessRefusedLine(arg));
        MachineContract.WriteEventLog(CliEventClass.TransientSkip,
            () => InstallerLockAccessRefusedEventLogLine(arg));
        return ExitTransient;
    }

    /// <summary>
    /// The line printed for one leg of the wholesale withholding.
    ///
    /// Its own method rather than a switch inside the report, so
    /// CliWithholdingReasonsTests can walk the enum against it: a leg added to
    /// WithholdingLeg is a leg the gate acts on and the scan withholds for, and a leg
    /// with no line here would leave a reader a heading with nothing under it.
    ///
    /// The fallback is the heading's own antecedent rather than a blank, because a
    /// heading followed by nothing reads as output that failed rather than as a
    /// condition nobody wrote up. It is unreachable while every leg is handled.
    /// </summary>
    internal static string LineFor(WithholdingLeg leg) => leg switch
    {
        WithholdingLeg.RecordedPathUnestablished => Strings.Cli_WithheldReasons_RecordedPath,
        WithholdingLeg.FileIdentityUnestablished => Strings.Cli_WithheldReasons_FileIdentity,
        WithholdingLeg.SecondInstanceNotRuledOut => Strings.Cli_WithheldReasons_SecondInstance,
        _ => Strings.Cli_WithheldReasons_Header,
    };

    /// <summary>
    /// The line printed for one arm of the per-file withholding.
    ///
    /// Its own method for the same reason the one above it is, so
    /// CliWithholdingReasonsTests can walk the enum against it: an arm added to
    /// WithholdingSplit is an arm the scan counts files into, and an arm with no line
    /// here would leave a reader a heading with one fewer reason under it.
    ///
    /// THE WHOLESALE ARM HAS NO MEMBER AND SO CANNOT REACH THIS. The three leg lines
    /// speak for it, and a line here as well would say the same thing twice about one
    /// machine.
    ///
    /// The fallback is the heading's own antecedent rather than a blank, on the same
    /// reasoning as above, and it is unreachable while every arm is handled.
    /// </summary>
    internal static string LineFor(WithholdingSplitArm arm) => arm switch
    {
        WithholdingSplitArm.IdentityUnestablished =>
            Strings.Cli_WithheldReasons_CandidateIdentity,
        WithholdingSplitArm.DeclaredProductInstalled =>
            Strings.Cli_WithheldReasons_DeclaredProductInstalled,
        WithholdingSplitArm.DeclaredProductUnestablished =>
            Strings.Cli_WithheldReasons_DeclaredProductUnestablished,
        WithholdingSplitArm.ScreenUnanswered =>
            Strings.Cli_WithheldReasons_ScreenUnanswered,
        _ => Strings.Cli_WithheldReasons_Header,
    };

    /// <summary>
    /// The sentence a blocked run prints, one per reason.
    ///
    /// Its own method rather than an expression inside the emitter because the emitter
    /// writes to the console and to the Application channel, so nothing could read this
    /// back without doing both. CliPendingRebootStringsTests walks the enum against it,
    /// which is what keeps the fallback below unreachable; the window's banner is held
    /// to the same standard by ScanViewModelPendingRebootTests and is testable already,
    /// being a bound property.
    /// </summary>
    internal static string PendingRebootBlockedMessage(PendingRebootReason reason, string? detail) =>
        reason switch
        {
            PendingRebootReason.MsiExecuteMutexHeld =>
                Strings.Cli_PendingRebootBlocked_MsiExecuteMutex,
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
            // What a reason with no line of its own gets. It threw before, which
            // landed in the generic catch and reported an unexpected crash with
            // exit 1, where a blocked run wants the 75 a scheduler retries on.
            // It cannot fire for a null reason either: PendingRebootResult.Block
            // takes a non-nullable one.
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
            PendingRebootReason.InstallerInProgress =>
                Strings.Cli_EventLogReason_InstallerInProgress,
            PendingRebootReason.PendingRenameInCache =>
                Strings.Cli_EventLogReason_PendingRenameInCache,
            PendingRebootReason.PendingRenameUnresolved =>
                Strings.Cli_EventLogReason_PendingRenameUnresolved,
            PendingRebootReason.RegistryCheckUnreadable =>
                Strings.Cli_EventLogReason_RegistryCheckUnreadable,
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
    /// what the line claims about the condition. Of the five, one is an installer
    /// running right now, one a suspended transaction, two are operations queued for
    /// the next restart, and one is a registry value the check could not read, so
    /// nothing shorter than the label is true of all five.
    ///
    /// The detail arrives carrying its own separator, which is why the template ends
    /// in a placeholder with no space in front of it: the four reasons that never
    /// carry a detail would otherwise each log a line with a space hanging off it.
    ///
    /// CALL IT FROM INSIDE THE EVENT-LOG SCOPE, for the reason
    /// <see cref="PendingRebootEventLogReason"/> gives: it reads through the ordinary
    /// door, so it answers in whatever culture is current when it runs.
    /// </summary>
    internal static string PendingRebootEventLogLine(string arg, PendingRebootReason reason, string? detail) =>
        string.Format(
            Strings.Cli_EventLogPendingRebootBlocked,
            arg,
            PendingRebootEventLogReason(reason),
            string.IsNullOrEmpty(detail) ? string.Empty : " " + detail);

    /// <summary>
    /// Emits the pending-reboot-blocked outcome: the localised stdout reason
    /// sentence, the English Application-log entry, and the exit code its reason
    /// takes.
    /// Shared by the pre-act gate check and the action services' own boundary
    /// refusal. When a Move or Delete service acquires <c>Global\_MSIExecute</c>
    /// and finds it held
    /// (<see cref="Models.MoveResult.InstallerBusy"/> /
    /// <see cref="Models.DeleteResult.InstallerBusy"/>), a Windows Installer
    /// transaction started in the sub-millisecond race after the gate check passed;
    /// mapping that to <see cref="PendingRebootReason.MsiExecuteMutexHeld"/> here
    /// makes the service-boundary refusal produce the identical machine contract
    /// (stdout line, event-log entry, exit code) a gate block does, so an RMM
    /// consumer cannot tell the two apart.
    /// </summary>
    private static int EmitPendingRebootBlocked(string arg, PendingRebootReason reason, string? detail)
    {
        Console.WriteLine(PendingRebootBlockedMessage(reason, detail));

        // WHAT A SCHEDULER IS TOLD, DECIDED FROM THE REASON AND TAKEN AS A PAIR.
        // The exit code and the entry class are one statement about this run said to
        // two audiences, so they are read out of one expression: a code meaning
        // "come back later" beside an entry classed as a run that failed outright
        // would leave the machine contract disagreeing with itself.
        //
        // THE FOUR THAT ASSERT SOMETHING IS IN FLIGHT TAKE THE TRANSIENT CODE. An
        // install holding the mutex, a suspended transaction, a queued rename: each
        // of those ends by itself, so a scheduler that comes back finds the machine
        // in a different state.
        //
        // A READ THE APP COULD NOT MAKE IS NOT ONE OF THEM, and this is the arm the
        // default falls to. Nothing about waiting says the value will read next
        // time, and the sentence this run prints tells the operator that if it still
        // will not read, this is not a machine InstallerClean can clean. A code
        // meaning "temporary, retry" under those words has a nightly task coming
        // back for ever. The code it takes instead claims only what is true of the
        // run: nothing was processed. A reason added to the enum later lands here
        // too, on the same reasoning, until somebody decides otherwise in
        // CliPendingRebootOutcomeTests' own table.
        var (exitCode, entryClass) = reason switch
        {
            PendingRebootReason.MsiExecuteMutexHeld or
            PendingRebootReason.InstallerInProgress or
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
            PendingRebootEventLogLine(arg, reason, detail));
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
    /// Its own method so the wording can be held by a test without the suite
    /// writing to the Application channel, which is what the lock-refusal and
    /// stopped-move lines are separated for: an entry a test run forged is
    /// indistinguishable from one a real run wrote, on a channel whose contract
    /// is that a run leaves exactly one summary.
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
