using System.Globalization;
using InstallerClean.Helpers;
using InstallerClean.Resources;
using InstallerClean.Services;

namespace InstallerClean.Tests.Helpers;

public class DisplayHelpersTests
{
    [Theory]
    [InlineData(0, "0 B")]
    [InlineData(512, "512 B")]
    [InlineData(1_023, "1,023 B")]
    [InlineData(1_024, "1.0 KB")]
    [InlineData(5_500, "5.4 KB")]
    [InlineData(1_048_576, "1.0 MB")]
    [InlineData(52_428_800, "50.0 MB")]
    [InlineData(1_048_576_000, "1,000.0 MB")]
    [InlineData(1_073_741_824, "1.00 GB")]
    [InlineData(5_368_709_120, "5.00 GB")]
    [InlineData(107_374_182_400, "100.00 GB")]
    [InlineData(1_073_741_824_000, "1,000.00 GB")]
    [InlineData(1_099_511_627_776, "1.00 TB")]
    [InlineData(2_748_779_069_440, "2.50 TB")]
    public void FormatSize_formats_correctly_in_en_US(long bytes, string expected)
    {
        using var scope = new CultureScope(CultureInfo.GetCultureInfo("en-US"));
        Assert.Equal(expected, DisplayHelpers.FormatSize(bytes));
    }

    [Theory]
    // Each pair straddles a step: the first size prints just under 1,024 of its
    // unit and stays there, the second would print 1,024 of it and moves up.
    [InlineData(1_048_524, "1,023.9 KB")]
    [InlineData(1_048_525, "1.0 MB")]
    [InlineData(1_073_689_395, "1,023.9 MB")]
    [InlineData(1_073_689_396, "1.00 GB")]
    [InlineData(1_099_506_259_066, "1,023.99 GB")]
    [InlineData(1_099_506_259_067, "1.00 TB")]
    public void FormatSize_chooses_the_unit_on_the_figure_as_it_prints(long bytes, string expected)
    {
        // A size a few bytes short of the next unit rounds to 1,024 of the one
        // below. A step that compared the raw figure would keep it in the lower unit
        // and print "1,024.0 MB", the reading the larger unit is there to replace.
        using var scope = new LocalisationScope(British, British);
        Assert.Equal(expected, DisplayHelpers.FormatSize(bytes));
    }

    [Theory]
    [InlineData(1_280, "1.2 KB")]
    [InlineData(1_792, "1.8 KB")]
    [InlineData(1_310_720, "1.2 MB")]
    [InlineData(1_835_008, "1.8 MB")]
    [InlineData(1_207_959_552, "1.12 GB")]
    [InlineData(1_744_830_464, "1.62 GB")]
    public void FormatSize_rounds_a_figure_exactly_halfway_to_the_even_digit(long bytes, string expected)
    {
        // A whole number of bytes over a power of two can land exactly halfway
        // between two printed values: 1,280 bytes is 1.25 KB. Both forms round it
        // to the even digit, so the window and the Application-channel entry print
        // the same digits for a size both give in the same unit.
        using var scope = new LocalisationScope(British, British);
        Assert.Equal(expected, DisplayHelpers.FormatSize(bytes));
        Assert.Equal(expected, DisplayHelpers.FormatSizeForMachine(bytes));
    }

    [Theory]
    [InlineData("de-DE")]
    [InlineData("fr-FR")]
    [InlineData("ru-RU")]
    [InlineData("en-GB")]
    [InlineData("ja-JP")]
    public void FormatSize_groups_a_four_digit_figure_as_the_region_does(string region)
    {
        // Composed from the culture's own separators rather than written out, so the
        // test holds whichever release of the culture data the machine carries:
        // French groups with a narrow no-break space in current data and a no-break
        // space in older data, and the assertion is that the size takes the one the
        // region has, not which one that is.
        var culture = CultureInfo.GetCultureInfo(region);
        using var scope = new LocalisationScope(British, culture);

        var format = culture.NumberFormat;
        Assert.Equal(
            $"1{format.NumberGroupSeparator}000{format.NumberDecimalSeparator}0 MB",
            DisplayHelpers.FormatSize(1_048_576_000));
    }

