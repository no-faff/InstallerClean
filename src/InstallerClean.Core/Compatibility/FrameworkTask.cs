using System.Collections.Generic;

namespace System.Threading.Tasks;

/// <summary>
/// Task.WaitAny as .NET 10 times it. On .NET Framework 4.8 the runtime's own WaitAny is woken
/// by a completion action, and a task made with RunContinuationsAsynchronously queues every
/// completion action to the thread pool, so its wake waits for a free pool thread. .NET 10
/// runs that wake on the completing thread.
/// </summary>
internal static class FrameworkTask
{
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
    // WaitHandle.WaitAny's limit.
    private const int MaxHandles = 64;

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
}
