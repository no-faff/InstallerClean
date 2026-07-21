namespace InstallerClean.Services;

/// <summary>
/// The single containment invariant for every file InstallerClean offers to
/// move or delete: it must resolve to a file sitting DIRECTLY in
/// <c>C:\Windows\Installer</c>, and must not be a reparse point. The
/// destination of a Move is guarded six ways; this is the matching guard on the
/// SOURCE, which enumeration alone would enforce only as a side effect.
/// Orphan-walk candidates are in-bounds because they came out of the folder,
/// but the superseded and obsoleted candidates come from an API or registry
/// <c>LocalPackage</c> value that a corrupt registration could point anywhere,
/// so the guard is applied both where a candidate is created and again per file
/// at each action-service boundary.
///
/// Directly, not merely inside: the walk never descends, and a registered
/// <c>LocalPackage</c> value names a file at the root, so no legitimate
/// candidate is a descendant. The descendant form let in exactly one thing, a
/// corrupt registration naming a file under <c>$PatchCache$</c> (the patch
/// engine's baseline payload copies, a subtree the scan deliberately puts out
/// of scope by never descending). SECURITY.md states the narrow promise to
/// anyone deciding whether they have found a bug worth reporting, so the code
/// has to be what makes it true.
/// </summary>
internal static class CandidateGuard
{
    /// <summary>
    /// What <see cref="CheckSafeToRemove"/> established about a path.
    /// </summary>
    internal enum RemovalSafety
    {
        /// <summary>Resolves to a file directly in the cache folder and is not a reparse point.</summary>
        Safe,

        /// <summary>
        /// Established as not qualifying: it resolves outside the cache folder
        /// or into one of its subfolders, or its attributes carry the
        /// reparse-point bit.
        /// </summary>
        Refused,

        /// <summary>
        /// Neither was established: the path could not be resolved, or its
        /// attributes could not be read. The file is kept, and a caller that
        /// reports the refusal must not name a cause it has not shown.
        /// </summary>
        Unproven,
    }

    /// <summary>
    /// Whether <paramref name="path"/> qualifies for removal, with an
    /// unestablished answer kept distinct from a refusal. Both checks
    /// deliberately hit the REAL filesystem regardless of any injected
    /// <c>IFileSystem</c> (see
    /// <see cref="InstallerCacheHelpers.TryResolveFinalPath"/> and
    /// <see cref="Helpers.StorageHelpers.CheckReparsePoint"/>), so a test's
    /// MockFileSystem cannot make an out-of-bounds path look safe.
    ///
    /// Unproven exists because the two kinds of caller want different words for
    /// it. At candidate creation it simply keeps the file off the list, and a
    /// transient read failure self-heals on the next scan. At the action
    /// services it decides which per-file error the user is shown, and the ones
    /// naming a cause ("this file is a symlink", "this file is outside the
    /// cache") would each be asserting something no check here demonstrated.
    /// </summary>
    /// <param name="installerFolderRoot">
    /// Test-only real-folder override for the cache root (null in production).
    /// See <see cref="InstallerCacheHelpers.IsInstallerFolderOrChild"/>.
    /// </param>
    internal static RemovalSafety CheckSafeToRemove(string path, string? installerFolderRoot = null)
    {
        if (string.IsNullOrWhiteSpace(path)) return RemovalSafety.Refused;

        var reparse = Helpers.StorageHelpers.CheckReparsePoint(path, out _);
        if (reparse == Helpers.StorageHelpers.ReparseCheck.Yes) return RemovalSafety.Refused;
        if (reparse == Helpers.StorageHelpers.ReparseCheck.Unreadable) return RemovalSafety.Unproven;

        // A path the kernel never expanded is compared on the strength of how
        // its name is spelled, which is what a junction defeats, so an
        // unresolved path is Unproven rather than measured.
        if (!InstallerCacheHelpers.TryResolveFinalPath(path, out var resolved))
            return RemovalSafety.Unproven;

        return InstallerCacheHelpers.ResolvesDirectlyInInstallerFolder(resolved, installerFolderRoot)
            ? RemovalSafety.Safe
            : RemovalSafety.Refused;
    }

}
