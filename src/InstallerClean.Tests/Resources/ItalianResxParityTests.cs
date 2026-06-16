using System.Globalization;
using InstallerClean.Resources;

namespace InstallerClean.Tests.Resources;

public class ItalianResxParityTests
{
    // Guards against a neutral key being added without its Italian translation,
    // which would silently render English inside the Italian UI. Cli. keys are
    // excluded by design: the CLI ships English (the satellite omits them). The
    // it ResourceSet is read with tryParents: false so a fallback to the neutral
    // does not mask a genuinely missing translation.
    [Fact]
    public void Every_non_cli_neutral_key_has_an_italian_translation()
    {
        var neutral = Strings.ResourceManager.GetResourceSet(
            CultureInfo.InvariantCulture, createIfNotExists: true, tryParents: true)!;
        var italian = Strings.ResourceManager.GetResourceSet(
            CultureInfo.GetCultureInfo("it"), createIfNotExists: true, tryParents: false);

        Assert.NotNull(italian);

        var missing = neutral.Cast<System.Collections.DictionaryEntry>()
            .Select(e => (string)e.Key)
            .Where(k => !k.StartsWith("Cli.", StringComparison.Ordinal))
            .Where(k => italian!.GetString(k) is null)
            .OrderBy(k => k, StringComparer.Ordinal)
            .ToList();

        Assert.True(missing.Count == 0,
            $"{missing.Count} neutral key(s) have no Italian translation: {string.Join(", ", missing)}");
    }
}
