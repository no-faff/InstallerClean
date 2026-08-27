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

        var result = await svc.DeleteFilesAsync(new[] { a, b }, UnderLeaseClaims.None);

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

        var result = await svc.DeleteFilesAsync(new[] { ghost }, UnderLeaseClaims.None);

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

        var result = await svc.DeleteFilesAsync(new[] { ok1, missing, ok2 }, UnderLeaseClaims.None);

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

        await svc.DeleteFilesAsync(files, UnderLeaseClaims.None, progress: progress);

        Assert.Equal(3, reports.Count);
        Assert.Equal(1, reports[0].CurrentFile);
        Assert.Equal(3, reports[2].CurrentFile);
        Assert.All(reports, r => Assert.Equal(3, r.TotalFiles));
    }

    [Fact]
    public async Task A_progress_handler_that_throws_costs_one_file_and_not_the_batch()
    {
        var fs = new MockFileSystem();
        var files = new[] { "a.msi", "b.msi", "c.msi" }.Select(n => AddFile(fs, n)).ToArray();
        var progress = new SyncProgress<OperationProgress>(p =>
        {
            if (p.CurrentFile == 2) throw new InvalidOperationException("stdout closed");
        });
        var svc = new DeleteFilesService(fs);

        var result = await svc.DeleteFilesAsync(files, UnderLeaseClaims.None, progress: progress);

        // The report is inside the per-file try, so a consumer that throws is
        // categorised like any other per-file failure and the batch carries on.
        // From outside the try the same throw left the loop, and the files
        // already deleted went unreported with it.
        Assert.Equal(2, result.DeletedCount);
        var err = Assert.Single(result.Errors);
        Assert.IsType<UnknownError>(err);
        Assert.Equal(files[1], err.FilePath);
        Assert.False(fs.File.Exists(files[0]));
        Assert.True(fs.File.Exists(files[1]));
        Assert.False(fs.File.Exists(files[2]));
    }

    [Fact]
    public async Task A_missing_file_still_advances_the_progress_counter()
    {
        var fs = new MockFileSystem();
        var present = AddFile(fs, "a.msi");
        var ghost = $@"{Dir}\ghost.msi";
        var reports = new List<OperationProgress>();
        var progress = new SyncProgress<OperationProgress>(reports.Add);
        var svc = new DeleteFilesService(fs);

        var result = await svc.DeleteFilesAsync(new[] { present, ghost }, UnderLeaseClaims.None, progress: progress);

        // Pins the half of the report's placement that moving it inside the try
        // had to preserve: it runs ahead of the File.Exists skip, so a file the
        // batch cannot act on does not make the visible counter jump.
        Assert.Equal(2, reports.Count);
        Assert.Equal("ghost.msi", reports[1].CurrentFileName);
        Assert.IsType<MissingSourceFile>(Assert.Single(result.Errors));
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
        var result = await svc.DeleteFilesAsync(files, UnderLeaseClaims.None, progress: progress, cancellationToken: cts.Token);

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

        var result = await svc.DeleteFilesAsync(new[] { outside }, UnderLeaseClaims.None);

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

    /// <summary>
    /// The same filesystem posing as one holding a READ-ONLY file: the first
    /// Delete refuses, and a Delete after the attribute has been cleared
    /// succeeds. <paramref name="clearThrows"/> makes SetAttributes refuse too,
    /// which is the fail-closed leg. <paramref name="retryThrows"/> lets the
    /// clear succeed and beats the retry anyway, which is the only way to reach
    /// the arm that has an attribute to put back.
    /// </summary>
    private static IFileSystem FileSystemHoldingAReadOnlyFile(
        string source, out IFile file, Exception? clearThrows = null, Exception? retryThrows = null)
    {
        var fs = FileSystemReporting(source);
        file = fs.File;
        // Archive as well as ReadOnly, which is what a real file in the cache
        // carries, so the clear has something to preserve and the assertion can
        // say it did.
        var attributes = FileAttributes.ReadOnly | FileAttributes.Archive;
        file.GetAttributes(source).Returns(_ => attributes);
        file.When(f => f.SetAttributes(source, Arg.Any<FileAttributes>())).Do(ci =>
        {
            if (clearThrows is not null) throw clearThrows;
            attributes = ci.Arg<FileAttributes>();
        });
        file.When(f => f.Delete(source)).Do(_ =>
        {
            if (attributes.HasFlag(FileAttributes.ReadOnly))
                throw new UnauthorizedAccessException("read-only");
            if (retryThrows is not null) throw retryThrows;
        });
        return fs;
    }

    [Fact]
    public async Task A_read_only_file_has_the_attribute_cleared_and_the_delete_retried()
    {
        // The shell delete this replaced cleared the attribute and carried on,
        // so a read-only file in the cache went. Without the retry it comes back
        // as "Windows refused access to this file", which is true, useless and
        // indistinguishable from a permissions problem.
        var source = $@"{Dir}\readonly.msi";
        var fs = FileSystemHoldingAReadOnlyFile(source, out var file);

        var result = await new DeleteFilesService(fs).DeleteFilesAsync(new[] { source }, UnderLeaseClaims.None);

        Assert.Equal(1, result.DeletedCount);
        Assert.Empty(result.Errors);
        // Cleared, and cleared to exactly that: only the read-only bit comes
        // off, and the retry hands the file no attribute it did not have.
        file.Received(1).SetAttributes(source, FileAttributes.Archive);
    }

    [Fact]
    public async Task A_permissions_refusal_is_still_access_denied_and_is_not_retried()
    {
        // Not read-only, so there is nothing for the clear to fix and the retry
        // is not attempted. The narrowness is the point: this must not become a
        // second try at every refused delete.
        var source = $@"{Dir}\refused.msi";
        var fs = FileSystemReporting(source, new UnauthorizedAccessException("refused"));
        fs.File.GetAttributes(source).Returns(FileAttributes.Normal);

        var result = await new DeleteFilesService(fs).DeleteFilesAsync(new[] { source }, UnderLeaseClaims.None);

        Assert.Equal(0, result.DeletedCount);
        Assert.IsType<AccessDenied>(Assert.Single(result.Errors));
        fs.File.DidNotReceive().SetAttributes(source, Arg.Any<FileAttributes>());
    }

    [Fact]
    public async Task A_read_only_file_whose_attribute_cannot_be_cleared_fails_closed()
    {
        var source = $@"{Dir}\stuck.msi";
        var fs = FileSystemHoldingAReadOnlyFile(
            source, out var file, clearThrows: new UnauthorizedAccessException("refused"));

        var result = await new DeleteFilesService(fs).DeleteFilesAsync(new[] { source }, UnderLeaseClaims.None);

        Assert.Equal(0, result.DeletedCount);
        Assert.IsType<AccessDenied>(Assert.Single(result.Errors));
        // One attempt, then the same error as before: a clear that throws leaves
        // the file exactly where a delete that throws leaves it.
        file.Received(1).Delete(source);
    }

    [Fact]
    public async Task A_read_only_file_the_retry_could_not_delete_keeps_its_attribute()
    {
        // The clear succeeds and the retried delete is beaten anyway, which is
        // the arm that was leaving one step of an abandoned operation committed:
        // the file stays in the cache, the screen says it could not be deleted,
        // and the read-only bit it arrived with had come off. Neither test above
        // reaches this, one retrying successfully and the other never clearing.
        var source = $@"{Dir}\readonly-refused.msi";
        var fs = FileSystemHoldingAReadOnlyFile(source, out var file,
            retryThrows: new UnauthorizedAccessException("refused after the clear"));

        var result = await new DeleteFilesService(fs).DeleteFilesAsync(new[] { source }, UnderLeaseClaims.None);

        Assert.Equal(0, result.DeletedCount);
        Assert.IsType<AccessDenied>(Assert.Single(result.Errors));
        // The clear happened, so the end state below is a restore rather than an
        // attribute nothing ever touched.
        file.Received(1).SetAttributes(source, FileAttributes.Archive);
        Assert.Equal(FileAttributes.ReadOnly | FileAttributes.Archive, file.GetAttributes(source));
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
        var result = await svc.DeleteFilesAsync(new[] { source }, UnderLeaseClaims.None);

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
        var result = await svc.DeleteFilesAsync(new[] { source }, UnderLeaseClaims.None);

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

        var result = await svc.DeleteFilesAsync(new[] { source }, UnderLeaseClaims.None);

        Assert.Equal(0, result.DeletedCount);
        Assert.IsType(expected, Assert.Single(result.Errors));
    }

    [Fact]
    public async Task An_access_refusal_is_filed_as_access_denied()
    {
        var source = $@"{Dir}\refused.msi";
        var svc = new DeleteFilesService(
            FileSystemReporting(source, new UnauthorizedAccessException("refused")));

        var result = await svc.DeleteFilesAsync(new[] { source }, UnderLeaseClaims.None);

        Assert.Equal(0, result.DeletedCount);
        Assert.IsType<AccessDenied>(Assert.Single(result.Errors));
    }

    [Fact]
    public async Task Refuses_when_the_installer_mutex_is_held()
    {
        var fs = new MockFileSystem();
        var a = AddFile(fs, "a.msi");
        var mutex = new FakeMutexProbe(FakeMutexProbe.Mode.HeldByAnother);
        var svc = new DeleteFilesService(fs, mutex, installerFolderOverride: null);

        var result = await svc.DeleteFilesAsync(new[] { a }, UnderLeaseClaims.None);

        Assert.True(result.InstallerBusy);
        Assert.Equal(0, result.DeletedCount);
        Assert.Empty(result.Errors);
        Assert.True(fs.File.Exists(a));
    }

    [Fact]
    public async Task Refuses_when_the_installer_mutex_cannot_be_acquired_and_nobody_holds_it()
    {
        var fs = new MockFileSystem();
        var a = AddFile(fs, "a.msi");
        var mutex = new FakeMutexProbe(FakeMutexProbe.Mode.RefusedNotHeld);
        var svc = new DeleteFilesService(fs, mutex, installerFolderOverride: null);

        var result = await svc.DeleteFilesAsync(new[] { a }, UnderLeaseClaims.None);

        // A delete without the hold has nothing stopping a program registering a
        // package part-way through it, and no bin to fetch a wrongly-removed file
        // back from, so the batch does not run. The Move path answers this input
        // the same way, for a reason of its own that its test states.
        Assert.True(result.InstallerLockUnavailable);
        Assert.Equal(0, result.DeletedCount);
        Assert.Empty(result.Errors);
        Assert.True(fs.File.Exists(a));
        // Distinguishable from the held case, because the caller answers them
        // differently: the pending-reboot gate can explain that one and can
        // account for this one neither way.
        Assert.False(result.InstallerBusy);
        Assert.Equal(0, mutex.Released);
    }

    [Fact]
    public async Task Holds_and_releases_the_installer_mutex_when_acquired()
    {
        var fs = new MockFileSystem();
        var a = AddFile(fs, "a.msi");
        var mutex = new FakeMutexProbe(FakeMutexProbe.Mode.Acquire);
        var svc = new DeleteFilesService(fs, mutex, installerFolderOverride: null);

        var result = await svc.DeleteFilesAsync(new[] { a }, UnderLeaseClaims.None);

        Assert.Equal(1, result.DeletedCount);
        Assert.False(result.InstallerBusy);
        Assert.Equal(1, mutex.Acquired);
        Assert.Equal(1, mutex.Released);
    }

    [Fact]
    public async Task The_under_lease_re_read_runs_inside_the_hold_and_before_any_file_is_touched()
    {
        // The property the whole check rests on. A re-read taken after the lease
        // was released, or before it was taken, would satisfy every other
        // assertion in this file and be worth nothing: the window it exists to
        // close is exactly the one outside the hold.
        var fs = new MockFileSystem();
        var a = AddFile(fs, "a.msi");
        var mutex = new FakeMutexProbe(FakeMutexProbe.Mode.Acquire);
        bool? sourcePresentAtRecheck = null;
        var reverifier = new FakeReclaimingReverifier(Array.Empty<string>(), mutex,
            atRecheck: () => sourcePresentAtRecheck = fs.File.Exists(a));
        var svc = new DeleteFilesService(fs, mutex, null, reverifier);

        var result = await svc.DeleteFilesAsync(new[] { a }, UnderLeaseClaims.None);

        Assert.Equal(1, reverifier.LeasesHeldWhenCalled);
        Assert.Equal(0, reverifier.LeasesReleasedWhenCalled);
        // The other half of the name, and it needs a real observation rather than
        // a lease count: a re-read taken after the first delete holds the lease
        // just as truthfully. The Move twin makes it on the destination folder;
        // here the batch's own file is what a delete would have taken.
        Assert.True(sourcePresentAtRecheck);
        // Without these the assertion above passes on a batch that deleted
        // nothing at all, which is the shape of vacuous pass this file exists to
        // keep out.
        Assert.Equal(1, result.DeletedCount);
        Assert.False(fs.File.Exists(a));
    }

    [Fact]
    public async Task A_re_read_that_throws_still_releases_the_installer_mutex()
    {
        // The lease is released by a finally that encloses the re-read, so this
        // holds today. Nothing exercised it, and the identity work is about to
        // build in this exact region: a refactor narrowing that try would leave
        // the machine-wide installer mutex held until the process exits, every
        // installer on the machine failing 1618 behind it, with the suite green.
        var fs = new MockFileSystem();
        var a = AddFile(fs, "a.msi");
        var mutex = new FakeMutexProbe(FakeMutexProbe.Mode.Acquire);
        var reverifier = new FakeReclaimingReverifier(Array.Empty<string>(), mutex,
            atRecheck: () => throw new InvalidOperationException("the re-read failed"));
        var svc = new DeleteFilesService(fs, mutex, null, reverifier);

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => svc.DeleteFilesAsync(new[] { a }, UnderLeaseClaims.None));

        Assert.Equal(1, mutex.Acquired);
        Assert.Equal(1, mutex.Released);
        // A re-read that could not answer is not permission to act.
        Assert.True(fs.File.Exists(a));
    }

    [Fact]
    public async Task A_path_reclaimed_under_the_lease_is_kept_back_and_the_rest_go()
    {
        var fs = new MockFileSystem();
        var keep = AddFile(fs, "reclaimed.msp");
        var go = AddFile(fs, "orphan.msi");
        var mutex = new FakeMutexProbe(FakeMutexProbe.Mode.Acquire);
        var svc = new DeleteFilesService(fs, mutex, null,
            new FakeReclaimingReverifier(new[] { keep }));

        var result = await svc.DeleteFilesAsync(new[] { keep, go }, UnderLeaseClaims.None);

        Assert.Equal(1, result.DeletedCount);
        Assert.Equal(new[] { keep }, result.HeldBack);
        // Held back is not failed: nothing went wrong with this file, a program
        // simply wants it again, so it is not in the per-file error list.
        Assert.Empty(result.Errors);
        Assert.True(fs.File.Exists(keep));
        Assert.False(fs.File.Exists(go));
    }

    [Fact]
    public async Task A_batch_the_re_read_empties_reports_what_it_kept_back()
    {
        var fs = new MockFileSystem();
        var only = AddFile(fs, "reclaimed.msp");
        var mutex = new FakeMutexProbe(FakeMutexProbe.Mode.Acquire);
        var svc = new DeleteFilesService(fs, mutex, null,
            new FakeReclaimingReverifier(new[] { only }));

        var result = await svc.DeleteFilesAsync(new[] { only }, UnderLeaseClaims.None);

        Assert.Equal(0, result.DeletedCount);
        Assert.Empty(result.Errors);
        Assert.Equal(new[] { only }, result.HeldBack);
        Assert.True(fs.File.Exists(only));
        // The lease is still released on the short-circuit: the batch ended, and
        // an unreleased machine-wide installer mutex would outlive it.
        Assert.Equal(1, mutex.Released);
    }

    [Fact]
    public async Task The_claims_are_handed_to_the_re_read_untouched()
    {
        var fs = new MockFileSystem();
        var a = AddFile(fs, "a.msp");
        var claims = new[]
        {
            new PatchClaim(a, "{AAAA0000-0000-0000-0000-000000000001}",
                "{1111FFFF-0000-0000-0000-000000000001}", null, 4),
        };
        var mutex = new FakeMutexProbe(FakeMutexProbe.Mode.Acquire);
        var reverifier = new FakeReclaimingReverifier(Array.Empty<string>(), mutex);
        var svc = new DeleteFilesService(fs, mutex, null, reverifier);

        await svc.DeleteFilesAsync(new[] { a }, new UnderLeaseClaims(claims, claims));

        Assert.Equal(claims, reverifier.ClaimsSeen);
    }

    [Fact]
    public async Task Zero_files_returns_an_empty_result_without_taking_the_mutex()
    {
        var fs = new MockFileSystem();
        var mutex = new FakeMutexProbe(FakeMutexProbe.Mode.Acquire);
        var svc = new DeleteFilesService(fs, mutex, installerFolderOverride: null);

        var result = await svc.DeleteFilesAsync(Array.Empty<string>(), UnderLeaseClaims.None);

        Assert.Equal(0, result.DeletedCount);
        Assert.Empty(result.Errors);
        // An empty batch mutates nothing, so it never takes the machine-wide
        // installer lock: taking it would block every installer on the machine
        // for a run with no work in it.
        Assert.Equal(0, mutex.AcquireAttempts);
    }
}
