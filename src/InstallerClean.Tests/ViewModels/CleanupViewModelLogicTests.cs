using InstallerClean.Models;
using InstallerClean.ViewModels;

namespace InstallerClean.Tests.ViewModels;

/// <summary>
/// The static answer <see cref="CleanupViewModel"/> gives about a destination kind.
/// <see cref="CompletionViewModelTests"/> covers what the completion screen does
/// with a <see cref="MoveSpaceOutcome"/>; this covers the half that decides
/// which one it gets, which is the half that can be got wrong without anything
/// looking different until somebody moves files to a share. The label that decides
/// the kind is covered by <c>MoveDestinationClassifierTests</c>.
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

    [Theory]
    // Files done of files in the batch, and the percentage the label shows: the exact
    // share to the nearest whole number, a tie to the even one.
    [InlineData(1, 8, 12)]
    [InlineData(3, 8, 38)]
    [InlineData(5, 8, 62)]
    [InlineData(7, 8, 88)]
    [InlineData(1, 200, 0)]
    [InlineData(3, 200, 2)]
    [InlineData(29, 200, 14)]
    [InlineData(11, 40, 28)]
    [InlineData(23, 40, 58)]
    [InlineData(1, 3, 33)]
    [InlineData(2, 3, 67)]
    [InlineData(8, 8, 100)]
    [InlineData(0, 0, 0)]
    public void The_progress_label_rounds_a_halfway_percentage_to_the_even_number(int done, int total, int expected)
    {
        Assert.Equal(expected, CleanupViewModel.ShownPercent(done, total));
    }

    [Theory]
    // 199 of 200 is exactly 99.5, which rounds to the even 100; 1,999 of 2,000 is 99.95.
    [InlineData(199, 200, 99)]
    [InlineData(1_999, 2_000, 99)]
    [InlineData(200, 200, 100)]
    [InlineData(2_000, 2_000, 100)]
    public void The_progress_label_reads_100_only_once_the_last_file_is_done(int done, int total, int expected)
    {
        Assert.Equal(expected, CleanupViewModel.ShownPercent(done, total));
    }

    // The count a report names is the file in hand (MoveFilesServiceUnitTests and
    // DeleteFilesServiceUnitTests pin that each file is reported before it is acted on),
    // so the report naming the last file shows the files before it as done.
    [Theory]
    [InlineData(1, 1, 0)]
    [InlineData(1, 200, 0)]
    [InlineData(2, 200, 0)]
    [InlineData(200, 200, 99)]
    [InlineData(101, 200, 50)]
    public void The_progress_label_counts_the_file_in_hand_as_not_done(int current, int total, int expected)
    {
        Assert.Equal(expected, CleanupViewModel.ShownPercent(CleanupViewModel.FilesDone(current), total));
    }

#if NETFRAMEWORK
    // .NET Framework's own F0, which the label does not use, takes 12.5 up to 13.
    [Fact]
    public void The_framework_s_own_F0_rounds_a_halfway_percentage_up()
    {
        Assert.Equal("13", 12.5.ToString("F0", System.Globalization.CultureInfo.InvariantCulture));
    }
#endif
}
