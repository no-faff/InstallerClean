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

    /// <summary>
    /// The key names in the published list of <paramref name="context"/> for
    /// <paramref name="sid"/>, the list a program's own product key and source list are in:
    /// the per-machine list for <see cref="MsiInstallContext.Machine"/> (<paramref name="sid"/>
    /// null), the account's managed list, or the account's own list in its hive. Empty where
    /// the list is not there; null where it would not read, and for an unmanaged account
    /// whose hive is not loaded, whose own list cannot be read at all.
    /// </summary>
    IReadOnlyList<string>? PublishedNames(MsiInstallContext context, string? sid, CancellationToken ct);
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

    public IReadOnlyList<string>? Accounts(MsiInstallContext context, CancellationToken ct)
    {
        var parent = context switch
        {
            MsiInstallContext.UserManaged => InstallerKey + @"\Managed",
            MsiInstallContext.UserUnmanaged => InstallerKey + @"\UserData",
            _ => null,
        };
        if (parent is null) return null;

        var names = Read(RegistryHive.LocalMachine, parent, ct);
        if (names is null) return null;
        if (context != MsiInstallContext.UserUnmanaged) return AccountsOf(names, []);

        var loaded = LoadedHives(RecordsWatchFactory.LoadedHiveNames, RegistryNames.Pause(ct));
        return loaded is null ? null : AccountsOf(names, loaded);
    }

    /// <summary>
    /// The accounts of <paramref name="listed"/>, the subkeys of the <c>Managed</c> or
    /// <c>UserData</c> key, and of <paramref name="loadedHives"/>, the subkeys of
    /// <c>HKEY_USERS</c>, each once, in that order.
    ///
    /// AN UNMANAGED ACCOUNT'S OWN LIST IS IN ITS HIVE, so every hive loaded under an
    /// account's name is walked as well, whether or not the account has records under
    /// <c>UserData</c>. A hive whose name is not an account, the default user's and each
    /// account's classes hive among them, holds no such list.
    ///
    /// THE SYSTEM ACCOUNT IS NEVER ONE: its records under <c>UserData</c> are the per-machine
    /// installations', which are walked in their own context, and Windows Installer refuses a
    /// per-user walk for it with ERROR_INVALID_PARAMETER. Its hive is loaded on every
    /// machine, so it is left out of both sources.
    /// </summary>
    internal static IReadOnlyList<string> AccountsOf(IEnumerable<string> listed, IEnumerable<string> loadedHives)
    {
        var accounts = new List<string>();
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { InstallerQueryService.LocalSystemAccount };
        foreach (var name in listed.Concat(loadedHives))
            if (InstallerQueryService.IsAccount(name) && seen.Add(name))
                accounts.Add(name);

        return accounts;
    }

    public IReadOnlyList<string>? Names(MsiInstallContext context, string? sid, CancellationToken ct)
    {
        if (context != MsiInstallContext.UserUnmanaged) return PublishedNames(context, sid, ct);

        // An account is taken only as 'S-' followed by digits and hyphens, so no account
        // names a key outside its own.
        if (!InstallerQueryService.IsAccount(sid)) return null;

        // Windows Installer walks a signed-out account's records under UserData. A loaded
        // hive's list is read through the hive's own key, so a hive that unloads after it is
        // opened reads as a list that would not read rather than as an empty one.
        try
        {
            using var users = RegistryKey.OpenBaseKey(RegistryHive.Users, RegistryView.Registry64);
            using var hive = users.OpenSubKeyEarned(sid!);
            if (hive is null)
                return Read(RegistryHive.LocalMachine, InstallerKey + @"\UserData\" + sid + @"\Products", ct);
            using var key = hive.OpenSubKeyEarned(UserList);
            return key is null ? Array.Empty<string>() : RegistryNames.SubKeysOf(key, ct);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            return null;
        }
    }

    public IReadOnlyList<string>? PublishedNames(MsiInstallContext context, string? sid, CancellationToken ct)
    {
        if (context == MsiInstallContext.Machine)
            return sid is null ? Read(RegistryHive.LocalMachine, MachineList, ct) : null;

        if (!InstallerQueryService.IsAccount(sid)) return null;

        if (context == MsiInstallContext.UserManaged)
            return Read(RegistryHive.LocalMachine, InstallerKey + @"\Managed\" + sid + @"\Installer\Products", ct);

        if (context != MsiInstallContext.UserUnmanaged) return null;

        // Read through the hive's own key, so a hive that unloads after the check reads as
        // a list that would not read rather than as an empty one.
        try
        {
            using var users = RegistryKey.OpenBaseKey(RegistryHive.Users, RegistryView.Registry64);
            using var hive = users.OpenSubKeyEarned(sid!);
            if (hive is null) return null;
            using var key = hive.OpenSubKeyEarned(UserList);
            return key is null ? Array.Empty<string>() : RegistryNames.SubKeysOf(key, ct);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            return null;
        }
    }

    // How many listings of HKEY_USERS are made before one whose readings keep disagreeing
    // is taken as a list that would not read, and the pause before each after the second.
    internal const int HiveReadings = 4;
    internal static readonly TimeSpan[] HivePauseBefore = [Durations.FromMilliseconds(50), Durations.FromMilliseconds(250)];

    /// <summary>
    /// The names of the hives loaded under <c>HKEY_USERS</c>, as <paramref name="list"/>
    /// gives them, taken where two listings in a row agree, as
    /// <see cref="RegistryNames.SubKeysOf"/> takes a key's names; null where no two of
    /// <see cref="HiveReadings"/> did, or a listing would not read. A hive loads and unloads
    /// as an account signs in and out, and a listing made across that can leave one out.
    /// <paramref name="pause"/> waits before each listing after the second.
    /// </summary>
    internal static string[]? LoadedHives(Func<string[]?> list, Action<TimeSpan> pause)
    {
        var last = list();
        if (last is null) return null;
        for (var reading = 1; reading < HiveReadings; reading++)
        {
            if (reading > 1) pause(HivePauseBefore[Math.Min(reading - 2, HivePauseBefore.Length - 1)]);
            var next = list();
            if (next is null) return null;
            if (next.SequenceEqual(last, StringComparer.Ordinal)) return next;
            last = next;
        }

        return null;
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
