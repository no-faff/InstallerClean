using System.Windows.Automation.Peers;
using System.Windows.Controls;

namespace InstallerClean.Controls;

/// <summary>
/// AutomationPeer that reports false for both IsControlElement and
/// IsContentElement so the element is invisible in Narrator's
/// content view AND in Inspect.exe / advanced screen-reader
/// navigation. The default FrameworkElementAutomationPeer reports
/// true for IsControlElement on every visible FrameworkElement, so
/// a 1px separator or a dim-overlay panel announces as a "pane"
/// during heading navigation; this peer suppresses that.
///
/// WPF does not expose AutomationProperties.AccessibilityView
/// declaratively (that's a UWP/WinUI property), so the only way to
/// hide a decorative element from the UIA tree is to override
/// OnCreateAutomationPeer on the element and return a peer that
/// reports false for both Core flags.
/// </summary>
internal sealed class RawAutomationPeer : FrameworkElementAutomationPeer
{
    public RawAutomationPeer(System.Windows.FrameworkElement owner) : base(owner) { }
    protected override bool IsControlElementCore() => false;
    protected override bool IsContentElementCore() => false;
}

/// <summary>
/// Border that hides itself from the UIA tree. Used for decorative
/// chrome: 1px separators, dim overlays in front of disabled
/// surfaces. Replaces both <c>Border</c> and the (sealed)
/// <c>Rectangle</c> for those sites; the layout differences between
/// Border + Background and Rectangle + Fill are imperceptible at
/// the sizes used here.
/// </summary>
internal sealed class DecorativeBorder : Border
{
    protected override AutomationPeer OnCreateAutomationPeer() => new RawAutomationPeer(this);
}

/// <summary>
/// Image that hides itself from the UIA tree. Used for decorative
/// artwork whose meaning is carried by neighbouring text (the icon
/// beside a window title, the splash hero, the completion overlay's
/// icon). <c>AutomationProperties.Name=""</c> only empties an image's
/// name and leaves an unnamed image element for scan-mode navigation
/// to stop on; the peer override is what removes it.
/// </summary>
internal sealed class DecorativeImage : Image
{
    protected override AutomationPeer OnCreateAutomationPeer() => new RawAutomationPeer(this);
}
