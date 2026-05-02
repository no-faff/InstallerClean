using InstallerClean.ViewModels;

namespace InstallerClean.Tests.ViewModels;

public class DescribeWriteFailureTests
{
    private const string Dest = @"D:\backup\installer";
    private const string LogPath = @"C:\Users\Test\AppData\Local\NoFaff\InstallerClean\crash.log";

    [Fact]
    public void UnauthorizedAccess_mentions_permission_and_dest()
    {
        var msg = CleanupViewModel.DescribeWriteFailure(
            Dest, new UnauthorizedAccessException("denied"), LogPath);

        Assert.Contains("permission", msg, StringComparison.OrdinalIgnoreCase);
        Assert.Contains(Dest, msg);
    }

    [Fact]
    public void PathTooLong_mentions_shorter_path()
    {
        var msg = CleanupViewModel.DescribeWriteFailure(
            Dest, new PathTooLongException("too long"), LogPath);

        Assert.Contains("long", msg, StringComparison.OrdinalIgnoreCase);
        Assert.Contains(Dest, msg);
    }

    [Fact]
    public void DirectoryNotFound_mentions_drive_or_network()
    {
        var msg = CleanupViewModel.DescribeWriteFailure(
            Dest, new DirectoryNotFoundException("missing"), LogPath);

        Assert.Contains("does not exist", msg, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void IOException_does_not_leak_inner_message_but_does_show_log_path()
    {
        // Path-leak rule: under elevation an IOException's .Message can
        // include paths from outside the user's typed destination
        // (lock-holder paths, NTFS resolution chains). Confirm the inner
        // message does NOT make it to the dialog and the log path DOES.
        var msg = CleanupViewModel.DescribeWriteFailure(
            Dest, new IOException("the device is not ready"), LogPath);

        Assert.DoesNotContain("the device is not ready", msg);
        Assert.Contains(LogPath, msg);
        Assert.Contains(Dest, msg);
    }

    [Fact]
    public void Unknown_exception_falls_through_to_generic_wording_without_inner_message()
    {
        var msg = CleanupViewModel.DescribeWriteFailure(
            Dest, new InvalidOperationException("some other thing"), LogPath);

        Assert.DoesNotContain("some other thing", msg);
        Assert.Contains(LogPath, msg);
        Assert.Contains(Dest, msg);
    }

    [Fact]
    public void Every_branch_includes_the_destination_text()
    {
        Exception[] cases =
        {
            new UnauthorizedAccessException(),
            new PathTooLongException(),
            new DirectoryNotFoundException(),
            new IOException("disk"),
            new InvalidOperationException("unexpected"),
        };

        foreach (var ex in cases)
        {
            var msg = CleanupViewModel.DescribeWriteFailure(Dest, ex, LogPath);
            Assert.Contains(Dest, msg);
        }
    }
}
