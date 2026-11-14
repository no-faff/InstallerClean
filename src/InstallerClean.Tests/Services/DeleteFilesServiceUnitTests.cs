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

    // Progress is reported after the hold a file was deleted in has ended, never inside it,
    // so the file a report names is already gone and the report says it is finished: the
    // window's bar counts it as done. While a hold is open every installer on the machine
    // is turned away, and a report can wait on whatever reads it.
    [Fact]
    public async Task Reports_each_file_after_its_hold_has_ended()
    {
        var fs = new MockFileSystem();
        var files = new[] { "a.msi", "b.msi", "c.msi" }.Select(n => AddFile(fs, n)).ToArray();
        var api = new FakeInstallerTransactionApi(0);
        var runner = HoldRunnerFixture.Runner(api, new FakeRecordsFingerprint());
        var holdOpen = new List<bool>();
        var stillThere = new List<bool>();
        var finished = new List<bool>();
        var progress = new SyncProgress<OperationProgress>(p =>
        {
            holdOpen.Add(api.Open);
            stillThere.Add(fs.File.Exists(files[p.CurrentFile - 1]));
            finished.Add(p.Finished);
        });
        var svc = new DeleteFilesService(fs, runner, Dir);

        await svc.DeleteFilesAsync(files, HoldRunnerFixture.CheckedAgainst("A"), progress: progress);

        Assert.Equal(1, api.Commits);
        Assert.Equal(new[] { false, false, false }, holdOpen);
        Assert.Equal(new[] { false, false, false }, stillThere);
        Assert.Equal(new[] { true, true, true }, finished);
    }

    [Fact]
    public async Task A_progress_handler_that_throws_costs_the_batch_nothing()
    {
        var fs = new MockFileSystem();
        var files = new[] { "a.msi", "b.msi", "c.msi" }.Select(n => AddFile(fs, n)).ToArray();
        var reported = new List<int>();
        var progress = new SyncProgress<OperationProgress>(p =>
        {
            reported.Add(p.CurrentFile);
            if (p.CurrentFile == 2) throw new InvalidOperationException("stdout closed");
        });
        var svc = new DeleteFilesService(fs);

        var result = await svc.DeleteFilesAsync(files, UnderLeaseClaims.None, progress: progress);

        // A report names a file already dealt with, so a consumer that throws has
        // nothing of that file's to cost: the throw is recorded and the reports go on.
        Assert.Equal(3, result.DeletedCount);
        Assert.Empty(result.Errors);
        Assert.Equal(new[] { 1, 2, 3 }, reported);
        Assert.All(files, f => Assert.False(fs.File.Exists(f)));
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

        // A file the batch could not act on is still dealt with, as an error, so it
        // is reported like any other and the visible counter does not jump.
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
        var svc = new DeleteFilesService(fs);

        // No throw on a mid-batch cancel: the partial result comes back with
        // Cancelled set and the tally of what was deleted before the stop. The
        // cancel comes while the second file is in hand (its question to the host),
        // so that file is finished and the cancel is seen before the third.
        var result = await svc.DeleteFilesAsync(files, UnderLeaseClaims.None,
            mayAct: path => { if (path == files[1]) cts.Cancel(); return true; },
            cancellationToken: cts.Token);

        Assert.True(result.Cancelled);
        Assert.Equal(2, result.DeletedCount);
        Assert.Empty(result.Errors);
        Assert.False(fs.File.Exists(files[0]));
        Assert.False(fs.File.Exists(files[1]));
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
    public async Task Refuses_when_Windows_Installer_stays_busy_and_touches_nothing()
    {
        var fs = new MockFileSystem();
        var a = AddFile(fs, "a.msi");
        var api = new FakeInstallerTransactionApi(Enumerable.Repeat(1618u, 100).ToArray());
        var runner = HoldRunnerFixture.Runner(api, new FakeRecordsFingerprint(),
            limits: HoldRunnerLimits.Default with { BusyWait = TimeSpan.FromSeconds(5) });
        var svc = new DeleteFilesService(fs, runner, installerFolderOverride: null);

        var result = await svc.DeleteFilesAsync(new[] { a }, HoldRunnerFixture.CheckedAgainst("A"));

        Assert.True(result.InstallerBusy);
        Assert.Equal(0, result.DeletedCount);
        Assert.True(fs.File.Exists(a));
        Assert.Equal(0, api.Commits);
        Assert.True(api.Begins > 1);
    }

    [Fact]
    public async Task Refuses_under_the_DisableRollback_policy_and_touches_nothing()
    {
        var fs = new MockFileSystem();
        var a = AddFile(fs, "a.msi");
        var api = new FakeInstallerTransactionApi(1653);
        var svc = new DeleteFilesService(fs, HoldRunnerFixture.Runner(api, new FakeRecordsFingerprint()), null);

        var result = await svc.DeleteFilesAsync(new[] { a }, HoldRunnerFixture.CheckedAgainst("A"));

        Assert.True(result.InstallerRollbackDisabled);
        Assert.False(result.InstallerBusy);
        Assert.Equal(0, result.DeletedCount);
        Assert.True(fs.File.Exists(a));
        Assert.Equal(0, api.Commits);
    }

    [Fact]
    public async Task Refuses_when_the_hold_cannot_be_taken_and_touches_nothing()
    {
        var fs = new MockFileSystem();
        var a = AddFile(fs, "a.msi");
        var api = new FakeInstallerTransactionApi(1601);
        var svc = new DeleteFilesService(fs, HoldRunnerFixture.Runner(api, new FakeRecordsFingerprint()), null);

        var result = await svc.DeleteFilesAsync(new[] { a }, HoldRunnerFixture.CheckedAgainst("A"));

        Assert.True(result.InstallerLockUnavailable);
        Assert.False(result.InstallerBusy);
        Assert.Equal(0, result.DeletedCount);
        Assert.True(fs.File.Exists(a));
        Assert.Equal(0, api.Commits);
    }

    [Fact]
    public async Task The_reading_is_taken_inside_the_hold_before_any_file_is_touched()
    {
        var fs = new MockFileSystem();
        var a = AddFile(fs, "a.msi");
        var api = new FakeInstallerTransactionApi(0);
        bool? heldAtReading = null;
        bool? presentAtReading = null;
        var fingerprint = new FakeRecordsFingerprint
        {
            OnTake = () => { heldAtReading = api.Open; presentAtReading = fs.File.Exists(a); },
        };
        bool? heldAtDelete = null;
        var svc = new DeleteFilesService(fs, HoldRunnerFixture.Runner(api, fingerprint), null);

        var result = await svc.DeleteFilesAsync(new[] { a }, HoldRunnerFixture.CheckedAgainst("A"),
            mayAct: _ => { heldAtDelete ??= api.Open; return true; });

        Assert.True(heldAtReading);
        Assert.True(presentAtReading);
        Assert.True(heldAtDelete);
        Assert.Equal(1, result.DeletedCount);
        Assert.False(fs.File.Exists(a));
        Assert.Equal(1, api.Commits);
        Assert.Equal(0, api.StrayEnds);
        Assert.False(api.Open);
    }

    [Fact]
    public async Task Progress_is_reported_after_the_hold_has_ended()
    {
        var fs = new MockFileSystem();
        var a = AddFile(fs, "a.msi");
        var b = AddFile(fs, "b.msi");
        var api = new FakeInstallerTransactionApi(0);
        var heldAtReport = new List<bool>();
        var svc = new DeleteFilesService(fs, HoldRunnerFixture.Runner(api, new FakeRecordsFingerprint()), null);

        var result = await svc.DeleteFilesAsync(new[] { a, b }, HoldRunnerFixture.CheckedAgainst("A"),
            new RecordingProgress<OperationProgress>(_ => heldAtReport.Add(api.Open)));

        Assert.Equal(2, result.DeletedCount);
        Assert.Equal(new[] { false, false }, heldAtReport);
    }

    [Fact]
    public async Task A_reading_that_differs_touches_nothing_and_the_re_check_decides()
    {
        var fs = new MockFileSystem();
        var keep = AddFile(fs, "reclaimed.msp");
        var go = AddFile(fs, "orphan.msi");
        var api = new FakeInstallerTransactionApi(0, 0);
        var fingerprint = new FakeRecordsFingerprint { Digest = "B" };
        var reverifier = new FakeRecheckingReverifier(new[] { keep }, newReading: "B");
        var svc = new DeleteFilesService(fs, HoldRunnerFixture.Runner(api, fingerprint, reverifier), null);

        var result = await svc.DeleteFilesAsync(new[] { keep, go }, HoldRunnerFixture.CheckedAgainst("A"));

        Assert.Equal(new[] { keep }, result.HeldBack);
        Assert.Equal(1, result.HeldBackReasons.Reclaimed);
        Assert.Equal(1, result.DeletedCount);
        Assert.True(fs.File.Exists(keep));
        Assert.False(fs.File.Exists(go));
        Assert.Single(reverifier.Calls);
        Assert.Equal(2, api.Commits);
        Assert.Equal(0, api.StrayEnds);
    }

    [Fact]
    public async Task A_batch_checked_against_no_reading_deletes_nothing()
    {
        var fs = new MockFileSystem();
        var a = AddFile(fs, "a.msi");
        var api = new FakeInstallerTransactionApi(0, 0, 0, 0);
        var reverifier = new FakeRecheckingReverifier(newReading: "never");
        var svc = new DeleteFilesService(fs,
            HoldRunnerFixture.Runner(api, new FakeRecordsFingerprint(), reverifier), null);

        var result = await svc.DeleteFilesAsync(new[] { a }, UnderLeaseClaims.None);

        Assert.Equal(0, result.DeletedCount);
        Assert.True(fs.File.Exists(a));
        Assert.Equal(new[] { a }, result.HeldBack);
        Assert.Equal(1, result.HeldBackReasons.RecordsChanged + result.HeldBackReasons.RecordsUnreadable);
    }

    [Fact]
    public async Task Each_hold_begins_and_ends_on_one_thread()
    {
        var fs = new MockFileSystem();
        var a = AddFile(fs, "a.msi");
        var api = new FakeInstallerTransactionApi(0);
        var svc = new DeleteFilesService(fs, HoldRunnerFixture.Runner(api, new FakeRecordsFingerprint()), null);

        await svc.DeleteFilesAsync(new[] { a }, HoldRunnerFixture.CheckedAgainst("A"));

        Assert.Equal(api.BeginThreads, api.CommitThreads);
    }

    [Fact]
    public async Task Zero_files_returns_an_empty_result_without_taking_a_hold()
    {
        var fs = new MockFileSystem();
        var api = new FakeInstallerTransactionApi(0);
        var svc = new DeleteFilesService(fs, HoldRunnerFixture.Runner(api, new FakeRecordsFingerprint()), null);

        var result = await svc.DeleteFilesAsync(Array.Empty<string>(), UnderLeaseClaims.None);

        Assert.Equal(0, result.DeletedCount);
        Assert.Empty(result.Errors);
        Assert.Equal(0, api.Begins);
    }
}
