using System.Buffers;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace InstallerClean.Tests.Compatibility;

/// <summary>
/// The stream, reader and response members the app calls as .NET 10 has them: memory reads
/// and writes that go through a stream's own array members with the caller's token, a read
/// that fills its buffer or reports the end, a read to the end that a cancel stops between
/// blocks, and a response body whose read ends when its token is cancelled.
/// </summary>
public class FrameworkStreamsTests
{
    [Fact]
    public async Task ReadAsync_into_an_array_goes_through_the_stream_own_array_read_with_the_token()
    {
        using var cts = new CancellationTokenSource();
        var stream = new ArrayOnlyStream(Encoding.ASCII.GetBytes("abcdef"));
        var buffer = new byte[8];

        var read = await stream.ReadAsync(buffer.AsMemory(2, 4), cts.Token);

        Assert.Equal(4, read);
        Assert.Equal("abcd", Encoding.ASCII.GetString(buffer, 2, 4));
        Assert.Equal((2, 4, cts.Token), stream.LastRead);
    }

    [Fact]
    public async Task ReadAsync_into_memory_over_no_array_reads_through_a_copy()
    {
        var stream = new ArrayOnlyStream(Encoding.ASCII.GetBytes("abcdef"));
        using var owner = new NativeMemory(3);

        var read = await stream.ReadAsync(owner.Memory, CancellationToken.None);

        Assert.Equal(3, read);
        Assert.Equal("abc", Encoding.ASCII.GetString(owner.Memory.Span.ToArray()));
        Assert.Equal(0, stream.LastRead.Offset);
    }

    [Fact]
    public async Task WriteAsync_from_an_array_goes_through_the_stream_own_array_write_with_the_token()
    {
        using var cts = new CancellationTokenSource();
        var stream = new ArrayOnlyStream([]);

        await stream.WriteAsync(Encoding.ASCII.GetBytes("xyz"), cts.Token);

        Assert.Equal("xyz", Encoding.ASCII.GetString(stream.Written.ToArray()));
        Assert.Equal(cts.Token, stream.LastWriteToken);
    }

    [Fact]
    public async Task WriteAsync_from_memory_over_no_array_writes_a_copy()
    {
        var stream = new ArrayOnlyStream([]);
        using var owner = new NativeMemory(2);
        "hi"u8.CopyTo(owner.Memory.Span);

        await stream.WriteAsync((ReadOnlyMemory<byte>)owner.Memory, CancellationToken.None);

        Assert.Equal("hi", Encoding.ASCII.GetString(stream.Written.ToArray()));
    }

    [Fact]
    public void ReadExactly_reads_again_until_the_buffer_is_full()
    {
        var stream = new ArrayOnlyStream([(byte)'M', (byte)'Z', 0x90, 0x00, (byte)'r'], maxPerRead: 1);
        var buffer = new byte[4];

        stream.ReadExactly(buffer);

        Assert.Equal(new byte[] { (byte)'M', (byte)'Z', 0x90, 0x00 }, buffer);
    }

    [Fact]
    public void ReadExactly_past_the_end_throws_EndOfStreamException_with_NET_10_message()
    {
        var stream = new ArrayOnlyStream(Encoding.ASCII.GetBytes("MZ"), maxPerRead: 1);

        var thrown = Assert.Throws<EndOfStreamException>(() => stream.ReadExactly(new byte[3]));

        Assert.Equal("Unable to read beyond the end of the stream.", thrown.Message);
    }

    [Fact]
    public void ReadExactly_of_nothing_reads_nothing()
    {
        var stream = new ArrayOnlyStream([]);

        stream.ReadExactly(Span<byte>.Empty);

        Assert.Equal(0, stream.Reads);
    }

    [Fact]
    public async Task ReadToEndAsync_reads_all_of_a_long_text()
    {
        var text = new string('a', 10_000) + "é" + new string('b', 5_000);
        using var reader = new StreamReader(new MemoryStream(Encoding.UTF8.GetBytes(text)), Encoding.UTF8);

        Assert.Equal(text, await reader.ReadToEndAsync(CancellationToken.None));
    }

    [Fact]
    public async Task ReadToEndAsync_with_a_cancelled_token_throws_TaskCanceledException_carrying_it()
    {
        using var cts = new CancellationTokenSource();
        cts.Cancel();
        using var reader = new StreamReader(new MemoryStream(Encoding.UTF8.GetBytes("{}")), Encoding.UTF8);

        var thrown = await Assert.ThrowsAsync<TaskCanceledException>(() => reader.ReadToEndAsync(cts.Token));

        Assert.Equal(cts.Token, thrown.CancellationToken);
    }

