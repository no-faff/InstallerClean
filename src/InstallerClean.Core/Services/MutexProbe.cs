using System.Threading;

namespace InstallerClean.Services;

/// <summary>
/// Production IMutexProbe: a zero-timeout acquire-and-release.
/// Existence alone is the wrong signal for Global\_MSIExecute: the
/// Windows Installer service lingers for several minutes after its
/// last job and can keep the object alive unheld, so an existence
/// check reads "installer busy" long after the install finished.
/// Acquiring with a zero wait measures ownership itself; on success
/// the mutex is released immediately on the same thread (the instant
/// of ownership briefly serialises a starting msiexec, which is the
/// object's documented purpose).
/// </summary>
internal sealed class MutexProbe : IMutexProbe
{
    public bool IsHeld(string name)
    {
        try
        {
            if (!Mutex.TryOpenExisting(name, out var mutex))
                return false;

            using (mutex)
            {
                bool acquired = false;
                try
                {
                    acquired = mutex.WaitOne(0);
                    return !acquired;
                }
                catch (AbandonedMutexException)
                {
                    // The previous owner died while holding it; ownership
                    // transferred to this thread, so nothing is installing.
                    acquired = true;
                    return false;
                }
                finally
                {
                    // Release on the acquiring thread, inside the same
                    // call; Win32 requires the owning thread to release.
                    if (acquired) mutex.ReleaseMutex();
                }
            }
        }
        catch (UnauthorizedAccessException)
        {
            // The object exists but its DACL refuses SYNCHRONIZE; held
            // cannot be ruled out, so the gate stays closed.
            return true;
        }
        catch (Exception ex) when (ex is not OutOfMemoryException and not StackOverflowException)
        {
            // PendingRebootService treats false as "not blocked".
            // Folding OOM into that return would silently route a real
            // "MSI install in flight" condition through the gate as
            // Clean, so only benign failures take this path.
            return false;
        }
    }

    public IMutexLease? TryAcquire(string name, out bool shownHeldByAnother)
    {
        shownHeldByAnother = false;

        // Create-or-open: if _MSIExecute does not exist (no install running,
        // or the Installer service lingering with the object gone), we create
        // it and take the lock, so a msiexec starting mid-batch opens the same
        // named object and waits on us. If it exists we open it and measure
        // ownership with a zero wait.
        //
        // Creating it is the COMMON path, not the rare one: Windows Installer
        // creates _MSIExecute when an install begins and drops it when the
        // install ends, so between installs the object is absent. That means
        // this process usually owns the object's security descriptor for the
        // batch, and with no explicit one passed it takes the default DACL from
        // this (elevated) process's token, rather than the wider one the
        // Installer service would have given it.
        //
        // Left at the default deliberately. A token default DACL grants SYSTEM,
        // and the installs that write the cache this app is protecting run
        // through the Windows Installer service as SYSTEM, so they can still
        // open the object and serialise against it, which is the whole point of
        // the hold. Passing an explicit permissive DACL to cover the remaining
        // case (a non-elevated per-user install, which does not write the
        // machine cache anyway) would hand every logged-on user the right to
        // take the machine's installer mutex and stall every install on the box
        // for as long as they cared to hold it. A stray refusal on a per-user
        // install for the length of one batch is the better end of that trade.
        // The object dies with the last handle, so the window is the batch, not
        // the session.
        Mutex mutex;
        try
        {
            mutex = new Mutex(initiallyOwned: false, name);
        }
        catch (UnauthorizedAccessException)
        {
            // The object exists but its DACL refuses us create/open rights. The
            // probe reports and the callers decide, which is why this returns a
            // null lease with shownHeldByAnother left false rather than choosing for
            // them: nothing has been shown to hold the object, and that fact
            // alone does not settle whether an act should run without the hold.
            //
            // Both callers refuse on it. A file this app has moved out of the cache
            // is as absent from it as one this app has deleted, so an installer
            // transaction that starts mid-batch fails to find it either way, and
            // the recovery a move leaves the user is not a reason to run unheld.
            // Each states its own reasoning at its own acquire; neither belongs
            // here, and this method still reports rather than decides, because the
            // answer it hands back is the same one either way.
            return null;
        }
        catch (Exception ex) when (ex is not OutOfMemoryException and not StackOverflowException)
        {
            return null;
        }

        bool acquired;
        try
        {
            acquired = mutex.WaitOne(0);
        }
        catch (AbandonedMutexException)
        {
            // The previous owner died while holding it; ownership transferred to
            // this thread, so nothing is installing and we now hold it.
            acquired = true;
        }
        catch (Exception ex) when (ex is not OutOfMemoryException and not StackOverflowException)
        {
            mutex.Dispose();
            return null;
        }

        if (!acquired)
        {
            shownHeldByAnother = true;
            mutex.Dispose();
            return null;
        }

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
