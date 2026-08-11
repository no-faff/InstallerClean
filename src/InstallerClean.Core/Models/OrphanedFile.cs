using System.IO;
using InstallerClean.Helpers;

namespace InstallerClean.Models;

/// <summary>
/// A single file at the root of <c>C:\Windows\Installer</c> that the scan is
/// offering for removal. ONE pathway adds entries here: a file no registration
/// names, which the identity pass then found nothing claiming.
///
/// It was three. Patches Windows reports superseded (PatchState 2) or obsoleted
/// (4) reached this list until 3.0.0, on a reading of those states that Microsoft
/// does not support (<see cref="RegisteredPackage"/> carries the citations). They
/// are registered files and are kept with the rest.
/// </summary>
/// <param name="FullPath">Absolute path inside <c>C:\Windows\Installer</c>.</param>
/// <param name="SizeBytes">File size on disk; 0 if the file disappeared between scan and stat.</param>
/// <param name="IsPatch">True for <c>.msp</c>, false for <c>.msi</c>. Drives the patch/installer column.</param>
/// <param name="IsRemovablePatch">
/// PERMANENTLY FALSE FROM 3.0.0: no registered patch is offered, whatever its
/// state. Kept so the result-log schema's <c>supersededCount</c> and
/// <c>obsoletedCount</c> go on being derived from the offer rather than being
/// hard-coded to zero somewhere a later change could quietly make wrong again.
/// </param>
/// <param name="IsObsoleted">
/// Permanently false for the same reason, and formerly true only for
/// PatchState=Obsoleted (4). It implies <see cref="IsRemovablePatch"/>; the
/// inverse does not hold.
/// </param>
/// <param name="Reason">
/// Localised tag shown in the Reason column of the orphan list. Now always
/// <c>Reason.Orphaned</c>, there being one pathway; callers pass a localised
/// value rather than relying on a default so a non-en-GB UI never shows a stray
/// English fragment.
/// </param>
public record OrphanedFile(
    string FullPath,
    long SizeBytes,
    bool IsPatch,
    bool IsRemovablePatch,
    bool IsObsoleted,
    string Reason)
{
    public string FileName => Path.GetFileName(FullPath);
    public string SizeDisplay => DisplayHelpers.FormatSize(SizeBytes);

    /// <summary>
    /// Spoken name for the row, composed from the visible cells (File,
    /// Reason, Size). The list container binds it to
    /// AutomationProperties.Name; without that, UI Automation's item peer
    /// falls back to the record's generated ToString and a screen reader
    /// reads the whole member dump per row.
    /// </summary>
    public string AccessibleName => string.Join(", ", FileName, Reason, SizeDisplay);
}
