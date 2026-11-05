using System.Diagnostics;
using System.Runtime.InteropServices;
using InstallerClean.Helpers;
using InstallerClean.Interop.Native;
using Microsoft.Win32.SafeHandles;

namespace InstallerClean.Services;

/// <summary>
/// A newer version this copy has started: the wait for it to say its own code is running,
/// and the ending of one that has not. Every wait here dispatches nothing, so on a window's
/// thread no window code runs during it.
/// </summary>
internal static class NewerVersionProcess
{
    /// <summary>
    /// Starts <paramref name="stagedExe"/>, the newer version this copy downloaded, to put
    /// itself at <paramref name="exePath"/> once this process has ended
    /// (<see cref="HandOverContract.ReplaceArguments"/>), and waits up to
    /// <see cref="HandOverContract.StartBound"/> for it to say it will. Answers as
    /// <see cref="StartAndAwait"/> does. A newer version answered as
    /// <see cref="NewerVersionAnswer.StillRunning"/> sets the event before it moves anything
    /// onto the path, and the event is gone once this returns, so it puts nothing at the
    /// path unless it had already opened the event; crash.log says so. The waits between
    /// tries at the start, and every other wait here, dispatch nothing, so on a window's
    /// thread no window code runs during them. Never throws.
    /// </summary>
    public static NewerVersionAnswer StartToReplace(string stagedExe, string exePath) =>
        StartToReplace(stagedExe, exePath, [], HandOverContract.StartBound);

    /// <summary>
    /// Test seam: <see cref="StartToReplace(string, string)"/>, with <paramref name="before"/>
    /// ahead of the replace arguments and <paramref name="bound"/> for the wait.
    /// </summary>
    internal static NewerVersionAnswer StartToReplace(
        string stagedExe, string exePath, IReadOnlyList<string> before, TimeSpan bound)
    {
        var answer = StartAndAwait(
            stagedExe, id => [.. before, .. HandOverContract.ReplaceArguments(exePath, Environment.ProcessId, id)],
            bound, interval => Kernel32.Sleep((uint)interval.TotalMilliseconds));
        if (answer == NewerVersionAnswer.StillRunning)
            CrashLog.TryWrite(new UpdateFailedException(UpdateFailureStage.Install,
                "The newer version, which did not say it was ready and which Windows would not end, is still running. The event it says so with is gone, so it puts nothing at the path unless it had already opened that event."));
        return answer;
    }

    /// <summary>
    /// Starts <paramref name="file"/> with the arguments <paramref name="argumentsFor"/> gives
    /// for a new started event's id, working in the folder the file is in, and waits up to
    /// <paramref name="bound"/> for it to set that event. One that does not is ended and
    /// waited for (<see cref="EndNewerVersion(SafeProcessHandle)"/>). Answers
    /// <see cref="NewerVersionAnswer.Started"/>; <see cref="NewerVersionAnswer.NotStarted"/>
    /// where it could not be started, did not set the event and has been ended, or no event
    /// could be made to see it by; or <see cref="NewerVersionAnswer.StillRunning"/> where it
    /// did not set the event and Windows would not end it. A start that fails is tried again
    /// through <see cref="ProcessStartRetry"/>, with <paramref name="sleep"/> between tries.
    /// What failed is written to crash.log. Never throws.
    /// </summary>
    internal static NewerVersionAnswer StartAndAwait(
        string file, Func<Guid, IEnumerable<string>> argumentsFor, TimeSpan bound, Action<TimeSpan> sleep)
    {
        try
        {
            var id = Guids.NewGuid();
            using var started = HandOverContract.CreateStartedEvent(id);
            if (started is null) return NewerVersionAnswer.NotStarted;

            var info = new ProcessStartInfo(file)
            {
                UseShellExecute = false,
                WorkingDirectory = Path.GetDirectoryName(file) ?? string.Empty,
            };
            foreach (var argument in argumentsFor(id)) info.ArgumentList.Add(argument);
            Process newer;
            try
            {
                newer = ProcessStartRetry.Start(info, _ => true, sleep);
            }
            catch (Exception ex)
            {
                CrashLog.TryWrite(ex);
                return NewerVersionAnswer.NotStarted;
            }
            using (newer) return Await(newer.SafeHandle, started, bound, out _);
        }
        catch (Exception ex)
        {
            CrashLog.TryWrite(ex);
            return NewerVersionAnswer.NotStarted;
        }
    }

