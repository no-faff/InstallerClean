using System.Globalization;
using InstallerClean.Helpers;
using InstallerClean.Resources;

namespace InstallerClean.Tests.Helpers;

/// <summary>
/// Which language a counted sentence takes its plural rule from: the language the WORDS
/// came from, which is not the same as the culture the machine is running under.
///
/// HOW THE TWO PART COMPANY. The text comes out of whichever satellite the
/// ResourceManager's fallback chain reached, and a culture with no satellite on its
/// chain gets the English neutral. The rule is chosen by two-letter code, and that
/// switch has arms for the languages the app ships and for their whole two-letter
/// family alike. So taking it from the REQUESTED culture would apply one language's
/// arm to a sentence written in another: Traditional Chinese (zh-TW, zh-HK, zh-MO,
/// zh-Hant and bare zh) displays English, and the "zh" arm answers Other at every
/// count, which would spell "1 files"; European Portuguese (pt-PT and bare pt, the
/// satellite being pt-BR, which is not on their chain) displays English, and the "pt"
/// arm makes zero singular, which would spell "0 file".
///
/// WHICH CULTURES DISPLAY ENGLISH IS ALREADY PINNED, in SupportedLanguagesTests, which
/// puts zh-TW and pt-PT in the same theory with a comment on each saying so. This file
/// is the other half of that: what the plural rule then does with them.
///
/// WHY IT ITERATES WHAT IT ITERATES, WHICH IS THE PART TO CARRY FORWARD. Every other
/// localisation theory here walks SupportedLanguages.CultureNames, and that is by
/// definition the set of languages that HAVE a satellite, so a rule taken from the
/// wrong language cannot appear anywhere in it. This walks every culture the framework
/// knows and DERIVES the divergent set rather than listing it: the cultures whose own
/// rule and whose displayed language's rule disagree. That is a question that can be
/// asked again when a sixteenth language moves the boundary, where a list cannot.
/// </summary>
public class PluralRuleLanguageTests
{
    /// <summary>
    /// Counts spread across every band the rules distinguish: zero, exactly one, the
    /// two-to-four paucal, the eleven-to-fourteen exception East Slavic carves out of
    /// it, a twenty-one that is One in Russian and Many in Polish, and a hundred-and-one.
    /// </summary>
    private static readonly int[] Counts = { 0, 1, 2, 5, 11, 21, 101 };

    /// <summary>
    /// The prefix every observation below is made through, chosen for one property:
    /// no language defines a CLDR override for it, so what comes back is the category
    /// the rule selected and nothing else.
    /// <see cref="The_observation_prefix_carries_no_override_in_any_language"/> is what
    /// keeps that true, because a translation round adding one would quietly turn every
    /// assertion here into a reading of the override instead.
    /// </summary>
    private const string Observed = "Completion.HeldBack";

    private const string One = "SINGULAR FORM";
    private const string Other = "PLURAL FORM";

    /// <summary>
    /// What the app would render for <paramref name="count"/> with the language pinned
    /// to <paramref name="culture"/>. Sentinels rather than resx values, so the answer
    /// says which form was picked and not what any language happens to word it as.
    /// </summary>
    private static string FormPicked(CultureInfo culture, int count)
    {
        using var scope = new LocalisationScope(culture);
        return DisplayHelpers.Pluralise(count, One, Other, Observed);
    }

    /// <summary>The language whose satellite the app will actually display for <paramref name="culture"/>.</summary>
    private static CultureInfo Displayed(CultureInfo culture) =>
        CultureInfo.GetCultureInfo(SupportedLanguages.Active(culture));

    private static string FormExpectedOf(CultureInfo language, int count) =>
        DisplayHelpers.CategoryFor(language, count, DisplayHelpers.QuestionFor(Observed))
            == DisplayHelpers.PluralCategory.One ? One : Other;

