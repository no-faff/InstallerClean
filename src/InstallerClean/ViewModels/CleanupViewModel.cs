using System.ComponentModel;
using System.IO.Abstractions;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using InstallerClean.Helpers;
using InstallerClean.Models;
using InstallerClean.Resources;
using InstallerClean.Services;

namespace InstallerClean.ViewModels;

/// <summary>
/// Cleanup slice: the move-destination input plus the Move and Delete
/// commands and their progress overlay. Reads orphans from
/// <see cref="ScanViewModel.LastScanResult"/>, pushes outcomes into
/// <see cref="CompletionViewModel"/>, and asks the scan VM to refresh
/// after each successful operation.
/// </summary>
public partial class CleanupViewModel : ObservableObject, IDisposable
{
    private readonly IMoveFilesService _moveService;
    private readonly IDeleteFilesService _deleteService;
    private readonly ISettingsService _settingsService;
    private readonly IDialogService _dialogService;
    private readonly IConfirmationService _confirmationService;
    private readonly IFileSystem _fs;
    private readonly ScanViewModel _scan;
    private readonly CompletionViewModel _completion;
    private readonly IResultLogService _resultLogService;
    private readonly IRemovableReverifier _reverifier;
    private readonly PropertyChangedEventHandler _scanHandler;

    private CancellationTokenSource? _operationCts;
    private CancellationTokenSource? _moveDestinationSaveCts;
    private AppSettings _settings;

