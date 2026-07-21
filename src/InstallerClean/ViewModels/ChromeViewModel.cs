using System.ComponentModel;
using System.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using InstallerClean.Helpers;
using InstallerClean.Resources;
using InstallerClean.Services;

namespace InstallerClean.ViewModels;

/// <summary>
/// Window-management slice. Holds the commands wired to the chrome
/// (About / Donate / Close), the two "open the details
/// window" commands that take the user from the main scan summary
/// into the registered- or orphaned-files detail windows, and the
/// update check (the manual button and the once-per-session
/// automatic check behind <c>AppSettings.AutoUpdateCheck</c>).
///
/// Reads <see cref="ScanViewModel.LastScanResult"/> for the details
/// commands so the detail windows always show the same scan the main
/// window is currently summarising. The details commands' CanExecute
/// reflects whether a scan has completed at all, so the buttons are
/// disabled (greyed out by the pill styles' IsEnabled trigger) until
/// the user has data to view.
/// </summary>
public partial class ChromeViewModel : ObservableObject, IDisposable
{
    /// <summary>
    /// Minimum spacing between successive update checks from the manual
    /// button. Only bites if something is hammering the button: a
    /// hand-driven user clicks once and reads the result. Five seconds
    /// keeps a stuck button or a UI-automation loop from running into
    /// GitHub's 60/hour unauthenticated rate limit on a long enough
    /// timescale. The command stays non-executable for the duration, so
    /// the pill's disabled paint doubles as the feedback.
    /// </summary>
    private static readonly TimeSpan CheckForUpdatesCooldown = TimeSpan.FromSeconds(5);

    private readonly IWindowService _windowService;
    private readonly IMsiFileInfoService _msiInfoService;
    private readonly ISettingsService _settings;
    private readonly IUpdateCheckService _updateCheck;
    private readonly IDialogService _dialogs;
    private readonly ScanViewModel _scan;
    private readonly Func<bool> _isBusy;
    private readonly TimeSpan _checkCooldown;
    private readonly PropertyChangedEventHandler _scanHandler;
    private CancellationTokenSource? _updateCts;

    /// <param name="isBusy">
    /// MainViewModel.IsBusy: a scan or a Move/Delete in flight. Null in a bare
    /// test construction (never busy).
    /// </param>
    /// <param name="checkCooldownOverride">
    /// Replaces <see cref="CheckForUpdatesCooldown"/>. Exists so a test can
    /// run the manual check to completion without waiting out five real
    /// seconds; production callers pass nothing.
    /// </param>
    public ChromeViewModel(
        IWindowService windowService,
        IMsiFileInfoService msiInfoService,
        ISettingsService settings,
        IUpdateCheckService updateCheck,
        IDialogService dialogs,
        ScanViewModel scan,
        Func<bool>? isBusy = null,
        TimeSpan? checkCooldownOverride = null)
    {
        _windowService = windowService;
        _msiInfoService = msiInfoService;
        _settings = settings;
        _updateCheck = updateCheck;
        _dialogs = dialogs;
        _scan = scan;
        _isBusy = isBusy ?? (() => false);
        _checkCooldown = checkCooldownOverride ?? CheckForUpdatesCooldown;

        // Re-evaluate the Details buttons when a scan finishes.
        // HasScanned is observable; LastScanResult is a plain auto-
        // property and won't raise PropertyChanged. Held as a field
        // so Dispose can unhook it; the singleton container disposes
        // this VM on shutdown.
        _scanHandler = OnScanPropertyChanged;
        _scan.PropertyChanged += _scanHandler;
    }

