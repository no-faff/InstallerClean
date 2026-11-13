using InstallerClean.Helpers;
using InstallerClean.ViewModels;

namespace InstallerClean.Tests.ViewModels;

public class DescribeWriteFailureTests
{
    private const string Dest = @"D:\backup\installer";
    private const string LogPath = @"C:\Users\Test\AppData\Local\InstallerClean\crash.log";

    private static Func<CrashLogEntry> WrittenTo(string path) => () => new CrashLogEntry(path, true);

    private static readonly Func<CrashLogEntry> NotWritten = () => new CrashLogEntry(string.Empty, false);

    private static readonly Dictionary<string, (Func<Exception> Create, bool NamesLog)> Failures = new()
    {
        [nameof(UnauthorizedAccessException)] = (() => new UnauthorizedAccessException("x"), false),
        [nameof(PathTooLongException)] = (() => new PathTooLongException("x"), false),
        [nameof(DirectoryNotFoundException)] = (() => new DirectoryNotFoundException("x"), false),
        [nameof(FileNotFoundException)] = (() => new FileNotFoundException("x"), true),
        [nameof(DriveNotFoundException)] = (() => new DriveNotFoundException("x"), true),
        [nameof(IOException)] = (() => new IOException("x"), true),
        [nameof(InvalidOperationException)] = (() => new InvalidOperationException("x"), true),
    };

    public static TheoryData<string> FailureNames => new(Failures.Keys);

    [Theory]
    [MemberData(nameof(FailureNames))]
    public void The_crash_log_is_written_exactly_where_the_message_names_it(string failureName)
    {
        var (create, namesLog) = Failures[failureName];
        var written = 0;

        var message = CleanupViewModel.DescribeWriteFailure(
            Dest, create(), () => { written++; return new CrashLogEntry(LogPath, true); });

        Assert.Equal(namesLog ? 1 : 0, written);
        Assert.Equal(namesLog, message.Contains(LogPath));
    }

    [Fact]
    public void UnauthorizedAccess_mentions_permission_and_dest()
    {
        var msg = CleanupViewModel.DescribeWriteFailure(
            Dest, new UnauthorizedAccessException("denied"), WrittenTo(LogPath));

        Assert.Contains("permission", msg, StringComparison.OrdinalIgnoreCase);
        Assert.Contains(Dest, msg);
    }

    [Fact]
    public void PathTooLong_mentions_shorter_path()
    {
        var msg = CleanupViewModel.DescribeWriteFailure(
            Dest, new PathTooLongException("too long"), WrittenTo(LogPath));

        Assert.Contains("long", msg, StringComparison.OrdinalIgnoreCase);
        Assert.Contains(Dest, msg);
    }

    [Fact]
    public void DirectoryNotFound_mentions_drive_or_network()
    {
        var msg = CleanupViewModel.DescribeWriteFailure(
            Dest, new DirectoryNotFoundException("missing"), WrittenTo(LogPath));

        Assert.Contains("does not exist", msg, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void IOException_does_not_leak_inner_message_but_does_show_log_path()
    {
        var msg = CleanupViewModel.DescribeWriteFailure(
            Dest, new IOException("the device is not ready"), WrittenTo(LogPath));

        Assert.DoesNotContain("the device is not ready", msg);
        Assert.Contains(LogPath, msg);
        Assert.Contains(Dest, msg);
    }

    [Fact]
    public void IOException_with_no_log_substitutes_log_failure_message()
    {
        var msg = CleanupViewModel.DescribeWriteFailure(
            Dest, new IOException("the device is not ready"), NotWritten);

        Assert.DoesNotContain("the device is not ready", msg);
        Assert.DoesNotContain(LogPath, msg);
        Assert.Contains(Dest, msg);
        Assert.Contains("crash log could not be written", msg, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Unknown_exception_falls_through_to_generic_wording_without_inner_message()
    {
        var msg = CleanupViewModel.DescribeWriteFailure(
            Dest, new InvalidOperationException("some other thing"), WrittenTo(LogPath));

        Assert.DoesNotContain("some other thing", msg);
        Assert.Contains(LogPath, msg);
        Assert.Contains(Dest, msg);
    }

    [Fact]
    public void Unknown_exception_with_no_log_substitutes_log_failure_message()
    {
        var msg = CleanupViewModel.DescribeWriteFailure(
            Dest, new InvalidOperationException("some other thing"), NotWritten);

        Assert.DoesNotContain("some other thing", msg);
        Assert.DoesNotContain(LogPath, msg);
        Assert.Contains(Dest, msg);
        Assert.Contains("crash log could not be written", msg, StringComparison.OrdinalIgnoreCase);
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
            var msg = CleanupViewModel.DescribeWriteFailure(Dest, ex, WrittenTo(LogPath));
            Assert.Contains(Dest, msg);
        }
    }
}
