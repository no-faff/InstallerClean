using InstallerClean.Models;
using InstallerClean.Services;

namespace InstallerClean.Tests.Services;

/// <summary>
/// WHICH PRODUCTS GET ASKED WHETHER THEY ARE A SECOND INSTANCE OF THEMSELVES.
///
/// The reading itself is pinned beside the enumeration's own loop, where the spellings
/// and the failure direction are held. What is here is the OTHER population: a product
/// the machine-wide enumeration lost, which is recovered afterwards by name, asked
/// whether it is installed, asked about every patch it holds, and asked this in its own
/// account and context.
///
/// THAT IS THE POPULATION MOST LIKELY TO HOLD THE CONDITION. The machine this class of
/// work is about is one carrying the same program twice, and the sibling file on the
/// recovered-product condition opens by describing exactly that machine: a copy the
/// sweep does not return and the registry does.
///
/// EACH INSTALLATION IS LISTED WITH ITS OWN READING. A positive reading, or one that did
/// not read, marks the row it was taken for
/// (<see cref="ListedInstallation.SecondCopyNotRuledOut"/>), a recovered row as much as
/// an enumerated one, and the declared-product check compares every installation package
/// with the packages each marked installation opens. The counts decide nothing.
///
/// AND THE PRODUCTS NOTHING SHOWS WERE ASKED. A product the registry names that Windows
/// would not say is installed is never recovered, so it is never put the question, and
/// a registry key whose name yields no code cannot be matched to any product that was.
/// The RegistryProductUnaskable withholding leg reads both counts, and two fixtures are those two states.
///
/// READ WHAT EACH FIXTURE SETS UP. Every one of them differs from its neighbour in a
/// single reading, so a count that moved for any other reason would show up as the
/// wrong pair moving together.
/// </summary>
public class InstallerQueryServiceSecondInstanceTests
{
    private const string Enumerated = "{AAAAAAAA-0000-0000-0000-00000000000A}";
    private const string Recovered = "{BBBBBBBB-0000-0000-0000-00000000000B}";
    private const string EnumeratedFile = @"C:\Windows\Installer\enumerated.msi";

    private const uint AccessDenied = 5;
    private const uint BadConfiguration = 1610;
    private const uint UnknownProperty = 1608;

    [Fact]
    public async Task A_recovered_product_that_is_a_second_instance_of_itself_is_counted()
    {
        // A recovered product answering that it is a second instance is counted like an
        // enumerated one.
        var census = await Scan(recoveredInstanceType: "1");

        Assert.Equal(1, census.RecoveredProductCount);
        Assert.Equal(1, census.InstanceProductCount);
        Assert.Equal(0, census.InstanceTypeUnreadableCount);
        Assert.False(census.RegistryProductUnaskable);
    }

    [Fact]
    public async Task A_recovered_product_that_answers_ordinary_is_counted_nowhere()
    {
        // THE MUST-MISS, and without it the test above passes just as well against a
        // rule that fires on any recovered product at all. Recovering a product costs
        // nothing by design: the gap it would have been part of was closed by asking
        // rather than covered by withholding, and one more question to it does not
        // turn that back into a withholding.
        var census = await Scan(recoveredInstanceType: "0");

        Assert.Equal(1, census.RecoveredProductCount);
        Assert.Equal(0, census.InstanceProductCount);
        Assert.Equal(0, census.InstanceTypeUnreadableCount);
        Assert.False(census.RegistryProductUnaskable);
    }

    [Fact]
    public async Task A_recovered_product_carrying_no_InstanceType_is_ordinary_and_not_unreadable()
    {
        // The ordinary case, and the one that keeps this off every machine whose
        // enumeration came back short for innocent reasons. An absent property is
        // documented as meaning an ordinary installation, so it is a clean negative
        // and not a read that failed. If this ever goes red, the app has started
        // emptying the offer on every machine holding a leftover UserData key.
        var census = await Scan(recoveredInstanceTypeResult: UnknownProperty);

        Assert.Equal(1, census.RecoveredProductCount);
        Assert.Equal(0, census.InstanceProductCount);
        Assert.Equal(0, census.InstanceTypeUnreadableCount);
        Assert.False(census.RegistryProductUnaskable);
    }

