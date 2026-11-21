using System.Runtime.InteropServices;
using Microsoft.Win32;
using Microsoft.Win32.SafeHandles;

namespace InstallerClean.Tests.Helpers;

/// <summary>
/// Writes a registry value of any type with any bytes, which RegistryKey.SetValue cannot:
/// it writes a REG_DWORD as exactly four bytes and a REG_QWORD as exactly eight.
/// </summary>
internal static class RawRegistryValue
{
    public const int REG_SZ = 1;
    public const int REG_DWORD = 4;
    public const int REG_MULTI_SZ = 7;
    public const int REG_QWORD = 11;

    public static void Write(RegistryKey key, string name, int type, byte[] data)
    {
        var result = RegSetValueEx(key.Handle, name, 0, type, data, data.Length);
        if (result != 0)
            throw new InvalidOperationException($"RegSetValueEx answered {result} for {name}.");
    }

    [DllImport("advapi32.dll", EntryPoint = "RegSetValueExW", CharSet = CharSet.Unicode, ExactSpelling = true)]
    private static extern int RegSetValueEx(SafeRegistryHandle hKey, string lpValueName, int reserved, int dwType, byte[] lpData, int cbData);
}
