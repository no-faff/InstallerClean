namespace InstallerClean.Helpers;

/// <summary>
/// Walks a Windows Installer list again from its first index where a row that did not
/// read stopped the walk.
///
/// A walk asks for one index after another until Windows Installer answers
/// ERROR_NO_MORE_ITEMS. One call of a walk can answer an error while Windows Installer
/// rolls a transaction back, where a walk made a moment later reads the list to its
/// end. So a walk that a row it could not read stopped is made again from
/// index 0, up to <see cref="Attempts"/> walks in all with a pause before each restart,
/// and the first walk that reaches the end is the answer. It is the list as a walk that
/// met no error reads it. Where none reaches the end, the caller is handed the first
/// walk and holds or refuses on it, as it does where a list is walked once.
///
/// A walk that spends its index budget on rows that all read is not made again: the
/// next would read the same rows and spend it again.
///
/// The reads of one property of one installation, patch or source list are made again the
/// same way where an answer does not stand (<see cref="KeyedRead"/>).
///
/// One of these serves one pass over the lists, one part of a pass that keeps a budget of its
/// own, or one check under a Move or Delete's lease, on the thread that makes it.
/// </summary>
/// <param name="pause">The wait before a restart.</param>
/// <param name="keyedPauseLimit">
/// The longest the keyed restarts pause in all, <see cref="KeyedPauseLimit"/> where none is given.
/// </param>
internal sealed class WalkRestarts(Action<TimeSpan> pause, TimeSpan? keyedPauseLimit = null)
{
    /// <summary>
    /// The pause the test suite has every scan make for its whole run, so that no test
    /// waits out a restart. Null everywhere else. Nothing in the app sets it.
    /// </summary>
    internal static Action<TimeSpan>? PauseForTests { get; set; }

    /// <summary>
    /// The restarts for one scan. They pause with <paramref name="pause"/> where one is
    /// handed in, and otherwise on <paramref name="cancellationToken"/>, so a cancel of
    /// the scan ends the wait with OperationCanceledException
    /// (<see cref="RegistryNames.Pause"/>).
    /// </summary>
    internal static WalkRestarts For(
        CancellationToken cancellationToken, Action<TimeSpan>? pause = null, TimeSpan? keyedPauseLimit = null) =>
        new(pause ?? PauseForTests ?? RegistryNames.Pause(cancellationToken), keyedPauseLimit);

    /// <summary>
    /// Restarts that make no property read again (<see cref="KeyedRead"/>; never hand them to a
    /// walk, which they still restart), for a read whose answer is
    /// compared with what was read before it, so that one read made again after an error would be
    /// compared with a record read while it may have been changing. Each ask makes a new one.
    /// </summary>
    internal static WalkRestarts ReadsOnce => new(_ => { }, TimeSpan.Zero);

    /// <summary>How many times one list is walked, in all, before the first walk is taken as it stands.</summary>
    internal const int Attempts = 3;

    // The pause before the second walk and before the third. The second lets a run of
    // writes to the records finish before they are read again.
    private static readonly TimeSpan[] PauseBefore = [Durations.FromMilliseconds(50), Durations.FromMilliseconds(250)];

    /// <summary>
    /// The longest the restarts made for one program or patch at a time pause, in all, where
    /// no other limit is given: the lists walked for one (<see cref="KeyedWalk"/>) and the
    /// properties read for one (<see cref="KeyedRead"/>) share it.
    /// </summary>
    internal static readonly TimeSpan KeyedPauseLimit = Durations.FromSeconds(2);

    private readonly TimeSpan _keyedPauseLimit = keyedPauseLimit ?? KeyedPauseLimit;

    private TimeSpan _keyedPaused;

    /// <summary>
    /// A list the scan walks once: <paramref name="walk"/> is one walk from index 0, and
    /// <paramref name="stands"/> says whether its answer is taken as it stands, which is
    /// false for a walk that a row it could not read stopped.
    /// </summary>
    internal T Walk<T>(Func<T> walk, Func<T, bool> stands) => Run(walk, stands, keyed: false);

    /// <summary>
    /// A list the scan walks for one program or patch at a time, or one ask about one.
    /// Where Windows Installer
    /// refuses such a list for one program it can refuse it for every program recorded
    /// under the same account, so these walks share the keyed pause limit: a
    /// restart whose pause would take them past it is made at once, without the pause.
    /// </summary>
    internal T KeyedWalk<T>(Func<T> walk, Func<T, bool> stands) => Run(walk, stands, keyed: true);

