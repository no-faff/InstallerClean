namespace InstallerClean.Models;

/// <summary>
/// The output of one <c>InstallerQueryService</c> enumeration: every
/// <c>LocalPackage</c> path the Windows Installer API and the registry fallback
/// between them claim, plus how much of the enumeration failed to read.
/// </summary>
/// <param name="Packages">
/// One row per claimed path. On a run where
/// <see cref="UnreadableProductCount"/> is non-zero, no row carries
/// <see cref="RegisteredPackage.IsRemovable"/>: the removable class is withheld
/// wholesale (see <see cref="RecordsIncomplete"/>) and the rows that would have
/// carried it are marked <see cref="RegisteredPackage.RemovableWithheld"/>
/// instead.
/// </param>
/// <param name="UnreadableProductCount">
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
/// THE NAME IS NARROWER THAN THE NUMBER, which is worth knowing before quoting
/// either: this counts what the enumeration could not account for, by any of those
/// four routes, and "unreadable" is true of half of them. Any sentence built on it
/// is a sentence about all four. The wording that stood here named the three read
/// failures and stopped, and a sweep for messages stating a cause for a mixed set
/// checked a cause-stating string against this comment, found it consistent, and
/// nearly filed it clean; the string is false of every member the other two
/// contributors put in. A superordinate is checked against the code that BUILDS
/// the set, never against the comment describing it.
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
public record InstallerQueryResult(
    IReadOnlyList<RegisteredPackage> Packages,
    int UnreadableProductCount = 0,
    IReadOnlyList<PatchClaim>? PatchClaims = null)
{
    /// <summary>Never null: an absent list reads as no claims rather than as a fault.</summary>
    public IReadOnlyList<PatchClaim> PatchClaims { get; init; } = PatchClaims ?? Array.Empty<PatchClaim>();

    /// <summary>
    /// The enumeration did not account for at least one installed product, so it
    /// cannot say of any patch that no installed product still needs it. Whether
    /// that product's records failed to read or were never reached does not enter
    /// into it: what withholds the verdict is the missing claim, not the mechanism
    /// (see <see cref="UnreadableProductCount"/> for the four). Every consumer of a
    /// removable verdict inherits this through the withheld rows themselves; it is
    /// exposed for the copy that explains the shorter list.
    /// </summary>
    public bool RecordsIncomplete => UnreadableProductCount > 0;
}
