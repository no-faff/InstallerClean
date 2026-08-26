using System;
using System.Globalization;
using System.Reflection;
using InstallerClean.Resources;

namespace InstallerClean.Helpers;

internal static class DisplayHelpers
{
    internal static string GetVersionString()
    {
        var version = Assembly.GetExecutingAssembly().GetName().Version;
        return version is not null
            ? string.Format(Strings.Version_Display, $"{version.Major}.{version.Minor}.{version.Build}")
            : string.Empty;
    }

    internal static string FormatSize(long bytes) => bytes switch
    {
        >= 1_073_741_824 => string.Format(Localisation.FormatCulture, Strings.Display_Size_GB, bytes / 1_073_741_824.0),
        >= 1_048_576 => string.Format(Localisation.FormatCulture, Strings.Display_Size_MB, bytes / 1_048_576.0),
        >= 1_024 => string.Format(Localisation.FormatCulture, Strings.Display_Size_KB, bytes / 1_024.0),
        _ => string.Format(Localisation.FormatCulture, Strings.Display_Size_B, bytes)
    };

    internal static string FormatElapsed(TimeSpan elapsed) =>
        elapsed.TotalSeconds < 1
            ? string.Format(Localisation.FormatCulture, Strings.Display_Elapsed_Ms, elapsed.TotalMilliseconds)
            : string.Format(Localisation.FormatCulture, Strings.Display_Elapsed_S, elapsed.TotalSeconds);

    /// <summary>
    /// Natural-language elapsed time for body copy. Renders sub-second
    /// scans as "less than a second" and second-plus scans as
    /// "{N.N} seconds" so an all-clean overlay reads as a sentence
    /// rather than a CLI status pill. <see cref="FormatElapsed"/> stays
    /// the right call for the short-form metadata pills.
    /// </summary>
    internal static string FormatElapsedLong(TimeSpan elapsed) =>
        elapsed.TotalSeconds < 1
            ? Strings.Display_ElapsedLong_LessThanASecond
            : string.Format(Localisation.FormatCulture, Strings.Display_ElapsedLong_Seconds, elapsed.TotalSeconds);

    internal enum PluralCategory { One, Few, Many, Other }

    /// <summary>
    /// Unicode CLDR plural category for <paramref name="culture"/>, so a language
    /// with more than English's one/other split (Russian's 2-4 "few", etc) selects
    /// the right fragment. Integer counts only. Takes the culture explicitly so it
    /// is testable without process-global state.
    ///
    /// THIS ANSWERS ONE OF THE TWO QUESTIONS A COUNT ASKS, and the arms below are
    /// written for that one: which form a noun takes after a numeral. Russian's 21 is
    /// One because "21 файл" is what a Russian writes, and Turkish is Other at every
    /// count because a Turkish noun does not inflect after one. Both are right, and
    /// neither says anything about whether a sentence should read "this file" or
    /// "these files". <see cref="CountQuestion"/> is where that second question lives.
    /// </summary>
    internal static PluralCategory CategoryFor(CultureInfo culture, int count)
    {
        int n = count < 0 ? -count : count;
        switch (culture.TwoLetterISOLanguageName)
        {
            case "ru":
            case "uk": // East Slavic: one / few / many
                int mod10 = n % 10, mod100 = n % 100;
                if (mod10 == 1 && mod100 != 11) return PluralCategory.One;
                if (mod10 >= 2 && mod10 <= 4 && (mod100 < 12 || mod100 > 14)) return PluralCategory.Few;
                return PluralCategory.Many;
            case "pl": // Polish one / few / many. "one" is strictly n==1, NOT the
                       // East Slavic rule (where 21, 31, ... are also "one"); few =
                       // n%10 in 2..4 and n%100 not in 12..14; many = the rest
                       // (including 0 and the 11..14 band).
            {
                int m10 = n % 10, m100 = n % 100;
                if (n == 1) return PluralCategory.One;
                if (m10 >= 2 && m10 <= 4 && (m100 < 12 || m100 > 14)) return PluralCategory.Few;
                return PluralCategory.Many;
            }
            case "fr":
            case "pt": // 0 and 1 are singular
                return n is 0 or 1 ? PluralCategory.One : PluralCategory.Other;
            case "tr": // Turkish: a noun stays singular after a numeral ("5 dosya",
                       // never "dosyalar"), so the count sentence does not inflect.
                return PluralCategory.Other;
            case "ja":
            case "ko":
            case "zh":
            case "id":
            case "vi": // no count inflection
                return PluralCategory.Other;
            default: // en, de, es, it, ...: singular only at exactly 1
                return n == 1 ? PluralCategory.One : PluralCategory.Other;
        }
    }

