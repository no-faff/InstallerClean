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
    public async Task Real_directory_enumeration_finds_root_msi_and_msp_only()
    {
        File.WriteAllBytes(Path.Combine(_fakeInstallerDir, "a.msi"), new byte[] { 1, 2, 3 });
        File.WriteAllBytes(Path.Combine(_fakeInstallerDir, "b.msp"), new byte[] { 1, 2 });
        // A file in a subdirectory: no longer a candidate (root-only scanning),
        // because a registered LocalPackage path never sits in a subfolder.
        File.WriteAllBytes(Path.Combine(_fakeInstallerDir, "nested", "c.msi"), new byte[] { 9 });
        File.WriteAllBytes(Path.Combine(_fakeInstallerDir, "readme.txt"), new byte[] { 7 });

        var query = Substitute.For<IInstallerQueryService>();
        query.GetRegisteredPackagesAsync(Arg.Any<IProgress<ScanProgressUpdate>?>(), Arg.Any<CancellationToken>())
            .Returns(new InstallerQueryResult(new List<RegisteredPackage>().AsReadOnly()));

        var svc = new FileSystemScanService(query, null, _fakeInstallerDir);
        var result = await svc.ScanAsync();

        Assert.Equal(2, result.RemovableFiles.Count);
        Assert.Contains(result.RemovableFiles, f => f.FileName == "a.msi" && f.SizeBytes == 3);
        Assert.Contains(result.RemovableFiles, f => f.FileName == "b.msp" && f.SizeBytes == 2);
        Assert.DoesNotContain(result.RemovableFiles, f => f.FileName == "c.msi");     // subdirectory
        Assert.DoesNotContain(result.RemovableFiles, f => f.FileName == "readme.txt"); // wrong extension
    }

    [Fact]
    public async Task Real_directory_skips_registered_files_case_insensitively()
    {
        File.WriteAllBytes(Path.Combine(_fakeInstallerDir, "KEPT.msi"), new byte[] { 1 });
        File.WriteAllBytes(Path.Combine(_fakeInstallerDir, "orphan.msi"), new byte[] { 2 });

        var query = Substitute.For<IInstallerQueryService>();
        query.GetRegisteredPackagesAsync(Arg.Any<IProgress<ScanProgressUpdate>?>(), Arg.Any<CancellationToken>())
            .Returns(new InstallerQueryResult(new List<RegisteredPackage>
            {
                new(Path.Combine(_fakeInstallerDir, "kept.msi"), "Kept Product", "{K}"),
            }.AsReadOnly()));

        var svc = new FileSystemScanService(query, null, _fakeInstallerDir);
        var result = await svc.ScanAsync();

        Assert.Single(result.RemovableFiles);
        Assert.Equal("orphan.msi", result.RemovableFiles[0].FileName);
    }

    [Fact]
    public async Task Real_directory_walk_matches_the_two_patterns_it_replaced()
    {
        // The walk is one pass over the folder filtered on the extension, where
        // it used to be a "*.msi" pass concatenated with a "*.msp" one. This is
        // the equivalence that swap rests on, asserted against the real matcher
        // rather than argued: the names below are the ones where a pattern and
        // an extension test could plausibly disagree.
        var names = new[]
        {
            "a.msi", "b.msp", "UPPER.MSI", "MiXeD.MsP", ".msi",
            "x.msix", "y.msi_old", "z.msi.bak", "readme.txt", "noext",
        };
        for (int i = 0; i < names.Length; i++)
            File.WriteAllBytes(Path.Combine(_fakeInstallerDir, names[i]), new byte[i + 1]);

        var options = new EnumerationOptions
        {
            RecurseSubdirectories = false,
            AttributesToSkip = FileAttributes.ReparsePoint,
            IgnoreInaccessible = true,
        };
        var byPattern = Directory.EnumerateFiles(_fakeInstallerDir, "*.msi", options)
            .Concat(Directory.EnumerateFiles(_fakeInstallerDir, "*.msp", options))
            .OrderBy(p => p, StringComparer.Ordinal)
            .ToList();

        var query = Substitute.For<IInstallerQueryService>();
        query.GetRegisteredPackagesAsync(Arg.Any<IProgress<ScanProgressUpdate>?>(), Arg.Any<CancellationToken>())
            .Returns(new InstallerQueryResult(new List<RegisteredPackage>().AsReadOnly()));

        var result = await new FileSystemScanService(query, null, _fakeInstallerDir).ScanAsync();
        var walked = result.RemovableFiles
            .Select(f => f.FullPath)
            .OrderBy(p => p, StringComparer.Ordinal)
            .ToList();

        Assert.Equal(byPattern, walked);

        // And the size on each row is the file's own, now that it is carried
        // through from the directory entry instead of being read again.
        foreach (var f in result.RemovableFiles)
            Assert.Equal(new FileInfo(f.FullPath).Length, f.SizeBytes);
    }

    public void Dispose()
    {
        try { if (Directory.Exists(_fakeInstallerDir)) Directory.Delete(_fakeInstallerDir, recursive: true); }
        catch { }
    }
}
