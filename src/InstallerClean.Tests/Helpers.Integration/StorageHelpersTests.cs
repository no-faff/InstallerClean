using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using InstallerClean.Helpers;
using InstallerClean.Interop.Native;

namespace InstallerClean.Tests.Helpers.Integration;

public class StorageHelpersTests
{
    /// <summary>
    /// The append-only open is the whole reason crash.log entries cannot
    /// clobber each other, and its guarantee is a Win32 one (a write on a
    /// FILE_APPEND_DATA handle is resolved to the end of the file and performed
    /// as one step), so it can only be proved by running it. Eight threads
    /// racing on one file: every line has to survive. The same loop against a
    /// GENERIC_WRITE handle plus a seek to the end loses lines, because the
    /// seek and the write are two steps and two writers can be handed the same
    /// offset.
    /// </summary>
    [Fact]
    public async Task OpenAtomicAppend_does_not_lose_a_line_to_concurrent_writers()
    {
        const int writers = 8;
        const int linesEach = 25;
        var file = Path.Combine(Path.GetTempPath(), $"ic-append-{Guid.NewGuid():N}.log");
        // Long enough that a lost write is a lost line rather than a few
        // scrambled characters, and that the writers genuinely overlap.
        var filler = new string('x', 300);

        try
        {
            await Task.WhenAll(Enumerable.Range(0, writers).Select(w => Task.Run(() =>
            {
                for (var i = 0; i < linesEach; i++)
                {
                    using var handle = StorageHelpers.OpenAtomicAppend(file);
                    Assert.NotNull(handle);
                    using var fs = new FileStream(handle, FileAccess.Write, bufferSize: 0);
                    fs.Seek(0, SeekOrigin.End);
                    var bytes = Encoding.UTF8.GetBytes($"{w}:{i}:{filler}\n");
                    fs.Write(bytes, 0, bytes.Length);
                }
            })));

            var lines = File.ReadAllLines(file);
            Assert.Equal(writers * linesEach, lines.Length);
            Assert.All(lines, line => Assert.EndsWith(filler, line));
        }
        finally
        {
            File.Delete(file);
        }
    }

    [Fact]
    public void OpenAtomicAppend_creates_the_file_and_keeps_what_is_already_there()
    {
        var file = Path.Combine(Path.GetTempPath(), $"ic-append-{Guid.NewGuid():N}.log");

        try
        {
            using (var first = StorageHelpers.OpenAtomicAppend(file))
            {
                Assert.NotNull(first);
                using var fs = new FileStream(first, FileAccess.Write, bufferSize: 0);
                // Length is readable on an append-only handle: it is what
                // CrashLog uses to decide whether the file is fresh.
                Assert.Equal(0, fs.Length);
                var bytes = Encoding.UTF8.GetBytes("one\n");
                fs.Write(bytes, 0, bytes.Length);
            }

            using (var second = StorageHelpers.OpenAtomicAppend(file))
            {
                Assert.NotNull(second);
                using var fs = new FileStream(second, FileAccess.Write, bufferSize: 0);
                Assert.Equal(4, fs.Length);
                var bytes = Encoding.UTF8.GetBytes("two\n");
                fs.Write(bytes, 0, bytes.Length);
            }

            Assert.Equal(new[] { "one", "two" }, File.ReadAllLines(file));
        }
        finally
        {
            File.Delete(file);
        }
    }

    /// <summary>
    /// The names a file's folder entry holds, read through the real WIN32_FIND_DATA, against
    /// the short name GetShortPathName answers for the same file. The file's name is not in
    /// the 8.3 form, so on a volume that makes short names it has one of its own and the
    /// entry gives both back, the same whichever of the two the path spells; on a volume
    /// that makes none, the entry gives the name alone. A path naming nothing answers null.
    /// </summary>
    [Fact]
    public void GetNamesInFolder_reads_a_files_name_and_short_name_from_its_folder_entry()
    {
        const string Name = "InstallerClean folder entry test.msi";
        var folder = Path.Combine(Path.GetTempPath(), $"ic-names-{Guid.NewGuid():N}");
        Directory.CreateDirectory(folder);
        var file = Path.Combine(folder, Name);
        File.WriteAllBytes(file, []);

        try
        {
            var shortPath = ShortPathOf(file);
            var shortName = shortPath[(shortPath.LastIndexOf('\\') + 1)..];
            string[] expected = shortName == Name ? [Name] : [Name, shortName];

            Assert.Equal(expected, StorageHelpers.GetNamesInFolder(file));
            Assert.Equal(expected, StorageHelpers.GetNamesInFolder(Path.Combine(folder, shortName)));
            Assert.Null(StorageHelpers.GetNamesInFolder(Path.Combine(folder, "not there.msi")));
        }
        finally
        {
            Directory.Delete(folder, recursive: true);
        }
    }

