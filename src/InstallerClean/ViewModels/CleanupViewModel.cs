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
    private readonly ScanViewModel _scan;
    private readonly CompletionViewModel _completion;

    private CancellationTokenSource? _operationCts;
    private AppSettings _settings;

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
        ScanViewModel scan,
        CompletionViewModel completion)
    {
        _moveService = moveService;
        _deleteService = deleteService;
        _settingsService = settingsService;
        _dialogService = dialogService;
        _confirmationService = confirmationService;
        _scan = scan;
        _completion = completion;

        _settings = settingsService.Load();
        MoveDestination = _settings.MoveDestination;

        // Wake up CanExecute when scan/operating state changes upstream.
        _scan.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName == nameof(ScanViewModel.IsScanning) ||
                e.PropertyName == nameof(ScanViewModel.OrphanedFileCount))
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

        if (!string.Equals(_settings.MoveDestination, value, StringComparison.Ordinal))
        {
            _settings.MoveDestination = value;
            _settingsService.TrySave(_settings);
        }
    }

    private bool CanMove() =>
        !_scan.IsScanning && !IsOperating
        && _scan.OrphanedFileCount > 0
        && !string.IsNullOrWhiteSpace(MoveDestination);

    private bool CanDelete() =>
        !_scan.IsScanning && !IsOperating
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

        try
        {
            Directory.CreateDirectory(dest);
            var probe = Path.Combine(dest, Path.GetRandomFileName());
            File.WriteAllBytes(probe, Array.Empty<byte>());
            File.Delete(probe);
        }
        catch (Exception ex)
        {
            _dialogService.ShowWarning(
                DescribeWriteFailure(dest, ex),
                Strings.Error_InvalidDestinationTitle);
            return;
        }

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
            _dialogService.ShowWarning(
                string.Format(Strings.Error_NotEnoughSpaceBody,
                    dest,
                    DisplayHelpers.FormatSize(totalBytes),
                    DisplayHelpers.FormatSize(free)),
                Strings.Error_NotEnoughSpaceTitle);
            return;
        }

        if (!_confirmationService.ConfirmMove(count, sizeDisplay, MoveDestination)) return;

        IsOperating = true;
        _operationCts = new CancellationTokenSource();
        OperationProgress = string.Format(Strings.Status_Moving, count, DisplayHelpers.PluraliseFile(count));

        try
        {
            var progress = new Progress<OperationProgress>(OnOperationProgressUpdate);
            var result = await _moveService.MoveFilesAsync(filePaths, MoveDestination, progress, _operationCts.Token);
            var movedCount = result.MovedCount;
            var movedDest = MoveDestination;
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
            OperationProgress = string.Format(Strings.Status_MoveFailed, ex.Message);
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
            OperationProgress = string.Format(Strings.Status_DeleteFailed, ex.Message);
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

    private void OnOperationProgressUpdate(OperationProgress p)
    {
        OperationCurrentFile = p.CurrentFile;
        OperationTotalFiles = p.TotalFiles;
        OperationCurrentFileName = p.CurrentFileName;
        OperationProgressPercent = p.TotalFiles > 0
            ? (double)p.CurrentFile / p.TotalFiles * 100
            : 0;
        OperationProgress = string.Format(Strings.Summary_OperationFiles, p.CurrentFile, p.TotalFiles);
    }

    /// <summary>
    /// Maps a destination-write failure to a localised explanation the
    /// user can act on. Internal so MainViewModelTests can exercise
    /// the mapping directly.
    /// </summary>
    internal static string DescribeWriteFailure(string dest, Exception ex) => ex switch
    {
        UnauthorizedAccessException =>
            string.Format(Strings.Error_AccessDeniedDestination, dest),
        PathTooLongException =>
            string.Format(Strings.Error_PathTooLong, dest),
        DirectoryNotFoundException =>
            string.Format(Strings.Error_DestinationMissing, dest),
        IOException io =>
            string.Format(Strings.Error_IOWriteDestination, dest, io.Message),
        _ =>
            string.Format(Strings.Error_WriteDestination, dest, ex.Message),
    };
}
