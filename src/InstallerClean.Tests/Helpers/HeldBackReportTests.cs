using InstallerClean.Helpers;
using InstallerClean.Resources;
using InstallerClean.Services;

namespace InstallerClean.Tests.Helpers;

/// <summary>
/// The one place either host gets its held-back wording from, and the reason it
/// is one place: the window and the command line must not answer differently for
/// one machine state, and neither can reach the other's printing code. Everything
/// pinned here is pinned once for both.
/// </summary>
public class HeldBackReportTests
{
    private static string Line(string flat, int count) =>
        string.Format(flat, count, DisplayHelpers.PluraliseFile(count));

    [Fact]
    public void Nothing_kept_back_produces_no_lines()
    {
        // Not "a line saying zero". Both hosts test this result for emptiness to
        // decide whether the block appears at all.
        Assert.Empty(HeldBackReport.Lines(default));
    }

    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    public void One_cause_produces_exactly_one_line_carrying_its_own_count(int count)
    {
        var lines = HeldBackReport.Lines(new HeldBackReasons(Reclaimed: count));

        Assert.Equal(new[] { Line(Strings.Completion_ReverifySkipped, count) }, lines);
    }

    [Fact]
    public void A_cause_that_did_not_occur_gets_no_line()
    {
        // The whole point of the partition. A batch that met one cause must not
        // carry a sentence for the other two, at any count including zero.
        var lines = HeldBackReport.Lines(new HeldBackReasons(RecordsChanged: 3));

        Assert.Equal(new[] { Line(Strings.Completion_ReverifyRecordsChanged, 3) }, lines);
        Assert.DoesNotContain(Strings.Completion_ReverifySkipped, lines);
        Assert.DoesNotContain(Strings.Completion_ReverifyIncomplete, lines);
    }

    [Fact]
    public void Every_cause_present_gets_its_own_line_in_the_settled_order()
    {
        // Most specific cause first: what a program was found to need, then what
        // the records no longer hold, then what could not be read at all. The
        // counts stay each cause's own; a line reading the batch total against any
        // one sentence would be the collapse this replaced.
        var lines = HeldBackReport.Lines(
            new HeldBackReasons(Reclaimed: 4, RecordsChanged: 2, RecordsUnreadable: 1));

        Assert.Equal(
            new[]
            {
                Line(Strings.Completion_ReverifySkipped, 4),
                Line(Strings.Completion_ReverifyRecordsChanged, 2),
                Line(Strings.Completion_ReverifyIncomplete, 1),
            },
            lines);
    }

    [Fact]
    public void The_counts_across_the_lines_account_for_every_file_kept_back()
    {
        // The totals have to add up on screen (acted on + kept = the scan's
        // candidates), so no cause may be rendered away.
        var reasons = new HeldBackReasons(Reclaimed: 4, RecordsChanged: 2, RecordsUnreadable: 1);

        Assert.Equal(7, reasons.Total);
        Assert.Equal(3, HeldBackReport.Lines(reasons).Count);
    }

    [Fact]
    public void Two_tallies_add_rather_than_one_standing_in_for_the_other()
    {
        // The fold's arithmetic. The pre-act re-verify and the under-lease re-read
        // keep back different files, so their causes accumulate; anything that
        // merged them would put one sentence over files it is false of.
        var preAct = new HeldBackReasons(Reclaimed: 1, RecordsUnreadable: 2);
        var underLease = new HeldBackReasons(Reclaimed: 3, RecordsChanged: 1);

        Assert.Equal(
            new HeldBackReasons(Reclaimed: 4, RecordsChanged: 1, RecordsUnreadable: 2),
            preAct + underLease);
        Assert.Equal(preAct.Total + underLease.Total, (preAct + underLease).Total);
    }

    [Fact]
    public void Plus_counts_one_more_file_against_the_cause_it_names()
    {
        // How both producers build their tally, one condemned path at a time and
        // in the same statement that adds the path, so the two cannot come apart.
        var reasons = default(HeldBackReasons)
            .Plus(HeldBackReason.Reclaimed)
            .Plus(HeldBackReason.RecordsChanged)
            .Plus(HeldBackReason.RecordsChanged);

        Assert.Equal(new HeldBackReasons(Reclaimed: 1, RecordsChanged: 2), reasons);
    }
}
