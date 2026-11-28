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
    /// The open asks for SYNCHRONIZE and MUTEX_MODIFY_STATE.
    ///
    /// An <see cref="MutexSample.Unestablished"/> answer carries the Windows error number
    /// the failure was raised for, where it was raised for one
    /// (<see cref="MutexSampling.WindowsError"/>).
    /// </summary>
    MutexSampling Sample(string name);
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
