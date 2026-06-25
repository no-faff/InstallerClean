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

    // Once a satellite ships ANY Cli. key it must ship every human-facing one:
    // a half-translated CLI would render some lines Japanese and some English.
    // A satellite with no Cli. key at all is skipped (the CLI falls back to
    // neutral English until its first one lands). The machine-read event-log
    // keys are exempt either way: the CLI forces en-GB at the emit site
    // (MachineContract), so a satellite may carry them (coolvitto's ja does) or
    // omit them, and either is correct.
    [Fact]
    public void Every_human_facing_cli_key_is_translated_when_japanese_ships_the_cli()
    {
        var neutral = Strings.ResourceManager.GetResourceSet(
            CultureInfo.InvariantCulture, createIfNotExists: true, tryParents: true)!;
        var japanese = Strings.ResourceManager.GetResourceSet(
            CultureInfo.GetCultureInfo("ja"), createIfNotExists: true, tryParents: false);

        Assert.NotNull(japanese);

        var shipsCli = japanese!.Cast<System.Collections.DictionaryEntry>()
            .Any(e => ((string)e.Key).StartsWith("Cli.", StringComparison.Ordinal));
        if (!shipsCli)
            return;

        var missing = neutral.Cast<System.Collections.DictionaryEntry>()
            .Select(e => (string)e.Key)
            .Where(IsHumanFacingCliKey)
            .Where(k => japanese.GetString(k) is null)
            .OrderBy(k => k, StringComparer.Ordinal)
            .ToList();

        Assert.True(missing.Count == 0,
            $"{missing.Count} human-facing Cli. key(s) untranslated in Japanese: {string.Join(", ", missing)}");
    }

    // Human-facing = a Cli. key that is NOT an Application-channel event-log
    // line. The machine set is exactly the Cli.EventLog* keys minus
    // Cli.EventLogUnavailable, which despite its prefix is an operator-facing
    // stdout warning and so is human.
    private static bool IsHumanFacingCliKey(string key) =>
        key.StartsWith("Cli.", StringComparison.Ordinal) &&
        (!key.Contains("EventLog", StringComparison.Ordinal) || key == "Cli.EventLogUnavailable");
}
