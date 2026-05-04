using Microsoft.Win32;

namespace InstallerClean.Services;

public sealed class PendingRebootService : IPendingRebootService
{
    public bool HasPendingReboot()
    {
        // Registry64 is pinned explicitly so an x86-process rebuild
        // wouldn't silently redirect to WOW6432Node and miss the CBS /
        // WindowsUpdate / Session Manager keys (all of which are
        // unwowed). Matches the same pin in InstallerQueryService.
        using var hive = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry64);

        return KeyExists(hive, @"SOFTWARE\Microsoft\Windows\CurrentVersion\WindowsUpdate\Auto Update\RebootRequired")
            || KeyExists(hive, @"SOFTWARE\Microsoft\Windows\CurrentVersion\Component Based Servicing\RebootPending")
            || HasPendingFileRenames(hive)
            || KeyExists(hive, @"SOFTWARE\Microsoft\Windows\CurrentVersion\WindowsUpdate\Auto Update\PostRebootReporting");
    }

    private static bool HasPendingFileRenames(RegistryKey hive)
    {
        try
        {
            using var key = hive.OpenSubKey(@"SYSTEM\CurrentControlSet\Control\Session Manager");
            if (key is null) return false;

            // PendingFileRenameOperations is a REG_MULTI_SZ; some writers
            // leave an empty array after clearing, which we treat as "no pending".
            var raw = key.GetValue("PendingFileRenameOperations");
            return raw is string[] arr && arr.Any(s => !string.IsNullOrEmpty(s));
        }
        catch (Exception)
        {
            // fail open; a failed registry read must not block the user.
            return false;
        }
    }

    private static bool KeyExists(RegistryKey hive, string path)
    {
        try
        {
            using var key = hive.OpenSubKey(path);
            return key is not null;
        }
        catch (Exception)
        {
            return false;
        }
    }
}
