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
    public async Task An_unspellable_recorded_path_withholds_the_whole_walk_derived_offer()
    {
        // The condition is a registration whose recorded path could not be turned
        // into a path at all, so its claim is kept in the raw spelling Windows gave
        // and matches nothing the walk produces. The cached file it means is
        // therefore sitting in the candidate list unclaimed, and WHICH one cannot be
        // established, so none of them may be offered.
        File.WriteAllBytes(Path.Combine(_fakeInstallerDir, "one.msi"), new byte[] { 1 });
        File.WriteAllBytes(Path.Combine(_fakeInstallerDir, "two.msp"), new byte[] { 2, 2 });

        var query = Substitute.For<IInstallerQueryService>();
        query.GetRegisteredPackagesAsync(Arg.Any<IProgress<ScanProgressUpdate>?>(), Arg.Any<CancellationToken>())
            .Returns(new InstallerQueryResult(
                new List<RegisteredPackage>().AsReadOnly(),
                Census: new EnumerationCensus(PathNormalisationRefusedAtFullPathCount: 1)));

        var svc = new FileSystemScanService(query, null, _fakeInstallerDir);
        var result = await svc.ScanAsync();

        // Nothing offered, and the files accounted for rather than vanished: the
        // left-alone line is built from the withheld list, so a file counted nowhere
        // would make the two summary lines add up to less than the folder holds.
        Assert.Empty(result.RemovableFiles);
        Assert.Equal(2, result.WithheldFiles?.Count);
        Assert.Contains(result.WithheldFiles!, f => f.FileName == "one.msi");
        Assert.Contains(result.WithheldFiles!, f => f.FileName == "two.msp");
    }

    [Theory]
    [InlineData(1, 0, 0, 0)]
    [InlineData(0, 1, 0, 0)]
    [InlineData(0, 0, 1, 0)]
    [InlineData(0, 0, 0, 1)]
    public async Task Any_of_the_four_normalisation_refusals_withholds(
        int expansion, int prefixStrip, int fullPath, int embeddedNull)
    {
        // THE WITHHOLDING FIRES ON THE UNION, not on the one member that occurs in
        // practice. All four mean the same thing about the claim that came out of
        // it, and a rule keyed on one of them would let the other three through.
        //
        // THE FOURTH CASE IS WHY THIS IS A THEORY AND NOT ONE TEST. The embedded-null
        // member was added after the rule was written, and the rule then read three
        // named counters rather than their sum, so the count would have moved and the
        // offer would not. A case per member is what makes that visible instead of
        // arithmetic nobody re-reads.
        File.WriteAllBytes(Path.Combine(_fakeInstallerDir, "one.msi"), new byte[] { 1 });

        var query = Substitute.For<IInstallerQueryService>();
        query.GetRegisteredPackagesAsync(Arg.Any<IProgress<ScanProgressUpdate>?>(), Arg.Any<CancellationToken>())
            .Returns(new InstallerQueryResult(
                new List<RegisteredPackage>().AsReadOnly(),
                Census: new EnumerationCensus(
                    PathNormalisationRefusedAtExpansionCount: expansion,
                    PathNormalisationRefusedAtPrefixStripCount: prefixStrip,
                    PathNormalisationRefusedAtFullPathCount: fullPath,
                    PathNormalisationRefusedAtEmbeddedNullCount: embeddedNull)));

        var result = await new FileSystemScanService(query, null, _fakeInstallerDir).ScanAsync();

        Assert.Empty(result.RemovableFiles);
        Assert.Single(result.WithheldFiles!);
    }

    [Fact]
    public async Task A_clean_census_withholds_nothing_and_the_offer_stands()
    {
        // THE CONTROL FOR THE FOUR ABOVE. Identical fixture with every refusal count
        // at zero: without it, a withholding that fired unconditionally would pass
        // all four and nobody would know.
        File.WriteAllBytes(Path.Combine(_fakeInstallerDir, "one.msi"), new byte[] { 1 });

        var query = Substitute.For<IInstallerQueryService>();
        query.GetRegisteredPackagesAsync(Arg.Any<IProgress<ScanProgressUpdate>?>(), Arg.Any<CancellationToken>())
            .Returns(new InstallerQueryResult(
                new List<RegisteredPackage>().AsReadOnly(),
                Census: new EnumerationCensus()));

        var result = await new FileSystemScanService(query, null, _fakeInstallerDir).ScanAsync();

        Assert.Single(result.RemovableFiles);
        Assert.Equal("one.msi", result.RemovableFiles[0].FileName);
        Assert.Empty(result.WithheldFiles!);
    }

    [Fact]
    public async Task An_unspellable_recorded_path_does_not_withhold_a_superseded_row()
    {
        // The withholding covers the WALK-DERIVED half only. A superseded patch
        // reaches the offer from the registered set, judged on products through
        // registry keys read by product and patch code, and nothing on that path
        // reads a cached-package path. Withholding it here would cost a file for a
        // condition that has no bearing on it.
        var superseded = Path.Combine(_fakeInstallerDir, "superseded.msp");
        File.WriteAllBytes(superseded, new byte[] { 3, 3, 3 });

        var query = Substitute.For<IInstallerQueryService>();
        query.GetRegisteredPackagesAsync(Arg.Any<IProgress<ScanProgressUpdate>?>(), Arg.Any<CancellationToken>())
            .Returns(new InstallerQueryResult(
                new List<RegisteredPackage>
                {
                    new(superseded, "Product", "{P}", PatchState: 2, IsRemovable: true),
                }.AsReadOnly(),
                Census: new EnumerationCensus(PathNormalisationRefusedAtFullPathCount: 1)));

        var result = await new FileSystemScanService(query, null, _fakeInstallerDir).ScanAsync();

        Assert.Single(result.RemovableFiles);
        Assert.Equal("superseded.msp", result.RemovableFiles[0].FileName);
        Assert.Empty(result.WithheldFiles!);
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
