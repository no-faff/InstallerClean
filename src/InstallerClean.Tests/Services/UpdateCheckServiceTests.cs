using System.Net;
using System.Net.Http;
using InstallerClean.Services;
using Xunit;

namespace InstallerClean.Tests.Services;

/// <summary>
/// UpdateCheckService unit tests. The version comparison against a release
/// payload is not covered here; what is covered is the User-Agent (must parse
/// through HttpRequestMessage.Headers.UserAgent - a localised display string
/// in the version slot causes GitHub to return 403), the JSON depth cap, the
/// releases-page constant, and what each origin is allowed to write to
/// crash.log when the network will not answer. The last of those runs
/// against a stub handler and a collecting sink, so the failures are
/// deterministic and nothing is written under the runner's profile.
/// </summary>
public class UpdateCheckServiceTests
{
    /// <summary>
    /// Stands in for the network: one canned answer, or one thrown failure,
    /// for every request. The check issues exactly one GET, so a single
    /// response is the whole conversation.
    /// </summary>
    private sealed class StubHandler(Func<HttpResponseMessage> respond) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request, CancellationToken cancellationToken) =>
            Task.FromResult(respond());
    }

    private static HttpResponseMessage RateLimited()
    {
        var response = new HttpResponseMessage(HttpStatusCode.Forbidden);
        response.Headers.Add("x-ratelimit-remaining", "0");
        response.Headers.Add("x-ratelimit-reset", "1750000000");
        return response;
    }

    // A captive portal's login page: 200, and not JSON.
    private static HttpResponseMessage CaptivePortal() =>
        new(HttpStatusCode.OK) { Content = new StringContent("<html><body>Sign in</body></html>") };

    private static (UpdateCheckService Service, List<Exception> Logged) Build(
        Func<HttpResponseMessage> respond)
    {
        var logged = new List<Exception>();
        return (new UpdateCheckService(new StubHandler(respond), logged.Add), logged);
    }

    [Fact]
    public void UserAgent_parses_as_a_well_formed_HTTP_product()
    {
        // RFC 9110 product = token "/" token; if the version token
        // contains a space (e.g. "Version 1.8.0" from a localised
        // display string) HttpRequestMessage.Headers.UserAgent.ParseAdd
        // either throws or attaches "Version" as a separate product.
        // GitHub returns 403 if the User-Agent isn't well-formed.
        using var request = new HttpRequestMessage(HttpMethod.Get, "https://example.test/");
        request.Headers.UserAgent.ParseAdd(UpdateCheckService.UserAgent);

        Assert.Single(request.Headers.UserAgent);
        var product = request.Headers.UserAgent.First();
        Assert.NotNull(product.Product);
        Assert.Equal("InstallerClean", product.Product!.Name);
        Assert.False(string.IsNullOrEmpty(product.Product.Version));
        Assert.DoesNotContain(' ', product.Product.Version);
    }

    [Fact]
    public void JsonParseOptions_caps_depth_at_8()
    {
        // The GitHub Releases response is parsed by the elevated
        // process. MaxDepth=8 bounds exposure to pathologically nested
        // JSON; the 256 KiB MaxResponseContentBufferSize is the load-
        // bearing defence, this is hardening. Pinned so a refactor that
        // drops the JsonDocumentOptions and falls back to the BCL
        // default 64 fails CI rather than silently widening the cap.
        Assert.Equal(8, UpdateCheckService.JsonParseOptions.MaxDepth);
    }

    [Fact]
    public void ReleasesPageUrl_is_this_project_over_https()
    {
        // Every "there is an update" click lands here, from an elevated
        // process, so the constant itself is the whole of the destination's
        // trust: no response field feeds it and there is nothing to
        // validate at runtime. Pinned so a typo or an http:// slip in a
        // later edit fails CI rather than the browser.
        Assert.Equal(
            "https://github.com/no-faff/InstallerClean/releases/latest",
            UpdateCheckService.ReleasesPageUrl);
    }

    /// <summary>
    /// The automatic check meets an unreachable api.github.com on every
    /// launch of an offline, air-gapped or egress-filtered machine, none of
    /// which is a fault. Writing there would put a stack trace per run into
    /// a file called crash.log on a machine where nothing crashed.
    /// </summary>
    [Fact]
    public async Task Automatic_check_writes_nothing_when_the_name_does_not_resolve()
    {
        var (service, logged) = Build(() => throw new HttpRequestException("No such host is known."));

        var result = await service.CheckAsync(UpdateCheckOrigin.Automatic);

        Assert.Equal(new CheckFailed(UpdateCheckFailureReason.NetworkUnavailable), result);
        Assert.Empty(logged);
    }

    [Fact]
    public async Task Manual_check_records_a_name_that_does_not_resolve()
    {
        var (service, logged) = Build(() => throw new HttpRequestException("No such host is known."));

        var result = await service.CheckAsync(UpdateCheckOrigin.Manual);

        Assert.Equal(new CheckFailed(UpdateCheckFailureReason.NetworkUnavailable), result);
        // The user pressed the button and is reading a dialog about it; the
        // log is what that dialog's one localised sentence stands on.
        Assert.Single(logged);
    }

    [Fact]
    public async Task Automatic_check_writes_no_breadcrumb_for_a_refused_status()
    {
        var (service, logged) = Build(RateLimited);

        var result = await service.CheckAsync(UpdateCheckOrigin.Automatic);

        Assert.Equal(new CheckFailed(UpdateCheckFailureReason.ServerError), result);
        Assert.Empty(logged);
    }

    [Fact]
    public async Task Manual_check_records_the_status_and_the_rate_limit_headers()
    {
        var (service, logged) = Build(RateLimited);

        var result = await service.CheckAsync(UpdateCheckOrigin.Manual);

        Assert.Equal(new CheckFailed(UpdateCheckFailureReason.ServerError), result);
        var entry = Assert.Single(logged);
        // Separating GitHub's own 60/hour limit from a proxy answering 403 in
        // its place is the whole point of the breadcrumb, and the headers are
        // what separates them.
        Assert.Contains("403", entry.Message);
        Assert.Contains("x-ratelimit-remaining=0", entry.Message);
    }

    [Fact]
    public async Task Automatic_check_writes_nothing_when_the_answer_is_not_json()
    {
        var (service, logged) = Build(CaptivePortal);

        var result = await service.CheckAsync(UpdateCheckOrigin.Automatic);

        Assert.Equal(new CheckFailed(UpdateCheckFailureReason.ResponseParseError), result);
        Assert.Empty(logged);
    }

    [Fact]
    public async Task Manual_check_records_an_answer_that_is_not_json()
    {
        var (service, logged) = Build(CaptivePortal);

        var result = await service.CheckAsync(UpdateCheckOrigin.Manual);

        Assert.Equal(new CheckFailed(UpdateCheckFailureReason.ResponseParseError), result);
        Assert.Single(logged);
    }

    /// <summary>
    /// The catch-all is deliberately ungated: everything the origin silences
    /// is a network the app was built to meet, and what reaches here is not.
    /// </summary>
    [Theory]
    [InlineData(UpdateCheckOrigin.Automatic)]
    [InlineData(UpdateCheckOrigin.Manual)]
    public async Task An_unexpected_failure_is_recorded_whichever_check_hit_it(UpdateCheckOrigin origin)
    {
        var (service, logged) = Build(() => throw new InvalidOperationException("not a network failure"));

        var result = await service.CheckAsync(origin);

        Assert.Equal(new CheckFailed(UpdateCheckFailureReason.Unknown), result);
        Assert.Single(logged);
    }
}