    /// <summary>
    /// Every culture this framework build knows about. Deliberately NOT
    /// <see cref="SupportedLanguages.CultureNames"/>: that is the list of languages
    /// with a satellite, and a rule taken from the wrong language can only happen
    /// where there is no satellite to take it from.
    /// </summary>
    private static CultureInfo[] EveryCulture() =>
        CultureInfo.GetCultures(CultureTypes.AllCultures)
            .Where(c => !string.IsNullOrEmpty(c.Name))
            .ToArray();

    [Fact]
    public void The_rule_comes_from_the_language_the_words_come_from()
    {
        var cultures = EveryCulture();
        var wrong = new List<string>();

        // Counted while walking rather than asserted from a list: the cultures where
        // the two answers CAN disagree are the whole subject of this file, and a walk
        // that met none of them would pass without having tested anything. The
        // must-hit control is at the bottom, on the count this loop takes.
        var couldDisagree = 0;

        foreach (var culture in cultures)
        {
            var displayed = Displayed(culture);
            var divergent = false;

            foreach (var count in Counts)
            {
                var expected = FormExpectedOf(displayed, count);
                if (expected != FormExpectedOf(culture, count)) divergent = true;

                var actual = FormPicked(culture, count);
                if (actual != expected)
                    wrong.Add($"{culture.Name} at {count}: displays {displayed.Name} and wants "
                        + $"{expected}, picked {actual}");
            }

            if (divergent) couldDisagree++;
        }

        Assert.True(wrong.Count == 0,
            $"{wrong.Count} of {cultures.Length * Counts.Length} readings took their plural rule "
            + $"from a language other than the one on screen:{Environment.NewLine}"
            + string.Join(Environment.NewLine, wrong.Take(20)));

        // The must-hit control, and it is not a formality. If the app ever shipped a
        // satellite for every culture whose two-letter code the rule switch has an arm
        // for, this loop would be asserting something that cannot fail, and it would
        // report clean for exactly the same reason it does now.
        Assert.True(couldDisagree > 0,
            $"{cultures.Length} cultures walked and not one of them could have taken a rule from "
            + "the wrong language, so this test proved nothing. Either the rule switch lost its "
            + "arms or every language it names now ships a satellite.");
    }

    [Fact]
    public void The_two_families_that_display_English_get_English_plurals()
    {
        // The same property as the walk above, said in the words a user would read it
        // in, and against the real file noun rather than a sentinel. Safe to read the
        // resx forms here precisely because these cultures reach no satellite: the
        // neutral defines no CLDR override, so nothing can come back but the form the
        // rule picked. The_neutral_defines_no_override pins that.
        string[] displaysEnglish =
        {
            "zh-TW", "zh-HK", "zh-MO", "zh-Hant", "zh", // Traditional Chinese: no satellite ships
            "pt-PT", "pt",                              // the Portuguese satellite is pt-BR only
        };

        foreach (var name in displaysEnglish)
        {
            var culture = CultureInfo.GetCultureInfo(name);
            Assert.Equal(SupportedLanguages.Neutral, SupportedLanguages.Active(culture));

            using var scope = new LocalisationScope(culture);

            Assert.Equal(Strings.Plural_File_Singular, DisplayHelpers.PluraliseFile(1));
            Assert.Equal(Strings.Plural_File_Plural, DisplayHelpers.PluraliseFile(0));
            Assert.Equal(Strings.Plural_File_Plural, DisplayHelpers.PluraliseFile(2));
            Assert.Equal(Strings.Plural_File_Plural, DisplayHelpers.PluraliseFile(21));
        }
    }

    [Theory]
    // Simplified Chinese resolves through the zh-Hans parent, so these DO display
    // Chinese and keep the no-inflection rule. They are one parent hop from the family
    // above and take the opposite answer, which is why the rule cannot be a two-letter
    // comparison in either direction.
    [InlineData("zh-CN", "zh-Hans")]
    [InlineData("zh-SG", "zh-Hans")]
    [InlineData("pt-BR", "pt-BR")]
    [InlineData("fr-CA", "fr")]
    [InlineData("it-CH", "it")]
    [InlineData("ru-RU", "ru")]
    [InlineData("uk-UA", "uk")]
    [InlineData("pl-PL", "pl")]
    public void A_culture_that_reaches_a_satellite_keeps_that_satellites_rule(
        string culture, string expectedLanguage)
    {
        // The other side of the same fix, and the reason it is here: taking the rule
        // from the displayed language has to leave every case that already worked
        // exactly as it was, including the ones that arrive through a parent.
        var asked = CultureInfo.GetCultureInfo(culture);
        Assert.Equal(expectedLanguage, SupportedLanguages.Active(asked));

        var language = CultureInfo.GetCultureInfo(expectedLanguage);
        foreach (var count in Counts)
            Assert.Equal(FormExpectedOf(language, count), FormPicked(asked, count));
    }