    /// <summary>
    /// Which of the two questions a counted string's ONE form answers. They are
    /// different questions, and one function answering both is what put the plural
    /// sentence on the delete confirmation at a single file in six languages, and the
    /// singular one at twenty-one files in two more.
    ///
    /// <see cref="Grammatical"/> is "what form does this noun take after this
    /// numeral". That is CLDR, it varies by language, and <see cref="CategoryFor"/>
    /// answers it. <see cref="Cardinality"/> is "is this exactly one", which decides
    /// between "this file" and "these files" and is n == 1 in every language there is.
    ///
    /// THE LINE BETWEEN THEM IS WHETHER THE ONE-FORM CARRIES THE NUMBER. A one-form
    /// with a {0} in it shows the reader a numeral and must agree with it, so it is
    /// grammatical. A one-form asserting oneness in words, with no placeholder to put
    /// a number in, is cardinality: nothing in it can agree with anything, and its
    /// only claim is that there is one.
    /// </summary>
    internal enum CountQuestion { Grammatical, Cardinality }

    /// <summary>
    /// The question <paramref name="keyPrefix"/>'s one-form answers, so no call site
    /// has to choose. The call site could not choose well anyway: whether a sentence
    /// shows a numeral or asserts oneness in words is a property of the sentence, and
    /// the sentence is what the prefix names.
    ///
    /// THERE IS NO DEFAULT ARM AND THAT IS THE POINT OF THE SHAPE. A prefix nobody has
    /// classified throws here rather than being absorbed into either class, because a
    /// list that quietly answers for something it has never been told about is the
    /// failure this repository keeps finding in its own instruments. The list of
    /// counted prefixes in CountedStringTests was itself four short for a whole
    /// release, and said nothing; a check that is silently narrower than its subject
    /// reads exactly like one that covers it.
    ///
    /// CountedStringTests calls this for every counted prefix the app passes, and
    /// renders the cardinality ones in all sixteen languages, so an unclassified or
    /// misfiled prefix fails a push rather than a user's screen. The throw is the
    /// backstop under those tests, not the detector.
    /// </summary>
    /// <exception cref="ArgumentOutOfRangeException">
    /// A counted prefix with no class, which is a defect in this switch and not a
    /// machine state: a new counted string is classified here in the same edit that
    /// adds it to the resx.
    /// </exception>
    internal static CountQuestion QuestionFor(string keyPrefix) => keyPrefix switch
    {
        // The noun slots. No numeral of their own because they ARE the noun a numeral
        // elsewhere governs, so they are grammatical by definition, and they are the
        // one class the text cannot be read to work out: they look like cardinality
        // and behave like agreement. PluraliseFile(21) must stay "файл".
        "Plural.Error" or "Plural.File" or "Plural.Package" or "Plural.Patch"
            or "Plural.Product" => CountQuestion.Grammatical,

        // A numeral governing a noun. The one-form spells {0} out, so it has to agree
        // with the number the reader can see, whatever that language's rule is.
        "Cli.DeletedFiles" or "Cli.DeletingFiles" or "Cli.FoundOrphans"
            or "Cli.MissingFromDisk" or "Cli.MovedFiles" or "Cli.MovingFiles"
            or "Completion.FailedCount" or "Completion.FailedCountDelete"
            or "Completion.MoveCancelledSummary" or "Completion.MoveSummary"
            or "Completion.PermanentDeleteCancelledSummary"
            or "Completion.PermanentDeleteSummary"
            or "Completion.ReverifyIncomplete" or "Completion.ReverifyOwnershipUnestablished"
            or "Completion.ReverifyRecordsChanged" or "Completion.ReverifySkipped"
            or "Status.RegisteredPackagesFound"
            or "Summary.MissingFromDisk" or "Summary.MissingFromDisk.OtherPrograms"
            or "Summary.MissingFromDisk.Unnamed" or "Summary.OrphanedToCleanUp"
            or "Summary.RegisteredStillUsed" or "Summary.RegisteredWindow"
            => CountQuestion.Grammatical,

        // Oneness asserted in words. "This file will be deleted permanently" names no
        // number and agrees with nothing; it is simply false of twenty-one files and
        // false of none. Three of these carry a numeral in their PLURAL and none in
        // their singular, which is why the classification reads the one-form alone.
        "Cli.NothingOffered" or "Completion.MoveRestoreHint"
            or "Completion.MoveRestoreHintSameDrive" or "Completion.NothingOfferedBody"
            or "Confirm.DeletePermanently"
            or "Error.AccessDenied" or "Error.FileInUse" or "Error.IOFailure"
            or "Error.UnknownError" or "Summary.NothingListed"
            or "Cli.SupersededHeldBack" or "Summary.SupersededHeldBack"
            => CountQuestion.Cardinality,

        _ => throw new ArgumentOutOfRangeException(nameof(keyPrefix), keyPrefix,
            "A counted string with no class. Add it to QuestionFor and to "
            + "CountedStringTests.CountedPrefixes in the same edit as the resx key. "
            + "It is Cardinality if its one-form asserts oneness in words with no {0} "
            + "in it, Grammatical otherwise."),
    };

