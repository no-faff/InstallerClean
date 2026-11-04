using InstallerClean.Helpers;
using InstallerClean.Resources;

namespace InstallerClean.Services;

/// <summary>
/// Why a command-line run goes on with the version it is although a newer one is out, each
/// answered by its own Application-log notice.
/// </summary>
public enum UpdateNotMade
{
    /// <summary>The newer version could not be downloaded.</summary>
    DownloadFailed,

    /// <summary>InstallerClean was in use, so the update was not tried.</summary>
    InUse,

    /// <summary>
    /// The newer version was downloaded and could not be run: its staging folder not made,
    /// a download not carrying its version or not held, or a start that did not happen.
    /// </summary>
    NotRun,

    /// <summary>The copy cannot tell whether its setup installed it.</summary>
    CopyCannotTell,

    /// <summary>The copy was built from source.</summary>
    FromSource,

    /// <summary>The newer release needs .NET Framework 4.8, which this PC does not have.</summary>
    NeedsNetFramework48,

    /// <summary>The newer release rules this copy out.</summary>
    CannotInstallAutomatically,
}

/// <summary>
/// What a command-line run's update ended in, which the host turns into an
/// Application-log notice and an exit code.
/// </summary>
internal abstract record CommandLineUpdateResult
{
    private CommandLineUpdateResult() { }

    /// <summary>
    /// The update leaves the run to the version it is, which goes on with the command where
    /// nothing after this stops it: Ctrl+C, or for a Move or Delete, the mutex.
    /// <paramref name="NotMade"/> is null where no update was tried and none is noted: no
    /// newer version, the check failing, or a no at the question. Otherwise it says why the
    /// newer version was not run, and the two versions are the running one and the newer
    /// one. The host notes why at once.
    /// </summary>
    public sealed record CarriedOn(UpdateNotMade? NotMade, string? FromVersion, string? ToVersion)
        : CommandLineUpdateResult
    {
        /// <summary>
        /// Whether the host says the run goes on with this version, once it knows it does:
        /// where the update was tried and failed or InstallerClean was in use, whose 3006 is
        /// the failed-update one (<see cref="CliContract.UpdateNotMadeClass"/>), and not where
        /// the lines saying why already point to what to do instead.
        /// </summary>
        public bool SaysRunningInstead =>
            NotMade is { } notMade && CliContract.UpdateNotMadeClass(notMade) == CliEventClass.UpdateFailedNotice;
    }

    /// <summary>
    /// The newer version ran in this console with <paramref name="ArgumentsRun"/> and has
    /// finished; <paramref name="ExitCode"/> is its exit code, which the host ends this run
    /// with, or reads as Ctrl+C having ended it before it could answer. Where the newer
    /// version could be neither ended nor waited for, it is this run's
    /// <see cref="Helpers.CliExitCode.Error"/>, and the newer version may still be running
    /// in the console.
    /// </summary>
    public sealed record HandedOver(int ExitCode, string FromVersion, string ToVersion, string ArgumentsRun)
        : CommandLineUpdateResult;

    /// <summary>
    /// Ctrl+C before the newer version started: at the question, during the download, or as
    /// it was being started. What the download left is taken away.
    /// </summary>
    public sealed record Cancelled : CommandLineUpdateResult;

    /// <summary>
    /// The newer version was not run, for the reason <paramref name="NotMade"/> gives, and
    /// Ctrl+C had come, so nothing runs.
    /// </summary>
    public sealed record FailedThenCancelled(UpdateNotMade NotMade, string FromVersion, string ToVersion)
        : CommandLineUpdateResult;

    /// <summary>The answer where nothing was tried.</summary>
    public static readonly CarriedOn NothingTried = new(null, null, null);
}

/// <summary>
/// What <see cref="CommandLineUpdate"/> needs from the console host: the console
/// (<see cref="IConsolePrompt"/>), the single-instance mutex and starting the newer version
/// in this console. Each member is the host's because each touches the console or the
/// process.
/// </summary>
internal interface ICommandLineUpdateHost : IConsolePrompt
{
    /// <summary>
    /// Takes the single-instance mutex without waiting, on the calling thread. Answers
    /// whether it is now held.
    /// </summary>
    bool TryTakeInstanceLock();

