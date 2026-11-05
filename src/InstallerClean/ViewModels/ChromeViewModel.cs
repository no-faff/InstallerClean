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
/// (About / Close), the two "open the details
/// window" commands that take the user from the main scan summary
/// into the registered- or orphaned-files detail windows, the
/// update check (the manual button and the once-per-session
/// automatic check behind <c>AppSettings.AutoUpdateCheck</c>), the card the
/// window opens with where that automatic check finds a newer version, the
/// update from inside the app that Update now starts, the reason in Update now's place
/// where the newer version's release rules this copy out, and on a copy installed by
/// Scoop, Scoop's update command and its Copy button in Update now's place.
///
/// Reads <see cref="ScanViewModel.LastScanResult"/> for the details
/// commands so the detail windows always show the same scan the main
/// window is currently summarising. The details commands' CanExecute
/// reflects whether the scan view model holds a result, so the buttons are
/// disabled (greyed out by the pill styles' IsEnabled trigger) before the
/// first scan completes and at each ending <see cref="ScanViewModel.HasScanned"/>
/// lists.
/// </summary>
public partial class ChromeViewModel : ObservableObject, IDisposable
{
    /// <summary>
    /// Minimum spacing between successive update checks from the manual
    /// button. Only bites if something is hammering the button: a
    /// hand-driven user clicks once and reads the result. Five seconds
    /// keeps a stuck button or a UI-automation loop from running an
    /// elevated process into GitHub's abuse protection, which answers a
    /// caller with retry-after rather than a version. The command stays
    /// non-executable for the duration, so the pill's disabled paint
    /// doubles as the feedback.
    /// </summary>
    private static readonly TimeSpan CheckForUpdatesCooldown = TimeSpan.FromSeconds(5);

    /// <summary>
    /// How long the Copy button beside Scoop's update command reads "Copied" once the
    /// command is on the clipboard, before it reads Copy again.
    /// </summary>
    private static readonly TimeSpan ScoopUpdateCopiedFor = TimeSpan.FromSeconds(2);

    /// <summary>
    /// How long a check that has found a newer version waits for that release's
    /// update.json before it shows the notice. Past it, the notice offers Update now,
    /// whose own read decides.
    /// </summary>
    private static readonly TimeSpan RequirementsBound = TimeSpan.FromSeconds(3);

    /// <summary>
    /// How long from its start the automatic check has to show what it found, read
    /// included. The app holds the window for the automatic check for
    /// <see cref="UpdateCheckService.RequestTimeout"/> and a second from the check's start
    /// (App.UpdateCheckGrace), so the read after a slow answer gets only what is left of
    /// this, and none once it has gone, and the window still opens with the card. Do not
    /// lengthen it past that wait: a find shown after it puts up the notice alone.
    /// </summary>
    private static readonly TimeSpan AutomaticCheckWindow = UpdateCheckService.RequestTimeout;

    private readonly IWindowService _windowService;
    private readonly IMsiFileInfoService _msiInfoService;
    private readonly ISettingsService _settings;
    private readonly IUpdateCheckService _updateCheck;
    private readonly IDialogService _dialogs;
    private readonly ScanViewModel _scan;
    private readonly Func<bool> _isBusy;
    private readonly TimeSpan _checkCooldown;
    private readonly TimeSpan _scoopUpdateCopiedFor;
    private readonly TimeSpan _requirementsBound;
    private readonly TimeSpan _automaticCheckWindow;
    private readonly PropertyChangedEventHandler _scanHandler;
    private readonly IAppUpdater? _updater;
    private CancellationTokenSource? _updateCts;
    private CancellationTokenSource? _downloadCts;
    private CancellationTokenSource? _scoopUpdateCopiedCts;

    /// <summary>The newer version the latest find named, which Update now downloads.</summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(UpdateNoticeDetailText))]
    private string? _newerVersion;

