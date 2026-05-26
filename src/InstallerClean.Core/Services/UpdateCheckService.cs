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
/// The HTTP call lives inside the elevated process. CheckAsync runs
/// only when invoked from a user click: no timer, no startup hook,
/// no other call site.
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

    // GitHub's API returns 403 without a User-Agent. RFC 9110 product =
    // token "/" token; the version token must be a bare semver, no spaces,
    // because the localised "Version 1.8.0" display string contains an
    // internal space and parses as two adjacent products with no slash.
    // ResultLogService takes the same shape; the two must stay in sync.
    // Exposed internally so a unit test can assert the constant parses
    // through HttpRequestMessage.Headers.UserAgent.ParseAdd at build time.
    internal static readonly string UserAgent =
        $"InstallerClean/{Assembly.GetExecutingAssembly().GetName().Version?.ToString(3) ?? "0.0.0"}";

    private static readonly TimeSpan RequestTimeout = TimeSpan.FromSeconds(8);

    private const string ReleasesPageUrl =
        "https://github.com/no-faff/InstallerClean/releases/latest";

    // MaxDepth=8 matches SettingsService.JsonOptions. The schema is
    // shallow; the cap defends the elevated process against
    // pathologically nested JSON under the 256 KiB body cap.
    // Internal for the config-pin test.
    internal static readonly JsonDocumentOptions JsonParseOptions = new() { MaxDepth = 8 };

    private static readonly HttpClient HttpClient = new()
    {
        Timeout = RequestTimeout,
        MaxResponseContentBufferSize = 256 * 1024,
    };

    public async Task<UpdateCheckResult> CheckAsync(CancellationToken cancellationToken = default)
    {
        var currentVersion = GetCurrentVersion();

        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, ApiUrl);
            request.Headers.UserAgent.ParseAdd(UserAgent);
            request.Headers.Accept.ParseAdd("application/vnd.github+json");

            // HttpCompletionOption.ResponseHeadersRead so the body is
            // only buffered as it's read, not eagerly into HttpContent.
            // The 256 KiB cap on MaxResponseContentBufferSize still
            // gates the total bytes ReadAsStringAsync may materialise.
            using var response = await HttpClient.SendAsync(
                    request, HttpCompletionOption.ResponseHeadersRead, cancellationToken)
                .ConfigureAwait(false);
            if (!response.IsSuccessStatusCode)
                return new CheckFailed(UpdateCheckFailureReason.ServerError);

            var json = await response.Content.ReadAsStringAsync(cancellationToken)
                .ConfigureAwait(false);
            using var doc = JsonDocument.Parse(json, JsonParseOptions);

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
                // html_url comes from a TLS-validated github.com response,
                // so trust is anchored at the transport. Even so, the
                // returned string is constrained to the project's own
                // releases path; any other shape falls back to the
                // hardcoded ReleasesPageUrl so a manipulated response
                // can't redirect the user's browser elsewhere.
                var rawUrl = doc.RootElement.TryGetProperty("html_url", out var urlElement)
                    ? urlElement.GetString() : null;
                var releaseUrl = IsTrustedReleaseUrl(rawUrl) ? rawUrl! : ReleasesPageUrl;

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
            // Timeout setting; the request token was not cancelled
            // by the caller, so this is the timeout path rather than
            // the user-cancellation path below.
            return new CheckFailed(UpdateCheckFailureReason.Timeout);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            // Caller cancelled. Surface the cancellation as an
            // exception rather than wrapping it into a CheckFailed
            // result; an explicit OCE differentiates user-cancelled
            // from server-cancelled at the call site.
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

    internal static bool IsTrustedReleaseUrl(string? url)
    {
        if (string.IsNullOrEmpty(url)) return false;
        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri)) return false;
        if (uri.Scheme != Uri.UriSchemeHttps) return false;
        if (!string.Equals(uri.Host, "github.com", StringComparison.OrdinalIgnoreCase)) return false;
        // Host check is case-insensitive; path check follows. GitHub
        // serves the canonical lowercase form, but a redirect through
        // "/No-Faff/InstallerClean/releases/..." would still belong
        // to this project.
        return uri.AbsolutePath.StartsWith("/no-faff/InstallerClean/releases/", StringComparison.OrdinalIgnoreCase);
    }
}
