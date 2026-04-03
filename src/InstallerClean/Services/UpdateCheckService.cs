using System.Net.Http;
using System.Reflection;
using System.Text.Json;

namespace InstallerClean.Services;

public sealed class UpdateCheckService : IUpdateCheckService
{
    private const string ApiUrl = "https://api.github.com/repos/no-faff/InstallerClean/releases/latest";

    public async Task<string?> GetLatestVersionAsync()
    {
        try
        {
            using var client = new HttpClient();
            client.DefaultRequestHeaders.Add("User-Agent", "InstallerClean");
            client.Timeout = TimeSpan.FromSeconds(10);

            var json = await client.GetStringAsync(ApiUrl);
            using var doc = JsonDocument.Parse(json);
            var tagName = doc.RootElement.GetProperty("tag_name").GetString();
            if (tagName is null) return null;

            var latestVersion = Version.Parse(tagName.TrimStart('v'));
            var assemblyVersion = Assembly.GetExecutingAssembly().GetName().Version;
            if (assemblyVersion is null) return null;

            var currentVersion = new Version(assemblyVersion.Major, assemblyVersion.Minor,
                assemblyVersion.Build < 0 ? 0 : assemblyVersion.Build);

            return latestVersion > currentVersion ? tagName : null;
        }
        catch
        {
            return null;
        }
    }
}
