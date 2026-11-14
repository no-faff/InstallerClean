using InstallerClean.Helpers;

namespace InstallerClean.Tests.Helpers;

/// <summary>
/// The decision both hosts make before a Move: is there room.
/// </summary>
/// <remarks>
/// <c>RenamesOnInstallerCacheDrive</c> asks Windows which volume a folder is on, so every
/// case that reaches that question is in
/// <c>Helpers.Integration.MoveSpaceCheckIntegrationTests</c>, with the ancestor walk.
///
/// What is here is what touches nothing: the empty guards, the share that is
/// turned away on its spelling, and the arithmetic of "is this less than that".
/// EVERY DESTINATION BELOW IS A SHARE OR A BLANK ON PURPOSE, so the arithmetic is
/// exercised without a volume query deciding the outcome first, and so a runner
/// whose letters differ cannot change what these tests mean.
///
/// The arm where RefusalFreeSpace returns null without consulting the measurement,
/// a destination that IS on the cache volume, is
/// <c>RefusalFreeSpace_allows_a_same_drive_move_with_no_room_at_all</c> in the
/// integration file, built under the installer cache folder itself so its
/// destination is on the cache's volume on any machine.
/// </remarks>
public class MoveSpaceCheckTests
{
    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    // A Move through a share copies every file, onto whatever volume the share
    // is, so it is never a rename. Answered from the spelling, which is why this
    // case is still arithmetic when the rest are not.
    [InlineData(@"\\server\backup")]
    [InlineData("//server/backup")]
    [InlineData(@"\\?\UNC\server\backup")]
    public void RenamesOnInstallerCacheDrive_is_false_for_nothing_and_for_a_share(string destination) =>
        Assert.False(MoveSpaceCheck.RenamesOnInstallerCacheDrive(destination));

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
