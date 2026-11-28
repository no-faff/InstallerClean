using System.IO.Abstractions.TestingHelpers;
using InstallerClean.Interop.Native;
using InstallerClean.Services;

namespace InstallerClean.Tests.Services;

/// <summary>
/// The test seam's <see cref="ByPathInHandFolder"/> and the copy half of
/// <see cref="ByPathDestinationGuard"/> on a substitute filesystem: each step leaves the
/// file where the app's own leaves it, so a test of a Move or a Delete built on them
/// follows the same file through the same places.
/// </summary>
public class ByPathInHandFolderTests
{
    private const string Installer = @"C:\Windows\Installer";
    private const string Destination = @"D:\Backup";

    private readonly MockFileSystem _fs = new();
    private readonly ByPathInHandFolder _hand;
    private readonly ByPathDestinationGuard _guard;

    public ByPathInHandFolderTests()
    {
        _fs.AddDirectory(Installer);
        _fs.AddDirectory(Destination);
        _hand = new ByPathInHandFolder(_fs);
        _guard = new ByPathDestinationGuard(_fs);
    }

    private string Cached(string name)
    {
        var path = Installer + @"\" + name;
        _fs.AddFile(path, new MockFileData([1, 2, 3, 4, 5]));
        return path;
    }

    private static IHeldFile Held(IInHandBatch batch, string path)
    {
        var hold = batch.Hold(path);
        Assert.Equal(HoldOutcome.Held, hold.Outcome);
        return hold.File!;
    }

