using System.IO.Abstractions;
using InstallerClean.Helpers;
using InstallerClean.Models;
using InstallerClean.Resources;
using InstallerClean.Services;
using InstallerClean.Tests.Services;
using NSubstitute;

namespace InstallerClean.Tests.Helpers;

/// <summary>
/// The arithmetic behind the splash's progress bar.
///
/// THE WHOLE-RANGE TEST IS THE ONE TO KEEP. Every other property here holds just
/// as well of a bar whose top band is never drawn, because each of them asks
/// whether something went wrong and none of them asks whether everything the bar
/// has was used.
/// </summary>
public class ScanProgressFillTests
{
    private const double Floor = ScanProgressFill.FloorPercent;
    private const double Ceiling = ScanProgressFill.CeilingPercent;

    /// <summary>
    /// The scan's six milestones divide floor to ceiling into five bands, one per
    /// gap between them. Spelled here rather than read off the class, so a change
    /// to the division has to be made in both places and is seen.
    /// </summary>
    private const double Band = (Ceiling - Floor) / 5;

    [Fact]
    public void The_six_milestones_walk_the_whole_range_in_even_steps()
    {
        var fill = new ScanProgressFill();
        var floors = new[]
        {
            fill.AtMilestone(), fill.AtMilestone(), fill.AtMilestone(),
            fill.AtMilestone(), fill.AtMilestone(), fill.AtMilestone(),
        };

        // The first opens on the floor the host has already set, and the last
        // opens on the ceiling with the scan's result to show.
        Assert.Equal(Floor, floors[0], 10);
        Assert.Equal(Ceiling, floors[5], 10);

        // Even steps, because the scan cannot say which phase will cost the most
        // and a share handed out on a guess is a guess drawn on the screen.
        for (var i = 1; i < floors.Length; i++)
            Assert.Equal(Band, floors[i] - floors[i - 1], 10);
    }

    [Fact]
    public void A_phase_reporting_a_total_fills_its_band_in_proportion()
    {
        var fill = new ScanProgressFill();
        fill.AtMilestone();
        var bandFloor = fill.AtMilestone();

        Assert.Equal(bandFloor + Band * 0.25, fill.AtTicker(25, 100), 10);
        Assert.Equal(bandFloor + Band * 0.50, fill.AtTicker(50, 100), 10);
        Assert.Equal(bandFloor + Band * 0.75, fill.AtTicker(75, 100), 10);
    }

    [Fact]
    public void The_last_item_of_a_phase_lands_where_the_next_milestone_opens()
    {
        // So the phase hands over with no step in the bar: the milestone that
        // follows asks for the value the ticker has already reached.
        var fill = new ScanProgressFill();
        fill.AtMilestone();
        fill.AtMilestone();

        var atLastItem = fill.AtTicker(1883, 1883);
        Assert.Equal(atLastItem, fill.AtMilestone(), 10);
    }

    [Fact]
    public void A_phase_reporting_no_total_approaches_its_band_without_reaching_it()
    {
        var fill = new ScanProgressFill();
        fill.AtMilestone();
        var bandFloor = fill.AtMilestone();
        var bandTop = bandFloor + Band;

        var last = bandFloor;
        for (var i = 1; i <= 5_000; i++)
        {
            var now = fill.AtTicker(i, 0);
            Assert.True(now >= last, $"the fill fell from {last} to {now} at update {i}");
            Assert.True(now < bandTop, $"update {i} reached {now}, which is the next band");
            last = now;
        }

        // And it travels most of the band, so a phase that cannot say how long it
        // is still leaves a bar that has visibly moved.
        Assert.True(last > bandFloor + Band * 0.9, $"only reached {last} of {bandTop}");
    }

    [Fact]
    public void A_position_past_its_total_stops_at_the_end_of_its_band()
    {
        var fill = new ScanProgressFill();
        fill.AtMilestone();
        var bandFloor = fill.AtMilestone();
        Assert.Equal(bandFloor + Band, fill.AtTicker(5_000, 100), 10);
    }

    [Fact]
    public void The_fill_never_goes_backwards()
    {
        var fill = new ScanProgressFill();
        fill.AtMilestone();
        fill.AtMilestone();
        var high = fill.AtTicker(100, 100);

        Assert.Equal(high, fill.At(0), 10);
        Assert.Equal(high, fill.AtTicker(1, 100), 10);
    }

    [Fact]
    public void A_phase_beyond_the_bands_rests_on_the_ceiling()
    {
        // A phase added to the scan with no band left to put it in stays under
        // the host's closing step rather than running past it.
        var fill = new ScanProgressFill();
        for (var i = 0; i < 6; i++) fill.AtMilestone();

        Assert.Equal(Ceiling, fill.AtMilestone(), 10);
        Assert.Equal(Ceiling, fill.AtTicker(1, 2), 10);
    }

    [Fact]
    public void A_ticker_arriving_before_any_milestone_reads_the_first_band()
    {
        var fill = new ScanProgressFill();
        Assert.Equal(Floor + Band * 0.5, fill.AtTicker(1, 2), 10);
    }

