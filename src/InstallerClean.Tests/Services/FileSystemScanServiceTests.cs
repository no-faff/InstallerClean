using System.IO.Abstractions.TestingHelpers;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using InstallerClean.Models;
using InstallerClean.Services;

namespace InstallerClean.Tests.Services;

public class FileSystemScanServiceTests
{
    private static RegisteredPackage Registered(string path) =>
        new(path, "Test Product", "{00000000-0000-0000-0000-000000000001}");

    private static RegisteredPackage Superseded(string path) =>
        new(path, "Test Product", "{00000000-0000-0000-0000-000000000001}", PatchState: 2, IsRemovable: true);

    private static RegisteredPackage Obsoleted(string path) =>
        new(path, "Test Product", "{00000000-0000-0000-0000-000000000001}", PatchState: 4, IsRemovable: true);

    /// <summary>
    /// A superseded patch from a scan whose product enumeration was incomplete:
    /// the API called it removable, the scan withheld the verdict.
    /// </summary>
    private static RegisteredPackage Withheld(string path) =>
        new(path, "Test Product", "{00000000-0000-0000-0000-000000000001}",
            PatchState: 2, IsRemovable: false, RemovableWithheld: true);

    private static IInstallerQueryService QueryReturning(InstallerQueryResult result)
    {
        var q = Substitute.For<IInstallerQueryService>();
        q.GetRegisteredPackagesAsync(Arg.Any<IProgress<ScanProgressUpdate>?>(), Arg.Any<CancellationToken>())
            .Returns(result);
        return q;
    }

    [Fact]
    public async Task ScanAsync_never_offers_a_withheld_patch_for_removal()
    {
        const string patch = @"C:\Windows\Installer\withheld.msp";
        var query = QueryReturning(new InstallerQueryResult(
            new List<RegisteredPackage> { Withheld(patch) }.AsReadOnly(), UnreadableProductCount: 1));

        var fs = new MockFileSystem();
        fs.AddFile(patch, new MockFileData("x"));

        var result = await new FileSystemScanService(query, fs, new[] { patch }, null).ScanAsync();

        Assert.Empty(result.RemovableFiles);
        Assert.Single(result.RegisteredPackages);
        Assert.Equal(1, result.UnreadableProductCount);
    }

    [Fact]
    public async Task ScanAsync_keeps_a_withheld_patch_missing_from_disk_off_the_missing_from_disk_banner()
    {
        // The hazard in withholding by flipping IsRemovable: MissingNonRemovable
        // drives the "a future repair, update or uninstall could fail" banner. A
        // withheld patch whose file is already gone is the same benign end state
        // it was before the verdict was withheld, and firing that banner would
        // tell the user their machine has a problem it does not have.
        const string gone = @"C:\Windows\Installer\withheld-gone.msp";
        var query = QueryReturning(new InstallerQueryResult(
            new List<RegisteredPackage> { Withheld(gone) }.AsReadOnly(), UnreadableProductCount: 1));

        var result = await new FileSystemScanService(
            query, new MockFileSystem(), Array.Empty<string>(), null).ScanAsync();

        Assert.Equal(0, result.MissingNonRemovableCount);
        Assert.Equal(1, result.MissingRemovableCount);
    }

    [Fact]
    public async Task ScanAsync_still_counts_a_genuinely_needed_missing_file_as_missing()
    {
        // The other side of the branch above: a row that is non-removable because
        // Windows needs it, not because a verdict was withheld, still fires it.
        const string gone = @"C:\Windows\Installer\needed-gone.msi";
        var query = QueryReturning(new InstallerQueryResult(
            new List<RegisteredPackage> { Registered(gone) }.AsReadOnly()));

        var result = await new FileSystemScanService(
            query, new MockFileSystem(), Array.Empty<string>(), null).ScanAsync();

        Assert.Equal(1, result.MissingNonRemovableCount);
        Assert.Equal(0, result.MissingRemovableCount);
    }

