using InstallerClean.Helpers;
using InstallerClean.Models;
using InstallerClean.Resources;
using InstallerClean.Services;
using InstallerClean.ViewModels;
using NSubstitute;

namespace InstallerClean.Tests.ViewModels;

/// <summary>
/// The window where a scan put back files an earlier Move or Delete had left in the app's
/// own folder: the line under the left-alone count, the message of a scan that failed
/// after it, and a cancelled scan's files named with the next result. The scan reports
/// what it put back through its progress, before anything else, and each scan here is a
/// Re-scan started on a stand-in for the window's dispatcher.
/// </summary>
public class ScanViewModelPutBackTests
{
    private const string A = @"C:\Windows\Installer\a.msi";
    private const string B = @"C:\Windows\Installer\b.msp";

    private readonly IFileSystemScanService _scanService = Substitute.For<IFileSystemScanService>();
    private readonly IPendingRebootService _rebootService = Substitute.For<IPendingRebootService>();
    private readonly IDialogService _dialogs = Substitute.For<IDialogService>();
    private readonly DispatcherStandIn _dispatcher = new();

    public ScanViewModelPutBackTests() => _rebootService.Check().Returns(PendingRebootResult.Clean);

    private ScanViewModel NewViewModel() => new(_scanService, _rebootService, _dialogs);

    private static ScanResult Result() => new(
        [new OrphanedFile(A, 4096, false, false, false, "unclaimed")], [], 0);

    /// <summary>The next scan reports <paramref name="putBack"/> as put back, where any is given, and then ends as <paramref name="ending"/> says.</summary>
    private void ScanPutsBack(string[] putBack, Func<ScanResult> ending) =>
        _scanService.ScanAsync(Arg.Any<IProgress<ScanProgressUpdate>?>(), Arg.Any<CancellationToken>())
            .Returns(call =>
            {
                if (putBack.Length > 0)
                    call.Arg<IProgress<ScanProgressUpdate>?>()!.Report(
                        ScanProgressUpdate.ForPutBack(new InHandRecoveryResult(putBack)));
                return Task.FromResult(ending());
            });

    /// <summary>The next scan reports <paramref name="recovery"/> and then returns a result.</summary>
    private void ScanReports(InHandRecoveryResult recovery) =>
        _scanService.ScanAsync(Arg.Any<IProgress<ScanProgressUpdate>?>(), Arg.Any<CancellationToken>())
            .Returns(call =>
            {
                call.Arg<IProgress<ScanProgressUpdate>?>()!.Report(ScanProgressUpdate.ForPutBack(recovery));
                return Task.FromResult(Result());
            });

    private async Task RescanAsync(ScanViewModel vm)
    {
        var scan = Task.CompletedTask;
        await _dispatcher.RunAsync(() => scan = vm.ScanCommand.ExecuteAsync(null));
        await scan;
        Assert.Empty(_dispatcher.Thrown);
    }

    [Fact]
    public async Task A_scan_that_put_nothing_back_shows_no_line()
    {
        ScanPutsBack([], Result);
        var vm = NewViewModel();

        await RescanAsync(vm);

        Assert.Equal(string.Empty, vm.PutBackText);
        Assert.False(vm.HasPutBack);
    }

    [Fact]
    public async Task A_scan_that_put_files_back_names_them_in_the_line_with_its_result()
    {
        ScanPutsBack([A, B], Result);
        var vm = NewViewModel();

        await RescanAsync(vm);

        Assert.True(vm.HasPutBack);
        Assert.Equal(string.Format(Strings.Summary_PutBack, "a.msi, b.msp"), vm.PutBackText);
        Assert.True(vm.HasOrphans);
    }

    [Fact]
    public async Task The_line_goes_with_the_next_result_that_put_nothing_back()
    {
        ScanPutsBack([A], Result);
        var vm = NewViewModel();
        await RescanAsync(vm);
        Assert.True(vm.HasPutBack);

        ScanPutsBack([], Result);
        await RescanAsync(vm);

        Assert.False(vm.HasPutBack);
    }

