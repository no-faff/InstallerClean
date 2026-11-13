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
    /// <summary>
    /// Runs as Main does, before Application's constructor: checks for Windows 10 version
    /// 1607 and .NET Framework 4.8, writes what the module initializer kept for the crash
    /// log, then pins the AppContext switches. Application's constructor creates the
    /// Dispatcher, which is WPF's first read of the accessibility switches, and by this point
    /// the domain knows it targets .NET Framework 4.8, so the runtime's own defaults are those
    /// of 4.8.
    /// </summary>
    /// <remarks>
    /// On a PC older than Windows 10 version 1607 the window says which Windows it needs,
    /// and on a PC without .NET Framework 4.8 it says what to install, in a plain message
    /// box, and ends, before Application's constructor and before anything scans.
    /// The portable carries no config naming the runtime, so .NET Framework starts it on any
    /// 4.x. The runtime compiles a method whole the first time it runs, and a member the PC's
    /// .NET Framework lacks fails that compile, so the checks run first, in a method of their
    /// own, ahead of Application's constructor and every method the window's start compiles.
    /// The two calls after it are never compiled into this method, so nothing they name is
    /// compiled before the checks have answered. The message is in the language picked in the
    /// app, where one is, and otherwise in the language Windows displays.
    /// </remarks>
    static App()
    {
        EndOnAPcThatCannotRunIt();
        SystemLibraries.WriteWhatTheInitializerKept();
        FrameworkSwitches.Pin();
    }

    /// <summary>
    /// Where the PC runs Windows older than Windows 10 version 1607, or does not have .NET
    /// Framework 4.8, shows the message saying so and ends the process with exit code 1.
    /// Windows is checked first, as 1507 and 1511 cannot take .NET Framework 4.8.
    /// </summary>
    [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
    private static void EndOnAPcThatCannotRunIt()
    {
        // A throw out of a type initializer reaches no handler the app has set, so a check or
        // a box that fails lets the start go on, where the app's own handlers catch what
        // follows, unless it failed to load what it needs (StartChecks.IsLoadFailure).
        try
        {
            var refusal = StartChecks.OfThisPc();
            if (refusal == StartRefusal.None) return;
            // In the language picked in the app where one is, as the window would have been. A
            // pick that cannot be read or set leaves the message in the language Windows displays.
            try
            {
                if (PickedLanguageAtStart.Read() is { } picked)
                    Localisation.SetPickedLanguage(picked);
            }
            catch (Exception)
            {
            }
            MessageBox.Show(StartChecks.Message(refusal, commandLine: false), Strings.FrameworkCheck.Window_Main_Title,
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
        catch (Exception ex) when (StartChecks.IsLoadFailure(ex))
        {
            // The PC's .NET Framework is older than 4.8, where the strings cannot be trusted
            // to read, so the message is in English. The start ends whether or not the box
            // shows.
            try
            {
                MessageBox.Show(NetFrameworkCheck.EnglishMessage(commandLine: false),
                    NetFrameworkCheck.NeutralText.WindowMainTitle, MessageBoxButton.OK, MessageBoxImage.Error);
            }
            catch (Exception)
            {
            }
        }
        catch (Exception)
        {
            return;
        }
        Environment.Exit(1);
    }

    private static InstanceLock? _instanceLock;
    // Set where an update that did not hand over could not take the mutex back.
    private static bool _instanceLockLost;
    private static ServiceProvider? _services;
    // Reentry guard: a modal dialog pumps messages, so a queued
    // exception could fire DispatcherUnhandledException recursively.
    private static bool _handlingUnhandledException;

    // The six Type.* size tokens that scale with the OS text-size
    // setting. Type.FontFamily is not among them: it is not a size, and
    // OnStartup writes it once, for the displayed language.
    private static readonly string[] TypeSizeTokenKeys =
    {
        "Type.Display", "Type.Heading.Lg", "Type.Heading.Md",
        "Type.Heading.Sm", "Type.Body", "Type.Caption",
    };

    // Unscaled Type.* sizes captured once from the merged theme so
    // Tokens.xaml stays the single definition of the base sizes and every
    // re-scale derives from them, not from a previously scaled value.
    private readonly Dictionary<string, double> _baseTypeSizes = new();

    // Set where the app closes the splash itself, so its Closed handler does not read the
    // close as a person asking the app to stop.
    private bool _splashClosedByApp;

    /// <remarks>
    /// <c>async void</c> is the WPF override contract; sync and post-
    /// await throws both reach the catch blocks below.
    /// </remarks>
    protected override async void OnStartup(StartupEventArgs e)
    {
        // No critical-error box for the whole session: a read reaching a drive with no disc or
        // card in it answers with an error rather than stopping on a box.
        StorageHelpers.TurnOffCriticalErrorBoxForTheProcess();

        base.OnStartup(e);

        // Until the main window is up, the app ends only where this method says so, with
        // the exit code it gives: a dialog or the splash closing starts no shutdown of its
        // own. A person closing the splash is the exception, handled where it is made.
        ShutdownMode = ShutdownMode.OnExplicitShutdown;

        // A newer version a copy downloaded, started to put itself at that copy's path,
        // does that and nothing else: no window, no scan, no setting read. Before the
        // language preference is applied, so the Application-log entry it can write is
        // built in English (MachineCulture). A request carrying a command, or one not
        // well formed, ends at once with nothing shown and no event set, and crash.log
        // says which of the two it was.
        switch (HandOverContract.ReadReplaceRequest(e.Args, out var replaceRequest))
        {
            case ReplaceRead.Asked when replaceRequest!.Command.Count == 0:
                ReplaceAndEnd(replaceRequest);
                return;
            case ReplaceRead.Asked:
                CrashLog.TryWrite(new ArgumentException(HandOverContract.ReplaceWithCommandRefusal));
                Shutdown(1);
                return;
            case ReplaceRead.Refused:
                CrashLog.TryWrite(new ArgumentException(HandOverContract.ReplaceNotWellFormedRefusal));
                Shutdown(1);
                return;
        }

        // The started event a copy updating itself passes, and the switch a replace that
        // left the older version in place starts it with. Both starts follow a copy that
        // has just closed, so both wait for the single-instance mutex below.
        var args = HandOverContract.TakeStartedEvent(e.Args, out var startedEvent);
        HandOverContract.TakeUpdateFailed(args, out var updateFailed);

        // Apply the saved UI-language preference before anything reads a
        // resx string: the "already running" message below, the splash, and
        // every window. SettingsService has a no-dependency constructor, so
        // this runs without the DI container (not built until later). Resolve
        // returns null for Automatic or an unsupported value, leaving the OS
        // culture in place. A pick sets the language the strings are in; numbers
        // keep the PC's regional format, and times and dates take it wherever it
        // is a format of the picked language (Localisation.SetPickedLanguage).
        var languagePreference = LanguagePreference.Resolve(new SettingsService().Load().Language);
        if (languagePreference is not null)
        {
            // Localisation is the load-bearing one: the resx lookups and the
            // number, time and date formatting read it directly, so the chosen
            // language holds for every window, not just the ones built during
            // startup. A culture set only on the thread does not survive the
            // dispatcher's later callbacks, so a window opened from a click would
            // otherwise fall back to the OS language. SetPickedLanguage reads the
            // regional format off the thread, so it comes before the lines below.
            // The thread/default-thread cultures cover other culture-dependent
            // framework code, and are given the same format culture, so the
            // framework formats as the app does.
            Localisation.SetPickedLanguage(languagePreference);
            CultureInfo.CurrentUICulture = languagePreference;
            CultureInfo.CurrentCulture = Localisation.FormatCulture;
            CultureInfo.DefaultThreadCurrentUICulture = languagePreference;
            CultureInfo.DefaultThreadCurrentCulture = Localisation.FormatCulture;
        }

        // Outside the branch above, because Automatic is the default and returns
        // no preference: inside it, a fresh install would never reach this and
        // every element would keep WPF's own default for the property, the
        // literal en-US (FrameworkElement registers LanguageProperty with
        // XmlLanguage.GetLanguage("en-US")). All three projects declare en-GB as
        // their neutral language and the app ships no en-US at all, so that
        // default tags every element with a language the app does not have,
        // whatever is on screen.
        //
        // What hangs off it is font fallback. A character the element's family
        // does not carry, such as a Chinese, Japanese or Korean product name on a
        // screen in another language, comes from a fallback font. The chain is
        // all readable in dotnet/wpf. This property becomes the run's CultureInfo
        // (DynamicPropertyReader.GetCultureInfo); the four-argument Typeface
        // constructor the framework uses puts #GLOBAL USER INTERFACE behind the
        // element's own family, so a character that family lacks reaches that
        // composite font; and CompositeFontFamily.GetTargetFamilyMap picks
        // between its FontFamilyMaps by exactly this tag.
        //
        // The composite font it picks from is the pack resource
        // fonts/globaluserinterface.compositefont inside PresentationCore.dll,
        // which is the URI FamilyCollection.SystemCompositeFonts builds, never the
        // same-named file in the Windows Fonts folder. It carries one map set per
        // Windows generation and CompositeFontParser takes the first the machine
        // satisfies; the Windows10RS5 and Windows10RS1 sets, which between them
        // cover every Windows the setup's MinVersion admits, agree on all of the
        // below. A tag matching no map takes the maps carrying no language at all,
        // and those lead with Yu Gothic UI, Meiryo UI and Meiryo over the Han
        // block, the kana and CJK punctuation block and the fullwidth forms:
        // Japanese faces. Under zh-Hans its Simplified Chinese maps apply instead,
        // leading with Microsoft YaHei UI, and under zh-Hant its Traditional
        // Chinese maps, leading with Microsoft JhengHei UI; Han unification makes
        // the Japanese and Chinese forms different characters rather than a
        // different style. Japanese reaches the same
        // faces either way, its maps being identical to the unlanguaged ones over
        // kana and the fullwidth block and leading with the same three over Han,
        // and Hangul has one map with no language on it at all, so what the tag
        // moves for Korean is Han and CJK punctuation: a product name rather than
        // one of the app's own strings.
        //
        // Active(), not the preference and not the OS culture raw. The preference
        // is null on Automatic. The OS culture would be wrong the other way: on a
        // Windows set to a language the app does not ship, Automatic displays the
        // neutral en-GB text, and tagging that English prose Swedish would be a
        // fresh instance of the same fault. Active() walks the parent chain the
        // string lookup walks and answers en-GB when nothing matches, so
        // the value is always one of the languages the app ships: all ASCII, all
        // inside RFC 3066, so none of them can reach
        // XmlLanguage.GetLanguage's throw.
        //
        // Line breaking does not move with the tag, which matters because the
        // word joiners in InstallerPathText rest on it; the reading that settles
        // that is on InstallerPathText itself.
        FrameworkElement.LanguageProperty.OverrideMetadata(
            typeof(FrameworkElement),
            new FrameworkPropertyMetadata(
                XmlLanguage.GetLanguage(SupportedLanguages.Active(Localisation.UiCulture))));

        // The displayed language's font family, written over Type.FontFamily
        // before anything is drawn, the first dialog below included. The read
        // takes Tokens.xaml's value, nothing having been written over it yet.
        // LanguageFonts hands a language it does not name that same theme family
        // back, so for those languages the entry written holds the value
        // Tokens.xaml already holds. A language change relaunches the app, so
        // the family is chosen once per run.
        //
        // Every use of the token must be a DynamicResource: a StaticResource
        // inside Components.xaml resolves against Tokens.xaml and never sees
        // this entry.
        LanguageFonts.Initialise((System.Windows.Media.FontFamily)Resources["Type.FontFamily"]);
        Resources["Type.FontFamily"] = LanguageFonts.For(SupportedLanguages.Active(Localisation.UiCulture));

        // A .exe.config beside the copy that says more than the build's own is a file
        // somebody else wrote, which on .NET Framework the runtime reads. A config beside
        // the setup's copy is not refused. Refused here, in the language the user chose and
        // before anything scans, and read here rather than in a module initializer, which
        // reaches System.IO and the registry. The replace request above runs first and
        // reads no section of the config: it moves files, starts a process and writes to
        // the Application log, and .NET Framework reads none of those from the config. A
        // network request, a trace or a section of the app's own would, so none of them
        // may join the replace without this check moving above it.
        if (PlantedConfig.RefusalMessage(Environment.ProcessPath, new RegistryReader()) is { } plantedConfig)
        {
            MessageDialog.Show(plantedConfig, Strings.Startup_ErrorTitle, MessageKind.Error);
            EndFailedStart(splash: null);
            return;
        }

        // A process without administrator rights is told so and closes, before
        // anything scans. The manifest asks Windows for the rights, and Windows can
        // still start the app without them, a compatibility setting being one way,
        // so the app asks its own token; AdministratorRights says what the scan
        // needs them for.
        //
        // After the language, so the message is in the one the user chose, and
        // before the single-instance mutex, so such a launch is refused the same way
        // whether or not another instance is running.
        //
        // Before the handlers below are hooked, so the check carries its own catch.
        // The check throws where Windows will not open or duplicate the token, or
        // answer the membership question, and then the app shows the dialog the
        // startup catch shows, with the crash log, and closes without scanning.
        bool administrator;
        try
        {
            administrator = AdministratorRights.Held();
        }
        catch (Exception ex)
        {
            ShowFailedToStart(ex);
            EndFailedStart(splash: null);
            return;
        }
        if (!administrator)
        {
            MessageDialog.Show(
                Strings.Startup_AdminRightsNeededBody,
                Strings.Startup_AdminRightsNeededTitle,
                MessageKind.Warning);
            EndFailedStart(splash: null);
            return;
        }

        // Single instance: the mutex is taken without waiting, or after a wait for a start
        // that follows a copy that has just closed (InstanceLockWait), and released
        // in OnExit. A process that ends without releasing it hands it to
        // the next instance, which InstanceLock counts as taken.
        //
        // Before the handlers below are hooked too, so it carries its own catch, as the
        // administrator check does: where Windows will not open the mutex, the app shows
        // the dialog saying it failed to start, with the crash log, and closes.
        bool tookInstanceLock;
        try
        {
            _instanceLock = new InstanceLock();
            // The wait dispatches nothing, so no message sent to this thread is handled
            // before the handlers below are hooked.
            tookInstanceLock = _instanceLock.TryTake(
                startedEvent is not null || updateFailed ? InstanceLockWait : TimeSpan.Zero);
        }
        catch (Exception ex)
        {
            ShowFailedToStart(ex);
            EndFailedStart(splash: null);
            return;
        }
        if (!tookInstanceLock)
        {
            // The start is decided, so the copy that started this one is told: the dialog
            // below waits for a click, and that copy cannot wait for one.
            HandOverContract.SignalStarted(startedEvent);

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
                CrashLog.TryWrite(args.Exception);
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
                CrashLog.TryWrite(ex);
        };

        TaskScheduler.UnobservedTaskException += (_, args) =>
        {
            CrashLog.TryWrite(args.Exception);
            args.SetObserved();
        };

        SplashWindow? splash = null;
        try
        {
            // Scale the Type.* font tokens before any window is shown so
            // text honours the OS "Make text bigger" setting from first
            // paint, and tracks the slider if it moves at runtime.
            ApplyTextScaling();

            // Before any window is shown, so no element can take focus without
            // being tracked. What it is for is on FocusRing itself.
            FocusRing.Track();

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
            // A person closing the splash, with Alt+F4, asks the app to stop.
            splash.Closed += (_, _) =>
            {
                if (!_splashClosedByApp) Shutdown();
            };
            splash.Show();

            splash.UpdateStep(Strings.Status_Scanning, ScanProgressFill.FloorPercent);

            // Single container, and the view model resolved once. OnExit disposes the
            // container; MainViewModel, ChromeViewModel and CleanupViewModel
            // implement IDisposable so their ScanViewModel PropertyChanged
            // subscriptions unhook on teardown.
            _services = Composition.BuildServiceProvider();
            var viewModel = _services.GetRequiredService<MainViewModel>();

            // The copy that started this one, where it updated itself, waits for this before
            // it ends: the splash is up and the view model built, so the libraries, the
            // resources and the services this build needs have all loaded. Nothing before
            // this point waits on anything slow. Where the event cannot be set, the app
            // closes before it starts anything, and that copy puts itself back.
            if (!HandOverContract.SignalStarted(startedEvent))
            {
                EndFailedStart(splash);
                return;
            }

            // What an update left behind, in the background: what a replace left beside this
            // exe, and the folder a newer setup ran from. After the signal, and only once this
            // version's own code is running, so a newer version that never starts never
            // deletes the older one kept beside it.
            _ = Task.Run(RemoveUpdateLeftoversAsync);

            // Started before the startup scan, so it runs beside it and has usually
            // answered by the time the scan ends. What the scan finds is held until it
            // has: where a newer version is available, the window opens with the card
            // saying so in front of the scan rather than with the scan. It swallows
            // every outcome, exceptions included, so it cannot fail the splash path;
            // the AutoUpdateCheck setting is read inside it, so an opted-out install
            // opens no socket.
            viewModel.HoldResultsForUpdateCheck();
            if (updateFailed) viewModel.Chrome.ShowUpdateFailedAtStart();
            var sinceCheckStart = Stopwatch.StartNew();
            viewModel.Chrome.StartAutomaticUpdateCheck();

            // Whether the PC has already had its first run, started before the scan so it
            // runs beside it. Where an earlier run shows and the PC-wide mark is not yet
            // set, the check sets it. Its task never faults and the singleton holds it,
            // so nothing here waits on it.
            _ = _services.GetRequiredService<IEarlierRunCheck>().ShowsAnEarlierRunAsync();

            // This account's report, where an earlier start left it waiting to go. It
            // waits for the check above before it reads settings, and runs in the
            // background.
            viewModel.Completion.StartSavedReportRetry();

            using var startupCts = new CancellationTokenSource();
            splash.CancelRequested += (_, _) => startupCts.Cancel();

            // The splash is held open for its own minimum however little the scan
            // costs, so that a scan finishing in a fraction of a second still
            // leaves a window somebody can read. The closing step below is eased
            // over the time the window has left, what remains of the minimum plus
            // the close, rather than over the bar's own step, so the last of the
            // fill arrives as the window goes instead of being cut off by it.
            var splashFloor = Durations.FromMilliseconds(800);
            var splashClose = Durations.FromMilliseconds(200);

            // Throttled, because the scan's ticker fires once per installed
            // program and many times while the files are counted, so its rate
            // belongs to the machine. Wrapped outside the Progress<T> rather
            // than inside it, so an update the interval drops is never posted
            // to the dispatcher at all.
            var splashProgress = new ThrottledScanProgress(
                new Progress<ScanProgressUpdate>(splash.OnScanProgress));
            var cancelled = false;
            var sinceScanStart = Stopwatch.StartNew();
            try
            {
                await viewModel.Scan.ScanWithProgressAsync(splashProgress, startupCts.Token);
            }
            catch (OperationCanceledException)
            {
                // Cancelled startup scan falls through to the main
                // window in its empty pre-scan state. The user clicked
                // Cancel intentionally; a silent process exit at this
                // point is indistinguishable from a crash to the user.
                cancelled = true;
            }

            // A cancelled startup scan has nothing to hold, so the window opens at once
            // and the notice at the top shows whenever the check finds a newer version.
            //
            // Otherwise, once the scan has finished, Cancel has nothing left to stop and
            // the splash takes it out of use. The splash stays up until the update check
            // has answered or given up, which its own timeout bounds, and only then says
            // "Ready". The wait here is held to that timeout and a second more, counted
            // from the check's start, so a check that never answered could not keep the
            // splash up.
            //
            // The card a startup scan that offered nothing puts up can then be waiting on
            // the window's start check, which on a PC's first launch can outlast the scan.
            // Building the window after it is settled means the card is either up as the
            // window is constructed, which replays it, or not coming; the wait is that
            // check's own bound at most. Where the window opens with the card saying a
            // newer version is available there is nothing to wait for: the scan's card
            // waits for Continue without updating, and the window waits on it then.
            if (cancelled)
            {
                viewModel.ShowStartupScan(mayOfferUpdate: false);
                await viewModel.ScanCardDecided;
            }
            else
            {
                splash.ShowScanEnded();
                var checkLeft = UpdateCheckService.RequestTimeout + UpdateCheckGrace - sinceCheckStart.Elapsed;
                if (checkLeft > TimeSpan.Zero)
                    await Task.WhenAny(viewModel.Chrome.AutomaticCheck, Task.Delay(checkLeft));
                viewModel.ShowStartupScan(mayOfferUpdate: !StartedAfterLanguageChange(e.Args));

                var floorLeft = splashFloor - sinceScanStart.Elapsed;
                var ease = (floorLeft > TimeSpan.Zero ? floorLeft : TimeSpan.Zero) + splashClose;
                splash.ShowReady(ease);
                await Task.WhenAll(
                    viewModel.Chrome.IsOfferingUpdate ? Task.CompletedTask : viewModel.ScanCardDecided,
                    Task.Delay(ease));
            }

            var window = new MainWindow(viewModel);
            Application.Current.MainWindow = window;
            window.Show();
            ShutdownMode = ShutdownMode.OnLastWindowClose;
            _splashClosedByApp = true;
            splash.Close();

            if (updateFailed) _ = SayUpdateFailedUnlessShownAsync(viewModel.Chrome, window, sinceCheckStart);
        }
        // Both arms show the dialog before EndFailedStart, and the order is
        // load-bearing: see EndFailedStart. Anything the composition build, the
        // view-model resolve or new MainWindow() throws lands here, XAML
        // resource-type mismatches among them, so it is the arm that has to
        // survive a broken app.
        catch (UnauthorizedAccessException)
        {
            MessageDialog.Show(
                Strings.Error_AdminRequiredBody,
                Strings.Error_AdminRequiredTitle,
                MessageKind.Warning);
            EndFailedStart(splash);
        }
        catch (Exception ex)
        {
            ShowFailedToStart(ex);
            EndFailedStart(splash);
        }
    }

    /// <summary>
    /// Puts this version at the path <paramref name="request"/> names once the copy there has
    /// ended, starts it there and waits for it to say it is running
    /// (<see cref="ExeReplacement.Run(ReplaceRequest, bool)"/>), and ends. Where the copy's
    /// own file is at the path afterwards, put back or never replaced, that copy is started
    /// with <see cref="HandOverContract.UpdateFailedSwitch"/>, so it opens saying the update
    /// couldn't be installed. Where the newer version is at the path, or nothing is, or the
    /// copy carries on, nothing is started. The exit code is 0 where the newer version is in
    /// place and started, 1 otherwise. Its own catch writes to crash.log, the app's handlers
    /// not being hooked this early.
    /// </summary>
    private void ReplaceAndEnd(ReplaceRequest request)
    {
        try
        {
            var outcome = ExeReplacement.Run(request, startAtPath: true);
            if (outcome is ReplaceOutcome.Refused or ReplaceOutcome.RolledBack or ReplaceOutcome.OldCopyNotSeenToEnd)
                ExeReplacement.StartWithUpdateFailed(request.ExePath);
            Shutdown(outcome == ReplaceOutcome.Replaced ? 0 : 1);
        }
        catch (Exception ex)
        {
            CrashLog.TryWrite(ex);
            Shutdown(1);
        }
    }

    /// <summary>
    /// Once the automatic check has answered or its time is up, shows a dialog saying the
    /// update couldn't be installed where no card or notice carries that line: where the
    /// check has not found the newer version, or found one its release rules this copy out
    /// of, which takes the line off. Not where <paramref name="window"/> has closed
    /// meanwhile. A failure is written to crash.log and goes no further.
    /// </summary>
    private static async Task SayUpdateFailedUnlessShownAsync(
        ChromeViewModel chrome, Window window, Stopwatch sinceCheckStart)
    {
        try
        {
            var left = UpdateCheckService.RequestTimeout + UpdateCheckGrace - sinceCheckStart.Elapsed;
            if (left > TimeSpan.Zero)
                await Task.WhenAny(chrome.AutomaticCheck, Task.Delay(left));
            if (!window.IsVisible || (chrome.HasNewerVersion && chrome.HasUpdateFailure)) return;
            MessageDialog.Show(Strings.UpdateNotice_InstallFailed, Strings.Window_Main_Title, MessageKind.Warning);
        }
        catch (Exception ex)
        {
            CrashLog.TryWrite(ex);
        }
    }

    /// <summary>
    /// How long a start following a copy that has just closed waits for the single-instance
    /// mutex: a copy that has just told this one to start, or put it back, may still be
    /// closing and holding it, and Windows hands the mutex to the waiting thread as that copy
    /// lets go of it.
    /// </summary>
    private static readonly TimeSpan InstanceLockWait = Durations.FromSeconds(15);

    /// <summary>
    /// Ends a start that failed, with exit code 1, and closes <paramref name="splash"/>
    /// where it is up.
    /// </summary>
    /// <remarks>
    /// Call it only once any dialog saying why the start failed has closed. The shutdown is
    /// queued as a dispatcher operation, and a modal opened after it pumps the dispatcher,
    /// runs the queued shutdown from inside its own frame, and the only diagnosis a user
    /// gets for a failed start flashes and vanishes. The splash's close is marked as the
    /// app's own before it happens, so the exit code is this one whichever comes first.
    /// </remarks>
    private void EndFailedStart(SplashWindow? splash)
    {
        _splashClosedByApp = true;
        Shutdown(1);
        splash?.Close();
    }

    /// <summary>
    /// Writes <paramref name="ex"/> to the crash log and shows the dialog saying the
    /// app failed to start, naming the exception's type and, where the log was
    /// written, its path. The caller closes the app. Type name only, for the reason
    /// the unhandled-exception handler gives.
    /// </summary>
    private static void ShowFailedToStart(Exception ex)
    {
        var crash = CrashLog.TryWrite(ex);
        var typeName = ex.GetType().Name;
        var body = crash.Written
            ? string.Format(Strings.Startup_FailedToStart, typeName, crash.Path)
            : string.Format(Strings.Startup_FailedToStart_NoLog, typeName);
        MessageDialog.Show(body, Strings.Startup_ErrorTitle, MessageKind.Error);
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
    /// on a background thread, which takes the process down. A background thread
    /// and its handle both die with the process.
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

    /// <summary>
    /// Runs on every close, an update's hand-over included: what closing settles, the move
    /// destination still being saved and the report's sends and saves still running, is
    /// settled here before the mutex goes, and the newer version an update started puts
    /// itself in place only once this process has ended.
    /// </summary>
    protected override void OnExit(ExitEventArgs e)
    {
        _services?.Dispose();
        // Release-before-Dispose: a clean release hands the next instance
        // a released mutex, not an abandoned one. OnExit runs on the
        // dispatcher thread (same thread that took the mutex in
        // OnStartup), so ReleaseMutex can't throw ApplicationException.
        _instanceLock?.Release();
        _instanceLock?.Dispose();
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
    /// paths it exists for) and waits, for no more than two seconds, for the
    /// report's sends and saves still running. The child reads that same file
    /// twice early, in OnStartup for the language and in CleanupViewModel's
    /// constructor for the destination, so the writes are settled before
    /// Process.Start rather than racing those reads. The bounded wait sitting
    /// before the launch is the right side of that trade: the bound is only
    /// reached by a disk or a network that has stopped answering, and a child
    /// that reads a stale file gets it wrong every time.
    ///
    /// Then the mutex: the child takes the
    /// same <see cref="InstanceLock"/> on startup, and a
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

        _instanceLock?.Release();

        var exePath = Environment.ProcessPath;
        if (exePath is not null)
            Process.Start(new ProcessStartInfo(exePath, LanguageChangedArgument) { UseShellExecute = true });

        Shutdown();
    }

    /// <summary>
    /// What the language relaunch passes the new process. That start carries on the
    /// session that changed the language, so it opens without the card saying a newer
    /// version is available, and the notice at the top of the main window shows instead
    /// where its check finds one. It changes nothing else.
    /// </summary>
    private const string LanguageChangedArgument = "/languagechanged";

    private static bool StartedAfterLanguageChange(string[] args) =>
        args.Any(a => string.Equals(a, LanguageChangedArgument, StringComparison.OrdinalIgnoreCase));

    /// <summary>
    /// How much longer than the update check's own timeout the startup waits for it
    /// before showing the scan anyway. The automatic check reads what a newer version's
    /// release needs inside the check's own timeout (ChromeViewModel's
    /// AutomaticCheckWindow), so a find is shown within this wait.
    /// </summary>
    private static readonly TimeSpan UpdateCheckGrace = Durations.FromSeconds(1);

    /// <summary>
    /// Hands over to the newer version <paramref name="prepared"/> holds and closes the
    /// app: the newer setup (<see cref="StartSetupAndClose"/>), or the newer version itself,
    /// started to put itself at this exe's path once the app has closed
    /// (<see cref="ReplaceAndClose"/>). Answers false where it could not be started, or did
    /// not say it would carry on, and the app then carries on as it was. Runs on the
    /// dispatcher thread, from Update now, the thread that took the mutex.
    /// </summary>
    public Task<bool> StartUpdateAndCloseAsync(PreparedUpdate prepared) =>
        Task.FromResult(prepared.Replaces is { } exePath
            ? ReplaceAndClose(prepared.FileToStart, exePath)
            : StartSetupAndClose(prepared.FileToStart, prepared.Arguments));

    /// <summary>
    /// The setup's copy: releases the single-instance mutex, starts the newer setup, which
    /// starts the app again once it has installed, and closes the app. The mutex goes
    /// first: the setup names it as its AppMutex and stops while the name can be opened,
    /// and this app's handle stays open until it closes, so a setup started from here
    /// waits for this app to close before that check. Where the setup cannot be started,
    /// the mutex is taken back where it is free. A start that fails is tried again through
    /// <see cref="ProcessStartRetry"/>, on the dispatcher thread with its processing held,
    /// so none of the window's own code runs while the mutex is released. The setup starts through CreateProcess rather than the shell,
    /// which can wait on a DDE conversation and pump messages; the app runs elevated, so
    /// the setup CreateProcess starts runs elevated too, without a prompt. While the tries
    /// and the waits between them run, the window takes no input and shows the update card
    /// as the download left it; then it closes, or the card says the update could not be
    /// installed.
    /// </summary>
    private bool StartSetupAndClose(string fileToStart, string arguments)
    {
        var released = _instanceLock?.Release() == true;
        var info = new ProcessStartInfo(fileToStart, arguments)
        {
            UseShellExecute = false,
            WorkingDirectory = PathText.GetDirectoryName(fileToStart) ?? string.Empty,
        };

        try
        {
            using (Dispatcher.DisableProcessing())
                ProcessStartRetry.Start(
                    info, _ => true, interval => Kernel32.Sleep((uint)interval.TotalMilliseconds)).Dispose();
        }
        catch (Exception ex)
        {
            CrashLog.TryWrite(ex);
            if (released) RetakeAfterHandOverFailed();
            return false;
        }

        Shutdown();
        return true;
    }

    /// <summary>
    /// Deletes what a replace left beside this exe (<see cref="ExeReplacement.TryRemoveLeftovers(string)"/>),
    /// and removes the folders earlier updates downloaded into. It tries for ten
    /// seconds, because what put each there may still be closing as this copy starts: the
    /// newer version that put this copy at its path, which runs from the staging folder
    /// until this copy has said it started and then deletes the older version's second
    /// name, holding off the pass beside the exe until it ends
    /// (<see cref="ExeReplacement.LockSuffix"/>); or the setup, which opens the app before
    /// it exits.
    /// Whatever will not go by then is tried again at the next start, and so is a staging
    /// folder an update still holds, in this process or another. Never throws.
    /// </summary>
    private static async Task RemoveUpdateLeftoversAsync()
    {
        try
        {
            var exePath = Environment.ProcessPath;
            var foldersGone = false;
            var leftoversGone = exePath is null;
            for (var attempt = 0; attempt < 20; attempt++)
            {
                foldersGone = foldersGone || UpdateStagingFolder.RemoveLeftovers(UpdateStagingFolder.DefaultRoot);
                leftoversGone = leftoversGone || ExeReplacement.TryRemoveLeftovers(exePath!);
                if (foldersGone && leftoversGone) return;
                await Task.Delay(500);
            }
        }
        catch (Exception ex)
        {
            CrashLog.TryWrite(ex);
        }
    }

    /// <summary>
    /// A copy that replaces its own file: starts the newer version <paramref name="stagedExe"/>
    /// from the folder it was downloaded to, to put itself at <paramref name="exePath"/> once
    /// this process has ended, waits for it to say it will
    /// (<see cref="NewerVersionProcess.StartToReplace(string, string)"/>), and closes the app, which lets go
    /// of the mutex in <see cref="OnExit"/>. Where it does not say so, it has been ended, and
    /// the app's windows come back as they were. The mutex is held throughout, so nothing
    /// else that takes it starts meanwhile.
    /// </summary>
    /// <remarks>
    /// The windows are hidden for the wait, which can run to
    /// <see cref="HandOverContract.StartBound"/>: Windows shows a window that has stopped
    /// answering as not responding, and closing it then would close the app before the
    /// newer version is seen to start. Dispatcher processing is off for the wait, which
    /// dispatches nothing, so none of the window's own code runs during it.
    /// </remarks>
    private bool ReplaceAndClose(string stagedExe, string exePath)
    {
        var hidden = Windows.Cast<Window>().Where(w => w.IsVisible).ToList();
        foreach (var window in hidden) window.Hide();
        NewerVersionAnswer answer;
        using (Dispatcher.DisableProcessing())
            answer = NewerVersionProcess.StartToReplace(stagedExe, exePath);
        if (answer == NewerVersionAnswer.Started)
        {
            Shutdown();
            return true;
        }
        foreach (var window in hidden) window.Show();
        MainWindow?.Activate();
        return false;
    }

    /// <summary>
    /// Takes the single-instance mutex back after an update that did not hand over.
    /// Where another process took it meanwhile, a command-line run among them,
    /// <see cref="IsInstanceLockLost"/> answers true until the app holds it again.
    /// </summary>
    private static void RetakeAfterHandOverFailed()
    {
        _instanceLockLost = true;
        TryRetakeLostLock();
    }

    /// <summary>
    /// Whether the app has let go of the single-instance mutex for an update that did not
    /// hand over, and another process still holds it. Each call where it has tries to take
    /// it back, on the dispatcher thread, which is the thread that took it first and
    /// releases it in <see cref="OnExit"/>.
    /// </summary>
    public static bool IsInstanceLockLost()
    {
        TryRetakeLostLock();
        return _instanceLockLost;
    }

    /// <summary>
    /// Where the app let go of the single-instance mutex for an update that did not hand
    /// over and has not had it back, tries to take it back, on the dispatcher thread.
    /// </summary>
    private static void TryRetakeLostLock()
    {
        if (!_instanceLockLost) return;
        _instanceLockLost = _instanceLock?.TryTake() != true;
    }
}