    [Fact]
    public async Task ScanAsync_returns_files_not_in_registered_set()
    {
        var registered = new List<RegisteredPackage>
        {
            Registered(@"C:\Windows\Installer\aaa.msi"),
        };

        var mockQuery = Substitute.For<IInstallerQueryService>();
        mockQuery
            .GetRegisteredPackagesAsync(Arg.Any<IProgress<ScanProgressUpdate>?>(), Arg.Any<CancellationToken>())
            .Returns(new InstallerQueryResult(registered.AsReadOnly()));

        var fakeFiles = new[]
        {
            @"C:\Windows\Installer\aaa.msi",   // registered, should NOT appear
            @"C:\Windows\Installer\bbb.msi",   // orphaned, should appear
        };

        // The registered file is present on disk, so the correlation gate
        // (every registered package missing while files on disk are orphaned)
        // does not fire on this small scenario.
        var fs = new MockFileSystem();
        fs.AddFile(@"C:\Windows\Installer\aaa.msi", new MockFileData("x"));
        fs.AddFile(@"C:\Windows\Installer\bbb.msi", new MockFileData("x"));

        var svc = new FileSystemScanService(mockQuery, fs, fakeFiles, null);

        var result = await svc.ScanAsync();

        Assert.Single(result.RemovableFiles);
        Assert.Equal(@"C:\Windows\Installer\bbb.msi", result.RemovableFiles[0].FullPath);
    }

    [Fact]
    public async Task ScanAsync_scans_the_root_only_and_ignores_subdirectory_files()
    {
        var mockQuery = Substitute.For<IInstallerQueryService>();
        mockQuery
            .GetRegisteredPackagesAsync(Arg.Any<IProgress<ScanProgressUpdate>?>(), Arg.Any<CancellationToken>())
            .Returns(new InstallerQueryResult(new List<RegisteredPackage>().AsReadOnly()));

        // Only root-level files are candidates. A registered LocalPackage path
        // never sits in a subdirectory, so the API can say nothing about a file
        // there. A payload .msp under $PatchCache$ (the patch engine's baseline
        // copy) and any other subfolder file are both out of scope now.
        var fs = new MockFileSystem(new Dictionary<string, MockFileData>
        {
            [@"C:\Windows\Installer\bbb.msi"] = new("x"),                                     // root: a candidate
            [@"C:\Windows\Installer\$PatchCache$\Managed\abc\1.0.0\payload.msp"] = new("x"),  // subdir: not
            [@"C:\Windows\Installer\SomeVendor\leftover.msi"] = new("x"),                     // subdir: not
        });

        var svc = new FileSystemScanService(mockQuery, fs);

        var result = await svc.ScanAsync();

        Assert.Single(result.RemovableFiles);
        Assert.Equal(@"C:\Windows\Installer\bbb.msi", result.RemovableFiles[0].FullPath);
    }

    [Fact]
    public async Task ScanAsync_path_comparison_is_case_insensitive()
    {
        var registered = new List<RegisteredPackage>
        {
            Registered(@"C:\Windows\Installer\AAA.MSI"),
        };

        var mockQuery = Substitute.For<IInstallerQueryService>();
        mockQuery
            .GetRegisteredPackagesAsync(Arg.Any<IProgress<ScanProgressUpdate>?>(), Arg.Any<CancellationToken>())
            .Returns(new InstallerQueryResult(registered.AsReadOnly()));

        var fakeFiles = new[] { @"C:\Windows\Installer\aaa.msi" };

        var svc = new FileSystemScanService(mockQuery, fakeFiles);
        var result = await svc.ScanAsync();

        Assert.Empty(result.RemovableFiles);
    }

    [Fact]
    public async Task ScanAsync_registered_packages_contains_all_api_packages()
    {
        // One registered package present on disk, one missing: the missing one
        // must still be listed (registered packages are included regardless of
        // disk presence), while the present one keeps the correlation gate from
        // firing on this small scenario.
        var registered = new List<RegisteredPackage>
        {
            Registered(@"C:\Windows\Installer\aaa.msi"),  // present
            Registered(@"C:\Windows\Installer\bbb.msi"),  // missing on disk
        };

        var mockQuery = Substitute.For<IInstallerQueryService>();
        mockQuery
            .GetRegisteredPackagesAsync(Arg.Any<IProgress<ScanProgressUpdate>?>(), Arg.Any<CancellationToken>())
            .Returns(new InstallerQueryResult(registered.AsReadOnly()));

        var fs = new MockFileSystem();
        fs.AddFile(@"C:\Windows\Installer\aaa.msi", new MockFileData(new byte[100]));
        var fakeFiles = new[] { @"C:\Windows\Installer\ccc.msi" }; // orphan

        var svc = new FileSystemScanService(mockQuery, fs, fakeFiles, null);
        var result = await svc.ScanAsync();

        // Both API packages are included, even the one missing from disk.
        Assert.Equal(2, result.RegisteredPackages.Count);
        // Only the present package contributes bytes; the missing one is excluded.
        Assert.Equal(100, result.RegisteredTotalBytes);
        Assert.Equal(1, result.MissingNonRemovableCount);
        Assert.Single(result.RemovableFiles);
    }

