using System.IO.Abstractions;
using InstallerClean.Services;

namespace InstallerClean.Tests.Helpers.Integration;

/// <summary>
/// The scan's walk of a real folder, made as the walk makes it:
/// <c>DirectoryInfo.New(folder).EnumerateFiles("*", SearchOption.TopDirectoryOnly)</c> on the
/// app's own file system. Each file it lists carries the full path, attributes and size the
/// listing read, as .NET 10's DirectoryInfo gives them, and the folder is resolved by .NET 10's
/// rules, which keep characters such as a trailing no-break space that .NET Framework's own
/// DirectoryInfo trims.
/// </summary>
/// <remarks>
/// Every path is under one folder in the temp folder, made and removed with <c>\\?\</c> in
/// front so a name .NET Framework's own calls would change is made and removed as written.
/// </remarks>
public sealed class FrameworkDirectoryInfoTests : IDisposable
{
    private readonly string _root = TestTemp.NewFolder("ic-folder-info-");
    private readonly IFileSystem _fs = new AppFileSystem();


    public void Dispose()
    {
        TestTemp.Delete(_root);
    }

    private IEnumerable<IFileInfo> Walk(string folder) =>
        _fs.DirectoryInfo.New(folder).EnumerateFiles("*", SearchOption.TopDirectoryOnly);

    [Fact]
    public void Each_file_carries_the_full_path_size_and_attributes_the_listing_read()
    {
        // 200 names of more than 200 bytes each: the listing's 4096-byte buffer is read many
        // times, and each entry after the first in a buffer is reached by its offset.
        var sizes = new Dictionary<string, int>(StringComparer.Ordinal);
        for (var i = 0; i < 200; i++)
        {
            var name = i.ToString("D3") + new string('n', 100) + ".msi";
            sizes[name] = i * 7 + 1;
            File.WriteAllBytes(Path.Combine(_root, name), new byte[sizes[name]]);
        }
        var readOnly = Path.Combine(_root, "000" + new string('n', 100) + ".msi");
        File.SetAttributes(readOnly, FileAttributes.ReadOnly);

        var listed = Walk(_root).ToList();

        Assert.Equal(sizes.Keys.OrderBy(n => n, StringComparer.Ordinal),
            listed.Select(f => f.Name).OrderBy(n => n, StringComparer.Ordinal));
        foreach (var file in listed)
        {
            Assert.Equal(Path.Combine(_root, file.Name), file.FullName);
            Assert.Equal(file.FullName, file.ToString());
            Assert.Equal(".msi", file.Extension);
            Assert.Equal(sizes[file.Name], file.Length);
            Assert.Equal(file.FullName == readOnly, (file.Attributes & FileAttributes.ReadOnly) != 0);
            Assert.Equal(0, (int)(file.Attributes & FileAttributes.Directory));
        }
    }

    [Fact]
    public void The_size_is_the_one_the_listing_read_until_the_file_is_read_again()
    {
        var path = Path.Combine(_root, "a.msi");
        File.WriteAllBytes(path, new byte[10]);

        var file = Assert.Single(Walk(_root));
        File.WriteAllBytes(path, new byte[25]);

        Assert.Equal(10, file.Length);
        file.Refresh();
        Assert.Equal(25, file.Length);
    }

    [Fact]
    public void An_empty_file_lists_with_no_size_and_a_folder_is_left_out()
    {
        File.WriteAllBytes(Path.Combine(_root, "empty.msp"), []);
        Directory.CreateDirectory(Path.Combine(_root, "sub.msi"));

        var file = Assert.Single(Walk(_root));

        Assert.Equal("empty.msp", file.Name);
        Assert.Equal(0, file.Length);
    }

