namespace InstallerClean.Services;

/// <summary>
/// No-op <see cref="IMutexProbe"/> for the action-service test constructors that
/// do not exercise the mutex hold. It hands out a lease that owns nothing, so a
/// service built with it proceeds exactly as one holding the real
/// <c>Global\_MSIExecute</c> would and never refuses on the mutex. Production
/// always gets the real <see cref="MutexProbe"/> through DI.
/// </summary>
/// <remarks>
/// It used to report "could not acquire, nothing shown to be holding it"
/// instead, which meant the same thing back when both services ran on without
/// the hold. Both refuse that answer now, so reporting it here would have every
/// test built through those constructors exercise the refusal path rather than
/// the move or delete it was written for, and pass or fail for a reason
/// unrelated to its subject.
///
/// So the fall-back is deliberately NOT what this stands for. A test that means
/// to drive an acquire failure says so with <c>FakeMutexProbe</c>, whose three
/// modes name the three outcomes; this one stands for "the mutex is not what
/// this test is about".
/// </remarks>
internal sealed class NullMutexProbe : IMutexProbe
{
    internal static readonly NullMutexProbe Instance = new();

    public bool IsHeld(string name) => false;

    public IMutexLease? TryAcquire(string name, out bool shownHeldByAnother)
    {
        shownHeldByAnother = false;
        return NullLease.Instance;
    }

    /// <summary>A lease over nothing. Disposing it releases nothing, there being nothing held.</summary>
    private sealed class NullLease : IMutexLease
    {
        internal static readonly NullLease Instance = new();
        public void Dispose() { }
    }
}
