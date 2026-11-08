using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace InstallerClean.Tests.Helpers;

/// <summary>
/// <see cref="FrameworkTask.WaitAny"/> answers as .NET 10's Task.WaitAny(Task[], int,
/// CancellationToken) does, and wakes when a task made with RunContinuationsAsynchronously
/// completes even while every thread-pool thread is busy, which .NET Framework's own WaitAny
/// does not. <see cref="FrameworkTask.Delay(int, CancellationToken)"/> answers as .NET 10's
/// Task.Delay with a token does, and a cancel that ends it queues its continuations.
/// </summary>
[Collection(nameof(ThreadPoolCollection))]
public class FrameworkTaskTests
{
    [Fact]
    public void The_wait_wakes_while_every_pool_thread_is_busy()
    {
        var source = new TaskCompletionSource<int>(TaskCreationOptions.RunContinuationsAsynchronously);
        Task[] tasks = [source.Task, new TaskCompletionSource<int>().Task];

        using (new BusyPool())
        {
            var completer = new Thread(() =>
            {
                Thread.Sleep(100);
                source.TrySetResult(1);
            }) { IsBackground = true };
            var clock = Stopwatch.StartNew();
            completer.Start();

            var woken = FrameworkTask.WaitAny(tasks, 5_000, CancellationToken.None);

            Assert.Equal(0, woken);
            Assert.True(clock.ElapsedMilliseconds < 2_500, $"woke after {clock.ElapsedMilliseconds} ms");
        }
    }

    [Fact]
    public void The_lowest_index_already_completed_is_returned()
    {
        Task[] tasks = [new TaskCompletionSource<int>().Task, Task.CompletedTask, Task.CompletedTask];

        Assert.Equal(1, FrameworkTask.WaitAny(tasks, 0, CancellationToken.None));
    }

    [Fact]
    public void A_wait_no_task_ends_gives_minus_one()
    {
        using var cts = new CancellationTokenSource();
        Task[] tasks = [new TaskCompletionSource<int>().Task];

        Assert.Equal(-1, FrameworkTask.WaitAny(tasks, 50, cts.Token));
        Assert.Equal(-1, FrameworkTask.WaitAny(tasks, 0, CancellationToken.None));
    }

    [Fact]
    public void A_token_cancelled_before_the_call_throws_even_where_a_task_has_completed()
    {
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        Assert.Throws<OperationCanceledException>(
            () => FrameworkTask.WaitAny([Task.CompletedTask], 0, cts.Token));
    }

    [Fact]
    public void A_cancel_during_the_wait_throws()
    {
        using var cts = new CancellationTokenSource(100);
        Task[] tasks = [new TaskCompletionSource<int>().Task];

        var thrown = Assert.Throws<OperationCanceledException>(
            () => FrameworkTask.WaitAny(tasks, 5_000, cts.Token));
        Assert.Equal(cts.Token, thrown.CancellationToken);
    }

    [Fact]
    public void A_task_named_twice_is_waited_on_and_its_first_index_returned()
    {
        var source = new TaskCompletionSource<int>(TaskCreationOptions.RunContinuationsAsynchronously);
        var other = new TaskCompletionSource<int>().Task;
        Task[] tasks = [other, source.Task, other, source.Task];
        _ = Task.Delay(50).ContinueWith(_ => source.TrySetResult(1));

        Assert.Equal(1, FrameworkTask.WaitAny(tasks, 5_000, CancellationToken.None));
    }

    [Fact]
    public void A_token_whose_source_is_disposed_is_waited_past()
    {
        var cts = new CancellationTokenSource();
        var token = cts.Token;
        cts.Dispose();

        Assert.Equal(-1, FrameworkTask.WaitAny([new TaskCompletionSource<int>().Task], 20, token));
    }

    [Fact]
    public void An_empty_array_gives_minus_one_at_once()
    {
        Assert.Equal(-1, FrameworkTask.WaitAny([], Timeout.Infinite, CancellationToken.None));
    }

    [Fact]
    public void Bad_arguments_throw_as_on_dotnet_10()
    {
        Assert.Throws<ArgumentNullException>(() => FrameworkTask.WaitAny(null!, 0, CancellationToken.None));
        Assert.Throws<ArgumentOutOfRangeException>(
            () => FrameworkTask.WaitAny([Task.CompletedTask], -2, CancellationToken.None));
        Assert.Throws<ArgumentException>(
            () => FrameworkTask.WaitAny([Task.CompletedTask, null!], 0, CancellationToken.None));
    }

