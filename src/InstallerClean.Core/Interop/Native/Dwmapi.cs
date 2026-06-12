using System.Runtime.InteropServices;

namespace InstallerClean.Interop.Native;

/// <summary>
/// P/Invoke surface for dwmapi.dll. Used only to set the dark title-bar
/// attribute on every window the app creates.
/// </summary>
internal static partial class Dwmapi
{
    private const string Library = "dwmapi.dll";

    /// <summary>
    /// Sets a Desktop Window Manager attribute. Called with
    /// <see cref="DWMWA_USE_IMMERSIVE_DARK_MODE"/> to opt into dark
    /// chrome. Attribute 20 exists from Windows 10 2004 (20H1); on the
    /// older Windows 10 builds the app's 1607 floor still admits, the
    /// call fails with E_INVALIDARG, which is swallowed, and the title
    /// bar stays light. Cosmetic only; everything else works there.
    /// </summary>
    [LibraryImport(Library)]
    public static partial int DwmSetWindowAttribute(
        IntPtr hwnd,
        int attr,
        ref int value,
        int size);

    public const int DWMWA_USE_IMMERSIVE_DARK_MODE = 20;
}