    [Fact]
    public async Task ScanAsync_superseded_patches_appear_in_removable_list()
    {
        var registered = new List<RegisteredPackage>
        {
            Registered(@"C:\Windows\Installer\applied.msp"),
            Superseded(@"C:\Windows\Installer\superseded.msp"),
        };

        var mockQuery = Substitute.For<IInstallerQueryService>();
        mockQuery
            .GetRegisteredPackagesAsync(Arg.Any<IProgress<ScanProgressUpdate>?>(), Arg.Any<CancellationToken>())
            .Returns(new InstallerQueryResult(registered.AsReadOnly()));

        // Both registered files present on disk so the superseded entry
        // passes the on-disk existence guard.
        var fs = new MockFileSystem();
        fs.AddFile(@"C:\Windows\Installer\applied.msp", new MockFileData(new byte[100]));
        fs.AddFile(@"C:\Windows\Installer\superseded.msp", new MockFileData(new byte[200]));

        var svc = new FileSystemScanService(mockQuery, fs, Array.Empty<string>(), null);
        var result = await svc.ScanAsync();

        // The superseded patch should appear in RemovableFiles with Reason="Superseded"
        Assert.Single(result.RemovableFiles);
        Assert.Equal("Superseded", result.RemovableFiles[0].Reason);

        // The applied patch stays in RegisteredPackages
        Assert.Single(result.RegisteredPackages);
    }

    [Fact]
    public async Task ScanAsync_obsoleted_patches_use_distinct_reason_label()
    {
        // MSI PatchState 4 (obsoleted) is a different API state from 2
        // (superseded). The Reason column distinguishes them so a user
        // examining the orphan list sees the precise MSI lifecycle state.
        var registered = new List<RegisteredPackage>
        {
            Obsoleted(@"C:\Windows\Installer\obsoleted.msp"),
        };

        var mockQuery = Substitute.For<IInstallerQueryService>();
        mockQuery
            .GetRegisteredPackagesAsync(Arg.Any<IProgress<ScanProgressUpdate>?>(), Arg.Any<CancellationToken>())
            .Returns(new InstallerQueryResult(registered.AsReadOnly()));

        var fs = new MockFileSystem();
        fs.AddFile(@"C:\Windows\Installer\obsoleted.msp", new MockFileData(new byte[150]));

        var svc = new FileSystemScanService(mockQuery, fs, Array.Empty<string>(), null);
        var result = await svc.ScanAsync();

        Assert.Single(result.RemovableFiles);
        Assert.Equal("Obsoleted", result.RemovableFiles[0].Reason);
    }

    [Fact]
    public async Task ScanAsync_superseded_patches_missing_from_disk_counted_to_removable_bucket()
    {
        // MSI database lists a patch as superseded but the underlying
        // file is no longer on disk. The scan should count it against
        // MissingRemovableCount (benign: Windows considers the patch
        // removable already) and leave it out of RemovableFiles so a
        // subsequent Delete or Move does not fail with MissingSourceFile.
        var registered = new List<RegisteredPackage>
        {
            Superseded(@"C:\Windows\Installer\ghost.msp"),
        };

        var mockQuery = Substitute.For<IInstallerQueryService>();
        mockQuery
            .GetRegisteredPackagesAsync(Arg.Any<IProgress<ScanProgressUpdate>?>(), Arg.Any<CancellationToken>())
            .Returns(new InstallerQueryResult(registered.AsReadOnly()));

        // Empty MockFileSystem: ghost.msp is registered but not present.
        var fs = new MockFileSystem();

        var svc = new FileSystemScanService(mockQuery, fs, Array.Empty<string>(), null);
        var result = await svc.ScanAsync();

        Assert.Empty(result.RemovableFiles);
        Assert.Empty(result.RegisteredPackages);
        Assert.Equal(1, result.MissingRemovableCount);
        Assert.Equal(0, result.MissingNonRemovableCount);
        Assert.Equal(1, result.MissingFromDiskCount);
    }

