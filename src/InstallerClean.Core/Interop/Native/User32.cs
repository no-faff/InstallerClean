using System.Runtime.InteropServices;

namespace InstallerClean.Interop.Native;

/// <summary>
/// P/Invoke surface for user32.dll. Consumed by the WPF host's
/// WindowChromeExtensions (focus-visual suppression gate),
/// DetailWindowSizing (work-area measurement) and App's single-instance
/// hand-off (foreground rights).
/// </summary>
internal static partial class User32
{
    private const string Library = "user32.dll";

    /// <summary>
    /// Grants another process the right to take the foreground. Win32 gives that
    /// right only to the process the user last interacted with, so a running
    /// instance asked (by a second launch) to bring its window forward does not
    /// have it and its Activate() would only flash the taskbar button. The
    /// second instance, which the user has just launched, does have it, and hands
    /// it over with this before it signals and exits.
    ///
    /// ASFW_ANY rather than the first instance's PID: the second instance knows
    /// the first only through a named event, and the grant is consumed by the
    /// next foreground change either way.
    /// </summary>
    [LibraryImport(Library, EntryPoint = "AllowSetForegroundWindow", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static partial bool AllowSetForegroundWindow(uint dwProcessId);

    /// <summary>ASFW_ANY: any process may take the foreground.</summary>
    public const uint ASFW_ANY = 0xFFFFFFFF;

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
