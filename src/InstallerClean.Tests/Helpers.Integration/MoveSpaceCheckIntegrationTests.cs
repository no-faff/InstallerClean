using InstallerClean.Helpers;
using InstallerClean.Services;

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
///
/// AND THERE ARE TWO SUCH SHAPES NOW, NOT ONE. The 3.0.0 change fixed the
/// DESTINATION side, so the shape it could not set up was a volume mounted
/// under the folder the user names. <c>IsOnInstallerCacheDrive</c> then went on
/// asking about the system directory's volume rather than the cache's, so the
/// second shape is a volume mounted at <c>C:\Windows\Installer</c> itself. This
/// file can build neither, for the same reason.
///
/// WHICH ASSERTIONS REST ON THE TWO COINCIDING IS NOT UNIFORM, AND EACH SAYS SO
/// WHERE IT STANDS. Of those asking whether something is on the CACHE's volume,
/// the ones built under the cache folder itself hold on any machine, and the
/// ones built from a PATH ROOT hold only where nothing is mounted between that
/// root and the cache folder and are false where something is. The root is kept
/// in those rather than swapped for the call under test, which would let the
/// fixture agree with the code by construction.
///
/// The tests asking about the SYSTEM directory's own volume are a separate
/// matter and none of this touches them: a volume mounted at the cache folder
/// does not sit between <c>C:\</c> and <c>C:\Windows\System32</c>, so a path
/// root answers for them on that machine as well as on any other.
/// </remarks>
public class MoveSpaceCheckIntegrationTests
{
    [Fact]
    public void IsOnInstallerCacheDrive_is_true_for_a_folder_under_the_cache_path_root()
    {
        // THIS TEST WAS NAMED FOR THE SYSTEM DRIVE AND PINNED A FAULT. The
        // method asked which volume the Windows system directory is on, which
        // is not the installer cache's volume where a volume is mounted at
        // C:\Windows\Installer, and the test asserted that behaviour by name.
        // The subject moved to the cache when the method did.
        //
        // THE FIXTURE COMPUTES A PATH ROOT AND THE METHOD COMPARES VOLUMES, AND
        // THE NAME NOW SAYS WHICH. Path.GetPathRoot answers C:\ for the cache
        // folder whatever is mounted there, which is the arithmetic
        // MoveSpaceCheck exists to have abolished. It is kept here on purpose:
        // building the fixture from GetVolumeMountPoint would have it compute
        // its expectation with the call under test, and a fixture that agrees
        // with the code by construction cannot fail at what its name claims.
        //
        // SO THIS ASSERTION IS FALSE ON THE MACHINE THE FIX IS FOR. With a
        // volume mounted at C:\Windows\Installer the cache's volume is
        // C:\Windows\Installer\, C:\backup is genuinely not on it, the method
        // correctly answers false, and this test fails. It holds only where
        // nothing is mounted between the path root and the cache folder.
        //
        // AND WHERE IT DOES HOLD IT CANNOT TELL THE FIXED CODE FROM THE CODE IT
        // REPLACED, which is a second statement and a weaker one, not the same
        // one said twice. The cache's volume and the system root are the same
        // string on every host this suite runs on, so the assertion passes
        // against both. Building a host where it could tell them apart needs a
        // second volume and administrator rights.
        //
        // What it does newly pin is that the cache-side query answers at all.
        // Before the fix this method never touched the cache path; it does now,
        // and a GetVolumePathName that would not answer for it returns null, so
        // the method answers false for every destination on the machine, the
        // free-space check runs on a move that is a rename, and the Move this
        // app exists for is refused for want of space it does not need. Whether
        // anything about that folder could make the call refuse is not asserted
        // here and was not reasoned out. This test is what answers it, on a
        // real Windows host.
        //
        // READ A RED HERE AGAINST ONE DIFFERENCE BEFORE READING IT AS A FAULT.
        // Both hosts ship requireAdministrator in their manifests, so the app
        // always asks this question elevated; the suite does not. A green run
        // is therefore stronger than production needs, and a red one would have
        // to be shown to survive elevation before it meant anything about the
        // app.
        var cachePathRoot = Path.GetPathRoot(InstallerCacheHelpers.InstallerFolder)!;

        Assert.True(MoveSpaceCheck.IsOnInstallerCacheDrive(Path.Combine(cachePathRoot, "backup")));
        // Case and separator shape must not change the answer: the box takes
        // whatever the user typed.
        Assert.True(MoveSpaceCheck.IsOnInstallerCacheDrive(
            Path.Combine(cachePathRoot.ToLowerInvariant(), "backup") + Path.DirectorySeparatorChar));
    }

