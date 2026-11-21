using InstallerClean.Services;

namespace InstallerClean.Tests.Helpers.Integration;

/// <summary>
/// <c>File.Move(source, destination, overwrite)</c> and <see cref="FrameworkDirectory.Move"/>
/// move files and folders on a real volume as .NET 10's <c>File.Move</c> and
/// <c>Directory.Move</c> do, with the exceptions and HResults .NET 10 gives, and
/// <see cref="AppFileSystem"/>'s <c>File.Move</c> moves through the same code.
/// </summary>
/// <remarks>
/// Every path is under one folder in the temp folder, made and removed with <c>\\?\</c> in
/// front so a path of 300 characters is made and removed whatever the long-path policy is.
/// </remarks>
public sealed class FrameworkFileTests : IDisposable
{
    private readonly string _root = TestTemp.NewFolder("ic-move-");


    public void Dispose()
    {
        TestTemp.Delete(_root);
    }

    private string Write(string name, string text)
    {
        var path = Path.Combine(_root, name);
        File.WriteAllText(path, text);
        return path;
    }

    [Fact]
    public void A_file_moves_to_the_destination()
    {
        var source = Write("a.msp", "source");
        var destination = Path.Combine(_root, "b.msp");

        File.Move(source, destination, overwrite: false);

        Assert.False(File.Exists(source));
        Assert.Equal("source", File.ReadAllText(destination));
    }

    [Fact]
    public void Overwrite_replaces_a_file_at_the_destination()
    {
        var source = Write("a.msp", "source");
        var destination = Write("b.msp", "existing");

        File.Move(source, destination, overwrite: true);

        Assert.False(File.Exists(source));
        Assert.Equal("source", File.ReadAllText(destination));
    }

    [Fact]
    public void Without_overwrite_a_file_at_the_destination_stops_the_move_with_183()
    {
        // On one volume Windows answers ERROR_ALREADY_EXISTS; across volumes it answers
        // ERROR_FILE_EXISTS (80). Either way nothing is moved.
        var source = Write("a.msp", "source");
        var destination = Write("b.msp", "existing");

        var ex = Assert.Throws<IOException>(() => File.Move(source, destination, overwrite: false));

        Assert.Equal(unchecked((int)0x800700B7), ex.HResult);
        Assert.Equal("source", File.ReadAllText(source));
        Assert.Equal("existing", File.ReadAllText(destination));
    }

    [Fact]
    public void A_missing_source_is_FileNotFoundException_naming_its_full_path()
    {
        var spelled = Path.Combine(_root, "x", "..", "missing.msp");
        var full = Path.Combine(_root, "missing.msp");

        var ex = Assert.Throws<FileNotFoundException>(
            () => File.Move(spelled, Path.Combine(_root, "b.msp"), overwrite: false));

        Assert.Equal(full, ex.FileName);
        Assert.Equal($"Could not find file '{full}'.", ex.Message);
    }

    [Fact]
    public void A_folder_named_as_the_source_is_not_a_file_and_stays_where_it_is()
    {
        // MoveFileExW itself moves a folder, so the check before it is what refuses one.
        var folder = Path.Combine(_root, "folder.msp");
        Directory.CreateDirectory(folder);
        var destination = Path.Combine(_root, "b.msp");

        Assert.Throws<FileNotFoundException>(() => File.Move(folder, destination, overwrite: false));

        Assert.True(Directory.Exists(folder));
        Assert.False(Directory.Exists(destination));
        Assert.False(File.Exists(destination));
    }

    [Fact]
    public void A_missing_destination_folder_is_DirectoryNotFoundException()
    {
        var source = Write("a.msp", "source");

        Assert.Throws<DirectoryNotFoundException>(
            () => File.Move(source, Path.Combine(_root, "absent", "b.msp"), overwrite: false));

        Assert.True(File.Exists(source));
    }

    [Fact]
    public void A_source_held_open_without_delete_sharing_is_in_use_by_its_HResult()
    {
        // MoveFilesService files a failure as "in use" by the HResult alone.
        var source = Write("a.msp", "source");
        var destination = Path.Combine(_root, "b.msp");

        IOException ex;
        using (new FileStream(source, FileMode.Open, FileAccess.Read, FileShare.Read))
            ex = Assert.Throws<IOException>(() => File.Move(source, destination, overwrite: false));

        Assert.Equal(unchecked((int)0x80070020), ex.HResult);
        Assert.True(File.Exists(source));
        Assert.False(File.Exists(destination));
    }

    [Fact]
    public void Overwrite_replaces_a_symbolic_link_at_the_destination_and_leaves_its_target_alone()
    {
        var source = Write("a.msp", "source");
        var target = Write("target.txt", "link target");
        var link = Path.Combine(_root, "b.msp");
        try
        {
            File.CreateSymbolicLink(link, target);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            return; // this host cannot create a symbolic link
        }

        File.Move(source, link, overwrite: true);

        Assert.Equal(0, (int)(File.GetAttributes(link) & FileAttributes.ReparsePoint));
        Assert.Equal("source", File.ReadAllText(link));
        Assert.Equal("link target", File.ReadAllText(target));
    }

