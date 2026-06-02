using InstallerClean.Models;
using InstallerClean.Services;
using InstallerClean.Tests.Helpers;

namespace InstallerClean.Tests.Services.Integration;

/// <summary>
/// Real-filesystem, real-COM integration tests: a live
/// <see cref="RecycleEngine"/> drives the Windows IFileOperation API
/// against throwaway files under %TEMP%, so the full recycle pipeline
/// (STA thread, activation, the progress sink) is exercised. The
/// unit suite under InstallerClean.Tests.Services uses MockFileSystem
/// and a fake IRecycleEngine for the outcome-mapping coverage instead.
/// These run on Windows only (the engine's STA apartment is a Windows
/// concept); the Linux pre-commit run filters the Integration namespace
/// out.
///
/// xUnit constructs a fresh instance per test method, so each test gets
/// its own engine, disposed in <see cref="Dispose"/> (which drains the
/// queue and joins the STA thread).
/// </summary>
public class DeleteFilesServiceTests : IDisposable
{
    private readonly string _tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
    private readonly RecycleEngine _engine = new();

    public DeleteFilesServiceTests()
    {
        Directory.CreateDirectory(_tempDir);
    }

    private DeleteFilesService NewService() =>
        new(new System.IO.Abstractions.FileSystem(), _engine);

    [Fact]
    public async Task DeleteFilesAsync_deletes_file()
    {
        var file = Path.Combine(_tempDir, "test.msi");
        await File.WriteAllTextAsync(file, "content");

        var svc = NewService();
        var result = await svc.DeleteFilesAsync(new[] { file });

        Assert.Equal(1, result.DeletedCount);
        Assert.Empty(result.Errors);
        Assert.False(result.RecycleUnavailable);
        Assert.False(File.Exists(file));
    }

    [Fact]
    public async Task DeleteFilesAsync_reports_error_for_missing_source()
    {
        var file = Path.Combine(_tempDir, "nonexistent.msi");

        var svc = NewService();
        var result = await svc.DeleteFilesAsync(new[] { file });

        Assert.Equal(0, result.DeletedCount);
        Assert.Single(result.Errors);
        Assert.Equal(file, result.Errors[0].FilePath);
        // Typed category check: a missing source file produces a
        // MissingSourceFile entry (not a generic UnknownError) so the
        // UI can group/count by cause.
        Assert.IsType<MissingSourceFile>(result.Errors[0]);
    }

    [Fact]
    public async Task DeleteFilesAsync_continues_after_per_file_error_in_mixed_batch()
    {
        var ok1 = Path.Combine(_tempDir, "ok1.msi");
        var missing = Path.Combine(_tempDir, "gone.msi");
        var ok2 = Path.Combine(_tempDir, "ok2.msi");
        await File.WriteAllTextAsync(ok1, "content");
        await File.WriteAllTextAsync(ok2, "content");

        var svc = NewService();
        var result = await svc.DeleteFilesAsync(new[] { ok1, missing, ok2 });

        Assert.Equal(2, result.DeletedCount);
        Assert.Single(result.Errors);
        Assert.Equal(missing, result.Errors[0].FilePath);
        Assert.False(File.Exists(ok1));
        Assert.False(File.Exists(ok2));
    }

    [Fact]
    public async Task DeleteFilesAsync_stops_when_cancelled()
    {
        var files = new List<string>();
        for (int i = 0; i < 3; i++)
        {
            var file = Path.Combine(_tempDir, $"test{i}.msi");
            await File.WriteAllTextAsync(file, "content");
            files.Add(file);
        }

        var cts = new CancellationTokenSource();
        var progress = new SyncProgress<OperationProgress>(p => { if (p.CurrentFile == 1) cts.Cancel(); });

        var svc = NewService();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => svc.DeleteFilesAsync(files, progress: progress, cancellationToken: cts.Token));

        var remaining = Directory.GetFiles(_tempDir).Length;
        Assert.True(remaining > 0, "Cancellation should have stopped before deleting all files");
    }

    public void Dispose()
    {
        _engine.Dispose();
        if (Directory.Exists(_tempDir))
            Directory.Delete(_tempDir, recursive: true);
    }
}
