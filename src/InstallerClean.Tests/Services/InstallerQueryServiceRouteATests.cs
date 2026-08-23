using InstallerClean.Helpers;
using InstallerClean.Models;
using InstallerClean.Services;

namespace InstallerClean.Tests.Services;

/// <summary>
/// ROUTE A: the machine-wide patch enumeration, asked about no product in particular.
/// <see cref="InstallerQueryService"/> makes that call to hear about a product the PRODUCT
/// enumeration never returned, which is the only source that can, and the per-product
/// condition then judges that product's patch set like any other.
///
/// NOTHING IN THIS SUITE EXERCISED IT UNTIL THIS FILE, AND THAT WAS NOT A GAP IN COVERAGE
/// SO MUCH AS A FIXTURE THAT COULD NOT REACH IT. <see cref="FakeMsiApi"/> answers every
/// patch-enumeration knob by product code, route A passes none, and the call fell through
/// to the "this product holds no patches" return. So route A succeeded and named nothing on
/// every fixture ever written here, in both directions at once: no test could show it
/// contributing, and no test could show what happens when it refuses. The fake now takes a
/// forced return code and a scripted holder list, both default-off, so nothing already
/// written moves.
///
/// WHY IT IS WORTH ITS OWN FILE. The two things route A decides are on opposite sides of the
/// app. What it NAMES can take a file off the offer, and whether it ANSWERED AT ALL decides
/// what the missing-files split may claim. A file that holds both keeps them from being
/// tidied apart into places where each looks like a detail.
///
/// EVERY FIXTURE HERE DECLARES A TARGET IN ITS PATCH FILE, deliberately, because
/// <see cref="PackageIdentityReader"/> cannot produce an identity that declares none: a patch
/// whose Template names no product is returned as UNREAD, and the caller then withholds. A
/// fixture handing back a readable identity with an empty target list is describing a machine
/// that cannot exist, which is the same shape as the trap recorded against the DEFAULT reader
/// in <c>FileSystemScanServiceTests</c>.
/// </summary>
public class InstallerQueryServiceRouteATests
{
    private const string ProductFile = @"C:\Windows\Installer\route-a-product.msi";
    private const string PatchFile = @"C:\Windows\Installer\route-a-shared.msp";

    /// <summary>The enumerated product, which claims the patch and reads clean.</summary>
    private const string Enumerated = "{A}";

    /// <summary>
    /// The product ONLY route A can name. It is absent from the product enumeration, carries
    /// no claim, and its registry patch set holds something that can be uninstalled, so it is
    /// exactly the registration that should overturn a clean verdict.
    /// </summary>
    private const string RouteAOnly = "{C}";

    /// <summary>
    /// An identity reader that answers as the production one does for a patch on the disk:
    /// a readable file yielding the product codes its Template names. It declares
    /// <see cref="Enumerated"/> and never <see cref="RouteAOnly"/>, so route B cannot be
    /// what puts the second product into the judged set and route A is the only candidate.
    /// </summary>
    private sealed class DeclaringReader : IPackageIdentityReader
    {
        public PackageIdentity? Read(string filePath, bool isPatch, out string detail)
        {
            detail = string.Empty;
            return isPatch
                ? new PackageIdentity(string.Empty, true, new[] { Enumerated })
                : new PackageIdentity(string.Empty, false, Array.Empty<string>());
        }
    }

    /// <param name="routeANamesTheOtherHolder">
    /// The one field the pair differs in. Everything else about the machine is identical.
    /// </param>
    private static async Task<RegisteredPackage> Enumerate(bool routeANamesTheOtherHolder)
    {
        var msi = new FakeMsiApi();
        msi.AddProduct(Enumerated);
        msi.SetProductProperty(Enumerated, "LocalPackage", ProductFile);
        msi.SetProductProperty(Enumerated, "ProductName", "Test Product");
        msi.AddPatch(Enumerated, "{P}", PatchFile, state: "2", uninstallable: "0");

        var sets = new Dictionary<string, ProductPatchSet>(StringComparer.OrdinalIgnoreCase)
        {
            [Enumerated] = ProductPatchSet.AllNonRemovable,
            // Present in both halves, so the pair does not differ in what the registry holds.
            // It is only ever CONSULTED when something puts this product into the judged set.
            [RouteAOnly] = ProductPatchSet.RemovablePatchPresent,
        };

        if (routeANamesTheOtherHolder) msi.RouteAHolders.Add(("{P}", RouteAOnly));

        var result = await new InstallerQueryService(msi,
                (_, _) => new InstallerQueryService.FallbackRead(0, 1, ProductPatchSets: sets),
                crashLogSink: null, identityReader: new DeclaringReader())
            .GetRegisteredPackagesAsync();

        return result.Packages.Single(p => p.PatchState == 2);
    }

