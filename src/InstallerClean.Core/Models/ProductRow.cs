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
/// name. It carries no marking of its own and no cause: the one place the
/// distinction is read is <see cref="HasNoNamedProduct"/>, which keeps these
/// rows at the foot of the list rather than the head of it.
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
    /// Orders a row carrying no established product name below every row that
    /// has one. Two values reach it: an empty name, where no registration names
    /// the file at all, and <c>Field.UnknownProductName</c>, where a
    /// registration exists and its display name did not come back. They are
    /// different facts and they sit together here for one reason, that neither
    /// gives the reader a name to scan for.
    ///
    /// The list sorts on this before the product name itself and always
    /// ascending, so these rows stay at the foot whichever way the header
    /// points. Sorting on any other column ignores it.
    /// </summary>
    public bool HasNoNamedProduct =>
        ProductName.Length == 0 || ProductName == Strings.Field_UnknownProductName;

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
