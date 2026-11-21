using System.Reflection;

namespace System.Threading;

/// <summary>
/// CancellationTokenRegistration.Unregister, which .NET Framework 4.8 does not have, answering
/// as .NET 10's does, so <c>registration.Unregister()</c> binds here unchanged.
/// </summary>
/// <remarks>
/// It removes the callback where it has not started and returns true. Where the callback has
/// run or is running, or the registration is empty, it returns false at once and never waits
/// for a running callback, which Dispose does. .NET Framework performs exactly that removal in
/// its internal TryDeregister, the first step of its own Dispose, and this calls it by name.
/// The member is looked up at the first call and kept, never in a static initialiser, so a .NET
/// Framework without it fails each call with MissingMethodException and nothing else.
/// </remarks>
internal static class FrameworkCancellationTokenRegistration
{
    internal const string TryDeregisterName = "TryDeregister";

    private static MethodInfo? s_tryDeregister;

    extension(CancellationTokenRegistration registration)
    {
        public bool Unregister()
        {
            var tryDeregister = s_tryDeregister ??= FindTryDeregister()
                ?? throw new MissingMethodException(nameof(CancellationTokenRegistration), TryDeregisterName);
            return (bool)tryDeregister.Invoke(registration, null)!;
        }
    }

    /// <summary>.NET Framework's internal <c>bool TryDeregister()</c>, or null where it has none.</summary>
    internal static MethodInfo? FindTryDeregister()
    {
        var method = typeof(CancellationTokenRegistration).GetMethod(
            TryDeregisterName, BindingFlags.Instance | BindingFlags.NonPublic, null, Type.EmptyTypes, null);
        return method?.ReturnType == typeof(bool) ? method : null;
    }
}
