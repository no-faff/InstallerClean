using System.Collections.Generic;

namespace System.Threading.Tasks;

/// <summary>
/// The non-generic TaskCompletionSource .NET 10 has and .NET Framework 4.8 does not, over
/// .NET Framework's TaskCompletionSource&lt;object?&gt;, whose Task it hands out as a Task.
/// </summary>
/// <remarks>
/// The creation options go to .NET Framework's promise task, which accepts AttachedToParent and
/// RunContinuationsAsynchronously, the set .NET 10 accepts, and throws ArgumentOutOfRangeException
/// for any other. With RunContinuationsAsynchronously, a continuation never runs on the thread that
/// completes the task. Every Try member that loses a race waits until the task has completed
/// before returning false, and every Set member throws InvalidOperationException where its Try
/// member returns false.
/// </remarks>
internal class TaskCompletionSource
{
    private const string AlreadyCompleted =
        "An attempt was made to transition a task to a final state when it had already completed.";

    private readonly TaskCompletionSource<object?> _source;

    public TaskCompletionSource() => _source = new TaskCompletionSource<object?>();

    public TaskCompletionSource(TaskCreationOptions creationOptions) =>
        _source = new TaskCompletionSource<object?>(creationOptions);

    public TaskCompletionSource(object? state) => _source = new TaskCompletionSource<object?>(state);

    public TaskCompletionSource(object? state, TaskCreationOptions creationOptions) =>
        _source = new TaskCompletionSource<object?>(state, creationOptions);

    public Task Task => _source.Task;

    public void SetException(Exception exception)
    {
        if (!TrySetException(exception)) throw new InvalidOperationException(AlreadyCompleted);
    }

    public void SetException(IEnumerable<Exception> exceptions)
    {
        if (!TrySetException(exceptions)) throw new InvalidOperationException(AlreadyCompleted);
    }

    public bool TrySetException(Exception exception) => _source.TrySetException(exception);

    public bool TrySetException(IEnumerable<Exception> exceptions) => _source.TrySetException(exceptions);

    public void SetResult()
    {
        if (!TrySetResult()) throw new InvalidOperationException(AlreadyCompleted);
    }

    public bool TrySetResult() => _source.TrySetResult(null);

    public void SetCanceled() => SetCanceled(default);

    public void SetCanceled(CancellationToken cancellationToken)
    {
        if (!TrySetCanceled(cancellationToken)) throw new InvalidOperationException(AlreadyCompleted);
    }

    public bool TrySetCanceled() => TrySetCanceled(default);

    public bool TrySetCanceled(CancellationToken cancellationToken) => _source.TrySetCanceled(cancellationToken);
}
