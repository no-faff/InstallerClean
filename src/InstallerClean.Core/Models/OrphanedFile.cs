using System.IO;
using InstallerClean.Helpers;

namespace InstallerClean.Models;

/// <summary>
/// A single file in <c>C:\Windows\Installer</c> that the scan classified
/// as removable. Three pathways add entries here: files the API never
/// claimed (true orphans, PatchState absent), patches the API still
/// claims but has marked PatchState=Superseded (2), and patches marked
/// PatchState=Obsoleted (4). The user-visible outcome (removable) is the
/// same for all three; the distinction is recorded so the result-log
/// schema and the in-app body copy can describe each cause separately.
/// </summary>
/// <param name="FullPath">Absolute path inside <c>C:\Windows\Installer</c>.</param>
/// <param name="SizeBytes">File size on disk; 0 if the file disappeared between scan and stat.</param>
/// <param name="IsPatch">True for <c>.msp</c>, false for <c>.msi</c>. Drives the patch/installer column.</param>
/// <param name="IsRemovablePatch">
/// True for entries added because the Windows Installer database marks the
/// patch removable (PatchState=Superseded or =Obsoleted), false for true
/// orphans the API never claimed. Renamed from <c>IsSuperseded</c> in
/// v1.8.2; the old name was a misnomer once the obsoleted distinction
/// was added because PatchState=4 entries also set the flag.
/// </param>
/// <param name="IsObsoleted">
/// True only for entries with PatchState=Obsoleted (4); false for both
/// PatchState=Superseded (2) and true orphans. Used by the result-log
/// schema to split <c>supersededCount</c> and <c>obsoletedCount</c>.
/// Implies <see cref="IsRemovablePatch"/>; the inverse does not hold.
/// </param>
/// <param name="Reason">
/// Localised tag shown in the Reason column of the orphan list. Sourced
/// from the resx (<c>Reason.Orphaned</c>, <c>Reason.Superseded</c> or
/// <c>Reason.Obsoleted</c>); callers pass a localised value rather than
/// relying on a default so a non-en-GB UI never shows a stray English
/// fragment. Display only; the machine-readable signals are
/// <see cref="IsRemovablePatch"/> and <see cref="IsObsoleted"/>.
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
}
