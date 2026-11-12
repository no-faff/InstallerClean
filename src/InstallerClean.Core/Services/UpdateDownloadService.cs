using System.Net;
using System.Net.Http;
using InstallerClean.Helpers;

namespace InstallerClean.Services;

/// <summary>
/// Downloads the newer version's file to a path, following GitHub's redirect from the
/// release's download address to the server holding the file.
/// </summary>
/// <remarks>
/// Every address on the way has to be https, the first included. Redirects are
/// followed here rather than by the handler, so that rule is applied to each hop the
/// same way whatever handler is underneath, a test's included. The body is refused
/// past <see cref="MaxBytes"/>, refused where it stops arriving for thirty seconds,
/// and refused unless it starts as every Windows program
/// does, with "MZ", so a page some network puts in GitHub's place is never saved as
/// the program to run. The file is written through
/// <see cref="StorageHelpers.OpenAtomic"/> and flushed to the disk before the call
/// returns.
/// </remarks>
public sealed class UpdateDownloadService
{
    /// <summary>Several times the size of any download the project publishes.</summary>
    internal const long MaxBytes = 512L * 1024 * 1024;

    private static readonly TimeSpan DefaultStallTimeout = Durations.FromSeconds(30);

    private const int MaxRedirects = 10;

    private readonly HttpClient _http;
    private readonly TimeSpan _stallTimeout;

    public UpdateDownloadService()
        : this(ProxyFromVariables.Carrying(new HttpClientHandler { AllowAutoRedirect = false, UseCookies = false }), null) { }

    /// <summary>
    /// Test seam: a handler stands in for the network, and a shorter stall timeout
    /// keeps a test from waiting out the real one.
    /// </summary>
    internal UpdateDownloadService(HttpMessageHandler handler, TimeSpan? stallTimeout)
    {
        // No overall timeout: a large file on a slow line can take minutes, and what
        // decides that a download has failed is the stall timeout, which starts again
        // with every read that brings data.
        _http = new HttpClient(handler) { Timeout = Timeout.InfiniteTimeSpan };
        _stallTimeout = stallTimeout ?? DefaultStallTimeout;
    }

    /// <summary>
    /// Downloads <paramref name="address"/> to <paramref name="destinationPath"/>.
    /// Reports the fraction done where the server gives the length, and null where it
    /// does not. Throws <see cref="UpdateFailedException"/> on any failure, with
    /// nothing left at the destination, and <see cref="OperationCanceledException"/>
    /// where <paramref name="cancellationToken"/> is cancelled, again leaving nothing.
    /// </summary>
    public async Task DownloadAsync(
        Uri address, string destinationPath, IProgress<double?>? progress,
        CancellationToken cancellationToken)
    {
        try
        {
            await DownloadCoreAsync(address, destinationPath, progress, cancellationToken)
                .ConfigureAwait(false);
        }
        catch
        {
            StorageHelpers.TryDeleteTempFile(destinationPath);
            throw;
        }
    }

