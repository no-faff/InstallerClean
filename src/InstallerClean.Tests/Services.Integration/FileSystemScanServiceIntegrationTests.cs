using NSubstitute;
using InstallerClean.Models;
using InstallerClean.Services;

namespace InstallerClean.Tests.Services.Integration;

public class FileSystemScanServiceIntegrationTests : IDisposable
{
    private readonly string _fakeInstallerDir =
        Path.Combine(Path.GetTempPath(), "ic-tests-" + Guid.NewGuid());

    public FileSystemScanServiceIntegrationTests()
    {
        Directory.CreateDirectory(_fakeInstallerDir);
        Directory.CreateDirectory(Path.Combine(_fakeInstallerDir, "nested"));
    }

    [Fact]
    public async Task Real_directory_enumeration_finds_msi_and_msp_across_subdirs()
    {
        File.WriteAllBytes(Path.Combine(_fakeInstallerDir, "a.msi"), new byte[] { 1, 2, 3 });
        File.WriteAllBytes(Path.Combine(_fakeInstallerDir, "b.msp"), new byte[] { 1, 2 });
        File.WriteAllBytes(Path.Combine(_fakeInstallerDir, "nested", "c.msi"), new byte[] { 9 });
        File.WriteAllBytes(Path.Combine(_fakeInstallerDir, "readme.txt"), new byte[] { 7 });

        var query = Substitute.For<IInstallerQueryService>();
        query.GetRegisteredPackagesAsync(Arg.Any<IProgress<ScanProgressUpdate>?>(), Arg.Any<CancellationToken>())
            .Returns(new List<RegisteredPackage>().AsReadOnly());

        var svc = new FileSystemScanService(query, null, _fakeInstallerDir);
        var result = await svc.ScanAsync();

        Assert.Equal(3, result.RemovableFiles.Count);
        Assert.Contains(result.RemovableFiles, f => f.FileName == "a.msi" && f.SizeBytes == 3);
        Assert.Contains(result.RemovableFiles, f => f.FileName == "b.msp" && f.SizeBytes == 2);
        Assert.Contains(result.RemovableFiles, f => f.FileName == "c.msi" && f.SizeBytes == 1);
        Assert.DoesNotContain(result.RemovableFiles, f => f.FileName == "readme.txt");
    }

    [Fact]
    public async Task Real_directory_skips_registered_files_case_insensitively()
    {
        File.WriteAllBytes(Path.Combine(_fakeInstallerDir, "KEPT.msi"), new byte[] { 1 });
        File.WriteAllBytes(Path.Combine(_fakeInstallerDir, "orphan.msi"), new byte[] { 2 });

        var query = Substitute.For<IInstallerQueryService>();
        query.GetRegisteredPackagesAsync(Arg.Any<IProgress<ScanProgressUpdate>?>(), Arg.Any<CancellationToken>())
            .Returns(new List<RegisteredPackage>
            {
                new(Path.Combine(_fakeInstallerDir, "kept.msi"), "Kept Product", "{K}"),
            }.AsReadOnly());

        var svc = new FileSystemScanService(query, null, _fakeInstallerDir);
        var result = await svc.ScanAsync();

        Assert.Single(result.RemovableFiles);
        Assert.Equal("orphan.msi", result.RemovableFiles[0].FileName);
    }

    public void Dispose()
    {
        try { if (Directory.Exists(_fakeInstallerDir)) Directory.Delete(_fakeInstallerDir, recursive: true); }
        catch { }
    }
}
