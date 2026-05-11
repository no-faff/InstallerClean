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
    /// Clears logical and keyboard focus when the window deactivates.
    /// WPF restores focus to the previously focused element when the
    /// window reactivates after Alt+Tab or any out-of-process focus
    /// steal (screenshot tool hotkey, browser opened from a link,
    /// UAC prompt, anything that briefly takes the foreground).
    /// The restored focus paints a focus ring on whatever button held
    /// focus before deactivation, even though the user did not
    /// initiate any keyboard interaction on return. Clearing focus
    /// here breaks the restoration chain so the next paint after
    /// reactivation has nothing to ring. Keyboard navigation
    /// initiated by the user after return (Tab, accelerators) still
    /// acquires focus normally and paints the ring on the right
    /// target, so the accessibility affordance is preserved; only
    /// the spurious "stuck selected" appearance after returning to
    /// the app is removed.
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
