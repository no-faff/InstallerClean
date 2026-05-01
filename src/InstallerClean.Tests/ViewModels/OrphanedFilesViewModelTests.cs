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
            new(@"C:\Windows\Installer\small.msi", 100, false, Orphaned),
            new(@"C:\Windows\Installer\large.msi", 10_000, false, Orphaned),
            new(@"C:\Windows\Installer\medium.msi", 1_000, false, Orphaned),
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
            new(@"C:\Windows\Installer\a.msi", 524_288, false, Orphaned),
            new(@"C:\Windows\Installer\b.msi", 524_288, false, Orphaned),
        };

        var vm = new OrphanedFilesViewModel(files, NullInfoService());

        Assert.Equal("2 files (1.0 MB)", vm.Summary);
    }

    [Fact]
    public void Summary_singular_for_one_file()
    {
        var files = new List<OrphanedFile>
        {
            new(@"C:\Windows\Installer\a.msi", 1_048_576, false, Orphaned),
        };

        var vm = new OrphanedFilesViewModel(files, NullInfoService());

        Assert.Equal("1 file (1.0 MB)", vm.Summary);
    }

    [Fact]
    public void First_file_is_selected_by_default()
    {
        var files = new List<OrphanedFile>
        {
            new(@"C:\Windows\Installer\small.msi", 100, false, Orphaned),
            new(@"C:\Windows\Installer\large.msi", 10_000, false, Orphaned),
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
            new(@"C:\Windows\Installer\a.msi", 1024, false, Orphaned),
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
        // Pattern: gate the substituted info service on a ManualResetEventSlim
        // (synchronous wait, sub-second timeout) rather than awaiting a
        // TaskCompletionSource inside the Do() callback. xUnit1031 forbids
        // task blocking inside test bodies, but synchronous synchronisation
        // primitives in a worker callback are fine: the Do() runs on the
        // thread-pool task spun up by OrphanedFilesViewModel.OnSelectedFileChanged,
        // not the test thread.
        var infoServiceStarted = new ManualResetEventSlim(initialState: false);
        var allowComplete = new ManualResetEventSlim(initialState: false);
        var infoService = Substitute.For<IMsiFileInfoService>();
        infoService
            .When(s => s.GetSummaryInfo(Arg.Any<string>()))
            .Do(_ =>
            {
                infoServiceStarted.Set();
                allowComplete.Wait(TimeSpan.FromSeconds(2));
            });
        infoService.GetSummaryInfo(Arg.Any<string>()).Returns((MsiSummaryInfo?)null);

        var files = new List<OrphanedFile>
        {
            new(@"C:\Windows\Installer\a.msi", 1024, false, Orphaned),
        };
        var vm = new OrphanedFilesViewModel(files, infoService);

        // Wait for the worker to enter the substituted info service via
        // a polling loop with an async sleep, so the test thread itself
        // never blocks on a Task.
        var deadline = DateTime.UtcNow.AddSeconds(2);
        while (!infoServiceStarted.IsSet && DateTime.UtcNow < deadline)
            await Task.Delay(10);
        Assert.True(infoServiceStarted.IsSet, "Worker never entered the info service.");

        vm.Dispose();
        allowComplete.Set();

        // Settle the worker. The cancellation token is checked AFTER the
        // GetSummaryInfo call completes; SelectedDetails should not be
        // assigned because the token signals cancellation by then.
        await Task.Delay(50);
        Assert.Null(vm.SelectedDetails);
    }
}
