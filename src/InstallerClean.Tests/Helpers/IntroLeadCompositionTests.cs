using System.Globalization;
using InstallerClean.Helpers;
using InstallerClean.Resources;

namespace InstallerClean.Tests.Helpers;

/// <summary>
/// The main window's first line, across all sixteen languages.
///
/// MainViewModel.IntroLead picks one of four strings and MainWindow's
/// BuildIntroLeadLine renders it, hyperlinking a phrase wrapped in <c>[ ]</c>.
/// Only the scanned state's lead carries a pair; the scan-failed,
/// nothing-scanned-yet and Windows-Installer-hold leads carry none and must
/// render verbatim as a single Run.
///
/// Neither half of that is visible to any existing gate. The brackets are
/// ordinary characters in a resx value, so a translator can drop one, add one or
/// move it, and check-resx-parity (key presence and placeholder arity) sees
/// nothing. The two ways it goes wrong are opposites and both reach a user:
/// a lead that should have no link growing a stray bracket renders the bracket
/// on screen as text, and the one lead that should have a link losing its pair
/// renders the sentence with no link at all, silently dropping the window's only
/// route to the safety reasoning.
///
/// The parse itself is CompositionParsing.SplitAtBracketedPhrase, covered for
/// its own edge cases in CompositionParsingTests; what is covered here is the
/// shipped text it is fed.
/// </summary>
public class IntroLeadCompositionTests
{
    /// <summary>
    /// The three leads that must render as plain text. Keys rather than typed
    /// accessors, because the assertion is about what each language ships and
    /// the lookup has to name a culture.
    /// </summary>
    private static readonly string[] PlainLeadKeys =
    {
        "Error.ScanFailedTitle",   // a scan failed, startup or Re-scan
        "Body.NotScanned.Lead",    // the startup scan was cancelled
        "Body.PendingReboot.Lead", // files found, but Windows Installer is busy
    };

    private const string LinkLeadKey = "Body.MainExplanation.Lead";

    public static TheoryData<string> Cultures()
    {
        var data = new TheoryData<string>();
        foreach (var name in SupportedLanguages.CultureNames) data.Add(name);
        return data;
    }

    [Theory]
    [MemberData(nameof(Cultures))]
    public void The_three_non_link_leads_render_as_plain_text(string cultureName)
    {
        var culture = CultureInfo.GetCultureInfo(cultureName);

        foreach (var key in PlainLeadKeys)
        {
            var value = Lead(key, culture);

            // No complete pair, so the window takes the single-Run arm.
            Assert.Null(CompositionParsing.SplitAtBracketedPhrase(value));

            // And no half of a pair either: an unmatched bracket leaves the
            // split returning null exactly as a clean sentence does, so the
            // assertion above cannot see one. It would paint on screen.
            Assert.DoesNotContain('[', value);
            Assert.DoesNotContain(']', value);
        }
    }

    [Theory]
    [MemberData(nameof(Cultures))]
    public void The_scanned_lead_carries_exactly_one_link_phrase(string cultureName)
    {
        var culture = CultureInfo.GetCultureInfo(cultureName);
        var value = Lead(LinkLeadKey, culture);

        var split = CompositionParsing.SplitAtBracketedPhrase(value);

        Assert.NotNull(split);
        Assert.NotEqual("", split!.LinkText);
        // The window builds its accessible name as prefix + link + suffix, so
        // the three parts have to reconstitute the sentence exactly, minus the
        // two brackets. A second pair would leave one of them in the suffix.
        Assert.Equal(value.Replace("[", "").Replace("]", ""),
            split.Prefix + split.LinkText + split.Suffix);
    }

    [Theory]
    [MemberData(nameof(Cultures))]
    public void The_link_phrase_is_a_phrase_rather_than_the_whole_sentence(string cultureName)
    {
        // The bracketed span is part of the sentence, not a destination: it
        // reads as words a person clicks mid-sentence. A translation that
        // wrapped the entire line would turn the whole lead into a hyperlink,
        // which no other language does and which reads as an error.
        var value = Lead(LinkLeadKey, CultureInfo.GetCultureInfo(cultureName));
        var split = CompositionParsing.SplitAtBracketedPhrase(value);

        Assert.NotNull(split);
        Assert.True(split!.Prefix.Length + split.Suffix.Length > 0,
            $"{cultureName}: the whole lead is inside the brackets");
    }

    /// <summary>
    /// Resolves a lead the way the window does: through the app's UI culture,
    /// so the installer-folder token is spent before anything counts brackets.
    /// The token's replacement is a real path and could in principle contain
    /// one, which is the reason for going through this door rather than reading
    /// the raw resource.
    /// </summary>
    private static string Lead(string key, CultureInfo culture)
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
