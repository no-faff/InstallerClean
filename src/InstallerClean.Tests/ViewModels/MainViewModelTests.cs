using System.IO.Abstractions.TestingHelpers;
using InstallerClean.Tests.Helpers;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using InstallerClean.Helpers;
using InstallerClean.Models;
using InstallerClean.Resources;
using InstallerClean.Services;
using InstallerClean.ViewModels;

namespace InstallerClean.Tests.ViewModels;

public class MainViewModelTests : IDisposable
{
    private static readonly string Orphaned = Strings.Reason_Orphaned;

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
    private readonly IEarlierRunCheck _earlierRunCheck = Substitute.For<IEarlierRunCheck>();
    private readonly IFirstRunMark _firstRunMark = FirstRunMarks.Claiming();
    private readonly IWindowsRegion _windowsRegion = Substitute.For<IWindowsRegion>();
    private readonly MockFileSystem _fileSystem = new();

    [Fact]
    public async Task The_card_after_a_Delete_gives_the_line_about_recently_changed_files_of_the_scan_the_refresh_ran()
    {
        // The Delete starts from a scan holding one file back for its times and ends with a
        // refresh holding two. The card is revealed over the second scan and speaks for it.
        var vm = CreateViewModel();
        var before = RecentChangeScan(ScanResultWithOrphans(1), held: 1);
        var after = RecentChangeScan(EmptyScanResult(), held: 2);
        _scanService.ScanAsync(Arg.Any<IProgress<ScanProgressUpdate>?>(), Arg.Any<CancellationToken>())
            .Returns(before, after);
        _deleteService.DeleteFilesAsync(
                Arg.Any<IEnumerable<string>>(), Arg.Any<UnderLeaseClaims>(),
                Arg.Any<IProgress<OperationProgress>?>(), Arg.Any<CancellationToken>())
            .Returns(new DeleteResult(1, Array.Empty<FileOperationError>()));
        _confirmationService.ConfirmDelete(Arg.Any<int>(), Arg.Any<string>()).Returns(true);

        await vm.Scan.ScanWithProgressAsync(null);
        await vm.Cleanup.DeleteAllCommand.ExecuteAsync(null);

        var afterLine = RecentChangeReport.Line(after);
        Assert.True(vm.Completion.IsComplete);
        Assert.Same(after, vm.Scan.LastScanResult);
        Assert.NotEqual(string.Empty, afterLine);
        Assert.NotEqual(RecentChangeReport.Line(before), afterLine);
        Assert.Equal(afterLine, vm.Completion.ScanStartLine);
    }

    /// <summary>
    /// <paramref name="scan"/> holding <paramref name="held"/> files of 1 KB back because
    /// their times are not before the scan's clock.
    /// </summary>
    private static ScanResult RecentChangeScan(ScanResult scan, int held) =>
        scan with
        {
            WithheldFiles = Enumerable.Range(0, held)
                .Select(i => new OrphanedFile($@"C:\Windows\Installer\young{i}.msi", 1024, false, false, false, Orphaned))
                .ToList(),
            WithheldBy = new WithholdingSplit(NotBeforeTheClockCount: held),
            WithheldNotBeforeTheClockBytes = 1024L * held,
        };

    private MainViewModel CreateViewModel() => CreateViewModel(new AppSettings());

    /// <summary>The view models this class made, flushed after each test.</summary>
    private readonly MadeViewModels _made = new();

    public void Dispose() => _made.Dispose();

    public MainViewModelTests()
    {
        // A working settings service: a destination save that does land records nothing.
        _settingsService.Update(Arg.Any<Action<AppSettings>>()).Returns(true);
    }