    /// <summary>
    /// Starts <paramref name="file"/> as <see cref="DetachedProcess"/> starts a program,
    /// working in <paramref name="workingDirectory"/>, with the arguments
    /// <paramref name="argumentsFor"/> gives for a new started event's id, and waits up to
    /// <paramref name="bound"/> for it to set that event. Answers as
    /// <see cref="StartAndAwait"/> does, and writes to crash.log what it does, with
    /// <paramref name="endedByItself"/> set where it was started and ended before it set the
    /// event, having run its own code. Never throws.
    /// </summary>
    internal static NewerVersionAnswer StartDetachedAndAwait(
        string file, Func<Guid, IReadOnlyList<string>> argumentsFor, string workingDirectory, TimeSpan bound,
        out bool endedByItself)
    {
        endedByItself = false;
        try
        {
            var id = Guids.NewGuid();
            using var started = HandOverContract.CreateStartedEvent(id);
            if (started is null) return NewerVersionAnswer.NotStarted;
            using var newer = DetachedProcess.Start(file, argumentsFor(id), workingDirectory);
            return newer is null ? NewerVersionAnswer.NotStarted : Await(newer, started, bound, out endedByItself);
        }
        catch (Exception ex)
        {
            CrashLog.TryWrite(ex);
            return NewerVersionAnswer.NotStarted;
        }
    }

    /// <summary>
    /// Waits up to <paramref name="bound"/> for <paramref name="newer"/> to set
    /// <paramref name="started"/>, and ends one that does not (<see cref="EndNewerVersion(SafeProcessHandle)"/>),
    /// writing why to crash.log. Answers <see cref="NewerVersionAnswer.Started"/>,
    /// <see cref="NewerVersionAnswer.NotStarted"/> or <see cref="NewerVersionAnswer.StillRunning"/>,
    /// with <paramref name="endedByItself"/> set where it ended before it set the event.
    /// </summary>
    private static NewerVersionAnswer Await(SafeProcessHandle newer, WaitHandle started, TimeSpan bound, out bool endedByItself)
    {
        var answer = AwaitStart(newer, started, bound, cancel: null, out var waitError);
        endedByItself = answer == NewerVersionStart.Ended;
        if (answer == NewerVersionStart.Started) return NewerVersionAnswer.Started;
        CrashLog.TryWrite(NotStartedFailure(answer, waitError, newer, bound));
        return EndNewerVersion(newer) ? NewerVersionAnswer.NotStarted : NewerVersionAnswer.StillRunning;
    }

    /// <summary>
    /// Waits for the newer version <paramref name="newer"/> to set
    /// <paramref name="started"/>, for up to <paramref name="bound"/>, and answers what
    /// came first: the event, the newer version ending, the bound, <paramref name="cancel"/>
    /// being set, or Windows refusing the wait, whose error code is
    /// <paramref name="waitError"/> (0 for every other answer). An event set by a newer
    /// version that has also ended answers <see cref="NewerVersionStart.Started"/>, and so
    /// does one found set by a look at the event alone, taken once more after the bound,
    /// <paramref name="cancel"/> or a refused wait: Windows refuses a wait on several
    /// handles where any one of them cannot be waited on, so the event can be readable
    /// where the wait was not. A look Windows refuses after the bound or
    /// <paramref name="cancel"/> is written to crash.log and the answer stands; after a
    /// refused wait it is not, the refusal being the answer already. Nothing else is
    /// written: the caller writes <see cref="NotStartedFailure(NewerVersionStart, int, SafeProcessHandle, TimeSpan)"/>. The wait dispatches
    /// nothing, so on a window's thread no window code runs during it. Never throws.
    /// </summary>
    internal static NewerVersionStart AwaitStart(
        Process newer, WaitHandle started, TimeSpan bound, WaitHandle? cancel, out int waitError) =>
        AwaitStart(newer.SafeHandle, started, bound, cancel, out waitError);

    /// <summary><see cref="AwaitStart(Process, WaitHandle, TimeSpan, WaitHandle?, out int)"/> for a process's handle.</summary>
    internal static NewerVersionStart AwaitStart(
        SafeProcessHandle newer, WaitHandle started, TimeSpan bound, WaitHandle? cancel, out int waitError)
    {
        // In the order of StartedIndex, EndedIndex and CancelIndex.
        List<SafeHandle> handles = cancel is null
            ? [started.SafeWaitHandle, newer]
            : [started.SafeWaitHandle, newer, cancel.SafeWaitHandle];
        var answer = Wait(handles, bound, out waitError) switch
        {
            StartedIndex => NewerVersionStart.Started,
            EndedIndex => NewerVersionStart.Ended,
            CancelIndex => NewerVersionStart.Cancelled,
            TimedOutWait => NewerVersionStart.TimedOut,
            _ => NewerVersionStart.WaitFailed,
        };
        if (answer is NewerVersionStart.Started or NewerVersionStart.Ended) return answer;

        switch (LookAt(started, out var lookError))
        {
            case HandleLook.Signalled:
                waitError = 0;
                return NewerVersionStart.Started;
            case HandleLook.NotSignalled:
                return answer;
            default:
                if (answer != NewerVersionStart.WaitFailed) CrashLog.TryWrite(LookFailure(lookError));
                return answer;
        }
    }