    [Fact]
    public async Task A_product_only_route_A_names_is_judged_against_its_patch_set()
    {
        // WHAT THIS PINS, AND IT COULD NOT BE WRITTEN BEFORE THE FAKE COULD REFUSE OR NAME.
        // The verdict is taken across every product sharing the patch, and this product
        // shares it while appearing in no enumeration and no claim. If route A's rows stop
        // reaching the judged set, the verdict here reads AllNonRemovable, the row stays
        // removable, and a file another product can still roll back onto goes on the offer.
        var row = await Enumerate(routeANamesTheOtherHolder: true);

        Assert.Equal(ProductPatchSet.RemovablePatchPresent, row.ProductPatchSetVerdict);
        Assert.False(row.IsRemovable);
        // Established rather than unestablished: the app did not fail to find out, it found
        // out. The two withhold alike and they are not the same finding.
        Assert.False(row.RemovableWithheld);
    }

    private const string ObsoletedFile = @"C:\Windows\Installer\route-a-obsoleted.msp";

    /// <summary>
    /// A machine carrying an OBSOLETED registration whose file has gone, alongside a
    /// superseded patch that is offer-eligible so the judging pass runs at all. The
    /// obsoleted row is the subject: it is never offer-eligible in 3.0.0, so nothing
    /// downstream can take a verdict away from it and the verdict this pass stamps is the
    /// only one it will ever carry.
    /// </summary>
    private static async Task<RegisteredPackage> EnumerateWithMissingObsoletedPatch(bool routeARefuses)
    {
        var msi = new FakeMsiApi();
        msi.AddProduct(Enumerated);
        msi.SetProductProperty(Enumerated, "LocalPackage", ProductFile);
        msi.SetProductProperty(Enumerated, "ProductName", "Test Product");
        msi.AddPatch(Enumerated, "{OBS}", ObsoletedFile, state: "4", uninstallable: "0");
        msi.AddPatch(Enumerated, "{P}", PatchFile, state: "2", uninstallable: "0");

        var sets = new Dictionary<string, ProductPatchSet>(StringComparer.OrdinalIgnoreCase)
        {
            [Enumerated] = ProductPatchSet.AllNonRemovable,
        };

        if (routeARefuses) msi.RouteAResult = 5;   // ERROR_ACCESS_DENIED

        var result = await new InstallerQueryService(msi,
                (_, _) => new InstallerQueryService.FallbackRead(0, 1, ProductPatchSets: sets),
                crashLogSink: null, identityReader: new DeclaringReader())
            .GetRegisteredPackagesAsync();

        return result.Packages.Single(p => p.PatchState == 4);
    }

    [Fact]
    public async Task An_obsoleted_row_is_not_judged_clean_when_the_machine_wide_enumeration_refused()
    {
        // THE ROW NOTHING DOWNSTREAM PROTECTS. When the machine-wide enumeration does not
        // answer, the confirmation pass withholds every still-removable path, and that is
        // what keeps a missing SUPERSEDED registration under the banner on such a run.
        // Downgrade takes a removable verdict away and returns at once where there is
        // none, so it never reaches a row that was never removable. An obsoleted
        // registration is never removable in 3.0.0, for a policy reason rather than a
        // dangerous one.
        //
        // SO THIS ROW READ ITS VERDICT OFF A PRODUCT SET THE APP HAD JUST BEEN TOLD WAS
        // SHORT, and read it as positively clean, and the missing-files split then took
        // that for the app having established the absence was harmless. Route A is the
        // only source that can name a product no enumeration returned, so losing it does
        // not narrow the set by a knowable margin; it removes the only means of finding
        // out. The verdict has to say so.
        var row = await EnumerateWithMissingObsoletedPatch(routeARefuses: true);

        Assert.Equal(4, row.PatchState);
        Assert.Equal(ProductPatchSet.Unestablished, row.ProductPatchSetVerdict);
        // And the split reports it, which is the half the verdict assertion cannot see.
        Assert.True(MissingFilesReport.Affected(row with { FileExists = false }));
    }

    [Fact]
    public async Task The_same_obsoleted_row_stays_quiet_when_the_enumeration_answered()
    {
        // THE MUST-MISS CONTROL, differing in one field. Without it, a change that simply
        // stopped stamping clean verdicts anywhere would satisfy the test above while
        // putting every missing obsoleted registration on every machine under a banner
        // about files this app itself removed, which is the direction the whole carve-out
        // exists to avoid.
        var row = await EnumerateWithMissingObsoletedPatch(routeARefuses: false);

        Assert.Equal(ProductPatchSet.AllNonRemovable, row.ProductPatchSetVerdict);
        Assert.False(MissingFilesReport.Affected(row with { FileExists = false }));
    }

    [Fact]
    public async Task The_same_machine_without_that_row_offers_the_patch()
    {
        // THE MUST-HIT CONTROL, and it is what stops the test above passing for the wrong
        // reason. The two fixtures differ in one field. Without this, a build that offered
        // nothing at all -- or a fake whose route A never names anything, which is what this
        // suite had until now -- would satisfy the assertions above by never producing a
        // removable row in the first place.
        var row = await Enumerate(routeANamesTheOtherHolder: false);

        Assert.Equal(ProductPatchSet.AllNonRemovable, row.ProductPatchSetVerdict);
        Assert.True(row.IsRemovable);
    }
}
