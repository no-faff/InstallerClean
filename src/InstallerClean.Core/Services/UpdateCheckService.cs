using System.Net.Http;
using System.Net.Http.Headers;
using System.Reflection;
using InstallerClean.Helpers;

namespace InstallerClean.Services;

/// <summary>
/// Version check, read from the redirect GitHub answers the releases page
/// with.
/// </summary>
/// <remarks>
/// The HTTP call lives inside the elevated process. CheckAsync is reached
/// only from ChromeViewModel, from two places: the automatic check, which
/// runs once per session at launch if <c>AppSettings.AutoUpdateCheck</c> is
/// set, and the main window's update button, which the user can press any
/// number of times, each press spaced from the last by the button's own
/// cooldown. There is no timer and no retry. Which of the two is calling
/// arrives as <see cref="UpdateCheckOrigin"/> and decides how much of a
/// failure is worth writing down.
///
/// The version is read from the redirect rather than from
/// api.github.com, because that API's anonymous allowance is spent by
/// address rather than by caller: 60 requests an hour, and GitHub
/// documents unauthenticated requests as "associated with the originating
/// IP address, not with the user or application that made the request",
/// answering 403 with x-ratelimit-remaining=0 once they are gone. Behind a
/// shared address, an office, CGNAT, or a commercial VPN's exit node,
/// unrelated traffic can leave nothing for this check, and there is
/// nothing the app or the user can do about it. GitHub publishes no such
/// allowance for the ordinary releases page.
///
/// The redirect is GitHub's long-standing behaviour rather than a
/// documented contract, so every unexpected-answer path below is kept: if
/// /latest stops redirecting, the check reports a failure and the app is
/// otherwise unaffected.
///
/// One GET, and the body is never read. The answer contributes a version
/// number to compare and nothing else; the browser's destination is
/// <see cref="ReleasesPageUrl"/>, a constant, so nothing a server returns
/// decides where the user is sent.
///
/// The shipping instance holds HttpClient in a static field per the
/// documented BCL guidance: a fresh instance per call leaks Windows-side
/// socket handles under concurrent use, and the check is cheap enough that
/// reusing the connection pool across runs of the dialog is fine.
/// </remarks>
public sealed class UpdateCheckService : IUpdateCheckService
{
    // Identifies the app to GitHub. RFC 9110 product =
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
    /// Both what the check asks for and the one page any "there is an update"
    /// control opens: the status-line link and the update dialog's open button
    /// both come here. Deliberately the /latest redirect rather than the found
    /// release's own tag page, so the click lands on whatever is newest at the
    /// moment it happens rather than on the tag this particular check saw.
    /// </summary>
    public const string ReleasesPageUrl =
        "https://github.com/no-faff/InstallerClean/releases/latest";

    private static readonly Uri ReleasesPage = new(ReleasesPageUrl);

    /// <summary>
    /// The path a usable Location has to sit under, built from
    /// <see cref="ReleasesPageUrl"/> so a repository move cannot leave the
    /// check accepting redirects into somewhere the app would never open:
    /// "/owner/repo/releases/" with "latest" swapped for "tag/".
    /// </summary>
    private static readonly string TagPathPrefix =
        ReleasesPage.AbsolutePath[..(ReleasesPage.AbsolutePath.LastIndexOf('/') + 1)] + "tag/";

    /// <summary>
    /// Redirect following OFF is what makes the check work: the answer it
    /// reads IS the 302, and a followed redirect delivers the tag page's
    /// HTML instead, which carries no version this parses. Cookies off
    /// because the elevated process has no use for the session cookie
    /// github.com offers, and declining it is cheaper than holding it.
    /// Internal so a test can pin both, neither being reachable through the
    /// handler seam the rest of the tests use.
    /// </summary>
    internal static HttpClientHandler CreateShippingHandler() =>
        new() { AllowAutoRedirect = false, UseCookies = false };

    private static readonly HttpClient SharedClient = CreateClient(CreateShippingHandler());

    private static HttpClient CreateClient(HttpMessageHandler handler)
    {
        var client = new HttpClient(handler);
        client.Timeout = RequestTimeout;
        // Bounds the eager buffering only: MaxResponseContentBufferSize gates
        // HttpCompletionOption.ResponseContentRead, and a ReadAsStringAsync
        // taken after the ResponseHeadersRead send in CheckAsync buffers
        // straight past it (400 KiB read back whole under this 256 KiB cap,
        // .NET 10). It is what would gate the read if that completion option
        // ever changed; what keeps a body out of this process is that
        // nothing reads one.
        client.MaxResponseContentBufferSize = 256 * 1024;
        return client;
    }