    private Task DownloadCoreAsync(
        Uri address, string destinationPath, IProgress<double?>? progress,
        CancellationToken cancellationToken) =>
        WithStallAsync<object?>(async stall =>
        {
            // Null only for a 404 asked to be an answer, which the download does not ask.
            using var response = (await SendFollowingRedirectsAsync(address, stall.Token).ConfigureAwait(false))!;

            var length = response.Content.Headers.ContentLength;
            if (length > MaxBytes)
                throw Download($"The download's length, {length} bytes, is over the ceiling.");

            using var handle = StorageHelpers.OpenAtomic(
                    destinationPath, FileAccess.Write, StorageHelpers.AtomicOpenMode.CreateAlways)
                ?? throw new UpdateFailedException(UpdateFailureStage.Install,
                    "The file the download is saved to could not be created.");
            using var file = StorageHelpers.UnbufferedWriter(handle);

            stall.CancelAfter(_stallTimeout);
            using var body = await response.Content.ReadAsStreamAsync(stall.Token).ConfigureAwait(false);

            var buffer = new byte[81920];
            long total = 0;
            progress?.Report(length is > 0 ? 0d : null);
            while (true)
            {
                stall.CancelAfter(_stallTimeout);
                var read = await body.ReadAsync(buffer, stall.Token).ConfigureAwait(false);
                if (read == 0) break;

                // The first two bytes are checked as they arrive, whichever reads bring them.
                for (var i = 0; i < read && total + i < 2; i++)
                {
                    if (buffer[i] != (total + i == 0 ? (byte)'M' : (byte)'Z'))
                        throw Download("The download is not a Windows program.");
                }

                total += read;
                if (total > MaxBytes)
                    throw Download("The download is over the ceiling.");

                try
                {
                    file.Write(buffer, 0, read);
                }
                catch (IOException ex)
                {
                    throw new UpdateFailedException(UpdateFailureStage.Install,
                        "The download could not be saved.", ex);
                }

                if (length is > 0)
                    progress?.Report(Math.Min(1d, (double)total / length.Value));
            }

            if (total < 2)
                throw Download("The download is not a Windows program.");
            if (length is not null && total != length)
                throw Download($"The download ended after {total} of {length} bytes.");

            try
            {
                file.Flush(flushToDisk: true);
            }
            catch (IOException ex)
            {
                throw new UpdateFailedException(UpdateFailureStage.Install,
                    "The download could not be saved.", ex);
            }
            return null;
        }, "The download", cancellationToken);

    /// <summary>
    /// Runs <paramref name="work"/> with a token that is cancelled where
    /// <paramref name="cancellationToken"/> is, or where no data has arrived for the stall
    /// timeout, which <paramref name="work"/> starts again with every read that brings
    /// data. A broken connection or a stall becomes an <see cref="UpdateFailedException"/>
    /// at the Download stage, named by <paramref name="what"/>; a cancel stays a cancel.
    /// </summary>
    private async Task<T> WithStallAsync<T>(
        Func<CancellationTokenSource, Task<T>> work, string what, CancellationToken cancellationToken)
    {
        using var stall = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        try
        {
            stall.CancelAfter(_stallTimeout);
            return await work(stall).ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex) when ((ex is HttpRequestException or IOException or ObjectDisposedException)
            && cancellationToken.IsCancellationRequested)
        {
            // A connection broken off by the cancel itself is the cancel.
            throw new OperationCanceledException($"{what} was cancelled.", ex, cancellationToken);
        }
        catch (UpdateFailedException ex) when (ex.Stage == UpdateFailureStage.Download
            && cancellationToken.IsCancellationRequested)
        {
            // A Download-stage failure that comes as the cancel does, a server's error answer or
            // a body refused for what it holds among them, is the cancel. A failure to save
            // what arrived is the Install stage's and stays a failure.
            throw new OperationCanceledException($"{what} was cancelled.", ex, cancellationToken);
        }
        catch (OperationCanceledException ex)
        {
            throw new UpdateFailedException(UpdateFailureStage.Download,
                $"No data arrived for {_stallTimeout.TotalSeconds} seconds.", ex);
        }
        catch (HttpRequestException ex)
        {
            throw new UpdateFailedException(UpdateFailureStage.Download, $"{what} failed.", ex);
        }
        catch (IOException ex)
        {
            // A write failure is caught where it happens, so an IOException reaching
            // here came from the connection.
            throw new UpdateFailedException(UpdateFailureStage.Download, $"{what} failed.", ex);
        }
    }

