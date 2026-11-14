using InstallerClean.Helpers;
using InstallerClean.Interop;
using InstallerClean.Models;
using InstallerClean.Services;

namespace InstallerClean.Tests.Services;

/// <summary>
/// Four of the Windows Installer lists the scan walks: every product, the machine-wide
/// patch list, each product's own patches, and the installations of one product code. A walk
/// that a row it cannot read stops is made again from the first index
/// (<see cref="WalkRestarts"/>).
///
/// EVERY TEST RUNS THE SAME MACHINE: two products, each holding a superseded patch of its
/// own that the scan offers. An error that passes is planted on one row of one list, and
/// the scan then offers exactly what it offers with nothing planted. An error that stays
/// leaves the scan holding or refusing as a single walk that met it does.
///
/// The pause is recorded and never slept. <see cref="InstallerQueryServiceKeyedReadRetryTests"/>
/// scans the same machine.
/// </summary>
public class InstallerQueryServiceWalkRestartTests
{
    private const uint AccessDenied = 5, BadConfiguration = 1610;

    internal const string First = "{A}";
    internal const string FirstFile = @"C:\Windows\Installer\restart-first.msi";
    internal const string FirstPatchFile = @"C:\Windows\Installer\restart-first.msp";
    internal const string Second = "{B}";
    internal const string SecondFile = @"C:\Windows\Installer\restart-second.msi";
    internal const string SecondPatchFile = @"C:\Windows\Installer\restart-second.msp";

    internal static readonly TimeSpan[] BothPauses =
        [TimeSpan.FromMilliseconds(50), TimeSpan.FromMilliseconds(250)];

    /// <summary>
    /// Answers as the production reader does for a patch on the disk, naming
    /// <see cref="First"/> as the product each patch file declares, so the scan asks for
    /// that code's installations.
    /// </summary>
    private sealed class DeclaringReader : IPackageIdentityReader
    {
        public PackageIdentity? Read(string filePath, bool isPatch, out string detail, out PackageReadRefusal refusal)
        {
            detail = string.Empty;
            refusal = PackageReadRefusal.WouldNotRead;
            return isPatch
                ? new PackageIdentity(string.Empty, true, new[] { First })
                : new PackageIdentity(string.Empty, false, Array.Empty<string>());
        }

        public string? PackageCodeOf(string filePath) => null;

        public bool HoldsNoRevisionNumber(string filePath) => false;
    }

    internal sealed record Scanned(
        InstallerQueryResult? Result, Exception? Refusal, FakeMsiApi Msi, List<TimeSpan> Pauses, List<Exception> Logged);

    internal static async Task<Scanned> Scan(Action<FakeMsiApi>? plant = null)
    {
        var msi = new FakeMsiApi();
        msi.AddProduct(First);
        msi.SetProductProperty(First, "LocalPackage", FirstFile);
        msi.SetProductProperty(First, "ProductName", "First Product");
        msi.AddPatch(First, "{P}", FirstPatchFile, state: "2", uninstallable: "0");
        msi.AddProduct(Second);
        msi.SetProductProperty(Second, "LocalPackage", SecondFile);
        msi.SetProductProperty(Second, "ProductName", "Second Product");
        msi.AddPatch(Second, "{Q}", SecondPatchFile, state: "2", uninstallable: "0");
        msi.RouteAHolders.Add(("{P}", First));
        msi.RouteAHolders.Add(("{Q}", Second));
        plant?.Invoke(msi);

        var sets = new Dictionary<string, ProductPatchSet>(StringComparer.OrdinalIgnoreCase)
        {
            [First] = ProductPatchSet.AllNonRemovable,
            [Second] = ProductPatchSet.AllNonRemovable,
        };

        var pauses = new List<TimeSpan>();
        var logged = new List<Exception>();
        var service = new InstallerQueryService(msi,
            (_, _) => new InstallerQueryService.FallbackRead(0, sets.Count, ProductPatchSets: sets),
            crashLogSink: logged.Add, identityReader: new DeclaringReader())
        {
            WalkPause = pauses.Add,
        };

        try
        {
            return new Scanned(await service.GetRegisteredPackagesAsync(), null, msi, pauses, logged);
        }
        catch (Exception refusal) when (refusal is LocalisedAccessException or LocalisedInvalidOperationException)
        {
            return new Scanned(null, refusal, msi, pauses, logged);
        }
    }

