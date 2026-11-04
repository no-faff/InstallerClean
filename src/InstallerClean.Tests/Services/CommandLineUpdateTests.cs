using InstallerClean.Resources;
using InstallerClean.Services;

namespace InstallerClean.Tests.Services;

/// <summary>
/// The command line's update before its scan: what it says and asks, when it holds the
/// single-instance mutex, what it starts and with what, and what it takes away when the
/// newer version is not run. The console, the mutex and the newer version's run are a
/// recording host, and the download a stand-in updater.
/// </summary>
public sealed class CommandLineUpdateTests : IDisposable
{
    private const string Current = "3.5.0";
    private const string Latest = "3.6.0";
    private const string Exe = @"C:\Tools\installerclean-cli.exe";
    private const string Staged = @"C:\ProgramData\InstallerClean-update-1\installerclean-cli.exe";

    private readonly RecordingHost _host = new();
    private readonly IDisposable _crashLog = TestCrashLog.OwnFolder();

    public void Dispose() => _crashLog.Dispose();

    private static Func<CancellationToken, Task<UpdateCheckResult>> Answers(UpdateCheckResult result) =>
        _ => Task.FromResult(result);

    private static readonly Func<CancellationToken, Task<UpdateCheckResult>> NewerOut =
        Answers(new UpdateAvailable(Current, Latest));

    /// <summary>The watched check's bound every run here is given.</summary>
    private static readonly TimeSpan WatchedBound = TimeSpan.FromMilliseconds(200);

    /// <summary>The bound on an answer every run here is given.</summary>
    private static readonly TimeSpan AnswerBound = TimeSpan.FromMilliseconds(50);

    private CommandLineUpdateResult Run(
        FakeUpdater updater, bool canAsk, string command = "/s", string[]? arguments = null,
        Func<CancellationToken, Task<UpdateCheckResult>>? check = null, CancellationToken token = default) =>
        new CommandLineUpdate(check ?? NewerOut, updater, _host, WatchedBound, TimeSpan.FromMilliseconds(20), AnswerBound)
            .Run(canAsk, command, arguments ?? [command], Exe, token);

    private static string Prompt() =>
        string.Format(Strings.Cli_UpdatePrompt,
            Strings.Cli_UpdateAnswerYes.ToUpper(System.Globalization.CultureInfo.CurrentUICulture),
            Strings.Cli_UpdateAnswerNo.ToLower(System.Globalization.CultureInfo.CurrentUICulture)) + " ";

    private static string ReleaseLink =>
        string.Format(Strings.Cli_UpdateDownloadLink, Strings.UpdateNotice_Download, UpdateCheckService.ReleasePageUrl(Latest));

    private static string Available => string.Format(Strings.UpdateNotice_Available, Latest);

    private static string Downloading => string.Format(Strings.UpdateNotice_Downloading, Latest);

    [Fact]
    public void No_newer_version_tries_nothing_and_says_nothing()
    {
        var updater = new FakeUpdater();

        var result = Run(updater, canAsk: true, check: Answers(new UpToDate(Current)));

        Assert.Same(CommandLineUpdateResult.NothingTried, result);
        Assert.Empty(_host.Output);
        Assert.Equal(0, updater.Prepares);
    }

    [Fact]
    public void Ctrl_C_during_the_check_is_the_runs_cancel()
    {
        using var cancel = new CancellationTokenSource();
        cancel.Cancel();

        var result = Run(new FakeUpdater(), canAsk: false, token: cancel.Token,
            check: token => Task.FromCanceled<UpdateCheckResult>(token));

        Assert.IsType<CommandLineUpdateResult.Cancelled>(result);
    }

    [Theory]
    [InlineData(AppCopyKind.FromSource, UpdateNotMade.FromSource)]
    public void A_copy_that_cannot_update_itself_says_so_notes_why_and_runs_this_version(
        AppCopyKind kind, UpdateNotMade expected)
    {
        var updater = new FakeUpdater(kind);

        var result = Run(updater, canAsk: false, command: "/d");

        Assert.Equal(new CommandLineUpdateResult.CarriedOn(expected, Current, Latest), result);
        Assert.Equal([Available, Strings.UpdateNotice_Recommended, ReleaseLink], _host.Output);
        Assert.Equal(0, updater.Prepares);
        Assert.Equal(0, updater.RequirementReads);
        Assert.Empty(_host.LockTakes);
    }

