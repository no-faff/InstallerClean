using System.Globalization;
using InstallerClean.Helpers;
using InstallerClean.Resources;

namespace InstallerClean.Tests.Helpers;

/// <summary>
/// Covers the explicit-language path: what happens when the user picks a
/// language rather than leaving it on Automatic. Everything downstream of that
/// pick runs through Localisation's override, the resx resolution in
/// Strings.Get and the number formatting in DisplayHelpers. Covering it at all
/// depends on LocalisationScope: an override that can be set but not unset
/// leaks the pinned language into every test that runs after it, rewriting
/// their expected strings.
///
/// The assertions deliberately hold no French text. French punctuation needs
/// narrow no-break spaces, which many editors and text tools normalise away
/// without showing it, so an expected value typed into C# here could differ
/// from the resx by an invisible character. Every expectation below is read
/// back out of the resx, or is a decimal separator.
/// </summary>
public class LocalisationOverrideTests
{
    private static readonly CultureInfo French = CultureInfo.GetCultureInfo("fr");
    private static readonly CultureInfo British = CultureInfo.GetCultureInfo("en-GB");

    [Fact]
    public void An_explicit_pick_drives_the_generated_string_accessor()
    {
        var english = Strings.ResourceManager.GetString("Completion.AllClean", British);
        var french = Strings.ResourceManager.GetString("Completion.AllClean", French);
        Assert.NotEqual(english, french); // guards the test itself: the satellite must differ

        using var scope = new LocalisationScope(French, French);

        Assert.Equal(french, Strings.Completion_AllClean);
    }

    [Fact]
    public void An_explicit_pick_drives_number_formatting_even_when_the_thread_disagrees()
    {
        // The thread stays British while the app is pinned to French. This is
        // the case Localisation exists for: a thread culture does not reliably
        // survive the dispatcher's per-callback context, so the override is
        // what every window has to read.
        using var thread = new CultureScope(British);
        using var scope = new LocalisationScope(French, French);

        var size = DisplayHelpers.FormatSize(1024);

        Assert.Contains("1,0", size);
        Assert.DoesNotContain("1.0", size);
    }

    [Fact]
    public void The_long_elapsed_form_follows_the_format_culture_too()
    {
        // FormatElapsedLong was the one formatter in DisplayHelpers that did
        // not pass Localisation.FormatCulture, so it rendered the French
        // sentence with an English decimal point. This fails on that code.
        using var thread = new CultureScope(British);
        using var scope = new LocalisationScope(French, French);

        var elapsed = DisplayHelpers.FormatElapsedLong(TimeSpan.FromSeconds(1.5));

        Assert.Contains("1,5", elapsed);
        Assert.DoesNotContain("1.5", elapsed);
    }

    [Fact]
    public void An_explicit_pick_drives_the_plural_rules()
    {
        // English pluralises zero ("0 files"), French does not ("0 fichier").
        // Same count, same call: only the pinned language decides.
        Assert.Equal(DisplayHelpers.PluraliseFile(2), DisplayHelpers.PluraliseFile(0));

        using var scope = new LocalisationScope(French, French);

        Assert.NotEqual(DisplayHelpers.PluraliseFile(2), DisplayHelpers.PluraliseFile(0));
        Assert.Equal(Strings.ResourceManager.GetString("Plural.File.Singular", French),
            DisplayHelpers.PluraliseFile(0));
    }

    [Fact]
    public void Automatic_falls_back_to_the_ambient_thread_culture()
    {
        // No override set: the Automatic case, and the whole of the CLI.
        Assert.Null(Localisation.UiCultureOverride);
        using var thread = new CultureScope(French);

        Assert.Equal(French, Localisation.FormatCulture);
    }

    [Fact]
    public void Dropping_the_override_restores_the_fallback()
    {
        var english = Strings.Completion_AllClean;

        using (new LocalisationScope(French, French))
        {
            Assert.NotEqual(english, Strings.Completion_AllClean);
        }

        Assert.Null(Localisation.UiCultureOverride);
        Assert.Equal(english, Strings.Completion_AllClean);
    }

    /// <summary>
    /// Pins the app's language for one test and drops it again on the way out.
    /// The override is process-global, so a test that leaked it would rewrite
    /// every string the tests after it expect; the assembly runs its tests
    /// serially (see AssemblyInfo.cs) so a pin is never visible to a test
    /// running alongside.
    /// </summary>
    private sealed class LocalisationScope : IDisposable
    {
        public LocalisationScope(CultureInfo uiCulture, CultureInfo formatCulture) =>
            Localisation.Set(uiCulture, formatCulture);

        public void Dispose() => Localisation.Reset();
    }

    private sealed class CultureScope : IDisposable
    {
        private readonly CultureInfo _previous;

        public CultureScope(CultureInfo culture)
        {
            _previous = CultureInfo.CurrentCulture;
            CultureInfo.CurrentCulture = culture;
        }

        public void Dispose() => CultureInfo.CurrentCulture = _previous;
    }
}
