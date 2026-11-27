using InstallerClean.Helpers;
using InstallerClean.Models;
using InstallerClean.Resources;
using InstallerClean.Services;
using InstallerClean.ViewModels;
using NSubstitute;

namespace InstallerClean.Tests.ViewModels;

/// <summary>
/// The window after a scan, where Windows Installer's in-progress signals were read twice:
/// by the scan just after its clock, and by the window when the scan finished. The line
/// under the left-alone count follows the first and the banner, with Move and Delete, the
/// second, so each says what was true when it was read. Each scan is a Re-scan started on a
/// stand-in for the window's dispatcher.
/// </summary>
public class ScanViewModelScanSignalTests
{
    private static readonly PendingRebootResult Busy =
        PendingRebootResult.Block(PendingRebootReason.MsiExecuteMutexHeld);

    private static readonly PendingRebootResult Restart =
        PendingRebootResult.Block(PendingRebootReason.PendingRenameInCache);

    private readonly IFileSystemScanService _scanService = Substitute.For<IFileSystemScanService>();
    private readonly IPendingRebootService _rebootService = Substitute.For<IPendingRebootService>();
    private readonly DispatcherStandIn _dispatcher = new();

    private ScanViewModel NewViewModel() =>
        new(_scanService, _rebootService, Substitute.For<IDialogService>());

    /// <summary>
    /// A scan offering one superseded patch and holding two files from the walk, with
    /// <paramref name="atScan"/> as what it read just after its clock and the files counted
    /// under the arm that reading puts them in.
    /// </summary>
    private void ScanAnswers(PendingRebootResult atScan) =>
        _scanService.ScanAsync(Arg.Any<IProgress<ScanProgressUpdate>?>(), Arg.Any<CancellationToken>())
            .Returns(new ScanResult(
                [new OrphanedFile(@"C:\Windows\Installer\superseded.msp", 4096, false, false, true, "superseded")],
                [], 0,
                WithheldFiles:
                [
                    new OrphanedFile(@"C:\Windows\Installer\a.msi", 1024, false, false, false, "unclaimed"),
                    new OrphanedFile(@"C:\Windows\Installer\b.msi", 1024, false, false, false, "unclaimed"),
                ],
                WithheldBy: atScan.IsBlocked
                    ? new WithholdingSplit(ScanSignalCount: 2)
                    : new WithholdingSplit(DeclaredProductUnestablishedCount: 2),
                InstallerActivityAtScan: atScan));

    private async Task RescanAsync(ScanViewModel vm)
    {
        var scan = Task.CompletedTask;
        await _dispatcher.RunAsync(() => scan = vm.ScanCommand.ExecuteAsync(null));
        await scan;
        Assert.Empty(_dispatcher.Thrown);
    }

    [Fact]
    public async Task Clear_at_both_reads_shows_neither_the_line_nor_the_banner()
    {
        ScanAnswers(PendingRebootResult.Clean);
        _rebootService.Check().Returns(PendingRebootResult.Clean);
        var vm = NewViewModel();

        await RescanAsync(vm);

        Assert.Equal(string.Empty, vm.ScanSignalText);
        Assert.False(vm.HasScanSignal);
        Assert.False(vm.HasPendingReboot);
    }

    [Fact]
    public async Task Set_at_the_scan_s_read_and_clear_after_shows_the_line_and_leaves_Move_and_Delete_alone()
    {
        // An install that ended while the scan ran. The banner would say something is
        // using Windows Installer now, which is no longer true.
        ScanAnswers(Busy);
        _rebootService.Check().Returns(PendingRebootResult.Clean);
        var vm = NewViewModel();

        await RescanAsync(vm);

        Assert.Equal(Strings.Summary_ScanSignal_Busy, vm.ScanSignalText);
        Assert.True(vm.HasScanSignal);
        Assert.False(vm.HasPendingReboot);
        Assert.Same(PendingRebootResult.Clean, vm.PendingRebootResult);
        Assert.True(vm.HasOrphans);
    }

    [Fact]
    public async Task Clear_at_the_scan_s_read_and_set_after_shows_the_banner_alone()
    {
        ScanAnswers(PendingRebootResult.Clean);
        _rebootService.Check().Returns(Busy);
        var vm = NewViewModel();

        await RescanAsync(vm);

        Assert.False(vm.HasScanSignal);
        Assert.True(vm.HasPendingReboot);
        Assert.Equal(Strings.Body_PendingReboot_MsiExecuteMutex, vm.PendingRebootBannerText);
    }

    [Fact]
    public async Task Set_at_both_reads_shows_each_with_its_own_reason()
    {
        ScanAnswers(Busy);
        _rebootService.Check().Returns(Restart);
        var vm = NewViewModel();

        await RescanAsync(vm);

        Assert.Equal(Strings.Summary_ScanSignal_Busy, vm.ScanSignalText);
        Assert.True(vm.HasPendingReboot);
        Assert.Equal(Strings.Body_PendingReboot_PendingRenameInCache, vm.PendingRebootBannerText);
    }

