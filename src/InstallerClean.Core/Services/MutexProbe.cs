using InstallerClean.Helpers;
using InstallerClean.Interop.Native;
using System.Runtime.InteropServices;
using System.Threading;

namespace InstallerClean.Services;

/// <summary>
/// Production IMutexProbe. Existence alone is the wrong signal for
/// Global\_MSIExecute: a named mutex exists for as long as any process holds a
/// handle to it, owned or not, so an existence check reads "installer busy"
/// whenever something merely has the object open. <see cref="Sample"/> reads
/// ownership itself, and reads it without waiting on the mutex, so a sample never
/// owns the object and a Windows Installer client that asks for it at the same
/// moment finds it as it would with no sample running. <see cref="TryAcquire"/>
/// takes the mutex with a zero wait and holds it.
/// </summary>
internal sealed class MutexProbe : IMutexProbe
{
    public MutexSampling Sample(string name)
    {
        try
        {
            if (!Mutex.TryOpenExisting(name, out var mutex))
                return MutexSample.NotHeld;

            using (mutex)
            {
                // The state is read off the handle and nothing waits on it, so this
                // thread never owns the mutex. Do not turn this into a zero wait:
                // a wait that succeeds owns Windows Installer's mutex until it is
                // released, and a Windows Installer client that asks for it in that
                // time finds it taken.
                var status = Ntdll.NtQueryMutant(
                    mutex.SafeWaitHandle, Ntdll.MutantBasicInformation, out var state,
                    (uint)Marshal.SizeOf<Ntdll.MUTANT_BASIC_INFORMATION>(), IntPtr.Zero);
                if (status < Ntdll.STATUS_SUCCESS)
                    return new MutexSampling(
                        MutexSample.Unestablished, unchecked((int)Ntdll.RtlNtStatusToDosError(status)));

                // Owned by a thread other than this one. An abandoned mutex has no
                // owner, the one that held it having ended, so nothing is installing.
                // One this thread owns is one a zero wait would take again.
                return state.CurrentCount <= 0 && state.OwnedByCaller == 0
                    ? MutexSample.Held
                    : MutexSample.NotHeld;
            }
        }
        catch (UnauthorizedAccessException)
        {
            // The object exists and its security refuses this process the
            // rights to open it, so ownership was never sampled. The gate
            // closes on this as it does on a held mutex, under a reason of
            // its own, because nothing here has seen anything hold the
            // object: all that is known is that the app was not allowed to
            // look.
            return MutexSample.AccessRefused;
        }
        catch (Exception ex) when (ex is not OutOfMemoryException and not StackOverflowException)
        {
            // Nothing was learned about the mutex, which the gate holds on
            // as it does on a signal it could not read, giving the Windows
            // error where the failure was raised for one. OOM and
            // StackOverflow are not caught at all.
            return new MutexSampling(MutexSample.Unestablished, WindowsErrors.Of(ex));
        }
    }

    public IMutexLease? TryAcquire(string name, out MutexAcquireOutcome outcome)
    {
        outcome = MutexAcquireOutcome.NotAcquired;

        // Create-or-open: if _MSIExecute does not exist (no install running,
        // or the Installer service lingering with the object gone), we create
        // it and take the lock, so for the batch the object exists and reads
        // owned to anything that opens it by name, Windows Installer's own
        // client included. If it exists we open it and measure ownership with
        // a zero wait.
        //
        // Creating it is the COMMON path, not the rare one: Windows Installer
        // creates _MSIExecute when an install begins and drops it when the
        // install ends, so between installs the object is absent. That means
        // this process usually owns the object's security descriptor for the
        // batch, and with no explicit one passed it takes the default DACL from
        // this (elevated) process's token, rather than the wider one the
        // Installer service would have given it.
        //
        // Left at the default. An explicit permissive DACL would hand every
        // logged-on user the right to take the machine's installer mutex for as
        // long as they cared to hold it. Nothing here depends on what a process
        // the default DACL refuses does when it cannot open the object. The
        // object dies with the last handle, so the window is the batch, not the
        // session.
        Mutex mutex;
        try
        {
            mutex = new Mutex(initiallyOwned: false, name);
        }
        catch (UnauthorizedAccessException)
        {
            // The object exists but its DACL refuses us create/open rights, so
            // ownership was never sampled. The probe reports and the callers
            // decide, which is why this returns a null lease under its own
            // outcome rather than choosing for them: nothing has been shown to
            // hold the object, and that fact alone does not settle whether an act
            // should run without the hold.
            //
            // It travels apart from NotAcquired because the two are different
            // facts about the machine: this one is a setting on the object, which
            // is a thing an administrator can go and look at, and the callers say
            // so in their own words rather than telling the user the lock was
            // busy.
            //
            // Sample opens with the same rights this constructor asks for, so a
            // refusal already standing when the pending-reboot gate runs stops the
            // run there, and this arm meets only one that began after it.
            //
            // Both callers refuse on it. A file this app has moved out of the cache
            // is as absent from it as one this app has deleted, so an installer
            // transaction that starts mid-batch fails to find it either way, and
            // the recovery a move leaves the user is not a reason to run unheld.
            // Each states its own reasoning at its own acquire; neither belongs
            // here, and this method still reports rather than decides, because the
            // answer it hands back is the same one either way.
            outcome = MutexAcquireOutcome.AccessRefused;
            return null;
        }
        catch (Exception ex) when (ex is not OutOfMemoryException and not StackOverflowException)
        {
            return null;
        }

        bool acquired;
        try
        {
            // Abandoned: the previous owner died while holding it; ownership
            // transferred to this thread, so nothing is installing and this thread now
            // holds it. The wait dispatches nothing (HandleWait).
            acquired = HandleWait.One(mutex.SafeWaitHandle, TimeSpan.Zero) is not HandleWaitResult.TimedOut;
        }
        catch (Exception ex) when (ex is not OutOfMemoryException and not StackOverflowException)
        {
            mutex.Dispose();
            return null;
        }

        if (!acquired)
        {
            outcome = MutexAcquireOutcome.HeldByAnother;
            mutex.Dispose();
            return null;
        }

        outcome = MutexAcquireOutcome.Acquired;
        return new MutexLease(mutex);
    }

    /// <summary>
    /// Holds an acquired mutex; releases it on Dispose. Dispose must run on the
    /// acquiring thread (Win32 owner-thread rule); the callers arrange that by
    /// disposing inside the same synchronous worker-thread body that acquired it.
    /// </summary>
    private sealed class MutexLease : IMutexLease
    {
        private readonly Mutex _mutex;
        public MutexLease(Mutex mutex) => _mutex = mutex;

        public void Dispose()
        {
            try
            {
                _mutex.ReleaseMutex();
            }
            catch (Exception ex) when (ex is not OutOfMemoryException and not StackOverflowException)
            {
                // A release that throws (wrong thread, already released) must not
                // take the process down after a committed batch; the mutex is
                // freed when the handle closes below regardless.
            }
            _mutex.Dispose();
        }
    }
}
