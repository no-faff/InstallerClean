namespace InstallerClean;

/// <summary>
/// The AppContext switches whose .NET Framework 4.8 default, for an exe targeting 4.8, is not
/// what .NET 10 does, each set to .NET 10's behaviour before the window's first code runs.
/// </summary>
/// <remarks>
/// WPF reads these once, when they are first asked for, so they are set from the module
/// initializer, ahead of App and the first Dispatcher. .NET Framework checks the accessibility
/// switches when the first Dispatcher is created: once one of
/// <c>Switch.UseLegacyAccessibilityFeatures</c> and its .2 to .5 is true every later one must be,
/// and OptOutOfGridColumnResizeUsingKeyboard may be false only where .4 is false, or the window
/// gets a NotSupportedException. So the column resize is set false only with .4; setting it
/// alone, or .5 alone, breaks the window at start.
/// </remarks>
internal static class FrameworkSwitches
{
    internal const string LegacyAccessibility4 = "Switch.UseLegacyAccessibilityFeatures.4";
    internal const string LegacyAccessibility5 = "Switch.UseLegacyAccessibilityFeatures.5";
    internal const string OptOutOfGridColumnResizeUsingKeyboard =
        "Switch.System.Windows.Controls.OptOutOfGridColumnResizeUsingKeyboard";
    internal const string SelectorUpdatesSelectionPropertiesWhenDisconnected =
        "Switch.System.Windows.Controls.SelectorUpdatesSelectionPropertiesWhenDisconnected";

    internal static void Pin()
    {
        // In WPF .4 and .5 are read only by the accessibility check. .4 is false so the column
        // resize below passes it; .5 is false as well, .NET 10 having neither.
        AppContext.SetSwitch(LegacyAccessibility4, false);
        AppContext.SetSwitch(LegacyAccessibility5, false);
        // Alt+Left and Alt+Right resize a GridView column from its header.
        AppContext.SetSwitch(OptOutOfGridColumnResizeUsingKeyboard, false);
        // A ListBox, ComboBox or TabControl whose DataContext is WPF's disconnected item leaves
        // its selection properties as they are when its items change.
        AppContext.SetSwitch(SelectorUpdatesSelectionPropertiesWhenDisconnected, false);
    }
}
