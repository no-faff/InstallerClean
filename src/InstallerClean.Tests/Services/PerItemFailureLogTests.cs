using InstallerClean.Services;

namespace InstallerClean.Tests.Services;

/// <summary>
/// Pins the crash-log budget a run spends on its per-item failures. The contract
/// these protect: an ordinary partial failure is logged item by item, a storm of
/// one known cause cannot evict the crash history, a cause never seen before is
/// logged however late in the run it arrives, and the closing entry says where
/// the detail went in words true of the caller that wrote it.
///
/// The sink is injected, so nothing here touches the real crash.log.
/// </summary>
public class PerItemFailureLogTests
{
    private const string MoveTrail = "The per-file list is on the completion screen and in the result log.";
    private const string ScanTrail = "There is no other record of which files these were.";

    /// <summary>
    /// An IOException with a chosen HRESULT, which is half the cause identity
    /// the budget de-duplicates on (the type is the other half).
    /// </summary>
    private static IOException Io(int hresult) => new("boom") { HResult = hresult };

    [Fact]
    public void Logs_every_failure_in_full_while_within_the_budget()
    {
        var written = new List<Exception>();
        var log = new PerItemFailureLog("Move", MoveTrail, written.Add);

        for (int i = 0; i < 20; i++) log.Record(Io(unchecked((int)0x80070070)));
        log.WriteClosingEntry();

        // All twenty, and no closing entry: nothing was left out, so claiming
        // otherwise would be a lie in the log.
        Assert.Equal(20, written.Count);
        Assert.All(written, e => Assert.IsType<IOException>(e));
    }

    [Fact]
    public void Suppresses_repeats_of_a_known_cause_once_the_budget_is_spent()
    {
        var written = new List<Exception>();
        var log = new PerItemFailureLog("Move", MoveTrail, written.Add);

        // The realistic storm: a destination volume that filled mid-batch fails
        // every remaining file with the same code.
        for (int i = 0; i < 25; i++) log.Record(Io(unchecked((int)0x80070070)));
        log.WriteClosingEntry();

        Assert.Equal(21, written.Count);                  // 20 in full + the closing entry
        var closing = Assert.IsType<InvalidOperationException>(written[^1]);
        Assert.Contains("Move", closing.Message);
        Assert.Contains("5 further failures", closing.Message);
    }

    [Fact]
    public void Logs_a_cause_never_seen_before_even_past_the_budget()
    {
        var written = new List<Exception>();
        var log = new PerItemFailureLog("Delete", MoveTrail, written.Add);

        for (int i = 0; i < 30; i++) log.Record(Io(unchecked((int)0x80070070)));
        // Arrives long after the budget is spent, and is the one entry in this
        // batch worth having: a flat cap would have thrown it away.
        var novel = new UnauthorizedAccessException("denied");
        log.Record(novel);
        log.WriteClosingEntry();

        Assert.Contains(novel, written);
    }

    [Fact]
    public void Distinguishes_causes_by_hresult_not_just_exception_type()
    {
        var written = new List<Exception>();
        var log = new PerItemFailureLog("Move", MoveTrail, written.Add);

        for (int i = 0; i < 25; i++) log.Record(Io(unchecked((int)0x80070070)));
        // Same exception type, different Win32 code: a sharing violation
        // arriving during a disk-full storm is a different diagnosis.
        log.Record(Io(unchecked((int)0x80070020)));

        Assert.Equal(21, written.Count);
        Assert.Equal(unchecked((int)0x80070020), written[^1].HResult);
    }

    [Fact]
    public void Distinguishes_causes_a_synthesised_exception_cannot_tell_apart()
    {
        var written = new List<Exception>();
        var log = new PerItemFailureLog("Scan", ScanTrail, written.Add);

        // Every entry the scan writes is a synthesised InvalidOperationException,
        // so all four of its refusal kinds carry the same type AND the same
        // HRESULT. Without the cause string the first kind would spend the
        // budget and silently swallow the other three.
        for (int i = 0; i < 25; i++)
            log.Record(new InvalidOperationException($"refused {i}"), cause: "walk/Refused");

        var otherKind = new InvalidOperationException("could not be read");
        log.Record(otherKind, cause: "walk/Unproven");
        var thirdKind = new InvalidOperationException("patch refused");
        log.Record(thirdKind, cause: "removable-patch/Refused");

        Assert.Contains(otherKind, written);
        Assert.Contains(thirdKind, written);
        Assert.Equal(22, written.Count);                  // 20 in full + the two novel causes
    }

    [Fact]
    public void Closing_entry_carries_the_callers_own_account_of_where_the_detail_went()
    {
        var written = new List<Exception>();
        var log = new PerItemFailureLog("Scan", ScanTrail, written.Add);

        for (int i = 0; i < 25; i++)
            log.Record(new InvalidOperationException($"refused {i}"), cause: "walk/Refused");
        log.WriteClosingEntry();

        var closing = Assert.IsType<InvalidOperationException>(written[^1]);
        // A scan's refused candidate reaches no completion screen and no result
        // log, so the Move and Delete wording would be false here.
        Assert.Contains(ScanTrail, closing.Message);
        Assert.DoesNotContain("completion screen", closing.Message);
        Assert.Contains("1 distinct cause ", closing.Message);
    }

    [Fact]
    public void Writes_nothing_at_all_for_a_batch_that_had_no_failures()
    {
        var written = new List<Exception>();
        var log = new PerItemFailureLog("Move", MoveTrail, written.Add);

        log.WriteClosingEntry();

        Assert.Empty(written);
    }
}
