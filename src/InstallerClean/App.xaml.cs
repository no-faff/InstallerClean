using System.Threading;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media.Imaging;
using InstallerClean.Helpers;
using InstallerClean.Interop.Native;
using InstallerClean.Resources;
using InstallerClean.Services;
using InstallerClean.ViewModels;

namespace InstallerClean;

public partial class App : Application
{
    private static Mutex? _singleInstanceMutex;

    protected override async void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        _singleInstanceMutex = new Mutex(true, @"Global\InstallerClean_SingleInstance", out bool isFirstInstance);
        if (!isFirstInstance)
        {
            if (e.Args.Length > 0)
            {
                Kernel32.AttachConsole(Kernel32.ATTACH_PARENT_PROCESS);
                Console.WriteLine(Strings.Startup_AlreadyRunningCli);
                Shutdown(1);
            }
            else
            {
                MessageBox.Show(
                    Strings.Startup_AlreadyRunningBody,
                    Strings.Startup_AlreadyRunningTitle, MessageBoxButton.OK, MessageBoxImage.Information);
                Shutdown();
            }
            return;
        }

        if (e.Args.Length > 0)
        {
            await RunCliAsync(e.Args);
            return;
        }

        DispatcherUnhandledException += (_, args) =>
        {
            var logPath = CrashLog.Write(args.Exception);
            MessageBox.Show(
                string.Format(Strings.Startup_UnhandledBody, args.Exception.Message, logPath),
                Strings.Startup_UnhandledTitle, MessageBoxButton.OK, MessageBoxImage.Error);
            args.Handled = true;
            Shutdown(1);
        };

        AppDomain.CurrentDomain.UnhandledException += (_, args) =>
        {
            if (args.ExceptionObject is Exception ex)
                CrashLog.Write(ex);
        };

        TaskScheduler.UnobservedTaskException += (_, args) =>
        {
            CrashLog.Write(args.Exception);
            args.SetObserved();
        };

