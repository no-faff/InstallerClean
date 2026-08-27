using System.Collections;
using System.Globalization;
using InstallerClean.Helpers;
using InstallerClean.Resources;

namespace InstallerClean.Tests.Helpers;

/// <summary>
/// The counted strings: every sentence whose wording changes with a number, and
/// the satellite-only CLDR overrides that inflect it. DisplayHelpersTests covers
/// which category a count falls in; nothing covered whether that category then
/// resolves to anything, which is where this file sits.
///
/// Two failure modes drive it, and neither reaches a build error, a parity
/// failure (check-resx-parity.mjs reads key presence and placeholder arity) or
/// an English screen.
///
/// A keyPrefix is a bare string passed alongside the resx forms, so one that
/// does not match its key spells no error anywhere: the override lookup simply
/// misses, and all fifteen languages keep the uninflected form at once. The
/// first test walks the whole inventory against the shipped resources.
///
/// The mirror of it is an override whose prefix matches nothing the code passes.
/// A translator writing "Completion.HeldBak.One" adds a key that parity
/// accepts, that nothing reads and that can never be selected. The last test
/// walks every override in all fifteen satellites back to a prefix in the
/// inventory, so an orphan names itself.
/// </summary>
public class CountedStringTests
{
    private static readonly CultureInfo British = CultureInfo.GetCultureInfo("en-GB");
    private static readonly CultureInfo Russian = CultureInfo.GetCultureInfo("ru");

    /// <summary>
    /// Every keyPrefix the app passes to <see cref="DisplayHelpers.Pluralise(int, string, string, string)"/>,
    /// taken from the call sites rather than from the resx, because the prefix
    /// is what the lookup is built from.
    ///
    /// A COUNT OF HOW MANY ARRIVE BY WHICH ROUTE USED TO STAND HERE AND WENT STALE
    /// TWICE, once when a release cut two held-back causes and added one, and again
    /// when the four collapsed into Completion.HeldBack. The warning it carried is
    /// what matters and is kept without the figure: a prefix that reaches Pluralise
    /// in a LOCAL rather than as a literal at the call site is invisible to a sweep
    /// of the call expressions, which is how this list once ran four prefixes short
    /// with nothing failing. Walk the resx for Singular and Plural pairs as well as
    /// the call sites.
    /// </summary>
    private static readonly string[] CountedPrefixes =
    {
        "Cli.DeletedFiles", "Cli.DeletingFiles", "Cli.FoundOrphans", "Cli.MissingFromDisk",
        "Cli.MovedFiles", "Cli.MovingFiles", "Cli.NothingOffered",
        "Completion.FailedCount", "Completion.FailedCountDelete",
        "Completion.HeldBack",
        "Completion.MoveCancelledSummary", "Completion.MoveRestoreHint",
        "Completion.MoveRestoreHintSameDrive", "Completion.MoveSummary",
        "Completion.NothingOfferedBody",
        "Completion.PermanentDeleteCancelledSummary", "Completion.PermanentDeleteSummary",
        "Confirm.DeletePermanently",
        "Error.AccessDenied", "Error.FileInUse", "Error.IOFailure", "Error.UnknownError",
        "Plural.Error", "Plural.File", "Plural.Package", "Plural.Patch", "Plural.Product",
        "Status.RegisteredPackagesFound",
        "Summary.MissingFromDisk", "Summary.MissingFromDisk.OtherPrograms",
        "Summary.MissingFromDisk.Unnamed",
        "Summary.NothingListed",
        "Summary.OrphanedToCleanUp", "Summary.RegisteredStillUsed",
        "Summary.RegisteredWindow",
    };

