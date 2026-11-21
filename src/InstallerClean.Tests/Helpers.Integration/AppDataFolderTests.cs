using System.Text;
using InstallerClean.Helpers;

namespace InstallerClean.Tests.Helpers.Integration;

/// <summary>
/// <see cref="AppDataFolder.Settle"/> driven against a folder of its own under the temp
/// folder standing in for %LOCALAPPDATA%, so no test moves the folder of the account
/// running the suite.
/// </summary>
public class AppDataFolderTests : IDisposable
{
    private const string Header = "# crash.log header, first line\n# second line\n\n";

    private readonly string _root = TestTemp.NewFolder("ic-appdata-");
    private readonly string _parent;
    private readonly string _earlier;
    private readonly string _current;

    public AppDataFolderTests()
    {
        _parent = Path.Combine(_root, "NoFaff");
        _earlier = Path.Combine(_parent, "InstallerClean");
        _current = Path.Combine(_root, "InstallerClean");
    }

    public void Dispose()
    {
        // A link is removed as a link, so a decoy's contents are never followed into.
        foreach (var link in new[] { _earlier, _parent })
        {
            try
            {
                if (StorageHelpers.CheckReparsePoint(link, out _) == StorageHelpers.ReparseCheck.Yes)
                    Directory.Delete(link);
            }
            catch (Exception) { }
        }
        TestTemp.Delete(_root);
    }

    private static void Write(string folder, string name, string text)
    {
        Directory.CreateDirectory(folder);
        File.WriteAllText(Path.Combine(folder, name), text);
    }

    private static string Read(string folder, string name) =>
        File.ReadAllText(Path.Combine(folder, name));

    /// <summary>A directory symlink at <paramref name="link"/>, or false where the host refuses one.</summary>
    private static bool TryLink(string link, string target)
    {
        try
        {
            Directory.CreateSymbolicLink(link, target);
            return true;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            return false;
        }
    }

    [Fact]
    public void With_no_earlier_folder_the_answer_is_the_new_folder_and_nothing_is_made()
    {
        var settled = AppDataFolder.Settle(_root);

        Assert.Equal(_current, settled.Folder);
        Assert.Null(settled.Failure);
        Assert.Empty(Directory.EnumerateFileSystemEntries(_root));
    }

    [Fact]
    public void The_earlier_folder_is_moved_whole_and_the_empty_NoFaff_folder_goes()
    {
        Write(_earlier, "settings.json", "{\"language\":\"it\"}");
        Write(_earlier, "last-run.json", "{\"schemaVersion\":9}");
        Write(_earlier, "crash.log", Header + "entry\n");
        Write(_earlier, "settings.json.bad", "not json");

        var settled = AppDataFolder.Settle(_root);

        Assert.Equal(_current, settled.Folder);
        Assert.Null(settled.Failure);
        Assert.Equal("{\"language\":\"it\"}", Read(_current, "settings.json"));
        Assert.Equal("{\"schemaVersion\":9}", Read(_current, "last-run.json"));
        Assert.Equal(Header + "entry\n", Read(_current, "crash.log"));
        Assert.Equal("not json", Read(_current, "settings.json.bad"));
        Assert.False(Directory.Exists(_parent));
    }

    [Fact]
    public void A_NoFaff_folder_holding_anything_else_stays()
    {
        Write(_earlier, "settings.json", "{}");
        Write(Path.Combine(_parent, "OtherApp"), "kept.txt", "kept");

        var settled = AppDataFolder.Settle(_root);

        Assert.Equal(_current, settled.Folder);
        Assert.False(Directory.Exists(_earlier));
        Assert.Equal("kept", Read(Path.Combine(_parent, "OtherApp"), "kept.txt"));
    }

    [Fact]
    public void An_empty_NoFaff_folder_with_nothing_to_move_goes()
    {
        Directory.CreateDirectory(_parent);

        var settled = AppDataFolder.Settle(_root);

        Assert.Equal(_current, settled.Folder);
        Assert.False(Directory.Exists(_parent));
    }

    [Fact]
    public void Where_both_folders_exist_the_new_copies_are_kept_and_the_rest_moved_across()
    {
        Write(_earlier, "settings.json", "earlier");
        Write(_earlier, "last-run.json", "earlier");
        Write(_earlier, "only-earlier.txt", "moved");
        Write(_current, "settings.json", "kept");
        Write(_current, "last-run.json", "kept");

        var settled = AppDataFolder.Settle(_root);

        Assert.Equal(_current, settled.Folder);
        Assert.Null(settled.Failure);
        Assert.Equal("kept", Read(_current, "settings.json"));
        Assert.Equal("kept", Read(_current, "last-run.json"));
        Assert.Equal("moved", Read(_current, "only-earlier.txt"));
        Assert.False(Directory.Exists(_earlier));
        Assert.False(Directory.Exists(_parent));
    }

