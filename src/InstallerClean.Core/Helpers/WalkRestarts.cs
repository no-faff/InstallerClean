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
/// One of these serves one pass over the lists, on the thread that makes it.
/// </summary>
/// <param name="pause">The wait before a restart.</param>
internal sealed class WalkRestarts(Action<TimeSpan> pause)
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
    internal static WalkRestarts For(CancellationToken cancellationToken, Action<TimeSpan>? pause = null) =>
        new(pause ?? PauseForTests ?? RegistryNames.Pause(cancellationToken));

    /// <summary>How many times one list is walked, in all, before the first walk is taken as it stands.</summary>
    internal const int Attempts = 3;

    // The pause before the second walk and before the third. The second lets a run of
    // writes to the records finish before they are read again.
    private static readonly TimeSpan[] PauseBefore = [Durations.FromMilliseconds(50), Durations.FromMilliseconds(250)];

    /// <summary>
    /// The longest the walks sharing one of these pause, in all, for the lists walked once
    /// for each program or patch (<see cref="KeyedWalk"/>).
    /// </summary>
    internal static readonly TimeSpan KeyedPauseLimit = Durations.FromSeconds(2);

    private TimeSpan _keyedPaused;

    /// <summary>
    /// A list the scan walks once: <paramref name="walk"/> is one walk from index 0, and
    /// <paramref name="stands"/> says whether its answer is taken as it stands, which is
    /// false for a walk that a row it could not read stopped.
    /// </summary>
    internal T Walk<T>(Func<T> walk, Func<T, bool> stands) => Run(walk, stands, keyed: false);

    /// <summary>
    /// A list the scan walks once for each program or patch. Where Windows Installer
    /// refuses such a list for one program it can refuse it for every program recorded
    /// under the same account, so these walks share <see cref="KeyedPauseLimit"/>: a
    /// restart whose pause would take them past it is made at once, without the pause.
    /// </summary>
    internal T KeyedWalk<T>(Func<T> walk, Func<T, bool> stands) => Run(walk, stands, keyed: true);

    private T Run<T>(Func<T> walk, Func<T, bool> stands, bool keyed)
    {
        var first = walk();
        if (stands(first)) return first;

        for (var attempt = 1; attempt < Attempts; attempt++)
        {

            var wait = PauseBefore[attempt - 1];
            if (keyed)
            {
                if (_keyedPaused + wait > KeyedPauseLimit) wait = TimeSpan.Zero;
                _keyedPaused += wait;
            }

            if (wait > TimeSpan.Zero) pause(wait);
            var again = walk();
            if (stands(again)) return again;
        }

        return first;
    }
}
