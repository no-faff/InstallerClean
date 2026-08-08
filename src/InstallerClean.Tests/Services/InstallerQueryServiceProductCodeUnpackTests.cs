using InstallerClean.Services;

namespace InstallerClean.Tests.Services;

/// <summary>
/// <see cref="InstallerQueryService.UnpackRegistryProductCode"/>, which turns a
/// UserData subkey name back into the braced GUID the Windows Installer API
/// answers in.
///
/// IT IS THE WHOLE COMPARISON'S FOUNDATION, which is why it is pinned on its own.
/// The scan decides whether an enumeration came back short by asking whether the
/// registry names a product the API never returned, and a transform that is
/// merely close would answer that question with fabricated product codes: every
/// registry key would look like a product the API had missed, every one of them
/// would then be put to Windows as a question, and a machine in perfect health
/// would read as a machine whose enumeration had collapsed.
///
/// THE EXPECTED VALUES ARE MEASURED, NOT COMPOSED. Each packed/unpacked pair below
/// was read off one elevated machine's SOFTWARE hive (2026-08-08), where 136 of
/// its 137 product keys unpacked to exactly the GUID inside their own
/// InstallProperties UninstallString and none disagreed. A test whose expectations
/// were written by applying the same reading of the format that the code applies
/// would pass on a shared misunderstanding.
/// </summary>
public class InstallerQueryServiceProductCodeUnpackTests
{
    [Theory]
    // A product key and its own UninstallString GUID, from that hive.
    [InlineData("1926E8D15D0BCE53481466615F760A7F", "{1D8E6291-B0D5-35EC-8441-6616F567A0F7}")]
    // Two patch keys from the same hive, which are packed identically. The app
    // only unpacks product keys today; the format is one format, and a reading
    // that held for one kind of key and not the other would be a coincidence.
    [InlineData("4D54076CED4F5BA32BBD3E5FAD1CD4C9", "{C67045D4-F4DE-3AB5-B2DB-E3F5DAC14D9C}")]
    [InlineData("2D0058F6F08A743309184BE1178C95B2", "{6F8500D2-A80F-3347-9081-B41E71C8592B}")]
    public void A_packed_key_name_unpacks_to_the_code_the_machine_records_for_it(
        string packed, string expected)
    {
        Assert.Equal(expected, InstallerQueryService.UnpackRegistryProductCode(packed));
    }

    /// <summary>
    /// The control that makes the three above mean something. Each field of the
    /// packed form is rearranged differently, so a transform that got one field's
    /// rule wrong still produces a well-formed GUID of the right length; only
    /// comparing against a DIFFERENT key's answer shows that the rules are not
    /// interchangeable.
    /// </summary>
    [Fact]
    public void One_character_different_is_a_different_product()
    {
        var real = InstallerQueryService.UnpackRegistryProductCode("1926E8D15D0BCE53481466615F760A7F");
        var altered = InstallerQueryService.UnpackRegistryProductCode("1926E8D15D0BCE53481466615F760A7E");

        Assert.NotNull(real);
        Assert.NotNull(altered);
        Assert.NotEqual(real, altered);
    }

    [Theory]
    [InlineData("")]
    [InlineData("1926E8D15D0BCE53481466615F760A7")]    // 31: one short
    [InlineData("1926E8D15D0BCE53481466615F760A7FF")]  // 33: one over
    [InlineData("1926E8D15D0BCE53481466615F760A7G")]   // 32, and G is not hex
    [InlineData("{1D8E6291-B0D5-35EC-8441-6616F567A0F7}")]  // already unpacked
    public void A_name_that_is_not_a_packed_guid_yields_nothing(string name)
    {
        // Refusing matters more than it looks. The caller turns every code this
        // returns into a question about a real machine, and a code invented out of
        // a key name that never was one asks about a product that does not exist:
        // the answer withholds, so a guess here costs the user files that were
        // safe.
        Assert.Null(InstallerQueryService.UnpackRegistryProductCode(name));
    }

    /// <summary>
    /// Hex case is not normalised, and nothing downstream needs it to be: the set
    /// these go into and the comparison against the enumerated codes are both
    /// case-insensitive. Pinned because a future normalisation would be a silent
    /// behaviour change if anything ever compared these ordinally.
    /// </summary>
    [Fact]
    public void Lower_case_input_unpacks_to_the_same_code_ignoring_case()
    {
        var upper = InstallerQueryService.UnpackRegistryProductCode("1926E8D15D0BCE53481466615F760A7F");
        var lower = InstallerQueryService.UnpackRegistryProductCode("1926e8d15d0bce53481466615f760a7f");

        Assert.NotNull(lower);
        Assert.Equal(upper, lower, StringComparer.OrdinalIgnoreCase);
    }
}