    [Fact]
    public void The_host_sets_the_floor_and_the_close_itself()
    {
        var fill = new ScanProgressFill();
        Assert.Equal(Floor, fill.At(Floor), 10);

        for (var i = 0; i < 6; i++) fill.AtMilestone();
        Assert.Equal(100, fill.At(100), 10);
    }
}

/// <summary>
/// Ties the bar's bands to the scan that drives it, by running a real scan and
/// reading what it reports rather than by restating the number here. A phase
/// added to or taken out of the scan fails these, instead of quietly leaving the
/// bar with a band nothing fills or a phase with no band to fill.
/// </summary>
public class ScanProgressAgainstTheScanTests
{
    /// <summary>
    /// Collects the scan's updates on the thread that reports them.
    /// <see cref="Progress{T}"/> posts, and with no synchronisation context to
    /// post to it would hand them to the thread pool, so a test reading the list
    /// could read it before the scan had finished filling it.
    /// </summary>
    private sealed class Collected : IProgress<ScanProgressUpdate>
    {
        private readonly List<ScanProgressUpdate> _seen = new();

        public void Report(ScanProgressUpdate value)
        {
            lock (_seen) _seen.Add(value);
        }

        public IReadOnlyList<ScanProgressUpdate> Updates
        {
            get { lock (_seen) return _seen.ToList(); }
        }
    }

    /// <summary>
    /// A machine whose last product records no package path of its own. That
    /// product is the one the count must not skip: it takes the same turn in the
    /// loop as every other, so a count that follows the claims rather than the
    /// loop stops one short of the total and leaves the bar short of its band.
    /// </summary>
    private static FakeMsiApi MachineWith(int products)
    {
        var msi = new FakeMsiApi();
        for (var i = 1; i <= products; i++)
        {
            var code = $"{{{i:D8}-0000-0000-0000-000000000000}}";
            msi.AddProduct(code);
            msi.SetProductProperty(code, "ProductName", $"Product {i}");
            if (i < products)
                msi.SetProductProperty(code, "LocalPackage", $@"C:\Windows\Installer\{i}.msi");
        }
        return msi;
    }

    /// <summary>
    /// Runs the scan against the files given, none unless a test names some, so
    /// it reaches every milestone with only those for the classification to
    /// judge. What is under test is the shape of the report stream, which files
    /// lengthen and do not change.
    /// </summary>
    private static async Task<IReadOnlyList<ScanProgressUpdate>> Run(int products, params string[] files)
    {
        var collected = new Collected();
        var query = new InstallerQueryService(
            MachineWith(products),
            (_, _) => new InstallerQueryService.FallbackRead(0, 0),
            crashLogSink: null);

        await new FileSystemScanService(query, files).ScanAsync(collected);
        return collected.Updates;
    }

    [Fact]
    public async Task The_scan_reports_one_more_milestone_than_the_bar_has_bands()
    {
        var seen = await Run(products: 3);

        var fill = new ScanProgressFill();
        foreach (var update in seen)
        {
            if (update.IsMilestone) fill.AtMilestone();
            else fill.AtTicker(update.Position, update.Total);
        }

        // Six milestones and five bands. Asserted through the bar rather than on
        // the count alone, because the count on its own says nothing about where
        // the last one lands.
        Assert.Equal(6, seen.Count(u => u.IsMilestone));
        Assert.Equal(ScanProgressFill.CeilingPercent, fill.Fill, 10);
    }

    [Fact]
    public async Task The_product_phase_counts_every_product_it_was_handed()
    {
        var seen = await Run(products: 7);
        var ticks = seen.Where(u => !u.IsMilestone).ToList();

        // Every product the enumeration returned takes a turn, whatever its
        // records turn out to hold, so the positions run one to the total with
        // none missing and a bar filled from them reaches the end of its band.
        Assert.All(ticks, t => Assert.Equal(7, t.Total));
        Assert.Equal(Enumerable.Range(1, 7), ticks.Select(t => t.Position));
    }

    [Fact]
    public async Task The_product_phase_names_each_product_as_it_reaches_it()
    {
        var seen = await Run(products: 3);
        var ticks = seen.Where(u => !u.IsMilestone).Select(u => u.Message).ToList();

        Assert.Equal(new[] { "Product 1", "Product 2", "Product 3" }, ticks);
    }

    [Fact]
    public async Task The_matching_phase_counts_against_the_files_in_the_folder()
    {
        // Three files, each claimed by one of the first three products, so every
        // one is matched and none goes on to be judged.
        var seen = await Run(4,
            @"C:\Windows\Installer\1.msi", @"C:\Windows\Installer\2.msi", @"C:\Windows\Installer\3.msi");
        var matching = seen.SkipWhile(u => u.Message != Strings.Status_RegisteredPackagesFound)
            .Where(u => !u.IsMilestone).Select(u => u.Message).ToList();

        string Line(int position) => string.Format(Strings.Status_MatchingCount,
            DisplayHelpers.FormatCount(position), DisplayHelpers.FormatCount(3));
        Assert.Equal(new[] { Line(1), Line(2), Line(3) }, matching);
    }

