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
public partial class CleanupViewModel : ObservableObject
{
    private readonly IMoveFilesService _moveService;
    private readonly IDeleteFilesService _deleteService;
    private readonly ISettingsService _settingsService;
    private readonly IDialogService _dialogService;
    private readonly IConfirmationService _confirmationService;
    private readonly IFileSystem _fs;
    private readonly ScanViewModel _scan;
    private readonly CompletionViewModel _completion;

    private CancellationTokenSource? _operationCts;
    private CancellationTokenSource? _moveDestinationSaveCts;
    private AppSettings _settings;

    /// <summary>
    /// Debounce window for write-back of MoveDestination edits to disk.
    /// Each keystroke cancels the previous pending save and starts a
    /// new timer; the actual TrySave runs only if the user stops
    /// typing for this long. 400ms is roughly half a comfortable
    /// keystroke interval, so a normal typist never triggers more
    /// than one save per pause.
    /// </summary>
    /// <remarks>
    /// Exposed as <c>internal</c> so MainViewModelTests can wait on this
    /// value plus a small margin instead of hardcoding 700 ms (which
    /// drifts silently if the constant is ever tuned).
    /// </remarks>
    internal static readonly TimeSpan MoveDestinationSaveDelay = TimeSpan.FromMilliseconds(400);

    [ObservableProperty] private string _moveDestination = string.Empty;

    [ObservableProperty] private bool _isOperating;
    [ObservableProperty] private string _operationProgress = string.Empty;
    [ObservableProperty] private int _operationCurrentFile;
    [ObservableProperty] private int _operationTotalFiles;
    [ObservableProperty] private string _operationCurrentFileName = string.Empty;
    [ObservableProperty] private double _operationProgressPercent;

