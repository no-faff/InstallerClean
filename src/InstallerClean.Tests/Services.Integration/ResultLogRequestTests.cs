using System.Net;
using System.Net.Http;
using System.Net.Sockets;
using System.Text;
using InstallerClean.Services;

namespace InstallerClean.Tests.Services.Integration;

/// <summary>
/// The opt-in report's POST, as it goes out on the wire: <see cref="ResultLogService.Request"/>
/// sent through a default HttpClient, the handler the service's own uses, to a server on
/// the loopback address that reads the request whole, head and body, and answers 200.
/// </summary>
public class ResultLogRequestTests
{
    private const string Body = "{\"a\":1}";

    [Fact]
    public async Task The_report_goes_with_its_headers_and_asks_no_leave_first()
    {
        var (head, body) = await Send(url => ResultLogService.Request(Body, url));

        Assert.StartsWith("POST /api/result-log HTTP/1.1\r\n", head, StringComparison.Ordinal);
        Assert.Contains("\r\nUser-Agent: InstallerClean/", head, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("\r\nContent-Type: application/json\r\n", head, StringComparison.OrdinalIgnoreCase);
        Assert.Contains($"\r\nContent-Length: {Encoding.UTF8.GetByteCount(Body)}\r\n", head, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("\r\nExpect:", head, StringComparison.OrdinalIgnoreCase);
        Assert.Equal(Body, body);
    }

#if NETFRAMEWORK
    // The control: on .NET Framework the same POST, with nothing said about a continue,
    // goes out asking the server's leave, which is what the request above is made against.
    [Fact]
    public async Task Without_the_request_saying_so_the_framework_asks_leave_first()
    {
        var (head, _) = await Send(url =>
        {
            var request = new HttpRequestMessage(HttpMethod.Post, url);
            request.Content = new StringContent(Body, Encoding.UTF8);
            return request;
        });

        Assert.Contains("\r\nExpect: 100-continue\r\n", head, StringComparison.OrdinalIgnoreCase);
    }
#endif

    /// <summary>
    /// Sends the request <paramref name="build"/> makes for a loopback address of its own
    /// port, so no two sends share a service point, and gives back the head and the body
    /// the server read.
    /// </summary>
    private static async Task<(string Head, string Body)> Send(Func<string, HttpRequestMessage> build)
    {
        var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        try
        {
            var url = $"http://127.0.0.1:{((IPEndPoint)listener.LocalEndpoint).Port}/api/result-log";
            var served = Task.Run(() => ServeOne(listener));

            using (var client = new HttpClient { Timeout = TimeSpan.FromSeconds(30) })
            using (var request = build(url))
            {
                var sent = client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead);
                // A server that fails is the cause to report, not the client's wait for it.
                if (await Task.WhenAny(served, sent) == served && served.IsFaulted)
                    await served;
                using var response = await sent;
                Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            }

            return await served;
        }
        finally
        {
            listener.Stop();
        }
    }

    /// <summary>
    /// Reads one request whole, by its Content-Length, answers 200 with no body, and gives
    /// back its head and body. Where the request asks leave first it is given it. Reading
    /// the body before answering means the socket closes with nothing unread, which Windows
    /// would otherwise answer with a reset.
    /// </summary>
    private static async Task<(string Head, string Body)> ServeOne(TcpListener listener)
    {
        using var client = await listener.AcceptTcpClientAsync();
        using var connection = client.GetStream();
        var received = new List<byte>();
        var buffer = new byte[4096];
        int headEnd;
        while ((headEnd = HeadEnd(received)) < 0)
        {
            var read = await connection.ReadAsync(buffer, 0, buffer.Length);
            if (read == 0)
                throw new IOException("The connection closed before the request's head ended: "
                    + Encoding.ASCII.GetString(received.ToArray()));
            received.AddRange(buffer.Take(read));
        }

        var head = Encoding.ASCII.GetString(received.ToArray(), 0, headEnd + 2);
        var length = ContentLength(head);
        if (head.IndexOf("\r\nExpect: 100-continue\r\n", StringComparison.OrdinalIgnoreCase) >= 0)
        {
            var proceed = Encoding.ASCII.GetBytes("HTTP/1.1 100 Continue\r\n\r\n");
            await connection.WriteAsync(proceed, 0, proceed.Length);
        }

        var body = received.Skip(headEnd + 4).ToList();
        while (body.Count < length)
        {
            var read = await connection.ReadAsync(buffer, 0, buffer.Length);
            if (read == 0)
                throw new IOException($"The connection closed after {body.Count} of the body's {length} bytes.");
            body.AddRange(buffer.Take(read));
        }

        var answer = Encoding.ASCII.GetBytes("HTTP/1.1 200 OK\r\nContent-Length: 0\r\nConnection: close\r\n\r\n");
        await connection.WriteAsync(answer, 0, answer.Length);
        await connection.FlushAsync();
        return (head, Encoding.UTF8.GetString(body.ToArray()));
    }

    private static int HeadEnd(List<byte> received)
    {
        for (var i = 0; i + 3 < received.Count; i++)
            if (received[i] == '\r' && received[i + 1] == '\n' && received[i + 2] == '\r' && received[i + 3] == '\n')
                return i;
        return -1;
    }

    private static int ContentLength(string head)
    {
        foreach (var line in head.Split(new[] { "\r\n" }, StringSplitOptions.RemoveEmptyEntries))
            if (line.StartsWith("Content-Length:", StringComparison.OrdinalIgnoreCase))
                return int.Parse(line.Substring("Content-Length:".Length).Trim(), System.Globalization.CultureInfo.InvariantCulture);
        throw new InvalidDataException("The request names no Content-Length: " + head);
    }
}
