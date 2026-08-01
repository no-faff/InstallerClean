using System.Net.Http;
using System.Net.Http.Headers;
using System.Reflection;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using InstallerClean.Helpers;
using InstallerClean.Models;

namespace InstallerClean.Services;

/// <summary>
/// Default <see cref="IResultLogService"/>. Writes the JSON to
/// <c>%LOCALAPPDATA%\NoFaff\InstallerClean\last-run.json</c> via the
/// same atomic-rename pattern <see cref="SettingsService"/> uses, and
/// POSTs the file's contents to the No Faff result-log endpoint on
/// user click.
/// </summary>
public sealed class ResultLogService : IResultLogService
{
    private const string EndpointUrl = "https://nofaff.netlify.app/api/result-log";

    private static readonly TimeSpan RequestTimeout = TimeSpan.FromSeconds(8);

    private static readonly string DefaultLogFile = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "NoFaff", "InstallerClean", "last-run.json");

    private readonly string _logFile;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.Never,
    };

    private static readonly string UserAgent =
        $"InstallerClean/{Assembly.GetExecutingAssembly().GetName().Version?.ToString(3) ?? "0.0.0"}";

    private static readonly HttpClient HttpClient = new()
    {
        Timeout = RequestTimeout,
        // Bounds the eager buffering only: MaxResponseContentBufferSize
        // gates HttpCompletionOption.ResponseContentRead, which is the
        // default, so a send that stops passing the explicit option
        // buffers into 4 KiB rather than HttpClient's own 2 GiB. It does
        // not reach a ReadAsStringAsync taken after the ResponseHeadersRead
        // send in SendAsync (measured on .NET 10, a 400 KiB body read back
        // whole under a 256 KiB cap); what keeps the ack out of this process
        // is that nothing reads it. 4 KiB is generous for the expected
        // {"ok":true} ack.
        MaxResponseContentBufferSize = 4 * 1024,
    };

    public ResultLogService() : this(DefaultLogFile) { }

    /// <summary>
    /// Test seam, mirroring <see cref="SettingsService"/>'s next door: points
    /// the whole write-read cycle at a sandbox path so the byte cap, the
    /// atomic write and the temp-file cleanup can be driven without a test run
    /// writing over the real last-run.json in the running user's profile.
    /// </summary>
    internal ResultLogService(string logFile)
    {
        _logFile = logFile;
    }

    public string LastLogPath => _logFile;

    public bool HasFreshLog => File.Exists(_logFile);

    /// <summary>
    /// Runs off the caller's thread from its first line, not from its first
    /// await. The GUI awaits this on the dispatcher immediately after a Move or
    /// a Delete, and the folder create, the serialise and the atomic open all
    /// sit before the first await: on a roaming or redirected profile that is
    /// network I/O on the UI thread. The token is deliberately not passed to
    /// Task.Run, so a run cancelled before this starts still comes back as a
    /// false rather than as a throw, which is what the caller handles.
    /// </summary>
    public Task<bool> WriteAsync(ResultLogEntry entry, CancellationToken cancellationToken = default) =>
        Task.Run(() => WriteCoreAsync(entry, cancellationToken));

    private async Task<bool> WriteCoreAsync(ResultLogEntry entry, CancellationToken cancellationToken)
    {
        // Named outside the try so every failure path below can remove it,
        // matching SettingsService.TrySave. A refused open, a cancelled token, a
        // full disk or a failed rename all leave the temp file on disk otherwise,
        // and it shares a folder with settings.json for the life of the install.
        var tempFile = _logFile + "." + Path.GetRandomFileName() + ".tmp";
        try
        {
            // GetDirectoryName is null only for a root path, which the
            // default never is and a test sandbox never should be.
            var folder = Path.GetDirectoryName(_logFile);
            if (folder is not null) Directory.CreateDirectory(folder);

            var json = JsonSerializer.Serialize(entry, JsonOptions);

            // OpenAtomic + MoveFileEx(REPLACE_EXISTING) keeps the swap
            // race-free: the temp open refuses a symlink, and the
            // rename replaces a symlink at the destination rather than
            // following it.
            using (var handle = StorageHelpers.OpenAtomic(
                       tempFile, FileAccess.Write, StorageHelpers.AtomicOpenMode.CreateAlways))
            {
                // Null means the open was refused after CREATE_ALWAYS had already
                // made the file (a reparse point at the name, or an attribute read
                // that failed), so there is something to clean up here too.
                if (handle is null)
                {
                    StorageHelpers.TryDeleteTempFile(tempFile);
                    return false;
                }
                using var fs = new FileStream(handle, FileAccess.Write);
                await fs.WriteAsync(Encoding.UTF8.GetBytes(json), cancellationToken).ConfigureAwait(false);
            }

            File.Move(tempFile, _logFile, overwrite: true);
            return true;
        }
        catch (Exception ex)
        {
            CrashLog.TryWrite(ex);
            StorageHelpers.TryDeleteTempFile(tempFile);
            return false;
        }
    }

    public async Task<ResultLogSendOutcome> SendAsync(string body, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(body))
            return ResultLogSendOutcome.NoLogToSend;

        // Defence in depth: a caller that builds the body in-memory
        // (rather than piping through ReadLastLogAsync, which enforces
        // MaxLogBytes on read) would otherwise bypass the byte cap.
        if (Encoding.UTF8.GetByteCount(body) > IResultLogService.MaxLogBytes)
            return ResultLogSendOutcome.Unknown;

        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Post, EndpointUrl);
            request.Headers.UserAgent.ParseAdd(UserAgent);
            request.Content = new StringContent(body, Encoding.UTF8);
            request.Content.Headers.ContentType = new MediaTypeHeaderValue("application/json");

            // ResponseHeadersRead returns as soon as the status line and
            // headers are in. Only IsSuccessStatusCode is read and Content
            // is never touched, so a body from a hijacked or DNS-poisoned
            // endpoint never materialises here at all; the default
            // ResponseContentRead would buffer one, bounded by the 4 KiB
            // MaxResponseContentBufferSize above, which refuses past that
            // rather than truncating.
            using var response = await HttpClient.SendAsync(
                    request, HttpCompletionOption.ResponseHeadersRead, cancellationToken)
                .ConfigureAwait(false);
            return response.IsSuccessStatusCode
                ? ResultLogSendOutcome.Sent
                : ResultLogSendOutcome.ServerError;
        }
        catch (TaskCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            return ResultLogSendOutcome.Timeout;
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (HttpRequestException ex)
        {
            CrashLog.TryWrite(ex);
            return ResultLogSendOutcome.NetworkUnavailable;
        }
        catch (Exception ex)
        {
            CrashLog.TryWrite(ex);
            return ResultLogSendOutcome.Unknown;
        }
    }

    /// <summary>
    /// Off the caller's thread from its first line, for the same reason as
    /// <see cref="WriteAsync"/>: the existence check, the atomic open and the
    /// length read all precede the first await, and this one is awaited on the
    /// dispatcher with the send dialog about to open. The token is not passed to
    /// Task.Run, so a cancellation still arrives as the rethrow below rather
    /// than from the scheduler.
    /// </summary>
    public Task<string?> ReadLastLogAsync(CancellationToken cancellationToken = default) =>
        Task.Run(() => ReadLastLogCoreAsync(cancellationToken));

    private async Task<string?> ReadLastLogCoreAsync(CancellationToken cancellationToken)
    {
        if (!File.Exists(_logFile))
            return null;

        try
        {
            using var handle = StorageHelpers.OpenAtomic(
                _logFile, FileAccess.Read, StorageHelpers.AtomicOpenMode.OpenExisting);
            if (handle is null) return null;
            using var fs = new FileStream(handle, FileAccess.Read);
            if (fs.Length > IResultLogService.MaxLogBytes)
            {
                // Oversize is not a normal outcome (writer caps at the
                // schema's natural size); record it so a "Didn't work"
                // user report has a breadcrumb to follow.
                CrashLog.TryWrite(new InvalidDataException(
                    $"last-run.json exceeds the {IResultLogService.MaxLogBytes}-byte cap and was not read."));
                return null;
            }
            using var reader = new StreamReader(fs, Encoding.UTF8);
            return await reader.ReadToEndAsync(cancellationToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            CrashLog.TryWrite(ex);
            return null;
        }
    }
}