    internal static RegisteredPackage[] Offered(Scanned scanned) =>
        scanned.Result!.Packages.Where(p => p.IsRemovable).ToArray();

    [Fact]
    public async Task With_nothing_planted_both_superseded_patches_are_offered_and_every_list_is_walked_once()
    {
        // The control: the machine offers something, and each of the four lists is walked,
        // so a test below cannot pass on a scan that offers nothing or never asks.
        var clean = await Scan();

        Assert.Empty(clean.Pauses);
        Assert.Equal(1, clean.Msi.WalksStarted(MsiWalk.Products));
        Assert.Equal(1, clean.Msi.WalksStarted(MsiWalk.RouteA));
        Assert.Equal(1, clean.Msi.WalksStarted(MsiWalk.Patches, First));
        Assert.True(clean.Msi.WalksStarted(MsiWalk.Instances, First) >= 1);
        Assert.Equal(new[] { FirstPatchFile, SecondPatchFile }, Offered(clean).Select(p => p.LocalPackagePath).OrderBy(p => p));
    }

    [Theory]
    [InlineData(MsiWalk.Products, null, 1u, BadConfiguration)]
    [InlineData(MsiWalk.Products, null, 0u, AccessDenied)]
    [InlineData(MsiWalk.RouteA, null, 1u, BadConfiguration)]
    [InlineData(MsiWalk.RouteA, null, 0u, AccessDenied)]
    [InlineData(MsiWalk.Patches, First, 0u, BadConfiguration)]
    [InlineData(MsiWalk.Patches, Second, 1u, BadConfiguration)]
    [InlineData(MsiWalk.Patches, First, 0u, AccessDenied)]
    [InlineData(MsiWalk.Instances, First, 0u, BadConfiguration)]
    [InlineData(MsiWalk.Instances, First, 1u, BadConfiguration)]
    public async Task An_error_one_walk_meets_and_the_next_does_not_leaves_the_scan_as_a_clean_one(
        MsiWalk walk, string? productCode, uint index, uint error)
    {
        var clean = await Scan();
        var planted = await Scan(msi => msi.PassingError(walk, productCode, index, error));

        Assert.Null(planted.Refusal);
        Assert.Equal(clean.Result!.Packages, planted.Result!.Packages);
        Assert.Equal(clean.Result.Census, planted.Result.Census);

        // The planted list was walked once more than on the clean scan, after one pause.
        Assert.Equal(clean.Msi.WalksStarted(walk, productCode) + 1, planted.Msi.WalksStarted(walk, productCode));
        Assert.Equal(new[] { BothPauses[0] }, planted.Pauses);
        Assert.Empty(planted.Logged);

        Assert.Equal(2, Offered(planted).Length);
    }

    [Theory]
    [InlineData(MsiWalk.Products, null)]
    [InlineData(MsiWalk.RouteA, null)]
    [InlineData(MsiWalk.Patches, First)]
    [InlineData(MsiWalk.Instances, First)]
    public async Task An_error_that_lasts_two_walks_is_gone_by_the_third(MsiWalk walk, string? productCode)
    {
        var clean = await Scan();
        var planted = await Scan(msi => msi.PassingError(walk, productCode, 0, BadConfiguration, calls: 2));

        Assert.Null(planted.Refusal);
        Assert.Equal(clean.Result!.Packages, planted.Result!.Packages);
        Assert.Equal(BothPauses, planted.Pauses);
    }

    [Fact]
    public async Task A_product_row_that_never_reads_refuses_the_scan_after_three_walks()
    {
        var planted = await Scan(msi => msi.Products[1] = (Second, BadConfiguration));

        Assert.IsType<LocalisedInvalidOperationException>(planted.Refusal);
        Assert.Equal(WalkRestarts.Attempts, planted.Msi.WalksStarted(MsiWalk.Products));
        Assert.Equal(BothPauses, planted.Pauses);
    }

