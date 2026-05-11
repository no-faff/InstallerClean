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
        // Receipts (for any AV-heuristic review of the elevated host):
        // the pair reads the foreground window's owning PID, never the
        // window text, never a hook handle, never a keystroke buffer.
        // Fires at most once per loss of activation (Window.Deactivated
        // is a low-frequency event).
        //
        // Handler lifetime: the lambda captures `window` only, so a
        // window-Closed teardown collects both. No explicit Closed
        // unhook unlike the field-pinned PropertyChanged handlers in
        // the view-models, which need precise control over subscriber
        // lifetime across DI scope.
        window.Deactivated += (_, _) =>
        {
            // Editable text input keeps focus on Alt+Tab return: the
            // caret position is preserved so a user mid-edit who tabs
            // away to copy a path can paste back without re-clicking.
            // TextBoxBase covers both TextBox and RichTextBox.
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
