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
}
