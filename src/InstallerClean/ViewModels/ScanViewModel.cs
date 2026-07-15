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

    /// <summary>
    /// Reveals the scanning overlay. Deliberately not the gate on any command:
    /// see <see cref="IsScanInFlight"/>.
    /// </summary>
    [ObservableProperty] private bool _isScanning;

    /// <summary>
    /// True from the first line of every scan (the Scan command, the splash
    /// startup scan and the silent post-operation refresh) until it ends.
    ///
    /// <see cref="IsScanning"/> cannot serve as this gate: it is an
    /// overlay-reveal flag, set only once a scan outlives the 200 ms delay,
    /// and never at all by the silent refresh. It left the Move and Delete
    /// buttons live through the first 200 ms of a scan, which is long enough
    /// to start a destructive batch against the previous scan's result while
    /// a fresh scan walks the same folder, and to leave two scans writing
    /// <see cref="LastScanResult"/> with no ordering between them.
    /// </summary>
    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(ScanCommand))]
    private bool _isScanInFlight;

    [ObservableProperty] private string _scanProgress = string.Empty;

    /// <summary>
    /// Per-product ticker line under the scan overlay's milestone text.
    /// Display-only: the bound TextBlock carries no LiveSetting because
    /// the ticker updates once per registered product, up to hundreds in
    /// a few seconds, and a live region would queue an announcement for
    /// every one of them.
    /// </summary>
    [ObservableProperty] private string _scanTicker = string.Empty;

    /// <summary>
    /// True once a scan has completed. False before the first one, which is a
    /// state the user reaches by cancelling the startup scan, and the main
    /// window has to say so rather than paint a zeroed scan result.
    /// </summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasOrphans))]
    private bool _hasScanned;

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
    /// gone from disk. Drives the diagnostic-info line under the body
    /// explanation.
    /// </summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasStaleMsiEntries))]
    [NotifyPropertyChangedFor(nameof(StaleMsiEntriesText))]
    private int _missingRemovableCount;

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
                Strings.Summary_RegisteredStillUsed_Plural,
                "Summary.RegisteredStillUsed"),
            RegisteredFileCount);

    public string OrphanedSummaryText =>
        string.Format(
            DisplayHelpers.Pluralise(OrphanedFileCount,
                Strings.Summary_OrphanedToCleanUp_Singular,
                Strings.Summary_OrphanedToCleanUp_Plural,
                "Summary.OrphanedToCleanUp"),
            OrphanedFileCount);

    /// <summary>
    /// True when the last scan found files to clean up. The main window's
    /// action zone (the Move location, Move and Delete) hangs off this: with
    /// nothing found there is nothing for it to act on, and a greyed-out pair
    /// of buttons under copy that tells the user to press them reads as a
    /// broken app rather than a clean machine.
    /// </summary>
    public bool HasOrphans => HasScanned && OrphanedFileCount > 0;

    public bool HasMissingFromDisk => MissingNonRemovableCount > 0;

    public string MissingFromDiskSummaryText =>
        string.Format(
            DisplayHelpers.Pluralise(MissingNonRemovableCount,
                Strings.Summary_MissingFromDisk_Singular,
                Strings.Summary_MissingFromDisk_Plural,
                "Summary.MissingFromDisk"),
            MissingNonRemovableCount);

    /// <summary>
    /// True when the MSI database carries superseded-patch registrations
    /// whose underlying files are already gone from disk. Distinct from
    /// <see cref="HasMissingFromDisk"/>: that case is load-bearing
    /// (Windows still claims the file but it's gone, so a future
    /// install/uninstall/patch will fail); this case is benign (Windows
    /// considers the patch removable, the file having gone is the
    /// expected end state). Surfaced as a small informational line.
    /// </summary>
    public bool HasStaleMsiEntries => MissingRemovableCount > 0;

    public string StaleMsiEntriesText =>
        string.Format(
            DisplayHelpers.Pluralise(MissingRemovableCount,
                Strings.Summary_StaleMsiEntries_Singular,
                Strings.Summary_StaleMsiEntries_Plural,
                "Summary.StaleMsiEntries"),
            MissingRemovableCount);

    partial void OnRegisteredFileCountChanged(int value) =>
        OnPropertyChanged(nameof(RegisteredSummaryText));

    partial void OnOrphanedFileCountChanged(int value)
    {
        OnPropertyChanged(nameof(OrphanedSummaryText));
        OnPropertyChanged(nameof(HasOrphans));
    }

    /// <summary>
    /// Runs the scan service and updates this VM's display fields.
    /// Used by the user-driven Scan command and by the splash startup
    /// scan. Does not raise <see cref="ScanCompleted"/>; that fires
    /// from <see cref="ScanAsync"/> and
    /// <see cref="ScanWithProgressAsync"/> after their respective
    /// success paths.
    /// </summary>
    private async Task RunScanCoreAsync(IProgress<ScanProgressUpdate>? progress, CancellationToken cancellationToken = default)
    {
        // Set before the first await, so it is already true when the caller's
        // command returns to the dispatcher: every scan entry point routes
        // through here, so this is the one place the gate can be complete.
        IsScanInFlight = true;
        try
        {
            // Compute everything off the call results before touching any
            // observable property; on throw or cancel the VM stays at its
            // prior consistent state.
            var result = await _scanService.ScanAsync(progress, cancellationToken);
            // Sample reboot after the scan; ordering matters. An MSI install
            // starting mid-scan could flip the _MSIExecute mutex, and
            // probing first would miss it.
            var pendingRebootResult = await Task.Run(() => _rebootService.Check(), cancellationToken);

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
        finally
        {
            IsScanInFlight = false;
        }
    }

    /// <summary>
    /// User-driven scan command. Shows the scan overlay if the scan
    /// takes longer than 200ms, surfaces admin / DB / unknown errors
    /// to the dialog service, and updates <see cref="ScanProgress"/>
    /// throughout.
    /// </summary>
    private bool CanScan() => !IsScanInFlight && !_isExternallyBlocked();

    /// <summary>
    /// True when the most recent scan ended because the user cancelled it
    /// (rather than completing or failing). The view reads this when the
    /// scanning overlay collapses to re-announce "Scan cancelled." past the
    /// focus move that would otherwise swallow it, and the main window's
    /// not-yet-scanned state reads it to say why there is nothing on screen.
    /// Reset at the start of every scan.
    ///
    /// Observable, not a plain property: the startup scan is the one that gets
    /// cancelled in practice, and it sets this without ever setting
    /// <see cref="HasScanned"/>, so nothing else raises for the window to
    /// re-read it.
    /// </summary>
    public bool LastScanWasCancelled
    {
        get => _lastScanWasCancelled;
        private set => SetProperty(ref _lastScanWasCancelled, value);
    }

    private bool _lastScanWasCancelled;

    [RelayCommand(CanExecute = nameof(CanScan))]
    private async Task ScanAsync()
    {
        LastScanWasCancelled = false;
        ScanProgress = Strings.Status_StartingScan;
        ScanTicker = string.Empty;
        var sw = Stopwatch.StartNew();
        var cts = new CancellationTokenSource();
        _scanCts = cts;

        try
        {
            var progress = new Progress<ScanProgressUpdate>(ApplyProgressUpdate);
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
            LastScanWasCancelled = true;
            ScanProgress = Strings.Status_ScanCancelled;
        }
        catch (LocalisedAccessException ex)
        {
            // LocalisedAccessException carries a safe-to-echo resx
            // message; surfacing it preserves the precise diagnosis
            // (e.g., "Access denied enumerating installed products")
            // rather than the generic "Run as administrator" guidance
            // the BCL-UAE branch below shows. Order matters: this
            // catch must precede catch (UnauthorizedAccessException)
            // because LocalisedAccessException inherits from it.
            _dialogService.ShowWarning(ex.Message, Strings.Error_AdminRequiredTitle);
            ScanProgress = Strings.Status_ScanAccessDenied;
        }
        catch (UnauthorizedAccessException)
        {
            _dialogService.ShowWarning(
                Strings.Error_AdminRequiredBody,
                Strings.Error_AdminRequiredTitle);
            ScanProgress = Strings.Status_ScanAccessDenied;
        }
        catch (LocalisedInvalidOperationException ex)
        {
            // LocalisedInvalidOperationException is the contract: sites
            // that raise it have built Message from a resx string with
            // only fixed-shape template args (counts, error codes), so
            // echoing under elevation is safe. BCL-raised
            // InvalidOperationException from deep in the framework falls
            // through to the generic catch below with type-name + crash
            // log only.
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

    /// <summary>
    /// Routes one scan progress update to the overlay's two lines:
    /// milestones to the announced status text, the per-product ticker
    /// to the display-only line beneath it. A milestone also clears the
    /// ticker so the last product name does not sit stale beside the
    /// next phase's message.
    /// </summary>
    private void ApplyProgressUpdate(ScanProgressUpdate update)
    {
        if (update.IsMilestone)
        {
            ScanProgress = update.Message;
            ScanTicker = string.Empty;
        }
        else
        {
            ScanTicker = update.Message;
        }
    }

    [RelayCommand]
    private void CancelScan()
    {
        // Set the status before cancelling. The synchronous write updates
        // the overlay the instant Esc is pressed (the ScanAsync progress
        // reporter only fires on its next callback). Ordering it before
        // _scanCts.Cancel() also guarantees ScanAsync's own
        // "Scan cancelled." write lands after this one: cancelling first
        // leaves a window where the scan can complete and write
        // "Scan cancelled." before this line overwrites it with
        // "Cancelling...", harmless on the single UI thread but a race in
        // a SynchronizationContext-free unit test.
        ScanProgress = Strings.Status_Cancelling;
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
    public async Task ScanWithProgressAsync(IProgress<ScanProgressUpdate>? progress, CancellationToken cancellationToken = default)
    {
        LastScanWasCancelled = false;
        var sw = Stopwatch.StartNew();
        try
        {
            await RunScanCoreAsync(progress, cancellationToken);
        }
        catch (OperationCanceledException)
        {
            // Cancelling the splash leaves the app with no scan at all, and the
            // main window says so. Recording it here (the caller lets the
            // cancellation through to close the splash) is what lets that window
            // say why, rather than showing a bare "nothing scanned yet" to a user
            // who knows perfectly well they pressed Cancel.
            LastScanWasCancelled = true;
            throw;
        }
        sw.Stop();
        LastScanDurationMs = sw.ElapsedMilliseconds;
        ScanProgress = string.Format(Strings.Status_ScanComplete, DisplayHelpers.FormatElapsed(sw.Elapsed));
        ScanCompleted?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>
    /// Re-checks the pending-reboot gate at the moment of action, off the
    /// dispatcher, and updates <see cref="PendingRebootResult"/> so the banner
    /// paints and the Move/Delete commands drop out through the existing
    /// <see cref="HasPendingReboot"/> wiring. Returns true when the gate now
    /// blocks. The scan samples the gate once; a Windows Installer transaction
    /// that starts after that sample, hours later if the window sits open, is
    /// invisible until this runs immediately before a Move or Delete acts.
    /// Mirrors the scan's own off-dispatcher Check() so a registry or mutex read
    /// cannot stall the UI thread.
    /// </summary>
    public async Task<bool> RecheckPendingRebootAsync()
    {
        var result = await Task.Run(() => _rebootService.Check());
        PendingRebootResult = result;
        return result.IsBlocked;
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
        catch (Exception ex)
        {
            // Best-effort refresh. The completion screen still renders
            // from the cached pre-operation result; the next scan
            // command will retry with full error reporting. The failure
            // is logged rather than swallowed silently: this is the one
            // path that leaves stale registered and orphaned counts on
            // the completion screen, so "the counts were wrong after
            // cleaning up" needs a trail in crash.log to be diagnosable.
            CrashLog.Write(ex);
        }
    }
}
