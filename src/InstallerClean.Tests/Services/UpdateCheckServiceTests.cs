using System.Net.Http;
using InstallerClean.Services;
using Xunit;

namespace InstallerClean.Tests.Services;

/// <summary>
/// UpdateCheckService unit tests. CheckAsync itself isn't covered
/// here because it depends on a live HttpClient against GitHub; these
/// tests pin the User-Agent (must parse through
/// HttpRequestMessage.Headers.UserAgent - a localised display string
/// in the version slot causes GitHub to return 403) and the JSON
/// depth cap.
/// </summary>
public class UpdateCheckServiceTests
{
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
}
