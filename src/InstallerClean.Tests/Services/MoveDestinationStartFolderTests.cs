using System.Diagnostics;
using InstallerClean.Services;

namespace InstallerClean.Tests.Services;

/// <summary>
/// The folder the destination picker starts in: the Move box's text where it names a folder,
/// and no start folder where it does not, where the check throws, or where the check has not
/// answered within the bound, while the thread that asked goes on running.
/// </summary>
public class MoveDestinationStartFolderTests
{
    private static readonly TimeSpan Bound = TimeSpan.FromMilliseconds(300);

    [Fact]
    public async Task A_folder_that_exists_is_the_start_folder_trimmed()
    {
        Assert.Equal(@"D:\Moved", await ConfirmationService.StartFolderAsync(@"  D:\Moved ", _ => true, Bound));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task An_empty_box_gives_no_start_folder_and_asks_nothing(string? text)
    {
        var asked = false;
        Assert.Null(await ConfirmationService.StartFolderAsync(text, _ => asked = true, Bound));
        Assert.False(asked);
    }

    [Fact]
    public async Task A_path_that_is_not_a_folder_gives_no_start_folder()
    {
        Assert.Null(await ConfirmationService.StartFolderAsync(@"D:\Moved", _ => false, Bound));
    }

    [Fact]
    public async Task A_check_that_throws_gives_no_start_folder()
    {
        Assert.Null(await ConfirmationService.StartFolderAsync(
            @"D:\Moved", _ => throw new OperationCanceledException(), Bound));
    }

    [Fact]
    public async Task A_check_that_does_not_answer_gives_no_start_folder_at_the_bound()
    {
        using var held = new ManualResetEventSlim();
        try
        {
            var clock = Stopwatch.StartNew();
            var started = ConfirmationService.StartFolderAsync(@"\\gone\share", _ => held.Wait(TimeSpan.FromSeconds(30)), Bound);

            // The call hands back before the check answers, so the caller's thread is free.
            Assert.False(started.IsCompleted);
            Assert.Null(await started);
            Assert.InRange(clock.Elapsed, Bound - TimeSpan.FromMilliseconds(50), TimeSpan.FromSeconds(10));
        }
        finally
        {
            held.Set();
        }
    }
}
