using System.Diagnostics;
using System.Threading;
using InstallerClean.Helpers;
using InstallerClean.Resources;
using InstallerClean.Services;
using Microsoft.Win32.SafeHandles;

namespace InstallerClean.Cli;

/// <summary>
/// The console, the single-instance mutex and the processes <see cref="CommandLineUpdate"/>
/// works with, for one run.
/// </summary>
internal sealed class ConsoleUpdateHost(string arg, Func<InstanceLock>? newLock = null) : ICommandLineUpdateHost
{
    private readonly Func<InstanceLock> _newLock = newLock ?? (() => new InstanceLock());
    private InstanceLock? _instanceLock;
    private bool _instanceLockLeftToProcessEnd;
    private readonly object _ctrlCGate = new();
    private bool _newerVersionAnswersCtrlC;
    private bool _cancellingHeldBack;

    /// <summary>
    /// The read of the console still waiting for a line, which the next read in this process
    /// waits on rather than start a second read beside it. One for the process, as there is
    /// one console.
    /// </summary>
    private static Task<string?>? s_pendingRead;
    private static readonly object s_readGate = new();

    /// <summary>
    /// Takes the line saying the run is cancelling, for a Ctrl+C that has just come: answers
    /// whether this run prints it now. The run's answer to Ctrl+C calls it once. Once the
    /// newer version is being started with the event that passes it Ctrl+C, or has
    /// started, the newer version answers Ctrl+C, so this run holds the line back, prints
    /// nothing and keeps waiting for its exit code; the newer version writes the run's
    /// summary, unless Ctrl+C ended it before it could answer, when this run writes it.
    /// This run prints the line it held back where that start fails, or where Ctrl+C ended
    /// the newer version before it could answer. Otherwise a Ctrl+C cancels this run as
    /// anywhere else, which stops the newer version being started.
    /// </summary>
    public bool TakeCancellingLine()
    {
        lock (_ctrlCGate)
        {
            if (!_newerVersionAnswersCtrlC) return true;
            _cancellingHeldBack = true;
            return false;
        }
    }

    // Neither throws, as IConsolePrompt promises: Main puts ConsoleGuard on the console's
    // output before any run starts, and it takes a refused write, records a run's first in
    // crash.log and lets the run go on. Writing anywhere but Console.Out loses that.
    public void WriteLine(string line) => Console.WriteLine(line);

    public void Write(string text) => Console.Write(text);

    /// <summary>
    /// Empties the console's input buffer where standard input is the console, so key
    /// presses typed during a scan do not answer the question that follows. A question is
    /// put only where input is not redirected (Program.ConsoleCanAsk); the check here keeps
    /// a redirected input's lines, which are answers somebody wrote on purpose, safe from
    /// any later caller. A read already waiting from an earlier question has taken its
    /// keys from the buffer itself, so this reaches only keys no read has started on. A
    /// failure is recorded in crash.log once a process, as the question is asked again
    /// and would record the same failure each time, and the question is asked anyway.
    /// </summary>
    public void DiscardTypedAhead()
    {
        try
        {
            if (Console.IsInputRedirected) return;
            var input = Interop.Native.Kernel32.GetStdHandle(Interop.Native.Kernel32.STD_INPUT_HANDLE);
            if (input == IntPtr.Zero || input == new IntPtr(-1))
            {
                NoteDiscardFailed(new InvalidOperationException("Standard input has no console handle to empty."));
                return;
            }
            if (!Interop.Native.Kernel32.FlushConsoleInputBuffer(input))
                NoteDiscardFailed(new System.ComponentModel.Win32Exception());
        }
        catch (Exception ex)
        {
            NoteDiscardFailed(ex);
        }
    }

    private static int s_discardFailureNoted;

    private static void NoteDiscardFailed(Exception ex)
    {
        if (Interlocked.Exchange(ref s_discardFailureNoted, 1) == 0) CrashLog.TryWrite(ex);
    }

