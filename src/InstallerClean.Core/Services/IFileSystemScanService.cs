using InstallerClean.Models;

namespace InstallerClean.Services;

/// <summary>
/// Walks <c>C:\Windows\Installer</c>, asks the Windows Installer API
/// which packages are registered, and returns a <see cref="ScanResult"/>
/// describing what is safe to clean up.
/// </summary>
/// <remarks>
/// The scan is the only authoritative source of orphan-vs-registered
/// truth in the system. Other components (<see cref="IMoveFilesService"/>,
/// <see cref="IDeleteFilesService"/>) operate on file lists derived
/// from a <see cref="ScanResult"/> and never re-classify. Callers
/// should not cache results across user-driven mutations: run a fresh
/// scan after every Move or Delete.
/// </remarks>
public interface IFileSystemScanService
{
    /// <summary>
    /// Run the scan. Reports human-readable progress via
    /// <paramref name="progress"/> (status text only, not percentages):
    /// milestone updates carry the fixed phase messages, non-milestone
    /// updates the per-product ticker (see <see cref="ScanProgressUpdate"/>).
    /// Throws <see cref="UnauthorizedAccessException"/> if the process
    /// cannot read the MSI database (typically: not elevated), or
    /// <see cref="InvalidOperationException"/> if Windows Installer
    /// returns no registered products at all (an empty database is
    /// usually a sign of a deeper Windows-side problem, surfaced as
    /// an exception rather than silently flagging every cached file
    /// as orphaned).
    /// </summary>
    Task<ScanResult> ScanAsync(
        IProgress<ScanProgressUpdate>? progress = null,
        CancellationToken cancellationToken = default);
}
