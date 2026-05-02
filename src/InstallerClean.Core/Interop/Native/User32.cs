using System.Runtime.InteropServices;

namespace InstallerClean.Interop.Native;

/// <summary>
/// P/Invoke surface for user32.dll. Used only by
/// <see cref="Helpers.UnelevatedLauncher"/>.
/// </summary>
internal static partial class User32
{
    private const string Library = "user32.dll";

    /// <summary>
    /// HWND of the desktop shell (explorer.exe). Zero if no shell is
    /// registered (session 0, scheduled task, no desktop).
    /// </summary>
    [LibraryImport(Library, EntryPoint = "GetShellWindow")]
    public static partial IntPtr GetShellWindow();

    [LibraryImport(Library, EntryPoint = "GetWindowThreadProcessId")]
    public static partial uint GetWindowThreadProcessId(IntPtr hWnd, out uint lpdwProcessId);
}