    /// <summary>
    /// Reads a line on a thread of its own and waits for it or for <paramref name="token"/>.
    /// A console being closed cancels the token and leaves the read waiting, so the run goes
    /// on to end as cancelled while the read is left to end with the process. A later read
    /// in this process waits on that same read.
    /// </summary>
    public string? ReadLine(CancellationToken token)
    {
        Task<string?> read;
        lock (s_readGate)
            read = s_pendingRead ??= Task.Factory.StartNew(ReadConsoleLine, CancellationToken.None,
                TaskCreationOptions.LongRunning, TaskScheduler.Default);
        try
        {
            read.Wait(token);
        }
        catch (OperationCanceledException)
        {
            return null;
        }
        lock (s_readGate)
            if (ReferenceEquals(s_pendingRead, read)) s_pendingRead = null;
        return read.GetAwaiter().GetResult();
    }

    /// <summary>Test seam: forgets the read left waiting, so a test starts with none.</summary>
    internal static void ForgetPendingRead()
    {
        lock (s_readGate) s_pendingRead = null;
    }

    private static string? ReadConsoleLine()
    {
        try
        {
            return Console.ReadLine();
        }
        catch (IOException ex)
        {
            CrashLog.TryWrite(ex);
            return null;
        }
    }

    /// <summary>
    /// Whether the update question went unanswered in this run, after which the run asks
    /// nothing more at the console (<see cref="ICommandLineUpdateHost.NobodyAnswered"/>).
    /// </summary>
    public bool AsksNothingMore { get; private set; }

    public void NobodyAnswered() => AsksNothingMore = true;

    public bool TryTakeInstanceLock()
    {
        _instanceLock = _newLock();
        if (_instanceLock.TryTake()) return true;
        _instanceLock.Dispose();
        _instanceLock = null;
        return false;
    }

    public void ReleaseInstanceLock()
    {
        if (_instanceLockLeftToProcessEnd) return;
        _instanceLock?.Release();
        _instanceLock?.Dispose();
        _instanceLock = null;
    }

    /// <summary>
    /// Keeps the single-instance mutex this run holds until its process ends, where it stops
    /// waiting for a newer version that may still be running a Move or Delete for it:
    /// <see cref="ReleaseInstanceLock"/> then lets go of nothing. Do not release the mutex
    /// on these paths. The newer version's cover (StagedRunCover) counts this process
    /// running as the mutex held, so a release while it runs leaves the newer version acting
    /// while the window or another run may hold the mutex. Ending with the process, the
    /// mutex is the abandoned one, which the newer version's cover takes or, taken by another
    /// process first, stops that version's batch before its next file.
    /// </summary>
    internal void LeaveInstanceLockToProcessEnd() => _instanceLockLeftToProcessEnd = true;

