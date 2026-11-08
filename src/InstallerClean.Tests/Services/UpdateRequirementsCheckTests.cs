using System.Net;
using InstallerClean.Services;
using NSubstitute;

namespace InstallerClean.Tests.Services;

/// <summary>
/// An update's read of what the release's update.json requires
/// (<see cref="AppUpdater.CheckRequirementsAsync"/>) when the token is cancelled while it
/// runs, and with a time limit: a cancel is answered as a cancel wherever it comes, never
/// as a refusal or a read that failed; a file read in full is kept; and the time limit
/// covers the read of the file alone, running out as a read that failed.
/// </summary>
public sealed class UpdateRequirementsCheckTests
{
    [Fact]
    public async Task A_cancelled_check_against_a_kept_refusing_file_answers_the_cancel_not_the_refusal()
    {
        var updater = Updater(new Answers(HttpStatusCode.OK, RefusesEveryDownload));
        Assert.Equal(UpdateRefusal.CannotInstallFromThisCopy,
            await updater.CheckRequirementsAsync("9.9.9", null, CancellationToken.None));

        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => updater.CheckRequirementsAsync("9.9.9", null, new CancellationToken(canceled: true)));
    }

    [Fact]
    public async Task A_release_file_read_in_full_as_the_check_is_cancelled_is_kept()
    {
        using var cts = new CancellationTokenSource();
        var network = new Answers(HttpStatusCode.OK, AllowsEveryDownload, cancelAtBodysEnd: cts);
        var updater = Updater(network);

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => updater.CheckRequirementsAsync("9.9.9", null, cts.Token));

        Assert.Null(await updater.CheckRequirementsAsync("9.9.9", null, CancellationToken.None));
        Assert.Equal(1, network.Requests);
    }

    [Fact]
    public async Task A_server_error_as_the_check_is_cancelled_answers_the_cancel()
    {
        using var cts = new CancellationTokenSource();
        var updater = Updater(new Answers(HttpStatusCode.InternalServerError, null, cancelAsItAnswers: cts));

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => updater.CheckRequirementsAsync("9.9.9", null, cts.Token));
    }

    [Fact]
    public async Task A_check_cancelled_while_the_pc_is_read_for_dotnet_framework_answers_the_cancel_not_the_refusal()
    {
        using var cts = new CancellationTokenSource();
        var updater = Updater(new Answers(HttpStatusCode.OK, NeedsNetFramework48), CancelsAsItIsRead(cts));

        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => updater.CheckRequirementsAsync("9.9.9", null, cts.Token));
        Assert.Equal(UpdateRefusal.NeedsNetFramework48,
            await updater.CheckRequirementsAsync("9.9.9", null, CancellationToken.None));
    }

    [Fact]
    public async Task A_time_limit_that_runs_out_while_the_pc_is_read_for_dotnet_framework_keeps_the_refusal()
    {
        var registry = Substitute.For<IRegistryReader>();
        registry.LocalMachineDwordValue(Arg.Any<string>(), Arg.Any<string>()).Returns(_ =>
        {
            Thread.Sleep(TimeSpan.FromMilliseconds(2500));
            return new RegistryDwordRead(RegistryDwordState.Absent);
        });
        var updater = Updater(new Answers(HttpStatusCode.OK, NeedsNetFramework48), registry);

        Assert.Equal(UpdateRefusal.NeedsNetFramework48,
            await updater.CheckRequirementsAsync("9.9.9", TimeSpan.FromSeconds(2), CancellationToken.None));
    }

    // A timer is set for at most int.MaxValue milliseconds; CancelAfter throws for longer.
    public static TheoryData<TimeSpan> NoLimits =>
    [
        Timeout.InfiniteTimeSpan,
        TimeSpan.FromMilliseconds(int.MaxValue + 1L),
        TimeSpan.FromMilliseconds(uint.MaxValue - 1),
        TimeSpan.FromMilliseconds(uint.MaxValue),
        TimeSpan.MaxValue,
    ];

    [Theory]
    [MemberData(nameof(NoLimits))]
    public async Task An_infinite_limit_or_one_longer_than_a_timer_takes_reads_the_file(TimeSpan within)
    {
        // The answer takes a moment, so a limit taken as zero would give the read up first.
        var updater = Updater(new Answers(HttpStatusCode.OK, RefusesEveryDownload, delay: TimeSpan.FromMilliseconds(100)));

        Assert.Equal(UpdateRefusal.CannotInstallFromThisCopy,
            await updater.CheckRequirementsAsync("9.9.9", within, CancellationToken.None));
    }

    [Fact]
    public async Task The_longest_limit_a_timer_takes_reads_the_file()
    {
        var updater = Updater(new Answers(HttpStatusCode.OK, RefusesEveryDownload, delay: TimeSpan.FromMilliseconds(100)));

        Assert.Equal(UpdateRefusal.CannotInstallFromThisCopy, await updater.CheckRequirementsAsync(
            "9.9.9", TimeSpan.FromMilliseconds(int.MaxValue), CancellationToken.None));
    }

    [Fact]
    public void The_longest_limit_a_timer_takes_is_a_limit_and_anything_longer_is_none()
    {
        Assert.False(AppUpdater.IsNoLimit(TimeSpan.FromMilliseconds(int.MaxValue)));
        Assert.True(AppUpdater.IsNoLimit(TimeSpan.FromMilliseconds(int.MaxValue) + TimeSpan.FromTicks(1)));

        using var timer = new CancellationTokenSource();
        timer.CancelAfter(TimeSpan.FromMilliseconds(int.MaxValue));
    }

    [Fact]
    public async Task A_file_already_kept_answers_with_a_zero_limit()
    {
        var network = new Answers(HttpStatusCode.OK, RefusesEveryDownload);
        var updater = Updater(network);

        Assert.Equal(UpdateRefusal.CannotInstallFromThisCopy,
            await updater.CheckRequirementsAsync("9.9.9", null, CancellationToken.None));
        Assert.Equal(UpdateRefusal.CannotInstallFromThisCopy,
            await updater.CheckRequirementsAsync("9.9.9", TimeSpan.Zero, CancellationToken.None));
        Assert.Equal(1, network.Requests);
    }

    [Theory]
    [InlineData(0.5)]
    [InlineData(0)]
    [InlineData(-0.5)]
    [InlineData(-1.5)]
    [InlineData(-2)]
    public async Task A_limit_under_a_millisecond_fetches_nothing_and_fails_the_read(double milliseconds)
    {
        var network = new Answers(HttpStatusCode.OK, RefusesEveryDownload);
        var updater = Updater(network);

        var failed = await Assert.ThrowsAsync<UpdateFailedException>(() => updater.CheckRequirementsAsync(
            "9.9.9", TimeSpan.FromMilliseconds(milliseconds), CancellationToken.None));

        Assert.Equal(UpdateFailureStage.Download, failed.Stage);
        Assert.Equal(0, network.Requests);
    }

    [Fact]
    public async Task A_cancel_with_a_zero_limit_answers_the_cancel_and_sends_nothing()
    {
        var network = new Answers(HttpStatusCode.OK, RefusesEveryDownload);
        var updater = Updater(network);

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => updater.CheckRequirementsAsync(
            "9.9.9", TimeSpan.Zero, new CancellationToken(canceled: true)));
        Assert.Equal(0, network.Requests);
    }

    [Fact]
    public async Task A_read_that_runs_past_its_time_limit_is_a_read_that_failed_not_a_cancel()
    {
        var updater = Updater(new Stalls());

        var failed = await Assert.ThrowsAsync<UpdateFailedException>(
            () => updater.CheckRequirementsAsync("9.9.9", TimeSpan.FromMilliseconds(50), CancellationToken.None));

        Assert.Equal(UpdateFailureStage.Download, failed.Stage);
    }

    [Fact]
    public async Task An_update_cancelled_while_the_pc_is_read_for_dotnet_framework_answers_the_cancel_not_the_refusal()
    {
        using var cts = new CancellationTokenSource();
        var updater = Updater(new Answers(HttpStatusCode.OK, NeedsNetFramework48), CancelsAsItIsRead(cts));

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => updater.PrepareAsync("9.9.9", null, cts.Token));
    }

    /// <summary>An update.json that lists no download, so every copy is refused.</summary>
    private const string RefusesEveryDownload = """{"schema": 1, "downloads": {}}""";

    /// <summary>An update.json whose every download needs .NET Framework 4.8.</summary>
    private const string NeedsNetFramework48 =
        """{"schema": 1, "downloads": {"setup": {"requires": ["netfx-4.8"]}, "portable": {"requires": ["netfx-4.8"]}, "slim": {"requires": ["netfx-4.8"]}, "cli": {"requires": ["netfx-4.8"]}}}""";

    /// <summary>An update.json that lists every download with nothing required.</summary>
    private const string AllowsEveryDownload =
        """{"schema": 1, "downloads": {"setup": {"requires": []}, "portable": {"requires": []}, "slim": {"requires": []}, "cli": {"requires": []}}}""";

    /// <summary>A registry that cancels <paramref name="cts"/> as .NET Framework's release is read, and has none.</summary>
    private static IRegistryReader CancelsAsItIsRead(CancellationTokenSource cts)
    {
        var registry = Substitute.For<IRegistryReader>();
        registry.LocalMachineDwordValue(Arg.Any<string>(), Arg.Any<string>()).Returns(_ =>
        {
            cts.Cancel();
            return new RegistryDwordRead(RegistryDwordState.Absent);
        });
        return registry;
    }

    /// <summary>
    /// A portable copy's updater whose requests <paramref name="network"/> answers, and whose
    /// PC <paramref name="registry"/> reads. Nothing here reaches a write.
    /// </summary>
    private static AppUpdater Updater(HttpMessageHandler network, IRegistryReader? registry = null) =>
        new(AppCopyKind.Portable, Path.Combine(Path.GetTempPath(), "InstallerClean.exe"),
            new UpdateDownloadService(network, null), Path.GetTempPath(), AppUpdater.WindowSetupArguments,
            UpdateStagingFolder.AdministratorsOnly, registry ?? Substitute.For<IRegistryReader>());

    /// <summary>
    /// Answers every request with <paramref name="status"/> and <paramref name="body"/>, and
    /// counts the requests. Cancels <paramref name="cancelAsItAnswers"/> as it answers, and
    /// <paramref name="cancelAtBodysEnd"/> once the body's last byte has been read. Waits
    /// <paramref name="delay"/> first, ended by the request's own cancel.
    /// </summary>
    private sealed class Answers(
        HttpStatusCode status, string? body,
        CancellationTokenSource? cancelAsItAnswers = null, CancellationTokenSource? cancelAtBodysEnd = null,
        TimeSpan? delay = null)
        : HttpMessageHandler
    {
        public int Requests { get; private set; }

        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Requests++;
            if (delay is { } wait) await Task.Delay(wait, cancellationToken);
            cancelAsItAnswers?.Cancel();
            var bytes = System.Text.Encoding.UTF8.GetBytes(body ?? string.Empty);
            return new HttpResponseMessage(status)
            {
                Content = cancelAtBodysEnd is null
                    ? new ByteArrayContent(bytes)
                    : new StreamContent(new CancelsAtEnd(bytes, cancelAtBodysEnd)),
            };
        }
    }

    /// <summary>Answers no request until it is cancelled.</summary>
    private sealed class Stalls : HttpMessageHandler
    {
        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request, CancellationToken cancellationToken)
        {
            await Task.Delay(Timeout.InfiniteTimeSpan, cancellationToken);
            throw new InvalidOperationException("A delay without end ended.");
        }
    }

    /// <summary>A body that cancels <paramref name="cts"/> when a read finds nothing left.</summary>
    private sealed class CancelsAtEnd(byte[] bytes, CancellationTokenSource cts) : MemoryStream(bytes)
    {
        public override async Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            var read = await base.ReadAsync(buffer, offset, count, cancellationToken);
            if (read == 0) cts.Cancel();
            return read;
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            var read = base.Read(buffer, offset, count);
            if (read == 0) cts.Cancel();
            return read;
        }
    }
}
