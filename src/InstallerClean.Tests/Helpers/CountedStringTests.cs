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
/// A translator writing "Completion.ReverifySkiped.One" adds a key that parity
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
    /// is what the lookup is built from. Four of them reach Pluralise through
    /// HeldBackReport's flat overload, where the key arrives in a local and a
    /// search of the call expression alone does not see it.
    ///
    /// THAT SENTENCE SAID FIVE AND THE COUNT HAD BEEN STALE SINCE THE RELEASE THAT
    /// CUT TWO CAUSES AND ADDED ONE. It is corrected here rather than dropped
    /// because it is the warning that matters: a sweep of the call expressions
    /// misses exactly those, which is how this list went four prefixes short
    /// without anything failing. The three added alongside this note were found by
    /// walking the resx for Singular and Plural pairs instead, and one of the four
    /// had been missing since the empty-offer screen came back.
    /// </summary>
    private static readonly string[] CountedPrefixes =
    {
        "Cli.DeletedFiles", "Cli.DeletingFiles", "Cli.FoundOrphans", "Cli.MissingFromDisk",
        "Cli.MovedFiles", "Cli.MovingFiles", "Cli.NothingOffered",
        "Completion.FailedCount", "Completion.FailedCountDelete",
        "Completion.MoveCancelledSummary", "Completion.MoveRestoreHint",
        "Completion.MoveRestoreHintSameDrive", "Completion.MoveSummary",
        "Completion.NothingOfferedBody",
        "Completion.PermanentDeleteCancelledSummary", "Completion.PermanentDeleteSummary",
        "Completion.ReverifyIncomplete", "Completion.ReverifyOwnershipUnestablished",
        "Completion.ReverifyRecordsChanged", "Completion.ReverifySkipped",
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
        "Completion.MoveRestoreHint", "Completion.MoveRestoreHintSameDrive",
        "Completion.NothingOfferedBody",
        "Completion.PermanentDeleteSummary",
        "Completion.ReverifyIncomplete", "Completion.ReverifyOwnershipUnestablished",
        "Completion.ReverifyRecordsChanged", "Completion.ReverifySkipped",
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
        using var scope = new LocalisationScope(Russian);

        var overridden = Strings.Find("Completion.ReverifySkipped.One");
        var flat = Strings.Get("Completion.ReverifySkipped");
        Assert.NotNull(overridden);
        Assert.NotEqual(flat, overridden); // guards the test itself: the two must differ

        Assert.Equal(overridden, DisplayHelpers.Pluralise(1, flat, "Completion.ReverifySkipped"));
        // 21 is "one" in East Slavic, which is why the override is read by
        // category rather than by count.
        Assert.Equal(overridden, DisplayHelpers.Pluralise(21, flat, "Completion.ReverifySkipped"));
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
