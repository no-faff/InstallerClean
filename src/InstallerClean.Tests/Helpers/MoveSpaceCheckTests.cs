using InstallerClean.Helpers;

namespace InstallerClean.Tests.Helpers;

/// <summary>
/// The decision both hosts make before a Move: is there room.
/// </summary>
/// <remarks>
/// WHAT IS LEFT HERE IS NARROWER THAN IT WAS, AND THE LINE MOVED IN 3.0.0.
/// <c>IsOnInstallerCacheDrive</c> used to compare two path roots and could be
/// tested as arithmetic; it now asks Windows which volume a folder is on, so
/// every case that reaches that question has gone to
/// <c>Helpers.Integration.MoveSpaceCheckIntegrationTests</c> along with the
/// ancestor walk that was already there.
///
/// What stays is what still touches nothing: the empty guards, the share that
/// is turned away on its spelling, and the arithmetic of "is this less than
/// that". EVERY DESTINATION BELOW IS A SHARE OR A BLANK ON PURPOSE, so the
/// arithmetic is exercised without a volume query deciding the outcome first,
/// and so a runner whose letters differ cannot change what these tests mean.
///
/// THAT LEAVES ONE SIDE OF A SPLIT UNCOVERED IN THIS FILE and it is said here
/// rather than left for a reader to notice: nothing below sets up a destination
/// that IS on the cache volume, which is the arm where RefusalFreeSpace returns
/// null without consulting the measurement at all. That arm is
/// <c>RefusalFreeSpace_allows_a_same_drive_move_with_no_room_at_all</c> in the
/// integration file, where it can be built from a real volume.
///
/// AND WHAT IT IS BUILT FROM THERE MATTERS TO THIS SENTENCE, WHICH IS WHY IT IS
/// SAID HERE AND NOT ONLY THERE. That test named the system directory's root
/// until the method stopped asking about the system directory's volume, so this
/// file was pointing at a test whose subject was the wrong one. It now names the
/// cache folder's root, and on every host the suite runs on those two are the
/// same string. So the arm is covered and the VOLUME is not: a destination that
/// is on the cache's volume and not on the system's needs a volume mounted at
/// <c>C:\Windows\Installer</c>, which nothing here or there can build.
/// </remarks>
public class MoveSpaceCheckTests
{
    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    // A share is not a local volume, so it is not the one the cache is on, and
    // no query could make it so. Answered from the spelling, which is why this
    // case is still arithmetic when the rest are not.
    [InlineData(@"\\server\backup")]
    [InlineData("//server/backup")]
    [InlineData(@"\\?\UNC\server\backup")]
    public void IsOnInstallerCacheDrive_is_false_when_it_cannot_prove_otherwise(string destination) =>
        Assert.False(MoveSpaceCheck.IsOnInstallerCacheDrive(destination));

    [Fact]
    public void RefusalFreeSpace_refuses_and_reports_what_is_there()
    {
        Assert.Equal(500L, MoveSpaceCheck.RefusalFreeSpace(@"\\server\backup", 1_000, 500));
    }

    [Theory]
    // Exactly enough is enough: the refusal is strictly "less than".
    [InlineData(1_000L)]
    [InlineData(1_001L)]
    public void RefusalFreeSpace_allows_a_destination_with_room(long free) =>
        Assert.Null(MoveSpaceCheck.RefusalFreeSpace(@"\\server\backup", 1_000, free));

    [Fact]
    public void RefusalFreeSpace_allows_an_unmeasurable_destination()
    {
        // A share the caller cannot query has established nothing, so it makes
        // no claim in either direction rather than blocking on a number it does
        // not have.
        Assert.Null(MoveSpaceCheck.RefusalFreeSpace(@"\\server\backup", 1_000, null));
    }
}
