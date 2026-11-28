using System.IO.Abstractions;
using System.IO.Abstractions.TestingHelpers;
using NSubstitute;
using InstallerClean.Helpers;
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
/// path without touching the disk at all. A refused write probe is
/// driven through a substituted destination guard, which is how the
/// message for each kind of refusal is held here; what Windows answers
/// on real folders is DestinationGuardWriteProbeTests' job.
/// </summary>
public class MoveFilesServiceUnitTests
{
    private const string SourceDir = @"C:\Windows\Installer";
    private const string DestDir = @"D:\backup\installer";

    // A destination inside the installer cache, and one inside a system folder,
    // each built from the folder the gate itself asks Windows about, so the two
    // tests that use them are one test on whichever host runs them. The gate ahead
    // of the containment pair takes only a fully qualified path, and a
    // drive-letter spelling is not one off Windows while a slash-rooted one is not
    // one on it.
    private static readonly string InsideInstallerCache = Path.GetFullPath(
        Path.Combine(InstallerCacheHelpers.InstallerFolder, "installerclean-gate-test"));

    private static readonly string InsideSystemFolder = Path.GetFullPath(Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData),
        "installerclean-gate-test"));

    [Fact]
    public async Task MoveFilesAsync_moves_a_single_file()
    {
        var fs = new MockFileSystem();
        var source = $@"{SourceDir}\a.msi";
        fs.AddFile(source, new MockFileData("payload"));
        fs.AddDirectory(DestDir);

        var svc = new MoveFilesService(fs);
        var result = await svc.MoveFilesAsync(Helpers.StandingChecks.Of(new[] { source }), DestDir);

        Assert.Equal(1, result.MovedCount);
        Assert.Empty(result.Errors);
        Assert.False(fs.File.Exists(source));
        Assert.True(fs.File.Exists($@"{DestDir}\a.msi"));
    }

    [Fact]
    public async Task MoveFilesAsync_refuses_a_name_already_in_the_destination()
    {
        // A name already in the destination is refused rather than worked around,
        // and what decides that is a promise the main window makes before the move:
        // putting the moved files back into the cache folder restores everything.
        // Windows Installer looks for a cached package by the exact path it
        // recorded, so a file parked as "dup (1).msi" does not go back as
        // "dup.msi". Refusing states the problem and leaves the file recoverable
        // where it is.
        var fs = new MockFileSystem();
        var source = $@"{SourceDir}\dup.msi";
        var existing = $@"{DestDir}\dup.msi";
        fs.AddFile(source, new MockFileData("source bytes"));
        fs.AddFile(existing, new MockFileData("existing bytes"));

        var svc = new MoveFilesService(fs);
        var result = await svc.MoveFilesAsync(Helpers.StandingChecks.Of(new[] { source }), DestDir);

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
        var result = await svc.MoveFilesAsync(Helpers.StandingChecks.Of(new[] { clash, fine }), DestDir);

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
        var result = await svc.MoveFilesAsync(Helpers.StandingChecks.Of(new[] { ghost }), DestDir);

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
        var result = await svc.MoveFilesAsync(Helpers.StandingChecks.Of(new[] { ok1, missing, ok2 }), DestDir);

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
        var result = await svc.MoveFilesAsync(Helpers.StandingChecks.Of(new[] { source }), DestDir);

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
        await svc.MoveFilesAsync(Helpers.StandingChecks.Of(sources), DestDir, progress);

        Assert.Equal(3, reports.Count);
        Assert.Equal(1, reports[0].CurrentFile);
        Assert.Equal(3, reports[2].CurrentFile);
        Assert.All(reports, r => Assert.Equal(3, r.TotalFiles));
        Assert.All(reports, r => Assert.Null(r.Check));
    }

    // The window's progress label counts the file a report names as still in hand.
    [Fact]
    public async Task MoveFilesAsync_reports_each_file_before_moving_it()
    {
        var fs = new MockFileSystem();
        var sources = new[] { "a.msi", "b.msi", "c.msi" }
            .Select(n => $@"{SourceDir}\{n}").ToArray();
        foreach (var s in sources) fs.AddFile(s, new MockFileData("payload"));
        fs.AddDirectory(DestDir);

        var stillThere = new List<bool>();
        var progress = new Helpers.SyncProgress<OperationProgress>(p =>
            stillThere.Add(fs.File.Exists(sources[p.CurrentFile - 1])));

        var svc = new MoveFilesService(fs);
        await svc.MoveFilesAsync(Helpers.StandingChecks.Of(sources), DestDir, progress);

        Assert.Equal(new[] { true, true, true }, stillThere);
        Assert.All(sources, s => Assert.False(fs.File.Exists(s)));
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
        var result = await svc.MoveFilesAsync(Helpers.StandingChecks.Of(sources), DestDir, progress);

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
        var result = await svc.MoveFilesAsync(Helpers.StandingChecks.Of(new[] { present, ghost }), DestDir, progress);

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

        // Cancel during the second progress report, so the token asked again after
        // it leaves the second file alone.
        var cts = new CancellationTokenSource();
        var progress = new Helpers.SyncProgress<OperationProgress>(p =>
        {
            if (p.CurrentFile == 2) cts.Cancel();
        });

        var svc = new MoveFilesService(fs);

        // No throw on a mid-batch cancel: the accumulated result comes back with
        // Cancelled set and the count of what completed before the stop.
        var result = await svc.MoveFilesAsync(Helpers.StandingChecks.Of(sources), DestDir, progress, cts.Token);

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
        var result = await svc.MoveFilesAsync(Helpers.StandingChecks.Of(new[] { outside }), DestDir);

        Assert.Equal(0, result.MovedCount);
        Assert.IsType<CandidateOutsideCache>(Assert.Single(result.Errors));
        Assert.True(fs.File.Exists(outside)); // left where it was
    }

    [Fact]
    public async Task MoveFilesAsync_zero_files_returns_empty_result()
    {
        var fs = new MockFileSystem();
        fs.AddDirectory(DestDir);

        var svc = new MoveFilesService(fs);
        var result = await svc.MoveFilesAsync(Helpers.StandingChecks.Of(Array.Empty<string>()), DestDir);

        Assert.Equal(0, result.MovedCount);
        Assert.Empty(result.Errors);
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
        // The destination answers that it is there, as a folder the move has just made
        // does, so the move opens it for the batch and goes on to the file.
        var directory = Substitute.For<IDirectory>();
        directory.Exists(DestDir).Returns(true);
        fs.Directory.Returns(directory);
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
        var result = await svc.MoveFilesAsync(Helpers.StandingChecks.Of(new[] { source }), DestDir);

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
        var result = await svc.MoveFilesAsync(Helpers.StandingChecks.Of(new[] { source }), DestDir);

        // UnknownError rather than CandidateOutsideCache, for the same reason
        // as the test above: the check did not place the file anywhere, so the
        // report must not say where it is.
        Assert.IsType<UnknownError>(Assert.Single(result.Errors));
        Assert.Equal(0, result.MovedCount);
        file.DidNotReceive().Move(Arg.Any<string>(), Arg.Any<string>());
    }

    [Theory]
    [InlineData(32)] // ERROR_SHARING_VIOLATION
    [InlineData(33)] // ERROR_LOCK_VIOLATION
    public async Task MoveFilesAsync_files_a_held_open_file_as_in_use(int windowsError)
    {
        var fs = new MockFileSystem();
        var source = $@"{SourceDir}\locked.msi";
        fs.AddFile(source, new MockFileData("payload"));
        var hand = new Helpers.ScriptedInHandFolder(fs).Refuse("delete", source, windowsError);

        var svc = new MoveFilesService(fs, null, null, hand, null, null);
        var result = await svc.MoveFilesAsync(Helpers.StandingChecks.Of(source), DestDir);

        // The one IO failure with a cause the user can act on, so it must not
        // be flattened into the generic "Windows reported a file error".
        var error = Assert.Single(result.Errors);
        Assert.IsType<FileInUse>(error);
        Assert.Equal(0, result.MovedCount);
        // The user has exactly one copy of the file, where it started.
        Assert.True(fs.File.Exists(source));
        Assert.False(fs.File.Exists($@"{DestDir}\locked.msi"));
        Assert.Contains("put-back locked.msi", hand.Steps);
    }

    [Fact]
    public async Task MoveFilesAsync_tries_a_file_something_has_open_a_second_time_and_files_it_as_in_use()
    {
        var fs = new MockFileSystem();
        var source = $@"{SourceDir}\locked.msi";
        fs.AddFile(source, new MockFileData("payload"));
        var hand = new Helpers.ScriptedInHandFolder(fs).InUse(source, times: 2);

        var svc = new MoveFilesService(fs, null, null, hand, null, null);
        var result = await svc.MoveFilesAsync(Helpers.StandingChecks.Of(source), DestDir);

        Assert.IsType<FileInUse>(Assert.Single(result.Errors));
        Assert.Equal(0, result.MovedCount);
        Assert.True(fs.File.Exists(source));
        Assert.False(fs.File.Exists($@"{DestDir}\locked.msi"));
        // This destination is on another drive, so the file is opened to be copied first, and the second try follows the last file.
        Assert.Equal(new[] { "copy-open locked.msi", "copy-open locked.msi" }, hand.Steps);
    }

    [Fact]
    public async Task MoveFilesAsync_moves_a_file_that_was_in_use_only_for_the_first_try()
    {
        var fs = new MockFileSystem();
        var source = $@"{SourceDir}\scanned.msi";
        fs.AddFile(source, new MockFileData("payload"));
        var hand = new Helpers.ScriptedInHandFolder(fs).InUse(source, times: 1);

        var svc = new MoveFilesService(fs, null, null, hand, null, null);
        var result = await svc.MoveFilesAsync(Helpers.StandingChecks.Of(source), DestDir);

        Assert.Equal(1, result.MovedCount);
        Assert.Empty(result.Errors);
        Assert.False(fs.File.Exists(source));
        Assert.True(fs.File.Exists($@"{DestDir}\scanned.msi"));
    }

    [Fact]
    public async Task MoveFilesAsync_files_any_other_io_error_as_a_generic_failure()
    {
        var fs = new MockFileSystem();
        var source = $@"{SourceDir}\a.msi";
        fs.AddFile(source, new MockFileData("payload"));
        // ERROR_DISK_FULL: real, and nothing to do with the file being held.
        var hand = new Helpers.ScriptedInHandFolder(fs).Refuse("delete", source, 112);

        var svc = new MoveFilesService(fs, null, null, hand, null, null);
        var result = await svc.MoveFilesAsync(Helpers.StandingChecks.Of(source), DestDir);

        var error = Assert.Single(result.Errors);
        Assert.IsType<IOFailure>(error);
        Assert.Equal(0, result.MovedCount);
        Assert.True(fs.File.Exists(source));
        Assert.False(fs.File.Exists($@"{DestDir}\a.msi"));
        Assert.Contains("put-back a.msi", hand.Steps);
    }

    [Fact]
    public async Task MoveFilesAsync_files_an_access_refusal_as_access_denied_and_leaves_no_copy()
    {
        var fs = new MockFileSystem();
        var source = $@"{SourceDir}\a.msi";
        fs.AddFile(source, new MockFileData("payload"));
        var hand = new Helpers.ScriptedInHandFolder(fs).Refuse("delete", source, 5);

        var svc = new MoveFilesService(fs, null, null, hand, null, null);
        var result = await svc.MoveFilesAsync(Helpers.StandingChecks.Of(source), DestDir);

        Assert.IsType<AccessDenied>(Assert.Single(result.Errors));
        Assert.Equal(0, result.MovedCount);
        Assert.True(fs.File.Exists(source));
        Assert.False(fs.File.Exists($@"{DestDir}\a.msi"));
    }

    [Fact]
    public async Task A_source_that_goes_as_the_open_reaches_it_is_filed_as_no_longer_there()
    {
        // The file is there for the existence check and gone when the hand opens
        // it, as when Windows Installer removes a copy it has finished with. The
        // generic file error says the file was left in place, which it was not.
        // The copy made before the open is removed again, so nothing of the file
        // is left in the destination.
        var fs = new MockFileSystem();
        var source = $@"{SourceDir}\a.msi";
        fs.AddFile(source, new MockFileData("payload"));
        var hand = new Helpers.ScriptedInHandFolder(fs);
        // The step is recorded before the hand's own open, so the file is gone when that open looks.
        hand.OnStep = (step, path) =>
        {
            if (step == "hold" && path == source) fs.RemoveFile(source);
        };

        var svc = new MoveFilesService(fs, null, null, hand, null, null);
        var result = await svc.MoveFilesAsync(Helpers.StandingChecks.Of(source), DestDir);

        Assert.IsType<MissingSourceFile>(Assert.Single(result.Errors));
        Assert.Equal(0, result.MovedCount);
        Assert.False(fs.File.Exists($@"{DestDir}\a.msi"));
    }

    [Fact]
    public async Task MoveFilesAsync_to_a_folder_on_the_same_drive_goes_by_rename()
    {
        // A destination on the Installer folder's own drive letter is reached by a
        // rename, so the file is not copied first.
        var fs = new MockFileSystem();
        var source = $@"{SourceDir}\a.msi";
        const string sameDrive = @"C:\ic-test-backup";
        fs.AddFile(source, new MockFileData("payload"));
        var hand = new Helpers.ScriptedInHandFolder(fs);

        var svc = new MoveFilesService(fs, null, null, hand, null, null);
        var result = await svc.MoveFilesAsync(Helpers.StandingChecks.Of(source), sameDrive);

        Assert.Equal(1, result.MovedCount);
        Assert.Empty(result.Errors);
        Assert.Contains("move-into a.msi", hand.Steps);
        Assert.DoesNotContain("copy-open a.msi", hand.Steps);
        Assert.False(fs.File.Exists(source));
        Assert.True(fs.File.Exists($@"{sameDrive}\a.msi"));
    }

    [Fact]
    public async Task MoveFilesAsync_to_a_folder_on_another_drive_goes_by_copy()
    {
        // A destination on another drive letter cannot be reached by a rename, so
        // the file is copied first and the original is deleted once it is taken.
        var fs = new MockFileSystem();
        var source = $@"{SourceDir}\a.msi";
        fs.AddFile(source, new MockFileData("payload"));
        var hand = new Helpers.ScriptedInHandFolder(fs);

        var svc = new MoveFilesService(fs, null, null, hand, null, null);
        var result = await svc.MoveFilesAsync(Helpers.StandingChecks.Of(source), DestDir);

        Assert.Equal(1, result.MovedCount);
        Assert.Empty(result.Errors);
        var copyOpen = hand.Steps.IndexOf("copy-open a.msi");
        Assert.True(copyOpen >= 0);
        Assert.True(copyOpen < hand.Steps.IndexOf("hold a.msi"));
        Assert.True(copyOpen < hand.Steps.IndexOf("delete a.msi"));
        Assert.DoesNotContain("move-into a.msi", hand.Steps);
        Assert.False(fs.File.Exists(source));
        Assert.True(fs.File.Exists($@"{DestDir}\a.msi"));
    }

    // THE TWO CONTAINMENT GATES AT THE SERVICE BOUNDARY, one test each, driven
    // through the real method with a forbidden destination. They are anchored
    // inside MoveFilesAsync so that a caller arriving at the service directly
    // meets them, and a test that asked the helper behind them would be answering
    // about something else.
    //
    // WHAT EACH ASSERTION HOLDS, because the two hold different halves. The same
    // destination is refused a second time once the destination folder has been
    // created, and that refusal carries the same exception type and the same text,
    // so the folder is what tells the pair apart: a refusal at the boundary leaves
    // none behind, and Directory.Exists is the line holding the boundary gate. The
    // message holds which forbidden set the refusal names, the cache folder and
    // the system folders having a sentence each.

    [Fact]
    public async Task MoveFilesAsync_refuses_a_destination_inside_the_installer_cache()
    {
        var fs = new MockFileSystem();
        var source = $@"{SourceDir}\a.msi";
        fs.AddFile(source, new MockFileData("payload"));

        var svc = new MoveFilesService(fs);
        var ex = await Assert.ThrowsAsync<LocalisedInvalidOperationException>(() =>
            svc.MoveFilesAsync(Helpers.StandingChecks.Of(new[] { source }), InsideInstallerCache));

        Assert.Equal(
            string.Format(InstallerClean.Resources.Strings.Error_MoveIntoInstaller, InsideInstallerCache),
            ex.Message);
        Assert.False(fs.Directory.Exists(InsideInstallerCache));
        Assert.True(fs.File.Exists(source));
    }

    [Fact]
    public async Task MoveFilesAsync_refuses_a_destination_inside_a_system_folder()
    {
        var fs = new MockFileSystem();
        var source = $@"{SourceDir}\a.msi";
        fs.AddFile(source, new MockFileData("payload"));

        var svc = new MoveFilesService(fs);
        var ex = await Assert.ThrowsAsync<LocalisedInvalidOperationException>(() =>
            svc.MoveFilesAsync(Helpers.StandingChecks.Of(new[] { source }), InsideSystemFolder));

        Assert.Equal(
            string.Format(InstallerClean.Resources.Strings.Error_DestinationInSystemFolder, InsideSystemFolder),
            ex.Message);
        Assert.False(fs.Directory.Exists(InsideSystemFolder));
        Assert.True(fs.File.Exists(source));
    }

    [Fact]
    public async Task MoveFilesAsync_refuses_a_destination_where_a_moved_file_would_reach_260_characters()
    {
        var fs = new MockFileSystem();
        var source = $@"{SourceDir}\a.msi";
        fs.AddFile(source, new MockFileData("payload"));
        // 254 characters, a separator and "a.msi" make 260.
        var destination = @"D:\" + new string('d', 254 - 3);

        var svc = new MoveFilesService(fs);
        var ex = await Assert.ThrowsAsync<LocalisedInvalidOperationException>(() =>
            svc.MoveFilesAsync(Helpers.StandingChecks.Of(new[] { source }), destination));

        Assert.Equal(
            string.Format(InstallerClean.Resources.Strings.Error_PathTooLong, destination),
            ex.Message);
        Assert.False(fs.Directory.Exists(destination));
        Assert.True(fs.File.Exists(source));
    }

    [Fact]
    public async Task MoveFilesAsync_refuses_a_destination_too_long_before_reading_where_it_leads()
    {
        // Inside the installer cache as well, so the refusal the message names is the
        // check that answered first.
        var fs = new MockFileSystem();
        var source = $@"{SourceDir}\a.msi";
        fs.AddFile(source, new MockFileData("payload"));
        var destination = PathText.Combine(InsideInstallerCache, new string('d', 260));

        var svc = new MoveFilesService(fs);
        var ex = await Assert.ThrowsAsync<LocalisedInvalidOperationException>(() =>
            svc.MoveFilesAsync(Helpers.StandingChecks.Of(new[] { source }), destination));

        Assert.Equal(
            string.Format(InstallerClean.Resources.Strings.Error_PathTooLong, destination),
            ex.Message);
        Assert.True(fs.File.Exists(source));
    }

    [Theory]
    [InlineData(WriteProbeRefusal.FolderOpen)]
    [InlineData(WriteProbeRefusal.CreateRefusedAccess)]
    [InlineData(WriteProbeRefusal.CreateFailed)]
    [InlineData(WriteProbeRefusal.ReadBackRefusedAccess)]
    [InlineData(WriteProbeRefusal.ReadBackFailed)]
    public async Task A_refused_write_probe_stops_the_move_with_the_message_for_its_refusal(WriteProbeRefusal refusal)
    {
        var fs = new MockFileSystem();
        var source = $@"{SourceDir}\a.msi";
        fs.AddFile(source, new MockFileData("payload"));
        fs.AddDirectory(DestDir);
        var guard = Substitute.For<IDestinationGuard>();
        guard.TryProbeWritable(Arg.Any<string>(), out Arg.Any<int>(), out Arg.Any<WriteProbeRefusal>())
            .Returns(call =>
            {
                call[1] = 5;
                call[2] = refusal;
                return false;
            });

        var svc = new MoveFilesService(fs, null, null, null, guard, null);
        var ex = await Assert.ThrowsAsync<LocalisedAccessException>(() =>
            svc.MoveFilesAsync(Helpers.StandingChecks.Of(new[] { source }), DestDir));

        // The service names a cause only for a read-back refused access; every other
        // refusal is a folder the Move cannot write to.
        var expected = refusal == WriteProbeRefusal.ReadBackRefusedAccess
            ? InstallerClean.Resources.Strings.Error_FolderWillNotGiveFilesBack
            : InstallerClean.Resources.Strings.Error_CannotWriteFolder;
        Assert.Equal(string.Format(expected, DestDir), ex.Message);
        Assert.Equal(5, Assert.IsType<System.ComponentModel.Win32Exception>(ex.InnerException).NativeErrorCode);
        Assert.True(fs.File.Exists(source));
        Assert.False(fs.File.Exists($@"{DestDir}\a.msi"));
    }

    /// <summary>A guard whose write test is refused and whose folder removal acts on <paramref name="fs"/>.</summary>
    private static IDestinationGuard GuardRefusingTheWriteTest(IFileSystem fs)
    {
        var removal = new ByPathDestinationGuard(fs);
        var guard = Substitute.For<IDestinationGuard>();
        guard.TryProbeWritable(Arg.Any<string>(), out Arg.Any<int>(), out Arg.Any<WriteProbeRefusal>())
            .Returns(call =>
            {
                call[1] = 5;
                call[2] = WriteProbeRefusal.CreateFailed;
                return false;
            });
        guard.TryCreateFolder(Arg.Any<string>())
            .Returns(call => removal.TryCreateFolder(call.ArgAt<string>(0)));
        guard.TryRemoveEmptyFolder(Arg.Any<string>(), Arg.Any<string>())
            .Returns(call => removal.TryRemoveEmptyFolder(call.ArgAt<string>(0), call.ArgAt<string>(1)));
        return guard;
    }

    [Fact]
    public async Task A_refused_write_probe_removes_the_destination_and_the_folders_above_it_that_the_move_made()
    {
        var fs = new MockFileSystem();
        var source = $@"{SourceDir}\a.msi";
        fs.AddFile(source, new MockFileData("payload"));
        var svc = new MoveFilesService(
            fs, null, null, null, GuardRefusingTheWriteTest(fs), null);

        await Assert.ThrowsAsync<LocalisedAccessException>(() =>
            svc.MoveFilesAsync(Helpers.StandingChecks.Of(new[] { source }), DestDir));

        Assert.False(fs.Directory.Exists(DestDir));
        Assert.False(fs.Directory.Exists(@"D:\backup"));
        Assert.True(fs.File.Exists(source));
    }

    [Fact]
    public async Task A_create_that_fails_part_way_removes_the_folders_it_made()
    {
        var directory = Substitute.For<IDirectory>();
        directory.When(d => d.CreateDirectory(DestDir)).Do(_ => throw new IOException("create refused"));
        var fs = Substitute.For<IFileSystem>();
        fs.Directory.Returns(directory);
        fs.Path.Returns(new MockFileSystem().Path);
        var svc = new MoveFilesService(fs);

        await Assert.ThrowsAsync<LocalisedAccessException>(() =>
            svc.MoveFilesAsync(Helpers.StandingChecks.Of(new[] { $@"{SourceDir}\a.msi" }), DestDir));

        directory.Received(1).Delete(@"D:\backup");
        directory.Received(1).Delete(Arg.Any<string>());
    }

    [Fact]
    public async Task A_write_test_that_throws_removes_the_folders_the_move_made_and_the_throw_goes_on()
    {
        var fs = new MockFileSystem();
        var source = $@"{SourceDir}\a.msi";
        fs.AddFile(source, new MockFileData("payload"));
        var removal = new ByPathDestinationGuard(fs);
        var guard = Substitute.For<IDestinationGuard>();
        guard.TryProbeWritable(Arg.Any<string>(), out Arg.Any<int>(), out Arg.Any<WriteProbeRefusal>())
            .Returns(_ => throw new IOException("the write test failed"));
        guard.TryCreateFolder(Arg.Any<string>())
            .Returns(call => removal.TryCreateFolder(call.ArgAt<string>(0)));
        guard.TryRemoveEmptyFolder(Arg.Any<string>(), Arg.Any<string>())
            .Returns(call => removal.TryRemoveEmptyFolder(call.ArgAt<string>(0), call.ArgAt<string>(1)));
        var svc = new MoveFilesService(
            fs, null, null, null, guard, null);

        await Assert.ThrowsAsync<IOException>(() =>
            svc.MoveFilesAsync(Helpers.StandingChecks.Of(new[] { source }), DestDir));

        Assert.False(fs.Directory.Exists(DestDir));
        Assert.False(fs.Directory.Exists(@"D:\backup"));
        Assert.True(fs.File.Exists(source));
    }

    [Fact]
    public async Task A_refused_write_probe_leaves_a_folder_above_the_destination_that_was_already_there()
    {
        var fs = new MockFileSystem();
        var source = $@"{SourceDir}\a.msi";
        fs.AddFile(source, new MockFileData("payload"));
        fs.AddDirectory(@"D:\backup");
        var svc = new MoveFilesService(
            fs, null, null, null, GuardRefusingTheWriteTest(fs), null);

        await Assert.ThrowsAsync<LocalisedAccessException>(() =>
            svc.MoveFilesAsync(Helpers.StandingChecks.Of(new[] { source }), DestDir));

        Assert.False(fs.Directory.Exists(DestDir));
        Assert.True(fs.Directory.Exists(@"D:\backup"));
    }

    [Fact]
    public async Task A_refused_write_probe_leaves_a_destination_that_was_already_there()
    {
        var fs = new MockFileSystem();
        var source = $@"{SourceDir}\a.msi";
        fs.AddFile(source, new MockFileData("payload"));
        fs.AddDirectory(DestDir);
        var svc = new MoveFilesService(
            fs, null, null, null, GuardRefusingTheWriteTest(fs), null);

        await Assert.ThrowsAsync<LocalisedAccessException>(() =>
            svc.MoveFilesAsync(Helpers.StandingChecks.Of(new[] { source }), DestDir));

        Assert.True(fs.Directory.Exists(DestDir));
    }

    // The destination guard's classification, at every combination of its four
    // inputs. The resolve behind those inputs asks the real kernel whatever
    // IFileSystem is injected, so the theory hands the four values to the
    // classification directly and pins its answer to each combination: which are a
    // destination that has changed, which are one whose resolution was lost, and
    // which are the state an ordinary batch runs in. The integration test next
    // door drives one of those conditions end to end; these hold that the
    // conditions are told apart at all.

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
