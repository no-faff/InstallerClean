using System.IO.Abstractions;
using System.IO.Abstractions.TestingHelpers;
using InstallerClean.Interop.Native;
using InstallerClean.Models;
using InstallerClean.Services;
using InstallerClean.Tests.Helpers;

namespace InstallerClean.Tests.Services;

/// <summary>
/// The steps <see cref="MoveFilesService"/> makes on each file, on both of its ways: by a
/// rename, where a rename reaches the destination, and by a copy made first, where it
/// does not. What follows a look that is not clean, a step Windows refuses, a file that
/// changes under a copy, and a copy whose original then stays.
///
/// The filesystem is a substitute and the hand acts on it by path
/// (<see cref="ScriptedInHandFolder"/>), which answers that a rename reaches a
/// destination on the Installer folder's drive letter and not one on another.
/// </summary>
public class MoveFilesServiceStepsTests
{
    private const string Dir = @"C:\Windows\Installer";
    private const string SameDrive = @"C:\ic-test-backup";
    private const string OtherDrive = @"D:\ic-test-backup";
    private static readonly string A = $@"{Dir}\a.msi";
    private static readonly string B = $@"{Dir}\b.msi";
    private static readonly string C = $@"{Dir}\c.msi";
    private static readonly string InHand = $@"{Dir}\{InHandFolders.FolderName}";

    private readonly MockFileSystem _fs = new();
    private readonly ScriptedInHandFolder _hand;
    private readonly ScriptedSignals _signals = new();
    private readonly ScriptedBatchCheck _checks = new();
    private readonly FakeRecordsWatch _watch = new();

    public MoveFilesServiceStepsTests()
    {
        _fs.AddFile(A, new MockFileData("payload a"));
        _fs.AddFile(B, new MockFileData("payload b"));
        _fs.AddFile(C, new MockFileData("payload c"));
        _hand = new ScriptedInHandFolder(_fs);
    }

    private Task<MoveResult> Move(
        string destination, IReadOnlyList<string>? files = null, CancellationToken token = default,
        Func<string, bool>? mayAct = null, IDestinationGuard? guard = null) =>
        new MoveFilesService(_fs, _signals, _checks, _hand, guard, installerFolderOverride: null).MoveFilesAsync(
            StandingChecks.Of(files ?? [A, B, C], _watch), destination, cancellationToken: token, mayAct: mayAct);

    /// <summary>Makes the batch's first watch read as changed at one read of it, counted from one.</summary>
    private void RecordsChangeAtRead(int read) =>
        _watch.OnRead = (watch, n) =>
        {
            if (n == read) watch.Reading = RecordsWatchReading.Changed;
        };

    private string At(string destination, string name) => _fs.Path.Combine(destination, name);

    // By a rename.

    [Fact]
    public async Task Where_a_rename_reaches_the_destination_each_file_is_held_taken_and_renamed_on()
    {
        var result = await Move(SameDrive);

        Assert.Equal(3, result.MovedCount);
        Assert.Empty(result.Errors);
        Assert.Equal(["hold a.msi", "take a.msi", "move-into a.msi"], _hand.Steps.Take(3));
        Assert.DoesNotContain(_hand.Steps, step => step.StartsWith("copy-open", StringComparison.Ordinal));
        Assert.Equal("payload a", _fs.File.ReadAllText(At(SameDrive, "a.msi")));
        Assert.False(_fs.File.Exists(A));
        Assert.Equal(6, _watch.Reads);
        Assert.Equal(6, _signals.BusyReads);
        Assert.False(_fs.Directory.Exists(InHand));
        Assert.True(_watch.Disposed);
    }

    [Fact]
    public async Task By_rename_a_second_look_that_is_not_clean_puts_the_file_back_checks_again_and_then_moves_it()
    {
        RecordsChangeAtRead(2);

        var result = await Move(SameDrive);

        Assert.Equal(3, result.MovedCount);
        Assert.Equal(
            ["hold a.msi", "take a.msi", "put-back a.msi", "hold a.msi", "take a.msi", "move-into a.msi"],
            _hand.Steps.Take(6));
        Assert.Equal([A, B, C], Assert.Single(_checks.Calls));
    }

