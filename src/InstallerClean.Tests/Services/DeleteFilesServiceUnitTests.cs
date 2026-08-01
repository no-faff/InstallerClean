using System.IO.Abstractions;
using System.IO.Abstractions.TestingHelpers;
using InstallerClean.Models;
using InstallerClean.Services;
using InstallerClean.Tests.Helpers;
using NSubstitute;

namespace InstallerClean.Tests.Services;

/// <summary>
/// Unit tests for <see cref="DeleteFilesService"/> against an in-memory
/// <see cref="MockFileSystem"/>. These prove the per-file outcome mapping,
/// progress, cancellation and the installer-mutex hold. The two safety gates
/// they exercise (reparse point, containment) deliberately read the REAL
/// filesystem whatever is injected, which is what the refusal tests below turn
/// on: a mock cannot talk its way past either.
/// </summary>
public class DeleteFilesServiceUnitTests
{
    private const string Dir = @"C:\Windows\Installer";

    private static string AddFile(MockFileSystem fs, string name)
    {
        var path = $@"{Dir}\{name}";
        fs.AddFile(path, new MockFileData("payload"));
        return path;
    }

    [Fact]
    public async Task Deletes_all_files_in_a_clean_batch()
    {
        var fs = new MockFileSystem();
        var a = AddFile(fs, "a.msi");
        var b = AddFile(fs, "b.msi");
        var svc = new DeleteFilesService(fs);

        var result = await svc.DeleteFilesAsync(new[] { a, b });

        Assert.Equal(2, result.DeletedCount);
        Assert.Empty(result.Errors);
        Assert.False(fs.File.Exists(a));
        Assert.False(fs.File.Exists(b));
    }

    [Fact]
    public async Task Missing_source_is_recorded_and_nothing_else_is_touched()
    {
        var fs = new MockFileSystem();
        var ghost = $@"{Dir}\ghost.msi"; // never added to the mock filesystem
        var svc = new DeleteFilesService(fs);

        var result = await svc.DeleteFilesAsync(new[] { ghost });

        Assert.Equal(0, result.DeletedCount);
        var err = Assert.Single(result.Errors);
        Assert.IsType<MissingSourceFile>(err);
        Assert.Equal(ghost, err.FilePath);
    }

    [Fact]
    public async Task Continues_after_a_per_file_error_in_a_mixed_batch()
    {
        var fs = new MockFileSystem();
        var ok1 = AddFile(fs, "ok1.msi");
        var missing = $@"{Dir}\gone.msi";
        var ok2 = AddFile(fs, "ok2.msi");
        var svc = new DeleteFilesService(fs);

        var result = await svc.DeleteFilesAsync(new[] { ok1, missing, ok2 });

        Assert.Equal(2, result.DeletedCount);
        var err = Assert.Single(result.Errors);
        Assert.IsType<MissingSourceFile>(err);
        Assert.Equal(missing, err.FilePath);
    }

    [Fact]
    public async Task Reports_progress_per_file()
    {
        var fs = new MockFileSystem();
        var files = new[] { "a.msi", "b.msi", "c.msi" }.Select(n => AddFile(fs, n)).ToArray();
        var reports = new List<OperationProgress>();
        var progress = new SyncProgress<OperationProgress>(reports.Add);
        var svc = new DeleteFilesService(fs);

        await svc.DeleteFilesAsync(files, progress: progress);

        Assert.Equal(3, reports.Count);
        Assert.Equal(1, reports[0].CurrentFile);
        Assert.Equal(3, reports[2].CurrentFile);
        Assert.All(reports, r => Assert.Equal(3, r.TotalFiles));
    }

    [Fact]
    public async Task Returns_the_partial_result_when_cancelled_mid_batch()
    {
        var fs = new MockFileSystem();
        var files = new[] { "a.msi", "b.msi", "c.msi" }.Select(n => AddFile(fs, n)).ToArray();
        var cts = new CancellationTokenSource();
        var progress = new SyncProgress<OperationProgress>(p => { if (p.CurrentFile == 1) cts.Cancel(); });
        var svc = new DeleteFilesService(fs);

        // No throw on a mid-batch cancel: the partial result comes back with
        // Cancelled set and the tally of what was deleted before the stop.
        var result = await svc.DeleteFilesAsync(files, progress: progress, cancellationToken: cts.Token);

        Assert.True(result.Cancelled);
        Assert.Equal(1, result.DeletedCount);
        Assert.Empty(result.Errors);
        Assert.False(fs.File.Exists(files[0]));
        Assert.True(fs.File.Exists(files[2]));
    }

    [Fact]
    public async Task Refuses_a_source_outside_the_installer_cache()
    {
        // The README's central promise as a test that cannot rot: a path that
        // resolves OUTSIDE C:\Windows\Installer is refused at the service
        // boundary even though the file exists. No installer-folder override
        // here, so the real C:\Windows\Installer is the boundary; C:\Temp is
        // not inside it.
        var fs = new MockFileSystem();
        const string outside = @"C:\Temp\evil.msi";
        fs.AddFile(outside, new MockFileData("payload"));
        var svc = new DeleteFilesService(fs);

        var result = await svc.DeleteFilesAsync(new[] { outside });

        Assert.Equal(0, result.DeletedCount);
        Assert.IsType<CandidateOutsideCache>(Assert.Single(result.Errors));
        Assert.True(fs.File.Exists(outside));
    }

