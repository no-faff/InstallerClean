namespace InstallerClean.Helpers;

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
}
