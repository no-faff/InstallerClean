using System.Globalization;
using InstallerClean.Resources;

namespace InstallerClean.Tests.Resources;

public class JapaneseResxParityTests
{
    // Guards against a neutral key being added without its Japanese translation,
    // which would silently render English inside the Japanese UI. Cli. keys are
    // excluded: the CLI is pinned to English, so although the ja satellite carries
    // coolvitto's Cli. translations they are dormant until CLI localisation ships
    // and are not part of the live GUI's completeness. The ja ResourceSet is read
    // with tryParents: false so a fallback to the neutral does not mask a genuinely
    // missing translation.
    [Fact]
    public void Every_non_cli_neutral_key_has_a_japanese_translation()
    {
        var neutral = Strings.ResourceManager.GetResourceSet(
            CultureInfo.InvariantCulture, createIfNotExists: true, tryParents: true)!;
        var japanese = Strings.ResourceManager.GetResourceSet(
            CultureInfo.GetCultureInfo("ja"), createIfNotExists: true, tryParents: false);

        Assert.NotNull(japanese);

        var missing = neutral.Cast<System.Collections.DictionaryEntry>()
            .Select(e => (string)e.Key)
            .Where(k => !k.StartsWith("Cli.", StringComparison.Ordinal))
            .Where(k => japanese!.GetString(k) is null)
            .OrderBy(k => k, StringComparer.Ordinal)
            .ToList();

        Assert.True(missing.Count == 0,
            $"{missing.Count} neutral key(s) have no Japanese translation: {string.Join(", ", missing)}");
    }
}
