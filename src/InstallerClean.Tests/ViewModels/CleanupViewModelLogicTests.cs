using InstallerClean.Models;
using InstallerClean.ViewModels;

namespace InstallerClean.Tests.ViewModels;

/// <summary>
/// The two static answers <see cref="CleanupViewModel"/> gives about a
/// destination, both of them pure path or string work with no service behind
/// them. <see cref="CompletionViewModelTests"/> covers what the completion
/// screen does with a <see cref="MoveSpaceOutcome"/>; this covers the half that
/// decides which one it gets, which is the half that can be got wrong without
/// anything looking different until somebody moves files to a share.
/// </summary>
public class CleanupViewModelLogicTests
{
    [Theory]
    // A share frees space on the system drive as surely as another disk does:
    // the files have left it. This is the arm that reads as a special case and
    // is not one.
    [InlineData(MoveDestinationKinds.UncShare, MoveSpaceOutcome.FreedSpace)]
    [InlineData(MoveDestinationKinds.DifferentFixedDrive, MoveSpaceOutcome.FreedSpace)]
    [InlineData(MoveDestinationKinds.RemovableDrive, MoveSpaceOutcome.FreedSpace)]
    // A rename: the bytes are still on the drive until the folder goes, so the
    // heading claims nothing and the line beneath says when the space returns.
    [InlineData(MoveDestinationKinds.SameDrive, MoveSpaceOutcome.SameDrive)]
    // Only a volume the classification could not read declines to say either
    // way, and it is the default rather than a listed kind, so a destination
    // kind added later lands here instead of silently claiming freed space.
    [InlineData(MoveDestinationKinds.Unknown, MoveSpaceOutcome.Unclassified)]
    [InlineData("a kind nothing has ever emitted", MoveSpaceOutcome.Unclassified)]
    public void ClassifySpaceOutcome_maps_every_destination_kind(string kind, MoveSpaceOutcome expected) =>
        Assert.Equal(expected, CleanupViewModel.ClassifySpaceOutcome(kind));

    [Fact]
    public void IsOnInstallerCacheDrive_is_true_only_for_the_system_drive()
    {
        var systemRoot = Path.GetPathRoot(
            Environment.GetFolderPath(Environment.SpecialFolder.System))!;

        Assert.True(CleanupViewModel.IsOnInstallerCacheDrive(Path.Combine(systemRoot, "backup")));
        // Case and separator shape must not change the answer: the box takes
        // whatever the user typed.
        Assert.True(CleanupViewModel.IsOnInstallerCacheDrive(
            Path.Combine(systemRoot.ToLowerInvariant(), "backup") + Path.DirectorySeparatorChar));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    // A share has no drive letter to compare with, so it has not been shown to
    // be the system drive. False is the safe way round: the tooltip then omits
    // a claim rather than making a wrong one.
    [InlineData(@"\\server\backup")]
    public void IsOnInstallerCacheDrive_is_false_when_it_cannot_prove_otherwise(string destination) =>
        Assert.False(CleanupViewModel.IsOnInstallerCacheDrive(destination));
}