    [Theory]
    [InlineData(Kernel32.ERROR_NOT_SAME_DEVICE, typeof(IOFailure))]
    [InlineData(Kernel32.ERROR_ACCESS_DENIED, typeof(AccessDenied))]
    [InlineData(Kernel32.ERROR_ALREADY_EXISTS, typeof(DestinationCollision))]
    [InlineData(1117, typeof(IOFailure))]
    public async Task A_refused_rename_into_the_destination_puts_the_file_back_and_the_batch_goes_on(
        int windowsError, Type expected)
    {
        _hand.Refuse("move-into", A, windowsError);

        var result = await Move(SameDrive);

        var error = Assert.Single(result.Errors);
        Assert.IsType(expected, error);
        Assert.Equal(A, error.FilePath);
        Assert.Equal(["hold a.msi", "take a.msi", "move-into a.msi", "put-back a.msi"], _hand.Steps.Take(4));
        Assert.True(_fs.File.Exists(A));
        Assert.False(_fs.File.Exists(At(SameDrive, "a.msi")));
        Assert.Equal(2, result.MovedCount);
        Assert.Null(result.Ended);
        Assert.False(_fs.Directory.Exists(InHand));
    }

    [Fact]
    public async Task A_file_that_will_not_go_back_after_a_refused_rename_on_is_filed_with_the_folder_it_is_in()
    {
        _hand.Refuse("move-into", A, Kernel32.ERROR_ACCESS_DENIED).Refuse("put-back", A, Kernel32.ERROR_ACCESS_DENIED);

        var result = await Move(SameDrive);

        var error = Assert.IsType<FileLeftInHand>(Assert.Single(result.Errors));
        Assert.Equal(_hand.BatchFolder, error.InHandFolder);
        Assert.True(_fs.File.Exists(At(_hand.BatchFolder!, "a.msi")));
        Assert.Equal(2, result.MovedCount);
    }

    [Fact]
    public async Task A_batch_that_ends_in_a_later_check_reports_what_it_moved()
    {
        RecordsChangeAtRead(3);
        _checks.ThenEnds(BatchEndReason.WaitBoundReached, PendingRebootResult.Block(PendingRebootReason.MsiExecuteMutexHeld));

        var result = await Move(SameDrive);

        Assert.Equal(BatchEndReason.WaitBoundReached, result.Ended!.Reason);
        Assert.Equal(1, result.MovedCount);
        Assert.True(_fs.File.Exists(At(SameDrive, "a.msi")));
        Assert.True(_fs.File.Exists(B));
        Assert.True(_fs.File.Exists(C));
    }

    [Fact]
    public async Task A_batch_that_moves_nothing_removes_the_folders_it_made()
    {
        var destination = $@"{SameDrive}\made\by-the-move";
        RecordsChangeAtRead(1);
        _checks.ThenStands(A, B, C);

        var result = await Move(destination);

        Assert.Equal(0, result.MovedCount);
        Assert.Equal([A, B, C], result.HeldBack);
        Assert.False(_fs.Directory.Exists(destination));
        Assert.False(_fs.Directory.Exists(SameDrive));
    }

    [Fact]
    public async Task A_batch_that_moves_a_file_keeps_the_folders_it_made()
    {
        var destination = $@"{SameDrive}\made";

        await Move(destination);

        Assert.True(_fs.File.Exists(At(destination, "a.msi")));
    }

    [Fact]
    public async Task A_batch_whose_folder_cannot_be_made_ends_with_nothing_touched_and_no_destination_left()
    {
        _hand.OpenBatchError = Kernel32.ERROR_DIRECTORY;

        var result = await Move(SameDrive);

        Assert.Equal(BatchEndReason.InHandFolderUnavailable, result.Ended!.Reason);
        Assert.Equal(Kernel32.ERROR_DIRECTORY, result.Ended.WindowsError);
        Assert.Equal(0, result.MovedCount);
        Assert.True(_fs.File.Exists(A));
        Assert.False(_fs.Directory.Exists(SameDrive));
    }

    [Fact]
    public async Task A_destination_that_will_not_open_for_the_batch_is_refused_before_any_file_and_leaves_no_folder()
    {
        var guard = new GuardThatWillNotOpen(new ByPathDestinationGuard(_fs));

        var thrown = await Assert.ThrowsAsync<LocalisedAccessException>(() => Move(SameDrive, guard: guard));

        Assert.IsType<System.ComponentModel.Win32Exception>(thrown.InnerException);
        Assert.Empty(_hand.Steps);
        Assert.True(_fs.File.Exists(A));
        Assert.False(_fs.Directory.Exists(SameDrive));
        Assert.False(_fs.Directory.Exists(InHand));
    }

    // By a copy.

