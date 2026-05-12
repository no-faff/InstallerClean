using NSubstitute;
using InstallerClean.Models;
using InstallerClean.Resources;
using InstallerClean.Services;
using InstallerClean.ViewModels;

namespace InstallerClean.Tests.ViewModels;

public class OrphanedFilesViewModelTests
{
    private static readonly string Orphaned = Strings.Reason_Orphaned;

    private static IMsiFileInfoService NullInfoService()
    {
        var mock = Substitute.For<IMsiFileInfoService>();
        mock.GetSummaryInfo(Arg.Any<string>()).Returns((MsiSummaryInfo?)null);
        return mock;
    }

    [Fact]
    public void Files_sorted_by_size_descending()
    {
        var files = new List<OrphanedFile>
        {
            new(@"C:\Windows\Installer\small.msi", 100, false, false, Orphaned),
            new(@"C:\Windows\Installer\large.msi", 10_000, false, false, Orphaned),
            new(@"C:\Windows\Installer\medium.msi", 1_000, false, false, Orphaned),
        };

        var vm = new OrphanedFilesViewModel(files, NullInfoService());

        Assert.Equal("large.msi", vm.Files[0].FileName);
        Assert.Equal("medium.msi", vm.Files[1].FileName);
        Assert.Equal("small.msi", vm.Files[2].FileName);
    }

    [Fact]
    public void Summary_shows_count_and_total_size()
    {
        var files = new List<OrphanedFile>
        {
            new(@"C:\Windows\Installer\a.msi", 524_288, false, false, Orphaned),
            new(@"C:\Windows\Installer\b.msi", 524_288, false, false, Orphaned),
        };

        var vm = new OrphanedFilesViewModel(files, NullInfoService());

        Assert.Equal("2 unused files (1.0 MB)", vm.Summary);
    }

    [Fact]
    public void Summary_singular_for_one_file()
    {
        var files = new List<OrphanedFile>
        {
            new(@"C:\Windows\Installer\a.msi", 1_048_576, false, false, Orphaned),
        };

        var vm = new OrphanedFilesViewModel(files, NullInfoService());

        Assert.Equal("1 unused file (1.0 MB)", vm.Summary);
    }

    [Fact]
    public void First_file_is_selected_by_default()
    {
        var files = new List<OrphanedFile>
        {
            new(@"C:\Windows\Installer\small.msi", 100, false, false, Orphaned),
            new(@"C:\Windows\Installer\large.msi", 10_000, false, false, Orphaned),
        };

        var vm = new OrphanedFilesViewModel(files, NullInfoService());

        Assert.NotNull(vm.SelectedFile);
        Assert.Equal("large.msi", vm.SelectedFile!.FileName); // largest first
    }

    [Fact]
    public void Empty_list_has_no_selection()
    {
        var vm = new OrphanedFilesViewModel(
            new List<OrphanedFile>(), NullInfoService());

        Assert.Null(vm.SelectedFile);
        Assert.False(vm.HasSelection);
    }

    [Fact]
    public void HasSelection_true_when_file_selected()
    {
        var files = new List<OrphanedFile>
        {
            new(@"C:\Windows\Installer\a.msi", 1024, false, false, Orphaned),
        };

        var vm = new OrphanedFilesViewModel(files, NullInfoService());

        Assert.True(vm.HasSelection);
    }

    [Fact]
    public async Task Dispose_cancels_in_flight_metadata_load()
    {
        // The detail-window VM kicks off a metadata read on the worker
        // thread when the selection changes. If the user closes the
        // window while that read is still pending, Dispose must cancel
        // it so the SHFile / MSI-API call doesn't keep running and
        // populate SelectedDetails on a window that's already gone.
        // Without this guarantee a regression that drops the Closed
        // handler on the window code-behind would silently leak per-
        // open Tasks into the process lifetime.
        //
        // Test-side wait uses a TaskCompletionSource the worker callback
        // signals via TrySetResult. The test body awaits that TCS task
        // (no polling, no fixed-time deadline). The worker callback
        // still uses ManualResetEventSlim because it has to BLOCK
        // synchronously inside NSubstitute's Do() (which has no async
        // overload) to keep the metadata load "in flight" while the
        // test calls Dispose.
        var infoServiceStarted = new TaskCompletionSource();
        var allowComplete = new ManualResetEventSlim(initialState: false);
        var infoService = Substitute.For<IMsiFileInfoService>();
        infoService
            .When(s => s.GetSummaryInfo(Arg.Any<string>()))
            .Do(_ =>
            {
                infoServiceStarted.TrySetResult();
                allowComplete.Wait(TimeSpan.FromSeconds(5));
            });
        infoService.GetSummaryInfo(Arg.Any<string>()).Returns((MsiSummaryInfo?)null);

        var files = new List<OrphanedFile>
        {
            new(@"C:\Windows\Installer\a.msi", 1024, false, false, Orphaned),
        };
        var vm = new OrphanedFilesViewModel(files, infoService);

        // Wait for the worker to enter the substituted info service.
        // No polling: just await the TCS the callback set above. Bound
        // by a generous 5s timeout via Task.WhenAny so a regression
        // that fails to call the info service at all surfaces as a
        // test failure rather than hanging the suite.
        var firstToFinish = await Task.WhenAny(
            infoServiceStarted.Task,
            Task.Delay(TimeSpan.FromSeconds(5)));
        Assert.Same(infoServiceStarted.Task, firstToFinish);

        vm.Dispose();
        allowComplete.Set();

        // Poll-with-timeout for the worker's post-GetSummaryInfo
        // cancellation observation to settle. SelectedDetails becomes
        // non-null only if the worker missed the cancellation; a
        // continuously-null reading across the polling window confirms
        // the cancellation was honoured. Polling instead of a fixed
        // delay so the test surfaces a regression immediately rather
        // than relying on a fixed-time hope.
        var pollEnd = DateTime.UtcNow + TimeSpan.FromSeconds(5);
        while (DateTime.UtcNow < pollEnd)
        {
            if (vm.SelectedDetails is not null) break;
            await Task.Delay(10);
        }
        Assert.Null(vm.SelectedDetails);
    }
}