    /// <summary>
    /// A filesystem reporting exactly one path as existing, and optionally
    /// throwing <paramref name="deleteThrows"/> when asked to delete it. See
    /// the twin in <see cref="MoveFilesServiceUnitTests"/>: the reparse and
    /// containment gates consult the REAL filesystem whatever is injected, so
    /// reaching them means handing the service a source string the real
    /// filesystem cannot hold, which MockFileSystem will not store.
    /// </summary>
    private static IFileSystem FileSystemReporting(string source, Exception? deleteThrows = null)
    {
        var fs = Substitute.For<IFileSystem>();
        fs.Path.Returns(new MockFileSystem().Path);
        fs.Directory.Returns(Substitute.For<IDirectory>());
        var file = Substitute.For<IFile>();
        fs.File.Returns(file);
        file.Exists(Arg.Any<string>()).Returns(ci => (string?)ci[0] == source);
        if (deleteThrows is not null)
            file.When(f => f.Delete(source)).Do(_ => throw deleteThrows);
        return fs;
    }

    [Fact]
    public async Task Refuses_a_source_whose_attributes_cannot_be_read()
    {
        // An embedded null makes File.GetAttributes throw ArgumentException on
        // every platform and every version, which is how the "the read failed"
        // arm is reached. That arm must refuse: answering "not a reparse point"
        // on a failed read would let the file through to the delete call.
        var source = $"{Dir}\\unreadable\0.msi";

        var svc = new DeleteFilesService(FileSystemReporting(source));
        var result = await svc.DeleteFilesAsync(new[] { source });

        // UnknownError, NOT SourceIsReparsePoint: a read that could not be made
        // has not shown the file is a symlink.
        Assert.IsType<UnknownError>(Assert.Single(result.Errors));
        Assert.Equal(0, result.DeletedCount);
    }

    [Fact]
    public async Task Refuses_a_source_it_cannot_resolve()
    {
        // A path on a drive letter nothing is mounted on: no existing ancestor
        // to open, so where the path really leads was never established and the
        // file is refused rather than deleted on the strength of its spelling.
        var unmounted = TestHost.FirstUnmountedDriveLetter();
        if (unmounted is null)
            return; // every letter is in use on this host; nothing to pose the question with

        var source = $@"{unmounted}:\Windows\Installer\unresolvable.msi";

        var svc = new DeleteFilesService(FileSystemReporting(source));
        var result = await svc.DeleteFilesAsync(new[] { source });

        Assert.IsType<UnknownError>(Assert.Single(result.Errors));
        Assert.Equal(0, result.DeletedCount);
    }

    [Theory]
    // ERROR_SHARING_VIOLATION and ERROR_LOCK_VIOLATION as HRESULTs: another
    // program holds the file open, the one IO failure with a cause the user can
    // act on. Discriminated off exactly the two codes MoveFilesService uses, so
    // both halves of the app name the same condition the same way.
    [InlineData(unchecked((int)0x80070020), typeof(FileInUse))]
    [InlineData(unchecked((int)0x80070021), typeof(FileInUse))]
    // Anything else is an IO failure with no cause worth naming.
    [InlineData(unchecked((int)0x80070070), typeof(IOFailure))]
    public async Task An_io_failure_is_filed_as_in_use_only_for_a_sharing_or_lock_violation(
        int hresult, Type expected)
    {
        var source = $@"{Dir}\held.msi";
        var svc = new DeleteFilesService(
            FileSystemReporting(source, new IOException("held", hresult)));

        var result = await svc.DeleteFilesAsync(new[] { source });

        Assert.Equal(0, result.DeletedCount);
        Assert.IsType(expected, Assert.Single(result.Errors));
    }

    [Fact]
    public async Task An_access_refusal_is_filed_as_access_denied()
    {
        var source = $@"{Dir}\refused.msi";
        var svc = new DeleteFilesService(
            FileSystemReporting(source, new UnauthorizedAccessException("refused")));

        var result = await svc.DeleteFilesAsync(new[] { source });

        Assert.Equal(0, result.DeletedCount);
        Assert.IsType<AccessDenied>(Assert.Single(result.Errors));
    }

    [Fact]
    public async Task Refuses_when_the_installer_mutex_is_held()
    {
        var fs = new MockFileSystem();
        var a = AddFile(fs, "a.msi");
        var mutex = new FakeMutexProbe(FakeMutexProbe.Mode.HeldByAnother);
        var svc = new DeleteFilesService(fs, mutex, null);

        var result = await svc.DeleteFilesAsync(new[] { a });

        Assert.True(result.InstallerBusy);
        Assert.Equal(0, result.DeletedCount);
        Assert.Empty(result.Errors);
        Assert.True(fs.File.Exists(a));
    }

    [Fact]
    public async Task Holds_and_releases_the_installer_mutex_when_acquired()
    {
        var fs = new MockFileSystem();
        var a = AddFile(fs, "a.msi");
        var mutex = new FakeMutexProbe(FakeMutexProbe.Mode.Acquire);
        var svc = new DeleteFilesService(fs, mutex, null);

        var result = await svc.DeleteFilesAsync(new[] { a });

        Assert.Equal(1, result.DeletedCount);
        Assert.False(result.InstallerBusy);
        Assert.Equal(1, mutex.Acquired);
        Assert.Equal(1, mutex.Released);
    }

    [Fact]
    public async Task Zero_files_returns_an_empty_result_without_taking_the_mutex()
    {
        var fs = new MockFileSystem();
        var mutex = new FakeMutexProbe(FakeMutexProbe.Mode.Acquire);
        var svc = new DeleteFilesService(fs, mutex, null);

        var result = await svc.DeleteFilesAsync(Array.Empty<string>());

        Assert.Equal(0, result.DeletedCount);
        Assert.Empty(result.Errors);
        // An empty batch mutates nothing, so it never takes the machine-wide
        // installer lock: taking it would block every installer on the machine
        // for a run with no work in it.
        Assert.Equal(0, mutex.AcquireAttempts);
    }
}