    /// <summary>
    /// Whether <paramref name="key"/> resolves for <paramref name="culture"/>, asked
    /// through the same door <c>Pluralise</c> asks its override question through, so
    /// the controls below cannot answer differently from the code they are controlling.
    /// </summary>
    private static bool Resolves(CultureInfo culture, string key)
    {
        using var scope = new LocalisationScope(culture);
        return Strings.Find(key) is not null;
    }

    [Fact]
    public void The_observation_prefix_carries_no_override_in_any_language()
    {
        // The control under the walk. Pluralise returns a satellite's CLDR override
        // ahead of the form it was handed, so an override on this prefix would replace
        // the sentinel with a translated string and every comparison above would be
        // reading the override rather than the rule.
        var found = new List<string>();

        foreach (var name in SupportedLanguages.CultureNames)
            foreach (var form in new[] { "One", "Few", "Many" })
                if (Resolves(CultureInfo.GetCultureInfo(name), $"{Observed}.{form}"))
                    found.Add($"{name}: {Observed}.{form}");

        Assert.True(found.Count == 0,
            $"{string.Join(", ", found)} now exists, so this file is reading an override rather "
            + $"than the form the rule picked. Point {nameof(Observed)} at a prefix nothing "
            + "overrides.");

        // Must-miss and must-hit on the same reader, so a lookup that had stopped
        // answering could not make the sweep above look clean.
        Assert.False(Resolves(CultureInfo.GetCultureInfo("ru"), $"{Observed}.NoSuchForm"));
        Assert.True(Resolves(CultureInfo.GetCultureInfo("ru"), "Plural.File.Few"));
    }

    [Fact]
    public void The_neutral_defines_no_override_so_an_English_screen_reads_the_rule_directly()
    {
        // What lets the second test above read the real resx forms. Every CLDR override
        // is satellite-only by design, so a culture whose chain ends at the neutral can
        // never be handed one, whatever prefix it asks about.
        var neutral = CultureInfo.GetCultureInfo(SupportedLanguages.Neutral);
        string[] carriesOverridesSomewhere =
            { "Plural.File", "Cli.MovedFiles", "Status.RegisteredPackagesFound" };

        foreach (var prefix in carriesOverridesSomewhere)
            foreach (var form in new[] { "One", "Few", "Many" })
                Assert.False(Resolves(neutral, $"{prefix}.{form}"));

        // The must-hit half: these three prefixes were picked because a satellite DOES
        // override them, so the falses above are the neutral's answer and not a lookup
        // that has stopped finding anything.
        Assert.True(Resolves(CultureInfo.GetCultureInfo("pl"), "Plural.File.Few"));
        Assert.True(Resolves(CultureInfo.GetCultureInfo("fr"), "Cli.MovedFiles.One"));
        Assert.True(Resolves(CultureInfo.GetCultureInfo("de"), "Status.RegisteredPackagesFound.One"));
    }

    /// <summary>
    /// Pins the app's language for one assertion and drops it again. The override is
    /// process-global; the assembly runs its tests serially (AssemblyInfo.cs) so a pin
    /// is never visible to a test running alongside, but one left set would rewrite
    /// every string expected after it.
    /// </summary>
    private sealed class LocalisationScope : IDisposable
    {
        public LocalisationScope(CultureInfo culture) => Localisation.Set(culture, culture);

        public void Dispose() => Localisation.Reset();
    }
}
