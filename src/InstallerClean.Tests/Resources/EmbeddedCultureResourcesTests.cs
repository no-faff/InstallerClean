using System.Globalization;
using InstallerClean.Helpers;
using InstallerClean.Resources;

namespace InstallerClean.Tests.Resources;

// The languages are resources inside the assembly that holds Strings, read by
// EmbeddedCultureResources, and no satellite assembly is built for any of them.
public class EmbeddedCultureResourcesTests
{
    private static EmbeddedCultureResources Manager => (EmbeddedCultureResources)Strings.ResourceManager;

    private static string? AllClean(string culture) =>
        Manager.GetString("Completion.AllClean", CultureInfo.GetCultureInfo(culture));

    [Fact]
    public void The_assembly_holds_one_resource_for_each_supported_language_but_the_neutral()
    {
        var expected = SupportedLanguages.CultureNames
            .Where(name => name != SupportedLanguages.Neutral)
            .OrderBy(name => name, StringComparer.Ordinal);

        Assert.Equal(expected, Manager.Cultures.OrderBy(name => name, StringComparer.Ordinal));
    }

    [Fact]
    public void No_satellite_assembly_of_the_apps_is_built_beside_the_assembly()
    {
        // The test platform's own satellite assemblies sit in the same culture folders. The
        // domain's base folder is the build's output; the assembly itself can be loaded from a
        // shadow copy elsewhere.
        var folder = AppDomain.CurrentDomain.BaseDirectory;

        Assert.Empty(Directory.GetFiles(folder, "InstallerClean*.resources.dll", SearchOption.AllDirectories));
    }

    [Theory]
    [InlineData("fr-CA", "fr")]
    [InlineData("zh-CN", "zh-Hans")]
    [InlineData("zh-TW", "zh-Hant")]
    public void A_culture_with_no_resource_of_its_own_reads_its_parents(string culture, string parent)
    {
        Assert.NotEqual(AllClean(SupportedLanguages.Neutral), AllClean(parent));
        Assert.Equal(AllClean(parent), AllClean(culture));
    }

    [Theory]
    [InlineData("en-US")]
    [InlineData("pt-PT")]
    [InlineData("zh")]
    [InlineData("sv")]
    public void A_culture_with_no_resource_on_its_parent_chain_reads_the_neutral_strings(string culture)
    {
        Assert.Equal(AllClean(SupportedLanguages.Neutral), AllClean(culture));
        Assert.Null(Manager.GetResourceSet(CultureInfo.GetCultureInfo(culture), createIfNotExists: true, tryParents: false));
    }

    [Fact]
    public void The_neutral_language_reads_the_neutral_strings_without_trying_parents()
    {
        var neutral = Manager.GetResourceSet(CultureInfo.InvariantCulture, createIfNotExists: true, tryParents: false);
        var named = Manager.GetResourceSet(
            CultureInfo.GetCultureInfo(SupportedLanguages.Neutral), createIfNotExists: true, tryParents: false);

        Assert.NotNull(neutral);
        Assert.Same(neutral, named);
    }

    [Fact]
    public void A_key_a_language_does_not_hold_is_answered_from_the_neutral_strings()
    {
        var neutral = Manager.GetResourceSet(CultureInfo.InvariantCulture, createIfNotExists: true, tryParents: false)!;
        var german = Manager.GetResourceSet(CultureInfo.GetCultureInfo("de"), createIfNotExists: true, tryParents: false)!;
        var key = neutral.Cast<System.Collections.DictionaryEntry>()
            .Select(entry => (string)entry.Key)
            .Where(name => german.GetString(name) is null)
            .OrderBy(name => name, StringComparer.Ordinal)
            .First();

        Assert.Equal(neutral.GetString(key), Manager.GetString(key, CultureInfo.GetCultureInfo("de")));
    }

    [Fact]
    public void A_key_no_resource_holds_reads_null()
    {
        Assert.Null(Manager.GetString("No.Such.Key", CultureInfo.GetCultureInfo("fr")));
    }

    [Fact]
    public void A_language_is_read_again_after_its_resources_are_released()
    {
        var before = AllClean("ja");

        Manager.ReleaseAllResources();

        Assert.Equal(before, AllClean("ja"));
    }
}