    /// <param name="isBusy">
    /// MainViewModel.IsBusy: a scan or a Move/Delete in flight. Null in a bare
    /// test construction (never busy).
    /// </param>
    /// <param name="checkCooldownOverride">
    /// Replaces <see cref="CheckForUpdatesCooldown"/>. Exists so a test can
    /// run the manual check to completion without waiting out five real
    /// seconds; production callers pass nothing.
    /// </param>
    /// <param name="updater">
    /// The update from inside the app. Null in a bare test construction, which
    /// then offers the release page alone, as a build from source does.
    /// </param>
    /// <param name="scoopUpdateCopiedForOverride">
    /// Replaces <see cref="ScoopUpdateCopiedFor"/>, so a test can see Copy come back
    /// without waiting two real seconds; production callers pass nothing.
    /// </param>
    /// <param name="requirementsBoundOverride">
    /// Replaces <see cref="RequirementsBound"/>, so a test can see a read run past it
    /// without waiting three real seconds; production callers pass nothing.
    /// </param>
    /// <param name="automaticCheckWindowOverride">
    /// Replaces <see cref="AutomaticCheckWindow"/>, so a test can see a late answer cut
    /// the read short without waiting eight real seconds; production callers pass nothing.
    /// </param>
    public ChromeViewModel(
        IWindowService windowService,
        IMsiFileInfoService msiInfoService,
        ISettingsService settings,
        IUpdateCheckService updateCheck,
        IDialogService dialogs,
        ScanViewModel scan,
        Func<bool>? isBusy = null,
        TimeSpan? checkCooldownOverride = null,
        IAppUpdater? updater = null,
        TimeSpan? scoopUpdateCopiedForOverride = null,
        TimeSpan? requirementsBoundOverride = null,
        TimeSpan? automaticCheckWindowOverride = null)
    {
        _windowService = windowService;
        _msiInfoService = msiInfoService;
        _settings = settings;
        _updateCheck = updateCheck;
        _dialogs = dialogs;
        _scan = scan;
        _isBusy = isBusy ?? (() => false);
        _checkCooldown = checkCooldownOverride ?? CheckForUpdatesCooldown;
        _updater = updater;
        _scoopUpdateCopiedFor = scoopUpdateCopiedForOverride ?? ScoopUpdateCopiedFor;
        _requirementsBound = requirementsBoundOverride ?? RequirementsBound;
        _automaticCheckWindow = automaticCheckWindowOverride ?? AutomaticCheckWindow;

        // Re-evaluate the Details buttons when a scan finishes or its result
        // is dropped. HasScanned is observable; LastScanResult is a plain
        // auto-property and won't raise PropertyChanged, which is why the scan
        // view model drops the result before it sets HasScanned false. Held as
        // a field so Dispose can unhook it; the singleton container disposes
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
        // (a post-operation refresh that completes only moves the counts), so
        // the orphaned Details button needs this one to grey out behind the
        // completion overlay rather than sit live over an empty list.
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
        // A download still running stops, and leaves nothing behind.
        _downloadCts?.Cancel();
        _scoopUpdateCopiedCts?.Cancel();
    }

    /// <summary>
    /// The line beside the update button: "Checking..." while the manual check
    /// runs, and "Up to date." when it answers that way. Empty at rest, which
    /// collapses the bound TextBlock.
    /// </summary>
    [ObservableProperty] private string _updateStatusText = string.Empty;

    /// <summary>
    /// The notice's first line, naming the newer version a check found. Set before
    /// <see cref="HasNewerVersion"/>, so the line is whole by the time the notice
    /// shows.
    /// </summary>
    [ObservableProperty] private string _updateNoticeText = string.Empty;

    /// <summary>
    /// Set once a check has found a newer version, and held for the session. It
    /// shows the notice at the top of the main window. A later check failing leaves
    /// it set; only a check answering that this build is the newest clears it. A bool
    /// because the bindings need the positive form of the test: a XAML DataTrigger
    /// tests equality only.
    /// </summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ShowsUpdateNow))]
    [NotifyPropertyChangedFor(nameof(ShowsReleaseLink))]
    [NotifyPropertyChangedFor(nameof(ShowsScoopCommand))]
    [NotifyPropertyChangedFor(nameof(ShowsNetFrameworkLink))]
    [NotifyCanExecuteChangedFor(nameof(OpenReleasePageCommand))]
    [NotifyCanExecuteChangedFor(nameof(OpenNetFrameworkDownloadCommand))]
    [NotifyCanExecuteChangedFor(nameof(UpdateNowCommand))]
    private bool _hasNewerVersion;

