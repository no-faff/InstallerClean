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
/// A VOLUME MOUNTED INTO A FOLDER NEEDS A SECOND VOLUME AND ADMINISTRATOR
/// RIGHTS, and that is the shape the 3.0.0 change was written for. What these
/// tests do is put the real question to a real volume and use the answer, and in
/// particular establish that a folder which does not exist yet still resolves,
/// which is the property the command line's first run depends on and the one that
/// would have broken the app's own reason for existing had it not held.
///
/// AND THERE ARE TWO SUCH SHAPES, NOT ONE. The 3.0.0 change fixed the
/// DESTINATION side, where the mount sits under the folder the user names.
/// <c>IsOnInstallerCacheDrive</c> then went on asking about the system
/// directory's volume rather than the cache's, so the second shape is a volume
/// mounted at <c>C:\Windows\Installer</c> itself. Both want the same second
/// volume and the same rights.
///
/// WHICH ASSERTIONS DEPEND ON THE TWO COINCIDING IS NOT UNIFORM. Of those asking
/// whether something is on the CACHE's volume, the ones built under the cache
/// folder itself hold on any machine, and the ones built from a PATH ROOT are
/// written for a machine with nothing mounted between that root and the cache
/// folder. The root is kept in those rather than swapped for the call under test,
/// which would let the fixture agree with the code by construction.
///
/// The tests asking about the SYSTEM directory's own volume are a separate
/// matter and none of this touches them: a volume mounted at the cache folder
/// does not sit between <c>C:\</c> and <c>C:\Windows\System32</c>, so a path
/// root answers for them on that machine as well as on any other.
///
/// AND AN ASSERTION RESTS ON NEITHER WHEN IT MOVES THE CACHE ROOT ITSELF,
/// WHICH IS THE PROPERTY A READER SHOULD APPLY RATHER THAN A LIST TO TRUST.
/// <c>IsOnInstallerCacheDrive</c> takes a test-only cache-root override, so a
/// fixture can put the cache root where the system directory is not without
/// owning a second disk. That is what lets an assertion here go red against the
/// code this change replaced: leave the cache root alone and the two are the
/// same string, so the fixture answers the same whichever version is underneath.
/// <see cref="IsOnInstallerCacheDrive_asks_the_cache_root_it_is_given_and_not_the_system_directory"/>
/// is written for it.
/// </remarks>
public class MoveSpaceCheckIntegrationTests
{
    /// <summary>
    /// Pins that the cache-side query is asked and answered, on a host where the
    /// cache folder's path root and the cache's own volume are the same string.
    /// </summary>
    [Fact]
    public void IsOnInstallerCacheDrive_is_true_for_a_folder_under_the_cache_path_root()
    {
        // THE SUBJECT IS THE CACHE'S VOLUME AND THE NAME SAYS SO. The method
        // asks which volume the installer cache is on, not which volume the
        // Windows system directory is on. Those are the same string unless a
        // volume is mounted at C:\Windows\Installer, which is the machine the
        // method was rewritten for and the reason it no longer asks about the
        // system directory.
        //
        // THE FIXTURE COMPUTES A PATH ROOT AND THE METHOD COMPARES VOLUMES, AND
        // THE NAME NOW SAYS WHICH. Path.GetPathRoot answers C:\ for the cache
        // folder whatever is mounted there, which is the arithmetic
        // MoveSpaceCheck exists to have abolished. It is kept here on purpose:
        // building the fixture from GetVolumeMountPoint would have it compute
        // its expectation with the call under test, and a fixture that agrees
        // with the code by construction cannot fail at what its name claims.
        //
        // SO THE ASSERTION IS WRITTEN FOR A PATH ROOT AND A CACHE VOLUME THAT
        // ARE THE SAME STRING, which is the shape of a machine with nothing
        // mounted between them. That is the condition it holds under, and it
        // follows from the fixture keeping the path root rather than asking the
        // method.
        //
        // What it pins is that the cache-side query answers at all. Before the
        // fix this method never touched the cache path; it does now, and a
        // GetVolumePathName that would not answer for it returns null, so the
        // method answers false for every destination on the machine, the
        // free-space check runs on a move that is a rename, and the Move this
        // app exists for is refused for want of space it does not need. Whether
        // anything about that folder could make the call refuse is not asserted
        // here. This test is what answers it, on a real Windows host.
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
        // MACHINE-INDEPENDENT BECAUSE THE FIXTURE IS THE CACHE FOLDER AND NOT
        // ITS PATH ROOT, which is the property rather than a tally: an assertion
        // built under the cache folder holds whatever is mounted where, and one
        // built from a path root holds only where nothing is mounted between the
        // root and the folder. The cache folder is on its own volume, so this is
        // true with a volume mounted at C:\Windows\Installer and true without
        // one, that mounted machine included. Under the code this replaced it was
        // true only where the cache and the system directory shared a volume, and
        // false on the machine the fix is for.
        //
        // WHAT IT TURNS ON IS THE QUERY ANSWERING FOR THE CACHE PATH. A
        // GetVolumePathName that would not answer for the cache folder returns
        // null and the method answers false, so a green here is the call
        // reaching an answer and not an identity. The test above says what
        // elevation does to that.
        Assert.True(MoveSpaceCheck.IsOnInstallerCacheDrive(InstallerCacheHelpers.InstallerFolder));
    }

    [Fact]
    public void IsOnInstallerCacheDrive_asks_the_cache_root_it_is_given_and_not_the_system_directory()
    {
        // WHAT LETS AN ASSERTION TELL THIS METHOD FROM THE ONE IT REPLACED, and
        // the reason the override parameter exists. A fixture that leaves the
        // cache root alone cannot: the cache's volume and the system directory's
        // are the same string on every host this suite runs on, so it answers the
        // same whichever version is underneath. This one moves the cache root off
        // that volume, which is what no fixture can do to the real cache folder
        // without a second disk and administrator rights.
        //
        // AN UNMOUNTED LETTER, so it asks nothing of the host but a free one.
        // GetVolumePathName cannot answer for it, the cache side returns null and
        // the method answers false. The code this replaced never read the cache
        // root at all: it asked the system directory, which does answer, so it
        // returns true for both destinations below and this goes red against it.
        // The first does that on any machine; the second does it on one where
        // nothing is mounted at the cache.
        //
        // WHICH OF THE METHOD'S TWO ROUTES TO FALSE THIS ONE TAKES. An unmounted
        // letter is a path GetVolumePathName cannot answer for, so the cache side
        // returns null and the method answers false on that alone. The other
        // route is two volumes that both resolve and are found to differ, which
        // is a separate way through the same method and a separate reason for the
        // same answer.
        var unmounted = TestHost.FirstUnmountedDriveLetter();
        if (unmounted is null)
            return; // every letter is in use on this host

        Assert.False(MoveSpaceCheck.IsOnInstallerCacheDrive(
            Environment.GetFolderPath(Environment.SpecialFolder.System),
            installerCacheRoot: $@"{unmounted}:\nope"));

        // The path the test above answers TRUE for, answered false here because
        // the cache root moved and nothing else did. One destination, two cache
        // roots, two answers, which is the whole of what the parameter buys.
        Assert.False(MoveSpaceCheck.IsOnInstallerCacheDrive(
            InstallerCacheHelpers.InstallerFolder,
            installerCacheRoot: $@"{unmounted}:\nope"));
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
        // call replaced the arithmetic.
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
