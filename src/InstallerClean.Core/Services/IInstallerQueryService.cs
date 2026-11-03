using InstallerClean.Models;

namespace InstallerClean.Services;

/// <summary>
/// Wraps the Windows Installer API (msi.dll) to enumerate the products and
/// patches it lists across all install contexts, with the registry's own
/// records of them read beside it. The returned packages carry every <c>LocalPackage</c> path Windows
/// considers "still in use", along with metadata for the details
/// window.
/// </summary>
/// <remarks>
/// This service asks the filesystem exactly one question, whether a cached
/// file only the registry fallback claimed is really on disk, because a
/// claim the product enumeration never made is evidence of a lost
/// registration only if the file it names is there to be lost. Nothing else
/// here reads the disk, and the on-disk orphan-vs-registered
/// cross-reference belongs to <see cref="IFileSystemScanService"/>.
/// Callers must be elevated:
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
    /// <remarks>
    /// The result also reports whether the enumeration read every product it lists. Where
    /// it did not, no row comes back removable: a verdict that no product still needs a
    /// file cannot be drawn from a product set short of a member. A product key the walks
    /// do not list is read off the registry instead, and a superseded patch its listing
    /// names comes back withheld, as does every one where such a listing or any read of
    /// those records did not settle (<see cref="InstallerQueryResult.UnlistedProductKeys"/>).
    /// Every consumer of a removable verdict therefore inherits the withholding by
    /// consuming this method, including the act-time re-verify, which is the same call.
    /// </remarks>
    Task<InstallerQueryResult> GetRegisteredPackagesAsync(
        IProgress<ScanProgressUpdate>? progress = null,
        CancellationToken cancellationToken = default);
}
