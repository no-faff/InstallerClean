using System.Globalization;
using InstallerClean.Services;

namespace InstallerClean.Helpers;

/// <summary>
/// The tip page's address, written once, because it moves. Written out twice, the
/// second copy goes stale on the day the first one changes, and a tip button that
/// opens a page which is no longer there raises no error, fails no check and
/// produces no complaint, because the person who meets it is not the person who
/// would report it.
/// </summary>
public static class SupportLink
{
    /// <summary>
    /// The tip page. The command line prints it as it stands; the app's tip
    /// buttons open it through <see cref="TipPage(IWindowsRegion)"/>, with the
    /// app's language and the Windows region added.
    /// </summary>
    /// <remarks>
    /// Keep this the first https address in the file: the release script reads the
    /// first one as the address the release notes must name.
    /// </remarks>
    public const string Url = "https://nofaff.netlify.app/support";

    /// <summary>
    /// The address the app's tip buttons open: the large and the small one on the
    /// card shown at the end of a run, and the pill in the About window. It reads
    /// the language the app is showing and the Country or region set in Windows at
    /// the moment of the click.
    /// </summary>
    public static string TipPage(IWindowsRegion windowsRegion) =>
        TipPage(Localisation.UiCulture, windowsRegion.Read());

    /// <summary>
    /// <see cref="Url"/> with <c>lang</c>, the language the app shows for
    /// <paramref name="uiCulture"/> (<c>en</c> for English, otherwise the
    /// <see cref="SupportedLanguages"/> name), and <c>region</c>,
    /// <paramref name="windowsRegion"/> in upper case where it is two letters. A
    /// UN M.49 number or a region that could not be read sends no
    /// <c>region</c> at all. The page uses the region only to put that country's
    /// own way to pay after Ko-fi.
    /// </summary>
    public static string TipPage(CultureInfo uiCulture, string? windowsRegion)
    {
        var language = SupportedLanguages.Active(uiCulture);
        if (language == SupportedLanguages.Neutral) language = "en";

        var address = Url + "?lang=" + language;
        if (windowsRegion is { Length: 2 } && windowsRegion.All(char.IsAsciiLetter))
            address += "&region=" + windowsRegion.ToUpperInvariant();
        return address;
    }
}
