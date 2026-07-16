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
/// Installed products whose records this enumeration could not fully read: a
/// product row the API skipped (identity unknowable, counted one per row), or a
/// product whose own row read cleanly but whose patch enumeration skipped a row.
/// Both leave the same hole, a product whose claim on a shared patch never
/// reached the merge, so both count here. Surfaced to the user as the scan
/// summary's kept-patches notice.
/// </param>
public record InstallerQueryResult(
    IReadOnlyList<RegisteredPackage> Packages,
    int UnreadableProductCount = 0)
{
    /// <summary>
    /// The enumeration lost at least one product's records, so it cannot say of
    /// any patch that no installed product still needs it. Every consumer of a
    /// removable verdict inherits this through the withheld rows themselves; it
    /// is exposed for the copy that explains the shorter list.
    /// </summary>
    public bool RecordsIncomplete => UnreadableProductCount > 0;
}
