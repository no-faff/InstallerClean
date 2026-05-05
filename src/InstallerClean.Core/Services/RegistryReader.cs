using Microsoft.Win32;

namespace InstallerClean.Services;

/// <summary>Production IRegistryReader: opens HKLM Registry64 and swallows any failure as null/false.</summary>
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
        catch
        {
            return false;
        }
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
        catch
        {
            return null;
        }
    }
}
