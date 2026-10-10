using System.Globalization;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text.Json.Serialization;
using InstallerClean.Helpers;
using InstallerClean.Services;

namespace InstallerClean.Models;

/// <summary>
/// The report of the PC's first finished run: a Move, a Delete, or a scan
/// that offered nothing. Written once, as that run's card appears, to
/// <c>last-run.json</c> in <c>%LOCALAPPDATA%\NoFaff\InstallerClean</c>;
/// the file's contents are exactly what is POSTed when the card closes
/// with its report box ticked.
///
/// Schema is intentionally narrow. Every field is either categorical
/// or a count; no file paths, no usernames, no machine identifiers,
/// no time-of-day, nothing that could correlate two runs from the
/// same machine.
/// </summary>
public sealed record ResultLogEntry(
    int SchemaVersion,
    AppInfo App,
    string Os,
    MachineInfo Machine,
    ScanInfo Scan,
    OperationInfo Operation)
{
    /// <summary>
    /// Schema 2 separates <see cref="ScanInfo.ObsoletedCount"/>
    /// (PatchState=4) from <see cref="ScanInfo.SupersededCount"/>
    /// (PatchState=2). Schema 1 envelopes lump both states under
    /// supersededCount; receivers must branch on this version before
    /// reading either field.
    ///
    /// Schema 3 added an optional per-code count map to each error bucket,
    /// carrying the shell HRESULTs behind two delete-only categories. Delete
    /// no longer goes through the shell, so those two categories and the map
    /// with them stopped being produced: a schema-3 report from that change
    /// on carries the same error categories as a Move and no <c>codes</c>
    /// field at all. Both are subtractions from an allowlisting receiver's
    /// point of view, which is why the version did not move for them.
    ///
    /// SCHEMA 4 IS THE FIRST BUMP THAT IS NOT A SHAPE CHANGE FOR ITS OWN SAKE.
    /// The payload carries what varies BETWEEN machines: a <c>machine</c> object of shape
    /// facts, the three terms behind the withholding rather than the one number
    /// that mixes them, the identity pass's three outcomes, the act-time
    /// re-verify's five, and the byte totals a count-shaped question cannot
    /// answer. <c>pendingReboot</c> leaves, being structurally forced on any run
    /// that could act and unvarying across every report received.
    ///
    /// SIX OF THOSE KEYS WENT AGAIN IN 3.0.0 AND THE VERSION DID NOT MOVE, on the
    /// schema-3 precedent above: the identity pass's three scan-time outcomes, its
    /// instance-transform count, and two of the re-verify's five held-back causes
    /// all stopped being produced when the check that produced them was removed. Two
    /// arrived in the same release, under <c>machine</c>, counting the products
    /// installed as a second instance of themselves and the products that would not
    /// answer that question. A KEY THAT CEASES TO BE PRODUCED IS A SUBTRACTION; A
    /// KEY WHOSE MEANING CHANGES IS NOT, which is why the missing-files split was
    /// added beside its total rather than over it.
    ///
    /// SCHEMA 5 ADDS KEYS AND TAKES NONE AWAY: the registry side's failed reads under
    /// <c>machine</c>; under <c>scan</c> the seven arms of the withholding split that
    /// schema 4 does not carry, so that from 5 the split's twelve counts add up to
    /// <c>withheldCandidateCount</c>, the two counts of superseded rows the containment
    /// check kept back, the count of superseded rows withheld on a recorded path the scan
    /// could not settle, the count of superseded rows a condition on the whole machine held
    /// back, the count of listed programs the scan could not check, the count of
    /// installations of a program the enumeration returned that the scan found under an
    /// account or context the enumeration did not list them in, and the count of patch
    /// registrations naming a cached file that the scan could not match to a program it
    /// asks about; and under <c>operation</c> a fifth
    /// held-back cause, <c>heldBackFileNotConfirmed</c>. Under both <c>scan</c> and
    /// <c>operation</c> it adds the drives and shares the declared-product screen gave up and
    /// kept a file at, one count for each way it gives one up, the files it kept at them and
    /// the waits it made: the scan's under <c>scan</c>, and under <c>operation</c> those of
    /// the check made when Move or Delete is clicked. Under <c>app</c> it adds
    /// <c>windowsLanguage</c>, the Windows display language with no country, AND
    /// <c>app.language</c> CHANGES WHAT IT MEANS AT 5: it is the language the app was
    /// showing, one of the languages it ships, where schema 4 carries the UI culture's
    /// own tag, which on Automatic is the Windows display language with its region, or
    /// <c>invariant</c>. AND <c>scan.withheldCandidateCount</c> AND
    /// <c>scan.withheldTotalBytes</c> TAKE IN MORE FILES AT 5: from 5 the withheld list
    /// holds the files from the folder walk that the containment check kept back, which
    /// schema 4's figures leave out. AN ADDITION MOVES THE VERSION ONCE A RELEASE SENDS
    /// THE VERSION IT WOULD BE ADDED TO. From schema 4 on, the receiver holds each
    /// version to its exact set of keys and requires every count in it, so a key added
    /// to a version a release already sends could only be permitted there, never
    /// required, without rejecting every report from that release.
    ///
    /// SCHEMA 6 ADDS TWENTY-THREE KEYS UNDER <c>scan</c> AND TAKES NONE AWAY: what the
    /// declared-product screen found of the two conditions under which it holds back every
    /// installation package it would otherwise let through
    /// (<see cref="ScanInfo.SecondCopyListedCheckedCount"/> to
    /// <see cref="ScanInfo.SecondCopyUnseenByNameFileCount"/>).
    ///
    /// SCHEMA 7 ADDS ONE KEY UNDER <c>app</c> AND ONE UNDER <c>scan</c> AND TAKES NONE AWAY:
    /// <c>windowsRegion</c>, the Country or region set in Windows
    /// (<see cref="AppInfo.WindowsRegion"/>), and
    /// <c>secondCopyReleasedOpensNoPackageCount</c>, the installations the declared-product
    /// screen found with no package for Windows Installer to open
    /// (<see cref="ScanInfo.SecondCopyReleasedOpensNoPackageCount"/>).
    ///
    /// A receiver that does not recognise a version stores the report under a
    /// lenient v&lt;n&gt;-unknown/ prefix rather than rejecting it, so a bump
    /// never loses data even if the allowlist has not caught up. THAT LENIENCE
    /// DOES NOT EXTEND TO THE TOP LEVEL: the receiver's top-level key allowlist
    /// runs for every version including the ones it cannot validate, so
    /// <c>machine</c> arriving before the receiving end knows the name is a
    /// rejected report and a user told sending failed. The receiver ships first.
    /// </summary>
    public const int CurrentSchemaVersion = 7;

    // Each factory takes windowsRegion as IWindowsRegion.Read returned it for the
    // report box, so the report carries the value the box started from.
    public static ResultLogEntry ForScanOnly(ScanResult scan, long scanDurationMs, string? windowsRegion) =>
        new(
            CurrentSchemaVersion,
            AppInfo.Current(windowsRegion),
            ResolveOs(),
            MachineInfo.From(scan),
            ScanInfo.From(scan, scanDurationMs),
            OperationInfo.ScanOnly());

    public static ResultLogEntry ForMove(
        ScanResult scan,
        long scanDurationMs,
        MoveResult move,
        long bytesFreed,
        long operationDurationMs,
        string moveDestinationKind,
        ReverifyResult check,
        string? windowsRegion) =>
        new(
            CurrentSchemaVersion,
            AppInfo.Current(windowsRegion),
            ResolveOs(),
            MachineInfo.From(scan),
            ScanInfo.From(scan, scanDurationMs),
            OperationInfo.FromMove(move, bytesFreed, operationDurationMs, moveDestinationKind, check));

    public static ResultLogEntry ForDelete(
        ScanResult scan,
        long scanDurationMs,
        DeleteResult delete,
        long bytesFreed,
        long operationDurationMs,
        ReverifyResult check,
        string? windowsRegion) =>
        new(
            CurrentSchemaVersion,
            AppInfo.Current(windowsRegion),
            ResolveOs(),
            MachineInfo.From(scan),
            ScanInfo.From(scan, scanDurationMs),
            OperationInfo.FromDelete(delete, bytesFreed, operationDurationMs, check));

    private static string ResolveOs()
    {
        // Bucket to OS family and architecture only. The raw
        // RuntimeInformation.OSDescription string carries the Windows
        // build number (e.g. "10.0.26100"), which can narrow an
        // Insider-ring user to a population small enough to function
        // as a fingerprint. The schema's no-machine-identifier
        // contract requires a coarser shape.
        //
        // Build-number boundaries: Windows 11 starts at 22000
        // (released 2021-10-05). Earlier NT 10 builds are Windows 10.
        // The boundary holds for Windows 11 24H2 / build 26100 which
        // is also the Server 2025 build; the family label calls it
        // Windows 11 because the client population dominates and the
        // server population is a fraction of a percent of installs.
        var build = Environment.OSVersion.Version.Build;
        var family = build switch
        {
            >= 22000 => "Windows 11",
            >= 10000 => "Windows 10",
            > 0 => "Windows",
            _ => "Unknown",
        };
        return $"{family} ({RuntimeInformation.OSArchitecture})";
    }
}

/// <summary>
/// Which build produced the report, which language its user was reading, which
/// language Windows was showing them, and the Country or region set in Windows.
/// </summary>
/// <param name="Language">
/// The language the app was showing for this run: one of
/// <see cref="SupportedLanguages.CultureNames"/>, as
/// <see cref="SupportedLanguages.Active"/> resolves it from
/// <see cref="Localisation.UiCulture"/>, the culture the app's strings are looked up
/// in, which is the same answer the language menu ticks. It follows a language picked
/// in the app, and on Automatic, the Windows display language resolved the way the app's
/// own strings resolve, so a display language the app has no translation for reports
/// <see cref="SupportedLanguages.Neutral"/>, the English the user saw.
///
/// It can only be one of the languages the app ships, so it cannot narrow anybody. It
/// is there because a report about a screen nobody can read in their own language is
/// a different report, and because which languages are actually used is not
/// otherwise knowable.
/// </param>
/// <param name="WindowsLanguage">
/// The language Windows shows its own interface in for this user, with any country or
/// region taken off and a script kept: <see cref="WindowsDisplayLanguage.Current"/>,
/// which also gives its two fixed labels. It is read from Windows, so a language picked
/// in the app does not change it.
///
/// BESIDE <paramref name="Language"/>, THE PAIR SAYS WHO READ THE APP IN A LANGUAGE
/// OTHER THAN THE ONE WINDOWS SHOWS: a machine showing Windows in Czech and the app in
/// English is somebody the app has no translation for. It names a language and never a
/// country.
/// </param>
/// <param name="WindowsRegion">
/// The Country or region set in Windows for the account running the app, the value
/// <see cref="IWindowsRegion.Read"/> gave when the report box was put on the card and
/// <see cref="ReportBoxRegions.StartsTicked"/> decided the box's starting state from, in
/// the shape <see cref="WindowsRegionLabel.For"/> gives it: an ISO 3166-1 two-letter code,
/// a UN M.49 number, or one of two fixed labels.
/// </param>
public sealed record AppInfo(string Version, string Language, string WindowsLanguage, string WindowsRegion)
{
    /// <summary>
    /// This build and this user's languages, with <paramref name="windowsRegion"/>, a value
    /// <see cref="IWindowsRegion.Read"/> returned.
    /// </summary>
    public static AppInfo Current(string? windowsRegion) =>
        new(Assembly.GetExecutingAssembly().GetName().Version?.ToString(3) ?? "0.0.0",
            // The UI culture rather than the format culture, resolved to the
            // language whose strings were shown: a UI culture with no satellite of
            // its own, the invariant culture included, shows the neutral English.
            SupportedLanguages.Active(Localisation.UiCulture),
            WindowsDisplayLanguage.Current(),
            WindowsRegionLabel.For(windowsRegion));
}

