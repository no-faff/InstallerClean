using System.Net;
using System.Net.Http;
using InstallerClean.Services;
using Xunit;

namespace InstallerClean.Tests.Services;

/// <summary>
/// UpdateCheckService unit tests. The check reads the newest version out of
/// the Location header GitHub answers the releases page with, so what is
/// covered is every shape that answer can take: the redirect it expects, the
/// redirects it must refuse, the statuses that are not redirects at all, and
/// what each origin is then allowed to write to crash.log. Also pinned are
/// the two settings no stub handler can exercise (redirects off, cookies
/// off), the User-Agent (must parse through
/// HttpRequestMessage.Headers.UserAgent) and the releases-page constant.
/// Everything but those runs against a stub handler and a collecting sink,
/// so the failures are deterministic and nothing is written under the
/// runner's profile.
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

    private static HttpResponseMessage Refused()
    {
        // A refusal carrying the throttling headers the breadcrumb reports,
        // so the assertion below reads a status plus what arrived beside it
        // rather than a status alone.
        var response = new HttpResponseMessage(HttpStatusCode.Forbidden);
        response.Headers.Add("x-ratelimit-remaining", "0");
        response.Headers.Add("x-ratelimit-reset", "1750000000");
        return response;
    }

    // A captive portal's login page: 200 and a body, where the releases page
    // would have redirected.
    private static HttpResponseMessage CaptivePortal() =>
        new(HttpStatusCode.OK) { Content = new StringContent("<html><body>Sign in</body></html>") };

    /// <summary>
    /// The answer the check is built to read: GitHub's 302 off
    /// /releases/latest onto the newest release's own tag page.
    /// </summary>
    private static HttpResponseMessage RedirectToTag(string tag)
    {
        var response = new HttpResponseMessage(HttpStatusCode.Found);
        response.Headers.Location =
            new Uri($"https://github.com/no-faff/InstallerClean/releases/tag/{tag}");
        return response;
    }

    private static HttpResponseMessage RedirectTo(string? location)
    {
        var response = new HttpResponseMessage(HttpStatusCode.Found);
        if (location is not null) response.Headers.Location = new Uri(location, UriKind.RelativeOrAbsolute);
        return response;
    }

    private static (UpdateCheckService Service, List<Exception> Logged) Build(
        Func<HttpResponseMessage> respond)
    {
        var logged = new List<Exception>();
        return (new UpdateCheckService(new StubHandler(respond), logged.Add), logged);
    }

    [Fact]
    public void UserAgent_parses_as_a_well_formed_HTTP_product()
    {
        // RFC 9110 product = token "/" token. A version token carrying a
        // space (e.g. "Version 1.8.0" from a localised display string) does
        // not throw: ParseAdd reads "Version" as the version and "1.8.0" as
        // a second product, so a malformed constant reaches the wire looking
        // parsed and the product count is what catches it. An empty version
        // token does throw, out of CheckAsync's try and into the catch-all
        // that writes crash.log.
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
    public void The_shipping_handler_does_not_follow_redirects()
    {
        // The load-bearing setting, and the one the handler seam these
        // tests run through structurally cannot cover: every other test
        // supplies its own handler, so all of them pass either way. With
        // following on, HttpClient swallows the 302 and hands back the tag
        // page's HTML with a 200, which the check reads as something
        // answering in GitHub's place and reports as a failure on every
        // run.
        using var handler = UpdateCheckService.CreateShippingHandler();

        Assert.False(handler.AllowAutoRedirect);
        // Nothing here needs github.com's session cookie, and an elevated
        // process is the last one that should be keeping it.
        Assert.False(handler.UseCookies);
    }

    [Fact]
    public async Task A_newer_tag_is_reported_as_an_update()
    {
        // Far enough ahead that the assembly version can never overtake it.
        var (service, logged) = Build(() => RedirectToTag("v99.0.0"));

        var result = await service.CheckAsync(UpdateCheckOrigin.Manual);

        var available = Assert.IsType<UpdateAvailable>(result);
        Assert.Equal("99.0.0", available.LatestVersion);
        Assert.Empty(logged);
    }

    [Fact]
    public async Task An_older_tag_is_reported_as_up_to_date()
    {
        var (service, logged) = Build(() => RedirectToTag("v0.0.1"));

        var result = await service.CheckAsync(UpdateCheckOrigin.Manual);

        Assert.IsType<UpToDate>(result);
        Assert.Empty(logged);
    }

    [Fact]
    public async Task A_tag_without_the_leading_v_is_still_read()
    {
        // The project has tagged with the v since 1.0.0; accepting both
        // costs one comparison and outlives whoever tags the next one.
        var (service, _) = Build(() => RedirectToTag("99.0.0"));

        var result = await service.CheckAsync(UpdateCheckOrigin.Manual);

        Assert.Equal("99.0.0", Assert.IsType<UpdateAvailable>(result).LatestVersion);
    }

    /// <summary>
    /// The version shown to the user may only come off the releases path of
    /// the host the app itself links to. A redirect elsewhere is refused
    /// rather than parsed, whatever it carries.
    /// </summary>
    [Theory]
    [InlineData("https://example.test/no-faff/InstallerClean/releases/tag/v99.0.0")]
    [InlineData("http://github.com/no-faff/InstallerClean/releases/tag/v99.0.0")]
    [InlineData("https://github.com/someone-else/Other/releases/tag/v99.0.0")]
    [InlineData("https://github.com/no-faff/InstallerClean/releases")]
    [InlineData("https://github.com/no-faff/InstallerClean/releases/tag/")]
    [InlineData("https://github.com/no-faff/InstallerClean/releases/tag/v99.0.0/assets")]
    [InlineData(null)]
    public async Task A_redirect_the_check_cannot_trust_is_refused(string? location)
    {
        var (service, logged) = Build(() => RedirectTo(location));

        var result = await service.CheckAsync(UpdateCheckOrigin.Manual);

        Assert.Equal(new CheckFailed(UpdateCheckFailureReason.ResponseParseError), result);
        // The shape that would report GitHub having changed how /latest
        // answers, which is the one assumption the check rests on.
        Assert.Single(logged);
    }

    [Fact]
    public async Task A_redirect_to_something_that_is_not_a_version_is_refused()
    {
        var (service, logged) = Build(() => RedirectToTag("nightly"));

        var result = await service.CheckAsync(UpdateCheckOrigin.Manual);

        Assert.Equal(new CheckFailed(UpdateCheckFailureReason.ResponseParseError), result);
        Assert.Contains("nightly", Assert.Single(logged).Message);
    }

    [Fact]
    public async Task Automatic_check_writes_nothing_for_a_redirect_it_cannot_trust()
    {
        var (service, logged) = Build(() => RedirectTo(null));

        var result = await service.CheckAsync(UpdateCheckOrigin.Automatic);

        Assert.Equal(new CheckFailed(UpdateCheckFailureReason.ResponseParseError), result);
        Assert.Empty(logged);
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
    /// The automatic check meets an unreachable github.com on every launch of
    /// an offline, air-gapped or egress-filtered machine, none of which is a
    /// fault. Writing there would put a stack trace per run into a file
    /// called crash.log on a machine where nothing crashed.
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
        var (service, logged) = Build(Refused);

        var result = await service.CheckAsync(UpdateCheckOrigin.Automatic);

        Assert.Equal(new CheckFailed(UpdateCheckFailureReason.ServerError), result);
        Assert.Empty(logged);
    }

    [Fact]
    public async Task Manual_check_records_the_status_and_any_throttling_headers()
    {
        var (service, logged) = Build(Refused);

        var result = await service.CheckAsync(UpdateCheckOrigin.Manual);

        Assert.Equal(new CheckFailed(UpdateCheckFailureReason.ServerError), result);
        var entry = Assert.Single(logged);
        // A status on its own cannot tell a refusal by GitHub apart from a
        // refusal by whatever sits between; the headers it arrived with can,
        // which is the whole point of the breadcrumb.
        Assert.Contains("403", entry.Message);
        Assert.Contains("x-ratelimit-remaining=0", entry.Message);
    }

    [Fact]
    public async Task Automatic_check_writes_nothing_when_the_answer_is_not_a_redirect()
    {
        var (service, logged) = Build(CaptivePortal);

        var result = await service.CheckAsync(UpdateCheckOrigin.Automatic);

        Assert.Equal(new CheckFailed(UpdateCheckFailureReason.ResponseParseError), result);
        Assert.Empty(logged);
    }

    [Fact]
    public async Task Manual_check_records_an_answer_that_is_not_a_redirect()
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
