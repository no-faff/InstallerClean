using InstallerClean.Models;
using InstallerClean.Services;
using InstallerClean.Tests.Helpers;

namespace InstallerClean.Tests.Services;

public class MoveFilesServiceTests : IDisposable
{
    private readonly string _sourceDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
    private readonly string _destDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());

    public MoveFilesServiceTests()
    {
        Directory.CreateDirectory(_sourceDir);
        Directory.CreateDirectory(_destDir);
    }

    [Fact]
    public async Task MoveFilesAsync_moves_file_to_destination()
    {
        var file = Path.Combine(_sourceDir, "test.msi");
        await File.WriteAllTextAsync(file, "content");

        var svc = new MoveFilesService();
        var results = await svc.MoveFilesAsync(new[] { file }, _destDir);

        Assert.Empty(results.Errors);
        Assert.False(File.Exists(file));
        Assert.True(File.Exists(Path.Combine(_destDir, "test.msi")));
    }

    [Fact]
    public async Task MoveFilesAsync_handles_name_collision_by_appending_number()
    {
        var file1 = Path.Combine(_sourceDir, "test.msi");
        var existing = Path.Combine(_destDir, "test.msi");
        await File.WriteAllTextAsync(file1, "source");
        await File.WriteAllTextAsync(existing, "existing");

        var svc = new MoveFilesService();
        var results = await svc.MoveFilesAsync(new[] { file1 }, _destDir);

        Assert.Empty(results.Errors);
        Assert.True(File.Exists(Path.Combine(_destDir, "test.msi")));         // original
        Assert.True(File.Exists(Path.Combine(_destDir, "test (1).msi")));     // moved with suffix
    }

    [Fact]
    public async Task MoveFilesAsync_reports_error_for_missing_source()
    {
        var file = Path.Combine(_sourceDir, "nonexistent.msi");

        var svc = new MoveFilesService();
        var results = await svc.MoveFilesAsync(new[] { file }, _destDir);

        Assert.Single(results.Errors);
        Assert.Equal(file, results.Errors[0].FilePath);
        Assert.IsType<MissingSourceFile>(results.Errors[0]);
    }

    [Fact]
    public async Task MoveFilesAsync_continues_after_per_file_error_in_mixed_batch()
    {
        var ok1 = Path.Combine(_sourceDir, "ok1.msi");
        var missing = Path.Combine(_sourceDir, "gone.msi");
        var ok2 = Path.Combine(_sourceDir, "ok2.msi");
        await File.WriteAllTextAsync(ok1, "content");
        await File.WriteAllTextAsync(ok2, "content");

        var svc = new MoveFilesService();
        var result = await svc.MoveFilesAsync(new[] { ok1, missing, ok2 }, _destDir);

        Assert.Equal(2, result.MovedCount);
        Assert.Single(result.Errors);
        Assert.Equal(missing, result.Errors[0].FilePath);
        Assert.True(File.Exists(Path.Combine(_destDir, "ok1.msi")));
        Assert.True(File.Exists(Path.Combine(_destDir, "ok2.msi")));
        Assert.False(File.Exists(ok1));
        Assert.False(File.Exists(ok2));
    }

    [Fact]
    public async Task MoveFilesAsync_stops_when_cancelled()
    {
        var files = new List<string>();
        for (int i = 0; i < 3; i++)
        {
            var file = Path.Combine(_sourceDir, $"test{i}.msi");
            await File.WriteAllTextAsync(file, "content");
            files.Add(file);
        }

        var cts = new CancellationTokenSource();
        var progress = new SyncProgress<OperationProgress>(p => { if (p.CurrentFile == 1) cts.Cancel(); });

        var svc = new MoveFilesService();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => svc.MoveFilesAsync(files, _destDir, progress, cts.Token));

        var remaining = Directory.GetFiles(_sourceDir).Length;
        Assert.True(remaining > 0, "Cancellation should have stopped before moving all files");
    }

    public void Dispose()
    {
        if (Directory.Exists(_sourceDir)) Directory.Delete(_sourceDir, recursive: true);
        if (Directory.Exists(_destDir)) Directory.Delete(_destDir, recursive: true);
    }

}
