using NSubstitute;
using NSubstitute.ExceptionExtensions;
using InstallerClean.Models;
using InstallerClean.Services;
using InstallerClean.ViewModels;

namespace InstallerClean.Tests.ViewModels;

public class MainViewModelTests
{
    private readonly IFileSystemScanService _scanService = Substitute.For<IFileSystemScanService>();
    private readonly IMoveFilesService _moveService = Substitute.For<IMoveFilesService>();
    private readonly IDeleteFilesService _deleteService = Substitute.For<IDeleteFilesService>();
    private readonly ISettingsService _settingsService = Substitute.For<ISettingsService>();
    private readonly IPendingRebootService _rebootService = Substitute.For<IPendingRebootService>();
    private readonly IMsiFileInfoService _msiInfoService = Substitute.For<IMsiFileInfoService>();
    private readonly IDialogService _dialogService = Substitute.For<IDialogService>();
    private readonly IConfirmationService _confirmationService = Substitute.For<IConfirmationService>();
    private readonly IWindowService _windowService = Substitute.For<IWindowService>();

    private MainViewModel CreateViewModel()
    {
        _settingsService.Load().Returns(new AppSettings());

        return new MainViewModel(
            _scanService, _moveService, _deleteService,
            _settingsService, _rebootService, _msiInfoService,
            _dialogService, _confirmationService, _windowService);
    }

    private static ScanResult EmptyScanResult() =>
        new(Array.Empty<OrphanedFile>(), Array.Empty<RegisteredPackage>(), 0);

    private static ScanResult ScanResultWithOrphans(int count)
    {
        var files = Enumerable.Range(0, count)
            .Select(i => new OrphanedFile($@"C:\Windows\Installer\orphan{i}.msi", 1024 * (i + 1), false))
            .ToList();
        return new ScanResult(files, Array.Empty<RegisteredPackage>(), 0);
    }

    [Fact]
    public async Task ScanAsync_sets_HasScanned_after_scan()
    {
        var vm = CreateViewModel();
        _scanService.ScanAsync(Arg.Any<IProgress<string>?>(), Arg.Any<CancellationToken>())
            .Returns(EmptyScanResult());

        Assert.False(vm.HasScanned);
        await vm.ScanWithProgressAsync(null);
        Assert.True(vm.HasScanned);
    }

    [Fact]
    public async Task ScanAsync_populates_counts()
    {
        var vm = CreateViewModel();
        var orphans = new List<OrphanedFile>
        {
            new(@"C:\Windows\Installer\a.msi", 1_048_576, false),
            new(@"C:\Windows\Installer\b.msi", 2_097_152, false),
        };
        var registered = new List<RegisteredPackage>
        {
            new(@"C:\Windows\Installer\c.msi", "Product", "{AAA}"),
        };
        _scanService.ScanAsync(Arg.Any<IProgress<string>?>(), Arg.Any<CancellationToken>())
            .Returns(new ScanResult(orphans, registered, 5_000_000));

        await vm.ScanWithProgressAsync(null);

        Assert.Equal(2, vm.OrphanedFileCount);
        Assert.Equal(1, vm.RegisteredFileCount);
        Assert.Equal("3.0 MB", vm.OrphanedSizeDisplay);
        Assert.Equal("4.8 MB", vm.RegisteredSizeDisplay);
    }

    [Fact]
    public async Task ScanAsync_shows_all_clear_when_no_orphans()
    {
        var vm = CreateViewModel();
        _scanService.ScanAsync(Arg.Any<IProgress<string>?>(), Arg.Any<CancellationToken>())
            .Returns(EmptyScanResult());

        await vm.ScanWithProgressAsync(null);

        Assert.True(vm.IsComplete);
        Assert.Equal("All clear", vm.CompletionHeading);
    }

    [Fact]
    public async Task ScanAsync_does_not_show_completion_when_orphans_exist()
    {
        var vm = CreateViewModel();
        _scanService.ScanAsync(Arg.Any<IProgress<string>?>(), Arg.Any<CancellationToken>())
            .Returns(ScanResultWithOrphans(3));

        await vm.ScanWithProgressAsync(null);

        Assert.False(vm.IsComplete);
    }

    [Fact]
    public void MoveDestination_loads_from_settings()
    {
        _settingsService.Load().Returns(new AppSettings { MoveDestination = @"D:\Backup" });

        var vm = new MainViewModel(
            _scanService, _moveService, _deleteService,
            _settingsService, _rebootService, _msiInfoService,
            _dialogService, _confirmationService, _windowService);

        Assert.Equal(@"D:\Backup", vm.MoveDestination);
    }