    /// <summary>
    /// The resx form to take for a string whose one-form asserts oneness in words:
    /// One at exactly one, in every language, and at no other count anywhere.
    ///
    /// FEW AND MANY ARE LEFT WHERE CLDR PUT THEM, WHICH IS THE PART THAT IS NOT
    /// OBVIOUS AND MUST NOT BE TIDIED AWAY. Three of these strings carry a numeral in
    /// their PLURAL and none in their singular, so their plural side is still a numeral
    /// governing a noun and still wants its paucal band.
    ///
    /// NO CARDINALITY KEY SHIPS A .Few OR .Many OVERRIDE TODAY, WHICH IS SAID HERE
    /// RATHER THAN LEFT TO BE DISCOVERED. Polish, Russian and Ukrainian each shipped a
    /// live Completion.NothingOfferedBody.Few until that key's noun moved into a slot
    /// Plural.File fills, which left the override character-identical to its own .Plural
    /// and it was deleted. So collapsing everything that is not One into Other would
    /// currently change nothing a user sees, and that is the danger rather than the
    /// reassurance: this arm is correct with no live consumer, which is the state in
    /// which somebody removes it. A satellite may add such an override at any time and
    /// the collapse would silently disarm it, so the band is pinned directly, on the
    /// selector, by CountedStringTests.The_cardinality_selector_keeps_its_paucal_band.
    ///
    /// A CLDR One that is not exactly one becomes Other rather than Many. Nothing
    /// special applies to it, so it wants the plain plural, and a Many override written
    /// for the five-and-up band has no business turning up at 21.
    /// </summary>
    internal static PluralCategory CardinalCategoryFor(CultureInfo culture, int count)
    {
        int n = count < 0 ? -count : count;
        if (n == 1) return PluralCategory.One;

        var grammatical = CategoryFor(culture, count);
        return grammatical == PluralCategory.One ? PluralCategory.Other : grammatical;
    }

    /// <summary>
    /// The resx form to take for <paramref name="count"/>, answering whichever of the
    /// two questions <paramref name="question"/> names. The whole of the difference
    /// between them lives here and in <see cref="CardinalCategoryFor"/>; every caller
    /// reaches it through <see cref="Pluralise(int, string, string, string)"/>, which
    /// looks the question up from the key rather than being told.
    /// </summary>
    /// <exception cref="ArgumentOutOfRangeException">
    /// A question with no answer here, which is a defect in this switch: the enum and
    /// this method are edited together or not at all.
    /// </exception>
    internal static PluralCategory CategoryFor(CultureInfo culture, int count, CountQuestion question) =>
        question switch
        {
            CountQuestion.Grammatical => CategoryFor(culture, count),
            CountQuestion.Cardinality => CardinalCategoryFor(culture, count),
            _ => throw new ArgumentOutOfRangeException(nameof(question), question,
                "A count question with no selector. Add it here in the same edit as "
                + "the CountQuestion member."),
        };