    /// <summary>Releases the single-instance mutex, on the thread that took it.</summary>
    void ReleaseInstanceLock();

    /// <summary>
    /// Records that the question went unanswered, so the rest of the run asks nothing at
    /// this console, as where nobody can answer it. The read left waiting would otherwise
    /// take a line typed later as the answer to the next question.
    /// </summary>
    void NobodyAnswered();

    /// <summary>
    /// Starts the newer version at <paramref name="file"/> in this console, with
    /// <paramref name="arguments"/>, the switches asking it to run them standing for the copy
    /// at <paramref name="replaces"/> and then to take that copy's place
    /// (<see cref="Helpers.HandOverContract.ReplaceArguments"/>), the Ctrl+C event and
    /// <see cref="Helpers.CliContract.NoUpdateSwitch"/>. Once it has set the started event,
    /// calls <paramref name="onStarted"/>, notes in the Application log that this run, at
    /// <paramref name="fromVersion"/>, found <paramref name="toVersion"/> and is running
    /// <paramref name="arguments"/> with it,
    /// waits for it to end and answers with its exit code. Answers null where it did not
    /// start: it ended, or did not set the event within
    /// <see cref="Helpers.HandOverContract.StartBound"/> and was ended, with nothing said and
    /// nothing noted. One Windows will not end is waited for until it ends, and counts as
    /// started unless the event is then seen unset; where that wait fails, the answer is
    /// <see cref="Helpers.CliExitCode.Error"/> once <paramref name="onStarted"/> and the note
    /// have run. Throws <see cref="OperationCanceledException"/> where
    /// <paramref name="token"/> is cancelled before it has started, the newer version having
    /// been ended. Nothing is thrown once it has, a throw from <paramref name="onStarted"/>
    /// included. From the moment it is being started, Ctrl+C is the newer version's to
    /// answer.
    /// </summary>
    int? RunNewerVersion(
        string file, IReadOnlyList<string> arguments, string replaces, string fromVersion, string toVersion,
        Action onStarted, CancellationToken token);
}

/// <summary>
/// The command line's update, run before the scan of a <c>/s</c>, <c>/d</c> or <c>/m</c>:
/// it checks for a newer version and, where one is out, downloads it into a staging folder
/// and runs the command with it there, in the same console. The newer version then takes
/// this copy's place once both have ended. Where a person can see the console and answer
/// it, it asks first, and after a yes the newer version scans and lists as <c>/s</c> does
/// and moves or deletes nothing. Where nobody can answer, or nobody answers within
/// <see cref="ConsoleQuestion.AnswerBound"/>, it updates without asking and the newer
/// version runs the command as given. Where the copy cannot update itself, the
/// newer version's release rules this copy out, InstallerClean is in use or a part fails,
/// it says so, notes it, and runs the command with the version it is, unless Ctrl+C has
/// come.
/// </summary>
/// <remarks>
/// Synchronous on purpose. The host calls it on the thread that owns the single-instance
/// mutex, and the mutex has to be released on the thread that took it, so the check and
/// the download are waited on here rather than awaited. A console host has no
/// SynchronizationContext, so the wait cannot deadlock.
/// </remarks>
internal sealed class CommandLineUpdate
{
    /// <summary>
    /// How long the check may take where a person is waiting on the console. Where nobody
    /// is, the check's own <see cref="UpdateCheckService.RequestTimeout"/> bounds it.
    /// </summary>
    internal static readonly TimeSpan WatchedCheckBound = TimeSpan.FromSeconds(3);

    /// <summary>
    /// How long the run waits for Ctrl+C's cancellation once something Ctrl+C can end has
    /// ended without it: the question's read, which Ctrl+C can end as the end of input, or
    /// the newer version, which Windows ends with <see cref="ControlCExit"/> where it has
    /// no handler for Ctrl+C yet. The host's handler cancels the token on the console's
    /// control-handler thread, which can run after either has ended, so the run waits up
    /// to this long for it and reads that Ctrl+C as its own.
    /// </summary>
    internal static readonly TimeSpan ControlCBound = TimeSpan.FromMilliseconds(500);

