using InstallerClean.Services;

namespace InstallerClean.Tests.Helpers.Integration;

/// <summary>
/// Making, removing and listing folders on a real volume as .NET 10 does: a new folder goes
/// to Windows with <c>\\?\</c> in front whatever its length, so a folder of 248 to 259
/// characters is made; the path is made full by .NET 10's rules, which keep characters such
/// as a trailing no-break space that .NET Framework's own trims; and a listing matches its
/// pattern against each entry's own name and spells each result from the folder as given.
/// </summary>
/// <remarks>
/// Every path is under one folder in the temp folder, made and removed with <c>\\?\</c> in
/// front so a path of 300 characters is made and removed whatever the long-path policy is.
/// </remarks>
public sealed class FrameworkFolderTests : IDisposable
{
    private const int ERROR_ALREADY_EXISTS = 183;
    private const int ERROR_DIR_NOT_EMPTY = 145;

    private readonly string _root = TestTemp.NewFolder("ic-folders-");


    public void Dispose()
    {
        TestTemp.Delete(_root);
    }

    private static bool FolderIsThere(string path) => Directory.Exists(@"\\?\" + path);

    private string PathOfLength(int length)
    {
        var path = _root;
        while (path.Length + 41 < length) path = Path.Combine(path, new string('f', 40));
        path = Path.Combine(path, new string('g', length - path.Length - 1));
        Assert.Equal(length, path.Length);
        return path;
    }

    [Fact]
    public void A_folder_and_the_folders_above_it_are_made()
    {
        var folder = Path.Combine(_root, "a", "b", "c");

        FrameworkDirectory.CreateDirectory(folder);
        FrameworkDirectory.CreateDirectory(folder);

        Assert.True(FolderIsThere(folder));
    }

    [Theory]
    [InlineData(248)]
    [InlineData(259)]
    [InlineData(300)]
    public void A_folder_of_248_characters_or_more_is_made(int length)
    {
        var folder = PathOfLength(length);

        FrameworkDirectory.CreateDirectory(folder);

        Assert.True(FolderIsThere(folder));
    }

    [Fact]
    public void A_folder_is_made_under_its_name_with_a_no_break_space_after_it()
    {
        var plain = Path.Combine(_root, "n");

        FrameworkDirectory.CreateDirectory(plain + "\u00A0");

        Assert.True(FolderIsThere(plain + "\u00A0"));
        Assert.False(FolderIsThere(plain));
    }

    [Fact]
    public void The_app_file_system_makes_a_folder_and_hands_back_its_own_folder_info()
    {
        var fileSystem = new AppFileSystem();
        var folder = Path.Combine(_root, "made", "here");

        var made = fileSystem.Directory.CreateDirectory(folder);

        Assert.True(FolderIsThere(folder));
        Assert.IsType<FrameworkDirectoryInfo>(made);
        Assert.Equal(folder, made.FullName);
    }

    [Fact]
    public void The_app_file_system_makes_a_folder_under_its_name_with_a_no_break_space_after_it()
    {
        var plain = Path.Combine(_root, "m");

        new AppFileSystem().Directory.CreateDirectory(plain + "\u00A0");

        Assert.True(FolderIsThere(plain + "\u00A0"));
        Assert.False(FolderIsThere(plain));
    }

    [Fact]
    public void The_app_file_system_makes_a_folder_of_259_characters()
    {
        var folder = PathOfLength(259);

        new AppFileSystem().Directory.CreateDirectory(folder);

        Assert.True(FolderIsThere(folder));
    }

    [Theory]
    [InlineData("a*b")]
    [InlineData("a?b")]
    public void The_app_file_system_refuses_a_wildcard_in_a_folder_name_with_an_IOException(string name)
    {
        // .NET Framework's own CreateDirectory throws ArgumentException for these.
        var ex = Record.Exception(() => new AppFileSystem().Directory.CreateDirectory(Path.Combine(_root, name)));

        Assert.IsType<IOException>(ex);
    }

    [Fact]
    public void The_app_file_system_refuses_a_colon_after_the_drive_with_an_IOException()
    {
        // .NET Framework's own CreateDirectory throws NotSupportedException for this.
        var ex = Record.Exception(() => new AppFileSystem().Directory.CreateDirectory(_root + @"\a:b"));

        Assert.IsType<IOException>(ex);
    }

    [Fact]
    public void The_app_file_system_removes_an_empty_folder_and_leaves_one_holding_anything()
    {
        var fileSystem = new AppFileSystem();
        var empty = Path.Combine(_root, "empty");
        var full = Path.Combine(_root, "full");
        FrameworkDirectory.CreateDirectory(empty);
        FrameworkDirectory.CreateDirectory(full);
        File.WriteAllText(Path.Combine(full, "x"), "x");

        fileSystem.Directory.Delete(empty);
        var ex = Record.Exception(() => fileSystem.Directory.Delete(full));

        Assert.False(FolderIsThere(empty));
        Assert.IsType<IOException>(ex);
        Assert.True(FolderIsThere(full));
    }

    [Fact]
    public void A_file_where_a_folder_is_to_go_is_an_IOException_naming_it()
    {
        var file = Path.Combine(_root, "taken");
        File.WriteAllText(file, "x");

        var refused = Assert.Throws<IOException>(() => FrameworkDirectory.CreateDirectory(Path.Combine(file, "below")));

        Assert.Equal(unchecked((int)0x80070000 | ERROR_ALREADY_EXISTS), refused.HResult);
        Assert.Contains(file, refused.Message);
    }

    [Fact]
    public void A_null_or_empty_folder_is_refused_before_Windows_is_asked()
    {
        Assert.Throws<ArgumentNullException>(() => FrameworkDirectory.CreateDirectory(null!));
        Assert.Throws<ArgumentException>(() => FrameworkDirectory.CreateDirectory(""));
        Assert.Throws<ArgumentNullException>(() => FrameworkDirectory.EnumerateFiles(null!, "*"));
        Assert.Throws<ArgumentNullException>(() => FrameworkDirectory.EnumerateFiles(_root, null!));
    }

    [Fact]
    public void An_empty_folder_is_removed_and_one_holding_anything_stays()
    {
        var empty = Path.Combine(_root, "empty");
        var full = Path.Combine(_root, "full");
        Directory.CreateDirectory(empty);
        Directory.CreateDirectory(full);
        File.WriteAllText(Path.Combine(full, "x.txt"), "x");

        FrameworkDirectory.Delete(empty);
        var refused = Assert.Throws<IOException>(() => FrameworkDirectory.Delete(full));

        Assert.False(FolderIsThere(empty));
        Assert.True(FolderIsThere(full));
        Assert.Equal(unchecked((int)0x80070000 | ERROR_DIR_NOT_EMPTY), refused.HResult);
    }

    [Fact]
    public void A_folder_that_is_not_there_is_not_removed()
    {
        Assert.Throws<DirectoryNotFoundException>(() => FrameworkDirectory.Delete(Path.Combine(_root, "absent")));
    }

    [Fact]
    public void A_removal_under_the_name_with_a_no_break_space_after_it_leaves_the_folder()
    {
        var plain = Path.Combine(_root, "n");
        Directory.CreateDirectory(plain);

        Assert.Throws<DirectoryNotFoundException>(() => FrameworkDirectory.Delete(plain + "\u00A0"));
        Assert.True(FolderIsThere(plain));
    }

    [Fact]
    public void A_folder_of_300_characters_is_removed()
    {
        var folder = PathOfLength(300);
        Directory.CreateDirectory(@"\\?\" + folder);

        FrameworkDirectory.Delete(folder);

        Assert.False(FolderIsThere(folder));
    }

    [Fact]
    public void A_listing_spells_each_entry_from_the_folder_as_given()
    {
        File.WriteAllText(Path.Combine(_root, "a.txt"), "x");

        Assert.Equal([Path.Combine(_root, "a.txt")], FrameworkDirectory.EnumerateFiles(_root, "*"));
        Assert.Equal([_root + @"\" + "a.txt"], FrameworkDirectory.EnumerateFiles(_root + @"\", "*"));
    }

    [Fact]
    public void A_listing_keeps_files_folders_or_both_and_never_the_dot_entries()
    {
        File.WriteAllText(Path.Combine(_root, "f.txt"), "x");
        Directory.CreateDirectory(Path.Combine(_root, "d"));

        Assert.Equal(["f.txt"], FrameworkDirectory.EnumerateFiles(_root, "*").Select(Path.GetFileName));
        Assert.Equal(["d"], FrameworkDirectory.EnumerateDirectories(_root, "*").Select(Path.GetFileName));
        Assert.Equal(["d", "f.txt"], FrameworkDirectory.EnumerateFileSystemEntries(_root).Select(Path.GetFileName).OrderBy(n => n, StringComparer.Ordinal));
    }

    [Fact]
    public void A_pattern_is_matched_against_the_entry_s_own_name_only()
    {
        // Where the volume makes 8.3 names, foo.exeold's is FOO~1.EXE, which *.exe matches.
        File.WriteAllText(Path.Combine(_root, "foo.exeold"), "x");
        File.WriteAllText(Path.Combine(_root, "bar.exe"), "x");
        File.WriteAllText(Path.Combine(_root, "InstallerClean-1.exe"), "x");

        Assert.Equal(["InstallerClean-1.exe", "bar.exe"],
            FrameworkDirectory.EnumerateFiles(_root, "*.exe").Select(Path.GetFileName).OrderBy(n => n, StringComparer.Ordinal));
        Assert.Equal(["InstallerClean-1.exe"],
            FrameworkDirectory.EnumerateFiles(_root, "installerclean-*.EXE").Select(Path.GetFileName));
        Assert.Equal(["foo.exeold"], FrameworkDirectory.EnumerateFiles(_root, "foo*").Select(Path.GetFileName));
    }

    [Fact]
    public void A_folder_at_the_front_of_the_pattern_is_listed()
    {
        var sub = Path.Combine(_root, "sub");
        Directory.CreateDirectory(sub);
        File.WriteAllText(Path.Combine(sub, "a.txt"), "x");

        Assert.Equal([Path.Combine(_root, "sub", "a.txt")], FrameworkDirectory.EnumerateFiles(_root, @"sub\*.txt"));
        Assert.Throws<ArgumentException>(() => FrameworkDirectory.EnumerateFiles(_root, @"C:\*.txt"));
    }

    [Fact]
    public void A_folder_in_the_pattern_is_cut_off_where_it_is_written()
    {
        var folder = Path.Combine(_root, "a", "b");
        Directory.CreateDirectory(folder);
        File.WriteAllText(Path.Combine(folder, "c1.txt"), "x");
        File.WriteAllText(Path.Combine(folder, "bc.txt"), "x");

        Assert.Equal(["c1.txt"], FrameworkDirectory.EnumerateFiles(_root, @"a\\\b\c*").Select(Path.GetFileName));
    }

    [Fact]
    public void A_folder_that_is_not_there_throws_when_the_listing_is_made()
    {
        var absent = Path.Combine(_root, "absent");

        var missing = Assert.Throws<DirectoryNotFoundException>(() => FrameworkDirectory.EnumerateFileSystemEntries(absent));
        Assert.Contains(absent, missing.Message);
        Assert.Throws<DirectoryNotFoundException>(() => FrameworkDirectory.EnumerateFiles(Path.Combine(_root, "absent.txt"), "*"));
    }

    [Fact]
    public void A_name_with_a_no_break_space_after_it_is_listed_with_it()
    {
        File.WriteAllText(@"\\?\" + Path.Combine(_root, "x.txt\u00A0"), "x");

        Assert.Equal(["x.txt\u00A0"], FrameworkDirectory.EnumerateFiles(_root, "*").Select(Path.GetFileName));
    }

    [Fact]
    public void A_folder_of_300_characters_is_listed()
    {
        var folder = PathOfLength(300);
        Directory.CreateDirectory(@"\\?\" + folder);
        File.WriteAllText(@"\\?\" + Path.Combine(folder, "a.txt"), "x");

        Assert.Equal([Path.Combine(folder, "a.txt")], FrameworkDirectory.EnumerateFiles(folder, "*"));
    }

    [Fact]
    public void Entries_past_the_first_read_of_the_folder_are_listed()
    {
        // Each name takes more than 200 bytes of the listing's 4096-byte buffer.
        var names = Enumerable.Range(0, 200).Select(i => i.ToString("D3") + new string('n', 100) + ".txt").ToList();
        foreach (var name in names) File.WriteAllText(Path.Combine(_root, name), "x");

        Assert.Equal(names, FrameworkDirectory.EnumerateFiles(_root, "*").Select(Path.GetFileName).OrderBy(n => n, StringComparer.Ordinal));
    }

    [Fact]
    public void A_second_enumeration_lists_the_folder_again()
    {
        File.WriteAllText(Path.Combine(_root, "a.txt"), "x");
        var listing = FrameworkDirectory.EnumerateFiles(_root, "*");

        Assert.Single(listing);
        File.WriteAllText(Path.Combine(_root, "b.txt"), "x");
        Assert.Equal(2, listing.Count());
    }
}