    /// <summary>
    /// The counted strings 3.0.0 adds: the ones whose singular form no shipped
    /// build has ever rendered.
    /// </summary>
    private static readonly string[] NewInThisRelease =
    {
        "Cli.DeletedFiles", "Cli.DeletingFiles", "Cli.FoundOrphans", "Cli.MissingFromDisk",
        "Cli.MovedFiles", "Cli.MovingFiles",
        "Cli.NothingOffered",
        "Completion.HeldBack",
        "Completion.MoveRestoreHint", "Completion.MoveRestoreHintSameDrive",
        "Completion.NothingOfferedBody",
        "Completion.PermanentDeleteSummary",
        "Confirm.DeletePermanently", "Error.FileInUse",
        "Summary.MissingFromDisk.OtherPrograms", "Summary.MissingFromDisk.Unnamed",
        "Summary.NothingListed",
        "Summary.RegisteredStillUsed", "Summary.RegisteredWindow",
    };

    private static readonly string[] CategorySuffixes = { ".One", ".Few", ".Many" };

    public static TheoryData<string> AllPrefixes() => Data(CountedPrefixes);

    public static TheoryData<string> NewPrefixes() => Data(NewInThisRelease);

    [Theory]
    [MemberData(nameof(AllPrefixes))]
    public void Every_counted_prefix_names_a_real_string(string prefix)
    {
        // A prefix matching nothing is the silent one: the override lookup
        // misses in every language at once, and English, where the string was
        // written and read, looks perfect.
        using var scope = new LocalisationScope(British);
        var (singular, plural) = Forms(prefix);

        Assert.False(string.IsNullOrEmpty(singular), $"{prefix}: neither a .Singular nor a flat key");
        Assert.False(string.IsNullOrEmpty(plural), $"{prefix}: a .Singular with no .Plural");
    }

    [Theory]
    [MemberData(nameof(NewPrefixes))]
    public void A_new_counted_string_renders_its_singular_at_one(string prefix)
    {
        // Pinned rather than left on the ambient culture: Pluralise reads the
        // app's UI language, so a runner whose culture was French would take
        // zero as singular and this would read as a product failure.
        using var scope = new LocalisationScope(British);
        var (singular, plural) = Forms(prefix);

        Assert.Equal(singular, DisplayHelpers.Pluralise(1, singular, plural, prefix));
        Assert.Equal(plural, DisplayHelpers.Pluralise(2, singular, plural, prefix));
        Assert.Equal(plural, DisplayHelpers.Pluralise(0, singular, plural, prefix));
    }

    [Fact]
    public void A_satellite_One_override_is_selected_at_a_count_of_one()
    {
        // The assertion the override mechanism rests on, and the one thing no
        // other test reaches. The Russian base form is the impersonal plural, so
        // without the override a single file reads with the wrong agreement.
        //
        // IT WAS WRITTEN AGAINST Completion.ReverifySkipped, WHOSE OVERRIDE WENT
        // WITH THE HELD-BACK COLLAPSE. Cli.FoundOrphans is the same shape and not a
        // convenience: a flat key, grammatical, and carrying a Russian .One that
        // differs from its base form, which is what the assertion below needs.
        using var scope = new LocalisationScope(Russian);

        var overridden = Strings.Find("Cli.FoundOrphans.One");
        var flat = Strings.Get("Cli.FoundOrphans");
        Assert.NotNull(overridden);
        Assert.NotEqual(flat, overridden); // guards the test itself: the two must differ

        Assert.Equal(overridden, DisplayHelpers.Pluralise(1, flat, "Cli.FoundOrphans"));
        // 21 is "one" in East Slavic, which is why the override is read by
        // category rather than by count.
        Assert.Equal(overridden, DisplayHelpers.Pluralise(21, flat, "Cli.FoundOrphans"));
    }

    [Fact]
    public void A_satellite_Few_override_is_selected_only_in_its_own_band()
    {
        using var scope = new LocalisationScope(Russian);

        var few = Strings.Find("Plural.File.Few");
        var (singular, plural) = Forms("Plural.File");
        Assert.NotNull(few);
        Assert.NotEqual(plural, few); // guards the test itself

        Assert.Equal(few, DisplayHelpers.Pluralise(2, singular, plural, "Plural.File"));
        Assert.Equal(few, DisplayHelpers.Pluralise(24, singular, plural, "Plural.File"));
        // 11 is "many" despite ending in 1, and no language ships a .Many
        // override, so this is the fallback arm in the same test.
        Assert.Equal(plural, DisplayHelpers.Pluralise(11, singular, plural, "Plural.File"));
    }

