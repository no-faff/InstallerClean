using System.Security.AccessControl;
using System.Threading;

namespace InstallerClean.Services;

/// <summary>Production IMutexProbe via MutexAcl.OpenExisting with READ_CONTROL only.</summary>
internal sealed class MutexProbe : IMutexProbe
{
    public bool Exists(string name)
    {
        try
        {
            // READ_CONTROL is sufficient to test for existence; MUTEX_MODIFY_STATE
            // and SYNCHRONIZE are not needed and could be denied by a tighter DACL.
            // Mutex.OpenExisting(string) requests both implicitly, so the AccessControl
            // overload is the route that maps to bare OpenMutexW(READ_CONTROL).
            using var m = MutexAcl.OpenExisting(name, MutexRights.ReadPermissions);
            return true;
        }
        catch (WaitHandleCannotBeOpenedException)
        {
            return false;
        }
        catch (UnauthorizedAccessException)
        {
            // Object exists, access denied: counts as exists.
            return true;
        }
        catch (Exception ex) when (ex is not OutOfMemoryException and not StackOverflowException)
        {
            // PendingRebootService treats Exists==false as "not blocked";
            // an OOM here would silently route a real "MSI install in
            // flight" condition through the gate as Clean. Narrowing
            // matches SettingsService.Load's documented pattern: known
            // failure modes return false; OOM/SOH propagate.
            return false;
        }
    }
}
