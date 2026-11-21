namespace InstallerClean.Tests.Helpers;

/// <summary>
/// Sets an event a stand-in waits on when the test leaves the scope, by an assert that
/// fails as well as by its own end, so a stand-in held on it never outlives the test.
/// <c>using var _ = new SetOnExit(release);</c> after the event is made.
/// </summary>
internal sealed class SetOnExit : IDisposable
{
    private readonly ManualResetEventSlim _gate;

    public SetOnExit(ManualResetEventSlim gate) => _gate = gate;

    public void Dispose() => _gate.Set();
}