    [Theory]
    // Sizes below the separator band, below the step and below a terabyte: the
    // three places the two forms part, each with a row further down.
    [InlineData(0)]
    [InlineData(999)]
    [InlineData(1_024)]
    [InlineData(5_500)]
    [InlineData(1_048_576)]
    [InlineData(52_428_800)]
    [InlineData(1_073_741_824)]
    [InlineData(107_374_182_400)]
    [InlineData(1_023_999_999_999)]
    public void The_two_forms_agree_wherever_no_separator_step_or_terabyte_is_involved(long bytes)
    {
        using var scope = new LocalisationScope(British, British);
        Assert.Equal(DisplayHelpers.FormatSizeForMachine(bytes), DisplayHelpers.FormatSize(bytes));
    }

    [Theory]
    [InlineData(1_023, "1023 B")]
    [InlineData(1_048_576_000, "1000.0 MB")]
    [InlineData(1_073_741_823, "1024.0 MB")]
    [InlineData(1_099_511_627_776, "1024.00 GB")]
    [InlineData(2_199_023_255_552, "2048.00 GB")]
    public void FormatSizeForMachine_carries_no_separator_and_no_unit_above_gigabytes(long bytes, string expected)
    {
        // The Application-channel entries carry this form and tooling reads them by
        // their exact text. A group separator splits the figure for anything taking
        // it by its digits, and a terabyte unit is one no such match expects, so a
        // terabyte cache reads in gigabytes here and the unit never moves up on a
        // rounding step either.
        using var scope = new LocalisationScope(British, British);
        Assert.Equal(expected, DisplayHelpers.FormatSizeForMachine(bytes));
    }

    [Theory]
    [InlineData(0, "files")]
    [InlineData(1, "file")]
    [InlineData(2, "files")]
    [InlineData(100, "files")]
    public void Pluralise_returns_correct_form(int count, string expected)
    {
        // English (the test host culture) only ever resolves One/Other, so the
        // key prefix's Few/Many lookup is never read; any prefix works here.
        Assert.Equal(expected, DisplayHelpers.Pluralise(count, "file", "files", "Plural.File"));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(5)]
    public void Pluralise_flat_overload_equals_the_doubled_form(int count)
    {
        // The flat overload takes ONE string, so it cannot be handed two
        // different ones the way the doubled call sites could be. It must behave
        // exactly like the three-string form called with that string twice. A
        // prefix with no resx overrides makes every plural category fall back to
        // the flat string, so this holds in any UI culture.
        //
        // A REAL PREFIX RATHER THAN A MADE-UP ONE, AND THAT IS NOT TIDINESS.
        // DisplayHelpers.QuestionFor has no default arm: a prefix nobody has
        // classified throws rather than being absorbed, which is what stops the
        // inventory being silently short. "Test.FlatOverload" was invented here and
        // could never have exercised the override lookup this test describes, since
        // no satellite has ever held a key by that name in any form. This one is
        // passed by the app, carries no override in any language,
        // and so falls back in every category exactly as the comment above says.
        const string flat = "Found {0} registered {1}.";
        const string prefix = "Completion.MoveCancelledSummary";
        Assert.Equal(
            DisplayHelpers.Pluralise(count, flat, flat, prefix),
            DisplayHelpers.Pluralise(count, flat, prefix));
        Assert.Equal(flat, DisplayHelpers.Pluralise(count, flat, prefix));
    }

    [Theory]
    [InlineData(1, "One")]
    [InlineData(21, "One")]
    [InlineData(101, "One")]
    [InlineData(2, "Few")]
    [InlineData(4, "Few")]
    [InlineData(22, "Few")]
    [InlineData(5, "Many")]
    [InlineData(11, "Many")]
    [InlineData(12, "Many")]
    [InlineData(25, "Many")]
    [InlineData(111, "Many")]
    public void CategoryFor_russian_selects_one_few_many(int n, string expected)
    {
        Assert.Equal(expected, DisplayHelpers.CategoryFor(new CultureInfo("ru"), n).ToString());
    }

    [Theory]
    [InlineData(1, "One")]
    [InlineData(2, "Few")]
    [InlineData(4, "Few")]
    [InlineData(22, "Few")]
    [InlineData(24, "Few")]
    [InlineData(0, "Many")]
    [InlineData(5, "Many")]
    [InlineData(11, "Many")]
    [InlineData(14, "Many")]
    [InlineData(25, "Many")]
    public void CategoryFor_polish_selects_one_few_many(int n, string expected)
    {
        Assert.Equal(expected, DisplayHelpers.CategoryFor(new CultureInfo("pl"), n).ToString());
    }

