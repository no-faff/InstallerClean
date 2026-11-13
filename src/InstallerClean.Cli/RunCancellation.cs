namespace InstallerClean.Cli;

/// <summary>
/// The run's cancellation source, cancelled on the thread that brings the cancel (a thread-pool
/// thread for Ctrl+C, Windows' own handler thread for a console being closed) and disposed by
/// Main as the run returns. Cancel and Dispose take one lock, so they never
/// overlap.
/// </summary>
/// <remarks>
/// On .NET Framework 4.8 a Cancel that overlaps a Dispose can throw NullReferenceException or
/// ObjectDisposedException after the source has begun cancelling, and its registered callbacks
/// then do not run; the source's wait handle being made, as the run's waits make it, opens that
/// window. .NET 10 allows the overlap. A Cancel after the Dispose throws ObjectDisposedException,
/// as it does on .NET 10. A Dispose that comes while Cancel is running the callbacks waits for
/// them, so a callback on the run's token must not wait for anything Main does after the run
/// returns, or Main and the callback each wait for the other.
/// </remarks>
internal sealed class RunCancellation : IDisposable
{
    private readonly CancellationTokenSource _source = new();
    private readonly object _gate = new();

    /// <summary>The run's token, read before the source is disposed.</summary>
    public CancellationToken Token { get; }

    public RunCancellation() => Token = _source.Token;

    public bool IsCancellationRequested => _source.IsCancellationRequested;

    /// <summary>Cancels the run, or throws ObjectDisposedException where the source is disposed.</summary>
    public void Cancel()
    {
        lock (_gate) _source.Cancel();
    }

    public void Dispose()
    {
        lock (_gate) _source.Dispose();
    }
}
