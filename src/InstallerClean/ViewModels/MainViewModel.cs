using System.Diagnostics;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using InstallerClean.Helpers;
using InstallerClean.Models;
using InstallerClean.Resources;
using InstallerClean.Services;

namespace InstallerClean.ViewModels;

public partial class MainViewModel : ObservableObject
{
    private readonly IFileSystemScanService _scanService;
    private readonly IMoveFilesService _moveService;
    private readonly IDeleteFilesService _deleteService;
    private readonly ISettingsService _settingsService;
    private readonly IPendingRebootService _rebootService;
    private readonly IMsiFileInfoService _msiInfoService;
    private readonly IDialogService _dialogService;
    private readonly IConfirmationService _confirmationService;
    private readonly IWindowService _windowService;

    [ObservableProperty] private bool _isScanning;
    [ObservableProperty] private string _scanProgress = string.Empty;

    [ObservableProperty] private int _registeredFileCount;
    [ObservableProperty] private string _registeredSizeDisplay = string.Empty;
    [ObservableProperty] private int _orphanedFileCount;
    [ObservableProperty] private string _orphanedSizeDisplay = string.Empty;

    public string RegisteredSummaryText =>
        string.Format(Strings.Summary_RegisteredStillUsed,
            RegisteredFileCount, DisplayHelpers.PluraliseFileVerb(RegisteredFileCount));

    public string OrphanedSummaryText =>
        string.Format(Strings.Summary_OrphanedToCleanUp,
            OrphanedFileCount, DisplayHelpers.PluraliseFile(OrphanedFileCount));

