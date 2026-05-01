using System.Threading;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media.Imaging;
using Microsoft.Extensions.DependencyInjection;
using InstallerClean.Helpers;
using InstallerClean.Interop.Native;
using InstallerClean.Resources;
using InstallerClean.ViewModels;

namespace InstallerClean;

public partial class App : Application
{
    private static Mutex? _singleInstanceMutex;
    private static ServiceProvider? _services;

    protected override async void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        // The WPF binary handles the GUI only. CLI args are owned by
        // installerclean-cli.exe (a real console-subsystem .NET exe
        // that ships in the same folder). If a user double-clicks the
        // GUI exe with stray args, ignore them.
        _singleInstanceMutex = new Mutex(true, @"Global\InstallerClean_SingleInstance", out bool isFirstInstance);
        if (!isFirstInstance)
        {
            MessageBox.Show(
                Strings.Startup_AlreadyRunningBody,
                Strings.Startup_AlreadyRunningTitle, MessageBoxButton.OK, MessageBoxImage.Information);
            Shutdown();
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

            // Build the service container once and resolve the entire
            // view-model graph from it. The container is disposed in
            // OnExit; no registered service is currently IDisposable,
            // but disposing the container is forward-looking against
            // a future service that does need cleanup.
            _services = Composition.BuildServiceProvider();
            var viewModel = _services.GetRequiredService<MainViewModel>();

            using var startupCts = new CancellationTokenSource();
            splash.CancelRequested += (_, _) => startupCts.Cancel();

            var splashProgress = new Progress<string>(splash.OnScanProgress);
            try
            {
                var scanTask = viewModel.Scan.ScanWithProgressAsync(splashProgress, startupCts.Token);
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
        _services?.Dispose();
        _singleInstanceMutex?.Dispose();
        base.OnExit(e);
    }
}
