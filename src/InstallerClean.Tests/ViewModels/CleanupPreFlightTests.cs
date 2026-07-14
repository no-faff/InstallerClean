using System.IO.Abstractions;
using System.IO.Abstractions.TestingHelpers;
using NSubstitute;
using InstallerClean.Models;
using InstallerClean.Services;
using InstallerClean.ViewModels;

namespace InstallerClean.Tests.ViewModels;

/// <summary>
/// The Move pre-flight: the CreateDirectory-and-write-probe pass that runs
/// on a worker thread before the confirmation dialog. It has its own
/// cancellation scope, its own overlay reveal and four early returns that
/// never reach the operation's finally, so it is exercised here rather than
/// in <see cref="MainViewModelTests"/>, whose MockFileSystem cannot be made
/// to block mid-probe.
///
/// The filesystem is substituted per member (Path from a real MockFileSystem,
/// Directory and File as fakes) so a single probe call can be held open on a
/// gate and the Cancel click landed at an exact point inside it.
/// </summary>
public class CleanupPreFlightTests
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
    private readonly IResultLogService _resultLogService = Substitute.For<IResultLogService>();

    private readonly IFileSystem _fileSystem = Substitute.For<IFileSystem>();
    private readonly IDirectory _directory = Substitute.For<IDirectory>();
    private readonly IFile _file = Substitute.For<IFile>();

    private readonly string _destination = Path.Combine(Path.GetTempPath(), "ic-test-preflight");

    public CleanupPreFlightTests()
    {
        _settingsService.Load().Returns(new AppSettings());
        // MockFileSystem's Path is a working implementation; a bare substitute
        // would return null from Combine and GetRandomFileName.
        _fileSystem.Path.Returns(new MockFileSystem().Path);
        _fileSystem.Directory.Returns(_directory);
        _fileSystem.File.Returns(_file);

        _scanService.ScanAsync(Arg.Any<IProgress<ScanProgressUpdate>?>(), Arg.Any<CancellationToken>())
            .Returns(new ScanResult(
                new List<OrphanedFile>
                {
                    new(@"C:\Windows\Installer\a.msi", 1024, false, false, false,
                        InstallerClean.Resources.Strings.Reason_Orphaned),
                },
                Array.Empty<RegisteredPackage>(), 0));
    }

    private MainViewModel CreateViewModel() =>
        new(_scanService, _moveService, _deleteService,
            _settingsService, _rebootService, _msiInfoService,
            _dialogService, _confirmationService, _windowService,
            _fileSystem, _resultLogService);

    /// <summary>
    /// Spins until <paramref name="condition"/> holds. The view-models have no
    /// dispatcher under test, so a state change made on a thread-pool thread
    /// (the probe's overlay reveal) has no callback to await.
    /// </summary>
    private static async Task WaitUntil(Func<bool> condition)
    {
        for (var i = 0; i < 200 && !condition(); i++)
            await Task.Delay(25);
        Assert.True(condition(), "state was not reached within 5 seconds");
    }

    [Fact]
    public async Task Cancelling_the_pre_flight_leaves_the_next_operation_cancellable()
    {
        var probeEntered = new ManualResetEventSlim();
        var releaseProbe = new ManualResetEventSlim();
        _directory.When(d => d.CreateDirectory(Arg.Any<string>())).Do(_ =>
        {
            probeEntered.Set();
            releaseProbe.Wait();
        });

        var vm = CreateViewModel();
        await vm.Scan.ScanWithProgressAsync(null);
        vm.Cleanup.MoveDestination = _destination;

        var move = vm.Cleanup.MoveAllCommand.ExecuteAsync(null);

        // The probe outlives the 200 ms reveal delay, so the overlay (and with
        // it a live Cancel button) is up, exactly as it would be on the slow
        // UNC destination the off-thread probe exists for.
        await WaitUntil(() => vm.Cleanup.IsOperating);
        Assert.True(vm.Cleanup.CancelOperationCommand.CanExecute(null));
        vm.Cleanup.CancelOperationCommand.Execute(null);
        releaseProbe.Set();
        await move;

        Assert.True(probeEntered.IsSet);
        Assert.False(vm.Cleanup.IsOperating);
        Assert.Equal(string.Empty, vm.Cleanup.OperationProgress);
        await _moveService.DidNotReceive().MoveFilesAsync(
            Arg.Any<IEnumerable<string>>(), Arg.Any<string>(),
            Arg.Any<IProgress<OperationProgress>?>(), Arg.Any<CancellationToken>());

        // The regression: IsCancellationRequested is what gates the Cancel
        // button, and the cancelled pre-flight used to leave it set, so Cancel
        // and Esc were dead for the whole of the next operation.
        Assert.False(vm.Cleanup.IsCancellationRequested);

        var releaseMove = new ManualResetEventSlim();
        _confirmationService.ConfirmMove(Arg.Any<int>(), Arg.Any<string>(), Arg.Any<string>())
            .Returns(true);
        _moveService.MoveFilesAsync(
                Arg.Any<IEnumerable<string>>(), Arg.Any<string>(),
                Arg.Any<IProgress<OperationProgress>?>(), Arg.Any<CancellationToken>())
            .Returns(_ => Task.Run(() =>
            {
                releaseMove.Wait();
                return new MoveResult(1, Array.Empty<FileOperationError>());
            }));

        var second = vm.Cleanup.MoveAllCommand.ExecuteAsync(null);
        await WaitUntil(() => vm.Cleanup.IsOperating);
        Assert.True(vm.Cleanup.CancelOperationCommand.CanExecute(null));
        releaseMove.Set();
        await second;
    }

    [Fact]
    public async Task Cancelling_on_the_probes_last_call_never_reaches_the_confirmation()
    {
        // Hold the probe open on the last filesystem call it makes, so the
        // Cancel lands at the latest point the probe can still act on it. The
        // move must not proceed, and the user must not be asked to confirm one.
        var probeFinishing = new ManualResetEventSlim();
        var releaseProbe = new ManualResetEventSlim();
        _file.When(f => f.Delete(Arg.Any<string>())).Do(_ =>
        {
            probeFinishing.Set();
            releaseProbe.Wait();
        });

        var vm = CreateViewModel();
        await vm.Scan.ScanWithProgressAsync(null);
        vm.Cleanup.MoveDestination = _destination;

        var move = vm.Cleanup.MoveAllCommand.ExecuteAsync(null);
        await WaitUntil(() => vm.Cleanup.IsOperating);
        vm.Cleanup.CancelOperationCommand.Execute(null);
        releaseProbe.Set();
        await move;

        Assert.True(probeFinishing.IsSet);
        // The pre-flight used to check the token only between its filesystem
        // calls, so a Cancel this late was honoured too late: the user who
        // cancelled still got the "move 1 file?" dialog, and only the move
        // behind it failed on the already-cancelled token.
        _confirmationService.DidNotReceive().ConfirmMove(
            Arg.Any<int>(), Arg.Any<string>(), Arg.Any<string>());
        await _moveService.DidNotReceive().MoveFilesAsync(
            Arg.Any<IEnumerable<string>>(), Arg.Any<string>(),
            Arg.Any<IProgress<OperationProgress>?>(), Arg.Any<CancellationToken>());
        Assert.False(vm.Cleanup.IsCancellationRequested);
        Assert.False(vm.Cleanup.IsOperating);
    }

    [Fact]
    public async Task A_destination_that_cannot_be_written_stops_before_the_confirmation()
    {
        _file.When(f => f.WriteAllBytes(Arg.Any<string>(), Arg.Any<byte[]>()))
            .Do(_ => throw new UnauthorizedAccessException("denied"));

        var vm = CreateViewModel();
        await vm.Scan.ScanWithProgressAsync(null);
        vm.Cleanup.MoveDestination = _destination;

        await vm.Cleanup.MoveAllCommand.ExecuteAsync(null);

        _dialogService.Received(1).ShowWarning(
            Arg.Any<string>(), InstallerClean.Resources.Strings.Error_DestinationWriteFailedTitle);
        _confirmationService.DidNotReceive().ConfirmMove(
            Arg.Any<int>(), Arg.Any<string>(), Arg.Any<string>());
        await _moveService.DidNotReceive().MoveFilesAsync(
            Arg.Any<IEnumerable<string>>(), Arg.Any<string>(),
            Arg.Any<IProgress<OperationProgress>?>(), Arg.Any<CancellationToken>());
        Assert.False(vm.Cleanup.IsOperating);
        Assert.Equal(string.Empty, vm.Cleanup.OperationProgress);
    }

    [Fact]
    public async Task A_destination_inside_the_installer_cache_is_refused_without_creating_it()
    {
        var vm = CreateViewModel();
        await vm.Scan.ScanWithProgressAsync(null);
        vm.Cleanup.MoveDestination = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.Windows), "Installer", "backup");

        await vm.Cleanup.MoveAllCommand.ExecuteAsync(null);

        _dialogService.Received(1).ShowWarning(
            InstallerClean.Resources.Strings.Error_DestinationInsideInstaller,
            InstallerClean.Resources.Strings.Error_InvalidDestinationTitle);
        // The gate runs before anything is created: the whole restore story
        // collapses if the files end up back inside the cache folder.
        _directory.DidNotReceive().CreateDirectory(Arg.Any<string>());
        _confirmationService.DidNotReceive().ConfirmMove(
            Arg.Any<int>(), Arg.Any<string>(), Arg.Any<string>());
        await _moveService.DidNotReceive().MoveFilesAsync(
            Arg.Any<IEnumerable<string>>(), Arg.Any<string>(),
            Arg.Any<IProgress<OperationProgress>?>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task A_destination_inside_a_system_folder_is_refused_without_creating_it()
    {
        var vm = CreateViewModel();
        await vm.Scan.ScanWithProgressAsync(null);
        vm.Cleanup.MoveDestination = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "ic-test-backup");

        await vm.Cleanup.MoveAllCommand.ExecuteAsync(null);

        _dialogService.Received(1).ShowWarning(
            Arg.Any<string>(), InstallerClean.Resources.Strings.Error_InvalidDestinationTitle);
        _directory.DidNotReceive().CreateDirectory(Arg.Any<string>());
        await _moveService.DidNotReceive().MoveFilesAsync(
            Arg.Any<IEnumerable<string>>(), Arg.Any<string>(),
            Arg.Any<IProgress<OperationProgress>?>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task A_refused_pre_flight_leaves_the_commands_usable()
    {
        var vm = CreateViewModel();
        await vm.Scan.ScanWithProgressAsync(null);
        vm.Cleanup.MoveDestination = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.Windows), "Installer", "backup");

        await vm.Cleanup.MoveAllCommand.ExecuteAsync(null);

        // Every exit from the pre-flight, refusals included, has to end the
        // operation: the in-flight flag is what disables both destructive
        // commands and Re-scan, and a leaked one locks the window down.
        Assert.False(vm.Cleanup.IsOperationInFlight);
        Assert.True(vm.Cleanup.MoveAllCommand.CanExecute(null));
        Assert.True(vm.Cleanup.DeleteAllCommand.CanExecute(null));
        Assert.True(vm.Scan.ScanCommand.CanExecute(null));
    }

    [Fact]
    public async Task Move_and_Delete_are_blocked_while_a_pre_flight_runs()
    {
        var releaseProbe = new ManualResetEventSlim();
        _directory.When(d => d.CreateDirectory(Arg.Any<string>())).Do(_ => releaseProbe.Wait());

        var vm = CreateViewModel();
        await vm.Scan.ScanWithProgressAsync(null);
        vm.Cleanup.MoveDestination = _destination;
        Assert.True(vm.Cleanup.DeleteAllCommand.CanExecute(null));

        var move = vm.Cleanup.MoveAllCommand.ExecuteAsync(null);

        // Asserted synchronously, so the probe is provably still inside the
        // 200 ms before the overlay would appear. The dispatcher pumps input
        // through that window: a Delete started in it would overwrite the
        // Move's cancellation source while the Move was still using it.
        Assert.True(vm.Cleanup.IsOperationInFlight);
        Assert.False(vm.Cleanup.IsOperating);
        Assert.False(vm.Cleanup.DeleteAllCommand.CanExecute(null));
        Assert.False(vm.Cleanup.MoveAllCommand.CanExecute(null));
        Assert.False(vm.Scan.ScanCommand.CanExecute(null));

        releaseProbe.Set();
        await move;
    }

    [Fact]
    public async Task The_pre_flight_creates_and_probes_the_destination_before_the_confirmation()
    {
        _confirmationService.ConfirmMove(Arg.Any<int>(), Arg.Any<string>(), Arg.Any<string>())
            .Returns(false);

        var vm = CreateViewModel();
        await vm.Scan.ScanWithProgressAsync(null);
        vm.Cleanup.MoveDestination = _destination;

        await vm.Cleanup.MoveAllCommand.ExecuteAsync(null);

        _directory.Received(1).CreateDirectory(_destination);
        // Write-then-delete of a throwaway file: the probe proves the folder is
        // writable, so a read-only destination fails once here rather than once
        // per file inside the move.
        _file.Received(1).WriteAllBytes(Arg.Any<string>(), Arg.Any<byte[]>());
        _file.Received(1).Delete(Arg.Any<string>());
        _confirmationService.Received(1).ConfirmMove(1, Arg.Any<string>(), _destination);
    }

    [Fact]
    public async Task A_relative_destination_is_refused_without_creating_it()
    {
        var vm = CreateViewModel();
        await vm.Scan.ScanWithProgressAsync(null);
        vm.Cleanup.MoveDestination = "backup";

        await vm.Cleanup.MoveAllCommand.ExecuteAsync(null);

        _dialogService.Received(1).ShowWarning(
            string.Format(InstallerClean.Resources.Strings.Error_DestinationNotFullyQualified, "backup"),
            InstallerClean.Resources.Strings.Error_InvalidDestinationTitle);
        // The refusal comes before the probe, not after the confirmation:
        // "backup" resolves against the process CWD, so creating and probing it
        // first would have the elevated process write to a folder the user never
        // named, at a path that moves with wherever the exe was started from.
        _directory.DidNotReceive().CreateDirectory(Arg.Any<string>());
        _file.DidNotReceive().WriteAllBytes(Arg.Any<string>(), Arg.Any<byte[]>());
        _confirmationService.DidNotReceive().ConfirmMove(
            Arg.Any<int>(), Arg.Any<string>(), Arg.Any<string>());
        await _moveService.DidNotReceive().MoveFilesAsync(
            Arg.Any<IEnumerable<string>>(), Arg.Any<string>(),
            Arg.Any<IProgress<OperationProgress>?>(), Arg.Any<CancellationToken>());
        Assert.False(vm.Cleanup.IsOperationInFlight);
    }
}
