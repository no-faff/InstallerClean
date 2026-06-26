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
        // Pin to UTF-8 so a Cli.* translation into a non-ASCII language
        // doesn't mojibake under redirected output (cmd /c
        // installerclean-cli /s > out.txt) or PowerShell 5's OEM
        // default code page.
        Console.OutputEncoding = Encoding.UTF8;

        // Human-facing stdout follows the OS UI culture: Italian on an Italian
        // machine, Japanese on a Japanese one; a locale with no satellite falls
        // back through the resx hierarchy to neutral English. CurrentCulture is
        // left untouched, so sizes format in the OS region ("3,2 GB"). The lines
        // other software reads stay English regardless: the Application-channel
        // Event Log (RMM greps it for known English phrases) and the
        // "\d+ errors:" stdout header (a documented script scrape) are built
        // through MachineContract, which forces en-GB at the emit site. The
        // count in that header and the "[i/total]" progress lines are plain
        // integers that group in no culture, so only the "errors" noun needs
        // forcing, not the numbers.

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
                if (EventLogWriter.EventLogUnavailable)
                    Console.WriteLine(Strings.Cli_EventLogUnavailable);
                return ExitError;
            case CliCommand.UnknownArgument:
            case CliCommand.TooManyArguments:
                Console.WriteLine(string.Format(Strings.Cli_UnknownArgument, invocation.OffendingArgument));
                Console.WriteLine();
                PrintUsage();
                // Audit the bad invocation: a scheduled task with a
                // fat-fingered flag otherwise exits 1 with no Application-channel
                // trace, the exact ambiguity the EventLog summary exists to
                // remove. This switch returns before the try/finally that emits
                // the unavailable note, so emit it inline as the mutex path does.
                MachineContract.WriteEventLog(CliEventClass.HardError,
                    () => string.Format(Strings.Cli_EventLogBadArguments, invocation.OffendingArgument));
                if (EventLogWriter.EventLogUnavailable)
                    Console.WriteLine(Strings.Cli_EventLogUnavailable);
                return ExitError;
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
            // Idempotent under repeated Ctrl+C: the stdout "Cancelling..."
            // line must not double or scripts grepping `\d+ errors:` on
            // a later line count drift by one.
            if (cts.IsCancellationRequested) return;
            Console.WriteLine();
            Console.WriteLine(Strings.Cli_Cancelling);
            cts.Cancel();
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
                Console.WriteLine(Strings.Cli_MutexBlocked);
                // RMM consumer polling the Application channel for
                // InstallerClean entries needs an audit record on the
                // skipped path to distinguish it from "the task never
                // fired".
                MachineContract.WriteEventLog(CliEventClass.TransientSkip,
                    () => string.Format(Strings.Cli_EventLogMutexBlocked, arg));
                if (EventLogWriter.EventLogUnavailable)
                    Console.WriteLine(Strings.Cli_EventLogUnavailable);
                mutex.Dispose();
                Console.CancelKeyPress -= cancelHandler;
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
            // One stdout audit line per run: if any Write fell into the
            // unavailable path, RMM consumers polling the Application
            // channel see a record that the channel was unwritable.
            if (EventLogWriter.EventLogUnavailable)
                Console.WriteLine(Strings.Cli_EventLogUnavailable);
            Console.CancelKeyPress -= cancelHandler;
            if (holdsMutex) mutex!.ReleaseMutex();
            mutex?.Dispose();
        }
    }

    private static async Task<int> RunWorkAsync(string arg, CliInvocation invocation, CancellationToken token)
    {
        // Tracks the highest CurrentFile reported by the move/delete
        // progress reporter. On a Ctrl+C mid-loop the OCE catch reads
        // this to write an EventLog summary and pick ExitPartial vs
        // ExitCancelled.
        int processedCount = 0;
        int totalToProcess = 0;

        try
        {
            using var services = new ServiceCollection()
                .AddInstallerCleanCore()
                .BuildServiceProvider(validateScopes: true);

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
            Console.WriteLine(string.Format(
                DisplayHelpers.Pluralise(count, Strings.Cli_FoundOrphans, Strings.Cli_FoundOrphans, "Cli.FoundOrphans"),
                count, DisplayHelpers.PluraliseFile(count), size));

            if (count == 0)
            {
                Console.WriteLine(Strings.Cli_NothingToDo);
                MachineContract.WriteEventLog(CliEventClass.Ok,
                    () => string.Format(Strings.Cli_EventLogScanNoOrphans,
                        arg, scanResult.RegisteredPackages.Count,
                        DisplayHelpers.PluralisePackage(scanResult.RegisteredPackages.Count)));
                return ExitOk;
            }

            if (arg == "/s")
            {
                Console.WriteLine(string.Join(Environment.NewLine,
                    scanResult.RemovableFiles.Select(f =>
                        $"  {f.FileName}  ({f.SizeDisplay}, {f.Reason})")));
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
                {
                    // Block + null Reason is unreachable per the PendingRebootResult.Block
                    // factory contract; .Value is safe inside this IsBlocked branch.
                    var reason = rebootCheck.Reason!.Value;
                    var stdoutMessage = reason switch
                    {
                        PendingRebootReason.MsiExecuteMutexHeld =>
                            Strings.Cli_PendingRebootBlocked_MsiExecuteMutex,
                        PendingRebootReason.InstallerInProgress =>
                            Strings.Cli_PendingRebootBlocked_InstallerInProgress,
                        PendingRebootReason.PendingRenameInCache =>
                            string.Format(
                                Strings.Cli_PendingRebootBlocked_PendingRenameInCache,
                                rebootCheck.Detail ?? string.Empty),
                        _ => throw new InvalidOperationException(
                            $"Unhandled PendingRebootReason: {reason}. " +
                            "A new enum value was added without updating the CLI message switch."),
                    };
                    Console.WriteLine(stdoutMessage);
                    // The reason label and template are built English: the
                    // Cli.EventLogReason.* labels ARE translated in the
                    // satellites, but the Application channel is sysadmin-facing
                    // and an RMM grep on a known phrase needs a stable English
                    // target. The localised stdout sentence above is what the
                    // operator reads; the label switch lives inside the scope so
                    // it resolves en-GB, not the OS language.
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
                            arg, reasonLabel, rebootCheck.Detail ?? string.Empty);
                    });
                    // Transient: a reboot clears the gate. Hard scan and
                    // move/delete failures stay on ExitError.
                    return ExitTransient;
                }
            }

            var filePaths = scanResult.RemovableFiles.Select(f => f.FullPath).ToList();

            // Per-file progress, reported synchronously on the producing
            // thread (see SynchronousProgress). A console Main has no
            // SynchronizationContext, so Progress<T> would marshal each
            // report through the thread pool and let a "[i/total]" line
            // print after the post-await summary ("Deleted N files."),
            // breaking the stdout line order an RMM scrapes. Report also
            // advances processedCount so the OCE catch can attribute a
            // cancellation to the right file count.
            totalToProcess = count;
            var progress = new SynchronousProgress<OperationProgress>(p =>
            {
                processedCount = p.CurrentFile;
                Console.WriteLine($"  [{p.CurrentFile}/{p.TotalFiles}] {p.CurrentFileName}");
            });

            if (arg == "/d")
            {
                var deleteService = services.GetRequiredService<IDeleteFilesService>();
                Console.WriteLine(string.Format(
                    DisplayHelpers.Pluralise(count, Strings.Cli_DeletingFiles, Strings.Cli_DeletingFiles, "Cli.DeletingFiles"),
                    count, DisplayHelpers.PluraliseFile(count)));
                var result = await deleteService.DeleteFilesAsync(
                    filePaths, permitPermanentDelete: false, progress: progress, cancellationToken: token);

                // The shell recycle is recycle-or-permanently-delete. When the bin is
                // unavailable for the volume the service refuses rather than nuking, and a
                // non-interactive CLI cannot offer the Move/permanent/cancel choice the GUI
                // will: surface guidance and exit transient (re-enabling the bin or a reboot
                // clears it). There is deliberately no /force permanent-delete flag.
                if (result.RecycleUnavailable)
                {
                    Console.WriteLine(Strings.Cli_RecycleUnavailable);
                    MachineContract.WriteEventLog(CliEventClass.TransientSkip,
                        () => string.Format(Strings.Cli_EventLogRecycleUnavailable, arg));
                    return ExitTransient;
                }

                Console.WriteLine(string.Format(
                    DisplayHelpers.Pluralise(result.DeletedCount, Strings.Cli_DeletedFiles, Strings.Cli_DeletedFiles, "Cli.DeletedFiles"),
                    result.DeletedCount, DisplayHelpers.PluraliseFile(result.DeletedCount)));
                if (result.Errors.Count > 0)
                {
                    // Plural "errors:" emitted regardless of count: the
                    // documented RMM-scrape contract is \d+ errors: on
                    // stdout; the one-error case must keep the same
                    // shape so a `grep -E '[0-9]+ errors:'` matches.
                    // English-grammar oddity ("1 errors:") is the cost
                    // of a stable machine-parseable surface. The noun is forced
                    // English (MachineContract) so it stays "errors", not the
                    // localised plural, on a non-English machine.
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
                    : SumBytesExcludingErrors(scanResult.RemovableFiles, result.Errors);
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
            Console.WriteLine(string.Format(
                DisplayHelpers.Pluralise(count, Strings.Cli_MovingFiles, Strings.Cli_MovingFiles, "Cli.MovingFiles"),
                count, DisplayHelpers.PluraliseFile(count), moveDest));
            var moveResult = await moveService.MoveFilesAsync(filePaths, moveDest, progress, token);
            Console.WriteLine(string.Format(
                DisplayHelpers.Pluralise(moveResult.MovedCount, Strings.Cli_MovedFiles, Strings.Cli_MovedFiles, "Cli.MovedFiles"),
                moveResult.MovedCount, DisplayHelpers.PluraliseFile(moveResult.MovedCount)));
            if (moveResult.Errors.Count > 0)
            {
                // See the matching block in the /d branch for the
                // always-plural rationale and the English-forced noun: the
                // RMM-scrape contract on stdout requires "\d+ errors:".
                Console.WriteLine(MachineContract.English(
                    () => $"{moveResult.Errors.Count} {Strings.Plural_Error_Plural}:"));
                foreach (var err in moveResult.Errors)
                    Console.WriteLine($"  {Path.GetFileName(err.FilePath)}: {err.LocalisedMessage}");
            }
            // Same per-file error exclusion as the /d branch.
            long actualMovedBytes = moveResult.Errors.Count == 0
                ? totalBytes
                : SumBytesExcludingErrors(scanResult.RemovableFiles, moveResult.Errors);
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
            if (processedCount > 0)
            {
                MachineContract.WriteEventLog(CliEventClass.Partial,
                    () => string.Format(Strings.Cli_EventLogCancelledPartial,
                        arg, processedCount, totalToProcess,
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
            // ex.Message stays out of stdout AND the EventLog: under
            // elevation it can carry cross-profile paths, and Task
            // Scheduler / RMM tooling routinely captures stdout to disk.
            // Type-name + crash-log path only.
            var crash = Helpers.CrashLog.TryWrite(ex);
            var typeName = ex.GetType().Name;
            Console.WriteLine(crash.Written
                ? string.Format(Strings.Cli_GenericError, typeName, crash.Path)
                : string.Format(Strings.Cli_GenericError_NoLog, typeName));
            MachineContract.WriteEventLog(CliEventClass.HardError, () => crash.Written
                ? string.Format(Strings.Cli_EventLogHardError, arg, typeName, crash.Path)
                : string.Format(Strings.Cli_EventLogHardError_NoLog, arg, typeName));
            return ExitError;
        }
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
        Console.WriteLine(Strings.Cli_Help_Header);
        Console.WriteLine();
        Console.WriteLine(Strings.Cli_Help_Usage);
        Console.WriteLine(Strings.Cli_Help_Help);
        Console.WriteLine(Strings.Cli_Help_Version);
        Console.WriteLine(Strings.Cli_Help_ScanOnly);
        Console.WriteLine(Strings.Cli_Help_Delete);
        Console.WriteLine(Strings.Cli_Help_MoveDefault);
        Console.WriteLine(Strings.Cli_Help_MovePath);
        Console.WriteLine();
        Console.WriteLine(Strings.Cli_Help_ExitCodesHeader);
        Console.WriteLine(Strings.Cli_Help_ExitCodeOk);
        Console.WriteLine(Strings.Cli_Help_ExitCodeError);
        Console.WriteLine(Strings.Cli_Help_ExitCodePartial);
        Console.WriteLine(Strings.Cli_Help_ExitCodeTransient);
        Console.WriteLine(Strings.Cli_Help_ExitCodeCancelled);
        Console.WriteLine();
        Console.WriteLine(Strings.Cli_Help_NoteLine1);
        Console.WriteLine(Strings.Cli_Help_NoteLine2);
        Console.WriteLine(Strings.Cli_Help_NoteLine3);
        Console.WriteLine();
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
