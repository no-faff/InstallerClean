using InstallerClean.Helpers;
using InstallerClean.Services;

namespace InstallerClean.Tests.Helpers.Integration;

/// <summary>
/// The app's own Windows calls on a file and a folder at 300 characters: each one is handed
/// the full path with <c>\\?\</c> in front, so it answers whether or not the long-path
/// policy is on.
/// </summary>
/// <remarks>
/// Without the prefix these fail with error 3 only in a process where long paths are off
/// (the policy off, or no longPathAware manifest), so a test host with long paths on cannot
/// tell the two apart. The trip kit runs the same calls with long paths off.
/// </remarks>
public sealed class LongPathWindowsCallTests : IDisposable
{
    private readonly string _root = TestTemp.NewFolder("ic-long-");
    private readonly string _folder;
    private readonly string _file;

    public LongPathWindowsCallTests()
    {
        // Two folders, each well under the 255-character limit on one name, making 300.
        var first = Path.Combine(_root, new string('d', 120));
        _folder = Path.Combine(first, new string('e', 300 - first.Length - 1));
        _file = Path.Combine(_folder, "1a2b3c.msi");
        Directory.CreateDirectory(@"\\?\" + _folder);
        File.WriteAllBytes(@"\\?\" + _file, [1, 2, 3]);
    }

    public void Dispose()
    {
        TestTemp.Delete(_root);
    }

    [Fact]
    public void The_metadata_open_opens_a_file_and_a_folder_at_300_characters()
    {
        Assert.Equal(300, _folder.Length);

        using (var file = StorageHelpers.OpenForMetadata(_file, out var fileError))
            Assert.False(file.IsInvalid, $"error {fileError}");
        using (var folder = StorageHelpers.OpenForMetadata(_folder, out var folderError))
            Assert.False(folder.IsInvalid, $"error {folderError}");
    }

    [Fact]
    public void A_missing_file_at_300_characters_is_read_as_not_there()
    {
        using var handle = StorageHelpers.OpenForMetadata(Path.Combine(_folder, "missing.msi"), out var error);

        Assert.True(handle.IsInvalid);
        Assert.Equal(2, error);
    }

    [Fact]
    public void The_identity_of_a_file_at_300_characters_is_read()
    {
        Assert.Equal(FileIdentityRead.Read, new FileIdentityReader().ReadOutcome(_file, out _));
    }

    [Fact]
    public void The_times_of_a_file_at_300_characters_are_read()
    {
        Assert.Equal(FileTimesRead.Read, new FileTimesReader().ReadOutcome(_file, out _));
    }

    [Fact]
    public void The_names_in_the_folder_are_read_for_a_file_at_300_characters()
    {
        Assert.Equal("1a2b3c.msi", StorageHelpers.GetNamesInFolder(_file)?[0]);
    }

    [Fact]
    public void Free_space_is_read_for_a_folder_at_300_characters()
    {
        Assert.NotNull(StorageHelpers.GetAvailableFreeSpace(_folder));
    }

    [Fact]
    public void A_file_at_300_characters_is_written_through_the_atomic_open()
    {
        using (var handle = StorageHelpers.OpenAtomic(
                   Path.Combine(_folder, "new.txt"), FileAccess.Write, StorageHelpers.AtomicOpenMode.CreateNew))
            Assert.NotNull(handle);

        Assert.True(File.Exists(@"\\?\" + Path.Combine(_folder, "new.txt")));
    }

    [Fact]
    public void A_folder_at_300_characters_is_opened_as_a_folder()
    {
        using var handle = DirectoryGuard.OpenDirectoryFollow(_folder, out var error);

        Assert.True(handle is { IsInvalid: false }, $"error {error}");
    }
}