    public int? RunNewerVersion(
        string file, IReadOnlyList<string> arguments, string replaces, string fromVersion, string toVersion,
        Action onStarted, CancellationToken token)
    {
        Console.Out.Flush();
        // The console gives Ctrl+C only to the processes connected to it, so one that comes
        // as the newer version starts can reach this run alone. This event is set on Ctrl+C
        // from here until the newer version ends and passed to it by name, and the newer
        // version takes it as its own Ctrl+C, one set before it opened the event included.
        // Where it cannot be made, the newer version answers Ctrl+C once it has started.
        var cancelId = Guids.NewGuid();
        var startedId = Guids.NewGuid();
        using var cancelEvent = CreateCancelEvent(cancelId);
        // The event the newer version sets once its own code is running. Where it cannot be
        // made, the newer version is not started: one that cannot be seen to start could run
        // the command a second time beside this version.
        using var startedEvent = HandOverContract.CreateStartedEvent(startedId);
        if (startedEvent is null)
        {
            NewerVersionNotStarted();
            return null;
        }
        using var setOnCtrlC = cancelEvent is null
            ? default
            : token.Register(static e => ((EventWaitHandle)e!).Set(), cancelEvent);
        if (cancelEvent is not null) NewerVersionAnswersCtrlC(true);
        // The command, then the three switches asking the newer version to run it standing
        // for this copy and then take its place, each once, as HandOverContract.ReadReplaceRequest
        // reads them, then the Ctrl+C event and /noupdate, so it goes straight to the command.
        List<string> passed =
        [
            .. arguments,
            .. HandOverContract.ReplaceArguments(replaces, Environment.ProcessId, startedId),
        ];
        if (cancelEvent is not null) passed.Add(CliContract.CancelEventArgument(cancelId));
        passed.Add(CliContract.NoUpdateSwitch);
        Process? started;
        try
        {
            started = Start(file, passed, token);
        }
        catch
        {
            NewerVersionNotStarted();
            throw;
        }
        if (started is null)
        {
            NewerVersionNotStarted();
            return null;
        }
        using var process = started;
        var seen = NewerVersionSaidItStarted(process, startedEvent, token);
        if (seen == StartSeen.NotStarted) return null;
        // The newer version is running the command from here, or has run it, so nothing
        // leaves this method as an exception: the run reads a throw as the newer version not
        // having started, takes away what it runs from and runs the command again with this
        // version. A throw is written to crash.log. One from the
        // wait itself is answered with the error code, so this run ends with nothing more
        // run, and the newer version writes its own summary. This run then leaves the
        // console's code page to the newer version, which may still be writing to it.
        if (cancelEvent is null) NewerVersionAnswersCtrlC(true);
        // Said before the Application-log write, which can take a moment the first time.
        try
        {
            onStarted();
        }
        catch (Exception ex)
        {
            CrashLog.TryWrite(ex);
        }
        try
        {
            MachineContract.WriteEventLog(CliEventClass.UpdatedNotice,
                () => Program.UpdatedEventLogLine(arg, fromVersion, toVersion, arguments));
        }
        catch (Exception ex)
        {
            CrashLog.TryWrite(ex);
        }
        // A newer version this run could neither end nor wait for answers the error code, and
        // may still be running in this console. It may be running a Move or Delete, so this
        // run's mutex is left to its process end (LeaveInstanceLockToProcessEnd).
        if (seen == StartSeen.NotWaitedFor)
        {
            LeaveInstanceLockToProcessEnd();
            Program.LeaveConsoleToNewerVersion();
            return CliExitCode.Error;
        }
        try
        {
            process.WaitForExit();
        }
        catch (Exception ex)
        {
            CrashLog.TryWrite(ex);
            LeaveInstanceLockToProcessEnd();
            Program.LeaveConsoleToNewerVersion();
            return CliExitCode.Error;
        }
        try
        {
            return process.ExitCode;
        }
        catch (Exception ex)
        {
            CrashLog.TryWrite(ex);
            return CliExitCode.Error;
        }
    }

    /// <summary>
    /// Waits for the newer version <paramref name="process"/> to set
    /// <paramref name="startedEvent"/>, for up to <see cref="HandOverContract.StartBound"/>,
    /// and answers <see cref="StartSeen.Started"/> where it does. Where it ends first, the
    /// bound runs out, Windows refuses the wait or Ctrl+C comes first, the newer version has
    /// run none of the command: it is ended and waited for, what happened is written to
    /// crash.log, and the answer is <see cref="StartSeen.NotStarted"/>, with the line saying
    /// the run is cancelling printed where one was held back. Ctrl+C, or a newer version
    /// that Ctrl+C ended before it had a handler for it, throws
    /// <see cref="OperationCanceledException"/> instead, so the run answers it as Ctrl+C
    /// before the newer version started.
    /// </summary>
    /// <remarks>
    /// A newer version Windows would not end is still running its own code, so it is waited
    /// for until it ends, Ctrl+C or not, and nothing it runs from is taken away under it.
    /// It answers <see cref="StartSeen.NotStarted"/> only where a look at the event shows it
    /// unset once it has ended. Where it set the event, or the look fails, it may have run
    /// the command, so it answers <see cref="StartSeen.Started"/> and the run hands back its
    /// exit code; where the wait for it to end fails, <see cref="StartSeen.NotWaitedFor"/>.
    /// Either way the run never runs the command a second time with this version.
    /// </remarks>
    private StartSeen NewerVersionSaidItStarted(Process process, EventWaitHandle startedEvent, CancellationToken token)
    {
        var answer = NewerVersionProcess.AwaitStart(
            process, startedEvent, HandOverContract.StartBound, token.WaitHandle, out var waitError);
        if (answer == NewerVersionStart.Started) return StartSeen.Started;
        var failure = NewerVersionProcess.NotStartedFailure(answer, waitError, process, HandOverContract.StartBound);
        if (!NewerVersionProcess.EndNewerVersion(process))
        {
            try
            {
                process.WaitForExit();
            }
            catch (Exception ex)
            {
                CrashLog.TryWrite(ex);
                return StartSeen.NotWaitedFor;
            }
            switch (NewerVersionProcess.LookAt(startedEvent, out var lookError))
            {
                case HandleLook.Signalled:
                    return StartSeen.Started;
                case HandleLook.Failed:
                    CrashLog.TryWrite(NewerVersionProcess.LookFailure(lookError));
                    return StartSeen.Started;
            }
        }
        CrashLog.TryWrite(failure);
        if (answer == NewerVersionStart.Ended && EndedForCtrlC(process))
            token.WaitHandle.WaitOne(CommandLineUpdate.ControlCBound);
        NewerVersionNotStarted();
        token.ThrowIfCancellationRequested();
        return StartSeen.NotStarted;
    }

