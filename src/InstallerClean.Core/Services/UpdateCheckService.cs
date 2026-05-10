using System.Net;
using System.Net.Http;
using System.Reflection;
using System.Text.Json;
using InstallerClean.Helpers;

namespace InstallerClean.Services;

/// <summary>
/// User-triggered version check against the GitHub Releases API.
/// </summary>
/// <remarks>
/// The HTTP call lives inside the elevated process. The architectural
/// constraint v1.5.3 inherited from the DeepInstinct flag was "no
/// automatic outbound HTTP from elevated startup": this service
/// preserves that constraint by only running when
/// <see cref="CheckAsync"/> is invoked from a user click. There is no
/// timer, no startup hook, no other call site.
///
/// HttpClient is held in a static field per the documented BCL
/// guidance: a fresh instance per call leaks Windows-side socket
/// handles under concurrent use, and the check is cheap enough that
/// reusing the connection pool across runs of the dialog is fine.
/// </remarks>
public sealed class UpdateCheckService : IUpdateCheckService
{
    private const string ApiUrl =
        "https://api.github.com/repos/no-faff/InstallerClean/releases/latest";

    // GitHub's API returns 403 if no User-Agent is supplied. The
    // project name plus the running version is the canonical form;
    // GitHub's docs use the same shape in their examples.
    private static readonly string UserAgent =
        $"InstallerClean/{DisplayHelpers.GetVersionString()}";

    private static readonly HttpClient HttpClient = new()
    {
        Timeout = TimeSpan.FromSeconds(8),
    };

    public async Task<UpdateCheckResult> CheckAsync(CancellationToken cancellationToken = default)
    {
        var currentVersion = GetCurrentVersion();

        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, ApiUrl);
            request.Headers.UserAgent.ParseAdd(UserAgent);
            request.Headers.Accept.ParseAdd("application/vnd.github+json");

            using var response = await HttpClient.SendAsync(request, cancellationToken)
                .ConfigureAwait(false);
            if (!response.IsSuccessStatusCode)
                return new CheckFailed(UpdateCheckFailureReason.ServerError);

            var json = await response.Content.ReadAsStringAsync(cancellationToken)
                .ConfigureAwait(false);
            using var doc = JsonDocument.Parse(json);

            if (!doc.RootElement.TryGetProperty("tag_name", out var tagElement))
                return new CheckFailed(UpdateCheckFailureReason.ResponseParseError);
            var tagName = tagElement.GetString();
            if (string.IsNullOrWhiteSpace(tagName))
                return new CheckFailed(UpdateCheckFailureReason.ResponseParseError);

            // tag_name on the project's releases is "vX.Y.Z"; strip
            // the leading 'v' before parsing as System.Version.
            var latestVersion = tagName.StartsWith('v')
                ? tagName.Substring(1)
                : tagName;
            if (!Version.TryParse(latestVersion, out var parsedLatest))
                return new CheckFailed(UpdateCheckFailureReason.ResponseParseError);

            // Normalise both sides to MAJOR.MINOR.BUILD before
            // comparing: System.Version's fourth Revision component is
            // 0 in the assembly version (e.g. 1.7.0.0) but absent from
            // the GitHub tag_name. Comparing without normalising would
            // make 1.7.0.0 always "newer than" 1.7.0.
            var currentNormalised = NormaliseToBuild(currentVersion);
            var latestNormalised = NormaliseToBuild(parsedLatest);

            if (latestNormalised > currentNormalised)
            {
                var releaseUrl = doc.RootElement.TryGetProperty("html_url", out var urlElement)
                    ? urlElement.GetString() ?? "https://github.com/no-faff/InstallerClean/releases/latest"
                    : "https://github.com/no-faff/InstallerClean/releases/latest";

                return new UpdateAvailable(
                    CurrentVersion: FormatVersion(currentNormalised),
                    LatestVersion: FormatVersion(latestNormalised),
                    ReleaseUrl: releaseUrl);
            }

            return new UpToDate(FormatVersion(currentNormalised));
        }
        catch (TaskCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            // HttpClient throws TaskCanceledException on its own
            // Timeout setting; the request token wasn't cancelled by
            // the caller, so this is the timeout path.
            return new CheckFailed(UpdateCheckFailureReason.Timeout);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            // Caller cancelled. Surface the cancellation rather than
            // wrapping it in a result, so the UI can suppress the
            // "couldn't check" dialog when the user dismissed.
            throw;
        }
        catch (HttpRequestException ex)
        {
            CrashLog.TryWrite(ex);
            return new CheckFailed(UpdateCheckFailureReason.NetworkUnavailable);
        }
        catch (JsonException ex)
        {
            CrashLog.TryWrite(ex);
            return new CheckFailed(UpdateCheckFailureReason.ResponseParseError);
        }
        catch (Exception ex)
        {
            CrashLog.TryWrite(ex);
            return new CheckFailed(UpdateCheckFailureReason.Unknown);
        }
    }

    private static Version GetCurrentVersion() =>
        Assembly.GetExecutingAssembly().GetName().Version ?? new Version(0, 0, 0);

    private static Version NormaliseToBuild(Version v) =>
        new(v.Major, v.Minor, Math.Max(v.Build, 0));

    private static string FormatVersion(Version v) =>
        $"{v.Major}.{v.Minor}.{v.Build}";
}