    private readonly HttpClient _http;
    private readonly Action<Exception> _logDiagnostic;

    public UpdateCheckService() : this(SharedClient, ex => CrashLog.TryWrite(ex)) { }

    /// <summary>
    /// Test seam. A handler stands in for the network so the failures this
    /// check exists to survive (DNS refusal, a proxy's 403, an HTML login
    /// page answered with 200) can be produced deterministically, and the
    /// sink collects what would have gone to crash.log rather than writing
    /// to the real one under the runner's own profile. Both are built
    /// through the same <see cref="CreateClient"/> the shipping path uses,
    /// so the timeout and body cap under test are the shipping values.
    /// </summary>
    internal UpdateCheckService(HttpMessageHandler handler, Action<Exception> logDiagnostic)
        : this(CreateClient(handler), logDiagnostic) { }

    private UpdateCheckService(HttpClient http, Action<Exception> logDiagnostic)
    {
        _http = http;
        _logDiagnostic = logDiagnostic;
    }

    public async Task<UpdateCheckResult> CheckAsync(
        UpdateCheckOrigin origin, CancellationToken cancellationToken = default)
    {
        var currentVersion = GetCurrentVersion();

        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, ReleasesPageUrl);
            request.Headers.UserAgent.ParseAdd(UserAgent);

            // ResponseHeadersRead because the headers are the whole answer.
            // The redirect's body is empty in practice, and none of it is
            // read either way.
            using var response = await _http.SendAsync(
                    request, HttpCompletionOption.ResponseHeadersRead, cancellationToken)
                .ConfigureAwait(false);

            var status = (int)response.StatusCode;
            if (status is < 300 or >= 400)
            {
                // Two different situations wearing one status range each,
                // and the sentence on screen differs accordingly. A 4xx or
                // 5xx is GitHub refusing. A 2xx is something answering in
                // GitHub's place, a captive portal or a filtering proxy
                // serving its own page, because the releases page itself
                // does not answer this request with a body.
                //
                // Either way the status alone is useless for diagnosis
                // after the fact, and nothing in the breadcrumb names a
                // user, a path or a machine.
                LogIfUserIsWaiting(origin, new HttpRequestException(
                    $"GitHub releases page returned {status} " +
                    $"({response.StatusCode}).{DescribeThrottling(response.Headers)}"));
                return new CheckFailed(response.IsSuccessStatusCode
                    ? UpdateCheckFailureReason.ResponseParseError
                    : UpdateCheckFailureReason.ServerError);
            }

            if (!TryReadTag(response.Headers.Location, out var tagName))
            {
                // A redirect somewhere else entirely. Worth the whole
                // Location because this is the shape that would report
                // GitHub having changed how /latest answers, which is the
                // one assumption this check rests on.
                LogIfUserIsWaiting(origin, new HttpRequestException(
                    $"GitHub releases page returned {status} with an unusable " +
                    $"Location: {Clamp(response.Headers.Location?.ToString() ?? "(absent)")}"));
                return new CheckFailed(UpdateCheckFailureReason.ResponseParseError);
            }

            // Tags on the project's releases are "vX.Y.Z"; strip
            // the leading 'v' before parsing as System.Version.
            var latestVersion = tagName.StartsWith('v')
                ? tagName.Substring(1)
                : tagName;
            if (!Version.TryParse(latestVersion, out var parsedLatest))
            {
                LogIfUserIsWaiting(origin, new HttpRequestException(
                    $"GitHub redirected to a tag that is not a version: {Clamp(tagName)}"));
                return new CheckFailed(UpdateCheckFailureReason.ResponseParseError);
            }

            // Normalise both sides to MAJOR.MINOR.BUILD before
            // comparing: System.Version's fourth Revision component is
            // 0 in the assembly version (e.g. 1.7.0.0) but absent from
            // the GitHub tag. Comparing without normalising would
            // make 1.7.0.0 always "newer than" 1.7.0.
            var currentNormalised = NormaliseToBuild(currentVersion);
            var latestNormalised = NormaliseToBuild(parsedLatest);

