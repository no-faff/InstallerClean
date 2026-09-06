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
/// name. It carries no marking of its own and no cause. Two properties read
/// that emptiness and nothing else does: <see cref="HasNoNamedProduct"/>, which
/// keeps these rows at the foot of the list rather than the head of it, and
/// <see cref="ProductNameDisplay"/>, which is what the product cell and the
/// spoken line actually say.
/// </summary>
/// <param name="ProductName">
/// The product Windows names for the registration behind this row, or empty
/// where no registration names the file at all. Empty is a fact rather than a
/// gap, and it stays the empty string here rather than becoming the words the
/// cell shows: <see cref="HasNoNamedProduct"/> reads this field, so a
/// placeholder stored here would take the empty case away from it and the
/// ordering would revert with nothing failing to say so.
///
/// <c>Field.UnknownProductName</c> is a different fact and never stands in for
/// this one. It means a REGISTERED product whose display name did not come
/// back, and using it here would claim a product that was never established.
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
    /// What the product cell paints, and what the spoken line opens with. A row
    /// Windows names shows that name; a row it holds no record of shows
    /// <c>Field.NoNamedProduct</c>, because there is no program to name and an
    /// empty cell reads as something the app left unfilled.
    ///
    /// It is composed here rather than stored in <see cref="ProductName"/> so
    /// that <see cref="HasNoNamedProduct"/> goes on seeing the empty string and
    /// these rows keep their place at the foot of the list.
    ///
    /// <c>Field.UnknownProductName</c> passes straight through. A registration
    /// whose display name did not come back is a different fact from no
    /// registration at all, and each says its own thing.
    /// </summary>
    public string ProductNameDisplay =>
        ProductName.Length == 0 ? Strings.Field_NoNamedProduct : ProductName;

    /// <summary>
    /// What the patches cell paints, and the same value the spoken line uses.
    /// The count goes through <see cref="DisplayHelpers.FormatCount"/> so that a
    /// four-figure total carries the group separator the reader's region writes,
    /// as every other number the app shows already does.
    ///
    /// It is a composed string rather than the bare <see cref="PatchCount"/>
    /// because a bound integer renders through the default converter, under the
    /// binding's own culture rather than the one the rest of the window reads,
    /// and the cell and the spoken line would then punctuate the same figure
    /// differently. One property is what keeps them saying the same thing.
    /// </summary>
    public string PatchCountDisplay => DisplayHelpers.FormatCount(PatchCount);

    /// <summary>
    /// Spoken name for the row, composed from the visible cells. The list
    /// container binds it to AutomationProperties.Name; without that, UI
    /// Automation's item peer falls back to the record's generated
    /// ToString and a screen reader reads the whole member dump per row.
    /// A missing file says "missing" where the size would be, matching
    /// the Size column.
    ///
    /// It opens with <see cref="ProductNameDisplay"/> rather than the stored
    /// name, so that a row no registration names is spoken exactly as its cell
    /// reads and a listener is given what the screen gives.
    /// </summary>
    public string AccessibleName => string.Join(", ",
        ProductNameDisplay,
        FileName,
        IsMissing ? Strings.Field_Missing : SizeDisplay,
        $"{PatchCountDisplay} {DisplayHelpers.PluralisePatch(PatchCount)}");
}
