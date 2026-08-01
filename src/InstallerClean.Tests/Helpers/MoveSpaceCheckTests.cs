using InstallerClean.Helpers;

namespace InstallerClean.Tests.Helpers;

/// <summary>
/// The decision both hosts make before a Move: is there room. Pure path and
/// arithmetic work here; the ancestor walk that measures a destination which
/// does not exist yet touches a real volume and is in
/// <c>Helpers.Integration.MoveSpaceCheckIntegrationTests</c>.
/// </summary>
public class MoveSpaceCheckTests
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

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    // A share has no drive letter to compare with, so it has not been shown to
    // be the system drive. False is the safe way round: a caller then omits a
    // claim rather than making a wrong one.
    [InlineData(@"\\server\backup")]
    public void IsOnInstallerCacheDrive_is_false_when_it_cannot_prove_otherwise(string destination) =>
        Assert.False(MoveSpaceCheck.IsOnInstallerCacheDrive(destination));

    [Fact]
    public void RefusalFreeSpace_refuses_and_reports_what_is_there()
    {
        Assert.Equal(500L, MoveSpaceCheck.RefusalFreeSpace(@"D:\backup", 1_000, 500));
    }

    [Theory]
    // Exactly enough is enough: the refusal is strictly "less than".
    [InlineData(1_000L)]
    [InlineData(1_001L)]
    public void RefusalFreeSpace_allows_a_destination_with_room(long free) =>
        Assert.Null(MoveSpaceCheck.RefusalFreeSpace(@"D:\backup", 1_000, free));

    [Fact]
    public void RefusalFreeSpace_allows_an_unmeasurable_destination()
    {
        // A share the caller cannot query has established nothing, so it makes
        // no claim in either direction rather than blocking on a number it does
        // not have.
        Assert.Null(MoveSpaceCheck.RefusalFreeSpace(@"\\server\backup", 1_000, null));
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
}
