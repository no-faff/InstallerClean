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

    /// <summary>1 = total failure: scan failed, mutex contention, bad
    /// args, or every file in the batch failed. Sysadmin scripts
    /// should treat this as "the run accomplished nothing".</summary>
    private const int ExitError = 1;

    /// <summary>2 = partial success: the operation processed some
    /// files but at least one failed. Distinguishable from ExitError
    /// so a Task Scheduler retry policy can decide whether to retry
    /// (probably yes on partial; probably no on full failure).</summary>
    private const int ExitPartial = 2;

    /// <summary>POSIX convention: 130 == 128 + SIGINT (Ctrl+C).</summary>
    private const int ExitCancelled = 130;

    public static async Task<int> Main(string[] args)
    {
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
                mutex.Dispose();
                Console.CancelKeyPress -= cancelHandler;
                return ExitError;
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

            // Match the GUI: cleaning the cache mid-update can break
            // the pending repair sequence regardless of how it was
            // launched. /s reads only, so it skips the gate.
            if (arg is "/d" or "/m")
            {
                var rebootService = services.GetRequiredService<IPendingRebootService>();
                if (rebootService.HasPendingReboot())
                {
                    Console.WriteLine(Strings.Cli_PendingRebootBlocked);
                    EventLogWriter.Write(EventLogWriter.Level.Warning,
                        string.Format(Strings.Cli_EventLogPendingRebootBlocked, arg));
                    return ExitError;
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
                // 0 / 2 / 1: full / partial / total failure (see
                // ExitOk / ExitPartial / ExitError).
                if (result.Errors.Count == 0) return ExitOk;
                if (result.DeletedCount > 0) return ExitPartial;
                return ExitError;
            }

            // arg == "/m"
            string dest;
            if (args.Length > 1)
            {
                dest = args[1].Trim();
            }
            else
            {
                var settingsService = services.GetRequiredService<ISettingsService>();
                dest = settingsService.Load().MoveDestination;
            }
            if (string.IsNullOrWhiteSpace(dest))
            {
                Console.WriteLine(Strings.Cli_NoMoveDestination);
                EventLogWriter.Write(EventLogWriter.Level.Warning,
                    string.Format(Strings.Cli_EventLogMoveNoDestination, arg));
                return ExitError;
            }

            if (InstallerCacheHelpers.IsInstallerFolderOrChild(dest))
            {
                Console.WriteLine(Strings.Cli_MoveDestinationInsideInstaller);
                EventLogWriter.Write(EventLogWriter.Level.Warning,
                    string.Format(Strings.Cli_EventLogMoveDestinationInsideInstaller, arg, dest));
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
        Console.WriteLine(Strings.Cli_Help_ExitCodePartial);
        Console.WriteLine(Strings.Cli_Help_ExitCodeError);
        Console.WriteLine(Strings.Cli_Help_ExitCodeCancelled);
        Console.WriteLine();
        Console.WriteLine(Strings.Cli_Help_NoteLine1);
        Console.WriteLine(Strings.Cli_Help_NoteLine2);
        Console.WriteLine(Strings.Cli_Help_NoteLine3);
        Console.WriteLine();
    }
}