    [Fact]
    public void An_absent_override_falls_back_to_the_resx_form()
    {
        // Most counted strings in most languages take this path. It is the
        // ordinary case rather than an error one: the pair in the satellite
        // already reads correctly and no extra category form is wanted.
        using var scope = new LocalisationScope(Russian);
        Assert.Null(Strings.Find("Confirm.DeletePermanently.One"));

        var (singular, plural) = Forms("Confirm.DeletePermanently");

        Assert.Equal(singular, DisplayHelpers.Pluralise(1, singular, plural, "Confirm.DeletePermanently"));
    }

    [Theory]
    [MemberData(nameof(SatelliteCultures))]
    public void Every_satellite_override_belongs_to_a_counted_prefix(string cultureName)
    {
        var known = new HashSet<string>(CountedPrefixes, StringComparer.Ordinal);
        var satellite = Strings.ResourceManager.GetResourceSet(
            CultureInfo.GetCultureInfo(cultureName), createIfNotExists: true, tryParents: false);
        Assert.NotNull(satellite);

        var orphans = satellite!.Cast<DictionaryEntry>()
            .Select(e => (string)e.Key)
            .Where(k => CategorySuffixes.Any(s => k.EndsWith(s, StringComparison.Ordinal)))
            .Where(k => !known.Contains(k[..k.LastIndexOf('.')]))
            .OrderBy(k => k, StringComparer.Ordinal)
            .ToList();

        Assert.True(orphans.Count == 0,
            $"{cultureName}: {orphans.Count} override(s) name a prefix nothing passes to Pluralise, "
            + $"so they can never be selected: {string.Join(", ", orphans)}");
    }

    /// <summary>
    /// The five noun slots, and the only part of the classification that cannot be
    /// read off the strings. Every other counted prefix declares itself: a one-form
    /// with a <c>{0}</c> in it shows the reader a numeral and has to agree with it,
    /// one without asserts oneness in words. These five carry no numeral and are
    /// still grammatical, because they ARE the noun a numeral elsewhere governs.
    /// PluraliseFile(21) is "файл" in Russian and that is right.
    /// </summary>
    private static readonly string[] NounSlots =
    {
        "Plural.Error", "Plural.File", "Plural.Package", "Plural.Patch", "Plural.Product",
    };

    /// <summary>
    /// The counted prefixes whose chosen string reaches a screen without being handed
    /// to string.Format, so a satellite override carrying a numeric placeholder would
    /// render the placeholder itself. Taken from the call sites: ConfirmDeleteWindow
    /// uses the returned string raw, CompletionViewModel returns MoveRestoreText
    /// straight out, and FileOperationError.LocalisedGroupHeading returns what
    /// Pluralise gave it.
    /// </summary>
    private static readonly string[] UnformattedPrefixes =
    {
        "Completion.MoveRestoreHint", "Completion.MoveRestoreHintSameDrive",
        "Confirm.DeletePermanently",
        "Error.AccessDenied", "Error.FileInUse", "Error.IOFailure", "Error.UnknownError",
    };

    [Theory]
    [MemberData(nameof(AllPrefixes))]
    public void Every_counted_prefix_has_a_class(string prefix)
    {
        // QuestionFor has no default arm, so this is the whole test: an unclassified
        // prefix throws here rather than being absorbed into one class or the other.
        // Without it the inventory would have the shape that has caught this project
        // out repeatedly, a list that is silently short and reports success anyway.
        var question = DisplayHelpers.QuestionFor(prefix);

        Assert.True(Enum.IsDefined(question), $"{prefix}: classified as {(int)question}");
    }

    [Fact]
    public void A_counted_prefix_with_no_class_throws_rather_than_taking_one()
    {
        // The control on the test above. Were there a default arm, every prefix would
        // "have a class" and that theory would pass over a list of any length.
        var ex = Assert.Throws<ArgumentOutOfRangeException>(
            () => DisplayHelpers.QuestionFor("Summary.NoSuchCountedStringAsThis"));

        Assert.Contains("QuestionFor", ex.Message, StringComparison.Ordinal);
    }