    [Fact]
    public void DismissCompletion_clears_state()
    {
        var vm = CreateViewModel();
        vm.DismissCompletionCommand.Execute(null);

        Assert.False(vm.IsComplete);
        Assert.Equal(string.Empty, vm.CompletionErrors);
    }

    [Fact]
    public async Task SummaryText_uses_correct_pluralisation()
    {
        var vm = CreateViewModel();
        _scanService.ScanAsync(Arg.Any<IProgress<string>?>(), Arg.Any<CancellationToken>())
            .Returns(ScanResultWithOrphans(1));

        await vm.ScanWithProgressAsync(null);

        Assert.Equal("1 file to clean up", vm.OrphanedSummaryText);
    }

    [Fact]
    public async Task ScanAsync_handles_10000_orphans()
    {
        var vm = CreateViewModel();
        _scanService.ScanAsync(Arg.Any<IProgress<string>?>(), Arg.Any<CancellationToken>())
            .Returns(ScanResultWithOrphans(10_000));

        await vm.ScanWithProgressAsync(null);

        Assert.Equal(10_000, vm.OrphanedFileCount);
        Assert.Equal("10000 files to clean up", vm.OrphanedSummaryText);
        Assert.False(vm.IsComplete);
    }

    [Fact]
    public async Task ScanAsync_handles_large_total_size()
    {
        var vm = CreateViewModel();
        var orphans = new List<OrphanedFile>
        {
            new(@"C:\Windows\Installer\huge.msi", 107_374_182_400, false), // 100 GB
        };
        _scanService.ScanAsync(Arg.Any<IProgress<string>?>(), Arg.Any<CancellationToken>())
            .Returns(new ScanResult(orphans, Array.Empty<RegisteredPackage>(), 0));

        await vm.ScanWithProgressAsync(null);

        Assert.Equal("100.00 GB", vm.OrphanedSizeDisplay);
    }

