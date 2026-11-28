using System.ComponentModel;
using System.Diagnostics;
using System.IO.Abstractions;
using InstallerClean.Compatibility;
using InstallerClean.Helpers;
using InstallerClean.Interop.Native;
using InstallerClean.Models;

namespace InstallerClean.Services;

/// <summary>What a look found (<see cref="BatchRun.Look"/>).</summary>
internal enum LookResult
{
    /// <summary>
    /// The read could not be made, and the batch has been ended on it
    /// (<see cref="BatchRun.Ended"/>). First member so that it is what the type's own zero
    /// carries.
    /// </summary>
    Failed,

    /// <summary>The watch reads unchanged and Windows Installer is not busy.</summary>
    Clean,

    /// <summary>The records have changed, the watch is not established, or Windows Installer is busy.</summary>
    NotClean,
}

/// <summary>
/// One run of a Move or a Delete over one batch: what both services keep while they work
/// through the files, and the steps both make the same way. A service makes one for each
/// call and disposes of it when the call ends, so nothing of a batch outlives it; the
/// services themselves are singletons.
///
/// THE LOOK is a read of the watch armed before the batch's last full check and a read of
/// whether Windows Installer is busy. A service looks before it begins on a file and
/// again once the file is out of its place, and acts only on two clean looks.
///
/// THE SETTLE is what follows a look that is not clean: the watch is given up, the full
/// check is made again over every file not yet done (<see cref="IBatchCheck"/>, which
/// waits for Windows Installer first), the files that check drops leave the batch as
/// held back, and the run goes on under the new check's watch. A file in hand is put back
/// before a settle begins, since a full check reads each file in its place.
/// </summary>
internal sealed class BatchRun : IDisposable
{
    private readonly IBatchCheck _batchCheck;
    private readonly IPendingRebootService _signals;
    private readonly IFileSystem _fs;
    private readonly InstallerCacheRoot _cacheRoot;
    private readonly string _operation;
    private readonly IProgress<OperationProgress>? _progress;
    private readonly CancellationToken _token;

    private readonly HashSet<string> _dropped = new(StringComparer.OrdinalIgnoreCase);
    private readonly HashSet<string> _done = new(StringComparer.OrdinalIgnoreCase);
    private readonly HashSet<string> _reported = new(StringComparer.OrdinalIgnoreCase);
    private readonly List<string> _heldBack = [];
    private readonly List<SourceRootGivenUp> _sourceRootsGivenUp = [];
    private readonly List<string> _inUse = [];
    private StandingCheck _check;
    private int _position;
    private long _putBackAt;
    private long _refusedAsInUseAt;

    /// <summary>
    /// How long a settle waits after a put-back before the check is made again.
    ///
    /// A RENAME MOVES A FILE'S CHANGE TIME, and the full check passes a file no record
    /// names only where each of its times is before the check's own clock, which moves in
    /// the system clock's steps (<see cref="FileStampClock"/>, <see cref="CachedFileAge"/>).
    /// A check begun in the step the file was put back in would read the file as changed
    /// while the check ran and drop it from the batch. The longest such step is under 16
    /// milliseconds, and this is three of them.
    /// </summary>
    internal static readonly TimeSpan PauseAfterAPutBack = Durations.FromMilliseconds(50);

    /// <summary>
    /// The least time between a file being refused as in use and its second try. In a
    /// long batch the files that follow it take longer than this and nothing is waited
    /// for. In a short one the second try would otherwise come at once, while whatever
    /// had the file open for a moment still has it.
    /// </summary>
    internal static readonly TimeSpan PauseBeforeTheSecondTry = Durations.FromSeconds(1);

    /// <summary>What a run pauses through, for a test that must not wait or that reads the pauses. Null is the app's own delay.</summary>
    internal static Func<TimeSpan, CancellationToken, Task>? PauseForTests { get; set; }

