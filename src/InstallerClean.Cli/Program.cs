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
/// </summary>
internal static class Program
{
    /// <summary>0 = every file the scan flagged was processed.</summary>
    private const int ExitOk = 0;

    /// <summary>1 = hard failure: scan failed, bad args, or every file
    /// in the batch failed. Not transient.</summary>
    private const int ExitError = 1;

    /// <summary>2 = partial: the operation processed some files but at
    /// least one failed. Distinct from ExitError so a retry policy
    /// can act on the partial case differently from total failure.</summary>
    private const int ExitPartial = 2;

    /// <summary>75 = POSIX EX_TEMPFAIL: transient. The single-instance
    /// mutex is held by the GUI or another CLI run, or a pending
    /// Windows Installer transaction blocks cache changes. Distinct
    /// from ExitError so a retry-on-transient policy can fire here
    /// and back off on hard failure.</summary>
    private const int ExitTransient = 75;

    /// <summary>POSIX convention: 130 == 128 + SIGINT (Ctrl+C).</summary>
    private const int ExitCancelled = 130;

    public static int Main(string[] args)
    {
        // Pin to UTF-8 so a Cli.* translation into a non-ASCII language
        // doesn't mojibake under redirected output (cmd /c
        // installerclean-cli /s > out.txt) or PowerShell 5's OEM
        // default code page.
        Console.OutputEncoding = Encoding.UTF8;

        // Lowercase up front so every later comparison (--help, /?, -h,
        // /s, /d, /m) is case-insensitive. PowerShell users frequently
        // type /S in upper case.
        var arg = args.Length == 0 ? string.Empty : args[0].ToLowerInvariant();

        if (args.Length == 0 || arg is "--help" or "/?" or "-h")
        {
            PrintUsage();
            return ExitOk;
        }

        if (arg is not "/d" and not "/m" and not "/s")
        {
            Console.WriteLine(string.Format(Strings.Cli_UnknownArgument, args[0]));
            Console.WriteLine();
            PrintUsage();
            return ExitError;
        }

        // /m takes an optional second arg (the destination); /s and /d
        // take none. Reject anything beyond so an unquoted path with
        // spaces ("/m D:\My Backup") doesn't silently become "D:\My".
        var maxArgs = arg == "/m" ? 2 : 1;
        if (args.Length > maxArgs)
        {
            Console.WriteLine(string.Format(Strings.Cli_UnknownArgument, args[maxArgs]));
            Console.WriteLine();
            PrintUsage();
            return ExitError;
        }

        // Cancel handler before mutex: a Ctrl+C in the gap should
        // print "Cancelling..." rather than terminate via the default
        // handler.
        using var cts = new CancellationTokenSource();
        ConsoleCancelEventHandler cancelHandler = (_, cancelArgs) =>
        {
            cancelArgs.Cancel = true; // keep the process running long enough to stop gracefully
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
                EventLogWriter.Write(EventLogWriter.Level.Information,
                    string.Format(Strings.Cli_EventLogMutexBlocked, arg));
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
            return RunWorkAsync(arg, args, cts.Token).GetAwaiter().GetResult();
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

    private static async Task<int> RunWorkAsync(string arg, string[] args, CancellationToken token)
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

            var scanService = services.GetRequiredService<IFileSystemScanService>();

            Console.WriteLine(Strings.Cli_ScanningInstaller);
            var scanResult = await scanService.ScanAsync(cancellationToken: token);

            var count = scanResult.RemovableFiles.Count;
            var totalBytes = scanResult.RemovableFiles.Sum(f => f.SizeBytes);
            var size = DisplayHelpers.FormatSize(totalBytes);
            Console.WriteLine(string.Format(Strings.Cli_FoundOrphans,
                count, DisplayHelpers.PluraliseFile(count), size));

            if (count == 0)
            {
                Console.WriteLine(Strings.Cli_NothingToDo);
                EventLogWriter.Write(EventLogWriter.Level.Information,
                    string.Format(Strings.Cli_EventLogScanNoOrphans,
                        arg, scanResult.RegisteredPackages.Count,
                        DisplayHelpers.PluralisePackage(scanResult.RegisteredPackages.Count)));
                return ExitOk;
            }

            if (arg == "/s")
            {
                Console.WriteLine(string.Join(Environment.NewLine,
                    scanResult.RemovableFiles.Select(f =>
                        $"  {f.FileName}  ({f.SizeDisplay}, {f.Reason})")));
                EventLogWriter.Write(EventLogWriter.Level.Information,
                    string.Format(Strings.Cli_EventLogScanFound,
                        arg, count, DisplayHelpers.PluraliseFile(count), size));
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
                    var stdoutMessage = rebootCheck.Reason!.Value switch
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
                            $"Unhandled PendingRebootReason: {rebootCheck.Reason!.Value}. " +
                            "A new enum value was added without updating the CLI message switch."),
                    };
                    Console.WriteLine(stdoutMessage);
                    EventLogWriter.Write(EventLogWriter.Level.Warning,
                        string.Format(Strings.Cli_EventLogPendingRebootBlocked,
                            arg,
                            rebootCheck.Reason?.ToString() ?? "Unknown",
                            rebootCheck.Detail ?? string.Empty));
                    // Transient: a reboot clears the gate. Hard scan and
                    // move/delete failures stay on ExitError.
                    return ExitTransient;
                }
            }

            var filePaths = scanResult.RemovableFiles.Select(f => f.FullPath).ToList();

            // Per-file progress: prints the file name to stdout and
            // updates processedCount so the OCE catch can attribute
            // the cancellation correctly.
            totalToProcess = count;
            var progress = new Progress<OperationProgress>(p =>
            {
                processedCount = p.CurrentFile;
                Console.WriteLine($"  [{p.CurrentFile}/{p.TotalFiles}] {p.CurrentFileName}");
            });

            if (arg == "/d")
            {
                var deleteService = services.GetRequiredService<IDeleteFilesService>();
                Console.WriteLine(string.Format(Strings.Cli_DeletingFiles,
                    count, DisplayHelpers.PluraliseFile(count)));
                var result = await deleteService.DeleteFilesAsync(filePaths, progress, token);
                Console.WriteLine(string.Format(Strings.Cli_DeletedFiles,
                    result.DeletedCount, DisplayHelpers.PluraliseFile(result.DeletedCount)));
                if (result.Errors.Count > 0)
                {
                    Console.WriteLine($"{result.Errors.Count} {DisplayHelpers.PluraliseError(result.Errors.Count)}:");
                    foreach (var err in result.Errors)
                        Console.WriteLine($"  {Path.GetFileName(err.FilePath)}: {err.LocalisedMessage}");
                }
                var level = result.Errors.Count > 0 ? EventLogWriter.Level.Warning : EventLogWriter.Level.Information;
                EventLogWriter.Write(level,
                    string.Format(Strings.Cli_EventLogDeleteSummary,
                        arg, result.DeletedCount, count, DisplayHelpers.PluraliseFile(count),
                        size, result.Errors.Count, DisplayHelpers.PluraliseError(result.Errors.Count)));
                if (result.Errors.Count == 0) return ExitOk;
                if (result.DeletedCount > 0) return ExitPartial;
                return ExitError;
            }

            // Two destination sources: a command-line argument supplied
            // at invocation, or the path last written into
            // %LOCALAPPDATA%. Both go through the same fully-qualified,
            // not-inside-Installer, not-inside-System-folder gates below.
            // A stale Scheduled Task argument has the same trust posture
            // as a stale settings.json once the CLI is running elevated.
            string dest;
            if (args.Length > 1)
                dest = args[1].Trim();
            else
                dest = services.GetRequiredService<ISettingsService>().Load().MoveDestination;
            if (string.IsNullOrWhiteSpace(dest))
            {
                Console.WriteLine(Strings.Cli_NoMoveDestination);
                EventLogWriter.Write(EventLogWriter.Level.Warning,
                    string.Format(Strings.Cli_EventLogMoveNoDestination, arg));
                return ExitError;
            }

            // Reject relative destinations: Path.GetFullPath would
            // otherwise resolve them against the process CWD, and the
            // CLI host's CWD is whatever the caller invoked it from.
            if (!Path.IsPathFullyQualified(dest))
            {
                Console.WriteLine(string.Format(Strings.Cli_MoveDestinationRelative, dest));
                EventLogWriter.Write(EventLogWriter.Level.Warning,
                    string.Format(Strings.Cli_EventLogMoveDestinationRelative, arg, dest));
                return ExitError;
            }

            if (InstallerCacheHelpers.IsInstallerFolderOrChild(dest))
            {
                Console.WriteLine(Strings.Cli_MoveDestinationInsideInstaller);
                EventLogWriter.Write(EventLogWriter.Level.Warning,
                    string.Format(Strings.Cli_EventLogMoveDestinationInsideInstaller, arg, dest));
                return ExitError;
            }

            // System-folder gate covers both destination sources: the
            // command-line /m argument and the settings-loaded fallback
            // have the same trust posture (an admin's stale Scheduled
            // Task argument can drift just like a stale settings.json).
            if (InstallerCacheHelpers.IsSystemFolderOrChild(dest))
            {
                Console.WriteLine(string.Format(Strings.Cli_MoveDestinationInSystemFolder, dest));
                EventLogWriter.Write(EventLogWriter.Level.Warning,
                    string.Format(Strings.Cli_EventLogMoveDestinationInSystemFolder, arg, dest));
                return ExitError;
            }

            var moveService = services.GetRequiredService<IMoveFilesService>();
            Console.WriteLine(string.Format(Strings.Cli_MovingFiles,
                count, DisplayHelpers.PluraliseFile(count), dest));
            var moveResult = await moveService.MoveFilesAsync(filePaths, dest, progress, token);
            Console.WriteLine(string.Format(Strings.Cli_MovedFiles,
                moveResult.MovedCount, DisplayHelpers.PluraliseFile(moveResult.MovedCount)));
            if (moveResult.Errors.Count > 0)
            {
                Console.WriteLine($"{moveResult.Errors.Count} {DisplayHelpers.PluraliseError(moveResult.Errors.Count)}:");
                foreach (var err in moveResult.Errors)
                    Console.WriteLine($"  {Path.GetFileName(err.FilePath)}: {err.LocalisedMessage}");
            }
            var moveLevel = moveResult.Errors.Count > 0 ? EventLogWriter.Level.Warning : EventLogWriter.Level.Information;
            EventLogWriter.Write(moveLevel,
                string.Format(Strings.Cli_EventLogMoveSummary,
                    arg, moveResult.MovedCount, count, DisplayHelpers.PluraliseFile(count),
                    dest, size, moveResult.Errors.Count, DisplayHelpers.PluraliseError(moveResult.Errors.Count)));
            if (moveResult.Errors.Count == 0) return ExitOk;
            if (moveResult.MovedCount > 0) return ExitPartial;
            return ExitError;
        }
        catch (OperationCanceledException)
        {
            Console.WriteLine(Strings.Cli_Cancelled);
            // EventLog the cancellation so a Task Scheduler audit can
            // see how far the run got, and pick ExitPartial when work
            // committed before the Ctrl+C arrived.
            if (processedCount > 0)
            {
                EventLogWriter.Write(EventLogWriter.Level.Warning,
                    string.Format(Strings.Cli_EventLogCancelledPartial,
                        arg, processedCount, totalToProcess,
                        DisplayHelpers.PluraliseFile(totalToProcess)));
                return ExitPartial;
            }
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
            EventLogWriter.Write(EventLogWriter.Level.Warning,
                string.Format(Strings.Cli_EventLogValidationFailed, arg, ex.Message));
            return ExitError;
        }
        catch (LocalisedInvalidOperationException ex)
        {
            // Same contract as LocalisedAccessException: resx-templated
            // safe-to-echo. Reached for InstallerQueryService throws
            // (empty database, MSI enumerator hard-fail) and
            // MoveFilesService validation throws (not-fully-qualified
            // destination, IsInstallerFolderOrChild race, destination-
            // changed-mid-batch). The sysadmin sees what to fix
            // instead of a generic "see crash.log" breadcrumb.
            Console.WriteLine(ex.Message);
            EventLogWriter.Write(EventLogWriter.Level.Warning,
                string.Format(Strings.Cli_EventLogValidationFailed, arg, ex.Message));
            return ExitError;
        }
        catch (Exception ex)
        {
            // ex.Message stays out of stdout: under elevation it can carry
            // cross-profile paths, and Task Scheduler / RMM tooling
            // routinely captures stdout to disk.
            var crash = Helpers.CrashLog.TryWrite(ex);
            var typeName = ex.GetType().Name;
            Console.WriteLine(crash.Written
                ? string.Format(Strings.Cli_GenericError, typeName, crash.Path)
                : string.Format(Strings.Cli_GenericError_NoLog, typeName));
            // EventLog the failure so an Application-channel audit picks
            // up every CLI invocation, not just the success and partial-
            // success branches. Same path-leak discipline as stdout:
            // type-name + crash-log path only, never ex.Message.
            EventLogWriter.Write(EventLogWriter.Level.Warning, crash.Written
                ? string.Format(Strings.Cli_EventLogHardError, arg, typeName, crash.Path)
                : string.Format(Strings.Cli_EventLogHardError_NoLog, arg, typeName));
            return ExitError;
        }
    }

    private static void PrintUsage()
    {
        Console.WriteLine(Strings.Cli_Help_Header);
        Console.WriteLine();
        Console.WriteLine(Strings.Cli_Help_Usage);
        Console.WriteLine(Strings.Cli_Help_Help);
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
}