    // An override key exists only in a satellite, so it has no typed accessor and
    // is read by name. Strings.Find is the door for exactly that: it resolves at
    // the app's UI culture and spends the installer-folder token, so an override
    // on a key naming the cache folder cannot reach a screen raw in that one
    // language. Named for what the read is, the lookup missing being the normal
    // answer here rather than a fault.
    private static string? Override(string key) => Strings.Find(key);

    /// <summary>
    /// Picks the count fragment for <paramref name="count"/> in the current UI
    /// language. <paramref name="singular"/>/<paramref name="plural"/> are the resx
    /// one/other forms; a language may override a CLDR category with a satellite-only
    /// <c>{keyPrefix}.One</c> / <c>.Few</c> / <c>.Many</c> key, read here by name
    /// (absent ones fall back to <paramref name="singular"/> for One, else
    /// <paramref name="plural"/>). The <c>.One</c> override lets a flat string (passed
    /// as singular==plural) gain a correct n==1 form in an inflecting language without
    /// splitting the neutral key. <paramref name="keyPrefix"/> is the resx key without
    /// the form suffix and MUST match it exactly, or the lookup silently misses.
    ///
    /// THE KEY ALSO DECIDES WHICH QUESTION THE ONE FORM ANSWERS, through
    /// <see cref="QuestionFor"/>, and that is deliberately not a decision the call site
    /// makes. A call site knows how many files there are; it does not know whether the
    /// sentence it is about to render spells the number out or asserts oneness in
    /// words, and that is the only thing the answer turns on. Read
    /// <see cref="CountQuestion"/> before adding a counted string: an unclassified
    /// prefix throws here rather than guessing.
    /// </summary>
    internal static string Pluralise(int count, string singular, string plural, string keyPrefix) =>
        CategoryFor(Localisation.UiCulture, count, QuestionFor(keyPrefix)) switch
        {
            PluralCategory.One => Override($"{keyPrefix}.One") ?? singular,
            PluralCategory.Few => Override($"{keyPrefix}.Few") ?? plural,
            PluralCategory.Many => Override($"{keyPrefix}.Many") ?? plural,
            _ => plural,
        };

    /// <summary>
    /// Overload for the flat-string case: ONE resx string carries the whole
    /// sentence (with its own <c>{N}</c>) for every count, inflecting only
    /// through the satellite-only <c>{keyPrefix}.One</c> / <c>.Few</c> /
    /// <c>.Many</c> overrides. The three-string form above, called with the same
    /// string twice, reads like a copy-paste slip and invites being "fixed" into
    /// a single argument, which silently drops the inflection an inflecting
    /// language needs. This overload states that intent and takes only one
    /// string, so it cannot be misused that way, and is the form every call
    /// site whose singular and plural are the same resx key takes.
    /// </summary>
    internal static string Pluralise(int count, string flat, string keyPrefix) =>
        Pluralise(count, flat, flat, keyPrefix);

    /// <summary>"file"/"files" pair, sourced from Strings.resx.</summary>
    internal static string PluraliseFile(int count) =>
        Pluralise(count, Strings.Plural_File_Singular, Strings.Plural_File_Plural, "Plural.File");

    /// <summary>"error"/"errors" pair, sourced from Strings.resx.</summary>
    internal static string PluraliseError(int count) =>
        Pluralise(count, Strings.Plural_Error_Singular, Strings.Plural_Error_Plural, "Plural.Error");

    /// <summary>"package"/"packages" pair, sourced from Strings.resx.</summary>
    internal static string PluralisePackage(int count) =>
        Pluralise(count, Strings.Plural_Package_Singular, Strings.Plural_Package_Plural, "Plural.Package");

    /// <summary>"product"/"products" pair, sourced from Strings.resx.</summary>
    internal static string PluraliseProduct(int count) =>
        Pluralise(count, Strings.Plural_Product_Singular, Strings.Plural_Product_Plural, "Plural.Product");

    internal static string PluralisePatch(int count) =>
        Pluralise(count, Strings.Plural_Patch_Singular, Strings.Plural_Patch_Plural, "Plural.Patch");
}
