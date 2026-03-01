using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media.Imaging;
using SimpleWindowsInstallerCleaner.Helpers;
using SimpleWindowsInstallerCleaner.Services;
using SimpleWindowsInstallerCleaner.ViewModels;

namespace SimpleWindowsInstallerCleaner;

public partial class App : Application
{
    [DllImport("dwmapi.dll", PreserveSig = true)]
    private static extern int DwmSetWindowAttribute(IntPtr hwnd, int attr, ref int value, int size);

    [DllImport("kernel32.dll")]
    private static extern bool AttachConsole(int dwProcessId);

    private const int ATTACH_PARENT_PROCESS = -1;

    protected override async void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        // CLI mode: /d (delete), /m (move to saved location), /m <path> (move to path)
        if (e.Args.Length > 0)
        {
            await RunCliAsync(e.Args);
            return;
        }

        DispatcherUnhandledException += (_, args) =>
        {
            MessageBox.Show(
                $"An unexpected error occurred:\n\n{args.Exception.Message}",
                "InstallerClean", MessageBoxButton.OK, MessageBoxImage.Error);
            args.Handled = true;
        };

        // Force dark titlebar and app icon on all windows
        var appIcon = new BitmapImage(new Uri("pack://application:,,,/Assets/splash-icon.png"));
        EventManager.RegisterClassHandler(typeof(Window), Window.LoadedEvent,
            new RoutedEventHandler((s, _) =>
            {
                if (s is Window w)
                {
                    var hwnd = new WindowInteropHelper(w).Handle;
                    int value = 1;
                    DwmSetWindowAttribute(hwnd, 20, ref value, sizeof(int));
                    w.Icon = appIcon;
                }
            }));

        // Show splash immediately and force a render so it paints before scan work begins
        var splash = new SplashWindow();
        splash.Show();
        await Dispatcher.InvokeAsync(() => { }, System.Windows.Threading.DispatcherPriority.Render);

        try
        {
            // Step 1: show immediately while services are constructed
            splash.UpdateStep("Step 1/5: Initialising...", 10);

            var settingsService = new SettingsService();
            var queryService = new InstallerQueryService();
            var scanService = new FileSystemScanService(queryService);
            var moveService = new MoveFilesService();
            var deleteService = new DeleteFilesService();
            var exclusionService = new ExclusionService();
            var rebootService = new PendingRebootService();
            var msiInfoService = new MsiFileInfoService();

            var viewModel = new MainViewModel(
                scanService, moveService, deleteService,
                exclusionService, settingsService, rebootService, msiInfoService);

            // Step 2: the actual scan (this is where the time is spent)
            splash.UpdateStep("Step 2/5: Enumerating installed products...", 20);
            var scanTask = viewModel.ScanWithProgressAsync(null);
            // Ensure step 2 shows for at least 400ms even on very fast machines
            await Task.WhenAll(scanTask, Task.Delay(400));

            // Steps 3–5: post-scan, blaze through visibly
            splash.UpdateStep("Step 3/5: Enumerating patches...", 50);
            await Task.Delay(400);

            splash.UpdateStep("Step 4/5: Finding installation files...", 70);
            await Task.Delay(400);

            splash.UpdateStep("Step 5/5: Calculating results...", 90);
            await Task.Delay(400);

            var window = new MainWindow(viewModel);
            Application.Current.MainWindow = window;
            window.Show();
            splash.Close();

            // Offer Start Menu shortcut on first launch
            var settings = settingsService.Load();
            if (!settings.ShortcutOffered)
            {
                settings.ShortcutOffered = true;
                settingsService.Save(settings);

                var shortcutDialog = new ShortcutOfferWindow { Owner = window };
                if (shortcutDialog.ShowDialog() == true)
                {
                    Services.ShortcutService.CreateStartMenuShortcut();
                    if (shortcutDialog.CreateDesktopShortcut)
                        Services.ShortcutService.CreateDesktopShortcut();
                }
            }
        }
        catch (UnauthorizedAccessException)
        {
            splash.Close();
            MessageBox.Show(
                "This app requires administrator privileges.\n\nPlease right-click and choose 'Run as administrator'.",
                "Administrator rights required",
                MessageBoxButton.OK,
                MessageBoxImage.Warning);
            Shutdown();
        }
        catch (Exception ex)
        {
            splash.Close();
            MessageBox.Show(
                $"Failed to start: {ex.Message}",
                "Startup error",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
            Shutdown();
        }
    }