    [Fact]
    public async Task ReadToEndAsync_stops_at_a_cancel_that_comes_part_way()
    {
        using var cts = new CancellationTokenSource();
        var source = new CancellingStream(Encoding.UTF8.GetBytes(new string('a', 100_000)), cts);
        using var reader = new StreamReader(source, Encoding.UTF8);

        var thrown = await Assert.ThrowsAsync<TaskCanceledException>(() => reader.ReadToEndAsync(cts.Token));

        Assert.Equal(cts.Token, thrown.CancellationToken);
    }

    [Fact]
    public async Task A_response_body_hands_back_the_same_stream_on_every_call()
    {
        using var server = StallingServer.Start(bodyLength: 4, sent: 4);
        using var client = new HttpClient();
        using var response = await client.GetAsync(server.Address, HttpCompletionOption.ResponseHeadersRead);

        var first = await response.Content.ReadAsStreamAsync(CancellationToken.None);
        var second = await response.Content.ReadAsStreamAsync(CancellationToken.None);

        Assert.Same(first, second);
    }

    [Fact]
    public async Task A_response_body_reads_what_the_server_sends()
    {
        using var server = StallingServer.Start(bodyLength: 4, sent: 4);
        using var client = new HttpClient();
        using var response = await client.GetAsync(server.Address, HttpCompletionOption.ResponseHeadersRead);
        using var body = await response.Content.ReadAsStreamAsync(CancellationToken.None);
        var buffer = new byte[16];

        var total = 0;
        int read;
        while ((read = await body.ReadAsync(buffer.AsMemory(total), CancellationToken.None)) > 0)
            total += read;

        Assert.Equal("MZab", Encoding.ASCII.GetString(buffer, 0, total));
    }

    [Fact]
    public async Task A_response_body_read_the_server_stops_answering_ends_when_its_token_is_cancelled()
    {
        using var server = StallingServer.Start(bodyLength: 1_000_000, sent: 4);
        using var client = new HttpClient { Timeout = Timeout.InfiniteTimeSpan };
        using var response = await client.GetAsync(server.Address, HttpCompletionOption.ResponseHeadersRead);
        using var body = await response.Content.ReadAsStreamAsync(CancellationToken.None);
        var buffer = new byte[4096];
        var got = 0;
        while (got < 4)
            got += await body.ReadAsync(buffer.AsMemory(got), CancellationToken.None);
        using var stall = new CancellationTokenSource(TimeSpan.FromMilliseconds(200));

        var read = body.ReadAsync(buffer, stall.Token).AsTask();
        var finished = await Task.WhenAny(read, Task.Delay(TimeSpan.FromSeconds(10)));

        Assert.Same(read, finished);
        var thrown = await Assert.ThrowsAsync<TaskCanceledException>(() => read);
        Assert.Equal(stall.Token, thrown.CancellationToken);
        Assert.Equal("The operation was canceled.", thrown.Message);
    }

    [Fact]
    public async Task A_response_body_read_given_a_cancelled_token_fails_before_it_reads()
    {
        using var server = StallingServer.Start(bodyLength: 4, sent: 4);
        using var client = new HttpClient();
        using var response = await client.GetAsync(server.Address, HttpCompletionOption.ResponseHeadersRead);
        using var body = await response.Content.ReadAsStreamAsync(CancellationToken.None);
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        var thrown = await Assert.ThrowsAnyAsync<OperationCanceledException>(() => body.ReadAsync(new byte[4], cts.Token).AsTask());

        Assert.Equal(cts.Token, thrown.CancellationToken);
    }

    [Fact]
    public async Task A_body_whose_stream_looks_at_a_token_only_before_a_read_still_ends_the_read_when_it_is_cancelled()
    {
        var waiting = new TokenIgnoringStream();
        using var content = new StreamingContent(waiting);
        using var body = await content.ReadAsStreamAsync(CancellationToken.None);
        using var stall = new CancellationTokenSource(TimeSpan.FromMilliseconds(200));

        var read = body.ReadAsync(new byte[16], stall.Token).AsTask();
        var finished = await Task.WhenAny(read, Task.Delay(TimeSpan.FromSeconds(10)));

        Assert.Same(read, finished);
        var thrown = await Assert.ThrowsAsync<TaskCanceledException>(() => read);
        Assert.Equal(stall.Token, thrown.CancellationToken);
        Assert.Same(waiting.Disposed.Task, await Task.WhenAny(waiting.Disposed.Task, Task.Delay(TimeSpan.FromSeconds(10))));
    }

