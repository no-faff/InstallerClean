using InstallerClean.Helpers;
using InstallerClean.Models;
using InstallerClean.Services;
using InstallerClean.Tests.Helpers;
using InstallerClean.ViewModels;
using Xunit.Abstractions;

namespace InstallerClean.Tests.Helpers.Integration;

/// <summary>
/// The Move's volume questions put to a real volume mounted into a folder on the
/// drive that holds the installer cache.
/// </summary>
/// <remarks>
/// The run that mounts the volume names three things through the environment (see
/// <see cref="MountedVolumeFactAttribute"/>): the mount folder, an ordinary folder
/// beside it on the cache's volume, and the mounted volume's free space as Windows
/// reported it. The expected answers come from those, never from the calls under
/// test.
///
/// THE MOUNTED VOLUME'S FIGURE IS COMPARED WITHIN <see cref="FreeSpaceTolerance"/>, not
/// to the byte. The run reads it through Get-Volume and the app through
/// GetDiskFreeSpaceEx, minutes apart, and Windows can write its own files to a newly
/// mounted volume in between. The tolerance is far smaller than the gap between the
/// mounted volume's figure and the cache drive's, so it cannot let one answer for the
/// other.
///
/// Every destination asked about under the mount folder is one the run did not
/// create, except <see cref="ExistingUnderMount"/>, which it did. Nothing here
/// creates anything.
///
/// Each fact writes the call, the expected answer and the answer it got before it
/// asserts anything, so the run's record says what was asked as well as whether it
/// agreed.
/// </remarks>
public class MoveSpaceCheckMountPointTests(ITestOutputHelper output)
{
    private const long FreeSpaceTolerance = 1024 * 1024;

    private static string MountFolder =>
        PathText.TrimEndingDirectorySeparator(Path.GetFullPath(
            Environment.GetEnvironmentVariable(MountedVolumeFactAttribute.FolderVariable)!));

    private static string BesideFolder =>
        PathText.TrimEndingDirectorySeparator(Path.GetFullPath(
            Environment.GetEnvironmentVariable(MountedVolumeFactAttribute.BesideVariable)!));

    private static long MountedFreeBytes =>
        long.Parse(Environment.GetEnvironmentVariable(MountedVolumeFactAttribute.FreeBytesVariable)!,
            System.Globalization.CultureInfo.InvariantCulture);

    /// <summary>
    /// GetVolumePathName's spelling of the mount folder: the folder with a trailing
    /// backslash. Compared ignoring case, as the volume comparison itself does.
    /// </summary>
    private static string MountVolume => MountFolder + Path.DirectorySeparatorChar;

    private static string UnderMount => Path.Combine(MountFolder, "backup");
    private static string DeepUnderMount => Path.Combine(MountFolder, "new", "deeper");
    private static string ExistingUnderMount => Path.Combine(MountFolder, "exists");
    private static string CacheRootUnderMount => Path.Combine(MountFolder, "cache");
    private static string BesideDestination => Path.Combine(BesideFolder, "backup");

    private static string AboutMountedFree => $"within {FreeSpaceTolerance} of {MountedFreeBytes}";

    private static bool IsMountedFree(long? got) =>
        got is long value && Math.Abs(value - MountedFreeBytes) <= FreeSpaceTolerance;

    private void Row(int row, string call, object? expected, object? got) =>
        output.WriteLine($"ROW {row} | {call} | expected {expected ?? "null"} | got {got ?? "null"}");

    [MountedVolumeFact]
    public void Row01_the_volume_of_a_missing_folder_under_the_mount_is_the_mount_folder()
    {
        var got = StorageHelpers.GetVolumeMountPoint(UnderMount);
        Row(1, $"GetVolumeMountPoint({UnderMount})", MountVolume, got);
        Assert.Equal(MountVolume, got, ignoreCase: true);
    }

    [MountedVolumeFact]
    public void Row02_the_volume_of_the_mount_folder_is_itself()
    {
        var got = StorageHelpers.GetVolumeMountPoint(MountFolder);
        Row(2, $"GetVolumeMountPoint({MountFolder})", MountVolume, got);
        Assert.Equal(MountVolume, got, ignoreCase: true);
    }

    [MountedVolumeFact]
    public void Row03_the_volume_of_the_folder_beside_the_mount_is_its_drive()
    {
        var expected = Path.GetPathRoot(BesideFolder);
        var got = StorageHelpers.GetVolumeMountPoint(BesideFolder);
        Row(3, $"GetVolumeMountPoint({BesideFolder})", expected, got);
        Assert.Equal(expected, got, ignoreCase: true);
    }