    [Fact]
    public async Task ScanAsync_non_removable_missing_from_disk_counted_to_non_removable_bucket()
    {
        // A registered, non-removable package (a current product, not a
        // superseded patch) whose file has gone missing from disk. The
        // load-bearing condition for the missing-from-disk banner: an
        // API-claimed file Windows still needs is gone, and a future
        // install / uninstall / patch will fail.
        var registered = new List<RegisteredPackage>
        {
            Registered(@"C:\Windows\Installer\ghost.msi"),
        };

        var mockQuery = Substitute.For<IInstallerQueryService>();
        mockQuery
            .GetRegisteredPackagesAsync(Arg.Any<IProgress<ScanProgressUpdate>?>(), Arg.Any<CancellationToken>())
            .Returns(new InstallerQueryResult(registered.AsReadOnly()));

        var fs = new MockFileSystem();

        var svc = new FileSystemScanService(mockQuery, fs, Array.Empty<string>(), null);
        var result = await svc.ScanAsync();

        Assert.Empty(result.RemovableFiles);
        Assert.Single(result.RegisteredPackages);
        Assert.Equal(1, result.MissingNonRemovableCount);
        Assert.Equal(0, result.MissingRemovableCount);
        Assert.Equal(1, result.MissingFromDiskCount);
    }

    [Fact]
    public async Task ScanAsync_registry_fallback_entries_are_not_removable()
    {
        // Simulate a registry-fallback entry: PatchState=0, IsRemovable=false
        var registered = new List<RegisteredPackage>
        {
            new(@"C:\Windows\Installer\fallback.msi", "", ""),
        };

        var mockQuery = Substitute.For<IInstallerQueryService>();
        mockQuery
            .GetRegisteredPackagesAsync(Arg.Any<IProgress<ScanProgressUpdate>?>(), Arg.Any<CancellationToken>())
            .Returns(new InstallerQueryResult(registered.AsReadOnly()));

        var fakeFiles = Array.Empty<string>();

        var svc = new FileSystemScanService(mockQuery, fakeFiles);
        var result = await svc.ScanAsync();

        // Fallback entries (PatchState=0, IsRemovable=false) stay in registered, not removable
        Assert.Empty(result.RemovableFiles);
        Assert.Single(result.RegisteredPackages);
    }

    [Fact]
    public async Task ScanAsync_handles_10000_orphaned_files()
    {
        var mockQuery = Substitute.For<IInstallerQueryService>();
        mockQuery
            .GetRegisteredPackagesAsync(Arg.Any<IProgress<ScanProgressUpdate>?>(), Arg.Any<CancellationToken>())
            .Returns(new InstallerQueryResult(new List<RegisteredPackage>().AsReadOnly()));

        var fakeFiles = Enumerable.Range(0, 10_000)
            .Select(i => $@"C:\Windows\Installer\orphan{i:D5}.msi")
            .ToArray();

        var svc = new FileSystemScanService(mockQuery, fakeFiles);
        var result = await svc.ScanAsync();

        Assert.Equal(10_000, result.RemovableFiles.Count);
        Assert.Empty(result.RegisteredPackages);
    }

    [Fact]
    public async Task ScanAsync_handles_10000_files_with_mixed_registered_and_orphaned()
    {
        // 5000 registered, 5000 orphaned
        var registered = Enumerable.Range(0, 5_000)
            .Select(i => Registered($@"C:\Windows\Installer\reg{i:D5}.msi"))
            .ToList();

        var mockQuery = Substitute.For<IInstallerQueryService>();
        mockQuery
            .GetRegisteredPackagesAsync(Arg.Any<IProgress<ScanProgressUpdate>?>(), Arg.Any<CancellationToken>())
            .Returns(new InstallerQueryResult(registered.AsReadOnly()));

        var fakeFiles = Enumerable.Range(0, 5_000)
            .Select(i => $@"C:\Windows\Installer\reg{i:D5}.msi")
            .Concat(Enumerable.Range(0, 5_000)
                .Select(i => $@"C:\Windows\Installer\orphan{i:D5}.msi"))
            .ToArray();

        // The 5000 registered packages are present on disk, so the correlation
        // gate does not fire (it would if every registered package were missing).
        var fs = new MockFileSystem();
        for (int i = 0; i < 5_000; i++)
            fs.AddFile($@"C:\Windows\Installer\reg{i:D5}.msi", new MockFileData("x"));

        var svc = new FileSystemScanService(mockQuery, fs, fakeFiles, null);
        var result = await svc.ScanAsync();

        Assert.Equal(5_000, result.RemovableFiles.Count);
        Assert.Equal(5_000, result.RegisteredPackages.Count);
    }

