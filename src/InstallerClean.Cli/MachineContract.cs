using System.Globalization;
using InstallerClean.Helpers;

namespace InstallerClean.Cli;

/// <summary>
/// Builds the CLI's machine-read output in English whatever the OS UI culture.
/// Two consumers match InstallerClean's output on exact English phrases
/// regardless of the machine's language: RMM tooling greps the Application
/// event-log entries, and scripts match the "\d+ errors:" stdout header. Those
/// lines are built through here; everything else the operator reads follows the
/// ambient (OS) culture and is localised.
/// </summary>
/// <remarks>
/// The mechanism is a thread-culture swap, and it is load-bearing that it
/// reaches the whole line, not just the resx template. A summary interpolates a
/// pluralised noun (<see cref="DisplayHelpers.PluraliseError"/>) and a size
/// (<see cref="DisplayHelpers.FormatSize"/>); the template resolves through
/// <see cref="Localisation.UiCulture"/>, the noun through the same, the size
/// through <see cref="Localisation.FormatCulture"/>. Both of those fall through
/// to the thread's <see cref="CultureInfo.CurrentUICulture"/> /
/// <see cref="CultureInfo.CurrentCulture"/> only while no override is set
/// (Localisation.cs), and the CLI never calls <see cref="Localisation.Set"/>, so
/// swapping the two thread cultures forces the template, the noun and the size
/// to en-GB together. A "3,2 GB" size or an "errori" noun reaching the audit
/// line is the failure this prevents.
///
/// A future --lang override would break that. <see cref="Localisation.Set"/>
/// pins UiCultureOverride/FormatCultureOverride ABOVE the thread culture, so the
/// swap below would no longer reach the template, noun or size, only the raw
/// interpolated integers, and the machine line would render in the chosen
/// language. Such a flag has to force <see cref="Localisation"/> back to en-GB
/// for the machine build too, not only the thread culture.
/// </remarks>
internal static class MachineContract
{
    private static readonly CultureInfo MachineCulture = CultureInfo.GetCultureInfo("en-GB");

    /// <summary>
    /// Runs <paramref name="build"/> with the thread UI and format cultures
    /// forced to en-GB and restores both before returning. The restore is in a
    /// finally so a throw mid-build cannot leak en-GB onto the thread; every
    /// caller builds synchronously on one thread, so the swap never spans an
    /// await.
    /// </summary>
    internal static string English(Func<string> build)
    {
        var ui = CultureInfo.CurrentUICulture;
        var format = CultureInfo.CurrentCulture;
        CultureInfo.CurrentUICulture = MachineCulture;
        CultureInfo.CurrentCulture = MachineCulture;
        try
        {
            return build();
        }
        finally
        {
            CultureInfo.CurrentUICulture = ui;
            CultureInfo.CurrentCulture = format;
        }
    }

    /// <summary>
    /// Writes one Application-channel summary, built in English via
    /// <see cref="English"/>. The sanctioned path for every event-log write:
    /// routing them all through here keeps a localised noun or size out of the
    /// line RMM greps for a known English phrase.
    /// </summary>
    internal static void WriteEventLog(CliEventClass outcome, Func<string> build) =>
        EventLogWriter.Write(outcome, English(build));
}
