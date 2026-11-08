using System.ComponentModel;
using NSubstitute;
using InstallerClean.Helpers;
using InstallerClean.Models;
using InstallerClean.Resources;
using InstallerClean.Services;
using InstallerClean.ViewModels;

namespace InstallerClean.Tests.ViewModels;

/// <summary>
/// The update check's two paths. They call the same service and differ
/// entirely in what they are allowed to say: the automatic check runs
/// unasked at every launch, so it reports a newer version and nothing else,
/// while the manual check is a click that must always answer. A newer version
/// either finds shows the notice.
/// </summary>
public class ChromeViewModelTests
{
    // Where the link to the release page goes once a check has found 9.9.9: that
    // version's own release page, written out rather than built, so the address the
    // app builds is held to what it should be.
    private const string ReleasesPage = "https://github.com/no-faff/InstallerClean/releases/tag/v9.9.9";

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

    private void CheckAnswers(params UpdateCheckResult[] results) =>
        _updateCheckService.CheckAsync(Arg.Any<UpdateCheckOrigin>(), Arg.Any<CancellationToken>())
            .Returns(results[0], results.Slice(1..));

    [Fact]
    public async Task Automatic_check_opens_no_socket_when_the_setting_is_off()
    {
        var vm = CreateViewModel(autoUpdateCheck: false);

        await vm.RunAutomaticUpdateCheckAsync();

        await _updateCheckService.DidNotReceive().CheckAsync(Arg.Any<UpdateCheckOrigin>(), Arg.Any<CancellationToken>());
        Assert.Equal(string.Empty, vm.UpdateStatusText);
        Assert.False(vm.HasNewerVersion);
    }

    [Fact]
    public async Task Automatic_check_shows_the_notice_when_a_newer_version_exists()
    {
        CheckAnswers(new UpdateAvailable("2.1.0", "9.9.9"));
        var vm = CreateViewModel();
        var found = 0;
        vm.NewerVersionFound += (_, _) => found++;

        await vm.RunAutomaticUpdateCheckAsync();

        Assert.True(vm.HasNewerVersion);
        Assert.Equal(string.Format(Strings.UpdateNotice_Available, "9.9.9"), vm.UpdateNoticeText);
        Assert.Equal(1, found);
        // A check nobody asked for opens no window over whatever the user was
        // doing, and the status line beside the button stays empty.
        Assert.Equal(string.Empty, vm.UpdateStatusText);
        _dialogService.DidNotReceive().ShowWarning(Arg.Any<string>(), Arg.Any<string>());
        _windowService.DidNotReceive().OpenUrl(Arg.Any<string>());
    }

