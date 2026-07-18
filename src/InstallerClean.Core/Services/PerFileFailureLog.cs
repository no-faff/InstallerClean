using InstallerClean.Helpers;

namespace InstallerClean.Services;

/// <summary>
/// Crash-log budget for one Move or Delete batch. A per-file failure is caught,
/// categorised and shown to the user, and before this existed the exception
/// itself was dropped on the floor: on 2026-07-18 two moves failed as
/// IOException and left no trace anywhere, which is the opposite of the rule
/// that a catch block logs the full exception.
///
/// Logging every one unconditionally trades that for a worse failure. crash.log
/// rotates at 512 KB with a single archive, and an every-file failure mode is
/// reachable: the destination write probe writes a zero-byte file, so a volume
/// with room for that and not for the batch fails on every remaining file. A few
/// hundred stack traces then evict the whole crash history behind near-identical
/// copies of one already-recorded cause.
///
/// So the budget spends its entries on causes rather than on files. The first
/// <see cref="Budget"/> failures are logged in full, which covers every
/// realistic partial failure outright. Past that, a failure is still logged in
/// full if its exception type and HRESULT have not been seen in this batch, so a
/// novel cause arriving at file 500 is never lost to a storm of a known one;
/// identical repeats only increment a counter. <see cref="WriteClosingEntry"/>
/// then records what was left out, so the log never implies it holds everything.
/// The file-to-category map those suppressed entries would carry is not lost: it
/// is on the completion overlay and in the result log.
///
/// One instance per batch, used from the single worker thread that runs the
/// batch loop, so it needs no synchronisation of its own.
///
/// <paramref name="write"/> is the sink, defaulting to crash.log. Tests pass
/// their own so pinning the budget does not append two dozen entries to the
/// real log on whatever machine runs the suite.
/// </summary>
internal sealed class PerFileFailureLog(string operationKind, Action<Exception>? write = null)
{
    private readonly Action<Exception> _write = write ?? (ex => CrashLog.TryWrite(ex));

    /// <summary>
    /// Full entries logged before the type-and-HRESULT filter takes over.
    /// Twenty is above any per-file failure count a user would sit and read
    /// through, so an ordinary partial failure is logged file by file and the
    /// filter only ever engages on a storm.
    /// </summary>
    private const int Budget = 20;

    private readonly HashSet<(Type, int)> _seenCauses = [];
    private int _logged;
    private int _suppressed;

    /// <summary>
    /// Logs <paramref name="ex"/> in full if it is within the batch budget or
    /// carries a cause not yet seen in this batch; otherwise counts it as
    /// suppressed for <see cref="WriteClosingEntry"/>.
    /// </summary>
    internal void Record(Exception ex)
    {
        // Add before the budget test either way, so the causes seen during the
        // budgeted phase are already known by the time the filter engages and a
        // repeat of one of them does not buy a second entry.
        var novelCause = _seenCauses.Add((ex.GetType(), ex.HResult));

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

        _write(new InvalidOperationException(
            $"{operationKind}: {_suppressed} further per-file failures were not logged individually. " +
            $"{_logged} were logged in full, covering all {_seenCauses.Count} distinct " +
            "exception-type and HRESULT combinations this batch produced. " +
            "The per-file list is on the completion screen and in the result log."));
    }
}
