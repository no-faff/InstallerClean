using System.Diagnostics;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using InstallerClean.Helpers;
using InstallerClean.Models;
using InstallerClean.Resources;
using InstallerClean.Services;

namespace InstallerClean.ViewModels;

/// <summary>
/// Scanning slice of the main window's state. Owns the scan command,
/// the displayed registered/orphaned counts, the pending-reboot
/// warning, the missing-from-disk warning and a reference to the last
/// scan result.
///
/// Other slices (CleanupViewModel, ChromeViewModel) read
/// <see cref="LastScanResult"/> rather than calling the scan service
/// themselves so the cached result stays the single source of truth.
/// </summary>
public partial class ScanViewModel : ObservableObject
{
    private readonly IFileSystemScanService _scanService;
    private readonly IPendingRebootService _rebootService;
    private readonly IDialogService _dialogService;
    private readonly Func<bool> _isExternallyBlocked;

    private CancellationTokenSource? _scanCts;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(ScanCommand))]
    private bool _isScanning;
    [ObservableProperty] private string _scanProgress = string.Empty;
    [ObservableProperty] private bool _hasScanned;

    [ObservableProperty] private int _registeredFileCount;
    [ObservableProperty] private string _registeredSizeDisplay = string.Empty;
    [ObservableProperty] private int _orphanedFileCount;
    [ObservableProperty] private string _orphanedSizeDisplay = string.Empty;

    /// <summary>Last pending-reboot probe result; null until the first scan.</summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasPendingReboot))]
    [NotifyPropertyChangedFor(nameof(PendingRebootBannerText))]
    private PendingRebootResult? _pendingRebootResult;

    /// <summary>True when the last probe returned Block.</summary>
    public bool HasPendingReboot => PendingRebootResult?.IsBlocked == true;

    /// <summary>
    /// Stable, non-localised label for the current pending-reboot state.
    /// Drives the diagnostic log so a non-en-GB user's report still
    /// matches a developer's filter on <c>"installerInProgress"</c>.
    /// </summary>
    public string PendingRebootLabel => PendingRebootResult?.Reason switch
    {
        PendingRebootReason.MsiExecuteMutexHeld => PendingRebootLabels.MsiExecuteMutexHeld,
        PendingRebootReason.InstallerInProgress => PendingRebootLabels.InstallerInProgress,
        PendingRebootReason.PendingRenameInCache => PendingRebootLabels.PendingRenameInCache,
        _ => PendingRebootLabels.Clean,
    };

    /// <summary>Localised banner text for the current Block reason; empty otherwise.</summary>
    public string PendingRebootBannerText => PendingRebootResult?.Reason switch
    {
        PendingRebootReason.MsiExecuteMutexHeld => Strings.Body_PendingReboot_MsiExecuteMutex,
        PendingRebootReason.InstallerInProgress => Strings.Body_PendingReboot_InstallerInProgress,
        PendingRebootReason.PendingRenameInCache => Strings.Body_PendingReboot_PendingRenameInCache,
        null => string.Empty,
        _ => throw new InvalidOperationException(
            $"Unhandled PendingRebootReason: {PendingRebootResult?.Reason}. " +
            "A new enum value was added without updating PendingRebootBannerText."),
    };

    /// <summary>
    /// Count of registered, non-removable packages whose file is missing
    /// from disk. Drives the missing-from-disk banner: the banner only
    /// triggers on this population, not on superseded patches whose
    /// file is already gone (those are benign and counted separately
    /// in <see cref="MissingRemovableCount"/>).
    /// </summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasMissingFromDisk))]
    [NotifyPropertyChangedFor(nameof(MissingFromDiskSummaryText))]
    private int _missingNonRemovableCount;

    /// <summary>
    /// Count of superseded / obsoleted packages whose file is already
    /// gone from disk. Informational only; does not surface in the UI.
    /// </summary>
    [ObservableProperty] private int _missingRemovableCount;

    /// <summary>
    /// Cached result of the most recent successful scan. Null until
    /// the first scan completes; remains the same instance until the
    /// next scan replaces it.
    /// </summary>
    public ScanResult? LastScanResult { get; private set; }

    /// <summary>
    /// Wall-clock duration of the most recent user-visible scan, in
    /// milliseconds. Set by <c>ScanAsync</c> and <c>ScanWithProgressAsync</c>;
    /// not overwritten by <c>RefreshAsync</c> so the result-log entry
    /// built after a Move or Delete reports the duration of the scan
    /// that surfaced the orphans, not the silent post-operation refresh.
    /// </summary>
    public long LastScanDurationMs { get; private set; }

    /// <summary>
    /// Raised after every successful scan completes, including the
    /// initial startup scan. Subscribers can read
    /// <see cref="LastScanResult"/> at this point.
    /// </summary>
    public event EventHandler? ScanCompleted;

    public ScanViewModel(
        IFileSystemScanService scanService,
        IPendingRebootService rebootService,
        IDialogService dialogService,
        Func<bool>? isExternallyBlocked = null)
    {
        _scanService = scanService;
        _rebootService = rebootService;
        _dialogService = dialogService;
        _isExternallyBlocked = isExternallyBlocked ?? (() => false);
    }

    /// <summary>
    /// Tells the Scan command to re-evaluate its CanExecute. MainViewModel
    /// calls this when the externally-blocked predicate's inputs change
    /// (Cleanup.IsOperating or Completion.IsComplete).
    /// </summary>
    public void NotifyExternallyBlockedChanged() =>
        ScanCommand.NotifyCanExecuteChanged();

    public string RegisteredSummaryText =>
        string.Format(
            DisplayHelpers.Pluralise(RegisteredFileCount,
                Strings.Summary_RegisteredStillUsed_Singular,
                Strings.Summary_RegisteredStillUsed_Plural),
            RegisteredFileCount);

    public string OrphanedSummaryText =>
        string.Format(
            DisplayHelpers.Pluralise(OrphanedFileCount,
                Strings.Summary_OrphanedToCleanUp_Singular,
                Strings.Summary_OrphanedToCleanUp_Plural),
            OrphanedFileCount);

    public bool HasMissingFromDisk => MissingNonRemovableCount > 0;

    public string MissingFromDiskSummaryText =>
        string.Format(
            DisplayHelpers.Pluralise(MissingNonRemovableCount,
                Strings.Summary_MissingFromDisk_Singular,
                Strings.Summary_MissingFromDisk_Plural),
            MissingNonRemovableCount);

    partial void OnRegisteredFileCountChanged(int value) =>
        OnPropertyChanged(nameof(RegisteredSummaryText));

    partial void OnOrphanedFileCountChanged(int value) =>
        OnPropertyChanged(nameof(OrphanedSummaryText));

    /// <summary>
    /// Runs the scan service and updates this VM's display fields.
    /// Used by the user-driven Scan command and by the splash startup
    /// scan. Does not raise <see cref="ScanCompleted"/>; that fires
    /// from <see cref="ScanAsync"/> and
    /// <see cref="ScanWithProgressAsync"/> after their respective
    /// success paths.
    /// </summary>
    private async Task RunScanCoreAsync(IProgress<string>? progress, CancellationToken cancellationToken = default)
    {
        // Compute everything off the call results before touching any
        // observable property; on throw or cancel the VM stays at its
        // prior consistent state.
        var result = await _scanService.ScanAsync(progress, cancellationToken);
        // Sample reboot after the scan; ordering matters. An MSI install
        // starting mid-scan could flip the _MSIExecute mutex, and
        // probing first would miss it.
        var pendingRebootResult = _rebootService.Check();

        var registeredCount = result.RegisteredPackages.Count;
        var registeredSize = DisplayHelpers.FormatSize(result.RegisteredTotalBytes);
        var orphanedCount = result.RemovableFiles.Count;
        var orphanedSize = DisplayHelpers.FormatSize(result.RemovableFiles.Sum(f => f.SizeBytes));

        PendingRebootResult = pendingRebootResult;
        LastScanResult = result;
        RegisteredFileCount = registeredCount;
        RegisteredSizeDisplay = registeredSize;
        OrphanedFileCount = orphanedCount;
        OrphanedSizeDisplay = orphanedSize;
        MissingNonRemovableCount = result.MissingNonRemovableCount;
        MissingRemovableCount = result.MissingRemovableCount;
        HasScanned = true;
    }

    /// <summary>
    /// User-driven scan command. Shows the scan overlay if the scan
    /// takes longer than 200ms, surfaces admin / DB / unknown errors
    /// to the dialog service, and updates <see cref="ScanProgress"/>
    /// throughout.
    /// </summary>
    private bool CanScan() => !IsScanning && !_isExternallyBlocked();

    [RelayCommand(CanExecute = nameof(CanScan))]
    private async Task ScanAsync()
    {
        ScanProgress = Strings.Status_StartingScan;
        var sw = Stopwatch.StartNew();
        var cts = new CancellationTokenSource();
        _scanCts = cts;

        try
        {
            var progress = new Progress<string>(message => ScanProgress = message);
            var scanTask = RunScanCoreAsync(progress, cts.Token);
            if (await Task.WhenAny(scanTask, Task.Delay(200, cts.Token)) != scanTask)
                IsScanning = true;
            await scanTask;

            sw.Stop();
            LastScanDurationMs = sw.ElapsedMilliseconds;
            ScanProgress = string.Format(Strings.Status_ScanComplete, DisplayHelpers.FormatElapsed(sw.Elapsed));
            ScanCompleted?.Invoke(this, EventArgs.Empty);
        }
        catch (OperationCanceledException)
        {
            ScanProgress = Strings.Status_ScanCancelled;
        }
        catch (UnauthorizedAccessException)
        {
            _dialogService.ShowWarning(
                Strings.Error_AdminRequiredBody,
                Strings.Error_AdminRequiredTitle);
            ScanProgress = Strings.Status_ScanAccessDenied;
        }
        catch (InvalidOperationException ex)
        {
            // ex.Message is safe here only because every
            // InstallerQueryService throw uses a resx-sourced message.
            // A path-bearing throw site would need to switch to the
            // type-name+crashlog pattern used in the generic catch.
            _dialogService.ShowError(ex.Message, Strings.Error_InstallerDbUnavailableTitle);
            ScanProgress = Strings.Status_ScanFailedDb;
        }
        catch (Exception ex)
        {
            // ex.Message never reaches UI: type name + log path only.
            var crash = CrashLog.TryWrite(ex);
            var typeName = ex.GetType().Name;
            var msg = crash.Written
                ? string.Format(Strings.Status_ScanFailedDetails, typeName, crash.Path)
                : string.Format(Strings.Status_ScanFailedDetails_NoLog, typeName);
            ScanProgress = msg;
            _dialogService.ShowError(msg, Strings.Error_ScanFailedTitle);
        }
        finally
        {
            // Capture, null, dispose: a concurrent CancelScanCommand
            // reading _scanCts after the null sees no CTS and no-ops.
            // Mirrors CleanupViewModel.DisposeOperationCts.
            var local = _scanCts;
            _scanCts = null;
            local?.Dispose();
            IsScanning = false;
        }
    }

    [RelayCommand]
    private void CancelScan()
    {
        try { _scanCts?.Cancel(); }
        catch (ObjectDisposedException) { /* scan already finished */ }
        // The progress reporter inside ScanAsync only fires on its
        // next callback; without a synchronous write the overlay
        // holds the previous step's text after Esc until that
        // callback runs. ScanAsync overwrites this on its next step.
        ScanProgress = Strings.Status_Cancelling;
    }

    /// <summary>
    /// Splash-driven startup scan. Caller controls the progress
    /// reporter (it pipes to the splash UI) and the cancellation token
    /// (it ties to the splash Cancel button). Raises
    /// <see cref="ScanCompleted"/> on success so MainViewModel can
    /// trigger the all-clear path if appropriate.
    /// </summary>
    public async Task ScanWithProgressAsync(IProgress<string>? progress, CancellationToken cancellationToken = default)
    {
        var sw = Stopwatch.StartNew();
        await RunScanCoreAsync(progress, cancellationToken);
        sw.Stop();
        LastScanDurationMs = sw.ElapsedMilliseconds;
        ScanProgress = string.Format(Strings.Status_ScanComplete, DisplayHelpers.FormatElapsed(sw.Elapsed));
        ScanCompleted?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>
    /// Silent refresh used by Cleanup after a Move or Delete completes.
    /// Skips the scan overlay (IsScanning stays false) so the operating
    /// overlay can stay visible until its own finally block clears it.
    /// </summary>
    public async Task RefreshAsync()
    {
        try
        {
            await RunScanCoreAsync(null);
            ScanCompleted?.Invoke(this, EventArgs.Empty);
        }
        catch
        {
            // Best-effort refresh. The completion screen still renders
            // from the cached pre-operation result; the next scan
            // command will retry with full error reporting.
        }
    }
}
