using InstallerClean.Resources;

namespace InstallerClean.Models;

/// <summary>
/// A patch file entry inside a <see cref="ProductRow"/>'s patch list.
/// </summary>
/// <param name="IsMissing">
/// Windows holds a record naming this patch and the file is not in the folder.
///
/// IT IS CARRIED BECAUSE THE ROW OTHERWISE STATES A SIZE FOR A FILE THAT IS NOT
/// THERE. A missing row's byte count is zero, and zero renders as an ordinary
/// size, so the list showed a plausible nought bytes and the spoken line said the
/// same. Neither was a gap a reader could notice.
///
/// AND THE PRODUCT ROW CANNOT ANSWER FOR IT, which is why the flag belongs here
/// rather than being rolled upwards. A product row takes its own missing state
/// from its own cached package, so a product whose package is present and whose
/// patch has gone is correctly not missing. Marking that row would put "missing"
/// where its own size goes, which is false of it.
/// </param>
public sealed record PatchRow(
    string FileName,
    string FullPath,
    string SizeDisplay,
    bool IsMissing = false)
{
    /// <summary>
    /// Spoken name for the row, composed from the visible lines; the
    /// list container binds it to AutomationProperties.Name so the item
    /// peer does not fall back to the record's generated ToString dump.
    ///
    /// A missing file says "missing" where the size would be, matching what the
    /// list paints and matching <see cref="ProductRow.AccessibleName"/> next door.
    /// </summary>
    public string AccessibleName =>
        $"{FileName}, {(IsMissing ? Strings.Field_Missing : SizeDisplay)}";
}