    // The keys (KeyedRead) whose reads have answered with nothing that stands on every
    // attempt, in this pass.
    private readonly HashSet<string> _readsSpent = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// One read of one property of the installation, patch or source list that
    /// <paramref name="key"/> names, made again where its answer does not stand, with the
    /// pauses keyed walks make and sharing the keyed pause limit with them.
    /// <paramref name="stands"/> says which answers stand on the first read, and where none
    /// stands, the first read is the answer.
    ///
    /// A READ MADE AFTER ONE THAT DID NOT STAND IS TAKEN ONLY WHERE THE READ AFTER IT, MADE
    /// AFTER THE LONGER PAUSE, ANSWERS THE SAME (<paramref name="agrees"/>), and only for an
    /// answer <paramref name="standsAgain"/> accepts. A first read that did not stand can be
    /// Windows Installer answering while a transaction rewrites the record, and a read a
    /// moment later can then show what that transaction wrote and is about to undo. Where
    /// the two reads differ, or the pause between them cannot be made within
    /// the keyed pause limit, the first read is the answer.
    ///
    /// A KEY WHOSE READS MADE AGAIN ANSWERED WITH NOTHING <paramref name="stands"/> ACCEPTS IS
    /// NOT READ AGAIN FOR THE REST OF THE PASS, and nor is any key whose
    /// <paramref name="within"/> key is spent: a read for either is answered by its first
    /// read alone. The <paramref name="within"/> key is spent too where the first read is one
    /// <paramref name="spendsWithin"/> names. An installation whose registration Windows
    /// Installer cannot read answers every property of its own and of its patches that way,
    /// so without this one such installation would be read again for each of them and spend
    /// the pauses the other reads share.
    ///
    /// The keys are made only for a read whose first answer does not stand.
    /// </summary>
    internal T KeyedRead<T>(
        Func<string> key,
        Func<T> read,
        Func<T, bool> stands,
        Func<T, bool> standsAgain,
        Func<T, T, bool> agrees,
        Func<string>? within = null,
        Func<T, bool>? spendsWithin = null)
    {
        var first = read();
        if (stands(first)) return first;

        var spent = key();
        var withinKey = within?.Invoke();
        if (_readsSpent.Contains(spent) || (withinKey is not null && _readsSpent.Contains(withinKey))) return first;

        var answered = false;
        for (var attempt = 1; attempt < Attempts; attempt++)
        {
            // A read made again is taken only once the read after the longer pause agrees with
            // it, so where the limit cannot pay for both pauses nothing more is read.
            if (_keyedPaused + PauseBefore[attempt - 1] + PauseBefore[^1] > _keyedPauseLimit) return first;

            Wait(attempt - 1, keyed: true);
            var again = read();
            answered |= stands(again);
            if (!standsAgain(again)) continue;

            // Two values were read, so the record answers, and a disagreement leaves the first
            // read standing and spends nothing.
            Wait(PauseBefore.Length - 1, keyed: true);
            var confirmed = read();
            return standsAgain(confirmed) && agrees(again, confirmed) ? confirmed : first;
        }

        // A read made again that answered, with an absence, shows the record answers, so the
        // key is not spent.
        if (answered) return first;

        _readsSpent.Add(spent);
        if (withinKey is not null && spendsWithin?.Invoke(first) == true) _readsSpent.Add(withinKey);
        return first;
    }

    private T Run<T>(Func<T> walk, Func<T, bool> stands, bool keyed)
    {
        var first = walk();
        if (stands(first)) return first;

        for (var attempt = 1; attempt < Attempts; attempt++)
        {
            Wait(attempt - 1, keyed);
            var again = walk();
            if (stands(again)) return again;
        }

        return first;
    }

    // The pause PauseBefore[index] names, cut to nothing where keyed restarts would pass
    // the limit. False where it was cut.
    private bool Wait(int index, bool keyed)
    {
        var wait = PauseBefore[index];
        if (keyed && _keyedPaused + wait > _keyedPauseLimit) return false;
        if (keyed) _keyedPaused += wait;

        pause(wait);
        return true;
    }
}
