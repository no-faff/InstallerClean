using System.ComponentModel;
using NSubstitute;
using InstallerClean.Models;
using InstallerClean.Resources;
using InstallerClean.Services;
using InstallerClean.ViewModels;

namespace InstallerClean.Tests.ViewModels;

/// <summary>
/// The update check's two paths. They call the same service and differ
/// entirely in what they are allowed to say: the automatic check runs
/// unasked at every launch, so it reports a newer version and nothing else,
/// while the manual check is a click that must always answer.
/// </summary>
public class ChromeViewModelTests
{
    // The one destination every update control opens; the found release's
    // own tag page is nowhere in the flow.
    private const string ReleasesPage = UpdateCheckService.ReleasesPageUrl;

    private readonly IWindowService _windowService = Substitute.For<IWindowService>();
    private readonly IMsiFileInfoService _msiInfoService = Substitute.For<IMsiFileInfoService>();
    private readonly ISettingsService _settingsService = Substitute.For<ISettingsService>();
    private readonly IUpdateCheckService _updateCheckService = Substitute.For<IUpdateCheckService>();
    private readonly IDialogService _dialogService = Substitute.For<IDialogService>();
    private readonly IFileSystemScanService _scanService = Substitute.For<IFileSystemScanService>();
    private readonly IPendingRebootService _rebootService = Substitute.For<IPendingRebootService>();

    /// <summary>
    /// Cooldown zeroed so a manual check runs to its end (including the
    /// clear that follows the spacing window) inside the awaited command
    /// rather than five real seconds later.
    /// </summary>
    private ChromeViewModel CreateViewModel(bool autoUpdateCheck = true)
    {
        _settingsService.Load().Returns(new AppSettings { AutoUpdateCheck = autoUpdateCheck });
        var scan = new ScanViewModel(_scanService, _rebootService, _dialogService);
        return new ChromeViewModel(
            _windowService, _msiInfoService, _settingsService,
            _updateCheckService, _dialogService, scan,
            checkCooldownOverride: TimeSpan.Zero);
    }

