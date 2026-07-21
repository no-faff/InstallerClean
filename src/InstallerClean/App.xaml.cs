using System.Diagnostics;
using System.Globalization;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Markup;
using System.Windows.Media.Imaging;
using Microsoft.Extensions.DependencyInjection;
using InstallerClean.Helpers;
using InstallerClean.Interop.Native;
using InstallerClean.Models;
using InstallerClean.Resources;
using InstallerClean.Services;
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
    // Reentry guard: a modal dialog pumps messages, so a queued
    // exception could fire DispatcherUnhandledException recursively.
    private static bool _handlingUnhandledException;

    // The six Type.* size tokens that scale with the OS text-size
    // setting. Type.FontFamily is deliberately absent: it is not a size.
    private static readonly string[] TypeSizeTokenKeys =
    {
        "Type.Display", "Type.Heading.Lg", "Type.Heading.Md",
        "Type.Heading.Sm", "Type.Body", "Type.Caption",
    };

    // Unscaled Type.* sizes captured once from the merged theme so
    // Tokens.xaml stays the single definition of the base sizes and every
    // re-scale derives from them, not from a previously scaled value.
    private readonly Dictionary<string, double> _baseTypeSizes = new();

    /// <remarks>
    /// <c>async void</c> is the WPF override contract; sync and post-
    /// await throws both reach the catch blocks below.
    /// </remarks>
    protected override async void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        // Apply the saved UI-language preference before anything reads a
        // resx string: the "already running" message below, the splash, and
        // every window. SettingsService has a no-dependency constructor, so
        // this runs without the DI container (not built until later). Resolve
        // returns null for Automatic or an unsupported value, leaving the OS
        // culture in place. Both UICulture (which strings) and Culture (number
        // formatting) are set, so a deliberate pick reads fully in that
        // language, file sizes ("3,2 GB") included.
        var languagePreference = LanguagePreference.Resolve(new SettingsService().Load().Language);
        if (languagePreference is not null)
        {
            // Localisation.Set is the load-bearing one: the resx lookups and the
            // size formatting read it directly, so the chosen language holds for
            // every window, not just the ones built during startup. A culture set
            // only on the thread does not survive the dispatcher's later
            // callbacks, so a window opened from a click would otherwise fall back
            // to the OS language. The thread/default-thread cultures and the
            // FrameworkElement.Language default cover other culture-dependent
            // framework code and XAML number bindings.
            Localisation.Set(languagePreference, languagePreference);
            CultureInfo.CurrentUICulture = languagePreference;
            CultureInfo.CurrentCulture = languagePreference;
            CultureInfo.DefaultThreadCurrentUICulture = languagePreference;
            CultureInfo.DefaultThreadCurrentCulture = languagePreference;
            FrameworkElement.LanguageProperty.OverrideMetadata(
                typeof(FrameworkElement),
                new FrameworkPropertyMetadata(
                    XmlLanguage.GetLanguage(languagePreference.IetfLanguageTag)));
        }

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
            // Another instance holds the mutex. If it is the GUI, bring its
            // window to the front, which is what clicking the icon of a running
            // app is supposed to do, and say nothing: telling the user it is
            // already running and then leaving them to find a window that may be
            // minimised or behind the browser is worse than useless.
            //
            // The dialog stays for the case that has no window to raise: the CLI
            // holds the same mutex (deliberately, so the two binaries never race
            // on C:\Windows\Installer) and never creates the activation event, so
            // an unattended /d or /m run reports itself here rather than
            // swallowing the click in silence.
            if (!TryRaiseRunningInstance())
                MessageDialog.Show(
                    Strings.Startup_AlreadyRunningBody,
                    Strings.Startup_AlreadyRunningTitle, MessageKind.Information);
            Shutdown();
            return;
        }

        StartActivationListener();

        DispatcherUnhandledException += (_, args) =>
        {
            // Recursive entry: log silently and bail. A nested handler
            // call would stack a second dialog over the first.
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
                // The crash log is written first, so a themed window that cannot
                // itself be built (a broken theme resource is one of the things
                // that reaches this handler) costs the user only the dialog's
                // paint: MessageDialog falls back to the stock box, which needs
                // none of the app's own resources.
                MessageDialog.Show(body, Strings.Startup_UnhandledTitle, MessageKind.Error);
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
            // Scale the Type.* font tokens before any window is shown so
            // text honours the OS "Make text bigger" setting from first
            // paint, and tracks the slider if it moves at runtime.
            ApplyTextScaling();

            // Title-bar Window.Icon assignment in the class handler
            // below degrades to WPF's default icon on a pack-URI load
            // failure (resource renamed, embed step broken). XAML
            // Image consumers in the windows resolve the same URI and
            // fall back to blank via WPF's own loader.
            BitmapImage? appIcon = null;
            try
            {
                appIcon = new BitmapImage(new Uri("pack://application:,,,/Assets/splash-icon.png"));
                // Freeze so the same instance can be assigned to many
                // windows safely without a per-window copy.
                appIcon.Freeze();
            }
            catch (Exception iconEx)
            {
                CrashLog.TryWrite(iconEx);
            }

            EventManager.RegisterClassHandler(typeof(Window), Window.LoadedEvent,
                new RoutedEventHandler((s, _) =>
                {
                    if (s is Window w)
                    {
                        var hwnd = new WindowInteropHelper(w).Handle;
                        int value = 1;
                        Dwmapi.DwmSetWindowAttribute(hwnd, Dwmapi.DWMWA_USE_IMMERSIVE_DARK_MODE, ref value, sizeof(int));
                        if (appIcon is not null)
                            w.Icon = appIcon;
                    }
                }));

            // WPF's default TextBox handles double-click (select word) but
            // not triple-click. A class handler at the preview tunnel adds
            // triple-click = select all uniformly across every TextBox in
            // the app, matching the Windows convention for editable and
            // read-only text controls.
            EventManager.RegisterClassHandler(typeof(TextBox), TextBox.PreviewMouseLeftButtonDownEvent,
                new MouseButtonEventHandler((s, e) =>
                {
                    if (e.ClickCount == 3 && s is TextBox tb)
                    {
                        tb.SelectAll();
                        e.Handled = true;
                    }
                }));

            splash = new SplashWindow();
            splash.Show();

            splash.UpdateStep(Strings.Status_Scanning, 10);

            // Single container, single resolve. OnExit disposes the
            // container; MainViewModel, ChromeViewModel and CleanupViewModel
            // implement IDisposable so their ScanViewModel PropertyChanged
            // subscriptions unhook on teardown.
            _services = Composition.BuildServiceProvider();
            var viewModel = _services.GetRequiredService<MainViewModel>();

            // Started here rather than after the window shows, and before the
            // startup scan is awaited rather than after it, because a typical
            // session is over in seconds: a check queued behind the scan
            // routinely resolves after the user has closed the window. It runs
            // beside the scan instead, so the status line is usually already
            // painted at first paint. Fire-and-forget by contract (it swallows
            // every outcome, exceptions included), so it cannot fail the splash
            // path or delay the window; the AutoUpdateCheck setting is read
            // inside it, so an opted-out install opens no socket.
            viewModel.Chrome.StartAutomaticUpdateCheck();

            using var startupCts = new CancellationTokenSource();
            splash.CancelRequested += (_, _) => startupCts.Cancel();

            var splashProgress = new Progress<ScanProgressUpdate>(splash.OnScanProgress);
            var cancelled = false;
            try
            {
                var scanTask = viewModel.Scan.ScanWithProgressAsync(splashProgress, startupCts.Token);
                await Task.WhenAll(scanTask, Task.Delay(800, startupCts.Token));
            }
            catch (OperationCanceledException)
            {
                // Cancelled startup scan falls through to the main
                // window in its empty pre-scan state. The user clicked
                // Cancel intentionally; a silent process exit at this
                // point is indistinguishable from a crash to the user.
                cancelled = true;
            }

            if (!cancelled)
            {
                splash.UpdateStep(Strings.Status_Done, 100);
                await Task.Delay(200);
            }

            var window = new MainWindow(viewModel);
            Application.Current.MainWindow = window;
            window.Show();
            splash.Close();
        }
        catch (UnauthorizedAccessException)
        {
            splash?.Close();
            MessageDialog.Show(
                Strings.Error_AdminRequiredBody,
                Strings.Error_AdminRequiredTitle,
                MessageKind.Warning);
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
            MessageDialog.Show(body, Strings.Startup_ErrorTitle, MessageKind.Error);
            Shutdown();
        }
    }

    /// <summary>
    /// The event a second launch signals to say "you are the one with the
    /// window, come to the front". Only the GUI creates it, so its absence is
    /// how the second launch knows the mutex holder is the CLI.
    ///
    /// <c>Local\</c>, the per-session namespace, while the single-instance mutex
    /// next door stays <c>Global\</c>. The two want opposite scopes because they
    /// answer opposite questions. The mutex asks whether any InstallerClean
    /// anywhere on the machine is touching C:\Windows\Installer, which is one
    /// folder shared by every session, so it must span them. This event's whole
    /// job is to raise a window in front of the person who just double-clicked
    /// the icon, and a window belongs to one desktop. Signalled across sessions
    /// it raises the window on somebody else's desktop AND suppresses the
    /// already-running message on this one, so the person who clicked sees the
    /// icon, the UAC prompt, then nothing at all. Under fast user switching, a
    /// terminal server or an RDP reconnect that is the ordinary case, not an
    /// exotic one. Scoped to the session, a launch from another one finds no
    /// event, falls back to the dialog, and is told what is actually true.
    /// Same-session relaunch is untouched, elevation included: the session
    /// namespace does not change across the elevation boundary.
    /// </summary>
    private const string ActivationEventName = @"Local\InstallerClean_Activate";

    private static EventWaitHandle? _activationSignal;

    /// <summary>
    /// Signals the running GUI instance to raise its window. Returns false when
    /// there is no GUI to raise (the CLI holds the mutex, or the GUI is between
    /// taking it and creating the event), in which case the caller shows the
    /// already-running message instead.
    /// </summary>
    private static bool TryRaiseRunningInstance()
    {
        try
        {
            if (!EventWaitHandle.TryOpenExisting(ActivationEventName, out var signal))
                return false;

            using (signal)
            {
                // Foreground rights belong to the process the user last acted on,
                // which is this one: it exists because they double-clicked the
                // icon. Handing them over is what lets the other process's
                // Activate() actually raise the window rather than flash its
                // taskbar button. Failure is not fatal, so the return is ignored:
                // the flash is the honest degradation.
                User32.AllowSetForegroundWindow(User32.ASFW_ANY);
                signal.Set();
            }
            return true;
        }
        catch (Exception ex)
        {
            // A denied open (a squatted name, an event created by a process whose
            // DACL keeps this one out) is the same outcome as no event at all: fall
            // back to the message. Nothing here can raise a window, so nothing here
            // is worth failing the launch over.
            CrashLog.TryWrite(ex);
            return false;
        }
    }

    /// <summary>
    /// Creates the activation event and waits on it for the life of the process,
    /// raising the main window whenever a second launch signals.
    ///
    /// The signal carries no data and grants no capability: any process on the
    /// machine can open this event and set it, and the most it can achieve is
    /// bringing InstallerClean's window to the foreground, which is what its own
    /// icon does anyway. The event handle is deliberately never disposed: the
    /// listener is parked inside WaitOne, and disposing the handle under it throws
    /// on a background thread, which takes the process down (the same trap
    /// RecycleEngine's bounded join has to avoid). A background thread and its
    /// handle both die with the process.
    /// </summary>
    private static void StartActivationListener()
    {
        try
        {
            _activationSignal = new EventWaitHandle(
                initialState: false, EventResetMode.AutoReset, ActivationEventName);
        }
        catch (Exception ex)
        {
            // Without the event, a second launch falls back to the message box,
            // which is a degraded outcome, not a broken one. Not worth failing
            // a start-up over.
            CrashLog.TryWrite(ex);
            return;
        }

        var listener = new Thread(ActivationListenerLoop)
        {
            IsBackground = true,
            Name = "InstallerClean activation listener",
        };
        listener.Start();
    }

    private static void ActivationListenerLoop()
    {
        var signal = _activationSignal;
        if (signal is null) return;

        while (true)
        {
            try
            {
                signal.WaitOne();
            }
            catch (Exception ex)
            {
                // Nothing disposes the handle, so this should not happen; if it
                // ever does, stop waiting rather than spin, and leave a trail.
                CrashLog.TryWrite(ex);
                return;
            }

            var dispatcher = Current?.Dispatcher;
            if (dispatcher is null || dispatcher.HasShutdownStarted) return;
            dispatcher.BeginInvoke(RaiseMainWindow);
        }
    }

    private static void RaiseMainWindow()
    {
        // Null while the splash still owns the screen: the startup scan is running
        // and the window is seconds away, so it will show itself. The second
        // instance has already exited, and nothing is owed to it.
        if (Current?.MainWindow is not { } window) return;

        if (window.WindowState == WindowState.Minimized)
            window.WindowState = WindowState.Normal;
        window.Activate();
    }

    /// <summary>
    /// Scales the Type.* font-size tokens by the OS text-size factor and
    /// keeps them in step with runtime changes to that setting. The XAML
    /// consumes the tokens via DynamicResource, so reassigning a token
    /// here resizes live text.
    /// </summary>
    private void ApplyTextScaling()
    {
        // Capture the unscaled sizes once, before the first override is
        // written, so re-scaling always derives from the theme definition
        // rather than a value already multiplied by an earlier factor.
        if (_baseTypeSizes.Count == 0)
            foreach (var key in TypeSizeTokenKeys)
                if (Resources[key] is double baseSize)
                    _baseTypeSizes[key] = baseSize;

        ScaleTypeTokens();

        AccessibilitySettings.Current.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName is null or nameof(AccessibilitySettings.TextScaleFactor))
                ScaleTypeTokens();
        };
    }

    private void ScaleTypeTokens()
    {
        var factor = AccessibilitySettings.Current.TextScaleFactor;
        foreach (var (key, baseSize) in _baseTypeSizes)
            Resources[key] = baseSize * factor;
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

    /// <summary>
    /// Relaunches the app to apply a new UI language. Disposes the container
    /// and releases the single-instance mutex BEFORE starting the child.
    ///
    /// The container first, because disposing it is what settles this process's
    /// last writes to settings.json: MainViewModel.Dispose flushes a
    /// MoveDestination edit still inside its 400 ms debounce (see
    /// CleanupViewModel.Dispose, which names the language switch as one of the
    /// paths it exists for) and waits out the result-log lifetime-lock write.
    /// The child reads that same file twice early, in OnStartup for the
    /// language and in CleanupViewModel's constructor for the destination.
    /// Leaving the disposal to OnExit put both writes after Process.Start, a
    /// race the parent happened to win only because the child has process
    /// creation, runtime start, DI build and a splash-driven scan to get
    /// through first. The lifetime-lock wait is bounded at five seconds and now
    /// sits before the launch rather than beside it, which is the right side of
    /// that trade: the bound is only reached by a disk that has stopped
    /// answering, and a child that reads a stale file gets it wrong every time.
    ///
    /// Then the mutex: the child takes the
    /// same Global\InstallerClean_SingleInstance mutex on startup, and a
    /// still-held mutex would send it down the "already running" exit, so the
    /// release order is load-bearing. ReleaseMutex must run on the thread that
    /// acquired it (Win32 owner-thread rule); OnStartup acquired it on the
    /// dispatcher thread and this runs on the dispatcher thread (a button
    /// click), so the release is legal here. The already-elevated process
    /// launches the admin-manifest child without a second UAC prompt.
    /// </summary>
    public void RelaunchForLanguageChange()
    {
        // Nulled so OnExit's own _services?.Dispose() is a no-op rather than a
        // second teardown; the flush obligation stays in the one place that has
        // always held it, MainViewModel.Dispose.
        _services?.Dispose();
        _services = null;

        if (_holdsSingleInstanceMutex)
        {
            _singleInstanceMutex?.ReleaseMutex();
            _holdsSingleInstanceMutex = false;
        }

        var exePath = Environment.ProcessPath;
        if (exePath is not null)
            Process.Start(new ProcessStartInfo(exePath) { UseShellExecute = true });

        Shutdown();
    }
}