    /// <param name="check">The standing check the host made, which this run owns from here on.</param>
    /// <param name="operation">"Move" or "Delete", for the crash log's entries.</param>
    internal BatchRun(
        StandingCheck check, IBatchCheck batchCheck, IPendingRebootService signals, IFileSystem fileSystem,
        InstallerCacheRoot cacheRoot, string operation, IProgress<OperationProgress>? progress,
        CancellationToken token)
    {
        _check = check;
        _batchCheck = batchCheck;
        _signals = signals;
        _fs = fileSystem;
        _cacheRoot = cacheRoot;
        _operation = operation;
        _progress = progress;
        _token = token;
        Files = check.Files;
        FailureLog = new PerItemFailureLog(operation, "The per-file list is on the completion screen.");
    }

    /// <summary>The files the batch began with, in order. A settle can drop some (<see cref="InBatch"/>).</summary>
    internal IReadOnlyList<string> Files { get; }

    /// <summary>Each file the run could not act on, with its cause.</summary>
    internal List<FileOperationError> Errors { get; } = [];

    internal PerItemFailureLog FailureLog { get; }

    /// <summary>The files later checks dropped from the batch, none of them touched.</summary>
    internal IReadOnlyList<string> HeldBack => _heldBack;

    /// <summary>How many of <see cref="HeldBack"/> fell to each cause.</summary>
    internal HeldBackReasons HeldBackReasons { get; private set; }

    /// <summary>The drives and shares later checks that stood stopped waiting for.</summary>
    internal IReadOnlyList<SourceRootGivenUp> SourceRootsGivenUp => _sourceRootsGivenUp;

    /// <summary>The waits on a source folder those checks made.</summary>
    internal int SourceWaitCount { get; private set; }

    /// <summary>What the later checks did, for the opt-in report.</summary>
    internal BatchCheckCounts CheckCounts { get; private set; }

    /// <summary>Why the batch ended before its last file. Null while it has not.</summary>
    internal BatchEnd? Ended { get; private set; }

    /// <summary>The files refused as in use on their first try, for the one more try after the last file.</summary>
    internal IReadOnlyList<string> InUse => _inUse;

    /// <summary>Whether <paramref name="path"/> is still in the batch: no settle has dropped it.</summary>
    internal bool InBatch(string path) => !_dropped.Contains(path);

    /// <summary>Ends the batch for a reason a service finds itself.</summary>
    internal void End(BatchEnd end) => Ended ??= end;

    /// <summary>
    /// Marks <paramref name="path"/> as finished with, whether it was acted on or filed as
    /// an error, so no later check is asked about it.
    /// </summary>
    internal void Done(string path) => _done.Add(path);

    /// <summary>
    /// Reports <paramref name="path"/> as the file the run has reached, once however often
    /// a settle starts the file again. The count is of the files reached and the total of
    /// the files still in the batch.
    /// </summary>
    internal void ReportReached(string path)
    {
        if (!_reported.Add(path)) return;
        _position++;
        _progress?.Report(new OperationProgress(_position, Files.Count - _dropped.Count, PathText.GetFileName(path)));
    }

    /// <summary>
    /// The checks made on a file in its place before the first look: that it is there, is
    /// not a link, and resolves to a file directly in the Installer folder. False where
    /// one refuses it, with the error filed and the file finished with.
    ///
    /// THE LAST TWO READ THE REAL FILESYSTEM whatever filesystem was injected, so a
    /// substitute cannot answer for them, and they run on the file in its place and never
    /// on a file in hand: the containment guard passes only a file that stands directly in
    /// the Installer folder.
    ///
    /// A LINK IS REFUSED FIRST, so a symlink is reported as one: acting on a link acts on
    /// the link or reads through it to a file outside the folder. An attribute read that
    /// fails refuses the file as an unknown error, since it has not shown a link.
    ///
    /// THE CONTAINMENT GUARD IS TESTED INVERTED, so only an answer that positively said
    /// Safe goes on to the take, and a member added to
    /// <see cref="CandidateGuard.RemovalSafety"/> later is a refusal. The default arm files
    /// the unknown error Unproven files and does not borrow its sentence: a member nobody
    /// has written yet has not been shown to be a path that could not be resolved.
    /// </summary>
    internal bool PassesSourceChecks(string path)
    {
        if (!_fs.File.Exists(path)) return Refuse(new MissingSourceFile(path));

        var reparse = StorageHelpers.CheckReparsePoint(path, out var reparseError);
        if (reparse == StorageHelpers.ReparseCheck.Yes) return Refuse(new SourceIsReparsePoint(path));
        if (reparse == StorageHelpers.ReparseCheck.Unreadable)
        {
            FailureLog.Record(reparseError!);
            return Refuse(new UnknownError(path));
        }

        var safety = CandidateGuard.CheckSafeToRemove(path, _cacheRoot);
        if (safety == CandidateGuard.RemovalSafety.Safe) return true;
        switch (safety)
        {
            case CandidateGuard.RemovalSafety.Refused:
                return Refuse(new CandidateOutsideCache(path));
            case CandidateGuard.RemovalSafety.Unproven:
                FailureLog.Record(new InvalidOperationException(
                    $"{_operation} refused: {path} could not be resolved, so it could not be shown to be inside the Installer cache."));
                return Refuse(new UnknownError(path));
            default:
                FailureLog.Record(new InvalidOperationException(
                    $"{_operation} refused: the containment guard answered {safety} for {path}, which this service has no handling for."));
                return Refuse(new UnknownError(path));
        }
    }

