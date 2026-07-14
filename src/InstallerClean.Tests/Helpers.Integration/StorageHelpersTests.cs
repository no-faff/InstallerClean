using System.Text;
using InstallerClean.Helpers;

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
}
