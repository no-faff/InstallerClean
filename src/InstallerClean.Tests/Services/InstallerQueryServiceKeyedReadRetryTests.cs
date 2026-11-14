using InstallerClean.Helpers;
using static InstallerClean.Tests.Services.InstallerQueryServiceWalkRestartTests;

namespace InstallerClean.Tests.Services;

/// <summary>
/// The scan's reads of one property of one installation or one patch pairing
/// (<c>MsiGetProductInfoEx</c> and <c>MsiGetPatchInfoEx</c>) are made again where the answer
/// does not stand (<see cref="WalkRestarts.KeyedRead"/>).
///
/// EVERY TEST SCANS THE MACHINE <see cref="InstallerQueryServiceWalkRestartTests"/> SCANS:
/// two products, each holding a superseded patch of its own that the scan offers. An error
/// that passes is planted on one read, and where the reads after it give one value twice the
/// scan reaches exactly what it reaches with nothing planted. An answer that stands is read
/// once. An error that stays, an absence read after an error and two values that differ
/// leave the scan holding as a single read that met the error does.
/// </summary>
public class InstallerQueryServiceKeyedReadRetryTests
{
    private const uint AccessDenied = 5, MoreData = 234, UnknownProduct = 1605, UnknownProperty = 1608;
    private const uint BadConfiguration = 1610, UnknownPatch = 1647;

    [Fact]
    public async Task With_nothing_planted_no_read_is_made_again_and_a_product_not_holding_a_patch_answers_once()
    {
        var clean = await Scan();

        Assert.Empty(clean.Pauses);
        Assert.Equal(1, clean.Msi.Reads(null, First, "LocalPackage"));
        Assert.Equal(1, clean.Msi.Reads("{P}", First, "LocalPackage"));

        // The second product holds no record of the first product's patch, and says so once.
        Assert.Equal(1, clean.Msi.Reads("{P}", Second, "State"));
        Assert.Equal(2, Offered(clean).Length);
    }

    [Theory]
    [InlineData(null, "LocalPackage", BadConfiguration)]
    [InlineData(null, "LocalPackage", AccessDenied)]
    [InlineData(null, "LocalPackage", UnknownProduct)]
    [InlineData(null, "ProductName", BadConfiguration)]
    [InlineData("{P}", "LocalPackage", BadConfiguration)]
    [InlineData("{P}", "LocalPackage", UnknownPatch)]
    [InlineData("{P}", "State", BadConfiguration)]
    [InlineData("{P}", "State", UnknownPatch)]
    [InlineData("{P}", "State", UnknownProduct)]
    [InlineData("{P}", "Uninstallable", BadConfiguration)]
    public async Task A_read_that_meets_an_error_once_leaves_the_scan_as_a_clean_one(
        string? patchCode, string property, uint error)
    {
        var clean = await Scan();
        var planted = await Scan(msi => msi.PassingReadError(patchCode, First, property, error));

        Assert.Null(planted.Refusal);
        Assert.Equal(clean.Result!.Packages, planted.Result!.Packages);
        Assert.Equal(clean.Result.Census, planted.Result.Census);

        // The planted read was made twice more than on the clean scan, once after each pause.
        Assert.Equal(clean.Msi.Reads(patchCode, First, property) + 2, planted.Msi.Reads(patchCode, First, property));
        Assert.Equal(BothPauses, planted.Pauses);

        Assert.Equal(2, Offered(planted).Length);
    }

    [Fact]
    public async Task A_value_that_will_not_read_after_its_length_did_is_read_again_from_its_length()
    {
        // The sizing call answers the value's length and the call writing the value answers
        // that the value is longer, as where it grew between the two calls.
        var clean = await Scan();
        var planted = await Scan(msi => msi.PassingReadError(null, First, "LocalPackage", MoreData, onValueCall: true));

        Assert.Equal(clean.Result!.Packages, planted.Result!.Packages);
        Assert.Equal(clean.Result.Census, planted.Result.Census);
        Assert.Equal(3, planted.Msi.Reads(null, First, "LocalPackage"));
        Assert.Equal(BothPauses, planted.Pauses);
    }

    [Fact]
    public async Task A_read_that_meets_an_error_twice_is_answered_by_the_third_and_the_read_after_it()
    {
        var clean = await Scan();
        var planted = await Scan(msi => msi.PassingReadError(null, First, "LocalPackage", BadConfiguration, calls: 2));

        Assert.Equal(clean.Result!.Packages, planted.Result!.Packages);
        Assert.Equal(clean.Result.Census, planted.Result.Census);
        Assert.Equal(4, planted.Msi.Reads(null, First, "LocalPackage"));
        Assert.Equal(new[] { BothPauses[0], BothPauses[1], BothPauses[1] }, planted.Pauses);
    }

