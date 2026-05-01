using InstallerClean.ViewModels;

namespace InstallerClean.Tests.ViewModels;

public class DescribeWriteFailureTests
{
    private const string Dest = @"D:\backup\installer";

    [Fact]
    public void UnauthorizedAccess_mentions_permission_and_dest()
    {
        var msg = CleanupViewModel.DescribeWriteFailure(
            Dest, new UnauthorizedAccessException("denied"));

        Assert.Contains("permission", msg, StringComparison.OrdinalIgnoreCase);
        Assert.Contains(Dest, msg);
    }

    [Fact]
    public void PathTooLong_mentions_shorter_path()
    {
        var msg = CleanupViewModel.DescribeWriteFailure(
            Dest, new PathTooLongException("too long"));

        Assert.Contains("long", msg, StringComparison.OrdinalIgnoreCase);
        Assert.Contains(Dest, msg);
    }

    [Fact]
    public void DirectoryNotFound_mentions_drive_or_network()
    {
        var msg = CleanupViewModel.DescribeWriteFailure(
            Dest, new DirectoryNotFoundException("missing"));

        Assert.Contains("does not exist", msg, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void IOException_carries_inner_message()
    {
        var msg = CleanupViewModel.DescribeWriteFailure(
            Dest, new IOException("the device is not ready"));

        Assert.Contains("the device is not ready", msg);
    }

    [Fact]
    public void Unknown_exception_falls_through_to_generic_wording()
    {
        var msg = CleanupViewModel.DescribeWriteFailure(
            Dest, new InvalidOperationException("some other thing"));

        Assert.Contains("some other thing", msg);
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
            var msg = CleanupViewModel.DescribeWriteFailure(Dest, ex);
            Assert.Contains(Dest, msg);
        }
    }
}