    /// <summary>
    /// The exit code of a console process Windows ended for Ctrl+C because it had no
    /// handler for it yet, STATUS_CONTROL_C_EXIT.
    /// </summary>
    internal const int ControlCExit = unchecked((int)0xC000013A);

    private readonly Func<CancellationToken, Task<UpdateCheckResult>> _check;
    private readonly IAppUpdater _updater;
    private readonly ICommandLineUpdateHost _host;
    private readonly TimeSpan _watchedCheckBound;
    private readonly TimeSpan _controlCBound;
    private readonly TimeSpan _answerBound;

    public CommandLineUpdate(
        Func<CancellationToken, Task<UpdateCheckResult>> check, IAppUpdater updater, ICommandLineUpdateHost host)
        : this(check, updater, host, WatchedCheckBound, ControlCBound, ConsoleQuestion.AnswerBound) { }

    /// <summary>Test seam: the three bounds given.</summary>
    internal CommandLineUpdate(
        Func<CancellationToken, Task<UpdateCheckResult>> check, IAppUpdater updater, ICommandLineUpdateHost host,
        TimeSpan watchedCheckBound, TimeSpan controlCBound, TimeSpan answerBound)
    {
        _check = check;
        _updater = updater;
        _host = host;
        _watchedCheckBound = watchedCheckBound;
        _controlCBound = controlCBound;
        _answerBound = answerBound;
    }

    /// <summary>
    /// What the newer setup is started with on the command line's route, where it puts the
    /// newer version in this copy's place (<see cref="SetupReplacement"/>). /SP- leaves out
    /// the page asking whether to install, /NORESTART keeps Setup from restarting Windows, and
    /// /SUPPRESSMSGBOXES answers any box Setup would show with its default, so it never
    /// waits on one; a box it would have shown fails the setup. /NOCLOSEAPPLICATIONS keeps
    /// Setup from closing programs holding files it replaces. /VERYSILENT shows nothing.
    /// /UPDATEFROMCLI has Setup wait up to 30 seconds for every copy to let the
    /// single-instance mutex go before it checks it, and opens nothing afterwards.
    /// /UPDATEFROMAPP is left out: it is what opens the window afterwards.
    /// </summary>
    internal const string SetupArguments =
        "/VERYSILENT /SP- /NORESTART /SUPPRESSMSGBOXES /NOCLOSEAPPLICATIONS /UPDATEFROMCLI";

    /// <summary>
    /// Runs the update for a run of <paramref name="command"/> given
    /// <paramref name="arguments"/>.
    /// </summary>
    /// <param name="canAsk">Whether a person can see the console and answer it.</param>
    /// <param name="command">The command typed, lower-cased: /s, /d or /m.</param>
    /// <param name="arguments">The arguments as given, which the newer version gets where nobody can answer.</param>
    /// <param name="exePath">The running command line's own path.</param>
    /// <param name="token">Cancelled by Ctrl+C.</param>
    public CommandLineUpdateResult Run(
        bool canAsk, string command, IReadOnlyList<string> arguments, string exePath, CancellationToken token)
    {
        if (Check(canAsk, token) is not { } available)
            return token.IsCancellationRequested ? new CommandLineUpdateResult.Cancelled() : CommandLineUpdateResult.NothingTried;

        var current = available.CurrentVersion;
        var latest = available.LatestVersion;
        _host.WriteLine(string.Format(Strings.UpdateNotice_Available, latest));

        if (!AppCopy.CanUpdateItself(_updater.Kind))
            return CannotUpdateItself(current, latest, token);

        // Ctrl+C answers null here, so the token says whether it came.
        var refusal = Requirements(canAsk, latest, token);
        if (token.IsCancellationRequested) return new CommandLineUpdateResult.Cancelled();
        if (refusal is not null) return Refused(refusal.Value, current, latest, token);

        // A Move or Delete takes the mutex here, before the question, so a person is not asked
        // one whose answer cannot be acted on. Where the newer version runs the Move or Delete
        // it is held until that has ended, so nothing else that takes it runs meanwhile: the
        // window, another Move or Delete, and another run's update. The newer version goes on
        // without it where it cannot take it. After a yes the newer version only scans, which
        // takes no mutex, so it is let go before that starts. A question nobody answers goes
        // on as a run nobody can answer does, the mutex still held.
        var holdsLock = false;
        try
        {
            if (command is "/d" or "/m")
            {
                holdsLock = _host.TryTakeInstanceLock();
                if (!holdsLock) return CarryOn(UpdateNotMade.InUse, null, current, latest, token);
            }

            if (canAsk)
            {
                switch (Ask(token))
                {
                    case Answer.Cancelled: return new CommandLineUpdateResult.Cancelled();
                    case Answer.No: return CommandLineUpdateResult.NothingTried;
                    case Answer.TimedOut:
                        _host.NobodyAnswered();
                        SayLine(Strings.Cli_UpdateNoAnswer);
                        return Update(false, true, command, arguments, exePath, current, latest, token);
                }
                if (holdsLock)
                {
                    _host.ReleaseInstanceLock();
                    holdsLock = false;
                }
            }

            return Update(canAsk, canAsk, command, arguments, exePath, current, latest, token);
        }
        finally
        {
            if (holdsLock) _host.ReleaseInstanceLock();
        }
    }

