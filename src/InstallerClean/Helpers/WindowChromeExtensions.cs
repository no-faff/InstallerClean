using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Threading;
using InstallerClean.Interop.Native;

namespace InstallerClean.Helpers;

internal static class WindowChromeExtensions
{
    /// <summary>
    /// Restores Alt+Space system menu behaviour on a Window that uses a
    /// custom <see cref="System.Windows.Shell.WindowChrome"/>. WPF's chrome
    /// swallows the shortcut by default, which removes a standard
    /// accessibility affordance (keyboard move / resize / close).
    ///
    /// A right click on the title bar opens the same menu, through
    /// <see cref="ShowSystemMenu"/> as well. The chrome opens it there itself, from its
    /// own WM_NCRBUTTONUP handler, with .NET Framework's flags, and leaves the message
    /// unhandled, so this hook must run ahead of the chrome's or the menu opens twice.
    /// An HwndSource runs the hook added last first. The chrome adds its hook as the
    /// window gets its handle, or at once where the window has one, so this hook is added
    /// on a later dispatcher pass than that, whether this is called before or after
    /// InitializeComponent. A window that replaces its WindowChrome after it has its
    /// handle has the chrome's hook added again, ahead of this one; none does.
    /// </summary>
    public static void EnableAltSpaceSystemMenu(this Window window)
    {
        KeyEventHandler handler = (s, e) =>
        {
            if (e.Key == Key.System && e.SystemKey == Key.Space)
            {
                ShowSystemMenu(window, new Point(window.Left, window.Top));
                e.Handled = true;
            }
        };
        window.PreviewKeyDown += handler;
        window.Closed += (_, _) => window.PreviewKeyDown -= handler;

        HwndSourceHook titleBarRightClick = (IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled) =>
            OnTitleBarRightClick(window, hwnd, msg, wParam, lParam, ref handled);
        void AddAfterTheChrome() => window.Dispatcher.BeginInvoke(DispatcherPriority.Send, new Action(() =>
            (PresentationSource.FromVisual(window) as HwndSource)?.AddHook(titleBarRightClick)));
        if (PresentationSource.FromVisual(window) is not null)
            AddAfterTheChrome();
        else
            window.SourceInitialized += (_, _) => AddAfterTheChrome();
    }

