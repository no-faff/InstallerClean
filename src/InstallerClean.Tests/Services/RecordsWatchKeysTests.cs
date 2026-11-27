using InstallerClean.Services;
using Microsoft.Win32;
using WatchedKey = InstallerClean.Services.RecordsWatchFactory.WatchedKey;

namespace InstallerClean.Tests.Services;

/// <summary>
/// The list of registry keys the app's watch is armed on
/// (<see cref="RecordsWatchFactory.RecordKeys"/>), which reads no registry: the two
/// machine keys, and two for each hive loaded under HKEY_USERS.
/// </summary>
public class RecordsWatchKeysTests
{
    [Fact]
    public void The_machine_keys_come_first_each_with_its_subtree_and_the_key_above_it_in_its_place()
    {
        var keys = RecordsWatchFactory.RecordKeys([]).ToList();

        Assert.Equal(
            [
                new WatchedKey(RegistryHive.LocalMachine,
                    @"SOFTWARE\Microsoft\Windows\CurrentVersion\Installer", Subtree: true, ParentWhereAbsent: true),
                new WatchedKey(RegistryHive.LocalMachine,
                    @"SOFTWARE\Classes\Installer", Subtree: true, ParentWhereAbsent: true),
            ],
            keys);
    }

    [Fact]
    public void Each_loaded_hive_gives_its_Microsoft_key_for_names_and_then_its_Installer_key_with_its_subtree()
    {
        var keys = RecordsWatchFactory.RecordKeys(["S-1-5-18", "S-1-5-21-1-2-3-1001"]).Skip(2).ToList();

        Assert.Equal(
            [
                new WatchedKey(RegistryHive.Users,
                    @"S-1-5-18\Software\Microsoft", Subtree: false, ParentWhereAbsent: false),
                new WatchedKey(RegistryHive.Users,
                    @"S-1-5-18\Software\Microsoft\Installer", Subtree: true, ParentWhereAbsent: false),
                new WatchedKey(RegistryHive.Users,
                    @"S-1-5-21-1-2-3-1001\Software\Microsoft", Subtree: false, ParentWhereAbsent: false),
                new WatchedKey(RegistryHive.Users,
                    @"S-1-5-21-1-2-3-1001\Software\Microsoft\Installer", Subtree: true, ParentWhereAbsent: false),
            ],
            keys);
    }

    [Fact]
    public void The_null_factory_arms_a_watch_that_reads_unchanged()
    {
        using var watch = NullRecordsWatchFactory.Instance.Arm();

        Assert.Equal(RecordsWatchReading.Unchanged, watch.Read());
    }
}
