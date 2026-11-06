using InstallerClean.Models;

namespace InstallerClean.Services;

/// <summary>
/// Writes the report of the PC's first finished run to disk and POSTs
/// the same JSON to No Faff. The window sends it in two places: as that
/// run's card closes with its report box ticked, and at a later start
/// where this account's report is still waiting to go
/// (<see cref="Models.AppSettings.ReportSendPending"/>). The command line sends it
/// where its question is answered yes (<see cref="ConsoleReportOffer"/>), and at a
/// later run at a console where the report is still waiting. Nothing in this
/// service starts a call on its own.
/// </summary>
public interface IResultLogService
{
    /// <summary>
    /// Serialises <paramref name="entry"/> to JSON and replaces the
    /// previous <c>last-run.json</c> atomically. Never throws; a disk-
    /// full / locked-file / read-only profile situation logs the
    /// failure to crash.log and returns false.
    /// </summary>
    Task<bool> WriteAsync(ResultLogEntry entry, CancellationToken cancellationToken = default);

    /// <summary>
    /// POSTs <paramref name="body"/> to the No Faff result-log endpoint.
    /// The caller reads the body with <see cref="ReadLastLogAsync"/>, so
    /// what goes is what <c>last-run.json</c> holds. Returns one of
    /// <see cref="ResultLogSendOutcome"/>. Never throws for a network,
    /// server or IO failure; a token cancelled by the caller surfaces as
    /// OperationCanceledException, so a caller that passes one must catch it.
    /// </summary>
    Task<ResultLogSendOutcome> SendAsync(string body, CancellationToken cancellationToken = default);

    /// <summary>
    /// Reads <c>last-run.json</c> as UTF-8 text and returns the
    /// raw content, which is the body <see cref="SendAsync"/> sends. Returns null
    /// when the file doesn't exist, exceeds the <see cref="ResultLogService.MaxLogBytes"/>
    /// cap, or fails to read; oversize and read-failure cases write a
    /// breadcrumb to crash.log. Never throws for an IO failure; a token
    /// cancelled by the caller surfaces as OperationCanceledException, so
    /// a caller that passes one must catch it.
    /// </summary>
    Task<string?> ReadLastLogAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Whether <c>last-run.json</c> is there, answered from the folder without opening
    /// the file. False where the folder cannot be read. Never throws.
    /// </summary>
    bool LastLogExists();
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
