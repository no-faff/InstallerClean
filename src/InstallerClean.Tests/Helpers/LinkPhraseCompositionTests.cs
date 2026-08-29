using System.Collections;
using System.Globalization;
using InstallerClean.Helpers;
using InstallerClean.Resources;

namespace InstallerClean.Tests.Helpers;

/// <summary>
/// Every sentence the app renders with a link inside it, across all sixteen
/// languages.
///
/// Four windows hand a resx value to CompositionParsing.SplitAtBracketedPhrase and
/// hyperlink the phrase wrapped in <c>[ ]</c>. The brackets are ordinary characters
/// in a resx value, so a translation can drop one, add one or add a second pair, and
/// check-resx-parity, which reads key presence and placeholder arity, is looking at
/// something else. The two ways it goes wrong are opposites and both reach a user: a
/// sentence that has lost its pair renders as plain text with the link silently gone,
/// and one that has grown a stray bracket paints the bracket on screen.
///
/// THE SUBJECTS ARE DERIVED FROM THE NEUTRAL'S OWN PUNCTUATION rather than listed
/// here, which is how check-cross-key-rules builds the same set: a key is a subject
/// because the English sentence carries a bracket, so a fifth linked sentence is
/// covered the day somebody writes it and nothing below has to be edited. Membership
/// is any bracket rather than a well-formed pair, so an unbalanced English value is a
/// failure here rather than a key that quietly leaves the rule.
///
/// A DERIVED SET CAN GO EMPTY, AND A GREEN RUN OVER NOTHING READS EXACTLY LIKE A
/// CLEAN ONE. So the floor below asserts the set is not empty and names the sentences
/// that link today. That is a floor and not the list: nothing has to be added to it
/// for a new sentence to be covered.
///
/// check-cross-key-rules reads the resx files. This reads the values through the door
/// the app opens, with the culture set, so what it asserts is what the window would be
/// handed: the installer-folder token is spent before anything counts brackets, and a
/// language that declares none of its own is served the neutral, which is what that
/// language would render.
///
/// IntroLeadCompositionTests covers the main window's four leads and asserts more
/// about the one that links than its bracket shape;
/// DeleteConfirmationCompositionTests covers the dialog whose value must carry no
/// pair at all, which is the opposite rule. The overlap on the lead costs nothing.
///
/// The parse itself is CompositionParsing.SplitAtBracketedPhrase, covered for its own
/// edge cases in CompositionParsingTests; what is covered here is the shipped text it
/// is fed.
/// </summary>
public class LinkPhraseCompositionTests
{
    /// <summary>
    /// The sentences that carry a link phrase in English. The floor the derived set
    /// is held to, so a derivation that found nothing cannot pass by finding nothing
    /// wrong with it.
    /// </summary>
    private static readonly string[] KnownLinkKeys =
    {
        "Body.MainExplanation.Lead",              // the main window's scanned lead
        "Body.RegisteredMissingFromDisk.SeeAlso", // the registered-files window
        "ConfirmSendResultLog.Reassurance",       // the send-report confirmation
    };

    /// <summary>
    /// Every neutral key whose English value carries a bracket. Read raw and at the
    /// neutral culture: this is the question of which sentences the English marks as
    /// linking, which is one answer for the whole app rather than one per language.
    /// </summary>
    private static IReadOnlyList<string> LinkKeys()
    {
        var neutral = Strings.ResourceManager.GetResourceSet(
            CultureInfo.InvariantCulture, createIfNotExists: true, tryParents: true)!;

        return neutral.Cast<DictionaryEntry>()
            .Where(e => e.Value is string value
                && (value.Contains('[') || value.Contains(']')))
            .Select(e => (string)e.Key)
            .OrderBy(key => key, StringComparer.Ordinal)
            .ToList();
    }

    public static TheoryData<string> Cultures()
    {
        var data = new TheoryData<string>();
        foreach (var name in SupportedLanguages.CultureNames) data.Add(name);
        return data;
    }

    [Fact]
    public void The_derived_set_holds_every_sentence_that_links_in_english()
    {
        var derived = LinkKeys();

        Assert.NotEmpty(derived);
        foreach (var key in KnownLinkKeys)
            Assert.Contains(key, derived);
    }

    [Theory]
    [MemberData(nameof(Cultures))]
    public void Every_linked_sentence_carries_exactly_one_link_phrase(string cultureName)
    {
        var culture = CultureInfo.GetCultureInfo(cultureName);
        var faults = new List<string>();

        foreach (var key in LinkKeys())
        {
            var value = Sentence(key, culture);
            var split = CompositionParsing.SplitAtBracketedPhrase(value);

            if (split is null)
            {
                faults.Add($"{key} carries no complete [phrase], so the window would "
                    + $"render the sentence with no link in it: \"{value}\"");
                continue;
            }

            if (split.LinkText.Length == 0)
            {
                faults.Add($"{key} brackets an empty phrase, so the link would have no "
                    + $"words to sit on: \"{value}\"");
                continue;
            }

            // The window builds its line, and its accessible name, as prefix + link +
            // suffix, so the three parts have to reconstitute the sentence minus its
            // two brackets. A second pair leaves one of them in the suffix.
            if (split.Prefix + split.LinkText + split.Suffix
                != value.Replace("[", "").Replace("]", ""))
            {
                faults.Add($"{key} carries more than one bracket pair, so a bracket "
                    + $"would paint on screen: \"{value}\"");
            }
        }

        // Collected across the whole set and asserted once, so one run names every
        // sentence a language gets wrong. An assertion inside the loop stops at the
        // first and the rest of that language goes unread.
        Assert.True(faults.Count == 0, $"{cultureName}: {string.Join("; ", faults)}");
    }

    /// <summary>
    /// Resolves a sentence the way its window does, through the app's UI culture, so
    /// the installer-folder token is spent before anything counts brackets. The
    /// token's replacement is a real path and could in principle carry a bracket of
    /// its own, which is the reason for going through this door rather than reading
    /// the raw resource.
    /// </summary>
    private static string Sentence(string key, CultureInfo culture)
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