    [Fact]
    public void Where_both_folders_hold_a_crash_log_the_earlier_entries_are_added_once_without_their_header()
    {
        var bom = Encoding.UTF8.GetPreamble();
        Directory.CreateDirectory(_earlier);
        File.WriteAllBytes(Path.Combine(_earlier, "crash.log"),
            [.. bom, .. Encoding.UTF8.GetBytes(Header + "---- earlier entry ----\n")]);
        Directory.CreateDirectory(_current);
        File.WriteAllBytes(Path.Combine(_current, "crash.log"),
            [.. bom, .. Encoding.UTF8.GetBytes(Header + "---- kept entry ----\n")]);

        var settled = AppDataFolder.Settle(_root);

        Assert.Equal(_current, settled.Folder);
        Assert.Null(settled.Failure);
        var bytes = File.ReadAllBytes(Path.Combine(_current, "crash.log"));
        Assert.Equal(
            [.. bom, .. Encoding.UTF8.GetBytes(Header + "---- kept entry ----\n---- earlier entry ----\n")],
            bytes);
        Assert.False(Directory.Exists(_earlier));
    }

    [Fact]
    public void A_subfolder_in_the_earlier_folder_keeps_that_folder_in_place()
    {
        Write(_earlier, "settings.json", "earlier");
        Write(Path.Combine(_earlier, "sub"), "inside.txt", "left");
        Directory.CreateDirectory(_current);

        var settled = AppDataFolder.Settle(_root);

        Assert.Equal(_current, settled.Folder);
        Assert.Equal("earlier", Read(_current, "settings.json"));
        Assert.Equal("left", Read(Path.Combine(_earlier, "sub"), "inside.txt"));
    }

    [Fact]
    public void A_refused_rename_answers_with_the_earlier_folder_and_moves_nothing()
    {
        // Windows refuses to rename a folder while a file inside it is open; other hosts
        // rename it regardless, so there is nothing to refuse there.
        if (!OperatingSystem.IsWindows()) return;

        Write(_earlier, "settings.json", "earlier");
        Write(_earlier, "crash.log", "log");
        using (new FileStream(Path.Combine(_earlier, "settings.json"), FileMode.Open, FileAccess.Read, FileShare.None))
        {
            var settled = AppDataFolder.Settle(_root);

            Assert.Equal(_earlier, settled.Folder);
            Assert.NotNull(settled.Failure);
        }

        Assert.False(Directory.Exists(_current));
        Assert.Equal("earlier", Read(_earlier, "settings.json"));
        Assert.Equal("log", Read(_earlier, "crash.log"));
    }

    [Fact]
    public void An_earlier_folder_that_is_a_link_is_left_alone()
    {
        var decoy = Path.Combine(_root, "decoy");
        Write(decoy, "settings.json", "decoy");
        Directory.CreateDirectory(_parent);
        if (!TryLink(_earlier, decoy)) return; // this host cannot create a directory symlink

        var settled = AppDataFolder.Settle(_root);

        Assert.Equal(_earlier, settled.Folder);
        Assert.False(Directory.Exists(_current));
        Assert.Equal("decoy", Read(decoy, "settings.json"));
        Assert.Equal(StorageHelpers.ReparseCheck.Yes, StorageHelpers.CheckReparsePoint(_earlier, out _));
    }

    [Fact]
    public void A_NoFaff_folder_that_is_a_link_is_left_alone()
    {
        var decoy = Path.Combine(_root, "decoy");
        Write(Path.Combine(decoy, "InstallerClean"), "settings.json", "decoy");
        if (!TryLink(_parent, decoy)) return; // this host cannot create a directory symlink

        var settled = AppDataFolder.Settle(_root);

        Assert.Equal(_earlier, settled.Folder);
        Assert.False(Directory.Exists(_current));
        Assert.Equal("decoy", Read(Path.Combine(decoy, "InstallerClean"), "settings.json"));
    }

    [Fact]
    public void A_new_folder_that_is_a_link_leaves_the_earlier_folder_in_use()
    {
        var decoy = Path.Combine(_root, "decoy");
        Write(decoy, "settings.json", "decoy");
        Write(_earlier, "settings.json", "earlier");
        if (!TryLink(_current, decoy)) return; // this host cannot create a directory symlink

        var settled = AppDataFolder.Settle(_root);

        Assert.Equal(_earlier, settled.Folder);
        Assert.Equal("earlier", Read(_earlier, "settings.json"));
        Assert.Equal("decoy", Read(decoy, "settings.json"));
        Directory.Delete(_current);
    }

    [Fact]
    public void With_no_profile_folder_the_answer_is_relative_and_nothing_is_moved()
    {
        var settled = AppDataFolder.Settle(string.Empty);

        Assert.Equal("InstallerClean", settled.Folder);
        Assert.Null(settled.Failure);
    }

    [Theory]
    [InlineData("# one\n# two\n\n---- entry ----\nbody\n", "---- entry ----\nbody\n")]
    [InlineData("# one\r\n# two\r\n\r\n---- entry ----\r\n", "---- entry ----\r\n")]
    [InlineData("---- entry ----\n# not a header here\n", "---- entry ----\n# not a header here\n")]
    [InlineData("# only a header\n", "")]
    public void The_header_at_the_head_of_a_log_is_taken_off_and_nothing_after_it(string text, string expected)
    {
        Assert.Equal(expected, AppDataFolder.WithoutHeader(text));
    }
}
