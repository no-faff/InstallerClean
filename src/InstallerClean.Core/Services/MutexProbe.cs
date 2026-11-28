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
/// moment finds it as it would with no sample running.
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
}
