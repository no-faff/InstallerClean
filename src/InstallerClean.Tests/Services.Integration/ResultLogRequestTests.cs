using System.Net;
using System.Net.Http;
using System.Net.Sockets;
using System.Text;
using InstallerClean.Services;

namespace InstallerClean.Tests.Services.Integration;

/// <summary>
/// The opt-in report's POST, as it goes out on the wire: <see cref="ResultLogService.Request"/>
/// sent through a default HttpClient, the handler the service's own uses, to a server on
/// the loopback address that records the request's head and answers 200.
/// </summary>
public class ResultLogRequestTests
{
    [Fact]
    public async Task The_report_goes_with_its_headers_and_asks_no_leave_first()
    {
        var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        try
        {
            var url = $"http://127.0.0.1:{((IPEndPoint)listener.LocalEndpoint).Port}/api/result-log";
            var served = Task.Run(() => ServeOne(listener));

            using (var client = new HttpClient())
            using (var request = ResultLogService.Request("{\"a\":1}", url))
            using (var response = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead))
                Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            var head = await served;
            Assert.StartsWith("POST /api/result-log HTTP/1.1\r\n", head, StringComparison.Ordinal);
            Assert.Contains("\r\nContent-Type: application/json\r\n", head, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("\r\nExpect:", head, StringComparison.OrdinalIgnoreCase);
        }
        finally
        {
            listener.Stop();
        }
    }

    /// <summary>Reads one request's head, answers 200 with no body, and gives back the head.</summary>
    private static async Task<string> ServeOne(TcpListener listener)
    {
        using var client = await listener.AcceptTcpClientAsync();
        using var connection = client.GetStream();
        var head = new StringBuilder();
        var buffer = new byte[4096];
        while (head.ToString().IndexOf("\r\n\r\n", StringComparison.Ordinal) < 0)
        {
            var read = await connection.ReadAsync(buffer, 0, buffer.Length);
            if (read == 0) break;
            head.Append(Encoding.ASCII.GetString(buffer, 0, read));
        }

        var answer = Encoding.ASCII.GetBytes("HTTP/1.1 200 OK\r\nContent-Length: 0\r\nConnection: close\r\n\r\n");
        await connection.WriteAsync(answer, 0, answer.Length);
        await connection.FlushAsync();
        var text = head.ToString();
        return text.Substring(0, text.IndexOf("\r\n\r\n", StringComparison.Ordinal) + 2);
    }
}
