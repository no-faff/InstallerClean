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
/// FOUR contributors, and only the first two are failures to read. A product row
/// the API skipped (identity unknowable, counted one per row); a product whose
/// rows came back but whose LocalPackage value, or one of whose patch rows, would
/// not read. Then two that are absences rather than failures: a shortfall of the
/// API's product headcount against the registry's own, and a cached file the
/// registry claims and the API never mentioned. Both of those are computed NET of
/// the read failures (see the subtractions at the assembly site), so by
/// construction they count only products where nothing failed to read at all.
/// A product meeting more than one contributor counts once.
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
/// to a clean end. One per product however many of the three it met. The only
/// term of the three that is a failure to read.
/// </param>
/// <param name="ShortfallProducts">
/// How far the API's product headcount fell short of the registry's own, net of
/// the read failures already counted above, and zero unless the shortfall clears
/// the tolerance band. An inference from two counts that can differ innocently,
/// never a measurement.
/// </param>
/// <param name="ApiNeverClaimed">
/// Products inferred from cached files the registry claims, whose file is really
/// on the disk, and which the API's own loop never named. An observation rather
/// than an inference, and the one the headcount's tolerance band cannot make.
///
/// SEPARATE FROM <see cref="ShortfallProducts"/> BECAUSE THEY ESTIMATE THE SAME
/// QUANTITY FROM OPPOSITE SIDES and the assembly site combines them by the larger
/// of the two rather than by addition. Adding them here would be the same double
/// count in a different place.
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
public readonly record struct EnumerationCensus(
    int UnreadableProducts = 0,
    int ShortfallProducts = 0,
    int ApiNeverClaimed = 0,
    int NonStringLocalPackageValues = 0,
    int UnreadablePatchStates = 0,
    int ProductCount = 0,
    int PatchClaimCount = 0,
    int LongLeafStemCount = 0);
