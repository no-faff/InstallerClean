using System.Collections.Generic;

namespace System.Threading.Tasks;

/// <summary>
/// Task.WaitAny, and Task.Delay with a token, as .NET 10 times them. On .NET Framework 4.8 the
/// runtime's own WaitAny is woken by a completion action, and a task made with
/// RunContinuationsAsynchronously queues every completion action to the thread pool, so its wake
/// waits for a free pool thread; .NET 10 runs that wake on the completing thread. A Task.Delay
/// that a cancel ends runs its continuations inside the cancel on .NET Framework 4.8, where
/// .NET 10 queues them.
/// </summary>
internal static class FrameworkTask
{
    // WaitHandle.WaitAny's limit.
    private const int MaxHandles = 64;

    /// <summary>
    /// Waits for any of <paramref name="tasks"/> to complete, as .NET 10's
    /// Task.WaitAny(Task[], int, CancellationToken) does: the index of a completed task, or -1
    /// where none completes within <paramref name="millisecondsTimeout"/>.
    /// </summary>
    /// <remarks>
    /// The wait is on each task's wait handle, which the completing thread sets before it runs
    /// or queues any continuation. Where a task had completed before the call, the lowest such
    /// index is returned, as on .NET 10. Where two complete during one wait, this returns the
    /// lower index of the two and .NET 10 the first to complete. More than 64 distinct tasks, or 64
    /// with a token that can cancel, throws NotSupportedException, where .NET 10 waits. A cancel of
    /// <paramref name="cancellationToken"/> before the call, or one seen when the wait wakes,
    /// throws OperationCanceledException, a task completing at the same moment included.
    /// </remarks>
    public static int WaitAny(Task[] tasks, int millisecondsTimeout, CancellationToken cancellationToken)
    {
        if (tasks is null) throw new ArgumentNullException(nameof(tasks));
        if (millisecondsTimeout < -1) throw new ArgumentOutOfRangeException(nameof(millisecondsTimeout));

        cancellationToken.ThrowIfCancellationRequested();

        var signalled = -1;
        for (var i = 0; i < tasks.Length; i++)
        {
            var task = tasks[i] ?? throw new ArgumentException(
                "The tasks array included at least one null element.", nameof(tasks));
            if (signalled == -1 && task.IsCompleted) signalled = i;
        }

        if (signalled != -1 || tasks.Length == 0) return signalled;

        // One handle per distinct task, since a wait handle may appear only once in a wait, each
        // standing for the first index that task holds.
        var first = new List<int>(tasks.Length);
        for (var i = 0; i < tasks.Length; i++)
            if (Array.IndexOf(tasks, tasks[i]) == i) first.Add(i);

        WaitHandle? stop = null;
        if (cancellationToken.CanBeCanceled)
        {
            try
            {
                stop = cancellationToken.WaitHandle;
            }
            catch (ObjectDisposedException)
            {
                // A disposed source can no longer cancel its token, and .NET 10 waits on the
                // tasks alone.
            }
        }

        var count = first.Count + (stop is null ? 0 : 1);
        if (count > MaxHandles)
            throw new NotSupportedException($"FrameworkTask.WaitAny waits on at most {MaxHandles} handles, the token's among them.");

        var handles = new WaitHandle[count];
        for (var i = 0; i < first.Count; i++)
            handles[i] = ((IAsyncResult)tasks[first[i]]).AsyncWaitHandle;
        if (stop is not null) handles[first.Count] = stop;

        var woken = WaitHandle.WaitAny(handles, millisecondsTimeout);

        // A cancel seen at the wake throws, even where a task completed at the same moment, so
        // a caller's cancelled wait always ends in the throw.
        cancellationToken.ThrowIfCancellationRequested();
        if (woken == WaitHandle.WaitTimeout) return -1;
        if (woken >= first.Count) throw new OperationCanceledException(cancellationToken);
        return first[woken];
    }

    /// <summary>
    /// A task that completes after <paramref name="millisecondsDelay"/>, or as cancelled with
    /// <paramref name="cancellationToken"/> where the token cancels first, as .NET 10's
    /// Task.Delay(int, CancellationToken) does. -1 waits until the cancel.
    /// </summary>
    /// <remarks>
    /// The cancel completes the task inside the call that cancels, and every continuation is
    /// queued, never run inside that call, as on .NET 10. A delay ending by its time queues its
    /// continuations too, where .NET 10 runs them on the timer's thread.
    /// </remarks>
    public static Task Delay(int millisecondsDelay, CancellationToken cancellationToken)
    {
        if (millisecondsDelay < -1) throw new ArgumentOutOfRangeException(nameof(millisecondsDelay));
        return DelayCore((uint)millisecondsDelay, cancellationToken);
    }

    /// <summary>
    /// <see cref="Delay(int, CancellationToken)"/> for a <see cref="TimeSpan"/>, taken in whole
    /// milliseconds from -1 to <see cref="uint.MaxValue"/> less one, as .NET 10 takes it.
    /// </summary>
    public static Task Delay(TimeSpan delay, CancellationToken cancellationToken)
    {
        var milliseconds = (long)delay.TotalMilliseconds;
        if (milliseconds < -1 || milliseconds > uint.MaxValue - 1)
            throw new ArgumentOutOfRangeException(nameof(delay));
        return DelayCore((uint)milliseconds, cancellationToken);
    }

    private static Task DelayCore(uint millisecondsDelay, CancellationToken cancellationToken)
    {
        if (cancellationToken.IsCancellationRequested) return Task.FromCanceled(cancellationToken);
        if (millisecondsDelay == 0) return Task.CompletedTask;

        return new DelayPromise(millisecondsDelay, cancellationToken).Task;
    }

    /// <summary>
    /// One delay: a timer that completes it, and a registration on the token that cancels it.
    /// Whichever comes first disposes both.
    /// </summary>
    private sealed class DelayPromise
    {
        private readonly TaskCompletionSource<bool> _source =
            new(TaskCreationOptions.RunContinuationsAsynchronously);
        private readonly CancellationToken _token;
        private readonly Timer? _timer;
        private readonly CancellationTokenRegistration _registration;

        public DelayPromise(uint millisecondsDelay, CancellationToken cancellationToken)
        {
            _token = cancellationToken;
            // Neither the timer nor the registration carries the caller's execution context, as
            // .NET 10's do not.
            using (ExecutionContext.SuppressFlow())
            {
                if (millisecondsDelay != uint.MaxValue)
                    _timer = new Timer(static state => ((DelayPromise)state!).Complete(), this,
                        (long)millisecondsDelay, Timeout.Infinite);

                if (cancellationToken.CanBeCanceled)
                    _registration = cancellationToken.Register(
                        static state => ((DelayPromise)state!).Cancel(), this);
            }

            if (_source.Task.IsCompleted) Cleanup();
        }

        public Task Task => _source.Task;

        private void Complete()
        {
            if (_source.TrySetResult(true)) Cleanup();
        }

        private void Cancel()
        {
            if (_source.TrySetCanceled(_token)) Cleanup();
        }

        private void Cleanup()
        {
            _timer?.Dispose();
            _registration.Dispose();
        }
    }
}