    /// <summary>
    /// Build a MainViewModel against the substituted services with a
    /// caller-provided initial AppSettings. The one place in this class
    /// that constructs a MainViewModel, so a change to the constructor's
    /// parameters is made here and not at each test.
    /// </summary>
    private MainViewModel CreateViewModel(AppSettings settings, IAppUpdater? appUpdater = null)
    {
        _settingsService.Load().Returns(settings);
        // Check() returns Clean or Block, never null (the interface contract);
        // default it Clean so the scan and the act-time re-check both proceed.
        // Tests covering the gate override with a Clean-then-Block sequence.
        _rebootService.Check().Returns(PendingRebootResult.Clean);
        // Default the act-time re-verify to a no-op: every candidate survives and
        // nothing is dropped, so a Move/Delete acts on the full scanned set.
        // Tests covering the re-verify override this to drop entries or to throw.
        _reverifier.ReverifyAsync(Arg.Any<IReadOnlyList<string>>(), Arg.Any<CancellationToken>(), Arg.Any<IProgress<ScanProgressUpdate>?>())
            .Returns(ci => new ReverifyResult((IReadOnlyList<string>)ci[0]!, Array.Empty<string>()));

        return _made.Add(new MainViewModel(
            _scanService, _moveService, _deleteService,
            _settingsService, _rebootService, _msiInfoService,
            _dialogService, _confirmationService, _windowService,
            _fileSystem, _resultLogService, _updateCheckService, _reverifier,
            _earlierRunCheck, _firstRunMark, _windowsRegion,
            destinationGuard: new ByPathDestinationGuard(_fileSystem),
            appUpdater: appUpdater));
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
    public async Task ScanAsync_leaves_a_registration_whose_file_is_gone_out_of_the_left_alone_count()
    {
        // The line says files were left alone. A registration whose cached file is not
        // in the folder names no file to leave alone, and the size beside it counts
        // none either, so the count and the size answer for one population.
        var vm = CreateViewModel();
        var registered = new List<RegisteredPackage>
        {
            new(@"C:\Windows\Installer\here.msi", "Product A", "{AAA}"),
            new(@"C:\Windows\Installer\gone.msi", "Product B", "{BBB}", FileExists: false),
        };
        _scanService.ScanAsync(Arg.Any<IProgress<ScanProgressUpdate>?>(), Arg.Any<CancellationToken>())
            .Returns(new ScanResult(Array.Empty<OrphanedFile>(), registered, 1_048_576));

        await vm.Scan.ScanWithProgressAsync(null);

        Assert.Equal(1, vm.Scan.RegisteredFileCount);
        Assert.Equal("1.0 MB", vm.Scan.RegisteredSizeDisplay);
    }

    [Fact]
    public async Task The_left_alone_count_and_the_details_footer_agree_on_one_scan()
    {
        // THE PROPERTY BOTH VIEW MODELS SAY THEY ARE PROTECTING, HELD ACROSS THE PAIR.
        // The two are one click apart and are built from the same two lists off one
        // ScanResult, by this view model and by ChromeViewModel.OpenRegisteredDetails,
        // so a change to either count that misses the other is invisible until a reader
        // compares the screens. This fixture holds every population at once: a
        // registration present, one whose file is gone, and a withheld file.
        var vm = CreateViewModel();
        var registered = new List<RegisteredPackage>
        {
            new(@"C:\Windows\Installer\here.msi", "Product A", "{AAA}"),
            new(@"C:\Windows\Installer\alsohere.msi", "Product B", "{BBB}"),
            new(@"C:\Windows\Installer\gone.msi", "Product C", "{CCC}", FileExists: false),
        };
        var withheld = new List<OrphanedFile>
        {
            new(@"C:\Windows\Installer\kept.msi", 1_048_576, false, false, false, "orphaned"),
        };
        var scan = new ScanResult(
            RemovableFiles: Array.Empty<OrphanedFile>(),
            RegisteredPackages: registered,
            RegisteredTotalBytes: 2_097_152,
            WithheldFiles: withheld);
        _scanService.ScanAsync(Arg.Any<IProgress<ScanProgressUpdate>?>(), Arg.Any<CancellationToken>())
            .Returns(scan);

        await vm.Scan.ScanWithProgressAsync(null);

        // The Details view model comes off the button, not out of this method: the
        // window service is substituted, so opening the window hands over the object
        // ChromeViewModel itself builds. Which lists that construction passes is
        // therefore part of what the comparison below holds, and the two screens are
        // compared as the app assembles them rather than as a fixture repeats them.
        RegisteredFilesViewModel? opened = null;
        _windowService.When(s => s.ShowRegisteredDetails(Arg.Any<RegisteredFilesViewModel>()))
            .Do(ci => opened = ci.Arg<RegisteredFilesViewModel>());

        vm.Chrome.OpenRegisteredDetailsCommand.Execute(null);

        var details = Assert.IsType<RegisteredFilesViewModel>(opened);

        // Three files left alone: two registered and present, plus the withheld one.
        Assert.Equal(3, vm.Scan.RegisteredFileCount);
        Assert.StartsWith(
            string.Format(Strings.Summary_RegisteredWindow_Plural, 3, "3.0 MB"),
            details.Summary, StringComparison.Ordinal);
        Assert.EndsWith(
            string.Format(Strings.Summary_RegisteredWindow_Missing_Singular, 1),
            details.Summary, StringComparison.Ordinal);
    }

    [Fact]
    public async Task The_all_clear_receipt_counts_the_files_the_line_behind_it_counts()
    {
        // THE OVERLAY AND THE LINE IT SITS OVER, HELD TOGETHER ON ONE SCAN. Dismissing
        // the overlay puts that line in front of the reader, so two different numbers
        // for one machine read as a mistake. Both are taken off the same property here
        // rather than recounted, and this fixture holds the registration that used to
        // separate them: a superseded patch whose cached file has gone and whose
        // absence the scan established nothing can be hurt by.
        //
        // IT ALSO PINS THE ORDER. The count is assigned while the scan result is being
        // applied and read on the completion path that follows, so a read that
        // overtook the write would show here as a receipt counting nothing.
        var vm = CreateViewModel();
        var registered = new List<RegisteredPackage>
        {
            new(@"C:\Windows\Installer\here.msi", "Product A", "{AAA}"),
            new(@"C:\Windows\Installer\alsohere.msi", "Product B", "{BBB}"),
            new(@"C:\Windows\Installer\a1d2f.msp", "Product C", "{CCC}",
                PatchState: 2,
                RemovableWithheld: true,
                ProductPatchSetVerdict: ProductPatchSet.AllNonRemovable,
                FileExists: false,
                WithheldOnUnreadableFile: true),
        };
        _scanService.ScanAsync(Arg.Any<IProgress<ScanProgressUpdate>?>(), Arg.Any<CancellationToken>())
            .Returns(new ScanResult(Array.Empty<OrphanedFile>(), registered, 2_097_152));

        await vm.Scan.ScanWithProgressAsync(null);

        Assert.Equal(2, vm.Scan.RegisteredFileCount);
        Assert.StartsWith("Scanned 2 files in ", vm.Completion.Restore, StringComparison.Ordinal);

        RegisteredFilesViewModel? opened = null;
        _windowService.When(s => s.ShowRegisteredDetails(Arg.Any<RegisteredFilesViewModel>()))
            .Do(ci => opened = ci.Arg<RegisteredFilesViewModel>());
        vm.Chrome.OpenRegisteredDetailsCommand.Execute(null);
        var details = Assert.IsType<RegisteredFilesViewModel>(opened);

        // The window behind the overlay says the same number, holds no row for the
        // registration whose file the scan cleared, and carries no missing clause.
        Assert.Equal("2 files left alone (2.0 MB)", details.Summary);
        Assert.Equal(
            new[] { @"C:\Windows\Installer\alsohere.msi", @"C:\Windows\Installer\here.msi" },
            details.Products.Select(p => p.FullPath).OrderBy(p => p, StringComparer.Ordinal));
    }

    [Fact]
    public async Task The_nothing_offered_receipt_counts_the_held_back_files()
    {
        // A HELD-BACK FILE IS IN THE RECEIPT. The receipt says how many files the run
        // examined, and it examined that one: it is in the folder, the scan opened it and
        // judged it. A receipt that left it out would understate what was looked at,
        // which is the one thing it is for.
        var vm = CreateViewModel();
        var registered = new List<RegisteredPackage>
        {
            new(@"C:\Windows\Installer\here.msi", "Product A", "{AAA}"),
            new(@"C:\Windows\Installer\alsohere.msi", "Product B", "{BBB}"),
        };
        var withheld = new List<OrphanedFile>
        {
            new(@"C:\Windows\Installer\kept.msi", 1_048_576, false, false, false, Orphaned),
            new(@"C:\Windows\Installer\alsokept.msi", 1_048_576, false, false, false, Orphaned),
        };
        _scanService.ScanAsync(Arg.Any<IProgress<ScanProgressUpdate>?>(), Arg.Any<CancellationToken>())
            .Returns(new ScanResult(
                RemovableFiles: Array.Empty<OrphanedFile>(),
                RegisteredPackages: registered,
                RegisteredTotalBytes: 2_097_152,
                WithheldFiles: withheld));

        await vm.Scan.ScanWithProgressAsync(null);

        // Four examined, two of them kept back, and the receipt counts all four.
        Assert.Equal(4, vm.Scan.RegisteredFileCount);
        Assert.Equal(Strings.Completion_NothingOffered, vm.Completion.Heading);
        Assert.StartsWith("Scanned 4 files in ", vm.Completion.Restore, StringComparison.Ordinal);
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

    // AN EMPTY OFFER HAS THREE MEANINGS AND THE THREE TESTS BELOW ARE THE PARTITION.
    // The folder held nothing to offer, which gets the all-clear; or a rule about the
    // machine's records emptied the walk-derived offer in one go, or the files were
    // judged one at a time and none could be cleared, each of which gets the screen
    // saying nothing was offered. Read what each one SETS UP rather than what it asserts: they
    // differ in the withheld list and in the split that says what put those files
    // there, which is exactly what the branch reads, and any two of them passing while
    // the third fails names the mistake.

    [Fact]
    public async Task A_wholesale_withholding_gets_its_own_screen_rather_than_the_all_clear()
    {
        // The offer is empty because a rule about the machine's records emptied it,
        // not because the folder held nothing. "Nothing to clean up in your Installer
        // folder" is a claim about that disk the scan never made.
        var vm = CreateViewModel();
        var withheld = new List<OrphanedFile>
        {
            new(@"C:\Windows\Installer\a.msi", 1024, false, false, false, Orphaned),
            new(@"C:\Windows\Installer\b.msi", 2048, false, false, false, Orphaned),
        };
        _scanService.ScanAsync(Arg.Any<IProgress<ScanProgressUpdate>?>(), Arg.Any<CancellationToken>())
            .Returns(new ScanResult(
                Array.Empty<OrphanedFile>(), Array.Empty<RegisteredPackage>(), 0,
                WithheldFiles: withheld,
                WithheldBy: new WithholdingSplit(WholesaleCount: 2)));

        await vm.Scan.ScanWithProgressAsync(null);

        Assert.True(vm.Completion.IsComplete);
        Assert.Equal(Strings.Completion_NothingOffered, vm.Completion.Heading);
        Assert.NotEqual(Strings.Completion_AllClean, vm.Completion.Heading);
    }

    [Fact]
    public async Task A_wholesale_withholding_that_held_nothing_back_still_shows_the_all_clear()
    {
        // A walk that produced no candidates holds nothing back. Nothing in that folder
        // went unclaimed, so the all-clear is right.
        //
        // THE FIXTURE IS THE RESULT THE SCAN BUILDS WHEN THE GATE FIRES ON SUCH A WALK:
        // a leg fired, the withheld list is empty and the wholesale arm took nothing.
        // The window counts the withheld list, which holds nothing here, so it shows the
        // all-clear. FileSystemScanServiceSecondInstanceTests pins that the scan builds
        // this result.
        var vm = CreateViewModel();
        _scanService.ScanAsync(Arg.Any<IProgress<ScanProgressUpdate>?>(), Arg.Any<CancellationToken>())
            .Returns(new ScanResult(
                Array.Empty<OrphanedFile>(), Array.Empty<RegisteredPackage>(), 0,
                Census: new EnumerationCensus(UnansweredProductCount: 1),
                WithheldFiles: Array.Empty<OrphanedFile>()));

        await vm.Scan.ScanWithProgressAsync(null);

        Assert.Equal(Strings.Completion_AllClean, vm.Completion.Heading);
    }

    [Fact]
    public async Task Files_withheld_one_at_a_time_get_the_nothing_offered_screen_and_not_the_all_clear()
    {
        // Its folder is not clean: a file is sitting in it that nobody has vouched for,
        // so it gets the screen saying nothing was offered. The screen names no cause for
        // the file, and the scan held it for nothing that carries a line of its own.
        var vm = CreateViewModel();
        var withheld = new List<OrphanedFile>
        {
            new(@"C:\Windows\Installer\a.msi", 1024, false, false, false, Orphaned),
        };
        _scanService.ScanAsync(Arg.Any<IProgress<ScanProgressUpdate>?>(), Arg.Any<CancellationToken>())
            .Returns(new ScanResult(
                Array.Empty<OrphanedFile>(), Array.Empty<RegisteredPackage>(), 0,
                WithheldFiles: withheld,
                WithheldBy: new WithholdingSplit(DeclaredProductUnestablishedCount: 1)));

        await vm.Scan.ScanWithProgressAsync(null);

        Assert.Equal(Strings.Completion_NothingOffered, vm.Completion.Heading);
        Assert.NotEqual(Strings.Completion_AllClean, vm.Completion.Heading);
        Assert.Equal(string.Empty, vm.Completion.Summary);
    }

    [Fact]
    public async Task Files_kept_for_an_installed_program_get_the_all_clear()
    {
        // Nothing offered, and every held file was kept for a program Windows still
        // has installed, so the machine gets the screen a clean folder gets.
        var vm = CreateViewModel();
        var withheld = new List<OrphanedFile>
        {
            new(@"C:\Windows\Installer\a.msi", 1024, false, false, false, Orphaned),
            new(@"C:\Windows\Installer\b.msi", 2048, false, false, false, Orphaned),
        };
        _scanService.ScanAsync(Arg.Any<IProgress<ScanProgressUpdate>?>(), Arg.Any<CancellationToken>())
            .Returns(new ScanResult(
                Array.Empty<OrphanedFile>(), Array.Empty<RegisteredPackage>(), 0,
                WithheldFiles: withheld,
                WithheldBy: new WithholdingSplit(DeclaredProductInstalledCount: 2)));

        await vm.Scan.ScanWithProgressAsync(null);

        Assert.Equal(Strings.Completion_AllClean, vm.Completion.Heading);
        Assert.NotEqual(Strings.Completion_NothingOffered, vm.Completion.Heading);
    }

    private const string DriveDLine =
        "InstallerClean carried on without drive D: and left alone any file still to be checked against it. "
        + "Once it's responding normally, Re-scan.";

    [Fact]
    public async Task Files_kept_for_an_installed_program_at_a_drive_given_up_get_the_screen_naming_the_drive()
    {
        // The same withholding as the all-clear above, except that the check of these
        // files stopped at a drive the scan gave up, so nobody compared them with the
        // packages their program opens. The held-back count leaves them out, as it
        // leaves out every file kept for an installed program, so the line naming the
        // drive is everything the card says about them.
        var vm = CreateViewModel();
        var withheld = new List<OrphanedFile>
        {
            new(@"C:\Windows\Installer\a.msi", 1024, false, false, false, Orphaned),
            new(@"C:\Windows\Installer\b.msi", 2048, false, false, false, Orphaned),
        };
        _scanService.ScanAsync(Arg.Any<IProgress<ScanProgressUpdate>?>(), Arg.Any<CancellationToken>())
            .Returns(new ScanResult(
                Array.Empty<OrphanedFile>(), Array.Empty<RegisteredPackage>(), 0,
                WithheldFiles: withheld,
                WithheldBy: new WithholdingSplit(DeclaredProductInstalledCount: 2),
                SourceRootsGivenUp: [new("d:", SourceRootGiveUpRoute.StoppedWaiting, 2)]));

        await vm.Scan.ScanWithProgressAsync(null);

        Assert.Equal(Strings.Completion_NothingOffered, vm.Completion.Heading);
        Assert.Equal(DriveDLine, vm.Completion.Summary);
        Assert.Equal(DriveDLine, vm.Scan.SourcesGivenUpText);
        Assert.True(vm.Scan.HasSourcesGivenUp);
    }

    [Fact]
    public async Task A_drive_given_up_with_no_file_kept_there_leaves_the_all_clear()
    {
        // Given up after its last read was used: nothing the scan decided turned on it,
        // so the folder is as clean as the all-clear says and nothing names the drive.
        var vm = CreateViewModel();
        _scanService.ScanAsync(Arg.Any<IProgress<ScanProgressUpdate>?>(), Arg.Any<CancellationToken>())
            .Returns(new ScanResult(
                Array.Empty<OrphanedFile>(), Array.Empty<RegisteredPackage>(), 0,
                SourceRootsGivenUp: [new("D:", SourceRootGiveUpRoute.ReadsAddUp, 0)]));

        await vm.Scan.ScanWithProgressAsync(null);

        Assert.Equal(Strings.Completion_AllClean, vm.Completion.Heading);
        Assert.Equal(string.Empty, vm.Scan.SourcesGivenUpText);
        Assert.False(vm.Scan.HasSourcesGivenUp);
    }

    [Fact]
    public async Task Files_held_back_beside_a_drive_given_up_get_the_line_naming_the_drive_alone()
    {
        var vm = CreateViewModel();
        var withheld = new List<OrphanedFile>
        {
            new(@"C:\Windows\Installer\a.msi", 1024, false, false, false, Orphaned),
            new(@"C:\Windows\Installer\b.msi", 2048, false, false, false, Orphaned),
        };
        _scanService.ScanAsync(Arg.Any<IProgress<ScanProgressUpdate>?>(), Arg.Any<CancellationToken>())
            .Returns(new ScanResult(
                Array.Empty<OrphanedFile>(), Array.Empty<RegisteredPackage>(), 0,
                WithheldFiles: withheld,
                WithheldBy: new WithholdingSplit(DeclaredProductInstalledCount: 1, SecondCopyUnestablishedCount: 1),
                SourceRootsGivenUp: [new("D:", SourceRootGiveUpRoute.NoAnswer, 2)]));

        await vm.Scan.ScanWithProgressAsync(null);

        Assert.Equal(Strings.Completion_NothingOffered, vm.Completion.Heading);
        Assert.Equal(DriveDLine, vm.Completion.Summary);
    }

    [Fact]
    public async Task A_scan_that_found_files_shows_the_line_on_the_window_and_a_failed_one_takes_it_away()
    {
        // No card where files are offered: the line stands under the left-alone count
        // for as long as that result is on screen, and goes with the result.
        var vm = CreateViewModel();
        var found = ScanResultWithOrphans(2) with
        {
            SourceRootsGivenUp = [new(@"\\nas\installers", SourceRootGiveUpRoute.SlowFailure, 1)],
        };
        var calls = 0;
        _scanService.ScanAsync(Arg.Any<IProgress<ScanProgressUpdate>?>(), Arg.Any<CancellationToken>())
            .Returns(_ => calls++ == 0
                ? Task.FromResult(found)
                : Task.FromException<ScanResult>(new UnauthorizedAccessException("denied")));

        await vm.Scan.ScanCommand.ExecuteAsync(null);

        Assert.False(vm.Completion.IsComplete);
        Assert.Equal(
            @"InstallerClean carried on without \\nas\installers and left alone any file still to be checked against it. "
            + "Once it's responding normally, Re-scan.",
            vm.Scan.SourcesGivenUpText);

        await vm.Scan.ScanCommand.ExecuteAsync(null);

        Assert.Equal(string.Empty, vm.Scan.SourcesGivenUpText);
        Assert.False(vm.Scan.HasSourcesGivenUp);
    }

    [Fact]
    public async Task Patch_copies_kept_for_their_patch_s_registrations_get_the_all_clear()
    {
        // Nothing offered, and every held file is a patch copy kept because Windows
        // holds a registration of the patch it declares, so the machine gets the screen
        // a clean folder gets.
        var vm = CreateViewModel();
        var withheld = new List<OrphanedFile>
        {
            new(@"C:\Windows\Installer\a.msp", 1024, true, false, false, Orphaned),
            new(@"C:\Windows\Installer\b.msp", 2048, true, false, false, Orphaned),
        };
        _scanService.ScanAsync(Arg.Any<IProgress<ScanProgressUpdate>?>(), Arg.Any<CancellationToken>())
            .Returns(new ScanResult(
                Array.Empty<OrphanedFile>(), Array.Empty<RegisteredPackage>(), 0,
                WithheldFiles: withheld,
                WithheldBy: new WithholdingSplit(DeclaredPatchRegisteredCount: 2)));

        await vm.Scan.ScanWithProgressAsync(null);

        Assert.Equal(Strings.Completion_AllClean, vm.Completion.Heading);
        Assert.NotEqual(Strings.Completion_NothingOffered, vm.Completion.Heading);
    }

    [Fact]
    public async Task A_patch_copy_the_scan_could_not_settle_gets_the_nothing_offered_screen_and_not_the_all_clear()
    {
        // Two held patch copies: one kept for its patch's registrations, which is
        // silent, and one the scan could not settle, which is not. The screen is the
        // nothing-offered one.
        var vm = CreateViewModel();
        var withheld = new List<OrphanedFile>
        {
            new(@"C:\Windows\Installer\a.msp", 1024, true, false, false, Orphaned),
            new(@"C:\Windows\Installer\b.msp", 2048, true, false, false, Orphaned),
        };
        _scanService.ScanAsync(Arg.Any<IProgress<ScanProgressUpdate>?>(), Arg.Any<CancellationToken>())
            .Returns(new ScanResult(
                Array.Empty<OrphanedFile>(), Array.Empty<RegisteredPackage>(), 0,
                WithheldFiles: withheld,
                WithheldBy: new WithholdingSplit(DeclaredPatchRegisteredCount: 1, DeclaredPatchUnestablishedCount: 1)));

        await vm.Scan.ScanWithProgressAsync(null);

        Assert.Equal(Strings.Completion_NothingOffered, vm.Completion.Heading);
        Assert.NotEqual(Strings.Completion_AllClean, vm.Completion.Heading);
        Assert.Equal(string.Empty, vm.Completion.Summary);
    }

    [Fact]
    public async Task A_file_the_scan_could_not_settle_beside_one_kept_for_an_installed_program_gets_the_nothing_offered_screen()
    {
        // Two held: one for an installed program and one the scan could not settle. The
        // second keeps the all-clear off.
        var vm = CreateViewModel();
        var withheld = new List<OrphanedFile>
        {
            new(@"C:\Windows\Installer\a.msi", 1024, false, false, false, Orphaned),
            new(@"C:\Windows\Installer\b.msi", 2048, false, false, false, Orphaned),
        };
        _scanService.ScanAsync(Arg.Any<IProgress<ScanProgressUpdate>?>(), Arg.Any<CancellationToken>())
            .Returns(new ScanResult(
                Array.Empty<OrphanedFile>(), Array.Empty<RegisteredPackage>(), 0,
                WithheldFiles: withheld,
                WithheldBy: new WithholdingSplit(DeclaredProductInstalledCount: 1, ScreenUnansweredCount: 1)));

        await vm.Scan.ScanWithProgressAsync(null);

        Assert.Equal(Strings.Completion_NothingOffered, vm.Completion.Heading);
        Assert.Equal(string.Empty, vm.Completion.Summary);
    }

    // THE FILES THAT KEEP THE ALL-CLEAR OFF THAT THE COMMAND LINE'S APPLICATION LOG ENTRY
    // FOR THE FOLDER WALK DOES NOT COUNT: a file kept for its times and a superseded patch
    // the scan held back.

    [Fact]
    public async Task Files_kept_for_their_times_get_their_line_above_the_receipt_beside_another_hold()
    {
        // Two files kept for their times and one whose times would not read: the line
        // about the first two is the card's line, above its receipt.
        var vm = CreateViewModel();
        var withheld = new List<OrphanedFile>
        {
            new(@"C:\Windows\Installer\a.msi", 1024, false, false, false, Orphaned),
            new(@"C:\Windows\Installer\b.msi", 2048, false, false, false, Orphaned),
            new(@"C:\Windows\Installer\c.msi", 4096, false, false, false, Orphaned),
        };
        var result = new ScanResult(
            Array.Empty<OrphanedFile>(), Array.Empty<RegisteredPackage>(), 0,
            WithheldFiles: withheld,
            WithheldBy: new WithholdingSplit(NotBeforeTheClockCount: 2, AgeUnestablishedCount: 1),
            WithheldNotBeforeTheClockBytes: 3072);
        _scanService.ScanAsync(Arg.Any<IProgress<ScanProgressUpdate>?>(), Arg.Any<CancellationToken>())
            .Returns(result);

        await vm.Scan.ScanWithProgressAsync(null);

        Assert.Equal(Strings.Completion_NothingOffered, vm.Completion.Heading);
        Assert.Equal(RecentChangeReport.Line(result), vm.Completion.Summary);
        Assert.NotEqual(string.Empty, vm.Completion.Summary);
        Assert.Equal(string.Empty, vm.Completion.ScanStartLine);
    }

    [Fact]
    public async Task A_file_kept_for_its_times_beside_one_kept_for_an_installed_program_gets_its_line_and_not_the_all_clear()
    {
        var vm = CreateViewModel();
        var withheld = new List<OrphanedFile>
        {
            new(@"C:\Windows\Installer\a.msi", 1024, false, false, false, Orphaned),
            new(@"C:\Windows\Installer\b.msi", 2048, false, false, false, Orphaned),
            new(@"C:\Windows\Installer\c.msi", 4096, false, false, false, Orphaned),
        };
        _scanService.ScanAsync(Arg.Any<IProgress<ScanProgressUpdate>?>(), Arg.Any<CancellationToken>())
            .Returns(new ScanResult(
                Array.Empty<OrphanedFile>(), Array.Empty<RegisteredPackage>(), 0,
                WithheldFiles: withheld,
                WithheldBy: new WithholdingSplit(
                    DeclaredProductInstalledCount: 1, NotBeforeTheClockCount: 1, AgeUnestablishedCount: 1),
                WithheldNotBeforeTheClockBytes: 2048));

        await vm.Scan.ScanWithProgressAsync(null);

        Assert.Equal(Strings.Completion_NothingOffered, vm.Completion.Heading);
        Assert.Equal(RecentChangeReport.Line(vm.Scan.LastScanResult), vm.Completion.Summary);
    }

    [Fact]
    public async Task Superseded_files_held_back_get_the_nothing_offered_screen_and_not_the_all_clear()
    {
        // The rows are on the kept list with their files on disk, as the scan leaves
        // them, so the receipt counts them among the files the scan examined.
        var vm = CreateViewModel();
        var kept = new[]
        {
            new RegisteredPackage(@"C:\Windows\Installer\p1.msp", "Contoso", "{aaa}",
                PatchState: 2, RemovableWithheld: true, FileSizeBytes: 1000),
            new RegisteredPackage(@"C:\Windows\Installer\p2.msp", "Contoso", "{aaa}",
                PatchState: 2, RemovableWithheld: true, FileSizeBytes: 3000),
            new RegisteredPackage(@"C:\Windows\Installer\p3.msp", "Contoso", "{aaa}",
                PatchState: 2, RemovableWithheld: true, FileSizeBytes: 5000),
        };
        _scanService.ScanAsync(Arg.Any<IProgress<ScanProgressUpdate>?>(), Arg.Any<CancellationToken>())
            .Returns(new ScanResult(
                Array.Empty<OrphanedFile>(), kept, 9000,
                WithheldCount: 3,
                WithheldFiles: Array.Empty<OrphanedFile>()));

        await vm.Scan.ScanWithProgressAsync(null);

        Assert.Equal(Strings.Completion_NothingOffered, vm.Completion.Heading);
        Assert.Equal(string.Empty, vm.Completion.Summary);
        Assert.StartsWith("Scanned 3 files in ", vm.Completion.Restore, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Files_the_containment_check_kept_back_get_the_nothing_offered_screen_and_not_the_all_clear()
    {
        // Two files from the folder walk, one the check refused and one it could not
        // answer for, and nothing else held back. They are on the left-alone line, so
        // the receipt counts them.
        var vm = CreateViewModel();
        var withheld = new List<OrphanedFile>
        {
            new(@"C:\Windows\Installer\link.msi", 1024, false, false, false, Orphaned),
            new(@"C:\Windows\Installer\unread.msp", 2048, true, false, false, Orphaned),
        };
        _scanService.ScanAsync(Arg.Any<IProgress<ScanProgressUpdate>?>(), Arg.Any<CancellationToken>())
            .Returns(new ScanResult(
                Array.Empty<OrphanedFile>(), Array.Empty<RegisteredPackage>(), 0,
                WithheldFiles: withheld,
                WithheldBy: new WithholdingSplit(ContainmentRefusedCount: 1, ContainmentUnestablishedCount: 1)));

        await vm.Scan.ScanWithProgressAsync(null);

        Assert.Equal(2, vm.Scan.RegisteredFileCount);
        Assert.Equal(Strings.Completion_NothingOffered, vm.Completion.Heading);
        Assert.Equal(string.Empty, vm.Completion.Summary);
        Assert.StartsWith("Scanned 2 files in ", vm.Completion.Restore, StringComparison.Ordinal);
    }

    [Fact]
    public async Task A_superseded_patch_the_containment_check_refused_gets_the_nothing_offered_screen_and_not_the_all_clear()
    {
        // The row carried its removable verdict to the offer and the check refused it,
        // so it stays on the kept list with its file on disk, as the scan leaves it.
        var vm = CreateViewModel();
        var kept = new[]
        {
            new RegisteredPackage(@"C:\Windows\Installer\p1.msp", "Contoso", "{aaa}",
                PatchState: 2, IsRemovable: true, ProductPatchSetVerdict: ProductPatchSet.AllNonRemovable,
                FileSizeBytes: 7000),
        };
        _scanService.ScanAsync(Arg.Any<IProgress<ScanProgressUpdate>?>(), Arg.Any<CancellationToken>())
            .Returns(new ScanResult(
                Array.Empty<OrphanedFile>(), kept, 7000,
                WithheldFiles: Array.Empty<OrphanedFile>(),
                SupersededContainmentRefusedCount: 1));

        await vm.Scan.ScanWithProgressAsync(null);

        Assert.Equal(1, vm.Scan.RegisteredFileCount);
        Assert.Equal(Strings.Completion_NothingOffered, vm.Completion.Heading);
        Assert.Equal(string.Empty, vm.Completion.Summary);
        Assert.StartsWith("Scanned 1 file in ", vm.Completion.Restore, StringComparison.Ordinal);
    }

    [Fact]
    public async Task One_superseded_file_held_back_gets_the_nothing_offered_screen()
    {
        var vm = CreateViewModel();
        _scanService.ScanAsync(Arg.Any<IProgress<ScanProgressUpdate>?>(), Arg.Any<CancellationToken>())
            .Returns(new ScanResult(
                Array.Empty<OrphanedFile>(), Array.Empty<RegisteredPackage>(), 0,
                WithheldCount: 1));

        await vm.Scan.ScanWithProgressAsync(null);

        Assert.Equal(Strings.Completion_NothingOffered, vm.Completion.Heading);
        Assert.Equal(string.Empty, vm.Completion.Summary);
    }

    [Fact]
    public async Task A_wholesale_withholding_beside_a_superseded_file_gets_the_nothing_offered_screen()
    {
        var vm = CreateViewModel();
        var withheld = new List<OrphanedFile>
        {
            new(@"C:\Windows\Installer\a.msi", 1024, false, false, false, Orphaned),
            new(@"C:\Windows\Installer\b.msi", 2048, false, false, false, Orphaned),
        };
        _scanService.ScanAsync(Arg.Any<IProgress<ScanProgressUpdate>?>(), Arg.Any<CancellationToken>())
            .Returns(new ScanResult(
                Array.Empty<OrphanedFile>(), Array.Empty<RegisteredPackage>(), 0,
                WithheldCount: 1,
                WithheldFiles: withheld,
                WithheldBy: new WithholdingSplit(WholesaleCount: 2)));

        await vm.Scan.ScanWithProgressAsync(null);

        Assert.Equal(Strings.Completion_NothingOffered, vm.Completion.Heading);
        Assert.Equal(string.Empty, vm.Completion.Summary);
    }

    [Fact]
    public async Task A_wholesale_withholding_gets_the_nothing_offered_screen_with_no_sentence_naming_it()
    {
        var vm = CreateViewModel();
        var withheld = new List<OrphanedFile>
        {
            new(@"C:\Windows\Installer\a.msi", 1024, false, false, false, Orphaned),
            new(@"C:\Windows\Installer\b.msi", 2048, false, false, false, Orphaned),
        };
        _scanService.ScanAsync(Arg.Any<IProgress<ScanProgressUpdate>?>(), Arg.Any<CancellationToken>())
            .Returns(new ScanResult(
                Array.Empty<OrphanedFile>(), Array.Empty<RegisteredPackage>(), 0,
                WithheldFiles: withheld,
                WithheldBy: new WithholdingSplit(WholesaleCount: 2)));

        await vm.Scan.ScanWithProgressAsync(null);

        Assert.Equal(Strings.Completion_NothingOffered, vm.Completion.Heading);
        Assert.Equal(string.Empty, vm.Completion.Summary);
    }

    [Fact]
    public async Task Records_that_did_not_all_read_get_the_all_clear_where_nothing_was_held_back()
    {
        // The count of products the scan could not account for is a trigger for
        // withholding superseded files, not a count of files held back. Set high here
        // with nothing held back, so a screen gated on it rather than on the files
        // fails this.
        var vm = CreateViewModel();
        _scanService.ScanAsync(Arg.Any<IProgress<ScanProgressUpdate>?>(), Arg.Any<CancellationToken>())
            .Returns(new ScanResult(
                Array.Empty<OrphanedFile>(), Array.Empty<RegisteredPackage>(), 0,
                UnaccountedProductCount: 3,
                WithheldCount: 0));

        await vm.Scan.ScanWithProgressAsync(null);

        Assert.Equal(Strings.Completion_AllClean, vm.Completion.Heading);
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
        // Grouped, because a count on a screen is written the way the reader's
        // region writes one. The comma is English's; the same line reads
        // "10.000" in German and "10 000" in French.
        Assert.Equal("10,000 unneeded files to clean up", vm.Scan.OrphanedSummaryText);
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

        // Against the resx and never a copy of the English: a fragment written out
        // here goes stale the moment the string is reworded, which is what this
        // assertion has always been guarding. StartsWith against the constant keeps
        // that guard and drops a different one.
        //
        // WHAT IT STOPS DEPENDING ON IS THE MACHINE. The ladder appends a sentence
        // naming the crash log where one was written and omits it where none was, so
        // an equality here asks the host whether it can write to LocalApplicationData
        // and reads the answer as a verdict on the view model. The composition itself
        // is pinned by ScanViewModelStoppedScanTests, which asks that question first
        // and asserts whichever form the host can reach.
        _dialogService.Received(1).ShowError(
            Arg.Is<string>(m => m != null
                && m.StartsWith(Strings.Error_InstallerDbEmpty, StringComparison.Ordinal)),
            Strings.Error_StoppedTitle);
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
    public async Task The_Details_buttons_are_out_of_use_from_the_first_instant_of_a_scan()
    {
        var vm = CreateViewModel();
        _scanService.ScanAsync(Arg.Any<IProgress<ScanProgressUpdate>?>(), Arg.Any<CancellationToken>())
            .Returns(ScanResultWithOrphans(2));
        await vm.Scan.ScanWithProgressAsync(null);
        Assert.True(vm.Chrome.OpenOrphanedDetailsCommand.CanExecute(null));
        Assert.True(vm.Chrome.OpenRegisteredDetailsCommand.CanExecute(null));

        // A button asks CanExecute again only when its command raises
        // CanExecuteChanged, so the last answer recorded is the one it shows.
        var orphanedAnswers = new List<bool>();
        var registeredAnswers = new List<bool>();
        vm.Chrome.OpenOrphanedDetailsCommand.CanExecuteChanged += (_, _) =>
            orphanedAnswers.Add(vm.Chrome.OpenOrphanedDetailsCommand.CanExecute(null));
        vm.Chrome.OpenRegisteredDetailsCommand.CanExecuteChanged += (_, _) =>
            registeredAnswers.Add(vm.Chrome.OpenRegisteredDetailsCommand.CanExecute(null));

        var release = new ManualResetEventSlim();
        _scanService.ScanAsync(Arg.Any<IProgress<ScanProgressUpdate>?>(), Arg.Any<CancellationToken>())
            .Returns(_ => Task.Run(() =>
            {
                release.Wait();
                return ScanResultWithOrphans(3);
            }));

        var scan = vm.Scan.ScanCommand.ExecuteAsync(null);

        // Inside the 200 ms before the overlay disables the window, the previous
        // result is still there to open. A window opened from it now would keep
        // that set after this scan replaces it.
        Assert.False(vm.Scan.IsScanning);
        Assert.True(vm.Scan.IsScanInFlight);
        Assert.NotNull(vm.Scan.LastScanResult);
        Assert.False(vm.Chrome.OpenOrphanedDetailsCommand.CanExecute(null));
        Assert.False(vm.Chrome.OpenRegisteredDetailsCommand.CanExecute(null));
        Assert.NotEmpty(orphanedAnswers);
        Assert.False(orphanedAnswers[^1]);
        Assert.NotEmpty(registeredAnswers);
        Assert.False(registeredAnswers[^1]);

        release.Set();
        await scan;

        Assert.False(vm.Scan.IsScanInFlight);
        Assert.True(vm.Chrome.OpenOrphanedDetailsCommand.CanExecute(null));
        Assert.True(vm.Chrome.OpenRegisteredDetailsCommand.CanExecute(null));
        Assert.True(orphanedAnswers[^1]);
        Assert.True(registeredAnswers[^1]);
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
    public async Task Rescan_is_refused_under_the_completion_overlay_and_runs_after_Done()
    {
        var vm = CreateViewModel();
        _scanService.ScanAsync(Arg.Any<IProgress<ScanProgressUpdate>?>(), Arg.Any<CancellationToken>())
            .Returns(EmptyScanResult());

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
    public async Task Esc_closes_the_finished_card_while_the_Delete_behind_it_is_still_finishing()
    {
        var vm = CreateViewModel();
        _scanService.ScanAsync(Arg.Any<IProgress<ScanProgressUpdate>?>(), Arg.Any<CancellationToken>())
            .Returns(ScanResultWithOrphans(1));
        _deleteService.DeleteFilesAsync(
                Arg.Any<IEnumerable<string>>(), Arg.Any<UnderLeaseClaims>(),
                Arg.Any<IProgress<OperationProgress>?>(), Arg.Any<CancellationToken>())
            .Returns(new DeleteResult(1, Array.Empty<FileOperationError>()));
        _confirmationService.ConfirmDelete(Arg.Any<int>(), Arg.Any<string>()).Returns(true);

        // Esc is pressed from inside the report write, which the Delete awaits after
        // putting its card up and before it lets go: the moment both overlays are up.
        bool cardUp = false, operating = false, taken = false, cancelRequested = true;
        _resultLogService.WriteAsync(Arg.Any<ResultLogEntry>(), Arg.Any<CancellationToken>())
            .Returns(_ =>
            {
                cardUp = vm.Completion.IsComplete;
                operating = vm.Cleanup.IsOperating;
                taken = vm.HandleEscape();
                cancelRequested = vm.Cleanup.IsCancellationRequested;
                return Task.FromResult(true);
            });

        await vm.Scan.ScanWithProgressAsync(null);
        await vm.Cleanup.DeleteAllCommand.ExecuteAsync(null);

        // The fixture reached the moment it is for.
        Assert.True(cardUp);
        Assert.True(operating);
        // The card took the Esc and the finished Delete was not asked to stop.
        Assert.True(taken);
        Assert.False(cancelRequested);
        Assert.False(vm.Completion.IsComplete);
    }

    [Fact]
    public async Task Esc_asks_a_running_Delete_to_stop_where_no_card_is_up()
    {
        var vm = CreateViewModel();
        _scanService.ScanAsync(Arg.Any<IProgress<ScanProgressUpdate>?>(), Arg.Any<CancellationToken>())
            .Returns(ScanResultWithOrphans(2));
        bool cardUp = true, taken = false, cancelRequested = false;
        _deleteService.DeleteFilesAsync(
                Arg.Any<IEnumerable<string>>(), Arg.Any<UnderLeaseClaims>(),
                Arg.Any<IProgress<OperationProgress>?>(), Arg.Any<CancellationToken>())
            .Returns(_ =>
            {
                cardUp = vm.Completion.IsComplete;
                taken = vm.HandleEscape();
                cancelRequested = vm.Cleanup.IsCancellationRequested;
                return new DeleteResult(0, Array.Empty<FileOperationError>(), Cancelled: true);
            });
        _confirmationService.ConfirmDelete(Arg.Any<int>(), Arg.Any<string>()).Returns(true);

        await vm.Scan.ScanWithProgressAsync(null);
        await vm.Cleanup.DeleteAllCommand.ExecuteAsync(null);

        Assert.False(cardUp);
        Assert.True(taken);
        Assert.True(cancelRequested);
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
                Arg.Any<IEnumerable<string>>(), Arg.Any<string>(), Arg.Any<UnderLeaseClaims>(),
                Arg.Any<IProgress<OperationProgress>?>(), Arg.Any<CancellationToken>())
            .Returns(new MoveResult(2, Array.Empty<FileOperationError>()));
        _confirmationService.ConfirmMove(
            Arg.Any<int>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<bool>()).Returns(true);

        await vm.Scan.ScanWithProgressAsync(null);
        vm.Cleanup.MoveDestination = Path.Combine(Path.GetTempPath(), "ic-test-move");

        await vm.Cleanup.MoveAllCommand.ExecuteAsync(null);

        _confirmationService.Received(1).ConfirmMove(2, Arg.Any<string>(), vm.Cleanup.MoveDestination, Arg.Any<bool>());
        await _moveService.Received(1).MoveFilesAsync(
            Arg.Any<IEnumerable<string>>(), vm.Cleanup.MoveDestination, Arg.Any<UnderLeaseClaims>(),
            Arg.Any<IProgress<OperationProgress>?>(), Arg.Any<CancellationToken>());
        Assert.True(vm.Completion.IsComplete);
        // %TEMP% and the installer cache are on one volume on every host this
        // runs on, so this classifies as a same-volume move and the heading
        // claims "moved", not "freed". WHAT THE DESTINATION IS MEASURED AGAINST
        // IS THE CACHE'S VOLUME, never the system drive.
        //
        // AND THIS ASSERTION IS FALSE WHERE THOSE TWO PART. With a volume
        // mounted at C:\Windows\Installer, %TEMP% is not on the cache's volume,
        // the move genuinely frees space, the heading reads "freed" and this
        // fails. There is no path arithmetic here to correct: the fixture names
        // %TEMP% and the coincidence is the machine's rather than the test's.
        // Nor can it be pointed at the cache folder, which Move refuses as
        // inside the installer cache before the classification is reached.
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
            Arg.Any<IEnumerable<string>>(), Arg.Any<string>(), Arg.Any<UnderLeaseClaims>(),
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
                Arg.Any<IEnumerable<string>>(), Arg.Any<UnderLeaseClaims>(),
                Arg.Any<IProgress<OperationProgress>?>(), Arg.Any<CancellationToken>())
            .Returns(new DeleteResult(1, Array.Empty<FileOperationError>()));
        _confirmationService.ConfirmDelete(
            Arg.Any<int>(), Arg.Any<string>()).Returns(true);

        await vm.Scan.ScanWithProgressAsync(null);

        await vm.Cleanup.DeleteAllCommand.ExecuteAsync(null);

        _confirmationService.Received(1).ConfirmDelete(1, Arg.Any<string>());
        await _deleteService.Received(1).DeleteFilesAsync(
            Arg.Any<IEnumerable<string>>(), Arg.Any<UnderLeaseClaims>(),
            Arg.Any<IProgress<OperationProgress>?>(), Arg.Any<CancellationToken>());
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
                Arg.Any<IEnumerable<string>>(), Arg.Any<string>(), Arg.Any<UnderLeaseClaims>(),
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
                Arg.Any<IEnumerable<string>>(), Arg.Any<string>(), Arg.Any<UnderLeaseClaims>(),
                Arg.Any<IProgress<OperationProgress>?>(), Arg.Any<CancellationToken>())
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
                Arg.Any<IEnumerable<string>>(), Arg.Any<UnderLeaseClaims>(),
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
    public async Task DeleteAllAsync_crash_rescans_and_stops_describing_the_batch_it_lost()
    {
        var vm = CreateViewModel();
        _scanService.ScanAsync(Arg.Any<IProgress<ScanProgressUpdate>?>(), Arg.Any<CancellationToken>())
            .Returns(ScanResultWithOrphans(3));
        _deleteService.DeleteFilesAsync(
                Arg.Any<IEnumerable<string>>(), Arg.Any<UnderLeaseClaims>(),
                Arg.Any<IProgress<OperationProgress>?>(), Arg.Any<CancellationToken>())
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
            Arg.Any<IEnumerable<string>>(), Arg.Any<UnderLeaseClaims>(),
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
        // The service returns its partial result with Cancelled set rather than
        // throwing the tally away: two moved before the stop.
        _moveService.MoveFilesAsync(
                Arg.Any<IEnumerable<string>>(), Arg.Any<string>(), Arg.Any<UnderLeaseClaims>(),
                Arg.Any<IProgress<OperationProgress>?>(), Arg.Any<CancellationToken>())
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
        // A cancelled run writes no result-log entry, which keeps the public
        // reports stats meaning what they mean.
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
                Arg.Any<IEnumerable<string>>(), Arg.Any<UnderLeaseClaims>(),
                Arg.Any<IProgress<OperationProgress>?>(), Arg.Any<CancellationToken>())
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
    public async Task MoveButtonTooltip_answers_from_the_box_and_changes_as_it_is_typed_in()
    {
        var vm = CreateViewModel();
        // THE CACHE FOLDER'S PATH ROOT, WHICH IS NOT THE CACHE'S VOLUME. It was
        // the system directory's path root until the question stopped being
        // about the system directory, and on every host this suite runs on all
        // three are the same string, so nothing here behaves differently and
        // nothing here can tell them apart.
        //
        // THE SAME-DRIVE ASSERTION BELOW IS FALSE WHERE THEY PART. With a
        // volume mounted at C:\Windows\Installer the folder built from this root
        // is on another volume, so DestinationIsOnCacheVolume lands false, the
        // tooltip keeps the plain wording and the Tooltip_MoveSameDrive
        // assertion fails.
        //
        // A FIXTURE UNDER THE CACHE FOLDER WOULD ANSWER SAME-DRIVE ON EITHER
        // HOST, AND WHAT IT WOULD PIN IS A STATE A USER CAN REACH. Nothing on
        // this path runs the destination validation the Move does, so somebody
        // who types C:\Windows\Installer\backup gets this tooltip, correctly.
        // What no user can reach is a MOVE there, which the pre-flight refuses
        // as inside the installer cache. Which fixture this test should stand
        // on is open and nothing here settles it.
        var cachePathRoot = Path.GetPathRoot(InstallerCacheHelpers.InstallerFolder)!;

        // Empty box: the browser opens first, so the tooltip warns of it rather
        // than naming a step that is missing.
        Assert.Equal(Strings.Tooltip_MoveNeedsDestination, vm.Cleanup.MoveButtonTooltip);

        // A folder on the volume the cached files are already on: a rename, so
        // it says when the space actually comes back. This is the state that
        // pairs the button with Delete beside it.
        //
        // THE WAIT IS THE BEHAVIOUR, NOT A TEST WORKAROUND. From 3.0.0 the
        // same-volume question goes to Windows, and Win32 validates a remote
        // path over the network, so it cannot be asked on the dispatcher for
        // every keystroke. It is asked on the debounce the destination's own
        // write-back already uses, and until it answers the tooltip shows the
        // plain wording, which claims nothing about any drive.
        vm.Cleanup.MoveDestination = Path.Combine(cachePathRoot, "ic-test-backup");
        Assert.Equal(Strings.Tooltip_Move, vm.Cleanup.MoveButtonTooltip);
        await Task.Delay(DebounceWait);
        Assert.Equal(Strings.Tooltip_MoveSameDrive, vm.Cleanup.MoveButtonTooltip);

        // Anywhere else, including a share, takes the plain wording. Asserted
        // straight away AND after the window, because the answer this replaces
        // has to be dropped on the keystroke rather than only on the reply: a
        // share left reading "same drive" for a debounce would be the old fault
        // in a new place.
        vm.Cleanup.MoveDestination = @"\\server\backup";
        Assert.Equal(Strings.Tooltip_Move, vm.Cleanup.MoveButtonTooltip);
        await Task.Delay(DebounceWait);
        Assert.Equal(Strings.Tooltip_Move, vm.Cleanup.MoveButtonTooltip);

        // Back to empty, because the box is a TextBox and a user can clear it.
        vm.Cleanup.MoveDestination = string.Empty;
        Assert.Equal(Strings.Tooltip_MoveNeedsDestination, vm.Cleanup.MoveButtonTooltip);
    }

    [Fact]
    public void MoveButtonTooltip_speaks_of_the_drive_only_where_Windows_has_named_it()
    {
        // The selector alone, driven from the flag instead of from a real folder,
        // so every value of it is reachable on any host. Only true reaches the
        // wording that mentions space. False, a question still in flight and a
        // question Windows will not answer all take the plain wording, and the
        // last two of those three cannot be produced by the timing test above.
        var vm = CreateViewModel();

        // A path that exists only as a string here. Setting it schedules a
        // resolve, which sits in its debounce for the length of this test and
        // never publishes over the values set below.
        vm.Cleanup.MoveDestination = @"D:\ic-test-backup";

        vm.Cleanup.DestinationIsOnCacheVolume = null;
        Assert.Equal(Strings.Tooltip_Move, vm.Cleanup.MoveButtonTooltip);

        vm.Cleanup.DestinationIsOnCacheVolume = true;
        Assert.Equal(Strings.Tooltip_MoveSameDrive, vm.Cleanup.MoveButtonTooltip);

        vm.Cleanup.DestinationIsOnCacheVolume = false;
        Assert.Equal(Strings.Tooltip_Move, vm.Cleanup.MoveButtonTooltip);

        // An empty box outranks a volume answer, and the flag is set AFTER the
        // box so that there is one to outrank. Clearing the box nulls the flag
        // on its own, so the order of these two lines is what makes this the
        // precedence rather than the empty branch standing on its own.
        vm.Cleanup.MoveDestination = string.Empty;
        vm.Cleanup.DestinationIsOnCacheVolume = true;
        Assert.Equal(Strings.Tooltip_MoveNeedsDestination, vm.Cleanup.MoveButtonTooltip);
    }

    [Fact]
    public async Task MoveButtonTooltip_drops_a_stale_answer_when_the_box_moves_on()
    {
        // The resolve is started per edit and belongs to the path that started
        // it. Typing a folder the resolve will call same-drive and then a share,
        // faster than the debounce, must leave the share's answer standing: if
        // the first resolve could land late it would tell the user a folder on
        // another volume was on this one, which is the fault this change
        // removes, reintroduced by the machinery that made it cheap.
        var vm = CreateViewModel();
        // THE FIXTURE'S VOLUME CANNOT AFFECT THIS TEST ON ANY HOST, AND THE
        // TWO LINES BELOW ARE WHY. They are adjacent synchronous property sets,
        // so the first resolve is still parked in its debounce when the second
        // cancels it: the Task.Delay throws, ResolveDestinationVolumeAsync
        // returns, and ResolveIsOnInstallerCacheDrive is never called for that
        // path.
        // The string is built from the cache folder's path root as in
        // MoveButtonTooltip_answers_from_the_box_and_changes_as_it_is_typed_in;
        // here it names a folder nothing asks a question about.
        //
        // WHAT THIS SEQUENCE FIRES IS THE CANCEL. Two things keep a stale answer
        // off the tooltip: ScheduleDestinationVolumeResolve cancelling the resolve
        // in flight, which is what these two lines fire, and the token re-check on
        // the way back, which belongs to a resolve whose debounce had already
        // elapsed. Here the cancelled resolve dies parked in its debounce and
        // never reaches that re-check; the surviving resolve reaches it and passes
        // through. Putting an answer in flight rather than parked is what
        // CleanupDestinationVolumeTests does, by taking the volume question as a
        // function so an answer can be held open across the keystroke that makes
        // it stale; two adjacent property sets leave the earlier resolve parked,
        // and that is the state this one is for.
        //
        // A KEYSTROKE ARRIVING AFTER AN ANSWER HAS LANDED IS A DIFFERENT PATH,
        // and it is the one
        // MoveButtonTooltip_answers_from_the_box_and_changes_as_it_is_typed_in
        // takes when it sets a share over a landed same-drive answer and reads the
        // tooltip before the next resolve can reply. What drops the answer there
        // is the unconditional DestinationIsOnCacheVolume = null at the top of
        // OnMoveDestinationChanged: the publish that landed the answer nulled the
        // token source on its way out, so by then there is nothing to cancel.
        var cachePathRoot = Path.GetPathRoot(InstallerCacheHelpers.InstallerFolder)!;

        vm.Cleanup.MoveDestination = Path.Combine(cachePathRoot, "ic-test-backup");
        vm.Cleanup.MoveDestination = @"\\server\backup";

        await Task.Delay(DebounceWait);

        Assert.Equal(Strings.Tooltip_Move, vm.Cleanup.MoveButtonTooltip);
    }

    [Fact]
    public async Task DeleteAllAsync_cancelled_rearms_cancel_and_stops_describing_the_batch_it_stopped()
    {
        var vm = CreateViewModel();
        _scanService.ScanAsync(Arg.Any<IProgress<ScanProgressUpdate>?>(), Arg.Any<CancellationToken>())
            .Returns(ScanResultWithOrphans(3));
        _deleteService.DeleteFilesAsync(
                Arg.Any<IEnumerable<string>>(), Arg.Any<UnderLeaseClaims>(),
                Arg.Any<IProgress<OperationProgress>?>(), Arg.Any<CancellationToken>())
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

    /// <summary>
    /// What the overlay's Cancel button shows: the answer CanExecute gave when
    /// CanExecuteChanged was last raised, which is the only time the button asks.
    /// Null where nothing has been raised since this was made.
    /// </summary>
    private sealed class CancelButtonState
    {
        public CancelButtonState(CleanupViewModel cleanup) =>
            cleanup.CancelOperationCommand.CanExecuteChanged += (_, _) =>
                Live = cleanup.CancelOperationCommand.CanExecute(null);

        public bool? Live { get; private set; }
    }

    [Fact]
    public async Task Cancel_is_live_through_the_rescan_after_a_Delete_and_out_of_use_through_the_wait_for_the_report()
    {
        // A PC's first Delete, with the start check still reading as the card is about
        // to go up, so the overlay waits on it with the batch and the rescan over.
        var vm = FirstRunAllClear();
        _scanService.ScanAsync(Arg.Any<IProgress<ScanProgressUpdate>?>(), Arg.Any<CancellationToken>())
            .Returns(ScanResultWithOrphans(2));
        _deleteService.DeleteFilesAsync(
                Arg.Any<IEnumerable<string>>(), Arg.Any<UnderLeaseClaims>(),
                Arg.Any<IProgress<OperationProgress>?>(), Arg.Any<CancellationToken>())
            .Returns(new DeleteResult(2, Array.Empty<FileOperationError>()));
        _confirmationService.ConfirmDelete(Arg.Any<int>(), Arg.Any<string>()).Returns(true);
        await vm.Scan.ScanWithProgressAsync(null);

        var button = new CancelButtonState(vm.Cleanup);
        bool? liveInRescan = null;
        _scanService.ScanAsync(Arg.Any<IProgress<ScanProgressUpdate>?>(), Arg.Any<CancellationToken>())
            .Returns(_ =>
            {
                liveInRescan = button.Live;
                return EmptyScanResult();
            });
        var answer = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        bool? operating = null, canCancel = null, liveInWait = null, escTaken = null;
        string? heading = null;
        _earlierRunCheck.ShowsAnEarlierRunWithinAsync(Arg.Any<TimeSpan>()).Returns(_ =>
        {
            operating = vm.Cleanup.IsOperating;
            canCancel = vm.Cleanup.CancelOperationCommand.CanExecute(null);
            liveInWait = button.Live;
            escTaken = vm.HandleEscape();
            heading = vm.Cleanup.OperationProgress;
            return answer.Task;
        });

        var delete = vm.Cleanup.DeleteAllCommand.ExecuteAsync(null);
        answer.SetResult(false);
        await delete;

        // The rescan can be stopped, and the button says so.
        Assert.True(liveInRescan);
        // The wait for the report cannot, so the button is out of use and Esc goes
        // nowhere, with the overlay still up and naming nothing, the scan being over.
        Assert.True(operating);
        Assert.False(canCancel);
        Assert.False(liveInWait);
        Assert.False(escTaken);
        Assert.Equal(string.Empty, heading);
        Assert.True(vm.Completion.IsComplete);
        Assert.True(vm.Completion.OffersReport);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Cancel_is_live_through_the_rescan_after_a_Move_or_Delete_that_was_cancelled(bool deleting)
    {
        var vm = CreateViewModel();
        _scanService.ScanAsync(Arg.Any<IProgress<ScanProgressUpdate>?>(), Arg.Any<CancellationToken>())
            .Returns(ScanResultWithOrphans(2));
        _confirmationService.ConfirmMove(
            Arg.Any<int>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<bool>()).Returns(true);
        _confirmationService.ConfirmDelete(Arg.Any<int>(), Arg.Any<string>()).Returns(true);
        await vm.Scan.ScanWithProgressAsync(null);
        vm.Cleanup.MoveDestination = Path.Combine(Path.GetTempPath(), "ic-test-cancelled-batch-rescan");

        var button = new CancelButtonState(vm.Cleanup);
        bool? liveInBatch = null, liveAfterPress = null, liveInRescan = null;
        void PressDuringTheBatch()
        {
            liveInBatch = button.Live;
            vm.Cleanup.CancelOperationCommand.Execute(null);
            liveAfterPress = button.Live;
        }
        _moveService.MoveFilesAsync(
                Arg.Any<IEnumerable<string>>(), Arg.Any<string>(), Arg.Any<UnderLeaseClaims>(),
                Arg.Any<IProgress<OperationProgress>?>(), Arg.Any<CancellationToken>())
            .Returns(_ =>
            {
                PressDuringTheBatch();
                return new MoveResult(1, Array.Empty<FileOperationError>(), Cancelled: true);
            });
        _deleteService.DeleteFilesAsync(
                Arg.Any<IEnumerable<string>>(), Arg.Any<UnderLeaseClaims>(),
                Arg.Any<IProgress<OperationProgress>?>(), Arg.Any<CancellationToken>())
            .Returns(_ =>
            {
                PressDuringTheBatch();
                return new DeleteResult(1, Array.Empty<FileOperationError>(), Cancelled: true);
            });
        _scanService.ScanAsync(Arg.Any<IProgress<ScanProgressUpdate>?>(), Arg.Any<CancellationToken>())
            .Returns(_ =>
            {
                liveInRescan = button.Live;
                return EmptyScanResult();
            });
        bool? liveAtTheCard = null;
        vm.Completion.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName == nameof(CompletionViewModel.IsComplete) && vm.Completion.IsComplete)
                liveAtTheCard = button.Live;
        };

        if (deleting) await vm.Cleanup.DeleteAllCommand.ExecuteAsync(null);
        else await vm.Cleanup.MoveAllCommand.ExecuteAsync(null);

        // The press puts the button out of use for the rest of the batch, the
        // rescan after it, which the press did not stop, brings it back, and the
        // card that follows the rescan has nothing left for it to stop.
        Assert.True(liveInBatch);
        Assert.False(liveAfterPress);
        Assert.True(liveInRescan);
        Assert.False(liveAtTheCard);
    }

    /// <summary>
    /// Sets both batches to run <paramref name="during"/> and then return
    /// <paramref name="done"/> files done, <paramref name="errored"/> files failed, and
    /// cancelled or not as <paramref name="cancelled"/> says. The scan finds two files.
    /// </summary>
    private void TwoFileBatch(Action<CancellationToken> during, int done, int errored, bool cancelled)
    {
        _scanService.ScanAsync(Arg.Any<IProgress<ScanProgressUpdate>?>(), Arg.Any<CancellationToken>())
            .Returns(ScanResultWithOrphans(2));
        _confirmationService.ConfirmMove(
            Arg.Any<int>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<bool>()).Returns(true);
        _confirmationService.ConfirmDelete(Arg.Any<int>(), Arg.Any<string>()).Returns(true);
        FileOperationError[] Errors() => Enumerable.Range(0, errored)
            .Select(i => (FileOperationError)new FileInUse($@"C:\Windows\Installer\orphan{i}.msi"))
            .ToArray();
        _moveService.MoveFilesAsync(
                Arg.Any<IEnumerable<string>>(), Arg.Any<string>(), Arg.Any<UnderLeaseClaims>(),
                Arg.Any<IProgress<OperationProgress>?>(), Arg.Any<CancellationToken>())
            .Returns(ci =>
            {
                during((CancellationToken)ci[4]);
                return new MoveResult(done, Errors(), Cancelled: cancelled);
            });
        _deleteService.DeleteFilesAsync(
                Arg.Any<IEnumerable<string>>(), Arg.Any<UnderLeaseClaims>(),
                Arg.Any<IProgress<OperationProgress>?>(), Arg.Any<CancellationToken>())
            .Returns(ci =>
            {
                during((CancellationToken)ci[3]);
                return new DeleteResult(done, Errors(), Cancelled: cancelled);
            });
    }

    private static Task RunBatch(MainViewModel vm, bool deleting) =>
        (deleting ? vm.Cleanup.DeleteAllCommand.ExecuteAsync(null) : vm.Cleanup.MoveAllCommand.ExecuteAsync(null))
            .WaitAsync(TimeSpan.FromSeconds(10));

    [Theory]
    [InlineData(false, false)]
    [InlineData(true, false)]
    [InlineData(false, true)]
    [InlineData(true, true)]
    public async Task A_close_during_a_Move_or_Delete_runs_no_rescan_and_takes_the_list_off_the_window(
        bool deleting, bool cancelPressedFirst)
    {
        var vm = CreateViewModel();
        bool? batchStopped = null;
        TwoFileBatch(token =>
        {
            if (cancelPressedFirst) vm.Cleanup.CancelOperationCommand.Execute(null);
            vm.Cleanup.RequestClose();
            batchStopped = token.IsCancellationRequested;
        }, done: 1, errored: 0, cancelled: true);
        await vm.Scan.ScanWithProgressAsync(null);
        vm.Cleanup.MoveDestination = Path.Combine(Path.GetTempPath(), "ic-test-close-during-batch");

        await RunBatch(vm, deleting);

        // The batch is stopped, by the close or by the Cancel pressed before it, and
        // the scan after it never runs: the only scan is the one the window opened on.
        Assert.True(batchStopped);
        Assert.True(vm.Cleanup.CloseRequested);
        await _scanService.Received(1).ScanAsync(
            Arg.Any<IProgress<ScanProgressUpdate>?>(), Arg.Any<CancellationToken>());
        // The list from before the batch goes all the same, so nothing on the window
        // offers files the batch has already acted on.
        Assert.False(vm.Scan.HasScanned);
        Assert.Null(vm.Scan.LastScanResult);
        AssertTheWindowSaysTheScanAfterTheBatchDidNotFinish(vm);
        Assert.False(vm.Cleanup.IsOperating);
        // A file went, so this was the PC's first run.
        _firstRunMark.Received(1).Set();
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task A_close_during_a_Move_or_Delete_after_a_cancelled_rescan_reports_no_scan_cancelled(bool deleting)
    {
        var vm = CreateViewModel();
        TwoFileBatch(_ => vm.Cleanup.RequestClose(), done: 1, errored: 0, cancelled: true);
        await vm.Scan.ScanWithProgressAsync(null);
        vm.Cleanup.MoveDestination = Path.Combine(Path.GetTempPath(), "ic-test-close-after-a-cancelled-rescan");
        // A Re-scan the user cancels keeps the list, with the cancel still recorded.
        _scanService.ScanAsync(Arg.Any<IProgress<ScanProgressUpdate>?>(), Arg.Any<CancellationToken>())
            .Returns(ci =>
            {
                vm.Scan.CancelScanCommand.Execute(null);
                ci.Arg<CancellationToken>().ThrowIfCancellationRequested();
                return ScanResultWithOrphans(2);
            });
        await vm.Scan.ScanCommand.ExecuteAsync(null);
        Assert.True(vm.Scan.HasScanned);
        Assert.True(vm.Scan.LastScanWasCancelled);

        await RunBatch(vm, deleting);

        Assert.False(vm.Scan.HasScanned);
        AssertTheWindowSaysTheScanAfterTheBatchDidNotFinish(vm);
    }

    [Theory]
    [InlineData(false, false)]
    [InlineData(true, false)]
    [InlineData(false, true)]
    [InlineData(true, true)]
    public async Task A_close_during_the_rescan_after_a_Move_or_Delete_reports_no_scan_cancelled(
        bool deleting, bool cancelPressedFirst)
    {
        var vm = CreateViewModel();
        TwoFileBatch(_ => { }, done: 2, errored: 0, cancelled: false);
        // The close lands while the scan after the batch runs, the second scan, alone
        // or after the user's own Cancel on that scan.
        var scans = 0;
        bool? rescanStopped = null;
        _scanService.ScanAsync(Arg.Any<IProgress<ScanProgressUpdate>?>(), Arg.Any<CancellationToken>())
            .Returns(ci =>
            {
                if (++scans == 2)
                {
                    if (cancelPressedFirst) vm.Cleanup.CancelOperationCommand.Execute(null);
                    vm.Cleanup.RequestClose();
                    var token = ci.Arg<CancellationToken>();
                    rescanStopped = token.IsCancellationRequested;
                    token.ThrowIfCancellationRequested();
                }
                return ScanResultWithOrphans(2);
            });
        await vm.Scan.ScanWithProgressAsync(null);
        vm.Cleanup.MoveDestination = Path.Combine(Path.GetTempPath(), "ic-test-close-during-the-rescan");

        await RunBatch(vm, deleting);

        Assert.True(rescanStopped);
        Assert.True(vm.Cleanup.CloseRequested);
        Assert.Equal(2, scans);
        Assert.False(vm.Scan.HasScanned);
        Assert.Null(vm.Scan.LastScanResult);
        AssertTheWindowSaysTheScanAfterTheBatchDidNotFinish(vm);
        Assert.False(vm.Cleanup.IsOperating);
    }

    /// <summary>
    /// What the window says where the scan after a Move or Delete did not run, or was
    /// stopped, because the window is closing: that scan did not finish, and no scan
    /// was cancelled.
    /// </summary>
    private static void AssertTheWindowSaysTheScanAfterTheBatchDidNotFinish(MainViewModel vm)
    {
        Assert.False(vm.Scan.LastScanWasCancelled);
        Assert.Equal(Strings.Body_RescanNotFinished_Lead, vm.IntroLead);
        Assert.Equal(Strings.Body_RescanNotFinished_Why, vm.IntroDetail);
        Assert.Equal(string.Empty, vm.IntroNotice);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task A_Move_or_Delete_that_finishes_after_a_close_carries_no_box_and_writes_no_report(bool deleting)
    {
        // The PC's first run. The close lands as the last file finishes, so the batch
        // reports itself complete.
        var vm = FirstRunAllClear();
        TwoFileBatch(_ => vm.Cleanup.RequestClose(), done: 2, errored: 0, cancelled: false);
        _resultLogService.SendAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(ResultLogSendOutcome.Sent);
        await vm.Scan.ScanWithProgressAsync(null);
        vm.Cleanup.MoveDestination = Path.Combine(Path.GetTempPath(), "ic-test-close-before-the-card");

        await RunBatch(vm, deleting);
        vm.Dispose();

        // The card goes up and the window takes it away unseen, so it carries no box,
        // and nothing is written, sent or left waiting to go.
        Assert.True(vm.Completion.IsComplete);
        Assert.False(vm.Completion.OffersReport);
        AssertTheWindowSaysTheScanAfterTheBatchDidNotFinish(vm);
        await _earlierRunCheck.DidNotReceive().ShowsAnEarlierRunWithinAsync(Arg.Any<TimeSpan>());
        await _resultLogService.DidNotReceive().WriteAsync(Arg.Any<ResultLogEntry>(), Arg.Any<CancellationToken>());
        await _resultLogService.DidNotReceive().SendAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
        await _scanService.Received(1).ScanAsync(
            Arg.Any<IProgress<ScanProgressUpdate>?>(), Arg.Any<CancellationToken>());
        // Files went, so this was the PC's first run.
        _firstRunMark.Received(1).Set();
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task A_Move_or_Delete_that_finishes_after_a_close_with_every_file_failed_leaves_the_first_run_to_come(
        bool deleting)
    {
        var vm = FirstRunAllClear();
        TwoFileBatch(_ => vm.Cleanup.RequestClose(), done: 0, errored: 2, cancelled: false);
        await vm.Scan.ScanWithProgressAsync(null);
        vm.Cleanup.MoveDestination = Path.Combine(Path.GetTempPath(), "ic-test-close-every-file-failed");

        await RunBatch(vm, deleting);

        // Every file is where it was, so the PC's first report is still to come.
        Assert.True(vm.Completion.IsComplete);
        Assert.False(vm.Completion.OffersReport);
        AssertTheWindowSaysTheScanAfterTheBatchDidNotFinish(vm);
        await _resultLogService.DidNotReceive().WriteAsync(Arg.Any<ResultLogEntry>(), Arg.Any<CancellationToken>());
        _firstRunMark.DidNotReceive().Set();
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task A_close_during_the_wait_for_the_start_check_ends_the_wait(bool deleting)
    {
        // The PC's first run, with the start check still reading as the card is about
        // to go up, and never answering. The close lands during that wait, where Cancel
        // is out of use.
        var vm = FirstRunAllClear();
        TwoFileBatch(_ => { }, done: 2, errored: 0, cancelled: false);
        await vm.Scan.ScanWithProgressAsync(null);
        vm.Cleanup.MoveDestination = Path.Combine(Path.GetTempPath(), "ic-test-close-during-the-report-wait");
        var neverAnswers = new TaskCompletionSource<bool>();
        bool? canCancelInTheWait = null;
        _earlierRunCheck.ShowsAnEarlierRunWithinAsync(Arg.Any<TimeSpan>()).Returns(_ =>
        {
            canCancelInTheWait = vm.Cleanup.CancelOperationCommand.CanExecute(null);
            vm.Cleanup.RequestClose();
            return neverAnswers.Task;
        });

        await RunBatch(vm, deleting);

        Assert.False(canCancelInTheWait);
        Assert.False(vm.Cleanup.IsOperating);
        Assert.True(vm.Completion.IsComplete);
        Assert.False(vm.Completion.OffersReport);
        await _resultLogService.DidNotReceive().WriteAsync(Arg.Any<ResultLogEntry>(), Arg.Any<CancellationToken>());
        _firstRunMark.Received(1).Set();
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task A_start_check_that_fails_after_a_close_goes_to_the_crash_log(bool deleting)
    {
        // The PC's first run. The close lands as the start check is asked, and the check
        // fails, which its contract says it never does.
        var vm = FirstRunAllClear();
        TwoFileBatch(_ => { }, done: 2, errored: 0, cancelled: false);
        await vm.Scan.ScanWithProgressAsync(null);
        vm.Cleanup.MoveDestination = Path.Combine(Path.GetTempPath(), "ic-test-start-check-fails-after-a-close");
        // The crash log is the user's own, shared with every run, so the marker is new to
        // this run and is looked for only in what the run appends to the log.
        var marker = $"start check failed {Guid.NewGuid()}";
        var logBefore = CrashLogText();
        _earlierRunCheck.ShowsAnEarlierRunWithinAsync(Arg.Any<TimeSpan>()).Returns(_ =>
        {
            vm.Cleanup.RequestClose();
            return Task.FromException<bool>(new InvalidOperationException(marker));
        });

        await RunBatch(vm, deleting);

        var logAfter = CrashLogText();
        // A log that rotated during the run starts again, and all of it is this run's.
        var appended = logAfter.StartsWith(logBefore, StringComparison.Ordinal)
            ? logAfter[logBefore.Length..]
            : logAfter;
        Assert.Contains(marker, appended);
        Assert.False(vm.Completion.OffersReport);
        await _resultLogService.DidNotReceive().WriteAsync(Arg.Any<ResultLogEntry>(), Arg.Any<CancellationToken>());
        _firstRunMark.Received(1).Set();
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task The_overlay_names_nothing_while_a_finished_Move_or_Delete_waits_to_put_up_its_card(bool deleting)
    {
        // The PC's first run, with the start check still reading once the rescan is over.
        var vm = FirstRunAllClear();
        TwoFileBatch(_ => { }, done: 2, errored: 0, cancelled: false);
        await vm.Scan.ScanWithProgressAsync(null);
        vm.Cleanup.MoveDestination = Path.Combine(Path.GetTempPath(), "ic-test-heading-in-the-report-wait");
        string? heading = null;
        _earlierRunCheck.ShowsAnEarlierRunWithinAsync(Arg.Any<TimeSpan>()).Returns(_ =>
        {
            heading = vm.Cleanup.OperationProgress;
            return Task.FromResult(false);
        });

        await RunBatch(vm, deleting);

        Assert.Equal(string.Empty, heading);
        Assert.True(vm.Completion.OffersReport);
    }

    public static TheoryData<string> ArmsEndingInADialogOrTheGateReCheck => new()
    {
        "Move busy", "Move unavailable", "Move access refused", "Move stopped",
        "Move destination refused", "Move destination unwritable",
        "Delete busy", "Delete unavailable", "Delete access refused",
    };

    [Theory]
    [MemberData(nameof(ArmsEndingInADialogOrTheGateReCheck))]
    public async Task The_overlay_names_nothing_behind_a_dialog_or_the_gate_re_check_a_Move_or_Delete_ends_in(
        string arm)
    {
        var vm = CreateViewModel();
        TwoFileBatch(_ => { }, done: 0, errored: 0, cancelled: false);
        var deleting = arm.StartsWith("Delete");
        var result = arm[(arm.IndexOf(' ') + 1)..];
        var expected = result == "access refused" ? 2 : 1;
        Exception? thrown = result switch
        {
            "stopped" => new MoveAbortedException("stopped", new MoveResult(1, Array.Empty<FileOperationError>()),
                @"D:\Kept", MoveAbortReason.ResolvesElsewhere),
            "destination refused" => new LocalisedInvalidOperationException("refused"),
            "destination unwritable" => new LocalisedAccessException("unwritable"),
            _ => null,
        };
        var refusal = new MoveResult(0, Array.Empty<FileOperationError>(),
            InstallerBusy: result == "busy",
            InstallerLockUnavailable: result == "unavailable",
            InstallerLockAccessRefused: result == "access refused");
        _moveService.MoveFilesAsync(
                Arg.Any<IEnumerable<string>>(), Arg.Any<string>(), Arg.Any<UnderLeaseClaims>(),
                Arg.Any<IProgress<OperationProgress>?>(), Arg.Any<CancellationToken>())
            .Returns(_ => thrown is null ? Task.FromResult(refusal) : Task.FromException<MoveResult>(thrown));
        _deleteService.DeleteFilesAsync(
                Arg.Any<IEnumerable<string>>(), Arg.Any<UnderLeaseClaims>(),
                Arg.Any<IProgress<OperationProgress>?>(), Arg.Any<CancellationToken>())
            .Returns(new DeleteResult(0, Array.Empty<FileOperationError>(),
                InstallerBusy: result == "busy",
                InstallerLockUnavailable: result == "unavailable",
                InstallerLockAccessRefused: result == "access refused"));
        await vm.Scan.ScanWithProgressAsync(null);
        vm.Cleanup.MoveDestination = Path.Combine(Path.GetTempPath(), "ic-test-heading-behind-a-dialog");

        // The heading as each dialog shows and as each re-check of the gate runs with
        // the overlay up. The re-check made at the click runs before the overlay does,
        // and a scan's own read of the gate is not a re-check.
        var headings = new List<string>();
        _dialogService.When(d => d.ShowWarning(Arg.Any<string>(), Arg.Any<string>()))
            .Do(_ => headings.Add(vm.Cleanup.OperationProgress));
        _rebootService.Check().Returns(_ =>
        {
            if (vm.Cleanup.IsOperating && !vm.Scan.IsScanInFlight)
                headings.Add(vm.Cleanup.OperationProgress);
            return PendingRebootResult.Clean;
        });

        await RunBatch(vm, deleting);

        Assert.Equal(expected, headings.Count);
        Assert.All(headings, heading => Assert.Equal(string.Empty, heading));
    }

    [Fact]
    public async Task Cancel_and_Esc_are_out_of_use_while_a_Delete_refused_at_the_installer_lock_re_checks_the_gate()
    {
        var vm = CreateViewModel();
        _scanService.ScanAsync(Arg.Any<IProgress<ScanProgressUpdate>?>(), Arg.Any<CancellationToken>())
            .Returns(ScanResultWithOrphans(2));
        _deleteService.DeleteFilesAsync(
                Arg.Any<IEnumerable<string>>(), Arg.Any<UnderLeaseClaims>(),
                Arg.Any<IProgress<OperationProgress>?>(), Arg.Any<CancellationToken>())
            .Returns(new DeleteResult(0, Array.Empty<FileOperationError>(), InstallerBusy: true));
        _confirmationService.ConfirmDelete(Arg.Any<int>(), Arg.Any<string>()).Returns(true);
        await vm.Scan.ScanWithProgressAsync(null);

        var button = new CancelButtonState(vm.Cleanup);
        bool? operating = null, canCancel = null, live = null, escTaken = null;
        var checks = 0;
        // The gate at the moment of action, then the re-check after the refusal.
        _rebootService.Check().Returns(_ =>
        {
            if (++checks == 1) return PendingRebootResult.Clean;
            operating = vm.Cleanup.IsOperating;
            canCancel = vm.Cleanup.CancelOperationCommand.CanExecute(null);
            live = button.Live;
            escTaken = vm.HandleEscape();
            return PendingRebootResult.Block(PendingRebootReason.MsiExecuteMutexHeld);
        });

        await vm.Cleanup.DeleteAllCommand.ExecuteAsync(null);

        Assert.Equal(2, checks);
        Assert.True(operating);
        Assert.False(canCancel);
        Assert.False(live);
        Assert.False(escTaken);
    }

    [Fact]
    public async Task Cancel_is_out_of_use_while_a_Move_refused_at_the_installer_lock_shows_its_dialog()
    {
        var vm = CreateViewModel();
        _scanService.ScanAsync(Arg.Any<IProgress<ScanProgressUpdate>?>(), Arg.Any<CancellationToken>())
            .Returns(ScanResultWithOrphans(2));
        _moveService.MoveFilesAsync(
                Arg.Any<IEnumerable<string>>(), Arg.Any<string>(), Arg.Any<UnderLeaseClaims>(),
                Arg.Any<IProgress<OperationProgress>?>(), Arg.Any<CancellationToken>())
            .Returns(new MoveResult(0, Array.Empty<FileOperationError>(), InstallerLockAccessRefused: true));
        _confirmationService.ConfirmMove(
            Arg.Any<int>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<bool>()).Returns(true);
        await vm.Scan.ScanWithProgressAsync(null);
        vm.Cleanup.MoveDestination = Path.Combine(Path.GetTempPath(), "ic-test-lock-refused-cancel");

        var button = new CancelButtonState(vm.Cleanup);
        bool? operating = null, canCancel = null, live = null;
        _dialogService
            .When(d => d.ShowWarning(Strings.Error_MoveInstallerLockAccessRefused, Arg.Any<string>()))
            .Do(_ =>
            {
                operating = vm.Cleanup.IsOperating;
                canCancel = vm.Cleanup.CancelOperationCommand.CanExecute(null);
                live = button.Live;
            });

        await vm.Cleanup.MoveAllCommand.ExecuteAsync(null);

        Assert.True(operating);
        Assert.False(canCancel);
        Assert.False(live);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Cancel_is_out_of_use_while_a_re_verify_that_failed_shows_its_dialog(bool deleting)
    {
        var vm = CreateViewModel();
        _scanService.ScanAsync(Arg.Any<IProgress<ScanProgressUpdate>?>(), Arg.Any<CancellationToken>())
            .Returns(ScanResultWithOrphans(2));
        _reverifier.ReverifyAsync(Arg.Any<IReadOnlyList<string>>(), Arg.Any<CancellationToken>(), Arg.Any<IProgress<ScanProgressUpdate>?>())
            .ThrowsAsync(new LocalisedInvalidOperationException(Strings.Error_InstallerDbEmpty));
        _confirmationService.ConfirmMove(
            Arg.Any<int>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<bool>()).Returns(true);
        _confirmationService.ConfirmDelete(Arg.Any<int>(), Arg.Any<string>()).Returns(true);
        await vm.Scan.ScanWithProgressAsync(null);
        vm.Cleanup.MoveDestination = Path.Combine(Path.GetTempPath(), "ic-test-reverify-throw-cancel");

        var button = new CancelButtonState(vm.Cleanup);
        bool? operating = null, canCancel = null, live = null;
        _dialogService
            .When(d => d.ShowError(Arg.Any<string>(), Strings.Error_StoppedTitle))
            .Do(_ =>
            {
                operating = vm.Cleanup.IsOperating;
                canCancel = vm.Cleanup.CancelOperationCommand.CanExecute(null);
                live = button.Live;
            });

        if (deleting) await vm.Cleanup.DeleteAllCommand.ExecuteAsync(null);
        else await vm.Cleanup.MoveAllCommand.ExecuteAsync(null);

        Assert.True(operating);
        Assert.False(canCancel);
        Assert.False(live);
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
            Arg.Any<IEnumerable<string>>(), Arg.Any<string>(), Arg.Any<UnderLeaseClaims>(),
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
            Arg.Any<IEnumerable<string>>(), Arg.Any<UnderLeaseClaims>(),
            Arg.Any<IProgress<OperationProgress>?>(), Arg.Any<CancellationToken>());
        Assert.False(vm.Completion.IsComplete);
    }

    // The gate is clean at the click and a transaction is under way by the time the
    // re-verify reads the in-progress signals. The whole batch stops as it does at the
    // gate: the banner paints with the re-verify's own read, neither service is called,
    // nothing is rescanned and no card is shown. With every file confirmed, the batch
    // stands for one holding a superseded patch, which the re-verify confirms whatever
    // the signals say; with none confirmed, for one the re-verify emptied, which still
    // ends under the banner rather than the held-back card.

    [Theory]
    [InlineData(false, false)]
    [InlineData(false, true)]
    [InlineData(true, false)]
    [InlineData(true, true)]
    public async Task A_blocked_signal_read_at_the_re_verify_stops_the_whole_batch_and_paints_the_banner(
        bool deleting, bool reverifyConfirmsNone)
    {
        var vm = CreateViewModel();
        _scanService.ScanAsync(Arg.Any<IProgress<ScanProgressUpdate>?>(), Arg.Any<CancellationToken>())
            .Returns(ScanResultWithOrphans(2));
        _confirmationService.ConfirmMove(
            Arg.Any<int>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<bool>()).Returns(true);
        _confirmationService.ConfirmDelete(Arg.Any<int>(), Arg.Any<string>()).Returns(true);
        await vm.Scan.ScanWithProgressAsync(null);
        var dest = Path.Combine(Path.GetTempPath(), "ic-test-reverify-signal-blocked");
        vm.Cleanup.MoveDestination = dest;

        var blocked = PendingRebootResult.Block(PendingRebootReason.InstallerInProgressMarker);
        bool? destinationThere = null;
        _reverifier.ReverifyAsync(Arg.Any<IReadOnlyList<string>>(), Arg.Any<CancellationToken>(), Arg.Any<IProgress<ScanProgressUpdate>?>())
            .Returns(ci =>
            {
                destinationThere = _fileSystem.Directory.Exists(dest);
                var batch = (IReadOnlyList<string>)ci[0]!;
                return reverifyConfirmsNone
                    ? new ReverifyResult(Array.Empty<string>(), batch,
                        new HeldBackReasons(FileNotConfirmed: batch.Count), InstallerActivity: blocked)
                    : new ReverifyResult(batch, Array.Empty<string>(), InstallerActivity: blocked);
            });

        if (deleting) await vm.Cleanup.DeleteAllCommand.ExecuteAsync(null);
        else await vm.Cleanup.MoveAllCommand.ExecuteAsync(null);

        Assert.Same(blocked, vm.Scan.PendingRebootResult);
        Assert.True(vm.Scan.HasPendingReboot);
        Assert.False(vm.Cleanup.MoveAllCommand.CanExecute(null));
        Assert.False(vm.Cleanup.DeleteAllCommand.CanExecute(null));
        await _moveService.DidNotReceive().MoveFilesAsync(
            Arg.Any<IEnumerable<string>>(), Arg.Any<string>(), Arg.Any<UnderLeaseClaims>(),
            Arg.Any<IProgress<OperationProgress>?>(), Arg.Any<CancellationToken>());
        await _deleteService.DidNotReceive().DeleteFilesAsync(
            Arg.Any<IEnumerable<string>>(), Arg.Any<UnderLeaseClaims>(),
            Arg.Any<IProgress<OperationProgress>?>(), Arg.Any<CancellationToken>());
        await _scanService.Received(1).ScanAsync(
            Arg.Any<IProgress<ScanProgressUpdate>?>(), Arg.Any<CancellationToken>());
        await _resultLogService.DidNotReceive().WriteAsync(
            Arg.Any<ResultLogEntry>(), Arg.Any<CancellationToken>());
        Assert.False(vm.Completion.IsComplete);
        Assert.False(vm.Cleanup.IsOperating);
        Assert.Equal(string.Empty, vm.Cleanup.OperationProgress);
        if (!deleting)
        {
            // The pre-flight made the folder before the re-verify ran, and it is gone.
            Assert.True(destinationThere);
            Assert.False(_fileSystem.Directory.Exists(dest));
        }
    }

    // Nothing runs on the operation's source once the re-verify has returned a blocked
    // read, so Cancel is out of use by the time the banner's result is set, which on a
    // Move is before the folder the pre-flight made is removed.

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Cancel_is_out_of_use_once_a_blocked_signal_read_at_the_re_verify_stops_the_batch(bool deleting)
    {
        var vm = CreateViewModel();
        _scanService.ScanAsync(Arg.Any<IProgress<ScanProgressUpdate>?>(), Arg.Any<CancellationToken>())
            .Returns(ScanResultWithOrphans(2));
        _confirmationService.ConfirmMove(
            Arg.Any<int>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<bool>()).Returns(true);
        _confirmationService.ConfirmDelete(Arg.Any<int>(), Arg.Any<string>()).Returns(true);
        await vm.Scan.ScanWithProgressAsync(null);
        vm.Cleanup.MoveDestination = Path.Combine(Path.GetTempPath(), "ic-test-reverify-signal-blocked-cancel");

        var blocked = PendingRebootResult.Block(PendingRebootReason.InstallerInProgressMarker);
        _reverifier.ReverifyAsync(Arg.Any<IReadOnlyList<string>>(), Arg.Any<CancellationToken>(), Arg.Any<IProgress<ScanProgressUpdate>?>())
            .Returns(ci => new ReverifyResult(
                (IReadOnlyList<string>)ci[0]!, Array.Empty<string>(), InstallerActivity: blocked));

        var button = new CancelButtonState(vm.Cleanup);
        bool? operating = null, canCancel = null, live = null;
        vm.Scan.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName != nameof(ScanViewModel.PendingRebootResult)
                || !ReferenceEquals(vm.Scan.PendingRebootResult, blocked)) return;
            operating = vm.Cleanup.IsOperating;
            canCancel = vm.Cleanup.CancelOperationCommand.CanExecute(null);
            live = button.Live;
        };

        if (deleting) await vm.Cleanup.DeleteAllCommand.ExecuteAsync(null);
        else await vm.Cleanup.MoveAllCommand.ExecuteAsync(null);

        Assert.True(operating);
        Assert.False(canCancel);
        Assert.False(live);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task A_clean_signal_read_at_the_re_verify_goes_on_to_the_batch(bool deleting)
    {
        var vm = CreateViewModel();
        _scanService.ScanAsync(Arg.Any<IProgress<ScanProgressUpdate>?>(), Arg.Any<CancellationToken>())
            .Returns(ScanResultWithOrphans(2));
        _confirmationService.ConfirmMove(
            Arg.Any<int>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<bool>()).Returns(true);
        _confirmationService.ConfirmDelete(Arg.Any<int>(), Arg.Any<string>()).Returns(true);
        _moveService.MoveFilesAsync(
                Arg.Any<IEnumerable<string>>(), Arg.Any<string>(), Arg.Any<UnderLeaseClaims>(),
                Arg.Any<IProgress<OperationProgress>?>(), Arg.Any<CancellationToken>())
            .Returns(new MoveResult(2, Array.Empty<FileOperationError>()));
        _deleteService.DeleteFilesAsync(
                Arg.Any<IEnumerable<string>>(), Arg.Any<UnderLeaseClaims>(),
                Arg.Any<IProgress<OperationProgress>?>(), Arg.Any<CancellationToken>())
            .Returns(new DeleteResult(2, Array.Empty<FileOperationError>()));
        await vm.Scan.ScanWithProgressAsync(null);
        vm.Cleanup.MoveDestination = Path.Combine(Path.GetTempPath(), "ic-test-reverify-signal-clean");
        _reverifier.ReverifyAsync(Arg.Any<IReadOnlyList<string>>(), Arg.Any<CancellationToken>(), Arg.Any<IProgress<ScanProgressUpdate>?>())
            .Returns(ci => new ReverifyResult((IReadOnlyList<string>)ci[0]!, Array.Empty<string>(),
                InstallerActivity: PendingRebootResult.Clean));

        if (deleting) await vm.Cleanup.DeleteAllCommand.ExecuteAsync(null);
        else await vm.Cleanup.MoveAllCommand.ExecuteAsync(null);

        Assert.False(vm.Scan.HasPendingReboot);
        if (deleting)
            await _deleteService.Received(1).DeleteFilesAsync(
                Arg.Any<IEnumerable<string>>(), Arg.Any<UnderLeaseClaims>(),
                Arg.Any<IProgress<OperationProgress>?>(), Arg.Any<CancellationToken>());
        else
            await _moveService.Received(1).MoveFilesAsync(
                Arg.Any<IEnumerable<string>>(), Arg.Any<string>(), Arg.Any<UnderLeaseClaims>(),
                Arg.Any<IProgress<OperationProgress>?>(), Arg.Any<CancellationToken>());
        Assert.True(vm.Completion.IsComplete);
    }

    // The pair below covers the one case in which Check() throws rather than
    // answering: a read of Windows Installer's in-progress file that failed in a
    // way it has no reading for. At the click that throw arrives before anything
    // has been touched, so each command ends in its own failure dialog, acts on
    // nothing, and is available again afterwards.

    [Fact]
    public async Task MoveAllAsync_reboot_gate_throwing_at_action_time_shows_the_move_failed_dialog_and_moves_nothing()
    {
        var vm = CreateViewModel();
        _scanService.ScanAsync(Arg.Any<IProgress<ScanProgressUpdate>?>(), Arg.Any<CancellationToken>())
            .Returns(ScanResultWithOrphans(2));
        _rebootService.Check().Returns(
            _ => PendingRebootResult.Clean,
            _ => throw new IOException("boom"));
        _confirmationService.ConfirmMove(
            Arg.Any<int>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<bool>()).Returns(true);

        await vm.Scan.ScanWithProgressAsync(null);
        var dest = Path.Combine(Path.GetTempPath(), "ic-test-reboot-gate-throws");
        vm.Cleanup.MoveDestination = dest;

        await vm.Cleanup.MoveAllCommand.ExecuteAsync(null);

        _dialogService.Received(1).ShowWarning(
            Arg.Is<string>(s => s.Contains(nameof(IOException))), Strings.Error_MoveFailedTitle);
        await _reverifier.DidNotReceive().ReverifyAsync(
            Arg.Any<IReadOnlyList<string>>(), Arg.Any<CancellationToken>(), Arg.Any<IProgress<ScanProgressUpdate>?>());
        await _moveService.DidNotReceive().MoveFilesAsync(
            Arg.Any<IEnumerable<string>>(), Arg.Any<string>(), Arg.Any<UnderLeaseClaims>(),
            Arg.Any<IProgress<OperationProgress>?>(), Arg.Any<CancellationToken>());
        await _resultLogService.DidNotReceive().WriteAsync(
            Arg.Any<ResultLogEntry>(), Arg.Any<CancellationToken>());
        Assert.False(vm.Completion.IsComplete);
        Assert.Equal(string.Empty, vm.Cleanup.OperationProgress);
        // The folder the pre-flight made is removed, as on every other arm that
        // stops before the batch, and both commands come back for the next click.
        Assert.False(_fileSystem.Directory.Exists(dest));
        Assert.True(vm.Cleanup.MoveAllCommand.CanExecute(null));
        Assert.True(vm.Cleanup.DeleteAllCommand.CanExecute(null));
    }

    [Fact]
    public async Task DeleteAllAsync_reboot_gate_throwing_at_action_time_shows_the_delete_failed_dialog_and_deletes_nothing()
    {
        var vm = CreateViewModel();
        _scanService.ScanAsync(Arg.Any<IProgress<ScanProgressUpdate>?>(), Arg.Any<CancellationToken>())
            .Returns(ScanResultWithOrphans(2));
        _rebootService.Check().Returns(
            _ => PendingRebootResult.Clean,
            _ => throw new IOException("boom"));
        _confirmationService.ConfirmDelete(Arg.Any<int>(), Arg.Any<string>()).Returns(true);

        await vm.Scan.ScanWithProgressAsync(null);

        await vm.Cleanup.DeleteAllCommand.ExecuteAsync(null);

        _dialogService.Received(1).ShowWarning(
            Arg.Is<string>(s => s.Contains(nameof(IOException))), Strings.Error_DeleteFailedTitle);
        await _reverifier.DidNotReceive().ReverifyAsync(
            Arg.Any<IReadOnlyList<string>>(), Arg.Any<CancellationToken>(), Arg.Any<IProgress<ScanProgressUpdate>?>());
        await _deleteService.DidNotReceive().DeleteFilesAsync(
            Arg.Any<IEnumerable<string>>(), Arg.Any<UnderLeaseClaims>(),
            Arg.Any<IProgress<OperationProgress>?>(), Arg.Any<CancellationToken>());
        await _resultLogService.DidNotReceive().WriteAsync(
            Arg.Any<ResultLogEntry>(), Arg.Any<CancellationToken>());
        Assert.False(vm.Completion.IsComplete);
        Assert.Equal(string.Empty, vm.Cleanup.OperationProgress);
        Assert.True(vm.Cleanup.MoveAllCommand.CanExecute(null));
        Assert.True(vm.Cleanup.DeleteAllCommand.CanExecute(null));
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
                Arg.Any<IEnumerable<string>>(), Arg.Any<string>(), Arg.Any<UnderLeaseClaims>(),
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
    public async Task MoveAllAsync_lock_refused_result_shows_the_dialog_and_paints_the_banner()
    {
        // The third of the three lock answers, and it takes something from each of
        // the other two. A dialog, because the click is owed an answer naming what
        // did not happen to the files, and then the gate, because the gate has a
        // reason for a refused lock and a banner of its own for it. Clean at the
        // scan and at the act-time gate, so the refusal the service meets began
        // after that gate, which is the only way this arm is reached.
        var vm = CreateViewModel();
        _scanService.ScanAsync(Arg.Any<IProgress<ScanProgressUpdate>?>(), Arg.Any<CancellationToken>())
            .Returns(ScanResultWithOrphans(2));
        _rebootService.Check().Returns(
            PendingRebootResult.Clean,
            PendingRebootResult.Clean,
            PendingRebootResult.Block(PendingRebootReason.MsiExecuteMutexAccessRefused));
        _moveService.MoveFilesAsync(
                Arg.Any<IEnumerable<string>>(), Arg.Any<string>(), Arg.Any<UnderLeaseClaims>(),
                Arg.Any<IProgress<OperationProgress>?>(), Arg.Any<CancellationToken>())
            .Returns(new MoveResult(0, Array.Empty<FileOperationError>(),
                InstallerLockAccessRefused: true));
        _confirmationService.ConfirmMove(
            Arg.Any<int>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<bool>()).Returns(true);

        await vm.Scan.ScanWithProgressAsync(null);
        vm.Cleanup.MoveDestination = Path.Combine(Path.GetTempPath(), "ic-test-lock-refused");

        await vm.Cleanup.MoveAllCommand.ExecuteAsync(null);

        _dialogService.Received(1).ShowWarning(
            Strings.Error_MoveInstallerLockAccessRefused,
            Strings.Error_MoveInstallerLockUnavailableTitle);
        // Three: the scan, the act-time gate, and the re-check this arm runs.
        _rebootService.Received(3).Check();
        Assert.True(vm.Scan.HasPendingReboot);
        Assert.Equal(Strings.Body_PendingReboot_MsiExecuteMutexAccessRefused, vm.Scan.PendingRebootBannerText);
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
        // has no account of the condition: its probe reads the same failure as not
        // held, so a re-check comes back clean and leaves the user refused with no
        // reason on screen. The dialog carries it instead, and the title is pinned
        // as well as the body because the Move copy is a separate pair from the
        // Delete copy and either could be wired to the other's.
        var vm = CreateViewModel();
        _scanService.ScanAsync(Arg.Any<IProgress<ScanProgressUpdate>?>(), Arg.Any<CancellationToken>())
            .Returns(ScanResultWithOrphans(2));
        _moveService.MoveFilesAsync(
                Arg.Any<IEnumerable<string>>(), Arg.Any<string>(), Arg.Any<UnderLeaseClaims>(),
                Arg.Any<IProgress<OperationProgress>?>(), Arg.Any<CancellationToken>())
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
        _reverifier.ReverifyAsync(Arg.Any<IReadOnlyList<string>>(), Arg.Any<CancellationToken>(), Arg.Any<IProgress<ScanProgressUpdate>?>())
            .Returns(new ReverifyResult(
                new[] { @"C:\Windows\Installer\a.msi", @"C:\Windows\Installer\c.msi" },
                new[] { @"C:\Windows\Installer\b.msi" },
                new HeldBackReasons(Reclaimed: 1)));
        _moveService.MoveFilesAsync(
                Arg.Any<IEnumerable<string>>(), Arg.Any<string>(), Arg.Any<UnderLeaseClaims>(),
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
            Arg.Any<string>(), Arg.Any<UnderLeaseClaims>(), Arg.Any<IProgress<OperationProgress>?>(), Arg.Any<CancellationToken>());
        Assert.True(vm.Completion.IsComplete);
        Assert.NotEqual(string.Empty, vm.Completion.Skipped);
        Assert.Contains("1", vm.Completion.Skipped);
    }

    /// <summary>
    /// Where the card's bar stands in the three stretches of one batch: inside the
    /// re-verify, when the service is handed the batch, and once the batch has reported
    /// its first file. The first count comes through the progress reporter, which posts
    /// its callback, so the last sample waits for it to land.
    /// </summary>
    private sealed class BarSamples
    {
        public bool? DuringReverify, AtHandOver, ClearedByFirstCount;

        public void Batch(MainViewModel vm, IProgress<OperationProgress>? progress)
        {
            AtHandOver = vm.Cleanup.IsOperationProgressIndeterminate;
            progress?.Report(new OperationProgress(1, 1, "a.msi"));
            ClearedByFirstCount = SpinWait.SpinUntil(
                () => !vm.Cleanup.IsOperationProgressIndeterminate, TimeSpan.FromSeconds(5));
        }
    }

    private BarSamples SampleTheBarThroughTheReverify(MainViewModel vm)
    {
        var samples = new BarSamples();
        var orphans = new List<OrphanedFile>
        {
            new(@"C:\Windows\Installer\a.msi", 1_048_576, false, false, false, Orphaned),
        };
        _scanService.ScanAsync(Arg.Any<IProgress<ScanProgressUpdate>?>(), Arg.Any<CancellationToken>())
            .Returns(new ScanResult(orphans, Array.Empty<RegisteredPackage>(), 0));
        _reverifier.ReverifyAsync(Arg.Any<IReadOnlyList<string>>(), Arg.Any<CancellationToken>(), Arg.Any<IProgress<ScanProgressUpdate>?>())
            .Returns(ci =>
            {
                samples.DuringReverify = vm.Cleanup.IsOperationProgressIndeterminate;
                return new ReverifyResult((IReadOnlyList<string>)ci[0]!, Array.Empty<string>());
            });
        return samples;
    }

    [Fact]
    public async Task MoveAllAsync_bar_moves_over_no_count_until_the_batch_reports_its_first_file()
    {
        var vm = CreateViewModel();
        var samples = SampleTheBarThroughTheReverify(vm);
        _moveService.MoveFilesAsync(
                Arg.Any<IEnumerable<string>>(), Arg.Any<string>(), Arg.Any<UnderLeaseClaims>(),
                Arg.Any<IProgress<OperationProgress>?>(), Arg.Any<CancellationToken>())
            .Returns(ci =>
            {
                samples.Batch(vm, (IProgress<OperationProgress>?)ci[3]);
                return new MoveResult(1, Array.Empty<FileOperationError>());
            });
        _confirmationService.ConfirmMove(
            Arg.Any<int>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<bool>()).Returns(true);

        await vm.Scan.ScanWithProgressAsync(null);
        vm.Cleanup.MoveDestination = Path.Combine(Path.GetTempPath(), "ic-test-reverify-busy-bar");

        await vm.Cleanup.MoveAllCommand.ExecuteAsync(null);

        Assert.True(samples.DuringReverify);
        Assert.True(samples.AtHandOver);
        Assert.True(samples.ClearedByFirstCount);
    }

    [Fact]
    public async Task DeleteAllAsync_bar_moves_over_no_count_until_the_batch_reports_its_first_file()
    {
        var vm = CreateViewModel();
        var samples = SampleTheBarThroughTheReverify(vm);
        _deleteService.DeleteFilesAsync(
                Arg.Any<IEnumerable<string>>(), Arg.Any<UnderLeaseClaims>(),
                Arg.Any<IProgress<OperationProgress>?>(), Arg.Any<CancellationToken>())
            .Returns(ci =>
            {
                samples.Batch(vm, (IProgress<OperationProgress>?)ci[2]);
                return new DeleteResult(1, Array.Empty<FileOperationError>());
            });
        _confirmationService.ConfirmDelete(Arg.Any<int>(), Arg.Any<string>()).Returns(true);

        await vm.Scan.ScanWithProgressAsync(null);

        await vm.Cleanup.DeleteAllCommand.ExecuteAsync(null);

        Assert.True(samples.DuringReverify);
        Assert.True(samples.AtHandOver);
        Assert.True(samples.ClearedByFirstCount);
    }

    [Fact]
    public async Task MoveAllAsync_reverify_throwing_stops_the_batch_and_surfaces_the_failure()
    {
        var vm = CreateViewModel();
        _scanService.ScanAsync(Arg.Any<IProgress<ScanProgressUpdate>?>(), Arg.Any<CancellationToken>())
            .Returns(ScanResultWithOrphans(2));
        _reverifier.ReverifyAsync(Arg.Any<IReadOnlyList<string>>(), Arg.Any<CancellationToken>(), Arg.Any<IProgress<ScanProgressUpdate>?>())
            .ThrowsAsync(new LocalisedInvalidOperationException(Strings.Error_InstallerDbEmpty));
        _confirmationService.ConfirmMove(
            Arg.Any<int>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<bool>()).Returns(true);

        await vm.Scan.ScanWithProgressAsync(null);
        vm.Cleanup.MoveDestination = Path.Combine(Path.GetTempPath(), "ic-test-reverify-throw");

        await vm.Cleanup.MoveAllCommand.ExecuteAsync(null);

        // Never act on an un-verified batch: the move service is not called.
        await _moveService.DidNotReceive().MoveFilesAsync(
            Arg.Any<IEnumerable<string>>(), Arg.Any<string>(), Arg.Any<UnderLeaseClaims>(),
            Arg.Any<IProgress<OperationProgress>?>(), Arg.Any<CancellationToken>());
        // The failure surfaces through the scan error ladder, not a completion.
        _dialogService.Received(1).ShowError(Arg.Any<string>(), Strings.Error_StoppedTitle);
        Assert.False(vm.Completion.IsComplete);
    }

    [Fact]
    public async Task DeleteAllAsync_reverify_throwing_stops_the_batch_and_surfaces_the_failure()
    {
        var vm = CreateViewModel();
        _scanService.ScanAsync(Arg.Any<IProgress<ScanProgressUpdate>?>(), Arg.Any<CancellationToken>())
            .Returns(ScanResultWithOrphans(2));
        _reverifier.ReverifyAsync(Arg.Any<IReadOnlyList<string>>(), Arg.Any<CancellationToken>(), Arg.Any<IProgress<ScanProgressUpdate>?>())
            .ThrowsAsync(new LocalisedInvalidOperationException(Strings.Error_InstallerDbEmpty));
        _confirmationService.ConfirmDelete(Arg.Any<int>(), Arg.Any<string>()).Returns(true);

        await vm.Scan.ScanWithProgressAsync(null);

        await vm.Cleanup.DeleteAllCommand.ExecuteAsync(null);

        // Never act on an un-verified batch: the delete service is not called.
        await _deleteService.DidNotReceive().DeleteFilesAsync(
            Arg.Any<IEnumerable<string>>(), Arg.Any<UnderLeaseClaims>(),
            Arg.Any<IProgress<OperationProgress>?>(), Arg.Any<CancellationToken>());
        // The same rung and the same heading as a Move that stops here.
        _dialogService.Received(1).ShowError(
            Arg.Is<string>(m => m != null
                && m.StartsWith(Strings.Error_InstallerDbEmpty, StringComparison.Ordinal)),
            Strings.Error_StoppedTitle);
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
        _reverifier.ReverifyAsync(Arg.Any<IReadOnlyList<string>>(), Arg.Any<CancellationToken>(), Arg.Any<IProgress<ScanProgressUpdate>?>())
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
            Arg.Any<IEnumerable<string>>(), Arg.Any<string>(), Arg.Any<UnderLeaseClaims>(),
            Arg.Any<IProgress<OperationProgress>?>(), Arg.Any<CancellationToken>());
        Assert.True(vm.Completion.IsComplete);
        Assert.Contains("2", vm.Completion.Summary);
    }

    [Fact]
    public async Task MoveAllAsync_a_degraded_reverify_reports_what_it_kept_back()
    {
        // A re-verify that could not read the records keeps files back without any
        // program having reclaimed them. The screen names no cause either way since
        // 3.0.0, so what is pinned here is that the file reaches the count at all:
        // a re-verify that kept a file back and reported nothing would leave the
        // batch's own numbers not adding up.
        var vm = CreateViewModel();
        var orphans = new List<OrphanedFile>
        {
            new(@"C:\Windows\Installer\a.msp", 1_048_576, true, false, false, Orphaned),
        };
        _scanService.ScanAsync(Arg.Any<IProgress<ScanProgressUpdate>?>(), Arg.Any<CancellationToken>())
            .Returns(new ScanResult(orphans, Array.Empty<RegisteredPackage>(), 0));
        _reverifier.ReverifyAsync(Arg.Any<IReadOnlyList<string>>(), Arg.Any<CancellationToken>(), Arg.Any<IProgress<ScanProgressUpdate>?>())
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
            Arg.Any<IEnumerable<string>>(), Arg.Any<string>(), Arg.Any<UnderLeaseClaims>(),
            Arg.Any<IProgress<OperationProgress>?>(), Arg.Any<CancellationToken>());
        Assert.Equal(
            string.Format(Strings.Completion_HeldBack_Singular, 1),
            vm.Completion.Summary);
    }

    [Fact]
    public async Task MoveAllAsync_a_healthy_reverify_still_reports_what_it_kept_back()
    {
        var vm = CreateViewModel();
        var orphans = new List<OrphanedFile>
        {
            new(@"C:\Windows\Installer\a.msp", 1_048_576, true, false, false, Orphaned),
            new(@"C:\Windows\Installer\b.msi", 1_048_576, false, false, false, Orphaned),
        };
        _scanService.ScanAsync(Arg.Any<IProgress<ScanProgressUpdate>?>(), Arg.Any<CancellationToken>())
            .Returns(new ScanResult(orphans, Array.Empty<RegisteredPackage>(), 0));
        _reverifier.ReverifyAsync(Arg.Any<IReadOnlyList<string>>(), Arg.Any<CancellationToken>(), Arg.Any<IProgress<ScanProgressUpdate>?>())
            .Returns(new ReverifyResult(
                new[] { @"C:\Windows\Installer\b.msi" },
                new[] { @"C:\Windows\Installer\a.msp" },
                new HeldBackReasons(Reclaimed: 1)));
        _moveService.MoveFilesAsync(
                Arg.Any<IEnumerable<string>>(), Arg.Any<string>(), Arg.Any<UnderLeaseClaims>(),
                Arg.Any<IProgress<OperationProgress>?>(), Arg.Any<CancellationToken>())
            .Returns(new MoveResult(1, Array.Empty<FileOperationError>()));
        _confirmationService.ConfirmMove(
            Arg.Any<int>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<bool>()).Returns(true);

        await vm.Scan.ScanWithProgressAsync(null);
        vm.Cleanup.MoveDestination = Path.Combine(Path.GetTempPath(), "ic-test-reverify-healthy");

        await vm.Cleanup.MoveAllCommand.ExecuteAsync(null);

        Assert.Equal(
            string.Format(Strings.Completion_HeldBack_Singular, 1),
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
        _reverifier.ReverifyAsync(Arg.Any<IReadOnlyList<string>>(), Arg.Any<CancellationToken>(), Arg.Any<IProgress<ScanProgressUpdate>?>())
            .Returns(new ReverifyResult(
                new[] { @"C:\Windows\Installer\x.msi" },
                new[] { @"C:\Windows\Installer\y.msi" },
                new HeldBackReasons(Reclaimed: 1)));
        _deleteService.DeleteFilesAsync(
                Arg.Any<IEnumerable<string>>(), Arg.Any<UnderLeaseClaims>(),
                Arg.Any<IProgress<OperationProgress>?>(), Arg.Any<CancellationToken>())
            .Returns(new DeleteResult(1, Array.Empty<FileOperationError>()));
        _confirmationService.ConfirmDelete(Arg.Any<int>(), Arg.Any<string>()).Returns(true);

        await vm.Scan.ScanWithProgressAsync(null);

        await vm.Cleanup.DeleteAllCommand.ExecuteAsync(null);

        await _deleteService.Received(1).DeleteFilesAsync(
            Arg.Is<IEnumerable<string>>(paths =>
                paths != null && paths.Count() == 1 && paths.Contains(@"C:\Windows\Installer\x.msi")), Arg.Any<UnderLeaseClaims>(),
            Arg.Any<IProgress<OperationProgress>?>(), Arg.Any<CancellationToken>());
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
            new(@"C:\Windows\Installer\a.msp", 1_048_576, true, false, false, Orphaned),
        };
        _scanService.ScanAsync(Arg.Any<IProgress<ScanProgressUpdate>?>(), Arg.Any<CancellationToken>())
            .Returns(new ScanResult(orphans, Array.Empty<RegisteredPackage>(), 0));
        var claims = new[] { Claim(@"C:\Windows\Installer\a.msp") };
        // The sibling half is the wire D7 added, and a wire that is not asserted is
        // a wire that can quietly come loose.
        var siblings = new[] { Claim(@"C:\Windows\Installer\a.msp"), Claim(@"C:\Windows\Installer\sibling.msp") };
        _reverifier.ReverifyAsync(Arg.Any<IReadOnlyList<string>>(), Arg.Any<CancellationToken>(), Arg.Any<IProgress<ScanProgressUpdate>?>())
            .Returns(new ReverifyResult(
                new[] { @"C:\Windows\Installer\a.msp" }, Array.Empty<string>(),
                SurvivingPatchClaims: claims,
                SiblingPatchClaims: siblings));
        _moveService.MoveFilesAsync(
                Arg.Any<IEnumerable<string>>(), Arg.Any<string>(), Arg.Any<UnderLeaseClaims>(),
                Arg.Any<IProgress<OperationProgress>?>(), Arg.Any<CancellationToken>())
            .Returns(new MoveResult(1, Array.Empty<FileOperationError>()));
        _confirmationService.ConfirmMove(
            Arg.Any<int>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<bool>()).Returns(true);

        await vm.Scan.ScanWithProgressAsync(null);
        vm.Cleanup.MoveDestination = Path.Combine(Path.GetTempPath(), "ic-test-claims-move");

        await vm.Cleanup.MoveAllCommand.ExecuteAsync(null);

        await _moveService.Received(1).MoveFilesAsync(
            Arg.Any<IEnumerable<string>>(), Arg.Any<string>(), Arg.Is<UnderLeaseClaims>(c =>
                c.Batch.SequenceEqual(claims)
                && c.Siblings.SequenceEqual(siblings)),
            Arg.Any<IProgress<OperationProgress>?>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task DeleteAllAsync_hands_the_re_verifys_claims_to_the_service()
    {
        var vm = CreateViewModel();
        var orphans = new List<OrphanedFile>
        {
            new(@"C:\Windows\Installer\x.msp", 524_288, true, false, false, Orphaned),
        };
        _scanService.ScanAsync(Arg.Any<IProgress<ScanProgressUpdate>?>(), Arg.Any<CancellationToken>())
            .Returns(new ScanResult(orphans, Array.Empty<RegisteredPackage>(), 0));
        var claims = new[] { Claim(@"C:\Windows\Installer\x.msp") };
        // The sibling half is the wire D7 added, and a wire that is not asserted is
        // a wire that can quietly come loose: the batch would then be re-checked
        // against the narrower question with nothing to say so.
        var siblings = new[] { Claim(@"C:\Windows\Installer\x.msp"), Claim(@"C:\Windows\Installer\sibling.msp") };
        _reverifier.ReverifyAsync(Arg.Any<IReadOnlyList<string>>(), Arg.Any<CancellationToken>(), Arg.Any<IProgress<ScanProgressUpdate>?>())
            .Returns(new ReverifyResult(
                new[] { @"C:\Windows\Installer\x.msp" }, Array.Empty<string>(),
                SurvivingPatchClaims: claims,
                SiblingPatchClaims: siblings));
        _deleteService.DeleteFilesAsync(
                Arg.Any<IEnumerable<string>>(), Arg.Any<UnderLeaseClaims>(),
                Arg.Any<IProgress<OperationProgress>?>(), Arg.Any<CancellationToken>())
            .Returns(new DeleteResult(1, Array.Empty<FileOperationError>()));
        _confirmationService.ConfirmDelete(Arg.Any<int>(), Arg.Any<string>()).Returns(true);

        await vm.Scan.ScanWithProgressAsync(null);

        await vm.Cleanup.DeleteAllCommand.ExecuteAsync(null);

        await _deleteService.Received(1).DeleteFilesAsync(
            Arg.Any<IEnumerable<string>>(), Arg.Is<UnderLeaseClaims>(c =>
                c.Batch.SequenceEqual(claims)
                && c.Siblings.SequenceEqual(siblings)),
            Arg.Any<IProgress<OperationProgress>?>(), Arg.Any<CancellationToken>());
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
            new(@"C:\Windows\Installer\c.msp", 4_194_304, true, false, false, Orphaned),
        };
        _scanService.ScanAsync(Arg.Any<IProgress<ScanProgressUpdate>?>(), Arg.Any<CancellationToken>())
            .Returns(new ScanResult(orphans, Array.Empty<RegisteredPackage>(), 0));
        _deleteService.DeleteFilesAsync(
                Arg.Any<IEnumerable<string>>(), Arg.Any<UnderLeaseClaims>(),
                Arg.Any<IProgress<OperationProgress>?>(), Arg.Any<CancellationToken>())
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
            string.Format(Strings.Completion_HeldBack_Singular, 1),
            vm.Completion.Skipped);
        // 3.0 MB, not 7.0: the held-back file's 4 MB is not freed space.
        Assert.Equal(
            string.Format(Strings.Completion_Freed, DisplayHelpers.FormatSize(3_145_728)),
            vm.Completion.Heading);
        await _resultLogService.Received(1).WriteAsync(
            Arg.Is<ResultLogEntry>(e => e != null && e.Operation.BytesFreed == 3_145_728),
            Arg.Any<CancellationToken>());
    }

    [Theory]
    [InlineData(OperationKinds.Move)]
    [InlineData(OperationKinds.Delete)]
    public async Task A_Move_or_Delete_report_carries_the_region_read_for_the_card_and_the_box_is_ticked(string kind)
    {
        var updater = Substitute.For<IAppUpdater>();
        updater.Kind.Returns(AppCopyKind.Slim);
        var vm = CreateViewModel(new AppSettings(), updater);
        _windowsRegion.Read().Returns("NO");
        _scanService.ScanAsync(Arg.Any<IProgress<ScanProgressUpdate>?>(), Arg.Any<CancellationToken>())
            .Returns(ScanResultWithOrphans(2));
        _moveService.MoveFilesAsync(
                Arg.Any<IEnumerable<string>>(), Arg.Any<string>(), Arg.Any<UnderLeaseClaims>(),
                Arg.Any<IProgress<OperationProgress>?>(), Arg.Any<CancellationToken>())
            .Returns(new MoveResult(2, Array.Empty<FileOperationError>()));
        _deleteService.DeleteFilesAsync(
                Arg.Any<IEnumerable<string>>(), Arg.Any<UnderLeaseClaims>(),
                Arg.Any<IProgress<OperationProgress>?>(), Arg.Any<CancellationToken>())
            .Returns(new DeleteResult(2, Array.Empty<FileOperationError>()));
        _confirmationService.ConfirmMove(
            Arg.Any<int>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<bool>()).Returns(true);
        _confirmationService.ConfirmDelete(Arg.Any<int>(), Arg.Any<string>()).Returns(true);
        _resultLogService.WriteAsync(Arg.Any<ResultLogEntry>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(true));

        await vm.Scan.ScanWithProgressAsync(null);
        if (kind == OperationKinds.Move)
        {
            vm.Cleanup.MoveDestination = Path.Combine(Path.GetTempPath(), "ic-test-move-region");
            await vm.Cleanup.MoveAllCommand.ExecuteAsync(null);
        }
        else
        {
            await vm.Cleanup.DeleteAllCommand.ExecuteAsync(null);
        }

        Assert.True(vm.Completion.OffersReport);
        Assert.True(vm.Completion.SendsReport);
        await _resultLogService.Received(1).WriteAsync(
            Arg.Is<ResultLogEntry>(e => e.Operation.Kind == kind && e.App.WindowsRegion == "NO"
                && e.App.Host == "window" && e.App.CopyKind == "slim"),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task DeleteAllAsync_a_batch_the_service_empties_reports_nothing_deleted_and_logs_nothing()
    {
        // One condition, one screen, one record. The re-read taking the whole
        // batch back is the same machine state as the pre-act re-verify taking it
        // back, so it gets that arm's screen: the kept-everything-back heading with
        // the kept-back sentence as its summary, and no result-log entry. Falling
        // through instead reported a completed delete of zero and wrote a run that
        // freed nothing into the public reports.
        //
        // NOT THE ALL-CLEAN HEADING. That one belongs to a machine with nothing to
        // do. Here everything the user confirmed was kept back, which is the opposite
        // finding, and "All clean" over a summary naming the causes would say the
        // opposite of what happened.
        //
        // AND THE DELETE HEADING RATHER THAN A WORD COVERING BOTH BUTTONS. The Move
        // twin below asserts the other one, so a change collapsing the two back into
        // one string cannot leave both green.
        var vm = CreateViewModel();
        var orphans = new List<OrphanedFile>
        {
            new(@"C:\Windows\Installer\a.msp", 1_048_576, true, false, false, Orphaned),
        };
        _scanService.ScanAsync(Arg.Any<IProgress<ScanProgressUpdate>?>(), Arg.Any<CancellationToken>())
            .Returns(new ScanResult(orphans, Array.Empty<RegisteredPackage>(), 0));
        _deleteService.DeleteFilesAsync(
                Arg.Any<IEnumerable<string>>(), Arg.Any<UnderLeaseClaims>(),
                Arg.Any<IProgress<OperationProgress>?>(), Arg.Any<CancellationToken>())
            .Returns(new DeleteResult(0, Array.Empty<FileOperationError>(),
                HeldBack: new[] { @"C:\Windows\Installer\a.msp" },
                HeldBackReasons: new HeldBackReasons(Reclaimed: 1)));
        _confirmationService.ConfirmDelete(Arg.Any<int>(), Arg.Any<string>()).Returns(true);

        await vm.Scan.ScanWithProgressAsync(null);

        await vm.Cleanup.DeleteAllCommand.ExecuteAsync(null);

        Assert.True(vm.Completion.IsComplete);
        Assert.Equal(Strings.Completion_NothingDeleted, vm.Completion.Heading);
        Assert.Equal(
            string.Format(Strings.Completion_HeldBack_Singular, 1),
            vm.Completion.Summary);
        await _resultLogService.DidNotReceive().WriteAsync(
            Arg.Any<ResultLogEntry>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task MoveAllAsync_a_batch_the_service_empties_reports_nothing_moved_and_logs_nothing()
    {
        // The Move twin, which had two more things wrong with it: the summary read
        // "0 files moved to: <folder>" and the restore line told the user how to
        // put back files that never left.
        //
        // AND ITS OWN HEADING RATHER THAN THE DELETE TWIN'S, which is the third
        // thing that was wrong with it. The argument for one string was that the
        // two are describing one machine state, and the machine state is not what
        // the heading is about: the user pressed Move, and a screen that answers
        // with a word from neither button is answering somebody else's click. It
        // costs nothing to say so, both strings having been in the file and in
        // every language since before the collapsed one was written.
        var vm = CreateViewModel();
        var orphans = new List<OrphanedFile>
        {
            new(@"C:\Windows\Installer\a.msp", 1_048_576, true, false, false, Orphaned),
        };
        _scanService.ScanAsync(Arg.Any<IProgress<ScanProgressUpdate>?>(), Arg.Any<CancellationToken>())
            .Returns(new ScanResult(orphans, Array.Empty<RegisteredPackage>(), 0));
        _moveService.MoveFilesAsync(
                Arg.Any<IEnumerable<string>>(), Arg.Any<string>(), Arg.Any<UnderLeaseClaims>(),
                Arg.Any<IProgress<OperationProgress>?>(), Arg.Any<CancellationToken>())
            .Returns(new MoveResult(0, Array.Empty<FileOperationError>(),
                HeldBack: new[] { @"C:\Windows\Installer\a.msp" },
                HeldBackReasons: new HeldBackReasons(Reclaimed: 1)));
        _confirmationService.ConfirmMove(
            Arg.Any<int>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<bool>()).Returns(true);

        await vm.Scan.ScanWithProgressAsync(null);
        vm.Cleanup.MoveDestination = Path.Combine(Path.GetTempPath(), "ic-test-heldback-all-move");

        await vm.Cleanup.MoveAllCommand.ExecuteAsync(null);

        Assert.True(vm.Completion.IsComplete);
        Assert.Equal(Strings.Completion_NothingMoved, vm.Completion.Heading);
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
            new(@"C:\Windows\Installer\c.msp", 4_194_304, true, false, false, Orphaned),
        };
        _scanService.ScanAsync(Arg.Any<IProgress<ScanProgressUpdate>?>(), Arg.Any<CancellationToken>())
            .Returns(new ScanResult(orphans, Array.Empty<RegisteredPackage>(), 0));
        _deleteService.DeleteFilesAsync(
                Arg.Any<IEnumerable<string>>(), Arg.Any<UnderLeaseClaims>(),
                Arg.Any<IProgress<OperationProgress>?>(), Arg.Any<CancellationToken>())
            .Returns(new DeleteResult(1, Array.Empty<FileOperationError>(), Cancelled: true,
                HeldBack: new[] { @"C:\Windows\Installer\c.msp" },
                HeldBackReasons: new HeldBackReasons(Reclaimed: 1)));
        _confirmationService.ConfirmDelete(Arg.Any<int>(), Arg.Any<string>()).Returns(true);

        await vm.Scan.ScanWithProgressAsync(null);

        await vm.Cleanup.DeleteAllCommand.ExecuteAsync(null);

        Assert.True(vm.Completion.IsComplete);
        Assert.Equal(
            string.Format(Strings.Completion_HeldBack_Singular, 1),
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
            new(@"C:\Windows\Installer\a.msp", 4_194_304, true, false, false, Orphaned),
            new(@"C:\Windows\Installer\b.msi", 1_048_576, false, false, false, Orphaned),
            new(@"C:\Windows\Installer\c.msi", 2_097_152, false, false, false, Orphaned),
        };
        _scanService.ScanAsync(Arg.Any<IProgress<ScanProgressUpdate>?>(), Arg.Any<CancellationToken>())
            .Returns(new ScanResult(orphans, Array.Empty<RegisteredPackage>(), 0));
        _moveService.MoveFilesAsync(
                Arg.Any<IEnumerable<string>>(), Arg.Any<string>(), Arg.Any<UnderLeaseClaims>(),
                Arg.Any<IProgress<OperationProgress>?>(), Arg.Any<CancellationToken>())
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
            string.Format(Strings.Completion_HeldBack_Singular, 1),
            vm.Completion.Skipped);
        // b.msi's 1 MB, the first file of the folded list. Held back is a.msp at
        // 4 MB, which is first in the unfolded one, so a fold that did not happen
        // shows here as 4 MB.
        Assert.Contains(DisplayHelpers.FormatSize(1_048_576), vm.Completion.Heading);
        _dialogService.Received(1).ShowWarning(
            Arg.Any<string>(), Strings.Error_MoveStoppedTitle);
    }

    [Fact]
    public async Task MoveAllAsync_an_abort_names_the_folder_the_files_are_in_not_the_one_asked_for()
    {
        // The one path in the app where those are different folders, and the
        // reason this exception exists: the destination the user typed no longer
        // resolves to where their files went. The summary line names the folder,
        // and the restore line beneath it points back at it as "that folder"
        // without naming it again, so the two lines are read as one and a summary
        // naming the typed path sends somebody to an empty folder to look for
        // files they have just been told are there.
        var vm = CreateViewModel();
        var orphans = new List<OrphanedFile>
        {
            new(@"C:\Windows\Installer\a.msi", 1_048_576, false, false, false, Orphaned),
            new(@"C:\Windows\Installer\b.msi", 2_097_152, false, false, false, Orphaned),
        };
        _scanService.ScanAsync(Arg.Any<IProgress<ScanProgressUpdate>?>(), Arg.Any<CancellationToken>())
            .Returns(new ScanResult(orphans, Array.Empty<RegisteredPackage>(), 0));
        _moveService.MoveFilesAsync(
                Arg.Any<IEnumerable<string>>(), Arg.Any<string>(), Arg.Any<UnderLeaseClaims>(),
                Arg.Any<IProgress<OperationProgress>?>(), Arg.Any<CancellationToken>())
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

    [Theory]
    [InlineData(MoveAbortReason.ResolvesElsewhere)]
    [InlineData(MoveAbortReason.StoppedResolving)]
    public async Task MoveAllAsync_a_stopped_batch_gets_its_own_heading(MoveAbortReason reason)
    {
        // The heading is what a person reads first and what a screen reader
        // announces, and the guard behind this exception has two conditions: one
        // is a destination that resolves somewhere else, the other a share that
        // dropped or an access rule that closed, which leaves the folder exactly
        // where the user put it. Nothing selects on which fired, so the four other
        // destination messages' heading is the wrong one here whichever it was.
        var vm = CreateViewModel();
        var orphans = new List<OrphanedFile>
        {
            new(@"C:\Windows\Installer\a.msi", 1_048_576, false, false, false, Orphaned),
        };
        _scanService.ScanAsync(Arg.Any<IProgress<ScanProgressUpdate>?>(), Arg.Any<CancellationToken>())
            .Returns(new ScanResult(orphans, Array.Empty<RegisteredPackage>(), 0));
        _moveService.MoveFilesAsync(
                Arg.Any<IEnumerable<string>>(), Arg.Any<string>(), Arg.Any<UnderLeaseClaims>(),
                Arg.Any<IProgress<OperationProgress>?>(), Arg.Any<CancellationToken>())
            .Returns<MoveResult>(_ => throw new MoveAbortedException(
                "stopped", new MoveResult(1, Array.Empty<FileOperationError>()),
                @"E:\where-they-really-went", reason));
        _confirmationService.ConfirmMove(
            Arg.Any<int>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<bool>()).Returns(true);

        await vm.Scan.ScanWithProgressAsync(null);
        vm.Cleanup.MoveDestination = Path.Combine(Path.GetTempPath(), "ic-test-stopped-heading");

        await vm.Cleanup.MoveAllCommand.ExecuteAsync(null);

        _dialogService.Received(1).ShowWarning(
            Arg.Any<string>(), Strings.Error_MoveStoppedTitle);
        // Beside the positive, so the heading is pinned rather than merely
        // present: the four pre-flight refusals keep the invalid-destination one
        // and this arm must not be swept back in with them.
        _dialogService.DidNotReceive().ShowWarning(
            Arg.Any<string>(), Strings.Error_InvalidDestinationTitle);
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
                Arg.Any<IEnumerable<string>>(), Arg.Any<string>(), Arg.Any<UnderLeaseClaims>(),
                Arg.Any<IProgress<OperationProgress>?>(), Arg.Any<CancellationToken>())
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
    public async Task DeleteAllAsync_a_hold_back_the_records_could_not_read_reaches_the_screen()
    {
        // The tallies are added across the two halves of the check, because either
        // can fail to read a record. Here the pre-act re-verify was healthy and the
        // under-lease re-read was not, so the file is kept back by the second half
        // alone and the screen has to account for it.
        var vm = CreateViewModel();
        var orphans = new List<OrphanedFile>
        {
            new(@"C:\Windows\Installer\a.msi", 1_048_576, false, false, false, Orphaned),
            new(@"C:\Windows\Installer\b.msp", 2_097_152, true, false, false, Orphaned),
        };
        _scanService.ScanAsync(Arg.Any<IProgress<ScanProgressUpdate>?>(), Arg.Any<CancellationToken>())
            .Returns(new ScanResult(orphans, Array.Empty<RegisteredPackage>(), 0));
        _deleteService.DeleteFilesAsync(
                Arg.Any<IEnumerable<string>>(), Arg.Any<UnderLeaseClaims>(),
                Arg.Any<IProgress<OperationProgress>?>(), Arg.Any<CancellationToken>())
            .Returns(new DeleteResult(1, Array.Empty<FileOperationError>(),
                HeldBack: new[] { @"C:\Windows\Installer\b.msp" },
                HeldBackReasons: new HeldBackReasons(RecordsUnreadable: 1)));
        _confirmationService.ConfirmDelete(Arg.Any<int>(), Arg.Any<string>()).Returns(true);

        await vm.Scan.ScanWithProgressAsync(null);

        await vm.Cleanup.DeleteAllCommand.ExecuteAsync(null);

        Assert.Equal(
            string.Format(Strings.Completion_HeldBack_Singular, 1),
            vm.Completion.Skipped);
    }

    [Fact]
    public async Task DeleteAllAsync_a_batch_held_back_two_ways_counts_both_in_one_line()
    {
        // The state neither half of the check can describe on its own: the pre-act
        // re-verify kept one file back and the under-lease re-read kept a DIFFERENT
        // one back. Two files, two causes, ONE line, and the number on it is two.
        // The regression this now catches is a fold that loses a side, which would
        // print "1 file held back" over a batch that held back two and leave the
        // overlay's own arithmetic wrong.
        var vm = CreateViewModel();
        var orphans = new List<OrphanedFile>
        {
            new(@"C:\Windows\Installer\a.msi", 1_048_576, false, false, false, Orphaned),
            new(@"C:\Windows\Installer\b.msp", 2_097_152, true, false, false, Orphaned),
            new(@"C:\Windows\Installer\c.msp", 4_194_304, true, false, false, Orphaned),
        };
        _scanService.ScanAsync(Arg.Any<IProgress<ScanProgressUpdate>?>(), Arg.Any<CancellationToken>())
            .Returns(new ScanResult(orphans, Array.Empty<RegisteredPackage>(), 0));
        _reverifier.ReverifyAsync(Arg.Any<IReadOnlyList<string>>(), Arg.Any<CancellationToken>(), Arg.Any<IProgress<ScanProgressUpdate>?>())
            .Returns(new ReverifyResult(
                new[] { @"C:\Windows\Installer\a.msi", @"C:\Windows\Installer\b.msp" },
                new[] { @"C:\Windows\Installer\c.msp" },
                new HeldBackReasons(Reclaimed: 1)));
        _deleteService.DeleteFilesAsync(
                Arg.Any<IEnumerable<string>>(), Arg.Any<UnderLeaseClaims>(),
                Arg.Any<IProgress<OperationProgress>?>(), Arg.Any<CancellationToken>())
            .Returns(new DeleteResult(1, Array.Empty<FileOperationError>(),
                HeldBack: new[] { @"C:\Windows\Installer\b.msp" },
                HeldBackReasons: new HeldBackReasons(RecordsUnreadable: 1)));
        _confirmationService.ConfirmDelete(Arg.Any<int>(), Arg.Any<string>()).Returns(true);

        await vm.Scan.ScanWithProgressAsync(null);

        await vm.Cleanup.DeleteAllCommand.ExecuteAsync(null);

        Assert.Equal(
            string.Format(Strings.Completion_HeldBack_Plural, 2),
            vm.Completion.Skipped);
        // And it is one line rather than two, which is what the collapse changed.
        Assert.DoesNotContain(Environment.NewLine, vm.Completion.Skipped, StringComparison.Ordinal);
        // A fold that dropped either half would count one and read as the singular.
        Assert.DoesNotContain(
            string.Format(Strings.Completion_HeldBack_Singular, 1),
            vm.Completion.Skipped);
    }

    [Fact]
    public async Task DeleteAllAsync_a_record_that_changed_is_counted_like_any_other_hold_back()
    {
        // The cause that once had no sentence because it had no outcome: the
        // under-lease re-read found the records no longer hold the registration the
        // claim named, and the file was released to the delete instead of kept. It
        // is kept now, and it reaches the same count as every other cause; the
        // per-cause tally survives in the result log for anyone asking which.
        var vm = CreateViewModel();
        var orphans = new List<OrphanedFile>
        {
            new(@"C:\Windows\Installer\a.msi", 1_048_576, false, false, false, Orphaned),
            new(@"C:\Windows\Installer\b.msp", 2_097_152, true, false, false, Orphaned),
        };
        _scanService.ScanAsync(Arg.Any<IProgress<ScanProgressUpdate>?>(), Arg.Any<CancellationToken>())
            .Returns(new ScanResult(orphans, Array.Empty<RegisteredPackage>(), 0));
        _deleteService.DeleteFilesAsync(
                Arg.Any<IEnumerable<string>>(), Arg.Any<UnderLeaseClaims>(),
                Arg.Any<IProgress<OperationProgress>?>(), Arg.Any<CancellationToken>())
            .Returns(new DeleteResult(1, Array.Empty<FileOperationError>(),
                HeldBack: new[] { @"C:\Windows\Installer\b.msp" },
                HeldBackReasons: new HeldBackReasons(RecordsChanged: 1)));
        _confirmationService.ConfirmDelete(Arg.Any<int>(), Arg.Any<string>()).Returns(true);

        await vm.Scan.ScanWithProgressAsync(null);

        await vm.Cleanup.DeleteAllCommand.ExecuteAsync(null);

        Assert.Equal(
            string.Format(Strings.Completion_HeldBack_Singular, 1),
            vm.Completion.Skipped);
    }

    [Fact]
    public async Task DeleteAllAsync_lock_refused_result_shows_the_dialog_and_paints_the_banner()
    {
        // The delete path's copy of the Move test above: the dialog for the click,
        // then the gate, which has a banner for a refused lock. The pair is kept
        // because the two sentences and the two titles are separate keys and
        // either could be wired to the other's.
        var vm = CreateViewModel();
        _scanService.ScanAsync(Arg.Any<IProgress<ScanProgressUpdate>?>(), Arg.Any<CancellationToken>())
            .Returns(ScanResultWithOrphans(2));
        _rebootService.Check().Returns(
            PendingRebootResult.Clean,
            PendingRebootResult.Clean,
            PendingRebootResult.Block(PendingRebootReason.MsiExecuteMutexAccessRefused));
        _deleteService.DeleteFilesAsync(
                Arg.Any<IEnumerable<string>>(), Arg.Any<UnderLeaseClaims>(),
                Arg.Any<IProgress<OperationProgress>?>(), Arg.Any<CancellationToken>())
            .Returns(new DeleteResult(0, Array.Empty<FileOperationError>(),
                InstallerLockAccessRefused: true));
        _confirmationService.ConfirmDelete(Arg.Any<int>(), Arg.Any<string>()).Returns(true);

        await vm.Scan.ScanWithProgressAsync(null);

        await vm.Cleanup.DeleteAllCommand.ExecuteAsync(null);

        _dialogService.Received(1).ShowWarning(
            Strings.Error_InstallerLockAccessRefused, Strings.Error_InstallerLockUnavailableTitle);
        // Three: the scan, the act-time gate, and the re-check this arm runs.
        _rebootService.Received(3).Check();
        Assert.True(vm.Scan.HasPendingReboot);
        Assert.Equal(Strings.Body_PendingReboot_MsiExecuteMutexAccessRefused, vm.Scan.PendingRebootBannerText);
        Assert.False(vm.Completion.IsComplete);
        await _resultLogService.DidNotReceive().WriteAsync(
            Arg.Any<ResultLogEntry>(), Arg.Any<CancellationToken>());
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task DeleteAllAsync_a_gate_re_check_that_throws_after_a_lock_refusal_shows_the_failure_dialog_without_a_rescan(
        bool accessRefused)
    {
        // The two arms that re-run the gate after the service refused at the lock.
        // The re-check throws, as the gate does when a read of Windows Installer's
        // in-progress file fails in a way none of its readings names. The service
        // touched nothing, so the counts on screen still describe the folder: the
        // window shows the failure dialog and does not rescan. A refused lock has
        // its own dialog first, and the failure dialog follows it.
        var vm = CreateViewModel();
        _scanService.ScanAsync(Arg.Any<IProgress<ScanProgressUpdate>?>(), Arg.Any<CancellationToken>())
            .Returns(ScanResultWithOrphans(2));
        var checks = 0;
        _rebootService.Check().Returns(_ => ++checks <= 2
            ? PendingRebootResult.Clean
            : throw new IOException("marker unreadable"));
        _deleteService.DeleteFilesAsync(
                Arg.Any<IEnumerable<string>>(), Arg.Any<UnderLeaseClaims>(),
                Arg.Any<IProgress<OperationProgress>?>(), Arg.Any<CancellationToken>())
            .Returns(new DeleteResult(0, Array.Empty<FileOperationError>(),
                InstallerBusy: !accessRefused, InstallerLockAccessRefused: accessRefused));
        _confirmationService.ConfirmDelete(Arg.Any<int>(), Arg.Any<string>()).Returns(true);

        await vm.Scan.ScanWithProgressAsync(null);

        await vm.Cleanup.DeleteAllCommand.ExecuteAsync(null);

        _rebootService.Received(3).Check();
        _dialogService.Received(accessRefused ? 2 : 1).ShowWarning(Arg.Any<string>(), Arg.Any<string>());
        _dialogService.Received(1).ShowWarning(
            Arg.Is<string>(s => s.Contains(nameof(IOException))), Strings.Error_DeleteFailedTitle);
        if (accessRefused)
        {
            Received.InOrder(() =>
            {
                _dialogService.ShowWarning(
                    Strings.Error_InstallerLockAccessRefused, Strings.Error_InstallerLockUnavailableTitle);
                _dialogService.ShowWarning(Arg.Any<string>(), Strings.Error_DeleteFailedTitle);
            });
        }
        await _scanService.Received(1).ScanAsync(
            Arg.Any<IProgress<ScanProgressUpdate>?>(), Arg.Any<CancellationToken>());
        Assert.Equal(2, vm.Scan.OrphanedFileCount);
        Assert.False(vm.Completion.IsComplete);
        Assert.False(vm.Cleanup.IsOperating);
        Assert.Equal(string.Empty, vm.Cleanup.OperationProgress);
        Assert.True(vm.Cleanup.MoveAllCommand.CanExecute(null));
        Assert.True(vm.Cleanup.DeleteAllCommand.CanExecute(null));
    }

    [Fact]
    public async Task DeleteAllAsync_lock_unavailable_result_shows_the_dialog_and_leaves_the_gate_alone()
    {
        // The twin of the installer-busy test above, and the difference between
        // them is the whole reason this is a second flag rather than a second
        // cause behind the first. Busy re-runs the pending-reboot gate, which now
        // meets the held mutex and paints its banner. This one must NOT, because
        // the gate has no account of the condition: its probe reads the same
        // failure as not held, so a re-check comes back clean and leaves the user
        // refused with no reason on screen. The dialog carries it instead, and the
        // title is pinned as well as the body because this arm borrowed the crash
        // arm's title once.
        var vm = CreateViewModel();
        _scanService.ScanAsync(Arg.Any<IProgress<ScanProgressUpdate>?>(), Arg.Any<CancellationToken>())
            .Returns(ScanResultWithOrphans(2));
        _deleteService.DeleteFilesAsync(
                Arg.Any<IEnumerable<string>>(), Arg.Any<UnderLeaseClaims>(),
                Arg.Any<IProgress<OperationProgress>?>(), Arg.Any<CancellationToken>())
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
    public void The_small_tip_button_opens_the_tip_page_with_the_windows_region()
    {
        _windowsRegion.Read().Returns("SE");
        var vm = CreateViewModel();

        vm.Completion.DonateSmallCommand.Execute(null);

        _windowService.Received(1).OpenUrl(SupportLink.TipPage(Localisation.UiCulture, "SE"));
        _windowService.Received(1).OpenUrl(Arg.Is<string>(u => u.EndsWith("&region=SE", StringComparison.Ordinal)));
    }

    // The report box. The start check, the mark and the Windows region are faked, so each
    // test fixes whether the PC has had its first run and which Country or region Windows gives.
    // vm.Dispose() is the barrier for the sends and saves, which run off the
    // dispatcher: it settles a card still up and waits for them, as closing the app
    // does.

    /// <summary>
    /// Every settings save the report makes, kept so a test can apply them all afterwards
    /// and read what settings.json would hold.
    /// </summary>
    private List<Action<AppSettings>> RecordSettingsSaves()
    {
        var saves = new List<Action<AppSettings>>();
        _settingsService.Update(Arg.Any<Action<AppSettings>>()).Returns(ci =>
        {
            lock (saves) saves.Add((Action<AppSettings>)ci[0]!);
            return true;
        });
        return saves;
    }

    private static AppSettings AppliedInOrder(List<Action<AppSettings>> saves)
    {
        var settings = new AppSettings();
        lock (saves)
            foreach (var save in saves) save(settings);
        return settings;
    }

    /// <summary>The scan finds nothing, the report is written, and the region is <paramref name="region"/>.</summary>
    private MainViewModel FirstRunAllClear(string? region = "GB", IAppUpdater? appUpdater = null)
    {
        var vm = CreateViewModel(new AppSettings(), appUpdater);
        _windowsRegion.Read().Returns(region);
        _scanService.ScanAsync(Arg.Any<IProgress<ScanProgressUpdate>?>(), Arg.Any<CancellationToken>())
            .Returns(EmptyScanResult());
        _resultLogService.WriteAsync(Arg.Any<ResultLogEntry>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(true));
        _resultLogService.ReadLastLogAsync(Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<string?>("{\"schemaVersion\":5}"));
        return vm;
    }

    [Fact]
    public async Task The_PCs_first_card_carries_the_box_ticked_writes_the_report_and_claims_the_mark()
    {
        var vm = FirstRunAllClear();

        await vm.Scan.ScanCommand.ExecuteAsync(null);

        Assert.True(vm.Completion.IsComplete);
        Assert.True(vm.Completion.OffersReport);
        Assert.True(vm.Completion.SendsReport);
        await _resultLogService.Received(1).WriteAsync(Arg.Any<ResultLogEntry>(), Arg.Any<CancellationToken>());
        _firstRunMark.Received(1).TryClaim();
    }

    [Theory]
    [InlineData("DE", "DE")]
    [InlineData("NO", "NO")]
    [InlineData("GB", "GB")]
    [InlineData("US", "US")]
    [InlineData("419", "419")]
    [InlineData(null, WindowsRegionLabel.Unreadable)]
    public async Task The_box_starts_ticked_whatever_Country_or_region_Windows_says_and_the_report_carries_it(
        string? region, string reported)
    {
        var vm = FirstRunAllClear(region);

        await vm.Scan.ScanCommand.ExecuteAsync(null);

        Assert.True(vm.Completion.OffersReport);
        Assert.True(vm.Completion.SendsReport);
        await _resultLogService.Received(1).WriteAsync(
            Arg.Is<ResultLogEntry>(e => e.App.WindowsRegion == reported), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task The_report_carries_the_region_read_for_the_card_not_a_later_read()
    {
        var vm = FirstRunAllClear("GB");
        _windowsRegion.Read().Returns("GB", "DE");

        await vm.Scan.ScanCommand.ExecuteAsync(null);

        Assert.True(vm.Completion.SendsReport);
        await _resultLogService.Received(1).WriteAsync(
            Arg.Is<ResultLogEntry>(e => e.App.WindowsRegion == "GB"), Arg.Any<CancellationToken>());
        _windowsRegion.Received(1).Read();
    }

    [Theory]
    [InlineData(AppCopyKind.Setup, "setup")]
    [InlineData(AppCopyKind.Portable, "portable")]
    [InlineData(AppCopyKind.Slim, "slim")]
    [InlineData(AppCopyKind.Scoop, "scoop")]
    [InlineData(AppCopyKind.FromSource, "fromSource")]
    [InlineData(AppCopyKind.Undetermined, "undetermined")]
    public async Task The_report_names_the_window_and_the_kind_of_copy_the_app_decided(
        AppCopyKind kind, string reported)
    {
        var updater = Substitute.For<IAppUpdater>();
        updater.Kind.Returns(kind);
        var vm = FirstRunAllClear(appUpdater: updater);

        await vm.Scan.ScanCommand.ExecuteAsync(null);

        await _resultLogService.Received(1).WriteAsync(
            Arg.Is<ResultLogEntry>(e => e.App.Host == "window" && e.App.CopyKind == reported),
            Arg.Any<CancellationToken>());
    }

    [Theory]
    [InlineData(AppCopyKind.Setup)]
    [InlineData(AppCopyKind.Portable)]
    [InlineData(AppCopyKind.Scoop)]
    public void The_card_carries_the_kind_of_copy_the_app_decided(AppCopyKind kind)
    {
        var updater = Substitute.For<IAppUpdater>();
        updater.Kind.Returns(kind);

        var vm = CreateViewModel(new AppSettings(), updater);

        Assert.Equal(kind, vm.Completion.ReportCopy);
    }

    [Fact]
    public void The_card_without_an_updater_carries_an_undetermined_copy()
    {
        var vm = CreateViewModel();

        Assert.Equal(AppCopyKind.Undetermined, vm.Completion.ReportCopy);
    }

    [Fact]
    public async Task A_report_written_without_an_updater_names_an_undetermined_copy()
    {
        var vm = FirstRunAllClear();

        await vm.Scan.ScanCommand.ExecuteAsync(null);

        await _resultLogService.Received(1).WriteAsync(
            Arg.Is<ResultLogEntry>(e => e.App.Host == "window" && e.App.CopyKind == "undetermined"),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task A_PC_whose_first_run_is_recorded_shows_no_box_and_writes_nothing()
    {
        // The same for a second Windows account, or an account carrying an older
        // version's last-run.json: the check answers that the PC has had its first run.
        var vm = FirstRunAllClear();
        _earlierRunCheck.ShowsAnEarlierRunWithinAsync(Arg.Any<TimeSpan>()).Returns(Task.FromResult(true));

        await vm.Scan.ScanCommand.ExecuteAsync(null);

        Assert.True(vm.Completion.IsComplete);
        Assert.False(vm.Completion.OffersReport);
        await _resultLogService.DidNotReceive().WriteAsync(Arg.Any<ResultLogEntry>(), Arg.Any<CancellationToken>());
        _firstRunMark.DidNotReceive().Set();
    }

    [Fact]
    public async Task A_later_card_in_the_same_sitting_carries_no_box_and_writes_nothing()
    {
        // The check keeps answering "first run" all sitting, as the real one does once
        // its answer is taken, so only the sitting's own record can keep the box off.
        var vm = FirstRunAllClear();
        await vm.Scan.ScanCommand.ExecuteAsync(null);
        Assert.True(vm.Completion.OffersReport);
        vm.Completion.DismissCommand.Execute(null);

        await vm.Scan.ScanCommand.ExecuteAsync(null);

        Assert.True(vm.Completion.IsComplete);
        Assert.False(vm.Completion.OffersReport);
        await _resultLogService.Received(1).WriteAsync(Arg.Any<ResultLogEntry>(), Arg.Any<CancellationToken>());
        await _earlierRunCheck.Received(1).ShowsAnEarlierRunWithinAsync(Arg.Any<TimeSpan>());
        _firstRunMark.Received(1).TryClaim();
    }

    public static TheoryData<string> WaysToCloseTheCard => new() { "Done", "Esc", "Donate", "Window" };

    [Theory]
    [MemberData(nameof(WaysToCloseTheCard))]
    public async Task Each_way_of_closing_the_card_sends_the_report_when_the_box_is_ticked(string way)
    {
        var vm = FirstRunAllClear();
        var saves = RecordSettingsSaves();
        _resultLogService.SendAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(ResultLogSendOutcome.Sent);
        await vm.Scan.ScanCommand.ExecuteAsync(null);

        switch (way)
        {
            case "Done": vm.Completion.DismissCommand.Execute(null); break;
            case "Esc": Assert.True(vm.HandleEscape()); break;
            case "Donate": vm.Completion.DonateCommand.Execute(null); break;
            case "Window": break;
        }
        vm.Dispose();

        await _resultLogService.Received(1).SendAsync("{\"schemaVersion\":5}", Arg.Any<CancellationToken>());
        Assert.False(vm.Completion.OffersReport);
        var settings = AppliedInOrder(saves);
        Assert.True(settings.HasSentResultLog);
        Assert.False(settings.ReportSendPending);
    }

    [Fact]
    public async Task Closing_the_card_with_the_box_unticked_sends_nothing_and_leaves_nothing_waiting()
    {
        var vm = FirstRunAllClear();
        var saves = RecordSettingsSaves();
        await vm.Scan.ScanCommand.ExecuteAsync(null);

        vm.Completion.SendsReport = false;
        vm.Completion.DismissCommand.Execute(null);
        vm.Dispose();

        await _resultLogService.DidNotReceive().SendAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
        // The mark is set all the same: this was the PC's first run.
        _firstRunMark.Received(1).TryClaim();
        var settings = AppliedInOrder(saves);
        Assert.False(settings.ReportSendPending);
        Assert.False(settings.HasSentResultLog);
    }

    [Fact]
    public async Task While_the_card_is_up_nothing_is_recorded_as_waiting_whatever_the_box_says()
    {
        var vm = FirstRunAllClear();
        var saves = RecordSettingsSaves();
        await vm.Scan.ScanCommand.ExecuteAsync(null);
        await vm.Completion.ReportWork;
        Assert.True(vm.Completion.OffersReport);
        Assert.False(AppliedInOrder(saves).ReportSendPending);

        vm.Completion.SendsReport = false;
        vm.Completion.SendsReport = true;
        await vm.Completion.ReportWork;

        Assert.True(vm.Completion.OffersReport);
        Assert.Empty(saves);
        await _resultLogService.DidNotReceive().SendAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Theory]
    [InlineData(true, 1)]
    [InlineData(false, 0)]
    public async Task What_the_box_says_as_the_card_closes_decides_after_it_was_toggled(bool endsTicked, int sends)
    {
        var vm = FirstRunAllClear();
        _resultLogService.SendAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(ResultLogSendOutcome.Sent);
        await vm.Scan.ScanCommand.ExecuteAsync(null);

        vm.Completion.SendsReport = !endsTicked;
        vm.Completion.SendsReport = endsTicked;
        vm.Completion.SendsReport = !endsTicked;
        vm.Completion.SendsReport = endsTicked;
        vm.Completion.SendsReport = !endsTicked;
        vm.Completion.SendsReport = endsTicked;
        vm.Completion.DismissCommand.Execute(null);
        vm.Dispose();

        await _resultLogService.Received(sends).SendAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task A_send_that_fails_leaves_the_report_waiting_for_a_later_start()
    {
        var vm = FirstRunAllClear();
        var saves = RecordSettingsSaves();
        _resultLogService.SendAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(ResultLogSendOutcome.NetworkUnavailable);
        await vm.Scan.ScanCommand.ExecuteAsync(null);

        vm.Completion.DismissCommand.Execute(null);
        vm.Dispose();

        await _resultLogService.Received(1).SendAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
        var settings = AppliedInOrder(saves);
        Assert.True(settings.ReportSendPending);
        Assert.False(settings.HasSentResultLog);
    }

    [Fact]
    public async Task A_report_that_could_not_be_written_takes_the_box_away_and_sends_nothing()
    {
        var vm = FirstRunAllClear();
        _resultLogService.WriteAsync(Arg.Any<ResultLogEntry>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(false));
        await vm.Scan.ScanCommand.ExecuteAsync(null);

        Assert.True(vm.Completion.IsComplete);
        Assert.False(vm.Completion.OffersReport);
        // The write was attempted, so this was the PC's first run.
        _firstRunMark.Received(1).TryClaim();

        vm.Completion.DismissCommand.Execute(null);
        vm.Dispose();
        await _resultLogService.DidNotReceive().SendAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task A_card_closed_before_its_report_is_written_sends_it_once_it_is()
    {
        var vm = FirstRunAllClear();
        var write = new TaskCompletionSource<bool>();
        var writeStarted = new TaskCompletionSource();
        _resultLogService.WriteAsync(Arg.Any<ResultLogEntry>(), Arg.Any<CancellationToken>())
            .Returns(_ =>
            {
                writeStarted.TrySetResult();
                return write.Task;
            });
        _resultLogService.SendAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(ResultLogSendOutcome.Sent);
        var scan = vm.Scan.ScanCommand.ExecuteAsync(null);
        await writeStarted.Task.WaitAsync(TimeSpan.FromSeconds(10));

        Assert.True(vm.Completion.IsComplete);
        Assert.True(vm.Completion.OffersReport);
        vm.Completion.DismissCommand.Execute(null);
        await _resultLogService.DidNotReceive().SendAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());

        write.SetResult(true);
        await scan;
        vm.Dispose();

        await _resultLogService.Received(1).SendAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    // The panel the "i" beside the box opens, saying what the report holds. Opened here
    // by setting ReportPanelOpen, as the "i"'s two-way binding does.

    [Fact]
    public async Task The_report_panel_starts_closed_on_the_card_that_carries_the_box()
    {
        var vm = FirstRunAllClear();

        await vm.Scan.ScanCommand.ExecuteAsync(null);

        Assert.True(vm.Completion.OffersReport);
        Assert.False(vm.Completion.ReportPanelOpen);
    }

    [Fact]
    public async Task Esc_closes_the_report_panel_and_leaves_the_card_and_the_box_as_they_are()
    {
        var vm = FirstRunAllClear();
        _resultLogService.SendAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(ResultLogSendOutcome.Sent);
        await vm.Scan.ScanCommand.ExecuteAsync(null);
        vm.Completion.ReportPanelOpen = true;

        Assert.True(vm.HandleEscape());

        Assert.False(vm.Completion.ReportPanelOpen);
        Assert.True(vm.Completion.IsComplete);
        Assert.True(vm.Completion.OffersReport);
        Assert.True(vm.Completion.SendsReport);
        await vm.Completion.ReportWork;
        await _resultLogService.DidNotReceive().SendAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());

        // The next Esc is the card's, and closing it sends the report as usual.
        Assert.True(vm.HandleEscape());
        Assert.False(vm.Completion.IsComplete);
        vm.Dispose();
        await _resultLogService.Received(1).SendAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    public static TheoryData<string> WaysToCloseTheCardWithThePanelOpen => new() { "Done", "Donate", "Window" };

    [Theory]
    [MemberData(nameof(WaysToCloseTheCardWithThePanelOpen))]
    public async Task Closing_the_card_closes_the_report_panel_with_it(string way)
    {
        var vm = FirstRunAllClear();
        await vm.Scan.ScanCommand.ExecuteAsync(null);
        Assert.True(vm.Completion.OffersReport);
        vm.Completion.ReportPanelOpen = true;

        switch (way)
        {
            case "Done": vm.Completion.DismissCommand.Execute(null); break;
            case "Donate": vm.Completion.DonateCommand.Execute(null); break;
            case "Window": vm.Dispose(); break;
        }

        Assert.False(vm.Completion.ReportPanelOpen);
        Assert.False(vm.Completion.OffersReport);
        if (way != "Window") vm.Dispose();
    }

    [Fact]
    public async Task A_report_that_could_not_be_written_closes_the_report_panel_with_the_box()
    {
        var vm = FirstRunAllClear();
        var write = new TaskCompletionSource<bool>();
        var writeStarted = new TaskCompletionSource();
        _resultLogService.WriteAsync(Arg.Any<ResultLogEntry>(), Arg.Any<CancellationToken>())
            .Returns(_ =>
            {
                writeStarted.TrySetResult();
                return write.Task;
            });
        var scan = vm.Scan.ScanCommand.ExecuteAsync(null);
        await writeStarted.Task.WaitAsync(TimeSpan.FromSeconds(10));
        vm.Completion.ReportPanelOpen = true;
        // The card's write runs behind the scan rather than inside it, so the box going
        // is waited for rather than the scan.
        var boxGone = new TaskCompletionSource();
        vm.Completion.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName == nameof(CompletionViewModel.OffersReport) && !vm.Completion.OffersReport)
                boxGone.TrySetResult();
        };

        write.SetResult(false);
        await scan;
        await boxGone.Task.WaitAsync(TimeSpan.FromSeconds(10));

        Assert.True(vm.Completion.IsComplete);
        Assert.False(vm.Completion.OffersReport);
        Assert.False(vm.Completion.ReportPanelOpen);
        vm.Dispose();
    }

    [Theory]
    [InlineData(true, true, false, true)]
    [InlineData(true, false, false, false)]
    [InlineData(true, true, true, false)]
    [InlineData(false, true, false, false)]
    public void A_start_sends_this_accounts_report_only_where_it_is_still_waiting(
        bool read, bool reportSendPending, bool hasSent, bool sends)
    {
        // The second row is a last-run.json from an older version, which never has
        // ReportSendPending beside it; the last is a settings file that could not be read.
        var vm = CreateViewModel();
        _settingsService.TryLoad(out _).ReturnsForAnyArgs(ci =>
        {
            ci[0] = new AppSettings { ReportSendPending = reportSendPending, HasSentResultLog = hasSent };
            return read;
        });
        _resultLogService.ReadLastLogAsync(Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<string?>("{\"schemaVersion\":5}"));
        _resultLogService.SendAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(ResultLogSendOutcome.Sent);

        vm.Completion.StartSavedReportRetry();
        vm.Dispose();

        _resultLogService.Received(sends ? 1 : 0).SendAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
        _earlierRunCheck.Received(1).ShowsAnEarlierRunAsync();
    }

    [Fact]
    public async Task A_run_that_held_every_file_back_leaves_the_first_run_to_the_next_card()
    {
        var vm = FirstRunAllClear();
        _scanService.ScanAsync(Arg.Any<IProgress<ScanProgressUpdate>?>(), Arg.Any<CancellationToken>())
            .Returns(ScanResultWithOrphans(1), EmptyScanResult());
        _reverifier.ReverifyAsync(Arg.Any<IReadOnlyList<string>>(), Arg.Any<CancellationToken>(), Arg.Any<IProgress<ScanProgressUpdate>?>())
            .Returns(ci => new ReverifyResult(
                Array.Empty<string>(), (IReadOnlyList<string>)ci[0]!, new HeldBackReasons(Reclaimed: 1)));
        _confirmationService.ConfirmDelete(Arg.Any<int>(), Arg.Any<string>()).Returns(true);

        await vm.Scan.ScanWithProgressAsync(null);
        await vm.Cleanup.DeleteAllCommand.ExecuteAsync(null);

        Assert.True(vm.Completion.IsComplete);
        Assert.False(vm.Completion.OffersReport);
        _firstRunMark.DidNotReceive().Set();
        await _resultLogService.DidNotReceive().WriteAsync(Arg.Any<ResultLogEntry>(), Arg.Any<CancellationToken>());

        // The control: the next card in the sitting is the PC's first.
        vm.Completion.DismissCommand.Execute(null);
        await vm.Scan.ScanCommand.ExecuteAsync(null);
        Assert.True(vm.Completion.OffersReport);
    }

    public static TheoryData<string> RunsThatActedWithoutAReport =>
        new() { "Move cancelled", "Delete cancelled", "Move stopped", "Move failed", "Delete failed" };

    [Theory]
    [MemberData(nameof(RunsThatActedWithoutAReport))]
    public async Task A_run_that_acted_on_files_without_a_report_records_the_first_run(string run)
    {
        var vm = FirstRunAllClear();
        _scanService.ScanAsync(Arg.Any<IProgress<ScanProgressUpdate>?>(), Arg.Any<CancellationToken>())
            .Returns(ScanResultWithOrphans(2), EmptyScanResult());
        _confirmationService.ConfirmMove(
            Arg.Any<int>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<bool>()).Returns(true);
        _confirmationService.ConfirmDelete(Arg.Any<int>(), Arg.Any<string>()).Returns(true);
        Task<MoveResult> MoveCall() => _moveService.MoveFilesAsync(
            Arg.Any<IEnumerable<string>>(), Arg.Any<string>(), Arg.Any<UnderLeaseClaims>(),
            Arg.Any<IProgress<OperationProgress>?>(), Arg.Any<CancellationToken>());
        Task<DeleteResult> DeleteCall() => _deleteService.DeleteFilesAsync(
            Arg.Any<IEnumerable<string>>(), Arg.Any<UnderLeaseClaims>(),
            Arg.Any<IProgress<OperationProgress>?>(), Arg.Any<CancellationToken>());
        switch (run)
        {
            case "Move cancelled":
                MoveCall().Returns(new MoveResult(1, Array.Empty<FileOperationError>(), Cancelled: true));
                break;
            case "Delete cancelled":
                DeleteCall().Returns(new DeleteResult(1, Array.Empty<FileOperationError>(), Cancelled: true));
                break;
            case "Move stopped":
                MoveCall().Returns<MoveResult>(_ => throw new MoveAbortedException(
                    "stopped", new MoveResult(1, Array.Empty<FileOperationError>()),
                    @"D:\Backup", MoveAbortReason.ResolvesElsewhere));
                break;
            case "Move failed":
                MoveCall().Returns<MoveResult>(_ => throw new InvalidOperationException("planted"));
                break;
            case "Delete failed":
                DeleteCall().Returns<DeleteResult>(_ => throw new InvalidOperationException("planted"));
                break;
        }

        await vm.Scan.ScanWithProgressAsync(null);
        if (run.StartsWith("Move", StringComparison.Ordinal))
        {
            vm.Cleanup.MoveDestination = Path.Combine(Path.GetTempPath(), "ic-test-first-run-move");
            await vm.Cleanup.MoveAllCommand.ExecuteAsync(null);
        }
        else
        {
            await vm.Cleanup.DeleteAllCommand.ExecuteAsync(null);
        }

        _firstRunMark.Received(1).Set();
        Assert.False(vm.Completion.OffersReport);
        await _resultLogService.DidNotReceive().WriteAsync(Arg.Any<ResultLogEntry>(), Arg.Any<CancellationToken>());

        // And no later card in the sitting carries the box.
        if (vm.Completion.IsComplete) vm.Completion.DismissCommand.Execute(null);
        await vm.Scan.ScanCommand.ExecuteAsync(null);
        Assert.True(vm.Completion.IsComplete);
        Assert.False(vm.Completion.OffersReport);
    }

    [Fact]
    public async Task A_cancelled_run_that_reached_no_file_leaves_the_first_run_to_the_next_card()
    {
        // The control for the theory above: the same cancel with nothing moved.
        var vm = FirstRunAllClear();
        _scanService.ScanAsync(Arg.Any<IProgress<ScanProgressUpdate>?>(), Arg.Any<CancellationToken>())
            .Returns(ScanResultWithOrphans(2), EmptyScanResult());
        _confirmationService.ConfirmDelete(Arg.Any<int>(), Arg.Any<string>()).Returns(true);
        _deleteService.DeleteFilesAsync(
                Arg.Any<IEnumerable<string>>(), Arg.Any<UnderLeaseClaims>(),
                Arg.Any<IProgress<OperationProgress>?>(), Arg.Any<CancellationToken>())
            .Returns(new DeleteResult(0, Array.Empty<FileOperationError>(), Cancelled: true));

        await vm.Scan.ScanWithProgressAsync(null);
        await vm.Cleanup.DeleteAllCommand.ExecuteAsync(null);

        _firstRunMark.DidNotReceive().Set();
        await vm.Scan.ScanCommand.ExecuteAsync(null);
        Assert.True(vm.Completion.OffersReport);
    }

    [Fact]
    public async Task A_card_waiting_on_the_start_check_is_not_put_up_for_a_scan_since_replaced()
    {
        var vm = FirstRunAllClear();
        var answer = new TaskCompletionSource<bool>();
        _earlierRunCheck.ShowsAnEarlierRunWithinAsync(Arg.Any<TimeSpan>()).Returns(answer.Task);
        _scanService.ScanAsync(Arg.Any<IProgress<ScanProgressUpdate>?>(), Arg.Any<CancellationToken>())
            .Returns(EmptyScanResult(), ScanResultWithOrphans(1));

        await vm.Scan.ScanCommand.ExecuteAsync(null);
        Assert.False(vm.ScanCardDecided.IsCompleted);
        await vm.Scan.ScanCommand.ExecuteAsync(null);
        answer.SetResult(false);
        await vm.ScanCardDecided;

        Assert.False(vm.Completion.IsComplete);
        Assert.False(vm.Completion.OffersReport);
        _firstRunMark.DidNotReceive().Set();
    }

    [Fact]
    public async Task The_scan_card_is_decided_once_the_start_check_answers()
    {
        // The control for the test above, and what App waits on before it builds the
        // window: the card goes up, with its box, as the check answers.
        var vm = FirstRunAllClear();
        var answer = new TaskCompletionSource<bool>();
        _earlierRunCheck.ShowsAnEarlierRunWithinAsync(Arg.Any<TimeSpan>()).Returns(answer.Task);

        await vm.Scan.ScanWithProgressAsync(null);
        Assert.False(vm.ScanCardDecided.IsCompleted);
        Assert.False(vm.Completion.IsComplete);

        answer.SetResult(false);
        await vm.ScanCardDecided;

        Assert.True(vm.Completion.IsComplete);
        Assert.True(vm.Completion.OffersReport);
    }

    [Fact]
    public async Task ScanViewModel_missing_files_line_names_the_programs()
    {
        // The line is a count and the programs behind it. A bare count would be
        // an alarm with nothing in it: the one thing a reader can act on is which
        // program to run an installer for, and the count alone does not say.
        var vm = CreateViewModel();

        var scan = new ScanResult(
            RemovableFiles: Array.Empty<OrphanedFile>(),
            RegisteredPackages: new[]
            {
                new RegisteredPackage(@"C:\Windows\Installer\a.msi", "Contoso Reader", "{aaa}", FileExists: false),
                new RegisteredPackage(@"C:\Windows\Installer\b.msp", "Contoso Reader", "{aaa}", FileExists: false),
                new RegisteredPackage(@"C:\Windows\Installer\c.msi", "Fabrikam Suite", "{bbb}", FileExists: false),
            },
            RegisteredTotalBytes: 0,
            MissingAffectedCount: 3);
        _scanService.ScanAsync(Arg.Any<IProgress<ScanProgressUpdate>?>(), Arg.Any<CancellationToken>())
            .Returns(scan);

        await vm.Scan.ScanCommand.ExecuteAsync(null);

        Assert.True(vm.Scan.HasMissingFromDisk);
        Assert.Equal(3, vm.Scan.MissingFromDiskCount);
        Assert.Contains("3", vm.Scan.MissingFromDiskSummaryText);
        Assert.Contains("Contoso Reader", vm.Scan.MissingFromDiskSummaryText);
        Assert.Contains("Fabrikam Suite", vm.Scan.MissingFromDiskSummaryText);
    }

    [Fact]
    public async Task ScanViewModel_missing_files_line_fires_for_a_superseded_patch_too()
    {
        // THE REVERSE OF THE TEST THIS REPLACES, which held that a superseded
        // patch whose file had gone must never reach this line because the file
        // having gone was its expected end state. Windows opens every registered
        // patch's cached file whether superseded or not, so the record is the same
        // condition as any other and the line covers both. The scan result carries
        // the split apart; the line does not.
        var vm = CreateViewModel();

        var scan = new ScanResult(
            RemovableFiles: Array.Empty<OrphanedFile>(),
            RegisteredPackages: new[]
            {
                new RegisteredPackage(@"C:\Windows\Installer\superseded.msp", "Contoso Reader", "{bbb}",
                    PatchState: 2, FileExists: false),
            },
            RegisteredTotalBytes: 0,
            // AFFECTED, and the swap is the point of this test rather than a detail. A
            // superseded patch reaches this line when the scan could not establish that
            // nothing could roll back onto its file, which is what a row carrying no
            // product verdict means. Built in the other half it would be the one case
            // the line is right to stay silent about, and the test would be asserting
            // that the app alarms on a file it has shown to be spare.
            MissingAffectedCount: 1,
            MissingUnaffectedCount: 0);
        _scanService.ScanAsync(Arg.Any<IProgress<ScanProgressUpdate>?>(), Arg.Any<CancellationToken>())
            .Returns(scan);

        await vm.Scan.ScanCommand.ExecuteAsync(null);

        Assert.True(vm.Scan.HasMissingFromDisk);
        Assert.Equal(1, vm.Scan.MissingFromDiskCount);
        Assert.Contains("Contoso Reader", vm.Scan.MissingFromDiskSummaryText);
    }

    [Fact]
    public async Task ScanViewModel_missing_files_line_says_nothing_about_what_removed_them()
    {
        // A hard constraint rather than a nuance. Every tool that deletes from
        // that folder leaves the same record, this one included up to v2.3.0, so
        // the app cannot tell whose work it is looking at. The line fires on
        // machines it has never run on.
        var vm = CreateViewModel();

        var scan = new ScanResult(
            RemovableFiles: Array.Empty<OrphanedFile>(),
            RegisteredPackages: new[]
            {
                new RegisteredPackage(@"C:\Windows\Installer\a.msi", "Contoso Reader", "{aaa}", FileExists: false),
            },
            RegisteredTotalBytes: 0,
            MissingAffectedCount: 1);
        _scanService.ScanAsync(Arg.Any<IProgress<ScanProgressUpdate>?>(), Arg.Any<CancellationToken>())
            .Returns(scan);

        await vm.Scan.ScanCommand.ExecuteAsync(null);

        var line = vm.Scan.MissingFromDiskSummaryText;
        Assert.DoesNotContain("InstallerClean", line);
        Assert.DoesNotContain("another tool", line, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("deleted", line, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("removed", line, StringComparison.OrdinalIgnoreCase);
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
        // StartsWith for the reason the sibling above gives: the crash-log sentence
        // is appended after the account where the host could write one, and what this
        // test is about is the account reaching the window at all.
        Assert.StartsWith(Strings.Error_InstallerDbEmpty, vm.Scan.LastScanError,
            StringComparison.Ordinal);
        // The window's intro shows the diagnosis, not "nothing scanned yet", under
        // the heading a Re-scan's dialog would carry, and the startup path is
        // inline: no modal fires over the splash.
        Assert.Equal(Strings.Error_StoppedTitle, vm.IntroLead);
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
        _dialogService.Received(1).ShowError(Arg.Any<string>(), Strings.Error_StoppedTitle);
        // ...and the same message is recorded inline, so the window under the
        // modal shows the diagnosis, as a failed startup scan's does, under the
        // modal's own heading.
        Assert.True(vm.Scan.HasScanError);
        Assert.Equal(Strings.Error_StoppedTitle, vm.IntroLead);
    }

    [Theory]
    [InlineData("stop")]
    [InlineData("refused")]
    [InlineData("unforeseen")]
    public async Task A_failed_rescan_heads_the_window_as_its_dialog_is_headed(string ending)
    {
        // Each rung of the ladder has its own heading, and the window carries the
        // one its dialog did, so the two are not one click apart and saying
        // different things.
        var vm = CreateViewModel();
        Exception ex = ending switch
        {
            "stop" => new LocalisedInvalidOperationException(Strings.Error_InstallerDbEmpty),
            "refused" => new UnauthorizedAccessException("denied"),
            _ => new InvalidProgramException("nothing anticipated this"),
        };
        var expected = ending switch
        {
            "stop" => Strings.Error_StoppedTitle,
            "refused" => Strings.Error_AdminRequiredTitle,
            _ => Strings.Error_ScanFailedTitle,
        };
        var headings = new List<string>();
        _dialogService.When(d => d.ShowError(Arg.Any<string>(), Arg.Any<string>()))
            .Do(ci => headings.Add(ci.ArgAt<string>(1)));
        _dialogService.When(d => d.ShowWarning(Arg.Any<string>(), Arg.Any<string>()))
            .Do(ci => headings.Add(ci.ArgAt<string>(1)));
        _scanService.ScanAsync(Arg.Any<IProgress<ScanProgressUpdate>?>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(ex);

        await vm.Scan.ScanCommand.ExecuteAsync(null);

        Assert.Equal(new[] { expected }, headings);
        Assert.Equal(expected, vm.IntroLead);
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

    // THE WINDOW AFTER A SCAN THAT LEAVES NO LIST. After an earlier scan completed,
    // three endings do that: a Re-scan that stops or fails, the scan after a Move or
    // Delete that stops or fails, and that scan cancelled by the user. Each leaves no
    // list on screen and nothing for Move, Delete or Details to act on. The
    // assertions are on the commands and not on the layout, so they hold whatever
    // the window hides.

    /// <summary>
    /// Three files offered and one left alone, so both count lines, both Details
    /// buttons and both action commands are live before the scan under test.
    /// </summary>
    private static ScanResult AnOfferAndAFileLeftAlone() =>
        new(ScanResultWithOrphans(3).RemovableFiles,
            new[] { new RegisteredPackage(@"C:\Windows\Installer\kept.msi", "Contoso Reader", "{aaa}") },
            4096);

    private static void AssertEverythingIsLive(MainViewModel vm)
    {
        Assert.True(vm.Scan.HasScanned);
        Assert.Equal(3, vm.Scan.OrphanedFileCount);
        Assert.Equal(1, vm.Scan.RegisteredFileCount);
        Assert.True(vm.Cleanup.MoveAllCommand.CanExecute(null));
        Assert.True(vm.Cleanup.DeleteAllCommand.CanExecute(null));
        Assert.True(vm.Chrome.OpenOrphanedDetailsCommand.CanExecute(null));
        Assert.True(vm.Chrome.OpenRegisteredDetailsCommand.CanExecute(null));
    }

    private static void AssertNoListAndNothingActsOnOne(MainViewModel vm)
    {
        Assert.False(vm.Scan.HasScanned);
        Assert.Null(vm.Scan.LastScanResult);
        Assert.Equal(0, vm.Scan.OrphanedFileCount);
        Assert.Equal(0, vm.Scan.RegisteredFileCount);
        Assert.False(vm.ShowActionZone);
        Assert.False(vm.ShowMainAction);
        Assert.False(vm.Cleanup.MoveAllCommand.CanExecute(null));
        Assert.False(vm.Cleanup.DeleteAllCommand.CanExecute(null));
        Assert.False(vm.Chrome.OpenOrphanedDetailsCommand.CanExecute(null));
        Assert.False(vm.Chrome.OpenRegisteredDetailsCommand.CanExecute(null));
    }

    /// <summary>
    /// The message the window gives when the scan after a Move or Delete stopped or
    /// failed. Whether the log can be written is a property of the host, so the probe
    /// decides which of the two forms is expected, as in ScanViewModelStoppedScanTests.
    /// </summary>
    private static string UnfinishedMessageWithTheLogWhereWritten()
    {
        var probe = CrashLog.TryWrite(new InvalidOperationException("probe"));
        return probe.Written
            ? Strings.Body_RescanNotFinished_Why + Environment.NewLine + Environment.NewLine
                + string.Format(Strings.Body_RescanNotFinished_Recorded, probe.Path)
            : Strings.Body_RescanNotFinished_Why;
    }

    private void ScansReturn(ScanResult result) =>
        _scanService.ScanAsync(Arg.Any<IProgress<ScanProgressUpdate>?>(), Arg.Any<CancellationToken>())
            .Returns(result);

    private void ScansThrow(Exception ex) =>
        _scanService.ScanAsync(Arg.Any<IProgress<ScanProgressUpdate>?>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(ex);

    [Fact]
    public async Task A_failed_rescan_takes_the_earlier_list_off_the_window_and_closes_its_commands()
    {
        var vm = CreateViewModel();
        ScansReturn(AnOfferAndAFileLeftAlone());
        await vm.Scan.ScanCommand.ExecuteAsync(null);
        AssertEverythingIsLive(vm);

        ScansThrow(new LocalisedInvalidOperationException(Strings.Error_InstallerDbEmpty));
        await vm.Scan.ScanCommand.ExecuteAsync(null);

        _dialogService.Received(1).ShowError(Arg.Any<string>(), Strings.Error_StoppedTitle);
        AssertNoListAndNothingActsOnOne(vm);
        // The window a failed startup scan opens: the diagnosis and Re-scan.
        Assert.Equal(Strings.Error_StoppedTitle, vm.IntroLead);
        Assert.Equal(vm.Scan.LastScanError, vm.IntroDetail);
        Assert.Equal(string.Empty, vm.IntroNotice);
    }

    [Fact]
    public async Task A_failed_rescan_takes_the_two_warnings_with_the_list()
    {
        // Both lines describe the earlier scan, and both speak of what the list
        // offers: the pending-reboot banner of Move and Delete being paused, the
        // missing-files line of its Details window.
        var vm = CreateViewModel();
        _rebootService.Check().Returns(PendingRebootResult.Block(PendingRebootReason.MsiExecuteMutexHeld));
        ScansReturn(new ScanResult(
            ScanResultWithOrphans(1).RemovableFiles,
            new[] { new RegisteredPackage(@"C:\Windows\Installer\gone.msi", "Contoso Reader", "{aaa}", FileExists: false) },
            0,
            MissingAffectedCount: 1));
        await vm.Scan.ScanCommand.ExecuteAsync(null);
        Assert.True(vm.Scan.HasPendingReboot);
        Assert.True(vm.Scan.HasMissingFromDisk);

        ScansThrow(new LocalisedInvalidOperationException(Strings.Error_InstallerDbEmpty));
        await vm.Scan.ScanCommand.ExecuteAsync(null);

        Assert.False(vm.Scan.HasPendingReboot);
        Assert.Equal(string.Empty, vm.Scan.PendingRebootBannerText);
        Assert.False(vm.Scan.HasMissingFromDisk);
    }

    [Fact]
    public async Task The_Details_buttons_are_told_to_grey_out_once_the_list_has_gone()
    {
        // A button asks CanExecute again only when its command raises
        // CanExecuteChanged, so what it shows is the answer at that moment. The
        // left-alone Details button re-asks when HasScanned changes and reads the
        // result, which raises nothing of its own, so the answer recorded here is
        // the one the button is left showing.
        var vm = CreateViewModel();
        ScansReturn(AnOfferAndAFileLeftAlone());
        await vm.Scan.ScanCommand.ExecuteAsync(null);
        var registeredAnswers = new List<bool>();
        var orphanedAnswers = new List<bool>();
        vm.Chrome.OpenRegisteredDetailsCommand.CanExecuteChanged += (_, _) =>
            registeredAnswers.Add(vm.Chrome.OpenRegisteredDetailsCommand.CanExecute(null));
        vm.Chrome.OpenOrphanedDetailsCommand.CanExecuteChanged += (_, _) =>
            orphanedAnswers.Add(vm.Chrome.OpenOrphanedDetailsCommand.CanExecute(null));

        ScansThrow(new LocalisedInvalidOperationException(Strings.Error_InstallerDbEmpty));
        await vm.Scan.ScanCommand.ExecuteAsync(null);

        Assert.NotEmpty(registeredAnswers);
        Assert.False(registeredAnswers[^1]);
        Assert.NotEmpty(orphanedAnswers);
        Assert.False(orphanedAnswers[^1]);
    }

    [Fact]
    public async Task After_a_failed_rescan_each_command_refuses_when_run_directly()
    {
        var vm = CreateViewModel();
        ScansReturn(AnOfferAndAFileLeftAlone());
        await vm.Scan.ScanCommand.ExecuteAsync(null);
        vm.Cleanup.MoveDestination = Path.Combine(Path.GetTempPath(), "ic-test-move");
        _confirmationService.ConfirmDelete(Arg.Any<int>(), Arg.Any<string>()).Returns(true);
        _confirmationService.ConfirmMove(
            Arg.Any<int>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<bool>()).Returns(true);
        ScansThrow(new LocalisedInvalidOperationException(Strings.Error_InstallerDbEmpty));
        await vm.Scan.ScanCommand.ExecuteAsync(null);

        // The toolkit's Execute and ExecuteAsync do not consult CanExecute, so each
        // of these reaches its command body whatever state the buttons are in, and
        // what refuses is the command itself.
        await vm.Cleanup.DeleteAllCommand.ExecuteAsync(null);
        await vm.Cleanup.MoveAllCommand.ExecuteAsync(null);
        vm.Chrome.OpenOrphanedDetailsCommand.Execute(null);
        vm.Chrome.OpenRegisteredDetailsCommand.Execute(null);

        _confirmationService.DidNotReceive().ConfirmDelete(Arg.Any<int>(), Arg.Any<string>());
        _confirmationService.DidNotReceive().ConfirmMove(
            Arg.Any<int>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<bool>());
        await _reverifier.DidNotReceive().ReverifyAsync(
            Arg.Any<IReadOnlyList<string>>(), Arg.Any<CancellationToken>(), Arg.Any<IProgress<ScanProgressUpdate>?>());
        await _deleteService.DidNotReceive().DeleteFilesAsync(
            Arg.Any<IEnumerable<string>>(), Arg.Any<UnderLeaseClaims>(),
            Arg.Any<IProgress<OperationProgress>?>(), Arg.Any<CancellationToken>());
        await _moveService.DidNotReceive().MoveFilesAsync(
            Arg.Any<IEnumerable<string>>(), Arg.Any<string>(), Arg.Any<UnderLeaseClaims>(),
            Arg.Any<IProgress<OperationProgress>?>(), Arg.Any<CancellationToken>());
        _windowService.DidNotReceive().ShowOrphanedDetails(Arg.Any<OrphanedFilesViewModel>());
        _windowService.DidNotReceive().ShowRegisteredDetails(Arg.Any<RegisteredFilesViewModel>());
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task A_scan_after_a_delete_that_does_not_finish_leaves_the_card_and_no_list_under_it(bool deliberateStop)
    {
        var vm = CreateViewModel();
        ScansReturn(AnOfferAndAFileLeftAlone());
        _deleteService.DeleteFilesAsync(
                Arg.Any<IEnumerable<string>>(), Arg.Any<UnderLeaseClaims>(),
                Arg.Any<IProgress<OperationProgress>?>(), Arg.Any<CancellationToken>())
            .Returns(new DeleteResult(3, Array.Empty<FileOperationError>()));
        _confirmationService.ConfirmDelete(Arg.Any<int>(), Arg.Any<string>()).Returns(true);
        await vm.Scan.ScanWithProgressAsync(null);
        AssertEverythingIsLive(vm);

        Exception ended = deliberateStop
            ? new LocalisedInvalidOperationException(Strings.Error_ScanInstallerFolderListFailed)
            : new InvalidProgramException("nothing anticipated this");
        ScansThrow(ended);

        await vm.Cleanup.DeleteAllCommand.ExecuteAsync(null);

        // The card reports the batch from the batch's own tally.
        Assert.True(vm.Completion.IsComplete);
        Assert.Contains("permanently deleted", vm.Completion.Summary);
        // Under it there is no list: the one from before the batch names the three
        // files just deleted.
        AssertNoListAndNothingActsOnOne(vm);
        Assert.Equal(Strings.Body_RescanNotFinished_Lead, vm.IntroLead);
        Assert.Equal(vm.Scan.UnfinishedRefreshMessage, vm.IntroDetail);
        Assert.Equal(UnfinishedMessageWithTheLogWhereWritten(), vm.IntroDetail);
        // Not the stop's own account, which says nothing was removed.
        Assert.DoesNotContain(ended.Message, vm.IntroDetail, StringComparison.Ordinal);
        Assert.Equal(string.Empty, vm.IntroNotice);
        Assert.False(vm.Scan.HasScanError);
        _dialogService.DidNotReceive().ShowError(Arg.Any<string>(), Arg.Any<string>());
        _dialogService.DidNotReceive().ShowWarning(Arg.Any<string>(), Arg.Any<string>());

        // Done takes the card away and leaves the same window under it.
        vm.Completion.DismissCommand.Execute(null);
        Assert.Equal(Strings.Body_RescanNotFinished_Lead, vm.IntroLead);
        AssertNoListAndNothingActsOnOne(vm);
    }

    [Fact]
    public async Task A_scan_after_a_move_that_stops_leaves_the_card_and_no_list_under_it()
    {
        var vm = CreateViewModel();
        ScansReturn(AnOfferAndAFileLeftAlone());
        _moveService.MoveFilesAsync(
                Arg.Any<IEnumerable<string>>(), Arg.Any<string>(), Arg.Any<UnderLeaseClaims>(),
                Arg.Any<IProgress<OperationProgress>?>(), Arg.Any<CancellationToken>())
            .Returns(new MoveResult(3, Array.Empty<FileOperationError>()));
        _confirmationService.ConfirmMove(
            Arg.Any<int>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<bool>()).Returns(true);
        await vm.Scan.ScanWithProgressAsync(null);
        vm.Cleanup.MoveDestination = Path.Combine(Path.GetTempPath(), "ic-test-move");
        ScansThrow(new LocalisedInvalidOperationException(Strings.Error_ScanInstallerFolderListFailed));

        await vm.Cleanup.MoveAllCommand.ExecuteAsync(null);

        await _moveService.Received(1).MoveFilesAsync(
            Arg.Any<IEnumerable<string>>(), Arg.Any<string>(), Arg.Any<UnderLeaseClaims>(),
            Arg.Any<IProgress<OperationProgress>?>(), Arg.Any<CancellationToken>());
        Assert.True(vm.Completion.IsComplete);
        AssertNoListAndNothingActsOnOne(vm);
        Assert.Equal(Strings.Body_RescanNotFinished_Lead, vm.IntroLead);
        Assert.Equal(UnfinishedMessageWithTheLogWhereWritten(), vm.IntroDetail);
    }

    [Fact]
    public async Task A_scan_after_a_delete_that_is_cancelled_leaves_the_card_and_no_list_under_it()
    {
        var vm = CreateViewModel();
        ScansReturn(AnOfferAndAFileLeftAlone());
        _deleteService.DeleteFilesAsync(
                Arg.Any<IEnumerable<string>>(), Arg.Any<UnderLeaseClaims>(),
                Arg.Any<IProgress<OperationProgress>?>(), Arg.Any<CancellationToken>())
            .Returns(_ =>
            {
                vm.Cleanup.CancelOperationCommand.Execute(null);
                return new DeleteResult(1, Array.Empty<FileOperationError>(), Cancelled: true);
            });
        _confirmationService.ConfirmDelete(Arg.Any<int>(), Arg.Any<string>()).Returns(true);
        await vm.Scan.ScanWithProgressAsync(null);

        // The second Cancel, pressed while the overlay reads "Scanning...".
        _scanService.ScanAsync(Arg.Any<IProgress<ScanProgressUpdate>?>(), Arg.Any<CancellationToken>())
            .Returns(ci =>
            {
                vm.Cleanup.CancelOperationCommand.Execute(null);
                ci.Arg<CancellationToken>().ThrowIfCancellationRequested();
                return ScanResultWithOrphans(2);
            });
        var logBefore = CrashLogText();

        await vm.Cleanup.DeleteAllCommand.ExecuteAsync(null);

        // One file went before the first Cancel, so the card is up.
        Assert.True(vm.Completion.IsComplete);
        AssertNoListAndNothingActsOnOne(vm);
        Assert.True(vm.Scan.LastScanWasCancelled);
        Assert.Equal(Strings.Body_RescanNotFinished_Lead, vm.IntroLead);
        // THE CRASH-LOG SENTENCE NEVER FOLLOWS A CANCEL, and nothing is written for
        // it to name: the user asked for this ending. "Scan cancelled." is the whole
        // of what follows the message.
        Assert.Equal(Strings.Body_RescanNotFinished_Why, vm.IntroDetail);
        Assert.DoesNotContain(CrashLog.LogPath, vm.IntroDetail, StringComparison.OrdinalIgnoreCase);
        Assert.Equal(logBefore, CrashLogText());
        Assert.Equal(Strings.Status_ScanCancelled, vm.IntroNotice);
    }

    /// <summary>
    /// The crash log as it stands, or empty where there is none or it will not
    /// read. Compared before and after a run to show the run wrote nothing.
    /// </summary>
    private static string CrashLogText()
    {
        try { return File.Exists(CrashLog.LogPath) ? File.ReadAllText(CrashLog.LogPath) : string.Empty; }
        catch (IOException) { return string.Empty; }
    }

    [Fact]
    public async Task A_cancelled_rescan_keeps_the_earlier_list_and_its_commands()
    {
        // The control for the three above: the one ending that keeps an earlier
        // result, because that scan completed and nothing has acted on it since.
        var vm = CreateViewModel();
        ScansReturn(AnOfferAndAFileLeftAlone());
        await vm.Scan.ScanCommand.ExecuteAsync(null);
        _scanService.ScanAsync(Arg.Any<IProgress<ScanProgressUpdate>?>(), Arg.Any<CancellationToken>())
            .Returns(ci =>
            {
                vm.Scan.CancelScanCommand.Execute(null);
                ci.Arg<CancellationToken>().ThrowIfCancellationRequested();
                return ScanResultWithOrphans(1);
            });

        await vm.Scan.ScanCommand.ExecuteAsync(null);

        Assert.True(vm.Scan.LastScanWasCancelled);
        Assert.NotNull(vm.Scan.LastScanResult);
        AssertEverythingIsLive(vm);
        Assert.Equal(Strings.Body_MainExplanation_Lead, vm.IntroLead);
        Assert.Equal(string.Empty, vm.IntroNotice);
    }

    [Fact]
    public async Task A_cancelled_rescan_after_a_failed_one_leaves_the_failure_on_screen()
    {
        var vm = CreateViewModel();
        ScansReturn(AnOfferAndAFileLeftAlone());
        await vm.Scan.ScanCommand.ExecuteAsync(null);
        ScansThrow(new LocalisedInvalidOperationException(Strings.Error_InstallerDbEmpty));
        await vm.Scan.ScanCommand.ExecuteAsync(null);
        var diagnosis = vm.Scan.LastScanError;
        _scanService.ScanAsync(Arg.Any<IProgress<ScanProgressUpdate>?>(), Arg.Any<CancellationToken>())
            .Returns(ci =>
            {
                vm.Scan.CancelScanCommand.Execute(null);
                ci.Arg<CancellationToken>().ThrowIfCancellationRequested();
                return ScanResultWithOrphans(1);
            });

        await vm.Scan.ScanCommand.ExecuteAsync(null);

        AssertNoListAndNothingActsOnOne(vm);
        Assert.Equal(Strings.Error_StoppedTitle, vm.IntroLead);
        Assert.Equal(diagnosis, vm.IntroDetail);
        Assert.Equal(Strings.Status_ScanCancelled, vm.IntroNotice);
    }

    [Fact]
    public async Task A_rescan_that_completes_after_an_unfinished_one_puts_its_own_list_on_screen()
    {
        var vm = CreateViewModel();
        ScansReturn(AnOfferAndAFileLeftAlone());
        _deleteService.DeleteFilesAsync(
                Arg.Any<IEnumerable<string>>(), Arg.Any<UnderLeaseClaims>(),
                Arg.Any<IProgress<OperationProgress>?>(), Arg.Any<CancellationToken>())
            .Returns(new DeleteResult(1, Array.Empty<FileOperationError>()));
        _confirmationService.ConfirmDelete(Arg.Any<int>(), Arg.Any<string>()).Returns(true);
        await vm.Scan.ScanWithProgressAsync(null);
        ScansThrow(new LocalisedInvalidOperationException(Strings.Error_ScanInstallerFolderListFailed));
        await vm.Cleanup.DeleteAllCommand.ExecuteAsync(null);
        vm.Completion.DismissCommand.Execute(null);
        Assert.True(vm.Scan.HasUnfinishedRefresh);
        // The window reads the lead when the view model says it changed, so the
        // lead as it stood at the last such raise is the one on screen.
        var leadsRaised = new List<string>();
        vm.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName == nameof(MainViewModel.IntroLead)) leadsRaised.Add(vm.IntroLead);
        };

        ScansReturn(AnOfferAndAFileLeftAlone());
        await vm.Scan.ScanCommand.ExecuteAsync(null);

        Assert.False(vm.Scan.HasUnfinishedRefresh);
        AssertEverythingIsLive(vm);
        Assert.Equal(Strings.Body_MainExplanation_Lead, vm.IntroLead);
        Assert.NotEmpty(leadsRaised);
        Assert.Equal(Strings.Body_MainExplanation_Lead, leadsRaised[^1]);
        Assert.Equal(string.Empty, vm.IntroNotice);
    }

    [Fact]
    public async Task A_rescan_that_fails_after_an_unfinished_one_shows_its_own_failure()
    {
        // The window's intro reads the unfinished message ahead of a failure, so a
        // failed Re-scan has to clear it or the window keeps saying the scan after
        // the Delete did not finish, over a failure it is not describing.
        var vm = CreateViewModel();
        ScansReturn(AnOfferAndAFileLeftAlone());
        _deleteService.DeleteFilesAsync(
                Arg.Any<IEnumerable<string>>(), Arg.Any<UnderLeaseClaims>(),
                Arg.Any<IProgress<OperationProgress>?>(), Arg.Any<CancellationToken>())
            .Returns(new DeleteResult(1, Array.Empty<FileOperationError>()));
        _confirmationService.ConfirmDelete(Arg.Any<int>(), Arg.Any<string>()).Returns(true);
        await vm.Scan.ScanWithProgressAsync(null);
        ScansThrow(new LocalisedInvalidOperationException(Strings.Error_ScanInstallerFolderListFailed));
        await vm.Cleanup.DeleteAllCommand.ExecuteAsync(null);
        vm.Completion.DismissCommand.Execute(null);
        Assert.True(vm.Scan.HasUnfinishedRefresh);

        ScansThrow(new LocalisedInvalidOperationException(Strings.Error_InstallerDbEmpty));
        await vm.Scan.ScanCommand.ExecuteAsync(null);

        _dialogService.Received(1).ShowError(Arg.Any<string>(), Strings.Error_StoppedTitle);
        Assert.False(vm.Scan.HasUnfinishedRefresh);
        Assert.True(vm.Scan.HasScanError);
        AssertNoListAndNothingActsOnOne(vm);
        Assert.Equal(Strings.Error_StoppedTitle, vm.IntroLead);
        Assert.Equal(vm.Scan.LastScanError, vm.IntroDetail);
        Assert.StartsWith(Strings.Error_InstallerDbEmpty, vm.IntroDetail, StringComparison.Ordinal);
        Assert.Equal(string.Empty, vm.IntroNotice);
    }

    [Fact]
    public async Task A_rescan_that_stops_after_a_refused_startup_scan_raises_the_stops_heading()
    {
        // Both scans leave the window in the failed state, so nothing about the list
        // changes between them, and the window moves off "Access denied" only on a
        // raise that comes after the new heading is set. The window reads the lead
        // when the view model says it changed, so the lead as it stood at the last
        // such raise is the one on screen.
        var vm = CreateViewModel();
        ScansThrow(new UnauthorizedAccessException("denied"));
        await vm.Scan.ScanWithProgressAsync(null);
        Assert.Equal(Strings.Error_AdminRequiredTitle, vm.IntroLead);
        var leadsRaised = new List<string>();
        vm.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName == nameof(MainViewModel.IntroLead)) leadsRaised.Add(vm.IntroLead);
        };

        ScansThrow(new LocalisedInvalidOperationException(Strings.Error_InstallerDbEmpty));
        await vm.Scan.ScanCommand.ExecuteAsync(null);

        Assert.Equal(Strings.Error_StoppedTitle, vm.IntroLead);
        Assert.NotEmpty(leadsRaised);
        Assert.Equal(Strings.Error_StoppedTitle, leadsRaised[^1]);
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
    public void MainExplanationWhyText_renders_with_no_placeholder_markers_left()
    {
        // Three arguments go into a two-slot sentence, and MainExplanationWhyText's own
        // doc carries why the third is passed. What this asserts is the outcome either
        // way: no marker survives to the screen.
        var vm = CreateViewModel();

        Assert.DoesNotContain("{0}", vm.MainExplanationWhyText);
        Assert.DoesNotContain("{1}", vm.MainExplanationWhyText);
        Assert.DoesNotContain("{2}", vm.MainExplanationWhyText);
    }

    [Fact]
    public void MainExplanationWhyText_claims_only_what_the_scan_established()
    {
        // The sentence is about the ANSWER the app got, not about the state of the
        // world, and the difference is load-bearing rather than a hedge. Saying
        // outright that Windows has no record of these files would be an assertion
        // the app cannot make. What it did establish is that it asked.
        //
        // DO NOT PIN A PHRASE. The sentence must not name one cause for a list that
        // has two, so the wording is free to move and an assertion on it would go
        // stale without failing for anything real. What follows guards the property
        // against the wording that ships and against a rewrite of it.
        //
        // THE THREE THINGS IT HOLDS. The opening clause carries the epistemics for
        // both halves, so the sentence has to say that the app ASKED. The two
        // causes the offer can contain are both named, so a rewrite cannot collapse
        // back to one cause for a set that has two. And the third label must not
        // appear: an obsoleted registration is counted at scan time and never
        // offered, so naming it here would describe a file this list cannot hold.
        //
        // WHAT IT WOULD LET THROUGH, SAID PLAINLY BECAUSE A PHRASE MATCH IS WHAT
        // FAILED HERE. It is a frame check and not a truth check. A sentence that
        // said the app asked, named both causes, avoided that one phrase and then
        // asserted something false about the machine in different words would pass
        // it. Nothing in a test can settle whether a sentence is true; that is a
        // copy review, and the resx comment beside this string is where the standard
        // for it lives.
        var text = CreateViewModel().MainExplanationWhyText;

        Assert.Contains("ask", text, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("Windows has no record", text, StringComparison.OrdinalIgnoreCase);

        // Through the resx labels rather than through English, so the partition is
        // pinned in whatever language the run renders and a label edit moves the
        // test with the copy.
        Assert.Contains(Strings.Reason_Orphaned, text, StringComparison.Ordinal);
        Assert.Contains(Strings.Reason_Superseded, text, StringComparison.Ordinal);
        Assert.DoesNotContain(Strings.Reason_Obsoleted, text, StringComparison.Ordinal);
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

    // ---- The card saying a newer version is available ----

    /// <summary>
    /// The startup as App runs it: the scan held, the check started beside the scan,
    /// and the decision taken once both are done.
    /// </summary>
    private static async Task StartAsync(MainViewModel vm, bool mayOfferUpdate = true)
    {
        vm.HoldResultsForUpdateCheck();
        vm.Chrome.StartAutomaticUpdateCheck();
        await vm.Scan.ScanWithProgressAsync(null);
        await vm.Chrome.AutomaticCheck;
        vm.ShowStartupScan(mayOfferUpdate);
    }

    private void CheckAnswers(UpdateCheckResult result) =>
        _updateCheckService.CheckAsync(Arg.Any<UpdateCheckOrigin>(), Arg.Any<CancellationToken>())
            .Returns(result);

    private void ScanFinds(ScanResult result) =>
        _scanService.ScanAsync(Arg.Any<IProgress<ScanProgressUpdate>?>(), Arg.Any<CancellationToken>())
            .Returns(result);

    /// <summary>
    /// The report's write lands. One that fails takes the box off the card once the card
    /// is up, after <see cref="MainViewModel.ScanCardDecided"/> has completed.
    /// </summary>
    private void ReportWriteLands() =>
        _resultLogService.WriteAsync(Arg.Any<ResultLogEntry>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(true));

    [Fact]
    public async Task A_newer_version_puts_the_card_up_and_holds_the_scan_s_card_and_its_report()
    {
        var vm = CreateViewModel();
        ScanFinds(EmptyScanResult());
        CheckAnswers(new UpdateAvailable("2.1.0", "9.9.9"));

        await StartAsync(vm);

        Assert.True(vm.Chrome.IsOfferingUpdate);
        Assert.False(vm.Completion.IsComplete);
        Assert.False(vm.IsMainContentInteractive);
        Assert.False(vm.Scan.ScanCommand.CanExecute(null));
        // Nothing is taken for the report while the card is up, so an update leaves the
        // PC's first run to the newer version.
        await _earlierRunCheck.DidNotReceive().ShowsAnEarlierRunWithinAsync(Arg.Any<TimeSpan>());
        _firstRunMark.DidNotReceive().Set();
        Assert.False(vm.ScanCardDecided.IsCompleted);
    }

    [Fact]
    public async Task Continue_without_updating_shows_the_scan_s_card_and_takes_the_report()
    {
        var vm = CreateViewModel();
        ScanFinds(EmptyScanResult());
        ReportWriteLands();
        CheckAnswers(new UpdateAvailable("2.1.0", "9.9.9"));
        await StartAsync(vm);

        vm.Chrome.ContinueWithoutUpdatingCommand.Execute(null);
        await vm.ScanCardDecided;

        Assert.False(vm.Chrome.IsOfferingUpdate);
        Assert.True(vm.Completion.IsComplete);
        Assert.Equal(Strings.Completion_AllClean, vm.Completion.Heading);
        Assert.True(vm.Completion.OffersReport);
        _firstRunMark.Received(1).TryClaim();
        // The notice at the top stays for the session.
        Assert.True(vm.Chrome.HasNewerVersion);
    }

    [Fact]
    public async Task Esc_on_the_card_is_Continue_without_updating()
    {
        var vm = CreateViewModel();
        ScanFinds(ScanResultWithOrphans(2));
        CheckAnswers(new UpdateAvailable("2.1.0", "9.9.9"));
        await StartAsync(vm);

        Assert.True(vm.HandleEscape());

        Assert.False(vm.Chrome.IsOfferingUpdate);
        Assert.True(vm.IsMainContentInteractive);
        Assert.True(vm.Scan.ScanCommand.CanExecute(null));
    }

    [Theory]
    [MemberData(nameof(AnswersThatPutNoCardUp))]
    public async Task No_newer_version_shows_the_scan_as_soon_as_the_check_has_answered(UpdateCheckResult answer)
    {
        var vm = CreateViewModel();
        ScanFinds(EmptyScanResult());
        ReportWriteLands();
        CheckAnswers(answer);

        await StartAsync(vm);
        await vm.ScanCardDecided;

        Assert.False(vm.Chrome.IsOfferingUpdate);
        Assert.True(vm.Completion.IsComplete);
        Assert.True(vm.Completion.OffersReport);
    }

    public static TheoryData<UpdateCheckResult> AnswersThatPutNoCardUp() =>
    [
        new UpToDate("2.1.0"),
        new CheckFailed(UpdateCheckFailureReason.NetworkUnavailable),
        new CheckFailed(UpdateCheckFailureReason.Timeout),
    ];

    [Fact]
    public async Task The_check_switched_off_shows_the_scan_with_no_card()
    {
        var vm = CreateViewModel(new AppSettings { AutoUpdateCheck = false });
        ScanFinds(EmptyScanResult());
        CheckAnswers(new UpdateAvailable("2.1.0", "9.9.9"));

        await StartAsync(vm);
        await vm.ScanCardDecided;

        Assert.False(vm.Chrome.IsOfferingUpdate);
        Assert.True(vm.Completion.IsComplete);
    }

    [Fact]
    public async Task Where_the_card_may_not_be_offered_a_newer_version_shows_the_notice_and_the_scan()
    {
        var vm = CreateViewModel();
        ScanFinds(EmptyScanResult());
        CheckAnswers(new UpdateAvailable("2.1.0", "9.9.9"));

        await StartAsync(vm, mayOfferUpdate: false);
        await vm.ScanCardDecided;

        Assert.False(vm.Chrome.IsOfferingUpdate);
        Assert.True(vm.Chrome.HasNewerVersion);
        Assert.True(vm.Completion.IsComplete);
    }

    [Fact]
    public async Task A_failed_startup_scan_is_held_behind_the_card_like_a_result()
    {
        var vm = CreateViewModel();
        _scanService.ScanAsync(Arg.Any<IProgress<ScanProgressUpdate>?>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromException<ScanResult>(new InvalidOperationException("failed")));
        CheckAnswers(new UpdateAvailable("2.1.0", "9.9.9"));

        await StartAsync(vm);

        Assert.True(vm.Scan.HasScanError);
        Assert.True(vm.Chrome.IsOfferingUpdate);
        Assert.False(vm.IsMainContentInteractive);
    }

    [Fact]
    public async Task Update_now_from_the_card_takes_no_report_and_hands_over()
    {
        var updater = Substitute.For<IAppUpdater>();
        updater.Kind.Returns(AppCopyKind.Portable);
        var prepared = new PreparedUpdate("9.9.9", @"C:\ProgramData\InstallerClean-update-x\InstallerClean-9.9.9-setup.exe",
            "/SILENT", () => { });
        updater.PrepareAsync(default!, default, default).ReturnsForAnyArgs(prepared);
        _windowService.StartUpdateAndCloseAsync(default!).ReturnsForAnyArgs(true);
        var vm = CreateViewModel(new AppSettings(), updater);
        ScanFinds(EmptyScanResult());
        CheckAnswers(new UpdateAvailable("2.1.0", "9.9.9"));
        await StartAsync(vm);

        await vm.Chrome.UpdateNowCommand.ExecuteAsync(null);

        await _windowService.Received(1).StartUpdateAndCloseAsync(prepared);
        Assert.False(vm.ResultsMayShow);
        Assert.False(vm.Completion.IsComplete);
        _firstRunMark.DidNotReceive().Set();
        await _resultLogService.DidNotReceiveWithAnyArgs().WriteAsync(default!, default);
    }

    [Fact]
    public async Task Move_and_Delete_stay_off_while_another_process_holds_the_mutex_an_update_let_go_of()
    {
        var vm = CreateViewModel();
        _scanService.ScanAsync(Arg.Any<IProgress<ScanProgressUpdate>?>(), Arg.Any<CancellationToken>())
            .Returns(ScanResultWithOrphans(2));
        await vm.Scan.ScanWithProgressAsync(null);
        Assert.True(vm.Cleanup.MoveAllCommand.CanExecute(null));

        _windowService.IsInstanceLockLost().Returns(true);
        Assert.False(vm.Cleanup.MoveAllCommand.CanExecute(null));
        Assert.False(vm.Cleanup.DeleteAllCommand.CanExecute(null));

        _windowService.IsInstanceLockLost().Returns(false);
        Assert.True(vm.Cleanup.MoveAllCommand.CanExecute(null));
        Assert.True(vm.Cleanup.DeleteAllCommand.CanExecute(null));
    }

    [Fact]
    public async Task An_update_that_does_not_hand_over_has_Move_and_Delete_ask_again_whether_they_can_run()
    {
        var updater = Substitute.For<IAppUpdater>();
        updater.Kind.Returns(AppCopyKind.Portable);
        updater.PrepareAsync(default!, default, default).ReturnsForAnyArgs(
            new PreparedUpdate("9.9.9", @"C:\ProgramData\InstallerClean-update-x\InstallerClean.exe", string.Empty,
                () => { }, @"C:\Users\x\InstallerClean.exe"));
        _windowService.StartUpdateAndCloseAsync(default!).ReturnsForAnyArgs(false);
        var vm = CreateViewModel(new AppSettings(), updater);
        ScanFinds(EmptyScanResult());
        CheckAnswers(new UpdateAvailable("2.1.0", "9.9.9"));
        await StartAsync(vm);
        var moveAsked = 0;
        var deleteAsked = 0;
        vm.Cleanup.MoveAllCommand.CanExecuteChanged += (_, _) => moveAsked++;
        vm.Cleanup.DeleteAllCommand.CanExecuteChanged += (_, _) => deleteAsked++;

        await vm.Chrome.UpdateNowCommand.ExecuteAsync(null);

        Assert.False(vm.Chrome.IsUpdating);
        Assert.True(moveAsked > 0);
        Assert.True(deleteAsked > 0);
    }

    [Fact]
    public async Task A_rescan_after_Continue_does_not_bring_the_card_back()
    {
        var vm = CreateViewModel();
        ScanFinds(ScanResultWithOrphans(2));
        CheckAnswers(new UpdateAvailable("2.1.0", "9.9.9"));
        await StartAsync(vm);
        vm.Chrome.ContinueWithoutUpdatingCommand.Execute(null);

        await vm.Scan.ScanCommand.ExecuteAsync(null);

        Assert.False(vm.Chrome.IsOfferingUpdate);
        Assert.True(vm.Scan.HasScanned);
        Assert.True(vm.Chrome.HasNewerVersion);
    }
}