    [Fact]
    public async Task Where_a_rename_does_not_reach_the_destination_the_copy_is_made_before_the_file_is_taken()
    {
        var result = await Move(OtherDrive);

        Assert.Equal(3, result.MovedCount);
        Assert.Empty(result.Errors);
        Assert.Equal(
            ["copy-open a.msi", "hold a.msi", "stamp a.msi", "take a.msi", "delete a.msi"], _hand.Steps.Take(5));
        Assert.DoesNotContain(_hand.Steps, step => step.StartsWith("move-into", StringComparison.Ordinal));
        Assert.Equal("payload a", _fs.File.ReadAllText(At(OtherDrive, "a.msi")));
        Assert.False(_fs.File.Exists(A));
        Assert.Equal(["a.msi", "b.msi", "c.msi"], _fs.Directory.GetFiles(OtherDrive).Select(_fs.Path.GetFileName).OrderBy(n => n));
        Assert.False(_fs.Directory.Exists(InHand));
    }

    [Fact]
    public async Task By_copy_there_is_a_look_before_the_copy_after_it_and_with_the_file_out_of_its_place()
    {
        await Move(OtherDrive, [A]);

        Assert.Equal(3, _watch.Reads);
        Assert.Equal(3, _signals.BusyReads);
    }

    [Fact]
    public async Task A_destination_whose_reach_was_not_established_is_copied_to()
    {
        _hand.Reach = RenameReach.Unestablished;

        var result = await Move(SameDrive, [A]);

        Assert.Equal(1, result.MovedCount);
        Assert.Contains("copy-open a.msi", _hand.Steps);
        Assert.Equal("payload a", _fs.File.ReadAllText(At(SameDrive, "a.msi")));
    }

    [Fact]
    public async Task By_copy_a_look_before_the_copy_that_is_not_clean_checks_again_before_anything_is_copied()
    {
        RecordsChangeAtRead(1);

        var result = await Move(OtherDrive);

        Assert.Equal(3, result.MovedCount);
        Assert.Single(_checks.Calls);
        Assert.Equal(1, _hand.Steps.Count(step => step == "copy-open a.msi"));
    }

    [Fact]
    public async Task By_copy_a_check_made_after_the_copy_keeps_the_copy_and_does_not_make_it_twice()
    {
        // The second read of the watch is the look after the copy.
        RecordsChangeAtRead(2);

        var result = await Move(OtherDrive);

        Assert.Equal(3, result.MovedCount);
        Assert.Equal(1, _hand.Steps.Count(step => step == "copy-open a.msi"));
        Assert.Equal("payload a", _fs.File.ReadAllText(At(OtherDrive, "a.msi")));
        Assert.False(_fs.File.Exists(A));
    }

    [Fact]
    public async Task By_copy_a_look_with_the_file_out_that_is_not_clean_puts_it_back_and_finishes_without_copying_again()
    {
        // The third read of the watch is the look with the original out of its place.
        RecordsChangeAtRead(3);

        var result = await Move(OtherDrive, [A]);

        Assert.Equal(1, result.MovedCount);
        Assert.Equal(
            ["copy-open a.msi", "hold a.msi", "stamp a.msi", "take a.msi", "put-back a.msi",
             "hold a.msi", "stamp a.msi", "take a.msi", "delete a.msi"],
            _hand.Steps);
        Assert.False(_fs.File.Exists(A));
        Assert.True(_fs.File.Exists(At(OtherDrive, "a.msi")));
    }

    [Fact]
    public async Task A_copy_whose_file_a_later_check_drops_is_removed_and_the_file_stays()
    {
        RecordsChangeAtRead(2);
        _checks.ThenStands(A);

        var result = await Move(OtherDrive);

        Assert.Equal(2, result.MovedCount);
        Assert.Equal([A], result.HeldBack);
        Assert.Empty(result.Errors);
        Assert.True(_fs.File.Exists(A));
        Assert.False(_fs.File.Exists(At(OtherDrive, "a.msi")));
    }

    [Fact]
    public async Task A_copy_made_before_the_batch_ended_is_removed_and_the_file_stays()
    {
        RecordsChangeAtRead(2);
        _checks.ThenEnds(BatchEndReason.RecordsKeptChanging);

        var result = await Move(OtherDrive);

        Assert.Equal(BatchEndReason.RecordsKeptChanging, result.Ended!.Reason);
        Assert.Equal(0, result.MovedCount);
        Assert.True(_fs.File.Exists(A));
        Assert.False(_fs.Directory.Exists(OtherDrive));
    }

    [Fact]
    public async Task A_copy_the_host_then_stops_the_batch_at_is_removed_and_the_file_stays()
    {
        var asks = 0;

        // The first ask comes before the copy; the second is the one before the take.
        var result = await Move(OtherDrive, [A], mayAct: _ => ++asks < 2);

        Assert.True(result.Cancelled);
        Assert.Equal(0, result.MovedCount);
        Assert.Contains("copy-open a.msi", _hand.Steps);
        Assert.DoesNotContain("hold a.msi", _hand.Steps);
        Assert.True(_fs.File.Exists(A));
        Assert.False(_fs.Directory.Exists(OtherDrive));
    }

