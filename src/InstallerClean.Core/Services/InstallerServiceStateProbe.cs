using System.Runtime.InteropServices;
using InstallerClean.Helpers;
using InstallerClean.Interop.Native;

namespace InstallerClean.Services;

/// <summary>
/// Production <see cref="IInstallerServiceState"/>: asks the service control manager for
/// the state of <c>msiserver</c>.
///
/// EACH READ OPENS THE SERVICE AND CLOSES IT AGAIN, and no handle on it is kept between
/// reads. Windows does not remove a service that has been deleted while any handle on it
/// is open, so a handle the app kept would stand in the way of Windows Installer being
/// registered again (<c>msiexec /unregister</c>, then <c>/regserver</c>) for as long as
/// the app was open.
/// </summary>
internal sealed class InstallerServiceStateProbe : IInstallerServiceState
{
    /// <summary>The Windows Installer service's name.</summary>
    internal const string ServiceName = "msiserver";

    private readonly string _serviceName;

    /// <summary>Production constructor: the Windows Installer service.</summary>
    public InstallerServiceStateProbe() : this(ServiceName) { }

    /// <summary>Test constructor: the service <paramref name="serviceName"/> names.</summary>
    internal InstallerServiceStateProbe(string serviceName) => _serviceName = serviceName;

    public unsafe InstallerServiceRead Read()
    {
        try
        {
            using var manager = Advapi32.OpenSCManager(null, null, Advapi32.SC_MANAGER_CONNECT);
            if (manager.IsInvalid)
                return new InstallerServiceRead(InstallerServiceState.Unreadable, Marshal.GetLastWin32Error());

            using var service = Advapi32.OpenService(manager, _serviceName, Advapi32.SERVICE_QUERY_STATUS);
            if (service.IsInvalid)
            {
                var error = Marshal.GetLastWin32Error();
                return error == Advapi32.ERROR_SERVICE_DOES_NOT_EXIST
                    ? new InstallerServiceRead(InstallerServiceState.NotThere)
                    : new InstallerServiceRead(InstallerServiceState.Unreadable, error);
            }

            if (!Advapi32.QueryServiceStatusEx(
                    service, Advapi32.SC_STATUS_PROCESS_INFO, out var status,
                    (uint)sizeof(Advapi32.SERVICE_STATUS_PROCESS), out _))
                return new InstallerServiceRead(InstallerServiceState.Unreadable, Marshal.GetLastWin32Error());

            // Stopped is the one state with no engine in it: a service that is starting or
            // stopping is on its way to or from running one.
            return new InstallerServiceRead(status.dwCurrentState == Advapi32.SERVICE_STOPPED
                ? InstallerServiceState.Stopped
                : InstallerServiceState.NotStopped);
        }
        catch (Exception ex) when (ex is not OutOfMemoryException and not StackOverflowException)
        {
            CrashLog.TryWrite(ex);
            return new InstallerServiceRead(InstallerServiceState.Unreadable, WindowsErrors.Of(ex));
        }
    }
}