    /// <summary>
    /// Says why a copy that cannot update itself is not updated, leaving the command to this
    /// version. The command line is never Scoop's copy (<see cref="AppCopy.DetectRunningCommandLine"/>),
    /// so a copy that is neither undetermined nor downloaded is a build from source.
    /// </summary>
    private CommandLineUpdateResult CannotUpdateItself(string current, string latest, CancellationToken token)
    {
        UpdateNotMade notMade;
        if (_updater.Kind == AppCopyKind.Undetermined)
        {
            _host.WriteLine(Strings.UpdateNotice_CopyCannotTell);
            WriteReleaseLink(latest);
            notMade = UpdateNotMade.CopyCannotTell;
        }
        else
        {
            _host.WriteLine(Strings.UpdateNotice_Recommended);
            WriteReleaseLink(latest);
            notMade = UpdateNotMade.FromSource;
        }
        return Unchanged(notMade, current, latest, token);
    }

    /// <summary>Says why the newer version's release rules this copy out, leaving the command to this version.</summary>
    private CommandLineUpdateResult Refused(UpdateRefusal refusal, string current, string latest, CancellationToken token)
    {
        if (refusal == UpdateRefusal.NeedsNetFramework48)
        {
            _host.WriteLine(Strings.Cli_UpdateNeedsNetFramework48);
            _host.WriteLine(string.Format(Strings.Cli_UpdateDownloadLink,
                Strings.UpdateNotice_DownloadNetFramework48, UpdateManifest.NetFramework48DownloadUrl));
            return Unchanged(UpdateNotMade.NeedsNetFramework48, current, latest, token);
        }
        _host.WriteLine(string.Format(Strings.UpdateNotice_CannotInstallAutomatically, latest));
        WriteReleaseLink(latest);
        return Unchanged(UpdateNotMade.CannotInstallAutomatically, current, latest, token);
    }

    /// <summary>
    /// The answer where the newer version was not tried for a reason already said, with no
    /// further line: the command is left to this version, or nothing runs where Ctrl+C has come.
    /// </summary>
    private static CommandLineUpdateResult Unchanged(
        UpdateNotMade notMade, string current, string latest, CancellationToken token) =>
        token.IsCancellationRequested
            ? new CommandLineUpdateResult.FailedThenCancelled(notMade, current, latest)
            : new CommandLineUpdateResult.CarriedOn(notMade, current, latest);

    private void WriteReleaseLink(string latest) =>
        _host.WriteLine(string.Format(Strings.Cli_UpdateDownloadLink,
            Strings.UpdateNotice_Download, UpdateCheckService.ReleasePageUrl(latest)));

