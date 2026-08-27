using InstallerClean.Helpers;
using InstallerClean.Resources;
using InstallerClean.Services;

namespace InstallerClean.Tests.Helpers;

/// <summary>
/// The one place either host gets its held-back wording from, and the reason it
/// is one place: the window and the command line must not answer differently for
/// one machine state, and neither can reach the other's printing code. Everything
/// pinned here is pinned once for both.
///
/// IT PINNED A PARTITION OF FOUR SENTENCES UNTIL 3.0.0 and now pins one. The
/// claims worth holding moved with it: not the order of the lines, which no longer
/// exists, but that the mix of causes cannot be read off the sentence and that the
/// count on it is the batch total rather than any one cause's.
/// </summary>
public class HeldBackReportTests
{
    private static string Expected(int count) =>
        string.Format(
            count == 1 ? Strings.Completion_HeldBack_Singular : Strings.Completion_HeldBack_Plural,
            count);

    [Fact]
    public void Nothing_kept_back_produces_no_sentence()
    {
        // Not "a sentence saying zero". Both hosts test this result for emptiness
        // to decide whether the line appears at all, and a run that kept nothing
        // back is the commonest run by far.
        Assert.Equal(string.Empty, HeldBackReport.Line(default));
    }

    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    public void One_cause_produces_the_sentence_at_its_own_count(int count)
    {
        Assert.Equal(Expected(count), HeldBackReport.Line(new HeldBackReasons(Reclaimed: count)));
    }

    [Fact]
    public void Several_causes_produce_one_sentence_carrying_the_batch_total()
    {
        // The collapse itself. Four causes occurred; the reader gets one line and
        // the number on it is every file held back, not the largest cause's count
        // and not the number of causes.
        var reasons = new HeldBackReasons(
            Reclaimed: 4, RecordsChanged: 2, RecordsUnreadable: 1, OwnershipUnestablished: 3);

        Assert.Equal(10, reasons.Total);
        Assert.Equal(Expected(10), HeldBackReport.Line(reasons));
    }

    [Fact]
    public void Two_different_mixes_of_causes_at_one_total_are_indistinguishable()
    {
        // WHAT "NAMES NO CAUSE" MEANS, PINNED RATHER THAN ASSERTED IN A COMMENT.
        // Neither fixture is a degenerate one: the first met a single cause seven
        // times, the second met three different causes, and the third met the one
        // cause that is about the machine rather than about any file. A sentence
        // that leaked which had occurred would separate at least two of these.
        var single = new HeldBackReasons(Reclaimed: 7);
        var mixed = new HeldBackReasons(Reclaimed: 1, RecordsChanged: 4, RecordsUnreadable: 2);
        var machineWide = new HeldBackReasons(OwnershipUnestablished: 7);

        Assert.Equal(7, single.Total);
        Assert.Equal(7, mixed.Total);
        Assert.Equal(7, machineWide.Total);

        var line = HeldBackReport.Line(single);
        Assert.Equal(line, HeldBackReport.Line(mixed));
        Assert.Equal(line, HeldBackReport.Line(machineWide));
    }

    [Fact]
    public void The_count_on_the_sentence_accounts_for_every_file_kept_back()
    {
        // The totals have to add up on screen: acted on + held back = what the
        // user selected. So the sentence carries Total and nothing narrower.
        var reasons = new HeldBackReasons(Reclaimed: 4, RecordsChanged: 2, RecordsUnreadable: 1);

        Assert.Equal(7, reasons.Total);
        Assert.Contains("7", HeldBackReport.Line(reasons), System.StringComparison.Ordinal);
    }

    [Fact]
    public void Two_tallies_add_rather_than_one_standing_in_for_the_other()
    {
        // The fold's arithmetic, and it outlives the partition because the counts
        // do. The pre-act re-verify and the under-lease re-read keep back DIFFERENT
        // files, so their causes accumulate; anything merging them would under-count
        // the one number the sentence prints.
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
        // The per-cause counts still travel in the opt-in result log, which is why
        // they are still worth pinning now that no sentence reads them.
        var reasons = default(HeldBackReasons)
            .Plus(HeldBackReason.Reclaimed)
            .Plus(HeldBackReason.RecordsChanged)
            .Plus(HeldBackReason.RecordsChanged);

        Assert.Equal(new HeldBackReasons(Reclaimed: 1, RecordsChanged: 2), reasons);
    }
}
