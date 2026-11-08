using InstallerClean.Services;

namespace InstallerClean.Tests.Helpers.Integration;

/// <summary>
/// Deleting a file, setting its attributes and reading its size on a real volume as .NET 10
/// does: the path is made full by .NET 10's rules, which keep characters such as a trailing
/// no-break space that .NET Framework's own trims, so the act lands on the file the name
/// names; a path of 260 characters or more is acted on whole; and the app's file system acts
/// the same way.
/// </summary>
/// <remarks>
/// Every path is under one folder in the temp folder, made and removed with <c>\\?\</c> in
/// front so a path of 300 characters is made and removed whatever the long-path policy is.
/// </remarks>
public sealed class FrameworkFileDeleteAndLengthTests : IDisposable
{
    private readonly string _root = TestTemp.NewFolder("ic-delete-length-");
    private readonly string _file;
    private readonly string _spaced;

    public FrameworkFileDeleteAndLengthTests()
    {
        _file = Path.Combine(_root, "x.txt");
        File.WriteAllText(_file, "xyz");
        _spaced = _file + "\u00A0";
    }

    public void Dispose()
    {
        TestTemp.Delete(_root);
    }

    private string LongFolder()
    {
        var folder = _root;
        while (folder.Length < 290) folder = Path.Combine(folder, new string('f', 40));
        Directory.CreateDirectory(@"\\?\" + folder);
        return folder;
    }

    [Fact]
    public void A_delete_under_the_name_with_a_no_break_space_after_it_leaves_the_file()
    {
        FrameworkFile.Delete(_spaced);
        new AppFileSystem().File.Delete(_spaced);

        Assert.True(File.Exists(_file));
    }

    [Fact]
    public void A_delete_of_a_file_that_is_not_there_is_not_an_error()
    {
        FrameworkFile.Delete(Path.Combine(_root, "absent.txt"));
    }

    [Fact]
    public void A_delete_of_a_file_in_a_folder_that_is_not_there_is_a_DirectoryNotFoundException()
    {
        Assert.Throws<DirectoryNotFoundException>(
            () => FrameworkFile.Delete(Path.Combine(_root, "absent", "x.txt")));
    }

    [Fact]
    public void A_delete_of_a_read_only_file_is_refused_and_leaves_it()
    {
        File.SetAttributes(_file, FileAttributes.ReadOnly);

        Assert.Throws<UnauthorizedAccessException>(() => FrameworkFile.Delete(_file));
        Assert.Throws<UnauthorizedAccessException>(() => new AppFileSystem().File.Delete(_file));
        Assert.True(File.Exists(_file));
    }

    [Fact]
    public void A_delete_of_a_folder_is_refused()
    {
        Assert.Throws<UnauthorizedAccessException>(() => FrameworkFile.Delete(_root));
        Assert.True(Directory.Exists(_root));
    }

    [Fact]
    public void A_null_or_empty_path_is_refused_before_Windows_is_asked()
    {
        Assert.Throws<ArgumentNullException>(() => FrameworkFile.Delete(null!));
        Assert.Throws<ArgumentException>(() => FrameworkFile.Delete(""));
        Assert.Throws<ArgumentException>(() => FrameworkFile.SetAttributes("", FileAttributes.Normal));
        Assert.Throws<ArgumentNullException>(() => new AppFileSystem().FileInfo.New(null!));
        Assert.Throws<ArgumentException>(() => new AppFileSystem().FileInfo.New(""));
    }

    [Fact]
    public void A_file_on_a_path_of_300_characters_is_deleted()
    {
        var file = Path.Combine(LongFolder(), "long.txt");
        File.WriteAllText(@"\\?\" + file, "x");

        Assert.True(file.Length >= 300);
        new AppFileSystem().File.Delete(file);
        Assert.False(File.Exists(@"\\?\" + file));
    }

    [Fact]
    public void Attributes_set_under_the_name_with_a_no_break_space_after_it_reach_no_file()
    {
        Assert.Throws<FileNotFoundException>(() => FrameworkFile.SetAttributes(_spaced, FileAttributes.ReadOnly));
        Assert.Throws<FileNotFoundException>(
            () => new AppFileSystem().File.SetAttributes(_spaced, FileAttributes.ReadOnly));

        Assert.Equal(0, (int)(File.GetAttributes(_file) & FileAttributes.ReadOnly));
    }

    [Fact]
    public void Attributes_are_set_and_cleared_on_a_path_of_300_characters()
    {
        var file = Path.Combine(LongFolder(), "long.txt");
        File.WriteAllText(@"\\?\" + file, "x");
        var fileSystem = new AppFileSystem();

        Assert.True(file.Length >= 300);

        fileSystem.File.SetAttributes(file, FileAttributes.ReadOnly);
        Assert.Equal(FileAttributes.ReadOnly, File.GetAttributes(@"\\?\" + file) & FileAttributes.ReadOnly);

        FrameworkFile.SetAttributes(file, FileAttributes.Normal);
        Assert.Equal(0, (int)(File.GetAttributes(@"\\?\" + file) & FileAttributes.ReadOnly));
    }

    [Fact]
    public void The_size_is_read_from_the_file_the_name_names()
    {
        Assert.Equal(3, FrameworkFile.GetLength(_file));
        Assert.Equal(3, new AppFileSystem().FileInfo.New(_file).Length);

        var missing = Assert.Throws<FileNotFoundException>(() => FrameworkFile.GetLength(_spaced));
        Assert.Equal(_spaced, missing.FileName);
        Assert.Equal(unchecked((int)0x80070002), missing.HResult);
        Assert.Throws<FileNotFoundException>(() => new AppFileSystem().FileInfo.New(_spaced).Length);
    }

    [Fact]
    public void A_folder_has_no_size()
    {
        var folder = Assert.Throws<FileNotFoundException>(() => FrameworkFile.GetLength(_root));
        Assert.Equal(_root, folder.FileName);
        Assert.Throws<FileNotFoundException>(() => new AppFileSystem().FileInfo.New(_root).Length);
    }

    [Fact]
    public void A_file_in_a_folder_that_is_not_there_has_no_size()
    {
        var path = Path.Combine(_root, "absent", "x.txt");

        Assert.Throws<FileNotFoundException>(() => FrameworkFile.GetLength(path));
        Assert.Equal((FileAttributes)(-1), new AppFileSystem().FileInfo.New(path).Attributes);
    }

    [Fact]
    public void The_size_of_a_file_on_a_path_of_300_characters_is_read()
    {
        var file = Path.Combine(LongFolder(), "long.txt");
        File.WriteAllText(@"\\?\" + file, "12345");

        Assert.True(file.Length >= 300);

        Assert.Equal(5, FrameworkFile.GetLength(file));
        Assert.Equal(5, new AppFileSystem().FileInfo.New(file).Length);
    }

    [Fact]
    public void A_file_info_reads_the_file_on_the_first_ask_and_again_after_a_refresh()
    {
        var path = Path.Combine(_root, "later.txt");
        var info = new AppFileSystem().FileInfo.New(path);

        File.WriteAllText(path, "ab");
        Assert.Equal(2, info.Length);

        File.WriteAllText(path, "abcd");
        Assert.Equal(2, info.Length);
        info.Refresh();
        Assert.Equal(4, info.Length);
    }

    [Fact]
    public void A_file_info_names_the_file_as_NET_10_does()
    {
        var info = new AppFileSystem().FileInfo.New(_spaced);

        Assert.Equal(_spaced, info.FullName);
        Assert.Equal("x.txt\u00A0", info.Name);
        Assert.Equal(".txt\u00A0", info.Extension);
        Assert.Equal(_root, info.DirectoryName);
        Assert.Equal(_spaced, info.ToString());
    }
}
