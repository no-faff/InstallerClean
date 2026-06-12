using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;
using InstallerClean.Interop.Native;

namespace InstallerClean.Helpers;

/// <summary>
/// Window sizing against the work area (the monitor minus the taskbar)
/// of the screen the app is on: startup heights for the detail windows,
/// and MaxWidth / MaxHeight limits for the SizeToContent windows so
/// large OS text scales grow them up to the screen and no further. A
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
    /// <paramref name="preferred"/> clamped to the work area of the
    /// monitor hosting <paramref name="reference"/> (the primary
    /// monitor when the reference is null or not yet shown), and never
    /// below <paramref name="minimum"/>. All values in
    /// device-independent units.
    /// </summary>
    public static double ClampHeightToWorkArea(Window? reference, double preferred, double minimum)
        => Math.Max(minimum, Math.Min(preferred, WorkAreaHeightLimit(reference)));

    /// <summary>
    /// Horizontal counterpart of
    /// <see cref="ClampHeightToWorkArea(Window?, double, double)"/>.
    /// </summary>
    public static double ClampWidthToWorkArea(Window? reference, double preferred, double minimum)
        => Math.Max(minimum, Math.Min(preferred, WorkAreaWidthLimit(reference)));

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
        var hwnd = new WindowInteropHelper(window).Handle;
        if (hwnd == IntPtr.Zero || double.IsNaN(window.Left) || double.IsNaN(window.Top))
            return;

        var monitor = User32.MonitorFromWindow(hwnd, User32.MONITOR_DEFAULTTONEAREST);
        var info = new User32.MONITORINFO { cbSize = Unsafe.SizeOf<User32.MONITORINFO>() };
        if (monitor == IntPtr.Zero || !User32.GetMonitorInfo(monitor, ref info))
            return;

        // rcWork is device pixels; the window's DPI scale converts to
        // the units Window.Left/Top are expressed in.
        var dpi = VisualTreeHelper.GetDpi(window);
        var workLeft = info.rcWork.Left / dpi.DpiScaleX;
        var workTop = info.rcWork.Top / dpi.DpiScaleY;
        var workRight = info.rcWork.Right / dpi.DpiScaleX;
        var workBottom = info.rcWork.Bottom / dpi.DpiScaleY;

        if (window.Left + window.ActualWidth > workRight)
            window.Left = Math.Max(workLeft, workRight - window.ActualWidth);
        if (window.Top + window.ActualHeight > workBottom)
            window.Top = Math.Max(workTop, workBottom - window.ActualHeight);
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
