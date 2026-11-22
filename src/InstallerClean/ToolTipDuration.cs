using System.Windows;
using System.Windows.Controls;

namespace InstallerClean;

/// <summary>
/// How long a tooltip stays open: for as long as the pointer rests on its control.
/// </summary>
/// <remarks>
/// WPF on .NET Framework 4.8 takes its own default from a compatibility switch,
/// <c>Switch.System.Windows.Controls.ToolTip.OptOutOfWCAG21ToolTipBehavior</c>: five seconds
/// where the switch is on, as long as the pointer rests where it is off (PresentationFramework,
/// <c>ToolTipService</c>'s type initializer). The app sets the duration itself so every PC
/// behaves the same whatever the switch says. Five seconds is too short for the app's longest
/// tooltip, the sentence explaining why Cancel has gone dead mid-operation. Lasting as long as
/// the pointer rests is one of WCAG 1.4.13's requirements of hover content.
///
/// A default rather than a Setter on the two ToolTip styles: this is an attached property,
/// read from the element the tooltip hangs off, and those are Buttons, TextBlocks and Borders
/// in every window of the app. Changing the default on FrameworkElement covers each of them
/// without an opinion about which end the framework reads, and changing it on
/// FrameworkContentElement covers a tooltip on a Hyperlink, a Run or a Span, which are not
/// FrameworkElements. Called before any window exists, because OverrideMetadata is refused
/// once the property has been used on the type.
/// </remarks>
internal static class ToolTipDuration
{
    internal static void KeepOpenWhileThePointerRests()
    {
        ToolTipService.ShowDurationProperty.OverrideMetadata(
            typeof(FrameworkElement), new FrameworkPropertyMetadata(int.MaxValue));
        ToolTipService.ShowDurationProperty.OverrideMetadata(
            typeof(FrameworkContentElement), new FrameworkPropertyMetadata(int.MaxValue));
    }
}
