using Microsoft.Win32;

namespace InstallerClean.Services;

public sealed class PendingRebootService : IPendingRebootService
{
    public bool HasPendingReboot()
    {
        // Pin Registry64. Today the app ships x64-only and the CBS /
        // WindowsUpdate / Session Manager keys live in the 64-bit view,
        // but a future bitness flip would silently redirect via
        // Wow6432Node and miss real entries. Matches InstallerQueryService.
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