    [Fact]
    public async Task A_file_that_changed_between_the_copy_and_the_take_is_copied_again()
    {
        var changed = false;
        _hand.OnStep = (step, path) =>
        {
            if (step != "hold" || changed) return;
            changed = true;
            _fs.File.WriteAllText(path, "a longer payload, written after the copy");
        };

        var result = await Move(OtherDrive, [A]);

        Assert.Equal(1, result.MovedCount);
        Assert.Equal(2, _hand.Steps.Count(step => step == "copy-open a.msi"));
        Assert.DoesNotContain("take a.msi", _hand.Steps.Take(3));
        Assert.Equal("a longer payload, written after the copy", _fs.File.ReadAllText(At(OtherDrive, "a.msi")));
        Assert.False(_fs.File.Exists(A));
    }

    [Fact]
    public async Task A_file_that_changes_after_every_copy_is_filed_after_three_and_stays_with_no_copy_left()
    {
        var writes = 0;
        _hand.OnStep = (step, path) =>
        {
            if (step == "hold") _fs.File.WriteAllText(path, new string('x', 20 + ++writes));
        };

        var result = await Move(OtherDrive, [A]);

        Assert.IsType<IOFailure>(Assert.Single(result.Errors));
        Assert.Equal(0, result.MovedCount);
        Assert.Equal(3, _hand.Steps.Count(step => step == "copy-open a.msi"));
        Assert.DoesNotContain("take a.msi", _hand.Steps);
        Assert.True(_fs.File.Exists(A));
        Assert.False(_fs.Directory.Exists(OtherDrive));
    }

    [Fact]
    public async Task By_copy_a_refused_delete_of_the_original_puts_it_back_and_removes_the_copy()
    {
        _hand.Refuse("delete", A, Kernel32.ERROR_ACCESS_DENIED);

        var result = await Move(OtherDrive);

        var error = Assert.IsType<AccessDenied>(Assert.Single(result.Errors));
        Assert.Equal(A, error.FilePath);
        Assert.True(_fs.File.Exists(A));
        Assert.False(_fs.File.Exists(At(OtherDrive, "a.msi")));
        Assert.Equal(2, result.MovedCount);
        Assert.Contains("put-back a.msi", _hand.Steps);
    }

    [Fact]
    public async Task By_copy_a_file_in_use_when_it_is_opened_to_copy_is_tried_once_more_after_the_last_file()
    {
        _hand.InUse(A, times: 1);

        var result = await Move(OtherDrive);

        Assert.Equal(3, result.MovedCount);
        Assert.Empty(result.Errors);
        Assert.Equal(2, _hand.Steps.Count(step => step == "copy-open a.msi"));
        Assert.Equal("delete a.msi", _hand.Steps[^1]);
    }

    [Fact]
    public async Task By_copy_a_file_in_use_at_the_take_has_its_copy_removed_and_is_tried_once_more()
    {
        _hand.InUseAtTheHold(A, times: 2);

        var result = await Move(OtherDrive, [A]);

        Assert.IsType<FileInUse>(Assert.Single(result.Errors));
        Assert.Equal(["copy-open a.msi", "hold a.msi", "copy-open a.msi", "hold a.msi"], _hand.Steps);
        Assert.True(_fs.File.Exists(A));
        Assert.False(_fs.Directory.Exists(OtherDrive));
    }

    /// <summary>A destination guard that does everything the by-path one does and refuses the open for the batch.</summary>
    private sealed class GuardThatWillNotOpen(IDestinationGuard inner) : IDestinationGuard
    {
        public FolderCreation TryCreateFolder(string folder) => inner.TryCreateFolder(folder);

        public bool TryProbeWritable(string folder, out int failureCode, out WriteProbeRefusal refusal) =>
            inner.TryProbeWritable(folder, out failureCode, out refusal);

        public bool TryDiscardCopy(DestinationFolder destination, string name, out int failureCode) =>
            inner.TryDiscardCopy(destination, name, out failureCode);

        public bool TryRemoveEmptyFolder(string folder, string existingAncestor) =>
            inner.TryRemoveEmptyFolder(folder, existingAncestor);

        public DestinationFolder? Open(string folder, out int failureCode)
        {
            failureCode = Kernel32.ERROR_ACCESS_DENIED;
            return null;
        }

        public ICopyTarget? CreateCopy(DestinationFolder destination, out int failureCode) =>
            inner.CreateCopy(destination, out failureCode);
    }
}