    [Fact]
    public async Task A_cancelled_delay_does_not_run_its_continuation_inside_the_cancel()
    {
        using var cts = new CancellationTokenSource();
        var delay = FrameworkTask.Delay(Timeout.Infinite, cts.Token);
        var cancelling = Environment.CurrentManagedThreadId;
        var ranInside = false;
        var continuation = delay.ContinueWith(
            _ => ranInside = Environment.CurrentManagedThreadId == cancelling,
            CancellationToken.None, TaskContinuationOptions.ExecuteSynchronously, TaskScheduler.Default);

        cts.Cancel();
        var seenAtCancel = Volatile.Read(ref ranInside);

        Assert.True(delay.IsCanceled);
        Assert.False(seenAtCancel);
        await continuation;
    }

    [Fact]
    public async Task A_cancelled_delay_carries_the_token()
    {
        using var cts = new CancellationTokenSource(50);

        var thrown = await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => FrameworkTask.Delay(TimeSpan.FromSeconds(30), cts.Token));
        Assert.Equal(cts.Token, thrown.CancellationToken);
    }

    [Fact]
    public void A_delay_with_a_token_already_cancelled_is_cancelled_at_once()
    {
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        Assert.True(FrameworkTask.Delay(100, cts.Token).IsCanceled);
        Assert.True(FrameworkTask.Delay(0, cts.Token).IsCanceled);
    }

    [Fact]
    public void A_zero_delay_has_completed_when_it_returns()
    {
        using var cts = new CancellationTokenSource();

        Assert.Equal(TaskStatus.RanToCompletion, FrameworkTask.Delay(0, cts.Token).Status);
        Assert.Equal(TaskStatus.RanToCompletion, FrameworkTask.Delay(TimeSpan.Zero, cts.Token).Status);
    }

    [Fact]
    public async Task A_delay_ends_after_its_time()
    {
        using var cts = new CancellationTokenSource();
        var clock = Stopwatch.StartNew();

        await FrameworkTask.Delay(TimeSpan.FromMilliseconds(100), cts.Token);

        // The timer may fire up to one system clock tick early, as a stopwatch measures it.
        Assert.True(clock.ElapsedMilliseconds >= 80, $"ended after {clock.ElapsedMilliseconds} ms");
    }

    [Fact]
    public void A_delay_out_of_range_throws_as_on_dotnet_10()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => { _ = FrameworkTask.Delay(-2, CancellationToken.None); });
        Assert.Throws<ArgumentOutOfRangeException>(
            () => { _ = FrameworkTask.Delay(TimeSpan.FromMilliseconds(-2), CancellationToken.None); });
        Assert.Throws<ArgumentOutOfRangeException>(
            () => { _ = FrameworkTask.Delay(TimeSpan.FromMilliseconds(uint.MaxValue), CancellationToken.None); });

        using var cts = new CancellationTokenSource();
        var endless = FrameworkTask.Delay(TimeSpan.FromMilliseconds(-1), cts.Token);
        Assert.Equal(TaskStatus.WaitingForActivation, endless.Status);
        cts.Cancel();
        Assert.True(endless.IsCanceled);
    }

    /// <summary>
    /// Holds every worker thread the pool may start until disposed: the pool's maximum is set
    /// to its minimum, and a work item blocks on a gate in every thread left free, the test's
    /// own thread among the busy ones where it is a pool thread.
    /// </summary>
    private sealed class BusyPool : IDisposable
    {
        private readonly ManualResetEventSlim _gate = new();
        private readonly int _oldWorkers;
        private readonly int _oldPorts;

        public BusyPool()
        {
            ThreadPool.GetMaxThreads(out _oldWorkers, out _oldPorts);
            ThreadPool.GetMinThreads(out var minWorkers, out _);
            Assert.True(ThreadPool.SetMaxThreads(minWorkers, _oldPorts));
            ThreadPool.GetAvailableThreads(out var free, out _);
            if (free <= 0)
            {
                Dispose();
                Assert.Fail("the pool has no free thread to fill");
            }

            var started = new CountdownEvent(free);
            for (var i = 0; i < free; i++)
                ThreadPool.QueueUserWorkItem(_ =>
                {
                    started.Signal();
                    _gate.Wait();
                });
            if (!started.Wait(10_000))
            {
                Dispose();
                Assert.Fail("the pool's threads did not all start");
            }
        }

        public void Dispose()
        {
            _gate.Set();
            ThreadPool.SetMaxThreads(_oldWorkers, _oldPorts);
        }
    }
}

/// <summary>Keeps the test that fills the thread pool from running beside others.</summary>
[CollectionDefinition(nameof(ThreadPoolCollection), DisableParallelization = true)]
public class ThreadPoolCollection;
