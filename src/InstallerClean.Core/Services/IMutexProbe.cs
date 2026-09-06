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
    ///   <item><c>null</c> with <see cref="MutexAcquireOutcome.HeldByAnother"/>
    ///   when the mutex is held by someone else: the caller should refuse the
    ///   operation;</item>
    ///   <item><c>null</c> with <see cref="MutexAcquireOutcome.AccessRefused"/>
    ///   when the object exists and its security refused this process the rights
    ///   to open it, so nothing was learned about whether anyone holds it;</item>
    ///   <item><c>null</c> with <see cref="MutexAcquireOutcome.NotAcquired"/>
    ///   when the acquire failed for any other non-fatal reason.</item>
    /// </list>
    /// Only <see cref="MutexAcquireOutcome.HeldByAnother"/> is a measurement, an
    /// opened mutex a zero wait failed to take. The other two say this process
    /// could not find out, which is never the same as finding out that nothing
    /// holds it. Every caller that means to act on the cache refuses on all
    /// three.
    /// </summary>
    IMutexLease? TryAcquire(string name, out MutexAcquireOutcome outcome);
}

/// <summary>
/// How an attempt to take a named mutex ended. The three refusals are kept apart
/// because they are different facts about the machine and a caller reports them
/// to the user in different words: one says something is installing, one says the
/// app was not allowed to look, and one says the attempt failed.
///
/// A member added here reaches the callers' final arm, which refuses with the
/// general wording. A member that needs to tell the user something else takes an
/// arm of its own, which is the failure worth having.
/// </summary>
public enum MutexAcquireOutcome
{
    /// <summary>The mutex was taken and the caller now owns it.</summary>
    Acquired,

    /// <summary>
    /// The mutex was opened and a zero wait failed to take it, so another thread
    /// holds it. The one outcome here that is a positive observation.
    /// </summary>
    HeldByAnother,

    /// <summary>
    /// The object exists and its security descriptor refused this process the
    /// rights to open it, so ownership was never sampled. Distinct from
    /// <see cref="NotAcquired"/> because the cause is a setting on the object
    /// rather than a condition that arose while asking.
    /// </summary>
    AccessRefused,

    /// <summary>
    /// The acquire failed for some other non-fatal reason and nothing was shown
    /// to be holding the mutex.
    /// </summary>
    NotAcquired,
}

/// <summary>
/// A held named mutex. Disposing it releases the mutex; it MUST be disposed on
/// the same thread that acquired it (Win32 owner-thread rule).
/// </summary>
public interface IMutexLease : IDisposable
{
}
