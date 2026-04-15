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

    // Windows Update reboot required
    private static bool HasWindowsUpdateReboot() => TryKeyExists(
        @"SOFTWARE\Microsoft\Windows\CurrentVersion\WindowsUpdate\Auto Update\RebootRequired");

    // Component Based Servicing reboot pending
    private static bool HasComponentBasedServicingReboot() => TryKeyExists(
        @"SOFTWARE\Microsoft\Windows\CurrentVersion\Component Based Servicing\RebootPending");

    // File rename/delete scheduled for next boot (commonly set by Windows
    // Update and installer rollbacks).
    private static bool HasPendingFileRenames()
    {
        try
        {
            using var key = Registry.LocalMachine.OpenSubKey(
                @"SYSTEM\CurrentControlSet\Control\Session Manager");
            if (key is null) return false;

            // The value is a REG_MULTI_SZ. If present and non-empty, a rename
            // is pending. Some writers leave an empty array after clearing;
            // treat empty as "no pending".
            var raw = key.GetValue("PendingFileRenameOperations");
            if (raw is string[] arr && arr.Any(s => !string.IsNullOrEmpty(s)))
                return true;
        }
        catch (Exception)
        {
            // fail open, don't block the user
        }
        return false;
    }

    // Windows Update has reported a post-reboot action it still needs to run.
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
