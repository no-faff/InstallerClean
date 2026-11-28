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
/// progress and cancellation. A file really moves on the mock, so each test
/// asserts where the file ended. The two safety gates they exercise (reparse
/// point, containment) deliberately read the REAL filesystem whatever is
/// injected, which is what the refusal tests below turn on: a mock cannot talk
/// its way past either.
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

    private static DeleteFilesService ServiceWith(MockFileSystem fs, ScriptedInHandFolder hand) =>
        new(fs, null, null, hand, null);

    [Fact]
    public async Task Deletes_all_files_in_a_clean_batch()
    {
        var fs = new MockFileSystem();
        var a = AddFile(fs, "a.msi");
        var b = AddFile(fs, "b.msi");
        var svc = new DeleteFilesService(fs);

        var result = await svc.DeleteFilesAsync(StandingChecks.Of(new[] { a, b }));

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

        var result = await svc.DeleteFilesAsync(StandingChecks.Of(new[] { ghost }));

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

        var result = await svc.DeleteFilesAsync(StandingChecks.Of(new[] { ok1, missing, ok2 }));

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

        await svc.DeleteFilesAsync(StandingChecks.Of(files), progress: progress);

        Assert.Equal(3, reports.Count);
        Assert.Equal(1, reports[0].CurrentFile);
        Assert.Equal(3, reports[2].CurrentFile);
        Assert.All(reports, r => Assert.Equal(3, r.TotalFiles));
        Assert.All(reports, r => Assert.Null(r.Check));
    }

    // The window's progress label counts the file a report names as still in hand.
    [Fact]
    public async Task Reports_each_file_before_deleting_it()
    {
        var fs = new MockFileSystem();
        var files = new[] { "a.msi", "b.msi", "c.msi" }.Select(n => AddFile(fs, n)).ToArray();
        var stillThere = new List<bool>();
        var progress = new SyncProgress<OperationProgress>(p => stillThere.Add(fs.File.Exists(files[p.CurrentFile - 1])));
        var svc = new DeleteFilesService(fs);

        await svc.DeleteFilesAsync(StandingChecks.Of(files), progress: progress);

        Assert.Equal(new[] { true, true, true }, stillThere);
        Assert.All(files, f => Assert.False(fs.File.Exists(f)));
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

        var result = await svc.DeleteFilesAsync(StandingChecks.Of(files), progress: progress);

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

        var result = await svc.DeleteFilesAsync(StandingChecks.Of(new[] { present, ghost }), progress: progress);

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
        var progress = new SyncProgress<OperationProgress>(p => { if (p.CurrentFile == 2) cts.Cancel(); });
        var svc = new DeleteFilesService(fs);

        // No throw on a mid-batch cancel: the partial result comes back with
        // Cancelled set and the tally of what was deleted before the stop. The
        // cancel comes during the second file's report, so that file is left alone.
        var result = await svc.DeleteFilesAsync(StandingChecks.Of(files), progress: progress, cancellationToken: cts.Token);

        Assert.True(result.Cancelled);
        Assert.Equal(1, result.DeletedCount);
        Assert.Empty(result.Errors);
        Assert.False(fs.File.Exists(files[0]));
        Assert.True(fs.File.Exists(files[1]));
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

        var result = await svc.DeleteFilesAsync(StandingChecks.Of(new[] { outside }));

        Assert.Equal(0, result.DeletedCount);
        Assert.IsType<CandidateOutsideCache>(Assert.Single(result.Errors));
        Assert.True(fs.File.Exists(outside));
    }

    /// <summary>
    /// A filesystem reporting exactly one path as existing, with every other call
    /// unwired. See the twin in <see cref="MoveFilesServiceUnitTests"/>: the
    /// reparse and containment gates consult the REAL filesystem whatever is
    /// injected, so reaching them means handing the service a source string the
    /// real filesystem cannot hold, which MockFileSystem will not store.
    /// </summary>
    private static IFileSystem FileSystemReporting(string source, out IFile file)
    {
        var fs = Substitute.For<IFileSystem>();
        fs.Directory.Returns(Substitute.For<IDirectory>());
        file = Substitute.For<IFile>();
        fs.File.Returns(file);
        file.Exists(Arg.Any<string>()).Returns(ci => (string?)ci[0] == source);
        return fs;
    }

    [Fact]
    public async Task A_read_only_file_is_deleted()
    {
        // A read-only file in the cache goes: the hand clears the attribute as
        // part of its delete. Without that it would come back as "Windows
        // refused access to this file", which is true, useless and
        // indistinguishable from a permissions problem.
        var fs = new MockFileSystem();
        var source = $@"{Dir}\readonly.msi";
        fs.AddFile(source, new MockFileData("payload") { Attributes = FileAttributes.ReadOnly | FileAttributes.Archive });
        var svc = new DeleteFilesService(fs);

        var result = await svc.DeleteFilesAsync(StandingChecks.Of(source));

        Assert.Equal(1, result.DeletedCount);
        Assert.Empty(result.Errors);
        Assert.False(fs.File.Exists(source));
    }

    [Fact]
    public async Task A_permissions_refusal_is_access_denied_and_is_asked_once()
    {
        // The narrowness is the point: a refused delete is filed, with the file
        // back where it was, and is not tried a second time.
        var fs = new MockFileSystem();
        var source = AddFile(fs, "refused.msi");
        var hand = new ScriptedInHandFolder(fs).Refuse("delete", source, 5);

        var result = await ServiceWith(fs, hand).DeleteFilesAsync(StandingChecks.Of(source));

        Assert.Equal(0, result.DeletedCount);
        Assert.IsType<AccessDenied>(Assert.Single(result.Errors));
        Assert.True(fs.File.Exists(source));
        Assert.Equal(1, hand.Steps.Count(step => step == "delete refused.msi"));
    }

    [Fact]
    public async Task A_read_only_file_whose_delete_is_refused_goes_back_keeping_its_attribute()
    {
        // The file is out of its place when the delete is refused. It goes back
        // where it was, with the read-only bit it arrived with, and the screen
        // says it could not be deleted.
        var fs = new MockFileSystem();
        var source = $@"{Dir}\readonly-refused.msi";
        fs.AddFile(source, new MockFileData("payload") { Attributes = FileAttributes.ReadOnly | FileAttributes.Archive });
        var hand = new ScriptedInHandFolder(fs).Refuse("delete", source, 5);

        var result = await ServiceWith(fs, hand).DeleteFilesAsync(StandingChecks.Of(source));

        Assert.Equal(0, result.DeletedCount);
        Assert.IsType<AccessDenied>(Assert.Single(result.Errors));
        Assert.True(fs.File.Exists(source));
        Assert.True(fs.File.GetAttributes(source).HasFlag(FileAttributes.ReadOnly));
        Assert.Contains("put-back readonly-refused.msi", hand.Steps);
    }

    [Fact]
    public async Task Refuses_a_source_whose_attributes_cannot_be_read()
    {
        // An embedded null makes File.GetAttributes throw ArgumentException on
        // every platform and every version, which is how the "the read failed"
        // arm is reached. That arm must refuse: answering "not a reparse point"
        // on a failed read would let the file through to the delete call.
        var source = $"{Dir}\\unreadable\0.msi";

        var fs = FileSystemReporting(source, out var file);
        var svc = new DeleteFilesService(fs);
        var result = await svc.DeleteFilesAsync(StandingChecks.Of(source));

        // UnknownError, NOT SourceIsReparsePoint: a read that could not be made
        // has not shown the file is a symlink.
        Assert.IsType<UnknownError>(Assert.Single(result.Errors));
        Assert.Equal(0, result.DeletedCount);
        // The source checks refuse the file before it is taken, so nothing is
        // renamed or deleted.
        file.DidNotReceive().Move(Arg.Any<string>(), Arg.Any<string>());
        file.DidNotReceive().Delete(Arg.Any<string>());
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

        var fs = FileSystemReporting(source, out var file);
        var svc = new DeleteFilesService(fs);
        var result = await svc.DeleteFilesAsync(StandingChecks.Of(source));

        Assert.IsType<UnknownError>(Assert.Single(result.Errors));
        Assert.Equal(0, result.DeletedCount);
        file.DidNotReceive().Move(Arg.Any<string>(), Arg.Any<string>());
        file.DidNotReceive().Delete(Arg.Any<string>());
    }

    [Theory]
    // ERROR_SHARING_VIOLATION and ERROR_LOCK_VIOLATION: another program holds
    // the file open, the one IO failure with a cause the user can act on.
    // Discriminated off exactly the two codes MoveFilesService uses, so both
    // halves of the app name the same condition the same way.
    [InlineData(32, typeof(FileInUse))]
    [InlineData(33, typeof(FileInUse))]
    // Anything else is an IO failure with no cause worth naming.
    [InlineData(112, typeof(IOFailure))]
    public async Task A_refused_delete_is_filed_as_in_use_only_for_a_sharing_or_lock_violation(
        int windowsError, Type expected)
    {
        var fs = new MockFileSystem();
        var source = AddFile(fs, "held.msi");
        var hand = new ScriptedInHandFolder(fs).Refuse("delete", source, windowsError);

        var result = await ServiceWith(fs, hand).DeleteFilesAsync(StandingChecks.Of(source));

        Assert.Equal(0, result.DeletedCount);
        Assert.IsType(expected, Assert.Single(result.Errors));
        // A refused delete leaves the file where it was.
        Assert.True(fs.File.Exists(source));
        Assert.Contains("put-back held.msi", hand.Steps);
    }

    [Fact]
    public async Task A_file_something_else_holds_open_is_tried_twice_and_filed_as_in_use()
    {
        var fs = new MockFileSystem();
        var source = AddFile(fs, "held.msi");
        var hand = new ScriptedInHandFolder(fs).InUse(source, times: 2);

        var result = await ServiceWith(fs, hand).DeleteFilesAsync(StandingChecks.Of(source));

        Assert.Equal(0, result.DeletedCount);
        Assert.IsType<FileInUse>(Assert.Single(result.Errors));
        Assert.True(fs.File.Exists(source));
        // The second try comes after the last file.
        Assert.Equal(new[] { "hold held.msi", "hold held.msi" }, hand.Steps);
    }

    [Fact]
    public async Task A_file_that_goes_as_the_open_reaches_it_is_filed_as_no_longer_there()
    {
        var fs = new MockFileSystem();
        var source = AddFile(fs, "gone.msi");
        var hand = new ScriptedInHandFolder(fs);
        // The step is recorded before the hand's own open, so the file is gone when that open looks.
        hand.OnStep = (step, path) =>
        {
            if (step == "hold" && path == source) fs.RemoveFile(source);
        };

        var result = await ServiceWith(fs, hand).DeleteFilesAsync(StandingChecks.Of(source));

        Assert.Equal(0, result.DeletedCount);
        Assert.IsType<MissingSourceFile>(Assert.Single(result.Errors));
    }

    [Fact]
    public async Task An_access_refusal_is_filed_as_access_denied()
    {
        var fs = new MockFileSystem();
        var source = AddFile(fs, "refused.msi");
        var hand = new ScriptedInHandFolder(fs).Refuse("delete", source, 5);

        var result = await ServiceWith(fs, hand).DeleteFilesAsync(StandingChecks.Of(source));

        Assert.Equal(0, result.DeletedCount);
        Assert.IsType<AccessDenied>(Assert.Single(result.Errors));
        Assert.True(fs.File.Exists(source));
    }

    [Fact]
    public async Task Zero_files_returns_an_empty_result()
    {
        var fs = new MockFileSystem();
        var svc = new DeleteFilesService(fs);

        var result = await svc.DeleteFilesAsync(StandingChecks.Of(Array.Empty<string>()));

        Assert.Equal(0, result.DeletedCount);
        Assert.Empty(result.Errors);
    }
}
