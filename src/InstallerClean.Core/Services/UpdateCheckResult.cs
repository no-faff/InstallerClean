namespace InstallerClean.Services;

/// <summary>
/// Outcome of an <see cref="IUpdateCheckService.CheckAsync"/> call.
/// Three discriminated states correspond to: the released version on
/// GitHub matches the running build, a newer release is available, or
/// the check could not be completed (network, parse, or rate-limit
/// failure). Callers pattern-match on the record subtype.
/// </summary>
public abstract record UpdateCheckResult;

/// <summary>The running build matches the latest release tag on GitHub.</summary>
public sealed record UpToDate(string CurrentVersion) : UpdateCheckResult;

/// <summary>
/// A newer release tag is published on GitHub. <see cref="ReleaseUrl"/>
/// is the html_url from the GitHub API and lands on the release page
/// for the new version, with the binaries attached.
/// </summary>
public sealed record UpdateAvailable(string CurrentVersion, string LatestVersion, string ReleaseUrl)
    : UpdateCheckResult;

/// <summary>
/// The check failed before a comparison could be made. The
/// <see cref="ReasonCode"/> categorises the failure for the UI to
/// localise; the underlying exception (if any) goes to crash.log
/// rather than into the displayed message.
/// </summary>
public sealed record CheckFailed(UpdateCheckFailureReason ReasonCode) : UpdateCheckResult;

/// <summary>
/// Reason a check could not complete. The discriminated set lets the
/// UI pick a localised resx string per case rather than echoing a
/// framework exception message.
/// </summary>
public enum UpdateCheckFailureReason
{
    /// <summary>HTTP send failed: DNS, TLS, no network, etc.</summary>
    NetworkUnavailable,

    /// <summary>GitHub returned a 4xx or 5xx response. Includes 403 rate-limit.</summary>
    ServerError,

    /// <summary>Response body was missing or did not parse as the expected schema.</summary>
    ResponseParseError,

    /// <summary>The check timed out waiting on the network.</summary>
    Timeout,

    /// <summary>Catch-all for unanticipated exceptions in the check pipeline.</summary>
    Unknown,
}
