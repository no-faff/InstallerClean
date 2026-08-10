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

    // Every specification of MoveFilesAsync / DeleteFilesAsync below names the
    // trailing patchClaims argument, assertions included. It is optional on the
    // interface and never omitted in practice, ReverifyResult defaulting
    // SurvivingPatchClaims to an empty array, so a specification that leaves it
    // out matches a literal null that no real call can carry: a Received() fails
    // against a call that did happen, and a DidNotReceive() passes whatever did.
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
    private readonly IUpdateCheckService _updateCheckService = Substitute.For<IUpdateCheckService>();
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
        // Check() returns Clean or Block, never null (the interface contract);
        // default it Clean so the scan and the act-time re-check both proceed.
        // Tests covering the gate override with a Clean-then-Block sequence.
        _rebootService.Check().Returns(PendingRebootResult.Clean);
        // Default the act-time re-verify to a no-op: every candidate survives and
        // nothing is dropped, so a Move/Delete acts on the full scanned set.
        // Tests covering the re-verify override this to drop entries or to throw.
        _reverifier.ReverifyAsync(Arg.Any<IReadOnlyList<string>>(), Arg.Any<CancellationToken>())
            .Returns(ci => new ReverifyResult((IReadOnlyList<string>)ci[0]!, Array.Empty<string>()));

        return new MainViewModel(
            _scanService, _moveService, _deleteService,
            _settingsService, _rebootService, _msiInfoService,
            _dialogService, _confirmationService, _windowService,
            _fileSystem, _resultLogService, _updateCheckService, _reverifier);
    }

    /// <summary>
    /// A per-machine claim on one cached patch. The codes are arbitrary and the
    /// path is not: it is what a consumer matches a claim to a file on.
    /// </summary>
    private static PatchClaim Claim(string path) =>
        new(path, "{AAAA0000-0000-0000-0000-000000000001}",
            "{1111FFFF-0000-0000-0000-000000000001}", null, 4);

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
            .ThrowsAsync(new LocalisedInvalidOperationException(Strings.Error_InstallerDbEmpty));

        await vm.Scan.ScanCommand.ExecuteAsync(null);

        // Equality against the resx, not a fragment of it: the ladder passes
        // ex.Message through untouched and titles it, and a fragment match kept
        // a copy of the English in the test that went stale the moment the
        // string was reworded.
        _dialogService.Received(1).ShowError(
            Strings.Error_InstallerDbEmpty, Strings.Error_InstallerDbUnavailableTitle);
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
        // window. Without the flush on Dispose the destination goes down with
        // the pending debounced save, and the box is empty next session.
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

        // Asserted synchronously after the command's first await, so the scan is
        // provably still inside its 200 ms overlay-reveal delay: IsScanning is
        // false throughout it. Gating the buttons on IsScanning therefore leaves
        // a window in which a Delete can start against the previous scan's
        // result while this one walks the same folder, which is why
        // IsScanInFlight exists and is what the buttons read.
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
        // IsScanning alone cannot make the Scan command's CanExecute see it.
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
                Arg.Any<IProgress<OperationProgress>?>(), Arg.Any<CancellationToken>(),
                Arg.Any<IReadOnlyList<PatchClaim>?>())
            .Returns(new MoveResult(2, Array.Empty<FileOperationError>()));
        _confirmationService.ConfirmMove(
            Arg.Any<int>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<bool>()).Returns(true);

        await vm.Scan.ScanWithProgressAsync(null);
        vm.Cleanup.MoveDestination = Path.Combine(Path.GetTempPath(), "ic-test-move");

        await vm.Cleanup.MoveAllCommand.ExecuteAsync(null);

        _confirmationService.Received(1).ConfirmMove(2, Arg.Any<string>(), vm.Cleanup.MoveDestination, Arg.Any<bool>());
        await _moveService.Received(1).MoveFilesAsync(
            Arg.Any<IEnumerable<string>>(), vm.Cleanup.MoveDestination,
            Arg.Any<IProgress<OperationProgress>?>(), Arg.Any<CancellationToken>(),
            Arg.Any<IReadOnlyList<PatchClaim>?>());
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
            Arg.Any<IProgress<OperationProgress>?>(), Arg.Any<CancellationToken>(),
            Arg.Any<IReadOnlyList<PatchClaim>?>());
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
                Arg.Any<IEnumerable<string>>(),
                Arg.Any<IProgress<OperationProgress>?>(), Arg.Any<CancellationToken>(),
                Arg.Any<IReadOnlyList<PatchClaim>?>())
            .Returns(new DeleteResult(1, Array.Empty<FileOperationError>()));
        _confirmationService.ConfirmDelete(
            Arg.Any<int>(), Arg.Any<string>()).Returns(true);

        await vm.Scan.ScanWithProgressAsync(null);

        await vm.Cleanup.DeleteAllCommand.ExecuteAsync(null);

        _confirmationService.Received(1).ConfirmDelete(1, Arg.Any<string>());
        await _deleteService.Received(1).DeleteFilesAsync(
            Arg.Any<IEnumerable<string>>(),
            Arg.Any<IProgress<OperationProgress>?>(), Arg.Any<CancellationToken>(),
            Arg.Any<IReadOnlyList<PatchClaim>?>());
        Assert.True(vm.Completion.IsComplete);
        Assert.Contains("permanently deleted", vm.Completion.Summary);
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
                Arg.Any<IProgress<OperationProgress>?>(), Arg.Any<CancellationToken>(),
                Arg.Any<IReadOnlyList<PatchClaim>?>())
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

    // The pair below covers what a crash leaves behind rather than what it
    // reports, on each action path. They are written to the same shape because
    // the two arms answer the same question and had drifted apart on it once.

    [Fact]
    public async Task MoveAllAsync_crash_rescans_and_stops_describing_the_batch_it_lost()
    {
        var vm = CreateViewModel();
        _scanService.ScanAsync(Arg.Any<IProgress<ScanProgressUpdate>?>(), Arg.Any<CancellationToken>())
            .Returns(ScanResultWithOrphans(3));
        _moveService.MoveFilesAsync(
                Arg.Any<IEnumerable<string>>(), Arg.Any<string>(),
                Arg.Any<IProgress<OperationProgress>?>(), Arg.Any<CancellationToken>(),
                Arg.Any<IReadOnlyList<PatchClaim>?>())
            .Returns<MoveResult>(_ =>
            {
                // The overlay as the crash finds it: a batch part-way through,
                // with a filename and a part-filled bar on screen. Written onto
                // the properties rather than through the progress reporter, for
                // the reason the cancel case records: Progress<T> posts its
                // callback, and with no dispatcher that lands on the thread pool.
                vm.Cleanup.OperationCurrentFile = 1;
                vm.Cleanup.OperationTotalFiles = 3;
                vm.Cleanup.OperationCurrentFileName = "orphan0.msi";
                vm.Cleanup.OperationProgressPercent = 33;
                throw new IOException("boom");
            });
        _confirmationService.ConfirmMove(
            Arg.Any<int>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<bool>()).Returns(true);

        await vm.Scan.ScanWithProgressAsync(null);
        Assert.Equal(3, vm.Scan.OrphanedFileCount);
        vm.Cleanup.MoveDestination = Path.Combine(Path.GetTempPath(), "ic-test-move-crash-rescan");

        // Sampled from inside the rescan, the only moment any of it is on
        // screen: the caller's finally clears it again, and runs afterwards.
        bool indeterminate = false;
        string headingDuringRescan = string.Empty, fileNameDuringRescan = "not sampled";
        int totalDuringRescan = -1;
        _scanService.ScanAsync(Arg.Any<IProgress<ScanProgressUpdate>?>(), Arg.Any<CancellationToken>())
            .Returns(_ =>
            {
                indeterminate = vm.Cleanup.IsOperationProgressIndeterminate;
                headingDuringRescan = vm.Cleanup.OperationProgress;
                fileNameDuringRescan = vm.Cleanup.OperationCurrentFileName;
                totalDuringRescan = vm.Cleanup.OperationTotalFiles;
                return ScanResultWithOrphans(1);
            });

        await vm.Cleanup.MoveAllCommand.ExecuteAsync(null);

        // How far the batch got is unknown, so the counts it left cannot stand:
        // they may be offering files that are already at the destination.
        Assert.Equal(1, vm.Scan.OrphanedFileCount);
        await _scanService.Received(2).ScanAsync(
            Arg.Any<IProgress<ScanProgressUpdate>?>(), Arg.Any<CancellationToken>());
        // And the card stops describing the batch that died. The heading says
        // scanning, so the bar runs indeterminate and the count row and filename
        // come off rather than freezing at "1 of 3" for the length of the walk.
        Assert.Equal(Strings.Status_Scanning, headingDuringRescan);
        Assert.True(indeterminate);
        Assert.Equal(string.Empty, fileNameDuringRescan);
        Assert.Equal(0, totalDuringRescan);
        Assert.False(vm.Cleanup.IsOperationProgressIndeterminate);
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
                Arg.Any<IEnumerable<string>>(),
                Arg.Any<IProgress<OperationProgress>?>(), Arg.Any<CancellationToken>(),
                Arg.Any<IReadOnlyList<PatchClaim>?>())
            .ThrowsAsync(new IOException("boom"));
        _confirmationService.ConfirmDelete(
            Arg.Any<int>(), Arg.Any<string>()).Returns(true);

        await vm.Scan.ScanWithProgressAsync(null);

        await vm.Cleanup.DeleteAllCommand.ExecuteAsync(null);

        _dialogService.Received(1).ShowWarning(Arg.Any<string>(), Strings.Error_DeleteFailedTitle);
        Assert.Equal(string.Empty, vm.Cleanup.OperationProgress);
    }

    [Fact]
    public async Task DeleteAllAsync_crash_rescans_and_stops_describing_the_batch_it_lost()
    {
        var vm = CreateViewModel();
        _scanService.ScanAsync(Arg.Any<IProgress<ScanProgressUpdate>?>(), Arg.Any<CancellationToken>())
            .Returns(ScanResultWithOrphans(3));
        _deleteService.DeleteFilesAsync(
                Arg.Any<IEnumerable<string>>(),
                Arg.Any<IProgress<OperationProgress>?>(), Arg.Any<CancellationToken>(),
                Arg.Any<IReadOnlyList<PatchClaim>?>())
            .Returns<DeleteResult>(_ =>
            {
                vm.Cleanup.OperationCurrentFile = 1;
                vm.Cleanup.OperationTotalFiles = 3;
                vm.Cleanup.OperationCurrentFileName = "orphan0.msi";
                vm.Cleanup.OperationProgressPercent = 33;
                throw new IOException("boom");
            });
        _confirmationService.ConfirmDelete(Arg.Any<int>(), Arg.Any<string>()).Returns(true);

        await vm.Scan.ScanWithProgressAsync(null);
        Assert.Equal(3, vm.Scan.OrphanedFileCount);

        bool indeterminate = false;
        string headingDuringRescan = string.Empty, fileNameDuringRescan = "not sampled";
        int totalDuringRescan = -1;
        _scanService.ScanAsync(Arg.Any<IProgress<ScanProgressUpdate>?>(), Arg.Any<CancellationToken>())
            .Returns(_ =>
            {
                indeterminate = vm.Cleanup.IsOperationProgressIndeterminate;
                headingDuringRescan = vm.Cleanup.OperationProgress;
                fileNameDuringRescan = vm.Cleanup.OperationCurrentFileName;
                totalDuringRescan = vm.Cleanup.OperationTotalFiles;
                return ScanResultWithOrphans(1);
            });

        await vm.Cleanup.DeleteAllCommand.ExecuteAsync(null);

        // The delete twin of the move case, and the stakes are the reason the
        // pair exists: these files went permanently, so counts left standing are
        // the window offering to delete files that have already gone.
        Assert.Equal(1, vm.Scan.OrphanedFileCount);
        await _scanService.Received(2).ScanAsync(
            Arg.Any<IProgress<ScanProgressUpdate>?>(), Arg.Any<CancellationToken>());
        Assert.Equal(Strings.Status_Scanning, headingDuringRescan);
        Assert.True(indeterminate);
        Assert.Equal(string.Empty, fileNameDuringRescan);
        Assert.Equal(0, totalDuringRescan);
        // The rescan hands the screen back to the caller's own clear, so the
        // heading it set is not the one left showing.
        Assert.Equal(string.Empty, vm.Cleanup.OperationProgress);
        Assert.False(vm.Cleanup.IsOperationProgressIndeterminate);
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
            Arg.Any<IEnumerable<string>>(),
            Arg.Any<IProgress<OperationProgress>?>(), Arg.Any<CancellationToken>(),
            Arg.Any<IReadOnlyList<PatchClaim>?>());
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
        // The service returns its partial result with Cancelled set rather than
        // throwing the tally away: two moved before the stop.
        _moveService.MoveFilesAsync(
                Arg.Any<IEnumerable<string>>(), Arg.Any<string>(),
                Arg.Any<IProgress<OperationProgress>?>(), Arg.Any<CancellationToken>(),
                Arg.Any<IReadOnlyList<PatchClaim>?>())
            .Returns(new MoveResult(2, Array.Empty<FileOperationError>(), Cancelled: true));
        _confirmationService.ConfirmMove(
            Arg.Any<int>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<bool>()).Returns(true);

        await vm.Scan.ScanWithProgressAsync(null);
        vm.Cleanup.MoveDestination = Path.Combine(Path.GetTempPath(), "ic-test-move-cancel");

        await vm.Cleanup.MoveAllCommand.ExecuteAsync(null);

        // The overlay carries "Moved 2 of 3 files before you cancelled.", so a
        // cancel reports what it managed rather than leaving an empty status
        // line.
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
                Arg.Any<IEnumerable<string>>(),
                Arg.Any<IProgress<OperationProgress>?>(), Arg.Any<CancellationToken>(),
                Arg.Any<IReadOnlyList<PatchClaim>?>())
            .Returns(new DeleteResult(1, Array.Empty<FileOperationError>(), Cancelled: true));
        _confirmationService.ConfirmDelete(
            Arg.Any<int>(), Arg.Any<string>()).Returns(true);

        await vm.Scan.ScanWithProgressAsync(null);

        await vm.Cleanup.DeleteAllCommand.ExecuteAsync(null);

        Assert.True(vm.Completion.IsComplete);
        Assert.Contains("1 of 2", vm.Completion.Summary);
        // The one file the cancel did reach is gone, so the summary says so
        // rather than describing the batch it was given.
        Assert.Contains("Permanently deleted", vm.Completion.Summary);
        await _resultLogService.DidNotReceive().WriteAsync(
            Arg.Any<ResultLogEntry>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public void MoveButtonTooltip_answers_from_the_box_and_changes_as_it_is_typed_in()
    {
        var vm = CreateViewModel();
        var systemRoot = Path.GetPathRoot(
            Environment.GetFolderPath(Environment.SpecialFolder.System))!;

        // Empty box: the browser opens first, so the tooltip warns of it rather
        // than naming a step that is missing.
        Assert.Equal(Strings.Tooltip_MoveNeedsDestination, vm.Cleanup.MoveButtonTooltip);

        // A folder on the drive the files are already on: a rename, so it says
        // when the space actually comes back. This is the state that pairs the
        // button with Delete beside it.
        vm.Cleanup.MoveDestination = Path.Combine(systemRoot, "ic-test-backup");
        Assert.Equal(Strings.Tooltip_MoveSameDrive, vm.Cleanup.MoveButtonTooltip);

        // Anywhere else, including a share, takes the plain wording.
        vm.Cleanup.MoveDestination = @"\\server\backup";
        Assert.Equal(Strings.Tooltip_Move, vm.Cleanup.MoveButtonTooltip);

        // Back to empty, because the box is a TextBox and a user can clear it.
        vm.Cleanup.MoveDestination = string.Empty;
        Assert.Equal(Strings.Tooltip_MoveNeedsDestination, vm.Cleanup.MoveButtonTooltip);
    }

    [Fact]
    public async Task DeleteAllAsync_cancelled_rearms_cancel_and_stops_describing_the_batch_it_stopped()
    {
        var vm = CreateViewModel();
        _scanService.ScanAsync(Arg.Any<IProgress<ScanProgressUpdate>?>(), Arg.Any<CancellationToken>())
            .Returns(ScanResultWithOrphans(3));
        _deleteService.DeleteFilesAsync(
                Arg.Any<IEnumerable<string>>(),
                Arg.Any<IProgress<OperationProgress>?>(), Arg.Any<CancellationToken>(),
                Arg.Any<IReadOnlyList<PatchClaim>?>())
            .Returns(_ =>
            {
                // The overlay as the user left it: a batch stopped at file 1 of
                // 3, with a filename and a part-filled bar on screen. Written
                // straight onto the properties rather than through the progress
                // reporter the view-model hands the service: Progress<T> POSTS
                // its callback, and with no dispatcher under a test that lands
                // on the thread pool and races everything below.
                vm.Cleanup.OperationCurrentFile = 1;
                vm.Cleanup.OperationTotalFiles = 3;
                vm.Cleanup.OperationCurrentFileName = "orphan0.msi";
                vm.Cleanup.OperationProgressPercent = 33;
                vm.Cleanup.CancelOperationCommand.Execute(null);
                return new DeleteResult(1, Array.Empty<FileOperationError>(), Cancelled: true);
            });
        _confirmationService.ConfirmDelete(Arg.Any<int>(), Arg.Any<string>()).Returns(true);

        await vm.Scan.ScanWithProgressAsync(null);

        // Sampled from inside the post-cancel rescan, which is the only moment
        // any of this is on screen: the caller's finally clears it all again,
        // and it runs after the rescan has finished.
        bool cancelLive = false, indeterminate = false, detailShown = true;
        string headingDuringRefresh = string.Empty, fileNameDuringRefresh = "not sampled";
        int totalDuringRefresh = -1;
        _scanService.ScanAsync(Arg.Any<IProgress<ScanProgressUpdate>?>(), Arg.Any<CancellationToken>())
            .Returns(ci =>
            {
                cancelLive = vm.Cleanup.CancelOperationCommand.CanExecute(null);
                indeterminate = vm.Cleanup.IsOperationProgressIndeterminate;
                detailShown = vm.Cleanup.ShowOperationProgressDetail;
                headingDuringRefresh = vm.Cleanup.OperationProgress;
                fileNameDuringRefresh = vm.Cleanup.OperationCurrentFileName;
                totalDuringRefresh = vm.Cleanup.OperationTotalFiles;
                return ScanResultWithOrphans(2);
            });

        await vm.Cleanup.DeleteAllCommand.ExecuteAsync(null);

        // The button is live again, which is the whole of the fix it belongs to:
        // the rescan is a full folder walk plus a full API enumeration, and held
        // behind a dead button it is the shape people report as a hang.
        Assert.True(cancelLive);
        // And the card no longer describes the batch that stopped. The heading
        // says scanning, so the bar runs indeterminate and the count row and
        // filename come off rather than freezing at "1 of 3".
        Assert.Equal(Strings.Status_Scanning, headingDuringRefresh);
        Assert.True(indeterminate);
        Assert.False(detailShown);
        Assert.Equal(string.Empty, fileNameDuringRefresh);
        Assert.Equal(0, totalDuringRefresh);
        // Back to a measured bar for the next operation.
        Assert.False(vm.Cleanup.IsOperationProgressIndeterminate);
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
            Arg.Any<IProgress<OperationProgress>?>(), Arg.Any<CancellationToken>(),
            Arg.Any<IReadOnlyList<PatchClaim>?>());
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
            Arg.Any<IEnumerable<string>>(),
            Arg.Any<IProgress<OperationProgress>?>(), Arg.Any<CancellationToken>(),
            Arg.Any<IReadOnlyList<PatchClaim>?>());
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
                Arg.Any<IProgress<OperationProgress>?>(), Arg.Any<CancellationToken>(),
                Arg.Any<IReadOnlyList<PatchClaim>?>())
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
    public async Task MoveAllAsync_lock_unavailable_result_shows_the_dialog_and_leaves_the_gate_alone()
    {
        // The twin of the installer-busy test above, and of the delete path's own
        // pair, which is why this is a second flag rather than a second cause
        // behind the first. Busy re-runs the pending-reboot gate, which meets the
        // held mutex and paints its banner. This one must NOT, because the gate
        // can account for the condition neither way: clean paints nothing and
        // leaves the user refused with no reason on screen, and held asserts an
        // install nothing has shown. The dialog carries it instead,
        // and the title is pinned as well as the body because the Move copy is a
        // separate pair from the Delete copy and either could be wired to the
        // other's.
        var vm = CreateViewModel();
        _scanService.ScanAsync(Arg.Any<IProgress<ScanProgressUpdate>?>(), Arg.Any<CancellationToken>())
            .Returns(ScanResultWithOrphans(2));
        _moveService.MoveFilesAsync(
                Arg.Any<IEnumerable<string>>(), Arg.Any<string>(),
                Arg.Any<IProgress<OperationProgress>?>(), Arg.Any<CancellationToken>(),
                Arg.Any<IReadOnlyList<PatchClaim>?>())
            .Returns(new MoveResult(0, Array.Empty<FileOperationError>(),
                InstallerLockUnavailable: true));
        _confirmationService.ConfirmMove(
            Arg.Any<int>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<bool>()).Returns(true);

        await vm.Scan.ScanWithProgressAsync(null);
        vm.Cleanup.MoveDestination = Path.Combine(Path.GetTempPath(), "ic-test-lock-unavailable");

        await vm.Cleanup.MoveAllCommand.ExecuteAsync(null);

        _dialogService.Received(1).ShowWarning(
            Strings.Error_MoveInstallerLockUnavailable,
            Strings.Error_MoveInstallerLockUnavailableTitle);
        // Twice and no more: once for the scan, once for the act-time gate. A
        // third would be the busy arm's re-check, which this arm must not run.
        _rebootService.Received(2).Check();
        Assert.False(vm.Scan.HasPendingReboot);
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
                new[] { @"C:\Windows\Installer\b.msi" },
                new HeldBackReasons(Reclaimed: 1)));
        _moveService.MoveFilesAsync(
                Arg.Any<IEnumerable<string>>(), Arg.Any<string>(),
                Arg.Any<IProgress<OperationProgress>?>(), Arg.Any<CancellationToken>(),
                Arg.Any<IReadOnlyList<PatchClaim>?>())
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
            Arg.Any<string>(), Arg.Any<IProgress<OperationProgress>?>(), Arg.Any<CancellationToken>(),
            Arg.Any<IReadOnlyList<PatchClaim>?>());
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
            .ThrowsAsync(new LocalisedInvalidOperationException(Strings.Error_InstallerDbEmpty));
        _confirmationService.ConfirmMove(
            Arg.Any<int>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<bool>()).Returns(true);

        await vm.Scan.ScanWithProgressAsync(null);
        vm.Cleanup.MoveDestination = Path.Combine(Path.GetTempPath(), "ic-test-reverify-throw");

        await vm.Cleanup.MoveAllCommand.ExecuteAsync(null);

        // Never act on an un-verified batch: the move service is not called.
        await _moveService.DidNotReceive().MoveFilesAsync(
            Arg.Any<IEnumerable<string>>(), Arg.Any<string>(),
            Arg.Any<IProgress<OperationProgress>?>(), Arg.Any<CancellationToken>(),
            Arg.Any<IReadOnlyList<PatchClaim>?>());
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
                new[] { @"C:\Windows\Installer\a.msi", @"C:\Windows\Installer\b.msi" },
                new HeldBackReasons(Reclaimed: 2)));
        _confirmationService.ConfirmMove(
            Arg.Any<int>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<bool>()).Returns(true);

        await vm.Scan.ScanWithProgressAsync(null);
        vm.Cleanup.MoveDestination = Path.Combine(Path.GetTempPath(), "ic-test-reverify-all");

        await vm.Cleanup.MoveAllCommand.ExecuteAsync(null);

        await _moveService.DidNotReceive().MoveFilesAsync(
            Arg.Any<IEnumerable<string>>(), Arg.Any<string>(),
            Arg.Any<IProgress<OperationProgress>?>(), Arg.Any<CancellationToken>(),
            Arg.Any<IReadOnlyList<PatchClaim>?>());
        Assert.True(vm.Completion.IsComplete);
        Assert.Contains("2", vm.Completion.Summary);
    }

    [Fact]
    public async Task MoveAllAsync_a_degraded_reverify_reports_the_unread_records_not_a_reclaim()
    {
        // A re-verify that could not read the records keeps files back without any
        // program having reclaimed them. Reporting the reclaim reason there would
        // state a specific cause that did not happen, which is a false statement
        // about the user's machine rather than a wording preference.
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
                new HeldBackReasons(RecordsUnreadable: 1)));
        _confirmationService.ConfirmMove(
            Arg.Any<int>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<bool>()).Returns(true);

        await vm.Scan.ScanWithProgressAsync(null);
        vm.Cleanup.MoveDestination = Path.Combine(Path.GetTempPath(), "ic-test-reverify-degraded");

        await vm.Cleanup.MoveAllCommand.ExecuteAsync(null);

        await _moveService.DidNotReceive().MoveFilesAsync(
            Arg.Any<IEnumerable<string>>(), Arg.Any<string>(),
            Arg.Any<IProgress<OperationProgress>?>(), Arg.Any<CancellationToken>(),
            Arg.Any<IReadOnlyList<PatchClaim>?>());
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
                new[] { @"C:\Windows\Installer\a.msp" },
                new HeldBackReasons(Reclaimed: 1)));
        _moveService.MoveFilesAsync(
                Arg.Any<IEnumerable<string>>(), Arg.Any<string>(),
                Arg.Any<IProgress<OperationProgress>?>(), Arg.Any<CancellationToken>(),
                Arg.Any<IReadOnlyList<PatchClaim>?>())
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
                new[] { @"C:\Windows\Installer\y.msi" },
                new HeldBackReasons(Reclaimed: 1)));
        _deleteService.DeleteFilesAsync(
                Arg.Any<IEnumerable<string>>(),
                Arg.Any<IProgress<OperationProgress>?>(), Arg.Any<CancellationToken>(),
                Arg.Any<IReadOnlyList<PatchClaim>?>())
            .Returns(new DeleteResult(1, Array.Empty<FileOperationError>()));
        _confirmationService.ConfirmDelete(Arg.Any<int>(), Arg.Any<string>()).Returns(true);

        await vm.Scan.ScanWithProgressAsync(null);

        await vm.Cleanup.DeleteAllCommand.ExecuteAsync(null);

        await _deleteService.Received(1).DeleteFilesAsync(
            Arg.Is<IEnumerable<string>>(paths =>
                paths != null && paths.Count() == 1 && paths.Contains(@"C:\Windows\Installer\x.msi")),
            Arg.Any<IProgress<OperationProgress>?>(), Arg.Any<CancellationToken>(),
            Arg.Any<IReadOnlyList<PatchClaim>?>());
        Assert.True(vm.Completion.IsComplete);
        Assert.Contains("1", vm.Completion.Skipped);
    }

    // The two below pin the wire between the pre-act re-verify and the action
    // services. Each half is covered elsewhere and neither covers the join:
    // RemovableReverifierTests pins that the re-verify produces the claims, and
    // each service's own tests pin that it hands what it was given to the
    // under-lease re-read untouched. The claim a service is never given is the
    // one it can never re-ask about, and it would be silently absent: an empty
    // list is the ordinary case, since a true orphan carries no claim.

    [Fact]
    public async Task MoveAllAsync_hands_the_re_verifys_claims_to_the_service()
    {
        var vm = CreateViewModel();
        var orphans = new List<OrphanedFile>
        {
            new(@"C:\Windows\Installer\a.msp", 1_048_576, true, true, false, Superseded),
        };
        _scanService.ScanAsync(Arg.Any<IProgress<ScanProgressUpdate>?>(), Arg.Any<CancellationToken>())
            .Returns(new ScanResult(orphans, Array.Empty<RegisteredPackage>(), 0));
        var claims = new[] { Claim(@"C:\Windows\Installer\a.msp") };
        _reverifier.ReverifyAsync(Arg.Any<IReadOnlyList<string>>(), Arg.Any<CancellationToken>())
            .Returns(new ReverifyResult(
                new[] { @"C:\Windows\Installer\a.msp" }, Array.Empty<string>(),
                SurvivingPatchClaims: claims));
        _moveService.MoveFilesAsync(
                Arg.Any<IEnumerable<string>>(), Arg.Any<string>(),
                Arg.Any<IProgress<OperationProgress>?>(), Arg.Any<CancellationToken>(),
                Arg.Any<IReadOnlyList<PatchClaim>?>())
            .Returns(new MoveResult(1, Array.Empty<FileOperationError>()));
        _confirmationService.ConfirmMove(
            Arg.Any<int>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<bool>()).Returns(true);

        await vm.Scan.ScanWithProgressAsync(null);
        vm.Cleanup.MoveDestination = Path.Combine(Path.GetTempPath(), "ic-test-claims-move");

        await vm.Cleanup.MoveAllCommand.ExecuteAsync(null);

        await _moveService.Received(1).MoveFilesAsync(
            Arg.Any<IEnumerable<string>>(), Arg.Any<string>(),
            Arg.Any<IProgress<OperationProgress>?>(), Arg.Any<CancellationToken>(),
            Arg.Is<IReadOnlyList<PatchClaim>?>(c => c != null && c.SequenceEqual(claims)));
    }

    [Fact]
    public async Task DeleteAllAsync_hands_the_re_verifys_claims_to_the_service()
    {
        var vm = CreateViewModel();
        var orphans = new List<OrphanedFile>
        {
            new(@"C:\Windows\Installer\x.msp", 524_288, true, true, false, Superseded),
        };
        _scanService.ScanAsync(Arg.Any<IProgress<ScanProgressUpdate>?>(), Arg.Any<CancellationToken>())
            .Returns(new ScanResult(orphans, Array.Empty<RegisteredPackage>(), 0));
        var claims = new[] { Claim(@"C:\Windows\Installer\x.msp") };
        _reverifier.ReverifyAsync(Arg.Any<IReadOnlyList<string>>(), Arg.Any<CancellationToken>())
            .Returns(new ReverifyResult(
                new[] { @"C:\Windows\Installer\x.msp" }, Array.Empty<string>(),
                SurvivingPatchClaims: claims));
        _deleteService.DeleteFilesAsync(
                Arg.Any<IEnumerable<string>>(),
                Arg.Any<IProgress<OperationProgress>?>(), Arg.Any<CancellationToken>(),
                Arg.Any<IReadOnlyList<PatchClaim>?>())
            .Returns(new DeleteResult(1, Array.Empty<FileOperationError>()));
        _confirmationService.ConfirmDelete(Arg.Any<int>(), Arg.Any<string>()).Returns(true);

        await vm.Scan.ScanWithProgressAsync(null);

        await vm.Cleanup.DeleteAllCommand.ExecuteAsync(null);

        await _deleteService.Received(1).DeleteFilesAsync(
            Arg.Any<IEnumerable<string>>(),
            Arg.Any<IProgress<OperationProgress>?>(), Arg.Any<CancellationToken>(),
            Arg.Is<IReadOnlyList<PatchClaim>?>(c => c != null && c.SequenceEqual(claims)));
    }

    // The block below pins the other direction of that wire: what the window does
    // with the paths an action service hands BACK. The production of both signals
    // was tested and the consumption of neither, and three defects landed inside
    // that gap. Every one of these stages a result with HeldBack non-empty, which
    // is the shape a batch takes when the re-read under the installer mutex found
    // a claim had moved.

    [Fact]
    public async Task DeleteAllAsync_a_path_held_back_leaves_the_batchs_account_of_itself()
    {
        // The ordinary fold. The held-back file was never touched, so it leaves
        // the count, the byte total and the freed figure that reaches the result
        // log, and joins the kept-back tally instead.
        var vm = CreateViewModel();
        var orphans = new List<OrphanedFile>
        {
            new(@"C:\Windows\Installer\a.msi", 1_048_576, false, false, false, Orphaned),
            new(@"C:\Windows\Installer\b.msi", 2_097_152, false, false, false, Orphaned),
            new(@"C:\Windows\Installer\c.msp", 4_194_304, true, true, false, Superseded),
        };
        _scanService.ScanAsync(Arg.Any<IProgress<ScanProgressUpdate>?>(), Arg.Any<CancellationToken>())
            .Returns(new ScanResult(orphans, Array.Empty<RegisteredPackage>(), 0));
        _deleteService.DeleteFilesAsync(
                Arg.Any<IEnumerable<string>>(),
                Arg.Any<IProgress<OperationProgress>?>(), Arg.Any<CancellationToken>(),
                Arg.Any<IReadOnlyList<PatchClaim>?>())
            .Returns(new DeleteResult(2, Array.Empty<FileOperationError>(),
                HeldBack: new[] { @"C:\Windows\Installer\c.msp" },
                HeldBackReasons: new HeldBackReasons(Reclaimed: 1)));
        _confirmationService.ConfirmDelete(Arg.Any<int>(), Arg.Any<string>()).Returns(true);

        await vm.Scan.ScanWithProgressAsync(null);

        await vm.Cleanup.DeleteAllCommand.ExecuteAsync(null);

        Assert.True(vm.Completion.IsComplete);
        // The pre-act re-verify dropped nothing, so the one kept-back file on
        // screen is the service's, folded into the same tally and the same
        // sentence.
        Assert.Equal(
            string.Format(Strings.Completion_ReverifySkipped, 1, DisplayHelpers.PluraliseFile(1)),
            vm.Completion.Skipped);
        // 3.0 MB, not 7.0: the held-back file's 4 MB is not freed space.
        Assert.Equal(
            string.Format(Strings.Completion_Freed, DisplayHelpers.FormatSize(3_145_728)),
            vm.Completion.Heading);
        await _resultLogService.Received(1).WriteAsync(
            Arg.Is<ResultLogEntry>(e => e != null && e.Operation.BytesFreed == 3_145_728),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task DeleteAllAsync_a_batch_the_service_empties_reports_all_clean_and_logs_nothing()
    {
        // One condition, one screen, one record. The re-read taking the whole
        // batch back is the same machine state as the pre-act re-verify taking it
        // back, so it gets that arm's screen: the all-clean heading with the
        // kept-back sentence as its summary, and no result-log entry. Falling
        // through instead reported a completed delete of zero and wrote a run that
        // freed nothing into the public reports.
        var vm = CreateViewModel();
        var orphans = new List<OrphanedFile>
        {
            new(@"C:\Windows\Installer\a.msp", 1_048_576, true, true, false, Superseded),
        };
        _scanService.ScanAsync(Arg.Any<IProgress<ScanProgressUpdate>?>(), Arg.Any<CancellationToken>())
            .Returns(new ScanResult(orphans, Array.Empty<RegisteredPackage>(), 0));
        _deleteService.DeleteFilesAsync(
                Arg.Any<IEnumerable<string>>(),
                Arg.Any<IProgress<OperationProgress>?>(), Arg.Any<CancellationToken>(),
                Arg.Any<IReadOnlyList<PatchClaim>?>())
            .Returns(new DeleteResult(0, Array.Empty<FileOperationError>(),
                HeldBack: new[] { @"C:\Windows\Installer\a.msp" },
                HeldBackReasons: new HeldBackReasons(Reclaimed: 1)));
        _confirmationService.ConfirmDelete(Arg.Any<int>(), Arg.Any<string>()).Returns(true);

        await vm.Scan.ScanWithProgressAsync(null);

        await vm.Cleanup.DeleteAllCommand.ExecuteAsync(null);

        Assert.True(vm.Completion.IsComplete);
        Assert.Equal(Strings.Completion_AllClean, vm.Completion.Heading);
        Assert.Equal(
            string.Format(Strings.Completion_ReverifySkipped, 1, DisplayHelpers.PluraliseFile(1)),
            vm.Completion.Summary);
        await _resultLogService.DidNotReceive().WriteAsync(
            Arg.Any<ResultLogEntry>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task MoveAllAsync_a_batch_the_service_empties_reports_all_clean_and_logs_nothing()
    {
        // The Move twin, which had two more things wrong with it: the summary read
        // "0 files moved to: <folder>" and the restore line told the user how to
        // put back files that never left.
        var vm = CreateViewModel();
        var orphans = new List<OrphanedFile>
        {
            new(@"C:\Windows\Installer\a.msp", 1_048_576, true, true, false, Superseded),
        };
        _scanService.ScanAsync(Arg.Any<IProgress<ScanProgressUpdate>?>(), Arg.Any<CancellationToken>())
            .Returns(new ScanResult(orphans, Array.Empty<RegisteredPackage>(), 0));
        _moveService.MoveFilesAsync(
                Arg.Any<IEnumerable<string>>(), Arg.Any<string>(),
                Arg.Any<IProgress<OperationProgress>?>(), Arg.Any<CancellationToken>(),
                Arg.Any<IReadOnlyList<PatchClaim>?>())
            .Returns(new MoveResult(0, Array.Empty<FileOperationError>(),
                HeldBack: new[] { @"C:\Windows\Installer\a.msp" },
                HeldBackReasons: new HeldBackReasons(Reclaimed: 1)));
        _confirmationService.ConfirmMove(
            Arg.Any<int>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<bool>()).Returns(true);

        await vm.Scan.ScanWithProgressAsync(null);
        vm.Cleanup.MoveDestination = Path.Combine(Path.GetTempPath(), "ic-test-heldback-all-move");

        await vm.Cleanup.MoveAllCommand.ExecuteAsync(null);

        Assert.True(vm.Completion.IsComplete);
        Assert.Equal(Strings.Completion_AllClean, vm.Completion.Heading);
        Assert.Equal(string.Empty, vm.Completion.Restore);
        Assert.Equal(string.Empty, vm.Completion.SummaryDestination);
        await _resultLogService.DidNotReceive().WriteAsync(
            Arg.Any<ResultLogEntry>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task DeleteAllAsync_a_cancel_after_a_hold_back_still_reports_the_hold_back()
    {
        // Cancelled AND held back is the ordinary shape of that combination, not a
        // corner: the re-read runs before the loop and the cancel happens inside
        // it. The cancelled summary carries the kept-back tally, and the byte
        // figure is taken from the list the hold-back has already left.
        var vm = CreateViewModel();
        var orphans = new List<OrphanedFile>
        {
            new(@"C:\Windows\Installer\a.msi", 1_048_576, false, false, false, Orphaned),
            new(@"C:\Windows\Installer\b.msi", 2_097_152, false, false, false, Orphaned),
            new(@"C:\Windows\Installer\c.msp", 4_194_304, true, true, false, Superseded),
        };
        _scanService.ScanAsync(Arg.Any<IProgress<ScanProgressUpdate>?>(), Arg.Any<CancellationToken>())
            .Returns(new ScanResult(orphans, Array.Empty<RegisteredPackage>(), 0));
        _deleteService.DeleteFilesAsync(
                Arg.Any<IEnumerable<string>>(),
                Arg.Any<IProgress<OperationProgress>?>(), Arg.Any<CancellationToken>(),
                Arg.Any<IReadOnlyList<PatchClaim>?>())
            .Returns(new DeleteResult(1, Array.Empty<FileOperationError>(), Cancelled: true,
                HeldBack: new[] { @"C:\Windows\Installer\c.msp" },
                HeldBackReasons: new HeldBackReasons(Reclaimed: 1)));
        _confirmationService.ConfirmDelete(Arg.Any<int>(), Arg.Any<string>()).Returns(true);

        await vm.Scan.ScanWithProgressAsync(null);

        await vm.Cleanup.DeleteAllCommand.ExecuteAsync(null);

        Assert.True(vm.Completion.IsComplete);
        Assert.Equal(
            string.Format(Strings.Completion_ReverifySkipped, 1, DisplayHelpers.PluraliseFile(1)),
            vm.Completion.Skipped);
        // One file deleted out of the TWO the batch still had, and its 1 MB is the
        // freed figure. The held-back file is neither in the total nor in the sum.
        Assert.Equal(
            string.Format(Strings.Completion_Freed, DisplayHelpers.FormatSize(1_048_576)),
            vm.Completion.Heading);
        // A cancelled run writes no result-log entry whatever else it did.
        await _resultLogService.DidNotReceive().WriteAsync(
            Arg.Any<ResultLogEntry>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task MoveAllAsync_an_abort_after_a_hold_back_counts_only_what_the_batch_reached()
    {
        // The abort arm's own fold, on the one path where files have already left
        // the Installer folder. The byte figure is positional over the surviving
        // list, so a held-back file left in that list would have its bytes counted
        // as moved and push a file that really moved off the end.
        var vm = CreateViewModel();
        var orphans = new List<OrphanedFile>
        {
            new(@"C:\Windows\Installer\a.msp", 4_194_304, true, true, false, Superseded),
            new(@"C:\Windows\Installer\b.msi", 1_048_576, false, false, false, Orphaned),
            new(@"C:\Windows\Installer\c.msi", 2_097_152, false, false, false, Orphaned),
        };
        _scanService.ScanAsync(Arg.Any<IProgress<ScanProgressUpdate>?>(), Arg.Any<CancellationToken>())
            .Returns(new ScanResult(orphans, Array.Empty<RegisteredPackage>(), 0));
        _moveService.MoveFilesAsync(
                Arg.Any<IEnumerable<string>>(), Arg.Any<string>(),
                Arg.Any<IProgress<OperationProgress>?>(), Arg.Any<CancellationToken>(),
                Arg.Any<IReadOnlyList<PatchClaim>?>())
            .Returns<MoveResult>(_ => throw new MoveAbortedException(
                "swapped",
                new MoveResult(1, Array.Empty<FileOperationError>(),
                    HeldBack: new[] { @"C:\Windows\Installer\a.msp" },
                    HeldBackReasons: new HeldBackReasons(Reclaimed: 1)),
                @"E:\resolved-elsewhere", MoveAbortReason.ResolvesElsewhere));
        _confirmationService.ConfirmMove(
            Arg.Any<int>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<bool>()).Returns(true);

        await vm.Scan.ScanWithProgressAsync(null);
        vm.Cleanup.MoveDestination = Path.Combine(Path.GetTempPath(), "ic-test-heldback-abort");

        await vm.Cleanup.MoveAllCommand.ExecuteAsync(null);

        Assert.True(vm.Completion.IsComplete);
        Assert.Equal(
            string.Format(Strings.Completion_ReverifySkipped, 1, DisplayHelpers.PluraliseFile(1)),
            vm.Completion.Skipped);
        // b.msi's 1 MB, the first file of the folded list. Held back is a.msp at
        // 4 MB, which is first in the unfolded one, so a fold that did not happen
        // shows here as 4 MB.
        Assert.Contains(DisplayHelpers.FormatSize(1_048_576), vm.Completion.Heading);
        _dialogService.Received(1).ShowWarning(
            Arg.Any<string>(), Strings.Error_InvalidDestinationTitle);
    }

    [Fact]
    public async Task MoveAllAsync_an_abort_names_the_folder_the_files_are_in_not_the_one_asked_for()
    {
        // The one path in the app where those are different folders, and the
        // reason this exception exists: the destination the user typed no longer
        // resolves to where their files went. The summary line names the folder,
        // and the restore line beneath it says "the files in that folder", so a
        // summary naming the typed path sends somebody to an empty folder to look
        // for files they have just been told are there.
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
                Arg.Any<IProgress<OperationProgress>?>(), Arg.Any<CancellationToken>(),
                Arg.Any<IReadOnlyList<PatchClaim>?>())
            .Returns<MoveResult>(_ => throw new MoveAbortedException(
                "swapped", new MoveResult(1, Array.Empty<FileOperationError>()),
                @"E:\where-they-really-went", MoveAbortReason.ResolvesElsewhere));
        _confirmationService.ConfirmMove(
            Arg.Any<int>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<bool>()).Returns(true);

        await vm.Scan.ScanWithProgressAsync(null);
        var typed = Path.Combine(Path.GetTempPath(), "ic-test-abort-destination");
        vm.Cleanup.MoveDestination = typed;

        await vm.Cleanup.MoveAllCommand.ExecuteAsync(null);

        Assert.True(vm.Completion.IsComplete);
        Assert.Equal(@"E:\where-they-really-went", vm.Completion.SummaryDestination);
        Assert.Contains(@"E:\where-they-really-went", vm.Completion.Summary);
        Assert.DoesNotContain(typed, vm.Completion.Summary);
    }

    [Fact]
    public async Task MoveAllAsync_a_completed_move_still_names_the_destination_it_was_given()
    {
        // The other side of the pair, and the reason the fix is scoped to the
        // abort. On a batch that finished, the folder the user asked for is where
        // their files are, whatever it resolves through: a backup folder reached by
        // a junction is an ordinary setup, and naming the junction's target back at
        // somebody who has never seen it would be a worse answer, not a truer one.
        var vm = CreateViewModel();
        var orphans = new List<OrphanedFile>
        {
            new(@"C:\Windows\Installer\a.msi", 1_048_576, false, false, false, Orphaned),
        };
        _scanService.ScanAsync(Arg.Any<IProgress<ScanProgressUpdate>?>(), Arg.Any<CancellationToken>())
            .Returns(new ScanResult(orphans, Array.Empty<RegisteredPackage>(), 0));
        _moveService.MoveFilesAsync(
                Arg.Any<IEnumerable<string>>(), Arg.Any<string>(),
                Arg.Any<IProgress<OperationProgress>?>(), Arg.Any<CancellationToken>(),
                Arg.Any<IReadOnlyList<PatchClaim>?>())
            .Returns(new MoveResult(1, Array.Empty<FileOperationError>()));
        _confirmationService.ConfirmMove(
            Arg.Any<int>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<bool>()).Returns(true);

        await vm.Scan.ScanWithProgressAsync(null);
        var typed = Path.Combine(Path.GetTempPath(), "ic-test-completed-destination");
        vm.Cleanup.MoveDestination = typed;

        await vm.Cleanup.MoveAllCommand.ExecuteAsync(null);

        Assert.Equal(typed, vm.Completion.SummaryDestination);
    }

    [Fact]
    public async Task DeleteAllAsync_a_hold_back_the_records_could_not_read_names_that_reason()
    {
        // The tallies are added across the two halves of the check, because either
        // can fail to read a record and the causes have different copy. Here the
        // pre-act re-verify was healthy and the under-lease re-read was not, so the
        // only cause present is the one that must reach the screen.
        var vm = CreateViewModel();
        var orphans = new List<OrphanedFile>
        {
            new(@"C:\Windows\Installer\a.msi", 1_048_576, false, false, false, Orphaned),
            new(@"C:\Windows\Installer\b.msp", 2_097_152, true, true, false, Superseded),
        };
        _scanService.ScanAsync(Arg.Any<IProgress<ScanProgressUpdate>?>(), Arg.Any<CancellationToken>())
            .Returns(new ScanResult(orphans, Array.Empty<RegisteredPackage>(), 0));
        _deleteService.DeleteFilesAsync(
                Arg.Any<IEnumerable<string>>(),
                Arg.Any<IProgress<OperationProgress>?>(), Arg.Any<CancellationToken>(),
                Arg.Any<IReadOnlyList<PatchClaim>?>())
            .Returns(new DeleteResult(1, Array.Empty<FileOperationError>(),
                HeldBack: new[] { @"C:\Windows\Installer\b.msp" },
                HeldBackReasons: new HeldBackReasons(RecordsUnreadable: 1)));
        _confirmationService.ConfirmDelete(Arg.Any<int>(), Arg.Any<string>()).Returns(true);

        await vm.Scan.ScanWithProgressAsync(null);

        await vm.Cleanup.DeleteAllCommand.ExecuteAsync(null);

        Assert.Equal(
            string.Format(Strings.Completion_ReverifyIncomplete, 1, DisplayHelpers.PluraliseFile(1)),
            vm.Completion.Skipped);
    }

    [Fact]
    public async Task DeleteAllAsync_a_batch_held_back_two_ways_names_both_causes_with_their_own_counts()
    {
        // The state neither half of the check can describe on its own, and the one
        // the fold used to destroy: the pre-act re-verify kept a file back because
        // a program reclaimed it, and the under-lease re-read kept a DIFFERENT file
        // back because a read failed. Merging those into one cause put a sentence
        // over files it was false of, whichever cause won. Two files, two causes,
        // two lines, one count each.
        var vm = CreateViewModel();
        var orphans = new List<OrphanedFile>
        {
            new(@"C:\Windows\Installer\a.msi", 1_048_576, false, false, false, Orphaned),
            new(@"C:\Windows\Installer\b.msp", 2_097_152, true, true, false, Superseded),
            new(@"C:\Windows\Installer\c.msp", 4_194_304, true, true, false, Superseded),
        };
        _scanService.ScanAsync(Arg.Any<IProgress<ScanProgressUpdate>?>(), Arg.Any<CancellationToken>())
            .Returns(new ScanResult(orphans, Array.Empty<RegisteredPackage>(), 0));
        _reverifier.ReverifyAsync(Arg.Any<IReadOnlyList<string>>(), Arg.Any<CancellationToken>())
            .Returns(new ReverifyResult(
                new[] { @"C:\Windows\Installer\a.msi", @"C:\Windows\Installer\b.msp" },
                new[] { @"C:\Windows\Installer\c.msp" },
                new HeldBackReasons(Reclaimed: 1)));
        _deleteService.DeleteFilesAsync(
                Arg.Any<IEnumerable<string>>(),
                Arg.Any<IProgress<OperationProgress>?>(), Arg.Any<CancellationToken>(),
                Arg.Any<IReadOnlyList<PatchClaim>?>())
            .Returns(new DeleteResult(1, Array.Empty<FileOperationError>(),
                HeldBack: new[] { @"C:\Windows\Installer\b.msp" },
                HeldBackReasons: new HeldBackReasons(RecordsUnreadable: 1)));
        _confirmationService.ConfirmDelete(Arg.Any<int>(), Arg.Any<string>()).Returns(true);

        await vm.Scan.ScanWithProgressAsync(null);

        await vm.Cleanup.DeleteAllCommand.ExecuteAsync(null);

        Assert.Equal(
            string.Format(Strings.Completion_ReverifySkipped, 1, DisplayHelpers.PluraliseFile(1))
                + Environment.NewLine
                + string.Format(Strings.Completion_ReverifyIncomplete, 1, DisplayHelpers.PluraliseFile(1)),
            vm.Completion.Skipped);
        // And the counts stay each cause's own rather than the batch's: a line
        // reading "2 files" against either sentence would be the collapse in a
        // different disguise.
        Assert.DoesNotContain(
            string.Format(Strings.Completion_ReverifySkipped, 2, DisplayHelpers.PluraliseFile(2)),
            vm.Completion.Skipped);
    }

    [Fact]
    public async Task DeleteAllAsync_a_record_that_changed_says_so_rather_than_naming_a_reclaim()
    {
        // The third cause reaching the screen. The under-lease re-read found the
        // records no longer hold the registration the claim named, which is neither
        // a program taking the file back nor a read that failed, and until this
        // sentence existed the file was released to the delete instead.
        var vm = CreateViewModel();
        var orphans = new List<OrphanedFile>
        {
            new(@"C:\Windows\Installer\a.msi", 1_048_576, false, false, false, Orphaned),
            new(@"C:\Windows\Installer\b.msp", 2_097_152, true, true, false, Superseded),
        };
        _scanService.ScanAsync(Arg.Any<IProgress<ScanProgressUpdate>?>(), Arg.Any<CancellationToken>())
            .Returns(new ScanResult(orphans, Array.Empty<RegisteredPackage>(), 0));
        _deleteService.DeleteFilesAsync(
                Arg.Any<IEnumerable<string>>(),
                Arg.Any<IProgress<OperationProgress>?>(), Arg.Any<CancellationToken>(),
                Arg.Any<IReadOnlyList<PatchClaim>?>())
            .Returns(new DeleteResult(1, Array.Empty<FileOperationError>(),
                HeldBack: new[] { @"C:\Windows\Installer\b.msp" },
                HeldBackReasons: new HeldBackReasons(RecordsChanged: 1)));
        _confirmationService.ConfirmDelete(Arg.Any<int>(), Arg.Any<string>()).Returns(true);

        await vm.Scan.ScanWithProgressAsync(null);

        await vm.Cleanup.DeleteAllCommand.ExecuteAsync(null);

        Assert.Equal(
            string.Format(Strings.Completion_ReverifyRecordsChanged, 1, DisplayHelpers.PluraliseFile(1)),
            vm.Completion.Skipped);
    }

    [Fact]
    public async Task DeleteAllAsync_lock_unavailable_result_shows_the_dialog_and_leaves_the_gate_alone()
    {
        // The twin of the installer-busy test above, and the difference between
        // them is the whole reason this is a second flag rather than a second
        // cause behind the first. Busy re-runs the pending-reboot gate, which now
        // meets the held mutex and paints its banner. This one must NOT: nothing
        // holds the mutex, so the gate would come back clean and paint nothing,
        // leaving the user refused with no reason on screen. The dialog carries it
        // instead, and the title is pinned as well as the body because this arm
        // borrowed the crash arm's title once.
        var vm = CreateViewModel();
        _scanService.ScanAsync(Arg.Any<IProgress<ScanProgressUpdate>?>(), Arg.Any<CancellationToken>())
            .Returns(ScanResultWithOrphans(2));
        _deleteService.DeleteFilesAsync(
                Arg.Any<IEnumerable<string>>(),
                Arg.Any<IProgress<OperationProgress>?>(), Arg.Any<CancellationToken>(),
                Arg.Any<IReadOnlyList<PatchClaim>?>())
            .Returns(new DeleteResult(0, Array.Empty<FileOperationError>(),
                InstallerLockUnavailable: true));
        _confirmationService.ConfirmDelete(Arg.Any<int>(), Arg.Any<string>()).Returns(true);

        await vm.Scan.ScanWithProgressAsync(null);

        await vm.Cleanup.DeleteAllCommand.ExecuteAsync(null);

        _dialogService.Received(1).ShowWarning(
            Strings.Error_InstallerLockUnavailable, Strings.Error_InstallerLockUnavailableTitle);
        // Twice and no more: once for the scan, once for the act-time gate. A
        // third would be the busy arm's re-check, which this arm must not run.
        _rebootService.Received(2).Check();
        Assert.False(vm.Scan.HasPendingReboot);
        Assert.False(vm.Completion.IsComplete);
        await _resultLogService.DidNotReceive().WriteAsync(
            Arg.Any<ResultLogEntry>(), Arg.Any<CancellationToken>());
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
                Arg.Any<IProgress<OperationProgress>?>(), Arg.Any<CancellationToken>(),
                Arg.Any<IReadOnlyList<PatchClaim>?>())
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
                Arg.Any<IEnumerable<string>>(),
                Arg.Any<IProgress<OperationProgress>?>(), Arg.Any<CancellationToken>(),
                Arg.Any<IReadOnlyList<PatchClaim>?>())
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
    public async Task ScanViewModel_MissingRemovableCount_stays_off_the_missing_from_disk_banner()
    {
        // Removable+missing is the expected end state of a patch the API
        // still claims but whose file has already been cleaned away, and
        // InstallerClean leaves one behind every time it removes a
        // superseded patch. Nothing on screen reports them; the count is
        // carried for the opt-in report. What matters is that it never
        // reaches the load-bearing missing-from-disk banner, which means
        // files Windows still needs have gone.
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

        Assert.False(vm.Scan.HasMissingFromDisk);
        Assert.Equal(2, vm.Scan.MissingRemovableCount);
    }

    [Fact]
    public async Task ScanViewModel_HasRecordsNotMatched_tracks_UnaccountedProductCount()
    {
        // A scan that could not account for everything the records hold kept its
        // superseded patches back. Without this line the only symptom is a quietly
        // shorter list, so the line is the whole point of the count reaching the VM.
        var vm = CreateViewModel();

        var scan = new ScanResult(
            RemovableFiles: Array.Empty<OrphanedFile>(),
            RegisteredPackages: Array.Empty<RegisteredPackage>(),
            RegisteredTotalBytes: 0,
            UnaccountedProductCount: 3);
        _scanService.ScanAsync(Arg.Any<IProgress<ScanProgressUpdate>?>(), Arg.Any<CancellationToken>())
            .Returns(scan);

        await vm.Scan.ScanCommand.ExecuteAsync(null);

        Assert.True(vm.Scan.HasRecordsNotMatched);
        Assert.Equal(3, vm.Scan.UnaccountedProductCount);
        // The count gates the line and stays out of it. Four different things feed
        // that number, only two are failures to read, and a registry key an
        // uninstall left behind is one of them with no installed program answering
        // to it, so a figure on screen would be a precision the scan has not got.
        // Pinned as a negative because the count reaching the VM and the count
        // reaching the user are now different questions, and the assertion that
        // used to stand here was the second one answering yes.
        Assert.Equal(Strings.Summary_RecordsNotMatched, vm.Scan.RecordsNotMatchedText);
        Assert.DoesNotContain("3", vm.Scan.RecordsNotMatchedText);
    }

    [Fact]
    public async Task ScanViewModel_HasRecordsNotMatched_is_false_on_a_healthy_scan()
    {
        var vm = CreateViewModel();
        _scanService.ScanAsync(Arg.Any<IProgress<ScanProgressUpdate>?>(), Arg.Any<CancellationToken>())
            .Returns(new ScanResult(
                Array.Empty<OrphanedFile>(), Array.Empty<RegisteredPackage>(), 0));

        await vm.Scan.ScanCommand.ExecuteAsync(null);

        Assert.False(vm.Scan.HasRecordsNotMatched);
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
        // anyway. HasScanned is what stops that window painting "0 unneeded
        // files to clean up" and "0 files left alone": a clean bill of health
        // for a scan that never ran.
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
            .ThrowsAsync(new LocalisedInvalidOperationException(Strings.Error_InstallerDbEmpty));

        // Must NOT throw. A propagated exception here reaches App.OnStartup,
        // which exits the process; returning instead is what lets the window
        // open in its error state.
        await vm.Scan.ScanWithProgressAsync(null);

        Assert.False(vm.Scan.HasScanned);
        Assert.True(vm.Scan.HasScanError);
        Assert.Equal(Strings.Error_InstallerDbEmpty, vm.Scan.LastScanError);
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
            .ThrowsAsync(new LocalisedInvalidOperationException(Strings.Error_InstallerDbEmpty));

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
    public async Task A_scan_that_finds_nothing_keeps_the_windows_one_shape()
    {
        var vm = CreateViewModel();
        _scanService.ScanAsync(Arg.Any<IProgress<ScanProgressUpdate>?>(), Arg.Any<CancellationToken>())
            .Returns(EmptyScanResult());

        await vm.Scan.ScanCommand.ExecuteAsync(null);

        // Zero files changes no part of the window's shape: the lead ("Any
        // unneeded files below...") reads correctly over an empty list, the
        // explanation and the action line stay, and the action zone shows with
        // the buttons disabled through their commands.
        Assert.Equal(Strings.Body_MainExplanation_Lead, vm.IntroLead);
        Assert.Equal(vm.MainExplanationWhyText, vm.IntroDetail);
        Assert.True(vm.Scan.HasScanned);
        Assert.False(vm.Scan.HasOrphans);
        Assert.True(vm.ShowMainAction);
        Assert.True(vm.ShowActionZone);
        Assert.False(vm.Cleanup.MoveAllCommand.CanExecute(null));
        Assert.False(vm.Cleanup.DeleteAllCommand.CanExecute(null));
        // Details still greys: the orphaned list it opens is empty here.
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
        Assert.True(vm.ShowActionZone);
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
        // The zone itself stays: greyed buttons under the banner, not a hole.
        Assert.True(vm.ShowActionZone);
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

        // Idle, so the busy check does not block: the relaunch still fires.
        _windowService.Received(1).RelaunchForLanguageChange();
    }
}
