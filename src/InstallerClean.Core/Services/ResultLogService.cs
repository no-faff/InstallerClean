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
    private const long MaxLogBytes = 64 * 1024;

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
        Timeout = TimeSpan.FromSeconds(8),
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

    public async Task<ResultLogSendOutcome> SendAsync(CancellationToken cancellationToken = default)
    {
        if (!File.Exists(LogFile))
            return ResultLogSendOutcome.NoLogToSend;

        try
        {
            string body;
            using (var handle = StorageHelpers.OpenAtomic(
                       LogFile, FileAccess.Read, StorageHelpers.AtomicOpenMode.OpenExisting))
            {
                if (handle is null)
                    return ResultLogSendOutcome.NoLogToSend;
                using var fs = new FileStream(handle, FileAccess.Read);
                if (fs.Length > MaxLogBytes)
                    return ResultLogSendOutcome.Unknown;
                using var reader = new StreamReader(fs, Encoding.UTF8);
                body = await reader.ReadToEndAsync(cancellationToken).ConfigureAwait(false);
            }

            using var request = new HttpRequestMessage(HttpMethod.Post, EndpointUrl);
            request.Headers.UserAgent.ParseAdd(UserAgent);
            request.Content = new StringContent(body, Encoding.UTF8);
            request.Content.Headers.ContentType = new MediaTypeHeaderValue("application/json");

            using var response = await HttpClient.SendAsync(request, cancellationToken)
                .ConfigureAwait(false);
            if (!response.IsSuccessStatusCode)
                return ResultLogSendOutcome.ServerError;
            return ResultLogSendOutcome.Sent;
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
}
