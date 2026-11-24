using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;
using InstallerClean.Interop.Native;

namespace InstallerClean.Helpers;

/// <summary>
/// Window sizing against the work area (the monitor minus the taskbar)
/// of the screen the app is on: startup heights for the detail windows,
/// MaxWidth / MaxHeight limits for the SizeToContent windows, and the
/// card widths of the dialogs that size to their card, so large OS text
/// scales grow them up to the screen and no further. A
/// fixed default cannot cover the range: a 1080p laptop at 150% scale
/// has roughly 672 device-independent units of work-area height, while
/// a 100% desktop has roughly 1030.
/// </summary>
internal static class DetailWindowSizing
{
    /// <summary>
    /// Gap kept between the window and the work-area edge so a clamped
    /// window never opens flush against the taskbar.
    /// </summary>
    private const double EdgeMargin = 24;

    /// <summary>
    /// <paramref name="preferred"/>, raised to <paramref name="minimum"/>
    /// and then held to the work-area height of the monitor hosting
    /// <paramref name="reference"/> (the primary monitor when the
    /// reference is null or not yet shown). See <see cref="Within"/>.
    /// All values in device-independent units.
    /// </summary>
    public static double ClampHeightToWorkArea(Window? reference, double preferred, double minimum)
        => Within(preferred, minimum, WorkAreaHeightLimit(reference));

    /// <summary>
    /// Horizontal counterpart of
    /// <see cref="ClampHeightToWorkArea(Window?, double, double)"/>.
    /// </summary>
    public static double ClampWidthToWorkArea(Window? reference, double preferred, double minimum)
        => Within(preferred, minimum, WorkAreaWidthLimit(reference));

    /// <summary>
    /// <paramref name="preferred"/>, never below <paramref name="minimum"/>
    /// and never above <paramref name="limit"/>. Where the limit is under the
    /// minimum the limit wins, so a window sized through this is never larger
    /// than its work area, whatever minimum it asks for.
    /// </summary>
    public static double Within(double preferred, double minimum, double limit)
        => Math.Min(Math.Max(minimum, preferred), limit);

    /// <summary>
    /// Sizes a resizable window, before it is shown, to
    /// <paramref name="preferredWidth"/> by <paramref name="preferredHeight"/>
    /// held inside the work area of the monitor the main window is on. The window's MinWidth and MinHeight are
    /// lowered to that work area first where they exceed it, because WPF holds
    /// a window's Width and Height at its MinWidth and MinHeight; on a larger
    /// work area they keep the values the XAML gives them.
    /// </summary>
    public static void SizeWithinWorkArea(this Window window, double preferredWidth, double preferredHeight)
    {
        var reference = Application.Current?.MainWindow;
        window.MinWidth = Math.Min(window.MinWidth, WorkAreaWidthLimit(reference));
        window.MinHeight = Math.Min(window.MinHeight, WorkAreaHeightLimit(reference));
        window.Width = ClampWidthToWorkArea(reference, preferredWidth, window.MinWidth);
        window.Height = ClampHeightToWorkArea(reference, preferredHeight, window.MinHeight);
    }

    /// <summary>
    /// The tallest a window may sensibly open on the monitor hosting
    /// <paramref name="reference"/>: the work-area height less the edge
    /// margin. Suited to a MaxHeight on a SizeToContent window.
    /// </summary>
    public static double WorkAreaHeightLimit(Window? reference)
        => WorkArea(reference).Height - EdgeMargin;

    /// <summary>
    /// The widest a window may sensibly open on the monitor hosting
    /// <paramref name="reference"/>: the work-area width less the edge
    /// margin. Suited to a MaxWidth on a SizeToContent window.
    /// </summary>
    public static double WorkAreaWidthLimit(Window? reference)
        => WorkArea(reference).Width - EdgeMargin;

    /// <summary>
    /// The least and greatest width of a dialog's card: <paramref name="minimum"/>
    /// and <paramref name="maximum"/>, its widths at 100% text scale, multiplied by
    /// <paramref name="factor"/>, and neither wider than <paramref name="limit"/>.
    /// The minimum is held to the limit as well as the maximum, because WPF lets
    /// MinWidth win over both MaxWidth and the room an element is given.
    /// </summary>
    public static (double Minimum, double Maximum) CardWidths(
        double minimum, double maximum, double factor, double limit)
        => (Math.Min(minimum * factor, limit), Math.Min(maximum * factor, limit));