    [Fact]
    public async Task The_put_back_report_is_not_shown_as_progress()
    {
        ScanPutsBack([A], Result);
        var vm = NewViewModel();
        var tickers = new List<string>();
        vm.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName == nameof(ScanViewModel.ScanTicker)) tickers.Add(vm.ScanTicker);
        };

        await RescanAsync(vm);

        // The report carries no message, and a ticker line set from it would blank the line.
        Assert.DoesNotContain(tickers, t => t.Length > 0);
    }

    [Fact]
    public async Task A_scan_that_fails_after_putting_files_back_says_so_in_its_message_once()
    {
        ScanPutsBack([A], () => throw new LocalisedInvalidOperationException("The scan stopped.", "Nothing was removed."));
        var vm = NewViewModel();

        await RescanAsync(vm);

        var line = string.Format(Strings.Summary_PutBack, "a.msi");
        Assert.EndsWith(line, vm.LastScanError, StringComparison.Ordinal);
        Assert.StartsWith("The scan stopped.", vm.LastScanError, StringComparison.Ordinal);
        Assert.False(vm.HasPutBack);

        // The next scan puts nothing back, and has nothing left over to say.
        ScanPutsBack([], Result);
        await RescanAsync(vm);
        Assert.False(vm.HasPutBack);
    }

    [Fact]
    public async Task A_scan_that_fails_having_put_nothing_back_has_its_message_as_it_is()
    {
        ScanPutsBack([], () => throw new LocalisedInvalidOperationException("The scan stopped.", "Nothing was removed."));
        var vm = NewViewModel();

        await RescanAsync(vm);

        Assert.DoesNotContain("a.msi", vm.LastScanError, StringComparison.Ordinal);
    }

    [Fact]
    public async Task A_cancelled_scans_files_are_named_with_the_next_result()
    {
        ScanPutsBack([A], () => throw new OperationCanceledException());
        var vm = NewViewModel();
        await RescanAsync(vm);
        Assert.True(vm.LastScanWasCancelled);
        Assert.False(vm.HasPutBack);

        // This scan found nothing left to put back: the first one's file is named now.
        ScanPutsBack([], Result);
        await RescanAsync(vm);

        Assert.Equal(string.Format(Strings.Summary_PutBack, "a.msi"), vm.PutBackText);
    }

    [Fact]
    public async Task Files_a_scan_left_are_named_under_the_files_it_put_back_those_for_a_later_scan_first()
    {
        ScanReports(new InHandRecoveryResult(
            [A], LeftAsSource: 1, LeftNameTaken: 1, LeftForALaterScan: ["c.msi"], LeftAsSourceNames: ["d.msi"]));
        var vm = NewViewModel();

        await RescanAsync(vm);

        Assert.Equal(
            string.Join(Environment.NewLine,
                string.Format(Strings.Summary_PutBack, "a.msi"),
                string.Format(Strings.Summary_LeftInHand, "c.msi"),
                string.Format(Strings.Summary_LeftAsSource, "d.msi")),
            vm.PutBackText);
    }

    [Fact]
    public async Task Files_left_by_a_scan_that_put_nothing_back_show_on_their_own()
    {
        ScanReports(new InHandRecoveryResult([], LeftInstallerBusy: 1, LeftForALaterScan: ["c.msi"]));
        var vm = NewViewModel();

        await RescanAsync(vm);

        Assert.True(vm.HasPutBack);
        Assert.Equal(string.Format(Strings.Summary_LeftInHand, "c.msi"), vm.PutBackText);
    }

    [Fact]
    public async Task Files_left_are_said_by_each_scan_for_itself_and_go_with_a_scan_that_left_none()
    {
        ScanReports(new InHandRecoveryResult([], LeftInstallerBusy: 1, LeftForALaterScan: ["c.msi"]));
        var vm = NewViewModel();
        await RescanAsync(vm);
        Assert.True(vm.HasPutBack);

        ScanPutsBack([], Result);
        await RescanAsync(vm);

        Assert.False(vm.HasPutBack);
    }

    [Fact]
    public async Task A_cancelled_scans_files_left_are_not_carried_to_the_next_result()
    {
        _scanService.ScanAsync(Arg.Any<IProgress<ScanProgressUpdate>?>(), Arg.Any<CancellationToken>())
            .Returns(call =>
            {
                call.Arg<IProgress<ScanProgressUpdate>?>()!.Report(ScanProgressUpdate.ForPutBack(
                    new InHandRecoveryResult([], LeftInstallerBusy: 1, LeftForALaterScan: ["c.msi"])));
                return Task.FromException<ScanResult>(new OperationCanceledException());
            });
        var vm = NewViewModel();
        await RescanAsync(vm);

        ScanPutsBack([], Result);
        await RescanAsync(vm);

        Assert.False(vm.HasPutBack);
    }
}