    /// <summary>
    /// Why the newer version's release rules this copy out
    /// (<see cref="IAppUpdater.CheckRequirementsAsync"/>), or null where it does not or
    /// could not be read. Set with the version it was read for, before
    /// <see cref="HasNewerVersion"/>.
    /// </summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsUpdateRefused))]
    [NotifyPropertyChangedFor(nameof(UpdateNoticeDetailText))]
    [NotifyPropertyChangedFor(nameof(ShowsUpdateNow))]
    [NotifyPropertyChangedFor(nameof(ShowsReleaseLink))]
    [NotifyPropertyChangedFor(nameof(ShowsNetFrameworkLink))]
    [NotifyCanExecuteChangedFor(nameof(OpenReleasePageCommand))]
    [NotifyCanExecuteChangedFor(nameof(OpenNetFrameworkDownloadCommand))]
    [NotifyCanExecuteChangedFor(nameof(UpdateNowCommand))]
    private UpdateRefusal? _newerVersionRefusal;

    /// <summary>Whether the newer version's release rules this copy out.</summary>
    public bool IsUpdateRefused => NewerVersionRefusal is not null;

    /// <summary>
    /// The second line of the card and of the notice: on a copy that cannot tell whether
    /// the setup installed it, that; where the newer version's release rules this copy
    /// out, why; otherwise that the latest version is strongly recommended.
    /// </summary>
    public string UpdateNoticeDetailText =>
        _updater?.Kind == AppCopyKind.Undetermined ? Strings.UpdateNotice_CopyCannotTell
        : NewerVersionRefusal switch
        {
            UpdateRefusal.NeedsNetFramework48 => Strings.UpdateNotice_NeedsNetFramework48,
            UpdateRefusal.CannotInstallFromThisCopy =>
                string.Format(Strings.UpdateNotice_CannotInstallAutomatically, NewerVersion),
            _ => Strings.UpdateNotice_Recommended,
        };

    /// <summary>
    /// Whether this copy can put a newer version in its own place
    /// (<see cref="AppCopy.CanUpdateItself"/>): Scoop's copy is updated by Scoop, and a
    /// build from source and an undetermined copy cannot.
    /// </summary>
    public bool CanUpdateItself => _updater is not null && AppCopy.CanUpdateItself(_updater.Kind);

    /// <summary>
    /// Whether the notice carries Update now: where this copy can update itself and the
    /// newer version's release does not rule it out.
    /// </summary>
    public bool ShowsUpdateNow => HasNewerVersion && CanUpdateItself && !IsUpdateRefused;

    /// <summary>
    /// Whether the notice carries the link to the release page: where this copy cannot
    /// update itself or the newer version's release rules it out for any reason but
    /// .NET Framework 4.8, and after an update that failed, beside Update now.
    /// </summary>
    public bool ShowsReleaseLink =>
        HasNewerVersion && !ShowsNetFrameworkLink && (!CanUpdateItself || IsUpdateRefused || HasUpdateFailure);

    /// <summary>
    /// Whether the notice carries the link that downloads Microsoft's .NET Framework 4.8
    /// installer, in place of Update now and the release page: where the newer version's
    /// release says its download for this copy needs .NET Framework 4.8 and this PC does
    /// not have it.
    /// </summary>
    public bool ShowsNetFrameworkLink =>
        HasNewerVersion && NewerVersionRefusal == UpdateRefusal.NeedsNetFramework48;

    /// <summary>
    /// Whether the card and the notice give Scoop's update command, with its Copy
    /// button, in place of Update now: on a copy installed by Scoop, once a check has
    /// found a newer version. The link to the release page shows under it.
    /// </summary>
    public bool ShowsScoopCommand =>
        HasNewerVersion && _updater is not null && AppCopy.UpdatedByScoop(_updater.Kind);

    /// <summary>
    /// What this copy's folder says about its Scoop update: the command for the app it was
    /// installed as, and whether it is a machine-wide install. A folder that does not say
    /// gets <see cref="AppCopy.DefaultScoopUpdate"/>.
    /// </summary>
    private ScoopUpdate ScoopUpdate => _updater?.ScoopUpdate ?? AppCopy.DefaultScoopUpdate;

    /// <summary>Scoop's update command, in the box beside Copy.</summary>
    public string ScoopUpdateCommand => ScoopUpdate.Command;

    /// <summary>
    /// The line above Scoop's update command: for a machine-wide install, that it was
    /// installed for all users and the command is run from an administrator's terminal.
    /// </summary>
    public string ScoopUpdateLine =>
        ScoopUpdate.Global ? Strings.UpdateNotice_ScoopGlobal : Strings.UpdateNotice_Scoop;

    /// <summary>
    /// The line above Scoop's update command and the command, as one line for a screen
    /// reader: the line's spoken name, so reading it out reads the command too.
    /// </summary>
    public string ScoopUpdateSpoken =>
        DisplayHelpers.SpokenSentences(ScoopUpdateLine, ScoopUpdateCommand);

