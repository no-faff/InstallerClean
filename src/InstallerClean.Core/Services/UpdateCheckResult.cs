namespace InstallerClean.Services;

/// <summary>
/// Outcome of an <see cref="IUpdateCheckService.CheckAsync"/> call.
/// Three discriminated states correspond to: the released version on
/// GitHub matches the running build, a newer release is available, or
/// the check could not be completed. Callers pattern-match on the record
/// subtype.
/// </summary>
public abstract record UpdateCheckResult;

/// <summary>The running build matches the latest release tag on GitHub.</summary>
public sealed record UpToDate(string CurrentVersion) : UpdateCheckResult;

/// <summary>
/// A newer release tag is published on GitHub. The two version strings
/// and nothing else: where a click goes is
/// <see cref="UpdateCheckService.ReleasesPageUrl"/>, fixed in the app, so
/// what GitHub answered has no say in it.
/// </summary>
public sealed record UpdateAvailable(string CurrentVersion, string LatestVersion)
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

    /// <summary>GitHub answered with a 4xx or 5xx status.</summary>
    ServerError,

    /// <summary>
    /// The answer carried no version the check will take: not a redirect at
    /// all, a redirect pointing outside the project's own releases path, or
    /// one whose tag does not parse as a version.
    /// </summary>
    ResponseParseError,

    /// <summary>The check timed out waiting on the network.</summary>
    Timeout,

    /// <summary>Catch-all for unanticipated exceptions in the check pipeline.</summary>
    Unknown,
}