    /// <summary>
    /// Says the newer version was not run and why, and writes <paramref name="failure"/> to
    /// crash.log where there is one. Whether the command then runs with this version is
    /// decided by the host after this, by Ctrl+C and, for a Move or Delete, by its own take
    /// of the mutex, so the line saying it runs is the host's to print once it does.
    /// </summary>
    private CommandLineUpdateResult CarryOn(
        UpdateNotMade notMade, Exception? failure, string current, string latest, CancellationToken token)
    {
        if (failure is not null) Helpers.CrashLog.TryWrite(failure);
        SayGuarded(() => _host.WriteLine(notMade switch
        {
            UpdateNotMade.InUse => Strings.Cli_UpdateInUse,
            UpdateNotMade.DownloadFailed => Strings.UpdateNotice_DownloadFailed,
            _ => Strings.Cli_UpdateNotRun,
        }));
        if (token.IsCancellationRequested)
            return new CommandLineUpdateResult.FailedThenCancelled(notMade, current, latest);
        return new CommandLineUpdateResult.CarriedOn(notMade, current, latest);
    }

    private UpdateAvailable? Check(bool canAsk, CancellationToken token)
    {
        using var bound = CancellationTokenSource.CreateLinkedTokenSource(token);
        if (canAsk) bound.CancelAfter(_watchedCheckBound);
        try
        {
            return _check(bound.Token).GetAwaiter().GetResult() as UpdateAvailable;
        }
        catch (OperationCanceledException)
        {
            return null;
        }
    }

    /// <summary>
    /// What the release says about running its command line here, and on the setup's copy
    /// its setup, read before the question so a person is not asked one whose answer cannot
    /// be acted on. A read that fails answers null, and the download's own read decides.
    /// </summary>
    private UpdateRefusal? Requirements(bool canAsk, string latest, CancellationToken token)
    {
        try
        {
            return _updater.CheckStagedRunRequirementsAsync(latest, canAsk ? _watchedCheckBound : null, token)
                .GetAwaiter().GetResult();
        }
        catch (Exception)
        {
            return null;
        }
    }

    private enum Answer { Yes, No, Cancelled, TimedOut }

    /// <summary>
    /// Asks the question until it gets an answer. Enter, or an answer starting with this
    /// language's letter for yes or with Y, is yes; one starting with its letter for no or
    /// with N is no; anything else asks again. The read is handed the token and ends as it
    /// is cancelled, by Ctrl+C or by the console being closed, and the question then
    /// answers cancelled. The end of input is no, unless the token is cancelled while the
    /// question waits on it, up to <see cref="ControlCBound"/> in a run, since Ctrl+C at the
    /// prompt can itself be read as the end of input. Nothing answered within the answer
    /// bound is <see cref="Answer.TimedOut"/>.
    /// </summary>
    private Answer Ask(CancellationToken token)
    {
        while (true)
        {
            switch (ConsoleQuestion.Ask(
                _host, (yes, no) => string.Format(Strings.Cli_UpdatePrompt, yes, no),
                token, _controlCBound, _answerBound, out _))
            {
                case ConsoleAnswer.Yes: return Answer.Yes;
                case ConsoleAnswer.No: return Answer.No;
                case ConsoleAnswer.Cancelled: return Answer.Cancelled;
                case ConsoleAnswer.TimedOut: return Answer.TimedOut;
            }
        }
    }