    [Fact]
    public async Task ScanAsync_propagates_access_denied()
    {
        var vm = CreateViewModel();
        _scanService.ScanAsync(Arg.Any<IProgress<string>?>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(new UnauthorizedAccessException("denied"));

        await Assert.ThrowsAsync<UnauthorizedAccessException>(
            () => vm.ScanWithProgressAsync(null));
    }

    [Fact]
    public async Task ScanAsync_zero_byte_files_display_correctly()
    {
        var vm = CreateViewModel();
        var orphans = new List<OrphanedFile>
        {
            new(@"C:\Windows\Installer\empty.msi", 0, false),
        };
        _scanService.ScanAsync(Arg.Any<IProgress<string>?>(), Arg.Any<CancellationToken>())
            .Returns(new ScanResult(orphans, Array.Empty<RegisteredPackage>(), 0));

        await vm.ScanWithProgressAsync(null);

        Assert.Equal(1, vm.OrphanedFileCount);
        Assert.Equal("0 B", vm.OrphanedSizeDisplay);
    }

    [Fact]
    public async Task ScanCommand_access_denied_shows_warning_via_dialog_service()
    {
        var vm = CreateViewModel();
        _scanService.ScanAsync(Arg.Any<IProgress<string>?>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(new UnauthorizedAccessException("denied"));

        await vm.ScanCommand.ExecuteAsync(null);

        _dialogService.Received(1).ShowWarning(
            Arg.Is<string>(s => s.Contains("administrator privileges")),
            Arg.Any<string>());
    }

    [Fact]
    public async Task ScanCommand_empty_installer_database_shows_targeted_error()
    {
        var vm = CreateViewModel();
        _scanService.ScanAsync(Arg.Any<IProgress<string>?>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(new InvalidOperationException(
                "The Windows Installer database appears to be empty or inaccessible."));

        await vm.ScanCommand.ExecuteAsync(null);

        _dialogService.Received(1).ShowError(
            Arg.Is<string>(s => s.Contains("installer database", StringComparison.OrdinalIgnoreCase)),
            Arg.Any<string>());
    }

    [Fact]
    public async Task CancelScanCommand_cancels_running_scan()
    {
        var vm = CreateViewModel();

        // The test awaits `entered` before triggering cancel so there is no
        // sleep-based race on when the mock has registered for the token.
        var entered = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        var completion = new TaskCompletionSource<ScanResult>(TaskCreationOptions.RunContinuationsAsynchronously);
        _scanService.ScanAsync(Arg.Any<IProgress<string>?>(), Arg.Any<CancellationToken>())
            .Returns(call =>
            {
                var ct = call.Arg<CancellationToken>();
                ct.Register(() => completion.TrySetCanceled(ct));
                entered.TrySetResult(true);
                return completion.Task;
            });

        var scanTask = vm.ScanCommand.ExecuteAsync(null);

        await entered.Task;
        vm.CancelScanCommand.Execute(null);

        await scanTask;

        Assert.Equal("Scan cancelled.", vm.ScanProgress);
        Assert.False(vm.IsScanning);
    }

    [Fact]
    public void CancelScanCommand_no_running_scan_is_no_op()
    {
        var vm = CreateViewModel();

        var ex = Record.Exception(() => vm.CancelScanCommand.Execute(null));

        Assert.Null(ex);
    }

    [Fact]
    public void MoveDestination_change_is_persisted_through_settings_service()
    {
        var vm = CreateViewModel();

        vm.MoveDestination = @"D:\Backup\Installer-cache";

        _settingsService.Received().TrySave(Arg.Is<AppSettings>(
            s => s.MoveDestination == @"D:\Backup\Installer-cache"));
    }

    [Fact]
    public void MoveDestination_setting_same_value_does_not_resave()
    {
        _settingsService.Load().Returns(new AppSettings { MoveDestination = @"D:\Backup" });
        var vm = new MainViewModel(
            _scanService, _moveService, _deleteService,
            _settingsService, _rebootService, _msiInfoService,
            _dialogService, _confirmationService, _windowService);
        _settingsService.ClearReceivedCalls();

        vm.MoveDestination = @"D:\Backup";

        _settingsService.DidNotReceive().TrySave(Arg.Any<AppSettings>());
    }

    [Fact]
    public async Task RescanAfterCompletion_dismisses_and_triggers_scan()
    {
        var vm = CreateViewModel();
        _scanService.ScanAsync(Arg.Any<IProgress<string>?>(), Arg.Any<CancellationToken>())
            .Returns(EmptyScanResult());

        await vm.ScanWithProgressAsync(null);
        Assert.True(vm.IsComplete);

        await vm.RescanAfterCompletionCommand.ExecuteAsync(null);

        await _scanService.Received(2).ScanAsync(
            Arg.Any<IProgress<string>?>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task MoveAllAsync_happy_path_moves_files_and_shows_completion()
    {
        var vm = CreateViewModel();
        var orphans = new List<OrphanedFile>
        {
            new(@"C:\Windows\Installer\a.msi", 1_048_576, false),
            new(@"C:\Windows\Installer\b.msi", 2_097_152, false),
        };
        _scanService.ScanAsync(Arg.Any<IProgress<string>?>(), Arg.Any<CancellationToken>())
            .Returns(new ScanResult(orphans, Array.Empty<RegisteredPackage>(), 0));
        _moveService.MoveFilesAsync(
                Arg.Any<IEnumerable<string>>(), Arg.Any<string>(),
                Arg.Any<IProgress<OperationProgress>?>(), Arg.Any<CancellationToken>())
            .Returns(new MoveResult(2, Array.Empty<FileOperationError>()));
        _confirmationService.ConfirmMove(
            Arg.Any<int>(), Arg.Any<string>(), Arg.Any<string>()).Returns(true);

        await vm.ScanWithProgressAsync(null);
        vm.MoveDestination = Path.Combine(Path.GetTempPath(), "ic-test-move");

        await vm.MoveAllCommand.ExecuteAsync(null);

        _confirmationService.Received(1).ConfirmMove(2, Arg.Any<string>(), vm.MoveDestination);
        await _moveService.Received(1).MoveFilesAsync(
            Arg.Any<IEnumerable<string>>(), vm.MoveDestination,
            Arg.Any<IProgress<OperationProgress>?>(), Arg.Any<CancellationToken>());
        Assert.True(vm.IsComplete);
        Assert.Contains("cleared", vm.CompletionHeading);
    }

    [Fact]
    public async Task MoveAllAsync_confirmation_cancelled_does_not_invoke_service()
    {
        var vm = CreateViewModel();
        _scanService.ScanAsync(Arg.Any<IProgress<string>?>(), Arg.Any<CancellationToken>())
            .Returns(ScanResultWithOrphans(3));
        _confirmationService.ConfirmMove(
            Arg.Any<int>(), Arg.Any<string>(), Arg.Any<string>()).Returns(false);

        await vm.ScanWithProgressAsync(null);
        vm.MoveDestination = Path.Combine(Path.GetTempPath(), "ic-test-move");

        await vm.MoveAllCommand.ExecuteAsync(null);

        await _moveService.DidNotReceive().MoveFilesAsync(
            Arg.Any<IEnumerable<string>>(), Arg.Any<string>(),
            Arg.Any<IProgress<OperationProgress>?>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task DeleteAllAsync_happy_path_deletes_and_shows_completion()
    {
        var vm = CreateViewModel();
        var orphans = new List<OrphanedFile>
        {
            new(@"C:\Windows\Installer\x.msi", 524_288, false),
        };
        _scanService.ScanAsync(Arg.Any<IProgress<string>?>(), Arg.Any<CancellationToken>())
            .Returns(new ScanResult(orphans, Array.Empty<RegisteredPackage>(), 0));
        _deleteService.DeleteFilesAsync(
                Arg.Any<IEnumerable<string>>(),
                Arg.Any<IProgress<OperationProgress>?>(), Arg.Any<CancellationToken>())
            .Returns(new DeleteResult(1, Array.Empty<FileOperationError>()));
        _confirmationService.ConfirmDelete(
            Arg.Any<int>(), Arg.Any<string>(), Arg.Any<long>(), Arg.Any<long>()).Returns(true);

        await vm.ScanWithProgressAsync(null);

        await vm.DeleteAllCommand.ExecuteAsync(null);

        _confirmationService.Received(1).ConfirmDelete(1, Arg.Any<string>(), 524_288, 524_288);
        await _deleteService.Received(1).DeleteFilesAsync(
            Arg.Any<IEnumerable<string>>(),
            Arg.Any<IProgress<OperationProgress>?>(), Arg.Any<CancellationToken>());
        Assert.True(vm.IsComplete);
        Assert.Contains("Recycle Bin", vm.CompletionSummary);
    }

    [Fact]
    public async Task DeleteAllAsync_confirmation_cancelled_does_not_invoke_service()
    {
        var vm = CreateViewModel();
        _scanService.ScanAsync(Arg.Any<IProgress<string>?>(), Arg.Any<CancellationToken>())
            .Returns(ScanResultWithOrphans(2));
        _confirmationService.ConfirmDelete(
            Arg.Any<int>(), Arg.Any<string>(), Arg.Any<long>(), Arg.Any<long>()).Returns(false);

        await vm.ScanWithProgressAsync(null);

        await vm.DeleteAllCommand.ExecuteAsync(null);

        await _deleteService.DidNotReceive().DeleteFilesAsync(
            Arg.Any<IEnumerable<string>>(),
            Arg.Any<IProgress<OperationProgress>?>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task OpenOrphanedDetails_after_scan_invokes_window_service_with_scanned_files()
    {
        var vm = CreateViewModel();
        _scanService.ScanAsync(Arg.Any<IProgress<string>?>(), Arg.Any<CancellationToken>())
            .Returns(ScanResultWithOrphans(2));
        await vm.ScanWithProgressAsync(null);

        vm.OpenOrphanedDetailsCommand.Execute(null);

        _windowService.Received(1).ShowOrphanedDetails(
            Arg.Is<OrphanedFilesViewModel>(v => v.Files.Count == 2));
    }

    [Fact]
    public void OpenOrphanedDetails_without_scan_is_noop()
    {
        var vm = CreateViewModel();

        vm.OpenOrphanedDetailsCommand.Execute(null);

        _windowService.DidNotReceive().ShowOrphanedDetails(Arg.Any<OrphanedFilesViewModel>());
    }

    [Fact]
    public async Task OpenRegisteredDetails_after_scan_invokes_window_service_with_scanned_packages()
    {
        var vm = CreateViewModel();
        var packages = new List<RegisteredPackage>
        {
            new(@"C:\Windows\Installer\a.msi", "Product A", "{aaa}", FileSizeBytes: 1024),
            new(@"C:\Windows\Installer\b.msi", "Product B", "{bbb}", FileSizeBytes: 2048),
        };
        _scanService.ScanAsync(Arg.Any<IProgress<string>?>(), Arg.Any<CancellationToken>())
            .Returns(new ScanResult(Array.Empty<OrphanedFile>(), packages, 3072));
        await vm.ScanWithProgressAsync(null);

        vm.OpenRegisteredDetailsCommand.Execute(null);

        _windowService.Received(1).ShowRegisteredDetails(
            Arg.Is<RegisteredFilesViewModel>(v => v.Products.Count == 2));
    }

    [Fact]
    public void ShowAbout_invokes_window_service()
    {
        var vm = CreateViewModel();

        vm.ShowAboutCommand.Execute(null);

        _windowService.Received(1).ShowAbout();
    }

    [Fact]
    public void CloseApp_invokes_window_service()
    {
        var vm = CreateViewModel();

        vm.CloseAppCommand.Execute(null);

        _windowService.Received(1).CloseMainWindow();
    }

    [Fact]
    public void StarOnGitHub_opens_repo_url()
    {
        var vm = CreateViewModel();

        vm.StarOnGitHubCommand.Execute(null);

        _windowService.Received(1).OpenUrl("https://github.com/no-faff/InstallerClean");
    }

    [Fact]
    public void Donate_opens_no_faff_url()
    {
        var vm = CreateViewModel();

        vm.DonateCommand.Execute(null);

        _windowService.Received(1).OpenUrl("https://nofaff.netlify.app");
    }
}
