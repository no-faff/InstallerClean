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
    /// <summary>0 = the operation completed and every file the scan
    /// flagged was successfully processed.</summary>
    private const int ExitOk = 0;

    /// <summary>1 = total failure: scan failed, bad args, or every
    /// file in the batch failed. Sysadmin scripts should treat this
    /// as "the run accomplished nothing and it's not transient".</summary>
    private const int ExitError = 1;

    /// <summary>2 = partial success: the operation processed some
    /// files but at least one failed. Distinguishable from ExitError
    /// so a Task Scheduler retry policy can decide whether to retry
    /// (probably yes on partial; probably no on full failure).</summary>
    private const int ExitPartial = 2;

    /// <summary>75 = POSIX EX_TEMPFAIL: transient condition the
    /// caller may want to retry later. Used when the single-instance
    /// mutex is held (GUI or another CLI run in flight) or a pending
    /// Windows Installer transaction blocks cache changes. Distinct
    /// from ExitError so a Task Scheduler "retry every 15 minutes for
    /// 3 attempts" policy can fire on transient and back off on hard
    /// failure.</summary>
    private const int ExitTransient = 75;

    /// <summary>POSIX convention: 130 == 128 + SIGINT (Ctrl+C).</summary>
    private const int ExitCancelled = 130;

    public static async Task<int> Main(string[] args)
    {
        // UTF-8 stdout so a future translation of Cli.* into a
        // non-ASCII language doesn't mojibake under redirected output
        // (cmd /c installerclean-cli /s > out.txt) or under
        // PowerShell 5 which still defaults to OEM. Today every CLI
        // resx value is ASCII so this is benign; setting it now
        // future-proofs the resx surface.
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
                // Previous owner crashed without releasing; .NET hands
                // us ownership and we proceed.
                holdsMutex = true;
            }
            if (!holdsMutex)
            {
                Console.WriteLine(Strings.Startup_AlreadyRunningBody);
                // Audit the skipped run on the Application channel so an
                // RMM consumer polling for InstallerClean entries can
                // tell "the task fired but the GUI was open" from "the
                // task never fired".
                EventLogWriter.Write(EventLogWriter.Level.Information,
                    string.Format(Strings.Cli_EventLogMutexBlocked, arg));
                if (EventLogWriter.EventLogUnavailable)
                    Console.WriteLine(Strings.Cli_EventLogUnavailable);
                mutex.Dispose();
                Console.CancelKeyPress -= cancelHandler;
                return ExitTransient;
            }
        }

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
            var scanResult = await scanService.ScanAsync(cancellationToken: cts.Token);

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
                    // Pending reboot is transient (the user reboots and
                    // the next scheduled run proceeds); distinct from
                    // ExitError so a retry policy can act on it without
                    // also retrying hard scan/move/delete failures.
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
                var result = await deleteService.DeleteFilesAsync(filePaths, progress, cts.Token);
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
                // All-deleted is full success; any-deleted-with-errors is partial; nothing-deleted is total failure.
                if (result.Errors.Count == 0) return ExitOk;
                if (result.DeletedCount > 0) return ExitPartial;
                return ExitError;
            }

            // /d and /s already returned; everything below this point runs only for /m.
            string dest;
            // Two destination sources with different trust posture:
            // the command-line argument is supplied at invocation, the
            // settings-loaded path is whatever was last written into
            // %LOCALAPPDATA% (which the CLI then writes to silently
            // without showing the resolved path first). The settings
            // path therefore goes through an extra system-folder gate.
            bool destFromSettings;
            if (args.Length > 1)
            {
                dest = args[1].Trim();
                destFromSettings = false;
            }
            else
            {
                var settingsService = services.GetRequiredService<ISettingsService>();
                dest = settingsService.Load().MoveDestination;
                destFromSettings = true;
            }
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

            if (destFromSettings && InstallerCacheHelpers.IsSystemFolderOrChild(dest))
            {
                Console.WriteLine(string.Format(Strings.Cli_MoveDestinationInSystemFolder, dest));
                EventLogWriter.Write(EventLogWriter.Level.Warning,
                    string.Format(Strings.Cli_EventLogMoveDestinationInSystemFolder, arg, dest));
                return ExitError;
            }

            var moveService = services.GetRequiredService<IMoveFilesService>();
            Console.WriteLine(string.Format(Strings.Cli_MovingFiles,
                count, DisplayHelpers.PluraliseFile(count), dest));
            var moveResult = await moveService.MoveFilesAsync(filePaths, dest, progress, cts.Token);
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
        catch (UnauthorizedAccessException ex)
        {
            // Two production sites throw UnauthorizedAccessException with
            // resx-sourced messages safe to print under elevation: the MSI
            // enumerator on AccessDenied (Strings.Error_MsiAccessDenied,
            // which itself names the admin requirement) and the Move
            // pre-flight probe on a non-writable destination
            // (Strings.Error_CannotWriteFolder, naming the destination).
            // Echoing ex.Message directly distinguishes the two; printing
            // a static "AdminRequired" line for both made a read-only
            // destination look like a privilege failure.
            Console.WriteLine(ex.Message);
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
            return ExitError;
        }
        finally
        {
            // Surface the EventLog-unavailable warning once per run,
            // after the main output but before mutex release. RMM
            // consumers expecting Application-channel entries get a
            // record (in stdout) that the channel was unwritable.
            if (EventLogWriter.EventLogUnavailable)
                Console.WriteLine(Strings.Cli_EventLogUnavailable);
            Console.CancelKeyPress -= cancelHandler;
            if (holdsMutex) mutex!.ReleaseMutex();
            mutex?.Dispose();
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
