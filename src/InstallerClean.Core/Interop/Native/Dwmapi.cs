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
    /// chrome on Windows 10 1809+ and Windows 11.
    /// </summary>
    [LibraryImport(Library)]
    public static partial int DwmSetWindowAttribute(
        IntPtr hwnd,
        int attr,
        ref int value,
        int size);

    public const int DWMWA_USE_IMMERSIVE_DARK_MODE = 20;
}
