using InstallerClean.Models;
using InstallerClean.Services;

// The reading is declared inside the query service, where the two listings that make it
// up are taken. Aliased rather than written out at every use, because what these tests
// are about is what it answers, not where it lives.
using EstablishedPatchReach = InstallerClean.Services.InstallerQueryService.EstablishedPatchReach;

namespace InstallerClean.Tests.Services;

/// <summary>
/// THE MACHINE THESE ARE ABOUT, IN PLAIN WORDS. A PC has the same program installed
/// twice, side by side, and a patch was applied to one of the two copies by name. The
/// machine-wide sweep does not return that second copy; the registry names it, and the
/// scan recovers it by asking Windows about it directly.
///
/// WHAT WENT WRONG. The scan asked that copy "do you hold this patch, and can it be
/// uninstalled" and was told, truthfully, that it holds it and it cannot. It never
/// asked the question that decides the case: does this copy hold anything ELSE that
/// could be uninstalled and reach back for this file. It works that answer out for
/// every program on the machine and files it under the program's name, and then never
/// looked it up for the recovered one, so the file went on the list. Uninstalling that
/// other patch is the operation measured taking a product back to its unpatched base,
/// discarding both patches and reporting success.
///
/// AND WHAT IS KEPT BACK IS THE FILE AND NOT THE SET. A recovered copy is named by the
/// machine rather than by any one file, so keeping every superseded patch back on such
/// a machine would cost somebody every file on it for one program's sake. The scan
/// reads two registry listings it is already taking, and keeps back the files that copy
/// could actually reach: the patches it holds, and where each of those patches records
/// its own cached package. Anything it cannot establish keeps everything, and the tests
/// below that end in "keeps the file" are the ones that hold that line.
///
/// THEY RUN THE WHOLE ENUMERATION rather than the condition on its own, because what is
/// being asserted is whether a file reaches the offer, and every pass between the
/// registry read and that answer is part of the question.
/// </summary>
public class InstallerQueryServiceRecoveredProductTests
{
    private const string Enumerated = "{AAAAAAAA-0000-0000-0000-00000000000A}";
    private const string SecondCopy = "{BBBBBBBB-0000-0000-0000-00000000000B}";
    private const string SharedPatch = "{CCCCCCCC-0000-0000-0000-00000000000C}";
    private const string ItsOwnPatch = "{DDDDDDDD-0000-0000-0000-00000000000D}";

    private const string ProductFile = @"C:\Windows\Installer\second-copy-product.msi";
    private const string SharedFile = @"C:\Windows\Installer\second-copy-shared.msp";
    private const string OtherFile = @"C:\Windows\Installer\second-copy-other.msp";

    [Fact]
    public async Task A_second_copy_holding_a_removable_patch_keeps_the_file()
    {
        // WHAT THIS FIXTURE MAKES TRUE, and it is the route itself: the second copy is
        // absent from the product sweep, named by the registry, holds the same
        // superseded patch and holds a patch of its own that can be uninstalled.
        // Nothing establishes what it holds, so it is judged against every cached patch
        // file on the machine and this one is kept.
        //
        // WITHOUT THE FIX THIS FILE IS OFFERED. The recovered copy reached the
        // per-pairing pass and never reached the per-product condition, so the only
        // question ever put to it was one it could answer truthfully without saving the
        // file.
        var row = await Scan(new EstablishedPatchReach());

        Assert.False(row.IsRemovable);
        // The verdict names the reason: something on a product sharing this patch can be
        // uninstalled. The clean copy's own verdict is AllNonRemovable, so this value is
        // reachable only through the recovered copy having been asked at all.
        Assert.Equal(ProductPatchSet.RemovablePatchPresent, row.ProductPatchSetVerdict);
    }

