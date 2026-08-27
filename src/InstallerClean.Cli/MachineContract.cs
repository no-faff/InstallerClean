using System.Globalization;
using InstallerClean.Helpers;

namespace InstallerClean.Cli;

/// <summary>
/// Builds the CLI's machine-read output in English whatever the OS UI culture.
/// Two consumers match InstallerClean's output on exact English phrases
/// regardless of the machine's language: RMM tooling greps the Application
/// event-log entries, and scripts match the "\d+ errors:" stdout header, whose
/// always-plural shape is held for exactly that (the emit site carries the
/// reasoning, and what is and is not published about it). Those lines are built
/// through here; everything else the operator reads follows the ambient (OS)
/// culture and is localised.
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
    /// Writes one Application-channel entry, built in English via
    /// <see cref="English"/>. The sanctioned path for every event-log write:
    /// routing them all through here keeps a localised noun or size out of the
    /// line RMM greps for a known English phrase. The culture swap and the build
    /// are handed on as a callback rather than run here, so both sit inside
    /// <see cref="EventLogWriter.Write"/>'s guard; run in this frame they would
    /// throw past it to the caller.
    /// </summary>
    /// <remarks>
    /// What a consumer may rely on, because two of these entries are newer than
    /// most tooling watching for them. Every <c>/s</c>, <c>/d</c> or <c>/m</c>
    /// run writes exactly ONE summary entry, and its Event ID is in the 1000,
    /// 2000 or 4000 band (<see cref="CliContract.EventIdFor"/>). Beside it a run
    /// may write NOTICES, in the 3000 band, which are conditions the scan found and
    /// never the run's outcome. THERE ARE THREE. 3000: entries in the records could
    /// not be matched up, so no superseded patch was offered and the count of files
    /// missing from the cache may be short. 3001: packages Windows still references
    /// have no file on disk. 3002: the scan could not establish which cached files
    /// belong to the programs installed here, so it offered nothing it walked. A
    /// notice never replaces the summary and never stands in for one, so counting
    /// runs means counting the summary bands, and each repeats for as long as its
    /// own condition holds, so a machine can emit one on every run for weeks.
    ///
    /// THE LIST HELD TWO AND ONE OF THEM NAMED THE WRONG CLASS. It gave 3000 as the
    /// scan withholding "its superseded and obsoleted verdicts" and left 3002 out
    /// altogether. Nothing withholds an obsoleted registration, because none is ever
    /// offered on any run and so there is no verdict to hold back: the wire text names
    /// the superseded class alone (<c>Cli.EventLogScanWithheld</c>) and
    /// <c>ScanResult.WithheldCount</c> says the same in its own words. A tool built
    /// from this paragraph as the band's contents was watching two IDs of three.
    ///
    /// ONE FIGURE IN THE 3000 NOTICE IS AN ESTIMATE AND NOTHING HAS EVER SAID SO.
    /// Its count of installed products is assembled from four contributors, two
    /// of which are inferred from a headcount and from unclaimed registry values
    /// rather than observed, and the inference can run high as well as low (see
    /// <see cref="Models.InstallerQueryResult.UnaccountedProductCount"/> for what
    /// each contributes). It is sound to alert on and to trend; it is not a figure
    /// to reconcile against an inventory, and a fleet report presenting it as a
    /// headcount of affected programs would be presenting an estimate as a count.
    /// The files figure beside it is exact.
    /// </remarks>
    internal static void WriteEventLog(CliEventClass outcome, Func<string> build) =>
        EventLogWriter.Write(outcome, () => English(build));
}
