using InstallerClean.Models;
using InstallerClean.Services;

namespace InstallerClean.Tests.Services;

/// <summary>
/// WHICH PRODUCTS GET ASKED WHETHER THEY ARE A SECOND INSTANCE OF THEMSELVES.
///
/// The reading itself is pinned beside the enumeration's own loop, where the spellings
/// and the failure direction are held. What is here is the OTHER population, and the
/// reason it needs a file of its own is that it was the population nobody asked: the
/// property read sits inside the loop over the products the machine-wide enumeration
/// returned, and a product that enumeration lost is recovered afterwards by name, asked
/// whether it is installed and asked about every patch it holds, and never asked this.
///
/// THAT IS THE POPULATION MOST LIKELY TO HOLD THE CONDITION, which is what makes the gap
/// worth closing rather than noting. The machine this whole class of work is about is one
/// carrying the same program twice, and the sibling file on the recovered-product
/// condition opens by describing exactly that machine: a copy the sweep does not return
/// and the registry does.
///
/// READ WHAT EACH FIXTURE SETS UP. Every one of them differs from its neighbour in a
/// single property reading, so a count that moved for any other reason would show up as
/// the wrong pair moving together.
/// </summary>
public class InstallerQueryServiceSecondInstanceTests
{
    private const string Enumerated = "{AAAAAAAA-0000-0000-0000-00000000000A}";
    private const string Recovered = "{BBBBBBBB-0000-0000-0000-00000000000B}";
    private const string EnumeratedFile = @"C:\Windows\Installer\enumerated.msi";

    private const uint BadConfiguration = 1610;
    private const uint UnknownProperty = 1608;

    [Fact]
    public async Task A_recovered_product_that_is_a_second_instance_of_itself_is_counted()
    {
        // ARM THREE, AND THE WHOLE OF IT. Before this the answer here was zero, because
        // nothing put the question to a recovered product at all.
        var census = await Scan(recoveredInstanceType: "1");

        Assert.Equal(1, census.RecoveredProductCount);
        Assert.Equal(1, census.InstanceProductCount);
        Assert.Equal(0, census.InstanceTypeUnreadableCount);
        Assert.True(census.SecondInstanceNotRuledOut);
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
        Assert.False(census.SecondInstanceNotRuledOut);
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
        Assert.False(census.SecondInstanceNotRuledOut);
    }

    [Fact]
    public async Task A_recovered_product_that_would_not_answer_withholds_on_the_same_terms()
    {
        // A question put and not answered, on the population that had never been asked
        // one. It reaches the unreadable count and not the positive one, which is the
        // only honest place for it: nothing has been established either way, and the
        // rule reads the two together.
        var census = await Scan(recoveredInstanceTypeResult: BadConfiguration);

        Assert.Equal(1, census.RecoveredProductCount);
        Assert.Equal(0, census.InstanceProductCount);
        Assert.Equal(1, census.InstanceTypeUnreadableCount);
        Assert.True(census.SecondInstanceNotRuledOut);
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

    /// <param name="enumeratedInstanceType">
    /// What the product the machine-wide sweep DID return answers. Null leaves the
    /// property unset, which is the ordinary machine.
    /// </param>
    /// <param name="recoveredInstanceType">
    /// What the product the sweep lost answers once the registry has named it and
    /// Windows has confirmed it installed.
    /// </param>
    /// <param name="recoveredInstanceTypeResult">
    /// A forced return code out of that read instead, for the two fixtures whose
    /// subject is a read that did not produce a value.
    /// </param>
    private static async Task<EnumerationCensus> Scan(
        string? enumeratedInstanceType = null,
        string? recoveredInstanceType = null,
        uint? recoveredInstanceTypeResult = null)
    {
        var msi = new FakeMsiApi();
        msi.AddProduct(Enumerated);
        msi.SetProductProperty(Enumerated, "LocalPackage", EnumeratedFile);
        msi.SetProductProperty(Enumerated, "ProductName", "A Program");
        if (enumeratedInstanceType is not null)
            msi.SetProductProperty(Enumerated, "InstanceType", enumeratedInstanceType);

        // The recovered product is deliberately NOT added to the enumeration. It reaches
        // the scan only through the registry comparison naming it and the keyed question
        // answering that it is installed, which is the whole population under test.
        if (recoveredInstanceType is not null)
            msi.SetProductProperty(Recovered, "InstanceType", recoveredInstanceType);
        if (recoveredInstanceTypeResult is { } forced)
            msi.ProductPropertyResult[(Recovered, "InstanceType")] = forced;

        var registryCodes = new[] { Enumerated, Recovered };
        var patchSets = new Dictionary<string, ProductPatchSet>(StringComparer.OrdinalIgnoreCase)
        {
            [Enumerated] = ProductPatchSet.AllNonRemovable,
            [Recovered] = ProductPatchSet.AllNonRemovable,
        };

        var result = await new InstallerQueryService(msi,
                (_, _) => new InstallerQueryService.FallbackRead(
                    0, registryCodes.Length,
                    RegistryProductCodes: registryCodes,
                    ProductPatchSets: patchSets),
                crashLogSink: null)
            .GetRegisteredPackagesAsync();

        return result.Census;
    }
}
