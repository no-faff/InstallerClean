using System.Runtime.InteropServices;

namespace InstallerClean.Interop.Native;

/// <summary>
/// P/Invoke surface for user32.dll. Consumed by the WPF host's
/// WindowChromeExtensions (focus-visual suppression gate),
/// DetailWindowSizing (work-area measurement) and App's single-instance
/// hand-off (foreground rights), and by <see cref="WindowStation"/> for the
/// command line.
/// </summary>
internal static class User32
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
    [DllImport(Library, EntryPoint = "AllowSetForegroundWindow", ExactSpelling = true, SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool AllowSetForegroundWindow(uint dwProcessId);

    /// <summary>ASFW_ANY: any process may take the foreground.</summary>
    public const uint ASFW_ANY = 0xFFFFFFFF;

    /// <summary>
    /// HWND of the foreground window across the desktop, or zero if no
    /// window has activation. Called from the app's
    /// <c>WindowChromeExtensions.SuppressFocusVisualOnDeactivation</c>
    /// to gate focus-visual suppression on whether activation went to a
    /// window in another process: the call reads only the foreground
    /// HWND, never window text or a keystroke buffer.
    /// </summary>
    [DllImport(Library, EntryPoint = "GetForegroundWindow", ExactSpelling = true)]
    public static extern IntPtr GetForegroundWindow();

    [DllImport(Library, EntryPoint = "GetWindowThreadProcessId", ExactSpelling = true)]
    public static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint lpdwProcessId);

    /// <summary>
    /// Monitor with the largest intersection with the window.
    /// MONITOR_DEFAULTTONEAREST maps a window that intersects no
    /// monitor to the nearest one, so the return is never zero for a
    /// valid HWND.
    /// </summary>
    [DllImport(Library, EntryPoint = "MonitorFromWindow", ExactSpelling = true)]
    public static extern IntPtr MonitorFromWindow(IntPtr hWnd, uint dwFlags);

    public const uint MONITOR_DEFAULTTONEAREST = 2;

    /// <summary>
    /// Fills <paramref name="lpmi"/> for the monitor. <c>cbSize</c>
    /// must hold the struct size before the call, per the Win32
    /// versioned-struct contract; with it zero the call fails and
    /// reports nothing through last-error.
    /// </summary>
    [DllImport(Library, EntryPoint = "GetMonitorInfoW", CharSet = CharSet.Unicode, ExactSpelling = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool GetMonitorInfo(IntPtr hMonitor, ref MONITORINFO lpmi);

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

    /// <summary>The window station the process is attached to.</summary>
    [DllImport(Library, EntryPoint = "GetProcessWindowStation", ExactSpelling = true)]
    public static extern IntPtr GetProcessWindowStation();

    /// <summary>
    /// Reads one kind of information about a window station or desktop object. Called
    /// with <see cref="UOI_FLAGS"/>, <paramref name="pvInfo"/> is a
    /// <see cref="USEROBJECTFLAGS"/> and <paramref name="nLength"/> its size.
    /// </summary>
    [DllImport(Library, EntryPoint = "GetUserObjectInformationW", CharSet = CharSet.Unicode, ExactSpelling = true, SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool GetUserObjectInformation(
        IntPtr hObj, int nIndex, ref USEROBJECTFLAGS pvInfo, uint nLength, out uint lpnLengthNeeded);

    /// <summary>UOI_FLAGS: the object's <see cref="USEROBJECTFLAGS"/>.</summary>
    public const int UOI_FLAGS = 1;

    /// <summary>WSF_VISIBLE: the window station can show windows to a user.</summary>
    public const int WSF_VISIBLE = 0x0001;

    [StructLayout(LayoutKind.Sequential)]
    public struct USEROBJECTFLAGS
    {
        public int fInherit;
        public int fReserved;
        public int dwFlags;
    }
}
