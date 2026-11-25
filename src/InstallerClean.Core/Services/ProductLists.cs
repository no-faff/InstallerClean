using Microsoft.Win32;
using InstallerClean.Helpers;
using InstallerClean.Interop;

namespace InstallerClean.Services;

/// <summary>
/// The lists of product keys Windows Installer walks when it enumerates the installed
/// programs of one installation context, read by name in the order Windows gives them.
/// The product walk reads one of these by name only where its own walk of that list
/// stopped at an entry that did not read, and it relies on the names only where the rows
/// the walk returned before the stop are, in order, the codes those names unpack to
/// (<see cref="InstallerQueryService"/>, <c>SettleAgainstList</c>).
/// </summary>
internal interface IProductLists
{
    /// <summary>
    /// The accounts holding a list of their own in <paramref name="context"/>, which is
    /// per user managed or per user unmanaged. Null where they could not be read.
    /// </summary>
    IReadOnlyList<string>? Accounts(MsiInstallContext context, CancellationToken ct);

    /// <summary>
    /// The key names in the list the walk of <paramref name="context"/> for
    /// <paramref name="sid"/> reads, in the order Windows gives them: empty where the list
    /// is not there, null where it would not read. <paramref name="sid"/> is null for the
    /// per-machine list.
    /// </summary>
    IReadOnlyList<string>? Names(MsiInstallContext context, string? sid, CancellationToken ct);
}

/// <summary>
/// The lists as the registry holds them: the per-machine list under
/// <c>HKLM\SOFTWARE\Classes\Installer\Products</c>; a managed account's under
/// <c>Installer\Managed\&lt;account&gt;\Installer\Products</c>; an unmanaged account's in
/// that account's own hive while it is loaded, and its records under
/// <c>Installer\UserData\&lt;account&gt;\Products</c> while it is not.
/// </summary>
internal sealed class RegistryProductLists : IProductLists
{
    internal static readonly RegistryProductLists Instance = new();

    private const string InstallerKey = @"SOFTWARE\Microsoft\Windows\CurrentVersion\Installer";
    private const string MachineList = @"SOFTWARE\Classes\Installer\Products";
    private const string UserList = @"Software\Microsoft\Installer\Products";
    private const string ProfileList = @"SOFTWARE\Microsoft\Windows NT\CurrentVersion\ProfileList";

    public IReadOnlyList<string>? Accounts(MsiInstallContext context, CancellationToken ct)
    {
        var parent = context switch
        {
            MsiInstallContext.UserManaged => InstallerKey + @"\Managed",
            MsiInstallContext.UserUnmanaged => InstallerKey + @"\UserData",
            _ => null,
        };
        if (parent is null) return null;

        // The system account is never one: its records under UserData are the per-machine
        // installations', which are walked in their own context, and Windows Installer
        // refuses a per-user walk for it with ERROR_INVALID_PARAMETER. Its hive is loaded
        // and its profile listed on every machine, so it is left out of both sources.
        var names = Read(RegistryHive.LocalMachine, parent, ct);
        if (names is null) return null;
        var accounts = new List<string>();
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { InstallerQueryService.LocalSystemAccount };
        foreach (var name in names)
            if (InstallerQueryService.IsAccount(name) && seen.Add(name))
                accounts.Add(name);

        // An unmanaged account's own list is in its hive, so every profile whose hive is
        // loaded is walked as well, records under UserData or none.
        if (context == MsiInstallContext.UserUnmanaged)
        {
            var profiles = Read(RegistryHive.LocalMachine, ProfileList, ct);
            if (profiles is null) return null;
            foreach (var sid in profiles)
            {
                if (!InstallerQueryService.IsAccount(sid) || seen.Contains(sid)) continue;
                var loaded = HiveLoaded(sid);
                if (loaded is null) return null;
                if (loaded.Value && seen.Add(sid)) accounts.Add(sid);
            }
        }

        return accounts;
    }

    public IReadOnlyList<string>? Names(MsiInstallContext context, string? sid, CancellationToken ct)
    {
        if (context == MsiInstallContext.Machine)
            return sid is null ? Read(RegistryHive.LocalMachine, MachineList, ct) : null;

        // An account is taken only as 'S-' followed by digits and hyphens, so no account
        // names a key outside its own.
        if (!InstallerQueryService.IsAccount(sid)) return null;

        if (context == MsiInstallContext.UserManaged)
            return Read(RegistryHive.LocalMachine, InstallerKey + @"\Managed\" + sid + @"\Installer\Products", ct);

        if (context != MsiInstallContext.UserUnmanaged) return null;

        var loaded = HiveLoaded(sid!);
        if (loaded is null) return null;
        return loaded.Value
            ? Read(RegistryHive.Users, sid + @"\" + UserList, ct)
            : Read(RegistryHive.LocalMachine, InstallerKey + @"\UserData\" + sid + @"\Products", ct);
    }

    // Whether the account's own hive is loaded under HKEY_USERS; null where that would not
    // read.
    private static bool? HiveLoaded(string sid)
    {
        try
        {
            using var users = RegistryKey.OpenBaseKey(RegistryHive.Users, RegistryView.Registry64);
            using var hive = users.OpenSubKeyEarned(sid);
            return hive is not null;
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            return null;
        }
    }

    private static IReadOnlyList<string>? Read(RegistryHive root, string path, CancellationToken ct)
    {
        try
        {
            using var hive = RegistryKey.OpenBaseKey(root, RegistryView.Registry64);
            using var key = hive.OpenSubKeyEarned(path);
            return key is null ? Array.Empty<string>() : RegistryNames.SubKeysOf(key, ct);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            return null;
        }
    }
}

/// <summary>
/// Lists that never read, for the constructors whose callers drive the enumeration alone:
/// a walk that stops at an entry that did not read then refuses the scan.
/// </summary>
internal sealed class NoProductLists : IProductLists
{
    internal static readonly NoProductLists Instance = new();

    public IReadOnlyList<string>? Accounts(MsiInstallContext context, CancellationToken ct) => null;

    public IReadOnlyList<string>? Names(MsiInstallContext context, string? sid, CancellationToken ct) => null;
}
