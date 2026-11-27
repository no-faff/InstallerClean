using InstallerClean.Services;

namespace InstallerClean.Tests.Services.Integration;

/// <summary>
/// The real <see cref="InstallerServiceStateProbe"/> against the service control manager
/// of the PC running the test. Whether Windows Installer happens to be running there is
/// not this test's to say, so the real service is held only to answering one of its two
/// states.
/// </summary>
public class InstallerServiceStateProbeTests
{
    [Fact]
    public void The_windows_installer_service_reads_as_stopped_or_not_at_every_read()
    {
        var probe = new InstallerServiceStateProbe();

        for (var read = 0; read < 3; read++)
        {
            var answer = probe.Read();
            Assert.True(answer.State is InstallerServiceState.Stopped or InstallerServiceState.NotStopped,
                $"{answer.State}, error {answer.WindowsError}");
        }
    }

    [Fact]
    public void A_service_windows_does_not_have_reads_as_not_there()
    {
        var probe = new InstallerServiceStateProbe("ic-test-no-such-service-" + Guid.NewGuid().ToString("N"));

        Assert.Equal(new InstallerServiceRead(InstallerServiceState.NotThere), probe.Read());
    }
}