    /// <summary>
    /// Set for <see cref="ScoopUpdateCopiedFor"/> once Copy has put Scoop's update command
    /// on the clipboard, while the button reads "Copied".
    /// </summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CopyScoopUpdateText))]
    private bool _isScoopUpdateCopied;

    /// <summary>The Copy button's text: "Copied" just after a copy, Copy otherwise.</summary>
    public string CopyScoopUpdateText =>
        IsScoopUpdateCopied ? Strings.UpdateNotice_CommandCopied : Strings.Action_Copy;

    /// <summary>
    /// Set where the clipboard refused Scoop's update command, which shows the line
    /// saying so under Copy. The next copy that succeeds clears it.
    /// </summary>
    [ObservableProperty] private bool _hasScoopUpdateCopyFailed;

    /// <summary>
    /// Raised after every press of Copy, true where the command went on the clipboard,
    /// so the window can read out the answer to each press, a repeat of the last
    /// included.
    /// </summary>
    public event EventHandler<bool>? ScoopUpdateCopyAnswered;

    /// <summary>
    /// The notice's line saying an update failed, under its first two lines, and empty
    /// until one does. A press of Update now clears it.
    /// </summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasUpdateFailure))]
    [NotifyPropertyChangedFor(nameof(ShowsReleaseLink))]
    [NotifyCanExecuteChangedFor(nameof(OpenReleasePageCommand))]
    private string _updateFailureText = string.Empty;

    /// <summary>Whether the notice shows the line saying an update failed.</summary>
    public bool HasUpdateFailure => UpdateFailureText.Length > 0;

