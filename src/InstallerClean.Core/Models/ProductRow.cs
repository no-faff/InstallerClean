using InstallerClean.Helpers;
using InstallerClean.Resources;

namespace InstallerClean.Models;

/// <summary>
/// A row in the details window's file list. Groups an MSI package with its
/// patches so the UI can sort and display them as a single product.
///
/// IT ALSO CARRIES A ROW NO REGISTRATION NAMES: a file the scan declined to
/// offer sits in the same list, and the only thing that distinguishes it is
/// that <see cref="ProductName"/> is empty, because there is no product to
/// name. Nothing here treats such a row differently, which is the point of
/// merging the two lists.
/// </summary>
/// <param name="ProductName">
/// The product Windows names for the registration behind this row, or empty
/// where no registration names the file at all. Empty is a fact rather than a
/// gap, and no placeholder stands in for it: <c>Field.UnknownProductName</c>
/// means a REGISTERED product whose display name did not come back, which is a
/// different thing and would claim a product that was never established.
/// </param>
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
    ///
    /// EMPTY PARTS ARE DROPPED RATHER THAN JOINED, which matters for exactly
    /// one cell: a row no registration names has no product name, and a plain
    /// join would open the spoken line with a stray comma before the file
    /// name. The visible cell is blank in that case and the spoken line simply
    /// starts at the file name, which is the same information.
    /// </summary>
    public string AccessibleName => string.Join(", ", new[]
        {
            ProductName,
            FileName,
            IsMissing ? Strings.Field_Missing : SizeDisplay,
            $"{PatchCount} {DisplayHelpers.PluralisePatch(PatchCount)}",
        }.Where(part => !string.IsNullOrEmpty(part)));
}
