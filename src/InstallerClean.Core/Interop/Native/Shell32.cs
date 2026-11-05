using System.Runtime.InteropServices;

namespace InstallerClean.Interop.Native;

/// <summary>
/// P/Invoke surface for shell32.dll, declared as <see cref="Kernel32"/>'s are.
/// Consumed by <see cref="Helpers.KnownFolders"/> and by the window app's folder picker.
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

    /// <summary>
    /// The shell item for the folder or file <paramref name="pszPath"/> names, as the
    /// interface <paramref name="riid"/> asks for. Returns S_OK or an error HRESULT.
    /// <paramref name="ppv"/> comes back with a reference the caller owns and releases.
    /// <paramref name="pbc"/> is a bind context, null for none.
    /// </summary>
    [DllImport(Library, EntryPoint = "SHCreateItemFromParsingName", CharSet = CharSet.Unicode, ExactSpelling = true)]
    public static extern int SHCreateItemFromParsingName(string pszPath, IntPtr pbc, in Guid riid, out IntPtr ppv);
}
