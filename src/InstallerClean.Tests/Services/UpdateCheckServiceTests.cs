using System.Net;
using System.Net.Http;
using InstallerClean.Services;

namespace InstallerClean.Tests.Services;

public class UpdateCheckServiceTests
{
    private static UpdateCheckService BuildService(HttpStatusCode status, string responseBody)
    {
        var handler = new FakeHandler(status, responseBody);
        var client = new HttpClient(handler);
        return new UpdateCheckService(client);
    }

    private static UpdateCheckService BuildService(Exception throwFromSend)
    {
        var handler = new FakeHandler(throwFromSend);
        var client = new HttpClient(handler);
        return new UpdateCheckService(client);
    }

    [Fact]
    public async Task GetLatestVersionAsync_returns_failed_when_http_request_throws()
    {
        var svc = BuildService(new HttpRequestException("No network"));

        var result = await svc.GetLatestVersionAsync();

        Assert.True(result.CheckFailed);
        Assert.Null(result.LatestVersion);
    }

    [Fact]
    public async Task GetLatestVersionAsync_returns_failed_on_http_500()
    {
        var svc = BuildService(HttpStatusCode.InternalServerError, "oops");

        var result = await svc.GetLatestVersionAsync();

        Assert.True(result.CheckFailed);
    }

    [Fact]
    public async Task GetLatestVersionAsync_returns_failed_when_json_is_malformed()
    {
        var svc = BuildService(HttpStatusCode.OK, "not json {");

        var result = await svc.GetLatestVersionAsync();

        Assert.True(result.CheckFailed);
    }

    [Fact]
    public async Task GetLatestVersionAsync_returns_failed_when_tag_name_missing()
    {
        var svc = BuildService(HttpStatusCode.OK, """{"name":"release"}""");

        var result = await svc.GetLatestVersionAsync();

        Assert.True(result.CheckFailed);
    }

    [Fact]
    public async Task GetLatestVersionAsync_returns_failed_when_tag_unparseable()
    {
        var svc = BuildService(HttpStatusCode.OK, """{"tag_name":"release-candidate"}""");

        var result = await svc.GetLatestVersionAsync();

        Assert.True(result.CheckFailed);
    }

    [Fact]
    public async Task GetLatestVersionAsync_returns_up_to_date_when_remote_not_greater()
    {
        // v0.0.1 is lower than any realistic assembly version, so this
        // should report "up to date" (not an available update).
        var svc = BuildService(HttpStatusCode.OK, """{"tag_name":"v0.0.1"}""");

        var result = await svc.GetLatestVersionAsync();

        Assert.False(result.CheckFailed);
        Assert.Null(result.LatestVersion);
    }

    [Fact]
    public async Task GetLatestVersionAsync_returns_available_when_remote_greater()
    {
        // v999.0.0 is higher than any realistic assembly version, so this
        // should report an available update.
        var svc = BuildService(HttpStatusCode.OK, """{"tag_name":"v999.0.0"}""");

        var result = await svc.GetLatestVersionAsync();

        Assert.False(result.CheckFailed);
        Assert.Equal("v999.0.0", result.LatestVersion);
    }

    private sealed class FakeHandler : HttpMessageHandler
    {
        private readonly HttpStatusCode _status;
        private readonly string _body;
        private readonly Exception? _throw;

        public FakeHandler(HttpStatusCode status, string body)
        {
            _status = status;
            _body = body;
        }

        public FakeHandler(Exception throwFromSend)
        {
            _throw = throwFromSend;
            _status = HttpStatusCode.OK;
            _body = string.Empty;
        }

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request, CancellationToken cancellationToken)
        {
            if (_throw is not null)
                throw _throw;

            var response = new HttpResponseMessage(_status)
            {
                Content = new StringContent(_body)
            };
            return Task.FromResult(response);
        }
    }
}
