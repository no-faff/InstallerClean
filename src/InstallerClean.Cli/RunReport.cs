using InstallerClean.Models;

namespace InstallerClean.Cli;

/// <summary>
/// What a run hands to the offer to send its report. The work fills in
/// <see cref="Entry"/> where the run finished and is the PC's first; the run then asks
/// once its work is over and the single-instance mutex is released, so a person at the
/// question holds nothing up.
/// </summary>
internal sealed class RunReport(bool canAsk)
{
    /// <summary>
    /// Whether somebody can see the console and answer in it. Where not, the run builds
    /// no report, writes none and sends none.
    /// </summary>
    public bool CanAsk { get; } = canAsk;

    /// <summary>
    /// The report of the run, set where the run finished and the PC had no earlier run;
    /// null otherwise.
    /// </summary>
    public ResultLogEntry? Entry { get; set; }

    /// <summary>
    /// Whether a Delete or Move that acted on files held its tip line back for the end
    /// of the run, because the run goes on to ask about the report it carried or to send
    /// one an earlier run left waiting. Main prints the line after that.
    /// </summary>
    public bool DonationHeld { get; set; }
}