    /// <summary>Where <see cref="AwaitStart(SafeProcessHandle, WaitHandle, TimeSpan, WaitHandle?, out int)"/> puts the started event in the handles it waits on.</summary>
    private const int StartedIndex = 0;

    /// <summary>Where <see cref="AwaitStart(SafeProcessHandle, WaitHandle, TimeSpan, WaitHandle?, out int)"/> puts the newer version's process.</summary>
    private const int EndedIndex = 1;

    /// <summary>Where <see cref="AwaitStart(SafeProcessHandle, WaitHandle, TimeSpan, WaitHandle?, out int)"/> puts the Ctrl+C handle, where there is one.</summary>
    private const int CancelIndex = 2;

    /// <summary>
    /// Looks at <paramref name="handle"/> without waiting and answers whether it is
    /// signalled, or that Windows refused the look, whose error code is
    /// <paramref name="error"/>. Writes nothing. Never throws.
    /// </summary>
    internal static HandleLook LookAt(WaitHandle handle, out int error) => LookAt(handle.SafeWaitHandle, out error);

    private static HandleLook LookAt(SafeHandle handle, out int error) => Wait([handle], TimeSpan.Zero, out error) switch
    {
        0 => HandleLook.Signalled,
        TimedOutWait => HandleLook.NotSignalled,
        _ => HandleLook.Failed,
    };

    /// <summary>What is written to crash.log where Windows refuses a look at the started event with <paramref name="error"/>.</summary>
    internal static UpdateFailedException LookFailure(int error) => new(UpdateFailureStage.Install,
        $"Looking at whether the newer version had said it started failed with error {error}.");

    /// <summary>
    /// Ends <paramref name="newer"/>, a newer version that has not said its own code is
    /// running, and waits up to <see cref="EndBound"/> for it to have ended. Answers
    /// whether it runs no more of its code: it has ended, or Windows has accepted the
    /// request to end it, which no process can refuse once it is accepted, and which a
    /// process stuck in the middle of a call into Windows can take a while to complete.
    /// Answers false where Windows refused the request and the process is still running,
    /// and the caller then never renames the file it runs from. Any answer from
    /// TerminateProcess but success counts as refused. Where the newer version has not
    /// been seen to end, one line goes to crash.log: the request refused, with its error
    /// code; the newer version still running after the bound; or the wait for it refused,
    /// with its error code. Never throws.
    /// </summary>
    internal static bool EndNewerVersion(Process newer) => EndNewerVersion(newer.SafeHandle);

    /// <summary><see cref="EndNewerVersion(Process)"/> for a process's handle.</summary>
    internal static bool EndNewerVersion(SafeProcessHandle handle)
    {
        try
        {
            // A look Windows refuses goes on to the request to end it, as for a newer
            // version still running.
            if (LookAt(handle, out _) == HandleLook.Signalled) return true;
            var added = false;
            bool accepted;
            var terminateError = 0;
            try
            {
                handle.DangerousAddRef(ref added);
                accepted = Kernel32.TerminateProcess(handle.DangerousGetHandle(), NotStartedExitCode);
                if (!accepted) terminateError = Marshal.GetLastWin32Error();
            }
            finally
            {
                if (added) handle.DangerousRelease();
            }
            var ended = Wait([handle], EndBound, out var waitError);
            if (ended == 0) return true;
            var waitFailed = ended == FailedWait ? $" Waiting for it to end failed with error {waitError}." : string.Empty;
            CrashLog.TryWrite(new UpdateFailedException(UpdateFailureStage.Install,
                !accepted ? $"Windows refused the request to end the newer version, with error {terminateError}.{waitFailed}"
                : ended == FailedWait ? $"Waiting for the newer version to end, after Windows accepted the request to end it, failed with error {waitError}."
                : "The newer version had not ended a few seconds after Windows accepted the request to end it."));
            return accepted;
        }
        catch (Exception ex)
        {
            CrashLog.TryWrite(ex);
            return false;
        }
    }

