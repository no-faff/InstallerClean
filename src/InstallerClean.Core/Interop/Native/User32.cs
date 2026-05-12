using System.Runtime.InteropServices;

namespace InstallerClean.Interop.Native;

/// <summary>
/// P/Invoke surface for user32.dll. Consumed by
/// <see cref="Helpers.UnelevatedLauncher"/> (shell-token chain) and
/// the WPF host's WindowChromeExtensions (focus-clear gate).
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

    /// <summary>
    /// HWND of the foreground window across the desktop, or zero if no
    /// window has activation. Called from
    /// <see cref="Helpers.WindowChromeExtensions.ClearFocusOnDeactivation"/>
    /// to gate the focus-clear on whether activation went to a window
    /// in another process: the call reads only the foreground HWND,
    /// never window text or a keystroke buffer.
    /// </summary>
    [LibraryImport(Library, EntryPoint = "GetForegroundWindow")]
    public static partial IntPtr GetForegroundWindow();

    [LibraryImport(Library, EntryPoint = "GetWindowThreadProcessId")]
    public static partial uint GetWindowThreadProcessId(IntPtr hWnd, out uint lpdwProcessId);
}
