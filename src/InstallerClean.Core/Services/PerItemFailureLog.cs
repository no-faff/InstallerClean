using InstallerClean.Helpers;

namespace InstallerClean.Services;

/// <summary>
/// Crash-log budget for one run that can fail, or refuse, once per item of
/// whatever it repeats over: a file, a registry key, an installed product. A
/// per-item failure is caught, categorised and either shown to the user or
/// counted, and before this existed the exception itself was dropped on the
/// floor: on 2026-07-18 two moves failed as IOException and left no trace
/// anywhere, which is the opposite of the rule that a catch block logs the full
/// exception.
///
/// Logging every one unconditionally trades that for a worse failure. crash.log
/// rotates at 512 KB with a single archive, and every-item failure modes are
/// reachable in both directions. On the Move and Delete side, the destination
/// write probe writes a zero-byte file, so a volume with room for that and not
/// for the batch fails on every remaining file. On the scan side, anything that
/// makes the containment guard refuse wholesale refuses every candidate; driven
/// at 100,000 refusals it wrote 19 MB across 37 rotations, and the crash history
/// that was in the file before the run did not survive. In the query service, a
/// DACL or hive problem across the UserData subtree fails a read per registered
/// product and per registered patch, and those entries carry a real stack trace
/// each. A few hundred of them are enough to evict the history behind
/// near-identical copies of one already-recorded cause.
///
/// So the budget spends its entries on causes rather than on items. The first
/// <see cref="Budget"/> are logged in full, which covers every realistic partial
/// failure outright. Past that, one is still logged in full if its cause has not
/// been seen in this run, so a novel cause arriving at item 500 is never lost to
/// a storm of a known one; identical repeats only increment a counter.
/// <see cref="WriteClosingEntry"/> then records what was left out, so the log
/// never implies it holds everything.
///
/// A cause is the exception's type and HRESULT, plus whatever
/// <see cref="Record"/>'s caller adds. The addition is what makes the escape
/// hatch work for a caller whose entries are synthesised rather than caught:
/// every <c>new InvalidOperationException(...)</c> carries the same type and the
/// same HRESULT (-2146233079), so a caller distinguishing its refusals only by
/// message would collapse all of them into one cause and lose every kind but the
/// first past the budget.
///
/// One instance per run, used from the single thread that runs the loop, so it
/// needs no synchronisation of its own.
/// </summary>
/// <param name="operationKind">
/// Names the run in the closing entry: Move, Delete, Scan, Registry fallback,
/// Patch enumeration.
/// </param>
/// <param name="detailTrail">
/// The closing entry's last sentence, saying where the detail the suppressed
/// entries would have carried can still be found, or that it is nowhere. It is a
/// per-caller string because the answer differs and a wrong one is a lie in the
/// log: a Move or Delete failure is on the completion screen and in the result
/// log, where a scan's refused candidate reaches neither, having been dropped
/// before the result was built, and an abandoned patch enumeration is the only
/// place the product's identity is written down at all.
/// </param>
/// <param name="write">
/// The sink, defaulting to crash.log. Tests pass their own so pinning the budget
/// does not append two dozen entries to the real log on whatever machine runs
/// the suite.
/// </param>
internal sealed class PerItemFailureLog(string operationKind, string detailTrail, Action<Exception>? write = null)
{
    private readonly Action<Exception> _write = write ?? (ex => CrashLog.TryWrite(ex));

    /// <summary>
    /// Full entries logged before the cause filter takes over. Twenty is above
    /// any per-item failure count a user would sit and read through, so an
    /// ordinary partial failure is logged item by item and the filter only ever
    /// engages on a storm.
    /// </summary>
    private const int Budget = 20;

    private readonly HashSet<(Type, int, string?)> _seenCauses = [];
    private int _logged;
    private int _suppressed;

    /// <summary>
    /// Logs <paramref name="ex"/> in full if it is within the run's budget or
    /// carries a cause not yet seen in this run; otherwise counts it as
    /// suppressed for <see cref="WriteClosingEntry"/>.
    /// </summary>
    /// <param name="cause">
    /// Distinguishes entries the exception itself cannot. Null for a caught
    /// exception, whose type and HRESULT already identify it; set by a caller
    /// synthesising its entries, where they would otherwise be identical.
    /// </param>
    internal void Record(Exception ex, string? cause = null)
    {
        // Add before the budget test either way, so the causes seen during the
        // budgeted phase are already known by the time the filter engages and a
        // repeat of one of them does not buy a second entry.
        var novelCause = _seenCauses.Add((ex.GetType(), ex.HResult, cause));

        if (_logged < Budget || novelCause)
        {
            _write(ex);
            _logged++;
            return;
        }

        _suppressed++;
    }

    /// <summary>
    /// Writes the one closing entry naming what the budget left out, or nothing
    /// at all when it left out nothing (the overwhelmingly common case: no
    /// failures, or few enough that every one was logged).
    /// </summary>
    internal void WriteClosingEntry()
    {
        if (_suppressed == 0) return;

        var causes = _seenCauses.Count == 1 ? "cause" : "causes";
        _write(new InvalidOperationException(
            $"{operationKind}: {_suppressed} further failures were not logged individually. " +
            $"{_logged} were logged in full, covering all {_seenCauses.Count} distinct {causes} " +
            $"this run produced. {detailTrail}"));
    }
}
