using System.Net.Http;
using System.Net.Http.Headers;
using System.Reflection;
using System.Text.Json;
using InstallerClean.Helpers;

namespace InstallerClean.Services;

/// <summary>
/// Version check against the GitHub Releases API.
/// </summary>
/// <remarks>
/// The HTTP call lives inside the elevated process. CheckAsync is reached
/// only from ChromeViewModel, from two places: the automatic check, which
/// runs once per session at launch if <c>AppSettings.AutoUpdateCheck</c> is
/// set, and the main window's update button, which the user can press any
/// number of times, each press spaced from the last by the button's own
/// cooldown. There is no timer and no retry.
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

    /// <summary>
    /// The one page any "there is an update" control opens: the status-line
    /// link and the update dialog's open button both come here. Deliberately
    /// the /latest redirect rather than the found release's own tag page, so
    /// the click lands on whatever is newest at the moment it happens rather
    /// than on the tag this particular check saw. It also keeps the browser's
    /// destination out of the response body entirely: nothing GitHub returns
    /// decides where the user is sent.
    /// </summary>
    public const string ReleasesPageUrl =
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
            {
                // ServerError collapses every non-2xx into one localised
                // sentence, which is right for the dialog and useless for
                // diagnosis: the common case is GitHub's 60-per-hour
                // unauthenticated rate limit answering 403, and it is
                // indistinguishable from a real outage in what the user can
                // see or report. The status plus the rate-limit headers
                // separate them, and none of it names a user, a path or a
                // machine.
                CrashLog.TryWrite(new HttpRequestException(
                    $"GitHub releases API returned {(int)response.StatusCode} " +
                    $"({response.StatusCode}).{DescribeRateLimit(response.Headers)}"));
                return new CheckFailed(UpdateCheckFailureReason.ServerError);
            }

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
                // Only the two version strings. The destination is
                // ReleasesPageUrl, fixed, so the response body supplies
                // nothing the browser acts on.
                return new UpdateAvailable(
                    CurrentVersion: FormatVersion(currentNormalised),
                    LatestVersion: FormatVersion(latestNormalised));
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

    /// <summary>
    /// GitHub's rate-limit headers as a log fragment, empty when none
    /// arrived. Each is reported only if present, because absence is itself
    /// the reading: a captive portal or a corporate proxy answering the 403
    /// in GitHub's place sends none of them, and telling that apart from a
    /// genuine limit is what the breadcrumb is for.
    ///
    /// All three are needed to cover the two limits. The primary one spends
    /// x-ratelimit-remaining down to 0 and names its recovery in
    /// x-ratelimit-reset. The secondary limit also answers 403 but is
    /// documented to signal itself with retry-after and need not report the
    /// primary allowance as exhausted, so on those two headers alone it is
    /// indistinguishable from a bare 403 out of a proxy.
    /// </summary>
    private static string DescribeRateLimit(HttpResponseHeaders headers)
    {
        var parts = new List<string>(RateLimitHeaders.Length);
        foreach (var name in RateLimitHeaders)
        {
            if (headers.TryGetValues(name, out var values))
                parts.Add($"{name}={Clamp(string.Join(',', values))}");
        }
        return parts.Count == 0 ? string.Empty : " " + string.Join(", ", parts);
    }

    // Header values come from whoever answered, which on the 403 path is not
    // necessarily GitHub. crash.log rotates at 512 KiB and HttpClient accepts
    // 64 KiB of response headers, so verbatim values let an intermediary roll
    // the real crash history out of the log in a handful of failed checks.
    // A genuine value here is a small integer, so the cap costs nothing and
    // anything that reaches it is itself worth seeing the head of.
    private static string Clamp(string value) =>
        value.Length <= MaxLoggedHeaderChars
            ? value
            : value[..MaxLoggedHeaderChars] + "...(truncated)";

    private const int MaxLoggedHeaderChars = 100;

    private static readonly string[] RateLimitHeaders =
        ["x-ratelimit-remaining", "x-ratelimit-reset", "retry-after"];

    private static Version GetCurrentVersion() =>
        Assembly.GetExecutingAssembly().GetName().Version ?? new Version(0, 0, 0);

    private static Version NormaliseToBuild(Version v) =>
        new(v.Major, v.Minor, Math.Max(v.Build, 0));

    private static string FormatVersion(Version v) =>
        $"{v.Major}.{v.Minor}.{v.Build}";
}
