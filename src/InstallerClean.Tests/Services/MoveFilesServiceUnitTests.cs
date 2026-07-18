using System.IO.Abstractions;
using System.IO.Abstractions.TestingHelpers;
using NSubstitute;
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
        // DestDir is not pre-created so the test exercises the
        // service's directory-create path.

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
    public async Task MoveFilesAsync_returns_the_partial_result_when_cancelled_mid_batch()
    {
        var fs = new MockFileSystem();
        var sources = new[] { "a.msi", "b.msi", "c.msi" }
            .Select(n => $@"{SourceDir}\{n}").ToArray();
        foreach (var s in sources) fs.AddFile(s, new MockFileData("payload"));
        fs.AddDirectory(DestDir);

        // Cancel after the first progress report so the second
        // iteration's ThrowIfCancellationRequested fires.
        var cts = new CancellationTokenSource();
        var progress = new Helpers.SyncProgress<OperationProgress>(p =>
        {
            if (p.CurrentFile == 1) cts.Cancel();
        });

        var svc = new MoveFilesService(fs);

        // No throw on a mid-batch cancel: the accumulated result comes back with
        // Cancelled set and the count of what completed before the stop.
        var result = await svc.MoveFilesAsync(sources, DestDir, progress, cts.Token);

        Assert.True(result.Cancelled);
        Assert.Equal(1, result.MovedCount);
        Assert.Empty(result.Errors);
        // First file already moved before cancellation landed; the rest stay.
        Assert.True(fs.File.Exists($@"{DestDir}\a.msi"));
        Assert.True(fs.File.Exists($@"{SourceDir}\b.msi"));
        Assert.True(fs.File.Exists($@"{SourceDir}\c.msi"));
    }

    [Fact]
    public async Task MoveFilesAsync_refuses_a_source_outside_the_installer_cache()
    {
        // Cannot-rot boundary test: a source that resolves OUTSIDE
        // C:\Windows\Installer is refused per file even though the file exists.
        // No installer-folder override, so the real cache folder is the
        // boundary; C:\Temp is not inside it.
        var fs = new MockFileSystem();
        const string outside = @"C:\Temp\evil.msi";
        fs.AddFile(outside, new MockFileData("payload"));
        fs.AddDirectory(DestDir);

        var svc = new MoveFilesService(fs);
        var result = await svc.MoveFilesAsync(new[] { outside }, DestDir);

        Assert.Equal(0, result.MovedCount);
        Assert.IsType<CandidateOutsideCache>(Assert.Single(result.Errors));
        Assert.True(fs.File.Exists(outside)); // left where it was
    }

    [Fact]
    public async Task MoveFilesAsync_refuses_when_the_installer_mutex_is_held()
    {
        var fs = new MockFileSystem();
        var source = $@"{SourceDir}\a.msi";
        fs.AddFile(source, new MockFileData("payload"));
        fs.AddDirectory(DestDir);
        var mutex = new Helpers.FakeMutexProbe(Helpers.FakeMutexProbe.Mode.HeldByAnother);

        var svc = new MoveFilesService(fs, mutex);
        var result = await svc.MoveFilesAsync(new[] { source }, DestDir);

        Assert.True(result.InstallerBusy);
        Assert.Equal(0, result.MovedCount);
        Assert.Empty(result.Errors);
        Assert.True(fs.File.Exists(source)); // nothing moved
    }

    [Fact]
    public async Task MoveFilesAsync_holds_and_releases_the_installer_mutex_when_acquired()
    {
        var fs = new MockFileSystem();
        var source = $@"{SourceDir}\a.msi";
        fs.AddFile(source, new MockFileData("payload"));
        fs.AddDirectory(DestDir);
        var mutex = new Helpers.FakeMutexProbe(Helpers.FakeMutexProbe.Mode.Acquire);

        var svc = new MoveFilesService(fs, mutex);
        var result = await svc.MoveFilesAsync(new[] { source }, DestDir);

        Assert.False(result.InstallerBusy);
        Assert.Equal(1, result.MovedCount);
        Assert.Equal(1, mutex.Acquired);
        Assert.Equal(1, mutex.Released); // released exactly once, on the worker thread
    }

    [Fact]
    public async Task MoveFilesAsync_falls_back_and_proceeds_when_the_mutex_cannot_be_acquired()
    {
        var fs = new MockFileSystem();
        var source = $@"{SourceDir}\a.msi";
        fs.AddFile(source, new MockFileData("payload"));
        fs.AddDirectory(DestDir);
        var mutex = new Helpers.FakeMutexProbe(Helpers.FakeMutexProbe.Mode.FallBack);

        var svc = new MoveFilesService(fs, mutex);
        var result = await svc.MoveFilesAsync(new[] { source }, DestDir);

        Assert.False(result.InstallerBusy);
        Assert.Equal(1, result.MovedCount); // proceeded without the hold
        Assert.Equal(0, mutex.Released);
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

    /// <summary>
    /// A filesystem whose File.Move throws <paramref name="onMove"/> for the one
    /// source path, and which reports nothing else as existing so the
    /// unique-name probe takes the first candidate. MockFileSystem cannot raise
    /// an IOException carrying a chosen HRESULT, and the HRESULT is the whole
    /// subject of the tests below.
    /// </summary>
    private static IFileSystem FileSystemThatFailsMove(string source, Exception onMove)
    {
        var fs = Substitute.For<IFileSystem>();
        // MockFileSystem's Path is a working implementation; a bare substitute
        // would return null from Combine and GetRandomFileName.
        fs.Path.Returns(new MockFileSystem().Path);
        // A substituted IDirectory reports the cache folder as absent, which
        // returns the post-batch empty-subdirectory prune at its first line.
        fs.Directory.Returns(Substitute.For<IDirectory>());

        var file = Substitute.For<IFile>();
        fs.File.Returns(file);
        file.Exists(Arg.Any<string>()).Returns(ci => (string?)ci[0] == source);
        file.When(f => f.Move(source, Arg.Any<string>())).Do(_ => throw onMove);
        fs.File.Returns(file);
        return fs;
    }

    /// <summary>
    /// A filesystem reporting exactly one path as existing, with nothing wired
    /// to fail. Lets a test hand the service a source string the real
    /// filesystem could never hold, which is the only way to reach the two
    /// gates below: both deliberately consult the REAL filesystem whatever is
    /// injected, so a MockFileSystem entry cannot pose them a question.
    /// </summary>
    private static (IFileSystem Fs, IFile File) FileSystemReporting(string source)
    {
        var fs = Substitute.For<IFileSystem>();
        fs.Path.Returns(new MockFileSystem().Path);
        fs.Directory.Returns(Substitute.For<IDirectory>());
        var file = Substitute.For<IFile>();
        fs.File.Returns(file);
        file.Exists(Arg.Any<string>()).Returns(ci => (string?)ci[0] == source);
        return (fs, file);
    }

    [Fact]
    public async Task MoveFilesAsync_refuses_a_source_whose_attributes_cannot_be_read()
    {
        // An embedded null makes File.GetAttributes throw ArgumentException on
        // every platform and every version, which is the "the read failed" arm.
        // It used to answer "not a reparse point" and let the file through.
        var source = $"{SourceDir}\\unreadable\0.msi";
        var (fs, file) = FileSystemReporting(source);

        var svc = new MoveFilesService(fs);
        var result = await svc.MoveFilesAsync(new[] { source }, DestDir);

        // UnknownError, NOT SourceIsReparsePoint: that entry tells the user the
        // file IS a symlink, which a read that could not be made has not shown.
        Assert.IsType<UnknownError>(Assert.Single(result.Errors));
        Assert.Equal(0, result.MovedCount);
        file.DidNotReceive().Move(Arg.Any<string>(), Arg.Any<string>());
    }

    [Fact]
    public async Task MoveFilesAsync_refuses_a_source_it_cannot_resolve()
    {
        // A path on a drive letter nothing is mounted on: the resolver finds no
        // existing ancestor to open, so nothing about where the path really
        // leads was ever established and the file is refused rather than moved
        // on the strength of how its name is spelled.
        var unmounted = Helpers.TestHost.FirstUnmountedDriveLetter();
        if (unmounted is null)
            return; // every letter is in use on this host; nothing to pose the question with

        var source = $@"{unmounted}:\Windows\Installer\unresolvable.msi";
        var (fs, file) = FileSystemReporting(source);

        var svc = new MoveFilesService(fs);
        var result = await svc.MoveFilesAsync(new[] { source }, DestDir);

        // UnknownError rather than CandidateOutsideCache, for the same reason
        // as the test above: the check did not place the file anywhere, so the
        // report must not say where it is.
        Assert.IsType<UnknownError>(Assert.Single(result.Errors));
        Assert.Equal(0, result.MovedCount);
        file.DidNotReceive().Move(Arg.Any<string>(), Arg.Any<string>());
    }

    [Theory]
    [InlineData(0x80070020)] // ERROR_SHARING_VIOLATION
    [InlineData(0x80070021)] // ERROR_LOCK_VIOLATION
    public async Task MoveFilesAsync_files_a_held_open_file_as_in_use(long hresult)
    {
        var source = $@"{SourceDir}\locked.msi";
        var fs = FileSystemThatFailsMove(source,
            new IOException("held open") { HResult = unchecked((int)hresult) });

        var svc = new MoveFilesService(fs);
        var result = await svc.MoveFilesAsync(new[] { source }, DestDir);

        // The one IO failure with a cause the user can act on, so it must not
        // be flattened into the generic "Windows reported a file error".
        var error = Assert.Single(result.Errors);
        Assert.IsType<FileInUse>(error);
        Assert.Equal(0, result.MovedCount);
    }

    [Fact]
    public async Task MoveFilesAsync_files_any_other_io_error_as_a_generic_failure()
    {
        var source = $@"{SourceDir}\a.msi";
        // ERROR_DISK_FULL: real, and nothing to do with the file being held.
        var fs = FileSystemThatFailsMove(source,
            new IOException("disk full") { HResult = unchecked((int)0x80070070) });

        var svc = new MoveFilesService(fs);
        var result = await svc.MoveFilesAsync(new[] { source }, DestDir);

        var error = Assert.Single(result.Errors);
        Assert.IsType<IOFailure>(error);
    }
}
