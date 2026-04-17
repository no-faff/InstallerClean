using System.Net;
using System.Net.Http;
using InstallerClean.Services;

namespace InstallerClean.Tests.Services;

public class UpdateCheckServiceDisposeTests
{
    [Fact]
    public void Parameterless_constructor_produces_disposable()
    {
        var svc = new UpdateCheckService();

        Assert.IsAssignableFrom<IDisposable>(svc);
        svc.Dispose();
    }

    [Fact]
    public async Task After_dispose_GetLatestVersionAsync_returns_failed_without_throwing()
    {
        var svc = new UpdateCheckService();
        svc.Dispose();

        var result = await svc.GetLatestVersionAsync();

        Assert.True(result.CheckFailed);
    }

    [Fact]
    public void Double_dispose_is_safe()
    {
        var svc = new UpdateCheckService();
        svc.Dispose();

        var ex = Record.Exception(() => svc.Dispose());

        Assert.Null(ex);
    }

    [Fact]
    public async Task Injected_HttpClient_is_not_disposed_by_service()
    {
        var handler = new CountingHandler();
        var client = new HttpClient(handler);
        using (var svc = new UpdateCheckService(client))
        {
            svc.Dispose();
        }

        // If the client were disposed, sending a request would throw.
        // Invoke the handler directly to stay hermetic.
        var response = await client.SendAsync(new HttpRequestMessage(HttpMethod.Get, "http://localhost/"));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(1, handler.CallCount);
    }

    private sealed class CountingHandler : HttpMessageHandler
    {
        public int CallCount { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request, CancellationToken cancellationToken)
        {
            CallCount++;
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK));
        }
    }
}