    [Fact]
    public async Task A_recovered_product_that_would_not_answer_is_counted_as_unreadable()
    {
        // A question put and not answered, on the population that had never been asked
        // one. It reaches the unreadable count and not the positive one, which is the
        // only honest place for it: nothing has been established either way. The row it
        // was taken for is marked as a positive reading marks it, which the test on the
        // marks below pins.
        var census = await Scan(recoveredInstanceTypeResult: BadConfiguration);

        Assert.Equal(1, census.RecoveredProductCount);
        Assert.Equal(0, census.InstanceProductCount);
        Assert.Equal(1, census.InstanceTypeUnreadableCount);
        Assert.False(census.RegistryProductUnaskable);
    }

    [Fact]
    public async Task Asking_a_recovered_product_does_not_withhold_anything_else()
    {
        // THE FAILURE MUST NOT LEAK INTO THE COUNT THAT WITHHOLDS THE SUPERSEDED CLASS,
        // which is the trap the enumerated half of this reading already carries a note
        // about: every other failed read in that loop is about a CLAIM that never got to
        // the merge, and this property carries no claim on any file. A read that fails
        // here has to leave the rest of the scan exactly where it was.
        var census = await Scan(recoveredInstanceTypeResult: BadConfiguration);

        Assert.Equal(0, census.UnreadableProducts);
        Assert.Equal(0, census.UnansweredProductCount);
    }

    [Fact]
    public async Task The_two_populations_are_counted_into_one_pair_of_totals()
    {
        // Both halves at once, which is what the rule reads. One enumerated product
        // answering positively and one recovered product refusing to answer are two
        // different findings about two different products, and the scan has to carry
        // both rather than let either stand for the machine.
        var census = await Scan(
            enumeratedInstanceType: "1",
            recoveredInstanceTypeResult: BadConfiguration);

        Assert.Equal(1, census.InstanceProductCount);
        Assert.Equal(1, census.InstanceTypeUnreadableCount);
    }

    [Fact]
    public async Task A_product_the_registry_names_and_Windows_will_not_answer_about_withholds()
    {
        // NEVER ASKED, WHICH IS NOT THE SAME AS ASKED AND ORDINARY. The registry names
        // the product, the enumeration did not return it, and the keyed question about
        // it met a row nobody could read, so it was never recovered and never put the
        // InstanceType question. Nothing shows it is not a second instance of itself,
        // which is the state the rule is named for.
        var census = await Scan(recoveredUnaskable: true);

        Assert.Equal(0, census.RecoveredProductCount);
        Assert.Equal(1, census.UnansweredProductCount);
        Assert.Equal(0, census.InstanceProductCount);
        Assert.Equal(0, census.InstanceTypeUnreadableCount);
        Assert.True(census.RegistryProductUnaskable);
    }

    [Fact]
    public async Task A_product_key_whose_name_yields_no_code_withholds()
    {
        // The same not-knowing one step earlier. The registry holds a product key and
        // its name gives no code, so nothing can be asked by it and nothing shows the
        // product it belongs to was asked. The recovered product beside it answers
        // ordinary, so this key is the only thing in the fixture able to arm the rule.
        var census = await Scan(unparseableKeyNames: 1);

        Assert.Equal(1, census.UnparseableProductKeyNames);
        Assert.Equal(0, census.UnansweredProductCount);
        Assert.Equal(0, census.InstanceProductCount);
        Assert.Equal(0, census.InstanceTypeUnreadableCount);
        Assert.True(census.RegistryProductUnaskable);
    }

    public static TheoryData<string?, uint?, string?, uint?, bool, bool> Readings() => new()
    {
        // enumerated value, enumerated forced result, recovered value, recovered forced result,
        //                                     enumerated marked, recovered marked
        { "1",  null,             "0",  null,             true,  false },
        { null, null,             "1",  null,             false, true },
        { null, null,             null, BadConfiguration, false, true },
        { null, BadConfiguration, "0",  null,             true,  false },
        { "0",  null,             null, UnknownProperty,  false, false },
    };

