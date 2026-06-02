using System.IO.Abstractions.TestingHelpers;
using InstallerClean.Models;
using InstallerClean.Services;
using InstallerClean.Tests.Helpers;
using NSubstitute;

namespace InstallerClean.Tests.Services;

/// <summary>
/// Unit tests for <see cref="DeleteFilesService"/> against an in-memory
/// <see cref="MockFileSystem"/> and a fake <see cref="IRecycleEngine"/>.
/// These prove the per-file outcome mapping, the recycle-or-permanent
/// decision, the probe-and-refuse behaviour, progress and cancellation,
/// without touching the real Recycle Bin (the real COM engine is
/// covered by the Windows integration tests).
/// </summary>
public class DeleteFilesServiceUnitTests
{
    private const string Dir = @"C:\Windows\Installer";

    private static (MockFileSystem fs, IRecycleEngine engine) Setup()
    {
        var fs = new MockFileSystem();
        var engine = Substitute.For<IRecycleEngine>();
        // Default happy path: the bin is available and every file recycles.
        engine.CanRecycleToVolume(Arg.Any<string>()).Returns(true);
        engine.RecycleFile(Arg.Any<string>()).Returns(new RecycleFileOutcome(RecycleOutcome.Recycled, 0));
        return (fs, engine);
    }

    private static string AddFile(MockFileSystem fs, string name)
    {
        var path = $@"{Dir}\{name}";
        fs.AddFile(path, new MockFileData("payload"));
        return path;
    }

    [Fact]
    public async Task Recycles_all_files_in_a_clean_batch()
    {
        var (fs, engine) = Setup();
        var a = AddFile(fs, "a.msi");
        var b = AddFile(fs, "b.msi");
        var svc = new DeleteFilesService(fs, engine);

        var result = await svc.DeleteFilesAsync(new[] { a, b });

        Assert.Equal(2, result.DeletedCount);
        Assert.Empty(result.Errors);
        Assert.False(result.RecycleUnavailable);
    }

    [Fact]
    public async Task Refuses_batch_when_bin_unavailable_and_not_permitted()
    {
        var (fs, engine) = Setup();
        engine.CanRecycleToVolume(Arg.Any<string>()).Returns(false);
        var a = AddFile(fs, "a.msi");
        var svc = new DeleteFilesService(fs, engine);

        var result = await svc.DeleteFilesAsync(new[] { a }, permitPermanentDelete: false);

        Assert.True(result.RecycleUnavailable);
        Assert.Equal(0, result.DeletedCount);
        Assert.Empty(result.Errors);
        // Refuse means touch nothing.
        engine.DidNotReceive().RecycleFile(Arg.Any<string>());
    }

    [Fact]
    public async Task Permit_skips_the_probe_and_counts_permanent_delete_as_deleted()
    {
        var (fs, engine) = Setup();
        engine.RecycleFile(Arg.Any<string>())
            .Returns(new RecycleFileOutcome(RecycleOutcome.PermanentlyDeleted, 0));
        var a = AddFile(fs, "a.msi");
        var svc = new DeleteFilesService(fs, engine);

        var result = await svc.DeleteFilesAsync(new[] { a }, permitPermanentDelete: true);

        Assert.Equal(1, result.DeletedCount);
        Assert.Empty(result.Errors);
        Assert.False(result.RecycleUnavailable);
        // Consent given up front, so there is nothing to probe for.
        engine.DidNotReceive().CanRecycleToVolume(Arg.Any<string>());
    }

    [Fact]
    public async Task Permanent_delete_without_permit_is_recorded_as_an_error()
    {
        var (fs, engine) = Setup(); // probe returns true; this file still nukes
        var a = AddFile(fs, "a.msi");
        engine.RecycleFile(a).Returns(new RecycleFileOutcome(RecycleOutcome.PermanentlyDeleted, 0));
        var svc = new DeleteFilesService(fs, engine);

        var result = await svc.DeleteFilesAsync(new[] { a }, permitPermanentDelete: false);

        Assert.Equal(0, result.DeletedCount);
        var err = Assert.Single(result.Errors);
        Assert.IsType<PermanentlyDeleted>(err);
        Assert.Equal(a, err.FilePath);
    }

