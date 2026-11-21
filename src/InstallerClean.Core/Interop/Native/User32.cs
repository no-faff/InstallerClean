using System.Runtime.InteropServices;

namespace InstallerClean.Interop.Native;

/// <summary>
/// P/Invoke surface for user32.dll. Consumed by the WPF host's
/// WindowChromeExtensions (focus-visual suppression gate, the system menu),
/// DetailWindowSizing (work-area measurement), App's single-instance
/// hand-off (foreground rights) and the folder picker (its owner window), and
/// by <see cref="WindowStation"/> for the command line.
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

    /// <summary>
    /// HWND of the active window attached to the calling thread's message queue, or
    /// zero if none is. The window app's folder picker opens owned by it, so the
    /// picker is modal to whichever of the app's windows the user was in.
    /// </summary>
    [DllImport(Library, EntryPoint = "GetActiveWindow", ExactSpelling = true)]
    public static extern IntPtr GetActiveWindow();

    [DllImport(Library, EntryPoint = "GetWindowThreadProcessId", ExactSpelling = true)]
    public static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint lpdwProcessId);

    /// <summary>Whether <paramref name="hWnd"/> names a window that exists.</summary>
    [DllImport(Library, EntryPoint = "IsWindow", ExactSpelling = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool IsWindow(IntPtr hWnd);

    /// <summary>
    /// The window's system menu, the one Alt+Space opens. With <paramref name="bRevert"/>
    /// false the window's own copy, which is what Windows shows.
    /// </summary>
    [DllImport(Library, EntryPoint = "GetSystemMenu", ExactSpelling = true)]
    public static extern IntPtr GetSystemMenu(IntPtr hWnd, [MarshalAs(UnmanagedType.Bool)] bool bRevert);

    /// <summary>
    /// Shows <paramref name="hmenu"/> at the screen point in physical pixels and waits for
    /// the user. With TPM_RETURNCMD it returns the chosen item's command, or 0 where
    /// nothing was chosen, and sends the window nothing itself.
    /// </summary>
    [DllImport(Library, EntryPoint = "TrackPopupMenuEx", ExactSpelling = true, SetLastError = true)]
    public static extern uint TrackPopupMenuEx(IntPtr hmenu, uint uFlags, int x, int y, IntPtr hwnd, IntPtr lptpm);

    /// <summary>TPM_RETURNCMD: the chosen command comes back as the return value.</summary>
    public const uint TPM_RETURNCMD = 0x0100;

    /// <summary>TPM_LEFTBUTTON: the left mouse button chooses an item.</summary>
    public const uint TPM_LEFTBUTTON = 0x0000;

    /// <summary>TPM_RIGHTBUTTON: either mouse button chooses an item.</summary>
    public const uint TPM_RIGHTBUTTON = 0x0002;

    /// <summary>Puts a message on the window's queue and returns without waiting for it.</summary>
    [DllImport(Library, EntryPoint = "PostMessageW", CharSet = CharSet.Unicode, ExactSpelling = true, SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool PostMessage(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam);

    /// <summary>WM_SYSCOMMAND: a command from the window's system menu.</summary>
    public const uint WM_SYSCOMMAND = 0x0112;

    /// <summary>WM_NCRBUTTONUP: the right mouse button released outside the client area.</summary>
    public const int WM_NCRBUTTONUP = 0x00A5;

    /// <summary>HTCAPTION: the hit-test answer for the title bar.</summary>
    public const int HTCAPTION = 2;

    /// <summary>The default handling Windows gives a message the window has not handled.</summary>
    [DllImport(Library, EntryPoint = "DefWindowProcW", CharSet = CharSet.Unicode, ExactSpelling = true)]
    public static extern IntPtr DefWindowProc(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam);

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
