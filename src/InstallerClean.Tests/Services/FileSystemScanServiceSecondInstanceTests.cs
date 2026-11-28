using System.IO.Abstractions.TestingHelpers;
using InstallerClean.Models;
using InstallerClean.Services;
using NSubstitute;

namespace InstallerClean.Tests.Services;

/// <summary>
/// What a scan offers on a PC that may carry the same program installed twice.
///
/// THE CONDITION AND WHAT IT HOLDS. A product installed under an instance transform
/// registers under a product code the transform produced, while the original package it
/// was installed from declares the base code and can be a file in the folder that the
/// copy's source list names. An installation the scan listed and could not rule out as
/// such a copy is marked, and the declared-product screen compares every file with the
/// packages it opens (<see cref="DeclaredProductCheckTests"/>). A product the registry
/// names that the scan could not ask about is on no list, so nothing compares a file
/// with it, and the whole walk-derived offer is held, installation packages and patch
/// files alike.
///
/// READ WHAT THESE FIXTURES SET UP AND NOT WHAT THEY ASSERT. Every one of them differs
/// from <see cref="An_ordinary_machine_keeps_every_file_it_would_have_offered"/> in the
/// census alone. Without that must-hit sitting beside them, a scan that offered nothing
/// to anybody would pass every test here that holds files back.
/// </summary>
public class FileSystemScanServiceSecondInstanceTests
{
    private const string CacheRoot = @"C:\Windows\Installer";
    private const string Orphan = @"C:\Windows\Installer\orphan.msi";

    /// <summary>
    /// One registration naming a file that is really there, on all three sides at once.
    /// The scan refuses outright when the records hold rows, no row names a file in the
    /// folder it walked and the walk still produced candidates, so without this every
    /// fixture here would go red at that gate rather than at its own subject. See the
    /// same constant in <see cref="FileSystemScanServicePathSpellingTests"/>.
    /// </summary>
    private const string Anchor = @"C:\Windows\Installer\anchor.msi";

