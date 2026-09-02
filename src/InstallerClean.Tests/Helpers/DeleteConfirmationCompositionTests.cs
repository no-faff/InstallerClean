using System.Collections;
using System.Globalization;
using InstallerClean.Helpers;
using InstallerClean.Resources;

namespace InstallerClean.Tests.Helpers;

/// <summary>
/// The delete confirmation's body, across all sixteen languages.
///
/// ConfirmDeleteWindow's BuildBodyLine takes one of the two count forms from
/// DisplayHelpers.Pluralise and renders it, hyperlinking a phrase wrapped in
/// <c>[ ]</c>. Neither form carries a pair, in any language.
///
/// The brackets are ordinary characters in a resx value, so a translation can add
/// one, drop one or move it, and check-resx-parity, which reads key presence and
/// placeholder arity, is looking at something else. check-cross-key-rules reads
/// the resx files and compares each language's brackets against the neutral
/// sentence each key answers for, leaving a key the neutral has not got to
/// check-resx-parity; this reads the values through the door the app opens, with
/// the culture set, so what it asserts is what the dialog would be handed.
///
/// IntroLeadCompositionTests does this job for the main window's four leads. This
/// is the same job for the delete confirmation, which is the only value the app
/// splits whose form varies by count. None of the four leads goes through
/// DisplayHelpers.Pluralise and this value does, so the set read here is every
/// form Pluralise can choose and not only the pair the neutral declares.
///
/// The parse itself is CompositionParsing.SplitAtBracketedPhrase, covered for its
/// own edge cases in CompositionParsingTests; what is covered here is the shipped
/// text it is fed.
/// </summary>
public class DeleteConfirmationCompositionTests
{
    /// <summary>The resx key without the form suffix, as Pluralise is handed it.</summary>
    private const string Prefix = "Confirm.DeletePermanently";

    /// <summary>
    /// The CLDR categories a language may override, in the spelling
    /// DisplayHelpers.Pluralise builds the lookup from.
    /// </summary>
    private static readonly string[] CategorySuffixes = { ".One", ".Few", ".Many" };

    /// <summary>
    /// Every form the dialog can be handed in this language: the two count forms
    /// the neutral declares, plus any category override this language defines for
    /// the same prefix. Pluralise takes an override in preference to the pair, so a
    /// bracket in one reaches the dialog exactly as a bracket in the pair does.
    ///
    /// The overrides are enumerated from the language's own resource set rather
    /// than named here, because which language defines which is the language's
    /// decision and a fixed list would answer for the ones it happened to name on
    /// the day it was written. Keys rather than typed accessors throughout: an
    /// override has no accessor, and the assertion is about what each language
    /// ships, so the lookup has to name a culture.
    /// </summary>
    private static IEnumerable<string> BodyKeys(CultureInfo culture)
    {
        yield return $"{Prefix}.Singular";
        yield return $"{Prefix}.Plural";

        // tryParents: false, so this is what this language declares and not what it
        // inherits. A category key belongs in a satellite and the neutral declares
        // none; if one ever appeared there, Pluralise would serve it to every culture
        // through the ordinary fallback and the neutral's own case below would be the
        // one to fail.
        var own = Strings.ResourceManager.GetResourceSet(
            culture, createIfNotExists: true, tryParents: false);
        if (own is null) yield break;

        var overrides = own.Cast<DictionaryEntry>()
            .Select(e => (string)e.Key)
            .Where(k => CategorySuffixes.Any(suffix => k == Prefix + suffix))
            .OrderBy(k => k, StringComparer.Ordinal);
        foreach (var key in overrides) yield return key;
    }

    public static TheoryData<string> Cultures()
    {
        var data = new TheoryData<string>();
        foreach (var name in SupportedLanguages.CultureNames) data.Add(name);
        return data;
    }

    [Theory]
    [MemberData(nameof(Cultures))]
    public void Both_delete_confirmation_bodies_render_as_plain_text(string cultureName)
    {
        var culture = CultureInfo.GetCultureInfo(cultureName);
        var faults = new List<string>();

        foreach (var key in BodyKeys(culture))
        {
            var value = Body(key, culture);

            // No complete pair, so the dialog takes the single-Run arm. The key is
            // named in the message because the set read here varies by language: a
            // failure that named only the value would leave a reader working out
            // which form it came from.
            if (CompositionParsing.SplitAtBracketedPhrase(value) is not null)
            {
                faults.Add($"{key} splits at a bracketed phrase, so the dialog would "
                    + $"render a hyperlink in it: \"{value}\"");
            }

            // And no half of a pair either: an unmatched bracket leaves the split
            // returning null exactly as a clean sentence does, so the test above it
            // cannot see one. It would paint on screen. Reported only where the pair
            // did not fire, because a complete pair carries brackets as well and one
            // form is owed one line rather than two.
            else if (value.Contains('[') || value.Contains(']'))
            {
                faults.Add($"{key} carries a square bracket: \"{value}\"");
            }
        }

        // Collected across every form and asserted once, so one run names all of them.
        // The set read here is as long as the language's own declarations make it, and
        // an assertion inside the loop answers for the first form alone.
        Assert.True(faults.Count == 0, $"{cultureName}: {string.Join("; ", faults)}");
    }

    /// <summary>
    /// Resolves a body the way the dialog does, through the app's UI culture, so
    /// any token in the value is spent before anything counts brackets.
    /// </summary>
    private static string Body(string key, CultureInfo culture)
    {
        using var scope = new LocalisationScope(culture);
        return Strings.Get(key);
    }

    private sealed class LocalisationScope : IDisposable
    {
        public LocalisationScope(CultureInfo culture) => Localisation.Set(culture, culture);

        public void Dispose() => Localisation.Reset();
    }
}
