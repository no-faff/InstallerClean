using InstallerClean.Helpers;
using InstallerClean.Models;

namespace InstallerClean.Tests.Helpers;

/// <summary>
/// The label <see cref="MoveDestinationClassifier"/> gives a Move destination for the result log
/// and the opt-in report.
/// </summary>
/// <remarks>
/// A SHARE IS LABELLED BY THE VOLUME QUESTION FIRST. A Move through this PC's own share,
/// or a drive mapped onto it, lands on the cache's own volume, so the classifier asks the
/// resolve it is handed before calling a share a share. The fixed and removable arms go on
/// to ask what is mounted where and want real storage to answer. The same-drive arm is
/// covered end to end by
/// <c>CleanupPreFlightTests.A_move_to_the_same_drive_tells_the_confirmation_it_frees_no_space</c>
/// with the predicate underneath it in
/// <c>Helpers.Integration.MoveSpaceCheckIntegrationTests</c>, and the share side of the
/// predicate in <c>MoveSpaceCheckShareTests</c>.
/// </remarks>
public class MoveDestinationClassifierTests
{
    /// <summary>
    /// The volume question for the empty-destination theory below, which answers before
    /// asking it: reaching this is the failure that theory exists to catch.
    /// </summary>
    private static bool? TheVolumeIsNeverAsked(string dest) =>
        throw new InvalidOperationException(
            $"the volume was asked about '{dest}', which is not a path");

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
    public void Classify_calls_a_share_on_another_volume_a_share(string dest) =>
        Assert.Equal(MoveDestinationKinds.UncShare,
            MoveDestinationClassifier.Classify(dest, _ => false));

    [Theory]
    [InlineData(@"\\localhost\C$\Moved")]
    [InlineData(@"\\?\UNC\localhost\C$\Moved")]
    public void Classify_calls_a_share_on_the_cache_volume_the_same_drive(string dest) =>
        Assert.Equal(MoveDestinationKinds.SameDrive,
            MoveDestinationClassifier.Classify(dest, _ => true));

    [Fact]
    public void Classify_says_unknown_for_a_share_whose_volume_was_not_settled() =>
        Assert.Equal(MoveDestinationKinds.Unknown,
            MoveDestinationClassifier.Classify(@"\\localhost\C$\Moved", _ => null));

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Classify_says_unknown_when_there_is_nothing_to_classify(string dest) =>
        Assert.Equal(MoveDestinationKinds.Unknown,
            MoveDestinationClassifier.Classify(dest, TheVolumeIsNeverAsked));
}
