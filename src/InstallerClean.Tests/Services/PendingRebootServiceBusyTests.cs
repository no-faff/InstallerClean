using InstallerClean.Services;
using NSubstitute;

namespace InstallerClean.Tests.Services;

/// <summary>
/// <see cref="PendingRebootService.CheckBusy"/>: the three signals an installation sets,
/// then the Windows Installer service's state, and never the queued file operations;
/// and <see cref="PendingRebootService.Check"/> never reading the service.
/// </summary>
public class PendingRebootServiceBusyTests
{
    private readonly IRegistryReader _registry = Substitute.For<IRegistryReader>();
    private readonly IMutexProbe _mutex = Substitute.For<IMutexProbe>();
    private readonly IVolumeMountProbe _volumes = Substitute.For<IVolumeMountProbe>();
    private readonly IInstallerInProgressMarker _marker = Substitute.For<IInstallerInProgressMarker>();
    private readonly IInstallerServiceState _service = Substitute.For<IInstallerServiceState>();

    /// <summary>
    /// The quiet machine every test starts from, each reading stated: the mutex not held,
    /// no suspended transaction, no in-progress file, nothing queued and the service
    /// stopped. Every one of those fakes answers a blocking reading when nobody sets it.
    /// </summary>
    public PendingRebootServiceBusyTests()
    {
        _mutex.Sample(Arg.Any<string>()).Returns(MutexSample.NotHeld);
        _registry.LocalMachineKeyPresence(PendingRebootService.InstallerInProgressKey)
            .Returns(RegistryKeyPresence.Absent);
        _marker.Read().Returns(InstallerInProgressMarkerReading.Absent);
        Queued(new RegistryMultiStringRead(RegistryMultiStringState.Absent));
        Service(InstallerServiceState.Stopped);
    }

    private PendingRebootService Build() =>
        new(_registry, _mutex, _volumes, _marker, @"C:\Windows", _service);

    private void Service(InstallerServiceState state, int? windowsError = null) =>
        _service.Read().Returns(new InstallerServiceRead(state, windowsError));

    private void Queued(RegistryMultiStringRead read) =>
        _registry.LocalMachineMultiStringValue(
                PendingRebootService.SessionManagerKey,
                PendingRebootService.PendingFileRenameOperationsValue)
            .Returns(read);

    [Fact]
    public void A_quiet_machine_with_the_service_stopped_is_not_busy()
    {
        Assert.False(Build().CheckBusy().IsBlocked);
    }

    [Fact]
    public void A_service_that_is_not_stopped_is_busy_with_a_reason_of_its_own()
    {
        Service(InstallerServiceState.NotStopped);

        var result = Build().CheckBusy();

        Assert.Equal(PendingRebootReason.InstallerServiceRunning, result.Reason);
    }

    [Fact]
    public void A_service_reading_nobody_set_is_busy()
    {
        _service.Read().Returns(default(InstallerServiceRead));

        Assert.Equal(PendingRebootReason.InstallerServiceRunning, Build().CheckBusy().Reason);
    }

    [Fact]
    public void A_pc_with_no_such_service_is_not_busy()
    {
        Service(InstallerServiceState.NotThere);

        Assert.False(Build().CheckBusy().IsBlocked);
    }

    [Fact]
    public void A_service_whose_state_will_not_read_blocks_with_the_error_it_failed_on()
    {
        Service(InstallerServiceState.Unreadable, windowsError: 5);

        var result = Build().CheckBusy();

        Assert.Equal(PendingRebootReason.InstallerServiceUnreadable, result.Reason);
        Assert.Equal(5, result.WindowsError);
    }

    /// <summary>
    /// Each of an installation's own signals is given before the service's, so the
    /// service is the reason only for the minutes after an installation has finished.
    /// </summary>
    [Fact]
    public void A_held_mutex_is_the_reason_given_while_the_service_runs_and_the_service_is_not_read()
    {
        _mutex.Sample(PendingRebootService.MsiExecuteMutexName).Returns(MutexSample.Held);
        Service(InstallerServiceState.NotStopped);

        Assert.Equal(PendingRebootReason.MsiExecuteMutexHeld, Build().CheckBusy().Reason);
        _service.DidNotReceive().Read();
    }

    [Fact]
    public void A_suspended_transaction_and_the_in_progress_file_are_each_given_before_the_service()
    {
        Service(InstallerServiceState.NotStopped);

        _marker.Read().Returns(InstallerInProgressMarkerReading.Present);
        Assert.Equal(PendingRebootReason.InstallerInProgressMarker, Build().CheckBusy().Reason);

        _registry.LocalMachineKeyPresence(PendingRebootService.InstallerInProgressKey)
            .Returns(RegistryKeyPresence.Present);
        Assert.Equal(PendingRebootReason.InstallerInProgress, Build().CheckBusy().Reason);
    }

    [Fact]
    public void A_file_operation_queued_in_the_cache_is_not_read_and_does_not_make_the_machine_busy()
    {
        Queued(new RegistryMultiStringRead(
            RegistryMultiStringState.Read, [@"\??\C:\Windows\Installer\1a2b3c.msi", string.Empty]));

        Assert.False(Build().CheckBusy().IsBlocked);
        _registry.DidNotReceive().LocalMachineMultiStringValue(Arg.Any<string>(), Arg.Any<string>());
        Assert.Equal(PendingRebootReason.PendingRenameInCache, Build().Check().Reason);
    }

    [Fact]
    public void A_mutex_that_was_not_sampled_blocks_where_nothing_else_does_and_gives_way_to_a_running_service()
    {
        _mutex.Sample(PendingRebootService.MsiExecuteMutexName)
            .Returns(new MutexSampling(MutexSample.Unestablished, 6));

        var quiet = Build().CheckBusy();
        Assert.Equal(PendingRebootReason.MsiExecuteMutexUnreadable, quiet.Reason);
        Assert.Equal(6, quiet.WindowsError);

        Service(InstallerServiceState.NotStopped);
        Assert.Equal(PendingRebootReason.InstallerServiceRunning, Build().CheckBusy().Reason);
    }

    /// <summary>
    /// The scan's own check does not read the service: the service runs on for minutes
    /// after every installation, and a scan made in those minutes reads clean.
    /// </summary>
    [Fact]
    public void The_scans_check_reads_clean_while_the_service_runs_and_never_reads_it()
    {
        Service(InstallerServiceState.NotStopped);

        Assert.False(Build().Check().IsBlocked);
        _service.DidNotReceive().Read();
    }

    [Fact]
    public void Exactly_the_four_reasons_an_installation_ending_clears_are_worth_waiting_for()
    {
        var clears = Enum.GetValues<PendingRebootReason>().Where(r => r.ClearsByWaiting()).ToList();

        Assert.Equal(
            [
                PendingRebootReason.MsiExecuteMutexHeld,
                PendingRebootReason.InstallerInProgress,
                PendingRebootReason.InstallerInProgressMarker,
                PendingRebootReason.InstallerServiceRunning,
            ],
            clears);
    }
}