/// <summary>
/// What the machine is like, as opposed to what this run did. Every field is a
/// count or a fixed label, and none of them narrows a machine to a person: the
/// shape of a Windows Installer cache is not a fingerprint, and nothing here
/// records a path, a product, a name or a time.
///
/// IT IS A TOP-LEVEL OBJECT RATHER THAN MORE KEYS UNDER <c>scan</c> because these
/// answer the same on two consecutive scans of the same machine and the run
/// figures do not, and a reader that has to keep a list of which key is which has
/// been handed the wrong structure.
///
/// It is collected because the shape of a machine's installer records varies from
/// one machine to the next, and only these reports can say how: whether short-name
/// creation is on, how many patches a machine carries, how large its cache is and
/// how its registry reads answer.
/// </summary>
/// <param name="ShortNameCreation">
/// Where the machine still generates 8dot3 short names, one of
/// <see cref="ShortNameCreationLabels"/>.
/// </param>
/// <param name="LongFileNameCount">
/// Registered cached paths whose file name is longer than eight characters before
/// the extension, so the name itself cannot be an 8dot3 short name. Read against
/// <see cref="ScanInfo.RegisteredCount"/> in the same report, which is the
/// population it is drawn from.
///
/// The census calls the same number <c>LongLeafStemCount</c>, which is the precise
/// word for the part of a name before its extension. This one is what a person
/// reads off the confirmation dialog, and it pairs with
/// <see cref="ShortNameCreation"/> two lines above: short name against long name is
/// a pair anybody can follow without knowing what a stem is.
/// </param>
/// <param name="NonStringLocalPackageCount">
/// Cached-package values, <c>LocalPackage</c> or <c>ManagedLocalPackage</c>, that were
/// there and were not a string, one per value. Every report answering zero is the
/// evidence that reading them as one is safe; a single report answering otherwise is
/// the evidence that it is not, and one such report is worth more than any number of
/// the first kind.
/// </param>
/// <param name="UnreadablePatchStateCount">
/// Patches whose State or Uninstallable read failed during the scan, or came back
/// empty where the pairing's verdict turns on it, one per product-to-patch pairing.
/// Each such pairing keeps its file, so the count says how often a machine cannot
/// answer the question, and never counts a file lost.
/// </param>
/// <param name="UnreadableVerdictPathCount">
/// The same unanswered reads counted once per cached PATH rather than once per pairing.
///
/// IT IS HERE RATHER THAN UNDER THE SCAN BECAUSE THE PAIR IS THE READING and
/// splitting them across two objects would throw that away: a machine where
/// several products' reads failed on one shared patch reports a high pairing count
/// against a single path, and a machine where the failures are spread reports the
/// two close together. Those are different faults wearing one number, and only the
/// two side by side tell them apart.
///
/// It counts REGISTRATIONS, not files on the disk: existence is not tested, unlike
/// the unclaimed-file counts under the scan. Nothing may read it as a quantity of
/// space or as files a user could go and look at.
/// </param>
/// <param name="UnparseableProductKeyCount">
/// Registry product key names that yielded no product code, so there was nothing
/// to ask Windows about. The registry says the machine has a product and nothing
/// can turn its name into a question.
///
/// A MACHINE FACT AND NOT A RUN OBSERVATION, which is what puts it here: it is
/// counted while walking every product key rather than only the ones a run
/// happened to miss, so two scans of one machine agree about it. Its sibling under
/// the scan, the unanswered count, is the opposite on both points.
/// </param>
/// <param name="ProductCount">Installed products the enumeration returned.</param>
/// <param name="RegistryProductKeyCount">
/// Product keys under Windows Installer's <c>UserData</c> key, in every account's
/// subtree: a count of the machine's products that does not come from the
/// enumeration, and that also counts a key a failed or partial uninstall left
/// behind. It sits here beside <see cref="ProductCount"/> rather than under the scan
/// because two scans of one machine agree about it.
///
/// The app derives nothing from the difference between the two. Each product the
/// registry names with a code and the enumeration did not return is asked about by
/// name: one found installed travels as the recovered count, one Windows would not
/// answer about as the unanswered count, and one not installed in neither. A key whose
/// name yields no code travels as the unparseable count. The pair travels because
/// how far a machine's registry runs ahead of its enumeration is a fact only these
/// reports can establish.
/// </param>
/// <param name="PatchClaimCount">
/// Product-to-patch claims read, one per claim rather than per patch. With
/// <see cref="ProductCount"/> it gives the ratio that says how patch-heavy a
/// machine is.
/// </param>
/// <param name="InstanceProductCount">
/// Products installed as a second instance of themselves under an instance
/// transform. PRODUCTS, not files, and not a count of anything held back.
///
/// IT TRAVELS BECAUSE HOW OFTEN A MACHINE CARRIES SUCH A PRODUCT is a fact only these
/// reports can establish.
///
/// THE COUNT DECIDES NOTHING AND THE READING BEHIND IT IS LIVE. Each such installation,
/// and each whose InstanceType read failed, carries
/// <see cref="ListedInstallation.SecondCopyNotRuledOut"/> into the declared-product
/// screen, which compares every walk-derived installation package with the packages
/// that installation opens. A file one of them opens is kept as a package an installed
/// program opens, and where they cannot all be seen every such package is kept and
/// counted in <see cref="ScanInfo.WithheldSecondCopyUnestablishedCount"/>. The re-verification pass
/// puts the same question again between the scan and the click, and the command line
/// prints a line for that count among the reasons a run could not be certain.
///
/// A machine fact rather than a run observation, which is what puts it in this
/// object: two scans of one machine agree about it.
/// </param>
/// <param name="InstanceTypeUnreadableCount">
/// Products whose <c>InstanceType</c> read failed, so they were neither counted
/// above nor shown to be ordinary.
///
/// IT TRAVELS SO THAT A ZERO ABOVE CANNOT BE READ AS "NO SUCH PRODUCT HERE". A
/// complete negative is a zero in both, and a zero above with a number here is a
/// machine that did not answer rather than a machine with none. A product the
/// enumeration never returned is asked only once Windows confirms it installed. One
/// Windows would not answer about travels as the unanswered count and a key whose name
/// yields no code as the unparseable count, and both arm the same rule.
/// </param>
/// <param name="SupersededRegistrationCount">
/// Cached patch paths whose merged row Windows reports superseded: one per path however
/// many programs register the patch, whether or not the file is on the disk, counted
/// off the machine rather than off the offer. The name says registrations and the count
/// is of paths. A machine fact: two scans of one machine agree about it, where the scan
/// object's <c>supersededCount</c> answers what a run OFFERED.
///
/// THE DIFFERENCE BETWEEN THE TWO IS A MIXED SET. Several separate conditions keep a
/// superseded row off the offer, so no single cause may be stated for it.
/// </param>
/// <param name="ObsoletedRegistrationCount">
/// The same for cached patch paths whose merged row reads obsoleted, and for that class
/// this is the only figure that can ever be non-zero: they are not offered, so nothing
/// derived from the offer can see them. It answers whether a machine has one.
/// </param>
/// <param name="ProductPatchKeyCount">
/// Products whose registry patch-list key opened, from the listing the per-product
/// condition rests on. Against <see cref="ProductCount"/> it says how usual it is for
/// a product to carry one.
/// </param>
/// <param name="ProductPatchRegistrationCount">
/// Patch subkeys under those keys, one per (product, patch) registration. With the
/// count above it gives how many patches a machine's products carry.
/// </param>
/// <param name="ProductsWithRemovablePatchCount">
/// Products where at least one registered patch positively declared itself removable,
/// so a rollback there could reach for a superseded patch's cached file. THE FIGURE
/// THAT SAYS ON HOW MANY PRODUCTS THE CONDITION IS ARMED.
/// </param>
/// <param name="ProductsWithPatchSetUnestablishedCount">
/// Products whose patch set could not be established. The other half of the same
/// question and kept apart from it: one is the condition finding a reason to withhold,
/// this is the condition unable to look.
/// </param>
/// <param name="PathResolverAttemptCount">
/// Recorded paths this scan put to the final-path resolver, which from 3.0.0 is EVERY
/// value that got past the embedded-null test and the expansion.
///
/// READ THE FIVE BELOW AGAINST IT OR NOT AT ALL. Five zeros are five clean answers on
/// a machine that asked, and nothing at all on a machine with no registrations to ask
/// about. This is the only thing separating those two readings.
///
/// ITS MEANING CHANGED IN 3.0.0 AND THE OLD ONE IS WORTH KNOWING, because a receiver
/// comparing across that boundary is comparing two different quantities. Until then
/// the resolver was asked only for a value carrying a long-path or NT object prefix or
/// an 8dot3 alias, so this counted flagged spellings as well as attempts. It counts
/// attempts alone now, and <see cref="PathFlaggedSpellingCount"/> carries the other
/// half.
/// </param>
/// <param name="PathResolverNotAPathCount">
/// Of those, refused outright as not a path.
/// </param>
/// <param name="PathResolverNoAncestorCount">
/// Of those, with no existing component up to the root: an unattached drive, an
/// unmapped share, a detached virtual disk.
/// </param>
/// <param name="PathResolverOpenRefusedCount">
/// Of those, where an ancestor existed and no handle could be opened on it, most often
/// an ACL.
/// </param>
/// <param name="PathResolverNoFinalNameCount">
/// Of those, where an opened handle yielded an empty final name.
/// </param>
/// <param name="PathResolverFaultedCount">
/// Of those, where the attempt threw rather than answering. The resolved count is not
/// sent: it is the attempts less these five, and a stored copy could disagree with its
/// own parts.
///
/// THESE FIVE ARE SENT APART AND ACTED ON TOGETHER. All five withhold alike, and the
/// split is here as five numbers a receiver can read separately rather than as one
/// figure folding them together.
/// See <see cref="PathResolverRefusedCount"/>.
/// </param>
/// <param name="PathNormalisationRefusedAtExpansionCount">
/// Recorded values this scan could not turn into a path at all, refused while expanding
/// an environment variable. It and the three refusal counts below add up to
/// <see cref="PathNormalisationRefusedCount"/>.
///
/// THE FOUR ARE ONE POPULATION SPLIT BY CAUSE AND MUST NOT BE DESCRIBED AS ONE. A
/// sentence naming any single cause is false of the other three members; the only
/// thing true of every member is that the recorded value could not be turned into a
/// path.
/// </param>
/// <param name="PathNormalisationRefusedAtPrefixStripCount">
/// Recorded values this scan could not turn into a path, refused while taking a prefix
/// off or preparing the resolver's ask.
/// </param>
/// <param name="PathNormalisationRefusedAtFullPathCount">
/// Recorded values this scan could not turn into a path, refused by the full-path call:
/// one of nothing but spaces, a length past the API's limit.
/// </param>
/// <param name="PathFlaggedSpellingCount">
/// Recorded values carrying a spelling only the filesystem can settle: an 8dot3 alias,
/// or a prefix the strip left on for want of a drive root.
///
/// NOT AN OUTCOME AND NOT A FAULT. Every other count in this group says what happened
/// to a value; this says what the value looked like. A flagged spelling that resolves
/// is the mechanism working. What a figure above zero says is that this machine holds
/// the spellings the resolution exists for.
/// </param>
/// <param name="PathNormalisationRefusedAtEmbeddedNullCount">
/// Recorded values this scan could not turn into a path, refused for carrying an
/// embedded null, which no path can carry. The member that fires on Windows, and the
/// reason it is asked for separately: the expansion cuts such a value at the null and
/// returns without throwing, so this count, taken before it, is where the condition
/// shows.
///
/// LAST IN THE LIST AND FIRST IN THE METHOD. These are positional parameters and all
/// four are <c>int</c>, so a member inserted among the others would re-point every
/// argument after it with nothing in the build to say so.
/// </param>
/// <param name="RegistrationIdentityAttemptCount">
/// Recorded paths this scan asked the filesystem to identify, so that a registration
/// written in a spelling the folder walk never produces is still matched to its file.
///
/// READ THE FIVE BELOW AGAINST IT OR NOT AT ALL, on the same rule as the resolver's
/// attempts count. The comparison is skipped where the walk found no candidates and
/// where the records hold no registrations, and five zeros from a machine that never
/// asked are indistinguishable on the wire from five clean answers.
/// </param>
/// <param name="RegistrationIdentityNamesNothingCount">
/// Of those, the ones with no file at the path: a registration whose cached file has
/// already gone.
///
/// IT IS THE ONE FAILURE HERE THAT IS NOT A FAILURE OF THE MACHINE, and it is
/// deliberately outside <see cref="RegistrationIdentityRefusedCount"/>. Such a
/// registration claims none of the walked files, so nothing was given up by not
/// identifying it, and a machine reporting a high figure here is a machine that has
/// uninstalled things rather than one with anything wrong.
/// </param>
/// <param name="RegistrationIdentityNotAPathCount">
/// Of those, the ones with no string to open at all. Nothing either side of this
/// comparison can produce today, so a report carrying it says something nobody has
/// seen.
/// </param>
/// <param name="RegistrationIdentityOpenRefusedCount">
/// Of those, where something is at the path and no handle could be opened on it.
/// </param>
/// <param name="RegistrationIdentityUnavailableCount">
/// Of those, where the handle opened and the filesystem would not give the file's id.
/// A volume or a Windows build that does not answer that information class, which on
/// the volume this folder sits on has never been observed and would take a machine's
/// whole offer with it.
/// </param>
/// <param name="RegistrationIdentityFaultedCount">
/// Of those, where the attempt threw rather than answering.
/// </param>
/// <param name="CandidateIdentityAttemptCount">
/// Walked files this scan asked the filesystem to identify, one per candidate the
/// registration side left an identity to compare against. Zero where no registration
/// yielded one, which is ordinary on a machine with nothing to compare.
/// </param>
/// <param name="CandidateIdentityNamesNothingCount">
/// Of those, the ones that were no longer there: a file that went between the walk
/// and this read. Outside the refused total for the same reason as its opposite
/// number, a file that is not there being one no registration's identity could have
/// matched either.
/// </param>
/// <param name="CandidateIdentityNotAPathCount">
/// Of those, the ones with no string to open. Unreachable, as above.
/// </param>
/// <param name="CandidateIdentityOpenRefusedCount">
/// Of those, where the file is there and no handle could be opened on it. A candidate
/// has already had a handle opened on it once, by the containment guard, so this is a
/// race or a volume that answers one call and not the other.
/// </param>
/// <param name="CandidateIdentityUnavailableCount">
/// Of those, where the handle opened and the filesystem would not name the file.
/// </param>
/// <param name="CandidateIdentityFaultedCount">
/// Of those, where the attempt threw.
///
/// SUCCESSFUL READS ARE NOT SENT ON EITHER SIDE. They are the attempts less the five,
/// and a stored copy could disagree with its own parts.
///
/// APPENDED, LIKE EVERY GROUP BEFORE THEM, for the reason the embedded-null note
/// above gives: these are positional <c>int</c> parameters and an insertion re-points
/// every argument after it with nothing in the build to say so.
/// </param>
/// <param name="RegistryKeyReadFailureCount">
/// Reads of Windows Installer's own registry records, under its <c>UserData</c> key,
/// that failed: a read that threw, whether of that key, of one account's products or
/// patches, or of one product's or patch's entry, and a package path value that was
/// there and was not a string.
/// <see cref="NonStringLocalPackageCount"/> is that last kind, so it is a part of this
/// figure and the reads that threw are this less that.
///
/// IT IS ONE OF THE TWO TERMS OF THE CHECK THAT REFUSES A SCAN OUTRIGHT, the other
/// being the scan object's <c>unreadableProductCount</c>. With both above zero the scan
/// stops before it produces a result, so no report carries both above zero. With the
/// product enumeration whole, that check passes whatever this says.
///
/// Appended after the identity groups for the reason given above.
/// </param>
public sealed record MachineInfo(
    string ShortNameCreation,
    int LongFileNameCount,
    int NonStringLocalPackageCount,
    int UnreadablePatchStateCount,
    int UnreadableVerdictPathCount,
    int UnparseableProductKeyCount,
    int ProductCount,
    int RegistryProductKeyCount,
    int PatchClaimCount,
    int InstanceProductCount,
    int InstanceTypeUnreadableCount,
    int SupersededRegistrationCount,
    int ObsoletedRegistrationCount,
    int ProductPatchKeyCount,
    int ProductPatchRegistrationCount,
    int ProductsWithRemovablePatchCount,
    int ProductsWithPatchSetUnestablishedCount,
    int PathResolverAttemptCount,
    int PathResolverNotAPathCount,
    int PathResolverNoAncestorCount,
    int PathResolverOpenRefusedCount,
    int PathResolverNoFinalNameCount,
    int PathResolverFaultedCount,
    int PathNormalisationRefusedAtExpansionCount,
    int PathNormalisationRefusedAtPrefixStripCount,
    int PathNormalisationRefusedAtFullPathCount,
    int PathNormalisationRefusedAtEmbeddedNullCount,
    int PathFlaggedSpellingCount,
    int RegistrationIdentityAttemptCount,
    int RegistrationIdentityNamesNothingCount,
    int RegistrationIdentityNotAPathCount,
    int RegistrationIdentityOpenRefusedCount,
    int RegistrationIdentityUnavailableCount,
    int RegistrationIdentityFaultedCount,
    int CandidateIdentityAttemptCount,
    int CandidateIdentityNamesNothingCount,
    int CandidateIdentityNotAPathCount,
    int CandidateIdentityOpenRefusedCount,
    int CandidateIdentityUnavailableCount,
    int CandidateIdentityFaultedCount,
    int RegistryKeyReadFailureCount)
{
    public static MachineInfo From(ScanResult scan) =>
        new(
            scan.ShortNameCreation,
            scan.Census.LongLeafStemCount,
            scan.Census.NonStringLocalPackageValues,
            scan.Census.UnreadablePatchStates,
            scan.Census.UnreadableVerdictPaths,
            scan.Census.UnparseableProductKeyNames,
            scan.Census.ProductCount,
            scan.Census.RegistryProductKeys,
            scan.Census.PatchClaimCount,
            scan.Census.InstanceProductCount,
            scan.Census.InstanceTypeUnreadableCount,
            scan.SupersededRegistrationCount,
            scan.ObsoletedRegistrationCount,
            scan.Census.ProductPatchKeyCount,
            scan.Census.ProductPatchRegistrationCount,
            scan.Census.ProductsWithRemovablePatchCount,
            scan.Census.ProductsWithPatchSetUnestablishedCount,
            scan.Census.PathResolverAttemptCount,
            scan.Census.PathResolverNotAPathCount,
            scan.Census.PathResolverNoAncestorCount,
            scan.Census.PathResolverOpenRefusedCount,
            scan.Census.PathResolverNoFinalNameCount,
            scan.Census.PathResolverFaultedCount,
            scan.Census.PathNormalisationRefusedAtExpansionCount,
            scan.Census.PathNormalisationRefusedAtPrefixStripCount,
            scan.Census.PathNormalisationRefusedAtFullPathCount,
            scan.Census.PathNormalisationRefusedAtEmbeddedNullCount,
            scan.Census.PathFlaggedSpellingCount,
            // The two identity tallies, and they come off the SCAN rather than off
            // the census: the census is what the enumeration measured about the
            // machine's records, and these are what the scan measured about its
            // files.
            scan.RegistrationIdentityReads.AttemptCount,
            scan.RegistrationIdentityReads.NamesNothingCount,
            scan.RegistrationIdentityReads.NotAPathCount,
            scan.RegistrationIdentityReads.OpenRefusedCount,
            scan.RegistrationIdentityReads.IdentityUnavailableCount,
            scan.RegistrationIdentityReads.FaultedCount,
            scan.CandidateIdentityReads.AttemptCount,
            scan.CandidateIdentityReads.NamesNothingCount,
            scan.CandidateIdentityReads.NotAPathCount,
            scan.CandidateIdentityReads.OpenRefusedCount,
            scan.CandidateIdentityReads.IdentityUnavailableCount,
            scan.CandidateIdentityReads.FaultedCount,
            scan.Census.RegistryKeyReadFailures);

    /// <summary>
    /// Every recorded value this scan could not turn into a path, whatever refused
    /// it: the sum of the four above.
    ///
    /// DERIVED RATHER THAN PASSED IN, and that is the whole of why it is down here
    /// instead of among the parameters. As a parameter it could be constructed
    /// disagreeing with its own parts, and a total that contradicts its breakdown
    /// inside one object is the failure that no reader of the payload could
    /// possibly diagnose. It is serialised like any other property, so the receiver
    /// sees it as a key beside them.
    ///
    /// IT IS THE ONLY MEMBER OF THIS GROUP A SENTENCE MAY BE BUILT ON, the four
    /// parts being four different facts about a machine. What it means is that the
    /// recorded path could not be turned into a path at all, so the claim is kept in
    /// the raw spelling Windows gave and matches nothing the folder walk produces.
    /// None of the four steps it counts refuses a value for anything it finds on disk,
    /// so no missing file, missing drive or permission produces it. The cached file it
    /// names is withheld with the rest of the walk-derived offer, which is what this
    /// figure says about the machine it came from.
    /// </summary>
    public int PathNormalisationRefusedCount =>
        PathNormalisationRefusedAtExpansionCount
        + PathNormalisationRefusedAtPrefixStripCount
        + PathNormalisationRefusedAtFullPathCount
        + PathNormalisationRefusedAtEmbeddedNullCount;

    /// <summary>
    /// Recorded values the final-path resolver was asked about and did not resolve,
    /// whichever way it failed: the sum of the five resolver outcomes. Derived here
    /// for the same reason as the total above, so it cannot be sent disagreeing with
    /// its own parts.
    ///
    /// A SECOND POPULATION BESIDE THAT TOTAL AND NEVER A PART OF IT. Those are values
    /// that could not be turned into a path at all; these are values that ARE paths
    /// and whose spelling the filesystem would not settle. One value can appear in
    /// both, so the two must not be added and the sum called a count of anything.
    ///
    /// AND THE APPLICATION ACTS ON IT, on the same rule as the other total: above
    /// zero on either, the scan withholds its whole walk-derived offer rather than
    /// name a file it cannot say is spare. So a report carrying a figure here is a
    /// report from a machine that was offered nothing from the folder walk.
    ///
    /// THE ONLY SENTENCE TRUE OF EVERY MEMBER is that the resolver was asked and did
    /// not answer. The five parts are five different facts about a machine and a
    /// sentence naming any one of them is false of the other four.
    /// </summary>
    public int PathResolverRefusedCount =>
        PathResolverNotAPathCount
        + PathResolverNoAncestorCount
        + PathResolverOpenRefusedCount
        + PathResolverNoFinalNameCount
        + PathResolverFaultedCount;

    /// <summary>
    /// Recorded paths the filesystem would not identify, whichever way the read
    /// failed. Derived here for the same reason as the two totals above, so it
    /// cannot be sent disagreeing with its own parts.
    ///
    /// A THIRD POPULATION AND NEVER PART OF EITHER OTHER TOTAL. Those two are about
    /// what Windows wrote down: a value that could not be turned into a path, and a
    /// path whose spelling the filesystem would not settle. This one is about the
    /// file: the path was settled and the file at the end of it could not be named.
    ///
    /// A MIXED SET, SO NOTHING MAY STATE A CAUSE FOR IT. The only thing true of every
    /// member is that the reader was asked which file a path names and did not say.
    /// The count of paths naming no file at all is outside it and is where an
    /// ordinary machine's failures go.
    ///
    /// AND THE APPLICATION ACTS ON IT. Above zero, the scan withheld its whole
    /// walk-derived offer, on the same rule as the other two totals: rather than name
    /// a file it cannot say is spare. So a report carrying a figure here is a report
    /// from a machine that was offered nothing from the folder walk.
    /// </summary>
    public int RegistrationIdentityRefusedCount =>
        RegistrationIdentityNotAPathCount
        + RegistrationIdentityOpenRefusedCount
        + RegistrationIdentityUnavailableCount
        + RegistrationIdentityFaultedCount;

    /// <summary>
    /// The same over the other side of that comparison, and the application acts on
    /// it DIFFERENTLY, which is the one thing to know before reading the two
    /// together.
    ///
    /// A registration nobody could identify might name any walked file, so the whole
    /// offer goes. A candidate nobody could identify is one file, every other
    /// candidate having been compared by a read that answered, so this figure is a
    /// count of files kept back one at a time and the offer around them stands.
    /// </summary>
    public int CandidateIdentityRefusedCount =>
        CandidateIdentityNotAPathCount
        + CandidateIdentityOpenRefusedCount
        + CandidateIdentityUnavailableCount
        + CandidateIdentityFaultedCount;
}