    [Fact]
    public async Task A_value_read_after_an_error_that_the_next_read_does_not_give_again_leaves_the_error_standing()
    {
        // After the error the patch's State reads as obsoleted, and then as superseded.
        var clean = await Scan();
        var planted = await Scan(msi =>
        {
            msi.PassingReadError("{P}", First, "State", BadConfiguration);
            msi.PassingReadValues("{P}", First, "State", "4");
        });

        Assert.NotEqual(clean.Result!.Packages, planted.Result!.Packages);
        Assert.DoesNotContain(Offered(planted), p => p.LocalPackagePath == FirstPatchFile);
    }

    [Fact]
    public async Task A_cached_package_read_as_not_set_after_an_error_leaves_the_error_standing()
    {
        // The installation's cached package meets an error and then reads as not set, which
        // would leave the installation claiming no file.
        var planted = await Scan(msi =>
        {
            msi.PassingReadError(null, First, "LocalPackage", BadConfiguration);
            msi.ProductPropertyResult[(First, "LocalPackage")] = UnknownProperty;
        });

        Assert.Equal(WalkRestarts.Attempts, planted.Msi.Reads(null, First, "LocalPackage"));
        Assert.Equal(1, planted.Result!.Census.UnreadableProducts);
    }

    [Fact]
    public async Task An_installation_saying_after_an_error_that_it_holds_no_record_of_the_patch_keeps_the_patch()
    {
        // The second product holds no record of the first product's patch, and its first
        // answer about it is an error.
        var clean = await Scan();
        var planted = await Scan(msi => msi.PassingReadError("{P}", Second, "State", BadConfiguration));

        Assert.Equal(clean.Msi.Reads("{P}", Second, "State") + 2, planted.Msi.Reads("{P}", Second, "State"));
        Assert.NotEqual(clean.Result!.Packages, planted.Result!.Packages);
        Assert.DoesNotContain(Offered(planted), p => p.LocalPackagePath == FirstPatchFile);
    }

    [Fact]
    public async Task A_listed_holder_answering_once_that_it_holds_no_record_of_the_patch_is_asked_again()
    {
        // The machine-wide patch list names the second product as holding the first
        // product's patch, and the second product's first answer about it is that it holds
        // no record of it.
        static void SecondHoldsP(FakeMsiApi msi)
        {
            msi.RouteAHolders.Add(("{P}", Second));
            msi.SetPatchProperty("{P}", Second, "State", "2");
            msi.SetPatchProperty("{P}", Second, "Uninstallable", "0");
        }

        var clean = await Scan(SecondHoldsP);
        var planted = await Scan(msi =>
        {
            SecondHoldsP(msi);
            msi.PassingReadError("{P}", Second, "State", UnknownPatch);
        });

        Assert.Equal(clean.Result!.Packages, planted.Result!.Packages);
        Assert.Equal(clean.Msi.Reads("{P}", Second, "State") + 2, planted.Msi.Reads("{P}", Second, "State"));
        Assert.Equal(BothPauses, planted.Pauses);
        Assert.Equal(2, Offered(planted).Length);
    }

    [Fact]
    public async Task A_property_that_is_not_set_is_read_once()
    {
        var planted = await Scan(msi => msi.ProductPropertyResult[(First, "LocalPackage")] = UnknownProperty);

        Assert.Equal(1, planted.Msi.Reads(null, First, "LocalPackage"));
        Assert.Empty(planted.Pauses);
        Assert.Equal(0, planted.Result!.Census.UnreadableProducts);
    }

    [Fact]
    public async Task A_read_that_never_answers_holds_as_one_read_did_and_its_installation_is_read_once_after()
    {
        var planted = await Scan(msi => msi.ProductPropertyResult[(First, "LocalPackage")] = BadConfiguration);

        Assert.Equal(WalkRestarts.Attempts, planted.Msi.Reads(null, First, "LocalPackage"));
        Assert.Equal(BothPauses, planted.Pauses);
        Assert.Equal(1, planted.Result!.Census.UnreadableProducts);

        // Every later read of the same installation's own properties is made once.
        Assert.Equal(1, planted.Msi.Reads(null, First, "InstanceType"));

        // The other installation's reads are its own.
        Assert.Equal(1, planted.Msi.Reads(null, Second, "LocalPackage"));
    }

    [Fact]
    public async Task A_patch_whose_record_never_reads_leaves_its_installation_read_again_about_another_patch()
    {
        // The first product's own patch never answers for its path. Its installation is
        // then asked about the second product's patch, and that read meets an error once
        // before it answers that it holds no record of it.
        var clean = await Scan();
        var planted = await Scan(msi =>
        {
            msi.PatchPropertyResult[("{P}", First, "LocalPackage")] = BadConfiguration;
            msi.PassingReadError("{Q}", First, "State", BadConfiguration);
        });

        Assert.Equal(WalkRestarts.Attempts, planted.Msi.Reads("{P}", First, "LocalPackage"));
        Assert.Equal(clean.Msi.Reads("{Q}", First, "State") + 2, planted.Msi.Reads("{Q}", First, "State"));
    }
}
