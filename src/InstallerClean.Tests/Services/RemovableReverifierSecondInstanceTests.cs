using InstallerClean.Models;
using InstallerClean.Services;
using NSubstitute;

namespace InstallerClean.Tests.Services;

/// <summary>
/// THE ACT-TIME HALF OF THE SCAN'S WHOLESALE WITHHOLDING.
///
/// The scan offers no walk-derived file on a machine whose recorded paths it could not
/// settle, or whose registry names a product it could not ask about. This pass re-runs
/// the whole enumeration immediately before a Move or a Delete and asks both questions
/// again, so on a machine that reached one of those states between the list appearing and
/// the button being pressed, the walk-derived files leave the batch as the scan would by
/// then have held them back. Nothing need be wrong with any file in it; the machine has
/// changed underneath it. A second copy the enumeration lists is not one of those
/// states: it is compared file by file by the declared-product screen, which
/// <see cref="RemovableReverifierWalkDerivedTests"/> drives.
///
/// IT DROPS THE WALK-DERIVED HALF AND NOT THE WHOLE BATCH. A superseded registration is
/// judged by its own row in the same enumeration, which takes the row's removable verdict
/// away there on either condition, and the pass drops the file on that row. A path no
/// registration names is the walk-derived half, and that is the test.
///
/// READ WHAT EACH FIXTURE SETS UP. They differ in the census alone, or in whether a
/// registration names the path, and nothing else.
/// </summary>
public class RemovableReverifierSecondInstanceTests
{
    private const string Orphan = @"C:\Windows\Installer\orphan.msi";
    private const string Superseded = @"C:\Windows\Installer\superseded.msp";
    private const string PatchOrphan = @"C:\Windows\Installer\orphan.msp";
    private const string Code = "{00000000-0000-0000-0000-000000000001}";

    private static RemovableReverifier Reverifier(IInstallerQueryService query) =>
        new(query);

    private static IInstallerQueryService Query(EnumerationCensus census, params RegisteredPackage[] pkgs)
    {
        var q = Substitute.For<IInstallerQueryService>();
        q.GetRegisteredPackagesAsync(Arg.Any<IProgress<ScanProgressUpdate>?>(), Arg.Any<CancellationToken>())
            .Returns(new InstallerQueryResult(pkgs.ToList().AsReadOnly(), Census: census));
        return q;
    }

    /// <summary>A superseded patch still carrying its removable verdict at act time.</summary>
    private static RegisteredPackage StillRemovable(string path) =>
        new(path, "Product", Code, PatchState: 2, IsRemovable: true);

    [Fact]
    public async Task An_ordinary_machine_keeps_its_whole_batch()
    {
        // THE MUST-HIT. Nothing below means anything without it: a pass that dropped
        // every candidate would satisfy every other test in this file.
        var svc = Reverifier(Query(new EnumerationCensus()));

        var result = await svc.ReverifyAsync(new[] { Orphan });

        Assert.Equal(Orphan, Assert.Single(result.Surviving));
        Assert.Empty(result.Dropped);
        Assert.Equal(0, result.Reasons.Total);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task A_product_nobody_could_ask_about_appearing_before_the_click_drops_the_walk_derived_batch(
        bool unanswered)
    {
        // The registry came to name a product the enumeration could not ask about between
        // the list appearing and the button being pressed. The scan would no longer offer
        // either file, so neither may the action.
        var census = unanswered
            ? new EnumerationCensus(UnansweredProductCount: 1)
            : new EnumerationCensus(UnparseableProductKeyNames: 1);
        var svc = Reverifier(Query(census));

        var result = await svc.ReverifyAsync(new[] { Orphan, PatchOrphan });

        Assert.Empty(result.Surviving);
        Assert.Equal(new[] { Orphan, PatchOrphan }, result.Dropped);
        Assert.Equal(2, result.Reasons.OwnershipUnestablished);
        Assert.Equal(2, result.Reasons.Total);
    }

    [Theory]
    [InlineData(nameof(EnumerationCensus.InstanceProductCount))]
    [InlineData(nameof(EnumerationCensus.InstanceTypeUnreadableCount))]
    public async Task A_second_copy_in_the_census_drops_nothing_on_its_own(string member)
    {
        // Each is an installation the enumeration listed with its mark, which the
        // declared-product screen acts on file by file. No screen is injected here, so
        // the count on its own drops nothing.
        var census = member == nameof(EnumerationCensus.InstanceProductCount)
            ? new EnumerationCensus(InstanceProductCount: 1)
            : new EnumerationCensus(InstanceTypeUnreadableCount: 1);
        var svc = Reverifier(Query(census));

        var result = await svc.ReverifyAsync(new[] { Orphan, PatchOrphan });

        Assert.Equal(new[] { Orphan, PatchOrphan }, result.Surviving);
        Assert.Equal(0, result.Reasons.Total);
    }

    [Fact]
    public async Task A_recorded_path_that_will_not_settle_drops_it_too()
    {
        // THE SECOND CONDITION. The scan withholds its whole walk offer on an unsettled
        // recorded path, and this pass re-applies that too. Asked of the census where the
        // members live, so a cause added to either question is re-applied without this
        // file being edited.
        var svc = Reverifier(Query(new EnumerationCensus(PathResolverOpenRefusedCount: 1)));

        var result = await svc.ReverifyAsync(new[] { Orphan });

        Assert.Equal(Orphan, Assert.Single(result.Dropped));
        Assert.Equal(1, result.Reasons.OwnershipUnestablished);
    }

    [Fact]
    public async Task A_superseded_row_still_removable_survives_the_same_machine()
    {
        // THE NARROW RULE AT ACT TIME, and the fixture that stops the blunt one being
        // written here by mistake. One batch, one file of each half, one machine
        // carrying the condition: the walk-derived file goes and the registered one
        // stays, because this pass's wholesale withholding does not reach a row judged by
        // product code. The enumeration takes such a row's verdict away itself on this
        // condition; the fixture leaves it removable so that what it pins is this pass's
        // own part.
        var svc = Reverifier(Query(
            new EnumerationCensus(UnansweredProductCount: 1),
            StillRemovable(Superseded)));

        var result = await svc.ReverifyAsync(new[] { Orphan, Superseded });

        Assert.Equal(Superseded, Assert.Single(result.Surviving));
        Assert.Equal(Orphan, Assert.Single(result.Dropped));
        Assert.Equal(1, result.Reasons.OwnershipUnestablished);
    }

    [Fact]
    public async Task A_files_own_finding_is_reported_ahead_of_the_machines()
    {
        // BOTH APPLY AND ONLY ONE CAUSE MAY BE REPORTED. A live claim on this file says
        // more than a fact about the machine, and the report the user reads names the
        // cause, so the stronger finding is the one that has to survive. Getting this
        // round the other way would tell somebody the app was unsure about a file it
        // had positively established a program still claims.
        var svc = Reverifier(Query(
            new EnumerationCensus(UnansweredProductCount: 1),
            new RegisteredPackage(Orphan, "Product", Code)));

        var result = await svc.ReverifyAsync(new[] { Orphan });

        Assert.Equal(Orphan, Assert.Single(result.Dropped));
        Assert.Equal(1, result.Reasons.Reclaimed);
        Assert.Equal(0, result.Reasons.OwnershipUnestablished);
    }
}
