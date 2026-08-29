using InstallerClean.Services;
using NSubstitute;

namespace InstallerClean.Tests.Services;

/// <summary>Unit tests for PendingRebootService: each branch of Check() exercised against fakes.</summary>
public class PendingRebootServiceUnitTests
{
    private readonly IRegistryReader _registry = Substitute.For<IRegistryReader>();
    private readonly IMutexProbe _mutex = Substitute.For<IMutexProbe>();
    private readonly IVolumeMountProbe _volumes = Substitute.For<IVolumeMountProbe>();

    /// <summary>Builds a service with a fixed Windows root so path comparisons don't depend on the host.</summary>
    private PendingRebootService Build(string windowsRoot = @"C:\Windows") =>
        new(_registry, _mutex, _volumes, windowsRoot);

    /// <summary>
    /// Scripts one volume: the GUID name the enumeration hands back, the NT device it
    /// is a link to, and where it is mounted. The device name is passed trimmed the way
    /// the service trims it, so a change to that trimming shows up here rather than
    /// leaving every volume silently unmatched.
    /// </summary>
    private void Volume(string guid, string device, params string[] mountedAt)
    {
        var guidPath = $@"\\?\Volume{{{guid}}}\";
        _volumes.VolumeGuidPaths().Returns(new[] { guidPath });
        _volumes.DosDeviceTarget($"Volume{{{guid}}}").Returns(device);
        _volumes.MountPointsFor(guidPath).Returns(VolumeMountPoints.Answer(mountedAt));
    }

    /// <summary>The value Session Manager holds, as the registry hands it over.</summary>
    private void Queued(params string[] entries) =>
        _registry.LocalMachineMultiStringValue(
                PendingRebootService.SessionManagerKey,
                PendingRebootService.PendingFileRenameOperationsValue)
            .Returns(entries);

    // Positive: must Block.

    [Fact]
    public void Mutex_held_blocks()
    {
        _mutex.IsHeld(PendingRebootService.MsiExecuteMutexName).Returns(true);

        var result = Build().Check();

        Assert.Equal(PendingRebootVerdict.Block, result.Verdict);
        Assert.Equal(PendingRebootReason.MsiExecuteMutexHeld, result.Reason);
    }

    [Fact]
    public void InProgress_key_exists_blocks()
    {
        _registry.LocalMachineKeyExists(
                PendingRebootService.InstallerInProgressKey)
            .Returns(true);

        var result = Build().Check();

        Assert.Equal(PendingRebootVerdict.Block, result.Verdict);
        Assert.Equal(PendingRebootReason.InstallerInProgress, result.Reason);
    }

    [Fact]
    public void Rename_targets_cache_blocks()
    {
        _registry.LocalMachineMultiStringValue(
                PendingRebootService.SessionManagerKey,
                PendingRebootService.PendingFileRenameOperationsValue)
            .Returns(new[] { @"\??\C:\Windows\Installer\1234.msi", "" });

        var result = Build().Check();

        Assert.Equal(PendingRebootVerdict.Block, result.Verdict);
        Assert.Equal(PendingRebootReason.PendingRenameInCache, result.Reason);
        Assert.Equal(@"C:\Windows\Installer\1234.msi", result.Detail);
    }

    [Fact]
    public void Multiple_renames_blocks_on_first_cache_match()
    {
        _registry.LocalMachineMultiStringValue(
                PendingRebootService.SessionManagerKey,
                PendingRebootService.PendingFileRenameOperationsValue)
            .Returns(new[]
            {
                @"\??\C:\Users\foo.tmp", "",
                @"\??\C:\Windows\Installer\1234.msp", "",
            });

        var result = Build().Check();

        Assert.Equal(PendingRebootVerdict.Block, result.Verdict);
        Assert.Equal(PendingRebootReason.PendingRenameInCache, result.Reason);
        Assert.Equal(@"C:\Windows\Installer\1234.msp", result.Detail);
    }

    [Fact]
    public void Long_path_prefix_still_matches()
    {
        _registry.LocalMachineMultiStringValue(
                PendingRebootService.SessionManagerKey,
                PendingRebootService.PendingFileRenameOperationsValue)
            .Returns(new[] { @"\\?\C:\Windows\Installer\1234.msi", "" });

        var result = Build().Check();

        Assert.Equal(PendingRebootVerdict.Block, result.Verdict);
        Assert.Equal(PendingRebootReason.PendingRenameInCache, result.Reason);
        Assert.Equal(@"C:\Windows\Installer\1234.msi", result.Detail);
    }

