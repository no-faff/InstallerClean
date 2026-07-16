using System.IO.Abstractions.TestingHelpers;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using InstallerClean.Helpers;
using InstallerClean.Models;
using InstallerClean.Resources;
using InstallerClean.Services;
using InstallerClean.ViewModels;

namespace InstallerClean.Tests.ViewModels;

public class MainViewModelTests
{
    private static readonly string Orphaned = Strings.Reason_Orphaned;
    private static readonly string Superseded = Strings.Reason_Superseded;

    private readonly IFileSystemScanService _scanService = Substitute.For<IFileSystemScanService>();
    private readonly IMoveFilesService _moveService = Substitute.For<IMoveFilesService>();
    private readonly IDeleteFilesService _deleteService = Substitute.For<IDeleteFilesService>();
    private readonly ISettingsService _settingsService = Substitute.For<ISettingsService>();
    private readonly IPendingRebootService _rebootService = Substitute.For<IPendingRebootService>();
    private readonly IMsiFileInfoService _msiInfoService = Substitute.For<IMsiFileInfoService>();
    private readonly IDialogService _dialogService = Substitute.For<IDialogService>();
    private readonly IConfirmationService _confirmationService = Substitute.For<IConfirmationService>();
    private readonly IWindowService _windowService = Substitute.For<IWindowService>();
    private readonly IResultLogService _resultLogService = Substitute.For<IResultLogService>();
    private readonly IRemovableReverifier _reverifier = Substitute.For<IRemovableReverifier>();
    private readonly MockFileSystem _fileSystem = new();

    private MainViewModel CreateViewModel() => CreateViewModel(new AppSettings());

    /// <summary>
    /// Build a MainViewModel against the substituted services with a
    /// caller-provided initial AppSettings. Single construction site
    /// so any future ctor parameter change touches one line, not the
    /// 10-arg <c>new MainViewModel(...)</c> site repeated across tests.
    /// </summary>
    private MainViewModel CreateViewModel(AppSettings settings)
    {
        _settingsService.Load().Returns(settings);
        // Default the recycle-volume probe to available so the delete flow
        // reaches DeleteFilesAsync; tests covering the bin-unavailable path
        // stub this false or return RecycleUnavailable from DeleteFilesAsync.
        _deleteService.CanRecycleToVolume(Arg.Any<string>()).Returns(true);
        // Check() returns Clean or Block, never null (the interface contract);
        // default it Clean so the scan and the act-time re-check both proceed.
        // Tests covering the gate override with a Clean-then-Block sequence.
        _rebootService.Check().Returns(PendingRebootResult.Clean);
        // Default the act-time re-verify to a no-op: every candidate survives and
        // nothing is dropped, so a Move/Delete acts on the full set as before.
        // Tests covering P2 override this to drop entries or to throw.
        _reverifier.ReverifyAsync(Arg.Any<IReadOnlyList<string>>(), Arg.Any<CancellationToken>())
            .Returns(ci => new ReverifyResult((IReadOnlyList<string>)ci[0]!, Array.Empty<string>()));

        return new MainViewModel(
            _scanService, _moveService, _deleteService,
            _settingsService, _rebootService, _msiInfoService,
            _dialogService, _confirmationService, _windowService,
            _fileSystem, _resultLogService, _reverifier);
    }

    private static ScanResult EmptyScanResult() =>
        new(Array.Empty<OrphanedFile>(), Array.Empty<RegisteredPackage>(), 0);

    private static ScanResult ScanResultWithOrphans(int count)
    {
        var files = Enumerable.Range(0, count)
            .Select(i => new OrphanedFile($@"C:\Windows\Installer\orphan{i}.msi", 1024 * (i + 1), false, false, false, InstallerClean.Resources.Strings.Reason_Orphaned))
            .ToList();
        return new ScanResult(files, Array.Empty<RegisteredPackage>(), 0);
    }

    [Fact]
    public async Task ScanAsync_sets_HasScanned_after_scan()
    {
        var vm = CreateViewModel();
        _scanService.ScanAsync(Arg.Any<IProgress<ScanProgressUpdate>?>(), Arg.Any<CancellationToken>())
            .Returns(EmptyScanResult());

        Assert.False(vm.Scan.HasScanned);
        await vm.Scan.ScanWithProgressAsync(null);
        Assert.True(vm.Scan.HasScanned);
    }

    [Fact]
    public async Task ScanAsync_populates_counts()
    {
        var vm = CreateViewModel();
        var orphans = new List<OrphanedFile>
        {
            new(@"C:\Windows\Installer\a.msi", 1_048_576, false, false, false, Orphaned),
            new(@"C:\Windows\Installer\b.msi", 2_097_152, false, false, false, Orphaned),
        };
        var registered = new List<RegisteredPackage>
        {
            new(@"C:\Windows\Installer\c.msi", "Product", "{AAA}"),
        };
        _scanService.ScanAsync(Arg.Any<IProgress<ScanProgressUpdate>?>(), Arg.Any<CancellationToken>())
            .Returns(new ScanResult(orphans, registered, 5_000_000));

        await vm.Scan.ScanWithProgressAsync(null);

        Assert.Equal(2, vm.Scan.OrphanedFileCount);
        Assert.Equal(1, vm.Scan.RegisteredFileCount);
        Assert.Equal("3.0 MB", vm.Scan.OrphanedSizeDisplay);
        Assert.Equal("4.8 MB", vm.Scan.RegisteredSizeDisplay);
    }

    [Fact]
    public async Task ScanAsync_shows_all_clear_when_no_orphans()
    {
        var vm = CreateViewModel();
        _scanService.ScanAsync(Arg.Any<IProgress<ScanProgressUpdate>?>(), Arg.Any<CancellationToken>())
            .Returns(EmptyScanResult());

        await vm.Scan.ScanWithProgressAsync(null);

        Assert.True(vm.Completion.IsComplete);
        Assert.Equal("All clean", vm.Completion.Heading);
    }

    [Fact]
    public async Task ScanAsync_does_not_show_completion_when_orphans_exist()
    {
        var vm = CreateViewModel();
        _scanService.ScanAsync(Arg.Any<IProgress<ScanProgressUpdate>?>(), Arg.Any<CancellationToken>())
            .Returns(ScanResultWithOrphans(3));

        await vm.Scan.ScanWithProgressAsync(null);

        Assert.False(vm.Completion.IsComplete);
    }

    [Fact]
    public void MoveDestination_loads_from_settings()
    {
        var vm = CreateViewModel(new AppSettings { MoveDestination = @"D:\Backup" });

        Assert.Equal(@"D:\Backup", vm.Cleanup.MoveDestination);
    }

    [Fact]
    public void DismissCompletion_clears_state()
    {
        var vm = CreateViewModel();
        vm.Completion.DismissCommand.Execute(null);

        Assert.False(vm.Completion.IsComplete);
        Assert.Equal(string.Empty, vm.Completion.Errors);
    }

    [Fact]
    public async Task SummaryText_uses_correct_pluralisation()
    {
        var vm = CreateViewModel();
        _scanService.ScanAsync(Arg.Any<IProgress<ScanProgressUpdate>?>(), Arg.Any<CancellationToken>())
            .Returns(ScanResultWithOrphans(1));

        await vm.Scan.ScanWithProgressAsync(null);

        Assert.Equal("1 unneeded file to clean up", vm.Scan.OrphanedSummaryText);
    }

    [Fact]
    public async Task ScanAsync_handles_10000_orphans()
    {
        var vm = CreateViewModel();
        _scanService.ScanAsync(Arg.Any<IProgress<ScanProgressUpdate>?>(), Arg.Any<CancellationToken>())
            .Returns(ScanResultWithOrphans(10_000));

        await vm.Scan.ScanWithProgressAsync(null);

        Assert.Equal(10_000, vm.Scan.OrphanedFileCount);
        Assert.Equal("10000 unneeded files to clean up", vm.Scan.OrphanedSummaryText);
        Assert.False(vm.Completion.IsComplete);
    }

    [Fact]
    public async Task ScanAsync_handles_large_total_size()
    {
        var vm = CreateViewModel();
        var orphans = new List<OrphanedFile>
        {
            new(@"C:\Windows\Installer\huge.msi", 107_374_182_400, false, false, false, Orphaned), // 100 GB
        };
        _scanService.ScanAsync(Arg.Any<IProgress<ScanProgressUpdate>?>(), Arg.Any<CancellationToken>())
            .Returns(new ScanResult(orphans, Array.Empty<RegisteredPackage>(), 0));

        await vm.Scan.ScanWithProgressAsync(null);

        Assert.Equal("100.00 GB", vm.Scan.OrphanedSizeDisplay);
    }

