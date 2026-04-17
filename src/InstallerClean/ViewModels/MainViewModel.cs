using System.Diagnostics;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using InstallerClean.Helpers;
using InstallerClean.Models;
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
    private readonly IUpdateCheckService _updateCheckService;
    private readonly IDialogService _dialogService;
    private readonly IConfirmationService _confirmationService;
    // Scan state
    [ObservableProperty] private bool _isScanning;
    [ObservableProperty] private string _scanProgress = string.Empty;

    // Summary line data
    [ObservableProperty] private int _registeredFileCount;
    [ObservableProperty] private string _registeredSizeDisplay = string.Empty;
    [ObservableProperty] private int _orphanedFileCount;
    [ObservableProperty] private string _orphanedSizeDisplay = string.Empty;

    public string RegisteredSummaryText =>
        $"{RegisteredFileCount} {DisplayHelpers.Pluralise(RegisteredFileCount, "file", "files")} still used";

    public string OrphanedSummaryText =>
        $"{OrphanedFileCount} {DisplayHelpers.Pluralise(OrphanedFileCount, "file", "files")} to clean up";

    [ObservableProperty] private bool _hasPendingReboot;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasMissingFromDisk))]
    [NotifyPropertyChangedFor(nameof(MissingFromDiskSummaryText))]
    private int _missingFromDiskCount;

    public bool HasMissingFromDisk => MissingFromDiskCount > 0;
    public string MissingFromDiskSummaryText =>
        $"{MissingFromDiskCount} registered {DisplayHelpers.Pluralise(MissingFromDiskCount, "file is", "files are")} missing from disk. Your Windows Installer database references installers that no longer exist.";

    [ObservableProperty] private string _moveDestination = string.Empty;

    // Busy state for move/delete operations
    [ObservableProperty] private bool _isOperating;
    [ObservableProperty] private string _operationProgress = string.Empty;
    [ObservableProperty] private int _operationCurrentFile;
    [ObservableProperty] private int _operationTotalFiles;
    [ObservableProperty] private string _operationCurrentFileName = string.Empty;
    [ObservableProperty] private double _operationProgressPercent;

    private CancellationTokenSource? _operationCts;
    private CancellationTokenSource? _scanCts;

    [ObservableProperty] private bool _hasScanned;

    // Completion screen state
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
        IUpdateCheckService updateCheckService,
        IDialogService dialogService,
        IConfirmationService confirmationService)
    {
        _scanService = scanService;
        _moveService = moveService;
        _deleteService = deleteService;
        _settingsService = settingsService;
        _rebootService = rebootService;
        _msiInfoService = msiInfoService;
        _updateCheckService = updateCheckService;
        _dialogService = dialogService;
        _confirmationService = confirmationService;

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
        ScanProgress = "Starting scan...";
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
            ScanProgress = $"Scan complete ({sw.Elapsed.TotalSeconds:F1}s)";
            OperationProgress = ScanProgress;

            if (OrphanedFileCount == 0 && !IsOperating)
            {
                CompletionHeading = "All clear";
                CompletionSummary = "Nothing to clean up in C:\\Windows\\Installer";
                CompletionRestore = string.Empty;
                CompletionErrors = string.Empty;
                IsComplete = true;
            }
        }
        catch (OperationCanceledException)
        {
            ScanProgress = "Scan cancelled.";
        }
        catch (UnauthorizedAccessException)
        {
            _dialogService.ShowWarning(
                "This app requires administrator privileges.\n\nPlease right-click and choose 'Run as administrator'.",
                "Administrator rights required");
            ScanProgress = "Access denied. Run as administrator.";
        }
        catch (InvalidOperationException ex)
        {
            _dialogService.ShowError(ex.Message, "Installer database unavailable");
            ScanProgress = "Scan failed: installer database unavailable.";
        }
        catch (Exception ex)
        {
            var logPath = CrashLog.Write(ex);
            ScanProgress = $"Scan failed: {ex.Message}. Details in {logPath}.";
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
            $"You don't have permission to write to {dest}.\nTry a folder in your user profile, or run as a different administrator.",
        System.IO.PathTooLongException =>
            $"The path {dest} is too long for Windows. Pick a shorter path.",
        System.IO.DirectoryNotFoundException =>
            $"The folder {dest} does not exist and could not be created. Check the drive letter or network path.",
        System.IO.IOException io =>
            $"Windows cannot write to {dest}:\n{io.Message}",
        _ =>
            $"Cannot write to {dest}:\n{ex.Message}"
    };

    private void OnScanProgressUpdate(string message) => ScanProgress = message;

    private void OnOperationProgressUpdate(Models.OperationProgress p)
    {
        OperationCurrentFile = p.CurrentFile;
        OperationTotalFiles = p.TotalFiles;
        OperationCurrentFileName = p.CurrentFileName;
        OperationProgressPercent = p.TotalFiles > 0
            ? (double)p.CurrentFile / p.TotalFiles * 100
            : 0;
        OperationProgress = $"{p.CurrentFile} of {p.TotalFiles} files";
    }

    [RelayCommand]
    private void BrowseDestination()
    {
        var dialog = new Microsoft.Win32.OpenFolderDialog
        {
            Title = "Choose destination folder for moved files"
        };
        if (dialog.ShowDialog() == true)
        {
            // Setting MoveDestination triggers OnMoveDestinationChanged
            // which persists the new value via _settingsService.TrySave.
            MoveDestination = dialog.FolderName;
        }
    }

    [RelayCommand]
    private void CancelOperation()
    {
        // Guard against a race with the finally block that disposes the
        // token source: a Cancel click landing between
        // _operationCts = null and cts.Dispose() can hit a disposed token.
        try
        {
            _operationCts?.Cancel();
        }
        catch (ObjectDisposedException)
        {
            // Operation already finished. Nothing to cancel.
        }
    }

    [RelayCommand(CanExecute = nameof(CanMove))]
    private async Task MoveAllAsync()
    {
        if (_lastScanResult is null) return;

        var dest = MoveDestination;
        if (InstallerCacheHelpers.IsInstallerFolderOrChild(dest))
        {
            _dialogService.ShowWarning(
                "The destination cannot be inside the Windows Installer folder.",
                "Invalid destination");
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
                "Invalid destination");
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
                $"Not enough space at {dest}\n\n" +
                $"Required: {DisplayHelpers.FormatSize(totalBytes)}\n" +
                $"Available: {DisplayHelpers.FormatSize(free)}",
                "Not enough space");
            return;
        }

        if (!_confirmationService.ConfirmMove(count, sizeDisplay, MoveDestination)) return;

        IsOperating = true;
        _operationCts = new CancellationTokenSource();
        OperationProgress = $"Moving {count} {DisplayHelpers.Pluralise(count, "file", "files")}...";

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

            // Refresh counts without flipping IsScanning (the operating overlay
            // is already up; we don't want both overlays layered).
            await RunScanCoreAsync(null);

            CompletionHeading = $"{DisplayHelpers.FormatSize(movedBytes)} cleared";
            var movedLabel = DisplayHelpers.Pluralise(movedCount, "file", "files");
            CompletionSummary = errorCount == 0
                ? $"{movedCount} {movedLabel} moved to {movedDest}"
                : $"{movedCount} {movedLabel} moved to {movedDest}. {errorCount} {DisplayHelpers.Pluralise(errorCount, "error", "errors")}.";
            CompletionRestore = "Copy them back if anything stops working";
            CompletionErrors = errorCount > 0
                ? string.Join("\n", result.Errors.Select(e => $"{Path.GetFileName(e.FilePath)}: {e.Message}"))
                : string.Empty;
            IsComplete = true;
        }
        catch (OperationCanceledException)
        {
            OperationProgress = "Move cancelled.";
            try { await RunScanCoreAsync(null); } catch { /* best effort refresh */ }
        }
        catch (Exception ex)
        {
            OperationProgress = $"Move failed: {ex.Message}";
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
        OperationProgress = $"Deleting {filePaths.Count} {DisplayHelpers.Pluralise(filePaths.Count, "file", "files")}...";

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

            // Refresh counts without flipping IsScanning (the operating overlay
            // is already up; we don't want both overlays layered).
            await RunScanCoreAsync(null);

            CompletionHeading = $"{DisplayHelpers.FormatSize(deletedBytes)} cleared";
            var deletedLabel = DisplayHelpers.Pluralise(deletedCount, "file", "files");
            CompletionSummary = errorCount == 0
                ? $"{deletedCount} {deletedLabel} sent to the Recycle Bin"
                : $"{deletedCount} {deletedLabel} deleted. {errorCount} {DisplayHelpers.Pluralise(errorCount, "error", "errors")}.";
            CompletionRestore = "Restore them if anything stops working";
            CompletionErrors = errorCount > 0
                ? string.Join("\n", result.Errors.Select(e => $"{Path.GetFileName(e.FilePath)}: {e.Message}"))
                : string.Empty;
            IsComplete = true;
        }
        catch (OperationCanceledException)
        {
            OperationProgress = "Delete cancelled.";
            try { await RunScanCoreAsync(null); } catch { /* best effort refresh */ }
        }
        catch (Exception ex)
        {
            OperationProgress = $"Delete failed: {ex.Message}";
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

        var window = new OrphanedFilesWindow(viewModel, _settingsService)
        {
            Owner = Application.Current.MainWindow
        };
        window.ShowDialog();
    }

    [RelayCommand]
    private void OpenRegisteredDetails()
    {
        if (_lastScanResult is null) return;

        var viewModel = new RegisteredFilesViewModel(
            _lastScanResult.RegisteredPackages,
            _lastScanResult.RegisteredTotalBytes,
            _msiInfoService);

        var window = new RegisteredFilesWindow(viewModel, _settingsService)
        {
            Owner = Application.Current.MainWindow
        };
        window.ShowDialog();
    }

    [RelayCommand]
    private void ShowAbout()
    {
        var window = new AboutWindow(_updateCheckService)
        {
            Owner = Application.Current.MainWindow
        };
        window.ShowDialog();
    }

    [RelayCommand]
    private void StarOnGitHub()
    {
        Process.Start(new ProcessStartInfo
        {
            FileName = "https://github.com/no-faff/InstallerClean",
            UseShellExecute = true
        });
    }

    [RelayCommand]
    private void Donate()
    {
        Process.Start(new ProcessStartInfo
        {
            FileName = "https://nofaff.netlify.app",
            UseShellExecute = true
        });
    }

    public async Task ScanWithProgressAsync(IProgress<string>? progress, CancellationToken cancellationToken = default)
    {
        var sw = Stopwatch.StartNew();
        await RunScanCoreAsync(progress, cancellationToken);
        sw.Stop();
        ScanProgress = $"Scan complete ({sw.Elapsed.TotalSeconds:F1}s)";

        if (OrphanedFileCount == 0)
        {
            CompletionHeading = "All clear";
            CompletionSummary = "Nothing to clean up in C:\\Windows\\Installer";
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
    private void CloseApp()
    {
        Application.Current.MainWindow?.Close();
    }

}