    [Fact]
    public async Task A_product_list_that_keeps_refusing_access_refuses_the_scan_as_access_denied()
    {
        var planted = await Scan(msi => msi.Products[0] = (First, AccessDenied));

        Assert.IsType<LocalisedAccessException>(planted.Refusal);
        Assert.Equal(WalkRestarts.Attempts, planted.Msi.WalksStarted(MsiWalk.Products));
    }

    [Fact]
    public async Task A_machine_wide_patch_list_that_never_reads_withholds_every_superseded_patch_after_three_walks()
    {
        var planted = await Scan(msi => msi.RouteAResult = BadConfiguration);

        Assert.Empty(Offered(planted));
        Assert.All(planted.Result!.Packages.Where(p => p.PatchState == 2), row =>
        {
            Assert.True(row.RemovableWithheld);
            Assert.True(row.WithheldScanWide);
        });
        Assert.Equal(WalkRestarts.Attempts, planted.Msi.WalksStarted(MsiWalk.RouteA));
        Assert.Equal(BothPauses, planted.Pauses);
    }

    [Fact]
    public async Task A_patch_list_that_never_reads_counts_its_product_once_and_leaves_one_breadcrumb()
    {
        var planted = await Scan(msi => msi.PatchEnumResult[First] = BadConfiguration);

        Assert.Equal(1, planted.Result!.Census.UnreadableProducts);
        Assert.Equal(WalkRestarts.Attempts, planted.Msi.WalksStarted(MsiWalk.Patches, First));
        Assert.Equal(BothPauses, planted.Pauses);

        // One entry for the product, not one for each walk.
        Assert.Single(planted.Logged, e => e.Message.Contains("Patch enumeration abandoned"));
    }

    [Fact]
    public async Task A_patch_list_that_never_ends_refuses_the_scan_after_one_walk()
    {
        // Every row read, so there is no error for a second walk to get past.
        var planted = await Scan(msi => msi.NeverEndPatchesFor = First);

        Assert.IsType<LocalisedInvalidOperationException>(planted.Refusal);
        Assert.Equal(1, planted.Msi.WalksStarted(MsiWalk.Patches, First));
        Assert.Empty(planted.Pauses);
    }

    [Fact]
    public async Task A_product_list_that_never_ends_refuses_the_scan_after_one_walk()
    {
        var planted = await Scan(msi => msi.NeverEndProducts = true);

        Assert.IsType<LocalisedInvalidOperationException>(planted.Refusal);
        Assert.Equal(1, planted.Msi.WalksStarted(MsiWalk.Products));
        Assert.Empty(planted.Pauses);
    }

    [Fact]
    public async Task An_installation_list_that_never_reads_withholds_the_patches_declaring_its_code()
    {
        var planted = await Scan(msi => msi.KeyedRowResult[(First, 0)] = BadConfiguration);

        Assert.Empty(Offered(planted));
        Assert.All(planted.Result!.Packages.Where(p => p.PatchState == 2), row => Assert.True(row.RemovableWithheld));
        Assert.Equal(BothPauses, planted.Pauses.Take(2));
    }

    [Fact]
    public async Task The_machine_wide_patch_list_is_walked_again_after_patch_lists_have_spent_their_pauses()
    {
        // Many products whose own patch lists never read, which spends the pauses those
        // lists share, and then one error that passes on the machine-wide list.
        var planted = await Scan(msi =>
        {
            for (var extra = 0; extra < 40; extra++)
            {
                var code = $"{{EXTRA-{extra}}}";
                msi.AddProduct(code);
                msi.PatchEnumResult[code] = BadConfiguration;
            }

            msi.PassingError(MsiWalk.RouteA, null, 1, BadConfiguration);
        });

        Assert.Equal(2, planted.Msi.WalksStarted(MsiWalk.RouteA));

        var paused = planted.Pauses.Aggregate(TimeSpan.Zero, (sum, wait) => sum + wait);
        Assert.True(paused <= WalkRestarts.KeyedPauseLimit + BothPauses[0], $"paused {paused}");

        // The last of the forty was still walked three times, with no pause left for it.
        Assert.Equal(WalkRestarts.Attempts, planted.Msi.WalksStarted(MsiWalk.Patches, "{EXTRA-39}"));
    }
}