    public CleanupViewModel(
        IMoveFilesService moveService,
        IDeleteFilesService deleteService,
        ISettingsService settingsService,
        IDialogService dialogService,
        IConfirmationService confirmationService,
        IFileSystem fileSystem,
        ScanViewModel scan,
        CompletionViewModel completion)
    {
        _moveService = moveService;
        _deleteService = deleteService;
        _settingsService = settingsService;
        _dialogService = dialogService;
        _confirmationService = confirmationService;
        _fs = fileSystem;
        _scan = scan;
        _completion = completion;

        _settings = settingsService.Load();
        MoveDestination = _settings.MoveDestination;

        // Wake up CanExecute when scan/operating state changes upstream.
        //
        // LIFETIME CONTRACT: this subscription is intentionally never
        // unhooked. Both VMs are constructed by MainViewModel and
        // share its lifetime; MainViewModel is constructed once per
        // MainWindow in App.xaml.OnStartup and dies with the process.
        // If a future test or feature ever creates throwaway
        // MainViewModel instances around a longer-lived ScanViewModel
        // (for example by hoisting Scan into a DI singleton), convert
        // this to a named handler stored on a field and detach it in
        // an IDisposable.Dispose. The handler does not capture mutable
        // state, only `this`.
        _scan.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName == nameof(ScanViewModel.IsScanning) ||
                e.PropertyName == nameof(ScanViewModel.OrphanedFileCount) ||
                e.PropertyName == nameof(ScanViewModel.HasPendingReboot))
            {
                MoveAllCommand.NotifyCanExecuteChanged();
                DeleteAllCommand.NotifyCanExecuteChanged();
            }
        };
    }

    partial void OnIsOperatingChanged(bool value)
    {
        MoveAllCommand.NotifyCanExecuteChanged();
        DeleteAllCommand.NotifyCanExecuteChanged();
    }

    partial void OnMoveDestinationChanged(string value)
    {
        MoveAllCommand.NotifyCanExecuteChanged();

        if (string.Equals(_settings.MoveDestination, value, StringComparison.Ordinal))
            return;

        _settings.MoveDestination = value;
        ScheduleMoveDestinationSave();
    }

    /// <summary>
    /// Debounced write-back of <see cref="MoveDestination"/> to disk.
    /// Each call cancels the previous pending save so a typist who
    /// pastes or types a path doesn't fire a TrySave per character.
    /// The save happens once they stop changing the value for
    /// <see cref="MoveDestinationSaveDelay"/>.
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

        // Re-load the on-disk settings before writing so the save
        // doesn't clobber updates made by other writers (the orphaned-
        // and registered-files detail windows persist their window
        // size on close via a fresh Load+Save through the same
        // ISettingsService instance). Without this re-load, a user who
        // typed in MoveDestination, opened a detail window, resized
        // it, closed it (triggering a window-size save), then waited
        // for the debounce to fire, would lose the window-size update
        // because we'd write our cached _settings over the top.
        var fresh = _settingsService.Load();
        fresh.MoveDestination = _settings.MoveDestination;
        _settings = fresh;
        // Use Save (fire-and-forget) rather than TrySave because the
        // bool return is unused here and Save matches the contract the
        // detail-window code-behinds use on close. Save itself
        // delegates to TrySave internally and never throws.
        _settingsService.Save(_settings);

        // Dispose the CTS now that this scheduled save has completed.
        // The "type once and never type again" case is the only path
        // that needs this: every other save is followed by another
        // ScheduleMoveDestinationSave call which disposes the previous
        // CTS as part of replacing it. Token equality ensures we only
        // dispose if THIS scheduled save's CTS is still the current
        // one (i.e. no further keystroke replaced it while we were
        // awaiting the delay).
        if (_moveDestinationSaveCts is { } current && current.Token == token)
        {
            _moveDestinationSaveCts = null;
            current.Dispose();
        }
    }

    // Move and Delete are gated on HasPendingReboot because cleaning
    // the installer cache while Windows Update is mid-staging can break
    // the pending repair / rollback sequence (see IPendingRebootService).
    // The banner above the buttons explains the gate; without this
    // check the warning was purely informational and the user could
    // proceed regardless.
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
            // OnMoveDestinationChanged persists the new value via TrySave.
            MoveDestination = dialog.FolderName;
        }
    }

    [RelayCommand]
    private void CancelOperation()
    {
        // Races the finally block that disposes _operationCts;
        // ObjectDisposedException here just means the operation
        // already finished.
        try { _operationCts?.Cancel(); }
        catch (ObjectDisposedException) { }
    }

    [RelayCommand(CanExecute = nameof(CanMove))]
    private async Task MoveAllAsync()
    {
        if (_scan.LastScanResult is null) return;

        var dest = MoveDestination;

        // SECURITY: never let files move back inside C:\Windows\Installer.
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

        // The pre-flight (CreateDirectory + write probe) used to run
        // synchronously on the UI thread. For a UNC destination on a
        // slow share this could freeze the main window for the SMB
        // timeout (tens of seconds) before the user saw any feedback.
        // Show the operating overlay first so the user has visible
        // progress text, then run the probe on a thread-pool task; on
        // local paths the probe finishes before the next layout pass
        // so the overlay flicker is invisible. The overlay is cleared
        // after the probe returns so the free-space check and the
        // confirmation dialog run with the main UI visible again.
        //
        // The probe goes through the injected IFileSystem so unit tests
        // running against MockFileSystem don't hit real disk for the
        // probe (the rest of the move pipeline already does). The
        // CTS is created BEFORE the probe so the Cancel button on the
        // operating overlay actually cancels: previously _operationCts
        // was created after the probe, leaving the user staring at a
        // Cancel button that did nothing while a slow share blocked
        // for the SMB timeout.
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
            // User cancelled before the operation started. Treat this
            // exactly like cancelling during the move loop: clear the
            // overlay, set the status pill, return.
            IsOperating = false;
            OperationProgress = Strings.Status_MoveCancelled;
            var cts = _operationCts;
            _operationCts = null;
            cts?.Dispose();
            return;
        }
        catch (Exception ex)
        {
            IsOperating = false;
            OperationProgress = string.Empty;
            // Write the full exception to the crash log; route the safe
            // parts of the failure (destination + log path) through
            // DescribeWriteFailure. The framework exception's .Message
            // is intentionally not surfaced in the dialog; under
            // elevation it could carry paths from another user's
            // profile.
            var logPath = CrashLog.Write(ex);
            var cts = _operationCts;
            _operationCts = null;
            cts?.Dispose();
            _dialogService.ShowWarning(
                DescribeWriteFailure(dest, ex, logPath),
                Strings.Error_InvalidDestinationTitle);
            return;
        }
        IsOperating = false;
        OperationProgress = string.Empty;

        var removableFiles = _scan.LastScanResult.RemovableFiles;
        var filePaths = removableFiles.Select(f => f.FullPath).ToList();
        var count = filePaths.Count;
        var totalBytes = removableFiles.Sum(f => f.SizeBytes);
        var sizeDisplay = _scan.OrphanedSizeDisplay;

        // Free-space check. Skip silently for paths we can't measure
        // (UNC shares where the caller lacks query rights, etc).
        var availableFreeSpace = StorageHelpers.GetAvailableFreeSpace(dest);
        if (availableFreeSpace is long free && free < totalBytes)
        {
            // Pre-flight CTS no longer needed; dispose before returning.
            var cts = _operationCts;
            _operationCts = null;
            cts?.Dispose();
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
            var cts = _operationCts;
            _operationCts = null;
            cts?.Dispose();
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
        }
        catch (OperationCanceledException)
        {
            OperationProgress = Strings.Status_MoveCancelled;
            await _scan.RefreshAsync();
        }
        catch (Exception ex)
        {
            // Write the full exception to the crash log; surface only
            // the type name + log path in the status pill so framework
            // messages can't leak absolute paths into the UI but the
            // user still has somewhere to look.
            var logPath = CrashLog.Write(ex);
            OperationProgress = string.Format(Strings.Status_MoveFailed, ex.GetType().Name, logPath);
        }
        finally
        {
            var cts = _operationCts;
            _operationCts = null;
            cts?.Dispose();
            IsOperating = false;
            OperationProgressPercent = 0;
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

        IsOperating = true;
        _operationCts = new CancellationTokenSource();
        var filePaths = removableFiles.Select(f => f.FullPath).ToList();
        OperationProgress = string.Format(Strings.Status_Deleting,
            filePaths.Count, DisplayHelpers.PluraliseFile(filePaths.Count));

        try
        {
            var progress = new Progress<OperationProgress>(OnOperationProgressUpdate);
            var result = await _deleteService.DeleteFilesAsync(filePaths, progress, _operationCts.Token);
            var deletedCount = result.DeletedCount;
            var errorCount = result.Errors.Count;

            long deletedBytes;
            if (errorCount == 0)
                deletedBytes = totalBytes;
            else
            {
                var errorPaths = new HashSet<string>(result.Errors.Select(e => e.FilePath), StringComparer.OrdinalIgnoreCase);
                deletedBytes = removableFiles.Where(f => !errorPaths.Contains(f.FullPath)).Sum(f => f.SizeBytes);
            }

            await _scan.RefreshAsync();

            _completion.ShowDeleteSummary(deletedCount, deletedBytes, result.Errors);
        }
        catch (OperationCanceledException)
        {
            OperationProgress = Strings.Status_DeleteCancelled;
            await _scan.RefreshAsync();
        }
        catch (Exception ex)
        {
            // Mirror MoveAllAsync: full detail to crash log, type name +
            // log path in the status pill.
            var logPath = CrashLog.Write(ex);
            OperationProgress = string.Format(Strings.Status_DeleteFailed, ex.GetType().Name, logPath);
        }
        finally
        {
            var cts = _operationCts;
            _operationCts = null;
            cts?.Dispose();
            IsOperating = false;
            OperationProgressPercent = 0;
        }
    }

    /// <summary>
    /// Localised "{current} of {total} files" line shown beneath the
    /// progress bar in the operating overlay. Recomputed from
    /// CurrentFile/TotalFiles via the partial-changed hooks below so
    /// XAML can bind to a single property and never assemble the line
    /// from concatenated <c>&lt;Run&gt;</c> literals.
    /// </summary>
    public string OperationProgressDetail =>
        string.Format(Strings.Summary_OperationFiles, OperationCurrentFile, OperationTotalFiles);

    partial void OnOperationCurrentFileChanged(int value) =>
        OnPropertyChanged(nameof(OperationProgressDetail));

    partial void OnOperationTotalFilesChanged(int value) =>
        OnPropertyChanged(nameof(OperationProgressDetail));

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
    /// Maps a destination-write failure to a localised explanation the
    /// user can act on. Internal so MainViewModelTests can exercise
    /// the mapping directly.
    /// </summary>
    /// <remarks>
    /// SECURITY: <paramref name="dest"/> is exposed to the dialog
    /// because the failing operation is a write probe to a path the
    /// CURRENT user just typed (or browsed to via OpenFolderDialog),
    /// so echoing it back is safe. The framework exception's .Message
    /// is NOT routed through this method's text - even an IOException
    /// .Message can include paths beyond <paramref name="dest"/> (lock-
    /// holder process paths, NTFS resolution chains) which under
    /// elevation could be paths from another user's profile. The full
    /// exception is written to crash.log by the caller and the path
    /// is surfaced as <paramref name="logPath"/> so the user can find
    /// the detail without it leaking into the dialog body.
    /// </remarks>
    internal static string DescribeWriteFailure(string dest, Exception ex, string logPath) => ex switch
    {
        UnauthorizedAccessException =>
            string.Format(Strings.Error_AccessDeniedDestination, dest),
        PathTooLongException =>
            string.Format(Strings.Error_PathTooLong, dest),
        DirectoryNotFoundException =>
            string.Format(Strings.Error_DestinationMissing, dest),
        IOException =>
            string.Format(Strings.Error_IOWriteDestination, dest, logPath),
        _ =>
            string.Format(Strings.Error_WriteDestination, dest, logPath),
    };
}