    /// <summary>
    /// Debounce window for write-back of MoveDestination edits. Each
    /// keystroke cancels the previous pending save; the save runs only
    /// if the user stops typing for this long. 400ms is roughly half a
    /// comfortable keystroke interval.
    /// </summary>
    /// <remarks>
    /// Exposed as <c>internal</c> so MainViewModelTests can wait on this
    /// value plus a small margin instead of hardcoding 700 ms (which
    /// drifts silently if the constant is ever tuned).
    /// </remarks>
    internal static readonly TimeSpan MoveDestinationSaveDelay = TimeSpan.FromMilliseconds(400);

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(MoveButtonTooltip))]
    private string _moveDestination = string.Empty;

    // Reveals the operating overlay. It is not the "work is underway" flag:
    // it goes false during the confirm-dialog window between the pre-flight
    // and the move call (so the modal owns the foreground state), it is set
    // only after 200 ms during the Move pre-flight, and the recycle probe
    // deliberately never sets it. IsOperationInFlight is the execution gate.
    [ObservableProperty] private bool _isOperating;

    /// <summary>
    /// True from the first line of a Move or a Delete until it has finished,
    /// pre-flight included, whether or not <see cref="IsOperating"/> ever
    /// raised the overlay. This is what the commands gate on.
    ///
    /// The pre-flights are the reason it exists. Both hop off the dispatcher
    /// (a Win32 path resolve or a shell recycle probe against a mapped drive
    /// can stall for the SMB timeout, and on the dispatcher that freezes the
    /// window), and while one is awaited the message loop pumps with no
    /// overlay up. Without this flag the other destructive command is still
    /// clickable in that window, and a Delete started during a Move pre-flight
    /// would overwrite <see cref="_operationCts"/> while the Move was still
    /// using it, leaving the Move's Cancel button wired to the Delete's token.
    /// </summary>
    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(MoveAllCommand))]
    [NotifyCanExecuteChangedFor(nameof(DeleteAllCommand))]
    private bool _isOperationInFlight;
    [ObservableProperty] private string _operationProgress = string.Empty;
    [ObservableProperty] private int _operationCurrentFile;
    [ObservableProperty] private int _operationTotalFiles;
    [ObservableProperty] private string _operationCurrentFileName = string.Empty;
    [ObservableProperty] private double _operationProgressPercent;

    /// <summary>
    /// Throttled copy of <see cref="OperationProgressDetail"/> for the
    /// screen-reader live region: updated on the first file, the last
    /// file and each time the batch crosses a tenth, never per file. A
    /// live region announces every text change, and per-file changes
    /// queue speech far faster than it can be spoken.
    /// </summary>
    [ObservableProperty] private string _operationProgressAnnouncement = string.Empty;

    /// <summary>Last tenth-of-batch boundary announced; -1 between operations.</summary>
    private int _lastAnnouncedDecile = -1;

    /// <summary>
    /// True between a Cancel click and the worker's next
    /// CancellationToken checkpoint. Gates the overlay Cancel button's
    /// IsEnabled binding and the disabled-state tooltip.
    /// </summary>
    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(CancelOperationCommand))]
    private bool _isCancellationRequested;

    public CleanupViewModel(
        IMoveFilesService moveService,
        IDeleteFilesService deleteService,
        ISettingsService settingsService,
        IDialogService dialogService,
        IConfirmationService confirmationService,
        IFileSystem fileSystem,
        ScanViewModel scan,
        CompletionViewModel completion,
        IResultLogService resultLogService,
        IRemovableReverifier reverifier)
    {
        _moveService = moveService;
        _deleteService = deleteService;
        _settingsService = settingsService;
        _dialogService = dialogService;
        _confirmationService = confirmationService;
        _fs = fileSystem;
        _scan = scan;
        _completion = completion;
        _resultLogService = resultLogService;
        _reverifier = reverifier;

        _settings = settingsService.Load();
        MoveDestination = _settings.MoveDestination;

        // Re-evaluate Move/Delete CanExecute when the upstream scan
        // VM's state changes. Held as a field so Dispose can unhook it;
        // the singleton container disposes this VM on shutdown.
        _scanHandler = OnScanPropertyChanged;
        _scan.PropertyChanged += _scanHandler;
    }

    private void OnScanPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(ScanViewModel.IsScanInFlight) ||
            e.PropertyName == nameof(ScanViewModel.OrphanedFileCount) ||
            e.PropertyName == nameof(ScanViewModel.HasPendingReboot))
        {
            MoveAllCommand.NotifyCanExecuteChanged();
            DeleteAllCommand.NotifyCanExecuteChanged();
        }
    }

    public void Dispose()
    {
        _scan.PropertyChanged -= _scanHandler;
        DisposeOperationCts();
        var saveCts = _moveDestinationSaveCts;
        _moveDestinationSaveCts = null;
        saveCts?.Cancel();
        saveCts?.Dispose();

        // A non-null CTS means an edit is still waiting out its debounce: the
        // field is nulled once a save lands. Cancelling it, which is all this
        // did, threw the edit away, so a destination typed or picked within
        // 400 ms of the window closing was never persisted and the box came
        // back empty (or holding the old path) next session. The language
        // switch, which relaunches the app, went through the same Dispose and
        // lost the same edit.
        //
        // Flushing it here is safe: the debounced save's own continuation is
        // posted to a dispatcher that is about to stop pumping, so it would not
        // run anyway, and SettingsService.Update takes its own lock, reloads,
        // applies and saves atomically, so it cannot race the write it just
        // cancelled. (The old comment justified the cancel by saying a late
        // write could land "after the VM is gone". Update touches no view-model
        // state, so that was never a hazard.)
        if (saveCts is not null)
            _settingsService.Update(s => s.MoveDestination = _settings.MoveDestination);
    }

    // Move and Delete gate on IsOperationInFlight, which is already set when
    // IsOperating flips; only the overlay's Cancel button reads IsOperating.
    partial void OnIsOperatingChanged(bool value) =>
        CancelOperationCommand.NotifyCanExecuteChanged();

    // The Move button works from either state, so this says what pressing it
    // will do rather than naming a missing step: with no destination set it
    // warns that a folder browser opens first, so the browser is expected
    // rather than a surprise.
    public string MoveButtonTooltip =>
        string.IsNullOrWhiteSpace(MoveDestination)
            ? Strings.Tooltip_MoveNeedsDestination
            : Strings.Tooltip_Move;

    partial void OnMoveDestinationChanged(string value)
    {
        // settings.json holds the trimmed string so a reader (CLI /m,
        // next session start) gets a normalised path. The TextBox
        // binding keeps the typed value mid-session.
        var normalised = value?.Trim() ?? string.Empty;
        if (string.Equals(_settings.MoveDestination, normalised, StringComparison.Ordinal))
            return;

        _settings.MoveDestination = normalised;
        ScheduleMoveDestinationSave();
    }

    /// <summary>
    /// Debounced write-back. Each call cancels the previous pending
    /// save so a typist doesn't fire a save per character; the actual
    /// save fires after <see cref="MoveDestinationSaveDelay"/>.
    /// </summary>
    private void ScheduleMoveDestinationSave()
    {
        var previous = _moveDestinationSaveCts;
        var cts = new CancellationTokenSource();
        _moveDestinationSaveCts = cts;
        previous?.Cancel();
        previous?.Dispose();

        _ = SaveAfterDelayAsync(cts.Token);
    }

    private async Task SaveAfterDelayAsync(CancellationToken token)
    {
        try
        {
            await Task.Delay(MoveDestinationSaveDelay, token);
        }
        catch (OperationCanceledException)
        {
            return;
        }

        // Reload-then-merge happens off the dispatcher because Load()
        // and TrySave() are disk hops (OneDrive-redirected or network-
        // roaming profiles bite hardest). The current MoveDestination
        // is captured on the dispatcher BEFORE the worker starts, so a
        // keystroke landing during the disk hop writes into
        // _settings.MoveDestination on the dispatcher and gets picked
        // up by the next debounce cycle without disturbing this save.
        // The _settings field is NOT reassigned; an earlier version of this
        // method swapped _settings = fresh and lost a keystroke that landed on
        // the orphan instance between the worker's snapshot read and the field
        // swap. SettingsService.Update reloads, applies the captured destination
        // and saves atomically under its own lock, so this thread-pool debounce
        // cannot lose the result-log lifetime lock or the language pick that run
        // on the dispatcher to a last-writer-wins rename.
        var destinationSnapshot = _settings.MoveDestination;
        await Task.Run(() =>
        {
            if (!_settingsService.Update(s => s.MoveDestination = destinationSnapshot))
            {
                // Update returns false on disk-full / read-only-profile /
                // path-redirection failure (it never throws). Without a
                // breadcrumb a user report of "my destination reset between
                // sessions" has no trail back to the failed write.
                CrashLog.TryWrite(new InvalidOperationException(
                    "Settings.Update returned false during MoveDestination debounced save."));
            }
        }, token).ConfigureAwait(true);

        // Dispose the type-once-and-stop case (every other path is
        // covered by the next schedule call replacing the field).
        // Token equality skips disposal if a fresh keystroke already
        // installed a new CTS during the await.
        if (_moveDestinationSaveCts is { } current && current.Token == token)
        {
            _moveDestinationSaveCts = null;
            current.Dispose();
        }
    }

    // Move and Delete are gated on HasPendingReboot for three reasons:
    // an MSI is in flight, a previous transaction is suspended, or a queued
    // post-reboot rename targets the cache (see IPendingRebootService).
    // The banner is informational only; the CanExecute predicate is what
    // stops a click from reaching the service.
    // Both gates are execution flags, not the overlay flags that used to stand
    // in for them: IsScanning is only set once a scan passes 200 ms, and
    // IsOperating is unset through both pre-flights. Reading either here left
    // a window in which the other destructive command was still clickable.
    // Deliberately identical to CanDelete: Move does NOT require a
    // destination to be typed first. A Move with an empty box asks where to
    // put the files and carries on, so gating the button on the box would
    // hide the action from anyone moving through the window by keyboard.
    // A disabled control is skipped by Tab entirely, so the button, its
    // spoken description and the tooltip explaining what it needs were all
    // unreachable without a mouse, which is the one route a screen-reader
    // user does not have.
    private bool CanMove() =>
        !_scan.IsScanInFlight && !IsOperationInFlight
        && !_scan.HasPendingReboot
        && _scan.OrphanedFileCount > 0;

    private bool CanDelete() =>
        !_scan.IsScanInFlight && !IsOperationInFlight
        && !_scan.HasPendingReboot
        && _scan.OrphanedFileCount > 0;

    [RelayCommand]
    private void BrowseDestination()
    {
        var chosen = _confirmationService.AskForMoveDestination();
        if (chosen is not null) MoveDestination = chosen;
    }

    private bool CanCancelOperation() => IsOperating && !IsCancellationRequested;

    [RelayCommand(CanExecute = nameof(CanCancelOperation))]
    private void CancelOperation()
    {
        IsCancellationRequested = true;
        // Races the finally block that disposes _operationCts;
        // ObjectDisposedException here means the operation finished
        // before the click reached the dispatcher.
        try { _operationCts?.Cancel(); }
        catch (ObjectDisposedException) { }
        // The move/delete loop only repaints OperationProgress on its
        // next iteration. Without a synchronous write the overlay holds
        // "Moving 23 of 100..." for one iteration past the click. The
        // operation loop then clears it, or replaces it with a partial-
        // progress line, once the loop observes the cancellation.
        OperationProgress = Strings.Status_Cancelling;
    }

    [RelayCommand(CanExecute = nameof(CanMove))]
    private async Task MoveAllAsync()
    {
        if (_scan.LastScanResult is null) return;

        // Move is offered without a destination typed, so this is where one
        // gets asked for. Cancelling the browser abandons the move and
        // touches nothing; the box is left as it was, empty.
        if (string.IsNullOrWhiteSpace(MoveDestination))
        {
            var chosen = _confirmationService.AskForMoveDestination();
            if (chosen is null) return;
            MoveDestination = chosen;
        }

        // Capture pre-operation scan state for the result-log entry.
        // RefreshAsync below replaces _scan.LastScanResult with the
        // post-move state, so the diagnostic record needs the pre-move
        // result captured here.
        var preOpScan = _scan.LastScanResult;
        var preOpDurationMs = _scan.LastScanDurationMs;
        var preOpRebootLabel = _scan.PendingRebootLabel;

        var dest = MoveDestination.Trim();

        // Relative destinations are refused before anything resolves or creates
        // them. Every gate below goes through Path.GetFullPath, which expands a
        // bare "backup" against the process CWD, so the pre-flight would create
        // that folder and write its probe file somewhere the user never named,
        // elevated, and only then would MoveFilesService refuse the batch, after
        // the confirmation dialog. The CWD is wherever the process happened to be
        // started from and nothing in the app constrains it, so the two
        // out-of-bounds gates below catching such a path is luck, not a gate.
        // Same three checks in the same order as the CLI's
        // ResolveAndValidateMoveDestination; MoveFilesService keeps its own copy
        // at the service boundary for callers that never come through here.
        if (!Path.IsPathFullyQualified(dest))
        {
            _dialogService.ShowWarning(
                string.Format(Strings.Error_DestinationNotFullyQualified, dest),
                Strings.Error_InvalidDestinationTitle);
            return;
        }

        // Every touch of the destination happens here, on a worker thread and
        // under one cancellable task: the two path gates, the CreateDirectory
        // and write probe, the drive classification and the free-space query.
        // Each of them resolves or queries a path through Win32, so each can
        // stall for the SMB timeout on a mapped drive or a UNC share that has
        // gone away, and any of them on the dispatcher freezes the window.
        // The CTS is created first so the overlay's Cancel button can
        // interrupt the wait. The probe goes through IFileSystem so
        // MockFileSystem-backed tests don't hit real disk; the gates and the
        // free-space query deliberately do not (a mock must not be able to
        // talk its way past a safety check).
        //
        // Nothing here touches view-model state: the verdict comes back as a
        // record and is applied below, on the dispatcher.
        _operationCts = new CancellationTokenSource();
        var probeToken = _operationCts.Token;
        IsOperationInFlight = true;
        var probeTask = Task.Run(() =>
        {
            // Never let files move back inside C:\Windows\Installer.
            // ResolveFinalPath inside IsInstallerFolderOrChild expands
            // junctions so a reparse-point destination cannot smuggle the
            // batch into the cache folder. Both gates run before the probe
            // creates anything.
            if (InstallerCacheHelpers.IsInstallerFolderOrChild(dest))
                return DestinationPreFlight.Rejected(insideInstallerCache: true);

            // %SystemRoot%, %ProgramFiles%, %ProgramFiles(x86)% and
            // %ProgramData% sit on the Win32 DLL search path and the SxS
            // resolution path: a process searching those paths trusts a
            // planted file at load time.
            if (InstallerCacheHelpers.IsSystemFolderOrChild(dest))
                return DestinationPreFlight.Rejected(insideSystemFolder: true);

            _fs.Directory.CreateDirectory(dest);
            probeToken.ThrowIfCancellationRequested();
            var probe = _fs.Path.Combine(dest, _fs.Path.GetRandomFileName());
            _fs.File.WriteAllBytes(probe, Array.Empty<byte>());
            probeToken.ThrowIfCancellationRequested();
            _fs.File.Delete(probe);
            probeToken.ThrowIfCancellationRequested();

            // A same-volume move is a rename and consumes no space, so the
            // free-space check would otherwise refuse exactly the nearly-full
            // system drive this app exists for; the caller applies the check
            // only when the move really copies.
            return new DestinationPreFlight(
                false, false,
                ClassifyMoveDestination(dest),
                StorageHelpers.GetAvailableFreeSpace(dest));
        }, probeToken);

        DestinationPreFlight preFlight;
        try
        {
            // Reveal the operating overlay only if the probe is slow, the way
            // the scan waits 200 ms before showing its overlay. A local
            // destination probes in well under that, so the overlay (and a
            // screen reader's "Preparing destination folder...") never flashes
            // in the instant before the confirm dialog; a UNC share that
            // stalls on the SMB timeout still gets the overlay and its
            // cancellable Cancel button. Heading before IsOperating keeps the
            // start announced exactly once if the overlay does appear.
            if (await Task.WhenAny(probeTask, Task.Delay(200, probeToken)) != probeTask)
            {
                OperationProgress = Strings.Status_PreparingDestination;
                IsOperating = true;
            }
            preFlight = await probeTask;
            // A Cancel clicked while the probe was in flight can lose the race
            // with a probe that completes anyway (the token is only checked
            // between the probe's filesystem calls). Without this the click is
            // honoured too late: the confirmation dialog opens, and only the
            // move that follows fails on the already-cancelled token.
            probeToken.ThrowIfCancellationRequested();
        }
        catch (OperationCanceledException)
        {
            IsOperating = false;
            OperationProgress = string.Empty;
            DisposeOperationCts();
            return;
        }
        catch (Exception ex)
        {
            IsOperating = false;
            OperationProgress = string.Empty;
            // ex.Message stays out of the dialog: path-leak risk under elevation.
            var crash = CrashLog.TryWrite(ex);
            DisposeOperationCts();
            _dialogService.ShowWarning(
                DescribeWriteFailure(dest, ex, crash.Path, crash.Written),
                Strings.Error_DestinationWriteFailedTitle);
            return;
        }
        IsOperating = false;
        OperationProgress = string.Empty;

        if (preFlight.InsideInstallerCache)
        {
            DisposeOperationCts();
            _dialogService.ShowWarning(
                Strings.Error_DestinationInsideInstaller,
                Strings.Error_InvalidDestinationTitle);
            return;
        }

        if (preFlight.InsideSystemFolder)
        {
            DisposeOperationCts();
            _dialogService.ShowWarning(
                string.Format(Strings.Error_DestinationInSystemFolder, dest),
                Strings.Error_InvalidDestinationTitle);
            return;
        }

        var removableFiles = _scan.LastScanResult.RemovableFiles;
        var filePaths = removableFiles.Select(f => f.FullPath).ToList();
        var count = filePaths.Count;
        var totalBytes = removableFiles.Sum(f => f.SizeBytes);
        var sizeDisplay = _scan.OrphanedSizeDisplay;

        var destinationKind = preFlight.Kind;

        // Free-space check. Skipped for a same-drive move (a rename frees
        // nothing and needs nothing) and silently for paths the API can't
        // measure (UNC shares where the caller lacks query rights, etc).
        if (destinationKind != MoveDestinationKinds.SameDrive
            && preFlight.AvailableFreeSpace is long free && free < totalBytes)
        {
            // Pre-flight CTS no longer needed; dispose before returning.
            DisposeOperationCts();
            OperationProgress = string.Empty;
            _dialogService.ShowWarning(
                string.Format(Strings.Error_NotEnoughSpaceBody,
                    dest,
                    DisplayHelpers.FormatSize(totalBytes),
                    DisplayHelpers.FormatSize(free)),
                Strings.Error_NotEnoughSpaceTitle);
            return;
        }

        // Use the captured `dest` consistently from here through the
        // move call. Reading MoveDestination live would re-read whatever
        // is in the textbox at that instant; if the user managed to
        // change it between the IsInstallerFolderOrChild validation and
        // here, the validated path and the moved-to path would diverge.
        // The pre-flight already classified the volume, so the confirmation can
        // tell the user the one thing this app exists to be right about: a move
        // to the drive the files are already on frees nothing until the copies
        // parked there are deleted.
        if (!_confirmationService.ConfirmMove(count, sizeDisplay, dest,
                destinationKind == MoveDestinationKinds.SameDrive))
        {
            // User cancelled at the confirmation dialog. The pre-flight
            // CTS is no longer needed; dispose it before returning.
            DisposeOperationCts();
            OperationProgress = string.Empty;
            return;
        }

        // Re-check the pending-reboot gate at the moment of action, after the
        // confirmation and immediately before acting. The scan sampled it once; a
        // user who left the confirmation dialog open while a Windows Installer
        // transaction started would otherwise move files against a live install,
        // the one condition the gate exists to block. Blocked => the banner paints
        // via HasPendingReboot and both commands drop out; refuse without calling
        // the service. (MoveInsteadAsync routes through MoveAllCommand, so it
        // inherits this check.)
        if (await _scan.RecheckPendingRebootAsync())
        {
            DisposeOperationCts();
            OperationProgress = string.Empty;
            return;
        }

        // Heading before IsOperating: a heading assigned after the reveal
        // can be spoken twice (see OperationHeadingText in MainWindow.xaml).
        OperationProgress = string.Format(
            DisplayHelpers.Pluralise(count, Strings.Status_Moving, "Status.Moving"),
            count, DisplayHelpers.PluraliseFile(count));
        IsOperating = true;

        try
        {
            // Re-verify the removable set against the API immediately before
            // acting, behind the overlay. This closes the one window neither the
            // fresh gate nor the mutex hold can see: a patch whose state changed
            // AND settled between the scan and the click (a superseded patch
            // reverted to Applied because its superseding patch was uninstalled).
            // It re-runs the enumeration (a beat) and can fail; a failure must STOP
            // the batch, never act on an un-verified set, so it is surfaced through
            // the scan's own error ladder. A cancellation propagates to the outer
            // OCE catch.
            ReverifyResult reverify;
            try
            {
                reverify = await _reverifier.ReverifyAsync(filePaths, _operationCts!.Token);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                var failure = _scan.DescribeScanFailure(ex);
                OperationProgress = string.Empty;
                if (failure.IsError)
                    _dialogService.ShowError(failure.Message, failure.Title);
                else
                    _dialogService.ShowWarning(failure.Message, failure.Title);
                return;
            }

            var survivingSet = new HashSet<string>(reverify.Surviving, StringComparer.OrdinalIgnoreCase);
            var survivingFiles = removableFiles.Where(f => survivingSet.Contains(f.FullPath)).ToList();

            if (survivingFiles.Count == 0)
            {
                // The re-verify kept every candidate back. Act on nothing and
                // report it, with the re-verify's own reason for keeping them.
                await _scan.RefreshAsync();
                _completion.ShowReverifyAllSkipped(reverify);
                OperationProgress = string.Empty;
                return;
            }

            var survivingPaths = survivingFiles.Select(f => f.FullPath).ToList();
            var survivingBytes = survivingFiles.Sum(f => f.SizeBytes);

            // _operationCts was created in the pre-flight block above; reuse it
            // through the move so a single Cancel signal covers the pre-flight, the
            // re-verify and the move loop.
            var progress = new Progress<OperationProgress>(OnOperationProgressUpdate);
            var result = await _moveService.MoveFilesAsync(survivingPaths, dest, progress, _operationCts!.Token);

            if (result.InstallerBusy)
            {
                // A Windows Installer transaction grabbed Global\_MSIExecute in the
                // sub-millisecond gap between item 4's gate re-check passing and
                // the service acquiring the mutex, so the service refused and
                // touched nothing. Re-run the gate, which now reports the held
                // mutex, to paint the banner, and report no completed operation.
                await _scan.RecheckPendingRebootAsync();
                OperationProgress = string.Empty;
                return;
            }

            if (result.Cancelled)
            {
                // Cancelled mid-batch: the service returned what completed rather
                // than throwing the tally away. Report the partial on the
                // completion overlay, and write no result-log entry for a
                // cancelled run (the owner's decision keeps the public reports
                // stats meaning what they mean). Only raise the overlay when
                // something actually moved or errored; a cancel that reached no
                // file just clears.
                await _scan.RefreshAsync();
                if (result.MovedCount > 0 || result.Errors.Count > 0)
                {
                    var cancelledFreesSpace = destinationKind is MoveDestinationKinds.DifferentFixedDrive
                        or MoveDestinationKinds.RemovableDrive
                        or MoveDestinationKinds.UncShare;
                    _completion.ShowMoveCancelledSummary(
                        result.MovedCount, survivingFiles.Count,
                        CompletedBytes(survivingFiles, result.MovedCount, result.Errors),
                        result.Errors, cancelledFreesSpace, reverify);
                }
                OperationProgress = string.Empty;
                return;
            }

            var movedCount = result.MovedCount;
            var movedDest = dest;
            var errorCount = result.Errors.Count;

            long movedBytes;
            if (errorCount == 0)
                movedBytes = survivingBytes;
            else
            {
                var errorPaths = new HashSet<string>(result.Errors.Select(e => e.FilePath), StringComparer.OrdinalIgnoreCase);
                movedBytes = survivingFiles.Where(f => !errorPaths.Contains(f.FullPath)).Sum(f => f.SizeBytes);
            }

            // Refresh through the scan VM so the registered/orphaned
            // counts update before the completion overlay reads them.
            // Silent refresh keeps the operating overlay visible until
            // this finally block clears it.
            await _scan.RefreshAsync();

            // "Freed" only when the move left the cache's volume; a
            // same-volume or unclassifiable destination claims "moved".
            var freesSpace = destinationKind is MoveDestinationKinds.DifferentFixedDrive
                or MoveDestinationKinds.RemovableDrive
                or MoveDestinationKinds.UncShare;
            _completion.ShowMoveSummary(movedCount, movedBytes, movedDest, result.Errors, freesSpace, reverify);

            // Skip the last-run.json write once the result-log surface
            // is locked. Nothing will ever read the file from this point
            // on: the Send button stays hidden for the rest of the
            // session via the session lock, and the next session checks
            // the persisted lifetime lock at startup.
            if (!_completion.IsResultLogLocked)
            {
                var entry = ResultLogEntry.ForMove(
                    preOpScan, preOpDurationMs, preOpRebootLabel,
                    result, movedBytes,
                    destinationKind);
                if (await _resultLogService.WriteAsync(entry).ConfigureAwait(true))
                    _completion.MarkResultLogReady();
            }
            // Completion overlay carries the user-facing summary; the
            // bottom-row pill stays blank once the overlay dismisses.
            OperationProgress = string.Empty;
        }
        catch (OperationCanceledException)
        {
            // A cancel before the worker starts (the token was cancelled between
            // the confirmation and Task.Run) moves nothing; a mid-batch cancel now
            // comes back as result.Cancelled above rather than as a throw, and is
            // reported on the overlay there. Nothing reached a file here, so just
            // refresh the counts and clear.
            await _scan.RefreshAsync();
            OperationProgress = string.Empty;
        }
        catch (LocalisedInvalidOperationException ex)
        {
            _dialogService.ShowWarning(ex.Message, Strings.Error_InvalidDestinationTitle);
            OperationProgress = string.Empty;
        }
        catch (LocalisedAccessException ex)
        {
            _dialogService.ShowWarning(ex.Message, Strings.Error_DestinationWriteFailedTitle);
            OperationProgress = string.Empty;
        }
        catch (Exception ex)
        {
            // A mid-move crash is surfaced the way every other failure in the
            // app is: a dialog naming the exception type and the crash-log
            // path, never ex.Message (it can carry another user's profile path
            // under elevation). It is not left to the body-row status, which
            // trims at a width cap and would cut the log path off, and the log
            // path is the one actionable thing for a report. Reaching here is
            // rare: the move service collects per-file errors rather than throwing.
            var crash = CrashLog.TryWrite(ex);
            var typeName = ex.GetType().Name;
            OperationProgress = string.Empty;
            _dialogService.ShowWarning(
                crash.Written
                    ? string.Format(Strings.Status_MoveFailed, typeName, crash.Path)
                    : string.Format(Strings.Status_MoveFailed_NoLog, typeName),
                Strings.Error_MoveFailedTitle);
        }
        finally
        {
            // DisposeOperationCts also clears IsCancellationRequested.
            DisposeOperationCts();
            IsOperating = false;
            OperationProgressPercent = 0;
            // Stale-state reset: a cancel-then-rerun cycle would otherwise
            // briefly show the previous run's last filename and "X of Y"
            // line through the next operation's first progress callback.
            OperationCurrentFile = 0;
            OperationTotalFiles = 0;
            OperationCurrentFileName = string.Empty;
            OperationProgressAnnouncement = string.Empty;
            _lastAnnouncedDecile = -1;
        }
    }

    [RelayCommand(CanExecute = nameof(CanDelete))]
    private async Task DeleteAllAsync()
    {
        if (_scan.LastScanResult is null) return;

        var removableFiles = _scan.LastScanResult.RemovableFiles;
        var count = removableFiles.Count;
        var totalBytes = removableFiles.Sum(f => f.SizeBytes);
        var sizeDisplay = _scan.OrphanedSizeDisplay;

        if (!_confirmationService.ConfirmDelete(count, sizeDisplay)) return;

        // Snapshot the pre-operation scan state once, after the confirm so a
        // cancel costs nothing. RefreshAsync (inside RunDeleteAsync) replaces
        // _scan.LastScanResult with the post-delete state, and the
        // permanent-delete retry reuses this same context; the recycle-first
        // pass touches nothing if it refuses, so the snapshot still matches it.
        var ctx = new DeleteContext(
            removableFiles,
            removableFiles.Select(f => f.FullPath).ToList(),
            count,
            totalBytes,
            _scan.LastScanResult,
            _scan.LastScanDurationMs,
            _scan.PendingRebootLabel);

        // Recycle-first. The service probes the files' volume and, rather than
        // silently permanently deleting when the bin is unavailable, refuses
        // the batch and touches nothing. That refusal comes back as true here.
        var recycleUnavailable = await RunDeleteAsync(ctx, permitPermanentDelete: false);
        if (!recycleUnavailable) return;

        // The bin is unavailable for the volume and nothing has been deleted.
        // Offer the safe Move path (primary), a consented permanent delete, or
        // cancel. The dialog names only the confirmed fact, not a cause.
        switch (_confirmationService.ConfirmRecycleUnavailable(count, sizeDisplay))
        {
            case RecycleUnavailableChoice.MoveInstead:
                await MoveInsteadAsync();
                break;
            case RecycleUnavailableChoice.DeletePermanently:
                await RunDeleteAsync(ctx, permitPermanentDelete: true);
                break;
            // Cancel: nothing was deleted and there is nothing more to do.
        }
    }

    /// <summary>
    /// Runs one delete pass and reports it on the completion overlay. Owns the
    /// operating overlay, the cancellation source and the per-run state reset,
    /// so the recycle-first attempt and the consented permanent-delete retry
    /// each get a clean lifecycle.
    /// </summary>
    /// <returns>
    /// <c>true</c> only when the Recycle Bin was unavailable for the volume and
    /// <paramref name="permitPermanentDelete"/> was <c>false</c>: the service
    /// refused and touched nothing, so <see cref="DeleteAllAsync"/> offers the
    /// Move / permanent / cancel choice instead of reporting a result. Every
    /// other outcome (completed, cancelled, failed) reports on the overlay and
    /// returns <c>false</c>. The permanent retry passes
    /// <paramref name="permitPermanentDelete"/> = <c>true</c>, which skips the
    /// probe, so it can never return <c>true</c>.
    /// </returns>
    private async Task<bool> RunDeleteAsync(DeleteContext ctx, bool permitPermanentDelete)
    {
        _operationCts = new CancellationTokenSource();
        IsOperationInFlight = true;

        // Re-check the pending-reboot gate at the moment of action. One site here
        // covers both passes: the recycle-first pass (after ConfirmDelete) and the
        // consented permanent retry (after ConfirmRecycleUnavailable), each of
        // which routes through here. Blocked => paint the banner and refuse; there
        // is nothing to offer, so return false rather than the bin-unavailable
        // true. A transaction that starts while the user reads a dialog is caught
        // on whichever pass follows it.
        if (await _scan.RecheckPendingRebootAsync())
        {
            DisposeOperationCts();
            OperationProgress = string.Empty;
            return false;
        }

        // Probe the volume before showing any overlay on the recycle-first
        // pass: when the bin is unavailable the service deletes nothing, so a
        // "Deleting N files..." overlay (and its screen-reader announcement)
        // for that pass would describe an operation that never happens. Hand
        // straight back so DeleteAllAsync offers the Move / permanent / cancel
        // choice. The permanent-retry pass (permitPermanentDelete) skips this
        // and always runs. DeleteFilesAsync re-checks the same volume and
        // still fails closed, so this only governs whether the overlay shows.
        //
        // Task.Run because the probe is a full shell IFileOperation round trip
        // (write a file, recycle it, then permanently delete the bin entry it
        // created), plus the recycle thread's creation and CoInitializeEx on
        // the session's first Delete. It is slowest exactly when the bin is
        // sick, which is the only case it exists to detect, and it used to run
        // on the dispatcher: the window sat frozen, with no overlay and no
        // Cancel, between the confirmation dialog closing and the delete
        // starting.
        if (!permitPermanentDelete && ctx.FilePaths.Count > 0
            && !await Task.Run(() => _deleteService.CanRecycleToVolume(ctx.FilePaths[0])))
        {
            DisposeOperationCts();
            return true;
        }

        // Heading before IsOperating: a heading assigned after the reveal
        // can be spoken twice (see OperationHeadingText in MainWindow.xaml).
        OperationProgress = string.Format(
            DisplayHelpers.Pluralise(ctx.Count, Strings.Status_Deleting, "Status.Deleting"),
            ctx.Count, DisplayHelpers.PluraliseFile(ctx.Count));
        IsOperating = true;

        try
        {
            // Re-verify the removable set against the API immediately before
            // acting, as the Move path does: a patch reverted to Applied between
            // the scan and the click must not be deleted. A failure STOPS the
            // batch (surfaced through the scan's error ladder); a cancellation
            // propagates to the outer OCE catch.
            ReverifyResult reverify;
            try
            {
                reverify = await _reverifier.ReverifyAsync(ctx.FilePaths, _operationCts.Token);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                var failure = _scan.DescribeScanFailure(ex);
                OperationProgress = string.Empty;
                if (failure.IsError)
                    _dialogService.ShowError(failure.Message, failure.Title);
                else
                    _dialogService.ShowWarning(failure.Message, failure.Title);
                return false;
            }

            var survivingSet = new HashSet<string>(reverify.Surviving, StringComparer.OrdinalIgnoreCase);
            var survivingFiles = ctx.RemovableFiles.Where(f => survivingSet.Contains(f.FullPath)).ToList();

            if (survivingFiles.Count == 0)
            {
                // The re-verify kept every candidate back. Act on nothing and
                // report it, with the re-verify's own reason for keeping them.
                await _scan.RefreshAsync();
                _completion.ShowReverifyAllSkipped(reverify);
                OperationProgress = string.Empty;
                return false;
            }

            var survivingPaths = survivingFiles.Select(f => f.FullPath).ToList();
            var survivingBytes = survivingFiles.Sum(f => f.SizeBytes);

            var progress = new Progress<OperationProgress>(OnOperationProgressUpdate);
            var result = await _deleteService.DeleteFilesAsync(
                survivingPaths, permitPermanentDelete, progress, _operationCts.Token);

            if (result.InstallerBusy)
            {
                // A Windows Installer transaction grabbed Global\_MSIExecute after
                // item 4's gate re-check passed, so the service refused and touched
                // nothing. Re-run the gate to paint the banner and report no
                // completed operation.
                await _scan.RecheckPendingRebootAsync();
                OperationProgress = string.Empty;
                return false;
            }

            // Bin unavailable for the volume and no consent to permanently
            // delete: the service refused the batch and touched nothing. Hand
            // control back so DeleteAllAsync can offer the choice; the finally
            // below tears the overlay down so the modal owns the foreground.
            // Returning here also skips the bytes calculation that would
            // otherwise read the empty error list as full success and claim the
            // whole size was freed while every orphan is still on disk. Only
            // the recycle-first pass reaches this; the permanent retry skips the
            // probe.
            if (result.RecycleUnavailable)
            {
                OperationProgress = string.Empty;
                return true;
            }

            if (result.Cancelled)
            {
                // Cancelled mid-batch: report the partial on the completion
                // overlay (no result-log entry for a cancelled run). The permanent
                // retry gets its own summary so it never claims the Recycle Bin;
                // the files it deleted did not reach it. Only raise the overlay
                // when something actually happened.
                await _scan.RefreshAsync();
                if (result.DeletedCount > 0 || result.Errors.Count > 0)
                {
                    var cancelledBytes = CompletedBytes(survivingFiles, result.DeletedCount, result.Errors);
                    if (permitPermanentDelete)
                        _completion.ShowPermanentDeleteCancelledSummary(
                            result.DeletedCount, survivingFiles.Count, cancelledBytes, result.Errors, reverify);
                    else
                        _completion.ShowDeleteCancelledSummary(
                            result.DeletedCount, survivingFiles.Count, cancelledBytes, result.Errors, reverify);
                }
                OperationProgress = string.Empty;
                return false;
            }

            var deletedCount = result.DeletedCount;
            var errorCount = result.Errors.Count;

            long deletedBytes;
            if (errorCount == 0)
                deletedBytes = survivingBytes;
            else
            {
                var errorPaths = new HashSet<string>(result.Errors.Select(e => e.FilePath), StringComparer.OrdinalIgnoreCase);
                deletedBytes = survivingFiles.Where(f => !errorPaths.Contains(f.FullPath)).Sum(f => f.SizeBytes);
            }

            await _scan.RefreshAsync();

            // A consented permanent delete did not reach the Recycle Bin, so it
            // gets its own summary copy rather than reusing the recycle-bin one.
            if (permitPermanentDelete)
                _completion.ShowPermanentDeleteSummary(deletedCount, deletedBytes, result.Errors, reverify);
            else
                _completion.ShowDeleteSummary(deletedCount, deletedBytes, result.Errors, reverify);

            // Same lock-aware gate as MoveAllAsync: skip the write once the
            // result-log surface is closed for the rest of the session and
            // across future sessions. Both the recycled and the consented-
            // permanent path log a delete entry of the same shape: the
            // operation freed real bytes either way and the result-log schema
            // carries no recycled-vs-permanent distinction.
            if (!_completion.IsResultLogLocked)
            {
                var entry = ResultLogEntry.ForDelete(
                    ctx.PreOpScan, ctx.PreOpDurationMs, ctx.PreOpRebootLabel,
                    result, deletedBytes);
                if (await _resultLogService.WriteAsync(entry).ConfigureAwait(true))
                    _completion.MarkResultLogReady();
            }
            OperationProgress = string.Empty;
            return false;
        }
        catch (OperationCanceledException)
        {
            // A cancel before the worker starts moves nothing; a mid-batch cancel
            // now returns as result.Cancelled above and is reported on the overlay
            // there. Refresh the counts and clear.
            await _scan.RefreshAsync();
            OperationProgress = string.Empty;
            return false;
        }
        catch (Exception ex)
        {
            // Same as the move crash path: a dialog naming the type and the
            // crash-log path, not a body-row status that trims the path off.
            var crash = CrashLog.TryWrite(ex);
            var typeName = ex.GetType().Name;
            OperationProgress = string.Empty;
            _dialogService.ShowWarning(
                crash.Written
                    ? string.Format(Strings.Status_DeleteFailed, typeName, crash.Path)
                    : string.Format(Strings.Status_DeleteFailed_NoLog, typeName),
                Strings.Error_DeleteFailedTitle);
            return false;
        }
        finally
        {
            // DisposeOperationCts also clears IsCancellationRequested.
            DisposeOperationCts();
            IsOperating = false;
            OperationProgressPercent = 0;
            // Stale-state reset: a cancel-then-rerun cycle would otherwise
            // briefly show the previous run's last filename and "X of Y"
            // line through the next operation's first progress callback.
            OperationCurrentFile = 0;
            OperationTotalFiles = 0;
            OperationCurrentFileName = string.Empty;
            OperationProgressAnnouncement = string.Empty;
            _lastAnnouncedDecile = -1;
        }
    }

    /// <summary>
    /// Routes the recycle-unavailable "Move instead" choice into the standard
    /// Move flow so its destination validation, free-space and write-probe
    /// checks and the Move confirmation all apply. Asking for a destination is
    /// the Move flow's own job, so an unset one is not handled here; backing
    /// out of that browser abandons the move and deletes nothing.
    /// </summary>
    private async Task MoveInsteadAsync()
    {
        // The guard covers the rare case where the move became unavailable
        // between the two dialogs (for example the scan emptied).
        if (MoveAllCommand.CanExecute(null))
            await MoveAllCommand.ExecuteAsync(null);
    }

    /// <summary>
    /// What one off-thread pass over a Move destination found: whether it is
    /// out of bounds, and, when it is not, what kind of volume it sits on and
    /// how much room is left there. <see cref="Kind"/> and
    /// <see cref="AvailableFreeSpace"/> are only meaningful when neither
    /// rejection flag is set, because the pass returns at the first gate that
    /// refuses and never touches the destination after that.
    /// </summary>
    private sealed record DestinationPreFlight(
        bool InsideInstallerCache,
        bool InsideSystemFolder,
        string Kind,
        long? AvailableFreeSpace)
    {
        public static DestinationPreFlight Rejected(
            bool insideInstallerCache = false, bool insideSystemFolder = false) =>
            new(insideInstallerCache, insideSystemFolder, MoveDestinationKinds.Unknown, null);
    }

    /// <summary>
    /// Snapshot of the scan state and file list a delete pass needs, captured
    /// before the first delete so the permanent-delete retry and the
    /// result-log entry both read the pre-operation values (RefreshAsync
    /// replaces <see cref="ScanViewModel.LastScanResult"/> on success).
    /// </summary>
    private sealed record DeleteContext(
        IReadOnlyList<OrphanedFile> RemovableFiles,
        IReadOnlyList<string> FilePaths,
        int Count,
        long TotalBytes,
        ScanResult PreOpScan,
        long PreOpDurationMs,
        string PreOpRebootLabel);

    /// <summary>
    /// Bytes of the files a cancelled batch actually completed. The action
    /// services process their input in order and stop at the cancel point, so the
    /// files they reached are the first (<paramref name="completedCount"/> plus
    /// the errors); of those, the ones that did not error are what moved or
    /// deleted. The exact figure on a cancel rests on that ordered processing,
    /// which both <see cref="IMoveFilesService"/> and <see cref="IDeleteFilesService"/>
    /// do; the success path can lean on "no errors means every file completed"
    /// instead, which a cancel cannot.
    /// </summary>
    private static long CompletedBytes(IReadOnlyList<OrphanedFile> files, int completedCount,
        IReadOnlyList<FileOperationError> errors)
    {
        var reached = completedCount + errors.Count;
        if (errors.Count == 0)
            return files.Take(reached).Sum(f => f.SizeBytes);

        var errorPaths = new HashSet<string>(errors.Select(e => e.FilePath), StringComparer.OrdinalIgnoreCase);
        return files.Take(reached).Where(f => !errorPaths.Contains(f.FullPath)).Sum(f => f.SizeBytes);
    }

    /// <summary>
    /// Localised "{current} of {total} files" line shown beneath the
    /// progress bar in the operating overlay. Recomputed from
    /// CurrentFile/TotalFiles via the partial-changed hooks below so
    /// XAML can bind to a single property and never assemble the line
    /// from concatenated <c>&lt;Run&gt;</c> literals.
    /// </summary>
    public string OperationProgressDetail =>
        string.Format(Strings.Summary_OperationFiles,
            OperationCurrentFile,
            OperationTotalFiles,
            DisplayHelpers.PluraliseFile(OperationTotalFiles));

    partial void OnOperationCurrentFileChanged(int value) =>
        OnPropertyChanged(nameof(OperationProgressDetail));

    partial void OnOperationTotalFilesChanged(int value) =>
        OnPropertyChanged(nameof(OperationProgressDetail));

    /// <summary>
    /// Ends the current operation: cancel-then-null-then-dispose
    /// <see cref="_operationCts"/>, then clear <see cref="IsCancellationRequested"/>
    /// (that CTS's UI mirror) and <see cref="IsOperationInFlight"/>. Every exit
    /// path of a Move or a Delete calls this, including the pre-flight's early
    /// returns, so it is the one place the operation's state is torn down.
    ///
    /// Order matters on two fronts: the null happens before Dispose so a
    /// concurrent CancelOperationCommand reading the field sees no CTS
    /// and no-ops instead of racing the dispose; the Cancel happens
    /// first so a still-running worker on the Dispose-during-shutdown
    /// path observes OperationCanceledException at its next
    /// ThrowIfCancellationRequested rather than ObjectDisposedException.
    /// Cancel on a completed CTS (the success-path callers below) is a
    /// no-op.
    ///
    /// The flag is cleared here rather than in each operation's finally
    /// because the Move pre-flight has four early returns that never reach
    /// one (cancelled probe, failed probe, not enough space, declined
    /// confirmation). A Cancel clicked during the probe left the flag set
    /// on those paths, and CanCancelOperation reads !IsCancellationRequested,
    /// so the Cancel button and Esc stayed dead for the whole of the next
    /// operation.
    /// </summary>
    private void DisposeOperationCts()
    {
        var cts = _operationCts;
        _operationCts = null;
        cts?.Cancel();
        cts?.Dispose();
        IsCancellationRequested = false;
        IsOperationInFlight = false;
    }

    private void OnOperationProgressUpdate(OperationProgress p)
    {
        OperationCurrentFile = p.CurrentFile;
        OperationTotalFiles = p.TotalFiles;
        OperationCurrentFileName = p.CurrentFileName;
        OperationProgressPercent = p.TotalFiles > 0
            ? (double)p.CurrentFile / p.TotalFiles * 100
            : 0;
        // Heading stays at the original "Moving N files..." / "Deleting
        // N files..." action verb for the operation's duration; the
        // DockPanel below the bar shows the live count via
        // OperationProgressDetail.

        var decile = p.TotalFiles > 0 ? p.CurrentFile * 10 / p.TotalFiles : 0;
        if (p.CurrentFile == 1 || p.CurrentFile == p.TotalFiles || decile != _lastAnnouncedDecile)
        {
            _lastAnnouncedDecile = decile;
            OperationProgressAnnouncement = OperationProgressDetail;
        }
    }

    /// <summary>
    /// Classifies the move destination for the diagnostic log. Returns
    /// one of <see cref="MoveDestinationKinds"/>: <c>uncShare</c> for
    /// <c>\\server\share</c>, <c>sameDrive</c> when the destination
    /// resolves to the same drive letter as the Installer cache (the
    /// system drive), <c>removableDrive</c> when the destination's
    /// <see cref="DriveType"/> is Removable, <c>differentFixedDrive</c>
    /// when it's Fixed but a different letter, and <c>unknown</c> for
    /// anything <see cref="DriveInfo"/> can't classify (network drive
    /// the API can't query, mapped path with no drive letter, etc).
    /// </summary>
    internal static string ClassifyMoveDestination(string dest)
    {
        if (string.IsNullOrWhiteSpace(dest)) return MoveDestinationKinds.Unknown;
        if (dest.StartsWith(@"\\", StringComparison.Ordinal) &&
            !dest.StartsWith(@"\\?\", StringComparison.Ordinal))
            return MoveDestinationKinds.UncShare;

        try
        {
            var destRoot = Path.GetPathRoot(Path.GetFullPath(dest));
            if (string.IsNullOrEmpty(destRoot)) return MoveDestinationKinds.Unknown;

            var systemRoot = Path.GetPathRoot(
                Environment.GetFolderPath(Environment.SpecialFolder.System));
            if (string.Equals(destRoot, systemRoot, StringComparison.OrdinalIgnoreCase))
                return MoveDestinationKinds.SameDrive;

            var info = new DriveInfo(destRoot);
            return info.DriveType switch
            {
                DriveType.Fixed => MoveDestinationKinds.DifferentFixedDrive,
                DriveType.Removable => MoveDestinationKinds.RemovableDrive,
                DriveType.Network => MoveDestinationKinds.UncShare,
                _ => MoveDestinationKinds.Unknown,
            };
        }
        catch
        {
            return MoveDestinationKinds.Unknown;
        }
    }

    /// <summary>
    /// Maps a destination-write failure to a localised explanation.
    /// <paramref name="dest"/> is the user's own typed path so echoing
    /// it back is safe; <paramref name="ex"/>.Message is never routed
    /// through this method (path-leak risk under elevation).
    /// </summary>
    internal static string DescribeWriteFailure(string dest, Exception ex, string logPath, bool logWritten) => ex switch
    {
        UnauthorizedAccessException =>
            string.Format(Strings.Error_AccessDeniedDestination, dest),
        PathTooLongException =>
            string.Format(Strings.Error_PathTooLong, dest),
        DirectoryNotFoundException =>
            string.Format(Strings.Error_DestinationMissing, dest),
        IOException => logWritten
            ? string.Format(Strings.Error_IOWriteDestination, dest, logPath)
            : string.Format(Strings.Error_IOWriteDestination_NoLog, dest),
        _ => logWritten
            ? string.Format(Strings.Error_WriteDestination, dest, logPath)
            : string.Format(Strings.Error_WriteDestination_NoLog, dest),
    };
}
