using System.Windows;
using System.Windows.Input;

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
    /// Clears logical and keyboard focus on Window.Deactivated. WPF
    /// restores focus to the previously focused element on
    /// reactivation, painting a ring on a button the user did not
    /// navigate to. Keyboard navigation after return (Tab,
    /// accelerators) acquires focus normally, so the keyboard
    /// affordance is preserved.
    ///
    /// Window.Deactivated also fires when another top-level window
    /// in the same process activates: a modal opened via ShowDialog
    /// or a non-modal window opened via Show(). The parent loses
    /// its prior focus across that round trip too.
    /// </summary>
    public static void ClearFocusOnDeactivation(this Window window)
    {
        window.Deactivated += (_, _) =>
        {
            FocusManager.SetFocusedElement(window, null);
            Keyboard.ClearFocus();
        };
    }
}
