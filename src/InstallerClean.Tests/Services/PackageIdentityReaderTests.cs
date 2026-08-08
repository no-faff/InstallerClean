using InstallerClean.Services;

namespace InstallerClean.Tests.Services;

/// <summary>
/// Unit tests for the two pure parts of <see cref="PackageIdentityReader"/>: the
/// GUID acceptance rule and the patch target-list split.
///
/// The rest of that class is msi.dll calls and cannot run off Windows. These two
/// can, and they are the parts worth pinning here, because BOTH FAIL IN THE
/// DIRECTION THAT COSTS A FILE. A value wrongly accepted becomes a code that is
/// put to Windows, comes back unrecognised because it was never a real code, and
/// permits an offer on the strength of a question that was never really asked.
/// Everything they reject is withheld instead, which costs nothing but space.
/// </summary>
public class PackageIdentityReaderTests
{
    [Theory]
    // The form both the API and the registry hand back, unchanged.
    [InlineData("{AAAAAAAA-BBBB-CCCC-DDDD-EEEEEEEEEEEE}", "{AAAAAAAA-BBBB-CCCC-DDDD-EEEEEEEEEEEE}")]
    // Case is not something a package author is obliged to get right, and two
    // spellings of one code have to reach the cache as one key.
    [InlineData("{aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee}", "{AAAAAAAA-BBBB-CCCC-DDDD-EEEEEEEEEEEE}")]
    [InlineData("{AaAaAaAa-bBbB-cCcC-dDdD-eEeEeEeEeEeE}", "{AAAAAAAA-BBBB-CCCC-DDDD-EEEEEEEEEEEE}")]
    public void Accepts_a_braced_guid_and_puts_it_in_one_spelling(string raw, string expected) =>
        Assert.Equal(expected, PackageIdentityReader.Canonicalise(raw));

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    // WHITESPACE-PADDED, AND THESE ARE THE ROWS WITH TEETH. Guid.TryParseExact
    // trims leading and trailing white space before parsing, which was measured
    // rather than assumed, so a padded value reaches it as a valid GUID and would
    // be accepted as the trimmed code. Whether Windows Installer registers such a
    // product under the padded form or the trimmed one is not established, and
    // asking about the wrong one gets "never heard of it" back, which permits an
    // offer. The exact-width test in front of the parse is what refuses them.
    [InlineData(" {AAAAAAAA-BBBB-CCCC-DDDD-EEEEEEEEEEEE}")]
    [InlineData("{AAAAAAAA-BBBB-CCCC-DDDD-EEEEEEEEEEEE} ")]
    [InlineData("\t{AAAAAAAA-BBBB-CCCC-DDDD-EEEEEEEEEEEE}\n")]
    // Every other GUID surface form is refused. The value read out of a package
    // is a ProductCode, which Windows Installer writes braced, and accepting the
    // others would be accepting a value this reading has no evidence about.
    [InlineData("AAAAAAAA-BBBB-CCCC-DDDD-EEEEEEEEEEEE")]
    [InlineData("(AAAAAAAA-BBBB-CCCC-DDDD-EEEEEEEEEEEE)")]
    // The unbraced 32-character form. It is a real identity in another spelling,
    // and nothing here has been shown to transform between the two.
    [InlineData("AAAAAAAABBBBCCCCDDDDEEEEEEEEEEEE")]
    // Truncated, over-long, and not hexadecimal at all.
    [InlineData("{AAAAAAAA-BBBB-CCCC-DDDD-EEEEEEEEEEE}")]
    [InlineData("{AAAAAAAA-BBBB-CCCC-DDDD-EEEEEEEEEEEEE}")]
    [InlineData("{ZZZZZZZZ-BBBB-CCCC-DDDD-EEEEEEEEEEEE}")]
    [InlineData("ProductCode")]
    public void Refuses_anything_that_is_not_a_braced_guid(string raw) =>
        Assert.Null(PackageIdentityReader.Canonicalise(raw));

    [Fact]
    public void Splits_a_template_into_target_products()
    {
        var targets = PackageIdentityReader.ParseTargets(
            "{AAAAAAAA-BBBB-CCCC-DDDD-EEEEEEEEEEEE};{11111111-2222-3333-4444-555555555555}");

        Assert.Equal(new[]
        {
            "{AAAAAAAA-BBBB-CCCC-DDDD-EEEEEEEEEEEE}",
            "{11111111-2222-3333-4444-555555555555}",
        }, targets);
    }

    [Theory]
    // A trailing separator carries no product, so it is nothing to fail over.
    [InlineData("{AAAAAAAA-BBBB-CCCC-DDDD-EEEEEEEEEEEE};")]
    [InlineData(";{AAAAAAAA-BBBB-CCCC-DDDD-EEEEEEEEEEEE}")]
    [InlineData("{AAAAAAAA-BBBB-CCCC-DDDD-EEEEEEEEEEEE};;")]
    public void Skips_empty_parts(string template) =>
        Assert.Equal(new[] { "{AAAAAAAA-BBBB-CCCC-DDDD-EEEEEEEEEEEE}" },
            PackageIdentityReader.ParseTargets(template));

    [Fact]
    public void An_empty_template_yields_no_targets_rather_than_a_failure()
    {
        // The split succeeded and found nothing, which is a different fact from a
        // value it could not read. The caller keeps the file either way; what
        // differs is which of the two it can honestly say happened.
        var targets = PackageIdentityReader.ParseTargets("");

        Assert.NotNull(targets);
        Assert.Empty(targets);
    }

    [Theory]
    // The installation-package reading of the same summary property. If a patch
    // ever carried it, the value means something this code does not understand,
    // and the safe response to that is to stop rather than to take the parts that
    // happen to parse.
    [InlineData("Intel;1033")]
    [InlineData("{AAAAAAAA-BBBB-CCCC-DDDD-EEEEEEEEEEEE};Intel")]
    [InlineData("not a guid")]
    // ONE BAD MEMBER REFUSES THE WHOLE LIST, and these are the rows that say so.
    // A target list short by one is a veto that does not fire for whatever that
    // target holds, so a partial parse would be worse than no parse. The middle
    // position is tested as well as the ends, a loop that gave up early having
    // exactly the same signature as one that refused.
    [InlineData("{AAAAAAAA-BBBB-CCCC-DDDD-EEEEEEEEEEEE} ;{11111111-2222-3333-4444-555555555555}")]
    [InlineData("{AAAAAAAA-BBBB-CCCC-DDDD-EEEEEEEEEEEE};nonsense;{11111111-2222-3333-4444-555555555555}")]
    [InlineData("{AAAAAAAA-BBBB-CCCC-DDDD-EEEEEEEEEEEE};{11111111-2222-3333-4444-555555555555};Intel")]
    // Unbraced, which is a real product code in another spelling and is refused
    // for the same reason a single unbraced value is.
    [InlineData("{AAAAAAAA-BBBB-CCCC-DDDD-EEEEEEEEEEEE};11111111-2222-3333-4444-555555555555")]
    public void Refuses_a_template_holding_anything_that_is_not_a_product_code(string template) =>
        Assert.Null(PackageIdentityReader.ParseTargets(template));
}
