using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls.Primitives;
using System.Windows.Input;

namespace InstallerClean.Helpers;

internal static partial class WindowChromeExtensions
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
                SystemCommands.ShowSystemMenu(
                    window,
                    new Point(window.Left, window.Top));
                e.Handled = true;
            }
        };
        window.PreviewKeyDown += handler;
        window.Closed += (_, _) => window.PreviewKeyDown -= handler;
    }

    /// <summary>
    /// Clears logical and keyboard focus on Window.Deactivated when
    /// activation moves to a window in another process. WPF otherwise
    /// restores focus to the previously focused element on
    /// reactivation, painting a ring on a button the user did not
    /// navigate to. Keyboard navigation after return (Tab,
    /// accelerators) acquires focus normally.
    ///
    /// In-process deactivations (a modal opened via ShowDialog or a
    /// non-modal Show()) are skipped, preserving the parent's tab
    /// position across the round trip.
    /// </summary>
    public static void ClearFocusOnDeactivation(this Window window)
    {
        // The Win32 pair reads the foreground window's owning PID
        // only: never the window text, never a hook handle, never a
        // keystroke buffer. Fires at most once per loss of activation;
        // Window.Deactivated is a low-frequency event.
        //
        // The lambda captures `window` only; the handler is collected
        // when the window is, so no explicit Closed unhook is needed.
        // ViewModels pin PropertyChanged handlers to fields and unhook
        // them because their subscriber lifetime crosses DI scope; a
        // window-local Deactivated subscription does not.
        window.Deactivated += (_, _) =>
        {
            // TextBoxBase (TextBox or RichTextBox) keeps focus across
            // Alt+Tab so a user mid-edit can copy a path from another
            // app and paste back without re-clicking the caret.
            if (FocusManager.GetFocusedElement(window) is TextBoxBase) return;

            IntPtr fg = GetForegroundWindow();
            if (fg == IntPtr.Zero) return;

            GetWindowThreadProcessId(fg, out uint fgPid);
            if (fgPid == (uint)Environment.ProcessId) return;

            FocusManager.SetFocusedElement(window, null);
            Keyboard.ClearFocus();
        };
    }

    [LibraryImport("user32.dll")]
    private static partial IntPtr GetForegroundWindow();

    [LibraryImport("user32.dll")]
    private static partial uint GetWindowThreadProcessId(IntPtr hWnd, out uint lpdwProcessId);
}