    [Fact]
    public async Task Disposing_a_body_after_its_read_was_cancelled_throws_nothing_however_the_connection_closes()
    {
        var waiting = new TokenIgnoringStream { DisposeThrows = true };
        // Not disposed: disposing the content disposes the stream underneath itself, and only
        // the body's own disposes are counted here.
        var content = new StreamingContent(waiting);
        var body = await content.ReadAsStreamAsync(CancellationToken.None);
        using var stall = new CancellationTokenSource(TimeSpan.FromMilliseconds(100));

        var read = body.ReadAsync(new byte[16], stall.Token).AsTask();
        Assert.Same(read, await Task.WhenAny(read, Task.Delay(TimeSpan.FromSeconds(10))));
        await Assert.ThrowsAsync<TaskCanceledException>(() => read);
        body.Dispose();
        body.Dispose();

        Assert.Same(waiting.Disposed.Task, await Task.WhenAny(waiting.Disposed.Task, Task.Delay(TimeSpan.FromSeconds(10))));
        Assert.Equal(1, waiting.DisposeCount);
    }

    [Fact]
    public async Task A_body_read_that_fails_with_no_cancel_fails_as_it_did()
    {
        var failing = new TokenIgnoringStream();
        using var content = new StreamingContent(failing);
        using var body = await content.ReadAsStreamAsync(CancellationToken.None);
        using var cts = new CancellationTokenSource();

        var read = body.ReadAsync(new byte[16], cts.Token).AsTask();
        failing.Fail(new IOException("The connection was reset."));

        var thrown = await Assert.ThrowsAsync<IOException>(() => read);
        Assert.Equal("The connection was reset.", thrown.Message);
    }

    /// <summary>
    /// Content handing back <paramref name="stream"/> as its body, which is what a response's
    /// content does.
    /// </summary>
    private sealed class StreamingContent(Stream stream) : HttpContent
    {
        protected override Task SerializeToStreamAsync(Stream target, TransportContext? context) =>
            throw new NotSupportedException();

        protected override Task<Stream> CreateContentReadStreamAsync() => Task.FromResult(stream);

        protected override bool TryComputeLength(out long length)
        {
            length = 0;
            return false;
        }
    }

    /// <summary>
    /// A body whose read looks at its token only before it starts and then waits until the
    /// stream is disposed, which fails it with ObjectDisposedException, or until
    /// <see cref="Fail"/> is called, as .NET Framework's response stream does.
    /// </summary>
    private sealed class TokenIgnoringStream : Stream
    {
        private readonly TaskCompletionSource<int> _read = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public TaskCompletionSource<bool> Disposed { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public void Fail(Exception failure) => _read.TrySetException(failure);

        public bool DisposeThrows { get; init; }

        public int DisposeCount { get; private set; }

        public override bool CanRead => true;

        public override bool CanSeek => false;

        public override bool CanWrite => false;

        public override long Length => throw new NotSupportedException();

        public override long Position
        {
            get => throw new NotSupportedException();
            set => throw new NotSupportedException();
        }

        public override Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken) =>
            cancellationToken.IsCancellationRequested ? Task.FromCanceled<int>(cancellationToken) : _read.Task;

        public override int Read(byte[] buffer, int offset, int count) => throw new NotSupportedException();

        public override void Flush()
        {
        }

        public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();

        public override void SetLength(long value) => throw new NotSupportedException();

        public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();

        protected override void Dispose(bool disposing)
        {
            if (!disposing) return;

            DisposeCount++;
            _read.TrySetException(new ObjectDisposedException(nameof(TokenIgnoringStream)));
            Disposed.TrySetResult(true);
            if (DisposeThrows) throw new IOException("The connection was closed while it was read.");
        }
    }

    /// <summary>
    /// A stream that overrides only the array members, as the app's own test streams do,
    /// recording what each read and write was given.
    /// </summary>
    private sealed class ArrayOnlyStream(byte[] content, int maxPerRead = int.MaxValue) : Stream
    {
        private int _position;

        public (int Offset, int Count, CancellationToken Token) LastRead { get; private set; }

        public CancellationToken LastWriteToken { get; private set; }

        public MemoryStream Written { get; } = new();

        public int Reads { get; private set; }

        public override bool CanRead => true;

        public override bool CanSeek => false;