    /// <summary>Files <paramref name="error"/> and finishes with its file. Always false, for a caller to return.</summary>
    internal bool Refuse(FileOperationError error)
    {
        Errors.Add(error);
        Done(error.FilePath);
        return false;
    }

    /// <summary>
    /// Files what Windows refused at <paramref name="step"/> for <paramref name="path"/>
    /// by its error: a sharing or lock violation as in use, an access refusal as that, a
    /// file or path not found as no longer there, and anything else as an IO failure. The
    /// error and the step go to the crash log, which is the only place the code is kept.
    /// </summary>
    internal void RefuseByWindowsError(string path, int windowsError, string step)
    {
        FailureLog.Record(
            new IOException($"{_operation}: {step} was refused for {path}.", new Win32Exception(windowsError)),
            cause: $"{step}:{windowsError}");
        Refuse(windowsError switch
        {
            Kernel32.ERROR_SHARING_VIOLATION or Kernel32.ERROR_LOCK_VIOLATION => new FileInUse(path),
            Kernel32.ERROR_ACCESS_DENIED => new AccessDenied(path),
            Kernel32.ERROR_FILE_NOT_FOUND or Kernel32.ERROR_PATH_NOT_FOUND => new MissingSourceFile(path),
            _ => new IOFailure(path),
        });
    }

    /// <summary>
    /// A file something else has open. On its first try it is remembered for one more try
    /// after the last file: a scanner's open of a file lasts a moment, and a file Windows
    /// Installer has open stays refused and stays in its place. On that second try it is
    /// filed as in use.
    /// </summary>
    internal void RefuseAsInUse(string path, bool lastTry)
    {
        if (lastTry)
        {
            Refuse(new FileInUse(path));
            return;
        }

        _inUse.Add(path);
        _refusedAsInUseAt = Stopwatch.GetTimestamp();
    }

    /// <summary>
    /// Waits, where any file was refused as in use, until
    /// <see cref="PauseBeforeTheSecondTry"/> has passed since the last such refusal. A
    /// service calls it between the batch's files and the second try at those files. A
    /// cancel ends the wait as it ends the batch.
    /// </summary>
    internal Task BeforeTheSecondTryAsync() =>
        _inUse.Count == 0 ? Task.CompletedTask : PauseSinceAsync(_refusedAsInUseAt, PauseBeforeTheSecondTry);

    /// <summary>Waits out what is left of <paramref name="length"/> since the moment <paramref name="since"/>, a timestamp of <see cref="Stopwatch"/>. Nought is no moment, and nothing is waited for.</summary>
    private Task PauseSinceAsync(long since, TimeSpan length)
    {
        if (since == 0) return Task.CompletedTask;
        var passed = Durations.FromSeconds((Stopwatch.GetTimestamp() - since) / (double)Stopwatch.Frequency);
        return passed >= length
            ? Task.CompletedTask
            : (PauseForTests ?? FrameworkTask.Delay)(length - passed, _token);
    }