/// <summary>
/// What the scan found. Counts and byte totals only.
/// </summary>
/// <param name="RegisteredBytes">
/// Total size of the registered files that are really on disk, and
/// <paramref name="RemovableBytes"/> the same for the files being offered.
///
/// THESE TWO ARE THE STRONGEST FIELDS IN THE SCHEMA: the question they answer is
/// whether somebody can tell, before running anything, that they probably have
/// something to reclaim. If the tell exists it is in bytes, since a count of
/// registered files says nothing about how large they are.
/// </param>
/// <param name="MissingNeededCount">
/// The half of <paramref name="MissingFromDiskCount"/> whose absence this scan could
/// not establish to be harmless, which is not the same as the half carrying no
/// superseded or obsoleted state. It is filled from
/// <see cref="ScanResult.MissingAffectedCount"/>, whose own remarks state the
/// conjunction in full and are the one place it is written down; a second copy here
/// would drift from it. It sits BESIDE the total rather than replacing it: the total
/// is read by the public chart with no version gate, and replacing it would split a
/// live series. The other half falls out by subtraction.
///
/// THE NAME SAYS "NEEDED", THE COUNT DOES NOT MEAN IT, AND THE WIRE SHAPE IS HELD
/// ANYWAY. What lands here is an absence this scan could not establish to be
/// harmless, which is a fact about what the scan managed to read rather than a
/// finding that anything wants the file back: a rise in this figure can as easily be
/// a run that read less. The two halves are registrations naming a file that is not
/// there, and they ARE graded, positively and narrowly, both hosts printing the graded
/// half rather than the total. The patch STATE does not grade them, Windows opening
/// every registered patch's cached file whether superseded or not, so the state is one
/// conjunct of three and settles nothing on its own.
///
/// REPORTS FROM v2.3.0 AND EARLIER COUNT A DIFFERENT POPULATION, SO A SERIES CROSSING
/// 3.0.0 IS NOT COMPARABLE. Those builds excluded every patch a scan called removable
/// and every one whose verdict it had withheld; from 3.0.0 only rows meeting that
/// whole conjunction are excluded. The key keeps its name because a receiver reads
/// it by that name.
/// </param>
/// <param name="WithheldPatchCount">
/// Superseded files a scan would have offered and did not, on one condition rather
/// than several: the scan could not establish something the offer needs. That covers a
/// read that established nothing, a scan unable to check every program entry in Windows
/// Installer's records or to match every cached patch file to a program it asks, a
/// product whose patch set could not be established at all, and a recorded path the
/// scan could not settle. The report counts the files a condition on the whole machine
/// held back in <c>supersededScanWideWithheldCount</c>, and of those the ones the scan-wide
/// withholding took while a recorded path was unsettled in
/// <c>supersededRecordedPathUnestablishedCount</c>. Reports from
/// builds that offered no registered file carry it as zero.
/// Obsoleted files are not in it: they are not withheld, they are simply not
/// offered, and they have their own count.
///
/// A PRODUCT HOLDING A PATCH THAT COULD BE UNINSTALLED AND ROLL BACK ONTO THE FILE IS
/// NOT IN IT. That row is downgraded with
/// withheld FALSE, the scan having positively established a live claim rather than
/// having failed to establish anything, so it never reaches this count. Stated here
/// because this is a wire contract and the receiver cannot see the predicate.
/// </param>
/// <param name="UnreadableProductCount">
/// Products whose records came back short. An exact per-product tally.
/// </param>
/// <param name="SkippedProductRowCount">
/// Rows the product walk passed without reading. This code always sends zero: the
/// walk refuses the scan on a row it cannot read, and a refused scan sends no report.
/// Counted inside <paramref name="UnreadableProductCount"/> as well.
/// </param>
/// <param name="UnclaimedProductFileCount">
/// Product registrations naming a cached file that is really on the disk and that
/// the enumeration never claimed.
/// </param>
/// <param name="RecoveredProductCount">
/// Installations of products the registry named, this enumeration never returned, and
/// a keyed ask then found installed: each one named and confirmed, not inferred from two
/// totals. Installations the same ask found of a product the enumeration did return are
/// <paramref name="RecoveredEnumeratedInstallationCount"/>.
///
/// A non-zero reading is a machine whose enumeration came back short of a product
/// that is installed. It withholds nothing: the products behind it were asked
/// about rather than guessed at.
///
/// UNDER THE SCAN AND NOT THE MACHINE because it exists only where a run came back
/// short, so two scans of one machine need not agree about it.
/// </param>
/// <param name="UnansweredProductCount">
/// Products the registry named, this enumeration never returned, and Windows would
/// then not say were installed or not. A question that was put and got no answer,
/// which holds back every superseded patch and the whole walk-derived offer,
/// because nothing about an enumeration's completeness follows from silence.
///
/// NOT THE MACHINE OBJECT'S UNPARSEABLE COUNT, and the two may never be added
/// together under one name outside the withholding total: Windows was never asked
/// about those, so a sentence about what Windows would not say is false of every
/// one of them.
/// </param>
/// <param name="UnclaimedPatchFileCount">
/// The same for patch registrations. A patch entry names no product, so it
/// establishes only that at least one product went unreached.
///
/// A tally. The part of it that withholds is
/// <paramref name="UnattributedPatchFileCount"/>.
///
/// The machine object's two product headcounts are inputs to nothing the app derives:
/// their difference cannot tell a truncated enumeration from ordinary registry residue,
/// and the products behind it are asked about by name instead. They travel because how
/// far a real machine's registry runs ahead of its enumeration is a fact only these
/// reports can establish.
/// </param>
/// <param name="WithheldCandidateCount">
/// Files the folder walk found, that no registration's recorded path claimed, and
/// that this scan then declined to offer. <c>ScanResult.WithheldFiles</c>, which is
/// the list the Details window shows and the main window's left-alone line counts.
///
/// IT IS NOT <paramref name="WithheldPatchCount"/> AND THE TWO COUNT DIFFERENT
/// POPULATIONS. That one is superseded registrations the scan would have offered and
/// did not; this one never came from the registered set at all. A third figure,
/// <c>ScanResult.RegisteredWithheldCount</c>, is different again and travels as
/// <paramref name="RegisteredWithheldCount"/> below. Nothing may add any two of them.
///
/// IT IS HOW MUCH THE APP'S OWN WITHHOLDING HOLDS BACK ON A MACHINE, AND NOTHING ELSE
/// ON THE WIRE ANSWERS THAT. It is the figure that says whether a machine got nothing
/// because its folder was clean or because the scan could not settle it.
///
/// NO CAUSE TRAVELS WITH THIS FIGURE AND NONE MAY BE ATTACHED TO IT. Five separate
/// decisions put files on that list and they are different facts about a machine; a
/// sentence naming any one of them would be false of the others. The twelve counts below
/// are where those decisions are counted apart, one finding each, and they are read
/// apart for the same reason. They are the scan's whole split of the list, taken off the
/// same result as this figure, so the twelve add up to it.
/// </param>
/// <param name="WithheldTotalBytes">
/// The bytes of the files behind <paramref name="WithheldCandidateCount"/>, summed
/// off the same list so the count sent and the size sent cannot come apart.
///
/// IT IS THE QUESTION A COUNT CANNOT ANSWER: forty megabytes and forty gigabytes are
/// the same file count. Several conditions withhold, and a count alone cannot say how
/// much any of them holds back. The window's finished screen prints a size beside the
/// count it gives, and so does the command line's sentence about files held back from
/// the folder walk, each the size of the files that sentence counts rather than this
/// whole-list figure, so a person at the machine can see how much was held back.
///
/// NO CAUSE TRAVELS WITH IT, on the same rule as the count it belongs to. It is a
/// long rather than an int because a byte total over a whole cache folder is not
/// bounded by anything an int holds.
/// </param>
/// <param name="RegisteredWithheldCount">
/// Registered rows whose removable verdict was taken away because a read established
/// nothing, whether or not the file is still on the disk:
/// <c>ScanResult.RegisteredWithheldCount</c>, which is a member of the three-way
/// partition of the rows the scan kept.
///
/// A THIRD POPULATION AND NOT A RESTATEMENT OF EITHER OF THE OTHERS.
/// <paramref name="WithheldPatchCount"/> answers what the withholding COST, so it
/// counts only the rows whose file is there; this is a member of a three-way
/// partition of the kept list, which lists a row whose file has gone like any other.
/// <paramref name="WithheldCandidateCount"/> never came from the registered set at
/// all. The two withheld registration figures agree on a machine whose cache is
/// intact and differ by exactly the rows something else has already removed, which is
/// a fact about that machine and is why both travel.
///
/// NO CAUSE TRAVELS WITH IT EITHER. Six separate findings take a removable verdict
/// away and they are different facts about a machine, so a sentence naming one would
/// be false of the rows the others put in the count.
/// </param>
/// <param name="WithheldIdentityUnestablishedCount">
/// Candidates kept back one at a time because the filesystem would not say which file
/// the candidate's own path names: at the identity comparison, or at the
/// declared-product screen when it came to compare the candidate with the packages or
/// copies the records name, where a candidate found gone is counted too:
/// <c>ScanResult.WithheldBy.IdentityUnestablishedCount</c>. The first of the twelve
/// counts that split <paramref name="WithheldCandidateCount"/>.
///
/// IT IS NOT <c>machine</c>'S CANDIDATE-SIDE REFUSAL TOTAL. That total counts the
/// identity comparison's refusals, which are the candidates that comparison kept, so
/// this count less that total is the candidates the screen kept on its own read.
/// </param>
/// <param name="WithheldWholesaleCount">
/// Candidates kept back in one go on a fact about the machine rather than about any
/// file: the whole walk-derived offer (<c>ScanResult.WithheldBy.WholesaleCount</c>).
///
/// NO CAUSE TRAVELS WITH IT. Three named conditions reach that branch and any
/// combination of them can be true at once, so nothing may say which one held a
/// machine's offer back.
/// </param>
/// <param name="WithheldDeclaredProductInstalledCount">
/// Candidates the declared-product screen kept back because Windows still holds a
/// record of the product the file itself declares it belongs to, and some
/// installation of that product opens a package, its cached copy or its original at
/// a source, that the screen could not show is another file; or because an
/// installation the scan could not rule out as a second copy of a program opens the
/// file as its package:
/// <c>ScanResult.WithheldBy.DeclaredProductInstalledCount</c>.
/// </param>
/// <param name="WithheldDeclaredProductUnestablishedCount">
/// Candidates the same screen kept back having settled nothing about them:
/// <c>ScanResult.WithheldBy.DeclaredProductUnestablishedCount</c>.
///
/// IT IS A DIFFERENT FINDING FROM THE ONE ABOVE IT AND THE TWO MUST NOT BE ADDED. That
/// one starts from a positive answer that the product is installed; this one is a
/// question that went unanswered, and it covers two different inabilities under one
/// name deliberately. A total over the pair would state a cause true of neither.
/// </param>
/// <param name="WithheldScreenUnansweredCount">
/// Candidates kept back because the screen answered a different number of files than it
/// was handed, so no verdict in it could be attached to any file:
/// <c>ScanResult.WithheldBy.ScreenUnansweredCount</c>.
///
/// A THIRD FACT AND NOT A MEMBER OF EITHER SCREEN OUTCOME ABOVE. Those two are verdicts
/// the screen reached about a file; this is the screen having reached none, and filing
/// it under either would state a cause that was never established.
/// </param>
/// <param name="WithheldUnderADayOldCount">
/// Candidates the age check kept back because their times show them to be under a day
/// old: <c>ScanResult.WithheldBy.UnderADayOldCount</c>. The command line's held-back
/// sentence does not count them and the window's finished screen does; either way they
/// are among the files left alone.
/// </param>
/// <param name="WithheldAgeUnestablishedCount">
/// Candidates the age check kept back because their age was not established:
/// <c>ScanResult.WithheldBy.AgeUnestablishedCount</c>. Both hosts' held-back sentences
/// count them, and no reason line names them.
///
/// A DIFFERENT FINDING FROM THE ONE ABOVE AND THE TWO MUST NOT BE ADDED. That one is an
/// age the scan established; this is an age it did not, and a total over the pair would
/// say the files were new when some of them were never shown to be.
/// </param>
/// <param name="WithheldDeclaredPatchRegisteredCount">
/// Patch copies the declared-product screen kept back because Windows holds a
/// registration of the patch each declares, and for at least one registration the screen
/// could not show that the cached copy it records is a different file:
/// <c>ScanResult.WithheldBy.DeclaredPatchRegisteredCount</c>.
/// Neither host's held-back sentence counts them, as with
/// <paramref name="WithheldDeclaredProductInstalledCount"/>.
/// </param>
/// <param name="WithheldDeclaredPatchUnestablishedCount">
/// Patch copies the same screen kept back without settling them: the copy yielded no
/// patch code and target products to ask about, or the registrations of the patch it
/// declares could not all be found:
/// <c>ScanResult.WithheldBy.DeclaredPatchUnestablishedCount</c>. Both
/// hosts' held-back sentences count them, and the command line names them in a reason
/// line of their own.
///
/// NOT TO BE ADDED TO THE ONE ABOVE, for the reason given at the product half's pair.
///
/// THESE FOUR ARE APPENDED AFTER THE OTHER FIVE, in the order the split declares them,
/// because the members here are positional <c>int</c> parameters and an insertion would
/// re-point every argument after it with nothing in the build to say so.
/// </param>
/// <param name="WithheldContainmentRefusedCount">
/// Files from the folder walk that the containment check refused, each being a reparse
/// point or resolving somewhere other than directly in the folder:
/// <c>ScanResult.WithheldBy.ContainmentRefusedCount</c>. Both hosts' held-back sentences
/// count them, and no reason line names them.
///
/// APPENDED AFTER THE NINE ABOVE, with the three members after it, for the reason the
/// four before it were.
/// </param>
/// <param name="WithheldContainmentUnestablishedCount">
/// Files from the folder walk that the containment check could not answer for, their
/// attributes or the location they resolve to not reading, or the Installer folder's own
/// location not being established, so that where they resolve could not be compared with
/// it: <c>ScanResult.WithheldBy.ContainmentUnestablishedCount</c>. Counted and left
/// unnamed as the one above is.
///
/// NOT TO BE ADDED TO THE ONE ABOVE. That one is a finding about the file and this one
/// the check failing to answer, and a total over the pair would say the files were
/// reparse points or lay elsewhere when some of them were never shown to be.
/// </param>
/// <param name="SupersededContainmentRefusedCount">
/// Superseded rows whose file is on disk, that carried a removable verdict to the offer,
/// and that the containment check then refused:
/// <c>ScanResult.SupersededContainmentRefusedCount</c>. They stay among the registered
/// files, so <paramref name="RegisteredCount"/> has them, and both hosts count them among
/// the superseded files held back.
///
/// IT IS NOT <paramref name="WithheldPatchCount"/>, which counts superseded rows on disk
/// whose removable verdict was taken away. These rows kept theirs, and the two travel
/// apart because they are different findings.
/// </param>
/// <param name="SupersededContainmentUnestablishedCount">
/// The same rows where the containment check could not answer:
/// <c>ScanResult.SupersededContainmentUnestablishedCount</c>. Not to be added to the one
/// above, for the reason given at the folder walk's pair.
/// </param>
/// <param name="SupersededRecordedPathUnestablishedCount">
/// Superseded files the scan withheld on a recorded path it could not settle: rows the
/// enumeration's scan-wide withholding took while that condition held, whose file is on
/// disk (<c>ScanResult.SupersededRecordedPathUnestablishedCount</c>).
///
/// A SUB-COUNT OF <paramref name="WithheldPatchCount"/>, never added to it. It counts a
/// row whether or not the scan also failed to account for every installed product,
/// which withholds the same rows, so it answers how many files this one condition holds
/// back, and <paramref name="WithheldPatchCount"/> less this is not a count of any other
/// condition. A row an earlier check had already withheld is not in it.
/// </param>
/// <param name="UnsettledEnumeratedProductCount">
/// Program codes the enumeration returned that the scan could not check: an installation
/// whose records came back short where the registry does not hold, under that
/// installation's own account, what the failed read would have returned, or a code whose
/// own keyed answer did not settle and whose registry entry names a cached file the
/// enumeration never claimed that is on the disk. One per code. Every installation whose
/// records came back short is in <c>UnreadableProductCount</c> whatever the registry
/// holds.
///
/// WITH <paramref name="UnansweredProductCount"/> AND THE MACHINE OBJECT'S UNPARSEABLE KEY
/// COUNT IT IS THE WHOLE OF THE FIGURE THE APPLICATION-LOG NOTICE CARRIES, and the three
/// never overlap, so their sum is that figure exactly. The sum is not sent.
///
/// THESE FOUR ARE APPENDED LAST, for the reason the members before them were.
/// </param>
/// <param name="RecoveredEnumeratedInstallationCount">
/// Installations of a product the enumeration returned, found by the keyed ask for that
/// product under an account or context the enumeration did not list them in. Each is
/// asked every question an enumerated installation is, and withholds nothing by being
/// found.
/// </param>
/// <param name="UnattributedPatchFileCount">
/// Patch registrations naming a cached file the enumeration never claimed that is on the
/// disk, whose key name yields no code or whose code no registry listing of a product the
/// scan asks by name records it as holding. One per registration. Any above zero
/// withholds every superseded patch. A count of registrations, never added to a count of
/// products.
/// </param>
/// <param name="SupersededScanWideWithheldCount">
/// Superseded files a condition on the whole machine held back, whose file is on disk
/// (<c>ScanResult.SupersededScanWideWithheldCount</c>): a program entry the scan could not
/// check, a cached patch file it could not match to a program it asks about, a recorded
/// path it could not settle, or a machine-wide list of patch registrations that did not
/// run to its end.
///
/// A SUB-COUNT OF <paramref name="WithheldPatchCount"/>, never added to it. The rest of
/// that count is files a check of the file's own patch withheld, so the two tell a hold
/// on the whole machine from a hold on one file.
/// <paramref name="SupersededRecordedPathUnestablishedCount"/> is a sub-count of this.
/// </param>
/// <param name="WithheldSecondCopyUnestablishedCount">
/// Candidates the declared-product screen kept back because an installation the scan
/// could not rule out as a second copy of a program opens packages that could not all
/// be seen, or because an installation's cached package did not say which product it
/// declares, its own record did not show it to be an ordinary installation, and it is not
/// shown to have no package for Windows Installer to open:
/// <c>ScanResult.WithheldBy.SecondCopyUnestablishedCount</c>. The last of the twelve
/// counts that split <paramref name="WithheldCandidateCount"/>, appended after the
/// members before it for the reason they were.
///
/// NEVER ADDED TO <paramref name="WithheldDeclaredProductUnestablishedCount"/>. That one
/// is about the product a file declares; this is about another installation on the
/// machine.
/// </param>
/// <param name="SourcesGivenUpStoppedCount">
/// Drives or shares, or paths of another form, that the declared-product screen gave up in
/// this scan and kept at least one file at, where the person stopped waiting for it
/// (<see cref="SourceRootGiveUpRoute.StoppedWaiting"/>). The first of the counts, one for
/// each way the screen gives one up, read off
/// <see cref="ScanResult.SourceRootsGivenUpKeepingFiles"/>. A drive or share given up with
/// no file kept at it is in none of them, having changed nothing the scan decided.
///
/// COUNTS ONLY. No drive's or share's name travels in this report.
/// </param>
/// <param name="SourcesGivenUpNoAnswerCount">
/// The same, where a read there did not answer within the screen's time limit
/// (<see cref="SourceRootGiveUpRoute.NoAnswer"/>).
/// </param>
/// <param name="SourcesGivenUpSlowFailureCount">
/// The same, where a package read there answered false only after a long wait
/// (<see cref="SourceRootGiveUpRoute.SlowFailure"/>).
/// </param>
/// <param name="SourcesGivenUpFailedReadsCount">
/// The same, where the package reads there that answered false took more time between them
/// than the screen allows (<see cref="SourceRootGiveUpRoute.FailedReadsAddUp"/>).
/// </param>
/// <param name="SourcesGivenUpReadTimeCount">
/// The same, where all the reads there took more time between them than the screen allows
/// one drive or share, whatever each answered (<see cref="SourceRootGiveUpRoute.ReadsAddUp"/>).
///
/// EACH DRIVE OR SHARE IS IN ONE OF THEM. Where the read that gave it up met more than one
/// way, it takes the one <see cref="SourceRootGiveUpRoute"/> declares first, which is the
/// order of these keys, so their sum is how many drives and shares the scan gave up and kept
/// a file at. The sum is not sent.
/// </param>
/// <param name="FilesKeptForSourcesGivenUpCount">
/// Files the screen kept in this scan whose check stopped at a read it refused for a drive
/// or share it gave up (<see cref="SourceRootGivenUp.FilesKept"/>, which says which reads
/// those are), summed over the drives and shares the counts above count. A check stops at
/// the first thing that keeps its file, so this says where the check stopped, never that the
/// drive or share alone kept the file.
///
/// A COUNT OF FILES ON THE WITHHELD LIST, NEVER AN ARM OF ITS SPLIT AND NEVER ADDED TO IT.
/// Every file it counts is counted in the split under the screen's own verdict.
/// </param>
/// <param name="SourceWaitsShownCount">
/// How many waits the screen made in this scan, each a read still waiting after about a
/// second, for which a host showing the waits put its waiting line up
/// (<see cref="ScanResult.SourceWaitCount"/>). Every wait is counted, whether or not its
/// drive or share was then given up.
/// </param>
/// <param name="SecondCopyListedCheckedCount">
/// The listed installations whose cached package the declared-product screen looked for in
/// this scan (<see cref="CachedPackageCensus.ListedChecked"/>): every one, once, where an
/// installation package reached that step, and none where none did. Off
/// <see cref="ScanResult.CachedPackageCensus"/>, as is every key after it.
///
/// THEY ARE APPENDED LAST, for the reason the members before them were. Counts
/// of installations and of files, and nothing naming one.
/// </param>
/// <param name="SecondCopyKeepPathUnreadableCount">
/// Installations that set the hold counted in
/// <paramref name="WithheldSecondCopyUnestablishedCount"/>: their cached package did not say
/// which product it declares, their own record did not show an ordinary installation, and
/// they are not shown to have no package for Windows Installer to open, nor, where they record
/// none, to open only packages their sources name that the screen saw.
/// Counted where the screen reads them, before any file's verdict, so they are counted
/// whether or not any file was then held back for them. This one counts those for which
/// Windows gave no answer about the cached package's path: a read that failed, or an answer
/// that it does not know the product in that account and context.
///
/// THE FIVE KEYS FROM THIS ONE SAY WHAT EACH SUCH INSTALLATION'S CACHED PACKAGE GAVE, AND
/// THE THREE AFTER THEM WHY ITS RECORD DID NOT SETTLE IT. Each installation is in one of
/// each, so the five and the three add up to the same figure, which is not sent.
/// </param>
/// <param name="SecondCopyKeepNoneRecordedCount">
/// The same installations, where the installation records no cached package, and a source it
/// names could not be ruled out.
/// </param>
/// <param name="SecondCopyKeepNotThereCount">
/// The same, where the cached package's path names no file that is there.
/// </param>
/// <param name="SecondCopyKeepWouldNotReadCount">
/// The same, where the cached package would not give up its product code: it would not open,
/// or its <c>Property</c> table or the value would not read.
/// </param>
/// <param name="SecondCopyKeepNoProductCodeCount">
/// The same, where the cached package read and declares no product code, or one that is not a
/// well-formed GUID, or reads as a patch.
/// </param>
/// <param name="SecondCopyKeepAnotherAccountCount">
/// The same installations, where the installation is per-user and not shown to belong to
/// the account the app runs as, so its record was not read.
/// </param>
/// <param name="SecondCopyKeepPackageCodeUnansweredCount">
/// The same, where the record's <c>PackageCode</c> did not read as a value.
/// </param>
/// <param name="SecondCopyKeepInstanceTypeNotOrdinaryCount">
/// The same, where <c>PackageCode</c> read and <c>InstanceType</c> did not read as an
/// ordinary installation.
/// </param>
/// <param name="SecondCopyKeepPerMachineCount">
/// The same installations, where the installation is per-machine. Every one in
/// <paramref name="SecondCopyKeepAnotherAccountCount"/> is per-user.
/// </param>
/// <param name="SecondCopyReleasedOrdinaryCount">
/// Installations whose cached package did not say which product it declares and whose own
/// record shows an ordinary installation, so they do not set the hold. One with no package
/// for Windows Installer to open is counted in
/// <paramref name="SecondCopyReleasedOpensNoPackageCount"/> instead, and its record is not
/// read.
/// </param>
/// <param name="SecondCopyUnruledCheckedCount">
/// The listed installations not ruled out as a second copy of a program whose packages the
/// screen looked for, the one it stopped at included. None where an installation sets the
/// hold, which the screen then does not look further than, or where no installation package
/// the answer about its own product lets through reached that step.
///
/// THE NINE KEYS AFTER THIS ONE SAY WHICH STEP STOPPED THE READ, at the first such
/// installation whose packages could not all be seen, which holds back every installation
/// package the screen would otherwise let through. At most one of them is 1 and the rest 0.
/// Counted where the read is made, before any file's verdict.
/// </param>
/// <param name="SecondCopyUnseenPathUnreadableCount">
/// Windows gave no answer about the cached package's path, as for
/// <paramref name="SecondCopyKeepPathUnreadableCount"/>.
/// </param>
/// <param name="SecondCopyUnseenNoneRecordedCount">
/// The installation records no cached package, and a source it names could not be ruled out.
/// </param>
/// <param name="SecondCopyUnseenNotThereCount">The cached package's path names no file that is there.</param>
/// <param name="SecondCopyUnseenWouldNotIdentifyCount">
/// The cached package's volume and file ID would not read.
/// </param>
/// <param name="SecondCopyUnseenWouldNotReadCount">
/// The cached package would not give up its product code, as for
/// <paramref name="SecondCopyKeepWouldNotReadCount"/>.
/// </param>
/// <param name="SecondCopyUnseenNoProductCodeCount">
/// The cached package declares no product code, or one that is not a well-formed GUID, or
/// reads as a patch.
/// </param>
/// <param name="SecondCopyUnseenPerUserUnmanagedCount">
/// The installation is in the per-user unmanaged context, whose source list is not read.
/// </param>
/// <param name="SecondCopyUnseenSourcesGivenUpCount">
/// A package in a folder its sources name was refused for a drive or share given up for the
/// scan.
/// </param>
/// <param name="SecondCopyUnseenSourceNotRuledOutCount">
/// Its sources could not be ruled out for any other reason
/// (<see cref="CachedPackageCensus.UnseenSourceNotRuledOut"/>).
/// </param>
/// <param name="SecondCopyUnseenPerMachineCount">
/// 1 where the installation the read stopped at is per-machine, and 0 where it is per-user or
/// the read did not stop.
/// </param>
/// <param name="SecondCopyUnseenByNameFileCount">
/// Files held back because a package in a folder on the network, which the sources of an
/// installation not ruled out as a second copy name, or those of an installation recording no
/// cached package whose record does not show an ordinary installation, and which the file could
/// be by its name, could not be ruled out. Each is also counted in
/// <paramref name="WithheldSecondCopyUnestablishedCount"/>.
/// </param>
/// <param name="SecondCopyReleasedOpensNoPackageCount">
/// Installations which do not set the hold because Windows Installer has no package to open
/// for them, whatever their own record shows: per machine, recording no cached package, and
/// with no source list, by the API's answer and in the registry alike
/// (<see cref="CachedPackageCensus.ReleasedOpensNoPackage"/>). Each is also counted in
/// <paramref name="SecondCopyListedCheckedCount"/>, and in none of the keys from
/// <paramref name="SecondCopyKeepPathUnreadableCount"/> to
/// <paramref name="SecondCopyReleasedOrdinaryCount"/>. Counted where the screen reads them,
/// before any file's verdict.
/// </param>
public sealed record ScanInfo(
    long DurationMs,
    int RegisteredCount,
    long RegisteredBytes,
    int OrphanedCount,
    int SupersededCount,
    int ObsoletedCount,
    long RemovableBytes,
    int MissingFromDiskCount,
    int MissingNeededCount,
    int WithheldPatchCount,
    int UnreadableProductCount,
    int SkippedProductRowCount,
    int UnclaimedProductFileCount,
    int UnclaimedPatchFileCount,
    int RecoveredProductCount,
    int UnansweredProductCount,
    int WithheldCandidateCount,
    long WithheldTotalBytes,
    int RegisteredWithheldCount,
    int WithheldIdentityUnestablishedCount,
    int WithheldWholesaleCount,
    int WithheldDeclaredProductInstalledCount,
    int WithheldDeclaredProductUnestablishedCount,
    int WithheldScreenUnansweredCount,
    int WithheldUnderADayOldCount,
    int WithheldAgeUnestablishedCount,
    int WithheldDeclaredPatchRegisteredCount,
    int WithheldDeclaredPatchUnestablishedCount,
    int WithheldContainmentRefusedCount,
    int WithheldContainmentUnestablishedCount,
    int SupersededContainmentRefusedCount,
    int SupersededContainmentUnestablishedCount,
    int SupersededRecordedPathUnestablishedCount,
    int UnsettledEnumeratedProductCount,
    int RecoveredEnumeratedInstallationCount,
    int UnattributedPatchFileCount,
    int SupersededScanWideWithheldCount,
    int WithheldSecondCopyUnestablishedCount,
    int SourcesGivenUpStoppedCount,
    int SourcesGivenUpNoAnswerCount,
    int SourcesGivenUpSlowFailureCount,
    int SourcesGivenUpFailedReadsCount,
    int SourcesGivenUpReadTimeCount,
    int FilesKeptForSourcesGivenUpCount,
    int SourceWaitsShownCount,
    int SecondCopyListedCheckedCount,
    int SecondCopyKeepPathUnreadableCount,
    int SecondCopyKeepNoneRecordedCount,
    int SecondCopyKeepNotThereCount,
    int SecondCopyKeepWouldNotReadCount,
    int SecondCopyKeepNoProductCodeCount,
    int SecondCopyKeepAnotherAccountCount,
    int SecondCopyKeepPackageCodeUnansweredCount,
    int SecondCopyKeepInstanceTypeNotOrdinaryCount,
    int SecondCopyKeepPerMachineCount,
    int SecondCopyReleasedOrdinaryCount,
    int SecondCopyUnruledCheckedCount,
    int SecondCopyUnseenPathUnreadableCount,
    int SecondCopyUnseenNoneRecordedCount,
    int SecondCopyUnseenNotThereCount,
    int SecondCopyUnseenWouldNotIdentifyCount,
    int SecondCopyUnseenWouldNotReadCount,
    int SecondCopyUnseenNoProductCodeCount,
    int SecondCopyUnseenPerUserUnmanagedCount,
    int SecondCopyUnseenSourcesGivenUpCount,
    int SecondCopyUnseenSourceNotRuledOutCount,
    int SecondCopyUnseenPerMachineCount,
    int SecondCopyUnseenByNameFileCount,
    int SecondCopyReleasedOpensNoPackageCount)
{
    public static ScanInfo From(ScanResult scan, long durationMs)
    {
        // DERIVED FROM THE OFFER, WHICH IS WHAT THESE TWO KEYS HAVE ALWAYS MEANT, and
        // the obsoleted one is now structurally zero rather than incidentally so: an
        // obsoleted patch cannot reach the offer at all. It stays derived rather than
        // written as a literal, because the derivation is what would notice if one ever
        // did, where a hard-coded zero would report a clean shape over it.
        //
        // THE SCAN-TIME COUNTS ARE THE DIFFERENT QUESTION AND TRAVEL SEPARATELY. These
        // two answer what this run OFFERED; the machine object's registration counts
        // answer what the machine HAS. For obsoleted patches the second is the only one
        // that can ever be non-zero, which is the whole reason it was added.
        var obsoletedCount = scan.RemovableFiles.Count(f => f.IsObsoleted);
        var supersededCount = scan.RemovableFiles.Count(f => f.IsRemovablePatch) - obsoletedCount;
        var givenUp = SourcesGivenUpCounts.Of(scan);
        var cachedPackages = scan.CachedPackageCensus;
        return new(
            durationMs,
            scan.RegisteredPackages.Count,
            scan.RegisteredTotalBytes,
            scan.RemovableFiles.Count - supersededCount - obsoletedCount,
            supersededCount,
            obsoletedCount,
            scan.RemovableTotalBytes,
            scan.MissingFromDiskCount,
            // The wire shape is unchanged and the population behind this one has
            // moved a little; see the field's own note.
            scan.MissingAffectedCount,
            scan.WithheldCount,
            scan.Census.UnreadableProducts,
            scan.Census.SkippedProductRows,
            scan.Census.UnclaimedProductFiles,
            scan.Census.UnclaimedPatchFiles,
            scan.Census.RecoveredProductCount,
            scan.Census.UnansweredProductCount,
            // Off the list rather than tallied, so the number sent and the rows the
            // Details window shows cannot come apart: both are read off the same
            // list. Null is a scan that never reached the decision, which reads as
            // zero here and is not the same thing as a scan that kept nothing back;
            // no run that produces a report can leave it null.
            scan.WithheldFiles?.Count ?? 0,
            // Summed off the same list the count above is read from, so the two are
            // one reading. A null list reads as zero here for the reason it does
            // there: a scan that never reached the decision, which no run producing a
            // report can be.
            scan.WithheldTotalBytes,
            // Counted off the kept list by the scan, so the number sent and the rows
            // the registered-files window shows cannot come apart.
            scan.RegisteredWithheldCount,
            // Eleven of the twelve counts that split the withheld count above, taken off
            // the one place that knows them, so with the twelfth they add up to it. Appended rather than
            // placed among the members they belong beside: every argument after an
            // insertion point re-points at its neighbour's value, and a shift within a
            // run of ints compiles silently.
            scan.WithheldBy.IdentityUnestablishedCount,
            scan.WithheldBy.WholesaleCount,
            scan.WithheldBy.DeclaredProductInstalledCount,
            scan.WithheldBy.DeclaredProductUnestablishedCount,
            scan.WithheldBy.ScreenUnansweredCount,
            scan.WithheldBy.UnderADayOldCount,
            scan.WithheldBy.AgeUnestablishedCount,
            scan.WithheldBy.DeclaredPatchRegisteredCount,
            scan.WithheldBy.DeclaredPatchUnestablishedCount,
            scan.WithheldBy.ContainmentRefusedCount,
            scan.WithheldBy.ContainmentUnestablishedCount,
            // The superseded rows the containment check kept back, by its verdict. They
            // are registered rows and not on the withheld list, so they are no part of
            // the split above.
            scan.SupersededContainmentRefusedCount,
            scan.SupersededContainmentUnestablishedCount,
            // A sub-count of the withheld superseded figure above, never added to it.
            scan.SupersededRecordedPathUnestablishedCount,
            scan.Census.UnsettledEnumeratedProductCount,
            scan.Census.RecoveredEnumeratedInstallationCount,
            scan.Census.UnattributedPatchFileCount,
            // Also a sub-count of the withheld superseded figure, and the one above is
            // a sub-count of this.
            scan.SupersededScanWideWithheldCount,
            // The twelfth arm of the split of the withheld count, appended here rather
            // than beside the other eleven for the reason they were.
            scan.WithheldBy.SecondCopyUnestablishedCount,
            // The drives and shares the screen gave up and kept a file at, by route, the
            // files it kept at them and its waits. Part of no total above.
            givenUp.Stopped,
            givenUp.NoAnswer,
            givenUp.SlowFailure,
            givenUp.FailedReads,
            givenUp.ReadTime,
            givenUp.FilesKept,
            givenUp.WaitsShown,
            // What the screen found of the two conditions under which it holds back every
            // installation package it would otherwise let through.
            cachedPackages.ListedChecked,
            cachedPackages.KeptPathUnreadable,
            cachedPackages.KeptNoneRecorded,
            cachedPackages.KeptNotThere,
            cachedPackages.KeptWouldNotRead,
            cachedPackages.KeptNoProductCode,
            cachedPackages.KeptAnotherAccount,
            cachedPackages.KeptPackageCodeUnanswered,
            cachedPackages.KeptInstanceTypeNotOrdinary,
            cachedPackages.KeptPerMachine,
            cachedPackages.ReleasedOrdinary,
            cachedPackages.UnruledChecked,
            cachedPackages.UnseenPathUnreadable,
            cachedPackages.UnseenNoneRecorded,
            cachedPackages.UnseenNotThere,
            cachedPackages.UnseenWouldNotIdentify,
            cachedPackages.UnseenWouldNotRead,
            cachedPackages.UnseenNoProductCode,
            cachedPackages.UnseenPerUserUnmanaged,
            cachedPackages.UnseenSourcesGivenUp,
            cachedPackages.UnseenSourceNotRuledOut,
            cachedPackages.UnseenPerMachine,
            cachedPackages.UnseenByNameFiles,
            cachedPackages.ReleasedOpensNoPackage);
    }
}

