using InstallerClean.Services;

namespace InstallerClean.Tests.Helpers;

/// <summary>
/// Hand fake for <see cref="IMutexProbe"/> so the pending-reboot check's read of
/// <c>Global\_MSIExecute</c> can be driven without a real Windows named mutex. Each mode
/// is the answer <see cref="IMutexProbe.Sample"/> gives: the mutex is held by another
/// thread, the object's security refused the open, or nothing owns it.
/// </summary>
internal sealed class FakeMutexProbe : IMutexProbe
{
    internal enum Mode { HeldByAnother, NotHeld, AccessRefused }

    private readonly Mode _mode;

    public FakeMutexProbe(Mode mode) => _mode = mode;

    /// <summary>
    /// The sample the real probe takes of the same machine: a mutex another process
    /// holds reads as held and one whose security refuses the open reads as refused.
    /// Every other mode reads as not held.
    /// </summary>
    public MutexSampling Sample(string name) => _mode switch
    {
        Mode.HeldByAnother => MutexSample.Held,
        Mode.AccessRefused => MutexSample.AccessRefused,
        _ => MutexSample.NotHeld,
    };
}
