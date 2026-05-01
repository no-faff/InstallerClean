using InstallerClean.Models;
using InstallerClean.Services;
using InstallerClean.Tests.Helpers;

namespace InstallerClean.Tests.Services;

public class DeleteFilesServiceTests : IDisposable
{
    private readonly string _tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());

    public DeleteFilesServiceTests()
    {
        Directory.CreateDirectory(_tempDir);
    }

    [Fact]
    public async Task DeleteFilesAsync_deletes_file()
    {
        var file = Path.Combine(_tempDir, "test.msi");
        await File.WriteAllTextAsync(file, "content");

        var svc = new DeleteFilesService();
        var result = await svc.DeleteFilesAsync(new[] { file });

        Assert.Equal(1, result.DeletedCount);
        Assert.Empty(result.Errors);
        Assert.False(File.Exists(file));
    }

    [Fact]
    public async Task DeleteFilesAsync_reports_error_for_missing_source()
    {
        var file = Path.Combine(_tempDir, "nonexistent.msi");

        var svc = new DeleteFilesService();
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

        var svc = new DeleteFilesService();
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

        var svc = new DeleteFilesService();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => svc.DeleteFilesAsync(files, progress, cts.Token));

        var remaining = Directory.GetFiles(_tempDir).Length;
        Assert.True(remaining > 0, "Cancellation should have stopped before deleting all files");
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempDir))
            Directory.Delete(_tempDir, recursive: true);
    }

}
