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
/// True where this offered file is a registered patch rather than a file no
/// registration names: Windows reported it superseded, it declared itself
/// non-removable, and every product it is registered under was established to hold
/// no patch that could be uninstalled and roll back onto it. The result-log schema's
/// <c>supersededCount</c> is derived from it.
/// </param>
/// <param name="IsObsoleted">
/// PERMANENTLY FALSE, AND FOR A REASON RATHER THAN AS A PLACEHOLDER. It was true only
/// for PatchState=Obsoleted (4), and an obsoleted patch is not offered at all from
/// 3.0.0: never observed on any machine in any report, so offering it reclaims
/// nothing, and never manufactured to test with, so it would put an unexercised class
/// on a list whose whole claim is certainty. The field is kept, and the schema's
/// <c>obsoletedCount</c> goes on being DERIVED from it rather than hard-coded to zero,
/// because the derivation is what would notice if one ever reached the offer again.
/// Whether any machine has one is answered instead by the scan-time registration count
/// on <see cref="ScanResult.ObsoletedRegistrationCount"/>, taken off the machine.
///
/// It still implies <see cref="IsRemovablePatch"/> by construction; the inverse does
/// not hold, and now never will.
/// </param>
/// <param name="Reason">
/// Localised tag shown in the Reason column of the orphan list, and there are two
/// again: <c>Reason.Orphaned</c> for a file no registration names, and
/// <c>Reason.Superseded</c> for a registered patch that passed the per-product
/// condition. Callers pass a localised value rather than relying on a default, so a
/// non-en-GB UI never shows a stray English fragment.
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
