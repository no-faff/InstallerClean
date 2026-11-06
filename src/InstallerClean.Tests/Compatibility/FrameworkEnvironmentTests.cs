using System.Diagnostics;

namespace InstallerClean.Tests.Compatibility;

/// <summary>
/// The process's own id and exe path as .NET 10 reads them: the id Windows gives, and the full
/// path of the exe, read through a buffer that grows until the path fits, kept for the life
/// of the process.
/// </summary>
public class FrameworkEnvironmentTests
{
    [Fact]
    public void ProcessId_is_the_id_Windows_gives_this_process()
    {
        using var process = Process.GetCurrentProcess();

        Assert.Equal(process.Id, Environment.ProcessId);
    }

    [Fact]
    public void ProcessPath_is_the_full_path_of_this_process_exe_and_the_same_on_every_read()
    {
        using var process = Process.GetCurrentProcess();

        var first = Environment.ProcessPath;

        Assert.Equal(process.MainModule!.FileName, first);
        Assert.Same(first, Environment.ProcessPath);
    }

    [Fact]
    public void A_path_that_fits_the_first_buffer_is_read_once()
    {
        var sizes = new List<int>();

        var path = FrameworkEnvironment.ReadModulePath(Writing(@"C:\Tools\InstallerClean.exe", sizes), () => 0);

        Assert.Equal(@"C:\Tools\InstallerClean.exe", path);
        Assert.Equal(new[] { 260 }, sizes);
    }

    [Theory]
    [InlineData(259, new[] { 260 })]
    [InlineData(260, new[] { 260, 1024 })]
    [InlineData(1023, new[] { 260, 1024 })]
    [InlineData(1024, new[] { 260, 1024, 2048 })]
    [InlineData(5000, new[] { 260, 1024, 2048, 4096, 8192 })]
    public void A_path_the_buffer_cannot_hold_is_read_again_into_the_next_pooled_size(int length, int[] expectedSizes)
    {
        var sizes = new List<int>();
        var whole = @"C:\" + new string('a', length - 3);

        var path = FrameworkEnvironment.ReadModulePath(Writing(whole, sizes), () => 0);

        Assert.Equal(whole, path);
        Assert.Equal(expectedSizes, sizes);
    }

    [Fact]
    public void A_failed_read_throws_the_exception_for_the_last_error()
    {
        Assert.IsType<UnauthorizedAccessException>(
            Record.Exception(() => FrameworkEnvironment.ReadModulePath(_ => 0, () => 5)));
        Assert.IsType<FileNotFoundException>(
            Record.Exception(() => FrameworkEnvironment.ReadModulePath(_ => 0, () => 2)));
    }

    /// <summary>
    /// GetModuleFileNameW's contract: as much of <paramref name="path"/> as fits, and the
    /// buffer's length as the return where it does not fit with its terminating null.
    /// </summary>
    private static Func<char[], uint> Writing(string path, List<int> sizes) => buffer =>
    {
        sizes.Add(buffer.Length);
        if (path.Length < buffer.Length)
        {
            path.CopyTo(0, buffer, 0, path.Length);
            buffer[path.Length] = '\0';
            return (uint)path.Length;
        }

        path.CopyTo(0, buffer, 0, buffer.Length - 1);
        buffer[buffer.Length - 1] = '\0';
        return (uint)buffer.Length;
    };
}