    [Theory]
    [MemberData(nameof(AllPrefixes))]
    public void The_class_of_a_counted_prefix_is_the_one_its_one_form_implies(string prefix)
    {
        // Derived from the shipped neutral rather than listed twice, so a new counted
        // string whose singular asserts oneness in words cannot be classified as
        // grammatical without this failing and somebody having to think about it.
        using var scope = new LocalisationScope(British);
        var (singular, _) = Forms(prefix);

        var expected =
            NounSlots.Contains(prefix) || singular.Contains("{0}", StringComparison.Ordinal)
                ? DisplayHelpers.CountQuestion.Grammatical
                : DisplayHelpers.CountQuestion.Cardinality;

        Assert.Equal(expected, DisplayHelpers.QuestionFor(prefix));
    }

    [Theory]
    [MemberData(nameof(CardinalityPrefixInEveryLanguage))]
    public void A_cardinality_string_takes_its_singular_at_one_and_at_no_other_count(
        string cultureName, string prefix)
    {
        // The two faults this file could not see, in every language at once. Before
        // the split, id/tr/vi/ko/ja/zh-Hans could never reach the singular at all and
        // read "these files will be permanently deleted" over a single file, while
        // ru/uk reached it at 21, 31 and 41 and told the reader one file was going
        // when twenty-one were.
        using var scope = new LocalisationScope(CultureInfo.GetCultureInfo(cultureName));
        var (singular, plural) = Forms(prefix);

        // Guards the test itself. A language whose two forms are byte-identical would
        // pass every assertion below without rendering anything, and a cardinality
        // string that cannot say "one" in that language is a finding, not a pass.
        Assert.NotEqual(singular, plural);

        var atOne = Strings.Find($"{prefix}.One") ?? singular;
        Assert.Equal(atOne, DisplayHelpers.Pluralise(1, singular, plural, prefix));

        foreach (var count in new[] { 0, 2, 3, 5, 11, 21, 22, 31, 41, 101, 121 })
        {
            Assert.False(
                DisplayHelpers.Pluralise(count, singular, plural, prefix) == singular,
                $"{cultureName} {prefix}: the one-form, which asserts there is a single "
                + $"file, rendered at a count of {count}");
        }
    }

    [Theory]
    [InlineData("ru")]
    [InlineData("uk")]
    public void At_twenty_one_East_Slavic_takes_the_plural_sentence_and_the_singular_noun(
        string cultureName)
    {
        // The fix and the thing the fix must not break, pinned side by side, because
        // the tempting way to fix the sentence is to change CategoryFor, and that
        // would render "21 файлов" where the language wants "21 файл".
        using var scope = new LocalisationScope(CultureInfo.GetCultureInfo(cultureName));

        foreach (var prefix in CountedPrefixes.Where(IsCardinality))
        {
            var (singular, plural) = Forms(prefix);
            Assert.Equal(plural, DisplayHelpers.Pluralise(21, singular, plural, prefix));
            Assert.Equal(singular, DisplayHelpers.Pluralise(1, singular, plural, prefix));
        }

        var noun = Forms("Plural.File");
        Assert.Equal(noun.Singular, DisplayHelpers.PluraliseFile(21));
        Assert.Equal(noun.Singular, DisplayHelpers.PluraliseFile(1));
        Assert.Equal(Strings.Find("Plural.File.Few"), DisplayHelpers.PluraliseFile(3));
    }

