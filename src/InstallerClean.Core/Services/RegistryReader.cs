using System.Security;
using Microsoft.Win32;

namespace InstallerClean.Services;

/// <summary>Production IRegistryReader: opens HKLM Registry64 and folds the
/// documented failure modes (SecurityException, IOException,
/// UnauthorizedAccessException, ObjectDisposedException) into null/false.
/// OutOfMemoryException and StackOverflowException propagate so a real
/// memory-pressure failure isn't silently downgraded to "no signal" by
/// PendingRebootService.Check.</summary>
internal sealed class RegistryReader : IRegistryReader
{
    public bool LocalMachineKeyExists(string relativePath)
    {
        try
        {
            using var hive = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry64);
            using var key = hive.OpenSubKey(relativePath);
            return key is not null;
        }
        catch (SecurityException) { return false; }
        catch (IOException) { return false; }
        catch (UnauthorizedAccessException) { return false; }
        catch (ObjectDisposedException) { return false; }
    }

    public string[]? LocalMachineMultiStringValue(string keyPath, string valueName)
    {
        try
        {
            using var hive = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry64);
            using var key = hive.OpenSubKey(keyPath);
            if (key is null) return null;
            return key.GetValue(valueName) as string[];
        }
        catch (SecurityException) { return null; }
        catch (IOException) { return null; }
        catch (UnauthorizedAccessException) { return null; }
        catch (ObjectDisposedException) { return null; }
    }
}
