namespace InstallerClean.Services;

/// <summary>
/// Single-call check against the GitHub Releases API. Triggered only
/// by an explicit user action (the "Check for updates" button in the
/// About window), never automatically. No outbound network capability
/// fires without a deliberate click.
/// </summary>
public interface IUpdateCheckService
{
    /// <summary>
    /// Issues a single HTTPS GET against the GitHub Releases API for
    /// the project's "latest" release, parses the tag, and compares it
    /// to the running assembly version. Returns one of three
    /// <see cref="UpdateCheckResult"/> subtypes. Never throws for a
    /// network, server or timeout failure, each of which comes back as a
    /// CheckFailed reason; a token cancelled by the caller surfaces as
    /// OperationCanceledException, deliberately, so the call site can tell
    /// a user-cancelled check from a server-cancelled one, and a caller
    /// that passes a token must catch it.
    /// </summary>
    Task<UpdateCheckResult> CheckAsync(CancellationToken cancellationToken = default);
}