    [Fact]
    public void A_copy_that_cannot_tell_how_it_was_installed_says_so()
    {
        var result = Run(new FakeUpdater(AppCopyKind.Undetermined), canAsk: false);

        Assert.Equal(new CommandLineUpdateResult.CarriedOn(UpdateNotMade.CopyCannotTell, Current, Latest), result);
        Assert.Equal([Available, Strings.UpdateNotice_CopyCannotTell, ReleaseLink], _host.Output);
    }

    [Fact]
    public void A_copy_that_cannot_update_itself_after_Ctrl_C_runs_nothing()
    {
        using var cancel = new CancellationTokenSource();
        Func<CancellationToken, Task<UpdateCheckResult>> check = _ =>
        {
            cancel.Cancel();
            return Task.FromResult<UpdateCheckResult>(new UpdateAvailable(Current, Latest));
        };

        var result = Run(new FakeUpdater(AppCopyKind.FromSource), canAsk: false, check: check, token: cancel.Token);

        Assert.Equal(new CommandLineUpdateResult.FailedThenCancelled(UpdateNotMade.FromSource, Current, Latest), result);
    }

    [Theory]
    [InlineData(UpdateRefusal.NeedsNetFramework48, UpdateNotMade.NeedsNetFramework48)]
    [InlineData(UpdateRefusal.CannotInstallFromThisCopy, UpdateNotMade.CannotInstallAutomatically)]
    public void A_release_that_rules_this_copy_out_is_said_before_any_question(
        UpdateRefusal refusal, UpdateNotMade expected)
    {
        var updater = new FakeUpdater { Requirements = () => refusal };

        var result = Run(updater, canAsk: true, command: "/d");

        Assert.Equal(new CommandLineUpdateResult.CarriedOn(expected, Current, Latest), result);
        Assert.Equal(0, _host.Reads);
        Assert.Equal(0, updater.Prepares);
        Assert.Empty(_host.LockTakes);
        Assert.Equal(Available, _host.Output[0]);
        Assert.DoesNotContain(Prompt(), _host.Output);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void The_requirements_read_is_bounded_only_where_a_person_waits(bool canAsk)
    {
        var updater = new FakeUpdater();
        _host.Answers.Enqueue("n");

        Run(updater, canAsk);

        Assert.Equal([canAsk ? WatchedBound : null], updater.RequirementLimits);
    }

    [Fact]
    public void Keys_typed_before_the_question_are_discarded_each_time_it_is_asked()
    {
        // An answer that is neither yes nor no asks again, and each asking discards what was
        // typed before it, ahead of printing it, so a key pressed during the update check
        // answers nothing.
        var updater = new FakeUpdater();
        _host.Answers.Enqueue("x");
        _host.Answers.Enqueue("n");

        var result = Run(updater, canAsk: true, command: "/d");

        Assert.Same(CommandLineUpdateResult.NothingTried, result);
        Assert.Equal([Available, Prompt(), Prompt()], _host.Output);
        Assert.Equal([1, 2], _host.DiscardsAtEachQuestion);
        Assert.Equal(0, updater.Prepares);
    }

    [Fact]
    public void A_watched_no_runs_this_version_and_downloads_nothing()
    {
        var updater = new FakeUpdater();
        _host.Answers.Enqueue("n");

        var result = Run(updater, canAsk: true, command: "/d");

        Assert.Same(CommandLineUpdateResult.NothingTried, result);
        Assert.Equal([Available, Prompt()], _host.Output);
        Assert.Equal(0, updater.Prepares);
        Assert.Equal([true], _host.LockTakes);
        Assert.Equal(1, _host.LockReleases);
    }

    [Theory]
    [InlineData("/d")]
    [InlineData("/m")]
    public void A_watched_yes_to_a_Move_or_Delete_lets_the_mutex_go_before_the_newer_version_scans(string command)
    {
        var updater = new FakeUpdater();
        _host.Answers.Enqueue(string.Empty);
        _host.NewerVersionExitCode = 0;

        var result = Run(updater, canAsk: true, command: command, arguments: [command, @"D:\kept"]);

        Assert.Equal(new CommandLineUpdateResult.HandedOver(0, Current, Latest, "/s"), result);
        Assert.Equal((Staged, "/s", Exe, Current, Latest, false), Assert.Single(_host.NewerVersionRuns));
        Assert.Equal([true], _host.LockTakes);
        Assert.Equal(1, _host.LockReleases);
        Assert.Equal(
            [
                Available, Prompt(), Downloading,
                string.Format(Strings.Cli_UpdateRunningNewer, "/s", Latest),
                command == "/d" ? Strings.Cli_UpdateListsOnlyAfterDelete : Strings.Cli_UpdateListsOnlyAfterMove,
            ],
            _host.Output);
        Assert.False(updater.Undone);
    }

    [Fact]
    public void An_unwatched_run_updates_without_asking_and_passes_the_command_as_given()
    {
        var updater = new FakeUpdater();
        _host.NewerVersionExitCode = 4;

        var result = Run(updater, canAsk: false, command: "/m", arguments: ["/m", @"D:\kept"]);

        Assert.Equal(new CommandLineUpdateResult.HandedOver(4, Current, Latest, @"/m D:\kept"), result);
        Assert.Equal((Staged, @"/m D:\kept", Exe, Current, Latest, true), Assert.Single(_host.NewerVersionRuns));
        Assert.Equal(0, _host.Reads);
        Assert.Equal([Available, Downloading, string.Format(Strings.Cli_UpdateRunningNewer, "/m", Latest)], _host.Output);
    }

    [Theory]
    [InlineData("/d", true)]
    [InlineData("/m", true)]
    [InlineData("/s", false)]
    public void A_question_nobody_answers_updates_as_an_unwatched_run_does(string command, bool holdsLock)
    {
        var updater = new FakeUpdater();
        _host.Answers.Enqueue(RecordingHost.NoAnswer);
        _host.NewerVersionExitCode = 4;

        var result = Run(updater, canAsk: true, command: command, arguments: [command, "/v"]);

        Assert.Equal(new CommandLineUpdateResult.HandedOver(4, Current, Latest, command + " /v"), result);
        Assert.Equal((Staged, command + " /v", Exe, Current, Latest, holdsLock), Assert.Single(_host.NewerVersionRuns));
        Assert.Equal(holdsLock ? [true] : [], _host.LockTakes);
        Assert.Equal(holdsLock ? 1 : 0, _host.LockReleases);
        Assert.Equal(1, _host.NobodyAnsweredCalls);
        Assert.Equal(
            [
                Available, Prompt(), string.Empty, Strings.Cli_UpdateNoAnswer, Downloading,
                string.Format(Strings.Cli_UpdateRunningNewer, command, Latest),
            ],
            _host.Output);
    }

    [Fact]
    public void Ctrl_C_while_the_question_waits_is_the_runs_cancel_not_a_question_nobody_answered()
    {
        var updater = new FakeUpdater();
        _host.Answers.Enqueue(RecordingHost.NoAnswer);
        using var cancel = new CancellationTokenSource();
        _host.OnRead = () => cancel.CancelAfter(TimeSpan.FromMilliseconds(10));

        var result = new CommandLineUpdate(
                NewerOut, updater, _host, WatchedBound, TimeSpan.FromMilliseconds(20), TimeSpan.FromMinutes(1))
            .Run(true, "/d", ["/d"], Exe, cancel.Token);

        Assert.IsType<CommandLineUpdateResult.Cancelled>(result);
        Assert.Equal(0, updater.Prepares);
        Assert.DoesNotContain(Strings.Cli_UpdateNoAnswer, _host.Output);
        Assert.Equal(1, _host.LockReleases);
        Assert.Equal(0, _host.NobodyAnsweredCalls);
    }

    [Fact]
    public void A_refusal_from_the_downloads_own_read_names_the_newer_version_again_after_a_question_nobody_answered()
    {
        var updater = new FakeUpdater
        {
            Prepare = () => throw new UpdateRefusedException(UpdateRefusal.CannotInstallFromThisCopy, Latest),
        };
        _host.Answers.Enqueue(RecordingHost.NoAnswer);

        var result = Run(updater, canAsk: true);

        Assert.Equal(new CommandLineUpdateResult.CarriedOn(UpdateNotMade.CannotInstallAutomatically, Current, Latest), result);
        Assert.Equal(
            [Available, Prompt(), string.Empty, Strings.Cli_UpdateNoAnswer, Downloading, Available,
                string.Format(Strings.UpdateNotice_CannotInstallAutomatically, Latest), ReleaseLink],
            _host.Output);
    }

    [Fact]
    public void The_answer_bound_is_the_minute_the_lines_name()
    {
        // Cli.UpdateNoAnswer, Cli.ReportNoAnswer and Cli.Help.UpdateNote say "a minute" in
        // every language, so a change here changes them too.
        Assert.Equal(TimeSpan.FromMinutes(1), ConsoleQuestion.AnswerBound);
    }

    [Fact]
    public void A_scan_takes_no_mutex()
    {
        _host.NewerVersionExitCode = 0;
        _host.LockAvailable = false;

        var result = Run(new FakeUpdater(), canAsk: false);

        Assert.IsType<CommandLineUpdateResult.HandedOver>(result);
        Assert.Empty(_host.LockTakes);
        Assert.False(Assert.Single(_host.NewerVersionRuns).HeldLock);
    }

    [Theory]
    [InlineData("/d")]
    [InlineData("/m")]
    public void A_Move_or_Delete_with_InstallerClean_in_use_says_so_and_leaves_whether_it_runs_to_the_host(string command)
    {
        var updater = new FakeUpdater();
        _host.LockAvailable = false;

        var result = Run(updater, canAsk: true, command: command);

        Assert.Equal(new CommandLineUpdateResult.CarriedOn(UpdateNotMade.InUse, Current, Latest), result);
        // Whether the run goes on is the host's to say once it knows, as the result asks.
        Assert.Equal([Available, Strings.Cli_UpdateInUse], _host.Output);
        Assert.Equal(0, _host.Reads);
        Assert.Equal(0, updater.Prepares);
        Assert.Equal(0, _host.LockReleases);
    }

    [Theory]
    [InlineData(UpdateFailureStage.Download, UpdateNotMade.DownloadFailed)]
    [InlineData(UpdateFailureStage.Install, UpdateNotMade.NotRun)]
    public void A_download_that_fails_runs_this_version_and_releases_the_mutex(
        UpdateFailureStage stage, UpdateNotMade expected)
    {
        var updater = new FakeUpdater { Prepare = () => throw new UpdateFailedException(stage, "failed") };

        var result = Run(updater, canAsk: false, command: "/d");

        Assert.Equal(new CommandLineUpdateResult.CarriedOn(expected, Current, Latest), result);
        Assert.Equal(
            [Available, Downloading,
                expected == UpdateNotMade.DownloadFailed ? Strings.UpdateNotice_DownloadFailed : Strings.Cli_UpdateNotRun],
            _host.Output);
        Assert.Empty(_host.NewerVersionRuns);
        Assert.Equal(1, _host.LockReleases);
        Assert.Contains("UpdateFailedException", TestCrashLog.Text());
    }

    [Fact]
    public void A_refusal_from_the_downloads_own_read_names_the_newer_version_again_where_asked()
    {
        var updater = new FakeUpdater
        {
            Prepare = () => throw new UpdateRefusedException(UpdateRefusal.CannotInstallFromThisCopy, Latest),
        };
        _host.Answers.Enqueue("y");

        var result = Run(updater, canAsk: true);

        Assert.Equal(new CommandLineUpdateResult.CarriedOn(UpdateNotMade.CannotInstallAutomatically, Current, Latest), result);
        Assert.Equal(
            [Available, Prompt(), Downloading, Available,
                string.Format(Strings.UpdateNotice_CannotInstallAutomatically, Latest), ReleaseLink],
            _host.Output);
    }

    [Fact]
    public void A_newer_version_that_did_not_start_is_taken_away_and_this_version_runs_the_command()
    {
        var updater = new FakeUpdater();
        _host.NewerVersionExitCode = null;

        var result = Run(updater, canAsk: false, command: "/d");

        Assert.Equal(new CommandLineUpdateResult.CarriedOn(UpdateNotMade.NotRun, Current, Latest), result);
        Assert.True(updater.Undone);
        Assert.Equal([Available, Downloading, Strings.Cli_UpdateNotRun], _host.Output);
        Assert.Equal(1, _host.LockReleases);
    }

    [Fact]
    public void A_start_that_throws_is_taken_away_and_this_version_runs_the_command()
    {
        var updater = new FakeUpdater();
        _host.OnNewerVersion = () => throw new InvalidOperationException("start");

        var result = Run(updater, canAsk: false);

        Assert.Equal(new CommandLineUpdateResult.CarriedOn(UpdateNotMade.NotRun, Current, Latest), result);
        Assert.True(updater.Undone);
    }

    [Fact]
    public void Ctrl_C_before_the_newer_version_started_takes_the_download_away()
    {
        using var cancel = new CancellationTokenSource();
        var updater = new FakeUpdater();
        _host.OnNewerVersion = cancel.Cancel;
        _host.NewerVersionExitCode = 0;

        var result = Run(updater, canAsk: false, command: "/d", token: cancel.Token);

        Assert.IsType<CommandLineUpdateResult.Cancelled>(result);
        Assert.True(updater.Undone);
        Assert.Equal(1, _host.LockReleases);
    }

    [Fact]
    public void Ctrl_C_during_the_download_is_the_runs_cancel()
    {
        using var cancel = new CancellationTokenSource();
        var updater = new FakeUpdater
        {
            Prepare = () =>
            {
                cancel.Cancel();
                throw new OperationCanceledException(cancel.Token);
            },
        };

        var result = Run(updater, canAsk: false, token: cancel.Token);

        Assert.IsType<CommandLineUpdateResult.Cancelled>(result);
        Assert.Empty(_host.NewerVersionRuns);
    }

    [Fact]
    public void Ctrl_C_after_a_failed_download_runs_nothing()
    {
        using var cancel = new CancellationTokenSource();
        var updater = new FakeUpdater
        {
            Prepare = () => throw new UpdateFailedException(UpdateFailureStage.Download, "failed"),
        };
        _host.OnWriteLine = line =>
        {
            if (line == Strings.UpdateNotice_DownloadFailed) cancel.Cancel();
        };

        var result = Run(updater, canAsk: false, token: cancel.Token);

        Assert.Equal(new CommandLineUpdateResult.FailedThenCancelled(UpdateNotMade.DownloadFailed, Current, Latest), result);
    }

    [Fact]
    public void A_newer_version_that_started_is_never_taken_away()
    {
        var updater = new FakeUpdater();
        _host.NewerVersionExitCode = 1;

        var result = Run(updater, canAsk: false, command: "/d");

        Assert.Equal(new CommandLineUpdateResult.HandedOver(1, Current, Latest, "/d"), result);
        Assert.False(updater.Undone);
    }

    [Fact]
    public void The_setups_copy_runs_the_command_with_its_newer_command_line()
    {
        var updater = new FakeUpdater(AppCopyKind.Setup);
        _host.NewerVersionExitCode = 0;

        var result = Run(updater, canAsk: false, command: "/d");

        Assert.IsType<CommandLineUpdateResult.HandedOver>(result);
        Assert.Equal(Staged, Assert.Single(_host.NewerVersionRuns).File);
        Assert.Equal(1, updater.RequirementReads);
    }

    /// <summary>
    /// The download: a command line of <see cref="Latest"/> staged at <see cref="Staged"/>,
    /// or whatever <see cref="Prepare"/> throws.
    /// </summary>
    private sealed class FakeUpdater(AppCopyKind kind = AppCopyKind.CommandLineDownload) : IAppUpdater
    {
        public AppCopyKind Kind => kind;
        public ScoopUpdate? ScoopUpdate => null;

        public Func<UpdateRefusal?> Requirements { get; init; } = () => null;
        public int RequirementReads { get; private set; }
        public List<TimeSpan?> RequirementLimits { get; } = [];

        public Action? Prepare { get; init; }
        public int Prepares { get; private set; }
        public bool Undone { get; private set; }

        public Task<UpdateRefusal?> CheckRequirementsAsync(
            string version, TimeSpan? within, CancellationToken cancellationToken) =>
            throw new NotSupportedException("The command line judges its run from staging.");

        public Task<UpdateRefusal?> CheckStagedRunRequirementsAsync(
            string version, TimeSpan? within, CancellationToken cancellationToken)
        {
            RequirementReads++;
            RequirementLimits.Add(within);
            var answer = Requirements();
            cancellationToken.ThrowIfCancellationRequested();
            return Task.FromResult(answer);
        }

        public Task<PreparedUpdate> PrepareAsync(
            string version, IProgress<UpdateProgress>? progress, CancellationToken cancellationToken) =>
            throw new NotSupportedException("The command line runs from staging.");

        public Task<PreparedUpdate> PrepareStagedRunAsync(string version, CancellationToken cancellationToken)
        {
            Prepares++;
            Prepare?.Invoke();
            return Task.FromResult(new PreparedUpdate(version, Staged, string.Empty, () => Undone = true, Exe));
        }
    }

    private sealed class RecordingHost : ICommandLineUpdateHost
    {
        /// <summary>An answer that never comes: the read waits until its token is cancelled.</summary>
        public const string NoAnswer = "\0 no answer";

        public List<string> Output { get; } = [];
        public Queue<string?> Answers { get; } = new();
        public int Reads { get; private set; }
        public bool LockAvailable { get; set; } = true;
        public bool HoldsLock { get; private set; }
        public List<bool> LockTakes { get; } = [];
        public int LockReleases { get; private set; }
        public int NobodyAnsweredCalls { get; private set; }
        public Action<string>? OnWriteLine { get; set; }
        public Action? OnNewerVersion { get; set; }
        public Action? OnRead { get; set; }
        public int? NewerVersionExitCode { get; set; }

        /// <summary>Each newer version run: what it ran, its arguments joined, and whether the mutex was held.</summary>
        public List<(string File, string Arguments, string Replaces, string From, string To, bool HeldLock)>
            NewerVersionRuns { get; } = [];

        public void WriteLine(string line)
        {
            OnWriteLine?.Invoke(line);
            Output.Add(line);
        }

        // The question and the answer share a line on screen; recorded as an entry of its own.
        public void Write(string text)
        {
            DiscardsAtEachQuestion.Add(Discards);
            Output.Add(text);
        }

        public int Discards { get; private set; }

        /// <summary>How many discards had been made as each question was printed.</summary>
        public List<int> DiscardsAtEachQuestion { get; } = [];

        public void DiscardTypedAhead() => Discards++;

        public string? ReadLine(CancellationToken token)
        {
            Reads++;
            OnRead?.Invoke();
            var answer = Answers.Count > 0 ? Answers.Dequeue() : null;
            if (answer != NoAnswer) return answer;
            token.WaitHandle.WaitOne();
            return null;
        }

        public bool TryTakeInstanceLock()
        {
            HoldsLock = LockAvailable;
            LockTakes.Add(HoldsLock);
            return HoldsLock;
        }

        public void NobodyAnswered() => NobodyAnsweredCalls++;

        public void ReleaseInstanceLock()
        {
            LockReleases++;
            HoldsLock = false;
        }

        public int? RunNewerVersion(
            string file, IReadOnlyList<string> arguments, string replaces, string fromVersion, string toVersion,
            Action onStarted, CancellationToken token)
        {
            OnNewerVersion?.Invoke();
            token.ThrowIfCancellationRequested();
            if (NewerVersionExitCode is null) return null;
            NewerVersionRuns.Add((file, string.Join(' ', arguments), replaces, fromVersion, toVersion, HoldsLock));
            // As the console host does, a throw from what is said once it has started goes no further.
            try
            {
                onStarted();
            }
            catch (Exception)
            {
            }
            return NewerVersionExitCode;
        }
    }
}
