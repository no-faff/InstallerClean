using System.ComponentModel;
using System.Diagnostics;
using InstallerClean.Services;
using InstallerClean.Tests.Helpers;

namespace InstallerClean.Tests.Services.Integration;

/// <summary>
/// <see cref="ProcessStartRetry"/> against a file that does not exist, which Windows
/// refuses to start every time.
/// </summary>
public sealed class ProcessStartRetryTests : IDisposable
{
    private readonly IDisposable _crashLog = TestCrashLog.OwnFolder();

    public void Dispose() => _crashLog.Dispose();

    private static ProcessStartInfo Missing() =>
        new(TestTemp.NewFilePath("InstallerClean-missing-", ".exe"))
        {
            UseShellExecute = false,
        };

    [Fact]
    public void A_start_that_always_fails_is_tried_every_time_with_a_wait_between_tries_and_the_last_failure_thrown()
    {
        var info = Missing();
        var waits = new List<TimeSpan>();
        Assert.Throws<Win32Exception>(() => ProcessStartRetry.Start(info, _ => true, waits.Add));

        Assert.Equal(Enumerable.Repeat(ProcessStartRetry.Interval, ProcessStartRetry.Attempts - 1), waits);
        // Each failed try but the last is written; the last is the caller's to write.
        Assert.Equal(ProcessStartRetry.Attempts - 1, TestCrashLog.FailedStartsOf(info.FileName));
    }

    [Fact]
    public void A_failure_the_caller_does_not_retry_is_thrown_at_the_first_try_with_no_wait_and_nothing_written()
    {
        var info = Missing();
        var waits = new List<TimeSpan>();
        Assert.Throws<Win32Exception>(() => ProcessStartRetry.Start(info, _ => false, waits.Add));

        Assert.Empty(waits);
        Assert.Empty(TestCrashLog.Text());
    }

    [Fact]
    public void A_failure_is_written_before_a_wait_that_throws_and_the_throw_ends_the_tries()
    {
        var info = Missing();
        var tries = 0;
        var waitedFor = TimeSpan.Zero;

        Assert.Throws<OperationCanceledException>(() => ProcessStartRetry.Start(
            info,
            _ => { tries++; return true; },
            interval =>
            {
                waitedFor = interval;
                throw new OperationCanceledException();
            }));

        Assert.Equal(1, tries);
        Assert.Equal(ProcessStartRetry.Interval, waitedFor);
        Assert.Equal(1, TestCrashLog.FailedStartsOf(info.FileName));
    }

    [Fact]
    public void A_failure_the_caller_stops_retrying_is_thrown_at_that_try()
    {
        var info = Missing();
        var asked = 0;
        var waits = 0;
        // A failure short of the last try, which the caller refuses to try again.
        var refusedAt = ProcessStartRetry.Attempts - 1;

        Assert.Throws<Win32Exception>(() => ProcessStartRetry.Start(
            info,
            _ => ++asked < refusedAt,
            _ => waits++));

        Assert.Equal(refusedAt, asked);
        Assert.Equal(refusedAt - 1, waits);
        Assert.Equal(refusedAt - 1, TestCrashLog.FailedStartsOf(info.FileName));
    }

    [Fact]
    public void A_refused_start_names_the_file_and_the_folder_as_dotnet_10_does()
    {
        var info = Missing();
        var thrown = Assert.Throws<Win32Exception>(() => ProcessStartRetry.Start(info, _ => false, _ => { }));

        // ERROR_FILE_NOT_FOUND, in the text .NET 10 gives it.
        Assert.Equal(2, thrown.NativeErrorCode);
        Assert.StartsWith(
            $"An error occurred trying to start process '{info.FileName}' with working directory '{Environment.CurrentDirectory}'. ",
            thrown.Message,
            StringComparison.Ordinal);
        Assert.EndsWith("The system cannot find the file specified.", thrown.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void A_start_from_a_named_folder_names_that_folder()
    {
        var info = Missing();
        info.WorkingDirectory = TestTemp.NewFolder("InstallerClean-start-folder-");
        var thrown = Assert.Throws<Win32Exception>(() => ProcessStartRetry.Start(info, _ => false, _ => { }));

        Assert.StartsWith(
            $"An error occurred trying to start process '{info.FileName}' with working directory '{info.WorkingDirectory}'. ",
            thrown.Message,
            StringComparison.Ordinal);
    }

    [Fact]
    public void A_file_that_is_not_a_program_is_answered_with_dotnet_10s_own_text()
    {
        var file = TestTemp.NewFilePath("InstallerClean-not-a-program-", ".exe");
        File.WriteAllText(file, "not a program");
        var info = new ProcessStartInfo(file) { UseShellExecute = false };
        var thrown = Assert.Throws<Win32Exception>(() => ProcessStartRetry.Start(info, _ => false, _ => { }));

        // ERROR_BAD_EXE_FORMAT or ERROR_EXE_MACHINE_TYPE_MISMATCH, as Windows reads the file;
        // .NET 10 gives both the one text.
        Assert.Contains(thrown.NativeErrorCode, new[] { 193, 216 });
        Assert.EndsWith(
            "'. The specified executable is not a valid application for this OS platform.",
            thrown.Message,
            StringComparison.Ordinal);
    }
}
