using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using InstallerClean.Interop.Native;

namespace InstallerClean.Helpers;

internal static class WindowChromeExtensions
{
    /// <summary>
    /// Restores Alt+Space system menu behaviour on a Window that uses a
    /// custom <see cref="System.Windows.Shell.WindowChrome"/>. WPF's chrome
    /// swallows the shortcut by default, which removes a standard
    /// accessibility affordance (keyboard move / resize / close).
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
        var hwnd = new WindowInteropHelper(window).Handle;
        if (hwnd == IntPtr.Zero || !User32.IsWindow(hwnd))
            return;

        var menu = User32.GetSystemMenu(hwnd, false);
        var command = User32.TrackPopupMenuEx(menu, SystemMenuFlags,
            (int)(screenLocation.X * dpi.DpiScaleX), (int)(screenLocation.Y * dpi.DpiScaleY), hwnd, IntPtr.Zero);
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