    /// <summary>
    /// Files as in use every file that was remembered for one more try and never had it,
    /// because the batch was cancelled, was stopped or ended first. Each was reached and
    /// refused, so it is an error of the batch and not a file the batch never came to. A
    /// host works out what a stopped batch did from the files it reached in order, which
    /// are the ones acted on and the ones filed.
    /// </summary>
    internal void FileWhatIsStillInUse()
    {
        foreach (var path in _inUse)
            if (!_done.Contains(path) && !_dropped.Contains(path)) Refuse(new FileInUse(path));
    }

    /// <summary>
    /// Files what Windows refused at <paramref name="step"/>, a step made on the Move's
    /// destination and not on the file: an access refusal as that, and anything else as an
    /// IO failure. The file itself is where it was.
    /// </summary>
    internal void RefuseForDestination(string path, int windowsError, string step)
    {
        FailureLog.Record(
            new IOException($"{_operation}: {step} was refused for {path}.", new Win32Exception(windowsError)),
            cause: $"{step}:{windowsError}");
        Refuse(windowsError == Kernel32.ERROR_ACCESS_DENIED ? new AccessDenied(path) : new IOFailure(path));
    }

    /// <summary>
    /// Opens <paramref name="path"/> to the app alone, where it stands. Null where it
    /// could not be held, with the file filed or remembered as in use.
    /// </summary>
    internal IHeldFile? Hold(IInHandBatch hand, string path, bool lastTry)
    {
        var hold = hand.Hold(path);
        return Opened(hold.Outcome, hold.File, hold.WindowsError, path, lastTry) ? hold.File : null;
    }

    /// <summary>
    /// Opens <paramref name="path"/> to be read for a copy, where it stands. Null where
    /// it could not be opened, with the file filed or remembered as in use.
    /// </summary>
    internal ICopySource? OpenToCopy(IInHandBatch hand, string path, bool lastTry)
    {
        var open = hand.OpenToCopy(path);
        return Opened(open.Outcome, open.Source, open.WindowsError, path, lastTry) ? open.Source : null;
    }

    /// <summary>
    /// Whether an open of <paramref name="path"/> gave something to work with. Where it
    /// did not, the file is filed by what the open found, or remembered as in use.
    /// </summary>
    private bool Opened(HoldOutcome outcome, object? opened, int windowsError, string path, bool lastTry)
    {
        switch (outcome)
        {
            case HoldOutcome.Held when opened is not null:
                return true;
            case HoldOutcome.InUse:
                RefuseAsInUse(path, lastTry);
                return false;
            case HoldOutcome.Gone:
                return Refuse(new MissingSourceFile(path));
            case HoldOutcome.NotAPlainFile:
                // A link or a folder now stands where the checks made on the file in its
                // place found a file. Which of the two is not established, so no error
                // naming one is filed.
                FailureLog.Record(new InvalidOperationException(
                    $"{_operation} refused: {path} was no longer a plain file when it was opened."));
                return Refuse(new UnknownError(path));
            default:
                RefuseByWindowsError(path, windowsError, "the open of the file");
                return false;
        }
    }

    /// <summary>
    /// Renames a held file out of its place into the batch's folder. False where Windows
    /// refused, with the file still in its place, the handle closed and the file filed, or
    /// remembered as in use where the refusal was a sharing or lock violation.
    /// </summary>
    internal bool TakeOut(IHeldFile held, bool lastTry)
    {
        if (held.TakeOut(out var code)) return true;
        held.Dispose();
        if (code is Kernel32.ERROR_SHARING_VIOLATION or Kernel32.ERROR_LOCK_VIOLATION)
            RefuseAsInUse(held.RecordedPath, lastTry);
        else
            RefuseByWindowsError(held.RecordedPath, code, "the rename into the batch's folder");
        return false;
    }

    /// <summary>
    /// Puts a held file back where it was recorded, if it is out of its place, and closes
    /// its handle. False where the file would not go back: it is then left whole in the
    /// batch's folder, filed with that folder's path so that a person can find it, and
    /// finished with.
    /// </summary>
    internal bool PutBack(IHeldFile held, IInHandBatch hand)
    {
        var wasOut = held.IsOut;
        var code = 0;
        var back = !wasOut || held.PutBack(out code);
        if (!back)
        {
            FailureLog.Record(
                new IOException(
                    $"{_operation}: {held.RecordedPath} could not be renamed back to its place and is in {hand.Folder}.",
                    new Win32Exception(code)),
                cause: $"put back:{code}");
            Refuse(new FileLeftInHand(held.RecordedPath, hand.Folder));
        }
        else if (wasOut)
        {
            _putBackAt = Stopwatch.GetTimestamp();
        }
        held.Dispose();
        return back;
    }