    [MountedVolumeFact]
    public void Row04_the_volume_of_the_installer_cache_is_the_drive_beside_the_mount()
    {
        // The beside folder is on the cache's drive by the run's construction, and
        // nothing is mounted at the cache folder.
        var expected = Path.GetPathRoot(BesideFolder);
        var got = StorageHelpers.GetVolumeMountPoint(InstallerCacheHelpers.InstallerFolder);
        Row(4, $"GetVolumeMountPoint({InstallerCacheHelpers.InstallerFolder})", expected, got);
        Assert.Equal(expected, got, ignoreCase: true);
    }

    [MountedVolumeFact]
    public void Row05_a_destination_under_the_mount_is_not_on_the_cache_volume()
    {
        var got = MoveSpaceCheck.ResolveIsOnInstallerCacheDrive(UnderMount);
        Row(5, $"ResolveIsOnInstallerCacheDrive({UnderMount})", false, got);
        Assert.False(got is null, "Windows did not settle the question.");
        Assert.False(got);
    }

    [MountedVolumeFact]
    public void Row06_the_mount_folder_is_not_on_the_cache_volume()
    {
        var got = MoveSpaceCheck.ResolveIsOnInstallerCacheDrive(MountFolder);
        Row(6, $"ResolveIsOnInstallerCacheDrive({MountFolder})", false, got);
        Assert.False(got is null, "Windows did not settle the question.");
        Assert.False(got);
    }

    [MountedVolumeFact]
    public void Row07_a_destination_beside_the_mount_is_on_the_cache_volume()
    {
        var got = MoveSpaceCheck.ResolveIsOnInstallerCacheDrive(BesideDestination);
        Row(7, $"ResolveIsOnInstallerCacheDrive({BesideDestination})", true, got);
        Assert.True(got);
    }

    [MountedVolumeFact]
    public void Row08_free_space_for_a_missing_folder_under_the_mount_is_the_mounted_volume()
    {
        // Two levels that do not exist, so the walk climbs to the mount folder, which
        // answers for the mounted volume rather than for the drive it sits on.
        var got = MoveSpaceCheck.AvailableFreeSpaceForDestination(DeepUnderMount);
        Row(8, $"AvailableFreeSpaceForDestination({DeepUnderMount})", AboutMountedFree, got);
        Assert.True(IsMountedFree(got));
    }

    [MountedVolumeFact]
    public void Row09_free_space_at_the_mount_folder_is_the_mounted_volume()
    {
        var got = MoveSpaceCheck.AvailableFreeSpaceForDestination(MountFolder);
        Row(9, $"AvailableFreeSpaceForDestination({MountFolder})", AboutMountedFree, got);
        Assert.True(IsMountedFree(got));
    }

    [MountedVolumeFact]
    public void Row10_free_space_beside_the_mount_is_not_the_mounted_volume()
    {
        var destination = Path.Combine(BesideFolder, "new");
        var got = MoveSpaceCheck.AvailableFreeSpaceForDestination(destination);
        Row(10, $"AvailableFreeSpaceForDestination({destination})",
            $"more than {MountedFreeBytes + FreeSpaceTolerance}", got);
        Assert.NotNull(got);
        Assert.True(got > MountedFreeBytes + FreeSpaceTolerance);
    }

    [MountedVolumeFact]
    public void Row11_the_command_line_refuses_a_move_too_big_for_the_mounted_volume()
    {
        // The command line's overload, which measures through the walk itself. The
        // size is past the mounted volume's figure by more than it can drift.
        var size = MountedFreeBytes + 2 * FreeSpaceTolerance;
        var got = MoveSpaceCheck.MeasuredRefusalFreeSpace(UnderMount, size);
        Row(11, $"MeasuredRefusalFreeSpace({UnderMount}, {size})", $"refused, returning {AboutMountedFree}", got);
        Assert.True(IsMountedFree(got));
    }

    [MountedVolumeFact]
    public void Row12_the_command_line_allows_a_move_that_fits_the_mounted_volume()
    {
        // The measurement is read for itself as well, so the allowance is shown to be
        // one made against the mounted volume's figure.
        var measured = MoveSpaceCheck.AvailableFreeSpaceForDestination(UnderMount);
        var got = MoveSpaceCheck.MeasuredRefusalFreeSpace(UnderMount, 1);
        Row(12, $"MeasuredRefusalFreeSpace({UnderMount}, 1), measuring {measured}",
            $"null, measuring {AboutMountedFree}", got);
        Assert.True(IsMountedFree(measured));
        Assert.Null(got);
    }

