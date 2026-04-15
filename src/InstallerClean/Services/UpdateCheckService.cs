using System.Net.Http;
using System.Reflection;
using System.Text.Json;
using InstallerClean.Models;

namespace InstallerClean.Services;

public sealed class UpdateCheckService : IUpdateCheckService
{
    private const string ApiUrl = "https://api.github.com/repos/no-faff/InstallerClean/releases/latest";

    private readonly HttpClient _httpClient;

    public UpdateCheckService()
    {
        _httpClient = new HttpClient();
        _httpClient.DefaultRequestHeaders.Add("User-Agent", "InstallerClean");
        _httpClient.Timeout = TimeSpan.FromSeconds(10);
    }

    internal UpdateCheckService(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<UpdateCheckResult> GetLatestVersionAsync()
    {
        string json;
        try
        {
            json = await _httpClient.GetStringAsync(ApiUrl);
        }
        catch
        {
            // Network unreachable, timeout, DNS failure, HTTP error, etc.
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
            // Malformed JSON or unparseable version. Treat as failure rather
            // than a false "up to date" signal.
            return UpdateCheckResult.Failed();
        }
    }
}