    /// <summary>What <see cref="NewerVersionSaidItStarted"/> found.</summary>
    private enum StartSeen
    {
        /// <summary>The newer version set the event, or may have and has ended: it runs or ran the command.</summary>
        Started,

        /// <summary>The newer version ran none of the command and has been ended.</summary>
        NotStarted,

        /// <summary>Windows would neither end the newer version nor let this run wait for it to end.</summary>
        NotWaitedFor,
    }

    private static bool EndedForCtrlC(Process process)
    {
        try
        {
            return process.ExitCode == CommandLineUpdate.ControlCExit;
        }
        catch (Exception ex) when (ex is InvalidOperationException or System.ComponentModel.Win32Exception)
        {
            return false;
        }
    }

    /// <summary>
    /// Starts <paramref name="file"/> sharing this console, its standard handles and its
    /// working folder, so a relative path
    /// given to the newer version means what it meant to this run. A start that fails is
    /// tried again through <see cref="ProcessStartRetry"/>, and Ctrl+C ends the wait between
    /// tries. Answers null where every try failed, each written
    /// to crash.log. Throws <see cref="OperationCanceledException"/> where
    /// <paramref name="token"/> is cancelled and no try has started a process.
    /// </summary>
    private static Process? Start(
        string file, IReadOnlyList<string> arguments, CancellationToken token)
    {
        var info = new ProcessStartInfo(file) { UseShellExecute = false };
        foreach (var argument in arguments) info.ArgumentList.Add(argument);

        token.ThrowIfCancellationRequested();
        try
        {
            return ProcessStartRetry.Start(
                info,
                IsStartFailure,
                interval =>
                {
                    token.WaitHandle.WaitOne(interval);
                    token.ThrowIfCancellationRequested();
                });
        }
        catch (Exception ex) when (IsStartFailure(ex))
        {
            CrashLog.TryWrite(ex);
            token.ThrowIfCancellationRequested();
            return null;
        }
    }

    /// <summary>
    /// A start Windows refused. The wait between tries reads the token's wait handle, which
    /// throws <see cref="ObjectDisposedException"/> where the token's source has been
    /// disposed; that derives from InvalidOperationException but is not a refused start, so
    /// it reaches the caller.
    /// </summary>
    private static bool IsStartFailure(Exception ex) =>
        ex is System.ComponentModel.Win32Exception or IOException
            or (InvalidOperationException and not ObjectDisposedException);

    internal void NewerVersionAnswersCtrlC(bool answers)
    {
        lock (_ctrlCGate) _newerVersionAnswersCtrlC = answers;
    }

    /// <summary>
    /// The newer version did not start, so this run answers Ctrl+C again, and prints the
    /// line saying it is cancelling where it held one back meanwhile.
    /// </summary>
    internal void NewerVersionNotStarted()
    {
        NewerVersionAnswersCtrlC(false);
        SayHeldBackCancelling();
    }

    /// <summary>
    /// Prints the line saying the run is cancelling, where it was held back: the run calls
    /// it where Ctrl+C ended the newer version before it could answer it.
    /// </summary>
    internal void SayHeldBackCancelling()
    {
        bool heldBack;
        lock (_ctrlCGate)
        {
            heldBack = _cancellingHeldBack;
            _cancellingHeldBack = false;
        }
        if (heldBack) Program.SayCancelling(Strings.Cli_Cancelling);
    }

    /// <summary>
    /// Creates the event <paramref name="id"/> names, or answers null where it cannot or
    /// where an event of that name exists already, which this run did not make. Each is
    /// written to crash.log.
    /// </summary>
    private static EventWaitHandle? CreateCancelEvent(Guid id) =>
        HandOverContract.CreateEvent(CliContract.CancelEventName(id));
}
