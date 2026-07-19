using InstallerClean.Services;

namespace InstallerClean.Tests.Helpers;

/// <summary>
/// Hand fake for <see cref="IMutexProbe"/> so the action services'
/// <c>Global\_MSIExecute</c> hold can be driven without a real Windows named
/// mutex. Simulates the three
/// outcomes of <see cref="IMutexProbe.TryAcquire"/>: the mutex is held by another
/// (refuse), it was acquired (proceed, then release), or it could not be acquired
/// for another reason (fall back and proceed). Records how many times a lease was
/// taken and released so a test can assert the lease is released exactly once.
/// </summary>
internal sealed class FakeMutexProbe : IMutexProbe
{
    internal enum Mode { HeldByAnother, Acquire, FallBack }

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

    public FakeMutexProbe(Mode mode) => _mode = mode;

    public bool IsHeld(string name) => _mode == Mode.HeldByAnother;

    public IMutexLease? TryAcquire(string name, out bool heldByAnother)
    {
        AcquireAttempts++;
        heldByAnother = false;
        switch (_mode)
        {
            case Mode.HeldByAnother:
                heldByAnother = true;
                return null;
            case Mode.FallBack:
                return null;
            default:
                Acquired++;
                return new Lease(this);
        }
    }

    private sealed class Lease : IMutexLease
    {
        private readonly FakeMutexProbe _owner;
        public Lease(FakeMutexProbe owner) => _owner = owner;
        public void Dispose() => _owner.Released++;
    }
}
