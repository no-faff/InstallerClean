using InstallerClean.Helpers;
using InstallerClean.Resources;

namespace InstallerClean.Models;

/// <summary>
/// A row in the Registered Files details grid. Groups an MSI package with
/// its patches so the UI can sort and display them as a single product.
/// </summary>
public sealed record ProductRow(
    string ProductName,
    string FileName,
    string FullPath,
    string SizeDisplay,
    long SizeBytes,
    int PatchCount,
    IReadOnlyList<PatchRow> Patches,
    // True when the product's representative file is still registered with
    // Windows Installer but absent from disk. Its summary metadata is read
    // from the file, so it is unavailable for a missing row.
    bool IsMissing = false)
{
    /// <summary>
    /// Spoken name for the row, composed from the visible cells. The list
    /// container binds it to AutomationProperties.Name; without that, UI
    /// Automation's item peer falls back to the record's generated
    /// ToString and a screen reader reads the whole member dump per row.
    /// A missing file says "missing" where the size would be, matching
    /// the Size column.
    /// </summary>
    public string AccessibleName => string.Join(", ",
        ProductName,
        FileName,
        IsMissing ? Strings.Field_Missing : SizeDisplay,
        $"{PatchCount} {DisplayHelpers.PluralisePatch(PatchCount)}");
}
