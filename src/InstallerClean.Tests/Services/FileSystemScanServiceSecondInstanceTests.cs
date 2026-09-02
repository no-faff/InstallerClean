using System.IO.Abstractions.TestingHelpers;
using InstallerClean.Models;
using InstallerClean.Services;
using NSubstitute;

namespace InstallerClean.Tests.Services;

/// <summary>
/// What a scan offers on a PC carrying the same program installed twice.
///
/// THE CONDITION AND WHY IT EMPTIES ANYTHING. A product installed under an instance
/// transform registers under a product code the transform produced, while the package
/// cached for it declares the base code. The last thing standing between a candidate no
/// registration claims and the offer is the declared-product screen, which reads a
/// product code OUT OF the candidate file and asks Windows about it, so on such a machine
/// that screen can be told there is no record while the second copy's own registration
/// still needs the file. Nothing in the scan can work out WHICH cached file belongs to
/// the second copy, so no walk-derived file is offered.
///
/// READ WHAT THESE FIXTURES SET UP AND NOT WHAT THEY ASSERT. Every one of them differs
/// from <see cref="An_ordinary_machine_keeps_every_file_it_would_have_offered"/> in the
/// census alone, which is exactly the pair the rule reads. Without that must-hit sitting
/// beside them, a scan that offered nothing to anybody would pass every other test here.
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
        Assert.False(result.WalkOfferWithheldWholesale);
        Assert.Empty(result.WithheldFiles!);
    }

    [Fact]
    public async Task A_product_installed_as_a_second_instance_of_itself_empties_the_walk_offer()
    {
        // ARM ONE. A positive reading from one product, and the file the machine would
        // otherwise have been offered is kept back instead.
        var result = await Scan(new EnumerationCensus(InstanceProductCount: 1));

        Assert.Empty(result.RemovableFiles);
        Assert.True(result.WalkOfferWithheldWholesale);
        Assert.Equal(Orphan, Assert.Single(result.WithheldFiles!).FullPath);
    }

    [Fact]
    public async Task A_product_that_would_not_answer_the_question_empties_it_on_the_same_terms()
    {
        // ARM TWO, AND IT IS THE ARM THE VERSION REMOVED IN 3.0.0 GOT WRONG. That one
        // read a positive only, so an enumeration that failed, was denied or stopped
        // short left the withholding unfired: the rule was armed by the machines that
        // answer and disarmed by the machines that do not. A question put and not
        // answered leaves the machine, as far as this rule can tell, in exactly the
        // state the positive reading describes.
        var result = await Scan(new EnumerationCensus(InstanceTypeUnreadableCount: 1));

        Assert.Empty(result.RemovableFiles);
        Assert.True(result.WalkOfferWithheldWholesale);
        Assert.Equal(Orphan, Assert.Single(result.WithheldFiles!).FullPath);
    }

    [Fact]
    public async Task A_second_instance_does_not_touch_a_superseded_row_the_records_cleared()
    {
        // THE NARROW RULE, PINNED, because the blunter one is the obvious thing to write
        // and nothing in the code would stop somebody writing it. The superseded half of
        // the offer is judged by REGISTERED product code and patch code: a second copy is
        // registered under its own code, so the per-product condition asks it like any
        // other product and a patch it still holds takes the offer away by that route.
        // The instance transform's peculiarity is confined to a code read out of a FILE,
        // and the only pass that does that is refused for patches at its own call site.
        //
        // Measured before it was written: with the cached patch's own Template naming
        // only the base code, the second copy still answered and the offer still went;
        // and on a machine where the holder is one neither the enumeration nor the
        // registry can name, the same file is wrongly offered whether or not any product
        // reads as a second instance, so emptying this half would protect nothing that
        // the walk-derived rule does not already protect.
        var result = await Scan(
            new EnumerationCensus(InstanceProductCount: 1),
            supersededOffer: true);

        Assert.Equal(Superseded, Assert.Single(result.RemovableFiles).FullPath);
        Assert.True(result.WalkOfferWithheldWholesale);
    }

    [Fact]
    public async Task A_wholesale_withholding_that_caught_nothing_does_not_report_itself()
    {
        // THE FLAG SAYS THE WITHHOLDING TOOK SOMETHING, NOT THAT THE BRANCH WAS TAKEN,
        // and the window depends on that: the screen it drives says the app "has held
        // back all N files it might otherwise have offered", which at zero is both
        // absurd and untrue. For this machine the all-clear is right, nothing in its
        // folder having gone unclaimed.
        //
        // A HOST COUNTING WithheldFiles WOULD BE DECIDING THIS OFF A LIST that three
        // separate decisions write to, so the guard would change meaning the moment any
        // one of their memberships moved and nothing would fail. This branch is the only
        // thing that knows what THIS withholding took.
        var result = await Scan(new EnumerationCensus(InstanceProductCount: 1), walkOrphan: false);

        Assert.Empty(result.RemovableFiles);
        Assert.False(result.WalkOfferWithheldWholesale);
        Assert.Empty(result.WithheldFiles!);
    }

    private const string Superseded = @"C:\Windows\Installer\superseded.msp";

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
    private static async Task<ScanResult> Scan(
        EnumerationCensus census,
        bool walkOrphan = true,
        bool supersededOffer = false)
    {
        var walked = new List<string> { Anchor };
        if (walkOrphan) walked.Add(Orphan);
        if (supersededOffer) walked.Add(Superseded);

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