    /// <summary>
    /// Holds a dialog that sizes to its card inside the work area of the monitor
    /// the main window is on: the card's MinWidth and MaxWidth come from
    /// <see cref="CardWidths"/> with <see cref="WorkAreaWidthLimit"/> as the limit,
    /// and the window's MaxHeight is <see cref="WorkAreaHeightLimit"/>. Both are
    /// taken again when the text size changes while the window is shown. Each change
    /// of a shown window's size keeps the window's centre where it was, through
    /// <see cref="KeepCentreWhenResized"/>, so a dialog keeps its centre over the
    /// window it opened from as its card grows or shrinks. Once the window is placed,
    /// <see cref="PlaceInsideWorkArea"/> brings back any edge its placement left
    /// outside the work area.
    /// <paramref name="minimumWidth"/> and <paramref name="maximumWidth"/> are the
    /// card's widths at 100% text scale, and the card carries no width of its own
    /// in XAML, so these are the only ones it has.
    /// </summary>
    public static void KeepCardInsideWorkArea(
        this Window window, FrameworkElement card, double minimumWidth, double maximumWidth)
    {
        void Apply()
        {
            var reference = Application.Current?.MainWindow;
            (card.MinWidth, card.MaxWidth) = CardWidths(minimumWidth, maximumWidth,
                AccessibilitySettings.Current.TextScaleFactor, WorkAreaWidthLimit(reference));
            window.MaxHeight = WorkAreaHeightLimit(reference);
        }

        Apply();
        window.OnTextScaleChangeWhileShown(Apply);
        window.Loaded += (_, _) => PlaceInsideWorkArea(window);
        window.SizeChanged += (_, e) => KeepCentreWhenResized(window, e);
    }

    /// <summary>
    /// Moves a shown <paramref name="window"/> by half the change in its size, so its
    /// centre stays where it was. WPF keeps a window's top-left fixed when its size
    /// changes, which leaves a window that sizes to its content off-centre once it
    /// shrinks. An edge the change carries out of the work area of the window's
    /// monitor is brought back to it; an edge already outside it, as on a window the
    /// user has dragged across two monitors, stays where it is. The sizes a window
    /// takes before it is loaded are left alone, so the placement its
    /// <see cref="Window.WindowStartupLocation"/> asks for stands. A window with no
    /// handle yet, or an unresolvable monitor, is left where it is. Only a window the
    /// user cannot resize is moved: on one the user drags to a new size, moving it
    /// would pull the edge the user is not dragging.
    /// </summary>
    public static void KeepCentreWhenResized(Window window, SizeChangedEventArgs e)
    {
        if (window.ResizeMode is ResizeMode.CanResize or ResizeMode.CanResizeWithGrip
            || !window.IsLoaded || e.PreviousSize.Width <= 0 || e.PreviousSize.Height <= 0
            || double.IsNaN(window.Left) || double.IsNaN(window.Top)
            || !TryWorkArea(window, out var work))
            return;

        window.Left = Recentred(window.Left, e.PreviousSize.Width, e.NewSize.Width, work.Left, work.Right);
        window.Top = Recentred(window.Top, e.PreviousSize.Height, e.NewSize.Height, work.Top, work.Bottom);
    }

    /// <summary>
    /// The start of a span that was <paramref name="previousLength"/> long at
    /// <paramref name="start"/> and is now <paramref name="newLength"/> long, moved by
    /// half the change so its centre stays put. Where that carries its far end past
    /// <paramref name="workEnd"/>, or its near end before <paramref name="workStart"/>,
    /// and that end was inside before, the end is brought back to the work area, the
    /// near end winning where both cross. An end within <see cref="EdgeTolerance"/>
    /// past the work-area edge counts as inside, so a window placed flush against the
    /// edge, which rounding to device pixels can leave a fraction over it, is held
    /// there. All values in device-independent units.
    /// </summary>
    public static double Recentred(
        double start, double previousLength, double newLength, double workStart, double workEnd)
    {
        var moved = start + (previousLength - newLength) / 2;
        if (moved + newLength > workEnd && start + previousLength <= workEnd + EdgeTolerance)
            moved = workEnd - newLength;
        if (moved < workStart && start >= workStart - EdgeTolerance)
            moved = workStart;
        return moved;
    }

    /// <summary>
    /// How far past a work-area edge a window's edge may sit and still count as
    /// inside it: half a device pixel at 100% display scale.
    /// </summary>
    private const double EdgeTolerance = 0.5;

