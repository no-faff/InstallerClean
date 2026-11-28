using System.IO.Abstractions.TestingHelpers;
using InstallerClean.Tests.Helpers;
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
public class ScanMoveCompletionTests : IDisposable
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
    private readonly IUpdateCheckService _updateCheckService = Substitute.For<IUpdateCheckService>();
    private readonly IBatchCheck _batchCheck = Substitute.For<IBatchCheck>();
    private readonly IEarlierRunCheck _earlierRunCheck = Substitute.For<IEarlierRunCheck>();
    private readonly IFirstRunMark _firstRunMark = FirstRunMarks.Claiming();
    private readonly IWindowsRegion _windowsRegion = Substitute.For<IWindowsRegion>();

    private readonly MockFileSystem _fileSystem = new();

    /// <summary>The view models this class made, flushed after each test.</summary>
    private readonly MadeViewModels _made = new();

    public void Dispose() => _made.Dispose();

    public ScanMoveCompletionTests()
    {
        // A working settings service: a destination save that does land records nothing.
        _settingsService.Update(Arg.Any<Action<AppSettings>>()).Returns(true);
    }

    private MainViewModel CreateMain()
    {
        _settingsService.Load().Returns(new AppSettings());
        // Clean gate and a check before the batch that stands over every file it is
        // given, so a Move/Delete acts on the full set.
        _rebootService.Check().Returns(PendingRebootResult.Clean);
        _batchCheck.RunAsync(
                Arg.Any<IReadOnlyList<string>>(), Arg.Any<TimeSpan?>(), Arg.Any<IProgress<BatchCheckProgress>>(),
                Arg.Any<CancellationToken>())
            .Returns(ci => Task.FromResult(StandingChecks.Stood(
                new ReverifyResult(ci.Arg<IReadOnlyList<string>>(), Array.Empty<string>()))));
        return _made.Add(new MainViewModel(
            _scanService, _moveService, _deleteService,
            _settingsService, _rebootService, _msiInfoService,
            _dialogService, _confirmationService, _windowService,
            _fileSystem, _resultLogService, _updateCheckService, _batchCheck,
            _earlierRunCheck, _firstRunMark, _windowsRegion,
            destinationGuard: new ByPathDestinationGuard(_fileSystem)));
    }

    [Fact]
    public async Task Scan_then_Move_paints_completion_overlay_with_summary()
    {
        var orphans = new[]
        {
            new OrphanedFile(@"C:\Windows\Installer\a.msi", 1_048_576, false, false, false, InstallerClean.Resources.Strings.Reason_Orphaned),
            new OrphanedFile(@"C:\Windows\Installer\b.msi", 2_097_152, false, false, false, InstallerClean.Resources.Strings.Reason_Orphaned),
        };
        _scanService.ScanAsync(Arg.Any<IProgress<ScanProgressUpdate>?>(), Arg.Any<CancellationToken>())
            .Returns(new ScanResult(orphans, Array.Empty<RegisteredPackage>(), 0));
        _confirmationService.ConfirmMove(
            Arg.Any<int>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<bool>()).Returns(true);
        _moveService.MoveFilesAsync(
                Arg.Any<StandingCheck>(), Arg.Any<string>(),
                Arg.Any<IProgress<OperationProgress>?>(), Arg.Any<CancellationToken>())
            .Returns(new MoveResult(2, Array.Empty<FileOperationError>()));

        var vm = CreateMain();

        // Stage 1: scan finds two orphans. Completion overlay must NOT
        // appear yet (orphans exist), Cleanup commands become available.
        await vm.Scan.ScanWithProgressAsync(null);
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
        Assert.Contains("3.0 MB", vm.Completion.Heading);  // 1MB + 2MB freed
        Assert.False(vm.Cleanup.IsOperating);
        await _scanService.Received(2).ScanAsync(  // initial + post-move refresh
            Arg.Any<IProgress<ScanProgressUpdate>?>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Scan_finding_no_orphans_immediately_paints_all_clear()
    {
        _scanService.ScanAsync(Arg.Any<IProgress<ScanProgressUpdate>?>(), Arg.Any<CancellationToken>())
            .Returns(new ScanResult(Array.Empty<OrphanedFile>(), Array.Empty<RegisteredPackage>(), 0));

        var vm = CreateMain();
        await vm.Scan.ScanWithProgressAsync(null);

        Assert.True(vm.Completion.IsComplete);
        Assert.Equal("All clean", vm.Completion.Heading);
        Assert.False(vm.Cleanup.MoveAllCommand.CanExecute(null));
        Assert.False(vm.Cleanup.DeleteAllCommand.CanExecute(null));
    }

    [Fact]
    public async Task Scan_then_Delete_paints_completion_overlay_with_the_delete_summary()
    {
        var orphans = new[]
        {
            new OrphanedFile(@"C:\Windows\Installer\x.msi", 524_288, false, false, false, InstallerClean.Resources.Strings.Reason_Orphaned),
        };
        _scanService.ScanAsync(Arg.Any<IProgress<ScanProgressUpdate>?>(), Arg.Any<CancellationToken>())
            .Returns(new ScanResult(orphans, Array.Empty<RegisteredPackage>(), 0));
        _confirmationService.ConfirmDelete(
            Arg.Any<int>(), Arg.Any<string>()).Returns(true);
        _deleteService.DeleteFilesAsync(
                Arg.Any<StandingCheck>(),
                Arg.Any<IProgress<OperationProgress>?>(), Arg.Any<CancellationToken>())
            .Returns(new DeleteResult(1, Array.Empty<FileOperationError>()));

        var vm = CreateMain();
        await vm.Scan.ScanWithProgressAsync(null);
        await vm.Cleanup.DeleteAllCommand.ExecuteAsync(null);

        Assert.True(vm.Completion.IsComplete);
        Assert.Contains("permanently deleted", vm.Completion.Summary);
    }

    [Fact]
    public async Task Rescan_after_Done_on_the_completion_overlay_runs_another_scan()
    {
        _scanService.ScanAsync(Arg.Any<IProgress<ScanProgressUpdate>?>(), Arg.Any<CancellationToken>())
            .Returns(new ScanResult(Array.Empty<OrphanedFile>(), Array.Empty<RegisteredPackage>(), 0));

        var vm = CreateMain();
        await vm.Scan.ScanWithProgressAsync(null);
        Assert.True(vm.Completion.IsComplete);
        Assert.False(vm.Scan.ScanCommand.CanExecute(null));

        vm.Completion.DismissCommand.Execute(null);

        Assert.False(vm.Completion.IsComplete);
        Assert.True(vm.Scan.ScanCommand.CanExecute(null));
        await vm.Scan.ScanCommand.ExecuteAsync(null);

        await _scanService.Received(2).ScanAsync(
            Arg.Any<IProgress<ScanProgressUpdate>?>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Real_scan_then_real_move_against_in_memory_filesystem_produces_correct_summary()
    {
        // This test stands up production FileSystemScanService and
        // MoveFilesService instances against an in-memory IFileSystem,
        // with only the MSI API and the user-input services
        // substituted. It is the only test in the suite that exercises
        // the full production wiring (scan -> orphan detection ->
        // move pipeline -> completion summary) end to end.
        const string installerFolder = @"C:\Windows\Installer";
        const string destFolder = @"D:\backup\installer";

        var fs = new MockFileSystem();
        fs.AddFile($@"{installerFolder}\registered.msi", new MockFileData(new byte[1024]));
        fs.AddFile($@"{installerFolder}\orphan-a.msi",   new MockFileData(new byte[2048]));
        fs.AddFile($@"{installerFolder}\orphan-b.msi",   new MockFileData(new byte[4096]));
        fs.AddDirectory(destFolder);

        // Only the registered.msi is claimed by the MSI API; the two
        // orphans remain unaccounted for and become removables.
        var queryService = Substitute.For<IInstallerQueryService>();
        queryService.GetRegisteredPackagesAsync(
                Arg.Any<IProgress<ScanProgressUpdate>?>(), Arg.Any<CancellationToken>())
            .Returns(new InstallerQueryResult(new List<RegisteredPackage>
            {
                new($@"{installerFolder}\registered.msi", "Registered", "{AAA}", FileSizeBytes: 1024),
            }.AsReadOnly()));

        // Real scan service over the in-memory FS.
        var scanService = new FileSystemScanService(queryService, fs, null, installerFolder);
        // Real move service over the in-memory FS.
        var moveService = new MoveFilesService(fs);
        // The delete path is not exercised in this move-focused journey; the
        // service is constructed only because the view-model graph needs one.
        var deleteService = new DeleteFilesService(fs);

        // External concerns stay substituted.
        var settingsService = Substitute.For<ISettingsService>();
        settingsService.Load().Returns(new AppSettings());
        settingsService.Update(Arg.Any<Action<AppSettings>>()).Returns(true);
        var rebootService = Substitute.For<IPendingRebootService>();
        rebootService.Check().Returns(PendingRebootResult.Clean);
        rebootService.CheckBusy().Returns(PendingRebootResult.Clean);
        // Real re-verifier over the same query service, inside the real check made
        // before the batch: it re-runs the same enumeration the scan did, so the two
        // orphans (unclaimed by the API) stay removable and nothing is dropped,
        // exercising the check before the batch end to end.
        var reverifier = new RemovableReverifier(queryService);
        var batchCheck = new BatchCheck(rebootService, reverifier, new FakeRecordsWatchFactory());
        var msiInfoService = Substitute.For<IMsiFileInfoService>();
        var dialogService = Substitute.For<IDialogService>();
        var confirmationService = Substitute.For<IConfirmationService>();
        confirmationService.ConfirmMove(
            Arg.Any<int>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<bool>()).Returns(true);
        var windowService = Substitute.For<IWindowService>();
        var resultLogService = Substitute.For<IResultLogService>();
        var updateCheckService = Substitute.For<IUpdateCheckService>();

        // Pass the same `fs` instance the production services were
        // wired against, here and to the by-path guard. The view model
        // creates the destination folder through it and the guard
        // probes that folder through it, so two distinct MockFileSystems
        // would split the folder from its probe; sharing the instance
        // keeps the test honest about the production graph.
        var vm = _made.Add(new MainViewModel(
            scanService, moveService, deleteService,
            settingsService, rebootService, msiInfoService,
            dialogService, confirmationService, windowService,
            fs, resultLogService, updateCheckService, batchCheck,
            Substitute.For<IEarlierRunCheck>(), FirstRunMarks.Claiming(), Substitute.For<IWindowsRegion>(),
            destinationGuard: new ByPathDestinationGuard(fs)));

        // Stage 1: real scan finds the two orphans.
        await vm.Scan.ScanWithProgressAsync(null);
        Assert.Equal(2, vm.Scan.OrphanedFileCount);
        Assert.False(vm.Completion.IsComplete);

        // Stage 2: real move relocates them to the in-memory dest.
        vm.Cleanup.MoveDestination = destFolder;
        await vm.Cleanup.MoveAllCommand.ExecuteAsync(null);

        // Verify the in-memory filesystem actually changed: the two
        // orphans are gone from the source and present at the dest.
        Assert.False(fs.File.Exists($@"{installerFolder}\orphan-a.msi"));
        Assert.False(fs.File.Exists($@"{installerFolder}\orphan-b.msi"));
        Assert.True(fs.File.Exists($@"{destFolder}\orphan-a.msi"));
        Assert.True(fs.File.Exists($@"{destFolder}\orphan-b.msi"));
        // Registered file untouched.
        Assert.True(fs.File.Exists($@"{installerFolder}\registered.msi"));

        // Completion overlay rendered and reads "0 orphans" after refresh.
        Assert.True(vm.Completion.IsComplete);
        Assert.Contains(destFolder, vm.Completion.Summary);
        Assert.Equal(0, vm.Scan.OrphanedFileCount);
    }

    [Fact]
    public async Task Move_with_per_file_errors_renders_grouped_breakdown()
    {
        var orphans = new[]
        {
            new OrphanedFile(@"C:\Windows\Installer\a.msi", 1024, false, false, false, InstallerClean.Resources.Strings.Reason_Orphaned),
            new OrphanedFile(@"C:\Windows\Installer\b.msi", 2048, false, false, false, InstallerClean.Resources.Strings.Reason_Orphaned),
            new OrphanedFile(@"C:\Windows\Installer\c.msi", 4096, false, false, false, InstallerClean.Resources.Strings.Reason_Orphaned),
        };
        _scanService.ScanAsync(Arg.Any<IProgress<ScanProgressUpdate>?>(), Arg.Any<CancellationToken>())
            .Returns(new ScanResult(orphans, Array.Empty<RegisteredPackage>(), 0));
        _confirmationService.ConfirmMove(
            Arg.Any<int>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<bool>()).Returns(true);

        // One success, two MissingSourceFile errors.
        var errors = new FileOperationError[]
        {
            new MissingSourceFile(@"C:\Windows\Installer\b.msi"),
            new MissingSourceFile(@"C:\Windows\Installer\c.msi"),
        };
        _moveService.MoveFilesAsync(
                Arg.Any<StandingCheck>(), Arg.Any<string>(),
                Arg.Any<IProgress<OperationProgress>?>(), Arg.Any<CancellationToken>())
            .Returns(new MoveResult(1, errors));

        var vm = CreateMain();
        await vm.Scan.ScanWithProgressAsync(null);
        vm.Cleanup.MoveDestination = @"D:\backup";
        await vm.Cleanup.MoveAllCommand.ExecuteAsync(null);

        Assert.True(vm.Completion.IsComplete);

        // The failure count lives on its own line rather than as a bracket on
        // the group header: 2 failed of the 3 the batch attempted (1 moved + 2
        // errors).
        Assert.Equal(
            string.Format(InstallerClean.Resources.Strings.Completion_FailedCount_Plural, 2, 3),
            vm.Completion.FailedCount);

        // The breakdown groups by category: a header sentence once per bucket,
        // then each filename on a line of its own behind a hyphen, spaces alone
        // being invisible in a proportional font. Verify the structural
        // ordering, not just substring presence, so a regression where one of
        // the files lands in a different bucket (e.g. IOFailure) would be caught.
        var lines = vm.Completion.Errors.Split('\n').Select(l => l.TrimEnd('\r')).ToArray();
        var headerIndex = Array.FindIndex(lines,
            l => l == InstallerClean.Resources.Strings.Error_MissingSourceFile);
        Assert.True(headerIndex >= 0, "expected the missing-file group heading");
        var followingFilenames = lines.Skip(headerIndex + 1).Take(2).ToArray();
        Assert.Equal(2, followingFilenames.Length);
        Assert.All(followingFilenames, l => Assert.StartsWith("- ", l));
        Assert.Contains("b.msi", followingFilenames[0] + followingFilenames[1]);
        Assert.Contains("c.msi", followingFilenames[0] + followingFilenames[1]);
    }
}
