using System.Net.Http;
using InstallerClean.Services;
using Xunit;

namespace InstallerClean.Tests.Services;

/// <summary>
/// UpdateCheckService unit tests. CheckAsync itself isn't covered
/// here because it depends on a live HttpClient against GitHub; these
/// tests pin the User-Agent (must parse through
/// HttpRequestMessage.Headers.UserAgent - a localised display string
/// in the version slot causes GitHub to return 403) and the
/// release-URL trust check (accept only this project's own releases
/// path).
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

    [Theory]
    [InlineData("https://github.com/no-faff/InstallerClean/releases/tag/v1.8.0", true)]
    [InlineData("https://github.com/no-faff/InstallerClean/releases/latest", true)]
    [InlineData("https://github.com/no-faff/InstallerClean/releases/", true)]
    [InlineData("https://GITHUB.COM/no-faff/InstallerClean/Releases/tag/v1.8.0", true)]
    [InlineData("https://github.com/No-Faff/InstallerClean/releases/tag/v1.8.0", true)]
    [InlineData("https://github.com/no-faff/InstallerClean", false)]
    [InlineData("https://github.com/no-faff/OtherProject/releases/v1.0", false)]
    [InlineData("https://github.com.attacker.example/no-faff/InstallerClean/releases/", false)]
    [InlineData("http://github.com/no-faff/InstallerClean/releases/", false)]
    [InlineData("javascript:alert(1)", false)]
    [InlineData("", false)]
    [InlineData(null, false)]
    public void IsTrustedReleaseUrl_accepts_only_project_releases_paths(string? url, bool expected)
    {
        Assert.Equal(expected, UpdateCheckService.IsTrustedReleaseUrl(url));
    }
}
