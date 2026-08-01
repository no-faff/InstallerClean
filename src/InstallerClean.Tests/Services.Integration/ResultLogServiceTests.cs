using System.Text;
using InstallerClean.Models;
using InstallerClean.Services;

namespace InstallerClean.Tests.Services.Integration;

/// <summary>
/// The write-and-read half of <see cref="ResultLogService"/>, driven against a
/// %TEMP% sandbox through the internal path-taking constructor. That seam is
/// what makes the byte cap, the atomic write and the temp-file cleanup testable
/// at all: with the paths fixed to %LOCALAPPDATA%, exercising them would mean
/// writing over the real last-run.json in whichever profile ran the suite.
///
/// SendAsync's network path is deliberately not covered: it posts to a live No
/// Faff endpoint. Its two pre-network guards (an empty body, and a body over
/// the byte cap) are covered below, which is possible precisely because both
/// return before the request is built.
/// </summary>
public class ResultLogServiceTests : IDisposable
{
    private readonly string _folder;
    private readonly string _logFile;

    public ResultLogServiceTests()
    {
        // Its own folder per test class run, because WriteAsync creates the
        // parent and the temp files are siblings of the log.
        _folder = Path.Combine(Path.GetTempPath(), $"ic-resultlog-{Guid.NewGuid()}");
        _logFile = Path.Combine(_folder, "last-run.json");
    }

    public void Dispose()
    {
        if (Directory.Exists(_folder))
            Directory.Delete(_folder, recursive: true);
    }

    private static ResultLogEntry SampleEntry() =>
        ResultLogEntry.ForScanOnly(
            new ScanResult(Array.Empty<OrphanedFile>(), Array.Empty<RegisteredPackage>(), 0),
            scanDurationMs: 1234,
            pendingReboot: "none");

    private IEnumerable<string> TempFiles() =>
        Directory.Exists(_folder)
            ? Directory.EnumerateFiles(_folder, "*.tmp")
            : Array.Empty<string>();

    [Fact]
    public async Task WriteAsync_creates_the_folder_and_the_file()
    {
        var svc = new ResultLogService(_logFile);

        Assert.True(await svc.WriteAsync(SampleEntry()));

        Assert.True(File.Exists(_logFile));
    }

    [Fact]
    public async Task WriteAsync_leaves_no_temp_file_behind_on_success()
    {
        var svc = new ResultLogService(_logFile);

        await svc.WriteAsync(SampleEntry());

        // The temp file is renamed into place, not copied and left. One
        // stranded per write would accumulate in the same folder as
        // settings.json for the life of the install.
        Assert.Empty(TempFiles());
    }

    [Fact]
    public async Task WriteAsync_leaves_no_temp_file_behind_when_the_rename_cannot_land()
    {
        // A directory sitting where the log file goes: the temp write succeeds
        // and File.Move then fails. Without the cleanup this pins, that path
        // strands a file under a fresh random name on every attempt.
        Directory.CreateDirectory(_logFile);
        var svc = new ResultLogService(_logFile);

        Assert.False(await svc.WriteAsync(SampleEntry()));

        Assert.Empty(TempFiles());
    }

    [Fact]
    public async Task A_written_log_reads_back_whole()
    {
        var svc = new ResultLogService(_logFile);
        await svc.WriteAsync(SampleEntry());

        var body = await svc.ReadLastLogAsync();

        Assert.NotNull(body);
        // Schema version is the field a receiver branches on before reading
        // anything else, so its presence is what makes the body usable.
        Assert.Contains($"\"schemaVersion\": {ResultLogEntry.CurrentSchemaVersion}", body);
    }

    [Fact]
    public async Task ReadLastLogAsync_returns_null_when_there_is_no_log()
    {
        var svc = new ResultLogService(_logFile);

        Assert.Null(await svc.ReadLastLogAsync());
    }

    [Fact]
    public async Task ReadLastLogAsync_refuses_a_file_over_the_byte_cap()
    {
        Directory.CreateDirectory(_folder);
        // One byte past the cap, so the test pins the boundary rather than
        // some arbitrary large size.
        await File.WriteAllBytesAsync(
            _logFile, Encoding.UTF8.GetBytes(new string('x', (int)IResultLogService.MaxLogBytes + 1)));
        var svc = new ResultLogService(_logFile);

        // Refused rather than truncated: the body is posted to an endpoint, so
        // an oversize file is not something to send part of.
        Assert.Null(await svc.ReadLastLogAsync());
    }

    [Fact]
    public async Task ReadLastLogAsync_accepts_a_file_exactly_at_the_byte_cap()
    {
        Directory.CreateDirectory(_folder);
        await File.WriteAllBytesAsync(
            _logFile, Encoding.UTF8.GetBytes(new string('x', (int)IResultLogService.MaxLogBytes)));
        var svc = new ResultLogService(_logFile);

        var body = await svc.ReadLastLogAsync();

        // The cap is a maximum, not an exclusive bound; a test only on the
        // over-cap side would pass with an off-by-one in either direction.
        Assert.NotNull(body);
        Assert.Equal((int)IResultLogService.MaxLogBytes, body.Length);
    }

    [Fact]
    public async Task SendAsync_refuses_a_body_over_the_byte_cap_without_reaching_the_network()
    {
        var svc = new ResultLogService(_logFile);

        // The second enforcement point: a caller building the body in memory
        // rather than piping it through ReadLastLogAsync would otherwise carry
        // an unbounded payload to the endpoint. Returning before the request is
        // built is what makes this runnable with no network at all.
        var outcome = await svc.SendAsync(new string('x', (int)IResultLogService.MaxLogBytes + 1));

        Assert.Equal(ResultLogSendOutcome.Unknown, outcome);
    }

    [Fact]
    public async Task SendAsync_reports_an_empty_body_as_nothing_to_send()
    {
        var svc = new ResultLogService(_logFile);

        Assert.Equal(ResultLogSendOutcome.NoLogToSend, await svc.SendAsync(string.Empty));
    }

    [Fact]
    public async Task A_second_write_replaces_the_first()
    {
        var svc = new ResultLogService(_logFile);
        await svc.WriteAsync(SampleEntry());
        var first = await svc.ReadLastLogAsync();

        await svc.WriteAsync(ResultLogEntry.ForScanOnly(
            new ScanResult(Array.Empty<OrphanedFile>(), Array.Empty<RegisteredPackage>(), 0),
            scanDurationMs: 9876,
            pendingReboot: "none"));
        var second = await svc.ReadLastLogAsync();

        // MoveFileEx(REPLACE_EXISTING), so there is exactly one log and it is
        // the newest run, never an append or a second file.
        Assert.NotEqual(first, second);
        Assert.Contains("9876", second);
        Assert.Single(Directory.EnumerateFiles(_folder));
    }
}
