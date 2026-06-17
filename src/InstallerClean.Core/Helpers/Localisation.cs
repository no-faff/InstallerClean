using System.Globalization;

namespace InstallerClean.Helpers;

/// <summary>
/// The culture the app resolves resx strings and formats numbers against.
/// </summary>
/// <remarks>
/// The resx lookups (the XAML <c>Translate</c> extension and the generated
/// <c>Strings</c> accessor) and <c>DisplayHelpers</c>'s size/elapsed formatting
/// read <see cref="UiCulture"/>/<see cref="FormatCulture"/> rather than
/// <see cref="CultureInfo.CurrentUICulture"/>/<see cref="CultureInfo.CurrentCulture"/>
/// directly. A culture assigned to the current thread does not reliably persist
/// across the WPF dispatcher's per-callback execution context: with only the
/// thread culture set, the main window (built during startup) rendered in the
/// chosen language while every window opened later from a click fell back to the
/// OS language. An explicit override here is honoured by every thread and window.
/// When no override is set, both fall back to the ambient thread culture, so the
/// Automatic case (follow the OS language) and the CLI's English UICulture pin
/// keep working unchanged.
/// </remarks>
public static class Localisation
{
    public static CultureInfo? UiCultureOverride { get; private set; }
    public static CultureInfo? FormatCultureOverride { get; private set; }

    public static CultureInfo UiCulture => UiCultureOverride ?? CultureInfo.CurrentUICulture;
    public static CultureInfo FormatCulture => FormatCultureOverride ?? CultureInfo.CurrentCulture;

    /// <summary>Pins the resx and formatting cultures for the rest of the process.</summary>
    public static void Set(CultureInfo uiCulture, CultureInfo formatCulture)
    {
        UiCultureOverride = uiCulture;
        FormatCultureOverride = formatCulture;
    }
}
