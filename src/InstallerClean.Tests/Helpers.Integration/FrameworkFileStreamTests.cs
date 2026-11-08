using System.Text;

namespace InstallerClean.Tests.Helpers.Integration;

/// <summary>
/// Files opened and read by path on a real volume as .NET 10 opens them: the path is made
/// full by .NET 10's rules, which keep a no-break space at the end of a name and a <c>|</c>
/// in it and take a period off the end, a path of 300 characters opens whatever the
/// long-path policy is, and a refusal is the exception .NET 10 gives, naming the full path.
/// </summary>
/// <remarks>
/// Every file is under one folder in the temp folder, written and removed with <c>\\?\</c>
/// in front so its name reaches the disk as written.
/// </remarks>
public sealed class FrameworkFileStreamTests : IDisposable
{
    private const int SharingViolation = unchecked((int)0x80070020);

    private readonly string _root = TestTemp.NewFolder("ic-open-");


    public void Dispose()
    {
        TestTemp.Delete(_root);
    }

    private static void Write(string path, byte[] bytes) => File.WriteAllBytes(@"\\?\" + path, bytes);

    private static void Write(string path, string text) => Write(path, Encoding.UTF8.GetBytes(text));

    [Fact]
    public void A_file_is_opened_under_its_name_with_a_no_break_space_after_it()
    {
        var plain = Path.Combine(_root, "n.txt");
        Write(plain, "plain");
        Write(plain + "\u00A0", "spaced");

        Assert.Equal("spaced", FrameworkFileStream.ReadAllText(plain + "\u00A0"));
    }

    [Fact]
    public void A_name_ending_in_a_period_opens_the_file_without_it()
    {
        var plain = Path.Combine(_root, "p");
        Write(plain, "plain");
        Write(plain + ".", "dotted");

        // A trailing period is taken off by GetFullPath on .NET 10 too, so the plain file
        // answers: .NET 10 asks Windows for the name its full path gives.
        Assert.Equal("plain", FrameworkFileStream.ReadAllText(plain + "."));
    }

    [Fact]
    public void A_bar_in_a_name_is_asked_of_Windows_and_refused_as_an_IOException()
    {
        // Joined by hand: .NET Framework's Path.Combine refuses a | itself, with an ArgumentException.
        var path = _root + @"\a|b";

        var ex = Record.Exception(() => FrameworkFileStream.OpenRead(path));

        Assert.IsType<IOException>(ex);
    }

    [Fact]
    public void A_file_of_300_characters_is_read()
    {
        var folder = _root;
        while (folder.Length + 41 < 300 - 10) folder = Path.Combine(folder, new string('f', 40));
        Directory.CreateDirectory(@"\\?\" + folder);
        var path = Path.Combine(folder, new string('g', 300 - folder.Length - 1));
        Assert.Equal(300, path.Length);
        Write(path, "long");

        Assert.Equal("long", FrameworkFileStream.ReadAllText(path));
        Assert.Equal(Encoding.UTF8.GetBytes("long"), FrameworkFileStream.ReadAllBytes(path));
    }

    [Fact]
    public void A_missing_file_is_a_FileNotFoundException_naming_its_full_path()
    {
        var path = Path.Combine(_root, "missing.txt");

        var ex = Assert.Throws<FileNotFoundException>(() => FrameworkFileStream.OpenRead(path));

        Assert.Equal(path, ex.FileName);
    }

    [Fact]
    public void A_file_in_a_missing_folder_is_a_DirectoryNotFoundException()
    {
        Assert.Throws<DirectoryNotFoundException>(
            () => FrameworkFileStream.OpenRead(Path.Combine(_root, "missing", "x.txt")));
    }

    [Fact]
    public void A_folder_opened_as_a_file_is_an_UnauthorizedAccessException()
    {
        Assert.Throws<UnauthorizedAccessException>(() => FrameworkFileStream.OpenRead(_root));
    }

    [Fact]
    public void A_drive_root_opened_as_a_file_is_an_UnauthorizedAccessException()
    {
        Assert.Throws<UnauthorizedAccessException>(
            () => FrameworkFileStream.OpenRead(Path.GetPathRoot(_root)!));
    }

    [Fact]
    public void A_file_held_open_with_no_sharing_is_a_sharing_violation()
    {
        var path = Path.Combine(_root, "held.txt");
        Write(path, "x");
        using var held = new FileStream(@"\\?\" + path, FileMode.Open, FileAccess.ReadWrite, FileShare.None);

        var ex = Assert.Throws<IOException>(
            () => FrameworkFileStream.Open(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete));

        Assert.Equal(SharingViolation, ex.HResult);
    }

    [Fact]
    public void A_null_or_empty_path_is_refused_before_Windows_is_asked()
    {
        Assert.Throws<ArgumentNullException>(() => FrameworkFileStream.OpenRead(null!));
        Assert.Throws<ArgumentException>(() => FrameworkFileStream.OpenRead(""));
    }

    [Fact]
    public void A_file_opened_to_write_is_refused()
    {
        Assert.Throws<NotSupportedException>(() => FrameworkFileStream.Open(
            Path.Combine(_root, "x"), FileMode.Create, FileAccess.Write, FileShare.Read));
        Assert.False(File.Exists(@"\\?\" + Path.Combine(_root, "x")));
    }

    [Fact]
    public void An_option_FileOptions_does_not_hold_is_refused()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => FrameworkFileStream.Open(
            Path.Combine(_root, "x"), FileMode.Open, FileAccess.Read, FileShare.Read, (FileOptions)0x00200000));
    }

    [Fact]
    public void A_mode_that_writes_with_read_access_alone_is_refused()
    {
        Assert.Throws<ArgumentException>(() => FrameworkFileStream.Open(
            Path.Combine(_root, "x"), FileMode.Create, FileAccess.Read, FileShare.Read));
    }

    [Theory]
    [InlineData(new byte[] { 0xEF, 0xBB, 0xBF, 0x61, 0xC3, 0xA9 })]
    [InlineData(new byte[] { 0x61, 0xC3, 0xA9 })]
    [InlineData(new byte[] { 0xFF, 0xFE, 0x61, 0x00, 0xE9, 0x00 })]
    public void Text_is_read_as_UTF8_unless_a_byte_order_mark_names_another_encoding(byte[] bytes)
    {
        var path = Path.Combine(_root, "t.txt");
        Write(path, bytes);

        Assert.Equal("a\u00E9", FrameworkFileStream.ReadAllText(path));
    }

    [Fact]
    public void An_empty_file_reads_as_no_bytes()
    {
        var path = Path.Combine(_root, "empty");
        Write(path, Array.Empty<byte>());

        Assert.Empty(FrameworkFileStream.ReadAllBytes(path));
    }
}
