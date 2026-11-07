using System.Runtime.CompilerServices;

namespace System.Threading.Tasks;

/// <summary>
/// The options .NET 10's Task.ConfigureAwait takes, with .NET 10's values, for the extension
/// members in <see cref="FrameworkConfigureAwait"/>.
/// </summary>
[Flags]
internal enum ConfigureAwaitOptions
{
    /// <summary>No option: the same as ConfigureAwait(false).</summary>
    None = 0x0,

    /// <summary>The continuation goes back to the caller's context: the same as ConfigureAwait(true).</summary>
    ContinueOnCapturedContext = 0x1,

    /// <summary>A faulted or cancelled Task ends the await without throwing. Task alone, never Task&lt;TResult&gt;.</summary>
    SuppressThrowing = 0x2,

    /// <summary>An await on a Task already complete suspends as if it were not.</summary>
    ForceYielding = 0x4,
}

/// <summary>
/// Task.ConfigureAwait(ConfigureAwaitOptions), which .NET Framework 4.8 does not have,
/// answering as .NET 10's does, so <c>await task.ConfigureAwait(ConfigureAwaitOptions.ForceYielding)</c>
/// binds here unchanged.
/// </summary>
/// <remarks>
/// The awaiter reports the task incomplete whenever ForceYielding is set, and schedules the
/// continuation through .NET Framework's own ConfigureAwait(bool) awaiter, passing only the
/// ContinueOnCapturedContext bit. Without that bit a continuation on a completed task is queued
/// to the thread pool and never posted to the caller's SynchronizationContext, as on .NET 10.
/// Task.Yield cannot stand in for ForceYielding: it posts to a WPF dispatcher's context, so the
/// rest of the method would run on the window's thread.
/// </remarks>
internal static class FrameworkConfigureAwait
{
    private const ConfigureAwaitOptions AllOptions =
        ConfigureAwaitOptions.ContinueOnCapturedContext |
        ConfigureAwaitOptions.SuppressThrowing |
        ConfigureAwaitOptions.ForceYielding;

    private const string SuppressThrowingOnTaskOfT =
        "Task<TResult>.ConfigureAwait does not support ConfigureAwaitOptions.SuppressThrowing. " +
        "To suppress throwing, instead cast the Task<TResult> to its base class Task and await that with SuppressThrowing.";

    extension(Task task)
    {
        /// <summary>An awaitable for <paramref name="options"/>; any other bit throws ArgumentOutOfRangeException.</summary>
        internal OptionsTaskAwaitable ConfigureAwait(ConfigureAwaitOptions options)
        {
            if ((options & ~AllOptions) != 0)
                throw new ArgumentOutOfRangeException(nameof(options));
            return new OptionsTaskAwaitable(task, options);
        }
    }

    extension<TResult>(Task<TResult> task)
    {
        /// <summary>
        /// An awaitable for <paramref name="options"/> that gives the task's result.
        /// SuppressThrowing throws ArgumentOutOfRangeException, as on .NET 10.
        /// </summary>
        internal OptionsTaskAwaitable<TResult> ConfigureAwait(ConfigureAwaitOptions options)
        {
            if ((options & ~(ConfigureAwaitOptions.ContinueOnCapturedContext | ConfigureAwaitOptions.ForceYielding)) != 0)
            {
                throw (options & ConfigureAwaitOptions.SuppressThrowing) == 0
                    ? new ArgumentOutOfRangeException(nameof(options))
                    : new ArgumentOutOfRangeException(nameof(options), SuppressThrowingOnTaskOfT);
            }
            return new OptionsTaskAwaitable<TResult>(task, options);
        }
    }

    internal readonly struct OptionsTaskAwaitable(Task task, ConfigureAwaitOptions options)
    {
        public Awaiter GetAwaiter() => new(task, options);

        public readonly struct Awaiter(Task task, ConfigureAwaitOptions options) : ICriticalNotifyCompletion
        {
            public bool IsCompleted => (options & ConfigureAwaitOptions.ForceYielding) == 0 && task.IsCompleted;

            public void OnCompleted(Action continuation) =>
                task.ConfigureAwait(ContinueOnCapturedContext(options)).GetAwaiter().OnCompleted(continuation);

            public void UnsafeOnCompleted(Action continuation) =>
                task.ConfigureAwait(ContinueOnCapturedContext(options)).GetAwaiter().UnsafeOnCompleted(continuation);

            /// <summary>
            /// Waits for the task where it has not completed, then throws as an await does, unless
            /// SuppressThrowing is set, in which case the fault is marked observed and nothing is thrown.
            /// </summary>
            public void GetResult()
            {
                if ((options & ConfigureAwaitOptions.SuppressThrowing) == 0)
                {
                    task.GetAwaiter().GetResult();
                    return;
                }

                if (!task.IsCompleted)
                    ((IAsyncResult)task).AsyncWaitHandle.WaitOne();
                _ = task.Exception;
            }
        }
    }

    internal readonly struct OptionsTaskAwaitable<TResult>(Task<TResult> task, ConfigureAwaitOptions options)
    {
        public Awaiter GetAwaiter() => new(task, options);

        public readonly struct Awaiter(Task<TResult> task, ConfigureAwaitOptions options) : ICriticalNotifyCompletion
        {
            public bool IsCompleted => (options & ConfigureAwaitOptions.ForceYielding) == 0 && task.IsCompleted;

            public void OnCompleted(Action continuation) =>
                task.ConfigureAwait(ContinueOnCapturedContext(options)).GetAwaiter().OnCompleted(continuation);

            public void UnsafeOnCompleted(Action continuation) =>
                task.ConfigureAwait(ContinueOnCapturedContext(options)).GetAwaiter().UnsafeOnCompleted(continuation);

            public TResult GetResult() => task.GetAwaiter().GetResult();
        }
    }

    private static bool ContinueOnCapturedContext(ConfigureAwaitOptions options) =>
        (options & ConfigureAwaitOptions.ContinueOnCapturedContext) != 0;
}