    [Fact]
    public async Task ScanAsync_handles_cancellation_during_large_scan()
    {
        var mockQuery = Substitute.For<IInstallerQueryService>();
        mockQuery
            .GetRegisteredPackagesAsync(Arg.Any<IProgress<ScanProgressUpdate>?>(), Arg.Any<CancellationToken>())
            .Returns(new InstallerQueryResult(new List<RegisteredPackage>().AsReadOnly()));

        var fakeFiles = Enumerable.Range(0, 10_000)
            .Select(i => $@"C:\Windows\Installer\orphan{i:D5}.msi")
            .ToArray();

        var cts = new CancellationTokenSource();
        cts.Cancel(); // cancel immediately

        var svc = new FileSystemScanService(mockQuery, fakeFiles);
        await Assert.ThrowsAsync<OperationCanceledException>(
            () => svc.ScanAsync(cancellationToken: cts.Token));
    }

    [Fact]
    public async Task ScanAsync_ignores_non_msi_msp_files()
    {
        var mockQuery = Substitute.For<IInstallerQueryService>();
        mockQuery
            .GetRegisteredPackagesAsync(Arg.Any<IProgress<ScanProgressUpdate>?>(), Arg.Any<CancellationToken>())
            .Returns(new InstallerQueryResult(new List<RegisteredPackage>().AsReadOnly()));

        var fakeFiles = new[]
        {
            @"C:\Windows\Installer\legit.msi",
            @"C:\Windows\Installer\patch.msp",
            @"C:\Windows\Installer\readme.txt",
            @"C:\Windows\Installer\data.dat",
            @"C:\Windows\Installer\script.exe",
        };

        var svc = new FileSystemScanService(mockQuery, fakeFiles);
        var result = await svc.ScanAsync();

        Assert.Equal(2, result.RemovableFiles.Count);
        Assert.All(result.RemovableFiles, f =>
            Assert.True(f.FullPath.EndsWith(".msi") || f.FullPath.EndsWith(".msp")));
    }

    [Fact]
    public async Task ScanAsync_query_service_throws_propagates_exception()
    {
        var mockQuery = Substitute.For<IInstallerQueryService>();
        mockQuery
            .GetRegisteredPackagesAsync(Arg.Any<IProgress<ScanProgressUpdate>?>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(new UnauthorizedAccessException("Access denied"));

        var svc = new FileSystemScanService(mockQuery, Array.Empty<string>());

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => svc.ScanAsync());
    }

    [Fact]
    public async Task ScanAsync_refuses_a_collapsed_correlation()
    {
        // The collapse signature: every registered (non-removable) package is
        // missing from disk while the walk still yields orphan candidates. A
        // path-form mismatch, a collapsed enumeration or the wrong folder all
        // produce it; no healthy machine does. The scan must refuse rather than
        // offer the whole cache for removal.
        var registered = Enumerable.Range(0, 30)
            .Select(i => Registered($@"C:\Windows\Installer\reg{i:D2}.msi"))
            .ToList();

        var mockQuery = Substitute.For<IInstallerQueryService>();
        mockQuery
            .GetRegisteredPackagesAsync(Arg.Any<IProgress<ScanProgressUpdate>?>(), Arg.Any<CancellationToken>())
            .Returns(new InstallerQueryResult(registered.AsReadOnly()));

        // Empty filesystem: none of the registered files exist on disk.
        var fs = new MockFileSystem();
        var orphans = new[] { @"C:\Windows\Installer\o1.msi", @"C:\Windows\Installer\o2.msi" };

        var svc = new FileSystemScanService(mockQuery, fs, orphans, null);

        await Assert.ThrowsAsync<LocalisedInvalidOperationException>(() => svc.ScanAsync());
    }

    [Fact]
    public async Task ScanAsync_does_not_refuse_when_some_registered_packages_are_present()
    {
        // The same shape as the collapse test but with a single registered
        // package present on disk: the correlation held for at least one, so the
        // gate must not fire and the orphans are classified normally.
        var registered = Enumerable.Range(0, 30)
            .Select(i => Registered($@"C:\Windows\Installer\reg{i:D2}.msi"))
            .ToList();

        var mockQuery = Substitute.For<IInstallerQueryService>();
        mockQuery
            .GetRegisteredPackagesAsync(Arg.Any<IProgress<ScanProgressUpdate>?>(), Arg.Any<CancellationToken>())
            .Returns(new InstallerQueryResult(registered.AsReadOnly()));

        var fs = new MockFileSystem();
        fs.AddFile(@"C:\Windows\Installer\reg00.msi", new MockFileData("x")); // one present
        var orphans = new[] { @"C:\Windows\Installer\o1.msi", @"C:\Windows\Installer\o2.msi" };

        var svc = new FileSystemScanService(mockQuery, fs, orphans, null);
        var result = await svc.ScanAsync();

        Assert.Equal(2, result.RemovableFiles.Count);
        Assert.Equal(29, result.MissingNonRemovableCount);
    }
}
