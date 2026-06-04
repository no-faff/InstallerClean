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

    // IsOperating goes false during the confirm-dialog window between
    // pre-flight and the move call (so the modal owns the foreground
    // state). Re-entry is gated by the [RelayCommand]-generated
    // AsyncRelayCommand's internal IsRunning flag, not by IsOperating
    // alone; a refactor that splits MoveAllAsync into two commands
    // must re-introduce a VM-level guard or accept double-execution.
    [ObservableProperty] private bool _isOperating;
    [ObservableProperty] private string _operationProgress = string.Empty;
    [ObservableProperty] private int _operationCurrentFile;
    [ObservableProperty] private int _operationTotalFiles;
    [ObservableProperty] private string _operationCurrentFileName = string.Empty;
    [ObservableProperty] private double _operationProgressPercent;

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
        IResultLogService resultLogService)
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
        if (e.PropertyName == nameof(ScanViewModel.IsScanning) ||
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
        // Cancel before Dispose so an in-flight SaveAfterDelayAsync
        // observes the token and stops; otherwise a final keystroke
        // could land a settings write after the VM is gone.
        saveCts?.Cancel();
        saveCts?.Dispose();
    }

    partial void OnIsOperatingChanged(bool value)
    {
        MoveAllCommand.NotifyCanExecuteChanged();
        DeleteAllCommand.NotifyCanExecuteChanged();
        CancelOperationCommand.NotifyCanExecuteChanged();
    }

    // Surfaced on the disabled Move button (ToolTipService.ShowOnDisabled
    // in the view): with no destination set, point the user at the one
    // missing step rather than describing an action they cannot take yet.
    public string MoveButtonTooltip =>
        string.IsNullOrWhiteSpace(MoveDestination)
            ? Strings.Tooltip_MoveNeedsDestination
            : Strings.Tooltip_Move;

    partial void OnMoveDestinationChanged(string value)
    {
        MoveAllCommand.NotifyCanExecuteChanged();

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
        // cannot lose the window-size or lifetime-lock persists that run on the
        // dispatcher to a last-writer-wins rename.
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
    private bool CanMove() =>
        !_scan.IsScanning && !IsOperating
        && !_scan.HasPendingReboot
        && _scan.OrphanedFileCount > 0
        && !string.IsNullOrWhiteSpace(MoveDestination);

    private bool CanDelete() =>
        !_scan.IsScanning && !IsOperating
        && !_scan.HasPendingReboot
        && _scan.OrphanedFileCount > 0;

    [RelayCommand]
    private void BrowseDestination()
    {
        var dialog = new Microsoft.Win32.OpenFolderDialog
        {
            Title = Strings.FilePicker_ChooseDestinationTitle,
        };
        if (dialog.ShowDialog() == true)
        {
            MoveDestination = dialog.FolderName;
        }
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
        // progress reporter overwrites with "Move cancelled." or
        // "Delete cancelled." once the loop observes the cancellation.
        OperationProgress = Strings.Status_Cancelling;
    }

    [RelayCommand(CanExecute = nameof(CanMove))]
    private async Task MoveAllAsync()
    {
        if (_scan.LastScanResult is null) return;

        // Capture pre-operation scan state for the result-log entry.
        // RefreshAsync below replaces _scan.LastScanResult with the
        // post-move state, so the diagnostic record needs the pre-move
        // result captured here.
        var preOpScan = _scan.LastScanResult;
        var preOpDurationMs = _scan.LastScanDurationMs;
        var preOpRebootLabel = _scan.PendingRebootLabel;

        var dest = MoveDestination.Trim();

        // Never let files move back inside C:\Windows\Installer.
        // ResolveFinalPath inside IsInstallerFolderOrChild expands
        // junctions so a reparse-point destination cannot smuggle the
        // batch into the cache folder.
        if (InstallerCacheHelpers.IsInstallerFolderOrChild(dest))
        {
            _dialogService.ShowWarning(
                Strings.Error_DestinationInsideInstaller,
                Strings.Error_InvalidDestinationTitle);
            return;
        }

        // %SystemRoot%, %ProgramFiles%, %ProgramFiles(x86)% and
        // %ProgramData% sit on the Win32 DLL search path and the SxS
        // resolution path: a process searching those paths trusts a
        // planted file at load time.
        if (InstallerCacheHelpers.IsSystemFolderOrChild(dest))
        {
            _dialogService.ShowWarning(
                string.Format(Strings.Error_DestinationInSystemFolder, dest),
                Strings.Error_InvalidDestinationTitle);
            return;
        }

        // Pre-flight: CreateDirectory + write probe. Runs on a
        // worker thread so a slow UNC share doesn't freeze the UI for
        // the SMB timeout. The CTS is created BEFORE the probe so
        // the operating overlay's Cancel button can interrupt it.
        // Goes through IFileSystem so MockFileSystem-backed tests
        // don't hit real disk.
        _operationCts = new CancellationTokenSource();
        IsOperating = true;
        OperationProgress = Strings.Status_PreparingDestination;
        try
        {
            var probeToken = _operationCts.Token;
            await Task.Run(() =>
            {
                _fs.Directory.CreateDirectory(dest);
                probeToken.ThrowIfCancellationRequested();
                var probe = _fs.Path.Combine(dest, _fs.Path.GetRandomFileName());
                _fs.File.WriteAllBytes(probe, Array.Empty<byte>());
                probeToken.ThrowIfCancellationRequested();
                _fs.File.Delete(probe);
            }, probeToken);
        }
        catch (OperationCanceledException)
        {
            IsOperating = false;
            OperationProgress = Strings.Status_MoveCancelled;
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

        var removableFiles = _scan.LastScanResult.RemovableFiles;
        var filePaths = removableFiles.Select(f => f.FullPath).ToList();
        var count = filePaths.Count;
        var totalBytes = removableFiles.Sum(f => f.SizeBytes);
        var sizeDisplay = _scan.OrphanedSizeDisplay;

        // Free-space check. Skip silently for paths the API can't
        // measure (UNC shares where the caller lacks query rights, etc).
        var availableFreeSpace = StorageHelpers.GetAvailableFreeSpace(dest);
        if (availableFreeSpace is long free && free < totalBytes)
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
        if (!_confirmationService.ConfirmMove(count, sizeDisplay, dest))
        {
            // User cancelled at the confirmation dialog. The pre-flight
            // CTS is no longer needed; dispose it before returning.
            DisposeOperationCts();
            OperationProgress = Strings.Status_MoveCancelled;
            return;
        }

        IsOperating = true;
        OperationProgress = string.Format(Strings.Status_Moving, count, DisplayHelpers.PluraliseFile(count));

        try
        {
            // _operationCts was created in the pre-flight block above;
            // reuse it through the move so a single Cancel signal
            // covers both the pre-flight and the move loop.
            var progress = new Progress<OperationProgress>(OnOperationProgressUpdate);
            var result = await _moveService.MoveFilesAsync(filePaths, dest, progress, _operationCts!.Token);
            var movedCount = result.MovedCount;
            var movedDest = dest;
            var errorCount = result.Errors.Count;

            long movedBytes;
            if (errorCount == 0)
                movedBytes = totalBytes;
            else
            {
                var errorPaths = new HashSet<string>(result.Errors.Select(e => e.FilePath), StringComparer.OrdinalIgnoreCase);
                movedBytes = removableFiles.Where(f => !errorPaths.Contains(f.FullPath)).Sum(f => f.SizeBytes);
            }

            // Refresh through the scan VM so the registered/orphaned
            // counts update before the completion overlay reads them.
            // Silent refresh keeps the operating overlay visible until
            // this finally block clears it.
            await _scan.RefreshAsync();

            _completion.ShowMoveSummary(movedCount, movedBytes, movedDest, result.Errors);

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
                    ClassifyMoveDestination(movedDest));
                if (await _resultLogService.WriteAsync(entry).ConfigureAwait(true))
                    _completion.MarkResultLogReady();
            }
            // Completion overlay carries the user-facing summary; the
            // bottom-row pill stays blank once the overlay dismisses.
            OperationProgress = string.Empty;
        }
        catch (OperationCanceledException)
        {
            // OperationCurrentFile and OperationTotalFiles hold the last
            // worker-reported counts when the OCE arrives; the finally
            // resets them, so the catch reads them first.
            OperationProgress = OperationCurrentFile > 0 && OperationTotalFiles > 0
                ? string.Format(Strings.Status_MoveCancelled_Partial,
                    OperationCurrentFile,
                    OperationTotalFiles,
                    DisplayHelpers.PluraliseFile(OperationTotalFiles))
                : Strings.Status_MoveCancelled;
            await _scan.RefreshAsync();
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
            DisposeOperationCts();
            IsOperating = false;
            IsCancellationRequested = false;
            OperationProgressPercent = 0;
            // Stale-state reset: a cancel-then-rerun cycle would otherwise
            // briefly show the previous run's last filename and "X of Y"
            // line through the next operation's first progress callback.
            OperationCurrentFile = 0;
            OperationTotalFiles = 0;
            OperationCurrentFileName = string.Empty;
        }
    }

    [RelayCommand(CanExecute = nameof(CanDelete))]
    private async Task DeleteAllAsync()
    {
        if (_scan.LastScanResult is null) return;

        var removableFiles = _scan.LastScanResult.RemovableFiles;
        var count = removableFiles.Count;
        var totalBytes = removableFiles.Sum(f => f.SizeBytes);
        var maxSingleFileBytes = removableFiles.Count > 0 ? removableFiles.Max(f => f.SizeBytes) : 0;
        var sizeDisplay = _scan.OrphanedSizeDisplay;

        if (!_confirmationService.ConfirmDelete(count, sizeDisplay, totalBytes, maxSingleFileBytes)) return;

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
        IsOperating = true;
        _operationCts = new CancellationTokenSource();
        OperationProgress = string.Format(Strings.Status_Deleting,
            ctx.Count, DisplayHelpers.PluraliseFile(ctx.Count));

        try
        {
            var progress = new Progress<OperationProgress>(OnOperationProgressUpdate);
            var result = await _deleteService.DeleteFilesAsync(
                ctx.FilePaths, permitPermanentDelete, progress, _operationCts.Token);

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

            var deletedCount = result.DeletedCount;
            var errorCount = result.Errors.Count;

            long deletedBytes;
            if (errorCount == 0)
                deletedBytes = ctx.TotalBytes;
            else
            {
                var errorPaths = new HashSet<string>(result.Errors.Select(e => e.FilePath), StringComparer.OrdinalIgnoreCase);
                deletedBytes = ctx.RemovableFiles.Where(f => !errorPaths.Contains(f.FullPath)).Sum(f => f.SizeBytes);
            }

            await _scan.RefreshAsync();

            // A consented permanent delete did not reach the Recycle Bin, so it
            // gets its own summary copy rather than reusing the recycle-bin one.
            if (permitPermanentDelete)
                _completion.ShowPermanentDeleteSummary(deletedCount, deletedBytes, result.Errors);
            else
                _completion.ShowDeleteSummary(deletedCount, deletedBytes, result.Errors);

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
            OperationProgress = OperationCurrentFile > 0 && OperationTotalFiles > 0
                ? string.Format(Strings.Status_DeleteCancelled_Partial,
                    OperationCurrentFile,
                    OperationTotalFiles,
                    DisplayHelpers.PluraliseFile(OperationTotalFiles))
                : Strings.Status_DeleteCancelled;
            await _scan.RefreshAsync();
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
            DisposeOperationCts();
            IsOperating = false;
            IsCancellationRequested = false;
            OperationProgressPercent = 0;
            // Stale-state reset: a cancel-then-rerun cycle would otherwise
            // briefly show the previous run's last filename and "X of Y"
            // line through the next operation's first progress callback.
            OperationCurrentFile = 0;
            OperationTotalFiles = 0;
            OperationCurrentFileName = string.Empty;
        }
    }

    /// <summary>
    /// Routes the recycle-unavailable "Move instead" choice into the standard
    /// Move flow so its destination validation, free-space and write-probe
    /// checks and the Move confirmation all apply. A destination is required;
    /// when none is set the folder picker opens first so the safe path is never
    /// a dead end. If the user backs out of the picker, nothing happens.
    /// </summary>
    private async Task MoveInsteadAsync()
    {
        if (string.IsNullOrWhiteSpace(MoveDestination))
        {
            var dialog = new Microsoft.Win32.OpenFolderDialog
            {
                Title = Strings.FilePicker_ChooseDestinationTitle,
            };
            if (dialog.ShowDialog() != true) return;
            MoveDestination = dialog.FolderName;
        }

        // Setting MoveDestination above re-evaluates CanMove synchronously via
        // OnMoveDestinationChanged, so the command is executable here once a
        // destination is present. The guard covers the rare case where it is
        // not (for example the scan emptied between the dialogs).
        if (MoveAllCommand.CanExecute(null))
            await MoveAllCommand.ExecuteAsync(null);
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
    /// Cancel-then-null-then-dispose <see cref="_operationCts"/>. Order
    /// matters on two fronts: the null happens before Dispose so a
    /// concurrent CancelOperationCommand reading the field sees no CTS
    /// and no-ops instead of racing the dispose; the Cancel happens
    /// first so a still-running worker on the Dispose-during-shutdown
    /// path observes OperationCanceledException at its next
    /// ThrowIfCancellationRequested rather than ObjectDisposedException.
    /// Cancel on a completed CTS (the success-path callers below) is a
    /// no-op.
    /// </summary>
    private void DisposeOperationCts()
    {
        var cts = _operationCts;
        _operationCts = null;
        cts?.Cancel();
        cts?.Dispose();
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