/// <summary>
/// Operation taken after the scan and the outcome.
/// <see cref="Kind"/> is <c>scan</c> when no Move or Delete ran
/// (the scan reported zero orphans, or the completion overlay was
/// dismissed without Move/Delete); <c>move</c> or <c>delete</c>
/// otherwise. <see cref="Outcome"/> is <c>complete</c> /
/// <c>partial</c> / <c>failed</c> / <c>noFiles</c>. <see cref="Errors"/>
/// is the per-category count only (no paths, no exception messages).
/// <see cref="MoveDestinationKind"/> is null when not a move; otherwise
/// <c>sameDrive</c> / <c>differentFixedDrive</c> / <c>removableDrive</c>
/// / <c>uncShare</c> / <c>unknown</c>.
///
/// <see cref="DurationMs"/> is THIS operation's, and the payload also carries the
/// scan's own under <c>scan</c>. This one answers whether a three-thousand-file
/// delete is a pleasant thing to sit through, which nothing else can. Zero on a
/// scan-only run, where no operation ran to time.
///
/// The held-back counts are the act-time re-verify's, and they are NOT the scan's
/// withholding: they count what the check made just before acting did not confirm,
/// where <c>scan.withheldPatchCount</c> and <c>scan.withheldCandidateCount</c> count
/// what never reached the list at all. They are several numbers rather than one because a single batch
/// can meet more than one cause and a cause named for the set would be false of some
/// of its members; they are not summed here for the same reason.
///
/// THE COUNT IS DELIBERATELY NOT WRITTEN HERE. A figure in prose beside a list that
/// moves is a figure that goes stale silently. The list below is the count.
///
/// EACH IS A REQUIRED KEY IN THE VERSION THAT CARRIES IT. Schema 4 carries the first
/// four and schema 5 adds <see cref="HeldBackFileNotConfirmed"/>. A receiver can start
/// requiring a key only while no release sends its version; after that, requiring it
/// would reject every report from that release, so it could never be required at all
/// and a machine could stop sending it with nothing to see. Receiver deployed first,
/// client second.
///
/// THE KEYS AFTER THE CAUSES ARE <see cref="ScanInfo"/>'S KEYS OF THE SAME NAMES, FOR THE
/// CHECK MADE WHEN MOVE OR DELETE IS CLICKED: the drives and shares its declared-product
/// screen gave up and kept a file at, by route, the files it kept at them and the waits it
/// made (<see cref="ReverifyResult.SourceRootsGivenUpKeepingFiles"/>,
/// <see cref="ReverifyResult.SourceWaitCount"/>), each read as the scan's is. The check
/// gives up its own drives and shares, whatever the scan before it gave up. Every file the
/// files count takes in is held back as <see cref="HeldBackFileNotConfirmed"/> or
/// <see cref="HeldBackOwnershipUnestablished"/> and counted there, so it is a part of those
/// two and never added to them. All of them are nought on a scan-only report.
/// </summary>
public sealed record OperationInfo(
    string Kind,
    string Outcome,
    long DurationMs,
    int FilesProcessed,
    int FilesFailed,
    long BytesFreed,
    IReadOnlyList<ErrorBucket> Errors,
    string? MoveDestinationKind,
    int HeldBackReclaimed,
    int HeldBackRecordsChanged,
    int HeldBackRecordsUnreadable,
    int HeldBackOwnershipUnestablished,
    int HeldBackFileNotConfirmed,
    int SourcesGivenUpStoppedCount,
    int SourcesGivenUpNoAnswerCount,
    int SourcesGivenUpSlowFailureCount,
    int SourcesGivenUpFailedReadsCount,
    int SourcesGivenUpReadTimeCount,
    int FilesKeptForSourcesGivenUpCount,
    int SourceWaitsShownCount)
{
    // Every held-back cause and every give-up count nought, no check having run.
    public static OperationInfo ScanOnly() =>
        new(OperationKinds.Scan, OperationOutcomes.NoFiles, 0, 0, 0, 0,
            Array.Empty<ErrorBucket>(), null, 0, 0, 0, 0, 0,
            0, 0, 0, 0, 0, 0, 0);

    /// <summary>
    /// The operation block of a Move. <paramref name="check"/> is the result of the check made
    /// when Move was clicked, with what the re-read under the installer lock held back added to
    /// it.
    /// </summary>
    public static OperationInfo FromMove(MoveResult result, long bytesFreed, long durationMs,
        string moveDestinationKind, ReverifyResult check)
    {
        var heldBack = check.Reasons;
        var givenUp = SourcesGivenUpCounts.Of(check);
        return new(
            OperationKinds.Move,
            ClassifyOutcome(result.MovedCount, result.Errors.Count),
            durationMs,
            result.MovedCount,
            result.Errors.Count,
            bytesFreed,
            BucketErrors(result.Errors),
            moveDestinationKind,
            heldBack.Reclaimed,
            heldBack.RecordsChanged,
            heldBack.RecordsUnreadable,
            heldBack.OwnershipUnestablished,
            heldBack.FileNotConfirmed,
            givenUp.Stopped,
            givenUp.NoAnswer,
            givenUp.SlowFailure,
            givenUp.FailedReads,
            givenUp.ReadTime,
            givenUp.FilesKept,
            givenUp.WaitsShown);
    }

    /// <summary>
    /// The operation block of a Delete, <paramref name="check"/> being as
    /// <see cref="FromMove"/> takes it.
    /// </summary>
    public static OperationInfo FromDelete(DeleteResult result, long bytesFreed, long durationMs,
        ReverifyResult check)
    {
        var heldBack = check.Reasons;
        var givenUp = SourcesGivenUpCounts.Of(check);
        return new(
            OperationKinds.Delete,
            ClassifyOutcome(result.DeletedCount, result.Errors.Count),
            durationMs,
            result.DeletedCount,
            result.Errors.Count,
            bytesFreed,
            BucketErrors(result.Errors),
            null,
            heldBack.Reclaimed,
            heldBack.RecordsChanged,
            heldBack.RecordsUnreadable,
            heldBack.OwnershipUnestablished,
            heldBack.FileNotConfirmed,
            givenUp.Stopped,
            givenUp.NoAnswer,
            givenUp.SlowFailure,
            givenUp.FailedReads,
            givenUp.ReadTime,
            givenUp.FilesKept,
            givenUp.WaitsShown);
    }

    /// <summary>
    /// The outcome label, decided from the two counts the finished batch
    /// reports and nothing else. Deliberately the same rule as
    /// <see cref="Helpers.CliContract.ClassifyFileOperation"/>, which reaches
    /// the same three answers for the CLI's exit code: the two surfaces
    /// describe one operation and must agree about it.
    ///
    /// No candidate total is taken. Those two counts ARE the batch that was
    /// attempted, whereas any total handed in from outside describes an
    /// earlier moment, and the act-time re-verify sits between the two: it can
    /// hold a candidate back, so a scan-shaped total exceeds what was attempted
    /// and "everything failed" stops being expressible as failed == total. That
    /// is what silently retired the failed label once, and a rule that reads
    /// only its own batch cannot be broken again by a stage landing between the
    /// scan and the act.
    /// </summary>
    private static string ClassifyOutcome(int processed, int failed)
    {
        if (failed == 0) return OperationOutcomes.Complete;
        if (processed > 0) return OperationOutcomes.Partial;
        return OperationOutcomes.Failed;
    }

    private static IReadOnlyList<ErrorBucket> BucketErrors(IReadOnlyList<FileOperationError> errors)
    {
        if (errors.Count == 0) return Array.Empty<ErrorBucket>();
        return errors
            .GroupBy(e => e.GetType().Name)
            .Select(g => new ErrorBucket(g.Key, g.Count()))
            .OrderByDescending(b => b.Count)
            .ToList();
    }
}

