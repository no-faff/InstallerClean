using InstallerClean.Helpers;

namespace InstallerClean.Tests.Helpers.Integration;

/// <summary>
/// Everything in the Move's space decision that a real volume has to answer.
/// </summary>
/// <remarks>
/// Nothing here can be mocked and that is the point of the file. The ancestor
/// walk exists because GetDiskFreeSpaceEx fails on a directory that does not
/// exist, which is exactly what a scheduled <c>/m D:\Backup</c> names on its
/// first run; and from 3.0.0 the same-volume question is a GetVolumePathName
/// call rather than a comparison of two path roots, so it lands here too.
///
/// WHAT THIS FILE CANNOT SET UP, said plainly so nobody reads its green as
/// wider than it is: a volume mounted into a folder, which is the shape the
/// 3.0.0 change was written for. Making one needs a second volume and
/// administrator rights, so no test here builds the fault it fixes. What is
/// covered is that the real question is being asked and its answer used, and in
/// particular that a folder which does not exist yet still resolves, which is
/// the property the command line's first run depends on and the one that would
/// have broken the app's own reason for existing had it not held.
/// </remarks>
public class MoveSpaceCheckIntegrationTests
{
    [Fact]
    public void IsOnInstallerCacheDrive_is_true_only_for_the_system_drive()
    {
        var systemRoot = Path.GetPathRoot(
            Environment.GetFolderPath(Environment.SpecialFolder.System))!;

        Assert.True(MoveSpaceCheck.IsOnInstallerCacheDrive(Path.Combine(systemRoot, "backup")));
        // Case and separator shape must not change the answer: the box takes
        // whatever the user typed.
        Assert.True(MoveSpaceCheck.IsOnInstallerCacheDrive(
            Path.Combine(systemRoot.ToLowerInvariant(), "backup") + Path.DirectorySeparatorChar));
    }

    [Fact]
    public void IsOnInstallerCacheDrive_answers_for_a_folder_that_does_not_exist_yet()
    {
        // THE CASE THAT WOULD HAVE COST THE APP ITS OWN PURPOSE. A scheduled
        // /m C:\Backup names a folder the Move is about to create, and this
        // question is asked before anything creates it. Had GetVolumePathName
        // needed the path to exist, this would answer "a different volume", the
        // free-space check would then run against a nearly-full system drive,
        // and the Move this app exists for would be refused. Win32 documents
        // trailing path elements that are invalid as ignored; this is that
        // documented behaviour pinned, three levels deep so no single missing
        // component can be doing the work.
        var missing = Path.Combine(
            Path.GetPathRoot(Environment.GetFolderPath(Environment.SpecialFolder.System))!,
            $"ic-volume-{Guid.NewGuid():N}", "backup", "monthly");

        Assert.False(Directory.Exists(missing));
        Assert.True(MoveSpaceCheck.IsOnInstallerCacheDrive(missing));
    }

    [Fact]
    public void IsOnInstallerCacheDrive_is_false_for_a_drive_that_is_not_there()
    {
        // No volume is mounted on the letter, so nothing was established and
        // the answer is false: the caller then omits its claim and, more to the
        // point, the free-space measurement runs instead of being skipped.
        var unmounted = TestHost.FirstUnmountedDriveLetter();
        if (unmounted is null)
            return; // every letter is in use on this host

        Assert.False(MoveSpaceCheck.IsOnInstallerCacheDrive($@"{unmounted}:\nope\never"));
    }

    [Fact]
    public void RefusalFreeSpace_allows_a_same_drive_move_with_no_room_at_all()
    {
        // The case the whole app exists for: a nearly-full system drive. The
        // move is a rename, so it needs none of the space it is short of, and
        // refusing it here would refuse the only user who came for this.
        var systemDrive = Path.Combine(
            Path.GetPathRoot(Environment.GetFolderPath(Environment.SpecialFolder.System))!,
            "backup");

        Assert.Null(MoveSpaceCheck.RefusalFreeSpace(systemDrive, 1_000, 0));
    }

