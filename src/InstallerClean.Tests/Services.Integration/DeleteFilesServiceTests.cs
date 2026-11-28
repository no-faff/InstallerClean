using InstallerClean.Models;
using InstallerClean.Services;
using InstallerClean.Tests.Helpers;

namespace InstallerClean.Tests.Services.Integration;

/// <summary>
/// Real-filesystem integration tests: the service deletes throwaway files
/// in the test run's temp folder through a real <c>FileSystem</c>, so the parts a
/// MockFileSystem cannot reach are exercised. Those are the two safety gates,
/// which read the real filesystem whatever is injected: the reparse-point
/// refusal needs a real symlink, and the containment guard needs a real path
/// to resolve. The unit suite under InstallerClean.Tests.Services covers the
/// outcome mapping against a mock.
/// </summary>
public class DeleteFilesServiceTests : IDisposable
{
    private readonly string _tempDir = TestTemp.NewFolder("ic-delete-");


    // The hand is the app's own, which opens each file on a real handle. The source-containment guard requires sources to resolve inside the cache
    // root; point that root at the test run's sandbox folder so the real files these tests
    // delete are not refused as out-of-bounds, without touching the real cache.
    private DeleteFilesService NewService() =>
        new(new System.IO.Abstractions.FileSystem(), null, null, new HandleInHandFolder(), _tempDir);

    [Fact]
    public async Task DeleteFilesAsync_deletes_file()
    {
        var file = Path.Combine(_tempDir, "test.msi");
        await File.WriteAllTextAsync(file, "content");

        var svc = NewService();
        var result = await svc.DeleteFilesAsync(StandingChecks.Of(new[] { file }));

        Assert.Equal(1, result.DeletedCount);
        Assert.Empty(result.Errors);
        Assert.False(File.Exists(file));
        // The batch's own folder is removed once it is empty, and so is the folder above it.
        Assert.False(Directory.Exists(Path.Combine(_tempDir, InHandFolders.FolderName)));
    }

    [Fact]
    public async Task DeleteFilesAsync_reports_error_for_missing_source()
    {
        var file = Path.Combine(_tempDir, "nonexistent.msi");

        var svc = NewService();
        var result = await svc.DeleteFilesAsync(StandingChecks.Of(new[] { file }));

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
        var result = await svc.DeleteFilesAsync(StandingChecks.Of(new[] { ok1, missing, ok2 }));

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
        var result = await svc.DeleteFilesAsync(StandingChecks.Of(new[] { link }));

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
        var result = await svc.DeleteFilesAsync(StandingChecks.Of(files), progress: progress, cancellationToken: cts.Token);

        Assert.True(result.Cancelled);
        var remaining = Directory.GetFiles(_tempDir).Length;
        Assert.True(remaining > 0, "Cancellation should have stopped before deleting all files");
    }

    [Fact]
    public async Task DeleteFilesAsync_stops_before_the_file_the_hosts_question_turns_down()
    {
        var files = new List<string>();
        for (int i = 0; i < 3; i++)
        {
            var file = Path.Combine(_tempDir, $"test{i}.msi");
            await File.WriteAllTextAsync(file, "content");
            files.Add(file);
        }

        // Asked before each file's progress report and as the last thing before it is
        // deleted, which is how the command line stops a run the single-instance mutex no
        // longer covers. The file turned down gets no progress line.
        var asked = new List<string>();
        var reported = new List<string>();
        var result = await NewService().DeleteFilesAsync(
            StandingChecks.Of(files),
            progress: new SyncProgress<OperationProgress>(p => reported.Add(p.CurrentFileName)),
            mayAct: path =>
            {
                asked.Add(path);
                return path != files[1];
            });

        Assert.True(result.Cancelled);
        Assert.Equal(1, result.DeletedCount);
        Assert.Empty(result.Errors);
        Assert.Equal([files[0], files[0], files[1]], asked);
        Assert.Equal([Path.GetFileName(files[0])], reported);
        Assert.False(File.Exists(files[0]));
        Assert.True(File.Exists(files[1]));
        Assert.True(File.Exists(files[2]));
    }

    [Fact]
    public async Task DeleteFilesAsync_stops_before_the_act_where_the_hosts_answer_turns_after_the_report()
    {
        var file = Path.Combine(_tempDir, "turned.msi");
        await File.WriteAllTextAsync(file, "content");

        // Yes before the report, no as the last thing before the delete.
        var asks = 0;
        var result = await NewService().DeleteFilesAsync(
            StandingChecks.Of(file), mayAct: _ => ++asks == 1);

        Assert.True(result.Cancelled);
        Assert.Equal(0, result.DeletedCount);
        Assert.Empty(result.Errors);
        Assert.Equal(2, asks);
        Assert.True(File.Exists(file));
    }

    [Fact]
    public async Task DeleteFilesAsync_counts_a_cancellation_from_elsewhere_as_that_files_error_and_goes_on()
    {
        var files = new List<string>();
        for (int i = 0; i < 2; i++)
        {
            var file = Path.Combine(_tempDir, $"other{i}.msi");
            await File.WriteAllTextAsync(file, "content");
            files.Add(file);
        }

        // Not the batch's own token: the batch is not stopped by it.
        var result = await NewService().DeleteFilesAsync(
            StandingChecks.Of(files), mayAct: path =>
                path == files[0] ? throw new OperationCanceledException() : true);

        Assert.False(result.Cancelled);
        Assert.Equal(1, result.DeletedCount);
        Assert.Single(result.Errors);
        Assert.True(File.Exists(files[0]));
        Assert.False(File.Exists(files[1]));
    }

    [Fact]
    public async Task DeleteFilesAsync_deletes_a_read_only_file()
    {
        // Windows refuses a delete of a read-only file, so the hand clears the
        // attribute as part of its own delete; without that a read-only file in
        // the cache is reported as a permissions problem the user can do nothing
        // about. Needs a real filesystem: the attribute is a Windows one and
        // MockFileSystem does not enforce it.
        var file = Path.Combine(_tempDir, "readonly.msi");
        await File.WriteAllTextAsync(file, "content");
        File.SetAttributes(file, File.GetAttributes(file) | FileAttributes.ReadOnly);

        var result = await NewService().DeleteFilesAsync(StandingChecks.Of(new[] { file }));

        Assert.Equal(1, result.DeletedCount);
        Assert.Empty(result.Errors);
        Assert.False(File.Exists(file));
    }

    public void Dispose()
    {
        TestTemp.Delete(_tempDir);
    }
}
