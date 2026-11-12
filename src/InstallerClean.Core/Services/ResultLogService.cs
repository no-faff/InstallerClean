using System.Net.Http;
using System.Net.Http.Headers;
using System.Reflection;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;
using InstallerClean.Helpers;
using InstallerClean.Models;

namespace InstallerClean.Services;

/// <summary>
/// Default <see cref="IResultLogService"/>. Writes the JSON to
/// <c>%LOCALAPPDATA%\InstallerClean\last-run.json</c> via the
/// same atomic-rename pattern <see cref="SettingsService"/> uses, and
/// POSTs the file's contents to the No Faff result-log endpoint.
/// </summary>
public sealed class ResultLogService : IResultLogService
{
    /// <summary>
    /// Maximum size of <c>last-run.json</c> the service will read or
    /// POST. The writer caps the JSON at this size by construction
    /// (the schema's natural size is well under it); a file larger
    /// than this came from outside the process and is rejected.
    /// </summary>
    public const long MaxLogBytes = 64 * 1024;

    private const string EndpointUrl = "https://nofaff.netlify.app/api/result-log";

    private static readonly TimeSpan RequestTimeout = Durations.FromSeconds(8);

    private readonly string _logFile;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.Never,
        TypeInfoResolver = new DefaultJsonTypeInfoResolver { Modifiers = { InDeclarationOrder } },
    };

    /// <summary>
    /// Writes each object's properties in the order its type declares them, which is the
    /// order .NET 10 writes them in. The serialiser otherwise takes its order from
    /// Type.GetProperties, which returns properties in no particular order, and on .NET
    /// Framework the order it gives can differ from one process to the next.
    /// </summary>
    private static void InDeclarationOrder(JsonTypeInfo info)
    {
        if (info.Kind != JsonTypeInfoKind.Object) return;
        var declared = info.Properties
            .OrderBy(p => (p.AttributeProvider as MemberInfo)?.MetadataToken ?? int.MaxValue)
            .ToList();
        for (var i = 0; i < declared.Count; i++) declared[i].Order = i;
    }

    /// <summary>The report as last-run.json holds it.</summary>
    internal static string Serialize(ResultLogEntry entry) => JsonSerializer.Serialize(entry, JsonOptions);

    private static readonly string UserAgent =
        $"InstallerClean/{Assembly.GetExecutingAssembly().GetName().Version?.ToString(3) ?? "0.0.0"}";

    // The handler carries the proxy .NET 10 takes by default (ProxyFromVariables).
    private static readonly HttpClient HttpClient = new(ProxyFromVariables.Carrying(new HttpClientHandler()))
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

    public ResultLogService() : this(PathText.Combine(AppDataFolder.Location, "last-run.json")) { }

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
            // GetDirectoryName is null for a root, an empty path and one of spaces
            // alone, and empty for a bare file name, which names the current folder:
            // there is no folder to make for any of them.
            var folder = PathText.GetDirectoryName(_logFile);
            if (folder is { Length: > 0 }) FrameworkDirectory.CreateDirectory(folder);

            var json = Serialize(entry);

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

    /// <summary>
    /// The POST carrying <paramref name="body"/> to <paramref name="url"/>: the app's user
    /// agent, the body as UTF-8 JSON, and no <c>Expect: 100-continue</c>, so the body goes
    /// with the headers as .NET 10 sends it. .NET Framework's handler asks the server's
    /// leave before a body unless the request says otherwise, and holds the body until the
    /// server answers or its wait runs out. It takes the request's word as the setting of
    /// the service point for the endpoint's host, or for the proxy in front of it, which
    /// every later request through that point shares; the app sends no other body.
    /// </summary>
    internal static HttpRequestMessage Request(string body, string url)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, url);
        request.Headers.UserAgent.ParseAdd(UserAgent);
        request.Headers.ExpectContinue = false;
        request.Content = new StringContent(body, Encoding.UTF8);
        request.Content.Headers.ContentType = new MediaTypeHeaderValue("application/json");
        return request;
    }

    public async Task<ResultLogSendOutcome> SendAsync(string body, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(body))
            return ResultLogSendOutcome.NoLogToSend;

        // Defence in depth: a caller that builds the body in-memory
        // (rather than piping through ReadLastLogAsync, which enforces
        // MaxLogBytes on read) would otherwise bypass the byte cap.
        if (Encoding.UTF8.GetByteCount(body) > MaxLogBytes)
            return ResultLogSendOutcome.Unknown;

        try
        {
            using var request = Request(body, EndpointUrl);

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
    /// length read all precede the first await. The token is not passed to
    /// Task.Run, so a cancellation still arrives as the rethrow below rather
    /// than from the scheduler.
    /// </summary>
    public Task<string?> ReadLastLogAsync(CancellationToken cancellationToken = default) =>
        Task.Run(() => ReadLastLogCoreAsync(cancellationToken));

    /// <summary>
    /// File.Exists, which answers false rather than throwing where the folder cannot be
    /// read. Called from the window's start check on a thread-pool thread.
    /// </summary>
    public bool LastLogExists() => FrameworkFile.Exists(_logFile);

    private async Task<string?> ReadLastLogCoreAsync(CancellationToken cancellationToken)
    {
        if (!FrameworkFile.Exists(_logFile))
            return null;

        try
        {
            using var handle = StorageHelpers.OpenAtomic(
                _logFile, FileAccess.Read, StorageHelpers.AtomicOpenMode.OpenExisting);
            if (handle is null) return null;
            using var fs = new FileStream(handle, FileAccess.Read);
            if (fs.Length > MaxLogBytes)
            {
                // Oversize is not a normal outcome (writer caps at the
                // schema's natural size); record it so a report that never
                // went has a breadcrumb to follow.
                CrashLog.TryWrite(new InvalidDataException(
                    $"last-run.json exceeds the {MaxLogBytes}-byte cap and was not read."));
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