    /// <summary>
    /// True from the press of Update now until the download ends: the card saying it is
    /// downloading stands over the window, with Move, Delete and Re-scan behind it. It
    /// stays up as the app hands over to the newer version and closes.
    /// </summary>
    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(UpdateNowCommand))]
    [NotifyCanExecuteChangedFor(nameof(CancelUpdateCommand))]
    private bool _isUpdating;

    /// <summary>
    /// Set once the download is ready and the app is handing over to it, when there is
    /// nothing left for Cancel to stop.
    /// </summary>
    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(CancelUpdateCommand))]
    private bool _isHandingOver;

    /// <summary>The card's line while it downloads, naming the version.</summary>
    [ObservableProperty] private string _updatingText = string.Empty;

    /// <summary>How much of the download has arrived, from 0 to 100.</summary>
    [ObservableProperty] private double _updateProgressPercent;

    /// <summary>
    /// Whether the server gave the download's length, so the bar can show how far it
    /// has got. Without it the bar moves without measuring, as the scanning card's does.
    /// </summary>
    [ObservableProperty] private bool _isUpdateProgressKnown;

    /// <summary>
    /// True while the card saying a newer version is available stands in front of the
    /// main window, which shows nothing of the scan behind it. The window opens with it
    /// where the check made as the app starts finds a newer version
    /// (<see cref="OfferUpdate"/>); Continue without updating takes it away, and the scan
    /// is then shown. Update now leaves it up: a failed or cancelled update comes back to
    /// it.
    /// </summary>
    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(ContinueWithoutUpdatingCommand))]
    private bool _isOfferingUpdate;

    /// <summary>
    /// Raised each time a check finds a newer version, whether or not the notice
    /// was already showing, so the window can read the notice out as the answer to
    /// a click that found the version it already names.
    /// </summary>
    public event EventHandler? NewerVersionFound;

    /// <summary>
    /// Starts the once-per-session automatic check. Called by the App as
    /// soon as the view-model exists, before the startup scan, so the check
    /// runs beside the scan and has usually answered by the time it ends. The
    /// App holds what the scan found until it has (<see cref="AutomaticCheck"/>).
    /// Every outcome but "newer version found" is deliberately invisible, and
    /// every exception is swallowed, so it cannot disturb the splash path. The
    /// enabling setting is read from disk inside the call, before anything
    /// touches the network, so an install that has opted out opens no
    /// socket. That read happens on this startup path, before the main
    /// window and with it the About checkbox exist, so unticking the box
    /// governs the next launch rather than this one.
    /// </summary>
    public void StartAutomaticUpdateCheck() => AutomaticCheck = RunAutomaticUpdateCheckAsync();

    /// <summary>
    /// Sets the line saying the update couldn't be installed, on a start that follows an
    /// update which left this version in place. Called before
    /// <see cref="StartAutomaticUpdateCheck"/>, so the card and the notice carry the line
    /// where the check finds the newer version.
    /// </summary>
    public void ShowUpdateFailedAtStart() => UpdateFailureText = Strings.UpdateNotice_InstallFailed;

    /// <summary>
    /// The automatic check, completing once it has answered, given up, found the check
    /// switched off or been cancelled. It never faults. Completed before
    /// <see cref="StartAutomaticUpdateCheck"/> is called.
    /// </summary>
    public Task AutomaticCheck { get; private set; } = Task.CompletedTask;

    /// <summary>
    /// Puts up the card saying a newer version is available, where a check has found
    /// one.
    /// </summary>
    public void OfferUpdate()
    {
        if (HasNewerVersion)
            IsOfferingUpdate = true;
    }

    /// <summary>
    /// The card's Continue without updating, and Esc while the card is up: the card goes
    /// and the main window shows the scan. The notice at the top stays, and whatever Copy
    /// last answered beside Scoop's update command is cleared, so the notice's button
    /// reads Copy and its line saying the copy failed is gone.
    /// </summary>
    [RelayCommand(CanExecute = nameof(IsOfferingUpdate))]
    private void ContinueWithoutUpdating()
    {
        IsOfferingUpdate = false;
        ClearScoopCopyAnswer();
    }

    /// <summary>
    /// Clears what Copy last answered: "Copied" on the button and the line saying the
    /// clipboard refused, and ends the time the button was to read "Copied" for.
    /// </summary>
    private void ClearScoopCopyAnswer()
    {
        _scoopUpdateCopiedCts?.Cancel();
        IsScoopUpdateCopied = false;
        HasScoopUpdateCopyFailed = false;
    }

    /// <summary>
    /// The notice appearing, and going when a check finds this build current, each start
    /// Copy's answer afresh. A check that finds a newer version while the notice is
    /// already up leaves it as it is.
    /// </summary>
    partial void OnHasNewerVersionChanged(bool value) => ClearScoopCopyAnswer();

    /// <summary>
    /// The automatic check's body. Internal rather than private so
    /// ChromeViewModelTests can run and await one check by itself.
    /// </summary>
    internal async Task RunAutomaticUpdateCheckAsync()
    {
        var token = ResetUpdateCheckToken();
        var sinceStart = System.Diagnostics.Stopwatch.StartNew();
        try
        {
            if (!_settings.Load().AutoUpdateCheck)
                return;
            var result = await _updateCheck.CheckAsync(UpdateCheckOrigin.Automatic, token);
            if (result is UpdateAvailable available)
            {
                // The time left in the window, never less than none, bounded by the read's own
                // bound unless that is no limit.
                var remaining = _automaticCheckWindow - sinceStart.Elapsed;
                var left = remaining > TimeSpan.Zero ? remaining : TimeSpan.Zero;
                var within = IAppUpdater.IsNoLimit(_requirementsBound) || left < _requirementsBound
                    ? left
                    : _requirementsBound;
                var requirements = await RequirementsAsync(available.LatestVersion, within, token);
                token.ThrowIfCancellationRequested();
                ShowUpdateFound(available, requirements);
            }
            // UpToDate and CheckFailed both end in silence: an automatic
            // check the user never asked to watch has nothing to report
            // but a newer version. The Automatic origin extends that
            // silence to crash.log, which is where an unreachable github.com
            // would otherwise leave a trace once per launch.
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
    /// up-to-date, the notice on a find, a warning dialog on a failure. The
    /// command's own execution covers the cooldown, so the button stays
    /// disabled for the spacing window.
    /// </summary>
    [RelayCommand]
    private async Task CheckForUpdatesAsync()
    {
        var token = ResetUpdateCheckToken();
        UpdateStatusText = Strings.UpdateCheck_Status_Checking;

        UpdateCheckResult result;
        Requirements requirements = default;
        try
        {
            result = await _updateCheck.CheckAsync(UpdateCheckOrigin.Manual, token);
            if (result is UpdateAvailable found)
            {
                requirements = await RequirementsAsync(found.LatestVersion, _requirementsBound, token);
                token.ThrowIfCancellationRequested();
            }
        }
        catch (OperationCanceledException)
        {
            // App closing; the window is on its way out, leave the text be.
            return;
        }

        switch (result)
        {
            case UpToDate:
                HasNewerVersion = false;
                NewerVersionRefusal = null;
                UpdateStatusText = Strings.UpdateCheck_Status_UpToDate;
                break;

            case UpdateAvailable available:
                UpdateStatusText = string.Empty;
                ShowUpdateFound(available, requirements);
                break;

            case CheckFailed failed:
                UpdateStatusText = string.Empty;
                _dialogs.ShowWarning(FailureReasonText(failed), Strings.UpdateCheck_Title);
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
        // "Up to date." clears once read; the notice a find shows stays.
        if (UpdateStatusText == Strings.UpdateCheck_Status_UpToDate)
            UpdateStatusText = string.Empty;
    }

    /// <summary>
    /// Opens the newer version's release page; bound to the link on the notice at the top of the
    /// main window and on the card saying a newer version is available. The
    /// CanExecute is the link's tab-order gate, not politeness: WPF's keyboard navigation descends into a collapsed TextBlock
    /// regardless of visibility (a TextBlock is an IContentHost) and admits a
    /// content element on Focusable, IsTabStop and IsEnabled alone, content
    /// elements having no IsVisible to test, so an always-executable command
    /// leaves the hidden link an invisible extra Tab stop. Disabled wherever the
    /// link is not shown, it drops out of the walk; where it shows it is enabled
    /// and a real stop. The body's guard stays because ICommand.Execute does not
    /// consult CanExecute.
    /// </summary>
    [RelayCommand(CanExecute = nameof(ShowsReleaseLink))]
    private void OpenReleasePage()
    {
        if (ShowsReleaseLink && NewerVersion is { } version)
            _windowService.OpenUrl(UpdateCheckService.ReleasePageUrl(version));
    }

    /// <summary>
    /// What a read of a release's update.json answered: whether it was made, and why the
    /// release rules this copy out, or null where it does not.
    /// </summary>
    private readonly record struct Requirements(bool Read, UpdateRefusal? Refusal);

    /// <summary>
    /// What the release of <paramref name="version"/> says of this copy: read, with its
    /// refusal or none; or not read, where this copy has no download to read it for, or
    /// where the read fails or runs past <paramref name="within"/>. With no time at all the
    /// file is not fetched, and a file already kept for the version still answers
    /// (<see cref="IAppUpdater.CheckRequirementsAsync"/>). Nothing is shown or written for
    /// a read that fails: Update now reads the file again before it downloads. Throws
    /// <see cref="OperationCanceledException"/> only where <paramref name="token"/> is
    /// cancelled.
    /// </summary>
    private async Task<Requirements> RequirementsAsync(string version, TimeSpan within, CancellationToken token)
    {
        if (_updater is null || !CanUpdateItself) return default;
        try
        {
            return new Requirements(true, await _updater.CheckRequirementsAsync(version, within, token));
        }
        catch (OperationCanceledException) when (token.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception)
        {
            return default;
        }
    }

    /// <summary>
    /// Downloads Microsoft's .NET Framework 4.8 installer in the browser; bound to the link
    /// on the card and the notice. Disabled wherever the link is not shown, for the reason
    /// <see cref="OpenReleasePage"/> gives.
    /// </summary>
    [RelayCommand(CanExecute = nameof(ShowsNetFrameworkLink))]
    private void OpenNetFrameworkDownload()
    {
        if (ShowsNetFrameworkLink)
            _windowService.OpenUrl(UpdateManifest.NetFramework48DownloadUrl);
    }

    /// <summary>
    /// Shows the notice for <paramref name="available"/>, with what its release says of this
    /// copy. A read that was not made leaves a refusal already read for the same version in
    /// place, so a check that cannot reach the file does not bring back Update now for a
    /// version this copy is known not to install.
    /// </summary>
    private void ShowUpdateFound(UpdateAvailable available, Requirements requirements)
    {
        var refusal = requirements.Read || available.LatestVersion != NewerVersion
            ? requirements.Refusal
            : NewerVersionRefusal;
        NewerVersion = available.LatestVersion;
        NewerVersionRefusal = refusal;
        if (refusal is not null)
            UpdateFailureText = string.Empty;
        UpdateNoticeText = string.Format(Strings.UpdateNotice_Available, available.LatestVersion);
        HasNewerVersion = true;
        NewerVersionFound?.Invoke(this, EventArgs.Empty);
    }

    private bool CanUpdateNow() =>
        HasNewerVersion && CanUpdateItself && !IsUpdateRefused && !IsUpdating && !_isBusy();

    /// <summary>
    /// Update now, on the card saying a newer version is available and on the notice:
    /// downloads the newer version as the same kind of copy, and hands over to it. The
    /// setup's copy runs the newer setup, which starts the app again once it has
    /// installed; the other copies start the newer version, which puts itself at their own
    /// path once this app has closed and starts from there. Either way this app closes,
    /// and the newer version scans as it opens, as every start does.
    ///
    /// Anything that fails while this app is still open takes the download card away
    /// and puts the line for what failed on the card and the notice, with the link
    /// beside Update now, having changed nothing on disk. Cancel takes the download card
    /// away and says nothing. What failed goes to crash.log; the screen names only which
    /// of the two things it was. Where the newer version's release rules this copy out,
    /// which the download reads before it fetches anything, the download card goes and
    /// the card and the notice say why in place of Update now, with the link; that is not
    /// a failure, and nothing is written. That holds where Cancel is pressed as the
    /// refusal arrives as well, since it is what the release says of this copy.
    /// </summary>
    [RelayCommand(CanExecute = nameof(CanUpdateNow))]
    private async Task UpdateNowAsync()
    {
        if (!CanUpdateNow() || _updater is null || NewerVersion is not { } version) return;

        UpdateFailureText = string.Empty;
        UpdateProgressPercent = 0;
        IsUpdateProgressKnown = false;
        UpdatingText = string.Format(Strings.UpdateNotice_Downloading, version);
        IsUpdating = true;

        using var cts = new CancellationTokenSource();
        _downloadCts = cts;
        PreparedUpdate? prepared = null;
        var handedOver = false;
        try
        {
            var progress = new Progress<UpdateProgress>(step =>
            {
                // Once the download is complete the card says it is being checked, which
                // waits while another program, an antivirus say, looks at the new file. The
                // bar moves without measuring meanwhile, so a long wait does not look stuck.
                if (step.Checking)
                {
                    UpdatingText = Strings.UpdateNotice_CheckingDownload;
                    IsUpdateProgressKnown = false;
                    return;
                }
                IsUpdateProgressKnown = step.Fraction is not null;
                UpdateProgressPercent = (step.Fraction ?? 0) * 100;
            });
            prepared = await _updater.PrepareAsync(version, progress, cts.Token);
            // Cancel pressed as the download finished: what was readied is taken away below.
            if (cts.IsCancellationRequested) return;

            IsHandingOver = true;
            handedOver = await _windowService.StartUpdateAndCloseAsync(prepared);
            if (!handedOver)
                UpdateFailureText = Strings.UpdateNotice_InstallFailed;
        }
        catch (OperationCanceledException) when (cts.IsCancellationRequested)
        {
            // Cancel: the card goes and the notice stays as it was.
        }
        catch (UpdateRefusedException ex)
        {
            // A refusal read as Cancel was pressed is still shown: it is what the release
            // says of this copy, and Update now is not offered again for it.
            NewerVersionRefusal = ex.Refusal;
        }
        catch (UpdateFailedException ex)
        {
            CrashLog.TryWrite(ex);
            UpdateFailureText = ex.Stage == UpdateFailureStage.Download
                ? Strings.UpdateNotice_DownloadFailed
                : Strings.UpdateNotice_InstallFailed;
        }
        catch (Exception ex)
        {
            CrashLog.TryWrite(ex);
            UpdateFailureText = Strings.UpdateNotice_InstallFailed;
        }
        finally
        {
            _downloadCts = null;
            // Handed over, the app is closing and the card stays up until it has.
            // Otherwise what getting the newer version ready left on disk goes, however
            // the attempt ended.
            if (!handedOver)
            {
                prepared?.Undo();
                IsHandingOver = false;
                IsUpdating = false;
            }
        }
    }

    /// <summary>
    /// Copy, beside Scoop's update command on the card and the notice: puts the command on
    /// the clipboard, and the button reads "Copied" for <see cref="ScoopUpdateCopiedFor"/>.
    /// A press while it reads "Copied" starts that time again. Where the clipboard refuses,
    /// the button reads Copy and the line saying so shows. Presses are never refused, so
    /// the button stays enabled while it reads "Copied".
    /// </summary>
    [RelayCommand(AllowConcurrentExecutions = true)]
    private async Task CopyScoopUpdateAsync()
    {
        var previous = _scoopUpdateCopiedCts;
        _scoopUpdateCopiedCts = new CancellationTokenSource();
        previous?.Cancel();
        previous?.Dispose();
        var token = _scoopUpdateCopiedCts.Token;

        var copied = _windowService.CopyToClipboard(ScoopUpdateCommand);
        HasScoopUpdateCopyFailed = !copied;
        IsScoopUpdateCopied = copied;
        ScoopUpdateCopyAnswered?.Invoke(this, copied);
        if (!copied) return;

        try
        {
            await Task.Delay(_scoopUpdateCopiedFor, token);
        }
        catch (OperationCanceledException)
        {
            // A later press took over, or the app is closing.
            return;
        }
        IsScoopUpdateCopied = false;
    }

    /// <summary>The download card's Cancel, and Esc while the card is up.</summary>
    [RelayCommand(CanExecute = nameof(CanCancelUpdate))]
    private void CancelUpdate() => _downloadCts?.Cancel();

    private bool CanCancelUpdate() => IsUpdating && !IsHandingOver;

    /// <summary>
    /// Asks Update now and the two Details commands to re-read whether they can run,
    /// which also depends on whether a scan or a Move or Delete is in flight.
    /// </summary>
    internal void NotifyBusyChanged()
    {
        UpdateNowCommand.NotifyCanExecuteChanged();
        OpenOrphanedDetailsCommand.NotifyCanExecuteChanged();
        OpenRegisteredDetailsCommand.NotifyCanExecuteChanged();
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

    /// <summary>
    /// The sentence for a failed check. Four of the five reasons are the reason
    /// alone; the fifth offers the crash log, so it takes the whole result and
    /// reads the path off it. A path is there only where this check wrote an entry
    /// and the write succeeded, so the sibling naming the file is chosen by whether
    /// the file exists to be named rather than by which branch ran.
    /// </summary>
    private static string FailureReasonText(CheckFailed failed) => failed.ReasonCode switch
    {
        UpdateCheckFailureReason.NetworkUnavailable => Strings.UpdateCheck_Failed_NetworkUnavailable,
        UpdateCheckFailureReason.ServerError => Strings.UpdateCheck_Failed_ServerError,
        UpdateCheckFailureReason.ResponseParseError => Strings.UpdateCheck_Failed_ResponseParseError,
        UpdateCheckFailureReason.Timeout => Strings.UpdateCheck_Failed_Timeout,
        _ => failed.CrashLogPath is { } path
            ? string.Format(Strings.UpdateCheck_Failed_Unknown, path)
            : Strings.UpdateCheck_Failed_Unknown_NoLog,
    };

    // A scan, or a Move or Delete with its pre-flight, replaces the result when it
    // ends, and the overlay that disables the main window shows only once one has
    // run for 200 ms. So both Details windows are out of use from the first line of
    // either. A Details window reads the result once, as it opens, so one opened
    // inside those 200 ms would go on showing the set, counts and sizes from
    // before.
    private bool CanOpenRegisteredDetails => _scan.LastScanResult is not null && !_isBusy();

    // The orphaned-files window lists what the scan found. With nothing found
    // there is nothing to list, and a live button onto an empty list is a dead
    // end, so its gate also asks for files. The registered window has content
    // after any scan the app accepts, and takes the gate above alone.
    private bool CanOpenOrphanedDetails => CanOpenRegisteredDetails && _scan.HasOrphans;

    [RelayCommand(CanExecute = nameof(CanOpenOrphanedDetails))]
    private void OpenOrphanedDetails()
    {
        if (_scan.LastScanResult is null) return;

        var viewModel = new OrphanedFilesViewModel(
            _scan.LastScanResult.RemovableFiles,
            _msiInfoService);

        _windowService.ShowOrphanedDetails(viewModel);
    }

    [RelayCommand(CanExecute = nameof(CanOpenRegisteredDetails))]
    private void OpenRegisteredDetails()
    {
        if (_scan.LastScanResult is null) return;

        var viewModel = new RegisteredFilesViewModel(
            _scan.LastScanResult.RegisteredPackages,
            _scan.LastScanResult.RegisteredTotalBytes,
            _msiInfoService,
            // The withheld files, so this window's own header counts the same two
            // populations the main window's left-alone line does. The two are one
            // click apart and must not disagree.
            _scan.LastScanResult.WithheldFiles);

        _windowService.ShowRegisteredDetails(viewModel);
    }

    [RelayCommand]
    private void ShowAbout() => _windowService.ShowAbout();

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