    [Theory]
    [InlineData(21)]
    [InlineData(101)]
    [InlineData(31)]
    public void CategoryFor_polish_parts_company_with_east_slavic_above_twenty(int n)
    {
        // The one this file exists for. Polish "one" is strictly n == 1, where
        // Russian and Ukrainian also take 21, 31 and 101, and the two branches
        // sit next to each other reading almost alike: folding Polish into the
        // one above it would make every Polish count ending in 1 read as a
        // singular, and nothing but the code's own comment stood in the way.
        Assert.Equal("Many", DisplayHelpers.CategoryFor(new CultureInfo("pl"), n).ToString());
        Assert.Equal("One", DisplayHelpers.CategoryFor(new CultureInfo("ru"), n).ToString());
        Assert.Equal("One", DisplayHelpers.CategoryFor(new CultureInfo("uk"), n).ToString());
    }

    [Theory]
    [InlineData("fr", 0, "One")]
    [InlineData("fr", 1, "One")]
    [InlineData("fr", 2, "Other")]
    [InlineData("pt", 0, "One")]
    [InlineData("pt", 1, "One")]
    [InlineData("pt", 2, "Other")]
    public void CategoryFor_french_and_portuguese_take_zero_as_singular(string culture, int n, string expected)
    {
        Assert.Equal(expected, DisplayHelpers.CategoryFor(new CultureInfo(culture), n).ToString());
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(5)]
    public void CategoryFor_turkish_never_inflects(int n)
    {
        // A Turkish noun stays singular after a numeral, so the count sentence
        // does not inflect at all and "one" would be the wrong fragment even at
        // one.
        Assert.Equal("Other", DisplayHelpers.CategoryFor(new CultureInfo("tr"), n).ToString());
    }

    [Theory]
    [InlineData("ja")]
    [InlineData("ko")]
    [InlineData("zh")]
    [InlineData("id")]
    [InlineData("vi")]
    public void CategoryFor_uninflected_languages_are_always_other(string culture)
    {
        foreach (var n in new[] { 0, 1, 2, 5, 21 })
            Assert.Equal("Other", DisplayHelpers.CategoryFor(new CultureInfo(culture), n).ToString());
    }

    [Theory]
    [InlineData("en")]
    [InlineData("de")]
    [InlineData("es")]
    [InlineData("it")]
    [InlineData("nl")]
    public void CategoryFor_defaults_to_singular_only_at_one(string culture)
    {
        Assert.Equal("One", DisplayHelpers.CategoryFor(new CultureInfo(culture), 1).ToString());
        foreach (var n in new[] { 0, 2, 5, 21 })
            Assert.Equal("Other", DisplayHelpers.CategoryFor(new CultureInfo(culture), n).ToString());
    }

    [Theory]
    [InlineData("de-DE", "1,0 KB")]
    [InlineData("fr-FR", "1,0 KB")]
    [InlineData("en-GB", "1.0 KB")]
    [InlineData("ja-JP", "1.0 KB")]
    public void FormatSize_follows_system_culture_for_decimal_separator(string cultureName, string expected)
    {
        using var scope = new CultureScope(CultureInfo.GetCultureInfo(cultureName));
        Assert.Equal(expected, DisplayHelpers.FormatSize(1024));
    }

    [Fact]
    public void FormatSize_never_throws_across_many_cultures()
    {
        var cultures = new[] { "en-US", "en-GB", "de-DE", "fr-FR", "ja-JP", "tr-TR", "ar-SA", "hi-IN" };
        foreach (var name in cultures)
        {
            using var scope = new CultureScope(CultureInfo.GetCultureInfo(name));
            var _ = DisplayHelpers.FormatSize(1_073_741_824);
        }
    }

    [Theory]
    [InlineData("D:", "D:")]
    [InlineData("z:", "Z:")]
    public void A_wait_on_a_drive_names_it_as_a_drive_in_capitals(string root, string named) =>
        Assert.Equal(string.Format(Strings.Status_WaitingForDrive, named), DisplayHelpers.WaitingFor(root));

