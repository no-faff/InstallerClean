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

    // The source-containment guard requires sources to resolve inside the cache
    // root; point that root at the %TEMP% sandbox so the real files these tests
    // recycle are not refused as out-of-bounds, without touching the real cache.
    private DeleteFilesService NewService() =>
        new(new System.IO.Abstractions.FileSystem(), _engine, _tempDir);

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
    public void A_disposed_engine_refuses_work_with_ObjectDisposedException()
    {
        // Both states matter: the engine that never started its worker (the
        // app was closed before any delete) and the one that did. The second
        // is the one that can bite: its queue is still there, just marked
        // complete, and adding to a completed BlockingCollection throws
        // InvalidOperationException. Callers, and DeleteFilesService's
        // unwrapped CanRecycleToVolume in particular, are promised
        // ObjectDisposedException.
        var neverStarted = new RecycleEngine();
        neverStarted.Dispose();
        Assert.Throws<ObjectDisposedException>(
            () => neverStarted.CanRecycleToVolume(_tempDir));

        var started = new RecycleEngine();
        started.CanRecycleToVolume(_tempDir);
        started.Dispose();
        Assert.Throws<ObjectDisposedException>(
            () => started.RecycleFile(Path.Combine(_tempDir, "anything.msi")));
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
    public async Task DeleteFilesAsync_refuses_a_reparse_point_source()
    {
        // A symlink source is refused, matching MoveFilesService: recycling it
        // would send the link, not follow it out of the cache. Real reparse
        // points need a real filesystem, so this is an integration test; it is
        // best-effort because creating a symlink needs SeCreateSymbolicLinkPrivilege
        // (admin or Developer Mode), which not every host grants.
        var target = Path.Combine(_tempDir, "real-target.txt");
        await File.WriteAllTextAsync(target, "content");
        var link = Path.Combine(_tempDir, "link.msi");
        try
        {
            File.CreateSymbolicLink(link, target);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            return; // this host cannot create symlinks; the refusal path is exercised where it can
        }

        var svc = NewService();
        // permitPermanentDelete skips the recycle-bin probe so the outcome does
        // not depend on the bin state of the %TEMP% volume.
        var result = await svc.DeleteFilesAsync(new[] { link }, permitPermanentDelete: true);

        Assert.Equal(0, result.DeletedCount);
        Assert.IsType<SourceIsReparsePoint>(Assert.Single(result.Errors));
        Assert.True(File.Exists(link)); // refused, not recycled
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
