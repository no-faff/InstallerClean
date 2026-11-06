using System.IO.Abstractions;
using System.IO.Abstractions.TestingHelpers;
using InstallerClean.Tests.Helpers;
using NSubstitute;
using InstallerClean.Helpers;
using InstallerClean.Models;
using InstallerClean.Services;
using InstallerClean.ViewModels;

namespace InstallerClean.Tests.ViewModels;

/// <summary>
/// The Move pre-flight: the folder-creation-and-write-probe pass that runs
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
public class CleanupPreFlightTests : IDisposable
{
    // Every specification of MoveFilesAsync below names the trailing under-lease
    // argument, and the DidNotReceive ones are why it matters most: the argument
    // is optional on the interface and never omitted in practice, so leaving it
    // out matches a literal null no real call can carry, and a negative asserted
    // that way passes whether the service was called or not.
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
    private readonly IEarlierRunCheck _earlierRunCheck = Substitute.For<IEarlierRunCheck>();
    private readonly IFirstRunMark _firstRunMark = FirstRunMarks.Claiming();
    private readonly IWindowsRegion _windowsRegion = Substitute.For<IWindowsRegion>();

    private readonly IFileSystem _fileSystem = Substitute.For<IFileSystem>();
    private readonly IDirectory _directory = Substitute.For<IDirectory>();
    private readonly IFile _file = Substitute.For<IFile>();

    private readonly IUpdateCheckService _updateCheckService = Substitute.For<IUpdateCheckService>();

    private readonly string _destination = Path.Combine(Path.GetTempPath(), "ic-test-preflight");