    /// <summary>
    /// Reads <paramref name="address"/> into memory over the same https-only redirects
    /// as <see cref="DownloadAsync"/>, under the same stall timeout, and answers its
    /// bytes, or null where the host of the address asked for answers 404 Not Found with
    /// every redirect on the way on that host, as github.com answers for a file a release
    /// does not have, at the address or through a redirect it makes for a repository that
    /// has moved. A 404 reached through another host is a failed read: github.com answers a
    /// file a release has by redirecting to the host that serves it. Throws
    /// <see cref="UpdateFailedException"/> at the Download stage for every other failure,
    /// a body over <paramref name="maxBytes"/> included, and
    /// <see cref="OperationCanceledException"/> where <paramref name="cancellationToken"/>
    /// is cancelled.
    /// </summary>
    public Task<byte[]?> ReadAsync(Uri address, int maxBytes, CancellationToken cancellationToken) =>
        WithStallAsync(async stall =>
        {
            using var response = await SendFollowingRedirectsAsync(address, stall.Token, notFoundIsAnswer: true)
                .ConfigureAwait(false);
            if (response is null) return null;
            if (response.Content.Headers.ContentLength > maxBytes)
                throw Download($"The file at {address} is over {maxBytes} bytes.");

            using var body = await response.Content.ReadAsStreamAsync(stall.Token).ConfigureAwait(false);
            using var held = new MemoryStream();
            var buffer = new byte[8192];
            while (true)
            {
                stall.CancelAfter(_stallTimeout);
                var read = await body.ReadAsync(buffer, stall.Token).ConfigureAwait(false);
                if (read == 0) break;
                if (held.Length + read > maxBytes)
                    throw Download($"The file at {address} is over {maxBytes} bytes.");
                held.Write(buffer, 0, read);
            }
            return (byte[]?)held.ToArray();
        }, $"Reading {address}", cancellationToken);

    /// <summary>
    /// Sends a GET for <paramref name="address"/> and follows each redirect to an https
    /// address, up to <see cref="MaxRedirects"/> of them, and answers with the first
    /// response that is not a redirect, which has to be a success, or null for a
    /// 404 Not Found where <paramref name="notFoundIsAnswer"/> is set and every address on
    /// the way was on <paramref name="address"/>'s host and port.
    /// </summary>
    private async Task<HttpResponseMessage?> SendFollowingRedirectsAsync(
        Uri address, CancellationToken cancellationToken, bool notFoundIsAnswer = false)
    {
        var current = address;
        var onFirstHost = true;
        for (var hop = 0; ; hop++)
        {
            if (!string.Equals(current.Scheme, Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase))
                throw Download($"The download was sent to an address that is not https: {current.Scheme}.");

            using var request = new HttpRequestMessage(HttpMethod.Get, current);
            request.Headers.UserAgent.ParseAdd(UpdateCheckService.UserAgent);
            var response = await _http.SendAsync(
                    request, HttpCompletionOption.ResponseHeadersRead, cancellationToken)
                .ConfigureAwait(false);

            if (IsRedirect(response.StatusCode))
            {
                var location = response.Headers.Location;
                response.Dispose();
                if (location is null)
                    throw Download("A redirect carried no address.");
                if (hop >= MaxRedirects)
                    throw Download("The download was redirected too many times.");
                current = location.IsAbsoluteUri ? location : new Uri(current, location);
                onFirstHost &= string.Equals(current.Authority, address.Authority, StringComparison.OrdinalIgnoreCase);
                continue;
            }

            if (notFoundIsAnswer && onFirstHost && response.StatusCode == HttpStatusCode.NotFound)
            {
                response.Dispose();
                return null;
            }

            if (!response.IsSuccessStatusCode)
            {
                var status = (int)response.StatusCode;
                response.Dispose();
                throw Download($"The server answered {status}.");
            }
            return response;
        }
    }

    private static bool IsRedirect(HttpStatusCode status) => status is
        HttpStatusCode.MovedPermanently or HttpStatusCode.Found or HttpStatusCode.SeeOther
        or HttpStatusCode.TemporaryRedirect or PermanentRedirect;

    /// <summary>
    /// 308 Permanent Redirect, which .NET Framework's <see cref="HttpStatusCode"/> has no name
    /// for.
    /// </summary>
    private const HttpStatusCode PermanentRedirect = (HttpStatusCode)308;

    private static UpdateFailedException Download(string message) =>
        new(UpdateFailureStage.Download, message);
}