    /// <summary>
    /// Reads the watch and, where it is unchanged, whether Windows Installer is busy. It
    /// makes those two reads and no other: a look is made twice for every file.
    ///
    /// A read that throws ends the batch (<see cref="BatchEndReason.CheckFailed"/>) and
    /// answers <see cref="LookResult.Failed"/>, so that a caller with a file in hand puts it back
    /// on the way out as it does after any look that is not clean.
    /// </summary>
    internal LookResult Look()
    {
        try
        {
            if (_check.Watch.Read() != RecordsWatchReading.Unchanged) return LookResult.NotClean;
            return _signals.CheckBusy().IsBlocked ? LookResult.NotClean : LookResult.Clean;
        }
        catch (Exception ex) when (!BatchStop.EndsBatch(ex, _token))
        {
            End(new BatchEnd(BatchEndReason.CheckFailed, Failure: ex));
            return LookResult.Failed;
        }
    }

    /// <summary>
    /// Settles after <paramref name="look"/>, which was not clean. True where the run goes
    /// on, under a new standing check; the caller then asks <see cref="InBatch"/> before it
    /// goes back to a file. False where the batch has ended (<see cref="Ended"/>).
    ///
    /// CALL IT WITH NO FILE IN HAND. The full check reads each file in its place, and the
    /// wait before it can last as long as an installation.
    ///
    /// ONLY A CHECK THAT STANDS DROPS FILES. One that did not stand is thrown away whole
    /// inside <see cref="IBatchCheck"/> and made again over the same files.
    /// </summary>
    internal async Task<bool> SettleAsync(LookResult look)
    {
        if (look == LookResult.Failed || Ended is not null) return false;

        // Nothing is left to check where the file the look was made for has since been
        // finished with and was the last.
        var notYetDone = Files.Where(f => !_done.Contains(f) && !_dropped.Contains(f)).ToList();
        if (notYetDone.Count == 0) return true;
        _check.Dispose();

        // See PauseAfterAPutBack. A cancel during it ends the batch as any cancel does.
        await PauseSinceAsync(_putBackAt, PauseAfterAPutBack).ConfigureAwait(false);

        BatchCheckResult result;
        try
        {
            result = await _batchCheck.RunAsync(notYetDone, _check.WaitBound, new CheckReports(this), _token)
                .ConfigureAwait(false);
        }
        catch (Exception ex) when (!BatchStop.EndsBatch(ex, _token))
        {
            End(new BatchEnd(BatchEndReason.CheckFailed, Failure: ex));
            return false;
        }

        CheckCounts += result.Counts;
        if (result.Stood is not { } stood)
        {
            // One of the two is always set (BatchCheckResult).
            End(result.Ended!);
            return false;
        }

        _check = stood;
        foreach (var dropped in stood.Result.Dropped)
        {
            if (!_dropped.Add(dropped)) continue;
            _heldBack.Add(dropped);
            // A file already reported gives its place in the count to the next one.
            if (_reported.Contains(dropped)) _position--;
        }
        HeldBackReasons += stood.Result.Reasons;
        _sourceRootsGivenUp.AddRange(stood.Result.SourceRootsGivenUp);
        SourceWaitCount += stood.Result.SourceWaitCount;
        return true;
    }

    /// <summary>Releases the watch of the standing check the run holds.</summary>
    public void Dispose() => _check.Dispose();

    /// <summary>
    /// Passes a later check's reports to the batch's own progress, with the count as it
    /// stands, on the thread that makes them.
    /// </summary>
    private sealed class CheckReports(BatchRun run) : IProgress<BatchCheckProgress>
    {
        public void Report(BatchCheckProgress value) =>
            run._progress?.Report(new OperationProgress(
                run._position, run.Files.Count - run._dropped.Count, string.Empty, value));
    }
}