    [Fact]
    public void GetVolumeMountPoint_answers_the_root_for_an_ordinary_system_path()
    {
        var systemRoot = Path.GetPathRoot(
            Environment.GetFolderPath(Environment.SpecialFolder.System))!;

        var volume = StorageHelpers.GetVolumeMountPoint(
            Environment.GetFolderPath(Environment.SpecialFolder.System));

        // Win32 returns the mount point with its trailing backslash, which is
        // what GetPathRoot spells for a drive letter too, so the two agree here.
        // They stop agreeing at a mount point, which is the whole reason the
        // call replaced the arithmetic and is the case no test here can build.
        Assert.Equal(systemRoot, volume, ignoreCase: true);
    }

    [Fact]
    public void GetVolumeMountPoint_is_null_for_a_drive_that_is_not_there()
    {
        var unmounted = TestHost.FirstUnmountedDriveLetter();
        if (unmounted is null)
            return; // every letter is in use on this host

        Assert.Null(StorageHelpers.GetVolumeMountPoint($@"{unmounted}:\nope"));
    }

    [Fact]
    public void GetDriveKind_reads_the_volume_at_a_mount_point()
    {
        var systemRoot = Path.GetPathRoot(
            Environment.GetFolderPath(Environment.SpecialFolder.System))!;

        // The system volume is fixed on every machine that can run this suite.
        Assert.Equal(DriveType.Fixed, StorageHelpers.GetDriveKind(systemRoot));

        // A trailing backslash is required by Win32 and supplied by the helper,
        // which is what lets a caller pass a mount point it built itself.
        Assert.Equal(DriveType.Fixed, StorageHelpers.GetDriveKind(systemRoot.TrimEnd('\\')));
    }

    [Fact]
    public void AvailableFreeSpaceForDestination_measures_a_folder_that_exists()
    {
        var space = MoveSpaceCheck.AvailableFreeSpaceForDestination(Path.GetTempPath());

        Assert.NotNull(space);
        Assert.True(space > 0);
    }

    [Fact]
    public void AvailableFreeSpaceForDestination_falls_back_to_the_nearest_existing_ancestor()
    {
        // Three levels of folder that were never created, under a folder that
        // exists. Measuring the leaf directly would return null and let a Move
        // with no room proceed; the ancestor is on the same volume and answers
        // for it.
        var missing = Path.Combine(Path.GetTempPath(),
            $"ic-space-{Guid.NewGuid():N}", "backup", "monthly");

        var viaAncestor = MoveSpaceCheck.AvailableFreeSpaceForDestination(missing);

        Assert.NotNull(viaAncestor);
        Assert.True(viaAncestor > 0);
    }

    [Fact]
    public void AvailableFreeSpaceForDestination_gives_up_on_a_drive_that_is_not_there()
    {
        // No ancestor exists, up to and including the root, so there is nothing
        // to measure and the caller makes no claim in either direction.
        //
        // The letter is found rather than named. A hardcoded one passes on a
        // runner that does not have that drive and fails on the machine of
        // anybody who does, for a reason with nothing to do with this code, and
        // the runners have C: and D: only.
        var unmounted = TestHost.FirstUnmountedDriveLetter();
        if (unmounted is null)
            return; // every letter is in use on this host

        Assert.Null(MoveSpaceCheck.AvailableFreeSpaceForDestination($@"{unmounted}:\nope\never"));
    }

    [Fact]
    public void RefusalFreeSpace_measures_for_itself_when_the_caller_has_not()
    {
        // The command line's overload. A byte fits anywhere, so this proves the
        // measured path runs and allows, rather than that any particular volume
        // is full.
        Assert.Null(MoveSpaceCheck.RefusalFreeSpace(Path.GetTempPath(), 1));
    }
}
