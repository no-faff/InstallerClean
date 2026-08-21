using System.Reflection;
using InstallerClean.Models;
using InstallerClean.Services;

namespace InstallerClean.Tests.Models;

/// <summary>
/// The identity-read tally as a STRUCTURE rather than as a number: that every member
/// of <see cref="FileIdentityRead"/> reaches a counter of its own, and that the set
/// of members inside <see cref="FileIdentityReadTally.RefusedTotal"/> is exactly the
/// set <see cref="FileIdentityReadOutcomes.GivesUpAWithholding"/> acts on.
///
/// WHY THAT SECOND ONE IS THE POINT OF THE FILE. The membership is written twice and
/// it has to be. The predicate is the COMPLEMENT of the two settled answers, so a
/// member added to the enum withholds by default instead of slipping through; the
/// total has to NAME its four, because a count cannot be written as a complement.
/// Two spellings of one rule is exactly the shape that drifts, and the drift is
/// silent in the direction that matters: the scan would keep withholding on the new
/// member while the report said nothing had failed, or the report would count a
/// refusal the scan did not act on. Enumerating the members is what makes either
/// visible.
///
/// FIELDS ARE NOT WALKED HERE AND THE CONSTRUCTOR'S PARAMETERS ARE. The tally is a
/// readonly record struct, so every backing field is init-only and a field sweep
/// finds none of them; a property sweep would pick up
/// <see cref="FileIdentityReadTally.RefusedTotal"/>, which is derived from four of
/// the others and would double-count. The primary constructor's parameters are the
/// counters this type declares, and a counter added there is picked up here without
/// anybody remembering an exclusion list.
///
/// WHAT THIS FILE DOES NOT REACH, said plainly so it is not read as end-to-end
/// cover. Nothing here calls the production reader, so the mapping from a Win32
/// failure to an outcome is untested: whether <c>CreateFile</c> really answers
/// <c>ERROR_FILE_NOT_FOUND</c> for the case this app calls an absence is a fact
/// about Windows and cannot be established off it. What is covered is that every
/// outcome is wired to a counter, and that the counters and the rule agree about
/// which of them withhold.
/// </summary>
public class FileIdentityReadTallyTests
{
    /// <summary>
    /// The tally's counters, less the attempt count, which moves on every read and
    /// so is not evidence about any one outcome.
    /// </summary>
    private static string[] Counters() =>
        typeof(FileIdentityReadTally)
            .GetConstructors()
            .OrderByDescending(c => c.GetParameters().Length)
            .First()
            .GetParameters()
            .Select(p => p.Name!)
            .Where(n => n != nameof(FileIdentityReadTally.AttemptCount))
            .OrderBy(n => n, StringComparer.Ordinal)
            .ToArray();

    private static int Read(string counter, FileIdentityReadTally tally) =>
        (int)typeof(FileIdentityReadTally).GetProperty(counter, BindingFlags.Instance | BindingFlags.Public)!
            .GetValue(tally)!;

    private static string[] Moved(FileIdentityReadTally tally) =>
        Counters().Where(c => Read(c, tally) != 0).ToArray();

    private static FileIdentityReadTally Recording(FileIdentityRead outcome)
    {
        var tally = new FileSystemScanService.IdentityReadTally();
        tally.Record(outcome);
        return tally.Taken();
    }

    [Fact]
    public void Every_outcome_reaches_a_counter_of_its_own()
    {
        // Record is a switch with no default arm, so a member added to
        // FileIdentityRead and forgotten there is counted nowhere: the attempts and
        // the outcomes then disagree with nothing to say which of them is short.
        var seen = new Dictionary<string, FileIdentityRead>(StringComparer.Ordinal);

        foreach (var outcome in Enum.GetValues<FileIdentityRead>())
        {
            // Read is deliberately not counted: it is the attempts less the five,
            // and a stored copy could disagree with them. Asserted below as this
            // walk's must-miss control rather than merely skipped.
            if (outcome == FileIdentityRead.Read) continue;

            var moved = Moved(Recording(outcome));

            Assert.True(moved.Length == 1,
                $"FileIdentityRead.{outcome} moved {moved.Length} counters ({string.Join(", ", moved)}). "
                + "Every outcome needs exactly one arm in IdentityReadTally.Record and one counter in "
                + "Taken; the switch has no default, so a missing arm counts the outcome nowhere and a "
                + "report reads it as an outcome that never occurred.");
            Assert.True(!seen.TryGetValue(moved[0], out var already),
                $"FileIdentityRead.{outcome} and FileIdentityRead.{already} both count into {moved[0]}, "
                + "so a report cannot tell the two apart.");
            seen[moved[0]] = outcome;
        }
    }

    [Fact]
    public void A_read_that_answered_is_counted_as_no_failure_at_all()
    {
        // THE MUST-MISS CONTROL for the walk above. Without it, a Record that
        // incremented something unconditionally would satisfy every case in it.
        var tally = Recording(FileIdentityRead.Read);

        Assert.Empty(Moved(tally));
        Assert.Equal(1, tally.AttemptCount);
    }

    [Fact]
    public void The_refusal_total_holds_exactly_the_outcomes_the_scan_acts_on()
    {
        // THE ONE THAT HOLDS THE TWO SPELLINGS OF THE MEMBERSHIP TOGETHER. The scan
        // acts on GivesUpAWithholding and the report is read off RefusedTotal, and
        // an outcome in one and not the other is a machine whose offer was emptied
        // by something its report says did not happen, or the reverse.
        foreach (var outcome in Enum.GetValues<FileIdentityRead>())
        {
            var tally = Recording(outcome);

            Assert.True(tally.AnyUnestablished == outcome.GivesUpAWithholding(),
                $"FileIdentityRead.{outcome} is {(outcome.GivesUpAWithholding() ? "" : "not ")}acted on "
                + $"as a give-up and is {(tally.AnyUnestablished ? "" : "not ")}inside RefusedTotal. "
                + "Both spellings of that membership have to move together: the predicate is written "
                + "as the complement of the settled answers so a new member withholds, and the total "
                + "names its members because a count cannot be a complement.");
        }
    }

    [Fact]
    public void A_file_that_is_not_there_is_counted_and_is_not_a_refusal()
    {
        // THE MEMBER THE WHOLE SPLIT WAS MADE FOR, pinned on its own because it is
        // the one that decides whether an ordinary machine keeps its offer. A
        // registration whose cached file has gone claims none of the walked files,
        // so nothing was given up by failing to identify it. Counted as a refusal it
        // would empty the offer on every machine that has uninstalled anything.
        var tally = Recording(FileIdentityRead.NamesNothing);

        Assert.Equal(1, tally.NamesNothingCount);
        Assert.Equal(0, tally.RefusedTotal);
        Assert.False(tally.AnyUnestablished);
    }

    [Fact]
    public void A_side_nobody_asked_reads_as_a_pass_that_never_ran()
    {
        // Five zero failures on a side that was never asked look identical on the
        // wire to five clean answers, and the attempt count is the only thing that
        // separates them. A default tally is the shape a scan leaves behind when the
        // walk found no candidates or the records held no registrations.
        var never = default(FileIdentityReadTally);

        Assert.Equal(0, never.AttemptCount);
        Assert.Equal(0, never.RefusedTotal);
        Assert.False(never.AnyUnestablished);
    }

    [Fact]
    public void Every_read_moves_the_attempt_count()
    {
        // The five failures are read against this, so an outcome that did not move
        // it would report a machine as having failed more reads than it made.
        foreach (var outcome in Enum.GetValues<FileIdentityRead>())
            Assert.Equal(1, Recording(outcome).AttemptCount);
    }
}
