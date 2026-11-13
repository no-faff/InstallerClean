using System.Collections;
using System.Globalization;
using System.Resources;
using InstallerClean.Helpers;
using InstallerClean.Resources;

namespace InstallerClean.Tests.Resources;

/// <summary>
/// StartCheckResources reads the embedded resources without a ResourceManager, so it has to
/// answer as the ResourceManager behind Strings does: for every key, in every language the app
/// ships, in a regional culture whose language it ships, and in a culture it does not ship,
/// where a key a language lacks comes from the neutral strings.
/// </summary>
public class StartCheckResourcesTests
{
    public static TheoryData<string> Cultures()
    {
        var data = new TheoryData<string>();
        foreach (var culture in SupportedLanguages.CultureNames) data.Add(culture);
        foreach (var culture in new[] { "en-GB", "en-US", "de-AT", "fr-CA", "pt-PT", "zh-CN", "zh-TW", "sv-SE" })
            data.Add(culture);
        return data;
    }

    [Theory]
    [MemberData(nameof(Cultures))]
    public void Every_key_reads_as_the_ResourceManager_reads_it(string name)
    {
        var culture = CultureInfo.GetCultureInfo(name);
        var keys = Strings.ResourceManager.GetResourceSet(CultureInfo.InvariantCulture, true, true)!
            .Cast<DictionaryEntry>().Select(entry => (string)entry.Key).ToList();
        Assert.NotEmpty(keys);
        foreach (var key in keys)
            Assert.Equal(Strings.ResourceManager.GetString(key, culture), StartCheckResources.GetString(key, culture));
    }

    [Fact]
    public void A_key_no_resources_hold_reads_as_null()
    {
        Assert.Null(StartCheckResources.GetString("No.Such.Key", CultureInfo.GetCultureInfo("fr")));
    }

    [Fact]
    public void Its_resources_are_those_Strings_reads()
    {
        Assert.Equal(StartCheckResources.BaseName, Strings.ResourceManager.BaseName);
    }
}