    [Fact]
    public async Task The_clock_going_back_during_the_scan_shows_its_line_and_leaves_Move_and_Delete_alone()
    {
        _scanService.ScanAsync(Arg.Any<IProgress<ScanProgressUpdate>?>(), Arg.Any<CancellationToken>())
            .Returns(new ScanResult(
                [new OrphanedFile(@"C:\Windows\Installer\superseded.msp", 4096, false, false, true, "superseded")],
                [], 0,
                WithheldFiles:
                [
                    new OrphanedFile(@"C:\Windows\Installer\a.msi", 1024, false, false, false, "unclaimed"),
                    new OrphanedFile(@"C:\Windows\Installer\b.msi", 1024, false, false, false, "unclaimed"),
                ],
                WithheldBy: new WithholdingSplit(ClockSetBackCount: 2),
                InstallerActivityAtScan: PendingRebootResult.Clean));
        _rebootService.Check().Returns(PendingRebootResult.Clean);
        var vm = NewViewModel();

        await RescanAsync(vm);

        Assert.Equal(Strings.Summary_ScanSignal_ClockSetBack, vm.ScanSignalText);
        Assert.True(vm.HasScanSignal);
        Assert.False(vm.HasPendingReboot);
    }

    [Fact]
    public async Task A_signal_set_with_no_file_held_for_it_gives_no_line()
    {
        _scanService.ScanAsync(Arg.Any<IProgress<ScanProgressUpdate>?>(), Arg.Any<CancellationToken>())
            .Returns(new ScanResult([], [], 0, InstallerActivityAtScan: Busy));
        _rebootService.Check().Returns(PendingRebootResult.Clean);
        var vm = NewViewModel();

        await RescanAsync(vm);

        Assert.False(vm.HasScanSignal);
    }

    [Fact]
    public void A_lock_whose_check_failed_on_a_Windows_error_is_given_with_it_on_both_hosts()
    {
        var withError = PendingRebootResult.Block(PendingRebootReason.MsiExecuteMutexUnreadable, windowsError: 8);
        var withNone = PendingRebootResult.Block(PendingRebootReason.MsiExecuteMutexUnreadable);

        Assert.Equal(
            string.Format(Strings.Summary_ScanSignal_LockUnreadableWindowsError, WindowsErrors.MessageFor(8), "8"),
            ScanSignalReport.WindowLine(withError));
        Assert.Equal(
            string.Format(Strings.Cli_ScanSignal_LockUnreadableWindowsError, WindowsErrors.MessageFor(8), "8"),
            ScanSignalReport.CommandLine(withError));
        Assert.Equal(Strings.Summary_ScanSignal_LockUnreadable, ScanSignalReport.WindowLine(withNone));
        Assert.Equal(Strings.Cli_ScanSignal_LockUnreadable, ScanSignalReport.CommandLine(withNone));
    }

    [Fact]
    public void Every_reason_the_banners_tell_apart_has_a_line_of_its_own()
    {
        // The two refusals share a banner, and the in-progress file takes the banner true
        // of the whole family, so each shares the line its banner's twin has. So do the
        // two reads that could not be made. The scan does not read the Windows Installer
        // service, so a running one is never the signal at a scan's start and takes the
        // family's line, as the in-progress file does.
        var shared = new Dictionary<PendingRebootReason, PendingRebootReason>
        {
            [PendingRebootReason.InstallerInProgressMarkerAccessRefused] = PendingRebootReason.MsiExecuteMutexAccessRefused,
            [PendingRebootReason.InstallerServiceUnreadable] = PendingRebootReason.MsiExecuteMutexUnreadable,
            [PendingRebootReason.InstallerServiceRunning] = PendingRebootReason.InstallerInProgressMarker,
        };
        var seen = new List<string>();
        foreach (var reason in Enum.GetValues<PendingRebootReason>())
        {
            if (shared.TryGetValue(reason, out var twin))
            {
                Assert.Equal(ScanSignalReport.WindowLine(twin), ScanSignalReport.WindowLine(reason));
                continue;
            }
            seen.Add(ScanSignalReport.WindowLine(reason));
        }

        Assert.Equal(seen.Count, seen.Distinct().Count());
        Assert.Contains(Strings.Summary_ScanSignal_Other, seen);
    }

    [Fact]
    public void Every_reason_has_a_window_line_and_a_command_line_line()
    {
        foreach (var reason in Enum.GetValues<PendingRebootReason>())
        {
            Assert.False(string.IsNullOrWhiteSpace(ScanSignalReport.WindowLine(reason)), $"{reason}");
            Assert.False(string.IsNullOrWhiteSpace(ScanSignalReport.CommandLine(reason)), $"{reason}");
        }

        // A reason added later takes the sentence true of the whole family.
        Assert.Equal(Strings.Summary_ScanSignal_Other, ScanSignalReport.WindowLine((PendingRebootReason)99));
        Assert.Equal(Strings.Cli_ScanSignal_Other, ScanSignalReport.CommandLine((PendingRebootReason)99));
    }
}
