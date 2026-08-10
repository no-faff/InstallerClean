namespace InstallerClean.Services;

/// <summary>Probes whether a named system mutex is currently held, without creating it.</summary>
public interface IMutexProbe
{
    /// <summary>
    /// True when the named mutex exists and is currently owned by some
    /// thread. Never creates the mutex. An existing mutex whose DACL
    /// refuses the probe counts as held (held cannot be ruled out);
    /// a missing mutex and other failures count as not held.
    /// </summary>
    bool IsHeld(string name);

    /// <summary>
    /// Acquires the named mutex with a zero wait, creating it if it does not
    /// exist, so the caller can HOLD it for the duration of an operation and
    /// release it via the returned lease. This converts a sample of
    /// <c>Global\_MSIExecute</c> into real mutual exclusion: a msiexec starting
    /// while the lease is held waits on the mutex instead of racing the cache.
    ///
    /// Returns:
    /// <list type="bullet">
    ///   <item>a non-null lease when acquired: the caller now OWNS the mutex and
    ///   MUST dispose the lease on the SAME thread that called this (Win32
    ///   requires the acquiring thread to release);</item>
    ///   <item><c>null</c> with <paramref name="heldByAnother"/> = <c>true</c>
    ///   when the mutex is held by someone else: the caller should refuse the
    ///   operation;</item>
    ///   <item><c>null</c> with <paramref name="heldByAnother"/> = <c>false</c>
    ///   when the mutex could not be acquired for any other reason (a DACL that
    ///   refuses creation/open, a transient failure). The false is "not shown to
    ///   be held", never "not held": this process could not find out. Both
    ///   callers refuse on it, and a new caller that means to act on the cache
    ///   should too.</item>
    /// </list>
    /// The flag is a positive signal only. True is a measurement, an opened
    /// mutex a zero wait failed to take; false is the absence of one, and the
    /// two are not opposites.
    /// </summary>
    IMutexLease? TryAcquire(string name, out bool heldByAnother);
}

/// <summary>
/// A held named mutex. Disposing it releases the mutex; it MUST be disposed on
/// the same thread that acquired it (Win32 owner-thread rule).
/// </summary>
public interface IMutexLease : IDisposable
{
}
