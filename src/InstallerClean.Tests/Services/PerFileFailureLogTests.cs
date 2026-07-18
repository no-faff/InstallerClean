using InstallerClean.Services;

namespace InstallerClean.Tests.Services;

/// <summary>
/// Pins the crash-log budget a Move or Delete batch spends on its per-file
/// failures. The contract these protect: an ordinary partial failure is logged
/// file by file, a storm of one known cause cannot evict the crash history, and
/// a cause never seen before is logged however late in the batch it arrives.
///
/// The sink is injected, so nothing here touches the real crash.log.
/// </summary>
public class PerFileFailureLogTests
{
    /// <summary>
    /// An IOException with a chosen HRESULT, which is half the cause identity
    /// the budget de-duplicates on (the type is the other half).
    /// </summary>
    private static IOException Io(int hresult) => new("boom") { HResult = hresult };

    [Fact]
    public void Logs_every_failure_in_full_while_within_the_budget()
    {
        var written = new List<Exception>();
        var log = new PerFileFailureLog("Move", written.Add);

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
        var log = new PerFileFailureLog("Move", written.Add);

        // The realistic storm: a destination volume that filled mid-batch fails
        // every remaining file with the same code.
        for (int i = 0; i < 25; i++) log.Record(Io(unchecked((int)0x80070070)));
        log.WriteClosingEntry();

        Assert.Equal(21, written.Count);                  // 20 in full + the closing entry
        var closing = Assert.IsType<InvalidOperationException>(written[^1]);
        Assert.Contains("Move", closing.Message);
        Assert.Contains("5 further per-file failures", closing.Message);
    }

    [Fact]
    public void Logs_a_cause_never_seen_before_even_past_the_budget()
    {
        var written = new List<Exception>();
        var log = new PerFileFailureLog("Delete", written.Add);

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
        var log = new PerFileFailureLog("Move", written.Add);

        for (int i = 0; i < 25; i++) log.Record(Io(unchecked((int)0x80070070)));
        // Same exception type, different Win32 code: a sharing violation
        // arriving during a disk-full storm is a different diagnosis.
        log.Record(Io(unchecked((int)0x80070020)));

        Assert.Equal(21, written.Count);
        Assert.Equal(unchecked((int)0x80070020), written[^1].HResult);
    }

    [Fact]
    public void Writes_nothing_at_all_for_a_batch_that_had_no_failures()
    {
        var written = new List<Exception>();
        var log = new PerFileFailureLog("Move", written.Add);

        log.WriteClosingEntry();

        Assert.Empty(written);
    }
}