    [Fact]
    public async Task A_second_copy_that_can_reach_neither_this_patch_nor_this_file_does_not_keep_it()
    {
        // The same machine, with both registry listings read cleanly: the second copy
        // holds one patch and it is not this one, and that patch records a cached file
        // of its own which is not this one either. It cannot reach this file, so keeping
        // it back would keep it for a reason that is not true of this machine.
        //
        // IT IS THE ONLY TEST HERE THAT OFFERS, and that is what makes the rest of them
        // mean something: a build that kept every file back would pass all of them but
        // this one.
        var row = await Scan(Reach(
            holds: new[] { ItsOwnPatch },
            itsOwnPatchIsCachedAt: new[] { Path.GetFullPath(OtherFile) }));

        Assert.True(row.IsRemovable);
        Assert.Equal(ProductPatchSet.AllNonRemovable, row.ProductPatchSetVerdict);
    }

    [Fact]
    public async Task A_second_copy_whose_patch_list_would_not_read_keeps_the_file()
    {
        // The listing was not established: the key refused, or one of the names under it
        // was not a packed GUID and nothing could turn it into a code to compare. The
        // copy may hold any patch on the machine, so it is judged against every file.
        var row = await Scan(Reach(
            holds: null,
            itsOwnPatchIsCachedAt: new[] { Path.GetFullPath(OtherFile) }));

        Assert.False(row.IsRemovable);
        Assert.Equal(ProductPatchSet.RemovablePatchPresent, row.ProductPatchSetVerdict);
    }

    [Fact]
    public async Task A_second_copy_no_reading_covered_at_all_keeps_the_file()
    {
        // The same not-knowing reached the other way: there is no entry for this product
        // anywhere in the reading, which is what a machine whose UserData would not open
        // produces. An absent entry and a null entry must answer alike.
        var row = await Scan(Reach(
            holds: new[] { ItsOwnPatch },
            itsOwnPatchIsCachedAt: new[] { Path.GetFullPath(OtherFile) },
            forProduct: "{EEEEEEEE-0000-0000-0000-00000000000E}"));

        Assert.False(row.IsRemovable);
        Assert.Equal(ProductPatchSet.RemovablePatchPresent, row.ProductPatchSetVerdict);
    }

    [Fact]
    public async Task A_patch_whose_own_cached_file_is_unknown_keeps_the_file()
    {
        // The copy's patch list is complete and does not name this patch, and the patch
        // it DOES hold would not say where its own cached package is. Nothing has
        // established that the file that patch reaches for is not this one, so the file
        // is kept.
        //
        // A BUILD THAT ASKED ONLY ABOUT PATCH CODES OFFERS HERE, which is what makes this
        // a control rather than a restatement of the test above it.
        var row = await Scan(Reach(
            holds: new[] { ItsOwnPatch },
            itsOwnPatchIsCachedAt: null));

        Assert.False(row.IsRemovable);
        Assert.Equal(ProductPatchSet.RemovablePatchPresent, row.ProductPatchSetVerdict);
    }

    [Fact]
    public async Task A_patch_recording_this_very_file_keeps_it_though_no_claim_names_it()
    {
        // THE MACHINE WHERE THE CODES ALONE ARE NOT ENOUGH. The claim on this file came
        // from the enumerated copy and names it as the shared patch's cached package.
        // The registry ALSO records the same file as the cached package of the patch the
        // second copy can uninstall. One of those two registrations is wrong about which
        // file it names and nothing on the machine can tell which; if the file is really
        // the second copy's, removing it costs that machine the ability to uninstall a
        // patch the registry says is uninstallable.
        //
        // So the question is put to the copy's own registration rather than to another
        // product's claim, and the file is kept.
        var row = await Scan(Reach(
            holds: new[] { ItsOwnPatch },
            itsOwnPatchIsCachedAt: new[] { Path.GetFullPath(SharedFile) }));

        Assert.False(row.IsRemovable);
        Assert.Equal(ProductPatchSet.RemovablePatchPresent, row.ProductPatchSetVerdict);
    }

    [Fact]
    public async Task The_path_the_scan_claims_is_the_path_the_reading_is_compared_against()
    {
        // THE CONTROL FOR THE TWO TESTS ABOVE, AND WITHOUT IT NEITHER MEANS ANYTHING. A
        // cached path is compared against the claimed path after both have been put into
        // the spelling the folder walk produces. If the fixture's spelling and the
        // scan's ever stopped agreeing, the comparison would match nothing, and matching
        // nothing reads exactly like a product that cannot reach the file: the alias test
        // would pass by keeping the file for the wrong reason and the offering test would
        // pass by offering it for the wrong reason.
        var row = await Scan(new EstablishedPatchReach());

        Assert.Equal(Path.GetFullPath(SharedFile), row.LocalPackagePath);
    }

