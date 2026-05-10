using InstallerClean.Services;

namespace InstallerClean.Tests.Services.Integration;

public class MsiFileInfoServiceTests : IDisposable
{
    private readonly string _tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());

    public MsiFileInfoServiceTests()
    {
        Directory.CreateDirectory(_tempDir);
    }

    [Fact]
    public void GetSummaryInfo_returns_null_for_empty_path()
    {
        var svc = new MsiFileInfoService();

        var result = svc.GetSummaryInfo(string.Empty);

        Assert.Null(result);
    }

    [Fact]
    public void GetSummaryInfo_returns_null_for_nonexistent_file()
    {
        var svc = new MsiFileInfoService();
        var path = Path.Combine(_tempDir, "does_not_exist.msi");

        var result = svc.GetSummaryInfo(path);

        Assert.Null(result);
    }

    [Fact]
    public void GetSummaryInfo_returns_null_for_corrupt_file()
    {
        // Catches the entire family of "exists but isn't a valid
        // structured-storage MSI": empty, text, random bytes, large
        // zero-filled, etc. all hit the same MsiGetSummaryInformation
        // failure path.
        var svc = new MsiFileInfoService();
        var path = Path.Combine(_tempDir, "corrupt.msi");
        File.WriteAllText(path, "this is not a valid MSI file at all");

        var result = svc.GetSummaryInfo(path);

        Assert.Null(result);
    }

    [Fact]
    public void GetSummaryInfo_handles_locked_file_gracefully()
    {
        var svc = new MsiFileInfoService();
        var path = Path.Combine(_tempDir, "locked.msi");
        File.WriteAllText(path, "dummy content");

        // Lock the file with an exclusive handle so MsiGetSummaryInformation
        // sees a sharing violation.
        using var stream = new FileStream(path, FileMode.Open, FileAccess.ReadWrite, FileShare.None);

        var result = svc.GetSummaryInfo(path);

        Assert.Null(result);
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempDir))
            Directory.Delete(_tempDir, recursive: true);
    }
}
