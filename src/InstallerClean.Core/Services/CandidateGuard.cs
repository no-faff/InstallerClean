namespace InstallerClean.Services;

/// <summary>
/// The single containment invariant for every file InstallerClean offers to
/// move or delete: it must resolve inside <c>C:\Windows\Installer</c> and must
/// not be a reparse point. The destination of a Move is guarded six ways; this
/// is the matching guard on the SOURCE, which the app enforced only as a side
/// effect of enumeration before. Orphan-walk candidates are in-bounds because
/// they came out of the folder, but the superseded and obsoleted candidates
/// come from an API or registry <c>LocalPackage</c> value that a corrupt
/// registration could point anywhere, so the guard is applied both where a
/// candidate is created and again per file at each action-service boundary.
/// </summary>
internal static class CandidateGuard
{
    /// <summary>
    /// True only if <paramref name="path"/> resolves inside the Installer cache
    /// folder and is not a symlink or junction. Both checks deliberately hit the
    /// REAL filesystem regardless of any injected <c>IFileSystem</c> (see
    /// <see cref="InstallerCacheHelpers.IsInstallerFolderOrChild"/> and
    /// <see cref="Helpers.StorageHelpers.IsReparsePoint"/>), so a test's
    /// MockFileSystem cannot make an out-of-bounds path look safe.
    /// </summary>
    /// <param name="installerFolderRoot">
    /// Test-only real-folder override for the cache root (null in production).
    /// See <see cref="InstallerCacheHelpers.IsInstallerFolderOrChild"/>.
    /// </param>
    internal static bool IsSafeToRemove(string path, string? installerFolderRoot = null) =>
        InstallerCacheHelpers.IsInstallerFolderOrChild(path, installerFolderRoot)
        && !Helpers.StorageHelpers.IsReparsePoint(path);
}
