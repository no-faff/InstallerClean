namespace InstallerClean.Models;

/// <summary>
/// A patch file entry inside a <see cref="ProductRow"/>'s patch list.
/// </summary>
public sealed record PatchRow(
    string FileName,
    string FullPath,
    string SizeDisplay)
{
    /// <summary>
    /// Spoken name for the row, composed from the visible lines; the
    /// list container binds it to AutomationProperties.Name so the item
    /// peer does not fall back to the record's generated ToString dump.
    /// </summary>
    public string AccessibleName => $"{FileName}, {SizeDisplay}";
}