    /// <summary>
    /// Moves <paramref name="window"/> so that every edge is inside its monitor's
    /// work area, the left and top edges winning where the window is larger than
    /// the work area, so its title bar stays reachable. A window with no handle yet,
    /// or an unresolvable monitor, is left where it is.
    /// </summary>
    public static void PlaceInsideWorkArea(Window window)
    {
        if (double.IsNaN(window.Left) || double.IsNaN(window.Top) || !TryWorkArea(window, out var work))
            return;

        window.Left = Math.Max(work.Left, Math.Min(window.Left, work.Right - window.ActualWidth));
        window.Top = Math.Max(work.Top, Math.Min(window.Top, work.Bottom - window.ActualHeight));
    }

    /// <summary>
    /// Runs <paramref name="apply"/> each time the text size changes while
    /// <paramref name="window"/> is shown, and once when it gets its handle if the
    /// text size has changed since this was called. The subscription to
    /// <see cref="AccessibilitySettings.Current"/>, which lives as long as the app, is
    /// taken only once the window is shown, so a window built and never shown is not
    /// kept alive by it.
    /// </summary>
    public static void OnTextScaleChangeWhileShown(this Window window, Action apply)
    {
        void OnSettingsChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (AccessibilitySettings.IsTextScaleChange(e))
                apply();
        }