    [ObservableProperty] private bool _hasPendingReboot;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasMissingFromDisk))]
    [NotifyPropertyChangedFor(nameof(MissingFromDiskSummaryText))]
    private int _missingFromDiskCount;

    public bool HasMissingFromDisk => MissingFromDiskCount > 0;
    public string MissingFromDiskSummaryText =>
        string.Format(Strings.Summary_MissingFromDisk,
            MissingFromDiskCount, DisplayHelpers.PluraliseFileVerb(MissingFromDiskCount));

    [ObservableProperty] private string _moveDestination = string.Empty;

    [ObservableProperty] private bool _isOperating;
    [ObservableProperty] private string _operationProgress = string.Empty;
    [ObservableProperty] private int _operationCurrentFile;
    [ObservableProperty] private int _operationTotalFiles;
    [ObservableProperty] private string _operationCurrentFileName = string.Empty;
    [ObservableProperty] private double _operationProgressPercent;

    private CancellationTokenSource? _operationCts;
    private CancellationTokenSource? _scanCts;

    [ObservableProperty] private bool _hasScanned;

    [ObservableProperty] private bool _isComplete;
    [ObservableProperty] private string _completionHeading = string.Empty;
    [ObservableProperty] private string _completionSummary = string.Empty;
    [ObservableProperty] private string _completionRestore = string.Empty;
    [ObservableProperty] private string _completionErrors = string.Empty;

    private ScanResult? _lastScanResult;
    private AppSettings _settings = new();

    public MainViewModel(
        IFileSystemScanService scanService,
        IMoveFilesService moveService,
        IDeleteFilesService deleteService,
        ISettingsService settingsService,
        IPendingRebootService rebootService,
        IMsiFileInfoService msiInfoService,
        IDialogService dialogService,
        IConfirmationService confirmationService,
        IWindowService windowService)
    {
        _scanService = scanService;
        _moveService = moveService;
        _deleteService = deleteService;
        _settingsService = settingsService;
        _rebootService = rebootService;
        _msiInfoService = msiInfoService;
        _dialogService = dialogService;
        _confirmationService = confirmationService;
        _windowService = windowService;

        _settings = settingsService.Load();
        MoveDestination = _settings.MoveDestination;
    }

    partial void OnIsScanningChanged(bool value)
    {
        MoveAllCommand.NotifyCanExecuteChanged();
        DeleteAllCommand.NotifyCanExecuteChanged();
    }

    partial void OnIsOperatingChanged(bool value)
    {
        MoveAllCommand.NotifyCanExecuteChanged();
        DeleteAllCommand.NotifyCanExecuteChanged();
    }

    partial void OnRegisteredFileCountChanged(int value)
    {
        OnPropertyChanged(nameof(RegisteredSummaryText));
    }

    partial void OnOrphanedFileCountChanged(int value)
    {
        MoveAllCommand.NotifyCanExecuteChanged();
        DeleteAllCommand.NotifyCanExecuteChanged();
        OnPropertyChanged(nameof(OrphanedSummaryText));
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
        !IsScanning && !IsOperating && OrphanedFileCount > 0 && !string.IsNullOrWhiteSpace(MoveDestination);

    private bool CanDelete() =>
        !IsScanning && !IsOperating && OrphanedFileCount > 0;

    private async Task RunScanCoreAsync(IProgress<string>? progress, CancellationToken cancellationToken = default)
    {
        HasPendingReboot = _rebootService.HasPendingReboot();

        _lastScanResult = await _scanService.ScanAsync(progress, cancellationToken);

        RegisteredFileCount = _lastScanResult.RegisteredPackages.Count;
        RegisteredSizeDisplay = DisplayHelpers.FormatSize(_lastScanResult.RegisteredTotalBytes);

        OrphanedFileCount = _lastScanResult.RemovableFiles.Count;
        OrphanedSizeDisplay = DisplayHelpers.FormatSize(_lastScanResult.RemovableFiles.Sum(f => f.SizeBytes));

        MissingFromDiskCount = _lastScanResult.MissingFromDiskCount;

        HasScanned = true;
    }

    [RelayCommand]
    private async Task ScanAsync()
    {
        ScanProgress = Strings.Status_StartingScan;
        var sw = Stopwatch.StartNew();
        var cts = new CancellationTokenSource();
        _scanCts = cts;

        try
        {
            var progress = new Progress<string>(OnScanProgressUpdate);
            var scanTask = RunScanCoreAsync(progress, cts.Token);
            if (await Task.WhenAny(scanTask, Task.Delay(200, cts.Token)) != scanTask)
                IsScanning = true;
            await scanTask;

            sw.Stop();
            ScanProgress = string.Format(Strings.Status_ScanComplete, DisplayHelpers.FormatElapsed(sw.Elapsed));
            OperationProgress = ScanProgress;

            if (OrphanedFileCount == 0 && !IsOperating)
            {
                CompletionHeading = Strings.Completion_AllClear;
                CompletionSummary = Strings.Completion_NothingToCleanUp;
                CompletionRestore = string.Empty;
                CompletionErrors = string.Empty;
                IsComplete = true;
            }
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
            _dialogService.ShowError(ex.Message, Strings.Error_InstallerDbUnavailableTitle);
            ScanProgress = Strings.Status_ScanFailedDb;
        }
        catch (Exception ex)
        {
            var logPath = CrashLog.Write(ex);
            ScanProgress = string.Format(Strings.Status_ScanFailedDetails, ex.Message, logPath);
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

    internal static string DescribeWriteFailure(string dest, Exception ex) => ex switch
    {
        UnauthorizedAccessException =>
            string.Format(Strings.Error_AccessDeniedDestination, dest),
        System.IO.PathTooLongException =>
            string.Format(Strings.Error_PathTooLong, dest),
        System.IO.DirectoryNotFoundException =>
            string.Format(Strings.Error_DestinationMissing, dest),
        System.IO.IOException io =>
            string.Format(Strings.Error_IOWriteDestination, dest, io.Message),
        _ =>
            string.Format(Strings.Error_WriteDestination, dest, ex.Message)
    };

    /// <summary>
    /// Renders the per-file error list shown on the completion screen.
    /// Errors are grouped by category so the user sees "Access denied
    /// (3): a.msi, b.msi, c.msi" rather than a flat list of identical
    /// sentences. Inside each category, files are listed by name with
    /// the category-specific detail appearing once at the top.
    /// </summary>
    internal static string FormatErrorBreakdown(IReadOnlyList<FileOperationError> errors)
    {
        if (errors.Count == 0) return string.Empty;

        // Group by runtime type so MissingSourceFile, ShellRefused etc.
        // each get their own bucket. Within a bucket the LocalisedMessage
        // is identical for stateless categories (e.g. MissingSourceFile)
        // and varies per-file for stateful ones (e.g. AccessDenied with
        // its captured Detail), so we list each file with its own message.
        var buckets = errors
            .GroupBy(e => e.GetType())
            .OrderByDescending(g => g.Count());

        var sb = new System.Text.StringBuilder();
        foreach (var bucket in buckets)
        {
            // Header line: category sample + count.
            var sample = bucket.First().LocalisedMessage;
            sb.Append(sample).Append(" (").Append(bucket.Count()).Append(')').AppendLine();
            foreach (var err in bucket)
                sb.Append("  ").Append(Path.GetFileName(err.FilePath)).AppendLine();
        }
        return sb.ToString().TrimEnd();
    }

    private void OnScanProgressUpdate(string message) => ScanProgress = message;

    private void OnOperationProgressUpdate(Models.OperationProgress p)
    {
        OperationCurrentFile = p.CurrentFile;
        OperationTotalFiles = p.TotalFiles;
        OperationCurrentFileName = p.CurrentFileName;
        OperationProgressPercent = p.TotalFiles > 0
            ? (double)p.CurrentFile / p.TotalFiles * 100
            : 0;
        OperationProgress = string.Format(Strings.Summary_OperationFiles, p.CurrentFile, p.TotalFiles);
    }

    [RelayCommand]
    private void BrowseDestination()
    {
        var dialog = new Microsoft.Win32.OpenFolderDialog
        {
            Title = Strings.FilePicker_ChooseDestinationTitle
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
        // Races the finally block that disposes _operationCts; ObjectDisposedException
        // here just means the operation already finished.
        try { _operationCts?.Cancel(); }
        catch (ObjectDisposedException) { }
    }

    [RelayCommand(CanExecute = nameof(CanMove))]
    private async Task MoveAllAsync()
    {
        if (_lastScanResult is null) return;

        var dest = MoveDestination;
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

        var removableFiles = _lastScanResult.RemovableFiles;
        var filePaths = removableFiles.Select(f => f.FullPath).ToList();
        var count = filePaths.Count;
        var totalBytes = removableFiles.Sum(f => f.SizeBytes);
        var sizeDisplay = OrphanedSizeDisplay;

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
            var progress = new Progress<Models.OperationProgress>(OnOperationProgressUpdate);
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

            // Refresh directly so IsScanning stays false and the scan overlay
            // doesn't stack on top of the still-visible operating overlay.
            await RunScanCoreAsync(null);

            CompletionHeading = string.Format(Strings.Completion_Cleared, DisplayHelpers.FormatSize(movedBytes));
            var movedLabel = DisplayHelpers.PluraliseFile(movedCount);
            CompletionSummary = errorCount == 0
                ? string.Format(Strings.Completion_MoveSummary, movedCount, movedLabel, movedDest)
                : string.Format(Strings.Completion_MoveSummaryWithErrors,
                    movedCount, movedLabel, movedDest, errorCount, DisplayHelpers.PluraliseError(errorCount));
            CompletionRestore = Strings.Completion_MoveRestoreHint;
            CompletionErrors = errorCount > 0
                ? FormatErrorBreakdown(result.Errors)
                : string.Empty;
            IsComplete = true;
        }
        catch (OperationCanceledException)
        {
            OperationProgress = Strings.Status_MoveCancelled;
            try { await RunScanCoreAsync(null); } catch { /* best effort refresh */ }
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
        if (_lastScanResult is null) return;

        var removableFiles = _lastScanResult.RemovableFiles;
        var count = removableFiles.Count;
        var totalBytes = removableFiles.Sum(f => f.SizeBytes);
        var maxSingleFileBytes = removableFiles.Count > 0 ? removableFiles.Max(f => f.SizeBytes) : 0;
        var sizeDisplay = OrphanedSizeDisplay;

        if (!_confirmationService.ConfirmDelete(count, sizeDisplay, totalBytes, maxSingleFileBytes)) return;

        IsOperating = true;
        _operationCts = new CancellationTokenSource();
        var filePaths = removableFiles.Select(f => f.FullPath).ToList();
        OperationProgress = string.Format(Strings.Status_Deleting,
            filePaths.Count, DisplayHelpers.PluraliseFile(filePaths.Count));

        try
        {
            var progress = new Progress<Models.OperationProgress>(OnOperationProgressUpdate);
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

            // Refresh directly so IsScanning stays false and the scan overlay
            // doesn't stack on top of the still-visible operating overlay.
            await RunScanCoreAsync(null);

            CompletionHeading = string.Format(Strings.Completion_Cleared, DisplayHelpers.FormatSize(deletedBytes));
            var deletedLabel = DisplayHelpers.PluraliseFile(deletedCount);
            CompletionSummary = errorCount == 0
                ? string.Format(Strings.Completion_DeleteSummary, deletedCount, deletedLabel)
                : string.Format(Strings.Completion_DeleteSummaryWithErrors,
                    deletedCount, deletedLabel, errorCount, DisplayHelpers.PluraliseError(errorCount));
            CompletionRestore = Strings.Completion_DeleteRestoreHint;
            CompletionErrors = errorCount > 0
                ? FormatErrorBreakdown(result.Errors)
                : string.Empty;
            IsComplete = true;
        }
        catch (OperationCanceledException)
        {
            OperationProgress = Strings.Status_DeleteCancelled;
            try { await RunScanCoreAsync(null); } catch { /* best effort refresh */ }
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

    [RelayCommand]
    private void OpenOrphanedDetails()
    {
        if (_lastScanResult is null) return;

        var viewModel = new OrphanedFilesViewModel(
            _lastScanResult.RemovableFiles,
            _msiInfoService);

        _windowService.ShowOrphanedDetails(viewModel);
    }

    [RelayCommand]
    private void OpenRegisteredDetails()
    {
        if (_lastScanResult is null) return;

        var viewModel = new RegisteredFilesViewModel(
            _lastScanResult.RegisteredPackages,
            _lastScanResult.RegisteredTotalBytes,
            _msiInfoService);

        _windowService.ShowRegisteredDetails(viewModel);
    }

    [RelayCommand]
    private void ShowAbout() => _windowService.ShowAbout();

    [RelayCommand]
    private void StarOnGitHub() => _windowService.OpenUrl("https://github.com/no-faff/InstallerClean");

    [RelayCommand]
    private void Donate() => _windowService.OpenUrl("https://nofaff.netlify.app");

    public async Task ScanWithProgressAsync(IProgress<string>? progress, CancellationToken cancellationToken = default)
    {
        var sw = Stopwatch.StartNew();
        await RunScanCoreAsync(progress, cancellationToken);
        sw.Stop();
        ScanProgress = string.Format(Strings.Status_ScanComplete, DisplayHelpers.FormatElapsed(sw.Elapsed));

        if (OrphanedFileCount == 0)
        {
            CompletionHeading = Strings.Completion_AllClear;
            CompletionSummary = Strings.Completion_NothingToCleanUp;
            CompletionRestore = string.Empty;
            CompletionErrors = string.Empty;
            IsComplete = true;
        }
    }

    [RelayCommand]
    private void DismissCompletion()
    {
        IsComplete = false;
        CompletionErrors = string.Empty;
    }

    [RelayCommand]
    private async Task RescanAfterCompletionAsync()
    {
        IsComplete = false;
        CompletionErrors = string.Empty;
        await ScanAsync();
    }

    [RelayCommand]
    private void CloseApp() => _windowService.CloseMainWindow();

}
