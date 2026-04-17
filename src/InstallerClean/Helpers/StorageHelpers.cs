using System.Runtime.InteropServices;

namespace InstallerClean.Helpers;

internal static class StorageHelpers
{
    /// <summary>
    /// Returns the number of bytes available to the current user at <paramref name="path"/>,
    /// or null if the space cannot be determined. Handles local drives, UNC shares and
    /// mapped drives uniformly via GetDiskFreeSpaceEx.
    /// </summary>
    internal static long? GetAvailableFreeSpace(string path)
    {
        if (string.IsNullOrEmpty(path)) return null;

        try
        {
            if (!GetDiskFreeSpaceEx(path, out var free, out _, out _))
                return null;
            return (long)free;
        }
        catch
        {
            return null;
        }
    }

    [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Unicode, EntryPoint = "GetDiskFreeSpaceExW")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool GetDiskFreeSpaceEx(
        string lpDirectoryName,
        out ulong lpFreeBytesAvailableToCaller,
        out ulong lpTotalNumberOfBytes,
        out ulong lpTotalNumberOfFreeBytes);
}