    [MountedVolumeFact]
    public void Row13_a_move_beside_the_mount_is_allowed_with_no_room_at_all()
    {
        // The window's call with a measured free space of zero, so only the
        // same-volume answer can allow it, and the command line's call, which
        // measures for itself, asked for more room than any volume has.
        var got = MoveSpaceCheck.RefusalFreeSpace(BesideDestination, long.MaxValue, 0);
        var gotMeasuring = MoveSpaceCheck.MeasuredRefusalFreeSpace(BesideDestination, long.MaxValue);
        Row(13, $"RefusalFreeSpace({BesideDestination}, {long.MaxValue}, 0) and MeasuredRefusalFreeSpace({BesideDestination}, {long.MaxValue})",
            "null and null", $"{got?.ToString() ?? "null"} and {gotMeasuring?.ToString() ?? "null"}");
        Assert.Null(got);
        Assert.Null(gotMeasuring);
    }

    [MountedVolumeFact]
    public void Row14_the_window_measures_an_existing_folder_under_the_mount_as_the_mounted_volume()
    {
        // The window's pre-flight creates the destination and measures it directly.
        var got = StorageHelpers.GetAvailableFreeSpace(ExistingUnderMount);
        Row(14, $"GetAvailableFreeSpace({ExistingUnderMount})", AboutMountedFree, got);
        Assert.True(Directory.Exists(ExistingUnderMount), "The run did not create the folder.");
        Assert.True(IsMountedFree(got));
    }

    [MountedVolumeFact]
    public void Row15_the_window_refuses_a_move_too_big_for_the_mounted_volume()
    {
        // The window's three-argument call, with its own measurement.
        var measured = StorageHelpers.GetAvailableFreeSpace(ExistingUnderMount);
        var size = (measured ?? 0) + 1;
        var got = MoveSpaceCheck.RefusalFreeSpace(ExistingUnderMount, size, measured);
        Row(15, $"RefusalFreeSpace({ExistingUnderMount}, {size}, {measured})", measured, got);
        Assert.NotNull(measured);
        Assert.Equal(measured, got);
    }

    [MountedVolumeFact]
    public void Row16_the_mounted_volume_reads_as_a_fixed_drive()
    {
        var volume = StorageHelpers.GetVolumeMountPoint(UnderMount);
        var got = volume is null ? (DriveType?)null : StorageHelpers.GetDriveKind(volume);
        Row(16, $"GetDriveKind({volume})", DriveType.Fixed, got);
        Assert.Equal(DriveType.Fixed, got);
    }

    [MountedVolumeFact]
    public void Row17_a_destination_under_the_mount_is_recorded_as_another_fixed_drive()
    {
        var got = MoveDestinationClassifier.Classify(UnderMount);
        Row(17, $"MoveDestinationClassifier.Classify({UnderMount})", MoveDestinationKinds.DifferentFixedDrive, got);
        Assert.Equal(MoveDestinationKinds.DifferentFixedDrive, got);
    }

    [MountedVolumeFact]
    public void Row18_a_destination_beside_the_mount_is_recorded_as_the_same_drive()
    {
        var got = MoveDestinationClassifier.Classify(BesideDestination);
        Row(18, $"MoveDestinationClassifier.Classify({BesideDestination})", MoveDestinationKinds.SameDrive, got);
        Assert.Equal(MoveDestinationKinds.SameDrive, got);
    }

    [MountedVolumeFact]
    public void Row19_a_cache_root_under_the_mount_answers_for_the_mounted_volume()
    {
        // The cache side asked about a real mount point, through the cache-root
        // override: a cache root under the mount folder and a destination under it
        // are on one volume.
        // The cache root's own volume is read as well, so a true here is the mount
        // folder answering for both sides rather than the drive answering for both.
        var cacheVolume = StorageHelpers.GetVolumeMountPoint(CacheRootUnderMount);
        var got = MoveSpaceCheck.ResolveIsOnInstallerCacheDrive(UnderMount, CacheRootUnderMount);
        Row(19, $"ResolveIsOnInstallerCacheDrive({UnderMount}, cache root {CacheRootUnderMount}), cache root on {cacheVolume}",
            $"true, cache root on {MountVolume}", got);
        Assert.Equal(MountVolume, cacheVolume, ignoreCase: true);
        Assert.True(got);
    }

    [MountedVolumeFact]
    public void Row20_a_destination_beside_the_mount_is_not_on_a_cache_root_under_it()
    {
        var got = MoveSpaceCheck.ResolveIsOnInstallerCacheDrive(BesideDestination, CacheRootUnderMount);
        Row(20, $"ResolveIsOnInstallerCacheDrive({BesideDestination}, cache root {CacheRootUnderMount})", false, got);
        Assert.False(got is null, "Windows did not settle the question.");
        Assert.False(got);
    }
}
