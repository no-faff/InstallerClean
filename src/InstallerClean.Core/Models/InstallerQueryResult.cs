namespace InstallerClean.Models;

/// <summary>
/// The output of one <c>InstallerQueryService</c> enumeration: every
/// <c>LocalPackage</c> path the Windows Installer API and the registry fallback
/// between them claim, plus how much of the enumeration failed to read.
/// </summary>
/// <param name="Packages">
/// One row per claimed path. On a run where
/// <see cref="UnaccountedProductCount"/> is non-zero, no row carries
/// <see cref="RegisteredPackage.IsRemovable"/>: the removable class is withheld
/// wholesale (see <see cref="RecordsIncomplete"/>) and the rows that would have
/// carried it are marked <see cref="RegisteredPackage.RemovableWithheld"/>
/// instead.
/// </param>
/// <param name="UnaccountedProductCount">
/// Installed products this enumeration did not account for. Surfaced to the user
/// as the scan summary's kept-patches notice, and the trigger for withholding the
/// removable class.
///
/// FIVE contributors, and only the first two are failures to read. A product row
/// the API skipped (identity unknowable, counted one per row); a product whose
/// rows came back but whose LocalPackage value, or one of whose patch rows, would
/// not read. Then two that are absences rather than failures: a shortfall of the
/// API's product headcount against the registry's own, and a cached file the
/// registry claims and the API never mentioned. Both of those are computed NET of
/// the read failures (see the subtractions at the assembly site), so by
/// construction they count only products where nothing failed to read at all.
/// A product meeting more than one contributor counts once.
///
/// The fifth is neither a failure to read nor an absence: a product code the
/// registry named and Windows would not say was installed or not, so whether the
/// enumeration was complete could not be established for it either way. Its
/// opposite number, a registry code confirmed installed and recovered into the
/// questions the scan asks, contributes NOTHING here, and that asymmetry is the
/// point: a product that can be asked about is asked, and only a product nobody
/// can get an answer about withholds.
///
/// IT WAS CALLED <c>UnreadableProductCount</c> AND THAT NAME WAS A CAUSE STATED
/// FOR A MIXED SET, inside the app rather than on a screen: half its contributors
/// are absences and nothing failed to read for them. Any sentence built on this
/// number is a sentence about all four, which is why the name now says only that
/// the enumeration could not account for them. The terms themselves are in
/// <see cref="Census"/>, separately, for anything that needs to say which.
///
/// It is not an exact headcount either, and cannot be made one. The two absence
/// terms are estimates the assembly site deliberately biases low, and the
/// headcount one can also run HIGH, a UserData product key surviving an uninstall
/// that failed; its tolerance band absorbs the ordinary residue and nothing bounds
/// the rest. So the number is an estimate that can be wrong in either direction,
/// and no surface may present it as a count of programs.
/// </param>
/// <param name="PatchClaims">
/// Every product-to-patch claim this enumeration read, one entry per claim
/// rather than per path. <see cref="Packages"/> answers what a path's verdict is;
/// this answers who to ask about it, which the merge behind
/// <see cref="Packages"/> cannot keep (see <see cref="PatchClaim"/>). Empty on a
/// machine with no registered patches, and empty on a result built by anything
/// that does not enumerate patches.
/// </param>
/// <param name="Census">
/// What the enumeration measured about itself and about the machine, for the
/// opt-in report and for nothing else. Default on a result built by anything that
/// does not enumerate, which reads as a census nobody took rather than as a
/// machine where every term was zero; only a real enumeration's result should be
/// read for these.
/// </param>
public record InstallerQueryResult(
    IReadOnlyList<RegisteredPackage> Packages,
    int UnaccountedProductCount = 0,
    IReadOnlyList<PatchClaim>? PatchClaims = null,
    EnumerationCensus Census = default)
{
    /// <summary>Never null: an absent list reads as no claims rather than as a fault.</summary>
    public IReadOnlyList<PatchClaim> PatchClaims { get; init; } = PatchClaims ?? Array.Empty<PatchClaim>();

    /// <summary>
    /// The enumeration did not account for at least one installed product, so it
    /// cannot say of any patch that no installed product still needs it. Whether
    /// that product's records failed to read or were never reached does not enter
    /// into it: what withholds the verdict is the missing claim, not the mechanism
    /// (see <see cref="UnaccountedProductCount"/> for the four). Every consumer of
    /// a removable verdict inherits this through the withheld rows themselves; it
    /// is exposed for the copy that explains the shorter list.
    /// </summary>
    public bool RecordsIncomplete => UnaccountedProductCount > 0;
}

