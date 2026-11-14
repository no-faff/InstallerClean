using System.Security;
using Microsoft.Win32;
using Microsoft.Win32.SafeHandles;
using InstallerClean.Interop.Native;

namespace InstallerClean.Helpers;

/// <summary>
/// Opens a registry subkey and tells a key that is not there from a key that is there and
/// would not open.
///
/// <c>RegistryKey.OpenSubKey</c> on .NET Framework makes one RegOpenKeyEx call for read
/// access in the key's view, throws a SecurityException where Windows answers error 5 or
/// 1346, and gives null for every other error, the same null it gives for a key that does
/// not exist. <see cref="OpenSubKeyEarned"/> makes that call again after a null and takes
/// the key as absent only where Windows answers ERROR_FILE_NOT_FOUND. Error 5 or 1346 is
/// thrown as the SecurityException <c>OpenSubKey</c> throws for it, and any other code as an
/// <see cref="IOException"/> carrying it, which every caller already takes as a key it
/// could not read.
/// </summary>
internal static class RegistryOpen
{
    private const int ERROR_SUCCESS = 0;
    private const int ERROR_FILE_NOT_FOUND = 2;
    private const int ERROR_ACCESS_DENIED = 5;
    private const int ERROR_BAD_IMPERSONATION_LEVEL = 1346;

    // KEY_READ, the access OpenSubKey(name) asks for.
    private const int KEY_READ = 0x20019;

    /// <summary>
    /// The subkey at <paramref name="path"/> under <paramref name="parent"/>, opened as
    /// <c>OpenSubKey</c> opens it, or null where Windows answers that it is not there.
    /// Throws where it is there and would not open.
    /// </summary>
    public static RegistryKey? OpenSubKeyEarned(this RegistryKey parent, string path)
    {
#pragma warning disable RS0030
        var key = parent.OpenSubKey(path);
        return key ?? OpenEarned(() => parent.OpenSubKey(path), () => OpenError(parent, path));
#pragma warning restore RS0030
    }

    /// <summary>
    /// What follows an open that gave null, over any key, so a test can stand in for the
    /// registry: <paramref name="openError"/> gives the code Windows answers the same open
    /// with, and <paramref name="open"/> opens the key again, null for any failure.
    ///
    /// WHERE WINDOWS OPENS THE KEY WHEN ASKED WHY, THE KEY IS TAKEN FROM ONE MORE OPEN, and
    /// that open giving null again throws.
    /// </summary>
    internal static T? OpenEarned<T>(Func<T?> open, Func<int> openError)
        where T : class
    {
        var code = openError();
        if (code == ERROR_FILE_NOT_FOUND) return null;
        if (code == ERROR_SUCCESS)
            return open() ?? throw new IOException(
                "The registry key opened when asked why it would not open, and then would not open again.");

        throw code is ERROR_ACCESS_DENIED or ERROR_BAD_IMPERSONATION_LEVEL
            ? new SecurityException(WindowsErrors.SystemMessage(code))
            : new IOException(WindowsErrors.SystemMessage(code), code);
    }

    /// <summary>
    /// The code Windows answers <c>OpenSubKey</c>'s own call with: RegOpenKeyEx for
    /// KEY_READ in the parent's view. A handle it opens is closed at once.
    /// </summary>
    private static int OpenError(RegistryKey parent, string path)
    {
        var access = KEY_READ | (int)parent.View;
        int code;
        IntPtr opened;
        if (parent.Name.IndexOf('\\') >= 0)
        {
            code = Advapi32.RegOpenKeyEx(parent.Handle, path, 0, access, out opened);
        }
        else
        {
            // A base key's Handle on .NET Framework opens the hive's root again, so the
            // hive's predefined handle is used, which nothing closes.
            using var hive = new SafeRegistryHandle(new IntPtr((int)HiveOf(parent)), ownsHandle: false);
            code = Advapi32.RegOpenKeyEx(hive, path, 0, access, out opened);
        }

        if (code == ERROR_SUCCESS) new SafeRegistryHandle(opened, ownsHandle: true).Dispose();
        return code;
    }

    /// <summary>
    /// The hive whose base key <paramref name="key"/> is, by the name .NET gives a base key.
    /// A <see cref="RegistryHive"/>'s value is the hive's predefined handle.
    /// </summary>
    private static RegistryHive HiveOf(RegistryKey key) => key.Name switch
    {
        "HKEY_CLASSES_ROOT" => RegistryHive.ClassesRoot,
        "HKEY_CURRENT_USER" => RegistryHive.CurrentUser,
        "HKEY_LOCAL_MACHINE" => RegistryHive.LocalMachine,
        "HKEY_USERS" => RegistryHive.Users,
        "HKEY_PERFORMANCE_DATA" => RegistryHive.PerformanceData,
        "HKEY_CURRENT_CONFIG" => RegistryHive.CurrentConfig,
        _ => throw new IOException($"The registry key '{key.Name}' is not a hive's base key."),
    };
}
