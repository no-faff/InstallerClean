namespace InstallerClean.Services;

/// <summary>Probes a named system mutex: whether it is held, and whether this process may open it.</summary>
public interface IMutexProbe
{
    /// <summary>
    /// Samples the named mutex: reads whether a thread owns it, without waiting on it,
    /// so the sample never owns the mutex. Never creates it.
    /// <see cref="MutexSample.Held"/> when it exists and another thread owns it.
    /// <see cref="MutexSample.AccessRefused"/> when it exists and its security
    /// refuses this process the rights to open it, so whether anything owns it was
    /// never sampled. <see cref="MutexSample.Unestablished"/> when the probe fails any
    /// other non-fatal way, so nothing about the mutex was learned.
    /// <see cref="MutexSample.NotHeld"/> for a mutex that is not there, one that is
    /// there and unowned, and an abandoned one.
    ///
    /// The open asks for exactly the rights <see cref="TryAcquire"/> asks for, so an
    /// object that refuses one refuses the other.
    ///
    /// An <see cref="MutexSample.Unestablished"/> answer carries the Windows error number
    /// the failure was raised for, where it was raised for one
    /// (<see cref="MutexSampling.WindowsError"/>).
    /// </summary>
    MutexSampling Sample(string name);

    /// <summary>
    /// Acquires the named mutex with a zero wait, creating it if it does not
    /// exist, so the caller can HOLD it for the duration of an operation and
    /// release it via the returned lease. The acquire is a measurement as well as
    /// a hold: it fails where a live install owns <c>Global\_MSIExecute</c>, which
    /// is what every caller refuses on, and while the lease is held the object reads
    /// owned to anything that samples it.
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
/// A sample's answer (<see cref="State"/>) and, where the sample failed on an error
/// Windows reported, that error's number (<see cref="WindowsError"/>), which is null on
/// every other answer.
///
/// A <see cref="MutexSample"/> converts to one carrying no number, which is every answer
/// but a failure on a Windows error.
/// </summary>
public readonly record struct MutexSampling(MutexSample State, int? WindowsError = null)
{
    public static implicit operator MutexSampling(MutexSample state) => new(state);
}

/// <summary>
/// What a sample of a named mutex found. The three answers that stop a
/// caller are kept apart because they are different facts about the machine: one
/// says another thread owns the mutex, one says this process was not allowed to
/// look, and one says the look failed.
///
/// NotHeld stays the first member: it is what <c>default(MutexSample)</c> and an
/// unconfigured test substitute return, and every other member stops a caller. A
/// member added here blocks <see cref="PendingRebootService"/> as
/// <see cref="Unestablished"/> does until it takes an arm of its own there.
/// </summary>
public enum MutexSample
{
    /// <summary>
    /// Nothing owns the mutex: it is not there, it is there and unowned, or it was
    /// abandoned.
    /// </summary>
    NotHeld,

    /// <summary>The mutex was opened and its state read as owned by another thread.</summary>
    Held,

    /// <summary>
    /// The object exists and its security descriptor refused this process the rights
    /// to open it, so whether anything owns it was never sampled.
    /// </summary>
    AccessRefused,

    /// <summary>
    /// The probe failed some other non-fatal way, so whether anything owns the mutex
    /// was not established. A read of the state that Windows refuses lands here too.
    /// </summary>
    Unestablished,
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
