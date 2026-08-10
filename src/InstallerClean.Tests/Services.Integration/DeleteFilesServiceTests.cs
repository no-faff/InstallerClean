using System.IO.Abstractions.TestingHelpers;
using InstallerClean.Helpers;
using InstallerClean.Models;
using InstallerClean.Services;
using InstallerClean.Tests.Helpers;

namespace InstallerClean.Tests.Services.Integration;

/// <summary>
/// Real-filesystem integration tests: the service deletes throwaway files
/// under %TEMP% through a real <c>FileSystem</c>, so the parts a
/// MockFileSystem cannot reach are exercised. Those are the two safety gates,
/// which read the real filesystem whatever is injected: the reparse-point
/// refusal needs a real symlink, and the containment guard needs a real path
/// to resolve. The unit suite under InstallerClean.Tests.Services covers the
/// outcome mapping against a mock.
///
/// These run on Windows only; the Linux pre-commit run filters the Integration
/// namespace out.
/// </summary>
public class DeleteFilesServiceTests : IDisposable
{
    private readonly string _tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());

    public DeleteFilesServiceTests()
    {
        Directory.CreateDirectory(_tempDir);
    }

    // The source-containment guard requires sources to resolve inside the cache
    // root; point that root at the %TEMP% sandbox so the real files these tests
    // delete are not refused as out-of-bounds, without touching the real cache.
    private DeleteFilesService NewService() =>
        new(new System.IO.Abstractions.FileSystem(), NullMutexProbe.Instance, _tempDir);

    [Fact]
    public async Task DeleteFilesAsync_deletes_file()
    {
        var file = Path.Combine(_tempDir, "test.msi");
        await File.WriteAllTextAsync(file, "content");

        var svc = NewService();
        var result = await svc.DeleteFilesAsync(new[] { file });

        Assert.Equal(1, result.DeletedCount);
        Assert.Empty(result.Errors);
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
    public async Task DeleteFilesAsync_refuses_a_reparse_point_source()
    {
        // A symlink source is refused, matching MoveFilesService: deleting it
        // would remove the link, not follow it out of the cache. Real reparse
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
        var result = await svc.DeleteFilesAsync(new[] { link });

        Assert.Equal(0, result.DeletedCount);
        Assert.IsType<SourceIsReparsePoint>(Assert.Single(result.Errors));
        Assert.True(File.Exists(link)); // refused, not deleted
    }

    [Fact]
    public async Task DeleteFilesAsync_returns_the_partial_result_when_cancelled()
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
        // A mid-batch cancel returns the partial result with Cancelled set rather
        // than throwing; some files remain, having been stopped before deletion.
        var result = await svc.DeleteFilesAsync(files, progress: progress, cancellationToken: cts.Token);

        Assert.True(result.Cancelled);
        var remaining = Directory.GetFiles(_tempDir).Length;
        Assert.True(remaining > 0, "Cancellation should have stopped before deleting all files");
    }

    [Fact]
    public async Task DeleteFilesAsync_deletes_a_read_only_file()
    {
        // Windows refuses File.Delete on a read-only file, and the shell delete
        // this replaced cleared the attribute and carried on, so without the
        // retry a read-only file in the cache is reported as a permissions
        // problem the user can do nothing about. Needs a real filesystem: the
        // attribute is a Windows one and MockFileSystem does not enforce it.
        var file = Path.Combine(_tempDir, "readonly.msi");
        await File.WriteAllTextAsync(file, "content");
        File.SetAttributes(file, File.GetAttributes(file) | FileAttributes.ReadOnly);

        var result = await NewService().DeleteFilesAsync(new[] { file });

        Assert.Equal(1, result.DeletedCount);
        Assert.Empty(result.Errors);
        Assert.False(File.Exists(file));
    }

    [Fact]
    public async Task DeleteFilesAsync_refuses_and_records_a_batch_it_could_not_hold_the_mutex_for()
    {
        // A hold that could not be taken, with nobody else holding it, stops the
        // batch. Without the hold nothing prevents a program registering a package
        // part-way through, so the act-time proof can go stale underneath a delete
        // that no longer has a Recycle Bin to take the mistake back out of.
        //
        // Its Move twin asserts the same outcome on the same input, and the pair
        // is where that is pinned: a file moved out of the cache is as absent
        // from it as a deleted one, so the exposure is shared and only the
        // recovery differs.
        //
        // Real filesystem because the record is a real crash-log write, which is
        // half the behaviour under test: the user is told the run refused, and the
        // log is the only place the machine's own condition is written down.
        var logPath = CrashLog.Write(
            new InvalidOperationException("baseline for the mutex fall-back record"));
        var baseline = new FileInfo(logPath).Length;

        var fs = new MockFileSystem();
        var source = @"C:\Windows\Installer\never-reached.msi";
        fs.AddFile(source, new MockFileData("payload"));
        var mutex = new FakeMutexProbe(FakeMutexProbe.Mode.RefusedNotHeld);

        var result = await new DeleteFilesService(fs, mutex, installerFolderOverride: null).DeleteFilesAsync(new[] { source });

        // Refused, and distinguishably so: nothing was shown to hold the mutex, so
        // the pending-reboot gate the busy case is answered by can account for this
        // one neither way.
        Assert.True(result.InstallerLockUnavailable);
        Assert.False(result.InstallerBusy);
        Assert.Equal(0, result.DeletedCount);
        Assert.Empty(result.Errors);
        Assert.True(fs.File.Exists(source), "A refused batch touches nothing");
        Assert.Equal(1, mutex.AcquireAttempts);
        Assert.Equal(0, mutex.Acquired);

        // Read from the baseline on, so a note left by an earlier run cannot
        // pass for this one's. A rotation between the two reads shortens the
        // file, and starting at 0 is then still right.
        using var stream = new FileStream(logPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
        using var reader = new StreamReader(stream);
        var written = await reader.ReadToEndAsync();
        var appended = written.Length >= baseline ? written[(int)baseline..] : written;
        Assert.Contains("Delete refused", appended);
        Assert.Contains("could not be acquired and was not held by another process", appended);
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempDir))
            Directory.Delete(_tempDir, recursive: true);
    }
}