    /// <summary>
    /// A right click released on the title bar of a window with custom chrome: the system
    /// menu at the pointer, as the chrome shows it on .NET 10 (dotnet/wpf v10.0.0,
    /// <c>WindowChromeWorker.cs</c>, <c>_HandleNCRButtonUp</c>). The message then goes
    /// to DefWindowProc, which on .NET 10 it reaches after the chrome's handler, and is
    /// marked handled so the chrome does not open the menu a second time.
    /// </summary>
    private static IntPtr OnTitleBarRightClick(Window window, IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
    {
        if (msg != User32.WM_NCRBUTTONUP || wParam.ToInt64() != User32.HTCAPTION
            || System.Windows.Shell.WindowChrome.GetWindowChrome(window) is null)
            return IntPtr.Zero;

        var packed = lParam.ToInt64();
        ShowSystemMenuAt(window, new Point((short)(packed & 0xFFFF), (short)((packed >> 16) & 0xFFFF)));
        handled = true;
        return User32.DefWindowProc(hwnd, (uint)msg, wParam, lParam);
    }

    /// <summary>
    /// What the system menu is shown with: the chosen command returned, and either mouse
    /// button choosing an item.
    /// </summary>
    internal const uint SystemMenuFlags = User32.TPM_LEFTBUTTON | User32.TPM_RIGHTBUTTON | User32.TPM_RETURNCMD;

    /// <summary>
    /// Shows the window's system menu with its top left at <paramref name="screenLocation"/>,
    /// in device-independent pixels, and sends the window the command chosen, as .NET 10's
    /// SystemCommands.ShowSystemMenu does (dotnet/wpf v10.0.0, <c>SystemCommands.cs</c>).
    /// SystemCommands.ShowSystemMenu on .NET Framework passes TPM_RETURNCMD alone, with which
    /// only the left mouse button chooses an item, and is banned in the shipped projects.
    /// </summary>
    internal static void ShowSystemMenu(Window window, Point screenLocation)
    {
        var dpi = VisualTreeHelper.GetDpi(window);
        ShowSystemMenuAt(window, new Point(screenLocation.X * dpi.DpiScaleX, screenLocation.Y * dpi.DpiScaleY));
    }

    /// <summary>
    /// The system menu at <paramref name="physicalScreenLocation"/>, in physical pixels,
    /// from .NET 10's SystemCommands.ShowSystemMenuPhysicalCoordinates.
    /// </summary>
    private static void ShowSystemMenuAt(Window window, Point physicalScreenLocation)
    {
        var hwnd = new WindowInteropHelper(window).Handle;
        if (hwnd == IntPtr.Zero || !User32.IsWindow(hwnd))
            return;

        var menu = User32.GetSystemMenu(hwnd, false);
        var command = User32.TrackPopupMenuEx(menu, SystemMenuFlags,
            (int)physicalScreenLocation.X, (int)physicalScreenLocation.Y, hwnd, IntPtr.Zero);
        if (command != 0)
            User32.PostMessage(hwnd, User32.WM_SYSCOMMAND, new IntPtr(command), IntPtr.Zero);
    }

    /// <summary>
    /// Hides the focus ring on Window.Deactivated when activation
    /// moves to a window in another process, without touching focus
    /// state.
    ///
    /// WPF preserves keyboard focus across Alt+Tab and repaints the
    /// focus visual on reactivation, leaving a ring on the
    /// previously-focused control across a cross-process activation
    /// round-trip. Swapping the focused element's FocusVisualStyle
    /// to null on Deactivated suppresses the ring without clearing
    /// focus: a TextBox keeps its caret position (so Ctrl+V after a
    /// path copy still pastes), a Button stays bound to Enter / Space.
    /// The original style is restored on the next PreviewKeyDown, so
    /// explicit keyboard navigation after return brings the ring back
    /// the normal way.
    ///
    /// In-process deactivations (a modal opened via ShowDialog or a
    /// non-modal Show()) are skipped, leaving the parent window's
    /// focus visual untouched.
    /// </summary>
    public static void SuppressFocusVisualOnDeactivation(this Window window)
    {
        FrameworkElement? suppressed = null;
        Style? originalStyle = null;

        // The Win32 pair reads the foreground window's owning PID
        // only: never the window text, never a hook handle, never a
        // keystroke buffer. Fires at most once per loss of activation;
        // Window.Deactivated is a low-frequency event.
        //
        // The lambda subscriptions are window-local: the handlers and
        // their captured `suppressed` / `originalStyle` locals are
        // collected when the window is, so no explicit Closed unhook
        // is needed. ViewModels pin PropertyChanged handlers to fields
        // and unhook them because their subscriber lifetime crosses
        // DI scope; a window-local event subscription does not.
        window.Deactivated += (_, _) =>
        {
            IntPtr fg = User32.GetForegroundWindow();
            if (fg == IntPtr.Zero) return;

            User32.GetWindowThreadProcessId(fg, out uint fgPid);
            if (fgPid == (uint)Environment.ProcessId) return;

            if (suppressed is not null) return;

            if (FocusManager.GetFocusedElement(window) is FrameworkElement fe)
            {
                suppressed = fe;
                originalStyle = fe.FocusVisualStyle;
                fe.FocusVisualStyle = null;
            }
        };

        window.PreviewKeyDown += (_, _) =>
        {
            if (suppressed is null) return;
            suppressed.FocusVisualStyle = originalStyle;
            suppressed = null;
            originalStyle = null;
        };
    }
}