    /// <summary>
    /// Every value the status line was given, in order. The manual check's
    /// wording is transient by design (it clears once the cooldown is out),
    /// so the end state alone cannot show that anything was ever displayed.
    /// </summary>
    private static List<string> RecordStatusText(ChromeViewModel vm)
    {
        var seen = new List<string>();
        vm.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName == nameof(ChromeViewModel.UpdateStatusText))
                seen.Add(vm.UpdateStatusText);
        };
        return seen;
    }

    [Fact]
    public async Task Automatic_check_opens_no_socket_when_the_setting_is_off()
    {
        var vm = CreateViewModel(autoUpdateCheck: false);

        await vm.RunAutomaticUpdateCheckAsync();

        await _updateCheckService.DidNotReceive().CheckAsync(Arg.Any<UpdateCheckOrigin>(), Arg.Any<CancellationToken>());
        Assert.Equal(string.Empty, vm.UpdateStatusText);
    }

    [Fact]
    public async Task Automatic_check_paints_the_link_when_a_newer_version_exists()
    {
        _updateCheckService.CheckAsync(Arg.Any<UpdateCheckOrigin>(), Arg.Any<CancellationToken>())
            .Returns(new UpdateAvailable("2.1.0", "9.9.9"));
        var vm = CreateViewModel();

        await vm.RunAutomaticUpdateCheckAsync();

        Assert.Equal(
            string.Format(Strings.UpdateCheck_Status_UpdateAvailable, "9.9.9"),
            vm.UpdateStatusText);
        Assert.True(vm.HasUpdateLink);
        // A check nobody asked for never opens a window over whatever the
        // user was doing; the status line is the whole of its voice.
        _windowService.DidNotReceive().ShowUpdateAvailable(Arg.Any<string>(), Arg.Any<string>());
        _dialogService.DidNotReceive().ShowWarning(Arg.Any<string>(), Arg.Any<string>());
    }

    [Fact]
    public async Task Automatic_check_says_nothing_when_it_fails()
    {
        _updateCheckService.CheckAsync(Arg.Any<UpdateCheckOrigin>(), Arg.Any<CancellationToken>())
            .Returns(new CheckFailed(UpdateCheckFailureReason.NetworkUnavailable));
        var vm = CreateViewModel();

        await vm.RunAutomaticUpdateCheckAsync();

        Assert.Equal(string.Empty, vm.UpdateStatusText);
        Assert.False(vm.HasUpdateLink);
        _dialogService.DidNotReceive().ShowWarning(Arg.Any<string>(), Arg.Any<string>());
    }

    /// <summary>
    /// The origin is what keeps a machine with no route to github.com from
    /// writing to crash.log at every launch, so which one each path passes
    /// is part of the contract rather than an implementation detail.
    /// </summary>
    [Fact]
    public async Task Automatic_check_tells_the_service_nobody_asked_for_it()
    {
        _updateCheckService.CheckAsync(Arg.Any<UpdateCheckOrigin>(), Arg.Any<CancellationToken>())
            .Returns(new CheckFailed(UpdateCheckFailureReason.NetworkUnavailable));
        var vm = CreateViewModel();

        await vm.RunAutomaticUpdateCheckAsync();

        await _updateCheckService.Received(1)
            .CheckAsync(UpdateCheckOrigin.Automatic, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Manual_check_tells_the_service_the_user_is_waiting()
    {
        _updateCheckService.CheckAsync(Arg.Any<UpdateCheckOrigin>(), Arg.Any<CancellationToken>())
            .Returns(new CheckFailed(UpdateCheckFailureReason.NetworkUnavailable));
        var vm = CreateViewModel();

        await vm.CheckForUpdatesCommand.ExecuteAsync(null);

        await _updateCheckService.Received(1)
            .CheckAsync(UpdateCheckOrigin.Manual, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Automatic_check_says_nothing_when_the_build_is_current()
    {
        _updateCheckService.CheckAsync(Arg.Any<UpdateCheckOrigin>(), Arg.Any<CancellationToken>())
            .Returns(new UpToDate("2.1.0"));
        var vm = CreateViewModel();

        await vm.RunAutomaticUpdateCheckAsync();

        Assert.Equal(string.Empty, vm.UpdateStatusText);
        Assert.False(vm.HasUpdateLink);
    }

    [Fact]
    public async Task Manual_check_reports_up_to_date_then_clears_it()
    {
        _updateCheckService.CheckAsync(Arg.Any<UpdateCheckOrigin>(), Arg.Any<CancellationToken>())
            .Returns(new UpToDate("2.1.0"));
        var vm = CreateViewModel();
        var seen = RecordStatusText(vm);

        await vm.CheckForUpdatesCommand.ExecuteAsync(null);

        Assert.Contains(Strings.UpdateCheck_Status_Checking, seen);
        Assert.Contains(Strings.UpdateCheck_Status_UpToDate, seen);
        Assert.Equal(string.Empty, vm.UpdateStatusText);
        Assert.False(vm.HasUpdateLink);
    }

    [Fact]
    public async Task Manual_check_opens_the_release_page_when_the_user_accepts()
    {
        _updateCheckService.CheckAsync(Arg.Any<UpdateCheckOrigin>(), Arg.Any<CancellationToken>())
            .Returns(new UpdateAvailable("2.1.0", "9.9.9"));
        _windowService.ShowUpdateAvailable("2.1.0", "9.9.9").Returns(true);
        var vm = CreateViewModel();

        await vm.CheckForUpdatesCommand.ExecuteAsync(null);

        _windowService.Received(1).OpenUrl(ReleasesPage);
        // Both survive the cooldown's clear: the dialog is dismissed by then,
        // and the line is the only remaining route to the download.
        Assert.Equal(
            string.Format(Strings.UpdateCheck_Status_UpdateAvailable, "9.9.9"),
            vm.UpdateStatusText);
        Assert.True(vm.HasUpdateLink);
    }

    [Fact]
    public async Task Manual_check_leaves_the_release_page_alone_when_the_user_declines()
    {
        _updateCheckService.CheckAsync(Arg.Any<UpdateCheckOrigin>(), Arg.Any<CancellationToken>())
            .Returns(new UpdateAvailable("2.1.0", "9.9.9"));
        _windowService.ShowUpdateAvailable("2.1.0", "9.9.9").Returns(false);
        var vm = CreateViewModel();

        await vm.CheckForUpdatesCommand.ExecuteAsync(null);

        _windowService.DidNotReceive().OpenUrl(Arg.Any<string>());
        Assert.True(vm.HasUpdateLink);
    }

    [Fact]
    public async Task Manual_check_shows_a_warning_when_it_fails()
    {
        _updateCheckService.CheckAsync(Arg.Any<UpdateCheckOrigin>(), Arg.Any<CancellationToken>())
            .Returns(new CheckFailed(UpdateCheckFailureReason.Timeout));
        var vm = CreateViewModel();

        await vm.CheckForUpdatesCommand.ExecuteAsync(null);

        _dialogService.Received(1).ShowWarning(
            Strings.UpdateCheck_Failed_Timeout, Strings.UpdateCheck_Title);
        Assert.Equal(string.Empty, vm.UpdateStatusText);
        Assert.False(vm.HasUpdateLink);
    }

    [Fact]
    public void OpenUpdatePage_does_nothing_before_a_check_has_found_one()
    {
        var vm = CreateViewModel();

        vm.OpenUpdatePageCommand.Execute(null);

        _windowService.DidNotReceive().OpenUrl(Arg.Any<string>());
    }

    [Fact]
    public async Task OpenUpdatePage_opens_the_releases_page_after_the_automatic_check()
    {
        _updateCheckService.CheckAsync(Arg.Any<UpdateCheckOrigin>(), Arg.Any<CancellationToken>())
            .Returns(new UpdateAvailable("2.1.0", "9.9.9"));
        var vm = CreateViewModel();
        await vm.RunAutomaticUpdateCheckAsync();

        vm.OpenUpdatePageCommand.Execute(null);

        _windowService.Received(1).OpenUrl(ReleasesPage);
    }
}