    [Theory]
    [InlineData("pl")]
    [InlineData("ru")]
    [InlineData("uk")]
    public void The_cardinality_selector_keeps_its_paucal_band(string cultureName)
    {
        // THE GUARD ON THE ONE THING HERE THAT LOOKS LIKE IT COULD BE SIMPLIFIED. A
        // cardinality string answers a yes-or-no question, so collapsing everything
        // that is not One into Other reads as the tidier version of it.
        //
        // THIS PINS THE SELECTOR AND NO LONGER A RENDERING, AND THE REASON IS THE POINT
        // OF THE NOTE. It used to assert that Completion.NothingOfferedBody.Few read at
        // two to four files in these three languages. That override is gone: the key's
        // noun moved into a slot Plural.File fills, which left the .Few value
        // character-identical to its own .Plural and therefore a copy waiting to drift.
        //
        // SO NO CARDINALITY KEY SHIPS A .Few OR .Many OVERRIDE ANYWHERE TODAY, and the
        // honest consequence is that collapsing the band would currently change nothing
        // a user sees. That is exactly why the pin has to sit on the selector: the arm
        // is now correct-in-principle with no live consumer, which is the state in which
        // somebody deletes it. A satellite may add such an override at any time, the
        // key's own resx note invites it, and the collapse would silently disarm it.
        //
        // Plural.File.Few is NOT what this guards. That prefix is Grammatical, so it
        // goes through CategoryFor and never through CardinalCategoryFor, and it would
        // survive the collapse untouched.
        using var scope = new LocalisationScope(CultureInfo.GetCultureInfo(cultureName));

        var culture = CultureInfo.GetCultureInfo(cultureName);
        const string Prefix = "Completion.NothingOfferedBody";
        Assert.Equal(DisplayHelpers.CountQuestion.Cardinality, DisplayHelpers.QuestionFor(Prefix));

        // The override really is absent, so a later reader cannot mistake this for a
        // test that merely stopped looking at one.
        Assert.Null(Strings.Find($"{Prefix}.Few"));

        // The band itself, which is what a collapse into Other would take away.
        Assert.Equal(DisplayHelpers.PluralCategory.Few, DisplayHelpers.CardinalCategoryFor(culture, 3));
        Assert.Equal(DisplayHelpers.PluralCategory.Few, DisplayHelpers.CardinalCategoryFor(culture, 22));
        Assert.Equal(DisplayHelpers.PluralCategory.Many, DisplayHelpers.CardinalCategoryFor(culture, 5));

        // And the two ends, so the band is pinned between them rather than on its own:
        // One is exactly one in every language, and a CLDR one that is not one file
        // becomes Other rather than Many.
        Assert.Equal(DisplayHelpers.PluralCategory.One, DisplayHelpers.CardinalCategoryFor(culture, 1));
        Assert.Equal(
            cultureName == "pl" ? DisplayHelpers.PluralCategory.Many : DisplayHelpers.PluralCategory.Other,
            DisplayHelpers.CardinalCategoryFor(culture, 21));
    }

    [Theory]
    [MemberData(nameof(EveryCulture))]
    public void The_two_selectors_differ_at_exactly_the_counts_the_language_says(string cultureName)
    {
        // The whole change, characterised rather than sampled: every count from 0 to
        // 200, and the complete list of the ones where asking "is it exactly one"
        // parts company with asking "what form does the noun take".
        //
        // Polish is the control and it is the empty case. Its CLDR one is already
        // strictly n == 1, so the split must not move it at any count, and if it ever
        // does this reads as a difference where the language expects none.
        var culture = CultureInfo.GetCultureInfo(cultureName);
        var counts = Enumerable.Range(0, 201).ToList();

        var differing = counts
            .Where(n => DisplayHelpers.CardinalCategoryFor(culture, n)
                        != DisplayHelpers.CategoryFor(culture, n))
            .ToList();

        // The default arm FAILS rather than absorbs: a language added later whose one
        // form is not n == 1 lands here with a non-empty difference and an empty
        // expectation, and says so.
        IEnumerable<int> expected = culture.TwoLetterISOLanguageName switch
        {
            // 21, 31, 41 ... are CLDR "one" in East Slavic and are not one file.
            "ru" or "uk" => counts.Where(n => n != 1 && n % 10 == 1 && n % 100 != 11),
            // French and Portuguese take the singular at zero, which is a rule about a
            // numeral in front of a noun and not about there being one of something.
            "fr" or "pt" => new[] { 0 },
            // No count inflection at all, so the one-form was unreachable at every
            // count, one included.
            "id" or "ja" or "ko" or "tr" or "vi" or "zh" => new[] { 1 },
            _ => Array.Empty<int>(),
        };

        Assert.Equal(expected.ToList(), differing);
    }

