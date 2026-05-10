using InstallerClean.Models;

namespace InstallerClean.Services;

/// <summary>
/// Writes the post-cleanup diagnostic log to disk and, on user
/// request, POSTs the same JSON to No Faff. The POST is strictly
/// click-triggered; nothing in this service fires on its own. The
/// pre-1.5.3 build's auto-HTTP-on-startup tripped DeepInstinct's
/// C2 heuristic on the elevated apphost, and the rule for any new
/// outbound call from an elevated process is one direct user action,
/// one network packet.
/// </summary>
public interface IResultLogService
{
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
    /// POSTs the contents of <see cref="LastLogPath"/> to the No Faff
    /// result-log endpoint. Returns one of <see cref="ResultLogSendOutcome"/>;
    /// the caller picks a localised message per case rather than the
    /// service echoing a framework exception. Never throws.
    /// </summary>
    Task<ResultLogSendOutcome> SendAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Reads <see cref="LastLogPath"/> as UTF-8 text and returns the
    /// raw content for display in the confirmation window. Never
    /// throws; returns null when the file doesn't exist, exceeds the
    /// 64 KiB read cap, or fails to read.
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
