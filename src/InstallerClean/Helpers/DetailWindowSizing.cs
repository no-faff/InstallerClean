using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;
using InstallerClean.Interop.Native;

namespace InstallerClean.Helpers;

/// <summary>
/// Startup sizing for the detail windows: as tall as the preferred
/// height allows while staying inside the work area (the monitor minus
/// the taskbar) of the screen the app is on. A fixed default cannot do
/// both: a 1080p laptop at 150% scale has roughly 672 device-independent
/// units of work-area height, while a 100% desktop has roughly 1030.
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
    /// The tallest a window may sensibly open on the monitor hosting
    /// <paramref name="reference"/>: the work-area height less the edge
    /// margin. Suited to a MaxHeight on a SizeToContent window.
    /// </summary>
    public static double WorkAreaHeightLimit(Window? reference)
        => WorkAreaHeight(reference) - EdgeMargin;

    private static double WorkAreaHeight(Window? reference)
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
                return (info.rcWork.Bottom - info.rcWork.Top) / VisualTreeHelper.GetDpi(reference).DpiScaleY;
            }
        }

        // Primary monitor's work area, already in device-independent units.
        return SystemParameters.WorkArea.Height;
    }
}
