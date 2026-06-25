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

    // Once a satellite ships ANY Cli. key it must ship every human-facing one:
    // a half-translated CLI would render some lines Italian and some English.
    // A satellite with no Cli. key at all is skipped, so it.resx passes until
    // the Italian CLI strings land. The machine-read event-log keys are exempt
    // either way: the CLI forces en-GB at the emit site (MachineContract), so a
    // satellite may carry them or omit them, and either is correct.
    [Fact]
    public void Every_human_facing_cli_key_is_translated_when_italian_ships_the_cli()
    {
        var neutral = Strings.ResourceManager.GetResourceSet(
            CultureInfo.InvariantCulture, createIfNotExists: true, tryParents: true)!;
        var italian = Strings.ResourceManager.GetResourceSet(
            CultureInfo.GetCultureInfo("it"), createIfNotExists: true, tryParents: false);

        Assert.NotNull(italian);

        var shipsCli = italian!.Cast<System.Collections.DictionaryEntry>()
            .Any(e => ((string)e.Key).StartsWith("Cli.", StringComparison.Ordinal));
        if (!shipsCli)
            return;

        var missing = neutral.Cast<System.Collections.DictionaryEntry>()
            .Select(e => (string)e.Key)
            .Where(IsHumanFacingCliKey)
            .Where(k => italian.GetString(k) is null)
            .OrderBy(k => k, StringComparer.Ordinal)
            .ToList();

        Assert.True(missing.Count == 0,
            $"{missing.Count} human-facing Cli. key(s) untranslated in Italian: {string.Join(", ", missing)}");
    }

    // Human-facing = a Cli. key that is NOT an Application-channel event-log
    // line. The machine set is exactly the Cli.EventLog* keys minus
    // Cli.EventLogUnavailable, which despite its prefix is an operator-facing
    // stdout warning and so is human.
    private static bool IsHumanFacingCliKey(string key) =>
        key.StartsWith("Cli.", StringComparison.Ordinal) &&
        (!key.Contains("EventLog", StringComparison.Ordinal) || key == "Cli.EventLogUnavailable");
}
