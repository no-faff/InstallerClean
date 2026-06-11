using System.Runtime.InteropServices;

namespace InstallerClean.Interop.Native;

/// <summary>
/// P/Invoke surface for user32.dll. Consumed by the WPF host's
/// WindowChromeExtensions (focus-visual suppression gate) and
/// DetailWindowSizing (work-area measurement).
/// </summary>
internal static partial class User32
{
    private const string Library = "user32.dll";

    /// <summary>
    /// HWND of the foreground window across the desktop, or zero if no
    /// window has activation. Called from
    /// <see cref="Helpers.WindowChromeExtensions.SuppressFocusVisualOnDeactivation"/>
    /// to gate focus-visual suppression on whether activation went to a
    /// window in another process: the call reads only the foreground
    /// HWND, never window text or a keystroke buffer.
    /// </summary>
    [LibraryImport(Library, EntryPoint = "GetForegroundWindow")]
    public static partial IntPtr GetForegroundWindow();

    [LibraryImport(Library, EntryPoint = "GetWindowThreadProcessId")]
    public static partial uint GetWindowThreadProcessId(IntPtr hWnd, out uint lpdwProcessId);

    /// <summary>
    /// Monitor with the largest intersection with the window.
    /// MONITOR_DEFAULTTONEAREST maps a window that intersects no
    /// monitor to the nearest one, so the return is never zero for a
    /// valid HWND.
    /// </summary>
    [LibraryImport(Library, EntryPoint = "MonitorFromWindow")]
    public static partial IntPtr MonitorFromWindow(IntPtr hWnd, uint dwFlags);

    public const uint MONITOR_DEFAULTTONEAREST = 2;

    /// <summary>
    /// Fills <paramref name="lpmi"/> for the monitor. <c>cbSize</c>
    /// must hold the struct size before the call, per the Win32
    /// versioned-struct contract; with it zero the call fails and
    /// reports nothing through last-error.
    /// </summary>
    [LibraryImport(Library, EntryPoint = "GetMonitorInfoW")]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static partial bool GetMonitorInfo(IntPtr hMonitor, ref MONITORINFO lpmi);

    [StructLayout(LayoutKind.Sequential)]
    public struct MONITORINFO
    {
        public int cbSize;
        public RECT rcMonitor;
        /// <summary>
        /// Work area: the monitor minus the taskbar and any app bars,
        /// in device pixels.
        /// </summary>
        public RECT rcWork;
        public uint dwFlags;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct RECT
    {
        public int Left;
        public int Top;
        public int Right;
        public int Bottom;
    }
}
