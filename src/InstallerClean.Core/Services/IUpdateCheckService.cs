namespace InstallerClean.Services;

/// <summary>
/// Single-call check against the GitHub Releases API. Triggered only
/// by an explicit user action (the "Check for updates" button in the
/// About window), never automatically. No outbound network capability
/// fires without a deliberate click. (DeepInstinct heuristic on
/// auto-HTTP-from-elevated-startup.)
/// </summary>
public interface IUpdateCheckService
{
    /// <summary>
    /// Issues a single HTTPS GET against the GitHub Releases API for
    /// the project's "latest" release, parses the tag, and compares it
    /// to the running assembly version. Returns one of three
    /// <see cref="UpdateCheckResult"/> subtypes; never throws.
    /// </summary>
    Task<UpdateCheckResult> CheckAsync(CancellationToken cancellationToken = default);
}