    [Theory]
    [InlineData(@"\\fileserver\apps")]
    [InlineData(@"GLOBALROOT\Device\Mup\fileserver\apps\setup.msi")]
    public void A_wait_on_a_share_or_any_other_path_names_it_as_it_is_spelled(string root) =>
        Assert.Equal(string.Format(Strings.Status_WaitingForPath, root), DisplayHelpers.WaitingFor(root));

    [Theory]
    [InlineData("D:", "D:")]
    [InlineData("z:", "Z:")]
    public void The_button_that_stops_a_wait_on_a_drive_names_it_as_a_drive_in_capitals(string root, string named) =>
        Assert.Equal(string.Format(Strings.Automation_StopWaitingForDrive, named), DisplayHelpers.StopWaitingFor(root));

    [Fact]
    public void A_drive_letter_takes_the_capital_that_names_the_drive_under_Turkish_too()
    {
        // Turkish upper-cases a dotted i to a dotted capital, which names no drive, so
        // the line and the button stay with the capital every drive letter is written in.
        var turkish = CultureInfo.GetCultureInfo("tr-TR");
        using var culture = new CultureScope(turkish);
        using var scope = new LocalisationScope(turkish, turkish);

        Assert.Equal("I:", DisplayHelpers.SourceRootName("i:"));
        Assert.Equal(string.Format(Strings.Status_WaitingForDrive, "I:"), DisplayHelpers.WaitingFor("i:"));
        Assert.Equal(string.Format(Strings.Automation_StopWaitingForDrive, "I:"), DisplayHelpers.StopWaitingFor("i:"));
    }

    [Theory]
    [InlineData(@"\\fileserver\apps")]
    [InlineData(@"GLOBALROOT\Device\Mup\fileserver\apps\setup.msi")]
    [InlineData("d")]
    public void A_share_or_any_other_path_is_named_as_it_is_spelled(string root) =>
        Assert.Equal(root, DisplayHelpers.SourceRootName(root));

    [Theory]
    [InlineData("1:")]
    [InlineData("é:")]
    public void A_root_of_two_characters_with_no_ASCII_letter_before_its_colon_is_named_as_a_path(string root)
    {
        Assert.False(DeclaredProductCheck.IsDriveLetter(root));
        Assert.Equal(root, DisplayHelpers.SourceRootName(root));
        Assert.Equal(string.Format(Strings.Status_WaitingForPath, root), DisplayHelpers.WaitingFor(root));
        Assert.Equal(string.Format(Strings.Automation_StopWaitingForPath, root), DisplayHelpers.StopWaitingFor(root));
    }

    [Theory]
    [InlineData(@"\\fileserver\apps")]
    [InlineData(@"GLOBALROOT\Device\Mup\fileserver\apps\setup.msi")]
    public void The_button_that_stops_a_wait_on_a_share_or_any_other_path_names_it_as_it_is_spelled(string root) =>
        Assert.Equal(string.Format(Strings.Automation_StopWaitingForPath, root), DisplayHelpers.StopWaitingFor(root));

    [Theory]
    [InlineData("_Cancel", 'C')]
    [InlineData("Carry on _without it", 'W')]
    [InlineData("\u5f85\u6a5f\u3092\u505c\u6b62(_S)", 'S')]
    [InlineData("Save__as _file", 'F')]
    [InlineData("Save__as", null)]
    [InlineData("No key", null)]
    [InlineData("Ends with_", null)]
    public void A_label_s_access_key_is_the_letter_after_its_first_single_underscore(string label, char? key)
    {
        using var scope = new LocalisationScope(British, British);

        Assert.Equal(key, DisplayHelpers.AccessKeyOf(label));
    }

    [Fact]
    public void An_access_key_is_named_in_the_case_the_language_shown_names_it()
    {
        using var scope = new LocalisationScope(CultureInfo.GetCultureInfo("tr"), British);

        Assert.Equal('\u0130', DisplayHelpers.AccessKeyOf("Onsuz _ilerle"));
    }

    [Fact]
    public void A_waiting_line_is_spoken_with_the_key_that_carries_on_without_its_drive_and_what_that_leaves_alone()
    {
        using var scope = new LocalisationScope(British, British);

        Assert.Equal(
            "Waiting for drive D: to respond... Press Alt+W to carry on without it. "
            + "Any file still to be checked against it will be left alone.",
            DisplayHelpers.WaitingLineWithStopKey(DisplayHelpers.WaitingFor("D:")));
    }

