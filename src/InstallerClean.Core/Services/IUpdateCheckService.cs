namespace InstallerClean.Services;

/// <summary>
/// Single-call check against the GitHub Releases API. Two call sites,
/// both in ChromeViewModel: the "Check for updates" button on the main
/// window, and the once-per-session automatic check the app starts at
/// launch. The automatic one is gated on <c>AppSettings.AutoUpdateCheck</c>,
/// read at the moment it fires, so an install that has opted out opens no
/// socket at all. This is the app's only outbound call besides the
/// explicitly consented result-log send.
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
    /// <param name="origin">
    /// Which call site is asking. It changes nothing about the request or
    /// the result, only how much of a failure reaches crash.log: a machine
    /// with no route to api.github.com is a normal machine, and the
    /// automatic check meets one at every launch.
    /// </param>
    Task<UpdateCheckResult> CheckAsync(
        UpdateCheckOrigin origin, CancellationToken cancellationToken = default);
}
