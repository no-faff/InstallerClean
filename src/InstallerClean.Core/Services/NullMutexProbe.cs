namespace InstallerClean.Services;

/// <summary>
/// No-op <see cref="IMutexProbe"/> for the action-service test constructors that
/// do not exercise the P1 mutex hold. <see cref="TryAcquire"/> always reports
/// "could not acquire, fall back", so a service built with it behaves exactly as
/// it did before P1: it proceeds without holding <c>Global\_MSIExecute</c> and
/// never refuses on the mutex. Production always gets the real
/// <see cref="MutexProbe"/> through DI.
/// </summary>
internal sealed class NullMutexProbe : IMutexProbe
{
    internal static readonly NullMutexProbe Instance = new();

    public bool IsHeld(string name) => false;

    public IMutexLease? TryAcquire(string name, out bool heldByAnother)
    {
        heldByAnother = false;
        return null;
    }
}
