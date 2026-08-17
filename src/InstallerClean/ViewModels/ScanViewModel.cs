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
    /// and never at all by the silent refresh. It would leave the Move and
    /// Delete buttons live through the first 200 ms of a scan, which is long
    /// enough to start a destructive batch against the previous scan's result
    /// while a fresh scan walks the same folder, and to leave two scans writing
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

    // A non-localised PendingRebootLabel lived here for the result-log payload
    // and went with that field in schema 4. Nothing else ever read it: the state
    // reaches the screen through PendingRebootBannerText below and reaches both
    // action paths through their own fresh probe, neither of which wants a label.

    /// <summary>
    /// Localised banner text for the current Block reason; empty otherwise.
    ///
    /// The last arm is what a reason with no line of its own gets, and it is a
    /// string rather than a throw because of where this is read from: WPF calls
    /// it through a binding and swallows anything it throws as a binding error,
    /// so the user would meet a blank banner over greyed buttons with nothing on
    /// screen saying why. A generic sentence that is true of the whole family
    /// beats that. It is unreachable while the enum has three members, all of
    /// which are handled above. Adding a fourth is a failing test rather than a
    /// silent gap because Every_reason_has_a_banner_of_its_own walks the enum,
    /// and A_reason_with_no_banner_of_its_own_still_says_something covers this
    /// arm (ScanViewModelPendingRebootTests).
    /// </summary>
    public string PendingRebootBannerText => PendingRebootResult?.Reason switch
    {
        PendingRebootReason.MsiExecuteMutexHeld => Strings.Body_PendingReboot_MsiExecuteMutex,
        PendingRebootReason.InstallerInProgress => Strings.Body_PendingReboot_InstallerInProgress,
        PendingRebootReason.PendingRenameInCache => Strings.Body_PendingReboot_PendingRenameInCache,
        null => string.Empty,
        _ => Strings.Body_PendingReboot_Other,
    };

    /// <summary>
    /// The registrations this scan found naming a file that is not on disk AND whose
    /// absence it could not establish to be harmless. Drives the missing-files line, and
    /// it is the count the line prints as well as the condition that fires it, so the two
    /// cannot disagree.
    ///
    /// IT IS A HALF, AND WHICH HALF HAS MOVED TWICE. It was once the registrations
    /// carrying no superseded or obsoleted state, on the reading that such a file having
    /// gone was its expected end state; that reading is false, Windows opening every
    /// registered patch's cached file whichever state it carries. It was then every
    /// missing registration, which alarms past users of this app about files the app
    /// itself removed. It is now neither axis but the conjunction: benign means the state
    /// is superseded or obsoleted AND every product sharing the patch was shown to hold
    /// no patch that could be uninstalled and roll back onto the file.
    ///
    /// <see cref="MissingFilesReport.Affected"/> is that expression, named once, and the
    /// programs this line names come off the same predicate. The full total still travels
    /// in the scan result and in the report payload, where a public chart reads it with no
    /// version gate.
    /// </summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasMissingFromDisk))]
    [NotifyPropertyChangedFor(nameof(MissingFromDiskSummaryText))]
    private int _missingFromDiskCount;

    /// <summary>
    /// The programs those files belong to, as the one phrase the line names them
    /// in, already capped and joined by <see cref="MissingFilesReport"/> so the
    /// window and the command line say the same thing. Empty when there are none.
    /// </summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(MissingFromDiskSummaryText))]
    private string _missingFromDiskPrograms = string.Empty;

    /// <summary>
    /// Installed products the last scan could not account for, straight from
    /// <see cref="ScanResult.UnaccountedProductCount"/>, whose remarks say what
    /// goes into it and why it is neither confined to records that failed to read
    /// nor an exact headcount. Non-zero drives the line saying the records were
    /// not fully read. Nothing shows the figure itself.
    /// </summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasRecordsNotMatched))]
    [NotifyPropertyChangedFor(nameof(RecordsNotMatchedText))]
    private int _unaccountedProductCount;

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
    /// action zone (the backup folder, Move and Delete) hangs off this: with
    /// nothing found there is nothing for it to act on, and a greyed-out pair
    /// of buttons under copy that tells the user to press them reads as a
    /// broken app rather than a clean machine.
    /// </summary>
    public bool HasOrphans => HasScanned && OrphanedFileCount > 0;

    public bool HasMissingFromDisk => MissingFromDiskCount > 0;

    public string MissingFromDiskSummaryText =>
        string.Format(
            DisplayHelpers.Pluralise(MissingFromDiskCount,
                Strings.Summary_MissingFromDisk_Singular,
                Strings.Summary_MissingFromDisk_Plural,
                "Summary.MissingFromDisk"),
            MissingFromDiskCount, MissingFromDiskPrograms);

    /// <summary>
    /// True when the last scan could not account for everything the Windows
    /// Installer records hold, so it did not read all of them. Informational,
    /// unlike <see cref="HasMissingFromDisk"/>: nothing is wrong with the machine
    /// and there is nothing for the user to do. It is shown because a scan that
    /// saw less than usual without saying so is the fault this line exists to
    /// avoid, and what it now bears on is the missing-files line rather than the
    /// offer: a registration this scan never reached is one whose file, had it
    /// gone, went uncounted.
    ///
    /// The count gates the line and does not appear in it. Four different things
    /// contribute to it and only two are failures to read, so it is an estimate
    /// that can come out high as well as low; a figure on screen would be a
    /// precision the scan has not got.
    /// </summary>
    public bool HasRecordsNotMatched => UnaccountedProductCount > 0;

    public string RecordsNotMatchedText => Strings.Summary_RecordsNotMatched;

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

            // THE LEFT-ALONE LINE COUNTS THE WITHHELD FILES TOO, so that it and the
            // offer between them account for every file in the folder. THE PROPERTY
            // IS THE ACCOUNTING, NOT THE NUMBER OF TERMS: a withheld file is in
            // neither population otherwise, being neither offered nor a registered
            // row, because no registration names it. The two lines would then add up
            // to less than the folder holds with the difference shown nowhere, which
            // is exactly what this term exists to prevent. Anything added here later
            // has to close it again.
            //
            // NO CAUSE TRAVELS WITH IT to this line, and none may be added. What the
            // user sees is that the app left these alone, which is true of every file
            // in both populations; why any particular one was left is not a sentence
            // this line can carry.
            //
            // The Details window's own header counts the same two populations, off
            // the same lists, because the two are one click apart and a reader
            // comparing them must not find them disagreeing.
            var withheld = result.WithheldFiles ?? Array.Empty<OrphanedFile>();
            var registeredCount = result.RegisteredPackages.Count + withheld.Count;
            var registeredSize = DisplayHelpers.FormatSize(
                result.RegisteredTotalBytes + withheld.Sum(f => f.SizeBytes));
            var orphanedCount = result.RemovableFiles.Count;
            var orphanedSize = DisplayHelpers.FormatSize(result.RemovableFiles.Sum(f => f.SizeBytes));
            // Built here with the rest of the display state rather than in the
            // property, which is read on every binding refresh: it walks the whole
            // registered set and groups it, and the answer only changes when a
            // scan does.
            var missingPrograms = MissingFilesReport.Inline(
                MissingFilesReport.Products(result.RegisteredPackages));

            PendingRebootResult = pendingRebootResult;
            LastScanResult = result;
            RegisteredFileCount = registeredCount;
            RegisteredSizeDisplay = registeredSize;
            OrphanedFileCount = orphanedCount;
            OrphanedSizeDisplay = orphanedSize;
            // THE AFFECTED HALF, NOT THE SUM, which is the line item 5 moves back. The
            // banner fires where something could still reach for a file that is gone, so
            // a registration whose absence the app positively established to be harmless
            // is not in the count and its program is not named. Both come off the same
            // predicate in MissingFilesReport, and the sum still travels in the report
            // payload, where a public chart reads it with no version gate.
            MissingFromDiskCount = result.MissingAffectedCount;
            MissingFromDiskPrograms = missingPrograms;
            UnaccountedProductCount = result.UnaccountedProductCount;
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

    /// <summary>
    /// Tailored, safe-to-show message for the most recent scan that FAILED. Empty
    /// until the first failure, cleared at the start of every scan and left empty
    /// on a success. Both the user-driven Scan command and the startup scan set it
    /// through the one error ladder (<see cref="DescribeScanFailure"/>); the main
    /// window shows it in place of the not-yet-scanned copy, with Re-scan focused,
    /// so a failed startup scan opens the window with the diagnosis rather than
    /// exiting.
    /// </summary>
    public string LastScanError
    {
        get => _lastScanError;
        private set
        {
            if (SetProperty(ref _lastScanError, value))
                OnPropertyChanged(nameof(HasScanError));
        }
    }

    private string _lastScanError = string.Empty;

    /// <summary>True when the last scan failed and its message is on screen.</summary>
    public bool HasScanError => LastScanError.Length > 0;

    /// <summary>
    /// The scan's one error ladder: maps a scan (or act-time re-verify) failure to
    /// the message, dialog title and overlay status line the user should see, so
    /// the user-driven Scan command, the startup scan and the re-verify all
    /// diagnose a failure the same way instead of each inventing its own. The
    /// generic branch writes the crash log (its message names the log path), so
    /// this is called once per failure. <see cref="OperationCanceledException"/> is
    /// handled by its own catch and never reaches here.
    /// </summary>
    internal ScanFailure DescribeScanFailure(Exception ex) => ex switch
    {
        // LocalisedAccessException before UnauthorizedAccessException: it derives
        // from it and carries a precise, safe-to-echo resx message (e.g. "Access
        // denied enumerating installed products"), where the BCL type only earns
        // the generic "run as administrator" guidance.
        LocalisedAccessException =>
            new(ex.Message, Strings.Error_AdminRequiredTitle, IsError: false, Strings.Status_ScanAccessDenied),
        UnauthorizedAccessException =>
            new(Strings.Error_AdminRequiredBody, Strings.Error_AdminRequiredTitle, IsError: false, Strings.Status_ScanAccessDenied),
        LocalisedInvalidOperationException =>
            new(ex.Message, Strings.Error_InstallerDbUnavailableTitle, IsError: true, Strings.Status_ScanFailedDb),
        _ => DescribeUnexpectedScanFailure(ex),
    };

    private static ScanFailure DescribeUnexpectedScanFailure(Exception ex)
    {
        // ex.Message never reaches UI: type name plus log path only, because a
        // framework message from an elevated process can carry a path out of
        // another user's profile.
        var crash = CrashLog.TryWrite(ex);
        var typeName = ex.GetType().Name;
        var message = crash.Written
            ? string.Format(Strings.Status_ScanFailedDetails, typeName, crash.Path)
            : string.Format(Strings.Status_ScanFailedDetails_NoLog, typeName);
        return new ScanFailure(message, Strings.Error_ScanFailedTitle, IsError: true, message);
    }

    /// <summary>One rung of the scan error ladder: what to show and how.</summary>
    internal readonly record struct ScanFailure(string Message, string Title, bool IsError, string StatusLine);

    [RelayCommand(CanExecute = nameof(CanScan))]
    private async Task ScanAsync()
    {
        LastScanWasCancelled = false;
        LastScanError = string.Empty;
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
        catch (Exception ex)
        {
            // One error ladder, shared with the startup scan (which shows the
            // message inline in the window rather than a modal) and the act-time
            // re-verify. LastScanError is set on every path so the main window can
            // reflect a failed Re-scan the same way it reflects a failed startup
            // scan; the modal is the immediate feedback for the explicit click.
            var failure = DescribeScanFailure(ex);
            LastScanError = failure.Message;
            ScanProgress = failure.StatusLine;
            if (failure.IsError)
                _dialogService.ShowError(failure.Message, failure.Title);
            else
                _dialogService.ShowWarning(failure.Message, failure.Title);
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
        LastScanError = string.Empty;
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
        catch (Exception ex)
        {
            // A failed startup scan opens the main window in an error state rather
            // than exiting: record the tailored message through the one ladder,
            // shared with the Scan command, so the window's not-yet-scanned state
            // shows it with Re-scan focused. Do NOT rethrow: the exception then
            // propagates to App.OnStartup, which tells an already-elevated user to
            // run as administrator and exits. An app that diagnoses "your
            // installer database is empty" and then vanishes is strictly worse than
            // one that says it and offers Re-scan.
            LastScanError = DescribeScanFailure(ex).Message;
            return;
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
    ///
    /// <paramref name="cancellationToken"/> is what makes it interruptible, and
    /// it is not optional in practice: this is a full folder walk plus a full
    /// API enumeration, the folder has been measured at 6.4 million files, and
    /// the caller runs it behind an overlay the user has usually just pressed
    /// Cancel on. Without a token every checkpoint inside the scan is unreachable
    /// and the wait reads as a hang. A cancellation is swallowed like any other
    /// failure below: the counts stay as they were, which is the same outcome a
    /// failed refresh already has.
    /// </summary>
    public async Task RefreshAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            await RunScanCoreAsync(null, cancellationToken);
            ScanCompleted?.Invoke(this, EventArgs.Empty);
        }
        catch (OperationCanceledException)
        {
            // Cancelled refresh: the completion screen renders from the cached
            // pre-operation result and the counts behind it are stale until the
            // next scan. Not written to crash.log, unlike the failure below,
            // because the user asked for it.
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
