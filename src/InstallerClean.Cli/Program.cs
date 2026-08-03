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
                // inside EventLogWriter.Write, which builds the summary there so
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
            // RunWorkAsync has already written this run's one Application-log
            // entry by the time it returns, so nothing in this cleanup may reach
            // Main's catch-all: that writes a second, and one entry per run is
            // what an RMM counts runs by. The stdout note carries a guard of its
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
    /// Prints what an action service's under-lease re-read kept back, in the
    /// operator's language, using the same sentence the pre-act re-verify's own
    /// held-back line uses. One condition, one wording: a program claims the file
    /// again, and which side of the installer mutex that was read on is a fact
    /// about the check rather than about the file.
    ///
    /// Deliberately not a machine-read line, exactly as the pre-act one is not.
    /// The Application-channel summary already carries what the run did in
    /// English, and an RMM reads that.
    /// </summary>
    private static void ReportHeldBack(IReadOnlyList<string> heldBack, bool recordsIncomplete)
    {
        if (heldBack.Count == 0) return;
        // Which of the two sentences, decided the same way the pre-act line above
        // decides it: a re-read that could not read the records has kept files
        // back without any program having reclaimed them, and saying one where the
        // other is true names a cause that was never shown.
        var (flat, key) = recordsIncomplete
            ? (Strings.Completion_ReverifyIncomplete, "Completion.ReverifyIncomplete")
            : (Strings.Completion_ReverifySkipped, "Completion.ReverifySkipped");
        Console.WriteLine(string.Format(
            DisplayHelpers.Pluralise(heldBack.Count, flat, key),
            heldBack.Count, DisplayHelpers.PluraliseFile(heldBack.Count)));
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

    private static async Task<int> RunWorkAsync(string arg, CliInvocation invocation, CancellationToken token)
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

        try
        {
            using var services = new ServiceCollection()
                .AddInstallerCleanCore()
                .BuildServiceProvider(new ServiceProviderOptions
                {
                    ValidateScopes = true,
                    ValidateOnBuild = true,
                });

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
            Console.WriteLine(count == 0
                ? Strings.Cli_FoundNoOrphans
                : string.Format(
                    DisplayHelpers.Pluralise(count, Strings.Cli_FoundOrphans, "Cli.FoundOrphans"),
                    count, DisplayHelpers.PluraliseFile(count), size));

            ReportScanSignals(arg, scanResult);

            if (count == 0)
            {
                MachineContract.WriteEventLog(CliEventClass.Ok,
                    () => string.Format(Strings.Cli_EventLogScanNoOrphans,
                        arg, scanResult.RegisteredPackages.Count,
                        DisplayHelpers.PluralisePackage(scanResult.RegisteredPackages.Count)));
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

            // Anything the re-verify kept back is reported with its reason (human
            // stdout, OS language; deliberately NOT a machine-read line). A
            // re-verify that could not read the records keeps files back without
            // any program having reclaimed them, so the reason comes from the
            // re-verify rather than being assumed from the count. The counts and
            // byte figures below are recomputed from the survivor subset so
            // "X of Y" and the freed-space total describe what was acted on, not
            // the pre-reverify scan.
            if (reverify.Dropped.Count > 0)
            {
                var (flat, key) = reverify.RecordsIncomplete
                    ? (Strings.Completion_ReverifyIncomplete, "Completion.ReverifyIncomplete")
                    : (Strings.Completion_ReverifySkipped, "Completion.ReverifySkipped");
                Console.WriteLine(string.Format(
                    DisplayHelpers.Pluralise(reverify.Dropped.Count, flat, key),
                    reverify.Dropped.Count, DisplayHelpers.PluraliseFile(reverify.Dropped.Count)));
            }

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
                // Silent when the re-verify just above took every file back. The
                // line it printed has already said so, with the reason; announcing
                // a batch of none and then reporting that none of it happened
                // reads as a fault twice over on a run where nothing went wrong.
                if (count > 0)
                    Console.WriteLine(string.Format(
                        DisplayHelpers.Pluralise(count, Strings.Cli_DeletingFiles, "Cli.DeletingFiles"),
                        count, DisplayHelpers.PluraliseFile(count)));
                // Skip the service when the re-verify left nothing to act on:
                // DeleteFilesService returns 0/0 for an empty list anyway, but
                // synthesizing it keeps the /d and /m branches symmetric (Move
                // would otherwise create and probe its destination for an empty
                // batch). The summary path below still fires with 0, exit Ok, so the
                // one-entry-per-run event-log contract holds.
                var result = filePaths.Count == 0
                    ? new DeleteResult(0, Array.Empty<FileOperationError>())
                    : await deleteService.DeleteFilesAsync(
                        filePaths, progress: progress, cancellationToken: token,
                        patchClaims: reverify.SurvivingPatchClaims);

                // A Windows Installer transaction grabbed Global\_MSIExecute in the
                // race after the gate check passed, so the service refused and
                // touched nothing. Report it identically to a pre-act gate block.
                if (result.InstallerBusy)
                    return EmitPendingRebootBlocked(arg, PendingRebootReason.MsiExecuteMutexHeld, null);

                // The service could not take Global\_MSIExecute and nothing was
                // holding it, so it refused and touched nothing. Not routed through
                // EmitPendingRebootBlocked: every PendingRebootReason it can name
                // asserts something is in progress, and the defining fact here is
                // that nothing is. Transient and TransientSkip all the same, on the
                // same reasoning its sibling carries: the condition can clear on
                // its own, so a scheduler should come back rather than treat the
                // machine as broken.
                if (result.InstallerLockUnavailable)
                {
                    Console.WriteLine(Strings.Cli_InstallerLockUnavailable);
                    MachineContract.WriteEventLog(CliEventClass.TransientSkip,
                        () => string.Format(Strings.Cli_EventLogInstallerLockUnavailable, arg));
                    return ExitTransient;
                }

                // Reported after the batch rather than beside the pre-act
                // re-verify's own line above, because it happened after that line
                // was printed: the service takes the installer mutex and re-reads
                // the batch's patch claims inside it, and anything reclaimed in
                // between is kept back there. Same condition and therefore the
                // same sentence; what differs is only when it was read.
                //
                // Ahead of the "deleted N" line, so the run reads in the order it
                // happened: what was intended, what was kept back, what was done.
                // Ahead of the cancel re-entry below for a harder reason than
                // order: that re-entry leaves this method, so a run that held
                // files back and was then cancelled used to say nothing at all
                // about them, where the window reports them on both paths.
                ReportHeldBack(result.HeldBack, result.HeldBackRecordsIncomplete);
                // Held-back files were never touched, so they leave the tally the
                // same way the errors below do. Without this they would be counted
                // as freed bytes, the byte sum discounting errors alone.
                // totalToProcess moves with them: it is the "of N" the cancelled-run
                // audit line prints, and left at the pre-fold count it describes a
                // batch that included files the run never intended to reach.
                if (result.HeldBack.Count > 0)
                {
                    var reclaimed = new HashSet<string>(result.HeldBack, StringComparer.OrdinalIgnoreCase);
                    survivingFiles = survivingFiles.Where(f => !reclaimed.Contains(f.FullPath)).ToList();
                    count = survivingFiles.Count;
                    totalBytes = survivingFiles.Sum(f => f.SizeBytes);
                    totalToProcess = count;
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

                Console.WriteLine(string.Format(
                    DisplayHelpers.Pluralise(result.DeletedCount, Strings.Cli_DeletedFiles, "Cli.DeletedFiles"),
                    result.DeletedCount, DisplayHelpers.PluraliseFile(result.DeletedCount)));
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
                    count, DisplayHelpers.PluraliseFile(count), moveDest));
            // See the /d branch: skip the service (and MoveFilesService's
            // destination-folder create + probe) when nothing survived the
            // re-verify; synthesize the empty result so the summary path still fires
            // with 0 and exit Ok.
            MoveResult moveResult;
            try
            {
                moveResult = filePaths.Count == 0
                    ? new MoveResult(0, Array.Empty<FileOperationError>())
                    : await moveService.MoveFilesAsync(filePaths, moveDest, progress, token,
                        reverify.SurvivingPatchClaims);
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
                ReportHeldBack(ex.Partial.HeldBack, ex.Partial.HeldBackRecordsIncomplete);
                if (ex.Partial.HeldBack.Count > 0)
                {
                    var abortReclaimed = new HashSet<string>(ex.Partial.HeldBack, StringComparer.OrdinalIgnoreCase);
                    survivingFiles = survivingFiles.Where(f => !abortReclaimed.Contains(f.FullPath)).ToList();
                    count = survivingFiles.Count;
                }
                return ReportAbortedMove(arg, ex, moveDest, count, survivingFiles);
            }

            // Global\_MSIExecute found held at the service boundary: same outcome
            // as a gate block.
            if (moveResult.InstallerBusy)
                return EmitPendingRebootBlocked(arg, PendingRebootReason.MsiExecuteMutexHeld, null);

            // See the /d branch for why this is reported here and not beside the
            // pre-act re-verify's line, and for why it comes ahead of the cancel
            // re-entry rather than after it.
            ReportHeldBack(moveResult.HeldBack, moveResult.HeldBackRecordsIncomplete);
            if (moveResult.HeldBack.Count > 0)
            {
                var movedReclaimed = new HashSet<string>(moveResult.HeldBack, StringComparer.OrdinalIgnoreCase);
                survivingFiles = survivingFiles.Where(f => !movedReclaimed.Contains(f.FullPath)).ToList();
                count = survivingFiles.Count;
                totalBytes = survivingFiles.Sum(f => f.SizeBytes);
                totalToProcess = count;
            }

            // Partial result returned on a mid-batch cancel; re-enter the OCE catch
            // so the machine contract matches a thrown cancellation. See the /d
            // branch above.
            if (moveResult.Cancelled)
            {
                committedCount = moveResult.MovedCount;
                token.ThrowIfCancellationRequested();
            }

            Console.WriteLine(string.Format(
                DisplayHelpers.Pluralise(moveResult.MovedCount, Strings.Cli_MovedFiles, "Cli.MovedFiles"),
                moveResult.MovedCount, DisplayHelpers.PluraliseFile(moveResult.MovedCount)));
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
            // EventLog the cancellation so a Task Scheduler audit can
            // see how far the run got, and pick ExitPartial when work
            // committed before the Ctrl+C arrived.
            if (committedCount > 0)
            {
                MachineContract.WriteEventLog(CliEventClass.Partial,
                    () => string.Format(Strings.Cli_EventLogCancelledPartial,
                        arg, committedCount, totalToProcess,
                        DisplayHelpers.PluraliseFile(totalToProcess)));
                return ExitPartial;
            }
            // Cancelled before any file was processed (a Ctrl+C during the
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
    /// Reports the two scan-level conditions that are facts about the machine
    /// rather than about this run: a scan that had to withhold its superseded and
    /// obsoleted verdicts, and registered files missing from disk. Each goes to
    /// stdout in the operator's language and to the Application log in English,
    /// because scheduled tasks and RMM tools discard the first and read the second.
    /// </summary>
    /// <remarks>
    /// Called once, immediately after the scan, so every return the work loop can
    /// take from there on has reported these first, the nothing-to-do one included:
    /// a fleet whose superseded-patch cleanup has been withheld every night for a
    /// month otherwise looks, on the only surface anybody watches, exactly like a
    /// fleet with nothing to clean. The four returns in
    /// <see cref="ResolveAndValidateMoveDestination"/> come before the scan, so
    /// there is nothing to report by the time they take it.
    /// </remarks>
    private static void ReportScanSignals(string arg, ScanResult scanResult)
    {
        if (scanResult.UnreadableProductCount > 0)
        {
            // The command line's own pair, not the window's Summary.* one. That
            // closes on Re-scan, which is a button this surface has not got, and
            // opens its second sentence on "Everything listed", which is true
            // only of /s: a /d or an /m lists nothing at all.
            Console.WriteLine(string.Format(
                DisplayHelpers.Pluralise(scanResult.UnreadableProductCount,
                    Strings.Cli_ProgramsUnreadable_Singular,
                    Strings.Cli_ProgramsUnreadable_Plural,
                    "Cli.ProgramsUnreadable"),
                scanResult.UnreadableProductCount));
            MachineContract.WriteEventLog(CliEventClass.ScanWithheldNotice,
                () => string.Format(Strings.Cli_EventLogScanWithheld,
                    arg, scanResult.WithheldCount, scanResult.UnreadableProductCount));
        }

        // MissingNonRemovableCount, never MissingFromDiskCount: a superseded patch
        // whose file has already gone is the expected end state, not an alarm.
        if (scanResult.MissingNonRemovableCount > 0)
        {
            Console.WriteLine(string.Format(
                DisplayHelpers.Pluralise(scanResult.MissingNonRemovableCount,
                    Strings.Cli_MissingFromDisk_Singular,
                    Strings.Cli_MissingFromDisk_Plural,
                    "Cli.MissingFromDisk"),
                scanResult.MissingNonRemovableCount));
            MachineContract.WriteEventLog(CliEventClass.ScanMissingFilesNotice,
                () => string.Format(Strings.Cli_EventLogMissingFromDisk,
                    arg, scanResult.MissingNonRemovableCount));
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
        catch (IOException)
        {
            // stdout itself is unwritable, which is one of the failures that can
            // bring us here; crash.log and the audit entry below carry the record.
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
    /// Bytes of the files a batch that stopped part way actually moved. The
    /// action services take their input in order and stop where they stop, so the
    /// files they reached are the first (<paramref name="completedCount"/> plus
    /// the errors); of those, the ones that did not error are what moved.
    /// <see cref="SumBytesExcludingErrors"/> can lean on "no errors means every
    /// file completed" instead, which a batch that stopped cannot.
    /// Matches CleanupViewModel's own CompletedBytes so a fleet of GUI and CLI
    /// machines produces telemetry on the same axis.
    /// </summary>
    private static long CompletedBytes(
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
    /// The summary and the error block come first, in the shape an ordinary
    /// <c>/m</c> prints them, so a script's <c>\d+ errors:</c> scrape lands where
    /// it always does; the guard's reason follows.
    /// </remarks>
    private static int ReportAbortedMove(
        string arg, MoveAbortedException ex, string moveDest, int count,
        IReadOnlyList<OrphanedFile> survivingFiles)
    {
        var partial = ex.Partial;
        Console.WriteLine(string.Format(
            DisplayHelpers.Pluralise(partial.MovedCount, Strings.Cli_MovedFiles, "Cli.MovedFiles"),
            partial.MovedCount, DisplayHelpers.PluraliseFile(partial.MovedCount)));
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
        // about the same folder. Should a second guard ever raise it, this line
        // is what has to move with it.
        Console.WriteLine(string.Format(Strings.Cli_DestinationChangedMidBatch, moveDest));

        var outcome = CliContract.ClassifyAbortedMove(partial.MovedCount);
        // Sizes and nouns recomputed inside the en-GB scope; see the /d summary
        // for why the stdout copies are not reused.
        MachineContract.WriteEventLog(outcome.EventClass,
            () => string.Format(Strings.Cli_EventLogMoveAborted,
                arg, partial.MovedCount, count, DisplayHelpers.PluraliseFile(count), moveDest,
                DisplayHelpers.FormatSize(CompletedBytes(survivingFiles, partial.MovedCount, partial.Errors)),
                partial.Errors.Count, DisplayHelpers.PluraliseError(partial.Errors.Count)));
        return outcome.ExitCode;
    }

    /// <summary>
    /// Emits the pending-reboot-blocked outcome: the localised stdout reason
    /// sentence, the English Application-log entry, and <see cref="ExitTransient"/>.
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
        var stdoutMessage = reason switch
        {
            PendingRebootReason.MsiExecuteMutexHeld =>
                Strings.Cli_PendingRebootBlocked_MsiExecuteMutex,
            PendingRebootReason.InstallerInProgress =>
                Strings.Cli_PendingRebootBlocked_InstallerInProgress,
            PendingRebootReason.PendingRenameInCache =>
                string.Format(
                    Strings.Cli_PendingRebootBlocked_PendingRenameInCache,
                    detail ?? string.Empty),
            // What a reason with no line of its own gets. It threw before, which
            // landed in the generic catch and reported an unexpected crash with
            // exit 1, where a blocked run wants the 75 a scheduler retries on.
            // Unreachable while the enum has three members, all handled above, and
            // it cannot fire for a null reason either: PendingRebootResult.Block
            // takes a non-nullable one.
            _ => Strings.Cli_PendingRebootBlocked_Other,
        };
        Console.WriteLine(stdoutMessage);
        // The reason label and template are built English: the
        // Cli.EventLogReason.* labels ARE translated in the satellites, but the
        // Application channel is sysadmin-facing and an RMM grep on a known phrase
        // needs a stable English target. The localised stdout sentence above is
        // what the operator reads; the label switch lives inside the scope so it
        // resolves en-GB, not the OS language.
        MachineContract.WriteEventLog(CliEventClass.TransientSkip, () =>
        {
            var reasonLabel = reason switch
            {
                PendingRebootReason.MsiExecuteMutexHeld =>
                    Strings.Cli_EventLogReason_MsiExecuteMutex,
                PendingRebootReason.InstallerInProgress =>
                    Strings.Cli_EventLogReason_InstallerInProgress,
                PendingRebootReason.PendingRenameInCache =>
                    Strings.Cli_EventLogReason_PendingRenameInCache,
                _ => reason.ToString(),
            };
            return string.Format(Strings.Cli_EventLogPendingRebootBlocked,
                arg, reasonLabel, detail ?? string.Empty);
        });
        // Transient: a reboot (or the in-flight transaction finishing) clears the
        // gate. Hard scan and move/delete failures stay on ExitError.
        return ExitTransient;
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
                () => string.Format(Strings.Cli_EventLogMoveDestinationInsideInstaller, arg, resolved));
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