    [Theory]
    [MemberData(nameof(SatelliteCultures))]
    public void No_override_on_an_unformatted_string_carries_a_numeric_placeholder(
        string cultureName)
    {
        // These seven reach a screen without going through string.Format, so an
        // override written with a {0} in it would put the braces themselves in front
        // of a reader. Nothing carries one today, and that is currently an accident of
        // nobody having written one; this is what makes it a rule.
        var satellite = Strings.ResourceManager.GetResourceSet(
            CultureInfo.GetCultureInfo(cultureName), createIfNotExists: true, tryParents: false);
        Assert.NotNull(satellite);

        var offenders = satellite!.Cast<DictionaryEntry>()
            .Where(e => CategorySuffixes.Any(suffix =>
                ((string)e.Key).EndsWith(suffix, StringComparison.Ordinal)))
            .Where(e => UnformattedPrefixes.Contains(
                ((string)e.Key)[..((string)e.Key).LastIndexOf('.')], StringComparer.Ordinal))
            .Where(e => HasNumericPlaceholder(e.Value as string ?? string.Empty))
            .Select(e => (string)e.Key)
            .OrderBy(k => k, StringComparer.Ordinal)
            .ToList();

        Assert.True(offenders.Count == 0,
            $"{cultureName}: {offenders.Count} override(s) on a string whose call site "
            + $"never formats it, so the placeholder would reach the screen as braces: "
            + string.Join(", ", offenders));
    }

    private static bool HasNumericPlaceholder(string value)
    {
        for (int i = 0; i + 1 < value.Length; i++)
            if (value[i] == '{' && char.IsAsciiDigit(value[i + 1])) return true;

        return false;
    }

    private static bool IsCardinality(string prefix) =>
        DisplayHelpers.QuestionFor(prefix) == DisplayHelpers.CountQuestion.Cardinality;

    /// <summary>
    /// Every cardinality prefix in every shipped language. The prefixes are read back
    /// out of <see cref="DisplayHelpers.QuestionFor"/> rather than listed here, so the
    /// theory covers whatever the app actually classifies that way and the two cannot
    /// drift apart.
    /// </summary>
    public static TheoryData<string, string> CardinalityPrefixInEveryLanguage()
    {
        var data = new TheoryData<string, string>();
        foreach (var culture in SupportedLanguages.CultureNames)
        {
            foreach (var prefix in CountedPrefixes.Where(IsCardinality))
                data.Add(culture, prefix);
        }

        return data;
    }

    public static TheoryData<string> EveryCulture() => Data(SupportedLanguages.CultureNames);

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

    /// <summary>
    /// The two shapes a counted string takes: a <c>.Singular</c>/<c>.Plural</c>
    /// pair, or one flat value used at every count and inflected only by the
    /// overrides. Read from the resources rather than listed, so the theories
    /// carry one inventory instead of two that could drift apart. Resolves at
    /// the pinned language, which every caller sets before calling.
    /// </summary>
    private static (string Singular, string Plural) Forms(string prefix)
    {
        var singular = Strings.Find($"{prefix}.Singular");
        if (singular is not null)
            return (singular, Strings.Find($"{prefix}.Plural") ?? "");

        var flat = Strings.Find(prefix) ?? "";
        return (flat, flat);
    }

    private static TheoryData<string> Data(IEnumerable<string> values)
    {
        var data = new TheoryData<string>();
        foreach (var v in values) data.Add(v);
        return data;
    }

    /// <summary>
    /// Pins the app's language for one test and drops it on the way out. The
    /// override is process-global; the assembly runs its tests serially
    /// (AssemblyInfo.cs), so a pin is never visible to a test alongside.
    /// </summary>
    private sealed class LocalisationScope : IDisposable
    {
        public LocalisationScope(CultureInfo culture) => Localisation.Set(culture, culture);

        public void Dispose() => Localisation.Reset();
    }
}
