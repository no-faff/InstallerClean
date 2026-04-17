using System.Net.Http;
using System.Reflection;
using System.Text.Json;
using InstallerClean.Models;

namespace InstallerClean.Services;

public sealed class UpdateCheckService : IUpdateCheckService, IDisposable
{
    private const string ApiUrl = "https://api.github.com/repos/no-faff/InstallerClean/releases/latest";

    private readonly HttpClient _httpClient;
    private readonly bool _ownsClient;
    private bool _disposed;

    public UpdateCheckService()
    {
        _httpClient = new HttpClient
        {
            Timeout = TimeSpan.FromSeconds(10),
            // GitHub's real response is ~10 KB; the cap defends against a
            // malicious or misbehaving endpoint serving a much larger body.
            MaxResponseContentBufferSize = 256 * 1024,
        };
        _httpClient.DefaultRequestHeaders.Add("User-Agent", "InstallerClean");
        _ownsClient = true;
    }

    internal UpdateCheckService(HttpClient httpClient)
    {
        _httpClient = httpClient;
        _ownsClient = false;
    }

    public async Task<UpdateCheckResult> GetLatestVersionAsync()
    {
        if (_disposed) return UpdateCheckResult.Failed();

        string json;
        try
        {
            json = await _httpClient.GetStringAsync(ApiUrl);
        }
        catch
        {
            return UpdateCheckResult.Failed();
        }

        try
        {
            using var doc = JsonDocument.Parse(json);
            var tagName = doc.RootElement.GetProperty("tag_name").GetString();
            if (string.IsNullOrEmpty(tagName))
                return UpdateCheckResult.Failed();

            var latestVersion = Version.Parse(tagName.TrimStart('v'));
            var assemblyVersion = Assembly.GetExecutingAssembly().GetName().Version;
            if (assemblyVersion is null)
                return UpdateCheckResult.Failed();

            var currentVersion = new Version(assemblyVersion.Major, assemblyVersion.Minor,
                assemblyVersion.Build < 0 ? 0 : assemblyVersion.Build);

            return latestVersion > currentVersion
                ? UpdateCheckResult.Available(tagName)
                : UpdateCheckResult.UpToDate();
        }
        catch
        {
            // Malformed JSON or unparseable version must not become a false
            // "up to date" signal.
            return UpdateCheckResult.Failed();
        }
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        if (_ownsClient) _httpClient.Dispose();
    }
}
