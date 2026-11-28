using System.Collections.Concurrent;
using System.IO.Abstractions;
using System.IO.Abstractions.TestingHelpers;
using InstallerClean.Helpers;
using InstallerClean.Models;
using InstallerClean.Resources;
using InstallerClean.Services;
using InstallerClean.ViewModels;
using InstallerClean.Tests.Helpers;
using NSubstitute;

namespace InstallerClean.Tests.ViewModels;

/// <summary>
/// The button on the Move or Delete card that gives up on the drive or share a waiting heading
/// names, during the check made before the batch and during the scan after it. It goes up with
/// the wait's own line and goes with the wait's end or any other write to the heading,
/// whatever writes it, whether or not any of these writes changes the text, and the name a
/// screen reader speaks for the heading changes with it in the same step. Each Move here is
/// started on a stand-in for the window's dispatcher, so the card's reporters are made there
/// and the waits reach the view model there, one at a time and in order, as they do in the
/// window. The check and the scan run on the thread pool and report the steps a test gives
/// them, each step returning once the card has applied what it reported. The filesystem and
/// the destination guard are substituted as in <see cref="CleanupPreFlightTests"/>, so the
/// check before the Move is reached.
/// </summary>
public class CleanupStopWaitingTests : IDisposable
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
    private readonly IBatchCheck _batchCheck = Substitute.For<IBatchCheck>();
    private readonly IEarlierRunCheck _earlierRunCheck = Substitute.For<IEarlierRunCheck>();
    private readonly IFirstRunMark _firstRunMark = FirstRunMarks.Claiming();
    private readonly IWindowsRegion _windowsRegion = Substitute.For<IWindowsRegion>();
    private readonly IUpdateCheckService _updateCheckService = Substitute.For<IUpdateCheckService>();

    private readonly IFileSystem _fileSystem = Substitute.For<IFileSystem>();
    private readonly IDirectory _directory = Substitute.For<IDirectory>();
    private readonly IFile _file = Substitute.For<IFile>();

    private readonly DispatcherStandIn _dispatcher = new();
    private readonly string _destination = Path.Combine(Path.GetTempPath(), "ic-test-stop-waiting");

    public CleanupStopWaitingTests()
    {
        _settingsService.Load().Returns(new AppSettings());
        // A working settings service: a destination save that does land records nothing.
        _settingsService.Update(Arg.Any<Action<AppSettings>>()).Returns(true);
        _rebootService.Check().Returns(PendingRebootResult.Clean);
        _confirmationService.ConfirmMove(Arg.Any<int>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<bool>())
            .Returns(true);
        // Everything survives the check unless a test says otherwise.
        _batchCheck.RunAsync(
                Arg.Any<IReadOnlyList<string>>(), Arg.Any<TimeSpan?>(), Arg.Any<IProgress<BatchCheckProgress>>(),
                Arg.Any<CancellationToken>())
            .Returns(ci => Task.FromResult(StandingChecks.Stood(
                new ReverifyResult(ci.Arg<IReadOnlyList<string>>(), Array.Empty<string>()))));
        _fileSystem.Path.Returns(new MockFileSystem().Path);
        _fileSystem.Directory.Returns(_directory);
        _fileSystem.File.Returns(_file);
        _scanService.ScanAsync(Arg.Any<IProgress<ScanProgressUpdate>?>(), Arg.Any<CancellationToken>())
            .Returns(new ScanResult(
                new List<OrphanedFile>
                {
                    new(@"C:\Windows\Installer\a.msi", 1024, false, false, false, Strings.Reason_Orphaned),
                },
                Array.Empty<RegisteredPackage>(), 0));
    }

    private MainViewModel CreateViewModel() =>
        _made.Add(new MainViewModel(_scanService, _moveService, _deleteService,
            _settingsService, _rebootService, _msiInfoService,
            _dialogService, _confirmationService, _windowService,
            _fileSystem, _resultLogService, _updateCheckService, _batchCheck,
            _earlierRunCheck, _firstRunMark, _windowsRegion,
            destinationGuard: new ByPathDestinationGuard(_fileSystem)));

    /// <summary>The view models this class made, flushed after each test.</summary>
    private readonly MadeViewModels _made = new();

    /// <summary>
    /// Flushes on the stand-in, after everything posted to it, as the window flushes on its
    /// own thread.
    /// </summary>
    public void Dispose()
    {
        var flushed = _dispatcher.RunAsync(_made.Dispose);
        if (flushed.Wait(TimeSpan.FromSeconds(10)))
        {
            flushed.GetAwaiter().GetResult();
            return;
        }

        // The stand-in is stuck behind a callback that never returns: flush here, which
        // still puts the crash-log folder back.
        _made.Dispose();
        throw new TimeoutException("The dispatcher stand-in did not reach the flush.");
    }

    private static string WaitLine(string root) => DisplayHelpers.WaitingFor(root);

    /// <summary>A view model whose scan found one file to move, with a destination typed.</summary>
    private async Task<MainViewModel> ScannedViewModelAsync()
    {
        var vm = CreateViewModel();
        await vm.Scan.ScanWithProgressAsync(null);
        vm.Cleanup.MoveDestination = _destination;
        return vm;
    }

    /// <summary>Reports <paramref name="update"/> and returns once the card has applied it.</summary>
    private async Task ShowAsync(IProgress<ScanProgressUpdate> progress, ScanProgressUpdate update)
    {
        progress.Report(update);
        await _dispatcher.IdleAsync();
    }

    /// <summary>
    /// A check before the Move that runs <paramref name="steps"/> in turn on the thread pool
    /// and then keeps every file back, so the batch never runs. Each step is handed the
    /// reporter the card gave the check and the check's token.
    /// </summary>
    private void CheckReports(params Func<IProgress<ScanProgressUpdate>, CancellationToken, Task>[] steps) =>
        _batchCheck.RunAsync(
                Arg.Any<IReadOnlyList<string>>(), Arg.Any<TimeSpan?>(), Arg.Any<IProgress<BatchCheckProgress>>(),
                Arg.Any<CancellationToken>())
            .Returns(ci =>
            {
                var files = ci.Arg<IReadOnlyList<string>>();
                var token = ci.Arg<CancellationToken>();
                var progress = new SourceWaitReporter(ci.Arg<IProgress<BatchCheckProgress>>());
                return Task.Run(async () =>
                {
                    foreach (var step in steps) await step(progress, token);
                    token.ThrowIfCancellationRequested();
                    return StandingChecks.Stood(
                        new ReverifyResult(Array.Empty<string>(), files, new HeldBackReasons(Reclaimed: files.Count)));
                });
            });

    /// <summary>Reports each wait a step tells as the check's own source-wait stage.</summary>
    private sealed class SourceWaitReporter(IProgress<BatchCheckProgress> check) : IProgress<ScanProgressUpdate>
    {
        public void Report(ScanProgressUpdate value) =>
            check.Report(new BatchCheckProgress(BatchCheckStage.SourceWait, SourceWait: value));
    }

    /// <summary>Starts the Move on the dispatcher, as the button does, and waits for it to end.</summary>
    private async Task MoveAsync(MainViewModel vm)
    {
        var move = Task.CompletedTask;
        await _dispatcher.RunAsync(() => move = vm.Cleanup.MoveAllCommand.ExecuteAsync(null));
        await move;
        Assert.Empty(_dispatcher.Thrown);
    }

    [Fact]
    public async Task A_wait_the_check_reports_puts_the_button_up_with_its_heading_and_its_end_takes_both_back()
    {
        var vm = await ScannedViewModelAsync();
        var wait = new SourceFolderWait("D:", () => { });
        bool? upWithItsHeading = null, goneWithIt = null;
        CheckReports(
            async (p, _) =>
            {
                await ShowAsync(p, ScanProgressUpdate.Waiting(wait));
                var c = vm.Cleanup;
                upWithItsHeading = c.OperationProgress == WaitLine("D:")
                    && c.CanStopWaiting && ReferenceEquals(c.WaitShown, wait)
                    && c.StopWaitingName == DisplayHelpers.StopWaitingFor("D:")
                    && c.OperationProgressName == DisplayHelpers.WaitingLineWithStopKey(WaitLine("D:"))
                    && c.StopWaitingCommand.CanExecute(null);
            },
            async (p, _) =>
            {
                await ShowAsync(p, ScanProgressUpdate.Waiting(null));
                var c = vm.Cleanup;
                goneWithIt = c.OperationProgress == Strings.Status_Moving
                    && !c.CanStopWaiting && c.WaitShown is null && c.StopWaitingName.Length == 0
                    && c.OperationProgressName == Strings.Status_Moving && !c.StopWaitingCommand.CanExecute(null);
            });

        await MoveAsync(vm);

        Assert.True(upWithItsHeading);
        Assert.True(goneWithIt);
    }

    [Fact]
    public async Task The_button_stops_the_wait_its_heading_names()
    {
        var vm = await ScannedViewModelAsync();
        var stops = 0;
        var wait = new SourceFolderWait(@"\\fileserver\apps", () => Interlocked.Increment(ref stops));
        CheckReports(async (p, _) =>
        {
            await ShowAsync(p, ScanProgressUpdate.Waiting(wait));
            await _dispatcher.RunAsync(() => vm.Cleanup.StopWaitingCommand.Execute(null));
            await ShowAsync(p, ScanProgressUpdate.Waiting(null));
        });

        await MoveAsync(vm);

        Assert.Equal(1, stops);
        Assert.False(vm.Cleanup.CanStopWaiting);
    }

    [Fact]
    public async Task Cancel_takes_the_button_away_and_a_wait_told_after_it_brings_none_back()
    {
        var vm = await ScannedViewModelAsync();
        bool? goneOnCancel = null, upAfterTheLaterWait = null;
        CheckReports(
            (p, _) => ShowAsync(p, ScanProgressUpdate.Waiting(new SourceFolderWait("D:", () => { }))),
            async (p, token) =>
            {
                await _dispatcher.RunAsync(() => vm.Cleanup.CancelOperationCommand.Execute(null));
                goneOnCancel = !vm.Cleanup.CanStopWaiting && vm.Cleanup.OperationProgress == Strings.Status_Cancelling;

                // A second wait, told once Cancel is pressed and before the check sees it.
                await ShowAsync(p, ScanProgressUpdate.Waiting(new SourceFolderWait("E:", () => { })));
                upAfterTheLaterWait = vm.Cleanup.CanStopWaiting;
                token.ThrowIfCancellationRequested();
            });

        await MoveAsync(vm);

        Assert.True(goneOnCancel);
        Assert.False(upAfterTheLaterWait);
        Assert.False(vm.Cleanup.CanStopWaiting);
    }

    [Fact]
    public async Task The_heading_written_after_the_check_takes_the_button_away()
    {
        // The check ends with a wait still on the heading, which it never leaves, so what
        // takes the button away is the scan after it writing its own heading. That heading
        // is read as the scan starts.
        var vm = await ScannedViewModelAsync();
        bool? upAtTheEnd = null;
        (string Heading, bool Button)? asTheScanStarts = null;
        CheckReports(async (p, _) =>
        {
            await ShowAsync(p, ScanProgressUpdate.Waiting(new SourceFolderWait("D:", () => { })));
            upAtTheEnd = vm.Cleanup.CanStopWaiting;
        });
        _scanService.ScanAsync(Arg.Any<IProgress<ScanProgressUpdate>?>(), Arg.Any<CancellationToken>())
            .Returns(_ =>
            {
                asTheScanStarts = (vm.Cleanup.OperationProgress, vm.Cleanup.CanStopWaiting);
                return new ScanResult(new List<OrphanedFile>(), Array.Empty<RegisteredPackage>(), 0);
            });

        await MoveAsync(vm);

        Assert.True(upAtTheEnd);
        Assert.Equal((Strings.Status_Scanning, false), asTheScanStarts);
        Assert.False(vm.Cleanup.CanStopWaiting);
    }

    [Fact]
    public async Task A_wait_the_scan_after_the_Move_reports_puts_the_button_up_with_its_heading_too()
    {
        var vm = await ScannedViewModelAsync();
        var wait = new SourceFolderWait("D:", () => { });
        bool? upWithItsHeading = null, goneWithIt = null;
        _moveService.MoveFilesAsync(
                Arg.Any<StandingCheck>(), Arg.Any<string>(),
                Arg.Any<IProgress<OperationProgress>?>(), Arg.Any<CancellationToken>())
            .Returns(new MoveResult(1, Array.Empty<FileOperationError>()));
        _scanService.ScanAsync(Arg.Any<IProgress<ScanProgressUpdate>?>(), Arg.Any<CancellationToken>())
            .Returns(ci =>
            {
                var progress = (IProgress<ScanProgressUpdate>)ci[0]!;
                return Task.Run(async () =>
                {
                    await ShowAsync(progress, ScanProgressUpdate.Waiting(wait));
                    var c = vm.Cleanup;
                    upWithItsHeading = c.OperationProgress == WaitLine("D:")
                        && ReferenceEquals(c.WaitShown, wait)
                        && c.OperationProgressName == DisplayHelpers.WaitingLineWithStopKey(WaitLine("D:"));
                    await ShowAsync(progress, ScanProgressUpdate.Waiting(null));
                    goneWithIt = vm.Cleanup.OperationProgress == Strings.Status_Scanning && !vm.Cleanup.CanStopWaiting;
                    return new ScanResult(new List<OrphanedFile>(), Array.Empty<RegisteredPackage>(), 0);
                });
            });

        await MoveAsync(vm);

        Assert.True(upWithItsHeading);
        Assert.True(goneWithIt);
    }

    [Fact]
    public async Task The_spoken_heading_changes_once_for_each_wait_and_never_to_the_bare_waiting_line()
    {
        // Every value the spoken name is raised with, and whether the button was up each
        // time the heading itself was raised. Narrator speaks a live region's name as it
        // changes, so a bare waiting line raised before the full one would be heard twice.
        var vm = await ScannedViewModelAsync();
        var spoken = new ConcurrentQueue<string>();
        var buttonWhenTheHeadingChanged = new ConcurrentQueue<(string Heading, bool Button)>();
        vm.Cleanup.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName == nameof(CleanupViewModel.OperationProgressName))
                spoken.Enqueue(vm.Cleanup.OperationProgressName);
            if (e.PropertyName == nameof(CleanupViewModel.OperationProgress))
                buttonWhenTheHeadingChanged.Enqueue((vm.Cleanup.OperationProgress, vm.Cleanup.CanStopWaiting));
        };
        CheckReports(
            (p, _) => ShowAsync(p, ScanProgressUpdate.Waiting(new SourceFolderWait("D:", () => { }))),
            (p, _) => ShowAsync(p, ScanProgressUpdate.Waiting(null)));

        await MoveAsync(vm);

        // The same value raised twice in a row is one change to a screen reader: the
        // heading's own change raises the name again with the value it already has.
        var changes = spoken.Aggregate(new List<string>(), (seen, name) =>
        {
            if (seen.Count == 0 || seen[^1] != name) seen.Add(name);
            return seen;
        });
        var full = DisplayHelpers.WaitingLineWithStopKey(WaitLine("D:"));

        Assert.DoesNotContain(WaitLine("D:"), changes);
        Assert.Single(changes, name => name == full);
        var at = changes.IndexOf(full);
        Assert.Equal([Strings.Status_Moving, full, Strings.Status_Moving], changes.Skip(at - 1).Take(3));
        Assert.Contains((WaitLine("D:"), true), buttonWhenTheHeadingChanged);
    }

    [Fact]
    public async Task A_second_wait_whose_heading_reads_the_same_as_the_first_s_takes_the_button()
    {
        // Two waits on one drive with no end between them write the same heading twice, so
        // the second write leaves the text as it was. The button stops the second.
        var vm = await ScannedViewModelAsync();
        int firstStops = 0, secondStops = 0;
        var first = new SourceFolderWait("D:", () => Interlocked.Increment(ref firstStops));
        var second = new SourceFolderWait("D:", () => Interlocked.Increment(ref secondStops));
        bool? onTheSecond = null;
        CheckReports(
            (p, _) => ShowAsync(p, ScanProgressUpdate.Waiting(first)),
            async (p, _) =>
            {
                await ShowAsync(p, ScanProgressUpdate.Waiting(second));
                onTheSecond = vm.Cleanup.OperationProgress == WaitLine("D:")
                    && ReferenceEquals(vm.Cleanup.WaitShown, second);
                await _dispatcher.RunAsync(() => vm.Cleanup.StopWaitingCommand.Execute(null));
                await ShowAsync(p, ScanProgressUpdate.Waiting(null));
            });

        await MoveAsync(vm);

        Assert.True(onTheSecond);
        Assert.Equal((0, 1), (firstStops, secondStops));
    }

    [Fact]
    public async Task A_wait_and_its_end_move_the_button_where_the_heading_reads_the_same_throughout()
    {
        // The heading is written with the waiting line's own words before the wait, so
        // neither the wait nor its end changes the text on it.
        var vm = await ScannedViewModelAsync();
        var wait = new SourceFolderWait("D:", () => { });
        bool? upWithTheWait = null, goneWithItsEnd = null;
        CheckReports(
            async (p, _) =>
            {
                await _dispatcher.RunAsync(() => vm.Cleanup.OperationProgress = WaitLine("D:"));
                await ShowAsync(p, ScanProgressUpdate.Waiting(wait));
                var c = vm.Cleanup;
                upWithTheWait = ReferenceEquals(c.WaitShown, wait) && c.StopWaitingCommand.CanExecute(null)
                    && c.OperationProgressName == DisplayHelpers.WaitingLineWithStopKey(WaitLine("D:"));
            },
            async (p, _) =>
            {
                await ShowAsync(p, ScanProgressUpdate.Waiting(null));
                var c = vm.Cleanup;
                goneWithItsEnd = c.WaitShown is null && !c.StopWaitingCommand.CanExecute(null)
                    && c.OperationProgress == WaitLine("D:") && c.OperationProgressName == WaitLine("D:");
            });

        await MoveAsync(vm);

        Assert.True(upWithTheWait);
        Assert.True(goneWithItsEnd);
    }

    [Fact]
    public async Task A_heading_worded_as_the_waiting_line_takes_the_button_away_and_raises_only_the_button()
    {
        // A write to the heading during the wait, other than the wait's own, that reads as
        // the waiting line, so the text does not change. The button goes with it.
        var vm = await ScannedViewModelAsync();
        var raised = new List<string?>();
        var buttonChecks = 0;
        void Raised(object? _, System.ComponentModel.PropertyChangedEventArgs e) => raised.Add(e.PropertyName);
        void ButtonChecked(object? _, EventArgs e) => buttonChecks++;
        bool? goneWithTheWrite = null;
        CheckReports(async (p, _) =>
        {
            await ShowAsync(p, ScanProgressUpdate.Waiting(new SourceFolderWait("D:", () => { })));
            await _dispatcher.RunAsync(() =>
            {
                var c = vm.Cleanup;
                c.PropertyChanged += Raised;
                c.StopWaitingCommand.CanExecuteChanged += ButtonChecked;
                c.OperationProgress = WaitLine("D:");
                c.PropertyChanged -= Raised;
                c.StopWaitingCommand.CanExecuteChanged -= ButtonChecked;
                goneWithTheWrite = c.WaitShown is null && !c.StopWaitingCommand.CanExecute(null)
                    && c.OperationProgress == WaitLine("D:") && c.OperationProgressName == WaitLine("D:");
            });
        });

        await MoveAsync(vm);

        Assert.True(goneWithTheWrite);
        Assert.Equal(
            [nameof(CleanupViewModel.WaitShown), nameof(CleanupViewModel.CanStopWaiting),
                nameof(CleanupViewModel.StopWaitingName), nameof(CleanupViewModel.OperationProgressName)],
            raised);
        Assert.Equal(1, buttonChecks);
    }

    [Fact]
    public async Task A_wait_and_its_end_each_move_the_button_once()
    {
        // Every change the button is raised with, from the Move's first heading to its last.
        var vm = await ScannedViewModelAsync();
        var moves = new ConcurrentQueue<bool>();
        vm.Cleanup.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName == nameof(CleanupViewModel.CanStopWaiting)) moves.Enqueue(vm.Cleanup.CanStopWaiting);
        };
        CheckReports(
            (p, _) => ShowAsync(p, ScanProgressUpdate.Waiting(new SourceFolderWait("D:", () => { }))),
            (p, _) => ShowAsync(p, ScanProgressUpdate.Waiting(null)));

        await MoveAsync(vm);

        Assert.Equal([true, false], moves);
    }
}
