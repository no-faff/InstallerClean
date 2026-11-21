using System.Net;
using System.Net.Http;
using InstallerClean.Services;
using InstallerClean.Tests.Helpers;

namespace InstallerClean.Tests.Services;

/// <summary>
/// The download an update makes: GitHub's redirects followed only to https, the body
/// refused when it is too large, not a Windows program, cut short or stalled, and
/// nothing left at the destination after any failure.
/// </summary>
public sealed class UpdateDownloadServiceTests : IDisposable
{
    private static readonly Uri ReleaseAddress =
        new("https://github.com/no-faff/InstallerClean/releases/download/v3.5.0/InstallerClean-portable.exe");

    private static readonly Uri AssetAddress =
        new("https://release-assets.githubusercontent.com/some/asset");

    private readonly string _folder = TestTemp.NewFolder("ic-download-");

    private string Destination => Path.Combine(_folder, "download.exe");

    public void Dispose()
    {
        TestTemp.Delete(_folder);
    }

    /// <summary>Answers each request from a table keyed by its address, and records the addresses.</summary>
    private sealed class RoutedHandler(Func<Uri, HttpResponseMessage> respond) : HttpMessageHandler
    {
        public List<Uri> Requested { get; } = [];

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Requested.Add(request.RequestUri!);
            return Task.FromResult(respond(request.RequestUri!));
        }
    }

    private static HttpResponseMessage Redirect(Uri to)
    {
        var response = new HttpResponseMessage(HttpStatusCode.Found);
        response.Headers.Location = to;
        return response;
    }

    private static HttpResponseMessage Body(byte[] bytes) =>
        new(HttpStatusCode.OK) { Content = new ByteArrayContent(bytes) };

    private static byte[] Program(int length = 4096)
    {
        var bytes = new byte[length];
        bytes[0] = (byte)'M';
        bytes[1] = (byte)'Z';
        for (var i = 2; i < length; i++) bytes[i] = (byte)(i % 251);
        return bytes;
    }

    [Fact]
    public async Task Follows_the_redirect_to_the_asset_and_writes_what_it_answered()
    {
        var program = Program();
        var handler = new RoutedHandler(uri => uri == ReleaseAddress ? Redirect(AssetAddress) : Body(program));
        var service = new UpdateDownloadService(handler, null);
        var reported = new List<double?>();

        await service.DownloadAsync(ReleaseAddress, Destination, new SyncProgress(reported.Add), CancellationToken.None);

        Assert.Equal([ReleaseAddress, AssetAddress], handler.Requested);
        Assert.Equal(program, await File.ReadAllBytesAsync(Destination));
        Assert.Equal(1d, reported[^1]);
    }

    /// <summary>A stream that cannot seek, so content over it carries no length.</summary>
    private sealed class UnseekableStream(byte[] bytes) : MemoryStream(bytes)
    {
        public override bool CanSeek => false;
    }

    private static readonly Uri ManifestAddress =
        new("https://github.com/no-faff/InstallerClean/releases/download/v3.5.0/update.json");

    [Fact]
    public async Task A_read_follows_the_redirect_and_answers_the_bytes_in_memory()
    {
        var text = "{}"u8.ToArray();
        var handler = new RoutedHandler(uri => uri == ManifestAddress ? Redirect(AssetAddress) : Body(text));
        var service = new UpdateDownloadService(handler, null);

        var read = await service.ReadAsync(ManifestAddress, 1024, CancellationToken.None);

        Assert.Equal(text, read);
        Assert.Equal([ManifestAddress, AssetAddress], handler.Requested);
    }

    [Fact]
    public async Task A_read_answers_null_for_a_404_at_the_address_asked_for()
    {
        var handler = new RoutedHandler(_ => new HttpResponseMessage(HttpStatusCode.NotFound));
        var service = new UpdateDownloadService(handler, null);

        Assert.Null(await service.ReadAsync(ManifestAddress, 1024, CancellationToken.None));
        Assert.Equal([ManifestAddress], handler.Requested);
    }

    [Fact]
    public async Task A_read_fails_as_a_download_for_a_404_from_the_host_it_is_redirected_to()
    {
        // github.com answers a file a release has by redirecting to the host that serves it,
        // so a 404 from that host is a file that could not be fetched, not one the release lacks.
        var handler = new RoutedHandler(uri => uri == ManifestAddress
            ? Redirect(AssetAddress)
            : new HttpResponseMessage(HttpStatusCode.NotFound));
        var service = new UpdateDownloadService(handler, null);

        var failure = await Assert.ThrowsAsync<UpdateFailedException>(
            () => service.ReadAsync(ManifestAddress, 1024, CancellationToken.None));

        Assert.Equal(UpdateFailureStage.Download, failure.Stage);
        Assert.Equal("The server answered 404.", failure.Message);
        Assert.Equal([ManifestAddress, AssetAddress], handler.Requested);
    }

    [Fact]
    public async Task A_read_fails_as_a_download_for_a_404_back_on_github_after_another_host()
    {
        var back = new Uri(ManifestAddress.GetLeftPart(UriPartial.Authority) + "/back/update.json");
        var handler = new RoutedHandler(uri =>
            uri == ManifestAddress ? Redirect(AssetAddress)
            : uri == AssetAddress ? Redirect(back)
            : new HttpResponseMessage(HttpStatusCode.NotFound));
        var service = new UpdateDownloadService(handler, null);

        var failure = await Assert.ThrowsAsync<UpdateFailedException>(
            () => service.ReadAsync(ManifestAddress, 1024, CancellationToken.None));

        Assert.Equal(UpdateFailureStage.Download, failure.Stage);
        Assert.Equal("The server answered 404.", failure.Message);
        Assert.Equal([ManifestAddress, AssetAddress, back], handler.Requested);
    }

    [Fact]
    public async Task A_read_answers_null_for_a_404_after_a_redirect_on_the_same_host()
    {
        // A repository that has moved is redirected on github.com itself, and a release there
        // without the file still answers 404 from github.com.
        var moved = new Uri(ManifestAddress.GetLeftPart(UriPartial.Authority) + "/moved/update.json");
        var handler = new RoutedHandler(uri => uri == ManifestAddress
            ? Redirect(moved)
            : new HttpResponseMessage(HttpStatusCode.NotFound));
        var service = new UpdateDownloadService(handler, null);

        Assert.Null(await service.ReadAsync(ManifestAddress, 1024, CancellationToken.None));
        Assert.Equal([ManifestAddress, moved], handler.Requested);
    }

    [Theory]
    [InlineData(HttpStatusCode.InternalServerError)]
    [InlineData(HttpStatusCode.Forbidden)]
    [InlineData(HttpStatusCode.Gone)]
    public async Task A_read_fails_as_a_download_for_any_status_but_404(HttpStatusCode status)
    {
        var handler = new RoutedHandler(_ => new HttpResponseMessage(status));
        var service = new UpdateDownloadService(handler, null);

        var failure = await Assert.ThrowsAsync<UpdateFailedException>(
            () => service.ReadAsync(ManifestAddress, 1024, CancellationToken.None));

        Assert.Equal(UpdateFailureStage.Download, failure.Stage);
    }

    [Fact]
    public async Task A_read_over_its_cap_fails_as_a_download_with_or_without_a_length()
    {
        var big = new byte[2048];
        var withLength = new UpdateDownloadService(new RoutedHandler(_ => Body(big)), null);
        var withoutLength = new UpdateDownloadService(new RoutedHandler(_ =>
            new HttpResponseMessage(HttpStatusCode.OK) { Content = new StreamContent(new UnseekableStream(big)) }), null);

        foreach (var service in new[] { withLength, withoutLength })
        {
            var failure = await Assert.ThrowsAsync<UpdateFailedException>(
                () => service.ReadAsync(ManifestAddress, 1024, CancellationToken.None));
            Assert.Equal(UpdateFailureStage.Download, failure.Stage);
        }
    }

    [Fact]
    public async Task A_404_is_still_a_failed_download_for_a_download()
    {
        var handler = new RoutedHandler(_ => new HttpResponseMessage(HttpStatusCode.NotFound));
        var service = new UpdateDownloadService(handler, null);

        var failure = await Assert.ThrowsAsync<UpdateFailedException>(
            () => service.DownloadAsync(ReleaseAddress, Destination, null, CancellationToken.None));

        Assert.Equal(UpdateFailureStage.Download, failure.Stage);
        Assert.False(File.Exists(Destination));
    }

    [Fact]
    public async Task A_redirect_to_an_address_that_is_not_https_is_refused()
    {
        var handler = new RoutedHandler(uri => uri == ReleaseAddress
            ? Redirect(new Uri("http://release-assets.githubusercontent.com/some/asset"))
            : Body(Program()));
        var service = new UpdateDownloadService(handler, null);

        var failure = await Assert.ThrowsAsync<UpdateFailedException>(
            () => service.DownloadAsync(ReleaseAddress, Destination, null, CancellationToken.None));

        Assert.Equal(UpdateFailureStage.Download, failure.Stage);
        Assert.Single(handler.Requested);
        Assert.False(File.Exists(Destination));
    }

    [Fact]
    public async Task A_first_address_that_is_not_https_is_refused_before_any_request()
    {
        var handler = new RoutedHandler(_ => Body(Program()));
        var service = new UpdateDownloadService(handler, null);

        var failure = await Assert.ThrowsAsync<UpdateFailedException>(() => service.DownloadAsync(
            new Uri("http://github.com/no-faff/InstallerClean/releases/download/v3.5.0/x.exe"),
            Destination, null, CancellationToken.None));

        Assert.Equal(UpdateFailureStage.Download, failure.Stage);
        Assert.Empty(handler.Requested);
    }

    [Fact]
    public async Task A_redirect_that_never_ends_is_refused()
    {
        var handler = new RoutedHandler(_ => Redirect(AssetAddress));
        var service = new UpdateDownloadService(handler, null);

        var failure = await Assert.ThrowsAsync<UpdateFailedException>(
            () => service.DownloadAsync(ReleaseAddress, Destination, null, CancellationToken.None));

        Assert.Equal(UpdateFailureStage.Download, failure.Stage);
    }

    [Fact]
    public async Task An_error_from_the_server_is_a_download_failure()
    {
        var handler = new RoutedHandler(_ => new HttpResponseMessage(HttpStatusCode.NotFound));
        var service = new UpdateDownloadService(handler, null);

        var failure = await Assert.ThrowsAsync<UpdateFailedException>(
            () => service.DownloadAsync(ReleaseAddress, Destination, null, CancellationToken.None));

        Assert.Equal(UpdateFailureStage.Download, failure.Stage);
        Assert.False(File.Exists(Destination));
    }

    [Fact]
    public async Task A_length_over_the_ceiling_is_refused_before_anything_is_written()
    {
        var handler = new RoutedHandler(_ =>
        {
            var response = Body(Program());
            response.Content.Headers.ContentLength = UpdateDownloadService.MaxBytes + 1;
            return response;
        });
        var service = new UpdateDownloadService(handler, null);

        var failure = await Assert.ThrowsAsync<UpdateFailedException>(
            () => service.DownloadAsync(ReleaseAddress, Destination, null, CancellationToken.None));

        Assert.Equal(UpdateFailureStage.Download, failure.Stage);
        Assert.False(File.Exists(Destination));
    }

    [Fact]
    public async Task A_page_in_place_of_a_program_is_refused_and_not_left_behind()
    {
        var handler = new RoutedHandler(_ => Body("<html><body>Sign in</body></html>"u8.ToArray()));
        var service = new UpdateDownloadService(handler, null);

        var failure = await Assert.ThrowsAsync<UpdateFailedException>(
            () => service.DownloadAsync(ReleaseAddress, Destination, null, CancellationToken.None));

        Assert.Equal(UpdateFailureStage.Download, failure.Stage);
        Assert.False(File.Exists(Destination));
    }

    [Fact]
    public async Task A_body_that_stops_arriving_fails_on_the_stall_timeout()
    {
        var handler = new RoutedHandler(_ => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StreamContent(new StallingStream("MZ"u8.ToArray())),
        });
        var service = new UpdateDownloadService(handler, TimeSpan.FromMilliseconds(200));

        var failure = await Assert.ThrowsAsync<UpdateFailedException>(
            () => service.DownloadAsync(ReleaseAddress, Destination, null, CancellationToken.None));

        Assert.Equal(UpdateFailureStage.Download, failure.Stage);
        Assert.False(File.Exists(Destination));
    }

