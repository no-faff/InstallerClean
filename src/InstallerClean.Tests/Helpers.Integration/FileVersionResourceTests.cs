using System.Buffers.Binary;
using System.Diagnostics;
using System.Reflection;
using InstallerClean.Helpers;
using InstallerClean.Interop.Native;

namespace InstallerClean.Tests.Helpers.Integration;

/// <summary>
/// The file version read from a file's own bytes, held against the version Windows
/// reads for the same files and against the version the compiler wrote, and the files
/// it answers null for. Each real file is read from a copy at a path no process was
/// started from, where Windows reads the file itself.
/// </summary>
public sealed class FileVersionResourceTests : IDisposable
{
    private readonly string _folder = TestTemp.NewFolder("ic-fileversion-");


    public void Dispose()
    {
        TestTemp.Delete(_folder);
    }

    private string CopyOf(string path, string name)
    {
        var copy = Path.Combine(_folder, name);
        File.Copy(path, copy);
        return copy;
    }

    private static Version WindowsRead(string path)
    {
        var info = FileVersionInfo.GetVersionInfo(path);
        return new Version(info.FileMajorPart, info.FileMinorPart, info.FileBuildPart, info.FilePrivatePart);
    }

    [Fact]
    public void A_library_built_here_carries_the_file_version_the_compiler_wrote()
    {
        var core = typeof(FileVersionResource).Assembly;
        var written = Version.Parse(core.GetCustomAttribute<AssemblyFileVersionAttribute>()!.Version);

        Assert.Equal(written, FileVersionResource.Read(CopyOf(core.Location, "core.dll")));
    }

    [Fact]
    public void A_Windows_library_reads_as_Windows_reads_it()
    {
        var copy = CopyOf(Path.Combine(Environment.SystemDirectory, "kernel32.dll"), "kernel32.dll");

        Assert.Equal(WindowsRead(copy), FileVersionResource.Read(copy));
    }

    [Fact]
    public void The_program_running_the_tests_reads_as_Windows_reads_it()
    {
        var copy = CopyOf(Environment.ProcessPath!, "host.exe");

        Assert.Equal(WindowsRead(copy), FileVersionResource.Read(copy));
    }

    [Fact]
    public void A_file_held_open_for_writing_is_still_read()
    {
        var copy = CopyOf(Path.Combine(Environment.SystemDirectory, "kernel32.dll"), "held.dll");
        // Windows answers no version for a file another handle holds open for writing,
        // so its read is taken first.
        var expected = WindowsRead(copy);
        using var held = new FileStream(copy, FileMode.Open, FileAccess.ReadWrite, FileShare.ReadWrite | FileShare.Delete);

        Assert.Equal(expected, FileVersionResource.Read(copy));
    }

    [Fact]
    public void A_missing_file_answers_null() =>
        Assert.Null(FileVersionResource.Read(Path.Combine(_folder, "absent.exe")));

    [Fact]
    public void A_missing_file_answers_null_from_the_read_that_throws_for_a_file_it_cannot_open() =>
        Assert.Null(FileVersionResource.ReadIfThere(Path.Combine(_folder, "absent.exe")));

    [Fact]
    public void A_file_held_open_sharing_nothing_answers_null()
    {
        var copy = CopyOf(Path.Combine(Environment.SystemDirectory, "kernel32.dll"), "held.dll");
        using var held = new FileStream(copy, FileMode.Open, FileAccess.Read, FileShare.None);

        Assert.Null(FileVersionResource.Read(copy));
    }

    /// <summary>
    /// A holder sharing reading does not refuse this read (A_file_held_open_for_writing_is_still_read);
    /// the holder here shares nothing, the one that does.
    /// </summary>
    [Fact]
    public void A_file_held_open_sharing_nothing_throws_a_sharing_violation_from_the_read_that_throws()
    {
        var copy = CopyOf(Path.Combine(Environment.SystemDirectory, "kernel32.dll"), "held.dll");
        using var held = new FileStream(copy, FileMode.Open, FileAccess.Read, FileShare.None);

        var thrown = Assert.ThrowsAny<IOException>(() => FileVersionResource.ReadIfThere(copy));
        Assert.Equal(Kernel32.ERROR_SHARING_VIOLATION, WindowsErrors.Of(thrown));
    }

    [Fact]
    public void A_file_that_is_not_a_program_answers_null()
    {
        var text = Path.Combine(_folder, "notes.exe");
        File.WriteAllText(text, "MZ is all this file has in common with a program.");

        Assert.Null(FileVersionResource.Read(text));
    }

    [Fact]
    public void A_program_cut_short_answers_null()
    {
        var whole = File.ReadAllBytes(Path.Combine(Environment.SystemDirectory, "kernel32.dll"));
        var cut = Path.Combine(_folder, "cut.dll");
        File.WriteAllBytes(cut, whole.Slice(..4096));

        Assert.Null(FileVersionResource.Read(cut));
    }

    [Fact]
    public void A_program_with_no_resource_table_answers_null()
    {
        var bytes = File.ReadAllBytes(Path.Combine(Environment.SystemDirectory, "kernel32.dll"));
        var pe = (int)BinaryPrimitives.ReadUInt32LittleEndian(bytes.AsSpan(0x3C));
        var directories = BinaryPrimitives.ReadUInt16LittleEndian(bytes.AsSpan(pe + 24)) == 0x20B ? 112 : 96;
        bytes.AsSpan(pe + 24 + directories + 2 * 8, 8).Clear();
        var bare = Path.Combine(_folder, "bare.dll");
        File.WriteAllBytes(bare, bytes);

        Assert.Null(FileVersionResource.Read(bare));
    }
}
