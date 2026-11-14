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
        // A wake that comes while a pool thread is free shows nothing about a full pool, since a
        // wake that needs a pool thread can be served by it. A work item an earlier test left
        // running can end during the wait and free a thread, so an attempt that ended with one
        // free is made again. A wake that did not come, or came late, fails in either case.
        const int attempts = 5;
        for (var attempt = 1; ; attempt++)
        {
            var source = new TaskCompletionSource<int>(TaskCreationOptions.RunContinuationsAsynchronously);
            Task[] tasks = [source.Task, new TaskCompletionSource<int>().Task];
            int woken;
            long elapsed;
            bool stillFull;

            using (var pool = new BusyPool())
            {
                var completer = new Thread(() =>
                {
                    Thread.Sleep(100);
                    source.TrySetResult(1);
                }) { IsBackground = true };
                var clock = Stopwatch.StartNew();
                completer.Start();

                woken = FrameworkTask.WaitAny(tasks, 5_000, CancellationToken.None);
                elapsed = clock.ElapsedMilliseconds;
                stillFull = pool.StillFull;
            }

            Assert.Equal(0, woken);
            Assert.True(elapsed < 2_500, $"woke after {elapsed} ms");
            if (stillFull) return;
            Assert.True(attempt < attempts,
                $"a pool thread came free during the wait in each of {attempts} attempts, so the wait showed nothing");
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
    /// Holds every worker thread the pool may start until disposed. The pool's maximum is set
    /// to the lowest the pool takes that still leaves a thread free, and a work item blocks on
    /// a gate in every thread left free, the test's own thread among the busy ones where it is
    /// a pool thread. A thread counts against the maximum while it is alive, idle or working,
    /// and SetMaxThreads takes a maximum below the threads alive, so the free count read after
    /// each cap is what settles it. The minimum is then raised to the cap, so that no work
    /// item waits for the pool to add a thread, and both are put back on disposal.
    /// </summary>
    private sealed class BusyPool : IDisposable
    {
        private readonly ManualResetEventSlim _gate = new();
        private readonly int _oldWorkers;
        private readonly int _oldPorts;
        private int _oldMinWorkers;
        private int _oldMinPorts;

        public BusyPool()
        {
            ThreadPool.GetMaxThreads(out _oldWorkers, out _oldPorts);
            ThreadPool.GetMinThreads(out var minWorkers, out _oldMinPorts);
            _oldMinWorkers = minWorkers;

            // Work items still running from earlier tests are given up to ten seconds to end,
            // so the cap can stay near the minimum.
            var waited = Stopwatch.StartNew();
            while (waited.ElapsedMilliseconds < 10_000)
            {
                ThreadPool.GetAvailableThreads(out var freeNow, out _);
                if (_oldWorkers - freeNow < minWorkers) break;
                Thread.Sleep(50);
            }

            // From the minimum up, one at a time, to the first cap leaving a thread free.
            var free = 0;
            var cap = minWorkers;
            for (; cap <= _oldWorkers; cap++)
            {
                if (!ThreadPool.SetMaxThreads(cap, _oldPorts)) continue;
                ThreadPool.GetAvailableThreads(out free, out _);
                if (free > 0) break;
            }
            if (free <= 0)
            {
                Dispose();
                Assert.Fail("the pool has no free thread to fill");
            }

            // THE MINIMUM IS RAISED TO THE CAP so that each work item below gets a thread at once.
            // The pool starts a thread on demand only up to its minimum, and beyond that it waits
            // for a running item to end or adds a thread at its own pace, so how long the pool
            // took to fill would depend on the host. With the minimum at the cap there is no such
            // wait, and the fill takes as long as starting the threads.
            if (!ThreadPool.SetMinThreads(cap, _oldMinPorts))
            {
                Dispose();
                Assert.Fail($"the pool's minimum would not be raised to its cap of {cap}");
            }

            // The pool is full once no thread is free, whichever work items fill it: an item
            // an earlier test queued can take a thread ahead of these, and holds it as these do.
            // As many items are queued as the cap allows threads, so that one an earlier test
            // left running and that ends takes the next of these in its place, and the pool
            // stays full.
            for (var i = 0; i < cap; i++)
                ThreadPool.QueueUserWorkItem(_ =>
                {
                    Interlocked.Increment(ref _started);
                    _gate.Wait();
                });
            var filling = Stopwatch.StartNew();
            while (!StillFull && filling.ElapsedMilliseconds < 10_000)
                Thread.Sleep(20);
            if (!StillFull)
            {
                ThreadPool.GetAvailableThreads(out var freeAtEnd, out _);
                Dispose();
                Assert.Fail($"the pool did not fill: minimum {minWorkers}, cap {cap}, {free} free after the cap, "
                    + $"{Volatile.Read(ref _started)} of {cap} work items started, {freeAtEnd} free after 10 s");
            }
        }

        private int _started;

        /// <summary>
        /// True where no pool thread is free: a work item the test does not hold that ends
        /// frees one, and a wake then shows nothing about a busy pool.
        /// </summary>
        public bool StillFull
        {
            get
            {
                ThreadPool.GetAvailableThreads(out var free, out _);
                return free <= 0;
            }
        }

        public void Dispose()
        {
            _gate.Set();
            ThreadPool.SetMaxThreads(_oldWorkers, _oldPorts);
            ThreadPool.SetMinThreads(_oldMinWorkers, _oldMinPorts);
        }
    }
}

/// <summary>Keeps the test that fills the thread pool from running beside others.</summary>
[CollectionDefinition(nameof(ThreadPoolCollection), DisableParallelization = true)]
public class ThreadPoolCollection;
