using System.IO.Abstractions;
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
    public async Task Permanent_delete_without_permit_is_recorded_as_an_error_carrying_the_hresult()
    {
        var (fs, engine) = Setup(); // probe returns true; this file still nukes
        var a = AddFile(fs, "a.msi");
        // A success hrDelete (COPYENGINE_S_*) that nonetheless skipped the
        // bin: the engine surfaces the nuke, and the code rides along for
        // telemetry even though the operation "succeeded".
        var hr = unchecked((int)0x00270008);
        engine.RecycleFile(a).Returns(new RecycleFileOutcome(RecycleOutcome.PermanentlyDeleted, hr));
        var svc = new DeleteFilesService(fs, engine);

        var result = await svc.DeleteFilesAsync(new[] { a }, permitPermanentDelete: false);

        Assert.Equal(0, result.DeletedCount);
        var err = Assert.Single(result.Errors);
        var nuked = Assert.IsType<PermanentlyDeleted>(err);
        Assert.Equal(hr, nuked.HResult);
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
    public async Task Returns_the_partial_result_when_cancelled_mid_batch()
    {
        var (fs, engine) = Setup();
        var files = new[] { "a.msi", "b.msi", "c.msi" }.Select(n => AddFile(fs, n)).ToArray();
        var cts = new CancellationTokenSource();
        var progress = new SyncProgress<OperationProgress>(p => { if (p.CurrentFile == 1) cts.Cancel(); });
        var svc = new DeleteFilesService(fs, engine);

        // No throw on a mid-batch cancel: the partial result comes back with
        // Cancelled set and the tally of what was recycled before the stop.
        var result = await svc.DeleteFilesAsync(files, progress: progress, cancellationToken: cts.Token);

        Assert.True(result.Cancelled);
        Assert.Equal(1, result.DeletedCount);
        Assert.False(result.RecycleUnavailable);
        Assert.Empty(result.Errors);
        engine.Received(1).RecycleFile(Arg.Any<string>());
    }

    [Fact]
    public async Task Refuses_a_source_outside_the_installer_cache()
    {
        // The README's central promise as a test that cannot rot: a path that
        // resolves OUTSIDE C:\Windows\Installer is refused at the service
        // boundary even though the file exists and the engine would recycle it.
        // No installer-folder override here, so the real C:\Windows\Installer is
        // the boundary; C:\Temp is not inside it.
        var (fs, engine) = Setup();
        const string outside = @"C:\Temp\evil.msi";
        fs.AddFile(outside, new MockFileData("payload"));
        var svc = new DeleteFilesService(fs, engine);

        var result = await svc.DeleteFilesAsync(new[] { outside }, permitPermanentDelete: true);

        Assert.Equal(0, result.DeletedCount);
        Assert.IsType<CandidateOutsideCache>(Assert.Single(result.Errors));
        engine.DidNotReceive().RecycleFile(outside);
    }

    /// <summary>
    /// A filesystem reporting exactly one path as existing. See the twin in
    /// <see cref="MoveFilesServiceUnitTests"/>: the reparse and containment
    /// gates consult the REAL filesystem whatever is injected, so reaching them
    /// means handing the service a source string the real filesystem cannot
    /// hold, which MockFileSystem will not store.
    /// </summary>
    private static IFileSystem FileSystemReporting(string source)
    {
        var fs = Substitute.For<IFileSystem>();
        fs.Path.Returns(new MockFileSystem().Path);
        fs.Directory.Returns(Substitute.For<IDirectory>());
        var file = Substitute.For<IFile>();
        fs.File.Returns(file);
        file.Exists(Arg.Any<string>()).Returns(ci => (string?)ci[0] == source);
        return fs;
    }

    [Fact]
    public async Task Refuses_a_source_whose_attributes_cannot_be_read()
    {
        // An embedded null makes File.GetAttributes throw ArgumentException on
        // every platform and every version: the "the read failed" arm, which
        // used to answer "not a reparse point" and let the file through to the
        // recycle call.
        var source = $"{Dir}\\unreadable\0.msi";
        var (_, engine) = Setup();

        var svc = new DeleteFilesService(FileSystemReporting(source), engine);
        var result = await svc.DeleteFilesAsync(new[] { source }, permitPermanentDelete: true);

        // UnknownError, NOT SourceIsReparsePoint: a read that could not be made
        // has not shown the file is a symlink.
        Assert.IsType<UnknownError>(Assert.Single(result.Errors));
        Assert.Equal(0, result.DeletedCount);
        engine.DidNotReceive().RecycleFile(Arg.Any<string>());
    }

    [Fact]
    public async Task Refuses_a_source_it_cannot_resolve()
    {
        // A path on a drive letter nothing is mounted on: no existing ancestor
        // to open, so where the path really leads was never established and the
        // file is refused rather than recycled on the strength of its spelling.
        var unmounted = TestHost.FirstUnmountedDriveLetter();
        if (unmounted is null)
            return; // every letter is in use on this host; nothing to pose the question with

        var source = $@"{unmounted}:\Windows\Installer\unresolvable.msi";
        var (_, engine) = Setup();

        var svc = new DeleteFilesService(FileSystemReporting(source), engine);
        var result = await svc.DeleteFilesAsync(new[] { source }, permitPermanentDelete: true);

        Assert.IsType<UnknownError>(Assert.Single(result.Errors));
        Assert.Equal(0, result.DeletedCount);
        engine.DidNotReceive().RecycleFile(Arg.Any<string>());
    }

    [Fact]
    public async Task Refuses_when_the_installer_mutex_is_held()
    {
        var (fs, engine) = Setup();
        var a = AddFile(fs, "a.msi");
        var mutex = new FakeMutexProbe(FakeMutexProbe.Mode.HeldByAnother);
        var svc = new DeleteFilesService(fs, engine, mutex);

        var result = await svc.DeleteFilesAsync(new[] { a });

        Assert.True(result.InstallerBusy);
        Assert.Equal(0, result.DeletedCount);
        Assert.Empty(result.Errors);
        engine.DidNotReceive().RecycleFile(Arg.Any<string>());
    }

    [Fact]
    public async Task Probes_the_bin_before_taking_the_installer_mutex()
    {
        // The probe mutates nothing, so it runs outside the hold: everything
        // between the acquire and the release blocks every installer on the
        // machine, and a batch that ends up refusing the whole list should not
        // have decided that inside the lock. The visible consequence is the
        // order of the two refusals, pinned here so it is a decision rather
        // than a side effect: a machine where the bin is unavailable AND an
        // installer holds the lock now reports the bin, not the installer.
        // Both refuse and neither touches a file, and the second refusal
        // arrives the moment the first is answered.
        var (fs, engine) = Setup();
        engine.CanRecycleToVolume(Arg.Any<string>()).Returns(false);
        var a = AddFile(fs, "a.msi");
        var mutex = new FakeMutexProbe(FakeMutexProbe.Mode.HeldByAnother);
        var svc = new DeleteFilesService(fs, engine, mutex);

        var result = await svc.DeleteFilesAsync(new[] { a });

        Assert.True(result.RecycleUnavailable);
        Assert.False(result.InstallerBusy);
        Assert.Equal(0, mutex.AcquireAttempts); // the refusal happened before the hold existed
        engine.DidNotReceive().RecycleFile(Arg.Any<string>());
    }

    [Fact]
    public async Task Holds_and_releases_the_installer_mutex_when_acquired()
    {
        var (fs, engine) = Setup();
        var a = AddFile(fs, "a.msi");
        var mutex = new FakeMutexProbe(FakeMutexProbe.Mode.Acquire);
        var svc = new DeleteFilesService(fs, engine, mutex);

        var result = await svc.DeleteFilesAsync(new[] { a });

        Assert.Equal(1, result.DeletedCount);
        Assert.False(result.InstallerBusy);
        Assert.Equal(1, mutex.Acquired);
        Assert.Equal(1, mutex.Released);
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
