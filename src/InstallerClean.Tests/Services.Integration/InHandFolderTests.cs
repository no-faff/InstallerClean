using InstallerClean.Interop.Native;
using InstallerClean.Services;
using InstallerClean.Tests.Helpers;

namespace InstallerClean.Tests.Services.Integration;

/// <summary>
/// The app's own <see cref="HandleInHandFolder"/> and the copy half of
/// <see cref="HandleDestinationGuard"/> against real files, with a temp folder standing
/// in for the Installer folder and a second one for a Move's destination. What Windows
/// refuses while a file is held is the subject, which no substitute filesystem can show.
/// </summary>
public sealed class InHandFolderTests : IDisposable
{
    private readonly string _installer = TestTemp.NewFolder("ic-inhand-");
    private readonly string _destination = TestTemp.NewFolder("ic-inhand-dest-");
    private readonly HandleInHandFolder _hand = new();
    private readonly HandleDestinationGuard _guard = new();

    public void Dispose()
    {
        TestTemp.Delete(_installer);
        TestTemp.Delete(_destination);
    }

    private string Cached(string name, FileAttributes attributes = FileAttributes.Normal)
    {
        var path = Path.Combine(_installer, name);
        File.WriteAllBytes(path, [1, 2, 3, 4, 5]);
        File.SetAttributes(path, attributes);
        return path;
    }

    /// <summary>
    /// A rename moves a file's change time, which is why a check made straight after a
    /// put-back waits first (<see cref="InHandPause.AfterAPutBack"/>): the full check
    /// passes a file no record names only where its change time is before the check's
    /// clock.
    /// </summary>
    [Fact]
    public void A_file_taken_out_and_put_back_carries_a_later_change_time()
    {
        var path = Cached("put-back.msi");
        var times = new FileTimesReader();
        Assert.Equal(FileTimesRead.Read, times.ReadOutcome(path, out var before));
        Thread.Sleep(50);

        using (var batch = OpenBatch())
        {
            var held = Held(batch, path);
            Assert.True(held.TakeOut(out _));
            Assert.True(held.PutBack(out _));
            held.Dispose();
        }

        Assert.Equal(FileTimesRead.Read, times.ReadOutcome(path, out var after));
        Assert.True(after.ChangeUtc > before.ChangeUtc, $"change time {after.ChangeUtc:O} is not after {before.ChangeUtc:O}");
        Assert.Equal(before.LastWriteUtc, after.LastWriteUtc);
        Assert.Equal(before.CreationUtc, after.CreationUtc);
    }

    // ---- What an earlier batch left ----

    /// <summary>
    /// Leaves <paramref name="name"/> as a batch whose process died leaves the file it had
    /// out of its place: in the batch's folder, with no handle on it. Answers that folder.
    /// </summary>
    private string LeaveInHand(string name)
    {
        var path = Cached(name);
        using var batch = OpenBatch();
        var held = Held(batch, path);
        Assert.True(held.TakeOut(out _));
        held.Dispose();
        return batch.Folder;
    }

    private string InHand => Path.Combine(_installer, InHandFolders.FolderName);

    [Fact]
    public void An_Installer_folder_with_no_in_hand_folder_has_nothing_left()
    {
        Cached("a.msp");

        Assert.Null(_hand.OpenLeft(_installer));
    }

    [Fact]
    public void A_file_standing_under_the_in_hand_folders_name_is_not_opened_as_what_was_left()
    {
        File.WriteAllBytes(InHand, []);

        Assert.Null(_hand.OpenLeft(_installer));
        Assert.True(File.Exists(InHand));
    }

