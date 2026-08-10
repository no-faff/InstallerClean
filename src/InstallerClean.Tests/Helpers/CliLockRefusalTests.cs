using System.Globalization;
using InstallerClean.Cli;
using InstallerClean.Resources;

namespace InstallerClean.Tests.Helpers;

/// <summary>
/// The two lines a <c>/d</c> or <c>/m</c> emits when the action service refused
/// the batch for want of <c>Global\_MSIExecute</c>: the sentence the operator
/// reads and the Application-channel entry an RMM matches on.
/// </summary>
/// <remarks>
/// What is NOT held here, so nobody reads this file as covering the branch: the
/// emitting method is private and stays that way, because reaching it would mean
/// a test run writing a real entry to the Application log. So nothing in the
/// suite proves that a refused batch calls it, or that it exits transient. Those
/// hold by inspection of two call sites and the exit code the method returns, and
/// a change that stopped calling it would pass everything here.
/// </remarks>
public class CliLockRefusalTests
{
    [Fact]
    public void The_stdout_sentence_follows_the_flag_that_ran()
    {
        // The selection is the whole of what this can get wrong, the two sentences
        // being interchangeable as far as the compiler is concerned. Each closes
        // by naming what did not happen to the files, so the pair swapped over
        // tells an operator their files were moved when they were deleted.
        Assert.Equal(Strings.Cli_InstallerLockUnavailable, Program.InstallerLockUnavailableLine("/d"));
        Assert.Equal(Strings.Cli_MoveInstallerLockUnavailable, Program.InstallerLockUnavailableLine("/m"));
        // And that they are two sentences rather than one key wired to both,
        // which the assertions above would not notice.
        Assert.NotEqual(
            Program.InstallerLockUnavailableLine("/d"),
            Program.InstallerLockUnavailableLine("/m"));
    }

    [Theory]
    [InlineData("/d")]
    [InlineData("/m")]
    public void The_event_log_line_opens_by_naming_the_flag(string arg)
    {
        // Every entry this tool writes opens with the flag, so that a fleet's
        // history can be filtered by what was actually run. One of them did not,
        // and a search for delete runs on those machines returned nothing.
        Assert.StartsWith(arg, Program.InstallerLockUnavailableEventLogLine(arg), StringComparison.Ordinal);
    }

    [Fact]
    public void The_event_log_line_is_one_line_and_its_remainder_serves_both_flags()
    {
        // One entry covers /d and /m, and the flag it opens with is the only
        // thing that may vary with which ran. Everything after it is read by
        // both, which is what this pins.
        var delete = MachineContract.English(() => Program.InstallerLockUnavailableEventLogLine("/d"));
        var move = MachineContract.English(() => Program.InstallerLockUnavailableEventLogLine("/m"));

        Assert.Equal(delete["/d".Length..], move["/m".Length..]);

        // What that shared remainder may not do, and the fault it had: it ended
        // "so nothing was deleted", which was false of every /m run that could
        // produce it. So a wording naming one of the two actions has to name the
        // other. A wording naming neither is fine and passes here on purpose,
        // "no files were touched" being true of both; what cannot stand is a
        // sentence true of half the runs it is written for.
        Assert.Equal(
            delete.Contains("deleted", StringComparison.OrdinalIgnoreCase),
            delete.Contains("moved", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void The_event_log_line_reads_English_on_a_machine_that_is_not_and_leaves_the_thread_as_it_found_it()
    {
        // Two properties, and the second is the one with teeth. The line is
        // machine-read, so it is built inside MachineContract.English and an RMM
        // gets the same words whatever language Windows is in. That much would
        // also hold today without the forcing, every satellite stripping the
        // machine-contract keys, so the English assertion is holding the guarantee
        // rather than proving the mechanism is what delivers it.
        //
        // The restore is different: English swaps two thread cultures and puts
        // them back in a finally, and a leak would leave en-GB on the thread for
        // everything that ran afterwards, sizes and nouns included. Nothing else
        // in the suite holds that.
        //
        // Safe to write the thread cultures because the assembly disables test
        // parallelisation (AssemblyInfo.cs, for a reason of its own).
        var ui = CultureInfo.CurrentUICulture;
        var format = CultureInfo.CurrentCulture;
        var italian = CultureInfo.GetCultureInfo("it-IT");
        try
        {
            CultureInfo.CurrentUICulture = italian;
            CultureInfo.CurrentCulture = italian;

            var line = MachineContract.English(() => Program.InstallerLockUnavailableEventLogLine("/d"));

            Assert.Contains("mode aborted", line, StringComparison.Ordinal);
            Assert.Contains("could not be acquired", line, StringComparison.Ordinal);
            // By name rather than by instance: what matters is the culture the
            // thread is left in, not which object carries it.
            Assert.Equal("it-IT", CultureInfo.CurrentUICulture.Name);
            Assert.Equal("it-IT", CultureInfo.CurrentCulture.Name);
        }
        finally
        {
            CultureInfo.CurrentUICulture = ui;
            CultureInfo.CurrentCulture = format;
        }
    }
}