    [Fact]
    public async Task Failed_file_is_recorded_as_RecycleFailed_carrying_the_hresult()
    {
        var (fs, engine) = Setup();
        var a = AddFile(fs, "a.msi");
        var hr = unchecked((int)0x80004005); // E_FAIL
        engine.RecycleFile(a).Returns(new RecycleFileOutcome(RecycleOutcome.Failed, hr));
        var svc = new DeleteFilesService(fs, engine);

        var result = await svc.DeleteFilesAsync(new[] { a });

        Assert.Equal(0, result.DeletedCount);
        var err = Assert.Single(result.Errors);
        var failed = Assert.IsType<RecycleFailed>(err);
        Assert.Equal(hr, failed.HResult);
        Assert.Equal(a, failed.FilePath);
    }

    [Fact]
    public async Task Missing_source_is_recorded_and_engine_not_called_for_it()
    {
        var (fs, engine) = Setup();
        var ghost = $@"{Dir}\ghost.msi"; // never added to the mock filesystem
        var svc = new DeleteFilesService(fs, engine);

        var result = await svc.DeleteFilesAsync(new[] { ghost });

        Assert.Equal(0, result.DeletedCount);
        var err = Assert.Single(result.Errors);
        Assert.IsType<MissingSourceFile>(err);
        Assert.Equal(ghost, err.FilePath);
        engine.DidNotReceive().RecycleFile(ghost);
    }

    [Fact]
    public async Task Continues_after_a_per_file_error_in_a_mixed_batch()
    {
        var (fs, engine) = Setup();
        var ok1 = AddFile(fs, "ok1.msi");
        var missing = $@"{Dir}\gone.msi";
        var ok2 = AddFile(fs, "ok2.msi");
        var svc = new DeleteFilesService(fs, engine);

        var result = await svc.DeleteFilesAsync(new[] { ok1, missing, ok2 });

        Assert.Equal(2, result.DeletedCount);
        var err = Assert.Single(result.Errors);
        Assert.IsType<MissingSourceFile>(err);
        Assert.Equal(missing, err.FilePath);
    }

    [Fact]
    public async Task Reports_progress_per_file()
    {
        var (fs, engine) = Setup();
        var files = new[] { "a.msi", "b.msi", "c.msi" }.Select(n => AddFile(fs, n)).ToArray();
        var reports = new List<OperationProgress>();
        var progress = new SyncProgress<OperationProgress>(reports.Add);
        var svc = new DeleteFilesService(fs, engine);

        await svc.DeleteFilesAsync(files, progress: progress);

        Assert.Equal(3, reports.Count);
        Assert.Equal(1, reports[0].CurrentFile);
        Assert.Equal(3, reports[2].CurrentFile);
        Assert.All(reports, r => Assert.Equal(3, r.TotalFiles));
    }

    [Fact]
    public async Task Throws_when_cancelled_mid_batch()
    {
        var (fs, engine) = Setup();
        var files = new[] { "a.msi", "b.msi", "c.msi" }.Select(n => AddFile(fs, n)).ToArray();
        var cts = new CancellationTokenSource();
        var progress = new SyncProgress<OperationProgress>(p => { if (p.CurrentFile == 1) cts.Cancel(); });
        var svc = new DeleteFilesService(fs, engine);

        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => svc.DeleteFilesAsync(files, progress: progress, cancellationToken: cts.Token));
    }

    [Fact]
    public async Task Zero_files_returns_empty_result_without_probing()
    {
        var (fs, engine) = Setup();
        var svc = new DeleteFilesService(fs, engine);

        var result = await svc.DeleteFilesAsync(Array.Empty<string>());

        Assert.Equal(0, result.DeletedCount);
        Assert.Empty(result.Errors);
        Assert.False(result.RecycleUnavailable);
        engine.DidNotReceive().CanRecycleToVolume(Arg.Any<string>());
    }
}