    [Fact]
    public void A_file_a_dead_batch_left_is_listed_held_alone_and_put_back_and_both_folders_go()
    {
        var folder = LeaveInHand("a.msp");
        var away = Path.Combine(folder, "a.msp");
        var home = Path.Combine(_installer, "a.msp");
        Assert.True(File.Exists(away));
        Assert.False(File.Exists(home));

        using (var left = _hand.OpenLeft(_installer))
        {
            Assert.NotNull(left);
            var batch = Assert.Single(left.Batches);
            Assert.Equal(Path.GetFileName(folder), batch.Name);
            var file = Assert.Single(batch.Files);
            Assert.Equal(("a.msp", 5L, false), (file.Name, file.Length, file.IsProbe));

            var hold = left.Hold(file);
            Assert.Equal(HoldOutcome.Held, hold.Outcome);
            using var held = hold.File!;
            Assert.True(held.IsOut);
            Assert.Equal(home, held.RecordedPath);
            Assert.Equal(Kernel32.ERROR_SHARING_VIOLATION,
                OpenError(away, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete));

            Assert.True(held.PutBack(out var code), $"the put-back was refused: {code}");
            Assert.False(held.IsOut);
        }

        Assert.Equal([1, 2, 3, 4, 5], File.ReadAllBytes(home));
        Assert.False(Directory.Exists(folder));
        Assert.False(Directory.Exists(InHand));
    }

    [Fact]
    public void A_read_only_file_a_dead_batch_left_goes_back_with_its_attribute()
    {
        var path = Cached("a.msp", FileAttributes.ReadOnly);
        string folder;
        using (var batch = OpenBatch())
        {
            folder = batch.Folder;
            var held = Held(batch, path);
            Assert.True(held.TakeOut(out _));
            held.Dispose();
        }

        using (var left = _hand.OpenLeft(_installer)!)
        {
            using var held = left.Hold(left.Batches[0].Files[0]).File!;
            Assert.True(held.PutBack(out var code), $"the put-back was refused: {code}");
        }

        Assert.True(File.GetAttributes(path).HasFlag(FileAttributes.ReadOnly));
        File.SetAttributes(path, FileAttributes.Normal);
        Assert.False(Directory.Exists(folder));
    }

    [Fact]
    public void A_file_a_running_batch_has_out_of_its_place_answers_in_use_and_its_folders_stay()
    {
        var path = Cached("a.msp");
        using var batch = OpenBatch();
        using var running = Held(batch, path);
        Assert.True(running.TakeOut(out _));

        using (var left = _hand.OpenLeft(_installer)!)
        {
            var file = Assert.Single(Assert.Single(left.Batches).Files);
            Assert.Equal(HoldOutcome.InUse, left.Hold(file).Outcome);
        }

        // The running batch has lost nothing: its file is where it took it, and goes back.
        Assert.True(File.Exists(Path.Combine(batch.Folder, "a.msp")));
        Assert.True(running.PutBack(out _));
        Assert.True(File.Exists(path));
    }