    [Fact]
    public void Replace_existing_destination_prefix_still_matches()
    {
        // A destination queued with MOVEFILE_REPLACE_EXISTING carries a
        // leading '!' before the NT prefix; the entry still targets the
        // cache and must block.
        _registry.LocalMachineMultiStringValue(
                PendingRebootService.SessionManagerKey,
                PendingRebootService.PendingFileRenameOperationsValue)
            .Returns(new[] { @"\??\C:\Users\elsewhere.tmp", @"!\??\C:\Windows\Installer\1234.msi" });

        var result = Build().Check();

        Assert.Equal(PendingRebootVerdict.Block, result.Verdict);
        Assert.Equal(PendingRebootReason.PendingRenameInCache, result.Reason);
        Assert.Equal(@"C:\Windows\Installer\1234.msi", result.Detail);
    }

    [Fact]
    public void Rename_targets_per_product_folder_blocks()
    {
        _registry.LocalMachineMultiStringValue(
                PendingRebootService.SessionManagerKey,
                PendingRebootService.PendingFileRenameOperationsValue)
            .Returns(new[]
            {
                @"\??\C:\Windows\Installer\{12345678-1234-1234-1234-123456789abc}\foo.dll", "",
            });

        var result = Build().Check();

        Assert.Equal(PendingRebootVerdict.Block, result.Verdict);
        Assert.Equal(PendingRebootReason.PendingRenameInCache, result.Reason);
    }

    // Negative: must Clean.

    [Fact]
    public void All_signals_clean_returns_clean()
    {
        var result = Build().Check();

        Assert.Equal(PendingRebootVerdict.Clean, result.Verdict);
        Assert.Null(result.Reason);
    }