    [Fact]
    public async Task ScanWithProgressAsync_records_access_denied_without_throwing()
    {
        var vm = CreateViewModel();
        _scanService.ScanAsync(Arg.Any<IProgress<ScanProgressUpdate>?>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(new UnauthorizedAccessException("denied"));

        // The startup scan records the tailored message through the error ladder
        // and returns, so App opens the main window in its error state instead of
        // the exception reaching App.OnStartup and exiting. Only cancellation
        // still propagates (it closes the splash).
        await vm.Scan.ScanWithProgressAsync(null);

        Assert.True(vm.Scan.HasScanError);
        Assert.Contains("already running as administrator", vm.Scan.LastScanError);
    }

    [Fact]
    public async Task ScanAsync_zero_byte_files_display_correctly()
    {
        var vm = CreateViewModel();
        var orphans = new List<OrphanedFile>
        {
            new(@"C:\Windows\Installer\empty.msi", 0, false, false, false, Orphaned),
        };
        _scanService.ScanAsync(Arg.Any<IProgress<ScanProgressUpdate>?>(), Arg.Any<CancellationToken>())
            .Returns(new ScanResult(orphans, Array.Empty<RegisteredPackage>(), 0));

        await vm.Scan.ScanWithProgressAsync(null);

        Assert.Equal(1, vm.Scan.OrphanedFileCount);
        Assert.Equal("0 B", vm.Scan.OrphanedSizeDisplay);
    }

    [Fact]
    public async Task ScanCommand_access_denied_shows_warning_via_dialog_service()
    {
        var vm = CreateViewModel();
        _scanService.ScanAsync(Arg.Any<IProgress<ScanProgressUpdate>?>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(new UnauthorizedAccessException("denied"));

        await vm.Scan.ScanCommand.ExecuteAsync(null);

        _dialogService.Received(1).ShowWarning(
            Arg.Is<string>(s => s != null && s.Contains("already running as administrator")),
            Arg.Any<string>());
    }

    [Fact]
    public async Task ScanCommand_empty_installer_database_shows_targeted_error()
    {
        var vm = CreateViewModel();
        _scanService.ScanAsync(Arg.Any<IProgress<ScanProgressUpdate>?>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(new LocalisedInvalidOperationException(
                "The Windows Installer database appears to be empty or inaccessible."));

        await vm.Scan.ScanCommand.ExecuteAsync(null);

        _dialogService.Received(1).ShowError(
            Arg.Is<string>(s => s != null && s.Contains("installer database", StringComparison.OrdinalIgnoreCase)),
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
        _scanService.ScanAsync(Arg.Any<IProgress<ScanProgressUpdate>?>(), Arg.Any<CancellationToken>())
            .Returns(call =>
            {
                var ct = call.Arg<CancellationToken>();
                ct.Register(() => completion.TrySetCanceled(ct));
                entered.TrySetResult(true);
                return completion.Task;
            });

        var scanTask = vm.Scan.ScanCommand.ExecuteAsync(null);

        await entered.Task;
        vm.Scan.CancelScanCommand.Execute(null);

        await scanTask;

        Assert.Equal("Scan cancelled.", vm.Scan.ScanProgress);
        Assert.False(vm.Scan.IsScanning);
    }

    [Fact]
    public void CancelScanCommand_no_running_scan_is_no_op()
    {
        var vm = CreateViewModel();

        var ex = Record.Exception(() => vm.Scan.CancelScanCommand.Execute(null));

        Assert.Null(ex);
    }

    // Wait one debounce window plus a 300 ms margin so a fast machine
    // doesn't race the timer. Reads the constant from the production
    // VM rather than hardcoding 700, so a future tune of the debounce
    // doesn't silently drift the test out of sync.
    private static readonly TimeSpan DebounceWait =
        CleanupViewModel.MoveDestinationSaveDelay + TimeSpan.FromMilliseconds(300);

    // Applies a captured Update(Action<AppSettings>) to a fresh AppSettings so a
    // Received().Update(...) assertion can check the action's effect:
    // ISettingsService.Update takes a mutator action, not a prepared snapshot to save.
    private static AppSettings Applied(Action<AppSettings> mutate)
    {
        var settings = new AppSettings();
        mutate(settings);
        return settings;
    }

    [Fact]
    public async Task MoveDestination_change_is_persisted_through_settings_service()
    {
        var vm = CreateViewModel();

        vm.Cleanup.MoveDestination = @"D:\Backup\Installer-cache";
        await Task.Delay(DebounceWait);

        _settingsService.Received().Update(Arg.Is<Action<AppSettings>>(
            a => a != null && Applied(a).MoveDestination == @"D:\Backup\Installer-cache"));
    }

    [Fact]
    public void Closing_flushes_a_destination_edit_still_inside_its_debounce()
    {
        var vm = CreateViewModel();

        vm.Cleanup.MoveDestination = @"D:\Backup\picked-then-closed";
        // No wait: the debounce has not elapsed, so nothing has been saved yet.
        _settingsService.DidNotReceive().Update(Arg.Any<Action<AppSettings>>());

        vm.Dispose();

        // Browse to a folder, change your mind about cleaning up, close the
        // window: the destination used to be thrown away with the pending save,
        // and the box came back empty next session.
        _settingsService.Received(1).Update(Arg.Is<Action<AppSettings>>(
            a => a != null && Applied(a).MoveDestination == @"D:\Backup\picked-then-closed"));
    }

    [Fact]
    public async Task Closing_with_no_pending_destination_edit_writes_nothing()
    {
        var vm = CreateViewModel(new AppSettings { MoveDestination = @"D:\Backup" });
        vm.Cleanup.MoveDestination = @"D:\Backup\changed";
        await Task.Delay(DebounceWait);
        _settingsService.ClearReceivedCalls();

        vm.Dispose();

        // The debounced save already landed and nulled the pending marker, so
        // the flush must not fire a second, redundant write.
        _settingsService.DidNotReceive().Update(Arg.Any<Action<AppSettings>>());
    }

    [Fact]
    public async Task MoveDestination_setting_same_value_does_not_resave()
    {
        var vm = CreateViewModel(new AppSettings { MoveDestination = @"D:\Backup" });
        _settingsService.ClearReceivedCalls();

        vm.Cleanup.MoveDestination = @"D:\Backup";
        await Task.Delay(DebounceWait);

        _settingsService.DidNotReceive().Update(Arg.Any<Action<AppSettings>>());
    }

    [Fact]
    public async Task Move_and_Delete_are_blocked_from_the_first_instant_of_a_scan()
    {
        var vm = CreateViewModel();
        _scanService.ScanAsync(Arg.Any<IProgress<ScanProgressUpdate>?>(), Arg.Any<CancellationToken>())
            .Returns(ScanResultWithOrphans(2));
        await vm.Scan.ScanWithProgressAsync(null);
        vm.Cleanup.MoveDestination = Path.Combine(Path.GetTempPath(), "ic-test-scan-gate");
        Assert.True(vm.Cleanup.MoveAllCommand.CanExecute(null));
        Assert.True(vm.Cleanup.DeleteAllCommand.CanExecute(null));

        var release = new ManualResetEventSlim();
        _scanService.ScanAsync(Arg.Any<IProgress<ScanProgressUpdate>?>(), Arg.Any<CancellationToken>())
            .Returns(_ => Task.Run(() =>
            {
                release.Wait();
                return ScanResultWithOrphans(2);
            }));

        var scan = vm.Scan.ScanCommand.ExecuteAsync(null);

        // Asserted synchronously after the command's first await, so the scan
        // is provably still inside its 200 ms overlay-reveal delay: IsScanning
        // is false, and gating the buttons on it used to leave a window in
        // which a Delete could start against the previous scan's result while
        // this one walked the same folder.
        Assert.False(vm.Scan.IsScanning);
        Assert.True(vm.Scan.IsScanInFlight);
        Assert.False(vm.Cleanup.MoveAllCommand.CanExecute(null));
        Assert.False(vm.Cleanup.DeleteAllCommand.CanExecute(null));

        release.Set();
        await scan;

        Assert.False(vm.Scan.IsScanInFlight);
        Assert.True(vm.Cleanup.MoveAllCommand.CanExecute(null));
        Assert.True(vm.Cleanup.DeleteAllCommand.CanExecute(null));
    }

    [Fact]
    public async Task The_post_operation_refresh_blocks_a_parallel_scan()
    {
        var vm = CreateViewModel();
        var release = new ManualResetEventSlim();
        _scanService.ScanAsync(Arg.Any<IProgress<ScanProgressUpdate>?>(), Arg.Any<CancellationToken>())
            .Returns(_ => Task.Run(() =>
            {
                release.Wait();
                return ScanResultWithOrphans(2);
            }));

        // The refresh a Move or Delete runs on completion never sets IsScanning
        // (it must not: the operating overlay owns the screen at that point), so
        // it was invisible to the Scan command's CanExecute.
        var refresh = vm.Scan.RefreshAsync();

        Assert.True(vm.Scan.IsScanInFlight);
        Assert.False(vm.Scan.IsScanning);
        Assert.False(vm.Scan.ScanCommand.CanExecute(null));

        release.Set();
        await refresh;

        Assert.True(vm.Scan.ScanCommand.CanExecute(null));
    }

    [Fact]
    public async Task RescanAfterCompletion_dismisses_and_triggers_scan()
    {
        var vm = CreateViewModel();
        _scanService.ScanAsync(Arg.Any<IProgress<ScanProgressUpdate>?>(), Arg.Any<CancellationToken>())
            .Returns(EmptyScanResult());

        await vm.Scan.ScanWithProgressAsync(null);
        Assert.True(vm.Completion.IsComplete);

        await vm.Completion.RescanAfterCompletionCommand.ExecuteAsync(null);

        await _scanService.Received(2).ScanAsync(
            Arg.Any<IProgress<ScanProgressUpdate>?>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task MoveAllAsync_happy_path_moves_files_and_shows_completion()
    {
        var vm = CreateViewModel();
        var orphans = new List<OrphanedFile>
        {
            new(@"C:\Windows\Installer\a.msi", 1_048_576, false, false, false, Orphaned),
            new(@"C:\Windows\Installer\b.msi", 2_097_152, false, false, false, Orphaned),
        };
        _scanService.ScanAsync(Arg.Any<IProgress<ScanProgressUpdate>?>(), Arg.Any<CancellationToken>())
            .Returns(new ScanResult(orphans, Array.Empty<RegisteredPackage>(), 0));
        _moveService.MoveFilesAsync(
                Arg.Any<IEnumerable<string>>(), Arg.Any<string>(),
                Arg.Any<IProgress<OperationProgress>?>(), Arg.Any<CancellationToken>())
            .Returns(new MoveResult(2, Array.Empty<FileOperationError>()));
        _confirmationService.ConfirmMove(
            Arg.Any<int>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<bool>()).Returns(true);

        await vm.Scan.ScanWithProgressAsync(null);
        vm.Cleanup.MoveDestination = Path.Combine(Path.GetTempPath(), "ic-test-move");

        await vm.Cleanup.MoveAllCommand.ExecuteAsync(null);

        _confirmationService.Received(1).ConfirmMove(2, Arg.Any<string>(), vm.Cleanup.MoveDestination, Arg.Any<bool>());
        await _moveService.Received(1).MoveFilesAsync(
            Arg.Any<IEnumerable<string>>(), vm.Cleanup.MoveDestination,
            Arg.Any<IProgress<OperationProgress>?>(), Arg.Any<CancellationToken>());
        Assert.True(vm.Completion.IsComplete);
        // %TEMP% sits on the system drive, so this is a same-volume move
        // and the heading claims "moved", not "freed".
        Assert.Contains("moved", vm.Completion.Heading);
    }

    [Fact]
    public async Task MoveAllAsync_confirmation_cancelled_does_not_invoke_service()
    {
        var vm = CreateViewModel();
        _scanService.ScanAsync(Arg.Any<IProgress<ScanProgressUpdate>?>(), Arg.Any<CancellationToken>())
            .Returns(ScanResultWithOrphans(3));
        _confirmationService.ConfirmMove(
            Arg.Any<int>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<bool>()).Returns(false);

        await vm.Scan.ScanWithProgressAsync(null);
        vm.Cleanup.MoveDestination = Path.Combine(Path.GetTempPath(), "ic-test-move");

        await vm.Cleanup.MoveAllCommand.ExecuteAsync(null);

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
            new(@"C:\Windows\Installer\x.msi", 524_288, false, false, false, Orphaned),
        };
        _scanService.ScanAsync(Arg.Any<IProgress<ScanProgressUpdate>?>(), Arg.Any<CancellationToken>())
            .Returns(new ScanResult(orphans, Array.Empty<RegisteredPackage>(), 0));
        _deleteService.DeleteFilesAsync(
                Arg.Any<IEnumerable<string>>(), Arg.Any<bool>(),
                Arg.Any<IProgress<OperationProgress>?>(), Arg.Any<CancellationToken>())
            .Returns(new DeleteResult(1, Array.Empty<FileOperationError>()));
        _confirmationService.ConfirmDelete(
            Arg.Any<int>(), Arg.Any<string>()).Returns(true);

        await vm.Scan.ScanWithProgressAsync(null);

        await vm.Cleanup.DeleteAllCommand.ExecuteAsync(null);

        _confirmationService.Received(1).ConfirmDelete(1, Arg.Any<string>());
        await _deleteService.Received(1).DeleteFilesAsync(
            Arg.Any<IEnumerable<string>>(), Arg.Any<bool>(),
            Arg.Any<IProgress<OperationProgress>?>(), Arg.Any<CancellationToken>());
        Assert.True(vm.Completion.IsComplete);
        Assert.Contains("Recycle Bin", vm.Completion.Summary);
    }

    [Fact]
    public async Task MoveAllAsync_crash_surfaces_a_dialog_not_an_inline_status()
    {
        var vm = CreateViewModel();
        var orphans = new List<OrphanedFile>
        {
            new(@"C:\Windows\Installer\a.msi", 1_048_576, false, false, false, Orphaned),
        };
        _scanService.ScanAsync(Arg.Any<IProgress<ScanProgressUpdate>?>(), Arg.Any<CancellationToken>())
            .Returns(new ScanResult(orphans, Array.Empty<RegisteredPackage>(), 0));
        _moveService.MoveFilesAsync(
                Arg.Any<IEnumerable<string>>(), Arg.Any<string>(),
                Arg.Any<IProgress<OperationProgress>?>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(new IOException("boom"));
        _confirmationService.ConfirmMove(
            Arg.Any<int>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<bool>()).Returns(true);

        await vm.Scan.ScanWithProgressAsync(null);
        vm.Cleanup.MoveDestination = Path.Combine(Path.GetTempPath(), "ic-test-move-crash");

        await vm.Cleanup.MoveAllCommand.ExecuteAsync(null);

        // A crash mid-move surfaces as a dialog (type + full crash-log path),
        // like every other failure, not a body-row status that would trim the
        // path; the body row is cleared.
        _dialogService.Received(1).ShowWarning(Arg.Any<string>(), Strings.Error_MoveFailedTitle);
        Assert.Equal(string.Empty, vm.Cleanup.OperationProgress);
    }

    [Fact]
    public async Task DeleteAllAsync_crash_surfaces_a_dialog_not_an_inline_status()
    {
        var vm = CreateViewModel();
        var orphans = new List<OrphanedFile>
        {
            new(@"C:\Windows\Installer\x.msi", 524_288, false, false, false, Orphaned),
        };
        _scanService.ScanAsync(Arg.Any<IProgress<ScanProgressUpdate>?>(), Arg.Any<CancellationToken>())
            .Returns(new ScanResult(orphans, Array.Empty<RegisteredPackage>(), 0));
        _deleteService.DeleteFilesAsync(
                Arg.Any<IEnumerable<string>>(), Arg.Any<bool>(),
                Arg.Any<IProgress<OperationProgress>?>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(new IOException("boom"));
        _confirmationService.ConfirmDelete(
            Arg.Any<int>(), Arg.Any<string>()).Returns(true);

        await vm.Scan.ScanWithProgressAsync(null);

        await vm.Cleanup.DeleteAllCommand.ExecuteAsync(null);

        _dialogService.Received(1).ShowWarning(Arg.Any<string>(), Strings.Error_DeleteFailedTitle);
        Assert.Equal(string.Empty, vm.Cleanup.OperationProgress);
    }

    [Fact]
    public async Task DeleteAllAsync_confirmation_cancelled_does_not_invoke_service()
    {
        var vm = CreateViewModel();
        _scanService.ScanAsync(Arg.Any<IProgress<ScanProgressUpdate>?>(), Arg.Any<CancellationToken>())
            .Returns(ScanResultWithOrphans(2));
        _confirmationService.ConfirmDelete(
            Arg.Any<int>(), Arg.Any<string>()).Returns(false);

        await vm.Scan.ScanWithProgressAsync(null);

        await vm.Cleanup.DeleteAllCommand.ExecuteAsync(null);

        await _deleteService.DidNotReceive().DeleteFilesAsync(
            Arg.Any<IEnumerable<string>>(), Arg.Any<bool>(),
            Arg.Any<IProgress<OperationProgress>?>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task MoveAllAsync_cancelled_mid_batch_reports_the_partial_on_the_completion_overlay()
    {
        var vm = CreateViewModel();
        var orphans = new List<OrphanedFile>
        {
            new(@"C:\Windows\Installer\a.msi", 1_048_576, false, false, false, Orphaned),
            new(@"C:\Windows\Installer\b.msi", 2_097_152, false, false, false, Orphaned),
            new(@"C:\Windows\Installer\c.msi", 3_145_728, false, false, false, Orphaned),
        };
        _scanService.ScanAsync(Arg.Any<IProgress<ScanProgressUpdate>?>(), Arg.Any<CancellationToken>())
            .Returns(new ScanResult(orphans, Array.Empty<RegisteredPackage>(), 0));
        // The service now returns the partial with Cancelled set rather than
        // throwing the tally away: two moved before the stop.
        _moveService.MoveFilesAsync(
                Arg.Any<IEnumerable<string>>(), Arg.Any<string>(),
                Arg.Any<IProgress<OperationProgress>?>(), Arg.Any<CancellationToken>())
            .Returns(new MoveResult(2, Array.Empty<FileOperationError>(), Cancelled: true));
        _confirmationService.ConfirmMove(
            Arg.Any<int>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<bool>()).Returns(true);

        await vm.Scan.ScanWithProgressAsync(null);
        vm.Cleanup.MoveDestination = Path.Combine(Path.GetTempPath(), "ic-test-move-cancel");

        await vm.Cleanup.MoveAllCommand.ExecuteAsync(null);

        // The overlay carries "Moved 2 of 3 files before you cancelled.", not just
        // an empty status line as before.
        Assert.True(vm.Completion.IsComplete);
        Assert.Contains("2 of 3", vm.Completion.Summary);
        Assert.Contains("cancel", vm.Completion.Summary, StringComparison.OrdinalIgnoreCase);
        // A cancelled run writes no result-log entry (owner's decision keeps the
        // public reports stats meaning what they mean).
        await _resultLogService.DidNotReceive().WriteAsync(
            Arg.Any<ResultLogEntry>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task DeleteAllAsync_cancelled_mid_batch_reports_the_partial_on_the_completion_overlay()
    {
        var vm = CreateViewModel();
        var orphans = new List<OrphanedFile>
        {
            new(@"C:\Windows\Installer\x.msi", 524_288, false, false, false, Orphaned),
            new(@"C:\Windows\Installer\y.msi", 524_288, false, false, false, Orphaned),
        };
        _scanService.ScanAsync(Arg.Any<IProgress<ScanProgressUpdate>?>(), Arg.Any<CancellationToken>())
            .Returns(new ScanResult(orphans, Array.Empty<RegisteredPackage>(), 0));
        _deleteService.DeleteFilesAsync(
                Arg.Any<IEnumerable<string>>(), Arg.Any<bool>(),
                Arg.Any<IProgress<OperationProgress>?>(), Arg.Any<CancellationToken>())
            .Returns(new DeleteResult(1, Array.Empty<FileOperationError>(), Cancelled: true));
        _confirmationService.ConfirmDelete(
            Arg.Any<int>(), Arg.Any<string>()).Returns(true);

        await vm.Scan.ScanWithProgressAsync(null);

        await vm.Cleanup.DeleteAllCommand.ExecuteAsync(null);

        Assert.True(vm.Completion.IsComplete);
        Assert.Contains("1 of 2", vm.Completion.Summary);
        // A recycle cancel names the Recycle Bin; it did reach the bin for the one
        // that completed.
        Assert.Contains("Recycle Bin", vm.Completion.Summary);
        await _resultLogService.DidNotReceive().WriteAsync(
            Arg.Any<ResultLogEntry>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task MoveAllAsync_reboot_gate_flipping_blocked_at_action_time_refuses_and_paints_the_banner()
    {
        var vm = CreateViewModel();
        _scanService.ScanAsync(Arg.Any<IProgress<ScanProgressUpdate>?>(), Arg.Any<CancellationToken>())
            .Returns(ScanResultWithOrphans(2));
        // Clean when the scan samples it, blocked by the time the user commits the
        // move: a Windows Installer transaction started while the window sat open.
        _rebootService.Check().Returns(
            PendingRebootResult.Clean,
            PendingRebootResult.Block(PendingRebootReason.MsiExecuteMutexHeld));
        _confirmationService.ConfirmMove(
            Arg.Any<int>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<bool>()).Returns(true);

        await vm.Scan.ScanWithProgressAsync(null);
        Assert.False(vm.Scan.HasPendingReboot);
        vm.Cleanup.MoveDestination = Path.Combine(Path.GetTempPath(), "ic-test-reboot-gate");

        await vm.Cleanup.MoveAllCommand.ExecuteAsync(null);

        // The act-time re-check found it blocked: the banner paints and the move
        // service is never called.
        Assert.True(vm.Scan.HasPendingReboot);
        await _moveService.DidNotReceive().MoveFilesAsync(
            Arg.Any<IEnumerable<string>>(), Arg.Any<string>(),
            Arg.Any<IProgress<OperationProgress>?>(), Arg.Any<CancellationToken>());
        Assert.False(vm.Completion.IsComplete);
    }

    [Fact]
    public async Task DeleteAllAsync_reboot_gate_flipping_blocked_at_action_time_refuses_and_paints_the_banner()
    {
        var vm = CreateViewModel();
        _scanService.ScanAsync(Arg.Any<IProgress<ScanProgressUpdate>?>(), Arg.Any<CancellationToken>())
            .Returns(ScanResultWithOrphans(2));
        _rebootService.Check().Returns(
            PendingRebootResult.Clean,
            PendingRebootResult.Block(PendingRebootReason.InstallerInProgress));
        _confirmationService.ConfirmDelete(Arg.Any<int>(), Arg.Any<string>()).Returns(true);

        await vm.Scan.ScanWithProgressAsync(null);

        await vm.Cleanup.DeleteAllCommand.ExecuteAsync(null);

        Assert.True(vm.Scan.HasPendingReboot);
        await _deleteService.DidNotReceive().DeleteFilesAsync(
            Arg.Any<IEnumerable<string>>(), Arg.Any<bool>(),
            Arg.Any<IProgress<OperationProgress>?>(), Arg.Any<CancellationToken>());
        Assert.False(vm.Completion.IsComplete);
    }

    [Fact]
    public async Task MoveAllAsync_installer_busy_result_paints_the_banner_and_reports_no_completion()
    {
        var vm = CreateViewModel();
        _scanService.ScanAsync(Arg.Any<IProgress<ScanProgressUpdate>?>(), Arg.Any<CancellationToken>())
            .Returns(ScanResultWithOrphans(2));
        // Clean at the scan and at the act-time gate; a Windows Installer
        // transaction then grabs the mutex in the residual race, so the service
        // returns InstallerBusy and the re-run gate reports the held mutex.
        _rebootService.Check().Returns(
            PendingRebootResult.Clean,
            PendingRebootResult.Clean,
            PendingRebootResult.Block(PendingRebootReason.MsiExecuteMutexHeld));
        _moveService.MoveFilesAsync(
                Arg.Any<IEnumerable<string>>(), Arg.Any<string>(),
                Arg.Any<IProgress<OperationProgress>?>(), Arg.Any<CancellationToken>())
            .Returns(new MoveResult(0, Array.Empty<FileOperationError>(), InstallerBusy: true));
        _confirmationService.ConfirmMove(
            Arg.Any<int>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<bool>()).Returns(true);

        await vm.Scan.ScanWithProgressAsync(null);
        vm.Cleanup.MoveDestination = Path.Combine(Path.GetTempPath(), "ic-test-busy");

        await vm.Cleanup.MoveAllCommand.ExecuteAsync(null);

        // The banner paints and nothing is reported as completed.
        Assert.True(vm.Scan.HasPendingReboot);
        Assert.False(vm.Completion.IsComplete);
        await _resultLogService.DidNotReceive().WriteAsync(
            Arg.Any<ResultLogEntry>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task MoveAllAsync_reverify_dropping_one_acts_on_the_rest_and_reports_it_skipped()
    {
        var vm = CreateViewModel();
        var orphans = new List<OrphanedFile>
        {
            new(@"C:\Windows\Installer\a.msi", 1_048_576, false, false, false, Orphaned),
            new(@"C:\Windows\Installer\b.msi", 2_097_152, false, false, false, Orphaned),
            new(@"C:\Windows\Installer\c.msi", 3_145_728, false, false, false, Orphaned),
        };
        _scanService.ScanAsync(Arg.Any<IProgress<ScanProgressUpdate>?>(), Arg.Any<CancellationToken>())
            .Returns(new ScanResult(orphans, Array.Empty<RegisteredPackage>(), 0));
        // A program needs b.msi again since the scan, so the re-verify drops it.
        _reverifier.ReverifyAsync(Arg.Any<IReadOnlyList<string>>(), Arg.Any<CancellationToken>())
            .Returns(new ReverifyResult(
                new[] { @"C:\Windows\Installer\a.msi", @"C:\Windows\Installer\c.msi" },
                new[] { @"C:\Windows\Installer\b.msi" }));
        _moveService.MoveFilesAsync(
                Arg.Any<IEnumerable<string>>(), Arg.Any<string>(),
                Arg.Any<IProgress<OperationProgress>?>(), Arg.Any<CancellationToken>())
            .Returns(new MoveResult(2, Array.Empty<FileOperationError>()));
        _confirmationService.ConfirmMove(
            Arg.Any<int>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<bool>()).Returns(true);

        await vm.Scan.ScanWithProgressAsync(null);
        vm.Cleanup.MoveDestination = Path.Combine(Path.GetTempPath(), "ic-test-reverify-move");

        await vm.Cleanup.MoveAllCommand.ExecuteAsync(null);

        // The service acts on only the two survivors, never the dropped one.
        await _moveService.Received(1).MoveFilesAsync(
            Arg.Is<IEnumerable<string>>(paths =>
                paths != null && paths.Count() == 2 && !paths.Contains(@"C:\Windows\Installer\b.msi")),
            Arg.Any<string>(), Arg.Any<IProgress<OperationProgress>?>(), Arg.Any<CancellationToken>());
        Assert.True(vm.Completion.IsComplete);
        Assert.NotEqual(string.Empty, vm.Completion.Skipped);
        Assert.Contains("1", vm.Completion.Skipped);
    }

    [Fact]
    public async Task MoveAllAsync_reverify_throwing_stops_the_batch_and_surfaces_the_failure()
    {
        var vm = CreateViewModel();
        _scanService.ScanAsync(Arg.Any<IProgress<ScanProgressUpdate>?>(), Arg.Any<CancellationToken>())
            .Returns(ScanResultWithOrphans(2));
        _reverifier.ReverifyAsync(Arg.Any<IReadOnlyList<string>>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(new LocalisedInvalidOperationException(
                "The Windows Installer database appears to be empty or inaccessible."));
        _confirmationService.ConfirmMove(
            Arg.Any<int>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<bool>()).Returns(true);

        await vm.Scan.ScanWithProgressAsync(null);
        vm.Cleanup.MoveDestination = Path.Combine(Path.GetTempPath(), "ic-test-reverify-throw");

        await vm.Cleanup.MoveAllCommand.ExecuteAsync(null);

        // Never act on an un-verified batch: the move service is not called.
        await _moveService.DidNotReceive().MoveFilesAsync(
            Arg.Any<IEnumerable<string>>(), Arg.Any<string>(),
            Arg.Any<IProgress<OperationProgress>?>(), Arg.Any<CancellationToken>());
        // The failure surfaces through the scan error ladder, not a completion.
        _dialogService.Received(1).ShowError(Arg.Any<string>(), Strings.Error_InstallerDbUnavailableTitle);
        Assert.False(vm.Completion.IsComplete);
    }

    [Fact]
    public async Task MoveAllAsync_reverify_dropping_everything_calls_no_service_and_reports_all_skipped()
    {
        var vm = CreateViewModel();
        var orphans = new List<OrphanedFile>
        {
            new(@"C:\Windows\Installer\a.msi", 1_048_576, false, false, false, Orphaned),
            new(@"C:\Windows\Installer\b.msi", 2_097_152, false, false, false, Orphaned),
        };
        _scanService.ScanAsync(Arg.Any<IProgress<ScanProgressUpdate>?>(), Arg.Any<CancellationToken>())
            .Returns(new ScanResult(orphans, Array.Empty<RegisteredPackage>(), 0));
        _reverifier.ReverifyAsync(Arg.Any<IReadOnlyList<string>>(), Arg.Any<CancellationToken>())
            .Returns(new ReverifyResult(
                Array.Empty<string>(),
                new[] { @"C:\Windows\Installer\a.msi", @"C:\Windows\Installer\b.msi" }));
        _confirmationService.ConfirmMove(
            Arg.Any<int>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<bool>()).Returns(true);

        await vm.Scan.ScanWithProgressAsync(null);
        vm.Cleanup.MoveDestination = Path.Combine(Path.GetTempPath(), "ic-test-reverify-all");

        await vm.Cleanup.MoveAllCommand.ExecuteAsync(null);

        await _moveService.DidNotReceive().MoveFilesAsync(
            Arg.Any<IEnumerable<string>>(), Arg.Any<string>(),
            Arg.Any<IProgress<OperationProgress>?>(), Arg.Any<CancellationToken>());
        Assert.True(vm.Completion.IsComplete);
        Assert.Contains("2", vm.Completion.Summary);
    }

    [Fact]
    public async Task MoveAllAsync_a_degraded_reverify_reports_the_unread_records_not_a_reclaim()
    {
        // A re-verify that could not read the records keeps files back without any
        // program having reclaimed them. Reporting the reclaim reason there would
        // state a specific cause that did not happen, which is the fault this
        // release exists to fix, not a wording preference.
        var vm = CreateViewModel();
        var orphans = new List<OrphanedFile>
        {
            new(@"C:\Windows\Installer\a.msp", 1_048_576, true, true, false, Superseded),
        };
        _scanService.ScanAsync(Arg.Any<IProgress<ScanProgressUpdate>?>(), Arg.Any<CancellationToken>())
            .Returns(new ScanResult(orphans, Array.Empty<RegisteredPackage>(), 0));
        _reverifier.ReverifyAsync(Arg.Any<IReadOnlyList<string>>(), Arg.Any<CancellationToken>())
            .Returns(new ReverifyResult(
                Array.Empty<string>(),
                new[] { @"C:\Windows\Installer\a.msp" },
                RecordsIncomplete: true));
        _confirmationService.ConfirmMove(
            Arg.Any<int>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<bool>()).Returns(true);

        await vm.Scan.ScanWithProgressAsync(null);
        vm.Cleanup.MoveDestination = Path.Combine(Path.GetTempPath(), "ic-test-reverify-degraded");

        await vm.Cleanup.MoveAllCommand.ExecuteAsync(null);

        await _moveService.DidNotReceive().MoveFilesAsync(
            Arg.Any<IEnumerable<string>>(), Arg.Any<string>(),
            Arg.Any<IProgress<OperationProgress>?>(), Arg.Any<CancellationToken>());
        Assert.Equal(
            string.Format(Strings.Completion_ReverifyIncomplete, 1, DisplayHelpers.PluraliseFile(1)),
            vm.Completion.Summary);
    }

    [Fact]
    public async Task MoveAllAsync_a_healthy_reverify_still_reports_the_reclaim_reason()
    {
        var vm = CreateViewModel();
        var orphans = new List<OrphanedFile>
        {
            new(@"C:\Windows\Installer\a.msp", 1_048_576, true, true, false, Superseded),
            new(@"C:\Windows\Installer\b.msi", 1_048_576, false, false, false, Orphaned),
        };
        _scanService.ScanAsync(Arg.Any<IProgress<ScanProgressUpdate>?>(), Arg.Any<CancellationToken>())
            .Returns(new ScanResult(orphans, Array.Empty<RegisteredPackage>(), 0));
        _reverifier.ReverifyAsync(Arg.Any<IReadOnlyList<string>>(), Arg.Any<CancellationToken>())
            .Returns(new ReverifyResult(
                new[] { @"C:\Windows\Installer\b.msi" },
                new[] { @"C:\Windows\Installer\a.msp" }));
        _moveService.MoveFilesAsync(
                Arg.Any<IEnumerable<string>>(), Arg.Any<string>(),
                Arg.Any<IProgress<OperationProgress>?>(), Arg.Any<CancellationToken>())
            .Returns(new MoveResult(1, Array.Empty<FileOperationError>()));
        _confirmationService.ConfirmMove(
            Arg.Any<int>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<bool>()).Returns(true);

        await vm.Scan.ScanWithProgressAsync(null);
        vm.Cleanup.MoveDestination = Path.Combine(Path.GetTempPath(), "ic-test-reverify-healthy");

        await vm.Cleanup.MoveAllCommand.ExecuteAsync(null);

        Assert.Equal(
            string.Format(Strings.Completion_ReverifySkipped, 1, DisplayHelpers.PluraliseFile(1)),
            vm.Completion.Skipped);
    }

    [Fact]
    public async Task DeleteAllAsync_reverify_dropping_one_acts_on_the_rest_and_reports_it_skipped()
    {
        var vm = CreateViewModel();
        var orphans = new List<OrphanedFile>
        {
            new(@"C:\Windows\Installer\x.msi", 524_288, false, false, false, Orphaned),
            new(@"C:\Windows\Installer\y.msi", 524_288, false, false, false, Orphaned),
        };
        _scanService.ScanAsync(Arg.Any<IProgress<ScanProgressUpdate>?>(), Arg.Any<CancellationToken>())
            .Returns(new ScanResult(orphans, Array.Empty<RegisteredPackage>(), 0));
        _reverifier.ReverifyAsync(Arg.Any<IReadOnlyList<string>>(), Arg.Any<CancellationToken>())
            .Returns(new ReverifyResult(
                new[] { @"C:\Windows\Installer\x.msi" },
                new[] { @"C:\Windows\Installer\y.msi" }));
        _deleteService.DeleteFilesAsync(
                Arg.Any<IEnumerable<string>>(), Arg.Any<bool>(),
                Arg.Any<IProgress<OperationProgress>?>(), Arg.Any<CancellationToken>())
            .Returns(new DeleteResult(1, Array.Empty<FileOperationError>()));
        _confirmationService.ConfirmDelete(Arg.Any<int>(), Arg.Any<string>()).Returns(true);

        await vm.Scan.ScanWithProgressAsync(null);

        await vm.Cleanup.DeleteAllCommand.ExecuteAsync(null);

        await _deleteService.Received(1).DeleteFilesAsync(
            Arg.Is<IEnumerable<string>>(paths =>
                paths != null && paths.Count() == 1 && paths.Contains(@"C:\Windows\Installer\x.msi")),
            Arg.Any<bool>(), Arg.Any<IProgress<OperationProgress>?>(), Arg.Any<CancellationToken>());
        Assert.True(vm.Completion.IsComplete);
        Assert.Contains("1", vm.Completion.Skipped);
    }

    [Fact]
    public async Task DeleteAllAsync_recycle_unavailable_offers_choice_and_cancel_does_nothing()
    {
        // Bin unavailable for the volume: the recycle-first pass refuses
        // (DeletedCount 0, no errors, RecycleUnavailable true) and touches
        // nothing, so the VM offers the Move / permanent / cancel choice. On
        // Cancel nothing more happens: no permanent retry, no completion
        // overlay, and no telemetry write of a "deleted nothing" run.
        var vm = CreateViewModel();
        var orphans = new List<OrphanedFile>
        {
            new(@"C:\Windows\Installer\big.msi", 200_000_000, false, false, false, Orphaned),
        };
        _scanService.ScanAsync(Arg.Any<IProgress<ScanProgressUpdate>?>(), Arg.Any<CancellationToken>())
            .Returns(new ScanResult(orphans, Array.Empty<RegisteredPackage>(), 0));
        _deleteService.DeleteFilesAsync(
                Arg.Any<IEnumerable<string>>(), Arg.Is(false),
                Arg.Any<IProgress<OperationProgress>?>(), Arg.Any<CancellationToken>())
            .Returns(new DeleteResult(0, Array.Empty<FileOperationError>(), RecycleUnavailable: true));
        _confirmationService.ConfirmDelete(
            Arg.Any<int>(), Arg.Any<string>()).Returns(true);
        _confirmationService.ConfirmRecycleUnavailable(Arg.Any<int>(), Arg.Any<string>())
            .Returns(RecycleUnavailableChoice.Cancel);

        await vm.Scan.ScanWithProgressAsync(null);
        await vm.Cleanup.DeleteAllCommand.ExecuteAsync(null);

        _confirmationService.Received(1).ConfirmRecycleUnavailable(1, Arg.Any<string>());
        await _deleteService.DidNotReceive().DeleteFilesAsync(
            Arg.Any<IEnumerable<string>>(), Arg.Is(true),
            Arg.Any<IProgress<OperationProgress>?>(), Arg.Any<CancellationToken>());
        Assert.False(vm.Completion.IsComplete);
        await _resultLogService.DidNotReceive().WriteAsync(
            Arg.Any<ResultLogEntry>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task DeleteAllAsync_recycle_unavailable_offers_choice_without_starting_the_delete()
    {
        // When the volume cannot recycle, the recycle-first pass presents no
        // operation: the VM offers the Move / permanent / cancel choice
        // without calling DeleteFilesAsync, so no "Deleting..." overlay (and
        // no screen-reader announcement of it) appears for a pass that would
        // delete nothing. DeleteFilesAsync still re-checks and fails closed;
        // the probe here only governs whether the overlay shows.
        var vm = CreateViewModel();
        _deleteService.CanRecycleToVolume(Arg.Any<string>()).Returns(false);
        var orphans = new List<OrphanedFile>
        {
            new(@"C:\Windows\Installer\big.msi", 200_000_000, false, false, false, Orphaned),
        };
        _scanService.ScanAsync(Arg.Any<IProgress<ScanProgressUpdate>?>(), Arg.Any<CancellationToken>())
            .Returns(new ScanResult(orphans, Array.Empty<RegisteredPackage>(), 0));
        _confirmationService.ConfirmDelete(Arg.Any<int>(), Arg.Any<string>()).Returns(true);
        _confirmationService.ConfirmRecycleUnavailable(Arg.Any<int>(), Arg.Any<string>())
            .Returns(RecycleUnavailableChoice.Cancel);

        await vm.Scan.ScanWithProgressAsync(null);
        await vm.Cleanup.DeleteAllCommand.ExecuteAsync(null);

        _confirmationService.Received(1).ConfirmRecycleUnavailable(1, Arg.Any<string>());
        await _deleteService.DidNotReceive().DeleteFilesAsync(
            Arg.Any<IEnumerable<string>>(), Arg.Is(false),
            Arg.Any<IProgress<OperationProgress>?>(), Arg.Any<CancellationToken>());
        Assert.False(vm.Cleanup.IsOperating);
        Assert.False(vm.Completion.IsComplete);
    }

    [Fact]
    public async Task DeleteAllAsync_recycle_unavailable_delete_permanently_redeletes_with_consent()
    {
        // Choosing "delete permanently" re-runs the delete with consent. The
        // completion copy must say the files were permanently deleted, never
        // that they were sent to the Recycle Bin.
        var vm = CreateViewModel();
        var orphans = new List<OrphanedFile>
        {
            new(@"C:\Windows\Installer\big.msi", 200_000_000, false, false, false, Orphaned),
        };
        _scanService.ScanAsync(Arg.Any<IProgress<ScanProgressUpdate>?>(), Arg.Any<CancellationToken>())
            .Returns(new ScanResult(orphans, Array.Empty<RegisteredPackage>(), 0));
        // First (recycle) pass refuses; the consented retry succeeds.
        _deleteService.DeleteFilesAsync(
                Arg.Any<IEnumerable<string>>(), Arg.Is(false),
                Arg.Any<IProgress<OperationProgress>?>(), Arg.Any<CancellationToken>())
            .Returns(new DeleteResult(0, Array.Empty<FileOperationError>(), RecycleUnavailable: true));
        _deleteService.DeleteFilesAsync(
                Arg.Any<IEnumerable<string>>(), Arg.Is(true),
                Arg.Any<IProgress<OperationProgress>?>(), Arg.Any<CancellationToken>())
            .Returns(new DeleteResult(1, Array.Empty<FileOperationError>()));
        _confirmationService.ConfirmDelete(
            Arg.Any<int>(), Arg.Any<string>()).Returns(true);
        _confirmationService.ConfirmRecycleUnavailable(Arg.Any<int>(), Arg.Any<string>())
            .Returns(RecycleUnavailableChoice.DeletePermanently);

        await vm.Scan.ScanWithProgressAsync(null);
        await vm.Cleanup.DeleteAllCommand.ExecuteAsync(null);

        await _deleteService.Received(1).DeleteFilesAsync(
            Arg.Any<IEnumerable<string>>(), Arg.Is(true),
            Arg.Any<IProgress<OperationProgress>?>(), Arg.Any<CancellationToken>());
        Assert.True(vm.Completion.IsComplete);
        Assert.Contains("permanently deleted", vm.Completion.Summary);
        Assert.DoesNotContain("sent to the Recycle Bin", vm.Completion.Summary);
        Assert.Equal(Strings.Completion_PermanentDeleteRestoreHint_Singular, vm.Completion.Restore);
    }

    [Fact]
    public async Task DeleteAllAsync_recycle_unavailable_move_instead_routes_to_move_flow()
    {
        // Choosing "Move instead" routes into the standard Move flow (with a
        // destination already set, so no folder picker is needed). The move
        // service must be invoked and no permanent delete must happen.
        var vm = CreateViewModel();
        var orphans = new List<OrphanedFile>
        {
            new(@"C:\Windows\Installer\big.msi", 1_048_576, false, false, false, Orphaned),
        };
        _scanService.ScanAsync(Arg.Any<IProgress<ScanProgressUpdate>?>(), Arg.Any<CancellationToken>())
            .Returns(new ScanResult(orphans, Array.Empty<RegisteredPackage>(), 0));
        _deleteService.DeleteFilesAsync(
                Arg.Any<IEnumerable<string>>(), Arg.Is(false),
                Arg.Any<IProgress<OperationProgress>?>(), Arg.Any<CancellationToken>())
            .Returns(new DeleteResult(0, Array.Empty<FileOperationError>(), RecycleUnavailable: true));
        _confirmationService.ConfirmDelete(
            Arg.Any<int>(), Arg.Any<string>()).Returns(true);
        _confirmationService.ConfirmRecycleUnavailable(Arg.Any<int>(), Arg.Any<string>())
            .Returns(RecycleUnavailableChoice.MoveInstead);
        _confirmationService.ConfirmMove(
            Arg.Any<int>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<bool>()).Returns(true);
        _moveService.MoveFilesAsync(
                Arg.Any<IEnumerable<string>>(), Arg.Any<string>(),
                Arg.Any<IProgress<OperationProgress>?>(), Arg.Any<CancellationToken>())
            .Returns(new MoveResult(1, Array.Empty<FileOperationError>()));

        await vm.Scan.ScanWithProgressAsync(null);
        vm.Cleanup.MoveDestination = Path.Combine(Path.GetTempPath(), "ic-test-move-instead");
        await vm.Cleanup.DeleteAllCommand.ExecuteAsync(null);

        // The Move-instead choice routed into the standard Move flow.
        await _moveService.Received(1).MoveFilesAsync(
            Arg.Any<IEnumerable<string>>(), vm.Cleanup.MoveDestination,
            Arg.Any<IProgress<OperationProgress>?>(), Arg.Any<CancellationToken>());
        // No permanent delete happened.
        await _deleteService.DidNotReceive().DeleteFilesAsync(
            Arg.Any<IEnumerable<string>>(), Arg.Is(true),
            Arg.Any<IProgress<OperationProgress>?>(), Arg.Any<CancellationToken>());
        Assert.True(vm.Completion.IsComplete);
        // %TEMP% sits on the system drive, so the routed move is
        // same-volume and the heading claims "moved", not "freed".
        Assert.Contains("moved", vm.Completion.Heading);
    }

    [Fact]
    public async Task OpenOrphanedDetails_after_scan_invokes_window_service_with_scanned_files()
    {
        var vm = CreateViewModel();
        _scanService.ScanAsync(Arg.Any<IProgress<ScanProgressUpdate>?>(), Arg.Any<CancellationToken>())
            .Returns(ScanResultWithOrphans(2));
        await vm.Scan.ScanWithProgressAsync(null);

        vm.Chrome.OpenOrphanedDetailsCommand.Execute(null);

        _windowService.Received(1).ShowOrphanedDetails(
            Arg.Is<OrphanedFilesViewModel>(v => v != null && v.Files.Count == 2));
    }

    [Fact]
    public void OpenOrphanedDetails_without_scan_is_noop()
    {
        var vm = CreateViewModel();

        vm.Chrome.OpenOrphanedDetailsCommand.Execute(null);

        _windowService.DidNotReceive().ShowOrphanedDetails(Arg.Any<OrphanedFilesViewModel>());
    }

    [Fact]
    public async Task OpenDetails_CanExecute_flips_after_first_scan()
    {
        // The Details buttons are bound through CanExecute. If the
        // ChromeViewModel ever stops listening for the right scan-VM
        // PropertyChanged event, the buttons stay greyed forever in
        // the UI even though the rest of the app works. Drive the
        // CanExecute path explicitly so a regression is loud.
        var vm = CreateViewModel();
        Assert.False(vm.Chrome.OpenOrphanedDetailsCommand.CanExecute(null));
        Assert.False(vm.Chrome.OpenRegisteredDetailsCommand.CanExecute(null));

        _scanService.ScanAsync(Arg.Any<IProgress<ScanProgressUpdate>?>(), Arg.Any<CancellationToken>())
            .Returns(ScanResultWithOrphans(1));
        await vm.Scan.ScanWithProgressAsync(null);

        Assert.True(vm.Chrome.OpenOrphanedDetailsCommand.CanExecute(null));
        Assert.True(vm.Chrome.OpenRegisteredDetailsCommand.CanExecute(null));
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
        _scanService.ScanAsync(Arg.Any<IProgress<ScanProgressUpdate>?>(), Arg.Any<CancellationToken>())
            .Returns(new ScanResult(Array.Empty<OrphanedFile>(), packages, 3072));
        await vm.Scan.ScanWithProgressAsync(null);

        vm.Chrome.OpenRegisteredDetailsCommand.Execute(null);

        _windowService.Received(1).ShowRegisteredDetails(
            Arg.Is<RegisteredFilesViewModel>(v => v != null && v.Products.Count == 2));
    }

    [Fact]
    public void ShowAbout_invokes_window_service()
    {
        var vm = CreateViewModel();

        vm.Chrome.ShowAboutCommand.Execute(null);

        _windowService.Received(1).ShowAbout();
    }

    [Fact]
    public void CloseApp_invokes_window_service()
    {
        var vm = CreateViewModel();

        vm.Chrome.CloseAppCommand.Execute(null);

        _windowService.Received(1).CloseMainWindow();
    }

    [Fact]
    public void StarOnGitHub_opens_repo_url()
    {
        var vm = CreateViewModel();

        vm.Chrome.StarOnGitHubCommand.Execute(null);

        _windowService.Received(1).OpenUrl("https://github.com/no-faff/InstallerClean");
    }

    [Fact]
    public void Donate_opens_no_faff_url()
    {
        var vm = CreateViewModel();

        vm.Chrome.DonateCommand.Execute(null);

        _windowService.Received(1).OpenUrl("https://nofaff.netlify.app/support");
    }

    // Result-log persistence path. The lifetime lock
    // (AppSettings.HasSentResultLog) is the contract behind "one report
    // ever per machine". The three tests below pin its load-bearing
    // behaviours:
    //
    //   - A successful Send persists HasSentResultLog=true to settings.
    //   - A failed Send does NOT persist, so a transient timeout on the
    //     first-ever click doesn't permanently lock the user out without
    //     anything reaching the receiver.
    //   - A Send invoked when last-run.json is unreadable (missing,
    //     oversize, IO failure) skips the modal AND the wire call and
    //     does not persist either.

    [Fact]
    public async Task SendResultLog_success_persists_HasSentResultLog_to_settings()
    {
        var settings = new AppSettings();
        var vm = CreateViewModel(settings);
        _resultLogService.ReadLastLogAsync().Returns(Task.FromResult<string?>("{\"schemaVersion\":1}"));
        _confirmationService.ConfirmSendResultLog(Arg.Any<string>()).Returns(true);
        _resultLogService.SendAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(ResultLogSendOutcome.Sent);

        await vm.Completion.SendResultLogCommand.ExecuteAsync(null);
        // The write runs off the dispatcher, so it is Dispose that guarantees
        // it landed. That is the barrier the app relies on too: the Send click
        // is often the last thing a user does before closing the window.
        vm.Dispose();

        _settingsService.Received().Update(Arg.Is<Action<AppSettings>>(a => a != null && Applied(a).HasSentResultLog));
    }

    [Fact]
    public async Task SendResultLog_network_failure_does_not_persist_HasSentResultLog()
    {
        var settings = new AppSettings();
        var vm = CreateViewModel(settings);
        _resultLogService.ReadLastLogAsync().Returns(Task.FromResult<string?>("{\"schemaVersion\":1}"));
        _confirmationService.ConfirmSendResultLog(Arg.Any<string>()).Returns(true);
        _resultLogService.SendAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(ResultLogSendOutcome.NetworkUnavailable);

        await vm.Completion.SendResultLogCommand.ExecuteAsync(null);
        vm.Dispose();

        _settingsService.DidNotReceive().Update(Arg.Is<Action<AppSettings>>(a => a != null && Applied(a).HasSentResultLog));
    }

    [Fact]
    public async Task SendResultLog_with_unreadable_log_skips_modal_and_send_and_does_not_persist()
    {
        var settings = new AppSettings();
        var vm = CreateViewModel(settings);
        _resultLogService.ReadLastLogAsync().Returns(Task.FromResult<string?>(null));

        await vm.Completion.SendResultLogCommand.ExecuteAsync(null);
        vm.Dispose();

        _confirmationService.DidNotReceive().ConfirmSendResultLog(Arg.Any<string>());
        await _resultLogService.DidNotReceive().SendAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
        _settingsService.DidNotReceive().Update(Arg.Is<Action<AppSettings>>(a => a != null && Applied(a).HasSentResultLog));
    }

    [Fact]
    public async Task MoveAllAsync_skips_last_run_log_write_when_lifetime_lock_set()
    {
        // Settings come in with HasSentResultLog=true (the user sent
        // in a previous session). _alreadySentBeforeThisSession is
        // therefore true at MainViewModel construction; Completion
        // .IsResultLogLocked reads it. CleanupViewModel must not
        // write last-run.json because the file has no consumer.
        var vm = CreateViewModel(new AppSettings { HasSentResultLog = true });
        var orphans = new List<OrphanedFile>
        {
            new(@"C:\Windows\Installer\a.msi", 1024, false, false, false, Orphaned),
        };
        _scanService.ScanAsync(Arg.Any<IProgress<ScanProgressUpdate>?>(), Arg.Any<CancellationToken>())
            .Returns(new ScanResult(orphans, Array.Empty<RegisteredPackage>(), 0));
        _moveService.MoveFilesAsync(
                Arg.Any<IEnumerable<string>>(), Arg.Any<string>(),
                Arg.Any<IProgress<OperationProgress>?>(), Arg.Any<CancellationToken>())
            .Returns(new MoveResult(1, Array.Empty<FileOperationError>()));
        _confirmationService.ConfirmMove(
            Arg.Any<int>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<bool>()).Returns(true);

        await vm.Scan.ScanWithProgressAsync(null);
        vm.Cleanup.MoveDestination = Path.Combine(Path.GetTempPath(), "ic-locked-move");
        await vm.Cleanup.MoveAllCommand.ExecuteAsync(null);

        await _resultLogService.DidNotReceive().WriteAsync(
            Arg.Any<ResultLogEntry>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task OnScanCompleted_skips_last_run_log_write_when_all_clear_and_lifetime_lock_set()
    {
        // The all-clear path in MainViewModel.OnScanCompleted runs after
        // a scan that returns zero orphans. The IsResultLogLocked gate
        // must skip WriteAsync because no Send path exists to drain the
        // resulting last-run.json: the Send button stays hidden via the
        // lifetime lock for the rest of the user's time on this machine.
        // Without the gate the file is overwritten on every all-clear
        // with a payload nobody can read.
        var vm = CreateViewModel(new AppSettings { HasSentResultLog = true });
        _scanService.ScanAsync(Arg.Any<IProgress<ScanProgressUpdate>?>(), Arg.Any<CancellationToken>())
            .Returns(new ScanResult(
                Array.Empty<OrphanedFile>(),
                Array.Empty<RegisteredPackage>(),
                0));

        await vm.Scan.ScanWithProgressAsync(null);

        await _resultLogService.DidNotReceive().WriteAsync(
            Arg.Any<ResultLogEntry>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task DeleteAllAsync_skips_last_run_log_write_after_in_session_send()
    {
        // First a successful Send flips the in-session lock. Then a
        // Delete runs; the IsResultLogLocked OR property covers
        // the in-session-only case (lifetime lock from settings is
        // still false at construction).
        var vm = CreateViewModel();
        _resultLogService.ReadLastLogAsync().Returns(Task.FromResult<string?>("{\"schemaVersion\":1}"));
        _confirmationService.ConfirmSendResultLog(Arg.Any<string>()).Returns(true);
        _resultLogService.SendAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(ResultLogSendOutcome.Sent);

        await vm.Completion.SendResultLogCommand.ExecuteAsync(null);
        Assert.True(vm.Completion.IsResultLogLocked);
        _resultLogService.ClearReceivedCalls();

        var orphans = new List<OrphanedFile>
        {
            new(@"C:\Windows\Installer\y.msi", 2048, false, false, false, Orphaned),
        };
        _scanService.ScanAsync(Arg.Any<IProgress<ScanProgressUpdate>?>(), Arg.Any<CancellationToken>())
            .Returns(new ScanResult(orphans, Array.Empty<RegisteredPackage>(), 0));
        _deleteService.DeleteFilesAsync(
                Arg.Any<IEnumerable<string>>(), Arg.Any<bool>(),
                Arg.Any<IProgress<OperationProgress>?>(), Arg.Any<CancellationToken>())
            .Returns(new DeleteResult(1, Array.Empty<FileOperationError>()));
        _confirmationService.ConfirmDelete(
            Arg.Any<int>(), Arg.Any<string>()).Returns(true);

        await vm.Scan.ScanWithProgressAsync(null);
        await vm.Cleanup.DeleteAllCommand.ExecuteAsync(null);

        await _resultLogService.DidNotReceive().WriteAsync(
            Arg.Any<ResultLogEntry>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task RescanAfterCompletion_suppresses_the_next_all_clear_prompt()
    {
        // First scan finds zero orphans. The all-clear path calls
        // WriteAsync (which the mock returns true for) and
        // MarkResultLogReady, so the Send button becomes visible.
        // Rescan-from-completion sets the one-shot suppression flag;
        // the second scan's all-clear path consumes the flag and
        // does NOT call WriteAsync or MarkResultLogReady. The button
        // stays hidden even though the second all-clear ran.
        var vm = CreateViewModel();
        _scanService.ScanAsync(Arg.Any<IProgress<ScanProgressUpdate>?>(), Arg.Any<CancellationToken>())
            .Returns(EmptyScanResult());
        _resultLogService.WriteAsync(Arg.Any<ResultLogEntry>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(true));

        await vm.Scan.ScanCommand.ExecuteAsync(null);
        Assert.True(vm.Completion.IsResultLogReady);
        Assert.True(vm.Completion.IsSendResultLogVisible);

        _resultLogService.ClearReceivedCalls();

        await vm.Completion.RescanAfterCompletionCommand.ExecuteAsync(null);

        Assert.False(vm.Completion.IsResultLogReady);
        Assert.False(vm.Completion.IsSendResultLogVisible);
        await _resultLogService.DidNotReceive().WriteAsync(
            Arg.Any<ResultLogEntry>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ScanViewModel_HasMissingFromDisk_tracks_MissingNonRemovableCount()
    {
        // HasMissingFromDisk fires on the non-removable count alone.
        // The removable+missing case (Windows considers them removed,
        // the file having gone is the expected end state) counts
        // separately so the load-bearing banner does not fire on it.
        var vm = CreateViewModel();

        var nonRemovable = new RegisteredPackage(
            @"C:\Windows\Installer\nonremovable.msi", "Product", "{aaa}",
            IsRemovable: false, FileExists: false);
        var scan = new ScanResult(
            RemovableFiles: Array.Empty<OrphanedFile>(),
            RegisteredPackages: new[] { nonRemovable },
            RegisteredTotalBytes: 0,
            MissingNonRemovableCount: 1);
        _scanService.ScanAsync(Arg.Any<IProgress<ScanProgressUpdate>?>(), Arg.Any<CancellationToken>())
            .Returns(scan);

        await vm.Scan.ScanCommand.ExecuteAsync(null);

        Assert.True(vm.Scan.HasMissingFromDisk);
        Assert.Equal(1, vm.Scan.MissingNonRemovableCount);
        Assert.Contains("1", vm.Scan.MissingFromDiskSummaryText);
    }

    [Fact]
    public async Task ScanViewModel_HasStaleMsiEntries_tracks_MissingRemovableCount()
    {
        // The stale-MSI banner sources from MissingRemovableCount, a
        // separate counter from the load-bearing missing-from-disk
        // banner. Removable+missing is the expected end state of a
        // patch the API still claims but the file has already been
        // cleaned away.
        var vm = CreateViewModel();

        var removable = new RegisteredPackage(
            @"C:\Windows\Installer\removable.msp", "Patch", "{bbb}",
            IsRemovable: true, FileExists: false);
        var scan = new ScanResult(
            RemovableFiles: Array.Empty<OrphanedFile>(),
            RegisteredPackages: new[] { removable },
            RegisteredTotalBytes: 0,
            MissingNonRemovableCount: 0,
            MissingRemovableCount: 2);
        _scanService.ScanAsync(Arg.Any<IProgress<ScanProgressUpdate>?>(), Arg.Any<CancellationToken>())
            .Returns(scan);

        await vm.Scan.ScanCommand.ExecuteAsync(null);

        Assert.True(vm.Scan.HasStaleMsiEntries);
        Assert.False(vm.Scan.HasMissingFromDisk);
        Assert.Equal(2, vm.Scan.MissingRemovableCount);
        Assert.Contains("2", vm.Scan.StaleMsiEntriesText);
    }

    [Fact]
    public async Task ScanViewModel_HasUnreadableProducts_tracks_UnreadableProductCount()
    {
        // A scan that could not read every program's records kept its superseded
        // patches back. Without this line the only symptom is a quietly shorter
        // list, so the line is the whole point of the count reaching the VM.
        var vm = CreateViewModel();

        var scan = new ScanResult(
            RemovableFiles: Array.Empty<OrphanedFile>(),
            RegisteredPackages: Array.Empty<RegisteredPackage>(),
            RegisteredTotalBytes: 0,
            UnreadableProductCount: 3);
        _scanService.ScanAsync(Arg.Any<IProgress<ScanProgressUpdate>?>(), Arg.Any<CancellationToken>())
            .Returns(scan);

        await vm.Scan.ScanCommand.ExecuteAsync(null);

        Assert.True(vm.Scan.HasUnreadableProducts);
        Assert.Equal(3, vm.Scan.UnreadableProductCount);
        Assert.Contains("3", vm.Scan.ProgramsUnreadableText);
    }

    [Fact]
    public async Task ScanViewModel_HasUnreadableProducts_is_false_on_a_healthy_scan()
    {
        var vm = CreateViewModel();
        _scanService.ScanAsync(Arg.Any<IProgress<ScanProgressUpdate>?>(), Arg.Any<CancellationToken>())
            .Returns(new ScanResult(
                Array.Empty<OrphanedFile>(), Array.Empty<RegisteredPackage>(), 0));

        await vm.Scan.ScanCommand.ExecuteAsync(null);

        Assert.False(vm.Scan.HasUnreadableProducts);
    }

    [Fact]
    public void Before_any_scan_the_intro_says_so_and_offers_nothing_to_act_on()
    {
        var vm = CreateViewModel();

        Assert.Equal(Strings.Body_NotScanned_Lead, vm.IntroLead);
        Assert.Equal(Strings.Body_NotScanned_Why, vm.IntroDetail);
        Assert.Equal(string.Empty, vm.IntroNotice);
        Assert.False(vm.Scan.HasOrphans);
        // The action zone and both count rows hang off these two: nothing has
        // been scanned, so there are no counts to show and nothing to move.
        Assert.False(vm.Scan.HasScanned);
        Assert.False(vm.Chrome.OpenOrphanedDetailsCommand.CanExecute(null));
    }

    [Fact]
    public async Task A_cancelled_startup_scan_leaves_the_window_saying_it_was_cancelled()
    {
        var vm = CreateViewModel();
        _scanService.ScanAsync(Arg.Any<IProgress<ScanProgressUpdate>?>(), Arg.Any<CancellationToken>())
            .Throws(new OperationCanceledException());

        await Assert.ThrowsAsync<OperationCanceledException>(
            () => vm.Scan.ScanWithProgressAsync(null));

        // The cancellation reaches App.OnStartup, which opens the main window
        // anyway. Before this, that window painted "0 unneeded files to clean
        // up" and "0 files still needed": a clean bill of health for a scan that
        // never ran.
        Assert.False(vm.Scan.HasScanned);
        Assert.True(vm.Scan.LastScanWasCancelled);
        Assert.Equal(Strings.Body_NotScanned_Lead, vm.IntroLead);
        Assert.Equal(Strings.Status_ScanCancelled, vm.IntroNotice);
    }

    [Fact]
    public async Task A_failed_startup_scan_opens_the_window_with_the_tailored_error_and_does_not_throw()
    {
        var vm = CreateViewModel();
        _scanService.ScanAsync(Arg.Any<IProgress<ScanProgressUpdate>?>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(new LocalisedInvalidOperationException(
                "The Windows Installer database appears to be empty or inaccessible."));

        // Must NOT throw: App.OnStartup used to catch the propagated exception and
        // exit. It now returns, so the window opens.
        await vm.Scan.ScanWithProgressAsync(null);

        Assert.False(vm.Scan.HasScanned);
        Assert.True(vm.Scan.HasScanError);
        Assert.Contains("installer database", vm.Scan.LastScanError, StringComparison.OrdinalIgnoreCase);
        // The window's intro shows the diagnosis, not "nothing scanned yet", and
        // the startup path is inline: no modal fires over the splash.
        Assert.Equal(Strings.Error_ScanFailedTitle, vm.IntroLead);
        Assert.Equal(vm.Scan.LastScanError, vm.IntroDetail);
        Assert.Equal(string.Empty, vm.IntroNotice);
        _dialogService.DidNotReceive().ShowError(Arg.Any<string>(), Arg.Any<string>());
        _dialogService.DidNotReceive().ShowWarning(Arg.Any<string>(), Arg.Any<string>());
    }

    [Fact]
    public async Task A_failed_rescan_shows_the_modal_and_records_the_same_inline_error()
    {
        var vm = CreateViewModel();
        _scanService.ScanAsync(Arg.Any<IProgress<ScanProgressUpdate>?>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(new LocalisedInvalidOperationException(
                "The Windows Installer database appears to be empty or inaccessible."));

        await vm.Scan.ScanCommand.ExecuteAsync(null);

        // The explicit click still gets its modal (one error ladder, two
        // presentations)...
        _dialogService.Received(1).ShowError(Arg.Any<string>(), Strings.Error_InstallerDbUnavailableTitle);
        // ...and the same message is recorded inline, so a later re-render of the
        // window shows the diagnosis rather than a stale count.
        Assert.True(vm.Scan.HasScanError);
        Assert.Equal(Strings.Error_ScanFailedTitle, vm.IntroLead);
    }

    [Fact]
    public async Task A_successful_scan_leaves_no_scan_error()
    {
        var vm = CreateViewModel();
        _scanService.ScanAsync(Arg.Any<IProgress<ScanProgressUpdate>?>(), Arg.Any<CancellationToken>())
            .Returns(ScanResultWithOrphans(2));

        await vm.Scan.ScanCommand.ExecuteAsync(null);

        Assert.False(vm.Scan.HasScanError);
        Assert.Equal(string.Empty, vm.Scan.LastScanError);
    }

    [Fact]
    public async Task A_scan_that_finds_nothing_stops_telling_the_user_to_delete_files()
    {
        var vm = CreateViewModel();
        _scanService.ScanAsync(Arg.Any<IProgress<ScanProgressUpdate>?>(), Arg.Any<CancellationToken>())
            .Returns(EmptyScanResult());

        await vm.Scan.ScanCommand.ExecuteAsync(null);

        // The lead used to read "The unneeded files below are safe to delete"
        // above a zero count, with Move and Delete greyed out beneath it.
        Assert.Equal(Strings.Completion_NothingToCleanUp, vm.IntroLead);
        Assert.Equal(string.Empty, vm.IntroDetail);
        Assert.True(vm.Scan.HasScanned);
        Assert.False(vm.Scan.HasOrphans);
        // A live Details button here opened an empty list.
        Assert.False(vm.Chrome.OpenOrphanedDetailsCommand.CanExecute(null));
    }

    [Fact]
    public async Task A_scan_that_finds_files_keeps_the_full_intro_and_the_action_zone()
    {
        var vm = CreateViewModel();
        _scanService.ScanAsync(Arg.Any<IProgress<ScanProgressUpdate>?>(), Arg.Any<CancellationToken>())
            .Returns(ScanResultWithOrphans(3));

        await vm.Scan.ScanCommand.ExecuteAsync(null);

        Assert.Equal(Strings.Body_MainExplanation_Lead, vm.IntroLead);
        Assert.Equal(vm.MainExplanationWhyText, vm.IntroDetail);
        Assert.Equal(string.Empty, vm.IntroNotice);
        Assert.True(vm.Scan.HasOrphans);
        Assert.True(vm.ShowMainAction);
        Assert.True(vm.Chrome.OpenOrphanedDetailsCommand.CanExecute(null));
    }

    [Fact]
    public async Task A_scan_that_finds_files_while_an_install_runs_explains_the_hold_not_the_action()
    {
        var vm = CreateViewModel();
        _scanService.ScanAsync(Arg.Any<IProgress<ScanProgressUpdate>?>(), Arg.Any<CancellationToken>())
            .Returns(ScanResultWithOrphans(3));
        // A Windows Installer transaction is in progress when the scan finishes.
        _rebootService.Check().Returns(PendingRebootResult.Block(PendingRebootReason.MsiExecuteMutexHeld));

        await vm.Scan.ScanCommand.ExecuteAsync(null);

        Assert.True(vm.Scan.HasOrphans);
        Assert.True(vm.Scan.HasPendingReboot);
        // The intro explains the hold rather than telling the user to act, and the
        // action line collapses; the reason-specific banner carries the why.
        Assert.Equal(Strings.Body_PendingReboot_Lead, vm.IntroLead);
        Assert.Equal(string.Empty, vm.IntroDetail);
        Assert.False(vm.ShowMainAction);
        // Move and Delete are held.
        Assert.False(vm.Cleanup.MoveAllCommand.CanExecute(null));
        Assert.False(vm.Cleanup.DeleteAllCommand.CanExecute(null));
    }

    [Fact]
    public void MainExplanationWhyText_carries_all_three_reason_labels()
    {
        // The "why" sentence of the intro carries the three Reason format
        // slots; a missing arg would surface as a literal "{2}" in the
        // rendered text rather than a localised tag.
        var vm = CreateViewModel();

        Assert.Contains(Strings.Reason_Orphaned, vm.MainExplanationWhyText);
        Assert.Contains(Strings.Reason_Superseded, vm.MainExplanationWhyText);
        Assert.Contains(Strings.Reason_Obsoleted, vm.MainExplanationWhyText);
        Assert.DoesNotContain("{0}", vm.MainExplanationWhyText);
        Assert.DoesNotContain("{1}", vm.MainExplanationWhyText);
        Assert.DoesNotContain("{2}", vm.MainExplanationWhyText);
    }

    [Fact]
    public void A_language_switch_relaunches_when_no_scan_or_operation_is_in_flight()
    {
        var vm = CreateViewModel();
        Assert.False(vm.IsBusy);

        // A culture guaranteed to differ from the active one, so the switch is a
        // real change and not the same-language no-op.
        var active = InstallerClean.Helpers.SupportedLanguages.Active(
            InstallerClean.Helpers.Localisation.UiCulture);
        var different = string.Equals(active, "fr", StringComparison.OrdinalIgnoreCase) ? "de" : "fr";

        vm.Chrome.SetLanguageCommand.Execute(different);

        // Idle, so the new busy check does not block: the relaunch still fires.
        _windowService.Received(1).RelaunchForLanguageChange();
    }
}
