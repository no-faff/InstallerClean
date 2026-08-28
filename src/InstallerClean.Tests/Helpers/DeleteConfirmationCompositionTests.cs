using System.Globalization;
using InstallerClean.Helpers;
using InstallerClean.Resources;

namespace InstallerClean.Tests.Helpers;

/// <summary>
/// The delete confirmation's body, across all sixteen languages.
///
/// ConfirmDeleteWindow's BuildBodyLine takes one of the two count forms from
/// DisplayHelpers.Pluralise and renders it, hyperlinking a phrase wrapped in
/// <c>[ ]</c>. Neither form carries a pair, in any language: the dialog is modal
/// over the main window, which states the same claim and links it into the
/// README's "Is it safe?" section, so a bracketed phrase here would put two
/// copies of one link on screen together, one live and one behind glass that
/// cannot be clicked.
///
/// The brackets are ordinary characters in a resx value, so a translation can add
/// one, drop one or move it, and check-resx-parity, which reads key presence and
/// placeholder arity, is looking at something else. check-cross-key-rules reads
/// the resx files and compares each language's brackets against the neutral's;
/// this reads the values through the door the app opens, with the culture set, so
/// what it asserts is what the dialog would be handed.
///
/// IntroLeadCompositionTests does this job for the main window's four leads. This
/// is the same job for the delete confirmation, which is the only value the app
/// splits that varies by count, so both forms are read rather than whichever one
/// a count happens to select.
///
/// The parse itself is CompositionParsing.SplitAtBracketedPhrase, covered for its
/// own edge cases in CompositionParsingTests; what is covered here is the shipped
/// text it is fed.
/// </summary>
public class DeleteConfirmationCompositionTests
{
    /// <summary>
    /// Both count forms. Keys rather than typed accessors, because the assertion
    /// is about what each language ships and the lookup has to name a culture.
    /// </summary>
    private static readonly string[] BodyKeys =
    {
        "Confirm.DeletePermanently.Singular",
        "Confirm.DeletePermanently.Plural",
    };

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

        foreach (var key in BodyKeys)
        {
            var value = Body(key, culture);

            // No complete pair, so the dialog takes the single-Run arm.
            Assert.Null(CompositionParsing.SplitAtBracketedPhrase(value));

            // And no half of a pair either: an unmatched bracket leaves the split
            // returning null exactly as a clean sentence does, so the assertion
            // above cannot see one. It would paint on screen.
            Assert.DoesNotContain('[', value);
            Assert.DoesNotContain(']', value);
        }
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
