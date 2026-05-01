using NSubstitute;
using InstallerClean.Models;
using InstallerClean.Services;
using InstallerClean.ViewModels;

namespace InstallerClean.Tests.EndToEnd;

/// <summary>
/// End-to-end behaviour tests that exercise the full ViewModel graph
/// (MainViewModel + Scan / Cleanup / Completion / Chrome) through a
/// realistic user journey. Service dependencies are NSubstitute stubs;
/// no filesystem or Win32 calls are made.
///
/// Distinct from <see cref="ViewModels.MainViewModelTests"/>, which
/// focuses on individual properties and commands. These tests bind
/// multiple commands together (scan -> orphans found -> move ->
/// completion overlay) and assert the resulting cross-VM state.
/// </summary>
public class ScanMoveCompletionTests
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

    private MainViewModel CreateMain()
    {
        _settingsService.Load().Returns(new AppSettings());
        return new MainViewModel(
            _scanService, _moveService, _deleteService,
            _settingsService, _rebootService, _msiInfoService,
            _dialogService, _confirmationService, _windowService);
    }

    [Fact]
    public async Task Scan_then_Move_paints_completion_overlay_with_summary()
    {
        var orphans = new[]
        {
            new OrphanedFile(@"C:\Windows\Installer\a.msi", 1_048_576, false),
            new OrphanedFile(@"C:\Windows\Installer\b.msi", 2_097_152, false),
        };
        _scanService.ScanAsync(Arg.Any<IProgress<string>?>(), Arg.Any<CancellationToken>())
            .Returns(new ScanResult(orphans, Array.Empty<RegisteredPackage>(), 0));
        _confirmationService.ConfirmMove(
            Arg.Any<int>(), Arg.Any<string>(), Arg.Any<string>()).Returns(true);
        _moveService.MoveFilesAsync(
                Arg.Any<IEnumerable<string>>(), Arg.Any<string>(),
                Arg.Any<IProgress<OperationProgress>?>(), Arg.Any<CancellationToken>())
            .Returns(new MoveResult(2, Array.Empty<FileOperationError>()));

        var vm = CreateMain();

        // Stage 1: scan finds two orphans. Completion overlay must NOT
        // appear yet (orphans exist), Cleanup commands become available.
        await vm.ScanWithProgressAsync(null);
        Assert.False(vm.Completion.IsComplete);
        Assert.Equal(2, vm.Scan.OrphanedFileCount);
        Assert.True(vm.Cleanup.DeleteAllCommand.CanExecute(null));

        // Stage 2: pick a destination and run Move. Completion overlay
        // appears with the post-move summary; Cleanup is no longer
        // operating, Scan has refreshed (RefreshAsync was called).
        vm.Cleanup.MoveDestination = @"D:\backup";
        await vm.Cleanup.MoveAllCommand.ExecuteAsync(null);

        Assert.True(vm.Completion.IsComplete);
        Assert.Contains(@"D:\backup", vm.Completion.Summary);
        Assert.Contains("3.0 MB", vm.Completion.Heading);  // 1MB + 2MB cleared
        Assert.False(vm.Cleanup.IsOperating);
        await _scanService.Received(2).ScanAsync(  // initial + post-move refresh
            Arg.Any<IProgress<string>?>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Scan_finding_no_orphans_immediately_paints_all_clear()
    {
        _scanService.ScanAsync(Arg.Any<IProgress<string>?>(), Arg.Any<CancellationToken>())
            .Returns(new ScanResult(Array.Empty<OrphanedFile>(), Array.Empty<RegisteredPackage>(), 0));

        var vm = CreateMain();
        await vm.ScanWithProgressAsync(null);

        Assert.True(vm.Completion.IsComplete);
        Assert.Equal("All clear", vm.Completion.Heading);
        Assert.False(vm.Cleanup.MoveAllCommand.CanExecute(null));
        Assert.False(vm.Cleanup.DeleteAllCommand.CanExecute(null));
    }

    [Fact]
    public async Task Scan_then_Delete_paints_completion_overlay_with_recycle_bin_summary()
    {
        var orphans = new[]
        {
            new OrphanedFile(@"C:\Windows\Installer\x.msi", 524_288, false),
        };
        _scanService.ScanAsync(Arg.Any<IProgress<string>?>(), Arg.Any<CancellationToken>())
            .Returns(new ScanResult(orphans, Array.Empty<RegisteredPackage>(), 0));
        _confirmationService.ConfirmDelete(
            Arg.Any<int>(), Arg.Any<string>(), Arg.Any<long>(), Arg.Any<long>()).Returns(true);
        _deleteService.DeleteFilesAsync(
                Arg.Any<IEnumerable<string>>(),
                Arg.Any<IProgress<OperationProgress>?>(), Arg.Any<CancellationToken>())
            .Returns(new DeleteResult(1, Array.Empty<FileOperationError>()));

        var vm = CreateMain();
        await vm.ScanWithProgressAsync(null);
        await vm.Cleanup.DeleteAllCommand.ExecuteAsync(null);

        Assert.True(vm.Completion.IsComplete);
        Assert.Contains("Recycle Bin", vm.Completion.Summary);
    }

    [Fact]
    public async Task Rescan_from_completion_overlay_runs_another_scan()
    {
        _scanService.ScanAsync(Arg.Any<IProgress<string>?>(), Arg.Any<CancellationToken>())
            .Returns(new ScanResult(Array.Empty<OrphanedFile>(), Array.Empty<RegisteredPackage>(), 0));

        var vm = CreateMain();
        await vm.ScanWithProgressAsync(null);
        Assert.True(vm.Completion.IsComplete);

        await vm.Completion.RescanAfterCompletionCommand.ExecuteAsync(null);

        await _scanService.Received(2).ScanAsync(
            Arg.Any<IProgress<string>?>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Move_with_per_file_errors_renders_grouped_breakdown()
    {
        var orphans = new[]
        {
            new OrphanedFile(@"C:\Windows\Installer\a.msi", 1024, false),
            new OrphanedFile(@"C:\Windows\Installer\b.msi", 2048, false),
            new OrphanedFile(@"C:\Windows\Installer\c.msi", 4096, false),
        };
        _scanService.ScanAsync(Arg.Any<IProgress<string>?>(), Arg.Any<CancellationToken>())
            .Returns(new ScanResult(orphans, Array.Empty<RegisteredPackage>(), 0));
        _confirmationService.ConfirmMove(
            Arg.Any<int>(), Arg.Any<string>(), Arg.Any<string>()).Returns(true);

        // One success, two MissingSourceFile errors.
        var errors = new FileOperationError[]
        {
            new MissingSourceFile(@"C:\Windows\Installer\b.msi"),
            new MissingSourceFile(@"C:\Windows\Installer\c.msi"),
        };
        _moveService.MoveFilesAsync(
                Arg.Any<IEnumerable<string>>(), Arg.Any<string>(),
                Arg.Any<IProgress<OperationProgress>?>(), Arg.Any<CancellationToken>())
            .Returns(new MoveResult(1, errors));

        var vm = CreateMain();
        await vm.ScanWithProgressAsync(null);
        vm.Cleanup.MoveDestination = @"D:\backup";
        await vm.Cleanup.MoveAllCommand.ExecuteAsync(null);

        Assert.True(vm.Completion.IsComplete);
        // The breakdown groups by category and lists each file by name
        // under its category header.
        Assert.Contains("(2)", vm.Completion.Errors);
        Assert.Contains("b.msi", vm.Completion.Errors);
        Assert.Contains("c.msi", vm.Completion.Errors);
    }
}
