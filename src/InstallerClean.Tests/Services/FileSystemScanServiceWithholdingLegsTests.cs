using System.IO.Abstractions.TestingHelpers;
using InstallerClean.Models;
using InstallerClean.Services;
using NSubstitute;

namespace InstallerClean.Tests.Services;

/// <summary>
/// The scan's own withholding gate, held to the list of legs a host prints under it.
///
/// WHAT THIS CATCHES THAT NOTHING ELSE DOES. WithholdingLegsTests holds
/// <c>WithholdingLegs.Any</c> to <c>WithholdingLegs.Fired</c>, which is a fact about
/// that pair and says nothing whatever about the scan. The scan is free to stop
/// calling either of them, or to write a condition of its own beside the call, and no
/// test over the pair can see it. That is a machine whose offer is withheld and whose
/// breakdown has nothing to say about why: a heading with nothing under it.
///
/// So this drives the real scan, one per member of the census, and asserts the flag
/// the hosts read implies a leg to explain it. The members are read off the type, so a
/// member added later is covered the day it lands rather than when somebody remembers.
///
/// THE IMPLICATION RUNS ONE WAY AND ONLY ONE. A gate that fires on a machine whose
/// walk turned up nothing to withhold sets no flag, because the flag is assigned from
/// what the withholding actually took. So legs without the flag is an ordinary state
/// and the flag without legs is the fault.
///
/// EVERY TEST HERE IS WITNESSED BY WINDOWS CI AND BY NOTHING ELSE. A scan walks a
/// folder, and the walk goes through the real filesystem whatever is injected, so
/// these cannot run on a Linux build. That is where the twenty in
/// FileSystemScanServiceIntegrationTests live too, and the suite runs there on every
/// push.
/// </summary>
public class FileSystemScanServiceWithholdingLegsTests
{
    private const string Folder = @"C:\Windows\Installer";

    public static TheoryData<string> EveryCensusMember()
    {
        var data = new TheoryData<string>();
        foreach (var p in typeof(EnumerationCensus).GetConstructors()
                     .OrderByDescending(c => c.GetParameters().Length).First().GetParameters())
            data.Add(p.Name!);
        return data;
    }

    [Theory]
    [MemberData(nameof(EveryCensusMember))]
    public async Task A_withheld_walk_offer_always_has_a_leg_to_explain_it(string member)
    {
        var result = await Scan(CensusWithOnly(member));

        if (!result.WalkOfferWithheldWholesale) return;

        Assert.True(result.WithholdingLegsFired.Count > 0,
            $"the offer was withheld wholesale on a census whose only member is {member}, "
            + "and no leg fired, so the breakdown would print a heading with nothing under it");
    }

    [Fact]
    public async Task A_scan_with_nothing_to_report_withholds_nothing_and_fires_no_leg()
    {
        // The floor for the theory above, which passes for the wrong reason on a census
        // that fires nothing: with no condition met at all the offer must survive, so a
        // run in which every member of that theory took the early return would be
        // visible as this test failing rather than as a clean sweep.
        var result = await Scan(default);

        Assert.False(result.WalkOfferWithheldWholesale);
        Assert.Empty(result.WithholdingLegsFired);
        Assert.Single(result.RemovableFiles);
    }

    [Fact]
    public async Task A_leg_that_fires_withholds_the_offer_and_is_reported()
    {
        // The other half, and the one that shows the theory can reach its assertion at
        // all: a census that does fire a leg withholds the offer, and the leg is on the
        // list a host would print.
        var result = await Scan(new EnumerationCensus(InstanceProductCount: 1));

        Assert.True(result.WalkOfferWithheldWholesale);
        Assert.Equal(
            new[] { WithholdingLeg.SecondInstanceNotRuledOut },
            result.WithholdingLegsFired);
        Assert.Empty(result.RemovableFiles);
    }

    private static EnumerationCensus CensusWithOnly(string member)
    {
        var ctor = typeof(EnumerationCensus).GetConstructors()
            .OrderByDescending(c => c.GetParameters().Length).First();
        return (EnumerationCensus)ctor.Invoke(ctor.GetParameters()
            .Select(p => p.Name == member ? (object)1 : p.DefaultValue!).ToArray());
    }

    /// <summary>
    /// One scan over a single unclaimed file. Unclaimed matters: the flag is assigned
    /// from what the withholding took, so a fixture whose walk found nothing to withhold
    /// would leave the flag false whatever the gate decided, and the assertion above
    /// would never be reached.
    /// </summary>
    private static Task<ScanResult> Scan(EnumerationCensus census)
    {
        var file = $@"{Folder}\unclaimed.msi";
        var fs = new MockFileSystem();
        fs.AddDirectory(Folder);
        fs.AddFile(file, new MockFileData(new byte[100]));

        var query = Substitute.For<IInstallerQueryService>();
        query.GetRegisteredPackagesAsync(
                Arg.Any<IProgress<ScanProgressUpdate>?>(), Arg.Any<CancellationToken>())
            .Returns(new InstallerQueryResult(Array.Empty<RegisteredPackage>(), Census: census));

        return new FileSystemScanService(query, fs, null, new[] { file }, null, null, null)
            .ScanAsync();
    }
}
