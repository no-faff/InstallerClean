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

    private CancellationTokenSource? _scanCts;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(ScanCommand))]
    private bool _isScanning;
    [ObservableProperty] private string _scanProgress = string.Empty;
    [ObservableProperty] private bool _hasScanned;

    /// <summary>
    /// Set by MainViewModel while another overlay is up. Gates the
    /// user-driven Scan command only; ScanWithProgressAsync (splash)
    /// and RefreshAsync (post-Move/Delete) bypass it.
    /// </summary>
    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(ScanCommand))]
    private bool _isExternallyBlocked;

    [ObservableProperty] private int _registeredFileCount;
    [ObservableProperty] private string _registeredSizeDisplay = string.Empty;
    [ObservableProperty] private int _orphanedFileCount;
    [ObservableProperty] private string _orphanedSizeDisplay = string.Empty;

    [ObservableProperty] private bool _hasPendingReboot;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasMissingFromDisk))]
    [NotifyPropertyChangedFor(nameof(MissingFromDiskSummaryText))]
    private int _missingFromDiskCount;

    /// <summary>
    /// Cached result of the most recent successful scan. Null until
    /// the first scan completes; remains the same instance until the
    /// next scan replaces it.
    /// </summary>
    public ScanResult? LastScanResult { get; private set; }

    /// <summary>
    /// Raised after every successful scan completes, including the
    /// initial startup scan. Subscribers can read
    /// <see cref="LastScanResult"/> at this point.
    /// </summary>
    public event EventHandler? ScanCompleted;

    public ScanViewModel(
        IFileSystemScanService scanService,
        IPendingRebootService rebootService,
        IDialogService dialogService)
    {
        _scanService = scanService;
        _rebootService = rebootService;
        _dialogService = dialogService;
    }

    public string RegisteredSummaryText =>
        string.Format(Strings.Summary_RegisteredStillUsed,
            RegisteredFileCount, DisplayHelpers.PluraliseFileVerb(RegisteredFileCount));

    public string OrphanedSummaryText =>
        string.Format(Strings.Summary_OrphanedToCleanUp,
            OrphanedFileCount, DisplayHelpers.PluraliseFile(OrphanedFileCount));

    public bool HasMissingFromDisk => MissingFromDiskCount > 0;

    public string MissingFromDiskSummaryText =>
        string.Format(Strings.Summary_MissingFromDisk,
            MissingFromDiskCount, DisplayHelpers.PluraliseFileVerb(MissingFromDiskCount));

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
        // Sample reboot AFTER the scan. A Windows Update queued during
        // a multi-second scan would otherwise let Move/Delete re-enable
        // on stale state.
        var pendingReboot = _rebootService.HasPendingReboot();

        var registeredCount = result.RegisteredPackages.Count;
        var registeredSize = DisplayHelpers.FormatSize(result.RegisteredTotalBytes);
        var orphanedCount = result.RemovableFiles.Count;
        var orphanedSize = DisplayHelpers.FormatSize(result.RemovableFiles.Sum(f => f.SizeBytes));
        var missingCount = result.MissingFromDiskCount;

        HasPendingReboot = pendingReboot;
        LastScanResult = result;
        RegisteredFileCount = registeredCount;
        RegisteredSizeDisplay = registeredSize;
        OrphanedFileCount = orphanedCount;
        OrphanedSizeDisplay = orphanedSize;
        MissingFromDiskCount = missingCount;
        HasScanned = true;
    }

    /// <summary>
    /// User-driven scan command. Shows the scan overlay if the scan
    /// takes longer than 200ms, surfaces admin / DB / unknown errors
    /// to the dialog service, and updates <see cref="ScanProgress"/>
    /// throughout.
    /// </summary>
    private bool CanScan() => !IsScanning && !IsExternallyBlocked;

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
            // Full detail to crash log; type name + log path to status
            // pill AND dialog (the pill alone gets clipped by
            // TextTrimming). ex.Message never reaches UI.
            var logPath = CrashLog.Write(ex);
            ScanProgress = string.Format(Strings.Status_ScanFailedDetails, ex.GetType().Name, logPath);
            _dialogService.ShowError(
                string.Format(Strings.Status_ScanFailedDetails, ex.GetType().Name, logPath),
                Strings.Error_ScanFailedTitle);
        }
        finally
        {
            _scanCts = null;
            cts.Dispose();
            IsScanning = false;
        }
    }

    [RelayCommand]
    private void CancelScan()
    {
        try { _scanCts?.Cancel(); }
        catch (ObjectDisposedException) { /* scan already finished */ }
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