    /// <summary>
    /// Downloads the newer version into its staging folder and runs the command with it
    /// there. Where the download fails, is refused or the newer version does not start, what
    /// was downloaded is taken away and the command is left to this version. Once the newer
    /// version has started nothing is taken away: it runs from the staging folder and then
    /// takes this copy's place from there. <paramref name="canAsk"/> says whether a yes
    /// sent the newer version to scan only, and <paramref name="asked"/> whether the
    /// question was printed, answered or not.
    /// </summary>
    private CommandLineUpdateResult Update(
        bool canAsk, bool asked, string command, IReadOnlyList<string> arguments, string exePath,
        string current, string latest, CancellationToken token)
    {
        PreparedUpdate prepared;
        try
        {
            _host.WriteLine(string.Format(Strings.UpdateNotice_Downloading, latest));
            prepared = _updater.PrepareStagedRunAsync(latest, token).GetAwaiter().GetResult();
        }
        // A refusal or failure that comes back with Ctrl+C pressed ends the run as cancelled
        // too, since the run was cancelled.
        catch (Exception) when (token.IsCancellationRequested)
        {
            return new CommandLineUpdateResult.Cancelled();
        }
        catch (UpdateRefusedException refused)
        {
            // The download's own read of the release's file ruled this copy out, where the
            // read before the question could not be made. Nothing was downloaded. Where the
            // question was asked, the line naming the newer version is said again, since the
            // reason follows it.
            if (asked) _host.WriteLine(string.Format(Strings.UpdateNotice_Available, latest));
            return Refused(refused.Refusal, current, latest, token);
        }
        catch (Exception ex)
        {
            var notMade = ex is UpdateFailedException { Stage: UpdateFailureStage.Download }
                ? UpdateNotMade.DownloadFailed
                : UpdateNotMade.NotRun;
            return CarryOn(notMade, ex, current, latest, token);
        }

        IReadOnlyList<string> run = canAsk ? ["/s"] : arguments;
        int? exitCode;
        try
        {
            exitCode = _host.RunNewerVersion(
                prepared.FileToStart, run, exePath, current, latest, SayRunningNewer, token);
        }
        catch (OperationCanceledException) when (token.IsCancellationRequested)
        {
            // Ctrl+C before the newer version started, which has been ended.
            prepared.Undo();
            return new CommandLineUpdateResult.Cancelled();
        }
        catch (Exception ex)
        {
            prepared.Undo();
            return CarryOn(UpdateNotMade.NotRun, ex, current, latest, token);
        }
        if (exitCode is null)
        {
            // It ended, or was ended, without setting the started event, so it ran none of the
            // command, and nothing on disk has changed.
            prepared.Undo();
            return CarryOn(UpdateNotMade.NotRun, null, current, latest, token);
        }

        // The newer version runs from the staging folder and then takes this copy's place from
        // there, so nothing is undone: the files stay held until this process ends
        // (IAppUpdater.PrepareStagedRunAsync).
        // A newer version ended for Ctrl+C before it had a handler for it exits with
        // ControlCExit, and the same Ctrl+C can reach the token here after that exit, so
        // the run waits for it before handing the exit code back.
        if (exitCode == ControlCExit) token.WaitHandle.WaitOne(_controlCBound);
        return new CommandLineUpdateResult.HandedOver(exitCode.Value, current, latest, string.Join(' ', run));

        // Says the newer version runs the command and takes this copy's place afterwards, and
        // where a person answered yes to a Delete or a Move, that it runs a scan, unless
        // Ctrl+C has come. Each line is written on its own, so one that fails takes neither
        // the other line nor the run with it.
        void SayRunningNewer()
        {
            SayLine(string.Format(Strings.Cli_UpdateRunningNewer, run[0], latest));
            if (token.IsCancellationRequested) return;
            if (canAsk && command == "/d") SayLine(Strings.Cli_UpdateListsOnlyAfterDelete);
            if (canAsk && command == "/m") SayLine(Strings.Cli_UpdateListsOnlyAfterMove);
        }
    }

    private void SayLine(string line) => SayGuarded(() => _host.WriteLine(line));

    /// <summary>
    /// Writes a line whose failure changes nothing about the run. Nothing it meets leaves
    /// it. The host's writes never throw (<see cref="IConsolePrompt"/>), and an
    /// <see cref="IOException"/> from a host that does all the same is a console refusing
    /// the line and is passed over. Any other failure is written to crash.log: a fault in a
    /// host, or a line that cannot be built from a translation naming a placeholder its
    /// English does not, which scripts/check-resx-parity.mjs refuses.
    /// </summary>
    private static void SayGuarded(Action say)
    {
        try
        {
            say();
        }
        catch (IOException)
        {
        }
        catch (Exception ex)
        {
            Helpers.CrashLog.TryWrite(ex);
        }
    }
}