            if (latestNormalised > currentNormalised)
            {
                // Only the two version strings. The destination is
                // ReleasesPageUrl, a constant, so nothing the answer
                // carried reaches the browser.
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
            LogIfUserIsWaiting(origin, ex);
            return new CheckFailed(UpdateCheckFailureReason.NetworkUnavailable);
        }
        catch (Exception ex)
        {
            // Ungated on purpose. Everything above is a network the app was
            // built to meet; what reaches here is not, and an unanticipated
            // exception inside an elevated process is what crash.log is for.
            _logDiagnostic(ex);
            return new CheckFailed(UpdateCheckFailureReason.Unknown);
        }
    }

    /// <summary>
    /// Writes a network-shaped failure to crash.log only for a check the
    /// user pressed a button for and is watching a dialog about, where the
    /// log entry is what backs the sentence on screen.
    ///
    /// The automatic check stays silent because the machines it fails on are
    /// a normal population rather than an error state: offline, air-gapped,
    /// or behind egress filtering that refuses github.com. That check
    /// runs unattended at every launch, so a logged failure there is a stack
    /// trace per run, accumulating in a file named crash.log on a machine
    /// where nothing has crashed. It is also a file this project invites
    /// sceptical users to read, which makes "the app writes a crash entry
    /// every time I start it" the impression it leaves. The timeout path has
    /// never written for the same reason.
    /// </summary>
    private void LogIfUserIsWaiting(UpdateCheckOrigin origin, Exception ex)
    {
        if (origin == UpdateCheckOrigin.Manual)
            _logDiagnostic(ex);
    }

    /// <summary>
    /// Whatever the answer says about being throttled, as a log fragment,
    /// empty when it says nothing. Each header is reported only if present,
    /// because absence is itself the reading.
    ///
    /// retry-after is the one that can legitimately appear here: GitHub's
    /// abuse protection sets it, and it names its own recovery. The
    /// x-ratelimit pair belongs to the REST API's per-address allowance,
    /// which the releases page has no equivalent of, so either of those
    /// turning up means something other than the releases page answered,
    /// which is worth knowing and costs two dictionary lookups to catch.
    /// </summary>
    private static string DescribeThrottling(HttpResponseHeaders headers)
    {
        var parts = new List<string>(RateLimitHeaders.Length);
        foreach (var name in RateLimitHeaders)
        {
            if (headers.TryGetValues(name, out var values))
                parts.Add($"{name}={Clamp(string.Join(',', values))}");
        }
        return parts.Count == 0 ? string.Empty : " " + string.Join(", ", parts);
    }

    // Everything clamped here came off the wire, from whoever answered,
    // which on a failing check is not necessarily GitHub. crash.log rotates
    // at 512 KiB and HttpClient accepts 64 KiB of response headers, so
    // verbatim values let an intermediary roll the real crash history out of
    // the log in a handful of failed checks. A genuine value here is a small
    // integer or a short URL, so the cap costs nothing and anything that
    // reaches it is itself worth seeing the head of.
    private static string Clamp(string value) =>
        value.Length <= MaxLoggedHeaderChars
            ? value
            : value[..MaxLoggedHeaderChars] + "...(truncated)";

    private const int MaxLoggedHeaderChars = 100;

    private static readonly string[] RateLimitHeaders =
        ["retry-after", "x-ratelimit-remaining", "x-ratelimit-reset"];

    /// <summary>
    /// Pulls the release tag out of a redirect's Location, or refuses it.
    /// Refusing is the important half: the version taken from here is shown
    /// to the user, so it may come only from the releases path of the host
    /// the app itself links to, never from wherever an answer points.
    /// </summary>
    private static bool TryReadTag(Uri? location, out string tag)
    {
        tag = string.Empty;
        if (location is null) return false;

        // A relative Location is legal per RFC 9110, so it is resolved
        // against the request's own URL rather than refused outright.
        // Resolution is not a safety step: "//host/path" is relative by
        // System.Uri's reckoning (IsAbsoluteUri is false) and resolves to
        // that host, so the scheme and host comparisons below are what keep
        // an answer from nominating where the version comes from.
        var absolute = location.IsAbsoluteUri ? location : new Uri(ReleasesPage, location);

        if (!string.Equals(absolute.Scheme, ReleasesPage.Scheme, StringComparison.OrdinalIgnoreCase))
            return false;
        if (!string.Equals(absolute.Host, ReleasesPage.Host, StringComparison.OrdinalIgnoreCase))
            return false;
        if (!absolute.AbsolutePath.StartsWith(TagPathPrefix, StringComparison.Ordinal))
            return false;

        var candidate = absolute.AbsolutePath[TagPathPrefix.Length..];
        // One path segment, or it is not a tag: a deeper path is some other
        // page under the same prefix rather than a release.
        if (candidate.Length == 0 || candidate.Contains('/')) return false;

        tag = candidate;
        return true;
    }

    private static Version GetCurrentVersion() =>
        Assembly.GetExecutingAssembly().GetName().Version ?? new Version(0, 0, 0);

    private static Version NormaliseToBuild(Version v) =>
        new(v.Major, v.Minor, Math.Max(v.Build, 0));

    private static string FormatVersion(Version v) =>
        $"{v.Major}.{v.Minor}.{v.Build}";
}