    [Theory]
    [MemberData(nameof(Readings))]
    public async Task Each_listed_installation_carries_the_reading_taken_for_it(
        string? enumeratedInstanceType, uint? enumeratedInstanceTypeResult,
        string? recoveredInstanceType, uint? recoveredInstanceTypeResult,
        bool enumeratedMarked, bool recoveredMarked)
    {
        // The walk's row first, then the recovered row, each marked by its own reading
        // and by nothing the other installation answered. A positive reading and a read
        // that failed mark the row alike; an ordinary value and an absent one leave it
        // unmarked.
        var result = await Enumerate(
            enumeratedInstanceType: enumeratedInstanceType,
            enumeratedInstanceTypeResult: enumeratedInstanceTypeResult,
            recoveredInstanceType: recoveredInstanceType,
            recoveredInstanceTypeResult: recoveredInstanceTypeResult);

        Assert.Equal(
            new[] { (Enumerated, enumeratedMarked), (Recovered, recoveredMarked) },
            result.Installations.Select(i => (i.ProductCode, i.SecondCopyNotRuledOut)));
    }

    /// <summary>
    /// The census of <see cref="Enumerate"/>, the enumerated product's read answering
    /// as it is scripted.
    /// </summary>
    private static async Task<EnumerationCensus> Scan(
        string? enumeratedInstanceType = null,
        string? recoveredInstanceType = null,
        uint? recoveredInstanceTypeResult = null,
        bool recoveredUnaskable = false,
        int unparseableKeyNames = 0) =>
        (await Enumerate(enumeratedInstanceType, null, recoveredInstanceType, recoveredInstanceTypeResult,
            recoveredUnaskable, unparseableKeyNames)).Census;

    /// <param name="enumeratedInstanceType">
    /// What the product the machine-wide sweep DID return answers. Null leaves the
    /// property unset, which is the ordinary machine.
    /// </param>
    /// <param name="enumeratedInstanceTypeResult">
    /// A forced return code out of the enumerated product's read instead.
    /// </param>
    /// <param name="recoveredInstanceType">
    /// What the product the sweep lost answers once the registry has named it and
    /// Windows has confirmed it installed.
    /// </param>
    /// <param name="recoveredInstanceTypeResult">
    /// A forced return code out of that read instead, for the two fixtures whose
    /// subject is a read that did not produce a value.
    /// </param>
    /// <param name="recoveredUnaskable">
    /// The keyed question about the product the sweep lost meets a row that cannot be
    /// read, so Windows never says whether it is installed and it is never recovered.
    /// </param>
    /// <param name="unparseableKeyNames">
    /// Registry product keys whose names yield no code, as the fallback reports them.
    /// </param>
    private static async Task<InstallerQueryResult> Enumerate(
        string? enumeratedInstanceType = null,
        uint? enumeratedInstanceTypeResult = null,
        string? recoveredInstanceType = null,
        uint? recoveredInstanceTypeResult = null,
        bool recoveredUnaskable = false,
        int unparseableKeyNames = 0)
    {
        var msi = new FakeMsiApi();
        msi.AddProduct(Enumerated);
        msi.SetProductProperty(Enumerated, "LocalPackage", EnumeratedFile);
        msi.SetProductProperty(Enumerated, "ProductName", "A Program");
        if (enumeratedInstanceType is not null)
            msi.SetProductProperty(Enumerated, "InstanceType", enumeratedInstanceType);
        if (enumeratedInstanceTypeResult is { } enumeratedForced)
            msi.ProductPropertyResult[(Enumerated, "InstanceType")] = enumeratedForced;

        // The recovered product is deliberately NOT added to the enumeration. It reaches
        // the scan only through the registry comparison naming it and the keyed question
        // answering that it is installed, which is the whole population under test.
        if (recoveredInstanceType is not null)
            msi.SetProductProperty(Recovered, "InstanceType", recoveredInstanceType);
        if (recoveredInstanceTypeResult is { } forced)
            msi.ProductPropertyResult[(Recovered, "InstanceType")] = forced;
        if (recoveredUnaskable)
            msi.KeyedRowResult[(Recovered, 0)] = AccessDenied;

        var registryCodes = new[] { Enumerated, Recovered };
        var patchSets = new Dictionary<string, ProductPatchSet>(StringComparer.OrdinalIgnoreCase)
        {
            [Enumerated] = ProductPatchSet.AllNonRemovable,
            [Recovered] = ProductPatchSet.AllNonRemovable,
        };

        var result = await new InstallerQueryService(msi,
                (_, _) => new InstallerQueryService.FallbackRead(
                    0, registryCodes.Length + unparseableKeyNames,
                    RegistryProductCodes: registryCodes,
                    UnparseableProductKeyNames: unparseableKeyNames,
                    ProductPatchSets: patchSets),
                crashLogSink: null)
            .GetRegisteredPackagesAsync();

        return result;
    }
}
