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
/// (<see cref="DisplayHelpers.FormatSizeForMachine"/>, which carries no group
/// separator and no unit above gigabytes); the template resolves through
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
    /// <summary>
    /// Runs <paramref name="build"/> with the thread UI and format cultures
    /// forced to en-GB and restores both before returning
    /// (<see cref="MachineCulture.English"/>).
    /// </summary>
    internal static string English(Func<string> build) => MachineCulture.English(build);

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
    /// What a consumer may rely on. Every <c>/s</c>, <c>/d</c> or <c>/m</c>
    /// run writes exactly ONE summary entry, and its Event ID is in the 1000,
    /// 2000 or 4000 band (<see cref="CliContract.EventIdFor"/>), except a run whose
    /// update saw Ctrl+C with nothing run, whose one entry is the 3006 below saying so.
    /// Beside it a run
    /// may write NOTICES, in the 3000 band, which are conditions the scan found and
    /// never the run's outcome. THE LIST BELOW IS THE WHOLE BAND, which is the part
    /// a consumer building a filter needs of it. 3000: the scan could not check some
    /// program entries in Windows Installer's records, or could not match some cached
    /// patch files to a program it asks about, so no superseded patch was listed as
    /// safe to delete. 3001: packages Windows still references have no file on disk.
    /// 3002: files were left alone because the scan could not be certain about them,
    /// and the entry's own text says which condition produced it, so a filter built for
    /// one phrase sees only some of the machines: either the scan could not establish
    /// which cached files belong to the programs installed here and listed as safe to
    /// delete no file it walked, or it could not establish that particular files it
    /// found are unneeded. 3003: superseded files were left alone, with their count,
    /// whatever the reason and whether or not anything was listed as safe to delete
    /// beside them. 3004: the scan, or the check made before a Move or Delete, stopped
    /// waiting for drives or shares while files still had to be checked against them and
    /// left those files alone; the entry names them, says which of the two it was, and
    /// counts the files, so a run whose scan and check each gave one up writes two. The
    /// check's is written as soon as the check returns, so a run refused after it
    /// carries it too. 3005: the run found a newer version and started it, naming both
    /// versions and what it was started to run; that newer version, a process of its own, writes the
    /// run's summary. 3006: the run found a newer version and did not run with it, naming
    /// both versions and why; it says nothing of what then ran, which the summary says.
    /// Where the update itself saw Ctrl+C, the 3006 says nothing ran and is the run's one
    /// entry. 3007: a newer version a copy had downloaded did not put itself in place of
    /// that copy, saying why; the newer version writes it, it is not a run, and it is the
    /// only entry its process writes. 3008: a scan put back into the Installer folder files
    /// an earlier Move or Delete had left in the app's own folder there, naming them; it is written as the files go back, by the window's scan as by
    /// the command line's, and once only, since the files are then back. 3009: a scan left
    /// such files where they lie, naming them, by whichever process scanned, on every scan
    /// that leaves them: one entry for the files every later scan tries again with, and one
    /// for the files left for good as the place a program or patch was installed from. That aside, a notice never replaces the
    /// summary and never stands in for one, so counting runs means counting the summary
    /// bands and that one, and each notice repeats for as long as its own condition
    /// holds, so a machine can emit one on every run for weeks.
    ///
    /// "NEVER THE RUN'S OUTCOME" IS ABOUT COUNTING RUNS AND IS NOT THE OTHER SENSE
    /// OF THE SAME WORDS. <see cref="CliContract.EntryTypeFor"/> separates a
    /// standing property of the machine from a run that fell short of its job, and
    /// on that question 3001 is the standing property while 3000, 3002, 3003 and 3004
    /// are about the run, which is why 3001 is Information and the other four are
    /// Warning. 3005 is Information because the run fell short of nothing. 3006 is
    /// Information where the copy does not update for a reason true on every run, and Warning
    /// where the run went on with an older version than it set out to use, or ran nothing.
    /// 3007 is Warning: the update fell short. 3008 is Information: the run fell short of
    /// nothing. 3009 is Warning for the files a later scan tries again with, which the run
    /// fell short of putting back, and Information for the files left for good. Nothing here
    /// contradicts that: this paragraph says only that a notice is not the run's SUMMARY
    /// entry, which is what a consumer counting runs is asking.
    /// Two questions, near-identical wording, and the sense that travels between
    /// them is the one that would put two Event IDs on the wrong side of an
    /// entry-type decision.
    ///
    /// NOTHING WITHHOLDS AN OBSOLETED REGISTRATION, because none is ever offered on
    /// any run and so there is no verdict to hold back. The wire text names the
    /// superseded class alone (<c>Cli.EventLogScanWithheld</c> and
    /// <c>Cli.EventLogScanWithheldPatchFiles</c>) and <c>ScanResult.WithheldCount</c>
    /// says the same in its own words.
    ///
    /// THE 3000 NOTICE COUNTS ENTRIES OR FILES, NEVER PROGRAMS. Its first form counts
    /// program entries in Windows Installer's records the scan could not check, which
    /// include registry keys whose name is no product code and so need not be programs
    /// (see <see cref="Models.InstallerQueryResult.UnaccountedProductCount"/>); its
    /// second counts cached patch files the scan could not match to a program it asks
    /// about. Both figures are exact. A fleet report presenting either as a headcount of
    /// affected programs would be presenting a count of something else.
    /// </remarks>
    internal static void WriteEventLog(CliEventClass outcome, Func<string> build) =>
        EventLogWriter.Write(outcome, () => English(build));
}
