using InstallerClean.Services;

namespace InstallerClean.Tests.Helpers;

/// <summary>
/// Hand fake for <see cref="IMutexProbe"/> so the action services'
/// <c>Global\_MSIExecute</c> hold can be driven without a real Windows named
/// mutex. Simulates the outcomes of
/// <see cref="IMutexProbe.TryAcquire"/>, named for what the probe returns rather
/// than for what a caller does with it: the mutex was acquired, it is held by
/// another process, the object's security refused the open, or the acquire failed
/// some other way with nothing shown to be holding it. Records how many times a
/// lease was taken and released so a test can assert the lease is released
/// exactly once.
/// </summary>
internal sealed class FakeMutexProbe : IMutexProbe
{
    internal enum Mode { HeldByAnother, Acquire, AccessRefused, RefusedNotHeld }

    private readonly Mode _mode;
    public int Acquired { get; private set; }
    public int Released { get; private set; }

    /// <summary>
    /// How many times the acquire was ATTEMPTED, whichever way it went.
    /// <see cref="Acquired"/> counts only the attempts that took a lease, so it
    /// cannot tell "the caller never asked" from "the caller asked and was
    /// refused", which is the whole question for a test pinning what runs
    /// before the hold is taken.
    /// </summary>
    public int AcquireAttempts { get; private set; }

    /// <summary>
    /// The managed thread the last lease was taken on, and the one it was released
    /// on; null where that has not happened yet. Recorded because the real
    /// <c>_MSIExecute</c> lease has a rule the counters above cannot see: Win32
    /// requires the owning thread to release a mutex, and the production
    /// <c>MutexLease.Dispose</c> swallows the <c>ApplicationException</c> a wrong-thread
    /// release raises, so a batch that hopped threads mid-hold still reports one
    /// acquire, one release and no error while leaving the machine-wide installer
    /// mutex held until the process exits.
    /// </summary>
    public int? AcquiredOnThread { get; private set; }

    /// <inheritdoc cref="AcquiredOnThread"/>
    public int? ReleasedOnThread { get; private set; }

    public FakeMutexProbe(Mode mode) => _mode = mode;

    public bool IsHeld(string name) => _mode == Mode.HeldByAnother;

    public IMutexLease? TryAcquire(string name, out MutexAcquireOutcome outcome)
    {
        AcquireAttempts++;
        switch (_mode)
        {
            case Mode.HeldByAnother:
                outcome = MutexAcquireOutcome.HeldByAnother;
                return null;
            case Mode.AccessRefused:
                outcome = MutexAcquireOutcome.AccessRefused;
                return null;
            case Mode.RefusedNotHeld:
                outcome = MutexAcquireOutcome.NotAcquired;
                return null;
            default:
                outcome = MutexAcquireOutcome.Acquired;
                Acquired++;
                AcquiredOnThread = Environment.CurrentManagedThreadId;
                return new Lease(this);
        }
    }

    private sealed class Lease : IMutexLease
    {
        private readonly FakeMutexProbe _owner;
        public Lease(FakeMutexProbe owner) => _owner = owner;
        public void Dispose()
        {
            _owner.Released++;
            _owner.ReleasedOnThread = Environment.CurrentManagedThreadId;
        }
    }
}
