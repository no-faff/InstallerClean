using InstallerClean.Models;

namespace InstallerClean.Services;

/// <summary>
/// Writes the post-cleanup diagnostic log to disk and, on user
/// request, POSTs the same JSON to No Faff. Each outbound call from
/// the elevated process is bound to one direct user action; nothing
/// in this service fires on its own. (DeepInstinct's auto-HTTP-from-
/// elevated-startup heuristic is the incident the constraint defends
/// against.)
/// </summary>
public interface IResultLogService
{
    /// <summary>
    /// Maximum size of <c>last-run.json</c> the service will read or
    /// POST. The writer caps the JSON at this size by construction
    /// (the schema's natural size is well under it); a file larger
    /// than this came from outside the process and is rejected.
    /// </summary>
    public const long MaxLogBytes = 64 * 1024;

    /// <summary>Absolute path to <c>last-run.json</c> on the local profile.</summary>
    string LastLogPath { get; }

    /// <summary>True when a fresh log file exists on disk.</summary>
    bool HasFreshLog { get; }

    /// <summary>
    /// Serialises <paramref name="entry"/> to JSON and replaces the
    /// previous <c>last-run.json</c> atomically. Never throws; a disk-
    /// full / locked-file / read-only profile situation logs the
    /// failure to crash.log and returns false.
    /// </summary>
    Task<bool> WriteAsync(ResultLogEntry entry, CancellationToken cancellationToken = default);

    /// <summary>
    /// POSTs <paramref name="body"/> to the No Faff result-log endpoint.
    /// The caller is expected to obtain the body via
    /// <see cref="ReadLastLogAsync"/> immediately before calling so the
    /// modal preview and the wire payload are the same bytes; reading
    /// last-run.json twice would open a TOCTOU window between user
    /// review and POST. Returns one of <see cref="ResultLogSendOutcome"/>;
    /// the caller picks a localised message per case rather than the
    /// service echoing a framework exception. Never throws.
    /// </summary>
    Task<ResultLogSendOutcome> SendAsync(string body, CancellationToken cancellationToken = default);

    /// <summary>
    /// Reads <see cref="LastLogPath"/> as UTF-8 text and returns the
    /// raw content for display in the confirmation window. Never
    /// throws; returns null when the file doesn't exist, exceeds the
    /// <see cref="MaxLogBytes"/> cap, or fails to read. Oversize and
    /// read-failure cases write a breadcrumb to crash.log.
    /// </summary>
    Task<string?> ReadLastLogAsync(CancellationToken cancellationToken = default);
}

public enum ResultLogSendOutcome
{
    Sent,
    NoLogToSend,
    NetworkUnavailable,
    Timeout,
    ServerError,
    Unknown,
}
