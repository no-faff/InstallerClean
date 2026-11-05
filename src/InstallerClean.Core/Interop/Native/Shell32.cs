using System.Runtime.InteropServices;

namespace InstallerClean.Interop.Native;

/// <summary>
/// P/Invoke surface for shell32.dll, declared as <see cref="Kernel32"/>'s are.
/// Consumed by <see cref="Helpers.KnownFolders"/>.
/// </summary>
internal static class Shell32
{
    private const string Library = "shell32.dll";

    /// <summary>
    /// The path of the known folder <paramref name="rfid"/> names. Returns S_OK
    /// or an error HRESULT. <paramref name="ppszPath"/> is allocated with
    /// CoTaskMemAlloc and the caller frees it with <see cref="Ole32.CoTaskMemFree"/>
    /// whether the call succeeds or not.
    /// </summary>
    [DllImport(Library, EntryPoint = "SHGetKnownFolderPath", ExactSpelling = true)]
    public static extern int SHGetKnownFolderPath(in Guid rfid, uint dwFlags, IntPtr hToken, out IntPtr ppszPath);
}