    [Fact]
    public void Not_knowing_judges_every_path_and_each_way_of_not_knowing_alike()
    {
        var codes = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { SharedPatch };

        // No reading at all, which is what a caller that supplies nothing gets. The
        // default value of the type has to be the wide answer, because a caller that
        // forgets is exactly the caller that must not narrow.
        Assert.True(new EstablishedPatchReach()
            .MustJudge(SecondCopy, SharedFile, codes));

        // A reading that covered other products and not this one.
        Assert.True(Reach(holds: new[] { ItsOwnPatch }, itsOwnPatchIsCachedAt: null,
                forProduct: Enumerated)
            .MustJudge(SecondCopy, SharedFile, codes));

        // A reading that covered this product and established nothing.
        Assert.True(Reach(holds: null, itsOwnPatchIsCachedAt: null)
            .MustJudge(SecondCopy, SharedFile, codes));
    }

    [Fact]
    public void A_product_holding_no_patch_at_all_is_judged_against_nothing()
    {
        // THE DISTINCTION THIS TURNS ON, AND IT IS THE ONE MOST EASILY LOST: an empty
        // listing is a complete answer and a null listing is the absence of one. A
        // product whose Patches key is not there holds no registered patch, so there is
        // nothing on it to uninstall and nothing it can reach for.
        var codes = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { SharedPatch };

        Assert.False(Reach(holds: Array.Empty<string>(), itsOwnPatchIsCachedAt: null)
            .MustJudge(SecondCopy, SharedFile, codes));
    }

    [Fact]
    public void A_patch_that_names_the_path_or_records_it_judges_and_either_alone_is_enough()
    {
        // The two questions are asked of the raw values here rather than through a scan,
        // so no spelling passes through the normaliser and both sides are the literal.
        var namingIt = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { SharedPatch };
        var namingNothingItHolds = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { SharedPatch };

        // The claims name a code this product holds, and where that code's cached file
        // is does not come into it.
        Assert.True(Reach(holds: new[] { SharedPatch },
                itsOwnPatchIsCachedAt: new[] { OtherFile })
            .MustJudge(SecondCopy, SharedFile, namingIt));

        // The claims name nothing this product holds, and what it does hold records this
        // very file for itself. Either question alone is enough to judge.
        Assert.True(Reach(holds: new[] { ItsOwnPatch },
                itsOwnPatchIsCachedAt: new[] { SharedFile })
            .MustJudge(SecondCopy, SharedFile, namingNothingItHolds));

        // Neither, which is the only combination that lets a path go.
        Assert.False(Reach(holds: new[] { ItsOwnPatch },
                itsOwnPatchIsCachedAt: new[] { OtherFile })
            .MustJudge(SecondCopy, SharedFile, namingIt));
    }

    [Fact]
    public void Two_readings_of_one_listing_merge_and_not_knowing_wins()
    {
        var a = new[] { SharedPatch };
        var b = new[] { ItsOwnPatch };

        // Both readings finished, so the union of them is a complete statement.
        var merged = InstallerQueryService.MergeEstablishedNames(a, b);
        Assert.NotNull(merged);
        Assert.Equal(2, merged.Count);
        Assert.Contains(SharedPatch, merged);
        Assert.Contains(ItsOwnPatch, merged);

        // One subtree's complete listing does not make another subtree's failed one
        // complete, and the order it was reached in must not decide the answer.
        Assert.Null(InstallerQueryService.MergeEstablishedNames(a, null));
        Assert.Null(InstallerQueryService.MergeEstablishedNames(null, b));
        Assert.Null(InstallerQueryService.MergeEstablishedNames(null, null));

        // Registry names arrive in whatever case the writer used, and every set they
        // are compared against is case-insensitive.
        var mixed = InstallerQueryService.MergeEstablishedNames(
            new[] { SharedPatch.ToLowerInvariant() }, new[] { SharedPatch });
        Assert.NotNull(mixed);
        Assert.Single(mixed);
    }