    private void OnScanPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(ScanViewModel.HasScanned))
        {
            OpenOrphanedDetailsCommand.NotifyCanExecuteChanged();
            OpenRegisteredDetailsCommand.NotifyCanExecuteChanged();
        }
        // A clean-up empties the orphan list without a fresh HasScanned raise
        // (the post-operation refresh only moves the counts), so the orphaned
        // Details button needs this one to grey out behind the completion
        // overlay rather than sit live over an empty list.
        else if (e.PropertyName == nameof(ScanViewModel.HasOrphans))
        {
            OpenOrphanedDetailsCommand.NotifyCanExecuteChanged();
        }
    }

    public void Dispose()
    {
        _scan.PropertyChanged -= _scanHandler;
        // Releases a check awaiting the network and a manual check parked
        // in its cooldown; both paths treat the cancellation as "say
        // nothing and stop".
        _updateCts?.Cancel();
        _updateCts?.Dispose();
    }

    /// <summary>
    /// The line beside the update button: "Checking...", "Up to date." or
    /// the update-available sentence. Empty at rest, which collapses the
    /// bound TextBlock.
    /// </summary>
    [ObservableProperty] private string _updateStatusText = string.Empty;

    /// <summary>
    /// The release page of a newer version once a check has found one;
    /// empty otherwise. Non-empty swaps the status line's presentation
    /// from plain text to a hyperlink onto this URL, so the sentence
    /// announcing the update is itself the way to get it.
    /// </summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasUpdateLink))]
    private string _updateAvailableUrl = string.Empty;

    /// <summary>
    /// Which of the status line's two presentations is live. The bindings
    /// need the POSITIVE form of "a URL is set": a XAML DataTrigger tests
    /// equality only, so <see cref="UpdateAvailableUrl"/> alone can express
    /// "no link" and never "a link", and the plain line has to hide itself
    /// in exactly that case.
    /// </summary>
    public bool HasUpdateLink => UpdateAvailableUrl.Length > 0;

    /// <summary>
    /// Starts the once-per-session automatic check. Called by the App as
    /// soon as the view-model exists, before the startup scan is awaited,
    /// so the answer is usually already painted when the window first
    /// shows: a typical session lasts seconds, and a check that arrived
    /// later would routinely arrive after the user had gone.
    /// Fire-and-forget because nothing downstream awaits it and every
    /// outcome but "newer version found" is deliberately invisible; every
    /// exception is swallowed, so it cannot disturb the splash path. The
    /// enabling setting is read inside the call rather than here, so
    /// unticking the About checkbox stops a check that has not yet fired.
    /// </summary>
    public void StartAutomaticUpdateCheck() => _ = RunAutomaticUpdateCheckAsync();

    /// <summary>
    /// The automatic check's body. Internal rather than private so
    /// ChromeViewModelTests can await the one path production deliberately
    /// leaves unawaited; polling the properties after
    /// <see cref="StartAutomaticUpdateCheck"/> would be a race dressed as a
    /// test.
    /// </summary>
    internal async Task RunAutomaticUpdateCheckAsync()
    {
        var token = ResetUpdateCheckToken();
        try
        {
            if (!_settings.Load().AutoUpdateCheck)
                return;
            var result = await _updateCheck.CheckAsync(token);
            if (result is UpdateAvailable available)
                ShowUpdateFound(available);
            // UpToDate and CheckFailed both end in silence: an automatic
            // check the user never asked to watch has nothing to report
            // but a newer version.
        }
        catch (OperationCanceledException)
        {
            // Superseded by a manual click, or the app is closing.
        }
        catch (Exception ex)
        {
            CrashLog.TryWrite(ex);
        }
    }

    /// <summary>
    /// The manual check. Same service call as the automatic path, but
    /// every outcome is shown: the status line while checking and on
    /// up-to-date, the update dialog on a find, a warning dialog on a
    /// failure. The command's own execution covers the cooldown, so the
    /// button stays disabled for the spacing window.
    /// </summary>
    [RelayCommand]
    private async Task CheckForUpdatesAsync()
    {
        var token = ResetUpdateCheckToken();
        UpdateAvailableUrl = string.Empty;
        UpdateStatusText = Strings.UpdateCheck_Status_Checking;

        UpdateCheckResult result;
        try
        {
            result = await _updateCheck.CheckAsync(token);
        }
        catch (OperationCanceledException)
        {
            // App closing; the window is on its way out, leave the text be.
            return;
        }

        switch (result)
        {
            case UpToDate:
                UpdateStatusText = Strings.UpdateCheck_Status_UpToDate;
                break;

            case UpdateAvailable available:
                ShowUpdateFound(available);
                if (_windowService.ShowUpdateAvailable(available.CurrentVersion, available.LatestVersion))
                    _windowService.OpenUrl(available.ReleaseUrl);
                break;

            case CheckFailed failed:
                UpdateStatusText = string.Empty;
                _dialogs.ShowWarning(FailureReasonText(failed.ReasonCode), Strings.UpdateCheck_Title);
                break;
        }

        try
        {
            await Task.Delay(_checkCooldown, token);
        }
        catch (OperationCanceledException)
        {
            return;
        }
        // The transient wordings clear once read; the update-available
        // sentence stays, because it is the one the user still needs after
        // dismissing the dialog, and it carries the link.
        if (UpdateAvailableUrl.Length == 0 && UpdateStatusText == Strings.UpdateCheck_Status_UpToDate)
            UpdateStatusText = string.Empty;
    }

    /// <summary>Opens the found release's page; bound to the status hyperlink.</summary>
    [RelayCommand]
    private void OpenUpdatePage()
    {
        if (UpdateAvailableUrl.Length > 0)
            _windowService.OpenUrl(UpdateAvailableUrl);
    }

    private void ShowUpdateFound(UpdateAvailable available)
    {
        // URL before text: the XAML swaps presentation on the URL and
        // reads the text, so both are in place when either raise lands.
        UpdateAvailableUrl = available.ReleaseUrl;
        UpdateStatusText = string.Format(
            Strings.UpdateCheck_Status_UpdateAvailable, available.LatestVersion);
    }

    /// <summary>
    /// Swap-then-dispose, matching CleanupViewModel.ScheduleMoveDestinationSave:
    /// cancelling the previous source releases whichever check is parked on it
    /// (the automatic check awaiting the network, most often, since a manual
    /// click can land while it is still in flight) without leaking one CTS per
    /// supersession.
    /// </summary>
    private CancellationToken ResetUpdateCheckToken()
    {
        var previous = _updateCts;
        _updateCts = new CancellationTokenSource();
        previous?.Cancel();
        previous?.Dispose();
        return _updateCts.Token;
    }

    private static string FailureReasonText(UpdateCheckFailureReason reason) => reason switch
    {
        UpdateCheckFailureReason.NetworkUnavailable => Strings.UpdateCheck_Failed_NetworkUnavailable,
        UpdateCheckFailureReason.ServerError => Strings.UpdateCheck_Failed_ServerError,
        UpdateCheckFailureReason.ResponseParseError => Strings.UpdateCheck_Failed_ResponseParseError,
        UpdateCheckFailureReason.Timeout => Strings.UpdateCheck_Failed_Timeout,
        _ => Strings.UpdateCheck_Failed_Unknown,
    };

    private bool HasScanResult => _scan.LastScanResult is not null;

    // The orphaned-files window lists what the scan found. With nothing found
    // there is nothing to list, and a live button onto an empty list is a dead
    // end; the registered window, which always has content after a scan, keeps
    // the plain has-a-result gate.
    private bool HasOrphansToShow => HasScanResult && _scan.HasOrphans;

    [RelayCommand(CanExecute = nameof(HasOrphansToShow))]
    private void OpenOrphanedDetails()
    {
        if (_scan.LastScanResult is null) return;

        var viewModel = new OrphanedFilesViewModel(
            _scan.LastScanResult.RemovableFiles,
            _msiInfoService);

        _windowService.ShowOrphanedDetails(viewModel);
    }

    [RelayCommand(CanExecute = nameof(HasScanResult))]
    private void OpenRegisteredDetails()
    {
        if (_scan.LastScanResult is null) return;

        var viewModel = new RegisteredFilesViewModel(
            _scan.LastScanResult.RegisteredPackages,
            _scan.LastScanResult.RegisteredTotalBytes,
            _msiInfoService);

        _windowService.ShowRegisteredDetails(viewModel);
    }

    [RelayCommand]
    private void ShowAbout() => _windowService.ShowAbout();

    // Bound to the heart on the completion card, which is the app's only
    // donate control outside the About window's own pill.
    [RelayCommand]
    private void Donate() => _windowService.OpenUrl("https://nofaff.netlify.app/support");

    [RelayCommand]
    private void CloseApp() => _windowService.CloseMainWindow();

    // Invoked by the bottom-bar language menu with a culture name
    // ("en-GB", "it"). Re-picking the language already on screen is a no-op,
    // so it does not pointlessly restart; the comparison is against the
    // DISPLAYED language (SupportedLanguages.Active), not the saved setting
    // (which this write changes) nor an explicit override alone (a default
    // install follows the OS with no override, yet still shows a language).
    // A real change is saved and applied by a relaunch, because the resx
    // strings resolve once when each window is built and do not re-read a
    // culture swapped at runtime.
    [RelayCommand]
    private void SetLanguage(string? culture)
    {
        if (string.Equals(culture, SupportedLanguages.Active(Localisation.UiCulture), StringComparison.OrdinalIgnoreCase))
            return;
        // Refuse a relaunch while a scan or a Move/Delete is in flight: the
        // relaunch tears the process down and the child cannot inherit the running
        // operation, so it would end exactly like closing the window mid-move. The
        // bottom-nav globe is already disabled while the scanning or operating
        // overlay is up, but a Move/Delete pre-flight runs with the overlay not yet
        // shown, so this is the belt that covers that window.
        if (_isBusy())
            return;
        _settings.Update(s => s.Language = culture);
        _windowService.RelaunchForLanguageChange();
    }
}