    /// <summary>Pin: the legacy broad signals are never queried by Check(). Even with a fake forcing them true, the verdict is Clean.</summary>
    [Theory]
    [InlineData(@"SOFTWARE\Microsoft\Windows\CurrentVersion\WindowsUpdate\Auto Update\RebootRequired")]
    [InlineData(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Component Based Servicing\RebootPending")]
    [InlineData(@"SOFTWARE\Microsoft\Windows\CurrentVersion\WindowsUpdate\Auto Update\PostRebootReporting")]
    public void Legacy_broad_signals_are_never_queried(string oldKey)
    {
        _registry.LocalMachineKeyExists(oldKey).Returns(true);

        var result = Build().Check();

        Assert.Equal(PendingRebootVerdict.Clean, result.Verdict);
        _registry.DidNotReceive().LocalMachineKeyExists(oldKey);
    }

    [Fact]
    public void Pending_renames_not_in_cache_returns_clean()
    {
        _registry.LocalMachineMultiStringValue(
                PendingRebootService.SessionManagerKey,
                PendingRebootService.PendingFileRenameOperationsValue)
            .Returns(new[]
            {
                @"\??\C:\Users\foo.tmp", "",
                @"\??\C:\ProgramData\Vendor\update.dat", @"\??\C:\ProgramData\Vendor\update.dat.bak",
            });

        var result = Build().Check();

        Assert.Equal(PendingRebootVerdict.Clean, result.Verdict);
    }

    /// <summary>
    /// A poisoned entry that lexically starts with the cache prefix but resolves
    /// outside it (via \..\ traversal) must canonicalise before the boundary check.
    /// Without GetFullPath, the literal C:\Windows\Installer\..\..\Users\Other\secret
    /// would StartsWith-match the prefix and surface the user-profile path through
    /// the Detail field on the way to the CLI / event log.
    /// </summary>
    [Fact]
    public void Pending_rename_with_traversal_outside_cache_returns_clean()
    {
        _registry.LocalMachineMultiStringValue(
                PendingRebootService.SessionManagerKey,
                PendingRebootService.PendingFileRenameOperationsValue)
            .Returns(new[]
            {
                @"\??\C:\Windows\Installer\..\..\Users\Other\secret.txt", "",
            });

        var result = Build().Check();

        Assert.Equal(PendingRebootVerdict.Clean, result.Verdict);
        Assert.Null(result.Reason);
    }

    /// <summary>
    /// The boundary check rejects sibling folders that share the cache's name as a prefix.
    /// Without the trailing-separator anchor, C:\Windows\InstallerExtra would match
    /// C:\Windows\Installer.
    /// </summary>
    [Fact]
    public void Pending_rename_in_sibling_folder_returns_clean()
    {
        _registry.LocalMachineMultiStringValue(
                PendingRebootService.SessionManagerKey,
                PendingRebootService.PendingFileRenameOperationsValue)
            .Returns(new[]
            {
                @"\??\C:\Windows\InstallerExtra\foo.dll", "",
            });

        var result = Build().Check();

        Assert.Equal(PendingRebootVerdict.Clean, result.Verdict);
    }

    /// <summary>
    /// An entry carrying an NT prefix over something that names no volume this can
    /// place. Nothing establishes that such a value is not a form the app does not
    /// understand, so it is a queued operation whose target is unknown rather than one
    /// that can be dismissed, and the gate says so.
    ///
    /// THE FIXTURE PUTS THE CACHE WHERE Path.GetFullPath WOULD COMPLETE THE VALUE TO,
    /// which is what makes this a test rather than a restatement: the Windows root is
    /// the working directory, so an entry naming "Installer" would complete to the very
    /// folder the comparison is looking for. That is why Detail is asserted null. A
    /// comparison reading a completed value would answer about wherever the app was
    /// started from and hand that path to the user as the queued rename it found. The
    /// entry carries no separator of its own so that the completion lands on the folder
    /// itself.
    /// </summary>
    [Fact]
    public void Pending_rename_naming_no_volume_this_can_place_blocks()
    {
        Queued(@"\??\Installer", "");

        var result = Build(windowsRoot: Directory.GetCurrentDirectory()).Check();

        Assert.Equal(PendingRebootVerdict.Block, result.Verdict);
        Assert.Equal(PendingRebootReason.PendingRenameUnresolved, result.Reason);
        Assert.Null(result.Detail);
    }

    [Fact]
    public void Empty_pending_file_renames_returns_clean()
    {
        _registry.LocalMachineMultiStringValue(
                PendingRebootService.SessionManagerKey,
                PendingRebootService.PendingFileRenameOperationsValue)
            .Returns(Array.Empty<string>());

        var result = Build().Check();

        Assert.Equal(PendingRebootVerdict.Clean, result.Verdict);
    }

    [Fact]
    public void Missing_pending_file_renames_value_returns_clean()
    {
        _registry.LocalMachineMultiStringValue(
                PendingRebootService.SessionManagerKey,
                PendingRebootService.PendingFileRenameOperationsValue)
            .Returns((string[]?)null);

        var result = Build().Check();

        Assert.Equal(PendingRebootVerdict.Clean, result.Verdict);
    }

    [Fact]
    public void Missing_in_progress_key_returns_clean()
    {
        _registry.LocalMachineKeyExists(
                PendingRebootService.InstallerInProgressKey)
            .Returns(false);

        var result = Build().Check();

        Assert.Equal(PendingRebootVerdict.Clean, result.Verdict);
    }

    // Edge: must not throw, must fail open.

    [Fact]
    public void Registry_read_throws_fails_open_returns_clean()
    {
        _registry.LocalMachineKeyExists(Arg.Any<string>())
            .Returns(_ => throw new UnauthorizedAccessException("denied"));
        _registry.LocalMachineMultiStringValue(Arg.Any<string>(), Arg.Any<string>())
            .Returns(_ => throw new UnauthorizedAccessException("denied"));

        var result = Build().Check();

        Assert.Equal(PendingRebootVerdict.Clean, result.Verdict);
    }

    [Fact]
    public void Mutex_probe_throws_fails_open_continues_to_other_checks()
    {
        _mutex.IsHeld(Arg.Any<string>())
            .Returns(_ => throw new InvalidOperationException("transient"));
        _registry.LocalMachineKeyExists(
                PendingRebootService.InstallerInProgressKey)
            .Returns(true);

        var result = Build().Check();

        Assert.Equal(PendingRebootVerdict.Block, result.Verdict);
        Assert.Equal(PendingRebootReason.InstallerInProgress, result.Reason);
    }

    [Fact]
    public void Mutex_probe_access_denied_fails_open_continues_to_other_checks()
    {
        _mutex.IsHeld(Arg.Any<string>())
            .Returns(_ => throw new UnauthorizedAccessException("denied"));
        _registry.LocalMachineMultiStringValue(
                PendingRebootService.SessionManagerKey,
                PendingRebootService.PendingFileRenameOperationsValue)
            .Returns(new[] { @"\??\C:\Windows\Installer\foo.msi", "" });

        var result = Build().Check();

        Assert.Equal(PendingRebootVerdict.Block, result.Verdict);
        Assert.Equal(PendingRebootReason.PendingRenameInCache, result.Reason);
    }

    /// <summary>
    /// A value that is not a path at all still describes a queued file operation, and
    /// the app cannot say what it names. It reaches the same verdict as an exotic
    /// spelling, and deliberately: no rule tells the two apart that is not a guess about
    /// what the object manager might mean by a string, and the nearest neighbour of
    /// "corrupted text" is a bare "\Windows\Installer\9f05cba.msi", which is the cache
    /// path with its volume missing.
    ///
    /// Not throwing is the other half and is why this test was written. Check answers
    /// with a verdict for every input, and a value nobody can parse is one of them.
    /// </summary>
    [Fact]
    public void Malformed_pending_rename_blocks_rather_than_being_dismissed()
    {
        Queued("this is not a path", "", "corrupted text", "");

        var result = Record.Exception(() => Build().Check());

        Assert.Null(result);

        var verdict = Build().Check();
        Assert.Equal(PendingRebootVerdict.Block, verdict.Verdict);
        Assert.Equal(PendingRebootReason.PendingRenameUnresolved, verdict.Reason);
        Assert.Null(verdict.Detail);
    }

    /// <summary>
    /// The nearest neighbour named above, on its own, because it is the member of this
    /// class most likely to be a real cache rename recorded oddly rather than junk.
    /// </summary>
    [Fact]
    public void Pending_rename_naming_the_cache_path_without_its_volume_blocks()
    {
        Queued(@"\Windows\Installer\9f05cba.msi", "");

        var result = Build().Check();

        Assert.Equal(PendingRebootVerdict.Block, result.Verdict);
        Assert.Equal(PendingRebootReason.PendingRenameUnresolved, result.Reason);
    }

    [Fact]
    public void Wrong_value_type_on_pending_file_renames_returns_clean()
    {
        // Production RegistryReader returns null when GetValue surfaces a non-string[]
        // via `as string[]`. Simulate the contract here.
        _registry.LocalMachineMultiStringValue(
                PendingRebootService.SessionManagerKey,
                PendingRebootService.PendingFileRenameOperationsValue)
            .Returns((string[]?)null);

        var result = Build().Check();

        Assert.Equal(PendingRebootVerdict.Clean, result.Verdict);
    }

    [Fact]
    public void Windows_on_different_drive_still_detects_cache_rename()
    {
        _registry.LocalMachineMultiStringValue(
                PendingRebootService.SessionManagerKey,
                PendingRebootService.PendingFileRenameOperationsValue)
            .Returns(new[] { @"\??\D:\Windows\Installer\foo.msi", "" });

        var result = Build(windowsRoot: @"D:\Windows").Check();

        Assert.Equal(PendingRebootVerdict.Block, result.Verdict);
        Assert.Equal(PendingRebootReason.PendingRenameInCache, result.Reason);
        Assert.Equal(@"D:\Windows\Installer\foo.msi", result.Detail);
    }

    // The two spellings that name a volume rather than carrying a drive root.

    [Fact]
    public void Rename_on_a_volume_named_by_guid_that_lands_in_the_cache_blocks()
    {
        Volume("aaaaaaaa-1111-2222-3333-444444444444", @"\Device\HarddiskVolume3", @"C:\");
        Queued(@"\??\Volume{aaaaaaaa-1111-2222-3333-444444444444}\Windows\Installer\1234.msi", "");

        var result = Build().Check();

        Assert.Equal(PendingRebootVerdict.Block, result.Verdict);
        Assert.Equal(PendingRebootReason.PendingRenameInCache, result.Reason);
        Assert.Equal(@"C:\Windows\Installer\1234.msi", result.Detail);
    }

    [Fact]
    public void Rename_on_a_volume_named_by_guid_that_lands_elsewhere_returns_clean()
    {
        Volume("aaaaaaaa-1111-2222-3333-444444444444", @"\Device\HarddiskVolume3", @"C:\");
        Queued(@"\??\Volume{aaaaaaaa-1111-2222-3333-444444444444}\Users\foo.tmp", "");

        var result = Build().Check();

        Assert.Equal(PendingRebootVerdict.Clean, result.Verdict);
    }

    /// <summary>
    /// THE CASE THE HANDLING EXISTS FOR, AND THE ONE A GATE THAT REFUSED ON EXOTIC
    /// SPELLINGS WOULD GET WRONG. Windows names a volume this way when there is no
    /// mount point to name it by, and it does so in its own boot-file servicing, which
    /// is an ordinary state for a machine to be in. A volume mounted nowhere holds
    /// nothing inside the installer cache, because that path reaches its files through
    /// a mount point, so this is a settled answer and not a refusal.
    /// </summary>
    [Fact]
    public void Rename_on_a_volume_mounted_nowhere_returns_clean()
    {
        Volume("bbbbbbbb-1111-2222-3333-444444444444", @"\Device\HarddiskVolume1");
        Queued(@"\??\GLOBALROOT\Device\HarddiskVolume1\EFI\Microsoft\Boot\BOOTSTAT.DAT", "");

        var result = Build().Check();

        Assert.Equal(PendingRebootVerdict.Clean, result.Verdict);
        Assert.Null(result.Reason);
    }

    [Fact]
    public void Rename_on_a_volume_named_by_device_that_lands_in_the_cache_blocks()
    {
        Volume("aaaaaaaa-1111-2222-3333-444444444444", @"\Device\HarddiskVolume3", @"C:\");
        Queued(@"\??\GLOBALROOT\Device\HarddiskVolume3\Windows\Installer\1234.msi", "");

        var result = Build().Check();

        Assert.Equal(PendingRebootVerdict.Block, result.Verdict);
        Assert.Equal(PendingRebootReason.PendingRenameInCache, result.Reason);
        Assert.Equal(@"C:\Windows\Installer\1234.msi", result.Detail);
    }

    /// <summary>
    /// A device name is not always two components, so the match is against the head of
    /// the entry rather than a fixed count: a volume reporting
    /// <c>\Device\Harddisk0\Partition3</c> is matched exactly as one reporting
    /// <c>\Device\HarddiskVolume3</c> is, and what follows it is the path on that volume.
    /// </summary>
    [Fact]
    public void A_device_name_of_more_than_two_components_is_matched_whole()
    {
        Volume("aaaaaaaa-1111-2222-3333-444444444444", @"\Device\Harddisk0\Partition3", @"C:\");
        Queued(@"\??\GLOBALROOT\Device\Harddisk0\Partition3\Windows\Installer\1234.msi", "");

        var result = Build().Check();

        Assert.Equal(PendingRebootVerdict.Block, result.Verdict);
        Assert.Equal(PendingRebootReason.PendingRenameInCache, result.Reason);
    }

    // The residue: the app knows something is queued and cannot say where.

    /// <summary>
    /// The mount-point query failing and the volume being mounted nowhere arrive as the
    /// same empty list. This is the first of the pair and its twin is
    /// Rename_on_a_volume_mounted_nowhere_returns_clean; the two fixtures differ only in
    /// whether the query answered, and they must reach opposite verdicts.
    /// </summary>
    [Fact]
    public void Rename_on_a_volume_whose_mount_points_would_not_read_blocks()
    {
        var guidPath = @"\\?\Volume{aaaaaaaa-1111-2222-3333-444444444444}\";
        _volumes.MountPointsFor(guidPath).Returns(VolumeMountPoints.NoAnswer);
        Queued(@"\??\Volume{aaaaaaaa-1111-2222-3333-444444444444}\Windows\Installer\1234.msi", "");

        var result = Build().Check();

        Assert.Equal(PendingRebootVerdict.Block, result.Verdict);
        Assert.Equal(PendingRebootReason.PendingRenameUnresolved, result.Reason);
        Assert.Null(result.Detail);
    }

    [Fact]
    public void Rename_on_a_device_no_volume_on_this_machine_claims_blocks()
    {
        Volume("aaaaaaaa-1111-2222-3333-444444444444", @"\Device\HarddiskVolume3", @"C:\");
        Queued(@"\??\GLOBALROOT\Device\HarddiskVolume9\Windows\Installer\1234.msi", "");

        var result = Build().Check();

        Assert.Equal(PendingRebootVerdict.Block, result.Verdict);
        Assert.Equal(PendingRebootReason.PendingRenameUnresolved, result.Reason);
    }

    [Fact]
    public void Rename_on_a_device_blocks_when_the_volumes_would_not_enumerate()
    {
        _volumes.VolumeGuidPaths().Returns((IReadOnlyList<string>?)null);
        Queued(@"\??\GLOBALROOT\Device\HarddiskVolume1\Windows\Installer\1234.msi", "");

        var result = Build().Check();

        Assert.Equal(PendingRebootVerdict.Block, result.Verdict);
        Assert.Equal(PendingRebootReason.PendingRenameUnresolved, result.Reason);
    }

    /// <summary>
    /// A volume query that throws is that query failing, so it lands where a failure
    /// lands rather than reaching the caller. Check answers with a verdict for every
    /// input, and this is the one path where a dependency could break that.
    /// </summary>
    [Fact]
    public void A_volume_query_that_throws_blocks_rather_than_propagating()
    {
        _volumes.VolumeGuidPaths().Returns(_ => throw new InvalidOperationException("transient"));
        Queued(@"\??\GLOBALROOT\Device\HarddiskVolume1\Windows\Installer\1234.msi", "");

        var result = Build().Check();

        Assert.Equal(PendingRebootVerdict.Block, result.Verdict);
        Assert.Equal(PendingRebootReason.PendingRenameUnresolved, result.Reason);
    }

    /// <summary>
    /// AN ENTRY THAT NAMES THE CACHE WINS OVER ONE THAT COULD NOT BE PLACED, WHATEVER
    /// ORDER THEY SIT IN. The unplaceable entry is FIRST here deliberately: a pass that
    /// returned on the first thing it could not place would answer with the weaker
    /// verdict and never reach the entry that names a path. The stronger message is
    /// certainly true and it tells the user which file is queued.
    /// </summary>
    [Fact]
    public void An_entry_naming_the_cache_wins_over_one_that_could_not_be_placed()
    {
        Queued("corrupted text", "", @"\??\C:\Windows\Installer\1234.msi", "");

        var result = Build().Check();

        Assert.Equal(PendingRebootVerdict.Block, result.Verdict);
        Assert.Equal(PendingRebootReason.PendingRenameInCache, result.Reason);
        Assert.Equal(@"C:\Windows\Installer\1234.msi", result.Detail);
    }

    /// <summary>
    /// A network path can never name %SystemRoot%\Installer, which is local on any
    /// machine that can boot, so the UNC form is placed and dismissed rather than
    /// refused. Refusing on it would stop the app for a reason that has nothing to do
    /// with its job.
    /// </summary>
    [Fact]
    public void A_unc_entry_is_placed_and_returns_clean()
    {
        Queued(@"\??\UNC\server\share\Windows\Installer\1234.msi", "");

        var result = Build().Check();

        Assert.Equal(PendingRebootVerdict.Clean, result.Verdict);
        _volumes.DidNotReceive().VolumeGuidPaths();
    }

    // Ordering: signal precedence.

    [Fact]
    public void Mutex_wins_over_in_progress_and_pending_rename()
    {
        _mutex.IsHeld(PendingRebootService.MsiExecuteMutexName).Returns(true);
        _registry.LocalMachineKeyExists(
                PendingRebootService.InstallerInProgressKey)
            .Returns(true);
        _registry.LocalMachineMultiStringValue(
                PendingRebootService.SessionManagerKey,
                PendingRebootService.PendingFileRenameOperationsValue)
            .Returns(new[] { @"\??\C:\Windows\Installer\foo.msi", "" });

        var result = Build().Check();

        Assert.Equal(PendingRebootReason.MsiExecuteMutexHeld, result.Reason);
        _registry.DidNotReceive().LocalMachineKeyExists(Arg.Any<string>());
        _registry.DidNotReceive().LocalMachineMultiStringValue(
            Arg.Any<string>(), Arg.Any<string>());
    }

    [Fact]
    public void In_progress_wins_over_pending_rename()
    {
        _registry.LocalMachineKeyExists(
                PendingRebootService.InstallerInProgressKey)
            .Returns(true);
        _registry.LocalMachineMultiStringValue(
                PendingRebootService.SessionManagerKey,
                PendingRebootService.PendingFileRenameOperationsValue)
            .Returns(new[] { @"\??\C:\Windows\Installer\foo.msi", "" });

        var result = Build().Check();

        Assert.Equal(PendingRebootReason.InstallerInProgress, result.Reason);
        _registry.DidNotReceive().LocalMachineMultiStringValue(
            Arg.Any<string>(), Arg.Any<string>());
    }
}
