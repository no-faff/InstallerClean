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
/// categorisation, the destination-collision refusal, and the cancellation
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
        var result = await svc.MoveFilesAsync(new[] { source }, DestDir, UnderLeaseClaims.None);

        Assert.Equal(1, result.MovedCount);
        Assert.Empty(result.Errors);
        Assert.False(fs.File.Exists(source));
        Assert.True(fs.File.Exists($@"{DestDir}\a.msi"));
    }

    [Fact]
    public async Task MoveFilesAsync_refuses_a_name_already_in_the_destination()
    {
        // The suffix this used to append made the completion screen's restore line
        // false for the renamed file: Windows Installer looks for a cached package
        // by the exact path it recorded, so "dup (1).msi" does not go back as
        // "dup.msi", and nothing recorded which file had been renamed. Refusing
        // states the problem and leaves the file recoverable where it is.
        var fs = new MockFileSystem();
        var source = $@"{SourceDir}\dup.msi";
        var existing = $@"{DestDir}\dup.msi";
        fs.AddFile(source, new MockFileData("source bytes"));
        fs.AddFile(existing, new MockFileData("existing bytes"));

        var svc = new MoveFilesService(fs);
        var result = await svc.MoveFilesAsync(new[] { source }, DestDir, UnderLeaseClaims.None);

        Assert.Equal(0, result.MovedCount);
        var error = Assert.Single(result.Errors);
        var collision = Assert.IsType<DestinationCollision>(error);
        Assert.Equal(source, collision.FilePath);
        Assert.Equal("dup.msi", collision.FileName);
        // The source is still in the cache, which is what makes the refusal cost
        // the user nothing they cannot recover: another folder, or an emptied one,
        // moves it.
        Assert.True(fs.File.Exists(source));
        // And nothing was written under a suffixed name.
        Assert.False(fs.File.Exists($@"{DestDir}\dup (1).msi"));
        // The file that was already there is untouched, which is the other half of
        // why this is a refusal rather than an overwrite.
        Assert.Equal("existing bytes", fs.File.ReadAllText(existing));
    }

    [Fact]
    public async Task MoveFilesAsync_a_refused_collision_does_not_stop_the_rest_of_the_batch()
    {
        // The refusal is a per-file outcome and not a batch failure, so a folder
        // holding one clashing name must not cost the user every other file.
        var fs = new MockFileSystem();
        var clash = $@"{SourceDir}\dup.msi";
        var fine = $@"{SourceDir}\other.msi";
        fs.AddFile(clash, new MockFileData("source bytes"));
        fs.AddFile(fine, new MockFileData("other bytes"));
        fs.AddFile($@"{DestDir}\dup.msi", new MockFileData("existing bytes"));

        var svc = new MoveFilesService(fs);
        var result = await svc.MoveFilesAsync(new[] { clash, fine }, DestDir, UnderLeaseClaims.None);

        Assert.Equal(1, result.MovedCount);
        Assert.IsType<DestinationCollision>(Assert.Single(result.Errors));
        Assert.True(fs.File.Exists($@"{DestDir}\other.msi"));
        Assert.True(fs.File.Exists(clash));
    }

    [Fact]
    public async Task MoveFilesAsync_records_MissingSourceFile_for_absent_source()
    {
        var fs = new MockFileSystem();
        fs.AddDirectory(DestDir);
        var ghost = $@"{SourceDir}\ghost.msi";

        var svc = new MoveFilesService(fs);
        var result = await svc.MoveFilesAsync(new[] { ghost }, DestDir, UnderLeaseClaims.None);

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
        var result = await svc.MoveFilesAsync(new[] { ok1, missing, ok2 }, DestDir, UnderLeaseClaims.None);

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
        var result = await svc.MoveFilesAsync(new[] { source }, DestDir, UnderLeaseClaims.None);

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
        await svc.MoveFilesAsync(sources, DestDir, UnderLeaseClaims.None, progress);

        Assert.Equal(3, reports.Count);
        Assert.Equal(1, reports[0].CurrentFile);
        Assert.Equal(3, reports[2].CurrentFile);
        Assert.All(reports, r => Assert.Equal(3, r.TotalFiles));
    }

    [Fact]
    public async Task MoveFilesAsync_progress_handler_that_throws_costs_one_file_and_not_the_batch()
    {
        var fs = new MockFileSystem();
        var sources = new[] { "a.msi", "b.msi", "c.msi" }
            .Select(n => $@"{SourceDir}\{n}").ToArray();
        foreach (var s in sources) fs.AddFile(s, new MockFileData("payload"));
        fs.AddDirectory(DestDir);

        var progress = new Helpers.SyncProgress<OperationProgress>(p =>
        {
            if (p.CurrentFile == 2) throw new InvalidOperationException("stdout closed");
        });

        var svc = new MoveFilesService(fs);
        var result = await svc.MoveFilesAsync(sources, DestDir, UnderLeaseClaims.None, progress);

        // The report is inside the per-file try, so a consumer that throws is
        // categorised like any other per-file failure and the batch carries on.
        // From outside the try the same throw left the loop, and the files
        // already moved went unreported with it.
        Assert.Equal(2, result.MovedCount);
        var err = Assert.Single(result.Errors);
        Assert.IsType<UnknownError>(err);
        Assert.Equal(sources[1], err.FilePath);
        Assert.True(fs.File.Exists($@"{DestDir}\a.msi"));
        Assert.True(fs.File.Exists(sources[1]));
        Assert.True(fs.File.Exists($@"{DestDir}\c.msi"));
    }

    [Fact]
    public async Task MoveFilesAsync_missing_source_still_advances_the_progress_counter()
    {
        var fs = new MockFileSystem();
        var present = $@"{SourceDir}\a.msi";
        var ghost = $@"{SourceDir}\ghost.msi";
        fs.AddFile(present, new MockFileData("payload"));
        fs.AddDirectory(DestDir);

        var reports = new List<OperationProgress>();
        var progress = new Helpers.SyncProgress<OperationProgress>(reports.Add);

        var svc = new MoveFilesService(fs);
        var result = await svc.MoveFilesAsync(new[] { present, ghost }, DestDir, UnderLeaseClaims.None, progress);

        // Pins the half of the report's placement that moving it inside the try
        // had to preserve: it runs ahead of the skip checks, so a file the batch
        // cannot act on does not make the visible counter jump.
        Assert.Equal(2, reports.Count);
        Assert.Equal("ghost.msi", reports[1].CurrentFileName);
        Assert.IsType<MissingSourceFile>(Assert.Single(result.Errors));
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
        var result = await svc.MoveFilesAsync(sources, DestDir, UnderLeaseClaims.None, progress, cts.Token);

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
        var result = await svc.MoveFilesAsync(new[] { outside }, DestDir, UnderLeaseClaims.None);

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

        var svc = new MoveFilesService(fs, mutex, installerFolderOverride: null);
        var result = await svc.MoveFilesAsync(new[] { source }, DestDir, UnderLeaseClaims.None);

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

        var svc = new MoveFilesService(fs, mutex, installerFolderOverride: null);
        var result = await svc.MoveFilesAsync(new[] { source }, DestDir, UnderLeaseClaims.None);

        Assert.False(result.InstallerBusy);
        Assert.Equal(1, result.MovedCount);
        Assert.Equal(1, mutex.Acquired);
        Assert.Equal(1, mutex.Released); // released exactly once, on the worker thread
    }

    [Fact]
    public async Task MoveFilesAsync_refuses_when_the_installer_mutex_cannot_be_acquired_and_nobody_holds_it()
    {
        var fs = new MockFileSystem();
        var source = $@"{SourceDir}\a.msi";
        fs.AddFile(source, new MockFileData("payload"));
        // The destination is deliberately NOT pre-created, which is half of what
        // this test is for: a refusal must leave the folder unmade as well as the
        // file unmoved. The service creates it on any run that gets past the
        // acquire, so an assertion that it does not exist afterwards is an
        // assertion that the acquire is what stopped the batch.
        var mutex = new Helpers.FakeMutexProbe(Helpers.FakeMutexProbe.Mode.RefusedNotHeld);

        var svc = new MoveFilesService(fs, mutex, installerFolderOverride: null);
        var result = await svc.MoveFilesAsync(new[] { source }, DestDir, UnderLeaseClaims.None);

        // Without the hold nothing stops a program registering a package part-way
        // through, so the act-time proof can go stale under the batch. A file this
        // moves out of the cache is as absent from it as a deleted one, so the
        // exposure is the delete path's and the answer matches it.
        Assert.True(result.InstallerLockUnavailable);
        Assert.Equal(0, result.MovedCount);
        Assert.Empty(result.Errors);
        Assert.True(fs.File.Exists(source));
        Assert.False(fs.Directory.Exists(DestDir));
        // Distinguishable from the held case, because the caller answers them
        // differently: the pending-reboot gate can explain that one and can
        // account for this one neither way.
        Assert.False(result.InstallerBusy);
        Assert.Equal(0, mutex.Released);
    }

    [Fact]
    public async Task MoveFilesAsync_re_reads_inside_the_hold_and_before_the_destination_is_made()
    {
        // Two properties at once. Inside the hold, which is the only place the
        // re-read is worth taking; and ahead of the destination work, so a batch
        // it empties does not leave a folder the user never asked for.
        var fs = new MockFileSystem();
        var source = $@"{SourceDir}\a.msi";
        fs.AddFile(source, new MockFileData("payload"));
        var mutex = new Helpers.FakeMutexProbe(Helpers.FakeMutexProbe.Mode.Acquire);
        var reverifier = new Helpers.FakeReclaimingReverifier(new[] { source }, mutex);
        var svc = new MoveFilesService(fs, mutex, null, reverifier);

        var result = await svc.MoveFilesAsync(new[] { source }, DestDir, UnderLeaseClaims.None);

        Assert.Equal(1, reverifier.LeasesHeldWhenCalled);
        Assert.Equal(0, reverifier.LeasesReleasedWhenCalled);
        Assert.Equal(new[] { source }, result.HeldBack);
        Assert.Equal(0, result.MovedCount);
        Assert.True(fs.File.Exists(source));
        Assert.False(fs.Directory.Exists(DestDir));
        Assert.Equal(1, mutex.Released);
    }

    [Fact]
    public async Task A_re_read_that_throws_still_releases_the_installer_mutex()
    {
        // See the delete twin. The lease is released by a finally enclosing the
        // re-read, nothing exercised that, and a narrowed try would leave the
        // machine-wide installer mutex held until the process exits.
        var fs = new MockFileSystem();
        var source = $@"{SourceDir}\a.msi";
        fs.AddFile(source, new MockFileData("payload"));
        var mutex = new Helpers.FakeMutexProbe(Helpers.FakeMutexProbe.Mode.Acquire);
        var reverifier = new Helpers.FakeReclaimingReverifier(Array.Empty<string>(), mutex,
            atRecheck: () => throw new InvalidOperationException("the re-read failed"));
        var svc = new MoveFilesService(fs, mutex, null, reverifier);

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => svc.MoveFilesAsync(new[] { source }, DestDir, UnderLeaseClaims.None));

        Assert.Equal(1, mutex.Acquired);
        Assert.Equal(1, mutex.Released);
        Assert.True(fs.File.Exists(source));
        // The re-read runs ahead of the destination work, so a throw from it
        // leaves no folder behind either.
        Assert.False(fs.Directory.Exists(DestDir));
    }

    [Fact]
    public async Task MoveFilesAsync_keeps_back_a_reclaimed_path_and_moves_the_rest()
    {
        var fs = new MockFileSystem();
        var keep = $@"{SourceDir}\reclaimed.msp";
        var go = $@"{SourceDir}\orphan.msi";
        fs.AddFile(keep, new MockFileData("a"));
        fs.AddFile(go, new MockFileData("b"));
        fs.AddDirectory(DestDir);
        var mutex = new Helpers.FakeMutexProbe(Helpers.FakeMutexProbe.Mode.Acquire);
        var svc = new MoveFilesService(fs, mutex, null,
            new Helpers.FakeReclaimingReverifier(new[] { keep }));

        var result = await svc.MoveFilesAsync(new[] { keep, go }, DestDir, UnderLeaseClaims.None);

        Assert.Equal(1, result.MovedCount);
        Assert.Equal(new[] { keep }, result.HeldBack);
        // Held back is not failed, so it is not in the per-file error list.
        Assert.Empty(result.Errors);
        Assert.True(fs.File.Exists(keep));
        Assert.True(fs.File.Exists($@"{DestDir}\orphan.msi"));
    }

    [Fact]
    public async Task The_claims_are_handed_to_the_re_read_untouched()
    {
        // The re-read can only re-ask about what it is given, and a service that
        // dropped, merged or reordered the claims on the way in would still pass
        // every held-back assertion above, because the fake decides what to
        // reclaim without looking at them. Its Delete twin pins the same thing.
        var fs = new MockFileSystem();
        var source = $@"{SourceDir}\a.msp";
        fs.AddFile(source, new MockFileData("payload"));
        fs.AddDirectory(DestDir);
        var claims = new[]
        {
            new PatchClaim(source, "{AAAA0000-0000-0000-0000-000000000001}",
                "{1111FFFF-0000-0000-0000-000000000001}", null, 4),
        };
        var mutex = new Helpers.FakeMutexProbe(Helpers.FakeMutexProbe.Mode.Acquire);
        var reverifier = new Helpers.FakeReclaimingReverifier(Array.Empty<string>(), mutex);
        var svc = new MoveFilesService(fs, mutex, null, reverifier);

        await svc.MoveFilesAsync(new[] { source }, DestDir, new UnderLeaseClaims(claims, claims));

        Assert.Equal(claims, reverifier.ClaimsSeen);
    }

    [Fact]
    public async Task MoveFilesAsync_zero_files_returns_empty_result()
    {
        var fs = new MockFileSystem();
        fs.AddDirectory(DestDir);

        var svc = new MoveFilesService(fs);
        var result = await svc.MoveFilesAsync(Array.Empty<string>(), DestDir, UnderLeaseClaims.None);

        Assert.Equal(0, result.MovedCount);
        Assert.Empty(result.Errors);
    }

    /// <summary>
    /// A filesystem whose File.Move throws <paramref name="onMove"/> for the one
    /// source path, and which reports nothing else as existing so the destination
    /// is clear and the move is actually attempted. MockFileSystem cannot raise
    /// an IOException carrying a chosen HRESULT, and the HRESULT is the whole
    /// subject of the tests below.
    /// </summary>
    private static IFileSystem FileSystemThatFailsMove(string source, Exception onMove)
    {
        var fs = Substitute.For<IFileSystem>();
        // MockFileSystem's Path is a working implementation; a bare substitute
        // would return null from Combine and GetRandomFileName.
        fs.Path.Returns(new MockFileSystem().Path);
        // The service creates the destination folder through IFileSystem.Directory
        // before the loop. A bare IFileSystem substitute returns null there, so the
        // batch would die on a NullReferenceException before reaching File.Move,
        // and the HRESULT these tests are named for would never be raised.
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
        // every platform and every version, which is how the "the read failed"
        // arm is reached. That arm must refuse: answering "not a reparse point"
        // on a failed read would let the file through.
        var source = $"{SourceDir}\\unreadable\0.msi";
        var (fs, file) = FileSystemReporting(source);

        var svc = new MoveFilesService(fs);
        var result = await svc.MoveFilesAsync(new[] { source }, DestDir, UnderLeaseClaims.None);

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
        var result = await svc.MoveFilesAsync(new[] { source }, DestDir, UnderLeaseClaims.None);

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
        var result = await svc.MoveFilesAsync(new[] { source }, DestDir, UnderLeaseClaims.None);

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
        var result = await svc.MoveFilesAsync(new[] { source }, DestDir, UnderLeaseClaims.None);

        var error = Assert.Single(result.Errors);
        Assert.IsType<IOFailure>(error);
    }

    // The five below drive the state Win32 documents and nothing else in the
    // suite can reach: a File.Move that returns success having copied the file
    // and left the original where it was. See HalfMoveFileSystem.

    [Fact]
    public async Task MoveFilesAsync_finishes_a_move_whose_source_was_read_only()
    {
        var fs = new Helpers.HalfMoveFileSystem();
        var source = $@"{SourceDir}\readonly.msi";
        fs.AddFile(source, new MockFileData("payload") { Attributes = FileAttributes.ReadOnly });
        fs.AddDirectory(DestDir);

        var svc = new MoveFilesService(fs);
        var result = await svc.MoveFilesAsync(new[] { source }, DestDir, UnderLeaseClaims.None);

        // The file the user asked to move has moved: one copy, in the
        // destination, and nothing left in the cache folder.
        Assert.Equal(1, result.MovedCount);
        Assert.Empty(result.Errors);
        Assert.False(fs.File.Exists(source));
        Assert.True(fs.File.Exists($@"{DestDir}\readonly.msi"));
    }

    [Fact]
    public async Task MoveFilesAsync_removes_the_copy_when_the_source_cannot_be_deleted()
    {
        var fs = new Helpers.HalfMoveFileSystem();
        var source = $@"{SourceDir}\locked.msi";
        var destPath = $@"{DestDir}\locked.msi";
        fs.AddFile(source, new MockFileData("payload"));
        fs.AddDirectory(DestDir);
        fs.DeleteFailures[source] =
            new IOException("held open") { HResult = unchecked((int)0x80070020) };

        var svc = new MoveFilesService(fs);
        var result = await svc.MoveFilesAsync(new[] { source }, DestDir, UnderLeaseClaims.None);

        // Reported as failed, and true: the user has exactly one copy of the
        // file, where it started. A duplicate in the backup folder would be the
        // worse outcome, being one the completion screen never mentions.
        Assert.IsType<FileInUse>(Assert.Single(result.Errors));
        Assert.Equal(0, result.MovedCount);
        Assert.True(fs.File.Exists(source));
        Assert.False(fs.File.Exists(destPath));
    }

    [Fact]
    public async Task MoveFilesAsync_files_a_failure_when_even_the_copy_cannot_be_removed()
    {
        var fs = new Helpers.HalfMoveFileSystem();
        var source = $@"{SourceDir}\stuck.msi";
        var destPath = $@"{DestDir}\stuck.msi";
        fs.AddFile(source, new MockFileData("payload"));
        fs.AddDirectory(DestDir);
        fs.DeleteFailures[source] = new UnauthorizedAccessException("source refused");
        fs.DeleteFailures[destPath] = new UnauthorizedAccessException("destination refused");

        var svc = new MoveFilesService(fs);
        var result = await svc.MoveFilesAsync(new[] { source }, DestDir, UnderLeaseClaims.None);

        // Needs a destination that took a create and refuses a delete, which
        // the write probe has already passed once. The file is still reported
        // as failed rather than moved, which is the half that must hold.
        Assert.IsType<AccessDenied>(Assert.Single(result.Errors));
        Assert.Equal(0, result.MovedCount);
        Assert.True(fs.File.Exists(source));
    }

    [Fact]
    public async Task MoveFilesAsync_removes_a_read_only_copy_of_a_source_it_could_not_delete()
    {
        var fs = new Helpers.HalfMoveFileSystem();
        var source = $@"{SourceDir}\readonly-locked.msi";
        var destPath = $@"{DestDir}\readonly-locked.msi";
        fs.AddFile(source, new MockFileData("payload") { Attributes = FileAttributes.ReadOnly });
        fs.AddDirectory(DestDir);
        fs.DeleteFailures[source] =
            new IOException("held open") { HResult = unchecked((int)0x80070020) };

        var svc = new MoveFilesService(fs);
        var result = await svc.MoveFilesAsync(new[] { source }, DestDir, UnderLeaseClaims.None);

        // CopyFile carries the source's attributes to the copy, so the copy of
        // a read-only source is read-only and the discard's own delete is
        // refused unless the attribute comes off first. Without that, the file
        // is in both places under an error saying it was left in place.
        Assert.IsType<FileInUse>(Assert.Single(result.Errors));
        Assert.Equal(0, result.MovedCount);
        Assert.True(fs.File.Exists(source));
        Assert.False(fs.File.Exists(destPath));

        // "Left in place" has to hold of the file's attributes as well as of the
        // file. The reconcile clears read-only so it can delete the source and
        // the delete then fails, so without the restore the run ends with one
        // step of an abandoned operation committed and nothing recording it.
        Assert.True(fs.File.GetAttributes(source).HasFlag(FileAttributes.ReadOnly));
    }

    [Fact]
    public async Task MoveFilesAsync_keeps_the_copy_when_the_source_goes_mid_reconcile()
    {
        var fs = new Helpers.HalfMoveFileSystem();
        var source = $@"{SourceDir}\quarantined.msi";
        var destPath = $@"{DestDir}\quarantined.msi";
        fs.AddFile(source, new MockFileData("payload"));
        fs.AddDirectory(DestDir);
        fs.VanishOnAttributeRead.Add(source);

        var svc = new MoveFilesService(fs);
        var result = await svc.MoveFilesAsync(new[] { source }, DestDir, UnderLeaseClaims.None);

        // Source gone and the copy in place is what a completed move leaves, so
        // this is one, however it got there. Treating the attribute read's
        // failure as the move's would discard the copy and file the result as
        // "still in the cache", leaving the user with no copy at all.
        Assert.Equal(1, result.MovedCount);
        Assert.Empty(result.Errors);
        Assert.False(fs.File.Exists(source));
        Assert.True(fs.File.Exists(destPath));
    }

    // The destination guard's classification, at every combination of its four
    // inputs. It is reachable only as a function: the resolve behind those inputs
    // asks the real kernel whatever IFileSystem is injected, and on a local disk
    // the walk always finds an existing ancestor to open, so no test in this suite
    // can drive the loop into the lost-destination condition. What the integration
    // test next door covers is one condition end to end; what these cover is that
    // the two are told apart at all. Neither covers what the kernel really returns
    // when a share drops, and nothing here should be read as though it did.

    [Theory]
    // Both proven, same folder: nothing happened, the batch carries on.
    [InlineData(true, true, @"D:\backup", @"D:\backup", null)]
    // Both proven, different folder: the destination now points somewhere else.
    [InlineData(true, true, @"D:\backup", @"E:\elsewhere", MoveAbortReason.ResolvesElsewhere)]
    // Proven at capture, unproven now: nothing can be shown about where it points.
    [InlineData(true, false, @"D:\backup", @"D:\backup", MoveAbortReason.StoppedResolving)]
    [InlineData(true, false, @"D:\backup", @"E:\elsewhere", MoveAbortReason.StoppedResolving)]
    // Never proven at capture, so a re-resolve that is also unproven has lost
    // nothing: two best-effort strings that agree are the state the batch started
    // in, and stopping on it would abort every move to an unresolvable path.
    [InlineData(false, false, @"D:\backup", @"D:\backup", null)]
    [InlineData(false, false, @"D:\backup", @"E:\elsewhere", MoveAbortReason.ResolvesElsewhere)]
    // The proof going UP is not a change either, on its own.
    [InlineData(false, true, @"D:\backup", @"D:\backup", null)]
    [InlineData(false, true, @"D:\backup", @"E:\elsewhere", MoveAbortReason.ResolvesElsewhere)]
    public void ClassifyAbort_tells_a_changed_destination_from_a_lost_one(
        bool canonicalProven, bool resolveProven,
        string canonical, string current, MoveAbortReason? expected)
    {
        Assert.Equal(expected,
            MoveFilesService.ClassifyAbort(canonicalProven, resolveProven, canonical, current));
    }

    [Fact]
    public void ClassifyAbort_reports_a_lost_destination_even_where_the_strings_agree()
    {
        // The ordering claim, pinned on the case that shows it is not a precedence
        // chain dressing up one condition. A degraded re-resolve hands back a
        // best-effort string, which for an ordinary path is the same string the
        // capture proved, so a comparison asked first answers "unchanged" and the
        // batch runs on with the one thing a containment check exists to see
        // through no longer established.
        Assert.Equal(MoveAbortReason.StoppedResolving,
            MoveFilesService.ClassifyAbort(true, false, @"D:\backup", @"D:\backup"));
    }

    [Fact]
    public void ClassifyAbort_does_not_read_a_change_of_case_as_a_change_of_folder()
    {
        // GetFinalPathNameByHandle returns the on-disk casing, and the path it was
        // asked about came from a text box. A case-sensitive comparison here would
        // abort an ordinary move at its second file.
        Assert.Null(MoveFilesService.ClassifyAbort(true, true, @"D:\Backup", @"d:\backup"));
    }
}