    [Fact]
    public void A_folder_whose_name_ends_in_a_no_break_space_is_the_folder_listed()
    {
        // .NET Framework's own DirectoryInfo trims the no-break space and lists the folder
        // without it: the plain folder's file, not the one in the folder as named.
        var plain = Path.Combine(_root, "n");
        var spaced = plain + "\u00A0";
        Directory.CreateDirectory(@"\\?\" + plain);
        Directory.CreateDirectory(@"\\?\" + spaced);
        File.WriteAllText(@"\\?\" + Path.Combine(plain, "plain.msi"), "x");
        File.WriteAllText(@"\\?\" + Path.Combine(spaced, "spaced.msi"), "x");

        var file = Assert.Single(Walk(spaced));

        Assert.Equal(Path.Combine(spaced, "spaced.msi"), file.FullName);
    }

    [Fact]
    public void A_long_name_is_read_whole_beside_a_short_one()
    {
        // Where the volume makes 8.3 names, the long one's is LONGNA~1.MSI.
        var longName = "long name with spaces " + new string('x', 60) + ".msi";
        File.WriteAllText(Path.Combine(_root, longName), "x");
        File.WriteAllText(Path.Combine(_root, "S.MSI"), "x");

        Assert.Equal(["S.MSI", longName],
            Walk(_root).Select(f => f.Name).OrderBy(n => n, StringComparer.Ordinal));
    }

    [Fact]
    public void A_folder_that_is_not_there_throws_when_the_listing_is_made()
    {
        var absent = Path.Combine(_root, "absent");
        var folder = _fs.DirectoryInfo.New(absent);

        var missing = Assert.Throws<DirectoryNotFoundException>(() => folder.EnumerateFiles("*", SearchOption.TopDirectoryOnly));
        Assert.Contains(absent, missing.Message);
    }

    [Fact]
    public void The_folder_keeps_its_path_as_given_and_its_full_path()
    {
        var folder = _fs.DirectoryInfo.New(_root + @"\.\");

        Assert.Equal(_root + @"\", folder.FullName);
        Assert.Equal(_root + @"\.\", folder.ToString());
        Assert.Equal(Path.GetFileName(_root), folder.Name);
    }

    [Fact]
    public void A_volume_root_joins_each_name_with_one_separator()
    {
        var root = Path.GetPathRoot(_root)!;

        foreach (var file in _fs.DirectoryInfo.New(root).EnumerateFiles("*", SearchOption.TopDirectoryOnly))
        {
            Assert.Equal(root + file.Name, file.FullName);
            Assert.DoesNotContain(@"\\", file.FullName.Substring(2));
        }
        Assert.Equal(root, _fs.DirectoryInfo.New(root).Name);
    }

    [Fact]
    public void A_folder_at_the_front_of_the_pattern_is_the_folder_listed()
    {
        var sub = Path.Combine(_root, "sub");
        Directory.CreateDirectory(sub);
        File.WriteAllBytes(Path.Combine(sub, "a.msi"), new byte[3]);
        File.WriteAllText(Path.Combine(sub, "b.txt"), "x");
        File.WriteAllText(Path.Combine(_root, "c.msi"), "x");

        var file = Assert.Single(_fs.DirectoryInfo.New(_root).EnumerateFiles(@"sub\*.msi"));

        Assert.Equal(Path.Combine(sub, "a.msi"), file.FullName);
        Assert.Equal(3, file.Length);
    }

    [Fact]
    public void A_folder_s_attributes_are_read_on_the_first_ask_and_again_after_a_refresh()
    {
        var sub = Path.Combine(_root, "attrs");
        Directory.CreateDirectory(sub);
        var folder = _fs.DirectoryInfo.New(sub);

        Assert.Equal(FileAttributes.Directory, folder.Attributes & (FileAttributes.Directory | FileAttributes.Hidden));
        File.SetAttributes(sub, File.GetAttributes(sub) | FileAttributes.Hidden);
        Assert.Equal(0, (int)(folder.Attributes & FileAttributes.Hidden));
        folder.Refresh();
        Assert.Equal(FileAttributes.Hidden, folder.Attributes & FileAttributes.Hidden);

        Assert.Equal(-1, (int)_fs.DirectoryInfo.New(Path.Combine(_root, "absent")).Attributes);
    }

    [Fact]
    public void A_listing_of_the_folders_inside_is_refused()
    {
        var folder = _fs.DirectoryInfo.New(_root);

        Assert.Throws<NotSupportedException>(() => folder.EnumerateFiles("*", SearchOption.AllDirectories));
        Assert.Throws<ArgumentOutOfRangeException>(() => folder.EnumerateFiles("*", (SearchOption)2));
        Assert.Throws<ArgumentNullException>(() => folder.EnumerateFiles(null!));
    }
}
