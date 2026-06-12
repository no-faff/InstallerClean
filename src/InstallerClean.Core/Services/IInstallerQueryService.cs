using InstallerClean.Models;

namespace InstallerClean.Services;

/// <summary>
/// Wraps the Windows Installer API (msi.dll) to enumerate every
/// registered product and patch across all install contexts. The
/// returned packages carry every <c>LocalPackage</c> path Windows
/// considers "still in use", along with metadata for the details
/// window.
/// </summary>
/// <remarks>
/// This service does not touch the filesystem. The on-disk
/// orphan-vs-registered cross-reference is performed by
/// <see cref="IFileSystemScanService"/>. Callers must be elevated:
/// <c>MsiEnumProductsExW</c> with the all-users SID returns
/// <c>ERROR_ACCESS_DENIED</c> for non-admin tokens.
/// </remarks>
public interface IInstallerQueryService
{
    /// <summary>
    /// Enumerate every registered product and patch. Returns one
    /// <see cref="RegisteredPackage"/> per <c>LocalPackage</c> path,
    /// with patch entries decorated with their state (applied,
    /// superseded, obsoleted) and an <c>IsRemovable</c> flag set when
    /// Windows itself has marked the patch unused.
    /// </summary>
    Task<IReadOnlyList<RegisteredPackage>> GetRegisteredPackagesAsync(
        IProgress<ScanProgressUpdate>? progress = null,
        CancellationToken cancellationToken = default);
}