    public static TheoryData<string> EveryCulture()
    {
        var data = new TheoryData<string>();
        foreach (var name in SupportedLanguages.CultureNames) data.Add(name);
        return data;
    }

    [Theory]
    [MemberData(nameof(EveryCulture))]
    public void Every_language_s_spoken_waiting_line_names_the_key_its_button_carries(string cultureName)
    {
        // The spoken line can only name a key the label marks, so a translation of the
        // button without one would leave a screen reader with no key to tell of.
        var culture = CultureInfo.GetCultureInfo(cultureName);
        using var scope = new LocalisationScope(culture, culture);
        var line = DisplayHelpers.WaitingFor("D:");

        var key = DisplayHelpers.AccessKeyOf(Strings.Action_StopWaiting);
        var spoken = DisplayHelpers.WaitingLineWithStopKey(line);

        Assert.NotNull(key);
        Assert.StartsWith(line, spoken, StringComparison.Ordinal);
        Assert.Contains(key.Value.ToString(), spoken[line.Length..], StringComparison.Ordinal);
    }

    [Fact]
    public void Spoken_sentences_put_the_language_s_stop_after_a_line_that_carries_none()
    {
        using var scope = new LocalisationScope(British, British);

        Assert.Equal(
            "All clean. Nothing to clean up in C:\\Windows\\Installer. Scanned 151 files in less than a second",
            DisplayHelpers.SpokenSentences(
                "All clean", "Nothing to clean up in C:\\Windows\\Installer", "Scanned 151 files in less than a second"));
    }

    [Fact]
    public void Spoken_sentences_put_only_a_space_after_a_line_that_carries_its_own_stop()
    {
        using var scope = new LocalisationScope(British, British);

        Assert.Equal(
            "InstallerClean 9.9.9 is available. It's strongly recommended to use the latest version.",
            DisplayHelpers.SpokenSentences(
                "InstallerClean 9.9.9 is available.", "It's strongly recommended to use the latest version."));
        Assert.Equal("Ready? Go", DisplayHelpers.SpokenSentences("Ready?", "Go"));
        Assert.Equal("Held back (3.6 MB). Next", DisplayHelpers.SpokenSentences("Held back (3.6 MB)", "Next"));
    }

    [Fact]
    public void Spoken_sentences_leave_out_blank_lines_and_trim_the_rest()
    {
        using var scope = new LocalisationScope(British, British);

        Assert.Equal("All clean. Done", DisplayHelpers.SpokenSentences("", "All clean ", "  ", "\nDone"));
        Assert.Equal(string.Empty, DisplayHelpers.SpokenSentences("", " "));
    }

    [Fact]
    public void Spoken_sentences_in_Japanese_take_its_full_width_stop_and_no_space()
    {
        var japanese = CultureInfo.GetCultureInfo("ja");
        using var scope = new LocalisationScope(japanese, japanese);

        Assert.Equal("すべてクリーン。完了。次", DisplayHelpers.SpokenSentences("すべてクリーン", "完了。", "次"));
    }

    [Theory]
    [MemberData(nameof(EveryCulture))]
    public void Every_language_s_own_stop_closes_a_spoken_sentence(string cultureName)
    {
        // A line ending in the stop its own language puts between sentences is followed by
        // the space alone, so no language hears a second stop after its own.
        var culture = CultureInfo.GetCultureInfo(cultureName);
        using var scope = new LocalisationScope(culture, culture);
        var stop = Strings.Display_SentenceSeparator.TrimEnd();

        Assert.Equal("a" + stop + DisplayHelpers.SentenceSpace + "b",
            DisplayHelpers.SpokenSentences("a" + stop, "b"));
        Assert.Equal("a" + Strings.Display_SentenceSeparator + "b", DisplayHelpers.SpokenSentences("a", "b"));
    }

    private static readonly CultureInfo British = CultureInfo.GetCultureInfo("en-GB");

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

    /// <summary>
    /// Pins the app's language and region for one test and drops them on the way
    /// out. The pin is process-global, which the assembly's serial test run (see
    /// AssemblyInfo.cs) is what makes safe.
    /// </summary>
    private sealed class LocalisationScope : IDisposable
    {
        public LocalisationScope(CultureInfo uiCulture, CultureInfo formatCulture) =>
            Localisation.Set(uiCulture, formatCulture);

        public void Dispose() => Localisation.Reset();
    }
}
