using InstallerClean.Helpers;

namespace InstallerClean.Tests.Helpers.Integration;

/// <summary>
/// The ancestor walk behind the Move's free-space refusal. It answers a
/// question about a real volume, so nothing here can be mocked: the point of
/// the walk is that GetDiskFreeSpaceEx fails on a directory that does not
/// exist, which is exactly what a scheduled <c>/m D:\Backup</c> names on its
/// first run.
/// </summary>
public class MoveSpaceCheckIntegrationTests
{
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