        SplashWindow? splash = null;
        try
        {
            var appIcon = new BitmapImage(new Uri("pack://application:,,,/Assets/splash-icon.png"));
            EventManager.RegisterClassHandler(typeof(Window), Window.LoadedEvent,
                new RoutedEventHandler((s, _) =>
                {
                    if (s is Window w)
                    {
                        var hwnd = new WindowInteropHelper(w).Handle;
                        int value = 1;
                        Dwmapi.DwmSetWindowAttribute(hwnd, Dwmapi.DWMWA_USE_IMMERSIVE_DARK_MODE, ref value, sizeof(int));
                        w.Icon = appIcon;
                    }
                }));

            splash = new SplashWindow();
            splash.Show();

            splash.UpdateStep(Strings.Status_Scanning, 10);

            var settingsService = new SettingsService();
            var queryService = new InstallerQueryService();
            var scanService = new FileSystemScanService(queryService);
            var moveService = new MoveFilesService();
            var deleteService = new DeleteFilesService();
            var rebootService = new PendingRebootService();
            var msiInfoService = new MsiFileInfoService();
            var dialogService = new DialogService();
            var confirmationService = new ConfirmationService();
            var windowService = new WindowService(settingsService);

            var viewModel = new MainViewModel(
                scanService, moveService, deleteService,
                settingsService, rebootService, msiInfoService,
                dialogService, confirmationService, windowService);

            using var startupCts = new CancellationTokenSource();
            splash.CancelRequested += (_, _) => startupCts.Cancel();

            var splashProgress = new Progress<string>(splash.OnScanProgress);
            try
            {
                var scanTask = viewModel.ScanWithProgressAsync(splashProgress, startupCts.Token);
                await Task.WhenAll(scanTask, Task.Delay(800, startupCts.Token));
            }
            catch (OperationCanceledException)
            {
                splash.Close();
                Shutdown(0);
                return;
            }

            splash.UpdateStep(Strings.Status_Done, 100);
            await Task.Delay(200);

            var window = new MainWindow(viewModel);
            Application.Current.MainWindow = window;
            window.Show();
            splash.Close();
        }
        catch (UnauthorizedAccessException)
        {
            splash?.Close();
            MessageBox.Show(
                Strings.Error_AdminRequiredBody,
                Strings.Error_AdminRequiredTitle,
                MessageBoxButton.OK,
                MessageBoxImage.Warning);
            Shutdown();
        }
        catch (Exception ex)
        {
            splash?.Close();
            CrashLog.Write(ex);
            MessageBox.Show(
                string.Format(Strings.Startup_FailedToStart, ex.Message),
                Strings.Startup_ErrorTitle,
                MessageBoxButton.OK,
                MessageBoxImage.Error);
            Shutdown();
        }
    }

    protected override void OnExit(ExitEventArgs e)
    {
        _singleInstanceMutex?.Dispose();
        base.OnExit(e);
    }

    private async Task RunCliAsync(string[] args)
    {
        // Return ignored: no parent console (Explorer, scheduled task) is a
        // valid case and Console.WriteLine is a no-op there.
        Kernel32.AttachConsole(Kernel32.ATTACH_PARENT_PROCESS);

        var arg = args[0].ToLowerInvariant();
        if (arg is not "/d" and not "/m" and not "/s" and not "--help" and not "/?" and not "-h")
        {
            Console.WriteLine(string.Format(Strings.Cli_UnknownArgument, args[0]));
            Console.WriteLine();
            PrintUsage();
            Shutdown(1);
            return;
        }

        if (arg is "--help" or "/?" or "-h")
        {
            PrintUsage();
            Shutdown();
            return;
        }

        using var cts = new CancellationTokenSource();
        ConsoleCancelEventHandler cancelHandler = (_, cancelArgs) =>
        {
            cancelArgs.Cancel = true; // keep the app running long enough to stop gracefully
            Console.WriteLine();
            Console.WriteLine(Strings.Cli_Cancelling);
            cts.Cancel();
        };
        Console.CancelKeyPress += cancelHandler;

        try
        {
            var queryService = new InstallerQueryService();
            var scanService = new FileSystemScanService(queryService);

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
                Shutdown(0);
                return;
            }

            if (arg == "/s")
            {
                Console.WriteLine(string.Join(Environment.NewLine,
                    scanResult.RemovableFiles.Select(f =>
                        $"  {f.FileName}  ({f.SizeDisplay}, {f.Reason})")));
                EventLogWriter.Write(EventLogWriter.Level.Information,
                    string.Format(Strings.Cli_EventLogScanFound, count, size));
                Shutdown(0);
                return;
            }

            var filePaths = scanResult.RemovableFiles.Select(f => f.FullPath).ToList();

            if (arg == "/d")
            {
                var deleteService = new DeleteFilesService();
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
                Shutdown(result.Errors.Count > 0 ? 1 : 0);
            }
            else if (arg == "/m")
            {
                var settingsService = new SettingsService();
                var settings = settingsService.Load();
                var dest = args.Length > 1 ? args[1] : settings.MoveDestination;
                if (string.IsNullOrWhiteSpace(dest))
                {
                    Console.WriteLine(Strings.Cli_NoMoveDestination);
                    EventLogWriter.Write(EventLogWriter.Level.Warning,
                        Strings.Cli_EventLogMoveNoDestination);
                    Shutdown(1);
                    return;
                }

                if (InstallerCacheHelpers.IsInstallerFolderOrChild(dest))
                {
                    Console.WriteLine(Strings.Cli_MoveDestinationInsideInstaller);
                    EventLogWriter.Write(EventLogWriter.Level.Warning,
                        string.Format(Strings.Cli_EventLogMoveDestinationInsideInstaller, dest));
                    Shutdown(1);
                    return;
                }

                var moveService = new MoveFilesService();
                Console.WriteLine(string.Format(Strings.Cli_MovingFiles, count, dest));
                var result = await moveService.MoveFilesAsync(filePaths, dest, null, cts.Token);
                Console.WriteLine(string.Format(Strings.Cli_MovedFiles,
                    result.MovedCount, DisplayHelpers.PluraliseFile(result.MovedCount)));
                if (result.Errors.Count > 0)
                {
                    Console.WriteLine($"{result.Errors.Count} {DisplayHelpers.PluraliseError(result.Errors.Count)}:");
                    foreach (var err in result.Errors)
                        Console.WriteLine($"  {Path.GetFileName(err.FilePath)}: {err.LocalisedMessage}");
                }
                var level = result.Errors.Count > 0 ? EventLogWriter.Level.Warning : EventLogWriter.Level.Information;
                EventLogWriter.Write(level,
                    string.Format(Strings.Cli_EventLogMoveSummary,
                        result.MovedCount, count, dest, size, result.Errors.Count));
                Shutdown(result.Errors.Count > 0 ? 1 : 0);
            }
        }
        catch (OperationCanceledException)
        {
            Console.WriteLine(Strings.Cli_Cancelled);
            Shutdown(130); // convention: exit 130 for Ctrl+C
        }
        catch (UnauthorizedAccessException)
        {
            Console.WriteLine(Strings.Cli_AdminRequired);
            Shutdown(1);
        }
        catch (Exception ex)
        {
            Console.WriteLine(string.Format(Strings.Cli_GenericError, ex.Message));
            Shutdown(1);
        }
        finally
        {
            Console.CancelKeyPress -= cancelHandler;
        }
    }

    private static void PrintUsage()
    {
        Console.WriteLine(Strings.Cli_Help_Header);
        Console.WriteLine();
        Console.WriteLine(Strings.Cli_Help_Usage);
        Console.WriteLine(Strings.Cli_Help_Gui);
        Console.WriteLine(Strings.Cli_Help_ScanOnly);
        Console.WriteLine(Strings.Cli_Help_Delete);
        Console.WriteLine(Strings.Cli_Help_MoveDefault);
        Console.WriteLine(Strings.Cli_Help_MovePath);
        Console.WriteLine();
        Console.WriteLine(Strings.Cli_Help_NoteLine1);
        Console.WriteLine(Strings.Cli_Help_NoteLine2);
        Console.WriteLine(Strings.Cli_Help_NoteLine3);
        Console.WriteLine(Strings.Cli_Help_WrapExample);
        Console.WriteLine();
    }
}
