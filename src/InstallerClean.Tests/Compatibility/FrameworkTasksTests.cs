namespace InstallerClean.Tests.Compatibility;

/// <summary>
/// The task members the app calls as .NET 10 has them: an await that leaves the caller's
/// thread and context even on a completed task, a completion source that keeps its
/// continuations off the completing thread, and a registration released without waiting for
/// a callback that is running.
/// </summary>
public class FrameworkTasksTests
{
    private static readonly TimeSpan Bound = TimeSpan.FromSeconds(10);

    [Fact]
    public async Task ForceYielding_on_a_completed_task_resumes_on_the_pool_and_never_posts_to_the_context()
    {
        var context = new RecordingContext();
        var (onPool, posts) = await Task.Run(async () =>
        {
            SynchronizationContext.SetSynchronizationContext(context);
            var resumed = await ResumeAfterForcedYield().ConfigureAwait(false);
            return (resumed, context.Posts);
        });

        Assert.True(onPool);
        Assert.Equal(0, posts);
    }

    private static async Task<bool> ResumeAfterForcedYield()
    {
        await Task.CompletedTask.ConfigureAwait(ConfigureAwaitOptions.ForceYielding);
        return Thread.CurrentThread.IsThreadPoolThread && SynchronizationContext.Current is null;
    }

    [Fact]
    public async Task ForceYielding_with_the_captured_context_posts_the_continuation_to_it()
    {
        var context = new RecordingContext();
        var posts = await Task.Run(async () =>
        {
            SynchronizationContext.SetSynchronizationContext(context);
            await ResumeOnCapturedContext().ConfigureAwait(false);
            return context.Posts;
        });

        Assert.Equal(1, posts);
    }

    private static async Task ResumeOnCapturedContext() =>
        await Task.CompletedTask.ConfigureAwait(
            ConfigureAwaitOptions.ForceYielding | ConfigureAwaitOptions.ContinueOnCapturedContext);

    [Fact]
    public void ForceYielding_reports_a_completed_task_incomplete()
    {
        Assert.False(Task.CompletedTask.ConfigureAwait(ConfigureAwaitOptions.ForceYielding).GetAwaiter().IsCompleted);
        Assert.True(Task.CompletedTask.ConfigureAwait(ConfigureAwaitOptions.None).GetAwaiter().IsCompleted);
    }

    [Fact]
    public async Task SuppressThrowing_ends_the_await_on_a_faulted_task_without_throwing()
    {
        var faulted = Task.FromException(new InvalidOperationException("x"));

        await faulted.ConfigureAwait(ConfigureAwaitOptions.SuppressThrowing);

        Assert.True(faulted.IsFaulted);
    }

    [Fact]
    public void An_unknown_option_and_SuppressThrowing_on_a_task_with_a_result_are_refused()
    {
        Action unknownOption = () => Task.CompletedTask.ConfigureAwait((ConfigureAwaitOptions)8);
        Action suppressOnResult = () => Task.FromResult(1).ConfigureAwait(ConfigureAwaitOptions.SuppressThrowing);

        Assert.Throws<ArgumentOutOfRangeException>(unknownOption);
        var refused = Assert.Throws<ArgumentOutOfRangeException>(suppressOnResult);
        Assert.Equal("options", refused.ParamName);
    }

    [Fact]
    public async Task ForceYielding_on_a_task_with_a_result_gives_the_result()
    {
        Assert.Equal(7, await Task.FromResult(7).ConfigureAwait(ConfigureAwaitOptions.ForceYielding));
    }

    [Fact]
    public void RunContinuationsAsynchronously_keeps_the_continuation_off_the_completing_thread()
    {
        var source = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var completing = Environment.CurrentManagedThreadId;
        var ranOn = new TaskCompletionSource<int>();
        source.Task.ContinueWith(
            _ => ranOn.TrySetResult(Environment.CurrentManagedThreadId),
            TaskContinuationOptions.ExecuteSynchronously);

        source.SetResult();

        Assert.True(ranOn.Task.Wait(Bound));
        Assert.NotEqual(completing, ranOn.Task.Result);
    }

    [Fact]
    public void A_completion_source_completes_once_and_refuses_a_second_completion()
    {
        var source = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

        Assert.True(source.TrySetResult());
        Assert.False(source.TrySetResult());
        Assert.False(source.TrySetCanceled());
        Assert.Throws<InvalidOperationException>(() => source.SetResult());
        Assert.Throws<InvalidOperationException>(() => source.SetCanceled(CancellationToken.None));
        Assert.Equal(TaskStatus.RanToCompletion, source.Task.Status);
    }

    [Fact]
    public void A_completion_source_refuses_an_option_a_promise_task_cannot_take()
    {
        Assert.Throws<ArgumentOutOfRangeException>(
            () => new TaskCompletionSource(TaskCreationOptions.LongRunning));
    }

    [Fact]
    public void Unregister_removes_a_callback_that_has_not_run()
    {
        using var cts = new CancellationTokenSource();
        var ran = false;
        var registration = cts.Token.Register(() => ran = true);

        Assert.True(registration.Unregister());
        cts.Cancel();

        Assert.False(ran);
        Assert.False(default(CancellationTokenRegistration).Unregister());
    }

    [Fact]
    public void Unregister_returns_at_once_while_the_callback_runs_on_another_thread()
    {
        using var cts = new CancellationTokenSource();
        using var entered = new ManualResetEventSlim();
        using var release = new ManualResetEventSlim();
        var registration = cts.Token.Register(() =>
        {
            entered.Set();
            release.Wait(Bound);
        });
        var cancelling = Task.Run(cts.Cancel);
        try
        {
            Assert.True(entered.Wait(Bound));

            var unregistering = Task.Run(() => registration.Unregister());

            Assert.True(unregistering.Wait(TimeSpan.FromSeconds(2)), "Unregister waited for the running callback");
            Assert.False(unregistering.Result);
        }
        finally
        {
            release.Set();
            cancelling.Wait(Bound);
        }
    }

    private sealed class RecordingContext : SynchronizationContext
    {
        private int _posts;

        public int Posts => Volatile.Read(ref _posts);

        public override void Post(SendOrPostCallback d, object? state)
        {
            Interlocked.Increment(ref _posts);
            base.Post(d, state);
        }

        public override void Send(SendOrPostCallback d, object? state)
        {
            Interlocked.Increment(ref _posts);
            base.Send(d, state);
        }
    }
}