    [Fact]
    public async Task Automatic_check_says_nothing_when_it_fails()
    {
        CheckAnswers(new CheckFailed(UpdateCheckFailureReason.NetworkUnavailable));
        var vm = CreateViewModel();

        await vm.RunAutomaticUpdateCheckAsync();

        Assert.Equal(string.Empty, vm.UpdateStatusText);
        Assert.False(vm.HasNewerVersion);
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
        CheckAnswers(new CheckFailed(UpdateCheckFailureReason.NetworkUnavailable));
        var vm = CreateViewModel();

        await vm.RunAutomaticUpdateCheckAsync();

        await _updateCheckService.Received(1)
            .CheckAsync(UpdateCheckOrigin.Automatic, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Manual_check_tells_the_service_the_user_is_waiting()
    {
        CheckAnswers(new CheckFailed(UpdateCheckFailureReason.NetworkUnavailable));
        var vm = CreateViewModel();

        await vm.CheckForUpdatesCommand.ExecuteAsync(null);

        await _updateCheckService.Received(1)
            .CheckAsync(UpdateCheckOrigin.Manual, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Automatic_check_says_nothing_when_the_build_is_current()
    {
        CheckAnswers(new UpToDate("2.1.0"));
        var vm = CreateViewModel();

        await vm.RunAutomaticUpdateCheckAsync();

        Assert.Equal(string.Empty, vm.UpdateStatusText);
        Assert.False(vm.HasNewerVersion);
    }

    [Fact]
    public async Task Manual_check_reports_up_to_date_then_clears_it()
    {
        CheckAnswers(new UpToDate("2.1.0"));
        var vm = CreateViewModel();
        var seen = RecordStatusText(vm);

        await vm.CheckForUpdatesCommand.ExecuteAsync(null);

        Assert.Contains(Strings.UpdateCheck_Status_Checking, seen);
        Assert.Contains(Strings.UpdateCheck_Status_UpToDate, seen);
        Assert.Equal(string.Empty, vm.UpdateStatusText);
        Assert.False(vm.HasNewerVersion);
    }

    [Fact]
    public async Task Manual_check_shows_the_notice_when_a_newer_version_exists()
    {
        CheckAnswers(new UpdateAvailable("2.1.0", "9.9.9"));
        var vm = CreateViewModel(autoUpdateCheck: false);
        var seen = RecordStatusText(vm);

        await vm.CheckForUpdatesCommand.ExecuteAsync(null);

        Assert.Contains(Strings.UpdateCheck_Status_Checking, seen);
        Assert.Equal(string.Empty, vm.UpdateStatusText);
        Assert.True(vm.HasNewerVersion);
        Assert.Equal(string.Format(Strings.UpdateNotice_Available, "9.9.9"), vm.UpdateNoticeText);
        _windowService.DidNotReceive().OpenUrl(Arg.Any<string>());
        _dialogService.DidNotReceive().ShowWarning(Arg.Any<string>(), Arg.Any<string>());
    }

    /// <summary>
    /// A click that finds the version the notice already names changes nothing on
    /// screen, so the event is the only answer the window can read out for it.
    /// </summary>
    [Fact]
    public async Task Manual_check_answers_a_click_that_finds_the_version_the_notice_names()
    {
        CheckAnswers(new UpdateAvailable("2.1.0", "9.9.9"), new UpdateAvailable("2.1.0", "9.9.9"));
        var vm = CreateViewModel();
        var found = 0;
        vm.NewerVersionFound += (_, _) => found++;
        await vm.RunAutomaticUpdateCheckAsync();

        await vm.CheckForUpdatesCommand.ExecuteAsync(null);

        Assert.Equal(2, found);
        Assert.True(vm.HasNewerVersion);
    }

    [Fact]
    public async Task Manual_check_that_fails_keeps_the_notice()
    {
        CheckAnswers(new UpdateAvailable("2.1.0", "9.9.9"), new CheckFailed(UpdateCheckFailureReason.Timeout));
        var vm = CreateViewModel();
        await vm.RunAutomaticUpdateCheckAsync();

        await vm.CheckForUpdatesCommand.ExecuteAsync(null);

        _dialogService.Received(1).ShowWarning(
            Strings.UpdateCheck_Failed_Timeout, Strings.UpdateCheck_Title);
        Assert.True(vm.HasNewerVersion);
    }

    [Fact]
    public async Task Manual_check_that_finds_this_build_current_takes_the_notice_down()
    {
        CheckAnswers(new UpdateAvailable("2.1.0", "9.9.9"), new UpToDate("2.1.0"));
        var vm = CreateViewModel();
        await vm.RunAutomaticUpdateCheckAsync();

        await vm.CheckForUpdatesCommand.ExecuteAsync(null);

        Assert.False(vm.HasNewerVersion);
    }

    [Fact]
    public async Task Manual_check_shows_a_warning_when_it_fails()
    {
        CheckAnswers(new CheckFailed(UpdateCheckFailureReason.Timeout));
        var vm = CreateViewModel();

        await vm.CheckForUpdatesCommand.ExecuteAsync(null);

        _dialogService.Received(1).ShowWarning(
            Strings.UpdateCheck_Failed_Timeout, Strings.UpdateCheck_Title);
        Assert.Equal(string.Empty, vm.UpdateStatusText);
        Assert.False(vm.HasNewerVersion);
    }

    [Fact]
    public void OpenReleasePage_does_nothing_before_a_check_has_found_one()
    {
        var vm = CreateViewModel();

        vm.OpenReleasePageCommand.Execute(null);

        _windowService.DidNotReceive().OpenUrl(Arg.Any<string>());
    }

    /// <summary>
    /// The CanExecute coupling is what keeps the hidden link out of the main
    /// window's Tab order while there is no notice (the why is on
    /// OpenReleasePage itself), and the CanExecuteChanged raise is how the link
    /// learns to re-query it, so both are contract rather than convenience.
    /// </summary>
    [Fact]
    public async Task OpenReleasePage_cannot_execute_until_a_check_has_found_one()
    {
        CheckAnswers(new UpdateAvailable("2.1.0", "9.9.9"));
        var vm = CreateViewModel();
        var raised = 0;
        vm.OpenReleasePageCommand.CanExecuteChanged += (_, _) => raised++;

        Assert.False(vm.OpenReleasePageCommand.CanExecute(null));

        await vm.RunAutomaticUpdateCheckAsync();

        Assert.True(vm.OpenReleasePageCommand.CanExecute(null));
        Assert.True(raised > 0);
    }

    [Fact]
    public async Task OpenReleasePage_opens_the_found_versions_release_page_after_the_automatic_check()
    {
        CheckAnswers(new UpdateAvailable("2.1.0", "9.9.9"));
        var vm = CreateViewModel();
        await vm.RunAutomaticUpdateCheckAsync();

        vm.OpenReleasePageCommand.Execute(null);

        _windowService.Received(1).OpenUrl(ReleasesPage);
    }

    // ---- Update now ----

    private readonly IAppUpdater _updater = Substitute.For<IAppUpdater>();
    private readonly List<string> _calls = [];

    /// <summary>
    /// A view model whose copy can update itself, and a check that has found 9.9.9.
    /// </summary>
    private async Task<ChromeViewModel> CreateUpdatingViewModelAsync()
    {
        _updater.Kind.Returns(AppCopyKind.Portable);
        _settingsService.Load().Returns(new AppSettings { AutoUpdateCheck = true });
        CheckAnswers(new UpdateAvailable("2.1.0", "9.9.9"));
        var scan = new ScanViewModel(_scanService, _rebootService, _dialogService);
        var vm = new ChromeViewModel(
            _windowService, _msiInfoService, _settingsService,
            _updateCheckService, _dialogService, scan,
            checkCooldownOverride: TimeSpan.Zero,
            updater: _updater);
        await vm.RunAutomaticUpdateCheckAsync();
        return vm;
    }

    private PreparedUpdate Prepared(Action? undo = null) =>
        new("9.9.9", @"C:\ProgramData\InstallerClean-update-x\InstallerClean-9.9.9-setup.exe", "/SILENT /UPDATEFROMAPP",
            undo ?? (() => _calls.Add("undo")));

    [Fact]
    public async Task A_copy_that_can_update_itself_shows_Update_now_and_not_the_link()
    {
        var vm = await CreateUpdatingViewModelAsync();

        Assert.True(vm.ShowsUpdateNow);
        Assert.False(vm.ShowsReleaseLink);
        Assert.True(vm.UpdateNowCommand.CanExecute(null));
        Assert.False(vm.OpenReleasePageCommand.CanExecute(null));
    }

    [Fact]
    public async Task A_copy_that_cannot_update_itself_shows_the_link_alone()
    {
        CheckAnswers(new UpdateAvailable("2.1.0", "9.9.9"));
        var vm = CreateViewModel();
        await vm.RunAutomaticUpdateCheckAsync();

        Assert.False(vm.ShowsUpdateNow);
        Assert.True(vm.ShowsReleaseLink);
        Assert.False(vm.UpdateNowCommand.CanExecute(null));
    }

    [Fact]
    public async Task Update_now_downloads_the_found_version_and_hands_over()
    {
        var vm = await CreateUpdatingViewModelAsync();
        var prepared = Prepared();
        _updater.PrepareAsync("9.9.9", Arg.Any<IProgress<UpdateProgress>>(), Arg.Any<CancellationToken>())
            .Returns(_ => { _calls.Add("prepare"); return prepared; });
        _windowService.StartUpdateAndCloseAsync(prepared)
            .Returns(_ => { _calls.Add("start"); return true; });

        await vm.UpdateNowCommand.ExecuteAsync(null);

        Assert.Equal(["prepare", "start"], _calls);
        // Handed over, the app is closing, and the card stays up until it has.
        Assert.True(vm.IsUpdating);
        Assert.False(vm.HasUpdateFailure);
        Assert.False(vm.CancelUpdateCommand.CanExecute(null));
    }

    [Fact]
    public async Task The_card_names_the_version_being_downloaded()
    {
        var vm = await CreateUpdatingViewModelAsync();
        var seen = new List<string>();
        _updater.PrepareAsync(default!, default, default).ReturnsForAnyArgs(_ =>
        {
            seen.Add(vm.UpdatingText);
            return Task.FromException<PreparedUpdate>(new UpdateFailedException(UpdateFailureStage.Download, "no"));
        });

        await vm.UpdateNowCommand.ExecuteAsync(null);

        Assert.Equal([string.Format(Strings.UpdateNotice_Downloading, "9.9.9")], seen);
    }

    [Fact]
    public async Task The_card_says_the_download_is_being_checked_once_it_is_complete()
    {
        var vm = await CreateUpdatingViewModelAsync();
        IProgress<UpdateProgress>? progress = null;
        var download = new TaskCompletionSource<PreparedUpdate>();
        _updater.PrepareAsync(default!, default, default).ReturnsForAnyArgs(call =>
        {
            progress = call.ArgAt<IProgress<UpdateProgress>>(1);
            return download.Task;
        });

        var run = vm.UpdateNowCommand.ExecuteAsync(null);
        bool measured;
        string? seen;
        bool known;
        try
        {
            Assert.NotNull(progress);
            // Progress<T> posts each report, so the card changes a moment after it is made.
            var waited = System.Diagnostics.Stopwatch.StartNew();
            progress.Report(new UpdateProgress(0.5, Checking: false));
            while (!vm.IsUpdateProgressKnown && waited.Elapsed < TimeSpan.FromSeconds(5))
                await Task.Delay(10);
            measured = vm.IsUpdateProgressKnown;
            progress.Report(new UpdateProgress(null, Checking: true));
            while (vm.UpdatingText != Strings.UpdateNotice_CheckingDownload && waited.Elapsed < TimeSpan.FromSeconds(10))
                await Task.Delay(10);
            seen = vm.UpdatingText;
            known = vm.IsUpdateProgressKnown;
        }
        finally
        {
            download.TrySetException(new UpdateFailedException(UpdateFailureStage.Install, "no"));
            await run;
        }

        Assert.True(measured);
        Assert.Equal(Strings.UpdateNotice_CheckingDownload, seen);
        Assert.False(known);
    }

    [Fact]
    public async Task A_start_that_fails_puts_everything_back_and_says_it_could_not_be_installed()
    {
        var vm = await CreateUpdatingViewModelAsync();
        _updater.PrepareAsync(default!, default, default).ReturnsForAnyArgs(Prepared());
        _windowService.StartUpdateAndCloseAsync(default!).ReturnsForAnyArgs(false);

        await vm.UpdateNowCommand.ExecuteAsync(null);

        Assert.Equal(["undo"], _calls);
        Assert.False(vm.IsUpdating);
        Assert.Equal(Strings.UpdateNotice_InstallFailed, vm.UpdateFailureText);
        // Update now stays, so the person can try again, and the link joins it.
        Assert.True(vm.ShowsUpdateNow);
        Assert.True(vm.ShowsReleaseLink);
        Assert.True(vm.OpenReleasePageCommand.CanExecute(null));
    }

    [Fact]
    public async Task A_hand_over_that_throws_takes_away_what_was_readied_and_says_it_could_not_be_installed()
    {
        var vm = await CreateUpdatingViewModelAsync();
        _updater.PrepareAsync(default!, default, default).ReturnsForAnyArgs(Prepared());
        _windowService.StartUpdateAndCloseAsync(default!).ReturnsForAnyArgs(
            Task.FromException<bool>(new InvalidOperationException("no")));

        await vm.UpdateNowCommand.ExecuteAsync(null);

        Assert.Equal(["undo"], _calls);
        Assert.False(vm.IsUpdating);
        Assert.Equal(Strings.UpdateNotice_InstallFailed, vm.UpdateFailureText);
    }

    [Fact]
    public async Task Cancel_pressed_as_the_download_finishes_takes_away_what_was_readied_once()
    {
        var vm = await CreateUpdatingViewModelAsync();
        _updater.PrepareAsync(default!, default, default).ReturnsForAnyArgs(_ =>
        {
            vm.CancelUpdateCommand.Execute(null);
            return Prepared();
        });

        await vm.UpdateNowCommand.ExecuteAsync(null);

        Assert.Equal(["undo"], _calls);
        await _windowService.DidNotReceiveWithAnyArgs().StartUpdateAndCloseAsync(default!);
        Assert.False(vm.IsUpdating);
        Assert.False(vm.HasUpdateFailure);
    }

    [Fact]
    public async Task A_refusal_read_as_Cancel_is_pressed_is_still_shown()
    {
        var vm = await CreateUpdatingViewModelAsync();
        _updater.PrepareAsync(default!, default, default).ReturnsForAnyArgs(_ =>
        {
            vm.CancelUpdateCommand.Execute(null);
            return Task.FromException<PreparedUpdate>(
                new UpdateRefusedException(UpdateRefusal.NeedsNetFramework48, "9.9.9"));
        });

        await vm.UpdateNowCommand.ExecuteAsync(null);

        Assert.True(vm.IsUpdateRefused);
        Assert.False(vm.IsUpdating);
        await _windowService.DidNotReceiveWithAnyArgs().StartUpdateAndCloseAsync(default!);
    }

    [Theory]
    [InlineData(UpdateFailureStage.Download)]
    [InlineData(UpdateFailureStage.Install)]
    public async Task A_failed_step_says_which_and_never_starts_anything(UpdateFailureStage stage)
    {
        var vm = await CreateUpdatingViewModelAsync();
        _updater.PrepareAsync(default!, default, default).ReturnsForAnyArgs(
            Task.FromException<PreparedUpdate>(new UpdateFailedException(stage, "no")));

        await vm.UpdateNowCommand.ExecuteAsync(null);

        Assert.False(vm.IsUpdating);
        Assert.Equal(
            stage == UpdateFailureStage.Download ? Strings.UpdateNotice_DownloadFailed : Strings.UpdateNotice_InstallFailed,
            vm.UpdateFailureText);
        await _windowService.DidNotReceiveWithAnyArgs().StartUpdateAndCloseAsync(default!);
    }

    [Fact]
    public async Task Cancel_takes_the_card_away_and_says_nothing()
    {
        var vm = await CreateUpdatingViewModelAsync();
        _updater.PrepareAsync(default!, default, default).ReturnsForAnyArgs(call =>
        {
            var token = call.Arg<CancellationToken>();
            Assert.True(vm.CancelUpdateCommand.CanExecute(null));
            vm.CancelUpdateCommand.Execute(null);
            return Task.FromCanceled<PreparedUpdate>(token);
        });

        await vm.UpdateNowCommand.ExecuteAsync(null);

        Assert.False(vm.IsUpdating);
        Assert.False(vm.HasUpdateFailure);
        await _windowService.DidNotReceiveWithAnyArgs().StartUpdateAndCloseAsync(default!);
    }

    [Fact]
    public async Task A_new_press_clears_the_line_from_the_last_failure()
    {
        var vm = await CreateUpdatingViewModelAsync();
        _updater.PrepareAsync(default!, default, default).ReturnsForAnyArgs(
            Task.FromException<PreparedUpdate>(new UpdateFailedException(UpdateFailureStage.Download, "no")),
            Task.FromResult(Prepared()));
        _windowService.StartUpdateAndCloseAsync(default!).ReturnsForAnyArgs(true);

        await vm.UpdateNowCommand.ExecuteAsync(null);
        Assert.True(vm.HasUpdateFailure);

        await vm.UpdateNowCommand.ExecuteAsync(null);
        Assert.False(vm.HasUpdateFailure);
    }

    [Fact]
    public async Task Update_now_waits_while_a_scan_or_a_Move_or_Delete_is_in_flight()
    {
        _updater.Kind.Returns(AppCopyKind.Portable);
        _settingsService.Load().Returns(new AppSettings { AutoUpdateCheck = true });
        CheckAnswers(new UpdateAvailable("2.1.0", "9.9.9"));
        var busy = true;
        var vm = new ChromeViewModel(
            _windowService, _msiInfoService, _settingsService,
            _updateCheckService, _dialogService, new ScanViewModel(_scanService, _rebootService, _dialogService),
            isBusy: () => busy, checkCooldownOverride: TimeSpan.Zero, updater: _updater);
        await vm.RunAutomaticUpdateCheckAsync();

        Assert.False(vm.UpdateNowCommand.CanExecute(null));
        busy = false;
        Assert.True(vm.UpdateNowCommand.CanExecute(null));
    }

    // ---- The card saying a newer version is available ----

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task The_automatic_check_s_task_completes_whatever_the_check_answers(bool found)
    {
        CheckAnswers(found
            ? new UpdateAvailable("2.1.0", "9.9.9")
            : new CheckFailed(UpdateCheckFailureReason.Timeout));
        var vm = CreateViewModel();

        vm.StartAutomaticUpdateCheck();
        await vm.AutomaticCheck;

        Assert.Equal(found, vm.HasNewerVersion);
    }

    [Fact]
    public async Task A_start_after_an_update_that_was_not_installed_says_so_where_the_check_finds_the_version()
    {
        CheckAnswers(new UpdateAvailable("2.1.0", "9.9.9"));
        var vm = CreateViewModel();

        vm.ShowUpdateFailedAtStart();
        vm.StartAutomaticUpdateCheck();
        await vm.AutomaticCheck;

        Assert.True(vm.HasNewerVersion);
        Assert.True(vm.HasUpdateFailure);
        Assert.Equal(Strings.UpdateNotice_InstallFailed, vm.UpdateFailureText);
    }

    [Fact]
    public async Task The_automatic_check_s_task_completes_with_the_check_switched_off()
    {
        var vm = CreateViewModel(autoUpdateCheck: false);

        vm.StartAutomaticUpdateCheck();
        await vm.AutomaticCheck;

        await _updateCheckService.DidNotReceive().CheckAsync(Arg.Any<UpdateCheckOrigin>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task The_automatic_check_s_task_completes_when_a_manual_check_cancels_it()
    {
        var calls = 0;
        _updateCheckService.CheckAsync(Arg.Any<UpdateCheckOrigin>(), Arg.Any<CancellationToken>())
            .Returns(call => ++calls == 1
                ? NeverAnswersAsync(call.Arg<CancellationToken>())
                : Task.FromResult<UpdateCheckResult>(new UpToDate("2.1.0")));
        var vm = CreateViewModel();

        vm.StartAutomaticUpdateCheck();
        Assert.False(vm.AutomaticCheck.IsCompleted);
        await vm.CheckForUpdatesCommand.ExecuteAsync(null);

        await vm.AutomaticCheck;
        Assert.True(vm.AutomaticCheck.IsCompletedSuccessfully);
    }

    private static async Task<UpdateCheckResult> NeverAnswersAsync(CancellationToken token)
    {
        await Task.Delay(Timeout.Infinite, token);
        throw new InvalidOperationException("unreachable");
    }

    [Fact]
    public void The_card_is_not_put_up_before_a_check_has_found_a_newer_version()
    {
        var vm = CreateViewModel();

        vm.OfferUpdate();

        Assert.False(vm.IsOfferingUpdate);
        Assert.False(vm.ContinueWithoutUpdatingCommand.CanExecute(null));
    }

    [Fact]
    public async Task Continue_without_updating_takes_the_card_away_and_keeps_the_notice()
    {
        CheckAnswers(new UpdateAvailable("2.1.0", "9.9.9"));
        var vm = CreateViewModel();
        await vm.RunAutomaticUpdateCheckAsync();

        vm.OfferUpdate();
        Assert.True(vm.IsOfferingUpdate);
        Assert.True(vm.ContinueWithoutUpdatingCommand.CanExecute(null));

        vm.ContinueWithoutUpdatingCommand.Execute(null);

        Assert.False(vm.IsOfferingUpdate);
        Assert.False(vm.ContinueWithoutUpdatingCommand.CanExecute(null));
        Assert.True(vm.HasNewerVersion);
    }

    [Fact]
    public async Task An_update_that_fails_from_the_card_leaves_the_card_up_with_the_failure_and_the_link()
    {
        var vm = await CreateUpdatingViewModelAsync();
        vm.OfferUpdate();
        _updater.PrepareAsync(default!, default, default).ReturnsForAnyArgs(
            Task.FromException<PreparedUpdate>(new UpdateFailedException(UpdateFailureStage.Download, "no")));

        await vm.UpdateNowCommand.ExecuteAsync(null);

        Assert.True(vm.IsOfferingUpdate);
        Assert.Equal(Strings.UpdateNotice_DownloadFailed, vm.UpdateFailureText);
        Assert.True(vm.ShowsUpdateNow);
        Assert.True(vm.ShowsReleaseLink);
        Assert.True(vm.ContinueWithoutUpdatingCommand.CanExecute(null));
    }

    // ---- What the newer version's release rules out ----

    /// <summary>
    /// A view model on a copy of <paramref name="kind"/>, answering whether it can update
    /// itself as the real updater answers for that kind, with the read of what the newer
    /// version's release needs given a bound of <paramref name="requirementsBound"/>, the
    /// automatic check given <paramref name="automaticCheckWindow"/> to show what it found,
    /// and a check that has found 9.9.9. The automatic check has not run.
    /// </summary>
    private ChromeViewModel CreateRequirementsViewModel(
        AppCopyKind kind = AppCopyKind.Setup, TimeSpan? requirementsBound = null,
        TimeSpan? automaticCheckWindow = null)
    {
        _updater.Kind.Returns(kind);
        _settingsService.Load().Returns(new AppSettings { AutoUpdateCheck = true });
        CheckAnswers(new UpdateAvailable("2.1.0", "9.9.9"));
        var scan = new ScanViewModel(_scanService, _rebootService, _dialogService);
        return new ChromeViewModel(
            _windowService, _msiInfoService, _settingsService,
            _updateCheckService, _dialogService, scan,
            checkCooldownOverride: TimeSpan.Zero,
            updater: _updater,
            requirementsBoundOverride: requirementsBound,
            automaticCheckWindowOverride: automaticCheckWindow);
    }

    private void RequirementsAnswer(UpdateRefusal? refusal) =>
        _updater.CheckRequirementsAsync("9.9.9", Arg.Any<TimeSpan?>(), Arg.Any<CancellationToken>()).Returns(refusal);

    private static string RefusalLine(UpdateRefusal refusal) =>
        refusal == UpdateRefusal.NeedsNetFramework48
            ? Strings.UpdateNotice_NeedsNetFramework48
            : string.Format(Strings.UpdateNotice_CannotInstallAutomatically, "9.9.9");

    /// <summary>
    /// The card and the notice as a refusal leaves them: the reason in the second line, no
    /// Update now, and one link, Microsoft's .NET Framework 4.8 installer where that is the
    /// reason and the release page otherwise.
    /// </summary>
    private static void AssertRefused(ChromeViewModel vm, UpdateRefusal refusal)
    {
        var needsNetFramework = refusal == UpdateRefusal.NeedsNetFramework48;
        Assert.True(vm.HasNewerVersion);
        Assert.True(vm.IsUpdateRefused);
        Assert.Equal(string.Format(Strings.UpdateNotice_Available, "9.9.9"), vm.UpdateNoticeText);
        Assert.Equal(RefusalLine(refusal), vm.UpdateNoticeDetailText);
        Assert.False(vm.ShowsUpdateNow);
        Assert.False(vm.UpdateNowCommand.CanExecute(null));
        Assert.Equal(!needsNetFramework, vm.ShowsReleaseLink);
        Assert.Equal(!needsNetFramework, vm.OpenReleasePageCommand.CanExecute(null));
        Assert.Equal(needsNetFramework, vm.ShowsNetFrameworkLink);
        Assert.Equal(needsNetFramework, vm.OpenNetFrameworkDownloadCommand.CanExecute(null));
        Assert.False(vm.HasUpdateFailure);
    }

    [Theory]
    [InlineData(UpdateRefusal.NeedsNetFramework48)]
    [InlineData(UpdateRefusal.CannotInstallFromThisCopy)]
    public async Task The_automatic_check_says_why_the_release_rules_this_copy_out_in_place_of_Update_now(UpdateRefusal refusal)
    {
        var vm = CreateRequirementsViewModel();
        RequirementsAnswer(refusal);

        await vm.RunAutomaticUpdateCheckAsync();

        AssertRefused(vm, refusal);
        await _updater.Received(1).CheckRequirementsAsync("9.9.9", Arg.Any<TimeSpan?>(), Arg.Any<CancellationToken>());
    }

    [Theory]
    [InlineData(UpdateRefusal.NeedsNetFramework48)]
    [InlineData(UpdateRefusal.CannotInstallFromThisCopy)]
    public async Task The_manual_check_says_why_and_takes_away_the_line_from_an_earlier_failure(UpdateRefusal refusal)
    {
        var vm = CreateRequirementsViewModel();
        await vm.RunAutomaticUpdateCheckAsync();
        _updater.PrepareAsync(default!, default, default).ReturnsForAnyArgs(
            Task.FromException<PreparedUpdate>(new UpdateFailedException(UpdateFailureStage.Download, "no")));
        await vm.UpdateNowCommand.ExecuteAsync(null);
        Assert.True(vm.HasUpdateFailure);
        RequirementsAnswer(refusal);

        await vm.CheckForUpdatesCommand.ExecuteAsync(null);

        AssertRefused(vm, refusal);
        Assert.Equal(string.Empty, vm.UpdateStatusText);
    }

    [Fact]
    public async Task A_read_that_fails_offers_Update_now_and_says_nothing()
    {
        var vm = CreateRequirementsViewModel();
        _updater.CheckRequirementsAsync("9.9.9", Arg.Any<TimeSpan?>(), Arg.Any<CancellationToken>()).Returns(
            Task.FromException<UpdateRefusal?>(new UpdateFailedException(UpdateFailureStage.Download, "no")));

        await vm.RunAutomaticUpdateCheckAsync();

        Assert.True(vm.HasNewerVersion);
        Assert.False(vm.IsUpdateRefused);
        Assert.True(vm.ShowsUpdateNow);
        Assert.False(vm.ShowsReleaseLink);
        Assert.Equal(Strings.UpdateNotice_Recommended, vm.UpdateNoticeDetailText);
        _dialogService.DidNotReceive().ShowWarning(Arg.Any<string>(), Arg.Any<string>());
    }

    /// <summary>
    /// The release's requirements read as the window closes: the view model is disposed during
    /// the read, which then ends cancelled, or where <paramref name="answers"/>, answers no refusal.
    /// </summary>
    private void RequirementsReadAsTheWindowCloses(ChromeViewModel vm, bool answers = false) =>
        _updater.CheckRequirementsAsync("9.9.9", Arg.Any<TimeSpan?>(), Arg.Any<CancellationToken>()).Returns(call =>
        {
            vm.Dispose();
            return answers
                ? Task.FromResult<UpdateRefusal?>(null)
                : Task.FromCanceled<UpdateRefusal?>(call.Arg<CancellationToken>());
        });

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task An_automatic_check_closed_while_its_requirements_are_read_shows_nothing(bool readAnswers)
    {
        var vm = CreateRequirementsViewModel();
        RequirementsReadAsTheWindowCloses(vm, readAnswers);

        await vm.RunAutomaticUpdateCheckAsync();

        await _updater.Received(1).CheckRequirementsAsync("9.9.9", Arg.Any<TimeSpan?>(), Arg.Any<CancellationToken>());
        Assert.False(vm.HasNewerVersion);
        Assert.False(vm.ShowsUpdateNow);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task A_manual_check_closed_while_its_requirements_are_read_shows_nothing(bool readAnswers)
    {
        var vm = CreateRequirementsViewModel();
        RequirementsReadAsTheWindowCloses(vm, readAnswers);

        await vm.CheckForUpdatesCommand.ExecuteAsync(null);

        await _updater.Received(1).CheckRequirementsAsync("9.9.9", Arg.Any<TimeSpan?>(), Arg.Any<CancellationToken>());
        Assert.False(vm.HasNewerVersion);
        Assert.False(vm.ShowsUpdateNow);
        _dialogService.DidNotReceive().ShowWarning(Arg.Any<string>(), Arg.Any<string>());
    }

    [Fact]
    public async Task A_cancellation_that_is_not_the_check_s_own_is_a_read_that_failed_and_keeps_a_refusal_already_read()
    {
        // A read that failed leaves a refusal already read for the version in place, where a
        // read answering no refusal would bring Update now back.
        var vm = CreateRequirementsViewModel();
        RequirementsAnswer(UpdateRefusal.CannotInstallFromThisCopy);
        await vm.RunAutomaticUpdateCheckAsync();
        AssertRefused(vm, UpdateRefusal.CannotInstallFromThisCopy);
        _updater.CheckRequirementsAsync("9.9.9", Arg.Any<TimeSpan?>(), Arg.Any<CancellationToken>()).Returns(
            Task.FromException<UpdateRefusal?>(new OperationCanceledException()));

        await vm.CheckForUpdatesCommand.ExecuteAsync(null);

        await _updater.Received(2).CheckRequirementsAsync("9.9.9", Arg.Any<TimeSpan?>(), Arg.Any<CancellationToken>());
        AssertRefused(vm, UpdateRefusal.CannotInstallFromThisCopy);
    }

    [Fact]
    public async Task A_read_is_given_its_bound_and_one_that_fails_offers_Update_now()
    {
        var vm = CreateRequirementsViewModel(requirementsBound: TimeSpan.FromMilliseconds(50));
        TimeSpan? given = null;
        _updater.CheckRequirementsAsync("9.9.9", Arg.Any<TimeSpan?>(), Arg.Any<CancellationToken>()).Returns(call =>
        {
            given = call.Arg<TimeSpan?>();
            return Task.FromException<UpdateRefusal?>(
                new UpdateFailedException(UpdateFailureStage.Download, "not read in the time given"));
        });

        await vm.RunAutomaticUpdateCheckAsync();

        Assert.NotNull(given);
        Assert.True(given <= TimeSpan.FromMilliseconds(50));
        Assert.True(vm.HasNewerVersion);
        Assert.True(vm.ShowsUpdateNow);
        Assert.False(vm.IsUpdateRefused);
    }

    [Fact]
    public async Task An_automatic_check_answering_after_its_window_reads_with_no_time_and_offers_Update_now()
    {
        var vm = AnsweringAfterItsWindow();
        // With no time and no file kept, the read is given up at once.
        _updater.CheckRequirementsAsync("9.9.9", TimeSpan.Zero, Arg.Any<CancellationToken>()).Returns(
            Task.FromException<UpdateRefusal?>(new UpdateFailedException(UpdateFailureStage.Download, "no time")));

        await vm.RunAutomaticUpdateCheckAsync();

        await _updater.Received(1).CheckRequirementsAsync("9.9.9", TimeSpan.Zero, Arg.Any<CancellationToken>());
        Assert.True(vm.HasNewerVersion);
        Assert.True(vm.ShowsUpdateNow);
    }

    [Fact]
    public async Task An_automatic_check_answering_after_its_window_shows_a_refusal_already_kept()
    {
        var vm = AnsweringAfterItsWindow();
        _updater.CheckRequirementsAsync("9.9.9", TimeSpan.Zero, Arg.Any<CancellationToken>())
            .Returns(UpdateRefusal.CannotInstallFromThisCopy);

        await vm.RunAutomaticUpdateCheckAsync();

        AssertRefused(vm, UpdateRefusal.CannotInstallFromThisCopy);
    }

    [Fact]
    public async Task An_infinite_bound_reads_within_the_automatic_checks_window()
    {
        var vm = CreateRequirementsViewModel(
            requirementsBound: Timeout.InfiniteTimeSpan, automaticCheckWindow: TimeSpan.FromSeconds(30));
        TimeSpan? given = null;
        _updater.CheckRequirementsAsync("9.9.9", Arg.Any<TimeSpan?>(), Arg.Any<CancellationToken>()).Returns(call =>
        {
            given = call.Arg<TimeSpan?>();
            return Task.FromResult<UpdateRefusal?>(null);
        });

        await vm.RunAutomaticUpdateCheckAsync();

        Assert.NotNull(given);
        // The window's time left, most of it with an answer at once: neither the infinite
        // bound nor a sliver.
        Assert.InRange(given!.Value, TimeSpan.FromSeconds(10), TimeSpan.FromSeconds(30));
    }

    /// <summary>A view model whose automatic check finds the newer version after its window has passed.</summary>
    private ChromeViewModel AnsweringAfterItsWindow()
    {
        var vm = CreateRequirementsViewModel(automaticCheckWindow: TimeSpan.FromMilliseconds(50));
        _updateCheckService.CheckAsync(Arg.Any<UpdateCheckOrigin>(), Arg.Any<CancellationToken>())
            .Returns(async _ =>
            {
                await Task.Delay(200);
                return (UpdateCheckResult)new UpdateAvailable("2.1.0", "9.9.9");
            });
        return vm;
    }

    [Fact]
    public async Task A_copy_that_cannot_tell_whether_its_setup_installed_it_says_so_and_reads_nothing()
    {
        var vm = CreateRequirementsViewModel(AppCopyKind.Undetermined);

        await vm.RunAutomaticUpdateCheckAsync();

        Assert.True(vm.HasNewerVersion);
        Assert.Equal(Strings.UpdateNotice_CopyCannotTell, vm.UpdateNoticeDetailText);
        Assert.False(vm.ShowsUpdateNow);
        Assert.True(vm.ShowsReleaseLink);
        await _updater.DidNotReceiveWithAnyArgs().CheckRequirementsAsync(default!, default, default);
    }

    [Theory]
    [InlineData(AppCopyKind.Scoop)]
    [InlineData(AppCopyKind.FromSource)]
    public async Task A_copy_with_no_download_reads_nothing_and_keeps_the_recommendation(AppCopyKind kind)
    {
        var vm = CreateRequirementsViewModel(kind);

        await vm.RunAutomaticUpdateCheckAsync();

        Assert.True(vm.HasNewerVersion);
        Assert.Equal(Strings.UpdateNotice_Recommended, vm.UpdateNoticeDetailText);
        await _updater.DidNotReceiveWithAnyArgs().CheckRequirementsAsync(default!, default, default);
    }

    [Fact]
    public async Task Update_now_ruled_out_by_its_own_read_says_why_on_the_card_and_writes_no_failure()
    {
        var vm = CreateRequirementsViewModel();
        await vm.RunAutomaticUpdateCheckAsync();
        vm.OfferUpdate();
        _updater.PrepareAsync(default!, default, default).ReturnsForAnyArgs(Task.FromException<PreparedUpdate>(
            new UpdateRefusedException(UpdateRefusal.CannotInstallFromThisCopy, "9.9.9")));
        var raised = new List<string?>();
        vm.PropertyChanged += (_, e) => raised.Add(e.PropertyName);

        await vm.UpdateNowCommand.ExecuteAsync(null);

        AssertRefused(vm, UpdateRefusal.CannotInstallFromThisCopy);
        Assert.False(vm.IsUpdating);
        Assert.True(vm.IsOfferingUpdate);
        Assert.Contains(nameof(ChromeViewModel.UpdateNoticeDetailText), raised);
        Assert.Contains(nameof(ChromeViewModel.ShowsUpdateNow), raised);
        Assert.Contains(nameof(ChromeViewModel.ShowsReleaseLink), raised);
        await _windowService.DidNotReceiveWithAnyArgs().StartUpdateAndCloseAsync(default!);
    }

    [Fact]
    public async Task The_NET_Framework_link_downloads_Microsofts_installer()
    {
        var vm = CreateRequirementsViewModel();
        RequirementsAnswer(UpdateRefusal.NeedsNetFramework48);
        await vm.RunAutomaticUpdateCheckAsync();

        vm.OpenNetFrameworkDownloadCommand.Execute(null);

        _windowService.Received(1).OpenUrl("https://go.microsoft.com/fwlink/?linkid=2088631");
        _windowService.DidNotReceive().OpenUrl(ReleasesPage);
    }

    [Fact]
    public async Task The_NET_Framework_link_waits_for_that_reason_and_shows_for_no_other()
    {
        var vm = CreateRequirementsViewModel();
        Assert.False(vm.OpenNetFrameworkDownloadCommand.CanExecute(null));
        vm.OpenNetFrameworkDownloadCommand.Execute(null);
        RequirementsAnswer(UpdateRefusal.CannotInstallFromThisCopy);
        await vm.RunAutomaticUpdateCheckAsync();

        Assert.False(vm.ShowsNetFrameworkLink);
        Assert.False(vm.OpenNetFrameworkDownloadCommand.CanExecute(null));
        vm.OpenNetFrameworkDownloadCommand.Execute(null);
        _windowService.DidNotReceive().OpenUrl(Arg.Any<string>());
    }

    [Fact]
    public async Task A_later_check_whose_read_fails_keeps_the_refusal_already_read_for_that_version()
    {
        var vm = CreateRequirementsViewModel();
        RequirementsAnswer(UpdateRefusal.NeedsNetFramework48);
        await vm.RunAutomaticUpdateCheckAsync();
        _updater.CheckRequirementsAsync("9.9.9", Arg.Any<TimeSpan?>(), Arg.Any<CancellationToken>()).Returns(
            Task.FromException<UpdateRefusal?>(new UpdateFailedException(UpdateFailureStage.Download, "no")));

        await vm.CheckForUpdatesCommand.ExecuteAsync(null);

        AssertRefused(vm, UpdateRefusal.NeedsNetFramework48);
    }

    [Fact]
    public async Task A_later_check_finding_another_version_whose_read_fails_offers_Update_now()
    {
        var vm = CreateRequirementsViewModel();
        RequirementsAnswer(UpdateRefusal.NeedsNetFramework48);
        await vm.RunAutomaticUpdateCheckAsync();
        CheckAnswers(new UpdateAvailable("2.1.0", "9.9.10"));
        _updater.CheckRequirementsAsync("9.9.10", Arg.Any<TimeSpan?>(), Arg.Any<CancellationToken>()).Returns(
            Task.FromException<UpdateRefusal?>(new UpdateFailedException(UpdateFailureStage.Download, "no")));

        await vm.CheckForUpdatesCommand.ExecuteAsync(null);

        Assert.False(vm.IsUpdateRefused);
        Assert.True(vm.ShowsUpdateNow);
    }

    [Fact]
    public async Task A_manual_check_finding_this_build_current_takes_the_refusal_down()
    {
        var vm = CreateRequirementsViewModel();
        RequirementsAnswer(UpdateRefusal.NeedsNetFramework48);
        await vm.RunAutomaticUpdateCheckAsync();
        CheckAnswers(new UpToDate("2.1.0"));

        await vm.CheckForUpdatesCommand.ExecuteAsync(null);

        Assert.False(vm.HasNewerVersion);
        Assert.False(vm.IsUpdateRefused);
        Assert.False(vm.ShowsNetFrameworkLink);
        Assert.Equal(Strings.UpdateNotice_Recommended, vm.UpdateNoticeDetailText);
    }

    // ---- Scoop's copy ----

    /// <summary>
    /// A view model on a copy installed by Scoop, answering as the real updater answers
    /// for that kind, with a check that has found 9.9.9 and the time Copy reads "Copied"
    /// set to <paramref name="copiedFor"/>.
    /// </summary>
    private async Task<ChromeViewModel> CreateScoopViewModelAsync(TimeSpan copiedFor)
    {
        _updater.Kind.Returns(AppCopyKind.Scoop);
        _settingsService.Load().Returns(new AppSettings { AutoUpdateCheck = true });
        CheckAnswers(new UpdateAvailable("2.1.0", "9.9.9"));
        var scan = new ScanViewModel(_scanService, _rebootService, _dialogService);
        var vm = new ChromeViewModel(
            _windowService, _msiInfoService, _settingsService,
            _updateCheckService, _dialogService, scan,
            checkCooldownOverride: TimeSpan.Zero,
            updater: _updater,
            scoopUpdateCopiedForOverride: copiedFor);
        await vm.RunAutomaticUpdateCheckAsync();
        return vm;
    }

    [Fact]
    public async Task A_copy_installed_by_Scoop_shows_Scoops_command_and_the_link_and_not_Update_now()
    {
        var vm = await CreateScoopViewModelAsync(TimeSpan.Zero);

        Assert.True(vm.ShowsScoopCommand);
        Assert.Equal("scoop update installerclean", vm.ScoopUpdateCommand);
        Assert.False(vm.ShowsUpdateNow);
        Assert.False(vm.UpdateNowCommand.CanExecute(null));
        Assert.True(vm.ShowsReleaseLink);
        Assert.True(vm.OpenReleasePageCommand.CanExecute(null));
        Assert.Equal(Strings.Action_Copy, vm.CopyScoopUpdateText);
        Assert.False(vm.HasScoopUpdateCopyFailed);
    }

    [Fact]
    public async Task A_copy_installed_by_Scoop_for_all_users_says_so_and_gives_the_global_command()
    {
        _updater.ScoopUpdate.Returns(new ScoopUpdate("scoop update installerclean --global", Global: true));
        _windowService.CopyToClipboard(default!).ReturnsForAnyArgs(true);
        var vm = await CreateScoopViewModelAsync(TimeSpan.Zero);

        Assert.Equal("scoop update installerclean --global", vm.ScoopUpdateCommand);
        Assert.Equal(Strings.UpdateNotice_ScoopGlobal, vm.ScoopUpdateLine);
        Assert.Equal(
            DisplayHelpers.SpokenSentences(Strings.UpdateNotice_ScoopGlobal, "scoop update installerclean --global"),
            vm.ScoopUpdateSpoken);

        await vm.CopyScoopUpdateCommand.ExecuteAsync(null);

        _windowService.Received(1).CopyToClipboard("scoop update installerclean --global");
    }

    [Fact]
    public async Task A_copy_installed_by_Scoop_for_one_user_keeps_the_plain_line_and_gives_the_command_for_its_folder()
    {
        _updater.ScoopUpdate.Returns(new ScoopUpdate("scoop update installerclean-test", Global: false));
        _windowService.CopyToClipboard(default!).ReturnsForAnyArgs(true);
        var vm = await CreateScoopViewModelAsync(TimeSpan.Zero);

        Assert.Equal("scoop update installerclean-test", vm.ScoopUpdateCommand);
        Assert.Equal(Strings.UpdateNotice_Scoop, vm.ScoopUpdateLine);

        await vm.CopyScoopUpdateCommand.ExecuteAsync(null);

        _windowService.Received(1).CopyToClipboard("scoop update installerclean-test");
    }

    [Fact]
    public async Task A_Scoop_copy_whose_folder_names_no_app_gives_the_plain_command()
    {
        var vm = await CreateScoopViewModelAsync(TimeSpan.Zero);

        Assert.Null(_updater.ScoopUpdate);
        Assert.Equal(AppCopy.ScoopUpdateCommand, vm.ScoopUpdateCommand);
        Assert.Equal(Strings.UpdateNotice_Scoop, vm.ScoopUpdateLine);
    }

    [Fact]
    public async Task Scoops_command_waits_for_a_newer_version_and_shows_on_no_other_copy()
    {
        _updater.Kind.Returns(AppCopyKind.Scoop);
        CheckAnswers(new UpdateAvailable("2.1.0", "9.9.9"));
        var scoop = new ChromeViewModel(
            _windowService, _msiInfoService, _settingsService, _updateCheckService, _dialogService,
            new ScanViewModel(_scanService, _rebootService, _dialogService), updater: _updater);
        var raised = new List<string?>();
        scoop.PropertyChanged += (_, e) => raised.Add(e.PropertyName);

        Assert.False(scoop.ShowsScoopCommand);
        _settingsService.Load().Returns(new AppSettings { AutoUpdateCheck = true });
        await scoop.RunAutomaticUpdateCheckAsync();
        Assert.True(scoop.ShowsScoopCommand);
        Assert.Contains(nameof(ChromeViewModel.ShowsScoopCommand), raised);

        _updater.Kind.Returns(AppCopyKind.Portable);
        var portable = await CreateUpdatingViewModelAsync();
        Assert.True(portable.HasNewerVersion);
        Assert.False(portable.ShowsScoopCommand);

        var fromSource = CreateViewModel();
        await fromSource.RunAutomaticUpdateCheckAsync();
        Assert.True(fromSource.HasNewerVersion);
        Assert.False(fromSource.ShowsScoopCommand);
    }

    [Fact]
    public async Task Copy_puts_the_command_on_the_clipboard_and_reads_Copied_until_the_time_is_up()
    {
        var vm = await CreateScoopViewModelAsync(TimeSpan.Zero);
        _windowService.CopyToClipboard(default!).ReturnsForAnyArgs(true);
        var texts = new List<string>();
        vm.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName == nameof(ChromeViewModel.CopyScoopUpdateText))
                texts.Add(vm.CopyScoopUpdateText);
        };
        var answers = new List<bool>();
        vm.ScoopUpdateCopyAnswered += (_, copied) => answers.Add(copied);

        await vm.CopyScoopUpdateCommand.ExecuteAsync(null);

        _windowService.Received(1).CopyToClipboard("scoop update installerclean");
        Assert.Equal([Strings.UpdateNotice_CommandCopied, Strings.Action_Copy], texts);
        Assert.False(vm.IsScoopUpdateCopied);
        Assert.False(vm.HasScoopUpdateCopyFailed);
        Assert.Equal([true], answers);
    }

    [Fact]
    public async Task A_second_press_while_Copy_reads_Copied_starts_the_time_again()
    {
        var vm = await CreateScoopViewModelAsync(TimeSpan.FromMinutes(5));
        _windowService.CopyToClipboard(default!).ReturnsForAnyArgs(true);
        var answers = new List<bool>();
        vm.ScoopUpdateCopyAnswered += (_, copied) => answers.Add(copied);

        var first = vm.CopyScoopUpdateCommand.ExecuteAsync(null);
        Assert.True(vm.CopyScoopUpdateCommand.CanExecute(null));
        var second = vm.CopyScoopUpdateCommand.ExecuteAsync(null);
        await first;

        // The first press's time ended with the second press, and left the button reading
        // "Copied" for the second press's time.
        Assert.True(vm.IsScoopUpdateCopied);
        Assert.Equal(Strings.UpdateNotice_CommandCopied, vm.CopyScoopUpdateText);
        Assert.False(second.IsCompleted);
        Assert.Equal([true, true], answers);

        vm.Dispose();
        await second;
    }

    [Fact]
    public async Task Continue_without_updating_clears_Copied_and_ends_its_time()
    {
        var vm = await CreateScoopViewModelAsync(TimeSpan.FromMinutes(5));
        _windowService.CopyToClipboard(default!).ReturnsForAnyArgs(true);
        var pressed = vm.CopyScoopUpdateCommand.ExecuteAsync(null);
        Assert.True(vm.IsScoopUpdateCopied);
        vm.OfferUpdate();

        vm.ContinueWithoutUpdatingCommand.Execute(null);

        Assert.False(vm.IsScoopUpdateCopied);
        Assert.Equal(Strings.Action_Copy, vm.CopyScoopUpdateText);
        await pressed.WaitAsync(TimeSpan.FromSeconds(5));
        Assert.False(vm.IsScoopUpdateCopied);
    }

    [Fact]
    public async Task Continue_without_updating_clears_the_line_saying_the_copy_failed()
    {
        var vm = await CreateScoopViewModelAsync(TimeSpan.Zero);
        _windowService.CopyToClipboard(default!).ReturnsForAnyArgs(false);
        await vm.CopyScoopUpdateCommand.ExecuteAsync(null);
        Assert.True(vm.HasScoopUpdateCopyFailed);
        vm.OfferUpdate();

        vm.ContinueWithoutUpdatingCommand.Execute(null);

        Assert.False(vm.HasScoopUpdateCopyFailed);
    }

    [Fact]
    public async Task A_check_that_finds_this_build_current_clears_Copied_and_ends_its_time()
    {
        var vm = await CreateScoopViewModelAsync(TimeSpan.FromMinutes(5));
        _windowService.CopyToClipboard(default!).ReturnsForAnyArgs(true);
        var pressed = vm.CopyScoopUpdateCommand.ExecuteAsync(null);
        Assert.True(vm.IsScoopUpdateCopied);
        CheckAnswers(new UpToDate("2.1.0"));

        await vm.CheckForUpdatesCommand.ExecuteAsync(null);

        Assert.False(vm.HasNewerVersion);
        Assert.False(vm.IsScoopUpdateCopied);
        await pressed.WaitAsync(TimeSpan.FromSeconds(5));
        Assert.False(vm.IsScoopUpdateCopied);
    }

    [Fact]
    public async Task A_newer_version_found_after_one_was_taken_back_clears_Copied()
    {
        var vm = await CreateScoopViewModelAsync(TimeSpan.FromMinutes(5));
        CheckAnswers(new UpToDate("2.1.0"), new UpdateAvailable("2.1.0", "9.9.9"));
        await vm.CheckForUpdatesCommand.ExecuteAsync(null);
        Assert.False(vm.HasNewerVersion);
        _windowService.CopyToClipboard(default!).ReturnsForAnyArgs(true);
        var pressed = vm.CopyScoopUpdateCommand.ExecuteAsync(null);
        Assert.True(vm.IsScoopUpdateCopied);

        await vm.CheckForUpdatesCommand.ExecuteAsync(null);

        Assert.True(vm.HasNewerVersion);
        Assert.False(vm.IsScoopUpdateCopied);
        await pressed.WaitAsync(TimeSpan.FromSeconds(5));
    }

    [Fact]
    public async Task A_newer_version_found_after_one_was_taken_back_clears_the_line_saying_the_copy_failed()
    {
        var vm = await CreateScoopViewModelAsync(TimeSpan.Zero);
        _windowService.CopyToClipboard(default!).ReturnsForAnyArgs(false);
        await vm.CopyScoopUpdateCommand.ExecuteAsync(null);
        Assert.True(vm.HasScoopUpdateCopyFailed);
        CheckAnswers(new UpToDate("2.1.0"), new UpdateAvailable("2.1.0", "9.9.9"));
        await vm.CheckForUpdatesCommand.ExecuteAsync(null);
        Assert.False(vm.HasNewerVersion);
        await vm.CopyScoopUpdateCommand.ExecuteAsync(null);
        Assert.True(vm.HasScoopUpdateCopyFailed);

        await vm.CheckForUpdatesCommand.ExecuteAsync(null);

        Assert.True(vm.HasNewerVersion);
        Assert.False(vm.HasScoopUpdateCopyFailed);
    }

    [Fact]
    public async Task A_copy_the_clipboard_refuses_keeps_Copy_and_shows_the_line_until_a_copy_succeeds()
    {
        var vm = await CreateScoopViewModelAsync(TimeSpan.Zero);
        _windowService.CopyToClipboard(default!).ReturnsForAnyArgs(false, true);
        var answers = new List<bool>();
        vm.ScoopUpdateCopyAnswered += (_, copied) => answers.Add(copied);

        await vm.CopyScoopUpdateCommand.ExecuteAsync(null);

        Assert.True(vm.HasScoopUpdateCopyFailed);
        Assert.False(vm.IsScoopUpdateCopied);
        Assert.Equal(Strings.Action_Copy, vm.CopyScoopUpdateText);

        await vm.CopyScoopUpdateCommand.ExecuteAsync(null);

        Assert.False(vm.HasScoopUpdateCopyFailed);
        Assert.Equal([false, true], answers);
    }
}