    [Fact]
    public void A_running_batchs_empty_folder_stays_and_one_nobody_has_open_goes()
    {
        using var batch = OpenBatch();
        var abandoned = Path.Combine(InHand, Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(abandoned);

        using (var left = _hand.OpenLeft(_installer)!)
            Assert.Equal(2, left.Batches.Count);

        Assert.False(Directory.Exists(abandoned));
        Assert.True(Directory.Exists(batch.Folder));
    }

    [Fact]
    public void A_file_left_whose_name_has_been_taken_is_not_put_back_and_stays_with_its_folders()
    {
        var folder = LeaveInHand("a.msp");
        var home = Path.Combine(_installer, "a.msp");
        File.WriteAllBytes(home, [9]);

        using (var left = _hand.OpenLeft(_installer)!)
        {
            using var held = left.Hold(left.Batches[0].Files[0]).File!;
            Assert.False(held.PutBack(out var code));
            Assert.Equal(Kernel32.ERROR_ALREADY_EXISTS, code);
            Assert.True(held.IsOut);
        }

        Assert.Equal([1, 2, 3, 4, 5], File.ReadAllBytes(Path.Combine(folder, "a.msp")));
        Assert.Equal([9], File.ReadAllBytes(home));
    }

    [Fact]
    public void The_empty_file_a_dead_Move_left_in_its_folder_is_told_apart_and_removed_through_its_hold()
    {
        var folder = Path.Combine(InHand, Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(folder);
        var probe = InHandFolders.NewProbeName();
        File.WriteAllBytes(Path.Combine(folder, probe), []);

        using (var left = _hand.OpenLeft(_installer)!)
        {
            var file = Assert.Single(Assert.Single(left.Batches).Files);
            Assert.True(file.IsProbe);
            var hold = left.Hold(file);
            Assert.Equal(HoldOutcome.Held, hold.Outcome);
            Assert.True(hold.File!.Delete(out var code), $"the delete was refused: {code}");
            hold.File.Dispose();
        }

        Assert.False(Directory.Exists(InHand));
        Assert.Empty(Directory.EnumerateFileSystemEntries(_installer));
    }

    [Fact]
    public void A_folder_or_a_file_that_has_gone_since_the_listing_answers_gone()
    {
        var folder = LeaveInHand("a.msp");

        using var left = _hand.OpenLeft(_installer)!;
        var file = left.Batches[0].Files[0];
        File.Delete(Path.Combine(folder, "a.msp"));
        Assert.Equal(HoldOutcome.Gone, left.Hold(file).Outcome);

        Directory.Delete(folder);
        Assert.Equal(HoldOutcome.Gone, left.Hold(file).Outcome);
    }

    [Fact]
    public void A_batchs_short_name_is_listed_where_the_volume_gives_it_one()
    {
        var folder = LeaveInHand("a.msp");

        using var left = _hand.OpenLeft(_installer)!;
        var batch = Assert.Single(left.Batches);

        // A volume that makes no short names gives none, and the name alone is then the
        // whole of what a source record could spell the folder as.
        var names = InstallerClean.Helpers.StorageHelpers.GetNamesInFolder(folder);
        Assert.NotNull(names);
        Assert.Equal(names.Count > 1 ? names[1] : null, batch.ShortName);
        Assert.NotEqual(batch.Name, batch.ShortName);

        using var held = left.Hold(batch.Files[0]).File!;
        Assert.True(held.PutBack(out _));
    }

    /// <summary>
    /// The whole recovery over real files: the file a dead batch left is back in its place
    /// with a later change time, which is why the recovery waits before the scan reads its
    /// clock (<see cref="InHandPause.AfterAPutBack"/>), and the Application log says so.
    /// </summary>
    [Fact]
    public async Task A_recovery_puts_a_dead_batchs_file_back_in_its_place_and_says_so()
    {
        var path = Cached("put-back.msi");
        var times = new FileTimesReader();
        Assert.Equal(FileTimesRead.Read, times.ReadOutcome(path, out var before));
        Thread.Sleep(50);
        using (var batch = OpenBatch())
        {
            var held = Held(batch, path);
            Assert.True(held.TakeOut(out _));
            held.Dispose();
        }
        Assert.False(File.Exists(path));
        EventLogRecorder.Clear();

        var result = await new InHandRecovery(_hand, new ScriptedSignals(), new ScriptedSourceRecords())
            .RecoverAsync(_installer, CancellationToken.None);

        Assert.Equal([path], result.PutBack);
        Assert.Equal([1, 2, 3, 4, 5], File.ReadAllBytes(path));
        Assert.False(Directory.Exists(InHand));
        Assert.Equal(FileTimesRead.Read, times.ReadOutcome(path, out var after));
        Assert.True(after.ChangeUtc > before.ChangeUtc, $"change time {after.ChangeUtc:O} is not after {before.ChangeUtc:O}");
        Assert.Equal(before.LastWriteUtc, after.LastWriteUtc);
        var entry = Assert.Single(EventLogRecorder.Entries);
        Assert.Equal(InstallerClean.Helpers.CliEventClass.ScanPutBackNotice, entry.Class);
        Assert.Contains("put-back.msi", entry.Text, StringComparison.Ordinal);
        EventLogRecorder.Clear();
    }

    [Fact]
    public async Task A_recovery_leaves_a_file_whose_batch_folder_a_source_record_names()
    {
        var folder = LeaveInHand("a.msi");

        var result = await new InHandRecovery(
                _hand, new ScriptedSignals(), new ScriptedSourceRecords().Naming(Path.GetFileName(folder)))
            .RecoverAsync(_installer, CancellationToken.None);

        Assert.Empty(result.PutBack);
        Assert.Equal(1, result.LeftAsSource);
        Assert.True(File.Exists(Path.Combine(folder, "a.msi")));
        Assert.False(File.Exists(Path.Combine(_installer, "a.msi")));
    }

    // ---- A scan in another process beside a batch that is starting ----

    [Fact]
    public void A_batch_makes_the_in_hand_folder_again_where_it_was_removed_before_the_batchs_own_was_made()
    {
        var removed = 0;
        var hand = new HandleInHandFolder();
        hand.BeforeTheBatchsFolderForTests = () =>
        {
            // What a scan in another process does where it finds the in-hand folder empty.
            if (removed++ == 0) Directory.Delete(InHand);
        };

        using var batch = hand.OpenBatch(_installer, out var code);

        Assert.True(batch is not null, $"the batch's folder could not be made: {code}");
        Assert.Equal(2, removed);
        Assert.True(Directory.Exists(batch.Folder));
    }

    [Fact]
    public void A_batch_gives_up_where_the_in_hand_folder_is_removed_under_it_every_time()
    {
        var tries = 0;
        var hand = new HandleInHandFolder();
        hand.BeforeTheBatchsFolderForTests = () =>
        {
            tries++;
            Directory.Delete(InHand);
        };

        var batch = hand.OpenBatch(_installer, out var code);

        Assert.Null(batch);
        Assert.NotEqual(0, code);
        Assert.Equal(HandleInHandFolder.OpenBatchTries, tries);
    }

    private IInHandBatch OpenBatch()
    {
        var batch = _hand.OpenBatch(_installer, out var code);
        Assert.True(batch is not null, $"the batch's folder could not be made: {code}");
        return batch;
    }

    private static IHeldFile Held(IInHandBatch batch, string path)
    {
        var hold = batch.Hold(path);
        Assert.Equal(HoldOutcome.Held, hold.Outcome);
        return hold.File!;
    }

    private static int OpenError(string path, FileAccess access, FileShare share)
    {
        try
        {
            using var _ = new FileStream(path, FileMode.Open, access, share);
            return 0;
        }
        catch (IOException ex)
        {
            return ex.HResult & 0xFFFF;
        }
    }

    [Fact]
    public void A_batch_makes_its_own_folder_inside_the_in_hand_folder_and_removes_both_when_it_ends()
    {
        var inHand = Path.Combine(_installer, InHandFolders.FolderName);
        string folder;
        using (var batch = OpenBatch())
        {
            folder = batch.Folder;
            Assert.True(Directory.Exists(folder));
            Assert.Equal(inHand, Path.GetDirectoryName(folder));
        }

        Assert.False(Directory.Exists(folder));
        Assert.False(Directory.Exists(inHand));
    }

    [Fact]
    public void Two_batches_have_folders_of_their_own_and_the_in_hand_folder_stays_while_one_is_open()
    {
        using var first = OpenBatch();
        string second;
        using (var other = OpenBatch())
        {
            second = other.Folder;
            Assert.NotEqual(first.Folder, second);
        }

        Assert.False(Directory.Exists(second));
        Assert.True(Directory.Exists(first.Folder));
    }

    [Fact]
    public void A_file_standing_under_the_in_hand_folders_name_refuses_the_batch()
    {
        File.WriteAllBytes(Path.Combine(_installer, InHandFolders.FolderName), []);

        var batch = _hand.OpenBatch(_installer, out var code);

        Assert.Null(batch);
        Assert.NotEqual(0, code);
    }

    [Fact]
    public void A_held_file_stays_in_its_place_and_nothing_else_can_open_it()
    {
        var path = Cached("a.msp");
        using var batch = OpenBatch();

        using (var held = Held(batch, path))
        {
            Assert.False(held.IsOut);
            Assert.True(File.Exists(path));
            Assert.Equal(Kernel32.ERROR_SHARING_VIOLATION,
                OpenError(path, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete));
        }

        Assert.Equal(0, OpenError(path, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete));
    }

    [Theory]
    [InlineData(FileAccess.Read, FileShare.ReadWrite | FileShare.Delete)]
    [InlineData(FileAccess.Read, FileShare.Read)]
    [InlineData(FileAccess.ReadWrite, FileShare.ReadWrite | FileShare.Delete)]
    public void A_file_anything_has_open_is_in_use_and_is_held_once_it_is_let_go(FileAccess access, FileShare share)
    {
        var path = Cached("a.msp");
        using var batch = OpenBatch();

        using (new FileStream(path, FileMode.Open, access, share))
        {
            var refused = batch.Hold(path);
            Assert.Equal(HoldOutcome.InUse, refused.Outcome);
            Assert.Null(refused.File);
        }

        using var held = Held(batch, path);
        Assert.True(File.Exists(path));
    }

    [Fact]
    public void A_path_with_nothing_at_it_is_gone_and_a_folder_is_not_a_plain_file()
    {
        using var batch = OpenBatch();
        Directory.CreateDirectory(Path.Combine(_installer, "folder.msp"));

        Assert.Equal(HoldOutcome.Gone, batch.Hold(Path.Combine(_installer, "absent.msp")).Outcome);
        Assert.Equal(HoldOutcome.NotAPlainFile, batch.Hold(Path.Combine(_installer, "folder.msp")).Outcome);
        Assert.Equal(HoldOutcome.NotAPlainFile, batch.OpenToCopy(Path.Combine(_installer, "folder.msp")).Outcome);
    }

    [Fact]
    public void Taking_a_file_out_renames_it_into_the_batchs_folder_and_putting_it_back_returns_it()
    {
        var path = Cached("a.msp");
        using var batch = OpenBatch();
        using var held = Held(batch, path);

        Assert.True(held.TakeOut(out _));
        Assert.True(held.IsOut);
        Assert.False(File.Exists(path));
        Assert.True(File.Exists(Path.Combine(batch.Folder, "a.msp")));

        Assert.True(held.PutBack(out _));
        Assert.False(held.IsOut);
        Assert.True(File.Exists(path));
        Assert.Empty(Directory.EnumerateFileSystemEntries(batch.Folder));
    }

    [Fact]
    public void A_file_in_hand_cannot_be_opened_or_renamed_by_anything_else()
    {
        var path = Cached("a.msp");
        using var batch = OpenBatch();
        using var held = Held(batch, path);
        Assert.True(held.TakeOut(out _));
        var away = Path.Combine(batch.Folder, "a.msp");

        Assert.Equal(Kernel32.ERROR_SHARING_VIOLATION,
            OpenError(away, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete));
        Assert.ThrowsAny<IOException>(() => File.Move(away, Path.Combine(_installer, "elsewhere.msp")));

        Assert.True(held.PutBack(out _));
    }

    [Fact]
    public void A_file_whose_name_has_been_taken_meanwhile_is_not_put_back_and_stays_in_the_batchs_folder()
    {
        var path = Cached("a.msp");
        string folder;
        using (var batch = OpenBatch())
        {
            folder = batch.Folder;
            using var held = Held(batch, path);
            Assert.True(held.TakeOut(out _));
            File.WriteAllBytes(path, [9]);

            Assert.False(held.PutBack(out var code));
            Assert.Equal(Kernel32.ERROR_ALREADY_EXISTS, code);
            Assert.True(held.IsOut);
        }

        // The batch's folder is left, with the file whole in it, and what took the name is untouched.
        Assert.Equal([1, 2, 3, 4, 5], File.ReadAllBytes(Path.Combine(folder, "a.msp")));
        Assert.Equal([9], File.ReadAllBytes(path));
    }

    [Theory]
    [InlineData(FileAttributes.Normal)]
    [InlineData(FileAttributes.ReadOnly)]
    [InlineData(FileAttributes.ReadOnly | FileAttributes.Hidden)]
    public void A_file_in_hand_is_deleted_as_its_hold_ends_read_only_or_not(FileAttributes attributes)
    {
        var path = Cached("a.msp", attributes);
        using var batch = OpenBatch();
        var held = Held(batch, path);
        Assert.True(held.TakeOut(out _));

        Assert.True(held.Delete(out var code), $"the delete was refused: {code}");
        held.Dispose();

        Assert.False(File.Exists(path));
        Assert.Empty(Directory.EnumerateFileSystemEntries(batch.Folder));
    }

    [Fact]
    public void A_file_in_hand_is_sent_on_into_the_destination_under_its_name()
    {
        var path = Cached("a.msp", FileAttributes.ReadOnly);
        using var batch = OpenBatch();
        using var destination = _guard.Open(_destination, out _);
        Assert.NotNull(destination);
        using (var held = Held(batch, path))
        {
            Assert.True(held.TakeOut(out _));
            Assert.True(held.MoveInto(destination, "a.msp", out var code), $"the rename was refused: {code}");
            Assert.False(held.IsOut);
        }

        var moved = Path.Combine(_destination, "a.msp");
        Assert.False(File.Exists(path));
        Assert.Equal([1, 2, 3, 4, 5], File.ReadAllBytes(moved));
        Assert.True(File.GetAttributes(moved).HasFlag(FileAttributes.ReadOnly));
        File.SetAttributes(moved, FileAttributes.Normal);
    }

    [Fact]
    public void A_name_already_in_the_destination_refuses_the_file_which_can_then_be_put_back()
    {
        var path = Cached("a.msp");
        File.WriteAllBytes(Path.Combine(_destination, "a.msp"), [9]);
        using var batch = OpenBatch();
        using var destination = _guard.Open(_destination, out _);
        using var held = Held(batch, path);
        Assert.True(held.TakeOut(out _));

        Assert.False(held.MoveInto(destination!, "a.msp", out var code));
        Assert.Equal(Kernel32.ERROR_ALREADY_EXISTS, code);
        Assert.True(held.IsOut);
        Assert.Equal([9], File.ReadAllBytes(Path.Combine(_destination, "a.msp")));

        Assert.True(held.PutBack(out _));
        Assert.True(File.Exists(path));
    }

    /// <summary>
    /// The file lands in the folder the destination's handle was opened on, where the
    /// path it was opened by has since come to name another folder.
    /// </summary>
    [Fact]
    public void A_file_sent_on_lands_in_the_folder_that_was_opened_whatever_its_path_names_since()
    {
        var path = Cached("a.msp");
        var chosen = Path.Combine(_destination, "chosen");
        var aside = Path.Combine(_destination, "aside");
        Directory.CreateDirectory(chosen);
        using var batch = OpenBatch();
        using (var destination = _guard.Open(chosen, out _))
        {
            Assert.NotNull(destination);
            Directory.Move(chosen, aside);
            Directory.CreateDirectory(chosen);
            using var held = Held(batch, path);
            Assert.True(held.TakeOut(out _));

            Assert.True(held.MoveInto(destination, "a.msp", out var code), $"the rename was refused: {code}");
        }

        Assert.True(File.Exists(Path.Combine(aside, "a.msp")));
        Assert.Empty(Directory.EnumerateFileSystemEntries(chosen));
    }

    [Fact]
    public void A_destination_on_the_same_volume_is_reached_by_a_rename_and_the_question_leaves_nothing_behind()
    {
        using var batch = OpenBatch();
        using var destination = _guard.Open(_destination, out _);

        var reach = batch.ReachOf(destination!, out var code);

        Assert.True(reach == RenameReach.SameVolume, $"{reach}, {code}");
        Assert.Empty(Directory.EnumerateFileSystemEntries(_destination));
        Assert.Empty(Directory.EnumerateFileSystemEntries(batch.Folder));
    }

    [Fact]
    public void A_file_open_for_a_copy_can_be_read_by_others_and_not_written_and_gives_the_stamp_a_hold_gives()
    {
        var path = Cached("a.msp");
        using var batch = OpenBatch();
        FileStamp copied;
        using (var open = batch.OpenToCopy(path).Source!)
        {
            copied = open.Stamp;
            var bytes = new byte[8];
            Assert.Equal(5, open.Content.Read(bytes, 0, bytes.Length));
            Assert.Equal(0, OpenError(path, FileAccess.Read, FileShare.Read));
            Assert.Equal(Kernel32.ERROR_SHARING_VIOLATION,
                OpenError(path, FileAccess.ReadWrite, FileShare.ReadWrite | FileShare.Delete));
        }

        using var held = Held(batch, path);
        Assert.True(held.TryReadStamp(out var taken));
        Assert.Equal(copied, taken);
        Assert.Equal(5, taken.Length);
    }

    [Fact]
    public void A_file_written_again_after_it_was_copied_gives_a_different_stamp()
    {
        var path = Cached("a.msp");
        using var batch = OpenBatch();
        FileStamp copied;
        using (var open = batch.OpenToCopy(path).Source!) copied = open.Stamp;

        File.WriteAllBytes(path, [1, 2, 3, 4, 5, 6]);

        using var held = Held(batch, path);
        Assert.True(held.TryReadStamp(out var taken));
        Assert.NotEqual(copied, taken);
    }

    [Fact]
    public void A_finished_copy_is_whole_under_its_real_name_with_the_attributes_and_time_it_was_given()
    {
        var written = new DateTime(2020, 3, 4, 5, 6, 7, DateTimeKind.Utc);
        using var destination = _guard.Open(_destination, out _);
        using (var copy = _guard.CreateCopy(destination!, out var created))
        {
            Assert.True(copy is not null, $"the copy could not be created: {created}");
            copy.Content.Write([1, 2, 3], 0, 3);
            Assert.Single(Directory.EnumerateFiles(_destination, "*.tmp"));

            var finish = copy.Finish("a.msp", 3, FileAttributes.ReadOnly | FileAttributes.Archive, written.ToFileTimeUtc(), out var code);

            Assert.True(finish == CopyFinish.Finished, $"{finish}, {code}");
        }

        var path = Path.Combine(_destination, "a.msp");
        Assert.Equal([path], Directory.EnumerateFileSystemEntries(_destination));
        Assert.Equal([1, 2, 3], File.ReadAllBytes(path));
        Assert.Equal(written, File.GetLastWriteTimeUtc(path));
        Assert.Equal(FileAttributes.ReadOnly | FileAttributes.Archive, File.GetAttributes(path));
        File.SetAttributes(path, FileAttributes.Normal);
    }

    [Fact]
    public void A_copy_that_is_not_finished_is_removed_when_it_is_disposed_of()
    {
        using var destination = _guard.Open(_destination, out _);
        using (var copy = _guard.CreateCopy(destination!, out _))
        {
            Assert.NotNull(copy);
            copy.Content.Write([1, 2, 3], 0, 3);
        }

        Assert.Empty(Directory.EnumerateFileSystemEntries(_destination));
    }

    [Fact]
    public void A_copy_of_the_wrong_length_or_onto_a_taken_name_is_not_finished_and_leaves_nothing_of_its_own()
    {
        File.WriteAllBytes(Path.Combine(_destination, "taken.msp"), [9]);
        using var destination = _guard.Open(_destination, out _);
        using (var copy = _guard.CreateCopy(destination!, out _))
        {
            Assert.NotNull(copy);
            copy.Content.Write([1, 2, 3], 0, 3);

            Assert.Equal(CopyFinish.LengthDiffers, copy.Finish("a.msp", 4, FileAttributes.Normal, DateTime.UtcNow.ToFileTimeUtc(), out _));
            Assert.Equal(CopyFinish.NameTaken, copy.Finish("taken.msp", 3, FileAttributes.ReadOnly, DateTime.UtcNow.ToFileTimeUtc(), out _));
        }

        Assert.Equal([Path.Combine(_destination, "taken.msp")], Directory.EnumerateFileSystemEntries(_destination));
        Assert.Equal([9], File.ReadAllBytes(Path.Combine(_destination, "taken.msp")));
    }
}