    private async Task RunCliAsync(string[] args)
    {
        AttachConsole(ATTACH_PARENT_PROCESS);

        var arg = args[0].ToLowerInvariant();
        if (arg is not "/d" and not "/m" and not "--help" and not "/?" and not "-h")
        {
            Console.WriteLine("InstallerClean — clean orphaned files from C:\\Windows\\Installer");
            Console.WriteLine();
            Console.WriteLine("Usage:");
            Console.WriteLine("  InstallerClean.exe          Launch the GUI");
            Console.WriteLine("  InstallerClean.exe /d       Delete orphaned files (Recycle Bin)");
            Console.WriteLine("  InstallerClean.exe /m       Move to saved default location");
            Console.WriteLine("  InstallerClean.exe /m PATH  Move to specified path");
            Console.WriteLine();
            Shutdown();
            return;
        }

        if (arg is "--help" or "/?" or "-h")
        {
            Console.WriteLine("InstallerClean — clean orphaned files from C:\\Windows\\Installer");
            Console.WriteLine();
            Console.WriteLine("Usage:");
            Console.WriteLine("  InstallerClean.exe          Launch the GUI");
            Console.WriteLine("  InstallerClean.exe /d       Delete orphaned files (Recycle Bin)");
            Console.WriteLine("  InstallerClean.exe /m       Move to saved default location");
            Console.WriteLine("  InstallerClean.exe /m PATH  Move to specified path");
            Console.WriteLine();
            Shutdown();
            return;
        }

        try
        {
            var settingsService = new SettingsService();
            var settings = settingsService.Load();
            var queryService = new InstallerQueryService();
            var scanService = new FileSystemScanService(queryService);
            var exclusionService = new ExclusionService();
            var msiInfoService = new MsiFileInfoService();

            Console.WriteLine("Scanning C:\\Windows\\Installer...");
            var scanResult = await scanService.ScanAsync();
            var filtered = exclusionService.ApplyFilters(
                scanResult.OrphanedFiles, settings.ExclusionFilters, msiInfoService);

            var orphanCount = filtered.Actionable.Count;
            var orphanSize = DisplayHelpers.FormatSize(filtered.Actionable.Sum(f => f.SizeBytes));
            Console.WriteLine($"Found {orphanCount} orphaned {DisplayHelpers.Pluralise(orphanCount, "file", "files")} ({orphanSize}).");

            if (orphanCount == 0)
            {
                Console.WriteLine("Nothing to do.");
                Shutdown(0);
                return;
            }

            var filePaths = filtered.Actionable.Select(f => f.FullPath).ToList();

            if (arg == "/d")
            {
                var deleteService = new DeleteFilesService();
                Console.WriteLine($"Deleting {orphanCount} files...");
                var result = await deleteService.DeleteFilesAsync(filePaths, null, CancellationToken.None);
                Console.WriteLine($"Deleted {result.DeletedCount} {DisplayHelpers.Pluralise(result.DeletedCount, "file", "files")}.");
                if (result.Errors.Count > 0)
                {
                    Console.WriteLine($"{result.Errors.Count} {DisplayHelpers.Pluralise(result.Errors.Count, "error", "errors")}:");
                    foreach (var err in result.Errors)
                        Console.WriteLine($"  {err}");
                }
                Shutdown(result.Errors.Count > 0 ? 1 : 0);
            }
            else if (arg == "/m")
            {
                var dest = args.Length > 1 ? args[1] : settings.MoveDestination;
                if (string.IsNullOrWhiteSpace(dest))
                {
                    Console.WriteLine("Error: no move destination specified. Use /m PATH or set a default in the GUI.");
                    Shutdown(1);
                    return;
                }

                var moveService = new MoveFilesService();
                Console.WriteLine($"Moving {orphanCount} files to {dest}...");
                var result = await moveService.MoveFilesAsync(filePaths, dest, null, CancellationToken.None);
                Console.WriteLine($"Moved {result.MovedCount} {DisplayHelpers.Pluralise(result.MovedCount, "file", "files")}.");
                if (result.Errors.Count > 0)
                {
                    Console.WriteLine($"{result.Errors.Count} {DisplayHelpers.Pluralise(result.Errors.Count, "error", "errors")}:");
                    foreach (var err in result.Errors)
                        Console.WriteLine($"  {err}");
                }
                Shutdown(result.Errors.Count > 0 ? 1 : 0);
            }
        }
        catch (UnauthorizedAccessException)
        {
            Console.WriteLine("Error: administrator privileges required. Run from an elevated command prompt.");
            Shutdown(1);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error: {ex.Message}");
            Shutdown(1);
        }
    }
}
