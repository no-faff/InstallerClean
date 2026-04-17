using Microsoft.Win32;

namespace InstallerClean.Services;

public sealed class PendingRebootService : IPendingRebootService
{
    public bool HasPendingReboot()
    {
        return HasWindowsUpdateReboot()
            || HasComponentBasedServicingReboot()
            || HasPendingFileRenames()
            || HasPostRebootReporting();
    }

    private static bool HasWindowsUpdateReboot() => TryKeyExists(
        @"SOFTWARE\Microsoft\Windows\CurrentVersion\WindowsUpdate\Auto Update\RebootRequired");

    private static bool HasComponentBasedServicingReboot() => TryKeyExists(
        @"SOFTWARE\Microsoft\Windows\CurrentVersion\Component Based Servicing\RebootPending");

    private static bool HasPendingFileRenames()
    {
        try
        {
            using var key = Registry.LocalMachine.OpenSubKey(
                @"SYSTEM\CurrentControlSet\Control\Session Manager");
            if (key is null) return false;

            // PendingFileRenameOperations is a REG_MULTI_SZ; some writers
            // leave an empty array after clearing, which we treat as "no pending".
            var raw = key.GetValue("PendingFileRenameOperations");
            if (raw is string[] arr && arr.Any(s => !string.IsNullOrEmpty(s)))
                return true;
        }
        catch (Exception)
        {
            // fail open; a failed registry read must not block the user.
        }
        return false;
    }

    private static bool HasPostRebootReporting() => TryKeyExists(
        @"SOFTWARE\Microsoft\Windows\CurrentVersion\WindowsUpdate\Auto Update\PostRebootReporting");

    private static bool TryKeyExists(string path)
    {
        try
        {
            using var key = Registry.LocalMachine.OpenSubKey(path);
            return key is not null;
        }
        catch (Exception)
        {
            return false; // fail open, don't block the user
        }
    }
}