#if DEBUG
    /// <summary>A program of <paramref name="length"/> bytes on disk, with the gate's path beside it.</summary>
    private (string File, string Gate, byte[] Bytes) GatedLocalFile(int length = 4 * 1024 * 1024)
    {
        var bytes = Program(length);
        var file = Path.Combine(_folder, "local.exe");
        File.WriteAllBytes(file, bytes);
        return (file, Path.Combine(_folder, "gate"), bytes);
    }

    /// <summary>
    /// Waits for the download to reach the gate, failing the test after ten seconds.
    /// </summary>
    private static async Task UntilReached(string gate, Task download)
    {
        var reached = gate + AppUpdater.GateReachedSuffix;
        for (var i = 0; i < 100 && !File.Exists(reached) && !download.IsCompleted; i++) await Task.Delay(100);
        Assert.True(File.Exists(reached), "the download did not reach the gate");
    }

    [Theory]
    [InlineData(2, 1)]
    [InlineData(3, 1)]
    [InlineData(4 * 1024 * 1024, 2 * 1024 * 1024)]
    [InlineData(16 * 1024 * 1024 - 2, 8 * 1024 * 1024 - 1)]
    [InlineData(16 * 1024 * 1024, 8 * 1024 * 1024)]
    [InlineData(16 * 1024 * 1024 + 2, 8 * 1024 * 1024)]
    [InlineData(100 * 1024 * 1024, 8 * 1024 * 1024)]
    public void The_gate_holds_a_file_part_way(long length, long heldAt)
    {
        var point = AppUpdater.GatePoint(length);
        Assert.Equal(heldAt, point);
        Assert.InRange(point, 1, length - 1);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    public void The_gate_never_holds_before_the_first_byte(long length) =>
        Assert.Equal(1, AppUpdater.GatePoint(length));

    [Fact]
    public async Task A_gate_and_a_reached_file_left_by_an_earlier_download_are_gone_once_the_local_file_answers()
    {
        var (file, gate, _) = GatedLocalFile();
        File.WriteAllText(gate, string.Empty);
        File.WriteAllText(gate + AppUpdater.GateReachedSuffix, string.Empty);
        using var client = new HttpClient(new AppUpdater.LocalFileAnswer(file, gate));

        using var response = await client.GetAsync(ReleaseAddress, HttpCompletionOption.ResponseHeadersRead);

        Assert.False(File.Exists(gate));
        Assert.False(File.Exists(gate + AppUpdater.GateReachedSuffix));
    }

    [Fact]
    public async Task A_download_after_an_earlier_downloads_gate_stops_at_its_own_gate_and_finishes_whole()
    {
        // A gate left by an earlier download would let this one straight through.
        var (file, gate, bytes) = GatedLocalFile();
        File.WriteAllText(gate, string.Empty);
        var service = new UpdateDownloadService(new AppUpdater.LocalFileAnswer(file, gate), TimeSpan.FromSeconds(30));

        var download = service.DownloadAsync(ReleaseAddress, Destination, null, CancellationToken.None);
        await UntilReached(gate, download);

        Assert.False(download.IsCompleted);
        Assert.False(File.Exists(gate));
        Assert.Equal(AppUpdater.GatePoint(bytes.Length), WrittenSoFar());
        File.WriteAllText(gate, string.Empty);
        await download;

        Assert.Equal(bytes, File.ReadAllBytes(Destination));
    }

    [Fact]
    public async Task A_local_file_that_cannot_be_opened_leaves_the_gate_and_reached_file_where_they_are()
    {
        // The local file is opened before the gate is made ready, so a download that cannot
        // start touches neither.
        var (_, gate, _) = GatedLocalFile();
        File.WriteAllText(gate, string.Empty);
        File.WriteAllText(gate + AppUpdater.GateReachedSuffix, string.Empty);
        using var client = new HttpClient(new AppUpdater.LocalFileAnswer(Path.Combine(_folder, "missing.exe"), gate));

        var refused = await Assert.ThrowsAsync<HttpRequestException>(
            () => client.GetAsync(ReleaseAddress, HttpCompletionOption.ResponseHeadersRead));

        Assert.Contains("could not be opened", refused.Message);
        Assert.True(File.Exists(gate));
        Assert.True(File.Exists(gate + AppUpdater.GateReachedSuffix));
    }

    /// <summary>The length of the file being written, read through a handle so it is current.</summary>
    private long WrittenSoFar()
    {
        using var open = new FileStream(Destination, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete);
        return open.Length;
    }

    [Theory]
    [InlineData(16 * 1024 * 1024 + 2, 8 * 1024 * 1024)]
    [InlineData(4 * 1024 * 1024, 2 * 1024 * 1024)]
    public async Task A_download_held_at_the_gate_has_part_of_the_file_on_disk_and_cancelling_it_leaves_nothing(
        int length, int heldAt)
    {
        Assert.Equal(heldAt, AppUpdater.GatePoint(length));
        var (file, gate, _) = GatedLocalFile(length);
        var service = new UpdateDownloadService(new AppUpdater.LocalFileAnswer(file, gate), TimeSpan.FromSeconds(30));
        using var cts = new CancellationTokenSource();

        var download = service.DownloadAsync(ReleaseAddress, Destination, null, cts.Token);
        await UntilReached(gate, download);

        Assert.False(download.IsCompleted);
        Assert.Equal(heldAt, WrittenSoFar());
        cts.Cancel();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => download);
        Assert.False(File.Exists(Destination));
    }

    [Fact]
    public async Task A_download_held_at_the_gate_finishes_whole_once_the_gate_file_exists()
    {
        var (file, gate, bytes) = GatedLocalFile();
        var service = new UpdateDownloadService(new AppUpdater.LocalFileAnswer(file, gate), TimeSpan.FromSeconds(30));
        var reported = new List<double?>();

        var download = service.DownloadAsync(ReleaseAddress, Destination, new SyncProgress(reported.Add), CancellationToken.None);
        await UntilReached(gate, download);
        Assert.False(download.IsCompleted);
        File.WriteAllText(gate, string.Empty);
        await download;

        Assert.Equal(bytes, File.ReadAllBytes(Destination));
        Assert.Equal(1d, reported[^1]);
    }

    [Fact]
    public async Task A_download_held_at_the_gate_past_the_stall_timeout_fails_as_stalled()
    {
        // A download whose reads are slower than the timeout stalls before it reaches the gate,
        // and fails in the same way without showing anything about the hold. Such an attempt
        // is made again with a timeout twice as long. A download the timeout does not end fails
        // the test rather than waiting on it.
        const int attempts = 5;
        var milliseconds = 500;
        for (var attempt = 1; ; attempt++, milliseconds *= 2)
        {
            var (file, gate, _) = GatedLocalFile();
            var service = new UpdateDownloadService(new AppUpdater.LocalFileAnswer(file, gate), TimeSpan.FromMilliseconds(milliseconds));

            var download = service.DownloadAsync(ReleaseAddress, Destination, null, CancellationToken.None);
            var ended = await Task.WhenAny(download, Task.Delay(milliseconds + 30_000));
            Assert.True(ReferenceEquals(ended, download), "the stall timeout did not end a download held at the gate");

            var failure = await Assert.ThrowsAsync<UpdateFailedException>(() => download);
            Assert.Equal(UpdateFailureStage.Download, failure.Stage);
            Assert.False(File.Exists(Destination));
            if (File.Exists(gate + AppUpdater.GateReachedSuffix)) return;
            Assert.True(attempt < attempts, $"the download stalled before it reached the gate in each of {attempts} attempts");
        }
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task A_gate_that_is_not_a_full_path_or_whose_folder_is_not_there_is_refused(bool fullPath)
    {
        var (file, _, _) = GatedLocalFile();
        var gate = fullPath ? Path.Combine(_folder, "not-there", "gate") : "gate";
        AppUpdater.LocalFileAnswer Answer() => new(file, gate);
        using var client = new HttpClient(Answer());
        var service = new UpdateDownloadService(Answer(), TimeSpan.FromSeconds(30));

        var refused = await Assert.ThrowsAsync<HttpRequestException>(
            () => client.GetAsync(ReleaseAddress, HttpCompletionOption.ResponseHeadersRead));
        var failure = await Assert.ThrowsAsync<UpdateFailedException>(
            () => service.DownloadAsync(ReleaseAddress, Destination, null, CancellationToken.None));

        Assert.Contains("could not be made ready", refused.Message);
        Assert.Equal(UpdateFailureStage.Download, failure.Stage);
        Assert.False(File.Exists(Destination));
    }
#endif

    [Fact]
    public async Task Cancelling_throws_cancellation_and_leaves_nothing()
    {
        var handler = new RoutedHandler(_ => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StreamContent(new StallingStream("MZ"u8.ToArray())),
        });
        var service = new UpdateDownloadService(handler, TimeSpan.FromSeconds(30));
        using var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(200));

        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => service.DownloadAsync(ReleaseAddress, Destination, null, cts.Token));

        Assert.False(File.Exists(Destination));
    }

    [Fact]
    public async Task A_server_error_answered_as_the_download_is_cancelled_throws_cancellation_and_leaves_nothing()
    {
        using var cts = new CancellationTokenSource();
        var handler = new RoutedHandler(_ =>
        {
            cts.Cancel();
            return new HttpResponseMessage(HttpStatusCode.ServiceUnavailable);
        });
        var service = new UpdateDownloadService(handler, TimeSpan.FromSeconds(30));

        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => service.DownloadAsync(ReleaseAddress, Destination, null, cts.Token));

        Assert.False(File.Exists(Destination));
    }

    [Fact]
    public async Task A_file_that_cannot_be_created_as_the_download_is_cancelled_is_a_failure_to_install()
    {
        using var cts = new CancellationTokenSource();
        var handler = new RoutedHandler(_ =>
        {
            cts.Cancel();
            return Body(Program());
        });
        var service = new UpdateDownloadService(handler, TimeSpan.FromSeconds(30));
        var nowhere = Path.Combine(_folder, "missing", "download.exe");

        var failed = await Assert.ThrowsAsync<UpdateFailedException>(
            () => service.DownloadAsync(ReleaseAddress, nowhere, null, cts.Token));

        Assert.Equal(UpdateFailureStage.Install, failed.Stage);
    }

    [Fact]
    public async Task A_connection_broken_off_by_cancelling_throws_cancellation_and_leaves_nothing()
    {
        var handler = new RoutedHandler(_ => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StreamContent(new StallingStream("MZ"u8.ToArray(), breaksOffOnCancel: true)),
        });
        var service = new UpdateDownloadService(handler, TimeSpan.FromSeconds(30));
        using var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(200));

        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => service.DownloadAsync(ReleaseAddress, Destination, null, cts.Token));

        Assert.False(File.Exists(Destination));
    }

    /// <summary>
    /// Reports synchronously, so the test reads every value once the call returns,
    /// where <see cref="Progress{T}"/> posts each to the thread pool.
    /// </summary>
    private sealed class SyncProgress(Action<double?> report) : IProgress<double?>
    {
        public void Report(double? value) => report(value);
    }

    /// <summary>
    /// Hands over its bytes, then waits on every later read until the read is
    /// cancelled, as a connection that has stopped sending does. Where
    /// <paramref name="breaksOffOnCancel"/> is set, the cancelled read throws an
    /// IOException, as a socket read the cancel breaks off can.
    /// </summary>
    private sealed class StallingStream(byte[] first, bool breaksOffOnCancel = false) : Stream
    {
        private bool _sent;

        public override async Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            if (!_sent)
            {
                _sent = true;
                if (count < first.Length)
                    throw new ArgumentException("The read is smaller than the bytes held.", nameof(count));
                Array.Copy(first, 0, buffer, offset, first.Length);
                return first.Length;
            }
            try
            {
                await FrameworkTask.Delay(Timeout.Infinite, cancellationToken);
            }
            catch (OperationCanceledException ex) when (breaksOffOnCancel)
            {
                throw new IOException("The read was broken off.", ex);
            }
            return 0;
        }

        public override int Read(byte[] buffer, int offset, int count) => throw new NotSupportedException();
        public override bool CanRead => true;
        public override bool CanSeek => false;
        public override bool CanWrite => false;
        public override long Length => throw new NotSupportedException();
        public override long Position { get => throw new NotSupportedException(); set => throw new NotSupportedException(); }
        public override void Flush() { }
        public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();
        public override void SetLength(long value) => throw new NotSupportedException();
        public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();
    }
}
