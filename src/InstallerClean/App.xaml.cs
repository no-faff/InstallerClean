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

/// <summary>
/// WPF application entry point. Builds the DI container, takes the
/// single-instance mutex, runs the splash-driven startup scan, hands
/// the resolved <see cref="ViewModels.MainViewModel"/> to a freshly
/// constructed <see cref="MainWindow"/>, and registers the global
/// last-resort exception handlers so any unhandled crash lands in the
/// user's <see cref="Helpers.CrashLog"/> file before the process exits.
/// </summary>
public partial class App : Application
{
    private static Mutex? _singleInstanceMutex;
    private static bool _holdsSingleInstanceMutex;
    private static ServiceProvider? _services;
    // Reentry guard: MessageBox.Show pumps messages, so a queued
    // exception could fire DispatcherUnhandledException recursively.
    private static bool _handlingUnhandledException;

    /// <remarks>
    /// <c>async void</c> is the WPF override contract; sync and post-
    /// await throws both reach the catch blocks below.
    /// </remarks>
    protected override async void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        // Single-instance pattern: open the mutex without taking
        // ownership, acquire via WaitOne(0), release explicitly in
        // OnExit. A process that crashes mid-run hands the mutex to
        // the next instance via AbandonedMutexException.
        _singleInstanceMutex = new Mutex(initiallyOwned: false, @"Global\InstallerClean_SingleInstance");
        try
        {
            _holdsSingleInstanceMutex = _singleInstanceMutex.WaitOne(TimeSpan.Zero);
        }
        catch (AbandonedMutexException)
        {
            _holdsSingleInstanceMutex = true;
        }
        if (!_holdsSingleInstanceMutex)
        {
            MessageBox.Show(
                Strings.Startup_AlreadyRunningBody,
                Strings.Startup_AlreadyRunningTitle, MessageBoxButton.OK, MessageBoxImage.Information);
            Shutdown();
            return;
        }

        DispatcherUnhandledException += (_, args) =>
        {
            // On recursive entry (see _handlingUnhandledException), log
            // silently and bail to avoid stacked dialogs.
            if (_handlingUnhandledException)
            {
                CrashLog.Write(args.Exception);
                args.Handled = true;
                return;
            }
            _handlingUnhandledException = true;
            try
            {
                var crash = CrashLog.TryWrite(args.Exception);
                var typeName = args.Exception.GetType().Name;
                // Type name only; ex.Message can carry cross-profile paths
                // under elevation.
                var body = crash.Written
                    ? string.Format(Strings.Startup_UnhandledBody, typeName, crash.Path)
                    : string.Format(Strings.Startup_UnhandledBody_NoLog, typeName);
                MessageBox.Show(body,
                    Strings.Startup_UnhandledTitle, MessageBoxButton.OK, MessageBoxImage.Error);
                args.Handled = true;
                Shutdown(1);
            }
            finally
            {
                _handlingUnhandledException = false;
            }
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
            // Freeze so the same instance can be assigned to many windows
            // safely without a per-window copy.
            appIcon.Freeze();
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
            // view-model graph from it. OnExit disposes the container
            // which in turn disposes any IDisposable singletons the
            // graph holds (today MainViewModel, ChromeViewModel and
            // CleanupViewModel, which need to unhook PropertyChanged
            // subscriptions from ScanViewModel for clean teardown).
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
            var crash = CrashLog.TryWrite(ex);
            var typeName = ex.GetType().Name;
            var body = crash.Written
                ? string.Format(Strings.Startup_FailedToStart, typeName, crash.Path)
                : string.Format(Strings.Startup_FailedToStart_NoLog, typeName);
            MessageBox.Show(body,
                Strings.Startup_ErrorTitle,
                MessageBoxButton.OK,
                MessageBoxImage.Error);
            Shutdown();
        }
    }

    protected override void OnExit(ExitEventArgs e)
    {
        _services?.Dispose();
        // Release-before-Dispose: a clean release lets the next instance
        // skip the AbandonedMutexException path. OnExit runs on the
        // dispatcher thread (same thread that took the mutex in
        // OnStartup), so ReleaseMutex can't throw ApplicationException.
        if (_holdsSingleInstanceMutex)
            _singleInstanceMutex?.ReleaseMutex();
        _singleInstanceMutex?.Dispose();
        base.OnExit(e);
    }
}