/// <summary>
/// The drives and shares one pass of the declared-product screen gave up and kept a file at,
/// counted by the route each was given up by, with the files kept at them and the waits the
/// pass made: the counts <see cref="ScanInfo"/> and <see cref="OperationInfo"/> each end with,
/// read for both off one expression.
/// </summary>
internal readonly record struct SourcesGivenUpCounts(
    int Stopped,
    int NoAnswer,
    int SlowFailure,
    int FailedReads,
    int ReadTime,
    int FilesKept,
    int WaitsShown)
{
    /// <summary>The counts for a scan.</summary>
    internal static SourcesGivenUpCounts Of(ScanResult scan) =>
        Count(scan.SourceRootsGivenUpKeepingFiles, scan.SourceWaitCount);

    /// <summary>The counts for the check made when Move or Delete is clicked.</summary>
    internal static SourcesGivenUpCounts Of(ReverifyResult check) =>
        Count(check.SourceRootsGivenUpKeepingFiles, check.SourceWaitCount);

    /// <summary>
    /// Counts each root in <paramref name="keepingFiles"/> under its route, and adds up the
    /// files kept at them.
    ///
    /// A ROUTE WITH NO COUNT HERE THROWS, so the first report meeting it fails rather than
    /// sending counts that add up to less than the drives and shares the pass gave up.
    /// A member added to <see cref="SourceRootGiveUpRoute"/> gains a count here, and a key in
    /// both report blocks, in the same edit.
    /// </summary>
    /// <exception cref="ArgumentOutOfRangeException">
    /// A root given up by a route with no count, which is a defect in this type and not a
    /// machine state.
    /// </exception>
    private static SourcesGivenUpCounts Count(IReadOnlyList<SourceRootGivenUp> keepingFiles, int waits)
    {
        var counts = new SourcesGivenUpCounts(0, 0, 0, 0, 0, 0, waits);
        foreach (var root in keepingFiles)
        {
            counts = root.Route switch
            {
                SourceRootGiveUpRoute.StoppedWaiting => counts with { Stopped = counts.Stopped + 1 },
                SourceRootGiveUpRoute.NoAnswer => counts with { NoAnswer = counts.NoAnswer + 1 },
                SourceRootGiveUpRoute.SlowFailure => counts with { SlowFailure = counts.SlowFailure + 1 },
                SourceRootGiveUpRoute.FailedReadsAddUp => counts with { FailedReads = counts.FailedReads + 1 },
                SourceRootGiveUpRoute.ReadsAddUp => counts with { ReadTime = counts.ReadTime + 1 },
                _ => throw new ArgumentOutOfRangeException(nameof(keepingFiles), root.Route,
                    "A give-up route with no count. Add it to SourcesGivenUpCounts, and a key for it to "
                    + "both blocks of the opt-in report, in the same edit as the enum member."),
            };
            counts = counts with { FilesKept = counts.FilesKept + root.FilesKept };
        }

        return counts;
    }
}

/// <summary>
/// One error category in a result-log operation: the category name and how
/// many files fell into it, and nothing else. The category name is the error
/// record's type name, so it is a value the receiver allowlists.
/// </summary>
public sealed record ErrorBucket(string Category, int Count);

public static class OperationKinds
{
    public const string Scan = "scan";
    public const string Move = "move";
    public const string Delete = "delete";
}

public static class OperationOutcomes
{
    public const string Complete = "complete";
    public const string Partial = "partial";
    public const string Failed = "failed";
    public const string NoFiles = "noFiles";
}

public static class MoveDestinationKinds
{
    public const string SameDrive = "sameDrive";
    public const string DifferentFixedDrive = "differentFixedDrive";
    public const string RemovableDrive = "removableDrive";
    public const string UncShare = "uncShare";
    public const string Unknown = "unknown";
}