    public CleanupPreFlightTests()
    {
        _settingsService.Load().Returns(new AppSettings());
        // A working settings service: a destination save that does land records nothing.
        _settingsService.Update(Arg.Any<Action<AppSettings>>()).Returns(true);
        // Check() returns Clean or Block, never null (the interface contract);
        // default it Clean so the act-time pending-reboot re-check proceeds.
        _rebootService.Check().Returns(PendingRebootResult.Clean);
        // No-op re-verify: everything survives, nothing dropped.
        _reverifier.ReverifyAsync(Arg.Any<IReadOnlyList<string>>(), Arg.Any<CancellationToken>(), Arg.Any<IProgress<ScanProgressUpdate>?>())
            .Returns(ci => new ReverifyResult((IReadOnlyList<string>)ci[0]!, Array.Empty<string>()));
        // MockFileSystem's Path is a working implementation; a bare substitute
        // returns an empty string from Combine and GetRandomFileName.
        _fileSystem.Path.Returns(new MockFileSystem().Path);
        _fileSystem.Directory.Returns(_directory);
        _fileSystem.File.Returns(_file);
        // The destination's parent is a folder that is there, so the destination is the
        // only folder a Move to it makes.
        _directory.Exists(Path.GetDirectoryName(_destination)!).Returns(true);

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
        _made.Add(new MainViewModel(_scanService, _moveService, _deleteService,
            _settingsService, _rebootService, _msiInfoService,
            _dialogService, _confirmationService, _windowService,
            _fileSystem, _resultLogService, _updateCheckService, _reverifier,
            _earlierRunCheck, _firstRunMark, _windowsRegion,
            destinationGuard: new ByPathDestinationGuard(_fileSystem)));

    /// <summary>The view models this class made, flushed after each test.</summary>
    private readonly MadeViewModels _made = new();

    public void Dispose() => _made.Dispose();

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

    /// <summary>
    /// Every <c>Delete</c> the Move has made on the substituted directory, one entry of
    /// arguments for each, whichever overload made it. The probe's own file delete goes to
    /// the substituted file, so it is not in this list.
    /// </summary>
    private List<object?[]> FolderRemovals() =>
        _directory.ReceivedCalls()
            .Where(call => call.GetMethodInfo().Name == nameof(IDirectory.Delete))
            .Select(call => call.GetArguments())
            .ToList();

    /// <summary>
    /// The Move called <c>Delete</c> on the substituted directory once, for the folder it
    /// made and not recursively. The two-argument overload carries the recursive flag as its
    /// second argument.
    /// </summary>
    private void AssertFolderRemoved()
    {
        var removals = FolderRemovals();
        Assert.True(removals.Count == 1,
            $"expected one Delete on the directory, but there were {removals.Count}{Listed(removals)}");
        var removal = removals[0];
        Assert.True(removal[0] as string == _destination, $"the Delete was for {removal[0]}, not the destination");
        Assert.True(removal.Length == 1 || removal[1] is false, "the folder was removed recursively");
    }

    /// <summary>
    /// The Move called <c>Delete</c> on the substituted directory once for each of
    /// <paramref name="innermostFirst"/>, in that order, and not recursively.
    /// </summary>
    private void AssertFoldersRemoved(params string[] innermostFirst)
    {
        var removals = FolderRemovals();
        Assert.True(removals.Count == innermostFirst.Length,
            $"expected {innermostFirst.Length} Delete calls on the directory, but there were {removals.Count}{Listed(removals)}");
        for (var i = 0; i < innermostFirst.Length; i++)
        {
            Assert.True(removals[i][0] as string == innermostFirst[i],
                $"Delete number {i + 1} was for {removals[i][0]}, not {innermostFirst[i]}");
            Assert.True(removals[i].Length == 1 || removals[i][1] is false, "a folder was removed recursively");
        }
    }

    /// <summary>The Move made no <c>Delete</c> call on the substituted directory.</summary>
    private void AssertNoFolderRemoved()
    {
        var removals = FolderRemovals();
        Assert.True(removals.Count == 0,
            $"expected no Delete on the directory, but there {(removals.Count == 1 ? "was" : "were")} {removals.Count}{Listed(removals)}");
    }

    /// <summary>The <c>Delete</c> calls in <paramref name="removals"/>, written out for a failure message.</summary>
    private static string Listed(List<object?[]> removals) =>
        removals.Count == 0
            ? string.Empty
            : ": " + string.Join("; ", removals.Select(arguments => $"Delete({string.Join(", ", arguments)})"));

    [Fact]
    public void The_removal_assertion_accepts_the_one_argument_Delete_of_the_destination()
    {
        _directory.Delete(_destination);

        AssertFolderRemoved();
    }

    [Fact]
    public void The_removal_assertion_accepts_the_non_recursive_two_argument_Delete_of_the_destination()
    {
        _directory.Delete(_destination, false);

        AssertFolderRemoved();
    }

    [Fact]
    public void The_removal_assertion_rejects_a_recursive_Delete_of_the_destination()
    {
        _directory.Delete(_destination, true);

        var failure = Assert.Throws<Xunit.Sdk.TrueException>(AssertFolderRemoved);
        Assert.Contains("removed recursively", failure.Message);
    }

    [Fact]
    public void The_removal_assertion_rejects_no_removal()
    {
        var failure = Assert.Throws<Xunit.Sdk.TrueException>(AssertFolderRemoved);
        Assert.Contains("there were 0", failure.Message);
    }

    [Fact]
    public void The_removal_assertion_rejects_a_second_removal()
    {
        _directory.Delete(_destination);
        _directory.Delete(_destination);

        var failure = Assert.Throws<Xunit.Sdk.TrueException>(AssertFolderRemoved);
        Assert.Contains("there were 2", failure.Message);
        Assert.Contains($"Delete({_destination}); Delete({_destination})", failure.Message);
    }

    [Fact]
    public void The_removal_assertion_rejects_the_removal_of_a_folder_named_from_the_destination()
    {
        _directory.Delete(_destination + "-other");

        var failure = Assert.Throws<Xunit.Sdk.TrueException>(AssertFolderRemoved);
        Assert.Contains("not the destination", failure.Message);
    }

    [Fact]
    public void The_removal_assertion_rejects_the_removal_of_an_unrelated_folder()
    {
        _directory.Delete(Path.Combine(Path.GetTempPath(), "ic-test-unrelated"));

        var failure = Assert.Throws<Xunit.Sdk.TrueException>(AssertFolderRemoved);
        Assert.Contains("not the destination", failure.Message);
    }

    [Fact]
    public void The_removal_assertions_count_only_Delete_on_the_directory()
    {
        _directory.Exists(_destination);
        _directory.CreateDirectory(_destination);
        _file.Delete(Path.Combine(_destination, "probe"));
        AssertNoFolderRemoved();

        _directory.Delete(_destination);
        AssertFolderRemoved();
    }

    [Fact]
    public void The_no_removal_assertion_accepts_none_and_rejects_either_overload()
    {
        AssertNoFolderRemoved();

        _directory.Delete(_destination);
        var oneArgument = Assert.Throws<Xunit.Sdk.TrueException>(AssertNoFolderRemoved);
        Assert.Contains($"there was 1: Delete({_destination})", oneArgument.Message);

        _directory.ClearReceivedCalls();
        _directory.Delete(_destination, false);
        var twoArguments = Assert.Throws<Xunit.Sdk.TrueException>(AssertNoFolderRemoved);
        Assert.Contains($"there was 1: Delete({_destination}, False)", twoArguments.Message);
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
            Arg.Any<IEnumerable<string>>(), Arg.Any<string>(), Arg.Any<UnderLeaseClaims>(),
            Arg.Any<IProgress<OperationProgress>?>(), Arg.Any<CancellationToken>());

        // IsCancellationRequested is what gates the Cancel button, so a
        // pre-flight that cancels without clearing it leaves Cancel and Esc
        // dead for the whole of the next operation.
        Assert.False(vm.Cleanup.IsCancellationRequested);

        var releaseMove = new ManualResetEventSlim();
        _confirmationService.ConfirmMove(Arg.Any<int>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<bool>())
            .Returns(true);
        _moveService.MoveFilesAsync(
                Arg.Any<IEnumerable<string>>(), Arg.Any<string>(), Arg.Any<UnderLeaseClaims>(),
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
        // A pre-flight that checked the token only between its filesystem calls
        // would honour a Cancel this late too late: the user who cancelled
        // still gets the "move 1 file?" dialog, and only the move behind it
        // fails on the already-cancelled token.
        _confirmationService.DidNotReceive().ConfirmMove(
            Arg.Any<int>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<bool>());
        await _moveService.DidNotReceive().MoveFilesAsync(
            Arg.Any<IEnumerable<string>>(), Arg.Any<string>(), Arg.Any<UnderLeaseClaims>(),
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
            Arg.Any<int>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<bool>());
        await _moveService.DidNotReceive().MoveFilesAsync(
            Arg.Any<IEnumerable<string>>(), Arg.Any<string>(), Arg.Any<UnderLeaseClaims>(),
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
            Arg.Any<int>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<bool>());
        await _moveService.DidNotReceive().MoveFilesAsync(
            Arg.Any<IEnumerable<string>>(), Arg.Any<string>(), Arg.Any<UnderLeaseClaims>(),
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
            Arg.Any<IEnumerable<string>>(), Arg.Any<string>(), Arg.Any<UnderLeaseClaims>(),
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
    public async Task The_Details_buttons_are_out_of_use_while_a_pre_flight_runs()
    {
        var releaseProbe = new ManualResetEventSlim();
        _directory.When(d => d.CreateDirectory(Arg.Any<string>())).Do(_ => releaseProbe.Wait());

        var vm = CreateViewModel();
        await vm.Scan.ScanWithProgressAsync(null);
        vm.Cleanup.MoveDestination = _destination;
        Assert.True(vm.Chrome.OpenOrphanedDetailsCommand.CanExecute(null));
        Assert.True(vm.Chrome.OpenRegisteredDetailsCommand.CanExecute(null));

        var move = vm.Cleanup.MoveAllCommand.ExecuteAsync(null);

        // Inside the 200 ms before the operating overlay would appear, with the
        // main window still taking clicks. The Move ends by replacing the result,
        // so a Details window opened now would show the set from before it.
        Assert.True(vm.Cleanup.IsOperationInFlight);
        Assert.False(vm.Cleanup.IsOperating);
        Assert.False(vm.Chrome.OpenOrphanedDetailsCommand.CanExecute(null));
        Assert.False(vm.Chrome.OpenRegisteredDetailsCommand.CanExecute(null));

        releaseProbe.Set();
        await move;
    }

    [Fact]
    public async Task A_language_switch_is_refused_while_a_move_pre_flight_is_in_flight()
    {
        var releaseProbe = new ManualResetEventSlim();
        _directory.When(d => d.CreateDirectory(Arg.Any<string>())).Do(_ => releaseProbe.Wait());

        var vm = CreateViewModel();
        await vm.Scan.ScanWithProgressAsync(null);
        vm.Cleanup.MoveDestination = _destination;

        var move = vm.Cleanup.MoveAllCommand.ExecuteAsync(null);

        // Provably inside the pre-flight: IsOperationInFlight is set but the
        // operating overlay is not yet up, so the disabled-nav mechanism that
        // normally blocks the globe does NOT catch this window. IsBusy does.
        Assert.True(vm.IsBusy);
        Assert.False(vm.Cleanup.IsOperating);

        // A relaunch here would tear the process down under the running operation,
        // exactly like closing the window mid-move. It must be refused.
        vm.Chrome.SetLanguageCommand.Execute("fr");
        _windowService.DidNotReceive().RelaunchForLanguageChange();

        releaseProbe.Set();
        await move;
    }

    [Fact]
    public async Task The_pre_flight_creates_and_probes_the_destination_before_the_confirmation()
    {
        _confirmationService.ConfirmMove(Arg.Any<int>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<bool>())
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
        _confirmationService.Received(1).ConfirmMove(1, Arg.Any<string>(), _destination, Arg.Any<bool>());
    }

    [Fact]
    public async Task Browse_starts_at_the_folder_the_box_already_names()
    {
        var vm = CreateViewModel();
        await vm.Scan.ScanWithProgressAsync(null);
        vm.Cleanup.MoveDestination = _destination;

        vm.Cleanup.BrowseDestinationCommand.Execute(null);

        // Browse is pressed to CHANGE a destination more often than to set the
        // first one, so the browser opens where the box points rather than
        // making the user navigate back to a path already on screen. Whether
        // that path still exists is the service's call, not the view-model's.
        _confirmationService.Received(1).AskForMoveDestination(_destination);
    }

    [Fact]
    public async Task Cancelling_the_confirmation_removes_the_folder_the_pre_flight_created()
    {
        // Exists is unconfigured, so it answers false: the pre-flight is the
        // one that brings the folder into existence.
        _confirmationService.ConfirmMove(Arg.Any<int>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<bool>())
            .Returns(false);

        var vm = CreateViewModel();
        await vm.Scan.ScanWithProgressAsync(null);
        vm.Cleanup.MoveDestination = _destination;

        await vm.Cleanup.MoveAllCommand.ExecuteAsync(null);

        // A fully cancelled Move leaves nothing behind. Non-recursive, so a
        // folder that gained a file meanwhile refuses at the syscall.
        AssertFolderRemoved();
    }

    [Fact]
    public async Task A_pending_reboot_at_act_time_removes_the_folder_the_pre_flight_created()
    {
        // Clean through the scan and the pre-flight, blocked from the moment the
        // confirmation is answered: an install that started while the dialog sat
        // open, which is the exact condition the act-time re-check exists to
        // catch. Blocking from the start instead would fail the scan-side gate
        // and never reach this path. The move does not go ahead and nothing was
        // placed in the folder, so the clean-up rule is the cancel's.
        var confirmed = false;
        _confirmationService.ConfirmMove(Arg.Any<int>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<bool>())
            .Returns(_ => { confirmed = true; return true; });
        _rebootService.Check().Returns(_ => confirmed
            ? PendingRebootResult.Block(PendingRebootReason.MsiExecuteMutexHeld)
            : PendingRebootResult.Clean);

        var vm = CreateViewModel();
        await vm.Scan.ScanWithProgressAsync(null);
        vm.Cleanup.MoveDestination = _destination;

        await vm.Cleanup.MoveAllCommand.ExecuteAsync(null);

        AssertFolderRemoved();
        await _moveService.DidNotReceive().MoveFilesAsync(
            Arg.Any<IEnumerable<string>>(), Arg.Any<string>(), Arg.Any<UnderLeaseClaims>(),
            Arg.Any<IProgress<OperationProgress>?>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task A_failed_write_probe_removes_the_folder_it_had_just_created()
    {
        // The probe's own failure arm, which never receives a pre-flight record
        // at all: CreateDirectory succeeded and the write probe then threw, so
        // by that point the folder exists, the move is abandoned and the user
        // gets the write-failure dialog. The removal therefore has to run off
        // the caller's captured list of created folders rather than off a record.
        _file.When(f => f.WriteAllBytes(Arg.Any<string>(), Arg.Any<byte[]>()))
            .Do(_ => throw new UnauthorizedAccessException("probe refused"));

        var vm = CreateViewModel();
        await vm.Scan.ScanWithProgressAsync(null);
        vm.Cleanup.MoveDestination = _destination;

        await vm.Cleanup.MoveAllCommand.ExecuteAsync(null);

        AssertFolderRemoved();
        _confirmationService.DidNotReceive().ConfirmMove(
            Arg.Any<int>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<bool>());
    }

    [Fact]
    public async Task A_folder_that_will_not_let_the_probe_file_be_read_back_is_refused_with_its_own_message()
    {
        _file.ReadAllBytes(Arg.Any<string>()).Returns(_ => throw new UnauthorizedAccessException("read refused"));

        var vm = CreateViewModel();
        await vm.Scan.ScanWithProgressAsync(null);
        vm.Cleanup.MoveDestination = _destination;

        await vm.Cleanup.MoveAllCommand.ExecuteAsync(null);

        _dialogService.Received(1).ShowWarning(
            string.Format(InstallerClean.Resources.Strings.Error_FolderWillNotGiveFilesBack, _destination),
            InstallerClean.Resources.Strings.Error_DestinationWriteFailedTitle);
        AssertFolderRemoved();
        _confirmationService.DidNotReceive().ConfirmMove(
            Arg.Any<int>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<bool>());
        await _moveService.DidNotReceive().MoveFilesAsync(
            Arg.Any<IEnumerable<string>>(), Arg.Any<string>(), Arg.Any<UnderLeaseClaims>(),
            Arg.Any<IProgress<OperationProgress>?>(), Arg.Any<CancellationToken>());
        Assert.False(vm.Cleanup.IsOperating);
        Assert.Equal(string.Empty, vm.Cleanup.OperationProgress);
        // A refusal, not a fault: the test's crash log does not name its folder or carry
        // its refusal.
        var log = TestCrashLog.Text();
        Assert.DoesNotContain(_destination, log, StringComparison.Ordinal);
        Assert.DoesNotContain("read refused", log, StringComparison.Ordinal);
    }

    [Fact]
    public async Task A_read_back_that_fails_for_another_cause_is_refused_as_a_write_failure()
    {
        // A failure of the read that is not a refusal of access, such as another
        // program holding the new file, says nothing about the folder's permissions,
        // so it takes the write-failure dialog and not the message for a folder that
        // will not give files back.
        _file.ReadAllBytes(Arg.Any<string>())
            .Returns(_ => throw new IOException("sharing violation", unchecked((int)0x80070020)));

        var vm = CreateViewModel();
        await vm.Scan.ScanWithProgressAsync(null);
        vm.Cleanup.MoveDestination = _destination;

        await vm.Cleanup.MoveAllCommand.ExecuteAsync(null);

        _dialogService.Received(1).ShowWarning(
            string.Format(
                InstallerClean.Resources.Strings.Error_IOWriteDestination, _destination, CrashLog.LogPath),
            InstallerClean.Resources.Strings.Error_DestinationWriteFailedTitle);
        AssertFolderRemoved();
        _confirmationService.DidNotReceive().ConfirmMove(
            Arg.Any<int>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<bool>());
        // A failure with no established cause is a fault, so the crash log takes an entry: the
        // write failure the pre-flight raises for this destination.
        Assert.Contains(
            string.Format(InstallerClean.Resources.Strings.Error_CannotWriteFolder, _destination),
            TestCrashLog.Text(), StringComparison.Ordinal);
        // The cause the entry carries is the Windows error the refusal stood for (32, sharing violation).
        Assert.Contains(new System.ComponentModel.Win32Exception(32).Message, TestCrashLog.Text(), StringComparison.Ordinal);
    }

    [Fact]
    public async Task A_folder_that_refuses_the_account_the_probe_file_shows_the_access_denied_message()
    {
        _file.When(f => f.WriteAllBytes(Arg.Any<string>(), Arg.Any<byte[]>()))
            .Do(_ => throw new UnauthorizedAccessException("create refused"));

        var vm = CreateViewModel();
        await vm.Scan.ScanWithProgressAsync(null);
        vm.Cleanup.MoveDestination = _destination;

        await vm.Cleanup.MoveAllCommand.ExecuteAsync(null);

        _dialogService.Received(1).ShowWarning(
            string.Format(InstallerClean.Resources.Strings.Error_AccessDeniedDestination, _destination),
            InstallerClean.Resources.Strings.Error_DestinationWriteFailedTitle);
        AssertFolderRemoved();
        _confirmationService.DidNotReceive().ConfirmMove(
            Arg.Any<int>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<bool>());
        // A refusal, not a fault: the test's crash log does not name its folder or carry
        // its refusal.
        var log = TestCrashLog.Text();
        Assert.DoesNotContain(_destination, log, StringComparison.Ordinal);
        Assert.DoesNotContain("create refused", log, StringComparison.Ordinal);
    }

    [Fact]
    public async Task A_probe_file_that_cannot_be_added_for_another_cause_is_refused_as_a_write_failure()
    {
        _file.When(f => f.WriteAllBytes(Arg.Any<string>(), Arg.Any<byte[]>()))
            .Do(_ => throw new IOException("disk full", unchecked((int)0x80070070)));

        var vm = CreateViewModel();
        await vm.Scan.ScanWithProgressAsync(null);
        vm.Cleanup.MoveDestination = _destination;

        await vm.Cleanup.MoveAllCommand.ExecuteAsync(null);

        _dialogService.Received(1).ShowWarning(
            string.Format(
                InstallerClean.Resources.Strings.Error_IOWriteDestination, _destination, CrashLog.LogPath),
            InstallerClean.Resources.Strings.Error_DestinationWriteFailedTitle);
        AssertFolderRemoved();
        _confirmationService.DidNotReceive().ConfirmMove(
            Arg.Any<int>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<bool>());
        // A failure with no established cause is a fault, so the crash log takes an entry: the
        // write failure the pre-flight raises for this destination.
        Assert.Contains(
            string.Format(InstallerClean.Resources.Strings.Error_CannotWriteFolder, _destination),
            TestCrashLog.Text(), StringComparison.Ordinal);
        // The cause the entry carries is the Windows error the refusal stood for (112, disk full).
        Assert.Contains(new System.ComponentModel.Win32Exception(112).Message, TestCrashLog.Text(), StringComparison.Ordinal);
    }

    [Fact]
    public async Task A_folder_the_account_is_refused_when_creating_it_shows_the_access_denied_message_and_no_crash_log()
    {
        _directory.When(d => d.CreateDirectory(Arg.Any<string>()))
            .Do(_ => throw new UnauthorizedAccessException("create refused"));

        var vm = CreateViewModel();
        await vm.Scan.ScanWithProgressAsync(null);
        vm.Cleanup.MoveDestination = _destination;

        await vm.Cleanup.MoveAllCommand.ExecuteAsync(null);

        _dialogService.Received(1).ShowWarning(
            string.Format(InstallerClean.Resources.Strings.Error_AccessDeniedDestination, _destination),
            InstallerClean.Resources.Strings.Error_DestinationWriteFailedTitle);
        // The create made no folder, so there is none to remove.
        AssertNoFolderRemoved();
        _confirmationService.DidNotReceive().ConfirmMove(
            Arg.Any<int>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<bool>());
        await _moveService.DidNotReceive().MoveFilesAsync(
            Arg.Any<IEnumerable<string>>(), Arg.Any<string>(), Arg.Any<UnderLeaseClaims>(),
            Arg.Any<IProgress<OperationProgress>?>(), Arg.Any<CancellationToken>());
        Assert.False(vm.Cleanup.IsOperating);
        // A refusal, not a fault: the test's crash log does not name its folder or carry
        // its refusal.
        var log = TestCrashLog.Text();
        Assert.DoesNotContain(_destination, log, StringComparison.Ordinal);
        Assert.DoesNotContain("create refused", log, StringComparison.Ordinal);
    }

    [Fact]
    public async Task A_folder_that_cannot_be_created_because_its_drive_is_missing_shows_its_message_and_no_crash_log()
    {
        _directory.When(d => d.CreateDirectory(Arg.Any<string>()))
            .Do(_ => throw new DirectoryNotFoundException("no such drive"));

        var vm = CreateViewModel();
        await vm.Scan.ScanWithProgressAsync(null);
        vm.Cleanup.MoveDestination = _destination;

        await vm.Cleanup.MoveAllCommand.ExecuteAsync(null);

        _dialogService.Received(1).ShowWarning(
            string.Format(InstallerClean.Resources.Strings.Error_DestinationMissing, _destination),
            InstallerClean.Resources.Strings.Error_DestinationWriteFailedTitle);
        AssertNoFolderRemoved();
        _confirmationService.DidNotReceive().ConfirmMove(
            Arg.Any<int>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<bool>());
        await _moveService.DidNotReceive().MoveFilesAsync(
            Arg.Any<IEnumerable<string>>(), Arg.Any<string>(), Arg.Any<UnderLeaseClaims>(),
            Arg.Any<IProgress<OperationProgress>?>(), Arg.Any<CancellationToken>());
        Assert.False(vm.Cleanup.IsOperating);
        // A mistake the message explains whole: the crash log is left as it was.
        // The test's crash log does not name its folder or carry its refusal.
        var log = TestCrashLog.Text();
        Assert.DoesNotContain(_destination, log, StringComparison.Ordinal);
        Assert.DoesNotContain("no such drive", log, StringComparison.Ordinal);
    }

    [Fact]
    public async Task A_refused_create_removes_the_folder_above_the_destination_that_the_move_made()
    {
        var outer = Path.Combine(Path.GetTempPath(), "ic-test-new");
        var inner = Path.Combine(outer, "inner");
        _directory.When(d => d.CreateDirectory(inner))
            .Do(_ => throw new UnauthorizedAccessException("create refused"));

        var vm = CreateViewModel();
        await vm.Scan.ScanWithProgressAsync(null);
        vm.Cleanup.MoveDestination = inner;

        await vm.Cleanup.MoveAllCommand.ExecuteAsync(null);

        AssertFoldersRemoved(outer);
    }

    [Fact]
    public async Task A_refused_create_of_the_only_missing_folder_removes_nothing()
    {
        var existing = Path.Combine(Path.GetTempPath(), "ic-test-existing");
        var inner = Path.Combine(existing, "inner");
        _directory.Exists(existing).Returns(true);
        _directory.When(d => d.CreateDirectory(Arg.Any<string>()))
            .Do(_ => throw new UnauthorizedAccessException("create refused"));

        var vm = CreateViewModel();
        await vm.Scan.ScanWithProgressAsync(null);
        vm.Cleanup.MoveDestination = inner;

        await vm.Cleanup.MoveAllCommand.ExecuteAsync(null);

        AssertNoFolderRemoved();
    }

    [Fact]
    public async Task A_folder_that_will_not_go_stops_the_removal_of_the_folders_above_it()
    {
        var top = Path.Combine(Path.GetTempPath(), "ic-test-new");
        var middle = Path.Combine(top, "middle");
        var inner = Path.Combine(middle, "inner");
        // The middle folder is missing when the pre-flight looks for the folders to make
        // and when it creates it, and is there when the removal reaches it, holding
        // something its removal refuses.
        _directory.Exists(middle).Returns(false, false, true);
        _directory.When(d => d.Delete(middle)).Do(_ => throw new IOException("not empty"));
        _directory.When(d => d.CreateDirectory(inner))
            .Do(_ => throw new UnauthorizedAccessException("create refused"));

        var vm = CreateViewModel();
        await vm.Scan.ScanWithProgressAsync(null);
        vm.Cleanup.MoveDestination = inner;

        await vm.Cleanup.MoveAllCommand.ExecuteAsync(null);

        AssertFoldersRemoved(middle);
    }

    [Fact]
    public async Task Cancelling_the_confirmation_leaves_a_folder_that_was_already_there()
    {
        _directory.Exists(_destination).Returns(true);
        _confirmationService.ConfirmMove(Arg.Any<int>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<bool>())
            .Returns(false);

        var vm = CreateViewModel();
        await vm.Scan.ScanWithProgressAsync(null);
        vm.Cleanup.MoveDestination = _destination;

        await vm.Cleanup.MoveAllCommand.ExecuteAsync(null);

        // The user's own folder, whatever is in it. Cancelling a move they
        // never confirmed must not reach into it.
        AssertNoFolderRemoved();
    }

    [Fact]
    public async Task A_confirmed_move_keeps_the_folder_the_pre_flight_created()
    {
        _confirmationService.ConfirmMove(Arg.Any<int>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<bool>())
            .Returns(true);
        _moveService.MoveFilesAsync(
                Arg.Any<IEnumerable<string>>(), Arg.Any<string>(), Arg.Any<UnderLeaseClaims>(),
                Arg.Any<IProgress<OperationProgress>?>(), Arg.Any<CancellationToken>())
            .Returns(new MoveResult(1, Array.Empty<FileOperationError>()));

        var vm = CreateViewModel();
        await vm.Scan.ScanWithProgressAsync(null);
        vm.Cleanup.MoveDestination = _destination;

        await vm.Cleanup.MoveAllCommand.ExecuteAsync(null);

        // The removal is scoped to the cancel; the files went here.
        AssertNoFolderRemoved();
    }

    [Fact]
    public async Task An_installer_taking_the_lock_at_the_service_removes_the_folder_the_pre_flight_created()
    {
        // The race the act-time gate cannot close: the gate passed, then a
        // transaction took Global\_MSIExecute before the service acquired it. The
        // service refuses ahead of its own CreateDestinationFolder, so nothing was
        // placed and the folder goes.
        _confirmationService.ConfirmMove(Arg.Any<int>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<bool>())
            .Returns(true);
        _moveService.MoveFilesAsync(
                Arg.Any<IEnumerable<string>>(), Arg.Any<string>(), Arg.Any<UnderLeaseClaims>(),
                Arg.Any<IProgress<OperationProgress>?>(), Arg.Any<CancellationToken>())
            .Returns(new MoveResult(0, Array.Empty<FileOperationError>(), InstallerBusy: true));

        var vm = CreateViewModel();
        await vm.Scan.ScanWithProgressAsync(null);
        vm.Cleanup.MoveDestination = _destination;

        await vm.Cleanup.MoveAllCommand.ExecuteAsync(null);

        AssertFolderRemoved();
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task A_gate_re_check_that_throws_after_a_lock_refusal_still_removes_the_folder_the_pre_flight_created(
        bool accessRefused)
    {
        // The two arms that re-run the gate after the service refused at the lock,
        // with the re-check throwing, as the gate does when a read of Windows
        // Installer's in-progress file fails in a way none of its readings names.
        // The service refused ahead of its own CreateDestinationFolder, so the
        // pre-flight's folder is empty and goes, the failure dialog says what
        // happened, and nothing moved, so there is no rescan. A refused lock has
        // its own dialog first, and the failure dialog follows it.
        var refused = false;
        _rebootService.Check().Returns(_ => refused
            ? throw new IOException("marker unreadable")
            : PendingRebootResult.Clean);
        _confirmationService.ConfirmMove(Arg.Any<int>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<bool>())
            .Returns(true);
        _moveService.MoveFilesAsync(
                Arg.Any<IEnumerable<string>>(), Arg.Any<string>(), Arg.Any<UnderLeaseClaims>(),
                Arg.Any<IProgress<OperationProgress>?>(), Arg.Any<CancellationToken>())
            .Returns(_ =>
            {
                refused = true;
                return new MoveResult(0, Array.Empty<FileOperationError>(),
                    InstallerBusy: !accessRefused, InstallerLockAccessRefused: accessRefused);
            });

        var vm = CreateViewModel();
        await vm.Scan.ScanWithProgressAsync(null);
        vm.Cleanup.MoveDestination = _destination;

        await vm.Cleanup.MoveAllCommand.ExecuteAsync(null);

        AssertFolderRemoved();
        _dialogService.Received(accessRefused ? 2 : 1).ShowWarning(Arg.Any<string>(), Arg.Any<string>());
        _dialogService.Received(1).ShowWarning(
            Arg.Is<string>(s => s.Contains(nameof(IOException))),
            InstallerClean.Resources.Strings.Error_MoveFailedTitle);
        if (accessRefused)
        {
            Received.InOrder(() =>
            {
                _dialogService.ShowWarning(
                    InstallerClean.Resources.Strings.Error_MoveInstallerLockAccessRefused,
                    InstallerClean.Resources.Strings.Error_MoveInstallerLockUnavailableTitle);
                _dialogService.ShowWarning(
                    Arg.Any<string>(), InstallerClean.Resources.Strings.Error_MoveFailedTitle);
            });
        }
        await _scanService.Received(1).ScanAsync(
            Arg.Any<IProgress<ScanProgressUpdate>?>(), Arg.Any<CancellationToken>());
        Assert.False(vm.Cleanup.IsOperating);
        Assert.Equal(string.Empty, vm.Cleanup.OperationProgress);
        Assert.True(vm.Cleanup.MoveAllCommand.CanExecute(null));
        Assert.True(vm.Cleanup.DeleteAllCommand.CanExecute(null));
    }

    [Fact]
    public async Task A_lock_the_service_could_not_take_removes_the_folder_the_pre_flight_created()
    {
        // The other way the service can refuse at its acquire: the mutex could not
        // be taken and nothing was shown to be holding it. It refuses ahead of its
        // own CreateDestinationFolder exactly as the busy arm above does, so the
        // only folder in existence is this pre-flight's and it has nothing in it.
        // Its own service-level test pins that the service creates none; this pins
        // that the host clears the one it made.
        _confirmationService.ConfirmMove(Arg.Any<int>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<bool>())
            .Returns(true);
        _moveService.MoveFilesAsync(
                Arg.Any<IEnumerable<string>>(), Arg.Any<string>(), Arg.Any<UnderLeaseClaims>(),
                Arg.Any<IProgress<OperationProgress>?>(), Arg.Any<CancellationToken>())
            .Returns(new MoveResult(0, Array.Empty<FileOperationError>(),
                InstallerLockUnavailable: true));

        var vm = CreateViewModel();
        await vm.Scan.ScanWithProgressAsync(null);
        vm.Cleanup.MoveDestination = _destination;

        await vm.Cleanup.MoveAllCommand.ExecuteAsync(null);

        AssertFolderRemoved();
    }

    [Fact]
    public async Task A_re_verify_that_keeps_every_file_back_removes_the_folder_the_pre_flight_created()
    {
        // A patch that was superseded at scan time but is wanted again by act
        // time: nothing survives the re-verify, so the batch never runs and the
        // folder the pre-flight made has nothing in it.
        _confirmationService.ConfirmMove(Arg.Any<int>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<bool>())
            .Returns(true);
        _reverifier.ReverifyAsync(Arg.Any<IReadOnlyList<string>>(), Arg.Any<CancellationToken>(), Arg.Any<IProgress<ScanProgressUpdate>?>())
            .Returns(ci => new ReverifyResult(
                Array.Empty<string>(), (IReadOnlyList<string>)ci[0]!,
                new HeldBackReasons(Reclaimed: ((IReadOnlyList<string>)ci[0]!).Count)));

        var vm = CreateViewModel();
        await vm.Scan.ScanWithProgressAsync(null);
        vm.Cleanup.MoveDestination = _destination;

        await vm.Cleanup.MoveAllCommand.ExecuteAsync(null);

        AssertFolderRemoved();
        await _moveService.DidNotReceive().MoveFilesAsync(
            Arg.Any<IEnumerable<string>>(), Arg.Any<string>(), Arg.Any<UnderLeaseClaims>(),
            Arg.Any<IProgress<OperationProgress>?>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task A_wait_the_check_before_a_Move_reports_takes_the_heading_and_gives_it_back()
    {
        // The check reports one wait on drive D: and its end. Nothing survives it, so the
        // batch never runs. The heading is read inside the check, once each report has
        // reached it.
        var waitLine = InstallerClean.Helpers.DisplayHelpers.WaitingFor("D:");
        string? duringWait = null;
        string? afterWait = null;
        MainViewModel? vm = null;
        _confirmationService.ConfirmMove(Arg.Any<int>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<bool>())
            .Returns(true);
        _reverifier.ReverifyAsync(Arg.Any<IReadOnlyList<string>>(), Arg.Any<CancellationToken>(), Arg.Any<IProgress<ScanProgressUpdate>?>())
            .Returns(async ci =>
            {
                var progress = (IProgress<ScanProgressUpdate>)ci[2]!;
                progress.Report(ScanProgressUpdate.Waiting(new SourceFolderWait("D:", () => { })));
                await WaitUntil(() => vm!.Cleanup.OperationProgress == waitLine);
                duringWait = vm!.Cleanup.OperationProgress;
                progress.Report(ScanProgressUpdate.Waiting(null));
                await WaitUntil(() => vm.Cleanup.OperationProgress != waitLine);
                afterWait = vm.Cleanup.OperationProgress;
                return new ReverifyResult(
                    Array.Empty<string>(), (IReadOnlyList<string>)ci[0]!,
                    new HeldBackReasons(Reclaimed: ((IReadOnlyList<string>)ci[0]!).Count));
            });

        vm = CreateViewModel();
        await vm.Scan.ScanWithProgressAsync(null);
        vm.Cleanup.MoveDestination = _destination;

        await vm.Cleanup.MoveAllCommand.ExecuteAsync(null);

        Assert.Equal(waitLine, duringWait);
        Assert.Equal(InstallerClean.Resources.Strings.Status_Moving, afterWait);
    }

    [Fact]
    public async Task Only_a_wait_the_scan_after_a_Move_reports_crosses_to_the_window_and_takes_the_heading_and_gives_it_back()
    {
        // The scan after the Move reports a milestone and a ticker update, as a scan does,
        // and then one wait on drive D: and its end. The Move runs on a stand-in for the
        // window's dispatcher, which keeps every scan update posted to it. The scan returns once
        // the dispatcher has run what it reported, as the window runs a scan's reports before
        // the code that awaits the scan.
        const string Milestone = "a milestone the scan reported";
        const string Ticker = "a ticker update the scan reported";
        var wait = ScanProgressUpdate.Waiting(new SourceFolderWait("D:", () => { }));
        var end = ScanProgressUpdate.Waiting(null);
        var headings = new System.Collections.Concurrent.ConcurrentQueue<string>();
        var dispatcher = new DispatcherStandIn();
        _confirmationService.ConfirmMove(Arg.Any<int>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<bool>())
            .Returns(true);
        _moveService.MoveFilesAsync(
                Arg.Any<IEnumerable<string>>(), Arg.Any<string>(), Arg.Any<UnderLeaseClaims>(),
                Arg.Any<IProgress<OperationProgress>?>(), Arg.Any<CancellationToken>())
            .Returns(new MoveResult(1, Array.Empty<FileOperationError>()));

        var vm = CreateViewModel();
        await vm.Scan.ScanWithProgressAsync(null);
        vm.Cleanup.MoveDestination = _destination;
        vm.Cleanup.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName == nameof(CleanupViewModel.OperationProgress))
                headings.Enqueue(vm.Cleanup.OperationProgress);
        };
        _scanService.ScanAsync(Arg.Any<IProgress<ScanProgressUpdate>?>(), Arg.Any<CancellationToken>())
            .Returns(async ci =>
            {
                var progress = (IProgress<ScanProgressUpdate>)ci[0]!;
                progress.Report(new ScanProgressUpdate(Milestone));
                progress.Report(new ScanProgressUpdate(Ticker, IsMilestone: false, Position: 1, Total: 2));
                progress.Report(wait);
                progress.Report(end);
                await dispatcher.IdleAsync();
                return new ScanResult(new List<OrphanedFile>(), Array.Empty<RegisteredPackage>(), 0);
            });

        Task move;
        var before = SynchronizationContext.Current;
        SynchronizationContext.SetSynchronizationContext(dispatcher);
        try
        {
            move = vm.Cleanup.MoveAllCommand.ExecuteAsync(null);
        }
        finally
        {
            SynchronizationContext.SetSynchronizationContext(before);
        }
        await move;

        Assert.Equal(new[] { wait, end }, dispatcher.Posted);
        Assert.Empty(dispatcher.Thrown);
        Assert.Contains(wait.Message, headings);
        Assert.Equal(
            InstallerClean.Resources.Strings.Status_Scanning,
            headings.SkipWhile(heading => heading != wait.Message).Skip(1).FirstOrDefault());
        Assert.DoesNotContain(Milestone, headings);
        Assert.DoesNotContain(Ticker, headings);
    }

    [Fact]
    public async Task A_batch_the_under_lease_re_read_empties_removes_the_folder_too()
    {
        // The service is careful to return before creating its destination, so a
        // batch its own re-read empties leaves no folder behind, and its own test
        // pins that. The folder that survives is the PRE-FLIGHT's, made before the
        // confirmation, and only this layer can remove it. Nothing was ever placed
        // in it: the service came straight back out.
        _confirmationService.ConfirmMove(Arg.Any<int>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<bool>())
            .Returns(true);
        _moveService.MoveFilesAsync(
                Arg.Any<IEnumerable<string>>(), Arg.Any<string>(), Arg.Any<UnderLeaseClaims>(),
                Arg.Any<IProgress<OperationProgress>?>(), Arg.Any<CancellationToken>())
            .Returns(new MoveResult(0, Array.Empty<FileOperationError>(),
                HeldBack: new[] { @"C:\Windows\Installer\a.msi" }));

        var vm = CreateViewModel();
        await vm.Scan.ScanWithProgressAsync(null);
        vm.Cleanup.MoveDestination = _destination;

        await vm.Cleanup.MoveAllCommand.ExecuteAsync(null);

        AssertFolderRemoved();
    }

    [Fact]
    public async Task A_re_verify_that_fails_removes_the_folder_the_pre_flight_created()
    {
        // The re-verify runs between the confirmation and the batch, so a throw
        // from it abandons the move with the folder still empty.
        _confirmationService.ConfirmMove(Arg.Any<int>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<bool>())
            .Returns(true);
        _reverifier.ReverifyAsync(Arg.Any<IReadOnlyList<string>>(), Arg.Any<CancellationToken>(), Arg.Any<IProgress<ScanProgressUpdate>?>())
            .Returns<ReverifyResult>(_ => throw new LocalisedInvalidOperationException("boom"));

        var vm = CreateViewModel();
        await vm.Scan.ScanWithProgressAsync(null);
        vm.Cleanup.MoveDestination = _destination;

        await vm.Cleanup.MoveAllCommand.ExecuteAsync(null);

        AssertFolderRemoved();
    }

    [Fact]
    public async Task A_destination_gate_refusing_after_the_pre_flight_leaves_the_folder_alone()
    {
        // The bare LocalisedInvalidOperationException is now only ever one of the
        // move service's destination gates, every one of which runs before the
        // per-file loop, so this arm is reached with nothing moved. It still
        // does not delete the folder, and the reason is the arm's own: the gates
        // that reach it fire precisely because the destination has just been
        // shown to resolve into the Installer folder or a system folder, and
        // deleting a directory at a path just proven to land somewhere
        // unexpected is the operation those guards exist to prevent. The
        // service's fully-qualified check is not among them: the window refuses
        // a relative destination before the batch is handed over.
        //
        // The mid-batch abort used to land here and no longer does. Its own twin
        // is the test below; the two are kept apart because they are now different
        // arms defending the same folder for entirely different reasons.
        _confirmationService.ConfirmMove(Arg.Any<int>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<bool>())
            .Returns(true);
        _moveService.MoveFilesAsync(
                Arg.Any<IEnumerable<string>>(), Arg.Any<string>(), Arg.Any<UnderLeaseClaims>(),
                Arg.Any<IProgress<OperationProgress>?>(), Arg.Any<CancellationToken>())
            .Returns<MoveResult>(_ => throw new LocalisedInvalidOperationException("swapped"));

        var vm = CreateViewModel();
        await vm.Scan.ScanWithProgressAsync(null);
        vm.Cleanup.MoveDestination = _destination;

        await vm.Cleanup.MoveAllCommand.ExecuteAsync(null);

        AssertNoFolderRemoved();
    }

    [Fact]
    public async Task A_move_that_stopped_inside_the_batch_keeps_the_folder_its_files_may_be_in()
    {
        // The load-bearing negative, now staging the exception that really does
        // come from inside the per-file loop. A destination swap detected
        // mid-batch throws MoveAbortedException carrying what the batch had
        // already done, so files may be sitting in the folder, and the
        // non-recursive Delete would only refuse at the syscall AFTER the app had
        // decided to try. A stray empty folder is the lesser outcome than any path
        // that reaches for a folder holding moved files.
        //
        // It is worth its own test rather than a rename of the one above: nothing
        // pinned this arm at all, and it pattern-matches the "nothing was placed"
        // family the removal helper documents, so a tidy-up adding the call here
        // would delete a folder holding the user's just-moved files with the
        // suite green.
        _confirmationService.ConfirmMove(Arg.Any<int>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<bool>())
            .Returns(true);
        _moveService.MoveFilesAsync(
                Arg.Any<IEnumerable<string>>(), Arg.Any<string>(), Arg.Any<UnderLeaseClaims>(),
                Arg.Any<IProgress<OperationProgress>?>(), Arg.Any<CancellationToken>())
            .Returns<MoveResult>(_ => throw new MoveAbortedException(
                "swapped", new MoveResult(1, Array.Empty<FileOperationError>()),
                @"E:\resolved-elsewhere", MoveAbortReason.ResolvesElsewhere));

        var vm = CreateViewModel();
        await vm.Scan.ScanWithProgressAsync(null);
        vm.Cleanup.MoveDestination = _destination;

        await vm.Cleanup.MoveAllCommand.ExecuteAsync(null);

        AssertNoFolderRemoved();
        // The batch's own account of itself still reaches the user, which is what
        // makes the folder worth keeping: it names files that are in it.
        Assert.True(vm.Completion.IsComplete);
        // And the card says the run stopped. This arm took the finished Move's
        // card, so the line under the summary told the reader to delete the folder
        // the dialog beside it had just told them to check, and the dialog is the
        // one that goes when it is dismissed. Asserted here rather than only on the
        // view model because it is the wiring that was wrong: the card was right
        // for a Move that reached the end.
        Assert.Equal(
            string.Format(InstallerClean.Resources.Strings.Error_DestinationChangedMidBatch, @"E:\resolved-elsewhere"),
            vm.Completion.Restore);
    }

    /// <summary>
    /// Pins that a destination on the cache's own volume reaches the confirmation
    /// as a same-drive move, on a host where the cache folder's path root and the
    /// cache's own volume are the same string.
    /// </summary>
    [Fact]
    public async Task A_move_to_the_same_drive_tells_the_confirmation_it_frees_no_space()
    {
        // Built from a real volume rather than %TEMP%, because
        // MoveDestinationClassifier.Classify asks Windows which volume the destination is
        // on and compares that against the installer cache's: a move to the
        // volume the cache is already on is a rename, and frees nothing until
        // the user deletes the copies parked there. The dialog is the last
        // moment anyone can tell them that, and it is the whole point of the
        // app.
        //
        // THE CACHE FOLDER'S PATH ROOT, WHICH IS NOT THE CACHE'S VOLUME, and it
        // was the system directory's path root until the comparison stopped
        // being about the system directory. The name says path root because the
        // two part on the machine this whole change is for.
        //
        // THE ASSERTION IS WRITTEN FOR A ROOT AND A CACHE VOLUME THAT ARE THE
        // SAME STRING, which is the shape of a machine with nothing mounted
        // between them. That is the condition it holds under and it is why the
        // fixture takes the path root.
        //
        // AND THE FIXTURE IS NOT BUILT UNDER THE CACHE FOLDER, which is the
        // other way to name a destination on the cache's own volume. Move
        // refuses any destination resolving inside the installer cache, on the
        // first of the pre-flight's two path gates, so such a fixture is
        // rejected before the classification this test is about is reached.
        var cachePathRoot = Path.GetPathRoot(InstallerCacheHelpers.InstallerFolder)!;
        var sameDriveDestination = Path.Combine(cachePathRoot, "ic-test-samedrive");
        _confirmationService.ConfirmMove(
            Arg.Any<int>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<bool>())
            .Returns(false);

        var vm = CreateViewModel();
        await vm.Scan.ScanWithProgressAsync(null);
        vm.Cleanup.MoveDestination = sameDriveDestination;

        await vm.Cleanup.MoveAllCommand.ExecuteAsync(null);

        _confirmationService.Received(1).ConfirmMove(
            1, Arg.Any<string>(), sameDriveDestination, sameDrive: true);
    }

    [Fact]
    public async Task Move_is_offered_with_no_destination_typed()
    {
        var vm = CreateViewModel();
        await vm.Scan.ScanWithProgressAsync(null);

        Assert.Equal(string.Empty, vm.Cleanup.MoveDestination);
        // Gating this on the box would drop the button out of the tab order,
        // taking the action and its own explanation of what it needs with it.
        Assert.True(vm.Cleanup.MoveAllCommand.CanExecute(null));
    }

    [Fact]
    public async Task Move_with_no_destination_asks_for_one_and_moves_there()
    {
        var chosen = Path.Combine(Path.GetTempPath(), "ic-test-picked");
        _confirmationService.AskForMoveDestination().Returns(chosen);
        _confirmationService.ConfirmMove(
            Arg.Any<int>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<bool>()).Returns(true);

        var vm = CreateViewModel();
        await vm.Scan.ScanWithProgressAsync(null);

        await vm.Cleanup.MoveAllCommand.ExecuteAsync(null);

        _confirmationService.Received(1).AskForMoveDestination();
        await _moveService.Received(1).MoveFilesAsync(
            Arg.Any<IEnumerable<string>>(), chosen, Arg.Any<UnderLeaseClaims>(),
            Arg.Any<IProgress<OperationProgress>?>(), Arg.Any<CancellationToken>());
        // The answer is kept, so the box shows where the files went.
        Assert.Equal(chosen, vm.Cleanup.MoveDestination);
    }

    [Fact]
    public async Task Backing_out_of_the_destination_browser_moves_nothing()
    {
        // Null is the cancelled answer.
        _confirmationService.AskForMoveDestination().Returns((string?)null);

        var vm = CreateViewModel();
        await vm.Scan.ScanWithProgressAsync(null);

        await vm.Cleanup.MoveAllCommand.ExecuteAsync(null);

        await _moveService.DidNotReceive().MoveFilesAsync(
            Arg.Any<IEnumerable<string>>(), Arg.Any<string>(), Arg.Any<UnderLeaseClaims>(),
            Arg.Any<IProgress<OperationProgress>?>(), Arg.Any<CancellationToken>());
        _confirmationService.DidNotReceive().ConfirmMove(
            Arg.Any<int>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<bool>());
        // Backing out leaves the box as it was rather than recording a choice
        // the user withdrew, and leaves the command usable.
        Assert.Equal(string.Empty, vm.Cleanup.MoveDestination);
        Assert.False(vm.Cleanup.IsOperationInFlight);
        Assert.True(vm.Cleanup.MoveAllCommand.CanExecute(null));
    }

    [Fact]
    public async Task A_destination_already_typed_is_not_asked_for_again()
    {
        _confirmationService.ConfirmMove(
            Arg.Any<int>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<bool>()).Returns(true);

        var vm = CreateViewModel();
        await vm.Scan.ScanWithProgressAsync(null);
        vm.Cleanup.MoveDestination = _destination;

        await vm.Cleanup.MoveAllCommand.ExecuteAsync(null);

        _confirmationService.DidNotReceive().AskForMoveDestination();
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
            Arg.Any<int>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<bool>());
        await _moveService.DidNotReceive().MoveFilesAsync(
            Arg.Any<IEnumerable<string>>(), Arg.Any<string>(), Arg.Any<UnderLeaseClaims>(),
            Arg.Any<IProgress<OperationProgress>?>(), Arg.Any<CancellationToken>());
        Assert.False(vm.Cleanup.IsOperationInFlight);
    }

    /// <summary>The Move's service was never handed a batch.</summary>
    private async Task AssertNoBatchWasHandedOver() =>
        await _moveService.DidNotReceive().MoveFilesAsync(
            Arg.Any<IEnumerable<string>>(), Arg.Any<string>(), Arg.Any<UnderLeaseClaims>(),
            Arg.Any<IProgress<OperationProgress>?>(), Arg.Any<CancellationToken>());

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task A_close_during_the_probe_is_held_until_the_created_folder_has_been_removed(bool overlayUp)
    {
        var probeEntered = new ManualResetEventSlim();
        var releaseProbe = new ManualResetEventSlim();
        var removalEntered = new ManualResetEventSlim();
        var releaseRemoval = new ManualResetEventSlim();
        var removalFinished = 0;
        _directory.When(d => d.CreateDirectory(Arg.Any<string>())).Do(_ =>
        {
            probeEntered.Set();
            releaseProbe.Wait();
        });
        _directory.When(d => d.Delete(Arg.Any<string>())).Do(_ =>
        {
            removalEntered.Set();
            releaseRemoval.Wait();
            Volatile.Write(ref removalFinished, 1);
        });

        var vm = CreateViewModel();
        await vm.Scan.ScanWithProgressAsync(null);
        vm.Cleanup.MoveDestination = _destination;
        int? removalFinishedWhenTheHoldFell = null;
        vm.Cleanup.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName == nameof(CleanupViewModel.IsMoveOrDeleteRunning)
                && !vm.Cleanup.IsMoveOrDeleteRunning)
                removalFinishedWhenTheHoldFell = Volatile.Read(ref removalFinished);
        };

        var move = vm.Cleanup.MoveAllCommand.ExecuteAsync(null);
        Assert.True(probeEntered.Wait(TimeSpan.FromSeconds(5)));
        // Closed straight away, before the overlay is up, or once the overlay and its
        // Cancel button are up.
        if (overlayUp) await WaitUntil(() => vm.Cleanup.IsOperating);
        Assert.True(vm.Cleanup.IsMoveOrDeleteRunning);
        vm.Cleanup.RequestClose();
        releaseProbe.Set();

        // The probe has stopped and the folder it made is being removed. The overlay is
        // down, which is the point at which the window used to take the close.
        await WaitUntil(() => removalEntered.IsSet);
        Assert.False(vm.Cleanup.IsOperating);
        Assert.True(vm.Cleanup.IsMoveOrDeleteRunning);
        Assert.Null(removalFinishedWhenTheHoldFell);
        // The execution gate has fallen by now, and neither command may start in the
        // gap: a second command's hold would be released by the first one's end.
        Assert.False(vm.Cleanup.IsOperationInFlight);
        Assert.False(vm.Cleanup.MoveAllCommand.CanExecute(null));
        Assert.False(vm.Cleanup.DeleteAllCommand.CanExecute(null));

        releaseRemoval.Set();
        await move.WaitAsync(TimeSpan.FromSeconds(10));

        Assert.Equal(1, removalFinishedWhenTheHoldFell);
        Assert.False(vm.Cleanup.IsMoveOrDeleteRunning);
        Assert.True(vm.Cleanup.CloseRequested);
        AssertFolderRemoved();
        _confirmationService.DidNotReceive().ConfirmMove(
            Arg.Any<int>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<bool>());
        await AssertNoBatchWasHandedOver();
        Assert.False(vm.Cleanup.IsOperationInFlight);
        Assert.False(vm.Cleanup.IsCancellationRequested);
    }

    [Fact]
    public async Task A_close_while_the_confirmation_is_up_ends_the_Move_and_removes_the_created_folder()
    {
        var vm = CreateViewModel();
        var heldWhileConfirming = false;
        _confirmationService.ConfirmMove(Arg.Any<int>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<bool>())
            .Returns(_ =>
            {
                heldWhileConfirming = vm.Cleanup.IsMoveOrDeleteRunning && !vm.Cleanup.IsOperating;
                vm.Cleanup.RequestClose();
                return true;
            });
        await vm.Scan.ScanWithProgressAsync(null);
        vm.Cleanup.MoveDestination = _destination;

        await vm.Cleanup.MoveAllCommand.ExecuteAsync(null);

        Assert.True(heldWhileConfirming);
        AssertFolderRemoved();
        await AssertNoBatchWasHandedOver();
        await _reverifier.DidNotReceive().ReverifyAsync(
            Arg.Any<IReadOnlyList<string>>(), Arg.Any<CancellationToken>(), Arg.Any<IProgress<ScanProgressUpdate>?>());
        Assert.False(vm.Cleanup.IsMoveOrDeleteRunning);
        Assert.False(vm.Cleanup.IsOperationInFlight);
    }

    [Fact]
    public async Task A_close_during_the_check_after_the_confirmation_ends_the_Move_and_removes_the_created_folder()
    {
        var vm = CreateViewModel();
        var confirmed = false;
        _confirmationService.ConfirmMove(Arg.Any<int>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<bool>())
            .Returns(_ => { confirmed = true; return true; });
        _rebootService.Check().Returns(_ =>
        {
            if (confirmed) vm.Cleanup.RequestClose();
            return PendingRebootResult.Clean;
        });
        await vm.Scan.ScanWithProgressAsync(null);
        vm.Cleanup.MoveDestination = _destination;

        await vm.Cleanup.MoveAllCommand.ExecuteAsync(null);

        Assert.True(vm.Cleanup.CloseRequested);
        AssertFolderRemoved();
        await AssertNoBatchWasHandedOver();
        await _reverifier.DidNotReceive().ReverifyAsync(
            Arg.Any<IReadOnlyList<string>>(), Arg.Any<CancellationToken>(), Arg.Any<IProgress<ScanProgressUpdate>?>());
        Assert.False(vm.Cleanup.IsOperating);
        Assert.False(vm.Cleanup.IsMoveOrDeleteRunning);
        Assert.False(vm.Cleanup.IsOperationInFlight);
    }

    [Fact]
    public async Task A_close_while_the_destination_browser_is_up_starts_no_Move()
    {
        var vm = CreateViewModel();
        _confirmationService.AskForMoveDestination().Returns(_ =>
        {
            vm.Cleanup.RequestClose();
            return _destination;
        });
        await vm.Scan.ScanWithProgressAsync(null);

        await vm.Cleanup.MoveAllCommand.ExecuteAsync(null);

        _directory.DidNotReceive().CreateDirectory(Arg.Any<string>());
        AssertNoFolderRemoved();
        _confirmationService.DidNotReceive().ConfirmMove(
            Arg.Any<int>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<bool>());
        await AssertNoBatchWasHandedOver();
        Assert.False(vm.Cleanup.IsMoveOrDeleteRunning);
        Assert.False(vm.Cleanup.IsOperationInFlight);
    }

    [Fact]
    public async Task A_close_while_the_Delete_confirmation_is_up_starts_no_Delete()
    {
        var vm = CreateViewModel();
        _confirmationService.ConfirmDelete(Arg.Any<int>(), Arg.Any<string>())
            .Returns(_ => { vm.Cleanup.RequestClose(); return true; });
        await vm.Scan.ScanWithProgressAsync(null);

        await vm.Cleanup.DeleteAllCommand.ExecuteAsync(null);

        await _deleteService.DidNotReceive().DeleteFilesAsync(
            Arg.Any<IEnumerable<string>>(), Arg.Any<UnderLeaseClaims>(),
            Arg.Any<IProgress<OperationProgress>?>(), Arg.Any<CancellationToken>());
        await _reverifier.DidNotReceive().ReverifyAsync(
            Arg.Any<IReadOnlyList<string>>(), Arg.Any<CancellationToken>(), Arg.Any<IProgress<ScanProgressUpdate>?>());
        Assert.False(vm.Cleanup.IsMoveOrDeleteRunning);
        Assert.False(vm.Cleanup.IsOperationInFlight);
    }

    [Fact]
    public async Task A_close_during_the_check_before_a_Delete_ends_the_Delete()
    {
        var vm = CreateViewModel();
        var confirmed = false;
        _confirmationService.ConfirmDelete(Arg.Any<int>(), Arg.Any<string>())
            .Returns(_ => { confirmed = true; return true; });
        _rebootService.Check().Returns(_ =>
        {
            if (confirmed) vm.Cleanup.RequestClose();
            return PendingRebootResult.Clean;
        });
        await vm.Scan.ScanWithProgressAsync(null);

        await vm.Cleanup.DeleteAllCommand.ExecuteAsync(null);

        Assert.True(vm.Cleanup.CloseRequested);
        await _deleteService.DidNotReceive().DeleteFilesAsync(
            Arg.Any<IEnumerable<string>>(), Arg.Any<UnderLeaseClaims>(),
            Arg.Any<IProgress<OperationProgress>?>(), Arg.Any<CancellationToken>());
        await _reverifier.DidNotReceive().ReverifyAsync(
            Arg.Any<IReadOnlyList<string>>(), Arg.Any<CancellationToken>(), Arg.Any<IProgress<ScanProgressUpdate>?>());
        Assert.False(vm.Cleanup.IsOperating);
        Assert.False(vm.Cleanup.IsMoveOrDeleteRunning);
        Assert.False(vm.Cleanup.IsOperationInFlight);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task The_hold_on_the_close_is_taken_for_a_command_the_user_declines_and_released_after_it(bool deleting)
    {
        _confirmationService.ConfirmMove(Arg.Any<int>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<bool>())
            .Returns(false);
        _confirmationService.ConfirmDelete(Arg.Any<int>(), Arg.Any<string>()).Returns(false);
        var vm = CreateViewModel();
        await vm.Scan.ScanWithProgressAsync(null);
        vm.Cleanup.MoveDestination = _destination;
        var changes = new List<bool>();
        vm.Cleanup.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName == nameof(CleanupViewModel.IsMoveOrDeleteRunning))
                changes.Add(vm.Cleanup.IsMoveOrDeleteRunning);
        };

        if (deleting) await vm.Cleanup.DeleteAllCommand.ExecuteAsync(null);
        else await vm.Cleanup.MoveAllCommand.ExecuteAsync(null);

        Assert.Equal([true, false], changes);
        Assert.True(vm.Cleanup.MoveAllCommand.CanExecute(null));
        Assert.True(vm.Cleanup.DeleteAllCommand.CanExecute(null));
    }

    [Fact]
    public async Task The_hold_on_the_close_is_released_by_a_Move_refused_for_a_relative_destination()
    {
        var vm = CreateViewModel();
        await vm.Scan.ScanWithProgressAsync(null);
        vm.Cleanup.MoveDestination = "backup";
        var changes = new List<bool>();
        vm.Cleanup.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName == nameof(CleanupViewModel.IsMoveOrDeleteRunning))
                changes.Add(vm.Cleanup.IsMoveOrDeleteRunning);
        };

        await vm.Cleanup.MoveAllCommand.ExecuteAsync(null);

        Assert.Equal([true, false], changes);
    }
}
