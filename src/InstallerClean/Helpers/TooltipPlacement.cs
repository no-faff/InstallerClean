using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;

namespace InstallerClean.Helpers;

/// <summary>Which edge of its control a tooltip would rather line up with.</summary>
internal enum ToolTipAnchor
{
    /// <summary>
    /// Its left edge on the control's, plus whatever horizontal offset the
    /// tooltip itself asks for. What PlacementMode.Top would do.
    /// </summary>
    Left,

    /// <summary>
    /// Its right edge on the control's. No mode in PlacementMode does this,
    /// and a control at the right of a window wants it.
    /// </summary>
    Right,
}

/// <summary>
/// Where a tooltip's box sits along the window's width.
///
/// Popups are positioned against the screen rather than against the window
/// that owns them, so a tooltip on a control near either edge is free to hang
/// off its own window. Every placement rule that aligns the box to the control
/// runs into that from one side or the other: aligning left edges grows the box
/// rightward off a control near the right, aligning right edges grows it
/// leftward off a control near the left, and a fixed nudge holds only for the
/// string lengths it was set against.
///
/// This states the window instead of the control, so it holds whichever edge
/// the control is near, in any language, and whether or not the row it sits on
/// has wrapped.
///
/// Do not replace it with an alignment rule. Each of them is right for a
/// control at one edge and wrong for the same control at the other, and which
/// edge a control is near is decided by translated strings rather than by the
/// markup: a row that fits on one line in most languages wraps in the longest,
/// and the control lands somewhere else entirely when it does.
/// </summary>
internal static class TooltipPlacement
{
    /// <summary>
    /// The box's left edge, in the window's own coordinates, given where it
    /// would rather sit. It keeps <paramref name="margin"/> clear of both edges
    /// of a window <paramref name="windowWidth"/> wide, moving the box only as
    /// far as it has to and leaving <paramref name="preferredLeft"/> alone
    /// where there is room for it.
    ///
    /// A box wider than the window can keep the margin on one side only, and
    /// this keeps the left one: that is where a left-to-right reader starts,
    /// and the alternative loses the first word rather than the last.
    /// </summary>
    public static double LeftInsideWindow(
        double preferredLeft, double popupWidth, double windowWidth, double margin)
    {
        var rightmost = windowWidth - margin - popupWidth;
        return Math.Max(margin, Math.Min(preferredLeft, rightmost));
    }

    /// <summary>
    /// A placement callback that opens <paramref name="toolTip"/> flush above
    /// its control, lined up with <paramref name="anchor"/> where there is room
    /// and pulled inside <paramref name="window"/> where there is not. The
    /// second candidate is the same position flush below, which WPF takes only
    /// when there is no room above, as when the window is against the top of
    /// the screen.
    ///
    /// The point a callback returns is relative to the control, so the
    /// control's own position is added on the way into
    /// <see cref="LeftInsideWindow"/> and taken off again on the way out.
    /// </summary>
    public static CustomPopupPlacementCallback KeptInsideWindow(
        ToolTip toolTip, Window window, ToolTipAnchor anchor, double margin) =>
        (popupSize, targetSize, offset) =>
        {
            var preferred = anchor == ToolTipAnchor.Right
                ? targetSize.Width - popupSize.Width
                : offset.X;

            // A control outside the window's tree cannot be transformed into
            // it. It never is while the window is up, and the anchor alone
            // still places a tooltip, so this asks rather than assuming: a
            // placement callback that throws takes the tooltip with it.
            var x = preferred;
            if (toolTip.PlacementTarget is UIElement control && control.IsDescendantOf(window))
            {
                var controlLeft = control.TransformToAncestor(window).Transform(new Point(0, 0)).X;
                x = LeftInsideWindow(
                    controlLeft + preferred, popupSize.Width, window.ActualWidth, margin) - controlLeft;
            }

            return
            [
                new CustomPopupPlacement(new Point(x, -popupSize.Height), PopupPrimaryAxis.Horizontal),
                new CustomPopupPlacement(new Point(x, targetSize.Height), PopupPrimaryAxis.Horizontal),
            ];
        };
}