    [Fact]
    public async Task The_screen_counts_against_the_files_it_was_handed()
    {
        // One file the first product claims and three no product claims, so those three
        // go on to the screen. The reader yields nothing from any of them, which keeps
        // each one back and still takes each one's turn, the patch file's included.
        string[] files =
        {
            @"C:\Windows\Installer\1.msi",
            @"C:\Windows\Installer\a.msi", @"C:\Windows\Installer\b.msi", @"C:\Windows\Installer\c.msp",
        };
        var collected = new Collected();
        var query = new InstallerQueryService(
            MachineWith(2),
            (_, _) => new InstallerQueryService.FallbackRead(0, 0),
            crashLogSink: null);
        var screen = new DeclaredProductCheck(new FakeMsiApi(), new YieldsNothing());

        await new FileSystemScanService(query, new FileSystem(), null, files, null, null, screen)
            .ScanAsync(collected);

        var screening = collected.Updates
            .SkipWhile(u => u.Message != Strings.Status_CheckingRemaining).Skip(1)
            .TakeWhile(u => !u.IsMilestone).ToList();

        string Line(int position) => string.Format(Strings.Status_MatchingCount,
            DisplayHelpers.FormatCount(position), DisplayHelpers.FormatCount(3));
        Assert.Equal(new[] { Line(1), Line(2), Line(3) }, screening.Select(u => u.Message));
        Assert.Equal(new[] { 1, 2, 3 }, screening.Select(u => u.Position));
        Assert.All(screening, u => Assert.Equal(3, u.Total));
    }

    [Fact]
    public async Task A_wait_the_screen_reports_reaches_the_host_as_a_wait_and_its_end_and_moves_no_milestone()
    {
        // The screen tells of one wait, on drive D:, while it judges the file it was handed.
        string[] files = { @"C:\Windows\Installer\1.msi", @"C:\Windows\Installer\a.msi" };
        var onD = new SourceFolderWait("D:", () => { });
        var collected = new Collected();
        var query = new InstallerQueryService(
            MachineWith(2),
            (_, _) => new InstallerQueryService.FallbackRead(0, 0),
            crashLogSink: null);
        var screen = Substitute.For<IDeclaredProductCheck>();
        screen.Screen(Arg.Any<IReadOnlyList<OrphanedFile>>(), Arg.Any<IReadOnlyList<ListedInstallation>>(), Arg.Any<IReadOnlyList<UnlistedProductKey>>(),
                Arg.Any<CancellationToken>(), Arg.Any<Action<Exception, string>?>(), Arg.Any<Func<string, bool?>?>(),
                Arg.Any<Action<int>?>(), Arg.Any<Action<SourceFolderWait?>?>(), Arg.Any<string?>(),
                Arg.Any<Func<string, bool?>?>())
            .Returns(call =>
            {
                var waitingOn = call.Args().OfType<Action<SourceFolderWait?>>().Single();
                waitingOn?.Invoke(onD);
                waitingOn?.Invoke(null);
                return new DeclaredProductScreening(
                    call.ArgAt<IReadOnlyList<OrphanedFile>>(0)
                        .Select(_ => DeclaredProductOutcome.DeclaredProductNotInstalled)
                        .ToList(),
                    [],
                    WaitCount: 1);
            });

        await new FileSystemScanService(query, new FileSystem(), null, files, null, null, screen)
            .ScanAsync(collected);

        var waits = collected.Updates.Where(u => u.IsWait).ToList();
        Assert.Equal(new[] { DisplayHelpers.WaitingFor("D:"), string.Empty }, waits.Select(u => u.Message));
        Assert.Equal(new[] { onD, null }, waits.Select(u => u.Wait));
        Assert.All(waits, u => Assert.False(u.IsMilestone));
        Assert.Equal(6, collected.Updates.Count(u => u.IsMilestone));
    }

    [Fact]
    public void A_wait_update_is_no_milestone_and_carries_its_wait_and_the_one_ending_it_is_empty()
    {
        var onTheShare = new SourceFolderWait(@"\\fileserver\apps", () => { });
        var starts = ScanProgressUpdate.Waiting(onTheShare);
        var ends = ScanProgressUpdate.Waiting(null);

        Assert.True(starts.IsWait);
        Assert.False(starts.IsMilestone);
        Assert.Equal(DisplayHelpers.WaitingFor(@"\\fileserver\apps"), starts.Message);
        Assert.Same(onTheShare, starts.Wait);
        Assert.True(ends.IsWait);
        Assert.False(ends.IsMilestone);
        Assert.Empty(ends.Message);
        Assert.Null(ends.Wait);
    }

    /// <summary>
    /// A reader that yields nothing from any file and says so with an empty detail, as
    /// the production readers do. The screen keeps each such file back.
    /// </summary>
    private sealed class YieldsNothing : IPackageIdentityReader
    {
        public PackageIdentity? Read(string filePath, bool isPatch, out string detail, out PackageReadRefusal refusal)
        {
            detail = string.Empty;
            refusal = PackageReadRefusal.WouldNotRead;
            return null;
        }
    }
}
