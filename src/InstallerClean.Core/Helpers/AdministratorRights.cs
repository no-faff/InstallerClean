using System.Security.Principal;

namespace InstallerClean.Helpers;

/// <summary>
/// Whether this process holds administrator rights, meaning its token carries the
/// Administrators group enabled. Both hosts ask before anything scans, and without
/// the rights they refuse to scan.
///
/// WHAT THE SCAN ASKS IS WHAT RESTS ON IT. The scan asks Windows Installer about products
/// and patches across every account on the machine, and Microsoft documents that asking
/// across accounts needs administrator privileges. An administrator is shown a product
/// installed for another account like any other. Each product key that no installation in
/// those answers matches by account and code is read off the registry
/// (<see cref="Services.InstallerQueryService.UnlistedEntries"/>).
///
/// ENABLED MEMBERSHIP, NOT MEMBERSHIP. IsInRole checks the token with
/// CheckTokenMembership, which counts the group only where it is enabled. Under UAC,
/// an administrator's process started without elevation carries the group deny-only,
/// so it gets false, as a standard account does. An elevated process gets true, and
/// so does one running as LocalSystem, whose token Microsoft documents as including
/// BUILTIN\Administrators, so the command line run as SYSTEM by a scheduled task or a
/// management agent passes.
///
/// Windows neither elevates a running process nor takes its elevation away, so the
/// answer taken at startup holds for every scan the process makes.
/// </summary>
internal static class AdministratorRights
{
    public static bool Held()
    {
        using var identity = WindowsIdentity.GetCurrent();
        return new WindowsPrincipal(identity).IsInRole(WindowsBuiltInRole.Administrator);
    }
}