        var factorWhenBuilt = AccessibilitySettings.Current.TextScaleFactor;
        window.SourceInitialized += (_, _) =>
        {
            AccessibilitySettings.Current.PropertyChanged += OnSettingsChanged;
            if (AccessibilitySettings.Current.TextScaleFactor != factorWhenBuilt)
                apply();
        };
        window.Closed += (_, _) => AccessibilitySettings.Current.PropertyChanged -= OnSettingsChanged;
    }

    /// <summary>
    /// Sends each change of <paramref name="window"/>'s size through
    /// <see cref="NudgeIntoWorkArea"/>. A shown window that sizes to its content
    /// grows down and right from a fixed top-left, which can carry its bottom or
    /// right edge out of the work area.
    /// </summary>
    public static void NudgeIntoWorkAreaWhenResized(this Window window)
        => window.SizeChanged += (_, _) => NudgeIntoWorkArea(window);

    /// <summary>
    /// The widest a card may be in a room <paramref name="roomWidth"/> wide when it
    /// keeps <paramref name="margins"/> clear across its two sides, and never below
    /// zero.
    /// </summary>
    public static double RoomLimit(double roomWidth, double margins)
        => Math.Max(0, roomWidth - margins);

    /// <summary>
    /// Holds a card that sits over the window's content inside the room
    /// <paramref name="room"/> gives it, less the card's own left and right margins:
    /// the card's MinWidth and MaxWidth come from <see cref="CardWidths"/> with that
    /// width as the limit. Both are taken at once, and again when the room's width
    /// changes or the text size changes while the window is shown, and never while
    /// the room has not been laid out. <paramref name="minimumWidth"/> and
    /// <paramref name="maximumWidth"/> are the card's widths at 100% text scale,
    /// and the card carries no width of its own in XAML.
    /// </summary>
    public static void KeepCardInsideRoom(
        this Window window, FrameworkElement card, FrameworkElement room,
        double minimumWidth, double maximumWidth)
    {
        void Apply()
        {
            if (room.ActualWidth <= 0)
                return;
            (card.MinWidth, card.MaxWidth) = CardWidths(minimumWidth, maximumWidth,
                AccessibilitySettings.Current.TextScaleFactor,
                RoomLimit(room.ActualWidth, card.Margin.Left + card.Margin.Right));
        }

        Apply();
        OnRoomWidthChange(window, room, Apply);
        window.OnTextScaleChangeWhileShown(Apply);
    }

    /// <summary>
    /// <paramref name="minimum"/>, held to the width a room
    /// <paramref name="roomWidth"/> wide leaves when <paramref name="margins"/>
    /// are kept clear across its two sides.
    /// </summary>
    public static double HeldMinimum(double minimum, double roomWidth, double margins)
        => Math.Min(minimum, RoomLimit(roomWidth, margins));

    /// <summary>
    /// Holds the MinWidth the XAML gives <paramref name="element"/> inside the room
    /// <paramref name="room"/> gives it, less the element's own left and right
    /// margins, through <see cref="HeldMinimum"/>. Taken at once, and again whenever
    /// the room's width changes until the window closes, and never while the room has
    /// not been laid out. Suits a MinWidth that does not scale with the text size; one that
    /// does goes through <see cref="KeepCardInsideRoom"/>.
    /// </summary>
    public static void KeepMinWidthInsideRoom(this Window window, FrameworkElement element, FrameworkElement room)
    {
        var minimum = element.MinWidth;

        void Apply()
        {
            if (room.ActualWidth > 0)
                element.MinWidth = HeldMinimum(minimum, room.ActualWidth, element.Margin.Left + element.Margin.Right);
        }

        Apply();
        OnRoomWidthChange(window, room, Apply);
    }

    /// <summary>
    /// Runs <paramref name="apply"/> each time <paramref name="room"/>'s width
    /// changes, until <paramref name="window"/> closes.
    /// </summary>
    private static void OnRoomWidthChange(Window window, FrameworkElement room, Action apply)
    {
        void OnRoomSizeChanged(object sender, SizeChangedEventArgs e)
        {
            if (e.WidthChanged)
                apply();
        }

        room.SizeChanged += OnRoomSizeChanged;
        window.Closed += (_, _) => room.SizeChanged -= OnRoomSizeChanged;
    }

    /// <summary>
    /// Moves <paramref name="window"/> back inside its monitor's work
    /// area if its bottom or right edge has crossed out. A shown
    /// SizeToContent window grows down and right from a fixed top-left,
    /// so a live OS text-scale increase can push its action rows under
    /// the taskbar even though the window itself still fits the screen;
    /// repositioning restores them. Best effort: a window with no
    /// handle yet, or an unresolvable monitor, is left where it is.
    /// </summary>
    public static void NudgeIntoWorkArea(Window window)
    {
        if (double.IsNaN(window.Left) || double.IsNaN(window.Top) || !TryWorkArea(window, out var work))
            return;

        if (window.Left + window.ActualWidth > work.Right)
            window.Left = Math.Max(work.Left, work.Right - window.ActualWidth);
        if (window.Top + window.ActualHeight > work.Bottom)
            window.Top = Math.Max(work.Top, work.Bottom - window.ActualHeight);
    }

    /// <summary>
    /// The work area of the monitor <paramref name="window"/> is on, in the units
    /// Window.Left and Window.Top are expressed in. False while the window has no
    /// handle, or when Windows names no monitor for it.
    /// </summary>
    private static bool TryWorkArea(Window window, out Rect work)
    {
        work = Rect.Empty;
        var hwnd = new WindowInteropHelper(window).Handle;
        if (hwnd == IntPtr.Zero)
            return false;

        var monitor = User32.MonitorFromWindow(hwnd, User32.MONITOR_DEFAULTTONEAREST);
        var info = new User32.MONITORINFO { cbSize = Unsafe.SizeOf<User32.MONITORINFO>() };
        if (monitor == IntPtr.Zero || !User32.GetMonitorInfo(monitor, ref info))
            return false;

        // rcWork is device pixels; the window's DPI scale converts to
        // the units Window.Left/Top are expressed in.
        var dpi = VisualTreeHelper.GetDpi(window);
        work = new Rect(
            new Point(info.rcWork.Left / dpi.DpiScaleX, info.rcWork.Top / dpi.DpiScaleY),
            new Point(info.rcWork.Right / dpi.DpiScaleX, info.rcWork.Bottom / dpi.DpiScaleY));
        return true;
    }

    private static (double Width, double Height) WorkArea(Window? reference)
    {
        if (reference is not null
            && new WindowInteropHelper(reference).Handle is var hwnd
            && hwnd != IntPtr.Zero)
        {
            var monitor = User32.MonitorFromWindow(hwnd, User32.MONITOR_DEFAULTTONEAREST);
            var info = new User32.MONITORINFO { cbSize = Unsafe.SizeOf<User32.MONITORINFO>() };
            if (monitor != IntPtr.Zero && User32.GetMonitorInfo(monitor, ref info))
            {
                // rcWork is device pixels; the reference window's DPI
                // scale converts to the units WPF sizes windows in.
                var dpi = VisualTreeHelper.GetDpi(reference);
                return ((info.rcWork.Right - info.rcWork.Left) / dpi.DpiScaleX,
                        (info.rcWork.Bottom - info.rcWork.Top) / dpi.DpiScaleY);
            }
        }

        // Primary monitor's work area, already in device-independent units.
        return (SystemParameters.WorkArea.Width, SystemParameters.WorkArea.Height);
    }
}
