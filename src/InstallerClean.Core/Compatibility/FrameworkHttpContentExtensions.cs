using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace System.Net.Http;

/// <summary>
/// The cancellable <see cref="HttpContent.ReadAsStreamAsync()"/> .NET Framework 4.8 does not
/// have, handing back a stream whose reads end when their token is cancelled, as the
/// response streams of .NET 10's HttpClient do.
/// </summary>
/// <remarks>
/// .NET Framework's response stream looks at a read's token only before the read starts, and
/// HttpClient stops listening to its own token once the headers are in, so a read the server
/// stops answering would wait for ever. Disposing the response stream is what ends such a
/// read there, and it then fails with IOException or ObjectDisposedException.
/// </remarks>
internal static class FrameworkHttpContentExtensions
{
    private static readonly ConditionalWeakTable<HttpContent, Task<Stream>> s_streams = new();

    /// <summary>
    /// The content's stream, the same one on every call. The token is not looked at here, as
    /// .NET 10's response content does not; each read is handed its own.
    /// </summary>
    internal static Task<Stream> ReadAsStreamAsync(this HttpContent content, CancellationToken cancellationToken)
    {
        return s_streams.GetValue(content, static c => Open(c));
    }

    private static async Task<Stream> Open(HttpContent content) =>
        new CancellableReadStream(await content.ReadAsStreamAsync().ConfigureAwait(false));

    /// <summary>
    /// A read-only stream over a response stream, whose reads end when their token is
    /// cancelled. A cancel disposes the stream underneath, which is how .NET 10's HttpClient
    /// ends a read too, and the read then throws TaskCanceledException carrying the token
    /// and, where the read underneath had already failed, that failure.
    /// </summary>
    private sealed class CancellableReadStream(Stream inner) : Stream
    {
        private int _disposed;

        public override bool CanRead => inner.CanRead;

        public override bool CanSeek => false;

        public override bool CanWrite => false;

        public override long Length => throw new NotSupportedException();

        public override long Position
        {
            get => throw new NotSupportedException();
            set => throw new NotSupportedException();
        }

        public override int Read(byte[] buffer, int offset, int count) => inner.Read(buffer, offset, count);

        public override Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken) =>
            ReadCancellableAsync(buffer, offset, count, cancellationToken);

        private async Task<int> ReadCancellableAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            if (cancellationToken.IsCancellationRequested)
                throw Cancelled(null, cancellationToken);

            var read = inner.ReadAsync(buffer, offset, count, cancellationToken);
            if (read.IsCompleted || !cancellationToken.CanBeCanceled)
            {
                try
                {
                    return await read.ConfigureAwait(false);
                }
                catch (Exception ex) when (ex is not OperationCanceledException && cancellationToken.IsCancellationRequested)
                {
                    throw Cancelled(ex, cancellationToken);
                }
            }

            // The registration's callback runs on the thread that cancels, a timer's thread
            // for CancelAfter, so it only signals and leaves the dispose to the thread pool:
            // .NET Framework's dispose can read what is left of a short body for up to half
            // a second before it closes the connection.
            var cancelled = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
            using (cancellationToken.Register(static state =>
                   {
                       var (stream, signal) = ((CancellableReadStream, TaskCompletionSource<bool>))state!;
                       signal.TrySetResult(true);
                       ThreadPool.QueueUserWorkItem(static s => ((CancellableReadStream)s!).DisposeInner(), stream);
                   }, (this, cancelled)))
            {
                var first = await Task.WhenAny(read, cancelled.Task).ConfigureAwait(false);
                if (first == read)
                {
                    try
                    {
                        return await read.ConfigureAwait(false);
                    }
                    catch (Exception ex) when (ex is not OperationCanceledException && cancellationToken.IsCancellationRequested)
                    {
                        throw Cancelled(ex, cancellationToken);
                    }
                }
            }

            // A read that finished as the token fired keeps what it read.
            if (read.Status == TaskStatus.RanToCompletion)
                return read.Result;

            // The token fired with the read still waiting. Its eventual failure is observed
            // here so it is never reported as unobserved.
            _ = read.ContinueWith(static t => _ = t.Exception, CancellationToken.None,
                TaskContinuationOptions.OnlyOnFaulted | TaskContinuationOptions.ExecuteSynchronously, TaskScheduler.Default);
            throw Cancelled(read.IsFaulted ? read.Exception!.InnerException : null, cancellationToken);
        }

        public override void Flush()
        {
        }

        public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();

        public override void SetLength(long value) => throw new NotSupportedException();

        public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();

        protected override void Dispose(bool disposing)
        {
            if (disposing) DisposeInner();
            base.Dispose(disposing);
        }

        /// <summary>
        /// Disposes the stream underneath once, whichever of a cancel and the caller comes
        /// first; the second returns at once. What the dispose meets on the way is not
        /// thrown, so a failure there never takes the place of the cancel being reported.
        /// </summary>
        private void DisposeInner()
        {
            if (Interlocked.Exchange(ref _disposed, 1) != 0) return;

            try
            {
                inner.Dispose();
            }
            catch (Exception ex) when (ex is IOException or ObjectDisposedException)
            {
                // The connection is going whatever the dispose meets on the way.
            }
        }

        /// <summary>
        /// The exception .NET 10's response streams throw for a cancelled read. .NET
        /// Framework's TaskCanceledException has no constructor taking a token, and gives
        /// <see cref="OperationCanceledException.CancellationToken"/> a private setter, which
        /// is reached by reflection.
        /// </summary>
        private static TaskCanceledException Cancelled(Exception? inner, CancellationToken token)
        {
            var exception = new TaskCanceledException("The operation was canceled.", inner);
            typeof(OperationCanceledException).GetProperty(nameof(OperationCanceledException.CancellationToken))!
                .GetSetMethod(nonPublic: true)!
                .Invoke(exception, [token]);
            return exception;
        }
    }
}