    [Fact]
    public void A_batchs_folder_is_inside_the_in_hand_folder_and_both_go_when_the_batch_ends_empty()
    {
        string folder;
        using (var batch = _hand.OpenBatch(Installer, out _)!)
        {
            folder = batch.Folder;
            Assert.StartsWith(Installer + @"\" + InHandFolders.FolderName + @"\", folder);
            Assert.True(_fs.Directory.Exists(folder));
        }

        Assert.False(_fs.Directory.Exists(folder));
        Assert.False(_fs.Directory.Exists(Installer + @"\" + InHandFolders.FolderName));
    }

    [Fact]
    public void A_file_goes_out_into_the_batchs_folder_and_back_to_its_place()
    {
        var path = Cached("a.msp");
        using var batch = _hand.OpenBatch(Installer, out _)!;
        using var held = Held(batch, path);

        Assert.True(held.TakeOut(out _));
        Assert.True(held.IsOut);
        Assert.False(_fs.File.Exists(path));
        Assert.True(_fs.File.Exists(batch.Folder + @"\a.msp"));

        Assert.True(held.PutBack(out _));
        Assert.False(held.IsOut);
        Assert.True(_fs.File.Exists(path));
    }

    [Fact]
    public void A_file_whose_name_was_taken_meanwhile_stays_in_the_batchs_folder_which_is_then_left()
    {
        var path = Cached("a.msp");
        string folder;
        using (var batch = _hand.OpenBatch(Installer, out _)!)
        {
            folder = batch.Folder;
            using var held = Held(batch, path);
            Assert.True(held.TakeOut(out _));
            _fs.AddFile(path, new MockFileData([9]));

            Assert.False(held.PutBack(out var code));
            Assert.Equal(Kernel32.ERROR_ALREADY_EXISTS, code);
        }

        Assert.True(_fs.File.Exists(folder + @"\a.msp"));
    }

    [Fact]
    public void A_read_only_file_in_hand_is_deleted()
    {
        var path = Cached("a.msp");
        _fs.File.SetAttributes(path, FileAttributes.ReadOnly);
        using var batch = _hand.OpenBatch(Installer, out _)!;
        using var held = Held(batch, path);
        Assert.True(held.TakeOut(out _));

        Assert.True(held.Delete(out _));

        Assert.False(_fs.File.Exists(path));
        Assert.Empty(_fs.Directory.GetFiles(batch.Folder));
    }

    [Fact]
    public void A_file_in_hand_is_sent_on_and_a_taken_name_refuses_it()
    {
        var first = Cached("a.msp");
        var second = Cached("b.msp");
        _fs.AddFile(Destination + @"\b.msp", new MockFileData([9]));
        using var batch = _hand.OpenBatch(Installer, out _)!;
        using var destination = _guard.Open(Destination, out _)!;

        using (var held = Held(batch, first))
        {
            Assert.True(held.TakeOut(out _));
            Assert.True(held.MoveInto(destination, "a.msp", out _));
        }
        using (var held = Held(batch, second))
        {
            Assert.True(held.TakeOut(out _));
            Assert.False(held.MoveInto(destination, "b.msp", out var code));
            Assert.Equal(Kernel32.ERROR_ALREADY_EXISTS, code);
            Assert.True(held.PutBack(out _));
        }

        Assert.True(_fs.File.Exists(Destination + @"\a.msp"));
        Assert.True(_fs.File.Exists(second));
    }

    [Fact]
    public void A_missing_path_is_gone_and_another_drive_is_not_reached_by_a_rename()
    {
        using var batch = _hand.OpenBatch(Installer, out _)!;
        using var other = _guard.Open(Destination, out _)!;
        _fs.AddDirectory(@"C:\Backup");
        using var same = _guard.Open(@"C:\Backup", out _)!;

        Assert.Equal(HoldOutcome.Gone, batch.Hold(Installer + @"\absent.msp").Outcome);
        Assert.Equal(RenameReach.AnotherVolume, batch.ReachOf(other, out _));
        Assert.Equal(RenameReach.SameVolume, batch.ReachOf(same, out _));
        Assert.Null(_guard.Open(@"D:\Missing", out var code));
        Assert.NotEqual(0, code);
    }

    [Fact]
    public void A_copy_is_finished_under_its_real_name_or_removed()
    {
        var path = Cached("a.msp");
        using var batch = _hand.OpenBatch(Installer, out _)!;
        using var destination = _guard.Open(Destination, out _)!;

        using (var source = batch.OpenToCopy(path).Source!)
        using (var copy = _guard.CreateCopy(destination, out _)!)
        {
            source.Content.CopyTo(copy.Content);
            Assert.Equal(CopyFinish.Finished,
                copy.Finish("a.msp", source.Stamp.Length, source.Attributes, source.Stamp.LastWriteTime, out _));
        }
        using (var copy = _guard.CreateCopy(destination, out _)!)
        {
            copy.Content.WriteByte(1);
            Assert.Equal(CopyFinish.LengthDiffers, copy.Finish("b.msp", 2, FileAttributes.Normal, 0, out _));
        }

        Assert.Equal([Destination + @"\a.msp"], _fs.Directory.GetFiles(Destination));
        Assert.Equal([1, 2, 3, 4, 5], _fs.File.ReadAllBytes(Destination + @"\a.msp"));
        Assert.True(_fs.File.Exists(path));
    }

    [Fact]
    public void With_no_in_hand_folder_nothing_was_left()
    {
        Cached("a.msp");

        Assert.Null(_hand.OpenLeft(Installer));
    }

    [Fact]
    public void A_file_left_in_a_batchs_folder_is_listed_and_goes_back_to_the_Installer_folder_and_the_folders_go()
    {
        var path = Cached("a.msp");
        string folder;
        using (var batch = _hand.OpenBatch(Installer, out _)!)
        {
            folder = batch.Folder;
            var gone = Held(batch, path);
            Assert.True(gone.TakeOut(out _));
            gone.Dispose();
        }
        Assert.True(_fs.File.Exists(folder + @"\a.msp"));

        using (var left = _hand.OpenLeft(Installer)!)
        {
            var batch = Assert.Single(left.Batches);
            Assert.Equal(folder, Installer + @"\" + InHandFolders.FolderName + @"\" + batch.Name);
            var file = Assert.Single(batch.Files);
            Assert.Equal(("a.msp", 5L), (file.Name, file.Length));

            using var held = Assert.IsAssignableFrom<IHeldFile>(left.Hold(file).File);
            Assert.True(held.IsOut);
            Assert.Equal(path, held.RecordedPath);
            Assert.True(held.PutBack(out _));
            Assert.False(held.IsOut);
        }

        Assert.Equal([1, 2, 3, 4, 5], _fs.File.ReadAllBytes(path));
        Assert.False(_fs.Directory.Exists(Installer + @"\" + InHandFolders.FolderName));
    }

    [Fact]
    public void A_file_left_whose_name_is_taken_stays_and_so_do_its_folders()
    {
        var path = Cached("a.msp");
        string folder;
        using (var batch = _hand.OpenBatch(Installer, out _)!)
        {
            folder = batch.Folder;
            var gone = Held(batch, path);
            Assert.True(gone.TakeOut(out _));
            gone.Dispose();
        }
        _fs.AddFile(path, new MockFileData([9]));

        using (var left = _hand.OpenLeft(Installer)!)
        {
            using var held = left.Hold(left.Batches[0].Files[0]).File!;
            Assert.False(held.PutBack(out var code));
            Assert.Equal(Kernel32.ERROR_ALREADY_EXISTS, code);
        }

        Assert.Equal([1, 2, 3, 4, 5], _fs.File.ReadAllBytes(folder + @"\a.msp"));
        Assert.Equal([9], _fs.File.ReadAllBytes(path));
    }

    [Fact]
    public void A_file_that_has_gone_since_it_was_listed_answers_gone()
    {
        var folder = Installer + @"\" + InHandFolders.FolderName + @"\0123456789abcdef0123456789abcdef";
        _fs.AddFile(folder + @"\a.msp", new MockFileData([1]));

        using var left = _hand.OpenLeft(Installer)!;
        var file = left.Batches[0].Files[0];
        _fs.File.Delete(folder + @"\a.msp");

        Assert.Equal(HoldOutcome.Gone, left.Hold(file).Outcome);
    }
}
