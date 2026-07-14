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
    public async Task Cancelling_a_pre_flight_that_completes_anyway_still_stops_the_move()
    {
        // Hold the probe open on its last call, which sits after its last
        // cancellation checkpoint: the click therefore lands too late for the
        // probe body to observe it, and the probe completes successfully.
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
        // No confirmation dialog, no move: the cancel is honoured even though
        // the probe won the race. Before this was checked, the user who
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
}
