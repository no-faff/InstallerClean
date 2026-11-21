using InstallerClean.Services;

namespace InstallerClean.Tests.Helpers.Integration;

/// <summary>
/// Whether a file or a folder is there, and a file's attributes, on a real volume as .NET 10
/// answers them: the path is made full by .NET 10's rules, which keep characters such as a
/// trailing no-break space that .NET Framework's own trims, a name Windows refuses is not
/// asked again as a search pattern, and a path of 260 characters or more is read whole. The
/// app's file system answers the same way.
/// </summary>
/// <remarks>
/// Every path is under one folder in the temp folder, made and removed with <c>\\?\</c> in
/// front so a path of 300 characters is made and removed whatever the long-path policy is.
/// </remarks>
public sealed class FrameworkFileAttributesTests : IDisposable
{
    private const int ERROR_INVALID_NAME = 123;

    private readonly string _root = TestTemp.NewFolder("ic-attributes-");
    private readonly string _file;

    public FrameworkFileAttributesTests()
    {
        _file = Path.Combine(_root, "x.txt");
        File.WriteAllText(_file, "x");
    }

    public void Dispose()
    {
        TestTemp.Delete(_root);
    }

    [Fact]
    public void A_file_is_not_found_under_its_name_with_a_no_break_space_after_it()
    {
        var spaced = _file + "\u00A0";
        var fileSystem = new AppFileSystem();

        Assert.True(FrameworkFile.Exists(_file));
        Assert.False(FrameworkFile.Exists(spaced));
        Assert.False(fileSystem.File.Exists(spaced));
        Assert.Throws<FileNotFoundException>(() => FrameworkFile.GetAttributes(spaced));
        Assert.Throws<FileNotFoundException>(() => fileSystem.File.GetAttributes(spaced));
    }

    [Fact]
    public void A_folder_is_not_found_under_its_name_with_a_no_break_space_after_it()
    {
        Assert.False(FrameworkDirectory.Exists(_root + "\u00A0"));
        Assert.False(new AppFileSystem().Directory.Exists(_root + "\u00A0"));
    }

    [Fact]
    public void A_name_Windows_refuses_is_not_asked_again_as_a_search_pattern()
    {
        var pattern = Path.Combine(_root, "x?txt");

        Assert.Equal(ERROR_INVALID_NAME, FrameworkFileSystem.FillAttributeInfo(pattern, out _));
        var refused = Assert.ThrowsAny<IOException>(() => FrameworkFile.GetAttributes(pattern));
        Assert.Equal(unchecked((int)0x8007007B), refused.HResult);
        Assert.ThrowsAny<IOException>(() => new AppFileSystem().File.GetAttributes(pattern));
        Assert.False(FrameworkFile.Exists(pattern));
    }

    [Fact]
    public void A_file_on_a_path_of_300_characters_is_there_with_its_attributes()
    {
        var folder = _root;
        while (folder.Length < 280) folder = Path.Combine(folder, new string('f', 40));
        Directory.CreateDirectory(@"\\?\" + folder);
        var file = Path.Combine(folder, "long.txt");
        File.WriteAllText(@"\\?\" + file, "x");

        Assert.True(file.Length >= 300);
        Assert.True(FrameworkFile.Exists(file));
        Assert.True(FrameworkDirectory.Exists(folder));
        Assert.Equal(FileAttributes.Archive, FrameworkFile.GetAttributes(file) & FileAttributes.Archive);
        Assert.True(new AppFileSystem().File.Exists(file));
    }

    [Fact]
    public void A_missing_file_is_a_FileNotFoundException_naming_its_full_path()
    {
        var missing = Path.Combine(_root, "missing.txt");

        var notFound = Assert.Throws<FileNotFoundException>(() => FrameworkFile.GetAttributes(missing));

        Assert.Equal(missing, notFound.FileName);
        Assert.Equal(unchecked((int)0x80070002), notFound.HResult);
        Assert.Equal($"Could not find file '{missing}'.", notFound.Message);
    }

    [Fact]
    public void A_file_in_a_missing_folder_is_a_DirectoryNotFoundException()
    {
        Assert.Throws<DirectoryNotFoundException>(
            () => FrameworkFile.GetAttributes(Path.Combine(_root, "missing", "x.txt")));
    }

    [Fact]
    public void A_folder_is_a_folder_and_not_a_file()
    {
        Assert.False(FrameworkFile.Exists(_root));
        Assert.True(FrameworkDirectory.Exists(_root));
        Assert.True(new AppFileSystem().Directory.Exists(_root));
        Assert.False(FrameworkDirectory.Exists(_file));
        Assert.True((FrameworkFile.GetAttributes(_root) & FileAttributes.Directory) != 0);
    }

    [Fact]
    public void A_file_asked_for_with_a_separator_after_it_is_not_there_but_has_its_attributes()
    {
        Assert.False(FrameworkFile.Exists(_file + @"\"));
        Assert.Equal(FrameworkFile.GetAttributes(_file), FrameworkFile.GetAttributes(_file + @"\"));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public void No_path_names_nothing(string? path)
    {
        Assert.False(FrameworkFile.Exists(path));
        Assert.False(FrameworkDirectory.Exists(path));
    }
}
