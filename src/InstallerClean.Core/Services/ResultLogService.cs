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
    public const long MaxLogBytes = 64 * 1024;

    private static readonly TimeSpan RequestTimeout = TimeSpan.FromSeconds(8);

    private static readonly string LogFolder = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "NoFaff", "InstallerClean");

    private static readonly string LogFile = Path.Combine(LogFolder, "last-run.json");

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
    };

    public string LastLogPath => LogFile;

    public bool HasFreshLog => File.Exists(LogFile);

    public async Task<bool> WriteAsync(ResultLogEntry entry, CancellationToken cancellationToken = default)
    {
        try
        {
            Directory.CreateDirectory(LogFolder);

            var json = JsonSerializer.Serialize(entry, JsonOptions);
            var tempFile = LogFile + "." + Path.GetRandomFileName() + ".tmp";

            // OpenAtomic + MoveFileEx(REPLACE_EXISTING) keeps the swap
            // race-free: the temp open refuses a symlink, and the
            // rename replaces a symlink at the destination rather than
            // following it.
            using (var handle = StorageHelpers.OpenAtomic(
                       tempFile, FileAccess.Write, StorageHelpers.AtomicOpenMode.CreateAlways))
            {
                if (handle is null)
                    return false;
                using var fs = new FileStream(handle, FileAccess.Write);
                await fs.WriteAsync(Encoding.UTF8.GetBytes(json), cancellationToken).ConfigureAwait(false);
            }

            File.Move(tempFile, LogFile, overwrite: true);
            return true;
        }
        catch (Exception ex)
        {
            CrashLog.TryWrite(ex);
            return false;
        }
    }

    public async Task<ResultLogSendOutcome> SendAsync(string body, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(body))
            return ResultLogSendOutcome.NoLogToSend;

        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Post, EndpointUrl);
            request.Headers.UserAgent.ParseAdd(UserAgent);
            request.Content = new StringContent(body, Encoding.UTF8);
            request.Content.Headers.ContentType = new MediaTypeHeaderValue("application/json");

            // ResponseHeadersRead returns as soon as the status line and
            // headers are in. The caller only reads IsSuccessStatusCode
            // and never touches Content; with the default
            // ResponseContentRead the body would be buffered into memory
            // anyway, exposing the elevated process to an oversize body
            // from a hijacked or DNS-poisoned endpoint.
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

    public async Task<string?> ReadLastLogAsync(CancellationToken cancellationToken = default)
    {
        if (!File.Exists(LogFile))
            return null;

        try
        {
            using var handle = StorageHelpers.OpenAtomic(
                LogFile, FileAccess.Read, StorageHelpers.AtomicOpenMode.OpenExisting);
            if (handle is null) return null;
            using var fs = new FileStream(handle, FileAccess.Read);
            if (fs.Length > MaxLogBytes)
            {
                // Oversize is not a normal outcome (writer caps at the
                // schema's natural size); record it so a "Didn't work"
                // user report has a breadcrumb to follow.
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
