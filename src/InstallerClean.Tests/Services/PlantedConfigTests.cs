using Microsoft.Win32;
using NSubstitute;
using InstallerClean.Services;

namespace InstallerClean.Tests.Services;

/// <summary>
/// Whether a copy refuses to start because a .exe.config sits beside it that it did not
/// ship. The setup's copy ships one and is not refused; a download ships none, so a config
/// beside it is foreign.
/// </summary>
public class PlantedConfigTests
{
    private const string SetupFolder = @"C:\Program Files\InstallerClean";
    private const string DownloadFolder = @"C:\Users\person\Downloads";
    private const string ConfigBesideDownload = @"C:\Users\person\Downloads\InstallerClean.exe.config";
    private const string ConfigBesideSetup = @"C:\Program Files\InstallerClean\InstallerClean.exe.config";

    // The setup's record as AppCopy.ReadSetupRecord reads a key holding installLocation, or
    // an absent key where it is null.
    private static SetupRecord RecordOf(string? installLocation) =>
        AppCopy.ReadSetupRecord(Registry(installLocation is null
            ? new RegistryKeyValues(RegistryKeyPresence.Absent)
            : new RegistryKeyValues(RegistryKeyPresence.Present,
                [new RegistryValue("InstallLocation", RegistryValueKind.String, installLocation)])));

    private static IRegistryReader Registry(RegistryKeyValues read)
    {
        var registry = Substitute.For<IRegistryReader>();
        registry.LocalMachineValues(AppCopy.SetupUninstallKeyPath, Arg.Any<CancellationToken>()).Returns(read);
        return registry;
    }

    [Fact]
    public void A_copy_with_no_config_beside_it_starts()
    {
        Assert.False(PlantedConfig.RefuseToStart(null, configExists: true, DownloadFolder, RecordOf(SetupFolder)));
    }

    [Fact]
    public void A_config_the_runtime_names_but_the_disk_does_not_hold_lets_the_copy_start()
    {
        Assert.False(PlantedConfig.RefuseToStart(ConfigBesideDownload, configExists: false, DownloadFolder, RecordOf(SetupFolder)));
    }

    [Fact]
    public void A_config_beside_a_download_refuses_the_start()
    {
        Assert.True(PlantedConfig.RefuseToStart(ConfigBesideDownload, configExists: true, DownloadFolder, RecordOf(SetupFolder)));
    }

    [Fact]
    public void A_config_beside_the_setup_copy_lets_it_start()
    {
        Assert.False(PlantedConfig.RefuseToStart(ConfigBesideSetup, configExists: true, SetupFolder, RecordOf(SetupFolder)));
    }

    [Fact]
    public void A_config_beside_a_copy_where_no_setup_is_recorded_refuses_the_start()
    {
        Assert.True(PlantedConfig.RefuseToStart(ConfigBesideDownload, configExists: true, DownloadFolder, RecordOf(null)));
    }
}
