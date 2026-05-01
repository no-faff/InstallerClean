using System.IO;
using InstallerClean.Helpers;

namespace InstallerClean.Models;

/// <summary>
/// A single file in <c>C:\Windows\Installer</c> that the scan classified
/// as removable. Two pathways add entries here: files the API never
/// claimed (true orphans) and patches the API still claims but has
/// marked superseded or obsoleted (carried via <see cref="Reason"/>).
/// </summary>
/// <param name="FullPath">Absolute path inside <c>C:\Windows\Installer</c>.</param>
/// <param name="SizeBytes">File size on disk; 0 if the file disappeared between scan and stat.</param>
/// <param name="IsPatch">True for <c>.msp</c>, false for <c>.msi</c>. Drives the patch/installer column.</param>
/// <param name="Reason">Short tag shown in the orphan list ("Orphaned" or "Superseded").</param>
public record OrphanedFile(
    string FullPath,
    long SizeBytes,
    bool IsPatch,
    string Reason = "Orphaned")
{
    public string FileName => Path.GetFileName(FullPath);
    public string SizeDisplay => DisplayHelpers.FormatSize(SizeBytes);
}