    [Fact]
    public async Task An_ordinary_machine_keeps_every_file_it_would_have_offered()
    {
        // THE MUST-HIT, AND EVERYTHING ELSE IN THIS FILE IS WORTHLESS WITHOUT IT. The
        // census is at its default, which is the shape the overwhelming majority of
        // scans produce: every product answered, and every one of them answered that it
        // is an ordinary single-instance installation.
        var result = await Scan(new EnumerationCensus());

        Assert.Equal(Orphan, Assert.Single(result.RemovableFiles).FullPath);
        Assert.Equal(0, result.WithheldBy.WholesaleCount);
        Assert.Empty(result.WithheldFiles!);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task A_product_nobody_could_ask_about_holds_the_whole_walk_offer(bool unanswered)
    {
        // A code Windows would not say was installed, or a key whose name is no code that its
        // account's published lists do not account for: no question the screen could
        // put reaches that product, whether about a package it opens or a patch it holds.
        var census = unanswered
            ? new EnumerationCensus(UnansweredProductCount: 1)
            : new EnumerationCensus(UnparseableProductKeyNames: 1, UnparseableProductKeysUnreachable: 1);

        var result = await Scan(census, walkPatchOrphan: true);

        Assert.Empty(result.RemovableFiles);
        Assert.Equal(2, result.WithheldFiles!.Count);
        Assert.Equal(2, result.WithheldBy.WholesaleCount);
    }

    [Fact]
    public async Task A_key_whose_name_is_no_code_that_its_accounts_lists_account_for_holds_none_of_the_walk_offer_back()
    {
        // Every code that account's published lists name settled in its list's own account and
        // context, and every cached file the key records is claimed by path.
        var result = await Scan(new EnumerationCensus(UnparseableProductKeyNames: 1), walkPatchOrphan: true);

        Assert.Equal(new[] { Orphan, PatchOrphan }, result.RemovableFiles.Select(f => f.FullPath));
        Assert.Equal(0, result.WithheldBy.WholesaleCount);
        Assert.Empty(result.WithheldFiles!);
    }

    [Theory]
    [InlineData(nameof(EnumerationCensus.InstanceProductCount))]
    [InlineData(nameof(EnumerationCensus.InstanceTypeUnreadableCount))]
    public async Task A_second_copy_in_the_census_holds_nothing_back_on_its_own(string member)
    {
        // A product that answered it is a second instance of itself, or one whose answer
        // did not read. Each is an installation the scan listed with its mark, which the
        // screen acts on file by file, and no screen is injected here, so the count on
        // its own holds nothing back: the installation package and the patch file are
        // both offered.
        var census = member == nameof(EnumerationCensus.InstanceProductCount)
            ? new EnumerationCensus(InstanceProductCount: 1)
            : new EnumerationCensus(InstanceTypeUnreadableCount: 1);

        var result = await Scan(census, walkPatchOrphan: true);

        Assert.Equal(new[] { Orphan, PatchOrphan }, result.RemovableFiles.Select(f => f.FullPath));
        Assert.Equal(0, result.WithheldBy.WholesaleCount);
        Assert.Empty(result.WithheldFiles!);
    }

    [Fact]
    public async Task A_wholesale_withholding_does_not_touch_a_superseded_row_the_records_cleared()
    {
        // THE NARROW RULE, PINNED, because the blunter one is the obvious thing to write
        // and nothing in the code would stop somebody writing it. The superseded half of
        // the offer is judged by REGISTERED product code and patch code, and the
        // enumeration withholds it on a product nobody could ask about before the scan
        // sees it. The enumeration would not hand over a removable superseded row beside
        // this census; the fixture does, so that what it pins is the scan's own part:
        // its wholesale withholding runs over the walk's unclaimed candidates, which a
        // superseded row never is.
        var result = await Scan(
            new EnumerationCensus(UnansweredProductCount: 1),
            supersededOffer: true);

        Assert.Equal(Superseded, Assert.Single(result.RemovableFiles).FullPath);
        Assert.True(result.WithheldBy.WholesaleCount > 0);
    }

    [Fact]
    public async Task A_wholesale_withholding_that_caught_nothing_does_not_report_itself()
    {
        // THE WHOLESALE COUNT SAYS WHAT THE WITHHOLDING TOOK, NOT THAT THE BRANCH WAS
        // TAKEN. The gate fires here on a walk with nothing unclaimed, so the count is
        // zero and the withheld list is empty. The window's finished screen counts that
        // list, and at zero it gives the all-clear, which is right for this machine,
        // nothing in its folder having gone unclaimed.
        var result = await Scan(new EnumerationCensus(UnansweredProductCount: 1), walkOrphan: false);

        Assert.Empty(result.RemovableFiles);
        Assert.Equal(0, result.WithheldBy.WholesaleCount);
        Assert.Empty(result.WithheldFiles!);
    }

    private const string Superseded = @"C:\Windows\Installer\superseded.msp";

    /// <summary>A walked patch file no registration names.</summary>
    private const string PatchOrphan = @"C:\Windows\Installer\orphan.msp";

    /// <param name="census">
    /// The only thing that varies between the fixtures here. Handed through the query
    /// seam exactly as the enumeration would produce it.
    /// </param>
    /// <param name="walkOrphan">
    /// Whether the folder holds a file no registration names. False builds the machine
    /// whose walk gives the withholding nothing to catch.
    /// </param>
    /// <param name="supersededOffer">
    /// Adds a registered patch that reached this point still carrying its removable
    /// verdict, which is the half of the offer the walk-derived rule does not cover.
    /// </param>
    /// <param name="walkPatchOrphan">
    /// Whether the folder also holds a patch file no registration names.
    /// </param>
    private static async Task<ScanResult> Scan(
        EnumerationCensus census,
        bool walkOrphan = true,
        bool supersededOffer = false,
        bool walkPatchOrphan = false)
    {
        var walked = new List<string> { Anchor };
        if (walkOrphan) walked.Add(Orphan);
        if (supersededOffer) walked.Add(Superseded);
        if (walkPatchOrphan) walked.Add(PatchOrphan);

        var fs = new MockFileSystem();
        fs.AddDirectory(CacheRoot);
        foreach (var path in walked) fs.AddFile(path, new MockFileData("x"));

        var registered = new List<RegisteredPackage>
        {
            new(Anchor, "Product", "{code}"),
        };
        if (supersededOffer)
            registered.Add(new RegisteredPackage(
                Superseded, "Product", "{code}", PatchState: 2, IsRemovable: true));

        var query = Substitute.For<IInstallerQueryService>();
        query.GetRegisteredPackagesAsync(
                Arg.Any<IProgress<ScanProgressUpdate>?>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(new InstallerQueryResult(registered, Census: census)));

        return await new FileSystemScanService(
            query, fs, null, walked.ToArray(), CacheRoot, null)
            .ScanAsync();
    }
}
