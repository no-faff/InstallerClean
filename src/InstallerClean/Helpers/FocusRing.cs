using System.Windows;
using System.Windows.Input;

namespace InstallerClean.Helpers;

/// <summary>
/// Reports whether the focus ring is actually on an element, so a style can
/// stand a second cue down while the ring is drawn and keep it while the ring
/// is not.
///
/// <para>
/// <c>IsKeyboardFocused</c> is the wrong question and the difference is not
/// academic. WPF draws the FocusVisualStyle adorner from
/// <c>FrameworkElement.OnGotKeyboardFocus</c>, which calls
/// <c>KeyboardNavigation.ShowFocusVisual()</c>, and that gates on
/// <c>AlwaysShowFocusVisual || IsKeyboardMostRecentInputDevice()</c>, where
/// <c>AlwaysShowFocusVisual</c> is seeded from
/// <see cref="SystemParameters.KeyboardCues"/> (off unless the user has asked
/// Windows to underline access keys always) and
/// <c>IsKeyboardMostRecentInputDevice</c> asks the input manager which device
/// was used last (dotnet/wpf, PresentationFramework,
/// <c>System/Windows/Input/KeyboardNavigation.cs</c>). A mouse click takes
/// keyboard focus and draws no ring. A control that dropped its own edge on
/// <c>IsKeyboardFocused</c> would therefore show neither ring nor edge in the
/// commonest case there is, which is somebody clicking the thing.
/// </para>
///
/// <para>
/// The same expression is evaluated here, on the same element, at the same
/// moment: focus arrival. The third term is the element's own
/// <see cref="FrameworkElement.FocusVisualStyle"/>, because a null one adorns
/// nothing however the focus arrived, which is the state
/// <c>SuppressFocusVisualOnDeactivation</c> leaves behind while another
/// application is in front.
/// </para>
/// </summary>
public static class FocusRing
{
    private static readonly DependencyPropertyKey IsShowingKey =
        DependencyProperty.RegisterAttachedReadOnly(
            "IsShowing", typeof(bool), typeof(FocusRing),
            new PropertyMetadata(false));

    public static readonly DependencyProperty IsShowingProperty = IsShowingKey.DependencyProperty;

    public static bool GetIsShowing(DependencyObject element) =>
        (bool)element.GetValue(IsShowingProperty);

    /// <summary>
    /// Starts tracking, once, before any window is shown. Class handlers rather
    /// than per-control subscriptions: the property has to be truthful on every
    /// focusable element, and a style cannot attach a handler to the control it
    /// is styling.
    /// </summary>
    internal static void Track()
    {
        EventManager.RegisterClassHandler(typeof(FrameworkElement), Keyboard.GotKeyboardFocusEvent,
            new KeyboardFocusChangedEventHandler(OnGotKeyboardFocus));
        EventManager.RegisterClassHandler(typeof(FrameworkElement), Keyboard.LostKeyboardFocusEvent,
            new KeyboardFocusChangedEventHandler(OnLostKeyboardFocus));
    }

    // Both events bubble, so every ancestor of the focused element sees them.
    // The OriginalSource guard is the one WPF's own handlers use to pick out
    // the element the focus actually landed on.
    private static void OnGotKeyboardFocus(object sender, KeyboardFocusChangedEventArgs e)
    {
        if (!ReferenceEquals(sender, e.OriginalSource) || sender is not FrameworkElement element) return;

        element.SetValue(IsShowingKey,
            element.FocusVisualStyle is not null &&
            (SystemParameters.KeyboardCues || InputManager.Current.MostRecentInputDevice is KeyboardDevice));
    }

    private static void OnLostKeyboardFocus(object sender, KeyboardFocusChangedEventArgs e)
    {
        if (!ReferenceEquals(sender, e.OriginalSource) || sender is not FrameworkElement element) return;

        // Includes losing focus to nothing, which is what a window deactivating
        // does, and which is also when WPF hides the ring.
        element.SetValue(IsShowingKey, false);
    }
}
