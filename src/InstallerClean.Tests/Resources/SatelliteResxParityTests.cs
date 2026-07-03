using System.Globalization;
using InstallerClean.Helpers;
using InstallerClean.Resources;

namespace InstallerClean.Tests.Resources;

// One parity contract for every shipped satellite, read from the COMPILED
// resources rather than the resx sources (scripts/check-resx-parity.mjs covers
// those, plus placeholder arity): a satellite that drops out of the build, or
// a culture listed in SupportedLanguages without a working satellite, fails
// here even though its resx on disk looks complete.
public class SatelliteResxParityTests
{
    public static TheoryData<string> SatelliteCultures()
    {
        var data = new TheoryData<string>();
        foreach (var name in SupportedLanguages.CultureNames)
        {
            if (!string.Equals(name, SupportedLanguages.Neutral, StringComparison.OrdinalIgnoreCase))
                data.Add(name);
        }

        return data;
    }

    // Guards against a neutral key being added without a translation, which
    // would silently render English inside that language's UI. Cli. keys are
    // excluded here: the machine-read ones may legitimately be absent (the
    // satellite generators strip them), and the human-facing ones get the
    // stricter all-or-nothing check below. The satellite ResourceSet is read
    // with tryParents: false so a fallback to the neutral cannot mask a
    // genuinely missing translation.
    [Theory]
    [MemberData(nameof(SatelliteCultures))]
    public void Every_non_cli_neutral_key_has_a_translation(string cultureName)
    {
        var neutral = Strings.ResourceManager.GetResourceSet(
            CultureInfo.InvariantCulture, createIfNotExists: true, tryParents: true)!;
        var satellite = Strings.ResourceManager.GetResourceSet(
            CultureInfo.GetCultureInfo(cultureName), createIfNotExists: true, tryParents: false);

        Assert.NotNull(satellite);

        var missing = neutral.Cast<System.Collections.DictionaryEntry>()
            .Select(e => (string)e.Key)
            .Where(k => !k.StartsWith("Cli.", StringComparison.Ordinal))
            .Where(k => satellite!.GetString(k) is null)
            .OrderBy(k => k, StringComparer.Ordinal)
            .ToList();

        Assert.True(missing.Count == 0,
            $"{missing.Count} neutral key(s) have no {cultureName} translation: {string.Join(", ", missing)}");
    }

    // Once a satellite ships ANY Cli. key it must ship every human-facing one:
    // a half-translated CLI would render some lines in the OS language and
    // some in English. Every shipped satellite carries the CLI surface; the
    // no-Cli.-key skip stays for a future language that lands GUI-first (the
    // CLI falls back to neutral English until its first Cli. key arrives).
    // The machine-read event-log keys are exempt either way: the CLI forces
    // en-GB at the emit site (MachineContract), so a satellite may carry them
    // (coolvitto's ja does) or omit them (the generated satellites strip
    // them), and either is correct.
    [Theory]
    [MemberData(nameof(SatelliteCultures))]
    public void Every_human_facing_cli_key_is_translated_when_a_satellite_ships_the_cli(string cultureName)
    {
        var neutral = Strings.ResourceManager.GetResourceSet(
            CultureInfo.InvariantCulture, createIfNotExists: true, tryParents: true)!;
        var satellite = Strings.ResourceManager.GetResourceSet(
            CultureInfo.GetCultureInfo(cultureName), createIfNotExists: true, tryParents: false);

        Assert.NotNull(satellite);

        var shipsCli = satellite!.Cast<System.Collections.DictionaryEntry>()
            .Any(e => ((string)e.Key).StartsWith("Cli.", StringComparison.Ordinal));
        if (!shipsCli)
            return;

        var missing = neutral.Cast<System.Collections.DictionaryEntry>()
            .Select(e => (string)e.Key)
            .Where(IsHumanFacingCliKey)
            .Where(k => satellite.GetString(k) is null)
            .OrderBy(k => k, StringComparer.Ordinal)
            .ToList();

        Assert.True(missing.Count == 0,
            $"{missing.Count} human-facing Cli. key(s) untranslated in {cultureName}: {string.Join(", ", missing)}");
    }

    // Human-facing = a Cli. key that is NOT an Application-channel event-log
    // line. The machine set is exactly the Cli.EventLog* keys minus
    // Cli.EventLogUnavailable, which despite its prefix is an operator-facing
    // stdout warning and so is human.
    private static bool IsHumanFacingCliKey(string key) =>
        key.StartsWith("Cli.", StringComparison.Ordinal) &&
        (!key.Contains("EventLog", StringComparison.Ordinal) || key == "Cli.EventLogUnavailable");
}
