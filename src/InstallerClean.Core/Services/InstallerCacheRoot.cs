namespace InstallerClean.Services;

/// <summary>
/// The Installer cache root's final path, resolved once and carried for one run
/// of a scan, a Move or a Delete. <see cref="CandidateGuard.CheckSafeToRemove"/>
/// takes one of these rather than a path string so the containment comparison
/// cannot be handed a root the kernel never expanded: measured against a
/// candidate that WAS expanded, an unexpanded root compares how two names are
/// spelled, which is precisely what a junction defeats.
///
/// Once per run, not once per candidate, because the guard was re-asking the
/// same question about the same folder for every file in it. That was half the
/// scan's per-file kernel cost, a handle open, a final-path query and a close
/// each time, and the folder runs to 800,000 files on the machines this app is
/// reported against.
///
/// Freezing a value a safety gate compares against only holds if it fails the
/// safe way, so the case is stated rather than assumed. The candidate's OWN
/// resolution is untouched and still goes through the kernel per file; nothing
/// here needs the root to be immutable. Should its resolution change mid-run,
/// the frozen value stops matching what a candidate resolves to, and a mismatch
/// is a refusal: fewer files offered, never more. The one interleaving that
/// lands the other way is a root that resolved to X when the run started and a
/// candidate still resolving to X\file, which is a file sitting directly in the
/// folder the run was launched against, and that is what the guard exists to
/// permit. A root whose own resolution degrades is compared in its best-effort
/// spelling, exactly as it was when the comparison re-ran per candidate.
/// </summary>
internal sealed class InstallerCacheRoot
{
    private InstallerCacheRoot(string resolved) => Resolved = resolved;

    /// <summary>
    /// The resolved root with any trailing separator removed, so a caller
    /// comparing a candidate's parent directory can compare it directly.
    /// </summary>
    internal string Resolved { get; }

    /// <summary>
    /// Resolves the root against the real filesystem. Call it once, immediately
    /// before the loop that will judge candidates against it.
    /// </summary>
    /// <param name="installerFolderRoot">
    /// Test-only real-folder override (null in production, which uses the real
    /// <see cref="InstallerCacheHelpers.InstallerFolder"/>). The resolution runs
    /// against the REAL filesystem either way, so this only relocates the check
    /// to a sandbox directory for the integration tests; it does NOT let a
    /// MockFileSystem bypass the gate.
    /// </param>
    internal static InstallerCacheRoot Resolve(string? installerFolderRoot = null) =>
        new(InstallerCacheHelpers
            .ResolveFinalPath(installerFolderRoot ?? InstallerCacheHelpers.InstallerFolder)
            .TrimEnd(Path.DirectorySeparatorChar));
}