        public override bool CanWrite => true;

        public override long Length => throw new NotSupportedException();

        public override long Position
        {
            get => throw new NotSupportedException();
            set => throw new NotSupportedException();
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            Reads++;
            var n = Math.Min(Math.Min(count, maxPerRead), content.Length - _position);
            Array.Copy(content, _position, buffer, offset, n);
            _position += n;
            return n;
        }

        public override Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            LastRead = (offset, count, cancellationToken);
            return Task.FromResult(Read(buffer, offset, count));
        }

        public override Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            LastWriteToken = cancellationToken;
            Written.Write(buffer, offset, count);
            return Task.CompletedTask;
        }

        public override void Write(byte[] buffer, int offset, int count) => Written.Write(buffer, offset, count);

        public override void Flush()
        {
        }

        public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();

        public override void SetLength(long value) => throw new NotSupportedException();
    }

    /// <summary>A stream that cancels <paramref name="cts"/> during its first read.</summary>
    private sealed class CancellingStream(byte[] content, CancellationTokenSource cts) : Stream
    {
        private readonly MemoryStream _inner = new(content);

        public override bool CanRead => true;

        public override bool CanSeek => false;

        public override bool CanWrite => false;

        public override long Length => throw new NotSupportedException();

        public override long Position
        {
            get => throw new NotSupportedException();
            set => throw new NotSupportedException();
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            cts.Cancel();
            return _inner.Read(buffer, offset, Math.Min(count, 512));
        }

        public override Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            if (cancellationToken.IsCancellationRequested) return Task.FromCanceled<int>(cancellationToken);
            return Task.FromResult(Read(buffer, offset, count));
        }

        public override void Flush()
        {
        }

        public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();

        public override void SetLength(long value) => throw new NotSupportedException();

        public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();
    }

    /// <summary>Memory that is not over a managed array.</summary>
    private sealed unsafe class NativeMemory : MemoryManager<byte>
    {
        private readonly IntPtr _pointer;
        private readonly int _length;

        public NativeMemory(int length)
        {
            _length = length;
            _pointer = System.Runtime.InteropServices.Marshal.AllocHGlobal(length);
            new Span<byte>((void*)_pointer, length).Clear();
        }

        public override Span<byte> GetSpan() => new((void*)_pointer, _length);

        public override MemoryHandle Pin(int elementIndex = 0) => new((byte*)_pointer + elementIndex);

        public override void Unpin()
        {
        }

        protected override void Dispose(bool disposing) => System.Runtime.InteropServices.Marshal.FreeHGlobal(_pointer);
    }

    /// <summary>
    /// A local HTTP server answering one request with a body of
    /// <c>bodyLength</c> bytes by its Content-Length, of which it sends the first
    /// <c>sent</c> ("MZab" and on) and then stops, holding the connection open.
    /// </summary>
    private sealed class StallingServer : IDisposable
    {
        private readonly TcpListener _listener;
        private readonly CancellationTokenSource _stop = new();

        private StallingServer(TcpListener listener) => _listener = listener;

        public Uri Address => new($"http://127.0.0.1:{((IPEndPoint)_listener.LocalEndpoint).Port}/");

        public static StallingServer Start(int bodyLength, int sent)
        {
            var listener = new TcpListener(IPAddress.Loopback, 0);
            listener.Start();
            var server = new StallingServer(listener);
            _ = Task.Run(() => server.Serve(bodyLength, sent));
            return server;
        }

        private async Task Serve(int bodyLength, int sent)
        {
            try
            {
                using var client = await _listener.AcceptTcpClientAsync();
                using var connection = client.GetStream();
                var request = new byte[4096];
                _ = await connection.ReadAsync(request, 0, request.Length);
                var head = Encoding.ASCII.GetBytes(
                    $"HTTP/1.1 200 OK\r\nContent-Length: {bodyLength}\r\nContent-Type: application/octet-stream\r\n\r\n");
                await connection.WriteAsync(head, 0, head.Length);
                var body = Encoding.ASCII.GetBytes("MZab".PadRight(sent, 'c').Substring(0, sent));
                await connection.WriteAsync(body, 0, body.Length);
                await connection.FlushAsync();
                await Task.Delay(Timeout.Infinite, _stop.Token);
            }
            catch (Exception ex) when (ex is OperationCanceledException or IOException or ObjectDisposedException or SocketException)
            {
            }
        }

        public void Dispose()
        {
            _stop.Cancel();
            _listener.Stop();
            _stop.Dispose();
        }
    }
}