    /// <summary>
    /// WIN32_FIND_DATAW as the API writes it: a DWORD, three FILETIMEs and four DWORDs, then
    /// the name's 260 characters at byte 44 and the short name's 14 at byte 564, 592 bytes in
    /// all. A name read from any other offset is another part of the entry. The struct is
    /// the same size in memory as the marshaller makes it, so the marshaller's offsets are the
    /// offsets in memory too.
    /// </summary>
    [Fact]
    public void The_folder_entry_is_laid_out_as_FindFirstFile_writes_it()
    {
        Assert.Equal(592, Unsafe.SizeOf<Kernel32.WIN32_FIND_DATA>());
        Assert.Equal(592, Marshal.SizeOf<Kernel32.WIN32_FIND_DATA>());
        Assert.Equal(44, (int)Marshal.OffsetOf<Kernel32.WIN32_FIND_DATA>(nameof(Kernel32.WIN32_FIND_DATA.cFileName)));
        Assert.Equal(564, (int)Marshal.OffsetOf<Kernel32.WIN32_FIND_DATA>(
            nameof(Kernel32.WIN32_FIND_DATA.cAlternateFileName)));
    }

    /// <summary>The path GetShortPathName answers for <paramref name="path"/>.</summary>
    private static string ShortPathOf(string path)
    {
        var buffer = new char[1024];
        var length = GetShortPathName(path, buffer, (uint)buffer.Length);
        Assert.InRange(length, 1u, (uint)buffer.Length - 1);
        return new string(buffer, 0, (int)length);
    }

    [DllImport("kernel32.dll", EntryPoint = "GetShortPathNameW", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern uint GetShortPathName(string lpszLongPath, [Out] char[] lpszShortPath, uint cchBuffer);

    [Fact]
    public void GetAvailableFreeSpace_returns_positive_for_current_drive()
    {
        var path = Path.GetTempPath();

        var result = StorageHelpers.GetAvailableFreeSpace(path);

        Assert.NotNull(result);
        Assert.True(result > 0);
    }

    [Fact]
    public void GetAvailableFreeSpace_returns_null_for_empty_path()
    {
        Assert.Null(StorageHelpers.GetAvailableFreeSpace(string.Empty));
    }

    [Fact]
    public void GetAvailableFreeSpace_returns_null_for_nonexistent_drive_letter()
    {
        // Pick a letter almost guaranteed not to be mapped
        var result = StorageHelpers.GetAvailableFreeSpace(@"Q:\nope\never");

        Assert.Null(result);
    }

    [Fact]
    public void GetAvailableFreeSpace_returns_null_for_unreachable_unc()
    {
        // Bogus UNC that will not resolve
        var result = StorageHelpers.GetAvailableFreeSpace(@"\\nonexistent-server-installerclean-test\share");

        Assert.Null(result);
    }

    /// <summary>
    /// The guard adds the flag that turns off the critical-error box to the thread's own mode,
    /// keeps the thread's other flags while it is in force, and puts the mode it found back on
    /// dispose. Run on a thread of its own, so the test thread's mode is never touched.
    /// </summary>
    [Fact]
    public void WithNoCriticalErrorBox_keeps_the_threads_other_flags_and_puts_its_mode_back()
    {
        // SEM_NOGPFAULTERRORBOX, a flag the guard does not set and must keep.
        const uint OtherFlag = 0x0002;
        uint before = 0, during = 0, after = 0;
        Exception? fault = null;

        var thread = new Thread(() =>
        {
            try
            {
                Kernel32.SetThreadErrorMode(OtherFlag, out _);
                before = Kernel32.GetThreadErrorMode();
                using (StorageHelpers.WithNoCriticalErrorBox())
                    during = Kernel32.GetThreadErrorMode();
                after = Kernel32.GetThreadErrorMode();
            }
            catch (Exception ex)
            {
                fault = ex;
            }
        });
        thread.Start();
        thread.Join();

        Assert.Null(fault);

        Assert.Equal(OtherFlag, before);
        Assert.Equal(OtherFlag | Kernel32.SEM_FAILCRITICALERRORS, during);
        Assert.Equal(OtherFlag, after);
    }

    /// <summary>
    /// The call each host makes as it starts adds the flag that turns off the critical-error box
    /// to the process's mode and keeps the process's other flags. The process's mode is put back
    /// afterwards, so no other test runs under a mode this one set.
    /// </summary>
    [Fact]
    public void TurnOffCriticalErrorBoxForTheProcess_adds_the_flag_and_keeps_the_others()
    {
        // SEM_NOGPFAULTERRORBOX, a flag the call does not set and must keep.
        const uint OtherFlag = 0x0002;
        var original = Kernel32.GetErrorMode();
        try
        {
            Kernel32.SetErrorMode(OtherFlag);

            StorageHelpers.TurnOffCriticalErrorBoxForTheProcess();

            Assert.Equal(OtherFlag | Kernel32.SEM_FAILCRITICALERRORS, Kernel32.GetErrorMode());
        }
        finally
        {
            Kernel32.SetErrorMode(original);
        }
    }
}