    [Fact]
    public void A_file_moves_to_and_from_a_path_of_300_characters()
    {
        var deep = _root;
        while (deep.Length < 290) deep = Path.Combine(deep, new string('d', 40));
        Directory.CreateDirectory(@"\\?\" + deep);
        var source = Write("a.msp", "source");
        var far = Path.Combine(deep, "b.msp");
        Assert.True(far.Length >= 300);

        File.Move(source, far, overwrite: false);

        Assert.False(File.Exists(source));
        Assert.True(File.Exists(@"\\?\" + far));

        File.Move(far, source, overwrite: false);

        Assert.False(File.Exists(@"\\?\" + far));
        Assert.Equal("source", File.ReadAllText(source));
    }

    [Fact]
    public void The_source_is_checked_before_the_destination()
    {
        Assert.Equal("sourceFileName",
            Assert.Throws<ArgumentNullException>(() => File.Move(null!, null!, overwrite: false)).ParamName);
        Assert.Equal("sourceFileName",
            Assert.Throws<ArgumentException>(() => File.Move("", null!, overwrite: false)).ParamName);
        Assert.Equal("destFileName",
            Assert.Throws<ArgumentNullException>(() => File.Move("a", null!, overwrite: false)).ParamName);
        Assert.Equal("destFileName",
            Assert.Throws<ArgumentException>(() => File.Move("a", "", overwrite: false)).ParamName);
    }

    [Fact]
    public void The_apps_file_system_moves_through_the_port()
    {
        var fileSystem = new AppFileSystem();
        Assert.IsType<FrameworkFileWrapper>(fileSystem.File);

        var deep = _root;
        while (deep.Length < 290) deep = Path.Combine(deep, new string('e', 40));
        Directory.CreateDirectory(@"\\?\" + deep);
        var source = Write("a.msp", "source");
        var far = Path.Combine(deep, "b.msp");

        fileSystem.File.Move(source, far);

        Assert.False(File.Exists(source));
        Assert.True(File.Exists(@"\\?\" + far));
    }

    [Fact]
    public void A_folder_moves_with_what_is_in_it()
    {
        var folder = Path.Combine(_root, "earlier");
        Directory.CreateDirectory(folder);
        File.WriteAllText(Path.Combine(folder, "settings.json"), "{}");
        var destination = Path.Combine(_root, "current");

        FrameworkDirectory.Move(folder, destination);

        Assert.False(Directory.Exists(folder));
        Assert.Equal("{}", File.ReadAllText(Path.Combine(destination, "settings.json")));
    }

    [Fact]
    public void A_folder_rename_that_changes_only_the_case_is_made()
    {
        var folder = Path.Combine(_root, "folder");
        Directory.CreateDirectory(folder);
        var renamed = Path.Combine(_root, "FOLDER");

        FrameworkDirectory.Move(folder, renamed);

        Assert.Equal(new[] { renamed }, Directory.GetDirectories(_root));
    }

    [Fact]
    public void A_folder_moved_onto_itself_is_refused_before_Windows_is_asked()
    {
        var folder = Path.Combine(_root, "folder");
        Directory.CreateDirectory(folder);

        var ex = Assert.Throws<IOException>(() => FrameworkDirectory.Move(folder, folder + Path.DirectorySeparatorChar));

        Assert.Equal("Source and destination path must be different.", ex.Message);
        Assert.True(Directory.Exists(folder));
    }

    [Fact]
    public void A_folder_moved_to_another_root_is_refused_before_Windows_is_asked()
    {
        var folder = Path.Combine(_root, "folder");
        Directory.CreateDirectory(folder);
        var otherRoot = string.Equals(Path.GetPathRoot(_root), @"Z:\", StringComparison.OrdinalIgnoreCase) ? @"Y:\x" : @"Z:\x";

        var ex = Assert.Throws<IOException>(() => FrameworkDirectory.Move(folder, otherRoot));

        Assert.Equal("Source and destination path must have identical roots. Move will not work across volumes.", ex.Message);
        Assert.True(Directory.Exists(folder));
    }

    [Fact]
    public void A_folder_whose_destination_exists_is_refused_naming_the_destination()
    {
        var folder = Path.Combine(_root, "earlier");
        var destination = Path.Combine(_root, "current");
        Directory.CreateDirectory(folder);
        Directory.CreateDirectory(destination);

        var ex = Assert.Throws<IOException>(() => FrameworkDirectory.Move(folder, destination));

        Assert.Equal(unchecked((int)0x800700B7), ex.HResult);
        Assert.Contains($"'{destination}'", ex.Message, StringComparison.Ordinal);
        Assert.True(Directory.Exists(folder));
    }

    [Fact]
    public void A_missing_folder_is_DirectoryNotFoundException_naming_it()
    {
        var folder = Path.Combine(_root, "absent");

        var ex = Assert.Throws<DirectoryNotFoundException>(
            () => FrameworkDirectory.Move(folder, Path.Combine(_root, "current")));

        Assert.Contains($"'{folder}'", ex.Message, StringComparison.Ordinal);
    }
}