    [Fact]
    public void IsOnInstallerCacheDrive_is_true_for_the_cache_folder_itself()
    {
        // THE ONE ASSERTION IN THIS FILE THAT HOLDS ON EVERY MACHINE, including
        // the one nothing here can build. The cache folder is on its own volume
        // whatever is mounted where, so this is true with a volume mounted at
        // C:\Windows\Installer and true without one. Under the code this
        // replaced it was true only where the cache and the system directory
        // shared a volume, and false on the machine the fix is for.
        //
        // It is close to asking one query whether it agrees with itself, and it
        // is kept for the part that is not: the query has to ANSWER for the
        // cache path, or this is false rather than trivially true. The test
        // above says what to make of that and what elevation does to it.
        Assert.True(MoveSpaceCheck.IsOnInstallerCacheDrive(InstallerCacheHelpers.InstallerFolder));
    }

    [Fact]
    public void IsOnInstallerCacheDrive_answers_for_a_folder_that_does_not_exist_yet()
    {
        // THE CASE THAT WOULD HAVE COST THE APP ITS OWN PURPOSE. A scheduled
        // /m C:\Backup names a folder the Move is about to create, and this
        // question is asked before anything creates it. Had GetVolumePathName
        // needed the path to exist, this would answer "a different volume", the
        // free-space check would then run against the nearly-full drive the
        // cache is on, and the Move this app exists for would be refused. Win32
        // documents trailing path elements that are invalid as ignored; this is
        // that documented behaviour pinned, three levels deep so no single
        // missing component can be doing the work.
        //
        // BUILT UNDER THE CACHE FOLDER ITSELF RATHER THAN UNDER ITS PATH ROOT,
        // so the assertion is true on every machine including one with a volume
        // mounted at C:\Windows\Installer. A path root is C:\ there while the
        // cache's volume is C:\Windows\Installer\, and this would have failed
        // for a reason with nothing to do with what it tests.
        //
        // That is not the fixture computing its expectation with the call under
        // test. The cache folder is the constant the app itself is built from,
        // not GetVolumeMountPoint's answer about it, so this can still fail at
        // what it is named for: if Win32 stops resolving a missing trailing
        // component the destination side returns null, the two sides answer
        // differently, and this goes red.
        //
        // Nothing creates the folder. The assertion below is what says so.
        //
        // AND THE ELEVATION CAVEAT ON
        // IsOnInstallerCacheDrive_is_true_for_a_folder_under_the_cache_path_root
        // COVERS THIS TEST TOO, and covers both of its queries now the fixture
        // stands under the cache folder rather than on a path root. The suite
        // does not ask this question elevated and both hosts do, so a red here
        // has to be shown to survive elevation before it means anything about
        // the app.
        var missing = Path.Combine(
            InstallerCacheHelpers.InstallerFolder,
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
        // The case the whole app exists for: a nearly-full drive holding the
        // cache. The move is a rename, so it needs none of the space it is
        // short of, and refusing it here would refuse the only user who came
        // for this.
        //
        // BUILT UNDER THE CACHE FOLDER ITSELF, so the destination really is on
        // the cache's volume on every machine, including one with a volume
        // mounted at C:\Windows\Installer. It was built from the system
        // directory's path root, then from the cache folder's; both of those are
        // C:\ on an ordinary machine and neither is the cache's volume on that
        // one. MoveSpaceCheckTests names this test as the one covering the arm
        // where RefusalFreeSpace returns null without consulting the
        // measurement, so a subject that was only accidentally right would have
        // been inherited by that file's account of itself as well as sitting in
        // this one.
        //
        // Nothing is created or measured: RefusalFreeSpace returns on the
        // same-volume answer before it reaches the free-space figure at all,
        // which is the arm being pinned.
        //
        // The elevation caveat on
        // IsOnInstallerCacheDrive_is_true_for_a_folder_under_the_cache_path_root
        // covers this test too, and covers both of its queries now the fixture
        // stands under the cache folder. A red here has to be shown to survive
        // elevation before it means anything about the app.
        var onCacheVolume = Path.Combine(InstallerCacheHelpers.InstallerFolder, "backup");

        Assert.Null(MoveSpaceCheck.RefusalFreeSpace(onCacheVolume, 1_000, 0));
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
