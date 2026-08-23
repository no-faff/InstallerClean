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

    [Theory]
    [InlineData(1, 0, 0, 0, 0)]
    [InlineData(0, 1, 0, 0, 0)]
    [InlineData(0, 0, 1, 0, 0)]
    [InlineData(0, 0, 0, 1, 0)]
    [InlineData(0, 0, 0, 0, 1)]
    public async Task Any_of_the_five_resolver_refusals_withholds(
        int notAPath, int noAncestor, int openRefused, int noFinalName, int faulted)
    {
        // THE SECOND POPULATION, ADDED IN 3.0.0, AND IT IS NOT A WIDER READING OF THE
        // FIRST. These are values that ARE paths and whose spelling the filesystem
        // would not settle, so the claim is compared in a form the walk never
        // produces and the file it means sits in this candidate list unclaimed,
        // exactly as an unspellable claim's does.
        //
        // TWO OF THE FIVE ARE ORDINARY MACHINE STATES and they are in this theory on
        // purpose. An unattached drive and a refused handle were counted apart and
        // acted on nothing until this release, on a trade-off the owner has since
        // ruled away: where the app can detect that one of its own checks did not
        // answer, it offers nothing that scan.
        //
        // A CASE PER MEMBER RATHER THAN ONE CASE OVER THE SUM, on the same reasoning
        // as the four above. A rule keyed on the members it happened to be written
        // for lets the rest through with a green build and a counter still reporting.
        File.WriteAllBytes(Path.Combine(_fakeInstallerDir, "one.msi"), new byte[] { 1 });

        var query = Substitute.For<IInstallerQueryService>();
        query.GetRegisteredPackagesAsync(Arg.Any<IProgress<ScanProgressUpdate>?>(), Arg.Any<CancellationToken>())
            .Returns(new InstallerQueryResult(
                new List<RegisteredPackage>().AsReadOnly(),
                Census: new EnumerationCensus(
                    // The attempts count travels with them and is not itself a
                    // reason to withhold; the control below holds it to that.
                    PathResolverAttemptCount: 1,
                    PathResolverNotAPathCount: notAPath,
                    PathResolverNoAncestorCount: noAncestor,
                    PathResolverOpenRefusedCount: openRefused,
                    PathResolverNoFinalNameCount: noFinalName,
                    PathResolverFaultedCount: faulted)));

        var result = await new FileSystemScanService(query, null, _fakeInstallerDir).ScanAsync();

        Assert.Empty(result.RemovableFiles);
        Assert.Single(result.WithheldFiles!);
    }

    [Fact]
    public async Task A_resolver_that_was_asked_and_answered_withholds_nothing()
    {
        // THE MUST-MISS CONTROL FOR THE THEORY ABOVE, and the one that matters most
        // of any control in this file. A machine whose recorded paths carry an 8dot3
        // spelling puts them to the resolver and gets clean answers, so its attempts
        // count is positive with five zeros behind it. A withholding that read the
        // attempts would empty the offer on precisely the machines the resolution
        // exists to help, and every case in the theory above would still pass.
        File.WriteAllBytes(Path.Combine(_fakeInstallerDir, "one.msi"), new byte[] { 1 });

        var query = Substitute.For<IInstallerQueryService>();
        query.GetRegisteredPackagesAsync(Arg.Any<IProgress<ScanProgressUpdate>?>(), Arg.Any<CancellationToken>())
            .Returns(new InstallerQueryResult(
                new List<RegisteredPackage>().AsReadOnly(),
                Census: new EnumerationCensus(PathResolverAttemptCount: 3)));

        var result = await new FileSystemScanService(query, null, _fakeInstallerDir).ScanAsync();

        Assert.Single(result.RemovableFiles);
        Assert.Equal("one.msi", result.RemovableFiles[0].FileName);
        Assert.Empty(result.WithheldFiles!);
    }

    [Fact]
    public async Task The_scan_reports_WHICH_BRANCH_emptied_the_offer_and_not_merely_that_it_is_empty()
    {
        // A HOST CANNOT RECOVER THIS FROM THE LISTS, which is why the flag exists. An
        // empty offer means either that the folder held nothing to offer or that a
        // rule about the records emptied it, and those are opposite things to tell
        // somebody. The completion screen picks its heading off this.
        File.WriteAllBytes(Path.Combine(_fakeInstallerDir, "one.msi"), new byte[] { 1 });

        var query = Substitute.For<IInstallerQueryService>();
        query.GetRegisteredPackagesAsync(Arg.Any<IProgress<ScanProgressUpdate>?>(), Arg.Any<CancellationToken>())
            .Returns(new InstallerQueryResult(
                new List<RegisteredPackage>().AsReadOnly(),
                Census: new EnumerationCensus(PathNormalisationRefusedAtFullPathCount: 1)));

        var result = await new FileSystemScanService(query, null, _fakeInstallerDir).ScanAsync();

        Assert.True(result.WalkOfferWithheldWholesale);
        Assert.Empty(result.RemovableFiles);
        Assert.Single(result.WithheldFiles!);
        Assert.Equal(1, result.WithheldTotalBytes);
    }

    [Fact]
    public async Task A_scan_that_withheld_nothing_wholesale_says_so()
    {
        // THE MUST-MISS CONTROL FOR THE FLAG, and without it a flag hard-wired to true
        // would satisfy the test above. This is the ordinary machine: a census with
        // nothing wrong on it, one candidate, and an offer.
        File.WriteAllBytes(Path.Combine(_fakeInstallerDir, "one.msi"), new byte[] { 1 });

        var query = Substitute.For<IInstallerQueryService>();
        query.GetRegisteredPackagesAsync(Arg.Any<IProgress<ScanProgressUpdate>?>(), Arg.Any<CancellationToken>())
            .Returns(new InstallerQueryResult(
                new List<RegisteredPackage>().AsReadOnly(),
                Census: new EnumerationCensus()));

        var result = await new FileSystemScanService(query, null, _fakeInstallerDir).ScanAsync();

        Assert.False(result.WalkOfferWithheldWholesale);
        Assert.Single(result.RemovableFiles);
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

    /// <summary>
    /// THE WIRE BETWEEN THE TWO ENDS, which nothing exercised. Both ends were
    /// already covered and neither could see this one. The enumeration's end is
    /// pinned by the query service's own fixtures: a real value goes through the
    /// real normalisation and the right counter moves while the other three do not.
    /// The scan's end is the pair of tests above, driven with a census written by
    /// hand. Between them sits one property read on a record built by merging the
    /// API loop's tally with the registry fallback's, and a scan whose census never
    /// reached the rule would look exactly like a machine with nothing to withhold.
    ///
    /// NEITHER END IS SIMULATED HERE. The census is the one a real enumeration built
    /// from a real refusal, and the scan is the real one reading it.
    /// </summary>
    [Fact]
    public async Task A_refusal_a_real_enumeration_counted_withholds_the_walk_derived_offer()
    {
        File.WriteAllBytes(Path.Combine(_fakeInstallerDir, "one.msi"), new byte[] { 1 });
        File.WriteAllBytes(Path.Combine(_fakeInstallerDir, "two.msp"), new byte[] { 2, 2 });

        var enumerated = await Enumerate("C:\\Windows\\Installer\\bad\0name.msi");

        // Asserted at both ends, so a failure says which half broke rather than only
        // that the two disagree.
        Assert.Equal(1, enumerated.Census.PathNormalisationRefusedTotal);

        var result = await new FileSystemScanService(
            QueryReturning(OnARealMachine(enumerated)), null, _fakeInstallerDir).ScanAsync();

        Assert.Empty(result.RemovableFiles);
        Assert.Equal(2, result.WithheldFiles!.Count);
    }

    [Fact]
    public async Task An_enumeration_that_spelled_everything_withholds_nothing()
    {
        // THE MUST-MISS CONTROL, and it guards the expensive direction: a wire that
        // withheld unconditionally would satisfy the test above on every machine
        // while costing a healthy one its whole offer. The two fixtures differ by one
        // character in one registered value.
        File.WriteAllBytes(Path.Combine(_fakeInstallerDir, "one.msi"), new byte[] { 1 });
        File.WriteAllBytes(Path.Combine(_fakeInstallerDir, "two.msp"), new byte[] { 2, 2 });

        var enumerated = await Enumerate(@"C:\Windows\Installer\ordinary.msi");

        Assert.Equal(0, enumerated.Census.PathNormalisationRefusedTotal);

        var result = await new FileSystemScanService(
            QueryReturning(OnARealMachine(enumerated)), null, _fakeInstallerDir).ScanAsync();

        Assert.Equal(2, result.RemovableFiles.Count);
        Assert.Empty(result.WithheldFiles!);
    }

    /// <summary>
    /// The enumeration's own answer, on a machine that could exist: one registration
    /// naming a file that really is in the folder this scan walks.
    ///
    /// WHY BOTH TESTS ABOVE NEED IT, AND WHY THEY FAILED FOR YEARS OF NIGHTS WITHOUT
    /// IT. Each drives a real enumeration holding ONE registration, whose recorded
    /// path is a real Windows path, and then scans a temp folder holding files. So no
    /// registration named anything in the walked folder while the walk still yielded
    /// candidates, which is exactly the machine the first correlation gate refuses:
    /// what Windows says it has and what the folder holds describe different places,
    /// and the scan is stopped rather than offering the folder as orphans. The guard
    /// is right and it was the fixture that could not exist, because on a real machine
    /// the folder being walked IS the cache those registrations name.
    ///
    /// THE ROW IS BUILT HERE RATHER THAN ENUMERATED, and that is deliberate rather
    /// than a shortcut. What these tests are about is the wire between the census a
    /// real enumeration builds and the withholding rule that reads it, and that census
    /// is the real one either way. What the extra row must do is name the walked folder
    /// in the SPELLING THAT FOLDER IS WALKED IN: a path taken through the enumeration
    /// is resolved against the filesystem, and a temp folder can be reached through an
    /// 8.3 alias, so an enumerated row could name the same folder in a spelling that
    /// does not compare equal to it and would fire the gate again on some machines and
    /// not others.
    /// </summary>
    private InstallerQueryResult OnARealMachine(InstallerQueryResult enumerated)
    {
        var claimed = Path.Combine(_fakeInstallerDir, "claimed.msi");
        File.WriteAllBytes(claimed, new byte[] { 4 });

        return enumerated with
        {
            Packages = enumerated.Packages
                .Append(new RegisteredPackage(claimed, "Another Program", "{B}"))
                .ToList()
                .AsReadOnly(),
        };
    }

    /// <summary>
    /// One product registered with the value given, enumerated by the real query
    /// service through the scriptable API fake, with no registry fallback. What
    /// comes back carries the census that enumeration actually built.
    /// </summary>
    private static async Task<InstallerQueryResult> Enumerate(string localPackage)
    {
        var msi = new FakeMsiApi();
        msi.AddProduct("{A}");
        msi.SetProductProperty("{A}", "LocalPackage", localPackage);
        return await new InstallerQueryService(msi, (_, _) =>
            new InstallerQueryService.FallbackRead(0, 0)).GetRegisteredPackagesAsync();
    }

    private static IInstallerQueryService QueryReturning(InstallerQueryResult result)
    {
        var q = Substitute.For<IInstallerQueryService>();
        q.GetRegisteredPackagesAsync(Arg.Any<IProgress<ScanProgressUpdate>?>(), Arg.Any<CancellationToken>())
            .Returns(result);
        return q;
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
