using System.Globalization;
using System.Text.RegularExpressions;
using InstallerClean.Helpers;
using InstallerClean.Resources;

namespace InstallerClean.Tests.Helpers;

/// <summary>
/// The counted strings AS RENDERED, which is the one thing nothing else in this
/// repository looks at.
///
/// WHY NOTHING ELSE CAN SEE THIS CLASS OF FAULT. CountedStringTests selects a
/// template and compares it against another template; it never hands one to
/// string.Format, so a value whose placeholders have moved satisfies every
/// assertion in it. check-resx-parity.mjs errors on a placeholder a satellite
/// provides and the base does not, so a value that OMITS one passes. And a build
/// error needs an argument count that is too small, which the app's call site
/// never produces. Between them, a template can lose its size placeholder, or
/// render its noun where the size belongs, with the suite green and the gate
/// clean.
///
/// WHAT IT PINS, AND IT IS A FAULT THAT SHIPPED. Completion.NothingOfferedBody
/// spelled its counted noun into the value, so the noun could not follow the
/// numeral standing in front of it. Russian and Ukrainian take the nominative
/// singular after 21, 31 and 41, so both read "21 файлов" where the language
/// wants "21 файл", at every such count. Polish was correct throughout, which is
/// why it is here: a fix that moved Polish would be wrong.
///
/// The assertions are on the OUTPUT STRING rather than on which form was
/// selected, because selecting the right form and then rendering it with the
/// wrong argument is the failure being guarded, and a selector-level assertion
/// cannot tell the two apart.
/// </summary>
public class CountedStringRenderTests
{
    private const long Bytes = 12_345_678;

    /// <summary>
    /// The three languages whose noun changes by band. Polish is not decoration:
    /// its CLDR one is strictly n == 1, so it never enters the band the other two
    /// get wrong, and it is the control that says the fix did not move a language
    /// that was already right.
    /// </summary>
    public static TheoryData<string> InflectingLanguages() => new() { "ru", "uk", "pl" };

    [Theory]
    [MemberData(nameof(InflectingLanguages))]
    public void The_body_renders_with_its_size_and_a_noun_that_agrees(string cultureName)
    {
        var culture = CultureInfo.GetCultureInfo(cultureName);
        using var scope = new LocalisationScope(culture);

        // THE GUARD ON THE TEST ITSELF, in the style CountedStringTests already
        // uses. If PluraliseFile returned one string at every count, every
        // assertion below would pass whatever the template did. Three counts
        // spanning the three bands must give three different words, and this is
        // asserted for ru and uk only: Polish takes the same form at 5 and at 21,
        // which is correct Polish and not a collapsed selector.
        if (cultureName != "pl")
        {
            var one = DisplayHelpers.PluraliseFile(21);
            var few = DisplayHelpers.PluraliseFile(2);
            var many = DisplayHelpers.PluraliseFile(5);
            Assert.NotEqual(one, few);
            Assert.NotEqual(few, many);
            Assert.NotEqual(one, many);
        }

        var size = DisplayHelpers.FormatSize(Bytes);

        foreach (var count in new[] { 1, 2, 5, 21, 31, 41 })
        {
            var noun = DisplayHelpers.PluraliseFile(count);
            var rendered = string.Format(
                Localisation.FormatCulture,
                DisplayHelpers.Pluralise(
                    count,
                    Strings.Completion_NothingOfferedBody_Singular,
                    Strings.Completion_NothingOfferedBody_Plural,
                    "Completion.NothingOfferedBody"),
                count,
                noun,
                size);

            // Every placeholder was spent. An unspent one reaches the screen raw.
            Assert.DoesNotContain("{", rendered, StringComparison.Ordinal);

            // THE SIZE SURVIVED, WHICH IS THE ASSERTION THAT CATCHES AN INDEX
            // SLIP. A value still carrying {1} for its size renders the noun there
            // instead and the megabytes vanish, which no other check can see: the
            // sentence is still fluent, still the right length and still in the
            // right language.
            Assert.Contains(size, rendered, StringComparison.Ordinal);

            // THE NOUN AGREES WITH THE NUMERAL BESIDE IT. The lookarounds are
            // load-bearing rather than tidy: "файл" is a prefix of "файлов", so a
            // plain Contains passes on the very form this exists to catch.
            if (count != 1)
            {
                Assert.Matches(
                    new Regex($@"(?<!\p{{L}}){count} {Regex.Escape(noun)}(?!\p{{L}})"),
                    rendered);
            }
        }
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
