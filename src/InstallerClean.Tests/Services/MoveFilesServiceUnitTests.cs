using System.IO.Abstractions.TestingHelpers;
using InstallerClean.Models;
using InstallerClean.Services;

namespace InstallerClean.Tests.Services;

/// <summary>
/// Unit tests for <see cref="MoveFilesService"/> against an in-memory
/// <see cref="MockFileSystem"/>. These complement (do not replace) the
/// real-filesystem integration tests under
/// InstallerClean.Tests.Services.Integration: integration tests prove
/// the service works against actual Windows filesystem behaviour
/// (case-insensitivity, locked files, junction handling, read-only
/// destinations); these unit tests prove the per-file error
/// categorisation, the unique-name fallback logic, and the cancellation
/// path without touching the disk at all.
///
/// Deliberately uncovered: the ProbeDestinationWriteable failure
/// path. MockFileSystem does not enforce a read-only directory
/// attribute, and the real-filesystem test would require dropping
/// the test process's write permission on a temp folder, which CI on
/// shared agents may refuse. The probe is exercised indirectly by
/// the existing destination-write integration tests; if a regression
/// reaches the production read-only path the user sees the localised
/// "cannot write to {dest}" message via DescribeWriteFailure, not a
/// silent swallow.
/// </summary>
public class MoveFilesServiceUnitTests
{
    private const string SourceDir = @"C:\Windows\Installer";
    private const string DestDir = @"D:\backup\installer";

    [Fact]
    public async Task MoveFilesAsync_moves_a_single_file()
    {
        var fs = new MockFileSystem();
        var source = $@"{SourceDir}\a.msi";
        fs.AddFile(source, new MockFileData("payload"));
        fs.AddDirectory(DestDir);

        var svc = new MoveFilesService(fs);
        var result = await svc.MoveFilesAsync(new[] { source }, DestDir);

        Assert.Equal(1, result.MovedCount);
        Assert.Empty(result.Errors);
        Assert.False(fs.File.Exists(source));
        Assert.True(fs.File.Exists($@"{DestDir}\a.msi"));
    }

    [Fact]
    public async Task MoveFilesAsync_appends_unique_suffix_on_collision()
    {
        var fs = new MockFileSystem();
        var source = $@"{SourceDir}\dup.msi";
        var existing = $@"{DestDir}\dup.msi";
        fs.AddFile(source, new MockFileData("source bytes"));
        fs.AddFile(existing, new MockFileData("existing bytes"));

        var svc = new MoveFilesService(fs);
        var result = await svc.MoveFilesAsync(new[] { source }, DestDir);

        Assert.Equal(1, result.MovedCount);
        Assert.Empty(result.Errors);
        Assert.True(fs.File.Exists($@"{DestDir}\dup.msi"));      // original
        Assert.True(fs.File.Exists($@"{DestDir}\dup (1).msi"));  // moved with suffix
    }

    [Fact]
    public async Task MoveFilesAsync_records_MissingSourceFile_for_absent_source()
    {
        var fs = new MockFileSystem();
        fs.AddDirectory(DestDir);
        var ghost = $@"{SourceDir}\ghost.msi";

        var svc = new MoveFilesService(fs);
        var result = await svc.MoveFilesAsync(new[] { ghost }, DestDir);

        Assert.Equal(0, result.MovedCount);
        var error = Assert.Single(result.Errors);
        Assert.IsType<MissingSourceFile>(error);
        Assert.Equal(ghost, error.FilePath);
    }

    [Fact]
    public async Task MoveFilesAsync_continues_after_per_file_error_in_mixed_batch()
    {
        var fs = new MockFileSystem();
        var ok1 = $@"{SourceDir}\ok1.msi";
        var missing = $@"{SourceDir}\gone.msi";
        var ok2 = $@"{SourceDir}\ok2.msi";
        fs.AddFile(ok1, new MockFileData("a"));
        fs.AddFile(ok2, new MockFileData("b"));
        fs.AddDirectory(DestDir);

        var svc = new MoveFilesService(fs);
        var result = await svc.MoveFilesAsync(new[] { ok1, missing, ok2 }, DestDir);

        Assert.Equal(2, result.MovedCount);
        var error = Assert.Single(result.Errors);
        Assert.IsType<MissingSourceFile>(error);
        Assert.Equal(missing, error.FilePath);
        Assert.True(fs.File.Exists($@"{DestDir}\ok1.msi"));
        Assert.True(fs.File.Exists($@"{DestDir}\ok2.msi"));
    }

    [Fact]
    public async Task MoveFilesAsync_creates_destination_directory_if_missing()
    {
        var fs = new MockFileSystem();
        var source = $@"{SourceDir}\a.msi";
        fs.AddFile(source, new MockFileData("payload"));
        // Note: DestDir not pre-created.

        var svc = new MoveFilesService(fs);
        var result = await svc.MoveFilesAsync(new[] { source }, DestDir);

        Assert.Equal(1, result.MovedCount);
        Assert.True(fs.Directory.Exists(DestDir));
    }

    [Fact]
    public async Task MoveFilesAsync_reports_progress_per_file()
    {
        var fs = new MockFileSystem();
        var sources = new[] { "a.msi", "b.msi", "c.msi" }
            .Select(n => $@"{SourceDir}\{n}").ToArray();
        foreach (var s in sources) fs.AddFile(s, new MockFileData("payload"));
        fs.AddDirectory(DestDir);

        var reports = new List<OperationProgress>();
        var progress = new Helpers.SyncProgress<OperationProgress>(reports.Add);

        var svc = new MoveFilesService(fs);
        await svc.MoveFilesAsync(sources, DestDir, progress);

        Assert.Equal(3, reports.Count);
        Assert.Equal(1, reports[0].CurrentFile);
        Assert.Equal(3, reports[2].CurrentFile);
        Assert.All(reports, r => Assert.Equal(3, r.TotalFiles));
    }

    [Fact]
    public async Task MoveFilesAsync_throws_when_cancelled_mid_batch()
    {
        var fs = new MockFileSystem();
        var sources = new[] { "a.msi", "b.msi", "c.msi" }
            .Select(n => $@"{SourceDir}\{n}").ToArray();
        foreach (var s in sources) fs.AddFile(s, new MockFileData("payload"));
        fs.AddDirectory(DestDir);

        // Cancel after the first progress report so the second
        // iteration's ThrowIfCancellationRequested fires.
        var cts = new CancellationTokenSource();
        var progress = new Helpers.SyncProgress<InstallerClean.Models.OperationProgress>(p =>
        {
            if (p.CurrentFile == 1) cts.Cancel();
        });

        var svc = new MoveFilesService(fs);

        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => svc.MoveFilesAsync(sources, DestDir, progress, cts.Token));

        // First file should already have moved before cancellation
        // landed; the rest stay in the source folder.
        Assert.True(fs.File.Exists($@"{DestDir}\a.msi"));
        Assert.True(fs.File.Exists($@"{SourceDir}\b.msi"));
        Assert.True(fs.File.Exists($@"{SourceDir}\c.msi"));
    }

    [Fact]
    public async Task MoveFilesAsync_zero_files_returns_empty_result()
    {
        var fs = new MockFileSystem();
        fs.AddDirectory(DestDir);

        var svc = new MoveFilesService(fs);
        var result = await svc.MoveFilesAsync(Array.Empty<string>(), DestDir);

        Assert.Equal(0, result.MovedCount);
        Assert.Empty(result.Errors);
    }
}