    /// <summary>How long <see cref="EndNewerVersion(SafeProcessHandle)"/> waits for the newer version to have ended.</summary>
    private static readonly TimeSpan EndBound = TimeSpan.FromSeconds(5);

    /// <summary>The exit code <see cref="EndNewerVersion(SafeProcessHandle)"/> ends the newer version with.</summary>
    private const uint NotStartedExitCode = 1;

    /// <summary>
    /// What a newer version that did not say its own code is running is written to
    /// crash.log as, for <paramref name="answer"/>: the exit code it ended with, how long
    /// it was waited for, or the error code <paramref name="waitError"/> Windows refused the
    /// wait with. Never throws.
    /// </summary>
    internal static UpdateFailedException NotStartedFailure(
        NewerVersionStart answer, int waitError, Process newer, TimeSpan bound) =>
        NotStartedFailure(answer, waitError, newer.SafeHandle, bound);

    /// <summary><see cref="NotStartedFailure(NewerVersionStart, int, Process, TimeSpan)"/> for a process's handle.</summary>
    internal static UpdateFailedException NotStartedFailure(
        NewerVersionStart answer, int waitError, SafeProcessHandle newer, TimeSpan bound)
    {
        var message = answer switch
        {
            NewerVersionStart.Ended => Kernel32.GetExitCodeProcess(newer, out var exitCode)
                ? $"The newer version ended with exit code {(int)exitCode} before it said it had started."
                : "The newer version ended before it said it had started.",
            NewerVersionStart.Cancelled => "Ctrl+C came before the newer version said it had started.",
            NewerVersionStart.WaitFailed =>
                $"Waiting for the newer version to say it had started failed with error {waitError}.",
            _ => $"The newer version did not say it had started within {bound.TotalSeconds:0} seconds.",
        };
        return new UpdateFailedException(UpdateFailureStage.Install, message);
    }

    /// <summary>What <see cref="Wait"/> answers where the bound ran out.</summary>
    private const int TimedOutWait = -1;

    /// <summary>What <see cref="Wait"/> answers where Windows refused the wait.</summary>
    private const int FailedWait = -2;

    /// <summary>
    /// Waits on <paramref name="handles"/> for up to <paramref name="bound"/>, dispatching
    /// nothing, and answers the index of the first signalled, <see cref="TimedOutWait"/>
    /// or <see cref="FailedWait"/>, with the error code Windows refused it with in
    /// <paramref name="error"/> (0 for every other answer). Writes nothing: each caller
    /// writes a refusal once.
    /// </summary>
    private static int Wait(List<SafeHandle> handles, TimeSpan bound, out int error)
    {
        var milliseconds = (uint)Math.Clamp(bound.TotalMilliseconds, 0, uint.MaxValue - 1);
        var result = HandleWait.Raw(handles, milliseconds, out error);
        if (result < Kernel32.WAIT_OBJECT_0 + (uint)handles.Count) return (int)(result - Kernel32.WAIT_OBJECT_0);
        if (result == Kernel32.WAIT_TIMEOUT) return TimedOutWait;
        // No mutex is among these handles, so nothing answers abandoned, and any other answer
        // is Windows refusing the wait, whose error code Raw has read.
        return FailedWait;
    }
}

/// <summary>What came first while a copy waited for the newer version it started (<see cref="NewerVersionProcess.AwaitStart(SafeProcessHandle, WaitHandle, TimeSpan, WaitHandle?, out int)"/>).</summary>
internal enum NewerVersionStart
{
    /// <summary>The newer version set the event: its own code is running.</summary>
    Started,

    /// <summary>The newer version ended without setting it.</summary>
    Ended,

    /// <summary>The wait's bound ran out.</summary>
    TimedOut,

    /// <summary>Ctrl+C came first.</summary>
    Cancelled,

    /// <summary>Windows refused the wait.</summary>
    WaitFailed,
}

/// <summary>What a look at a handle without waiting found (<see cref="NewerVersionProcess.LookAt(WaitHandle, out int)"/>).</summary>
internal enum HandleLook
{
    /// <summary>It is signalled.</summary>
    Signalled,

    /// <summary>It is not signalled.</summary>
    NotSignalled,

    /// <summary>Windows refused the look.</summary>
    Failed,
}

/// <summary>What a newer version this copy started did, once it was waited for (<see cref="NewerVersionProcess.StartAndAwait"/>).</summary>
internal enum NewerVersionAnswer
{
    /// <summary>It said its own code is running.</summary>
    Started,

    /// <summary>It was not started, or it did not say so and has been ended.</summary>
    NotStarted,

    /// <summary>It did not say so, and Windows would not end it.</summary>
    StillRunning,
}
