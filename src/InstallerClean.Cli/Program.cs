using Microsoft.Extensions.DependencyInjection;
using InstallerClean.Helpers;
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
    private const int ExitOk = 0;
    private const int ExitError = 1;
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
                // The previous owner (GUI or CLI) crashed without
                // releasing. .NET transfers ownership to us; treat that
                // as a successful acquisition rather than surfacing a
                // confusing generic error. The crashed process has
                // already exited, so the kernel state we now hold is
                // safe to use.
                holdsMutex = true;
            }
            if (!holdsMutex)
            {
                Console.WriteLine(Strings.Startup_AlreadyRunningBody);
                mutex.Dispose();
                return ExitError;
            }
        }

        using var cts = new CancellationTokenSource();
        ConsoleCancelEventHandler cancelHandler = (_, cancelArgs) =>
        {
            cancelArgs.Cancel = true; // keep the process running long enough to stop gracefully
            Console.WriteLine();
            Console.WriteLine(Strings.Cli_Cancelling);
            cts.Cancel();
        };
        Console.CancelKeyPress += cancelHandler;

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
                    string.Format(Strings.Cli_EventLogScanNoOrphans, arg, scanResult.RegisteredPackages.Count));
                return ExitOk;
            }

            if (arg == "/s")
            {
                Console.WriteLine(string.Join(Environment.NewLine,
                    scanResult.RemovableFiles.Select(f =>
                        $"  {f.FileName}  ({f.SizeDisplay}, {f.Reason})")));
                EventLogWriter.Write(EventLogWriter.Level.Information,
                    string.Format(Strings.Cli_EventLogScanFound, count, size));
                return ExitOk;
            }

            // SECURITY/CORRECTNESS: gate /d and /m on pending-reboot just
            // like the GUI does after step 10. Cleaning the installer
            // cache while Windows Update is mid-staging breaks the
            // pending repair / rollback sequence; the risk is the same
            // whether the user clicked Move in the GUI or scheduled
            // installerclean-cli /d in a Task. /s is unaffected because
            // it only reads.
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

            if (arg == "/d")
            {
                var deleteService = services.GetRequiredService<IDeleteFilesService>();
                Console.WriteLine(string.Format(Strings.Cli_DeletingFiles, count));
                var result = await deleteService.DeleteFilesAsync(filePaths, null, cts.Token);
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
                        result.DeletedCount, count, size, result.Errors.Count));
                return result.Errors.Count > 0 ? ExitError : ExitOk;
            }

            // arg == "/m"
            var settingsService = services.GetRequiredService<ISettingsService>();
            var settings = settingsService.Load();
            var dest = args.Length > 1 ? args[1] : settings.MoveDestination;
            if (string.IsNullOrWhiteSpace(dest))
            {
                Console.WriteLine(Strings.Cli_NoMoveDestination);
                EventLogWriter.Write(EventLogWriter.Level.Warning,
                    Strings.Cli_EventLogMoveNoDestination);
                return ExitError;
            }

            if (InstallerCacheHelpers.IsInstallerFolderOrChild(dest))
            {
                Console.WriteLine(Strings.Cli_MoveDestinationInsideInstaller);
                EventLogWriter.Write(EventLogWriter.Level.Warning,
                    string.Format(Strings.Cli_EventLogMoveDestinationInsideInstaller, dest));
                return ExitError;
            }

            var moveService = services.GetRequiredService<IMoveFilesService>();
            Console.WriteLine(string.Format(Strings.Cli_MovingFiles, count, dest));
            var moveResult = await moveService.MoveFilesAsync(filePaths, dest, null, cts.Token);
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
                    moveResult.MovedCount, count, dest, size, moveResult.Errors.Count));
            return moveResult.Errors.Count > 0 ? ExitError : ExitOk;
        }
        catch (OperationCanceledException)
        {
            Console.WriteLine(Strings.Cli_Cancelled);
            return ExitCancelled;
        }
        catch (UnauthorizedAccessException)
        {
            Console.WriteLine(Strings.Cli_AdminRequired);
            return ExitError;
        }
        catch (Exception ex)
        {
            // SECURITY: write the full exception (message + stack trace)
            // to the crash log; print only the type name to stdout. The
            // CLI runs elevated and stdout often gets redirected into a
            // log file by Task Scheduler or RMM tooling - putting the
            // raw .Message there would risk leaking paths from another
            // user's profile that the elevated process happened to
            // touch on this user's behalf.
            var logPath = Helpers.CrashLog.Write(ex);
            Console.WriteLine(string.Format(Strings.Cli_GenericError, ex.GetType().Name, logPath));
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
        Console.WriteLine(Strings.Cli_Help_NoteLine1);
        Console.WriteLine(Strings.Cli_Help_NoteLine2);
        Console.WriteLine(Strings.Cli_Help_NoteLine3);
        Console.WriteLine();
    }
}
