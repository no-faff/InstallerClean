using InstallerClean.Models;

namespace InstallerClean.Services;

/// <summary>
/// Reads the MSI / MSP summary-information stream for a single file
/// to populate the metadata block in the details windows. Decorative
/// only; failure to read returns null and the UI shows a graceful
/// "no metadata available" placeholder.
/// </summary>
public interface IMsiFileInfoService
{
    /// <summary>
    /// Open <paramref name="filePath"/>, read its summary information,
    /// and return a <see cref="MsiSummaryInfo"/>. Returns null if the
    /// file is missing, locked, or not a valid MSI / MSP. Never
    /// throws.
    /// </summary>
    MsiSummaryInfo? GetSummaryInfo(string filePath);
}