    /// <summary>
    /// One reading of the two registry listings, as the fallback would have returned it.
    /// <paramref name="holds"/> null is a listing that was not established;
    /// <paramref name="itsOwnPatchIsCachedAt"/> null is a patch registration that would
    /// not say where its cached package is.
    /// </summary>
    private static EstablishedPatchReach Reach(
        string[]? holds,
        string[]? itsOwnPatchIsCachedAt,
        string forProduct = SecondCopy) =>
        new(
            new Dictionary<string, IReadOnlyCollection<string>?>(StringComparer.OrdinalIgnoreCase)
            {
                // The enumerated copy, read cleanly, holding the shared patch and nothing
                // else. It is in every fixture because a reading that covered only the
                // product under test would not be one a real walk could produce.
                [Enumerated] = new[] { SharedPatch },
                [forProduct] = holds,
            },
            new Dictionary<string, IReadOnlyCollection<string>?>(StringComparer.OrdinalIgnoreCase)
            {
                [SharedPatch] = new[] { Path.GetFullPath(SharedFile) },
                [ItsOwnPatch] = itsOwnPatchIsCachedAt,
            });

    /// <summary>
    /// The machine, scanned. The second copy is hidden from the product sweep and named
    /// by the registry, so the scan recovers it by name exactly as it would in
    /// production, and the patch file declares the copy the sweep DID return, which is
    /// what an ordinary patch package looks like: the vendor listed its own base product
    /// codes and had no reason to list a second instance's.
    /// </summary>
    private static async Task<RegisteredPackage> Scan(EstablishedPatchReach reach)
    {
        var msi = new FakeMsiApi();
        msi.AddProduct(Enumerated);
        msi.SetProductProperty(Enumerated, "LocalPackage", ProductFile);
        msi.SetProductProperty(Enumerated, "ProductName", "A Program");
        msi.AddPatch(Enumerated, SharedPatch, SharedFile, state: "2", uninstallable: "0");

        // The second copy: never enumerated, holding the same superseded patch and a
        // patch of its own that can be uninstalled.
        msi.AddPatch(SecondCopy, SharedPatch, SharedFile, state: "2", uninstallable: "0");
        msi.AddPatch(SecondCopy, ItsOwnPatch, OtherFile, state: "1", uninstallable: "1");

        var patchSets = new Dictionary<string, ProductPatchSet>(StringComparer.OrdinalIgnoreCase)
        {
            [Enumerated] = ProductPatchSet.AllNonRemovable,
            [SecondCopy] = ProductPatchSet.RemovablePatchPresent,
        };
        var registryCodes = new[] { Enumerated, SecondCopy };

        var result = await new InstallerQueryService(msi,
                (_, _) => new InstallerQueryService.FallbackRead(
                    0, registryCodes.Length,
                    RegistryProductCodes: registryCodes,
                    ProductPatchSets: patchSets,
                    Reach: reach),
                crashLogSink: null,
                identityReader: new DeclaringReader(SharedFile, Enumerated))
            .GetRegisteredPackagesAsync();

        return result.Packages.Single(
            p => p.LocalPackagePath.EndsWith("second-copy-shared.msp", StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    /// A patch file that declares the products its own Template names. Declaring none is
    /// not an option worth offering here: the production reader returns null for a patch
    /// whose Template names no product, and null is the unread file, which withholds, so
    /// a fixture built that way would keep every file for a reason that has nothing to do
    /// with what is under test.
    /// </summary>
    private sealed class DeclaringReader(string patchPath, params string[] targets) : IPackageIdentityReader
    {
        public PackageIdentity? Read(string filePath, bool isPatch, out string detail)
        {
            detail = string.Empty;
            if (!filePath.EndsWith(Path.GetFileName(patchPath), StringComparison.OrdinalIgnoreCase))
                return new PackageIdentity(string.Empty, isPatch, new[] { Enumerated });
            return new PackageIdentity(string.Empty, isPatch, targets);
        }
    }
}
