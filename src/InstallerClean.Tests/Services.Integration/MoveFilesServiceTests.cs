using InstallerClean.Services;

namespace InstallerClean.Tests.Services.Integration;

/// <summary>
/// One real-disk smoke test for MoveFilesService. The unit suite under
/// InstallerClean.Tests.Services covers the full behavioural contract
/// against MockFileSystem; this file exists to catch integration
/// surprises that only show on a real NTFS filesystem (case folding,
/// sharing modes, drive boundaries).
/// </summary>
public class MoveFilesServiceTests : IDisposable
{
    private readonly string _sourceDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
    private readonly string _destDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());

    public MoveFilesServiceTests()
    {
        Directory.CreateDirectory(_sourceDir);
        Directory.CreateDirectory(_destDir);
    }

    [Fact]
    public async Task MoveFilesAsync_moves_file_to_destination_on_real_filesystem()
    {
        var file = Path.Combine(_sourceDir, "test.msi");
        await File.WriteAllTextAsync(file, "content");

        var svc = new MoveFilesService(new System.IO.Abstractions.FileSystem());
        var results = await svc.MoveFilesAsync(new[] { file }, _destDir);

        Assert.Empty(results.Errors);
        Assert.False(File.Exists(file));
        Assert.True(File.Exists(Path.Combine(_destDir, "test.msi")));
    }

    public void Dispose()
    {
        if (Directory.Exists(_sourceDir)) Directory.Delete(_sourceDir, recursive: true);
        if (Directory.Exists(_destDir)) Directory.Delete(_destDir, recursive: true);
    }
}
