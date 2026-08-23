using InstallerClean.Models;
using InstallerClean.Services;
using NSubstitute;

namespace InstallerClean.Tests.Services;

/// <summary>
/// THE ACT-TIME HALF OF THE SCAN'S WHOLESALE WITHHOLDING, which until 3.0.0 did not
/// exist at all.
///
/// The scan offers no walk-derived file on a machine whose recorded paths it could not
/// settle, or one carrying the same program installed twice. This pass re-runs the whole
/// enumeration immediately before a Move or a Delete and never asked either question, so
/// a machine that reached one of those states between the list appearing and the button
/// being pressed acted on a batch the scan itself would by then have refused. Nothing was
/// wrong with any file in it; the machine had changed underneath it.
///
/// IT DROPS THE WALK-DERIVED HALF AND NOT THE WHOLE BATCH, which is what the version
/// removed in 3.0.0 did. That was right when the whole offer was walk-derived. A
/// superseded registration is offered beside it now, judged by product code and patch
/// code and untouched by either condition, so refusing those as well would keep back
/// files the same scan would still offer a moment later. A path no registration names is
/// the walk-derived half, and that is the test.
///
/// READ WHAT EACH FIXTURE SETS UP. They differ in the census alone, or in whether a
/// registration names the path, and nothing else.
/// </summary>
public class RemovableReverifierSecondInstanceTests
{
    private const string Orphan = @"C:\Windows\Installer\orphan.msi";
    private const string Superseded = @"C:\Windows\Installer\superseded.msp";
    private const string Code = "{00000000-0000-0000-0000-000000000001}";

    private static RemovableReverifier Reverifier(IInstallerQueryService query) =>
        new(query, Substitute.For<InstallerClean.Interop.IMsiApi>());

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

    [Fact]
    public async Task A_second_instance_appearing_before_the_click_drops_the_walk_derived_batch()
    {
        // The machine gained a product installed as a second instance of itself between
        // the list appearing and the button being pressed. The scan would no longer offer
        // this file, so neither may the action.
        var svc = Reverifier(Query(new EnumerationCensus(InstanceProductCount: 1)));

        var result = await svc.ReverifyAsync(new[] { Orphan });

        Assert.Empty(result.Surviving);
        Assert.Equal(Orphan, Assert.Single(result.Dropped));
        Assert.Equal(1, result.Reasons.OwnershipUnestablished);
        Assert.Equal(1, result.Reasons.Total);
    }

    [Fact]
    public async Task A_question_that_could_not_be_answered_drops_it_on_the_same_terms()
    {
        // Arm two at act time. Not knowing withholds exactly as knowing does, or the
        // rule is armed by the machines that answer and disarmed by the machines that
        // do not.
        var svc = Reverifier(Query(new EnumerationCensus(InstanceTypeUnreadableCount: 1)));

        var result = await svc.ReverifyAsync(new[] { Orphan });

        Assert.Equal(Orphan, Assert.Single(result.Dropped));
        Assert.Equal(1, result.Reasons.OwnershipUnestablished);
    }

    [Fact]
    public async Task A_recorded_path_that_will_not_settle_drops_it_too()
    {
        // THE SECOND CONDITION, AND IT PREDATES THIS WORK. The scan has emptied its walk
        // offer on an unsettled recorded path since 3.0.0 and this pass never re-applied
        // that either, so the gap being closed here is wider than the condition that
        // exposed it. Asked of the census where the members live, so a cause added to
        // either question is re-applied without this file being edited.
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
        // stays, because the condition cannot reach a row judged by product code.
        var svc = Reverifier(Query(
            new EnumerationCensus(InstanceProductCount: 1),
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
            new EnumerationCensus(InstanceProductCount: 1),
            new RegisteredPackage(Orphan, "Product", Code)));

        var result = await svc.ReverifyAsync(new[] { Orphan });

        Assert.Equal(Orphan, Assert.Single(result.Dropped));
        Assert.Equal(1, result.Reasons.Reclaimed);
        Assert.Equal(0, result.Reasons.OwnershipUnestablished);
    }
}
