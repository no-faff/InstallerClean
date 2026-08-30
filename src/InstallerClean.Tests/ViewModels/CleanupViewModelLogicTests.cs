using InstallerClean.Models;
using InstallerClean.ViewModels;

namespace InstallerClean.Tests.ViewModels;

/// <summary>
/// The static answer <see cref="CleanupViewModel"/> gives about a destination.
/// <see cref="CompletionViewModelTests"/> covers what the completion screen does
/// with a <see cref="MoveSpaceOutcome"/>; this covers the half that decides
/// which one it gets, which is the half that can be got wrong without anything
/// looking different until somebody moves files to a share.
/// </summary>
/// <remarks>
/// THE ARMS THAT ANSWER FROM THE PATH'S SPELLING ARE HERE, and they are worth
/// pinning because they are the ones that run BEFORE anything goes to the
/// network. From 3.0.0 ClassifyMoveDestination reads the volume a folder is on
/// rather than the letter its path starts with, so the fixed and removable arms
/// go on to ask what is mounted where and want real storage to answer. The
/// same-drive arm takes its answer from the resolve the method is handed, and it
/// is covered end to end by
/// <c>CleanupPreFlightTests.A_move_to_the_same_drive_tells_the_confirmation_it_frees_no_space</c>
/// with the predicate underneath it in
/// <c>Helpers.Integration.MoveSpaceCheckIntegrationTests</c>.
///
/// AND "ANSWERS FROM THE SPELLING" IS PROVED HERE RATHER THAN READ. Both
/// theories hand the classifier a resolve that throws, so an arm that consulted
/// the volume before answering would fail rather than return the right kind by a
/// route these tests cannot see.
/// </remarks>
public class CleanupViewModelLogicTests
{
    /// <summary>
    /// The volume question, handed to <c>ClassifyMoveDestination</c> by the two
    /// theories below. Both cover an arm that answers from the spelling of the
    /// path, and reaching this is the failure those theories exist to catch:
    /// a reordering that asked the volume first would still return the right
    /// kind, and would have moved those answers onto the network to get it.
    /// </summary>
    private static bool? TheVolumeIsNeverAsked(string dest) =>
        throw new InvalidOperationException(
            $"the volume was asked about '{dest}' on an arm that answers from the path's spelling");

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

    [Theory]
    [InlineData(@"\\server\backup")]
    // Both separators and both device prefixes, for the reason
    // StorageHelpersRemotePathTests gives at length: a share that reads as local
    // here is a share sent to a call Win32 validates over the network. The
    // classifier runs inside the pre-flight, off the dispatcher, so the cost is
    // not the hazard; the wrong LABEL in the result log is, because that field
    // is one of the few things this project knows about real machines.
    [InlineData("//server/backup")]
    [InlineData(@"\\?\UNC\server\backup")]
    public void ClassifyMoveDestination_calls_a_share_a_share(string dest) =>
        Assert.Equal(MoveDestinationKinds.UncShare,
            CleanupViewModel.ClassifyMoveDestination(dest, TheVolumeIsNeverAsked));

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void ClassifyMoveDestination_says_unknown_when_there_is_nothing_to_classify(string dest) =>
        Assert.Equal(MoveDestinationKinds.Unknown,
            CleanupViewModel.ClassifyMoveDestination(dest, TheVolumeIsNeverAsked));
}