/// <summary>
/// The terms behind <see cref="InstallerQueryResult.UnaccountedProductCount"/>,
/// plus the shape facts one enumeration can see, carried separately so a report
/// never has to state one cause for a quantity built from several.
///
/// EVERY FIELD HERE IS INSTRUMENTATION. Nothing decides a file's fate on any of
/// them and nothing may start: the classification is settled where it is settled,
/// and a counter that acquired a consumer would be a second, quieter copy of a
/// rule that already exists in one place.
///
/// They exist because the evidence base for every safety claim this app makes is
/// one machine, and a single machine can falsify a universal and can never confirm
/// one. Each is a count or a fixed label, never a path, a name or an identifier.
/// </summary>
/// <param name="UnreadableProducts">
/// Products whose records came back short: a skipped enumeration row, an
/// unreadable <c>LocalPackage</c> value, or a patch enumeration that did not run
/// to a clean end. One per product however many of the three it met. An exact
/// per-product tally with no floor and no threshold under it, which is what
/// separates it from the two figures the withholding is actually computed from.
/// </param>
/// <param name="SkippedProductRows">
/// Enumeration rows the product loop could not read at all, one per row. A subset
/// of <see cref="UnreadableProducts"/>, which is seeded from it and then grows.
/// Carried separately because it, and not the wider count, is what the shortfall
/// arithmetic subtracts.
/// </param>
/// <param name="RegistryProductKeys">
/// Product subkeys the registry fallback walked under <c>UserData</c>. The only
/// independent count of how many products a machine has, and the other half of
/// the shortfall.
/// </param>
/// <param name="UnclaimedProductFiles">
/// Product registry entries whose cached path the API's own loop never claimed
/// AND whose file is really on the disk. Both halves are load-bearing and both
/// are observed rather than inferred.
/// </param>
/// <param name="UnclaimedPatchFiles">
/// The same for patch entries. A patch entry names no product, so it can
/// establish only that at least one product went unreached.
///
/// THESE FOUR ARE THE TALLIES THE APP'S OWN WITHHOLDING ARITHMETIC IS BUILT FROM,
/// AND THE ARITHMETIC ITSELF IS DELIBERATELY NOT CARRIED. Its two derived terms
/// are a shortfall that is silently zero inside a tolerance band, and a
/// product estimate floored at one by patch evidence and biased low by a generous
/// subtraction. Neither is the count its name would claim, and both are
/// reproducible from these four plus <see cref="UnreadableProducts"/>.
///
/// The band matters most. It was set from the residue of one machine, it decides
/// whether a truncated enumeration is noticed at all, and a machine whose real
/// shortfall it absorbs is indistinguishable from a machine with no shortfall
/// once the derived term is all that survives. Carrying both headcounts is what
/// makes that threshold answerable anywhere but the machine it came from.
/// </param>
/// <param name="NonStringLocalPackageValues">
/// Registrations whose <c>LocalPackage</c> value was present and was not a string.
/// Answers whether anything in the wild writes that value under a type other than
/// <c>REG_SZ</c>, which decides whether a string cast is a safe way to read it.
/// A subset of the fallback's failure count rather than a term beside it.
/// </param>
/// <param name="UnreadablePatchStates">
/// Patches whose <c>State</c> or <c>Uninstallable</c> read failed. The file is
/// safe either way, both reads failing towards keeping it; what the number sizes
/// is a known wrong SENTENCE, an act-time re-verify reading the same failure as a
/// product having reclaimed the patch.
/// </param>
/// <param name="ProductCount">
/// Product rows the API enumeration returned. With
/// <see cref="PatchClaimCount"/> it gives the patch-to-product ratio, which is
/// the shape of a machine's cache and the thing the owner's own machine, at two
/// patches, is least like.
/// </param>
/// <param name="PatchClaimCount">
/// Product-to-patch claims read, one per claim rather than per patch: a patch
/// applied to three products counts three times, which is the figure that
/// describes the enumeration's work.
/// </param>
/// <param name="LongLeafStemCount">
/// Claimed paths whose leaf name has more than eight characters before the
/// extension, so the name cannot itself be an 8dot3 short name. Counted across
/// every claimed path, removable rows included, because the question is about
/// what the records hold and not about what this run is offering.
///
/// Nine of nine on one machine would say the cache is not named the way that
/// machine's other measurements assumed. The comparison it is for is against
/// <see cref="ScanResult.RegisteredPackages"/>'s own count in the same report,
/// which is why this is a count rather than the boolean the question was first
/// asked as.
/// </param>
/// <param name="RecoveredProductCount">
/// Products the registry named, the enumeration never returned, and a keyed ask
/// then found installed. THE TRUNCATION, MEASURED: every other signal about a
/// short enumeration on this list is an inference from two totals, and this one
/// is a count of products identified individually and confirmed one at a time.
///
/// Zero is the answer on a machine whose enumeration was whole, and it is also
/// the answer on a machine whose registry holds nothing but residue, so a
/// non-zero reading is the interesting one. It withholds nothing, the products
/// behind it having been asked rather than guessed at, which is why it is only
/// here and nowhere in the arithmetic.
/// </param>
/// <param name="UnresolvableProductCount">
/// Product codes the registry named that Windows would not say were installed or
/// not. The one state the comparison cannot resolve, and unlike the count above
/// it does withhold, because nothing about the enumeration's completeness follows
/// from a question that got no answer.
///
/// These two are what make the tolerance band answerable somewhere other than the
/// machine it was set on. The band guesses at the proportion of registry keys that
/// are residue; between them these two report what that proportion actually was,
/// per machine, without any band being involved.
/// </param>
public readonly record struct EnumerationCensus(
    int UnreadableProducts = 0,
    int SkippedProductRows = 0,
    int RegistryProductKeys = 0,
    int UnclaimedProductFiles = 0,
    int UnclaimedPatchFiles = 0,
    int NonStringLocalPackageValues = 0,
    int UnreadablePatchStates = 0,
    int ProductCount = 0,
    int PatchClaimCount = 0,
    int LongLeafStemCount = 0,
    int RecoveredProductCount = 0,
    int UnresolvableProductCount = 0);
